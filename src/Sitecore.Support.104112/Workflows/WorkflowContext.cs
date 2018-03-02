using System;
using Sitecore.Common;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Exceptions;
using Sitecore.Security.AccessControl;
using Sitecore.Security.Accounts;
using Sitecore.SecurityModel;
using Sitecore.Sites;
using Sitecore.Workflows;

namespace Sitecore.Support.Workflows
{
  public class WorkflowContext
  {
    private readonly Context.ContextData _context;

    public WorkflowContext(Context.ContextData context)
    {
      _context = context;
    }

    public Item AddItem(string name, BranchItem branch, Item parent)
    {
      Item item2;
      Error.AssertString(name, "name", false);
      Error.AssertObject(branch, "master");
      Error.AssertObject(parent, "parent");
      Item item = null;
      try
      {
        item = parent.Add(name, branch);
        ProcessAdded(item);
        item2 = item;
      }
      catch (WorkflowException exception)
      {
        HandleException(exception);
        throw;
      }
      return item2;
    }

    public Item AddItem(string name, TemplateItem template, Item parent)
    {
      Item item2;
      Error.AssertString(name, "name", false);
      Error.AssertObject(template, "template");
      Error.AssertObject(parent, "parent");
      try
      {
        Item item = parent.Add(name, template);
        ProcessAdded(item);
        item2 = item;
      }
      catch (WorkflowException exception)
      {
        HandleException(exception);
        throw;
      }
      return item2;
    }

    public Item AddItem(string name, TemplateID templateID, Item parent)
    {
      Item item2;
      Error.AssertString(name, "name", false);
      Error.AssertID(templateID, "templateID", false);
      Error.AssertObject(parent, "parent");
      try
      {
        Item item = parent.Add(name, templateID);
        ProcessAdded(item);
        item2 = item;
      }
      catch (WorkflowException exception)
      {
        HandleException(exception);
        throw;
      }
      return item2;
    }

    public Item AddVersion(Item item)
    {
      Assert.ArgumentNotNull(item, "item");
      Item item2 = item.Versions.AddVersion();
      ProcessAdded(item2);
      return item2;
    }

    public Item CopyItem(Item item, Item destination, string copyName)
    {
      Assert.ArgumentNotNull(item, "item");
      Assert.ArgumentNotNull(destination, "destination");
      Assert.ArgumentNotNullOrEmpty(copyName, "copyName");
      return CopyItem(item, destination, copyName, ID.NewID, true);
    }

    public Item CopyItem(Item item, Item destination, string copyName, ID copyID, bool deep)
    {
      Error.AssertObject(item, "item");
      Error.AssertObject(destination, "destination");
      Error.AssertString(copyName, "copyName", false);
      Error.AssertID(copyID, "copyID", false);
      Item item2 = item.CopyTo(destination, copyName, copyID, deep);
      ProcessCopied(item2);
      return item2;
    }

    public void DeleteItem(Item item)
    {
      Error.AssertObject(item, "item");
      item.Delete();
    }

    public Item DuplicateItem(Item item)
    {
      Error.AssertObject(item, "item");
      return DuplicateItem(item, ItemUtil.GetCopyOfName(item.Parent, item.Name));
    }

    public Item DuplicateItem(Item item, string copyName)
    {
      Error.AssertObject(item, "item");
      Error.AssertString(copyName, "copyName", false);
      Item item2 = item.Duplicate(copyName);
      ProcessCopied(item2);
      return item2;
    }

    public AccessResult GetAccess(Item item, AccessRight accessRight, Account account)
    {
      Assert.ArgumentNotNull(item, "item");
      Assert.ArgumentNotNull(accessRight, "accessRight");
      Assert.ArgumentNotNull(account, "account");
      if (((accessRight != AccessRight.ItemWrite) && (accessRight != AccessRight.ItemDelete)) && (accessRight != AccessRight.ItemRemoveVersion))
      {
        return new AccessResult(AccessPermission.Allow, new AccessExplanation(item, account, accessRight, "Sitecore only tests workflow state definition item security settings for delete and write access rights.  All other item access rights are based on the item's security settings.", new object[0]));
      }
      return GetWorkflow(item)?.GetAccess(item, account, accessRight);
    }

    public IWorkflow GetWorkflow(Item item)
    {
      Error.AssertObject(item, "item");
      if (Enabled)
      {
        IWorkflowProvider workflowProvider = item.Database.WorkflowProvider;
        if (workflowProvider != null)
        {
          return workflowProvider.GetWorkflow(item);
        }
      }
      return null;
    }

    private void HandleException(WorkflowException ex)
    {
      try
      {
        if (ex.Item != null)
        {
          using (new SecurityDisabler())
          {
            ex.Item.Delete();
          }
        }
      }
      catch (Exception exception)
      {
        Log.Error("Error handling workflow exception", exception, this);
      }
    }

    public bool HasDefaultWorkflow(Item item)
    {
      Field field = item.Fields[FieldIDs.DefaultWorkflow];
      if (!Settings.Workflows.Enabled)
      {
        return false;
      }
      return ((field != null) && (field.InheritedValue.Length > 0));
    }

    public bool HasWorkflow(Item item)
    {
      if (!Settings.Workflows.Enabled)
      {
        return false;
      }
      return (GetWorkflow(item) != null);
    }

    public bool IsAllowed(AccessRight right, Item item)
    {
      Assert.ArgumentNotNull(right, "right");
      Assert.ArgumentNotNull(item, "item");
      return (GetAccess(item, right, Context.User).Permission == AccessPermission.Allow);
    }

    public bool IsApproved(Item item) =>
        IsApproved(item, null);

    public bool IsApproved(Item item, Database targetDatabase)
    {
      Error.AssertObject(item, "item");
      IWorkflow workflow = GetWorkflow(item);
      if (workflow != null)
      {
        return workflow.IsApproved(item, targetDatabase);
      }
      return true;
    }

    private Item Lock(Item item)
    {
      if (TemplateManager.IsFieldPartOfTemplate(FieldIDs.Lock, item) && !item.Locking.Lock())
      {
        return null;
      }
      return item;
    }

    private void ProcessAdded(Item item)
    {
      if (item != null)
      {
        StartEditing(item);
      }
    }

    private void ProcessCopied(Item item)
    {
      if (item != null)
      {
        ResetWorkflowState(item, true);
        Unlock(item);
        if (item.Versions.Count > 0)
        {
          StartEditing(item);
        }
      }
    }

    public void ResetWorkflowState(Item item)
    {
      Error.AssertObject(item, "item");
      IWorkflow workflow = GetWorkflow(item);
      if (workflow != null)
      {
        workflow.Start(item);
      }
    }

    public void ResetWorkflowState(Item item, bool allVersions)
    {
      Error.AssertObject(item, "item");
      foreach (Item version in item.Versions.GetVersions(true))
      {
        ResetWorkflowState(version);
        ResetWorkflowStateChildren(version, allVersions);
      }     
    }

    public void ResetWorkflowStateChildren(Item root, bool allVersions)
    {
      if (!root.HasChildren) return;
      foreach (Item child in root.Children)
      {
        ResetWorkflowState(child);
        if (child.HasChildren)
        {
          ResetWorkflowStateChildren(child, allVersions);
        }
      }
    }

    public Item StartEditing(Item item)
    {
      Error.AssertObject(item, "item");
      if (Context.User.IsAdministrator)
      {
        return item;
      }
      if (_context.IsAdministrator)
      {
        return Lock(item);
      }
      if (StandardValuesManager.IsStandardValuesHolder(item))
      {
        return Lock(item);
      }
      if (!HasWorkflow(item) && !HasDefaultWorkflow(item))
      {
        return Lock(item);
      }
      if (!IsApproved(item))
      {
        return Lock(item);
      }
      Item item2 = item.Versions.AddVersion();
      if (item2 != null)
      {
        return Lock(item2);
      }
      return null;
    }

    private static Item Unlock(Item item)
    {
      Assert.ArgumentNotNull(item, "item");
      if (TemplateManager.IsFieldPartOfTemplate(FieldIDs.Lock, item))
      {
        if (!item.Locking.IsLocked())
        {
          return item;
        }
        item.Locking.Unlock();
      }
      return item;
    }

    public bool Enabled
    {
      get
      {
        switch (Switcher<WorkflowContextState, WorkflowContextState>.CurrentValue)
        {
          case WorkflowContextState.Default:
            {
              SiteContext site = _context.Site;
              return ((site != null) && site.EnableWorkflow);
            }
          case WorkflowContextState.Disabled:
            return false;
        }
        return true;
      }
    }
  }
}
