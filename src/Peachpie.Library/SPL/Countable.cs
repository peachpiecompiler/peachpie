using Pchp.Core;

namespace Pchp.Library.Spl
{
    /// <summary>
    /// Classes implementing Countable can be used with the <c>count()</c> function.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public interface Countable
    {
        /// <summary>
        /// Count elements of an object.
        /// </summary>
        /// <returns>The custom count as an integer.</returns>
        /// <remarks>This method is executed when using the count() function on an object implementing <see cref="Countable"/>.</remarks>
        long count();
    }
}