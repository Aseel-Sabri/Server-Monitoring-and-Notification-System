namespace MessageProcessing;

public interface IServerStatisticsRepository
{
    Task AddServerStatistics(ServerStatistics serverStatistics);
    Task<ServerStatistics> GetMostRecentServerStatistic(string serverIdentifier);
}