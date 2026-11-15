# SOLQARYN V5 FINAL — C15-02.M01.03: inventario contrastable de fuentes y brechas de arquitectura/migración

**Fecha local:** 2026-11-14. **Autor:** PRIMARY DEV 01 JAVIER (TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`). **Repositorio:** `solqaryn/Solqaryn@dev`; **Scope Lock:** STRICT. **Clase:** análisis documental de código y evidencias disponibles, **NO** auditoría de infraestructura ni migración ejecutada.

**Autoridad única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`) y PLAN_MAESTRO/COLA/DAG_TECNICO del Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. Baseline de lectura Git `b232ab885ad49ced262e81f0aeeb0f8d33c632e6`, sujeto a readback del commit publicado. Preflight: PLAN fila 2200 PENDIENTE/sin owner, COLA G/O=SI, DAG fila 2161 F=VALIDADO/G=SI. La habilitación corresponde a **preparación M01**, nunca a cutover, migraciones, cuentas de pago ni nuevas Tasks.

## Alcance de evidencia

Se contrastó el inventario de paths y blobs expuestos por GitHub para definir qué afirmaciones se sostienen en contenido versionado. Un archivo presente acredita **existencia de fuente estática**, no un comportamiento seguro en ejecución, ni despliegue, ni respuesta HTTP, ni restauración de MySQL o disponibilidad externa. El conjunto de fuente se conserva por path y SHA de blob para su auditoría posterior; SHA de blob y SHA de commit son conceptos distintos.

## Inventario reproducible de 21 fuentes del baseline Git

| ID | Tipo | Ruta Git exacta | Blob SHA | Evidencia de lectura estática |
| --- | --- | --- | --- | --- |
| S01 | Gobierno canónico | `AGENTS.md` | `063d342891ec6e0b23b9adb195ef3b358b51548d` | Allowlist seis Tasks Javier y dev |
| S02 | Skill canónica | `.agents/skills/solqaryn-project-governance/SKILL.md` | `304e400d5c0f89fcfdcc65caece86c34276734b5` | Scope Lock STRICT y fail-closed |
| S03 | Arquitectura skill | `.agents/skills/solqaryn-project-governance/references/architecture.md` | `1931e1229752028229b6ad1bb3368038e2c6769b` | Flujos y fronteras declarados |
| S04 | Límite de proyecto | `docs/PROJECT_SCOPE_LOCK.md` | `7dabef6946d75e0381f121a70458240ac702a30f` | Entornos autorizados |
| S05 | Runtime automatizaciones | `docs/AUTOMATION_STATUS.md` | `4b25c0e88e8fb593cbafb5f8836db42c7d703fca` | Seis identidades vigentes |
| S06 | Autoridad operativa | `docs/AUTOMATION_AUTHORITY.md` | `83fd8cb8fdd611eab8972927c83e52cb3e3ccd05` | Conservación de identidad y permisos |
| S07 | Contratos de alcance M01.01 | `docs/v5/c15-02-m01-01-alcance-no-regresion-auditoria-arquitectura-migracion.md` | `4339701f87da9cbd0f20746742ec85374bc5d769` | 16 invariantes / 8 pares futuros |
| S08 | Interfaces M01.02 | `docs/v5/c15-02-m01-02-actores-interfaces-fronteras-auditoria-migracion.md` | `a39e7a4d5b9a0bd8ebab6cf19ad592c7b2191aa5` | 12 participantes, 12 contratos, 10 fronteras |
| S09 | Dependencias frontend | `frontend/package.json` | `e6d030f643421cb343cc12617e742469e88223c4` | Angular 22.2.1, zone.js 0.16.3 |
| S10 | Proyecto API | `backend/src/API/Solqaryn.API.csproj` | `2a7b33a0974d1ebe13676593d84b458b1243e037` | TargetFramework net10.0 |
| S11 | Proyecto Infrastructure | `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj` | `6210d8b12abd77171d95d17e2292c1a319b89263` | EF10.0.12, MySql.EntityFrameworkCore 10.0.9, Cloudinary |
| S12 | Composición HTTP | `backend/src/API/Program.cs` | `01425cff56eb13e12022b1b81a2f322d17fd1fff` | Middleware/DI/auth; lectura estática |
| S13 | Tenancy en persistencia | `backend/src/Infrastructure/Persistence/AppDbContext.cs` | `c4b32f6637f1b4426655bb0cd12af8dd9d229866` | Lectura de contexto confiable en EF |
| S14 | Reintentos y efectos | `backend/src/Application/Services/OutboxRetryProcessor.cs` | `542c0a655a3ee0698e3d80f1854d43903b6470fa` | Procesamiento declarado, no nuevo dispatcher |
| S15 | Resolución host/tenant | `backend/src/Infrastructure/Services/ConfiguredPublicStoreHostTenantResolver.cs` | `833940d3ce2f246d5946daeeb2ed7e14ffaffae2` | Host binding requiere verificación de comportamiento |
| S16 | Almacenamiento actual | `backend/src/Infrastructure/Services/CloudinaryImageStorageService.cs` | `2413e0b9a05859f7978a6c1708b05fbc2648d5bb` | Adaptador Cloudinary, sin inventario remoto |
| S17 | JWT actual | `backend/src/Infrastructure/Services/JwtService.cs` | `9051fe6b4aac23e0ce3ebc684a2830c2cd2e6c9b` | Servicio JWT; no afirma prueba A/B |
| S18 | Backup/restore DEV | `docs/BACKUP_RESTORE_DESARROLLO_RUNBOOK.md` | `81e8b11557912fffcd741ff0f34faf69e0ed6f47` | Procedimiento no es restore ejecutado |
| S19 | CI general | `.github/workflows/ci.yml` | `c02c3f00c3dccb91e79bfd2b5befe2fb5b517263` | Definición de CI, no ejecución por archivo |
| S20 | CI DEV | `.github/workflows/dev-ci.yml` | `3f6912458d17a2a049d7b6a804472a5e1cd1b746` | Pipeline DEV |
| S21 | Frontend routing | `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | Rutas Angular, no paridad externa |

**Jerarquía de certeza:** se observaron estos paths y blobs en `dev`; el texto de los contratos y proyectos seleccionados fue contrastado, otros paths fueron inventariados por metadatos de Git (sin atribuir análisis de todos sus métodos). `S01–S08` son gobierno y documentos de alcance; `S09–S17` código y dependencias; `S18–S21` runbook, workflows y routing. Los 21 hashes NO representan 21 pruebas runtime. Los checks GitHub del baseline (Scope Lock, CHANGELOG, frontend, backend, seguridad) llegaron a completed/success sobre el commit leído; esa evidencia sólo describe esa ejecución y no certifica Hostinger, Cloudflare, R2, restore o DNS.

## Brechas de evidencia: 12 aspectos NO_ACREDITADOS

| ID | Asunto | Límite real de lectura | Evidencia a obtener sólo con autorización |
| --- | --- | --- | --- |
| G01 | Topología real desplegada Vercel/Render/Aiven | No se inspeccionaron paneles ni configuración de producción; descripción de repositorio solamente | Evidencia de despliegues autorizada, URL/versión y entorno |
| G02 | Hostinger y R2 | Arquitectura objetivo; no se verificaron recursos, cuentas, buckets ni IAM | Inventario autorizado con captura auditada y no secretos |
| G03 | Dominio/DNS/TLS | No se ejecutaron consultas DNS ni comprobación certificada de certificados | Binding por host con negativas A/B y rollback aprobado |
| G04 | MySQL 8.4 operativo | La versión propuesta no demuestra DB real; no se consultaron conexiones ni esquemas remotos | MySQL efímero, versión, migraciones reversibles |
| G05 | Restore integral | Un runbook no acredita un backup restaurable | Restore descartable, checksums y conteos |
| G06 | Archivos históricos | Cloudinary declarado, inventario de objetos desconocido, no cero | Inventario completo por tenant, owner y checksums |
| G07 | Autenticación y autorización A/B | La existencia de middleware/JWT no prueba que nunca haya IDOR/BOLA | Tests HTTP y MySQL de A/B, roles y tokens manipulados |
| G08 | Jobs y replay | Código Outbox estático, no ejecuciones ni garantías de proveedor nuevo | Pruebas de idempotencia, deduplicación y caída aislada |
| G09 | CI causal para migración | Checks SUCCESS del HEAD basal no equivalen a ensayo de migración | Jobs, logs y artefactos exact-SHA de ensayo autorizado |
| G10 | Paridad frontend y accesibilidad | Angular package/rutas no acreditan navegadores, sesiones ni métricas | Playwright y presupuesto bundle, WCAG con entorno |
| G11 | Seguridad de secretos y telemetría | No se inspeccionaron valores de credenciales/logs remotos | Scanner y muestras redactadas sin exponer valores |
| G12 | Rollback y permiso de promoción | Dev y A5 no habilitan QA/main/PROD, DNS o transferencia de archivos | Acta independiente y plan de reversión probado |

**Semántica:** `NO_ACREDITADO` no significa `FALLO_VERIFICADO`; `INVENTARIO_DESCONOCIDO` no equivale a `CERO_ARCHIVOS`; `OBJETIVO_V5` no equivale a `DESPLEGADO`. No se ejecutaron inspecciones de cuentas Cloudflare/Hostinger, DNS real, consultas de MySQL vivo, peticiones HTTP, Playwright, Docker/restore, lectura de secretos ni migración de objetos.

## Ocho contratos positivo/negativo para M01.04–M01.06 y futuras fases (NO EJECUTADOS)

| Caso | Oráculo positivo futuro | Negativo bloqueante |
| --- | --- | --- |
| INV01 | Fuente, SHA y ambiente se cotejan por lectura posterior | Archivo de otra revisión se atribuye al SHA actual |
| INV02 | Diagrama separa Vercel/Render/Aiven declarados del objetivo Hostinger/Cloudflare/R2 | Infraestructura futura se presenta como activa |
| INV03 | JWT/RBAC y pertenencia tenant A/B se prueban en API/BD | Header, slug o EmpresaId de B confiere permisos a A |
| INV04 | Restore efímero concuerda en checksum, conteo y logs redactados | Existencia de runbook se reporta restore PASS |
| INV05 | Inventario real permite cero sólo si inspección completa lo acredita | Desconocido se sustituye por cero y se omite copia |
| INV06 | Retry/worker respeta tenant e idempotencia verificados | Replay o caída duplica efecto o expone datos de B |
| INV07 | Frontend, API, host y TLS preservan sesión/tenant | Host spoofing selecciona recurso de otra empresa |
| INV08 | Commit, diff, CI causal y A5 independiente corroborados | CANCELLED/SKIPPED/QUEUED o autor propio se usan como PASS |

## Impacto y criterios A0–A5

- **Componente:** documentación de preparación de auditoría de arquitectura/migración; único artefacto nuevo `docs/v5/c15-02-m01-03-inventario-fuentes-evidencia-migracion.md` y encabezado autocontenido en `CHANGELOG_AI.md`.
- **Capas y consumidores:** sin cambios en Angular, API/DTO, Domain/Application/Infrastructure, MySQL/EF, migraciones, Cloudinary, SMTP, QuestPDF, DNS, TLS, R2, workers o CI. Consumidores M01.04 riesgos, M01.05 aceptación y M01.06 gate terminal; ninguna etapa se libera por este inventario.
- **Seguridad:** conservar JWT/RBAC, tenant A/B, configuración de proveedor, secretos/PII fuera de Markdown y CORS/rate limiting; las brechas no se convierten en vulnerabilidades sin prueba causal.
- **A0:** inventario estructurado de 21 fuentes con paths y blobs verificables, 12 brechas clasificadas sin atribuir falsos PASS.
- **A1:** criterios INV01–INV08 positivos/negativos definidos como **FUTUROS_NO_EJECUTADOS**, sin atribuir pruebas de aplicación.
- **A2:** no se modifican controles de seguridad ni operaciones tenant; revisión documental de fronteras, sin prueba de explotación.
- **A3:** fuente/contrato y ruta de evidencia cotejables; build/HTTP/SQL/Playwright runtime `NO_APLICA` para diff sólo Markdown.
- **A4:** publicación mediante CAS fast-forward sobre dev, diff limitado a dos Markdown, checks `verify-solqaryn-project-scope` y V5 provenance `completed/success` del SHA publicado y readback PLAN/COLA/BITACORA antes de LISTO_DEV.
- **A5:** pendiente de otra Task Javier de TASK_ID distinto; no QA/main/PROD.
- **Rollback:** `git revert` normal, tras HEAD nuevo y análisis de dependencias; BITACORA histórica no se reescribe.

**No implementa eventos, workers nuevos, webhook, acceso a proveedores de pago, cambios de configuración ni promueve otros entornos.** M01.04 debe analizar riesgos condicionados a las doce brechas y M01.05 definir aceptación observable, sin inventar resultados.
