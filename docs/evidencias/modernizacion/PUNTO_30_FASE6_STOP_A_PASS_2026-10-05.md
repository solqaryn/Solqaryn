# Punto 30 — Dictamen fail-closed de Fase 6

Fecha: 2026-10-05 (UTC)  
Repositorio: `solqaryn/Solqaryn`  
Rama candidata: `codex/p30-fail-closed-baseline`  
PR hacia `dev`: [#3524](https://github.com/solqaryn/Solqaryn/pull/3524)  
HEAD del PR certificado: `49549a4583dd327d6c63e4a02b632f104827fa15`

## Hallazgo corregido

La comparación directa del provider Oracle contra el historial Pomelo no certifica drop-in:

- `ORACLE_DROP_IN=false`
- `ORACLE_FRESH_MIGRATIONS=false`
- `ORACLE_SCHEMA_EQUIVALENT=false`

Esos resultados no se ocultan ni se reinterpretan como compatibilidad directa. La ruta propuesta conserva el historial productivo Pomelo y adopta un baseline Oracle explícito. El defecto del gate era que no exigía la prueba de esa estrategia en el mismo HEAD antes de elegir Oracle.

## Evidencia del candidato

El workflow Fase 6 del PR [37361628462](https://github.com/solqaryn/Solqaryn/actions/runs/37361628462), sobre el HEAD candidato indicado arriba, finalizó con éxito:

- `Dictamen Fase 6`: success; `FASE_6_MYSQL_EF_PROVIDER=PASS`; `P0=0`; `P1=0`.
- La prueba reutilizable de baseline/adopción terminó success en ese run.
- La ruta limpia produjo y comparó el baseline físico: equivalencia exacta reportada, 136 tablas.
- La adopción sobre un esquema Pomelo existente pasó sin DDL físico.
- La prueba confirmó el retorno a Pomelo, la conservación de las 107 migraciones históricas y la preservación de esquema y datos.
- Oracle EF10/net10, la comparación Connector/NET, la validación aislada de net10 y la matriz de integración Pomelo terminaron success.
- La ruta candidata queda etiquetada `ORACLE_EF10_WITH_CERTIFIED_BASELINE_ADOPTION`; no se afirma que sea drop-in para las 107 migraciones.
- `PHASE7_EXECUTED=false`. No se cambiaron los proyectos productivos ni se ejecutó Fase 7.

La corrección añade la prueba baseline/adopción como dependencia obligatoria del dictamen. Un resultado distinto de success ya no puede habilitar la selección del provider objetivo.

## Estado de cierre

**Candidato del PR: PASS. Cierre definitivo de P30 en DEV: pendiente de integrar el PR aprobado por sus checks y verificar el nuevo SHA exacto de `dev`.** Hasta entonces esta evidencia no declara cerrado P30 ni autoriza ejecutar Fase 7, modificar `main`, Aiven, QA o PROD.

MAPA_ARQUITECTURA: SIN_CAMBIO.
