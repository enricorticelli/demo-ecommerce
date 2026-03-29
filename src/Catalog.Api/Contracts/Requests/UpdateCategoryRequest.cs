using System.ComponentModel.DataAnnotations;

namespace Catalog.Api.Contracts.Requests;

public sealed record UpdateCategoryRequest(
    [property: Required, StringLength(128)] string Name,
    [property: Required, StringLength(128)] string Slug,
    [property: StringLength(1024)] string Description);
