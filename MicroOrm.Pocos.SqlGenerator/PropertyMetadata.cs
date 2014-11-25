using System.Reflection;

using MicroOrm.Pocos.SqlGenerator.Attributes;

namespace MicroOrm.Pocos.SqlGenerator
{
    /// <summary>
    /// 
    /// </summary>
    public class PropertyMetadata
    {
        public PropertyInfo PropertyInfo { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public string Alias { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public string ColumnName
        {
            get
            {
                return string.IsNullOrEmpty(Alias) ? PropertyInfo.Name : Alias;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Name
        {
            get 
            {
                return PropertyInfo.Name;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyInfo"></param>
        public PropertyMetadata(PropertyInfo propertyInfo)
        {
            PropertyInfo = propertyInfo;

            var alias = PropertyInfo.GetCustomAttribute<StoredAs>();

            Alias = alias != null ? alias.Value : string.Empty;
        }
    }
}
