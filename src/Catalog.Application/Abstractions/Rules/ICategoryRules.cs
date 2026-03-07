namespace Catalog.Application.Abstractions.Rules;

public interface ICategoryRules
{
    Task EnsureSlugIsUniqueAsync(string slug, Guid? excludingId, CancellationToken cancellationToken);
    Task EnsureCanDeleteAsync(Guid id, CancellationToken cancellationToken);
}
