namespace Microsoft.EntityFrameworkCore.Metadata;

/// <summary>
/// Compatibilidad de compilación para las migraciones históricas Pomelo.
/// El runtime productivo no consume este assembly ni este enum.
/// </summary>
public enum MySqlValueGenerationStrategy
{
    None = 0,
    IdentityColumn = 1
}
