using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations;

/// <summary>
/// ERP-N1.1 C: crea la persistencia forward-only de Sucursal.
/// Es un cambio estrictamente aditivo: no existe histórico que migrar ni se crea
/// una FK ficticia para EmpresaId antes de la raíz multiempresa de ERP-N6.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260814175500_N1_1_SucursalPersistencia")]
public sealed class N1_1_SucursalPersistencia : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Preflight fail-closed: una tabla homónima fuera del historial EF requiere
        // reconciliación explícita antes de continuar; nunca se adopta implícitamente.
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N11CGuard;
            CREATE TEMPORARY TABLE __N11CGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N11C_Guard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N11CGuard (Id, Violaciones)
            SELECT 1, COUNT(*)
              FROM information_schema.tables
             WHERE table_schema = DATABASE()
               AND table_name = 'Sucursales';

            DROP TEMPORARY TABLE __N11CGuard;
            """);

        migrationBuilder.Sql("""
            CREATE TABLE Sucursales
            (
                Id INT NOT NULL AUTO_INCREMENT,
                EmpresaId INT NULL,
                Codigo VARCHAR(40) NOT NULL,
                Nombre VARCHAR(150) NOT NULL,
                Direccion VARCHAR(500) NULL,
                Telefono VARCHAR(50) NULL,
                Correo VARCHAR(254) NULL,
                ZonaHoraria VARCHAR(100) NOT NULL DEFAULT 'America/Tegucigalpa',
                Activa TINYINT(1) NOT NULL DEFAULT 1,
                FechaCreacion DATETIME(6) NOT NULL,
                FechaActualizacion DATETIME(6) NOT NULL,
                CreadoPorUsuarioId INT NULL,
                CreadoPorNombreUsuario VARCHAR(150) NULL,
                ActualizadoPorUsuarioId INT NULL,
                ActualizadoPorNombreUsuario VARCHAR(150) NULL,
                Eliminado TINYINT(1) NOT NULL DEFAULT 0,
                FechaEliminacion DATETIME(6) NULL,
                EliminadoPorUsuarioId INT NULL,
                CodigoActivoUnico VARCHAR(40)
                    GENERATED ALWAYS AS (IF(Eliminado = 0, UPPER(TRIM(Codigo)), NULL)) STORED,
                CONSTRAINT PK_Sucursales PRIMARY KEY (Id),
                CONSTRAINT CK_Sucursales_Codigo_NoVacio CHECK (CHAR_LENGTH(TRIM(Codigo)) > 0),
                CONSTRAINT CK_Sucursales_Nombre_NoVacio CHECK (CHAR_LENGTH(TRIM(Nombre)) > 0),
                CONSTRAINT UX_Sucursales_Codigo_Activo UNIQUE (CodigoActivoUnico),
                INDEX IX_Sucursales_EmpresaId (EmpresaId),
                INDEX IX_Sucursales_Estado (Activa, Eliminado)
            ) ENGINE=InnoDB;
            """);

        // Postcheck verificable de estructura mínima. Al ser una tabla nueva no hay
        // backfill ni reconciliación histórica; el snapshot lógico previo es conjunto vacío.
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N11CPostGuard;
            CREATE TEMPORARY TABLE __N11CPostGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N11C_PostGuard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N11CPostGuard (Id, Violaciones)
            SELECT 1, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
              FROM information_schema.tables
             WHERE table_schema = DATABASE()
               AND table_name = 'Sucursales';

            INSERT INTO __N11CPostGuard (Id, Violaciones)
            SELECT 2, CASE WHEN COUNT(DISTINCT index_name) = 3 THEN 0 ELSE 1 END
              FROM information_schema.statistics
             WHERE table_schema = DATABASE()
               AND table_name = 'Sucursales'
               AND index_name IN (
                    'UX_Sucursales_Codigo_Activo',
                    'IX_Sucursales_EmpresaId',
                    'IX_Sucursales_Estado');

            DROP TEMPORARY TABLE __N11CPostGuard;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Rollback deliberadamente fail-closed: no destruye sucursales ya capturadas.
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N11CDownGuard;
            CREATE TEMPORARY TABLE __N11CDownGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N11C_DownGuard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N11CDownGuard (Id, Violaciones)
            SELECT 1, COUNT(*) FROM Sucursales;

            DROP TEMPORARY TABLE __N11CDownGuard;
            DROP TABLE Sucursales;
            """);
    }
}
