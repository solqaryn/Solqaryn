using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N16AuditoriaCorrelationSecurityTests
{
    [Fact]
    public async Task Auditoria_UsaTraceIdentifierSaneado_YNoHeaderBrutoDelCliente()
    {
        var repository = new AuditoriaRepositoryFake();
        var currentUser = new CurrentUserFake();
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "corr-normalizado-77"
        };
        context.Request.Headers["X-Correlation-ID"] = "<script>correlacion-controlada-por-cliente</script>";
        context.Request.Headers["User-Agent"] = "N16-security-test";
        var accessor = new HttpContextAccessor { HttpContext = context };
        var service = new AuditoriaService(
            repository,
            currentUser,
            accessor,
            NullLogger<AuditoriaService>.Instance);

        await service.RegistrarEstrictoAsync(
            ModuloSistema.MovimientosInventario,
            AccionPermiso.Confirmar,
            "Transferencia despachada",
            referenciaId: 77,
            entidad: nameof(TransferenciaInventario));

        var registro = Assert.IsType<RegistroAuditoria>(repository.UltimoRegistro);
        Assert.Equal("corr-normalizado-77", registro.CorrelationId);
        Assert.DoesNotContain("script", registro.CorrelationId!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(77, registro.ReferenciaId);
        Assert.Equal(ModuloSistema.MovimientosInventario, registro.Modulo);
        Assert.Equal(AccionPermiso.Confirmar, registro.Accion);
        Assert.True(repository.Guardado);
    }

    private sealed class AuditoriaRepositoryFake : IAuditoriaRepository
    {
        public RegistroAuditoria? UltimoRegistro { get; private set; }
        public bool Guardado { get; private set; }

        public Task AddAsync(RegistroAuditoria registro)
        {
            UltimoRegistro = registro;
            return Task.CompletedTask;
        }

        public Task<(List<RegistroAuditoria> Items, int TotalCount)> GetFilteredAsync(AuditoriaFiltroDto filtro) =>
            Task.FromResult((new List<RegistroAuditoria>(), 0));

        public Task<bool> SaveChangesAsync()
        {
            Guardado = true;
            return Task.FromResult(true);
        }
    }

    private sealed class CurrentUserFake : ICurrentUserService
    {
        public int? UsuarioId => 9001;
        public string? NombreUsuario => "n16-security";
        public string? NombreCompleto => "N1.6 Security Test";
        public int? RolId => 10;
        public bool EsAdministrador => false;
        public bool EstaAutenticado => true;
    }
}
