using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Pchp.Core;

namespace Benchmarks
{
    //[SimpleJob(RuntimeMoniker.Net472)]
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [SimpleJob(RuntimeMoniker.NetCoreApp50, baseline: true)]
    //[MarkdownExporter]
    public class PhpArrayBenchmark
    {
        const int Size = 128;
        readonly PhpArray _array;

        //[Params(1, 7, 43, 73, 127, 128)]
        [Params(11)]
        public int Index
        {
            get => _index;
            set
            {
                _index = value;
                _intkey = new IntStringKey(value);
                _strkey = new IntStringKey("key" + value);
            }
        }

        int _index;
        IntStringKey _intkey, _strkey;

        public PhpArrayBenchmark()
        {
            // array with int and string keys,
            // not sequential
            _array = new PhpArray();
            for (int i = 0; i < Size; i++)
            {
                _array.Add(i.ToString());
                _array["key" + i] = i.ToString();
            }
        }

        [Benchmark]
        public void ReadArrayItemsByIntKey()
        {
            _array.GetItemValue(_intkey);
        }

        [Benchmark]
        public void ReadArrayItemsByStringKey()
        {
            _array.GetItemValue(_strkey);
        }
    }
}
