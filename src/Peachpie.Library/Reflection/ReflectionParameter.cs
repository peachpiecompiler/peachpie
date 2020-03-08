using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Pchp.Core;
using Pchp.Core.Reflection;

namespace Pchp.Library.Reflection
{
    /// <summary>
    /// The <see cref="ReflectionParameter"/> class retrieves information about function's or method's parameters. 
    /// 
    /// To introspect function parameters, first create an instance of the <see cref="ReflectionFunction"/> or
    /// <see cref="ReflectionMethod"/> classes and then use their <see cref="ReflectionFunctionAbstract.getParameters()"/>
    /// method to retrieve an array of parameters. 
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(ReflectionUtils.ExtensionName)]
    public class ReflectionParameter : Reflector
    {
        #region Fields & Properties

        public string name => _name;

        // `internal` modifier hides fields from PHP reflection:

        internal ReflectionFunctionAbstract _function;
        internal Type _type;
        internal bool _allowsNull;
        internal bool _isVariadic;
        internal string _name;

        /// <summary>Zero-based index of the parameter.</summary>
        internal int _index;
        internal PhpValue? _defaultValue;

        #endregion

        #region Construction

        [PhpFieldsOnlyCtor]
        protected ReflectionParameter() { }

        internal ReflectionParameter(ReflectionFunctionAbstract function, int index, Type type, bool allowsNull, bool isVariadic, string name, PhpValue? defaultValue = default)
        {
            Debug.Assert(function != null);
            Debug.Assert(index >= 0);
            Debug.Assert(!string.IsNullOrEmpty(name));

            _function = function;
            _index = index;
            _type = type;
            _allowsNull = allowsNull;
            _isVariadic = isVariadic;
            _name = name;
            _defaultValue = defaultValue;
        }

        /// <summary>Updates the parameter information with an overloaded parameter information.</summary>
        internal void AddOverload(Type type, bool allowsNull, bool isVariadic, string name, PhpValue? defaultValue = default)
        {
            if (!hasTypeInternal(_type) && hasTypeInternal(type))
            {
                _type = type;
            }

            _allowsNull |= allowsNull;
            
            if (!_defaultValue.HasValue && defaultValue.HasValue)
            {
                _defaultValue = defaultValue;
            }

            if (Core.Reflection.ReflectionUtils.IsAllowedPhpName(_name))
            {
                if (Core.Reflection.ReflectionUtils.IsAllowedPhpName(name))
                {
                    _isVariadic |= isVariadic;
                }
            }
            else
            {
                // previous parameter definition was synthesized,
                // override the reflection info
                _name = name;
                _isVariadic = isVariadic;
            }
        }

        /// <summary>Marks the parameter as optional if not yet.</summary>
        internal void SetOptional()
        {
            if (!_defaultValue.HasValue)
            {
                // set something in here so the parameter will be treated as optional
                _defaultValue = PhpValue.Null;
            }
        }

        internal void SetParameter(ReflectionParameter p)
        {
            _function = p._function;
            _index = p._index;
            _type = p._type;
            _allowsNull = p._allowsNull;
            _isVariadic = p._isVariadic;
            _name = p._name;
            _defaultValue = p._defaultValue;
        }

        public ReflectionParameter(Context ctx, PhpValue/*string|array*/ function, PhpValue/*string|int*/ parameter)
        {
            __construct(ctx, function, parameter);
        }

        public virtual void __construct(Context ctx, PhpValue/*string|array*/ function, PhpValue/*string|int*/ parameter)
        {
            // resolve RoutineInfo:

            PhpTypeInfo declaringclass = null;
            RoutineInfo routine = null;

            var function_str = function.AsString();
            if (function_str != null)
            {
                routine = ctx.GetDeclaredFunction(function_str);
            }
            else
            {
                var function_arr = function.AsArray();
                if (function_arr != null && function_arr.Count == 2)
                {
                    declaringclass = ReflectionUtils.ResolvePhpTypeInfo(ctx, function_arr[0]); // cannot be null
                    routine = declaringclass.RuntimeMethods[function_arr[1].ToStringOrThrow(ctx)];
                }
            }

            if (routine != null)
            {
                var func = (declaringclass == null)
                    ? (ReflectionFunctionAbstract)new ReflectionFunction(routine)
                    : new ReflectionMethod(declaringclass, routine);

                // resolve parameter:
                var parameters = ReflectionUtils.ResolveReflectionParameters(ctx, func, routine.Methods);
                var pstr = parameter.AsString();
                if (pstr != null)
                {
                    SetParameter(parameters.First(p => p._name == pstr));
                    return;
                }
                else
                {
                    if (parameter.IsLong(out long index) && index < parameters.Count && index >= 0)
                    {
                        SetParameter(parameters[(int)index]);
                        return;
                    }
                }
            }

            throw new ReflectionException();
        }

        #endregion

        public bool allowsNull() => _allowsNull;

        public bool canBePassedByValue() { throw new NotImplementedException(); }

        //private void __clone() { throw new NotImplementedException(); }

        public static string export(string function, string parameter, bool @return = false) { throw new NotImplementedException(); }

        public ReflectionClass getClass() =>
            (hasTypeInternal(_type) && Core.Reflection.ReflectionUtils.IsPhpClassType(_type))
                ? new ReflectionClass(_type.GetPhpTypeInfo())
                : null;

        public ReflectionClass getDeclaringClass() => (_function is ReflectionMethod m) ? new ReflectionClass(m._tinfo) : null;

        public ReflectionFunctionAbstract getDeclaringFunction() => _function;

        public PhpValue getDefaultValue() => _defaultValue.HasValue ? _defaultValue.Value.DeepCopy() : throw new ReflectionException();

        public string getDefaultValueConstantName() => null; // we don't know

        public string getName() => name;

        public int getPosition() => _index;

        public ReflectionType getType() => hasTypeInternal(_type) ? new ReflectionNamedType(_type, !_allowsNull) : null;

        public bool hasType() => hasTypeInternal(_type);

        public bool isArray() => _type == typeof(PhpArray);

        public bool isCallable() => _type == typeof(IPhpCallable);

        public bool isDefaultValueAvailable() => _defaultValue.HasValue; // value is initialized

        public bool isDefaultValueConstant() => false; // we don't know

        public bool isOptional() => _defaultValue.HasValue;

        public bool isPassedByReference() => _type == typeof(PhpAlias);

        public bool isVariadic() => _isVariadic;

        public virtual string __toString() => $"Parameter #{_index} [ <{(_defaultValue.HasValue ? "optional" : "required")}>{_debugTypeName} ${_name}{_debugDefaultValue} ]";

        public override string ToString() => __toString();

        private protected static bool hasTypeInternal(Type t) => t != null && t != typeof(PhpValue) && t != typeof(PhpAlias) && t != typeof(PhpValue[]) && t != typeof(PhpAlias[]);

        private protected string _debugTypeName => string.Empty; // TODO: " {typename}{or NULL}"
        private protected string _debugDefaultValue => _defaultValue.HasValue ? $" = {_defaultValue.Value.DisplayString}" : string.Empty;
    }
}
