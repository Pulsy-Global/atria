using Atria.Feed.Runtime.Engine.Models;
using System.Collections.Concurrent;

namespace Atria.Feed.Runtime.Engine;

public class FeedRuntimeRegistry
{
    private readonly ConcurrentDictionary<string, FeedRuntimeContext> _feeds = new();

    public void AddOrUpdate(FeedRuntimeContext feed)
        => _feeds[feed.FeedRuntime.Id] = feed;

    public bool Remove(string id)
        => _feeds.TryRemove(id, out _);

    public FeedRuntimeContext? Get(string id)
        => _feeds.GetValueOrDefault(id);

    public bool Exists(string id)
        => _feeds.ContainsKey(id);

    public ICollection<FeedRuntimeContext> GetAll()
        => _feeds.Values;
}
