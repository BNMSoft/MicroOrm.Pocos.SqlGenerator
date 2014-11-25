using NUnit.Framework;
using FluentAssertions;

using MicroOrm.Pocos.SqlGenerator.Tests.Models;
using MicroOrm.Pocos.SqlGenerator.Tests.Extensions;

namespace MicroOrm.Pocos.SqlGenerator.Tests
{
    [TestFixture]
    public class SqlGeneratorTests
    {
        [TestFixtureSetUp()]
        public void TestFixtureSetUp()
        {
            _sqlGenerator = new SqlGenerator<UserWithIdentityColumn>();
        }

        [Test]
        public void Should_Generate_Select_Sql_For_UserWithIdentityColumn()
        {
            var sql = _sqlGenerator.GetSelect(null);

            sql.TrimSql().Should().Be(UserWithIdentityColumnSql.SelectSqlShouldBe.TrimSql());
        }

        [Test]
        public void Should_Generate_Insert_Sql_For_UserWithIdentityColumn()
        {
            var sql = _sqlGenerator.GetInsert();

            sql.TrimSql().Should().Be(UserWithIdentityColumnSql.InsertSqlShouldBe.TrimSql());
        }

        [Test]
        public void Should_Generate_Update_Sql_For_UserWithIdentityColumn()
        {
            var sql = _sqlGenerator.GetUpdate();

            sql.TrimSql().Should().Be(UserWithIdentityColumnSql.UpdateSqlShouldBe.TrimSql());
        }

        [Test]
        public void Should_Generate_Delete_Sql_For_UserWithIdentityColumn()
        {
            var sql = _sqlGenerator.GetDelete();

            sql.TrimSql().Should().Be(UserWithIdentityColumnSql.DeleteSqlShouldBe.TrimSql());
        }

        private ISqlGenerator<UserWithIdentityColumn> _sqlGenerator;
    }
}