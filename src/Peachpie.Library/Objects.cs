using Pchp.Core;
using Pchp.Core.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Pchp.Library.Resources;
using Pchp.Library.Reflection;
using System.Diagnostics;

namespace Pchp.Library
{
    [PhpExtension(PhpExtensionAttribute.KnownExtensionNames.Core)]
    public static class Objects
    {
        /// <summary>
		/// Tests whether the class given by <paramref name="object"/> is derived from a class given by <paramref name="class_name"/>.
		/// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="object">The object or the name of a class (<see cref="string"/>).</param>
		/// <param name="class_name">The name of a base class or interface.</param>
        /// <param name="allow_string">Whether class name can be used as <paramref name="object"/>. Otherwise only an object instance is allowed.
        /// This can be used to prevent from calling autoloader if the class doesn't exist.</param>
		/// <returns><B>true</B> if <paramref name="object"/> implements or extends <paramref name="class_name"/>, <B>false</B> otherwise.</returns>
		public static bool is_subclass_of(Context ctx, PhpValue @object, string class_name, bool allow_string = true)
        {
            var tinfo = TypeNameOrObjectToType(ctx, @object, allowName: allow_string);
            if (tinfo == null) return false;

            // look for the class, do not use autoload (since PHP 5.1):
            var base_tinfo = ctx.GetDeclaredType(class_name, false);
            if (base_tinfo == null) return false;

            //
            return
                base_tinfo != tinfo &&
                base_tinfo.Type.IsAssignableFrom(tinfo.Type);
        }

        /// <summary>
		/// Tests whether a given class is defined.
		/// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="className">The name of the class.</param>
		/// <param name="autoload">Whether to attempt to call <c>__autoload</c>.</param>
		/// <returns><B>true</B> if the class given by <paramref name="className"/> has been defined,
		/// <B>false</B> otherwise.</returns>
		public static bool class_exists(Context ctx, string className, bool autoload = true)
        {
            if (className.Length == 0)
            {
                return false;
            }

            var info = ctx.GetDeclaredType(className, autoload);
            return info != null && !info.IsInterface;
        }

        /// <summary>
		/// Tests whether a given interface is defined.
		/// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="classname">The name of the interface.</param>
		/// <param name="autoload">Whether to attempt to call <c>__autoload</c>.</param>
		/// <returns><B>true</B> if the interface given by <paramref name="classname"/> has been defined,
		/// <B>false</B> otherwise.</returns>
		public static bool interface_exists(Context ctx, string classname, bool autoload = true)
        {
            var info = ctx.GetDeclaredType(classname, autoload);
            return info != null && info.IsInterface;
        }

        /// <summary>
		/// Tests whether a given trait is defined.
		/// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="traitname">The name of the trait.</param>
		/// <param name="autoload">Whether to attempt to call <c>__autoload</c>.</param>
		/// <returns><B>true</B> if the trait given by <paramref name="traitname"/> has been defined,
		/// <B>false</B> otherwise.</returns>
		public static bool trait_exists(Context ctx, string traitname, bool autoload = true)
        {
            var info = ctx.GetDeclaredType(traitname, autoload);
            return info != null && info.IsTrait;
        }

        /// <summary>
        /// Returns the name of the callers class context.
        /// </summary>
        /// <param name="tctx">Current class context.</param>
        /// <returns>Current class name.</returns>
        [return: CastToFalse]
        public static string get_class([ImportValue(ImportValueAttribute.ValueSpec.CallerClass)] string tctx)
        {
            if (tctx == null)
            {
                // Warning: get_class() called without object from outside a class
            }

            return tctx;
        }

        /// <summary>
        /// Returns the name of the class of which the object <paramref name="object"/> is an instance.
        /// </summary>
        /// <param name="object">The object whose class is requested.</param>
        /// <returns><paramref name="object"/>'s class name.</returns>
        [return: CastToFalse]
        public static string get_class(PhpValue @object)
        {
            var obj = @object.AsObject();
            if (obj == null || obj is PhpResource)
            {
                // TODO: Warning: get_class() expects parameter 1 to be object, {PhpVariable.GetTypeName(@object)} given 
                PhpException.InvalidArgumentType(nameof(@object), PhpVariable.TypeNameObject);
                return null; // FALSE
            }

            return PhpVariable.GetClassName(obj);
        }

        /// <summary>
        /// Gets the name of the class the static method is called in.
        /// </summary>
        [return: CastToFalse]
        public static string get_called_class([ImportValue(ImportValueAttribute.ValueSpec.CallerStaticClass)] PhpTypeInfo @static) => @static?.Name;

        /// <summary>
        /// Helper getting declared classes or interfaces.
        /// </summary>
        /// <param name="ctx">Runtime context with declared types.</param>
        /// <param name="interfaces">Whether to list interfaces or classes.</param>
        /// <param name="traits">Whether to list traits.</param>
        static PhpArray get_declared_types(Context ctx, bool interfaces, bool traits)
        {
            var result = new PhpArray();

            foreach (var t in ctx.GetDeclaredTypes())
            {
                if (t.IsInterface == interfaces && t.IsTrait == traits)
                {
                    result.Add(t.Name);
                }
            }

            return result;
        }

        /// <summary>
		/// Returns a <see cref="PhpArray"/> with names of all defined classes (system and user).
		/// </summary>
		/// <returns><see cref="PhpArray"/> of class names.</returns>
		public static PhpArray get_declared_classes(Context ctx) => get_declared_types(ctx, false, false);

        /// <summary>
        /// Returns a <see cref="PhpArray"/> with names of all defined interfaces (system and user).
        /// </summary>
        /// <returns><see cref="PhpArray"/> of interface names.</returns>
        public static PhpArray get_declared_interfaces(Context ctx) => get_declared_types(ctx, true, false);

        /// <summary>
        /// Returns a <see cref="PhpArray"/> with names of all defined traits (system and user).
        /// </summary>
        /// <returns><see cref="PhpArray"/> of traits names.</returns>
        public static PhpArray get_declared_traits(Context ctx) => get_declared_types(ctx, false, true);

        /// <summary>
        /// Retrieves the parent class name for current object from which this function is called.
        /// </summary>
        [return: CastToFalse]
        public static string get_parent_class([ImportValue(ImportValueAttribute.ValueSpec.CallerClass)] RuntimeTypeHandle caller)
        {
            if (caller.Equals(default))
            {
                return null;
            }

            var tinfo = Type.GetTypeFromHandle(caller)?.GetPhpTypeInfo();
            return tinfo.BaseType?.Name;
        }

        /// <summary>
        /// Gets the name of the class from which class given by <paramref name="object"/>
        /// inherits.
        /// </summary>
        /// <remarks>
        /// If the class given by <paramref name="object"/> has no parent in PHP class hierarchy, this method returns <B>false</B>.
        /// </remarks>
        [return: CastToFalse]
        public static string get_parent_class(Context ctx, PhpValue @object)
        {
            var tinfo = TypeNameOrObjectToType(ctx, @object);

            //
            return tinfo?.BaseType?.Name;
        }

        /// <summary>
		/// Tests whether <paramref name="object"/>'s class is derived from a class given by <paramref name="class_name"/>.
		/// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="object">The object to test.</param>
		/// <param name="class_name">The name of the class.</param>
        /// <param name="allow_string">If this parameter set to FALSE, string class name as object is not allowed. This also prevents from calling autoloader if the class doesn't exist.</param>
        /// <returns><B>true</B> if the object <paramref name="object"/> belongs to <paramref name="class_name"/> class or
		/// a class which is a subclass of <paramref name="class_name"/>, <B>false</B> otherwise.</returns>
        public static bool is_a(Context ctx, PhpValue @object, string class_name, bool allow_string = false)
        {
            // first load type of {value}
            PhpTypeInfo tvalue = TypeNameOrObjectToType(ctx, @object, autoload: true, allowName: allow_string);

            // second, load type of {class_name}
            var ctype = ctx.GetDeclaredType(class_name, false);

            // check is_a
            return tvalue != null && ctype != null && ctype.Type.IsAssignableFrom(tvalue.Type);
        }

        /// <summary>
        /// Gets the properties of the given object.
        /// </summary>
        /// <param name="caller">Caller context.</param>
        /// <param name="object"></param>
        /// <returns>Returns an associative array of defined object accessible non-static properties for the specified object in scope.
        /// If a property has not been assigned a value, it will be returned with a NULL value.</returns>
        public static PhpArray get_object_vars([ImportValue(ImportValueAttribute.ValueSpec.CallerClass)] RuntimeTypeHandle caller, object @object)
        {
            Debug.Assert(!(@object is PhpAlias), "obj must be dereferenced");

            if (@object == null)
            {
                return null; // not FALSE since PHP 5.3
            }

            if (@object is stdClass stdclass)
            {
                // optimization for stdClass:
                var arr = stdclass.GetRuntimeFields();
                return (arr != null) ? arr.DeepCopy() : PhpArray.NewEmpty();
            }

            if (@object is PhpResource || @object is __PHP_Incomplete_Class)
            {
                PhpException.InvalidArgumentType(nameof(@object), PhpVariable.TypeNameObject);
                return null;
            }

            var result = PhpArray.NewEmpty();

            foreach (var pair in TypeMembersUtils.EnumerateVisibleInstanceFields(@object, caller))
            {
                result.Add(pair.Key, pair.Value.DeepCopy());
            }

            return result;
        }

        /// <summary>
        /// Returns an array of mangled object properties.
        /// Does not respect property visibility.
        /// </summary>
        /// <returns>Returns an associative array of defined object accessible non-static properties.</returns>
        public static PhpArray get_mangled_object_vars(object @object)
        {
            Debug.Assert(!(@object is PhpAlias), "obj must be dereferenced");

            if (@object == null)
            {
                return null;
            }

            if (@object is stdClass stdclass)
            {
                // optimization for stdClass:
                var arr = stdclass.GetRuntimeFields();
                return (arr != null) ? arr.DeepCopy() : PhpArray.NewEmpty();
            }

            if (@object is PhpResource || @object is __PHP_Incomplete_Class)
            {
                PhpException.InvalidArgumentType(nameof(@object), PhpVariable.TypeNameObject);
                return null;
            }

            var result = PhpArray.NewEmpty();

            foreach (var pair in TypeMembersUtils.EnumerateInstanceFields(
                instance: @object,
                keyFormatter: p => new IntStringKey(p.PropertyName),
                keyFormatter2: key => key))
            {
                result.Add(pair.Key, pair.Value.DeepCopy());
            }

            return result;
        }

        /// <summary>
		/// Get <see cref="PhpTypeInfo"/> from either a class name or a class instance.
        /// </summary>
        /// <returns>Type info instance if object is valid class reference, otherwise <c>null</c>.</returns>
        internal static PhpTypeInfo TypeNameOrObjectToType(Context ctx, PhpValue @object, RuntimeTypeHandle selftype = default(RuntimeTypeHandle), bool autoload = true, bool allowName = true)
        {
            object obj;
            string str;

            if ((obj = (@object.AsObject())) != null)
            {
                return obj.GetPhpTypeInfo();
            }
            else if ((str = PhpVariable.AsString(@object)) != null)
            {
                return allowName ? ctx.GetDeclaredType(str, autoload) : null;
            }
            else
            {
                // other @object types are not handled
                return (selftype.Equals(default) || !@object.IsNull)
                    ? null
                    : Type.GetTypeFromHandle(selftype)?.GetPhpTypeInfo();
            }
        }

        /// <summary>
        /// Get the default properties of the given class.
        /// </summary>
        /// <returns>Returns an associative array of declared properties visible from the current scope, with their default value. The resulting array elements are in the form of varname => value.
        /// In case of an error, it returns <c>FALSE</c>.</returns>
        [return: CastToFalse]
        public static PhpArray get_class_vars(Context ctx, [ImportValue(ImportValueAttribute.ValueSpec.CallerClass)] RuntimeTypeHandle caller, string class_name)
        {
            var tinfo = ctx.GetDeclaredType(class_name, true);
            if (tinfo != null)
            {
                if (tinfo.IsInterface)
                {
                    // interfaces cannot have properties:
                    return PhpArray.NewEmpty();
                }
                else if (tinfo.IsTrait && tinfo.Type.IsGenericTypeDefinition)
                {
                    // construct the generic trait class with <object>
                    tinfo = tinfo.Type.MakeGenericType(typeof(object)).GetPhpTypeInfo();
                }

                var result = new PhpArray();
                var callerType = Type.GetTypeFromHandle(caller);

                // the class has to be instantiated in order to discover default instance property values
                // (the constructor will initialize default properties, user defined constructor will not be called)
                var instanceOpt = tinfo.CreateUninitializedInstance(ctx);

                foreach (var prop in tinfo.GetDeclaredProperties())
                {
                    if (prop.IsVisible(callerType))
                    {
                        // resolve the property value using temporary class instance
                        var value = prop.IsStatic
                            ? prop.GetValue(ctx, null)
                            : (instanceOpt != null)
                                ? prop.GetValue(ctx, instanceOpt)
                                : PhpValue.Null;

                        //
                        result[prop.PropertyName] = value.DeepCopy();
                    }
                }

                return result;
            }
            else
            {
                return null; // false
            }
        }

        /// <summary>
        /// Checks if the class method exists in the given object.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="object">An object instance or a class name.</param>
        /// <param name="method">The method name.</param>
        /// <returns>Returns <c>TRUE</c> if the method given by <paramref name="method"/> has been defined for the given object, <c>FALSE</c> otherwise.</returns>
        public static bool method_exists(Context ctx, PhpValue @object, string method)
        {
            if (@object.IsEmpty || string.IsNullOrEmpty(method))
            {
                return false;
            }

            var tinfo = TypeNameOrObjectToType(ctx, @object);

            return tinfo != null && tinfo.RuntimeMethods[method] != null;
        }

        /// <summary>
		/// Verifies whether a property has been defined for the given object object or class. 
		/// </summary>
        /// <remarks>
		/// If an object is passed in the first parameter, the property is searched among runtime fields as well.
		/// </remarks>
		public static bool property_exists(Context ctx, PhpValue object_or_class, string property_name)
        {
            var tinfo = TypeNameOrObjectToType(ctx, object_or_class);
            if (tinfo == null)
            {
                return false;
            }

            if (tinfo.GetDeclaredProperty(property_name) != null)
            {
                // CT property found
                return true;
            }

            var instance = object_or_class.AsObject();
            if (instance != null)
            {
                var rt = tinfo.GetRuntimeFields(instance);
                if (rt != null && rt.ContainsKey(property_name))
                {
                    // RT property found
                    return true;
                }
            }

            //
            return false;
        }

        /// <summary>
		/// Returns all methods defined in the specified class or class of specified object, and its predecessors.
		/// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="caller">The caller of the method to resolve visible properties properly. Can be UnknownTypeDesc.</param>
		/// <param name="class">The object (<see cref="DObject"/>) or the name of a class
		/// (<see cref="String"/>).</param>
		/// <returns>Array of all methods defined in <paramref name="class"/>.</returns>
		public static PhpArray get_class_methods(Context ctx, [ImportValue(ImportValueAttribute.ValueSpec.CallerClass)] RuntimeTypeHandle caller, PhpValue @class)
        {
            var tinfo = TypeNameOrObjectToType(ctx, @class);
            if (tinfo == null)
            {
                return null;
            }

            //

            var result = new PhpArray();

            foreach (var m in tinfo.RuntimeMethods.EnumerateVisible(Type.GetTypeFromHandle(caller)))
            {
                result.Add((PhpValue)m.Name);
            }

            return result;
        }

        /// <summary>
        /// Creates an alias named <paramref name="alias_name"/> based on the user defined class <paramref name="user_class_name"/>.
        /// The aliased class is exactly the same as the original class.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="user_class_name">Existing original class name.</param>
        /// <param name="alias_name">The alias name for the class.</param>
        /// <param name="autoload">Whether to autoload if the original class is not found. </param>
        /// <returns><c>true</c> on success.</returns>
        public static bool class_alias(Context ctx, string user_class_name, string alias_name, bool autoload = true)
        {
            if (!string.IsNullOrEmpty(user_class_name))
            {
                var type = ctx.GetDeclaredType(user_class_name, autoload);
                if (type != null && type.Name != alias_name)
                {
                    ctx.DeclareType(type, alias_name);
                    return ctx.GetDeclaredType(alias_name, false) == type;
                }
            }
            else
            {
                PhpException.InvalidArgument(nameof(user_class_name), LibResources.arg_null_or_empty);
            }

            return false;
        }
    }

    /// <summary>
    /// Container with SPL object functions.
    /// (they are listed as a part of SPL extensions)
    /// </summary>
    [PhpExtension(Spl.SplExtension.Name)]
    public static class ObjectsSpl
    {
        /// <summary>
		/// Returns a <see cref="PhpArray"/> with keys and values being names of a given class's
		/// base classes.
		/// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="caller">The caller of the method used in case <paramref name="classNameOrObject"/> is NULL.</param>
        /// <param name="classNameOrObject">The object or class name to get base classes of.</param>
		/// <param name="useAutoload"><B>True</B> if autoloading should be used.</param>
		/// <returns>The <see cref="PhpArray"/> with base class names.</returns>
		[return: CastToFalse]
        public static PhpArray class_parents(Context ctx, [ImportValue(ImportValueAttribute.ValueSpec.CallerClass)] RuntimeTypeHandle caller, PhpValue classNameOrObject, bool useAutoload = true)
        {
            var tinfo = Objects.TypeNameOrObjectToType(ctx, classNameOrObject, caller, useAutoload);

            PhpArray result = null;

            if (tinfo != null)
            {
                result = new PhpArray();
                while ((tinfo = tinfo.BaseType) != null)
                {
                    result.Add(tinfo.Name, tinfo.Name);
                }
            }

            return result;
        }

        /// <summary>
        /// This function returns an array with the names of the interfaces that the given class and its parents implement.
        /// </summary>
        [return: CastToFalse]
        public static PhpArray class_implements(Context ctx, [ImportValue(ImportValueAttribute.ValueSpec.CallerClass)] RuntimeTypeHandle caller, PhpValue classNameOrObject, bool useAutoload = true)
        {
            PhpArray result = null;

            var tinfo = Objects.TypeNameOrObjectToType(ctx, classNameOrObject, caller, useAutoload);
            if (tinfo != null)
            {
                result = new PhpArray();
                foreach (var iface in tinfo.Type.ImplementedInterfaces)
                {
                    if (!iface.IsHiddenType())
                    {
                        var ifaceinfo = iface.GetPhpTypeInfo();
                        result.Add(ifaceinfo.Name, ifaceinfo.Name);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// This function returns an array with the names of the traits that the given class uses.
        /// This does however not include any traits used by a parent class.
        /// </summary>
        [return: CastToFalse]
        public static PhpArray class_uses(Context ctx, [ImportValue(ImportValueAttribute.ValueSpec.CallerClass)] RuntimeTypeHandle caller, PhpValue classNameOrObject, bool useAutoload = true)
        {
            PhpArray result = null;

            var tinfo = Objects.TypeNameOrObjectToType(ctx, classNameOrObject, caller, useAutoload);
            if (tinfo != null)
            {
                result = new PhpArray();
                foreach (var trait in tinfo.GetImplementedTraits())
                {
                    result.Add(trait.Name, trait.Name);
                }
            }

            return result;
        }
    }

    #region class WeakReference

    /// <summary>
    /// Weak references allow to retain a reference to an object
    /// which does not prevent the object from being garbage collected.
    /// </summary>
    [PhpType(PhpTypeAttribute.PhpTypeName.NameOnly), PhpExtension(PhpExtensionAttribute.KnownExtensionNames.Core)]
    public sealed class WeakReference
    {
        [PhpHidden]
        readonly WeakReference<object> _value;

        private WeakReference(object value)
        {
            _value = new WeakReference<object>(value);
        }

        /// <summary>
        /// Private ctor.
        /// </summary>
        private void __construct()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Create a weak reference
        /// </summary>
        public static WeakReference create(object referent)
        {
            return new WeakReference(referent);
        }

        /// <summary>
        /// Gets a weakly referenced object.
        /// If the object has already been garbage collected, <c>NULL</c> is returned.
        /// </summary>
        public object get()
        {
            _value.TryGetTarget(out var value);
            return value;
        }
    }

    #endregion
}
