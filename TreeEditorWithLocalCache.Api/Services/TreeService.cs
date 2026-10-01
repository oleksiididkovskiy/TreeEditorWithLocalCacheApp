using Microsoft.EntityFrameworkCore;
using TreeEditorWithLocalCache.Api.Contracts;
using TreeEditorWithLocalCache.Api.Data;
using TreeEditorWithLocalCache.Api.Models;

namespace TreeEditorWithLocalCache.Api.Services;

public class TreeService(ApplicationDbContext dbContext)
{
    public async Task<IReadOnlyList<TreeNodeDto>> GetRootsAsync(CancellationToken cancellationToken)
        => await dbContext.TreeNodes.AsNoTracking()
            .Where(x => x.ParentId == null)
            .OrderBy(x => x.Value)
            .Select(x => new TreeNodeDto(x.Id, x.ParentId, x.Value))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TreeNodeDto>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken)
        => await dbContext.TreeNodes.AsNoTracking()
            .Where(x => x.ParentId == parentId)
            .OrderBy(x => x.Value)
            .Select(x => new TreeNodeDto(x.Id, x.ParentId, x.Value))
            .ToListAsync(cancellationToken);

    public async Task<TreeNodeDto?> GetNodeAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.TreeNodes.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new TreeNodeDto(x.Id, x.ParentId, x.Value))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task ApplyAsync(ApplyChangesRequest request, CancellationToken cancellationToken)
    {
        ValidateRequestShape(request);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var deleteIds = request.Deletes.Distinct().ToArray();
            var existingDeleteIds = await dbContext.TreeNodes
                .Where(x => deleteIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            var updateIds = request.Updates.Select(x => x.Id).Distinct().ToArray();
            if (updateIds.Length != request.Updates.Count)
            {
                throw new InvalidOperationException("Duplicate update IDs are not allowed.");
            }

            var existingUpdateIds = await dbContext.TreeNodes
                .Where(x => updateIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            if (existingUpdateIds.Count != updateIds.Length)
            {
                throw new InvalidOperationException("One or more nodes to update do not exist.");
            }

            var existingIds = await dbContext.TreeNodes.Select(x => x.Id).ToListAsync(cancellationToken);
            var existingIdsHashSet = existingIds.ToHashSet();
            ValidateCreates(request.Creates, existingIdsHashSet, deleteIds);

            foreach (var id in existingDeleteIds)
            {
                await DeleteSubtreeAsync(id, cancellationToken);
            }

            if (request.Updates.Count > 0)
            {
                var nodes = await dbContext.TreeNodes
                    .Where(x => updateIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, cancellationToken);

                if (nodes.Count != updateIds.Length)
                {
                    throw new InvalidOperationException("A node marked for update was deleted by another pending delete.");
                }

                foreach (var update in request.Updates)
                {
                    nodes[update.Id].Value = update.Value.Trim();
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }

            foreach (var create in OrderCreates(request.Creates))
            {
                dbContext.TreeNodes.Add(new TreeNode
                {
                    Id = create.Id,
                    ParentId = create.ParentId,
                    Value = create.Value.Trim()
                });
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw new InvalidOperationException("Something went wrong on applying the changes.");
        }
    }

    private async Task DeleteSubtreeAsync(Guid rootId, CancellationToken cancellationToken)
    {
        const string sql = """
            WITH RECURSIVE descendants AS
            (
                SELECT id FROM tree_nodes WHERE id = {0}
                UNION ALL
                SELECT child.id
                FROM tree_nodes child
                JOIN descendants parent ON child.parent_id = parent.id
            )
            DELETE FROM tree_nodes
            WHERE id IN (SELECT id FROM descendants);
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql, [rootId], cancellationToken);
    }

    private static void ValidateRequestShape(ApplyChangesRequest request)
    {
        foreach (var update in request.Updates)
        {
            ValidateValue(update.Value);
        }

        foreach (var create in request.Creates)
        {
            if (create.Id == Guid.Empty)
            {
                throw new InvalidOperationException("Created node IDs must not be empty.");
            }

            ValidateValue(create.Value);
        }
    }

    private static void ValidateCreates(
        IReadOnlyCollection<CreateNodeRequest> creates,
        IReadOnlySet<Guid> existingIds,
        IReadOnlyCollection<Guid> deletedIds)
    {
        var createIds = creates.Select(x => x.Id).ToHashSet();
        if (createIds.Count != creates.Count)
        {
            throw new InvalidOperationException("Duplicate created node IDs are not allowed.");
        }

        if (createIds.Overlaps(existingIds))
        {
            throw new InvalidOperationException("A created node ID already exists in the database.");
        }

        foreach (var create in creates)
        {
            if (create.ParentId is Guid parentId &&
                !existingIds.Contains(parentId) &&
                !createIds.Contains(parentId))
            {
                throw new InvalidOperationException($"Parent {parentId} does not exist.");
            }

            if (create.ParentId is Guid parent && deletedIds.Contains(parent))
            {
                throw new InvalidOperationException("A new node cannot be attached to a node being deleted.");
            }
        }
    }

    private static IEnumerable<CreateNodeRequest> OrderCreates(IReadOnlyList<CreateNodeRequest> creates)
    {
        var pending = creates.ToDictionary(x => x.Id);
        var emitted = new HashSet<Guid>();

        while (pending.Count > 0)
        {
            var ready = pending.Values
                .Where(x => x.ParentId is null || !pending.ContainsKey(x.ParentId.Value))
                .ToList();

            if (ready.Count == 0)
            {
                throw new InvalidOperationException("Created nodes contain a parent cycle.");
            }

            foreach (var item in ready)
            {
                emitted.Add(item.Id);
                pending.Remove(item.Id);
                yield return item;
            }
        }
    }

    private static void ValidateValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("Node value is required.");
        }

        if (value.Trim().Length > 500)
        {
            throw new InvalidOperationException("Node value must be at most 500 characters.");
        }
    }
}
