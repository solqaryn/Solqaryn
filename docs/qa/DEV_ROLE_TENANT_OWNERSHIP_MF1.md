# SOLQARYN — Roles empresariales por propietario (Microfase 1)

Rama autorizada: `dev`. Scope: Roles/Usuarios/Permisos y migración Oracle MySQL.
Esta acta es evidencia técnica del cambio candidato, NO una declaración `LISTO`.

## Contrato de autorización

- La UI selecciona una empresa pero no autoriza el tenant; `RequierePermiso` valida JWT y membresía vigente, y luego marca `EmpresaId` autorizado.
- Solo ese `EmpresaId` autenticado se transmite desde `RolesController` a `RolService` al crear o duplicar.
- Un rol asignable requiere `Ambito=Empresa`, estado activo y uno de: `EmpresaId == destino` o `EmpresaId=NULL && EsSistema=true`.
- Un rol de `Ambito=Plataforma`, rol privado de B, plantilla no sistema con dueño nulo, rol inactivo o eliminado es inválido para A.
- El filtro EF no materializa roles personalizados sin dueño como plantillas de empresa, ni sus permisos relacionados.
- Un tenant no puede editar plantillas de sistema compartidas: ni metadata de rol, ni activación/desactivación, ni matriz de grants.
- El guard de escritura EF rechaza incluso INSERT directo de RolPermiso hacia un rol empresarial sin dueño y EsSistema=false; solo admite grants de plantillas válidas.

## Backfill histórico: criterios fail-closed

Migración `20261018200000_TenantRoleOwnershipDeterministicBackfill`. Opera únicamente sobre filas `Roles.Ambito=1, EsSistema=0, EmpresaId IS NULL`. Solo atribuye si existe exactamente UNA fila de empresa en todo el catálogo (incluidas inactivas), sin colisión de nombre. Con A/B deja todos los roles personalizados sin propietario en cuarentena: una membresía incluso única puede haber sido manipulada. Nunca usa UsuarioEmpresa, JWT, la empresa activa ni Usuario.RolId para inferir dueño.

Cuando exista ambigüedad, el rol permanece sin dueño, inaccesible por RBAC empresarial. No se elimina, no se eleva a plantilla ni se inventa owner. Una reparación futura de esa fila exige identidad empresarial verificada y autorización explícita.

Readback agregado seguro, de solo lectura, tras desplegar la migración DEV:

```sql
SELECT COUNT(*) AS personalizadas_sin_owner
FROM Roles
WHERE Ambito=1 AND EsSistema=0 AND EmpresaId IS NULL AND Activo=1 AND Eliminado=0;
```

Ese contador no expone PII. Revisar los casos positivos antes de prometer recuperación operacional de roles históricos. Los roles sin dueño continúan denegados incluso si el contador es positivo.

## Certificación causal

1. Pruebas unitarias: crear/duplicar en A, ausencia de contexto, rol B, plataforma, plantilla no sistema, matríz y estado de rol compartido.
2. Pruebas EF y MySQL local/efímero: filtro lectura A/B, membresía manipulada y rechazo de rol ambiguo; backfill sin inferencia de tenant.
3. CI exact-head y REVIEW_FIRST: P0/P1=0, build backend y gates MySQL del commit candidato.
4. Readback DEV de migración y smoke A/B con credenciales autorizadas; cero acceso cross-tenant. No cambiar Render/Vercel hasta integración validada.
5. QA/PROD y secretos fuera de alcance. No forzar restauración destructiva. Nunca revertir guards de seguridad dejando la API privada multiempresa abierta.

## Impacto y rollback

Domain: `Rol.EsAsignableAEmpresa`; Application: `IRolService`, `RolService`, `UsuarioService`, `PermisoService`; Infrastructure: `UsuarioScopeService`, filtros EF y migración; API: `RolesController`. API cambia contratos internos de `IRolService.CreateAsync`/`DuplicarAsync`, agrega `RolDto.EmpresaId`; no cambia rutas ni formato requerido de solicitudes. No se modifican Cloudinary, facturas, HMAC, outbox ni datos productivos.

Roll-forward es preferido si falla el backfill; la migración no ejecuta un `Down` que borre/quite propietarios. Los guards deben quedar vigentes y la cuarentena comercial conserva el fail-closed.
