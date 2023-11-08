using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace MessageProcessing;

public class ServerStatisticsRepository : IServerStatisticsRepository
{
    private readonly IMongoCollection<ServerStatistics> _collection;
    private const string ServerStatisticsCollection = "ServerStatistics"; // TODO

    public ServerStatisticsRepository(IOptions<MongoDBConfig> options)
    {
        var mongoDbConfig = options.Value;
        var connectionString = mongoDbConfig.ConnectionString;
        var databaseName = mongoDbConfig.Database;
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);

        var conventionPack = new ConventionPack { new IgnoreExtraElementsConvention(true) };
        ConventionRegistry.Register("IgnoreExtraElements", conventionPack, _ => true);

        _collection = database.GetCollection<ServerStatistics>(ServerStatisticsCollection);

        CreateIndexes();
    }

    private void CreateIndexes()
    {
        var indexKeysDefinition = Builders<ServerStatistics>.IndexKeys
            .Ascending(serverStatistics => serverStatistics.ServerIdentifier)
            .Descending(serverStatistics => serverStatistics.Timestamp);
        var indexModel = new CreateIndexModel<ServerStatistics>(indexKeysDefinition, new CreateIndexOptions
        {
            Name = "ServerIdentifier_1_Timestamp_-1"
        });
        _collection.Indexes.CreateOne(indexModel);
    }

    public async Task AddServerStatistics(ServerStatistics serverStatistics)
    {
        await _collection.InsertOneAsync(serverStatistics);
    }

    public async Task<ServerStatistics> GetMostRecentServerStatistic(string serverIdentifier)
    {
        var filter =
            Builders<ServerStatistics>.Filter.Eq(serverStatistics => serverStatistics.ServerIdentifier,
                serverIdentifier);

        var mostRecentStatistics = await _collection.Find(filter)
            .SortByDescending(serverStatistics => serverStatistics.Timestamp)
            .Limit(1)
            .FirstOrDefaultAsync();

        return mostRecentStatistics;
    }
}