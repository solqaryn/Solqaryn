import { ensureSeedAdminTenantMembershipForCi } from './tenant-bootstrap';
import verifyRealTenant from './global-setup';
import { prepareRouteFixtures } from './qa-route-fixtures';

export default async function setup(): Promise<void> {
  const connection = process.env.ConnectionStrings__DefaultConnection || '';
  if (process.env.CI !== 'true' || process.env.GITHUB_REF !== 'refs/heads/qa' ||
      process.env.E2E_TENANT_ID !== '1' || process.env.PHASE7_API_URL !== 'http://127.0.0.1:5005' ||
      !/^Server=127\.0\.0\.1;Port=3306;Database=solqaryn_qa_route_audit;User=root;Password=[^;]+;SslMode=Required;$/.test(connection)) {
    throw new Error('Bootstrap tenant denegado: solo se permite MySQL desechable del runner QA.');
  }
  ensureSeedAdminTenantMembershipForCi('1');
  await verifyRealTenant();
  await prepareRouteFixtures();
}
