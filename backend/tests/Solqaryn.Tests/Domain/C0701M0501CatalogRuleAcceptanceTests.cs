using Solqaryn.Domain.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.C0701CatalogDomainRules;

namespace Solqaryn.Tests.Domain;

/// <summary>C07-01.M05.01: pure domain xUnit tests, not JWT/HTTP/MySQL certification.</summary>
public sealed class C0701M0501CatalogRuleAcceptanceTests
{
    private static Rules.VerifiedServerScope Scope() => new(71, 17, 3, 4, true, true, true, true, false);
    private static Rules.ProductSnapshot Product() => new(71, 31, 9, 4, 71, 71, true, false);
    private static Rules.Mutation Mutation() => new(31, 9, "PRODUCTO", "ABC", "Producto", 100m, 50m, 1500, true, true, true, true);
    private static Rules.Decision Evaluate(Rules.VerifiedServerScope? scope = null, Rules.ProductSnapshot? product = null, Rules.Mutation? mutation = null)
        => Rules.Evaluate(scope ?? Scope(), product ?? Product(), mutation ?? Mutation());
    private const string Prepared = "DOMAIN_PRECONDITIONS_ONLY_BACKEND_GATE_REQUIRED";

    [Fact]
    public void PositiveExactBoundaryStillRequiresRealBackendAuthority()
    {
        var decision = Evaluate(mutation: Mutation() with {
            Sku = new string('A', 64), Nombre = new string('N', 150),
            Precio = 999999999.99m, Costo = 0m, ImpuestoBasisPoints = 10000 });
        Assert.Equal(Prepared, decision.Code);
        Assert.False(decision.AuthorizesWrite);
        Assert.False(decision.AuthorizesApiAccess);
        Assert.False(decision.PermitsCrossTenantAccess);
        Assert.False(decision.CertifiesJwtOrDatabase);
        Assert.Equal(Prepared, Evaluate(mutation: Mutation() with {
            Sku = "AAA", Nombre = "AB", Precio = 0m, ImpuestoBasisPoints = 0 }).Code);
    }

    [Theory]
    [InlineData("AB")]
    [InlineData("aBC")]
    [InlineData("ABC DEF")]
    [InlineData("A/B")]
    [InlineData("../A")]
    [InlineData("ÁBC")]
    [InlineData("ABC\n")]
    public void NonCanonicalSkusAreDenied(string sku) =>
        Assert.Equal("CATALOG_CODE_OR_NAME_INVALID", Evaluate(mutation: Mutation() with { Sku = sku }).Code);

    [Theory]
    [InlineData("A")]
    [InlineData(" Producto")]
    [InlineData("Producto ")]
    [InlineData("Producto\nX")]
    public void NonCanonicalNamesAreDenied(string name) =>
        Assert.Equal("CATALOG_CODE_OR_NAME_INVALID", Evaluate(mutation: Mutation() with { Nombre = name }).Code);

    [Fact]
    public void ExceedingExactLengthBoundaryIsRejected()
    {
        Assert.Equal("CATALOG_CODE_OR_NAME_INVALID", Evaluate(mutation: Mutation() with { Sku = new string('A', 65) }).Code);
        Assert.Equal("CATALOG_CODE_OR_NAME_INVALID", Evaluate(mutation: Mutation() with { Nombre = new string('N', 151) }).Code);
    }

    [Fact]
    public void PricesAndTaxRejectOverflowPrecisionAndUnderflow()
    {
        foreach (var amount in new[] { -0.01m, 1000000000.00m, 1.001m })
        {
            Assert.Equal("PRICE_COST_OR_TAX_INVALID", Evaluate(mutation: Mutation() with { Precio = amount }).Code);
            Assert.Equal("PRICE_COST_OR_TAX_INVALID", Evaluate(mutation: Mutation() with { Costo = amount }).Code);
        }
        foreach (var tax in new[] { -1, 10001 })
            Assert.Equal("PRICE_COST_OR_TAX_INVALID", Evaluate(mutation: Mutation() with { ImpuestoBasisPoints = tax }).Code);
    }

    [Fact]
    public void TenantAProductOfTenantBAndUnattributedLegacyAreDenied()
    {
        Assert.Equal("CATALOG_TENANT_OR_PRODUCT_OWNERSHIP_DENIED", Evaluate(product: Product() with { EmpresaId = 72 }).Code);
        Assert.Equal("CATALOG_TENANT_OR_PRODUCT_OWNERSHIP_DENIED", Evaluate(product: Product() with { EmpresaId = null }).Code);
        Assert.Equal("CATALOG_BRANCH_NOT_AUTHORIZED", Evaluate(scope: Scope() with { AuthorizedSucursalId = 5 }).Code);
        Assert.Equal("CATEGORY_TENANT_MISMATCH", Evaluate(product: Product() with { CategoriaEmpresaId = 72 }).Code);
        Assert.Equal("VARIANT_TENANT_MISMATCH", Evaluate(product: Product() with { VarianteEmpresaId = 72 }).Code);
    }

    [Fact]
    public void RevokedAndInactiveRelationalMembershipsDeny()
    {
        foreach (var bad in new[] { Scope() with { PermissionRevoked = true },
            Scope() with { MembershipActive = false }, Scope() with { RoleActive = false },
            Scope() with { TenantActive = false }, Scope() with { CatalogRbacGranted = false } })
            Assert.Equal("TENANT_MEMBERSHIP_OR_RBAC_DENIED", Evaluate(scope: bad).Code);
    }

    [Fact]
    public void ProductVersionAndIdentifierAreFailClosed()
    {
        Assert.Equal("CATALOG_STALE_VERSION", Evaluate(mutation: Mutation() with { ExpectedVersion = 8 }).Code);
        Assert.Equal("CATALOG_STALE_VERSION", Evaluate(product: Product() with { Version = 0 }).Code);
        Assert.Equal("CATALOG_TENANT_OR_PRODUCT_OWNERSHIP_DENIED", Evaluate(mutation: Mutation() with { ProductoId = 32 }).Code);
    }

    [Fact]
    public void InvalidRelationsTypeAndServiceVariantsDoNotPass()
    {
        Assert.Equal("CATALOG_RELATION_OR_VARIANT_NOT_VALID", Evaluate(mutation: Mutation() with { CategoriaActiva = false }).Code);
        Assert.Equal("CATALOG_RELATION_OR_VARIANT_NOT_VALID", Evaluate(mutation: Mutation() with { ImpuestoActivo = false }).Code);
        Assert.Equal("CATALOG_RELATION_OR_VARIANT_NOT_VALID", Evaluate(mutation: Mutation() with { VarianteActiva = false }).Code);
        Assert.Equal("CATALOG_RELATION_OR_VARIANT_NOT_VALID", Evaluate(mutation: Mutation() with { Tipo = "SERVICIO" }).Code);
        Assert.Equal(Prepared, Evaluate(mutation: Mutation() with { Tipo = "SERVICIO", HasVariants = false }).Code);
        Assert.Equal("CATALOG_TYPE_NOT_SUPPORTED", Evaluate(mutation: Mutation() with { Tipo = "producto" }).Code);
    }
}
