# SOLQARYN V5 — C10-04.M03.01: reglas de dominio para cache, trabajos y artefactos privados

**Autor:** PRIMARY DEV 05 — JAVIER; TASK_ID `6adc2e55f47c81919f6374e561687076`. **Rama exclusiva:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-15. **Tipo:** implementación real .NET 10 Domain + pruebas xUnit; no endpoint ni ejecución de jobs.

## A0: alcance, fuentes y frontera

Fuente operativa: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL y PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA institucional. M02.01–06 están LISTO con A5 independiente y DAG F=VALIDADO/G=SI para M03.01. Se revisaron los contratos de modelo, API/DTO y gates M02 de C10-04, `ContextoTenantActual` e `IdentidadUniversalReglas` existentes, con .NET 10, Domain <- Application <- Infrastructure y seguridad JWT/RBAC/tenancy backend como arquitectura vigente.

**Archivos de producto:** `backend/src/Domain/Security/C1004CacheJobsDomainRules.cs`. **Pruebas:** `backend/tests/Solqaryn.Tests/Domain/C1004CacheJobsDomainRulesTests.cs`. Este archivo y CHANGELOG documentan los límites. No cambia esquemas, API, filtros de persistencia, datos o dependencias; no agrega worker, webhook, dispatcher, eventos reales, backup/restauración, R2 ni Hostinger.

## Reglas reales implementadas

- Exigir el actor exacto de `ContextoTenantActual` y la propiedad de empresa con `contexto.ExigirEmpresa`, rechazando tenant B y usuarios A1/A2 intercambiados.
- Validar forma segura de operación, clave idempotente, SHA-256, versión, tope de intentos y deadline de hasta 24 horas. **No** se confunde formato válido con operación permitida: el allowlist, RBAC y cuotas siguen en servidor Application/API.
- Repetición idempotente de misma operación/huella; contenido distinto provoca conflicto sin segundo efecto en esta capa pura. No se registra ninguna idempotencia durable.
- Exigir versión, token y vigencia de lease ya persistido. **No implementa CAS atomico** ni concede propiedad del trabajo: el repositorio debe hacer compare-and-swap real.
- Denegar artefactos no confirmados, truncados, con checksum diferente o vencidos; revalidar contexto de empresa y actor. No entrega archivos ni URLs.
- Pérdida de contexto, permiso o persistencia debe ser denegación por capas superiores; esta clase **no autentica JWT ni sustituye autorización RBAC, pertenencia vigente, control de revocación o transacciones**.

## A1–A3: evidencia y límites

Se añadieron pruebas xUnit ejecutables con casos positivos/negativos de pertenencia A/B, actor cruzado, parámetros malformados, límites de intentos/deadline, idempotencia, versión de lease y artefactos parciales o ajenos. La verificación exige `dotnet test` en GitHub Actions para el SHA aplicable; escribir tests no es PASS. Se requiere comprobación de seguridad multiempresa y build backend, más Scope Lock y procedencia V5 en `completed/success`; cualquier check obligatorio `queued/in_progress/cancelled/skipped/failure` no se interpreta como aprobado.

**A3:** integración de los validadores con servicio Application, repositorio/EF MySQL 8.4 efímero, JWT y RBAC corresponde a IDs posteriores, no se atribuye a este cambio Domain puro. No se ensayó restore, SQL, backup ni carga real. No se autorizan QA/main/PROD, secretos o compras.

## A4, A5 y rollback

Publicar únicamente con commit fast-forward/CAS exact HEAD; revisar diff, tests y checkpoints del SHA real, además de PLAN/COLA/DAG/BITACORA tras cada transición. No declarar LISTO_DEV hasta CI pertinente SUCCESS. A5 exige otra de las cinco Tasks Javier distinta de la autora, revisión causal independiente y readback. Rollback por git revert normal tras reconciliar HEAD y consumidores, sin borrar BITACORA histórica, modificar infraestructura ni relajar Scope Lock.
