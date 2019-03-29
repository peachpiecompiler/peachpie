using System;
using System.Collections.Generic;
using System.Text;

namespace Peachpie.NET.Sdk
{
    class SimpleAnalyzerAssemblyLoader : Microsoft.CodeAnalysis.IAnalyzerAssemblyLoader
    {
        public void AddDependencyLocation(string fullPath)
        {
            throw new NotImplementedException();
        }

        public System.Reflection.Assembly LoadFromPath(string fullPath)
        {
            throw new NotImplementedException();
        }
    }
}
