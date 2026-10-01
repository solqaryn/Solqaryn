using Solqaryn.API.Configuration;
using Xunit;

namespace Solqaryn.Tests.API;

public sealed class EnvironmentDatabaseGuardTests
{
    [Theory]
    [InlineData("Development", "solqaryn_dev", "solqaryn_dev_user")]
    [InlineData("Production", "solqaryn_prod", "solqaryn_prod_user")]
    public void ValidateRenderBinding_AceptaBindingCanonico(string environment, string database, string user)
    {
        var connection = $"Server=db.example;Port=3306;Database={database};User ID={user};Password=test;SslMode=Required;";
        EnvironmentDatabaseGuard.ValidateRenderBinding(environment, connection);
    }

    [Theory]
    [InlineData("Development", "solqaryn_prod", "solqaryn_dev_user")]
    [InlineData("Development", "solqaryn_dev", "solqaryn_prod_user")]
    [InlineData("Production", "solqaryn_dev", "solqaryn_prod_user")]
    [InlineData("Production", "solqaryn_prod", "solqaryn_dev_user")]
    public void ValidateRenderBinding_RechazaBaseOUsuarioCruzado(string environment, string database, string user)
    {
        var connection = $"Server=db.example;Port=3306;Database={database};User ID={user};Password=test;SslMode=Required;";
        Assert.Throws<InvalidOperationException>(() =>
            EnvironmentDatabaseGuard.ValidateRenderBinding(environment, connection));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Staging")]
    [InlineData("DEV")]
    [InlineData("PROD")]
    public void ValidateRenderBinding_RechazaEntornoAmbiguoONoCanonico(string environment)
    {
        const string connection = "Server=db.example;Port=3306;Database=solqaryn_dev;User ID=solqaryn_dev_user;Password=test;SslMode=Required;";
        Assert.Throws<InvalidOperationException>(() =>
            EnvironmentDatabaseGuard.ValidateRenderBinding(environment, connection));
    }

    [Fact]
    public void ValidateRenderBinding_RechazaCadenaSinBaseCanonica()
    {
        const string connection = "Server=db.example;Port=3306;User ID=solqaryn_dev_user;Password=test;SslMode=Required;";
        Assert.Throws<InvalidOperationException>(() =>
            EnvironmentDatabaseGuard.ValidateRenderBinding("Development", connection));
    }
}
