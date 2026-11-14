using Microsoft.AspNetCore.Http;

namespace Solqaryn.Application.Interfaces;

/// Almacenamiento dedicado para fotografías de perfil. Se separa de las
/// imágenes de productos para poder aplicar carpeta, límites y políticas
/// específicas sin mezclar recursos comerciales con identidades de usuario.
public interface IPerfilImagenStorageService
{
    Task<(string Url, string PublicId)> UploadAsync(IFormFile foto);
    Task DeleteAsync(string publicId);
}
