import { expect, test, type Page } from '@playwright/test';
import { readFileSync } from 'node:fs';

const api = process.env['C1304_API_URL'];
const password = process.env['P4_TEST_PASSWORD'];
if (api !== 'http://127.0.0.1:5005' || !password) {
  throw new Error('Solo CI aislada con API localhost y contrasena efimera.');
}

async function autenticarEnPantalla(page: Page, usuario: string): Promise<void> {
  await page.goto('/login');
  await page.getByRole('textbox', { name: 'Nombre de usuario' }).fill(usuario);
  await page.locator('input[formcontrolname="password"]').fill(password!);
  const respuesta = page.waitForResponse(r =>
    r.url() === 'http://localhost:5005/auth/login' && r.request().method() === 'POST');
  await page.getByRole('button', { name: /Ingresar al sistema/ }).click();
  const login = await respuesta;
  expect(login.status()).toBe(200);
  const cuerpo = await login.json();
  const token = cuerpo?.data?.token;
  expect(typeof token).toBe('string');
  expect(token.split('.')).toHaveLength(3);
  await expect(page.getByRole('button', { name: /Acceder a la consola global/ })).toBeVisible();
}

test.describe('C13-04.M05.04 - navegador privado y API verdaderos en CI', () => {
  test.describe.configure({ retries: 0 });

  test('usuario global firmado abre pantalla privada y muestra rutas registradas', async ({ page }) => {
    await autenticarEnPantalla(page, 'c1304_platform_only');
    const sesionApi = page.waitForResponse(r =>
      r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
    await page.goto('/plataforma/inventario-rutas-v19');
    const sesion = await sesionApi;
    expect(sesion.status()).toBe(200);
    const respuesta = await sesion.json();
    expect(respuesta?.data?.ambito).toBe('Plataforma');
    expect(respuesta?.data?.esAdministrador).toBe(true);
    await expect(page).toHaveURL(/\/plataforma\/inventario-rutas-v19$/);
    await expect(page.getByRole('heading', { name: 'Inventario de programas y rutas declaradas' })).toBeVisible();
    const rutas = page.getByTestId('c1304-route-catalogo').locator('li');
    expect(await rutas.count()).toBeGreaterThan(50);
    await expect(page.getByText('Acceso global no confirmado; inventario privado oculto.')).toHaveCount(0);
  });

  test('administrador empresarial sin rol global recibe 403 y no abre inventario privado', async ({ page }) => {
    await autenticarEnPantalla(page, 'c1304_admin_a');
    const sesionApi = page.waitForResponse(r =>
      r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
    await page.goto('/plataforma/inventario-rutas-v19');
    const sesion = await sesionApi;
    expect(sesion.status()).toBe(403);
    await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
    await expect(page.getByTestId('c1304-route-catalogo')).toHaveCount(0);
  });

  test('API MySQL valida membresia empresa A, niega A hacia B y sin JWT', async ({ page, request }) => {
    await autenticarEnPantalla(page, 'c1304_admin_a');
    const jwt = await page.evaluate(() => localStorage.getItem('solqaryn_token'));
    expect(typeof jwt).toBe('string');
    const headers = { Authorization: 'Bearer ' + jwt };
    const propia = await request.get(api + '/tenant-context/1', { headers });
    expect(propia.status()).toBe(200);
    expect((await propia.json())?.data?.empresaId).toBe(1);
    const ajena = await request.get(api + '/tenant-context/2', { headers });
    expect([401, 403, 404]).toContain(ajena.status());
    const anonima = await request.get(api + '/tenant-context/1');
    expect([401, 403]).toContain(anonima.status());
  });
});

test('JWT firmado autentico recorre cuatro rutas privadas protegidas distintas', async ({ page }) => {
  await autenticarEnPantalla(page, 'c1304_platform_only');
  const destinos = [
    { ruta: '/plataforma/cobertura-v5', titulo: 'Cobertura V5 · trazabilidad' },
    { ruta: '/plataforma/auditoria-operativa-v5', titulo: 'Controles de ejecución y evidencia' },
    { ruta: '/plataforma/gobierno-cambios-v5', titulo: 'Decisiones y controles V5' },
    { ruta: '/plataforma/revision-amenazas-v5', titulo: 'Verificaciones de acceso y abuso' },
  ] as const;
  for (const destino of destinos) {
    await test.step(destino.ruta, async () => {
      const documento = await page.goto(destino.ruta);
      expect(documento?.status()).toBe(200);
      await expect.poll(() => new URL(page.url()).pathname).toBe(destino.ruta);
      await expect(page.getByRole('heading', { name: destino.titulo, exact: true })).toBeVisible();
      await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toHaveCount(0);
    });
  }
});

test('JWT global recorre cada ruta estatica del catalogo plataforma en el HEAD', async ({ page }) => {
  // Matriz derivada del Router real del SHA que compila GitHub Actions.
  // Solo rutas planas con plataformaGuard: no equivale a cobertura de rutas hijas ni revocacion.
  const fuente = readFileSync('src/app/app.routes.ts', 'utf8');
  const rutas = [...fuente.matchAll(/\{\s*path:\s*'([^']+)',\s*canActivate:\s*\[plataformaGuard\]/g)]
    .map(coincidencia => coincidencia[1]);
  expect(rutas.length).toBeGreaterThanOrEqual(19);
  expect(new Set(rutas).size).toBe(rutas.length);
  expect(rutas).toContain('plataforma/inventario-rutas-v19');
  await autenticarEnPantalla(page, 'c1304_platform_only');
  for (const ruta of rutas) {
    await test.step(ruta, async () => {
      const respuesta = await page.goto('/' + ruta, { waitUntil: 'domcontentloaded' });
      expect(respuesta?.status()).toBe(200);
      await expect.poll(() => new URL(page.url()).pathname).toBe('/' + ruta);
      await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toHaveCount(0);
    });
  }
});
