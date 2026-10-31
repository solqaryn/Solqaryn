# P1-003.A — Contexto asistido de superadministrador (DEV, WIP)

**Estado:** EN_PROGRESO, sin autorización de activación ni certificación funcional.

## Alcance de esta entrega parcial

Se incorpora un objeto de dominio inerte `SolicitudContextoSoporte` con invariantes mínimas de solicitud: identificadores positivos para actor de plataforma y empresa destino, justificación de 20 a 500 caracteres sin caracteres de control, y expiración solicitada entre 1 y 15 minutos.

**Endurecimiento adicional:** la fábrica pública `CrearDesdeRelojServidor` recibe un `TimeProvider` del backend y calcula internamente inicio y vencimiento; el constructor con timestamps arbitrarios es privado. La normalización a UTC y la duración máxima se verifican con reloj determinista en pruebas. El request HTTP futuro no debe aceptar timestamps libres ni un `TimeProvider` controlado por el cliente.

**Endurecimiento temporal y de auditoría de la solicitud (30-10-2026):** se añade `EstaDentroDeVentanaSolicitada(TimeProvider)`, con instante inicial inclusivo y expiración exclusiva, normalización UTC y rechazo de reloj nulo. Esta comprobación cubre exclusivamente el intervalo solicitado; **NO es una concesión de permisos ni una sesión válida**. La justificación rechaza caracteres invisibles `UnicodeCategory.Format` (incluidos zero-width y controles de dirección bidi) además de caracteres de control ordinarios para reducir el riesgo de ocultamiento en auditoría. Las pruebas ejercitan bordes, último tick antes de expiración y offsets.

**Este objeto NO crea una sesión, NO emite JWT, NO cambia tenant, NO comprueba roles y NO escribe auditoría.** No hay endpoint, persistencia, migración ni UI. El cambio no concede acceso comercial entre empresas y no debe presentarse como `P1-003.A=LISTO`.

## Fronteras y dependencias antes de implementar runtime

1. Backend/Application debe volver a leer desde DB la autoridad **activa** de plataforma del actor autenticado, con permiso específico de soporte, y validar empresa destino habilitada. No confiar en flags, IDs, claims viejos o elección del cliente.
2. Justificación y plazo son datos solicitados, no fuente de autoridad. El servidor fija UTC. Cualquier contexto debe vincular actor, tenant, motivo, instante y correlación a una auditoría duradera confirmada antes del cambio.
3. Las operaciones de datos deben seguir pasando por guards/repositorios tenant y revalidar rol/permisos/estado/expiración en cada solicitud. Nunca se obtiene acceso general a todos los tenants por rol global.
4. Rechazar tenant inexistente/no autorizado, rol inactivo/revocado, justificación insuficiente, falta de registro auditable, vencimiento y reutilización de contexto. Negativos multiempresa obligatorios.
5. Establecer expiración/revocación y observabilidad sin registrar PII ni secretos en logs. No introducir campos de soporte en JWT ni habilitar endpoint hasta que las pruebas de seguridad y auditoría sean concluyentes.
6. Revisar el contrato existente `UsuarioRolPlataforma` / `RegistroAuditoria` y las dependencias P1-002.A/P1-003.B antes de integrar. Cambios de esquema o API requerirán análisis de impacto y pruebas correspondientes.

## Impacto, CI y rollback

- Componente: `backend/src/Domain/Entities/SolicitudContextoSoporte.cs`, tests xUnit, este documento. Sin frontend ni consumidor runtime.
- Datos, MySQL, migraciones, credenciales, proveedores y despliegues: **ningún cambio**.
- Seguridad: solicitud representada en memoria, sin autorización ni bypass tenant.
- Gate adicional transversal: issue #3805 describe un hallazgo del escáner en fixtures históricos de P0-008.B; no se cambia escáner ni se afirma CI aplicable PASS hasta verificación de SHA exacto.
- Rollback: revertir estos archivos mediante PR protegido, sin datos que migrar.

**Pendiente para VALIDANDO:** caso de uso integrado, autorización backend vigente, auditoría persistida, expiración/revocación efectivas, pruebas JWT/RBAC/tenancy positivas/negativas, CI aplicable exact-SHA y revisión/certificación conforme a autoridad viva.