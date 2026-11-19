import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { routes } from '../../app.routes';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { C1502RevisionArquitecturaComponent } from './c15-02-m04-01-revision-arquitectura.component';

describe('C15-02.M04.01 ruta privada de revisión arquitectónica', () => {
  it('declara ruta lazy con guard global exacto sin duplicación', async () => {
    const found = routes.filter(x => x.path === 'plataforma/revision-arquitectura-v20');
    expect(found).toHaveLength(1);
    expect(found[0].canActivate).toEqual([plataformaGuard]);
    expect(await (found[0].loadComponent as () => Promise<unknown>)()).toBe(C1502RevisionArquitecturaComponent);
  });

  it('oculta inventario por defecto y ante revocación, no contiene formularios ni escritura', async () => {
    const sesion = signal<PlataformaSesion | null>(null);
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [C1502RevisionArquitecturaComponent],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: { sesion: sesion.asReadonly() } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(C1502RevisionArquitecturaComponent);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelectorAll('[data-testid="architecture-item"]')).toHaveLength(0);
    expect(root.querySelector('[data-testid="architecture-denied"]')).not.toBeNull();
    sesion.set({ nombreUsuario: 'Admin', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true });
    fixture.detectChanges();
    expect(root.querySelectorAll('[data-testid="architecture-item"]')).toHaveLength(5);
    expect(root.textContent).toContain('NO_VERIFICADO');
    expect(root.querySelectorAll('button,form,input,select,textarea')).toHaveLength(0);
    sesion.set(null);
    fixture.detectChanges();
    expect(root.querySelectorAll('[data-testid="architecture-item"]')).toHaveLength(0);
  });
});
