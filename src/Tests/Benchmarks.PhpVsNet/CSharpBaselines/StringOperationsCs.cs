using System;
using System.Text;

namespace BenchmarkCs
{
    public static class StringOpsCs
    {
        public static string ConcatStrings(int count)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                sb.Append($"item_{i}");
            }
            return sb.ToString();
        }

        public static string SubstrInLoop(string str, int count)
        {
            var result = "";
            var len = str.Length;
            for (int i = 0; i < count; i++)
            {
                result += str[i % len];
            }
            return result;
        }

        public static string RepeatString(string str, int count)
        {
            return new string(str[0], str.Length * count); // approximate
        }

        public static string ExplodeImplode(string delim, string str)
        {
            var parts = str.Split(new[] { delim }, StringSplitOptions.None);
            return string.Join(delim, parts);
        }

        public static string SprintfLoop(string format, int count)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                sb.Append(string.Format(format, i));
            }
            return sb.ToString();
        }
    }
}
