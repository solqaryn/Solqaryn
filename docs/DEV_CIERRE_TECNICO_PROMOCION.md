# Cierre técnico DEV y contrato de promoción futura — SOLQARYN

Fecha de certificación técnica DEV: 2026-09-30.

Este documento es evidencia técnica de estado y runbook de promoción. **No es un roadmap, no modifica el Plan Maestro y no autoriza cambios en `main` o PROD.**

## 1. Scope cerrado

La certificación causal de runtime corresponde a:

- repositorio: `solqaryn/Solqaryn`;
- rama: `dev`;
- runtime HEAD certificado: `7452c43c489f467727ab2569f34d43074ce3e06a`;
- Vercel DEV deployment: `dpl_kXey6ucYhZjTjVFSBCX1PBv3St8v` — `READY`;
- Render DEV deployment: `dep-daustt3ncjis73876cu0` — `live`;
- frontend DEV: `https://solqaryn-dev.vercel.app`;
- API DEV: `https://solqaryn-api-dev-fxx8.onrender.com`;
- base DEV: `solqaryn_dev`;
- usuario de aplicación DEV esperado: `solqaryn_dev_user`.

El commit documental que incorpora este archivo es runtime-equivalente al HEAD anterior: no cambia frontend, backend, infraestructura, datos ni secretos.

## 2. Evidencia de QA exact-head

En `7452c43c489f467727ab2569f34d43074ce3e06a` quedaron en `success`:

- `DEV - Compilación y pruebas`;
- `Priority 4 - Interfaces architecture and quality`;
- `DEV - aceptación funcional integral`;
- `DEV - recuperación de migración MySQL parcial`;
- `Storefront Fase 12 - cuenta cliente evolucion`;
- `SOLQARYN Project Scope Lock`;
- `Priority 3 - ERP security and isolated restore`;
- `SOLQARYN - Backup y restauración en DEV`;
- `Fase 2 - Auditoría de configuración y dependencias`.

Readbacks vivos posteriores:

- `GET /api/health` -> HTTP 200;
- `GET /api/health/ready` -> HTTP 200, `database=connected`;
- catálogo público -> HTTP 200 con `Cache-Control: public, max-age=5, must-revalidate` + ETag;
- administración anónima `/api/productos` -> HTTP 401;
- Render abrió conexiones contra `solqaryn_dev`;
- Vercel DEV sirve el mismo HEAD técnico certificado.

Render Free puede producir cold start transitorio. Un 502 durante wake-up no autoriza compra ni keep-alive artificial; la certificación exige recuperación a 200 y servicio sano después del arranque.

## 3. Integridad de productos/datos

Snapshot DEV verificado durante la certificación:

| Producto | Stock |
| --- | ---: |
| Cargador | 26 |
| Funda para samsung | 1 |
| Laptop | 15 |
| UAT Producto 001 | 10 |

Total: 4 productos públicos.

La reconciliación EF de DEV corrigió únicamente deriva de esquema/historial y fue diseñada de forma idempotente/fail-closed. No se borraron ni reescribieron filas de productos como parte del cierre.

## 4. Fronteras de aislamiento certificadas

- Vercel no selecciona API por hostname o alias.
- Cada proyecto aporta `SOLQARYN_ENV`, `API_UPSTREAM`, `PUBLIC_ORIGIN` y `SEO_INDEXING_ENABLED`.
- DEV sólo puede resolver `solqaryn-api-dev-fxx8.onrender.com`; un cruce DEV/PROD falla cerrado.
- Commits de `dev` no deben construir previews dentro del proyecto Vercel PROD; el guard usa `VERCEL_PROJECT_ID`.
- Render Development sólo acepta `solqaryn_dev` + `solqaryn_dev_user`.
- Entorno ambiguo, base incompatible o usuario incompatible abortan el arranque.
- Cache pública y sus keys permanecen tenant-aware; administración, sesión, checkout y errores permanecen fuera de cache público.

## 5. Condiciones obligatorias para una futura promoción dev -> main/PROD

La promoción es una **operación independiente** y no se ejecuta por la existencia de este documento.

Antes de tocar `main` o PROD deben cumplirse todas:

1. autorización nueva, explícita y vigente del propietario;
2. congelar el HEAD de `dev` que se pretende promover y revalidar que no exista otro writer sobre el scope;
3. revisar el diff completo `main...dev` y confirmar P0=0/P1=0;
4. ejecutar gates exact-head aplicables y obtener `success`;
5. tomar readback/snapshot de la configuración productiva existente antes de modificar variables, sin almacenar secretos en Git;
6. confirmar que el proyecto Vercel PROD tiene binding PROD explícito y nunca fallback hacia DEV;
7. confirmar Render PROD con `ASPNETCORE_ENVIRONMENT=Production`, base `solqaryn_prod`, usuario `solqaryn_prod_user` y `Database__ApplyMigrationsOnStartup=false`;
8. cualquier migración de esquema/datos PROD requiere plan explícito, backup/rollback y autorización separada; no se reutiliza DEV como fuente de datos;
9. Cloudinary PROD debe conservar exclusivamente el prefijo `solqaryn_prod`;
10. no cambiar DNS/custom domain dentro de la promoción salvo autorización específica;
11. no reactivar infraestructura legacy ni cuentas personales como fallback;
12. después del merge/despliegue, certificar funcionalmente las cuatro URLs canónicas.

## 6. Las cuatro URLs que deberá certificar la promoción

1. Frontend DEV: `https://solqaryn-dev.vercel.app`
2. API DEV: `https://solqaryn-api-dev-fxx8.onrender.com`
3. Frontend PROD: `https://solqaryn-prod.vercel.app`
4. API PROD: `https://solqaryn-api-prod.onrender.com`

La certificación post-promoción debe incluir como mínimo: liveness/readiness, storefront, autenticación/RBAC, tenant isolation, catálogo/stock, cache/ETag, medios Cloudinary, rutas administrativas y ausencia de cruces DEV/PROD.

## 7. Rollback obligatorio para la promoción futura

### Código/frontend

- no force-push ni reescribir historia;
- revertir el changeset/merge productivo mediante commit normal si el defecto está en código;
- restaurar/promover el deployment PROD anterior `READY` cuando proceda;
- nunca apuntar PROD al proyecto Vercel DEV como mecanismo de rollback.

### Backend Render

- conservar identificado el deployment PROD anterior a la promoción;
- si el nuevo backend falla, volver al deployment productivo anterior;
- nunca usar `solqaryn-api-dev` como fallback de PROD.

### Variables/secretos

- capturar antes del cambio qué claves/valores productivos serán sustituidos mediante la superficie segura del proveedor;
- restaurar el valor anterior si la promoción falla;
- no persistir secretos en este runbook, Git, logs o artifacts públicos.

### Base de datos

- PROD no ejecuta migraciones automáticas;
- no usar migraciones `Down` destructivas como rollback por defecto;
- ante cambio de datos/esquema, recuperar desde backup verificado o aplicar una migración compensatoria explícitamente revisada;
- cualquier restore productivo exige autorización específica.

### DNS

- DNS permanece fuera de esta fase;
- si un cutover futuro es autorizado, debe tener TTL/readback/TLS/CORS/smoke y ruta de reversión independiente.

## 8. Plan Maestro y SQ-350 / SQ-351

En esta FASE 4 **no se modifica el Plan Maestro**.

Se reserva para la futura revisión mayor, con aprobación del propietario:

- incorporar la regla reforzada de aislamiento DEV/PROD;
- revisar/incorporar los refuerzos correspondientes a `SQ-350` y `SQ-351`.

Esta reserva es sólo una nota técnica de handoff. No crea una fila, gate, prioridad, dependencia ni autorización ejecutable fuera del único Plan Maestro vigente.

## 9. Coste

Esta fase no requiere comprar ni activar servicios. Se conserva la infraestructura actual y se acepta el comportamiento de cold start de Render Free en DEV.
