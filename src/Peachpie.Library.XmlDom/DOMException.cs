using System;
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;
using Pchp.Core;
using Pchp.Library.Spl;

namespace Peachpie.Library.XmlDom
{
	#region ExceptionCode and constants

	/// <summary>
	/// Enumerates <see cref="DOMException"/> codes.
	/// </summary>
    [PhpHidden]
	public enum ExceptionCode
	{
		/// <summary>
		/// Index or size is negative, or greater than the allowed value. 
		/// </summary>
		IndexOutOfBounds = 1,

		/// <summary>
		/// The specified range of text does not fit into a string.
		/// </summary>
		StringTooLong = 2,

		/// <summary>
		/// A node is inserted somewhere it doesn't belong.
		/// </summary>
		BadHierarchy = 3,

		/// <summary>
		/// A node is used in a different document than the one that created it.
		/// </summary>
		WrongDocument = 4,

		/// <summary>
		/// An invalid or illegal character is specified, such as in a name.
		/// </summary>
		InvalidCharacter = 5,

		/// <summary>
		/// Data is specified for a node which does not support data.
		/// </summary>
		DataNotAllowed = 6,

		/// <summary>
		/// An attempt is made to modify an object where modifications are not allowed.
		/// </summary>
		DomModificationNotAllowed = 7,

		/// <summary>
		/// An attempt is made to reference a node in a context where it does not exist.
		/// </summary>
		NotFound = 8,

		/// <summary>
		/// The implementation does not support the requested type of object or operation.
		/// </summary>
		NotSupported = 9,

		/// <summary>
		/// An attempt is made to add an attribute that is already in use elsewhere.
		/// </summary>
		AttributeInUse = 10,

		/// <summary>
		/// An attempt is made to use an object that is not, or is no longer, usable.
		/// </summary>
		InvalidState = 11,

		/// <summary>
		/// An invalid or illegal string is specified.
		/// </summary>
		SyntaxError = 12,

		/// <summary>
		/// An attempt is made to modify the type of the underlying object.
		/// </summary>
		ModificationNotAllowed = 13,

		/// <summary>
		/// An attempt is made to create or change an object in a way which is incorrect with
		/// regard to namespaces.
		/// </summary>
		NamespaceError = 14,

		/// <summary>
		/// A parameter or an operation is not supported by the underlying object.
		/// </summary>
		InvalidAccess = 15,

		/// <summary>
		/// A call to a method such as <B>insertBefore</B> or <B>removeChild</B> would make the
		/// node invalid with respect to &quot;partial validity&quot;, this exception would be
		/// raised and the operation would not be done. 
		/// </summary>
		ValidationError = 16
	}

    public static class ExceptionConstants
    {
        public const int DOM_INDEX_SIZE_ERR = (int)ExceptionCode.IndexOutOfBounds;
        public const int DOMSTRING_SIZE_ERR = (int)ExceptionCode.StringTooLong;
        public const int DOM_HIERARCHY_REQUEST_ERR = (int)ExceptionCode.BadHierarchy;
        public const int DOM_WRONG_DOCUMENT_ERR = (int)ExceptionCode.WrongDocument;
        public const int DOM_INVALID_CHARACTER_ERR = (int)ExceptionCode.InvalidCharacter;
        public const int DOM_NO_DATA_ALLOWED_ERR = (int)ExceptionCode.DataNotAllowed;
        public const int DOM_NO_MODIFICATION_ALLOWED_ERR = (int)ExceptionCode.DomModificationNotAllowed;
        public const int DOM_NOT_FOUND_ERR = (int)ExceptionCode.NotFound;
        public const int DOM_NOT_SUPPORTED_ERR = (int)ExceptionCode.NotSupported;
        public const int DOM_INUSE_ATTRIBUTE_ERR = (int)ExceptionCode.AttributeInUse;
        public const int DOM_INVALID_STATE_ERR = (int)ExceptionCode.InvalidState;
        public const int DOM_SYNTAX_ERR = (int)ExceptionCode.SyntaxError;
        public const int DOM_INVALID_MODIFICATION_ERR = (int)ExceptionCode.ModificationNotAllowed;
        public const int DOM_NAMESPACE_ERR = (int)ExceptionCode.NamespaceError;
        public const int DOM_INVALID_ACCESS_ERR = (int)ExceptionCode.InvalidAccess;
        public const int DOM_VALIDATION_ERR = (int)ExceptionCode.ValidationError;
    }

	#endregion

	/// <summary>
	/// The exception thrown by the DOM extension.
	/// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
	public sealed partial class DOMException : Pchp.Library.Spl.Exception
    {
		#region Properties

		/// <summary>
		/// Returns the exception code.
		/// </summary>
		public new long code => base.code;

        #endregion

        #region Construction

        public DOMException(string message = "", long code = 0, Throwable previous = null)
        {
            __construct(message, code, previous);
        }

        #endregion

        #region Throw

        /// <summary>
        /// Throws a <see cref="DOMException"/> user exception with the given code.
        /// </summary>
        /// <param name="code">The exception code.</param>
        /// <exception cref="DOMException"/>
        /// <exception cref="InvalidOperationException">If the <paramref name="code"/> is invalid.</exception>
        internal static void Throw(ExceptionCode code)
		{
			string msg;

			switch (code)
			{
				case ExceptionCode.IndexOutOfBounds: msg = Resources.ErrorIndexOutOfBounds; break;
				case ExceptionCode.StringTooLong: msg = Resources.ErrorStringTooLong; break;
				case ExceptionCode.BadHierarchy: msg = Resources.ErrorBadHierarchy; break;
				case ExceptionCode.WrongDocument: msg = Resources.ErrorWrongDocument; break;
				case ExceptionCode.InvalidCharacter: msg = Resources.ErrorInvalidCharacter; break;
				case ExceptionCode.DataNotAllowed: msg = Resources.ErrorDataNotAllowed; break;
				case ExceptionCode.DomModificationNotAllowed: msg = Resources.ErrorDomModificationNotAllowed; break;
				case ExceptionCode.NotFound: msg = Resources.ErrorNotFound; break;
				case ExceptionCode.NotSupported: msg = Resources.ErrorNotSupported; break;
				case ExceptionCode.AttributeInUse: msg = Resources.ErrorAttributeInUse; break;
				case ExceptionCode.InvalidState: msg = Resources.ErrorInvalidState; break;
				case ExceptionCode.SyntaxError: msg = Resources.ErrorSyntaxError; break;
				case ExceptionCode.ModificationNotAllowed: msg = Resources.ErrorModificationNotAllowed; break;
				case ExceptionCode.NamespaceError: msg = Resources.ErrorNamespaceError; break;
				case ExceptionCode.InvalidAccess: msg = Resources.ErrorInvalidAccess; break;
				case ExceptionCode.ValidationError: msg = Resources.ErrorValidationError; break;

				default:
					throw new InvalidOperationException();
			}

			Throw(code, msg);
		}

        /// <summary>
        /// Throws a <see cref="DOMException"/> user exception with the given code and message.
        /// </summary>
        /// <param name="code">The exception code.</param>
        /// <param name="message">The exception message.</param>
        /// <exception cref="DOMException"/>
        internal static void Throw(ExceptionCode code, string message)
		{
			throw new DOMException(message, (int)code);
		}

		#endregion
	}
}
