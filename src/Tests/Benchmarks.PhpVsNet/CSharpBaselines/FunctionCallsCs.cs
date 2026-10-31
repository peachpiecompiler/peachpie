using System;

namespace BenchmarkCs
{
    public static class FunctionCallsCs
    {
        public static int SimpleAdd(int a, int b) => a + b;
        public static int SimpleMultiply(int a, int b) => a * b;

        public static int NestedCall(int x)
        {
            return SimpleAdd(SimpleMultiply(x, 2), SimpleMultiply(x, 3));
        }

        public static int DeepChain(int x)
        {
            var a = SimpleAdd(x, 1);
            var b = SimpleMultiply(a, 2);
            var c = SimpleAdd(b, 3);
            var d = SimpleMultiply(c, 4);
            return d;
        }

        public static int RecursiveFactorial(int n)
        {
            if (n <= 1) return 1;
            return n * RecursiveFactorial(n - 1);
        }

        public static int DirectCall(int iterations)
        {
            int sum = 0;
            for (int i = 0; i < iterations; i++)
            {
                sum += SimpleAdd(i, i + 1);
            }
            return sum;
        }

        public static int NestedCalls(int iterations)
        {
            int sum = 0;
            for (int i = 0; i < iterations; i++)
            {
                sum += NestedCall(i);
            }
            return sum;
        }

        public static int DeepChains(int iterations)
        {
            int sum = 0;
            for (int i = 0; i < iterations; i++)
            {
                sum += DeepChain(i);
            }
            return sum;
        }

        public static int RecursiveFactorials(int n)
        {
            int sum = 0;
            for (int i = 1; i <= n; i++)
            {
                sum += RecursiveFactorial(i);
            }
            return sum;
        }
    }
}
