import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';
import { AuditoriaOperativaV5Component } from './auditoria-operativa-v5.component';

describe('C15-05.M04.03 branding tipado de auditoría operativa', () => {
  async function montar(autorizada: boolean) {
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [AuditoriaOperativaV5Component],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: {
        sesion: signal(autorizada ? {
          nombreUsuario: 'sintetico', rolNombre: 'Super Admin',
          ambito: 'Plataforma', esAdministrador: true
        } : null),
        obtener: vi.fn()
      } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(AuditoriaOperativaV5Component);
    fixture.detectChanges();
    return fixture;
  }

  it('expone solamente marca institucional segura a sesión autorizada', async () => {
    const fixture = await montar(true);
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('.audit-branding')?.textContent).toContain('SOLQARYN · Auditoría operativa');
    expect(root.querySelectorAll('.audit-branding form, .audit-branding input, .audit-branding button')).toHaveLength(0);
    expect(root.querySelector('.audit-branding')?.getAttribute('style')).toBeNull();
  });
  it('cambia a marca neutra sin retener texto institucional', async () => {
    const fixture = await montar(true);
    fixture.componentRef.setInput('modoIdentidad', 'neutra');
    fixture.detectChanges();
    const brand = (fixture.nativeElement as HTMLElement).querySelector('.audit-branding');
    expect(brand?.classList.contains('audit-branding--neutral')).toBe(true);
    expect(brand?.textContent).toContain('Interfaz neutra');
    expect(brand?.textContent).not.toContain('SOLQARYN · Auditoría operativa');
  });
  it('payload de marca no permitido falla hacia interfaz neutra, no inyecta HTML/CSS', async () => {
    const fixture = await montar(true);
    fixture.componentRef.setInput('modoIdentidad', '<img src=x onerror=alert(1)>');
    fixture.detectChanges();
    const brand = (fixture.nativeElement as HTMLElement).querySelector('.audit-branding');
    expect(brand?.textContent).toContain('Interfaz neutra');
    expect(brand?.querySelector('img')).toBeNull();
    expect(brand?.getAttribute('style')).toBeNull();
  });
  it('no publica branding sin sesión verificada aunque se solicite marca institucional', async () => {
    const fixture = await montar(false);
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('.audit-branding')).toBeNull();
    expect(root.textContent).toContain('Sin permiso de plataforma confirmado');
  });
});
