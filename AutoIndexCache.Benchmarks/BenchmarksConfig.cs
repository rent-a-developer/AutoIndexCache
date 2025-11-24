using BenchmarkDotNet.Configs;

namespace AutoIndexCache.Benchmarks;

public class BenchmarksConfig : ManualConfig
{
    /// <inheritdoc />
    public BenchmarksConfig()
    {
        this.Orderer = new BenchmarksOrderer();
    }
}