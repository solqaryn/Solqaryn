using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.Infrastructure.Services;

public sealed class EmpresaDocumentoIdentity(AppDbContext db) : IEmpresaDocumentoIdentity
{
    public async Task<EmpresaConfiguracion> ObtenerAsync(bool escritura, CancellationToken cancellationToken = default)
    {
        var id = escritura ? db.AuthorizedEmpresaId : db.CommercialReadEmpresaId;
        if (id is not > 0)
            throw new ForbiddenAccessException("El documento requiere una empresa autorizada.");

        var empresa = await db.Set<Empresa>().AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == id && e.Activa, cancellationToken)
            ?? throw new ForbiddenAccessException("La empresa del documento no está disponible.");
        var moneda = await db.Set<ConfigEmpresa>().AsNoTracking()
            .Where(c => c.EmpresaId == id).Select(c => c.Moneda).SingleOrDefaultAsync(cancellationToken);

        // No importar identidad, contactos ni textos de otra empresa. El correo
        // operativo de envío tampoco constituye una dirección comercial pública.
        return new EmpresaConfiguracion
        {
            NombreComercial = empresa.Nombre,
            RTN = empresa.Rtn,
            Direccion = empresa.Direccion,
            LogoUrl = empresa.LogoUrl,
            Moneda = moneda ?? "HNL",
            MostrarCopyright = false,
            Copyright = string.Empty
        };
    }
}
