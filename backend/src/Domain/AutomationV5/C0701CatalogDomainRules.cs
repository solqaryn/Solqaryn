namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// C07-01.M03.01: validaciones puras del catalogo universal a partir de evidencia
/// verificada en servidor. No concede acceso, crea grants, guarda datos ni sustituye
/// JWT/RBAC, tenancy, repositorio o transacciones de Application/API.
/// </summary>
public static class C0701CatalogDomainRules
{
    public sealed record VerifiedServerScope(
        int EmpresaId,
        int ActorId,
        int RoleId,
        int? AuthorizedSucursalId,
        bool TenantActive,
        bool MembershipActive,
        bool RoleActive,
        bool CatalogRbacGranted,
        bool PermissionRevoked);

    public sealed record ProductSnapshot(
        int? EmpresaId,
        int ProductoId,
        long Version,
        int? SucursalId,
        int? CategoriaEmpresaId,
        int? VarianteEmpresaId,
        bool Activo,
        bool Eliminado);

    public sealed record Mutation(
        int ProductoId,
        long ExpectedVersion,
        string? Tipo,
        string? Sku,
        string? Nombre,
        decimal Precio,
        decimal Costo,
        int ImpuestoBasisPoints,
        bool CategoriaActiva,
        bool ImpuestoActivo,
        bool VarianteActiva,
        bool HasVariants);

    public readonly record struct Decision(string Code)
    {
        public bool AuthorizesWrite => false;
        public bool AuthorizesApiAccess => false;
        public bool CertifiesJwtOrDatabase => false;
        public bool PermitsCrossTenantAccess => false;
    }

    public static Decision Evaluate(
        VerifiedServerScope? scope,
        ProductSnapshot? product,
        Mutation? command)
    {
        if (scope is null || product is null || command is null ||
            scope.EmpresaId <= 0 || scope.ActorId <= 0 || scope.RoleId <= 0)
            return Deny("VERIFIED_SERVER_SCOPE_REQUIRED");

        if (!scope.TenantActive || !scope.MembershipActive || !scope.RoleActive ||
            scope.PermissionRevoked || !scope.CatalogRbacGranted)
            return Deny("TENANT_MEMBERSHIP_OR_RBAC_DENIED");

        // Un producto legacy sin EmpresaId atribuido no demuestra propiedad.
        if (product.EmpresaId != scope.EmpresaId || product.ProductoId <= 0 ||
            command.ProductoId != product.ProductoId)
            return Deny("CATALOG_TENANT_OR_PRODUCT_OWNERSHIP_DENIED");

        if (product.SucursalId is int branch &&
            (branch <= 0 || scope.AuthorizedSucursalId != branch))
            return Deny("CATALOG_BRANCH_NOT_AUTHORIZED");

        if (product.CategoriaEmpresaId is int categoryTenant &&
            categoryTenant != scope.EmpresaId)
            return Deny("CATEGORY_TENANT_MISMATCH");

        if (product.VarianteEmpresaId is int variantTenant &&
            variantTenant != scope.EmpresaId)
            return Deny("VARIANT_TENANT_MISMATCH");

        if (product.Eliminado || !product.Activo)
            return Deny("PRODUCT_INACTIVE_OR_DELETED");

        if (product.Version <= 0 || command.ExpectedVersion != product.Version)
            return Deny("CATALOG_STALE_VERSION");

        if (!string.Equals(command.Tipo, "PRODUCTO", StringComparison.Ordinal) &&
            !string.Equals(command.Tipo, "SERVICIO", StringComparison.Ordinal))
            return Deny("CATALOG_TYPE_NOT_SUPPORTED");

        if (!SafeSku(command.Sku) || !SafeName(command.Nombre))
            return Deny("CATALOG_CODE_OR_NAME_INVALID");

        // Decimal preciso y impuesto en basis points (0..10000 = 0..100%).
        if (!Money(command.Precio) || !Money(command.Costo) ||
            command.ImpuestoBasisPoints is < 0 or > 10000)
            return Deny("PRICE_COST_OR_TAX_INVALID");

        if (!command.CategoriaActiva || !command.ImpuestoActivo ||
            (command.HasVariants && !command.VarianteActiva) ||
            (command.Tipo == "SERVICIO" && command.HasVariants))
            return Deny("CATALOG_RELATION_OR_VARIANT_NOT_VALID");

        // Las capas superiores deben volver a autenticar y reconsultar la
        // pertenencia, autorizacion y version en la misma transaccion DB.
        return Deny("DOMAIN_PRECONDITIONS_ONLY_BACKEND_GATE_REQUIRED");
    }

    private static Decision Deny(string reason) => new(reason);

    private static bool Money(decimal amount) =>
        amount >= 0m && amount <= 999999999.99m &&
        decimal.Round(amount, 2) == amount;

    private static bool SafeSku(string? value) =>
        value is { Length: >= 3 and <= 64 } &&
        value.All(c => c is >= 'A' and <= 'Z' or >= '0' and <= '9' or '_' or '-');

    private static bool SafeName(string? value) =>
        value is { Length: >= 2 and <= 150 } &&
        string.Equals(value, value.Trim(), StringComparison.Ordinal) &&
        value.All(c => !char.IsControl(c));
}
