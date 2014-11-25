namespace MicroOrm.Pocos.SqlGenerator
{
    public class SqlStrings
    {
        public static string Insert = "insert into [{0}].[{1}] {2} {3}";

        public static string Select = "select {0} from [{1}].[{2}] with (nolock)";

        public static string Delete = "delete from [{0}].[{1}] where {2}";

        public static string Update = "update [{0}].[{1}] set {2} where {3}";
    }
}