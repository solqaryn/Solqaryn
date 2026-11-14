using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Regla explicita de entitlement de un modulo para un plan SaaS.
/// N6.10.B mantiene este contrato separado de PlanLimite: un limite cuantitativo
/// nunca implica habilitacion de modulo.
/// </summary>
public sealed class PlanModulo : AuditableEntity
{
    private PlanModulo()
    {
    }

    public PlanModulo(int planId, string planCodigo, string moduloClave)
    {
        if (planId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(planId), "El identificador del plan debe ser positivo.");
        }

        PlanId = planId;
        PlanCodigo = NormalizarPlanCodigo(planCodigo);
        ModuloClave = NormalizarModuloClave(moduloClave);
        Activo = true;
    }

    public int PlanId { get; private set; }
    public string PlanCodigo { get; private set; } = string.Empty;
    public string ModuloClave { get; private set; } = string.Empty;
    public bool Activo { get; private set; }

    public void Activar() => Activo = true;
    public void Desactivar() => Activo = false;

    internal static string NormalizarPlanCodigo(string planCodigo)
    {
        if (string.IsNullOrWhiteSpace(planCodigo))
        {
            throw new ArgumentException("El codigo canonico del plan es obligatorio.", nameof(planCodigo));
        }

        return planCodigo.Trim().ToUpperInvariant();
    }

    internal static string NormalizarModuloClave(string moduloClave)
    {
        if (string.IsNullOrWhiteSpace(moduloClave))
        {
            throw new ArgumentException("La clave canonica del modulo es obligatoria.", nameof(moduloClave));
        }

        return moduloClave.Trim().ToUpperInvariant();
    }

    internal static bool TryNormalizarModuloClave(string? moduloClave, out string normalizada)
    {
        if (string.IsNullOrWhiteSpace(moduloClave))
        {
            normalizada = string.Empty;
            return false;
        }

        normalizada = moduloClave.Trim().ToUpperInvariant();
        return normalizada.Length > 0;
    }
}

/// <summary>
/// Resultado fail-closed de la evaluacion server-side de un modulo.
/// </summary>
public readonly record struct DecisionModuloSaaS(bool Habilitado, MotivoDecisionModuloSaaS Motivo)
{
    public static DecisionModuloSaaS Habilitar() => new(true, MotivoDecisionModuloSaaS.HabilitadoPorReglaExplicita);
    public static DecisionModuloSaaS Deshabilitar(MotivoDecisionModuloSaaS motivo) => new(false, motivo);
}

public enum MotivoDecisionModuloSaaS
{
    HabilitadoPorReglaExplicita = 1,
    TenantInvalido = 2,
    TenantNoCoincide = 3,
    SuscripcionNoVigente = 4,
    PlanNoDisponible = 5,
    PlanNoCoincide = 6,
    ModuloInvalido = 7,
    SinReglaExplicita = 8
}

/// <summary>
/// Politica pura de dominio para resolver entitlements de modulos.
/// La autoridad de EmpresaId debe provenir del contexto tenant server-side.
/// Cualquier dato ausente, inconsistente o no explicitamente permitido falla cerrado.
/// </summary>
public static class PoliticaModulosSaaS
{
    public static DecisionModuloSaaS Evaluar(
        int empresaContextoId,
        int empresaObjetivoId,
        Suscripcion? suscripcion,
        Plan? planEfectivo,
        IEnumerable<PlanModulo>? reglas,
        string? moduloClave,
        DateTime instanteUtc)
    {
        if (empresaContextoId <= 0 || empresaObjetivoId <= 0)
        {
            return DecisionModuloSaaS.Deshabilitar(MotivoDecisionModuloSaaS.TenantInvalido);
        }

        if (empresaContextoId != empresaObjetivoId)
        {
            return DecisionModuloSaaS.Deshabilitar(MotivoDecisionModuloSaaS.TenantNoCoincide);
        }

        if (suscripcion is null ||
            suscripcion.EmpresaId != empresaContextoId ||
            !suscripcion.EsVigenteEn(instanteUtc))
        {
            return DecisionModuloSaaS.Deshabilitar(MotivoDecisionModuloSaaS.SuscripcionNoVigente);
        }

        if (planEfectivo is null || planEfectivo.Id <= 0 || !planEfectivo.Activo)
        {
            return DecisionModuloSaaS.Deshabilitar(MotivoDecisionModuloSaaS.PlanNoDisponible);
        }

        if (suscripcion.PlanId != planEfectivo.Id)
        {
            return DecisionModuloSaaS.Deshabilitar(MotivoDecisionModuloSaaS.PlanNoCoincide);
        }

        if (!PlanModulo.TryNormalizarModuloClave(moduloClave, out var moduloNormalizado))
        {
            return DecisionModuloSaaS.Deshabilitar(MotivoDecisionModuloSaaS.ModuloInvalido);
        }

        var planCodigo = planEfectivo.Codigo.Trim().ToUpperInvariant();
        var habilitado = reglas?.Any(regla =>
            regla.Activo &&
            regla.PlanId == planEfectivo.Id &&
            string.Equals(regla.PlanCodigo, planCodigo, StringComparison.Ordinal) &&
            string.Equals(regla.ModuloClave, moduloNormalizado, StringComparison.Ordinal)) == true;

        return habilitado
            ? DecisionModuloSaaS.Habilitar()
            : DecisionModuloSaaS.Deshabilitar(MotivoDecisionModuloSaaS.SinReglaExplicita);
    }
}
