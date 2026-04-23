using Atria.Common.Web.Models.Abstractions;

namespace Atria.Core.Business.Models.Dto.Tag;

public class TagDto : IODataDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Color { get; set; } = "#000000";

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
