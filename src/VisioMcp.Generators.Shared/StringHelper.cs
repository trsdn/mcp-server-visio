using System.Text;
using Microsoft.CodeAnalysis;

namespace VisioMcp.Generators.Common;

/// <summary>
/// String manipulation utilities shared between generators.
/// </summary>
public static class StringHelper
{
    public static string ToKebabCase(string pascalCase)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < pascalCase.Length; i++)
        {
            var c = pascalCase[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                    sb.Append('-');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    public static string ToPascalCase(string kebabCase)
    {
        var parts = kebabCase.Split('-');
        return string.Concat(parts.Select(p =>
            p.Length > 0 ? char.ToUpperInvariant(p[0]) + p.Substring(1) : p));
    }

    /// <summary>
    /// Converts camelCase or PascalCase to snake_case.
    /// Example: "sheetName" → "sheet_name", "rangeAddress" → "range_address"
    /// </summary>
    public static string ToSnakeCase(string camelCase)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < camelCase.Length; i++)
        {
            var c = camelCase[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                    sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    public static bool IsStringType(string typeName)
    {
        var normalized = typeName.TrimEnd('?');
        return normalized == "string" || normalized == "System.String";
    }

    /// <summary>
    /// Last-resort label for a parameter with no XML <c>&lt;param&gt;</c> documentation.
    /// </summary>
    /// <remarks>
    /// This previously held a lookup table inherited from the Excel ancestor — <c>queryName</c>,
    /// <c>mCode</c>, <c>rangeAddress</c>, <c>pivotTableName</c>, <c>slicerName</c>, and the
    /// three-product artefact <c>"sheetName" =&gt; "Slide name"</c>. Of its eighteen keys exactly
    /// one (<c>formula</c>) matched a parameter that exists in this product, so seventeen were
    /// dead and every real lookup already fell through to <c>ToPascalCase</c> (#37).
    ///
    /// Deleting it changes no output. The real fix is a <c>&lt;param&gt;</c> doc on the interface;
    /// this exists only so an undocumented parameter gets a readable label rather than nothing.
    /// </remarks>
    public static string GetParameterDescription(string paramName) => ToPascalCase(paramName);
}

/// <summary>
/// Type name handling utilities.
/// </summary>
public static class TypeNameHelper
{
    /// <summary>
    /// FullyQualifiedFormat with IncludeNullableReferenceTypeModifier to preserve
    /// nullable annotations on type arguments (e.g., object? in List&lt;List&lt;object?&gt;&gt;).
    /// </summary>
    private static readonly SymbolDisplayFormat NullableQualifiedFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    public static string GetTypeName(ITypeSymbol type, NullableAnnotation nullableAnnotation = NullableAnnotation.None)
    {
        if (type.SpecialType == SpecialType.System_Void)
            return "void";
        if (type.SpecialType == SpecialType.System_String)
        {
            return nullableAnnotation == NullableAnnotation.Annotated ? "string?" : "string";
        }
        if (type.SpecialType == SpecialType.System_Boolean)
            return "bool";
        if (type.SpecialType == SpecialType.System_Int32)
            return "int";

        if (type is INamedTypeSymbol namedType)
        {
            if (namedType.IsGenericType && namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
            {
                return GetTypeName(namedType.TypeArguments[0]) + "?";
            }
        }

        // For custom types, use fully qualified name with nullable reference annotations preserved
        var fullName = type.ToDisplayString(NullableQualifiedFormat);
        // Remove "global::" prefix
        if (fullName.StartsWith("global::"))
            fullName = fullName.Substring(8);

        // Handle outer nullable annotation for reference types (e.g., List<>? parameters)
        if (nullableAnnotation == NullableAnnotation.Annotated && !fullName.EndsWith("?"))
            fullName += "?";

        return fullName;
    }

    public static string GetDefaultValueString(IParameterSymbol param)
    {
        if (!param.HasExplicitDefaultValue)
            return "default";

        var value = param.ExplicitDefaultValue;

        if (value is null)
        {
            // Non-nullable value types (e.g. TimeSpan) with `= default` have ExplicitDefaultValue == null
            // in Roslyn (it cannot represent default(TimeSpan) as a constant). Emit `default` so the
            // generated C# compiles for structs. Nullable<T> structs (bool?, int?, TimeSpan?) can hold
            // null, so they keep `null` to avoid ambiguous DefaultValueAttribute constructor calls.
            var isNonNullableValueType = param.Type.IsValueType
                && !(param.Type is INamedTypeSymbol nts
                     && nts.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T);
            if (isNonNullableValueType)
                return "default";
            return "null";
        }
        if (value is bool b)
            return b ? "true" : "false";
        if (value is string s)
            return $"\"{s}\"";
        if (value is int or long or short or byte)
        {
            // Handle enum defaults
            if (param.Type.TypeKind == TypeKind.Enum)
            {
                var enumType = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (enumType.StartsWith("global::"))
                    enumType = enumType.Substring(8);

                // Find the enum member with this value
                foreach (var member in param.Type.GetMembers())
                {
                    if (member is IFieldSymbol field && field.HasConstantValue &&
                        field.ConstantValue?.Equals(value) == true)
                    {
                        return $"{enumType}.{field.Name}";
                    }
                }
                // Fallback to numeric value
                return $"({enumType}){value}";
            }
            return value.ToString()!;
        }

        // Handle double/float defaults — ensure decimal point so C# treats as double literal
        // Without this, double width = 400 → DefaultValue(400) → int → InvalidCastException
        if (value is double d)
        {
            var ds = d.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
            if (!ds.Contains('.') && !ds.Contains('E') && !ds.Contains('e'))
                ds += ".0";
            return ds;
        }
        if (value is float f)
        {
            var fs = f.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
            if (!fs.Contains('.') && !fs.Contains('E') && !fs.Contains('e'))
                fs += ".0";
            return fs + "f";
        }

        // Handle enum defaults
        if (param.Type.TypeKind == TypeKind.Enum)
        {
            return $"{param.Type.Name}.{value}";
        }

        return value.ToString() ?? "default";
    }
}
