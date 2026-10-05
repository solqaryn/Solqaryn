# Punto 28 — Gate CI permanente para provider

Fecha: 2026-10-05 (UTC)  
Repositorio: `solqaryn/Solqaryn`  
Rama de integración: `dev`

## Control permanente

El workflow `.github/workflows/modernization-phase6-mysql-ef-provider.yml` se ejecuta automáticamente en `push` a `dev` y en `pull_request` cuyo destino sea `dev`, cuando cambian backend, scripts de aislamiento/provider, evidencias modernización o contratos de contexto/arquitectura. También conserva `workflow_dispatch` como vía manual, no como única vía.

Para ambos eventos automáticos valida el destino/branch `dev`, el scope canónico del repositorio, net8/EF8/Pomelo 8 productivo y exactamente un provider productivo. Después corre el baseline MySQL, las comprobaciones físicas/funcionales y la lane aislada Oracle EF10/net10; el dictamen final falla cerrado como `STOP` si alguna autoridad o lane requerida falla. La lane candidata no modifica los proyectos productivos.

## Resultado

**Punto 28: CERRADO** — el gate es reproducible automáticamente tanto antes de integrar cambios en `dev` como después de cada push relevante a `dev`; una ejecución manual ya no es la única barrera.

- Push automático verificado: run `37265475905`, commit exact-head `a5c93f14d0a3ee29573e7f0dcd23d4addeddd699`.
- Cuatro jobs de certificación y job Dictamen: `success`; dictamen `FASE_6_MYSQL_EF_PROVIDER=PASS`, `P0=0`, `P1=0`, `PHASE7_EXECUTED=false`.
- Run: <https://github.com/solqaryn/Solqaryn/actions/runs/37265475905>.
- El punto 29 validará la certificación contra el HEAD posterior de `dev`. Fase 7 permanece sin ejecutar.

MAPA_ARQUITECTURA: SIN_CAMBIO.
