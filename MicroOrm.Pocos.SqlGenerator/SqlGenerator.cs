using System;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using MicroOrm.Pocos.SqlGenerator.Attributes;

namespace MicroOrm.Pocos.SqlGenerator
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class SqlGenerator<TEntity> : ISqlGenerator<TEntity> where TEntity : new()
    {
        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        public SqlGenerator()
        {
            LoadEntityMetadata();
        }

        private void LoadEntityMetadata()
        {
            var entityType = typeof(TEntity);

            var aliasAttribute = entityType.GetCustomAttribute<StoredAs>();
            var schemeAttribute = entityType.GetCustomAttribute<Scheme>();
            
            TableName = aliasAttribute != null ? aliasAttribute.Value : entityType.Name;
            Scheme = schemeAttribute != null ? schemeAttribute.Value : "dbo";

            // Load all the "primitive" entity properties
            var properties  = entityType.GetProperties().Where(p => p.PropertyType.IsValueType || p.PropertyType.Name.Equals("String", StringComparison.InvariantCultureIgnoreCase)).ToArray();

            BaseProperties = properties.Where(p => !p.GetCustomAttributes<NonStored>().Any()).Select(p => new PropertyMetadata(p));

            // Filter key properties
            KeyProperties = properties.Where(p => p.GetCustomAttributes<KeyProperty>().Any()).Select(p => new PropertyMetadata(p));

            // Use identity as key pattern
            var identityProperty = properties.SingleOrDefault(p => p.GetCustomAttributes<KeyProperty>().Any(k => k.Identity));
            IdentityProperty = identityProperty != null ? new PropertyMetadata(identityProperty) : null ;

            //Status property (if exists, and if it does, it must be an enumeration)
            var statusProperty = properties.FirstOrDefault(p => p.PropertyType.IsEnum && p.GetCustomAttributes<StatusProperty>().Any());

            if (statusProperty != null)
            {
                StatusProperty = new PropertyMetadata(statusProperty);

                var statusPropertyType = statusProperty.PropertyType;
                var deleteOption = statusPropertyType.GetFields().FirstOrDefault(f => f.GetCustomAttribute<Deleted>() != null);

                if (deleteOption != null)
                {
                    var enumValue = Enum.Parse(statusPropertyType, deleteOption.Name);

                    if (enumValue != null)
                    {
                        LogicalDeleteValue = Convert.ChangeType(enumValue, Enum.GetUnderlyingType(statusPropertyType));
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        public bool IsIdentity
        {
            get
            {
                return IdentityProperty != null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool LogicalDelete
        {
            get
            {
                return StatusProperty != null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string TableName { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public string Scheme { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public PropertyMetadata IdentityProperty { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<PropertyMetadata> KeyProperties { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<PropertyMetadata> BaseProperties { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public PropertyMetadata StatusProperty { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public object LogicalDeleteValue { get; private set; }

        /// <summary>
        ///  
        /// </summary>
        /// <returns></returns>
        public virtual string GetInsert()
        {
            // Enumerate the entity properties
            // Identity property (if exists) has to be ignored
            IEnumerable<PropertyMetadata> properties = (IsIdentity ?
                                                        BaseProperties.Where(p => !p.Name.Equals(IdentityProperty.Name, StringComparison.InvariantCultureIgnoreCase)) :
                                                        BaseProperties).ToList();

            var columNames = string.Join(", ", properties.Select(p => string.Format("[{0}].[{1}]", TableName, p.ColumnName)));
            var values = string.Join(", ", properties.Select(p => string.Format("@{0}", p.Name)));

            var sqlBuilder = new StringBuilder();
            sqlBuilder.AppendFormat(SqlStrings.Insert,
                                    Scheme,
                                    TableName,
                                    string.IsNullOrEmpty(columNames) ? string.Empty : string.Format("({0})", columNames),
                                    string.IsNullOrEmpty(values) ? string.Empty : string.Format("values ({0})", values));

            // If the entity has an identity key, we create a new variable into the query in order to retrieve the generated id
            if (IsIdentity)
            {
                sqlBuilder.AppendLine("");
                sqlBuilder.AppendLine("declare @newId numeric(38, 0)");
                sqlBuilder.AppendLine("set @newId = scope_identity()");
                sqlBuilder.AppendLine("select @newId");
            }

            return sqlBuilder.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual string GetUpdate()
        {
            var properties = BaseProperties.Where(p => !KeyProperties.Any(k => k.Name.Equals(p.Name, StringComparison.InvariantCultureIgnoreCase)));

            var sqlBuilder = new StringBuilder();

            sqlBuilder.AppendFormat(SqlStrings.Update,
                                    Scheme,
                                    TableName,
                                    string.Join(", ", properties.Select(p => string.Format("[{0}].[{1}] = @{2}", TableName, p.ColumnName, p.Name))),
                                    string.Join(" and ", KeyProperties.Select(p => string.Format("[{0}].[{1}] = @{2}", TableName, p.ColumnName, p.Name))));
            
            return sqlBuilder.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual string GetSelectAll()
        {
            return GetSelect(new { });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        public virtual string GetSelect(object filters)
        {
            // Projection function
            Func<PropertyMetadata, string> projectionFunction = (p) =>
            {
                if (!string.IsNullOrEmpty(p.Alias))
                {
                    return string.Format("[{0}].[{1}] as [{2}]", TableName, p.ColumnName, p.Name);    
                }

                return string.Format("[{0}].[{1}]", TableName, p.ColumnName);
            };

            var sqlBuilder = new StringBuilder();
            sqlBuilder.AppendFormat(SqlStrings.Select,
                                    string.Join(", ", BaseProperties.Select(projectionFunction)),
                                    Scheme,
                                    TableName);

            // Properties of the dynamic filters object
            var filterProperties = (filters != null) ? filters.GetType().GetProperties().Select(p => p.Name).ToArray() : new List<string>().ToArray();
            var containsFilter = filterProperties.Any();

            if (containsFilter)
            {
                sqlBuilder.AppendFormat(" where {0} ", ToWhere(filterProperties));    
            }

            // Evaluates if this repository implements logical delete
            if (LogicalDelete)
            {
                if (containsFilter)
                {
                    sqlBuilder.AppendFormat(" and [{0}].[{1}] != {2}",
                        TableName,
                        StatusProperty.Name,
                        LogicalDeleteValue);
                }
                else
                {
                    sqlBuilder.AppendFormat(" where [{0}].[{1}] != {2}",
                        TableName,
                        StatusProperty.Name,
                        LogicalDeleteValue);
                }
            }

            return sqlBuilder.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual string GetDelete()
        {
            var sqlBuilder = new StringBuilder();

            if (!LogicalDelete)
            {
                sqlBuilder.AppendFormat(SqlStrings.Delete,
                    Scheme,
                    TableName,
                    string.Join(" and ",
                        KeyProperties.Select(p => string.Format("[{0}].[{1}] = @{2}", TableName, p.ColumnName, p.Name))));

            }
            else
            {
                sqlBuilder.AppendFormat(SqlStrings.Update,
                    Scheme,
                    TableName,
                    string.Format("[{0}].[{1}] = {2}", TableName, StatusProperty.ColumnName, LogicalDeleteValue),
                    string.Join(" and ",
                        KeyProperties.Select(p => string.Format("[{0}].[{1}] = @{2}", TableName, p.ColumnName, p.Name))));
            }

            return sqlBuilder.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        private string ToWhere(IEnumerable<string> properties)
        {
            return string.Join(" and ", properties.Select(p => {
                var propertyMetadata = BaseProperties.FirstOrDefault(pm => pm.Name.Equals(p, StringComparison.InvariantCultureIgnoreCase));

                if (propertyMetadata != null)
                {
                    return string.Format("[{0}].[{1}] = @{2}", TableName, propertyMetadata.ColumnName, propertyMetadata.Name);
                }

                if (p == null)
                {
                    return string.Format("[{0}].[{1}] is @{2}", TableName, p, p);
                }

                return string.Format("[{0}].[{1}] = @{2}", TableName, p, p);
            }));
        }
    }
}