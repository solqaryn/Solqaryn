using System.Text.RegularExpressions;

namespace Solqaryn.Infrastructure.Governance;

/// <summary>Instantánea institucional efímera: nunca fuente de permisos, estados o aprobaciones.</summary>
public sealed record LecturaGobiernoAislada(
    string MicrotareaId, long RevisionFuente, string ShaFuente,
    string EstadoObservado, DateTimeOffset FechaLecturaUtc)
{
    public string AmbitoInstitucional => "SOLQARYN:DEV";
    public bool AutorizaEjecucion => false;
    public bool AutorizaAprobacion => false;
    public bool AutorizaPersistenciaOficial => false;
    public bool AutorizaConcesionPermisos => false;
}

/// <summary>
/// C00-04.M03.04: adaptador aislado EN MEMORIA, sin SQL, API, DI, webhook ni Tasks.
/// Almacena copias no autoritativas; no equivale a lease, CAS o lectura actual de Sheets.
/// Una clave institucional no es EmpresaId ni acredita tenant comercial.
/// </summary>
public sealed class GobiernoCambiosProyeccionAislada
{
    private static readonly Regex IdValido = new(
        @"^C[0-9]{2}-[0-9]{2}\.M[0-9]{2}\.[0-9]{2}\z",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex ShaValido = new(
        @"^[a-f0-9]{40}\z", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private readonly object _mutex = new();
    private readonly Dictionary<(string Id, long Revision), LecturaGobiernoAislada> _copias = new();
    public const int LimiteLecturas = 4096;

    public int Cantidad
    {
        get { lock (_mutex) return _copias.Count; }
    }

    public bool IntentarRegistrar(LecturaGobiernoAislada lectura)
    {
        ArgumentNullException.ThrowIfNull(lectura);
        ExigirId(lectura.MicrotareaId);
        if (lectura.RevisionFuente <= 0)
            throw new ArgumentOutOfRangeException(nameof(lectura));
        if (lectura.ShaFuente is null || !ShaValido.IsMatch(lectura.ShaFuente))
            throw new ArgumentException("SHA inválido.", nameof(lectura));
        if (lectura.EstadoObservado is not ("PENDIENTE" or "EN_PROGRESO" or "VALIDANDO" or "LISTO"))
            throw new ArgumentException("Estado observado inválido.", nameof(lectura));
        if (lectura.FechaLecturaUtc == default || lectura.FechaLecturaUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("La fecha debe estar en UTC.", nameof(lectura));

        lock (_mutex)
        {
            var clave = (lectura.MicrotareaId, lectura.RevisionFuente);
            if (_copias.ContainsKey(clave) || _copias.Count >= LimiteLecturas)
                return false;
            _copias.Add(clave, lectura);
            return true;
        }
    }

    public LecturaGobiernoAislada? Obtener(string microtareaId, long revisionFuente)
    {
        ExigirId(microtareaId);
        if (revisionFuente <= 0)
            throw new ArgumentOutOfRangeException(nameof(revisionFuente));
        lock (_mutex)
            return _copias.GetValueOrDefault((microtareaId, revisionFuente));
    }

    private static void ExigirId(string? id)
    {
        if (id is null || !IdValido.IsMatch(id))
            throw new ArgumentException("ID institucional inválido.", nameof(id));
    }
}
