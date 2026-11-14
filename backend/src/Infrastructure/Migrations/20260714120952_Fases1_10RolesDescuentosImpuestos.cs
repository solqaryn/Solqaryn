using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fases1_10RolesDescuentosImpuestos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RolId",
                table: "Usuarios",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PermisoId",
                table: "RolPermisos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RolId",
                table: "RolPermisos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "RegistrosAuditoria",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Entidad",
                table: "RegistrosAuditoria",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Error",
                table: "RegistrosAuditoria",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Ip",
                table: "RegistrosAuditoria",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Motivo",
                table: "RegistrosAuditoria",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Resultado",
                table: "RegistrosAuditoria",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "RegistrosAuditoria",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ValoresAnteriores",
                table: "RegistrosAuditoria",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ValoresNuevos",
                table: "RegistrosAuditoria",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CompraImpuestos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CompraId = table.Column<int>(type: "int", nullable: false),
                    ImpuestoId = table.Column<int>(type: "int", nullable: false),
                    ImpuestoNombreSnapshot = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TasaSnapshot = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    BaseImponible = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MontoAplicado = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompraImpuestos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompraImpuestos_Compras_CompraId",
                        column: x => x.CompraId,
                        principalTable: "Compras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Descuentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodigoPromocional = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodigoPromocionalNormalizado = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FechaFin = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    MontoMinimo = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    MontoMaximoDescuento = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    CantidadMinima = table.Column<int>(type: "int", nullable: true),
                    RequiereAprobacion = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Acumulable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Prioridad = table.Column<int>(type: "int", nullable: false),
                    LimiteTotalUsos = table.Column<int>(type: "int", nullable: true),
                    LimiteUsosPorCliente = table.Column<int>(type: "int", nullable: true),
                    UsosRealizados = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Eliminado = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    EliminadoPorUsuarioId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Descuentos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Impuestos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Codigo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Tasa = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    MontoFijo = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    FechaInicio = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FechaFin = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IncluidoEnPrecio = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SeCalculaAntesDescuento = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Acumulativo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Prioridad = table.Column<int>(type: "int", nullable: false),
                    RequiereRetencion = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Eliminado = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    EliminadoPorUsuarioId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Impuestos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Permisos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Codigo = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Modulo = table.Column<int>(type: "int", nullable: false),
                    Accion = table.Column<int>(type: "int", nullable: false),
                    EsSistema = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Eliminado = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    EliminadoPorUsuarioId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permisos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NombreNormalizado = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EsSistema = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EsAdministrador = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Eliminado = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    EliminadoPorUsuarioId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VentaDescuentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    VentaId = table.Column<int>(type: "int", nullable: false),
                    DescuentoId = table.Column<int>(type: "int", nullable: false),
                    DescuentoNombreSnapshot = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DescuentoCodigoSnapshot = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TipoSnapshot = table.Column<int>(type: "int", nullable: false),
                    ValorSnapshot = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MontoAplicado = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VentaDescuentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VentaDescuentos_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VentaImpuestos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    VentaId = table.Column<int>(type: "int", nullable: false),
                    ImpuestoId = table.Column<int>(type: "int", nullable: false),
                    ImpuestoNombreSnapshot = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TasaSnapshot = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    BaseImponible = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MontoAplicado = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VentaImpuestos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VentaImpuestos_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DescuentoCategorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DescuentoId = table.Column<int>(type: "int", nullable: false),
                    CategoriaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DescuentoCategorias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DescuentoCategorias_Descuentos_DescuentoId",
                        column: x => x.DescuentoId,
                        principalTable: "Descuentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DescuentoClientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DescuentoId = table.Column<int>(type: "int", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DescuentoClientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DescuentoClientes_Descuentos_DescuentoId",
                        column: x => x.DescuentoId,
                        principalTable: "Descuentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DescuentoProductos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DescuentoId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DescuentoProductos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DescuentoProductos_Descuentos_DescuentoId",
                        column: x => x.DescuentoId,
                        principalTable: "Descuentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DescuentoRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DescuentoId = table.Column<int>(type: "int", nullable: false),
                    RolId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DescuentoRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DescuentoRoles_Descuentos_DescuentoId",
                        column: x => x.DescuentoId,
                        principalTable: "Descuentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "HistorialUsoDescuentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DescuentoId = table.Column<int>(type: "int", nullable: false),
                    VentaId = table.Column<int>(type: "int", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: true),
                    MontoAplicado = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialUsoDescuentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialUsoDescuentos_Descuentos_DescuentoId",
                        column: x => x.DescuentoId,
                        principalTable: "Descuentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "HistorialAplicacionImpuestos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ImpuestoId = table.Column<int>(type: "int", nullable: false),
                    DocumentoTipo = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DocumentoId = table.Column<int>(type: "int", nullable: false),
                    BaseImponible = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TasaAplicada = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    MontoAplicado = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialAplicacionImpuestos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialAplicacionImpuestos_Impuestos_ImpuestoId",
                        column: x => x.ImpuestoId,
                        principalTable: "Impuestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ImpuestoCategorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ImpuestoId = table.Column<int>(type: "int", nullable: false),
                    CategoriaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpuestoCategorias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImpuestoCategorias_Impuestos_ImpuestoId",
                        column: x => x.ImpuestoId,
                        principalTable: "Impuestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ImpuestoClientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ImpuestoId = table.Column<int>(type: "int", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpuestoClientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImpuestoClientes_Impuestos_ImpuestoId",
                        column: x => x.ImpuestoId,
                        principalTable: "Impuestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ImpuestoOperaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ImpuestoId = table.Column<int>(type: "int", nullable: false),
                    Operacion = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpuestoOperaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImpuestoOperaciones_Impuestos_ImpuestoId",
                        column: x => x.ImpuestoId,
                        principalTable: "Impuestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ImpuestoProductos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ImpuestoId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpuestoProductos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImpuestoProductos_Impuestos_ImpuestoId",
                        column: x => x.ImpuestoId,
                        principalTable: "Impuestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ImpuestoProveedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ImpuestoId = table.Column<int>(type: "int", nullable: false),
                    ProveedorId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpuestoProveedores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImpuestoProveedores_Impuestos_ImpuestoId",
                        column: x => x.ImpuestoId,
                        principalTable: "Impuestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "RolId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_RolId",
                table: "Usuarios",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_RolPermisos_PermisoId",
                table: "RolPermisos",
                column: "PermisoId");

            migrationBuilder.CreateIndex(
                name: "IX_RolPermisos_RolId",
                table: "RolPermisos",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_CorrelationId",
                table: "RegistrosAuditoria",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_Resultado",
                table: "RegistrosAuditoria",
                column: "Resultado");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_UsuarioId",
                table: "RegistrosAuditoria",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CompraImpuestos_CompraId",
                table: "CompraImpuestos",
                column: "CompraId");

            migrationBuilder.CreateIndex(
                name: "IX_DescuentoCategorias_DescuentoId",
                table: "DescuentoCategorias",
                column: "DescuentoId");

            migrationBuilder.CreateIndex(
                name: "IX_DescuentoClientes_DescuentoId",
                table: "DescuentoClientes",
                column: "DescuentoId");

            migrationBuilder.CreateIndex(
                name: "IX_DescuentoProductos_DescuentoId",
                table: "DescuentoProductos",
                column: "DescuentoId");

            migrationBuilder.CreateIndex(
                name: "IX_DescuentoRoles_DescuentoId",
                table: "DescuentoRoles",
                column: "DescuentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Descuentos_CodigoPromocionalNormalizado",
                table: "Descuentos",
                column: "CodigoPromocionalNormalizado",
                unique: true,
                filter: "`CodigoPromocionalNormalizado` IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialAplicacionImpuestos_ImpuestoId",
                table: "HistorialAplicacionImpuestos",
                column: "ImpuestoId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialUsoDescuentos_DescuentoId",
                table: "HistorialUsoDescuentos",
                column: "DescuentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ImpuestoCategorias_ImpuestoId",
                table: "ImpuestoCategorias",
                column: "ImpuestoId");

            migrationBuilder.CreateIndex(
                name: "IX_ImpuestoClientes_ImpuestoId",
                table: "ImpuestoClientes",
                column: "ImpuestoId");

            migrationBuilder.CreateIndex(
                name: "IX_ImpuestoOperaciones_ImpuestoId",
                table: "ImpuestoOperaciones",
                column: "ImpuestoId");

            migrationBuilder.CreateIndex(
                name: "IX_ImpuestoProductos_ImpuestoId",
                table: "ImpuestoProductos",
                column: "ImpuestoId");

            migrationBuilder.CreateIndex(
                name: "IX_ImpuestoProveedores_ImpuestoId",
                table: "ImpuestoProveedores",
                column: "ImpuestoId");

            migrationBuilder.CreateIndex(
                name: "IX_Impuestos_Codigo",
                table: "Impuestos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_Codigo",
                table: "Permisos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_Modulo_Accion",
                table: "Permisos",
                columns: new[] { "Modulo", "Accion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_NombreNormalizado",
                table: "Roles",
                column: "NombreNormalizado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VentaDescuentos_VentaId",
                table: "VentaDescuentos",
                column: "VentaId");

            migrationBuilder.CreateIndex(
                name: "IX_VentaImpuestos_VentaId",
                table: "VentaImpuestos",
                column: "VentaId");

            migrationBuilder.AddForeignKey(
                name: "FK_RolPermisos_Permisos_PermisoId",
                table: "RolPermisos",
                column: "PermisoId",
                principalTable: "Permisos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RolPermisos_Roles_RolId",
                table: "RolPermisos",
                column: "RolId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Roles_RolId",
                table: "Usuarios",
                column: "RolId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RolPermisos_Permisos_PermisoId",
                table: "RolPermisos");

            migrationBuilder.DropForeignKey(
                name: "FK_RolPermisos_Roles_RolId",
                table: "RolPermisos");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Roles_RolId",
                table: "Usuarios");

            migrationBuilder.DropTable(
                name: "CompraImpuestos");

            migrationBuilder.DropTable(
                name: "DescuentoCategorias");

            migrationBuilder.DropTable(
                name: "DescuentoClientes");

            migrationBuilder.DropTable(
                name: "DescuentoProductos");

            migrationBuilder.DropTable(
                name: "DescuentoRoles");

            migrationBuilder.DropTable(
                name: "HistorialAplicacionImpuestos");

            migrationBuilder.DropTable(
                name: "HistorialUsoDescuentos");

            migrationBuilder.DropTable(
                name: "ImpuestoCategorias");

            migrationBuilder.DropTable(
                name: "ImpuestoClientes");

            migrationBuilder.DropTable(
                name: "ImpuestoOperaciones");

            migrationBuilder.DropTable(
                name: "ImpuestoProductos");

            migrationBuilder.DropTable(
                name: "ImpuestoProveedores");

            migrationBuilder.DropTable(
                name: "Permisos");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "VentaDescuentos");

            migrationBuilder.DropTable(
                name: "VentaImpuestos");

            migrationBuilder.DropTable(
                name: "Descuentos");

            migrationBuilder.DropTable(
                name: "Impuestos");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_RolId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_RolPermisos_PermisoId",
                table: "RolPermisos");

            migrationBuilder.DropIndex(
                name: "IX_RolPermisos_RolId",
                table: "RolPermisos");

            migrationBuilder.DropIndex(
                name: "IX_RegistrosAuditoria_CorrelationId",
                table: "RegistrosAuditoria");

            migrationBuilder.DropIndex(
                name: "IX_RegistrosAuditoria_Resultado",
                table: "RegistrosAuditoria");

            migrationBuilder.DropIndex(
                name: "IX_RegistrosAuditoria_UsuarioId",
                table: "RegistrosAuditoria");

            migrationBuilder.DropColumn(
                name: "RolId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "PermisoId",
                table: "RolPermisos");

            migrationBuilder.DropColumn(
                name: "RolId",
                table: "RolPermisos");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "RegistrosAuditoria");

            migrationBuilder.DropColumn(
                name: "Entidad",
                table: "RegistrosAuditoria");

            migrationBuilder.DropColumn(
                name: "Error",
                table: "RegistrosAuditoria");

            migrationBuilder.DropColumn(
                name: "Ip",
                table: "RegistrosAuditoria");

            migrationBuilder.DropColumn(
                name: "Motivo",
                table: "RegistrosAuditoria");

            migrationBuilder.DropColumn(
                name: "Resultado",
                table: "RegistrosAuditoria");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "RegistrosAuditoria");

            migrationBuilder.DropColumn(
                name: "ValoresAnteriores",
                table: "RegistrosAuditoria");

            migrationBuilder.DropColumn(
                name: "ValoresNuevos",
                table: "RegistrosAuditoria");
        }
    }
}
