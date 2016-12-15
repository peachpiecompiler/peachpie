using Microsoft.AspNetCore.Builder;
using Peachpie.Web;

namespace WebServerCSharp
{
    class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UsePhp();
            app.UseDefaultFiles();
            app.UseStaticFiles();
        }
    }
}
