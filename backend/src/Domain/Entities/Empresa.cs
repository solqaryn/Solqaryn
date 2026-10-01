using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Raiz de dominio canónica para una empresa/tenant.
///
/// N6.1 introduce solamente la identidad empresarial y sus invariantes mínimas.
/// El aislamiento tenant-aware, la relación física con sucursales, membresías,
/// configuración independiente y storage aislado pertenecen a parents posteriores.
/// </summary>
public sealed class Empresa : AuditableEntity
{
    private Empresa()
    {
    }

    public Empresa(string nombre)
    {
        CambiarNombre(nombre);
    }

    /// <summary>
    /// Nombre identificable de la empresa y fuente autoritativa de identidad comercial.
    /// </summary>
    public string Nombre { get; private set; } = string.Empty;

    /// <summary>
    /// Identidad legal/comercial persistida en la raíz tenant. N6.7 evita duplicar
    /// estos datos en ConfigEmpresa.
    /// </summary>
    public string? Rtn { get; private set; }
    public string? Direccion { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? LogoPublicId { get; private set; }

    public bool Activa { get; private set; } = true;

    /// <summary>
    /// La Empresa usa ciclo de vida activa/inactiva y no soft-delete persistido.
    /// Esta proyección explícita mantiene compatibles los guards de aplicación
    /// sin introducir una columna o migración fuera de N6.4.D.
    /// </summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool Eliminado => false;

    public void CambiarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre de la empresa es obligatorio.", nameof(nombre));
        }

        Nombre = nombre.Trim();
    }

    public void ActualizarIdentidadLegal(string? rtn, string? direccion, string? logoUrl, string? logoPublicId)
    {
        Rtn = NormalizarRtn(rtn);
        Direccion = NormalizarOpcional(direccion);
        LogoUrl = NormalizarOpcional(logoUrl);
        LogoPublicId = NormalizarOpcional(logoPublicId);
    }

    public void Activar() => Activa = true;

    public void Desactivar() => Activa = false;

    private static string? NormalizarRtn(string? rtn)
    {
        if (string.IsNullOrWhiteSpace(rtn))
        {
            return null;
        }

        return new string(rtn.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
    }

    private static string? NormalizarOpcional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
