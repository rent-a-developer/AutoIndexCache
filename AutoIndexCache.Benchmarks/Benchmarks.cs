using AutoIndexCache.Tests.TestData;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

// ReSharper disable LocalVariableHidesMember
// ReSharper disable InconsistentNaming

namespace AutoIndexCache.Benchmarks;

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[Config(typeof(BenchmarksConfig))]
[MaxIterationCount(16)]
[IterationTime(150)]
[GcServer]
[HtmlExporter]
[RPlotExporter]
[KeepBenchmarkFiles]
public class Benchmarks
{
    [GlobalSetup]
    public void _GlobalSetup()
    {
        this.users = new User[10_000];

        var random = new Random();

        for (var i = 1; i <= 10_000; i++)
        {
            this.users[i - 1] = 
                new(
                    i,
                    "User " + i,
                    random.Next(1, 100),
                    random.Next(1, 10) >= 5
                );
        }

        this.dictionary_UserById = new();
        this.dictionary_UserByUserName = new();
        this.dictionary_UsersByGroupId = new();

        foreach (var user in this.users)
        {
            this.dictionary_UserById.Add(user.Id, user);
            this.dictionary_UserByUserName.Add(user.UserName, user);

            if (!this.dictionary_UsersByGroupId.TryGetValue(user.GroupId, out var groupIdUsers))
            {
                groupIdUsers = [];
                this.dictionary_UsersByGroupId.Add(user.GroupId, groupIdUsers);
            }

            groupIdUsers.Add(user);
        }

        this.autoIndexCache = new();
        this.autoIndexCache.SetItemsLoader(() => this.users);

        this.autoIndexCache_ItemsList_Users = this.autoIndexCache.Items<User>();
        this.autoIndexCache_UniqueIndex_UserById = this.autoIndexCache.Items<User>().UniqueIndex(a => a.Id);
        this.autoIndexCache_UniqueIndex_UserByUserName = this.autoIndexCache.Items<User>().UniqueIndex(a => a.UserName);
        this.autoIndexCache_NonUniqueIndex_UsersByGroupId = this.autoIndexCache.Items<User>().NonUniqueIndex(a => a.GroupId);

        this.autoIndexCache.Items<User>().GetAllItems();
        this.autoIndexCache.Items<User>().UniqueIndex(a => a.Id).GetKeys();
        this.autoIndexCache.Items<User>().UniqueIndex(a => a.UserName).GetKeys();
        this.autoIndexCache.Items<User>().NonUniqueIndex(a => a.GroupId).GetKeys();
    }

    [BenchmarkCategory("Initialization"), Benchmark(Baseline = true)]
    public Object Initialization_Dictionary()
    {
        var dictionary_UserById = new Dictionary<Int64, User>();
        var dictionary_UserByUserName = new Dictionary<String, User>();
        var dictionary_UsersByGroupId = new Dictionary<Int64, List<User>>();

        foreach (var user in this.users)
        {
            dictionary_UserById.Add(user.Id, user);
            dictionary_UserByUserName.Add(user.UserName, user);

            if (!dictionary_UsersByGroupId.TryGetValue(user.GroupId, out var groupIdUsers))
            {
                groupIdUsers = [];
                dictionary_UsersByGroupId.Add(user.GroupId, groupIdUsers);
            }

            groupIdUsers.Add(user);
        }

        return new Object?[]
        {
            dictionary_UserById,
            dictionary_UserByUserName,
            dictionary_UsersByGroupId
        };
    }

    [BenchmarkCategory("Initialization"), Benchmark]
    public Object Initialization_AutoIndexCacheOptimized()
    {
        var autoIndexCache = new AutoIndexCache();
        autoIndexCache.SetItemsLoader(() => this.users);

        var users = autoIndexCache.Items<User>();
        var userById = users.UniqueIndex(a => a.Id);
        var userByUserName = users.UniqueIndex(a => a.UserName);
        var usersByGroupId = users.NonUniqueIndex(a => a.GroupId);

        return new Object?[]
        {
            userById.GetItemOrDefault(1L),
            userByUserName.GetItemOrDefault("User 1"),
            usersByGroupId.GetItems(1L)
        };
    }

    [BenchmarkCategory("Initialization"), Benchmark]
    public Object Initialization_AutoIndexCache()
    {
        var autoIndexCache = new AutoIndexCache();
        autoIndexCache.SetItemsLoader(() => this.users);

        return new Object?[]
        {
            autoIndexCache.Items<User>().UniqueIndex(a => a.Id).GetItemOrDefault(1L),
            autoIndexCache.Items<User>().UniqueIndex(a => a.UserName).GetItemOrDefault("User 1"),
            autoIndexCache.Items<User>().NonUniqueIndex(a => a.GroupId).GetItems(1L)
        };
    }

    [BenchmarkCategory("List_GetAllItems"), Benchmark(Baseline = true)]
    public Object List_GetAllItems_Dictionary()
    {
        return this.dictionary_UserById.Values;
    }

    [BenchmarkCategory("List_GetAllItems"), Benchmark]
    public Object List_GetAllItems_AutoIndexCacheOptimized()
    {
        return this.autoIndexCache_ItemsList_Users.GetAllItems();
    }

    [BenchmarkCategory("List_GetAllItems"), Benchmark]
    public Object List_GetAllItems_AutoIndexCache()
    {
        return this.autoIndexCache.Items<User>().GetAllItems();
    }

    [BenchmarkCategory("NonUniqueIndex_ContainsKey"), Benchmark(Baseline = true)]
    public Object NonUniqueIndex_ContainsKey_Dictionary()
    {
        return new Object[]
        {
            this.dictionary_UsersByGroupId.ContainsKey(1)
        };
    }

    [BenchmarkCategory("NonUniqueIndex_ContainsKey"), Benchmark]
    public Object NonUniqueIndex_ContainsKey_AutoIndexCacheOptimized()
    {
        return new Object[]
        {
            this.autoIndexCache_NonUniqueIndex_UsersByGroupId.ContainsKey(1L)
        };
    }

    [BenchmarkCategory("NonUniqueIndex_ContainsKey"), Benchmark]
    public Object NonUniqueIndex_ContainsKey_AutoIndexCache()
    {
        return new Object[]
        {
            this.autoIndexCache.Items<User>().NonUniqueIndex(a => a.GroupId).ContainsKey(1L)
        };
    }

    [BenchmarkCategory("NonUniqueIndex_GetItems"), Benchmark(Baseline = true)]
    public Object NonUniqueIndex_GetItems_Dictionary()
    {
        return new Object[]
        {
            this.dictionary_UsersByGroupId[1]
        };
    }

    [BenchmarkCategory("NonUniqueIndex_GetItems"), Benchmark]
    public Object NonUniqueIndex_GetItems_AutoIndexCacheOptimized()
    {
        return new Object[]
        {
            this.autoIndexCache_NonUniqueIndex_UsersByGroupId.GetItems(1L)
        };
    }

    [BenchmarkCategory("NonUniqueIndex_GetItems"), Benchmark]
    public Object NonUniqueIndex_GetItems_AutoIndexCache()
    {
        return new Object[]
        {
            this.autoIndexCache.Items<User>().NonUniqueIndex(a => a.GroupId).GetItems(1L)
        };
    }

    [BenchmarkCategory("NonUniqueIndex_GetKeys"), Benchmark(Baseline = true)]
    public Object NonUniqueIndex_GetKeys_Dictionary()
    {
        return new Object[]
        {
            this.dictionary_UsersByGroupId.Keys
        };
    }

    [BenchmarkCategory("NonUniqueIndex_GetKeys"), Benchmark]
    public Object NonUniqueIndex_GetKeys_AutoIndexCacheOptimized()
    {
        return new Object[]
        {
            this.autoIndexCache_NonUniqueIndex_UsersByGroupId.GetKeys()
        };
    }

    [BenchmarkCategory("NonUniqueIndex_GetKeys"), Benchmark]
    public Object NonUniqueIndex_GetKeys_AutoIndexCache()
    {
        return new Object[]
        {
            this.autoIndexCache.Items<User>().NonUniqueIndex(a => a.GroupId).GetKeys()
        };
    }

    [BenchmarkCategory("UniqueIndex_ContainsKey"), Benchmark(Baseline = true)]
    public Object UniqueIndex_ContainsKey_Dictionary()
    {
        return new Object?[]
        {
            this.dictionary_UserById.ContainsKey(1),
            this.dictionary_UserByUserName.ContainsKey("User 1")
        };
    }

    [BenchmarkCategory("UniqueIndex_ContainsKey"), Benchmark]
    public Object UniqueIndex_ContainsKey_AutoIndexCacheOptimized()
    {
        return new Object?[]
        {
            this.autoIndexCache_UniqueIndex_UserById.ContainsKey(1L),
            this.autoIndexCache_UniqueIndex_UserByUserName.ContainsKey("User 1")
        };
    }

    [BenchmarkCategory("UniqueIndex_ContainsKey"), Benchmark]
    public Object UniqueIndex_ContainsKey_AutoIndexCache()
    {
        return new Object?[]
        {
            this.autoIndexCache.Items<User>().UniqueIndex(a => a.Id).ContainsKey(1L),
            this.autoIndexCache.Items<User>().UniqueIndex(a => a.UserName).ContainsKey("User 1")
        };
    }

    [BenchmarkCategory("UniqueIndex_GetItemOrDefault"), Benchmark(Baseline = true)]
    public Object UniqueIndex_GetItemOrDefault_Dictionary()
    {
        return new Object?[]
        {
            this.dictionary_UserById[1L],
            this.dictionary_UserByUserName["User 1"]
        };
    }

    [BenchmarkCategory("UniqueIndex_GetItemOrDefault"), Benchmark]
    public Object UniqueIndex_GetItemOrDefault_AutoIndexCacheOptimized()
    {
        return new Object?[]
        {
            this.autoIndexCache_UniqueIndex_UserById.GetItemOrDefault(1L),
            this.autoIndexCache_UniqueIndex_UserByUserName.GetItemOrDefault("User 1")
        };
    }

    [BenchmarkCategory("UniqueIndex_GetItemOrDefault"), Benchmark]
    public Object UniqueIndex_GetItemOrDefault_AutoIndexCache()
    {
        return new Object?[]
        {
            this.autoIndexCache.Items<User>().UniqueIndex(a => a.Id).GetItemOrDefault(1L),
            this.autoIndexCache.Items<User>().UniqueIndex(a => a.UserName).GetItemOrDefault("User 1")
        };
    }

    [BenchmarkCategory("UniqueIndex_GetKeys"), Benchmark(Baseline = true)]
    public Object UniqueIndex_GetKeys_Dictionary()
    {
        return new Object[]
        {
            this.dictionary_UserById.Keys,
            this.dictionary_UserByUserName.Keys
        };
    }

    [BenchmarkCategory("UniqueIndex_GetKeys"), Benchmark]
    public Object UniqueIndex_GetKeys_AutoIndexCacheOptimized()
    {
        return new Object[]
        {
            this.autoIndexCache_UniqueIndex_UserById.GetKeys(),
            this.autoIndexCache_UniqueIndex_UserByUserName.GetKeys()
        };
    }

    [BenchmarkCategory("UniqueIndex_GetKeys"), Benchmark]
    public Object UniqueIndex_GetKeys_AutoIndexCache()
    {
        return new Object[]
        {
            this.autoIndexCache.Items<User>().UniqueIndex(a => a.Id).GetKeys(),
            this.autoIndexCache.Items<User>().UniqueIndex(a => a.UserName).GetKeys()
        };
    }

    [BenchmarkCategory("Reset"), Benchmark(Baseline = true)]
    public void Reset_Dictionary()
    {
        this.dictionary_UserById.Clear();
        this.dictionary_UserByUserName.Clear();
        this.dictionary_UsersByGroupId.Clear();
    }

    [BenchmarkCategory("Reset"), Benchmark]
    public void Reset_AutoIndexCacheOptimized()
    {
        this.autoIndexCache_ItemsList_Users.Reset();
    }

    [BenchmarkCategory("Reset"), Benchmark]
    public void Reset_AutoIndexCache()
    {
        this.autoIndexCache.Items<User>().Reset();
    }

    private AutoIndexCache autoIndexCache = null!;
    private IItemsList<User> autoIndexCache_ItemsList_Users = null!;
    private INonUniqueIndex<User, Int64> autoIndexCache_NonUniqueIndex_UsersByGroupId = null!;
    private IUniqueIndex<User, Int64> autoIndexCache_UniqueIndex_UserById = null!;
    private IUniqueIndex<User, String> autoIndexCache_UniqueIndex_UserByUserName = null!;
    private Dictionary<Int64, User> dictionary_UserById = null!;
    private Dictionary<String, User> dictionary_UserByUserName = null!;

    private Dictionary<Int64, List<User>> dictionary_UsersByGroupId = null!;

    private User[] users = null!;
}