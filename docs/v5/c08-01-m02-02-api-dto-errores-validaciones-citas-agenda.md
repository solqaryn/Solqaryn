# SOLQARYN V16 — C08-01.M02.02: API, DTO, errores, validaciones y compatibilidad de agenda

**Microtarea:** C08-01.M02.02, PLAN_MAESTRO fila 1161. **Autor:** PRIMARY DEV 02 JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Riesgo V15:** R0 documental. **Repo/rama:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT.

**Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN/COLA/DAG. Resultado exigido: definir **contratos futuros** de API/DTO, errores seguros, validaciones y compatibilidad de citas, horarios, disponibilidad, personal, recursos y cancelaciones. No se crean endpoints, rutas, modelos C#, código Angular, datos de pacientes/clientes, migraciones ni integraciones de calendarios.

## 1. Contexto y barreras

`C08-01.M01.06` enumera G01–G16 (identidad, pertenencia, exclusión MySQL, UTC/IANA/DST, RBAC, idempotencia, redacción de PII y A5) y mantiene D01–D08 pendientes. `C08-01.M02.01` define E01–E14, INV01–INV18, REL01–REL12 y escenarios T01–T12 como requisitos **no ejecutados**. La API futura no puede mezclar cita de agenda con `ReservaInventario`: los saldos ERP no se modifican ni comparten tablas/estado.

Autorización siempre backend por empresa, sucursal, servicio, profesional, recursos y rol vigente. Disponibilidad proyectada no concede permiso para confirmar; la operación de confirmación debe validar estado y obtener aislamiento transaccional real. Usar UTC + zona IANA de la sucursal, y offset inequívoco cuando DST tenga hora duplicada; horario inexistente se rechaza. Ningún header, host, JWT sin verificación o tenant enviado en DTO concede acceso. Documentar solo semántica; nombres OP/DTO/ERR pueden requerir adaptación después de inspeccionar rutas existentes antes de alterar compatibilidad.

## 2. Operaciones contractuales futuras OP01–OP12 (no rutas activas)

| ID | Operación semántica | Restricción y resultado |
| --- | --- | --- |
| OP01 | ConsultarServiciosAgendables | solo empresa/sucursal autorizada y servicios visibles |
| OP02 | ConsultarDisponibilidadPublica | mínima, sin PII ni agenda interna |
| OP03 | ConsultarAgendaProfesional | RBAC y profesional del mismo tenant |
| OP04 | ConsultarRecursos | recurso/sucursal y capacidad autorizadas |
| OP05 | CrearCita | fecha/hora/offset válido, recursos libres y transacción |
| OP06 | ObtenerCita | owner, personal autorizado o rol explícito, no enumeración B |
| OP07 | ReprogramarCita | ETag/versión, swap atómico de ocupación |
| OP08 | CancelarCita | estado válido, idempotente, liberación única |
| OP09 | ConsultarAusenciasBloqueos | lectura restringida, sin datos sensibles |
| OP10 | RegistrarBloqueoHorario | permiso agenda, no bypass de citas existentes |
| OP11 | ConsultarPoliticaCancelacion | política vigente de sede/servicio |
| OP12 | ConsultarHistorialCita | auditoría minimizada y retención autorizada |

Ninguna de estas etiquetas es prueba de que exista endpoint. Antes de implementar, mapearlas a contratos HTTP actuales, mantener serialización/versionado, autorización y rollback sin rutas duplicadas.

## 3. DTO conceptuales DTO01–DTO12 (no clases C#)

| ID | Campos candidatos | Validación imprescindible |
| --- | --- | --- |
| DTO01 | ServicioResumen(Id,DuracionMin,BufferMin) | rangos y sucursal |
| DTO02 | SlotDisponible(InicioUtc,FinUtc,ZonaIana,Offset) | instante único |
| DTO03 | AgendaFiltro(ProfesionalId,DesdeUtc,HastaUtc) | rol, límites, paginación |
| DTO04 | RecursoResumen(Id,Capacidad,Estado) | pertenencia/capacidad |
| DTO05 | CrearCitaSolicitud(ServicioId,ProfesionalId,Recursos,Inicio,Clave) | fingerprint y permisos |
| DTO06 | CitaVista(Id,Estado,Version,InicioUtc) | mínima, sin PII de B |
| DTO07 | ReprogramarSolicitud(Id,NuevoInicio,Version,Clave) | CAS y doble intervalo |
| DTO08 | CancelarSolicitud(Id,Version,MotivoCodigo,Clave) | transición terminal |
| DTO09 | BloqueoConsulta(ProfesionalId,RecursoId,Intervalo) | privacidad y scope |
| DTO10 | BloqueoSolicitud(RecursoId,Intervalo,Version) | verificar solapamiento |
| DTO11 | PoliticaCancelacionVista(Plazo,Version) | política de tenant/sucursal |
| DTO12 | HistorialVista(CitaId,Operaciones,Cursor) | autor/correlación, sin secretos |

Identificadores de empresa proporcionados por cliente nunca son autoridad. Las cadenas y fechas deben someterse a límites de tamaño, formato y precisión antes del código R2.

## 4. Errores seguros ERR01–ERR14 y respuestas semánticas futuras

| ID | Error | Comportamiento a probar |
| --- | --- | --- |
| ERR01 | token ausente/inválido | 401 sin detalles de autorización |
| ERR02 | rol o membresía revocada | 403 sin mutación |
| ERR03 | sucursal/servicio/cliente de B | 403/404 sin enumeración |
| ERR04 | profesional o recurso inactivo | 409/422 fail-closed |
| ERR05 | hora DST inexistente | 422 sin cita |
| ERR06 | hora local duplicada sin offset | 422 solicitando desambiguación |
| ERR07 | duración/buffer fuera de rango | 422 |
| ERR08 | slot ya ocupado por concurrencia | 409 sin confirmación |
| ERR09 | recurso bloqueado o profesional ausente | 409/422 |
| ERR10 | clave idempotente reutilizada con distinto fingerprint | 409 |
| ERR11 | ETag/versión obsoleta | 409 sin pérdida del estado original |
| ERR12 | cancelación de estado terminal | 409 o resultado idempotente sin doble efecto |
| ERR13 | límite de rate, tamaño o cuota | 429/413 sin PII |
| ERR14 | timeout/falla transaccional o proveedor | 503 mínimo, estado desconocido hasta reconciliar |

Códigos HTTP son decisiones de contrato futuro, NO aseveraciones de rutas reales. No incluir SQL, números de teléfono ni secretos en errores.

## 5. Validaciones obligatorias futuras VAL01–VAL18

| ID | Regla negativa que debe fallar |
| --- | --- |
| VAL01 | empresa/sucursal backend verificadas |
| VAL02 | actor, membresía, rol y revocación vigentes |
| VAL03 | profesional de empresa A no reservable por B |
| VAL04 | recurso de empresa B no seleccionable por A |
| VAL05 | duración, intervalo y buffers coherentes |
| VAL06 | UTC y zona IANA de sede consistentes |
| VAL07 | DST ambiguo/inexistente sin decisión explícita rechazado |
| VAL08 | turno, feriado, ausencia y mantenimiento respetados |
| VAL09 | ocupación concurrente bloqueada en transacción MySQL |
| VAL10 | disponibilidad proyectada no equivale a reserva |
| VAL11 | idempotencia por tenant, actor, comando y fingerprint |
| VAL12 | reprogramación preserva slot original si falla |
| VAL13 | cancelación libera capacidad máximo una vez |
| VAL14 | versión CAS impide lost update |
| VAL15 | permisos de agenda privada no provienen de UI |
| VAL16 | datos de cliente/PII censurados en lectura pública |
| VAL17 | errores, logs, tokens y correlación sin filtración sensible |
| VAL18 | reserva de agenda nunca modifica saldo ERP/stock |

## 6. Doce pares de pruebas futuras T01–T12 (NO_EJECUTADOS)

| ID | Caso positivo futuro | Caso negativo/rollback futuro |
| --- | --- | --- |
| T01 | servicio de A visible dentro de sucursal A | host B no lista A |
| T02 | slot válido en UTC/IANA expuesto mínimo | DST duplicado sin offset no confirma |
| T03 | personal autorizado ve su agenda | rol revocado no accede |
| T04 | recurso libre participa en cita | recurso de B se rechaza |
| T05 | reserva única de slot disponible se confirma | 64 solicitudes simultáneas no hacen doble booking |
| T06 | crear cita idempotente devuelve mismo resultado | misma clave con otro payload da 409 |
| T07 | reprogramación válida cambia ambos intervalos | error a mitad de operación conserva original |
| T08 | cancelar cita permitida libera una vez | cancelar de nuevo no duplica capacidad |
| T09 | ausencia bloquea horario futuro | reserva sobre feriado no confirma |
| T10 | historia autorizada muestra trazabilidad mínima | cliente B no lee PII de A |
| T11 | 4xx/5xx muestran estado seguro en UI | timeout no pinta éxito optimista |
| T12 | cita normal preserva operaciones ERP | replay no modifica `ReservaInventario` ni kardex |

Se han escrito **12 pares, ejecutados 0, PASS runtime NO_MEDIDO**. Para implementar API R2 se requieren xUnit real, JWT/RBAC/tenant A/B y revocación, 64 intentos de concurrencia y MySQL 8.4 efímero `Category=Integration`; Angular/Playwright si cambia UI. Tests en memoria no certifican locks ni transacciones MySQL.

## 7. Gates y rollback A0–A5

- **A0:** fuente única, G01–G16, E/INV/REL previas y sólo este Markdown+CHANGELOG.
- **A1:** OP12/DTO12/ERR14/VAL18/T12 de aceptación positiva/negativa y error, futuros; ninguna prueba HTTP ejecutada.
- **A2:** seguridad tenant A/B, IDOR, JWT/RBAC/revocación, PII, DST y concurrencia contractuales.
- **A3:** contrato cotejado con precedentes; HTTP, MySQL, .NET, Angular, Playwright y proveedor `NO_APLICA` al diff R0 sin runtime.
- **A4:** fast-forward/CAS dev, SHA exacto con Scope Lock y V5/procedencia `completed/success`, BITACORA A:Z y lectura de estados.
- **A5:** otra Task Javier distinta del autor revisa requisitos, SHA, diff y evidencia; LISTO no implica promoción a QA/main/PROD.

Reversión documental: `git revert` normal con HEAD CAS, conservar BITACORA histórica y registro compensatorio. No cancelar ni crear citas reales, mover datos, ejecutar migrations, activar proveedor, secretos o tareas nuevas. Las decisiones D01–D08 permanecen pendientes.
