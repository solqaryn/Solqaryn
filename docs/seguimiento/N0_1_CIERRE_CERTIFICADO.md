# ERP-N0.1 — Cierre certificado

**Estado:** CERRADO Y CERTIFICADO  
**Fecha:** 2026-08-10  
**Rama:** `Desarrollo`

## Alcance cerrado

ERP-N0.1 establece `ProductoVariante` como autoridad operacional para dimensiones, SKU/código de barras, stock, costo, precio, umbral y estado de variante. `Producto` conserva la responsabilidad de familia y, temporalmente, columnas legacy únicamente como proyección/snapshot de compatibilidad.

Se completaron los 13 puntos requeridos:

1. inventario de columnas legacy;
2. consumidores backend;
3. consumidores frontend;
4. reportes dependientes;
5. migraciones históricas;
6. comparación `Producto` vs `ProductoVariante`;
7. script de backfill;
8. validación de datos;
9. cambio gradual de lecturas;
10. cambio gradual de escrituras;
11. desactivación de escritura legacy como autoridad;
12. eliminación/neutralización de dependencias operacionales;
13. evaluación de eliminación física de columnas.

El detalle técnico completo está en `docs/ERP_N0_1_PRODUCTO_LEGACY.md`.

## Decisión sobre DROP de columnas

No se eliminan físicamente en N0.1 `Cantidad`, `Costo`, `Precio`, `UmbralStockBajo`, `Marca`, `Modelo`, `ColorId`, `TallaId`, `MarcaId` ni `ModeloId`.

Esto es el resultado explícito del punto 13 ("eliminar columnas cuando sea seguro"), no un pendiente oculto. Se comprobaron dependencias históricas/compatibilidad que hacen inseguro el DROP inmediato, especialmente snapshots y reversión de compras en `AppDbContext`, además de la diferencia de autoridad FK entre `CatalogosProducto` y las tablas normalizadas.

Desde N0.1 esas columnas no son autoridad operacional cuando existen variantes; son proyecciones/snapshots temporales sujetas al gate de eliminación documentado.

## Evidencia de implementación

Commits funcionales:

- `a2ca49f0fcc692526242bdd26176b65efb980e92` — autoridad de `ProductoVariante`, lecturas/escrituras, scripts, documentación y pruebas.
- `e2957a62ff6e9c7c1ff1429de533421d1b101b9c` — compatibilidad validada del contrato legacy.
- `23b2b558bac32a86e4fb62ec69882e28eccc18af` — CI específico N0.1 con MySQL 8.4.

## Certificación automatizada

### Pipeline general de `Desarrollo`

Run `31446116670`: **5/5 jobs exitosos**.

- higiene del repositorio: OK;
- frontend `npm ci` + lint + build de producción: OK;
- Docker: OK;
- backend Release + pruebas: OK;
- integración MySQL: OK.

### Pipeline específico ERP-N0.1

Run `31446116699`: **exitoso**.

Sobre MySQL 8.4 limpio con el esquema EF actual se ejecutó:

1. creación completa del esquema;
2. seed de un producto legacy sin variante;
3. preflight N0.1;
4. backfill N0.1;
5. post-validación de autoridad;
6. comprobación de variante técnica;
7. comprobación de alineación stock/costo/precio/umbral;
8. pruebas `ProductoVarianteAuthorityTests`.

Resultado: **verde**.

## Scripts certificados

- `backend/scripts/preflight-erp-n0-1-producto-variante.sql`
- `backend/scripts/backfill-erp-n0-1-producto-variante.sql`

Los scripts fueron ejecutados en CI contra una base MySQL efímera creada con el esquema actual. **No se afirma ni se realiza desde este cierre una ejecución sobre la base de producción.** La aplicación en producción requiere ejecutar el preflight sobre los datos reales y solo continuar si el resultado cumple los gates documentados.

## Resultado final

`ProductoVariante` queda establecido y probado como autoridad operacional. `Producto` queda reducido conceptualmente a familia + proyección temporal de compatibilidad. ERP-N0.1 puede considerarse **cerrado** y no bloquea el siguiente punto del plan.
