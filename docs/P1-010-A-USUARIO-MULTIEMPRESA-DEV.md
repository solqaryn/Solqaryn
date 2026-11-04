# P1-010.A — Usuario asociado a varias empresas; permisos por membresia (DEV)

## Autoridad y claim
- Microtarea comercial P1-010.A, P1 orden 43, PLAN_MAESTRO/COLA fila 44; dependencia P0-012.B LISTO.
- Propietario individual TASK_ID 6adc2e4bd8a48191ac7d8641ed95a1a3 (JAVIER_CHATGPT); identidad GitHub jmejia31.
- Rama determinista work/solqaryn-p1-010-a nacida por GitHub create-if-absent desde dev@30bf43749a8eb7b164a065f5aabebfa024aa0faa; sin rama ni PR competidores observados.
- Segundo scope de la Task: P1-007.A permanece VALIDANDO en PR #3839, sin edicion activa. WIP observado 4 scopes comerciales abiertos antes del claim; MAX_CONCURRENT_SCOPES=6, MAX_OWNED_SCOPES_PER_TASK=2, MAX_ACTIVE_WRITES_PER_TASK=1.
- Plan rector institucional: Google Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA. Scope Lock STRICT; solo dev.

## Reutilizacion del codigo vigente y prueba causal
- UsuarioScopeService resuelve UsuarioTenantScopeActual exclusivamente desde UsuarioEmpresa activa, Usuario sin bloqueo, Empresa activa y Rol empresarial activo perteneciente al tenant o plantilla de sistema elegible; no convierte Usuario.RolId ni JWT en permiso.
- Se reutiliza UsuarioScopeService real y EF Core InMemory sin modificar runtime, DB, API, migrations ni RBAC. Nueva suite P1010AMultiEmpresaMembershipTests valida dos empresas simultaneas con roles dispares, seleccion implicita rechazada, inactivacion/reactivacion aislada, rol privado A invalidado en B, bloqueo de usuario, empresa y rol inactivos y ausencia de ambas membresias con rol legacy.
- Prueba dirigida esperada en GitHub Actions: dotnet test backend/tests/Solqaryn.Tests/Solqaryn.Tests.csproj --configuration Release --filter FullyQualifiedName~P1010A. Registrar el SHA y resultado TRX reales antes de VALIDANDO.
- Esta evidencia solo comprueba el servicio de autorizacion en EF InMemory; NO acredita filtros HTTP, JWT real, MySQL DEV ni despliegues, y no permite afirmar LISTO sin CI exact-SHA, AUTO_CERT=PASS genuino, merge protegido y readback institucional.

## Reglas de cierre
- PR a dev; conservar revision formal independiente cuando cualquier diferencia futura modifique controles protegidos o seguridad sensible. El autor no puede aprobar sus propios commits.
- Tras CI terminal sin fallos y verificacion de pruebas dirigidas, evaluar cambio EN_PROGRESO a VALIDANDO por via institucional autorizada.
- Merge solo por flujo GitHub protegido con permisos efectivos; postmerge CI y PLAN_MAESTRO/COLA (formulas intactas)/BITACORA requieren readback. P1-010.B queda separado.
- No QA/main/PROD, Aiven productivo, secretos, cambios de proveedores, adquisiciones, toggles ni tareas fisicas nuevas.
