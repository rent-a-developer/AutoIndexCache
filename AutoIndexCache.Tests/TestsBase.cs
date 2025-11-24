using System.Diagnostics;
using AutoIndexCache.Tests.TestData;
using NUnit.Framework;

#pragma warning disable CS8618

namespace AutoIndexCache.Tests;

public abstract class TestsBase
{
    #region Setup/Teardown

    [SetUp]
    public void SetUp()
    {
        this.TestUsers = new User[10_000];

        for (var i = 1; i <= 10_000; i++)
        {
            this.TestUsers[i - 1] =
                new(
                    i,
                    "User " + i,
                    i / 100 + 1,
                    i % 2 == 0
                );
        }

        this.FirstActiveTestUser = this.TestUsers.First(a => a.IsActive);
    }

    #endregion

    /// <summary>
    /// Executes <paramref name="action" /> and measures the time it took to execute it.
    /// </summary>
    /// <param name="action">The delegate to measure the execution time of.</param>
    /// <returns>The time it took to execute <paramref name="action" />.</returns>
    protected static TimeSpan ExecutionTimeOf(Action action)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        action();
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }

    protected User[] TestUsers;
    protected User FirstActiveTestUser;
}