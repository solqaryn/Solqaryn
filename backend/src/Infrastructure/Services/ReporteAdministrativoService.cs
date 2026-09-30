using System.Globalization;
using System.Net;
using System.Text;
using ClosedXML.Excel;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Solqaryn.Infrastructure.Services;

public sealed class ReporteAdministrativoService : IReporteAdministrativoService
{
    private readonly AppDbContext _db;

    private static readonly HashSet<AccionPermiso> AccionesSensibles =
    [
        AccionPermiso.Administrar,
        AccionPermiso.EliminarPermanente,
        AccionPermiso.AsignarRol,
        AccionPermiso.RestablecerContrasena,
        AccionPermiso.CambiarEstado,
        AccionPermiso.Exportar,
        AccionPermiso.Importar,
        AccionPermiso.Aprobar,
        AccionPermiso.Cerrar,
        AccionPermiso.Reabrir
    ];

    public ReporteAdministrativoService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ResumenAdministrativoDto> ObtenerResumenAsync(
        ReporteAdministrativoFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        var (desde, hasta) = NormalizarPeriodo(filtro);
        var usuarios = await ObtenerUsuariosAccesoAsync(cancellationToken);
        var roles = await ObtenerRolesPermisosAsync(cancellationToken);
        var auditoria = await ObtenerResumenAuditoriaAsync(
            new ReporteAdministrativoFiltroDto { Desde = desde, Hasta = hasta },
            cancellationToken);

        var resumen = new ResumenAdministrativoDto
        {
            Desde = desde,
            Hasta = hasta,
            UsuariosTotales = usuarios.Count,
            UsuariosActivos = usuarios.Count(x => x.Activo && !x.Bloqueado && !x.Eliminado && x.RolActivo),
            UsuariosBloqueados = usuarios.Count(x => x.Bloqueado),
            UsuariosEliminados = usuarios.Count(x => x.Eliminado),
            UsuariosPrivilegiados = usuarios.Count(x => x.PermisosSensibles > 0),
            RolesTotales = roles.Count,
            RolesActivos = roles.Count(x => x.Activo && !x.Eliminado),
            RolesSinPermisos = roles.Count(x => x.Activo && !x.Eliminado && x.PermisosAsignados == 0),
            RolesSinUsuarios = roles.Count(x => x.Activo && !x.Eliminado && x.UsuariosAsignados == 0),
            PermisosCatalogados = await _db.Permisos.AsNoTracking().CountAsync(x => x.Activo && !x.Eliminado, cancellationToken),
            EventosAuditoria = auditoria.Total,
            EventosExitosos = auditoria.Exitosos,
            EventosRechazados = auditoria.Rechazados,
            EventosConError = auditoria.ConError,
            ActividadPorModulo = auditoria.PorModulo
        };

        AgregarAlertas(resumen, usuarios, roles);
        return resumen;
    }

    public async Task<List<UsuarioAccesoReporteDto>> ObtenerUsuariosAccesoAsync(
        CancellationToken cancellationToken = default)
    {
        var permisosActivos = await _db.RolPermisos
            .AsNoTracking()
            .Where(x => x.Permiso.Activo && !x.Permiso.Eliminado)
            .Select(x => new { x.RolId, x.Permiso.Modulo, x.Permiso.Accion })
            .ToListAsync(cancellationToken);

        var permisosPorRol = permisosActivos
            .GroupBy(x => x.RolId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Total = g.Select(x => new { x.Modulo, x.Accion }).Distinct().Count(),
                    Sensibles = g.Where(x => AccionesSensibles.Contains(x.Accion))
                        .Select(x => new { x.Modulo, x.Accion }).Distinct().Count()
                });

        var usuarios = await _db.Usuarios
            .AsNoTracking()
            .Include(x => x.RolEntidad)
            .OrderBy(x => x.NombreUsuario)
            .ToListAsync(cancellationToken);

        return usuarios.Select(usuario =>
        {
            var rol = usuario.RolEntidad;
            permisosPorRol.TryGetValue(usuario.RolId, out var conteo);

            return new UsuarioAccesoReporteDto
            {
                UsuarioId = usuario.Id,
                NombreUsuario = usuario.NombreUsuario,
                NombreCompleto = usuario.NombreCompleto,
                RolId = usuario.RolId,
                Rol = rol?.Nombre ?? "Sin rol",
                EsAdministrador = rol?.EsAdministrador == true,
                RolActivo = rol is { Activo: true, Eliminado: false },
                Activo = usuario.Activo,
                Bloqueado = usuario.Bloqueado,
                Eliminado = usuario.Eliminado,
                PermisosEfectivos = conteo?.Total ?? 0,
                PermisosSensibles = conteo?.Sensibles ?? 0,
                EstadoAcceso = EstadoAcceso(usuario.Activo, usuario.Bloqueado, usuario.Eliminado, rol),
                FechaCreacion = usuario.FechaCreacion,
                FechaActualizacion = usuario.FechaActualizacion
            };
        }).ToList();
    }

    public async Task<List<RolPermisosReporteDto>> ObtenerRolesPermisosAsync(
        CancellationToken cancellationToken = default)
    {
        var totalPosible = await _db.Permisos
            .AsNoTracking()
            .CountAsync(x => x.Activo && !x.Eliminado, cancellationToken);

        var roles = await _db.Roles
            .AsNoTracking()
            .OrderBy(x => x.Nombre)
            .ToListAsync(cancellationToken);

        var permisos = await _db.RolPermisos
            .AsNoTracking()
            .Include(x => x.Permiso)
            .Where(x => x.Permiso.Activo && !x.Permiso.Eliminado)
            .ToListAsync(cancellationToken);

        var usuariosPorRol = await _db.Usuarios
            .AsNoTracking()
            .Where(x => !x.Eliminado)
            .GroupBy(x => x.RolId)
            .Select(g => new { RolId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.RolId, x => x.Total, cancellationToken);

        return roles.Select(rol =>
        {
            var filas = permisos
                .Where(x => x.RolId == rol.Id)
                .GroupBy(x => x.PermisoId)
                .Select(x => x.First())
                .OrderBy(x => x.Permiso.Modulo.ToString())
                .ThenBy(x => x.Permiso.Accion.ToString())
                .ToList();
            var total = filas.Count;
            var sensibles = filas.Count(x => AccionesSensibles.Contains(x.Permiso.Accion));
            var cobertura = totalPosible == 0 ? 0 : Math.Round(total * 100m / totalPosible, 2);

            return new RolPermisosReporteDto
            {
                RolId = rol.Id,
                Rol = rol.Nombre,
                EsSistema = rol.EsSistema,
                EsAdministrador = rol.EsAdministrador,
                Activo = rol.Activo,
                Eliminado = rol.Eliminado,
                UsuariosAsignados = usuariosPorRol.GetValueOrDefault(rol.Id),
                PermisosAsignados = total,
                ModulosConAcceso = filas.Select(x => x.Permiso.Modulo).Distinct().Count(),
                PermisosSensibles = sensibles,
                PorcentajeCobertura = cobertura,
                NivelPrivilegio = NivelPrivilegio(rol.EsAdministrador, cobertura, sensibles),
                EstadoConfiguracion = EstadoConfiguracion(rol, total, usuariosPorRol.GetValueOrDefault(rol.Id)),
                Permisos = filas.Select(x => $"{x.Permiso.Modulo}:{x.Permiso.Accion}").ToList()
            };
        }).ToList();
    }

    public async Task<AuditoriaResumenDto> ObtenerResumenAuditoriaAsync(
        ReporteAdministrativoFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        var (desde, hasta) = NormalizarPeriodo(filtro);
        var eventos = await _db.RegistrosAuditoria
            .AsNoTracking()
            .Where(x => x.Fecha >= desde && x.Fecha <= hasta)
            .Select(x => new { x.Modulo, x.Accion, x.Resultado, x.UsuarioId })
            .ToListAsync(cancellationToken);

        var porModulo = eventos
            .GroupBy(x => x.Modulo)
            .Select(g => new ActividadAuditoriaResumenDto
            {
                Modulo = g.Key.ToString(),
                Total = g.Count(),
                Exitosos = g.Count(x => EsExito(x.Resultado)),
                Rechazados = g.Count(x => EsRechazado(x.Resultado)),
                ConError = g.Count(x => !EsExito(x.Resultado) && !EsRechazado(x.Resultado))
            })
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Modulo)
            .ToList();

        return new AuditoriaResumenDto
        {
            Desde = desde,
            Hasta = hasta,
            Total = eventos.Count,
            Exitosos = eventos.Count(x => EsExito(x.Resultado)),
            Rechazados = eventos.Count(x => EsRechazado(x.Resultado)),
            ConError = eventos.Count(x => !EsExito(x.Resultado) && !EsRechazado(x.Resultado)),
            UsuariosUnicos = eventos.Where(x => x.UsuarioId.HasValue).Select(x => x.UsuarioId).Distinct().Count(),
            PorModulo = porModulo,
            PorAccion = eventos
                .GroupBy(x => x.Accion)
                .Select(g => new ActividadAuditoriaAccionDto { Accion = g.Key.ToString(), Total = g.Count() })
                .OrderByDescending(x => x.Total)
                .ThenBy(x => x.Accion)
                .ToList()
        };
    }

    public async Task<ArchivoDescargableDto> ExportarAsync(
        string tipo,
        string formato,
        ReporteAdministrativoFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        var tipoNormalizado = tipo.Trim().ToLowerInvariant();
        var formatoNormalizado = formato.Trim().ToLowerInvariant();
        if (formatoNormalizado is not ("csv" or "xlsx" or "pdf"))
            throw new BusinessRuleException("El formato de exportación debe ser csv, xlsx o pdf.");

        string[] encabezados;
        List<string[]> filas;
        string nombre;

        switch (tipoNormalizado)
        {
            case "usuarios":
            case "usuarios-accesos":
            {
                var datos = await ObtenerUsuariosAccesoAsync(cancellationToken);
                encabezados = ["UsuarioId", "Usuario", "NombreCompleto", "Rol", "Administrador", "EstadoAcceso", "PermisosEfectivos", "PermisosSensibles", "FechaCreacion"];
                filas = datos.Select(x => new[]
                {
                    x.UsuarioId.ToString(CultureInfo.InvariantCulture), x.NombreUsuario, x.NombreCompleto, x.Rol,
                    SiNo(x.EsAdministrador), x.EstadoAcceso,
                    x.PermisosEfectivos.ToString(CultureInfo.InvariantCulture),
                    x.PermisosSensibles.ToString(CultureInfo.InvariantCulture),
                    x.FechaCreacion.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                }).ToList();
                nombre = "usuarios-accesos";
                break;
            }
            case "roles":
            case "roles-permisos":
            {
                var datos = await ObtenerRolesPermisosAsync(cancellationToken);
                encabezados = ["RolId", "Rol", "Activo", "Administrador", "Usuarios", "Permisos", "Modulos", "PermisosSensibles", "Cobertura", "NivelPrivilegio", "EstadoConfiguracion", "DetallePermisos"];
                filas = datos.Select(x => new[]
                {
                    x.RolId.ToString(CultureInfo.InvariantCulture), x.Rol, SiNo(x.Activo && !x.Eliminado),
                    SiNo(x.EsAdministrador), x.UsuariosAsignados.ToString(CultureInfo.InvariantCulture),
                    x.PermisosAsignados.ToString(CultureInfo.InvariantCulture),
                    x.ModulosConAcceso.ToString(CultureInfo.InvariantCulture),
                    x.PermisosSensibles.ToString(CultureInfo.InvariantCulture),
                    x.PorcentajeCobertura.ToString("0.00", CultureInfo.InvariantCulture) + "%",
                    x.NivelPrivilegio, x.EstadoConfiguracion, string.Join(" | ", x.Permisos)
                }).ToList();
                nombre = "roles-permisos";
                break;
            }
            case "auditoria":
            {
                var (desde, hasta) = NormalizarPeriodo(filtro);
                var datos = await _db.RegistrosAuditoria.AsNoTracking()
                    .Where(x => x.Fecha >= desde && x.Fecha <= hasta)
                    .OrderByDescending(x => x.Fecha)
                    .Take(50_000)
                    .ToListAsync(cancellationToken);
                encabezados = ["Id", "Fecha", "Usuario", "Modulo", "Accion", "Entidad", "ReferenciaId", "Descripcion", "Motivo", "Resultado", "CorrelationId", "IpEnmascarada"];
                filas = datos.Select(x => new[]
                {
                    x.Id.ToString(CultureInfo.InvariantCulture),
                    x.Fecha.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    x.NombreUsuario, x.Modulo.ToString(), x.Accion.ToString(), x.Entidad ?? string.Empty,
                    x.ReferenciaId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                    x.Descripcion, x.Motivo ?? string.Empty, x.Resultado, x.CorrelationId ?? string.Empty,
                    EnmascararIp(x.Ip)
                }).ToList();
                nombre = $"auditoria-{desde:yyyyMMdd}-{hasta:yyyyMMdd}";
                break;
            }
            default:
                throw new BusinessRuleException("El tipo de reporte debe ser usuarios, roles o auditoria.");
        }

        return formatoNormalizado switch
        {
            "csv" => CrearCsv(encabezados, filas, nombre),
            "xlsx" => CrearXlsx(encabezados, filas, nombre),
            "pdf" => CrearPdf(encabezados, filas, nombre),
            _ => throw new BusinessRuleException("El formato de exportación debe ser csv, xlsx o pdf.")
        };
    }

    private static (DateTime Desde, DateTime Hasta) NormalizarPeriodo(ReporteAdministrativoFiltroDto filtro)
    {
        var hoy = DateTime.UtcNow.Date;
        var desde = (filtro.Desde ?? hoy.AddDays(-29)).Date;
        var hastaBase = (filtro.Hasta ?? hoy).Date;
        var hasta = hastaBase.AddDays(1).AddTicks(-1);

        if (hasta < desde)
            throw new BusinessRuleException("La fecha hasta no puede ser anterior a la fecha desde.");
        if ((hastaBase - desde).TotalDays > 366)
            throw new BusinessRuleException("El período máximo permitido es de 366 días.");

        return (desde, hasta);
    }

    private static void AgregarAlertas(
        ResumenAdministrativoDto resumen,
        IReadOnlyCollection<UsuarioAccesoReporteDto> usuarios,
        IReadOnlyCollection<RolPermisosReporteDto> roles)
    {
        AgregarAlerta(resumen, "USUARIOS_BLOQUEADOS", "Media", "Usuarios bloqueados que requieren revisión administrativa.", usuarios.Count(x => x.Bloqueado && !x.Eliminado));
        AgregarAlerta(resumen, "USUARIOS_SIN_ROL", "Alta", "Usuarios activos sin un rol dinámico válido.", usuarios.Count(x => x.Activo && !x.Eliminado && !x.RolActivo));
        AgregarAlerta(resumen, "ROLES_SIN_PERMISOS", "Alta", "Roles activos sin grants relacionales asignados.", roles.Count(x => x.Activo && !x.Eliminado && x.PermisosAsignados == 0));
        AgregarAlerta(resumen, "ROLES_SIN_USUARIOS", "Informativa", "Roles activos que no tienen usuarios asignados.", roles.Count(x => x.Activo && !x.Eliminado && x.UsuariosAsignados == 0));
        AgregarAlerta(resumen, "EVENTOS_RECHAZADOS", "Media", "Operaciones rechazadas registradas en el período seleccionado.", resumen.EventosRechazados);
        AgregarAlerta(resumen, "EVENTOS_CON_ERROR", "Alta", "Operaciones con error registradas en el período seleccionado.", resumen.EventosConError);
    }

    private static void AgregarAlerta(ResumenAdministrativoDto resumen, string codigo, string severidad, string mensaje, int cantidad)
    {
        if (cantidad <= 0) return;
        resumen.Alertas.Add(new AlertaAdministrativaDto
        {
            Codigo = codigo,
            Severidad = severidad,
            Mensaje = mensaje,
            Cantidad = cantidad
        });
    }

    private static string EstadoAcceso(bool activo, bool bloqueado, bool eliminado, Domain.Entities.Rol? rol)
    {
        if (eliminado) return "Eliminado";
        if (!activo) return "Inactivo";
        if (bloqueado) return "Bloqueado";
        if (rol is null) return "Sin rol";
        if (rol.Eliminado || !rol.Activo) return "Rol no disponible";
        return "Habilitado";
    }

    private static string EstadoConfiguracion(Domain.Entities.Rol rol, int permisos, int usuarios)
    {
        if (rol.Eliminado) return "Eliminado";
        if (!rol.Activo) return "Inactivo";
        if (permisos == 0) return "Sin permisos";
        if (usuarios == 0) return "Sin usuarios";
        return "Configurado";
    }

    private static string NivelPrivilegio(bool administrador, decimal cobertura, int sensibles)
    {
        if (administrador && cobertura >= 70m) return "Crítico administrado";
        if (cobertura >= 70m || sensibles >= 8) return "Alto";
        if (cobertura >= 35m || sensibles >= 3) return "Medio";
        return "Bajo";
    }

    private static bool EsExito(string? resultado) =>
        string.Equals(resultado, "Exito", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(resultado, "Éxito", StringComparison.OrdinalIgnoreCase);

    private static bool EsRechazado(string? resultado) =>
        string.Equals(resultado, "Rechazado", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(resultado, "Denegado", StringComparison.OrdinalIgnoreCase);

    private static string SiNo(bool valor) => valor ? "Sí" : "No";

    private static string EnmascararIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip) || !IPAddress.TryParse(ip, out var address)) return string.Empty;
        var bytes = address.GetAddressBytes();
        if (bytes.Length == 4) return $"{bytes[0]}.{bytes[1]}.x.x";
        return address.ToString().Split(':').Take(3).Aggregate((a, b) => $"{a}:{b}") + ":…";
    }

    private static ArchivoDescargableDto CrearCsv(string[] encabezados, IEnumerable<string[]> filas, string nombre)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', encabezados.Select(EscaparCsv)));
        foreach (var fila in filas)
            sb.AppendLine(string.Join(',', fila.Select(EscaparCsv)));

        return new ArchivoDescargableDto(
            new UTF8Encoding(true).GetBytes(sb.ToString()),
            "text/csv; charset=utf-8",
            $"{nombre}.csv");
    }

    private static ArchivoDescargableDto CrearXlsx(string[] encabezados, IReadOnlyList<string[]> filas, string nombre)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Reporte");
        for (var columna = 0; columna < encabezados.Length; columna++)
        {
            worksheet.Cell(1, columna + 1).Value = encabezados[columna];
            worksheet.Cell(1, columna + 1).Style.Font.Bold = true;
        }

        for (var fila = 0; fila < filas.Count; fila++)
        {
            for (var columna = 0; columna < encabezados.Length; columna++)
                worksheet.Cell(fila + 2, columna + 1).Value = ValorSeguro(filas[fila].ElementAtOrDefault(columna));
        }

        worksheet.SheetView.FreezeRows(1);
        worksheet.RangeUsed()?.SetAutoFilter();
        worksheet.Columns().AdjustToContents(8, 55);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new ArchivoDescargableDto(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{nombre}.xlsx");
    }

    private static ArchivoDescargableDto CrearPdf(string[] encabezados, IReadOnlyList<string[]> filas, string nombre)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(7));

                page.Header()
                    .PaddingBottom(8)
                    .Text($"Reporte · {nombre.Replace('-', ' ')}")
                    .FontSize(14)
                    .SemiBold();

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        foreach (var _ in encabezados)
                            columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        foreach (var encabezado in encabezados)
                        {
                            header.Cell()
                                .Background(Colors.Grey.Lighten3)
                                .Padding(3)
                                .Text(ValorSeguro(encabezado))
                                .SemiBold();
                        }
                    });

                    foreach (var fila in filas)
                    {
                        for (var columna = 0; columna < encabezados.Length; columna++)
                        {
                            table.Cell()
                                .BorderBottom(0.5f)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Padding(3)
                                .Text(ValorSeguro(fila.ElementAtOrDefault(columna)));
                        }
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Página ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        });

        return new ArchivoDescargableDto(
            documento.GeneratePdf(),
            "application/pdf",
            $"{nombre}.pdf");
    }

    private static string EscaparCsv(string? valor)
    {
        var seguro = ValorSeguro(valor);
        return $"\"{seguro.Replace("\"", "\"\"")}\"";
    }

    private static string ValorSeguro(string? valor)
    {
        var texto = valor ?? string.Empty;
        var recortado = texto.TrimStart();
        return recortado.StartsWith('=') || recortado.StartsWith('+') || recortado.StartsWith('-') || recortado.StartsWith('@')
            ? "'" + texto
            : texto;
    }
}
