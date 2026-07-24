using BenchmarkDotNet.Running;
using IOPipelinesLab.Benchmarks;

Console.WriteLine("==========================================================");
Console.WriteLine("🚀 LAB DE I/O PIPELINES");
Console.WriteLine("==========================================================");

BenchmarkRunner.Run<FileProcessorBenchmark>();