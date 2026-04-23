namespace Atria.Common.Net;

public class SsrfGuardOptions
{
    public const string SectionName = "SsrfGuard";

    public bool Enabled { get; set; } = true;

    public int ConnectTimeoutSeconds { get; set; } = 10;
}
