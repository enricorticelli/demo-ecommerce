namespace Order.Domain;

public enum OrderStatus
{
    Pending = 0,
    StockReserved = 1,
    PaymentAuthorized = 2,
    Completed = 3,
    Failed = 4
}
