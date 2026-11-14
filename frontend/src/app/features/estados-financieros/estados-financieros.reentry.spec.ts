import { TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { EstadosFinancierosComponent } from './estados-financieros.component';
import { EstadoFinancieroService } from '../../services/estado-financiero.service';
import { TipoEstadoFinanciero } from '../../core/models/estado-financiero.model';

describe('EstadosFinancierosComponent request reentry contract', () => {
  it('does not issue a second request while generation is already in progress', () => {
    const response$ = new Subject<any>();
    const generar = vi.fn(() => response$.asObservable());

    TestBed.configureTestingModule({
      imports: [EstadosFinancierosComponent],
      providers: [{ provide: EstadoFinancieroService, useValue: { generar } }],
    });

    const fixture = TestBed.createComponent(EstadosFinancierosComponent);
    const component = fixture.componentInstance;
    component.form.patchValue({
      tipo: TipoEstadoFinanciero.BalanceGeneral,
      modo: 'periodo',
      periodoContableId: 1,
    });

    component.generar();
    component.generar();

    expect(component.loading).toBe(true);
    expect(generar).toHaveBeenCalledTimes(1);

    response$.complete();
    expect(component.loading).toBe(false);
  });
});
