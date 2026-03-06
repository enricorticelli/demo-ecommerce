using System.Text.Json;
using MongoDB.Bson;
using Shared.BuildingBlocks.Contracts;
using Shared.BuildingBlocks.ReadModels;

namespace Cart.Infrastructure.Persistence.ReadModels;

public sealed class MongoCartReadModelStore
    : MongoGuidReadModelStoreBase<CartReadModelRow>, ICartReadModelStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public MongoCartReadModelStore()
        : base("CART_READ_DB", "cartread", "cart_read_models")
    {
    }

    protected override string GetDocumentId(CartReadModelRow model)
    {
        return model.CartId.ToString("D");
    }

    protected override CartReadModelRow MapToReadModel(Guid id, BsonDocument document)
    {
        var itemsJson = document["itemsJson"].AsString;
        var items = JsonSerializer.Deserialize<List<OrderItemDto>>(itemsJson, SerializerOptions) ?? [];
        return new CartReadModelRow(
            id,
            Guid.Parse(document["userId"].AsString),
            items,
            document["totalAmount"].ToDecimal());
    }

    protected override BsonDocument MapToDocument(CartReadModelRow model)
    {
        return new BsonDocument
        {
            ["userId"] = model.UserId.ToString("D"),
            ["itemsJson"] = JsonSerializer.Serialize(model.Items, SerializerOptions),
            ["totalAmount"] = model.TotalAmount
        };
    }
}
