# SOLQARYN V16 — C08-01.M02.01: entidades, ownership e invariantes de citas y agenda

**Autor:** PRIMARY DEV 02 JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Rama:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. **V15:** R0 documental, cero cambios runtime.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN_MAESTRO/COLA/DAG_TECNICO. Resultado M02.01: **definir entidades, ownership, relaciones e invariantes** para citas, horarios, disponibilidad, personal, recursos y cancelaciones. Esta especificación no crea modelos EF, DbSets, SQL, endpoints, UI, calendarios reales ni migraciones.

## 1. Gates vigentes y fronteras

Preflight PLAN H1160=PENDIENTE/I1160 vacío; COLA G/O=SI, DAG F1121=VALIDADO/G1121=SI. La única Task propietaria de esta microtarea es DEV02 tras claim CAS con readback. M01.01–M01.06 definen alcance, actores, brechas, riesgos R01–R16/S01–S08, decisiones D01–D08 pendientes, AC01–AC12 y NFR01–NFR08 como criterios futuros. El contrato terminal M01.06 establece G01–G16: no reutilizar `ReservaInventario` para citas, no confirmar disponibilidad sin transacción real, respetar zona IANA de sede, evitar double-booking y exigir revisión A5 independiente.

Arquitectura: Domain <- Application <- Infrastructure y API; .NET 10, Angular 22.2.1, EF Core 10.0.12/MySQL Oracle provider 10.0.9. `EmpresaId` y permisos se validan en backend mediante membresía, sucursal, recurso y estado; host público, UI y payload no crean pertenencia ni roles. Horarios públicos exponen solo ranuras mínimas, nunca datos personales de clientes ni agendas privadas. Las entidades enumeradas son **candidatas conceptuales, no clases existentes certificadas**.

## 2. Entidades y relaciones conceptuales E01–E14

| ID | Concepto | Propiedad y relación exigidas |
| --- | --- | --- |
| E01 | Empresa | tenant propietario server-side, sesión y membresía activas |
| E02 | Sucursal | 1:N por empresa, timezone IANA verificada |
| E03 | ServicioAgendable | duración y buffer explícitos, scope sucursal |
| E04 | Profesional | profesional habilitado para el servicio y sucursal |
| E05 | Recurso | cabina, equipo o sala con capacidad/versionado |
| E06 | AgendaProfesional | franjas laborales y excepciones por fecha |
| E07 | AgendaRecurso | límites de ocupación por recurso y sede |
| E08 | DisponibilidadCalculada | proyección, no autorización ni compromiso de cita |
| E09 | Cita | estado, versión CAS, cliente scoped y UTC de inicio/fin |
| E10 | ParticipacionRecurso | ocupación de 1..N recursos con exclusión temporal |
| E11 | BloqueoHorario | feriado, ausencia, mantenimiento, buffer y excepciones |
| E12 | Reprogramacion | transición única de slot previo al nuevo, atómica |
| E13 | Cancelacion | motivo codificado, plazo y política vigente |
| E14 | AuditoriaCita | actor, origen, correlación y resultado sin PII/secretos |

La cardinalidad se revisará contra el modelo real antes de cualquier EF/SQL. No se permite global fallback cuando EmpresaId/SucursalId son NULL. `Cita` y `ReservaInventario` son dominios diferentes, no comparten estado ni saldos.

## 3. Invariantes contractuales INV01–INV18

| ID | Condición que una futura implementación deberá demostrar |
| --- | --- |
| INV01 | tenant/empresa deriva de identidad backend verificada |
| INV02 | sucursal, profesional y recursos pertenecen al mismo tenant |
| INV03 | JWT/RBAC se valida por acción y recurso, incluso en rutas públicas |
| INV04 | usuario, rol, sesión o membresía revocados no reservan ni cancelan |
| INV05 | duración y buffers son positivos, acotados y versionados |
| INV06 | UTC representa instante inequívoco y zona IANA de sede conserva reglas locales |
| INV07 | hora local duplicada por DST requiere offset/selección inequívoca |
| INV08 | hora local inexistente por DST no produce reserva confirmada |
| INV09 | disponibilidad calculada no equivale a compromiso de un slot |
| INV10 | 64 intentos concurrentes sobre mismo recurso no confirman doble ocupación |
| INV11 | recursos compartidos y profesionales deben estar libres en toda la franja |
| INV12 | alta, reprogramación y cancelación usan idempotencia con fingerprint |
| INV13 | mismo key con payload diferente o tenant distinto no reutiliza resultado |
| INV14 | reprogramación fallida conserva cita original sin liberar capacidad anticipada |
| INV15 | cancelación/expiración libera capacidad como máximo una vez |
| INV16 | estado terminal o versión obsoleta rechaza transición |
| INV17 | disponibilidad pública no revela nombre, teléfono ni agenda privada |
| INV18 | error/timeout no marca cita confirmada en UI ni crea mutaciones parciales |

**INV01–INV18 son requisitos futuros, no afirmaciones de implementación.** La exclusión temporal requiere CI MySQL 8.4 real y prueba de 64 solicitudes; un lock de memoria, mock, build o simulación no demuestra serialización transaccional.

## 4. Relaciones y reglas de cardinalidad REL01–REL12

| ID | Relación lógica y autorización |
| --- | --- |
| REL01 | Empresa 1:N Sucursal con pertenencia verificada |
| REL02 | Sucursal 1:N ServicioAgendable con duración |
| REL03 | ServicioAgendable N:M Profesional mediante capacidad aprobada |
| REL04 | Sucursal 1:N Recurso con estado y capacidad |
| REL05 | Profesional 1:N AgendaProfesional |
| REL06 | Recurso 1:N AgendaRecurso |
| REL07 | Agenda 1:N BloqueoHorario sin degradación de disponibilidad |
| REL08 | Cita N:1 Sucursal; tenant no cruzado |
| REL09 | Cita N:1 Profesional habilitado |
| REL10 | Cita N:M Recurso con bloqueo transaccional |
| REL11 | Cita 1:N Reprogramacion con versión CAS |
| REL12 | Cita 0..1 Cancelacion terminal con auditoría |

No se asume que existan tablas, PK/FK, índices, constraints o una matriz de permisos con estos nombres. Las relaciones son candidatas para revisar luego contra clases existentes, `AppDbContext`, snapshot de migraciones y consumidores Angular.

## 5. Doce pares de aceptación futuros T01–T12 (NO_EJECUTADOS)

| ID | Positivo futuro | Negativo/fallo que deberá rechazarse |
| --- | --- | --- |
| T01 | empresa A lista su servicio y sede | B no enumera recursos de A |
| T02 | profesional activo recibe cita autorizada | rol/membresía revocado no muta |
| T03 | agenda de recurso libre confirma una sola cita | 64 solicitudes en carrera no duplican slot |
| T04 | cita dentro de horario y buffers pasa | sobrepasar turno o ausencia se rechaza |
| T05 | UTC e IANA representan instante conocido | DST ambiguo/inexistente sin offset falla |
| T06 | idempotency key repetida da mismo resultado | fingerprint divergente o scope B falla |
| T07 | reprogramación comprometida cambia slot atomizado | fallo intermedio conserva cita y ocupación previas |
| T08 | cancelación válida libera una sola vez | segunda cancelación no altera capacidad |
| T09 | capacidad de varios recursos se bloquea junta | uno ocupado aborta todo |
| T10 | disponibilidad pública mínima sin PII | acceso no autorizado no lee agenda privada |
| T11 | timeout conserva estado desconocido hasta confirmación | UI no presenta éxito optimista |
| T12 | reservar no altera saldos del ERP | cita/replay no muta `ReservaInventario` |

**Pruebas ejecutadas en esta microtarea:** 0, cobertura/runtime NO_MEDIDO. Implementaciones posteriores R2 requieren xUnit positivos/negativos/frontera, JWT/RBAC/tenant A/B, MySQL 8.4 efímero con `Category=Integration`, concurrencia 64 solicitudes y rollback; si cambian UI, Angular build y Playwright/Chromium accesibilidad 320px/teclado. No confundir criterios escritos con tests PASS.

## 6. Calidad, dependencias y rollback

- **A0:** contrato E01–E14/INV01–INV18/REL01–REL12/T01–T12, M01.06 G01–G16, alcance de dos Markdown, salida sin código.
- **A1:** escenarios positivos, negativos, DST, concurrencia, replay, error y timeout especificados, sin ejecución runtime.
- **A2:** JWT/RBAC, revocación, IDOR/BOLA, tenant A/B, PII y capacidad fail-closed solo documentados.
- **A3:** lectura de fuente M01 y arquitectura; MySQL, API, Angular, Playwright y pruebas live NO_APLICA por diff R0 exclusivo Markdown.
- **A4:** un commit de este documento y CHANGELOG autocontenido en dev, fast-forward/CAS, Scope Lock y procedencia V5 `completed/success` en SHA exacto, estados/bitácora/readback.
- **A5:** revisión independiente de otro TASK_ID Javier, no de DEV02; solo LISTO+A5 y prerequisitos DAG permiten M02.06/M03.

**Rollback R0:** `git revert` normal del SHA tras releer HEAD y validar CAS; preservar BITACORA, no tocar citas reales, MySQL, reservas de existencias, credenciales, proveedores ni QA/main/PROD. D01–D08 siguen pendientes y no se habilita calendar provider, worker ni webhook.
