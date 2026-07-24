using System.Buffers;
using System.Buffers.Text;
using System.IO.Pipelines;
using System.Text;
using IOPipelinesLab.Shared;

namespace IOPipelinesLab.Processors;

public static class PipelineProcessor
{
    public static async Task<int> ProcessAsync(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        var reader = PipeReader.Create(stream);
        int processedCount = 0;

        while (true)
        {
            ReadResult result = await reader.ReadAsync();
            ReadOnlySequence<byte> buffer = result.Buffer;

            while (TryReadLine(ref buffer, out ReadOnlySequence<byte> line))
            {
                ProcessLine(line);
                processedCount++;
            }

            reader.AdvanceTo(buffer.Start, buffer.End);

            if (result.IsCompleted)
                break;
        }

        await reader.CompleteAsync();
        return processedCount;
    }

    private static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
    {
        SequencePosition? position = buffer.PositionOf((byte)'\n');

        if (position == null)
        {
            line = default;
            return false;
        }

        line = buffer.Slice(0, position.Value);
        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
        return true;
    }

    private static void ProcessLine(ReadOnlySequence<byte> lineSequence)
    {
        if (lineSequence.IsSingleSegment)
        {
            ParseSpanUtf8(lineSequence.FirstSpan);
        }
        else
        {
            byte[] array = ArrayPool<byte>.Shared.Rent((int)lineSequence.Length);
            try
            {
                lineSequence.CopyTo(array);
                ParseSpanUtf8(array.AsSpan(0, (int)lineSequence.Length));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(array);
            }
        }
    }

    private static void ParseSpanUtf8(ReadOnlySpan<byte> lineSpan)
    {
        // Trim \r if present (Windows carriage return)
        if (!lineSpan.IsEmpty && lineSpan[^1] == (byte)'\r')
        {
            lineSpan = lineSpan[..^1];
        }

        if (lineSpan.IsEmpty)
            return;

        // 1. Slice Timestamp
        int commaIndex = lineSpan.IndexOf((byte)',');
        if (commaIndex == -1) return;
        ReadOnlySpan<byte> timestampSpan = lineSpan[..commaIndex];
        lineSpan = lineSpan[(commaIndex + 1)..];

        // Fast parsing of ISO DateTime directly from UTF-8 bytes without string allocation
        if (!Utf8Parser.TryParse(timestampSpan, out DateTime timestamp, out _, 'O'))
        {
            // Fallback parse if needed
            timestamp = DateTime.MinValue;
        }

        // 2. Slice LogLevel
        commaIndex = lineSpan.IndexOf((byte)',');
        if (commaIndex == -1) return;
        ReadOnlySpan<byte> logLevelSpan = lineSpan[..commaIndex];
        lineSpan = lineSpan[(commaIndex + 1)..];

        // 3. Slice Source
        commaIndex = lineSpan.IndexOf((byte)',');
        if (commaIndex == -1) return;
        ReadOnlySpan<byte> sourceSpan = lineSpan[..commaIndex];

        // 4. Slice Message
        ReadOnlySpan<byte> messageSpan = lineSpan[(commaIndex + 1)..];

        // String conversions happen strictly on final domain creation
        _ = new LogEntry(
            timestamp,
            Encoding.UTF8.GetString(logLevelSpan),
            Encoding.UTF8.GetString(sourceSpan),
            Encoding.UTF8.GetString(messageSpan));
    }
}