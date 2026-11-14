# SOLQARYN V5 — C12-01.M01.05: criterios observables de aceptación para topología objetivo

**Unidad:** `C12-01.M01.05` (PLAN_MAESTRO fila 1734), C12-01 / M01 preparación. **Autor:** PRIMARY DEV 06, Javier, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Autoridad única:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN/COLA/DAG](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). **Baseline de fuentes:** `dev` `0ac86bb40c032a69449468eb1c7d2c813c3fc6da`, `PROJECT_SCOPE_LOCK=STRICT`.

## Propósito y prohibiciones

Preparar condiciones de aceptación funcional, técnica y de no-regresión de una posible topología separada Angular 22.2.1 / ASP.NET Core 10 / workers / MySQL 8.4 con red privada, DNS/TLS, secretos, backups, volúmenes y rollback. Los criterios son **FUTUROS_NO_EJECUTADOS**: **no son suites realizadas ni certificación de proveedor**. Las fuentes Git inspeccionadas describen infraestructura actual de Vercel/Render/Aiven/Cloudinary; Hostinger/Cloudflare son objetivos sujetos a orden ulterior. No se crean VPS, servicios, R2, wallets, proyectos, tareas, proveedores, migraciones, nuevos endpoints, eventos, workers, DNS, cuentas ni compras.

Contrato previo del bloque: `C12-01.M01.01` alcance/no regresión (blob `808cb7d980c4c258a640cf5f78245b3dac78e421`), `C12-01.M01.02` actores/fronteras (blob `63a2698009d5d62499b4c81e160102a1db319310`), `C12-01.M01.03` 14 fuentes/12 brechas (blob `d0e0bc12185d60f63a1e91f61bcd9a2c2db22884`), `C12-01.M01.04` 16 riesgos/10 supuestos/10 decisiones (blob `d2c754bceec44d8592681392f694f6020d119df5`). Ningún documento habilita M01.06 ni M03 por sí solo.

## 16 escenarios positivos y 16 negativos, todos pendientes de ejecución

La fila define el comportamiento y su rechazo simétrico. La futura A1/A2/A3 requiere evidencia en GitHub Actions y/o ambiente efímero apropiado sobre un SHA exacto, no la mera presencia de la descripción.

| Caso positivo | Evidencia objetiva esperada | Caso negativo | Condición que debe fallar o abortar |
| --- | --- | --- | --- |
| P01 | La imagen .NET 10 objetivo se construye desde SHA fijado y registra digest/SBOM. | N01 | Rechazar imagen sin digest, con vulnerabilidad crítica no aceptada o build irreproducible. |
| P02 | El contenedor arranca con usuario no-root y escucha sólo puerto/host aprobados. | N02 | Rechazar contenedor root o puerto externo inesperado. |
| P03 | Health/readiness diferencia proceso saludable de dependencia MySQL caída. | N03 | No reportar READY si no hay conectividad o autorización DB. |
| P04 | API y MySQL objetivo se comunican sólo por red privada autorizada. | N04 | Intentos desde internet u otra red/tenant son rechazados. |
| P05 | La conexión MySQL exige TLS y credenciales del ambiente correcto. | N05 | Negar SSL desactivado, usuario/base incorrectos y binding host no autorizado. |
| P06 | Migraciones versionadas requieren paso y aprobación específicos por entorno. | N06 | Prohibir auto-migración accidental de QA/PROD al copiar DEV. |
| P07 | Datos de empresa A y B mantienen aislamiento backend y repositorios. | N07 | Cliente con EmpresaId/slug/header falso no accede a B. |
| P08 | JWT, RBAC y permisos por sucursal continúan iguales tras proxy/cutover. | N08 | JWT caducado, firma falsa o permiso ajeno siempre se deniegan. |
| P09 | SPA, SEO y backend-proxy conservan semántica, headers y códigos HTTP. | N09 | Rechazar loops de rewrites, bypass de CORS y rutas que oculten 401/403. |
| P10 | DNS y TLS resuelven el destino correcto con rollback controlado. | N10 | Host/certificado distinto o HSTS/redirect incoherente aborta cutover. |
| P11 | Secretos permanecen externos, con rotación y permisos mínimos. | N11 | Fallar si token/credencial aparece en Git, imagen o log. |
| P12 | Restore aislado valida checksums, conteos y transacciones de MySQL. | N12 | No aceptar backups sin restaurar o con objetos/tenants incompletos. |
| P13 | Rollback recupera rutas, DB y proveedor dentro de umbral aprobado. | N13 | Abortar si no hay reversión verificable o se supera punto de no retorno. |
| P14 | Worker futuro idempotente acota reintentos y registra deduplicación. | N14 | Duplicado/reentrega no debe ejecutar una segunda acción irreversible. |
| P15 | Migración futura de medios sólo cubre objetos reales inventariados. | N15 | Nunca migrar imágenes inexistentes ni perder referencia/ACL tenant. |
| P16 | Métricas, trazas y alertas tienen propietarios y retención aprobada. | N16 | No filtrar PII/secreto ni considerar PASS una alerta no entregada. |

## 10 requisitos no funcionales y regla de medición

| NFR | Evidencia exigible al implementar | Estado hoy |
| --- | --- | --- |
| NF01 | Latencia p50/p95 por ruta, observada en ambiente de prueba y comparada con umbrales aprobados. | Pendiente de objetivo cuantitativo/medición y responsable explícito |
| NF02 | Tiempo de recuperación y pérdida admisible RTO/RPO medidos, no inventados. | Pendiente de objetivo cuantitativo/medición y responsable explícito |
| NF03 | Disponibilidad del servicio por período y ambiente con fuente auditable. | Pendiente de objetivo cuantitativo/medición y responsable explícito |
| NF04 | Memoria/CPU/almacenamiento bajo carga tenant A/B y cuotas conocidas. | Pendiente de objetivo cuantitativo/medición y responsable explícito |
| NF05 | Minimización de superficies públicas: MySQL, secretos y puertos no expuestos. | Pendiente de objetivo cuantitativo/medición y responsable explícito |
| NF06 | Segregación IAM, auditoría y acceso mínimo por DEV/QA/PROD. | Pendiente de objetivo cuantitativo/medición y responsable explícito |
| NF07 | Reproducibilidad de artefactos, dependencias y restauración versionada. | Pendiente de objetivo cuantitativo/medición y responsable explícito |
| NF08 | Accesibilidad/ligereza Angular, UX responsive y navegación bajo degradación. | Pendiente de objetivo cuantitativo/medición y responsable explícito |
| NF09 | Trazabilidad por request/tenant sin almacenar secretos o datos privados innecesarios. | Pendiente de objetivo cuantitativo/medición y responsable explícito |
| NF10 | Reversibilidad de DNS, proxy, contenedores, datos y objetos con responsables. | Pendiente de objetivo cuantitativo/medición y responsable explícito |

**No se fijan cifras ficticias de p95, RTO/RPO, cuota, facturación ni costos**. Cada umbral deberá tener aprobación, línea base actual, forma de medir, tamaño de muestra, ambiente y responsable. Un resultado no medido no es PASS.

## Correspondencia causal con fuentes y con riesgos

- **R01/R02/R03/R15:** `render.yaml` blob `f64d291425e2bbfdd9b6466d268753f8f8d86b3d`; `EnvironmentDatabaseGuard.cs` blob `896aa003876f2b4299dfe5d4f1d69b83b5d1be22`; `RenderEnvironmentContractGuard.cs` blob `2c38ebb90a8360b61c9e05df96ff3a9abd5b8f65`. P05/P06/N05/N06 verifican que los guards no se salten al migrar.
- **R04/R10:** `frontend/vercel.json` blob `5f12bcf667a21afcfc4314b66787ff66627ae666` y referencia de arquitectura `1931e1229752028229b6ad1bb3368038e2c6769b`. P07–P10 requieren rechazos tenant/JWT y continuidad de routing/SSL.
- **R05/R14:** `backend/Dockerfile` blob `5bcfd9722c5b88fb7b081a7f23c2b70eb7b56327`, API csproj `2a7b33a0974d1ebe13676593d84b458b1243e037`, Infrastructure csproj `6210d8b12abd77171d95d17e2292c1a319b89263`. P01/P02 establecen build y usuario mínimos.
- **R06–R16:** matriz en M01.04; P11–P16 y NF01–NF10 requieren pruebas de secretos, restore, rollback, workers, medios y observabilidad antes de una futura implantación.

## Gate de aceptación de esta microtarea documental

**A0:** identidad y Skill canónica de SOLQARYN, HEAD exacto, Scope Lock, PLAN H/I, COLA G/O y DAG F/G por ID, preflight de ownership CAS exclusivo.

**A1 documental:** 16 criterios positivos y 16 negativos observables, cada uno asociado a una condición de rechazo; validar estructura, trazabilidad y ausencia de falsa ejecución. **No afirmar PASS de las pruebas futuras.**

**A2 seguridad documental:** escenarios explícitos de host/SSL, JWT, RBAC, tenant A/B, secretos, IAM y puertos privados; los controles en código no se modifican. **Negativo:** nunca tratar `tenant_id` del cliente como autoridad.

**A3 documental:** fuentes Git exactas y dependencias de M01.01–.04 cotejadas; Cloudinary/R2, DNS/TLS y MySQL futuro **NO_EJECUTADOS**. No correr operaciones live ni acceder a secretos.

**A4:** publicar exclusivamente este Markdown y la sección propia de `CHANGELOG_AI.md` por fast-forward CAS en dev, verificar `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` del SHA; confirmar blobs y readback de PLAN/COLA/BITACORA. Las compilaciones runtime no causales son **NO_APLICA**, nunca PASS.

**A5:** requiere TASK_ID distinto de otra Task Javier, pendiente tras el cierre DEV. No habilita desplegar Hostinger/Cloudflare, QA/main/PROD o migraciones. **Rollback:** `git revert` normal, commit aditivo, sin reescribir BITACORA ni force-push.

**Resultado permitido:** criterios futuros trazables para revisión independiente, sin afirmar pruebas de topología ejecutadas.
