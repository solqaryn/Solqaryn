# SOLQARYN V5 FINAL — C01-03.M02.02: contrato API/DTO, errores, validación y compatibilidad de configuración dinámica

**Tipo:** documental M02; no implementación. **Autor:** PRIMARY DEV 04, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Fuente:** Standard de Implementación de Automatizaciones V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`) y plan operativo 2.340 IDs. **Ámbito:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. El resultado requerido es **definir** contrato de entradas, salidas y errores para configuración dinámica tipada, no añadir rutas, DTOs ni modificar seguridad en ejecución.

## 1. Contratos realmente presentes y distinción de legado

| Fuente inspeccionada (blob exacto) | Contrato existente en dev | Restricción para un consumidor futuro |
| --- | --- | --- |
| `backend/src/API/Controllers/EmpresaConfiguracionController.cs` (`ea1bce800862d706387ccce18b368f401ce3ad3d`) | Ruta `empresa-configuracion`; GET `publica` anónimo con caché; GET/PUT bajo autorización y permiso; GET/PUT `tenant/{empresaId:int}` autenticados; upload/logo y plantillas | **No** existe aquí una ruta universal de catálogo de claves; `empresaId` de URL no confiere membresía |
| `backend/src/Application/DTOs/EmpresaConfiguracionDto.cs` (`203ec5ceaeb99ba2afdd14846ef6c373668fa0ff`) | `EmpresaConfiguracionDto` público/legacy y `ConfigEmpresaTenantDto` con `EmpresaId`, `Version`, campos operativos; `UpdateConfigEmpresaTenantDto.Version`; plantilla versionada | DTO existente no es contrato universal de clave/valor; no exponer `CorreoSecretoReferencia` |
| `backend/src/Application/Interfaces/IEmpresaConfiguracionService.cs` (`79923fcaf84b96c56e1f694a07b8bf9f9a20e8d2`) | `GetTenantAsync`, `UpdateTenantAsync`, logo/plantillas, métodos legacy | Compatibilidad sin modificar firmas ni prometer que todos los flujos existentes validan tenant completo |
| `backend/src/Domain/Entities/ConfigEmpresa.cs` (`664547d28b513d3187622b02c57a7db1f812f7ec`) | Config operacional con empresa positiva, campos JSON y versión; secreto por referencia opaca | Valor arbitrario no debe mutar estructura, expresiones o secretos por usar JSON |
| `backend/src/Domain/Entities/EmpresaConfiguracion.cs` (`d3d6e59a15c59bc04fa769f8bcefcfe5f50794e3`) | Campos de presencia/branding de legacy, sin ownership tenant expreso | No transformar `GET publica` en fuga de configuración privada ni asumir aislamiento por `Id` |
| `docs/v5/c01-03-m02-01-entidades-ownership-invariantes.md` (`8820c3b28b56a9134c6edc6fe8139c27a21ca340`) | Catálogo conceptual de claves, scope, owner, revisión, auditoría e invariantes INV-01–12 | Contrato de M02.02 debe respetar esos límites, sin sustituir M02.03–06 |

**Hallazgo relevante:** existe una ruta explícitamente anónima para datos públicos y rutas autenticadas separadas. Un futuro catálogo dinámico debe tener allowlist pública *por clave* distinta de la allowlist privada *por permiso*. No se reutiliza el endpoint público para leer valores globales/tenant sensibles.

## 2. Contratos propuestos y versión (aún NO rutas implementadas)

El diseño de API futuro podrá representar, después de aprobación de M03, las operaciones lógicas siguientes; las firmas y rutas definitivas deben ser fijadas con los consumidores reales, sin colisionar con `/empresa-configuracion`:

| Operación conceptual | Entrada controlada | Respuesta autorizada | Prohibición |
| --- | --- | --- | --- |
| `ConsultarClaveEfectiva` | `key` canónica y scope solicitado *no autoritativo* | `{key,type,value,effectiveScope,version,isDefault}` con proyección de campos permitidos | Leer A cuando actor resuelve B, secretos o claves privadas por endpoint público |
| `ListarClavesAutorizadas` | Paginación acotada, filtro de ámbito restringido, actor | Catálogo sin valores secretos; sólo claves accesibles y versión del esquema | Enumerar tenants, nombres de claves internas o credenciales |
| `ActualizarClave` | Clave allowlisted + `typedValue` + `expectedVersion` + idempotency/correlation seguros | Nueva revisión atómica o rechazo seguro, auditoría sin valor secreto | Cliente elige empresa/rol/capacidad, muta flag reservado o sobrescribe versión distinta |
| `RestaurarValorSeguro` | Clave propia, revisión esperada y permiso explícito | Valor default de catálogo o ausencia controlada, según scope | Borrar config global, remover RBAC, secret o auditoría por un rollback |

### Envoltorio DTO futuro — propuesta, no clase actual

```text
ConfigKeyRead  = key, type, effectiveValue, effectiveScope, version, isDefault
ConfigKeyWrite = key, typedValue, expectedVersion, idempotencyKey?
ConfigKeyError = code, safeMessage, correlationId, optionalFieldErrors
```

El **tipo de valor** se resuelve por catálogo *del servidor*, no por `type` enviado desde UI. `scope` y `ownerId` no son fuentes de autoridad y pueden omitirse del DTO de escritura. `expectedVersion` positivo y comparación atómica son obligatorios cuando se implemente persistencia. `idempotencyKey` se vinculará al usuario/tenant/operación, jamás a la clave global solamente. La precedencia global→tenant→sucursal/recurso y la publicación de defaults requieren reglas del catálogo aprobadas; no adoptar fallback cruzado entre empresas.

## 3. Validación y respuestas de error propuestas (no garantías del backend actual)

| Código conceptual | Situación | Respuesta externa futura y prueba negativa |
| --- | --- | --- |
| `AUTH_REQUIRED` | Sesión/token ausente, inválido o caducado en endpoint privado | 401 genérico; nunca indicar empresa/dato privados |
| `PERMISSION_DENIED` | Actor sin membresía, capacidad RBAC, entitlement o scope | 403 o 404 opaco según política aprobada, sin lectura parcial |
| `KEY_NOT_ALLOWED` | Clave desconocida, reservada, confusable o enum inválido | 400/404 seguro; no crear key nueva |
| `INVALID_TYPED_VALUE` | Tipo/rango/tamaño erróneo, JSON no tipado, nulidad indebida | 400 con errores de campo allowlisted, sin echo de payload peligroso |
| `CONCURRENT_VERSION` | `expectedVersion` obsoleta o doble escritor | 409, misma versión persistente anterior, sin overwrite |
| `INVALID_SCOPE` | Cambio a empresa B, sucursal ajena, owner ausente o plataforma global | 403/404 genérico, sin reconocer existencia de B |
| `CONFIG_SECRET_FORBIDDEN` | Credenciales, token SMTP, claves de proveedor o conexión | 403/400 sin incluir secretos en auditoría ni respuesta |
| `CONFIG_PERSISTENCE_FAILURE` | Excepción/trx cancelada/proveedor no disponible | 5xx seguro, no mutación parcial; correlación sin datos sensibles |

No afirmar que el API actual utiliza exactamente estos códigos. Compatibilidad requiere tests antes de consolidar contratos en Application/API y preservar formatos `ApiResponse<T>` existentes, métodos legacy y semántica pública autorizada.

## 4. Oráculos documentales A1 y negativos de seguridad A2

| Caso | Operación y escenario | Resultado esperado en futura implementación |
| --- | --- | --- |
| P01 (+) | Admin A con membresía activa y permiso Configuracion.Editar cambia una clave **no reservada** en A con versión vigente | 2xx y readback de A con revisión incrementada, auditoría propia |
| P02 (+) | Lector A autorizado consulta clave de A que no tiene override | Default seguro de A; nunca valor de B ni campo secreto |
| P03 (+) | Repetir petición idéntica con misma idempotency key y scope | Misma operación lógica sin doble escritura ni segunda auditoría |
| N01 (-) | Actor A modifica empresaId en URL/DTO para B | Denegar, sin revelar existencia o valor de B |
| N02 (-) | Anónimo consulta key privada a través del endpoint público existente | No publicar key ni token; fail-closed |
| N03 (-) | Rol legacy, flag o plan activo pero sin permiso efectivo | Denegar, sin cambio de valor |
| N04 (-) | Clave `__proto__`, clave reservada o no declarada | Error seguro; no expansión de catálogo |
| N05 (-) | `<script>`, `javascript:`, SQL literal, HTML/CSS libre en campo tipado | Rechazo por esquema/allowlist, no ejecución |
| N06 (-) | Versión anterior al último commit | 409, cero sobrescritura |
| N07 (-) | Membresía revocada o cambio de tenant durante llamada | Cero acceso y respuesta tardía descartada por UI |
| N08 (-) | Leer `CorreoSecretoReferencia` o credenciales internas | Campo omitido y acceso negado |
| N09 (-) | Error de DB o audit log tras validación | Transacción rollback, sin escritura mixta ni datos cruzados |
| N10 (-) | Cambio de tenant con idempotency key previamente usada en A | No reutilizar resultado ni credencial de A en B |

Estos trece escenarios son **criterios documentales**, no ejecución de xUnit, Playwright, API HTTP, MySQL ni evaluación runtime de inyección.

## 5. Impacto, gates, verificaciones y reversión

- **Gates:** `C01-03.M01.01–06` LISTO con revisión A5 por Javier; `DAG_TECNICO!A233:J233` F=VALIDADO/G=SI; `PLAN_MAESTRO!H225/I225` PENDIENTE/vacío y `COLA!G225/O225` SI antes del claim. Revalidar estado y HEAD en cada publicación. El `C01-03.M02.01` documental está LISTO, pero su A5 sigue pendiente y no se usa para liberar otro lote.
- **Alcance material:** sólo este documento y bloque independiente de `CHANGELOG_AI.md`. Domain/Application/API/Infrastructure/Angular/DB/SDK/roles/JWT/secretos/producción intactos.
- **A0:** inspección de seis archivos/blob, diff, checks Markdown, secretos y provenance sin encabezados vacíos ni atribución cruzada.
- **A1:** P01–P03 + N01–N10 y tablas de DTO/error son contrastadas documentalmente; no acreditar comportamiento runtime.
- **A2:** revisión de acceso A/B, permiso/membresía, tokens, idempotencia, secretos, payloads maliciosos y respuesta opaca; nada de bypass RBAC por flags.
- **A3:** compatibilidad de controladores/DTO `ApiResponse`, servicio tenant-first, `Version` y legado público; no cambiar rutas reales.
- **A4:** publicación directa fast-forward en `dev`, confirmación de SHA exacto y dos checks requeridos `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success`; PLAN/COLA/BITACORA con readback. Compilación .NET/Angular y MySQL no aplican a archivos Markdown-only.
- **A5:** otra Task de las cinco Javier (TASK_ID diferente) realiza revisión independiente sobre SHA; no se autoaprueba por ser la misma cuenta GitHub.
- **Rollback:** git revert normal del commit de documentación, con CAS HEAD y sin borrar BITACORA; no force-push ni migraciones.
- **No autorizados:** API nueva, entitlements globales, decisiones de deploy, Cloudflare/Hostinger, workers, eventos entre Tasks, QA, main o PROD.

**Cierre limitado:** el contrato de API y DTO de configuración dinámica está **especificado**, no implementado; M02.03–06/M03 y autorización de código mantienen puertas individuales.
