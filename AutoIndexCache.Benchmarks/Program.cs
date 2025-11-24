using BenchmarkDotNet.Running;

namespace RentADeveloper.AutoIndexCache.Benchmarks;

internal class Program
{
    private static void Main()
    {
        BenchmarkRunner.Run<Benchmarks>();
    }
}