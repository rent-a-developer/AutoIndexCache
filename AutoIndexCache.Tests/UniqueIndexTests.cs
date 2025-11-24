using AutoIndexCache.Exceptions;
using AutoIndexCache.Tests.TestData;
using FakeItEasy;
using FluentAssertions;
using NUnit.Framework;

namespace AutoIndexCache.Tests;

[TestFixture]
public class UniqueIndexTests : TestsBase
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
                cache.Items<User>().UniqueIndex(a => a.Id).ContainsKey(1);
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
                cache.Items<Group>().UniqueIndex(a => a.Id).ContainsKey(1);
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
                cache.Items<User>().UniqueIndex(a => a.Id).ContainsKey(1);
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
    public void ContainsKey_DuplicateKey_ShouldThrow()
    {
        var cache = new AutoIndexCache();
        var userLoader = A.Fake<Func<User[]>>();
        var users = new User[]
        {
            new(1, "User 1", 1, true) { NullableObject = null },
            new(1, "User 1", 1, true) { NullableObject = null }
        };

        cache.SetItemsLoader(userLoader);

        A.CallTo(() => userLoader())
            .Returns(users);

        cache.Invoking(a => a.Items<User>().UniqueIndex(user => user.GroupId).ContainsKey(this.TestUsers.First().Id))
            .Should()
            .Throw<DuplicateKeyException>()
            .WithMessage("Duplicate key found: Multiple cache items of the type 'AutoIndexCache.Tests.TestData.User' have the same key '1' for the key expression 'user => user.GroupId'.");

        cache.Invoking(a => a.Items<User>().UniqueIndex(user => user.NullableObject).ContainsKey(null))
            .Should()
            .Throw<DuplicateKeyException>()
            .WithMessage("Duplicate key found: Multiple cache items of the type 'AutoIndexCache.Tests.TestData.User' have the same key '{null}' for the key expression 'user => user.NullableObject'.");
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

        cache.Items<User>().UniqueIndex(user => user.NullableObject).ContainsKey(null)
            .Should()
            .BeTrue();

        cache.Items<User>().UniqueIndex(user => user.NullableObject).ContainsKey("")
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

        cache.Items<User>().UniqueIndex(user => user.NullableValue).ContainsKey(null)
            .Should()
            .BeTrue();

        cache.Items<User>().UniqueIndex(user => user.NullableValue).ContainsKey(1)
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

        cache.Items<User>().UniqueIndex(user => user.Id).ContainsKey(this.TestUsers.First().Id)
            .Should()
            .BeTrue();

        cache.Items<User>().UniqueIndex(user => user.Id).ContainsKey(-1)
            .Should()
            .BeFalse();
    }

    [Test]
    public void GetItemOrDefault_AccessFromItemsLoaderOfSameItemsType_ShouldThrow()
    {
        var cache = new AutoIndexCache();

        Exception? thrownException = null;

        User[] LoadUsers()
        {
            try
            {
                cache.Items<User>().UniqueIndex(a => a.Id).GetItemOrDefault(1);
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
    public void GetItemOrDefault_CyclicAccessFromItemsLoader_ShouldThrow()
    {
        var cache = new AutoIndexCache();

        Exception? loadUsersThrownException = null;
        Exception? loadGroupsThrownException = null;

        User[] LoadUsers()
        {
            try
            {
                cache.Items<Group>().UniqueIndex(a => a.Id).GetItemOrDefault(1);
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
                cache.Items<User>().UniqueIndex(a => a.Id).GetItemOrDefault(1);
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
    public void GetItemOrDefault_DuplicateKey_ShouldThrow()
    {
        var cache = new AutoIndexCache();
        var userLoader = A.Fake<Func<User[]>>();
        var users = new User[]
        {
            new(1, "User 1", 1, true) { NullableObject = null },
            new(1, "User 1", 1, true) { NullableObject = null }
        };

        cache.SetItemsLoader(userLoader);

        A.CallTo(() => userLoader())
            .Returns(users);

        cache.Invoking(a => a.Items<User>().UniqueIndex(user => user.GroupId).GetItemOrDefault(1))
            .Should()
            .Throw<DuplicateKeyException>()
            .WithMessage("Duplicate key found: Multiple cache items of the type 'AutoIndexCache.Tests.TestData.User' have the same key '1' for the key expression 'user => user.GroupId'.");

        cache.Invoking(a => a.Items<User>().UniqueIndex(user => user.NullableObject).GetItemOrDefault(null))
            .Should()
            .Throw<DuplicateKeyException>()
            .WithMessage("Duplicate key found: Multiple cache items of the type 'AutoIndexCache.Tests.TestData.User' have the same key '{null}' for the key expression 'user => user.NullableObject'.");
    }

    [Test]
    public void GetItemOrDefault_NullableObjectKey_ShouldHandleKey()
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

        cache.Items<User>().UniqueIndex(user => user.NullableObject).GetItemOrDefault(null)
            .Should()
            .Be(users[0]);

        cache.Items<User>().UniqueIndex(user => user.NullableObject).GetItemOrDefault("")
            .Should()
            .Be(users[1]);
    }

    [Test]
    public void GetItemOrDefault_NullableValueKey_ShouldHandleKey()
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

        cache.Items<User>().UniqueIndex(user => user.NullableValue).GetItemOrDefault(null)
            .Should()
            .Be(users[0]);

        cache.Items<User>().UniqueIndex(user => user.NullableValue).GetItemOrDefault(1)
            .Should()
            .Be(users[1]);
    }

    [Test]
    public void GetItemOrDefault_ShouldReturnMatchingItem()
    {
        var cache = new AutoIndexCache();
        var userLoader = A.Fake<Func<User[]>>();
        var users = this.TestUsers;

        cache.SetItemsLoader(userLoader);

        A.CallTo(() => userLoader())
            .Returns(users);

        cache.Items<User>().UniqueIndex(user => user.Id).GetItemOrDefault(1)
            .Should()
            .Be(this.TestUsers[0]);

        cache.Items<User>().UniqueIndex(user => user.Id).GetItemOrDefault(2)
            .Should()
            .Be(this.TestUsers[1]);

        cache.Items<User>().UniqueIndex(user => user.Id).GetItemOrDefault(3)
            .Should()
            .Be(this.TestUsers[2]);


        cache.Items<User>().UniqueIndex(user => user.UserName).GetItemOrDefault("User 1")
            .Should()
            .Be(this.TestUsers[0]);

        cache.Items<User>().UniqueIndex(user => user.UserName).GetItemOrDefault("User 2")
            .Should()
            .Be(this.TestUsers[1]);

        cache.Items<User>().UniqueIndex(user => user.UserName).GetItemOrDefault("User 3")
            .Should()
            .Be(this.TestUsers[2]);
    }

    [Test]
    public void GetItemOrDefault_ShouldReturnNullIfNoMatchingItemIsFound()
    {
        var cache = new AutoIndexCache();
        var userLoader = A.Fake<Func<User[]>>();
        var users = this.TestUsers;

        cache.SetItemsLoader(userLoader);

        A.CallTo(() => userLoader())
            .Returns(users);

        cache.Items<User>().UniqueIndex(user => user.Id).GetItemOrDefault(9999999)
            .Should()
            .BeNull();

        cache.Items<User>().UniqueIndex(user => user.UserName).GetItemOrDefault("Non Existent")
            .Should()
            .BeNull();
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
                cache.Items<User>().UniqueIndex(a => a.Id).GetKeys();
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
                cache.Items<Group>().UniqueIndex(a => a.Id).GetKeys();
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
                cache.Items<User>().UniqueIndex(a => a.Id).GetKeys();
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
    public void GetKeys_DuplicateKey_ShouldThrow()
    {
        var cache = new AutoIndexCache();
        var userLoader = A.Fake<Func<User[]>>();
        var users = new User[]
        {
            new(1, "User 1", 1, true) { NullableObject = null },
            new(1, "User 1", 1, true) { NullableObject = null }
        };

        cache.SetItemsLoader(userLoader);

        A.CallTo(() => userLoader())
            .Returns(users);

        cache.Invoking(a => a.Items<User>().UniqueIndex(user => user.GroupId).GetKeys())
            .Should()
            .Throw<DuplicateKeyException>()
            .WithMessage("Duplicate key found: Multiple cache items of the type 'AutoIndexCache.Tests.TestData.User' have the same key '1' for the key expression 'user => user.GroupId'.");

        cache.Invoking(a => a.Items<User>().UniqueIndex(user => user.NullableObject).GetKeys())
            .Should()
            .Throw<DuplicateKeyException>()
            .WithMessage("Duplicate key found: Multiple cache items of the type 'AutoIndexCache.Tests.TestData.User' have the same key '{null}' for the key expression 'user => user.NullableObject'.");
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

        cache.Items<User>().UniqueIndex(user => user.NullableObject).GetKeys()
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

        cache.Items<User>().UniqueIndex(user => user.NullableValue).GetKeys()
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

        cache.Items<User>().UniqueIndex(user => user.Id).GetKeys()
            .Should()
            .BeEquivalentTo(this.TestUsers.Select(a => a.Id));
    }
}
