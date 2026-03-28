using Catalog.Application.Abstractions.Commands;
using Catalog.Application.Abstractions.Queries;
using Catalog.Application.Abstractions.Repositories;
using Catalog.Application.Abstractions.Rules;
using Catalog.Application.Mappers;
using Catalog.Application.Services.Brands;
using Catalog.Application.Services.Categories;
using Catalog.Application.Services.Collections;
using Catalog.Application.Services.Products;
using Catalog.Application.Services.Rules;
using Catalog.Application.Views;
using Catalog.Domain.Entities;
using Catalog.Infrastructure.Messaging;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Persistence.Repositories;
using Evoluzione.TracedServiceCollection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.BuildingBlocks.Contracts.Messaging;
using Shared.BuildingBlocks.Mapping;
using Wolverine.EntityFrameworkCore;

namespace Catalog.Infrastructure.Configuration;

public static class CatalogInfrastructureExtensions
{
    public static IServiceCollection AddCatalogInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = CatalogTechnicalOptions.ResolveCatalogConnectionString(configuration);

        services.AddDbContextWithWolverineIntegration<CatalogDbContext>(options => options.UseNpgsql(connectionString));
        services.AddTracedScoped<IBrandRepository, BrandRepository>();
        services.AddTracedScoped<ICategoryRepository, CategoryRepository>();
        services.AddTracedScoped<ICollectionRepository, CollectionRepository>();
        services.AddTracedScoped<IProductRepository, ProductRepository>();
        services.AddTracedScoped<IDomainEventPublisher, OutboxDomainEventPublisher>();

        services.AddScoped<IBrandRules, BrandRules>();
        services.AddScoped<ICategoryRules, CategoryRules>();
        services.AddScoped<ICollectionRules, CollectionRules>();
        services.AddScoped<IProductRules, ProductRules>();

        services.AddScoped<IViewMapper<Brand, BrandView>, BrandViewMapper>();
        services.AddScoped<IViewMapper<Category, CategoryView>, CategoryViewMapper>();
        services.AddScoped<IViewMapper<Collection, CollectionView>, CollectionViewMapper>();
        services.AddScoped<IViewMapper<Product, ProductView>, ProductViewMapper>();

        services.AddTracedScoped<IBrandCommandService, BrandCommandService>();
        services.AddTracedScoped<ICategoryCommandService, CategoryCommandService>();
        services.AddTracedScoped<ICollectionCommandService, CollectionCommandService>();
        services.AddTracedScoped<IProductCommandCatalogService, ProductCommandCatalogService>();

        services.AddTracedScoped<IBrandQueryService, BrandQueryService>();
        services.AddTracedScoped<ICategoryQueryService, CategoryQueryService>();
        services.AddTracedScoped<ICollectionQueryService, CollectionQueryService>();
        services.AddTracedScoped<IProductQueryService, ProductQueryService>();

        return services;
    }
}
