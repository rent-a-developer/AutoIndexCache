using System.Data;
using AutoIndexCache.Exceptions;
using AutoIndexCache.Tests.TestData;
using FakeItEasy;
using FluentAssertions;
using NUnit.Framework;

namespace AutoIndexCache.Tests;

[TestFixture]
public class ItemsListTests : TestsBase
{
    [Test]
    public void ForceLoadItems_AccessFromItemsLoaderOfSameItemsType_ShouldThrow()
    {
        var cache = new AutoIndexCache();

        Exception? thrownException = null;

        User[] LoadUsers()
        {
            try
            {
                cache.Items<User>().ForceLoadItems();
            }
            catch (Exception ex)
            {
                thrownException = ex;
                throw;
            }

            return [];
        }

        cache.SetItemsLoader(LoadUsers);

        cache.Invoking(a => a.Items<User>().ForceLoadItems())
            .Should()
            .Throw<ItemsLoaderFailedException>()
            .WithMessage("Could not load the cache items of the type 'AutoIndexCache.Tests.TestData.User'. The cache items loader for that cache item type threw an exception. See the inner exception for details.")
            .WithInnerException<ItemsAccessedFromInsideItemsLoaderException>()
            .WithMessage("Cannot access the cache items of the type 'AutoIndexCache.Tests.TestData.User'. The current thread is inside the cache items loader for that cache item type.");

        thrownException.Should().BeOfType<ItemsAccessedFromInsideItemsLoaderException>();
        ((ItemsAccessedFromInsideItemsLoaderException)thrownException).Message.Should().Be("Cannot access the cache items of the type 'AutoIndexCache.Tests.TestData.User'. The current thread is inside the cache items loader for that cache item type.");
    }

    [Test]
    public void ForceLoadItems_CyclicAccessFromItemsLoader_ShouldThrow()
    {
        var cache = new AutoIndexCache();

        Exception? loadUsersThrownException = null;
        Exception? loadGroupsThrownException = null;

        User[] LoadUsers()
        {
            try
            {
                cache.Items<Group>().ForceLoadItems();
            }
            catch (Exception ex)
            {
                loadUsersThrownException = ex;
                throw;
            }

            return [];
        }

        Group[] LoadGroups()
        {
            try
            {
                cache.Items<User>().ForceLoadItems();
            }
            catch (Exception ex)
            {
                loadGroupsThrownException = ex;
                throw;
            }

            return [];
        }

        cache.SetItemsLoader(LoadUsers);
        cache.SetItemsLoader(LoadGroups);

        cache.Invoking(a => a.Items<User>().ForceLoadItems())
            .Should()
            .Throw<ItemsLoaderFailedException>()
            .WithMessage("Could not load the cache items of the type 'AutoIndexCache.Tests.TestData.User'. The cache items loader for that cache item type threw an exception. See the inner exception for details.")
            .WithInnerException<ItemsLoaderFailedException>()
            .WithMessage("Could not load the cache items of the type 'AutoIndexCache.Tests.TestData.Group'. The cache items loader for that cache item type threw an exception. See the inner exception for details.")
            .WithInnerException<ItemsAccessedFromInsideItemsLoaderException>()
            .WithMessage("Cannot access the cache items of the type 'AutoIndexCache.Tests.TestData.User'. The current thread is inside the cache items loader for that cache item type.");

        cache.Invoking(a => a.Items<Group>().ForceLoadItems())
            .Should()
            .Throw<ItemsLoaderFailedException>()
            .WithMessage("Could not load the cache items of the type 'AutoIndexCache.Tests.TestData.Group'. The cache items loader for that cache item type threw an exception. See the inner exception for details.")
            .WithInnerException<ItemsLoaderFailedException>()
            .WithMessage("Could not load the cache items of the type 'AutoIndexCache.Tests.TestData.User'. The cache items loader for that cache item type threw an exception. See the inner exception for details.")
            .WithInnerException<ItemsAccessedFromInsideItemsLoaderException>()
            .WithMessage("Cannot access the cache items of the type 'AutoIndexCache.Tests.TestData.Group'. The current thread is inside the cache items loader for that cache item type.");

        loadUsersThrownException.Should().BeOfType<ItemsAccessedFromInsideItemsLoaderException>();
        ((ItemsAccessedFromInsideItemsLoaderException)loadUsersThrownException).Message.Should().Be("Cannot access the cache items of the type 'AutoIndexCache.Tests.TestData.Group'. The current thread is inside the cache items loader for that cache item type.");

        loadGroupsThrownException.Should().BeOfType<ItemsLoaderFailedException>();
        ((ItemsLoaderFailedException)loadGroupsThrownException).Message.Should().Be("Could not load the cache items of the type 'AutoIndexCache.Tests.TestData.User'. The cache items loader for that cache item type threw an exception. See the inner exception for details.");
        ((ItemsLoaderFailedException)loadGroupsThrownException).InnerException.Should().BeOfType<ItemsAccessedFromInsideItemsLoaderException>();
        ((ItemsLoaderFailedException)loadGroupsThrownException).InnerException?.Message.Should().Be("Cannot access the cache items of the type 'AutoIndexCache.Tests.TestData.Group'. The current thread is inside the cache items loader for that cache item type.");
    }

    [Test]
    public void ForceLoadItems_ItemsLoaderReturnsNull_ShouldThrow()
    {
        var cache = new AutoIndexCache();
        var userLoader = A.Fake<Func<User[]>>();

        cache.SetItemsLoader(userLoader);

        A.CallTo(() => userLoader())
            .Returns(null!);

        cache.Invoking(c => c.Items<User>().ForceLoadItems())
            .Should()
            .Throw<ItemsLoaderReturnedNullException>()
            .WithMessage("Could not load the cache items of the type 'AutoIndexCache.Tests.TestData.User'. The cache items loader for that cache item type returned a null reference. It must return a list of cache items instead.");
    }

    [Test]
    public void ForceLoadItems_ItemsLoaderThrows_ShouldThrow()
    {
        var cache = new AutoIndexCache();

        cache.SetItemsLoader<User>(() => throw new DataException("Test Items Loader Exception"));

        cache.Invoking(a => a.Items<User>().ForceLoadItems())
            .Should()
            .Throw<ItemsLoaderFailedException>()
            .WithMessage("Could not load the cache items of the type 'AutoIndexCache.Tests.TestData.User'. The cache items loader for that cache item type threw an exception. See the inner exception for details.")
            .WithInnerException<DataException>()
            .WithMessage("Test Items Loader Exception");
    }

    [Test]
    public void ForceLoadItems_ShouldLoadItemsImmediately()
    {
        var cache = new AutoIndexCache();
        var userLoader = A.Fake<Func<User[]>>();
        var users = this.TestUsers.Take(10).ToArray();

        cache.SetItemsLoader(userLoader);

        A.CallTo(() => userLoader())
            .Returns(users);

        cache.Items<User>().ForceLoadItems();

        A.CallTo(() => userLoader())
            .MustHaveHappened(1, Times.Exactly);
    }

    [Test]
    public void ForceLoadItems_ShouldLoadItemsOnlyOnce()
    {
        var cache = new AutoIndexCache();
        var userLoader = A.Fake<Func<User[]>>();
        var users = this.TestUsers.Take(1).ToArray();

        cache.SetItemsLoader(userLoader);

        A.CallTo(() => userLoader())
            .Returns(users);

        cache.Items<User>().ForceLoadItems();
        cache.Items<User>().ForceLoadItems();

        A.CallTo(() => userLoader())
            .MustHaveHappened(1, Times.Exactly);
    }

    [Test]
    public void GetAllItems_AccessFromItemsLoaderOfSameItemsType_ShouldThrow()
    {
        var cache = new AutoIndexCache();

        Exception? thrownException = null;

        User[] LoadUsers()
        {
            try
            {
                cache.Items<User>().GetAllItems();
            }
            catch (Exception ex)
            {
                thrownException = ex;
                throw;
            }

            return [];
        }

        cache.SetItemsLoader(LoadUsers);

        cache.Invoking(a => a.Items<User>().GetAllItems())
            .Should()
            .Throw<ItemsLoaderFailedException>()
            .WithMessage("Could not load the cache items of the type 'AutoIndexCache.Tests.TestData.User'. The cache items loader for that cache item type threw an exception. See the inner exception for details.")
            .WithInnerException<ItemsAccessedFromInsideItemsLoaderException>()
            .WithMessage("Cannot access the cache items of the type 'AutoIndexCache.Tests.TestData.User'. The current thread is inside the cache items loader for that cache item type.");

        thrownException.Should().BeOfType<ItemsAccessedFromInsideItemsLoaderException>();
        ((ItemsAccessedFromInsideItemsLoaderException)thrownException).Message.Should().Be("Cannot access the cache items of the type 'AutoIndexCache.Tests.TestData.User'. The current thread is inside the cache items loader for that cache item type.");
    }

    [Test]
    public void GetAllItems_CyclicAccessFromItemsLoader_ShouldThrow()
    {
        var cache = new AutoIndexCache();

        Exception? loadUsersThrownException = null;
        Exception? loadGroupsThrownException = null;

        User[] LoadUsers()
        {
            try
            {
                cache.Items<Group>().GetAllItems();
            }
            catch (Exception ex)
            {
                loadUsersThrownException = ex;
                throw;
            }

            return [];
        }

        Group[] LoadGroups()
        {
            try
            {
                cache.Items<User>().GetAllItems();
            }
            catch (Exception ex)
            {
                loadGroupsThrownException = ex;
                throw;
            }

            return [];
        }

        cache.SetItemsLoader(LoadUsers);
        cache.SetItemsLoader(LoadGroups);

        cache.Invoking(a => a.Items<User>().GetAllItems())
            .Should()
            .Throw<ItemsLoaderFailedException>()
            .WithMessage("Could not load the cache items of the type 'AutoIndexCache.Tests.TestData.User'. The cache items loader for that cache item type threw an exception. See the inner exception for details.")
            .WithInnerException<ItemsLoaderFailedException>()
            .WithMessage("Could not load the cache items of the type 'AutoIndexCache.Tests.TestData.Group'. The cache items loader for that cache item type threw an exception. See the inner exception for details.")
            .WithInnerException<ItemsAccessedFromInsideItemsLoaderException>()
            .WithMessage("Cannot access the cache items of the type 'AutoIndexCache.Tests.TestData.User'. The current thread is inside the cache items loader for that cache item type.");

        cache.Invoking(a => a.Items<Group>().GetAllItems())
            .Should()
            .Throw<ItemsLoaderFailedException>()
            .WithMessage("Could not load the cache items of the type 'AutoIndexCache.Tests.TestData.Group'. The cache items loader for that cache item type threw an exception. See the inner exception for details.")
            .WithInnerException<ItemsLoaderFailedException>()
            .WithMessage("Could not load the cache items of the type 'AutoIndexCache.Tests.TestData.User'. The cache items loader for that cache item type threw an exception. See the inner exception for details.")
            .WithInnerException<ItemsAccessedFromInsideItemsLoaderException>()
            .WithMessage("Cannot access the cache items of the type 'AutoIndexCache.Tests.TestData.Group'. The current thread is inside the cache items loader for that cache item type.");

        loadUsersThrownException.Should().BeOfType<ItemsAccessedFromInsideItemsLoaderException>();
        ((ItemsAccessedFromInsideItemsLoaderException)loadUsersThrownException).Message.Should().Be("Cannot access the cache items of the type 'AutoIndexCache.Tests.TestData.Group'. The current thread is inside the cache items loader for that cache item type.");

        loadGroupsThrownException.Should().BeOfType<ItemsLoaderFailedException>();
        ((ItemsLoaderFailedException)loadGroupsThrownException).Message.Should().Be("Could not load the cache items of the type 'AutoIndexCache.Tests.TestData.User'. The cache items loader for that cache item type threw an exception. See the inner exception for details.");
        ((ItemsLoaderFailedException)loadGroupsThrownException).InnerException.Should().BeOfType<ItemsAccessedFromInsideItemsLoaderException>();
        ((ItemsLoaderFailedException)loadGroupsThrownException).InnerException?.Message.Should().Be("Cannot access the cache items of the type 'AutoIndexCache.Tests.TestData.Group'. The current thread is inside the cache items loader for that cache item type.");
    }

    [Test]
    public void GetAllItems_ItemsLoaderReturnsNull_ShouldThrow()
    {
        var cache = new AutoIndexCache();
        var userLoader = A.Fake<Func<User[]>>();

        cache.SetItemsLoader(userLoader);

        A.CallTo(() => userLoader())
            .Returns(null!);

        cache.Invoking(c => c.Items<User>().GetAllItems())
            .Should()
            .Throw<ItemsLoaderReturnedNullException>()
            .WithMessage("Could not load the cache items of the type 'AutoIndexCache.Tests.TestData.User'. The cache items loader for that cache item type returned a null reference. It must return a list of cache items instead.");
    }

    [Test]
    public void GetAllItems_ItemsLoaderThrows_ShouldThrow()
    {
        var cache = new AutoIndexCache();

        cache.SetItemsLoader<User>(() => throw new DataException("Test Items Loader Exception"));

        cache.Invoking(a => a.Items<User>().GetAllItems())
            .Should()
            .Throw<ItemsLoaderFailedException>()
            .WithMessage("Could not load the cache items of the type 'AutoIndexCache.Tests.TestData.User'. The cache items loader for that cache item type threw an exception. See the inner exception for details.")
            .WithInnerException<DataException>()
            .WithMessage("Test Items Loader Exception");
    }

    [Test]
    public void GetAllItems_ShouldLoadItemsOnlyOnce()
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

        cache.Items<User>().GetAllItems()
            .Should()
            .BeEquivalentTo(users);

        A.CallTo(() => userLoader())
            .MustHaveHappened(1, Times.Exactly);
    }

    [Test]
    public void GetAllItems_ShouldReturnAllItems()
    {
        var cache = new AutoIndexCache();
        var userLoader = A.Fake<Func<User[]>>();
        var users = this.TestUsers.Take(10).ToArray();

        cache.SetItemsLoader(userLoader);

        A.CallTo(() => userLoader())
            .Returns(users);

        cache.Items<User>().GetAllItems()
            .Should()
            .BeEquivalentTo(users);
    }

    [Test]
    public void NonUniqueIndex_ShouldReturnNonUniqueIndex()
    {
        var cache = new AutoIndexCache();
        var userLoader = A.Fake<Func<User[]>>();
        var users = this.TestUsers.Take(1).ToArray();

        cache.SetItemsLoader(userLoader);

        A.CallTo(() => userLoader())
            .Returns(users);

        cache.Items<User>().NonUniqueIndex(user => user.GroupId)
            .Should()
            .NotBeNull()
            .And
            .BeOfType<NonUniqueIndex<User, Int64>>();

        cache.Items<User>().NonUniqueIndex(user => user.IsActive)
            .Should()
            .NotBeNull()
            .And
            .BeOfType<NonUniqueIndex<User, Boolean>>();
    }

    [Test]
    public void Reset_AccessFromItemsLoaderOfSameItemsType_ShouldThrow()
    {
        var cache = new AutoIndexCache();

        Exception? thrownException = null;

        User[] LoadUsers()
        {
            try
            {
                cache.Items<User>().Reset();
            }
            catch (Exception ex)
            {
                thrownException = ex;
                throw;
            }

            return [];
        }

        cache.SetItemsLoader(LoadUsers);

        cache.Invoking(a => a.Items<User>().GetAllItems())
            .Should()
            .Throw<ItemsLoaderFailedException>()
            .WithMessage("Could not load the cache items of the type 'AutoIndexCache.Tests.TestData.User'. The cache items loader for that cache item type threw an exception. See the inner exception for details.")
            .WithInnerException<ItemsAccessedFromInsideItemsLoaderException>()
            .WithMessage("Cannot reset the cache items of the type 'AutoIndexCache.Tests.TestData.User'. The current thread is inside the cache items loader for that cache item type.");

        thrownException.Should().BeOfType<ItemsAccessedFromInsideItemsLoaderException>();
        ((ItemsAccessedFromInsideItemsLoaderException)thrownException).Message.Should().Be("Cannot reset the cache items of the type 'AutoIndexCache.Tests.TestData.User'. The current thread is inside the cache items loader for that cache item type.");
    }

    [Test]
    public void Reset_ShouldResetList()
    {
        var cache = new AutoIndexCache();
        var user1 = new User(1, "User 1", 1, true);
        var user2 = new User(2, "User 2", 2, true);

        var usersLoader = A.Fake<Func<User[]>>();
        cache.SetItemsLoader(usersLoader);

        A.CallTo(() => usersLoader()).Returns([user1]);

        cache.Items<User>().GetAllItems().Should().BeEquivalentTo([user1]);
        cache.Items<User>().UniqueIndex(a => a.Id).ContainsKey(1).Should().BeTrue();
        cache.Items<User>().UniqueIndex(a => a.Id).GetItemOrDefault(1).Should().Be(user1);
        cache.Items<User>().UniqueIndex(a => a.Id).GetKeys().Should().BeEquivalentTo([1]);
        cache.Items<User>().NonUniqueIndex(a => a.GroupId).ContainsKey(1).Should().BeTrue();
        cache.Items<User>().NonUniqueIndex(a => a.GroupId).GetItems(1).Should().BeEquivalentTo([user1]);
        cache.Items<User>().NonUniqueIndex(a => a.GroupId).GetKeys().Should().BeEquivalentTo([1]);

        A.CallTo(() => usersLoader()).MustHaveHappened(1, Times.Exactly);

        A.CallTo(() => usersLoader()).Returns([user2]);

        cache.Items<User>().Reset();

        cache.Items<User>().GetAllItems().Should().BeEquivalentTo([user2]);
        cache.Items<User>().UniqueIndex(a => a.Id).ContainsKey(2).Should().BeTrue();
        cache.Items<User>().UniqueIndex(a => a.Id).GetItemOrDefault(2).Should().Be(user2);
        cache.Items<User>().UniqueIndex(a => a.Id).GetKeys().Should().BeEquivalentTo([2]);
        cache.Items<User>().NonUniqueIndex(a => a.GroupId).ContainsKey(2).Should().BeTrue();
        cache.Items<User>().NonUniqueIndex(a => a.GroupId).GetItems(2).Should().BeEquivalentTo([user2]);
        cache.Items<User>().NonUniqueIndex(a => a.GroupId).GetKeys().Should().BeEquivalentTo([2]);

        A.CallTo(() => usersLoader()).MustHaveHappened(2, Times.Exactly);
    }

    [Test]
    public void UniqueIndex_ShouldReturnUniqueIndex()
    {
        var cache = new AutoIndexCache();
        var userLoader = A.Fake<Func<User[]>>();
        var users = this.TestUsers.Take(1).ToArray();

        cache.SetItemsLoader(userLoader);

        A.CallTo(() => userLoader())
            .Returns(users);

        cache.Items<User>().UniqueIndex(user => user.Id)
            .Should()
            .NotBeNull()
            .And
            .BeOfType<UniqueIndex<User, Int64>>();

        cache.Items<User>().UniqueIndex(user => user.UserName)
            .Should()
            .NotBeNull()
            .And
            .BeOfType<UniqueIndex<User, String>>();
    }
}
