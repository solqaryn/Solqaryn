# SOLQARYN V5 — C15-03.M01.03: inventario verificable de fuentes y brechas de evidencia de seguridad/tenancy

**Fecha local:** 2026-11-14. **Autor:** PRIMARY DEV 02 JAVIER (TASK_ID `6af16bb9492c8191b8e8c7d606f11507`). **Ámbito:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT; solo documentación. **HEAD de inventario de fuentes:** `4301356a98a98c90927dcd9998d8e0e90a7c97bb`.

## Objetivo y procedencia

Inventariar fuentes REALES del repositorio para la futura auditoría de JWT, RBAC, tenant A/B, sucursales, relaciones EF, archivos, jobs y control plane. El resultado de la unidad M01.03 no equivale a ejecutar un pentest, test contra MySQL ni a certificar una ausencia de cruces. V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, PLAN_MAESTRO fila 2236, COLA G/O=SI y DAG_TECNICO fila 2197 F=VALIDADO/G=SI en el preflight, con owner Task 02 exclusivo.

Antecedentes locales: M01.01 define NR01–NR16 y ocho pares de pruebas futuras, M01.02 delimita doce actores, doce interfaces y diez fronteras. El inventario siguiente coteja el **árbol real de GitHub** bajo el mismo HEAD, no fuentes de planes archivados. Blob SHA de cada ruta procede de la lectura por API `contents/<path>?ref=4301356a98a98c90927dcd9998d8e0e90a7c97bb` de la cuenta Javier.

## Quince archivos/contratos de código realmente leídos

| ID | Capa | Ruta verificada | Git blob SHA completo | Evidencia que proporciona (no certifica ejecución) |
| --- | --- | --- | --- | --- |
| S01 | API | `backend/src/API/Filters/TenantPermissionContext.cs` | `df7d49f3e185c538f3b3dafabb36c3a971a7257d` | Selector X-Empresa-Id; no confiere RBAC |
| S02 | Application | `backend/src/Application/Security/IdentidadUniversalAuthorizationGuard.cs` | `5c7a256ea9709f6bf4279f2ff89addf599e3a311` | Verificación de membresía/rol/sucursal actual |
| S03 | Application | `backend/src/Application/Security/VerifiedCommercialTenant.cs` | `2da140ce9d7f47329d39d292d3271596467910cc` | Marca de autorización empresa sólo server-side |
| S04 | API | `backend/src/API/Middleware/UnscopedCommercialTenantGuardMiddleware.cs` | `558dda1eb5be729b2bd9a6d9fefbccd5db94addc` | Cuarentena multiempresa y excepciones condicionadas |
| S05 | Infrastructure | `backend/src/Infrastructure/Persistence/TenantQueryBypassGuard.cs` | `2ba91bab0420f7e0fc840cf9c96e2b974838694d` | Filtra sólo si hay AuthorizedReadEmpresaId; analizar fallback |
| S06 | Infrastructure | `backend/src/Infrastructure/Persistence/CommercialCrossTenantWriteGuard.cs` | `1989e290a855a991782c59b4ee4f147be817f347` | Integridad de asociaciones y legacy NULL |
| S07 | Application | `backend/src/Application/AutomationV5/V5WorkItemTenantAuthorizationGuard.cs` | `d14aaf38094d11994130b3e6bf2db3116d576d8f` | Guardia de actor/scope/tenant sin dispatcher |
| S08 | Tests | `backend/tests/Solqaryn.Tests/API/Controllers/UsuarioEmpresaTenantBoundaryTests.cs` | `52acc04044441a0203139b8be404fd5ed2d6ce72` | Tests de usuarios/rol/empresa B desde A |
| S09 | Tests | `backend/tests/Solqaryn.Tests/API/SecurityBoundaryContractTests.cs` | `efd7f8111bdbabf0b63b5fbc9f89384dd3316fdc` | Tests de atributos authorize, uploads y rate limit |
| S10 | API | `backend/src/API/Controllers/TenantContextController.cs` | `2407c2eb6779ed724e841c688ee14dabbf163171` | Controlador de consulta contexto tenant |
| S11 | API | `backend/src/API/Controllers/PlataformaEmpresasController.cs` | `87fbf187b7993ba0f05edff393500f24d0856833` | Administración de empresas/control plane |
| S12 | Infrastructure | `backend/src/Infrastructure/Persistence/CommercialTenantReadFilterModel.cs` | `f2422beec8f2d054c20184d073bad668513fa53b` | Filtros de lectura EF tenant |
| S13 | Tests | `backend/tests/Solqaryn.Tests/API/UnscopedCommercialTenantGuardTests.cs` | `580a9b7fdc2353e3c8c3bf07bda37209144c6aa7` | Tests de cuarentena y rutas permitidas |
| S14 | Tests | `backend/tests/Solqaryn.Tests/API/PublicStoreTenantIdentitySecurityTests.cs` | `eb8680efa230cc60c9f28c714a2c84b56fb65733` | Tests de identidad tienda pública |
| S15 | Tests | `backend/tests/Solqaryn.Tests/API/Controllers/PlataformaSesionSecurityTests.cs` | `6d91aa8c6bd0fa1395c071576dff277fa73c63b2` | Tests de sesión de plataforma |

**Resultado de inventario:** 15 de 15 rutas solicitadas devolvieron contenido textual legible y blob SHA. Son **15 fuentes verificadas en Git**, no quince controles probados en runtime. La lista es una muestra causal para este alcance, no una afirmación de exhaustividad de todo el repositorio.

## Doce brechas de evidencia que impiden certificación global

| ID | Brecha o precondición de validación posterior | Prueba requerida / salida observable | Estado |
| --- | --- | --- | --- |
| G01 | No se ejecutó una matriz completa de HTTP A/B con JWT vigente/revocado | API efímera, requests con resultado 2xx propio y rechazo B, logs seguros | NO_VERIFICADO |
| G02 | Selector de empresa puede recibirse de header no confiable | Negativos header duplicado, inválido, tenant B, y fallback | NO_VERIFICADO |
| G03 | Autorización de sucursal puede divergir por ruta | Suite con sucursal A/B/no existente y verificación de membresía | NO_VERIFICADO |
| G04 | `IgnoreQueryFilters` depende del contexto de lectura autorizado | Test integrado guard + middleware ante contexto ausente y dos empresas | NO_VERIFICADO |
| G05 | Listados, detalles y exportaciones requieren anti-IDOR por owner | Fixture 2 empresas/2 usuarios con IDs cruzados y no enumeración | NO_VERIFICADO |
| G06 | Cross-tenant writes deben cubrir FK y entidades legacy con NULL | Tests EF/MySQL efímero de venta/compra/producto y rollback | NO_VERIFICADO |
| G07 | Storefront público usa bindings de host y proof no arbitrarios | Host legítimo vs falsificado, replay y cross-tenant de acceso | NO_VERIFICADO |
| G08 | Soporte y control plane no equivalen a usuario comercial universal | Actor platform/soporte, permisos expirados, intento de elevación | NO_VERIFICADO |
| G09 | Descargas/medios no se han probado exhaustivamente por propiedad | MIME, tamaño, TTL, owner A/B, URL temporal y resultado seguro | NO_VERIFICADO |
| G10 | No hay dispatcher V5 activo autorizado; jobs futuros requieren gates | Tests de actor, tenant, replay/idempotencia cuando se autoricen | FUTURO_NO_IMPLEMENTADO |
| G11 | Tests de código existentes no acreditan pipeline causal de este documento | CI .NET/security en SHA de cambio de código y evidencia job/status | NO_EJECUTADO_EN_M01 |
| G12 | A5 independiente y gates de promoción no derivan de la presencia de documentos | Task diferente, SHA original, review, BITACORA, permiso de entorno | PENDIENTE |

Las brechas son **necesidades de evidencia**, no hallazgos confirmados de seguridad. Especialmente la rama `if (AuthorizedReadEmpresaId is not > 0) return query;` de `TenantQueryBypassGuard` requiere verificar la cuarentena externa y no permite concluir una exposición real sin prueba integrada.

## Matriz de trazabilidad a las unidades previas

| Riesgo auditado | Contrato M01.01 | Interfaz/frontera M01.02 | Fuentes Git |
| --- | --- | --- | --- |
| Identidad/membresía/revocación | NR01–NR04 | IO01–IO03, T01–T03 | S01 S02 S03 S08 S10 |
| Sucursal/detalle/owner | NR05–NR07 | IO04–IO05, T04 | S02 S06 S08 S11 |
| Lectura EF histórica / escrituras FK | NR08–NR10 | IO06–IO07, T05–T06 | S05 S06 S12 S13 |
| Tienda pública y Host | NR11 | IO08, T07 | S04 S14 |
| Plataforma y soporte | NR12 | IO09, T08 | S04 S11 S15 |
| Archivos y workers futuros | NR13–NR14 | IO10–IO11, T09 | S07 + implementación futura pendiente |
| Auditoría, errores y gates | NR15–NR16 | IO12, T10 | S07 S09 y PLAN/COLA/BITACORA |

En la fila de filtros y FK la referencia contractual correcta es NR08–NR10 (no es un identificador de test); el resultado es trazabilidad documental, no una aserción ejecutada.

## Plan de pruebas y reversibilidad

- **A0:** verificación estática de quince rutas, blob SHA y clasificación de brechas sin cambiar código.
- **A1:** inventario positivo: fuente existe en HEAD; negativo de diseño: hash ausente o contrato histórico no cuenta como evidencia. Test de negocio real NO_EJECUTADO.
- **A2:** revisar superficies JWT/RBAC/tenant A/B, privilegios y legacy NULL. No se modificó auth ni se ejecutó pentest.
- **A3:** coherencia de referencias M01.01/M01.02, mapa S→G y autorizaciones de PLAN/COLA/DAG. MySQL, HTTP, Playwright y jobs NO_APLICA para este diff Markdown, no PASS.
- **A4:** GitHub fast-forward CAS, diff de solo este documento y sección autocontenida de CHANGELOG, Scope Lock y check V5 `completed/success` en SHA de publicación.
- **A5:** pendiente de revisión independiente de otra Task Javier, sin autorrevisión ni promoción QA/main/PROD.

Rollback: revert Git del changeset documental y evento aditivo de reconciliación si cambia el estado; no reescritura de BITACORA, no force-push. No desactivar controles, cambiar empresa/roles, hacer migraciones ni abrir servicios externos.
