import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { routes } from '../../app.routes';
import { ObservabilidadPrivadaV5Component } from './observabilidad-privada-v5.component';

describe('C11-04.M04.01 - observabilidad privada', () => {
  const admin: PlataformaSesion = {
    nombreUsuario: 'Plataforma', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true
  };
  async function montar(value: PlataformaSesion | null) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(value);
    await TestBed.configureTestingModule({
      imports: [ObservabilidadPrivadaV5Component],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: { sesion } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(ObservabilidadPrivadaV5Component);
    fixture.detectChanges();
    return { fixture, sesion };
  }
  it('ruta única lazy con guard de plataforma', async () => {
    const matches = routes.filter(x => x.path === 'plataforma/observabilidad-v5');
    expect(matches).toHaveLength(1);
    expect(matches[0].canActivate).toEqual([plataformaGuard]);
    expect(await matches[0].loadComponent?.()).toBe(ObservabilidadPrivadaV5Component);
  });
  it('anónimo no muestra detalles privados', async () => {
    const { fixture } = await montar(null);
    expect(fixture.componentInstance.verificado()).toBe(false);
    expect(fixture.nativeElement.querySelectorAll('.obs-points li')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).toContain('Observabilidad privada oculta');
  });
  it('plataforma autorizada muestra cuatro frentes sin acciones operativas', async () => {
    const { fixture } = await montar(admin);
    expect(fixture.componentInstance.verificado()).toBe(true);
    expect(fixture.nativeElement.querySelectorAll('.obs-points li')).toHaveLength(4);
    expect(fixture.nativeElement.querySelectorAll('button, form, input, textarea')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).toContain('NO_MEDIDO');
  });
  it('sesión empresarial forjada no habilita observabilidad', async () => {
    const { fixture } = await montar({ ...admin, ambito: 'Empresa' } as unknown as PlataformaSesion);
    expect(fixture.componentInstance.verificado()).toBe(false);
    expect(fixture.nativeElement.querySelectorAll('.obs-points li')).toHaveLength(0);
  });
  it('revocación oculta los cuatro frentes inmediatamente', async () => {
    const { fixture, sesion } = await montar(admin);
    expect(fixture.nativeElement.querySelectorAll('.obs-points li')).toHaveLength(4);
    sesion.set(null);
    fixture.detectChanges();
    expect(fixture.componentInstance.verificado()).toBe(false);
    expect(fixture.nativeElement.querySelectorAll('.obs-points li')).toHaveLength(0);
  });
});
