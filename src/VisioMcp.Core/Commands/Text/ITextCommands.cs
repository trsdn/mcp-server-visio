using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Text;

/// <summary>
/// Text operations within shapes: get, set, format, find, replace.
/// </summary>
[ServiceCategory("text")]
[McpTool("text", Title = "Text Operations", Destructive = true, Category = "text",
    Description = "Get, set, format, find, and replace text in shapes. "
    + "WORKFLOW: shape(add-textbox) → text(set) → text(format, font_name='Calibri', font_size=14, bold=true). "
    + "'find'/'replace' work across pages (page_index=0 for all). "
    + "format alignment: 'left'/'center'/'right'/'justify'. vertical_alignment: 'top'/'middle'/'bottom'. "
    + "bullet_type: 0=None, 1=Unnumbered (bullets), 2=Numbered. indent_level: 0-4. "
    + "change_case case_type: 1=Sentence, 2=Lower, 3=Upper, 4=Title, 5=Toggle. "
    + "color_hex: '#RRGGBB'. Combine multiple properties in one format call for efficiency. "
    + "insert-page-number and insert-datetime append literal text, not a live field, so they do not update when pages are reordered. "
    + "empty-placeholder-audit and insert-link are not supported: Visio pages have no layout placeholders, and Visio links whole shapes rather than text runs.")]
public interface ITextCommands
{
    /// <summary>
    /// Get text content from a shape including paragraph and run details.
    /// </summary>
    [ServiceAction("get")]
    TextResult GetText(IVisioBatch batch, int pageIndex, string shapeName);

    /// <summary>
    /// Set the text content of a shape (replaces all existing text).
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name</param>
    /// <param name="text">Replacement text for the whole shape. Existing text and its run formatting are discarded</param>
    [ServiceAction("set")]
    OperationResult SetText(IVisioBatch batch, int pageIndex, string shapeName, string text);

    /// <summary>
    /// Find text across all shapes on a page or across the entire document.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="searchText">Text to find</param>
    /// <param name="pageIndex">0 for all pages, or a specific 1-based page index</param>
    [ServiceAction("find")]
    OperationResult Find(IVisioBatch batch, string searchText, int pageIndex);

    /// <summary>
    /// Replace text across all shapes on a page or across the entire document.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="searchText">Text to find</param>
    /// <param name="replaceText">Replacement text</param>
    /// <param name="pageIndex">0 for all pages, or a specific 1-based page index</param>
    [ServiceAction("replace")]
    OperationResult Replace(IVisioBatch batch, string searchText, string replaceText, int pageIndex);

    /// <summary>
    /// Format text in a shape (font, size, bold, italic, color, alignment).
    /// Horizontal alignment: left, center, right, justify.
    /// Vertical alignment: top, middle, bottom.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name</param>
    /// <param name="fontName">Font family name or null to keep the current font</param>
    /// <param name="fontSize">Font size in points or null to keep the current size</param>
    /// <param name="bold">Set bold or null to leave unchanged</param>
    /// <param name="italic">Set italic or null to leave unchanged</param>
    /// <param name="color">Hex color string (#RRGGBB) or null to keep the current color</param>
    /// <param name="alignment">Horizontal alignment: left, center, right, justify; null leaves unchanged</param>
    /// <param name="verticalAlignment">Vertical alignment: top, middle, bottom; null leaves unchanged</param>
    [ServiceAction("format")]
    OperationResult Format(IVisioBatch batch, int pageIndex, string shapeName, string? fontName, float? fontSize, bool? bold, bool? italic, string? color, string? alignment, string? verticalAlignment);

    /// <summary>
    /// Set advanced text formatting: underline, strikethrough, subscript, superscript.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name</param>
    /// <param name="underline">Set underline (null = don't change)</param>
    /// <param name="strikethrough">Set strikethrough (null = don't change)</param>
    /// <param name="subscript">Set subscript (null = don't change)</param>
    /// <param name="superscript">Set superscript (null = don't change)</param>
    [ServiceAction("format-advanced")]
    OperationResult FormatAdvanced(IVisioBatch batch, int pageIndex, string shapeName, bool? underline, bool? strikethrough, bool? subscript, bool? superscript);

    /// <summary>
    /// Count words across all pages or on a specific page.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">0 for all pages, or a specific 1-based page index</param>
    [ServiceAction("word-count")]
    OperationResult WordCount(IVisioBatch batch, int pageIndex);

    /// <summary>
    /// Report shapes missing alt text (AlternativeText).
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">0 for all pages, or a specific 1-based page index</param>
    [ServiceAction("alt-text-audit")]
    OperationResult AltTextAudit(IVisioBatch batch, int pageIndex);

    /// <summary>
    /// Not supported: Visio pages have no layout placeholders to audit. Throws NotSupportedException.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">0 for all pages, or a specific 1-based page index</param>
    [ServiceAction("empty-placeholder-audit")]
    OperationResult EmptyPlaceholderAudit(IVisioBatch batch, int pageIndex);

    /// <summary>
    /// Set paragraph and character spacing for text in a shape.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name</param>
    /// <param name="lineSpacing">Line spacing in points (null = don't change)</param>
    /// <param name="spaceBefore">Space before paragraph in points (null = don't change)</param>
    /// <param name="spaceAfter">Space after paragraph in points (null = don't change)</param>
    /// <param name="characterSpacing">Character spacing in points (null = don't change)</param>
    [ServiceAction("set-spacing")]
    OperationResult SetSpacing(IVisioBatch batch, int pageIndex, string shapeName, float? lineSpacing, float? spaceBefore, float? spaceAfter, float? characterSpacing);

    /// <summary>
    /// Set bullet point style for text in a shape.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name</param>
    /// <param name="bulletType">0=None, 1=Unnumbered (bullets), 2=Numbered</param>
    /// <param name="bulletCharacter">Custom bullet character (e.g. "•", "→") - only used when bulletType is 1</param>
    /// <param name="indentLevel">Indent level 0-4</param>
    [ServiceAction("set-bullets")]
    OperationResult SetBullets(IVisioBatch batch, int pageIndex, string shapeName, int bulletType, string? bulletCharacter, int indentLevel);

    /// <summary>
    /// Insert a hyperlink on existing text within a shape.
    /// Finds linkText within the shape's text and adds a hyperlink to it.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name</param>
    /// <param name="linkText">Text to find and make into a hyperlink</param>
    /// <param name="url">URL for the hyperlink</param>
    [ServiceAction("insert-link")]
    OperationResult InsertLink(IVisioBatch batch, int pageIndex, string shapeName, string linkText, string url);

    /// <summary>
    /// Change the case of text in a shape.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name</param>
    /// <param name="caseType">1=Sentence, 2=Lower, 3=Upper, 4=Title, 5=Toggle</param>
    [ServiceAction("change-case")]
    OperationResult ChangeCase(IVisioBatch batch, int pageIndex, string shapeName, int caseType);

    /// <summary>
    /// Read paragraph and character spacing from a shape's text.
    /// Returns SpaceWithin, SpaceBefore, SpaceAfter, and character Spacing.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name</param>
    [ServiceAction("read-spacing")]
    OperationResult ReadSpacing(IVisioBatch batch, int pageIndex, string shapeName);

    /// <summary>
    /// Read bullet settings from a shape's text.
    /// Returns Bullet.Type, Bullet.Character, and IndentLevel for each paragraph.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name</param>
    [ServiceAction("read-bullets")]
    OperationResult ReadBullets(IVisioBatch batch, int pageIndex, string shapeName);

    /// <summary>
    /// Insert a symbol character from a specified font into a shape's text.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name</param>
    /// <param name="fontName">Font name containing the symbol (e.g. "Wingdings")</param>
    /// <param name="charNumber">Unicode/character code of the symbol</param>
    [ServiceAction("insert-symbol")]
    OperationResult InsertSymbol(IVisioBatch batch, int pageIndex, string shapeName, string fontName, int charNumber);

    /// <summary>
    /// Append the current date and time to a shape as literal text. Visio has no live date field reachable through a single cell write, so the value does not update.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name</param>
    /// <param name="dateTimeFormat">Date/time format selector (1-13)</param>
    [ServiceAction("insert-datetime")]
    OperationResult InsertDateTime(IVisioBatch batch, int pageIndex, string shapeName, int dateTimeFormat);

    /// <summary>
    /// Append the current page number to a shape as literal text. Not a live field, so it does not update when pages are reordered.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name</param>
    [ServiceAction("insert-page-number")]
    OperationResult InsertPageNumber(IVisioBatch batch, int pageIndex, string shapeName);
}
