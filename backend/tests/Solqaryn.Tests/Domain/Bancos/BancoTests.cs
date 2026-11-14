using Solqaryn.Domain.Entities.Catalogos;
using Xunit;

namespace Solqaryn.Tests.Domain.Bancos;

public class BancoTests
{
    [Fact]
    public void Banco_PropertiesCanBeSet()
    {
        var banco = new Banco { Codigo = "BANC01", Nombre = "Banco Uno", SwiftBic = "BUNOHNXX", Activo = false, Eliminado = true, FechaEliminacion = new DateTime(2026, 1, 1), EliminadoPorUsuarioId = 99 };
        Assert.Equal("BANC01", banco.Codigo);
        Assert.Equal("Banco Uno", banco.Nombre);
        Assert.Equal("BUNOHNXX", banco.SwiftBic);
        Assert.False(banco.Activo);
        Assert.True(banco.Eliminado);
        Assert.Equal(new DateTime(2026, 1, 1), banco.FechaEliminacion);
        Assert.Equal(99, banco.EliminadoPorUsuarioId);
    }
}
