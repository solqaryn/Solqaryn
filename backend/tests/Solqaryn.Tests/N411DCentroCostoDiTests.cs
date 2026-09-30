using Xunit;

namespace Solqaryn.Tests;

public sealed class N411DCentroCostoDiTests
{
    [Fact]
    public void Program_RegistersCentroCostoRepositoryAndService()
    {
        var programPath = FindRepositoryFile("backend/src/API/Program.cs");
        var program = File.ReadAllText(programPath);

        Assert.Contains("builder.Services.AddScoped<ICentroCostoRepository, CentroCostoRepository>();", program, StringComparison.Ordinal);
        Assert.Contains("builder.Services.AddScoped<ICentroCostoService, CentroCostoService>();", program, StringComparison.Ordinal);
    }

    private static string FindRepositoryFile(string relativePath)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException($"Repository file not found: {relativePath}");
    }
}
