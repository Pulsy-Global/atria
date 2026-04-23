using Atria.Contracts.Events.Feed.Enums;

namespace Atria.Contracts.Subjects.Blockchain;

public static class Blockchain
{
    public static class DataTypes
    {
        public const string Transactions = "transactions";
        public const string Logs = "logs";
        public const string Traces = "traces";

        public static string FromFeedDataType(FeedDataType dataType) => dataType switch
        {
            FeedDataType.Transactions => Transactions,
            FeedDataType.Logs => Logs,
            FeedDataType.Traces => Traces,
            _ => throw new ArgumentException($"Unknown data type: {dataType}")
        };
    }

    public static class Subjects
    {
        public const string BlockchainInternalPrefix = "blockchain";

        public const string ReorgDetectedAll = "blockchain.*.reorg";

        public static string ChainHeadRequest(string chainId) => $"{BlockchainInternalPrefix}.{chainId}.head.req";
        public static string ReorgDetected(string chainId) => $"{BlockchainInternalPrefix}.{chainId}.reorg";

        public static string ForType(string chain, FeedDataType dataType, string jsPrefix)
        {
            var typeStr = dataType switch
            {
                FeedDataType.Transactions => DataTypes.Transactions,
                FeedDataType.Logs => DataTypes.Logs,
                FeedDataType.Traces => DataTypes.Traces,
                _ => throw new ArgumentException($"Unknown data type: {dataType}")
            };

            return ForType(chain, typeStr, jsPrefix);
        }

        public static string ForType(string chain, string dataType, string jsPrefix)
        {
            return $"{jsPrefix}.{BlockchainInternalPrefix}.{chain}.{dataType}";
        }
    }
}
