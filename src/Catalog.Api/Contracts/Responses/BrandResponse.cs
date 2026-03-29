namespace Catalog.Api.Contracts.Responses;

public sealed record BrandResponse(Guid Id, string Name, string Slug, string Description);
