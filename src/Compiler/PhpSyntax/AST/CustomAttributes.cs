using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Pchp.Syntax.Parsers;
using System.Reflection.Emit;
using System.Reflection;

namespace Pchp.Syntax.AST
{
    [Flags]
    public enum PhpAttributeTargets
    {
        Assembly = 1,
        Function = 2,
        Method = 4,
        Class = 8,
        Interface = 16,
        Property = 32,
        Constant = 64,
        Parameter = 128,
        ReturnValue = 256,
        GenericParameter = 512,

        Routines = Function | Method,
        Types = Class | Interface,
        ClassMembers = Method | Property | Constant,

        All = Assembly | Function | Method | Class | Interface | Property | Constant | Parameter | ReturnValue | GenericParameter
    }

    public enum SpecialAttributes
    {
        AttributeUsage,
        AppStatic,
        Export,
        Out
    }

    #region CustomAttributes

    public sealed class CustomAttributes : AstNode
    {
        public List<CustomAttribute> Attributes { get { return attributes; } }
        private List<CustomAttribute> attributes;

        /// <summary>
        /// Creates a set of custom attributes.
        /// </summary>
        public CustomAttributes(List<CustomAttribute> attributes)
        {
            this.attributes = attributes;
        }

        internal void Merge(CustomAttributes other)
        {
            if (other == null || other.attributes == null)
                return;

            if (attributes == null || attributes.Count == 0)
            {
                attributes = other.attributes;
            }
            else
            {
                attributes.AddRange(other.attributes);
            }

            other.attributes = null;
        }

        internal static void Merge(AstNode node, CustomAttributes otherattributes)
        {
            if (otherattributes != null)
            {
                var attributes = node.GetCustomAttributes();
                if (attributes == null)
                    node.SetCustomAttributes(attributes = new CustomAttributes(null));

                attributes.Merge(otherattributes);
            }
        }
    }

    public static class CustomAttributesHelper
    {
        public static CustomAttributes GetCustomAttributes(this IPropertyCollection/*!*/properties)
        {
            return properties[typeof(CustomAttributes)] as CustomAttributes;
        }
        public static void SetCustomAttributes(this IPropertyCollection/*!*/properties, CustomAttributes attributes)
        {
            if (attributes != null)
                properties[typeof(CustomAttributes)] = attributes;
            else
                properties.RemoveProperty(typeof(CustomAttributes));
        }
    }

    #endregion

    #region CustomAttribute

    public sealed class CustomAttribute : LangElement
    {
        #region Nested Types: TargetSelectors

        /// <summary>
        /// Available target selectors. Lowercased names are reported to the user.
        /// The mapping to the <see cref="AttributeTargets"/> is used for correct usage checking.
        /// </summary>
        public enum TargetSelectors
        {
            Default = AttributeTargets.All,
            Return = AttributeTargets.ReturnValue,
            Assembly = AttributeTargets.Assembly,
            Module = AttributeTargets.Module
        }

        #endregion

        public TargetSelectors TargetSelector { get { return targetSelector; } internal /* friend Parser */ set { targetSelector = value; } }
        private TargetSelectors targetSelector;

        public QualifiedName QualifiedName { get { return qualifiedName; } }
        private QualifiedName qualifiedName;

        public CallSignature CallSignature { get { return callSignature; } }
        private CallSignature callSignature;

        public List<NamedActualParam>/*!*/ NamedParameters { get { return namedParameters; } }
        private List<NamedActualParam>/*!*/ namedParameters;

        public CustomAttribute(Text.Span span, QualifiedName qualifiedName, List<ActualParam>/*!*/ parameters,
                List<NamedActualParam>/*!*/ namedParameters)
            : base(span)
        {
            this.qualifiedName = qualifiedName;
            this.namedParameters = namedParameters;
            this.callSignature = new CallSignature(parameters, TypeRef.EmptyList);
        }

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitCustomAttribute(this);
        }
    }

    #endregion
}
