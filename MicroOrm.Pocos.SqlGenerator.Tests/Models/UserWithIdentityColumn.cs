using MicroOrm.Pocos.SqlGenerator.Attributes;

namespace MicroOrm.Pocos.SqlGenerator.Tests.Models
{
    [StoredAs("Users")]
    public class UserWithIdentityColumn
    {
        [KeyProperty(Identity = true)]
        public int Id { get; set; }

        public string Login { get; set; }

        [StoredAs("FName")]
        public string FirstName { get; set; }

        [StoredAs("LName")]
        public string LastName { get; set; }

        public string Email { get; set; }

        [StatusProperty]
        public UserStatus Status { get; set; }

        [NonStored]
        public string FullName
        {
            get
            {
                return string.Format("{0} {1}", FirstName, LastName);
            }
        }
    }

    public class UserWithIdentityColumnSql
    {
        public static string SelectSqlShouldBe = @"
select  [Users].[Id],
        [Users].[Login],
        [Users].[FName] as [FirstName],
        [Users].[LName] as [LastName],
        [Users].[Email],
        [Users].[Status]
from    [dbo].[Users] with (nolock)
where   [Users].[Status] != 3";

        public static string InsertSqlShouldBe = @"
insert
into    [dbo].[Users]
        ([Users].[Login], [Users].[FName], [Users].[LName], [Users].[Email], [Users].[Status])
values  (@Login, @FirstName, @LastName, @Email, @Status)
declare @newId numeric(38, 0)
set @newId = scope_identity()
select @newId";

        public static string UpdateSqlShouldBe = @"
update  [dbo].[Users]
set     [Users].[Login] = @Login,
        [Users].[FName] = @FirstName,
        [Users].[LName] = @LastName,
        [Users].[Email] = @Email,
        [Users].[Status] = @Status
where   [Users].[Id] = @Id";

        public static string DeleteSqlShouldBe = @"
update  [dbo].[Users]
set     [Users].[Status] = 3
where   [Users].[Id] = @Id";
    }
}