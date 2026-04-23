using Atria.Core.Data.Entities.Feeds;
using Atria.Core.Data.Entities.Outputs;
using Microsoft.EntityFrameworkCore;

namespace Atria.Core.Data.Extensions;

public static class SearchExtensions
{
    public static IQueryable<Feed> ApplyFeedSearch(
        this IQueryable<Feed> query,
        string searchQuery)
    {
        return query.ApplyTrigramSearch(searchQuery);
    }

    public static IQueryable<Output> ApplyOutputSearch(
        this IQueryable<Output> query,
        string searchQuery)
    {
        return query.ApplyTrigramSearch(searchQuery);
    }

    public static IQueryable<T> ApplyTrigramSearch<T>(
        this IQueryable<T> query,
        string searchQuery)
        where T : class
    {
        var trimmedQuery = searchQuery.Trim().ToLowerInvariant();
        var searchWords = trimmedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in searchWords)
        {
            query = query.Where(entity =>
                EF.Functions.TrigramsAreWordSimilar(word, EF.Property<string>(entity, "SearchContent")));
        }

        return query.OrderByDescending(entity =>
            EF.Functions.TrigramsSimilarity(EF.Property<string>(entity, "SearchContent"), trimmedQuery));
    }
}
