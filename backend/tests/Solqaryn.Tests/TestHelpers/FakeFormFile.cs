using Microsoft.AspNetCore.Http;

namespace Solqaryn.Tests.TestHelpers;

/// Implementación mínima de IFormFile para pruebas, sin depender del runtime completo de ASP.NET Core.
public class FakeFormFile : IFormFile
{
    private readonly Stream _stream;

    public FakeFormFile(string fileName, string contentType, long length)
    {
        FileName = fileName;
        ContentType = contentType;
        Length = length;
        _stream = new MemoryStream(new byte[Math.Max(length, 1)]);
    }

    public string ContentType { get; }
    public string ContentDisposition => string.Empty;
    public IHeaderDictionary Headers => throw new NotImplementedException("No usado en las pruebas actuales.");
    public long Length { get; }
    public string Name => "imagen";
    public string FileName { get; }

    public void CopyTo(Stream target) => _stream.CopyTo(target);
    public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default) => _stream.CopyToAsync(target, cancellationToken);
    public Stream OpenReadStream() => _stream;
}
