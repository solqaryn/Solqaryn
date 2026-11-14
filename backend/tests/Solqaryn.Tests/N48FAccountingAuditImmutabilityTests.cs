using System.Reflection;
using Solqaryn.Application.Interfaces;
using Solqaryn.Infrastructure.Services;
using Solqaryn.Domain.Entities.Contabilidad;
using Xunit;

namespace Solqaryn.Tests;

public class N48FAccountingAuditImmutabilityTests
{
    [Fact]
    public void AsientoContableWriter_Implements_Strict_CreateOnly_Audit_Contract()
    {
        var writerType = typeof(AsientoContableWriter);
        var createMethod = writerType.GetMethod("CreateAsync", BindingFlags.Public | BindingFlags.Instance);
        var updateMethod = writerType.GetMethod("UpdateAsync", BindingFlags.Public | BindingFlags.Instance) ?? writerType.GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
        var deleteMethod = writerType.GetMethod("DeleteAsync", BindingFlags.Public | BindingFlags.Instance) ?? writerType.GetMethod("Delete", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(createMethod);
        Assert.Null(updateMethod);
        Assert.Null(deleteMethod);
    }

    [Fact]
    public void IAsientoContableWriter_Exposes_Strict_CreateOnly_Interface()
    {
        var interfaceType = typeof(IAsientoContableWriter);
        Assert.NotNull(interfaceType.GetMethod("CreateAsync", BindingFlags.Public | BindingFlags.Instance));
        Assert.Null(interfaceType.GetMethod("UpdateAsync", BindingFlags.Public | BindingFlags.Instance));
        Assert.Null(interfaceType.GetMethod("DeleteAsync", BindingFlags.Public | BindingFlags.Instance));
    }

    [Fact]
    public void AsientoContable_Detalles_Is_ReadOnly_From_Outside()
    {
        var detallesProp = typeof(AsientoContable).GetProperty("Detalles");
        Assert.NotNull(detallesProp);
        Assert.True(typeof(IReadOnlyCollection<AsientoDetalle>).IsAssignableFrom(detallesProp!.PropertyType));
    }
}
