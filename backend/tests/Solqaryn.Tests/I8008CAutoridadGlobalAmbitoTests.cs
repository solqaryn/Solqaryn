using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-008.C: el ámbito global Plataforma no sirve como autorización de una
/// empresa. Prueba unitaria del predicado de asignación, sin HTTP/JWT/MySQL.
/// </summary>
public sealed class I8008CAutoridadGlobalAmbitoTests
{
    [Fact]
    public void Rol_Plataforma_no_es_asignable_a_tenants_ni_con_es_sistema_o_propietario_falsificado()
    {
        var rol = new Rol
        {
            Nombre = "Autoridad global",
            NombreNormalizado = "I8008C_PLATAFORMA",
            Ambito = AmbitoAutorizacion.Plataforma,
            EmpresaId = null,
            EsSistema = true,
            EsAdministrador = true,
            Activo = true
        };

        Assert.False(rol.EsAsignableAEmpresa(101));
        Assert.False(rol.EsAsignableAEmpresa(202));

        // La falsificación del propietario no puede convertir ámbito global en Empresa.
        rol.EmpresaId = 101;
        Assert.False(rol.EsAsignableAEmpresa(101));
        Assert.False(rol.EsAsignableAEmpresa(202));
    }

    [Fact]
    public void Rol_Empresa_respeta_propietario_plantilla_y_revocacion_sin_elevar_Plataforma()
    {
        var rol = new Rol
        {
            Nombre = "Operador privado A",
            NombreNormalizado = "I8008C_OPERADOR_A",
            Ambito = AmbitoAutorizacion.Empresa,
            EmpresaId = 101,
            EsSistema = false,
            EsAdministrador = false,
            Activo = true
        };

        Assert.True(rol.EsAsignableAEmpresa(101));
        Assert.False(rol.EsAsignableAEmpresa(202));
        Assert.False(rol.EsAsignableAEmpresa(0));

        // EsSistema no convierte un rol privado de A en rol de B.
        rol.EsSistema = true;
        Assert.False(rol.EsAsignableAEmpresa(202));

        // Sólo la plantilla empresarial de sistema y sin propietario puede reutilizarse.
        rol.EmpresaId = null;
        Assert.True(rol.EsAsignableAEmpresa(101));
        Assert.True(rol.EsAsignableAEmpresa(202));
        rol.EsSistema = false;
        Assert.False(rol.EsAsignableAEmpresa(101));
        Assert.False(rol.EsAsignableAEmpresa(202));

        rol.EsSistema = true;
        rol.Ambito = AmbitoAutorizacion.Plataforma;
        Assert.False(rol.EsAsignableAEmpresa(101));

        rol.Ambito = AmbitoAutorizacion.Empresa;
        rol.Activo = false;
        Assert.False(rol.EsAsignableAEmpresa(101));

        rol.Activo = true;
        rol.Eliminado = true;
        Assert.False(rol.EsAsignableAEmpresa(101));
    }
}
