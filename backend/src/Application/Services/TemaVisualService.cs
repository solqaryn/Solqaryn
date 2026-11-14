using System.Text.RegularExpressions;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public class TemaVisualService : ITemaVisualService
{
    private static readonly Regex HexColorRegex = new(@"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", RegexOptions.Compiled);

    private readonly ITemaVisualRepository _repository;
    private readonly IAuditoriaService _auditoria;
    private readonly ICurrentUserService _currentUser;

    public TemaVisualService(ITemaVisualRepository repository, IAuditoriaService auditoria, ICurrentUserService currentUser)
    {
        _repository = repository;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    public async Task<TemaVisualDto> GetAsync()
    {
        var tema = await ObtenerOCrearAsync();
        return ToDto(tema);
    }

    private async Task<TemaVisual> ObtenerOCrearAsync()
    {
        var tema = await _repository.GetAsync();
        if (tema is null)
        {
            tema = new TemaVisual();
            await _repository.AddAsync(tema);
            await _repository.SaveChangesAsync();
        }
        return tema;
    }

    public async Task<TemaVisualDto> UpdateAsync(ActualizarTemaVisualDto dto)
    {
        ValidarColores(dto);

        var tema = await ObtenerOCrearAsync();

        tema.ColorPrimario = dto.ColorPrimario;
        tema.ColorSecundario = dto.ColorSecundario;
        tema.ColorAcento = dto.ColorAcento;
        tema.FondoPrincipal = dto.FondoPrincipal;
        tema.FondoTarjetas = dto.FondoTarjetas;
        tema.MenuLateral = dto.MenuLateral;
        tema.BarraSuperior = dto.BarraSuperior;
        tema.Encabezados = dto.Encabezados;
        tema.BotonesPrincipales = dto.BotonesPrincipales;
        tema.TextoPrincipal = dto.TextoPrincipal;
        tema.TextoSecundario = dto.TextoSecundario;
        tema.ColorExito = dto.ColorExito;
        tema.ColorAdvertencia = dto.ColorAdvertencia;
        tema.ColorError = dto.ColorError;
        tema.ColorInformacion = dto.ColorInformacion;
        tema.FechaActualizacion = DateTime.UtcNow;
        tema.ActualizadoPorUsuarioId = _currentUser.UsuarioId;

        _repository.Update(tema);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Configuracion, AccionPermiso.Editar,
            "Actualizó los colores del tema visual.", tema.Id, entidad: "TemaVisual",
            valoresNuevos: dto);

        return ToDto(tema);
    }

    public async Task<TemaVisualDto> RestaurarPredeterminadoAsync()
    {
        var tema = await ObtenerOCrearAsync();
        var predeterminado = new TemaVisual(); // valores por defecto de la clase

        tema.ColorPrimario = predeterminado.ColorPrimario;
        tema.ColorSecundario = predeterminado.ColorSecundario;
        tema.ColorAcento = predeterminado.ColorAcento;
        tema.FondoPrincipal = predeterminado.FondoPrincipal;
        tema.FondoTarjetas = predeterminado.FondoTarjetas;
        tema.MenuLateral = predeterminado.MenuLateral;
        tema.BarraSuperior = predeterminado.BarraSuperior;
        tema.Encabezados = predeterminado.Encabezados;
        tema.BotonesPrincipales = predeterminado.BotonesPrincipales;
        tema.TextoPrincipal = predeterminado.TextoPrincipal;
        tema.TextoSecundario = predeterminado.TextoSecundario;
        tema.ColorExito = predeterminado.ColorExito;
        tema.ColorAdvertencia = predeterminado.ColorAdvertencia;
        tema.ColorError = predeterminado.ColorError;
        tema.ColorInformacion = predeterminado.ColorInformacion;
        tema.FechaActualizacion = DateTime.UtcNow;
        tema.ActualizadoPorUsuarioId = _currentUser.UsuarioId;

        _repository.Update(tema);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Configuracion, AccionPermiso.Editar,
            "Restauró el tema visual a los valores predeterminados.", tema.Id, entidad: "TemaVisual");

        return ToDto(tema);
    }

    private static void ValidarColores(TemaVisualDto dto)
    {
        var colores = new (string Nombre, string Valor)[]
        {
            (nameof(dto.ColorPrimario), dto.ColorPrimario),
            (nameof(dto.ColorSecundario), dto.ColorSecundario),
            (nameof(dto.ColorAcento), dto.ColorAcento),
            (nameof(dto.FondoPrincipal), dto.FondoPrincipal),
            (nameof(dto.FondoTarjetas), dto.FondoTarjetas),
            (nameof(dto.MenuLateral), dto.MenuLateral),
            (nameof(dto.BarraSuperior), dto.BarraSuperior),
            (nameof(dto.Encabezados), dto.Encabezados),
            (nameof(dto.BotonesPrincipales), dto.BotonesPrincipales),
            (nameof(dto.TextoPrincipal), dto.TextoPrincipal),
            (nameof(dto.TextoSecundario), dto.TextoSecundario),
            (nameof(dto.ColorExito), dto.ColorExito),
            (nameof(dto.ColorAdvertencia), dto.ColorAdvertencia),
            (nameof(dto.ColorError), dto.ColorError),
            (nameof(dto.ColorInformacion), dto.ColorInformacion),
        };

        foreach (var (nombre, valor) in colores)
        {
            if (string.IsNullOrWhiteSpace(valor) || !HexColorRegex.IsMatch(valor))
                throw new BusinessRuleException($"El color '{nombre}' no tiene un formato hexadecimal válido (ej. #4f46e5).");
        }
    }

    private static TemaVisualDto ToDto(TemaVisual t) => new()
    {
        ColorPrimario = t.ColorPrimario,
        ColorSecundario = t.ColorSecundario,
        ColorAcento = t.ColorAcento,
        FondoPrincipal = t.FondoPrincipal,
        FondoTarjetas = t.FondoTarjetas,
        MenuLateral = t.MenuLateral,
        BarraSuperior = t.BarraSuperior,
        Encabezados = t.Encabezados,
        BotonesPrincipales = t.BotonesPrincipales,
        TextoPrincipal = t.TextoPrincipal,
        TextoSecundario = t.TextoSecundario,
        ColorExito = t.ColorExito,
        ColorAdvertencia = t.ColorAdvertencia,
        ColorError = t.ColorError,
        ColorInformacion = t.ColorInformacion,
        FechaActualizacion = t.FechaActualizacion
    };
}
