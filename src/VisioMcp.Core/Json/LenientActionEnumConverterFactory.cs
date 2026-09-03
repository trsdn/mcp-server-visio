using System.Text.Json;
using System.Text.Json.Serialization;

namespace VisioMcp.Core.Json;

/// <summary>
/// Supplies <see cref="LenientActionEnumConverter{TEnum}"/> for every generated tool action enum.
///
/// A <c>[JsonConverter]</c> attribute on the enum itself is not enough. In System.Text.Json the
/// precedence is: a converter on the member, then <c>options.Converters</c>, and only then an
/// attribute on the type. The MCP SDK registers its own enum converter in <c>options.Converters</c>,
/// so it wins and the attribute never runs — which is exactly what the failure showed:
///
/// <code>
/// System.Text.Json.JsonException: The JSON value could not be converted to
///   System.Nullable`1[VisioMcp.Generated.TextAction]
///     at System.Text.Json.Serialization.Converters.EnumConverter`1.Read(...)
/// </code>
///
/// So this factory has to be inserted ahead of the SDK's converter in the options handed to
/// <c>WithToolsFromAssembly</c>, rather than attached to the enums.
/// </summary>
public sealed class LenientActionEnumConverterFactory : JsonConverterFactory
{
    /// <summary>
    /// The namespace the service-registry generator emits action enums into.
    /// </summary>
    private const string GeneratedNamespace = "VisioMcp.Generated";

    /// <inheritdoc />
    /// <remarks>
    /// Deliberately claims only the non-nullable enum. System.Text.Json wraps it for
    /// <c>Nullable&lt;T&gt;</c> itself, and rejects a factory that claims the nullable form while
    /// returning a converter for the underlying one. The sentinel survives that wrapping, so the
    /// nullable parameter still receives it.
    /// </remarks>
    public override bool CanConvert(Type typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);

        return typeToConvert.IsEnum
            && string.Equals(typeToConvert.Namespace, GeneratedNamespace, StringComparison.Ordinal)
            && typeToConvert.Name.EndsWith("Action", StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);

        return (JsonConverter?)Activator.CreateInstance(
            typeof(LenientActionEnumConverter<>).MakeGenericType(typeToConvert));
    }
}
