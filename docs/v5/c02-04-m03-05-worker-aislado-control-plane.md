# SOLQARYN V17 — C02-04.M03.05: worker aislado de previsualización del control plane

**Autor:** PRIMARY DEV 03 — JAVIER (`6adc2e4bd8a48191ac7d8641ed95a1a3`). **Repo:** `solqaryn/Solqaryn@dev`. **Scope Lock:** STRICT. **Riesgo:** V15 R2 autorización, JWT/RBAC, MFA, tenant y control plane. **Fuentes:** STANDARD vigente, PLAN_MAESTRO, COLA y DAG; antecedente C02-04.M03.04 aprobado A5 por Task independiente.

## Entrega implementada

Se añade `V5GlobalControlPlaneIsolatedDispatchWorker` en Infrastructure .NET 10. Una instancia recibe la proyección no autorizante existente `V5GlobalControlPlaneIsolatedSnapshotAdapter` (M03.04), y revisa **una sola tanda en memoria** de 1–24 tickets. Se limita a códigos de prevalidación por ticket y rechaza tenant B, actor falso, SHA stale, duplicados ordinales, entradas inseguras, ausencia de procedencia sintética, revocación, MFA insuficiente y acceso al directorio sin grant separado. El adapter enlaza con el gate Application y reglas Domain actuales para evitar copiarlas. Ningún `Receipt` permite despacho, concesión de acceso, enumeración, lease excepcional ni escritura.

Este componente es código funcional aislado pero **NO** un `BackgroundService`, scheduler, consumidor de cola real ni proveedor live. No registra DI, no persiste idempotencia, no reintenta ni invoca JWT/HTTP, MySQL 8.4, EF, MFA de proveedor, auditoría real o credenciales. `IsolatedBackendProvenanceClaim` es solo bandera sintética del caller de pruebas, nunca autenticación real ni permiso institucional. `AUTHENTICATED_GLOBAL_CONTROL_PLANE_ADAPTER_REQUIRED` no equivale a autorización. Para runtime productivo hace falta otra etapa con autoridad y pruebas independientes.

## Calidad por riesgo A0–A5

A0: diff individual Infrastructure + nueve pruebas xUnit + documento + CHANGELOG; precedentes C02-04.M02 y M03.01–04+A5, bloqueo V17 y única EN_PROGRESO propia. Rollback por `git revert` normal.
A1: xUnit positivo y negativos de tenant/actor, SHA, idempotencia ordinal, batch vacío/excesivo, input malformado, excepciones de cancelación.
A2: MFA, revocación, grant plataforma separado y denegación de client authority, exportación/sensibilidad; la UI nunca otorga permiso. Resultados nunca conceden acceso.
A3: composición de código real in-process Domain/Application/Infrastructure en CI Release/.NET 10; MySQL, HTTP y Playwright NO_APLICA al diff sin SQL ni UI, sin extrapolar a certificación global.
A4: GitHub-hosted xUnit, Backend Release, seguridad, Authorization, Scope Lock y procedencia V5 con checks `completed/success` sobre SHA publicado o descendiente idéntico acreditado. CI queued/in_progress/fail/cancelled/skipped NO PASS. BITACORA y readback PLAN/COLA/DAG por transición.
A5: revisión independiente por otra Task Javier distinta; el autor nunca se autoaprueba. No QA/main/PROD, no cambios de provider, secretos ni datos reales.
