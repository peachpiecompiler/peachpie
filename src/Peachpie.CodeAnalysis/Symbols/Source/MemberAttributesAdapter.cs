using Devsense.PHP.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis
{
    static class MemberAttributesAdapter
    {
        public static Accessibility GetAccessibility(this PhpMemberAttributes member)
        {
            if ((member & PhpMemberAttributes.Private) != 0)
                return Accessibility.Private;

            if ((member & PhpMemberAttributes.Protected) != 0)
                return Accessibility.Protected;

            return Accessibility.Public;
        }

        public static bool IsStatic(this PhpMemberAttributes member) => (member & PhpMemberAttributes.Static) != 0;
        public static bool IsSealed(this PhpMemberAttributes member) => (member & PhpMemberAttributes.Final) != 0;
        public static bool IsAbstract(this PhpMemberAttributes member) => (member & PhpMemberAttributes.Abstract) != 0;
    }
}
