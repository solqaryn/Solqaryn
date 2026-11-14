using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

public sealed class ReportesComprasContractTests
{
    [Fact]
    public void Controller_exige_auth_ruta_y_dependencias_acotadas()
    {
        var type = typeof(ReportesComprasController);
        Assert.NotNull(type.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Equal("compras/reportes", Assert.Single(type.GetCustomAttributes<RouteAttribute>()).Template);

        var ctor = Assert.Single(type.GetConstructors());
        var parameters = ctor.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(IReporteComprasService), parameters[0].ParameterType);
        Assert.Equal(typeof(IAuditoriaService), parameters[1].ParameterType);
        Assert.True(parameters[1].HasDefaultValue);
        Assert.Null(parameters[1].DefaultValue);
    }

    [Fact]
    public void Detalle_exige_get_y_permiso_compras_ver()
    {
        var method = typeof(ReportesComprasController).GetMethod(nameof(ReportesComprasController.GetDetalle))
            ?? throw new InvalidOperationException("No existe GetDetalle.");

        Assert.Equal("detalle", Assert.Single(method.GetCustomAttributes<HttpGetAttribute>()).Template);
        var permiso = Assert.Single(method.CustomAttributes.Where(x => x.AttributeType == typeof(RequierePermisoAttribute)));
        Assert.Equal((int)ModuloSistema.Compras, Convert.ToInt32(permiso.ConstructorArguments[0].Value));
        Assert.Equal((int)AccionPermiso.Ver, Convert.ToInt32(permiso.ConstructorArguments[1].Value));
    }

    [Fact]
    public async Task Detalle_invalido_falla_cerrado_y_no_registra_auditoria_de_exito()
    {
        var auditoria = new RecordingAuditoriaService();
        var controller = CrearController(new ThrowingReporteComprasService(), auditoria, "corr-invalid");

        var result = await controller.GetDetalle(new ReporteComprasFiltroDto());

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(auditoria.Entries);
    }

    [Fact]
    public async Task Detalle_exitoso_registra_solo_metadata_acotada_de_correlacion()
    {
        var auditoria = new RecordingAuditoriaService();
        var controller = CrearController(new SuccessfulReporteComprasService(), auditoria, "corr-123");

        var result = await controller.GetDetalle(new ReporteComprasFiltroDto());

        Assert.IsType<OkObjectResult>(result);
        var entry = Assert.Single(auditoria.Entries);
        Assert.Equal(ModuloSistema.Compras, entry.Modulo);
        Assert.Equal(AccionPermiso.Ver, entry.Accion);
        Assert.Equal("ReportesCompras", entry.Entidad);
        Assert.Contains("reporte de compras", entry.Descripcion, StringComparison.OrdinalIgnoreCase);

        var metadata = Assert.IsAssignableFrom<object>(entry.ValoresNuevos);
        var properties = metadata.GetType().GetProperties();
        Assert.Equal(new[] { "CorrelationId", "Reporte" }, properties.Select(x => x.Name).OrderBy(x => x).ToArray());
        Assert.Equal("corr-123", metadata.GetType().GetProperty("CorrelationId")!.GetValue(metadata));
        Assert.Equal("detalle", metadata.GetType().GetProperty("Reporte")!.GetValue(metadata));
    }

    [Fact]
    public void DTO_no_inventa_porcentaje_ranking_sla_ni_score()
    {
        var properties = typeof(ReporteComprasDetalleDto).GetProperties().Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("VariacionPrecioPorcentual", properties);
        Assert.DoesNotContain("VariacionPrecioPorcentaje", properties);
        Assert.DoesNotContain("RankingProveedor", properties);
        Assert.DoesNotContain("SlaScore", properties);
        Assert.DoesNotContain("CumplimientoScore", properties);
    }

    [Fact]
    public void Filtro_conserva_dimensiones_restrictivas_y_paginacion_acotable()
    {
        var properties = typeof(ReporteComprasFiltroDto).GetProperties().Select(x => x.Name).ToHashSet();
        foreach (var expected in new[]
        {
            nameof(ReporteComprasFiltroDto.DesdeUtc), nameof(ReporteComprasFiltroDto.HastaUtc),
            nameof(ReporteComprasFiltroDto.ProveedorId), nameof(ReporteComprasFiltroDto.ProductoId),
            nameof(ReporteComprasFiltroDto.ProductoVarianteId), nameof(ReporteComprasFiltroDto.EstadoOrden),
            nameof(ReporteComprasFiltroDto.EstadoFactura), nameof(ReporteComprasFiltroDto.EstadoRecepcion),
            nameof(ReporteComprasFiltroDto.EstadoDevolucion), nameof(ReporteComprasFiltroDto.Page),
            nameof(ReporteComprasFiltroDto.PageSize)
        })
        {
            Assert.Contains(expected, properties);
        }
    }

    private static ReportesComprasController CrearController(
        IReporteComprasService service,
        IAuditoriaService auditoria,
        string correlationId)
    {
        return new ReportesComprasController(service, auditoria)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { TraceIdentifier = correlationId }
            }
        };
    }

    private sealed class ThrowingReporteComprasService : IReporteComprasService
    {
        public Task<PagedResult<ReporteComprasDetalleDto>> ObtenerDetallePaginadoAsync(
            ReporteComprasFiltroDto filtro,
            CancellationToken cancellationToken = default) =>
            Task.FromException<PagedResult<ReporteComprasDetalleDto>>(new ArgumentException("Filtro inválido."));
    }

    private sealed class SuccessfulReporteComprasService : IReporteComprasService
    {
        public Task<PagedResult<ReporteComprasDetalleDto>> ObtenerDetallePaginadoAsync(
            ReporteComprasFiltroDto filtro,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new PagedResult<ReporteComprasDetalleDto>
            {
                Items = [],
                Page = filtro.Page,
                PageSize = filtro.PageSize,
                TotalCount = 0
            });
    }

    private sealed class RecordingAuditoriaService : IAuditoriaService
    {
        public List<AuditEntry> Entries { get; } = [];

        public Task RegistrarAsync(
            ModuloSistema modulo,
            AccionPermiso accion,
            string descripcion,
            int? referenciaId = null,
            string? entidad = null,
            object? valoresAnteriores = null,
            object? valoresNuevos = null,
            string? motivo = null,
            string resultado = "Exito",
            string? error = null)
        {
            Entries.Add(new AuditEntry(modulo, accion, descripcion, entidad, valoresNuevos));
            return Task.CompletedTask;
        }

        public Task RegistrarEstrictoAsync(
            ModuloSistema modulo,
            AccionPermiso accion,
            string descripcion,
            int? referenciaId = null,
            string? entidad = null,
            object? valoresAnteriores = null,
            object? valoresNuevos = null,
            string? motivo = null,
            string resultado = "Exito",
            string? error = null) => throw new NotSupportedException();

        public Task<PagedResult<RegistroAuditoriaDto>> GetFilteredAsync(AuditoriaFiltroDto filtro) =>
            throw new NotSupportedException();
    }

    private sealed record AuditEntry(
        ModuloSistema Modulo,
        AccionPermiso Accion,
        string Descripcion,
        string? Entidad,
        object? ValoresNuevos);
}
