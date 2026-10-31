using System;

namespace BenchmarkCs
{
    public static class TightLoopCs
    {
        public static int SumInts(int n)
        {
            int sum = 0;
            for (int i = 0; i < n; i++)
            {
                sum += i;
            }
            return sum;
        }

        public static int SumIntsUnrolled(int n)
        {
            int sum = 0;
            int i = 0;
            for (; i + 3 < n; i += 4)
            {
                sum += i;
                sum += (i + 1);
                sum += (i + 2);
                sum += (i + 3);
            }
            for (; i < n; i++)
            {
                sum += i;
            }
            return sum;
        }

        public static int[][] MultiplyMatrix(int size)
        {
            int[][] result = new int[size][];
            for (int i = 0; i < size; i++)
            {
                int[] row = new int[size];
                for (int j = 0; j < size; j++)
                {
                    int val = 0;
                    for (int k = 0; k < size; k++)
                    {
                        val += i * k + j;
                    }
                    row[j] = val;
                }
                result[i] = row;
            }
            return result;
        }

        public static int FibonacciIterative(int n)
        {
            if (n <= 1)
            {
                return n;
            }
            int a = 0;
            int b = 1;
            for (int i = 2; i <= n; i++)
            {
                int temp = a + b;
                a = b;
                b = temp;
            }
            return b;
        }
    }
}
