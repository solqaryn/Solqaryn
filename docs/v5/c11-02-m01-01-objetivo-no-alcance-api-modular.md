# SOLQARYN V5 — C11-02.M01.01: objetivo, no-alcance y no-regresión de API modular

**Fuente ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Plan:** fila 1622, C11-02.M01.01, fase documental M01 de C11-02 API modular. **Responsable:** PRIMARY DEV 01 / JAVIER_CHATGPT / Task `6adbb9fff1548191a1bd5b30d9e9984a`. **Repositorio:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. Fecha: 2026-11-14.

## Objetivo contractual de la unidad

Fijar el propósito, exclusiones y criterios de no-regresión para una evolución **futura** hacia puertos de aplicación, casos de uso acotados, DTO tipados, validación server-side, controladores HTTP pequeños y errores seguros sin quebrar consumidores existentes. El resultado esperado de **C11-02 como lote** es contratos, errores seguros y compatibilidad; esta M01.01 entrega **solo un contrato documental preliminar**, no un endpoint, implementación, API nueva ni refactor de controladores.

Las dependencias de preparación declaradas por PLAN_MAESTRO!G1622 y DAG_TECNICO!A1583:J1583 se cotejaron por ID exacto: DAG F=VALIDADO/G=SI y COLA G/O=SI para el claim; esto habilita DOCUMENTACIÓN M01, no ejecutables, nueva autoridad de plataforma, nuevas operaciones financieras ni despliegues. Las fases C01/C02/C03/C10 conservan sus puertas técnicas individuales.

## Evidencia de arquitectura y código inspeccionado en dev

| Fuente real | Blob Git inspeccionado | Evidencia limitada |
| --- | --- | --- |
| `backend/src/API/Controllers/AuthController.cs` | `af437c869b4800a499c42aeff00347de3a3cc521` | `auth/login` permite anónimo con rate limit AuthLogin; `renovar` requiere autorización, respuestas ApiResponse y auditoría del resultado |
| `backend/src/API/Controllers/TenantContextController.cs` | `2407c2eb6779ed724e841c688ee14dabbf163171` | `tenant-context` exige autenticación y verifica membresía desde servidor; la selección del cliente no concede ámbito |
| `backend/src/API/Controllers/PlataformaSesionController.cs` | `965a05321a8b8c3f90011addb84e14bcc5978775` | sesión de plataforma separada de empresa; control por alcance real, no-cache y auditoría obligatoria |
| `backend/src/API/Program.cs` | `01425cff56eb13e12022b1b81a2f322d17fd1fff` | raíz de composición, controles de servicios/validación/auth/configuración existentes; no demostrar conformidad integral por esta lectura |
| `backend/src/API/Solqaryn.API.csproj` | `2a7b33a0974d1ebe13676593d84b458b1243e037` | ASP.NET Core net10.0, JWT Bearer 10.0.12 y referencias a Application/Infrastructure |
| `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj` | `6210d8b12abd77171d95d17e2292c1a319b89263` | EF Core 10.0.12, MySql.EntityFrameworkCore 10.0.9; versiones, no migraciones aplicadas |

Se utiliza la Skill canónica de `dev` y `references/architecture.md`: Domain <- Application <- Infrastructure y API como frontera HTTP/composition root; Angular 22.2.1 no sustituye autorización. Los blobs son evidencia de código versionado, **no evidencia de ejecución, cobertura o despliegue**. M01.03 deberá inventariar la superficie completa; no se afirma que estas tres rutas sean todo el API.

## Inclusiones de la futura evolución

- Describir puertos e interfaces por caso de uso; DTOs de entrada/salida explícitos; validación backend, paginación y errores con contrato seguro.
- Mantener semántica HTTP estable, cancelación, idempotencia donde corresponda y compatibilidad controlada con clientes Angular, storefront y consumidores existentes.
- Distinguir auth empresarial, soporte solo lectura y autoridad global de plataforma; autorización basada en contexto comprobado por servidor, no `tenant_id`, `EmpresaId` o claims inventados.
- Probar con alcance real: .NET 10, integración de EF/MySQL efímero cuando se afecte persistencia, regresión JWT/RBAC/tenant A/B, contratos de API, Angular/Playwright cuando se altere consumidor.

## Exclusiones de M01.01

No crear/modificar controller, DTO, service, migration, esquema, worker, job, webhook, outbox dispatcher, carga productiva, secretos, token/JWT/RBAC, roles, claims, configuración de proveedores, CI, DNS, QA/main/PROD, ni realizar cambios de cuenta o plan. No suponer permiso para Hostinger/Cloudflare/Aiven ni ejecutar pagos. No marcar gate M01.06/M06.06 o dependencia posterior como satisfecha por una preparación no implementada.

## Invariantes de no-regresión NR01–NR14 (oráculos FUTUROS_NO_EJECUTADOS)

| ID | Condición exigible en implementación posterior |
| --- | --- |
| NR01 | La API conserva contratos/verbos/status documentados de `auth/login`, `auth/renovar` y `tenant-context` o versiona cambios con consumidores explícitos |
| NR02 | El tenant efectivo se obtiene desde identidad y membresía activas verificadas en servidor |
| NR03 | Un identificador de empresa enviado por cliente nunca autoriza consulta de la empresa B |
| NR04 | La autoridad global de plataforma no se convierte automáticamente en autoridad de empresa |
| NR05 | El soporte read-only no muta registros aunque encuentre un endpoint editable |
| NR06 | La validación rechaza DTOs inválidos con errores acotados, sin PII, SQL ni secretos |
| NR07 | La ausencia de permiso o membresía tiene decisión fail-closed, sin enumerar otros tenants |
| NR08 | Login aplica límites contra abuso; renovaciones no evitan revocación de sesión o usuario |
| NR09 | Auditoría exigible preserva resultado/correlación sin registrar contraseña/token |
| NR10 | Errores excepcionales no cambian operación fallida en éxito ni ocultan rollback transaccional |
| NR11 | Cualquier cambio MySQL/EF preserva relaciones/históricos y cuenta con rollback comprobable |
| NR12 | DTOs y rutas antiguas no se eliminan antes de migrar consumidores comprobados |
| NR13 | Frontend Angular, guard y URL pública no conceden permisos sobre los endpoints privados |
| NR14 | Documento, blob o gate DAG no se interpreta como PASS de API ejecutada, A5 propio ni producción |

## Pruebas que deberá ejecutar una microtarea de código futura — NO_EJECUTADAS

**Positivas:** P01 login autorizado/rate limit esperado; P02 renovar token vigente; P03 tenant A con membresía activa; P04 plataforma global validada independientemente; P05 DTO válido y respuesta estable; P06 consumidor Angular compatible; P07 EF/MySQL efímero para repositorios modificados; P08 rollback/reintento idempotente donde exista efecto lateral.

**Negativas:** N01 acceso tenant A a B; N02 EmpresaId adulterado en body/header/query; N03 rol o membresía revocados; N04 usuario bloqueado; N05 soporte read-only intenta POST/PUT; N06 token inválido o expirado; N07 carga malformada o sobre límite; N08 error expone stack/PII/secretos; N09 login sin rate limit; N10 cambio rompe DTO o status anterior; N11 auditoría ausente ante denegación relevante; N12 operación parcial sin rollback.

No se han ejecutado P01–P08 ni N01–N12 en esta M01.01; únicamente documentan escenarios. Una compilación ajena al diff no demuestra esos comportamientos.

## Auditorías, evidencia y reversión

- **A0:** verificar Skill/HEAD, fuente V5, PLAN/COLA/DAG por ID, ausencia de conflicto y cambios sólo en `docs/v5` y `CHANGELOG_AI.md`.
- **A1:** inspeccionar objetivo, NR01–NR14 y escenarios P/N como contrato documental, todos NO_EJECUTADOS.
- **A2:** revisar límites tenant A/B, JWT/RBAC, permisos de plataforma, datos personales y soporte de solo lectura sin cambiar runtime.
- **A3:** trazabilidad de las fuentes API, Application/Infrastructure y sus blobs; integración real **NO_APLICA** a la documentación.
- **A4:** publicación fast-forward bajo CAS del HEAD, diff y blobs exactos, `verify-solqaryn-project-scope` y procedencia V5 de CHANGELOG `completed/success` sobre SHA publicado, readback en PLAN/COLA/BITACORA; suites runtime sólo si la causa lo requiere.
- **A5:** permanece PENDIENTE de una de las otras cinco Tasks Javier con TASK_ID diferente, sin autoaprobación ni elevación a QA/main/PROD.

**Rollback del cambio:** `git revert` ordinario del commit documental con reconciliación de cambios concurrentes; nunca reset/force-push ni supresión de eventos históricos. Toda modificación futura de código API debe tener su propio preflight, permisos, pruebas y gates A0–A5.
