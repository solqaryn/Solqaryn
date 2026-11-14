namespace Solqaryn.Application.DTOs;

public sealed class TiendaCuentaRegistroDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Clave { get; set; } = string.Empty;
}

public sealed class TiendaCuentaLoginDto
{
    public string Correo { get; set; } = string.Empty;
    public string Clave { get; set; } = string.Empty;
}

public sealed class TiendaCuentaSesionDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiraUtc { get; set; }
    public TiendaCuentaPerfilDto Perfil { get; set; } = new();
}

public sealed class TiendaCuentaPerfilDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
}

public sealed class TiendaDireccionDto
{
    public int Id { get; set; }
    public string Alias { get; set; } = string.Empty;
    public string Recibe { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public bool Predeterminada { get; set; }
}

public sealed class TiendaDireccionGuardarDto
{
    public string Alias { get; set; } = string.Empty;
    public string Recibe { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public bool Predeterminada { get; set; }
}

public sealed class TiendaPedidoCuentaDto
{
    public int Id { get; set; }
    public string Estado { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime FechaUtc { get; set; }
    public List<TiendaPedidoCuentaLineaDto> Lineas { get; set; } = new();
}

public sealed class TiendaPedidoCuentaLineaDto
{
    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Modelo { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Total { get; set; }
}

public sealed class TiendaNotificacionPedidoDto
{
    public int PedidoId { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public DateTime FechaUtc { get; set; }
}