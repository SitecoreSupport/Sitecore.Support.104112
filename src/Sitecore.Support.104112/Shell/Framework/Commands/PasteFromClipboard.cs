using System;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Support.Workflows;

namespace Sitecore.Support.Shell.Framework.Commands
{
  [Serializable]
  public class PasteFromClipboard : Sitecore.Shell.Framework.Commands.PasteFromClipboard
  {
    protected override void PasteItemsFromClipboard(string data, Item item, Item targetItem)
    {
      Assert.ArgumentNotNull(data, "data");
      Assert.ArgumentNotNull(item, "item");
      Assert.ArgumentNotNull(targetItem, "targetItem");
      if (data.StartsWith("sitecore:copy:", StringComparison.InvariantCulture))
      {
        string[] parameters = new string[] { AuditFormatter.FormatItem(item), AuditFormatter.FormatItem(targetItem) };
        Log.Audit(this, "Paste from: {0} to {1}", parameters);
        var worlflow = new WorkflowContext(Context.Data);
        worlflow.CopyItem(targetItem, item, ItemUtil.GetCopyOfName(item, targetItem.Name));
      }
      else if (targetItem.ID != item.ID)
      {
        string[] textArray2 = new string[] { AuditFormatter.FormatItem(item), AuditFormatter.FormatItem(targetItem) };
        Log.Audit(this, "Cut from: {0} to {1}", textArray2);
        targetItem.MoveTo(item);
      }
    }
  }
}
