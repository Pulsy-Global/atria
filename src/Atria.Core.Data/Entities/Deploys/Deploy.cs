using Atria.Core.Data.Entities.Context;
using Atria.Core.Data.Entities.Context.Interfaces;
using Atria.Core.Data.Entities.Enums;
using Atria.Core.Data.Entities.Feeds;
using System.ComponentModel.DataAnnotations;

namespace Atria.Core.Data.Entities.Deploys;

public class Deploy : BaseEntity<Guid>, IAuditCreated, IAuditDeleted
{
    [Required]
    public Guid FeedId { get; set; }

    [Required]
    [MaxLength(25)]
    public string Version { get; set; }

    [Required]
    public DeployStatus Status { get; set; } = DeployStatus.None;

    public DeployErrorCode? ErrorCode { get; set; }

    [MaxLength(64)]
    public string? ErrorSource { get; set; }

    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    public DateTimeOffset? ErrorOccurredAt { get; set; }

    public Feed Feed { get; set; }

    public ICollection<DeployStatusChange> StatusChanges { get; set; } = new List<DeployStatusChange>();

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public void MarkFailed(DeployErrorCode errorCode, string errorSource, string? errorMessage = null)
    {
        Status = DeployStatus.Failed;
        ErrorCode = errorCode;
        ErrorSource = errorSource;
        ErrorMessage = Truncate(errorMessage, 1000);
        ErrorOccurredAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ClearError()
    {
        ErrorCode = null;
        ErrorSource = null;
        ErrorMessage = null;
        ErrorOccurredAt = null;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Length <= maxLength
            ? value
            : value[..maxLength];
    }
}
