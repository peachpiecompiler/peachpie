using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Pchp.Core;
using Pchp.Core.Reflection;

namespace Peachpie.Library.Scripting
{
    [PhpExtension("Core")]
    public static class PhpFunctions
    {
        const string _createFunctionTemplate = "function {0} ({1}) {{ {2} }}";

        /// <summary>
        /// CLR name of the lambda function also internal PHP name if <c>__FUNCTION__</c> is used inside.
        /// </summary>
        const string _lambdaFuncName = "__lambda_func";

        /// <summary>
        /// Name of the lambda when registered in <see cref="Context"/>.
        /// </summary>
        const string _lambdaFormatString = "\0lambda_{0}";

        /// <summary>
        /// Creates an anonymous function from the parameters passed, and returns a unique name for it.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="args">The function arguments.</param>
        /// <param name="code">the function body.</param>
        /// <returns>Returns a unique function name as a string, or <c>FALSE</c> on error.</returns>
        [Obsolete]
        [return: CastToFalse]
        public static string create_function(Context ctx, string args, string code)
        {
            // prepare function script
            var data = ScriptingContext.EnsureContext(ctx);
            var source = string.Format(_createFunctionTemplate, _lambdaFuncName, args, code);

            // create script that declares the lambda function
            var script = ctx.ScriptingProvider.CreateScript(new Context.ScriptOptions()
            {
                Context = ctx,
                EmitDebugInformation = Debugger.IsAttached, // CONSIDER
                Location = new Location(Path.Combine(ctx.RootPath, "runtime-created function"), 0, 0),  // TODO: pass from calling script
                IsSubmission = true
            }, source);
            var methods = script.GetGlobalRoutineHandle(_lambdaFuncName).ToArray();

            if (methods.Length == 0)
            {
                return null;
            }

            var lambdaName = string.Format(_lambdaFormatString, ++data.LastLambdaIndex);
            var routine = RoutineInfo.CreateUserRoutine(lambdaName, methods);

            ctx.DeclareFunction(routine);

            return lambdaName;
        }
    }
}
