using Atria.Core.Data.Repositories.Context.Interfaces;

namespace Atria.Core.Data.UnitOfWork.Context;

public interface IUnitOfWork : IDisposable
{
    public IFeedRepository FeedRepository { get; set; }

    public IFeedOutputRepository FeedOutputRepository { get; set; }

    public IFeedTagRepository FeedTagRepository { get; set; }

    public IOutputRepository OutputRepository { get; set; }

    public IOutputTagRepository OutputTagRepository { get; set; }

    public IDeployRepository DeployRepository { get; set; }

    public ITagRepository TagRepository { get; set; }

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task BeginTransactionAsync(CancellationToken cancellationToken);

    Task CommitTransactionAsync(CancellationToken cancellationToken);
}
