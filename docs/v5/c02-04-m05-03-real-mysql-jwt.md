# C02-04.M05.03 — integración real de control global con MySQL 8.4 y JWT A/B

Autor DEV02, Task_ID 6af16bb9492c8191b8e8c7d606f11507. Exclusivo solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. PLAN_MAESTRO fila 424; elegibilidad PLAN PENDIENTE sin owner, COLA G/O=SI, DAG F=VALIDADO/G=SI y candado V17 2000000424 verificados antes del claim. Riesgo R2: JWT/RBAC, control global y CI de proveedor MySQL/HTTP.

## A0 — alcance y arquitectura

Se extiende el laboratorio GitHub-hosted ya existente p4-ab-real-auth-ephemeral.yml con una regresión independiente de sólo lectura sobre la API real en localhost y MySQL 8.4 efímero. No se modifica runtime backend, DTO, Angular, grants comerciales, esquemas, migraciones, secretos, Aiven, Render o PROD. El laboratorio previo compila .NET 10 Release, crea esquema OracleBaseline, levanta API real, materializa empresas A/B y usuarios JWT, concede un rol global persistido y ejecuta sus validaciones. El incremento integra respuestas HTTP con el estado real de MySQL por consultas SQL dirigidas en el contenedor efímero, sin datos de clientes.

## A1 — asserts causales falsables

Regresión Node real (no mocks): el token global autorizado debe listar empresas A y B con DTO minimizado y cache-control no-store; el usuario anónimo, administradores A y B y un intento de spoof de X-Empresa-Id deben resultar 401/403; página 0 y tamaño 51 deben producir HTTP 400; borde tamaño 1 debe devolver exactamente una fila, sin exponer secretos. Ante cualquiera de esos defectos, el runner falla con estado no-cero. La consulta MySQL valida que exista exactamente una membresía global activa persistida para el actor, y membresías de empresas A/B con IDs separados. P4 existentes ejecutan además revocación y empresas inactivas: este incremento se ubica ANTES de alterar grants efímeros.

## A2 — aislamiento y seguridad

Se comprueba token JWT real, grant relacional persistido y denegación de actor empresarial incluso con cabecera cruzada A→B; nunca se infiere una autorización global desde una sesión de empresa. El CI usa únicamente contraseñas sintéticas locales de la suite existente, sin credenciales de Aiven o producción. La prueba permite clasificar seguridad de API por respuesta real y SQL efímero, no una auditoría completa de todas las operaciones de negocio ni una certificación de PROD.

## A3 — integración REAL

Runner GitHub-hosted con imagen mysql:8.4, migraciones OracleBaseline efímeras, API ASP.NET Core net10.0, HTTP real localhost y consultas mysql CLI a UsuarioRolesPlataforma/UsuarioEmpresas; se ejecuta script scripts/ci/c02-04-m05-03-real-persistence-provider.mjs y los pasos existentes de laboratorio. A3 sólo PASS con ejecución y logs de todos los pasos, no por texto añadido a YAML. No nuevas integraciones pagadas.

## A4/A5 — gates y reversión

Exigir P4 job SUCCESS del SHA exacto o evidencia causal equivalente, además de verify-solqaryn-project-scope, procedencia V5, tests/backend/seguridad aplicables, PLAN/COLA/DAG/BITACORA readback. CI QUEUED/IN_PROGRESS/SKIPPED/CANCELLED/FAILED nunca es PASS. Entregar VALIDANDO si CI externa ejecuta y luego REVIEW_FIRST; A5 por otra Task Javier que no sea la autora. Rollback con git revert normal, no reescritura ni fuerza; no QA/main/PROD.
