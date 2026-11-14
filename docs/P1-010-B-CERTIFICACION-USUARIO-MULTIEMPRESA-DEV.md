# P1-010.B — Certificación independiente de usuario multiempresa (DEV)

## Alcance, propietario y fuente rectora

- Microtarea P1-010.B, orden comercial 44, fila institucional PLAN_MAESTRO!45 y dependencia P1-010.A=LISTO comprobada.
- Task autora: PRIMARY_DEV_04, JAVIER_CHATGPT, ID 6aeacb7820ac8191b7d768a6f33dfa09; cuenta GitHub jmejia31. No se hereda autoría ni aprobación de otras Tasks.
- Rama propia work/solqaryn-p1-010-b, creada desde dev@4ac3fe76cc7f98c40a2aa1160c92560e1aab6bb2 mediante creación nativa, con lectura posterior del SHA y registro institucional atómico SQ-P1-010-B-JAVIER30-CLAIM-4AC3FE76-20261104 (BITACORA!557).
- Fuente exclusiva: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (Google Doc ID 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA), P1-010: validar listado, activación y permisos separados por membresía; negativa obligatoria: la autoridad de A no pasa a B.
- Restricciones: PROJECT_SCOPE_LOCK=STRICT, WIP 6/1/1, rama dev, sin cambiar QA/main/PROD, permisos, credenciales, MySQL productivo o reglas protegidas.

## Implementación de certificación en TestServer

backend/tests/Solqaryn.Tests/P1010BMultiEmpresaHttpJwtCertificationTests.cs incorpora cuatro pruebas dirigidas con JWT de prueba firmado, JwtBearer real en TestServer, TenantContextController, CurrentUserService y UsuarioScopeService reales, sobre base temporal EF Core InMemory aislada:

1. Un usuario autenticado con dos membresías A/B ve solo sus dos empresas; el token porta un rol y claim de administrador A, pero el contexto B devuelve su rol limitado B. Empresa C ajena se rechaza con HTTP 403.
2. Revocar B invalida el acceso HTTP 403 inmediatamente manteniendo A HTTP 200; reactivar B restaura únicamente sus permisos de consulta, no los de administrador A.
3. Asignar fraudulentamente a B el rol privado de A provoca HTTP 403 en B y no rompe A.
4. Sin JWT, HTTP 401; usuario bloqueado con JWT aún vigente deja de enumerar empresas y recibe HTTP 403 en su contexto anterior.

No cambia código de ejecución, DB schemas, API, migraciones o roles reales. El JWT y las cuentas de esta suite son ficticios, no secretos del proyecto.

## Límites explícitos y aceptación pendiente

- Esto NO es una prueba HTTP en un servidor DEV remoto ni usa MySQL real. EF InMemory, aunque pruebe la lógica de servicio sobre contexto persistente de prueba, no certifica SQL físico, despliegue, índices, transacciones ni conectividad de Aiven. Tampoco verifica la ruta administrativa HTTP de activación de membresía: modifica el fixture y observa el efecto en la siguiente solicitud HTTP.
- La microtarea B requiere que las cuatro pruebas ejecuten efectivamente dotnet test backend/tests/Solqaryn.Tests/Solqaryn.Tests.csproj --configuration Release --filter FullyQualifiedName~P1010B con tests encontrados >0 y sin fallos, y que toda CI aplicable, incluido Scope Lock, sea terminal PASS en el SHA de PR exacto. Los resultados de P1-010.A no se reutilizan como prueba de B.
- Si el contrato de aceptación exige MySQL/HTTP DEV real para certificar B, ejecutar una prueba dirigida adicional solo en DEV y con autorización, aislamiento y limpieza demostrable; sin ese readback, dejar B en VALIDANDO o EN_PROGRESO con brecha explícita, nunca LISTO.
- Para fusionar: PR protegido en dev, revisión y controles efectivos sin autoaprobación, SHA verificado, CI posmerge exact SHA (GET check-runs + workflows push); LISTO únicamente por otra Task autorizada y distinta de la autora con TECH_REVIEW_PASS válido, readback PLAN/COLA/BITACORA y evento único.
- Rollback: revertir por PR protegido el archivo de tests y esta acta, preservando evidencias institucionales; no tocar datos comerciales.
