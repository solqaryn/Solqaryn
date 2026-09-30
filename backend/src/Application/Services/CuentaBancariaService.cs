using Solqaryn.Application.DTOs.Bancos;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities.Bancos;
using Solqaryn.Application.Bancos;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public sealed class CuentaBancariaService : ICuentaBancariaService
{
    private readonly ICuentaBancariaRepository _repository;
    private readonly IAuditoriaService _auditoria;

    public CuentaBancariaService(ICuentaBancariaRepository repository, IAuditoriaService auditoria)
    {
        _repository = repository;
        _auditoria = auditoria;
    }

    public async Task<CuentaBancariaDto?> GetByIdAsync(int id)
    {
        var cuenta = await _repository.GetByIdAsync(id);
        if (cuenta == null) return null;

        return MapToDto(cuenta);
    }

    public async Task<CuentaBancariaPage<CuentaBancariaDto>> GetAllAsync(CuentaBancariaQueryFilter filter)
    {
        var page = await _repository.GetAllAsync(filter);
        var dtoItems = page.Items.Select(MapToDto).ToList();
        return new CuentaBancariaPage<CuentaBancariaDto>(dtoItems, page.Page, page.PageSize, page.TotalCount);
    }

    public async Task<List<CuentaBancariaDto>> GetActivasAsync()
    {
        var cuentas = await _repository.GetActivasAsync();
        return cuentas.Select(MapToDto).ToList();
    }

    public async Task<CuentaBancariaDto> AddAsync(CreateCuentaBancariaDto dto)
    {
        var cuenta = new CuentaBancaria(
            dto.BancoId,
            dto.Nombre,
            dto.NumeroCuenta,
            dto.Moneda,
            dto.SaldoInicial);

        await _repository.AddAsync(cuenta);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Finanzas,
            AccionPermiso.Crear,
            $"Creó la cuenta bancaria '{cuenta.Nombre}'.",
            cuenta.Id,
            entidad: "CuentaBancaria",
            valoresNuevos: new { cuenta.BancoId, cuenta.Nombre, cuenta.Moneda, cuenta.SaldoInicial, cuenta.Estado });

        return MapToDto(cuenta);
    }

    public async Task ActivarAsync(int id)
    {
        var cuenta = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"No se encontró la cuenta con Id {id}.");

        cuenta.Activar();
        _repository.Update(cuenta);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Finanzas,
            AccionPermiso.Activar,
            $"Activó la cuenta bancaria '{cuenta.Nombre}'.",
            cuenta.Id,
            entidad: "CuentaBancaria");
    }

    public async Task DesactivarAsync(int id)
    {
        var cuenta = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"No se encontró la cuenta con Id {id}.");

        cuenta.Desactivar();
        _repository.Update(cuenta);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Finanzas,
            AccionPermiso.Desactivar,
            $"Desactivó la cuenta bancaria '{cuenta.Nombre}'.",
            cuenta.Id,
            entidad: "CuentaBancaria");
    }

    public async Task UpdateAsync(int id, UpdateCuentaBancariaDto dto)
    {
        var cuenta = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"No se encontró la cuenta con Id {id}.");

        var nombreAnterior = cuenta.Nombre;
        cuenta.UpdateNombre(dto.Nombre);
        _repository.Update(cuenta);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Finanzas,
            AccionPermiso.Editar,
            $"Editó la cuenta bancaria '{cuenta.Nombre}'.",
            cuenta.Id,
            entidad: "CuentaBancaria",
            valoresAnteriores: new { Nombre = nombreAnterior },
            valoresNuevos: new { cuenta.Nombre });
    }

    private static CuentaBancariaDto MapToDto(CuentaBancaria cuenta) => new()
    {
        Id = cuenta.Id,
        BancoId = cuenta.BancoId,
        Nombre = cuenta.Nombre,
        NumeroCuenta = cuenta.NumeroCuenta,
        Moneda = cuenta.Moneda,
        SaldoInicial = cuenta.SaldoInicial,
        Estado = cuenta.Estado
    };
}
