using System.Collections.Immutable;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace AutoIndexCache.Benchmarks;

public class BenchmarksOrderer : IOrderer
{
    /// <inheritdoc />
    public Boolean SeparateLogicalGroups => true;

    /// <inheritdoc />
    public IEnumerable<BenchmarkCase> GetExecutionOrder(ImmutableArray<BenchmarkCase> benchmarksCase, IEnumerable<BenchmarkLogicalGroupRule>? order = null)
    {
        return benchmarksCase.OrderBy(a => a.Descriptor.WorkloadMethod.DeclaringType!.GetMethods().ToList().IndexOf(a.Descriptor.WorkloadMethod));
    }

    /// <inheritdoc />
    public String? GetHighlightGroupKey(BenchmarkCase benchmarkCase)
    {
        return benchmarkCase.Descriptor.Categories.FirstOrDefault();
    }

    /// <inheritdoc />
    public String? GetLogicalGroupKey(ImmutableArray<BenchmarkCase> allBenchmarksCases, BenchmarkCase benchmarkCase)
    {
        return benchmarkCase.Descriptor.Categories.FirstOrDefault();
    }

    /// <inheritdoc />
    public IEnumerable<IGrouping<String, BenchmarkCase>> GetLogicalGroupOrder(IEnumerable<IGrouping<String, BenchmarkCase>> logicalGroups, IEnumerable<BenchmarkLogicalGroupRule>? order = null)
    {
        return logicalGroups.OrderBy(it => it.Key);
    }

    /// <inheritdoc />
    public IEnumerable<BenchmarkCase> GetSummaryOrder(ImmutableArray<BenchmarkCase> benchmarksCases, Summary summary)
    {
        return benchmarksCases.OrderBy(a => a.Descriptor.WorkloadMethod.DeclaringType!.GetMethods().ToList().IndexOf(a.Descriptor.WorkloadMethod));
    }
}