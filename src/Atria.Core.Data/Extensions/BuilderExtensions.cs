using Atria.Core.Data.Entities.Context.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Atria.Core.Data.Extensions;

public static class BuilderExtensions
{
    public static void EnableSoftDelete(this ModelBuilder builder)
    {
        var entityTypes = builder.Model.GetEntityTypes();

        var deletableEntityTypes = entityTypes
            .Where(entityType => typeof(IAuditDeleted)
                .IsAssignableFrom(entityType.ClrType))
            .ToList();

        deletableEntityTypes.ForEach(entityType =>
        {
            builder
                .Entity(entityType.ClrType)
                .Property(nameof(IAuditDeleted.DeletedAt));

            var parameter = Expression.Parameter(entityType.ClrType, "x");

            var body = Expression.Equal(
                Expression.Call(
                    typeof(EF),
                    nameof(EF.Property),
                    new[] { typeof(DateTimeOffset?) },
                    parameter,
                    Expression.Constant(nameof(IAuditDeleted.DeletedAt))),
                Expression.Constant(null));

            builder
                .Entity(entityType.ClrType)
                .HasQueryFilter(Expression.Lambda(body, parameter));
        });
    }
}
