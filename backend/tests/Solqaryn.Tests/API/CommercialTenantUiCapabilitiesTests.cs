using Microsoft.Extensions.Configuration;
using Solqaryn.API.Security;
using Solqaryn.Application.DTOs;
using Xunit;

namespace Solqaryn.Tests.API;

public sealed class CommercialTenantUiCapabilitiesTests
{
    private static MisPermisosDto Dto() => new()
    {
        Rol = "Administrador",
        EsAdministrador = true,
        Permisos = new List<string> { "Dashboard:Ver", "Productos:Ver", "Productos:Crear" }
    };

    [Fact]
    public void UnaEmpresa_PreservaGrantsSinRestringirMenuLegacy()
    {
        var dto = Dto();
        CommercialTenantUiCapabilities.Apply(dto, Config(), multipleEmpresas: false);
        Assert.False(dto.OperacionMultiEmpresaRestringida);
        Assert.Empty(dto.CapacidadesHabilitadas);
        Assert.Empty(dto.RutasHabilitadas);
        Assert.Equal(3, dto.Permisos.Count);
    }

    [Fact]
    public void TresEmpresas_CerradasNoDanCapacidadesAunqueSeanAdministrador()
    {
        var dto = Dto();
        CommercialTenantUiCapabilities.Apply(dto, Config(), multipleEmpresas: true);
        Assert.True(dto.OperacionMultiEmpresaRestringida);
        Assert.Empty(dto.CapacidadesHabilitadas);
        Assert.Empty(dto.RutasHabilitadas);
    }

    [Fact]
    public void LecturaCertificada_NoHabilitaEscrituraNiDashboardNiOtrasRutas()
    {
        var dto = Dto();
        CommercialTenantUiCapabilities.Apply(dto,
            Config(("MultiEmpresa:PrivateReadPilotEnabled", "true")), multipleEmpresas: true);
        Assert.Contains("Productos:Ver", dto.CapacidadesHabilitadas);
        Assert.Contains("productos", dto.RutasHabilitadas);
        Assert.Contains("productos/:id", dto.RutasHabilitadas);
        Assert.DoesNotContain("Productos:Crear", dto.CapacidadesHabilitadas);
        Assert.DoesNotContain("dashboard", dto.RutasHabilitadas);
        Assert.DoesNotContain("pedidos-venta", dto.RutasHabilitadas);
        Assert.DoesNotContain("ordenes-compra", dto.RutasHabilitadas);
    }

    [Fact]
    public void AltaYBorradores_RequierenSusFlagsIndependientes()
    {
        var dto = Dto();
        CommercialTenantUiCapabilities.Apply(dto,
            Config(("MultiEmpresa:PrivateWritePilotEnabled", "true"),
                ("MultiEmpresa:PrivateDraftPilotEnabled", "true")), multipleEmpresas: true);
        Assert.Contains("Clientes:Crear", dto.CapacidadesHabilitadas);
        Assert.Contains("clientes/nuevo", dto.RutasHabilitadas);
        Assert.Contains("Ventas:Crear", dto.CapacidadesHabilitadas);
        Assert.Contains("ventas/nueva", dto.RutasHabilitadas);
        Assert.DoesNotContain("Clientes:Editar", dto.CapacidadesHabilitadas);
        Assert.DoesNotContain("clientes/:id/editar", dto.RutasHabilitadas);
        Assert.DoesNotContain("finanzas", dto.RutasHabilitadas);
    }

    [Fact]
    public void OperacionesFinancierasSinUICompleta_NoAbrenModulosGenerales()
    {
        var dto = Dto();
        CommercialTenantUiCapabilities.Apply(dto,
            Config(("MultiEmpresa:PrivateFinancialPilotEnabled", "true"),
                ("MultiEmpresa:PrivatePdfPilotEnabled", "true"),
                ("MultiEmpresa:PrivateMediaExportPilotEnabled", "true")), multipleEmpresas: true);
        Assert.Empty(dto.CapacidadesHabilitadas);
        Assert.Empty(dto.RutasHabilitadas);
    }

    private static IConfiguration Config(params (string Key, string Value)[] variables) =>
        new ConfigurationBuilder().AddInMemoryCollection(
            variables.ToDictionary(x => x.Key, x => (string?)x.Value)).Build();
}
