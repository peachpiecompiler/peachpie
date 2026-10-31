using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using Pchp.Core;

namespace Benchmarks
{
    /// <summary>
    /// Entry point for the PHP vs .NET benchmark suite.
    ///
    /// Architecture:
    ///   1. PHP scripts in PhpScripts/ are compiled by PeachPie (via Peachpie.NET.Sdk)
    ///      into a .NET assembly (BenchmarkPhp.dll).
    ///   2. This project (targeting .NET 10) references BenchmarkPhp.dll and calls
    ///      compiled PHP functions through the PeachPie runtime Context API.
    ///   3. C# baselines in CSharpBaselines/ implement the same algorithms in pure C#.
    ///   4. BenchmarkDotNet measures and compares both implementations.
    ///
    /// Run: dotnet run -c Release -f net10.0
    /// </summary>
    class Program
    {
        /// <summary>
        /// Shared benchmark config: .NET 10, MemoryDiagnoser for GC allocation tracking,
        /// markdown + HTML exports, and console logging.
        /// </summary>
        internal static readonly IConfig Config = ManualConfig.CreateEmpty()
            .AddJob(Job.MediumRun.WithWarmupCount(3).WithIterationCount(5))
            .AddDiagnoser(MemoryDiagnoser.Default)
            .AddExporter(MarkdownExporter.Console)
            .AddExporter(HtmlExporter.Default)
            .AddLogger(ConsoleLogger.Default);

        static void Main(string[] args)
        {
            BenchmarkDotNet.Running.BenchmarkRunner.Run<TightLoopBenchmarks>(Config);
            BenchmarkDotNet.Running.BenchmarkRunner.Run<ObjectOrientedBenchmarks>(Config);
            BenchmarkDotNet.Running.BenchmarkRunner.Run<ArrayOpsBenchmarks>(Config);
            BenchmarkDotNet.Running.BenchmarkRunner.Run<StringOpsBenchmarks>(Config);
            BenchmarkDotNet.Running.BenchmarkRunner.Run<FunctionCallBenchmarks>(Config);
        }

        /// <summary>
        /// Invokes a static method on a compiled PHP class via reflection.
        /// Loads the type from the compiled PeachPie assembly (BenchmarkPhp_*.dll)
        /// produced by compiling PhpScripts/ with the Peachpie.NET.Sdk peach compiler.
        /// </summary>
        internal static T PhpStatic<T>(string className, string methodName, params object[] args)
        {
            // Compiled PHP assemblies are named BenchmarkPhp_<ScriptName>.dll
            // They are produced by: peach.dll /target:library /out:BenchmarkPhp_<name>.dll PhpScripts/<name>.php
            var asmName = $"BenchmarkPhp_{className}";
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == asmName)
                ?? Assembly.LoadFrom($"/tmp/{asmName}.dll");

            var type = assembly.GetType($"BenchmarkPhp.{className}");
            if (type == null)
                throw new InvalidOperationException($"PHP class BenchmarkPhp.{className} not found in {asmName}");
            var method = type.GetMethod(methodName);
            if (method == null)
                throw new InvalidOperationException($"Method {methodName} not found on PHP class {className}");
            return (T)method.Invoke(null, args)!;
        }
    }

    #region TightLoop Benchmarks

    [MemoryDiagnoser]
    public class TightLoopBenchmarks
    {
        private const int Size = 10_000;

        [Benchmark(Baseline = true)]
        public int CSharpSumInts() => BenchmarkCs.TightLoopCs.SumInts(Size);

        [Benchmark]
        public int PhpSumInts() => Program.PhpStatic<int>("TightLoop", "sumInts", Size);

        [Benchmark]
        public int CSharpSumIntsUnrolled() => BenchmarkCs.TightLoopCs.SumIntsUnrolled(Size);

        [Benchmark]
        public int PhpSumIntsUnrolled() => Program.PhpStatic<int>("TightLoop", "sumIntsUnrolled", Size);

        [Benchmark]
        public int CSharpFibonacci() => BenchmarkCs.TightLoopCs.FibonacciIterative(Size / 10);

        [Benchmark]
        public int PhpFibonacci() => Program.PhpStatic<int>("TightLoop", "fibonacciIterative", Size / 10);
    }

    #endregion

    #region Object-Oriented Benchmarks

    [MemoryDiagnoser]
    public class ObjectOrientedBenchmarks
    {
        private const int Iterations = 1_000;

        [Benchmark(Baseline = true)]
        public int CSharpDispatchViaInterface()
        {
            var adder = new BenchmarkCs.Adder();
            return BenchmarkCs.ObjectMathCs.DispatchViaInterface(adder, 10, 20);
        }

        [Benchmark]
        public int PhpDispatchViaInterface() => Program.PhpStatic<int>("ObjectMath", "dispatchViaInterface", 10, 20);

        [Benchmark]
        public int CSharpPolymorphicSum() => BenchmarkCs.ObjectMathCs.PolymorphicSum(Iterations);

        [Benchmark]
        public int PhpPolymorphicSum() => Program.PhpStatic<int>("ObjectMath", "polymorphicSum", Iterations);

        [Benchmark]
        public double CSharpSumDistances()
        {
            var points = BenchmarkCs.PointBenchmarkCs.CreatePoints(100);
            return BenchmarkCs.PointBenchmarkCs.SumDistances(points);
        }

        [Benchmark]
        public double PhpSumDistances()
        {
            var points = Program.PhpStatic<object>("PointBenchmark", "createPoints", 100);
            return Program.PhpStatic<double>("PointBenchmark", "sumDistances", points);
        }

        [Benchmark]
        public int CSharpAccessors()
        {
            var points = BenchmarkCs.PointBenchmarkCs.CreatePoints(100);
            return BenchmarkCs.PointBenchmarkCs.Accessors(points);
        }

        [Benchmark]
        public int PhpAccessors()
        {
            var points = Program.PhpStatic<object>("PointBenchmark", "createPoints", 100);
            return Program.PhpStatic<int>("PointBenchmark", "accessors", points);
        }
    }

    #endregion

    #region Array Operations Benchmarks

    [MemoryDiagnoser]
    public class ArrayOpsBenchmarks
    {
        private const int Size = 1_000;
        private static readonly int[] _intArray = Enumerable.Range(0, Size).ToArray();
        private static readonly object[] _phpArray = _intArray.Cast<object>().ToArray();

        [Benchmark(Baseline = true)]
        public long CSharpReadArray() => BenchmarkCs.ArrayOpsCs.ReadArray(_intArray);

        [Benchmark]
        public long PhpReadArray() => Program.PhpStatic<long>("ArrayOps", "readArray", _phpArray);

        [Benchmark]
        public long CSharpReadArrayByIndex() => BenchmarkCs.ArrayOpsCs.ReadArrayByIndex(_intArray);

        [Benchmark]
        public long PhpReadArrayByIndex() => Program.PhpStatic<long>("ArrayOps", "readArrayByIndex", _phpArray);

        [Benchmark]
        public int[] CSharpFillArray() => BenchmarkCs.ArrayOpsCs.FillArray(Size);

        [Benchmark]
        public object PhpFillArray() => Program.PhpStatic<object>("ArrayOps", "fillArray", Size);

        [Benchmark]
        public int[] CSharpArrayFilter() => BenchmarkCs.ArrayOpsCs.ArrayFilter(_intArray);

        [Benchmark]
        public object PhpArrayFilter() => Program.PhpStatic<object>("ArrayOps", "arrayFilter", _phpArray);
    }

    #endregion

    #region String Operations Benchmarks

    [MemoryDiagnoser]
    public class StringOpsBenchmarks
    {
        private const int Count = 100;
        private static readonly string _testStr = "abcdefghijklmnopqrstuvwxyz";

        [Benchmark(Baseline = true)]
        public string CSharpConcatStrings() => BenchmarkCs.StringOpsCs.ConcatStrings(Count);

        [Benchmark]
        public string PhpConcatStrings() => Program.PhpStatic<string>("StringOps", "concatStrings", Count);

        [Benchmark]
        public string CSharpSubstrInLoop() => BenchmarkCs.StringOpsCs.SubstrInLoop(_testStr, Count);

        [Benchmark]
        public string PhpSubstrInLoop() => Program.PhpStatic<string>("StringOps", "substrInLoop", _testStr, Count);

        [Benchmark]
        public string CSharpSprintfLoop() => BenchmarkCs.StringOpsCs.SprintfLoop("item_%d", Count);

        [Benchmark]
        public string PhpSprintfLoop() => Program.PhpStatic<string>("StringOps", "sprintfLoop", "item_%d", Count);
    }

    #endregion

    #region Function Call Benchmarks

    [MemoryDiagnoser]
    public class FunctionCallBenchmarks
    {
        private const int Iterations = 1_000;

        [Benchmark(Baseline = true)]
        public int CSharpDirectCall() => BenchmarkCs.FunctionCallsCs.DirectCall(Iterations);

        [Benchmark]
        public int PhpDirectCall() => Program.PhpStatic<int>("FunctionCallBench", "directCall", Iterations);

        [Benchmark]
        public int CSharpNestedCalls() => BenchmarkCs.FunctionCallsCs.NestedCalls(Iterations);

        [Benchmark]
        public int PhpNestedCalls() => Program.PhpStatic<int>("FunctionCallBench", "nestedCalls", Iterations);

        [Benchmark]
        public int CSharpDeepChains() => BenchmarkCs.FunctionCallsCs.DeepChains(Iterations);

        [Benchmark]
        public int PhpDeepChains() => Program.PhpStatic<int>("FunctionCallBench", "deepChains", Iterations);

        [Benchmark]
        public int CSharpRecursiveFactorials() => BenchmarkCs.FunctionCallsCs.RecursiveFactorials(20);

        [Benchmark]
        public int PhpRecursiveFactorials() => Program.PhpStatic<int>("FunctionCallBench", "recursiveFactorials", 20);
    }

    #endregion
}
