# Certificación funcional final DEV — 2026-09-25

Estado: **PASS / CERRADO**

## Ejecución canónica

- Workflow: `DEV - Certificación funcional final`
- Run: `36192919335`
- Resultado: `SUCCESS`
- Producción tocada: `FALSE`

Salida final del gate:

- `HTTP_ROUTE_CHECK=PASS`
- `API_AND_IMAGES=PASS`
- `RENDER_HEALTH=PASS`
- `DB_TEMP_WRITE_READ=PASS`
- `LEGACY_RUNTIME_REFERENCES=0`
- `FINAL_DEV_CERTIFICATION=PASS`

## Rutas

| Ruta | Resultado |
|---|---|
| `/login` | HTTP 200; branding SOLQARYN |
| `/dashboard` | HTTP 200; shell SOLQARYN; administrativo autenticado confirmado visualmente |
| `/SOLQARYN` | HTTP 200 |
| `/SOLQARYN/productos` | HTTP 200 |

## APIs públicas

### Identidad

`/api/empresa-configuracion/publica`:

- nombre comercial: `SOLQARYN`;
- eslogan: `Eleva tu mundo digital`;
- contexto cliente separado del shell SOLQARYN.

### Categorías

`/api/tienda/categorias`:

- `Cargadores`;
- `Fundas/Cobertores`.

Total observado: **2**.

### Productos

`/api/tienda/productos?pagina=1&tamano=100`:

- Cargador: 26 disponibles;
- Funda para samsung: 1 disponible;
- Laptop: 15 disponibles;
- UAT Producto 001: 10 disponibles.

Total público: **4**.

## Cloudinary

Todas las URLs de imagen enumeradas por la API fueron verificadas con HTTP 200.

Cloud canónico:

`res.cloudinary.com/riyrzmob/.../solqaryn_dev/...`

No se detectaron URLs del cloud personal legacy en el catálogo público.

## Render

Endpoint:

`https://solqaryn-api-dev-fxx8.onrender.com/health/ready`

Resultado: **HTTP 200**.

Readback de logs actual:

- conexiones a base: `solqaryn_dev`;
- host: `solqaryn-mysql-solqaryn.h.aivencloud.com`;
- búsqueda de `identidad-retirada-mysql-identidad-retirada.c.aivencloud.com`: 0 logs;
- búsqueda de `identidad-retirada-desarrollo`: 0 logs.

## Aiven DEV write/read

El gate creó una tabla **TEMPORARY** dentro de la sesión MySQL de `solqaryn_dev`, insertó un marcador único, lo leyó de vuelta y eliminó la tabla temporal.

Resultado: **PASS**.

La prueba no dejó datos persistentes y no tocó PROD.

## Cero referencias operativas legacy

El gate escaneó runtime/source canónico:

- `frontend/src`;
- `frontend/server`;
- `frontend/vercel.json`;
- `backend/src`;
- `render.yaml`.

Resultado:

- `identidad-retirada-desarrollo`: 0;
- `identidad-retirada-mysql-identidad-retirada.c.aivencloud.com`: 0;
- `SOLQARYN_desarrollo`: 0.

## Cierre

Los criterios definidos para la prueba funcional final DEV quedan satisfechos. Este punto queda cerrado antes de continuar con cualquier retiro adicional de recursos personales.
