import { expect, test } from '@playwright/test';
import { readFileSync } from 'node:fs';

// V21 C02-01.M04.02: JWT de B emitido ANTES de la revocacion real de su membresia.
// Se obtiene solo de RUNNER_TEMP en GitHub-hosted CI; sin mocks ni tokens fabricados.
const api = process.env['C1504_API_URL'];
const temp = process.env['RUNNER_TEMP'];
if (process.env['CI'] !== 'true' || api !== 'http://127.0.0.1:5005' || !temp) {
  throw new Error('C0201_REVOKE_PROOF_REQUIRES_EPHEMERAL_CI');
}
test.describe('C02-01.M04.02 - revocacion dinamica SQL con JWT firmado', () => {
  test.describe.configure({ retries: 0 });
  test('JWT B previamente valido no puede recuperar Empresa B tras UPDATE real', async ({ request }) => {
    const token = readFileSync(temp + '/c0201-m0402-jwt-b.txt', 'utf8').trim();
    expect(token.split('.'), 'JWT firmado previo a revocacion').toHaveLength(3);
    const headers = { Authorization: 'Bearer ' + token };
    const denied = await request.get(api + '/tenant-context/2', { headers });
    expect([401, 403, 404], 'revocacion no permite lectura de B ni HTTP 200/503').toContain(denied.status());

    const memberships = await request.get(api + '/tenant-context/empresas', { headers });
    if (memberships.status() === 200) {
      const payload: unknown = await memberships.json();
      expect(payload).toMatchObject({ success: true });
      const rows = (payload as { data: { empresaId: number }[] }).data;
      expect(Array.isArray(rows), 'respuesta de membresias tipada').toBe(true);
      expect(rows.map(x => x.empresaId), 'B revocada no debe aparecer').not.toContain(2);
    } else {
      expect([401, 403, 404], 'lista post revocacion debe fallar cerrada').toContain(memberships.status());
    }
  });
});
