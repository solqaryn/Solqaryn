using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Solqaryn.Infrastructure.Persistence;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations.Oracle;

[DbContext(typeof(AppDbContext))]
[Migration("20261014130000_CommercialTenantNaturalKeysPostStorefront")]
public partial class CommercialTenantNaturalKeysPostStorefront : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        // Ejecutar DESPUÉS de 20261014112000_PublicStoreAccountEmailPerEmpresa.
        // 12 claves pendientes; cuenta pública ya tenía unicidad por Empresa.
        // Expand sin tocar históricos. Primero se crean las unicidades tenant,
        // luego se retiran las restricciones globales, conservando índices de soporte.
        m.AddColumn<string>(
            name: "NombreNormalizadoGlobalUnico", table: "Roles",
            type: "varchar(80)", maxLength: 80, nullable: true,
            computedColumnSql: "IF(EmpresaId IS NULL, NombreNormalizado, NULL)", stored: true);
        m.CreateIndex("UX_Roles_NombreNormalizadoGlobalUnico", "Roles",
            "NombreNormalizadoGlobalUnico", unique: true);
        m.CreateIndex("UX_DevolucionesCliente_Empresa_IdempotencyKey", "DevolucionesCliente",
            new[] { "EmpresaId", "IdempotencyKey" }, unique: true);
        m.CreateIndex("UX_DevolucionesProveedor_Empresa_IdempotencyKey", "DevolucionesProveedor",
            new[] { "EmpresaId", "IdempotencyKey" }, unique: true);
        m.CreateIndex("UX_DevolucionesProveedor_Empresa_NumeroDevolucion", "DevolucionesProveedor",
            new[] { "EmpresaId", "NumeroDevolucion" }, unique: true);
        m.CreateIndex("UX_OrdenesCompra_Empresa_IdempotencyKey", "OrdenesCompra",
            new[] { "EmpresaId", "IdempotencyKey" }, unique: true);
        m.CreateIndex("UX_OrdenesCompra_Empresa_NumeroOrden", "OrdenesCompra",
            new[] { "EmpresaId", "NumeroOrden" }, unique: true);
        m.CreateIndex("UX_RecepcionesCompra_Empresa_IdempotencyKey", "RecepcionesCompra",
            new[] { "EmpresaId", "IdempotencyKey" }, unique: true);
        m.CreateIndex("UX_RecepcionesCompra_Empresa_NumeroRecepcion", "RecepcionesCompra",
            new[] { "EmpresaId", "NumeroRecepcion" }, unique: true);
        m.CreateIndex("UX_ReservasInventario_Empresa_Numero", "ReservasInventario",
            new[] { "EmpresaId", "Numero" }, unique: true);
        m.CreateIndex("UX_SeriesInventario_Empresa_NumeroSerie", "SeriesInventario",
            new[] { "EmpresaId", "NumeroSerie" }, unique: true);
        m.CreateIndex("UX_SolicitudesCompra_Empresa_NumeroSolicitud", "SolicitudesCompra",
            new[] { "EmpresaId", "NumeroSolicitud" }, unique: true);
        m.CreateIndex("UX_Sucursales_Empresa_CodigoActivoUnico", "Sucursales",
            new[] { "EmpresaId", "CodigoActivoUnico" }, unique: true);
        m.CreateIndex("UX_Roles_Empresa_NombreNormalizado", "Roles",
            new[] { "EmpresaId", "NombreNormalizado" }, unique: true);

        m.DropIndex("UX_DevolucionesCliente_IdempotencyKey", "DevolucionesCliente");
        m.CreateIndex("UX_DevolucionesCliente_IdempotencyKey", "DevolucionesCliente", "IdempotencyKey");
        m.DropIndex("UX_DevolucionesProveedor_IdempotencyKey", "DevolucionesProveedor");
        m.CreateIndex("UX_DevolucionesProveedor_IdempotencyKey", "DevolucionesProveedor", "IdempotencyKey");
        m.DropIndex("UX_DevolucionesProveedor_NumeroDevolucion", "DevolucionesProveedor");
        m.CreateIndex("UX_DevolucionesProveedor_NumeroDevolucion", "DevolucionesProveedor", "NumeroDevolucion");
        m.DropIndex("UX_OrdenesCompra_IdempotencyKey", "OrdenesCompra");
        m.CreateIndex("UX_OrdenesCompra_IdempotencyKey", "OrdenesCompra", "IdempotencyKey");
        m.DropIndex("UX_OrdenesCompra_NumeroOrden", "OrdenesCompra");
        m.CreateIndex("UX_OrdenesCompra_NumeroOrden", "OrdenesCompra", "NumeroOrden");
        m.DropIndex("UX_RecepcionesCompra_IdempotencyKey", "RecepcionesCompra");
        m.CreateIndex("UX_RecepcionesCompra_IdempotencyKey", "RecepcionesCompra", "IdempotencyKey");
        m.DropIndex("UX_RecepcionesCompra_NumeroRecepcion", "RecepcionesCompra");
        m.CreateIndex("UX_RecepcionesCompra_NumeroRecepcion", "RecepcionesCompra", "NumeroRecepcion");
        m.DropIndex("UX_ReservasInventario_Numero", "ReservasInventario");
        m.CreateIndex("UX_ReservasInventario_Numero", "ReservasInventario", "Numero");
        m.DropIndex("UX_SeriesInventario_NumeroSerie", "SeriesInventario");
        m.CreateIndex("UX_SeriesInventario_NumeroSerie", "SeriesInventario", "NumeroSerie");
        m.DropIndex("UX_SolicitudesCompra_NumeroSolicitud", "SolicitudesCompra");
        m.CreateIndex("UX_SolicitudesCompra_NumeroSolicitud", "SolicitudesCompra", "NumeroSolicitud");
        m.DropIndex("UX_Sucursales_Codigo_Activo", "Sucursales");
        m.CreateIndex("UX_Sucursales_Codigo_Activo", "Sucursales", "CodigoActivoUnico");
        m.DropIndex("IX_Roles_NombreNormalizado", "Roles");
        m.CreateIndex("IX_Roles_NombreNormalizado", "Roles", "NombreNormalizado");
    }

    protected override void Down(MigrationBuilder m) =>
        throw new NotSupportedException(
            "No restaurar índices globales de negocio ni quitar protección tenant sin backup DEV.");
}
