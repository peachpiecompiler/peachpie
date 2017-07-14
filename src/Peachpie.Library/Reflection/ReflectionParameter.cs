using System;
using System.Collections.Generic;
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
        public string name
        {
            get { throw new NotImplementedException(); }
        }

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

        public string getName() { throw new NotImplementedException(); }

        public int getPosition() { throw new NotImplementedException(); }

        public ReflectionType getType() { throw new NotImplementedException(); }

        public bool hasType() { throw new NotImplementedException(); }

        public bool isArray() { throw new NotImplementedException(); }

        public bool isCallable() { throw new NotImplementedException(); }

        public bool isDefaultValueAvailable() { throw new NotImplementedException(); }

        public bool isDefaultValueConstant() { throw new NotImplementedException(); }

        public bool isOptional() { throw new NotImplementedException(); }

        public bool isPassedByReference() { throw new NotImplementedException(); }

        public bool isVariadic() { throw new NotImplementedException(); }

        public string __toString() { throw new NotImplementedException(); }
    }
}
