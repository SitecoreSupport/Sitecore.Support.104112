using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Support.Workflows;

namespace Sitecore.Support.Shell.Framework.Pipelines
{
  public class CopyItems : Sitecore.Shell.Framework.Pipelines.CopyItems
  {
    protected override Item CopyItem(Item target, Item itemToCopy)
    {
      Assert.ArgumentNotNull(target, "target");
      Assert.ArgumentNotNull(itemToCopy, "itemToCopy");
      string str = target.Uri.ToString();
      string copyOfName = ItemUtil.GetCopyOfName(target, itemToCopy.Name);
      var workflow = new WorkflowContext(Context.Data);
      Item item = workflow.CopyItem(itemToCopy, target, copyOfName);
      string[] parameters = new string[] { AuditFormatter.FormatItem(itemToCopy), AuditFormatter.FormatItem(item), str };
      Log.Audit(this, "Copy item from: {0} to {1}", parameters);
      return item;
    }
  }
}
