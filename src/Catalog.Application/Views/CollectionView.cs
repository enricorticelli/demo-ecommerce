namespace Catalog.Application.Views;

public sealed record CollectionView(Guid Id, string Name, string Slug, string Description, bool IsFeatured);
