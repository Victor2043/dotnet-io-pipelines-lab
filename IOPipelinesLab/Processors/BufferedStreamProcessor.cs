using IOPipelinesLab.Shared;

namespace IOPipelinesLab.Processors;

public static class BufferedStreamProcessor
{
    public static async Task<int> ProcessAsync(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        using var reader = new StreamReader(stream);

        int processedCount = 0;
        string? line;

        // 1. Streams the file line by line to avoid loading the entire file into RAM
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] parts = line.Split(',');
            if (parts.Length >= 4)
            {
                _ = new LogEntry(
                    DateTime.Parse(parts[0]),
                    parts[1],
                    parts[2],
                    parts[3]);

                processedCount++;
            }
        }

        return processedCount;
    }
}