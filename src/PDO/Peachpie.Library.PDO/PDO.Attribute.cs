using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peachpie.Library.PDO
{
    partial class PDO
    {
        /// <summary>
        /// Retrieve a database connection attribute
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <returns></returns>
        public virtual PhpValue getAttribute(PDO_ATTR attribute)
        {
            switch (attribute)
            {
                case PDO_ATTR.ATTR_DRIVER_NAME:
                    return Driver.Name;

                case PDO_ATTR.ATTR_SERVER_VERSION:
                    return Connection.ServerVersion;

                case PDO_ATTR.ATTR_CLIENT_VERSION:
                    return Driver.ClientVersion;

                default:
                    if (m_attributes.TryGetValue(attribute, out var value))
                    {
                        return value;
                    }
                    break;
            }

            if (attribute > PDO_ATTR.ATTR_DRIVER_SPECIFIC)
            {
                return Driver.GetAttribute(this, attribute);
            }

            //TODO : what to do on unknown attribute ?
            return PhpValue.Null;
        }

        /// <summary>
        /// Set an attribute.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public virtual bool setAttribute(PDO_ATTR attribute, PhpValue value)
        {
            try
            {
                if (attribute >= PDO_ATTR.ATTR_DRIVER_SPECIFIC)
                {
                    return Driver.TrySetAttribute(m_attributes, attribute, value);
                }
            }
            catch (System.Exception ex)
            {
                this.HandleError(ex);
                return false;
            }

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
                    m_attributes[attribute] = value;
                    return true;

                //strict positif integers

                case PDO_ATTR.ATTR_PREFETCH:
                case PDO_ATTR.ATTR_TIMEOUT:
                    m_attributes[attribute] = value;
                    return true;

                //remaining

                case PDO_ATTR.ATTR_ERRMODE:
                    l = value.ToLong();
                    if (Enum.IsDefined(typeof(PDO_ERRMODE), (int)l))
                    {
                        m_attributes[attribute] = l;
                        return true;
                    }
                    else
                    {
                        // Warning: PDO::setAttribute(): SQLSTATE[HY000]: General error: invalid error mode
                        PhpException.InvalidArgument(nameof(value));
                        return false;
                    }
                case PDO_ATTR.ATTR_CASE:
                    l = value.ToLong();
                    if (Enum.IsDefined(typeof(PDO_CASE), (int)l))
                    {
                        m_attributes[attribute] = l;
                        return true;
                    }
                    return false;
                case PDO_ATTR.ATTR_CURSOR:
                    l = value.ToLong();
                    if (Enum.IsDefined(typeof(PDO_CURSOR), (int)l))
                    {
                        m_attributes[attribute] = l;
                        return true;
                    }
                    return false;
                case PDO_ATTR.ATTR_DEFAULT_FETCH_MODE:
                    l = value.ToLong();
                    if (Enum.IsDefined(typeof(PDO_FETCH), (int)l))
                    {
                        m_attributes[attribute] = l;
                        return true;
                    }
                    return false;

                case PDO_ATTR.ATTR_STATEMENT_CLASS:
                    if (value.IsPhpArray(out var arr) && arr.Count != 0)
                    {
                        m_attributes[attribute] = arr.DeepCopy();
                        return true;
                    }
                    return false;

                case PDO_ATTR.ATTR_FETCH_CATALOG_NAMES:
                case PDO_ATTR.ATTR_FETCH_TABLE_NAMES:
                case PDO_ATTR.ATTR_MAX_COLUMN_LEN:
                case PDO_ATTR.ATTR_ORACLE_NULLS:
                case PDO_ATTR.ATTR_PERSISTENT:
                case PDO_ATTR.ATTR_STRINGIFY_FETCHES:
                    throw new NotImplementedException();

                //statement only
                case PDO_ATTR.ATTR_CURSOR_NAME:
                    return false;

                default:
                    return false;
            }

        }
    }
}
