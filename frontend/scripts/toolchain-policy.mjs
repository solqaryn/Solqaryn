// SOLQARYN: exact toolchain for CI; bounded platform compatibility for Vercel builds.
// Vercel exposes Node 24.x (not a fixed patch); do not relax repository pins.
const EXPECTED_NODE = '24.21.0';
const EXPECTED_NPM = '11.19.0';
const EXPECTED_NODE_ENGINE = '24.x';
const EXPECTED_NPM_ENGINE = '11.19.0';
const EXPECTED_PACKAGE_MANAGER = 'npm@11.19.0';

function parseTriple(version) {
  const match = typeof version === 'string' && /^(\d+)\.(\d+)\.(\d+)$/.exec(version);
  return match ? match.slice(1).map(Number) : null;
}

function allowedVercelNode(version) {
  const parts = parseTriple(version);
  return Boolean(parts && parts[0] === 24 && parts[1] >= 21);
}

function allowedVercelNpm(version) {
  const parts = parseTriple(version);
  return Boolean(parts && parts[0] === 11 && parts[1] === 19);
}

export function assessToolchain({ actualNode, actualNpm, isVercel = false, packageJson }) {
  const failures = [];
  const nodeAllowed = isVercel ? allowedVercelNode(actualNode) : actualNode === EXPECTED_NODE;
  const npmAllowed = isVercel ? allowedVercelNpm(actualNpm) : actualNpm === EXPECTED_NPM;
  if (!nodeAllowed) failures.push(`Node esperado ${isVercel ? '24.x (>=24.21.0) en Vercel' : EXPECTED_NODE}, resuelto ${actualNode}`);
  if (!npmAllowed) failures.push(`npm esperado ${isVercel ? '11.19.x en Vercel' : EXPECTED_NPM}, resuelto ${actualNpm || 'desconocido'}`);
  if (packageJson?.engines?.node !== EXPECTED_NODE_ENGINE) failures.push(`engines.node esperado ${EXPECTED_NODE_ENGINE}, resuelto ${packageJson?.engines?.node || 'ausente'}`);
  if (packageJson?.engines?.npm !== EXPECTED_NPM_ENGINE) failures.push(`engines.npm esperado ${EXPECTED_NPM_ENGINE}, resuelto ${packageJson?.engines?.npm || 'ausente'}`);
  if (packageJson?.packageManager !== EXPECTED_PACKAGE_MANAGER) failures.push(`packageManager esperado ${EXPECTED_PACKAGE_MANAGER}, resuelto ${packageJson?.packageManager || 'ausente'}`);
  return { failures, mode: isVercel ? 'VERCEL_BOUNDED_24X_11_19X' : 'EXACT_LOCAL_CI' };
}
