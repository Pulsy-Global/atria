using Atria.Contracts.Events.Feed.Enums;
using System.ComponentModel.DataAnnotations;

namespace Atria.Feed.Runtime.Engine.Functions.Options;

public class RuntimeConfigOptions
{
    [Required(ErrorMessage = "RuntimeConfig WrapperFile is required")]
    public string WrapperFile { get; set; }

    [Required(ErrorMessage = "RuntimeConfig Language is required")]
    public FunctionLangKind Language { get; set; }
}
