using Pchp.Core;

namespace Pchp.Library.Spl
{
    /// <summary>
    /// Throwable is the base interface for any object that can be thrown via a throw statement in PHP 7,
    /// including Error and Exception.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension("Core")]
    public interface Throwable
    {
        /// <summary>
        /// Gets the message.
        /// </summary>
        string getMessage();

        /// <summary>
        /// Gets the exception code.
        /// </summary>
        int getCode();

        /// <summary>
        /// Gets the file in which the exception occurred.
        /// </summary>
        string getFile();

        /// <summary>
        /// Gets the line on which the object was instantiated.
        /// </summary>
        int getLine();

        /// <summary>
        /// Gets the stack trace.
        /// </summary>
        PhpArray getTrace();

        /// <summary>
        /// Gets the stack trace as a string.
        /// </summary>
        string getTraceAsString();

        /// <summary>
        /// Returns the previous Throwable.
        /// </summary>
        Throwable getPrevious();

        /// <summary>
        /// Gets a string representation of the thrown object.
        /// </summary>
        string __toString();
    }
}
