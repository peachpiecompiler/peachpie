using Pchp.Core;
using Pchp.Core.Dynamic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Generic empty class.
/// Used for casting values to an <c>object</c>.
/// </summary>
public sealed class stdClass
{
    /// <summary>
    /// Special field containing runtime fields.
    /// </summary>
    /// <remarks>
    /// The field is recognized by runtime and is not intended for direct use.
    /// </remarks>
    [CompilerGenerated]
    internal PhpArray __peach__runtimeFields;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public stdClass() { }

    /// <summary>
    /// Constructs the object with single runtime field <c>scalar</c>.
    /// </summary>
    /// <param name="scalar">Value of <c>scalar</c> field.</param>
    internal stdClass(PhpValue scalar)
    {
        __peach__runtimeFields = new PhpArray(1)
        {
            { new IntStringKey("scalar"), scalar }
        };
    }
}