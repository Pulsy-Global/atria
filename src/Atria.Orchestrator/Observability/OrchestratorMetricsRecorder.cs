using Atria.Common.Observability;
using System.Diagnostics.Metrics;

namespace Atria.Orchestrator.Observability;

public sealed class OrchestratorMetricsRecorder
{
    public const string Success = "success";
    public const string Failure = "failure";

    private static readonly Histogram<double> ProvisioningScanDuration = AtriaMeters.Observability.CreateHistogram<double>(
        "atria.orchestrator.provisioning.scan.duration",
        "s");
    private static readonly Counter<long> ManifestChanges = AtriaMeters.Observability.CreateCounter<long>(
        "atria.orchestrator.manifest.changes",
        "{change}");
    private static readonly Counter<long> ProvisioningActions = AtriaMeters.Observability.CreateCounter<long>(
        "atria.orchestrator.provisioning.actions",
        "{operation}");
    private static readonly Histogram<double> ReconciliationDuration = AtriaMeters.Observability.CreateHistogram<double>(
        "atria.orchestrator.reconciliation.duration",
        "s");
    private static readonly Counter<long> ReconciliationActions = AtriaMeters.Observability.CreateCounter<long>(
        "atria.orchestrator.reconciliation.actions",
        "{operation}");
    private int _pendingDeployments;

    public OrchestratorMetricsRecorder()
    {
        AtriaMeters.Observability.CreateObservableGauge(
            "atria.orchestrator.deployments.pending",
            () => Volatile.Read(ref _pendingDeployments),
            "{item}");
    }

    public void SetPendingDeployments(int count) => Interlocked.Exchange(ref _pendingDeployments, count);

    public void RecordProvisioningScan(bool succeeded, TimeSpan duration) => ProvisioningScanDuration.Record(
        duration.TotalSeconds,
        new KeyValuePair<string, object?>("outcome", succeeded ? Success : Failure));

    public void RecordManifestChanges(int added, int modified, int removed)
    {
        RecordChange("added", added);
        RecordChange("modified", modified);
        RecordChange("removed", removed);
    }

    public void RecordProvisioningAction(string operation, bool succeeded) => ProvisioningActions.Add(
        1,
        new KeyValuePair<string, object?>("operation", operation),
        new KeyValuePair<string, object?>("outcome", succeeded ? Success : Failure));

    public void RecordReconciliation(bool succeeded, TimeSpan duration) => ReconciliationDuration.Record(
        duration.TotalSeconds,
        new KeyValuePair<string, object?>("outcome", succeeded ? Success : Failure));

    public void RecordReconciliationAction(string operation, string reason) => ReconciliationActions.Add(
        1,
        new KeyValuePair<string, object?>("operation", operation),
        new KeyValuePair<string, object?>("reason", reason));

    private static void RecordChange(string change, int count)
    {
        if (count > 0)
        {
            ManifestChanges.Add(count, new KeyValuePair<string, object?>("change", change));
        }
    }
}
