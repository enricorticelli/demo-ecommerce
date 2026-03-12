using System.Net.Http.Json;
using Account.Application.Models;
using System.Net.Http.Headers;

namespace Account.Infrastructure.Services;

public sealed class OrderApiClient(HttpClient httpClient)
{
    public async Task ClaimGuestOrdersAsync(string accessToken, string customerEmail, CancellationToken cancellationToken)
    {
        var payload = new
        {
            customerEmail
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/store/v1/orders/claim-guest")
        {
            Content = JsonContent.Create(payload)
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        await httpClient.SendAsync(request, cancellationToken);
    }

    public async Task ClaimGuestOrdersInternalAsync(Guid authenticatedUserId, string customerEmail, string internalApiKey, CancellationToken cancellationToken)
    {
        var payload = new
        {
            authenticatedUserId,
            customerEmail
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/internal/v1/orders/claim-guest")
        {
            Content = JsonContent.Create(payload)
        };

        request.Headers.Add("X-Internal-Api-Key", internalApiKey);
        await httpClient.SendAsync(request, cancellationToken);
    }

    public async Task<IReadOnlyList<OrderSummary>> ListByAuthenticatedUserAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/store/v1/orders?limit=200&offset=0");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var payload = await response.Content.ReadFromJsonAsync<OrderSummaryRemote[]>(cancellationToken: cancellationToken);
        if (payload is null)
        {
            return [];
        }

        return payload.Select(x => new OrderSummary(
            x.Id,
            x.Status,
            x.TotalAmount,
            x.CreatedAtUtc,
            x.TrackingCode,
            x.TransactionId,
            x.FailureReason)).ToArray();
    }

    private sealed record OrderSummaryRemote(
        Guid Id,
        string Status,
        decimal TotalAmount,
        DateTimeOffset CreatedAtUtc,
        string? TrackingCode,
        string? TransactionId,
        string? FailureReason);
}
