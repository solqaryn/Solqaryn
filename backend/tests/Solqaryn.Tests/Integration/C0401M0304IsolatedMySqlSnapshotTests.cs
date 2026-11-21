using System.Data.Common;
using System.Security.Cryptography;
using MySql.Data.MySqlClient;
using Solqaryn.Infrastructure.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class C0401M0304IsolatedMySqlSnapshotTests
{
    private const string DbName = "solqaryn_c1502_m0503";
    private static string Connection()
    {
        var raw = Environment.GetEnvironmentVariable("C0401_M0304_MYSQL_TEST")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var c = new MySqlConnectionStringBuilder(raw);
        if (c.Server is not ("127.0.0.1" or "localhost") ||
            c.Port != 3306 || c.Database != DbName ||
            c.UserID != "root")
            throw new InvalidOperationException("ISOLATED_LOOPBACK_MYSQL84_ONLY");
        return raw;
    }

    private static async Task SqlAsync(MySqlConnection db, string sql,
        int tenant, int plan, int subscriptionTenant, string capacity = "catalog.max")
    {
        await using var command = db.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@tenant", tenant);
        command.Parameters.AddWithValue("@plan", plan);
        command.Parameters.AddWithValue("@sub", subscriptionTenant);
        command.Parameters.AddWithValue("@capacity", capacity);
        command.Parameters.AddWithValue("@start", DateTime.UtcNow.AddDays(-1));
        await command.ExecuteNonQueryAsync();
    }

    private const string Insert = """
        INSERT INTO V22C0401SnapshotIsolated
          (TenantId,PlanId,Codigo,Revision,PlanActivo,PlanDesdeUtc,PlanHastaUtc,
           SubEmpresaId,SubVersion,SubActiva,SubInicioUtc,SubFinUtc,
           CapacidadClave,CapacidadMax,Ilimitado,Consumo,
           PlanHabilitada,RubroHabilitada,RolHabilitada,TenantHabilitada)
        VALUES(@tenant,@plan,'PLAN-BASICO',3,1,@start,NULL,
               @sub,4,1,@start,NULL,@capacity,10,0,2,1,1,1,1)
        """;

    [Fact]
    public async Task RealMysql84TenantVersionAmbiguityRevocationAndRollbackFailClosed()
    {
        var raw = Connection();
        await using var db = new MySqlConnection(raw);
        await db.OpenAsync();
        await using (var version = db.CreateCommand())
        {
            version.CommandText = "SELECT VERSION()";
            Assert.StartsWith("8.4.", Convert.ToString(await version.ExecuteScalarAsync()));
        }
        await using (var create = db.CreateCommand())
        {
            create.CommandText = """
                CREATE TABLE IF NOT EXISTS V22C0401SnapshotIsolated(
                  TenantId INT NOT NULL, PlanId INT NOT NULL, Codigo VARCHAR(48) NOT NULL,
                  Revision BIGINT NOT NULL, PlanActivo TINYINT(1) NOT NULL,
                  PlanDesdeUtc DATETIME(6) NOT NULL, PlanHastaUtc DATETIME(6) NULL,
                  SubEmpresaId INT NOT NULL, SubVersion BIGINT NOT NULL,
                  SubActiva TINYINT(1) NOT NULL, SubInicioUtc DATETIME(6) NOT NULL,
                  SubFinUtc DATETIME(6) NULL, CapacidadClave VARCHAR(96) NOT NULL,
                  CapacidadMax BIGINT NULL, Ilimitado TINYINT(1) NOT NULL,
                  Consumo BIGINT NOT NULL, PlanHabilitada TINYINT(1) NOT NULL,
                  RubroHabilitada TINYINT(1) NOT NULL, RolHabilitada TINYINT(1) NOT NULL,
                  TenantHabilitada TINYINT(1) NOT NULL,
                  PRIMARY KEY(TenantId,PlanId,CapacidadClave)
                ) ENGINE=InnoDB
                """;
            await create.ExecuteNonQueryAsync();
        }
        var a = RandomNumberGenerator.GetInt32(100000, 300000000);
        var b = a + 1;
        const int planId = 4401;
        var source = new C0401IsolatedMySqlSnapshotSource(
            () => new MySqlConnection(raw));
        try
        {
            await SqlAsync(db, Insert, a, planId, a);
            await SqlAsync(db, Insert, b, planId, b);

            var own = await source.ReadForTenantAsync(a, planId, CancellationToken.None);
            Assert.NotNull(own);
            Assert.Equal(a, own.Suscripcion.EmpresaId);
            Assert.Equal(planId, own.Plan.PlanId);
            Assert.Equal(3L, own.Plan.Revision);
            Assert.Equal(4L, own.Suscripcion.Version);
            Assert.True(own.ConsultaFiltradaPorTenant);
            Assert.True(own.VersionesFrescas);
            Assert.Equal(DateTimeKind.Utc, own.Plan.VigenteDesdeUtc.Kind);
            Assert.Equal(DateTimeKind.Utc, own.Suscripcion.InicioUtc.Kind);
            Assert.Null(await source.ReadForTenantAsync(a, planId + 1, CancellationToken.None));
            Assert.Null(await source.ReadForTenantAsync(-1, planId, CancellationToken.None));
            Assert.Null(await source.ReadForTenantAsync(a, -1, CancellationToken.None));
            var foreign = await source.ReadForTenantAsync(b, planId, CancellationToken.None);
            Assert.NotNull(foreign);
            Assert.Equal(b, foreign.Suscripcion.EmpresaId);

            var verified = new C0401PlanDomainRules.VerifiedActor(
                a, 11, 22, null, true, true, true, true, false);
            var intent = new C0401PlanDomainRules.SolicitudUso(
                a, null, 4, 3, "catalog.max", 1, false);
            var approvedPreflight = C0401PlanDomainRules.Evaluar(
                verified, own.Plan, own.Suscripcion, own.Capacidad,
                intent, DateTime.UtcNow);
            Assert.Equal("PRECONDICIONES_DOMINIO_SOLAMENTE_GATE_BACKEND_REQUERIDO",
                approvedPreflight.Codigo);
            Assert.False(approvedPreflight.AutorizaEscritura);
            Assert.False(approvedPreflight.AutorizaCobro);

            // A second capacity is ambiguous in this SINGLE-capacity adapter.
            await SqlAsync(db, Insert, a, planId, a, "catalog.other");
            Assert.Null(await source.ReadForTenantAsync(a, planId, CancellationToken.None));
            await SqlAsync(db, "DELETE FROM V22C0401SnapshotIsolated WHERE TenantId=@tenant AND PlanId=@plan AND CapacidadClave=@capacity",
                a, planId, a, "catalog.other");

            await SqlAsync(db, "UPDATE V22C0401SnapshotIsolated SET SubActiva=0 WHERE TenantId=@tenant AND PlanId=@plan",
                a, planId, a);
            var revoked = await source.ReadForTenantAsync(a, planId, CancellationToken.None);
            Assert.NotNull(revoked);
            Assert.False(revoked.Suscripcion.Activa);
            Assert.NotEqual("PRECONDICIONES_DOMINIO_SOLAMENTE_GATE_BACKEND_REQUERIDO",
                C0401PlanDomainRules.Evaluar(verified, revoked.Plan,
                    revoked.Suscripcion, revoked.Capacidad, intent, DateTime.UtcNow).Codigo);

            // Uncommitted tenant snapshot must not survive rollback.
            var rollbackTenant = RandomNumberGenerator.GetInt32(300000001, 500000000);
            await using (var transaction = await db.BeginTransactionAsync())
            {
                await using var command = db.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = Insert;
                command.Parameters.AddWithValue("@tenant", rollbackTenant);
                command.Parameters.AddWithValue("@plan", planId);
                command.Parameters.AddWithValue("@sub", rollbackTenant);
                command.Parameters.AddWithValue("@capacity", "catalog.max");
                command.Parameters.AddWithValue("@start", DateTime.UtcNow.AddDays(-1));
                await command.ExecuteNonQueryAsync();
                await transaction.RollbackAsync();
            }
            Assert.Null(await source.ReadForTenantAsync(
                rollbackTenant, planId, CancellationToken.None));
        }
        finally
        {
            await SqlAsync(db, "DELETE FROM V22C0401SnapshotIsolated WHERE TenantId=@tenant AND PlanId=@plan",
                a, planId, a);
            await SqlAsync(db, "DELETE FROM V22C0401SnapshotIsolated WHERE TenantId=@tenant AND PlanId=@plan",
                b, planId, b);
        }
    }
}
