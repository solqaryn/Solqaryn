# SOLQARYN V17 — C12-01.M01.06: bloqueos, reversión y condición de liberación del lote M01

**Microtarea:** `C12-01.M01.06`, PLAN_MAESTRO fila 1735. **Responsable técnico:** PRIMARY DEV01 Javier, `6adbb9fff1548191a1bd5b30d9e9984a`. **Riesgo R0:** únicamente contrato documental; **PROJECT_SCOPE_LOCK=STRICT**, repositorio `solqaryn/Solqaryn@dev`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL y PLAN_MAESTRO/COLA/DAG_TECNICO del Sheet institucional.

## A0. Alcance real, no implantación

Cerrar **documentalmente M01 de C12-01** mediante una matriz de bloqueos, condiciones objetivas para una posible M02 y reversión. El resultado exigido es una preparación verificable para estudiar una topología futura: frontend Angular 22.2.1, API ASP.NET Core 10, workers aislados, MySQL 8.4, red privada, TLS, secretos, volúmenes, backups y observabilidad. **No existe decisión de migrar, adquirir, aprovisionar ni cambiar el entorno**. La topología vigente declarada continúa siendo Vercel, Render, Aiven y Cloudinary. Hostinger/Cloudflare son alternativas futuras, no proveedores verificados o aprobados. No declarar cuenta, tarifa, IP, hostname, RTO/RPO, DNS, capacidad ni secreto inexistente.

Este cambio no edita `ARCHITECTURE.md`, `render.yaml`, `frontend/vercel.json`, código, workflows ni parámetros del proveedor: no cambia ninguna frontera viva y no concede permisos para QA, main, PROD, migraciones, VPS, dominios o secretos. M01.06 puede permitir que la **fórmula DAG reevalúe un siguiente contrato documental** sólo con LISTO_DEV, A5 independiente y todas sus dependencias verdaderas; no permite ejecutar un cutover ni activar runtime.

## Evidencias versionadas antecedentes, sin confundir documentos con pruebas

| Entrada | Documento original | Blob Git del documento revisado |
| --- | --- | --- |
| M01.01 | `docs/v5/c12-01-m01-01-alcance-topologia-objetivo-sin-activacion.md` | `808cb7d980c4c258a640cf5f78245b3dac78e421` |
| M01.02 | `docs/v5/c12-01-m01-02-actores-superficies-topologia-objetivo.md` | `63a2698009d5d62499b4c81e160102a1db319310` |
| M01.03 | `docs/v5/c12-01-m01-03-inventario-evidencia-topologia-objetivo.md` | `d0e0bc12185d60f63a1e91f61bcd9a2c2db22884` |
| M01.04 | `docs/v5/c12-01-m01-04-riesgos-supuestos-decisiones-topologia-objetivo.md` | `d2c754bceec44d8592681392f694f6020d119df5` |
| M01.05 | `docs/v5/c12-01-m01-05-criterios-aceptacion-topologia-objetivo.md` | `f77181886f5071855b1d5bbb8ac435d9d1a00fa4` |

M01.01 fija NR01–NR16; M01.02 identifica 14 actores, doce pares de superficies y catorce fronteras; M01.03 diferencia inventario de catorce rutas y doce brechas; M01.04 deja abiertos R01–R16, S01–S10 y D01–D10; M01.05 define P01–P16/N01–N16 y NF01–NF10 **FUTUROS_NO_EJECUTADOS**. Los blobs solo fijan procedencia del repositorio, no despliegue ni ejecución de esas pruebas. Si una fuente cambia, actualizar su evidencia y volver a revisar antes de usar este contrato.

## G01–G16: bloqueos y condiciones causales de liberación FUTURA

| Gate | Bloqueo vigente/no acreditado | Evidencia mínima para una futura decisión separada |
| --- | --- | --- |
| G01 | Proveedor/plan/costo/permiso no acreditados | Orden explícita del propietario, identidad de cuenta y presupuesto aprobado |
| G02 | ADR de topología y zonas de confianza no aprobado | ADR versionado con diagrama, responsables y alternativa de retorno |
| G03 | Red privada API/worker/MySQL no probada | Rechazo real de acceso externo no autorizado en ambiente aislado |
| G04 | Puertos, contenedor y usuario no-root sin prueba objetivo | Build exact-SHA, SBOM, escaneo y test de exposición permitida |
| G05 | Guard `RenderEnvironmentContractGuard` acoplado a 28 claves Render | Diseño compatible y pruebas negativas sin suprimir guardas |
| G06 | Guard `EnvironmentDatabaseGuard` acoplado a Aiven/SSL | Contrato alterno seguro con rechazo host/usuario/BD/SSL inválidos |
| G07 | Secretos, rotación e IAM destino sin fuente ni autorización | Custodia externa, usuarios mínimos, control por ambiente y pruebas de revocación |
| G08 | Integridad EF/MySQL y migraciones no demostradas en destino | MySQL 8.4 efímero, migración versionada, datos sintéticos y 0 errores |
| G09 | Backup/restauración objetivo sin prueba reproducible | Restore aislado con hashes, conteos, transacciones y documentación de retorno |
| G10 | JWT/RBAC/tenant A/B y pertenencia tras proxy no comprobados | Tests positivos/negativos API y aislamiento, sin confiar en headers del cliente |
| G11 | Routing Angular, API proxy, CORS y SEO no comparados | Playwright/Chromium y pruebas HTTP de rutas, errores y cabeceras |
| G12 | DNS/TLS/certificados y cambio de tráfico no autorizados | Validación con nombres reales autorizados y rollback de DNS ensayado |
| G13 | Worker/outbox idempotente y cotas no construidos | CI de duplicación, reintento acotado, dead-letter y conciliación |
| G14 | Medios Cloudinary/R2 sin inventario/hashes/ACL comprobados | Inventario real autorizado, ownership A/B y prueba de no pérdida |
| G15 | SLI/SLO, cuota, latencia, costo y retención sin medidas | Fuentes, muestra, p95, RTO/RPO y umbrales aprobados, o NO_MEDIDO |
| G16 | Reversibilidad integral y separación DEV/QA/PROD no certificadas | Ensayo de abortar/restaurar, IAM por ambiente y aprobación de ventana |

**Estado de G01–G16 en esta entrega:** pendientes para toda acción de despliegue. No se les asigna PASS por existir este documento. Los riesgos abiertos R01–R16 de M01.04 siguen abiertos. Una eventual habilitación de M02 en DAG es **preparación documental**, nunca una declaración de que G01–G16 han aprobado ni un permiso para producción.

## R01–R08: runbook de reversión propuesto, NO EJECUTADO

1. **R01 — Entrada:** registrar SHA, propietario, contrato aprobado, ventana, responsables y métricas de línea base; si falta algo, abortar antes de alterar infraestructura.
2. **R02 — Aislamiento:** fijar inventario de secretos **solo por referencias opacas**, dominios, puertos, usuarios DB y rutas existentes; no copiarlos a Git/Sheets/logs.
3. **R03 — Conservación:** preparar respaldo coherente de artefactos, DB y medios existentes en un entorno autorizado con checksum y registro de propiedad tenant.
4. **R04 — Dry run:** ejercitar despliegue y restauración **sintéticos y aislados**, con verificación de servicios no-root, red privada, esquema y datos.
5. **R05 — Umbral abortar:** definir por aprobación futura latencia, errores, inconsistencia, fuga PII, fallos JWT/RBAC/tenant y punto de no retorno; si excede, detener sin seguir enviando tráfico.
6. **R06 — Reversión de rutas:** volver a rutas/headers/certificados anteriores de forma verificada y con monitoreo; no modificar DNS hoy.
7. **R07 — Integridad:** conciliar mensajes pendientes, backups, transacciones y referencias de archivos sin borrar registros legítimos, consentimientos, opt-outs o BITACORA.
8. **R08 — Cierre:** recopilar SHA, evidencia de smoke/negativos, valores medidos, responsables y readback; dejar NO_GO si falta cualquiera de los controles exigidos.

El único rollback ejecutable de **este cambio documental** es un `git revert` aditivo y autorizado del commit de M01.06, tras verificar el HEAD real; nunca force-push, borrar BITACORA ni manipular infraestructura real.

## T01–T12: pares POS/NEG para aceptación futura (0/12 ejecutados)

| Caso | Positivo esperado en ensayo autorizado | Negativo que debe bloquear |
| --- | --- | --- |
| T01 | Inventario exact-SHA de fuentes y permisos revisado | Proveedor/credencial/costo asumidos sin readback |
| T02 | Imagen .NET no-root con SBOM y puertos controlados | Imagen root o puerto MySQL público |
| T03 | API healthy con DB ready en red privada | Health devuelve READY con DB inaccesible |
| T04 | Guard de ambiente permite claves/hosts válidos | Guard deshabilitado o SSL ausente |
| T05 | Restore sintético compara hashes y relaciones | Backup no restaurado o conteos divergentes |
| T06 | Tenant A lee exclusivamente A con JWT vigente | Slug/header A forja pertenencia a B |
| T07 | Rol vigente accede a la función permitida | Rol revocado o token caducado concede acceso |
| T08 | Proxy conserva SPA, SEO, CORS y HTTP status | Bucle rewrite o 403 ocultado como 200 |
| T09 | TLS y DNS coinciden con plan aprobado | Certificado/host no permitido o rollback imposible |
| T10 | Worker idempotente entrega una única intención | Replay/timeout origina efecto duplicado |
| T11 | Medios inventariados conservan hashes y ACL | Objeto inexistente o medio B migrado a A |
| T12 | SLO medidos y rollback ejercitado con salida segura | Alerta no recibida, PII filtrada o objetivo NO_MEDIDO tratado como PASS |

**Ejecución real en esta microtarea:** 0/12; ninguna prueba de red, contenedor, MySQL, servidor, JWT HTTP, Angular/Playwright, DNS/TLS, proveedor, backup/restore ni worker se ejecuta por este Markdown. Son obligaciones futuras R2/R3 con autorización y pruebas específicas.

## Auditorías proporcionales, decisión y handoff

**A0:** el changeset M01.06 se limita a este archivo y una sección completa de `CHANGELOG_AI.md`, no cambia arquitectura viva ni licencias; comprobar PLAN/COLA/DAG y skill canónica. **A1:** revisión estática de dieciséis gates, ocho pasos de rollback y doce pares POS/NEG futuros, indicando que no han ejecutado. **A2:** proteger IAM, secretos, tenant A/B, JWT/RBAC, datos, red privada y fail-closed como diseño, no certificación de runtime. **A3:** .NET, MySQL/EF, browser, proveedores y DNS **NO_APLICA por cambio exclusivamente Markdown**, sin atribuir PASS a builds de otros scopes. **A4:** requerir SHA publicado por fast-forward/CAS, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` en `completed/success` del SHA exacto, más PLAN/COLA/BITACORA readback. Si CI externa sigue `queued/in_progress`, mantener VALIDANDO y owner, sin LISTO. **A5:** revisor de otra Task Javier distinta debe inspeccionar la evidencia y puede rechazar.

**Condición de liberación de C12-01.M01 hacia M02:** A0–A4 de M01.06 completos y A5_PASS independiente pertinente, predecesores M01.01–05 y gates DAG/COLA **recalculados en tiempo real**. Si no se acredita una condición, M02 sigue NO_GO. Incluso con M02 documental habilitada, cualquier cambio de proveedor, DNS, VPS, migración, QA/main/PROD y gasto sigue NO_GO sin otra orden específica y los ensayos exigibles.
