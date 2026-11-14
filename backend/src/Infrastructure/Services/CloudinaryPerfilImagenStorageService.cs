using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Solqaryn.Infrastructure.Services;

public class CloudinaryPerfilImagenStorageService : IPerfilImagenStorageService
{
    private const string BaseFolder = "solqaryn/perfiles";

    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryPerfilImagenStorageService> _logger;
    private readonly string _folder;
    private readonly string? _environmentPrefix;

    public CloudinaryPerfilImagenStorageService(
        IConfiguration configuration,
        ILogger<CloudinaryPerfilImagenStorageService> logger)
    {
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        if (string.IsNullOrWhiteSpace(cloudName) ||
            string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(apiSecret) ||
            cloudName == "CHANGE_ME" || apiKey == "CHANGE_ME" || apiSecret == "CHANGE_ME")
        {
            throw new BusinessRuleException("Cloudinary no está configurado para almacenar fotografías de perfil.");
        }

        _cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret));
        _cloudinary.Api.Secure = true;
        _logger = logger;
        _folder = CloudinaryFolderResolver.Resolve(configuration, BaseFolder);
        _environmentPrefix = CloudinaryFolderResolver.GetEnvironmentPrefix(configuration);
    }

    public async Task<(string Url, string PublicId)> UploadAsync(IFormFile foto)
    {
        try
        {
            using var segura = await ImagenUploadSecurity.ProcesarAsync(foto);
            var parametros = new ImageUploadParams
            {
                File = new FileDescription(segura.NombreArchivo, segura.Contenido),
                Folder = _folder,
                UseFilename = false,
                UniqueFilename = true,
                Overwrite = false,
                Transformation = new Transformation()
                    .Width(512)
                    .Height(512)
                    .Crop("fill")
                    .Gravity("auto")
                    .Quality("auto")
            };

            var resultado = await _cloudinary.UploadAsync(parametros);
            if (resultado.Error is not null || resultado.SecureUrl is null || string.IsNullOrWhiteSpace(resultado.PublicId))
            {
                _logger.LogWarning("Cloudinary rechazó una fotografía de perfil sanitizada.");
                throw new BusinessRuleException("No fue posible guardar la fotografía de perfil.");
            }

            return (resultado.SecureUrl.ToString(), resultado.PublicId);
        }
        catch (BusinessRuleException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al subir una fotografía de perfil sanitizada a Cloudinary.");
            throw new BusinessRuleException("No fue posible guardar la fotografía de perfil. Intenta nuevamente.");
        }
    }

    public async Task DeleteAsync(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId)) return;

        if (!CloudinaryFolderResolver.CanDelete(_environmentPrefix, publicId))
        {
            _logger.LogWarning(
                "El entorno con prefijo {EnvironmentPrefix} bloqueó la eliminación del activo externo {PublicId}.",
                _environmentPrefix,
                publicId);
            return;
        }

        try
        {
            var resultado = await _cloudinary.DestroyAsync(new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Image,
                Invalidate = true
            });

            if (!string.Equals(resultado.Result, "ok", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(resultado.Result, "not found", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Cloudinary no confirmó la eliminación de {PublicId}. Resultado={Resultado}", publicId, resultado.Result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No fue posible eliminar la foto de perfil {PublicId} de Cloudinary.", publicId);
        }
    }
}
