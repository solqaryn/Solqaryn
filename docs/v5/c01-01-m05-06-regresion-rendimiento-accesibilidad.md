# SOLQARYN V5 — C01-01.M05.06: regresión reproducible, costo acotado y accesibilidad

**Scope:** C01-01.M05.06, PLAN_MAESTRO fila 175, DAG_TECNICO fila 195. Autor PRIMARY DEV 03 `6adc2e4bd8a48191ac7d8641ed95a1a3`, repo `solqaryn/Solqaryn@dev`, Scope Lock STRICT. Autoridad: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL.

## Casos reales y límites de la evidencia
- **R01:** 128 resoluciones alternadas tenant A/B y roles distintos sin mezclar contexto; exactamente 128 lecturas de la fuente confiable (presupuesto estructural de I/O, **no** latencia p95 ni benchmark sintético).
- **R02:** 64 solicitudes cross-tenant denegadas antes de I/O, con cero lecturas de fuente.
- **R03:** cancelación del token antes de consultar fuente, cero I/O.
- **R04:** membresía revocada después de éxito invalida la siguiente resolución y exige relectura.
- **R05:** fuente devuelve sucursal no pedida y backend deniega adquisición implícita.
- **A11Y01:** en Chromium local 390 px + prefers-reduced-motion, foco por teclado, labels de login y ausencia de overflow horizontal.
- **A11Y02:** ruta privada /usuarios redirige anónimo a login etiquetado y no monta app-usuarios.

Las pruebas backend invocan **código real** `IdentidadUniversalApplicationService` y reglas Domain de SOLQARYN .NET 10, con snapshot source en memoria y sin secretos/DB. Los escenarios browser usan Angular local en GitHub-hosted runner sin servicio productivo. Ambos son pruebas dirigidas, **no** certificación global WCAG/penetración/API HTTP/JWT real/MySQL/EF ni métricas de rendimiento de producción.

## Arquitectura, impacto y no-regresión
Cambios solo en nuevas pruebas backend, nueva especificación Playwright, workflow CI dirigido y este documento/CHANGELOG. No cambian negocio, Domain/Application/Infrastructure/API runtime, JWT, RBAC, tenancy, routes de producción, datos, migraciones ni tablas. La interfaz sigue siendo presentación; nunca otorga permisos por header/slug/empresa elegida. CI backend existente y `V5 C01-01.M05.06` deben finalizar `completed/success` en SHA causal; una ejecución `queued/in_progress/failed/cancelled/skipped` no es PASS de prueba requerida.

**Preflight:** C01-01.M04.01–06+A5, C01-01.M05.01–05+A5, C00-03.M06.06+A5; PLAN H=PENDIENTE, I libre; COLA G/O=SI; DAG F=VALIDADO/G=SI. Esta microtarea se reclama individualmente, sin tocar owners ajenos.

## Cierre A0–A5 y rollback
- A0: review de scope, Skills/HEAD, dependencias, exclusivo H/I, diff y pruebas.
- A1: R01–R05 y A11Y01–02 ejecutados con resultados GitHub Actions; no inferir PASS antes de CI.
- A2: aislamiento empresas A/B, revocación, falta de sucursal, ruta anónima y no adquisición de permisos en cliente.
- A3: integración Application/Domain y runner Playwright/Angular local, sin DB productiva.
- A4: publicación fast-forward con expected HEAD, checks `verify-solqaryn-project-scope`, procedencia V5, backend, seguridad, y browser dirigido; readbacks PLAN/COLA/BITACORA.
- A5: queda pendiente revisión independiente por otra de las seis Tasks Javier, nunca por la autora.

Rollback con `git revert` normal y CI dirigida de nuevo SHA; no force-push, QA/main/PROD, servicios pagados ni alteraciones de proveedores.
