namespace Catalog.Api.Contracts.Responses;

public sealed record CollectionResponse(Guid Id, string Name, string Slug, string Description, bool IsFeatured);
