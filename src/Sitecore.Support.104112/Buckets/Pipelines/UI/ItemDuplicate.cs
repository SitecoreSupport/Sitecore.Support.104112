using Sitecore.Data.Items;
using Sitecore.Support.Workflows;

namespace Sitecore.Support.Buckets.Pipelines.UI
{
  public class ItemDuplicate : Sitecore.Buckets.Pipelines.UI.ItemDuplicate
  {
    protected override Item DuplicateItem(Item item, string name)
    {
      var worlflow = new WorkflowContext(Context.Data);
      return worlflow.DuplicateItem(item, name);
    }
  }
      
}
