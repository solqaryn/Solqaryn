import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { EstadosFinancierosComponent } from './estados-financieros.component';
import { EstadoFinancieroService } from '../../services/estado-financiero.service';
import { TipoEstadoFinanciero } from '../../core/models/estado-financiero.model';

describe('EstadosFinancierosComponent core regression contract', () => {
  let component: EstadosFinancierosComponent;
  let generarSpy: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    generarSpy = vi.fn();

    TestBed.configureTestingModule({
      imports: [EstadosFinancierosComponent],
      providers: [
        {
          provide: EstadoFinancieroService,
          useValue: { generar: generarSpy },
        },
      ],
    });

    const fixture = TestBed.createComponent(EstadosFinancierosComponent);
    component = fixture.componentInstance;
  });

  it('generates a period statement, replaces stale state and leaves loading deterministically', () => {
    const estado = {
      lineas: [
        { cuentaContableId: 101, cuenta: 'Caja', saldo: 1250 },
        { cuentaContableId: 202, cuenta: 'Proveedores', saldo: 400 },
      ],
    } as any;

    component.resultado = { lineas: [{ cuentaContableId: 999 }] } as any;
    component.error = 'stale error';
    component.form.patchValue({
      tipo: TipoEstadoFinanciero.BalanceGeneral,
      modo: 'periodo',
      periodoContableId: 7,
    });
    generarSpy.mockReturnValue(of({ success: true, data: estado }));

    component.generar();

    expect(generarSpy).toHaveBeenCalledTimes(1);
    expect(generarSpy).toHaveBeenCalledWith(
      TipoEstadoFinanciero.BalanceGeneral,
      { periodoContableId: 7 },
    );
    expect(component.resultado).toBe(estado);
    expect(component.error).toBe('');
    expect(component.loading).toBe(false);
  });

  it('keeps invalid period input fail-closed without calling the API or retaining stale output', () => {
    component.resultado = { lineas: [{ cuentaContableId: 999 }] } as any;
    component.form.patchValue({
      tipo: TipoEstadoFinanciero.BalanceGeneral,
      modo: 'periodo',
      periodoContableId: 0,
    });

    component.generar();

    expect(generarSpy).not.toHaveBeenCalled();
    expect(component.resultado).toBeNull();
    expect(component.error).toBe('Seleccione un período contable válido.');
    expect(component.loading).toBe(false);
  });

  it('switches query mode by clearing mutually exclusive criteria and stale result state', () => {
    component.resultado = { lineas: [{ cuentaContableId: 999 }] } as any;
    component.error = 'stale error';
    component.form.patchValue({
      modo: 'rango',
      periodoContableId: 9,
      fechaDesde: '2026-08-01',
      fechaHasta: '2026-08-31',
    });

    component.cambiarModo();

    expect(component.form.controls.periodoContableId.value).toBeNull();
    expect(component.form.controls.fechaDesde.value).toBe('2026-08-01');
    expect(component.form.controls.fechaHasta.value).toBe('2026-08-31');
    expect(component.resultado).toBeNull();
    expect(component.error).toBe('');
  });
});
