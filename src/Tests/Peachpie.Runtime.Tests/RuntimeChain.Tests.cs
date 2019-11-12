using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pchp.Core;
using Pchp.Core.Dynamic.RuntimeChain;

namespace Peachpie.Runtime.Tests
{
    [TestClass]
    public class RuntimeChainTest
    {
        readonly Context _context = Context.CreateEmpty();

        public class X : stdClass
        {
            // NOTE: stdClass has runtime fields

            public PhpValue prop = PhpValue.Null;
        }

        [TestMethod]
        public void TestProperties()
        {
            var value = new X()
            {
                prop = PhpValue.FromClass(new X()
                {
                    prop = 666,
                }),
            };

            // Construct the chain:
            // (new X)->prop->prop

            var chain1 = new Value<Property<Property<ChainEnd>>>();
            chain1.Next.Name = "prop";
            chain1.Next.Next.Name = "prop";

            //
            //var expression = chain1.BindRead(
            //    receiver: new DynamicMetaObject(Expression.Constant(value), BindingRestrictions.Empty, value),
            //    self: Expression.Constant(chain1),
            //    classContext: null,
            //    context: new DynamicMetaObject(Expression.Constant(_context), BindingRestrictions.Empty, _context));

            //// test the expression
            //var lambda = Expression.Lambda(expression.Expression);
            //var result = lambda.Compile().DynamicInvoke();

            // READ

            var result = chain1.GetValue(PhpValue.FromClass(value), _context, null);
            Assert.AreEqual(result.ToLong(), 666);

            // ENSURE $$->prop->prop = 123

            var alias = (PhpValue)new PhpAlias(PhpValue.Null);
            var propertyref = chain1.GetAlias(ref alias, _context, null);
            propertyref.Value = 123;

            //
            Assert.IsNotNull(alias.Object);
            result = chain1.GetValue(alias, _context, null);
            Assert.AreEqual(result.ToLong(), 123);
        }

        [TestMethod]
        public void TestArrayItem()
        {
            var value = new PhpArray() { 666 };

            // Construct the chain:
            // (array)[0]

            var chain1 = new Value<ArrayItem<ChainEnd>>();
            chain1.Next.Key = 0;

            //
            // READ

            var result = chain1.GetValue(value, _context, null);
            Assert.AreEqual(result.ToLong(), 666);

            // ENSURE $$[0] = 666

            var alias = (PhpValue)new PhpAlias(PhpValue.Null);
            var itemref = chain1.GetAlias(ref alias, _context, null);
            itemref.Value = 666;

            //
            Assert.IsNotNull(alias.Object);
            result = chain1.GetValue(alias, _context, null);
            Assert.AreEqual(result.ToLong(), 666);
        }
    }
}
