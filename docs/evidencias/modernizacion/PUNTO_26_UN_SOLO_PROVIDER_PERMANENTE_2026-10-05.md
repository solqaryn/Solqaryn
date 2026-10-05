# Punto 26 — Un solo provider permanente

Fecha: 2026-10-05 (UTC)  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`  
HEAD exacto validado: `d3a4084870b1f5afe16c387ead17a2f9b09beb98`

## Estado productivo observado

- El único `PackageReference` provider en `backend/src/**/*.csproj` es `Pomelo.EntityFrameworkCore.MySql 8.0.2`; no hay `MySql.EntityFrameworkCore` Oracle ni otra referencia de provider permanente.
- La API registra una sola autoridad de persistencia: `AddDbContext<AppDbContext>` con `UseMySql`; `AppDbContextFactory` también crea el contexto con Pomelo. El candidato Oracle aún no se configura en código productivo.
- La lane `oracle10-provider-lane.sh` copia el repo a `$RUNNER_TEMP/solqaryn-oracle10-lane/repo` y modifica sólo esa copia. La lane de baseline Oracle copia backend/proyecto a `$RUNNER_TEMP`; ambas incorporan Connector/NET temporal para certificar la ruta futura.

## Guard permanente de CI

En el gate `.github/workflows/modernization-phase6-mysql-ef-provider.yml`, la matriz inicial inspecciona todos los `.csproj` productivos. Ahora exige que el conjunto de providers sea exactamente `{ Pomelo.EntityFrameworkCore.MySql: 8.0.2 }`; agregar Oracle o dejar dos providers hace fallar el job antes de correr las pruebas.

En el run exact-head `37264684835`, job Pomelo `111618872888`, el guard reportó `PHASE6_SINGLE_PROVIDER_AUTHORITY=PASS product=Pomelo.EntityFrameworkCore.MySql/8.0.2`. Las 28 integraciones MySQL pasaron; los cuatro jobs del gate y el dictamen Fase 6 `111620301963` terminaron `success`.

**Punto 26: CERRADO**: hay una sola autoridad productiva y Oracle permanece temporal, aislado y verificable en CI. No se migró el código productivo a Oracle ni se inició Fase 7.

MAPA_ARQUITECTURA: SIN_CAMBIO.
