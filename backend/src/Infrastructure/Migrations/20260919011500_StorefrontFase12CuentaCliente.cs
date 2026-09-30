using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260919011500_StorefrontFase12CuentaCliente")]
    public partial class StorefrontFase12CuentaCliente : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TiendaCuentasCliente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    Correo = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false),
                    CorreoNormalizado = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Activa = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UltimoAccesoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiendaCuentasCliente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TiendaCuentasCliente_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TiendaDireccionesCliente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CuentaClienteId = table.Column<int>(type: "int", nullable: false),
                    Alias = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false),
                    Recibe = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    Telefono = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Direccion = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                    Predeterminada = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiendaDireccionesCliente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TiendaDireccionesCliente_TiendaCuentasCliente_CuentaClienteId",
                        column: x => x.CuentaClienteId,
                        principalTable: "TiendaCuentasCliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TiendaFavoritosCliente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CuentaClienteId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiendaFavoritosCliente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TiendaFavoritosCliente_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TiendaFavoritosCliente_TiendaCuentasCliente_CuentaClienteId",
                        column: x => x.CuentaClienteId,
                        principalTable: "TiendaCuentasCliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TiendaSesionesCliente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CuentaClienteId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false),
                    ExpiraUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RevocadaUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UltimoUsoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiendaSesionesCliente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TiendaSesionesCliente_TiendaCuentasCliente_CuentaClienteId",
                        column: x => x.CuentaClienteId,
                        principalTable: "TiendaCuentasCliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "UX_TiendaCuentasCliente_Correo", table: "TiendaCuentasCliente", column: "CorreoNormalizado", unique: true);
            migrationBuilder.CreateIndex(name: "UX_TiendaCuentasCliente_Cliente", table: "TiendaCuentasCliente", column: "ClienteId", unique: true);
            migrationBuilder.CreateIndex(name: "IX_TiendaDireccionesCliente_Cuenta_Predeterminada", table: "TiendaDireccionesCliente", columns: new[] { "CuentaClienteId", "Predeterminada" });
            migrationBuilder.CreateIndex(name: "IX_TiendaFavoritosCliente_ProductoId", table: "TiendaFavoritosCliente", column: "ProductoId");
            migrationBuilder.CreateIndex(name: "UX_TiendaFavoritosCliente_Cuenta_Producto", table: "TiendaFavoritosCliente", columns: new[] { "CuentaClienteId", "ProductoId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_TiendaSesionesCliente_Cuenta_Expira", table: "TiendaSesionesCliente", columns: new[] { "CuentaClienteId", "ExpiraUtc" });
            migrationBuilder.CreateIndex(name: "UX_TiendaSesionesCliente_TokenHash", table: "TiendaSesionesCliente", column: "TokenHash", unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TiendaDireccionesCliente");
            migrationBuilder.DropTable(name: "TiendaFavoritosCliente");
            migrationBuilder.DropTable(name: "TiendaSesionesCliente");
            migrationBuilder.DropTable(name: "TiendaCuentasCliente");
        }
    }
}
