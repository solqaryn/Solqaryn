import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';
import { GobiernoCambiosV5Component } from './gobierno-cambios-v5.component';

describe('C00-04.M04.03 branding tipado de plataforma', () => {
  async function montar(autorizada: boolean) {
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [GobiernoCambiosV5Component],
      providers: [
        provideRouter([]),
        { provide: PlataformaSesionService, useValue: {
          sesion: signal(autorizada ? {
            nombreUsuario: 'plataforma-test', rolNombre: 'Super Admin',
            ambito: 'Plataforma', esAdministrador: true
          } : null),
          obtener: vi.fn()
        } }
      ]
    }).compileComponents();
    const fixture = TestBed.createComponent(GobiernoCambiosV5Component);
    fixture.detectChanges();
    return fixture;
  }
  it('muestra la marca institucional permitida y sólo controles de consulta', async () => {
    const fixture = await montar(true);
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('.brand-context')?.textContent).toContain('SOLQARYN · Plataforma');
    expect(root.querySelector('.brand-context')?.textContent).toContain('controles visuales permitidos');
    expect(root.querySelectorAll('form, input, button:not([aria-label="Revalidar sesión de plataforma"])')).toHaveLength(0);
  });
  it('cambia a interfaz neutra sin reutilizar el branding anterior', async () => {
    const fixture = await montar(true);
    fixture.componentRef.setInput('modoMarca', 'neutra');
    fixture.detectChanges();
    const marca = (fixture.nativeElement as HTMLElement).querySelector('.brand-context');
    expect(marca?.classList.contains('brand-context--neutral')).toBe(true);
    expect(marca?.textContent).toContain('Interfaz neutra');
    expect(marca?.textContent).not.toContain('SOLQARYN · Plataforma');
  });
  it('rechaza payload visual arbitrario sin HTML o CSS dinámico', async () => {
    const fixture = await montar(true);
    fixture.componentRef.setInput('modoMarca', '<img src=x onerror=alert(1)>');
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('.brand-context')?.textContent).toContain('Interfaz neutra');
    expect(root.querySelector('.brand-context img')).toBeNull();
    expect(root.querySelector('.brand-context')?.getAttribute('style')).toBeNull();
  });
  it('no presenta branding protegido cuando el servidor no confirmo la sesion', async () => {
    const fixture = await montar(false);
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('.brand-context')).toBeNull();
    expect(root.textContent).toContain('Sin permiso de plataforma confirmado');
  });
});
