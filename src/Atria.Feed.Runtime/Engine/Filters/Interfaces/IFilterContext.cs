namespace Atria.Feed.Runtime.Engine.Filters.Interfaces;

public interface IFilterContext : IAsyncDisposable
{
    Task<object?> ExecuteAsync(string function, object? input, CancellationToken ct = default);
}
