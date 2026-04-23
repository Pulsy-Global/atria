using System.ComponentModel.DataAnnotations;

namespace Atria.Core.Data.Entities.Context;

public class BaseEntity<T>
{
    [Key]
    public T Id { get; set; }
}
