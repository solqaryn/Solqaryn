# SOLQARYN V5 FINAL — C01-03.M01.03: inventario verificable de contratos, código y evidencias

**Estado y límite:** lectura PRELIMINAR (M01, antes de M02). Esta entrega inventaría únicamente contratos/código observados en el HEAD de `dev`, sin afirmar que exista un subsistema completo de configuración dinámica ni que los controles multitenant de todas sus futuras superficies estén certificados. Fuente única de alcance: Plan V5 FINAL C01-03; DAG_TECNICO F=VALIDADO para M01.01–05 con once prerequisitos documentales cerrados. Revalidar frente a C00-01 final y C01-01 antes de M01.06, M02, cambios ejecutables, migración o QA/main/PROD.

## 1. Evidencia primaria leída y contratos visibles

| Archivo real inspeccionado en dev | Blob de referencia | Qué permite afirmar | Qué NO demuestra |
| --- | --- | --- | --- |
| `frontend/src/app/core/models/tema-visual.model.ts` | `9d99a1da70c9c6da408d9a564e91512150b4aedd` | `TemaVisual` expone campos de color nominados y `CAMPOS_TEMA` define metadatos UI y variables CSS | No acredita esquema configurable genérico, scopes de backend, migraciones ni permisos efectivos |
| `frontend/src/app/services/tema-visual.service.ts` | `588aa46fc4c10f8d6be85628e919a7bd89f366ac` | Cliente Angular usa `GET /tema-visual`, `PUT /tema-visual`, `POST /tema-visual/restaurar` mediante HttpClient con `ApiResponse<TemaVisual>` | No demuestra que servidor autorice todos los métodos para empresa A/B; hace falta lectura y prueba del controlador antes de certificar |
| `frontend/src/app/services/theme-applier.service.ts` | `cbccc2795cb1b010f74ce5b2b4dc1fb20f0b41f3` | Aplica sólo hex `#RGB` o `#RRGGBB` a tokens CSS definidos en CAMPOS_TEMA; invalida versiones de tema para evitar respuesta antigua en ese servicio | No valida cualquier DTO de configuración futura, no reemplaza autorización del backend ni protege de forma automática otras rutas |
| `backend/src/API/Program.cs` | `01425cff56eb13e12022b1b81a2f322d17fd1fff` | Confirma que la composición API usa referencias ASP.NET, JWT, EF Core y MySQL; es un punto de entrada a revisar para el contrato nuevo | La presencia de dependencias/imports no prueba por sí misma aislamiento, endpoint genérico, permisos o E2E exitosos |

**Fuentes externas autorizadas consultadas:** V5 FINAL en Drive, `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_V13.md`, Skill canónica `solqaryn-project-governance` y `references/architecture.md`; `PLAN_MAESTRO`, `COLA`, `DAG_TECNICO` y cabecera BITACORA física de 26 campos A:Z.

## 2. Matriz de afirmación y vacío por superficie

| Superficie | Hallazgo actual | Pendiente y prueba exigible al implementarse |
| --- | --- | --- |
| Modelo visual | Interfaz `TemaVisual` con campos tipados como `string` y metadatos de UI | Definir validación y tipos más estrictos para configuración universal en M02; no confundir tipos TypeScript con controles de servidor |
| Servicio HTTP cliente | Métodos `get`, `update`, `restaurar` sobre `/tema-visual` | Releer API/controller, DTO, rol y tenant-context; probar HTTP 200/4xx A/B con JWT real |
| Aplicación de CSS | `COLOR_HEX_SEGURO` y `CAMPOS_TEMA` minimizan CSS libre en este consumidor | Pruebas positivas con #fff/#112233; negativos con `<script>`, `javascript:`, `url(javascript:...)`, hex inválido; no inferir que todos los consumidores comparten la misma defensa |
| Tenancy y RBAC | Arquitectura documenta JWT/RBAC, auth del backend; el frontend no sustituye autorización | Examinar repositorio y handlers concretos antes de fijar permiso para un nuevo esquema; A→B y usuario sin rol deben ser denegados |
| Persistencia | Infraestructura EF Core/MySQL existe como stack declarado | Ninguna entidad/migración de configuración dinámica universal fue verificada en esta lectura; validar esquema, índice, transacción y rollback en M02/M03 |
| Versiones y concurrencia | El servicio de aplicación de tema controla respuesta tardía local por versión | No equivale a control de versión optimista en DB; diseñar rechazo de update obsoleto |
| Auditoría y logs | El proyecto exige redacción de PII/secretos en gobernanza | Revisar logs causales en endpoints reales, no declarar cobertura por mera existencia de middleware |
| Gobernanza CI | GitHub Actions check-runs por SHA exacto; `verify-solqaryn-project-scope` es check obligado | No usar estado combinado o PR-only como lista completa; no contar SKIPPED como PASS ni reutilizar green de otro SHA |

## 3. Resultado documental positivo y pruebas negativas dirigidas

**Caso positivo D-01:** la ruta contractual de cliente es `/tema-visual` y enumera únicamente GET/PUT/restaurar; la respuesta está tipada `ApiResponse<TemaVisual>`. Se puede encontrar y rastrear en el blob indicado, sin crear endpoints nuevos.

**Caso positivo D-02:** la validación de color vista en `ThemeApplierService` es `/^#(?:[0-9a-f]{3}|[0-9a-f]{6})$/i`, por lo que los literales `#fff` y `#112233` cumplen su regex; este documento no afirma haber ejecutado pruebas de navegador.

**Caso negativo D-03:** una afirmación de que `TemaVisualService.update` prueba permisos RBAC es **inválida**: sólo se ha leído código de cliente. Antes del cierre de implementación, exigir API/autorización y prueba negativa 401/403 y cross-tenant.

**Caso negativo D-04:** `<script>`, `javascript:` o SQL literal no son valores hex válidos para el servicio de tema actual. Ello no acredita que un futuro editor acepte código arbitrario ni que los DTO generales sean seguros; deben rechazarse por allowlist y validación server-side.

**Caso negativo D-05:** una CI de SHA histórico o workflow `SKIPPED` no puede fundamentar PASS técnico de cambio futuro. El resultado M01 es lectura documental y no atribuye pruebas .NET, Angular, MySQL o E2E nuevas al contenido inventariado.

**Caso negativo D-06:** cambiar `EmpresaId`, `tenant_id`, slug, URL o cabeceras desde el navegador no concede autoridad al cliente; cualquier contrato futuro que lo acepte debe bloquearse hasta corregirse.

## 4. Dependencias, límites, validaciones y rollback

El DAG20 validado autoriza esta preparación M01.01–05 tras C00-01.M02.01–06 y C00-03.M01.01–05 LISTO. La aprobación de una microtarea documental **no libera M01.06/M02 ni sustituye los 36 pasos de la fase**, ni resuelve por sí sola A5_FAIL previos. Revalidar versiones de archivos citados antes de usarlos en M02: GitHub puede avanzar por commits de otras Tasks.

Para esta entrega **TIPO1 documental sin modificación ejecutable**:
- `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`.
- `requiere_security=SI` en revisión documental de JWT/RBAC/tenant, PII y superficies de inyección, **no** prueba efectiva de backend.
- `requiere_scope_lock=SI` en commit documental a dev y SHA exacto.
- A0 = trazabilidad archivo/blob y diff/secret scan; A1 = casos D-01/D-02/D-03/D-04/D-05/D-06 positivos y negativos; A2 = evaluar límites de aislamiento/RBAC/inyección; A3 = cotejar V5/DAG, arquitectura y consumidores; A4 = commit FF, blobs, check Scope Lock y readback PLAN/COLA/BITACORA; A5 = revisor independiente otro TASK_ID.

**Rollback:** `git revert <SHA_DOCUMENTAL>` normal después de revalidar HEAD, sin force-push; no borrar BITACORA ni cambiar trabajo de otras Tasks. Sin escritura en QA/main/PROD, datos MySQL reales, secretos, tareas programadas, providers o DNS. **El inventario histórico de imágenes cero no requiere migración (NO_APLICA).**
