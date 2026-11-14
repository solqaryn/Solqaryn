# ERD N1.9 — Series, lotes y vencimientos

## Modelo lógico

```mermaid
erDiagram
    ProductoVariante ||--o{ ExistenciaVariante : tiene
    ProductoVariante ||--o{ LoteInventario : define
    ProductoVariante ||--o{ SerieInventario : serializa
    LoteInventario ||--o{ SerieInventario : agrupa_opcionalmente

    ProductoVariante {
      int Id PK
      bool ControlaLote
      bool ControlaNumeroSerie
      bool ControlaFechaVencimiento
      int DiasAlertaVencimiento nullable
    }

    ExistenciaVariante {
      int Id PK
      int ProductoVarianteId FK
      int AlmacenId FK
      int UbicacionAlmacenId FK_nullable
      int StockFisico
      int StockReservado
      int StockTransito
    }

    LoteInventario {
      int Id PK
      int ProductoVarianteId FK
      string Codigo
      date FechaFabricacion nullable
      date FechaVencimiento nullable
      bool Activo
    }

    SerieInventario {
      int Id PK
      int ProductoVarianteId FK
      int LoteInventarioId FK_nullable
      string NumeroSerie UK
      int Estado
    }
```

## Invariantes

- `LoteInventario.Codigo` es único dentro de la variante según la configuración persistente.
- `SerieInventario.NumeroSerie` tiene unicidad persistente.
- una serie ligada a lote debe pertenecer a la misma variante del lote;
- vencimiento sólo aplica cuando la política de la variante lo permite;
- FKs usan semántica restrictiva para preservar historial;
- `ExistenciaVariante` sigue siendo autoridad de cantidades; lote/serie representan identidad trazable.