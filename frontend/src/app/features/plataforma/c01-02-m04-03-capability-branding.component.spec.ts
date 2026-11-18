import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, Subject } from 'rxjs';
import { routes } from '../../app.routes';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { TemaVisualService } from '../../services/tema-visual.service';
import { C0102CapabilityBrandingComponent, c0102ColorSeguro } from './c01-02-m04-03-capability-branding.component';

const global: PlataformaSesion = { nombreUsuario: 'QA', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true };
const tema = { colorPrimario: '#123abc', colorSecundario: '#456', colorAcento: '#ab3299' };
async function crear(sesion: () => PlataformaSesion | null, empresa: () => number | null,
  permitido: () => boolean, temaGet: () => any) {
  TestBed.resetTestingModule();
  await TestBed.configureTestingModule({
    imports: [C0102CapabilityBrandingComponent],
    providers: [
      provideRouter(routes),
      { provide: PlataformaSesionService, useValue: { sesion } },
      { provide: TenantContextService, useValue: { tieneContextoVerificado: () => empresa() !== null, empresaIdVerificada: empresa } },
      { provide: PermisosRuntimeService, useValue: { cargado: () => true,
        puede: (modulo: string, accion: string) => permitido() && modulo === 'Configuracion' && accion === 'Ver',
        rutaHabilitada: (ruta: string) => permitido() && (ruta === '/configuracion' || ruta === 'configuracion') } },
      { provide: TemaVisualService, useValue: { get: temaGet } }
    ]
  }).compileComponents();
  const fixture = TestBed.createComponent(C0102CapabilityBrandingComponent);
  fixture.detectChanges();
  return fixture;
}
describe('C01-02.M04.03 - branding fail closed', () => {
  it('ruta con guarda exacta; solo hex de color seguro', async () => {
    const r = routes.find(x => x.path === 'plataforma/capacidades-branding-v19');
    expect(routes.filter(x => x.path === r?.path)).toHaveLength(1);
    expect(r?.canActivate).toEqual([plataformaGuard]);
    expect(await (r?.loadComponent as () => Promise<unknown>)()).toBe(C0102CapabilityBrandingComponent);
    expect(c0102ColorSeguro('#a0b')).toBe('#a0b');
    expect(c0102ColorSeguro('#A0b123')).toBe('#A0b123');
    for (const invalid of ['red', 'url(https://bad)', 'var(--x)', '#abcd', '#fff;position:fixed', '', null, 123])
      expect(c0102ColorSeguro(invalid)).toBeNull();
  });
  it('sin sesion o tenant no consulta API ni revela tema', async () => {
    const s = signal<PlataformaSesion | null>(null), id = signal<number | null>(null);
    let llamadas = 0;
    const f = await crear(s.asReadonly(), id.asReadonly(), () => true, () => { llamadas++; return of({ success: true, data: tema }); });
    const root: HTMLElement = f.nativeElement;
    expect(root.querySelector('[data-testid="marca-denegado"]')).not.toBeNull();
    s.set(global); f.detectChanges();
    expect(root.querySelector('[data-testid="marca-sin-tenant"]')).not.toBeNull();
    expect(root.querySelectorAll('.muestra,li,form,button')).toHaveLength(0);
    expect(llamadas).toBe(0);
  });
  it('permiso y tema leidos; revocacion y cambio A/B ocultan informacion', async () => {
    const s = signal<PlataformaSesion | null>(global), id = signal<number | null>(41), grant = signal(true);
    let llamadas = 0;
    const f = await crear(s.asReadonly(), id.asReadonly(), grant.asReadonly(), () => { llamadas++; return of({ success: true, data: tema }); });
    const root: HTMLElement = f.nativeElement;
    expect(llamadas).toBe(1);
    expect(root.querySelector('[data-testid="marca-listo"]')).not.toBeNull();
    expect(root.querySelectorAll('.muestra')).toHaveLength(3);
    expect(root.querySelectorAll('form,button,input')).toHaveLength(0);
    grant.set(false); f.detectChanges();
    expect(root.querySelector('[data-testid="marca-sin-permisos"]')).not.toBeNull();
    expect(root.querySelectorAll('.muestra,li')).toHaveLength(0);
    grant.set(true); id.set(42); f.detectChanges();
    expect(llamadas).toBe(2);
    expect(f.componentInstance.marca()?.primario).toBe(tema.colorPrimario);
    expect(root.querySelector('[data-testid="marca-listo"]')).not.toBeNull();
    s.set(null); f.detectChanges();
    expect(root.querySelector('[data-testid="marca-denegado"]')).not.toBeNull();
  });
  it('cambio A a B montado: solicita B, no muestra A tardio y finaliza LISTO', async () => {
    const id = signal<number | null>(41);
    const pendientes: Subject<{ success: boolean; data: typeof tema }>[] = [];
    const f = await crear(() => global, id.asReadonly(), () => true, () => {
      const subject = new Subject<{ success: boolean; data: typeof tema }>();
      pendientes.push(subject);
      return subject.asObservable();
    });
    expect(pendientes).toHaveLength(1);
    id.set(42); f.detectChanges();
    expect(pendientes).toHaveLength(2);
    expect(f.componentInstance.estado()).toBe('CARGANDO');
    pendientes[0].next({ success: true, data: { ...tema, colorPrimario: '#ff0000' } });
    f.detectChanges();
    expect(f.componentInstance.marca()).toBeNull();
    expect((f.nativeElement as HTMLElement).querySelectorAll('.muestra,li')).toHaveLength(0);
    pendientes[1].next({ success: true, data: { ...tema, colorPrimario: '#00ff00' } });
    f.detectChanges();
    expect(f.componentInstance.marca()?.primario).toBe('#00ff00');
    expect(f.componentInstance.estado()).toBe('LISTO');
    pendientes[0].error(new Error('Fallo tardio A')); f.detectChanges();
    expect(f.componentInstance.marca()?.primario).toBe('#00ff00');
  });
  it('fallo del nuevo tenant termina en ERROR sin restaurar colores anteriores', async () => {
    const id = signal<number | null>(41);
    const pendientes: Subject<{ success: boolean; data: typeof tema }>[] = [];
    const f = await crear(() => global, id.asReadonly(), () => true, () => {
      const subject = new Subject<{ success: boolean; data: typeof tema }>();
      pendientes.push(subject);
      return subject.asObservable();
    });
    pendientes[0].next({ success: true, data: tema }); f.detectChanges();
    expect(f.componentInstance.estado()).toBe('LISTO');
    id.set(42); f.detectChanges();
    expect(pendientes).toHaveLength(2);
    expect(f.componentInstance.marca()).toBeNull();
    pendientes[1].error(new Error('Sin tema B')); f.detectChanges();
    expect(f.componentInstance.estado()).toBe('ERROR');
    expect((f.nativeElement as HTMLElement).querySelectorAll('.muestra,li')).toHaveLength(0);
  });
  it('respuesta maliciosa de color provoca ERROR y no CSS', async () => {
    const f = await crear(() => global, () => 41, () => true,
      () => of({ success: true, data: { ...tema, colorPrimario: 'url(https://bad)' } }));
    expect((f.nativeElement as HTMLElement).querySelector('[data-testid="marca-error"]')).not.toBeNull();
    expect((f.nativeElement as HTMLElement).querySelectorAll('.muestra,li')).toHaveLength(0);
  });
});
