using System.Globalization;
using MySql.Data.MySqlClient;
using Xunit;
namespace Solqaryn.Tests.Infrastructure;

/// <summary>C10-04.M05.03: real MySQL 8.4 local provider test only; not a live job, JWT, Cloudflare or restore.</summary>
[Trait("Category","Integration")]
public sealed class C1004M0503MySqlProviderIntegrationTests
{
    private static MySqlConnectionStringBuilder LocalOnly(string raw)
    {
        var b=new MySqlConnectionStringBuilder(raw);
        if (b.Server is not ("127.0.0.1" or "localhost") || b.Port!=3306)
            throw new InvalidOperationException("Only ephemeral loopback MySQL 3306 is permitted.");
        return b;
    }

    [Theory]
    [InlineData("Server=example.org;Port=3306;Database=x;User=root;Password=dummy;")]
    [InlineData("Server=127.0.0.1;Port=3307;Database=x;User=root;Password=dummy;")]
    public void RemoteHostOrPortIsRejectedBeforeConnecting(string raw)
        => Assert.Throws<InvalidOperationException>(()=>LocalOnly(raw));

    [Fact]
    public async Task TenantABPersistenceNeverReturnsOtherTenantRows()
    {
        await InDatabase(async db=>{
            await Insert(db,101,"job-A","key-A",new string('a',64));
            await Insert(db,202,"job-B","key-B",new string('b',64));
            Assert.Equal("pending",await Status(db,101,"job-A"));
            Assert.Equal("pending",await Status(db,202,"job-B"));
            Assert.Null(await Status(db,101,"job-B"));
            Assert.Null(await Status(db,202,"job-A"));
            Assert.Equal(1L,await Scalar<long>(db,"SELECT COUNT(*) FROM review_jobs WHERE tenant_id=@t",("@t",101)));
            Assert.Equal(1L,await Scalar<long>(db,"SELECT COUNT(*) FROM review_jobs WHERE tenant_id=@t",("@t",202)));
        });
    }

    [Fact]
    public async Task PersistentCasHasOneWinnerAndRejectsWrongTenant()
    {
        await InDatabase(async db=>{
            await Insert(db,101,"job-A","key-A",new string('a',64));
            const string cas="UPDATE review_jobs SET status='claimed',version=version+1 WHERE tenant_id=@t AND work_id=@w AND version=@v AND status='pending'";
            Assert.Equal(1,await Exec(db,cas,null,("@t",101),("@w","job-A"),("@v",7L)));
            Assert.Equal(0,await Exec(db,cas,null,("@t",101),("@w","job-A"),("@v",7L)));
            Assert.Equal(0,await Exec(db,cas,null,("@t",202),("@w","job-A"),("@v",8L)));
            Assert.Equal("claimed",await Status(db,101,"job-A"));
            Assert.Equal(8L,await Scalar<long>(db,"SELECT version FROM review_jobs WHERE tenant_id=@t AND work_id=@w",("@t",101),("@w","job-A")));
        });
    }

    [Fact]
    public async Task IdempotencyCollisionAndRollbackKeepCommittedState()
    {
        await InDatabase(async db=>{
            await Insert(db,101,"job-A","key-A",new string('a',64));
            var e=await Assert.ThrowsAsync<MySqlException>(
                ()=>Insert(db,101,"job-X","key-A",new string('b',64)));
            Assert.Equal(1062,e.Number);
            using(var tx=db.BeginTransaction())
            {
                Assert.Equal(1,await Exec(db,"UPDATE review_jobs SET status='failed' WHERE tenant_id=@t AND work_id=@w",tx,("@t",101),("@w","job-A")));
                tx.Rollback();
            }
            Assert.Equal("pending",await Status(db,101,"job-A"));
            Assert.Equal(1L,await Scalar<long>(db,"SELECT COUNT(*) FROM review_jobs WHERE tenant_id=@t",("@t",101)));
        });
    }

    private static async Task InDatabase(Func<MySqlConnection,Task> body)
    {
        var raw=Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("Ephemeral GitHub-hosted MySQL required.");
        var c=LocalOnly(raw);
        var dbname="sqv19_c1004_"+Guid.NewGuid().ToString("N");
        c.Database="mysql";
        await using var admin=new MySqlConnection(c.ConnectionString);
        await admin.OpenAsync();
        try
        {
            await Exec(admin,$"CREATE DATABASE \u0060{dbname}\u0060");
            c.Database=dbname;
            await using var db=new MySqlConnection(c.ConnectionString);
            await db.OpenAsync();
            await Exec(db,"CREATE TABLE review_jobs (tenant_id INT NOT NULL,work_id VARCHAR(64) NOT NULL,idem_key VARCHAR(64) NOT NULL,digest CHAR(64) NOT NULL,version BIGINT NOT NULL,status VARCHAR(24) NOT NULL,PRIMARY KEY(tenant_id,work_id),UNIQUE KEY uq_idem(tenant_id,idem_key)) ENGINE=InnoDB");
            await body(db);
        }
        finally { await Exec(admin,$"DROP DATABASE IF EXISTS \u0060{dbname}\u0060"); }
    }

    private static Task<int> Insert(MySqlConnection db,int t,string w,string k,string d)
        =>Exec(db,"INSERT INTO review_jobs(tenant_id,work_id,idem_key,digest,version,status) VALUES(@t,@w,@k,@d,7,'pending')",null,("@t",t),("@w",w),("@k",k),("@d",d));

    private static Task<string?> Status(MySqlConnection db,int t,string w)
        =>Scalar<string>(db,"SELECT status FROM review_jobs WHERE tenant_id=@t AND work_id=@w",("@t",t),("@w",w));

    private static async Task<T?> Scalar<T>(MySqlConnection db,string sql,params (string Name,object Value)[] args)
    {
        using var cmd=new MySqlCommand(sql,db);
        foreach(var (n,v) in args)cmd.Parameters.AddWithValue(n,v);
        var v=await cmd.ExecuteScalarAsync();
        return v is null or DBNull?default:(T)Convert.ChangeType(v,typeof(T),CultureInfo.InvariantCulture);
    }

    private static async Task<int> Exec(MySqlConnection db,string sql,MySqlTransaction? tx=null,
        params (string Name,object Value)[] args)
    {
        using var cmd=new MySqlCommand(sql,db,tx);
        foreach(var (n,v) in args)cmd.Parameters.AddWithValue(n,v);
        return await cmd.ExecuteNonQueryAsync();
    }
}
