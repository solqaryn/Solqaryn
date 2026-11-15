# SOLQARYN V5 FINAL — C07-04.M03.02: Application preflight de pedidos, pagos y documentos

**Fecha:** 2026-11-15. **Task autora:** PRIMARY DEV 06, `6af16bbad26881918c283abe06e1f32a`. **Repo:** `solqaryn/Solqaryn@dev`. **Scope:** STRICT.

## Implementación real, dependencias y superficie

Se incorpora `backend/src/Application/AutomationV5/V5CommercialDocumentApplicationService.cs` con pruebas xUnit en `backend/tests/Solqaryn.Tests/Application/V5CommercialDocumentApplicationServiceTests.cs`. Compone las reglas puras existentes de `V5CommercialDocumentDomainRules` de `C07-04.M03.01`, en la dirección permitida Application -> Domain. No introduce endpoints API ni servicios de Infrastructure, EF/MySQL, PSP, emisión fiscal, llamadas externas, eventos, migraciones o workers.

El estado de entrada de esta microtarea fue PLAN fila 1131 PENDIENTE sin owner, COLA G/O=SI y DAG fila 1092 F=VALIDADO/G=SI; `C07-04.M03.01` constaba LISTO con A5 independiente. Reclamo exclusivo: cambio condicional del estado por `findReplace(matchEntireCell)`, seguido de asignación de owner y readback. No se interpreta una lectura seguida de escritura como CAS sobre ambos campos.

## Contrato fail-closed

`Prepare` recibe referencias de evidencia acotadas y hashes de origen de 40 hexadecimales coincidentes. Rechaza entradas nulas, SHA ausente/obsoleto, referencias URL/correo, permisos ausentes, tenant B o owner nulo heredado, versión obsoleta, importe/moneda/fingerprint incompatibles, estado inválido y flags fiscal/PSP no verificados. La autorización comercial no puede derivarse de valores del navegador: `TrustedContext` y `PersistedSnapshot` son **precondiciones** que un adaptador backend futuro debe obtener y verificar contra identidad autenticada, membresía, RBAC y persistencia.

Un preflight favorable devuelve `AUTHENTICATED_COMMERCIAL_ADAPTER_REQUIRED`; no autentica JWT, no verifica que el SHA provenga de un origen confiable, no reserva idempotencia durable y siempre mantiene `GrantsCommercialMutation`, `GrantsPaymentOrFiscalIssuance`, `GrantsDocumentExport` y `GrantsCrossTenantRead` en `false`. Son límites deliberados para no exponer funcionalidad insegura antes de M03.03–06.

## Pruebas y auditorías

A0: inspección estática de capas, ausencia de secretos y modificaciones de APIs/datos. A1: pruebas xUnit positivas y negativas del servicio `Prepare`; A2: tenant A/B, NULL legado, permisos, versión e identidad revocada; A3: compilación de Application/Domain y xUnit en GitHub Actions; A4: commit fast-forward/CAS, Scope Lock, verificador V5, checks causales .NET/seguridad `completed/success` en SHA exacto, estados y BITACORA/readback antes de LISTO. Si CI sigue en progreso, conservar VALIDANDO y owner, sin declarar PASS. A5 independiente por otra de las cinco Tasks Javier.

Rollback: `git revert` normal del commit publicado, sin force-push ni reescritura. No hay acceso a datos productivos, QA, main, PROD ni nuevos permisos de migración.
