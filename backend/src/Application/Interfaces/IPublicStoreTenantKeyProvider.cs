namespace Solqaryn.Application.Interfaces;

public interface IPublicStoreTenantKeyProvider
{
    Task<string> GetTenantKeyAsync(CancellationToken cancellationToken = default);
}
