using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Configuration.Autofac;
using Rhetos.Dom.DefaultConcepts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonConcepts.Test
{
    [TestClass]
    public class DbCommandExtensionTest
    {
        [TestMethod]
        public void InsertAndUpdateMultipleTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repos = container.Resolve<Common.DomRepository>().TestEntity.Principal;
                var context = container.Resolve<Common.ExecutionContext>();

                var item1 = new TestEntity.Principal { Name = "1", ID = Guid.NewGuid() };
                var item2 = new TestEntity.Principal { Name = "2", ID = Guid.NewGuid() };

                var command = context.PersistenceTransaction.Connection.CreateCommand();
                command.Transaction = context.PersistenceTransaction.Transaction;

                command.InsertMultiple(new[] { item1, item2 });

                Assert.AreEqual(item1.Name, repos.Query().FirstOrDefault(x => x.ID == item1.ID).Name);
                Assert.AreEqual(item2.Name, repos.Query().FirstOrDefault(x => x.ID == item2.ID).Name);

                item1.Name = "1.1";
                item2.Name = "2.1";

                command.UpdateMultiple(new[] { item1, item2 });
                
                Assert.AreEqual(item1.Name, repos.Query().FirstOrDefault(x => x.ID == item1.ID).Name);
                Assert.AreEqual(item2.Name, repos.Query().FirstOrDefault(x => x.ID == item2.ID).Name);
            }
        }

        /*[TestMethod]
        public void UpdateNonexistetnRecordTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repos = container.Resolve<Common.DomRepository>().TestEntity.Principal;
                var context = container.Resolve<Common.ExecutionContext>();

                var item1 = new TestEntity.Principal { Name = "1", ID = Guid.NewGuid() };

                var command = context.PersistenceTransaction.Connection.CreateCommand();
                command.Transaction = context.PersistenceTransaction.Transaction;

                command.Update(item1);
            }
        }*/
    }
}
