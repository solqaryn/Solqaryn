# N4.2.H — Matriz de cierre QA

## Alcance y criterio

Esta matriz documenta el cierre de `N4.2.H — Bancos — Documentación y certificación` bajo política fail-closed. Un estado `PRESENT` confirma existencia/evidencia; `PASS` solo se usa cuando existe ejecución terminal aplicable; `PENDING` bloquea `LISTO_REAL`; `N/A` exige justificación explícita.

## Autoridad de evidencia

- Rama de trabajo: `Desarrollo`.
- PR oficial: #2, `Desarrollo -> main`, debe permanecer OPEN + DRAFT y sin merge/auto-merge.
- `main` permanece congelada.
- SHA funcional de Bancos certificado por `N4.2.G`: `bef9eecba1c8edde0229e0725ed1c781fa35a7c1`.
- SHA de documentación previo a este QA takeover: `0e519a9644311562c92d1aceb83f35972205100a`.
- Los commits de documentación no sustituyen la certificación funcional; cualquier HEAD documental nuevo requiere que los gates aplicables vuelvan a quedar terminales antes de `LISTO_REAL`.

## Matriz

| Gate | Estado | Evidencia / criterio |
| --- | --- | --- |
| DoD funcional N4.2 Bancos | PASS | `N4.2.G` fue certificado `LISTO_REAL` sobre `bef9eecba1c8edde0229e0725ed1c781fa35a7c1`, con P0=0/P1=0 y suite aplicable terminal. |
| Backend build/tests/migración aplicables | PASS | Certificación `N4.2.G`; runs aplicables registrados: `33653456603`, `33653456623`, `33653456576`. |
| Frontend unit/CI aplicable | PASS | Certificación `N4.2.G`; runs aplicables registrados: `33653456491`, `33653456588`. |
| RBAC / seguridad de Bancos | PASS | Certificación `N4.2.G`; run aplicable registrado: `33653456587`. |
| UI/UX / accesibilidad | PASS | Certificación `N4.2.G`; run aplicable registrado: `33653456629`. |
| Playwright / E2E de Cuentas Bancarias | PRESENT | Existe `frontend/e2e/cuentas-bancarias.spec.ts` y usa `@playwright/test`; cubre login, navegación a `/cuentas-bancarias`, heading, botón `Nueva Cuenta`, `aria-expanded`, formulario, filtros y tabla. La afirmación previa de “sin Playwright/E2E” es falsa y queda corregida. |
| Configuración Playwright | PRESENT | El árbol de `Desarrollo` contiene `frontend/playwright.config.ts`; E2E no se clasifica como ausente. |
| Documentación de certificación | PRESENT | Integrada previamente por la lane A y revisada por VAEP. |
| Runbook operativo | PRESENT | Integrado previamente por la lane B y revisado por VAEP. |
| Rollback/recuperación | PRESENT | `docs/ROLLBACK_N4_2_BANCOS.md` integrado; establece rollback fail-closed, preservación de auditoría e historia y prohibición de actuar sobre Producción desde VAEP. |
| P0 abiertos | PASS | `0` según cierre `N4.2.G`; cualquier evidencia nueva P0 reabre el padre. |
| P1 abiertos | PASS | `0` según cierre `N4.2.G`; cualquier evidencia nueva P1 reabre el padre. |
| CI/gates del HEAD documental vigente | PENDING | El HEAD documental cambia con esta propia matriz. `LISTO_REAL` exige revalidación exact-head y todos los gates aplicables terminales; `SKIPPED` no se transforma en PASS. |
| Producción / main / secrets / deploy | PASS | No autorizados para este cierre; la documentación no concede autorización de merge o deploy. |

## Evidencia E2E corregida

`frontend/e2e/cuentas-bancarias.spec.ts` contiene una suite `Cuentas Bancarias E2E y Accesibilidad N4.2.E` que inicia sesión por UI y valida `/cuentas-bancarias`, elementos accesibles por rol y el estado `aria-expanded` del flujo de alta. Por tanto, la clasificación correcta es **E2E/Playwright PRESENT**, no ausente.

## Dictamen QA takeover

La matriz documental queda corregida y materializada por ChatGPT/VAEP tras agotamiento/stall del recovery Jules C R2 FINAL. No se solicita ni se permite Jules R3 para esta tarea.

`N4.2.H` **NO debe marcarse LISTO_REAL por este documento por sí solo**. El cierre solo puede ocurrir cuando el HEAD resultante de esta integración tenga CI/gates aplicables terminales y siga cumpliendo DoD + P0=0/P1=0.

## Invariantes

- No modificar `main`.
- No Producción.
- No ramas nuevas.
- No merge ni auto-merge.
- No force-push.
- No secrets.
- No deploy.
