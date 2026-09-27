# Certificación Cloudinary PROD — 2026-09-27

Estado: **PASS / CERRADO A NIVEL DE CREDENCIALES, RUNTIME Y AISLAMIENTO**

## Runtime certificado

Servicio Render:

- `solqaryn-api-prod`
- rama: `main`
- deploy de certificación: `dep-dasacvnpn0mc73fi53k0`
- commit: `48e041ad2a152c0486cb3e58e590de5e99dceab8`
- estado: `LIVE`
- entorno ASP.NET: `Production`

Contrato Cloudinary PROD observado por el runtime:

- cloud: `riyrzmob`
- prefijo: `solqaryn_prod`
- API key/secret: configurados como secretos de Render; sus valores no fueron impresos.
- autenticación Admin API: **PASS**

Evidencia de log:

```text
CLOUDINARY_PROD_CERT=PASS cloud=riyrzmob prefix=solqaryn_prod api_authenticated=true
```

La prueba se ejecutó desde el propio runtime productivo y autenticó contra la API de Cloudinary sin exponer credenciales.

## Aislamiento DEV / PROD

- DEV: `solqaryn_dev`
- PROD: `solqaryn_prod`

El prefijo productivo es distinto del prefijo DEV y el código de storage conserva validaciones fail-closed por prefijo/tenant.

## Inventario histórico previo a migración

Workflow read-only:

- `PROD - Historical Cloudinary inventory`
- run: `36296747821`
- resultado: **SUCCESS**
- escrituras productivas: **0**

El respaldo histórico verificado fue restaurado y llevado al esquema actual en MySQL 8.4 aislado. El inventario resultó:

- referencias URL Cloudinary: **351**
- tabla: `ProductoImagenes`
- columna URL: `Url` — 351 referencias Cloudinary
- columna locator: `PublicId` — 351 valores
- otros storage surfaces históricos con referencia Cloudinary: **0** en este respaldo

El cloud legacy autorizado por la migración DEV histórica es `vyijnqzq`; la migración PROD debe copiar, no mover ni eliminar, los activos origen.

## Destino obligatorio de la migración de media

```text
riyrzmob
└── solqaryn_prod/
    └── inventoryapp/
        └── productos/
            └── empresas/
                └── 1/
```

La migración debe ser determinista e idempotente, actualizar `ProductoImagenes.Url` y `ProductoImagenes.PublicId` solo después de confirmar cada upload y terminar con cero locators legacy.

## Resultado

`CLOUDINARY_PROD_CREDENTIALS=PASS`  
`CLOUDINARY_PROD_RUNTIME_AUTH=PASS`  
`CLOUDINARY_PROD_PREFIX_ISOLATION=PASS`  
`HISTORICAL_MEDIA_INVENTORY=351`  
`HISTORICAL_MEDIA_MIGRATION=WAITING_FOR_PROD_DATA_RESTORE`
