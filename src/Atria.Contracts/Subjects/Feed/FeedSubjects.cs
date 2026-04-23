namespace Atria.Contracts.Subjects.Feed;

public static class FeedSubjects
{
    public static class System
    {
        public const string DeployRequest = "feed.deploy.req";
        public const string PauseRequest = "feed.pause.req";
        public const string DeleteRequest = "feed.delete.req";
        public const string TestRequest = "feed.test.req";

        public const string FeedPaused = "feed.paused";
        public const string FeedDeployed = "feed.deployed";

        public const string DeliveryConfigRequest = "feed.delivery.config.req";
        public const string DeliveryConfigUpdated = "feed.delivery.output.updated";
        public const string DeliverTestOutput = "feed.delivery.test.req";

        public static string FeedOutput(string jsPrefix, string feedId) => $"{jsPrefix}.feed.result.{feedId}";

        public static string TestData(string blockchainId) => $"test.request.{blockchainId}";
    }
}
