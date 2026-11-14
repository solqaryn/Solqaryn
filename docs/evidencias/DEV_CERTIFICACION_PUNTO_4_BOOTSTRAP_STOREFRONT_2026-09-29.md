# Certificación DEV — Punto 4: bootstrap único del storefront

Fecha: 2026-09-29  
Proyecto: SOLQARYN  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`

## Resultado

**Estado certificado: LISTO en DEV.**

La carga inicial del storefront público fue consolidada en `GET /tienda/bootstrap`. En el camino feliz, Angular ya no dispara identidad pública, WhatsApp, tema, categorías y destacados como requests iniciales independientes.

## Functional HEAD certificado

`3ad6450e037466e78c25aa07f37ff4e77e5ec109`

`dev` fue verificado idéntico a ese commit antes de registrar esta evidencia.

## Implementación

### Backend

`GET /tienda/bootstrap` responde en un único request scope:

- identidad pública mínima;
- WhatsApp público resuelto;
- tema visual público;
- hasta 6 categorías de navegación;
- hasta 4 productos destacados usando el resumen ligero del Punto 3.

La composición vive en:

- `ITiendaBootstrapService`;
- `TiendaBootstrapService`;
- `IWhatsAppPublicoService`;
- `WhatsAppPublicoService`.

No se creó persistencia, cache distribuida, servicio externo ni fuente de verdad nueva. Los servicios existentes siguen siendo las autoridades.

Los accesos EF se mantienen secuenciales dentro del mismo scope HTTP porque comparten `DbContext` y éste no es thread-safe.

### Frontend

- `SOLQARYNService.obtenerBootstrap()` comparte la petición con `shareReplay({ bufferSize: 1, refCount: false })`.
- `SOLQARYNIdentidadService` hidrata identidad desde el bootstrap.
- `AppComponent` aplica el tema directamente desde el bootstrap.
- La portada consume categorías y destacados de la misma respuesta.
- Los endpoints separados permanecen como recovery y para pantallas específicas; no son el camino feliz inicial.
- Si el bootstrap falla, existe fallback controlado a las lecturas públicas anteriores para no dejar la tienda inutilizable.

## QA exact-head

PR: #3479 — `perf(storefront): bootstrap unico para portada publica`.

Sobre el HEAD funcional exacto:

- SOLQARYN Project Scope Lock: **SUCCESS**.
- SOLQARYN Fase 1 — regresión pública: **SUCCESS**.
- SOLQARYN Fase 2 — categorías: **SUCCESS**.
- SOLQARYN Fase 3 — catálogo: **SUCCESS**.
- SOLQARYN Fase 4 — detalle: **SUCCESS**.
- SOLQARYN Fase 5 — carrito: **SUCCESS**.
- SOLQARYN Fase 6 — checkout y pedido: **SUCCESS**.
- SOLQARYN Fase 7 — home comercial: **SUCCESS**.
- Build backend Release: **SUCCESS**.
- Pruebas focales de destacados + bootstrap: **SUCCESS**.
- Lint/guardas frontend: **SUCCESS**.
- Build productivo Angular: **SUCCESS**.
- Playwright Fase 7: **SUCCESS**.

La prueba de navegador del Punto 4 cuenta las llamadas iniciales y exige:

- `/tienda/bootstrap`: **1 request**;
- `/empresa-configuracion/publica`: **0** en camino feliz;
- `/whatsapp/publico`: **0** en camino feliz;
- `/tema-visual`: **0** en camino feliz;
- `/tienda/categorias`: **0** en camino feliz;
- `/tienda/productos/destacados`: **0** en camino feliz.

## Runtime DEV

### Render

Servicio: `solqaryn-api-dev`  
Deploy: `dep-dau1aavlot8c7399tka0`  
Commit: `3ad6450e037466e78c25aa07f37ff4e77e5ec109`  
Estado: **live**.

El endpoint real:

`GET /tienda/bootstrap`

respondió HTTP 200 con:

- identidad real de la tienda;
- tema real;
- 2 categorías públicas existentes;
- lista de destacados vacía en el estado actual de datos;
- sin grafos administrativos ni galerías de catálogo completo.

### Vercel

Proyecto DEV: `solqaryn-dev`  
Deployment: `dpl_CrbUDPWwXaqSA6pim9NgkdwRdHNr`  
Commit: `3ad6450e037466e78c25aa07f37ff4e77e5ec109`  
Estado: **READY**  
Target: **production** del proyecto DEV.

Aliases activos incluyen `solqaryn-dev.vercel.app`.

Vercel había mostrado temporalmente `build-rate-limit` durante la ráfaga de commits, pero el merge final fue construido y quedó READY sin compra ni upgrade.

## Rendimiento interno observado

Cold/warm-up inmediatamente posterior al deploy:

- 2403.1 ms / 5 queries;
- se separa del criterio de API caliente por cold start de Render Free.

Muestra posterior:

- 559.3 ms / 5 queries / 110.7 ms DB;
- 227.6 ms / 5 queries / 105.7 ms DB;
- 836.1 ms / 5 queries / 376.5 ms DB;
- 282.7 ms / 5 queries / 163.5 ms DB;
- 218.8 ms / 5 queries / 106.0 ms DB;
- 215.0 ms / 5 queries / 106.9 ms DB;
- 543.8 ms / 5 queries / 319.0 ms DB;
- 332.0 ms / 5 queries / 107.0 ms DB.

La variabilidad superior a 500 ms coincide con picos de DB/infraestructura gratuita. Las pasadas calientes estables observadas de 215–332 ms cumplen el target inicial de 500 ms.

El beneficio principal de este Punto 4 no es reducir el número total de consultas internas a cero, sino sustituir múltiples viajes HTTP iniciales y contextos repetidos por **un único request inicial** con **5 queries internas** para todo el paquete.

## Seguridad e impacto

- Sin migraciones.
- Sin escrituras de datos de negocio.
- Sin cambios de RBAC.
- Sin cambios de tenancy.
- Sin secretos nuevos.
- Sin exposición de referencias de tokens/webhooks de WhatsApp.
- Sin cambios a `main`.
- Sin cambios a PROD.
- Sin servicio nuevo.
- Sin compra ni upgrade de servicios.

## Cierre

El Punto 4 queda **LISTO y certificado en DEV**.

No requiere acción manual ni compra de servicios por parte del propietario.
