# C11-04.M05.01 — prueba unitaria causal de observabilidad

- Riesgo: R2 conservador por pruebas de denegación y pertenencia, aunque el cambio sea exclusivamente pruebas.
- A0: contrato de M05.01 para C11-04; no modifica UI productiva, APIs, DTO, JWT, RBAC, tenant, base de datos ni integración con exporter.
- A1: cuatro Vitest independientes: éxito sin métricas conectadas; revocación durante respuesta asíncrona; respuesta de admin ante pertenencia empresarial local; HTTP 429 con token sensible sin revelar.
- A2: denegación por defecto y revocación verificables por aserciones funcionales; NO se afirma prueba de JWT/HTTP real.
- A3: CI GitHub Actions debe ejecutar realmente Vitest y Chromium del workflow de observabilidad; MySQL/EF/proveedor no aplica porque ningún archivo productivo de esas capas cambia.
- A4: requerir SHA dev exacto, Scope Lock, V5, frontend build, seguridad, pruebas dirigidas, readback de PLAN/COLA/BITACORA antes de LISTO.
- A5: exclusivamente otra Task Javier entre las cinco autorizadas, revisión de código, pruebas y evidencia.
- Rollback: git revert normal del changeset sobre dev con revalidación; no force-push, no QA/main/PROD.
- Los resultados CI se consignan por SHA cuando concluyan, sin llamar PASS a QUEUED/IN_PROGRESS.
