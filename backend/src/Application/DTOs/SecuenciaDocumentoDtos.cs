namespace Solqaryn.Application.DTOs;

public sealed record ReservarSecuenciaDocumentoRequest(
    int EmpresaId,
    int? SucursalId,
    string TipoDocumento);

public sealed record SecuenciaDocumentoConsultaDto(
    int EmpresaId,
    int? SucursalId,
    string TipoDocumento,
    long UltimoValor,
    string Prefijo,
    int LongitudNumero,
    bool Activa,
    string? UltimoNumeroFormateado);

public sealed record SecuenciaDocumentoSiguienteDto(
    int EmpresaId,
    int? SucursalId,
    string TipoDocumento,
    long Valor,
    string Numero);
