# SOLQARYN V5 — C15-03.M01.02: actores, interfaces y fronteras de auditoría de seguridad/tenancy

**Fecha local:** 2026-11-14. **Autor:** PRIMARY DEV 02 JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Rama:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Entrega:** contrato documental de entradas, salidas, actores y superficies afectadas. No crea endpoints, jobs ni autorizaciones.

## Fuentes y dependencia de contexto

- Plan ejecutable: STANDARD V5 FINAL `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; PLAN_MAESTRO fila 2235 y COLA G/O=SI; DAG_TECNICO fila 2196 F=VALIDADO/G=SI al reclamar. A5 corresponde a otra Task Javier.
- M01.01 publicado en SHA `6f78719ba9e7318122b2efa1bf37795ef66f6d86`, archivo `docs/v5/c15-03-m01-01-alcance-no-regresion-auditoria-seguridad-tenancy.md` (NR01–NR16, P01/N01–P08/N08). Sus casos son FUTUROS_NO_EJECUTADOS, no cobertura medida.
- Fuentes de código examinadas en el HEAD anterior: `TenantPermissionContext.cs` blob `df7d49f3`, `IdentidadUniversalAuthorizationGuard.cs` blob `5c7a256e`, `VerifiedCommercialTenant.cs` blob `2da140ce`, `UnscopedCommercialTenantGuardMiddleware.cs` blob `558dda1e`, `TenantQueryBypassGuard.cs` blob `2ba91bab`, `CommercialCrossTenantWriteGuard.cs` blob `1989e290`, `V5WorkItemTenantAuthorizationGuard.cs` blob `d14aaf38` y tests de frontera `UsuarioEmpresaTenantBoundaryTests.cs` blob `52acc040` y `SecurityBoundaryContractTests.cs` blob `efd7f811`.
- El sistema actual utiliza Angular 22.2.1, ASP.NET Core net10.0, EF Core 10.0.12 y MySql.EntityFrameworkCore 10.0.9 según `package.json`/`csproj`. Cloudinary es integración actual; Cloudflare R2/Hostinger son objetivos futuros y NO se activan aquí.

## Doce actores y su autoridad

| Actor | Superficie/entrada | Autoridad válida, límite o estado |
| --- | --- | --- |
| A01 Usuario comercial autenticado | Angular → API privada | JWT y membresía/rol actuales; UI no concede permisos |
| A02 Administrador de empresa | API de usuarios/configuración | RBAC empresarial sólo para EmpresaId autorizado |
| A03 Operador de sucursal | API con sucursal seleccionada | Sucursal validada contra pertenencia vigente; no puede usar B |
| A04 Usuario revocado | JWT antiguo o sesión persistente | Snapshot revocado no puede seguir autorizando |
| A05 Administrador de plataforma | Rutas control plane | Autoridad global propia; no es automáticamente permiso comercial tenant |
| A06 Soporte delegado | Ruta `/plataforma/soporte` | Token/grant/contexto verificados en servidor; lectura delimitada |
| A07 Comprador anónimo de tienda pública | Host/ruta pública | Host autorizado determina tenant; no hereda sesión privada |
| A08 Propietario de datos y medios | Repositorios, export/download | Owner empresarial y recurso verificados para cada operación |
| A09 API/servicio de aplicación | Controller → Application | Ejecuta caso de uso y verifica permiso antes de side effects |
| A10 Repositorio EF/MySQL | AppDbContext | Scoped predicates y FK; no confiar en filtros frontend |
| A11 Worker/job futuro | Cola o acción diferida | Sin ejecución aprobada; exigir actor/tenant/idempotencia de fuente confiable |
| A12 Revisor/auditor independiente | GitHub checks + PLAN/COLA/BITACORA | Task Javier distinta del autor, evidencia exact-SHA, sin QA por inferencia |

## Doce contratos de entrada/salida para auditar (no todos son endpoints)

| Contrato | Entrada de origen | Salida observable requerida | Frontera |
| --- | --- | --- | --- |
| IO01 | Token JWT + ruta privada | Acceso sólo cuando se verifica pertenencia y permiso actual | Browser/API |
| IO02 | `X-Empresa-Id` como selector | Rechazar ambigüedad, inválidos o permisos ausentes | Header no confiable/servidor |
| IO03 | `UsuarioId` y `EmpresaId` | Snapshot server-side de membresía y rol o denegación | Application |
| IO04 | `SucursalId` solicitado | Sucursal propia verificada o denegada | Empresa/sucursal |
| IO05 | ID de recurso de detalle/listado | Respuesta exclusivamente de owner autorizado | API/EF |
| IO06 | Consulta histórica `IgnoreQueryFilters` | Predicado EmpresaId autorizado o bloqueo previo | EF/tenant |
| IO07 | DTO venta/compra con FKs relacionadas | Rechazo de asociaciones A→B y NULL legacy sin atribución | Persistencia |
| IO08 | Host/ruta pública | Bind a tenant real sin lectura comercial privada | Público/privado |
| IO09 | Solicitud de soporte/plataforma | Resultado condicionado a grant server-side, auditoría | Plataforma/comercial |
| IO10 | Solicitud de archivo/descarga futura | Verificación propietario, MIME/TTL/expiración y no enumeración | Medios, aún no auditados |
| IO11 | Evento/worker futuro o V5 work item | Scope tenant+actor verificados e idempotencia | Job/trusted source; NO IMPLEMENTADO aquí |
| IO12 | Publicación documental y A5 | Check-runs exact SHA y PLAN/COLA/BITACORA coherentes | GitHub/Drive; no QA/main/PROD |

## Diez fronteras de confianza y fallos negativos de diseño

| Frontera | Positivo futuro | Negativo futuro que debe bloquearse |
| --- | --- | --- |
| T01 Navegador → API | JWT vigente y permiso servidor | JWT A + `EmpresaId=B` o URL B no autoriza |
| T02 Header → contexto tenant | Selector único aceptado sólo con grant | Header duplicado/inválido no aplica fallback permisivo |
| T03 JWT → membership vigente | Snapshot propio de usuario/empresa/rol | Rol revocado u obsoleto no mantiene autoridad |
| T04 Empresa → sucursal | Sucursal propia | Sucursal B o ID inventado es rechazado |
| T05 API → EF query | Scope empresarial aplicado | `IgnoreQueryFilters` no expone cross-tenant |
| T06 EF write → FKs | Cliente/producto pertenecen al mismo tenant | Relaciones cruzadas o NULL legacy reasignado no persisten |
| T07 Host público → tenant | Host registrado resuelve dueño correcto | Host arbitrario no se convierte en autorización privada |
| T08 Plataforma/soporte → ERP | Delegación explícita con alcance | Soporte no se convierte en superusuario tenant implícito |
| T09 App → medios y background | Owner/tenant de servidor preservados | Job/URL/archivo de B no llega a A por payload externo |
| T10 GitHub → estado institucional | SHA exacto, CI pertinente y revisor distinto | `in_progress/skipped/cancelled`, autocertificación o propietario ajeno no son PASS |

**Todos los positivos y negativos de T01–T10 están PENDIENTE_DE_EJECUCIÓN como plan de prueba**, no son afirmaciones de cobertura. Una lista de actores o controles unitarios existentes no basta para afirmar que las rutas públicas, soporte, export, storage o jobs estén auditados.

## Secuencia de verificación A0–A5 y rollback

1. **A0 estática:** inspección de las fuentes y cotejo NR01–NR16 de M01.01, entradas/salidas y privilegios descritos.
2. **A1 documental:** consistencia de IO01–IO12 y T01–T10 con actor, positivo/negativo y resultado esperado; no hubo requests HTTP.
3. **A2 documental:** análisis de privilegios y tenant A/B sin modificar seguridad real. Ninguna prueba de penetración ejecutada.
4. **A3 documental:** trazabilidad de blobs/contratos actuales versus futuros y prerequisitos de PLAN/COLA/DAG; no se atribuye integración MySQL.
5. **A4 publicación:** este archivo y nueva sección íntegra del CHANGELOG en commit dev fast-forward con Scope Lock y verificador V5 `completed/success` en SHA exacto.
6. **A5_PENDIENTE:** sólo otra Task Javier del allowlist puede hacer revisión independiente; no se avanza QA/main/PROD ni M01.06 automáticamente.

**Rollback:** revert Git normal del único changeset documental y adenda de BITACORA si hay incidencia; conservar snapshots originales, sin force-push ni datos productivos. Sin endpoints, MySQL, variables, secretos, autenticación, clientes externos ni servicios de pago modificados.
