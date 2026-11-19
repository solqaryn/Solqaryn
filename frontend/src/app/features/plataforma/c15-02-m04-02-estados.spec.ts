import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { C1502RevisionArquitecturaComponent, C1502_SUPERFICIES_FUENTE, type C1502Superficie } from './c15-02-m04-01-revision-arquitectura.component';

describe('C15-02.M04.02 estados protegidos de referencias locales', () => {
  const admin: PlataformaSesion = { nombreUsuario: 'Admin', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true };
  async function montar(fuente: () => Promise<readonly C1502Superficie[]>, usuario: PlataformaSesion | null = admin) {
    const sesion = signal<PlataformaSesion | null>(usuario);
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [C1502RevisionArquitecturaComponent],
      providers: [provideRouter([]),
        { provide: PlataformaSesionService, useValue: { sesion: sesion.asReadonly() } },
        { provide: C1502_SUPERFICIES_FUENTE, useValue: fuente }]
    }).compileComponents();
    const fixture = TestBed.createComponent(C1502RevisionArquitecturaComponent);
    fixture.detectChanges();
    return { fixture, sesion, root: fixture.nativeElement as HTMLElement };
  }
  const item = { id: 'hosting', nombre: 'Hosting', objetivo: 'Solo evidencia local.' };

  it('carga sin filtrar contenido y distingue vacío de éxito', async () => {
    let resolver!: (items: readonly C1502Superficie[]) => void;
    const pendiente = new Promise<readonly C1502Superficie[]>(r => { resolver = r; });
    const { fixture, root } = await montar(() => pendiente);
    expect(root.querySelector('[data-testid="architecture-loading"]')).not.toBeNull();
    expect(root.querySelectorAll('[data-testid="architecture-item"]')).toHaveLength(0);
    resolver([]);
    await fixture.whenStable(); fixture.detectChanges();
    expect(root.querySelector('[data-testid="architecture-empty"]')).not.toBeNull();
    expect(root.querySelectorAll('[data-testid="architecture-item"]')).toHaveLength(0);
  });

  it('éxito solo exhibe referencias validadas sin certificación', async () => {
    const { fixture, root } = await montar(async () => [item]);
    await fixture.whenStable(); fixture.detectChanges();
    expect(root.querySelectorAll('[data-testid="architecture-item"]')).toHaveLength(1);
    expect(root.textContent).toContain('NO_VERIFICADO');
    expect(root.querySelectorAll('form,input,select,textarea')).toHaveLength(0);
  });

  it('error no expone secreto y reintento seguro recupera una referencia', async () => {
    let llamadas = 0;
    const { fixture, root } = await montar(async () => {
      if (++llamadas === 1) throw new Error('PROVIDER_SECRET_NEVER_RENDER');
      return [item];
    });
    await fixture.whenStable(); fixture.detectChanges();
    expect(root.querySelector('[data-testid="architecture-error"]')).not.toBeNull();
    expect(root.textContent).not.toContain('PROVIDER_SECRET_NEVER_RENDER');
    (root.querySelector('button.retry') as HTMLButtonElement).click();
    await fixture.whenStable(); fixture.detectChanges();
    expect(root.querySelectorAll('[data-testid="architecture-item"]')).toHaveLength(1);
  });

  it('rechaza duplicados y elementos malformados sin presentar contenido parcial', async () => {
    const { fixture, root } = await montar(async () => [item, { ...item, nombre: 'Duplicada' }]);
    await fixture.whenStable(); fixture.detectChanges();
    expect(root.querySelector('[data-testid="architecture-error"]')).not.toBeNull();
    expect(root.querySelectorAll('[data-testid="architecture-item"]')).toHaveLength(0);
  });

  it('revocación durante Promise invalida respuesta tardía del administrador', async () => {
    let resolver!: (items: readonly C1502Superficie[]) => void;
    const pendiente = new Promise<readonly C1502Superficie[]>(r => { resolver = r; });
    const { fixture, sesion, root } = await montar(() => pendiente);
    sesion.set(null); fixture.detectChanges();
    resolver([{ ...item, nombre: 'NO_FILTRAR_PRIVADO' }]);
    await fixture.whenStable(); fixture.detectChanges();
    expect(root.querySelectorAll('[data-testid="architecture-item"]')).toHaveLength(0);
    expect(root.textContent).not.toContain('NO_FILTRAR_PRIVADO');
    expect(root.querySelector('[data-testid="architecture-denied"]')).not.toBeNull();
  });

  it('rol Empresa aun marcado administrador jamás inicia el cargador', async () => {
    let invocaciones = 0;
    const usuario = { ...admin, ambito: 'Empresa' } as unknown as PlataformaSesion;
    const { fixture, root } = await montar(async () => { invocaciones++; return [item]; }, usuario);
    await fixture.whenStable(); fixture.detectChanges();
    expect(invocaciones).toBe(0);
    expect(root.querySelectorAll('[data-testid="architecture-item"]')).toHaveLength(0);
    expect(root.querySelector('[data-testid="architecture-denied"]')).not.toBeNull();
  });
});
