# SOLQARYN V5 FINAL — C14-04.M01.06: bloqueo, rollback y salida documental de puesta en marcha

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 02 Javier (`6af16bb9492c8191b8e8c7d606f11507`). **Repositorio:** `solqaryn/Solqaryn@dev`. **Scope Lock:** STRICT. **Tipo:** contrato DOCUMENTAL, no release, migración, restore o aceptación QA/PROD.

**Fuentes:** STANDARD V5 FINAL (`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`); PLAN_MAESTRO, COLA, DAG_TECNICO y BITACORA (`1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`); `AGENTS.md`; `docs/PROJECT_SCOPE_LOCK.md`; `docs/AUTOMATION_STATUS.md`; arquitectura canónica; documentos `C14-04.M01.01–05`.

## 1. Objetivo y delimitación

Se formalizan **condiciones de bloqueo, contención, reversión y liberación documental** para la futura puesta en marcha. `M01.01–05` figuraban LISTO y con revisión A5 independiente antes del claim de esta microtarea; el A5 de M01.05 se verificó por PRIMARY DEV 02 sobre SHA `83089fdbdc9aea9198c0ebec2cba197907354ba7`, doc blob `6ec8e0d2aa9a2383cb64089e2b104027c63a7a19`. M01.01–04 son de autoría DEV03 con A5 DEV04 según PLAN. Los gates se releen por ID exacto antes de avanzar a M02.

El stack actual mantiene Angular 22.2.1, ASP.NET Core net10.0, EF Core/MySQL, JWT/RBAC, Cloudinary y servicios Vercel/Render/Aiven. Hostinger, Cloudflare y R2 son destino conceptual **futuro**. Esta tarea no implementa eventos, callbacks, endpoints, tablas, proveedores, certificados, DNS, secretos ni cambios productivos. Criterios P01–P16/N01–N16 y NF01–NF12 de M01.05 son `FUTUROS_NO_EJECUTADOS` / `REQUISITOS_NO_MEDIDOS`, no resultados medidos.

## 2. Catorce condiciones de bloqueo fail-closed

| ID | Situación | Comportamiento exigido |
| --- | --- | --- |
| B01 | SHA, diff o manifiesto de artefacto no verificables | Detener promoción; exigir SHA y digest comprobados |
| B02 | Scope Lock o V5 obligatorio no completed/success exact-SHA | Conservar VALIDANDO y corregir causa, sin bypass |
| B03 | CI dirigida requerida failed, skipped, cancelled, queued o in_progress | No contar PASS; esperar o reparar con evidencia |
| B04 | M01.01–05 o A5 independiente sin acreditar | No habilitar salida terminal M01.06 ni M02 |
| B05 | Autorización QA/main/PROD o despliegue inexistente | Permanecer en dev; permiso expreso por ambiente |
| B06 | Host/TLS/tenant o redirección cross-tenant sin negativa probada | Restringir tráfico afectado y exigir prueba A/B |
| B07 | JWT/RBAC/roles o acceso a archivos de otra empresa sin control | Denegar por defecto y preservar evidencia redactada |
| B08 | Objetos Cloudinary o futuros R2 sin dueño/hash/ACL | No migrar ni inventar inventario acreditado |
| B09 | Respaldo/restauración sin ensayo aislado ni integridad verificada | No certificar RPO/RTO; prohibir restore productivo |
| B10 | Health/readiness o alertas sin pruebas con trazas | No declarar SLO/disponibilidad |
| B11 | Incidente/soporte sin dueño, severidad, escalación o privacidad | Mantener incidente abierto y aislar datos |
| B12 | UX accesible/responsive o rendimiento sin observación real | No declarar conformidad LCP/INP/CLS/teclado |
| B13 | P0/P1 causal abierto, error persistente o regresión de datos | Bloquear salida específica y corregir causalmente |
| B14 | Rollback, stop condition o responsable no acordado | Rechazar liberación hasta plan reversible autorizado |

B01–B14 son reglas de decisión propuestas; no hallazgos confirmados. Ante un fallo, detener el ámbito causal y preservar el resto de microtareas independientes. No desactivar verificadores, cambiar fórmulas de elegibilidad, autoaprobar A5, suplantar mediciones ni reescribir BITACORA.

## 3. Diez medidas preventivas de reversión y contención

| ID | Condición o actuación futura, siempre autorizada |
| --- | --- |
| RB01 | Registrar SHA exacto, digest, configuración y manifest antes de tocar estado |
| RB02 | Contener sólo la superficie afectada, sin detener scopes independientes seguros |
| RB03 | Revocar exposición o flag sólo con mecanismo autorizado y comprobado |
| RB04 | Revertir código dev mediante git revert, sin force-push ni reset destructivo |
| RB05 | Aislar tenants A/B y denegar accesos no autorizados; auditoría y PII redactada |
| RB06 | Preservar originales/hashes/ACL; no borrar ni mover objetos a R2 sin autorización |
| RB07 | Exigir backup e instrucción específica antes de migrar/restaurar bases vivas |
| RB08 | Revalidar pruebas positivas/negativas causales y Scope Lock en SHA nuevo |
| RB09 | Actualizar incidente con fecha UTC, responsable, resultado y correlación |
| RB10 | Exigir autorización independiente de entorno y readback para despliegue |

**Estado actual: NO_EJECUTADO.** No se probaron backup, restore, recuperación de tenant, DNS, RPO/RTO ni rollback en entornos reales. Para estos dos Markdown el rollback aplicable sería un `git revert` normal y causal sobre dev, tras releer HEAD, sin forzar historia ni borrar evidencia histórica.

## 4. Catorce condiciones de salida y sucesión controlada

| ID | Evidencia requerida o límite |
| --- | --- |
| L01 | M01.01–05 en LISTO y A5 independiente acreditada por Task diferente |
| L02 | M01.06 documental revisada A0–A4; su A5 permanece pendiente hasta revisión ajena |
| L03 | Scope Lock STRICT y CHANGELOG V5 completed/success en SHA publicado |
| L04 | Diff limitado a contrato documental y CHANGELOG autocontenido |
| L05 | Gates DAG/PLAN/COLA releídos después de cada transición |
| L06 | Ningún A5_FAIL causal activo sobre precedente requerido |
| L07 | Matriz NR/IN/OUT/B/G con estado conocido, sin afirmar ejecución futura |
| L08 | 16 pares P/N de M01.05 permanecen FUTUROS_NO_EJECUTADOS hasta pruebas |
| L09 | Doce NF siguen NO_MEDIDOS, sin porcentajes inventados |
| L10 | Seguridad JWT/RBAC/tenant A/B preservada, sin cambios de código |
| L11 | Host, archivos, backups, incidentes y accesibilidad con dueño de prueba futura |
| L12 | Rollback no destructivo identificado; ensayo real futuro condicionado |
| L13 | A5 técnica independiente de M01.06 antes de M02 cuando DAG la exija |
| L14 | QA/main/PROD, DNS, proveedores, datos productivos no autorizados |

`LISTO_DEV` significa únicamente A0–A4 documentales aprobadas con Scope Lock/V5 de SHA exacto. El siguiente gate técnico de M02 es independiente, exige A5 revisora distinta y DAG VALIDADO/SI más COLA G/O=SI. VALIDANDO no libera dependencias terminales y no se fuerza G/O. Ninguna fase documental autoriza QA/main/PROD.

## 5. Ocho pares de escenarios para aceptación futura

| Escenario | Positivo previsto | Negativo previsto |
| --- | --- | --- |
| P01/N01 | Fuente Git y SHA concuerdan con diff | SHA incorrecto/check cancelado nunca acredita salida |
| P02/N02 | M01.01–05 LISTO+A5 corroborados | Precedente VALIDANDO/A5_PENDIENTE bloquea gate |
| P03/N03 | Control A/B en entorno de prueba autorizado | Tenant A no extrae datos de tenant B |
| P04/N04 | Archivo sintético con hash y dueño legítimo | URL vencida o inter-tenant se deniega |
| P05/N05 | Backup sintético restaurado y checksums íntegros | Restore no ejecutado no prueba RPO/RTO |
| P06/N06 | Readiness/alerta correlacionadas con release y trace | Alerta ficticia no cierra incidente |
| P07/N07 | Playwright comprueba teclado, foco y responsive | Spec no ejecutada no es PASS |
| P08/N08 | Rollback autorizado sin pérdida de datos | Cambio DNS/DB/productivo sin permiso se rechaza |

**P01–P08/N01–N08 = FUTUROS_NO_EJECUTADOS.** No se realizaron en esta microtarea ensayos JWT, peticiones HTTP, MySQL, Playwright, restore, alertas, TLS ni mediciones reales; sus umbrales siguen NO_MEDIDOS. Deben diseñarse fixtures sin secretos y cumplir denegación por defecto.

## 6. Expediente obligatorio por transición

Cada transición futura debería enlazar `MICROTASK_ID`, SHA/digest, `CASE_ID`, actor/rol, tenant sintético A/B, evidencia observada contra esperado, entorno autorizado, timestamp UTC, owner y revisor distintos, `job_id`, `status`, `conclusion`, `head_sha`, incidente, riesgo, contención y rollback. `skipped`, `cancelled`, `failed`, `queued` o `in_progress` no son PASS si el check es obligatorio. Los casos no aplicables se motivan expresamente y no se contabilizan.

## 7. Impacto A0–A5 y reversión del changeset

- **Ámbito:** sólo este Markdown y sección autocontenida de `CHANGELOG_AI.md`; ninguna modificación en Domain/Application/Infrastructure/API/frontend, DTO, base, JWT, RBAC, tenants, objetos o infraestructura.
- **Dependencias/consumidores:** M01.01–05, su A5 independiente, alcance C14-04 y futuros contratos M02; futuras decisiones de ambiente permanecen separadas.
- **A0:** comprobar B01–B14, RB01–RB10, L01–L14, ocho escenarios P/N, referencias y ausencia de secretos y encabezados vacíos.
- **A1:** revisar falsabilidad documental positiva/negativa; pruebas runtime `NO_EJECUTADAS`, no atribuirles PASS.
- **A2:** fronteras JWT/RBAC/tenant A/B, host y PII preservadas por no cambiar código.
- **A3:** contrastar con documentos M01.01–05, autoridad canónica y DAG; compilación .NET/Angular/MySQL/browser no causal al diff Markdown se clasifica `NO_APLICA`, nunca PASS.
- **A4:** publicación Git fast-forward/CAS en dev; diff y blobs revisados; Scope Lock y V5 `completed/success` para SHA exacto, con readbacks PLAN/COLA/BITACORA.
- **A5:** queda `A5_PENDIENTE` para una de las otras cinco Tasks Javier, distinta de la autora. Nunca autoaprobación.
- **Rollback:** `git revert` regular con comprobación de HEAD; mantener trazabilidad y evitar force-push.
  
**Sin autorización para QA/main/PROD, datos productivos, DNS, Cloudflare/R2, compras, migraciones ni dispatcher de eventos.**
