using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// G0-002.C: delimita un unico escenario de composicion y lifetime para EmpresaService.
/// La resolucion de prueba no sustituye el arranque real del host ni certifica MySQL.
/// </summary>
public sealed class G0002CEmpresaServiceDiCompositionTests
{
    [Fact]
    public void Program_Registra_EmpresaService_UnaVez_ComoScoped_SinConstruccionManual()
    {
        var program = File.ReadAllText(FindRepositoryFile("backend/src/API/Program.cs"));

        const string scopedRegistration = @"\bbuilder\.Services\.AddScoped<IEmpresaService,\s*EmpresaService>\s*\(\s*\)\s*;";
        Assert.Equal(1, Regex.Matches(program, scopedRegistration).Count);
        Assert.DoesNotContain("new EmpresaService(", program);
    }

    [Fact]
    public void Servicio_FallaSinAuditoria_Y_SeAislaEntreScopes()
    {
        var services = new ServiceCollection();
        services.AddScoped<IEmpresaService, EmpresaService>();
        services.AddScoped<IEmpresaRepository>(_ => Mock.Of<IEmpresaRepository>());
        services.AddScoped<ICurrentUserService>(_ => Mock.Of<ICurrentUserService>());

        // Negativo: ninguna construccion ad hoc debe ocultar una dependencia obligatoria.
        using (var incompleteProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true }))
        using (var incompleteScope = incompleteProvider.CreateScope())
        {
            Assert.Throws<InvalidOperationException>(
                () => incompleteScope.ServiceProvider.GetRequiredService<IEmpresaService>());
        }

        services.AddScoped<IAuditoriaService>(_ => Mock.Of<IAuditoriaService>());

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scopeA = provider.CreateScope();
        using var scopeB = provider.CreateScope();

        var serviceA = scopeA.ServiceProvider.GetRequiredService<IEmpresaService>();
        var serviceB = scopeB.ServiceProvider.GetRequiredService<IEmpresaService>();

        Assert.IsType<EmpresaService>(serviceA);
        Assert.Same(serviceA, scopeA.ServiceProvider.GetRequiredService<IEmpresaService>());
        Assert.NotSame(serviceA, serviceB);
    }

    private static string FindRepositoryFile(string relativePath)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = Path.Combine(
                directory.FullName,
                relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException($"No se encontro el archivo del repositorio: {relativePath}");
    }
}
