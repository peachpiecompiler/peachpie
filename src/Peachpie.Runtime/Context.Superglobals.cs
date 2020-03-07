using Pchp.Core.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Core
{
    partial class Context
    {
        /// <summary>
        /// Superglobals holder.
        /// </summary>
        protected struct Superglobals
        {
            /// <summary>
            /// Content of superglobal variables.
            /// </summary>
            public PhpArray
                globals,
                server,
                env,
                request,
                files,
                get,
                post,
                session,
                cookie;

            #region Helpers

            /// <summary>
            /// Adds a variable to auto-global array.
            /// </summary>
            /// <param name="array">The array.</param>
            /// <param name="name">A unparsed name of variable.</param>
            /// <param name="value">A value to be added.</param>
            /// <param name="subname">A name of intermediate array inserted before the value.</param>
            public static void AddVariable(PhpArray/*!*/ array, string name, PhpValue value, string subname = null)
            {
                NameValueCollectionUtils.AddVariable(array, name, value, subname);
            }

            /// <summary>
            /// Adds variables from one auto-global array to another.
            /// </summary>
            /// <param name="dst">The target array.</param>
            /// <param name="src">The source array.</param>
            /// <remarks>Variable values are deeply copied.</remarks>
            public static void AddVariables(PhpArray/*!*/ dst, PhpArray/*!*/ src)
            {
                Debug.Assert(dst != null && src != null);

                var e = src.GetFastEnumerator();
                while (e.MoveNext())
                {
                    dst.SetItemValue(e.CurrentKey, e.CurrentValue.DeepCopy());
                }
            }

            /// <summary>
            /// Adds a form file to the <c>$_FILES</c> array.
            /// </summary>
            /// <param name="files">The $_FILES array.</param>
            /// <param name="field_name">Form field name.</param>
            /// <param name="file_name">Original file name, without the directory name.</param>
            /// <param name="type">Content type.</param>
            /// <param name="tmp_name">Local full file path where is the uploaded file temporarily stored.</param>
            /// <param name="error">Error code number.</param>
            /// <param name="file_length">Uploaded file size in bytes.</param>
            public static void AddFormFile(PhpArray/*!*/ files, string field_name, string file_name, string type, string tmp_name, int error, long file_length)
            {
                // field_name
                // field_name[]
                // field_name[key]

                var left = field_name.IndexOf('[');
                if (left > 0 && left < field_name.Length - 1)
                {
                    var right = field_name.IndexOf(']', left + 1);
                    if (right > 0)
                    {
                        // keyed file entry:

                        // the variable name is a key to the "array", dots are replaced by underscores in top-level name:
                        var field_name_key = new IntStringKey(NameValueCollectionUtils.EncodeTopLevelName(field_name.Substring(0, left)));
                        var file_entry = NameValueCollectionUtils.EnsureItemArray(files, field_name_key);

                        // file entry key,
                        // can be a string, empty or a number
                        var key = Convert.StringToArrayKey(field_name.Substring(left + 1, right - left - 1));

                        NameValueCollectionUtils.EnsureItemArray(file_entry, "name", key, file_name);
                        NameValueCollectionUtils.EnsureItemArray(file_entry, "type", key, type);
                        NameValueCollectionUtils.EnsureItemArray(file_entry, "tmp_name", key, tmp_name);
                        NameValueCollectionUtils.EnsureItemArray(file_entry, "error", key, error);
                        NameValueCollectionUtils.EnsureItemArray(file_entry, "size", key, file_length);

                        //
                        return;
                    }
                }

                // not keyed:
                AddVariable(files, field_name, new PhpArray(5)
                {
                    { "name", file_name },
                    { "type", type },
                    { "tmp_name", tmp_name },
                    { "error", error },
                    { "size", file_length },
                });
            }

            /// <summary>
            /// Adds file variables from $_FILE array to $GLOBALS array.
            /// </summary>
            /// <param name="globals">$GLOBALS array.</param>
            /// <param name="files">$_FILES array.</param>
            public static void AddFileVariablesToGlobals(PhpArray/*!*/ globals, PhpArray/*!*/ files)
            {
                var e = files.GetFastEnumerator();
                while (e.MoveNext())
                {
                    var file_info = e.CurrentValue.AsArray();
                    var keystr = e.CurrentKey.ToString();

                    globals[e.CurrentKey] = file_info["tmp_name"];
                    globals[keystr + "_name"] = file_info["name"];
                    globals[keystr + "_type"] = file_info["type"];
                    globals[keystr + "_size"] = file_info["size"];
                }
            }

            public static void InitializeEGPCSForWeb(PhpArray globals, ref Superglobals superglobals, string registering_order = null)
            {
                // deprecated:

                //// adds EGPCS variables as globals:
                //if (registering_order == null)
                //{
                //    return;
                //}

                //// adds items in the order specified by RegisteringOrder config option (overwrites existing):
                //for (int i = 0; i < registering_order.Length; i++)
                //{
                //    switch (registering_order[i])
                //    {
                //        case 'E': AddVariables(globals, superglobals.env); break;
                //        case 'G': AddVariables(globals, superglobals.get); break;

                //        case 'P':
                //            AddVariables(globals, superglobals.post);
                //            AddFileVariablesToGlobals(globals, superglobals.files);
                //            break;

                //        case 'C': AddVariables(globals, superglobals.cookie); break;
                //        case 'S': AddVariables(globals, superglobals.server); break;
                //    }
                //}
            }

            public static void InitializeEGPCSForConsole(PhpArray globals, ref Superglobals superglobals)
            {
                AddVariables(globals, superglobals.env);
            }

            #endregion

            /// <summary>
            /// Application wide $_ENV array.
            /// </summary>
            static PhpArray StaticEnv => s_env ?? (s_env = InitEnv());

            static PhpArray InitEnv()
            {
                var env_vars = Environment.GetEnvironmentVariables();
                var array = new PhpArray(env_vars.Count);

                foreach (DictionaryEntry entry in env_vars)
                {
                    AddVariable(array, (string)entry.Key, (string)entry.Value, null);
                }

                return array;
            }

            public static PhpArray CreateEnvArray() => StaticEnv.DeepCopy();

            static PhpArray s_env;
        }

        Superglobals _superglobals;

        /// <summary>
        /// Must be called by derived constructor to initialize content of superglobal variables.
        /// </summary>
        protected void InitSuperglobals() => InitSuperglobals(ref _superglobals);

        void InitSuperglobals(ref Superglobals superglobals)
        {
            //var var_order = DefaultPhpConfigurationService.Instance.Core.VariablesOrder; // TODO
            var egpcs = DefaultPhpConfigurationService.Instance.Core?.RegisteringOrder;

            superglobals.env = Superglobals.CreateEnvArray();
            superglobals.get = InitGetVariable();
            superglobals.post = InitPostVariable();
            superglobals.cookie = InitCookieVariable();
            superglobals.server = InitServerVariable();
            superglobals.files = InitFilesVariable();
            superglobals.session = null;    // $_SESSION is NULL if it is not initialized
            superglobals.request = InitRequestVariable(superglobals.get, superglobals.post, superglobals.cookie, egpcs);   // after get, post, cookie
            superglobals.globals = InitGlobals(egpcs);
        }

        /// <summary>
        /// Initializes <c>_GLOBALS</c> array.
        /// </summary>
        /// <param name="registering_order"><c>EGPCS</c> or <c>null</c> if register globals is disabled (default).</param>
        protected virtual PhpArray InitGlobals(string registering_order = null)
        {
            Debug.Assert(_superglobals.request != null && _superglobals.env != null && _superglobals.server != null && _superglobals.files != null);

            var globals = new PhpArray(128);

            // estimates the initial capacity of $GLOBALS array:

            // adds EGPCS variables as globals:
            if (registering_order != null)
            {
                if (IsWebApplication)
                {
                    Superglobals.InitializeEGPCSForWeb(globals, ref _superglobals);
                }
                else
                {
                    Superglobals.InitializeEGPCSForConsole(globals, ref _superglobals);
                }
            }

            // adds auto-global variables (overwrites potential existing variables in $GLOBALS):
            globals[CommonPhpArrayKeys._GET] = PhpValue.Create(_superglobals.get);
            globals[CommonPhpArrayKeys._POST] = PhpValue.Create(_superglobals.post);
            globals[CommonPhpArrayKeys._COOKIE] = PhpValue.Create(_superglobals.cookie);
            globals[CommonPhpArrayKeys._FILES] = PhpValue.Create(_superglobals.files);
            globals[CommonPhpArrayKeys._ENV] = PhpValue.Create(_superglobals.env);
            globals[CommonPhpArrayKeys._REQUEST] = PhpValue.Create(_superglobals.request);
            globals[CommonPhpArrayKeys._SERVER] = PhpValue.Create(_superglobals.server);
            globals[CommonPhpArrayKeys._SESSION] = PhpValue.Create(_superglobals.session);
            globals[CommonPhpArrayKeys.GLOBALS] = PhpValue.Create(new PhpAlias(PhpValue.Create(globals)));   // &$GLOBALS

            //// adds long arrays:
            //if (Configuration.Global.GlobalVariables.RegisterLongArrays)
            //{
            //    globals.Add("HTTP_ENV_VARS", new PhpReference(((PhpArray)Env.Value).DeepCopy()));
            //    globals.Add("HTTP_GET_VARS", new PhpReference(((PhpArray)Get.Value).DeepCopy()));
            //    globals.Add("HTTP_POST_VARS", new PhpReference(((PhpArray)Post.Value).DeepCopy()));
            //    globals.Add("HTTP_COOKIE_VARS", new PhpReference(((PhpArray)Cookie.Value).DeepCopy()));
            //    globals.Add("HTTP_SERVER_VARS", new PhpReference(((PhpArray)Server.Value).DeepCopy()));
            //    globals.Add("HTTP_POST_FILES", new PhpReference(((PhpArray)Files.Value).DeepCopy()));
            //    globals[CommonPhpArrayKeys.HTTP_RAW_POST_DATA] = HttpRawPostData;

            //    // both session array references the same array:
            //    globals.Add("HTTP_SESSION_VARS", Session);
            //}

            //
            return globals;
        }

        /// <summary>Initialize $_SERVER global variable.</summary>
        protected virtual PhpArray InitServerVariable() => PhpArray.NewEmpty();

        /// <summary>Initialize $_REQUEST global variable.</summary>
        protected PhpArray InitRequestVariable(PhpArray get, PhpArray post, PhpArray cookie, string gpcOrder)
        {
            Debug.Assert(get != null && post != null && cookie != null);

            if (IsWebApplication && gpcOrder != null)
            {
                var requestArray = new PhpArray(get.Count + post.Count + cookie.Count);

                // adds items from GET, POST, COOKIE arrays in the order specified by RegisteringOrder config option:
                for (int i = 0; i < gpcOrder.Length; i++)
                {
                    switch (char.ToUpperInvariant(gpcOrder[i]))
                    {
                        case 'G': Superglobals.AddVariables(requestArray, get); break;
                        case 'P': Superglobals.AddVariables(requestArray, post); break;
                        case 'C': Superglobals.AddVariables(requestArray, cookie); break;
                    }
                }

                return requestArray;
            }
            else
            {
                return PhpArray.NewEmpty();
            }
        }

        /// <summary>Initialize $_GET global variable.</summary>
        protected virtual PhpArray InitGetVariable() => PhpArray.NewEmpty();

        /// <summary>Initialize $_POST global variable.</summary>
        protected virtual PhpArray InitPostVariable() => PhpArray.NewEmpty();

        /// <summary>Initialize $_FILES global variable.</summary>
        /// <remarks>
		/// <list type="bullet">
		///   <item>$_FILES[{var_name}]['name'] - The original name of the file on the client machine.</item>
		///   <item>$_FILES[{var_name}]['type'] - The mime type of the file, if the browser provided this information. An example would be "image/gif".</item>
		///   <item>$_FILES[{var_name}]['size'] - The size, in bytes, of the uploaded file.</item> 
		///   <item>$_FILES[{var_name}]['tmp_name'] - The temporary filename of the file in which the uploaded file was stored on the server.</item>
		///   <item>$_FILES[{var_name}]['error'] - The error code associated with this file upload.</item> 
		/// </list>
		/// </remarks>
        protected virtual PhpArray InitFilesVariable() => PhpArray.NewEmpty();

        /// <summary>Initialize $_COOKIE global variable.</summary>
        protected virtual PhpArray InitCookieVariable() => PhpArray.NewEmpty();

        #region Properties

        /// <summary>
        /// Array of global variables.
        /// Cannot be <c>null</c>.
        /// </summary>
        public PhpArray Globals
        {
            get { return _superglobals.globals; }
            set
            {
                _superglobals.globals[CommonPhpArrayKeys.GLOBALS] = new PhpAlias(_superglobals.globals = value ?? throw new ArgumentNullException());
            }
        }

        /// <summary>
        /// Array of server and execution environment information.
        /// Cannot be <c>null</c>.
        /// </summary>
        public PhpArray Server
        {
            get { return _superglobals.server; }
            set
            {
                _superglobals.globals[CommonPhpArrayKeys._SERVER] = _superglobals.server = value ?? throw new ArgumentNullException();
            }
        }

        /// <summary>
        /// An associative array of variables passed to the current script via the environment method.
        /// Cannot be <c>null</c>.
        /// </summary>
        public PhpArray Env
        {
            get { return _superglobals.env; }
            set
            {
                _superglobals.globals[CommonPhpArrayKeys._ENV] = _superglobals.env = value;
            }
        }

        /// <summary>
        /// An array that by default contains the contents of $_GET, $_POST and $_COOKIE.
        /// Cannot be <c>null</c>.
        /// </summary>
        public PhpArray Request
        {
            get { return _superglobals.request; }
            set
            {
                _superglobals.globals[CommonPhpArrayKeys._REQUEST] = _superglobals.request = value;
            }
        }

        /// <summary>
        /// An associative array of variables passed to the current script via the URL parameters.
        /// Cannot be <c>null</c>.
        /// </summary>
        public PhpArray Get
        {
            get { return _superglobals.get; }
            set
            {
                _superglobals.globals[CommonPhpArrayKeys._GET] = _superglobals.get = value;
            }
        }

        /// <summary>
        /// An associative array of variables passed to the current script via the HTTP POST method.
        /// Cannot be <c>null</c>.
        /// </summary>
        public PhpArray Post
        {
            get { return _superglobals.post; }
            set
            {
                _superglobals.globals[CommonPhpArrayKeys._POST] = _superglobals.post = value;
            }
        }

        /// <summary>
        /// An associative array of items uploaded to the current script via the HTTP POST method.
        /// Cannot be <c>null</c>.
        /// </summary>
        public PhpArray Files
        {
            get { return _superglobals.files; }
            set
            {
                _superglobals.globals[CommonPhpArrayKeys._FILES] = _superglobals.files = value;
            }
        }

        /// <summary>
        /// An associative array containing session variables available to the current script.
        /// Can be <c>null</c>.
        /// </summary>
        public PhpArray Session
        {
            get { return _superglobals.session; }
            set
            {
                _superglobals.globals[CommonPhpArrayKeys._SESSION] = _superglobals.session = value;
            }
        }

        /// <summary>
        /// An associative array of variables passed to the current script via the HTTP POST method.
        /// Cannot be <c>null</c>.
        /// </summary>
        public PhpArray Cookie
        {
            get { return _superglobals.cookie; }
            set
            {
                _superglobals.globals[CommonPhpArrayKeys._COOKIE] = _superglobals.cookie = value;
            }
        }

        /// <summary>
        /// Gets value of <c>$HTTP_RAW_POST_DATA</c> variable.
        /// Note this variable has been removed in PHP 7.0 and should not be used.
        /// </summary>
        public virtual string HttpRawPostData
        {
            get
            {
                return this.Globals[CommonPhpArrayKeys.HTTP_RAW_POST_DATA].ToStringOrNull();
            }
            set
            {
                this.Globals[CommonPhpArrayKeys.HTTP_RAW_POST_DATA] = value;
            }
        }

        #endregion
    }
}
