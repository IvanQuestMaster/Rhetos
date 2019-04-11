using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Dom.DefaultConcepts
{
    public class DbException : Exception
    {
        public IEnumerable<IEntity> Entites { get; }

        public DbException(string message, IEntity[] entites) : base(message)
        {
            Entites = entites;
        }
    }
}
