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


test('revocacion de rol global invalida JWT aun vigente en API y navegador', async ({ page, request }) => {
  // Solo CI MySQL 8.4 efimero: la autoridad se consulta de nuevo en cada solicitud.
  const { execFileSync } = await import('node:child_process');
  const ids = execFileSync('docker', [
    'ps', '--filter', 'ancestor=mysql:8.4', '--format', '{{.ID}}'
  ], { encoding: 'utf8' }).trim().split('\n').filter(Boolean);
  expect(ids).toHaveLength(1);
  const containerId = ids[0];
  expect(containerId).toMatch(/^[0-9a-f]+$/);
  const sql = (statement: string): string => execFileSync('docker', [
    'exec', containerId, 'mysql', '-uroot', '-proot',
    'solqaryn_c1304_real_ci', '-Nse', statement
  ], { encoding: 'utf8' }).trim();
  const where = "u.NombreUsuario='c1304_platform_only' AND r.NombreNormalizado='C1304_GLOBAL_CI'";
  const from = ' FROM UsuarioRolesPlataforma ur JOIN Usuarios u ON ur.UsuarioId=u.Id JOIN Roles r ON ur.RolId=r.Id ';
  expect(sql('SELECT COUNT(*)' + from + 'WHERE ' + where + ' AND ur.Activa=1;')).toBe('1');

  await autenticarEnPantalla(page, 'c1304_platform_only');
  const jwt = await page.evaluate(() => localStorage.getItem('solqaryn_token'));
  expect(jwt).toMatch(/^[^.]+\.[^.]+\.[^.]+$/);
  const headers = { Authorization: 'Bearer ' + jwt };
  const antes = await request.get(api + '/plataforma/sesion', { headers });
  expect(antes.status()).toBe(200);

  let revocado = false;
  try {
    const cantidad = sql('UPDATE UsuarioRolesPlataforma ur JOIN Usuarios u ON ur.UsuarioId=u.Id JOIN Roles r ON ur.RolId=r.Id SET ur.Activa=0 WHERE ' + where + ' AND ur.Activa=1; SELECT ROW_COUNT();');
    expect(cantidad).toBe('1');
    revocado = true;
    const despues = await request.get(api + '/plataforma/sesion', { headers });
    expect(despues.status()).toBe(403);
    expect((await despues.text()).includes('Global CI C1304')).toBe(false);
    await page.goto('/plataforma/inventario-rutas-v19');
    await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
    await expect(page.getByTestId('c1304-route-catalogo')).toHaveCount(0);
  } finally {
    if (revocado) {
      const restauradas = sql('UPDATE UsuarioRolesPlataforma ur JOIN Usuarios u ON ur.UsuarioId=u.Id JOIN Roles r ON ur.RolId=r.Id SET ur.Activa=1 WHERE ' + where + ' AND ur.Activa=0; SELECT ROW_COUNT();');
      expect(restauradas).toBe('1');
      expect(sql('SELECT COUNT(*)' + from + 'WHERE ' + where + ' AND ur.Activa=1;')).toBe('1');
    }
  }
  const restaurado = await request.get(api + '/plataforma/sesion', { headers });
  expect(restaurado.status()).toBe(200);
});


test('Router recursivo importa rutas ERP y niega anonimato en Chrome real', async ({ page }) => {
  // Inventario del checkout exact-SHA; la expansion incluye imports anidados
  // pero no presupone que un usuario global tenga permisos ERP.
  const { dirname, resolve, sep } = await import('node:path');
  const base = resolve('src/app');
  const visitados = new Set<string>();
  const rutas = new Set<string>();
  const visitar = (archivo: string): void => {
    if (!archivo.startsWith(base + sep)) {
      throw new Error('Import de rutas fuera de src/app: ' + archivo);
    }
    if (visitados.has(archivo)) return;
    visitados.add(archivo);
    const fuente = readFileSync(archivo, 'utf8');
    for (const encontrada of fuente.matchAll(/\bpath\s*:\s*'([^']+)'/g)) {
      rutas.add(encontrada[1]);
    }
    for (const spread of fuente.matchAll(/\.\.\.([A-Z][A-Z0-9_]*_ROUTES)\b/g)) {
      const simbolo = spread[1];
      const linea = fuente.split('\n').find(fragmento =>
        fragmento.includes('import { ' + simbolo + ' } from '));
      expect(linea, 'Falta import concreto del spread ' + simbolo).toBeDefined();
      const modulo = linea!.match(/from\s+['"]([^'"]+)['"]/);
      expect(modulo, 'No se pudo resolver ' + simbolo).not.toBeNull();
      visitar(resolve(dirname(archivo), modulo![1] + '.ts'));
    }
  };
  visitar(resolve(base, 'app.routes.ts'));
  expect(visitados.size).toBeGreaterThanOrEqual(9);
  expect(rutas.size).toBeGreaterThan(120);
  for (const declarada of ['inventario/ajustes', 'facturas-proveedor',
                            'pedidos-venta', 'financieros']) {
    expect(rutas.has(declarada), 'Ruta importada ausente: ' + declarada).toBe(true);
  }

  // E2E negativo con router y guardas reales, sin page.route ni fixtures HTTP.
  // No afirma navegacion positiva ERP: requiere membresia y capacidad por modulo.
  for (const destino of ['/inventario/ajustes', '/facturas-proveedor',
                         '/pedidos-venta', '/centro-reportes/financieros']) {
    await test.step('anonimo ' + destino, async () => {
      const respuesta = await page.goto(destino);
      expect(respuesta?.status()).toBe(200);
      await expect(page).toHaveURL(/\/login(?:\?|$)/);
      await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible();
    });
  }
});


test('JWT real verifica empresa A y niega empresa B falsificada al abrir detalle dinamico', async ({ page, request }) => {
  // Toda la identidad procede del login real y el MySQL 8.4 efimero; la
  // eleccion de empresa en localStorage es una solicitud, nunca autoridad.
  await autenticarEnPantalla(page, 'c1304_admin_a');
  const token = await page.evaluate(() => localStorage.getItem('solqaryn_token'));
  expect(token).toMatch(/^[^.]+\.[^.]+\.[^.]+$/);
  const headers = { Authorization: 'Bearer ' + token };
  const correcta = await request.get(api + '/tenant-context/1', { headers });
  expect(correcta.status()).toBe(200);
  expect((await correcta.json())?.data?.empresaId).toBe(1);
  const externa = await request.get(api + '/tenant-context/2', { headers });
  expect([403, 404]).toContain(externa.status());

  // La navegacion privada positiva debe recuperar un contexto real despues
  // de recargar Angular y no depender de una señal inventada por el test.
  await page.evaluate(() => localStorage.setItem('solqaryn_empresa_solicitada_id', '1'));
  const verificada = page.waitForResponse(r =>
    r.url().includes('/tenant-context/1') && r.request().method() === 'GET');
  const perfil = await page.goto('/perfil');
  expect(perfil?.status()).toBe(200);
  expect((await verificada).status()).toBe(200);
  await expect(page).toHaveURL(/\/perfil$/);
  await expect(page.getByRole('heading', { name: 'Mi perfil' })).toBeVisible();
  await expect(page.getByText('Contexto empresarial no verificado')).toHaveCount(0);

  // Una empresa B inyectada por el navegador no puede habilitar una ruta
  // dinamica con el JWT legitimo de la empresa A; backend decide acceso.
  await page.evaluate(() => localStorage.setItem('solqaryn_empresa_solicitada_id', '2'));
  const rechazada = page.waitForResponse(r =>
    r.url().includes('/tenant-context/2') && r.request().method() === 'GET');
  const detalle = await page.goto('/productos/999999/editar');
  expect(detalle?.status()).toBe(200); // HTML SPA, no autorización backend
  expect([403, 404]).toContain((await rechazada).status());
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.getByRole('heading', { name: 'Mi perfil' })).toHaveCount(0);
});

test('revocacion de membresia empresa A invalida JWT aun firmado y bloquea pantalla privada', async ({ page, request }) => {
  // Solo CI MySQL 8.4 aislado. El JWT no pierde firma: la membresia se comprueba en backend.
  const { execFileSync } = await import('node:child_process');
  const ids = execFileSync('docker', [
    'ps', '--filter', 'ancestor=mysql:8.4', '--format', '{{.ID}}'
  ], { encoding: 'utf8' }).trim().split('\n').filter(Boolean);
  expect(ids).toHaveLength(1);
  const containerId = ids[0];
  expect(containerId).toMatch(/^[0-9a-f]+$/);
  const sql = (statement: string): string => execFileSync('docker', [
    'exec', containerId, 'mysql', '-uroot', '-proot',
    'solqaryn_c1304_real_ci', '-Nse', statement
  ], { encoding: 'utf8' }).trim();
  const where = "u.NombreUsuario='c1304_admin_a' AND ue.EmpresaId=1";
  const from = ' FROM UsuarioEmpresas ue JOIN Usuarios u ON ue.UsuarioId=u.Id ';
  expect(sql('SELECT COUNT(*)' + from + 'WHERE ' + where + ' AND ue.Activa=1;')).toBe('1');

  await autenticarEnPantalla(page, 'c1304_admin_a');
  const jwt = await page.evaluate(() => localStorage.getItem('solqaryn_token'));
  expect(jwt).toMatch(/^[^.]+\.[^.]+\.[^.]+$/);
  const headers = { Authorization: 'Bearer ' + jwt };
  const antes = await request.get(api + '/tenant-context/1', { headers });
  expect(antes.status()).toBe(200);
  await page.evaluate(() => localStorage.setItem('solqaryn_empresa_solicitada_id', '1'));
  const perfil = await page.goto('/perfil');
  expect(perfil?.status()).toBe(200);
  await expect(page).toHaveURL(/\/perfil$/);
  await expect(page.getByRole('heading', { name: 'Mi perfil' })).toBeVisible();

  let revocada = false;
  try {
    const filas = sql('UPDATE UsuarioEmpresas ue JOIN Usuarios u ON ue.UsuarioId=u.Id SET ue.Activa=0 WHERE ' +
      where + ' AND ue.Activa=1; SELECT ROW_COUNT();');
    expect(filas).toBe('1');
    revocada = true;
    const despues = await request.get(api + '/tenant-context/1', { headers });
    expect([403, 404]).toContain(despues.status());

    // La respuesta 200 del HTML SPA no concede autorización: lo decide el backend.
    const documento = await page.goto('/perfil');
    expect(documento?.status()).toBe(200);
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.getByRole('heading', { name: 'Mi perfil' })).toHaveCount(0);
  } finally {
    if (revocada) {
      const filas = sql('UPDATE UsuarioEmpresas ue JOIN Usuarios u ON ue.UsuarioId=u.Id SET ue.Activa=1 WHERE ' +
        where + ' AND ue.Activa=0; SELECT ROW_COUNT();');
      expect(filas).toBe('1');
      expect(sql('SELECT COUNT(*)' + from + 'WHERE ' + where + ' AND ue.Activa=1;')).toBe('1');
    }
  }
  const restaurada = await request.get(api + '/tenant-context/1', { headers });
  expect(restaurada.status()).toBe(200);
});


test('firma JWT alterada pierde acceso a API y pantalla privada en Chromium', async ({ page, request }) => {
  // Token original firmado por .NET; el test altera solamente su firma.
  await autenticarEnPantalla(page, 'c1304_platform_only');
  const original = await page.evaluate(() => localStorage.getItem('solqaryn_token'));
  if (!original) throw new Error('No se obtuvo JWT firmado de la API local.');
  expect(original).toMatch(/^[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+$/);
  const valido = await request.get(api + '/plataforma/sesion', {
    headers: { Authorization: 'Bearer ' + original }
  });
  expect(valido.status()).toBe(200);

  const partes = original.split('.');
  const firma = partes[2];
  const adulterada = (firma[0] === 'A' ? 'B' : 'A') + firma.slice(1);
  const invalido = [partes[0], partes[1], adulterada].join('.');
  expect(invalido).not.toBe(original);
  const denegado = await request.get(api + '/plataforma/sesion', {
    headers: { Authorization: 'Bearer ' + invalido }
  });
  expect(denegado.status()).toBe(401);

  // Recarga real de Angular, nunca page.route ni respuestas simuladas.
  await page.evaluate(token => localStorage.setItem('solqaryn_token', token), invalido);
  const documento = await page.goto('/plataforma/inventario-rutas-v19');
  expect(documento?.status()).toBe(200);
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.getByTestId('c1304-route-catalogo')).toHaveCount(0);
});


test('empresa A navega extension sectorial real; empresa B falsificada no entra', async ({ page, request }) => {
  // Ruta autenticada real (authGuard), sin page.route ni fixture HTTP.
  await page.goto('/extensiones-sectoriales');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.getByRole('heading', { name: 'Capacidades por rubro' })).toHaveCount(0);

  await autenticarEnPantalla(page, 'c1304_admin_a');
  const jwt = await page.evaluate(() => localStorage.getItem('solqaryn_token'));
  expect(jwt).toMatch(/^[^.]+\.[^.]+\.[^.]+$/);
  const headers = { Authorization: 'Bearer ' + jwt };
  const empresaA = await request.get(api + '/tenant-context/1', { headers });
  expect(empresaA.status()).toBe(200);
  expect((await empresaA.json())?.data?.empresaId).toBe(1);

  await page.evaluate(() => localStorage.setItem('solqaryn_empresa_solicitada_id', '1'));
  const validacionA = page.waitForResponse(r =>
    r.url().includes('/tenant-context/1') && r.request().method() === 'GET');
  const documento = await page.goto('/extensiones-sectoriales');
  expect(documento?.status()).toBe(200);
  expect((await validacionA).status()).toBe(200);
  await expect(page).toHaveURL(/\/extensiones-sectoriales$/);
  await expect(page.getByRole('heading', { name: 'Capacidades por rubro' })).toBeVisible();
  await expect(page.getByRole('status', { name: 'Estado de las capacidades' }))
    .toContainText('Sin módulos conectados');
  await expect(page.getByText('No conectado', { exact: true })).toHaveCount(4);

  // El estado del navegador es una solicitud no confiable: autoridad del backend.
  await page.evaluate(() => localStorage.setItem('solqaryn_empresa_solicitada_id', '2'));
  const denegacionB = page.waitForResponse(r =>
    r.url().includes('/tenant-context/2') && r.request().method() === 'GET');
  const html = await page.goto('/extensiones-sectoriales');
  expect(html?.status()).toBe(200); // HTML SPA no concede acceso.
  expect([403, 404]).toContain((await denegacionB).status());
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.getByRole('heading', { name: 'Capacidades por rubro' })).toHaveCount(0);
});


test('empresa A con grants reales recorre ERP lectura y ruta parametrizada sin mocks', async ({ page, request }) => {
  // Read pilot habilitado solamente en CI MySQL 8.4 efimero.
  await autenticarEnPantalla(page, 'c1304_admin_a');
  const jwt = await page.evaluate(() => localStorage.getItem('solqaryn_token'));
  expect(jwt).toMatch(/^[^.]+\.[^.]+\.[^.]+$/);
  const headers = { Authorization: 'Bearer ' + jwt };
  const permisosApi = await request.get(api + '/permisos/mis-permisos/empresa/1', { headers });
  expect(permisosApi.status()).toBe(200);
  const cuerpo = (await permisosApi.json())?.data;
  expect(cuerpo?.operacionMultiEmpresaRestringida).toBe(true);
  for (const [modulo, ruta] of [
    ['Productos', 'productos'], ['Clientes', 'clientes'], ['Proveedores', 'proveedores'],
  ] as const) {
    expect(cuerpo?.permisos).toContain(modulo + ':Ver');
    expect(cuerpo?.capacidadesHabilitadas).toContain(modulo + ':Ver');
    expect(cuerpo?.rutasHabilitadas).toContain(ruta);
  }

  await page.evaluate(() => localStorage.setItem('solqaryn_empresa_solicitada_id', '1'));
  for (const destino of [
    { ruta: '/productos', titulo: 'Productos' },
    { ruta: '/clientes', titulo: 'Clientes' },
    { ruta: '/proveedores', titulo: 'Proveedores' },
  ] as const) {
    await test.step('ERP permitido ' + destino.ruta, async () => {
      const permisosVerificados = page.waitForResponse(r =>
        r.url().includes('/permisos/mis-permisos/empresa/1') &&
        r.request().method() === 'GET');
      const html = await page.goto(destino.ruta);
      expect(html?.status()).toBe(200);
      expect((await permisosVerificados).status()).toBe(200);
      await expect(page).toHaveURL(new RegExp(destino.ruta + '$'));
      await expect(page.getByRole('heading', { name: destino.titulo, exact: true })).toBeVisible();
      await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toHaveCount(0);
    });
  }

  // Ruta dinamica con grant valido, pero sin fabricar datos comerciales.
  const detalle = await page.goto('/productos/999999');
  expect(detalle?.status()).toBe(200);
  await expect(page).toHaveURL(/\/productos\/999999$/);
  await expect(page.getByRole('heading', { name: 'Detalle de producto' })).toBeVisible();
  await expect(page.getByText('El producto no existe o fue eliminado.')).toBeVisible();

  const empresaB = await request.get(api + '/permisos/mis-permisos/empresa/2', { headers });
  expect([403, 404]).toContain(empresaB.status());
});
