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
        var group = adminGroup.MapGroup("/admins");

        group.MapGet("/", ListAdmins)
            .RequireAuthorization(Shared.BuildingBlocks.Api.AuthorizationPolicies.AccountAdminReadPolicy)
            .WithName("AdminAccountListAdmins");
        group.MapPost("/", CreateAdmin)
            .RequireAuthorization(Shared.BuildingBlocks.Api.AuthorizationPolicies.AccountAdminWritePolicy)
            .WithName("AdminAccountCreateAdmin");
        group.MapPost("/{adminUserId:guid}/password/reset", ResetAdminPassword)
            .RequireAuthorization(Shared.BuildingBlocks.Api.AuthorizationPolicies.AccountAdminWritePolicy)
            .WithName("AdminAccountResetAdminPassword");
        group.MapPut("/{adminUserId:guid}/permissions", SetAdminPermissions)
            .RequireAuthorization(Shared.BuildingBlocks.Api.AuthorizationPolicies.AccountAdminWritePolicy)
            .WithName("AdminAccountSetAdminPermissions");
        group.MapDelete("/{adminUserId:guid}", DeleteAdmin)
            .RequireAuthorization(Shared.BuildingBlocks.Api.AuthorizationPolicies.AccountAdminWritePolicy)
            .WithName("AdminAccountDeleteAdmin");

        return adminGroup;
    }

    private static async Task<IResult> ListAdmins(
        HttpContext context,
        IAccountAdministrationService service,
        int? limit,
        int? offset,
        string? searchTerm,
        CancellationToken cancellationToken)
    {
        try
        {
            var actingAdminUserId = context.GetRequiredUserId();
            var (normalizedLimit, normalizedOffset) = PaginationNormalizer.Normalize(limit, offset);
            var admins = await service.ListAdminsAsync(actingAdminUserId, normalizedLimit, normalizedOffset, searchTerm, cancellationToken);
            return Results.Ok(admins.Select(x => x.ToResponse()).ToArray());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> CreateAdmin(
        HttpContext context,
        AdminCreateAdminUserRequest request,
        IAccountAdministrationService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var actingAdminUserId = context.GetRequiredUserId();
            var created = await service.CreateAdminByAdminAsync(actingAdminUserId, request.ToInput(), cancellationToken);
            return Results.Created($"{AccountRoutes.AdminBase}/admins/{created.Id}", created.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> ResetAdminPassword(
        HttpContext context,
        Guid adminUserId,
        AdminResetAdminPasswordRequest request,
        IAccountAdministrationService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var actingAdminUserId = context.GetRequiredUserId();
            await service.SetAdminPasswordByAdminAsync(actingAdminUserId, adminUserId, request.NewPassword, cancellationToken);
            return Results.NoContent();
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> SetAdminPermissions(
        HttpContext context,
        Guid adminUserId,
        AdminSetAdminPermissionsRequest request,
        IAccountAdministrationService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var actingAdminUserId = context.GetRequiredUserId();
            await service.SetAdminPermissionsByAdminAsync(actingAdminUserId, adminUserId, request.Permissions, cancellationToken);
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
        IAccountAdministrationService service,
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
