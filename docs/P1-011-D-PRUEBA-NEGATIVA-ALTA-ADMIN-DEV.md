# P1-011.D — Ejecución dirigida del negativo de alta atómica (DEV)

## Autoridad y alcance
- Microtarea P1-011.D, PLAN_MAESTRO fila 61, orden 60, depende de P1-011.C=LISTO. Único plan comercial autorizado: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES.
- Autor físico: ALEX :10, SECONDARY_REVIEW_01, TASK_ID `6adbbc9ad9e88191bea8eb69104afb25`, GitHub `morales35alex`.
- Reclamo condicional de `PLAN_MAESTRO!H61` desde PENDIENTE a EN_PROGRESO (1 coincidencia), con `I61` propietario, `COLA!E61` derivada, evento `SQ-P1-011-D-ALEX10-CLAIM-99A2EADD-20261105T1511`. Rama exclusiva `work/solqaryn-p1-011-d`; base `dev@99a2eadd8503b1de15baf53da7a52e454c2bfe9a`.
- Impacto: **solo prueba de integración .NET, esta acta y changelog**. Sin cambios de API, DTO, configuración de proveedor, migraciones, JWT, RBAC, tenant, secretos, QA/main/PROD ni infraestructura.

## Negativo reproducible real
- Comando dirigido (con **MySQL 8.4 efímero y `ConnectionStrings__DefaultConnection` proporcionado por CI**, nunca por este documento):

  `dotnet test backend/tests/Solqaryn.Tests/Solqaryn.Tests.csproj --filter "FullyQualifiedName~P1011D" --no-restore`

- El nuevo test `P1011D_AltaAdministradorNuevo_AuditoriaFallida_NoDejaPersistenciaParcial` tiene `Category=Integration` y **ejecuta**, no inspecciona solamente, la integración reutilizable `PlataformaEmpresasMySqlTransactionIntegrationTests.AltaA_B_EsTransaccional_ConAuditoriaYRollbackEnMySqlReal`.
- El fixture real utiliza MySQL, nombres sintéticos, interceptación `SimulatedAuditFailure`, y crea una base independiente por ejecución. Debe rechazar `Empresa rollback auditoria nueva` con admin `p4-rollback-audit` por `InvalidOperationException`; verifica con nuevo `DbContext` e `IgnoreQueryFilters` que ninguno de los usuarios/empresas fallidos persiste y que los recuentos de `ConfigEmpresa`, `UsuarioEmpresa` y `RegistroAuditoria` son exactamente los esperados; destruye la base en `finally`.
- La integración ejecutada también valida los escenarios baseline de éxito y las fallas por configuración/membresía. La envoltura dirigida causa una **segunda ejecución aislada** de esa integración cuando una suite corre ambos Facts. No es una implementación duplicada del caso ni una prueba HTTP/JWT en un servidor.
- **FAIL**: cualquier alta parcial, cambio de recuentos, ausencia del filtro `P1011D`, excepción diferente o falta de servicio MySQL. No convertir `SKIPPED` ni CI anterior en PASS.

## Evidencia y límites de certificación
- Estado inicial: código y acta preparados, **pruebas aún no certificadas** en este commit. Los resultados se vincularán al HEAD exacto del PR y al job MySQL si dicho runner lo ejecuta. Sin .NET/MySQL disponibles localmente en esta ejecución; los checks de GitHub decidirán el resultado.
- Para entregar VALIDANDO: PR a `dev`, SHA, pruebas dirigidas MySQL ejecutadas con recuento >0 y cero fallos, CI aplicable exact-SHA, Scope Lock y readback GitHub/PLAN_MAESTRO/COLA/BITACORA. El autor libera custodia después de probarlo; revisión y LISTO requieren otra Task autorizada.
- Rollback: revert protegido del changeset de prueba/acta/changelog. Sin manipulación de datos productivos.
