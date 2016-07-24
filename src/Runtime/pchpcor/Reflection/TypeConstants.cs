using Pchp.Core.Dynamic;
using Pchp.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Reflection
{
    /// <summary>
    /// Collection of class constants declared in type.
    /// </summary>
    public class TypeConstants
    {
        #region Fields

        /// <summary>
        /// Declared constants, object can be either a value or <see cref="FieldInfo"/> to nested <c>_statics</c> container.
        /// </summary>
        readonly Dictionary<string, object> _values;

        readonly Func<Context, object> _staticsGetter;

        #endregion

        #region Initialization

        internal TypeConstants(Type type)
        {
            Dictionary<string, object> values = null;

            while (type != null)
            {
                var tinfo = type.GetTypeInfo();

                // reflect constants
                foreach (var c in ReflectConstants(tinfo))
                {
                    if (values == null)
                        values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                    values.Add(c.Key, c.Value);

                    //
                    if (c.Value is FieldInfo && _staticsGetter == null)
                    {
                        _staticsGetter = CreateStaticsGetter(((FieldInfo)c.Value).DeclaringType);
                    }
                }
            }

            //
            _values = values;
        }

        static Func<Context, object> CreateStaticsGetter(Type _statics)
        {
            Debug.Assert(_statics.Name == "_statics");
            Debug.Assert(_statics.IsNested);

            var getter = BinderHelpers.GetStatic_T_Method(_statics);    // ~ Context.GetStatics<_statics>, in closure
            return ctx => getter.Invoke(ctx, ArrayUtils.EmptyObjects);
        }

        static IEnumerable<KeyValuePair<string, object>> ReflectConstants(System.Reflection.TypeInfo tinfo)
        {
            // regular class constants
            foreach (var f in tinfo.DeclaredFields)
            {
                if (f.IsPublic && f.IsLiteral && f.IsStatic)
                {
                    yield return new KeyValuePair<string, object>(f.Name, f.GetValue(null));
                }
            }

            // context bound class constants in nested container "_statics"
            var staticscontainer = tinfo.GetDeclaredNestedType("_statics");
            if (staticscontainer != null)
            {
                foreach (var f in staticscontainer.DeclaredFields)
                {
                    if (f.IsPublic && f.IsInitOnly)
                    {
                        yield return new KeyValuePair<string, object>(f.Name, f);
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Gets value indicating the class contains a constant with specified name.
        /// </summary>
        public bool Contains(string name)
        {
            return _values != null && _values.ContainsKey(name);
        }

        /// <summary>
        /// Resolves a constant value in given context.
        /// </summary>
        public object GetConstantValue(string name, Context ctx)
        {
            if (ctx == null)
            {
                throw new ArgumentNullException("ctx");
            }

            if (_values != null)
            {
                object obj;
                if (_values.TryGetValue(name, out obj))
                {
                    if (obj is FieldInfo)
                    {
                        Debug.Assert(_staticsGetter != null);
                        return ((FieldInfo)obj).GetValue(_staticsGetter(ctx));  // Context.GetStatics<_statics>().FIELD                        
                    }
                    else
                    {
                        return obj;
                    }
                }
            }

            throw new ArgumentException();
        }

        /// <summary>
        /// Gets <see cref="Expression"/> representing constant value.
        /// </summary>
        /// <param name="name">Class constant name.</param>
        /// <param name="ctx">Expression representing current <see cref="Context"/>.</param>
        /// <returns><see cref="Expression"/> instance or <c>null</c> if constant does not exist.</returns>
        internal Expression Bind(string name, Expression ctx)
        {
            if (ctx == null)
            {
                throw new ArgumentNullException("ctx");
            }

            if (_values != null)
            {
                object obj;
                if (_values.TryGetValue(name, out obj))
                {
                    if (obj is FieldInfo)
                    {
                        var fld = (FieldInfo)obj;

                        // Context.GetStatics<_statics>().FIELD
                        var getstatics = BinderHelpers.GetStatic_T_Method(fld.DeclaringType);
                        return Expression.Field(Expression.Call(ctx, getstatics), fld);
                    }
                    else
                    {
                        return Expression.Constant(obj);
                    }
                }
            }

            return null;
        }
    }
}
