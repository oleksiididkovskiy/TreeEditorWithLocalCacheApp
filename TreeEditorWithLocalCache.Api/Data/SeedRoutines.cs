using Microsoft.EntityFrameworkCore;
using TreeEditorWithLocalCache.Api.Models;

namespace TreeEditorWithLocalCache.Api.Data;

public static class SeedRoutines
{
    public static async Task ResetAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE tree_nodes", cancellationToken);

        var root = NodeInstance("Mammalia"); // use a vertebrate animal of the class Mammalia (Mammal) as root

        var carnivorans = NodeInstance("Carnivora", root.Id);

        var felid = NodeInstance("Felidae", carnivorans.Id);
        var felis = NodeInstance("Felis", felid.Id);
        var cat = NodeInstance("Felis catus", felis.Id);
        var canadianSphynx = NodeInstance("Sphynx cat", cat.Id);

        var dogs = NodeInstance("Canidae", carnivorans.Id);

        var proboscideaOrder = NodeInstance("Proboscidea", root.Id);

        var gomphotheriidaeFamily = NodeInstance("Gomphotheriidae", proboscideaOrder.Id);

        var elephantsFamily = NodeInstance("Elephantidae", proboscideaOrder.Id);
        var africanElephant = NodeInstance("Loxodonta", elephantsFamily.Id);

        dbContext.TreeNodes.AddRange(root,
            carnivorans,
            felid,
            felis,
            cat,
            canadianSphynx,
            dogs,
            proboscideaOrder,
            gomphotheriidaeFamily,
            elephantsFamily,
            africanElephant);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static TreeNode NodeInstance(string value, Guid? parentId = null) => new()
    {
        Id = Guid.NewGuid(),
        ParentId = parentId,
        Value = value
    };
}
