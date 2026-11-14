# C00-03.M01.03 — Auditoría de fuentes, código y evidencias V5 sin certificación implícita

**ID:** `C00-03.M01.03`, C00-03 Matriz integrada de alcance, M01 preparación. **Fuente única de requisitos:** `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` V5 FINAL, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. **Código autorizado:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Ejecutor:** Javier PRIMARY DEV 02, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`.

Esta microtarea **lee** contratos, código y evidencia disponible para establecer rastros auditables de cobertura. La lectura NO convierte cualquier requisito en implementado, probado, certificado, promocionado ni en permiso de producción. La matriz completa se desarrolla en unidades C00-03 posteriores; esta entrega no replica las 2.340 filas ni crea una nueva cola.

## Inventario causal de fuentes consultadas

La siguiente tabla recoge blobs originales **leídos en GitHub** sobre el HEAD de observación `f0d3cb2ecc6e516552dd4b1af9a073a262667d5a`. Son huellas de la fuente, **no** hashes de una prueba nueva ni constancia de disponibilidad productiva.

| Fuente del código/contrato | Git blob leído | Hecho delimitado |
| --- | --- | --- |
| `AGENTS.md` | `d7d6b2bd4abd015a332ee4a88c31a65eb2928c12` | Prioridad V5, exclusividad de dev y gobernanza |
| `.agents/skills/solqaryn-project-governance/SKILL.md` | `304e400d5c0f89fcfdcc65caece86c34276734b5` | Skill única, Scope Lock estricto y reglas de seguridad |
| `.agents/skills/solqaryn-project-governance/references/architecture.md` | `1931e1229752028229b6ad1bb3368038e2c6769b` | Arquitectura frontend/backend/MySQL y fronteras tenant |
| `docs/PROJECT_SCOPE_LOCK.md` | `a92ae64d46a82b5255919012b474f7ece430ed6b` | Aislamiento de repo, identidad y branch permitida |
| `docs/AUTOMATION_STATUS.md` | `07c64d13af933f0ac262540f11cb086aa37627bc` | Allowlist de Tasks y contrato operativo con readback nativo |
| `docs/AUTOMATION_V13.md` | `008837173c78a9b371f3490ad4d35f7f8da5634b` | Auditorías A0–A5, solo dev sin PR |
| `frontend/package.json` | `e6d030f643421cb343cc12617e742469e88223c4` | Scripts npm/Angular existentes; no se ejecutaron aquí |
| `backend/src/API/Solqaryn.API.csproj` | `2a7b33a0974d1ebe13676593d84b458b1243e037` | TargetFramework `net10.0`; no es prueba de API HTTP |
| `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj` | `6210d8b12abd77171d95d17e2292c1a319b89263` | EF/MySQL Oracle declarado, sin certificar DB |
| `.github/workflows/solqaryn-core-ci.yml` | `bcb173e96d2a202caa622a4e8e2972ab88d31526` | Jobs remotos condicionados al YAML, runner efímero |
| `docs/v5/c00-03-m01-01-scope.md` | `9ffadccec6006c7d0a0a3be3a0f782152a640f4b` | Objetivo/no-alcance/no-regresión de matriz integrada |
| `docs/v5/c00-03-m01-02-surfaces.md` | `8770c840b151baf29b245cbb6a1389cf525cdba4` | Entradas/salidas/actores/superficies del mismo bloque |

El documento institucional V5 y la hoja son fuentes con IDs/revisión de Google; **no** se atribuye hash Git a sus celdas. La evidencia de estados se revalida mediante `PLAN_MAESTRO!H`, `COLA!E/G/O`, `DAG_TECNICO` y `BITACORA!A:Z` (26 columnas físicas, EVENT_ID A, IDEMPOTENCY_KEY R), nunca a partir de un extracto histórico.

## Lectura de dependencias y status sin falsas conclusiones

El `DAG_TECNICO` marca `C00-03.M01.01–05` como `PREPARACION_TRANSVERSAL` con seis prerrequisitos: `C00-01.M02.01`, `C00-01.M02.02`, `C00-01.M02.03`, `C00-01.M02.04`, `C00-01.M02.05` y `C00-01.M02.06`. Esos seis estados se leyeron `LISTO`; el ID actual fue elegible exclusivamente para **análisis/documentación**. `C00-03.M01.06` y los lotes `M02–M06` requieren puertas de fase propias. Revalidar contra `C00-01` completo antes de implementar, declarar fase final o promover.

La ruta de validación depende del artefacto: para un documento es A0 diff/secretos/links, A1 aceptación positiva/negativa, A2 fronteras de seguridad, A3 integridad de fuentes, A4 SHA FF y readback. Para cambios de runtime, exigir jobs reales de .NET/Angular/MySQL/Playwright/Docker cuando correspondan. Un resultado `SKIPPED`, `IN_PROGRESS`, `CANCELLED` o un CI verde del SHA anterior no es PASS del candidato nuevo.

Al momento del readback previo a esta redacción, el HEAD `f0d3cb2e` tenía Scope Lock `SUCCESS`, frontend `SUCCESS` y jobs de backend/seguridad aún `IN_PROGRESS`. Esos valores son fotografía del SHA consultado, no certificación de una microtarea ni garantía de CI general verde. Los checks de un posterior commit documental deben leerse por su **propio SHA**.

## Matriz inicial evidencia disponible frente a evidencia exigida

| Contrato o superficie | Lo que está disponible para consulta | Lo que NO queda acreditado por esta lectura | Siguiente comprobación causal cuando exista implementación |
| --- | --- | --- | --- |
| Gobernanza V5 | Doc fuente, Sheet, DAG, Skill, Scope Lock | Cierre de las 2.340 unidades | Estado/owner/evento por ID y A0–A4 |
| Dominio y API | csproj net10, rutas/test en repo | Compilación de código futuro, HTTP, validaciones live | `dotnet test` exact-SHA, positivos/negativos |
| Seguridad multiempresa | Contrato JWT/BCrypt/RBAC, tenancy server-side | Aislamiento A/B live ni ausencia de PII | Test autorizaciones reales y denegación por defecto |
| Persistencia | Infrastructure EF Oracle MySQL | Migración MySQL 8.4/reversibilidad real | Job de DB específico con preflight y rollback |
| Frontend | `frontend/package.json`, contratos Angular | Interacción Chromium, accesibilidad de UI futura | npm lint/build y Playwright para alcance visual |
| Integraciones y archivos | Referencia Cloudinary/QuestPDF/SMTP | Archivos históricos migrados, R2 operativo | Tests con proveedor/autorización, inventario y firmas cuando aplique |
| Publicación | Workflow YAML, Git commits/check runs | A5_PASS automático, QA/main/PROD | A4 SHA exacto, revisor Task distinto y autorización separada |

## Casos positivos, negativos y límites de seguridad

**Positivo documental:** la lista inventariada contiene doce rutas y hashes Git verificables; separa autoridad institucional, superficie runtime y CI; identifica cada brecha de certificación antes de reclamar implementación. **Negativos fail-closed:** no convertir la existencia de un csproj en test .NET, ni un workflow en job ejecutado; no inferir tenancy seguro porque aparece JWT en un documento; no aceptar `EmpresaId`, tenant_id, slug, URL ni cabecera cliente como permisos; no afirmar PASS por SKIPPED; no modificar responsable/estado de otra Task; no certificar QA/main/PROD por A5_PENDIENTE; no usar backlog V3/V13.

**No-alcance:** solo este documento y `CHANGELOG_AI.md`; no toca código, frontend, API, secretos, roles, datos, proveedores, branches, PR, DNS, Vercel/Render/Aiven, Hostinger/Cloudflare/R2 ni producción. Cero imágenes históricas implica migración `NO_APLICA`, no una excepción a la seguridad de archivos futuros.

**Clasificación:** `TIPO1_DOCUMENTACION`; `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`; `requiere_security=SI` (auditar frontera/secretos documentales), `requiere_scope_lock=SI` (commit exact-SHA). A0–A3 específicos del documento, A4 readback Plan/COLA/BITACORA y SHA Scope Lock; `A5_PENDIENTE` para otro TASK_ID.

**Rollback:** `git revert` del commit único sobre dev tras comparar HEAD, sin force-push, sin borrar BITACORA ni revertir commits de otras Tasks. Este documento NO completa toda C00-03 ni habilita `M01.06`.


## Corrección causal A5_FAIL — esquema físico BITACORA A:Z/26 (2026-11-13)

El encabezado vivo de `BITACORA!A1:Z1` contiene **26 campos**; A=`EVENT_ID`, H=`RUN_ID`, R=`IDEMPOTENCY_KEY` y Z=`TIMESTAMP_UTC_ORIGINAL`. Los eventos nuevos usan 26 posiciones contiguas, EVENT_ID e IDEMPOTENCY_KEY únicos, autor y revisor acreditados y readback posterior. Si no se acredita RUN_ID nativo, H queda vacío. Los registros históricos de 21 campos y el A5_FAIL del SHA original se conservan sin borrado ni relleno ficticio.

**Positivo:** verificar 26 nombres y posiciones reales A:Z, correlación PLAN_MAESTRO/COLA, evento nuevo no duplicado y evidencia del SHA exacto. **Negativos:** rechazar vectores de 21 a 25 campos, EVENT_ID o IDEMPOTENCY_KEY repetidos, suplantación de TASK_ID/revisor, A5 autoprobado, un job SKIPPED como PASS exigido y promoción sin autorización a QA/main/PROD. La documentación no cambia runtime, MySQL, Angular, proveedores ni gates de otra fase; dotnet/Node/Angular/MySQL/Docker/E2E son NO_APLICA al diff documental, pero Scope Lock y seguridad documental sí aplican. Rollback de un commit sólo mediante `git revert` normal, sin force-push. Se solicita nueva A5 por Task distinta del autor sobre el commit correctivo.
