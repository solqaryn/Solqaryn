global using MySqlValueGenerationStrategy = Solqaryn.Infrastructure.Migrations.History.MySqlValueGenerationStrategy;

namespace Solqaryn.Infrastructure.Migrations.History;

/// <summary>
/// Compatibilidad de compilación para las migraciones históricas Pomelo.
/// El runtime productivo no consume este assembly ni este enum.
/// </summary>
public enum MySqlValueGenerationStrategy
{
    None = 0,
    IdentityColumn = 1
}
