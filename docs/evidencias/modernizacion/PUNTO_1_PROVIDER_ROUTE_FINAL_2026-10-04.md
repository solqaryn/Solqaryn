# Punto 1 — Ruta estable de provider para modernización .NET 10

Fecha: 2026-10-04  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`  
HEAD certificado: `4069ce1a074145d10e68003c110296a4b39b655a`  
Run causal: `37222690061`  
Job de dictamen: `111497236868`

## Dictamen

`POINT_1_PROVIDER_STABLE_ROUTE=PASS`

`FASE_6_PROVIDER_MODERNIZATION_ROUTE=PASS`

`CURRENT_PROVIDER=POMELO_EF8_NET8_CERTIFIED`

`TARGET_PROVIDER=ORACLE_MYSQL_EFCORE_10_0_9`

`TARGET_EF=10.0.12`

`TARGET_NET=net10.0`

`TARGETFRAMEWORK_CHANGE=ALLOWED_AFTER_PHASE6_CLOSE`

`PHASE7_EXECUTED=false`

`P0=0`

`P1=0`

## Evidencia técnica

- El stack vigente `net8.0 + EF Core 8 + Pomelo 8.0.2 + MySqlConnector 2.3.7` pasó nuevamente regresión completa, migraciones, `has-pending-model-changes`, integración MySQL y auditoría tenant.
- La lane objetivo efímera `net10.0 + EF Core 10.0.12 + MySql.EntityFrameworkCore 10.0.9` compiló y ejecutó sobre el backend productivo sin modificar los `.csproj` vigentes.
- La lane Oracle EF10 pasó conexión runtime, LINQ, modelo JSON, `decimal(18,2)`, `decimal(18,4)`, `datetime(6)`, transacciones/rollback, traducción 1062 y retry transitorio.
- El historial Pomelo no se reescribe. La estrategia certificada usa un baseline físico canónico MySQL y una adopción Oracle sin DDL sobre esquemas históricos.
- La clasificación de errores MySQL quedó provider-neutral y fail-closed para MySqlConnector y Connector/NET.
- `EnvironmentDatabaseGuard` conserva aislamiento canónico de entorno sin depender de un connection-string builder específico del provider.
- No se modificaron QA, `main`, PROD, Aiven real, datos productivos ni secretos.

## Estado de migración

El Punto 1 queda **CERRADO**. La Fase 7 continúa **sin ejecutar**. Este cierre autoriza técnicamente el cambio futuro de TargetFramework sólo cuando se inicie formalmente la Fase 7 y se apliquen sus versiones/gates completos.

MAPA_ARQUITECTURA: SIN_CAMBIO.

## Revalidación exact-head — 2026-10-04

- HEAD de `dev`: `228df9cff10e6c3003e949f47d2cf951768dfdc1`.
- Run causal de GitHub Actions: [37226132134](https://github.com/solqaryn/Solqaryn/actions/runs/37226132134), asociado al mismo HEAD.
- Resultado general: `success`; los cinco jobs (`Pomelo actual`, `Oracle Connector NET`, `net10 aislado`, `Oracle EF10 net10` y `Dictamen Fase 6`) terminaron `success`.
- La referencia viva `dev` se confirmó en el mismo commit antes de actualizar esta evidencia.
- Dictamen del punto 1: `PASS`; Fase 7 no se ejecutó y el `TargetFramework` productivo no cambió.


## Revalidación exact-head DEV — 2026-10-06

- HEAD de `dev`: `f65f7866c09eecadae292276324f299f5200d9b9`.
- Gate de Fase 6 [run 37409611958](https://github.com/solqaryn/Solqaryn/actions/runs/37409611958) sobre ese mismo SHA: los seis jobs, incluido `Dictamen Fase 6`, terminaron `success`.
- El dictamen confirmó `FASE_6_MYSQL_EF_PROVIDER=PASS`, `TARGET_PROVIDER_ROUTE=ORACLE_EF10_WITH_CERTIFIED_BASELINE_ADOPTION`, `P0=0`, `P1=0` y `PHASE7_EXECUTED=false`.
- La lane Oracle EF10/net10 informó `ORACLE_EF10_NET10_PROVIDER_LANE=PASS`; el probe en el mismo gate exigió que la baseline/adopción terminara `success`.
- Scope Lock [run 37409611897](https://github.com/solqaryn/Solqaryn/actions/runs/37409611897) y VAEP [run 37409611790](https://github.com/solqaryn/Solqaryn/actions/runs/37409611790) también terminaron `success` sobre el mismo SHA.
- Este gate fue aislado de los entornos persistentes. No cambió los proyectos productivos ni el TargetFramework; Fase 7 no se ejecutó.

**Revalidación del Punto 1: PASS en `dev` HEAD `f65f7866c09eecadae292276324f299f5200d9b9`.** La ruta técnica queda certificada como Oracle EF10 con baseline/adopción obligatoria; no se afirma compatibilidad drop-in de la historia Pomelo.
