# SOLQARYN V5 — C12-01.M01.01: alcance y no-regresión de topología objetivo

**Microtarea:** `C12-01.M01.01` (PLAN_MAESTRO fila 1730), M01 preparación de `C12-01 — Topología objetivo`. **Owner:** PRIMARY DEV 04 / JAVIER_CHATGPT / TASK `6aeacb7820ac8191b7d768a6f33dfa09`. **Autoridad:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit); [Sheet institucional](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). **Corte Git:** `solqaryn/Solqaryn@dev` SHA `5fbbc04649f8de9ad74146160a6b3bff893078c6`; **PROJECT_SCOPE_LOCK=STRICT**. Fecha 2026-11-14.

## Objetivo de la fase y decisión de no activar

Definir el **alcance documental futuro** de una topología segregada de frontend, API .NET, workers y MySQL, con redes privadas, secretos, volúmenes, backups, recuperación, diagnóstico y rollback. La autoridad comercial contempla a Hostinger/Cloudflare como **alternativas objetivo**; no confirma aprovisionamiento, presupuesto, proveedor conectado o migración ejecutada. Los servicios existentes documentados (Vercel, Render, Aiven y Cloudinary) se conservan sin cambio. No se inventan cuentas, hostname, IP, puertos externos, costos, RTO/RPO ni volúmenes actuales.

**Esta entrega únicamente fija objetivo, límites, referencias y criterios de no-regresión para M01.** No implementa arquitectura, despliegue, infraestructura, migraciones, DNS, TLS, Cloudflare R2, seguridad de red, cobros o contratación. La decisión arquitectónica real requiere su ADR, evidencia, autorización del propietario y microtareas posteriores elegibles; no se actualiza `ARCHITECTURE.md` como si la topología objetivo ya existiera.

## Alcance técnico planificado, nunca desplegado aquí

| Capa y frontera | Contrato objetivo futuro | Regla de no ejecución actual |
|---|---|---|
| Frontend Angular 22.2.1 | Entrega SPA/CDN con configuración de backend, rutas y headers reproducibles | No cambiar Vercel, dominios, DNS, envvars o branding |
| API ASP.NET Core 10 | Contenedor separado, usuario mínimo, health/readiness y límites de recursos | No tocar Render, API runtime o configuración actual |
| Workers/colas | Procesamiento idempotente aislado, techo de reintentos y entrega con trazabilidad | No activar dispatcher, webhook ni llamadas entre Tasks |
| MySQL EF Core 10 | Red privada, cuentas con privilegio mínimo, transacciones y backup/restore | No mover Aiven, ejecutar EF migration o leer secretos |
| Red, certificados y DNS | Arquitectura privada y plan de TLS, proxy confiable y rollback verificable | No crear VPS, cambiar Cloudflare, DNS, host ni certificados |
| Datos/archivos | Inventario real, retención, propiedad tenant, migración reversible futura | No copiar Cloudinary/R2, archivos, backups ni datos productivos |
| Seguridad/observabilidad | JWT/RBAC/tenant A/B, métricas, alertas y reporte por ambiente | No habilitar proveedores, exports, cuentas de pago ni logs externos |

## Nueve fuentes y blobs versionados, no pruebas de despliegue

| Ruta | Blob Git | Observación verificada y límite |
|---|---|---|
| `render.yaml` | `f64d291425e2bbfdd9b6466d268753f8f8d86b3d` | Declara despliegues Render por ambiente y referencias de variables/health; **no** acredita ejecución o proveedor futuro. |
| `frontend/vercel.json` | `5f12bcf667a21afcfc4314b66787ff66627ae666` | Declara rewrites API, routing web y configuración de frontend Vercel; no prueba cutover ni DNS. |
| `Dockerfile` | `5bcfd9722c5b88fb7b081a7f23c2b70eb7b56327` | Contiene imagen build/runtime .NET 10 y usuario de ejecución; no certifica VPS o contenedores en Hostinger. |
| `backend/src/API/Configuration/RenderEnvironmentContractGuard.cs` | `2c38ebb90a8360b61c9e05df96ff3a9abd5b8f65` | Valida contrato de claves de Render; su acoplamiento requiere evaluación antes de migración. |
| `backend/src/API/Configuration/EnvironmentDatabaseGuard.cs` | `896aa003876f2b4299dfe5d4f1d69b83b5d1be22` | Vincula entorno Render a endpoint MySQL Aiven; no reconfigurar ni copiar credenciales. |
| `PROJECT_CONTEXT.md` | `48064b93299e973c33145662897d772712d2fa29` | Declara infraestructura/stack vigentes y autorización V5; cambios futuros requieren autorización. |
| `PROJECT_INDEX.md` | `98c2d82de74ff56ba92474572d39d6afb5bcd89` | Referencia plan comercial actual y prohíbe ejecución de backlog archivado. |
| `docs/PROJECT_SCOPE_LOCK.md` | `7dabef6946d75e0381f121a70458240ac702a30f` | Limita dev y seguridad, prohíbe inferir promoción y escritura productiva. |
| `.agents/skills/solqaryn-project-governance/references/architecture.md` | `1931e1229752028229b6ad1bb3368038e2c6769b` | Define API/Domain/Application/Infrastructure, Angular, MySQL, JWT/RBAC y fronteras de arquitectura. |

La presencia de YAML, Dockerfile, esquemas o scripts versionados no demuestra infraestructura conectada, DNS en vigor, TLS aprobado, restauración funcional ni control de costos. Los registros históricos de otros proveedores o proyectos no son autoridad para cambiar el entorno actual.

## NR01–NR16 — invariantes de no regresión

| ID | Invariante obligatorio |
|---|---|
| NR01 | Todo el alcance actual es preparación documental en dev; ninguna configuración operativa cambia. |
| NR02 | Frontend Angular 22.2.1 conserva rutas/guards, lazy loading y accesibilidad. |
| NR03 | Backend ASP.NET Core 10 conserva API y capas Domain/Application/Infrastructure. |
| NR04 | MySQL mantiene propiedad tenant/sucursal y separación dev/qa/prod, sin copiar datos reales. |
| NR05 | JWT, BCrypt y RBAC se validan siempre en backend; ningún proxy sustituye autorización. |
| NR06 | Connection strings, tokens, secretos y claves privadas no pasan a docs, logs o Git. |
| NR07 | Render/Vercel/Aiven/Cloudinary presentes permanecen como autoridades operativas hasta decisión expresa. |
| NR08 | Ningún VPS, contenedor externo, DNS, TLS o bucket se provisiona ahora. |
| NR09 | No se altera main, qa, producción, planes pagados ni permisos de cuentas. |
| NR10 | Todo cambio futuro tiene plan de backup/restauración y rollback probado antes de cutover. |
| NR11 | Puertos MySQL/red privada nunca se publican por conveniencia de migración. |
| NR12 | Workers y outbox futuros no generan doble procesamiento ni loops de Tasks. |
| NR13 | Health/readiness evalúan servicios por ambiente sin exponer secretos o información multiempresa. |
| NR14 | Trazas, métricas y logs no exportan PII, JWT, SQL sensible ni identificadores de tenant. |
| NR15 | Activación requiere evidencias reproducibles de CI, integración, aislamiento A/B y seguridad. |
| NR16 | Cloudflare/Hostinger son topología objetivo futura, no infraestructura certificada ni habilitada. |

## Pruebas de aceptación futuras, no ejecutadas

| Par | Positivo futuro | Negativo futuro |
|---|---|---|
| P01/N01 | Inventario del entorno DEV con artefactos y secretos sólo por referencia |
| P02/N02 | Contenedores efímeros sin privilegio y red privada en CI |
| P03/N03 | Backend responde health/readiness sin filtrar credenciales |
| P04/N04 | Frontend conserva API proxy y cabeceras bajo entorno de prueba |
| P05/N05 | Restauración verificada de snapshot sintético con checksum |
| P06/N06 | Rollback de una versión en ensayo con tráfico sintético |
| P07/N07 | Medición de latencia/errores y presupuesto de recursos en prueba |
| P08/N08 | Cotas de workers, reintentos, cuota y auditoría en CI |

Todos estos casos son `FUTUROS_NO_EJECUTADOS`: ningún test runtime nuevo ni simulación de VPS/Cloudflare/Hostinger se ejecutó para esta entrega documental. No atribuir PASS de backend/Angular/MySQL sólo porque un documento describe verificaciones futuras.

## G01–G10 — puertas independientes de cambio

| Gate | Componente | Prueba/evidencia futura requerida |
|---|---|---|
| G01 | Topología detallada y diagrama por entorno | ADR M01/M02 revisado; no deploy |
| G02 | Gobierno de red privada y permisos mínimos | Matriz puertos/ACL/tenants verificable |
| G03 | Separación de secretos | Referencias seguras; ningún valor en Git |
| G04 | Datos y persistencia | Backup/restauración sintética y plan MySQL reversible |
| G05 | Aplicación web/API | Compatibilidad Angular y .NET 10, health/routing definidos |
| G06 | Protección tenant/RBAC/JWT | Positivos/negativos A/B en CI antes de cutover |
| G07 | Observabilidad y costos | SLI/alertas/retención/cuotas sin PII verificadas |
| G08 | Plan de rollback | RTO/RPO y orden de reversión con dependencias |
| G09 | Autorización externa | Orden explícita separada del propietario para proveedores, DNS y PROD |
| G10 | Control exact SHA | CI completed/success causal, sin skipped/queued como PASS |

Los gates son **condiciones de diseño para futuro**, no sustituyen al `DAG_TECNICO` dinámico ni conceden autorización para activar entornos. Ni `M01.01 LISTO_DEV` ni A5 independiente habilitan por sí mismos M01.06/M06.06 o una migración real.

## Impacto inmediato, auditoría y reversión

**Impacto inmediato:** únicamente `docs/v5/` y una sección autocontenida de `CHANGELOG_AI.md`; sin cambios ejecutables, APIs, DTO, datos, dependencias npm/NuGet, configuración, servicios, usuarios, secretos o permisos. Consumers futuros incluyen clientes Angular, backend, operadores y responsables de seguridad; su acceso sigue sujeto a autorización.

**A0:** Skill instalada y canónica coinciden; identidad, HEAD, PLAN fila1730 libre, COLA G/O=SI, DAG F/G VALIDADO/SI; exclusión de otro owner. **A1:** objetivo, límites, 16 NR y ocho pares de casos futuros. **A2:** aislamiento por ambiente/tenant, JWT/RBAC, secretos y redes privadas documentados sin tocar controles. **A3:** nueve blobs y compatibilidad conceptual trazada, no smoke HTTP/DB real. **A4:** publicación fast-forward/CAS sobre HEAD exacto; checks Scope Lock y procedencia CHANGELOG `completed/success` del SHA publicado; BITACORA/PLAN/COLA readback. Suites runtime sobre Markdown = NO_APLICA motivado, nunca PASS inventado. **A5:** pendiente de Task Javier distinta; no promueve QA/main/PROD.

**Rollback:** `git revert <SHA_publicado>` como nuevo commit normal sobre dev; preservar autoría, BITACORA histórica y trabajo ajeno. Nunca force-push.
