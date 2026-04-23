using System.ComponentModel;
using System.Globalization;

namespace Atria.Common.Web.OData.Converters;

/// <summary>
/// This converter is a hack to tell asp.net core that this type can be
/// converted from string.
/// But beside that check to see if the conversion is possible,
/// the converter will never be used because we use a model binder.
/// In the future there should be a better way to do this:
/// https://github.com/aspnet/Mvc/issues/5850.
/// </summary>
public class ODataFromStringConverter : TypeConverter
{
    public override bool CanConvertFrom(
        ITypeDescriptorContext? context,
        Type sourceType) => sourceType == typeof(string);

    public override object ConvertFrom(
        ITypeDescriptorContext? context,
        CultureInfo? culture,
        object value) => throw new NotImplementedException();
}
