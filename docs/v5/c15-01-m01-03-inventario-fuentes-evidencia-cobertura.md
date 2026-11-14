# SOLQARYN V5 FINAL — C15-01.M01.03: inventario de código y evidencia de cobertura

**Fecha:** 2026-11-14. **Responsable:** PRIMARY DEV 05 JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Ámbito:** sólo `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. **Clase:** preparación documental M01; NO auditoría exhaustiva ni implementación.

**Autoridad:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), Skill canónica `.agents/skills/solqaryn-project-governance/SKILL.md`, `references/architecture.md`, AGENTS y Scope Lock. La microtarea es PLAN fila 2164; M01.01/M01.02 de DEV04 son documentos antecedentes, no auditoría certificada.

## 1. Método y limitación de la muestra

Se leyeron **18 archivos reales con sus blobs** del commit baseline `83089fdbdc9aea9198c0ebec2cba197907354ba7`, cuyo check Scope Lock concluyó SUCCESS. Se inspeccionaron de forma dirigida los contratos .NET/Angular, guards, composición de backend, scripts de gobernanza y CI; no se ejecutaron las suites de esos componentes como parte de esta lectura. Las declaraciones de versiones son `Angular 22.2.1`, `net10.0`, `EF Core 10.0.12`, `MySql.EntityFrameworkCore 10.0.9`. La existencia de `AddAuthentication`, `AddAuthorization` y guards Angular sólo acredita que hay rutas y controles declarados; NO certifica denegación tenant A/B de una petición real.

Este inventario NO reconcilia exhaustivamente todos los requisitos del documento V5, los 2340 IDs y la historia completa de BITACORA. Tampoco certifica cero requisitos huérfanos, duplicados críticos, ciclos, P0/P1 o brechas de seguridad. Los recuentos de PLAN/COLA/DAG son instantáneas cambiantes y sólo sus lecturas actuales autorizan claims.

## 2. Contratos y blobs leídos (baseline Git)

| Ref | Ruta | Blob SHA-1 | Evidencia comprobada | Límite |
| --- | --- | --- | --- | --- |
| F01 | `AGENTS.md` | `063d342891ec6e0b23b9adb195ef3b358b51548d` | allowlist y autoridad | no acredita corrida física |
| F02 | `docs/PROJECT_SCOPE_LOCK.md` | `7dabef6946d75e0381f121a70458240ac702a30f` | scope STRICT | no reemplaza check CI |
| F03 | `docs/AUTOMATION_STATUS.md` | `4b25c0e88e8fb593cbafb5f8836db42c7d703fca` | estados y telemetría | no mide actividad continua |
| F04 | `docs/AUTOMATION_AUTHORITY.md` | `83fd8cb8fdd611eab8972927c83e52cb3e3ccd05` | controles A0-A5 | no habilita QA/PROD |
| F05 | `scripts/verify-project-scope.mjs` | `5d51fe92566cfd0e51b7265b844af5432bac57c3` | control de alcance | existencia no es PASS |
| F06 | `scripts/verify-solqaryn-skills.mjs` | `c9d23a8fa677a460d878b6789e130a31cfc8e26f` | control de skill | no sustituye HEAD |
| F07 | `scripts/quality/verify-v5-changelog-provenance.mjs` | `00e0b1414695b433f9c48cf7ad1f099a0e4b010b` | encabezados/procedencia | no prueba runtime |
| F08 | `scripts/quality/direct-dev-traceability.mjs` | `9f5d12c8d49a76c3d84920a37dd71b955ab87bfa` | trazabilidad advisory | no cambia PLAN |
| F09 | `frontend/package.json` | `e6d030f643421cb343cc12617e742469e88223c4` | Angular 22.2.1 | no acredita build |
| F10 | `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | guards y rutas | no prueba HTTP |
| F11 | `frontend/src/app/core/guards/auth.guard.ts` | `b652a7af6343bdae83a4dc9f3065d9fc1aa120f6` | guard tenant | no concede RBAC backend |
| F12 | `frontend/src/app/core/guards/permiso.guard.ts` | `1052bd38041055cc81e84a14d5fe0364e169b1fe` | permiso UI | no aísla MySQL |
| F13 | `backend/src/API/Solqaryn.API.csproj` | `2a7b33a0974d1ebe13676593d84b458b1243e037` | ASP.NET net10.0 | no demuestra tests |
| F14 | `backend/src/API/Program.cs` | `01425cff56eb13e12022b1b81a2f322d17fd1fff` | DI/JWT | no valida sesión real |
| F15 | `backend/src/API/Controllers/AuthController.cs` | `af437c869b4800a499c42aeff00347de3a3cc521` | entrada autenticación | no prueba tenants A/B |
| F16 | `backend/src/API/Controllers/AuditoriaController.cs` | `82057f2acc571263ed7ed530eae0f81359b2f81a` | Authorize | no certifica auditoría completa |
| F17 | `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj` | `6210d8b12abd77171d95d17e2292c1a319b89263` | EF 10.0.12 MySQL 10.0.9 | no ejecuta migración |
| F18 | `.github/workflows/dev-ci.yml` | `3f6912458d17a2a049d7b6a804472a5e1cd1b746` | workflow CI dev | no equivale a éxito |

## 3. Uniones obligatorias para auditoría causal posterior

| Unión | Llave de conciliación | Prueba positiva futura | Ejemplo negativo |
| --- | --- | --- | --- |
| Documento V5 → PLAN | requisito e ID exacto | Requisito con resultado y microtarea identificados | ID huérfano se reporta cubierto |
| PLAN → COLA | ID exacto, PLAN H/I | Estado, dueño y COLA E/I/G/O consistentes | Dos owners o gate derivado usado como autorización |
| PLAN → DAG | ID + predecesores | DAG F VALIDADO, G SI y dependencias satisfechas | Gate SI pese a predecesor NO |
| PLAN → Git | ID + ruta + SOURCE_SHA | Diff y blobs de entrega vinculados | SHA diferente se presenta como el publicado |
| Git → checks | head_sha exacto + job pertinente | completed/success para controles causales | cancelled/skipped/queued/in_progress como PASS |
| Estado → BITACORA | EVENT_ID/IDEMPOTENCY_KEY/ID/TASK_ID | Evento nuevo A:Z, CAS, readback | Duplicado o cambio histórico |
| LISTO → A5 | autor/revisor TASK_ID distintos en Javier6 | Evidencia independiente y causal | Autorrevisión o Task retirada |
| Seguridad → runtime | JWT/RBAC, tenant A/B, recurso | Denegación negativa HTTP/DB en CI dirigido | Guard frontend como sustituto backend |

La unión se efectúa siempre por **ID completo**, nunca prefijos, posiciones o suposiciones de una fotografía anterior. Los documentos M01 son preparación y no habilitan migración, JWT/RBAC, worker, eventos, QA/main/PROD ni terminales M01.06/M06 sin gates propios.

## 4. Brechas pendientes, sin inventar defectos

- **G01** Sin extracción exhaustiva de todos los requisitos y versiones del Doc V5.
- **G02** Sin comparación integral de las 2340 claves y sus duplicados entre PLAN, COLA y DAG.
- **G03** Sin comprobación completa de ciclos y prerequisitos por cada arista del DAG.
- **G04** Sin mapping completo requerimiento → componente → prueba positiva/negativa → SHA.
- **G05** Sin certificación del universo de BITACORA A:Z contra todas las transiciones.
- **G06** Los guards Angular no prueban autorización backend JWT/RBAC ni aislamiento tenant A/B.
- **G07** La declaración EF/MySQL no acredita migración, rollback ni operaciones DB efímeras.
- **G08** El contenido de workflows no acredita que un job causal haya terminado en success.
- **G09** No se probaron Playwright, restore real, disponibilidad, accesibilidad ni carga en este M01.
- **G10** A5 históricos y ausencia de P0/P1 necesitan auditoría por autoría, SHA y prueba causal.

Todas estas brechas tienen estado **NO_ACREDITADO_EN_ESTA_LECTURA**, no `FALLO_VERIFICADO` ni evidencia de que producción esté insegura. Evitar secretos, credenciales, ejemplos con datos personales y cualquier conexión productiva.

## 5. Oráculos falsables futuros (NO_EJECUTADOS)

| Caso | Positivo | Negativo bloqueante |
| --- | --- | --- |
| P01/N01 | Cada requisito V5 tiene ID exacto y criterio único | Requisito no trazado pasa como cubierto |
| P02/N02 | Plan de 2340 IDs reconciliado sin colisiones | Duplicado/ausente ignorado |
| P03/N03 | DAG verifica predecesores y A5 antes de gate final | Fase terminal liberada por SI falso |
| P04/N04 | CLAIM y evento idempotente tienen mismo owner/readback | Dos Tasks compiten por un ID |
| P05/N05 | Check causal del SHA acaba completed/success | skipped/cancelled o SHA distinto contado PASS |
| P06/N06 | Backend prueba A permitido, B denegado | Contrato Markdown tratado como test API |

Los seis pares son **FUTUROS_NO_EJECUTADOS**; no se sustituyen por la inspección de archivos.

## 6. Impacto y controles de entrega

**Archivos del cambio:** documento presente y sección aditiva íntegra en `CHANGELOG_AI.md`. **Sin cambios** en Domain/Application/Infrastructure/API, frontend, DTO, base de datos, auth/JWT/RBAC, tenancy, Cloudinary, servicios, jobs, tests, workflows, infraestructura o datos. Consumidores: C15-01.M01.04/M01.05 y futura matriz de cobertura; no se modifica arquitectura canónica.

**A0:** contraste de fuente y 18 blobs, riesgo/rollback, headings y atribución. **A1:** se consignan pares P/N para ejecución futura; runtime NO_APLICA por diff Markdown. **A2:** ningún cambio funcional/PII; HTTP tenant A/B NO_APLICA (no PASS). **A3:** lectura documental y correspondencia con plan/Skill; CI backend/frontend/MySQL causal a estos Markdown NO_APLICA. **A4:** requiere commit fast-forward serializado, check Scope Lock y V5 exact-SHA completados success, diff y readback PLAN/COLA/BITACORA. **A5:** independiente aún PENDIENTE por otra Task Javier; no promover a QA/main/PROD.

**Rollback:** git revert normal del commit, revalidando HEAD y efectos concurrentes; jamás force-push, reset destructivo ni modificación de BITACORA histórica.
