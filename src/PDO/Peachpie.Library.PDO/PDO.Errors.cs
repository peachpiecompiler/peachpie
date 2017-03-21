using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peachpie.Library.PDO
{
    partial class PDO
    {
        private PhpValue m_errorCode;
        private PhpValue m_errorInfo;

        /// <summary>
        /// Clears the error.
        /// </summary>
        [PhpHidden]
        public void ClearError()
        {
            this.m_errorCode = PhpValue.Null;
            this.m_errorInfo = PhpValue.Null;
        }

        /// <summary>
        /// Handles the error.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <exception cref="Peachpie.Library.PDO.PDOException">
        /// </exception>
        [PhpHidden]
        public void HandleError(System.Exception ex)
        {
            PDO_ERRMODE mode = (PDO_ERRMODE)this.m_attributes[PDO_ATTR.ATTR_ERRMODE];
            //TODO : fill errorCode and errorInfo
            switch (mode)
            {
                case PDO_ERRMODE.ERRMODE_SILENT:
                    break;
                case PDO_ERRMODE.ERRMODE_WARNING:
                    this.m_ctx.Throw(PhpError.E_WARNING, ex.Message);
                    break;
                case PDO_ERRMODE.ERRMODE_EXCEPTION:
                    if (ex is Pchp.Library.Spl.Exception)
                    {
                        var pex = (Pchp.Library.Spl.Exception)ex;
                        throw new PDOException(pex.Message, pex.getCode(), pex);
                    }
                    else
                    {
                        throw new PDOException(ex.GetType().Name + ": " + ex.Message);
                    }
            }
        }

        /// <inheritDoc />
        public PhpValue errorCode()
        {
            return this.m_errorCode;
        }

        /// <inheritDoc />
        public PhpValue errorInfo()
        {
            return this.m_errorInfo;
        }
    }
}
