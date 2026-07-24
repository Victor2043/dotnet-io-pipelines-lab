namespace IOPipelinesLab.Shared;

public readonly record struct LogEntry(
    DateTime Timestamp,
    string LogLevel,
    string Source,
    string Message);