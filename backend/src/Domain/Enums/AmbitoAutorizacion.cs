namespace Solqaryn.Domain.Enums;

/// <summary>
/// Separa explícitamente la autoridad tenant de la autoridad global del SaaS.
/// </summary>
public enum AmbitoAutorizacion
{
    Empresa = 1,
    Plataforma = 2
}
