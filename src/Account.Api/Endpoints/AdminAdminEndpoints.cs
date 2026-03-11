using Account.Api.Contracts;
using Account.Api.Contracts.Requests;
using Account.Api.Mappers;
using Account.Application.Abstractions.Services;
using Shared.BuildingBlocks.Api.Errors;
using Shared.BuildingBlocks.Api.Pagination;

namespace Account.Api.Endpoints;

public static class AdminAdminEndpoints
{
    public static RouteGroupBuilder MapAdminAdminEndpoints(this RouteGroupBuilder adminGroup)
    {
        var group = adminGroup.MapGroup("/admins")
            .RequireAuthorization("AdminPolicy");

        group.MapGet("/", ListAdmins)
            .WithName("AdminAccountListAdmins");
        group.MapPost("/", CreateAdmin)
            .WithName("AdminAccountCreateAdmin");
        group.MapPost("/{adminUserId:guid}/password/reset", ResetAdminPassword)
            .WithName("AdminAccountResetAdminPassword");
        group.MapDelete("/{adminUserId:guid}", DeleteAdmin)
            .WithName("AdminAccountDeleteAdmin");

        return adminGroup;
    }

    private static async Task<IResult> ListAdmins(
        IAccountService service,
        int? limit,
        int? offset,
        string? searchTerm,
        CancellationToken cancellationToken)
    {
        var (normalizedLimit, normalizedOffset) = PaginationNormalizer.Normalize(limit, offset);
        var admins = await service.ListAdminsAsync(normalizedLimit, normalizedOffset, searchTerm, cancellationToken);
        return Results.Ok(admins.Select(x => x.ToResponse()).ToArray());
    }

    private static async Task<IResult> CreateAdmin(
        AdminCreateAdminUserRequest request,
        IAccountService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await service.CreateAdminByAdminAsync(request.ToInput(), cancellationToken);
            return Results.Created($"{AccountRoutes.AdminBase}/admins/{created.Id}", created.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> ResetAdminPassword(
        Guid adminUserId,
        AdminResetAdminPasswordRequest request,
        IAccountService service,
        CancellationToken cancellationToken)
    {
        try
        {
            await service.SetAdminPasswordByAdminAsync(adminUserId, request.NewPassword, cancellationToken);
            return Results.NoContent();
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> DeleteAdmin(
        HttpContext context,
        Guid adminUserId,
        IAccountService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var actingAdminUserId = context.GetRequiredUserId();
            await service.DeleteAdminByAdminAsync(actingAdminUserId, adminUserId, cancellationToken);
            return Results.NoContent();
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }
}
