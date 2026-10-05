# Punto 27 — Documentación arquitectónica reconciliada

Fecha: 2026-10-05 (UTC)
Repositorio: `solqaryn/Solqaryn`
Rama: `dev`
Estado técnico usado para reconciliar documentos: HEAD `2d84babb2863cbaadf0e201b9ff03ed113ecd476`

## Verificación

Se contrastó la decisión de Fase 6 con el workflow de provider y sus ejecuciones exact-head. Los documentos requeridos distinguen:

- runtime vigente: `net8.0` + EF Core 8 + Pomelo 8.0.2;
- ruta certificada como candidata de Fase 7: `net10.0` + EF Core 10.0.12 + Oracle `MySql.EntityFrameworkCore` 10.0.9, únicamente en lanes/copias aisladas;
- las 107 migraciones históricas de Pomelo se preservan; la estrategia candidata usa baseline/adopción y no pretende reproducirlas como drop-in;
- Fase 6 está en `PASS`; Fase 7 no se ejecutó y el runtime productivo no fue retargeteado.

## Archivos arquitectónicos reconciliados

- `ARCHITECTURE.md`
- `PROJECT_CONTEXT.md`
- `PROJECT_INDEX.md`
- `ARCHITECTURE_CHANGELOG.md`
- `CHANGELOG_AI.md`

## Evidencia técnica

- Gate exact-head Fase 6 [37268913820](https://github.com/solqaryn/Solqaryn/actions/runs/37268913820), HEAD `2d84babb2863cbaadf0e201b9ff03ed113ecd476`: `success`, dictamen `FASE_6_MYSQL_EF_PROVIDER=PASS`, `P0=0`, `P1=0`, `PHASE7_EXECUTED=false`.
- Aceptación integral DEV [37268913926](https://github.com/solqaryn/Solqaryn/actions/runs/37268913926), Fase 5 [37268913737](https://github.com/solqaryn/Solqaryn/actions/runs/37268913737), Fase 4 [37268913942](https://github.com/solqaryn/Solqaryn/actions/runs/37268913942), Scope Lock [37268913872](https://github.com/solqaryn/Solqaryn/actions/runs/37268913872) y VAEP admission [37268913783](https://github.com/solqaryn/Solqaryn/actions/runs/37268913783) también terminaron en `success` sobre ese HEAD.

## Resultado

**Punto 27: CERRADO** respecto a la decisión arquitectónica de Fase 6 y la separación explícita de la ruta candidata. No declara que la migración productiva de provider ya ocurrió ni que Fase 7 esté autorizada.

MAPA_ARQUITECTURA: ACTUALIZADO.
