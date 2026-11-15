# SOLQARYN V17 — C15-01.M03.04 adaptador aislado de cobertura de requisitos

**Task autor:** PRIMARY DEV 06, ID `6af16bbad26881918c283abe06e1f32a`. **Riesgo:** R2 por fuente institucional/concurrencia; `dev` exclusivamente, Scope Lock STRICT.

## A0 — Contrato implementado e impacto
`C1501RequirementCoverageSnapshotAdapter` es código Infrastructure .NET 10 **read-only**, compone el servicio Application `V5RequirementCoverageProjectionService` y el contrato Domain `V5RequirementCoverageRules`. `IC1501TrustedRequirementSnapshotSource` es puerto para futuro lector autenticado de GitHub/Standard/PLAN; aquí no hay conexión real a Sheets, ni adaptador Aiven, migración, cambios de fórmula, escritura ni dispatcher. Verifica SHA origen de 40 hex exactos, autorizaciones de lectura y auditoría del lector, catálogo oficial por coincidencia ordinal, mapeo único, control de tamaño (máx 2340) y estado PLAN válido. No interpreta un LISTO como test real o A5.

Los predecesores/autoridad se obtienen de PLAN_MAESTRO 2177, COLA G/O=SI y DAG F=VALIDADO/G=SI, con candado V17 `2000002177`. Los 4 archivos de entrega son Infrastructure, xUnit, este documento y CHANGELOG autocontenido. Sin cambios a API, Angular, JWT, RBAC, EF o datos reales.

## A1 — Oráculos xUnit
Doce [Fact] para flujo positivo todavía pendiente de evidencia externa, entradas inválidas antes de I/O, overrides cliente, SHA falsificado, revisor revocado, catálogos desconocidos, caso HashSet ordinal-insensible (regresión), duplicados, HEAD movido, estado inválido, fuente nula y cancelación. Tests reales deben correr con GitHub-hosted .NET 10; el texto no prueba PASS.

## A2 — Seguridad, integridad y correspondencia
Rechaza catálogos no exactos incluso si `IReadOnlySet` usa un comparador que acepte diferencias de mayúsculas/minúsculas; no confía en flags del cliente, ni autoriza permisos, A5, escrituras institucionales, bypass tenancy, promoción de ambiente o declaración de cobertura. Las huellas de git, estado PLAN y lectura autorizada de fuentes deben revalidarse en adaptador real separado.

## A3 — Integración y límites
La composición Infra->Application->Domain se valida con xUnit .NET 10. Consultas reales GitHub/Drive, MySQL 8.4, EF/migraciones, navegador, HTTP y producción son **NO_APLICA** al diff aislado; no se presentan como PASS, y la cobertura total queda NO_MEDIDO hasta leer fuentes reales y verificar requisitos sin huérfanos.

## A4/A5 — Evidencia y rollback
Backend Release + xUnit, seguridad multiempresa, autorización pertinente, Scope Lock y V5 deben estar `completed/success` en SHA exacto o equivalente justificado por blobs; `queued`, `in_progress`, `failed`, `skipped` o `cancelled` nunca PASS. Registrar estado PLAN/COLA/DAG, BITACORA A:Z, candado V17 y readbacks por transición. `git revert` normal, sin forzar HEAD, borrar historial o tocar QA/main/PROD. A5 requiere otra Task Javier distinta de DEV06.
