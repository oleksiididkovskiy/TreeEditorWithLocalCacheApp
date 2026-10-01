namespace TreeEditorWithLocalCache.Api.Contracts;

public record TreeNodeDto(Guid Id, Guid? ParentId, string Value);
