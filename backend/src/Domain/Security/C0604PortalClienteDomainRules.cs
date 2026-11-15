using Solqaryn.Domain.Entities;

namespace Solqaryn.Domain.Security;

/// <summary>
/// C06-04.M03.01: precondiciones puras para cuentas CUSTOMER de tienda.
/// Los ID del actor y tenant deben venir de autenticacion y recursos persistidos
/// revalidados en Application/API. Ningun resultado concede un JWT, rol o grant.
/// </summary>
public static class C0604PortalClienteDomainRules
{
    public static void ExigirSesionYPropiedad(
        int empresaVerificadaServidor,
        int cuentaVerificadaServidor,
        Cliente clientePersistido,
        TiendaCuentaCliente cuentaPersistida,
        TiendaSesionCliente sesionPersistida,
        DateTime ahoraUtc)
    {
        ArgumentNullException.ThrowIfNull(clientePersistido);
        ArgumentNullException.ThrowIfNull(cuentaPersistida);
        ArgumentNullException.ThrowIfNull(sesionPersistida);

        if (empresaVerificadaServidor <= 0 || cuentaVerificadaServidor <= 0 ||
            clientePersistido.Id <= 0 || cuentaPersistida.Id <= 0 ||
            clientePersistido.EmpresaId is null ||
            clientePersistido.EmpresaId != empresaVerificadaServidor ||
            cuentaPersistida.Id != cuentaVerificadaServidor ||
            cuentaPersistida.ClienteId != clientePersistido.Id)
            throw new InvalidOperationException("El cliente o la cuenta no pertenece al contexto verificado.");

        if (!clientePersistido.Activo || !cuentaPersistida.Activa ||
            sesionPersistida.CuentaClienteId != cuentaPersistida.Id ||
            string.IsNullOrWhiteSpace(sesionPersistida.TokenHash) ||
            sesionPersistida.RevocadaUtc is not null ||
            sesionPersistida.ExpiraUtc <= ahoraUtc)
            throw new InvalidOperationException("La cuenta o sesion privada no esta activa.");

        // Guard puro: el adaptador debe validar token hash, autenticacion,
        // pertenencia vigente y permisos en cada request, antes de usar datos.
    }

    /// <summary>
    /// Previene BOLA/IDOR por propiedad de empresa y cliente. La pertenencia
    /// del actor debe estar resuelta antes de invocar este metodo.
    /// </summary>
    public static void ExigirRecursoPrivado(
        int empresaVerificadaServidor,
        int clienteVerificadoServidor,
        int empresaPropietariaRecurso,
        int clientePropietarioRecurso)
    {
        if (empresaVerificadaServidor <= 0 || clienteVerificadoServidor <= 0 ||
            empresaPropietariaRecurso <= 0 || clientePropietarioRecurso <= 0 ||
            empresaVerificadaServidor != empresaPropietariaRecurso ||
            clienteVerificadoServidor != clientePropietarioRecurso)
            throw new InvalidOperationException("El recurso no pertenece al cliente y tenant verificados.");
    }

    /// <summary>
    /// Precondicion de control optimista; no sustituye el UPDATE CAS
    /// atomico ni vuelve a verificar permisos por si sola.
    /// </summary>
    public static void ExigirVersionEsperada(long versionEsperada, long versionPersistida)
    {
        if (versionEsperada <= 0 || versionPersistida != versionEsperada)
            throw new InvalidOperationException("Version de recurso obsoleta o invalida.");
    }
}
