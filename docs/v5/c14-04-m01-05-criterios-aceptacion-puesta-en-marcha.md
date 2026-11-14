# SOLQARYN V5 FINAL — C14-04.M01.05: criterios observables de aceptación de puesta en marcha

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 04 Javier (`6aeacb7820ac8191b7d768a6f33dfa09`). **Rama:** `solqaryn/Solqaryn@dev`. **Scope Lock:** `STRICT`. **Clase:** M01 documental de criterios; NO release, monitor desplegado, prueba de restore, ni aceptación QA/PROD.

**Autoridad:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_STATUS.md`, `references/architecture.md` y contratos C14-04.M01.01–03. M01.04 es fuente de riesgos a reconciliar al estar terminada por su propio autor; este documento no certifica ni suplanta su A5.

## 1. Objetivo y delimitación

Transformar el alcance M01.01 (NR01–NR16), interfaces M01.02 (A01–A14, IN01–IN12, OUT01–OUT12, B01–B10) y brechas M01.03 (G01–G12) en oráculos falsables para una futura puesta en marcha autorizada: landing y dominios, archivos, soporte, alertas, backup/restore, respuesta a incidentes, accesibilidad y observación. El criterio de aceptación es **resultado medido frente a un esperado y evidencia inmutable**, nunca la presencia de un Markdown, workflow o mock.

El stack vigente documentado es Angular 22.2.1, ASP.NET Core net10.0, MySQL/EF Core 10, JWT/RBAC, Cloudinary y servicios de transición Vercel/Render/Aiven. Hostinger/Cloudflare/R2 son objetivo futuro; esta tarea no usa esos proveedores ni cambia DNS, datos reales o secretos. Una CI terminada `skipped`, `cancelled`, `failed`, `queued` o `in_progress` no satisface un gate causal.

## 2. Dieciséis pares de aceptación POSITIVA / NEGATIVA (TODOS FUTUROS_NO_EJECUTADOS)

| Caso | Oráculo positivo a comprobar en futuro entorno autorizado | Oráculo negativo / bloqueo de salida | Invariantes y fuentes |
| --- | --- | --- | --- |
| P01 / N01 | Identidad `SOURCE_SHA` y digest desplegado coinciden con artefacto compilado y manifest de CI | SHA o digest difieren y la promoción es rechazada | NR02–04, IN01–03, OUT01–03 |
| P02 / N02 | `verify-solqaryn-project-scope` y checks causales del SHA terminan `completed/success` | `queued`, `skipped`, `cancelled`, `failed` o distinto `head_sha` se reporta como PASS | NR03–04, OUT03 |
| P03 / N03 | Host público permitido sirve landing apropiada y TLS válido, con identidad pública prevista | `x-forwarded-host`/Host falsificado redirige o muestra datos/configuración de otro tenant | NR07, B04, G01–02 |
| P04 / N04 | Usuario anónimo ve sólo contenido público autorizado y SEO coherente | Ruta pública revela contenido privado o indexa URLs internas | NR06–07, A09–A12, G02 |
| P05 / N05 | JWT válido permite recurso tenant A con rol y sucursal apropiados | JWT expirado/revocado, rol incorrecto o tenant A→B obtiene recurso de B | NR05–06, B01–03, G12 |
| P06 / N06 | Archivo de prueba propio recuperado con dueño, ACL, MIME, tamaño y vigencia de URL verificados | Token de otra empresa, URL caducada o archivo fuera de allowlist entrega datos | NR08, B06, G03–04 |
| P07 / N07 | Inventario real de objetos de transición se registra por tenant y hash, admitiendo cero objetos | Se inventa existencia de imágenes/archivos o se migra un objeto sin dueño verificable | NR08, IN07, G03–04 |
| P08 / N08 | Restore aislado de respaldo sintético verifica conteos, checksums, relaciones y auditoría | Restore no ejecutado, inconsistente o sobre datos productivos se declara probado | NR09–10, B05/B08, G05/G10 |
| P09 / N09 | Endpoint health/readiness y smoke API por release devuelven estado y trace ID medidos | Health falso, no correlacionado o servicio no probado se usa para declarar disponible | NR02/12, G07 |
| P10 / N10 | Alertas sintéticas de fallo 5xx/worker indican severidad, release, responsable y seguimiento | Incidente de prueba carece de señal o responsable y se marca resuelto | NR11–12, OUT09–10, G08–09 |
| P11 / N11 | Logs de pruebas redactados correlacionan operaciones sin JWT, claves, PII ni datos de B | Log o ticket divulga token, secreto o información inter-tenant | NR11, B09, G08/G12 |
| P12 / N12 | Ticket ficticio con clasificación, canal, escalación y respuesta documentada mantiene límite de tenant | Soporte asume identidad del solicitante o comparte historial de B con A | NR06/12, A06–08, G09 |
| P13 / N13 | Smoke Playwright verifica navegación, teclado, foco, loading/error y viewport pequeño | Spec presente sin ejecución real, pantalla bloqueada o inaccesible se presenta como PASS | NR13, G11 |
| P14 / N14 | Rollback autorizado revierte versión/configuración de ensayo, sin pérdida de datos | DNS/storage/código se cambia sin stop condition, reversión o permiso explícito | NR09/14, G10 |
| P15 / N15 | Ventana de observación define comienzo, fin, SLI, umbral acordado y decisión firmada | Objetivo SLO no medido o ventana omitida se registra como cumplido | NR12/15, OUT09–12, G08 |
| P16 / N16 | Revisor A5 distinto verifica cambios dev; QA/main/PROD requiere aprobación independiente de entorno | Autor se autoaprueba o LISTO_DEV se toma por permiso de producción | NR01/15/16, A01–04, B10 |

**Estado P01–P16 / N01–N16: `FUTUROS_NO_EJECUTADOS`.** Ninguno es evidencia de un HTTP real, MySQL, Playwright, restore, alarmas, SLO, proveedores o cert de seguridad. La salida futura sólo se autorizaría si los negativos son efectivamente rechazados y los positivos medidos, junto con permisos y ausencia de P0/P1 bloqueantes demostrados.

## 3. Doce requisitos no funcionales y forma de medirlos

| NF | Objetivo/condición (sin valor fabricado) | Métrica o prueba futura |
| --- | --- | --- |
| NF01 | Release reproducible | SHA, digest y manifiesto de build comparados por ambiente |
| NF02 | Disponibilidad definida por negocio | SLI/umbral/ventana acordados antes de medir; no declarar porcentaje ahora |
| NF03 | Latencia observable | p50/p95 de rutas públicas y privadas sobre fixtures y volumen acordado |
| NF04 | Aislamiento multitenant | Pares A/B y errores 403/404 esperados sin filtración en logs/caché |
| NF05 | Privacidad por defecto | Inspección de trazas, tickets, errores y exports redactados |
| NF06 | Recuperabilidad | Restore aislado y RPO/RTO medidos contra objetivos aprobados |
| NF07 | Integridad transaccional | Reconciliación de conteos, claves foráneas, auditoría y checksums |
| NF08 | Operabilidad de incidentes | Tiempo de detección/escalación y responsable asignado |
| NF09 | Accesibilidad y UX ligera | Keyboard/focus, responsive, loading/error y presupuesto de bundle con Playwright |
| NF10 | Reversibilidad | Ensayo controlado de rollback de artefacto/configuración con readback |
| NF11 | Seguridad y minimización de accesos | JWT/RBAC/CORS/host/storage probados por rol y tenant |
| NF12 | Auditabilidad y procedencia | EVENT_ID idempotente, `head_sha`, logs y A5 distinta sin reescritura |

**NF01–NF12 = REQUISITOS_NO_MEDIDOS.** No hay números objetivo ni métricas observadas fijados por este documento. Un futuro responsable debe establecer umbrales trazables y evidencia real en el ámbito autorizado, sin inventar cumplimiento.

## 4. Contrato de evidencia de futura aceptación

Por cada caso: `CASE_ID`, historia/actor, rol JWT (sin token), tenant sintético A/B, escenario y ambiente autorizado, SHA y digest, versión Angular/.NET/MySQL cuando corresponda, prueba/fixture no sensible, esperado, observado, `job_id`, `status`, `conclusion`, `head_sha` exacto, fecha UTC, redacción de logs, dueño y revisor distinto, severidad de hallazgo y rollback. Clasificar explícitamente `NO_APLICA` por causa y sin transformarlo en PASS. Una brecha G01–G12 requiere prueba específica, no declaración de vulnerabilidad por ausencia de datos.

Los artefactos futuros previstos son: matriz de pruebas positivas/negativas, informe de smoke de landing/API, inventario de objetos verificables, acta de restore sintético, correlación de alertas/incidentes, accesibilidad Playwright, autorización por ambiente y acta de stop/rollback. Referenciar A5 sólo como revisión técnica dev, nunca promoción automática. Para fallos de seguridad o integridad, bloqueo fail-closed y acción correctiva causal; no seguir al siguiente gate sin repetir la prueba fallida.

## 5. Impacto, controles A0–A5 y reversión

**Impacto primario:** únicamente este Markdown y una entrada autocontenida en `CHANGELOG_AI.md`. **Capas:** Domain/Application/Infrastructure/API/frontend **sin modificación**; no DTO, endpoints, contratos ejecutables, esquema EF, transacciones, migraciones, cola, workers, archivos reales, JWT, RBAC, DNS, TLS, secretos ni suscripciones. **Consumidores:** cierre documental M01.06 y planificación M02–M06 sujeto a DAG y A5; una matriz no libera gates de producción. **Riesgo:** equiparar aceptación escrita a validación real; mitigado por etiquetas FUTURO y NO_MEDIDO.

- **A0:** verificar 16 pares P/N, 12 NF, enlaces a M01.01–03, procedencia íntegra de CHANGELOG, ausencia de encabezados vacíos y secreto accidental.
- **A1:** oráculos positivos y negativos falsables pero `NO_EJECUTADO`; no declarar PASS de pruebas runtime.
- **A2:** preservar límites JWT/RBAC/tenant A/B, host, PII, storage y seguridad por no cambiar código.
- **A3:** cotejar alcance M01.01, interfaces M01.02, brechas M01.03 y stack vigente; build .NET/Angular, MySQL/Playwright `NO_APLICA` al diff exclusivamente Markdown, no PASS.
- **A4:** publicación direct dev fast-forward con CAS de HEAD, diff preciso, check Scope Lock y V5 de procedencia `completed/success` en SHA publicado y readback PLAN/COLA/BITACORA.
- **A5:** `A5_PENDIENTE` a una Task Javier diferente del autor. Ni A4 ni A5 confieren permiso de QA/main/PROD.

**Rollback:** `git revert` normal del único commit Markdown tras verificar HEAD contemporáneo; conservar BITACORA histórica. No force-push, reset, migración, evento/dispatcher o despliegue.
