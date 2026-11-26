import { expect, test, Locator } from './tenant-aware-test';

const API = 'http://localhost:5005';
function ok(data: unknown) { return { success: true, message: 'OK', errors: [], data }; }

async function verificarCromo(dialogo: Locator): Promise<void> {
  const contraste = await dialogo.evaluate(el => {
    const rgb = (value: string) => value.match(/[\d.]+/g)!.slice(0, 3).map(Number);
    const lum = (value: string) => rgb(value).map(n => n / 255).map(n => n <= .04045 ? n / 12.92 : ((n + .055) / 1.055) ** 2.4).reduce((s,n,i) => s + n * [0.2126,0.7152,0.0722][i], 0);
    const ratio = (a: string, b: string) => (Math.max(lum(a), lum(b)) + .05) / (Math.min(lum(a), lum(b)) + .05);
    const title = getComputedStyle(el.querySelector('.mat-mdc-dialog-title')!);
    const footer = getComputedStyle(el.querySelector('.mat-mdc-dialog-actions')!);
    const botones = [...el.querySelectorAll<HTMLButtonElement>('.mat-mdc-dialog-actions button:not(:disabled)')].map(b => {
      const css = getComputedStyle(b);
      const bg = css.backgroundColor === 'rgba(0, 0, 0, 0)' ? footer.backgroundColor : css.backgroundColor;
      return ratio(css.color, bg);
    });
    return { title: ratio(title.color, title.backgroundColor), botones };
  });
  expect(contraste.title).toBeGreaterThanOrEqual(4.5);
  expect(contraste.botones.every(r => r >= 4.5)).toBe(true);
}

async function verificarCampo(campo: Locator): Promise<void> {
  await campo.scrollIntoViewIfNeeded();
  await campo.focus();
  await expect(campo.locator('xpath=ancestor::mat-form-field')).toHaveClass(/mat-focused/);
  const medida = await campo.evaluate(el => {
    const field = el.closest('mat-form-field')!;
    const label = field.querySelector('.mdc-floating-label')!;
    const border = field.querySelector('.mdc-notched-outline__leading')!;
    const content = field.closest('.mat-mdc-dialog-content')!;
    const header = field.closest('.mat-mdc-dialog-container')!.querySelector('.mat-mdc-dialog-title')!;
    const css = getComputedStyle(el);
    const b = getComputedStyle(border);
    const labelRect = label.getBoundingClientRect();
    return {
      outline: css.outlineStyle,
      shadow: css.boxShadow,
      borderWidth: parseFloat(b.borderLeftWidth),
      borderColor: b.borderLeftColor,
      labelColor: getComputedStyle(label).color,
      labelHeight: labelRect.height,
      visibleTop: labelRect.top >= Math.max(content.getBoundingClientRect().top, header.getBoundingClientRect().bottom) - 1,
      visibleBottom: labelRect.bottom <= content.getBoundingClientRect().bottom + 1,
    };
  });
  expect(medida.outline).toBe('none');
  expect(medida.shadow).toBe('none');
  expect(medida.borderWidth).toBeGreaterThanOrEqual(2);
  expect(medida.labelColor).toBe(medida.borderColor);
  expect(medida.labelHeight).toBeGreaterThan(8);
  expect(medida.visibleTop).toBe(true);
  expect(medida.visibleBottom).toBe(true);
}

for (const viewport of [{ width: 1280, height: 900 }, { width: 390, height: 844 }, { width: 320, height: 568 }]) {
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
    await verificarCromo(consulta);
    await page.screenshot({ path: testInfo.outputPath('consulta.png') });
    await consulta.getByRole('button', { name: 'Cerrar', exact: true }).click();
    await expect(consulta).toHaveCount(0);

    // Reproduce exactamente el editor existente reportado por el usuario.
    await fila.getByRole('button', { name: 'Editar M', exact: true }).click();
    const editar = page.locator('.solqaryn-modal--m');
    await expect(editar).toBeVisible();
    await verificarCromo(editar);
    await verificarCampo(editar.getByRole('textbox', { name: 'Nombre', exact: true }));
    await verificarCampo(editar.getByRole('spinbutton', { name: 'Orden', exact: true }));
    await verificarCampo(editar.getByRole('textbox', { name: 'Descripción (opcional)', exact: true }));
    await page.screenshot({ path: testInfo.outputPath('editar-foco.png') });
    await editar.getByRole('button', { name: 'Cancelar', exact: true }).click();
    await expect(editar).toHaveCount(0);
    expect(posts).toBe(0);

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
    await verificarCromo(confirmacion);
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
