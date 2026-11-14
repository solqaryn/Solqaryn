import { TestBed } from '@angular/core/testing';
import { throwError } from 'rxjs';
import { EstadosFinancierosComponent } from './estados-financieros.component';
import { EstadoFinancieroService } from '../../services/estado-financiero.service';
import { TipoEstadoFinanciero } from '../../core/models/estado-financiero.model';

describe('EstadosFinancierosComponent unauthorized response contract', () => {
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
    component.form.patchValue({
      tipo: TipoEstadoFinanciero.BalanceGeneral,
      modo: 'periodo',
      periodoContableId: 7,
    });
  });

  it('sanitizes 401 details, clears stale data and leaves loading', () => {
    component.resultado = { lineas: [] } as any;
    generarSpy.mockReturnValue(
      throwError(() => ({
        status: 401,
        error: {
          detail: 'ORA-01017 invalid username/password; host=db-internal:1521',
          stackTrace: 'Sensitive.Internal.StackTrace',
        },
      })),
    );

    component.generar();

    expect(generarSpy).toHaveBeenCalledTimes(1);
    expect(component.resultado).toBeNull();
    expect(component.loading).toBe(false);
    expect(component.error).toBe('No fue posible generar el estado financiero. Intente nuevamente.');
    expect(component.error).not.toContain('ORA-01017');
    expect(component.error).not.toContain('db-internal');
    expect(component.error).not.toContain('StackTrace');
  });
});
