using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Pchp.Core;
using Pchp.Core.Reflection;

namespace Peachpie.Library.Scripting
{
    public static class PhpFunctions
    {
        const string _createFunctionTemplate = "function __lambda{0} ({1}) {{ {2} }}";

        /// <summary>
        /// Creates an anonymous function from the parameters passed, and returns a unique name for it.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="args">The function arguments.</param>
        /// <param name="code">the function body.</param>
        /// <returns>Returns a unique function name as a string, or <c>FALSE</c> on error.</returns>
        [return: CastToFalse]
        public static string create_function(Context ctx, string args, string code)
        {
            // prepare function script
            // TODO: increment lambda ID upon compilation
            var source = string.Format(_createFunctionTemplate, 666, args, code);
            var fooname = "__lambda666";

            // create script that declares the lambda function
            var script = ctx.ScriptingProvider.CreateScript(new Context.ScriptOptions() { Context = ctx, EmitDebugInformation = false, Location = new Location(/*TODO*/) }, source);
            // TODO: check for error
            var tmp = script.Evaluate(ctx, ctx.Globals, null);  // declare the function

            Debug.Assert(ctx.GetDeclaredFunction(fooname) != null);

            //
            return fooname;
        }
    }
}
