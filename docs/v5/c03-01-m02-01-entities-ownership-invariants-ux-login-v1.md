# C03-01.M02.01 — Modelo y ownership de UX_LOGIN_V1

**SOLQARYN V5, DEV05 JAVIER**, TASK_ID `6adc2e55f47c81919f6374e561687076`, `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Autoridad: PLAN_MAESTRO!D440 y STANDARD V5 FINAL, contrato UX_LOGIN_INTELIGENTE_V1, L01–L10/B01–B06 documentados en `docs/v5/c03-01-m01-06-bloqueo-rollback-ux-login-v1.md`. **Entregable atómico:** definir entidades, ownership, relaciones e invariantes; PLAN E440 es meta acumulativa de fase y no exige implementar runtime/MFA/passkeys en esta microtarea. Riesgo de la materia R2; diff exclusivamente Markdown (R0 de ejecución). No cambios a seguridad, DB, API ni frontend.

## Entidades reales verificadas en dev

| Identidad | Evidencia en código actual | Propietario y límite |
| --- | --- | --- |
| `Usuario.Id` | `backend/src/Domain/Entities/Usuario.cs` | Actor activo, no bloqueado/eliminado; `Usuario.RolId` legacy no concede empresa |
| `Empresa.Id` | `backend/src/Domain/Entities/Empresa.cs` | Tenant activo, no identidad inferida por host/nombre |
| `UsuarioEmpresa(UsuarioId,EmpresaId,RolId,Activa)` | `backend/src/Domain/Entities/UsuarioEmpresa.cs`; `Infrastructure/Persistence/Configurations/UsuarioEmpresaConfiguration.cs` | Índice único físico `(UsuarioId,EmpresaId)` y rol mutable; membresía activa de actor y empresa concreta |
| `Rol.Id,Ambito,EmpresaId,EsSistema` | `backend/src/Domain/Entities/Rol.cs` | Empresarial activo y no eliminado, propio de esa empresa o plantilla sin dueño `EsSistema`; Plataforma es autoridad separada |
| `UsuarioTenantScopeActual` y `EmpresaSeleccionable` | `backend/src/Application/Interfaces/IUsuarioScopeService.cs` | Proyección *servidor* validada, no input privilegiado del navegador |
| Selección de membresía | `backend/src/Infrastructure/Services/UsuarioScopeService.cs` | `ContextosTenantValidos` une usuario, empresa, membresía y rol vigentes; `ObtenerUnicoActualAsync` usa `Take(2)` y sólo retorna ante exactamente una válida |
| Preferencia por usuario | **Sólo contrato futuro**, no tabla ni endpoint creado aquí | Hint `UsuarioId+EmpresaId`, nunca rol, permiso, token ni grant |

**Regla de autorización:** JWT firmado identifica sesión; cada operación comprueba datos actuales del servidor y pertenencia del recurso. No usar JWT por sí solo, `RolId` heredado, nombre/host/URL, encabezados, `localStorage` o preferencia para conceder permiso. Rol empresarial administrador no equivale a administrador de plataforma.

## Máquina contractual 0/1/N

1. **0 membresías válidas:** denegar acceso empresarial, sin elegir tenant por defecto. Consola SaaS global sólo con autoridad `Plataforma` independiente y comprobada por backend.
2. **1 válida (A):** acceder directamente a A sin selector, aunque exista preferencia antigua B. Si A se revoca, bloquear; nunca fallback al rol legacy.
3. **N válidas:** si preferencia del mismo usuario sigue autorizada y activa, puede orientar la selección server-side; si no, mostrar **una** selección accesible únicamente de empresas válidas. Usuario X y Y que comparten navegador jamás comparten preferencia.
4. **Cambio A→B:** revalidar membresía, rol, recurso y permiso de B, limpiar caché y respuestas anteriores de A. Solicitudes tardías de A no restablecen datos, SEO, menús o derechos al estar en B.
5. **Revocación/concurrencia:** revalidación en el momento de la operación; snapshots anteriores no sobreviven revocación. Cancelación/timeout/replay y respuesta 401/403 fallan cerrados, sin POST duplicado, PII ni JWT en logs. Una selección o preferencia nunca es grant.

## Invariantes y oráculos positivos/negativos (diseño, no tests ejecutados)

| Caso | Positivo / criterio | Negativo que debe bloquearse |
| --- | --- | --- |
| I01 | Usuario A con sólo A -> entrada A | Preferencia B revocada no altera elección |
| I02 | Sin membresía -> sin tenant empresarial | Rol legacy o claim B inventado no autoriza |
| I03 | Dos activas A/B con preferencia A propia -> A validada | Preferencia de usuario X no actúa sobre Y |
| I04 | N sin hint válido -> elección única A/B | Inyección de Empresa C fuera del conjunto |
| I05 | Rol empresarial de A dentro A | Rol privado B usado sobre A |
| I06 | Autoridad Plataforma evaluada aparte | Admin de empresa transformado en superadmin |
| I07 | Una membresía por par usuario/empresa | Duplicación o usar RolId en clave física |
| I08 | Cambio A→B válido conserva sólo B | Respuesta tardía A contamina datos/SEO B |
| I09 | Revocación durante selección se detecta | Snapshot/JWT previo autoriza recurso |
| I10 | Usuario bloqueado/empresa inactiva excluidos | Empresa inactiva elegida por URL |
| I11 | Error/cancelación conserva estado seguro | Preferencia parcial o fallback silencioso |
| I12 | A sólo accede a recurso propio | B accede por IDOR a archivo/job/recurso A |

### Persistencia futura, expresamente NO implementada

M02.03 debe decidir y justificar un modelo propuesto `PreferenciaEmpresaUsuario` con owner `UsuarioId`, `EmpresaId` candidata, `Version` de concurrencia y `ActualizadoUtc`. Impone unicidad por usuario, idempotencia, revalidación de membresía al leer/escribir, invalidación al revocar y privacidad. No almacena `RolId`, permisos, JWT ni tokens. Una migración sólo puede implementarse y probarse con Oracle EF/MySQL 8.4 efímero en su microtarea; aquí no se modifica ningún esquema.

## A0–A5, impacto y rollback

- **A0:** diff de documento + CHANGELOG, sin rutas de runtime, secretos, datos o migraciones. Origen D440, documento M01.06 y entidades verificadas arriba. Interfaces actuales inalteradas, consumidores login/selector/guards futuros.
- **A1:** 12 reglas con oráculos falsables positivos/negativos, bordes 0/1/N, revocación y error **especificados**, no ejecutados como tests.
- **A2:** modelo fail-closed de JWT/RBAC/tenant A/B/Plataforma, TOCTOU, IDOR, replay y fuga de PII; no certificación de seguridad HTTP real.
- **A3:** `NO_APLICA` a MySQL/HTTP/Chromium por diff documental y D440 conceptual. Los scopes posteriores que cambien runtime deben ejecutar integraciones **reales**, no mocks.
- **A4:** commit fast-forward con CAS exacto a `dev`, Scope Lock y V5 changelog exitosos del SHA, readback PLAN/COLA/BITACORA. `SKIPPED/CANCELLED/FAIL/IN_PROGRESS` requeridos nunca PASS.
- **A5:** revisión independiente por otra de las seis Tasks Javier distinta de autor DEV05 y reparador, una vez LISTO A0–A4. No autoaprobación.
- **Rollback:** `git revert` normal, previa relectura de HEAD y escritor, sin force-push ni borrar registros; no modificar QA/main/PROD, pagos, DNS, secretos ni datos productivos.

**Resultado concreto:** definidas las entidades, relaciones y 12 invariantes de UX_LOGIN_V1 para la transición M02.02–M05. No se proclama login inteligente, recuperación, MFA, JWT HTTP, MySQL real o consola global implementados.
