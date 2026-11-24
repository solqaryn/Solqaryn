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
const output = resolve('v24-c0103-m0402-native-zoom');
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
async function physicalScreenshot(page, filename, loadingStatus = null) {
  await page.bringToFront();
  await page.waitForTimeout(3000);
  // A visible DOM node can be below the fold. Check the actual loading evidence
  // at capture time, independently of the keyboard-focus screenshot.
  if (loadingStatus) {
    for (const locator of [loadingStatus, loadingStatus.getByText(/Cargando configuración tenant/),
      loadingStatus.locator('mat-spinner')]) {
      const visible = await locator.evaluate(el => {
        const r = el.getBoundingClientRect(), css = getComputedStyle(el);
        const hit = document.elementFromPoint(r.left + r.width / 2, r.top + r.height / 2);
        return css.visibility !== 'hidden' && css.display !== 'none' &&
          r.width > 0 && r.height > 0 &&
          r.left >= 0 && r.right <= document.documentElement.clientWidth &&
          r.top >= 0 && r.bottom <= innerHeight && (hit === el || el.contains(hit));
      });
      assert.ok(visible, filename + ': loading indicator/message not in captured viewport');
    }
  }
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
  const g = await page.evaluate(() => {
    const root = document.scrollingElement;
    if (!root) throw Error('No scrolling element');
    const x = window.scrollX, y = window.scrollY;
    let left, right;
    try {
      window.scrollTo({ left: 100000, top: y, behavior: 'instant' });
      right = window.scrollX;
      window.scrollTo({ left: -100000, top: y, behavior: 'instant' });
      left = window.scrollX;
    } finally {
      window.scrollTo({ left: x, top: y, behavior: 'instant' });
    }
    return {
      viewport: innerWidth, height: innerHeight, dpr: devicePixelRatio,
      pinch: visualViewport?.scale ?? 1,
      document: document.documentElement.scrollWidth, body: document.body.scrollWidth,
      client: document.documentElement.clientWidth,
      scrollingClient: root.clientWidth, scrollingWidth: root.scrollWidth,
      actualHorizontalTravel: Math.abs(right - left),
      bodyRectWidth: document.body.getBoundingClientRect().width,
    };
  });
  assert.ok(Math.abs(g.pinch - 1) < .02, state + ': pinch zoom is not native zoom');
  assert.ok(Math.max(g.document, g.body) <= g.client + 2,
    state + ': horizontal body overflow');
  // Preserve the original CSSOM check, and additionally require zero real
  // horizontal travel; a rounded one-pixel body measurement alone is inconclusive.
  assert.equal(g.actualHorizontalTravel, 0, state + ': document can scroll horizontally');
  assert.equal(g.scrollingWidth, g.scrollingClient, state + ': scrolling root overflow');
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
  // The loading-state screenshot scrolls to a different part of the page.
  // Native Tab can finish focusing before Chromium finishes its own scrolling.
  // Observe that native scroll, without moving the target or forcing focus.
  const focus = await locator.evaluate(async el => {
    const measure = () => {
      const style = getComputedStyle(el), r = el.getBoundingClientRect();
      const hit = document.elementFromPoint(r.left + r.width / 2, r.top + r.height / 2);
      const active = el === document.activeElement, focusVisible = el.matches(':focus-visible');
      const inViewport = r.width > 0 && r.height > 0 &&
        r.left >= -2 && r.right <= document.documentElement.clientWidth + 2 &&
        r.top >= -2 && r.bottom <= innerHeight + 2;
      const unobscured = hit === el || el.contains(hit);
      return {
        active, focusVisible, inViewport, unobscured,
        visible: active && focusVisible && inViewport && unobscured &&
          style.visibility !== 'hidden' && style.display !== 'none',
        rect: { x:r.x, y:r.y, width:r.width, height:r.height },
        viewport: { width:document.documentElement.clientWidth, height:innerHeight },
        scroll: { x:window.scrollX, y:window.scrollY },
        outlineStyle:style.outlineStyle, outlineWidth:style.outlineWidth,
      };
    };
    const initial = measure();
    const deadline = performance.now() + 2000;
    let settled = initial;
    while (!settled.visible && performance.now() < deadline) {
      await new Promise(resolve => setTimeout(resolve, 50));
      settled = measure();
    }
    return { initial, settled };
  });
  console.log('M0402_NATIVE_TAB_GEOMETRY ' + state + ' ' + JSON.stringify(focus));
  if (!focus.settled.visible) {
    // Preserve the failing physical view without scrolling to the target.
    await physicalScreenshot(page, state + '-200-focus-failure.png');
  }
  assert.ok(focus.settled.visible, state + ': keyboard focus not visible at 200%');
  return focus;
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
    // This step runs before the workflow removes tenant B. The real multi-company
    // policy permits reads, not editing. Audit the actual enabled reload control.
    assert.ok(await form.locator('input[formcontrolname="nombre"]').isDisabled(),
      'multi-company success must remain read-only');
    assert.equal(await form.getByRole('button', { name: 'Guardar configuración tenant' }).count(), 0);
    return { target: card.getByRole('button', { name: /Recargar/ }), status: form };
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
  let intercepted = 0;
  let realReads = 0;
  if (state === 'loading') {
    const held = new Promise(resolve => { releaseLoading = resolve; });
    await page.route('**' + apiPath, async route => {
      if (!isTenantGet(route.request())) return route.continue();
      intercepted++;
      await held;
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
      intercepted++;
      await route.fulfill({ status: fixture.status, contentType: 'application/json',
        body: JSON.stringify(fixture.body) });
    });
  } else {
    await page.route('**' + apiPath, async route => {
      if (isTenantGet(route.request())) realReads++;
      await route.continue();
    });
  }
  await page.goto(base + '/configuracion/dinamica-v5');
  const { target, status } = await assertState(page, state);
  assert.ok(await status.isVisible(), state + ': diagnostic state missing');
  assert.ok(await target.isEnabled(), state + ': actionable control disabled');
  const baseline = await geometry(page, target, state + '-100');
  assert.ok(Math.abs(baseline.dpr - 1) < .1, 'baseline must be 100%');
  if (state === 'loading') await status.scrollIntoViewIfNeeded();
  await physicalScreenshot(page, state + '-100.png', state === 'loading' ? status : null);
  assert.equal(await setNativeZoom(worker, page, 2), 2);
  await page.waitForFunction(d => Math.abs(devicePixelRatio / d - 2) < .05, baseline.dpr);
  const native = await geometry(page, target, state + '-200');
  assert.ok(native.viewport < baseline.viewport && Math.abs(native.dpr/baseline.dpr - 2) < .05,
    state + ': viewport did not reflow on native browser zoom');
  if (state === 'loading') await status.scrollIntoViewIfNeeded();
  await physicalScreenshot(page, state + '-200.png', state === 'loading' ? status : null);
  const focusGeometry = await checkTabFocus(page, target, state);
  await geometry(page, target, state + '-focused');
  await physicalScreenshot(page, state + '-200-focus.png');
  assert.ok(await target.evaluate(el => el === document.activeElement && el.matches(':focus-visible')),
    state + ': focus lost during X11 capture');
  assert.ok(await status.isVisible(), state + ': state lost during evidence capture');
  if (state === 'error' || state === 'permissions') {
    assert.ok(!(await page.locator('body').innerText()).includes('PRIVATE_'),
      state + ': backend detail leaked');
  }
  if (state === 'success') {
    assert.ok(realReads > 0, 'success must reach authenticated real API');
    const beforeReload = realReads;
    const response = page.waitForResponse(r => isTenantGet(r.request()) && r.status() === 200);
    await page.keyboard.press('Enter');
    await response;
    await assertState(page, state);
    assert.ok(realReads > beforeReload, 'keyboard reload must reach real API');
  } else if (state !== 'loading') {
    assert.ok(intercepted > 0, state + ': response fixture was never exercised');
    const beforeRetry = intercepted;
    const response = page.waitForResponse(r => isTenantGet(r.request()));
    await page.keyboard.press('Enter');
    await response;
    await assertState(page, state);
    assert.ok(intercepted > beforeRetry, state + ': keyboard retry must issue GET');
  } else {
    assert.ok(intercepted > 0, 'loading must hold actual pending GETs');
    assert.ok(await page.locator('app-empresa-configuracion-tenant-card')
      .getByRole('button', { name: /Recargar/ }).isDisabled(), 'loading must disable reload');
    // Let every pending GET finish, then verify recovery without modifying data.
    releaseLoading();
    await assertState(page, 'success');
  }
  results.push({ state, nativeZoom: 2, baseline, native, focusVisible: true, focusGeometry,
    loadingIndicatorCapturedInViewport: state === 'loading' ? true : null,
    captures: [state + '-100.png', state + '-200.png', state + '-200-focus.png'],
    fixture: ['error', 'permissions', 'empty'].includes(state) ? 'injected UI HTTP response' : 'real JWT/MySQL UI' });
  if (releaseLoading) releaseLoading();
  await page.unrouteAll({ behavior: 'wait' });
  await setNativeZoom(worker, page, 1);
}

// C01-03.M04.01: close the missing tenant B native-browser evidence only.
// Reuse the proven M04.02 zoom/focus/physical capture helpers without repeating
// its five-state fixtures. A fresh profile prevents tenant A session carryover.
let tenantB = null;
async function auditTenantB() {
  const profileB = join(root, 'profile-tenant-b');
  await mkdir(join(profileB, 'Default'), { recursive: true });
  await writeFile(join(profileB, 'Default', 'Preferences'), JSON.stringify({
    credentials_enable_service: false, profile: { password_manager_enabled: false }
  }));
  const contextB = await chromium.launchPersistentContext(profileB, {
    headless: false, viewport: null, args: ['--window-size=1440,1000',
      '--disable-extensions-except=' + extension, '--load-extension=' + extension]
  });
  try {
    const worker = contextB.serviceWorkers()[0] ??
      await contextB.waitForEvent('serviceworker', { timeout: 15000 });
    const page = contextB.pages()[0] ?? await contextB.newPage();
    await page.goto(base + '/login');
    await page.getByRole('textbox', { name: 'Nombre de usuario' }).fill('c1504_admin_b');
    await page.locator('input[formcontrolname="password"]').fill(process.env.C1504_TEST_PASSWORD);
    const pending = page.waitForResponse(r => r.url().endsWith('/auth/login') &&
      r.request().method() === 'POST');
    await page.getByRole('button', { name: /Ingresar al sistema/ }).click();
    const login = await pending;
    assert.equal(login.status(), 200);
    const token = (await login.json())?.data?.token;
    assert.ok(typeof token === 'string' && token.split('.').length === 3);
    const headers = { Authorization: 'Bearer ' + token };
    const api = 'http://127.0.0.1:5005';
    const membership = await page.request.get(api + '/tenant-context/2', { headers });
    assert.equal(membership.status(), 200);
    const policy = await page.request.get(api + '/permisos/mis-permisos/empresa/2', { headers });
    assert.equal(policy.status(), 200);
    const grants = (await policy.json())?.data;
    assert.ok(grants?.permisos?.includes('Configuracion:Ver'), 'B must have actual read grant');
    await page.waitForFunction(() => Boolean(localStorage.getItem('solqaryn_token')));
    await page.evaluate(() => localStorage.setItem('solqaryn_empresa_solicitada_id', '2'));
    const isBGet = r => {
      const u = new URL(r.url());
      return ['127.0.0.1', 'localhost'].includes(u.hostname) && u.port === '5005'
        && u.pathname === '/empresa-configuracion/tenant/2' && r.request().method() === 'GET'
        && r.status() === 200;
    };
    const initialGet = page.waitForResponse(isBGet);
    await page.goto(base + '/configuracion/dinamica-v5');
    const response = await initialGet;
    const config = (await response.json())?.data;
    assert.equal(config?.empresaId, 2, 'verified server tenant B only');
    const form = page.getByRole('form', { name: 'Configuración del tenant activo' });
    await form.waitFor({ state: 'visible' });
    const tenantLabel = form.getByText('Empresa #2', { exact: true });
    await tenantLabel.waitFor({ state: 'visible' });
    const name = form.locator('input[formcontrolname="nombre"]');
    assert.equal(await name.inputValue(), config.nombre);
    assert.ok(await name.isDisabled(), 'B multi-tenant configuration stays read-only');
    assert.equal(await form.getByRole('button', { name: 'Guardar configuración tenant' }).count(), 0);
    const target = page.locator('app-empresa-configuracion-tenant-card')
      .getByRole('button', { name: /Recargar/ });
    assert.ok(await target.isEnabled());
    const zoom100 = await setNativeZoom(worker, page, 1);
    await page.waitForFunction(() => Math.abs(devicePixelRatio - 1) < .1);
    const baseline = await geometry(page, target, 'tenant-B-100');
    await tenantLabel.scrollIntoViewIfNeeded();
    await physicalScreenshot(page, 'tenant-B-100.png');
    const zoom200 = await setNativeZoom(worker, page, 2);
    await page.waitForFunction(d => Math.abs(devicePixelRatio / d - 2) < .05, baseline.dpr);
    const native = await geometry(page, target, 'tenant-B-200');
    assert.ok(native.viewport < baseline.viewport && Math.abs(native.dpr / baseline.dpr - 2) < .05);
    await tenantLabel.scrollIntoViewIfNeeded();
    await physicalScreenshot(page, 'tenant-B-200.png');
    const focusGeometry = await checkTabFocus(page, target, 'tenant-B');
    await geometry(page, target, 'tenant-B-focused');
    await physicalScreenshot(page, 'tenant-B-200-focus.png');
    assert.ok(await target.evaluate(el => el === document.activeElement && el.matches(':focus-visible')));
    const reload = page.waitForResponse(isBGet);
    await page.keyboard.press('Enter');
    assert.equal((await (await reload).json())?.data?.empresaId, 2);
    await tenantLabel.waitFor({ state: 'visible' });
    assert.ok(await name.isDisabled());
    tenantB = { scope: 'C01-03.M04.01', verifiedTenant: 2, zoom100, zoom200,
      baseline, native, focusGeometry, keyboardReload: true,
      captures: ['tenant-B-100.png', 'tenant-B-200.png', 'tenant-B-200-focus.png'] };
    console.log('M0401_NATIVE_TENANT_B_PASS screenshots=3');
  } finally { await contextB.close(); }
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
  const profile = join(root, 'profile');
  await mkdir(join(profile, 'Default'), { recursive: true });
  await writeFile(join(profile, 'Default', 'Preferences'), JSON.stringify({
    credentials_enable_service: false, profile: { password_manager_enabled: false }
  }));
  const context = await chromium.launchPersistentContext(profile, {
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
  await auditTenantB();
} finally {
  await writeFile(join(output, 'metrics.json'), JSON.stringify({
    sha: process.env.GITHUB_SHA,
    mechanism: 'chrome.tabs.setZoom/getZoom + physical X11 capture',
    note: '100% here is same-current-version baseline, NOT historical BEFORE',
    testScope: 'frontend state presentation; injected 403/503 do not certify backend authorization',
    results, tenantB
  }, null, 2));
  await rm(root, { recursive: true, force: true });
}
