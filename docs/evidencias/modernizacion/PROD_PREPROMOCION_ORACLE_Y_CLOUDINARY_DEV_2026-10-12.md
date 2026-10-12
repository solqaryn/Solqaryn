# SOLQARYN — Prepromoción PROD Oracle EF10 y Cloudinary DEV

Fecha: 2026-10-12. Identidad: `solqaryn/Solqaryn`, rama de trabajo `dev`. Documento de evidencia; no autoriza eliminar activos ni aplicar una migración sin sus gates.

## Autorización y controles preservados

El propietario autorizó expresamente en la conversación del 12 de octubre de 2026 la normalización de las 11 imágenes DEV y la promoción controlada `qa -> main/PROD`. Continúan deliberadamente aplazados: cutover `solqaryn.com`, SMTP OAuth2 end-to-end y Render Always-On. Las diez automatizaciones VAEP permanecen pausadas y no deben crearse/activarse/borrarse.

- QA fuente certificado: `f935cc0c93add0ce321245ee8fa3c6a5a874f2f2`. GitHub Actions QA Oracle, build y smoke exact-head en success; Render y Vercel QA sirviendo SHA correcto, alias público reconciliado.
- PROD base antes de promover: `1d2cefb55d3eff6d538e2143f1ca034f35fbd709`. QA y PROD divergen: preservar siete commits exclusivos de `main`, promover solo con merge normal y sin force-push.
- Pull request de promoción: [#3574](https://github.com/solqaryn/Solqaryn/pull/3574), creado en borrador como STOP de seguridad, no como deploy productivo.
- Run [37038904412 (attempt 2)](https://github.com/solqaryn/Solqaryn/actions/runs/37038904412): Aiven PROD real `solqaryn_prod`, 137 tablas incl. historial, 107 entradas EF, empresas=1, productos=73, usuarios=6, lectura exclusivamente SELECT, escrituras=0. No sustituye fingerprint físico ni respaldo restaurado.
- QA posee la migración Oracle `20261006111818_OracleBaseline` con historia de 108 filas. La PROD original mantiene 107; antes de desplegar EF10/Oracle deben aprobarse el backup cifrado con restore efímero, fingerprint exacto y adopción **metadata-only** revisada con rollback probado.
- Gates nuevos propuestos en `dev`: `.github/workflows/prod-oracle-pr-preflight.yml` y `prod-oracle-pr-backup-restore.yml` únicamente para PR corporativo exact-head `qa` → `main`. El último conserva backup Aiven PROD cifrado, lo restaura y prueba adopción Oracle en MySQL efímero; solo después de que eleven `SUCCESS` los once checks exigidos y coincidan los hashes de tablas/FKs/índices en Aiven, admite un único `INSERT` de marcador Oracle `20261006111818_OracleBaseline` en `__EFMigrationsHistory` (107→108), con readback e idempotencia. No ejecuta `CREATE/ALTER/DROP` ni cambia filas de negocio. **Operación aún no ejecutada; si falla un gate, cero escrituras.**
- Deuda de CI descubierta en #3574: `erp-n0-5-ci.yml` y `fase2c1-variante-tecnica.yml` intentaban ejecutar migraciones Pomelo retiradas. Se reemplaza exclusivamente su preparación efímera por `Solqaryn.DatabaseBootstrap` Oracle, conservando pruebas de unidad, integridad, FKs, variantes, control negativo y Playwright. **Nunca se omiten checks rojos ni se certifica un PR sin checks causales en success**.

## Cloudinary DEV

- Auditoría DEV [38298651685](https://github.com/solqaryn/Solqaryn/actions/runs/38298651685): 11 imágenes históricas de producto en carpetas DEV no canónicas; sin cruces QA/PROD detectados. Descarga individual y ZIP aceptadas funcionalmente por propietario, sin confundirlo con normalización física.
- La normalización exige backup/restore real de DEV, revalidar propiedad por empresa y contenido JPG/PNG/WebP, copiar cada original al prefijo `solqaryn_dev/solqaryn/productos/empresas/{tenant}/` desde contexto Cloudinary autorizado, comparar bytes/hash, actualizar URL y PublicId de la fila exacta de forma idempotente/atómica, readback y prueba descarga individual y ZIP. Originales **no se eliminan** durante este proceso.
- La cuenta API/secret de Cloudinary está custodiada en Render DEV, no en GitHub Actions. No utilizar secretos, credenciales de otros entornos ni renombrar rutas de texto sin transferir activos. Ausencia de canal firmado segura implica **STOP para escrituras**, no PASS.

## Condiciones para declarar LISTO

1. PR de preflight en `dev` y posterior promoción a QA, con CI exact-head en verde.
2. Preflight PROD con conteos, fingerprint de tablas/FKs/índices y CHECK/triggers idénticos a QA; backup PROD cifrado + restore y dry-run de adopción exitosos.
3. Adopción del marcador Oracle metadata-only en Aiven PROD con nuevo readback, sin alteraciones de esquema físico ni datos; autorización productiva ya concedida únicamente dentro de este alcance condicionado.
4. Pull request QA→main sin conflictos ni gates rojos, merge normal preservando historia propia PROD; Render/Vercel `READY/LIVE` exact-head y smoke productivo real.
5. Cloudinary DEV: 11/11 activos copiados, verificados y actualizados; 0 locators no canónicos tras auditoría y descargas/ZIP verificadas. Ninguna eliminación de originales.

Hasta superar las condiciones relevantes, registrar estados parciales; no declarar modernización o Cloudinary como certificación 100%.

## Corrección del límite de confianza PROD — ejecución exclusiva en main

GitHub documenta que los entornos con reglas de ramas seleccionadas comparan el `GITHUB_REF` real: un PR usa `refs/pull/<n>/merge`, no `main`. El Environment PROD canónico admite exclusivamente `main`. **Ningún workflow que tenga acceso a secretos PROD puede ejecutarse en un `pull_request`.**

Por ello, los workflows `prod-oracle-pr-preflight.yml` y `prod-oracle-pr-backup-restore.yml` (nombres históricos de archivo) se han corregido para `push` / `workflow_dispatch` **solo en `main`**. Su código deberá promoverse por una excepción de bootstrap de infraestructura previamente revisada, con equivalencia exacta de bytes `dev → qa → main`, sin introducir antes la aplicación Oracle EF10 en PROD. Esta excepción conserva los siete commits exclusivos de `main` y está limitada a workflows de certificación, sin cambios de app, variables, DNS o secretos.

En `main`, primero se certifica el fingerprint PROD de solo lectura y un respaldo cifrado real con restore MySQL descartable, verificando recuentos y ensayo SQL metadata-only. El chequeo de QA contra GitHub exige el PR real `qa → main` abierto, no draft, exact-head inalterado y los checks obligatorios verdes; también comprueba mediante lectura del archivo en QA que OracleBaseline tiene seis SELECT de solo lectura antes de permitir la inserción de un marcador. El write-scope PROD está limitado al único INSERT idempotente en `__EFMigrationsHistory` con validación post-fingerprint; ningún SQL empresarial se modifica. **No se ha ejecutado este proceso en PROD todavía**.

El primer `push` de bootstrap main puede detenerse sin mutar datos si el PR QA→main sigue draft. Tras certificar QA, repetir esa certificación de `main` mediante el mismo workflow aprobado es obligatorio antes del merge modernizado. Un workflow exitoso en QA no sustituye el certificado de PROD.

Referencia oficial: https://docs.github.com/en/actions/reference/workflows-and-actions/deployments-and-environments
