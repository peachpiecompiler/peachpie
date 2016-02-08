using System;

namespace Pchp.Syntax
{
    #region InclusionTypes

    /// <summary>
    /// Type of inclusion.
    /// </summary>
    /// <remarks>
    /// The properties of inclusion types are defined by IsXxxInclusion methods.
    /// </remarks>
    public enum InclusionTypes
    {
        Include, IncludeOnce, Require, RequireOnce, Prepended, Appended, RunSilverlight
    }

    #endregion
}
