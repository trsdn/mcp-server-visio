using Xunit;

namespace VisioMcp.ComInterop.Tests.Unit;

/// <summary>
/// Unit tests for OleMessageFilter registration and revocation.
/// Tests verify that the message filter can be registered/revoked without errors.
///
/// NOTE: These tests verify the registration mechanism but don't test actual
/// COM retry behavior (that requires Visio and would be OnDemand tests).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Speed", "Fast")]
[Trait("Layer", "ComInterop")]
public class OleMessageFilterTests
{
    [Fact]
    public void Register_OnStaThread_DoesNotThrow()
    {
        // Arrange & Act & Assert
        var thread = new Thread(() =>
        {
            try
            {
                OleMessageFilter.Register();
                OleMessageFilter.Revoke();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Filter registration failed: {ex.Message}", ex);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }

    [Fact]
    public void RegisterAndRevoke_MultipleTimes_DoesNotThrow()
    {
        // Arrange & Act & Assert
        var thread = new Thread(() =>
        {
            // First registration
            OleMessageFilter.Register();
            OleMessageFilter.Revoke();

            // Second registration (simulates reuse)
            OleMessageFilter.Register();
            OleMessageFilter.Revoke();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }

    [Fact]
    public void Revoke_WithoutRegister_DoesNotThrow()
    {
        // Revoke without prior Register should not crash
        // Arrange & Act & Assert - Should handle gracefully
        var thread = new Thread(OleMessageFilter.Revoke);

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }

    /// <summary>
    /// REGRESSION TEST for the STA deadlock bug:
    /// MessagePending MUST return PENDINGMSG_WAITDEFPROCESS (2), NOT PENDINGMSG_WAITNOPROCESS (1).
    ///
    /// The values come from objidl.h:
    ///   PENDINGMSG_CANCELCALL     = 0
    ///   PENDINGMSG_WAITNOPROCESS  = 1
    ///   PENDINGMSG_WAITDEFPROCESS = 2
    ///
    /// Returning WAITNOPROCESS blocks ALL inbound COM message processing while an outgoing
    /// call is in progress. When Visio fires a re-entrant callback during a long operation,
    /// the callback is queued but never dispatched. Visio waits for the callback, the STA
    /// thread waits for Visio, and the batch deadlocks.
    ///
    /// Returning WAITDEFPROCESS lets COM dispatch the pending inbound call into
    /// HandleInComingCall, which either accepts it or rejects it with SERVERCALL_RETRYLATER.
    /// </summary>
    [Fact]
    public void MessagePending_ReturnValue_MustBe_WaitDefProcess()
    {
        // The IOleMessageFilter interface is internal, so we instantiate the filter and
        // call MessagePending through the interface via reflection.
        const int PENDINGMSG_WAITNOPROCESS = 1;
        const int PENDINGMSG_WAITDEFPROCESS = 2;

        var returnValue = -1;
        Exception? threadException = null;

        var thread = new Thread(() =>
        {
            try
            {
                OleMessageFilter.Register();

                // The filter implements IOleMessageFilter which is internal.
                // We can verify via the public static IsRegistered and the logical behavior:
                // After Register(), the filter IS the active message filter for this thread.
                //
                // Verify that the filter is registered (prerequisite for the bug to manifest).
                Assert.True(OleMessageFilter.IsRegistered, "Filter must be registered to have any effect");

                // Use reflection to invoke MessagePending on the filter instance.
                // The filter class is internal, but we can get to it via the assembly.
                var filterType = typeof(OleMessageFilter);
                var iOleMsgFilterType = filterType.Assembly.GetType(
                    "VisioMcp.ComInterop.IOleMessageFilter");
                Assert.NotNull(iOleMsgFilterType);

                // Create a filter instance and call MessagePending
                var filterInstance = Activator.CreateInstance(filterType);
                Assert.NotNull(filterInstance);
                var method = iOleMsgFilterType.GetMethod("MessagePending");
                Assert.NotNull(method);

                returnValue = (int)method.Invoke(filterInstance, [IntPtr.Zero, 1000, 1])!;
                OleMessageFilter.Revoke();
            }
            catch (Exception ex)
            {
                threadException = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadException != null) throw new InvalidOperationException($"Thread exception: {threadException.Message}", threadException);

        // REGRESSION: If this returns 1 (WAITNOPROCESS), inbound COM callbacks are never
        // dispatched while the STA thread waits on an outgoing call, and the batch deadlocks.
        Assert.NotEqual(PENDINGMSG_WAITNOPROCESS, returnValue);
        Assert.Equal(PENDINGMSG_WAITDEFPROCESS, returnValue);
    }
}





