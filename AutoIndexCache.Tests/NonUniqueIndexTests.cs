using AutoIndexCache.Tests.TestData;
using FakeItEasy;
using FluentAssertions;
using NUnit.Framework;

// ReSharper disable ReturnValueOfPureMethodIsNotUsed
#pragma warning disable CA1806
#pragma warning disable CS8621
#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.

namespace AutoIndexCache.Tests;

[TestFixture]
public class NonUniqueIndexTests : TestsBase
{
    [Test]
    public void ContainsKey_AccessFromItemsLoaderOfSameItemsType_ShouldThrow()
    {
        var cache = new AutoIndexCache();

        Exception? thrownException = null;

        User[] LoadUsers()
        {
            try
            {
                cache.Items<User>().NonUniqueIndex(a => a.GroupId).ContainsKey(1);
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
    public void ContainsKey_CyclicAccessFromItemsLoader_ShouldThrow()
    {
        var cache = new AutoIndexCache();

        Exception? loadUsersThrownException = null;
        Exception? loadGroupsThrownException = null;

        User[] LoadUsers()
        {
            try
            {
                cache.Items<Group>().NonUniqueIndex(a => a.Category).ContainsKey("A");
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
                cache.Items<User>().NonUniqueIndex(a => a.GroupId).ContainsKey(1);
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
            .WithInnerException<ItemsAccessedFromInsideItemsLoaderException>()
            .WithMessage("Cannot access the cache items of the type 'AutoIndexCache.Tests.TestData.User'. The current thread is inside the cache items loader for that cache item type.");

        loadUsersThrownException.Should().BeOfType<ItemsLoaderFailedException>();
        ((ItemsLoaderFailedException)loadUsersThrownException).Message.Should().Be("Could not load the cache items of the type 'AutoIndexCache.Tests.TestData.Group'. The cache items loader for that cache item type threw an exception. See the inner exception for details.");

        loadGroupsThrownException.Should().BeOfType<ItemsAccessedFromInsideItemsLoaderException>();
        ((ItemsAccessedFromInsideItemsLoaderException)loadGroupsThrownException).Message.Should().Be("Cannot access the cache items of the type 'AutoIndexCache.Tests.TestData.User'. The current thread is inside the cache items loader for that cache item type.");
    }

    [Test]
    public void ContainsKey_NullableObjectKey_ShouldHandleKey()
    {
        var cache = new AutoIndexCache();
        var userLoader = A.Fake<Func<User[]>>();
        var users = new User[]
        {
            new(1, "User 1", 1, false) { NullableObject = null, NullableValue = null },
            new(1, "User 1", 1, false) { NullableObject = "", NullableValue = 1 }
        };

        cache.SetItemsLoader(userLoader);

        A.CallTo(() => userLoader())
            .Returns(users);

        cache.Items<User>().NonUniqueIndex(user => user.NullableObject).ContainsKey(null)
            .Should()
            .BeTrue();

        cache.Items<User>().NonUniqueIndex(user => user.NullableObject).ContainsKey("")
            .Should()
            .BeTrue();
    }

    [Test]
    public void ContainsKey_NullableValueKey_ShouldHandleKey()
    {
        var cache = new AutoIndexCache();
        var userLoader = A.Fake<Func<User[]>>();
        var users = new User[]
        {
            new(1, "User 1", 1, false) { NullableObject = null, NullableValue = null },
            new(1, "User 1", 1, false) { NullableObject = "", NullableValue = 1 }
        };

        cache.SetItemsLoader(userLoader);

        A.CallTo(() => userLoader())
            .Returns(users);

        cache.Items<User>().NonUniqueIndex(user => user.NullableValue).ContainsKey(null)
            .Should()
            .BeTrue();

        cache.Items<User>().NonUniqueIndex(user => user.NullableValue).ContainsKey(1)
            .Should()
            .BeTrue();
    }

    [Test]
    public void ContainsKey_ShouldReturnWhetherKeyExists()
    {
        var cache = new AutoIndexCache();
        var userLoader = A.Fake<Func<User[]>>();
        var users = this.TestUsers;

        cache.SetItemsLoader(userLoader);

        A.CallTo(() => userLoader())
            .Returns(users);

        cache.Items<User>().NonUniqueIndex(a => new { a.GroupId, a.IsActive }).ContainsKey(new { this.FirstActiveTestUser.GroupId, this.FirstActiveTestUser.IsActive })
            .Should()
            .BeTrue();

        cache.Items<User>().NonUniqueIndex(a => new { a.GroupId, a.IsActive }).ContainsKey(new { GroupId = -1L, IsActive = false })
            .Should()
            .BeFalse();
    }

    [Test]
    public void GetItems_AccessFromItemsLoaderOfSameItemsType_ShouldThrow()
    {
        var cache = new AutoIndexCache();

        Exception? thrownException = null;

        User[] LoadUsers()
        {
            try
            {
                cache.Items<User>().NonUniqueIndex(a => a.GroupId).GetItems(1);
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
    public void GetItems_CyclicAccessFromItemsLoader_ShouldThrow()
    {
        var cache = new AutoIndexCache();

        Exception? loadUsersThrownException = null;
        Exception? loadGroupsThrownException = null;

        User[] LoadUsers()
        {
            try
            {
                cache.Items<Group>().NonUniqueIndex(a => a.Category).GetItems("A");
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
                cache.Items<User>().NonUniqueIndex(a => a.GroupId).GetItems(1);
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
            .WithInnerException<ItemsAccessedFromInsideItemsLoaderException>()
            .WithMessage("Cannot access the cache items of the type 'AutoIndexCache.Tests.TestData.User'. The current thread is inside the cache items loader for that cache item type.");

        loadUsersThrownException.Should().BeOfType<ItemsLoaderFailedException>();
        ((ItemsLoaderFailedException)loadUsersThrownException).Message.Should().Be("Could not load the cache items of the type 'AutoIndexCache.Tests.TestData.Group'. The cache items loader for that cache item type threw an exception. See the inner exception for details.");

        loadGroupsThrownException.Should().BeOfType<ItemsAccessedFromInsideItemsLoaderException>();
        ((ItemsAccessedFromInsideItemsLoaderException)loadGroupsThrownException).Message.Should().Be("Cannot access the cache items of the type 'AutoIndexCache.Tests.TestData.User'. The current thread is inside the cache items loader for that cache item type.");
    }

    [Test]
    public void GetItems_NullableObjectKey_ShouldHandleKey()
    {
        var cache = new AutoIndexCache();
        var userLoader = A.Fake<Func<User[]>>();
        var users = new User[]
        {
            new(1, "User 1", 1, false) { NullableObject = null, NullableValue = null },
            new(1, "User 1", 1, false) { NullableObject = "", NullableValue = 1 }
        };

        cache.SetItemsLoader(userLoader);

        A.CallTo(() => userLoader())
            .Returns(users);

        cache.Items<User>().NonUniqueIndex(user => user.NullableObject).GetItems(null)
            .Should()
            .BeEquivalentTo([users[0]]);

        cache.Items<User>().NonUniqueIndex(user => user.NullableObject).GetItems("")
            .Should()
            .BeEquivalentTo([users[1]]);
    }

    [Test]
    public void GetItems_NullableValueKey_ShouldHandleKey()
    {
        var cache = new AutoIndexCache();
        var userLoader = A.Fake<Func<User[]>>();
        var users = new User[]
        {
            new(1, "User 1", 1, false) { NullableObject = null, NullableValue = null },
            new(1, "User 1", 1, false) { NullableObject = "", NullableValue = 1 }
        };

        cache.SetItemsLoader(userLoader);

        A.CallTo(() => userLoader())
            .Returns(users);

        cache.Items<User>().NonUniqueIndex(user => user.NullableValue).GetItems(null)
            .Should()
            .BeEquivalentTo([users[0]]);

        cache.Items<User>().NonUniqueIndex(user => user.NullableValue).GetItems(1)
            .Should()
            .BeEquivalentTo([users[1]]);
    }

    [Test]
    public void GetItems_ShouldReturnEmptyListIfNoMatchingItemsAreFound()
    {
        var cache = new AutoIndexCache();
        var userLoader = A.Fake<Func<User[]>>();
        var users = this.TestUsers;

        cache.SetItemsLoader(userLoader);

        A.CallTo(() => userLoader())
            .Returns(users);

        cache.Items<User>().NonUniqueIndex(user => new { user.GroupId, user.IsActive }).GetItems(new { GroupId = 99999999999L, IsActive = true })
            .Should()
            .BeEmpty();
    }

    [Test]
    public void GetItems_ShouldReturnMatchingItems()
    {
        var cache = new AutoIndexCache();
        var userLoader = A.Fake<Func<User[]>>();
        var users = this.TestUsers;

        cache.SetItemsLoader(userLoader);

        A.CallTo(() => userLoader())
            .Returns(users);

        cache.Items<User>().NonUniqueIndex(user => user.GroupId).GetItems(1)
            .Should()
            .BeEquivalentTo(this.TestUsers.Where(a => a.GroupId == 1));

        cache.Items<User>().NonUniqueIndex(user => user.GroupId).GetItems(2)
            .Should()
            .BeEquivalentTo(this.TestUsers.Where(a => a.GroupId == 2));


        cache.Items<User>().NonUniqueIndex(user => new { user.GroupId, user.IsActive }).GetItems(new { GroupId = 1L, IsActive = true })
            .Should()
            .BeEquivalentTo(this.TestUsers.Where(a => a is { GroupId: 1, IsActive: true }));

        cache.Items<User>().NonUniqueIndex(user => new { user.GroupId, user.IsActive }).GetItems(new { GroupId = 2L, IsActive = true })
            .Should()
            .BeEquivalentTo(this.TestUsers.Where(a => a is { GroupId: 2, IsActive: true }));

        cache.Items<User>().NonUniqueIndex(user => new { user.GroupId, user.IsActive }).GetItems(new { GroupId = 3L, IsActive = true })
            .Should()
            .BeEquivalentTo(this.TestUsers.Where(a => a is { GroupId: 3, IsActive: true }));
    }

    [Test]
    public void GetKeys_AccessFromItemsLoaderOfSameItemsType_ShouldThrow()
    {
        var cache = new AutoIndexCache();

        Exception? thrownException = null;

        User[] LoadUsers()
        {
            try
            {
                cache.Items<User>().NonUniqueIndex(a => a.GroupId).GetKeys();
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
    public void GetKeys_CyclicAccessFromItemsLoader_ShouldThrow()
    {
        var cache = new AutoIndexCache();

        Exception? loadUsersThrownException = null;
        Exception? loadGroupsThrownException = null;

        User[] LoadUsers()
        {
            try
            {
                cache.Items<Group>().NonUniqueIndex(a => a.Category).GetKeys();
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
                cache.Items<User>().NonUniqueIndex(a => a.GroupId).GetKeys();
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
            .WithInnerException<ItemsAccessedFromInsideItemsLoaderException>()
            .WithMessage("Cannot access the cache items of the type 'AutoIndexCache.Tests.TestData.User'. The current thread is inside the cache items loader for that cache item type.");

        loadUsersThrownException.Should().BeOfType<ItemsLoaderFailedException>();
        ((ItemsLoaderFailedException)loadUsersThrownException).Message.Should().Be("Could not load the cache items of the type 'AutoIndexCache.Tests.TestData.Group'. The cache items loader for that cache item type threw an exception. See the inner exception for details.");

        loadGroupsThrownException.Should().BeOfType<ItemsAccessedFromInsideItemsLoaderException>();
        ((ItemsAccessedFromInsideItemsLoaderException)loadGroupsThrownException).Message.Should().Be("Cannot access the cache items of the type 'AutoIndexCache.Tests.TestData.User'. The current thread is inside the cache items loader for that cache item type.");
    }

    [Test]
    public void GetKeys_NullableObjectKey_ShouldHandleKey()
    {
        var cache = new AutoIndexCache();
        var userLoader = A.Fake<Func<User[]>>();
        var users = new User[]
        {
            new(1, "User 1", 1, false) { NullableObject = null, NullableValue = null },
            new(1, "User 1", 1, false) { NullableObject = "", NullableValue = 1 }
        };

        cache.SetItemsLoader(userLoader);

        A.CallTo(() => userLoader())
            .Returns(users);

        cache.Items<User>().NonUniqueIndex(user => user.NullableObject).GetKeys()
            .Should()
            .BeEquivalentTo([null, ""]);
    }

    [Test]
    public void GetKeys_NullableValueKey_ShouldHandleKey()
    {
        var cache = new AutoIndexCache();
        var userLoader = A.Fake<Func<User[]>>();
        var users = new User[]
        {
            new(1, "User 1", 1, false) { NullableObject = null, NullableValue = null },
            new(1, "User 1", 1, false) { NullableObject = "", NullableValue = 1 }
        };

        cache.SetItemsLoader(userLoader);

        A.CallTo(() => userLoader())
            .Returns(users);

        cache.Items<User>().NonUniqueIndex(user => user.NullableValue).GetKeys()
            .Should()
            .BeEquivalentTo([(Nullable<Int32>)null, 1]);
    }

    [Test]
    public void GetKeys_ShouldReturnAllKeys()
    {
        var cache = new AutoIndexCache();
        var userLoader = A.Fake<Func<User[]>>();
        var users = this.TestUsers;

        cache.SetItemsLoader(userLoader);

        A.CallTo(() => userLoader())
            .Returns(users);

        cache.Items<User>().NonUniqueIndex(user => user.GroupId).GetKeys()
            .Should()
            .BeEquivalentTo(this.TestUsers.Select(a => a.GroupId).Distinct());

        cache.Items<User>().NonUniqueIndex(a => new { a.GroupId, a.IsActive }).GetKeys()
            .Should()
            .BeEquivalentTo(this.TestUsers.Select(a => new { a.GroupId, a.IsActive }).Distinct());
    }
}