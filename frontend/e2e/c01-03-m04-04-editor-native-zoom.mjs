// C01-03.M04.04: isolated editor evidence; native zoom mechanism adapted from the existing C15-04 private probe. No application change.
import { chromium } from '@playwright/test';
import assert from 'node:assert/strict';
import { mkdtemp, mkdir, readFile, writeFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';

const exec = promisify(execFile);
const origin = 'http://127.0.0.1:4200';
const password = process.env.C1504_TEST_PASSWORD;
assert.ok(process.env.CI === 'true' && process.env.C1504_API_URL === 'http://127.0.0.1:5005'
  && password && process.env.DISPLAY && process.env.GITHUB_SHA, 'Requires isolated local CI and X11');
const root = await mkdtemp(join(tmpdir(), 'c0103-editor-native-'));
const extension = join(root, 'extension');
const output = resolve('test-results/c0103-editor-native-zoom');
const records = [];
const apiResponse = (r, path, method) => {
  const u = new URL(r.url());
  return ['127.0.0.1', 'localhost'].includes(u.hostname) && u.port === '5005'
    && u.pathname === path && r.request().method() === method;
};

async function audit(page, worker, name, locator) {
  async function zoom(factor) {
    const actual = await worker.evaluate(async ({ url, factor }) => {
      const tabs = await chrome.tabs.query({});
      const tab = tabs.find(t => t.url === url);
      if (tab?.id == null) throw Error('Local CI tab missing');
      await chrome.tabs.setZoomSettings(tab.id, { mode: 'automatic', scope: 'per-tab' });
      await chrome.tabs.setZoom(tab.id, factor);
      return chrome.tabs.getZoom(tab.id);
    }, { url: page.url(), factor });
    assert.ok(Math.abs(actual - factor) < 0.001, 'Native zoom mismatch');
    return actual;
  }
  const geometry = () => page.evaluate(() => ({
    width: innerWidth, height: innerHeight, client: document.documentElement.clientWidth,
    scroll: Math.max(document.documentElement.scrollWidth, document.body.scrollWidth),
    dpr: devicePixelRatio, visualScale: visualViewport?.scale ?? 1
  }));
  async function capture(factor, g) {
    await locator.scrollIntoViewIfNeeded();
    await page.bringToFront();
    // Native browser zoom notifications are outside the DOM. Wait for them
    // to dismiss before recording unoccluded physical screenshots.
    await page.waitForTimeout(3000);
    const unobscured = await locator.evaluate(el => {
      const r = el.getBoundingClientRect();
      const hit = document.elementFromPoint(r.left + r.width / 2, r.top + r.height / 2);
      return !!hit && (hit === el || el.contains(hit));
    });
    assert.ok(unobscured, name + ': capture control obscured in page');
    await page.evaluate(() => new Promise(done => requestAnimationFrame(() => requestAnimationFrame(done))));
    const box = await locator.boundingBox();
    assert.ok(box && box.width > 0 && box.x >= -2 && box.x + box.width <= g.client + 2,
      name + ': control horizontally clipped');
    assert.ok(box.y >= -2 && box.y + box.height <= g.height + 2, name + ': control not reachable by scroll');
    const filename = name + '-' + (factor * 100) + '.png';
    await exec('ffmpeg', ['-nostdin', '-hide_banner', '-loglevel', 'error',
      '-f', 'x11grab', '-draw_mouse', '0', '-video_size', '1600x1200',
      '-i', process.env.DISPLAY, '-frames:v', '1', '-threads', '1', '-y', join(output, filename)],
      { timeout: 15000 });
    const png = await readFile(join(output, filename));
    assert.ok(png.length >= 24 && png.subarray(0, 8).equals(Buffer.from([137,80,78,71,13,10,26,10])));
    assert.equal(png.readUInt32BE(16), 1600);
    assert.equal(png.readUInt32BE(20), 1200);
    assert.ok(g.width * g.dpr <= 1600, 'Physical viewport exceeds capture');
    return filename;
  }
  await locator.waitFor({ state: 'visible', timeout: 20000 });
  const actual100 = await zoom(1);
  await page.waitForFunction(() => Math.abs(devicePixelRatio - 1) < 0.1);
  const before = await geometry();
  const image100 = await capture(1, before);
  const actual200 = await zoom(2);
  await page.waitForFunction(b => Math.abs(devicePixelRatio / b.dpr - 2) < 0.05
    && Math.abs(innerWidth * 2 - b.width) <= 12, before);
  const after = await geometry();
  const bounds = await locator.evaluate(el => {
    const r = el.getBoundingClientRect();
    return { left: r.left, right: r.right, width: r.width };
  });
  assert.ok(bounds.width > 0 && bounds.left >= -2 && bounds.right <= after.client + 2, name + ': horizontal clipping');
  assert.ok(Math.abs(after.visualScale - 1) < 0.01, name + ': pinch is not native zoom');
  assert.ok(before.scroll <= before.client + 2, name + ': overflow at 100%');
  assert.ok(after.scroll <= after.client + 2, name + ': overflow at 200%');
  const image200 = await capture(2, after);
  // The previous probe stopped on ANY editor control, even when auditing
  // the save button after scrolling. Test the actual target, not its container.
  // Wait for browser-driven focus scrolling to settle; never call focus(),
  // scrollIntoView() or click to obtain the keyboard assertion.
  let reached = false;
  for (let i = 0; i < 100; i++) {
    await page.keyboard.press('Tab');
    await page.evaluate(() => new Promise(done => requestAnimationFrame(() => requestAnimationFrame(done))));
    if (await locator.evaluate(el => el === document.activeElement)) {
      reached = true;
      break;
    }
  }
  assert.ok(reached, name + ': keyboard could not reach target');
  const focus = await locator.evaluate(el => {
    const css = getComputedStyle(el), r = el.getBoundingClientRect();
    const hit = document.elementFromPoint(r.left + r.width / 2, r.top + r.height / 2);
    return el === document.activeElement && el.matches(':focus-visible')
      && css.visibility !== 'hidden' && css.display !== 'none'
      && r.width > 0 && r.height > 0
      && r.top >= -2 && r.bottom <= innerHeight + 2
      && r.left >= -2 && r.right <= document.documentElement.clientWidth + 2
      && (hit === el || el.contains(hit));
  });
  assert.ok(focus, name + ': real target keyboard focus missing or obscured');
  records.push({ name, actual100, actual200, before, after, bounds, image100, image200, focus });
  await zoom(1);
}

async function actor(user, tenant, run) {
  // Isolated CI profile only; do not save test credentials or display the
  // browser's password manager overlay over the evidence. Real login unchanged.
  const profile = join(root, user);
  await mkdir(join(profile, 'Default'), { recursive: true });
  await writeFile(join(profile, 'Default', 'Preferences'), JSON.stringify({
    credentials_enable_service: false,
    profile: { password_manager_enabled: false }
  }));
  const context = await chromium.launchPersistentContext(profile, {
    headless: false, viewport: null, args: ['--window-size=1440,1000',
      '--disable-extensions-except=' + extension, '--load-extension=' + extension]
  });
  try {
    const worker = context.serviceWorkers()[0] ?? await context.waitForEvent('serviceworker', { timeout: 15000 });
    const page = context.pages()[0] ?? await context.newPage();
    await page.goto(origin + '/login');
    await page.getByRole('textbox', { name: 'Nombre de usuario' }).fill(user);
    await page.locator('input[formcontrolname="password"]').fill(password);
    const login = page.waitForResponse(r => apiResponse(r, '/auth/login', 'POST'));
    await page.getByRole('button', { name: /Ingresar al sistema/ }).click();
    const result = await login;
    assert.equal(result.status(), 200, 'Real UI authentication required');
    const body = await result.json();
    assert.ok(typeof body?.data?.token === 'string' && body.data.token.split('.').length === 3);
    await page.waitForFunction(() => Boolean(localStorage.getItem('solqaryn_token')));
    if (tenant) await page.evaluate(id => localStorage.setItem('solqaryn_empresa_solicitada_id', id), tenant);
    await run(page, worker, body.data.token);
  } finally {
    await context.close();
  }
}

try {
  await mkdir(extension);
  await mkdir(output, { recursive: true });
  await writeFile(join(extension, 'manifest.json'), JSON.stringify({
    manifest_version: 3, name: 'SOLQARYN isolated editor native zoom', version: '1.0.0',
    permissions: ['tabs'], background: { service_worker: 'background.js' }
  }));
  await writeFile(join(extension, 'background.js'), 'chrome.runtime.onInstalled.addListener(() => {});');
  await actor('c1504_admin_a', '1', async (page, worker, token) => {
    const headers = { Authorization: 'Bearer ' + token };
    const policy = await page.request.get('http://127.0.0.1:5005/permisos/mis-permisos/empresa/1', { headers });
    assert.equal(policy.status(), 200);
    const grants = (await policy.json())?.data;
    assert.equal(grants?.operacionMultiEmpresaRestringida, false);
    assert.equal(grants?.esAdministrador, true);
    assert.ok(grants?.permisos?.includes('Configuracion:Editar'));
    const membership = await page.request.get('http://127.0.0.1:5005/tenant-context/1', { headers });
    assert.equal(membership.status(), 200);
    const pending = page.waitForResponse(r => apiResponse(r, '/empresa-configuracion/tenant/1', 'GET'));
    await page.goto(origin + '/configuracion/dinamica-v5');
    const result = await pending;
    assert.equal(result.status(), 200);
    const persisted = (await result.json())?.data;
    assert.equal(persisted?.empresaId, 1);
    const form = page.getByRole('form', { name: 'Configuración del tenant activo' });
    const field = form.locator('input[formcontrolname="nombre"]');
    const save = page.getByRole('button', { name: 'Guardar configuración tenant' });
    await form.waitFor({ state: 'visible', timeout: 20000 });
    assert.equal(await field.inputValue(), persisted.nombre);
    assert.equal(await save.isEnabled(), true);
    await audit(page, worker, 'editor-name', field);
    await audit(page, worker, 'editor-save', save);
    assert.equal(await field.inputValue(), persisted.nombre);
    assert.equal(await save.isEnabled(), true);
  });
  assert.equal(records.length, 2);
  console.log('C0103_M0404_NATIVE_EDITOR_ZOOM_100_200_PASS records=2 png=4');
} finally {
  try {
    await writeFile(join(output, 'metrics.json'), JSON.stringify({
      sha: process.env.GITHUB_SHA, mechanism: 'chrome.tabs.setZoom/getZoom + X11 physical desktop',
      scope: 'single-company editor with verified grants, no mutations; not historic BEFORE or whole-product UX certification',
      records
    }, null, 2));
  } finally {
    await rm(root, { recursive: true, force: true });
  }
}
