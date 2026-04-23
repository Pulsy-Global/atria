using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System.Globalization;

namespace Atria.Common.Web.OData.Resolvers;

public class ODataEnumResolver : ODataUriResolver
{
    public override void PromoteBinaryOperandTypes(
           BinaryOperatorKind binaryOperatorKind,
           ref SingleValueNode leftNode,
           ref SingleValueNode rightNode,
           out IEdmTypeReference? typeReference)
    {
        typeReference = null;

        if (leftNode.TypeReference != null &&
            rightNode.TypeReference != null)
        {
            if (leftNode.TypeReference.IsEnum() &&
                rightNode.TypeReference.IsInt32() &&
                rightNode is ConstantNode)
            {
                int intValue = (int)((ConstantNode)rightNode).Value;

                ODataEnumValue? val;
                IEdmTypeReference typeRef = leftNode.TypeReference;

                if (TryParseEnum(
                        (IEdmEnumType)typeRef.Definition,
                        intValue,
                        out val))
                {
                    rightNode = new ConstantNode(
                        val,
                        intValue.ToString(),
                        typeRef);
                    return;
                }
            }
            else if (rightNode.TypeReference.IsEnum() &&
                leftNode.TypeReference.IsInt32() &&
                leftNode is ConstantNode)
            {
                int intValue = (int)((ConstantNode)leftNode).Value;

                ODataEnumValue? val;
                IEdmTypeReference typeRef = rightNode.TypeReference;

                if (TryParseEnum(
                    (IEdmEnumType)typeRef.Definition,
                    intValue,
                    out val))
                {
                    leftNode = new ConstantNode(
                        val,
                        intValue.ToString(),
                        typeRef);
                    return;
                }
            }
        }

        base.PromoteBinaryOperandTypes(
            binaryOperatorKind,
            ref leftNode,
            ref rightNode,
            out typeReference);
    }

    public override IDictionary<IEdmOperationParameter, SingleValueNode> ResolveOperationParameters(
        IEdmOperation operation,
        IDictionary<string, SingleValueNode> input)
    {
        var result = new Dictionary<IEdmOperationParameter, SingleValueNode>(
            EqualityComparer<IEdmOperationParameter>.Default);

        foreach (var item in input)
        {
            var functionParameter = operation.FindParameter(item.Key);

            if (functionParameter == null)
            {
                throw new ODataException();
            }

            SingleValueNode newVal = item.Value;

            if (functionParameter.Type.IsEnum()
                && newVal is ConstantNode
                && newVal.TypeReference != null
                && newVal.TypeReference.IsString())
            {
                int intValue = (int)((ConstantNode)item.Value).Value;

                ODataEnumValue? val;
                IEdmTypeReference typeRef = functionParameter.Type;

                if (TryParseEnum(
                    (IEdmEnumType)typeRef.Definition,
                    intValue,
                    out val))
                {
                    newVal = new ConstantNode(
                        val,
                        intValue.ToString(),
                        typeRef);
                }
            }

            result.Add(functionParameter, newVal);
        }

        return result;
    }

    public override IEnumerable<KeyValuePair<string, object>> ResolveKeys(
        IEdmEntityType type,
        IList<string> positionalValues,
        Func<IEdmTypeReference, string, object> convertFunc)
    {
        return base.ResolveKeys(
            type,
            positionalValues,
            (typeRef, valueText) =>
            {
                if (typeRef.IsEnum() &&
                    valueText.StartsWith("'", StringComparison.Ordinal) &&
                    valueText.EndsWith("'", StringComparison.Ordinal))
                {
                    valueText = typeRef.FullName() + valueText;
                }

                return convertFunc(typeRef, valueText);
            });
    }

    public override IEnumerable<KeyValuePair<string, object>> ResolveKeys(
        IEdmEntityType type,
        IDictionary<string, string> namedValues,
        Func<IEdmTypeReference, string, object> convertFunc)
    {
        return base.ResolveKeys(
            type,
            namedValues,
            (typeRef, valueText) =>
            {
                if (typeRef.IsEnum() &&
                    valueText.StartsWith("'", StringComparison.Ordinal) &&
                    valueText.EndsWith("'", StringComparison.Ordinal))
                {
                    valueText = typeRef.FullName() + valueText;
                }

                return convertFunc(typeRef, valueText);
            });
    }

    private static bool TryParseEnum(
        IEdmEnumType enumType,
        int value,
        out ODataEnumValue? enumValue)
    {
        long parsedValue;
        enumValue = null;

        bool success = enumType.TryParseEnum(
            value.ToString(),
            true,
            out parsedValue);

        if (success)
        {
            enumValue = new ODataEnumValue(
                parsedValue.ToString(CultureInfo.InvariantCulture),
                enumType.FullTypeName());
        }

        return success;
    }
}
