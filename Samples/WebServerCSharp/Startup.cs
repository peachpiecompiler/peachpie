using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Peachpie.Web;

namespace WebServerCSharp
{
    /// <summary>
    /// Startup class
    /// </summary>
    class Startup
    {
        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The application.</param>
        public void Configure(IApplicationBuilder app)
        {
            var phpRequestOptions = new PhpRequestOptions
            {
                ScriptAssembliesName = new[]
                {
                    //Name of the compiled php assembly, if not "website"
                    "website"
                }
            };
            app.UsePhp(phpRequestOptions);
            app.UseDefaultFiles();
            app.UseStaticFiles();
        }
    }
}
