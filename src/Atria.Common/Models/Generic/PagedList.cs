namespace Atria.Common.Models.Generic;

public class PagedList<T>
{
    public IEnumerable<T> Items { get; set; }

    public int TotalCount { get; set; }
}
