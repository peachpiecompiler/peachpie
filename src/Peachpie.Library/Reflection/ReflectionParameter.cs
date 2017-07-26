using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Pchp.Core;

namespace Pchp.Library.Reflection
{
    /// <summary>
    /// The <see cref="ReflectionParameter"/> class retrieves information about function's or method's parameters. 
    /// 
    /// To introspect function parameters, first create an instance of the <see cref="ReflectionFunction"/> or
    /// <see cref="ReflectionMethod"/> classes and then use their <see cref="ReflectionFunctionAbstract.getParameters()"/>
    /// method to retrieve an array of parameters. 
    /// </summary>
    [PhpType("[name]"), PhpExtension(ReflectionUtils.ExtensionName)]
    public class ReflectionParameter : Reflector
    {
        #region Fields & Properties

        public string name => _param.Name;

        /// <summary>
        /// Underlaying parameter information.
        /// Cannot be <c>null</c>.
        /// </summary>
        internal ParameterInfo _param;

        /// <summary>
        /// Whether is there an overload that doesn't need this parameter - it effectively makes it optional.
        /// </summary>
        internal bool _forceOptional;

        #endregion

        #region Construction

        [PhpFieldsOnlyCtor]
        protected ReflectionParameter() { }

        internal ReflectionParameter(ParameterInfo param, bool forceOptional)
        {
            Debug.Assert(param != null);
            _param = param;
            _forceOptional = forceOptional;
        }

        public ReflectionParameter(Context ctx, string function, string parameter)
        {
            __construct(ctx, function, parameter);
        }

        public virtual void __construct(Context ctx, string function, string parameter)
        {
            throw new NotImplementedException();
        }

        #endregion

        public bool allowsNull() { throw new NotImplementedException(); }

        public bool canBePassedByValue() { throw new NotImplementedException(); }

        //private void __clone() { throw new NotImplementedException(); }

        public void __construct(string function, string parameter) { throw new NotImplementedException(); }

        public static string export(string function, string parameter, bool @return = false) { throw new NotImplementedException(); }

        public ReflectionClass getClass() { throw new NotImplementedException(); }

        public ReflectionClass getDeclaringClass() { throw new NotImplementedException(); }

        public ReflectionFunctionAbstract getDeclaringFunction() { throw new NotImplementedException(); }

        public PhpValue getDefaultValue() { throw new NotImplementedException(); }

        public string getDefaultValueConstantName() { throw new NotImplementedException(); }

        public string getName() => name;

        public int getPosition() { throw new NotImplementedException(); }

        public ReflectionType getType() { throw new NotImplementedException(); }

        public bool hasType() { throw new NotImplementedException(); }

        public bool isArray() { throw new NotImplementedException(); }

        public bool isCallable() { throw new NotImplementedException(); }

        public bool isDefaultValueAvailable() { throw new NotImplementedException(); }

        public bool isDefaultValueConstant() { throw new NotImplementedException(); }

        public bool isOptional() => _forceOptional || _param.IsOptional;

        public bool isPassedByReference() { throw new NotImplementedException(); }

        public bool isVariadic() { throw new NotImplementedException(); }

        public string __toString() { throw new NotImplementedException(); }
    }
}
