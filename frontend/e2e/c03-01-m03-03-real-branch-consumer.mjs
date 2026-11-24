// C03-01.M03.03: actual Angular profile consumer, real signed JWT and MySQL A/B.
// No intercepted responses, no persisted contextual token, no production access.
import { chromium, expect } from '@playwright/test';
import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { mkdir, writeFile } from 'node:fs/promises';
import { resolve, join } from 'node:path';

const origin = 'http://127.0.0.1:4200';
const api = process.env.P4_API_URL;
const password = process.env.P4_TEST_PASSWORD;
const container = process.env.C0301_MYSQL_CONTAINER;
assert.ok(process.env.CI === 'true' && process.env.GITHUB_SHA &&
  api === 'http://127.0.0.1:5005' && password &&
  /^[a-f0-9]{12,64}$/.test(container || ''), 'Requires isolated GitHub-hosted P4 CI');
const out = resolve('c0301-branch-browser-evidence');
await mkdir(out, { recursive: true });
const results = [];
const matches = (response, path, method) => {
  const url = new URL(response.url());
  return ['127.0.0.1', 'localhost'].includes(url.hostname) && url.port === '5005' &&
    url.pathname === path && response.request().method() === method;
};
const sql = statement => execFileSync('docker', ['exec', container, 'mysql',
  '-uroot', '-proot', 'solqaryn_p4_http_ab_ci', '-Nse', statement],
  { encoding: 'utf8', timeout: 15000 }).trim();
const fixture = [
  { tenant: 1, username: 'p4_admin_a', code: 'P4-C0301-BROWSER-A' },
  { tenant: 2, username: 'p4_admin_b', code: 'P4-C0301-BROWSER-B' }
];
const ids = fixture.map(f => {
  const raw = sql("SELECT Id FROM Sucursales WHERE EmpresaId=" + f.tenant +
    " AND Codigo='" + f.code + "' AND Activa=1 AND Eliminado=0");
  assert.ok(/^[1-9][0-9]*$/.test(raw), 'Expected one active synthetic branch');
  return Number(raw);
});
assert.notEqual(ids[0], ids[1]);
const browser = await chromium.launch({ headless: true });
const contexts = [];
try {
  for (let i = 0; i < fixture.length; i++) {
    const f = fixture[i], own = ids[i], foreign = ids[1-i];
    const context = await browser.newContext({ viewport: { width: 1280, height: 900 } });
    contexts.push(context);
    const page = await context.newPage();
    await page.goto(origin + '/login');
    await page.getByRole('textbox', { name: 'Nombre de usuario' }).fill(f.username);
    await page.locator('input[formcontrolname="password"]').fill(password);
    const authenticated = page.waitForResponse(r => matches(r, '/auth/login', 'POST'));
    const verified = page.waitForResponse(r => matches(r, '/tenant-context/' + f.tenant, 'GET'));
    await page.getByRole('button', { name: /Ingresar al sistema/ }).click();
    const login = await authenticated;
    assert.equal(login.status(), 200, 'Real UI login');
    const jwt = (await login.json())?.data?.token;
    assert.ok(typeof jwt === 'string' && jwt.split('.').length === 3, 'Signed base session');
    const membership = await verified;
    assert.equal(membership.status(), 200, 'Server verified unique tenant membership');
    assert.equal((await membership.json())?.data?.empresaId, f.tenant);
    await page.waitForFunction(n => localStorage.getItem('solqaryn_empresa_solicitada_id') === String(n), f.tenant);
    await page.goto(origin + '/perfil');
    const section = page.locator('section[aria-labelledby="sucursal-contextual-titulo"]');
    await expect(section).toBeVisible({ timeout: 30000 });
    const list = page.waitForResponse(r => matches(r, '/tenant-context/sesion/sucursales/' + f.tenant, 'GET'));
    await section.getByRole('button', { name: 'Consultar opciones' }).click();
    const listResponse = await list;
    assert.equal(listResponse.status(), 200);
    const options = (await listResponse.json())?.data;
    assert.ok(Array.isArray(options) && options.length > 0 && options.every(x => x.empresaId === f.tenant));
    assert.ok(options.some(x => x.id === own) && !options.some(x => x.id === foreign),
      'Only own real branch appears in server selection');
    const selector = section.locator('select#sucursal-contextual');
    await expect(selector.locator('option[value="' + own + '"]')).toHaveCount(1);
    await expect(selector.locator('option[value="' + foreign + '"]')).toHaveCount(0);
    await selector.selectOption(String(own));
    const button = section.getByRole('button', { name: 'Verificar acceso' });
    const issued = page.waitForResponse(r => matches(r, '/tenant-context/sesion', 'POST'));
    const read = page.waitForResponse(r => matches(r, '/tenant-context/sesion/sucursal', 'GET'));
    await button.click();
    const issuedResponse = await issued;
    assert.equal(issuedResponse.status(), 200);
    const scoped = (await issuedResponse.json())?.data;
    assert.ok(scoped?.empresaId === f.tenant && scoped?.sucursalId === own &&
      typeof scoped.token === 'string' && scoped.token.split('.').length === 3 &&
      scoped.token !== jwt, 'Scoped credential distinct from base JWT');
    const resourceResponse = await read;
    assert.equal(resourceResponse.status(), 200);
    const resource = (await resourceResponse.json())?.data;
    assert.ok(resource?.id === own && resource?.empresaId === f.tenant && resource?.codigo === f.code);
    const headers = await resourceResponse.request().allHeaders();
    // Do not print or serialize tokens even when an assertion fails.
    assert.ok(headers.authorization === 'Bearer ' + scoped.token, 'Scoped bearer reached actual API');
    assert.equal(headers['x-empresa-id'], String(f.tenant));
    assert.equal(headers['x-sucursal-id'], String(own));
    const confirmation = section.getByRole('status').filter({ hasText: 'Acceso confirmado en esta consulta:' });
    await expect(confirmation).toContainText(f.code);
    assert.ok(await page.evaluate(t => localStorage.getItem('solqaryn_token') === t, jwt),
      'Contextual token must never replace base session');
    assert.ok(await page.evaluate(t => ![...Object.values(localStorage), ...Object.values(sessionStorage)].includes(t), scoped.token),
      'Contextual token must never be persisted');
    await section.screenshot({ path: join(out, 'tenant-' + f.tenant + '-success.png') });

    // Adversarial request changes ONLY the outgoing selection to the other tenant's
    // real branch. The real HTTP API, not a mock, must reject it and Angular must clear success.
    let modified = 0;
    const attack = async route => {
      const request = route.request();
      const u = new URL(request.url());
      if (u.port !== '5005' || u.pathname !== '/tenant-context/sesion' || request.method() !== 'POST')
        return route.continue();
      const payload = request.postDataJSON();
      assert.ok(payload?.empresaId === f.tenant && payload?.sucursalId === own, 'Real consumer submission expected');
      modified++;
      return route.continue({ postData: JSON.stringify({ empresaId: f.tenant, sucursalId: foreign }) });
    };
    await page.route('**/tenant-context/sesion', attack);
    try {
      const rejection = page.waitForResponse(r => matches(r, '/tenant-context/sesion', 'POST'));
      await button.click();
      const denied = await rejection;
      assert.equal(denied.status(), 403, 'Foreign branch must be rejected by real JWT/RBAC');
      assert.equal(modified, 1);
      await expect(section.getByRole('alert')).toContainText('No se pudo confirmar el acceso');
      await expect(confirmation).toHaveCount(0);
      assert.ok(await page.evaluate(t => localStorage.getItem('solqaryn_token') === t, jwt),
        'A scoped denial must not replace or terminate the base session');
    } finally {
      await page.unroute('**/tenant-context/sesion', attack);
    }

    // Change actual MySQL authority while this browser retains a valid JWT and
    // stale branch options. Verify the previously issued scoped credential and
    // the next real UI submission are both denied, with no client-side fallback.
    const deactivate = "UPDATE Sucursales SET Activa=0 WHERE Id=" + own +
      " AND EmpresaId=" + f.tenant + " AND Codigo='" + f.code + "' AND Activa=1; SELECT ROW_COUNT();";
    assert.equal(sql(deactivate), '1', 'Exactly one synthetic branch revoked');
    try {
      const previous = await page.request.get(api + '/tenant-context/sesion/sucursal', {
        headers: { Authorization: 'Bearer ' + scoped.token, 'X-Empresa-Id': String(f.tenant), 'X-Sucursal-Id': String(own) }
      });
      assert.equal(previous.status(), 403, 'Previously issued contextual JWT denied after real revocation');
      const rejection = page.waitForResponse(r => matches(r, '/tenant-context/sesion', 'POST'));
      await button.click();
      assert.equal((await rejection).status(), 403, 'Stale UI selection denied by current DB authority');
      await expect(section.getByRole('alert')).toContainText('No se pudo confirmar el acceso');
      await expect(confirmation).toHaveCount(0);
      assert.ok(await page.evaluate(t => localStorage.getItem('solqaryn_token') === t, jwt),
        'Branch revocation does not elevate or replace the base JWT');
      await section.screenshot({ path: join(out, 'tenant-' + f.tenant + '-revoked.png') });
    } finally {
      assert.equal(sql("UPDATE Sucursales SET Activa=1 WHERE Id=" + own +
        " AND EmpresaId=" + f.tenant + " AND Codigo='" + f.code + "' AND Activa=0; SELECT ROW_COUNT();"), '1');
    }

    // Manipulate the same browser storage with the same signed base JWT.
    // A tenant hint cannot retain an already verified A/B permission proof.
    const stalePosts = [];
    const observeStale = req => {
      const u = new URL(req.url());
      if (u.port === '5005' && u.pathname === '/tenant-context/sesion' && req.method() === 'POST')
        stalePosts.push(true);
    };
    page.on('request', observeStale);
    try {
      await expect(button).toBeVisible();
      await page.evaluate(other => {
        const target = [...document.querySelectorAll('section[aria-labelledby="sucursal-contextual-titulo"] button')]
          .find(element => element.textContent?.includes('Verificar acceso'));
        if (!target) throw new Error('Expected real contextual button before storage mutation');
        localStorage.setItem('solqaryn_empresa_solicitada_id', String(other));
        target.click();
      }, fixture[1-i].tenant);
      await expect(section).toHaveCount(0);
      assert.equal(stalePosts.length, 0, 'Stale contextual UI must not dispatch a new scoped-token POST');
      assert.ok(await page.evaluate(t => localStorage.getItem('solqaryn_token') === t, jwt),
        'Hint change does not replace the signed base identity');
      assert.equal(await page.evaluate(() => localStorage.getItem('solqaryn_empresa_solicitada_id')), String(fixture[1-i].tenant));
      // Restoring the old hint alone must not revive the old proof.
      await page.evaluate(n => localStorage.setItem('solqaryn_empresa_solicitada_id', String(n)), f.tenant);
      await expect(section).toHaveCount(0);
    } finally {
      page.off('request', observeStale);
    }

    results.push({ tenant: f.tenant, branch: own, uiLogin: 200, list: 200, issue: 200,
      resource: 200, foreignBranch: 403, revokedExistingJWT: 403, revokedSelection: 403,
      baseTokenPreserved: true, scopedTokenNotPersisted: true, staleHintDenied: true });
    console.log('C0301_M0303_REAL_BROWSER_TENANT_' + f.tenant + '_PASS');
  }
  assert.equal(results.length, 2);
} finally {
  try {
    await writeFile(join(out, 'metrics.json'), JSON.stringify({
      sha: process.env.GITHUB_SHA, mechanism: 'real browser UI + signed JWT + ephemeral Oracle EF MySQL 8.4',
      scope: 'profile branch consumer, real cross-tenant rejection and branch revocation; no claim of all ERP routes',
      results
    }, null, 2));
  } finally {
    await Promise.all(contexts.map(c => c.close()));
    await browser.close();
  }
}
