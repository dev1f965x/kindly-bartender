namespace KindlyBartender.Core.Diagnostics;

/// <summary>Writes fixed events to the diagnostic log; see <see cref="DiagnosticLogFile"/>.</summary>
public interface IDiagnosticLog
{
    void Write(LogEvent logEvent);

    void Write(LogEvent logEvent, long value);

    void Write<TValue>(LogEvent logEvent, TValue value)
        where TValue : struct, Enum;

    /// <summary>Writes the exception's type and HResult only.</summary>
    void Write(LogEvent logEvent, Exception exception);
}
