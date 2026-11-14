using Solqaryn.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace Solqaryn.Application.Interfaces;

public interface ICompraDocumentoService
{
    Task<List<CompraDocumentoDto>> GetByCompraAsync(int compraId);
    Task<CompraDocumentoDto> UploadAsync(int compraId, IFormFile archivo);
    Task<(Stream Contenido, string ContentType, string NombreArchivo)?> DownloadAsync(int compraId, int documentoId);
    Task<bool> DeleteAsync(int compraId, int documentoId);
}
