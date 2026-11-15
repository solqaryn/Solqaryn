# SOLQARYN V5 FINAL — C15-02.M01.05: aceptación observable de auditoría arquitectura/migración

**Fecha local:** 2026-11-14. **Autor:** PRIMARY DEV 01 JAVIER (`6adbb9fff1548191a1bd5b30d9e9984a`). **Rama:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Naturaleza:** criterios de aceptación propuestos y no ejecutados para futura auditoría de arquitectura/migración; no se declara sistema objetivo desplegado ni certificación P0/P1.

**Fuentes autorizadas:** STANDARD V5 FINAL `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`; `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_STATUS.md` y Skill SOLQARYN canónica de `dev`. Soporte documental de M01.01 (16 invariantes/8 pares), M01.02 (12 actores, 12 IO, 10 fronteras), M01.03 (21 fuentes/blobs y 12 brechas), M01.04 (14 riesgos potenciales, 10 supuestos, 10 decisiones). Habilitación dinámica previa: PLAN fila 2202 PENDIENTE sin owner, COLA G/O=SI, DAG F=VALIDADO/G=SI, seguida de reclamación exclusiva CAS.

## Contrato de evidencia y estados

Una prueba futura válida debe tener `CASE_ID`, objetivo y negativo explícitos, componente/ruta, entorno (efímero/DEV autorizado), actor/rol, tenant A/B, SHA de código, versión de servicio o artefacto, job o request verificable, esperado/observado, hora UTC, logs redactados y rollback. Un `CHECK_PASS` requiere GitHub `status=completed`, `conclusion=success`, `head_sha` exacto. Estado `NO_EJECUTADO` no es PASS ni FAIL. Un documento, un archivo de workflow y una entrada en DAG NO acreditan resultados runtime.

## Dieciséis criterios pares positivos/negativos futuros

| Caso | Oráculo positivo observable | Negativo bloqueante observable | Fuente o contrato |
| --- | --- | --- | --- |
| AC01 | Inventario Git de paths/blobs coincide con SHA base | Fuente inexistente, blob equivocado o SHA omitido | M01.03 S01–S21 |
| AC02 | Diagrama distingue actual declarado de objetivo futuro | Hostinger/R2 aparecen como activos sin readback | M01.01 NR01 |
| AC03 | Angular 22.2.1/net10.0/EF10 y proveedor declarado coinciden | Versiones de otra rama se presentan vigentes | package.json/csproj |
| AC04 | JWT legítimo, rol y pertenencia A autorizan recurso A | JWT manipulado, revocado o de B autoriza A | API JWT/RBAC |
| AC05 | Consulta MySQL por tenant/sucursal devuelve sólo pertenencia propia | Cross-tenant A/B o IDOR no es denegado | AppDbContext/EF fixture |
| AC06 | Host confiable enruta tenant correcto sin privilegio por request | Host/header arbitrario suplanta tenant | HostTenantResolver |
| AC07 | Migración EF reversible aplicada y validada en entorno efímero | DDL sin backup/rollback modifica datos reales | MySQL/EF |
| AC08 | Backup restaurado arroja conteos y checksum esperados | Solo existe archivo/runbook, sin restore probado | Runbook DEV |
| AC09 | Archivos reales se inventarían con owner por tenant | Desconocido se reporta CERO o B se entrega a A | Cloudinary/R2 objetivo |
| AC10 | URL firmada de objeto autorizado respeta TTL y propietario | Token expirado o archivo de B se acepta | Storage futuro |
| AC11 | Reintento del worker entrega efecto único para misma clave | Replay duplica efecto o cambia tenant | Outbox/worker |
| AC12 | Configuración CORS y credenciales separa entornos | Origin malicioso, secretos en logs o repo | API/workflows |
| AC13 | Frontend ruta y errores mantienen sesión/autorización | Guard UI confiere permisos omitidos por API | Angular/Playwright |
| AC14 | DNS/TLS host binding es comprobado, reversible y autorizado | Corte DNS real sin aprobación ni rollback | Gobierno/infra objetivo |
| AC15 | CI causal y Scope Lock completed/success en SHA exacto | queued/skipped/cancelled/failed se cuenta PASS | GitHub check-runs |
| AC16 | Revisor A5 tiene Task ID Javier distinto del autor | Autorrevisión o Task retirada se acepta | PLAN/BITACORA/allowlist |

**AC01–AC16: TODOS FUTUROS_NO_EJECUTADOS.** Ninguna prueba de HTTP, MySQL en servicio, EF migration, restore, Cloudflare/R2 real, DNS, Playwright, worker ni proveedor externo fue ejecutada en esta unidad. Los escenarios no se certifican por haber sido descritos. `INVENTARIO_DESCONOCIDO` nunca equivale a `INVENTARIO_CERO`.

## Doce requisitos no funcionales pendientes de medición

| ID | Cualidad | Oráculo/medición requerida para una fase posterior | Estado |
| --- | --- | --- | --- |
| NF01 | Aislamiento | Dos tenants de prueba A/B sin exposición cruzada; suite negativa | NO_MEDIDO |
| NF02 | Trazabilidad | Evidencia con HEAD, blob, job, actor, tenant, timestamp y entorno | NO_MEDIDO |
| NF03 | Reversibilidad | Restore/revert con integridad y criterio de abortar medido | NO_MEDIDO |
| NF04 | Idempotencia | Repetición de correlación/clave sin efecto adicional | NO_MEDIDO |
| NF05 | Cifrado | Secretos fuera de Git, certificados válidos, TLS aplicable | NO_MEDIDO |
| NF06 | Observabilidad | Logs seguros con correlación y sin PII/credenciales | NO_MEDIDO |
| NF07 | Disponibilidad | Health/readiness y fallo controlado, SLO futuro definido | NO_MEDIDO |
| NF08 | Latencia | Presupuesto definido y medición Angular/API, sin umbral inventado | NO_MEDIDO |
| NF09 | Accesibilidad | Teclado, semántica y responsive en browser reproducible | NO_MEDIDO |
| NF10 | Paridad | Artefacto y configuración de origen/destino contrastables | NO_MEDIDO |
| NF11 | Integridad | Checksums, conteos y owner por tenant de medios/DB | NO_MEDIDO |
| NF12 | Control de cambio | A5 diferente, SHA exacto, autorización de entorno y rollback | NO_MEDIDO |

No se inventan latencias, disponibilidad, RTO/RPO, totales de archivos, conteos por tenant, fallos de seguridad ni valores de pruebas. La ausencia de un reporte no prueba conformidad; riesgos potenciales M01.04 mantienen clasificación `NO_VERIFICADO` hasta evidencia causal. Al hallarse un fallo real, registrar P0/P1 y su reparación por mecanismo institucional antes de avance, sin convertirlo en un pase administrativo.

## Matriz de puertas de avance

| Puerta | Evidencia indispensable | Resultado actual |
| --- | --- | --- |
| M01.05 preparación documental | Criterios observables, negativos y límites de fuente | Documento preparado; A0–A4 documentales, A5 pendiente |
| M01.06 cierre documental terminal | Prerrequisitos DAG, documentos M01.01–05, A5 y autorización específica | **NO_HABILITAR_AHORA**; fila DAG G=NO al preflight |
| M02/M03 construcción y tests | Contratos+artefactos, JWT/RBAC/tenant A/B, CI .NET/Angular/MySQL por SHA | No liberados por esta unidad |
| Ensayo de migración | Ambiente efímero, inventarios reales, restore, autorizaciones y rollback | NO_ACREDITADO |
| QA/main/PROD y DNS/objetos | Nueva aprobación expresa del propietario y gates verificables | FUERA_DE_ALCANCE |

La topología actual declarada por el repositorio incluye Vercel, Render, Aiven y Cloudinary; Hostinger, Cloudflare y R2 son objetivo V5 futuro sin confirmación operativa. La mera mención de un proveedor no es permiso de uso ni prueba de inventario.

## A0–A5, impacto y reversión

- **A0:** integridad de dieciséis pares AC, doce NF y referencias M01.01–04; chequeo de CHANGELOG para encabezados vacíos y viñetas cruzadas.
- **A1:** se verificó consistencia de especificación y oráculos positivos/negativos; **no** se ejecutaron esas pruebas funcionales.
- **A2:** no hay cambio a JWT/RBAC, tenant A/B, dominios, DB, secrets, tokens, UI, endpoints o cloud. Estado de seguridad runtime **NO_ACREDITADO**.
- **A3:** contrastes documentales contra código, arquitectura y predecesores; .NET/Angular/MySQL/Docker/Playwright `NO_APLICA` al changeset Markdown sin pretender PASS.
- **A4:** fast-forward/CAS dev, diff sólo documento y sección autocontenida `CHANGELOG_AI.md`, checks Scope Lock y V5 provenance exact-SHA completed/success, readbacks PLAN/COLA/BITACORA, sin falsear otros jobs.
- **A5:** PENDIENTE de otra Task Javier con ID distinto del autor, antes de cualquier promoción. La eventual revisión documental no certificará servicios externos.
- **Rollback:** `git revert` normal tras comprobar HEAD concurrente; nunca forzar refs ni alterar BITACORA histórica.

**Exclusiones:** workers o eventos nuevos, webhook dispatcher, migración de datos, cambios de DNS/TLS/R2/Hostinger, compras, Secrets, QA, main y producción. El lote M01.06 permanece sujeto a gate dinámico, independientemente de este cierre documental.
