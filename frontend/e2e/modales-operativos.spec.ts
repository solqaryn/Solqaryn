import { expect, test } from './tenant-aware-test';
const API = 'http://localhost:5005';
const ok = (data: unknown) => ({ success: true, message: 'OK', errors: [], data });

for (const modulo of ['ventas', 'compras'] as const) {
  const venta = modulo === 'ventas';
  const nombre = venta ? 'venta' : 'compra';
  const numero = venta ? 'V-QA-1' : 'C-QA-1';
  for (const viewport of [{width: 1280, height: 900}, {width: 390, height: 844}]) {
    test('QA visual: ' + modulo + ' modales transaccionales ' + viewport.width, async ({ page }) => {
      await page.setViewportSize(viewport);
      await page.addInitScript(() => {
        localStorage.setItem('solqaryn_token', 'e2e-operativo');
        localStorage.setItem('solqaryn_user', 'qa_visual');
        localStorage.setItem('solqaryn_nombre_completo', 'QA Visual');
        localStorage.setItem('solqaryn_rol', 'Administrador');
        localStorage.setItem('solqaryn_expira_en', '2099-12-31T23:59:59Z');
        localStorage.setItem('solqaryn_empresa_solicitada_id', '1');
      });
      let estado = 'Borrador';
      let escrituras = 0;
      let puedeMutar = true;
      const registro = () => ({ id: 1, numeroVenta: numero, numeroCompra: numero, fecha: '2026-11-25T12:00:00Z', fechaCreacion: '2026-11-25T12:00:00Z', clienteNombre: 'Cliente QA', proveedorNombre: 'Proveedor QA', estado, estadoPago: 'Pendiente', metodoPago: 'Efectivo', subtotal: 100, total: 100, impuesto: 0, descuento: 0, importeBruto: 100, importeProductos: 100, costoEnvio: 0, envioExonerado: false, utilidadBruta: 10, detalles: [], descuentosAplicados: [], impuestosAplicados: [] });
      await page.route(API + '/permisos/mis-permisos/empresa/*', r => r.fulfill({ json: ok(puedeMutar ? { esAdministrador: true, permisos: [] } : { esAdministrador: false, permisos: [venta ? 'Ventas:Ver' : 'Compras:Ver'] }) }));
      await page.route(url => url.origin === API && url.pathname === '/' + modulo, r => r.fulfill({ json: ok({items: [registro()], totalCount: 1, page: 1, pageSize: 10}) }));
      await page.route(API + '/' + modulo + '/1', r => {
        if (r.request().method() === 'DELETE') escrituras++;
        return r.fulfill({ json: ok(registro()) });
      });
      await page.route(API + '/' + modulo + '/1/documentos', r => r.fulfill({ json: ok([]) }));
      await page.route(API + '/costos-envio', r => r.fulfill({ json: ok([]) }));
      await page.route(API + '/metodos-pago/activos', r => r.fulfill({ json: ok([{id: 1, codigo: 'EFECTIVO', nombre: 'Efectivo', tipo: 'Efectivo', activo: true, orden: 0}]) }));
      await page.route(API + '/' + modulo + '/1/confirmar', r => {
        escrituras++; estado = 'Confirmada'; return r.fulfill({ json: ok(registro()) });
      });
      await page.goto('/' + modulo);
      const tabla = page.locator('table.app-data-table');
      await expect(tabla).toHaveCount(1);
      await expect(tabla).toBeVisible();
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth + 1)).toBe(true);
      await tabla.getByRole('button', { name: 'Eliminar ' + numero, exact: true }).click();
      const alerta = page.locator('.solqaryn-modal--s');
      await expect(alerta).toBeVisible();
      await alerta.getByRole('button', {name:'Cancelar',exact:true}).click();
      expect(escrituras).toBe(0);
      await page.getByRole('button', { name: 'Nueva ' + nombre, exact: false }).click();
      const form = page.locator('.solqaryn-modal--xl');
      await expect(form.getByRole('heading', {name:'Nueva ' + nombre, exact:true})).toBeVisible();
      await form.getByRole('button', {name:'Cancelar',exact:true}).click();
      await expect(form).toHaveCount(0);
      await tabla.getByRole('button', {name:'Editar ' + numero,exact:true}).click();
      await expect(form.getByRole('heading', {name:'Editar ' + nombre + ' (Borrador)',exact:true})).toBeVisible();
      await form.getByRole('button', {name:'Cancelar',exact:true}).click();
      await expect(form).toHaveCount(0);
      await tabla.getByRole('button', {name:'Ver ' + numero,exact:true}).click();
      const detalle = page.locator('.solqaryn-modal--xl');
      await expect(detalle.getByRole('heading', {name:'Detalle de ' + nombre + ' ' + numero,exact:true})).toBeVisible();
      await detalle.getByRole('button', {name:'Confirmar ' + nombre,exact:true}).click();
      await alerta.getByRole('button', {name:'Cancelar',exact:true}).click();
      expect(escrituras).toBe(0);
      await detalle.getByRole('button', {name:'Confirmar ' + nombre,exact:true}).click();
      await alerta.getByRole('button', {name:'Confirmar',exact:true}).click();
      await expect(detalle.getByRole('button', {name:'Anular ' + nombre,exact:true})).toBeVisible();
      expect(escrituras).toBe(1);
      await detalle.getByRole('button', {name:'Cerrar',exact:true}).click();
      await expect(tabla.getByText('Confirmada',{exact:true})).toBeVisible();
      await expect(tabla.getByRole('button', {name:'Editar ' + numero,exact:true})).toHaveCount(0);
      expect(new URL(page.url()).pathname).toBe('/' + modulo);
      puedeMutar = false; await page.reload();
      await expect(tabla.getByRole('button',{name:'Ver ' + numero,exact:true})).toBeVisible();
      await expect(page.getByRole('button',{name:'Nueva ' + nombre,exact:false})).toHaveCount(0);
      await tabla.getByRole('button',{name:'Ver ' + numero,exact:true}).click();
      await expect(detalle.getByRole('button',{name:'Anular ' + nombre,exact:true})).toHaveCount(0);
      expect(escrituras).toBe(1);
    });
  }
}
