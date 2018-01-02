using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.CodeGen;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// The field kind.
    /// </summary>
    public enum PhpPropertyKind
    {
        InstanceField,
        StaticField,
        AppStaticField,
        ClassConstant,
    }

    /// <summary>
    /// Describes a PHP property.
    /// </summary>
    interface IPhpPropertySymbol : IPhpValue
    {
        /// <summary>
        /// PHP property kind.
        /// </summary>
        PhpPropertyKind FieldKind { get; }

        /// <summary>
        /// In case field is contained in <c>__statics</c> holder class, gets its type.
        /// Otherwise <c>null</c>.
        /// </summary>
        TypeSymbol ContainingStaticsHolder { get; }

        /// <summary>
        /// Whether initialization of the field requires reference to runtime context.
        /// </summary>
        bool RequiresContext { get; }

        /// <summary>
        /// Type declaring this PHP property.
        /// </summary>
        TypeSymbol DeclaringType { get; }

        /// <summary>
        /// Emits initialization of the field.
        /// </summary>
        /// <param name="cg"></param>
        void EmitInit(CodeGenerator cg);

        /// <summary>
        /// PHP property visibility.
        /// </summary>
        Accessibility DeclaredAccessibility { get; }

        /// <summary>
        /// PHP property name,
        /// </summary>
        string Name { get; }
    }
}
