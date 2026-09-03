using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VisioMcp.Core.Json;

/// <summary>
/// Binds a tool's <c>action</c> parameter, mapping a value the enum does not define to an
/// undeclared sentinel instead of throwing.
///
/// The default <see cref="JsonStringEnumConverter{TEnum}"/> throws on an unrecognised string. That
/// happens during argument binding, before any generated tool code runs, so neither
/// <c>VisioToolsBase.ExecuteToolAction</c>'s catch nor the generated <c>action == null</c> branch
/// ever sees it. The MCP SDK turns the exception into <c>"An error occurred invoking 'text'"</c> —
/// no JSON, no valid-action list, nothing a model can recover from (#55).
///
/// The sentinel is <see cref="UnknownValue"/>, which is <c>-1</c> cast to the enum. Generated
/// enums number from zero upwards, so it collides with nothing, and because it is not a declared
/// member it does **not** appear in the published JSON schema — a well-behaved client still sees
/// exactly the valid values. Generated tool code tests it with <see cref="Enum.IsDefined"/> and
/// answers with JSON naming every action the tool accepts.
///
/// A nullable converter would have been the obvious shape, but <c>System.Text.Json</c> unwraps
/// <c>Nullable&lt;T&gt;</c> before consulting a converter registered on <c>T</c>, so the nullable
/// form is never offered to it. The sentinel survives that unwrapping.
/// </summary>
/// <typeparam name="TEnum">The generated action enum.</typeparam>
public sealed class LenientActionEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    /// <summary>
    /// The value produced for an action the enum does not define. Not a declared member, so it
    /// stays out of the schema; generated code detects it with <see cref="Enum.IsDefined"/>.
    /// </summary>
    private static readonly TEnum UnknownValue = (TEnum)Enum.ToObject(typeof(TEnum), -1);

    private static readonly ConcurrentDictionary<string, TEnum> ByWireName = BuildLookup();

    /// <inheritdoc />
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            return UnknownValue;
        }

        var raw = reader.GetString();

        return !string.IsNullOrWhiteSpace(raw) && ByWireName.TryGetValue(raw, out var value)
            ? value
            : UnknownValue;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(GetWireName(value));
    }

    private static ConcurrentDictionary<string, TEnum> BuildLookup()
    {
        var lookup = new ConcurrentDictionary<string, TEnum>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is TEnum value)
            {
                lookup[GetWireName(field)] = value;
            }
        }

        return lookup;
    }

    private static string GetWireName(FieldInfo field)
        => field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name ?? field.Name;

    private static string GetWireName(TEnum value)
    {
        var field = typeof(TEnum).GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static);
        return field is null ? value.ToString() : GetWireName(field);
    }
}
