# Punto 23 — DbContext, DI y design-time factory

Fecha: 2026-10-05 (UTC)  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`

## Inspección del código productivo

- `AppDbContext` recibe `DbContextOptions<AppDbContext>` y pasa esas opciones al constructor base; no tiene `OnConfiguring` ni elige provider por sí solo.
- Hay un solo registro runtime de `AppDbContext`, en `API/Program.cs`: `AddDbContext<AppDbContext>` configura Pomelo y agrega el interceptor desde DI en el mismo options builder. La búsqueda del repositorio no encontró `AddDbContextFactory`, `AddDbContextPool` ni otro registro del contexto.
- Hay una sola `IDesignTimeDbContextFactory<AppDbContext>`, `AppDbContextFactory`. Toma conexión y versión desde variables de entorno, usa fallback exclusivamente de diseño y configura el mismo provider Pomelo. No necesita arrancar la API ni depender de servicios de runtime.
- La factory de diseño no registra el interceptor de runtime ni intenta resolver DI: esto es intencional y separa el host EF design-time del host API. La configuración productiva sigue siendo single-provider; no existe una selección dual permanente.

## Verificación del carril candidato

- El script `scripts/modernization/oracle10-provider-lane.sh` crea una copia efímera y retargetea **ambos** puntos de configuración (`API/Program.cs` y `AppDbContextFactory.cs`) de `.UseMySql(..., MySqlServerVersion(...))` a `.UseMySQL(...)`. Luego restaura/compila la API completa y ejecuta `dotnet ef dbcontext info` con `AppDbContext`, startup `API`, y el CLI/design/runtime EF 10.0.12. No modifica los archivos productivos del checkout.
- El script verifica la sustitución de provider en el resto de las pruebas candidatas; el workflow de baseline Oracle genera y aplica `OracleBaseline` desde una `IDesignTimeDbContextFactory<OracleBaselineDbContext>` aislada, y `dotnet ef migrations add/script/database update` usa explícitamente ese contexto y host temporales.
- Prueba exact-head de Fase 6 para `864c07dd1910c3c174d645a84b8778085111ff1b`: run `37322001456`, `success`. El log de Oracle EF10 registra `ORACLE10_EPHEMERAL_REWRITE=PASS`, compilación API exitosa, `dbcontext info`/toolchain `runtime=10.0.12 design=10.0.12 cli=10.0.12`, 2,336 unitarias, 22 integraciones, 34 probes LINQ y `ORACLE_EF10_NET10_PROVIDER_LANE=PASS`. El gate completo también mantuvo Pomelo actual y la copia net10 con provider actual en verde; `P0=0`, `P1=0`.

## Dictamen

**Punto 23: CERRADO.** Runtime DI, factory de diseño y carriles efímeros están delimitados sin provider duplicado en producción; tanto el host API como `dotnet ef` quedaron compilados/probados para el retarget aislado Oracle EF10. No se cambia TFM, paquetes ni provider productivo, ni se inicia Fase 7.

MAPA_ARQUITECTURA: SIN_CAMBIO.
