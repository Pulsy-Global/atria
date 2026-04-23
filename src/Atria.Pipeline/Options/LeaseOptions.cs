namespace Atria.Pipeline.Options;

public class LeaseOptions
{
    public const string SectionName = "Lease";

    public string BucketName { get; set; } = "service-leases";

    public int RenewalIntervalSeconds { get; set; } = 10;
}
