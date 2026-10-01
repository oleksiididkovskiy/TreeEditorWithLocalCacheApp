namespace TreeEditorWithLocalCache.Api.Contracts;

public class ApplyChangesRequest
{
    public List<UpdateNodeRequest> Updates { get; init; } = [];
    public List<CreateNodeRequest> Creates { get; init; } = [];
    public List<Guid> Deletes { get; init; } = [];
}

public record UpdateNodeRequest(Guid Id, string Value);

public record CreateNodeRequest(Guid Id, Guid? ParentId, string Value);
