using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Security;

/// <summary>
/// Narrow, fail-closed DEV support allowlist. Only the five reviewed catalog
/// read actions are eligible; all other controllers, actions and route templates
/// remain inaccessible even when they carry a read permission.
/// </summary>
public static class PlatformSupportPolicy
{
    public static bool IsAllowedModule(ModuloSistema modulo) =>
        modulo is ModuloSistema.Productos or ModuloSistema.Categorias;

    public static bool IsAllowedReadRoute(
        string method,
        string? controller,
        string? action,
        string? routeTemplate,
        ModuloSistema modulo)
    {
        if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) ||
            !IsAllowedModule(modulo) ||
            string.IsNullOrWhiteSpace(controller) ||
            string.IsNullOrWhiteSpace(action) ||
            string.IsNullOrWhiteSpace(routeTemplate))
            return false;

        var route = routeTemplate.TrimStart('/');

        return modulo switch
        {
            ModuloSistema.Productos =>
                string.Equals(controller, "Productos", StringComparison.Ordinal) &&
                ((string.Equals(action, "GetPaged", StringComparison.Ordinal) &&
                  string.Equals(route, "productos", StringComparison.OrdinalIgnoreCase)) ||
                 (string.Equals(action, "GetById", StringComparison.Ordinal) &&
                  string.Equals(route, "productos/{id:int}", StringComparison.OrdinalIgnoreCase))),
            ModuloSistema.Categorias =>
                string.Equals(controller, "Categorias", StringComparison.Ordinal) &&
                ((string.Equals(action, "GetAll", StringComparison.Ordinal) &&
                  string.Equals(route, "categorias", StringComparison.OrdinalIgnoreCase)) ||
                 (string.Equals(action, "GetActivas", StringComparison.Ordinal) &&
                  string.Equals(route, "categorias/activas", StringComparison.OrdinalIgnoreCase)) ||
                 (string.Equals(action, "GetById", StringComparison.Ordinal) &&
                  string.Equals(route, "categorias/{id:int}", StringComparison.OrdinalIgnoreCase))),
            _ => false
        };
    }
}
