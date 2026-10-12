# Punto 27 — Documentación arquitectónica reconciliada

Fecha: 2026-10-05 (UTC)
Repositorio: `solqaryn/Solqaryn`
Rama: `dev`
Estado técnico usado para reconciliar documentos: HEAD `425f9cab0ed291401c081d7946289875eb11e798`

## Verificación

Se contrastó la decisión de Fase 6 con el workflow de provider y sus ejecuciones exact-head. Los documentos requeridos distinguen:

- runtime vigente: `net8.0` + EF Core 8 + Pomelo 8.0.2;
- ruta certificada como candidata de Fase 7: SDK 10.0.401/runtime 10.0.12 + `net10.0`/EF Core/Design/CLI 10.0.12 + Oracle `MySql.EntityFrameworkCore` 10.0.9, únicamente en lanes/copias aisladas;
- las 107 migraciones históricas de Pomelo se preservan; la estrategia candidata usa baseline/adopción y no pretende reproducirlas como drop-in;
- Fase 6 está en `PASS`, `P0=0`, `P1=0`; Fase 7 no se ejecutó y el runtime productivo no fue retargeteado.

## Archivos arquitectónicos reconciliados

- `ARCHITECTURE.md`
- `PROJECT_CONTEXT.md`
- `PROJECT_INDEX.md`
- `ARCHITECTURE_CHANGELOG.md`
- `CHANGELOG_AI.md`

## Evidencia técnica

- Gate exact-head Fase 6 [37330696772](https://github.com/solqaryn/Solqaryn/actions/runs/37330696772), HEAD `425f9cab0ed291401c081d7946289875eb11e798`: `success`, dictamen `FASE_6_MYSQL_EF_PROVIDER=PASS`, `P0=0`, `P1=0`, `PHASE7_EXECUTED=false`; scope lock [37330696444](https://github.com/solqaryn/Solqaryn/actions/runs/37330696444) `success` sobre el mismo SHA.
- Certificación posterior del commit que contiene esta reconciliación, HEAD `d6d44590a96c0e17a195c78baf00315a839c6e66`: Fase 6 [37332208076](https://github.com/solqaryn/Solqaryn/actions/runs/37332208076), Fase 4 [37332207849](https://github.com/solqaryn/Solqaryn/actions/runs/37332207849), Fase 5 [37332207997](https://github.com/solqaryn/Solqaryn/actions/runs/37332207997), aceptación DEV [37332207768](https://github.com/solqaryn/Solqaryn/actions/runs/37332207768), Scope Lock [37332207857](https://github.com/solqaryn/Solqaryn/actions/runs/37332207857) y VAEP [37332207747](https://github.com/solqaryn/Solqaryn/actions/runs/37332207747): todos `success` sobre el mismo SHA.

## Resultado

**Punto 27: CERRADO** respecto a la decisión arquitectónica de Fase 6 y la separación explícita de la ruta candidata. No declara que la migración productiva de provider ya ocurrió ni que Fase 7 esté autorizada.

MAPA_ARQUITECTURA: ACTUALIZADO.
