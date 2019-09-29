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
        public virtual PhpValue getAttribute(int attribute)
        {
            if (m_attributes.TryGetValue((PDO_ATTR)attribute, out var value))
            {
                return value;
            }

            if (attribute > (int)PDO_ATTR.ATTR_DRIVER_SPECIFIC)
            {
                return m_driver.GetAttribute(this, attribute);
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
        public virtual bool setAttribute(int attribute, PhpValue value)
        {
            try
            {
                if (attribute >= (int)PDO_ATTR.ATTR_DRIVER_SPECIFIC)
                {
                    return m_driver.TrySetAttribute(m_attributes, (PDO_ATTR)attribute, value);
                }
            }
            catch (System.Exception ex)
            {
                this.HandleError(ex);
                return false;
            }

            switch ((PDO_ATTR)attribute)
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
                    m_attributes[(PDO_ATTR)attribute] = value;
                    return true;

                //strict positif integers

                case PDO_ATTR.ATTR_PREFETCH:
                case PDO_ATTR.ATTR_TIMEOUT:
                    m_attributes[(PDO_ATTR)attribute] = value;
                    return true;

                //remaining

                case PDO_ATTR.ATTR_ERRMODE:
                    if (value.IsLong(out var errmode) && Enum.IsDefined(typeof(PDO_ERRMODE), errmode))
                    {
                        m_attributes[(PDO_ATTR)attribute] = (PhpValue)errmode;
                        return true;
                    }
                    return false;
                case PDO_ATTR.ATTR_CASE:
                    int caseValue = (int)value.ToLong();
                    if (Enum.IsDefined(typeof(PDO_CASE), caseValue))
                    {
                        m_attributes[(PDO_ATTR)attribute] = (PhpValue)caseValue;
                        return true;
                    }
                    return false;
                case PDO_ATTR.ATTR_CURSOR:
                    int cursorValue = (int)value.ToLong();
                    if (Enum.IsDefined(typeof(PDO_CURSOR), cursorValue))
                    {
                        m_attributes[(PDO_ATTR)attribute] = (PhpValue)cursorValue;
                        return true;
                    }
                    return false;
                case PDO_ATTR.ATTR_DEFAULT_FETCH_MODE:
                    int fetchValue = value.ToInt();
                    if (Enum.IsDefined(typeof(PDO_FETCH), fetchValue))
                    {
                        m_attributes[(PDO_ATTR)attribute] = (PhpValue)fetchValue;
                        return true;
                    }
                    return false;

                case PDO_ATTR.ATTR_FETCH_CATALOG_NAMES:
                case PDO_ATTR.ATTR_FETCH_TABLE_NAMES:
                case PDO_ATTR.ATTR_MAX_COLUMN_LEN:
                case PDO_ATTR.ATTR_ORACLE_NULLS:
                case PDO_ATTR.ATTR_PERSISTENT:
                case PDO_ATTR.ATTR_STATEMENT_CLASS:
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
