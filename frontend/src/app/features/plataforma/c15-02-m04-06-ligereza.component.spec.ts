import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { C1502RevisionArquitecturaComponent, C1502_SUPERFICIES_FUENTE, type C1502Superficie } from './c15-02-m04-01-revision-arquitectura.component';

const platform: PlataformaSesion = { nombreUsuario: 'Admin', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true };
const limited: readonly C1502Superficie[] = [
  { id: 'hosting', nombre: 'Hosting', objetivo: 'Referencia local, no proveedor' },
  { id: 'database', nombre: 'MySQL', objetivo: 'No consultar producción' }
];
async function fixtureWithDeferredSource(session: ReturnType<typeof signal<PlataformaSesion | null>>) {
  let finish!: (items: readonly C1502Superficie[]) => void;
  const deferred = new Promise<readonly C1502Superficie[]>(resolve => { finish = resolve; });
  TestBed.resetTestingModule();
  await TestBed.configureTestingModule({
    imports: [C1502RevisionArquitecturaComponent],
    providers: [
      provideRouter([]),
      { provide: PlataformaSesionService, useValue: { sesion: session.asReadonly() } },
      { provide: C1502_SUPERFICIES_FUENTE, useValue: () => deferred }
    ]
  }).compileComponents();
  const fixture = TestBed.createComponent(C1502RevisionArquitecturaComponent);
  fixture.detectChanges();
  return { fixture, finish, root: fixture.nativeElement as HTMLElement };
}
describe('C15-02.M04.06 carga progresiva y fallo cerrado', () => {
  it('no presenta tarjetas durante carga; al resolver y revocar oculta todo', async () => {
    const session = signal<PlataformaSesion | null>(platform);
    const { fixture, finish, root } = await fixtureWithDeferredSource(session);
    expect(root.querySelector('[data-testid="architecture-loading"]')).not.toBeNull();
    expect(root.querySelectorAll('[data-testid="architecture-item"]')).toHaveLength(0);
    finish(limited);
    await fixture.whenStable();
    fixture.detectChanges();
    expect(root.querySelectorAll('[data-testid="architecture-item"]')).toHaveLength(2);
    expect(root.querySelector('[data-testid="architecture-loaded"]')).not.toBeNull();
    session.set(null);
    fixture.detectChanges();
    expect(root.querySelectorAll('[data-testid="architecture-item"]')).toHaveLength(0);
    expect(root.querySelector('[data-testid="architecture-denied"]')).not.toBeNull();
  });
  it('revocación antes de una respuesta tardía descarta contenido de fuente', async () => {
    const session = signal<PlataformaSesion | null>(platform);
    const { fixture, finish, root } = await fixtureWithDeferredSource(session);
    expect(root.querySelector('[data-testid="architecture-loading"]')).not.toBeNull();
    session.set(null);
    fixture.detectChanges();
    finish(limited);
    await fixture.whenStable();
    fixture.detectChanges();
    expect(root.querySelectorAll('[data-testid="architecture-item"]')).toHaveLength(0);
    expect(root.querySelector('[data-testid="architecture-denied"]')).not.toBeNull();
    expect(root.textContent).not.toContain('No consultar producción');
  });
});
