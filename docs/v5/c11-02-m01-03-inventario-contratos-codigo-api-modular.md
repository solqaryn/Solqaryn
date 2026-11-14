# SOLQARYN V5 — C11-02.M01.03: inventario verificable de código y contratos de API modular

**Autor:** PRIMARY DEV 01 / JAVIER_CHATGPT / Task `6adbb9fff1548191a1bd5b30d9e9984a`. **Plan:** C11-02.M01.03, PLAN_MAESTRO fila 1624, lote de preparación documental API modular. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Fecha:** 2026-11-14. **Ámbito:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT.

## Objetivo, inventario y límites

Esta unidad lee contratos, código y evidencia existente **sin confundir lectura con certificación**. Precedentes documentales C11-02.M01.01/.02 definen alcance, NR01–NR14, actores e interfaces; M01.03 identifica evidencia de implementaciones actuales y brechas que deben ser cerradas por las siguientes microtareas. DAG_TECNICO!A1585:G1585 F=VALIDADO/G=SI y COLA G/O=SI habilitan sólo este análisis; no implican autorización de cambios en runtime, IAM/JWT/RBAC, MySQL, eventos, workers, migración o QA/main/PROD.

## Fuentes reales Git inspeccionadas

| # | Archivo | Blob Git | Interpretación limitada |
| --- | --- | --- | --- |
| 01 | `backend/src/API/Controllers/AuthController.cs` | `af437c869b4800a499c42aeff00347de3a3cc521` | Login/renovación, rate limit, auditoría y respuesta ApiResponse |
| 02 | `backend/src/API/Controllers/TenantContextController.cs` | `2407c2eb6779ed724e841c688ee14dabbf163171` | Selector tenant y membresía activa verificada en backend |
| 03 | `backend/src/API/Controllers/PlataformaSesionController.cs` | `965a05321a8b8c3f90011addb84e14bcc5978775` | Sesión global, no-store, auditoría y autorización separada |
| 04 | `backend/src/API/Program.cs` | `01425cff56eb13e12022b1b81a2f322d17fd1fff` | Composición DI, middlewares y configuración del API |
| 05 | `backend/src/API/Solqaryn.API.csproj` | `2a7b33a0974d1ebe13676593d84b458b1243e037` | .NET net10.0, JWT, referencia Application/Infrastructure |
| 06 | `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj` | `6210d8b12abd77171d95d17e2292c1a319b89263` | EF 10.0.12, proveedor MySql.EntityFrameworkCore 10.0.9 |
| 07 | `backend/src/Application/Interfaces/IAuthService.cs` | `986743c5ba7015c5c6bda564cc50781f3b26cd65` | Contrato LoginAsync/RenovarAsync, tipo de retorno nullable |
| 08 | `backend/src/Application/Interfaces/IUsuarioScopeService.cs` | `5345ee2de1b1e942bff8eded658f5dc27c0a6745` | Contrato tenant-aware junto a contexto legacy que NO concede empresa |
| 09 | `backend/src/Application/Interfaces/IPlataformaScopeService.cs` | `ba8f5d313117cc4dd4e20a56e2c1959c724544cd` | Autoridad global desde roles de plataforma, separada de EmpresaId |
| 10 | `backend/src/Application/Common/ApiResponse.cs` | `aba995eae50ad756712961c9c394f5038a31a142` | Forma Success, Data, Message, Errors, fábricas Ok/Fail |
| 11 | `backend/src/Application/DTOs/LoginDto.cs` | `6851ae4763fab9bc05bcf14ab75c0c8fd446089e` | DTO login y respuesta Token/ExpiraEn, no prueba seguridad completa |
| 12 | `backend/src/API/Middleware/ExceptionHandlingMiddleware.cs` | `baee40143e4e48b8df200e77892606af96d60b7f` | Manejo de excepciones y validación; requiere pruebas de no filtración |
| 13 | `backend/src/API/Filters/FluentValidationActionFilter.cs` | `fec754eeb2c7e41599155f25a24e8adbb53298bb` | Resolución/ejecución de validadores por acción |
| 14 | `backend/src/API/Middleware/SupportReadOnlyCommandInterceptor.cs` | `3b80b59899522b9b41681ed75b8269f8720fe297` | Segunda barrera contra SQL de escritura en soporte |
| 15 | `backend/src/API/Filters/RequiereMembresiaEmpresaAttribute.cs` | `17c7d191e3e1b34f56c3d27ba4992b512526542b` | Filtro de membresía activa con contexto verificado |
| 16 | `backend/src/API/Filters/RequierePermisoAttribute.cs` | `b25a75b33d9cf809e2bbbf92b8e7f35463d3eea4` | Filtro RBAC empresarial y tratamiento del soporte limitado |

**Método:** lectura de los 16 archivos concretos desde el repositorio original en `dev` y cotejo de contenido con blobs. La lista representa una **muestra trazable** para delimitar contratos, no todo el árbol: se observó un directorio Controllers con múltiples controladores empresariales, filtros y servicios. No se verificaron rutas completas, todos los endpoints, logs reales, configuración de despliegue, bases productivas o datos personales.

## Lectura técnica de interfaces existentes

1. `IAuthService` retorna `LoginResponseDto?` en LoginAsync y RenovarAsync. El controlador conserva códigos 401 y aplicación de rate limit en login; **no se deduce por ello ausencia de bypass en otros controladores**.
2. `IUsuarioScopeService` distingue contexto legacy sin autoridad empresarial y contexto verificado tenant-aware; un usuario autenticado no equivale a membresía en A o B.
3. `IPlataformaScopeService` verifica autoridad de plataforma desde fuente relacional; la sesión de plataforma no requiere EmpresaId y no debe reutilizarse como concesión empresarial.
4. `ApiResponse<T>` define Success, Data, Message y Errors. Migrar a ProblemDetails o cambiar status/shape requeriría versionado, mapa de clientes y prueba de compatibilidad.
5. `FluentValidationActionFilter` y `ExceptionHandlingMiddleware` conforman puntos de validación/error. Su presencia no demuestra cobertura de todos los DTO ni anonimización de todos los mensajes.
6. `RequierePermisoAttribute` y `RequiereMembresiaEmpresaAttribute` muestran barreras por rol o membresía. Cada endpoint futuro debe comprobar autorización en backend y no depender de guard Angular.
7. `SupportReadOnlyCommandInterceptor` inspecciona SQL en soporte; se necesita evidencia de configuración y pruebas reales antes de certificar protección integral.
8. `Program.cs` y .csproj muestran composición .NET/EF/JWT. Un paquete instalado no acredita migraciones MySQL, contratos HTTP ni controles de seguridad ejecutados.

## Brechas de evidencia G01–G12 (NO son fallos confirmados)

| ID | Evidencia que falta para certificación futura |
| --- | --- |
| G01 | Inventario completo de endpoints/controladores con autorización efectiva por ruta |
| G02 | Matriz DTO y consumidores Angular/storefront por versión y status HTTP |
| G03 | Pruebas tenant A/B positivas, negativas e IDOR sobre endpoints de mayor riesgo |
| G04 | Prueba de revocación de rol/membresía y token/refresh en condiciones concurrentes |
| G05 | Cobertura real de validadores por DTO, límites de tamaño, paginación y sort allowlist |
| G06 | Regresión de errores: 400/401/403/404/409/500 sin PII, stack, tokens ni SQL |
| G07 | Integración de auditoría y correlación segura para autorizaciones y denegaciones |
| G08 | Certificación de soporte solo lectura incluyendo SaveChanges/raw SQL/caminos indirectos |
| G09 | Trazabilidad de transacciones financieras/inventario, errores y rollback |
| G10 | Suite dirigida .NET 10 con éxito exact-SHA para cambios de runtime futuros |
| G11 | MySQL efímero/EF y migraciones revertibles cuando se modifique persistencia |
| G12 | Medición de latencia/error y compatibilidad efectiva del frontend bajo cambios API |

**Clasificación:** G01–G12 son obligaciones de comprobación; no se declara que fallen. No se interpreta la nulabilidad legacy o ausencia de tests observados como vulnerabilidad explotable. Las 16 huellas son una línea base de lectura para esta microtarea y deberán revalidarse cuando cambie HEAD.

## Escenarios de prueba futuros, no ejecutados

**P01–P08 FUTUROS_NO_EJECUTADOS:** login válido y rate limit, renovación válida, membresía A vigente, plataforma global separada, DTO validado, error 4xx seguro, soporte read-only, uso de repositorio con MySQL efímero cuando corresponda.

**N01–N12 FUTUROS_NO_EJECUTADOS:** token expirado, EmpresaId manipulado, tenant A→B, rol ajeno/revocado, empresa bloqueada, client-side guard bypass, DTO malformado, fuga de PII/stack/token, soporte intenta DML, consulta sin scope, cambio incompatible de shape/HTTP y transacción parcialmente persistida.

Todos son criterios de aceptación para trabajo futuro, NO pruebas ejecutadas o PASS atribuidos a este inventario.

## Impacto, auditoría y rollback

- **A0:** alcance documental, identidad, Skill canónica, DAG/PLAN/COLA y exclusión de cambios runtime; no se reescriben fuentes inspectadas.
- **A1:** lista verificable de 16 fuentes, contratos/interpretaciones y 12 brechas, sin declarar cobertura completa.
- **A2:** JWT/RBAC, ámbitos de plataforma y empresa, tenant A/B, soporte, PII, secretos, validación y errores; no cambiar permisos.
- **A3:** correspondencia conceptual entre API, Application, Infrastructure y MySQL; ninguna prueba de integración runtime se ejecuta para este diff.
- **A4:** publicar sólo este Markdown y sección autocontenida de CHANGELOG vía fast-forward CAS y HEAD exacto; comprobar blobs, Scope Lock y control V5 CHANGELOG `completed/success`; ejecutar readback PLAN/COLA/BITACORA. Tests backend/frontend/seguridad no causales al cambio documental se informan NO_APLICA, nunca PASS.
- **A5_PENDIENTE:** revisor independiente distinto del autor entre las otras cinco Tasks Javier; no concede QA/main/PROD.
- **Rollback:** `git revert` normal del commit documental, sin reset/force push ni modificar BITACORA histórica. No se implementan servicios, workers, webhooks, eventos, migraciones, cambios de JWT/RBAC, DB o infraestructura.
