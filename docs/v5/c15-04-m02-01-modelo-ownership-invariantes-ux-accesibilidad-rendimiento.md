# SOLQARYN V5 — C15-04.M02.01: modelo conceptual, ownership e invariantes de auditoría UX

**Autor:** PRIMARY DEV 02 — JAVIER_CHATGPT, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Fecha:** 2026-11-14, America/Tegucigalpa. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Modalidad:** contrato documental M02; no implica migración MySQL, entidad EF, DTO publicado, captura WCAG o métrica ejecutada.

## 1. Fuentes, predecesores y criterio de inicio

La fuente ejecutable es el Standard V5 FINAL (Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`) y su plan operativo de 2.340 IDs. En el preflight, `PLAN_MAESTRO!H2276=PENDIENTE`, I2276 vacío, `COLA!G/O2276=SI`, y `DAG_TECNICO!A2237=C15-04.M02.01`, F=VALIDADO/G=SI. Los seis antecedentes `C15-04.M01.01–06` estaban LISTO con A5 independiente; la revisión de M01.06 fue realizada por esta Task, distinta de su autor PRIMARY DEV 01, sin que ello sustituya una revisión de este nuevo M02.01.

Evidencia documental de origen con blobs revisados: M01.01 `b6ce51bd1bfce97b1d6f86b9af433e63922bd9bd` (NR01–NR14); M01.03 `0bdf78db8c82b8edc0fec223012a66e2b780be2c` (S01–S12, G01–G12); M01.05 `e54c3d99839f1925bf7544876e304f2589aff009` (AC01–AC16); M01.06 `5fff95e8a351a85bb1fee19a8cc95ffb9a8aae96` (B01–B12, publicación `ced0fec19e5474d5d56615cdb81c6fd9dd3590f1`). M01.02/04 completan el contexto de actores, superficies y riesgos. Sus escenarios son compromisos futuros, no certificados de ejecución.

El código de base consultado al HEAD de entrada incluye `frontend/src/app/app.routes.ts` (blob `2f560c14693455b79ac4546e9333fb74586ef234`) con rutas de storefront públicas y dashboard privado protegido por `authGuard` y `permisoGuard`; `frontend/src/styles/_enterprise-design-system.scss` (blob `6a188590af9a958bff2e3e0375c0c46cdb2b14e6`) define `--target-min:44px`, escalas y breakpoints; `frontend/src/styles/_m10-accessibility.scss` (blob `6140d41b8a7e1e98f4fa1e43e0ab72f04b6ae29f`) define foco, skip-link y espaciado; `frontend/playwright.config.ts` (blob `87946080e7fe87f5fa9e9c8d4c8da8a36f49d5da`) tiene configuración browser, no equivale a una prueba ejecutada. Angular 22.2.1/Zone.js 0.16.3 y backend .NET 10/JWT/RBAC/tenant A/B siguen vigentes. Ningún guard Angular sustituye la autorización del backend.

## 2. Modelo conceptual, sin nuevas tablas ni clases EF

**Separación explícita:** las entidades siguientes son *artefactos de evidencia propuestos* para una auditoría posterior, no objetos de base de datos ya existentes ni una migración aprobada. Las relaciones de permisos son obligaciones contractuales; cualquier implementación en M03 requerirá revisar modelos reales y construir pruebas de seguridad.

| Clave lógica | Registro conceptual / propiedad | Dueño permitido | Relación y control mínimo |
| --- | --- | --- | --- |
| UX01 | `AuditScope`: capítulo, superficie, ID de microtarea, SHA | Proyecto SOLQARYN y Task Javier del ID | 1→N `SurfaceObservation`; solo dev/plan V5 |
| UX02 | `SurfaceObservation`: ruta/pantalla, estado, viewport, dispositivo | Observador autorizado, nunca visitante con datos privados | N→1 `AuditScope`; evidencia aislada por tenant |
| UX03 | `ActorContext`: rol, tipo público/privado, permiso esperado | Backend/JWT/RBAC para superficies privadas | N→N superficies por relación de alcance, sin elevar privilegios |
| UX04 | `TenantBoundary`: empresa/sucursal/branding y sensibilidad | Tenant y backend que autoriza el recurso | N→1 `ActorContext` verificado, prohíbe fugas A/B |
| UX05 | `InteractionContract`: foco, teclado, error, recuperación | Componente dueño de UI sin sobrescribir API | N→1 `SurfaceObservation`; AC05–AC11 |
| UX06 | `AccessibilityEvidence`: criterio, método, resultado, capturas | Auditor autorizado con PII minimizada | N→1 `SurfaceObservation`, estado NO_EJECUTADO hasta medición |
| UX07 | `PerformanceEvidence`: red, dispositivo, métrica, baseline | Auditor autorizado; sin datos productivos sin permiso | N→1 `SurfaceObservation`, no inferir LCP/INP/CLS |
| UX08 | `Finding`: severidad provisional, escenario, riesgo, reproducción | Revisor responsable del scope, respetando A5 distinto | N→1 `AuditScope`; P0/P1 solo tras reproducción |
| UX09 | `RemediationDecision`: acción, dependencia, owner, rollback | Task autorizada mediante PLAN H/I CAS | N→1 `Finding`; no liberar QA/main/PROD |
| UX10 | `EvidenceProvenance`: SHA, blob, fixture, run real/no nativo | GitHub/Drive nativos verificados | N→1 hallazgo o medición; distingue observado vs supuesto |
| UX11 | `ApprovalRecord`: A0–A4 y A5 con TASK_ID | Autor y revisor independientes | 1→1 SHA/scope; no aprobarse a sí mismo |
| UX12 | `ReleaseBoundary`: entorno, gate, bloqueo y reversión | Gobierno de SOLQARYN; QA/PROD requieren autorización | N→1 `AuditScope`; estado documental no despliega |

Se describen claves y ownership para evitar mezcla de datos, pero no se crean `DbSet`, FK, columnas, endpoints, workers, tablas de métricas, colas ni registros de usuario. Persistencia y API se diseñarán solo en scopes autorizados, con revisiones de seguridad y plan de rollback correspondientes.

## 3. Invariantes funcionales, seguridad y aislamiento

| Id | Regla comprobable futura | Negativo que impide declarar PASS |
| --- | --- | --- |
| INV01 | Cada evidencia tiene scope/ID/commit SHA exacto | Captura o check de otro HEAD se usa como A4 |
| INV02 | Toda ruta privada conserva autorización backend | Un guard del navegador se toma como permiso API |
| INV03 | PII y branding de tenant A/B se mantienen separados | Caché de A deja ver dato o marca privada de B |
| INV04 | Observación contiene viewport y orientación, incluida 320 px | Se declara responsive sin fixture o desborde inspeccionado |
| INV05 | Teclado y foco se comprueban con interacción real | Hover visual se atribuye como test de teclado |
| INV06 | Contraste y nombre accesible se miden en estado específico | Token CSS se trata como certificación WCAG |
| INV07 | Preferencia de motion reducido conserva contexto | Animación oculta errores y controles al reducir movimiento |
| INV08 | Formularios exhiben mensaje de error textual y foco recuperable | Solo color señala fallo sin anuncio |
| INV09 | Resultados distinguen `NO_EJECUTADO`, `FAIL`, `PASS` | Un criterio definido se presenta como prueba realizada |
| INV10 | Evidencia de rendimiento contiene red, perfil, muestra y baseline | Se inventa un LCP/CLS/INP sin medición |
| INV11 | Solo el owner validado puede publicar cambios del ID | Escritura simultánea o claim sin CAS readback |
| INV12 | A5 documenta autor y revisor TASK_ID diferentes | Autoaprobación o rol no permitido |
| INV13 | CI requerida por tecnología coincide con SHA y concluye SUCCESS | SKIPPED, CANCELLED, QUEUED o IN_PROGRESS contados PASS |
| INV14 | Cambios de UI no modifican secretos, esquema o roles de negocio | Auditoría documental abre una migración real |
| INV15 | BITACORA agrega eventos con claves únicas | Corrida reescribe una fila histórica o duplica cierre |
| INV16 | M02 no implica liberación de M03, QA/main/PROD | Gate SI documental interpretado como permiso productivo |

## 4. Contratos de aceptación y trazabilidad a las fuentes

| Contrato | Oráculo positivo FUTURO | Oráculo negativo FUTURO | Antecedente |
| --- | --- | --- | --- |
| AT01 | Ruta pública aislada sin auth privada | Visitante lee recursos tenant por otro contexto | M01.01 NR11/12; M01.05 AC01/14 |
| AT02 | Dashboard protegido por backend y permiso | URL o guard Angular burlado concede API | M01.05 AC02 |
| AT03 | CTA y formularios funcionan con teclado y anuncios | Tab/foco/errores son inaccesibles | M01.05 AC03/05/08 |
| AT04 | Vista 320 px conserva flujos y contenido | Desborde horizontal impide acción primaria | M01.01 NR02; M01.05 AC04 |
| AT05 | Tema y tokens muestran contraste medido | El branding del tenant deja texto ilegible | M01.05 AC12 |
| AT06 | Loading/empty/error contienen alternativas y recuperación | Spinner infinito o estado silencioso | M01.05 AC11 |
| AT07 | Resumen de rendimiento tiene muestra y origen | Métrica referida sin SHA/dispositivo/red | M01.05 AC15 |
| AT08 | Hallazgo reproduce severidad y prueba dirigida | Etiqueta P0/P1 se asigna sin evidencia | M01.04 riesgos; M01.06 B12 |
| AT09 | A5 lo realiza Task Javier distinta del autor | La misma Task firma su propio resultado | M01.06 A5 y AGENTS |
| AT10 | Scope Lock y procedencia SUCCESS del SHA | Check antiguo o skipped se atribuye PASS | M01.05 AC16 |

La tabla no acredita ejecución de AT01–AT10, WCAG, análisis visual, mediciones web, integración de MySQL ni pruebas multiempresa reales. Requiere una corrida posterior con fixture autorizado, semántica del resultado y evidencia reproducible antes de inferir cobertura.

## 5. Dependencias, interfaces y rollback

**Alcance de M02.01:** exclusivamente modelo conceptual y ownership. No modifica contratos API/DTO existentes (M02.02), persistencia/índices/migraciones (M02.03), flags (M02.04), contratos de eventos (M02.05), ni contrato de salida del lote (M02.06). El diseño no introduce nueva arquitectura, servicio o infraestructura. El frontend continúa Angular standalone Signals/Material/CDK; backend mantiene Domain→Application→Infrastructure, MySQL con EF Core y JWT/RBAC como autoridad. Los usuarios públicos no obtienen acceso por figurar en una tabla de evaluación.

**Riesgos:** confundir la evidencia propuesta con una tabla persistida, asignar owner cross-tenant sin API autorizada, introducir un permiso nuevo en UI, inferir métricas no tomadas, declarar A5 sin independencia. **Contención:** registrar hipótesis, no desplegar ni modificar runtime; si falla Scope Lock o procedencia CHANGELOG, mantener VALIDANDO y corregir con commit causal, nunca desactivar checks ni forzar la rama.

**Rollback documental:** `git revert` convencional sobre un HEAD reconciliado, revisión del diff y evento aditivo; no reset/force-push ni edición de BITACORA. Se reserva A5 independiente de otra Task Javier sobre el SHA publicado.

## 6. A0–A5 de esta entrega

- **A0:** fuentes oficiales, cinco antecedentes más M01.06 A5 independiente, modelos UX01–UX12, invariantes INV01–INV16 y contratos AT01–AT10.
- **A1:** validación estática de estructura y oráculos positivo/negativo definidos; casos de navegador siguen `FUTUROS_NO_EJECUTADOS`.
- **A2:** no alteración de JWT/RBAC, propiedad tenant A/B, roles backend, datos, secretos ni entorno; seguridad conceptual, no penetración certificada.
- **A3:** trazabilidad documental M01.01–06, PLAN/COLA/DAG y referencias a código existente; HTTP/MySQL/E2E no aplican al diff Markdown, nunca PASS inventado.
- **A4:** dos archivos Markdown, CHANGELOG con sección indivisible, publicación fast-forward por CAS parent exacto, checks de Scope Lock y V5 del SHA publicado `completed/success`, readback PLAN/COLA/BITACORA.
- **A5:** `PENDIENTE` hasta revisión por otra Task Javier distinta de `6af16bb9492c8191b8e8c7d606f11507`; ningún pase de dev supone aprobación a QA/main/PROD.
