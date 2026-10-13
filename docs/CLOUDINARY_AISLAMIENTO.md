# Cloudinary — aislamiento entre DEV y PROD

> **ESTADO VIGENTE (2026-10-12): LISTO para las 11 imágenes y descargas DEV.** Los apartados de inventarios, pendientes y preparación que siguen son registros históricos del estado anterior. El dictamen vigente aparece al final, en «Cierre integral Cloudinary DEV».

## Objetivo

Evitar que una base Aiven de DEV creada desde un fork productivo pueda eliminar activos reales de PROD.

El identificador oficial del entorno de DEV es `solqaryn_dev`.

## Comportamiento implementado

Cuando `Cloudinary__EnvironmentPrefix=solqaryn_dev`:

- las nuevas imágenes de productos se almacenan bajo `solqaryn_dev/solqaryn/productos`;
- los nuevos comprobantes se almacenan bajo `solqaryn_dev/solqaryn/compras`;
- las nuevas fotografías de perfil se almacenan bajo `solqaryn_dev/solqaryn/perfiles`;
- cualquier eliminación cuyo `PublicId` no comience con `solqaryn_dev/` queda bloqueada antes de llamar a Cloudinary.

Esto protege los activos productivos aunque la base de Aiven DEV conserve URLs y `PublicId` históricos de PROD.

Cuando el prefijo está vacío, el comportamiento productivo se conserva sin cambiar las carpetas ni los identificadores actuales.

## Recurso autorizado de DEV

La clave de API creada por el propietario y etiquetada `solqaryn_dev` es la única clave autorizada para el servicio Render de DEV. Sus valores deben permanecer exclusivamente como secretos externos y nunca deben copiarse al repositorio, al PR, a capturas públicas o al chat.

Las otras claves visibles en el panel de Cloudinary no se consideran entornos duplicados: pueden pertenecer a funciones internas de la plataforma. No deben eliminarse sin una auditoría de dependencias en el panel.

## Validación automatizada

`CloudinaryEnvironmentIsolationTests` verifica que DEV no pueda eliminar:

- una imagen productiva de producto;
- un comprobante productivo de compra.

El workflow de DEV también verifica que `render.yaml` declare exactamente el prefijo `solqaryn_dev`.

## Validación externa pendiente

En el entorno real de DEV se debe probar:

1. confirmar que Render usa la clave etiquetada `solqaryn_dev`, sin exponer su valor;
2. subir un activo nuevo y confirmar su prefijo `solqaryn_dev/`;
3. eliminar ese activo de DEV;
4. intentar eliminar una referencia heredada de PROD y confirmar que se rechaza;
5. revisar que ningún activo productivo haya cambiado;
6. verificar almacenamiento, transformaciones y ancho de banda.

La opción más segura sigue siendo usar un product environment o una cuenta Cloudinary completamente separada para DEV. Mientras se comparta el mismo product environment, el prefijo y la clave autorizada son controles obligatorios.

## Evidencia de ownership — 2026-09-25

- Cuenta/perfil: `Solqaryn Platform`
- Email: `solqaryn.platform@outlook.com`
- Cloud name: `riyrzmob`
- Product Environment ID: `7ab9e3e6de660a0b70eb4a5bacf331`
- Estado: `Active`
- Product environments observados: 1
- Media Library observada: contenido de ejemplo únicamente

Estado: ownership del Cloudinary nuevo confirmado. Aún falta certificar la API key usada por Render DEV y realizar un upload real que produzca un PublicId bajo `solqaryn_dev/` antes de retirar cualquier recurso Cloudinary legacy.

## Inventario de API keys — 2026-09-25

La evidencia visual del cloud `riyrzmob` muestra una única API key activa llamada `Root`, creada el 2026-09-22. No existe todavía una key dedicada `solqaryn_dev`.

Decisión operativa:

- conservar `Root` sin usarla como credencial de aplicación;
- crear una API key dedicada denominada `solqaryn_dev`;
- usar esa key exclusivamente en Render DEV;
- no revelar ni registrar el API Secret en chat/repositorio/capturas;
- PROD permanece fuera de alcance.

## API keys canónicas — 2026-09-25

Evidencia visual confirma las siguientes keys activas en `riyrzmob`:

- `Root`: administración/recuperación únicamente;
- `solqaryn_dev`: credencial operativa exclusiva de DEV;
- `solqaryn_prod`: credencial reservada para PROD, aún sin conectar.

No almacenar ni revelar API Secret en Git, documentación, capturas o chat.

## Render DEV conectado — 2026-09-25

La configuración visible de `solqaryn-api-dev` contiene:

- `Cloudinary__CloudName=riyrzmob`;
- `Cloudinary__EnvironmentPrefix=solqaryn_dev`;
- `Cloudinary__ApiKey` con valor oculto;
- `Cloudinary__ApiSecret` con valor oculto.

El redeploy manual `dep-darcsc0jo6nc73fffmtg` terminó `live` y el health posterior respondió HTTP 200. Pendiente final: upload funcional desde la aplicación y validación del locator resultante bajo `riyrzmob/.../solqaryn_dev/`.


## Prueba funcional e inventario legacy — 2026-09-25

### Upload funcional

- Render DEV: deploy `dep-darcsc0jo6nc73fffmtg` terminó `live`.
- Health posterior: `/health/ready` HTTP 200.
- Upload real desde SOLQARYN DEV: PASS.
- Cloud destino: `riyrzmob`.
- Prefijo observado en Media Library: `solqaryn_dev/solqaryn/productos/empresas/1/`.
- La API pública de tienda devuelve el asset nuevo desde `res.cloudinary.com/riyrzmob/.../solqaryn_dev/...`.

### Inventario read-only de referencias legacy

GitHub Actions run `36182095589` / artifact `cloudinary-dev-legacy-inventario-36182095589`:

- `ProductoImagenes.Url`: 10 filas;
- `ProductoImagenes.PublicId`: 10 filas;
- `CompraDocumentos.Url`: 1 fila;
- `CompraDocumentos.PublicId`: 1 fila;
- `Usuarios.FotoPerfilUrl`: 2 filas;
- `Usuarios.FotoPerfilPublicId`: 2 filas.

Interpretación operacional: 13 filas lógicas de assets continúan enlazadas al Cloudinary legacy. El escaneo fue solo lectura, sobre `solqaryn_dev`, y no tocó Producción.

Estado del punto: **Cloudinary DEV nuevo funcional; cierre global PENDIENTE de migrar 13 assets legacy y volver a escanear hasta cero referencias.**

## Corrección de descarga de imágenes DEV — 2026-10-12 (integrada)

- Hallazgo real del propietario: `GET /api/productos/7/imagenes/9/descargar` respondió HTTP 400 porque la URL persistida no contiene el directorio canónico de imágenes de productos; el archivo existe bajo el prefijo del ambiente DEV.
- La corrección propuesta en DEV utiliza `StorageTenantContext` obtenido mediante el permiso `Productos.Exportar` y la membresía activa verificada del usuario; nunca confía en una URL o un EmpresaId enviados como autoridad.
- Las nuevas cargas y las eliminaciones conservan el folder canónico exacto y la política fail-closed. Para descargas ya persistidas en DEV, la infraestructura puede aceptar exclusivamente el patrón verificado `{cloud}/image/upload/v{versión}/solqaryn_dev/{origen}/productos/empresas/{tenant}/{archivo JPG|PNG|WebP}`, siempre con cloud/cuenta corporativos, tenant actual y sin rutas ambiguas ni parámetros. Esta compatibilidad no permite copiar, mover, borrar o subir activos en carpetas no canónicas, ni es habilitada en QA/PROD.
- El ZIP no se genera incompleto cuando una imagen no puede recuperarse; los streams se liberan y el frontend distingue errores HTTP sin divulgar detalles internos de infraestructura.
- **Al cerrar la corrección #3569, todavía antes de la posterior normalización física:** no se habían modificado referencias en Aiven ni activos Cloudinary. La reconciliación de archivos a la carpeta canónica permanece separada y exige respaldo, comprobación de existencia/hash, migración controlada de URL/PublicId y verificación del resultado antes de retirar cualquier original.
- Pruebas obligatorias: unitarias de acceso permitido DEV, rechazo de otro tenant/QA/PROD/cuenta/tipo de recurso, bloqueo de borrado; CI del PR y exact-head DEV; smoke autenticado de descarga individual y ZIP sobre el producto afectado. Un build verde no equivale a smoke real ni cierra la deuda de normalización física.
- No hay compras, ni uso de TinyFish, ni cambios en QA/PROD/MySQL 8.4.8, ni cambio de credenciales.

## Certificación operativa y reconciliación posterior — 2026-10-12

- Pull request [#3569](https://github.com/solqaryn/Solqaryn/pull/3569) fusionado en `dev` SHA `71bcfbeea46dcc0bf35fd49123c6ebb63e577410` después de **15/15** workflows del PR en `SUCCESS`.
- Vercel DEV `READY` y Render DEV `live` confirmados ambos en ese SHA.
- El propietario confirmó que la descarga de imágenes de producto funciona en DEV e indicó que la descarga ZIP completa también fue verificada. Se documenta como **aceptación manual del usuario**, no como inspección automatizada del número de entradas del ZIP o hash de contenidos.
- La compatibilidad con carpetas históricas sólo resuelve la lectura. **NO mueve imágenes ni actualiza los `PublicId`/URL persistidos.** Prohibido eliminar originales o normalizar DB por simple sustitución textual de rutas.
- El inventario original detectó 13 filas lógicas (2026-09-25), pero su recuento está desactualizado. La nueva auditoría `.github/workflows/cloudinary-dev-legacy-audit.yml` ejecuta análisis **solo lectura** del cloud/carpeta canónica y expone únicamente recuentos por tipo; exige MySQL Aiven 8.4.8, usuario y base de DEV, sin mostrar URLs ni IDs, sin tocar QA/PROD.
- Para cerrar normalización física futura: evidenciar recuento y archivos disponibles, backup cifrado real de DEV, copia/rehidratación en carpeta canónica desde el origen autorizado, verificar integridad binaria, actualizar URL y PublicId en una transacción/reconciliación idempotente, readback y smoke individual/ZIP; no borrar originales sin otro permiso específico.
- Coste adicional autorizado: **USD 0**; no TinyFish ni compras. El dominio `solqaryn.com`, SMTP runtime real y VAEP conservan su estado deliberadamente aplazado/pausado, sin reactivación por este cambioset.

## Inventario pre-normalización de locators — 2026-10-12 (histórico)

Fuente: workflow DEV [38298651685](https://github.com/solqaryn/Solqaryn/actions/runs/38298651685), artefacto `cloudinary-dev-locators-38298651685`. Commit auditable `42e60d9b2f3e9408de47ecdbca8ec49984f112f5`; base `solqaryn_dev`; MySQL Aiven **8.4.8**; cloud `riyrzmob`; lectura exclusivamente SQL `SELECT`; cero escrituras, cero exposición de URL/PublicId y cero accesos a QA/PROD.

| Superficie | Filas | Locators canónicos | DEV no canónicos | Externos/cruzados | Sin referencia |
| --- | ---: | ---: | ---: | ---: | ---: |
| ProductoImagenes | 11 | 0 | **11** | 0 | 0 |
| CompraDocumentos | 1 | 0 | 0 | 0 | 1 |
| Usuarios (fotografía) | 5 | 2 | 0 | 0 | 3 |
| **Total** | **17** | **2** | **11** | **0** | **4** |

Dictamen: `CLOUDINARY_DEV_LOCATOR_AUDIT=PASS_READ_ONLY`; `CLOUDINARY_DEV_REMAINING_NONCANONICAL_OR_INVALID=11`; `normalization_certified=false`.

- **Descarga funcional:** individual y ZIP confirmadas por el propietario en DEV, tras PR #3569 y despliegues Vercel/Render `71bcfbee`.
- **Normalización física:** pendiente real, 11 imágenes de productos. No se confunde con que la descarga funcione ni se convierte el inventario histórico del 25 de septiembre en dato actual.
- Antes de modificar referencias, la operación debe demostrar ownership por empresa, respaldos cifrados con restore verificado, bytes y tipo de imagen íntegros, copia de cada asset con identificador canónico, UPDATE idempotente/atómico de URL y PublicId y readback de las 11 filas. Sin borrado automático ni sustituciones textuales de URLs.
- Render contiene credenciales de Cloudinary que no deben exportarse a GitHub, documentación o el chat. **No existe en esta ejecución una vía autorizada para ejecutar la copia firmada** sin exponer credenciales o abrir una operación insegura; por ello no se declara que las 11 imágenes fueron migradas. No se usará TinyFish ni se comprarán créditos.


## Preparación inicial de la normalización DEV — 2026-10-12 (histórico anterior al cierre)

- Servicio `DevCloudinaryLegacyNormalizationHostedService` propuesto en DEV. Solo puede registrarse en ASP.NET Development real sobre Render, con flag explícito `Cloudinary:NormalizeDevLegacyImagesOnStartup`. Este flag permanece inicialmente **false** en `appsettings.Development.json`; el commit preparatorio **no copia ni altera** activos o MySQL.
- Gate interno: exacto host MySQL corporativo Aiven `:14402`, base/usuario DEV, TLS Required, cloud `riyrzmob`, prefijo `solqaryn_dev`, exactamente un tenant y 11 imágenes registradas. Si cualquiera falla, STOP; no hay fallback QA/PROD.
- Cada URL y PublicId se valida contra el cloud correcto, environment DEV y el ID de empresa; se rechaza traversal, query, orígenes/tenants externos y MIME/firma no válidos. Los 11 originales se comprueban por lectura antes de copiar.
- La copia autenticada se realiza **dentro del runtime Render DEV** con credenciales ya custodiadas allí; no se exporta ninguna clave a GitHub ni al chat. Destino canónico determinista; lectura del destino y comparación binaria SHA-256 obligatoria **antes** del UPDATE condicional de la fila exacta.
- Las modificaciones de fila se realizan con compare-and-swap idempotente, bajo control de concurrencia. Si una validación falla, no se alteran las filas siguientes. **Los originales nunca se borran**.
- Precondición obligatoria **antes de activar el flag**: `.github/workflows/cloudinary-dev-backup-restore.yml` aprobado desde rama `dev` con backup Aiven DEV cifrado AES-256, restauración real en MySQL efímero y comparación de contadores/huella de locators. El workflow no lee secretos en PRs de ramas auxiliares. El backup/restore aún no sustituye la verificación en producción DEV del trabajo de copia.
- Solo tras backup y CI exact-head aprobados: activación separada y revisada del flag en DEV, comprobación logs `CLOUDINARY_DEV_NORMALIZATION=PASS total=11 remaining=0`, auditoría read-only DEV con `CLOUDINARY_DEV_REMAINING_NONCANONICAL_OR_INVALID=0`, readback y descarga individual/ZIP. Inmediatamente después, desactivar de nuevo el flag por otro commit DEV.
- Si el proveedor transforma bytes o alguna referencia no coincide, se conserva la fuente; no se declara éxito. Recuperación selectiva desde copia verificada del respaldo, sin restauración masiva de datos sin autorización específica.
- Restricciones permanentes: no se usa TinyFish, no se compran créditos/planes, no se modifican QA/PROD, Cloudinary PROD ni el dominio `solqaryn.com`.

**Activación y contrato Render 28/28:** No agregar `Cloudinary__NormalizeDevLegacyImagesOnStartup` a variables Render: `RenderEnvironmentContractGuard` rechaza cualquier clave `Cloudinary__*` adicional. El único mecanismo autorizado es cambiar temporalmente `Cloudinary.NormalizeDevLegacyImagesOnStartup` de `false` a `true` mediante un commit revisado **exclusivamente en `appsettings.Development.json` de `dev` después del backup/restore PASS**. CI exact-head + autoDeploy `checksPass` ejecutan la operación con las credenciales DEV ya existentes. Tras readback y auditoría 0 pendientes, revertir el flag a `false` con otro commit DEV y recertificar exact-head antes de promover a QA. No se añade ningún secreto, variable o plan Render.

**Rollback automático ante fallo de lote:** el normalizador conserva en memoria las referencias originales y ejecuta compensación inversa por fila con `UPDATE ... WHERE Id AND Url AND PublicId` (compare-and-swap). Nunca sobrescribe una modificación concurrente. Si el proceso cae antes de terminar la compensación, el backup Aiven DEV cifrado, restaurable y validado se usa para recuperación selectiva; los originales Cloudinary permanecen intactos. El estado se verifica con una nueva auditoría read-only y no se declara `PASS` sin 0 pendientes.

## Cierre integral Cloudinary DEV — 2026-10-12 (`LISTO`)

**Dictamen vigente:** las 11 imágenes históricas de productos de DEV están normalizadas. No quedan referencias de producto no canónicas, el proceso transitorio está desactivado y el propietario confirmó que las descargas funcionan después de la migración.

- **Respaldo/restore anterior a las escrituras:** [GitHub Actions 38319634960](https://github.com/solqaryn/Solqaryn/actions/runs/38319634960), PASS; backup cifrado de Aiven DEV y restore verificado en MySQL descartable, sin escrituras al origen.
- **Ejecución física:** [PR #3584](https://github.com/solqaryn/Solqaryn/pull/3584), commit `8406417f3862ce28a5d7303d4c3ac8317645db70`, deploy Render DEV `dep-db6msu2d0e5s73attck0`: `CLOUDINARY_DEV_NORMALIZATION=PASS total=11 newly_normalized=11 remaining=0 originals_deleted=0`. La implementación comprueba firma de imagen, compara SHA-256 origen/destino y actualiza URL/PublicId por compare-and-swap; ninguna imagen original se borró.
- **Readback independiente:** [GitHub Actions 38323224517](https://github.com/solqaryn/Solqaryn/actions/runs/38323224517), PASS, solo lectura MySQL DEV: `CLOUDINARY_DEV_LOCATOR_AUDIT=PASS_READ_ONLY`, `CLOUDINARY_DEV_REMAINING_NONCANONICAL_OR_INVALID=0`, `CLOUDINARY_DEV_PRODUCTOS_ROWCOUNT=11`.
- **Apagado seguro:** [PR #3585](https://github.com/solqaryn/Solqaryn/pull/3585), HEAD de `dev` `f6f79b9db313f258b0d57f55ca1d174ac82dc79b`, `Cloudinary:NormalizeDevLegacyImagesOnStartup=false`. Deploy Render DEV `dep-db6n26gu01pc73feos4g` LIVE; `/health` HTTP 200; 39/39 checks del HEAD aprobados.
- **Smoke funcional posterior a la migración:** el propietario declara el 2026-10-12 haber probado en SOLQARYN DEV la **descarga individual autenticada** y la **descarga ZIP completa autenticada**, ambas correctas. Se registra como **aceptación manual del propietario**, no como prueba de navegador/ZIP ejecutada por CI, ni conteo/hashing automatizado del contenido del ZIP.
- **Separación de alcances:** únicamente Cloudinary/MySQL DEV. No se modificaron QA, PROD, dominio `solqaryn.com`, secretos, planes ni automatizaciones con este cierre. La promoción **DEV → QA → PROD queda APLAZADA** para después, con PR #3583 y #3574 en borrador.

**Resultado:** `CLOUDINARY_DEV_11_IMAGES=LISTO`, `DEV_INDIVIDUAL_ZIP_POST_MIGRATION=MANUAL_ACCEPTANCE_PASS`, `NORMALIZATION_FLAG=false`. Los inventarios antiguos de 11 pendientes y las secciones de preparación de este archivo son evidencia histórica previa, no el estado vigente.
