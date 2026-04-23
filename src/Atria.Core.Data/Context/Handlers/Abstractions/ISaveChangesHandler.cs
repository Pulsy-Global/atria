using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Atria.Core.Data.Context.Handlers.Abstractions;

public interface ISaveChangesHandler
{
    Task HandleAsync(ChangeTracker changeTracker);
}
