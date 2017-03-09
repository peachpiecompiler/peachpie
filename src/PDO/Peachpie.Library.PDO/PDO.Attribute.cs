using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peachpie.Library.PDO
{
    partial class PDO
    {
        /// <inheritDoc />
        public PhpValue getAttribute(int attribute)
        {
            if (Enum.IsDefined(typeof(PDO_ATTR), attribute))
            {
                object value = this.getAttribute((PDO_ATTR)attribute);
                return PhpValue.FromClr(value);
            }

            if (attribute > (int)PDO_ATTR.ATTR_DRIVER_SPECIFIC)
            {
                return this.m_driver.GetAttribute(this, attribute);
            }

            //TODO : what to do on unknown attribute ?
            return PhpValue.Null;
        }

        /// <summary>
        /// Gets the attribute.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <returns></returns>
        [PhpHidden]
        public object getAttribute(PDO_ATTR attribute)
        {
            if (this.m_attributes.ContainsKey(attribute))
            {
                return this.m_attributes[attribute];
            }

            //TODO : what to do on unknown attribute ?
            return PhpValue.Null;
        }

        /// <inheritDoc />
        public bool setAttribute(int attribute, PhpValue value)
        {
            //if (Enum.IsDefined(typeof(PDO_ATTR), attribute))
            //{
            //    object value = this.getAttribute((PDO_ATTR)attribute);
            //    return PhpValue.FromClr(value);
            //}

            //if (attribute > (int)PDO_ATTR.ATTR_DRIVER_SPECIFIC)
            //{
            //    return this.m_driver.GetAttribute(this, attribute);
            //}

            ////TODO : what to do on unknown attribute ?
            //return PhpValue.Null;
            return this.setAttribute((PDO_ATTR)attribute, value);
        }

        /// <summary>
        /// Sets the attribute.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        [PhpHidden]
        public bool setAttribute(PDO_ATTR attribute, PhpValue value)
        {
            try
            {
                if ((int)attribute >= (int)PDO_ATTR.ATTR_DRIVER_SPECIFIC)
                {
                    return this.m_driver.TrySetAttribute(this.m_attributes, attribute, value);
                }
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
                        this.m_attributes.Set(attribute, value.ToBoolean());
                        return true;


                    //strict positif integers
                    case PDO_ATTR.ATTR_PREFETCH:
                    case PDO_ATTR.ATTR_TIMEOUT:
                        this.m_attributes.Set(attribute, (int)value.ToLong());
                        return true;

                    //remaining

                    case PDO_ATTR.ATTR_ERRMODE:
                        int errmodeValue = (int)value.ToLong();
                        if (Enum.IsDefined(typeof(PDO_ERRMODE), errmodeValue))
                        {
                            this.m_attributes.Set(attribute, (PDO_ERRMODE)errmodeValue);
                            return true;
                        }
                        return false;
                    case PDO_ATTR.ATTR_CASE:
                        int caseValue = (int)value.ToLong();
                        if (Enum.IsDefined(typeof(PDO_CASE), caseValue))
                        {
                            this.m_attributes.Set(attribute, (PDO_CASE)caseValue);
                            return true;
                        }
                        return false;
                    case PDO_ATTR.ATTR_CURSOR:
                        int cursorValue = (int)value.ToLong();
                        if (Enum.IsDefined(typeof(PDO_CURSOR), cursorValue))
                        {
                            this.m_attributes.Set(attribute, (PDO_CURSOR)cursorValue);
                            return true;
                        }
                        return false;
                    case PDO_ATTR.ATTR_DEFAULT_FETCH_MODE:
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
            catch (System.Exception ex)
            {
                this.HandleError(ex);
                return false;
            }
        }
    }
}
