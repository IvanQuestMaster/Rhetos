using System;
using System.Collections.Generic;
using System.Text;

namespace Rhetos.Implementations
{
    public class MockConfiguration : IConfiguration
    {
        public Lazy<bool> GetBool(string key, bool defaultValue)
        {
            return new Lazy<bool>(() => defaultValue);
        }

        public Lazy<int> GetInt(string key, int defaultValue)
        {
            return new Lazy<int>(() => defaultValue);
        }

        public Lazy<string> GetString(string key, string defaultValue)
        {
            return new Lazy<string>(() => defaultValue);
        }
    }
}
