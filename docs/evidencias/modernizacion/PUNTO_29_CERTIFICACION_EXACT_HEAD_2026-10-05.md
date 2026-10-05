# Punto 29 — Certificación exact-head

Fecha: 2026-10-05 (UTC)  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`  
HEAD certificado: `c8cf5e23704c3da6c17b9eab6782bb78d02b281a`

## Ejecuciones del mismo HEAD

| Cobertura | Run | Resultado |
| --- | --- | --- |
| Aceptación integral DEV (E2E canónico) | [37266823477](https://github.com/solqaryn/Solqaryn/actions/runs/37266823477) | `success` |
| Fase 4 — Angular 22 | [37266823462](https://github.com/solqaryn/Solqaryn/actions/runs/37266823462) | `FASE_4_ANGULAR_22=PASS`, E2E incluido |
| Fase 5 — Node/npm, Playwright, Vitest, jsdom, lint, auditoría y build | [37266823544](https://github.com/solqaryn/Solqaryn/actions/runs/37266823544) | `FASE_5_TOOLING_FRONTEND=PASS`; Playwright exact-head y aceptación canónica completados |
| Fase 6 — MySQL/EF provider | [37266823449](https://github.com/solqaryn/Solqaryn/actions/runs/37266823449) | `FASE_6_MYSQL_EF_PROVIDER=PASS`, `P0=0`, `P1=0`, `PHASE7_EXECUTED=false` |
| Scope lock del repositorio | [37266823494](https://github.com/solqaryn/Solqaryn/actions/runs/37266823494) | `success` |
| Invariante de admisión VAEP | [37266823620](https://github.com/solqaryn/Solqaryn/actions/runs/37266823620) | `success` |

Las seis ejecuciones reportaron el mismo `head_sha` indicado arriba. Fase 6 finalizó todos sus jobs —Pomelo/MySQL, compatibilidad Oracle, probe net10 aislado, lane Oracle EF10 y dictamen— en `success`. Fase 4/5 y la aceptación integral también terminaron correctamente sobre ese HEAD.

## Límite operativo

Los artefactos de baseline previo a la migración siguen siendo evidencia histórica de la congelación Fase 0. No se inició mantenimiento Aiven ni se repitió una copia de datos reales durante esta certificación; las validaciones actuales de código y datos se ejecutaron en CI aislado y el gate Aiven continúa fail-closed. No se modificaron QA, `main` ni PROD.

## Resultado

**Punto 29: CERRADO** — la regresión y la certificación integrada de la solución vigente pasaron contra un único HEAD de `dev`, con cero P0/P1. Fase 7 no ejecutada.

MAPA_ARQUITECTURA: SIN_CAMBIO.
