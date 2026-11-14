# N6.8 — Storage aislado por tenant — Certificación y runbook

## Propósito

Este documento certifica el alcance documental de `N6.8.H` sobre la cadena `N6.8.A–G`. Su autoridad operativa es `docs/VAEP_AUTHORITY.md`. No modifica runtime, esquema, migraciones, infraestructura, `main` ni Producción.

El objetivo de N6.8 es que logos, imágenes, PDFs, adjuntos y exports pertenezcan al tenant resuelto por servidor y no puedan leerse, listarse, descargarse, reemplazarse ni eliminarse desde otro tenant.

## Invariantes de aislamiento

1. **Tenant server-verified.** `EmpresaId` suministrado por cliente nunca concede autoridad. Toda operación sobre storage debe partir del tenant efectivo resuelto por la sesión/membresía autorizada.
2. **Fail-closed.** Ausencia de contexto tenant, membership inválida, owner distinto, locator fuera del namespace esperado, origen no permitido o mismatch de tenant deben rechazar la operación antes de tocar el proveedor de storage.
3. **Ownership antes de mutación.** Delete/replace/purge deben verificar tenant y ownership antes de cualquier mutación externa.
4. **Lectura y descarga aisladas.** Un locator/URL no basta para autorizar una descarga. Debe pertenecer al tenant efectivo y cumplir los controles de origen/transporte aplicables.
5. **Sin enumeración cross-tenant.** Listados y búsquedas deben quedar filtrados por el tenant efectivo; no se permite enumerar recursos de otra empresa mediante IDs, URLs o public IDs conocidos.
6. **Auditoría segura.** Los eventos de acceso denegado y mutación registran contexto suficiente para trazabilidad sin exponer secretos, contenido sensible ni credenciales del proveedor.
7. **Excepciones públicas explícitas.** Un recurso público sólo puede ser público por contrato explícito; la mera existencia de una URL accesible no convierte un recurso privado en público ni elimina la validación de ownership para operaciones de administración.

## Runbook de acceso tenant-aware

### Escritura / upload

- Resolver el tenant efectivo en servidor.
- Validar membership/permiso aplicable.
- Construir namespace o metadata de ownership a partir del tenant efectivo; no del `EmpresaId` recibido del cliente.
- Persistir locator/metadata de forma que el owner sea verificable en operaciones posteriores.
- Si falla cualquiera de los pasos anteriores, abortar sin crear un recurso huérfano o cross-tenant.

### Lectura / download

- Resolver tenant efectivo y permiso.
- Resolver el recurso desde una clave/locator perteneciente al tenant.
- Validar ownership y restricciones de origen/transporte antes de devolver o firmar acceso.
- Rechazar locators manipulados, namespaces de otra empresa y rutas inseguras.

### Eliminación / reemplazo

- Resolver tenant efectivo y permiso.
- Cargar/verificar el owner antes de llamar al proveedor.
- Rechazar mismatch sin side effects externos.
- Ejecutar la mutación y conservar evidencia/auditoría consistente con el resultado.

## Retención, expiración y purga

- La política de retención se evalúa dentro del tenant propietario; nunca se purgan recursos por una búsqueda global no acotada por tenant.
- La expiración debe conservar suficiente metadata para demostrar owner, motivo y momento de purga cuando la normativa/operación lo requiera.
- Una purga masiva debe ser reanudable e idempotente y volver a validar ownership antes de eliminar cada recurso.
- Recursos referenciados por documentos empresariales deben respetar las reglas de conservación de esos documentos; N6.8 no autoriza borrado retroactivo que rompa trazabilidad.
- En rollback, se revierte lógica/configuración de acceso antes de intentar cualquier manipulación destructiva del contenido almacenado. No se ejecuta rollback ni purga en Producción desde esta certificación.

## Respuesta ante incidente de aislamiento

1. Contener el endpoint/operación afectada de forma fail-closed.
2. Preservar evidencia: tenant efectivo, recurso, operación, correlation id y resultado, sin secretos.
3. Determinar si hubo sólo intento denegado o exposición/mutación real.
4. Corregir el boundary causal mínimo y ejecutar regresión tenant negativa antes de reabrir.
5. No resolver un incidente debilitando RBAC, aceptando `EmpresaId` de cliente como autoridad o haciendo público un recurso privado.

## Evidencia A–G verificada

| Etapa | Resultado | Evidencia canónica |
| --- | --- | --- |
| N6.8.A PRE | LISTO_REAL | receipt/control `abbf3746ba43a6c85c694813d5a9a65306870857`; inventario de storage clasificado por privacidad/ownership/expiración |
| N6.8.B DOMAIN | LISTO_REAL | receipt `749c17f0750a58e27ad637c238ed56ca8dba6dbc`; build/tests dirigidos y tenant/file-security PASS |
| N6.8.C DB_MIG | LISTO_REAL | receipt `579fb189cbeb916e8439368ecbbb011aec334b45`; `NO_SCHEMA_DELTA_REQUIRED`, REVIEW_FIRST P0=0/P1=0 |
| N6.8.D BACKEND_API | LISTO_REAL | receipt `c12a81b8718433d3a69ef2c6fa1324a623b3b486`; candidate `792078bfd46321fb84a31c1d7ec60bd6d5a00247`; storage/API/tenant gates verdes |
| N6.8.E FRONTEND_UX | LISTO_REAL | receipt `d26969a9627283f1b98940e7d8cb78c4fa58f123`; candidate `c9d600fb7765bd129e826302a546307485b83e5d` |
| N6.8.F SEC_AUDIT | LISTO_REAL | receipt `b9ec34b1c492508fcdc4cdce5773c4735a937f57`; candidate `5c24db6ae4f4ab5ab9171c7907d764d2c1d05bd0`; causal security gates SUCCESS |
| N6.8.G TEST_CI | LISTO_REAL | receipt `c8cfca7a1cff042d72f392c19b4000837beb378e`; REVIEW_FIRST `116d5502548b2966aaa1a44157fa9a5d6b1766f7`; causal storage-isolation regression run `34714029058`, job `103607801664`, SUCCESS |

La evidencia de N6.8.G certifica la regresión final de ownership/locators/file-security; checks de deploy no causales o fuera de alcance no sustituyen ni bloquean esta matriz causal.

## Criterio de cierre N6.8.H

N6.8.H sólo puede emitir `LISTO_REAL` cuando, en el mismo ciclo de cierre:

- esta documentación tenant-aware y el impacto en `CHANGELOG_AI.md` estén persistidos y releídos;
- la cadena A–G esté enlazada y no exista contradicción canónica;
- `REVIEW_FIRST` resulte `PASS` con `P0=0` y `P1=0`;
- los gates requeridos/aplicables/causales estén en PASS o explícitamente `NOT_APPLICABLE` con justificación; y
- el receipt final quede persistido y releído antes de promover el siguiente parent dependency-valid.

## DoD documental

- [x] Alcance storage aislado y límites documentados.
- [x] Acceso tenant-aware y comportamiento fail-closed documentados.
- [x] Retención/expiración/purga y rollback operativo documentados.
- [x] Evidencia A–G enlazada con SHAs/runs causales.
- [x] Prohibiciones de Producción, secretos y debilitamiento de gates preservadas.
- [ ] `CHANGELOG_AI.md` actualizado y releído.
- [ ] `REVIEW_FIRST` de H con P0=0/P1=0.
- [ ] Receipt `LISTO_REAL` de H persistido y releído.

Hasta completar los tres puntos pendientes, este documento constituye **delta material de N6.8.H**, no un `LISTO_REAL` por sí solo.
