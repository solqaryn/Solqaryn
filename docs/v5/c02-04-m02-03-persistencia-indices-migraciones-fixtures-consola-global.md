# SOLQARYN V5 FINAL — C02-04.M02.03: contrato de persistencia, índices, migraciones y fixtures de consola global

**Naturaleza:** diseño DOCUMENTAL de M02.03, **sin migración, DDL o acceso privilegiado ejecutado**. **Autor:** PRIMARY DEV 06 — JAVIER_CHATGPT, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Repositorio:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Autoridades:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, Sheet institucional `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU` PLAN/COLA/DAG/BITACORA.

## 1. Objetivo, estado real y no-alcance

C02-04 propone una consola global **separada del ERP** con identidad de operador de plataforma, reautenticación/MFA por operación sensible, motivo, autorización de excepción acotada por acción/recurso/empresa/tiempo, auditoría crítica y revocación. Esta unidad define únicamente el **diseño físico futuro y las reglas de integridad** de esa propuesta. No crea rutas, DTO, endpoints, IAM, grants, tokens MFA, tablas, índices, migraciones, worker, outbox, mensajes, permisos reales, cambios de JWT, datos ni cuentas.

El código existente, según M01.03 y M02.01–02, dispone de `PlataformaScopeService` que consulta privilegio global vigente en backend y `PlataformaEmpresasController` de operaciones específicas; **no es una implementación de grants excepcionales con MFA reciente y TTL**. `PlatformSupportPolicy` tiene allowlist DEV reducida a GET, **nunca** elevación de permisos empresariales o de plataforma. `UsuarioEmpresa` vincula roles empresariales; su `RolId` no es autorización de la consola.

No se añade una tabla para suplir un requisito de diseño sin threat model, aprobación de retención/privacidad, pruebas de control de acceso y rollback real. Ningún esquema abajo está desplegado en MySQL.

## 2. Fuentes y contratos actuales de `dev`

| Referencia verificada en los contratos previos | Blob de fuente | Límite que permanece |
| --- | --- | --- |
| `backend/src/Infrastructure/Services/PlataformaScopeService.cs` | `2a71a56750805ef513c7f312fb6c6c2d53a8c314` | Revalida rol global persistido; no emite permisos temporales ni MFA |
| `backend/src/API/Controllers/PlataformaEmpresasController.cs` | `87fbf187b7993ba0f05edff393500f24d0856833` | Endpoints precisos `plataforma/empresas`, no API de elevación transversal |
| `backend/src/Application/Interfaces/IPlataformaScopeService.cs` | `ba8f5d313117cc4dd4e20a56e2c1959c724544cd` | Contexto de plataforma limitado, no prueba reautenticación fuerte |
| `backend/src/Application/Security/PlatformSupportPolicy.cs` | `b103f0b0ec7b9169c060661f6a4456f8863152cf` | Soporte DEV de lectura restringido, sin escrituras ni tokens privilegiados |
| `docs/v5/c02-04-m01-06-bloqueo-rollback-liberacion-consola-global.md` | `37ffd150c0b0e67bbc7731198f283758c38fdee7` | Gate de preparación M01, no autorización de implementación |
| `docs/v5/c02-04-m02-01-entidades-ownership-invariantes-consola-global.md` | `791a1cc82d0cd51c0d3515b88290072395593c7f` | Modelo conceptual futuro: operador global, MFA/reauth, concesión, auditoría |
| `docs/v5/c02-04-m02-02-api-dto-errores-compatibilidad-consola-global.md` | `4c1b3444bccae8a6899e82b68a2cbf53141b4f4b` | API/DTO futuros, preservar contratos actuales; aún no endpoints nuevos |

Las referencias son blobs y contratos revisados previamente, no una certificación de grants físicos en Aiven. Al ejecutar M03 deberán volver a contrastarse fuentes, SHA, versión de MySQL/EF y migraciones efectivamente aplicadas.

## 3. Modelo de persistencia hipotético (sin DDL generado)

| Objeto futuro conceptual | Claves/campos mínimos propuestos | Restricciones obligatorias antes de implementarlo |
| --- | --- | --- |
| `PlatformOperatorAuthorization` | Operador activo, rol GLOBAL autorizado, versión de política, estado | Autoridad derivada de base de datos de plataforma, no de `Usuario.RolId`, JWT, EmpresaId del cliente ni miembro ERP |
| `StrongAuthenticationProof` | `ProofId` opaco, `OperatorId`, `ActionFingerprint`, `IssuedAtUtc`, `ExpiresAtUtc`, `ConsumedAtUtc`, `FactorPolicyVersion` | Prueba de MFA/reauth reciente, vinculada a operación/scope, un solo uso, no guardar OTP, JWT, seed o clave MFA |
| `ExceptionalAccessGrant` | `GrantId`, operador, acción allowlisted, empresa/recurso target autorizado, motivo seguro, `StartsAtUtc`, `ExpiresAtUtc`, revocación, `PolicyRevision`, `CreatedBy` | TTL corto por política aprobada, índices para expiración y revocación, scope exacto, sin wildcard de tenants, sin privilegio por omisión |
| `ExceptionalAccessApproval` | `GrantId`, revisor independiente si política lo exige, decisión, timestamp, rol de aprobación | Autoría y aprobación diferenciadas; no autorevisión cuando se exija separación de funciones |
| `PlatformCriticalAudit` | `AuditId`, operador, operación, target opaco, grant/revisión, decisión, motivo normalizado, correlación, sello temporal | Registro durable, append-only lógico, ausencia de PII/secretos y resultado sólo tras frontera transaccional aprobada |
| `PlatformOperationIdempotency` | Identidad de operador, scope, clave opaca hash, `ActionFingerprint`, revisión y resultado minimizado | Unique por operador+operación+scope+clave, sin replay cross-tenant ni reutilización para ampliar permisos |

**Invariantes adicionales:** expiración estricta en UTC, `ExpiresAtUtc > StartsAtUtc` y nunca más allá del máximo aprobado; `RevokedAtUtc` impide uso aunque TTL no haya vencido; `ConsumedAtUtc` de prueba fuerte evita reutilización; claves hash/índices de unicidad deben contemplar espacio del tenant y el scope. Las FK/índices proyectados requieren nombres reales de entidades, pruebas de backfill, integridad referencial y compatibilidad MySQL 8.4 antes de generar una migración. **Un índice único, rol empresarial o documento firmado no concede autorización.**

## 4. Transacción, integridad y privacidad de diseño

Una futura operación excepcional no puede ejecutarse sólo porque existe un registro `ExceptionalAccessGrant`: verificar **en la operación** operador plataforma activo, reautenticación/MFA vigente y vinculada al target, autorización concreta, motivo válido, TTL, ausencia de revocación, membresía/owner cuando se toca recurso de empresa, permisos propios, cuota y auditoría crítica. Si cualquiera falla, denegar sin leer datos B ni escribir fuera de A.

En implementación autorizada, la comprobación de versión y consumo de grant se debe realizar en transacción compatible con MySQL/EF y resolver carreras con predicado CAS/locking verificado; el resultado del intento, el registro idempotente y la auditoría deben reconciliarse sin doble efecto. Si el audit sink durable falla, **no** realizar una operación crítica irreversible ni declarar éxito. Nunca guardar códigos OTP, semillas MFA, credenciales o payload de empresa B en logs o tablas auxiliares.

Los eventos de auditoría no autorizan un dispatcher ni worker; la BITACORA institucional de microtareas A:Z es distinta de un eventual registro crítico de producto, que necesitará su propio contrato de seguridad y despliegue.

## 5. Plan futuro de migración reversible — NO ejecutado

1. **Inventario seguro** de esquema y grants actuales de plataforma con usuario sólo lectura autorizado: contrastar existencia de roles, FKs, índices, filas huérfanas y permisos; no asumir que la propuesta está presente.
2. **Revisión de política** antes de cambios: threat model, MFA provider, factor fiable, reautenticación, scopes permitidos, TTL/límites reales, separación de roles y retención/privacidad; aprobaciones y plan de reversión.
3. **Expand condicional**: migración nueva y aditiva con índices únicos y FKs a identidades reales verificadas; sin sobrescribir `UsuarioEmpresa` o `Empresas` ni permitir grant global al propietario ERP. Script EF inspeccionado y comprobado en MySQL 8.4 efímero.
4. **Integración previa**: pruebas .NET/API y MySQL efímero con concurrencia, revocación, expiración, índices, rol global, auditoría fallida, permisos de distintas empresas A/B, compatibilidad antigua y upgrade/rollback; ninguna prueba `SKIPPED/CANCELLED/FAIL` se considera PASS requerido.
5. **Cutover independiente** sólo con autorización expresa, backups y restauración verificada; feature flags fail-closed y retiro de rutas experimentales seguro. QA/main/PROD, VPS, secretos y redes no forman parte de este plan ejecutado.

**Rollback futuro:** deshabilitar ruta excepcional de forma segura, revocar grants emitidos, preservar logs/auditoría y revertir únicamente cambios aditivos tras migración reversible probada; no revertir ciegamente permisos/datos compartidos. En este scope Markdown, rollback real es sólo `git revert` con nueva auditoría, sin alterar BITACORA histórica.

## 6. Fixtures de aceptación — simbólicos, no MySQL/HTTP ejecutado

| ID | Caso futuro | Resultado exigido |
| --- | --- | --- |
| P01 (+) | Operador GLOBAL vigente + MFA reciente vinculada + motivo seguro + target permitido | Grant específico TTL y auditoría sólo bajo transacción segura |
| P02 (+) | Acceso excepcional legítimo de A en plazo, recurso A y permiso exacto | Sólo A, sin ampliación a B ni privilegios permanentes |
| P03 (+) | Revocación de grant autorizada | Bloqueo inmediato de uso posterior, auditoría con mínima información |
| P04 (+) | Operación idempotente repetida misma huella/scope con autorización vigente | Sin segundo efecto y respuesta segura coherente |
| N01 (-) | Admin ERP o `Usuario.RolId` legacy sin rol global | Denegado, sin grant ni consulta entre empresas |
| N02 (-) | JWT válido pero MFA ausente/vencida/no ligada al target | Denegado; no basta sesión o claim |
| N03 (-) | `EmpresaId=B` de cliente, host B o header falso con operador autorizado sólo a A | No cruzar límites, sin datos B |
| N04 (-) | Grant expirado, revocado, de otra acción o wildcard arbitrario | Denegar incluso si JWT sigue vigente |
| N05 (-) | Motivo vacío o contenido con SQL/JS/secretos | Rechazo/sanitización, sin persistir payload |
| N06 (-) | `PlatformCriticalAudit` no disponible o falla transacción | No ejecutar operación crítica ni afirmar éxito |
| N07 (-) | Repetición idempotency key con fingerprint o target diferente | Conflicto sin duplicar grant |
| N08 (-) | Dos operaciones concurrentes consumen prueba MFA de un uso | A lo sumo una gana; la otra rechazada |
| N09 (-) | Grant intenta cambiar configuración IAM o token de otra empresa fuera de scope | Rechazo sin escalamiento ni efecto |
| N10 (-) | Índice/FK/migración propuesta interpretados como grants reales | NO_GO a runtime/QA/PROD, ninguna escritura |

Son **cuatro positivos y diez negativos propuestos**, sin xUnit, HTTP, EF migration, SQL, Playwright, MFA real o prueba contra Aiven ejecutada. No reportar PASS runtime por redactar fixtures.

## 7. Dependencias y A0–A5 documentales

**Preflight de esta unidad:** `C02-04.M01.01–06` LISTO y A5 independiente de Javier autorizado; `PLAN_MAESTRO!H406=PENDIENTE/I406` vacío, `COLA!G406/O406=SI` y `DAG_TECNICO!F/G=VALIDADO/SI` exactos antes del claim; responsable exclusivo adquirido por CAS con readback. M02.01–02 son documentación preliminar, no obligan a habilitar ninguna escritura privilegiada.

**A0:** diff solo Markdown, seguridad de fuente, CHANGELOG de sección autocontenida, ausencia de secretos. **A1:** revisar F/P01–P04 y N01–N10 en texto, pruebas ejecutables = NO_APLICA. **A2:** segregación plataforma/ERP, A/B, MFA/reautenticación, duración/revocación, privacidad y auditoría fail-closed documental. **A3:** contrastar con M01/M02 y fronteras existentes, sin integración de servicio real. **A4:** publicar fast-forward en dev y comprobar SHA exacto, Scope Lock y V5 provenance `completed/success`, luego PLAN/COLA/BITACORA readback. **A5:** otra Task Javier revisa de modo independiente; nunca la autora.

Este documento no instala privilegios, ACL, usuarios, migraciones, credenciales, MFA, auditoría runtime, workers, eventos ni infraestructura. No libera M02.06 ni fases M03–M06/QA/main/PROD por sí solo.
