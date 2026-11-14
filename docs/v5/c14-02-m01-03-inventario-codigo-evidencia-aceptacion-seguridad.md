# SOLQARYN V5 FINAL — C14-02.M01.03: inventario de código y evidencia disponible para aceptación de seguridad

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 01 JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Repositorio:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Tipo:** inventario documental de lectura de fuentes Git. No es pentest, remediación, certificación de release ni prueba ejecutada durante esta microtarea.

**Autoridad:** [Standard de Implementación V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit); [M01.01: alcance de seguridad](./c14-02-m01-01-alcance-no-regresion-aceptacion-seguridad.md), [M01.02: actores e interfaces](./c14-02-m01-02-actores-interfaces-fronteras-seguridad.md). Antes del claim: PLAN_MAESTRO fila 2056 PENDIENTE sin owner, COLA G/O=SI y DAG_TECNICO fila 2017 F=VALIDADO/G=SI. Gate sólo para esta preparación M01, no QA/main/PROD ni M01.06.

## 1. Método y alcance probado por lectura

Se inspeccionaron rutas y fragmentos de fuentes originales en GitHub bajo `dev`, incluyendo su SHA de blob. Los hashes de la matriz son una **huella verificable de contenido leído**, no evidencias de ejecución. La presencia de control o de test no demuestra por sí sola que la aplicación esté libre de BOLA/IDOR, spoofing, exposición de PII o bypass. La matriz relaciona superficies reales con los criterios NR01–NR18 y actores/interfaz de las dos microtareas anteriores. Los escenarios que requieran HTTP, MySQL, E2E, MySQL efímero, runtime, configuración de despliegue o proveedor externo siguen `NO_EJECUTADOS_EN_ESTA_MICROTAREA`.

## 2. Fuentes Git inspeccionadas — inventario causal I01–I12

| ID | Fuente Git original y SHA de blob | Observación textual verificable y límite |
| --- | --- | --- |
| I01 | `backend/src/API/Filters/TenantPermissionContext.cs` — `df7d49f3e185c538f3b3dafabb36c3a971a7257d` | `X-Empresa-Id` es selector, no autoridad; exige uno solo positivo o contexto único; `GetAuthorizedEmpresaId` rechaza ausencia de marca. No prueba el middleware completo ni cada endpoint. |
| I02 | `backend/src/API/Filters/RequierePermisoAttribute.cs` — `b25a75b33d9cf809e2bbbf92b8e7f35463d3eea4` | Consulta `IPermisoService.VerificarPermisoAsync` con empresa, módulo y acción; soporte temporal tiene restricciones GET/Ver/modulo/empresa. No afirma que todos los controllers apliquen este atributo. |
| I03 | `backend/src/API/Filters/RequiereMembresiaEmpresaAttribute.cs` — `17c7d191e3e1b34f56c3d27ba4992b512526542b` | Exige sesión autenticada, obtiene `IUsuarioScopeService.ObtenerActualAsync` y marca empresa autorizada; no equivale a permiso para toda mutación. |
| I04 | `backend/src/Application/Security/IdentidadUniversalAuthorizationGuard.cs` — `5c7a256ea9709f6bf4279f2ff89addf599e3a311` | Comprueba tenant antes de I/O, revalida snapshot vigente y rol/membresía relacional; rechaza permiso inactivo. No instala endpoints ni cambia credenciales. |
| I05 | `backend/src/API/Middleware/UnscopedCommercialTenantGuardMiddleware.cs` — `558dda1eb5be729b2bd9a6d9fefbccd5db94addc` | Su comentario documenta cuarentena de API privada con dos o más empresas y excepciones explícitas; el código inspeccionado contiene rutas de soporte y controles. No se verificó cada ramal ni configuración live. |
| I06 | `backend/src/Infrastructure/Persistence/CommercialCrossTenantWriteGuard.cs` — `1989e290a855a991782c59b4ee4f147be817f347` | Comprueba propiedad antes de escrituras y prohíbe reasignación implícita; advierte que no sustituye filtros de lectura. No asegura cobertura de toda entidad. |
| I07 | `backend/src/Infrastructure/Persistence/TenantQueryBypassGuard.cs` — `2ba91bab0420f7e0fc840cf9c96e2b974838694d` | Aplica empresa a queries cuando `AuthorizedReadEmpresaId` es válido; sin marca devuelve query, condicionado en comentario a cuarentena. Este punto requiere revisión de todos los llamadores y middleware antes de certificar. |
| I08 | `backend/tests/Solqaryn.Tests/CommercialCrossTenantWriteGuardTests.cs` — `03dcd04340ad937a9b2c154867c20bad1f072a7b` | Hay test xUnit que rechaza modificación de producto de B desde A y confirma filtro de lectura; su fixture es InMemory, no prueba MySQL ni un HTTP real aquí. |
| I09 | `backend/tests/Solqaryn.Tests/CommercialTenantMySqlIsolationIntegrationTests.cs` — `e1a41bf39e3a7161d921cdfc50b7c76c0d173a42` | Test `[Trait("Category","Integration")]` usa cadena MySQL de entorno y DB efímera para A/B; no se ejecutó en este inventario ni se garantiza CI exitosa. |
| I10 | `backend/tests/Solqaryn.Tests/ConfiguredPublicStoreHostTenantResolverTests.cs` — `dd387f8b6e39c087121fbec1928fe1c5425aa66d` | Test define host exacto A/B, deniega host similar, duplicado y no configuración explícita; no demuestra configuración de hosts desplegados. |
| I11 | `backend/tests/Solqaryn.Tests/CloudinaryEnvironmentIsolationTests.cs` — `216bec0f7aa5e8983ae9ff1e12964b6195b20a43` | Hay casos que esperan rechazo al borrar ruta productiva desde configuración dev; no se consultó Cloudinary real. |
| I12 | `frontend/src/app/app.routes.ts` — `2f560c14693455b79ac4546e9333fb74586ef234` | Rutas privadas muestran `authGuard` y `permisoGuard`; la ruta de tienda vincula storeKey; frontend jamás sustituye autorización backend. |

Los SHA pueden cotejarse por `GET /repos/solqaryn/Solqaryn/git/blobs/<SHA>` o la API de Contents en HEAD compatible. El lector no debe extrapolar de la muestra a todo el árbol. No se descargó ni escribió dato personal real.

## 3. Brechas y límites probatorios B01–B12

| ID | Brecha sin certificar | Evidencia/acción causal necesaria |
| --- | --- | --- |
| B01 | No existe inventario exhaustivo controlador/endpoint → atributo/permiso → tenant | Generar matriz por rutas reales y probar acceso directo |
| B02 | No hay cierre de BOLA/IDOR por ID de B en cada recurso | Pruebas HTTP A/B positivas y negativas en entorno aislado |
| B03 | Cobertura completa de revocación JWT/rol/sucursal no certificada | Suite sesión caducada/rol revocado y readback runtime |
| B04 | Host/proxy/forwarded headers en despliegue no corroborados | Tests host inválido y configuración efectiva no secreta |
| B05 | No se demuestra cobertura de todos los `IgnoreQueryFilters` | Buscar llamadores y probar contexto autorizado con MySQL efímero |
| B06 | No se certifica aislamiento de documentos, PDFs o URLs firmadas | Tests de propietario/TTL/replay sin Cloudinary productivo |
| B07 | Jobs/webhooks/reintentos no auditados integralmente | Inventariar cada ruta existente, controles y pruebas, sin implementar dispatcher |
| B08 | Claves y expiración de caché por tenant no auditadas por completo | Inspección de cache keys y pruebas A→B/revocación |
| B09 | Backup y restauración seguras no observadas | Política vigente y restauración en sandbox autorizado |
| B10 | No existe certificación de no filtración de PII/secretos en errores/logs | Escaneo y pruebas negativas de respuestas trazadas |
| B11 | No hay evidencia integral P0/P1 cero o mitigados | Registro de hallazgos y cierre causal independiente |
| B12 | No hay E2E con métricas y aprobación de release C14-02 | CI exact-SHA pertinente, A0–A4 y A5 independiente por cada scope, autorización explícita QA/PROD |

Estas brechas **no son vulnerabilidades explotables demostradas**: son evidencia pendiente. La existencia de tests o guardias exige verificación de integración, despliegue y rutas antes de cerrar. No se infiere que faltan controles únicamente porque no se leyeron todas las fuentes.

## 4. Oráculos futuros, no ejecutados

P01 permite A leer A / N01 impide A leer B. P02 permite rol activo / N02 rechaza rol revocado. P03 permite host exacto / N03 bloquea host spoof. P04 permite URL propia vigente / N04 rechaza enlace ajeno o expirado. P05 permite transacción propia / N05 rechaza escritura con owner cruzado. P06 permite caché propia / N06 rechaza caché de tenant anterior. P07 permite job idempotente autorizado / N07 rechaza replay/otro tenant. P08 permite recuperación de fixture aislado / N08 impide restauración cruzada. P09 guarda auditoría mínima / N09 no expone JWT/PII. P10 exige CI `completed/success` del SHA exacto / N10 no acepta `queued`, `skipped`, `cancelled`, `in_progress` ni fallo.

**Estado:** `P01-P10 Y N01-N10 = FUTUROS_NO_EJECUTADOS`. La muestra I01–I12 es lectura documental; no declara HTTP, browser, DB, JWT, Cloudinary ni seguridad de producción comprobados.

## 5. A0–A4 proporcionales, dependencias y rollback

- **A0:** revisión de ID, fuentes Git, SHA de blobs y tablas con límites claros; secreto/PII no copiados.
- **A1:** diez positivos y diez negativos definidos como oráculos futuros; no se convierten en PASS de runtime.
- **A2:** revisión documental de aislamiento A/B, JWT/RBAC y cobertura incompleta con fail-closed.
- **A3:** correlación con M01.01, M01.02, V5 FINAL, arquitectura y código Git leído; no se fuerza integración.
- **A4:** exigir commit dev fast-forward, diff de este Markdown + `CHANGELOG_AI.md`, controles `verify-solqaryn-project-scope` y V5 CHANGELOG `completed/success` en el SHA publicado, readback PLAN/COLA/BITACORA. Compilaciones sin cambio de código son `NO_APLICA` motivado, jamás PASS ficticio.

El inventario no autoriza M01.06 ni fases M02–M06, migraciones, ampliación IAM, cambios de host o secrets, despliegues QA/main/PROD ni implementar eventos/workers/webhooks. Se mantiene A5_PENDIENTE para revisor **diferente** de PRIMARY DEV 01. Rollback del artefacto: `git revert` normal tras comprobar concurrencia; jamás modificar historial de BITACORA ni force-push.
