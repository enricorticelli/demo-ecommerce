using Account.Api.Contracts.Requests;
using Account.Application.Inputs;

namespace Account.Api.Mappers;

public static class AccountRequestMapper
{
    public static RegisterCustomerInput ToInput(this RegisterCustomerRequest request)
        => new(request.Email, request.Password, request.FirstName, request.LastName, request.Phone);

    public static LoginInput ToInput(this LoginRequest request)
        => new(request.Username, request.Password);

    public static VerifyEmailInput ToInput(this VerifyEmailRequest request)
        => new(request.Email, request.Code);

    public static ForgotPasswordInput ToInput(this ForgotPasswordRequest request)
        => new(request.Email);

    public static ResetPasswordInput ToInput(this ResetPasswordRequest request)
        => new(request.Email, request.Code, request.NewPassword);

    public static UpdateProfileInput ToInput(this UpdateProfileRequest request)
        => new(request.FirstName, request.LastName, request.Phone);

    public static UpsertAddressInput ToInput(this UpsertAddressRequest request)
        => new(
            request.Label,
            request.Street,
            request.City,
            request.PostalCode,
            request.Country,
            request.IsDefaultShipping,
            request.IsDefaultBilling);

    public static CreateAdminInput ToInput(this AdminCreateAdminUserRequest request)
        => new(request.Username, request.Password);
}
