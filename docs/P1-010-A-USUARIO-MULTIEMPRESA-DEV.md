# P1-010.A — Usuario asociado a varias empresas; permisos por membresia (DEV)

## Autoridad y claim
- Microtarea comercial P1-010.A, P1 orden 43, PLAN_MAESTRO/COLA fila 44; dependencia P0-012.B LISTO.
- Propietario VIGENTE: PRIMARY_DEV_01 Javier :00 TASK_ID 6adbb9fff1548191a1bd5b30d9e9984a, segun PLAN_MAESTRO!I44 y transferencia institucional documentada. Autor original de los commits: PRIMARY_DEV_02 Javier :20 TASK_ID 6adc2e4bd8a48191ac7d8641ed95a1a3; GitHub jmejia31. Conservar autoría y rama original.
- Rama determinista work/solqaryn-p1-010-a nacida por GitHub create-if-absent desde dev@30bf43749a8eb7b164a065f5aabebfa024aa0faa; sin rama ni PR competidores observados.
- Gobierno VIGENTE: WIP 6/1/1 (MAX_CONCURRENT_SCOPES=6, MAX_OWNED_SCOPES_PER_TASK=1, MAX_ACTIVE_WRITES_PER_TASK=1). Javier :00 posee solo P1-010.A mientras VALIDANDO; Javier :20 conserva autoría/ownership anterior de P1-007.A, cuyo PR #3839 ya fue MERGED sin que eso sustituya el readback de cierre en Sheets. Ningún segundo CLAIM ni transferencia implícita.
- Plan rector institucional: Google Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA. Scope Lock STRICT; solo dev.

## Reutilizacion del codigo vigente y prueba causal
- UsuarioScopeService resuelve UsuarioTenantScopeActual exclusivamente desde UsuarioEmpresa activa, Usuario sin bloqueo, Empresa activa y Rol empresarial activo perteneciente al tenant o plantilla de sistema elegible; no convierte Usuario.RolId ni JWT en permiso.
- Se reutiliza UsuarioScopeService real y EF Core InMemory sin modificar runtime, DB, API, migrations ni RBAC. Nueva suite P1010AMultiEmpresaMembershipTests de ocho pruebas xUnit valida dos empresas simultaneas con roles dispares, seleccion implicita rechazada, inactivacion/reactivacion aislada, rol privado A invalidado en B, bloqueo de usuario, empresa y rol inactivos y ausencia de ambas membresias con rol legacy.
- Prueba dirigida esperada en GitHub Actions: dotnet test backend/tests/Solqaryn.Tests/Solqaryn.Tests.csproj --configuration Release --filter FullyQualifiedName~P1010A. Registrar el SHA y resultado TRX reales antes de VALIDANDO.
- Esta evidencia solo comprueba el servicio de autorizacion en EF InMemory; NO acredita filtros HTTP, JWT real, MySQL DEV ni despliegues, y no permite afirmar LISTO sin CI exact-SHA, AUTO_CERT=PASS genuino, merge protegido y readback institucional.

## Reglas de cierre
- PR a dev; conservar revision formal independiente cuando cualquier diferencia futura modifique controles protegidos o seguridad sensible. El autor no puede aprobar sus propios commits.
- Tras CI terminal sin fallos y verificacion de pruebas dirigidas, evaluar cambio EN_PROGRESO a VALIDANDO por via institucional autorizada.
- Merge solo por flujo GitHub protegido con permisos efectivos; postmerge CI y PLAN_MAESTRO/COLA (formulas intactas)/BITACORA requieren readback. P1-010.B queda separado.
- No QA/main/PROD, Aiven productivo, secretos, cambios de proveedores, adquisiciones, toggles ni tareas fisicas nuevas.

## Reconciliación y evidencia viva — 2026-11-04

- La rama existente se reconcilia mediante merge Git no destructivo contra el HEAD actual de `dev` posterior a PR #3839, conservando su historial. Solo cambian la suite xUnit, esta acta y CHANGELOG_AI.md; los cambios funcionales de otros PR permanecen intactos.
- El SHA definitivo del nuevo HEAD, la prueba dirigida TRX, Scope Lock y la CI aplicable deben verificarse en GitHub sobre ese SHA. Ningún CI antiguo, aprobación o merge anterior sustituye esos controles.
- `LISTO` solo después de merge protegido, CI postmerge aplicable y readback de PLAN_MAESTRO/COLA/BITACORA. No atribuir a una Task una edición manual de esta reconciliación.
