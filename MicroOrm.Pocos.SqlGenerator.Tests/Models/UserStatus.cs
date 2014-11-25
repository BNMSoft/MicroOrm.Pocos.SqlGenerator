using MicroOrm.Pocos.SqlGenerator.Attributes;

namespace MicroOrm.Pocos.SqlGenerator.Tests.Models
{
    public enum UserStatus : byte
    {
        Registered = 1,

        Active = 2,

        [Deleted]
        Inactive = 3
    }
}