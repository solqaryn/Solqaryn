import { PEDIDOS_VENTA_ROUTES } from '../../pedidos-venta/pedidos-venta.routes';

describe('N3.7.E NotaCreditoCliente route contract', () => {
  it('expone únicamente las superficies soportadas por el API con RBAC de Ventas', () => {
    const home = PEDIDOS_VENTA_ROUTES.find(route => route.path === 'notas-credito-cliente');
    const create = PEDIDOS_VENTA_ROUTES.find(route => route.path === 'notas-credito-cliente/nueva');
    const detail = PEDIDOS_VENTA_ROUTES.find(route => route.path === 'notas-credito-cliente/:id');

    expect(home).toBeTruthy();
    expect(home?.data).toMatchObject({ modulo: 'Ventas', accion: 'Ver' });
    expect(create).toBeTruthy();
    expect(create?.data).toMatchObject({ modulo: 'Ventas', accion: 'Crear' });
    expect(detail).toBeTruthy();
    expect(detail?.data).toMatchObject({ modulo: 'Ventas', accion: 'Ver' });

    expect(PEDIDOS_VENTA_ROUTES.some(route => route.path === 'notas-credito-cliente/:id/editar')).toBe(false);
  });
});
