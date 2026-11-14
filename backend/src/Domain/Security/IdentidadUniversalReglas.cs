using Solqaryn.Domain.Entities;

namespace Solqaryn.Domain.Security;

/// <summary>
/// Invariantes de dominio de identidad universal. No reemplazan la autenticacion
/// JWT, los permisos por accion ni los filtros de persistencia en Application/API.
/// </summary>
public static class IdentidadUniversalReglas
{
    public static ContextoTenantActual CrearContextoVerificado(
        Usuario usuario, Empresa empresa, UsuarioEmpresa membresia, Rol rol)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        ArgumentNullException.ThrowIfNull(empresa);
        ArgumentNullException.ThrowIfNull(membresia);
        ArgumentNullException.ThrowIfNull(rol);

        // La identidad global y el RolId legacy de Usuario NO conceden acceso.
        if (usuario.Id <= 0 || !usuario.Activo || usuario.Bloqueado || usuario.Eliminado)
            throw new InvalidOperationException("El usuario no tiene una identidad activa.");

        if (empresa.Id <= 0 || !empresa.Activa)
            throw new InvalidOperationException("La empresa no tiene una identidad activa.");

        if (!membresia.Activa || membresia.UsuarioId != usuario.Id ||
            membresia.EmpresaId != empresa.Id)
            throw new InvalidOperationException("Falta membresia activa en la empresa solicitada.");

        if (rol.Id <= 0 || rol.Id != membresia.RolId ||
            !rol.EsAsignableAEmpresa(empresa.Id))
            throw new InvalidOperationException("El rol no autoriza esta empresa.");

        return ContextoTenantActual.DesdeMembresia(membresia, usuario.Id, empresa.Id);
    }

    public static void ExigirSucursal(ContextoTenantActual contexto, Sucursal sucursal)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ArgumentNullException.ThrowIfNull(sucursal);

        if (sucursal.Id <= 0 || !sucursal.Activa || sucursal.Eliminado)
            throw new InvalidOperationException("La sucursal no esta habilitada.");

        // El owner no se toma de una URL, claim o header; pertenece al recurso.
        contexto.ExigirEmpresa(sucursal.ObtenerEmpresaIdTenant());
    }
}
