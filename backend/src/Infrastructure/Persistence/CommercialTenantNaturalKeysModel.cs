using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Infrastructure.Persistence;

/// <summary>
/// Corrige unicidades naturales aún globales de módulos comerciales.
/// Las claves técnicas públicas (token/hash), identificadores de outbox y
/// claves ya acotadas por un padre globalmente identificado permanecen intactas.
/// </summary>
public static class CommercialTenantNaturalKeysModel
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder Get(string name)
        {
            var type = modelBuilder.Model.FindEntityType(name)
                ?? throw new InvalidOperationException($"Modelo tenant no encontrado: {name}");
            return modelBuilder.Entity(type.ClrType);
        }

        {
            var b = Get("Solqaryn.Domain.Entities.DevolucionCliente");
            b.HasIndex(new[] { "EmpresaId", "IdempotencyKey" }).IsUnique()
                .HasDatabaseName("UX_DevolucionesCliente_Empresa_IdempotencyKey");
            // Retener índice soporte legacy, pero nunca la unicidad global.
            b.HasIndex(new[] { "IdempotencyKey" }).IsUnique(false);
        }

        {
            var b = Get("Solqaryn.Domain.Entities.DevolucionProveedor");
            b.HasIndex(new[] { "EmpresaId", "IdempotencyKey" }).IsUnique()
                .HasDatabaseName("UX_DevolucionesProveedor_Empresa_IdempotencyKey");
            // Retener índice soporte legacy, pero nunca la unicidad global.
            b.HasIndex(new[] { "IdempotencyKey" }).IsUnique(false);
        }

        {
            var b = Get("Solqaryn.Domain.Entities.DevolucionProveedor");
            b.HasIndex(new[] { "EmpresaId", "NumeroDevolucion" }).IsUnique()
                .HasDatabaseName("UX_DevolucionesProveedor_Empresa_NumeroDevolucion");
            // Retener índice soporte legacy, pero nunca la unicidad global.
            b.HasIndex(new[] { "NumeroDevolucion" }).IsUnique(false);
        }

        {
            var b = Get("Solqaryn.Domain.Entities.OrdenCompra");
            b.HasIndex(new[] { "EmpresaId", "IdempotencyKey" }).IsUnique()
                .HasDatabaseName("UX_OrdenesCompra_Empresa_IdempotencyKey");
            // Retener índice soporte legacy, pero nunca la unicidad global.
            b.HasIndex(new[] { "IdempotencyKey" }).IsUnique(false);
        }

        {
            var b = Get("Solqaryn.Domain.Entities.OrdenCompra");
            b.HasIndex(new[] { "EmpresaId", "NumeroOrden" }).IsUnique()
                .HasDatabaseName("UX_OrdenesCompra_Empresa_NumeroOrden");
            // Retener índice soporte legacy, pero nunca la unicidad global.
            b.HasIndex(new[] { "NumeroOrden" }).IsUnique(false);
        }

        {
            var b = Get("Solqaryn.Domain.Entities.RecepcionCompra");
            b.HasIndex(new[] { "EmpresaId", "IdempotencyKey" }).IsUnique()
                .HasDatabaseName("UX_RecepcionesCompra_Empresa_IdempotencyKey");
            // Retener índice soporte legacy, pero nunca la unicidad global.
            b.HasIndex(new[] { "IdempotencyKey" }).IsUnique(false);
        }

        {
            var b = Get("Solqaryn.Domain.Entities.RecepcionCompra");
            b.HasIndex(new[] { "EmpresaId", "NumeroRecepcion" }).IsUnique()
                .HasDatabaseName("UX_RecepcionesCompra_Empresa_NumeroRecepcion");
            // Retener índice soporte legacy, pero nunca la unicidad global.
            b.HasIndex(new[] { "NumeroRecepcion" }).IsUnique(false);
        }

        {
            var b = Get("Solqaryn.Domain.Entities.ReservaInventario");
            b.HasIndex(new[] { "EmpresaId", "Numero" }).IsUnique()
                .HasDatabaseName("UX_ReservasInventario_Empresa_Numero");
            // Retener índice soporte legacy, pero nunca la unicidad global.
            b.HasIndex(new[] { "Numero" }).IsUnique(false);
        }

        {
            var b = Get("Solqaryn.Domain.Entities.SerieInventario");
            b.HasIndex(new[] { "EmpresaId", "NumeroSerie" }).IsUnique()
                .HasDatabaseName("UX_SeriesInventario_Empresa_NumeroSerie");
            // Retener índice soporte legacy, pero nunca la unicidad global.
            b.HasIndex(new[] { "NumeroSerie" }).IsUnique(false);
        }

        {
            var b = Get("Solqaryn.Domain.Entities.SolicitudCompra");
            b.HasIndex(new[] { "EmpresaId", "NumeroSolicitud" }).IsUnique()
                .HasDatabaseName("UX_SolicitudesCompra_Empresa_NumeroSolicitud");
            // Retener índice soporte legacy, pero nunca la unicidad global.
            b.HasIndex(new[] { "NumeroSolicitud" }).IsUnique(false);
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Sucursal");
            b.HasIndex(new[] { "EmpresaId", "CodigoActivoUnico" }).IsUnique()
                .HasDatabaseName("UX_Sucursales_Empresa_CodigoActivoUnico");
            // Retener índice soporte legacy, pero nunca la unicidad global.
            b.HasIndex(new[] { "CodigoActivoUnico" }).IsUnique(false);
        }

        // TiendaCuentaCliente ya fue certificada por la migración posterior
        // 20261014112000_PublicStoreAccountEmailPerEmpresa. Su índice
        // UX_TiendaCuentasCliente_Correo es compuesto y único por empresa;
        // NO redefinirlo ni recrearlo aquí.

        {
            var b = Get("Solqaryn.Domain.Entities.Rol");
            b.HasIndex(new[] { "EmpresaId", "NombreNormalizado" }).IsUnique()
                .HasDatabaseName("UX_Roles_Empresa_NombreNormalizado");
            // Retener índice soporte legacy, pero nunca la unicidad global.
            b.HasIndex(new[] { "NombreNormalizado" }).IsUnique(false);
        }

        // Los roles plantilla/plataforma con EmpresaId NULL mantienen unicidad
        // global sin impedir que dos empresas tengan su propio GERENTE/ADMIN.
        {
            var b = Get("Solqaryn.Domain.Entities.Rol");
            b.Property<string>("NombreNormalizadoGlobalUnico")
                .HasMaxLength(80).IsRequired(false)
                .HasComputedColumnSql(
                    "IF(EmpresaId IS NULL, NombreNormalizado, NULL)", stored: true);
            b.HasIndex(new[] { "NombreNormalizadoGlobalUnico" }).IsUnique()
                .HasDatabaseName("UX_Roles_NombreNormalizadoGlobalUnico");
        }
    }
}
