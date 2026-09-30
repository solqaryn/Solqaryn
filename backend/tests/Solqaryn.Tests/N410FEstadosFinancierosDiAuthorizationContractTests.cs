using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Solqaryn.Tests;

public class N410FEstadosFinancierosDiAuthorizationContractTests
{
    [Fact]
    public void Program_Registers_EstadoFinancieroService_WithConcreteImplementation()
    {
        var repositoryRoot = FindRepositoryRoot();
        var programPath = Path.Combine(repositoryRoot, "backend", "src", "API", "Program.cs");
        var program = File.ReadAllText(programPath);

        Assert.Contains(
            "builder.Services.AddScoped<IEstadoFinancieroService, EstadoFinancieroService>();",
            program);
        Assert.DoesNotContain(
            "AddScoped<IEstadoFinancieroService>(sp => throw",
            program);
    }

    [Fact]
    public void Controller_Requires_Authentication_And_FinanzasVer()
    {
        var controllerType = typeof(EstadosFinancierosController);
        Assert.NotNull(controllerType.GetCustomAttribute<AuthorizeAttribute>(inherit: true));
        Assert.Null(controllerType.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true));

        var generar = controllerType.GetMethod("Generar", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(generar);
        Assert.Null(generar!.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true));

        var permiso = generar.GetCustomAttribute<RequierePermisoAttribute>(inherit: true);
        Assert.NotNull(permiso);

        var moduloField = typeof(RequierePermisoAttribute)
            .GetField("_modulo", BindingFlags.NonPublic | BindingFlags.Instance);
        var accionField = typeof(RequierePermisoAttribute)
            .GetField("_accion", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(moduloField);
        Assert.NotNull(accionField);
        Assert.Equal(ModuloSistema.Finanzas, moduloField!.GetValue(permiso));
        Assert.Equal(AccionPermiso.Ver, accionField!.GetValue(permiso));
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "backend", "Solqaryn.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Solqaryn repository root from the test base directory.");
    }
}
