# Reconciliación de stock DEV — 2026-09-25

Estado: **CERRADO / RECONCILIADO**

## Causa

La pantalla administrativa mostraba cantidades desde el bridge de compatibilidad `ProductoVariante.Cantidad`. La tienda pública y el checkout, por diseño, fallan cerrados y usan `ExistenciaVariante.StockDisponible` como autoridad física. Tras migrar el dataset legacy, varias variantes activas no tenían una fila `ExistenciaVariante`, por lo que la tienda las mostraba agotadas aunque el bridge administrativo tuviera cantidad.

## Diagnóstico

GitHub Actions run `36188235798`:

- variantes legacy: 9;
- existencias físicas antes del fix: 1;
- total `ProductoVariante.Cantidad`: 57;
- total `ExistenciaVariante.StockFisico`: 10;
- productos activos/no eliminados afectados:
  - Cargador: 26 en bridge, 0 físico;
  - Laptop: 15 en bridge, 0 físico;
  - Funda para samsung: 1 en bridge, 0 físico;
  - UAT Producto 001: 10 en bridge, 10 físico.

La diferencia restante de 5 unidades pertenece a dos variantes cuyos productos están eliminados y no forman parte del catálogo activo.

## Backup previo

Run `36188392969`: backup cifrado de `solqaryn_dev` y restore/drill del mismo artifact, SUCCESS.

## Reconciliación

Run `36190745792`:

- base objetivo: `solqaryn_dev`;
- PROD touched: false;
- almacén determinista: ID 1, `UAT-ALM-001`;
- existencias creadas: 6;
- stock activo bridge antes/después: 52;
- stock físico activo después: 52;
- variantes activas sin existencia: 0;
- variantes activas con diferencia: 0.

La operación fue insert-only para variantes activas sin autoridad física; no sobrescribió la fila existente del producto UAT.

## Verificación pública

`GET /api/tienda/productos` posterior a la reconciliación devolvió:

| Producto | Cantidad disponible |
|---|---:|
| Cargador | 26 |
| Laptop | 15 |
| Funda para samsung | 1 |
| UAT Producto 001 | 10 |

Resultado: administración y storefront quedan reconciliados para los productos activos. `ExistenciaVariante` continúa siendo la autoridad física; el bridge legacy queda solo como compatibilidad.
