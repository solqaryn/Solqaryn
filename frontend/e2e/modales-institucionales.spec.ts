import { expect, test } from './tenant-aware-test';

const API = 'http://localhost:5005';
function ok(data: unknown) { return { success: true, message: 'OK', errors: [], data }; }

for (const viewport of [{ width: 1280, height: 900 }, { width: 390, height: 844 }]) {
  test('QA visual: modales institucionales de catálogo y confirmaciones ' + viewport.width, async ({ page }, testInfo) => {
    await page.setViewportSize(viewport);
    await page.addInitScript(() => {
      localStorage.setItem('solqaryn_token', 'e2e-institucional');
      localStorage.setItem('solqaryn_user', 'qa_visual');
      localStorage.setItem('solqaryn_nombre_completo', 'QA Visual');
      localStorage.setItem('solqaryn_rol', 'Administrador');
      localStorage.setItem('solqaryn_expira_en', '2099-12-31T23:59:59Z');
      localStorage.setItem('solqaryn_empresa_solicitada_id', '1');
    });
    let posts = 0;
    let patches = 0;
    let tallas = [{ id: 1, tipo: 'Talla', nombre: 'M', descripcion: 'Talla de prueba', orden: 0, activo: true, totalProductos: 0, totalModelos: 0, fechaCreacion: '', fechaActualizacion: '' }];
    await page.route(API + '/permisos/mis-permisos/empresa/*', r => r.fulfill({ json: ok({ esAdministrador: true, permisos: [] }) }));
    await page.route(API + '/tallas', async r => {
      if (r.request().method() === 'POST') {
        posts++;
        tallas = [...tallas, { ...tallas[0], ...r.request().postDataJSON(), id: 2 }];
        await r.fulfill({ status: 201, json: ok(tallas[1]) });
      } else {
        await r.fulfill({ json: ok(tallas) });
      }
    });
    await page.route(API + '/tallas/1/desactivar', async r => {
      patches++;
      tallas[0] = { ...tallas[0], activo: false };
      await r.fulfill({ json: ok(tallas[0]) });
    });
    await page.goto('/tallas');
    const tabla = page.locator('table.app-data-table');
    await expect(tabla).toBeVisible();
    await expect(tabla).toHaveCount(1);
    const fila = tabla.locator('tbody tr').filter({ has: page.getByRole('button', { name: 'Ver M', exact: true }) });
    await expect(fila).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth + 1)).toBe(true);
    const alineaciones = await tabla.evaluate(el => {
      const ths = [...el.querySelectorAll('thead th')];
      const tds = [...el.querySelectorAll('tbody tr:first-child td')];
      return ths.map((th, i) => getComputedStyle(th).textAlign === getComputedStyle(tds[i]).textAlign);
    });
    expect(alineaciones.every(Boolean)).toBe(true);
    await fila.getByRole('button', { name: 'Ver M', exact: true }).click();
    const consulta = page.locator('.solqaryn-modal--m');
    await expect(consulta).toBeVisible();
    await page.mouse.click(2, 2);
    await page.keyboard.press('Escape');
    await expect(consulta).toBeVisible();
    expect(await consulta.evaluate(el => {
      const h = el.querySelector('.mat-mdc-dialog-title')!;
      const f = el.querySelector('.mat-mdc-dialog-actions')!;
      return getComputedStyle(h).backgroundColor === getComputedStyle(f).backgroundColor && getComputedStyle(h).backgroundColor !== 'rgba(0, 0, 0, 0)';
    })).toBe(true);
    await page.screenshot({ path: testInfo.outputPath('consulta.png') });
    await consulta.getByRole('button', { name: 'Cerrar', exact: true }).click();
    await expect(consulta).toHaveCount(0);

    await page.getByRole('button', { name: /Nuevo talla/i }).click();
    const editor = page.locator('.solqaryn-modal--m');
    await expect(editor).toBeVisible();
    await editor.getByRole('textbox', { name: 'Nombre', exact: true }).fill('S');
    await editor.getByRole('button', { name: 'Guardar', exact: true }).click();
    const confirmacion = page.locator('.solqaryn-modal--s');
    await expect(confirmacion).toBeVisible();
    await page.mouse.click(2, 2);
    await page.keyboard.press('Escape');
    await expect(confirmacion).toBeVisible();
    expect(posts).toBe(0);
    await page.screenshot({ path: testInfo.outputPath('confirmacion.png') });
    await confirmacion.getByRole('button', { name: 'Cancelar', exact: true }).click();
    await expect(confirmacion).toHaveCount(0);
    await expect(editor.getByRole('textbox', { name: 'Nombre', exact: true })).toHaveValue('S');
    expect(posts).toBe(0);
    await editor.getByRole('button', { name: 'Guardar', exact: true }).click();
    await confirmacion.getByRole('button', { name: 'Guardar', exact: true }).click();
    await expect(editor).toHaveCount(0);
    await expect(tabla.getByRole('button', { name: 'Ver S', exact: true })).toBeVisible();
    expect(posts).toBe(1);

    const toggle = fila.getByRole('switch', { name: 'Desactivar M' });
    await toggle.click();
    await expect(confirmacion).toBeVisible();
    await confirmacion.getByRole('button', { name: 'Cancelar', exact: true }).click();
    await expect(toggle).toBeChecked();
    expect(patches).toBe(0);
    await toggle.click();
    await confirmacion.getByRole('button', { name: 'Desactivar', exact: true }).click();
    await expect(fila.getByRole('switch', { name: 'Activar M' })).not.toBeChecked();
    expect(patches).toBe(1);
    await page.screenshot({ path: testInfo.outputPath('tabla.png') });
  });
}
