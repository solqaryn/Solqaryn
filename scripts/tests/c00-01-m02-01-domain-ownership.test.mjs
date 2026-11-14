import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

const doc = readFileSync(new URL('../../docs/v5/c00-01-m02-01-domain-ownership.md', import.meta.url), 'utf8');
const paths = ['Empresa.cs','Usuario.cs','ContextoTenantActual.cs','Producto.cs','ProductoVariante.cs','MovimientoInventario.cs','Factura.cs','BaseEntity.cs','AuditableEntity.cs'];
const checks = ['C00-01.M02.01','PROJECT_SCOPE_LOCK=STRICT','PLAN_MAESTRO!H','COLA!E/G/O','A5_PENDIENTE','otro TASK_ID','git revert','BITACORA es apendicular','NO_APLICA','no prueban .NET en ejecución','EmpresaId? == null','tenant A','tenant B','DesdeMembresia','ExigirEmpresa','Vencimiento exige lote','Rol','PasswordHash'];
function valid(src) {
  const n = src.normalize('NFC').toLowerCase();
  const matrix = src.split('\n').filter(l => l.startsWith('| `'));
  return checks.every(k => n.includes(k.normalize('NFC').toLowerCase())) && paths.every(p => n.includes(p.toLowerCase())) && matrix.length >= 8 && /no conceder acceso por nulidad/i.test(src) && /no ejecutar migración mysql/i.test(src);
}
test('A1 positivo: matriz documental de entidades, ownership e invariantes completa',()=>assert.equal(valid(doc),true));
test('A2 negativo: no se permite otorgar acceso por EmpresaId historico nulo',()=>assert.equal(valid(doc.replace('no conceder acceso por nulidad','conceder acceso por nulidad')),false));
test('A2 negativo: tenant A nunca es equivalente a tenant B',()=>assert.equal(valid(doc.replaceAll('tenant B','tenant público')),false));
test('A2 negativo: no convertir documentación en migración MySQL',()=>assert.equal(valid(doc.replace('no ejecutar migración MySQL','ejecutar migración MySQL')),false));
test('A1 negativo: falta de fuente Empresa debe invalidar inventario',()=>assert.equal(valid(doc.replaceAll('Empresa.cs','EntidadSinFuente.cs')),false));
test('A3 negativo: no omitir límite del runtime .NET/HTTP',()=>assert.equal(valid(doc.replace('no prueban .NET en ejecución','certifican .NET en ejecución')),false));
test('A3 negativo: no omitir A5 del revisor ajeno',()=>assert.equal(valid(doc.replaceAll('A5_PENDIENTE','A5_PASS_AUTOR')),false));
test('A1 negativo: no omitir rollback aditivo',()=>assert.equal(valid(doc.replace('git revert','git reset --hard')),false));
