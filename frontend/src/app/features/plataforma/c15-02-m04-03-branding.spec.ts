import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import {
  C1502RevisionArquitecturaComponent, C1502_PRESENTACION,
  normalizarC1502Presentacion
} from './c15-02-m04-01-revision-arquitectura.component';

describe('C15-02.M04.03 branding seguro y configuracion dinamica allowlisted', () => {
  const admin: PlataformaSesion = {
    nombreUsuario: 'Admin', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true
  };
  async function mount(input: unknown, user: PlataformaSesion | null = admin) {
    const sesion = signal<PlataformaSesion | null>(user);
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [C1502RevisionArquitecturaComponent],
      providers: [provideRouter([]),
        { provide: PlataformaSesionService, useValue: { sesion: sesion.asReadonly() } },
        { provide: C1502_PRESENTACION, useValue: input }]
    }).compileComponents();
    const fixture = TestBed.createComponent(C1502RevisionArquitecturaComponent);
    fixture.detectChanges();
    return { fixture, root: fixture.nativeElement as HTMLElement, sesion };
  }
  it('aplica solo marca neutra/compacta permitida sin permisos adicionales', async () => {
    const { root } = await mount({ marca: 'neutra', densidad: 'compacta' });
    const surface = root.querySelector('[data-testid="c1502-review"]')!;
    expect(surface.getAttribute('data-marca')).toBe('neutra');
    expect(surface.getAttribute('data-densidad')).toBe('compacta');
    expect(surface.classList.contains('review-compacta')).toBe(true);
    expect(root.querySelectorAll('[data-testid="architecture-item"]')).toHaveLength(5);
    expect(root.textContent).toContain('NO_VERIFICADO');
  });
  it('getter hostil, arreglo y estilo no permitido se normalizan sin ejecutar codigo', () => {
    let reads = 0;
    const hostile = Object.defineProperty({}, 'marca', {
      get() { reads++; throw new Error('PRIVATE_CREDENTIAL'); }
    });
    expect(normalizarC1502Presentacion(hostile)).toEqual({ marca: 'neutra', densidad: 'regular' });
    expect(reads).toBe(1);
    expect(normalizarC1502Presentacion(['institucional'])).toEqual({ marca: 'neutra', densidad: 'regular' });
    expect(normalizarC1502Presentacion({ marca: 'javascript:alert(1)', densidad: 'compacta' }))
      .toEqual({ marca: 'neutra', densidad: 'regular' });
  });
  it('getter solo se lee una vez; una sesion Empresa no accede a tarjetas', async () => {
    let reads = 0;
    const input = {
      get marca() { reads++; return reads === 1 ? 'institucional' : 'malicioso'; },
      densidad: 'regular'
    };
    const x = normalizarC1502Presentacion(input);
    expect(x.marca).toBe('institucional');
    expect(reads).toBe(1);
    const empresa = { ...admin, ambito: 'Empresa' } as unknown as PlataformaSesion;
    const { root } = await mount({ marca: 'neutra', densidad: 'compacta' }, empresa);
    expect(root.querySelectorAll('[data-testid="architecture-item"]')).toHaveLength(0);
    expect(root.querySelector('[data-testid="architecture-denied"]')).not.toBeNull();
  });
});
