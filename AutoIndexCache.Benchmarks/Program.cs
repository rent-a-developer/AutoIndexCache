using BenchmarkDotNet.Running;

namespace AutoIndexCache.Benchmarks;

internal class Program
{
    private static void Main()
    {
        BenchmarkRunner.Run<Benchmarks>();
    }
}