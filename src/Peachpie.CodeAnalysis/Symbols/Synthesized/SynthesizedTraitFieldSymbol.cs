using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.CodeGen;
using Roslyn.Utilities;

namespace Pchp.CodeAnalysis.Symbols
{
    partial class SynthesizedTraitFieldSymbol : SynthesizedFieldSymbol, IPhpPropertySymbol
    {
        #region IPhpPropertySymbol

        PhpPropertyKind IPhpPropertySymbol.FieldKind => _traitmember.FieldKind;

        TypeSymbol IPhpPropertySymbol.ContainingStaticsHolder => PhpFieldSymbolExtension.RequiresHolder(this, _traitmember.FieldKind) ? this.ContainingType.TryGetStaticsHolder() : null;

        bool IPhpPropertySymbol.RequiresContext => !IsConst;

        TypeSymbol IPhpPropertySymbol.DeclaringType => this.ContainingType;

        #endregion

        readonly IPhpPropertySymbol _traitmember;
        readonly FieldSymbol _traitInstanceField;

        public SynthesizedTraitFieldSymbol(NamedTypeSymbol containing, FieldSymbol traitInstanceField, IPhpPropertySymbol sourceField)
            : base(containing, null, sourceField.Name, sourceField.DeclaredAccessibility, isStatic: false, isReadOnly: false)
        {
            _traitInstanceField = traitInstanceField;
            _traitmember = sourceField;
        }

        public override bool IsReadOnly => _traitmember is FieldSymbol f && f.IsReadOnly;
        public override bool IsStatic => IsConst || _traitmember.FieldKind == PhpPropertyKind.AppStaticField;
        public override bool IsConst => _traitmember is FieldSymbol f && f.IsConst;

        internal override ConstantValue GetConstantValue(bool earlyDecodingWellKnownAttributes)
        {
            if (_traitmember is FieldSymbol f)
            {
                return f.GetConstantValue(earlyDecodingWellKnownAttributes);
            }

            return null;
        }
        
        internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound) => ((Symbol)_traitmember).GetTypeOrReturnType();
    }
}
