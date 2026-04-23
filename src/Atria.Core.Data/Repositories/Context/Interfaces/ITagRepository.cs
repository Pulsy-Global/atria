using Atria.Core.Data.Entities.Tags;

namespace Atria.Core.Data.Repositories.Context.Interfaces;

public interface ITagRepository : IRepository<Guid, Tag>
{
    Task<List<Tag>> GetTagsByTypeAsync(string type, CancellationToken ct);
}
