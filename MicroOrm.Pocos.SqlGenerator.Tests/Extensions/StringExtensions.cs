using System.Text.RegularExpressions;

namespace MicroOrm.Pocos.SqlGenerator.Tests.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sqlString"></param>
        /// <returns></returns>
        public static string TrimSql(this string sqlString)
        {
            return Regex.Replace(sqlString, @"(\s+|\r\n)", " ").Trim();
        }
    }
}