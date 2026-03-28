namespace Communication.Domain.Entities;

public sealed record CommunicationEmailMessage(string Recipient, string Subject, string Body)
{
    public static CommunicationEmailMessage ForOrderCompleted(Guid orderId, decimal totalAmount, string customerEmail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerEmail);

        return new CommunicationEmailMessage(
            customerEmail,
            $"Conferma ordine {orderId}",
            $"Il tuo ordine {orderId} e' stato confermato. Totale: {totalAmount:0.00}.");
    }
}
