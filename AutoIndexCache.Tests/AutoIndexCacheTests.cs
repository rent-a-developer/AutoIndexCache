using AutoIndexCache.Exceptions;
using AutoIndexCache.Tests.TestData;
using FakeItEasy;
using FluentAssertions;
using NUnit.Framework;

namespace AutoIndexCache.Tests;

[TestFixture]
public class AutoIndexCacheTests : TestsBase
{
    [Test]
    public void AccessFromMultipleThreadsShouldCauseInvocationOfItemsLoaderOnlyOnce()
    {
        var cache = new AutoIndexCache();

        var loadUsersInvocations = 0;

        var random = new Random();

        User[] LoadUsers()
        {
            Interlocked.Increment(ref loadUsersInvocations);
            Thread.Sleep(random.Next(10, 100));
            return [];
        }

        cache.SetItemsLoader(LoadUsers);

        void ThreadBody()
        {
            Thread.Sleep(random.Next(10, 100));
            cache.Items<User>().GetAllItems();
        }

        var thread1 = new Thread(ThreadBody);
        var thread2 = new Thread(ThreadBody);
        var thread3 = new Thread(ThreadBody);
        var thread4 = new Thread(ThreadBody);
        var thread5 = new Thread(ThreadBody);

        thread1.Start();
        thread2.Start();
        thread3.Start();
        thread4.Start();
        thread5.Start();

        thread1.Join();
        thread2.Join();
        thread3.Join();
        thread4.Join();
        thread5.Join();

        loadUsersInvocations.Should().Be(1);
    }

    [Test]
    public void AccessFromOtherThreadsShouldBeBlockedUntilItemsLoaderHasCompletedLoading()
    {
        var cache = new AutoIndexCache();

        var waitTime = TimeSpan.FromMilliseconds(200);

        User[] LoadUsers()
        {
            Thread.Sleep(waitTime);
            return [];
        }

        cache.SetItemsLoader(LoadUsers);

        var getAllItemsTime = TimeSpan.Zero;
        var uniqueIndexGetItemOrDefaultTime = TimeSpan.Zero;
        var uniqueIndexGetKeysTime = TimeSpan.Zero;
        var nonUniqueIndexGetItemsTime = TimeSpan.Zero;
        var nonUniqueIndexGetKeys = TimeSpan.Zero;

        var thread1 = new Thread(() => { getAllItemsTime = ExecutionTimeOf(() => cache.Items<User>().GetAllItems()); });
        var thread2 = new Thread(() => { uniqueIndexGetItemOrDefaultTime = ExecutionTimeOf(() => cache.Items<User>().UniqueIndex(a => a.Id).GetItemOrDefault(1)); });
        var thread3 = new Thread(() => { uniqueIndexGetKeysTime = ExecutionTimeOf(() => cache.Items<User>().UniqueIndex(a => a.Id).GetKeys()); });
        var thread4 = new Thread(() => { nonUniqueIndexGetItemsTime = ExecutionTimeOf(() => cache.Items<User>().NonUniqueIndex(a => a.GroupId).GetItems(1)); });
        var thread5 = new Thread(() => { nonUniqueIndexGetKeys = ExecutionTimeOf(() => cache.Items<User>().NonUniqueIndex(a => a.GroupId).GetKeys()); });

        thread1.Start();
        thread2.Start();
        thread3.Start();
        thread4.Start();
        thread5.Start();
        thread1.Join();
        thread2.Join();
        thread3.Join();
        thread4.Join();
        thread5.Join();

        getAllItemsTime.Should().BeCloseTo(waitTime, TimeSpan.FromMilliseconds(50));
        uniqueIndexGetItemOrDefaultTime.Should().BeCloseTo(waitTime, TimeSpan.FromMilliseconds(50));
        uniqueIndexGetKeysTime.Should().BeCloseTo(waitTime, TimeSpan.FromMilliseconds(50));
        nonUniqueIndexGetItemsTime.Should().BeCloseTo(waitTime, TimeSpan.FromMilliseconds(50));
        nonUniqueIndexGetKeys.Should().BeCloseTo(waitTime, TimeSpan.FromMilliseconds(50));
    }

    [Test]
    public void AccessingItemsOfTypeBFromItemsLoaderOfItemsOfTypeAShouldNotThrow()
    {
        var cache = new AutoIndexCache();

        var isExceptionThrown = false;

        User[] LoadUsers()
        {
            try
            {
                cache.Items<Group>().GetAllItems();
                cache.Items<Group>().UniqueIndex(a => a.Id).ContainsKey(1);
                cache.Items<Group>().UniqueIndex(a => a.Id).GetItemOrDefault(1);
                cache.Items<Group>().UniqueIndex(a => a.Id).GetKeys();
                cache.Items<Group>().NonUniqueIndex(a => a.Category).ContainsKey("A");
                cache.Items<Group>().NonUniqueIndex(a => a.Category).GetItems("A");
                cache.Items<Group>().NonUniqueIndex(a => a.Category).GetKeys();
            }
            catch (Exception)
            {
                isExceptionThrown = true;
            }

            return [];
        }

        Group[] LoadGroups() => [];

        cache.SetItemsLoader(LoadUsers);
        cache.SetItemsLoader(LoadGroups);

        cache.Invoking(a => a.Items<User>().GetAllItems()).Should().NotThrow();
        isExceptionThrown.Should().BeFalse();
    }

    [Test]
    public void Items_NoItemsLoaderSetForItemsTypeYet_ShouldThrow()
    {
        var cache = new AutoIndexCache();

        cache.Invoking(c => c.Items<User>())
            .Should()
            .Throw<MissingItemsLoaderException>()
            .WithMessage("Cannot get cache items of type 'AutoIndexCache.Tests.TestData.User'. No cache items loader for this cache item type is set on this instance. Use the method AutoIndexCache.SetItemsLoader to set a cache items loader for the cache item type before trying to access the cache items of that type.");
    }

    [Test]
    public void Items_ShouldReturnItems()
    {
        var cache = new AutoIndexCache();
        var userLoader = A.Fake<Func<User[]>>();
        var users = this.TestUsers.Take(1).ToArray();

        cache.SetItemsLoader(userLoader);

        A.CallTo(() => userLoader())
            .Returns(users);

        cache.Items<User>()
            .Should()
            .NotBeNull()
            .And
            .BeOfType<ItemsList<User>>();
    }

    [Test]
    public void SetItemsLoader_CalledAgainWithDifferentItemsLoader_ShouldResetList()
    {
        var cache = new AutoIndexCache();
        var user1 = new User(1, "User 1", 1, true);
        var user2 = new User(2, "User 2", 2, true);

        var userLoader1 = A.Fake<Func<User[]>>();
        A.CallTo(() => userLoader1()).Returns([user1]);

        var userLoader2 = A.Fake<Func<User[]>>();
        A.CallTo(() => userLoader2()).Returns([user2]);

        cache.SetItemsLoader(userLoader1);
        cache.Items<User>().GetAllItems().Should().BeEquivalentTo([user1]);
        cache.Items<User>().UniqueIndex(a => a.Id).ContainsKey(1).Should().BeTrue();
        cache.Items<User>().UniqueIndex(a => a.Id).GetItemOrDefault(1).Should().Be(user1);
        cache.Items<User>().UniqueIndex(a => a.Id).GetKeys().Should().BeEquivalentTo([1]);
        cache.Items<User>().NonUniqueIndex(a => a.GroupId).ContainsKey(1).Should().BeTrue();
        cache.Items<User>().NonUniqueIndex(a => a.GroupId).GetItems(1).Should().BeEquivalentTo([user1]);
        cache.Items<User>().NonUniqueIndex(a => a.GroupId).GetKeys().Should().BeEquivalentTo([1]);

        A.CallTo(() => userLoader1()).MustHaveHappened(1, Times.Exactly);
        A.CallTo(() => userLoader2()).MustHaveHappened(0, Times.Exactly);

        cache.SetItemsLoader(userLoader2);
        cache.Items<User>().GetAllItems().Should().BeEquivalentTo([user2]);
        cache.Items<User>().UniqueIndex(a => a.Id).ContainsKey(2).Should().BeTrue();
        cache.Items<User>().UniqueIndex(a => a.Id).GetItemOrDefault(2).Should().Be(user2);
        cache.Items<User>().UniqueIndex(a => a.Id).GetKeys().Should().BeEquivalentTo([2]);
        cache.Items<User>().NonUniqueIndex(a => a.GroupId).ContainsKey(2).Should().BeTrue();
        cache.Items<User>().NonUniqueIndex(a => a.GroupId).GetItems(2).Should().BeEquivalentTo([user2]);
        cache.Items<User>().NonUniqueIndex(a => a.GroupId).GetKeys().Should().BeEquivalentTo([2]);

        A.CallTo(() => userLoader1()).MustHaveHappened(1, Times.Exactly);
        A.CallTo(() => userLoader2()).MustHaveHappened(1, Times.Exactly);
    }

    [Test]
    public void SetItemsLoader_CalledAgainWithSameItemsLoader_ShouldNotResetList()
    {
        var cache = new AutoIndexCache();
        var user1 = new User(1, "User 1", 1, true);

        var userLoader1 = A.Fake<Func<User[]>>();
        A.CallTo(() => userLoader1()).Returns([user1]);

        cache.SetItemsLoader(userLoader1);
        cache.Items<User>().GetAllItems().Should().BeEquivalentTo([user1]);

        cache.SetItemsLoader(userLoader1);
        cache.Items<User>().GetAllItems().Should().BeEquivalentTo([user1]);

        A.CallTo(() => userLoader1()).MustHaveHappened(1, Times.Exactly);
    }

    [Test]
    public void SetItemsLoader_LoaderIsNull_ShouldThrow()
    {
        var cache = new AutoIndexCache();

        cache.Invoking(c => c.SetItemsLoader<User>(null!))
            .Should()
            .Throw<ArgumentNullException>();
    }

    [Test]
    public void SetItemsLoader_ShouldSetLoader()
    {
        var cache = new AutoIndexCache();
        var userLoader = A.Fake<Func<User[]>>();
        var users = this.TestUsers.Take(1).ToArray();

        cache.SetItemsLoader(userLoader);

        A.CallTo(() => userLoader())
            .Returns(users);

        cache.Items<User>().GetAllItems()
            .Should()
            .BeEquivalentTo(users);

        A.CallTo(() => userLoader())
            .MustHaveHappened(1, Times.Exactly);
    }
}
