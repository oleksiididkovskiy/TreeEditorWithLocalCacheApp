using Microsoft.EntityFrameworkCore;
using TreeEditorWithLocalCache.Api.Models;

namespace TreeEditorWithLocalCache.Api.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<TreeNode> TreeNodes => Set<TreeNode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var node = modelBuilder.Entity<TreeNode>();

        node.ToTable("tree_nodes");
        node.HasKey(x => x.Id);
        node.Property(x => x.Id).HasColumnName("id");
        node.Property(x => x.ParentId).HasColumnName("parent_id");
        node.Property(x => x.Value).HasColumnName("value").HasMaxLength(500).IsRequired();

        node.HasIndex(x => x.ParentId);

        node.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
