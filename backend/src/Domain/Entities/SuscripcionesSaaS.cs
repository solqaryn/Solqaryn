using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Catalogo SaaS independiente de la identidad tenant. N6.9.B define solamente
/// dominio e invariantes; persistencia, API y enforcement se incorporan en parents
/// posteriores.
/// </summary>
public sealed class Plan : AuditableEntity
{
    private readonly List<PlanLimite> _limites = new();

    private Plan()
    {
    }

    public Plan(string codigo, string nombre)
    {
        CambiarCodigo(codigo);
        CambiarNombre(nombre);
    }

    public string Codigo { get; private set; } = string.Empty;
    public string Nombre { get; private set; } = string.Empty;
    public bool Activo { get; private set; } = true;

    public IReadOnlyCollection<PlanLimite> Limites => _limites.AsReadOnly();

    public void CambiarCodigo(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new ArgumentException("El codigo del plan es obligatorio.", nameof(codigo));
        }

        Codigo = codigo.Trim().ToUpperInvariant();
    }

    public void CambiarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre del plan es obligatorio.", nameof(nombre));
        }

        Nombre = nombre.Trim();
    }

    /// <summary>
    /// Mantiene una sola fuente de verdad por clave. null significa capacidad sin
    /// limite cuantitativo; cero o valores negativos son configuraciones invalidas.
    /// </summary>
    public void DefinirLimite(string clave, int? valorMaximo)
    {
        var claveNormalizada = PlanLimite.NormalizarClave(clave);
        if (valorMaximo is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(valorMaximo), "El limite debe ser mayor que cero o null para ilimitado.");
        }

        var existente = _limites.SingleOrDefault(x => x.Clave == claveNormalizada);
        if (existente is null)
        {
            _limites.Add(new PlanLimite(claveNormalizada, valorMaximo));
            return;
        }

        existente.CambiarValor(valorMaximo);
    }

    public void Activar() => Activo = true;
    public void Desactivar() => Activo = false;
}

/// <summary>
/// Capacidad/limite perteneciente al catalogo de un Plan. No porta EmpresaId:
/// la asignacion tenant ocurre exclusivamente mediante Suscripcion.
/// </summary>
public sealed class PlanLimite
{
    private PlanLimite()
    {
    }

    internal PlanLimite(string clave, int? valorMaximo)
    {
        Clave = NormalizarClave(clave);
        CambiarValor(valorMaximo);
    }

    public string Clave { get; private set; } = string.Empty;
    public int? ValorMaximo { get; private set; }

    internal void CambiarValor(int? valorMaximo)
    {
        if (valorMaximo is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(valorMaximo), "El limite debe ser mayor que cero o null para ilimitado.");
        }

        ValorMaximo = valorMaximo;
    }

    internal static string NormalizarClave(string clave)
    {
        if (string.IsNullOrWhiteSpace(clave))
        {
            throw new ArgumentException("La clave del limite es obligatoria.", nameof(clave));
        }

        return clave.Trim().ToUpperInvariant();
    }
}

/// <summary>
/// Asignacion de un Plan a la raiz tenant Empresa. EmpresaId es obligatorio y
/// nunca representa autoridad proveniente del cliente: las capas Application/API
/// deberan derivarlo del contexto tenant server-side en N6.9.D.
/// </summary>
public sealed class Suscripcion : AuditableEntity
{
    private Suscripcion()
    {
    }

    public Suscripcion(int empresaId, int planId, DateTime inicioUtc, DateTime? finUtc = null)
    {
        ValidarIdPositivo(empresaId, nameof(empresaId));
        ValidarIdPositivo(planId, nameof(planId));
        ValidarPeriodo(inicioUtc, finUtc);

        EmpresaId = empresaId;
        PlanId = planId;
        InicioUtc = NormalizarUtc(inicioUtc);
        FinUtc = finUtc.HasValue ? NormalizarUtc(finUtc.Value) : null;
        Estado = EstadoSuscripcion.Activa;
    }

    public int EmpresaId { get; private set; }
    public int PlanId { get; private set; }
    public EstadoSuscripcion Estado { get; private set; }
    public DateTime InicioUtc { get; private set; }
    public DateTime? FinUtc { get; private set; }

    public void CambiarPlan(int planId)
    {
        ValidarIdPositivo(planId, nameof(planId));
        if (Estado is EstadoSuscripcion.Cancelada or EstadoSuscripcion.Expirada)
        {
            throw new InvalidOperationException("No se puede cambiar el plan de una suscripcion finalizada.");
        }

        PlanId = planId;
    }

    public void Suspender()
    {
        if (Estado != EstadoSuscripcion.Activa)
        {
            throw new InvalidOperationException("Solo una suscripcion activa puede suspenderse.");
        }

        Estado = EstadoSuscripcion.Suspendida;
    }

    public void Reactivar()
    {
        if (Estado != EstadoSuscripcion.Suspendida)
        {
            throw new InvalidOperationException("Solo una suscripcion suspendida puede reactivarse.");
        }

        Estado = EstadoSuscripcion.Activa;
    }

    public void Cancelar(DateTime canceladaUtc)
    {
        if (Estado == EstadoSuscripcion.Cancelada)
        {
            return;
        }

        var instante = NormalizarUtc(canceladaUtc);
        if (instante < InicioUtc)
        {
            throw new ArgumentOutOfRangeException(nameof(canceladaUtc), "La cancelacion no puede ser anterior al inicio.");
        }

        Estado = EstadoSuscripcion.Cancelada;
        FinUtc = instante;
    }

    public bool EsVigenteEn(DateTime instanteUtc)
    {
        if (Estado != EstadoSuscripcion.Activa)
        {
            return false;
        }

        var instante = NormalizarUtc(instanteUtc);
        return instante >= InicioUtc && (!FinUtc.HasValue || instante < FinUtc.Value);
    }

    private static void ValidarPeriodo(DateTime inicioUtc, DateTime? finUtc)
    {
        var inicio = NormalizarUtc(inicioUtc);
        if (finUtc.HasValue && NormalizarUtc(finUtc.Value) <= inicio)
        {
            throw new ArgumentOutOfRangeException(nameof(finUtc), "El fin debe ser posterior al inicio.");
        }
    }

    private static void ValidarIdPositivo(int id, string parametro)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(parametro, "El identificador debe ser positivo.");
        }
    }

    private static DateTime NormalizarUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
}

public enum EstadoSuscripcion
{
    Activa = 1,
    Suspendida = 2,
    Cancelada = 3,
    Expirada = 4
}
