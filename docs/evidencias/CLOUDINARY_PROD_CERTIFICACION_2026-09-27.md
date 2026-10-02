# Certificación Cloudinary PROD — 2026-09-27

Estado: **PASS / CERRADO — CREDENCIALES, RUNTIME, AISLAMIENTO Y MIGRACIÓN HISTÓRICA**

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

- `PROD - Historical Cloudinary inventario`
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
    └── Solqaryn/
        └── productos/
            └── empresas/
                └── 1/
```

La migración debe ser determinista e idempotente, actualizar `ProductoImagenes.Url` y `ProductoImagenes.PublicId` solo después de confirmar cada upload y terminar con cero locators legacy.

## Resultado

`CLOUDINARY_PROD_CREDENTIALS=PASS`  
`CLOUDINARY_PROD_RUNTIME_AUTH=PASS`  
`CLOUDINARY_PROD_PREFIX_ISOLATION=PASS`  
`HISTORICAL_MEDIA_inventario=351`  
`HISTORICAL_MEDIA_MIGRATION=PASS`  
`HISTORICAL_MEDIA_MIGRATED_ROWS=351`  
`HISTORICAL_MEDIA_REMAINING_LEGACY_ROWS=0`  
`HISTORICAL_MEDIA_SOURCE_DELETED=FALSE`


## Migración histórica ejecutada

Deploy Render de migración:

- deploy: `dep-dasjaem0tbcc73flumrg`
- commit: `09251c84d949e6357269b0fbf008ab0f3b770511`
- resultado: **LIVE**
- fuente legacy: `vyijnqzq`
- destino: `riyrzmob/solqaryn_prod/Solqaryn/productos/empresas/1`
- filas origen: **351**
- filas migradas: **351**
- referencias legacy restantes: **0**
- activos origen eliminados: **no**

Evidencia runtime:

```text
CLOUDINARY_HISTORICAL_PROD_MIGRATION SUCCESS source_rows=351 migrated_rows=351 remaining=0 empresa=1
CloudinaryHistoricalProdMigration status=SUCCESS sourceRows=351 migratedRows=351 remainingLegacyRows=0 sourceDeleted=False
```

Después del éxito, el flag de ejecución se volvió a dejar en `false` y Render quedó nuevamente `LIVE` en el deploy `dep-dasjd459fdbs73dnf3t0`.

## Certificación final read-only

Workflow: `PROD - Final post-migration audit`  
Run: `36329897886`  
Resultado: **SUCCESS**

Validó:

- 351/351 URLs en el cloud PROD;
- 351/351 PublicIds bajo el prefijo PROD;
- 0 referencias al cloud legacy;
- 351/351 assets físicamente alcanzables;
- 0 fallos de assets;
- smoke HTTP final de PROD: **PASS**;
- escrituras de la auditoría: **0**.
