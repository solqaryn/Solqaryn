# Punto 26 — Un solo provider permanente

Fecha: 2026-10-05 (UTC)  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`

## Estado productivo inspeccionado

- En `backend/src/**/*.csproj` existe una sola referencia EF provider: `Pomelo.EntityFrameworkCore.MySql 8.0.2`, en `Infrastructure/Solqaryn.Infrastructure.csproj`. No hay una referencia permanente a `MySql.EntityFrameworkCore`.
- La API configura una sola autoridad runtime (`AddDbContext<AppDbContext>` + `UseMySql`); `AppDbContextFactory` también usa Pomelo. No hay `UseMySQL` Oracle productivo ni configuración de dos providers.
- `MySqlConnector 2.3.7` es la implementación/conector que acompaña a Pomelo, no un segundo EF provider. El clasificador también reconoce el tipo Oracle por nombre para mantener el contrato provider-neutral, pero no añade su paquete, registro ni uso en DI productiva.
- Los carriles Oracle EF10 retargetean copias efímeras bajo `$RUNNER_TEMP`; no escriben provider Oracle en el checkout ni en proyectos productivos.

## Guard permanente endurecido

La matriz de `.github/workflows/modernization-phase6-mysql-ef-provider.yml` recorre todos los `.csproj` de `backend/src` y ahora recopila **cada ocurrencia** de referencia provider junto con versión y ruta. Exige que la lista completa sea exactamente:

`[Pomelo.EntityFrameworkCore.MySql, 8.0.2, backend/src/Infrastructure/Solqaryn.Infrastructure.csproj]`

Esto rechaza Oracle, versiones inesperadas, referencias Pomelo duplicadas y provider agregado a otro proyecto. La versión anterior del guard usaba un diccionario; una referencia duplicada con la misma clave podía colapsarse, por lo que se reemplazó por la lista exacta.

## Certificación

- HEAD: `f5dfad3778b4c0921a88c983e46068d594bce3a8`.
- Fase 6 exact-head: run `37329458164`, `success`; scope lock `37329458328`, `success`.
- El log confirmó `PHASE6_SINGLE_PROVIDER_AUTHORITY=PASS product=Pomelo.EntityFrameworkCore.MySql/8.0.2`; las pruebas de integración MySQL Pomelo pasaron 28/28. El resto de lanes del gate y el dictamen Fase 6 también terminaron `success`.

**Punto 26: CERRADO.** Existe una sola autoridad productiva y CI impide introducir más de una referencia provider; Oracle queda sólo como candidato en copias temporales. No se cambió el provider productivo ni se inició Fase 7.

MAPA_ARQUITECTURA: SIN_CAMBIO.
