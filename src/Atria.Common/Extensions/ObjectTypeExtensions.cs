using System.Numerics;

namespace Atria.Common.Extensions;

public static class ObjectTypeExtensions
{
    public static bool IsDateTimeType(this Type o)
    {
        return o == typeof(DateTime) || o == typeof(DateTimeOffset);
    }

    public static bool IsNumericType(this Type o)
    {
        if (Nullable.GetUnderlyingType(o) != null)
        {
            o = Nullable.GetUnderlyingType(o) !;
        }

        switch (Type.GetTypeCode(o))
        {
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Decimal:
            case TypeCode.Double:
            case TypeCode.Single:
                return true;
            default:
                return false;
        }
    }

    public static bool IsBigIntegerType(this Type o)
    {
        if (Nullable.GetUnderlyingType(o) != null)
        {
            o = Nullable.GetUnderlyingType(o) !;
        }

        return o == typeof(BigInteger);
    }

    public static bool IsStringType(this Type o)
    {
        return Type.GetTypeCode(o) == TypeCode.String;
    }
}
