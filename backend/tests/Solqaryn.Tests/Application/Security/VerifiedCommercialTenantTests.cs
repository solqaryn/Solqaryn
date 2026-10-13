using Microsoft.AspNetCore.Http;
using Solqaryn.Application.Security;
using Xunit;

namespace Solqaryn.Tests.Application.Security;

public class VerifiedCommercialTenantTests
{
    [Fact]
    public void Header_no_concede_propiedad_empresarial()
    {
        Assert.Null(VerifiedCommercialTenant.Resolve(null));
        var request = new DefaultHttpContext();
        request.Request.Headers["X-Empresa-Id"] = "222";
        Assert.Null(VerifiedCommercialTenant.Resolve(request));
    }

    [Fact]
    public void Solo_acepta_marca_verificada_del_backend()
    {
        var request = new DefaultHttpContext();
        request.Request.Headers["X-Empresa-Id"] = "999";
        request.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = 12;
        Assert.Equal(12, VerifiedCommercialTenant.Resolve(request));
        request.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = "999";
        Assert.Null(VerifiedCommercialTenant.Resolve(request));
        request.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = 0;
        Assert.Null(VerifiedCommercialTenant.Resolve(request));
    }
}
