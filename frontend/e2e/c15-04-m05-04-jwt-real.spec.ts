import { expect, test, type Page } from '@playwright/test';

// C15-04.M05.04 R2: navegador, JWT, API y MySQL reales SOLO en CI efimera.
// No usa page.route, interceptores ni localStorage con tokens fabricados.
const api = process.env['C1504_API_URL'];
const clave = process.env['C1504_TEST_PASSWORD'];
if (process.env['CI'] !== 'true' || api !== 'http://127.0.0.1:5005' || !clave) {
  throw new Error('Requiere CI isolada con API local y password efimero.');
}

async function ingresar(page: Page, usuario: string): Promise<string> {
  await page.goto('/login');
  await page.getByRole('textbox', { name: 'Nombre de usuario' }).fill(usuario);
  await page.locator('input[formcontrolname="password"]').fill(clave!);
  const espera = page.waitForResponse(r => r.url() ===
    'http://localhost:5005/auth/login' && r.request().method() === 'POST');
  await page.getByRole('button', { name: /Ingresar al sistema/ }).click();
  const respuesta = await espera;
  expect(respuesta.status()).toBe(200);
  const data = await respuesta.json();
  const jwt: unknown = data?.data?.token;
  expect(typeof jwt).toBe('string');
  expect((jwt as string).split('.')).toHaveLength(3);
  return jwt as string;
}

test.describe('C15 UX R2 - JWT firmado en MySQL 8.4 descartable', () => {
  test.describe.configure({ retries: 0 });

  test('rol global ingresa a auditoria UX privada responsive', async ({ page }) => {
    await ingresar(page, 'c1504_platform_only');
    for (const width of [375, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      const documento = await page.goto('/plataforma/auditoria-ux-v5');
      expect(documento?.status()).toBe(200);
      expect((await sesion).status()).toBe(200);
      await expect(page).toHaveURL(/\/plataforma\/auditoria-ux-v5$/);
      await expect(page.locator('[data-testid="ux-audit-v19"]')).toBeVisible();
      await expect(page.locator('[data-testid="ux-surface"][data-state="NO_MEDIDO"]')).toHaveCount(5);
      await expect(page.getByRole('heading', { level: 1 })).toHaveCount(1);
      const ancho = await page.evaluate(() => document.documentElement.scrollWidth);
      expect(ancho).toBeLessThanOrEqual(width + 1);
    }
  });

  test('rol empresarial sin autoridad global: 403 y cero datos privados', async ({ page }) => {
    await ingresar(page, 'c1504_admin_a');
    const sesion = page.waitForResponse(r =>
      r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
    await page.goto('/plataforma/auditoria-ux-v5');
    expect((await sesion).status()).toBe(403);
    await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
    await expect(page.locator('[data-testid="ux-audit-v19"]')).toHaveCount(0);
    await expect(page.locator('[data-testid="ux-surface"]')).toHaveCount(0);
  });

  test('miembro A autorizado, B denegado al navegar detalle dinamico', async ({ page, request }) => {
    const jwt = await ingresar(page, 'c1504_admin_a');
    const headers = { Authorization: 'Bearer ' + jwt };
    const propia = await request.get(api + '/tenant-context/1', { headers });
    expect(propia.status()).toBe(200);
    expect((await propia.json())?.data?.empresaId).toBe(1);
    const ajena = await request.get(api + '/tenant-context/2', { headers });
    expect([403, 404]).toContain(ajena.status());

    await page.evaluate(() => localStorage.setItem('solqaryn_empresa_solicitada_id', '1'));
    const tenantA = page.waitForResponse(r =>
      r.url().includes('/tenant-context/1') && r.request().method() === 'GET');
    await page.goto('/perfil');
    expect((await tenantA).status()).toBe(200);
    await expect(page.getByRole('heading', { name: 'Mi perfil' })).toBeVisible();

    await page.evaluate(() => localStorage.setItem('solqaryn_empresa_solicitada_id', '2'));
    const tenantB = page.waitForResponse(r =>
      r.url().includes('/tenant-context/2') && r.request().method() === 'GET');
    await page.goto('/productos/999999/editar');
    expect([403, 404]).toContain((await tenantB).status());
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.getByRole('heading', { name: 'Mi perfil' })).toHaveCount(0);
  });

  test('JWT firmado con firma alterada pierde sesion y pantalla privada', async ({ page, request }) => {
    const jwt = await ingresar(page, 'c1504_platform_only');
    const valido = await request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + jwt }
    });
    expect(valido.status()).toBe(200);
    const partes = jwt.split('.');
    const firma = partes[2];
    const firmaMal = (firma[0] === 'A' ? 'B' : 'A') + firma.slice(1);
    const adulterado = [partes[0], partes[1], firmaMal].join('.');
    const invalido = await request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + adulterado }
    });
    expect([401, 403]).toContain(invalido.status());
    await page.evaluate(token => localStorage.setItem('solqaryn_token', token), adulterado);
    await page.goto('/plataforma/auditoria-ux-v5');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.locator('[data-testid="ux-audit-v19"]')).toHaveCount(0);
  });
  test('WCAG puntual: foco, contraste y rendimiento local UX autenticada 320-1280', async ({ page }) => {
    await ingresar(page, 'c1504_platform_only');
    for (const width of [320, 375, 768, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      await page.goto('/plataforma/auditoria-ux-v5', { waitUntil: 'load' });
      const enlace = page.getByRole('link', { name: 'Volver a administración global' });
      await expect(enlace).toBeVisible();
      let focused = false;
      for (let i = 0; i < 32; i++) {
        await page.keyboard.press('Tab');
        focused = await enlace.evaluate(el => el === document.activeElement);
        if (focused) break;
      }
      expect(focused, 'Tab real alcanza enlace de regreso').toBe(true);
      const outline = await enlace.evaluate(el => parseFloat(getComputedStyle(el).outlineWidth));
      expect(outline).toBeGreaterThan(0);
      const contraste = await page.locator('.ux-surface').first().evaluate(el => {
        const fg = getComputedStyle(el.querySelector('h3')!).color;
        const bg = getComputedStyle(el).backgroundColor;
        const luminancia = (color: string) => {
          const match = color.match(/^rgba?\(\s*(\d+),\s*(\d+),\s*(\d+)/);
          if (!match) throw Error('Color no RGB: ' + color);
          const [r, g, b] = match.slice(1, 4).map(Number).map(v => {
            const n = v / 255;
            return n <= .04045 ? n / 12.92 : ((n + .055) / 1.055) ** 2.4;
          });
          return .2126 * r + .7152 * g + .0722 * b;
        };
        const a = luminancia(fg), b = luminancia(bg);
        return (Math.max(a, b) + .05) / (Math.min(a, b) + .05);
      });
      expect(contraste).toBeGreaterThanOrEqual(4.5);
      const metrics = await page.evaluate(() => {
        const nav = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming | undefined;
        const paint = performance.getEntriesByType('paint').find(entry => entry.name === 'first-contentful-paint');
        return {
          ttfbMs: nav ? Math.round((nav.responseStart - nav.requestStart) * 100) / 100 : null,
          fcpMs: paint ? Math.round(paint.startTime * 100) / 100 : null,
          lcpMs: null,
          cls: null
        };
      });
      expect(metrics.ttfbMs === null || (Number.isFinite(metrics.ttfbMs) && metrics.ttfbMs >= 0)).toBe(true);
      expect(metrics.fcpMs === null || (Number.isFinite(metrics.fcpMs) && metrics.fcpMs >= 0)).toBe(true);
      console.log('C15_AUTH_UX_LOCAL_SAMPLE ' + JSON.stringify({ width, contraste, ...metrics }));
      // Estas muestras localhost no equivalen a CWV RUM ni auditoria WCAG global.
    }
  });


  test('A0.26 - LCP y CLS reales de UX privada JWT en cuatro anchos', async ({ page }) => {
    // Observer antes de cada navegacion: nunca rellenar un metric ausente con cero.
    await page.addInitScript(() => {
      const state = {
        lcpMs: null as number | null,
        cls: 0,
        lcpSupported: PerformanceObserver.supportedEntryTypes.includes('largest-contentful-paint'),
        clsSupported: PerformanceObserver.supportedEntryTypes.includes('layout-shift')
      };
      (window as Window & { __c1504Vitals?: typeof state }).__c1504Vitals = state;
      if (state.lcpSupported) {
        new PerformanceObserver(list => {
          for (const entry of list.getEntries()) {
            state.lcpMs = Math.round(entry.startTime * 100) / 100;
          }
        }).observe({ type: 'largest-contentful-paint', buffered: true });
      }
      if (state.clsSupported) {
        new PerformanceObserver(list => {
          for (const entry of list.getEntries()) {
            const shift = entry as PerformanceEntry & { value: number; hadRecentInput: boolean };
            if (!shift.hadRecentInput) state.cls += shift.value;
          }
        }).observe({ type: 'layout-shift', buffered: true });
      }
    });
    await ingresar(page, 'c1504_platform_only');
    for (const width of [320, 375, 768, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      await page.goto('/plataforma/auditoria-ux-v5', { waitUntil: 'load' });
      await expect(page.locator('[data-testid="ux-audit-v19"]')).toBeVisible();
      await page.waitForTimeout(800);
      const vitals = await page.evaluate(() =>
        (window as Window & { __c1504Vitals?: {
          lcpMs: number | null; cls: number; lcpSupported: boolean; clsSupported: boolean
        } }).__c1504Vitals
      );
      expect(vitals?.lcpSupported, 'LCP soportado por Chromium').toBe(true);
      expect(vitals?.clsSupported, 'CLS soportado por Chromium').toBe(true);
      expect(vitals?.lcpMs, 'LCP ausente no equivale a PASS').not.toBeNull();
      expect(vitals!.lcpMs).toBeGreaterThan(0);
      expect(vitals!.lcpMs).toBeLessThan(10000);
      expect(vitals!.cls, 'CLS de carga sin desplazamientos inesperados').toBeLessThanOrEqual(0.1);
      console.log('C15_AUTH_UX_REAL_LCP_CLS ' + JSON.stringify({ width, ...vitals }));
    }
    // localhost CI es una muestra diagnostica, no Core Web Vitals de produccion.
  });

  test('A0.27 - credenciales erradas rechazan JWT y no revelan UX privada', async ({ page }) => {
    // Integracion sin mocks: POST real a la API efimera respaldada por MySQL CI.
    await page.goto('/login');
    await page.getByRole('textbox', { name: 'Nombre de usuario' }).fill('c1504_admin_a');
    await page.locator('input[formcontrolname="password"]').fill(clave! + '-incorrecta');
    const respuestaPendiente = page.waitForResponse(r =>
      r.url() === 'http://localhost:5005/auth/login' && r.request().method() === 'POST');
    await page.getByRole('button', { name: /Ingresar al sistema/ }).click();
    const respuesta = await respuestaPendiente;
    expect(respuesta.status(), 'El backend debe rechazar contraseña incorrecta').toBe(401);
    await expect(page.getByRole('alert')).toBeVisible();
    await expect(page.locator('[data-testid="ux-audit-v19"]')).toHaveCount(0);
    expect(await page.evaluate(() => localStorage.getItem('solqaryn_token'))).toBeNull();
    await page.goto('/plataforma/auditoria-ux-v5');
    await expect(page).toHaveURL(/\/login(?:[?#]|$)/);
    await expect(page.locator('[data-testid="ux-surface"]')).toHaveCount(0);
    // No confundir error visible y ausencia de token con revocacion global de sesiones.
  });

  test('A0.28 - continuidad de plataforma con JWT real y responsive 320/1280', async ({ page }) => {
    await ingresar(page, 'c1504_platform_only');
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      await page.goto('/plataforma/continuidad-operativa-v5');
      expect((await sesion).status()).toBe(200);
      await expect(page).toHaveURL(/\/plataforma\/continuidad-operativa-v5$/);
      await expect(page.getByRole('heading', { level: 1, name: 'Controles de recuperación y aislamiento' })).toBeVisible();
      await expect(page.getByRole('region', { name: 'Límites de autoridad' })).toBeVisible();
      await expect(page.getByRole('link', { name: 'Regresar a plataforma' })).toBeVisible();
      const ancho = await page.evaluate(() => document.documentElement.scrollWidth);
      expect(ancho, 'No debe desbordar horizontalmente').toBeLessThanOrEqual(width + 1);
    }
  });

  test('A0.29 - rol empresarial recibe 403 y no ve continuidad de plataforma', async ({ page }) => {
    await ingresar(page, 'c1504_admin_a');
    const sesion = page.waitForResponse(r =>
      r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
    await page.goto('/plataforma/continuidad-operativa-v5');
    expect((await sesion).status()).toBe(403);
    await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
    await expect(page.getByRole('heading', { name: 'Controles de recuperación y aislamiento' })).toHaveCount(0);
    await expect(page.getByRole('region', { name: 'Límites de autoridad' })).toHaveCount(0);
    // Backend y guard reales; ninguna respuesta interceptada.
  });


  test('A0.30 - revalidacion de continuidad con JWT firmado y sin mutaciones', async ({ page }) => {
    await ingresar(page, 'c1504_platform_only');
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      await page.goto('/plataforma/continuidad-operativa-v5');
      const boton = page.getByRole('button', { name: 'Revalidar sesión de continuidad' });
      const guia = page.getByRole('region', { name: 'Límites de autoridad' });
      await expect(boton).toBeEnabled();
      await expect(guia).toBeVisible();
      const sesion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      await boton.click();
      const respuesta = await sesion;
      expect(respuesta.status(), 'Revalidacion debe consultar backend autentico').toBe(200);
      await expect(page.locator('section[aria-label="Estado de acceso"]').getByRole('status'))
        .toContainText('Sesión revalidada. Solo consulta documental.');
      await expect(guia).toBeVisible();
      await expect(page.locator('.controles li')).toHaveCount(4);
      await expect(boton).toBeEnabled();
      const ancho = await page.evaluate(() => document.documentElement.scrollWidth);
      expect(ancho).toBeLessThanOrEqual(width + 1);
    }
    // Flujo GET autentico en CI; no acredita revocacion ni WCAG global.
  });

  test('A0.31 - teclado real activa revalidacion JWT sin HTTP mutante', async ({ page }) => {
    await ingresar(page, 'c1504_platform_only');
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      await page.goto('/plataforma/continuidad-operativa-v5');
      const boton = page.getByRole('button', { name: 'Revalidar sesión de continuidad' });
      await expect(boton).toBeVisible();
      await expect(boton).toBeEnabled();

      // La navegacion comienza con Tab real: no se llama .focus() ni .click().
      let alcanzado = false;
      for (let i = 0; i < 64; i++) {
        await page.keyboard.press('Tab');
        alcanzado = await boton.evaluate(el => el === document.activeElement);
        if (alcanzado) break;
      }
      expect(alcanzado, 'El boton de revalidacion debe ser alcanzable con Tab').toBe(true);

      const mutaciones: string[] = [];
      page.on('request', req => {
        if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(req.method())) {
          mutaciones.push(req.method() + ' ' + new URL(req.url()).pathname);
        }
      });
      const sesion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      await page.keyboard.press('Enter');
      expect((await sesion).status(), 'Revalidacion accesible consulta GET real').toBe(200);
      await expect(page.locator('section[aria-label="Estado de acceso"]').getByRole('status'))
        .toContainText('Sesión revalidada. Solo consulta documental.');
      expect(mutaciones, 'La revalidacion no debe enviar solicitudes mutantes').toEqual([]);
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
    }
    // Verifica interaccion puntual teclado/API, no acredita WCAG ni RUM global.
  });


  test('A0.32 - recarga sin token revierte UX privada al login y API rechaza anonimato', async ({ page, request }) => {
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      await ingresar(page, 'c1504_platform_only');
      await page.goto('/plataforma/continuidad-operativa-v5');
      await expect(page.getByRole('heading', {
        name: 'Controles de recuperación y aislamiento'
      })).toBeVisible();

      // Simular perdida de la credencial persistida sin simular HTTP ni falsificar JWT.
      // La recarga reinicializa AuthService desde almacenamiento real del navegador.
      await page.evaluate(() => {
        localStorage.removeItem('solqaryn_token');
        localStorage.removeItem('solqaryn_expira_en');
      });
      await page.reload();
      await expect(page).toHaveURL(/\/login(?:[?#]|$)/);
      await expect(page.getByRole('heading', {
        name: 'Controles de recuperación y aislamiento'
      })).toHaveCount(0);
      await expect(page.getByRole('region', { name: 'Límites de autoridad' })).toHaveCount(0);
      expect(await page.evaluate(() => localStorage.getItem('solqaryn_token'))).toBeNull();
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
    }

    const sinBearer = await request.get(api + '/plataforma/sesion');
    expect(sinBearer.status(), 'API real debe denegar peticion sin JWT').toBe(401);
    // No demuestra revocacion de tokens ya emitidos ni cumplimiento WCAG global.
  });


  test('A0.33 - tres guias de plataforma autorizadas, responsive y lectura real', async ({ page }) => {
    const rutas = [
      { path: '/plataforma/observabilidad-v5', heading: 'Diagnóstico y señales seguras' },
      { path: '/plataforma/auditoria-tenancy-v5', heading: 'Fronteras de aislamiento y evidencia' },
      { path: '/plataforma/respuesta-incidentes-v5', heading: 'Respuesta, revocación y recuperación segura' }
    ];
    await ingresar(page, 'c1504_platform_only');
    const mutaciones: string[] = [];
    page.on('request', req => {
      if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(req.method())) {
        mutaciones.push(req.method() + ' ' + new URL(req.url()).pathname);
      }
    });
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      for (const ruta of rutas) {
        const sesion = page.waitForResponse(r =>
          r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
        const documento = await page.goto(ruta.path);
        expect(documento?.status()).toBe(200);
        expect((await sesion).status(), 'Autorizacion real del backend').toBe(200);
        expect(new URL(page.url()).pathname).toBe(ruta.path);
        await expect(page.getByRole('heading', { level: 1, name: ruta.heading })).toBeVisible();
        await expect(page.locator('main#main-content')).toBeVisible();
        await expect(page.getByRole('main'), 'Un solo landmark principal por pagina').toHaveCount(1);
        expect(await page.evaluate(() => document.documentElement.scrollWidth))
          .toBeLessThanOrEqual(width + 1);
      }
    }
    expect(mutaciones, 'Visitar guias de solo lectura no debe mutar datos').toEqual([]);
    // Muestra de tres superficies, no cobertura WCAG ni produccion global.
  });

  // Cada ruta tiene contexto de navegador aislado y credencial JWT real nueva.
  // Evita que el estado de redireccion de una ruta afecte el login de la siguiente.
  for (const ruta of [
    { path: '/plataforma/observabilidad-v5', heading: 'Diagnóstico y señales seguras', panel: '.observability-guide' },
    { path: '/plataforma/auditoria-tenancy-v5', heading: 'Fronteras de aislamiento y evidencia', panel: '.tenancy-audit' },
    { path: '/plataforma/respuesta-incidentes-v5', heading: 'Respuesta, revocación y recuperación segura', panel: '.incident-guide' }
  ]) {
    test(`A0.34 - empresa A denegada en ${ruta.path}`, async ({ page }) => {
      await ingresar(page, 'c1504_admin_a');
      const sesion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      await page.goto(ruta.path);
      expect((await sesion).status(), 'Backend debe negar autoridad global').toBe(403);
      await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
      await expect(page.getByRole('heading', { level: 1, name: ruta.heading })).toHaveCount(0);
      await expect(page.locator(ruta.panel), 'No filtrar el contenido de la guia').toHaveCount(0);
    });
  }
  // A0.34 son tres pruebas aisladas de API real; no sustituye auditoria tenant B por modulo.

  test('A0.35 - teclado, foco visible y revalidacion JWT en tres guias privadas', async ({ page }) => {
    const rutas = [
      { path: '/plataforma/observabilidad-v5', accion: 'Revalidar acceso de observabilidad' },
      { path: '/plataforma/auditoria-tenancy-v5', accion: 'Revalidar sesión de auditoría de tenancy' },
      { path: '/plataforma/respuesta-incidentes-v5', accion: 'Revalidar sesión de plataforma' }
    ];
    await ingresar(page, 'c1504_platform_only');
    const mutaciones: string[] = [];
    page.on('request', req => {
      if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(req.method())) {
        mutaciones.push(req.method() + ' ' + new URL(req.url()).pathname);
      }
    });
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      for (const ruta of rutas) {
        const documento = await page.goto(ruta.path);
        expect(documento?.status()).toBe(200);
        expect(new URL(page.url()).pathname, 'Ruta privada debe conservar URL autorizada').toBe(ruta.path);
        const boton = page.getByRole('button', { name: ruta.accion, exact: true });
        await expect(boton).toBeVisible();
        await expect(boton).toBeEnabled();
        let foco = false;
        for (let paso = 0; paso < 96; paso++) {
          await page.keyboard.press('Tab');
          foco = await boton.evaluate(el => el === document.activeElement);
          if (foco) break;
        }
        expect(foco, 'Tab real debe alcanzar el control en ' + ruta.path).toBe(true);
        const estilo = await boton.evaluate(el => {
          const s = getComputedStyle(el);
          return { ancho: parseFloat(s.outlineWidth), tipo: s.outlineStyle };
        });
        expect(estilo.ancho, 'Foco visible de al menos 2px').toBeGreaterThanOrEqual(2);
        expect(estilo.tipo).not.toBe('none');
        const respuesta = page.waitForResponse(r =>
          r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
        await page.keyboard.press('Enter');
        expect((await respuesta).status(), 'Revalidacion JWT backend real').toBe(200);
        expect(new URL(page.url()).pathname).toBe(ruta.path);
        expect(await page.evaluate(() => document.documentElement.scrollWidth))
          .toBeLessThanOrEqual(width + 1);
      }
    }
    expect(mutaciones, 'La navegacion y revalidacion son solo lectura').toEqual([]);
    // Muestra R2 en CI descartable; no certifica WCAG ni rendimiento global.
  });

  test('A0.36 - JWT empresa B solo permite tenant B y deniega UX global', async ({ page, request }) => {
    // Identidad empresarial B persistida unicamente en MySQL 8.4 efimero de CI.
    const jwt = await ingresar(page, 'c1504_admin_b');
    const headers = { Authorization: 'Bearer ' + jwt };
    const propia = await request.get(api + '/tenant-context/2', { headers });
    expect(propia.status(), 'B consulta B con JWT real').toBe(200);
    expect((await propia.json())?.data?.empresaId).toBe(2);
    const ajena = await request.get(api + '/tenant-context/1', { headers });
    expect([403, 404], 'B no puede leer datos de A').toContain(ajena.status());

    await page.setViewportSize({ width: 320, height: 840 });
    const sesion = page.waitForResponse(r =>
      r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
    await page.goto('/plataforma/auditoria-ux-v5');
    expect((await sesion).status(), 'B no tiene permiso global').toBe(403);
    await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
    await expect(page.locator('[data-testid="ux-audit-v19"]')).toHaveCount(0);
    await expect(page.locator('[data-testid="ux-surface"]')).toHaveCount(0);
    expect(await page.evaluate(() => document.documentElement.scrollWidth))
      .toBeLessThanOrEqual(321);
    // No afirmar certificacion WCAG o revocacion global con esta muestra.
  });


  // A0.37: tres superficies de plataforma y dos anchos, con una sesión JWT B nueva
  // por visita. API/tenants reales, sin interceptores ni permisos falsificados.
  for (const ruta of [
    { path: '/plataforma/observabilidad-v5', panel: '.observability-guide' },
    { path: '/plataforma/auditoria-tenancy-v5', panel: '.tenancy-audit' },
    { path: '/plataforma/respuesta-incidentes-v5', panel: '.incident-guide' }
  ]) {
    test('A0.37 - empresa B denegada en ' + ruta.path + ' en movil y escritorio', async ({ browser, request }) => {
      for (const width of [320, 1280]) {
        // Aislar cada ancho: el guard puede conservar JWT B tras el 403 y /login
        // redirige sesiones ya autenticadas; reusar page invalida el segundo login.
        const contexto = await browser.newContext({
          baseURL: 'http://127.0.0.1:4200',
          viewport: { width, height: 840 }
        });
        try {
          const page = await contexto.newPage();
          const jwt = await ingresar(page, 'c1504_admin_b');
          const headers = { Authorization: 'Bearer ' + jwt };
          const propia = await request.get(api + '/tenant-context/2', { headers });
          expect(propia.status(), 'Empresa B puede consultar B').toBe(200);
          expect((await propia.json())?.data?.empresaId).toBe(2);
          const ajena = await request.get(api + '/tenant-context/1', { headers });
          expect([403, 404], 'Empresa B no lee datos de A').toContain(ajena.status());
          const sesion = page.waitForResponse(r =>
            r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
          await page.goto(ruta.path);
          expect((await sesion).status(), 'API niega privilegios globales').toBe(403);
          await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
          await expect(page.locator(ruta.panel), 'No filtrar panel privado').toHaveCount(0);
          expect(await page.evaluate(() => document.documentElement.scrollWidth))
            .toBeLessThanOrEqual(width + 1);
        } finally {
          await contexto.close();
        }
      }
    });
  }


  test('A0.38 - empresa B denegada en continuidad global con API real', async ({ browser, request }) => {
    // R2: JWT B emitido por API/MySQL efimero. Contexto aislado por viewport
    // para impedir la reutilizacion de sesiones tras una denegacion 403.
    for (const width of [320, 1280]) {
      const contexto = await browser.newContext({
        baseURL: 'http://127.0.0.1:4200',
        viewport: { width, height: 840 }
      });
      try {
        const page = await contexto.newPage();
        const jwt = await ingresar(page, 'c1504_admin_b');
        const headers = { Authorization: 'Bearer ' + jwt };
        const propia = await request.get(api + '/tenant-context/2', { headers });
        expect(propia.status(), 'B solo accede a su empresa').toBe(200);
        expect((await propia.json())?.data?.empresaId).toBe(2);
        const ajena = await request.get(api + '/tenant-context/1', { headers });
        expect([403, 404], 'B no puede consultar A').toContain(ajena.status());
        const global = await request.get(api + '/plataforma/sesion', { headers });
        expect(global.status(), 'B no dispone del rol global').toBe(403);

        const mutantes: string[] = [];
        page.on('request', r => {
          if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(r.method())) {
            mutantes.push(r.method() + ' ' + r.url());
          }
        });
        const sesion = page.waitForResponse(r =>
          r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
        await page.goto('/plataforma/continuidad-operativa-v5');
        expect((await sesion).status(), 'Guard con GET real rechaza B').toBe(403);
        await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
        await expect(page.getByRole('heading', { name: 'Controles de recuperación y aislamiento' })).toHaveCount(0);
        await expect(page.getByRole('region', { name: 'Límites de autoridad' })).toHaveCount(0);
        expect(mutantes, 'Una denegacion no modifica datos').toEqual([]);
        const ancho = await page.evaluate(() => document.documentElement.scrollWidth);
        expect(ancho, 'Pantalla denegada sin desbordamiento').toBeLessThanOrEqual(width + 1);
      } finally {
        await contexto.close();
      }
    }
    // Prueba de continuidad global especifica; no acredita WCAG o RUM de todo el ERP.
  });


  // A0.39: rutas globales adicionales no incluidas en las 22 pruebas previas.
  // JWT firmado y API/MySQL efimeros, sin page.route ni mocks HTTP.
  test('A0.39 - cobertura y flujo V5 solo para plataforma global en dos anchos', async ({ page }) => {
    await ingresar(page, 'c1504_platform_only');
    const rutas = [
      { path: '/plataforma/cobertura-v5', heading: 'Cobertura V5 · trazabilidad' },
      { path: '/plataforma/flujo-v5', heading: 'Ciclo V5 · estados y controles' }
    ];
    const mutaciones: string[] = [];
    page.on('request', req => {
      if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(req.method())) {
        mutaciones.push(req.method() + ' ' + new URL(req.url()).pathname);
      }
    });
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      for (const ruta of rutas) {
        const sesion = page.waitForResponse(r =>
          r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
        const respuesta = await page.goto(ruta.path);
        expect(respuesta?.status()).toBe(200);
        expect((await sesion).status(), 'API global con JWT firmado').toBe(200);
        await expect(page).toHaveURL(new RegExp(ruta.path.replaceAll('/', '\\/') + '$'));
        await expect(page.getByRole('heading', { level: 1, name: ruta.heading })).toBeVisible();
        await expect(page.getByRole('main'), 'Un unico landmark main').toHaveCount(1);
        await expect(page.locator('section.plataforma')).toBeVisible();
        if (ruta.path.endsWith('/flujo-v5')) {
          await expect(page.getByTestId('release-success')).toBeVisible();
          await expect(page.getByTestId('release-empty')).toHaveAttribute('data-evidence-state', 'vacio');
        } else {
          await expect(page.getByRole('button', { name: 'Alto contraste de cobertura' })).toBeVisible();
        }
        expect(await page.evaluate(() => document.documentElement.scrollWidth))
          .toBeLessThanOrEqual(width + 1);
      }
    }
    expect(mutaciones, 'La consulta V5 no puede crear efectos de escritura').toEqual([]);
  });

  test('A0.39 - empresa B no accede a cobertura ni flujo V5', async ({ browser, request }) => {
    for (const ruta of ['/plataforma/cobertura-v5', '/plataforma/flujo-v5']) {
      for (const width of [320, 1280]) {
        const contexto = await browser.newContext({
          baseURL: 'http://127.0.0.1:4200',
          viewport: { width, height: 840 }
        });
        try {
          const page = await contexto.newPage();
          const jwt = await ingresar(page, 'c1504_admin_b');
          const directa = await request.get(api + '/plataforma/sesion', {
            headers: { Authorization: 'Bearer ' + jwt }
          });
          expect(directa.status(), 'La API debe negar el rol empresarial B').toBe(403);
          const mutaciones: string[] = [];
          page.on('request', req => {
            if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(req.method())) {
              mutaciones.push(req.method() + ' ' + new URL(req.url()).pathname);
            }
          });
          const sesion = page.waitForResponse(r =>
            r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
          await page.goto(ruta);
          expect((await sesion).status()).toBe(403);
          await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
          await expect(page.locator('section.plataforma')).toHaveCount(0);
          await expect(page.getByRole('heading', { name: /Cobertura V5|Ciclo V5/ })).toHaveCount(0);
          expect(mutaciones, 'Denegar acceso no debe mutar datos').toEqual([]);
          expect(await page.evaluate(() => document.documentElement.scrollWidth))
            .toBeLessThanOrEqual(width + 1);
        } finally {
          await contexto.close();
        }
      }
    }
  });

  test('A0.39 - alto contraste de cobertura alcanzable con Tab y activable con Space', async ({ page }) => {
    await ingresar(page, 'c1504_platform_only');
    const sesion = page.waitForResponse(r =>
      r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
    await page.goto('/plataforma/cobertura-v5');
    expect((await sesion).status()).toBe(200);
    const boton = page.getByRole('button', { name: 'Alto contraste de cobertura' });
    await expect(boton).toBeVisible();
    await expect(boton).toHaveAttribute('aria-pressed', 'false');
    let accesible = false;
    for (let intento = 0; intento < 90; intento++) {
      await page.keyboard.press('Tab');
      accesible = await boton.evaluate(elemento => elemento === document.activeElement);
      if (accesible) break;
    }
    expect(accesible, 'El control debe ser alcanzable con Tab real').toBe(true);
    await page.keyboard.press('Space');
    await expect(boton).toHaveAttribute('aria-pressed', 'true');
    await expect(page.locator('.v5-coverage-contrast')).toBeVisible();
    await page.keyboard.press('Space');
    await expect(boton).toHaveAttribute('aria-pressed', 'false');
    await expect(page.locator('.v5-coverage-contrast')).toHaveCount(0);
    // Muestra funcional de teclado: no sustituye contraste WCAG ni auditoria global.
  });


  // A0.41: control adversarial simetrico del tenant A con JWT real.
  // No equivale a certificar todas las rutas ni permisos de produccion.
  test('A0.41 - empresa A con JWT real no accede a cobertura ni flujo global', async ({ browser, request }) => {
    for (const ruta of ['/plataforma/cobertura-v5', '/plataforma/flujo-v5']) {
      for (const width of [320, 1280]) {
        const contexto = await browser.newContext({
          baseURL: 'http://127.0.0.1:4200',
          viewport: { width, height: 840 }
        });
        try {
          const page = await contexto.newPage();
          const jwt = await ingresar(page, 'c1504_admin_a');
          const headers = { Authorization: 'Bearer ' + jwt };
          const propia = await request.get(api + '/tenant-context/1', { headers });
          expect(propia.status(), 'A puede consultar su tenant').toBe(200);
          expect((await propia.json())?.data?.empresaId).toBe(1);
          const ajena = await request.get(api + '/tenant-context/2', { headers });
          expect([403, 404], 'A no puede consultar tenant B').toContain(ajena.status());
          const global = await request.get(api + '/plataforma/sesion', { headers });
          expect(global.status(), 'A no tiene permiso global').toBe(403);

          const mutaciones: string[] = [];
          page.on('request', req => {
            if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(req.method())) {
              mutaciones.push(req.method() + ' ' + new URL(req.url()).pathname);
            }
          });
          const sesion = page.waitForResponse(r =>
            r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
          await page.goto(ruta);
          expect((await sesion).status()).toBe(403);
          await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
          await expect(page.locator('section.plataforma')).toHaveCount(0);
          await expect(page.getByRole('heading', { name: /Cobertura V5|Ciclo V5/ })).toHaveCount(0);
          expect(mutaciones, 'Rol empresarial no debe provocar mutaciones globales').toEqual([]);
          expect(await page.evaluate(() => document.documentElement.scrollWidth))
            .toBeLessThanOrEqual(width + 1);
        } finally {
          await contexto.close();
        }
      }
    }
  });

  // A0.40: un JWT de plataforma con firma adulterada nunca autoriza
  // las rutas globales de cobertura/flujo, aunque el token tenga claims validos.
  // Navegador + API reales en CI con MySQL efimero; sin page.route ni focus().
  test('A0.40 - firma JWT adulterada deniega cobertura y flujo sin filtrar datos', async ({ browser, request }) => {
    for (const ruta of ['/plataforma/cobertura-v5', '/plataforma/flujo-v5']) {
      for (const width of [320, 1280]) {
        const contexto = await browser.newContext({
          baseURL: 'http://127.0.0.1:4200',
          viewport: { width, height: 840 }
        });
        try {
          const page = await contexto.newPage();
          const jwt = await ingresar(page, 'c1504_platform_only');
          const partes = jwt.split('.');
          expect(partes).toHaveLength(3);
          const firma = partes[2];
          const firmaAlterada = (firma[0] === 'A' ? 'B' : 'A') + firma.slice(1);
          const tokenAlterado = [partes[0], partes[1], firmaAlterada].join('.');
          const apiDenegada = await request.get(api + '/plataforma/sesion', {
            headers: { Authorization: 'Bearer ' + tokenAlterado }
          });
          expect([401, 403], 'La API real rechaza la firma alterada').toContain(apiDenegada.status());

          await page.evaluate(token => localStorage.setItem('solqaryn_token', token), tokenAlterado);
          const mutaciones: string[] = [];
          page.on('request', req => {
            if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(req.method())) {
              mutaciones.push(req.method() + ' ' + new URL(req.url()).pathname);
            }
          });
          await page.goto(ruta);
          await expect(page).toHaveURL(/\/login(?:\?|$)/);
          await expect(page.locator('section.plataforma')).toHaveCount(0);
          await expect(page.getByRole('heading', { name: /Cobertura V5|Ciclo V5/ })).toHaveCount(0);
          expect(mutaciones, 'Una firma invalida no debe producir escrituras').toEqual([]);
          expect(await page.evaluate(() => document.documentElement.scrollWidth))
            .toBeLessThanOrEqual(width + 1);
        } finally {
          await contexto.close();
        }
      }
    }
    // Muestra negativa JWT puntual: no certifica la UX completa, WCAG ni RUM.
  });


  // A0.42: dos superficies adicionales del plan, JWT de plataforma real y tenant B.
  // Test de navegador autentico/CI MySQL efimera; no constituye certificacion global.
  test('A0.42 - inventario de rutas y trazabilidad solo plataforma con JWT real', async ({ page }) => {
    const rutas = [
      { path: '/plataforma/inventario-rutas-v19', heading: 'Inventario de programas y rutas declaradas' },
      { path: '/plataforma/trazabilidad-cobertura-v5', heading: 'Revisión de cobertura del plan' }
    ];
    await ingresar(page, 'c1504_platform_only');
    const mutaciones: string[] = [];
    page.on('request', req => {
      if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(req.method())) {
        mutaciones.push(req.method() + ' ' + new URL(req.url()).pathname);
      }
    });
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      for (const ruta of rutas) {
        const sesion = page.waitForResponse(r =>
          r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
        const respuesta = await page.goto(ruta.path);
        expect(respuesta?.status()).toBe(200);
        expect((await sesion).status(), 'JWT de plataforma debe pasar API real').toBe(200);
        expect(new URL(page.url()).pathname).toBe(ruta.path);
        await expect(page.getByRole('heading', { level: 1, name: ruta.heading })).toBeVisible();
        await expect(page.getByRole('main'), 'Un unico landmark main').toHaveCount(1);
        if (ruta.path.endsWith('/inventario-rutas-v19')) {
          await expect(page.getByTestId('c1304-route-catalogo')).toBeVisible();
        } else {
          await expect(page.getByRole('heading', { name: 'Sin certificación automática' })).toBeVisible();
        }
        expect(await page.evaluate(() => document.documentElement.scrollWidth))
          .toBeLessThanOrEqual(width + 1);
      }
    }
    expect(mutaciones, 'Navegacion de catalogos no debe escribir').toEqual([]);
  });

  test('A0.42 - empresa B no puede leer inventario ni trazabilidad global', async ({ browser, request }) => {
    const rutas = [
      { path: '/plataforma/inventario-rutas-v19', heading: 'Inventario de programas y rutas declaradas' },
      { path: '/plataforma/trazabilidad-cobertura-v5', heading: 'Revisión de cobertura del plan' }
    ];
    for (const ruta of rutas) {
      for (const width of [320, 1280]) {
        const contexto = await browser.newContext({
          baseURL: 'http://127.0.0.1:4200',
          viewport: { width, height: 840 }
        });
        try {
          const page = await contexto.newPage();
          const jwt = await ingresar(page, 'c1504_admin_b');
          const headers = { Authorization: 'Bearer ' + jwt };
          const propia = await request.get(api + '/tenant-context/2', { headers });
          expect(propia.status()).toBe(200);
          expect((await propia.json())?.data?.empresaId).toBe(2);
          const ajena = await request.get(api + '/tenant-context/1', { headers });
          expect([403, 404]).toContain(ajena.status());
          const apiGlobal = await request.get(api + '/plataforma/sesion', { headers });
          expect(apiGlobal.status()).toBe(403);

          const mutaciones: string[] = [];
          page.on('request', req => {
            if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(req.method())) {
              mutaciones.push(req.method() + ' ' + new URL(req.url()).pathname);
            }
          });
          const sesion = page.waitForResponse(r =>
            r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
          await page.goto(ruta.path);
          expect((await sesion).status()).toBe(403);
          await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
          await expect(page.getByRole('heading', { level: 1, name: ruta.heading })).toHaveCount(0);
          await expect(page.getByTestId('c1304-route-catalogo')).toHaveCount(0);
          expect(mutaciones, 'Empresa B denegada no debe escribir').toEqual([]);
          expect(await page.evaluate(() => document.documentElement.scrollWidth))
            .toBeLessThanOrEqual(width + 1);
        } finally {
          await contexto.close();
        }
      }
    }
  });


  // A0.43: la raiz autenticada ya expone main#main-content.
  // La guia es una section interna y no puede duplicar ese landmark.
  test('A0.43 - guia de amenazas con JWT plataforma y un solo landmark principal', async ({ page }) => {
    await ingresar(page, 'c1504_platform_only');
    const mutaciones: string[] = [];
    page.on('request', req => {
      if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(req.method())) {
        mutaciones.push(req.method() + ' ' + new URL(req.url()).pathname);
      }
    });
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      const respuesta = await page.goto('/plataforma/revision-amenazas-v5');
      expect(respuesta?.status()).toBe(200);
      expect((await sesion).status()).toBe(200);
      await expect(page.getByRole('heading', { level: 1, name: 'Verificaciones de acceso y abuso' })).toBeVisible();
      await expect(page.locator('section.threat-guide')).toBeVisible();
      await expect(page.getByRole('main'), 'El shell autenticado conserva un solo landmark main').toHaveCount(1);
      await expect(page.locator('.threat-list li')).toHaveCount(4);
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
    }
    expect(mutaciones, 'Consultar guia no ejecuta mutaciones').toEqual([]);
  });

  test('A0.43 - empresa B sin grant global no puede ver la guia ni sus superficies', async ({ browser, request }) => {
    for (const width of [320, 1280]) {
      const contexto = await browser.newContext({
        baseURL: 'http://127.0.0.1:4200',
        viewport: { width, height: 840 }
      });
      try {
        const page = await contexto.newPage();
        const jwt = await ingresar(page, 'c1504_admin_b');
        const global = await request.get(api + '/plataforma/sesion', {
          headers: { Authorization: 'Bearer ' + jwt }
        });
        expect(global.status()).toBe(403);
        const mutaciones: string[] = [];
        page.on('request', req => {
          if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(req.method())) {
            mutaciones.push(req.method() + ' ' + new URL(req.url()).pathname);
          }
        });
        const sesion = page.waitForResponse(r =>
          r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
        await page.goto('/plataforma/revision-amenazas-v5');
        expect((await sesion).status()).toBe(403);
        await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
        await expect(page.locator('.threat-guide')).toHaveCount(0);
        await expect(page.locator('.threat-list li')).toHaveCount(0);
        await expect(page.getByRole('heading', { name: 'Verificaciones de acceso y abuso' })).toHaveCount(0);
        expect(mutaciones, 'Una cuenta empresarial sin grant global no escribe').toEqual([]);
      } finally {
        await contexto.close();
      }
    }
  });


  // A0.44: ampliar muestra de rutas globales con rol autentico y empresa B
  // denegada. API JWT firmada y MySQL efimero: cero page.route y cero mocks.
  test('A0.44 - pantallas globales de arquitectura y QA con JWT real y teclado', async ({ page }) => {
    await ingresar(page, 'c1504_platform_only');
    const rutas = ['/plataforma/qa-global-v19', '/plataforma/revision-arquitectura-v20'];
    const mutaciones: string[] = [];
    page.on('request', req => {
      if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(req.method())) {
        mutaciones.push(req.method() + ' ' + new URL(req.url()).pathname);
      }
    });
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      for (const ruta of rutas) {
        const sesion = page.waitForResponse(r =>
          r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
        const documento = await page.goto(ruta);
        expect(documento?.status()).toBe(200);
        expect((await sesion).status()).toBe(200);
        expect(new URL(page.url()).pathname).toBe(ruta);
        await expect(page.getByRole('main'), 'Solo un main visible por ruta').toHaveCount(1);
        await expect(page.getByRole('heading', { level: 1 }), 'Un h1 por ruta').toHaveCount(1);
        await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
        expect((await page.getByRole('heading', { level: 1 }).textContent())?.trim().length)
          .toBeGreaterThan(0);
        expect(await page.evaluate(() => document.documentElement.scrollWidth))
          .toBeLessThanOrEqual(width + 1);
        await page.keyboard.press('Tab');
        expect(await page.evaluate(() =>
          document.activeElement !== document.body &&
          document.activeElement !== document.documentElement),
          'Tab de teclado debe alcanzar control navegable').toBe(true);
      }
    }
    expect(mutaciones, 'Auditoria global de solo lectura sin HTTP mutante').toEqual([]);
  });

  test('A0.44 - empresa B sin permiso global no accede a arquitectura ni QA', async ({ browser, request }) => {
    for (const ruta of ['/plataforma/qa-global-v19', '/plataforma/revision-arquitectura-v20']) {
      for (const width of [320, 1280]) {
        const contexto = await browser.newContext({
          baseURL: 'http://127.0.0.1:4200',
          viewport: { width, height: 840 }
        });
        try {
          const page = await contexto.newPage();
          const jwt = await ingresar(page, 'c1504_admin_b');
          const respuestaApi = await request.get(api + '/plataforma/sesion', {
            headers: { Authorization: 'Bearer ' + jwt }
          });
          expect(respuestaApi.status(), 'JWT de empresa B denegado por API real').toBe(403);
          const mutaciones: string[] = [];
          page.on('request', req => {
            if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(req.method())) {
              mutaciones.push(req.method() + ' ' + new URL(req.url()).pathname);
            }
          });
          const sesion = page.waitForResponse(r =>
            r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
          await page.goto(ruta);
          expect((await sesion).status()).toBe(403);
          await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
          expect(new URL(page.url()).pathname).not.toBe(ruta);
          expect(mutaciones, 'Sin peticiones HTTP mutantes tras denegacion').toEqual([]);
          expect(await page.evaluate(() => document.documentElement.scrollWidth))
            .toBeLessThanOrEqual(width + 1);
        } finally {
          await contexto.close();
        }
      }
    }
  });


  // A0.46: prueba adversarial de landmark y lectura global. El shell ya
  // contiene main#main-content; el componente interno debe ser section.
  test('A0.46 - consola global JWT plataforma, landmark unico y Tab real', async ({ page }) => {
    await ingresar(page, 'c1504_platform_only');
    const mutaciones: string[] = [];
    page.on('request', req => {
      if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(req.method())) {
        mutaciones.push(req.method() + ' ' + new URL(req.url()).pathname);
      }
    });
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      const documento = await page.goto('/plataforma/consola-global-v5');
      expect(documento?.status()).toBe(200);
      expect((await sesion).status()).toBe(200);
      await expect(page).toHaveURL(/\/plataforma\/consola-global-v5$/);
      await expect(page.locator('section.control-plane')).toBeVisible();
      await expect(page.getByRole('main'), 'Un solo main provisto por el shell').toHaveCount(1);
      await expect(page.getByRole('heading', { level: 1, name: 'Consola global auditada' })).toBeVisible();
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
      await page.keyboard.press('Tab');
      expect(await page.evaluate(() =>
        document.activeElement !== document.body &&
        document.activeElement !== document.documentElement),
        'Tab real debe alcanzar un control enfocable').toBe(true);
    }
    expect(mutaciones, 'Consulta de consola no ejecuta HTTP mutante').toEqual([]);
  });

  test('A0.46 - empresa B no obtiene consola global con JWT firmado', async ({ browser, request }) => {
    for (const width of [320, 1280]) {
      const contexto = await browser.newContext({
        baseURL: 'http://127.0.0.1:4200',
        viewport: { width, height: 840 }
      });
      try {
        const page = await contexto.newPage();
        const jwt = await ingresar(page, 'c1504_admin_b');
        const respuestaApi = await request.get(api + '/plataforma/sesion', {
          headers: { Authorization: 'Bearer ' + jwt }
        });
        expect(respuestaApi.status()).toBe(403);
        const mutaciones: string[] = [];
        page.on('request', req => {
          if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(req.method())) {
            mutaciones.push(req.method() + ' ' + new URL(req.url()).pathname);
          }
        });
        const sesion = page.waitForResponse(r =>
          r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
        await page.goto('/plataforma/consola-global-v5');
        expect((await sesion).status()).toBe(403);
        await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
        await expect(page.locator('.control-plane')).toHaveCount(0);
        expect(mutaciones, 'Denegacion sin operaciones mutantes').toEqual([]);
        expect(await page.evaluate(() => document.documentElement.scrollWidth))
          .toBeLessThanOrEqual(width + 1);
      } finally {
        await contexto.close();
      }
    }
  });


  // A0.47: contraste local con JWT real; nunca grants ni persistencia.
  test('A0.47 - contraste global con teclado es local, reversible y sin escrituras', async ({ page }) => {
    await ingresar(page, 'c1504_platform_only');
    const mutaciones: string[] = [];
    page.on('request', req => {
      if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(req.method())) {
        mutaciones.push(req.method() + ' ' + new URL(req.url()).pathname);
      }
    });
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      const response = await page.goto('/plataforma/consola-global-v5');
      expect(response?.status()).toBe(200);
      expect((await sesion).status()).toBe(200);
      const grupo = page.getByTestId('c0204-branding');
      const normal = page.getByTestId('c0204-brand-institucional');
      const alto = page.getByTestId('c0204-brand-contraste');
      await expect(grupo).toBeVisible();
      await expect(normal).toHaveAttribute('aria-pressed', 'true');
      await expect(alto).toHaveAttribute('aria-pressed', 'false');
      const fondo = await grupo.evaluate(e => getComputedStyle(e).backgroundColor);
      await normal.click();
      await page.keyboard.press('Tab');
      await expect(alto, 'Tab real debe alcanzar contraste alto').toBeFocused();
      await page.keyboard.press('Space');
      await expect(alto).toHaveAttribute('aria-pressed', 'true');
      await expect(normal).toHaveAttribute('aria-pressed', 'false');
      await expect(grupo).toHaveClass(/control-brand--contrast/);
      await expect(page.getByTestId('c0204-brand-state')).toHaveText('Contraste alto seleccionado');
      const colores = await grupo.evaluate(e => ({
        fondo: getComputedStyle(e).backgroundColor,
        texto: getComputedStyle(e).color
      }));
      expect(colores.fondo).not.toBe(fondo);
      expect(colores.texto).toBe('rgb(255, 255, 255)');
      await page.reload();
      await expect(normal).toHaveAttribute('aria-pressed', 'true');
      await expect(alto).toHaveAttribute('aria-pressed', 'false');
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
    }
    expect(mutaciones, 'Apariencia local sin HTTP mutante').toEqual([]);
  });


  // A0.48: nueva superficie privada, JWT firmado y MySQL efimero en CI.
  test('A0.48 - observabilidad privada: teclado, responsive y revalidacion sin mutaciones', async ({ page }) => {
    await ingresar(page, 'c1504_platform_only');
    const mutaciones: string[] = [];
    page.on('request', req => {
      if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(req.method())) {
        mutaciones.push(req.method() + ' ' + new URL(req.url()).pathname);
      }
    });
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      const documento = await page.goto('/plataforma/observabilidad-v5');
      expect(documento?.status()).toBe(200);
      expect((await sesion).status()).toBe(200);
      await expect(page).toHaveURL(/\/plataforma\/observabilidad-v5$/);
      await expect(page.locator('.observability-guide')).toBeVisible();
      await expect(page.getByRole('main')).toHaveCount(1);
      await expect(page.getByRole('heading', {
        level: 1, name: 'Diagnóstico y señales seguras'
      })).toHaveCount(1);
      const revalidar = page.getByRole('button', { name: 'Revalidar acceso de observabilidad' });
      await expect(revalidar).toBeEnabled();
      let foco = false;
      for (let n = 0; n < 60; n++) {
        await page.keyboard.press('Tab');
        foco = await revalidar.evaluate(el => el === document.activeElement);
        if (foco) break;
      }
      expect(foco, 'Tab real debe alcanzar revalidacion').toBe(true);
      const respuesta = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      await page.keyboard.press('Enter');
      expect((await respuesta).status()).toBe(200);
      await expect(page.getByText(
        'Sesión revalidada. Información solo de lectura, sin datos operativos.'
      )).toBeVisible();
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
    }
    expect(mutaciones, 'Guia no crea ni modifica datos').toEqual([]);
  });

  test('A0.48 - empresa B no lee observabilidad global con JWT real', async ({ page, request }) => {
    const jwt = await ingresar(page, 'c1504_admin_b');
    const backend = await request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + jwt }
    });
    expect(backend.status()).toBe(403);
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      await page.goto('/plataforma/observabilidad-v5');
      expect((await sesion).status()).toBe(403);
      await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
      await expect(page.locator('.observability-guide')).toHaveCount(0);
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
    }
  });


  // A0.49: pantalla privada de incidentes, sin iniciar restauraciones reales.
  test('A0.49 - incidentes privados: JWT real, teclado, responsive y solo lectura', async ({ page }) => {
    await ingresar(page, 'c1504_platform_only');
    const mutaciones: string[] = [];
    page.on('request', req => {
      if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(req.method())) {
        mutaciones.push(req.method() + ' ' + new URL(req.url()).pathname);
      }
    });
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      const documento = await page.goto('/plataforma/respuesta-incidentes-v5');
      expect(documento?.status()).toBe(200);
      expect((await sesion).status()).toBe(200);
      await expect(page.locator('.incident-guide')).toBeVisible();
      await expect(page.getByRole('main')).toHaveCount(1);
      await expect(page.getByRole('heading', {
        level: 1, name: 'Respuesta, revocación y recuperación segura'
      })).toBeVisible();
      await expect(page.getByRole('heading', { name: 'Sin incidentes conectados' }))
        .toBeVisible();
      const revalidar = page.getByRole('button', { name: 'Revalidar sesión de plataforma' });
      await expect(revalidar).toBeVisible();
      let foco = false;
      for (let n = 0; n < 65; n++) {
        await page.keyboard.press('Tab');
        foco = await revalidar.evaluate(el => el === document.activeElement);
        if (foco) break;
      }
      expect(foco, 'Tecla Tab llega a revalidacion').toBe(true);
      const refresco = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      await page.keyboard.press('Enter');
      expect((await refresco).status()).toBe(200);
      await expect(page.getByText(
        'Sesión administradora confirmada por la API; ninguna operación de respuesta a incidentes fue ejecutada.'
      )).toBeVisible();
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
    }
    expect(mutaciones, 'Guia de incidentes no realiza escrituras').toEqual([]);
  });

  test('A0.49 - empresa B denegada en incidentes privados con JWT real', async ({ page, request }) => {
    const token = await ingresar(page, 'c1504_admin_b');
    const backend = await request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + token }
    });
    expect(backend.status()).toBe(403);
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      await page.goto('/plataforma/respuesta-incidentes-v5');
      expect((await sesion).status()).toBe(403);
      await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
      await expect(page.locator('.incident-guide')).toHaveCount(0);
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
    }
  });


  // A0.50: gobierno de cambios real; detecta main anidado y denegacion por JWT.
  test('A0.50 - gobierno V5: JWT real, main unico, teclado y GET sin mutaciones', async ({ page }) => {
    await ingresar(page, 'c1504_platform_only');
    const cambios: string[] = [];
    page.on('request', request => {
      if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(request.method())) {
        cambios.push(request.method() + ' ' + new URL(request.url()).pathname);
      }
    });
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      const documento = await page.goto('/plataforma/gobierno-cambios-v5');
      expect(documento?.status()).toBe(200);
      expect((await sesion).status()).toBe(200);
      await expect(page.getByRole('main')).toHaveCount(1);
      await expect(page.getByRole('heading', {
        level: 1, name: 'Decisiones y controles V5'
      })).toHaveCount(1);
      const revalidar = page.getByRole('button', { name: 'Revalidar sesión de plataforma' });
      await expect(revalidar).toBeEnabled();
      let foco = false;
      for (let n = 0; n < 65; n++) {
        await page.keyboard.press('Tab');
        foco = await revalidar.evaluate(el => el === document.activeElement);
        if (foco) break;
      }
      expect(foco, 'Tab llega a revalidar').toBe(true);
      const siguiente = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      await page.keyboard.press('Enter');
      expect((await siguiente).status()).toBe(200);
      await expect(page.getByText(
        'Sesión de plataforma revalidada correctamente. La vista sigue siendo solo lectura.'
      )).toBeVisible();
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
    }
    expect(cambios, 'Gobierno no muta backend').toEqual([]);
  });

  test('A0.50 - empresa B denegada por API real y guard en gobierno privado', async ({ page, request }) => {
    const token = await ingresar(page, 'c1504_admin_b');
    const backend = await request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + token }
    });
    expect(backend.status()).toBe(403);
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      await page.goto('/plataforma/gobierno-cambios-v5');
      expect((await sesion).status()).toBe(403);
      await expect(page).toHaveURL(url =>
        url.pathname === '/login' && url.searchParams.get('plataforma') === 'denegado');
      await expect(page.getByRole('heading', { name: 'Decisiones y controles V5' }))
        .toHaveCount(0);
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
    }
  });


  // A0.51: firma JWT falsificada: el guard y la API real no exponen gobierno privado.
  test('A0.51 - JWT con firma alterada bloquea gobierno V5 en mobile y desktop', async ({ page, request }) => {
    const jwt = await ingresar(page, 'c1504_platform_only');
    const valido = await request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + jwt }
    });
    expect(valido.status()).toBe(200);
    const partes = jwt.split('.');
    expect(partes).toHaveLength(3);
    const firma = partes[2];
    expect(firma.length).toBeGreaterThan(12);
    const alterada = (firma[0] === 'A' ? 'B' : 'A') + firma.slice(1);
    const falsificado = [partes[0], partes[1], alterada].join('.');
    const denegado = await request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + falsificado }
    });
    expect([401, 403], 'Firma manipulada se rechaza en API real').toContain(denegado.status());
    const mutaciones: string[] = [];
    page.on('request', req => {
      if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(req.method())) {
        mutaciones.push(req.method() + ' ' + new URL(req.url()).pathname);
      }
    });
    await page.evaluate(token => localStorage.setItem('solqaryn_token', token), falsificado);
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      await page.goto('/plataforma/gobierno-cambios-v5');
      await expect(page).toHaveURL(/\/login(?:[?#]|$)/);
      await expect(page.getByRole('heading', { name: 'Decisiones y controles V5' })).toHaveCount(0);
      await expect(page.locator('section.governance')).toHaveCount(0);
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
    }
    expect(mutaciones, 'Rechazo de JWT no modifica informacion operativa').toEqual([]);
  });



  // A0.52: placement privado, sin DSN ni cutover; JWT/API/MySQL reales de CI.
  test('A0.52 - placement: plataforma con JWT real, Tab/Enter y solo GET', async ({ page }) => {
    await ingresar(page, 'c1504_platform_only');
    const mutaciones: string[] = [];
    page.on('request', req => {
      if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(req.method())) {
        mutaciones.push(req.method() + ' ' + new URL(req.url()).pathname);
      }
    });
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      const documento = await page.goto('/plataforma/placement-v5');
      expect(documento?.status()).toBe(200);
      expect((await sesion).status()).toBe(200);
      await expect(page.getByRole('main')).toHaveCount(1);
      await expect(page.getByRole('heading', {
        level: 1, name: 'Placement compartido y dedicado'
      })).toBeVisible();
      await expect(page.getByRole('heading', { name: 'Modalidades previstas' }))
        .toBeVisible();
      await expect(page.getByText('Placement, router del servidor y migraciones reales: NO_MEDIDO / NO_EJECUTADO.'))
        .toBeVisible();
      const revalidar = page.getByRole('button', { name: 'Revalidar sesión de placement' });
      await expect(revalidar).toBeEnabled();
      let foco = false;
      for (let n = 0; n < 65; n++) {
        await page.keyboard.press('Tab');
        foco = await revalidar.evaluate(el => el === document.activeElement);
        if (foco) break;
      }
      expect(foco, 'Tab alcanza revalidacion placement').toBe(true);
      const refresco = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      await page.keyboard.press('Enter');
      expect((await refresco).status()).toBe(200);
      await expect(page.getByText('Sesión revalidada. Solo consulta documental, sin migraciones.'))
        .toBeVisible();
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
    }
    expect(mutaciones, 'Placement no crea migraciones ni cambios desde browser').toEqual([]);
  });

  test('A0.52 - empresa B no accede a placement global con JWT firmado', async ({ page, request }) => {
    const token = await ingresar(page, 'c1504_admin_b');
    const backend = await request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + token }
    });
    expect(backend.status()).toBe(403);
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      await page.goto('/plataforma/placement-v5');
      expect((await sesion).status()).toBe(403);
      await expect(page).toHaveURL(url =>
        url.pathname === '/login' && url.searchParams.get('plataforma') === 'denegado');
      await expect(page.getByRole('heading', { name: 'Placement compartido y dedicado' }))
        .toHaveCount(0);
      await expect(page.locator('section.placement-review')).toHaveCount(0);
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
    }
  });

  // A0.53: sustituye la sesion sintetica de C15-01 por JWT firmado y API/MySQL reales.
  test('A0.53 - cobertura real: plataforma JWT, teclado Tab y 4 dimensiones NO_MEDIDO', async ({ page }) => {
    await ingresar(page, 'c1504_platform_only');
    const mutaciones: string[] = [];
    page.on('request', req => {
      if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(req.method())) {
        mutaciones.push(req.method() + ' ' + new URL(req.url()).pathname);
      }
    });
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      const documento = await page.goto('/plataforma/trazabilidad-cobertura-v5');
      expect(documento?.status()).toBe(200);
      expect((await sesion).status()).toBe(200);
      await expect(page.getByTestId('c1501-loaded')).toBeVisible();
      await expect(page.locator('section.coverage-shell[aria-labelledby="coverage-title"]'))
        .toHaveCount(1);
      await expect(page.locator('section[aria-labelledby="coverage-dimensions"]'))
        .toHaveCount(1);
      await expect(page.locator('.coverage-list li')).toHaveCount(4);
      await expect(page.locator('.coverage-shell [role="status"]')).toContainText('NO_MEDIDO');
      const volver = page.getByRole('link', { name: 'Regresar a administración de plataforma' });
      await expect(volver).toBeVisible();
      let foco = false;
      for (let n = 0; n < 100; n++) {
        await page.keyboard.press('Tab');
        foco = await volver.evaluate(el => el === document.activeElement);
        if (foco) break;
      }
      expect(foco, 'Tab autentico alcanza enlace de regreso').toBe(true);
      const objetivo = await volver.evaluate(el => {
        const rect = el.getBoundingClientRect();
        return { width: rect.width, height: rect.height,
          outline: parseFloat(getComputedStyle(el).outlineWidth) };
      });
      expect(objetivo.width).toBeGreaterThanOrEqual(44);
      expect(objetivo.height).toBeGreaterThanOrEqual(44);
      expect(objetivo.outline).toBeGreaterThanOrEqual(3);
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
    }
    expect(mutaciones, 'Auditoria de cobertura solo lectura').toEqual([]);
  });

  test('A0.53 - cobertura global niega empresa B mediante JWT real y API 403', async ({ page, request }) => {
    const token = await ingresar(page, 'c1504_admin_b');
    const backend = await request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + token }
    });
    expect(backend.status()).toBe(403);
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      await page.goto('/plataforma/trazabilidad-cobertura-v5');
      expect((await sesion).status()).toBe(403);
      await expect(page).toHaveURL(url =>
        url.pathname === '/login' && url.searchParams.get('plataforma') === 'denegado');
      await expect(page.getByTestId('c1501-loaded')).toHaveCount(0);
      await expect(page.locator('.coverage-list li')).toHaveCount(0);
      await expect(page.getByRole('heading', { name: 'Revisión de cobertura del plan' }))
        .toHaveCount(0);
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
    }
  });


  // A0.54: cobertura global autenticada adicional; cutover permanece solo lectura.
  test('A0.54 - cutover privado: JWT real, tema por teclado y cero mutaciones', async ({ page }) => {
    await ingresar(page, 'c1504_platform_only');
    const mutaciones: string[] = [];
    page.on('request', req => {
      if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(req.method())) {
        mutaciones.push(req.method() + ' ' + new URL(req.url()).pathname);
      }
    });
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      const documento = await page.goto('/plataforma/mysql-cutover-readonly-v5');
      expect(documento?.status()).toBe(200);
      expect((await sesion).status()).toBe(200);
      await expect(page.getByRole('main')).toHaveCount(1);
      await expect(page.locator('section.cutover[aria-labelledby="cutover-title"]')).toHaveCount(1);
      await expect(page.getByRole('heading', {
        level: 1, name: 'Controles de preparación para cutover'
      })).toHaveCount(1);
      await expect(page.getByTestId('c1204-cutover-private').locator('li')).toHaveCount(4);
      await expect(page.getByRole('note', { name: 'Límite de operación' }))
        .toContainText('NO OPERATIVO');
      await expect(page.getByText('NO_AUTORIZADO')).toBeVisible();
      const revalidar = page.getByTestId('c1204-m0402-revalidate');
      let focoRevalidar = false;
      for (let n = 0; n < 100; n++) {
        await page.keyboard.press('Tab');
        focoRevalidar = await revalidar.evaluate(el => el === document.activeElement);
        if (focoRevalidar) break;
      }
      expect(focoRevalidar, 'Tab llega a revalidar cutover').toBe(true);
      const respuesta = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      await page.keyboard.press('Enter');
      expect((await respuesta).status()).toBe(200);
      await expect(page.getByTestId('c1204-m0402-state')).toContainText('Éxito');
      const contraste = page.getByTestId('c1204-m0403-theme-alto-contraste');
      let focoContraste = false;
      for (let n = 0; n < 12; n++) {
        await page.keyboard.press('Tab');
        focoContraste = await contraste.evaluate(el => el === document.activeElement);
        if (focoContraste) break;
      }
      expect(focoContraste, 'Tema alcanzable con teclado real').toBe(true);
      await page.keyboard.press('Space');
      await expect(contraste).toHaveAttribute('aria-pressed', 'true');
      await expect(page.locator('section.cutover')).toHaveClass(/contraste-alto/);
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
    }
    expect(mutaciones, 'Cutover no ejecuta operaciones MySQL desde UI').toEqual([]);
  });

  test('A0.54 - empresa B no lee controles privados de cutover con JWT real', async ({ page, request }) => {
    const token = await ingresar(page, 'c1504_admin_b');
    const acceso = await request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + token }
    });
    expect(acceso.status()).toBe(403);
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      await page.goto('/plataforma/mysql-cutover-readonly-v5');
      expect((await sesion).status()).toBe(403);
      await expect(page).toHaveURL(url =>
        url.pathname === '/login' && url.searchParams.get('plataforma') === 'denegado');
      await expect(page.getByTestId('c1204-cutover-private')).toHaveCount(0);
      await expect(page.getByTestId('c1204-m0403-branding')).toHaveCount(0);
      await expect(page.getByRole('heading', {
        name: 'Controles de preparación para cutover'
      })).toHaveCount(0);
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
    }
  });


  test('A0.56 - auditoría operativa privada conserva un main, teclado y HTTP GET auténticos', async ({ page }) => {
    await ingresar(page, 'c1504_platform_only');
    const mutaciones: string[] = [];
    page.on('request', solicitud => {
      if (/^(POST|PUT|PATCH|DELETE)$/.test(solicitud.method()) &&
          solicitud.url().includes('localhost:5005')) {
        mutaciones.push(solicitud.method() + ' ' + solicitud.url());
      }
    });
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      const documento = await page.goto('/plataforma/auditoria-operativa-v5');
      expect(documento?.status()).toBe(200);
      expect((await sesion).status()).toBe(200);
      await expect(page.getByRole('main')).toHaveCount(1);
      await expect(page.locator('section.audit-view[aria-labelledby="audit-view-title"]')).toHaveCount(1);
      await expect(page.getByRole('heading', { level: 1, name: 'Controles de ejecución y evidencia' })).toBeVisible();
      const boton = page.getByRole('button', { name: 'Revalidar sesión de auditoría' });
      let focusReal = false;
      for (let n = 0; n < 100; n++) {
        await page.keyboard.press('Tab');
        focusReal = await boton.evaluate(elemento => elemento === document.activeElement);
        if (focusReal) break;
      }
      expect(focusReal, 'Teclado Tab llega a revalidación privada').toBe(true);
      const revalidar = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      await page.keyboard.press('Enter');
      expect((await revalidar).status()).toBe(200);
      const estadoAcceso = page.locator('section.audit-state[aria-label="Estado de acceso y consulta"]').getByRole('status');
      await expect(estadoAcceso).toHaveCount(1);
      await expect(estadoAcceso).toContainText('Sesión de plataforma revalidada correctamente');
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
    }
    expect(mutaciones, 'La vista es de solo lectura después del login').toEqual([]);
  });

  test('A0.56 - tenant B rechaza auditoría operativa y no renderiza contenido protegido', async ({ page, request }) => {
    const jwt = await ingresar(page, 'c1504_admin_b');
    const directo = await request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + jwt }
    });
    expect(directo.status()).toBe(403);
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      await page.goto('/plataforma/auditoria-operativa-v5');
      expect((await sesion).status()).toBe(403);
      await expect(page).toHaveURL(url =>
        url.pathname === '/login' && url.searchParams.get('plataforma') === 'denegado');
      await expect(page.locator('section.audit-view')).toHaveCount(0);
      await expect(page.getByRole('heading', { name: 'Controles de ejecución y evidencia' })).toHaveCount(0);
    }
  });


  // A0.57: another private route with the same nested-main accessibility regression.
  test('A0.57 - experiencia visual JWT real: región única, Tab/Space y responsive', async ({ page }) => {
    await ingresar(page, 'c1504_platform_only');
    const mutaciones: string[] = [];
    page.on('request', solicitud => {
      if (/^(POST|PUT|PATCH|DELETE)$/.test(solicitud.method()) &&
          solicitud.url().includes('localhost:5005')) {
        mutaciones.push(solicitud.method() + ' ' + solicitud.url());
      }
    });
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      const documento = await page.goto('/plataforma/experiencia-visual-v5');
      expect(documento?.status()).toBe(200);
      expect((await sesion).status()).toBe(200);
      await expect(page.getByRole('main')).toHaveCount(1);
      const region = page.locator('section.visual-shell[aria-labelledby="visual-title"]');
      await expect(region).toHaveCount(1);
      await expect(page.getByRole('heading', { level: 1, name: 'Configuración visual permitida' })).toHaveCount(1);
      await expect(page.getByTestId('c1304-m0402-panel')).toContainText('SIN_VERIFICAR');
      const contraste = page.getByTestId('c1304-contrast');
      let foco = false;
      for (let intento = 0; intento < 120; intento++) {
        await page.keyboard.press('Tab');
        foco = await contraste.evaluate(elemento => elemento === document.activeElement);
        if (foco) break;
      }
      expect(foco, 'Tab real llega a contraste sin focus() programático').toBe(true);
      await page.keyboard.press('Space');
      await expect(contraste).toHaveAttribute('aria-pressed', 'true');
      await expect(region).toHaveClass(/visual-contrast/);
      const compacta = page.getByTestId('c1304-compact');
      let focoCompacta = false;
      for (let intento = 0; intento < 24; intento++) {
        await page.keyboard.press('Tab');
        focoCompacta = await compacta.evaluate(elemento => elemento === document.activeElement);
        if (focoCompacta) break;
      }
      expect(focoCompacta, 'Tab alcanza selector de densidad').toBe(true);
      await page.keyboard.press('Enter');
      await expect(compacta).toHaveAttribute('aria-pressed', 'true');
      await expect(region).toHaveClass(/visual-compact/);
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
    }
    expect(mutaciones, 'La UI visual solo cambia estado local, sin operaciones backend').toEqual([]);
  });

  test('A0.57 - empresa B con JWT real recibe 403 y no ve referencia visual privada', async ({ page, request }) => {
    const token = await ingresar(page, 'c1504_admin_b');
    const backend = await request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + token }
    });
    expect(backend.status()).toBe(403);
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      await page.goto('/plataforma/experiencia-visual-v5');
      expect((await sesion).status()).toBe(403);
      await expect(page).toHaveURL(url =>
        url.pathname === '/login' && url.searchParams.get('plataforma') === 'denegado');
      await expect(page.locator('section.visual-shell')).toHaveCount(0);
      await expect(page.getByTestId('c1304-contrast')).toHaveCount(0);
      await expect(page.getByRole('heading', { name: 'Configuración visual permitida' })).toHaveCount(0);
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
    }
  });


  // A0.59: dos pruebas R2 de alcance limitado, sin mocks ni concesiones empresariales.
  test('A0.59 - estados multicanal JWT real: vacio, lectura y responsive', async ({ page }) => {
    await ingresar(page, 'c1504_platform_only');
    const escrituras: string[] = [];
    page.on('request', solicitud => {
      if (/^(POST|PUT|PATCH|DELETE)$/.test(solicitud.method()) &&
          solicitud.url().includes('localhost:5005')) {
        escrituras.push(solicitud.method() + ' ' + new URL(solicitud.url()).pathname);
      }
    });
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      const documento = await page.goto('/plataforma/mensajeria-multicanal-v19/estados');
      expect(documento?.status()).toBe(200);
      expect((await sesion).status(), 'Autorizacion de plataforma viene del backend').toBe(200);
      await expect(page.getByRole('main')).toHaveCount(1);
      const panel = page.getByRole('region', { name: 'Estado del panel de mensajeria multicanal' });
      await expect(panel).toBeVisible();
      await expect(panel).toHaveAttribute('aria-busy', 'false');
      await expect(page.getByTestId('m0402-vacio')).toContainText('Sin mensajes verificados');
      await expect(page.getByTestId('m0402-exito')).toHaveCount(0);
      await expect(page.getByTestId('m0402-error')).toHaveCount(0);
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
    }
    expect(escrituras, 'Inspeccionar estado no envia mensajes ni escribe datos').toEqual([]);
  });

  test('A0.59 - empresa B JWT real no renderiza estados privados multicanal', async ({ page, request }) => {
    const jwt = await ingresar(page, 'c1504_admin_b');
    const directo = await request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + jwt }
    });
    expect(directo.status(), 'API no admite rol empresarial como plataforma').toBe(403);
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      await page.goto('/plataforma/mensajeria-multicanal-v19/estados');
      expect((await sesion).status()).toBe(403);
      await expect(page).toHaveURL(url =>
        url.pathname === '/login' && url.searchParams.get('plataforma') === 'denegado');
      await expect(page.locator('section.estado-mensajeria')).toHaveCount(0);
      await expect(page.getByTestId('m0402-vacio')).toHaveCount(0);
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
    }
  });



  // A0.60: P2 a11y, ruta existente con shell main. JWT y base reales, sin HTTP mock.
  test('A0.60 - capacidades privadas: un main, region etiquetada y Tab con JWT real', async ({ page }) => {
    await ingresar(page, 'c1504_platform_only');
    const mutaciones: string[] = [];
    page.on('request', req => {
      if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(req.method()) &&
          req.url().includes('localhost:5005')) {
        mutaciones.push(req.method() + ' ' + new URL(req.url()).pathname);
      }
    });
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(res =>
        res.url().includes('/plataforma/sesion') && res.request().method() === 'GET');
      const documento = await page.goto('/plataforma/capacidades-estados-v19');
      expect(documento?.status()).toBe(200);
      expect((await sesion).status()).toBe(200);
      await expect(page.getByRole('main'), 'Solo el main de la shell').toHaveCount(1);
      const region = page.locator('section.estados[aria-labelledby="estados-title"]');
      await expect(region).toBeVisible();
      await expect(page.getByRole('heading', {
        level: 1, name: 'Estado de capacidades y permisos'
      })).toHaveCount(1);
      await expect(region.locator('[data-testid^="estado-"]')).toHaveCount(1);
      await expect(region.getByTestId('estado-denegado')).toHaveCount(0);
      const volver = region.getByRole('link', { name: 'Volver a plataforma' });
      let foco = false;
      for (let intento = 0; intento < 120; intento++) {
        await page.keyboard.press('Tab');
        foco = await volver.evaluate(el => el === document.activeElement);
        if (foco) break;
      }
      expect(foco, 'Tab real llega al enlace de retorno').toBe(true);
      expect(await volver.evaluate(el => parseFloat(getComputedStyle(el).outlineWidth)))
        .toBeGreaterThan(0);
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
    }
    expect(mutaciones, 'Consulta de capacidades no muta backend').toEqual([]);
  });

  test('A0.60 - empresa B con JWT firmado no ve estados globales protegidos', async ({ page, request }) => {
    const jwt = await ingresar(page, 'c1504_admin_b');
    const directo = await request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + jwt }
    });
    expect(directo.status()).toBe(403);
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(res =>
        res.url().includes('/plataforma/sesion') && res.request().method() === 'GET');
      await page.goto('/plataforma/capacidades-estados-v19');
      expect((await sesion).status()).toBe(403);
      await expect(page).toHaveURL(url =>
        url.pathname === '/login' && url.searchParams.get('plataforma') === 'denegado');
      await expect(page.locator('section.estados')).toHaveCount(0);
      await expect(page.getByRole('heading', { name: 'Estado de capacidades y permisos' })).toHaveCount(0);
      await expect(page.locator('[data-testid^="estado-"]')).toHaveCount(0);
    }
  });



  // A0.61: ruta anteriormente no cubierta. JWT firmado, API .NET10 y MySQL8.4 efimero.
  test('A0.61 - auditoria de tenancy privada: JWT, cuatro frentes y teclado real', async ({ page }) => {
    await ingresar(page, 'c1504_platform_only');
    const escrituras: string[] = [];
    page.on('request', req => {
      if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(req.method()) &&
          req.url().includes('localhost:5005')) {
        escrituras.push(req.method() + ' ' + new URL(req.url()).pathname);
      }
    });
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(res =>
        res.url().includes('/plataforma/sesion') && res.request().method() === 'GET');
      const documento = await page.goto('/plataforma/auditoria-tenancy-v5');
      expect(documento?.status()).toBe(200);
      expect((await sesion).status()).toBe(200);
      await expect(page.getByRole('main')).toHaveCount(1);
      const panel = page.locator('section.tenancy-audit');
      await expect(panel).toBeVisible();
      await expect(panel.getByRole('heading', { level: 1, name: 'Fronteras de aislamiento y evidencia' })).toBeVisible();
      await expect(panel.locator('ol.fronteras > li')).toHaveCount(4);
      await expect(panel.getByText('NO_MEDIDO', { exact: false })).toBeVisible();
      const saltar = panel.getByRole('link', { name: 'Saltar a los cuatro frentes de revisión' });
      let alcanzado = false;
      for (let i = 0; i < 120; i++) {
        await page.keyboard.press('Tab');
        alcanzado = await saltar.evaluate(el => el === document.activeElement);
        if (alcanzado) break;
      }
      expect(alcanzado, 'Tab real debe alcanzar enlace de salto').toBe(true);
      await page.keyboard.press('Enter');
      await expect(panel.locator('#fronteras-title')).toBeFocused();
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
    }
    const revalidacion = page.waitForResponse(res =>
      res.url().includes('/plataforma/sesion') && res.request().method() === 'GET');
    await page.getByRole('button', { name: 'Revalidar sesión de auditoría de tenancy' }).click();
    expect((await revalidacion).status()).toBe(200);
    await expect(page.getByText('Sesión revalidada. Solo guía de revisión, no pentest real.')).toBeVisible();
    expect(escrituras, 'Guía privada no altera API ni ejecuta pentest').toEqual([]);
  });

  test('A0.61 - empresa B con JWT firmado no obtiene auditoria de tenancy global', async ({ page, request }) => {
    const jwt = await ingresar(page, 'c1504_admin_b');
    const directo = await request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + jwt }
    });
    expect(directo.status(), 'Backend rechaza empresa sin rol global').toBe(403);
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(res =>
        res.url().includes('/plataforma/sesion') && res.request().method() === 'GET');
      await page.goto('/plataforma/auditoria-tenancy-v5');
      expect((await sesion).status()).toBe(403);
      await expect(page).toHaveURL(url =>
        url.pathname === '/login' && url.searchParams.get('plataforma') === 'denegado');
      await expect(page.locator('section.tenancy-audit')).toHaveCount(0);
      await expect(page.getByRole('heading', { name: 'Fronteras de aislamiento y evidencia' })).toHaveCount(0);
      await expect(page.locator('ol.fronteras > li')).toHaveCount(0);
    }
  });


  // A0.62: una firma JWT alterada debe perder autoridad global en HTTP y navegador.
  test('A0.62 - JWT adulterado pierde ruta privada de auditoria tenancy', async ({ page, request }) => {
    const jwt = await ingresar(page, 'c1504_platform_only');
    const autorizada = await request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + jwt }
    });
    expect(autorizada.status()).toBe(200);
    const partes = jwt.split('.');
    const firma = partes[2];
    expect(firma.length).toBeGreaterThan(20);
    const alterada = (firma[0] === 'A' ? 'B' : 'A') + firma.slice(1);
    const jwtFalso = [partes[0], partes[1], alterada].join('.');
    const denegada = await request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + jwtFalso }
    });
    expect([401, 403], 'Servidor debe rechazar firma JWT no autentica').toContain(denegada.status());

    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      await page.evaluate(token => localStorage.setItem('solqaryn_token', token), jwtFalso);
      await page.goto('/plataforma/auditoria-tenancy-v5');
      await expect(page).toHaveURL(/\/login(?:\?|$)/);
      await expect(page.locator('section.tenancy-audit')).toHaveCount(0);
      await expect(page.getByRole('heading', {
        name: 'Fronteras de aislamiento y evidencia'
      })).toHaveCount(0);
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
      if (width === 320) await ingresar(page, 'c1504_platform_only');
    }
  });

  // A0.63: JWT firmado tenant A no equivale a permiso de plataforma.
  test('A0.63 - empresa A JWT real no obtiene auditoria de tenancy global', async ({ page, request }) => {
    const jwt = await ingresar(page, 'c1504_admin_a');
    const directo = await request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + jwt }
    });
    expect(directo.status(), 'Rol empresarial A no es permiso global').toBe(403);
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(res =>
        res.url().includes('/plataforma/sesion') && res.request().method() === 'GET');
      await page.goto('/plataforma/auditoria-tenancy-v5');
      expect((await sesion).status()).toBe(403);
      await expect(page).toHaveURL(url =>
        url.pathname === '/login' && url.searchParams.get('plataforma') === 'denegado');
      await expect(page.locator('section.tenancy-audit')).toHaveCount(0);
      await expect(page.getByRole('heading', { name: 'Fronteras de aislamiento y evidencia' })).toHaveCount(0);
      await expect(page.locator('ol.fronteras > li')).toHaveCount(0);
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
    }
  });


  // A0.64: alcance R2 test-only; un usuario global autenticado debe acceder sin mouse.
  test('A0.64 - revalidación privada de tenancy por Tab y Enter reales', async ({ page }) => {
    await ingresar(page, 'c1504_platform_only');
    const mutaciones: string[] = [];
    page.on('request', req => {
      if (/^(POST|PUT|PATCH|DELETE)$/.test(req.method()) &&
          req.url().includes('localhost:5005')) {
        mutaciones.push(req.method() + ' ' + new URL(req.url()).pathname);
      }
    });
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(res =>
        res.url().includes('/plataforma/sesion') && res.request().method() === 'GET');
      const documento = await page.goto('/plataforma/auditoria-tenancy-v5');
      expect(documento?.status()).toBe(200);
      expect((await sesion).status()).toBe(200);
      const region = page.locator('section.tenancy-audit');
      await expect(region).toBeVisible();
      await expect(page.getByRole('main')).toHaveCount(1);
      const boton = page.getByRole('button', { name: 'Revalidar sesión de auditoría de tenancy' });
      await expect(boton).toBeVisible();
      let foco = false;
      for (let intento = 0; intento < 120; intento++) {
        await page.keyboard.press('Tab');
        foco = await boton.evaluate(el => el === document.activeElement);
        if (foco) break;
      }
      expect(foco, 'Tab llega al control sin .focus() artificial').toBe(true);
      const revalidacion = page.waitForResponse(res =>
        res.url().includes('/plataforma/sesion') && res.request().method() === 'GET');
      await page.keyboard.press('Enter');
      expect((await revalidacion).status()).toBe(200);
      await expect(page.getByText('Sesión revalidada. Solo guía de revisión, no pentest real.'))
        .toBeVisible();
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
    }
    expect(mutaciones, 'Tab y Enter solo provocan GET y no mutan recursos empresariales').toEqual([]);
  });


  // A0.65: region viva y salto accesible con JWT firmado, sin autorizacion simulada.
  test('A0.65 - acceso a frentes por teclado y anuncio de sesión', async ({ page }) => {
    await ingresar(page, 'c1504_platform_only');
    const mutaciones: string[] = [];
    page.on('request', req => {
      if (/^(POST|PUT|PATCH|DELETE)$/.test(req.method()) &&
          req.url().includes('localhost:5005')) {
        mutaciones.push(req.method() + ' ' + new URL(req.url()).pathname);
      }
    });
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(res =>
        res.url().includes('/plataforma/sesion') && res.request().method() === 'GET');
      const documento = await page.goto('/plataforma/auditoria-tenancy-v5');
      expect(documento?.status()).toBe(200);
      expect((await sesion).status()).toBe(200);
      const estado = page.locator('section.tenancy-audit section.estado');
      await expect(estado).toHaveAttribute('aria-live', 'polite');
      await expect(estado).toHaveAttribute('aria-atomic', 'true');
      await expect(estado).toHaveAttribute('aria-busy', 'false');
      await expect(estado.getByRole('status')).toContainText('Sesión verificada');
      const salto = page.getByRole('link', { name: 'Saltar a los cuatro frentes de revisión' });
      await expect(salto).toBeVisible();
      let alcanzado = false;
      for (let intento = 0; intento < 160; intento++) {
        await page.keyboard.press('Tab');
        alcanzado = await salto.evaluate(el => el === document.activeElement);
        if (alcanzado) break;
      }
      expect(alcanzado, 'Tab llega al enlace sin .focus() artificial').toBe(true);
      await page.keyboard.press('Enter');
      await expect(page.locator('#fronteras-title')).toBeFocused();
      await expect(page.locator('ol.fronteras')).toHaveAttribute('aria-labelledby', 'fronteras-title');
      await expect(page.locator('ol.fronteras li')).toHaveCount(4);
      await expect(page).toHaveURL(/\/plataforma\/auditoria-tenancy-v5$/);
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
    }
    expect(mutaciones, 'Navegacion accesible solo lectura no muta API').toEqual([]);
  });


  // A0.66: inventario AST de rutas de plataforma (contrato estatico, no prueba HTTP).
  test('A0.66 - todas las rutas privadas directas de plataforma conservan guard y nombre unico', async () => {
    const { existsSync, readFileSync } = await import('node:fs');
    const { resolve } = await import('node:path');
    const ts = await import('typescript');
    const sourcePath = [
      resolve(process.cwd(), 'src/app/app.routes.ts'),
      resolve(process.cwd(), 'frontend/src/app/app.routes.ts')
    ].find(p => existsSync(p));
    expect(sourcePath, 'app.routes.ts debe estar presente en el checkout exact-SHA').toBeDefined();
    const sourceText = readFileSync(sourcePath!, 'utf8');
    const ast = ts.createSourceFile(sourcePath!, sourceText, ts.ScriptTarget.Latest, true);
    const catalogo: { ruta: string; guard: boolean }[] = [];
    const visit = (node: import('typescript').Node): void => {
      if (ts.isObjectLiteralExpression(node)) {
        const property = (name: string) => node.properties.find(p =>
          ts.isPropertyAssignment(p) && p.name.getText(ast) === name);
        const path = property('path');
        if (path && ts.isPropertyAssignment(path) &&
            ts.isStringLiteral(path.initializer) &&
            (path.initializer.text === 'plataforma' ||
             path.initializer.text.startsWith('plataforma/'))) {
          const route = path.initializer.text;
          const guardProp = property('canActivate');
          const guard = Boolean(guardProp && ts.isPropertyAssignment(guardProp) &&
            ts.isArrayLiteralExpression(guardProp.initializer) &&
            guardProp.initializer.elements.some(e => ts.isIdentifier(e) &&
              e.text === 'plataformaGuard'));
          catalogo.push({ ruta: route, guard });
        }
      }
      ts.forEachChild(node, visit);
    };
    visit(ast);
    expect(catalogo.length, 'baseline de 24 rutas privadas de plataforma').toBeGreaterThanOrEqual(24);
    const noGuard = catalogo.filter(x => !x.guard).map(x => x.ruta);
    expect(noGuard, 'toda ruta de plataforma debe conservar plataformaGuard').toEqual([]);
    const nombres = catalogo.map(x => x.ruta);
    expect(new Set(nombres).size, 'sin rutas de plataforma duplicadas').toBe(nombres.length);
    expect(nombres).toContain('plataforma/auditoria-ux-v5');
    expect(nombres).toContain('plataforma/auditoria-tenancy-v5');
    expect(nombres).toContain('plataforma');
    // No hay certificacion de UX/routing de lazy modules, JWT HTTP ni backend RBAC en esta prueba.
  });


  // A0.67: ocho arrays ERP importados; solo contrato estatico, no autorizacion runtime.
  test('A0.67 - 51 rutas ERP importadas preservan autenticacion, RBAC y contrato de permisos', async () => {
    const { existsSync, readFileSync } = await import('node:fs');
    const { resolve } = await import('node:path');
    const ts = await import('typescript');
    const archivos = [
      'features/inventario/ajustes.routes.ts',
      'features/compras/facturas-proveedor/facturas-proveedor.routes.ts',
      'features/pedidos-venta/pedidos-venta.routes.ts',
      'features/inventario/transferencias.routes.ts',
      'features/inventario/conteos.routes.ts',
      'features/inventario/reservas.routes.ts',
      'features/finanzas/cuentas-por-cobrar/cuentas-por-cobrar.routes.ts',
      'features/finanzas/cuentas-por-pagar/cuentas-por-pagar.routes.ts'
    ];
    const registros: { archivo: string; ruta: string; auth: boolean; rbac: boolean; metadata: boolean }[] = [];
    for (const archivo of archivos) {
      const fuente = [
        resolve(process.cwd(), 'src/app', archivo),
        resolve(process.cwd(), 'frontend/src/app', archivo)
      ].find(existsSync);
      expect(fuente, 'No ocultar fuente de ruta privada: ' + archivo).toBeDefined();
      const ast = ts.createSourceFile(fuente!, readFileSync(fuente!, 'utf8'), ts.ScriptTarget.Latest, true);
      const propiedad = (obj: import('typescript').ObjectLiteralExpression, nombre: string) =>
        obj.properties.find(p => ts.isPropertyAssignment(p) &&
          (ts.isIdentifier(p.name) || ts.isStringLiteral(p.name)) && p.name.text === nombre);
      const visita = (nodo: import('typescript').Node): void => {
        if (ts.isObjectLiteralExpression(nodo)) {
          const path = propiedad(nodo, 'path');
          if (path && ts.isPropertyAssignment(path) && ts.isStringLiteral(path.initializer)) {
            const can = propiedad(nodo, 'canActivate');
            const guards = can && ts.isPropertyAssignment(can) &&
              ts.isArrayLiteralExpression(can.initializer)
                ? can.initializer.elements.filter(ts.isIdentifier).map(x => x.text) : [];
            const data = propiedad(nodo, 'data');
            const metadata = Boolean(data && ts.isPropertyAssignment(data) &&
              ts.isObjectLiteralExpression(data.initializer) &&
              ['modulo', 'accion'].every(campo => {
                const v = propiedad(data.initializer as import('typescript').ObjectLiteralExpression, campo);
                return Boolean(v && ts.isPropertyAssignment(v) &&
                  ts.isStringLiteral(v.initializer) && v.initializer.text.trim().length);
              }));
            registros.push({ archivo, ruta: path.initializer.text,
              auth: guards.includes('authGuard'),
              rbac: guards.includes('permisoGuard'), metadata });
          }
        }
        ts.forEachChild(nodo, visita);
      };
      visita(ast);
    }
    expect(registros.length, 'baseline de 51 declaraciones ERP importadas').toBeGreaterThanOrEqual(51);
    expect(registros.filter(x => !x.auth || !x.rbac || !x.metadata),
      'sin ruta ERP sin guard o metadatos de permiso').toEqual([]);
    expect(new Set(registros.map(x => x.ruta)).size,
      'sin ambiguedad de rutas ERP importadas').toBe(registros.length);
    expect(registros.some(x => x.ruta.includes(':id'))).toBe(true);
    // No demuestra llamadas HTTP, JWT valido, permisos efectivos, accesibilidad o UX de 160 pantallas.
  });


  // A0.68: rutas ERP importadas, JWT firmado y aislamiento real del tenant.
  // No hay page.route, respuestas HTTP fabricadas ni cambios de permisos en datos productivos.
  for (const destino of [
    { nombre: 'ajustes-inventario', ruta: '/inventario/ajustes' },
    { nombre: 'transferencias', ruta: '/inventario/transferencias' },
    { nombre: 'pedidos-venta', ruta: '/pedidos-venta' },
    { nombre: 'facturas-proveedor', ruta: '/facturas-proveedor' },
    { nombre: 'cuentas-por-pagar', ruta: '/cuentas-por-pagar' }
  ]) {
    for (const ancho of [320, 1280]) {
      test(`A0.68 - tenant B no suplanta A en ${destino.nombre} a ${ancho}px`, async ({ page, request }) => {
        await page.setViewportSize({ width: ancho, height: 840 });
        const jwt = await ingresar(page, 'c1504_admin_b');
        const headers = { Authorization: 'Bearer ' + jwt };
        const propia = await request.get(api + '/tenant-context/2', { headers });
        expect(propia.status(), 'Empresa B valida con JWT y MySQL reales').toBe(200);
        const ajena = await request.get(api + '/tenant-context/1', { headers });
        expect([403, 404], 'Backend deniega empresa A ajena').toContain(ajena.status());

        // localStorage es una solicitud, nunca fuente de autoridad de tenancy.
        // La recarga de la ruta exige materializar de nuevo contexto en el backend.
        await page.evaluate(() => localStorage.setItem('solqaryn_empresa_solicitada_id', '1'));
        const bloqueo = page.waitForResponse(res =>
          res.url().includes('/tenant-context/1') && res.request().method() === 'GET');
        await page.goto(destino.ruta);
        expect([403, 404], 'Revalidacion HTTP no concede tenant A').toContain((await bloqueo).status());
        await expect(page).toHaveURL(url => url.pathname === '/login');
        const scroll = await page.evaluate(() => document.documentElement.scrollWidth);
        expect(scroll, 'Login de acceso denegado no desborda').toBeLessThanOrEqual(ancho + 1);
      });
    }
  }

  // A0.69: evidencia incremental de reflujo a 200% sobre superficies globales
  // autenticas: JWT firmado, HTTP .NET y MySQL descartable, sin page.route.
  test('A0.69 - reflujo 200% y foco visible por teclado en 3 superficies JWT reales', async ({ page, request }) => {
    const jwt = await ingresar(page, 'c1504_platform_only');
    const probe = await request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + jwt }
    });
    expect(probe.status(), 'JWT firmado y rol plataforma verificados en API real').toBe(200);
    const mutaciones: string[] = [];
    page.on('request', req => {
      if (req.url().includes('localhost:5005') &&
          /^(POST|PUT|PATCH|DELETE)$/.test(req.method())) {
        mutaciones.push(req.method() + ' ' + new URL(req.url()).pathname);
      }
    });
    for (const ruta of [
      '/plataforma/auditoria-tenancy-v5',
      '/plataforma/observabilidad-v5',
      '/plataforma/capacidades-estados-v19'
    ]) {
      for (const width of [320, 1280]) {
        await page.setViewportSize({ width, height: 840 });
        const sesion = page.waitForResponse(r =>
          r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
        const respuesta = await page.goto(ruta);
        expect(respuesta?.status(), 'Angular dev local accesible').toBe(200);
        expect((await sesion).status(), 'Backend JWT firmado autoriza plataforma').toBe(200);
        await expect(page).toHaveURL(url => url.pathname === ruta);
        await expect(page.getByRole('main')).toHaveCount(1);
        await page.evaluate(() => { document.documentElement.style.fontSize = '200%'; });
        const dimensiones = await page.evaluate(() => ({
          contenido: document.documentElement.scrollWidth,
          viewport: document.documentElement.clientWidth
        }));
        expect(dimensiones.contenido, 'reflujo sin scroll lateral a 200%: ' + ruta + ' ' + width)
          .toBeLessThanOrEqual(dimensiones.viewport + 2);
        let focoVisible = false;
        for (let intento = 0; intento < 120; intento++) {
          await page.keyboard.press('Tab');
          focoVisible = await page.evaluate(() => {
            const el = document.activeElement as HTMLElement | null;
            if (!el || el === document.body) return false;
            const rect = el.getBoundingClientRect();
            const css = getComputedStyle(el);
            return rect.width > 0 && rect.height > 0 &&
              css.visibility !== 'hidden' && css.display !== 'none' &&
              (parseFloat(css.outlineWidth) > 0 || css.boxShadow !== 'none');
          });
          if (focoVisible) break;
        }
        expect(focoVisible, 'foco perceptible por Tab real a 200%: ' + ruta).toBe(true);
      }
    }
    expect(mutaciones, 'navegacion y reflujo no modifican datos').toEqual([]);
  });


  // A0.70: cuatro guias privadas nuevas, roles reales y control de teclado sin .focus().
  // Incremento positivo R2 en CI: JWT firmado, backend HTTP y MySQL 8.4 efimero.
  test('A0.70 - cuatro guias privadas JWT reales, semantica y teclado responsive', async ({ page, request }) => {
    const jwt = await ingresar(page, 'c1504_platform_only');
    const autorizado = await request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + jwt }
    });
    expect(autorizado.status(), 'La autoridad procede del backend real').toBe(200);
    const mutaciones: string[] = [];
    page.on('request', req => {
      if (req.url().includes('localhost:5005') &&
          /^(POST|PUT|PATCH|DELETE)$/.test(req.method())) {
        mutaciones.push(req.method() + ' ' + new URL(req.url()).pathname);
      }
    });
    for (const destino of [
          {
                "path": "/plataforma/continuidad-operativa-v5",
                "heading": "Controles de recuperación y aislamiento",
                "link": "Regresar a plataforma"
          },
          {
                "path": "/plataforma/revision-amenazas-v5",
                "heading": "Verificaciones de acceso y abuso",
                "link": "Volver a administración de plataforma"
          },
          {
                "path": "/plataforma/respuesta-incidentes-v5",
                "heading": "Respuesta, revocación y recuperación segura",
                "link": "Regresar a plataforma"
          },
          {
                "path": "/plataforma/gobierno-cambios-v5",
                "heading": "Decisiones y controles V5",
                "link": "Volver a administración de plataforma"
          }
    ]) {
      for (const ancho of [375, 1280]) {
        await page.setViewportSize({ width: ancho, height: 840 });
        const sesion = page.waitForResponse(res =>
          res.url().includes('/plataforma/sesion') && res.request().method() === 'GET');
        const navegacion = await page.goto(destino.path);
        expect(navegacion?.status(), destino.path + ' Angular localhost').toBe(200);
        expect((await sesion).status(), destino.path + ' JWT autorizado').toBe(200);
        await expect(page).toHaveURL(url => url.pathname === destino.path);
        await expect(page.getByRole('heading', { name: destino.heading, level: 1 })).toBeVisible();
        const regreso = page.getByRole('link', { name: destino.link });
        await expect(regreso).toBeVisible();
        const dimensiones = await page.evaluate(() => ({
          documento: document.documentElement.scrollWidth,
          viewport: document.documentElement.clientWidth
        }));
        expect(dimensiones.documento, 'no overflow horizontal: ' + destino.path + ' ' + ancho)
          .toBeLessThanOrEqual(dimensiones.viewport + 2);
        let alcanzable = false;
        for (let i = 0; i < 120; i++) {
          await page.keyboard.press('Tab');
          alcanzable = await regreso.evaluate(el => el === document.activeElement);
          if (alcanzable) break;
        }
        expect(alcanzable, 'Tab fisico debe alcanzar regreso: ' + destino.path).toBe(true);
      }
    }
    expect(mutaciones, 'La auditoria de lectura no muta recursos de API').toEqual([]);
  });

});
