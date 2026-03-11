using Account.Api.Contracts;
using Account.Api.Contracts.Requests;
using Account.Api.Mappers;
using Account.Application.Abstractions.Services;
using Shared.BuildingBlocks.Api.Errors;
using Shared.BuildingBlocks.Api.Pagination;

namespace Account.Api.Endpoints;

public static class AdminCustomerEndpoints
{
    public static RouteGroupBuilder MapAdminCustomerEndpoints(this RouteGroupBuilder adminGroup)
    {
        var customerGroup = adminGroup.MapGroup("/customers")
            .RequireAuthorization("AdminPolicy");

        customerGroup.MapGet("/", ListCustomers)
            .WithName("AdminAccountListCustomers");
        customerGroup.MapGet("/{customerId:guid}", GetCustomer)
            .WithName("AdminAccountGetCustomer");
        customerGroup.MapPut("/{customerId:guid}", UpdateCustomer)
            .WithName("AdminAccountUpdateCustomer");
        customerGroup.MapPost("/{customerId:guid}/password/reset", ResetCustomerPassword)
            .WithName("AdminAccountResetCustomerPassword");

        customerGroup.MapGet("/{customerId:guid}/addresses", ListCustomerAddresses)
            .WithName("AdminAccountListCustomerAddresses");
        customerGroup.MapPost("/{customerId:guid}/addresses", CreateCustomerAddress)
            .WithName("AdminAccountCreateCustomerAddress");
        customerGroup.MapPut("/{customerId:guid}/addresses/{addressId:guid}", UpdateCustomerAddress)
            .WithName("AdminAccountUpdateCustomerAddress");
        customerGroup.MapDelete("/{customerId:guid}/addresses/{addressId:guid}", DeleteCustomerAddress)
            .WithName("AdminAccountDeleteCustomerAddress");

        return adminGroup;
    }

    private static async Task<IResult> ListCustomers(
        IAccountAdministrationService service,
        int? limit,
        int? offset,
        string? searchTerm,
        CancellationToken cancellationToken)
    {
        var (normalizedLimit, normalizedOffset) = PaginationNormalizer.Normalize(limit, offset);
        var customers = await service.ListCustomersAsync(normalizedLimit, normalizedOffset, searchTerm, cancellationToken);
        return Results.Ok(customers.Select(x => x.ToResponse()).ToArray());
    }

    private static async Task<IResult> GetCustomer(
        Guid customerId,
        IAccountAdministrationService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var customer = await service.GetCustomerAsync(customerId, cancellationToken);
            return Results.Ok(customer.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> UpdateCustomer(
        Guid customerId,
        UpdateProfileRequest request,
        IAccountAdministrationService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var customer = await service.UpdateCustomerByAdminAsync(customerId, request.ToInput(), cancellationToken);
            return Results.Ok(customer.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> ResetCustomerPassword(
        Guid customerId,
        AdminResetCustomerPasswordRequest request,
        IAccountAdministrationService service,
        CancellationToken cancellationToken)
    {
        try
        {
            await service.SetCustomerPasswordByAdminAsync(customerId, request.NewPassword, cancellationToken);
            return Results.NoContent();
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> ListCustomerAddresses(
        Guid customerId,
        IAccountAdministrationService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var addresses = await service.ListCustomerAddressesByAdminAsync(customerId, cancellationToken);
            return Results.Ok(addresses.Select(x => x.ToResponse()).ToArray());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> CreateCustomerAddress(
        Guid customerId,
        UpsertAddressRequest request,
        IAccountAdministrationService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var address = await service.CreateCustomerAddressByAdminAsync(customerId, request.ToInput(), cancellationToken);
            return Results.Created($"{AccountRoutes.AdminBase}/customers/{customerId}/addresses/{address.Id}", address.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> UpdateCustomerAddress(
        Guid customerId,
        Guid addressId,
        UpsertAddressRequest request,
        IAccountAdministrationService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var address = await service.UpdateCustomerAddressByAdminAsync(customerId, addressId, request.ToInput(), cancellationToken);
            return Results.Ok(address.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> DeleteCustomerAddress(
        Guid customerId,
        Guid addressId,
        IAccountAdministrationService service,
        CancellationToken cancellationToken)
    {
        try
        {
            await service.DeleteCustomerAddressByAdminAsync(customerId, addressId, cancellationToken);
            return Results.NoContent();
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }
}
