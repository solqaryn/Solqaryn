# P1-003.A — Contexto asistido de superadministrador (DEV, WIP)

**Estado:** EN_PROGRESO, sin autorización de activación ni certificación funcional.

## Alcance de esta entrega parcial

Se incorpora un objeto de dominio inerte `SolicitudContextoSoporte` con invariantes mínimas de solicitud: usuario de plataforma y empresa destino con ID positivo, justificación no vacía de 20 a 500 caracteres sin controles, reloj UTC y vigencia solicitada mayor que cero y de máximo 15 minutos. Sus tests incluyen entradas válidas y rechazo de IDs, motivos, horas y expiraciones inválidas.

**Este objeto NO crea una sesión, NO emite JWT, NO cambia tenant, NO comprueba roles y NO escribe auditoría.** No hay endpoint, persistencia, migración ni UI. El cambio no concede acceso comercial entre empresas y no debe presentarse como `P1-003.A=LISTO`.

## Fronteras y dependencias antes de implementar runtime

1. Backend/Application debe volver a leer desde DB la autoridad **activa** de plataforma del actor autenticado, con permiso específico de soporte, y validar empresa destino habilitada. No confiar en flags, IDs, claims viejos o elección del cliente.
2. Justificación y plazo son datos solicitados, no fuente de autoridad. El servidor establece UTC y debe acotar la expiración efectiva. Cualquier contexto debe vincular actor, tenant, motivo, instante y correlación a una auditoría duradera confirmada antes del cambio.
3. Las operaciones de datos deben seguir pasando por guards/repositorios tenant y revalidar rol/permisos/estado/expiración en cada solicitud. Nunca se obtiene acceso general a todos los tenants por rol global.
4. Rechazar tenant inexistente/no autorizado, rol inactivo/revocado, justificación insuficiente, falta de registro auditable, vencimiento y reutilización de contexto. Negativos multiempresa obligatorios.
5. Establecer expiración/revocación y observabilidad sin registrar PII ni secretos en logs. No introducir campos de soporte en JWT ni habilitar endpoint hasta que las pruebas de seguridad y auditoría sean concluyentes.
6. Revisar el contrato existente `UsuarioRolPlataforma` / `RegistroAuditoria` y las dependencias P1-002.A/P1-003.B antes de integrar. Cambios de esquema o API requerirán el análisis arquitectónico y pruebas correspondientes.

## Impacto y rollback

- Componente primario: `backend/src/Domain` y tests xUnit de backend. No hay frontend ni consumidor runtime todavía.
- Datos, MySQL, migraciones, credenciales, proveedores y despliegues: **ningún cambio**.
- Seguridad: el código nuevo representa únicamente una solicitud inválida o válida en memoria; no realiza autorización.
- Rollback: revertir los dos archivos de dominio/tests mediante PR protegido, sin datos que migrar.

**Pendiente para VALIDANDO:** implementación integrada y validada del caso de uso, auditoría persistida, controles JWT/RBAC/tenancy, pruebas causales positivas/negativas, CI exact-SHA y revisión/certificación conforme a autoridad viva.