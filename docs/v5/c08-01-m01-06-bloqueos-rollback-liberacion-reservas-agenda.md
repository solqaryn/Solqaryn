# SOLQARYN V5 — C08-01.M01.06: bloqueos, rollback y contrato de liberación de reservas y agenda

**ID:** C08-01.M01.06; **PLAN_MAESTRO:** fila 1159; **autor:** PRIMARY DEV 06 — JAVIER (`6af16bbad26881918c283abe06e1f32a`); **proyecto:** solqaryn/Solqaryn@dev; **PROJECT_SCOPE_LOCK=STRICT**; **fecha:** 2026-11-15. **Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`).

## Resultado y exclusiones

Cerrar **sólo el contrato documental de preparación M01** para futuras citas, horarios, disponibilidad, profesionales, recursos y cancelaciones. El resultado exigido de etapa (conflicto de horario, zona horaria y autorización) sigue **SIN_PRUEBA_RUNTIME**. Esta unidad R0 no crea reservas, tablas, índices, migraciones, API, componentes Angular, trabajadores, eventos, colas, pagos, proveedores de calendario o cambios de seguridad. Un contrato documental no sustituye pruebas MySQL 8.4, JWT/RBAC/tenant A/B, API HTTP ni Playwright cuando se implemente M03/M05.

Las predecesoras M01.01–05 se encuentran LISTO y con A5 independiente acreditado en la fuente operativa antes del claim. La revisión A5 de **esta** M01.06 continúa siendo independiente y necesaria para habilitar M02 por DAG; LISTO_DEV A0–A4 no implica A5_PASS. La matriz R16/S08/D08 de M01.04 y AC12/NFR08 de M01.05 define casos **FUTUROS_NO_EJECUTADOS**, nunca resultados aprobados.

| ID | Evidencia documental previa | Archivo |
| --- | --- | --- |
| C08-01.M01.01 | NR01–NR12 y límites de no-regresión | `docs/v5/c08-01-m01-01-alcance-reservas-agenda.md` |
| C08-01.M01.02 | A01–A14, I01–I14 y fronteras F01–F07 | `docs/v5/c08-01-m01-02-actores-interfaces-reservas-agenda.md` |
| C08-01.M01.03 | Inventario de fuentes y brechas G01–G10 | `docs/v5/c08-01-m01-03-inventario-fuentes-brechas-reservas-agenda.md` |
| C08-01.M01.04 | R01–R16, S01–S08 y decisiones D01–D08 no resueltas | `docs/v5/c08-01-m01-04-riesgos-supuestos-decisiones-reservas-agenda.md` |
| C08-01.M01.05 | AC01–AC12 y NFR01–NFR08 con positivos/negativos futuros | `docs/v5/c08-01-m01-05-criterios-aceptacion-reservas-agenda.md` |

## Dieciséis gates de liberación y evidencia

| Gate | Condición comprobable para diseño/implementación futura | Bloqueo si falta |
| --- | --- | --- |
| G01 | Repositorio/rama/SHA/Scope Lock STRICT y Task Javier allowlist verificados | No reclamar ni publicar |
| G02 | M01.01–05 LISTO y A5 independientes, sin conflictos de autoría | No liberar M01.06 |
| G03 | PLAN H=PENDIENTE/I vacío y COLA G/O=SI, DAG F=VALIDADO/G=SI antes de CAS | No forzar gate ni doble owner |
| G04 | Contrato de cita **distinto** de `ReservaInventario` y no regresión ERP | No inferir reutilización de stock |
| G05 | Contexto Empresa/sucursal/actor se obtiene del servidor, nunca del payload o host no confiable | Denegar IDOR/BOLA/tenant cruzado |
| G06 | Modelo de recursos, personal, duración y buffers con propiedad y capacidad versionadas | No confirmar slots de capacidad incierta |
| G07 | Exclusión transaccional MySQL de intervalos superpuestos, con test real de 64 solicitudes | Un lock en proceso o mock no acredita concurrencia |
| G08 | Hora UTC, zona IANA de sede, ambigüedad DST y offsets explícitos diseñados | No aceptar hora local ambigua sin regla |
| G09 | JWT/RBAC, membresía activa y revocación comprobados por operación | No depender de Angular/UI como autoridad |
| G10 | Alta/reprogramación/cancelación idempotentes con claves por tenant/actor/payload y versión | Replay cruzado o duplicado NO_GO |
| G11 | Estados terminales, auditoría sin PII y reversión transaccional de error intermedio | No liberar reserva previa sin nueva confirmación |
| G12 | Proyección pública mínima, rate limiting y sanitización de logs, JWT, teléfono y detalles | No exponer horarios privados o PII |
| G13 | D01–D08 de M01.04 requieren decisiones causales previas, sin asignar defaults inseguros | No activar flujos, proveedores o cobros |
| G14 | Pruebas de integración .NET/API, MySQL 8.4 EFÍMERO, Angular/Playwright y regresión de inventario cuando procedan | No certificar runtime desde MD/mock/build |
| G15 | Publicación de este contrato: 2 Markdown, sección CHANGELOG autocontenida, Scope Lock/V5 exact SHA completed/success, BITACORA A:Z y readback | En cola/CI incompleta mantener VALIDANDO |
| G16 | Revisor A5 independiente de otra Task Javier examina requisito/diff/tests/seguridad antes de M02 | LISTO_DEV solo no abre sucesora técnica |

**Condición de salida de esta unidad:** documentos + A0–A4 R0 + checks exactos y readback; la aprobación A5 del autor es inválida. Para M02 se exige A5_PASS ajeno, nueva lectura de PLAN, COLA y DAG por cada ID y permisos actuales. Para M03, persistencia, API, migraciones o producción son exigibles gates separados: jamás se desbloquean por VALIDANDO ni por documentación preparatoria.

## Diez pares de aceptación futura (no ejecutados)

| Oráculo | Positivo reproducible requerido | Negativo obligatorio |
| --- | --- | --- |
| P01/N01 | Empresa A lista solo disponibilidad pública propia | Host/ID/token de B no filtra calendario de B |
| P02/N02 | Profesional activo puede consultar su agenda autorizada | Revocación, rol sin permiso y sucursal ajena fallan |
| P03/N03 | Reserva confirma un solo slot válido bajo concurrencia | 64 reservas simultáneas no generan doble ocupación |
| P04/N04 | Turno, feriado, ausencia y buffers cierran franjas correctamente | Tiempo fuera de turno y duración manipulada se rechazan |
| P05/N05 | UTC/IANA reconstruyen instante de sede con offset válido | DST duplicado/inexistente sin desambiguar no confirma |
| P06/N06 | Repetir misma clave/payload devuelve una sola cita | Misma clave con payload/tenant distinto no muta |
| P07/N07 | Reprogramar en una transacción cambia ocupación y versión | Fallo intermedio conserva slot anterior y estado original |
| P08/N08 | Cancelación válida libera capacidad una sola vez | Estado terminal/version obsoleta no libera doble |
| P09/N09 | UI accesible 320px/teclado refleja confirmación servidor | Timeout no muestra confirmación optimista, no pierde foco |
| P10/N10 | Citas no alteran inventario/facturación/stock | `ReservaInventario` conserva saldos y invariantes con replay |

Todos son **FUTUROS_NO_EJECUTADOS**. El resultado de este Markdown no permite reportar 10/10 PASS runtime. Cada implementación R2 necesitará asserts positivos, negativos, frontera, error/rollback y seguridad real; A3 de MySQL requiere job CI con Category=Integration y MySQL efímero, no sólo build; UI real requerirá Playwright/Chromium y accesibilidad. No usar QA/main/PROD, proveedores vivos ni datos productivos.

## Reversión secuencial y seguridad

1. Registrar ID, SOURCE_SHA, HEAD, autor verdadero, diff, pruebas aplicables, actor, estado y EVENT_ID idempotente antes de reparar o revertir.
2. Bloquear operación afectada si se demuestra doble ocupación, aislamiento tenant A/B roto, autorización permisiva o PII expuesta; P0/P1 no certificables.
3. No cancelar citas legítimas, saldos o reservas de inventario por corregir documentación; no modificar datos de terceros.
4. La reversión de **este documento** usa un `git revert <COMMIT_SHA>` normal en dev, con comprobación CAS y sin force-push ni reescritura de BITACORA.
5. Migraciones futuras necesitarán plan de backup y rollback propio sobre MySQL efímero; no aplicar migraciones en este changeset.
6. Un cambio runtime futuro debe recuperar funcionalidad anterior tras revert y validar estados, pertenencias, índices y autoría en CI dirigida.
7. Nunca reactivar Tasks retiradas, activar workers, disparar eventos, comprar proveedor ni tocar secretos/hosts sin permiso separado.
8. Escribir evento compensatorio y readback PLAN/COLA/DAG/BITACORA tras reversión; no borrar A5_FAIL ni permitir gate SI ficticio.

## Evidencia de calidad y handoff A0–A5

- **A0:** ID y alcance C08-01.M01.06, 16 gates y 10 pares futuro, cinco predecesoras acreditadas, rutas documentales y rollback, un solo owner EN_PROGRESO.
- **A1:** correspondencia entre NR12/A14/F07/G10, R16/S08/D08 y AC12/NFR08; escenarios **especificados**, no tests ejecutados.
- **A2:** análisis documental fail-closed sobre tenant A/B, JWT/RBAC/revocación, IDOR, UTC/IANA/DST, PII y concurrencia; ninguna policy real modificada.
- **A3:** verificación de documentos/contratos y las fronteras de código previamente inventariadas; HTTP, MySQL, EF, Angular/Playwright, calendario externo y stock operativo **NO_APLICA** al diff R0.
- **A4:** commit directo por fast-forward/CAS solo en dev; Scope Lock y V5 procedencia en status completed/success sobre exact SHA, BITACORA idempotente, lectura institucional post-cierre.
- **A5:** queda PENDIENTE para otra Task Javier de diferente TASK_ID, revisión independiente de requisitos/diff/seguridad y falsabilidad de pruebas; sin autoaprobación ni permisos de QA/main/PROD.

Esta entrega no certifica una reserva real, motor de horarios, disponibilidad online, cancelación ni integración; no añade endpoints, bases, pagos, emails, migraciones, servicios, schedulers, eventos o secretos. El cierre documental M01 no es cierre del capítulo.
