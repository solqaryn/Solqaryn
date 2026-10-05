# Punto 23 — DbContext, DI y design-time factory

Fecha: 2026-10-05 (UTC)  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`  
HEAD de la lane candidata: `46e52ea9887db783183c2bfe529819b487547685`

## Revisión

- `AppDbContext` recibe `DbContextOptions<AppDbContext>` por constructor y no tiene `OnConfiguring`; el contexto no selecciona ni agrega un segundo provider por su cuenta.
- El único registro runtime del contexto está en `API/Program.cs`: un `AddDbContext<AppDbContext>` añade el provider y el `DbQueryTimingInterceptor` en la misma configuración.
- La única factory de diseño es `AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>`. Usa la conexión y versión leídas de variables de entorno, con fallback de diseño; configura el mismo provider y no registra una segunda instancia runtime.
- El código productivo permanece consistentemente en Pomelo mientras su TFM productivo es net8/EF8. En la lane aislada Oracle, el script modifica temporalmente tanto `Program.cs` (DI) como `AppDbContextFactory.cs` y reemplaza la pareja `.UseMySql(..., MySqlServerVersion(...))` por `.UseMySQL(...)`. Luego restaura/builda la API completa; los probes ejecutados con esa lane terminaron PASS en los runs `37261240274` y `37261240261`.
- No existe `AddDbContextFactory`, `AddDbContextPool` ni un `OnConfiguring` paralelo en los proyectos productivos; el scope contiene un único registro runtime y una única design-time factory.

## Dictamen

**Punto 23: CERRADO**: configuración runtime y de diseño singular, coherente dentro de cada lane y validada mediante build/probes Oracle efímeros. No se añade una selección dual permanente: la migración del código productivo al provider candidato queda para el cambio de framework/provider autorizado después del gate, no se adelanta en esta verificación.

No se modificaron TFM, paquetes ni configuración productiva; no se inició Fase 7.

MAPA_ARQUITECTURA: SIN_CAMBIO.
