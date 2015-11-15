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

                            value = StringExtensions.GetRandomString(length);
                            client.Set(key, value, new TimeSpan(0, 0, 50));
                        }
                        Console.WriteLine("Cache set {0}: {1}", key, value.Length);
                    }
                });
            }

            redis.CreateClient().ClearByPattern("Thread");
        }       
    }
}
