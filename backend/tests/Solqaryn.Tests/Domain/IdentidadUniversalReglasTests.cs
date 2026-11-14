using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.Security;
using Xunit;

namespace Solqaryn.Tests.Domain;

public sealed class IdentidadUniversalReglasTests
{
    private static (Usuario U, Empresa E, UsuarioEmpresa M, Rol R) Vigentes()
    {
        var u = new Usuario { Id = 7, Activo = true, RolId = 999 };
        var e = new Empresa("A") { Id = 11 };
        var m = new UsuarioEmpresa(7, 11, 19);
        var r = new Rol { Id = 19, EmpresaId = 11, Activo = true };
        return (u, e, m, r);
    }

    private static ContextoTenantActual Contexto()
    {
        var (u, e, m, r) = Vigentes();
        return IdentidadUniversalReglas.CrearContextoVerificado(u, e, m, r);
    }

    [Fact]
    public void ContextoValido_UsaRolDeMembresiaNoRolLegacy()
    {
        var (u, e, m, r) = Vigentes();
        var ctx = IdentidadUniversalReglas.CrearContextoVerificado(u, e, m, r);
        Assert.Equal(7, ctx.UsuarioId);
        Assert.Equal(11, ctx.EmpresaId);
        Assert.Equal(19, ctx.RolId);
    }

    [Fact]
    public void MembresiaRevocada_FallaCerrado()
    {
        var (u, e, m, r) = Vigentes();
        m.Desactivar();
        Assert.Throws<InvalidOperationException>(() =>
            IdentidadUniversalReglas.CrearContextoVerificado(u, e, m, r));
    }

    [Fact]
    public void MiembroDeEmpresaB_NoTieneContextoA()
    {
        var (u, e, _, r) = Vigentes();
        var m = new UsuarioEmpresa(7, 22, 19);
        Assert.Throws<InvalidOperationException>(() =>
            IdentidadUniversalReglas.CrearContextoVerificado(u, e, m, r));
    }

    [Fact]
    public void MiembroDeOtroUsuario_NoTieneContexto()
    {
        var (u, e, _, r) = Vigentes();
        var m = new UsuarioEmpresa(8, 11, 19);
        Assert.Throws<InvalidOperationException>(() =>
            IdentidadUniversalReglas.CrearContextoVerificado(u, e, m, r));
    }

    [Theory]
    [InlineData("inactivo")]
    [InlineData("bloqueado")]
    [InlineData("eliminado")]
    [InlineData("sin-id")]
    public void UsuarioInvalido_FallaCerrado(string estado)
    {
        var (u, e, m, r) = Vigentes();
        switch (estado)
        {
            case "inactivo": u.Activo = false; break;
            case "bloqueado": u.Bloqueado = true; break;
            case "eliminado": u.Eliminado = true; break;
            case "sin-id": u.Id = 0; break;
        }
        Assert.Throws<InvalidOperationException>(() =>
            IdentidadUniversalReglas.CrearContextoVerificado(u, e, m, r));
    }

    [Fact]
    public void EmpresaInactiva_FallaCerrado()
    {
        var (u, e, m, r) = Vigentes();
        e.Desactivar();
        Assert.Throws<InvalidOperationException>(() =>
            IdentidadUniversalReglas.CrearContextoVerificado(u, e, m, r));
    }

    [Theory]
    [InlineData("ajeno")]
    [InlineData("plataforma")]
    [InlineData("inactivo")]
    [InlineData("eliminado")]
    [InlineData("otro-id")]
    [InlineData("sin-id")]
    public void RolNoAsignable_FallaCerrado(string estado)
    {
        var (u, e, m, r) = Vigentes();
        switch (estado)
        {
            case "ajeno": r.EmpresaId = 22; break;
            case "plataforma": r.Ambito = AmbitoAutorizacion.Plataforma; break;
            case "inactivo": r.Activo = false; break;
            case "eliminado": r.Eliminado = true; break;
            case "otro-id": r.Id = 99; break;
            case "sin-id": r.Id = 0; break;
        }
        Assert.Throws<InvalidOperationException>(() =>
            IdentidadUniversalReglas.CrearContextoVerificado(u, e, m, r));
    }

    [Fact]
    public void RolSistemaEmpresa_PuedeAsignarseSinOwnerPropio()
    {
        var (u, e, m, r) = Vigentes();
        r.EmpresaId = null;
        r.EsSistema = true;
        Assert.Equal(11,
            IdentidadUniversalReglas.CrearContextoVerificado(u, e, m, r).EmpresaId);
    }

    [Fact]
    public void RolLegacySinOwnerNiSistema_NoDaAcceso()
    {
        var (u, e, m, r) = Vigentes();
        r.EmpresaId = null;
        Assert.Throws<InvalidOperationException>(() =>
            IdentidadUniversalReglas.CrearContextoVerificado(u, e, m, r));
    }

    [Fact]
    public void SucursalPropia_SeAcepta_YExternaSeRechaza()
    {
        var ctx = Contexto();
        IdentidadUniversalReglas.ExigirSucursal(ctx,
            new Sucursal { Id = 3, EmpresaId = 11 });
        Assert.Throws<InvalidOperationException>(() =>
            IdentidadUniversalReglas.ExigirSucursal(ctx,
                new Sucursal { Id = 4, EmpresaId = 22 }));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public void SucursalSinEmpresaValida_FallaCerrado(int? empresaId)
    {
        Assert.Throws<InvalidOperationException>(() =>
            IdentidadUniversalReglas.ExigirSucursal(Contexto(),
                new Sucursal { Id = 3, EmpresaId = empresaId }));
    }

    [Theory]
    [InlineData("inactiva")]
    [InlineData("eliminada")]
    [InlineData("sin-id")]
    public void SucursalNoVigente_FallaCerrado(string estado)
    {
        var s = new Sucursal { Id = 3, EmpresaId = 11 };
        switch (estado)
        {
            case "inactiva": s.Activa = false; break;
            case "eliminada": s.Eliminado = true; break;
            case "sin-id": s.Id = 0; break;
        }
        Assert.Throws<InvalidOperationException>(() =>
            IdentidadUniversalReglas.ExigirSucursal(Contexto(), s));
    }

    [Fact]
    public void ReferenciasNulas_NoPuedenCrearContexto()
    {
        var (u, e, m, r) = Vigentes();
        Assert.Throws<ArgumentNullException>(() =>
            IdentidadUniversalReglas.CrearContextoVerificado(null!, e, m, r));
        Assert.Throws<ArgumentNullException>(() =>
            IdentidadUniversalReglas.CrearContextoVerificado(u, e, null!, r));
        Assert.Throws<ArgumentNullException>(() =>
            IdentidadUniversalReglas.ExigirSucursal(Contexto(), null!));
    }
}
