using Atria.Common.Exceptions;
using Atria.Core.Data.Context;
using Atria.Core.Data.Extensions;
using Atria.Core.Data.Repositories.Context.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Atria.Core.Data.UnitOfWork.Context;

public class UnitOfWork : IUnitOfWork
{
    private readonly IServiceScope? _uowScope;

    private readonly AtriaDbContext _dbContext;

    private IDbContextTransaction? _transaction;

    public IFeedRepository FeedRepository { get; set; }

    public IFeedOutputRepository FeedOutputRepository { get; set; }

    public IFeedTagRepository FeedTagRepository { get; set; }

    public IOutputRepository OutputRepository { get; set; }

    public IOutputTagRepository OutputTagRepository { get; set; }

    public IDeployRepository DeployRepository { get; set; }

    public ITagRepository TagRepository { get; set; }

    public UnitOfWork(
        AtriaDbContext dbContext,
        IFeedRepository feedRepository,
        IFeedOutputRepository feedOutputRepository,
        IFeedTagRepository feedTagRepository,
        IOutputRepository outputRepository,
        IOutputTagRepository outputTagRepository,
        IDeployRepository deployRepository,
        ITagRepository tagRepository,
        IServiceScope? uowScope = null)
    {
        _uowScope = uowScope;
        _dbContext = dbContext;

        FeedRepository = feedRepository;
        FeedOutputRepository = feedOutputRepository;
        FeedTagRepository = feedTagRepository;
        OutputRepository = outputRepository;
        OutputTagRepository = outputTagRepository;
        DeployRepository = deployRepository;
        TagRepository = tagRepository;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException(ex.Message, ex);
        }
        catch (DbUpdateException ex)
        {
            if (ex.IsConflict())
            {
                throw new ItemExistsException("Item already exists.", ex);
            }

            throw;
        }
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken)
    {
        _transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("Transaction was not created.");
        }

        await _transaction.CommitAsync(cancellationToken);
    }

    public void Dispose()
    {
        try
        {
            _transaction?.Dispose();

            if (_dbContext.Database.IsNpgsql())
            {
                _dbContext.Database.CloseConnection();
            }

            _uowScope?.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // Ignore
        }

        GC.SuppressFinalize(this);
    }
}
