# Certificación final de migración PROD — 2026-09-27

Estado: **PASS / MIGRACIÓN HISTÓRICA COMPLETA**

## Base de datos productiva

Workflow de cutover:

- nombre: `PROD - Final historical cutover retry`
- run: `36328852574`
- commit: `09251c84d949e6357269b0fbf008ab0f3b770511`
- resultado: **SUCCESS**

El cutover:

1. revalidó `solqaryn_prod` antes de la primera escritura;
2. verificó el rollback cifrado previo;
3. verificó y descifró únicamente el respaldo histórico certificado;
4. restauró el histórico;
5. aplicó las migraciones EF actuales;
6. reaplicó el overlay de autenticación bootstrap sin exponer credenciales;
7. verificó el estado exacto final;
8. dejó disponible rollback automático, que no fue necesario;
9. ejecutó smoke runtime final.

Resultado de datos:

```text
PROD_FINAL_HISTORICAL_DB_CUTOVER=PASS
baseTables=137
efMigrations=107
empresas=1
productos=73
usuarios=6
productoImagenes=351
```

Artifact de resultado:

- `prod-final-cutover-result-36328852574`
- artifact id: `10935475329`
- digest: `sha256:96fefd21240d773cb1679699cad82b11b645f6a4a0ef41590b8302ef182afa5f`

## Cloudinary productivo

Migración runtime:

- deploy: `dep-dasjaem0tbcc73flumrg`
- destino: `riyrzmob`
- prefijo: `solqaryn_prod`
- source rows: **351**
- migrated rows: **351**
- remaining legacy rows: **0**
- source deleted: **false**

Después de la migración se deshabilitó el flag one-shot y Render volvió a quedar `LIVE` con deploy `dep-dasjd459fdbs73dnf3t0`.

## Auditoría final independiente

Workflow:

- `PROD - Final post-migration audit`
- run: `36329897886`
- resultado: **SUCCESS**
- escrituras: **0**

Resultados:

```text
PROD_FINAL_TABLES=137
PROD_FINAL_MIGRATIONS=107
PROD_FINAL_EMPRESAS=1
PROD_FINAL_PRODUCTOS=73
PROD_FINAL_USUARIOS=6
PROD_FINAL_PRODUCTO_IMAGENES=351
PROD_FINAL_LEGACY_CLOUDINARY_REFS=0
PROD_FINAL_TARGET_CLOUDINARY_URLS=351
PROD_FINAL_TARGET_CLOUDINARY_PUBLIC_IDS=351
PROD_CLOUDINARY_ASSETS_EXPECTED=351
PROD_CLOUDINARY_ASSETS_REACHABLE=351
PROD_CLOUDINARY_ASSET_FAILURES=0
PROD_FINAL_RUNTIME_SMOKE=PASS
PRODUCTION_WRITES=0
```

La auditoría también confirmó al menos un administrador activo y observó **3** usuarios con rol administrador activo en el conjunto histórico migrado.

## Rollback conservado

Rollback anterior al cutover:

- run: `36298199171`
- artifact: `solqaryn-prod-empty-rollback-36298199171`
- artifact id: `10924897018`
- digest: `sha256:415e31d932437dd73c1491f1516e06da04de7959f1ab654c394bad0462bbda2f`
- contiene dump cifrado y overlay bootstrap cifrado.

Conservar hasta que el propietario decida que ya no es necesario para rollback histórico.

## Resultado

`PROD_HISTORICAL_DATABASE_MIGRATION=PASS`  
`PROD_CLOUDINARY_HISTORICAL_MIGRATION=PASS`  
`PROD_LEGACY_CLOUDINARY_REFERENCES=0`  
`PROD_RUNTIME_SMOKE=PASS`  
`PROD_MIGRATION_STATUS=CLOSED`

## Seguimiento de housekeeping — 2026-09-28

El propietario aceptó PROD y autorizó retirar la infraestructura personal legacy de SOLQARYN/VariStoreHN.

- El rollback pre-cutover del destino PROD `10924897018` ya no es requisito de recuperación histórica y queda autorizado para eliminación controlada o expiración natural.
- El backup histórico independiente de la fuente `10901905430` se conserva durante el retiro de la cuenta personal. Es un artifact corporativo cifrado con restore verificado y no depende de la infraestructura legacy viva.
- La retirada de cuentas/servicios personales no autoriza eliminar proyectos personales ajenos a SOLQARYN/VariStoreHN.
- El cierre definitivo exige postcheck de DEV/PROD después de la eliminación manual.
