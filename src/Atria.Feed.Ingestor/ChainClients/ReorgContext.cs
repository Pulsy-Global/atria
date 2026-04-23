namespace Atria.Feed.Ingestor.ChainClients;

public static class ReorgContext
{
    private static readonly AsyncLocal<bool> IsReorgFetch = new();

    public static bool IsActive => IsReorgFetch.Value;

    public static IDisposable Activate()
    {
        if (IsReorgFetch.Value)
        {
            return NullScope.Instance;
        }

        IsReorgFetch.Value = true;
        return new ReorgScope();
    }

    private sealed class ReorgScope : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            IsReorgFetch.Value = false;
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
