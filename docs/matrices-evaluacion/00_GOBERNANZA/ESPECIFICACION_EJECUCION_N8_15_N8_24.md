# Especificación de ejecución — intervención N8.15–N8.24

## 0. Propósito, posición y frontera

Esta intervención crea un paréntesis obligatorio en el Plan Maestro y en `COLA` exactamente después de la fila 640.

Posición física canónica:

- `COLA` fila `640` = `N8.3.A`.
- `COLA` fila `641` = `N8.15.A`, primera microtarea de la intervención.
- `N8.15.A` → `N8.24.H` ocupan consecutivamente las filas `641` → `720`.
- `COLA` fila `721` = `N8.3.B`, primer punto del plan histórico que se retoma después de la intervención.
- No se permite conservar una segunda copia de N8.15–N8.24 al final o en otra zona de `COLA`.

Frontera causal inmediata:

- `N8.3.A` es el predecessor de `N8.15.A`.
- `N8.3.B` queda gateada por `N8.24.H`.
- Todo trabajo histórico posterior se preserva como historia/evidencia; no se borra, no se invalida automáticamente y tampoco se usa como autoridad autosuficiente.
- Las diez automatizaciones canónicas permanecen pausadas hasta reactivación explícita del propietario.

Regla de ejecución/reentrada:

`N8.3.A -> N8.15 -> N8.16 -> N8.17 -> N8.18 -> N8.19 -> N8.20 -> N8.21 -> N8.22 -> N8.23 -> N8.24 -> N8.3.B`

La auditoría forense de `N8.19` conserva un scope especial sobre `N8.6.G/H`, `N8.7.A-H`, `N8.8.A-H` y cualquier avance posterior; ese scope no cambia el ancla física ni el sucesor de reentrada.

Mientras el modo de intervención esté activo, `UNRELATED_WORKFLOW_BLOCKING_PROHIBITED` NO autoriza saltar a trabajo ajeno al paréntesis: no se trata de un blocker causal, sino de una prioridad/gate deliberado del propietario.

## 1. Principios absolutos

- Rama única de trabajo: `Desarrollo`.
- `main` intacta.
- Producción write = 0.
- PR #2 sin merge.
- Secretos/credenciales = 0 exposición.
- Single writer, lease, read-before-write y readback para cualquier scope material.
- No borrar historia Git ni receipts históricos.
- No fabricar `PASS`, `LISTO_REAL`, N/A ni evidencia.
- `EVIDENCE > DECLARATION`.
- Todo defecto interno detectado se corrige por `FIRST_DETECTOR_OWNS_RECOVERY` cuando sea seguro.
- Frontend no es autoridad de negocio: presenta/interactúa; backend/BD revalidan autorización, reglas, invariantes, integridad, totales, estados y tenant isolation.
- Ningún componente, servicio, tabla, ruta, endpoint o documento se elimina por apariencia. Clasificación obligatoria: `KEEP | CONSOLIDATE | DEPRECATE | REMOVE_SAFE | UNKNOWN`; `UNKNOWN` prohíbe eliminación.
- Eliminaciones físicas destructivas de BD requieren backup/restore certificado en `N8.21`; antes sólo pueden quedar clasificadas/deprecadas.

## 2. Orden correcto: arquitectura vs matrices

La secuencia evita diseñar sobre basura y evita refactorizar sin contrato:

1. `N8.15` inventaría el estado real completo; no borra.
2. `N8.16` fija la arquitectura objetivo, taxonomía padre→hijo, gobierno de matrices e IDs.
3. `N8.17` crea TODAS las matrices del inventario ya cerrado.
4. `N8.18` implementa/refactoriza/limpia contra esas matrices y la arquitectura objetivo.
5. `N8.19–N8.24` ejecuta la misión forense/pre-go-live y cierra el paréntesis con una certificación global de cero deuda técnica material pendiente dentro del alcance.

Por tanto: el contrato arquitectónico se define antes de las matrices; la reestructuración física y limpieza ocurre después de que las matrices estén especificadas.

## 3. Contrato de matriz obligatorio

Cada contrato UI material debe aparecer exactamente una vez en el catálogo:

- `SCREEN`: pantalla/ruta navegable.
- `BUSINESS_DIALOG`: diálogo/modal con contrato de negocio propio.
- `EMBEDDED_INTERACTIVE`: formulario/widget interactivo independiente.
- `SHELL`: shell, sidebar, header, navegación, guards visuales.
- `SHARED_PRIMITIVE`: primitive reutilizable con comportamiento/contrato propio.

No lleva matriz independiente un elemento puramente decorativo sin datos, permisos, flujo o interacción material.

Cada matriz debe contener como mínimo:

- ID estable; padre; hijo; ruta; componente; objetivo; actores; precondiciones; flujo feliz/alternativo; side effects; idempotencia.
- Campo por campo: fuente, tipo, required/default, autofill, server-calculated, visible/editable, permisos view/edit, sensibilidad/máscara, validación UX/backend, constraint BD y auditoría.
- API/DTO/servicio/caso de uso/entidad/tabla/relaciones/índices.
- tenant isolation, concurrencia y locking cuando aplique.
- estados `loading/empty/error/forbidden/read-only/offline-retry` cuando corresponda.
- acciones, permisos, confirmaciones, resultado y auditoría.
- seguridad/RBAC/tamper/cross-tenant.
- responsive y accesibilidad.
- pruebas unit/integration/contract/E2E/security/responsive/accessibility/CI.
- evidencia y P0/P1.

Estados permitidos de una matriz:

`BASELINE_CREATED -> LEVANTAMIENTO_COMPLETO -> SPEC_COMPLETE -> IMPLEMENTATION_REVIEWED -> CERTIFIED`

`CERTIFIED` exige evidencia material; un archivo `.md` por sí solo no certifica nada.

## 4. Alertas, confirmaciones y modales

Infraestructura objetivo:

- Una primitive global reutilizable de alerta/confirmación configurada por semántica: `INFO | SUCCESS | WARNING | ERROR | CONFIRM`.
- Un servicio compartido de toast/snackbar para notificaciones transitorias.
- Los dialogs complejos de negocio pueden ser componentes propios, pero reutilizan la primitive global para confirmaciones/alertas; queda prohibido crear un modal de alerta por pantalla.

## 5. N8.15 — Baseline arquitectónico e inventario exhaustivo

Objetivo: conocer exactamente qué existe antes de diseñar o eliminar.

### A PRE
- Fresh HEAD/authority/Sheet.
- Congelar baseline de inventario y método de conteo.
- Inventariar roots frontend/backend/docs/infra/config y dependencias.
- Prohibido borrar o renombrar en esta etapa.

### B DOMAIN
- Inventario de dominios, módulos, casos de uso, bounded areas, permisos y dependencias padre→hijo actuales.
- Detectar duplicados, aliases, cross-domain coupling y ownership ambiguo.

### C DB_MIG
- Inventario de DbContexts/providers, entidades, tablas, migraciones, FKs, constraints, índices y seeds/fixtures.
- Clasificar candidatos sin ejecutar DDL destructivo.

### D BACKEND_API
- Inventario de controllers/endpoints/DTOs/services/repos/jobs/middleware/integraciones/config.
- Trazar endpoint→caso de uso→persistencia→permiso.

### E FRONTEND_UX
- Inventario exhaustivo de routes/features/screens/components/forms/dialogs/widgets/menu/shell/guards/shared primitives.
- Generar candidatos de contrato UI y detectar duplicados/orphans.

### F SEC_AUDIT
- Mapear authn/authz/RBAC/tenant/auditoría/PII/logging/health/observabilidad a los contratos inventariados.
- Detectar reglas críticas confiadas sólo al cliente.

### G TEST_CI
- Validar inventario mediante búsquedas estáticas, builds/lint/tests dirigidos y referencias runtime/config razonables.
- Confirmar orphans/duplicados antes de clasificarlos como removibles.

### H DOC_CERT
- Publicar baseline canónico, catálogo de assets, dependency map y clasificación `KEEP/CONSOLIDATE/DEPRECATE/REMOVE_SAFE/UNKNOWN`.
- Fijar conteos exactos; cero estimaciones presentadas como definitivas.

## 6. N8.16 — Gobierno y catálogo de matrices

### A PRE
Consumir N8.15 certificado, congelar inventario y establecer reglas de cambio.

### B DOMAIN
Definir arquitectura objetivo `PADRE -> HIJO -> INTERFAZ/ACCIÓN`, IDs estables, ownership y relación con menú.

### C DB_MIG
Definir cómo cada matriz documenta fuente de dato, autollenado, server-calculated, constraints, índices y auditoría.

### D BACKEND_API
Definir backend autoritativo, contratos API/DTO, errores, idempotencia, tenant y autorización.

### E FRONTEND_UX
Definir contrato visual/UX, estados, responsive, accesibilidad, shell, dialogs y primitives reutilizables.

### F SEC_AUDIT
Definir RBAC por campo/acción, sensitive data, tamper tests, cross-tenant y fail-closed.

### G TEST_CI
Validar la plantilla contra muestras representativas y crear checks que detecten matrices incompletas/TBD injustificados.

### H DOC_CERT
Publicar `CATALOGO_MATRICES.md` con el conteo exacto y mapping 1:1 de cada contrato inventariado a su `MATRIX_ID`; ningún candidato material queda sin ID.

## 7. N8.17 — Matrices de TODAS las interfaces

### A PRE
Particionar el catálogo sin omisiones y congelar el conjunto objetivo.

### B DOMAIN
Completar objetivo, actores, invariantes y flujos de negocio de todas las matrices.

### C DB_MIG
Completar contratos de datos campo a campo, persistencia, integridad y trazabilidad BD.

### D BACKEND_API
Completar APIs/DTOs/servicios/permisos/errores/idempotencia/backend authority.

### E FRONTEND_UX
Completar rutas/componentes/acciones/estados/responsive/accesibilidad/modal/primitive de todas las matrices.

### F SEC_AUDIT
Completar RBAC de view/edit/action, sensitive data, auditoría, tenant, tamper/cross-tenant.

### G TEST_CI
Verificar exhaustividad y trazabilidad matriz→código→test→evidencia. Prohibidos `TBD` silenciosos.

### H DOC_CERT
100% de los contratos del catálogo deben existir en archivo y quedar al menos `SPEC_COMPLETE`. Sólo los que además coincidan materialmente con implementación + pruebas pueden quedar `CERTIFIED`.

## 8. N8.18 — Arquitectura limpia y limpieza certificada

### A PRE
Crear plan de refactor por dependencias y orden seguro usando N8.15–N8.17; establecer rollback por changeset.

### B DOMAIN
Implementar límites padre→hijo, ownership y naming; eliminar/consolidar duplicidad lógica demostrada.

### C DB_MIG
Aplicar únicamente cambios de integridad/modelo seguros y verificables. DDL destructivo queda prohibido hasta N8.21; registrar candidatos post-backup.

### D BACKEND_API
Reorganizar backend por dominio/caso de uso sin duplicar reglas; consolidar servicios/endpoints repetidos; backend vuelve a validar toda decisión crítica.

### E FRONTEND_UX
Reorganizar menú/routes/features conforme al catálogo padre→hijo; consolidar primitive global de alertas/confirmaciones y toast; retirar componentes dead/duplicados sólo con `REMOVE_SAFE`.

### F SEC_AUDIT
Revalidar RBAC, tenant, auditoría, authz, secrets/PII, logging y no-bypass después de la refactorización.

### G TEST_CI
Build/lint/unit/integration/E2E/security/responsive/accessibility + regresión dirigida de rutas/menú; P0=0/P1=0.

### H DOC_CERT
Actualizar matrices a `IMPLEMENTATION_REVIEWED` o `CERTIFIED` según evidencia; reconciliar documentación superseded; conservar historia. Candidatos destructivos de BD quedan explícitamente encadenados a N8.21 y no se consideran olvidados.

## 9. N8.19 — Auditoría forense desde N8.6.G

Ejecutar la FASE 0 completa de la misión pre-go-live:

- revisar mínimo `N8.6.G/H`, `N8.7.A-H`, `N8.8.A-H` y cualquier avance posterior;
- reconstruir `DEPENDENCY -> REVIEW_FIRST -> LEASE -> WORK -> TESTS -> CANDIDATE_HEAD -> GATE -> RECEIPT -> LISTO_REAL -> NEXT`;
- investigar timestamps sospechosos, incluido N8.7.E y su correction receipt;
- revalidar todo N/A con prueba causal;
- revalidar blocker de performance N8.7.G usando herramientas DEV seguras si existen;
- clasificar cada fila como `CONFIRMED_LISTO_REAL | N_A_JUSTIFIED | REOPEN_REQUIRED | BLOCKER_CONFIRMED | STALE_CONTROL_ONLY | SUPERSEDED_BY_LATER_EVIDENCE`;
- crear evidencia forense append-only; no reescribir receipts históricos.

N8.19.H sólo cierra cuando la historia desde N8.6.G tiene dictamen causal verificable.

## 10. N8.20 — Motor y proveedor real de BD DEV

Certificar runtime real: engine/version/EF provider/migration provider/proveedor/plan/backup/PITR/retention/restore capability sin exponer secretos ni comprar recursos. La mención histórica de un proveedor no es prueba.

N8.20.H requiere `PASS_PROVIDER_IDENTIFIED`.

## 11. N8.21 — Backup real + restore aislado

- Probar backup automático real de la instancia DEV concreta.
- Cuando sea útil/autorizado, backup lógico temporal consistente sin commitear dumps.
- Restore aislado real, nunca sobre DEV activo.
- Verificar schema/migrations/tables/FKs/indexes/conteos/hashes/sanity sin exponer PII.
- Smoke temporal del backend contra restore cuando sea seguro.
- Cleanup completo.
- Reconciliar N8.8/N8.9 sólo con evidencia nueva causal.
- Una vez certificado backup/restore, ejecutar o cerrar formalmente los candidatos de limpieza física DB que N8.18 dejó bloqueados por seguridad; al cerrar N8.21 no puede quedar `REMOVE_SAFE_DB_AFTER_BACKUP` pendiente.

N8.21.H requiere `BACKUP_REAL=PASS` y `RESTORE_ISOLATED=PASS`.

## 12. N8.22 — STAGING_EQUIVALENT

Autorización limitada: metadata PROD no secreta/read-only únicamente para matriz de equivalencia. Prohibidos datos productivos, secrets, writes, deploys, DNS/certificados/main/migraciones PROD.

Clasificar diferencias: `EQUIVALENT | EXPECTED_ENV_DIFFERENCE | MATERIAL_GAP | UNKNOWN`. `UNKNOWN` o MATERIAL_GAP pendiente impide certificación. Corregir sólo DEV.

N8.22.H requiere `STAGING_EQUIVALENT_CERTIFIED`.

## 13. N8.23 — Rollback DEV + runbooks ejecutables

### Rollback DEV
Demostrar `CURRENT -> PREVIOUS_KNOWN_GOOD -> HEALTHY -> CURRENT -> HEALTHY` en backend DEV y frontend DEV usando mecanismos reversibles; medir tiempos; DB rollback mediante restore verificado/forward-fix, nunca Down destructivo sobre DB activa.

### Runbooks
Crear y dry-run seguro de:

- `GO_LIVE_MIGRATION_RUNBOOK.md`
- `GO_LIVE_SMOKE_RUNBOOK.md`
- `HYPERCARE_RUNBOOK.md`

Deben contener comandos/patrones reales, nombres reales, preconditions, STOP conditions, rollback, evidencia, P0/P1 y cero placeholders técnicos decorativos.

N8.23.H requiere rollback/forward recovery PASS + tres runbooks PASS + dry-run PASS.

## 14. N8.24 — GO_LIVE_GATE deliberado + cierre cero deuda

Implementar gobierno de N9.4:

- `GO_LIVE_GATE_MODE=DELIBERATE_OWNER_GATE`
- `GO_LIVE_AUTHORIZED=FALSE`
- readiness calculable y auditable;
- decisión humana planificada NO se clasifica como blocker técnico;
- una sola notificación deduplicada cuando el gate esté listo;
- cero Producción mientras no exista autorización futura explícita.

Casos mínimos de prueba:

1. READY=false/AUTHORIZED=false -> no go-live.
2. READY=true/AUTHORIZED=false -> `READY_FOR_GO_LIVE`, cero Production write.
3. READY=true/AUTHORIZED=true -> admisión sólo en futura ejecución realmente autorizada.
4. gate version ya notificada -> no duplicado.
5. prerequisito técnico falla -> blocker técnico causal.

### Certificación final de intervención en N8.24.H

Antes del handoff, releer desde cero repo + PLAN_MAESTRO + COLA + CONFIG + CONTROL_TOWER y exigir:

- todos N8.15–N8.24 cerrados causalmente;
- `COLA!641:720` contiene exactamente N8.15.A–N8.24.H una sola vez y en secuencia;
- `COLA!721` vuelve a `N8.3.B`;
- catálogo de matrices sin contratos materiales omitidos;
- cero `TBD`/`UNKNOWN` injustificado;
- cero `REMOVE_SAFE` pendiente;
- cero `REMOVE_SAFE_DB_AFTER_BACKUP` pendiente;
- matrices actualizadas al estado real (`SPEC_COMPLETE`, `IMPLEMENTATION_REVIEWED` o `CERTIFIED` sin inflar estados);
- P0=0/P1=0;
- code/docs/build/test state limpio;
- main intacta;
- Production write=0;
- secrets exposed=0;
- GATE-N8 depende también de la intervención;
- determinar la reentrada real en `N8.3.B`, utilizando N8.19 para no repetir trabajo histórico confirmado ni proteger cierres inválidos.

Sólo entonces desactivar el modo de intervención y permitir reentrada al flujo normal.

## 15. Condición de éxito del paréntesis

La intervención no se considera terminada sólo porque existan documentos o filas verdes. Éxito exige que arquitectura, matrices, limpieza, auditoría forense, proveedor DB, backup/restore, staging, rollback, runbooks y GO_LIVE_GATE tengan evidencia material y estado coherente.

Única apertura deliberada permitida al final: `GO_LIVE_AUTHORIZED=FALSE` si toda la preparación técnica está lista; eso es una decisión futura del propietario, no deuda técnica.
