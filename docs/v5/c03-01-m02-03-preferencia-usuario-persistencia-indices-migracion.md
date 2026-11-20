# C03-01.M02.03 — Contrato de persistencia e índices UX_LOGIN_V1

**SOLQARYN V5, PRIMARY DEV05 JAVIER**. TASK_ID `6adc2e55f47c81919f6374e561687076`, `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Fuente institucional: PLAN_MAESTRO!D442, STANDARD V5 FINAL/UX_LOGIN_INTELIGENTE_V1, anexos `C03-01.M01.06` y documentos `C03-01.M02.01/02`. **Entrega exclusiva:** definir persistencia, índices, migraciones reversibles y datos de prueba de *última empresa por usuario*, sin mezclar cuentas ni guardar permisos en navegador. La meta E442 es acumulativa; aquí no se crean tablas, migraciones, ORM, datos ni endpoints. Riesgo conceptual R2; diff sólo documental.

## Estado actual comprobado en `dev`

- `backend/src/Domain/Entities/UsuarioEmpresa.cs` contiene `UsuarioId,EmpresaId,RolId,Activa` y un contrato de unicidad lógica; `backend/src/Infrastructure/Persistence/Configurations/UsuarioEmpresaConfiguration.cs` materializa índice `UX_UsuarioEmpresas_UsuarioId_EmpresaId` **único**, índices `IX_UsuarioEmpresas_EmpresaId_Activa` y `IX_UsuarioEmpresas_RolId`, relaciones a Usuario, Empresa y Rol con `DeleteBehavior.Restrict`.
- `Usuario.Id` es actor; `Usuario.Activo`, `Bloqueado`, `Eliminado`, `Empresa.Activa`, `Rol.Ambito/EmpresaId/EsSistema/Activo/Eliminado` forman condiciones de autorización. `Usuario.RolId` **legacy** jamás concede tenant.
- `UsuarioScopeService.ContextosTenantValidos` consulta las membresías válidas actuales con EF; su `ObtenerUnicoActualAsync` devuelve sólo un scope para exactamente una membresía. `ListarEmpresasSeleccionablesAsync` proyecta únicamente empresas que corresponden al actor.
- **No se confirma** actualmente una entidad o tabla persistente específica de preferencia UX_LOGIN_V1. No inferir que `localStorage` o el JWT constituya persistencia autoritativa.

## Modelo propuesto de datos (NO implementado)

**PreferenciaEmpresaUsuario**, sólo si M03 justifica persistirla: `UsuarioId` (PK/unique, FK Usuario, dueño), `EmpresaId` (FK Empresa, valor no autoritativo), `Version` (entero positivo o token de revisión para CAS), `ActualizadoUtc` (fecha UTC), `RevocadaUtc?` (marcador opcional) y metadatos de audit mínimos sin PII sensible. No se almacena `RolId`, contraseña, refresh token, JWT, permiso, identidad global o dato de otro actor. **No copiar `EmpresaId` desde encabezado o host sin revalidar membresía actual.**

Relaciones y cardinalidades: Usuario 0..1 preferencia (no N preferencias concurrentes); Empresa 0..N preferencias, pero una preferencia puede quedar **obsoleta** sin otorgar permisos; Usuario 0..N UsuarioEmpresas activas; par UsuarioEmpresa único y rol mutable. Separar tabla de membresías (autoridad) y preferencia (hint). La **validez efectiva** de una preferencia es `Preferencia.UsuarioId == Actor.Id` AND existe membresía `UsuarioEmpresa` activa para ese UsuarioId/EmpresaId AND Usuario/Empresa/Rol vigente y ámbito correcto. Con 1 empresa válida, ignorar hint incompatible; con N elegir sólo una preferencia inequívoca validada o exigir selección. Sin empresas: denegar.

**Concurrencia/TOCTOU:** escribir con comprobación de actor real y membresía vigente dentro de transacción, `Version` esperada, clave idempotente por operación y valor normalizado; carrera entre revocación y preferencia fuerza fallo cerrado. Leer no garantiza autoridad posterior: revalidar al confirmar y en operaciones de recursos. Preferencias antiguas se invalidan lógicamente; no borrar historial operativo ni miembros vigentes por revocar hint.

## Índices y migración futura, propuesta no ejecutada

| Restricción | Necesidad | Oráculo |
| --- | --- | --- |
| `PRIMARY/UNIQUE (UsuarioId)` en preferencia | Aislar actor y evitar elección doble | Insert duplicado o CAS obsoleto rechaza/actualiza de modo controlado |
| `FK UsuarioId -> Usuarios.Id` con borrado restrictivo | No heredar preferencia entre cuentas | Eliminar/recrear usuario no recupera hint de otro ID |
| `FK EmpresaId -> Empresas.Id` y validación membresía **en aplicación** | Integridad de referencia sin confundir FK con permiso | Empresa vigente sin membresía del actor NO queda autorizada |
| `(UsuarioId,EmpresaId)` único en `UsuarioEmpresas` **existente** | Evitar duplicar roles/owners | Dos membresías del mismo par no se crean |
| Control de versión por UsuarioId | Evitar carreras en último tenant elegido | A escribe v2; B con v1 fracasa; nada rompe la sesión |
| Índices audit mínimos por actor/fecha si exigidos | Consulta razonable, sin PII | No indexar JWT, contraseña, cookies ni claims secretos |

**Secuencia propuesta para unidad de implementación, no realizada hoy:** (1) auditar esquema y migración EF actual, inventario/baseline y nombres de constraints; (2) migración **aditiva** compatible con cero preferencias, sin backfill global por tenant; (3) despliegue reversible sólo en MySQL **8.4 efímero** de GitHub-hosted; (4) prueba de Up, inserción 0/1/N, índices, FK y transacción, rechazo de duplicados; (5) rollback Down bajo precondiciones sin borrar Usuarios, UsuarioEmpresas ni BITACORA, preferencia hipotética sólo si se demuestra pérdida de datos aceptable; preferir feature flag seguro y migración aditiva frente a Down destructivo; (6) readback de esquema, filas y versión exacta del job. Sin permiso adicional NO tocar Aiven live, QA/main/PROD ni ejecutar migraciones contra proveedor real.

## Fixtures adversariales exigibles en CI de la futura unidad EF/MySQL

| Caso | Datos de prueba efímeros | Resultado esperado / negativo |
| --- | --- | --- |
| P01 | U1 con A únicamente, hint B | Elegir A; B no autoriza |
| P02 | U1 sin membresías, `Usuario.RolId` legacy | Denegar, sin fallback |
| P03 | U1 con A/B; preferencia A vigente | A elegible tras revalidación, sin copiar a U2 |
| P04 | U2 sólo B, navegador compartido con U1 | U1.A no se lee ni autoriza U2 |
| P05 | U1 hint B, membresía B revocada | Ignorar B, invalidar hint, no filtrar B |
| P06 | Empresa B inactiva o rol B no asignable | B fuera del conjunto; ni FK ni JWT conceden |
| P07 | U1 A/B, intento de escoger C de U3 | 403 fail-closed, sin preferencia persistida |
| P08 | Doble inserción para mismo UsuarioId | Constraint único o CAS serializado, sin dos preferidas |
| P09 | Writer con versión vieja tras otro cambio | Conflicto versionado, sin sobrescribir hint actual |
| P10 | Error/cancelación en medio de transacción | Rollback completo y cero cambios parciales |
| P11 | Revocación de B concurrente con selección | Denegación en comprobación final |
| P12 | Migración Up/rollback seguro en MySQL aislado | Conserva Usuarios, UsuarioEmpresas e historia audit tras ensayo |

Los 12 fixtures se **definen**, no se ejecutan ahora. P01–P12 no certifican MySQL 8.4, migraciones, JWT o API sin logs reales, SHA y `Category=Integration` del futuro alcance.

## A0–A5, impacto y seguridad

**A0:** salida D442, fuentes EF/domino reales, diff sólo docs+CHANGELOG, sin modificaciones a interfaces, esquemas, API, datos, pipelines ni secretos. **A1:** doce oráculos positivos, negativos, frontera y concurrencia documentados, no pruebas ejecutadas. **A2:** usuario/empresa/rol, A/B, revocación, preferencias no autoritativas, versionado y denegación por defecto; no claim de seguridad runtime. **A3:** `NO_APLICA` MySQL/HTTP/Chromium para este cambio documental; imprescindible CI de integración real en futura tarea que materialice persistencia. **A4:** fast-forward CAS GitHub, checks Scope Lock/V5 `completed/success` SHA exacto, readback PLAN/COLA/BITACORA; checks `SKIPPED/FAIL/CANCELLED/IN_PROGRESS` exigibles no PASS. **A5:** Task Javier distinta del autor DEV05 y reparador revisa genuinamente.

**Rollback de esta entrega:** `git revert` documental normal en dev con HEAD revalidado, sin force-push ni borrar eventos. **No autorización R3:** ninguna migración live, secreto, QA/main/PROD, DNS o pago. La entrega define persistencia, índices, plan reversible y fixtures, pero **no crea** preferencia persistente ni certifica el flujo UX_LOGIN_V1.
