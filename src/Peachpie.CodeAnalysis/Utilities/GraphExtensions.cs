using System;
using System.Collections.Generic;
using System.Text;
using Pchp.CodeAnalysis.Semantics.Graph;

namespace Pchp.CodeAnalysis
{
    internal static class GraphExtensions
    {
        internal static TBlock WithLocalPropertiesFrom<TBlock>(this TBlock self, TBlock other)
            where TBlock : BoundBlock
        {
            self.Tag = other.Tag;
            self.Ordinal = other.Ordinal;
            self.FlowState = other.FlowState?.Clone();

            return self;
        }
    }
}
