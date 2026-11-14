namespace Solqaryn.Application.Interfaces;

/// <summary>
/// P1-004.A: autorización de una capacidad de solo lectura delegada.
/// Nunca concede autoridad a partir de claims JWT ni membresías empresariales.
/// </summary>
public interface IPlataformaDelegacionService
{
    Task<UsuarioPlataformaScopeActual?> ObtenerDelegadoActualAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Catálogo fijo del único permiso delegado permitido por esta microtarea.
/// No hay endpoint para escoger permisos o elevar a superadministrador.
/// </summary>
public static class PoliticaDelegacionPlataforma
{
    public const string RolNombreNormalizado = "DELEGADO_PLATAFORMA_CONSULTA";
    public const string CodigoPermiso = "PLATAFORMA.DELEGACION.EMPRESAS_VER";
}
