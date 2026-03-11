using Account.Application.Inputs;
using Account.Application.Models;

namespace Account.Application.Abstractions.Services;

public interface IAccountAuthService
{
    Task<AuthTokenResult> RegisterCustomerAsync(RegisterCustomerInput request, CancellationToken cancellationToken);
    Task<AuthTokenResult> LoginAsync(string realm, LoginInput request, CancellationToken cancellationToken);
    Task<AuthTokenResult> RefreshAsync(string realm, string refreshToken, CancellationToken cancellationToken);
    Task LogoutAsync(string realm, string refreshToken, CancellationToken cancellationToken);
    Task<(bool Issued, string? PreviewCode)> CreateEmailVerificationCodeByEmailAsync(string email, CancellationToken cancellationToken);
    Task VerifyEmailAsync(VerifyEmailInput request, CancellationToken cancellationToken);
    Task<(bool Issued, string? PreviewCode)> CreatePasswordResetCodeAsync(ForgotPasswordInput request, CancellationToken cancellationToken);
    Task ResetPasswordAsync(ResetPasswordInput request, CancellationToken cancellationToken);
}
