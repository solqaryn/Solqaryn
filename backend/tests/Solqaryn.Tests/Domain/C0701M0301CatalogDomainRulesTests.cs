using Solqaryn.Domain.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.C0701CatalogDomainRules;

namespace Solqaryn.Tests.Domain;

public sealed class C0701M0301CatalogDomainRulesTests
{
    private static Rules.VerifiedServerScope Scope() => new(
        100, 9, 5, 7, true, true, true, true, false);

    private static Rules.ProductSnapshot Product() => new(
        100, 42, 11, 7, 100, 100, true, false);

    private static Rules.Mutation Command() => new(
        42, 11, "PRODUCTO", "SKU-42", "Producto A",
        100.25m, 70.00m, 1500, true, true, true, true);

    [Fact]
    public void PositiveDomainPreconditionsCannotAuthorizeRuntimeWrite()
    {
        var result = Rules.Evaluate(Scope(), Product(), Command());
        Assert.Equal("DOMAIN_PRECONDITIONS_ONLY_BACKEND_GATE_REQUIRED", result.Code);
        Assert.False(result.AuthorizesWrite);
        Assert.False(result.AuthorizesApiAccess);
        Assert.False(result.CertifiesJwtOrDatabase);
        Assert.False(result.PermitsCrossTenantAccess);
    }

    [Fact]
    public void NullEvidenceAndMissingIdentityAreDenied()
    {
        Assert.Equal("VERIFIED_SERVER_SCOPE_REQUIRED", Rules.Evaluate(null, Product(), Command()).Code);
        Assert.Equal("VERIFIED_SERVER_SCOPE_REQUIRED", Rules.Evaluate(Scope(), null, Command()).Code);
        Assert.Equal("VERIFIED_SERVER_SCOPE_REQUIRED", Rules.Evaluate(Scope(), Product(), null).Code);
        Assert.Equal("VERIFIED_SERVER_SCOPE_REQUIRED", Rules.Evaluate(Scope() with { ActorId = 0 }, Product(), Command()).Code);
    }

    [Fact]
    public void RevokedInactiveAndMissingRelationalRbacNeverGrant()
    {
        foreach (var invalid in new[]
        {
            Scope() with { TenantActive = false },
            Scope() with { MembershipActive = false },
            Scope() with { RoleActive = false },
            Scope() with { CatalogRbacGranted = false },
            Scope() with { PermissionRevoked = true }
        })
            Assert.Equal("TENANT_MEMBERSHIP_OR_RBAC_DENIED",
                Rules.Evaluate(invalid, Product(), Command()).Code);
    }

    [Fact]
    public void TenantAActorCannotMutateTenantBOrLegacyUnattributedProduct()
    {
        Assert.Equal("CATALOG_TENANT_OR_PRODUCT_OWNERSHIP_DENIED",
            Rules.Evaluate(Scope(), Product() with { EmpresaId = 200 }, Command()).Code);
        Assert.Equal("CATALOG_TENANT_OR_PRODUCT_OWNERSHIP_DENIED",
            Rules.Evaluate(Scope(), Product() with { EmpresaId = null }, Command()).Code);
        Assert.Equal("CATALOG_TENANT_OR_PRODUCT_OWNERSHIP_DENIED",
            Rules.Evaluate(Scope(), Product(), Command() with { ProductoId = 99 }).Code);
    }

    [Fact]
    public void BranchCategoryAndVariantMustBelongToServerVerifiedTenant()
    {
        Assert.Equal("CATALOG_BRANCH_NOT_AUTHORIZED",
            Rules.Evaluate(Scope() with { AuthorizedSucursalId = 8 }, Product(), Command()).Code);
        Assert.Equal("CATEGORY_TENANT_MISMATCH",
            Rules.Evaluate(Scope(), Product() with { CategoriaEmpresaId = 200 }, Command()).Code);
        Assert.Equal("VARIANT_TENANT_MISMATCH",
            Rules.Evaluate(Scope(), Product() with { VarianteEmpresaId = 200 }, Command()).Code);
    }

    [Fact]
    public void StaleDeletedAndInactiveProductsAreRejected()
    {
        Assert.Equal("PRODUCT_INACTIVE_OR_DELETED",
            Rules.Evaluate(Scope(), Product() with { Eliminado = true }, Command()).Code);
        Assert.Equal("PRODUCT_INACTIVE_OR_DELETED",
            Rules.Evaluate(Scope(), Product() with { Activo = false }, Command()).Code);
        Assert.Equal("CATALOG_STALE_VERSION",
            Rules.Evaluate(Scope(), Product(), Command() with { ExpectedVersion = 10 }).Code);
    }

    [Fact]
    public void SkuAndNamesMustBeCanonicalNeverImplicitlyNormalized()
    {
        foreach (var candidate in new[] { "sku-42", "../SKU", "SKU 42", "" })
            Assert.Equal("CATALOG_CODE_OR_NAME_INVALID",
                Rules.Evaluate(Scope(), Product(), Command() with { Sku = candidate }).Code);
        Assert.Equal("CATALOG_CODE_OR_NAME_INVALID",
            Rules.Evaluate(Scope(), Product(), Command() with { Nombre = " Producto A " }).Code);
        Assert.Equal("CATALOG_CODE_OR_NAME_INVALID",
            Rules.Evaluate(Scope(), Product(), Command() with { Nombre = "A\nB" }).Code);
    }

    [Fact]
    public void MoneyTaxAndPrecisionBoundariesRejectInvalidValues()
    {
        foreach (var bad in new[] { -0.01m, 1.001m, 1000000000.00m })
            Assert.Equal("PRICE_COST_OR_TAX_INVALID",
                Rules.Evaluate(Scope(), Product(), Command() with { Precio = bad }).Code);
        Assert.Equal("PRICE_COST_OR_TAX_INVALID",
            Rules.Evaluate(Scope(), Product(), Command() with { Costo = -1m }).Code);
        Assert.Equal("PRICE_COST_OR_TAX_INVALID",
            Rules.Evaluate(Scope(), Product(), Command() with { ImpuestoBasisPoints = 10001 }).Code);
        Assert.Equal("DOMAIN_PRECONDITIONS_ONLY_BACKEND_GATE_REQUIRED",
            Rules.Evaluate(Scope(), Product(), Command() with { ImpuestoBasisPoints = 0, Precio = 0m }).Code);
    }

    [Fact]
    public void ServicesCannotCarryVariantsAndInactiveRelationsDeny()
    {
        Assert.Equal("CATALOG_RELATION_OR_VARIANT_NOT_VALID",
            Rules.Evaluate(Scope(), Product(), Command() with { Tipo = "SERVICIO" }).Code);
        Assert.Equal("CATALOG_RELATION_OR_VARIANT_NOT_VALID",
            Rules.Evaluate(Scope(), Product(), Command() with { CategoriaActiva = false }).Code);
        Assert.Equal("CATALOG_RELATION_OR_VARIANT_NOT_VALID",
            Rules.Evaluate(Scope(), Product(), Command() with { VarianteActiva = false }).Code);
        Assert.Equal("DOMAIN_PRECONDITIONS_ONLY_BACKEND_GATE_REQUIRED",
            Rules.Evaluate(Scope(), Product(), Command() with { Tipo = "SERVICIO", HasVariants = false }).Code);
        Assert.Equal("CATALOG_TYPE_NOT_SUPPORTED",
            Rules.Evaluate(Scope(), Product(), Command() with { Tipo = "producto" }).Code);
    }

    [Fact]
    public void NoDomainDecisionEverGrantsCrossTenantOrApiWrite()
    {
        foreach (var verdict in new[]
        {
            Rules.Evaluate(Scope(), Product(), Command()),
            Rules.Evaluate(Scope(), Product() with { EmpresaId = 200 }, Command()),
            Rules.Evaluate(Scope(), Product(), Command() with { Precio = -1m })
        })
        {
            Assert.False(verdict.AuthorizesWrite);
            Assert.False(verdict.AuthorizesApiAccess);
            Assert.False(verdict.PermitsCrossTenantAccess);
        }
    }
}
