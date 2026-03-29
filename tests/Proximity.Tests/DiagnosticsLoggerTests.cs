using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Proximity.Audio.Diagnostics;
using Proximity.Core.Interfaces;

namespace Proximity.Tests;

public class DiagnosticsLoggerTests
{
    [Fact]
    public void Constructor_InvalidInterval_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DiagnosticsLogger(NullLogger.Instance, () => null, intervalMs: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DiagnosticsLogger(NullLogger.Instance, () => null, intervalMs: -1));
    }

    [Fact]
    public void Constructor_SetsInterval()
    {
        using var logger = new DiagnosticsLogger(NullLogger.Instance, () => null, intervalMs: 2000);
        Assert.Equal(TimeSpan.FromMilliseconds(2000), logger.Interval);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var logger = new DiagnosticsLogger(NullLogger.Instance, () => null, intervalMs: 1000);
        logger.Dispose();
        // Double dispose should also not throw
        logger.Dispose();
    }

    [Fact]
    public async Task Logger_InvokesSnapshotProviderPeriodically()
    {
        int callCount = 0;
        var diag = new AudioDiagnostics();

        AudioDiagnosticsSnapshot? Provider()
        {
            Interlocked.Increment(ref callCount);
            return diag.GetSnapshot(true, false, 0, 0);
        }

        // Use a very short interval to trigger quickly in the test
        using var diagLogger = new DiagnosticsLogger(NullLogger.Instance, Provider, intervalMs: 50);

        // Wait enough time for at least 2 ticks
        await Task.Delay(200);

        Assert.True(callCount >= 2, $"Expected at least 2 snapshot calls but got {callCount}");
    }

    [Fact]
    public async Task Logger_HandlesNullSnapshotGracefully()
    {
        // Provider returns null — should not throw
        using var diagLogger = new DiagnosticsLogger(NullLogger.Instance, () => null, intervalMs: 50);
        await Task.Delay(150);
        // If we get here without exception, the test passes
    }

    [Fact]
    public async Task Logger_HandlesProviderException_Gracefully()
    {
        int callCount = 0;

        AudioDiagnosticsSnapshot? Provider()
        {
            Interlocked.Increment(ref callCount);
            throw new InvalidOperationException("Test failure");
        }

        using var diagLogger = new DiagnosticsLogger(NullLogger.Instance, Provider, intervalMs: 50);
        await Task.Delay(200);

        // Should have attempted at least 2 calls without crashing
        Assert.True(callCount >= 2, $"Expected at least 2 calls despite exceptions, got {callCount}");
    }

    [Fact]
    public async Task Logger_LogsWithTestLogger()
    {
        var sink = new TestLogSink();
        var logger = new TestLogger(sink);
        var diag = new AudioDiagnostics();

        using var diagLogger = new DiagnosticsLogger(logger, () => diag.GetSnapshot(true, false, 0, 0), intervalMs: 50);
        await Task.Delay(200);

        Assert.True(sink.Messages.Count >= 2, $"Expected at least 2 log messages but got {sink.Messages.Count}");
        Assert.Contains(sink.Messages, m => m.Contains("[AudioDiag]"));
    }

    /// <summary>Simple in-memory logger for capturing log output in tests.</summary>
    private sealed class TestLogSink
    {
        private readonly List<string> _messages = new();
        private readonly object _lock = new();

        public IReadOnlyList<string> Messages
        {
            get { lock (_lock) { return _messages.ToList(); } }
        }

        public void Add(string message)
        {
            lock (_lock) { _messages.Add(message); }
        }
    }

    private sealed class TestLogger : ILogger
    {
        private readonly TestLogSink _sink;
        public TestLogger(TestLogSink sink) => _sink = sink;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _sink.Add(formatter(state, exception));
        }
    }
}
