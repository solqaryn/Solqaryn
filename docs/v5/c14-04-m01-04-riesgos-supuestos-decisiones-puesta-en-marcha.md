# SOLQARYN V5 FINAL — C14-04.M01.04: riesgos, supuestos y decisiones de puesta en marcha

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 03 JAVIER `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Rama:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Clase:** evaluación documental, no prueba operativa, simulacro, despliegue ni decisión de contratación.

**Autoridad:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit) y gobierno vigente. Contratos previos [M01.01](./c14-04-m01-01-alcance-puesta-en-marcha-soporte.md), [M01.02](./c14-04-m01-02-actores-interfaces-puesta-en-marcha.md), [M01.03](./c14-04-m01-03-inventario-fuentes-evidencia-puesta-en-marcha.md). PLAN fila 2129, COLA G/O SI, DAG F VALIDADO/G SI antes de claim. La aptitud de este documento no libera automáticamente M01.06 ni QA/main/PROD.

## 1. Alcance y clasificación de evidencia

Se inventarían **riesgos potenciales**, supuestos aún no demostrados y decisiones **abiertas** para el futuro circuito de landing/host, archivos, backup, soporte, alertas, observación y rollback. Las severidades P0/P1/P2 son **severidad potencial de diseño**, nunca incidencias realmente halladas ni vulnerabilidades verificadas. Un riesgo sólo es hallazgo real cuando un test/alerta causal produce evidencia, timestamp, actor y SHA en ambiente autorizado. Ningún plan cumple por sí mismo RPO/RTO o seguridad de tenant.

El inventario M01.03 acredita 19 blobs Git, no checks operacionales. Se conservan ASP.NET Core net10.0, Angular 22.2.1, EF/MySQL, JWT/RBAC y backend como autoridad; los entornos actuales documentados siguen Vercel/Render/Aiven/Cloudinary. Hostinger/Cloudflare/R2 son futuros; no se crean cuentas ni se alteran credenciales, DNS, jobs, datos ni infraestructura.

## 2. Riesgos potenciales R01–R14

| ID | Riesgo potencial no observado | Impacto si ocurre | Mitigación futura | Evidencia exigible para reducirlo |
| --- | --- | --- | --- | --- |
| R01 | Release sin prueba causal exact-SHA | P0 potencial | Diferenciar tests dirigidos y checks queued/failed; verificación de digest y Scope Lock | Documento de manifiesto + jobs success del SHA antes de cualquier autorización |
| R02 | Despliegue por interpretación de LISTO_DEV/A5 | P0 potencial | Autorización futura separada por entorno, custodio y rollback | Acta de autorización humana específica para QA/main/PROD |
| R03 | Host header o tenant spoofing en landing | P0 potencial | Resolver host permitido del lado servidor; negar host/slug fabricado | Prueba HTTP con host inválido + comparación tenant A/B |
| R04 | Filtración entre empresas vía API o caché | P0 potencial | JWT, RBAC y ownership siempre en backend; claves de caché tenant-aware | Suite cross-tenant con fixtures A/B y respuesta sin PII |
| R05 | Archivos ajenos o URL firmadas vencidas | P1 potencial | Validar ACL, propietario y vencimiento en proveedor actual/futuro | Pruebas download A/B y URL expirada |
| R06 | Migración/restore destruye historial de negocio | P0 potencial | Snapshot cifrado, restore aislado, reconciliación y rollback no destructivo | Bitácora de restore sintético + inventario/saldos comparados |
| R07 | Backup programado sin restauración usable | P1 potencial | Separar éxito de backup y éxito de restore; medir RPO/RTO real | Prueba de restauración aislada de última copia |
| R08 | DNS/TLS/ingress con ventana mal planificada | P1 potencial | Preflight, TTL/redirects/reversión y stop condition por entorno | Smoke host + DNS/TLS verificados en ventana aprobada |
| R09 | Alertas sin cobertura o falsos positivos | P1 potencial | Inventario SLI/SLO, correlación SHA y severidad, reducción de ruido | Prueba de alerta sintética y acuse de guardia |
| R10 | Incidente sin respuesta o propietario | P1 potencial | Responsable, canales, escalación, umbrales y runbook con prácticas | Simulacro controlado documentado y decisión stop |
| R11 | Secreto/PII en logs o artefactos CI | P0 potencial | Redacción, control de acceso y pruebas sintéticas sin secretos reales | Escáner de outputs y revisión de trazas |
| R12 | Regresión UX responsive/accesibilidad | P2 potencial | Playwright/teclado/error/loading con fixtures por release | Browser test real y regresión de foco/viewports |
| R13 | Proveedor futuro sin permisos/costo aprobado | P1 potencial | No crear Hostinger/Cloudflare/R2 ni comprar; permitir sólo diseño documental | Permiso/comercial/rollback documentados antes de contrato |
| R14 | Eventos y retries duplican efectos o auditoría | P1 potencial | No crear dispatcher ahora; futura idempotencia, exclusión y cuota | Ensayo de reintento con dedupe y no duplicación transaccional |

**Estado R01–R14:** `POTENCIAL_NO_VERIFICADO`. No se declara abierto un incidente real, P0/P1 comprobado ni una solución desplegada. Un hallazgo real futuro debe enlazar `CASE_ID`, `SOURCE_SHA`, actor/tenant/rol, request o fixture sintético, severidad, log redactado, decisión y responsable.

## 3. Supuestos S01–S08 pendientes de validación

| ID | Supuesto | Condición y falta de evidencia |
| --- | --- | --- |
| S01 | La arquitectura Angular22.2.1/.NET10/MySQL 8.4 sigue siendo base | Basado en archivos; validar por ambiente antes de release |
| S02 | Existe permiso de lectura Git y de los artefactos de control | Verificado sólo en esta corrida; no implica permisos en proveedores |
| S03 | Las políticas de host/SEO publicadas son suficientes | NO_VERIFICADO; host/cors/tenant requieren pruebas adversariales reales |
| S04 | La infraestructura de backup ofrece restore usable | NO_VERIFICADO por runbook; hace falta ensayo restauración |
| S05 | El inventario de medios puede construirse sin acceso a datos privados | NO_VERIFICADO; priorizar metadata autorizada y sintética |
| S06 | Las alertas tienen ownership y cobertura por tenant | NO_VERIFICADO; analizar paneles y alert rules en fase aprobada |
| S07 | QA/PROD pueden tener paridad sin copiar secretos | NO_VERIFICADO; comparar sólo variables no sensibles y digest |
| S08 | La salida puede revertirse sin pérdida comercial | NO_VERIFICADO; exigir prueba de rollback y transacciones |

**Estado S01–S08:** sólo S01/S02 tienen evidencia estática de repositorio/conexión en esta corrida, no certificación del proveedor. Los restantes están `NO_VERIFICADO` o limitados por ambiente. Si un supuesto crítico falla, se revisa alcance/gate específico, no se declara LISTO por documentación.

## 4. Decisiones abiertas D01–D08

| ID | Decisión futura | Autoridad competente | Estado |
| --- | --- | --- | --- |
| D01 | Autorizar o rechazar alcance de QA/main/PROD | Propietario | Abierta; este plan no concede ese permiso |
| D02 | Proveedor y estrategia DNS/TLS por entorno | Propietario + operador | Abierta; Render/Vercel actuales y Cloudflare/Hostinger objetivo |
| D03 | Política de migración de archivos y R2 | Propietario + seguridad | Abierta; no asumir objetos existentes |
| D04 | RPO/RTO objetivo y evidencia restore | Propietario + infraestructura | Abierta; no afirmar objetivos cumplidos |
| D05 | Ventana de observación, umbrales y stop | Responsable futuro de release | Abierta; no definir SLO como logrado |
| D06 | On-call, incident severity y escalación | Responsable de soporte | Abierta; no crear guardias ficticias |
| D07 | Conjunto obligatorio de smoke/CI por componente | Líder técnico + revisor separado | Abierta; exigir exact SHA y scope |
| D08 | Condición formal de rollback y custodia del acta | Propietario + operador | Abierta; prohibido force-push/reset |

Ninguna decisión D01–D08 se toma implícitamente mediante este commit, por checks, ni por A5. Cuando exista una orden de propietario para QA, main o producción, registrar el alcance, aprobación, ventana, rollback y readback; no anticipar gastos ni infraestructura.

## 5. Ocho pares de escenarios futuros (no ejecutados)

| Caso | Positivo que debe acreditarse | Negativo que mantiene NO_GO |
| --- | --- | --- |
| P01/N01 | SHA/digest y required jobs completed/success | Cualquier job obligatorio en cola/cancelled o digest distinto |
| P02/N02 | QA/PROD tienen autorización independiente | LISTO_DEV/A5 sin orden expresa |
| P03/N03 | Host real resuelve tenant propio | Host spoofing o tenant B desde A |
| P04/N04 | JWT/RBAC servidor y propiedad de archivos correctos | API/file con permiso sólo por UI o ID externo |
| P05/N05 | Backup restaurado aislado y reconciliado | Snapshot corrupto o persistencia cruzada |
| P06/N06 | Alerta/guardia correlacionan error e incidente | 5xx sin respuesta/criterio stop |
| P07/N07 | UI Playwright responsive/teclado real | Spec sin corrida o foco inaccesible |
| P08/N08 | Rollback seguro y BITACORA append-only | Reset/force-push/borrado o duplicación |

Los pares P01–P08/N01–N08 son `FUTUROS_NO_EJECUTADOS`. Por este change no se ejecutaron HTTP, MySQL, Playwright, backup/restore ni provider APIs. `skipped`/`cancelled`/`queued`/`in_progress` no es PASS requerido. Pruebas extrañas a dos Markdown se clasifican `NO_APLICA` por causa y no como PASS.

## 6. Impacto, A0–A5, condiciones y reversión

**Impacto exclusivo:** este Markdown + encabezado y viñetas unidos en CHANGELOG_AI.md. No se modifica API/DTO, Domain/Application/Infrastructure, frontend, base de datos, JWT/RBAC/tenant, rutas host, DNS, cloud, proveedores, secretos, datos históricos ni tareas automatizadas. Consumidores: M01.05 aceptación, M01.06 gate de etapa y futuros M02–M06 sólo tras DAG y autorizaciones propias.

- **A0 estática:** R01–R14, S01–S08, D01–D08 y ocho pares P/N; encabezados CHANGELOG con viñetas bajo su encabezado.
- **A1 documental:** mitigaciones, responsables, positivos/negativos y evidencia falsable, ninguno ejecutado.
- **A2 documental:** preservación de JWT, RBAC, tenant A/B, integridad comercial, PII y secretos; sin runtime alterado.
- **A3 integración documental:** coherencia con M01.01–03 y blobs verificados; compilación .NET/Angular, Playwright/MySQL `NO_APLICA` a dos Markdown, no `PASS`.
- **A4 publicación:** fast-forward/CAS de dev, diff exacto dos Markdown, Scope Lock y verificador V5 de procedencia `completed/success` del SHA, readback PLAN/COLA/BITACORA.
- **A5:** `PENDIENTE` de otra Task Javier con TASK_ID distinto; nunca autoaprobar ni publicar QA/main/PROD por A5.

**Reversión:** git revert normal del commit documental después de verificar HEAD vigente. No forzar rama, resetear, borrar histórico de BITACORA, modificar proveedores ni tocar datos reales.
