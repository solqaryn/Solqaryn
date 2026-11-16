# C12-04.M03.03 — autorización y aislamiento de preflight MySQL privado

## Clasificación y alcance
R2 (tenant, RBAC, MFA, recuperación de MySQL y concurrencia). Se añade código
.NET 10 en Application para filtrar solicitudes **antes** de consultar un
lector de evidencia. La identidad y membresía deben ser afirmaciones
verificadas por el servidor; el cliente jamás puede fabricar un objeto
C1204VerifiedActor válido. Este cambio no registra endpoints, tokens, grants
ni lectores de base de datos, y no activa MySQL, Aiven, proveedores o migraciones.

## A0: invariantes
- A/B: el tenant solicitado debe igualar exactamente al tenant autenticado
  y la propiedad de origen/destino debe ser verdadera, sin fallback.
- Estado de autorización: JWT validado por servidor, membresía vigente/no
  revocada, permiso de revisión y MFA reciente. Ausencia de cualquiera =>
  denegación antes de consultar evidencia externa.
- Entorno exacto dev, intención reconocida, commit SHA hexadecimal de 40
  caracteres y manifiesto coincidente; dry-run exige clave opaca de idempotencia.
- Prohibidos cutover real, SQL/destrucción, privilegios elevados, secretos,
  fallback cross-tenant y P0/P1 sin resolver. Un resultado positivo habilita
  SOLO consulta read-only para validación adicional, no despliegue ni SQL.
- El servicio de aplicación existente ahora usa la política y mantiene
  validación de procedencia de actor, tenant, SHA y evidencia de Domain.

## A1–A3: pruebas reales pertinentes
La nueva suite xUnit comprueba casos positivos y adversariales sobre el
servicio existente con lector espía: tenant A/B, revocación, rol, JWT, MFA,
identificadores vacíos/inseguros, límite de longitud, QA/PROD, permisos
excesivos, P0/P1, SHA, idempotencia y ausencia de llamadas al lector ante
rechazos. Un lector falso devuelve null incluso en caso positivo para
demostrar que no existe aprobación operativa por simple validación de campos.
CI GitHub-hosted .NET 10, build Release y checks de seguridad/autorización
son OBLIGATORIOS; la redacción de este documento no prueba su ejecución.
MySQL 8.4, Category=Integration, restore, HTTP JWT real o un proveedor externo:
NO_APLICA para diff sin código EF/DB/HTTP; su conformidad real NO_MEDIDO.

## A4, A5, reversión
Exigir SHA causal y checks REQUIRED = completed/success, Scope Lock STRICT,
V5 y readback del Plan/COLA/DAG/BITACORA; nunca contar skipped/in_progress
como PASS. No marcar LISTO con fallo o CI pendiente. A5 por Task Javier
**distinta del autor**, con examen de código y pruebas independiente.
Rollback normal mediante git revert después de revalidar HEAD, sin
force-push, QA/main/PROD, secrets ni mutaciones productivas.
