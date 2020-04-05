using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pchp.Core;

namespace Peachpie.Runtime.Tests
{
    [TestClass]
    public class OrderedDictionaryTest
    {
        //static string ToString(OrderedDictionary array) => string.Join("", array.Select(entry => entry.Item2));
        static string ToString(OrderedDictionary array)
        {
            var result = new StringBuilder();

            foreach (var value in array) // uses FastEnumerator
            {
                Debug.Assert(value.Value.String != null);   // only string values for the test
                result.Append(value.Value.String);
            }

            return result.ToString();
        }

        [TestMethod]
        public void Test1()
        {
            var array = new OrderedDictionary();

            Assert.AreEqual(0, array.Count);

            array.Add("Hello");
            array.Add(" ");
            array.Add("World");

            Assert.AreEqual(3, array.Count);

            Assert.AreEqual("Hello World", ToString(array));

            Assert.AreEqual("Hello", array[0]);
            Assert.AreEqual("World", array[2]);

            array.Remove(1);

            Assert.AreEqual(2, array.Count);

            Assert.AreEqual("HelloWorld", ToString(array));

            Assert.AreEqual("World", array[2]);

            array.Add("!");

            Assert.AreEqual(3, array.Count);

            Assert.AreEqual("HelloWorld!", ToString(array));

            Assert.AreEqual("!", array[3]);

            Assert.IsTrue(array.ContainsKey(0));
            Assert.IsFalse(array.ContainsKey(1));
        }

        [TestMethod]
        public void Test2()
        {
            var array = new OrderedDictionary(100000);

            const int count = 1000000;

            for (int i = count; i > 0; i--)
            {
                array[i] = i.ToString("x4");
            }

            Assert.AreEqual(count, array.Count);

            int removed = 0;

            for (int i = 1; i < count; i += 3)
            {
                Assert.IsTrue(array.Remove(i));
                removed++;
            }

            Assert.AreEqual(count - removed, array.Count);
        }

        [TestMethod]
        public void TestPacked()
        {
            var array = new OrderedDictionary(100000);

            Assert.IsTrue(array.IsPacked);

            const int count = 1000000;

            for (int i = 0; i < count; i++)
            {
                array[i] = i.ToString("x4");
            }

            Assert.IsTrue(array.IsPacked);
            Assert.AreEqual(count, array.Count);

            for (int i = count - 1; i >= 0; i--)
            {
                Assert.IsTrue(array.Remove(i));
                Assert.IsTrue(array.IsPacked);
            }

            Assert.AreEqual(0, array.Count);
            Assert.AreEqual("", ToString(array)); // enumeration of empty array works

            for (int i = 0; i < count; i++)
            {
                array[i] = i.ToString("x4");
            }

            Assert.IsTrue(array.IsPacked);

            array.Add("last");

            Assert.IsTrue(array.IsPacked);
            Assert.AreEqual(count + 1, array.Count);
            Assert.IsTrue(array.ContainsKey(count));
            Assert.AreEqual("last", array[count]);
        }

        [TestMethod]
        public void TestShuffle()
        {
            // create array and check shuffle

            var array = new OrderedDictionary();

            const int count = 123;

            for (int i = 0; i < count; i++)
            {
                array.Add(i.ToString("x4"));
            }

            array.Remove(44);
            array.Remove(45);
            array.Remove(46);
            array.Remove(0);

            array.Shuffle(new Random());

            var set = new HashSet<long>();

            foreach (var pair in array)
            {
                Assert.IsTrue(set.Add(pair.Key.Integer));
            }

            Assert.AreEqual(array.Count, set.Count);
        }

        [TestMethod]
        public void TestReverse()
        {
            var array = new OrderedDictionary();

            const int count = 11;

            for (int i = 0; i < count; i++)
            {
                array.Add(i.ToString("x4"));
            }

            // reverse reverse -> must result in the same array as before

            var before = ToString(array);

            array.Reverse();
            array.Reverse();

            Assert.AreEqual(before, ToString(array));

            // expected count

            Assert.AreEqual(count, array.Count);

            // remove items and reverse array with holes:

            Assert.IsTrue(array.Remove(3));
            Assert.IsTrue(array.Remove(4));
            Assert.IsTrue(array.Remove(7));

            array.Reverse();

            var last = new KeyValuePair<IntStringKey, PhpValue>(int.MaxValue, default);

            foreach (var pair in array)
            {
                Assert.IsTrue(last.Key.Integer > pair.Key.Integer);
                Assert.AreEqual(pair.Value.String, pair.Key.Integer.ToString("x4"));

                last = pair;
            }
        }

        class KeyComparer : IComparer<KeyValuePair<IntStringKey, PhpValue>>
        {
            public int Compare(KeyValuePair<IntStringKey, PhpValue> x, KeyValuePair<IntStringKey, PhpValue> y)
            {
                return x.Key.Integer.CompareTo(y.Key.Integer);
            }
        }

        class ValueComparer : IComparer<KeyValuePair<IntStringKey, PhpValue>>
        {
            public int Compare(KeyValuePair<IntStringKey, PhpValue> x, KeyValuePair<IntStringKey, PhpValue> y)
            {
                return StringComparer.OrdinalIgnoreCase.Compare(x.Value.String, y.Value.String);
            }
        }

        [TestMethod]
        public void TestSort()
        {
            var array = new OrderedDictionary();

            const int count = 10;

            for (int i = count; i > 0; i--)
            {
                array[i] = i.ToString();
            }

            // remove items and reverse array with holes:

            Assert.IsTrue(array.Remove(3));
            Assert.IsTrue(array.Remove(4));
            Assert.IsTrue(array.Remove(7));

            array.Sort(new KeyComparer());

            Assert.AreEqual(count - 3, array.Count);

            var last = new KeyValuePair<IntStringKey, PhpValue>(0, default);

            foreach (var pair in array)
            {
                Assert.IsTrue(last.Key.Integer < pair.Key.Integer);
                Assert.AreEqual(pair.Value.String, pair.Key.Integer.ToString());

                last = pair;
            }
        }

        [TestMethod]
        public void TestDiff()
        {
            var array = new OrderedDictionary();

            const int count = 100;

            for (int i = 0; i < count; i++)
            {
                array[i] = i.ToString();
            }

            array.Shuffle(new Random());

            var array2 = new OrderedDictionary();
            array2.Add("3");
            array2.Add("4");
            array2.Add("7");

            var diff = array.SetOperation(SetOperations.Difference, new[] { new PhpArray(array2) }, new ValueComparer());

            Assert.AreEqual(array.Count - array2.Count, diff.Count);

            var diff_diff = array.SetOperation(SetOperations.Difference, new[] { new PhpArray(diff) }, new ValueComparer());

            Assert.AreEqual(diff_diff.Count, array2.Count);
        }

        [TestMethod]
        public void TestPrepend()
        {
            var array = new OrderedDictionary();
            array[0] = "0";
            array[1] = "1";

            array.AddFirst(-1, "-1");
            array.AddFirst(-2, "-2");

            Assert.AreEqual(4, array.Count);

            Assert.AreEqual("-2-101", ToString(array));
        }
    }
}
