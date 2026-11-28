import { expect, test } from './tenant-aware-test';

const API = 'http://localhost:5005';
const ok = (data: unknown) => ({ success: true, message: 'OK', errors: [], data });

for (const viewport of [{ width: 1280, height: 900 }, { width: 390, height: 844 }, { width: 320, height: 568 }]) {
  test('QA visual: modales institucionales de costos de envío ' + viewport.width, async ({ page }) => {
    await page.setViewportSize(viewport);
    await page.addInitScript(() => {
      localStorage.setItem('solqaryn_token', 'e2e-envios');
      localStorage.setItem('solqaryn_user', 'qa_visual');
      localStorage.setItem('solqaryn_nombre_completo', 'QA Visual');
      localStorage.setItem('solqaryn_rol', 'Administrador');
      localStorage.setItem('solqaryn_expira_en', '2099-12-31T23:59:59Z');
      localStorage.setItem('solqaryn_empresa_solicitada_id', '1');
    });
    let writes = 0;
    let item = { id: 1, nombre: 'Tarifa QA', ciudad: 'Ciudad QA', monto: 25, prioridad: 1, esPredeterminado: false, activo: true, estaVigente: true, fechaCreacion: '2026-11-27T12:00:00Z', fechaActualizacion: '2026-11-27T12:00:00Z' };
    await page.route(API + '/permisos/mis-permisos/empresa/*', r => r.fulfill({ json: ok({ esAdministrador: true, permisos: [] }) }));
    await page.route(API + '/costos-envio/administracion', r => r.fulfill({ json: ok([item]) }));
    await page.route(API + '/costos-envio', r => { if (r.request().method() === 'POST') writes++; return r.fulfill({ status: 201, json: ok(item) }); });
    await page.route(API + '/costos-envio/1', r => {
      if (r.request().method() === 'PUT') {
        const body = r.request().postDataJSON();
        writes++;
        item = { ...item, ...body };
      } else if (r.request().method() === 'DELETE') writes++;
      return r.fulfill({ json: ok(item) });
    });
    await page.route(API + '/costos-envio/1/estado', r => {
      writes++;
      item = { ...item, activo: r.request().postDataJSON().activo };
      return r.fulfill({ json: ok(item) });
    });
    await page.route(API + '/costos-envio/resolver', r => {
      const body = r.request().postDataJSON();
      return r.fulfill({ json: ok(body.ciudad === 'Ciudad QA'
        ? { ...item, activo: true }
        : { ...item, id: 2, nombre: 'Tarifa general QA', monto: 37.25, ciudad: '', esPredeterminado: true, activo: true }) });
    });
    await page.route(API + '/costos-envio/1/historial?*', r => r.fulfill({ json: ok({ items: [{id:1, accion:'Editar', fecha:'2026-11-27T12:00:00Z', nombreUsuario:'QA', descripcion:'Tarifa actualizada'}], page:1, pageSize:20, totalPages:1, totalCount:1 }) }));
    await page.goto('/costos-envio');
    const row = page.getByRole('row').filter({ hasText: 'Tarifa QA' });
    await expect(row).toBeVisible();
    const large = page.locator('.solqaryn-modal--l');
    const medium = page.locator('.solqaryn-modal--m');
    const alert = page.locator('.solqaryn-modal--s');
    const geometry = async () => {
      const dialog = page.getByRole('dialog');
      await expect(dialog).toBeVisible();
      const box = await dialog.boundingBox();
      expect(box).not.toBeNull();
      expect(box!.x).toBeGreaterThanOrEqual(0);
      expect(box!.x + box!.width).toBeLessThanOrEqual(viewport.width + 1);
      expect(box!.y + box!.height).toBeLessThanOrEqual(viewport.height + 1);
      await page.keyboard.press('Escape');
      await expect(dialog).toBeVisible();
    };
    const nueva = page.getByRole('button', { name: 'Nueva tarifa' });
    await nueva.click();
    await expect(large.getByRole('heading', { name: 'Nuevo costo de envío' })).toBeVisible();
    await geometry();
    await expect(large.locator('mat-dialog-content')).toBeVisible();
    await large.locator('input[formcontrolname="nombre"]').fill('No persistir');
    await large.getByRole('button', { name: 'Cancelar' }).click();
    await expect(large).toHaveCount(0);
    await expect(nueva).toBeFocused();
    expect(writes).toBe(0);
    const editar = row.getByRole('button', { name: 'Editar costo de envío' });
    await editar.click();
    await expect(large.locator('input[formcontrolname="nombre"]')).toHaveValue('Tarifa QA');
    await geometry();
    await large.getByRole('button', { name: 'Cancelar' }).click();
    await expect(large).toHaveCount(0);
    expect(writes).toBe(0);
    await editar.click();
    const monto = large.locator('input[formcontrolname="monto"]');
    await expect(monto).toBeEnabled();
    await expect(monto).toHaveValue('25');
    // Simula un valor visible modificado por autofill sin evento input.
    await monto.evaluate((element: HTMLInputElement) => { element.value = '46.25'; });
    await expect(monto).toHaveValue('46.25');
    const update = page.waitForRequest(r => r.url() === API + '/costos-envio/1' && r.method() === 'PUT');
    await large.getByRole('button', { name: 'Actualizar', exact: true }).click();
    const updated = (await update).postDataJSON();
    expect(updated.monto).toBe(46.25);
    expect(updated.ciudad).toBe('Ciudad QA');
    expect(updated.activo).toBe(true);
    expect(updated.esPredeterminado).toBe(false);
    await expect(large).toHaveCount(0);
    await expect(row).toContainText(/46[.,]25/);
    expect(writes).toBe(1);
    await row.getByRole('button', { name: 'Desactivar costo de envío' }).click();
    await expect(alert).toBeVisible();
    await alert.getByRole('button', { name: 'Cancelar' }).click();
    await expect(alert).toHaveCount(0);
    expect(writes).toBe(1);
    await row.getByRole('button', { name: 'Desactivar costo de envío' }).click();
    await alert.getByRole('button', { name: 'Desactivar', exact: true }).click();
    await expect(row).toContainText('Inactivo');
    expect(writes).toBe(2);
    await page.getByRole('button', { name: 'Simular tarifa' }).click();
    await expect(medium.getByRole('heading', { name: 'Simular tarifa' })).toBeVisible();
    await geometry();
    const simulador = medium.locator('form[data-form-contract="shipping-simulator-v2"]');
    await expect(simulador).toHaveAttribute('data-form-ready', 'true');
    const ciudad = simulador.locator('input[formcontrolname="ciudad"]');
    await expect(ciudad).toBeEnabled();
    for (const [input, nombre] of [['Ciudad QA', 'Tarifa QA'], ['Otra ciudad QA', 'Tarifa general QA']]) {
      if (input === 'Ciudad QA') await ciudad.fill(input);
      else {
        // Reproduce el fallo de serialización nativa y un valor actualizado sin evento input.
        // El envío usa las referencias del template, no los nombres de FormData.
        await ciudad.evaluate((element: HTMLInputElement, value: string) => {
          element.removeAttribute('name');
          element.value = value;
        }, input);
        expect(await ciudad.evaluate((element: HTMLInputElement) =>
          element.form !== null && new FormData(element.form).get('ciudad') === null)).toBe(true);
      }
      await expect(ciudad).toHaveValue(input);
      const resolve = page.waitForRequest(r => r.url() === API + '/costos-envio/resolver' && r.method() === 'POST');
      await medium.getByRole('button', { name: 'Resolver tarifa' }).click();
      expect((await resolve).postDataJSON().ciudad).toBe(input);
      await expect(medium.getByRole('status')).toContainText(nombre);
    }
    await medium.getByRole('button', { name: 'Cerrar' }).click();
    await expect(medium).toHaveCount(0);
    await row.getByRole('button', { name: 'Ver historial de tarifa' }).click();
    await expect(large.getByRole('heading', { name: 'Historial: Tarifa QA' })).toBeVisible();
    await expect(large.getByText('Tarifa actualizada', { exact: false })).toBeVisible();
    await geometry();
    await large.getByRole('button', { name: 'Cerrar' }).click();
    await expect(large).toHaveCount(0);
    expect(writes).toBe(2);
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth + 1)).toBe(true);
  });
}
