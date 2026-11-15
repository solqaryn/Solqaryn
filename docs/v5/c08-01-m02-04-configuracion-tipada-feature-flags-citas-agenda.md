# SOLQARYN V16 — C08-01.M02.04: configuración tipada, ámbitos y flags seguros de citas/agenda

**Microtarea:** C08-01.M02.04 (PLAN_MAESTRO fila 1163). **Autor:** PRIMARY DEV 05 Javier, TASK_ID \`6adc2e55f47c81919f6374e561687076\`. **Proyecto:** solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT. **Clasificación V15:** R0 documental; ningún runtime configurado.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive \`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA\`, y PLAN_MAESTRO, COLA, DAG_TECNICO. Resultado exigido: definir opciones tipadas, ámbitos y feature flags seguros para citas, horarios, disponibilidad, personal, recursos y cancelaciones.

## 1. Contrato, dependencias y límites

- C08-01.M01.06 contiene barreras G01–G16 y decisiones D01–D08 todavía pendientes, incluido el NO-GO para confirmar reservas sin exclusión transaccional real.
- C08-01.M02.01 define entidades conceptuales E01–E14, invariantes INV01–INV18 y REL01–REL12. C08-01.M02.02 define OP01–OP12/DTO01–DTO12/ERR01–ERR14/VAL01–VAL18 como operaciones semánticas futuras. C08-01.M02.03 define P01–P12, IDX01–IDX12, MIG01–MIG08 y F01–F12 FUTUROS sin DDL.
- Aquí no se registran IOptions, variables de entorno, entradas de CONFIG/Sheet, flags en DB, workers, proveedores externos, calendario, endpoint, Angular, MySQL, EF, datos personales ni cambios de configuración real. Los nombres son **propuestas de contrato**, no opciones existentes verificadas.
- Empresa, sucursal, profesional y recursos **nunca** se autorizan por host, query string o payload. El backend verifica JWT, membresía, rol, tenant A/B, revocación y versión de política antes de devolver cualquier configuración visible o confirmar una operación.
- La duración, buffers, horarios y límites son reglas de negocio independientes de los feature flags: ningún flag relajará seguridad, integridad, auditoría, privacidad, idempotencia, control de concurrencia o límites de empresa.

## 2. Orden seguro de resolución del ámbito

1. **Baseline de seguridad no relajable:** autorización backend, aislamiento por EmpresaId, protección PII, UTC/IANA y validación de versiones. No existe override que lo suprima.
2. **Política por empresa verificada:** solo para la empresa de la sesión activa y rol autorizado; nunca fallback a otra empresa.
3. **Política de sucursal válida:** debe pertenecer a esa empresa, emplear zona IANA aprobada y no ampliar límites de su matriz.
4. **Política de servicio/profesional/recurso:** restricciones adicionales que estrechan capacidades y tiempos; se rechaza referencia a otro tenant o recurso deshabilitado.
5. **Solicitud de cliente:** propone parámetros, pero jamás convierte OFF en ON ni amplía un límite servidor. Ausencia de identidad, conflicto, versión obsoleta o error de parseo => fail closed.

Precedencia formal futura: baseline protegido > política de empresa > restricciones de sucursal > restricciones de servicio/recurso; entre restricciones aplicables se usa el valor **más restrictivo**. Configuración ausente o inválida no habilita funciones. Cambios de política requieren versión, actor autorizado, auditoría minimizada, control de concurrencia y reversa. No inferir una modalidad de despliegue por este documento.

## 3. Opciones tipadas propuestas OPT01–OPT18 (NO IMPLEMENTADAS)

| ID | Opción conceptual / tipo | Regla de validación y valor seguro |
| --- | --- | --- |
| OPT01 | AgendaEnabled: bool | OFF hasta aprobación de código R2 y gates |
| OPT02 | PublicAvailabilityEnabled: bool | OFF; solo ranuras sin PII y rate limit si se activa |
| OPT03 | BookingEnabled: bool | OFF mientras no exista transacción MySQL probada |
| OPT04 | ReschedulingEnabled: bool | OFF hasta rollback atómico probado |
| OPT05 | CancellationEnabled: bool | OFF hasta liberación de ocupación idempotente probada |
| OPT06 | ExternalCalendarSyncEnabled: bool | OFF; sin proveedor ni credenciales |
| OPT07 | NotificationDispatchEnabled: bool | OFF; sin SMTP/webhook/worker por este documento |
| OPT08 | WaitlistEnabled: bool | OFF; sin cola ni escrituras especulativas |
| OPT09 | StaffSelfServiceEnabled: bool | OFF; RBAC y revocación obligatorios |
| OPT10 | ResourceCapacityBookingEnabled: bool | OFF; concurrencia real antes de activar |
| OPT11 | SucursalZonaIana: string | zona IANA aprobada, no zona de browser |
| OPT12 | MaxBookingHorizonDays: int | positivo y acotado por política; no aceptar ilimitado |
| OPT13 | MinLeadMinutes: int | no negativo y limitado; evita reserva en pasado |
| OPT14 | MaxServiceDurationMinutes: int | positivo y acotado; nunca overflow |
| OPT15 | BufferBeforeAfterMinutes: int | no negativo, sin solapamiento |
| OPT16 | IdempotencyRetentionHours: int | positivo, acotado y mayor que ventana de retry autorizada |
| OPT17 | MaxAvailabilityPageSize: int | positivo y mínimo entre topes de tenant/servidor |
| OPT18 | PolicyVersion: long | monotónica; escritura CAS, rollback compensatorio |

Los números concretos, zona aprobada y retenciones no se asignan arbitrariamente: requieren D01–D08 y aceptación de negocio. No convertir el esquema de esta tabla en defaults de producción. El OFF fail-closed es una **intención futura**, no lectura de un flag implementado.

## 4. Flags y estrategias de degradación FL01–FL12

| ID | Puerta futura y estrategia segura cuando está OFF/error |
| --- | --- |
| FL01 | Disponibilidad pública OFF => no publicar agendas privadas ni datos de profesional |
| FL02 | Confirmación de cita OFF => ofrecer información no contractual, jamás éxito simulado |
| FL03 | Reprogramación OFF => preservar cita anterior y ocupaciones |
| FL04 | Cancelación automática OFF => no liberar recursos sin transición autorizada |
| FL05 | Sincronización externa OFF => no crear tráfico/credenciales/duplicados |
| FL06 | Notificaciones OFF => no enviar PII ni suponer que fueron entregadas |
| FL07 | Lista de espera OFF => no reservar capacidad ni prometer orden de atención |
| FL08 | Autoservicio staff OFF => no elevar roles por UI |
| FL09 | Recursos múltiples OFF => no aceptar reserva incompleta |
| FL10 | Agenda offline/caché OFF => no confirmar datos obsoletos |
| FL11 | Configuración de sucursal inválida => 4xx seguro, no default de otro tenant |
| FL12 | Kill switch de agenda => detener nuevos intentos sin alterar citas comprometidas ni histórico |

Ningún toggle sustituye requisitos R2: locks MySQL efímero, pruebas de 64 reservas concurrentes, control JWT/RBAC/tenant A/B, idempotencia por tenant+actor+comando+fingerprint, zonas DST y rollback. La UI solo comunica disponibilidad verificada por el servidor.

## 5. Validaciones contractuales VAL01–VAL16 para configuración

| ID | Rechazo o requisito futuro |
| --- | --- |
| VAL01 | EmpresaId derivado de identidad backend con membresía activa |
| VAL02 | SucursalId pertenece a empresa y tiene zona IANA válida |
| VAL03 | Actor posee rol/permisos de administración de agenda, no claim self-assigned |
| VAL04 | Revocación de token/rol/sesión invalida cambio y lectura privada |
| VAL05 | Flags ausentes/desconocidos se interpretan OFF, sin defaults abiertos |
| VAL06 | No activar BookingEnabled antes de CI MySQL 8.4 y exclusión real |
| VAL07 | No permitir duración, lead, buffer, retención y página fuera de rango |
| VAL08 | No usar hora local ambigua por DST sin offset decidido |
| VAL09 | Cambios de política requieren versión vigente y CAS |
| VAL10 | Cache/etag invalidado ante revocación o nueva versión |
| VAL11 | No persistir secretos/tokens en opciones ni logs |
| VAL12 | No exponer PII o configuración privada en respuesta pública |
| VAL13 | Override de tenant B no contamina límites ni flags de A |
| VAL14 | Cambio parcial o fallo intermedio no debe aplicar mitad de opciones |
| VAL15 | Deshabilitar feature no destruye reservas previas ni impide auditoría |
| VAL16 | Booking nunca altera \`ReservaInventario\`, stock ni saldos ERP |

## 6. Fixtures de aceptación FUTUROS T01–T12 (NO EJECUTADOS)

| ID | Positivo futuro | Negativo/fallo que deberá rechazarse |
| --- | --- | --- |
| T01 | empresa A lee su política autorizada | B no lee ni cambia valores de A |
| T02 | sucursal válida resuelve zona IANA | zona inexistente/DST ambiguo sin offset falla |
| T03 | admin de agenda actualiza versión autorizada | rol revocado o actor sin permiso no muta |
| T04 | flags OFF impiden crear reservas | cliente manipulando JSON no los habilita |
| T05 | duración y buffer acotados preservan intervalos | valores cero, negativos y overflow rechazan |
| T06 | límite horizonte y lead válido aplica | fecha pasada o demasiado futura se rechaza |
| T07 | cambio CAS incrementa PolicyVersion | versión obsoleta falla sin lost update |
| T08 | kill switch detiene nuevas confirmaciones | no cancela citas comprometidas ni libera doble |
| T09 | expiración idempotente mantiene replay seguro | token/key de B o fingerprint distinto falla |
| T10 | 64 solicitudes sobre mismo slot confirman una | configuración no elude transacción ni bloqueo |
| T11 | vista pública muestra solo ranuras mínimas | detalles staff/PII no aparecen en caché |
| T12 | desactivar sincronización no genera tráfico | fallo externo no confirma ni altera ERP |

**Evidencia de ejecución actual: 0/12 tests runtime; integración, MySQL, HTTP, xUnit, Playwright, carga, latencia y precisión de DST NO_MEDIDO.** Los fixtures futuros serán sintéticos (empresa A/B, sucursales distintas, zona con DST y sin DST); nunca datos reales.

## 7. A0–A5, riesgos y rollback

- **A0:** trazabilidad al ID, predecesores y PLAN/COLA/DAG verificados; diff limitado a este Markdown y CHANGELOG_AI.md, riesgo R0.
- **A1:** inspección documental de OPT01–OPT18, FL01–FL12, VAL01–VAL16 y T01–T12; pares positivos/negativos/frontera/error *especificados*, no pruebas ejecutadas.
- **A2:** validación estática de fail-closed, JWT/RBAC/tenant A/B, PII, revocación, CAS, DST, concurrencia e idempotencia.
- **A3:** cotejo de modelos y fuentes previas; configuración ejecutable, API, MySQL, Angular, browser, proveedores y migraciones **NO_APLICA** por diff R0. Al implementar será R2 con pruebas reales.
- **A4:** publicación dev fast-forward/CAS, checks Scope Lock y procedencia V5 \`completed/success\` del SHA propio, readback PLAN/BITACORA. CI auxiliar no causal en progreso no equivale a PASS.
- **A5:** revisión independiente por otra Task Javier de ID diferente; no autoaprobación ni inferir autorización QA/main/PROD.

**Rollback R0:** commit compensatorio \`git revert\` tras releer HEAD/CAS; preservar BITACORA histórica. La documentación no habilita servicios, módulos de agenda, datos MySQL reales, scripts EF, secretos, servidores ni cambios en cuentas. No adelantar M02.06/M03 sin puertas verdaderas y A5 causal.
