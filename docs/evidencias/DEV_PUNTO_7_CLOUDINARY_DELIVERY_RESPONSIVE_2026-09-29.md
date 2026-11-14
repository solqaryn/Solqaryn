# DEV — Punto 7: Cloudinary delivery responsive

Fecha: 2026-09-29  
Scope: `solqaryn/Solqaryn` / `dev`  
Estado: **IMPLEMENTADO / validación runtime pendiente por límite gratuito externo**

## Cambios implementados

- Helper central: `frontend/src/app/shared/cloudinary-image.util.ts`.
- Sólo reescribe URLs HTTPS de `res.cloudinary.com/.../image/upload/`; cualquier URL ajena a Cloudinary se conserva sin modificación.
- Transformaciones de entrega: `f_auto,q_auto,c_limit,w_*`.
- Anchos responsive permitidos: 320, 480, 640 y 800 px.
- `app-producto-imagen` emite `srcset` y `sizes` y conserva dimensiones explícitas, `loading`, `decoding="async"` y prioridad opt-in.
- Storefront cubierto: home, listado, categoría, detalle, miniaturas, relacionados, carrito y lightbox.
- Imágenes no críticas conservan `loading="lazy"` + `decoding="async"`.
- Cada vista crítica conserva `fetchpriority="high"` sólo en su imagen LCP.
- No se modificaron uploads, carpetas, assets, credenciales ni configuración Cloudinary.

## Contrato backend verificado

El catálogo público ya separaba correctamente listado y detalle:

- `BuscarAsync` y destacados retornan `TiendaProductoResumenDto`.
- El resumen incluye `ImagenPrincipalUrl`, sin galería completa.
- El detalle usa `includeGalleries: true`.
- Contextos no destinados a galería usan `includeGalleries: false`.

No fue necesario cambiar DTOs ni persistencia.

## Guardas añadidas

- `frontend/scripts/validate-cloudinary-responsive.mjs`.
- Integrada en `npm run lint`.
- Comprueba transformaciones, anchos, `srcset`, `sizes`, lazy/async, prioridad LCP y separación listado/detalle del backend.

## Validación externa

El intento de validación local integral desde este entorno no pudo clonar GitHub porque el runtime de ejecución no tiene resolución DNS hacia `github.com`; no se declara PASS local.

El check visible de Vercel para el HEAD de trabajo respondió `build-rate-limit` y ofrece upgrade a Pro. Por decisión del propietario:

- no se compra ningún servicio;
- no se hace upgrade;
- no se altera plan de Vercel;
- no se toca PROD para resolver el límite.

La implementación queda versionada en `dev`. El cierre runtime debe producirse con una ejecución gratuita disponible del pipeline/deployment; esta evidencia no falsifica ese estado.

## Impacto

- Migraciones DB: ninguna.
- Datos: ninguna escritura.
- RBAC/tenancy: sin cambios.
- Secretos: sin cambios.
- `main`: intacta.
- PROD: sin cambios intencionales.
- Servicios pagos: ninguno.
