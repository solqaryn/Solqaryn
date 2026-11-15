# SOLQARYN V5 FINAL — C05-02.M02.03: persistencia, índices, migraciones y datos de prueba futuros para branding y presets

**Autor:** PRIMARY DEV 06 — JAVIER_CHATGPT, TASK_ID 6af16bbad26881918c283abe06e1f32a. **Rama exclusiva:** solqaryn/Solqaryn dev, PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-15.

**Fuentes:** Standard de Implementación V5 FINAL (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA), PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA, skill canónica, AGENTS.md y C05-02.M01.01–06. Se reclama M02.03 independiente con DAG F=VALIDADO/G=SI y COLA G/O=SI. **Esta entrega es DOCUMENTACIÓN de un contrato físico propuesto, NO migración ni pruebas de base ejecutadas.**

## 1. Estado real y riesgo de confundir niveles

- TemaVisual.cs es un tema GLOBAL con Id=1 y quince propiedades de color; no contiene EmpresaId. TemaVisualRepository/GetAsync tampoco es un repositorio tenant validado por este documento.
- ConfigEmpresa.cs tiene EmpresaId, Moneda, ZonaHoraria, valores operativos, Version y plantillas tenant; no contiene un historial de presets visuales certificado.
- Empresa contiene identidad/logo comercial; su referencia actual no prueba gestión versionada de assets por tenant. ThemeApplierService aplica un whitelist de colores hex y limpia tokens anteriores para evitar respuestas antiguas.
- No reutilizar TemaVisual Id=1 como si fuera una clave foránea tenant ni agregarle EmpresaId sin una migración futura evaluada. El reset a defaults NO equivale a rollback histórico.
- La especificación M02.01 define el modelo lógico, pero no crea tablas ni índices. Cualquier cambio físico futuro necesita control de migraciones, MySQL 8.4 efímera, pruebas A/B y autorización causal.

## 2. Esquema físico FUTURO propuesto, no creado

| ID | Tabla o proyección propuesta | Clave candidata y ownership | Política de almacenamiento y límite |
| --- | --- | --- | --- |
| T01 | TenantBrandProfile | (EmpresaId, ProfileId) | Cada perfil pertenece a una empresa verificada; no global |
| T02 | TenantBrandRevision | (EmpresaId, ProfileId, Revision) | Append-only y versión monotónica; revisión no equivale a publicación |
| T03 | TenantBrandPalette | (EmpresaId, ProfileId, Revision, TokenKey) | TokenKey de allowlist, valor HEX seguro; no CSS libre |
| T04 | TenantBrandAsset | (EmpresaId, AssetId, Revision) | Guardar referencia opaca y checksum, jamás JWT/secret/DSN |
| T05 | BrandPresetDefinition | (PresetId, PresetVersion) | Catálogo de definición global no autoriza acceso a ningún tenant |
| T06 | TenantPresetSelection | (EmpresaId, PresetId, PresetVersion) | Selección tenant validada; no copiadora de grants |
| T07 | TenantBrandPublication | (EmpresaId, PublishedVersion) | CAS y auditoría; una versión activa por empresa |
| T08 | TenantBrandAudit | (EmpresaId, OperationId) | Actor, instante, revisión y resultado redactados |
| T09 | TenantBrandPreview | (EmpresaId, PreviewId) | TTL definido, no afecta versión publicada ni a otra empresa |
| T10 | TenantBrandRollback | (EmpresaId, FromRevision, ToRevision, OperationId) | Compensación registrada, no borrar trazas previas |

**Estas diez tablas son hipótesis de diseño**, NO existen por efecto de esta M02.03. Evitar duplicar configuraciones de ConfigEmpresa o crear diez tablas innecesarias sin conciliación de arquitectura y pruebas; M03 sólo puede materializar subconjuntos justificados con CI e impacto verificados.

## 3. Índices y constraints futuros I01–I10

| ID | Constraint/índice conceptual | Caso adverso a bloquear |
| --- | --- | --- |
| I01 | PK/UK con EmpresaId primero en perfiles y versiones | Perfil de A se consulta con identidad B |
| I02 | FK tenant-aware entre perfil y revisión | Revisión de B asociada a perfil A |
| I03 | UK EmpresaId+ProfileId+Revision | Dos editores publican misma revisión con payload diferente |
| I04 | UK EmpresaId+ProfileId+Revision+TokenKey | Repetir token sobrescribe silenciosamente |
| I05 | Verificación TokenKey en catálogo permitido | CSS arbitrario o variable no reconocida |
| I06 | Check de HEX seguro y límite de caracteres | URL, javascript o payload gigante en color |
| I07 | UK EmpresaId+PublishedVersion o fila publicadora única | Dos publicaciones simultáneas de A |
| I08 | FK EmpresaId+AssetId en assets privados | B reutiliza logo o archivo de A |
| I09 | UK EmpresaId+OperationId en auditoría idempotente | Retry duplica eventos materiales |
| I10 | Índice de recuperación por EmpresaId+Version+UpdatedAt | Reversión toma revisión obsoleta o de otro tenant |

La configuración real de constraints, tamaños y tipos MySQL/EF se decide solo al implementar, tras comparar con índices y FKs actuales. Ningún statement SQL se aplica aquí.

## 4. Plan futuro de migración reversible M01–M08

| Etapa | Condición previa | Evidencia y rollback futuro obligatorio |
| --- | --- | --- |
| M01 | Cierre A5 independiente de M02, evaluación de tablas existentes | Snapshot/DDL exactos y matriz de compatibilidad con TemaVisual global |
| M02 | Migración aprobada en DB efímera MySQL 8.4 | Crear estructuras opcionales sin eliminar/alterar Id=1 |
| M03 | Datos origen por Empresa verificada | Backfill aislado y conciliación empresa A/B, sin copiar identidad ajena |
| M04 | Verificador de constraints/índices activos | Duplicados/scope/consistencia 0, errores deterministas |
| M05 | Aplicación dual-read controlada y segura | Fallback probado, no conceder permisos por flags del cliente |
| M06 | Activación por cohortes autorizadas | Una sola versión por empresa y auditoría de rollout |
| M07 | Restauración ensayada en DB descartable | Revierte sin pérdida contable, logs ni referencias de assets |
| M08 | Gate exact-SHA .NET/EF/MySQL, seguridad y UI si procede | Prohibir QA/main/PROD si CI falla o falta autorización |

Estas M01–M08 son pasos FUTUROS_NO_EJECUTADOS de migración, no microtareas nuevas del PLAN, ni grants, SQL o cambios del runner.

## 5. Fixtures sintéticos FUTUROS F01–F08 y oráculos

| Caso | Tipo | Datos sintéticos propuestos | Resultado esperado al ejecutar más adelante |
| --- | --- | --- | --- |
| F01 | positivo | Empresa A=101 y B=202, perfil separado | Lectura A sólo devuelve perfil A |
| F02 | positivo | A versión 1 -> 2 con actor autorizado | CAS publica una versión nueva y audita |
| F03 | positivo | A revierte 2 -> 1 | Revisión compensatoria, historial y trazas intactos |
| F04 | positivo | Color HEX válido en allowlist | Persiste y renderiza sin contraste ilegible |
| F05 | negativo | Payload A menciona ProfileId de B | FK/check tenant niega acceso y no filtra datos B |
| F06 | negativo | Dos publicaciones concurrentes con expectedVersion=1 | Solo un ganador, otro conflicto |
| F07 | negativo | Color contiene CSS/URL/script | Rechazo de esquema/servicio, sin inyección |
| F08 | negativo | Rollback solicita imagen o versión de B | Denegación sin mutar A ni B |

F01–F08 son datos y expectativas descritos, no fixtures creados, ni seeds sobre ninguna DB. Ningún CRUD, migración, restore o prueba HTTP/MySQL se ejecutó.

## 6. Seguridad, gates A0–A5, impacto y recuperación

**A0:** dos archivos Markdown, identificación exacta de fuentes, revisión de CHANGELOG sin encabezados vacíos/atribución cruzada y sin secretos. **A1:** revisar T01–T10, I01–I10, M01–M08 y F01–F08, con distinción entre existente/propuesto. **A2:** aislamiento A/B en claves y FKs, JWT/RBAC server-side, logos privados, tokens CSS seguros, CAS/versionado, auditoría redactada, retención y rollback. **A3:** contratos M01 y M02.01 existentes, otros M02 según su estado real y DAG/COLA F/G/H/I; no liberar sucesoras por VALIDANDO. **A4:** commit fast-forward CAS dev, Scope Lock y V5 completed/success SHA exacto, readbacks PLAN/COLA/DAG/BITACORA; compilar/runtime/MySQL NO_APLICA al diff solo documental (no llamarlos PASS). **A5:** otra Task Javier distinta del autor, sobre este SHA y fuentes exactas, antes de QA/main/PROD o de sucesora dependiente.

**Impacto y rollback de este artefacto:** solo docs/v5 y CHANGELOG_AI.md; revert normal en dev, conservar BITACORA. No cambiar EF, tablas, MySQL, backend, frontend, secrets, integración, proveedor, cuentas ni ambientes QA/main/PROD. Implementaciones reales quedan sujetas a nuevo preflight, CAS de owner, permisos, CI dirigida y prueba temporal de aislamiento.
