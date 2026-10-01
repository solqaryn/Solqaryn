using System.Text.Json;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Solqaryn.Application.Services;

public class EmpresaConfiguracionService : IEmpresaConfiguracionService
{
    private static readonly HashSet<string> ExtensionesPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private static readonly HashSet<string> MimePermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    private const long MaxLogoBytes = 5 * 1024 * 1024;

    private readonly IEmpresaConfiguracionRepository _repository;
    private readonly IImageStorageService _imageStorage;
    private readonly IAuditoriaService _auditoria;
    private readonly IEmpresaRepository? _empresaRepository;
    private readonly IUsuarioScopeService? _usuarioScopeService;

    // Constructor histórico conservado para compatibilidad con pruebas y consumidores legacy.
    public EmpresaConfiguracionService(
        IEmpresaConfiguracionRepository repository,
        IImageStorageService imageStorage,
        IAuditoriaService auditoria)
    {
        _repository = repository;
        _imageStorage = imageStorage;
        _auditoria = auditoria;
    }

    // ASP.NET Core selecciona este constructor cuando todas las dependencias tenant-first están registradas.
    public EmpresaConfiguracionService(
        IEmpresaConfiguracionRepository repository,
        IImageStorageService imageStorage,
        IAuditoriaService auditoria,
        IEmpresaRepository empresaRepository,
        IUsuarioScopeService usuarioScopeService)
        : this(repository, imageStorage, auditoria)
    {
        _empresaRepository = empresaRepository;
        _usuarioScopeService = usuarioScopeService;
    }

    public async Task<EmpresaConfiguracionDto> GetActivaAsync()
    {
        var config = await GetActivaEntidadAsync();
        return ToDto(config);
    }

    public async Task<EmpresaConfiguracion> GetActivaEntidadAsync()
    {
        return await GetOrCreateAsync();
    }

    public async Task<EmpresaConfiguracionDto> UpdateAsync(UpdateEmpresaConfiguracionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NombreComercial))
            throw new BusinessRuleException("El nombre comercial es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.NombreVisibleSistema))
            throw new BusinessRuleException("El nombre visible del sistema es obligatorio.");

        var config = await GetOrCreateAsync();
        var anterior = ToDto(config);

        config.NombreComercial = dto.NombreComercial.Trim();
        config.RazonSocial = Limpiar(dto.RazonSocial);
        config.Eslogan = dto.Eslogan?.Trim() ?? string.Empty;
        config.RTN = Limpiar(dto.RTN);
        config.Telefono = Limpiar(dto.Telefono);
        config.Correo = Limpiar(dto.Correo);
        config.Direccion = Limpiar(dto.Direccion);
        config.SitioWeb = Limpiar(dto.SitioWeb);
        config.Facebook = Limpiar(dto.Facebook);
        config.Instagram = Limpiar(dto.Instagram);
        config.WhatsApp = Limpiar(dto.WhatsApp);
        config.NombreVisibleSistema = dto.NombreVisibleSistema.Trim();
        config.DescripcionSistema = string.IsNullOrWhiteSpace(dto.DescripcionSistema)
            ? "Administrativo"
            : dto.DescripcionSistema.Trim();
        config.MensajeLogin = string.IsNullOrWhiteSpace(dto.MensajeLogin)
            ? $"Inicia sesión para administrar {config.NombreVisibleSistema}"
            : dto.MensajeLogin.Trim();
        config.Copyright = string.IsNullOrWhiteSpace(dto.Copyright)
            ? $"© {DateTime.UtcNow.Year} {config.NombreVisibleSistema}. Todos los derechos reservados."
            : dto.Copyright.Trim();
        config.MostrarCopyright = dto.MostrarCopyright;
        config.UsarAnioAutomaticoCopyright = dto.UsarAnioAutomaticoCopyright;
        config.EncabezadoActivo = dto.EncabezadoActivo;
        config.EncabezadoTexto = Limpiar(dto.EncabezadoTexto);
        config.PiePaginaActivo = dto.PiePaginaActivo;
        config.PiePaginaTexto = Limpiar(dto.PiePaginaTexto);
        config.Moneda = string.IsNullOrWhiteSpace(dto.Moneda) ? "HNL" : dto.Moneda.Trim().ToUpperInvariant();
        config.ZonaHoraria = string.IsNullOrWhiteSpace(dto.ZonaHoraria) ? "America/Tegucigalpa" : dto.ZonaHoraria.Trim();
        config.FormatoFecha = string.IsNullOrWhiteSpace(dto.FormatoFecha) ? "dd/MM/yyyy" : dto.FormatoFecha.Trim();
        config.InformacionFiscal = Limpiar(dto.InformacionFiscal);
        config.TextoLegal = Limpiar(dto.TextoLegal);
        config.TextoFactura = Limpiar(dto.TextoFactura);
        config.TextoReportes = Limpiar(dto.TextoReportes);
        config.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(config);
        await _repository.SaveChangesAsync();

        var nueva = ToDto(config);
        await _auditoria.RegistrarAsync(
            ModuloSistema.Configuracion,
            AccionPermiso.Editar,
            "Configuracion empresarial actualizada.",
            config.Id,
            entidad: "EmpresaConfiguracion",
            valoresAnteriores: anterior,
            valoresNuevos: nueva);

        return nueva;
    }

    public async Task<EmpresaConfiguracionDto> UpdateLogoAsync(IFormFile logo)
    {
        ValidarLogo(logo);

        var config = await GetOrCreateAsync();
        var anterior = ToDto(config);
        var publicIdAnterior = config.LogoPublicId;

        var (url, publicId) = await _imageStorage.UploadAsync(logo);
        config.LogoUrl = url;
        config.LogoPublicId = publicId;
        config.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(config);
        await _repository.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(publicIdAnterior))
            await _imageStorage.DeleteAsync(publicIdAnterior);

        var nueva = ToDto(config);
        await _auditoria.RegistrarAsync(
            ModuloSistema.Configuracion,
            AccionPermiso.Editar,
            "Logo empresarial actualizado.",
            config.Id,
            entidad: "EmpresaConfiguracion",
            valoresAnteriores: anterior,
            valoresNuevos: nueva);

        return nueva;
    }

    public async Task<EmpresaConfiguracionDto> RestaurarLogoAsync()
    {
        var config = await GetOrCreateAsync();
        var anterior = ToDto(config);
        var publicIdAnterior = config.LogoPublicId;

        config.LogoUrl = null;
        config.LogoPublicId = null;
        config.FechaActualizacion = DateTime.UtcNow;
        _repository.Update(config);
        await _repository.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(publicIdAnterior))
            await _imageStorage.DeleteAsync(publicIdAnterior);

        var nueva = ToDto(config);
        await _auditoria.RegistrarAsync(
            ModuloSistema.Configuracion,
            AccionPermiso.Editar,
            "Logo empresarial restaurado al valor predeterminado.",
            config.Id,
            entidad: "EmpresaConfiguracion",
            valoresAnteriores: anterior,
            valoresNuevos: nueva);

        return nueva;
    }

    public async Task<ConfigEmpresaTenantDto> GetTenantAsync(int empresaId, CancellationToken cancellationToken = default)
    {
        var (empresaRepository, scopeService) = TenantDependencies();
        await EnsureTenantAccessAsync(scopeService, empresaId, cancellationToken);
        return await BuildTenantDtoAsync(empresaRepository, empresaId, cancellationToken);
    }

    public async Task<ConfigEmpresaTenantDto> UpdateTenantAsync(
        int empresaId,
        UpdateConfigEmpresaTenantDto dto,
        CancellationToken cancellationToken = default)
    {
        var (empresaRepository, scopeService) = TenantDependencies();
        await EnsureTenantAccessAsync(scopeService, empresaId, cancellationToken);
        ValidarTenant(dto);

        var empresa = await GetEmpresaAsync(empresaRepository, empresaId, cancellationToken);
        var config = await GetOrCreateTenantConfigAsync(empresaId, cancellationToken);
        if (dto.Version != config.Version)
            throw new ConflictException("La configuración cambió desde la última lectura. Recarga e intenta nuevamente.");

        var anterior = await BuildTenantDtoAsync(empresaRepository, empresaId, cancellationToken, empresa, config);

        empresa.CambiarNombre(dto.Nombre);
        empresa.ActualizarIdentidadLegal(dto.Rtn, dto.Direccion, empresa.LogoUrl, empresa.LogoPublicId);
        config.Moneda = dto.Moneda.Trim().ToUpperInvariant();
        config.ZonaHoraria = dto.ZonaHoraria.Trim();
        config.ImpuestosJson = dto.ImpuestosJson.Trim();
        config.EmisionJson = dto.EmisionJson.Trim();
        config.CorreoRemitente = Limpiar(dto.CorreoRemitente);
        config.CorreoNombreRemitente = Limpiar(dto.CorreoNombreRemitente);
        config.Version++;

        empresaRepository.Update(empresa);
        _repository.UpdateTenant(config);
        if (!await _repository.SaveChangesAsync())
            throw new BusinessRuleException("No se pudo guardar la configuración tenant.");

        var nueva = await BuildTenantDtoAsync(empresaRepository, empresaId, cancellationToken, empresa, config);
        await _auditoria.RegistrarAsync(
            ModuloSistema.Configuracion,
            AccionPermiso.Editar,
            $"Configuración tenant de empresa {empresaId} actualizada.",
            empresaId,
            entidad: "ConfigEmpresa",
            valoresAnteriores: anterior,
            valoresNuevos: nueva);

        return nueva;
    }

    public async Task<ConfigEmpresaTenantDto> UpdateTenantLogoAsync(
        int empresaId,
        IFormFile logo,
        CancellationToken cancellationToken = default)
    {
        var (empresaRepository, scopeService) = TenantDependencies();
        var tenantScope = await EnsureTenantAccessAsync(scopeService, empresaId, cancellationToken);
        var storageTenant = StorageTenantContext.Desde(tenantScope);
        ValidarLogo(logo);

        var empresa = await GetEmpresaAsync(empresaRepository, empresaId, cancellationToken);
        var publicIdAnterior = empresa.LogoPublicId;
        var (url, publicId) = await _imageStorage.UploadAsync(storageTenant, logo, cancellationToken);

        empresa.ActualizarIdentidadLegal(empresa.Rtn, empresa.Direccion, url, publicId);
        empresaRepository.Update(empresa);
        if (!await empresaRepository.SaveChangesAsync(cancellationToken))
            throw new BusinessRuleException("No se pudo guardar el logo tenant.");

        if (!string.IsNullOrWhiteSpace(publicIdAnterior))
            await _imageStorage.DeleteAsync(storageTenant, publicIdAnterior, cancellationToken);

        await _auditoria.RegistrarAsync(
            ModuloSistema.Configuracion,
            AccionPermiso.Editar,
            $"Logo tenant de empresa {empresaId} actualizado.",
            empresaId,
            entidad: "Empresa");

        return await BuildTenantDtoAsync(empresaRepository, empresaId, cancellationToken, empresa);
    }

    public async Task<ConfigEmpresaTenantDto> RestaurarTenantLogoAsync(
        int empresaId,
        CancellationToken cancellationToken = default)
    {
        var (empresaRepository, scopeService) = TenantDependencies();
        var tenantScope = await EnsureTenantAccessAsync(scopeService, empresaId, cancellationToken);
        var storageTenant = StorageTenantContext.Desde(tenantScope);

        var empresa = await GetEmpresaAsync(empresaRepository, empresaId, cancellationToken);
        var publicIdAnterior = empresa.LogoPublicId;
        empresa.ActualizarIdentidadLegal(empresa.Rtn, empresa.Direccion, null, null);
        empresaRepository.Update(empresa);
        if (!await empresaRepository.SaveChangesAsync(cancellationToken))
            throw new BusinessRuleException("No se pudo restaurar el logo tenant.");

        if (!string.IsNullOrWhiteSpace(publicIdAnterior))
            await _imageStorage.DeleteAsync(storageTenant, publicIdAnterior, cancellationToken);

        await _auditoria.RegistrarAsync(
            ModuloSistema.Configuracion,
            AccionPermiso.Editar,
            $"Logo tenant de empresa {empresaId} restaurado.",
            empresaId,
            entidad: "Empresa");

        return await BuildTenantDtoAsync(empresaRepository, empresaId, cancellationToken, empresa);
    }

    public async Task<ConfigEmpresaTenantDto> UpsertPlantillaTenantAsync(
        int empresaId,
        string tipoPlantilla,
        UpdatePlantillaCorreoEmpresaDto dto,
        CancellationToken cancellationToken = default)
    {
        var (empresaRepository, scopeService) = TenantDependencies();
        await EnsureTenantAccessAsync(scopeService, empresaId, cancellationToken);
        await GetEmpresaAsync(empresaRepository, empresaId, cancellationToken);

        var tipo = ValidarPlantilla(tipoPlantilla, dto);
        var config = await GetOrCreateTenantConfigAsync(empresaId, cancellationToken);
        if (dto.Version != config.Version)
            throw new ConflictException("La configuración cambió desde la última lectura. Recarga e intenta nuevamente.");

        var plantilla = await _repository.GetPlantillaAsync(empresaId, tipo, cancellationToken);
        if (plantilla is null)
        {
            plantilla = new PlantillaCorreoEmpresa(empresaId, tipo, dto.Asunto, dto.Cuerpo)
            {
                Activa = dto.Activa
            };
            await _repository.AddPlantillaAsync(plantilla, cancellationToken);
        }
        else
        {
            plantilla.Asunto = dto.Asunto.Trim();
            plantilla.Cuerpo = dto.Cuerpo.Trim();
            plantilla.Activa = dto.Activa;
            _repository.UpdatePlantilla(plantilla);
        }

        config.Version++;
        _repository.UpdateTenant(config);
        if (!await _repository.SaveChangesAsync())
            throw new BusinessRuleException("No se pudo guardar la plantilla tenant.");

        await _auditoria.RegistrarAsync(
            ModuloSistema.Configuracion,
            AccionPermiso.Editar,
            $"Plantilla de correo '{tipo}' de empresa {empresaId} actualizada.",
            empresaId,
            entidad: "PlantillaCorreoEmpresa");

        return await BuildTenantDtoAsync(empresaRepository, empresaId, cancellationToken, config: config);
    }

    public async Task<ConfigEmpresaTenantDto> DesactivarPlantillaTenantAsync(
        int empresaId,
        string tipoPlantilla,
        long version,
        CancellationToken cancellationToken = default)
    {
        var (empresaRepository, scopeService) = TenantDependencies();
        await EnsureTenantAccessAsync(scopeService, empresaId, cancellationToken);
        await GetEmpresaAsync(empresaRepository, empresaId, cancellationToken);

        var tipo = NormalizarRequerido(tipoPlantilla, "El tipo de plantilla es obligatorio.", 80);
        var config = await GetOrCreateTenantConfigAsync(empresaId, cancellationToken);
        if (version != config.Version)
            throw new ConflictException("La configuración cambió desde la última lectura. Recarga e intenta nuevamente.");

        var plantilla = await _repository.GetPlantillaAsync(empresaId, tipo, cancellationToken)
            ?? throw new ResourceNotFoundException("La plantilla de correo no existe para esta empresa.");

        plantilla.Activa = false;
        _repository.UpdatePlantilla(plantilla);
        config.Version++;
        _repository.UpdateTenant(config);
        if (!await _repository.SaveChangesAsync())
            throw new BusinessRuleException("No se pudo desactivar la plantilla tenant.");

        await _auditoria.RegistrarAsync(
            ModuloSistema.Configuracion,
            AccionPermiso.Editar,
            $"Plantilla de correo '{tipo}' de empresa {empresaId} desactivada.",
            empresaId,
            entidad: "PlantillaCorreoEmpresa");

        return await BuildTenantDtoAsync(empresaRepository, empresaId, cancellationToken, config: config);
    }

    private async Task<EmpresaConfiguracion> GetOrCreateAsync()
    {
        var config = await _repository.GetActivaAsync();
        if (config is not null) return config;

        config = new EmpresaConfiguracion { Activa = true };
        await _repository.AddAsync(config);
        await _repository.SaveChangesAsync();
        return config;
    }

    private async Task<ConfigEmpresa> GetOrCreateTenantConfigAsync(int empresaId, CancellationToken cancellationToken)
    {
        var config = await _repository.GetTenantAsync(empresaId, cancellationToken);
        if (config is not null) return config;

        config = new ConfigEmpresa(empresaId);
        await _repository.AddTenantAsync(config, cancellationToken);

        try
        {
            if (!await _repository.SaveChangesAsync())
                throw new BusinessRuleException("No se pudo inicializar la configuración tenant.");

            return config;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Dos lecturas concurrentes pueden observar la ausencia y competir por crear
            // la única ConfigEmpresa del tenant. El índice único decide el ganador.
            // Descartamos el agregado perdedor antes de releer para no dejar el DbContext
            // contaminado y provocar otro INSERT duplicado en una mutación posterior.
            _repository.DetachTenant(config);
            var concurrentWinner = await _repository.GetTenantAsync(empresaId, cancellationToken);
            if (concurrentWinner is not null)
                return concurrentWinner;

            throw;
        }
    }

    private async Task<ConfigEmpresaTenantDto> BuildTenantDtoAsync(
        IEmpresaRepository empresaRepository,
        int empresaId,
        CancellationToken cancellationToken,
        Empresa? empresa = null,
        ConfigEmpresa? config = null)
    {
        empresa ??= await GetEmpresaAsync(empresaRepository, empresaId, cancellationToken);
        config ??= await GetOrCreateTenantConfigAsync(empresaId, cancellationToken);
        var plantillas = await _repository.ListPlantillasAsync(empresaId, cancellationToken);

        return new ConfigEmpresaTenantDto
        {
            EmpresaId = empresaId,
            Nombre = empresa.Nombre,
            Rtn = empresa.Rtn,
            Direccion = empresa.Direccion,
            LogoUrl = empresa.LogoUrl,
            Moneda = config.Moneda,
            ZonaHoraria = config.ZonaHoraria,
            ImpuestosJson = config.ImpuestosJson,
            EmisionJson = config.EmisionJson,
            CorreoRemitente = config.CorreoRemitente,
            CorreoNombreRemitente = config.CorreoNombreRemitente,
            CorreoConfigurado = config.CorreoConfigurado,
            Version = config.Version,
            PlantillasCorreo = plantillas.Select(ToTenantPlantillaDto).ToArray()
        };
    }

    private static PlantillaCorreoEmpresaDto ToTenantPlantillaDto(PlantillaCorreoEmpresa plantilla) => new()
    {
        TipoPlantilla = plantilla.TipoPlantilla,
        Asunto = plantilla.Asunto,
        Cuerpo = plantilla.Cuerpo,
        Activa = plantilla.Activa
    };

    private static async Task<Empresa> GetEmpresaAsync(
        IEmpresaRepository empresaRepository,
        int empresaId,
        CancellationToken cancellationToken) =>
        await empresaRepository.GetByIdAsync(empresaId, cancellationToken)
        ?? throw new ResourceNotFoundException("La empresa solicitada no existe.");

    private static async Task<UsuarioTenantScopeActual> EnsureTenantAccessAsync(
        IUsuarioScopeService scopeService,
        int empresaId,
        CancellationToken cancellationToken)
    {
        if (empresaId <= 0)
            throw new BusinessRuleException("La empresa es obligatoria.");

        var scope = await scopeService.ObtenerActualAsync(empresaId, cancellationToken);
        if (scope is null)
            throw new ForbiddenAccessException("No existe una membresía activa para la empresa solicitada.");

        return scope;
    }

    private (IEmpresaRepository EmpresaRepository, IUsuarioScopeService ScopeService) TenantDependencies()
    {
        if (_empresaRepository is null || _usuarioScopeService is null)
            throw new InvalidOperationException("Las dependencias tenant-first de configuración no están disponibles.");

        return (_empresaRepository, _usuarioScopeService);
    }

    private static void ValidarTenant(UpdateConfigEmpresaTenantDto dto)
    {
        NormalizarRequerido(dto.Nombre, "El nombre de la empresa es obligatorio.", 200);
        var moneda = NormalizarRequerido(dto.Moneda, "La moneda es obligatoria.", 3);
        if (moneda.Length != 3 || moneda.Any(c => !char.IsLetter(c)))
            throw new BusinessRuleException("La moneda debe usar un código alfabético de 3 caracteres.");

        NormalizarRequerido(dto.ZonaHoraria, "La zona horaria es obligatoria.", 100);
        ValidarJsonObject(dto.ImpuestosJson, "La configuración de impuestos debe ser un objeto JSON válido.");
        ValidarJsonObject(dto.EmisionJson, "La configuración de emisión debe ser un objeto JSON válido.");

        if (Limpiar(dto.CorreoRemitente)?.Length > 254)
            throw new BusinessRuleException("El correo remitente no puede exceder 254 caracteres.");
        if (Limpiar(dto.CorreoNombreRemitente)?.Length > 200)
            throw new BusinessRuleException("El nombre del remitente no puede exceder 200 caracteres.");
        if (dto.Version <= 0)
            throw new BusinessRuleException("La versión de configuración es obligatoria.");
    }

    private static string ValidarPlantilla(string tipoPlantilla, UpdatePlantillaCorreoEmpresaDto dto)
    {
        var tipo = NormalizarRequerido(tipoPlantilla, "El tipo de plantilla es obligatorio.", 80);
        NormalizarRequerido(dto.Asunto, "El asunto de la plantilla es obligatorio.", 250);
        NormalizarRequerido(dto.Cuerpo, "El cuerpo de la plantilla es obligatorio.", int.MaxValue);
        if (dto.Version <= 0)
            throw new BusinessRuleException("La versión de configuración es obligatoria.");
        return tipo;
    }

    private static string NormalizarRequerido(string? value, string error, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new BusinessRuleException(error);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new BusinessRuleException($"{error} Máximo permitido: {maxLength} caracteres.");
        return normalized;
    }

    private static void ValidarJsonObject(string? json, string error)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new BusinessRuleException(error);

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new BusinessRuleException(error);
        }
        catch (JsonException)
        {
            throw new BusinessRuleException(error);
        }
    }

    private static void ValidarLogo(IFormFile logo)
    {
        if (logo.Length <= 0)
            throw new BusinessRuleException("El archivo del logo está vacío.");
        if (logo.Length > MaxLogoBytes)
            throw new BusinessRuleException("El logo no puede exceder 5 MB.");

        var extension = Path.GetExtension(logo.FileName);
        if (!ExtensionesPermitidas.Contains(extension))
            throw new BusinessRuleException("Formato de logo no permitido. Usa JPG, PNG o WEBP.");
        if (!MimePermitidos.Contains(logo.ContentType))
            throw new BusinessRuleException("Tipo MIME de logo no permitido.");
    }

    private static string? Limpiar(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static EmpresaConfiguracionDto ToDto(EmpresaConfiguracion c) => new()
    {
        Id = c.Id,
        NombreComercial = c.NombreComercial,
        RazonSocial = c.RazonSocial,
        Eslogan = c.Eslogan,
        RTN = c.RTN,
        Telefono = c.Telefono,
        Correo = c.Correo,
        Direccion = c.Direccion,
        SitioWeb = c.SitioWeb,
        Facebook = c.Facebook,
        Instagram = c.Instagram,
        WhatsApp = c.WhatsApp,
        LogoUrl = c.LogoUrl,
        NombreVisibleSistema = c.NombreVisibleSistema,
        DescripcionSistema = c.DescripcionSistema,
        MensajeLogin = c.MensajeLogin,
        Copyright = c.UsarAnioAutomaticoCopyright
            ? c.Copyright.Replace("2026", DateTime.UtcNow.Year.ToString())
            : c.Copyright,
        MostrarCopyright = c.MostrarCopyright,
        UsarAnioAutomaticoCopyright = c.UsarAnioAutomaticoCopyright,
        EncabezadoActivo = c.EncabezadoActivo,
        EncabezadoTexto = c.EncabezadoTexto,
        PiePaginaActivo = c.PiePaginaActivo,
        PiePaginaTexto = c.PiePaginaTexto,
        Moneda = c.Moneda,
        ZonaHoraria = c.ZonaHoraria,
        FormatoFecha = c.FormatoFecha,
        InformacionFiscal = c.InformacionFiscal,
        TextoLegal = c.TextoLegal,
        TextoFactura = c.TextoFactura,
        TextoReportes = c.TextoReportes
    };
}
