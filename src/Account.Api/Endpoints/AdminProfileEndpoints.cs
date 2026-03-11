using Account.Api.Mappers;
using Account.Application.Abstractions.Services;
using Shared.BuildingBlocks.Api.Errors;

namespace Account.Api.Endpoints;

public static class AdminProfileEndpoints
{
    public static RouteGroupBuilder MapAdminProfileEndpoints(this RouteGroupBuilder adminGroup)
    {
        adminGroup.MapGet("/me", AdminGetMe)
            .RequireAuthorization("AdminPolicy")
            .WithName("AdminAccountGetMe");
        
        adminGroup.MapGet("/me/permissions", AdminGetPermissions)
            .RequireAuthorization("AdminPolicy")
            .WithName("AdminAccountGetPermissions");
        
        return adminGroup;
    }

    private static async Task<IResult> AdminGetMe(HttpContext context, IAccountAdministrationService service, CancellationToken cancellationToken)
    {
        try
        {
            var userId = context.GetRequiredUserId();
            var user = await service.GetAdminAsync(userId, cancellationToken);
            return Results.Ok(user.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static IResult AdminGetPermissions(HttpContext context)
    {
        var realm = context.GetRequiredRealm();
        var permissions = context.User.Claims
            .Where(x => x.Type == "permission")
            .Select(x => x.Value)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        return Results.Ok(realm.ToPermissionsResponse(permissions));
    }
}
