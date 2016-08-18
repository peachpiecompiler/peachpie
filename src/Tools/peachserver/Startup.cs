using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace peachserver
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
