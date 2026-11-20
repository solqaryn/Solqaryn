# C15-02.M06.04 — Ensayo real de rollback seguro y aislado (V22)

## Alcance A0 y riesgo

PLAN_MAESTRO!D2231 exige «Ensayar rollback o desactivación sin borrar datos legítimos». Este changeset R2 agrega código de pruebas y workflow de CI; no modifica endpoints, permisos, bases, configuraciones reales, infraestructura ni terceros. PLAN!E2231 es una meta acumulativa de fase M06, no una certificación por esta microtarea de Hostinger, Cloudflare, R2, MySQL productivo, DNS o proveedores. PROJECT_SCOPE_LOCK=STRICT; solo dev.

## Ensayo reproducible GitHub-hosted

El workflow v22-c15-02-m06-04-rollback.yml ejecuta node --test con Node 24 en ubuntu-latest sobre el SHA publicado, sin secretos, con contents:read y concurrency por SHA (cancel-in-progress:false). Crea un repositorio Git temporal y **ejecuta realmente git revert**, no una simulación de un comando. No opera sobre el checkout del proyecto ni un servicio real.

La prueba configura un interruptor de revisión ficticio con externalCalls:false, 2.340 IDs **sintéticos**, datos separados de tenants A/B, bitácora y evidencia; desactiva el interruptor en un commit y después añade un evento legítimo y cambio de saldo sintético. El git revert de aquel commit restaura solo el interruptor y verifica por SHA-256 que todos los archivos protegidos mantienen exactamente su estado posterior, sin perder el evento.

Casos adversariales reales: revert de un commit que toca tenant/bitácora bloqueado; HEAD stale bloqueado antes de tocar datos; SHA malformado y worktree sucio bloqueados. Pruebas 4; exigir cero FAIL, cero SKIPPED, log y job completed/success del SHA causal.

## A0–A5 y límites

A0: diff y contrato puntual de rollback, sin despliegue ni modificación externa. A1: Git revert CLI real y aserciones positivas/negativas/estado fronterizo. A2: aislamiento sintético A/B y preservación de bitácora; no afirma JWT/RBAC runtime. A3: integración REAL con Git y filesystem efímero; MySQL/EF/HTTP/servicios externos NO_APLICA al **diff test-only**, no se certifican tales integraciones ni restauraciones de datos. A4: CI dirigido, Scope Lock exact-SHA y V5 provenance completados SUCCESS, más CAS y readback de PLAN/COLA/BITACORA. A5: evaluación independiente de otra Task Javier distinta del autor DEV04 después del cierre de A0–A4.

LIMITACIÓN: ninguna prueba usa proveedores live ni dispositivos físicos; no acredita rollback de Aiven, Vercel, Render, Cloudflare, R2, Hostinger o DNS, ni resuelve FAIL históricos de CI global. R3 NO_GO para QA/main/PROD, compras, secretos, cambios externos o restauración destructiva. Revertir este changeset en dev solo mediante git revert no destructivo, con HEAD revalidado y exclusión por ID; nunca reset o force-push, y BITACORA histórica permanece intacta. Ningún sucesor se desbloquea sin A5 y fórmulas DAG genuinas.
