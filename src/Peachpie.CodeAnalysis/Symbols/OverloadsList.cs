using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Semantics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// List of overloads for a function call.
    /// </summary>
    internal class OverloadsList
    {
        readonly List<MethodSymbol> _methods;

        public OverloadsList(params MethodSymbol[] methods)
        {
            _methods = new List<MethodSymbol>(methods);
        }

        public MethodSymbol Resolve(TypeRefContext typeCtx, TypeRefMask[] args, TypeSymbol classCtx)
        {
            if (_methods.Count == 0)
            {
                return new MissingMethodSymbol();
            }

            if (_methods.Count == 1 && _methods[0].IsErrorMethod())
            {
                return _methods[0];
            }

            // see Pchp.Core.Dynamic.OverloadBinder

            var result = new List<MethodSymbol>(_methods);

            RemoveInaccessible(result, classCtx);

            if (result.Count == 0)
            {
                return new InaccessibleMethodSymbol(_methods.AsImmutable());
            }

            if (result.Count == 1)
            {
                return result[0];
            }

            // TODO: cost of args convert operation

            // by params count

            var result2 = new List<MethodSymbol>();

            foreach (var m in result)
            {
                var nmandatory = 0;
                var hasoptional = false;
                var hasparams = false;
                var match = true;

                var expectedparams = m.GetExpectedArguments(typeCtx);

                foreach (var p in expectedparams)
                {
                    hasoptional |= p.DefaultValue != null;
                    hasparams |= p.IsVariadic;
                    if (!hasoptional && !hasparams) nmandatory++;

                    // TODO: check args[i] is convertible to p.Type
                    if (p.Index < args.Length)
                    {
                        match &= args[p.Index] == p.Type;
                    }
                }

                //
                if (args.Length >= nmandatory && (hasparams || args.Length <= expectedparams.Length))
                {
                    // TODO: this is naive implementation of overload resolution,
                    // make it properly using Conversion Cost
                    if (match && !hasparams)
                    {
                        return m;   // perfect match
                    }

                    //
                    result2.Add(m);
                }
            }

            //
            return (result2.Count == 1) ? result2[0] : new AmbiguousMethodSymbol(result.AsImmutable(), true);
        }

        static void RemoveInaccessible(List<MethodSymbol> methods, TypeSymbol classCtx)
        {
            for (int i = methods.Count - 1; i >= 0; i--)
            {
                if (methods[i].IsErrorMethod() && !methods[i].IsAccessible(classCtx) || methods[i].IsFieldsOnlyConstructor())
                {
                    methods.RemoveAt(i);
                }
            }
        }
    }
}
