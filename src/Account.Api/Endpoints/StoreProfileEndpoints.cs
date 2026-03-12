using Account.Api.Contracts.Requests;
using Account.Api.Mappers;
using Account.Application.Abstractions.Services;
using Shared.BuildingBlocks.Api.Errors;
using Account.Api.Contracts;

namespace Account.Api.Endpoints;

public static class StoreProfileEndpoints
{
    public static RouteGroupBuilder MapStoreProfileEndpoints(this RouteGroupBuilder storeGroup)
    {
        storeGroup.MapGet("/me", StoreGetMe)
            .RequireAuthorization("CustomerPolicy")
            .WithName("StoreAccountGetMe");
        
        storeGroup.MapPut("/me", StoreUpdateMe)
            .RequireAuthorization("CustomerPolicy")
            .WithName("StoreAccountUpdateMe");
        
        storeGroup.MapGet("/me/addresses", StoreListAddresses)
            .RequireAuthorization("CustomerPolicy")
            .WithName("StoreAccountListAddresses");
        
        storeGroup.MapPost("/me/addresses", StoreCreateAddress)
            .RequireAuthorization("CustomerPolicy")
            .WithName("StoreAccountCreateAddress");
        
        storeGroup.MapPut("/me/addresses/{addressId:guid}", StoreUpdateAddress)
            .RequireAuthorization("CustomerPolicy")
            .WithName("StoreAccountUpdateAddress");
        
        storeGroup.MapDelete("/me/addresses/{addressId:guid}", StoreDeleteAddress)
            .RequireAuthorization("CustomerPolicy")
            .WithName("StoreAccountDeleteAddress");
        
        storeGroup.MapGet("/me/orders", StoreMyOrders)
            .RequireAuthorization("CustomerPolicy")
            .WithName("StoreAccountMyOrders");

        return storeGroup;
    }

    private static async Task<IResult> StoreGetMe(HttpContext context, IAccountCustomerProfileService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = context.GetRequiredUserId();
            var user = await service.GetProfileAsync(userId, cancellationToken);
            return Results.Ok(user.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> StoreUpdateMe(HttpContext context, UpdateProfileRequest request,
        IAccountCustomerProfileService service, CancellationToken cancellationToken)
    {
        try
        {
            var userId = context.GetRequiredUserId();
            var user = await service.UpdateProfileAsync(userId, request.ToInput(), cancellationToken);
            return Results.Ok(user.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> StoreListAddresses(HttpContext context, IAccountCustomerProfileService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = context.GetRequiredUserId();
            var addresses = await service.ListAddressesAsync(userId, cancellationToken);
            return Results.Ok(addresses.Select(x => x.ToResponse()).ToArray());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> StoreCreateAddress(HttpContext context, UpsertAddressRequest request,
        IAccountCustomerProfileService service, CancellationToken cancellationToken)
    {
        try
        {
            var userId = context.GetRequiredUserId();
            var address = await service.CreateAddressAsync(userId, request.ToInput(), cancellationToken);
            return Results.Created($"{AccountRoutes.StoreBase}/me/addresses/{address.Id}", address.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> StoreUpdateAddress(HttpContext context, Guid addressId,
        UpsertAddressRequest request, IAccountCustomerProfileService service, CancellationToken cancellationToken)
    {
        try
        {
            var userId = context.GetRequiredUserId();
            var address = await service.UpdateAddressAsync(userId, addressId, request.ToInput(), cancellationToken);
            return Results.Ok(address.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> StoreDeleteAddress(HttpContext context, Guid addressId, IAccountCustomerProfileService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = context.GetRequiredUserId();
            await service.DeleteAddressAsync(userId, addressId, cancellationToken);
            return Results.NoContent();
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> StoreMyOrders(HttpContext context, IAccountCustomerProfileService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = context.GetRequiredUserId();
            var accessToken = Shared.BuildingBlocks.Api.HttpContextAuthExtensions.GetRequiredBearerToken(context);
            var orders = await service.ListMyOrdersAsync(userId, accessToken, cancellationToken);
            return Results.Ok(orders.Select(x => x.ToResponse()).ToArray());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }
}
