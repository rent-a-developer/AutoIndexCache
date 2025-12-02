[![NuGet Version](https://img.shields.io/nuget/v/RentADeveloper.AutoIndexCache)](https://www.nuget.org/packages/RentADeveloper.AutoIndexCache/)
[![license](https://img.shields.io/badge/License-MIT-purple.svg)](LICENSE.md)
![semver](https://img.shields.io/badge/semver-1.3.0-blue)

# AutoIndexCache
A high-performance, thread-safe cache for .NET applications that provides automatic indexing of cached data.

```csharp
using System;
using RentADeveloper.AutoIndexCache;

public class User
{
    public Int64 GroupId { get; set; }
    public Int64 Id { get; set; }
    public Boolean IsActive { get; set; }
    public String UserName { get; set; }
}

public class Program
{
    public static void Main(String[] args)
    {
        var cache = new AutoIndexCache();

        cache.SetItemsLoader(() => LoadUsers());

        // Get all users:
        cache.Items<User>().GetAllItems();

        // Get the user that has the Id 1:
        cache.Items<User>().UniqueIndex(a => a.Id).GetItemOrDefault(1);

        // Get all users that belong to group 1:
        cache.Items<User>().NonUniqueIndex(a => a.GroupId).GetItems(1);

        // Get all active users of group 10:
        cache.Items<User>().NonUniqueIndex(a => (a.IsActive, a.GroupId)).GetItems((true, 10));

        // Get all Ids of users:
        cache.Items<User>().UniqueIndex(a => a.Id).GetKeys();

        // Reset the users cache (LoadUsers will be called again next time User items are requested from the cache):
        cache.Items<User>().Reset();
    }

    private static User[] LoadUsers()
    {
        var result = new User[10_000];

        for (var i = 1; i <= result.Length; i++)
        {
            result[i - 1] = new() { Id = i, UserName = "User " + i, GroupId = i % 2 == 0 ? 2 : 1, IsActive = i % 2 == 0 };
        }

        return result;
    }
}
```

# Performance
AutoIndexCache is blazingly fast (as a cache should be).  
However, some overhead is still needed to make it thread-safe and for the auto indexing, so a hand crafted custom solution might be even faster.

# Benchmarks
In the following benchmarks AutoIndexCache is compared to the [Dictionary<TKey,TValue>](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2?view=net-9.0) type.  
You can find the source code of the bechmarks [here](https://github.com/rent-a-developer/AutoIndexCache/tree/main/AutoIndexCache.Benchmarks).

<img width="3054" height="1276" alt="image" src="https://github.com/user-attachments/assets/55da6363-5ea1-4445-8c5a-5db3b3bc6c39" />

## Explanation
### ..._Dicationary
In these benchmarks the [Dictionary<TKey,TValue>](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2?view=net-9.0) is used to index cached items.  
There is hardly any faster soltuion in .NET to index cached items.  
However, the Dictionary is completely non-thread-safe.  

### ..._AutoIndexCache
In these benchmarks the AutoIndexCache is used.

### ..._AutoIndexCacheOptimized
In these bechmarks, also, the AutoIndexCache is used.  
However, a trick was applied to drastically improve the performance:  

To access cached items you need to call the AutoIndexCache.Items<TItem> method.  
And to access indexes you need to call the ItemsList<TItem>.NonUniqueIndex<TKey> and ItemsList<TItem>.UniqueIndex<TKey> methods.  
These methods have some overhead, because they need to be thread-safe.

Instead of calling these method each time you want to access cached items or indexes, you can just call them once and store their return values in fields (so in essence, you are caching the cache :smiley:).  
This way the performance of the cache is improved drastically.

So instead of this:
```csharp
class Service
{
    public Service(IAutoIndexCache cache)
    {
        this.cache = cache;
    }

    public User? GetUserById(Int64 id)
    {
        return this.cache.Items<User>().UniqueIndex(a => a.Id).GetItemOrDefault(id);
    }

    private readonly IAutoIndexCache cache;
}
```

you do this:
```csharp
class Service
{
    public Service(IAutoIndexCache cache)
    {
        this.cache = cache;
        this.users = this.cache.Items<User>();
        this.userById = this.users.UniqueIndex(a => a.Id);
    }

    public User? GetUserById(Int64 id)
    {
        return this.userById.GetItemOrDefault(id);
    }

    private readonly IAutoIndexCache cache;
    private readonly IUniqueIndex<User, Int64> userById;
    private readonly IItemsList<User> users;
}
```

# License
This library is licensed under the [MIT license](LICENSE.md).

# Installation
First, [install NuGet](http://docs.nuget.org/docs/start-here/installing-nuget).

Then install the [NuGet package](https://www.nuget.org/packages/RentADeveloper.AutoIndexCache/) from the package manager console:
```shell
PM> Install-Package RentADeveloper.AutoIndexCache
```

# Documentation

The API documentation can be found [here](https://rent-a-developer.github.io/AutoIndexCache/api/RentADeveloper.AutoIndexCache.html).

# Contributors

## Main contributors
- David Liebeherr (info@rent-a-developer.de)
