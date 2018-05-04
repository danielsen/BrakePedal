using Enyim.Caching;
using NSubstitute;
using BrakePedal.Memcached;
using Xunit;

namespace BrakePedal.Tests
{
    public class MemcachedThrottleRepositoryTests
    {
        public class AddOrIncrementWithExpirationMethod
        {
            [Fact]
            public void IncrementReturnsOne_ExpireKey()
            {
                // Arrange
                var key = new SimpleThrottleKey("test", "key");
                Limiter limiter = new Limiter().Limit(1).Over(10);
                var memcachedClient = Substitute.For<IMemcachedClient>();
                var repository = new MemcachedThrottleRepository(memcachedClient);
                string id = repository.CreateThrottleKey(key, limiter);

                memcachedClient.Increment(id, 1, 1, limiter.Period).Returns((ulong) 1);

                // Act
                repository.AddOrIncrementWithExpiration(key, limiter);

                // Assert
                memcachedClient.Received(1).Increment(id, 1, 1, limiter.Period);
                memcachedClient.Received(1).Increment(id, 1, 0, limiter.Period);
            }
        }

        public class GetThrottleCountMethod
        {
            [Fact]
            public void KeyDoesNotExist_ReturnsNull()
            {
                // Arrange
                var key = new SimpleThrottleKey("test", "key");
                Limiter limiter = new Limiter().Limit(1).Over(1);
                var memcachedClient = Substitute.For<IMemcachedClient>();
                var repository = new MemcachedThrottleRepository(memcachedClient);
                string id = repository.CreateThrottleKey(key, limiter);

                memcachedClient.Get<string>(id).Returns("xx");

                // Act
                long? result = repository.GetThrottleCount(key, limiter);

                // Assert
                Assert.Equal(null, result);
            }

            [Fact]
            public void KeyExists_ReturnsValue()
            {
                // Arrange
                var key = new SimpleThrottleKey("test", "key");
                Limiter limiter = new Limiter().Limit(1).Over(1);
                var memcachedClient = Substitute.For<IMemcachedClient>();
                var repository = new MemcachedThrottleRepository(memcachedClient);
                string id = repository.CreateThrottleKey(key, limiter);

                memcachedClient.Get<string>(id).Returns("10");

                // Act
                long? result = repository.GetThrottleCount(key, limiter);

                // Assert
                Assert.Equal(10, result);
            }
        }

        public class LockExistsMethod
        {
            [Theory]
            [InlineData(true, true)]
            [InlineData(false, false)]
            public void KeyExists_ReturnsTrue(bool keyExists, bool expected)
            {
                // Arrange
                var key = new SimpleThrottleKey("test", "key");
                Limiter limiter = new Limiter().Limit(1).Over(1).LockFor(1);
                var memcachedClient = Substitute.For<IMemcachedClient>();
                var repository = new MemcachedThrottleRepository(memcachedClient);
                string id = repository.CreateThrottleKey(key, limiter);

                memcachedClient.TryGet(id, out _).ReturnsForAnyArgs(keyExists);

                // Act
                bool result = repository.LockExists(key, limiter);

                // Assert
                Assert.Equal(expected, result);
            }
        }

        public class RemoveThrottleMethod
        {
            [Fact]
            public void RemoveThrottle()
            {
                // Arrange
                var key = new SimpleThrottleKey("test", "key");
                Limiter limiter = new Limiter().Limit(1).Over(1);
                var memcachedClient = Substitute.For<IMemcachedClient>();
                var repository = new MemcachedThrottleRepository(memcachedClient);
                string id = repository.CreateThrottleKey(key, limiter);
                
                // Act
                repository.RemoveThrottle(key, limiter);

                // Assert
                memcachedClient.Received(1).Remove(id);
            }
        }

        public class SetLockMethod
        {
            [Fact]
            public void SetLock()
            {
                // Arrange
                var key = new SimpleThrottleKey("test", "key");
                Limiter limiter = new Limiter().Limit(1).Over(1).LockFor(1);
                var memcachedClient = Substitute.For<IMemcachedClient>();
                var repository = new MemcachedThrottleRepository(memcachedClient);
                string id = repository.CreateLockKey(key, limiter);
                
                // Act
                repository.SetLock(key, limiter);

                // Assert
                memcachedClient.Received(1).Increment(id, 1, 1, limiter.LockDuration.Value);
            }
        }
    }
}
