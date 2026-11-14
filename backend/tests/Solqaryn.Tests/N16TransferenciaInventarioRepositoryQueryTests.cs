using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N16TransferenciaInventarioRepositoryQueryTests
{
    [Fact]
    public async Task GetPaged_AplicaScopeEstadoAlmacenYPaginacion()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n16-transferencias-{Guid.NewGuid():N}")
            .Options;

        await using var context = new AppDbContext(options);
        SeedAlmacenes(context, 10, 20, 30);
        var transferencias = new[]
        {
            Crear(1, "TRF-001", 10, 20, 7, EstadoTransferenciaInventario.Solicitada, new DateTime(2026, 8, 14)),
            Crear(2, "TRF-002", 10, 30, 7, EstadoTransferenciaInventario.Solicitada, new DateTime(2026, 8, 15)),
            Crear(3, "TRF-003", 10, 20, 8, EstadoTransferenciaInventario.Solicitada, new DateTime(2026, 8, 16)),
            Crear(4, "TRF-004", 10, 20, 7, EstadoTransferenciaInventario.Aprobada, new DateTime(2026, 8, 16))
        };
        context.Set<TransferenciaInventario>().AddRange(transferencias);
        await context.SaveChangesAsync();

        var repository = new TransferenciaInventarioRepository(context);
        var filtro = new TransferenciaInventarioFiltroDto
        {
            UsuarioIdScope = 7,
            Estado = EstadoTransferenciaInventario.Solicitada,
            AlmacenOrigenId = 10,
            AlmacenDestinoId = 20,
            Page = 1,
            PageSize = 10,
            SortBy = "numero",
            SortDirection = "asc"
        };

        var (items, total) = await repository.GetPagedAsync(filtro);

        Assert.Equal(1, total);
        var item = Assert.Single(items);
        Assert.Equal("TRF-001", item.Numero);
    }

    [Fact]
    public async Task GetPaged_BusquedaYPageSize_DevuelvenTotalSinPerderPaginacion()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n16-transferencias-page-{Guid.NewGuid():N}")
            .Options;

        await using var context = new AppDbContext(options);
        SeedAlmacenes(context, 1, 2, 3, 4);
        context.Set<TransferenciaInventario>().AddRange(
            Crear(11, "TRF-ALFA-01", 1, 2, 7, EstadoTransferenciaInventario.Borrador, new DateTime(2026, 8, 14), "reposición central"),
            Crear(12, "TRF-ALFA-02", 1, 3, 7, EstadoTransferenciaInventario.Borrador, new DateTime(2026, 8, 15), "reposición secundaria"),
            Crear(13, "TRF-BETA-01", 1, 4, 7, EstadoTransferenciaInventario.Borrador, new DateTime(2026, 8, 16), "traslado urgente"));
        await context.SaveChangesAsync();

        var repository = new TransferenciaInventarioRepository(context);
        var filtro = new TransferenciaInventarioFiltroDto
        {
            Search = "reposición",
            Page = 2,
            PageSize = 1,
            SortBy = "numero",
            SortDirection = "asc"
        };

        var (items, total) = await repository.GetPagedAsync(filtro);

        Assert.Equal(2, total);
        var item = Assert.Single(items);
        Assert.Equal("TRF-ALFA-02", item.Numero);
    }

    private static void SeedAlmacenes(AppDbContext context, params int[] ids)
    {
        context.Set<Almacen>().AddRange(ids.Distinct().Select(id => new Almacen
        {
            Id = id,
            SucursalId = 1,
            Codigo = $"ALM-{id}",
            Nombre = $"Almacén {id}",
            Tipo = TipoAlmacen.Bodega,
            Activo = true,
            Eliminado = false,
            CreadoPorUsuarioId = 7
        }));
    }

    private static TransferenciaInventario Crear(
        int id,
        string numero,
        int origen,
        int destino,
        int usuarioId,
        EstadoTransferenciaInventario estado,
        DateTime fecha,
        string? observaciones = null)
    {
        var detalle = new TransferenciaInventarioDetalle
        {
            Id = 1000 + id,
            ProductoVarianteId = 1,
            CreadoPorUsuarioId = usuarioId,
            FechaCreacion = fecha
        };
        detalle.EstablecerCantidadSolicitada(1);

        var transferencia = new TransferenciaInventario
        {
            Id = id,
            Numero = numero,
            AlmacenOrigenId = origen,
            AlmacenDestinoId = destino,
            Observaciones = observaciones,
            CreadoPorUsuarioId = usuarioId,
            FechaCreacion = fecha,
            Detalles = new List<TransferenciaInventarioDetalle> { detalle }
        };

        if (estado is EstadoTransferenciaInventario.Solicitada or EstadoTransferenciaInventario.Aprobada)
            transferencia.Solicitar(usuarioId, fecha);

        if (estado == EstadoTransferenciaInventario.Aprobada)
        {
            detalle.AprobarCantidad(1);
            transferencia.Aprobar(usuarioId, fecha);
        }

        return transferencia;
    }
}
