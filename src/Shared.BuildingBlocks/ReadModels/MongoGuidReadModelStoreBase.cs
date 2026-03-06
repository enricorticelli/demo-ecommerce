using MongoDB.Bson;
using MongoDB.Driver;
using Shared.BuildingBlocks.Infrastructure;

namespace Shared.BuildingBlocks.ReadModels;

public abstract class MongoGuidReadModelStoreBase<TReadModel> : IReadModelStore<Guid, TReadModel>
    where TReadModel : class
{
    private readonly IMongoCollection<BsonDocument> _collection;

    protected MongoGuidReadModelStoreBase(string databaseEnvVarName, string defaultDatabaseName, string collectionName)
    {
        var connectionString = InfrastructureConnectionFactory.BuildMongoConnectionString();
        var databaseName = Environment.GetEnvironmentVariable(databaseEnvVarName) ?? defaultDatabaseName;

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _collection = database.GetCollection<BsonDocument>(collectionName);
    }

    public async Task<TReadModel?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var documentId = id.ToString("D");
        var filter = Builders<BsonDocument>.Filter.Eq("_id", documentId);
        var doc = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return doc is null ? null : MapToReadModel(id, doc);
    }

    public Task UpsertAsync(TReadModel model, CancellationToken cancellationToken)
    {
        var documentId = GetDocumentId(model);
        var filter = Builders<BsonDocument>.Filter.Eq("_id", documentId);
        var doc = MapToDocument(model);
        doc["_id"] = documentId;
        doc["updatedAtUtc"] = DateTime.UtcNow;
        return _collection.ReplaceOneAsync(filter, doc, new ReplaceOptions { IsUpsert = true }, cancellationToken);
    }

    protected abstract string GetDocumentId(TReadModel model);
    protected abstract TReadModel MapToReadModel(Guid id, BsonDocument document);
    protected abstract BsonDocument MapToDocument(TReadModel model);
}
