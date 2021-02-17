using System;
using BenchmarkDotNet.Running;

namespace Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<PhpArrayBenchmark>();

            //var b = new PhpArrayBenchmark { Index = 47, };
            //for (int i = 0; i < 219200000; i++)
            //{
            //    b.ReadArrayItemsByStringKey();
            //}
        }
    }
}
