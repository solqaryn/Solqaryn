import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { PedidosDocumentosReadinessV5Panel } from './pedidos-venta.components';
import { PEDIDOS_VENTA_ROUTES } from './pedidos-venta.routes';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { authGuard } from '../../core/guards/auth.guard';
import { permisoGuard } from '../../core/guards/permiso.guard';

describe('C07-04.M04.01 - criterios de pedidos y documentos sin operaciones', () => {
  async function montar() {
    TestBed.resetTestingModule();
    const empresa = signal(true);
    const permiso = signal(true);
    const ruta = signal(true);
    await TestBed.configureTestingModule({
      imports: [PedidosDocumentosReadinessV5Panel],
      providers: [
        { provide: TenantContextService, useValue: {
          tieneContextoVerificado: empresa
        } },
        { provide: PermisosRuntimeService, useValue: {
          puede: (modulo: string, accion: string) => modulo === 'Ventas' &&
            accion === 'Ver' && permiso(),
          rutaHabilitada: (path: string) => path === 'pedidos-venta' && ruta()
        } }
      ]
    }).compileComponents();
    const fixture = TestBed.createComponent(PedidosDocumentosReadinessV5Panel);
    fixture.detectChanges();
    return { fixture, empresa, permiso, ruta };
  }

  it('mantiene ruta de listado preexistente con autenticacion y RBAC real', () => {
    const matches = PEDIDOS_VENTA_ROUTES.filter(r => r.path === 'pedidos-venta');
    expect(matches).toHaveLength(1);
    expect(matches[0].canActivate).toEqual([authGuard, permisoGuard]);
    expect(matches[0].data).toEqual({ modulo: 'Ventas', accion: 'Ver' });
  });

  it('muestra cuatro criterios exclusivamente al lector autorizado', async () => {
    const { fixture } = await montar();
    const host: HTMLElement = fixture.nativeElement;
    expect(host.querySelectorAll('.pedidos-readiness-cards li')).toHaveLength(4);
    expect(host.textContent).toContain('NO_MEDIDO');
    expect(host.querySelectorAll('form,input,textarea,button')).toHaveLength(0);
    expect(host.textContent).not.toContain('A5_PASS');
  });

  it('sin tenant verificado no expone criterios', async () => {
    const { fixture, empresa } = await montar();
    empresa.set(false); fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.pedidos-readiness-cards li')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).toContain('criterios internos ocultos');
  });

  it('sin Ventas:Ver no expone criterios aunque exista tenant', async () => {
    const { fixture, permiso } = await montar();
    permiso.set(false); fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.pedidos-readiness-cards li')).toHaveLength(0);
  });

  it('ruta retirada del allowlist de servidor deniega visualizacion', async () => {
    const { fixture, ruta } = await montar();
    ruta.set(false); fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.pedidos-readiness-cards li')).toHaveLength(0);
  });

  it('revocacion reactiva oculta todo sin recuperar permisos de cache', async () => {
    const { fixture, permiso, empresa } = await montar();
    expect(fixture.nativeElement.querySelectorAll('.pedidos-readiness-cards li')).toHaveLength(4);
    permiso.set(false); fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.pedidos-readiness-cards li')).toHaveLength(0);
    empresa.set(false); permiso.set(true); fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.pedidos-readiness-cards li')).toHaveLength(0);
  });
});
