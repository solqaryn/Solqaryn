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
  && password && process.env.DISPLAY, 'Requires isolated local CI and X11');
const root = await mkdtemp(join(tmpdir(), 'c1504-private-native-'));
const extension = join(root, 'extension');
const output = resolve('test-results/c1504-private-native-zoom');
const records = [];
const focusFailures = [];
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
  async function capture(factor, g, focusedAnchor = null) {
    const anchor = focusedAnchor ?? (name === 'global-ux' ? locator.getByRole('heading').first() : locator);
    // Do not programmatically scroll the post-Tab capture: browser keyboard navigation must reveal its own focus.
    if (!focusedAnchor) await anchor.scrollIntoViewIfNeeded();
    await page.bringToFront();
    // Chrome's native zoom bubble is outside the page DOM and transient.
    // Wait for it to dismiss before taking the actual desktop screenshot.
    await page.waitForTimeout(3000);
    const visible = await anchor.evaluate(el => {
      const r = el.getBoundingClientRect();
      const x = Math.max(0, Math.min(innerWidth - 1, r.left + r.width / 2));
      const y = Math.max(0, Math.min(innerHeight - 1, r.top + r.height / 2));
      const hit = document.elementFromPoint(x, y);
      return r.top >= 0 && r.bottom <= innerHeight && !!hit && (hit === el || el.contains(hit));
    });
    assert.ok(visible, name + ': capture anchor is vertically visible and unobscured in the page');
    await page.evaluate(() => new Promise(done => requestAnimationFrame(() => requestAnimationFrame(done))));
    const filename = name + '-' + (factor * 100) + (focusedAnchor ? '-focus' : '') + '.png';
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
  // Preserve the first keyboard-selected element. Natural CSS transition or
  // browser scrolling can take a frame; do not select another element, force
  // focus, scroll programmatically, or accept an obscured target.
  const beforeTab = await page.evaluate(() => document.activeElement?.tagName ?? null);
  await page.keyboard.press('Tab');
  const target = await page.evaluateHandle(() => document.activeElement);
  const snapshot = el => {
    if (!(el instanceof HTMLElement)) return { valid: false, tag: null };
    const css = getComputedStyle(el), r = el.getBoundingClientRect();
    const x = r.left + r.width / 2, y = r.top + r.height / 2;
    const hit = document.elementFromPoint(x, y);
    const same = document.activeElement === el;
    const keyboard = el.matches(':focus-visible');
    const within = r.width > 0 && r.height > 0 && r.top >= 0
      && r.bottom <= innerHeight && r.left >= -1
      && r.right <= document.documentElement.clientWidth + 1;
    const unobscured = !!hit && (hit === el || el.contains(hit));
    return {
      tag: el.tagName, id: el.id, role: el.getAttribute('role'),
      same, keyboard, within, unobscured,
      rect: { x: r.x, y: r.y, width: r.width, height: r.height },
      viewport: { width: innerWidth, height: innerHeight },
      visibility: css.visibility, display: css.display,
      outlineStyle: css.outlineStyle, outlineWidth: css.outlineWidth,
      activeTag: document.activeElement?.tagName ?? null,
      hitTag: hit?.tagName ?? null,
      valid: same && keyboard && within && unobscured && el !== document.body
        && el !== document.documentElement && css.visibility !== 'hidden'
        && css.display !== 'none'
    };
  };
  const initial = await target.evaluate(snapshot);
  let final = initial;
  let focusImage200;
  try {
    // Poll only the original target for up to one second. The test still
    // fails if the original focus is missing, moves, or stays clipped.
    for (let attempt = 0; attempt < 10 && !final.valid && final.same && final.keyboard; attempt++) {
      await page.waitForTimeout(100);
      final = await target.evaluate(snapshot);
    }
    assert.ok(final.valid, name + ': real visible keyboard focus missing or clipped at 200%');
    const focusedAnchor = page.locator(':focus-visible');
    assert.equal(await focusedAnchor.count(), 1, name + ': unique actual focus-visible target required');
    focusImage200 = await capture(2, after, focusedAnchor);
    assert.equal(await focusedAnchor.count(), 1, name + ': focus was lost during physical capture');
    final = await target.evaluate(snapshot);
    assert.ok(final.valid, name + ': original keyboard focus changed or was obscured during capture');
  } catch (error) {
    const failureImage = name + '-200-focus-failure.png';
    // Save authentic physical failure evidence, including cases where the
    // element is absent or occluded. This never converts a failure to PASS.
    try {
      await exec('ffmpeg', ['-nostdin', '-hide_banner', '-loglevel', 'error',
        '-f', 'x11grab', '-draw_mouse', '0', '-video_size', '1600x1200',
        '-i', process.env.DISPLAY, '-frames:v', '1', '-threads', '1', '-y', join(output, failureImage)],
        { timeout: 15000 });
    } catch (captureError) {
      console.error(name + ': failure screenshot unavailable: ' + captureError.message);
    }
    focusFailures.push({ name, beforeTab, initial, final, failureImage, error: error.message });
    console.error('C1504_NATIVE_FOCUS_FAILURE ' + JSON.stringify(focusFailures.at(-1)));
    throw error;
  } finally {
    await target.dispose();
  }
  records.push({ name, actual100, actual200, before, after, bounds, image100, image200,
    focus: final.valid, focusImage200, focusEvidence: { beforeTab, initial, final } });
  await zoom(1);
}

async function actor(user, tenant, run) {
  // CI-only browser profile: prevent the browser's credential-save UI from
  // covering the physical screenshot. Application authentication is unchanged.
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
    // Isolate the audit navigation from outstanding post-login requests.
    // The new tab uses the same authenticated, actor-specific browser profile.
    const auditPage = await context.newPage();
    await page.close();
    await run(auditPage, worker);
  } finally {
    await context.close();
  }
}

try {
  await mkdir(extension);
  await mkdir(output, { recursive: true });
  await writeFile(join(extension, 'manifest.json'), JSON.stringify({
    manifest_version: 3, name: 'SOLQARYN isolated native zoom evidence', version: '1.0.0',
    permissions: ['tabs'], background: { service_worker: 'background.js' }
  }));
  await writeFile(join(extension, 'background.js'), 'chrome.runtime.onInstalled.addListener(() => {});');
  await actor('c1504_platform_only', null, async (page, worker) => {
    const check = page.waitForResponse(r => apiResponse(r, '/plataforma/sesion', 'GET'));
    await page.goto(origin + '/plataforma/auditoria-ux-v5');
    assert.equal((await check).status(), 200);
    await audit(page, worker, 'global-ux', page.getByTestId('ux-audit-v19'));
  });
  for (const tenant of ['1', '2']) {
    await actor(tenant === '1' ? 'c1504_admin_a' : 'c1504_admin_b', tenant, async (page, worker) => {
      const check = page.waitForResponse(r => apiResponse(r, '/tenant-context/' + tenant, 'GET'));
      await page.goto(origin + '/perfil');
      const response = await check;
      assert.equal(response.status(), 200);
      // Chromium can discard the DevTools response body during navigation.
      // Keep the actual UI response status, and independently verify the same
      // real endpoint with the exact Authorization header of that UI request.
      const authorization = await response.request().headerValue('authorization');
      assert.match(authorization ?? '', /^Bearer \S+$/);
      const contract = await page.request.get(response.url(), {
        headers: { Authorization: authorization }
      });
      try {
        assert.equal(contract.status(), 200, 'Real authenticated tenant contract');
        assert.equal((await contract.json())?.data?.empresaId, Number(tenant));
      } finally {
        await contract.dispose();
      }
      await audit(page, worker, 'tenant-' + tenant + '-profile', page.getByRole('heading', { name: 'Mi perfil' }));
      if (tenant === '2') {
        const denied = page.waitForResponse(r => apiResponse(r, '/plataforma/sesion', 'GET'));
        await page.goto(origin + '/plataforma/auditoria-ux-v5');
        assert.equal((await denied).status(), 403);
        await page.waitForURL(/\/login\?plataforma=denegado/);
        assert.equal(await page.getByTestId('ux-audit-v19').count(), 0);
        // Authenticated denial renders the tenant selector and explicit alert,
        // not the anonymous credentials form. Preserve the real 403 and guard.
        const denial = page.locator('.plataforma-error[role="alert"]');
        await denial.waitFor({ state: 'visible', timeout: 20000 });
        assert.match(await denial.innerText(), /Tu sesión no posee autoridad global de SOLQARYN/);
        assert.equal(await page.locator('#login-title').count(), 0);
        await audit(page, worker, 'tenant-2-global-denied', denial);
        assert.match(page.url(), /\/login\?plataforma=denegado/);
        assert.match(await denial.innerText(), /Tu sesión no posee autoridad global de SOLQARYN/);
        assert.equal(await page.getByTestId('ux-audit-v19').count(), 0);
      }
    });
  }
  assert.equal(records.length, 4);
} finally {
  await writeFile(join(output, 'metrics.json'), JSON.stringify({
    sha: process.env.GITHUB_SHA, mechanism: 'chrome.tabs.setZoom/getZoom + X11 physical desktop',
    scope: 'private global UX, own tenant A/B profile and B global denial; synthetic CI identities only; not whole-product or historical before/after certification',
    records, focusFailures
  }, null, 2));
  await rm(root, { recursive: true, force: true });
}
