using System;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260817204000_N1_10_CosteoPersistencia")]
    public partial class N1_10_CosteoPersistencia : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N110CGuard;
                CREATE TEMPORARY TABLE __N110CGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N110C_Guard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N110CGuard (Id, Violaciones)
                SELECT 1, COUNT(*)
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name IN ('PoliticasCosteoInventario','CostosEstandarInventario','CapasCostoInventario','AsignacionesCostoMovimientoInventario','VariacionesCostoEstandarInventario');
                INSERT INTO __N110CGuard (Id, Violaciones)
                SELECT 2, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name = 'EmpresaConfiguraciones';

                -- Fresh install: Program.cs ejecuta MigrateAsync antes del seeding de aplicación.
                -- Sólo una tabla absolutamente vacía puede recibir este bootstrap. Si existe
                -- cualquier configuración previa (activa o inactiva), los guards de upgrade
                -- permanecen fail-closed y exigen exactamente una activa.
                INSERT INTO `EmpresaConfiguraciones`
                    (`NombreComercial`,`Eslogan`,`NombreVisibleSistema`,`DescripcionSistema`,`MensajeLogin`,
                     `Copyright`,`MostrarCopyright`,`UsarAnioAutomaticoCopyright`,`EncabezadoActivo`,`PiePaginaActivo`,
                     `Moneda`,`ZonaHoraria`,`FormatoFecha`,`Activa`,`FechaActualizacion`)
                SELECT
                    'Storefront',
                    'Eleva tu mundo digital',
                    'Storefront',
                    'Sistema integral de inventario y ventas',
                    'Bienvenido a Storefront',
                    'Storefront',
                    1,
                    1,
                    1,
                    1,
                    'HNL',
                    'America/Tegucigalpa',
                    'dd/MM/yyyy',
                    1,
                    UTC_TIMESTAMP()
                WHERE NOT EXISTS (SELECT 1 FROM `EmpresaConfiguraciones` LIMIT 1);

                INSERT INTO __N110CGuard (Id, Violaciones)
                SELECT 3, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM `EmpresaConfiguraciones`
                 WHERE `Activa` = 1;
                DROP TEMPORARY TABLE __N110CGuard;
                """);

            migrationBuilder.CreateTable(
                name: "PoliticasCosteoInventario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmpresaConfiguracionId = table.Column<int>(type: "int", nullable: false),
                    Metodo = table.Column<int>(type: "int", nullable: false),
                    VigenteDesdeUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    VigenteHastaUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Motivo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                    EmpresaConfiguracionVigenteId = table.Column<int>(type: "int", nullable: true, computedColumnSql: "CASE WHEN `VigenteHastaUtc` IS NULL THEN `EmpresaConfiguracionId` ELSE NULL END", stored: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoliticasCosteoInventario", x => x.Id);
                    table.CheckConstraint("CK_PoliticasCosteo_Metodo", "`Metodo` IN (1,2,3)");
                    table.CheckConstraint("CK_PoliticasCosteo_Vigencia", "`VigenteHastaUtc` IS NULL OR `VigenteHastaUtc` > `VigenteDesdeUtc`");
                    table.ForeignKey("FK_PoliticasCosteo_EmpresaConfiguracion", x => x.EmpresaConfiguracionId, "EmpresaConfiguraciones", "Id", onDelete: ReferentialAction.Restrict);
                }).Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CostosEstandarInventario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductoVarianteId = table.Column<int>(type: "int", nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    VigenteDesdeUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    VigenteHastaUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Motivo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                    ProductoVarianteVigenteId = table.Column<int>(type: "int", nullable: true, computedColumnSql: "CASE WHEN `VigenteHastaUtc` IS NULL THEN `ProductoVarianteId` ELSE NULL END", stored: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostosEstandarInventario", x => x.Id);
                    table.UniqueConstraint("AK_CostosEstandar_Variante_Id", x => new { x.ProductoVarianteId, x.Id });
                    table.CheckConstraint("CK_CostosEstandar_Costo", "`CostoUnitario` >= 0");
                    table.CheckConstraint("CK_CostosEstandar_Vigencia", "`VigenteHastaUtc` IS NULL OR `VigenteHastaUtc` > `VigenteDesdeUtc`");
                    table.ForeignKey("FK_CostosEstandar_ProductoVariantes", x => x.ProductoVarianteId, "ProductoVariantes", "Id", onDelete: ReferentialAction.Restrict);
                }).Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CapasCostoInventario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductoVarianteId = table.Column<int>(type: "int", nullable: false),
                    AlmacenId = table.Column<int>(type: "int", nullable: false),
                    UbicacionAlmacenId = table.Column<int>(type: "int", nullable: true),
                    MovimientoInventarioOrigenId = table.Column<int>(type: "int", nullable: true),
                    CapaCostoOrigenId = table.Column<int>(type: "int", nullable: true),
                    EsApertura = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    MotivoApertura = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    CantidadOriginal = table.Column<int>(type: "int", nullable: false),
                    CantidadRestante = table.Column<int>(type: "int", nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    FechaOrigenUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CorrelationId = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapasCostoInventario", x => x.Id);
                    table.UniqueConstraint("AK_CapasCosto_Variante_Id", x => new { x.ProductoVarianteId, x.Id });
                    table.CheckConstraint("CK_CapasCosto_Cantidades", "`CantidadOriginal` > 0 AND `CantidadRestante` >= 0 AND `CantidadRestante` <= `CantidadOriginal`");
                    table.CheckConstraint("CK_CapasCosto_Costo", "`CostoUnitario` >= 0");
                    table.CheckConstraint("CK_CapasCosto_Origen", "(`EsApertura` = 1 AND `MovimientoInventarioOrigenId` IS NULL AND `CapaCostoOrigenId` IS NULL AND `MotivoApertura` IS NOT NULL) OR (`EsApertura` = 0 AND `MovimientoInventarioOrigenId` IS NOT NULL AND `MotivoApertura` IS NULL)");
                    table.ForeignKey("FK_CapasCosto_Almacenes", x => x.AlmacenId, "Almacenes", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_CapasCosto_MovimientoOrigen", x => x.MovimientoInventarioOrigenId, "MovimientosInventario", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_CapasCosto_ProductoVariantes", x => x.ProductoVarianteId, "ProductoVariantes", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_CapasCosto_CapaOrigen_MismaVariante", x => new { x.ProductoVarianteId, x.CapaCostoOrigenId }, "CapasCostoInventario", new[] { "ProductoVarianteId", "Id" }, onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_CapasCosto_Ubicacion_MismoAlmacen", x => new { x.AlmacenId, x.UbicacionAlmacenId }, "UbicacionesAlmacen", new[] { "AlmacenId", "Id" }, onDelete: ReferentialAction.Restrict);
                }).Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AsignacionesCostoMovimientoInventario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MovimientoInventarioId = table.Column<int>(type: "int", nullable: false),
                    ProductoVarianteId = table.Column<int>(type: "int", nullable: false),
                    CapaCostoInventarioId = table.Column<int>(type: "int", nullable: true),
                    Metodo = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CorrelationId = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsignacionesCostoMovimientoInventario", x => x.Id);
                    table.CheckConstraint("CK_AsignacionesCosto_Metodo", "`Metodo` IN (1,2,3)");
                    table.CheckConstraint("CK_AsignacionesCosto_Cantidad", "`Cantidad` > 0");
                    table.CheckConstraint("CK_AsignacionesCosto_Costo", "`CostoUnitario` >= 0");
                    table.CheckConstraint("CK_AsignacionesCosto_CapaPorMetodo", "(`Metodo` = 2 AND `CapaCostoInventarioId` IS NOT NULL) OR (`Metodo` <> 2 AND `CapaCostoInventarioId` IS NULL)");
                    table.ForeignKey("FK_AsignacionesCosto_MovimientosInventario", x => x.MovimientoInventarioId, "MovimientosInventario", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_AsignacionesCosto_ProductoVariantes", x => x.ProductoVarianteId, "ProductoVariantes", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_AsignacionesCosto_Capa_MismaVariante", x => new { x.ProductoVarianteId, x.CapaCostoInventarioId }, "CapasCostoInventario", new[] { "ProductoVarianteId", "Id" }, onDelete: ReferentialAction.Restrict);
                }).Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VariacionesCostoEstandarInventario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MovimientoInventarioId = table.Column<int>(type: "int", nullable: false),
                    ProductoVarianteId = table.Column<int>(type: "int", nullable: false),
                    CostoEstandarInventarioId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    CostoRealUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CostoEstandarUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    VariacionTotal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CorrelationId = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariacionesCostoEstandarInventario", x => x.Id);
                    table.CheckConstraint("CK_VariacionesCostoEstandar_Cantidad", "`Cantidad` > 0");
                    table.CheckConstraint("CK_VariacionesCostoEstandar_Costos", "`CostoRealUnitario` >= 0 AND `CostoEstandarUnitario` >= 0");
                    table.ForeignKey("FK_VariacionesCostoEstandar_MovimientosInventario", x => x.MovimientoInventarioId, "MovimientosInventario", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_VariacionesCostoEstandar_ProductoVariantes", x => x.ProductoVarianteId, "ProductoVariantes", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_VariacionesCostoEstandar_Version_MismaVariante", x => new { x.ProductoVarianteId, x.CostoEstandarInventarioId }, "CostosEstandarInventario", new[] { "ProductoVarianteId", "Id" }, onDelete: ReferentialAction.Restrict);
                }).Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex("UX_PoliticasCosteo_Empresa_Vigente", "PoliticasCosteoInventario", "EmpresaConfiguracionVigenteId", unique: true);
            migrationBuilder.CreateIndex("IX_PoliticasCosteo_Empresa_Vigencia", "PoliticasCosteoInventario", new[] { "EmpresaConfiguracionId", "VigenteDesdeUtc" });
            migrationBuilder.CreateIndex("UX_CostosEstandar_Variante_Vigente", "CostosEstandarInventario", "ProductoVarianteVigenteId", unique: true);
            migrationBuilder.CreateIndex("IX_CostosEstandar_Variante_Vigencia", "CostosEstandarInventario", new[] { "ProductoVarianteId", "VigenteDesdeUtc" });
            migrationBuilder.CreateIndex("IX_CapasCosto_FIFO", "CapasCostoInventario", new[] { "ProductoVarianteId", "AlmacenId", "UbicacionAlmacenId", "FechaOrigenUtc", "Id" });
            migrationBuilder.CreateIndex("IX_CapasCosto_MovimientoOrigen", "CapasCostoInventario", "MovimientoInventarioOrigenId");
            migrationBuilder.CreateIndex("IX_CapasCosto_Linaje", "CapasCostoInventario", new[] { "ProductoVarianteId", "CapaCostoOrigenId" });
            migrationBuilder.CreateIndex("IX_CapasCosto_Almacen_Ubicacion", "CapasCostoInventario", new[] { "AlmacenId", "UbicacionAlmacenId" });
            migrationBuilder.CreateIndex("IX_AsignacionesCosto_Movimiento", "AsignacionesCostoMovimientoInventario", "MovimientoInventarioId");
            migrationBuilder.CreateIndex("IX_AsignacionesCosto_Capa", "AsignacionesCostoMovimientoInventario", new[] { "ProductoVarianteId", "CapaCostoInventarioId" });
            migrationBuilder.CreateIndex("IX_VariacionesCostoEstandar_Movimiento", "VariacionesCostoEstandarInventario", "MovimientoInventarioId");
            migrationBuilder.CreateIndex("IX_VariacionesCostoEstandar_Version", "VariacionesCostoEstandarInventario", new[] { "ProductoVarianteId", "CostoEstandarInventarioId" });

            migrationBuilder.Sql("""
                INSERT INTO `PoliticasCosteoInventario`
                    (`EmpresaConfiguracionId`,`Metodo`,`VigenteDesdeUtc`,`VigenteHastaUtc`,`Motivo`,`FechaCreacion`,`FechaActualizacion`)
                SELECT `Id`, 1, UTC_TIMESTAMP(6), NULL,
                       'Cutover ERP-N1.10 — Promedio Ponderado compatible', UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)
                  FROM `EmpresaConfiguraciones`
                 WHERE `Activa` = 1;

                DROP TEMPORARY TABLE IF EXISTS __N110CPostGuard;
                CREATE TEMPORARY TABLE __N110CPostGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N110C_PostGuard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N110CPostGuard (Id, Violaciones)
                SELECT 1, CASE WHEN COUNT(*) = 5 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name IN ('PoliticasCosteoInventario','CostosEstandarInventario','CapasCostoInventario','AsignacionesCostoMovimientoInventario','VariacionesCostoEstandarInventario');
                INSERT INTO __N110CPostGuard (Id, Violaciones)
                SELECT 2, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM `PoliticasCosteoInventario`
                 WHERE `VigenteHastaUtc` IS NULL AND `Metodo` = 1;
                INSERT INTO __N110CPostGuard (Id, Violaciones)
                SELECT 3, COUNT(*) FROM `CapasCostoInventario`;
                INSERT INTO __N110CPostGuard (Id, Violaciones)
                SELECT 4, COUNT(*) FROM `CostosEstandarInventario`;
                DROP TEMPORARY TABLE __N110CPostGuard;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N110CDownGuard;
                CREATE TEMPORARY TABLE __N110CDownGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N110C_DownGuard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N110CDownGuard (Id, Violaciones)
                SELECT 1,
                    (SELECT COUNT(*) FROM `CapasCostoInventario`) +
                    (SELECT COUNT(*) FROM `CostosEstandarInventario`) +
                    (SELECT COUNT(*) FROM `AsignacionesCostoMovimientoInventario`) +
                    (SELECT COUNT(*) FROM `VariacionesCostoEstandarInventario`);
                INSERT INTO __N110CDownGuard (Id, Violaciones)
                SELECT 2, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM `PoliticasCosteoInventario`
                 WHERE `Metodo` = 1
                   AND `VigenteHastaUtc` IS NULL
                   AND `Motivo` = 'Cutover ERP-N1.10 — Promedio Ponderado compatible';
                DROP TEMPORARY TABLE __N110CDownGuard;
                """);

            migrationBuilder.DropTable(name: "AsignacionesCostoMovimientoInventario");
            migrationBuilder.DropTable(name: "VariacionesCostoEstandarInventario");
            migrationBuilder.DropTable(name: "CapasCostoInventario");
            migrationBuilder.DropTable(name: "CostosEstandarInventario");
            migrationBuilder.DropTable(name: "PoliticasCosteoInventario");
        }
    }
}