# ARCHITECTURE_CHANGELOG — Solqaryn

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
