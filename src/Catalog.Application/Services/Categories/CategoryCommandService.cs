using Catalog.Application.Abstractions.Commands;
using Catalog.Application.Abstractions.Repositories;
using Catalog.Application.Abstractions.Rules;
using Catalog.Application.Views;
using Catalog.Domain.Entities;
using Shared.BuildingBlocks.Contracts.IntegrationEvents;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Catalog;
using Shared.BuildingBlocks.Contracts.Messaging;
using Shared.BuildingBlocks.Exceptions;
using Shared.BuildingBlocks.Mapping;

namespace Catalog.Application.Services.Categories;

public sealed class CategoryCommandService(
    ICategoryRepository categoryRepository,
    ICategoryRules categoryRules,
    IDomainEventPublisher eventPublisher,
    IViewMapper<Category, CategoryView> mapper) : ICategoryCommandService
{
    public async Task<CategoryView> CreateAsync(string name, string slug, string description, string correlationId, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim();
        var normalizedSlug = slug.Trim();
        var normalizedDescription = description.Trim();

        await categoryRules.EnsureSlugIsUniqueAsync(normalizedSlug, null, cancellationToken);

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            Slug = normalizedSlug,
            Description = normalizedDescription
        };

        categoryRepository.Add(category);

        var integrationEvent = new CategoryCreatedV1(category.Id, category.Name, category.Slug, CreateMetadata(correlationId));
        await eventPublisher.PublishAndFlushAsync(integrationEvent, cancellationToken);

        return mapper.Map(category);
    }

    public async Task<CategoryView> UpdateAsync(Guid id, string name, string slug, string description, string correlationId, CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundAppException($"Category '{id}' not found.");

        var normalizedName = name.Trim();
        var normalizedSlug = slug.Trim();
        var normalizedDescription = description.Trim();

        await categoryRules.EnsureSlugIsUniqueAsync(normalizedSlug, id, cancellationToken);

        category.Name = normalizedName;
        category.Slug = normalizedSlug;
        category.Description = normalizedDescription;

        var integrationEvent = new CategoryUpdatedV1(category.Id, category.Name, category.Slug, CreateMetadata(correlationId));
        await eventPublisher.PublishAndFlushAsync(integrationEvent, cancellationToken);

        return mapper.Map(category);
    }

    public async Task DeleteAsync(Guid id, string correlationId, CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundAppException($"Category '{id}' not found.");

        await categoryRules.EnsureCanDeleteAsync(id, cancellationToken);

        categoryRepository.Remove(category);

        var integrationEvent = new CategoryDeletedV1(id, CreateMetadata(correlationId));
        await eventPublisher.PublishAndFlushAsync(integrationEvent, cancellationToken);
    }

    private static IntegrationEventMetadata CreateMetadata(string correlationId)
    {
        return new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, correlationId, "Catalog");
    }
}
