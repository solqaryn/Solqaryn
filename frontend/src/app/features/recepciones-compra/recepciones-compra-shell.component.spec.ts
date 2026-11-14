import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { RecepcionesCompraShellComponent } from './recepciones-compra-shell.component';

describe('RecepcionesCompraShellComponent RBAC', () => {
  function crear(permisos: string[]) {
    const service = {
      getPaged: vi.fn(() => of({ data: { items: [], totalCount: 0 } }))
    };
    const router = { navigate: vi.fn(() => Promise.resolve(true)) };
    const runtime = {
      puede: vi.fn((modulo: string, accion: string) => permisos.includes(`${modulo}:${accion}`))
    };

    const component = new RecepcionesCompraShellComponent(service as any, router as any, runtime as any);
    return { component, service, router, runtime };
  }

  it('no consulta ni navega cuando falta Compras:Ver', () => {
    const { component, service, router } = crear(['Compras:Crear']);

    component.ngOnInit();
    component.verDetalle(41);

    expect(component.puedeVer()).toBe(false);
    expect(component.puedeCrear()).toBe(true);
    expect(service.getPaged).not.toHaveBeenCalled();
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('oculta Crear pero permite consultar cuando sólo existe Compras:Ver', () => {
    const { component, service, router } = crear(['Compras:Ver']);

    component.ngOnInit();
    component.nuevaRecepcion();

    expect(component.puedeVer()).toBe(true);
    expect(component.puedeCrear()).toBe(false);
    expect(service.getPaged).toHaveBeenCalledTimes(1);
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('pagina de forma explícita sin perder el tamaño solicitado', () => {
    const { component, service } = crear(['Compras:Ver']);
    component.ngOnInit();
    service.getPaged.mockClear();

    component.cambiarPagina({ pageIndex: 2, pageSize: 50, length: 180, previousPageIndex: 1 });

    expect(component.page).toBe(3);
    expect(component.pageSize).toBe(50);
    expect(service.getPaged).toHaveBeenCalledTimes(1);
    expect(service.getPaged).toHaveBeenCalledWith(expect.objectContaining({ page: 3, pageSize: 50 }));
  });

  it('sanitiza el error del backend y no expone detail/message sensibles', () => {
    const { component, service } = crear(['Compras:Ver']);
    service.getPaged.mockImplementation(() => throwError(() => ({
      error: { detail: 'SQLSTATE 23000 tabla_secreta', message: 'stack interno' }
    })) as any);

    component.ngOnInit();

    expect(component.error()).toBe('No fue posible cargar las recepciones de compra.');
    expect(component.error()).not.toContain('SQLSTATE');
    expect(component.error()).not.toContain('stack interno');
  });
});
