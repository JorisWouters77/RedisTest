using System.IO;
using System.Text;

namespace RedisTest
{
    public class StringExtensions
    {
        public static string GetRandomString(int length)
        {
            var sb = new StringBuilder();
            while (sb.Length < length)
                sb.Append(Path.GetRandomFileName());
            return sb.ToString().Substring(0, length);
        }
    }
}
