# SOLQARYN V5 — C08-01.M01.01 — Alcance y no-regresión de reservas y agenda

**Autor:** PRIMARY DEV 01 — JAVIER (`6adbb9fff1548191a1bd5b30d9e9984a`). **Fecha:** 2026-11-14. **Fuente institucional:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, bloque C08-01, M01.01. **Naturaleza:** preparación documental; no crea agenda, reserva, cita, tablas, cobros, workers, permisos ni endpoints.

## 1. Objetivo, salida observable y delimitación

El bloque C08-01 — **Reservas y agenda** — requiere planificar citas, horarios, disponibilidad, personal, recursos y cancelaciones con evidencia futura de **conflicto de horario, zona horaria y autorización**. En M01.01 únicamente se fija alcance, exclusiones y criterios de no-regresión; las fases M02–M05 deberán definir modelo, interfaces, implementación y pruebas reales. La presencia de un módulo de inventario o de un nombre `ReservaInventario` no demuestra que exista una agenda de servicios: stock y reserva de tiempo son contextos distintos.

**Objetivo de producto:** una empresa autorizada puede ofrecer prestaciones con duración y calendario, consultar franjas por sucursal/profesional/recurso, crear una cita sin solapamiento, reprogramar o cancelar conforme reglas verificables, y observar su estado en UI accesible. Esta es una **capacidad objetivo**, NO funcionalidad certificada ni implementada en esta microtarea.

| Superficie | Alcance objetivo futuro | Lo expresamente excluido de M01.01 |
| --- | --- | --- |
| Servicio | Duración, buffers, precio informativo y disponibilidad | Nuevas entidades, migraciones, API o facturación |
| Empresa/tenant | Calendarios separados por empresa/sucursal y roles | Cambiar JWT, RBAC, claims o policies |
| Personal | Turnos, descanso, ausencias, permisos y calendario por profesional | Crear usuarios/cuentas o asignaciones reales |
| Recursos | Cabina, sala, equipo y capacidad concurrente | Reglas de reserva de stock/costo ajenas |
| Cita | Solicitud, confirmación, reprogramación y cancelación trazables | Capturar pagos o enviar notificaciones |
| Calendario | Horas locales con zona IANA y persistencia instantánea UTC | Cambiar timezone de la plataforma o Cron |
| Concurrencia | Impedir doble ocupación y replay idempotente de reintentos | Ejecutar transacciones MySQL sobre datos reales |
| Interfaz | Disponibilidad clara, estados vacíos/error, foco y navegación móvil | Implementar Angular o Playwright en esta unidad |

## 2. Fronteras de confianza y autoridad

Una cita pertenece a una empresa, servicio, sucursal y recurso/profesional autorizado, con identidad y estado persistidos por el servidor. El browser sólo propone hora, zona y referencias: nunca es autoridad para `EmpresaId`, permisos, duración, capacidad o disponibilidad. Las búsquedas públicas futuras deben proyectar únicamente franjas autorizadas y no exponer nombre, teléfono, datos privados, JWT o detalles de agenda interna. Una cita de A jamás se lee, modifica, cancela o desplaza desde B, incluso usando un ID existente.

La normalización temporal debe distinguir **instante UTC** de **hora local con zona IANA**. Una hora inexistente por cambio horario se rechaza, una hora ambigua se desambigua explícitamente y límites de día/festivo se verifican. El horario de una sucursal puede diferir de la zona del cliente; no inferir una zona por `DateTime.Now` del navegador. No se impone una zona horaria global, tampoco se cambia la configuración actual `America/Tegucigalpa` de las Tasks.

## 3. Invariantes de no-regresión NR01–NR12

- **NR01 — Aislamiento:** actor o visitante de A no accede a citas, clientes, calendarios privados, recursos o servicios de B por ID o URL.
- **NR02 — Autoridad:** toda modificación valida JWT/RBAC/membresía/tenant/ámbito de sucursal en backend; UI no confiere permiso.
- **NR03 — Capacidad:** un recurso de capacidad uno no admite citas confirmadas solapadas, incluso con dos solicitudes simultáneas.
- **NR04 — Personal:** una cita requiere profesional/turno compatible; ausencia y descanso no se consideran disponibles.
- **NR05 — Duración:** duración del servicio y buffers de preparación/cierre se aplican en el servidor; cliente no puede reducirlos para forzar hueco.
- **NR06 — Tiempo:** instantes se comparan en UTC y se muestran en zona IANA validada, sin doble reserva por DST/hora ambigua.
- **NR07 — Estados:** propuesta, confirmación, cancelación, ausencia y ejecución no se confunden; transiciones inválidas fallan sin efectos.
- **NR08 — Idempotencia:** doble clic, replay y timeout no crean citas duplicadas ni cancelan más de una vez; se requiere clave/constraint durable futuro.
- **NR09 — Privacidad:** agenda pública/compartida no muestra datos personales ni razones de ausencia ni referencias de otras empresas.
- **NR10 — Compatibilidad ERP:** reserva horaria no altera reserva de inventario, saldo, compras, facturas, pagos ni snapshots históricos.
- **NR11 — Accesibilidad:** navegación por teclado, foco, lector de pantalla, carga, error y mobile 320px permiten corrección sin afirmar éxito falso.
- **NR12 — Rollback/auditoría:** error/rollback preserva citas válidas, correlación de cambios y auditoría, sin liberar o duplicar ocupaciones ajenas.

## 4. Aceptación futura — criterios P01–P08/N01–N12 NO_EJECUTADOS

| Par | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| P01/N01 | Empresa A ve sólo horarios publicados de A | Un ID de cita/empleado B devuelve 403/404 sin PII |
| P02/N02 | Profesional A con turno ofrece huecos y buffers | Pausa, ausencia o turno cerrado no ofrece disponibilidad |
| P03/N03 | Reserva capacidad 1 en hora libre queda confirmada una vez | Dos reservas simultáneas del mismo recurso se rechazan sin doble ocupación |
| P04/N04 | Diferentes recursos con capacidad permiten citas compatibles | Recurso de B o incompatible no se utiliza por ID falsificado |
| P05/N05 | Zona IANA convierte correctamente instante UTC | Hora inexistente/ambigua DST sin elección explícita se rechaza |
| P06/N06 | Cliente autorizado reprograma respetando reglas y permisos | Usuario sin permiso cambia cita ajena o estado terminal |
| P07/N07 | Cancelación válida libera disponibilidad una vez con auditoría | Dos cancelaciones o replay no generan doble liberación ni transiciones ilegales |
| P08/N08 | UI móvil muestra hora local, zona, error y estado confirmado real | Timeout no muestra cita confirmada ni pierde acción recuperable |

Adicionales **N09–N12**: N09 modificar duración/price/capacidad del cliente; N10 exportar agenda con PII a rol lector público; N11 condición de carrera en checkout/reserva del servicio; N12 fallo entre insert y ocupación deja cita huérfana. Estos también son futuros. Ninguno se ejecutó ni tiene PASS runtime en M01.01.

## 5. Dependencias, impacto, trazabilidad y seguridad

- `PLAN_MAESTRO!H1154/I1154`, `COLA!G1154/O1154`, `DAG_TECNICO!A1115:J1115`: F=VALIDADO/G=SI verificados antes del claim. Grupos C01-01–04, C02-01–04 y C06-01–04 M01.01–05: **60/60 LISTO** de preparación, no autorización para M01.06/M02 ni integración de agenda.
- El inventario selectivo del directorio `backend/src/Domain/Entities/` no encontró archivos con nombre `Cita` o `Agenda` en la lectura realizada; **no se concluye ausencia total de código de agenda** en otros paquetes, capas, ramas o archivos. M01.03 debe localizar fuentes exactas y gaps sin inferir certificación.
- La arquitectura y seguridad objetivo conservan .NET 10, Angular 22.2.1, EF/MySQL 8.4, JWT/RBAC y segregación tenant; nada se modifica ahora. No manipular tokens, correos, calendarios externos, pagos, trabajadores, webhooks ni infraestructura.
- **A0:** fuente única V5, alcance, 60 predecesores y mapa de invariantes. **A1:** coherencia de P/N como oráculos futuros NO_EJECUTADOS. **A2:** amenazas tenant, PII, permisos, carrera y DST inventariadas; no probado en HTTP/SQL. **A3:** pruebas .NET/Angular/MySQL/Playwright `NO_APLICA` exclusivamente a diff Markdown, no certificación. **A4:** publicación fast-forward en dev, Scope Lock y check V5 CHANGELOG `completed/success` en SHA exacto, readback PLAN/COLA/BITACORA. **A5:** independiente por otra Task Javier distinta a la autora.
- **Rollback** documental por `git revert` normal con evento compensatorio nuevo, sin borrar historia o eventos de 26 columnas.
- **Impacto permitido:** sólo este archivo `docs/v5` y encabezado/viñetas autocontenidos en `CHANGELOG_AI.md`. Ninguna ejecución automática de eventos, reservas, cambios de JWT, datos ni promoción QA/main/PROD.
