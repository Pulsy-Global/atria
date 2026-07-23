namespace Atria.Core.Business.Services.Metrics;

public static class FeedMetricsCatalog
{
    public const string RuntimeQueryGroup = "runtime";
    public const string DeliveryAttemptsQueryGroup = "delivery_attempts";
    public const string DeliveryBytesQueryGroup = "delivery_bytes";
    public const string DeliveryExhaustedQueryGroup = "delivery_exhausted";

    public const string BlocksProcessed = "atria_business_feed_blocks_processed_total";
    public const string OutputsProduced = "atria_business_feed_outputs_produced_total";
    public const string InputBytes = "atria_business_feed_input_bytes_total";
    public const string OutputBytes = "atria_business_feed_output_bytes_total";
    public const string ProcessingFailures = "atria_business_feed_processing_failures_total";
    public const string DeliveryAttempts = "atria_business_feed_delivery_attempts_total";
    public const string DeliveryBytes = "atria_business_feed_delivery_bytes_total";
    public const string DeliveryExhausted = "atria_business_feed_delivery_exhausted_total";

    public const string ProcessedBlocksKey = "processed_blocks";
    public const string ProducedOutputsKey = "produced_outputs";
    public const string ProcessingFailuresKey = "processing_failures";
    public const string DeliveryAttemptsKey = "delivery_attempts";
    public const string SuccessfulDeliveriesKey = "successful_deliveries";
    public const string FailedDeliveriesKey = "failed_deliveries";
    public const string ProcessedBytesKey = "processed_bytes";
    public const string ProducedBytesKey = "produced_bytes";
    public const string DeliveredBytesKey = "delivered_bytes";
}
