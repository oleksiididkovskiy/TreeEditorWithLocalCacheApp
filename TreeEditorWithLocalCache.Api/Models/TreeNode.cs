namespace TreeEditorWithLocalCache.Api.Models;

public class TreeNode
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Value { get; set; } = string.Empty;

    public TreeNode? Parent { get; set; }
    public ICollection<TreeNode> Children { get; set; } = new List<TreeNode>();
}
