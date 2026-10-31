using System;
using System.Collections.Generic;
using System.Linq;

namespace BenchmarkCs
{
    public static class ArrayOpsCs
    {
        public static int[] FillArray(int size)
        {
            var arr = new int[size];
            for (int i = 0; i < size; i++)
            {
                arr[i] = i * 2;
            }
            return arr;
        }

        public static long ReadArray(int[] arr)
        {
            long sum = 0;
            foreach (var val in arr)
            {
                sum += val;
            }
            return sum;
        }

        public static long ReadArrayByIndex(int[] arr)
        {
            long sum = 0;
            var count = arr.Length;
            for (int i = 0; i < count; i++)
            {
                sum += arr[i];
            }
            return sum;
        }

        public static Dictionary<string, int> WriteArray(int size)
        {
            var arr = new Dictionary<string, int>(size);
            for (int i = 0; i < size; i++)
            {
                arr[$"key_{i}"] = i;
            }
            return arr;
        }

        public static List<object> MixedOperations(int size)
        {
            var arr = new List<object>(size * 3);
            for (int i = 0; i < size; i++)
            {
                arr.Add(i);
                arr.Add($"value_{i}");
                arr.Add(i * 3);
            }
            return arr;
        }

        public static int[] ArrayFilter(int[] arr)
        {
            return arr.Where(v => v % 2 == 0).ToArray();
        }

        public static int[] ArrayMap(int[] arr)
        {
            return arr.Select(v => v * 2 + 1).ToArray();
        }
    }
}
