using BenchmarkDotNet.Configs;

namespace RentADeveloper.AutoIndexCache.Benchmarks;

public class BenchmarksConfig : ManualConfig
{
    /// <inheritdoc />
    public BenchmarksConfig()
    {
        this.Orderer = new BenchmarksOrderer();
    }
}