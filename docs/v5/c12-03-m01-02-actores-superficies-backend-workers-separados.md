# SOLQARYN V5 — C12-03.M01.02: actores, entradas, salidas y fronteras backend/worker

**Fecha:** 2026-11-14. **ID:** C12-03.M01.02, PLAN fila 1803. **Responsable:** PRIMARY DEV 02 JAVIER, Task `6af16bb9492c8191b8e8c7d606f11507`. **Rama:** `solqaryn/Solqaryn@dev`. **Tipo:** documento M01 de preparación, sin ejecución de código.

## Trazabilidad y alcance
Fuente: [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [Sheet institucional](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Preflight individual: PLAN!H1803 PENDIENTE/I vacío, COLA G/O=SI, DAG F=VALIDADO G=SI, transición CAS a EN_PROGRESO y readback de Task única. Autorización solo para documentación de C12-03.M01.02; no publica worker, cola, migración, API ni cambio de infraestructura.

Referencias Git inspeccionadas: `backend/src/API/Program.cs` (API DI, conexión MySQL, middleware y guards Render), `backend/src/API/Solqaryn.API.csproj` (`net10.0`, JwtBearer 10.0.12), `backend/src/Application/Solqaryn.Application.csproj` (Application -> Domain, FluentValidation/BCrypt), `backend/src/Infrastructure/Persistence/AppDbContext.cs` (VerifiedCommercialTenant, VerifiedPublicStoreTenant, VerifiedPlatformSupportTenant, HttpContextAccessor), `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj` (EF Core 10.0.12, MySql.EntityFrameworkCore 10.0.9, Cloudinary/SMTP/PDF), `ARCHITECTURE.md` y `references/architecture.md` canónica. Estas referencias describen **código versionado**, no servicios live verificados hoy.

## Actores y límites de potestad

| ID | Actor/consumidor | Responsabilidad o restricción |
|---|---|---|
| A01 | Navegador Angular | Solicita operación por HTTPS; no firma un contexto tenant arbitrario |
| A02 | Usuario autenticado | Aporta credenciales JWT, sujetas a validación y membresía actuales |
| A03 | Administrador empresarial | Tiene roles de empresa, no privilegio de plataforma global por defecto |
| A04 | Soporte de plataforma | Solo lectura y autorización acotada cuando existe vía certificada |
| A05 | Consumidor público | Usa capabilities públicas estrictas; no accede a inventario privado |
| A06 | API ASP.NET Core | Valida entrada, JWT, tenant, RBAC, DI y contratos HTTP |
| A07 | Servicio Application | Autoriza y orquesta casos de uso y transacciones |
| A08 | Dominio | Define entidades/reglas sin dependencia de Infrastructure |
| A09 | Repositorio/DbContext | Persiste EF/MySQL, aplica verificaciones server-side |
| A10 | Proceso worker futuro | Procesaría jobs solo con contexto verificado, idempotencia y reautorización |
| A11 | Almacén de entrega futuro | Persistiría mensajes y claves de idempotencia; NO EXISTE aquí implementado |
| A12 | Proveedor de archivos/correo | Integración de Cloudinary/SMTP vigente, sin nuevos secretos ni efectos en esta fase |
| A13 | Operador CI/observabilidad | Consulta estado, trazas saneadas, pruebas/rollback exact SHA |
| A14 | Operador de migración futuro | Requeriría contrato independiente de ownership de DDL, restore y aprobación |

## Entradas, salidas y fronteras de confianza

Las rutas F07–F12 describen **topología y pruebas futuras**; sus componentes no se presumen operativos. En particular, `AppDbContext.AuthorizedReadEmpresaId` depende de marcas Verified* asociadas al contexto HTTP; la extracción a un proceso sin `HttpContext` requiere un contrato explícito de binding tenant para jobs. La falta de contexto **no** habilita consulta global ni privilegio de soporte.

| ID | Frontera | Entrada | Salida | Validación mínima |
|---|---|---|---|---|
| F01 | Browser → API | Ruta, método, cuerpo, token | HTTP respuesta/autorización | JWT/CORS/rate-limit |
| F02 | API → Application | Identidad validada, claims y parámetros normalizados | Resultado de caso de uso | Revalidación de permisos |
| F03 | Application → Domain | Reglas y entidades | Decisión de dominio | Sin dependencias inversas |
| F04 | Application → Infrastructure | Comando con ownership resuelto | Persistencia/servicio | No confundir input cliente con empresa autorizada |
| F05 | Infrastructure → MySQL | Consulta parametrizada/transaction | Registros por tenant | Aislamiento A/B y auditoría |
| F06 | AppDbContext → contexto HTTP | Marcas Verified* de servidor | Empresa autorizada o ninguna | No usar contexto ausente como global |
| F07 | API → Worker futuro | Comando autenticado o job duradero | Recepción/resultado diferido | Sin bypass RBAC/membresía |
| F08 | Worker → almacén futuro | ID de trabajo e idempotency key | Estado/retry/ack | Durabilidad y duplicados a probar |
| F09 | Worker → MySQL | Operación por tenant resuelto server-side | Commit/rollback | Aislamiento, cancelación y transacciones |
| F10 | Worker → integraciones | Payload saneado | Resultado remoto | Secrets fuera de logs, idempotencia |
| F11 | API/worker → observabilidad | Correlation ID y estado seguro | Logs y métricas | Sin JWT/PII/cadena de conexión |
| F12 | Operador → deploy/migration futuro | Artefacto firmado y autorización | Readiness/rollback | QA/main/PROD fuera de alcance |

## Superficies y cambios prohibidos
- **HTTP/DTO/guardas:** contratos existentes no se alteran; cualquier futuro endpoint exige control real de backend, no solo guard Angular.
- **Data plane:** ningún cambio a DbContext, migración, datos productivos, tabla outbox o configuración DB. Futuras operaciones deben ser por tenant, transaccionales e idempotentes.
- **Control plane:** ningún nuevo worker/dispatcher/webhook/Task. La separación de procesos, red privada, secretos, TLS o hosting no se activa en M01.
- **Integraciones:** Cloudinary/SMTP existentes no se consumen ni se sustituyen desde este documento.
- **Seguridad:** ningún nuevo rol/permisos, secretos o bypass. Tenancy A/B y RBAC deben probarse en CI si se implementa código.
- **Despliegue:** ni QA/main/PROD, ni DNS, ni Hostinger/Cloudflare; rollback documental por git revert normal.

## Pares de aceptación para implementación posterior — NO_EJECUTADOS

| Par | Positivo futuro | Negativo futuro |
|---|---|---|
| P01/N01 | JWT y membresía válidos pasan al servicio | Token ausente se rechaza antes de crear job |
| P02/N02 | Empresa A se persiste bajo A | ID B de payload no redirige datos |
| P03/N03 | Un administrador con permiso opera | Rol sin permiso no ejecuta job |
| P04/N04 | Job duplicado produce un solo efecto | Redelivery no duplica cobros/documentos |
| P05/N05 | Contexto tenant verificado llega al worker | Ausencia de HttpContext no otorga acceso global |
| P06/N06 | Readiness falla cuando MySQL cae | Liveness simple no se confunde con readiness |
| P07/N07 | Logs correlacionan trabajo sin PII | JWT y connection string nunca se imprimen |
| P08/N08 | Shutdown guarda estado durable | Kill no confirma transacción incompleta |

## Auditorías, dependencias y reversión
**A0–A4 de esta fase:** comprobar fuente, git paths, consistencia contractual y exclusión de datos/infra, publicación fast-forward de dos Markdown, Scope Lock y V5 del SHA exacto y readback PLAN/COLA/BITACORA. A1 pruebas de operaciones, A2 ejecución seguridad y A3 MySQL/worker en CI **NO_APLICA por ser solo documentación**; los casos anteriores quedan `FUTUROS_NO_EJECUTADOS`, no PASS runtime.

**Bloqueos para M02/M03:** antes de crear worker se necesita diseño explícito de contexto tenant fuera de HTTP, identidad de job, idempotencia/deduplicación, reintentos, permisos, gestión de secrets, DDL/transaction ownership, prueba positiva y negativa CI .NET/MySQL y recuperación. No habilitar eventos ni otro ambiente.

**A5:** revisión de una Task Javier diferente de la autora; no autoaprobación. Rollback mediante commit `git revert` ordinario de este changeset documental; no reescritura de BITACORA. **Resultado:** inventario de actores y flujos trazable, sin nuevos servicios.
