using Rhetos.Dom.DefaultConcepts;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Dom.DefaultConcepts
{
    public static class DbCommandExtensions
    {
        private static string GenerateInsertCommandTextForType(Type type, string variableSuffix = "")
        {
            var entityName = type.FullName;
            var insertString = "INSERT INTO " + entityName + " (";

            var properties = type.GetProperties();

            for (int i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                insertString += property.Name + ", ";
            }
            insertString = insertString.Remove(insertString.Length - 2, 2);
            insertString += ") VALUES (";

            for (int i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                insertString += "@" + property.Name + variableSuffix + ", ";
            }
            insertString = insertString.Remove(insertString.Length - 2, 2);
            insertString += ");";

            return insertString;
        }

        private static string GenerateUpdateCommandTextForType(Type type, string variableSuffix = "")
        {
            var entityName = type.FullName;
            var updateString = "UPDATE " + entityName + " SET ";

            var properties = type.GetProperties();

            //The only property on the entity is the ID so an update does not have any meaning
            if (properties.Length == 1)
                return "";

            for (int i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                if (property.Name != "ID")
                    updateString += property.Name + " = @" + property.Name + variableSuffix + ", ";
            }
            updateString = updateString.Remove(updateString.Length - 2, 2);
            updateString += " WHERE ID = @ID" + variableSuffix + ";";

            return updateString;
        }

        internal static void AppendInsertCommand(this DbCommand command, IEntity entity, Type entityType, string suffix)
        {
            var properties = entityType.GetProperties();

            command.CommandText += GenerateInsertCommandTextForType(entityType, suffix);
            for (int i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                var value = property.GetValue(entity, null);
                if (value != null)
                    command.Parameters.Add(new SqlParameter("@" + property.Name + suffix, value));
                else
                    command.Parameters.Add(new SqlParameter("@" + property.Name + suffix, DBNull.Value));
            }
        }

        internal static void AppendUpdateCommand(this DbCommand command, IEntity entity, Type entityType, string suffix)
        {
            var properties = entityType.GetProperties();

            command.CommandText += GenerateUpdateCommandTextForType(entityType, suffix);

            //The only property on the entity is the ID so an update does not have any meaning
            if (properties.Length > 1)
            {
                for (int j = 0; j < properties.Length; j++)
                {
                    var property = properties[j];
                    var value = property.GetValue(entity, null);
                    if (value != null)
                        command.Parameters.Add(new SqlParameter("@" + property.Name + suffix, value));
                    else
                        command.Parameters.Add(new SqlParameter("@" + property.Name + suffix, DBNull.Value));
                }
            }
        }

        internal static void AppendDeleteCommand(this DbCommand command, IEntity entity, Type entityType)
        {
            var entityName = entityType.FullName;
            command.CommandText += $@"DELETE FROM {entityName} WHERE ID = '{entity.ID.ToString()}';";
        }

        private static void AppendInsertOrUpdateMultiple<T>(this DbCommand command, IEnumerable<T> entities, bool insert = true) where T : IEntity
        {
            var entityList = new List<T>(entities);
            var entityType = typeof(T);

            for (int i = 0; i < entityList.Count; i++)
            {
                var entity = entityList[i];
                if (insert)
                    command.AppendInsertCommand(entity, entityType, i.ToString());
                else
                    command.AppendUpdateCommand(entity, entityType, i.ToString());
            }
        }

        internal static void Clear(this DbCommand command)
        {
            command.Parameters.Clear();
            command.CommandText = "";
        }

        public static void Insert<T>(this DbCommand command, T entity) where T : IEntity
        {
            command.CommandText = "";
            command.AppendInsertCommand(entity, typeof(T), "");
            command.ExecuteNonQuery();
            command.Clear();
        }

        public static void InsertMultiple<T>(this DbCommand command, IEnumerable<T> entities) where T : IEntity
        {
            foreach (var entity in entities)
            {
                command.Insert(entity);
            }
            /*command.CommandText = "";
            command.AppendInsertOrUpdateMultiple(entities, true);
            command.ExecuteNonQueryAndClear();*/
        }

        public static void Update<T>(this DbCommand command, T entity) where T : IEntity
        {
            command.CommandText = "";
            command.AppendUpdateCommand(entity, typeof(T), "");
            if (command.ExecuteNonQuery() == 0)
            {
                command.Clear();
                throw new DbException("Updating a record that does not exist in database.", new IEntity[] { entity });
            }
            command.Clear();
        }

        public static void UpdateMultiple<T>(this DbCommand command, IEnumerable<T> entities) where T : IEntity
        {
            foreach (var entity in entities)
            {
                command.Update(entity);
            }
            /*command.CommandText = "";
            command.AppendInsertOrUpdateMultiple(entities, false);
            command.ExecuteNonQueryAndClear();*/
        }

        public static void Delete<T>(this DbCommand command, T entity) where T : IEntity
        {
            command.CommandText = "";
            command.AppendDeleteCommand(entity, typeof(T));
            if (command.ExecuteNonQuery() == 0)
            {
                command.Clear();
                throw new DbException("Deleting a record that does not exist in database.", new IEntity[] { entity });
            }
            command.Clear();
        }

        public static void DeleteMultiple<T>(this DbCommand command, IEnumerable<T> entities) where T : IEntity
        {
            foreach (var entity in entities)
            {
                command.Delete(entity);
            }
            /*var entityName = typeof(T).FullName;
            command.CommandText = $@"DELETE FROM {entityName} WHERE ID IN ({string.Join(",", entities.Select(x => "'" + x.ID.ToString() + "'"))});";
            command.ExecuteNonQueryAndClear();*/
        }
    }
}
