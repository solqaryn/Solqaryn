// Fase 2/3: este guard es autoridad local/CI para Node/npm; Angular 21.2.x mantiene el mismo toolchain y Vercel lo ejecuta vía preinstall.
const expectedNode = '24.21.0';
const expectedNpm = '11.19.0';
const actualNode = process.versions.node;
const userAgent = process.env.npm_config_user_agent || '';
const npmMatch = userAgent.match(/^npm\/([^\s]+)/);
const actualNpm = npmMatch?.[1] || '';

const failures = [];
if (actualNode !== expectedNode) failures.push(`Node esperado ${expectedNode}, resuelto ${actualNode}`);
if (actualNpm !== expectedNpm) failures.push(`npm esperado ${expectedNpm}, resuelto ${actualNpm || 'desconocido'}`);

if (failures.length) {
  console.error('SOLQARYN_TOOLCHAIN=FAIL');
  for (const failure of failures) console.error(failure);
  process.exit(1);
}

console.log('SOLQARYN_TOOLCHAIN=PASS');
console.log(`NODE=${actualNode}`);
console.log(`NPM=${actualNpm}`);
