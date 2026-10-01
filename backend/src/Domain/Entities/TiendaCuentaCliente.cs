using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

public sealed class TiendaCuentaCliente : BaseEntity
{
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string CorreoNormalizado { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool Activa { get; set; } = true;
    public DateTime? UltimoAccesoUtc { get; set; }

    public ICollection<TiendaSesionCliente> Sesiones { get; set; } = new List<TiendaSesionCliente>();
    public ICollection<TiendaDireccionCliente> Direcciones { get; set; } = new List<TiendaDireccionCliente>();
    public ICollection<TiendaFavoritoCliente> Favoritos { get; set; } = new List<TiendaFavoritoCliente>();
}

public sealed class TiendaSesionCliente : BaseEntity
{
    public int CuentaClienteId { get; set; }
    public TiendaCuentaCliente CuentaCliente { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiraUtc { get; set; }
    public DateTime? RevocadaUtc { get; set; }
    public DateTime? UltimoUsoUtc { get; set; }
}

public sealed class TiendaDireccionCliente : BaseEntity
{
    public int CuentaClienteId { get; set; }
    public TiendaCuentaCliente CuentaCliente { get; set; } = null!;
    public string Alias { get; set; } = string.Empty;
    public string Recibe { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public bool Predeterminada { get; set; }
}

public sealed class TiendaFavoritoCliente : BaseEntity
{
    public int CuentaClienteId { get; set; }
    public TiendaCuentaCliente CuentaCliente { get; set; } = null!;
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
}