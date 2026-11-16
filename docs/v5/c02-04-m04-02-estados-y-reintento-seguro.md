# SOLQARYN V17 — C02-04.M04.02 estados y reintento seguro de consola global

**Autor:** PRIMARY DEV 05 JAVIER; TASK_ID `6adc2e55f47c81919f6374e561687076`. **Repositorio:** `solqaryn/Solqaryn@dev`. **Gobierno:** PROJECT_SCOPE_LOCK=STRICT. **PLAN fila 417**, lock V17 `SQV17_C02_04_M04_02` / `2000000417`; COLA G/O=SI y DAG F=VALIDADO/G=SI readback antes del CLAIM. Riesgo R2 por autorización y denegación reactiva de pantalla privada.

## A0: requisitos, alcance, dependencias y rollback

M04.01 ya implementó los cinco estados `vacio`, `cargando`, `exito`, `sin_permiso` y `error`. M04.02 mantiene exactamente `plataformaGuard`, `PlataformaSesionService`, cuatro controles informativos y estado fail-closed. Resuelve una carencia real: después de error transitorio HTTP 503 el servicio borra la sesión y el botón de revalidación previo desaparecía, impidiendo recuperación sin abandonar la pantalla. Ahora únicamente en `error` se presenta «Reintentar verificación», que realiza un GET **nuevo**; las tarjetas no regresan hasta que el servicio valida una sesión de plataforma auténtica del servidor. En 401/403 no aparece el botón privado. La UI nunca concede un permiso ni restaura token o tenant local.

Se modifican solamente componente Angular standalone y pruebas Vitest/Playwright del workflow existente, este documento y CHANGELOG_AI.md; sin rutas, endpoints, migraciones, jobs, proveedores, MFA, grants, secretos, infraestructura, QA/main/PROD. Rollback con `git revert` normal y revalidación de seguridad, sin force-push.

## A1–A3: pruebas y límites honestos

Pruebas dirigidas nuevas: 503 negativo con ocultación de tarjetas y diagnóstico, nuevo GET después de error, 200 positivo que revalida y habilita cuatro tarjetas, 403 que deniega cualquier reintento; escenarios previos comprueban carga de solicitud única, vacío, rol empresarial, 401, 503, error síncrono, observable sin valores, revocación y enrutamiento. Playwright debe ejecutar navegador Chromium real con **backend de sesión simulado**; lint/build Angular reales. No es una prueba de JWT HTTP, API/RBAC real, MFA, tenant A/B backend, MySQL 8.4, proveedor externo ni auditoría durable: todos NO_APLICA al diff local y **NO_MEDIDO** en producción. A2 se limita a seguridad de visibilidad y denegación UI, nunca sustituye autorización del backend.

## A4–A5: evidencias pendientes por SHA

Para LISTO_DEV se exige A0 diff, A1/A2/A3 pruebas ejecutadas, A4 `verify-solqaryn-project-scope`, V5, frontend, ERP, seguridad y workflow dirigido `C02-04.M04.01 - Vitest Angular y Chromium` `completed/success` en SHA causal o descendiente equivalente con hashes idénticos, readback PLAN/COLA/DAG/BITACORA y cero defectos P0/P1. `QUEUED/IN_PROGRESS/SKIPPED/CANCELLED/FAIL` nunca PASS. A5 solo por otra Task Javier distinta del autor DEV05. No activar QA, main ni PROD.
