using Account.Api.Contracts.Requests;
using Account.Api.Mappers;
using Account.Application.Abstractions.Services;
using Account.Application.Inputs;
using Account.Application.Models;
using Shared.BuildingBlocks.Api.Errors;

namespace Account.Api.Endpoints;

public static class AdminUserEndpoints
{
    public static RouteGroupBuilder MapAdminUserEndpoints(this RouteGroupBuilder adminGroup)
    {
        adminGroup.MapPost("/users/login", AdminLogin)
            .WithName("AdminAccountLogin");
        
        adminGroup.MapPost("/users/refresh", AdminRefresh)
            .WithName("AdminAccountRefresh");
        
        adminGroup.MapPost("/users/logout", AdminLogout)
            .WithName("AdminAccountLogout");
        
        return adminGroup;
    }

    private static Task<IResult> AdminLogin(LoginRequest request, IAccountAuthService service, CancellationToken cancellationToken)
        => LoginByRealm(request.ToInput(), AccountRealm.Admin, service, cancellationToken);

    private static Task<IResult> AdminRefresh(RefreshTokenRequest request, IAccountAuthService service, CancellationToken cancellationToken)
        => RefreshByRealm(request, AccountRealm.Admin, service, cancellationToken);

    private static Task<IResult> AdminLogout(LogoutRequest request, IAccountAuthService service, CancellationToken cancellationToken)
        => LogoutByRealm(request, AccountRealm.Admin, service, cancellationToken);

    private static async Task<IResult> LoginByRealm(LoginInput input, string realm, IAccountAuthService service, CancellationToken cancellationToken)
    {
        try
        {
            var auth = await service.LoginAsync(realm, input, cancellationToken);
            return Results.Ok(auth.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> RefreshByRealm(RefreshTokenRequest request, string realm, IAccountAuthService service, CancellationToken cancellationToken)
    {
        try
        {
            var auth = await service.RefreshAsync(realm, request.RefreshToken, cancellationToken);
            return Results.Ok(auth.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> LogoutByRealm(LogoutRequest request, string realm, IAccountAuthService service, CancellationToken cancellationToken)
    {
        try
        {
            await service.LogoutAsync(realm, request.RefreshToken, cancellationToken);
            return Results.NoContent();
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }
}
