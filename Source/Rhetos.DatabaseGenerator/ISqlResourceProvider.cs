using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.DatabaseGenerator
{
    public interface ISqlResourceProvider
    {
        string TryGet(string resourceName);

        string Get(string resourceName);

        string Format(string resourceName, params object[] args);
    }
}
