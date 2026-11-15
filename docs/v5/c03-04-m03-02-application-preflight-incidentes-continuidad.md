# SOLQARYN V5 — C03-04.M03.02: preflight Application de incidentes y continuidad

**Autor:** PRIMARY DEV 02 — JAVIER_CHATGPT (`6af16bb9492c8191b8e8c7d606f11507`). **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Scope:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

## Entrega efectiva

Se añade `V5ContinuityIncidentApplicationService` a la capa Application (.NET 10), con pruebas xUnit `V5ContinuityIncidentApplicationServiceTests`. El código compone las reglas preexistentes `V5ContinuityIncidentDomainRules` de la microtarea M03.01, verifica consistencia exacta entre SHA esperado y observado y exige referencia de evidencia limitada y no sensible. Todas las entradas son snapshots, **no certificados de autenticidad**.

Un paquete preliminar coherente retorna `AUTHENTICATED_INCIDENT_ADAPTER_REQUIRED`, no permiso efectivo. Los resultados `AllowsIncidentMutation`, `AllowsBackupRestore` y `AllowsEvidenceDisclosure` son siempre false; ninguna trayectoria puede ejecutar contención, revocación, restauración, backup, HTTP, escritura SQL ni exposición de artefactos.

## Fuentes y prerequisitos comprobados

- `backend/src/Domain/AutomationV5/V5ContinuityIncidentDomainRules.cs`, blob `94e685da2887581b94ecb114cbb992ea5e2018b7`: JWT/membresía/scope del servidor, ownership A/B, sucursal, versión, idempotencia, prueba de mitigación y autorización independiente.
- `backend/tests/Solqaryn.Tests/Domain/V5ContinuityIncidentDomainRulesTests.cs`, blob `27a1d9836715b4f676116f634378045be15ca849`.
- PLAN_MAESTRO fila 555 `C03-04.M03.02`: PENDIENTE sin propietario antes del CLAIM. COLA G/O=SI y DAG F=VALIDADO/G=SI; precedentes M02.01–06 y M03.01 con A5 según fórmula. No presupone éxito de M03.03 ni permite su bypass.

## Seguridad y contratos posteriores

La ejecución real futura requiere adaptador autenticado que recupere identidad JWT, revocación y membresía actuales, permisos de acción, ownership persistido por tenant y sucursal, bloqueo de versión y clave idempotente transaccional. Para resolución también debe acreditar contención anterior y prueba de mitigación. Una restauración sandbox exige runner autorizado distinto: la solicitud `RequestSandboxRestore` siempre se rechaza aquí aunque un campo declare aprobación.

La nueva suite xUnit incluye oráculos positivos de preflight de solo lectura e intención de contención, y negativos de SHA, evidencia con URL sensible, tenant A/B, cambio de sucursal, permisos revocados, revocación de incidente, evidencias no sanitizadas, versión/idempotencia, resolución sin mitigación y restauración sin runner.

Las pruebas solo son **ejecutadas cuando CI causal GitHub Actions las acredite**; escribir este documento no cuenta como xUnit PASS. A0: alcance, capas, diff, secretos; A1: pruebas positivas/negativas .NET; A2: seguridad JWT/RBAC y tenant A/B; A3: build y tests .NET10; A4: Scope Lock, procedencia CHANGELOG, SHA exacto y PLAN/COLA/BITACORA. Si falta CI requerida, mantener VALIDANDO.

## No alcance y rollback

Ningún cambio en Domain, API, DTO HTTP, DI activa, MySQL/EF, migraciones, workers, secretos, infraestructura, eventos o frontend. No se llama a servicios de backup ni se realiza restauración. A5 independiente por otra Task Javier tras A0–A4, sin autorización de QA/main/PROD. Reversión por git revert normal, sin force-push.
