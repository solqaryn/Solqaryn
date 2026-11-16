import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { routes } from '../../app.routes';
import { AuditoriaTenancyV5Component } from './auditoria-tenancy-v5.component';

describe('C15-03.M04.01 - guía privada de seguridad y tenancy', () => {
  const autorizado: PlataformaSesion = {
    nombreUsuario: 'Plataforma', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true
  };
  async function montar(valor: PlataformaSesion | null) {
    TestBed.resetTestingModule();
    const sesion = signal(valor);
    await TestBed.configureTestingModule({
      imports: [AuditoriaTenancyV5Component],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: { sesion } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(AuditoriaTenancyV5Component);
    fixture.detectChanges();
    return { fixture, sesion };
  }
  it('ruta singular diferida y protegida exclusivamente por plataformaGuard', async () => {
    const rutas = routes.filter(r => r.path === 'plataforma/auditoria-tenancy-v5');
    expect(rutas).toHaveLength(1);
    expect(rutas[0].canActivate).toEqual([plataformaGuard]);
    expect(await rutas[0].loadComponent?.()).toBe(AuditoriaTenancyV5Component);
  });
  it('sin sesión no renderiza guía, controles ni valores de autorización', async () => {
    const { fixture } = await montar(null);
    expect(fixture.componentInstance.accesoVerificado()).toBe(false);
    expect(fixture.nativeElement.querySelectorAll('.fronteras li')).toHaveLength(0);
    expect(fixture.nativeElement.querySelector('.notice')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Guía protegida oculta');
  });
  it('grant de plataforma muestra solo cuatro frentes estáticos no operativos', async () => {
    const { fixture } = await montar(autorizado);
    expect(fixture.componentInstance.accesoVerificado()).toBe(true);
    expect(fixture.nativeElement.querySelectorAll('.fronteras li')).toHaveLength(4);
    expect(fixture.nativeElement.querySelectorAll('button, form, input, textarea')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).toContain('NO_MEDIDO');
  });
  it('grant inválido por ambito empresarial se deniega', async () => {
    const falso = { ...autorizado, ambito: 'Empresa' } as unknown as PlataformaSesion;
    const { fixture } = await montar(falso);
    expect(fixture.componentInstance.accesoVerificado()).toBe(false);
    expect(fixture.nativeElement.querySelectorAll('.fronteras li')).toHaveLength(0);
  });
  it('revocación reactiva oculta contenido antes expuesto', async () => {
    const { fixture, sesion } = await montar(autorizado);
    expect(fixture.nativeElement.querySelectorAll('.fronteras li')).toHaveLength(4);
    sesion.set(null);
    fixture.detectChanges();
    expect(fixture.componentInstance.accesoVerificado()).toBe(false);
    expect(fixture.nativeElement.querySelectorAll('.fronteras li')).toHaveLength(0);
  });
});
