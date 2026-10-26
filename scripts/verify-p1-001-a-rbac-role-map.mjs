#!/usr/bin/env node
// P1-001.A: comprobación estrictamente read-only de contrato en código.
// No conecta a MySQL, no resuelve JWT ni afirma datos/controles HTTP de DEV.
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const files = new Map();
const get = (relative) => {
  if (!files.has(relative)) {
    files.set(relative, readFileSync(resolve(root, relative), 'utf8'));
  }
  return files.get(relative);
};
const expectFragments = (path, fragments) => {
  const source = get(path);
  for (const fragment of fragments) {
    assert.ok(source.includes(fragment), `Contrato ausente: ${path} :: ${fragment}`);
  }
};

const contract = [
  ['backend/src/Domain/Enums/AmbitoAutorizacion.cs',
    ['Empresa = 1', 'Plataforma = 2']],
  ['backend/src/Domain/Entities/Rol.cs',
    ['public bool EsSistema', 'public int? EmpresaId', 'public bool Activo',
      'public bool Eliminado', 'EsAsignableAEmpresa(int empresaId)',
      'AmbitoAutorizacion.Empresa', 'EmpresaId == empresaId',
      'EmpresaId is null && EsSistema']],
  ['backend/src/Domain/Entities/Usuario.cs',
    ['public int RolId', 'public Rol RolEntidad']],
  ['backend/src/Domain/Entities/UsuarioEmpresa.cs',
    ['public int UsuarioId', 'public int EmpresaId', 'public int RolId',
      'public bool Activa', 'ObtenerRolEfectivo(int usuarioId, int empresaId)']],
  ['backend/src/Domain/Entities/UsuarioRolPlataforma.cs',
    ['public int UsuarioId', 'public int RolId', 'public bool Activa']],
  ['backend/src/Domain/Entities/Permiso.cs',
    ['public string Codigo', 'public bool Activo', 'Ambito']],
  ['backend/src/Domain/Entities/RolPermiso.cs',
    ['public int RolId', 'public int PermisoId']],
  ['backend/src/Infrastructure/Persistence/Configurations/RolConfiguration.cs',
    ['CK_Roles_Ambito', 'CK_Roles_Ambito_Empresa',
      'HasIndex(r => r.NombreNormalizado).IsUnique()']],
  ['backend/src/Infrastructure/Persistence/Configurations/UsuarioEmpresaConfiguration.cs',
    ['builder.ToTable("UsuarioEmpresas")',
      'new { x.UsuarioId, x.EmpresaId }', '.IsUnique()']],
  ['backend/src/Infrastructure/Persistence/Configurations/UsuarioRolPlataformaConfiguration.cs',
    ['builder.ToTable("UsuarioRolesPlataforma"',
      'new { x.UsuarioId, x.RolId }', '.IsUnique()']],
  ['backend/src/Infrastructure/Persistence/Configurations/RolPermisoConfiguration.cs',
    ['builder.ToTable("RolPermisos")', 'new { p.RolId, p.PermisoId }']],
  ['backend/src/Infrastructure/Services/UsuarioScopeService.cs',
    ['membresia.Activa', 'empresa.Activa', 'rol.Activo',
      'rol.Ambito == AmbitoAutorizacion.Empresa',
      'rol.EmpresaId == empresa.Id', 'rol.EmpresaId == null && rol.EsSistema']],
  ['backend/src/Infrastructure/Services/PlataformaScopeService.cs',
    ['asignacion.Activa', 'rol.EsAdministrador',
      'rol.Ambito == AmbitoAutorizacion.Plataforma',
      'rol.EmpresaId == null', 'matching.Length == 1']],
  ['backend/src/Application/Services/RolService.cs',
    ['ExigirRolMutable(rol)', 'EmpresaId = empresaIdAutorizada']],
  ['backend/src/API/Controllers/RolesController.cs',
    ['[Authorize]', '[RequierePermiso(', 'TenantPermissionContext.GetAuthorizedEmpresaId']],
];

function asignableEmpresa(rol, empresaId) {
  return Number.isInteger(empresaId) && empresaId > 0 &&
    rol.activo && !rol.eliminado && rol.ambito === 1 &&
    (rol.empresaId === empresaId || (rol.empresaId === null && rol.esSistema));
}
function autoridadPlataforma(asignacion, usuario, rol) {
  return !!asignacion.activa && !!usuario.activo && !usuario.bloqueado &&
    !usuario.eliminado && !!rol.activo && !rol.eliminado &&
    !!rol.esAdministrador && rol.ambito === 2 && rol.empresaId === null &&
    asignacion.usuarioId === usuario.id && asignacion.rolId === rol.id;
}
function membresiaEmpresa(m, u, empresa, rol) {
  return m.activa && u.activo && !u.bloqueado && !u.eliminado &&
    empresa.activa && rol.activo && !rol.eliminado && rol.ambito === 1 &&
    m.usuarioId === u.id && m.empresaId === empresa.id &&
    m.rolId === rol.id && asignableEmpresa(rol, empresa.id);
}

function main() {
  assert.ok(process.argv.includes('--self-test'),
    'Solo ejecutar con --self-test; no es una migracion ni un test MySQL live');
  for (const [p, fragments] of contract) expectFragments(p, fragments);
  const report = get('docs/P1-001-A-MAPA-ROLES-DEV.md');
  for (const s of ['P1-001.A', 'UsuarioEmpresas', 'UsuarioRolesPlataforma',
    'Plataforma', 'personalizado', 'NO_VERIFICADO', 'solo conteos']) {
    assert.ok(report.includes(s), `Mapa documental incompleto: ${s}`);
  }

  const customA = { empresaId: 7, esSistema: false, ambito: 1,
    activo: true, eliminado: false, id: 10 };
  const plantilla = { ...customA, id: 11, empresaId: null, esSistema: true };
  const global = { ...customA, id: 20, empresaId: null, ambito: 2,
    esAdministrador: true };
  const huérfano = { ...customA, id: 21, empresaId: null };
  const usuario = { id: 15, activo: true, bloqueado: false, eliminado: false };
  const empresaA = { id: 7, activa: true };
  const base = { usuarioId: 15, empresaId: 7, rolId: 10, activa: true };
  const asignacion = { usuarioId: 15, rolId: 20, activa: true };

  const positivos = [
    asignableEmpresa(customA, 7),
    asignableEmpresa(plantilla, 8),
    membresiaEmpresa(base, usuario, empresaA, customA),
    autoridadPlataforma(asignacion, usuario, global),
    membresiaEmpresa({ ...base, rolId: 11 }, usuario, empresaA, plantilla),
  ];
  const negativos = [
    asignableEmpresa(customA, 8),
    asignableEmpresa(customA, 0),
    asignableEmpresa(global, 7),
    asignableEmpresa(huérfano, 7),
    asignableEmpresa({ ...customA, activo: false }, 7),
    asignableEmpresa({ ...customA, eliminado: true }, 7),
    membresiaEmpresa({ ...base, activa: false }, usuario, empresaA, customA),
    membresiaEmpresa(base, usuario, { ...empresaA, activa: false }, customA),
    membresiaEmpresa(base, usuario, empresaA, { ...customA, empresaId: 8 }),
    membresiaEmpresa({ ...base, rolId: 20 }, usuario, empresaA, global),
    autoridadPlataforma(asignacion, usuario, customA),
    autoridadPlataforma({ ...asignacion, activa: false }, usuario, global),
    autoridadPlataforma(asignacion, { ...usuario, bloqueado: true }, global),
    autoridadPlataforma(asignacion, usuario, { ...global, empresaId: 7 }),
  ];
  positivos.forEach((ok, i) => assert.equal(ok, true, `Positivo ${i + 1}`));
  negativos.forEach((ok, i) => assert.equal(ok, false, `Negativo ${i + 1}`));
  console.log(`PASS P1-001.A codigo: ${contract.length} fuentes; ${positivos.length} positivos; ${negativos.length} negativos sintéticos; MySQL/HTTP NO_VERIFICADO`);
}
main();
