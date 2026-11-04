# P1-012.B — Certificación negativa del alta con administrador registrado (DEV)

## Autoridad y propiedad

- Plan Comercial Inicial Unificado V3, microtarea **P1-012.B**, orden 48, `PLAN_MAESTRO!49`; depende de **P1-012.A=LISTO** (PR #3852 integrado a `dev`).
- Task autora: **SECONDARY_REVIEW_03 / ALEX_CHATGPT**, `6adcc98a80a48191b71a5156624c21df`, GitHub `morales35alex`.
- Rama única `work/solqaryn-p1-012-b` desde `dev@89b3687d14336734788bda26e2e187f0eea763c2`; claim y readback institucional `SQ-P1-012-B-ALEX50-CLAIM-89B3687D-20261104`, `BITACORA!578`.
- `PROJECT_SCOPE_LOCK=STRICT`, WIP 6/1/1, un scope propio; sin cambios en QA/main/PROD ni credenciales, permisos, bases de datos o despliegues.

## Impacto delimitado

La regresión existente `P1012A_PlataformaCreaEmpresaConAdminRegistradoSinEscaladaGlobal` y el flujo HTTP `POST /plataforma/empresas/alta-controlada` se conservan. Se agregan únicamente dos pruebas xUnit `P1012B_` al fixture ya existente `PlataformaEmpresasOnboardingSecurityTests`. No cambia código de ejecución, API/DTO, esquema, migraciones, roles productivos o política de autenticación.

1. **Reintento duplicado:** dos solicitudes con nombre de empresa idéntico y el mismo usuario preexistente producen primero `201 Created` y después `409 Conflict`; no se duplica la identidad, empresa, membresía, configuración ni auditoría, ni se otorga rol de plataforma.
2. **Aislamiento de rol privado A/B:** usuario con membresía A y rol personalizado restringido a empresa A pasa a administrar una nueva empresa B solo mediante una plantilla empresarial del sistema; conserva el rol privado A exclusivamente en A, su `Usuario.RolId` original y dos membresías de empresa distintas. No recibe un rol de plataforma ni duplica configuración o auditoría.

Se reutilizan instancias EF Core **InMemory**, identidades de test y `PlataformaEmpresasController` real del repositorio. Estas pruebas verifican lógica y efectos observables del controlador, **NO** la ruta HTTP/JWT de un servidor DEV externo, transacciones físicas MySQL, índices, unicidad SQL en carreras concurrentes ni valores de flags de un despliegue. La prueba MySQL del proyecto se rige por CI separada; nunca inferir un PASS por esta acta.

## Cierre exigido

- Ejecutar `dotnet test backend/tests/Solqaryn.Tests/Solqaryn.Tests.csproj --configuration Release --filter FullyQualifiedName~P1012B`, verificando **al menos dos pruebas descubiertas**, dos éxitos y cero fallos mediante salida real o TRX.
- Verificar regresión `P1012A`, Scope Lock `AUTO_CERT=PASS`, controles JWT/RBAC/tenancy, checks obligatorios y todos los checks aplicables **exact-SHA** del PR.
- Revisión técnica por otro **TASK_ID** realmente autorizado y diferente; no confundir con `APPROVED` humano de otro GitHub login. Autor no revisa ni cierra su propia entrega.
- Solo merge protegido a `dev` cuando el CI final y las reglas efectivas lo permitan, seguido de CI posmerge exact SHA y readback de `PLAN_MAESTRO`, `COLA` (sin editar fórmulas) y `BITACORA`. Mantener `VALIDANDO` si los gates siguen pendientes.
- Rollback mediante revert de los archivos de prueba y acta en PR protegido, preservando historial y trazabilidad institucional.
