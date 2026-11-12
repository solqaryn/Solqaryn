using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-008.F: prepara una matriz sintetica de casos por ambito y empresa.
/// Solo valida el fixture y su resultado ESPERADO para una prueba posterior;
/// NO certifica que el backend aplique autorizacion, JWT o aislamiento real.
/// </summary>
public sealed class I8008FPrepararCasoAmbitoGlobalFixtureTests
{
    [Theory]
    [InlineData(AmbitoAutorizacion.Empresa, 41, 41, false, true)]
    [InlineData(AmbitoAutorizacion.Empresa, 41, 42, true, false)]
    [InlineData(AmbitoAutorizacion.Plataforma, null, 41, false, false)]
    [InlineData(AmbitoAutorizacion.Plataforma, null, 41, true, false)]
    public void MatrizSintetica_DefineContextoMinimoSinBypassGlobal(
        AmbitoAutorizacion ambito,
        int? empresaDelRol,
        int empresaDestino,
        bool esAdministrador,
        bool candidatoEsperado)
    {
        var rol = new Rol
        {
            Id = 808,
            Nombre = "Rol fixture I8-008.F",
            NombreNormalizado = "I8008F_FIXTURE",
            EmpresaId = empresaDelRol,
            Ambito = ambito,
            EsSistema = false,
            EsAdministrador = esAdministrador,
            Activo = true
        };

        // Es una expectativa documentada del caso, no la funcion de permisos
        // del producto. La validacion real sera una prueba de servicio/HTTP.
        var tieneContextoEmpresaVerificado =
            rol.Ambito == AmbitoAutorizacion.Empresa &&
            rol.EmpresaId.HasValue &&
            rol.EmpresaId.Value == empresaDestino;

        Assert.Equal(candidatoEsperado, tieneContextoEmpresaVerificado);
        Assert.Equal(ambito, rol.Ambito);
        Assert.Equal(esAdministrador, rol.EsAdministrador);
        Assert.Equal(empresaDelRol, rol.EmpresaId);
    }

    [Fact]
    public void NegativoRector_RolPlataformaAdministradorNoAportaEmpresa()
    {
        var rol = new Rol
        {
            Id = 809,
            Nombre = "Plataforma fixture sin tenant",
            NombreNormalizado = "I8008F_PLATAFORMA",
            EmpresaId = null,
            Ambito = AmbitoAutorizacion.Plataforma,
            EsAdministrador = true,
            Activo = true
        };

        Assert.Null(rol.EmpresaId);
        Assert.Equal(AmbitoAutorizacion.Plataforma, rol.Ambito);
        Assert.True(rol.EsAdministrador);

        // No convertir la condicion sintetica en permiso productivo.
        // Esperado en la futura prueba integrada: DENEGAR sin contexto de
        // empresa autorizado y no exponer datos del tenant de destino.
    }
}
