# SOLQARYN V5 FINAL — C02-04.M02.02: API, DTO, errores y compatibilidad de consola global auditada

**Tipo y frontera:** contrato de API, validación y compatibilidad **exclusivamente DOCUMENTAL**. **Autor:** PRIMARY DEV 01 — JAVIER (`6adbb9fff1548191a1bd5b30d9e9984a`), 2026-11-14, `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. No implementa ni registra endpoints, DTOs C#, roles, MFA, grants, almacenamiento, workers, webhooks ni seguridad productiva.

**Autoridades:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [Sheet institucional](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit) `PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA`; Skill, `AGENTS.md`, Scope Lock y [entidades/invariantes M02.01](c02-04-m02-01-entidades-ownership-invariantes-consola-global.md). Los seis M01 ya tienen LISTO y revisión A5 independiente; DAG para esta M02.02 se consultó como `F=VALIDADO/G=SI`, gate exclusivo de preparación, NO autorización para ejecutar IAM ni pasar a otro entorno.

## 1. API real observada (no modificar ni ampliar por inferencia)

| Fuente real en `dev` | SHA blob | Contrato observado |
| --- | --- | --- |
| `backend/src/API/Controllers/PlataformaEmpresasController.cs` | `87fbf187b7993ba0f05edff393500f24d0856833` | `[Authorize]` + `[Route("plataforma/empresas")]`; `GET /` paginado con auditoría, `POST /borrador`, `POST /alta-controlada` para operaciones precisas; ninguno supone MFA excepcional transversal |
| `backend/src/Infrastructure/Services/PlataformaScopeService.cs` | `2a71a56750805ef513c7f312fb6c6c2d53a8c314` | `ObtenerActualAsync` reconsulta la autoridad global persistida por petición; no acepta claim JWT como rol global |
| `backend/src/Application/Interfaces/IPlataformaScopeService.cs` | `ba8f5d313117cc4dd4e20a56e2c1959c724544cd` | `UsuarioPlataformaScopeActual(UsuarioId, RolId, NombreUsuario, RolNombre)` actual; no incluye permiso temporal ni verificación MFA |
| `backend/src/Application/Security/PlatformSupportPolicy.cs` | `b103f0b0ec7b9169c060661f6a4456f8863152cf` | Lista acotada de GET de productos/categorías en DEV, no concede acceso global ni mutación |
| `docs/v5/c02-04-m02-01-entidades-ownership-invariantes-consola-global.md` | `791a1cc82d0cd51c0d3515b88290072395593c7f` | Modelo **conceptual** de operador, desafío/reautenticación futura, grant acotado y auditoría crítica; no es implementación |

**Compatibilidad prioritaria:** no cambiar firmas, códigos HTTP, formatos `ApiResponse`, rutas y claims de operaciones existentes. El `GET /plataforma/empresas` real verifica paginación `pagina` de 1 a 100000 y `tamano` de 1 a 50, retorna campos mínimos y `private, no-store`; **no** se reinterpretará como endpoint de elevación. `POST /borrador` y `POST /alta-controlada` tampoco se convierten en concesión de acceso excepcional. Cualquier contrato futuro necesita una ruta versionada y un changeset separado con aprobación, pruebas y backward compatibility, sin elegir un path ejecutable aquí.

## 2. Superficies de diseño FUTURAS, no rutas instaladas

Las claves DTO nombradas a continuación son **propuestas de contrato**, no tipos C# ni APIs publicadas.

| Flujo futuro | Entrada de mínima autoridad | Respuesta mínima | Control previo al efecto |
| --- | --- | --- | --- |
| Solicitar acceso excepcional | `operacion`, `recursoId`, `empresaObjetivoId`, `motivo`, `ticketReferencia` validable, `idempotencyKey` opaca | `solicitudId`, `estado=PROPUESTA`, `correlationId`, timestamp servidor | Autenticación válida, rol global persistido, solicitud bien formada; **no** se concede grant por solicitar |
| Verificar factor y reautenticación | Pruebas server-side ligadas a actor, sesión, desafío y operación (nunca OTP en DTO público de respuesta) | Estado de verificación acotado y no reutilizable | MFA/reautenticación real y reciente, anti-replay, sesión vigente; proveedor todavía por definir |
| Evaluar concesión puntual | `solicitudId`, alcance exacto y aprobación cuando política la exija | `grantId` opaco, recurso/acción/tenant y vencimiento fijados por servidor | Autoridad separada, MFA, reauth, motivo, TTL, aprobación efectiva y auditoría obligatoria |
| Consumir operación privilegiada | `grantId`, versión de autorización, recurso+acción+tenant exactos y clave idempotente | Resultado mínimo de la acción y correlación, sin PII extra | Reevaluación global, grant vigente no revocado, pertenencia de recurso, antifraude y auditoría transaccional |
| Revocar concesión | `grantId`, versión y motivo válido | Estado final `REVOCADA` | Usuario/plataforma autorizado, auditar revocación; imposible volver a `APROBADA` por simple reintento |
| Consultar auditoría segregada | Cursor, intervalo acotado y filtros autorizados | Eventos redactados, paginación, `no-store` | Permiso auditor independiente, sin poder de mutación ni acceso comercial cross-tenant |

**Campos rechazados como permisos:** `esAdministrador` del cliente, `Usuario.RolId` legacy, `X-Empresa-Id`, host, `tenantId` arbitrario, booleano `mfaOk` o vencimiento elegido por frontend. Un `empresaObjetivoId` en solicitud es un **objetivo para autorización server-side**, no una concesión.

## 3. Validaciones y errores normativos de diseño

| Condición comprobada en el backend FUTURO | Categoría HTTP objetivo | Consecuencia obligatoria |
| --- | --- | --- |
| Identidad ausente, JWT inválido o sesión caducada | `401` | Sin información privilegiada ni mutación; challenge compatible con política de autenticación |
| Rol global no acreditado, tenant B no autorizado, scope incorrecto | `403` | Fallo cerrado, no confirmar si recurso de B existe |
| DTO vacío, motivo malformado, límites excedidos, ticket inválido | `400` | Error genérico estructurado, sin eco de datos sensibles |
| Versión obsoleta, solicitud incompatible, reutilización de clave con otra carga | `409` | Ningún segundo efecto; registrar colisión sanitizada |
| Concesión revocada/caducada | `403` por defecto | No permitir reactivación/reuso ni revelar metadatos de otro tenant |
| Cuota excedida para solicitudes privilegiadas | `429` cuando política futura lo establezca | Rechazo y límites por identidad/tenant; nunca omitir auditoría |
| Fallo de MFA, auditoría durable, persistencia crítica o dependencia | `503` o error seguro conforme contrato publicado | Deny/rollback; nunca reportar 2xx sin persistencia y auditoría verificadas |
| Estado aprobado de operación única | `2xx` solo después del commit seguro | Resultado mínimo, correlación opaca, `Cache-Control: no-store` |

Estos códigos son **oráculos futuros**. No se afirma que el controlador existente retorne exactamente esos códigos para el flujo excepcional, todavía inexistente. Nunca revelar en respuesta la existencia de usuarios/tenants prohibidos, nombres internos de tablas, consultas SQL, proveedor MFA, stack traces ni tokens. Para rutas existentes preservar sus `ApiResponse` y comportamiento hasta una migración de contrato separada con tests de compatibilidad.

## 4. Invariantes de DTO, autorización, versión y cuotas

| ID | Regla no negociable |
| --- | --- |
| API-01 | Operador autenticado y rol de plataforma vigente reconsultado en DB antes de cualquier evaluación; cuenta ERP o JWT aislado no son rol global. |
| API-02 | Un único tenant/recurso/acción exactos por solicitud. Cambiar cualquier campo invalida grant y decisión anterior. |
| API-03 | Prueba MFA y reautenticación no pueden venir como booleans confiables del cliente ni reutilizarse entre operaciones. |
| API-04 | El motivo existe y se sanitiza, la referencia se valida; el motivo no debe contener secretos y no reemplaza permiso. |
| API-05 | El servidor define ventanas de tiempo, políticas, cuota, límites de longitud y versiones, jamás el cliente. |
| API-06 | `idempotencyKey` va ligada a actor+tenant+operación+payload normalizado+versión; misma clave/datos devuelve mismo resultado sin duplicar; misma clave/carga distinta es conflicto. |
| API-07 | IDs/correlación opacos y acotados; no exponer JWT, OTP, DSN ni datos sensibles en URL, logs o respuesta. |
| API-08 | Cada mutación registra autor, motivo mínimo, alcance, decisión y resultado; fallo en auditoría crítica implica deny/rollback. |
| API-09 | `Cache-Control: no-store` para respuestas privilegiadas y proyecciones minimizadas, sin cache compartido entre tenants. |
| API-10 | Revocación de rol, sesión o grant y cambio de tenant bloquean nuevas solicitudes; sin usar autorización cacheada. |
| API-11 | Versionado aditivo y compatibilidad verificada con las tres operaciones existentes de `PlataformaEmpresasController`. |
| API-12 | Una respuesta de consulta o preparación jamás se interpreta como elevación o promoción de estado institucional. |

## 5. Casos simbólicos positivos/negativos (no tests ejecutados)

| Caso | Tipo | Estímulo diseñado | Resultado que pruebas posteriores deben verificar |
| --- | --- | --- | --- |
| P01 | POS | Operador global vigente solicita grant para A con MFA, reauth, motivo y alcance específico | Solo solicitud/elevación permitida conforme política, auditoría íntegra |
| P02 | POS | Repetición idéntica de operación ya confirmada con misma clave/versión | Resultado equivalente sin segundo efecto |
| P03 | POS | Auditor autorizado consulta eventos redactados paginados | Mínima evidencia sin permisos de mutación |
| P04 | POS | Revocación válida de grant actual | Estado terminal y posterior rechazo de reutilización |
| P05 | POS | GET global actual de empresas con pagina/tamano correctos | Contrato HTTP existente intacto, paginación, campos mínimos, no-store |
| N01 | NEG | Administrador ERP A intenta acceder a B o scope plataforma | 403; sin información de B |
| N02 | NEG | JWT manipulado o header de empresa forzado | No elevación ni acceso global |
| N03 | NEG | MFA ausente, expirado o de otra sesión | Deny; no grant |
| N04 | NEG | Reauth vieja, sesión o rol revocado | Deny por comprobación actual |
| N05 | NEG | Motivo inválido o secreto en DTO | 400 seguro; sin log sensible |
| N06 | NEG | Grant A aplicado a B u otra acción | 403; cero efectos interempresa |
| N07 | NEG | Idempotency key reusada con contenido o versión distintos | 409; sin duplicación |
| N08 | NEG | Dos operaciones concurrentes sobre grant revocado/agotado | Una sola resolución autorizada o denegación |
| N09 | NEG | Auditar falla después del intento de mutación | Rollback; jamás 2xx ficticio |
| N10 | NEG | Proveedor MFA caído o timeout | Deny/503, sin bypass |
| N11 | NEG | `PlatformSupportPolicy` GET para ruta de mutación | 403, allowlist no se expande |
| N12 | NEG | `EmpresaId` de frontend apunta a tenant no autorizado | No filtrar datos A/B sin ownership |
| N13 | NEG | GET privilegiado cacheado entre actores | `no-store`; ninguna respuesta cruzada |
| N14 | NEG | CorrelationId o error contiene token/PII | Sanitización, no filtración |
| N15 | NEG | Cambiar firmas o semánticas de GET/borrador/alta-controlada existente | Test de compatibilidad falla; bloquear publicación |

## 6. Auditorías de esta unidad y rollback

- **A0 documental:** cotejar source blobs exactos, DAG individual, seis M01 LISTO+A5, esquema conceptual M02.01; inspeccionar diff y no-secrets/estructura del CHANGELOG.
- **A1 documental:** 5 oráculos positivos y 15 negativos coherentes, **no ejecutados** en HTTP, xUnit o MySQL.
- **A2 documental:** exigir control A/B, no-client-authority, MFA/reauth, revalidación server-side, cuotas, rate limiting y auditoría fail-closed; no se altera ningún control real.
- **A3:** `NO_APLICA` pruebas runtime por diff exclusivamente Markdown; revisar fronteras API/Application/Infrastructure/Domain y compatibilidad de contrato con el código. Compilaciones CI no pertinentes no sustituyen pruebas HTTP futuras.
- **A4:** commit fast-forward único en `dev`, documento + sección autocontenida CHANGELOG, `verify-solqaryn-project-scope` y verificador V5 `completed/success` sobre SHA exacto, readback PLAN/COLA/BITACORA A:Z.
- **A5:** auditoría independiente por otro TASK_ID de las cinco Tasks Javier restantes. Un LISTO_DEV documental no habilita IAM, JWT, MySQL, migraciones, runtime HTTP ni QA/main/PROD.

**Reversión:** `git revert` normal del commit documental preservando historia. BITACORA es append-only; ninguna revisión borra registros. La implementación posterior requerirá gates individuales, pruebas con dos tenants A/B, MFA real, integración MySQL efímera y aprobación separada antes de cualquier despliegue.
