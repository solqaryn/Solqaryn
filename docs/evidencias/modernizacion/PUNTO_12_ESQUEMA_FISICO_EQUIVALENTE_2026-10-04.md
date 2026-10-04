# Punto 12 — Equivalencia física de esquema

Fecha: 2026-10-04  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`

## Hallazgo y corrección

La lane de baseline ya comparaba tablas, columnas, índices, constraints, FK, CHECK, vistas, triggers, rutinas y eventos. La revisión detectó que faltaban algunos metadatos físicos: formato/comentario de tabla, charset explícito, expresión/comentario/SRS de columnas, propiedades de índices funcionales/visibilidad/prefijo, particiones, parámetros de rutinas y defaults de charset/collation de la base. Se amplió el comparator para incluirlos además de los contratos previamente cubiertos.

La captura ahora compara, entre la referencia creada aplicando las migraciones Pomelo, la restauración SQL canónica y el bootstrap Oracle EF10:

- Tablas: engine, row format, collation, create options y comentario.
- Columnas: tipo completo, nullabilidad, default, extra, charset/collation, expresión generada, comentario y SRS.
- Índices: unicidad, orden, columna/expresión, longitud de prefijo, dirección, tipo, visibilidad y comentario.
- Particiones/subparticiones y sus expresiones, límites, engine y opciones.
- Constraints, estado de enforcement de CHECK, FK y columnas/orden/reglas referenciales.
- Vistas, triggers, rutinas, parámetros de rutinas y eventos.
- Charset y collation por defecto de cada schema.

Cada exportación se ordena de forma determinista y se compara con `diff`; cualquier diferencia falla el job. Se excluye sólo `__EFMigrationsHistory` del cotejo del schema de aplicación, pues los proveedores mantienen historial distinto.

## Evidencia

- Run previo `37232938573` sobre `aeee293abb4beb757ff4196ebd52308bd54f2320`: comparator anterior success, 136 tablas en el paquete SQL limpio y adopción sin DDL. No basta por sí solo para certificar los nuevos campos.
- Run exact-head de la comparación ampliada: **pendiente de CI posterior a este commit**. No se declarará PASS hasta que su step `Capturar y comparar esquema físico Pomelo vs Oracle baseline` pase.
- Se comprobó que entre el baseline previo y el HEAD de trabajo no cambiaron modelo, configuraciones ni historia de migraciones; el nuevo run valida ahora además el comparador ampliado.

## Alcance

La prueba compara el contrato físico producido por las rutas CI efímeras en MySQL 8.4. No modifica esquemas persistentes ni datos reales y no ejecuta Fase 7.

MAPA_ARQUITECTURA: SIN_CAMBIO.
