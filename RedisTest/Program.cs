using StackExchange.Redis;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace RedisTest
{
    class Program
    {
        private const string CacheName = "Thread";

        static void Main(string[] args)
        {
            var config = ConfigurationOptions.Parse("winserver2012el:6379,winserver2012el:6379,defaultDatabase=14,syncTimeout=10000,AllowAdmin=true");
            var redis = new RedisClientFactory(config);
                        
            while (!Console.KeyAvailable)
            {
                Parallel.For(0, 10, (i) =>
                {
                    TestRedisCache(i, redis);
                });
            }

            redis.CreateClient().ClearByPattern(CacheName);
        }

        private static void TestRedisCache(int threadNumber, RedisClientFactory redis)
        {
            var client = redis.CreateClient();

            // query for all threads
            var keys = GetAllKeys(client);
            if (threadNumber == 0)
                Console.WriteLine(string.Format("Found keys '{0}*': {1}", CacheName, keys.Length));

            var key = "Thread " + threadNumber.ToString();

            string value;
            if (client.Get(key, out value))
            {
                // cache hit
                Debug.WriteLine("Cache hit {0}: {1} {2}", key, DateTime.Now, value.Length);
            }
            else
            {
                // cache miss
                Console.WriteLine("Thread {0} creating value...", threadNumber);

                value = CreateRandomLengthString(100, 30000);
                client.Set(key, value, new TimeSpan(0, 0, 50));

                Console.WriteLine("Cache set {0}: {1}", key, value.Length);
            }
        }

        private static string[] GetAllKeys(IRedisClient client)
        {
            return client.GetKeys(string.Format("{0}*", CacheName));
        }

        private static string CreateRandomLengthString(int minlength, int maxlength, Random random = null)
        {
            Random r = random ?? new Random(DateTime.Now.Millisecond);
            var length = r.Next(minlength, maxlength);

            return StringExtensions.GetRandomString(length);
        }
    }
}
