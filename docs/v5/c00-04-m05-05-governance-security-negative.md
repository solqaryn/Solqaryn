# C00-04.M05.05 — Casos negativos de seguridad de gobierno (V19)

## A0: contrato y limites
ID vigente C00-04.M05.05, propietario DEV04, riesgo R2 por autorización y aislamiento. Extiende pruebas existentes sobre GobiernoCambiosAuthorizationService y el guard vigente; NO modifica seguridad de producción, implementación, API, JWT, RBAC, base de datos, migraciones, tenant real, permisos, DI, endpoints, Vercel, Render ni Aiven. El resultado sigue siendo una consulta inerte, nunca autorización para ejecutar, persistir o conceder permisos.

## A1/A2: aserciones adversariales
Siete casos xUnit: aceptación positiva inerte como control; tenant B denegado antes de fuente externa; bypass por administrador legacy sin asignación relacional; permiso revocado; ámbito alterado; cambio sensible con rollback forzado a NO_GO; ID manipulado rechazado antes de I/O. Cada test comprueba resultado o excepción y el número causal de consultas a la fuente donde importa. Una vulnerabilidad de bypass debe provocar fallo en CI, no solo logging.

## A3: integración pertinente
Pruebas compiladas y ejecutadas con .NET 10 Release sobre el servicio y guard reales de Application y reglas de Domain, con IIdentidadUniversalSnapshotSource como doble en memoria. NO son pruebas HTTP reales, JWT firmado, persistencia MySQL 8.4 ni tenancy interempresarial end-to-end; se declara este límite. No se tocaron EF ni proveedor, por lo que MySQL aquí es NO_APLICA motivado. La suite de regresión y seguridad general sigue exigible cuando corresponda.

## A4/A5: publicacion, CI y rollback
Workflow dirigido por SHA sin cancelación entre commits, además de Scope Lock exacto, V5/procedencia, build/tests generales aplicables y evidencia en PLAN_MAESTRO/BITACORA. SKIPPED, CANCELLED, QUEUED, IN_PROGRESS o FAIL NO son PASS de pruebas requeridas. Con CI externa real pendiente registrar RUN/SHA/check, conservar VALIDANDO con owner y liberar candado V17 tras readback. A5 corresponde a otra Task Javier distinta de DEV04. Reversión mediante git revert normal y verificación de concurrencia, nunca force-push, QA/main/PROD ni secretos.
