using IOPipelinesLab.Shared;

namespace IOPipelinesLab.Processors;

public static class NaiveProcessor
{
    public static async Task<int> ProcessAsync(string filePath)
    {
        // 1. Reads the entire file into memory at once
        string[] lines = await File.ReadAllLinesAsync(filePath);
        int processedCount = 0;

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // 2. Allocates array of strings for each CSV line
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