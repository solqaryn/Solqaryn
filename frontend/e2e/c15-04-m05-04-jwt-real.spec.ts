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

});
