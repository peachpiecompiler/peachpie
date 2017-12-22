using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Pchp.CodeAnalysis.Symbols
{
    partial class SynthesizedTraitFieldSymbol : SynthesizedFieldSymbol
    {
        readonly PhpPropertyInfo _traitmember;
        readonly SourceFieldSymbol.KindEnum _memberkind;
        readonly FieldSymbol _traitInstanceField;

        public SourceFieldSymbol.KindEnum FieldKind => _memberkind;

        public SynthesizedTraitFieldSymbol(NamedTypeSymbol containing, FieldSymbol traitInstanceField, PhpPropertyInfo info)
            : base(containing, null, info.Symbol.Name, info.Symbol.DeclaredAccessibility, isStatic: false, isReadOnly: false)
        {
            _traitInstanceField = traitInstanceField;
            _traitmember = info;
            _memberkind = info.FieldKind;
        }

        public override bool IsReadOnly => _traitmember.Symbol is FieldSymbol f && f.IsReadOnly;
        public override bool IsStatic => IsConst || _memberkind == SourceFieldSymbol.KindEnum.AppStaticField;
        public override bool IsConst => _traitmember.Symbol is FieldSymbol f && f.IsConst;
        internal override ConstantValue GetConstantValue(bool earlyDecodingWellKnownAttributes)
        {
            if (_traitmember.Symbol is FieldSymbol f)
            {
                return f.GetConstantValue(earlyDecodingWellKnownAttributes);
            }

            return null;
        }
        
        internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound) => _traitmember.Symbol.GetTypeOrReturnType();
    }
}
