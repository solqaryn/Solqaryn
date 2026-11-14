import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
const source = readFileSync(new URL('../../docs/v5/c00-01-m02-02-api-contracts.md', import.meta.url), 'utf8');
const types=['EmpresasController.cs','EmpresaDto.cs','TenantContextController.cs','ProductosController.cs','CreateProductoDto.cs','CreateProductoValidator.cs','LoginRequestValidator.cs','ExceptionHandlingMiddleware.cs'];
const terms=['C00-01.M02.02','PROJECT_SCOPE_LOCK=STRICT','PLAN_MAESTRO!H','COLA!E/G/O','A5_PENDIENTE','A5_PASS','otro TASK_ID','git revert','Forbid()','tenant A','tenant B','400/403/404/409','No prueban .NET HTTP','NO_APLICA'];
function valid(src) {
  const text=src.normalize('NFC').toLowerCase();
  const rows=src.split('\n').filter(x=>x.startsWith('| ')&&x.includes('`backend/src/'));
  return rows.length===8 && types.every(x=>src.includes(x)) && terms.every(x=>text.includes(x.normalize('NFC').toLowerCase())) && rows.every(x=>/`[0-9a-f]{40}`/.test(x)) && /no expone[r]? detalles de infraestructura/i.test(src) && /sin force-push/.test(src);
}
test('A1 positivo: ocho contratos con fuentes Git y seguridad descrita',()=>assert.equal(valid(source),true));
test('A2 negativo: no conceder tenant por EmpresaId de cliente',()=>assert.equal(valid(source.replaceAll('tenant B','tenant cualquiera')),false));
test('A1 negativo: quitar Forbid debe fallar',()=>assert.equal(valid(source.replaceAll('Forbid()','Ok()')),false));
test('A1 negativo: no omitir middleware de errores',()=>assert.equal(valid(source.replaceAll('ExceptionHandlingMiddleware.cs','IgnoredMiddleware.cs')),false));
test('A2 negativo: no suprimir A5 independiente',()=>assert.equal(valid(source.replaceAll('A5_PENDIENTE','SELF_PASS')),false));
test('A3 negativo: no afirmar HTTP sin prueba',()=>assert.equal(valid(source.replace('No prueban .NET HTTP','Certifican .NET HTTP')),false));
test('A1 negativo: hash de fuente vacío no es evidencia',()=>assert.equal(valid(source.replace('cfedc20236e71db079529442ebf45b0431d339e2','0000')),false));
test('A3 negativo: no admitir force-push',()=>assert.equal(valid(source.replaceAll('sin force-push','con force-push')),false));
