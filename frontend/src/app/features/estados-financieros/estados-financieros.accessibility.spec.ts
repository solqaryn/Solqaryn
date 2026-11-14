import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { EstadosFinancierosComponent } from './estados-financieros.component';
import { EstadoFinancieroService } from '../../services/estado-financiero.service';

describe('EstadosFinancierosComponent accessibility contract', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [EstadosFinancierosComponent],
      providers: [
        {
          provide: EstadoFinancieroService,
          useValue: { generar: vi.fn(() => of({ success: true, data: null })) },
        },
      ],
    });
  });

  it('exposes the page heading as the accessible section label', () => {
    const fixture = TestBed.createComponent(EstadosFinancierosComponent);
    fixture.detectChanges();

    const section = fixture.nativeElement.querySelector('section.financial-page') as HTMLElement;
    const title = fixture.nativeElement.querySelector('#financial-title') as HTMLElement;

    expect(section).toBeTruthy();
    expect(title?.textContent?.trim()).toBe('Estados financieros');
    expect(section.getAttribute('aria-labelledby')).toBe('financial-title');
  });

  it('keeps filter controls natively labelled and the submit action keyboard-addressable', () => {
    const fixture = TestBed.createComponent(EstadosFinancierosComponent);
    fixture.detectChanges();

    const form = fixture.nativeElement.querySelector('form.filters') as HTMLFormElement;
    const labels = Array.from(form.querySelectorAll('label')).map(label => label.textContent?.trim() ?? '');
    const button = form.querySelector('button[type="submit"]') as HTMLButtonElement;
    const fieldset = form.querySelector('fieldset') as HTMLFieldSetElement;
    const legend = fieldset?.querySelector('legend') as HTMLElement;

    expect(labels.some(label => label.includes('Estado financiero'))).toBe(true);
    expect(labels.some(label => label.includes('Período contable'))).toBe(true);
    expect(labels.some(label => label.includes('Rango de fechas'))).toBe(true);
    expect(legend?.textContent?.trim()).toBe('Filtro');
    expect(button).toBeTruthy();
    expect(button.disabled).toBe(false);
    expect(button.textContent?.trim()).toBe('Generar estado financiero');
  });

  it('announces validation errors and loading state through explicit live semantics', () => {
    const fixture = TestBed.createComponent(EstadosFinancierosComponent);
    const component = fixture.componentInstance;

    component.error = 'Error controlado';
    component.loading = true;
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.status.error') as HTMLElement;
    const loading = fixture.nativeElement.querySelector('.status[aria-live="polite"]') as HTMLElement;

    expect(error?.getAttribute('role')).toBe('alert');
    expect(error?.textContent?.trim()).toBe('Error controlado');
    expect(loading?.getAttribute('aria-live')).toBe('polite');
    expect(loading?.textContent).toContain('Consultando información financiera');
  });

  it('provides table caption and column header scope for financial results', () => {
    const fixture = TestBed.createComponent(EstadosFinancierosComponent);
    const component = fixture.componentInstance;

    component.resultado = {
      nombre: 'Balance general',
      fechaInicio: '2026-09-01',
      fechaFin: '2026-09-30',
      lineas: [
        {
          cuentaContableId: 1,
          cuentaCodigo: '1101',
          cuentaNombre: 'Caja',
          saldo: 100,
          esRaiz: false,
        },
      ],
      totales: [],
    } as any;
    fixture.detectChanges();

    const caption = fixture.nativeElement.querySelector('table caption') as HTMLElement;
    const headers = Array.from(fixture.nativeElement.querySelectorAll('table th')) as HTMLElement[];

    expect(caption?.textContent?.trim()).toBe('Detalle de Balance general');
    expect(headers).toHaveLength(3);
    expect(headers.every(header => header.getAttribute('scope') === 'col')).toBe(true);
  });
});
