#nullable enable

using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Peachpie.Library.PDO
{
    partial class PDO
    {
        /// <summary>
        /// Lazily initialized set of explicitly set attributes.
        /// If not specified, the attributed has its default value.
        /// </summary>
        private protected Dictionary<PDO_ATTR, PhpValue>? _lazyAttributes;

        /// <summary>
        /// Lazily initializes <see cref="_lazyAttributes"/>.
        /// </summary>
        private protected Dictionary<PDO_ATTR, PhpValue> GetOrCreateAttributes() => _lazyAttributes ??= new Dictionary<PDO_ATTR, PhpValue>();

        /// <summary>
        /// "Oracle" handling of NULL and empty strings.
        /// </summary>
        private protected PDO_NULL _oracle_nulls; // = PDO_NULL.NULL_NATURAL; // 0

        /// <summary>
        /// <c>ATTR_STRINGIFY_FETCHES</c> option.
        /// Instructs the ResultResource to convert values to strings.
        /// </summary>
        [PhpHidden]
        public bool Stringify { get; set; } = true;

        private protected bool TryGetAttribute(PDO_ATTR attribute, out PhpValue value)
        {
            if (_lazyAttributes != null && _lazyAttributes.TryGetValue(attribute, out value))
            {
                return true;
            }

            // default values:
            switch (attribute)
            {
                case PDO_ATTR.ATTR_DRIVER_NAME: value = Driver.Name; return true;
                case PDO_ATTR.ATTR_SERVER_VERSION: value = Connection.ServerVersion; return true;
                case PDO_ATTR.ATTR_CLIENT_VERSION: value = Driver.ClientVersion; return true;
                case PDO_ATTR.ATTR_ORACLE_NULLS: value = (int)_oracle_nulls; return true;

                case PDO_ATTR.ATTR_AUTOCOMMIT: value = PhpValue.True; return true;
                case PDO_ATTR.ATTR_PREFETCH: value = 0; return true;
                case PDO_ATTR.ATTR_TIMEOUT: value = 30; return true;
                case PDO_ATTR.ATTR_ERRMODE: value = ERRMODE_SILENT; return true;
                case PDO_ATTR.ATTR_SERVER_INFO: value = PhpValue.Null; return true;
                case PDO_ATTR.ATTR_CONNECTION_STATUS: value = PhpValue.Null; return true;
                case PDO_ATTR.ATTR_CASE: value = (int)PDO_CASE.CASE_LOWER; return true;
                case PDO_ATTR.ATTR_CURSOR_NAME: value = PhpValue.Null; return true;
                case PDO_ATTR.ATTR_CURSOR: value = PhpValue.Null; return true;
                case PDO_ATTR.ATTR_PERSISTENT: value = PhpValue.False; return true;
                case PDO_ATTR.ATTR_STATEMENT_CLASS: value = PhpValue.Null; return true;
                case PDO_ATTR.ATTR_FETCH_CATALOG_NAMES: value = PhpValue.Null; return true;
                case PDO_ATTR.ATTR_FETCH_TABLE_NAMES: value = PhpValue.Null; return true;
                case PDO_ATTR.ATTR_STRINGIFY_FETCHES: value = this.Stringify; return true;
                case PDO_ATTR.ATTR_MAX_COLUMN_LEN: value = PhpValue.Null; return true;
                case PDO_ATTR.ATTR_DEFAULT_FETCH_MODE: value = 0; return true;
                case PDO_ATTR.ATTR_EMULATE_PREPARES: value = PhpValue.False; return true;

                default:
                    // driver specific:
                    if (attribute > PDO_ATTR.ATTR_DRIVER_SPECIFIC)
                    {
                        value = Driver.GetAttribute(this, attribute);
                        return Operators.IsSet(value);
                    }

                    //TODO : what to do on unknown attribute ?
                    value = PhpValue.Null;
                    return false;
            }
        }

        /// <summary>
        /// Retrieve a database connection attribute
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <returns>A successful call returns the value of the requested PDO attribute. An unsuccessful call returns <c>null</c>.</returns>
        public virtual PhpValue getAttribute(PDO_ATTR attribute) => TryGetAttribute(attribute, out var value) ? value : PhpValue.Null;

        /// <summary>
        /// Set an attribute.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public virtual bool setAttribute(PDO_ATTR attribute, PhpValue value)
        {
            long l; // temp value

            switch (attribute)
            {
                //readonly
                case PDO_ATTR.ATTR_SERVER_INFO:
                case PDO_ATTR.ATTR_SERVER_VERSION:
                case PDO_ATTR.ATTR_CLIENT_VERSION:
                case PDO_ATTR.ATTR_CONNECTION_STATUS:
                case PDO_ATTR.ATTR_DRIVER_NAME:
                    return false;

                //boolean

                case PDO_ATTR.ATTR_AUTOCOMMIT:
                case PDO_ATTR.ATTR_EMULATE_PREPARES:
                    GetOrCreateAttributes()[attribute] = value.ToBoolean();
                    return true;

                case PDO_ATTR.ATTR_STRINGIFY_FETCHES:
                    Driver.TrySetStringifyFetches(this, value.ToBoolean());
                    return true; // always returns TRUE

                //strict positif integers

                case PDO_ATTR.ATTR_PREFETCH:
                case PDO_ATTR.ATTR_TIMEOUT:
                    // TODO: strict positif integers
                    GetOrCreateAttributes()[attribute] = value.GetValue().DeepCopy();
                    return true;

                //remaining

                case PDO_ATTR.ATTR_ERRMODE:
                    l = value.ToLong();
                    if (Enum.IsDefined(typeof(PDO_ERRMODE), (int)l))
                    {
                        GetOrCreateAttributes()[attribute] = l;
                        return true;
                    }
                    else
                    {
                        // Warning: PDO::setAttribute(): SQLSTATE[HY000]: General error: invalid error mode
                        // TODO: this.RaiseError( ... ) ?
                        PhpException.InvalidArgument(nameof(value));
                        return false;
                    }
                case PDO_ATTR.ATTR_CASE:
                    l = value.ToLong();
                    if (Enum.IsDefined(typeof(PDO_CASE), (int)l))
                    {
                        GetOrCreateAttributes()[attribute] = l;
                        return true;
                    }
                    return false;
                case PDO_ATTR.ATTR_CURSOR:
                    l = value.ToLong();
                    if (Enum.IsDefined(typeof(PDO_CURSOR), (int)l))
                    {
                        GetOrCreateAttributes()[attribute] = l;
                        return true;
                    }
                    return false;
                case PDO_ATTR.ATTR_DEFAULT_FETCH_MODE:
                    l = value.ToLong();
                    if (Enum.IsDefined(typeof(PDO_FETCH), (int)l))
                    {
                        GetOrCreateAttributes()[attribute] = l;
                        return true;
                    }
                    return false;

                case PDO_ATTR.ATTR_STATEMENT_CLASS:
                    if (value.IsPhpArray(out var arr) && arr != null && arr.Count != 0)
                    {
                        GetOrCreateAttributes()[attribute] = arr.DeepCopy();
                        return true;
                    }
                    return false;

                case PDO_ATTR.ATTR_ORACLE_NULLS:
                    if (value.IsLong(out l))
                    {
                        Debug.Assert(l == (long)PDO_NULL.NULL_NATURAL, "nonstandard ATTR_ORACLE_NULLS is not yet supported");
                        _oracle_nulls = (PDO_NULL)l;
                        return true;
                    }
                    else
                    {
                        HandleError("attribute value must be an integer");
                        return false;
                    }

                case PDO_ATTR.ATTR_FETCH_CATALOG_NAMES:
                case PDO_ATTR.ATTR_FETCH_TABLE_NAMES:
                case PDO_ATTR.ATTR_MAX_COLUMN_LEN:
                case PDO_ATTR.ATTR_PERSISTENT:
                    throw new NotImplementedException($"setAttribute({attribute})");

                //statement only
                case PDO_ATTR.ATTR_CURSOR_NAME:
                    return false;

                default:

                    // driver specific
                    try
                    {
                        if (attribute >= PDO_ATTR.ATTR_DRIVER_SPECIFIC)
                        {
                            return Driver.TrySetAttribute(GetOrCreateAttributes(), attribute, value);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        this.HandleError(ex);
                        return false;
                    }

                    // invalid attribute:
                    Debug.WriteLine($"PDO_ATTR {attribute} is not known.");
                    return false;
            }

        }
    }
}
