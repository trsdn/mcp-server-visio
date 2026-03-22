using System.Security.Cryptography;
using System.Text;
using System.Reflection;
using System.Globalization;
using System.Runtime.InteropServices;
using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.DocumentProperty;

public class DocumentPropertyCommands : IDocumentPropertyCommands
{
    private const int VisSectionProp = 243;
    private const int VisTagDefault = 0;

    public DocumentPropertyResult GetAll(IVisioBatch batch)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic document = ctx.Document;

            return new DocumentPropertyResult
            {
                Success = true,
                FilePath = ctx.DocumentPath,
                Title = ReadOptionalString(document, "Title"),
                Subject = ReadOptionalString(document, "Subject"),
                Author = ReadOptionalString(document, "Creator"),
                Keywords = ReadOptionalString(document, "Keywords"),
                Comments = ReadOptionalString(document, "Description", "Comments"),
                Company = ReadOptionalString(document, "Company"),
                Category = ReadOptionalString(document, "Category")
            };
        });
    }

    public OperationResult SetAll(IVisioBatch batch, string title, string subject, string author, string keywords, string comments, string company, string category)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic document = ctx.Document;

            if (!string.IsNullOrWhiteSpace(title))
            {
                WriteStringProperty(document, title, "Title");
            }

            if (!string.IsNullOrWhiteSpace(subject))
            {
                WriteStringProperty(document, subject, "Subject");
            }

            if (!string.IsNullOrWhiteSpace(author))
            {
                WriteStringProperty(document, author, "Creator");
            }

            if (!string.IsNullOrWhiteSpace(keywords))
            {
                WriteStringProperty(document, keywords, "Keywords");
            }

            if (!string.IsNullOrWhiteSpace(comments))
            {
                WriteStringProperty(document, comments, "Description", "Comments");
            }

            if (!string.IsNullOrWhiteSpace(company))
            {
                WriteStringProperty(document, company, "Company");
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                WriteStringProperty(document, category, "Category");
            }

            return new OperationResult
            {
                Success = true,
                Action = "set",
                Message = "Updated document properties",
                FilePath = ctx.DocumentPath
            };
        });
    }

    public OperationResult GetCustom(IVisioBatch batch, string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? documentSheet = null;
            dynamic? valueCell = null;
            try
            {
                documentSheet = ctx.Document.DocumentSheet;
                var rowName = ResolveExistingCustomPropertyRowName(documentSheet, propertyName);
                if (rowName is null)
                {
                    throw new InvalidOperationException($"Custom document property '{propertyName}' was not found.");
                }

                valueCell = documentSheet.CellsU[$"Prop.{rowName}.Value"];
                string value = ReadShapeDataValue(valueCell);

                return new OperationResult
                {
                    Success = true,
                    Action = "get-custom",
                    Message = $"{propertyName} = {value}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (valueCell != null)
                {
                    ComUtilities.Release(ref valueCell!);
                }

                if (documentSheet != null)
                {
                    ComUtilities.Release(ref documentSheet!);
                }
            }
        });
    }

    public OperationResult SetCustom(IVisioBatch batch, string propertyName, string propertyValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? documentSheet = null;
            dynamic? labelCell = null;
            dynamic? typeCell = null;
            dynamic? valueCell = null;
            try
            {
                documentSheet = ctx.Document.DocumentSheet;
                string rowName = ResolveTargetCustomPropertyRowName(documentSheet, propertyName);

                EnsureCustomPropertyRow(documentSheet, rowName);

                labelCell = documentSheet.CellsU[$"Prop.{rowName}.Label"];
                typeCell = documentSheet.CellsU[$"Prop.{rowName}.Type"];
                valueCell = documentSheet.CellsU[$"Prop.{rowName}.Value"];

                labelCell.FormulaU = ToStringFormula(propertyName);
                typeCell.FormulaU = "0";
                valueCell.FormulaU = ToStringFormula(propertyValue ?? string.Empty);

                return new OperationResult
                {
                    Success = true,
                    Action = "set-custom",
                    Message = $"Set custom property '{propertyName}' = '{propertyValue}'",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (valueCell != null)
                {
                    ComUtilities.Release(ref valueCell!);
                }

                if (typeCell != null)
                {
                    ComUtilities.Release(ref typeCell!);
                }

                if (labelCell != null)
                {
                    ComUtilities.Release(ref labelCell!);
                }

                if (documentSheet != null)
                {
                    ComUtilities.Release(ref documentSheet!);
                }
            }
        });
    }

    private static string? ReadOptionalString(object target, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            try
            {
                return GetStringProperty(target, propertyName);
            }
            catch
            {
                continue;
            }
        }

        return string.Empty;
    }

    private static string GetStringProperty(object target, string propertyName)
    {
        var value = target.GetType()
            .InvokeMember(propertyName, BindingFlags.GetProperty, binder: null, target, args: null, culture: CultureInfo.InvariantCulture);

        if (value is null)
        {
            return string.Empty;
        }

        if (value is string stringValue)
        {
            return stringValue;
        }

        if (Marshal.IsComObject(value))
        {
            throw new InvalidOperationException($"Property '{propertyName}' returned a COM proxy instead of a string value.");
        }

        return value.ToString() ?? string.Empty;
    }

    private static void WriteStringProperty(object target, string value, params string[] propertyNames)
    {
        Exception? lastException = null;

        foreach (var propertyName in propertyNames)
        {
            try
            {
                target.GetType().InvokeMember(
                    propertyName,
                    BindingFlags.SetProperty,
                    binder: null,
                    target,
                    [value],
                    culture: CultureInfo.InvariantCulture);
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
            }
        }

        throw new InvalidOperationException($"Error while invoking {string.Join("/", propertyNames)}.", lastException);
    }

    private static string ResolveTargetCustomPropertyRowName(dynamic documentSheet, string propertyName)
    {
        var existingRowName = ResolveExistingCustomPropertyRowName(documentSheet, propertyName);
        if (!string.IsNullOrEmpty(existingRowName))
        {
            return existingRowName;
        }

        var trimmedName = propertyName.Trim();
        if (IsValidShapeDataRowName(trimmedName))
        {
            return trimmedName;
        }

        return BuildNormalizedShapeDataRowName(trimmedName);
    }

    private static string? ResolveExistingCustomPropertyRowName(dynamic documentSheet, string propertyName)
    {
        foreach (var candidate in GetCandidateRowNames(propertyName))
        {
            if (TryGetCustomPropertyValueCell(documentSheet, candidate, out dynamic? valueCell))
            {
                ComUtilities.Release(ref valueCell!);
                return candidate;
            }
        }

        return null;
    }

    private static IEnumerable<string> GetCandidateRowNames(string propertyName)
    {
        var trimmedName = propertyName.Trim();
        if (IsValidShapeDataRowName(trimmedName))
        {
            yield return trimmedName;
        }

        var normalized = BuildNormalizedShapeDataRowName(trimmedName);
        if (!string.Equals(normalized, trimmedName, StringComparison.Ordinal))
        {
            yield return normalized;
        }
    }

    private static bool TryGetCustomPropertyValueCell(dynamic documentSheet, string rowName, out dynamic? valueCell)
    {
        valueCell = null;
        try
        {
            valueCell = documentSheet.CellsU[$"Prop.{rowName}.Value"];
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void EnsureCustomPropertyRow(dynamic documentSheet, string rowName)
    {
        if (TryGetCustomPropertyValueCell(documentSheet, rowName, out dynamic? existingCell))
        {
            ComUtilities.Release(ref existingCell!);
            return;
        }

        try
        {
            documentSheet.AddNamedRow(VisSectionProp, rowName, VisTagDefault);
        }
        catch
        {
            EnsureShapeDataSection(documentSheet);
            documentSheet.AddNamedRow(VisSectionProp, rowName, VisTagDefault);
        }
    }

    private static void EnsureShapeDataSection(dynamic documentSheet)
    {
        try
        {
            documentSheet.AddSection(VisSectionProp);
        }
        catch
        {
            // Section already exists or Visio created it implicitly.
        }
    }

    private static bool IsValidShapeDataRowName(string rowName)
    {
        if (string.IsNullOrWhiteSpace(rowName))
        {
            return false;
        }

        if (!(char.IsLetter(rowName[0]) || rowName[0] == '_'))
        {
            return false;
        }

        for (var index = 1; index < rowName.Length; index++)
        {
            char character = rowName[index];
            if (!(char.IsLetterOrDigit(character) || character == '_'))
            {
                return false;
            }
        }

        return true;
    }

    private static string BuildNormalizedShapeDataRowName(string propertyName)
    {
        var builder = new StringBuilder("Metadata_");
        bool previousUnderscore = true;

        foreach (char character in propertyName)
        {
            char normalized = char.IsLetterOrDigit(character) ? character : '_';
            if (normalized == '_' && previousUnderscore)
            {
                continue;
            }

            builder.Append(normalized);
            previousUnderscore = normalized == '_';
        }

        var baseName = builder.ToString().TrimEnd('_');
        if (baseName.Length == "Metadata".Length)
        {
            baseName = "Metadata_Property";
        }

        if (baseName.Length > 32)
        {
            baseName = baseName[..32];
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(propertyName)))[..8].ToLowerInvariant();
        return $"{baseName}_{hash}";
    }

    private static string ToStringFormula(string value)
    {
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static string ReadShapeDataValue(dynamic valueCell)
    {
        try
        {
            string? formula = valueCell.FormulaU?.ToString();
            if (string.IsNullOrEmpty(formula))
            {
                return string.Empty;
            }

            if (formula.Length >= 2 && formula[0] == '"' && formula[^1] == '"')
            {
                return formula[1..^1].Replace("\"\"", "\"", StringComparison.Ordinal);
            }

            return formula;
        }
        catch
        {
            return string.Empty;
        }
    }
}
