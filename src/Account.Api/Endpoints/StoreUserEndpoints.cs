using Account.Api.Contracts.Requests;
using Account.Api.Contracts.Responses;
using Account.Api.Mappers;
using Account.Application.Abstractions.Services;
using Account.Application.Inputs;
using Account.Application.Models;
using Shared.BuildingBlocks.Api.Errors;
using Account.Api.Contracts;

namespace Account.Api.Endpoints;

public static class StoreUserEndpoints
{
    public static RouteGroupBuilder MapStoreUserEndpoints(this RouteGroupBuilder storeGroup)
    {
        storeGroup.MapPost("/users/register", StoreRegister)
            .WithName("StoreAccountRegister");
        
        storeGroup.MapPost("/users/login", StoreLogin)
            .WithName("StoreAccountLogin");
        
        storeGroup.MapPost("/users/refresh", StoreRefresh)
            .WithName("StoreAccountRefresh");
        
        storeGroup.MapPost("/users/logout", StoreLogout)
            .WithName("StoreAccountLogout");
        
        storeGroup.MapPost("/users/verify-email", StoreVerifyEmail)
            .WithName("StoreAccountVerifyEmail");
        
        storeGroup.MapPost("/users/forgot-password", StoreForgotPassword)
            .WithName("StoreAccountForgotPassword");
        
        storeGroup.MapPost("/users/reset-password", StoreResetPassword)
            .WithName("StoreAccountResetPassword");
        
        storeGroup.MapPost("/users/resend-verification", StoreResendVerification)
            .WithName("StoreAccountResendVerification");

        return storeGroup;
    }

    private static async Task<IResult> StoreRegister(RegisterCustomerRequest request, IAccountAuthService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var auth = await service.RegisterCustomerAsync(request.ToInput(), cancellationToken);
            return Results.Created($"{AccountRoutes.StoreBase}/me", auth.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static Task<IResult> StoreLogin(LoginRequest request, IAccountAuthService service,
        CancellationToken cancellationToken)
        => LoginByRealm(request.ToInput(), AccountRealm.Customer, service, cancellationToken);

    private static Task<IResult> StoreRefresh(RefreshTokenRequest request, IAccountAuthService service,
        CancellationToken cancellationToken)
        => RefreshByRealm(request, AccountRealm.Customer, service, cancellationToken);

    private static Task<IResult> StoreLogout(LogoutRequest request, IAccountAuthService service,
        CancellationToken cancellationToken)
        => LogoutByRealm(request, AccountRealm.Customer, service, cancellationToken);

    private static async Task<IResult> StoreResendVerification(ForgotPasswordRequest request, IAccountAuthService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.CreateEmailVerificationCodeByEmailAsync(request.Email, cancellationToken);
            return Results.Ok(new VerificationCodeIssuedResponse(result.Issued, result.PreviewCode));
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> StoreVerifyEmail(VerifyEmailRequest request, IAccountAuthService service,
        CancellationToken cancellationToken)
    {
        try
        {
            await service.VerifyEmailAsync(request.ToInput(), cancellationToken);
            return Results.Ok();
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> StoreForgotPassword(ForgotPasswordRequest request, IAccountAuthService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.CreatePasswordResetCodeAsync(request.ToInput(), cancellationToken);
            return Results.Ok(new VerificationCodeIssuedResponse(result.Issued, result.PreviewCode));
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> StoreResetPassword(ResetPasswordRequest request, IAccountAuthService service,
        CancellationToken cancellationToken)
    {
        try
        {
            await service.ResetPasswordAsync(request.ToInput(), cancellationToken);
            return Results.Ok();
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> LoginByRealm(LoginInput input, string realm, IAccountAuthService service,
        CancellationToken cancellationToken)
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

    private static async Task<IResult> RefreshByRealm(RefreshTokenRequest request, string realm, IAccountAuthService service,
        CancellationToken cancellationToken)
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

    private static async Task<IResult> LogoutByRealm(LogoutRequest request, string realm, IAccountAuthService service,
        CancellationToken cancellationToken)
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
