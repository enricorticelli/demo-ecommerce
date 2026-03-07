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

namespace Catalog.Application.Services.Brands;

public sealed class BrandCommandService(
    IBrandRepository brandRepository,
    IBrandRules brandRules,
    IDomainEventPublisher eventPublisher,
    IViewMapper<Brand, BrandView> mapper) : IBrandCommandService
{
    public async Task<BrandView> CreateAsync(string name, string slug, string description, string correlationId, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim();
        var normalizedSlug = slug.Trim();
        var normalizedDescription = description.Trim();

        await brandRules.EnsureSlugIsUniqueAsync(normalizedSlug, null, cancellationToken);

        var brand = new Brand
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            Slug = normalizedSlug,
            Description = normalizedDescription
        };

        brandRepository.Add(brand);

        var integrationEvent = new BrandCreatedV1(brand.Id, brand.Name, brand.Slug, CreateMetadata(correlationId));
        await eventPublisher.PublishAndFlushAsync(integrationEvent, cancellationToken);

        return mapper.Map(brand);
    }

    public async Task<BrandView> UpdateAsync(Guid id, string name, string slug, string description, string correlationId, CancellationToken cancellationToken)
    {
        var brand = await brandRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundAppException($"Brand '{id}' not found.");

        var normalizedName = name.Trim();
        var normalizedSlug = slug.Trim();
        var normalizedDescription = description.Trim();

        await brandRules.EnsureSlugIsUniqueAsync(normalizedSlug, id, cancellationToken);

        brand.Name = normalizedName;
        brand.Slug = normalizedSlug;
        brand.Description = normalizedDescription;

        var integrationEvent = new BrandUpdatedV1(brand.Id, brand.Name, brand.Slug, CreateMetadata(correlationId));
        await eventPublisher.PublishAndFlushAsync(integrationEvent, cancellationToken);

        return mapper.Map(brand);
    }

    public async Task DeleteAsync(Guid id, string correlationId, CancellationToken cancellationToken)
    {
        var brand = await brandRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundAppException($"Brand '{id}' not found.");

        await brandRules.EnsureCanDeleteAsync(id, cancellationToken);

        brandRepository.Remove(brand);

        var integrationEvent = new BrandDeletedV1(id, CreateMetadata(correlationId));
        await eventPublisher.PublishAndFlushAsync(integrationEvent, cancellationToken);
    }

    private static IntegrationEventMetadata CreateMetadata(string correlationId)
    {
        return new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, correlationId, "Catalog");
    }
}
