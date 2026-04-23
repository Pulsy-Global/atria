using Microsoft.EntityFrameworkCore;

namespace Atria.Core.Data.Extensions;

public static class ContextExtensions
{
    public static bool IsConflict(this DbUpdateException ex)
    {
        if (ex.InnerException == null)
        {
            return false;
        }

        return true;
    }
}
