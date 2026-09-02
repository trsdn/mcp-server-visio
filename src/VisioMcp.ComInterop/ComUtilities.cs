using System.Runtime.InteropServices;

namespace VisioMcp.ComInterop;

/// <summary>
/// Low-level COM interop utilities for Office automation.
/// Provides helpers for managing COM object lifecycle.
/// </summary>
public static class ComUtilities
{
    /// <summary>
    /// Safely releases a COM object and sets the reference to null
    /// </summary>
    /// <param name="comObject">The COM object to release</param>
    /// <remarks>
    /// Use this helper to release intermediate COM objects (like slides, shapes)
    /// to prevent Visio process from staying open. This is especially important when
    /// iterating through collections or accessing multiple COM properties.
    /// </remarks>
    /// <example>
    /// <code>
    /// dynamic? slides = null;
    /// try
    /// {
    ///     slides = presentation.Slides;
    ///     // Use slides...
    /// }
    /// finally
    /// {
    ///     ComUtilities.Release(ref slides);
    /// }
    /// </code>
    /// </example>
    public static void Release<T>(ref T? comObject) where T : class
    {
        if (comObject != null)
        {
            try
            {
                Marshal.ReleaseComObject(comObject);
            }
            catch (Exception)
            {
                // Ignore errors during release — COM object may already be released or RPC disconnected
            }
            comObject = null;
        }
    }

    /// <summary>
    /// Safely attempts to quit an application COM object.
    /// This is a fire-and-forget cleanup helper - errors are swallowed.
    /// </summary>
    /// <param name="application">The application COM object</param>
    /// <remarks>
    /// Use this for cleanup scenarios where you want to quit the application but don't
    /// need to handle or report errors. For production shutdown with retry
    /// logic, use VisioShutdownService.CloseAndQuit instead.
    /// </remarks>
    public static void TryQuitVisio(dynamic? application)
    {
        if (application == null) return;

        try
        {
            application.Quit();
        }
        catch (Exception)
        {
            // Swallow errors during cleanup — the application may already be gone
        }
    }

    /// <summary>
    /// Safely gets a string property from a COM object, returning empty string if null or if the
    /// COM call fails.
    /// </summary>
    /// <remarks>
    /// Only the property names listed below are supported. A name outside that set throws, rather
    /// than returning an empty string: an unsupported name is a mistake in the calling code, and
    /// silently reporting a populated COM property as empty hides it. Add a case here to support a
    /// new property.
    /// </remarks>
    /// <param name="obj">COM object</param>
    /// <param name="propertyName">One of: Name, NameU, Description, Prompt, UniqueID</param>
    /// <returns>Property value, or empty string when the property is null or the COM call fails</returns>
    /// <exception cref="ArgumentException">The property name is not one this method can read.</exception>
    public static string SafeGetString(dynamic? obj, string propertyName)
    {
        try
        {
            object? value = propertyName switch
            {
                "Name" => obj.Name,
                "NameU" => obj.NameU,
                "Description" => obj.Description,
                "Prompt" => obj.Prompt,
                "UniqueID" => obj.UniqueID,
                _ => throw new ArgumentException(
                    $"SafeGetString cannot read '{propertyName}'. Supported: Name, NameU, Description, "
                    + "Prompt, UniqueID. Add a case to ComUtilities.SafeGetString, or read the property "
                    + "directly — returning an empty string here would hide a value that exists.",
                    nameof(propertyName))
            };
            return value?.ToString() ?? string.Empty;
        }
        catch (ArgumentException)
        {
            // An unsupported property name is a coding error, not a COM failure to absorb.
            throw;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Safely gets an integer property from a COM object, returning 0 if null or if the COM call
    /// fails.
    /// </summary>
    /// <remarks>
    /// As with <see cref="SafeGetString"/>, an unsupported property name throws rather than
    /// returning 0 — a zero count that is really "this method cannot read that property" is
    /// indistinguishable from an empty collection.
    /// </remarks>
    /// <param name="obj">COM object</param>
    /// <param name="propertyName">Currently only: Count</param>
    /// <returns>Property value, or 0 when the property is null or the COM call fails</returns>
    /// <exception cref="ArgumentException">The property name is not one this method can read.</exception>
    public static int SafeGetInt(dynamic? obj, string propertyName)
    {
        try
        {
            object? value = propertyName switch
            {
                "Count" => obj.Count,
                _ => throw new ArgumentException(
                    $"SafeGetInt cannot read '{propertyName}'. Supported: Count. Add a case to "
                    + "ComUtilities.SafeGetInt, or read the property directly — returning 0 here "
                    + "would be indistinguishable from a genuine zero.",
                    nameof(propertyName))
            };
            return Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    [DllImport("kernel32.dll")]
    private static extern void Sleep(uint dwMilliseconds);

    /// <summary>
    /// Kernel-level sleep that does NOT pump the STA COM message queue.
    /// Unlike Thread.Sleep (which uses CoWaitForMultipleHandles internally and wakes early on
    /// every incoming COM event), this calls Win32 Sleep() directly via NtDelayExecution —
    /// the thread genuinely sleeps for the full interval regardless of COM callbacks.
    /// </summary>
    public static void KernelSleep(int milliseconds) =>
        Sleep((uint)Math.Max(0, milliseconds));
}


