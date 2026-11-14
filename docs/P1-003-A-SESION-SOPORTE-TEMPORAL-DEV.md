# P1-003.A — Sesión temporal de soporte de solo lectura

Estado: propuesta en DEV, pendiente de CI exact-head, revisión independiente, merge y despliegue.

## Contrato

- La autoridad se obtiene en cada petición desde una asignación activa de rol de plataforma ligada a una cuenta activa y no bloqueada. Un rol JWT o una membresía empresarial no concede autoridad de soporte.
- La concesión requiere una empresa activa, un motivo de 10–500 caracteres y una duración de 1–30 minutos. La allowlist inicial se limita a Productos (GET `/productos`, `/productos/{id}`) y Categorías (GET `/categorias`, `/categorias/activas`, `/categorias/{id}`); cada entrada está fijada por módulo, controlador, acción HTTP y plantilla exacta, y fue revisada contra su servicio y repositorio sin `IgnoreQueryFilters` ni SQL propio en esas lecturas.
- El secreto es aleatorio de 256 bits; solo se almacena su SHA-256. La respuesta de creación usa no-store.
- Solo se aceptan las cinco rutas GET exactas de la allowlist, con RequierePermiso(Ver) del módulo y X-Empresa-Id igual al de la sesión. No habilita rutas de identidad, RBAC, configuración, auditoría, ni permisos de escritura.
- El filtro global de EF usa únicamente el contexto verificado del servidor para limitar filas. SaveChanges y el interceptor de comandos rechazan mutaciones y consultas SQL con bloqueos.
- La autorización de lectura queda auditada de forma durable antes de pasar al endpoint. La fila de sesión permanece bloqueada durante la lectura HTTP; una revocación concurrente espera y luego invalida las siguientes solicitudes.
- La revocación exige autoridad de plataforma vigente y persiste su auditoría; si la auditoría de revocación falla, la sesión permanece revocada y el endpoint informa el error.

## Pruebas y límites

La propuesta agrega pruebas de autorización de rutas y una integración MySQL sobre una base efímera de CI: aislamiento A/B, límites de módulo/empresa/verbo, ausencia de membresías tenant del operador, revalidación de autoridad y empresa, rechazo de escritura EF/SQL, auditoría, revocación, replay y serialización frente a una revocación concurrente.

No se crean ni modifican membresías reales de DEV; el operador de soporte tampoco recibe una membresía empresarial. No se usaron ni alteraron las membresías existentes en esta etapa. La certificación de CI, la revisión, el merge y cualquier lectura autenticada contra el despliegue DEV se registran por separado; esta propuesta no afirma que esos gates o el despliegue ya hayan pasado.
