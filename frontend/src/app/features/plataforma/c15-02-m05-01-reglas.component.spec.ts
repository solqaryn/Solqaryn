import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { C1502RevisionArquitecturaComponent, C1502_SUPERFICIES_FUENTE, type C1502Superficie } from './c15-02-m04-01-revision-arquitectura.component';

const platform: PlataformaSesion = { nombreUsuario: 'Admin', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true };
const one: C1502Superficie = { id: 'hosting', nombre: 'Hosting', objetivo: 'Referencia local no certificada' };

async function createView(source: () => Promise<readonly C1502Superficie[]>) {
  TestBed.resetTestingModule();
  const session = signal<PlataformaSesion | null>(platform);
  await TestBed.configureTestingModule({
    imports: [C1502RevisionArquitecturaComponent],
    providers: [
      provideRouter([]),
      { provide: PlataformaSesionService, useValue: { sesion: session.asReadonly() } },
      { provide: C1502_SUPERFICIES_FUENTE, useValue: source }
    ]
  }).compileComponents();
  const fixture = TestBed.createComponent(C1502RevisionArquitecturaComponent);
  fixture.detectChanges();
  await fixture.whenStable();
  fixture.detectChanges();
  return { fixture, root: fixture.nativeElement as HTMLElement, session };
}

describe('C15-02.M05.01 reglas unitarias de fuente local no privilegiada', () => {
  it('acepta exactamente 16 identificadores distintos y conserva salida no verificada', async () => {
    const items: C1502Superficie[] = Array.from({ length: 16 }, (_, index) => ({
      id: 'local-' + index, nombre: 'Referencia ' + index, objetivo: 'Solo lectura, sin proveedor'
    }));
    const { root } = await createView(async () => items);
    expect(root.querySelectorAll('[data-testid="architecture-item"]')).toHaveLength(16);
    expect(root.querySelectorAll('.pending')).toHaveLength(16);
    expect(root.textContent).toContain('NO_VERIFICADO');
    expect(root.querySelector('[data-testid="architecture-error"]')).toBeNull();
  });

  it('rechaza 17 entradas como límite excedido sin filtrar un listado parcial', async () => {
    const items: C1502Superficie[] = Array.from({ length: 17 }, (_, index) => ({
      id: 'local-' + index, nombre: 'SECRETO_NUNCA_RENDERIZAR_' + index, objetivo: 'No visible'
    }));
    const { root } = await createView(async () => items);
    expect(root.querySelector('[data-testid="architecture-error"]')).not.toBeNull();
    expect(root.querySelectorAll('[data-testid="architecture-item"]')).toHaveLength(0);
    expect(root.textContent).not.toContain('SECRETO_NUNCA_RENDERIZAR');
  });

  it('bloquea id duplicado y recupera por reintento con una respuesta corregida', async () => {
    let calls = 0;
    const { fixture, root } = await createView(async () => {
      calls++;
      return calls === 1
        ? [one, { ...one, nombre: 'DATO_INTERNO_NO_FILTRAR' }]
        : [one];
    });
    expect(root.querySelector('[data-testid="architecture-error"]')).not.toBeNull();
    expect(root.querySelectorAll('[data-testid="architecture-item"]')).toHaveLength(0);
    expect(root.textContent).not.toContain('DATO_INTERNO_NO_FILTRAR');
    fixture.componentInstance.reintentarLectura();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(calls).toBe(2);
    expect(root.querySelector('[data-testid="architecture-error"]')).toBeNull();
    expect(root.querySelectorAll('[data-testid="architecture-item"]')).toHaveLength(1);
  });

  it('rechaza identificadores malformados sin mostrar datos de excepción o PII', async () => {
    const { root } = await createView(async () => [
      { id: '<script>alert(1)</script>', nombre: 'PII_NO_MOSTRAR', objetivo: 'No visible' }
    ]);
    expect(root.querySelector('[data-testid="architecture-error"]')).not.toBeNull();
    expect(root.querySelectorAll('[data-testid="architecture-item"]')).toHaveLength(0);
    expect(root.textContent).not.toContain('PII_NO_MOSTRAR');
    expect(root.textContent).not.toContain('alert(1)');
  });
});
