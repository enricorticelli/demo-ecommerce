using System.Text.Json;
using MongoDB.Bson;
using Shared.BuildingBlocks.Contracts;
using Shared.BuildingBlocks.ReadModels;

namespace Order.Infrastructure.Persistence.ReadModels;

public sealed class MongoOrderReadModelStore
    : MongoGuidReadModelStoreBase<OrderReadModelRow>, IOrderReadModelStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public MongoOrderReadModelStore()
        : base("ORDER_READ_DB", "orderread", "order_read_models")
    {
    }

    protected override string GetDocumentId(OrderReadModelRow model)
    {
        return model.OrderId.ToString("D");
    }

    protected override OrderReadModelRow MapToReadModel(Guid id, BsonDocument document)
    {
        var itemsJson = document["itemsJson"].AsString;
        var items = JsonSerializer.Deserialize<List<OrderItemDto>>(itemsJson, SerializerOptions) ?? [];

        return new OrderReadModelRow(
            id,
            Guid.Parse(document["cartId"].AsString),
            Guid.Parse(document["userId"].AsString),
            document["status"].AsString,
            document["totalAmount"].ToDecimal(),
            items,
            document["transactionId"].AsString,
            document["trackingCode"].AsString,
            document["failureReason"].AsString);
    }

    protected override BsonDocument MapToDocument(OrderReadModelRow model)
    {
        return new BsonDocument
        {
            ["cartId"] = model.CartId.ToString("D"),
            ["userId"] = model.UserId.ToString("D"),
            ["status"] = model.Status,
            ["totalAmount"] = model.TotalAmount,
            ["itemsJson"] = JsonSerializer.Serialize(model.Items, SerializerOptions),
            ["transactionId"] = model.TransactionId,
            ["trackingCode"] = model.TrackingCode,
            ["failureReason"] = model.FailureReason
        };
    }
}
