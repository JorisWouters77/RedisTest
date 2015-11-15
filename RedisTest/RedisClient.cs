using StackExchange.Redis;
using System;
using System.Linq;

namespace RedisTest
{
    public interface IRedisClient
    {
        bool Get(string key, out string value);
        void Set(string key, string value, TimeSpan timespan);
        string[] GetKeys(string pattern);
        string[] GetKeys();
        void Clear(string key);
        void FlushDb();
        void ChangeDbTo(int dbNumber);
    }

    public class RedisClient : IRedisClient
    {
        private IConnectionMultiplexer _multiplexer;
        private int _database;

        public RedisClient(IConnectionMultiplexer multiplexer, int database)
        {
            _multiplexer = multiplexer;
            _database = database;
        }

        private IServer Server
        {
            get
            {
                var endpoints = _multiplexer.GetEndPoints();
                return _multiplexer.GetServer(endpoints[0]);
            }
        }
        private IDatabase Db
        {
            get
            {
                return _multiplexer.GetDatabase(_database);
            }
        }

        public bool Get(string key, out string value)
        {
            var redisValue = Db.StringGet(key, CommandFlags.PreferSlave);
            value = redisValue;

            return redisValue.HasValue;
        }

        public string[] GetKeys(string pattern)
        {
            return Server
                .Keys(_database, pattern)
                .Select(k => (string)k)
                .ToArray();
        }

        public void Set(string key, string value, TimeSpan timespan)
        {
            Db.StringSet(key, value, timespan, When.Always, CommandFlags.FireAndForget | CommandFlags.PreferMaster);
        }

        public void Clear(string key)
        {
            Db.KeyDelete(key);
        }

        public void ClearByPattern(string pattern)
        {
            foreach (var key in GetKeys(pattern))
                Clear(key);
        }

        public void FlushDb()
        {
            Server.FlushDatabase(_database);
        }

        public void ChangeDbTo(int dbNumber)
        {
            _database = dbNumber;
        }

        public string[] GetKeys()
        {
            return Server
                .Keys(_database)
                .Select(k => (string)k)
                .ToArray();
        }
    }
}
