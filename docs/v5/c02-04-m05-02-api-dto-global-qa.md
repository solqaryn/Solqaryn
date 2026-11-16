# C02-04.M05.02 — Contrato API/DTO real y errores seguros de consola global

**Task:** PRIMARY DEV 01 — JAVIER; **rama:** dev; **riesgo:** R2 (autoridad global, JWT/RBAC, multitenancy). **Estado:** la ejecución de GitHub Actions por el SHA de entrega es condición necesaria antes de certificar A0–A4 o LISTO_DEV. Sin cambios de producción.

## A0 — Alcance comprobable y límites

Este incremento agrega pruebas de regresión xUnit al controlador **existente** `PlataformaGestionMvpController`, sin instalar endpoints o DTOs futuros. Se comprueba el contrato real de `GET /plataforma/gestion/roles` y `GET /plataforma/gestion/usuarios/buscar`, protegidos por `[Authorize]` y revalidación del grant persistido mediante `PlataformaScopeService`. La consulta controlada es API privada global, nunca lectura empresarial por un cliente arbitrario.

- **Entrada válida:** autoridad de plataforma vigente; búsqueda exacta devuelve solo DTO minimizado y registra auditoría global sin empresa.
- **Entradas inválidas:** empresaId negativo, username corto/malformado, rol solo empresarial, grant global revocado.
- **Salida segura:** `BadRequest`/ `Forbid` antes de consultar recursos o registrar auditoría; errores no reflejan payload o credenciales; `Cache-Control: no-store` en respuestas comprobadas.
- **Arquitectura:** API y pruebas existentes; Domain, Application, Infrastructure, EF/migraciones, Angular, claims, RBAC y DTOs de producción no se modifican.
- **No inventar:** el documento M02.02 describe futuros contratos MFA/reautenticación/grants y evidencia QA; no existe por ello autorización para publicar nuevos endpoints. La API de evidencia QA automática no se inventa ni se certifica como live en este scope.

## A1/A2 — Oráculos ejecutables que pueden fallar

Pruebas agregadas a `backend/tests/Solqaryn.Tests/API/Controllers/PlataformaGestionMvpSecurityTests.cs`:
1. Límite `empresaId=-1` y `int.MinValue`: respuesta 400, sin divulgación o auditoría.
2. Nombre de usuario mal formado (corto, con espacios y con markup): 400 sin eco de entrada ni consulta/auditoría.
3. JWT sintético autenticado pero **sin grant global persistido**: 403 en catálogo y búsqueda, sin auditoría de éxito.
4. Revocación persistida del grant: rechazo de nueva búsqueda, sin confiar en la identidad anterior.
5. Búsqueda global autorizada: DTO reducido, ausencia de hash de password y secreto sintético, no-store y auditoría de ámbito Plataforma sin EmpresaId.

Los tests usan **EF InMemory y claims sintéticos**, por lo cual estos asserts **no acreditan HTTP JWT real ni motor MySQL**. A1/A2 quedan condicionados a ejecución real de xUnit en GitHub-hosted por SHA.

## A3/A4 — Evidencia exigida antes del cierre

Exigir CI del SHA publicado en `dev`: `Backend Release y pruebas`, `SOLQARYN - Seguridad multiempresa`, `verify-solqaryn-project-scope`, `V5 - Sin encabezados vacios ni atribucion cruzada` y otros checks requeridos aplicables. Evaluar también el laboratorio HTTP JWT/RBAC/tenant A/B MySQL 8.4 cuando sea causal y observar ejecución real de sus fases; ningún build verde o test InMemory sustituye ese laboratorio. `QUEUED/IN_PROGRESS/SKIPPED/CANCELLED` no constituyen PASS.

Sin cambios de esquema o proveedor. Los controles de MFA, autorización global de otros endpoints y certificación de evidencias QA almacenadas siguen fuera de alcance. **Rollback:** `git revert` del commit de este incremento, sin reescribir historia ni borrar BITACORA. **A5:** revisión independiente por otra Task Javier de distinto ID, después de A0–A4, antes de liberar sucesoras.
