namespace Catalog.Api.Contracts.Responses;

public sealed record CategoryResponse(Guid Id, string Name, string Slug, string Description);
