# SOLQARYN V5 — C09-02.M01.05: criterios de aceptación observable para fidelización y reputación

**Microtarea:** C09-02.M01.05 · PLAN_MAESTRO fila 1338 · **Autor:** PRIMARY DEV 04 JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09` · **Fecha:** 2026-11-14 · `solqaryn/Solqaryn@dev` · `PROJECT_SCOPE_LOCK=STRICT`.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`). Esta unidad M01.05 describe condiciones **observables FUTURAS_NO_EJECUTADAS** de aceptación funcional, negativa y no funcional. No instala programas de puntos, campañas, reseñas, integración, worker o modificaciones de datos.

## Cadena de evidencia y alcance

- C09-02.M01.01: `docs/v5/c09-02-m01-01-alcance-fidelizacion-reputacion.md` (blob `1030c70296220626c5341f58d99e68e47b8a6cb2`), no-regresión NR01–NR14.
- C09-02.M01.02: `docs/v5/c09-02-m01-02-actores-interfaces-fidelizacion.md` (blob `7de37b4ccfa35cd07245975b97cf04dcca2b59a7`), A01–A14, I01–I12 y fronteras F01–F09.
- C09-02.M01.03: `docs/v5/c09-02-m01-03-inventario-codigo-brechas-fidelizacion.md` (blob `b5bbb9d2c8e85814390bd12c85e5f4d6bd3bfed7`), 12 fuentes Git y G01–G12; no confirma runtime.
- C09-02.M01.04: `docs/v5/c09-02-m01-04-riesgos-supuestos-decisiones-fidelizacion.md` (blob `fb8079c397ad3e413d4a52c9f848744fff16bd3c`), R01–R14, S01–S08 y D01–D08. **Supuestos y decisiones pendientes no se elevan a hechos.**

## AC01–AC16: matriz positiva/negativa de aceptación FUTURA_NO_EJECUTADA

| ID | Positivo que deberá demostrar la implementación | Negativo obligatorio (fail-closed) | Capa/gate esperado |
| --- | --- | --- | --- |
| AC01 | GET de saldo propio con usuario A/tenant A y RBAC permitido devuelve saldo y movimientos consistentes | Sesión B o ID Cliente B devuelve 403/404 sin detalle de saldo | JWT y tenant A/B |
| AC02 | Venta A confirmada y elegible produce una acreditación con referencia única | Replay de confirmación/retry no duplica ni cambia saldo | Idempotencia |
| AC03 | Venta anulada/reversada no produce beneficio neto indebido; compensación trazada | Evento anulado o falso no genera puntos | Transacciones |
| AC04 | Canje A aceptado produce débito atómico y comprobante de correlación | Dos canjes simultáneos no gastan doble; saldo insuficiente rechaza | MySQL concurrencia |
| AC05 | Política de promoción allowlisted y versionada se valida en tenant A | Promoción B, expirada o clave arbitraria rechazada | DTO seguros |
| AC06 | Concesión manual con permiso específico queda auditada sin afectar crédito | Rol lector, B o sesión revocada no ajustan saldo | RBAC negocio |
| AC07 | Reseña auténtica del cliente A se presenta saneada con estado de moderación | HTML/script, PII secreta o reseña forjada no se expone | XSS/privacidad |
| AC08 | Moderador A autorizado registra decisión reversible con actor y motivo | Rol lector, B o ausencia de motivo no cambia estado | Moderación |
| AC09 | Apelación válida restaura historial y versión de reseña con registro auditable | Eliminación dura o alteración histórica sin motivo falla | Auditoría |
| AC10 | Respuesta empresa A a reseña aprobada preserva asociación por tenant | Respuesta de B o HTML arbitrario es rechazada | Contenido |
| AC11 | Caso posventa A enlaza Cliente y Venta A con controles por recurso | ID venta/ticket B no filtra información ni permite cierre | Ownership |
| AC12 | Opt-in vigente para canal específico permite sólo un envío autorizado | Opt-out o canal distinto bloquea envío incluso por retry | Consentimiento |
| AC13 | Listado de incentivos usa paginación y límites tenant-scoped | Página no autorizada, filtro no allowlisted o caché B no expone A | Consultas/cache |
| AC14 | Error temporal de DB/proveedor presenta estado seguro y no concede puntos | Timeout, reloj incorrecto o fallbacks no activan beneficio | Fail-closed |
| AC15 | Deshabilitar módulo mantiene ventas, crédito e historial de incentivos intactos | Hard delete o cambio contable no previsto falla la prueba | Rollback |
| AC16 | UI futura accesible provee foco, teclado, responsive 320px y errores legibles | Saltar guard Angular no otorga autorización API | A11y + backend |

**Resultado:** AC01–AC16 son objetivos de prueba futuros. P01–P08/N01–N08 de M01.01 se amplían aquí, sin alterar su naturaleza **FUTUROS_NO_EJECUTADOS**. La matriz no certifica seguridad runtime, envío, moneda, consentimiento legal ni moderación real. Cada AC requiere evidencia de CI causal y SHA exacto de su futura implementación; los negativos no se sustituyen con pruebas de texto.

## RNF01–RNF10: requisitos no funcionales verificables

| ID | Requisito futuro | Oráculo/evidencia exigible |
| --- | --- | --- |
| RNF01 | Latencia de lectura y paginación con umbrales definidos por medición real | Profile + presupuesto antes de salida; sin afirmar métricas |
| RNF02 | Idempotencia por (tenant, event_id) y replay determinista | Prueba concurrente duplicada en MySQL efímero |
| RNF03 | Almacenamiento seguro del saldo, correcciones por movimientos no UPDATE oculto | Check de ledger y reconciliación de totales |
| RNF04 | Auditoría con correlación y sin secretos, PII mínima, retención autorizada | Test log redaction y readback |
| RNF05 | Controles JWT/RBAC/ownership en API independientes de UI | 401/403/404 con A/B y sesión revocada |
| RNF06 | Precisión de tiempo UTC y expiración tipada, sin habilitar por desfase | Pruebas frontera reloj y vigencia |
| RNF07 | Limitación de uso, rate limit y mitigación de scraping de saldo/reseñas | Pruebas de carga/abuso y denegación |
| RNF08 | Accesibilidad WCAG pertinente y motion reducido sin peso excesivo | Teclado/foco/320px Playwright y build Angular |
| RNF09 | Reversibilidad de módulos sin pérdida de venta o crédito histórico | Rollback probado y checksum datos |
| RNF10 | No dependencias nuevas de proveedores, webhooks o pagos sin permiso | Diff de configuración/secretos y Scope Lock |

No se asignan falsos umbrales de latencia, SLAs ni cuotas: se deben fijar con mediciones, presupuesto e impacto aprobados. Un escenario diseñado pero sin entorno de prueba no es PASS. Tests `skipped`, `cancelled` o `in_progress` nunca son éxito cuando sean pertinentes.

## Estrategia causal de pruebas por tecnología (futura)

| Capa | Evidencia positiva/negativa exigida | Reversión |
| --- | --- | --- |
| Domain/Application .NET 10 | xUnit para puntuación tipada, idempotencia, vencimiento, canje y reversión | Cambios aislados y tests de no-regresión |
| API/JWT/RBAC | HTTP autenticado, inválido, expirado, revocado, tenant A/B y permisos por recurso | Desactivar endpoints/config sin otorgar autoridad |
| EF Core 10 + MySQL 8.4 | Servicio efímero en GitHub Actions; 64+ operaciones concurrentes como caso orientativo, transaction/unique index y checksum | Migración reversible autorizada, preservar ventas |
| Angular 22.2.1 | npm build/test + Playwright de teclado, 320px, focus y errores; bypass guard no elude API | Feature flag seguro y rollback UI |
| Servicios externos (si se aprueban en el futuro) | Consentimiento vigente, idempotencia, retries acotados y fallos de proveedor | Desactivación reversible, sin registros perdidos |
| Gobierno de publicación | Scope Lock, diff, secretos, CHANGELOG y exact SHA; A5 independiente | Commit revert ordinario preservando historia |

**Frontera de autoridad:** el navegador no selecciona tenant ni permisos con un `EmpresaId`, slug, host, cabecera o cookie. `Cliente.EmpresaId=NULL` / `Venta.EmpresaId=NULL` no equivale a acceso global. Puntos no son `CreditoCliente`, facturación ni dinero; las decisiones D01–D08 siguen pendientes.

## Auditoría documental presente y rollback

**A0 (estática):** comprobar 4 antecedentes y sus blobs, correspondencia AC01–AC16 / RNF01–RNF10, evitar secretos y gates falsos. **A1 (funcional documental):** presencia de positivos/negativos completos como especificación, **no** ejecutar endpoints. **A2 (seguridad documental):** rastrear tenant A/B, JWT/RBAC, integridad, consentimiento y denegación por defecto. **A3 (integración documental):** coherencia con C09-02.M01.01–04 y arquitectura sin modificar servicios. **A4:** dos archivos Markdown, fast-forward de dev, Scope Lock y procedencia CHANGELOG exact SHA. **A5:** otra Task Javier distinta del autor, no autoaprobación.

**Rollback:** revertir sólo los cambios de este commit de documentación con un commit inverso normal y HEAD actualizado; BITACORA histórica no se reescribe. Ningún QA, main, PROD, compra, DNS, Cloudflare R2, dato vivo, permiso JWT/RBAC, worker, webhook o evento entre Tasks queda autorizado por este documento. Los RNF y los AC siguen **NO_EJECUTADOS** hasta pruebas y evidencias futuras.
