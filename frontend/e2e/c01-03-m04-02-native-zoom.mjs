// C01-03.M04.02 — native Chromium page zoom, never CSS/root font-size or pinch.
// Runs only against GitHub-hosted isolated MySQL/JWT/Angular with physical X11.
import { chromium } from '@playwright/test';
import assert from 'node:assert/strict';
import { mkdtemp, mkdir, writeFile, readFile, rm } from 'node:fs/promises';
import { join, resolve } from 'node:path';
import { tmpdir } from 'node:os';
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';

const exec = promisify(execFile);
assert.ok(process.env.CI === 'true' && process.env.GITHUB_SHA &&
  process.env.C1504_API_URL === 'http://127.0.0.1:5005' &&
  process.env.C1504_TEST_PASSWORD && process.env.DISPLAY,
  'Requires GitHub CI, real local API and physical X11');
const root = await mkdtemp(join(tmpdir(), 'sq-m0402-native-'));
const extension = join(root, 'extension');
const output = resolve('test-results/c0103-m0402-native-zoom');
const base = 'http://127.0.0.1:4200';
const apiPath = '/empresa-configuracion/tenant/1';
const results = [];
const isTenantGet = r => {
  const u = new URL(r.url());
  return ['127.0.0.1', 'localhost'].includes(u.hostname) &&
    u.port === '5005' && u.pathname === apiPath && r.method() === 'GET';
};

async function setNativeZoom(worker, page, factor) {
  const actual = await worker.evaluate(async ({ url, factor }) => {
    const tabs = await chrome.tabs.query({});
    const tab = tabs.find(t => t.url === url);
    if (!tab || tab.id == null) throw Error('Native zoom tab not found');
    await chrome.tabs.setZoomSettings(tab.id, { mode: 'automatic', scope: 'per-tab' });
    await chrome.tabs.setZoom(tab.id, factor);
    return chrome.tabs.getZoom(tab.id);
  }, { url: page.url(), factor });
  assert.ok(Math.abs(actual - factor) < .001, 'chrome.tabs.getZoom mismatch');
  return actual;
}
async function physicalScreenshot(page, filename) {
  await page.bringToFront();
  await page.waitForTimeout(3000);
  const path = join(output, filename);
  await exec('ffmpeg', ['-nostdin', '-hide_banner', '-loglevel', 'error',
    '-f', 'x11grab', '-draw_mouse', '0', '-video_size', '1600x1200',
    '-i', process.env.DISPLAY, '-frames:v', '1', '-threads', '1', '-y', path],
  { timeout: 20000 });
  const png = await readFile(path);
  assert.ok(png.length > 24 && png.subarray(0, 8)
    .equals(Buffer.from([137,80,78,71,13,10,26,10])), filename + ': missing PNG');
  assert.equal(png.readUInt32BE(16), 1600);
  assert.equal(png.readUInt32BE(20), 1200);
  return filename;
}
async function geometry(page, locator, state) {
  const g = await page.evaluate(() => ({
    viewport: innerWidth, height: innerHeight, dpr: devicePixelRatio,
    pinch: visualViewport?.scale ?? 1,
    document: document.documentElement.scrollWidth, body: document.body.scrollWidth,
    client: document.documentElement.clientWidth,
  }));
  assert.ok(Math.abs(g.pinch - 1) < .02, state + ': pinch zoom is not native zoom');
  assert.ok(Math.max(g.document, g.body) <= g.client + 2,
    state + ': horizontal body overflow');
  assert.ok(await locator.isVisible(), state + ': target invisible');
  await locator.scrollIntoViewIfNeeded();
  const rect = await locator.boundingBox();
  assert.ok(rect && rect.width > 0 && rect.height > 0 &&
    rect.x >= -2 && rect.x + rect.width <= g.client + 2 &&
    rect.y >= -2 && rect.y + rect.height <= g.height + 2,
    state + ': control clipped or unreachable');
  const hit = await locator.evaluate(el => {
    const r = el.getBoundingClientRect();
    const target = document.elementFromPoint(r.left + r.width/2, r.top + r.height/2);
    return target === el || el.contains(target);
  });
  assert.ok(hit, state + ': target obscured');
  return g;
}
async function checkTabFocus(page, locator, state) {
  // Browser keyboard only; no focus(), click() or forced DOM focus.
  let found = false;
  for (let n = 0; n < 150; n++) {
    await page.keyboard.press('Tab');
    if (await locator.evaluate(el => el === document.activeElement)) {
      found = true;
      break;
    }
  }
  assert.ok(found, state + ': control unreachable by Tab');
  const visible = await locator.evaluate(el => {
    const style = getComputedStyle(el);
    const r = el.getBoundingClientRect();
    return el.matches(':focus-visible') &&
      style.visibility !== 'hidden' && style.display !== 'none' &&
      r.width > 0 && r.height > 0 &&
      r.left >= -2 && r.right <= document.documentElement.clientWidth + 2 &&
      r.top >= -2 && r.bottom <= innerHeight + 2;
  });
  assert.ok(visible, state + ': keyboard focus not visible at 200%');
}
async function assertState(page, state) {
  const card = page.locator('app-empresa-configuracion-tenant-card');
  if (state === 'loading') {
    const status = card.getByRole('status').filter({ hasText: /Cargando configuración tenant/ });
    await status.waitFor({ state: 'visible' });
    return { target: page.getByRole('link', { name: 'Ver todos los ajustes' }), status };
  }
  if (state === 'success') {
    const form = page.getByRole('form', { name: 'Configuración del tenant activo' });
    await form.waitFor({ state: 'visible' });
    assert.ok(await form.locator('input[formcontrolname="nombre"]').isVisible());
    return { target: form.locator('input[formcontrolname="nombre"]'), status: form };
  }
  const kind = { empty: 'empty', error: 'unavailable', permissions: 'forbidden' }[state];
  const status = card.locator('[data-config-state="' + kind + '"]');
  await status.waitFor({ state: 'visible' });
  assert.equal(await page.getByRole('form', { name: 'Configuración del tenant activo' }).count(), 0,
    state + ': old writable form leaked');
  return { target: status.getByRole('button', { name: 'Reintentar consulta' }), status };
}
async function runState(page, worker, state) {
  let releaseLoading;
  if (state === 'loading') {
    await page.route('**' + apiPath, async route => {
      if (!isTenantGet(route.request())) return route.continue();
      await new Promise(resolve => { releaseLoading = resolve; });
      try { await route.continue(); } catch { /* navigation cleanup */ }
    });
  } else if (state !== 'success') {
    const fixtures = {
      empty: { status: 200, body: { success: true, data: null } },
      error: { status: 503, body: { message: 'PRIVATE_SERVER_DETAIL_NOT_FOR_UI' } },
      permissions: { status: 403, body: { message: 'PRIVATE_PERMISSION_DETAIL_NOT_FOR_UI' } },
    };
    const fixture = fixtures[state];
    await page.route('**' + apiPath, async route => {
      if (!isTenantGet(route.request())) return route.continue();
      await route.fulfill({ status: fixture.status, contentType: 'application/json',
        body: JSON.stringify(fixture.body) });
    });
  }
  await page.goto(base + '/configuracion/dinamica-v5');
  const { target, status } = await assertState(page, state);
  assert.ok(await status.isVisible(), state + ': diagnostic state missing');
  assert.ok(await target.isEnabled(), state + ': actionable control disabled');
  const baseline = await geometry(page, target, state + '-100');
  assert.ok(Math.abs(baseline.dpr - 1) < .1, 'baseline must be 100%');
  await physicalScreenshot(page, state + '-100.png');
  assert.equal(await setNativeZoom(worker, page, 2), 2);
  await page.waitForFunction(d => Math.abs(devicePixelRatio / d - 2) < .05, baseline.dpr);
  const native = await geometry(page, target, state + '-200');
  assert.ok(native.viewport < baseline.viewport && Math.abs(native.dpr/baseline.dpr - 2) < .05,
    state + ': viewport did not reflow on native browser zoom');
  await physicalScreenshot(page, state + '-200.png');
  await checkTabFocus(page, target, state);
  await geometry(page, target, state + '-focused');
  await physicalScreenshot(page, state + '-200-focus.png');
  if (state === 'error' || state === 'permissions') {
    assert.ok(!(await page.locator('body').innerText()).includes('PRIVATE_'),
      state + ': backend detail leaked');
  }
  if (state === 'success') {
    assert.ok(await target.isEditable(), 'success: field not usable');
  } else if (state !== 'loading') {
    // Verify that a retry control is interactable, without mutating persistence.
    await target.click();
  }
  results.push({ state, nativeZoom: 2, baseline, native, focusVisible: true,
    captures: [state + '-100.png', state + '-200.png', state + '-200-focus.png'],
    fixture: ['error', 'permissions', 'empty'].includes(state) ? 'injected UI HTTP response' : 'real JWT/MySQL UI' });
  if (releaseLoading) releaseLoading();
  await page.unrouteAll({ behavior: 'ignoreErrors' });
  await setNativeZoom(worker, page, 1);
}

try {
  await mkdir(extension, { recursive: true });
  await mkdir(output, { recursive: true });
  await writeFile(join(extension, 'manifest.json'), JSON.stringify({
    manifest_version: 3, name: 'SOLQARYN M04.02 native zoom evidence',
    version: '1.0.0', permissions: ['tabs'],
    background: { service_worker: 'background.js' }
  }));
  await writeFile(join(extension, 'background.js'), 'chrome.runtime.onInstalled.addListener(() => {});');
  const context = await chromium.launchPersistentContext(join(root, 'profile'), {
    headless: false, viewport: null, args: ['--window-size=1440,1000',
      '--disable-extensions-except=' + extension, '--load-extension=' + extension]
  });
  try {
    const worker = context.serviceWorkers()[0] ??
      await context.waitForEvent('serviceworker', { timeout: 15000 });
    const page = context.pages()[0] ?? await context.newPage();
    await page.goto(base + '/login');
    await page.getByRole('textbox', { name: 'Nombre de usuario' }).fill('c1504_admin_a');
    await page.locator('input[formcontrolname="password"]').fill(process.env.C1504_TEST_PASSWORD);
    const authPending = page.waitForResponse(r => r.url().endsWith('/auth/login') &&
      r.request().method() === 'POST');
    await page.getByRole('button', { name: /Ingresar al sistema/ }).click();
    const auth = await authPending;
    assert.equal(auth.status(), 200);
    const payload = await auth.json();
    assert.ok(payload?.data?.token?.split('.').length === 3);
    const bearer = { Authorization: 'Bearer ' + payload.data.token };
    const membership = await page.request.get('http://127.0.0.1:5005/tenant-context/1',
      { headers: bearer });
    assert.equal(membership.status(), 200, 'real tenant context required');
    await page.evaluate(() => localStorage.setItem('solqaryn_empresa_solicitada_id', '1'));
    for (const state of ['loading', 'empty', 'error', 'success', 'permissions']) {
      await runState(page, worker, state);
      console.log('M0402_NATIVE_STATE_PASS ' + state);
    }
    assert.equal(results.length, 5);
    console.log('M0402_NATIVE_200_ALL_5_PASS screenshots=15');
  } finally { await context.close(); }
} finally {
  await writeFile(join(output, 'metrics.json'), JSON.stringify({
    sha: process.env.GITHUB_SHA,
    mechanism: 'chrome.tabs.setZoom/getZoom + physical X11 capture',
    note: '100% here is same-current-version baseline, NOT historical BEFORE',
    testScope: 'frontend state presentation; injected 403/503 do not certify backend authorization',
    results
  }, null, 2));
  await rm(root, { recursive: true, force: true });
}
