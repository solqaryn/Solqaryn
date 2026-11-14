# Certificación N9.6 — Hypercare

Fecha UTC: 2026-09-18
Rama certificada: `Desarrollo`
Parent: `N9.6 — Hypercare`
Autoridad operativa: `docs/VAEP_AUTHORITY.md`

## Alcance certificado

N9.6 vigila intensivamente errores, performance, jobs, integraciones, base de datos y señales de impacto de usuario dentro del alcance permitido de Desarrollo. Esta certificación no autoriza ni ejecuta cambios en `main`, Producción, PR #2, secretos, DNS, certificados ni datos/infraestructura productiva.

## REVIEW_FIRST y recovery

Durante N9.6.A se detectó que un probe temporal apuntaba a URLs de Producción. Ese probe fue retirado same-run conforme a `FIRST_DETECTOR_OWNS_RECOVERY`. Después se ejecutó un gate one-shot estrictamente Desarrollo-only contra `solqaryn-api-desarrollo.onrender.com` y `solqaryn-desarrollo.vercel.app`; el run `35319732966` terminó `SUCCESS` y su job `105519333459` confirmó:

- backend DEV `/health` y `/health/ready` saludables, con mediciones de latencia;
- frontend DEV accesible y con muestras de latencia correctas;
- superficies críticas protegidas en DEV permanecen fail-closed de forma anónima (`401/403`);
- el proyecto Vercel `solqaryn-desarrollo` no reportó runtime errors en la ventana fresca de una hora consultada.

El workflow temporal Desarrollo-only fue retirado inmediatamente después de obtener la evidencia. El árbol de producto quedó equivalente al baseline previo al recovery.

## N9.6.A–G

- **A — PRE:** LISTO. Estado, alcance, riesgos, estrategia de rollback y pruebas revalidados current-standard. P0=0/P1=0.
- **B — DOMAIN:** LISTO como N/A material. Hypercare no introdujo cambios de dominio, invariantes ni contratos; reimplementar sería no causal.
- **C — DB_MIG:** LISTO como N/A material. No existe delta de persistencia, schema o migraciones para Hypercare y no se ejecutó Producción.
- **D — BACKEND_API:** LISTO como N/A material. No existe delta backend/API; health/readiness DEV está verde.
- **E — FRONTEND_UX:** LISTO como N/A material. No existe delta frontend/UX; shell/latencia DEV está verde y la observabilidad Vercel fresca no mostró runtime errors.
- **F — SEC_AUDIT:** LISTO. Las superficies críticas DEV permanecen fail-closed para solicitudes anónimas; no hubo bypasses ni exposición de secretos.
- **G — TEST_CI:** LISTO. El functional/test head `1ac95f51176d8b9240741b2e33ab2bd4c2605dc0` conserva equivalencia de producto. La aceptación exact-product `35316302603` permanece verde con `100/100` Playwright y SMTP/PDF; N9.6 añadió el gate Hypercare DEV `35319732966` en `SUCCESS`.

## Equivalencia, rollback y documentación

Los cambios posteriores al functional head se limitan a documentación, evidencia append-only y mecanismos temporales VAEP retirados/read-only; no existe delta causal de producto, contrato, schema o arquitectura. Por ello OpenAPI, ADR y ERD son **N/A** para N9.6.

Rollback de N9.6 no requiere reversión de producto: el único recovery material fue retirar probes temporales y preservar el árbol de producto equivalente. Si una verificación Hypercare futura detecta un defecto real, se aplica la política current-standard de recovery causal sobre Desarrollo, sin inferir autorización productiva.

## Criterio de cierre

N9.6 sólo se certifica cuando N9.6.H complete además los registros append-only de `TASKS.md` y `CHANGELOG_AI.md` preservando el prefijo histórico byte por byte, con `additions>0`, `deletions=0`, writer temporal retirado, REVIEW_FIRST final P0=0/P1=0, receipt y write/readback en el control-plane.
