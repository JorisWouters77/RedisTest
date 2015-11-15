using StackExchange.Redis;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace RedisTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = ConfigurationOptions.Parse("winserver2012el:6379,winserver2012el:6379,defaultDatabase=14,syncTimeout=10000,AllowAdmin=true");
            var redis = new RedisClientFactory(config);
                        
            while (!Console.KeyAvailable)
            {
                Parallel.For(0, 10, (i) =>
                {
                    var client = redis.CreateClient();
                    var keys = client.GetKeys("Thread*");
                    if (i == 0)
                        Console.WriteLine(string.Format("Found keys 'Thread*': {0}", keys.Length));

                    var key = "Thread " + i.ToString();

                    string value;
                    if (client.Get(key, out value))
                    {
                        Debug.WriteLine("Cache hit {0}: {1} {2}", key, DateTime.Now, value.Length);
                    }
                    else
                    {
                        Console.WriteLine("Thread {0} creating value...", i);
                        using (var webclient = new WebClient())
                        {
                            Random r = new Random(DateTime.Now.Millisecond);
                            var length = r.Next(100, 30000);

                            value = GetRandomString(length);
                            client.Set(key, value, new TimeSpan(0, 0, 50));
                        }
                        Console.WriteLine("Cache set {0}: {1}", key, value.Length);
                    }
                });
            }

            redis.CreateClient().ClearByPattern("Thread");
        }

        public static string GetRandomString(int length)
        {
            var sb = new StringBuilder();
            while (sb.Length < length)
                sb.Append(Path.GetRandomFileName());
            return sb.ToString().Substring(0, length);
        }

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
}
