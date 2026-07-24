using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using IOPipelinesLab.Processors;

namespace IOPipelinesLab.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class FileProcessorBenchmark
{
    private string _tempFilePath = default!;

    [GlobalSetup]
    public void Setup()
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"benchmark_log_{Guid.NewGuid()}.csv");

        // Generates a mock ~15MB CSV log file
        using var writer = new StreamWriter(_tempFilePath);
        var baseDate = new DateTime(2026, 1, 1, 10, 0, 0);

        for (int i = 0; i < 200_000; i++)
        {
            writer.WriteLine($"{baseDate.AddSeconds(i):o},INFO,AuthService,User authenticated successfully with ID {i}");
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    [Benchmark(Baseline = true)]
    public async Task<int> Naive()
    {
        return await NaiveProcessor.ProcessAsync(_tempFilePath);
    }

    [Benchmark]
    public async Task<int> BufferedStream()
    {
        return await BufferedStreamProcessor.ProcessAsync(_tempFilePath);
    }

    [Benchmark]
    public async Task<int> Pipeline()
    {
        return await PipelineProcessor.ProcessAsync(_tempFilePath);
    }
}