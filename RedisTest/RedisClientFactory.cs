using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RedisTest
{
    public interface IRedisClientFactory
    {
        RedisClient CreateClient();
    }

    public class RedisClientFactory : IRedisClientFactory
    {
        private ConfigurationOptions _options;
        private ConnectionMultiplexer _multiplexer;

        public RedisClientFactory(ConfigurationOptions options)
        {
            _options = options;
            _multiplexer = ConnectionMultiplexer.Connect(_options);
        }

        public RedisClient CreateClient()
        {
            return new RedisClient(_multiplexer, _options.DefaultDatabase.GetValueOrDefault(0));
        }
    }
}
