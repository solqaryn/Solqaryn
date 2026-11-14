# SOLQARYN V5 FINAL — C03-02.M01.04: riesgos, supuestos y decisiones pendientes

**Autor:** PRIMARY DEV 06 — JAVIER, TASK_ID 6af16bbad26881918c283abe06e1f32a. **Alcance:** preparación documental, sin cambios de código ni configuración. **Fecha:** 2026-11-13. **Repo:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT.

Fuentes únicas: [Plan V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [Sheet V5](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit) PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA A:Z. El resultado exigido de C03-02 es RBAC/ABAC server-side por tenant, sucursal, recurso/propiedad, archivo y operación. **M01.04 sólo registra riesgos y decisiones**: no afirma que el runtime final ya impida BOLA/IDOR o exportaciones cruzadas.

## Bases del análisis y trazabilidad de contratos

- [C03-02.M01.03](c03-02-m01-03-inventario-autorizacion-privacidad.md): 9 contratos de código y 5 clases de test inventariadas, commit 5847fdd57efe7555940cd4f9fc4aade1d24b5d0e; no constituye ejecución de pruebas.
- [C03-02.M01.05](c03-02-m01-05-criterios-aceptacion-tenant-privacidad.md): 14 criterios observables (3 positivos y 11 negativos) más seis reglas de no-regresión; commit c8763e1fe9fa2cab79c7a16d8f5cb31a0e9d95be; no constituye PASS de API.
- backend/src/API/Filters/TenantPermissionContext.cs, blob df7d49f3e185c538f3b3dafabb36c3a971a7257d: X-Empresa-Id sólo selecciona empresa, nunca autoriza.
- backend/src/API/Filters/RequierePermisoAttribute.cs, blob b25a75b33d9cf809e2bbbf92b8e7f35463d3eea4: grant por empresa/módulo/acción y marca autorizada sólo tras verificación.
- backend/src/API/Filters/RequiereMembresiaEmpresaAttribute.cs, blob 17c7d191e3e1b34f56c3d27ba4992b512526542b: sesión autenticada y membresía empresarial.
- backend/src/Application/Interfaces/IUsuarioScopeService.cs, blob 5345ee2de1b1e942bff8eded658f5dc27c0a6745: contexto tenant explícito separado del legacy.
- backend/src/Application/Security/VerifiedCommercialTenant.cs, blob 2da140ce9d7f47329d39d292d3271596467910cc: la marca de tenant procede del servidor, no del cliente.
- backend/src/Application/Common/CatalogoPermisosBase.cs, blob 6060b4c54c0428902603f2157984b693a324e8f3: definiciones de permiso no equivalen a concesiones persistidas.
- backend/tests/Solqaryn.Tests/P1001ARolesHttpJwtIsolationTests.cs, blob d0f29269117f7c0d6a08eed1cb31114073717c19: HTTP JWT sobre TestServer con grants simulados; no sustituye aislamiento MySQL real.
- backend/tests/Solqaryn.Tests/CommercialTenantMySqlIsolationIntegrationTests.cs, blob e1a41bf39e3a7161d921cdfc50b7c76c0d173a42: infraestructura de prueba MySQL efímera; el código se ha leído, no ejecutado para M01.04.

## Riesgos abiertos (R01–R12)

| ID | Riesgo concreto | Prioridad | Medida exigida para futura implementación | Caso de aceptación |
| --- | --- | --- | --- | --- |
| R01 | JWT o rol global interpretado como autorización empresarial | Crítica | Resolver membresía y grant vigentes por empresa en servidor | AC07, AC09 |
| R02 | BOLA/IDOR: ID de recurso B consultado desde A | Crítica | Ownership por empresa verificado en consulta/actualización y auditoría | AC04, AC05 |
| R03 | Selector X-Empresa-Id falso o repetido aceptado como grant | Alta | Validación unívoca, rechazo explícito, sin fallback ambiguo | AC07, AC08 |
| R04 | Usuario de A opera sobre sucursal B o sucursal no permitida | Crítica | Regla ABAC sucursal/propiedad en backend y filtro DB | AC06 |
| R05 | Exportación CSV/XLSX/PDF incluye filas de B o PII ajena | Crítica | Filtrado antes de serialización, inspección del artefacto | AC03, AC10 |
| R06 | URL firmada, miniatura o metadatos de archivo B son visibles a A | Crítica | Autorización sobre propietario y tenant previa a entrega/URL | AC11 |
| R07 | Caché o HttpContext reusa marca de empresa entre solicitudes | Alta | Contexto por request, sin scope mutable global | AC14 |
| R08 | Revocación de rol/membresía no impacta token o sesión aún vigente | Alta | Reconsultar autoridad efectiva por request y revocar acceso | AC09 |
| R09 | Excepción temporal de soporte escalada a escritura o rutas arbitrarias | Crítica | Allowlist GET, motivo y auditoría de acceso excepcional | AC12 |
| R10 | Registro legacy EmpresaId nulo atribuido silenciosamente a A | Alta | Cuarentena, migración explícita y prueba de no-regresión | AC04, AC13 |
| R11 | Error de permiso revela existencia, nombre, URL o conteo de datos de B | Alta | Respuesta neutral y sin PII; pruebas negativas de payload | AC04, AC10, AC11 |
| R12 | Prueba con mocks se presenta como certificación de tenancia física | Alta | Distinguir unidad/TestServer, MySQL 8.4 y E2E real por SHA | AC01–AC14 |

Los riesgos R01/R02/R04/R05/R06/R09 se tratan como críticos para una futura autorización de runtime. No se convierten en PASS por redactar esta tabla. Los de sesión, headers y cachés requieren pruebas A/B con valores anónimos y sin datos de producción.

## Supuestos que requieren corroboración (S01–S06)

| ID | Supuesto provisional | Confirmación necesaria |
| --- | --- | --- |
| S01 | UsuarioEmpresa mantiene una membresía activa por par usuario-empresa | Revisar índices físicos/migraciones en MySQL 8.4 |
| S02 | Todas las rutas de negocio usan filtros tenant-aware | Inventario controller por controller y tests negativos |
| S03 | Todas las entidades con dueño tienen EmpresaId o vínculo inequívoco | Matriz de ownership, consultas y recursos con null |
| S04 | Sucursales son verificables contra la empresa del usuario | Contrato y join de sucursal/empresa, pruebas B→A |
| S05 | La revocación no permite reusar un permiso en solicitudes posteriores | HTTP/JWT y pruebas de concurrencia con DB |
| S06 | Las exportaciones heredan el mismo scope que la consulta paginada | Inspeccionar servicio de export y comprobar bytes CSV/XLSX/PDF |

**No asumir** que S01–S06 están probados por solo encontrar clases o por tener CI global de un SHA antiguo.

## Decisiones de diseño por resolver (D01–D06)

| ID | Decisión propuesta | Condición de adopción futura |
| --- | --- | --- |
| D01 | Autoridad empresarial efectiva = identidad autenticada + membresía activa + grant RBAC + propiedad ABAC | Formalizar M02 y demostrar AC01/AC04/AC05 |
| D02 | Denegar si el selector es ambiguo, empresa inválida o propiedad desconocida | Probar casos negativos AC07/AC08/AC13 |
| D03 | Toda consulta/escritura/exportación debe filtrar tenant y sucursal antes de materializar | Ejecutar AC06/AC10 con MySQL efímero |
| D04 | URL, archivo y metadatos requieren verificación idéntica al recurso dueño | Ejecutar AC11 en entorno sin secretos productivos |
| D05 | Soporte plataforma sólo con allowlist, expiración, motivo y auditoría | Comprobar AC12 sin permisos implícitos |
| D06 | PRUEBAS por SHA exacto y A5 independiente de otra Task Javier | CI relevante + registro A0-A4 causal, rollback seguro |

Las seis decisiones son **propuestas de preparación**, pendientes de implementación/validación por sus microtareas y no autoridad para mutar datos o cambiar arquitectura.

## Clasificación técnica y controles A0–A5

**Tipo 1: documento sin runtime.** requiere_dotnet=NO_APLICA; requiere_node_angular=NO_APLICA; requiere_mysql=NO_APLICA; requiere_docker=NO_APLICA; requiere_e2e=NO_APLICA. **requiere_security=SI** (análisis de límites de JWT/RBAC/tenant, PII, IDOR, export y archivo). **requiere_scope_lock=SI** si se publica commit en dev.

- A0: revisar 12 riesgos, seis supuestos, seis decisiones, referencias de fuentes, enlaces, diff y secretos.
- A1: caso positivo: una prueba válida debe incluir membresía, grant y propiedad A; caso negativo: no aceptar rol/header falso o token de A para B. Son criterios documentales, NO pruebas ejecutadas de HTTP.
- A2: ninguna regla nueva habilita bypass de seguridad; los riesgos críticos permanecen abiertos hasta resultados de CI apropiados.
- A3: comprobar trazabilidad entre inventario M01.03, aceptación M01.05 y DAG, sin modificar interfaces, BD ni consumidores.
- A4: al publicar verificar parent, diff, blobs, check-run Scope Lock exact-SHA, PLAN/COLA/BITACORA y rollback git revert no destructivo.
- A5: PENDIENTE, revisión por TASK_ID Javier distinto del autor; jamás autoaprobar.

DAG_TECNICO para C03-02.M01.04 está VALIDADO como PREPARACION_DOC_CONDICIONAL, con C01-02.M01.01–.05 y C02-01.M01.01–.05 en LISTO en el preflight. No libera cierre M01.06, migraciones, JWT, QA/main/PROD ni acceso a otra cuenta. BITACORA real A:Z/26 campos, RUN_ID sólo nativo verificable; no reescribir eventos.
