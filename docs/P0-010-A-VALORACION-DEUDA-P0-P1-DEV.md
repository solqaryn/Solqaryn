# P0-010.A — Valoración acotada de deuda P0/P1 en DEV (corte 2026-10-26)

**Proyecto:** SOLQARYN · **Repositorio:** `solqaryn/Solqaryn` · **Rama de destino:** `dev` · **Task propietaria:** `6adbb9fff1548191a1bd5b30d9e9984a` / Javier PRIMARY_DEV_01 · **Corte UTC:** `2026-10-26T02:05:06Z` · **HEAD observado:** `acf5bca44a94d1bce9a39a6b30be1380e4e5f464`.

## Autoridad y método

Fuentes autorizadas: `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` (Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), `PLAN_MAESTRO!A2:I100` del Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`, issues abiertos del repositorio original y estado vivo `dev`. `docs/baselines/P0-010-A-DEV-20261025.json` conserva el corte anterior y **no** reemplaza la consulta vigente. El nuevo corte reproducible está en `docs/baselines/P0-010-A-DEV-20261026.json`. Los conteos son históricos desde esa lectura, **no una consulta permanente ni un diagnóstico de producción**.

Se consultaron estados de 56 microtareas comerciales en Google Sheets y GitHub Issues con `repo:solqaryn/Solqaryn is:issue is:open`. La única incidencia GitHub abierta observada fue #3730, seguimiento de coordinación histórica; no aporta reproducción de defecto de aplicación. **Un issue de coordinación no se transforma en bug ERP**. Tampoco una fila P0/P1 pendiente equivale a un defecto reproducido. Los incidentes fuera de GitHub/Sheet no se han verificado.

## Resultado observado

| Prioridad | LISTO | VALIDANDO | EN_PROGRESO | PENDIENTE | Total |
| --- | ---: | ---: | ---: | ---: | ---: |
| P0 | 14 | 1 | 3 | 6 | 24 |
| P1 | 0 | 0 | 0 | 32 | 32 |
| **Total** | **14** | **1** | **3** | **38** | **56** |

P0 sin `LISTO`: `P0-001.B`, `P0-003.B`, `P0-004.A`, `P0-004.B`, `P0-005.A`, `P0-005.B`, `P0-008.A`, `P0-008.B`, `P0-010.A`, `P0-010.B`.

En ese corte, dos PR comerciales estaban abiertos: #3758 (`P0-003.B`) y #3761 (`P0-001.B`). Las ramas `work/solqaryn-p0-004-a` y `work/solqaryn-p0-010-a` representan entregas comerciales aún sin PR. **WIP comercial distinto observado: 4/6**; ramas `test/`, gobernanza e integradas no cuentan. Este censo tampoco sustituye el chequeo de exclusividad previo a cada escritura.

## Afirmaciones expresamente NO acreditadas

- No se afirma `0 defectos`, ausencia de incidencias no registradas, ni que las 56 filas sean bugs reproducidos.
- `P0-001.B` tiene **NO_GO de paridad proveedor**: un snapshot `READY` antiguo y un Vercel `CANCELED` no acreditan despliegue `READY/live` al SHA actual.
- `P0-003.B` conserva su propio estado, PR y pruebas; tampoco hereda paridad de otro alcance.
- No se auditaron datos productivos, credenciales, telemetría externa, E2E ni planes ajenos.
- El cierre documental de P0-010.A **no certifica funcionalidad ERP o despliegue**.

## Verificación dirigida, riesgos y rollback

`node scripts/verify-p0-010-a-debt-baseline.mjs --self-test` ejecuta un caso positivo del corte y **12 mutaciones negativas**: fecha/corte convertido indebidamente a live, conteos inflados o negativos, omisión P1, ausencia de defectos afirmada, confusión de backlog con bug, incidentes supuestamente verificados, paridad falsamente `READY`, duplicidad P0, reclasificación indebida del issue y pérdida de limitaciones. Se ejecutó localmente el 26-10-2026: positivo PASS y 12/12 negativos PASS. El workflow `.github/workflows/p0-010-a-debt-baseline.yml` debe ejecutarse sobre un checkout auténtico de GitHub Actions y el SHA final del PR; no se declara `CI` o prueba nativa hasta verificar su ejecución.

**Impacto:** solo JSON documental, validador Node estático, workflow CI de lectura y registro de cambios. Ninguna modificación a Angular, ASP.NET, API/DTO, MySQL/migraciones, RBAC, PII, secretos, deployments, QA/main/PROD, ni dependencias. **Riesgo:** snapshot obsoleto si varían COLA/PLAN/PR; mitigación: comprobar fuentes otra vez antes del cierre y conservar el timestamp inmutable. **Rollback:** revertir este changeset mediante PR protegido, conservando evidencias anteriores; no hay datos operativos que restaurar.

## Gates de cierre

Mantener `P0-010.A=EN_PROGRESO` hasta PR verdadero, prueba nativa y CI obligatorios sobre SHA exacto, diff revisado y aprobación independiente por GitHub login distinto de `jmejia31`. Solo un merge protegido a `dev`, HEAD comprobado y readback de `PLAN_MAESTRO!H20:M20`, `COLA!E20` (solo fórmula) y BITACORA idempotente permiten `LISTO`. El objetivo de seis cierres por hora no sustituye ninguno de estos gates.
