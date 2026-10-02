## 2026-10-01 — Paridad fail-closed DEV/QA/PROD en Render, GitHub y Aiven

- `RenderEnvironmentContractGuard` valida en startup exactamente 28 claves Render administradas, sin exponer valores, y mantiene separadas las identidades/secretos propios de DEV, QA y PROD.
- `EnvironmentDatabaseGuard` amplía la frontera de datos a los tres entornos y exige endpoint Aiven corporativo, puerto `14402` y `SslMode=Required` además de la pareja base/usuario.
- Se añade `Environment infrastructure parity` para certificar por GitHub Environment la estructura 4 variables + 3 secretos, binding MySQL, mínimo privilegio, denegación cross-env, token Aiven y passphrase de backup.
- QA demostró el contrato Render exacto en runtime; DEV detectó y bloqueó ocho variables legacy antes de reemplazar la instancia sana, dejando la limpieza como deuda de control-plane explícita.
- La topología Vercel queda definida como tres proyectos corporativos con binding por `VERCEL_PROJECT_ID`; QA requiere que su Production Branch canónico sea `qa`.
- No se introducen datos, migraciones ni servicios pagos.

## 2026-10-01 — Vercel runtime binding por identidad de proyecto

- `frontend/server/environment-binding.js` deja de depender de variables manuales como requisito primario y usa `VERCEL_PROJECT_ID` como identidad inmutable del deployment.
- Sólo los project IDs corporativos DEV y PROD están allowlisted; cada uno resuelve un único API upstream, public origin y política SEO.
- Variables explícitas, si existen, se validan como overrides de coherencia; cualquier contradicción, project ID desconocido o intento de cruce falla cerrado.
- Se conserva la prohibición de seleccionar backend por hostname/alias y no se introduce fallback entre entornos.
- Sin datos, migraciones, secretos ni servicios pagos.

## 2026-09-30 — Guard fail-closed Render → MySQL por entorno

- Se incorpora `EnvironmentDatabaseGuard` en el arranque de API cuando `RENDER=true`.
- `Development` sólo admite `solqaryn_dev` con `solqaryn_dev_user`; `Production` sólo `solqaryn_prod` con `solqaryn_prod_user`.
- Base cruzada, usuario cruzado, cadena incompleta o entorno ambiguo/no canónico abortan startup antes de registrar/usar `AppDbContext`.
- El guard se valida con pruebas negativas y deja preparado el mismo código para una futura promoción autorizada a PROD sin modificar infraestructura productiva ahora.
- Sin migraciones, escrituras de datos, cambios en `main`, secretos ni servicios pagos.

# ARCHITECTURE_CHANGELOG — Solqaryn

## 2026-09-30 — Binding Vercel/API fail-closed por proyecto

- Se elimina la selección de backend mediante hostname y el fallback genérico hacia DEV.
- `/api/*` usa un proxy server-side local y `environment-binding.js` exige `SOLQARYN_ENV` + `API_UPSTREAM` coherentes.
- DEV sólo admite Render DEV y PROD sólo Render PROD; alias/custom domain no seleccionan entorno.
- SEO/canonical usa `PUBLIC_ORIGIN` + `SEO_INDEXING_ENABLED`; un entorno desconocido o cruzado falla cerrado.
- Sin cambios en main/PROD, datos, migraciones ni servicios pagos.

## 2026-09-29 — Cache HTTP + ETag + compresión del storefront

- ASP.NET Core habilita Brotli/Gzip para JSON/text sobre HTTPS.
- Toda respuesta backend parte de `private, no-store, max-age=0`; sólo GET públicos allowlisted usan `PublicHttpCacheAttribute`.
- Identidad/tema/WhatsApp/categorías: `max-age=120, s-maxage=300, stale-while-revalidate=600`.
- Bootstrap: `max-age=15, s-maxage=30, stale-while-revalidate=60` por incluir destacados.
- Productos/listados/detalle: `max-age=5, s-maxage=15, must-revalidate`.
- Respuestas públicas cacheables emiten ETag débil SHA-256 y resuelven `If-None-Match` con 304; `Vary: Accept-Encoding` preserva equivalencia con compresión.
- Contexto de carrito, checkout, sesión, administración, documentos y errores permanecen fuera de cache público.
- Vercel marca bundles Angular hashados como immutable por un año y habilita caching de rewrites API sólo para respetar el `Cache-Control` upstream.
- Sin persistencia nueva, migraciones, secretos ni servicios pagos.

## 2026-09-29 — Cache público tenant-aware en dos niveles

- Angular conserva el observable de identidad en vuelo y comparte la lista de categorías entre rutas mediante replay, evitando solicitudes concurrentes duplicadas.
- El backend incorpora `IPublicStoreCache` con `PublicStoreMemoryCache` sobre `IMemoryCache`; las claves incluyen tenant, segmento, generación y hash de parámetros.
- TTL: identidad/tema/categorías 5 min; destacados 30 s; listados 15 s. Detalle, contexto de carrito y checkout permanecen fuera de cache.
- La cache usa lock por key contra stampede y generaciones para invalidación.
- `AppDbContext.SaveChangesAsync` invalida después de escrituras relevantes de producto/variante/imágenes/stock, categorías, identidad/WhatsApp, tema, marca/modelo y descuentos.
- La partición tenant usa `empresa:{id}` cuando la identidad pública se resuelve inequívocamente; ante ambigüedad usa `public-config:{id}` fail-safe.
- Topología vigente: cache in-process por instancia. Un escalado horizontal futuro exige implementación distribuida e invalidación compartida antes de asumir coherencia entre instancias.
- Sin migraciones, persistencia nueva, secretos, PROD, `main` ni servicio pagado.

## 2026-09-29 — Bootstrap único del storefront público

- Se incorpora `GET /tienda/bootstrap` como carga inicial canónica de identidad pública mínima, WhatsApp público resuelto, tema visual, hasta 6 categorías de navegación y hasta 4 destacados ligeros.
- La composición vive en `ITiendaBootstrapService`/`TiendaBootstrapService` y reutiliza autoridades existentes; no crea persistencia ni fuente de verdad paralela.
- El bootstrap resuelve WhatsApp mediante `IWhatsAppPublicoService`/`WhatsAppPublicoService`, preservando las reglas públicas vigentes y sin exponer referencias de secretos.
- Angular comparte el bootstrap entre consumidores concurrentes mediante `shareReplay`; identidad, tema y portada ya no generan el stampede de requests en el camino feliz.
- Los endpoints públicos previos permanecen disponibles para recovery y pantallas específicas.
- Sin migraciones, PROD, `main` ni servicio externo/pagado.

## 2026-09-29 — Separación resumen público vs detalle rico

- `GET /tienda/productos` y `/tienda/productos/destacados` pasan a `TiendaProductoResumenDto` + `ProductoCatalogoResumenReadModel`.
- El resumen conserva identidad, categoría, precio/oferta, disponibilidad, imagen principal y variantes mínimas; elimina galerías de variantes, color/talla y descripción completa del payload de listado.
- `GET /tienda/productos/{slug}` conserva el DTO rico y es la única lectura general que carga la galería completa del producto.
- `POST /tienda/productos/contexto` conserva el contrato rico acotado a IDs persistidos para rehidratación de carrito/cuenta.
- No hay migraciones, nueva persistencia, nueva fuente de verdad ni servicio externo/pagado.

## 2026-09-29 — Read path público ligero para catálogo/storefront

- Se separó la consulta pública de productos del repositorio administrativo con includes completos.
- Nueva vía: `ICatalogoPublicoService` + `IProductoCatalogoPublicoRepository` + proyecciones específicas.
- `GET /tienda/productos` pagina y filtra server-side; detalle carga galerías sólo para un producto.
- `POST /tienda/productos/contexto` rehidrata únicamente los IDs persistidos de carrito/cuenta.
- Inventario/promociones conservan sus autoridades existentes; no hay nueva persistencia ni migración.
- El frontend deja de usar una operación de “descargar catálogo completo” como dependencia transversal.


## 2026-09-28 — Baseline first-party de rendimiento DEV

- Instrumentación transversal DEV para separar tiempo HTTP de cantidad/tiempo MySQL sin registrar SQL, parámetros ni secretos.
- Angular mide TTFB, requests/bytes por pantalla y LCP/INP/CLS únicamente en DEV/local.
- Baseline reproducible de bundles raw/gzip/Brotli sin SaaS de observabilidad ni servicio pagado.
- PROD permanece con el baseline desactivado por defecto.
- Rutas: `backend/src/API/Observability`, `backend/src/API/Middleware/RequestObservabilityMiddleware.cs`, `frontend/src/app/core/performance`, `frontend/scripts/performance-bundle-baseline.mjs`, `docs/PERFORMANCE_BASELINE_DEV.md`.


## 2026-09-24 — Topología Aiven canónica y environments SOLQARYN

- GitHub queda normalizado a dos environments canónicos: `Desarrollo` y `Produccion`.
- Aiven usa el proyecto `solqaryn` y un único servicio MySQL Free `solqaryn-mysql`.
- Desarrollo usa `solqaryn_dev` + `solqaryn_dev_user`; Produccion usa `solqaryn_prod` + `solqaryn_prod_user`.
- `avnadmin` queda reservado para administración y los usuarios de aplicación fueron restringidos a su base respectiva.
- La separación entre DEV y PROD es lógica dentro del mismo servicio físico; host/puerto/recursos se comparten, mientras base, usuario, secretos y GitHub Environment permanecen aislados.
- La prueba Aiven del Environment Desarrollo y el backup cifrado + restore same-artifact en MySQL descartable quedaron validados en GitHub Actions sin exponer secretos.


Registro conciso de cambios que obligan a actualizar `PROJECT_INDEX.md`, `PROJECT_CONTEXT.md` o `ARCHITECTURE.md`. No reconstruye historial anterior.

## Convención

Cada entrada debe indicar fecha, cambio observable, documentos/rutas afectados y verificación. Solo se agrega cuando cambian arquitectura, módulos, integraciones, rutas/API, datos o comandos documentados.

## 2026-08-24 — Guía operativa por dominio

- Cambio: `PROJECT_INDEX.md` amplió el mapa por capas a una matriz navegable por dominio y flujos transversales.
- Cobertura: frontend, API, Application, Domain, Infrastructure, DB/integraciones y migraciones ancla de los dominios principales.
- Alcance: se definieron límites explícitos para evitar reinspecciones globales ante cambios locales.
- Verificación: rutas y archivos citados contrastados con el checkout; guard documental y diff validados; sin cambios de código o configuración.

## 2026-08-24 — Contexto histórico ChatGPT/VAEP

- Cambio: se incorporó `docs/CONTEXTO_CHATGPT_VAEP.md` y se enlazó desde `PROJECT_INDEX.md`.
- Alcance: automatización VAEP, validación causal, cadena compras-recepciones-reservas-facturación, no duplicación y consulta selectiva.
- Límite: referencia histórica/operativa de Solqaryn; las fuentes canónicas y el HEAD actual prevalecen.
- Verificación: enlaces y guard documental local comprobados; sin cambios en código de producción.

## 2026-08-24 — Inicialización del mapa técnico persistente

- Cambio: se consolidó `PROJECT_INDEX.md` como mapa operativo y se agregó un índice de decisión para cambios frecuentes.
- Cobertura: backend .NET por capas, frontend Angular por features, puntos de entrada, API, persistencia, configuración, dependencias, comandos y pruebas.
- Evidencia: inspección estática selectiva de manifiestos, solución/proyectos, `Program.cs`, rutas Angular, controladores, `AppDbContext`, migraciones y directorios de pruebas.
- Verificación: rutas, archivos, scripts y ejecutables de comandos comprobados localmente; no se ejecutó la aplicación ni se modificó código de producción.


## 2026-09-30 — Identidad técnica canónica y storefront tenant-neutral

- Se normalizaron nombres de solución, proyectos, namespaces, build outputs, claves técnicas, tests y scripts a SOLQARYN.
- El frontend público quedó desacoplado de nombres comerciales y pasó a un módulo genérico de storefront con ruta técnica /tienda.
- No hubo migración ni eliminación de datos; los valores históricos persistidos permanecen bajo control de datos/migraciones explícitas.
