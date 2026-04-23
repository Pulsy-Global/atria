using Atria.Core.Data.Entities.Feeds;
using Atria.Core.Data.Entities.Outputs;
using Atria.Core.Data.Entities.Tags;

namespace Atria.Core.Data.Extensions;

public static class EntityExtensions
{
    public static (List<FeedOutput> ToRemove, List<FeedOutput> ToAdd) UpdateFeedOutputs(this Feed feed, List<Guid>? uniqueOutputIds)
    {
        uniqueOutputIds = uniqueOutputIds ?? new List<Guid>();

        var existingOutputIds = feed.FeedOutputs
            .Select(fo => fo.OutputId)
            .ToHashSet();

        var toRemove = feed.FeedOutputs
            .Where(fo => !uniqueOutputIds.Contains(fo.OutputId))
            .ToList();

        foreach (var item in toRemove)
        {
            feed.FeedOutputs.Remove(item);
        }

        var toAdd = uniqueOutputIds
            .Where(id => !existingOutputIds.Contains(id))
            .Select(outputId => new FeedOutput
            {
                FeedId = feed.Id,
                OutputId = outputId,
            })
            .ToList();

        foreach (var item in toAdd)
        {
            feed.FeedOutputs.Add(item);
        }

        return (toRemove, toAdd);
    }

    public static (List<FeedTag> ToRemove, List<FeedTag> ToAdd) UpdateFeedTags(this Feed feed, List<Guid>? uniqueTagIds)
    {
        uniqueTagIds = uniqueTagIds ?? new List<Guid>();

        var existingTagIds = feed.FeedTags
            .Select(ft => ft.TagId)
            .ToHashSet();

        var toRemove = feed.FeedTags
            .Where(ft => !uniqueTagIds.Contains(ft.TagId))
            .ToList();

        foreach (var item in toRemove)
        {
            feed.FeedTags.Remove(item);
        }

        var toAdd = uniqueTagIds
            .Where(id => !existingTagIds.Contains(id))
            .Select(tagId => new FeedTag
            {
                FeedId = feed.Id,
                TagId = tagId,
            })
            .ToList();

        foreach (var item in toAdd)
        {
            feed.FeedTags.Add(item);
        }

        return (toRemove, toAdd);
    }

    public static (List<OutputTag> ToRemove, List<OutputTag> ToAdd) UpdateOutputTags(this Output output, List<Guid>? uniqueTagIds)
    {
        uniqueTagIds = uniqueTagIds ?? new List<Guid>();

        var existingTagIds = output.OutputTags
            .Select(ot => ot.TagId)
            .ToHashSet();

        var toRemove = output.OutputTags
            .Where(ot => !uniqueTagIds.Contains(ot.TagId))
            .ToList();

        foreach (var item in toRemove)
        {
            output.OutputTags.Remove(item);
        }

        var toAdd = uniqueTagIds
            .Where(id => !existingTagIds.Contains(id))
            .Select(tagId => new OutputTag
            {
                OutputId = output.Id,
                TagId = tagId,
            })
            .ToList();

        foreach (var item in toAdd)
        {
            output.OutputTags.Add(item);
        }

        return (toRemove, toAdd);
    }
}
