using Microsoft.EntityFrameworkCore;
using TreeEditorWithLocalCache.Api.Contracts;
using TreeEditorWithLocalCache.Api.Data;
using TreeEditorWithLocalCache.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("ApplicationDb")
    ?? throw new InvalidOperationException("Connection string 'ApplicationDb' was not configured.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<TreeService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();

await EnsureDatabaseAsync(app.Services);

var tree = app.MapGroup("/api/tree");

tree.MapGet("/roots", async (TreeService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.GetRootsAsync(cancellationToken)));

tree.MapGet("/{id:guid}/children", async (Guid id, TreeService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.GetChildrenAsync(id, cancellationToken)));

tree.MapGet("/{id:guid}", async (Guid id, TreeService service, CancellationToken cancellationToken) =>
{
    var node = await service.GetNodeAsync(id, cancellationToken);
    return node is null ? Results.NotFound() : Results.Ok(node);
});

tree.MapPost("/apply", async (ApplyChangesRequest request, TreeService service, CancellationToken cancellationToken) =>
{
    try
    {
        await service.ApplyAsync(request, cancellationToken);
        return Results.NoContent();
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

tree.MapPost("/reset", async (ApplicationDbContext dbContext, CancellationToken cancellationToken) =>
{
    await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

    await SeedRoutines.ResetAsync(dbContext, cancellationToken);
    await transaction.CommitAsync(cancellationToken);

    return Results.NoContent();
});

app.Run();

static async Task EnsureDatabaseAsync(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.EnsureCreatedAsync();

    if (!await db.TreeNodes.AnyAsync())
    {
        await SeedRoutines.ResetAsync(db);
    }
}
