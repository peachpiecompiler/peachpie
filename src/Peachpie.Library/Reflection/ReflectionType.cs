using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;
using Pchp.Core.Reflection;

namespace Pchp.Library.Reflection
{
    /// <summary>
    /// The <see cref="ReflectionType"/> class reports information about a function's return type.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(ReflectionUtils.ExtensionName)]
    public class ReflectionType
    {
        public virtual bool allowsNull() { throw new NotImplementedException(); }

        public virtual bool isBuiltin() { throw new NotImplementedException(); }

        [Obsolete]
        public virtual string __toString() { throw new NotImplementedException(); }

#pragma warning disable CS0612 // Type or member is obsolete
        [PhpHidden]
        public override string ToString() => __toString();
#pragma warning restore CS0612 // Type or member is obsolete
    }

    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(ReflectionUtils.ExtensionName)]
    public class ReflectionNamedType : ReflectionType
    {
        private protected readonly Type _type;
        private protected readonly bool _notNullFlag;

        private protected static bool ResolvePhpType(Type type, out string name, out bool builtin, out bool nullable)
        {
            if (type == null)
            {
                name = null;
                builtin = false;
                nullable = true;
                return false;
            }

            if (type.IsByRef)
            {
                type = type.GetElementType();
            }

            nullable = !type.IsValueType;

            if (type == typeof(long) || type == typeof(int) || type.IsEnum)
            {
                name = PhpVariable.TypeNameInt;
                builtin = true;
            }
            else if (type == typeof(double))
            {
                name = PhpVariable.TypeNameDouble;
                builtin = true;
            }
            else if (type == typeof(bool))
            {
                name = PhpVariable.TypeNameBool;
                builtin = true;
            }
            else if (type == typeof(string) || type == typeof(PhpString))
            {
                name = PhpVariable.TypeNameString;
                builtin = true;
            }
            else if (type == typeof(PhpArray) || type.IsArray)
            {
                name = PhpArray.PhpTypeName;
                builtin = true;
            }
            else if (type == typeof(void))
            {
                name = PhpVariable.TypeNameVoid;
                builtin = true;
            }
            else if (type == typeof(PhpAlias) || type == typeof(PhpValue))
            {
                name = null;
                builtin = false;
                return false;
            }
            else if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                nullable = true;
                return ResolvePhpType(type.GetGenericArguments()[0], out name, out builtin, out var _);
            }
            else
            {
                var tinfo = type.GetPhpTypeInfo();
                name = tinfo.Name;
                builtin = false;
                nullable = true;
            }

            return true;
        }

        private protected bool ResolvePhpType(out string name, out bool builtin, out bool allowsNull)
        {
            if (ResolvePhpType(_type, out name, out builtin, out allowsNull))
            {
                allowsNull &= !_notNullFlag;
                return true;
            }
            else
            {
                return false;
            }
        }

        internal ReflectionNamedType(Type type, bool notNullFlag = false)
        {
            _type = type ?? throw new ArgumentNullException(nameof(type));
            _notNullFlag = notNullFlag;
        }

        public virtual string getName() => ResolvePhpType(out var name, out var _, out var _) ? name : "mixed";

        public override bool isBuiltin() => ResolvePhpType(out var _, out var builtin, out var _) && builtin;

        public override bool allowsNull() => ResolvePhpType(out var _, out var _, out var nullable) && nullable;

        [Obsolete]
        public override string __toString() => getName();
    }

    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(ReflectionUtils.ExtensionName)]
    public class ReflectionUnionType : ReflectionType
    {
        public override bool allowsNull() => throw new NotImplementedException();

        public override bool isBuiltin() => false;

        public PhpArray/*<ReflectionNamedType >*/getTypes() => throw new NotImplementedException();
    }

}