# SOLQARYN V5 FINAL — C13-04.M01.02: actores, interfaces, entradas/salidas y fronteras de experiencia visual

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 06 JAVIER (`6af16bbad26881918c283abe06e1f32a`). **Repositorio:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. **Alcance:** M01 preparacion documental de `C13-04`, sin alterar Angular, frontend, backend, MySQL, JWT/RBAC, tenant, assets, CI ni despliegues.

**Fuentes autoritativas:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), `AGENTS.md`, `PROJECT_SCOPE_LOCK.md`. Preflight: PLAN H=PENDIENTE/I vacio; COLA G/O=SI; DAG F=VALIDADO/G=SI, CAS exclusivo registrado. M01.01 [blob `fedf00f157e9d14e9150c48601070e84b0306ff3`] define NR01–NR14, matriz futura de ruta/rol/estado/viewport y ocho pares P/N; M01.03 [blob `f02a82a632b6abb6f35f83a22cb8e7fe834bb573`] inventaria 14 fuentes y 12 brechas. Ninguno equivale a conformidad WCAG ni a una prueba funcional ejecutada.

## 1. Actores y necesidades de la superficie

Los actores son **roles de analisis** para una matriz futura, no nuevos roles RBAC ni cuentas creadas. Una persona puede pertenecer a varios contextos; el permiso efectivo siempre se comprueba en el backend.

| Actor | Perfil / canal | Entrada, accion y salida observable | Limite imprescindible |
| --- | --- | --- | --- |
| A01 | Usuario de tienda publico | Producto, oferta, carrito, error recuperable | Navegacion publica sin acceso a datos de backoffice |
| A02 | Cliente autenticado | Checkout, cuenta, sesion y mensaje de error | Datos de pedidos del cliente solo bajo autorizacion |
| A03 | Operador comercial | Pedidos, inventario y filtros | Acciones RBAC/tenant se deciden por API |
| A04 | Supervisor de sucursal | Indicadores y revision de operaciones | Sucursal no equivale a permiso global |
| A05 | Administrador tenant | Configuracion, usuarios y branding | Sin acceso a datos de empresa ajena |
| A06 | Administrador plataforma | Vista global administrada | Controles elevados server-side y auditoria |
| A07 | Operador de catalogos | Tablas, formularios, estados vacios | No debe filtrar entidades ajenas en autocompletado |
| A08 | Persona que usa teclado | Foco, atajos, cierres de dialogo | Navegacion sin trampas ni acciones invisibles |
| A09 | Persona con lector de pantalla | Semantica y aria-live de acciones/errores | Sin depender solo de color o icono |
| A10 | Persona en pantalla pequena | Reflow, scroll, acciones primarias | Contenido critico accesible en 320px |
| A11 | Agente de soporte autorizado | Observa repro de UI con evidencia redaccion | No incluye PII ni credenciales en capturas |
| A12 | CI / validador de UX | Build, rutas, snapshots, pruebas dirigidas | Check de un SHA no sustituye medicion de otro |

## 2. Contratos de interfaz y flujos de entrada/salida

Los nombres de rutas y archivos aqui son rutas Git reales inspeccionadas en el inventario M01.03; se describen sus responsabilidades declarativas, sin inventar una API nueva.

| Interfaz | Superficie | Archivo/contrato | Entrada representativa | Salida/feedback |
| --- | --- | --- | --- | --- |
| I01 | Ruta publica/entrada | `app.routes.ts` | URL/parametro storeKey | Vista de storefront o error seguro |
| I02 | Ruta privada/guard | `app.routes.ts + permiso.guard.ts` | JWT valido/rol/accion | Acceso o redireccion segura |
| I03 | Contexto de empresa | `tenant-context.service.ts` | empresa solicitada + sesion | Empresa validada por backend o rechazo |
| I04 | Menu contextual | `app-navigation-menu.component.ts` | Permisos cargados + rutas habilitadas | Enlaces y grupos visibles segun contrato |
| I05 | Contrato de navegacion | `navigation-contract.json` | Modulo/accion/ruta | Jerarquia util para auditoria |
| I06 | Tema visual compartido | `styles.scss` | Tokens semanticos y viewport | Color, texto, estado y foco visibles |
| I07 | Escala enterprise | `_enterprise-design-system.scss` | Espaciado/tipografia/breakpoints | Componentes consistentes |
| I08 | Capa de accesibilidad | `_m10-accessibility.scss` | Preferencia/foco/scroll | Lectura y orientacion de pagina |
| I09 | Feedback de estado | `feedback-state.component.ts` | loading/empty/success/error/warning | Texto y anuncio aria-live |
| I10 | Router/animacion | `app.config.ts` | Transicion de ruta | Render y preloading configurable |
| I11 | Regresion responsive | `responsive-global.spec.ts` | Lista de rutas y viewports | Oraculos automatizados cuando se ejecuten |
| I12 | Auditoria de UX | `m10-ui-ux-accesibilidad.spec.ts` | Fixtures y browser E2E | Hallazgos solo si CI exact-SHA realmente ejecuta |

**Flujo positivo futuro (no ejecutado):** usuario elige ruta -> router y guard consultan contexto -> backend valida JWT/membresia/accion -> vista Angular carga datos de la empresa autorizada -> feedback y foco anuncian carga, exito o error -> componente se adapta al viewport sin perder accion principal. Una ruta publica no salta seguridad de APIs privadas.

**Flujo negativo futuro (no ejecutado):** solicitud de tenant B desde sesion autorizada en A o permiso ausente -> backend deniega -> la UI presenta error semantico sin mostrar datos, precio ni marca de B -> cache/sesion no contaminan la siguiente ruta. La prueba final debe cotejar HTTP y persistencia, no solo una captura de la UI.

## 3. Fronteras de confianza y no regresion

| Frontera | Cruce relevante | Invariante de seguridad o experiencia |
| --- | --- | --- |
| F01 | Navegacion browser -> router | Ruta solicitada no concede permiso |
| F02 | Router guard -> API | JWT/RBAC/tenant validados en backend, no por menu oculto |
| F03 | Contexto tenant -> UI | Selección cliente no es contexto verificado |
| F04 | Token CSS -> branding | Personalizacion no introduce CSS arbitrario ni fuga A/B |
| F05 | Componente -> estado accesible | Error/empty/loading debe informar sin exponer detalles internos |
| F06 | UI responsive -> interaccion | Breakpoint no elimina acciones ni orden de foco |
| F07 | E2E -> datos fixture | Prueba automatizada no opera cuentas ni PII reales |
| F08 | Documento -> certificacion | Oraculo propuesto no equivale a medicion, PASS o despliegue |

No se reemplaza un control del servidor por Signals, guards, estado de menu, almacenamiento local o token CSS. Los componentes visuales nunca elevan permisos ni determinan ownership de registros. Las auditorias de interfaz deben preservar confidencialidad, contrastar tenant A/B y no publicar secretos o PII en logs, imagenes o fixtures.

## 4. Matriz de aceptacion FUTURA_NO_EJECUTADA

| Caso | Prueba positiva propuesta | Prueba negativa propuesta |
| --- | --- | --- |
| P01/N01 | Login y formulario anuncian validacion con foco adecuado | Error solo por color o foco en lugar equivocado |
| P02/N02 | Usuario autorizado ve accion de menu y dato permitido | Ruta/menu expone datos a rol sin permiso |
| P03/N03 | Cambio A->B recarga tema/contexto autorizado sin datos residuales | Cache o logo de A permanecen en sesion B |
| P04/N04 | Estado empty/error/loading presenta texto y recuperacion | Spinner indefinido o silencio de lector |
| P05/N05 | Modal cierra y devuelve foco al desencadenante | Foco atrapado o restaurado al body |
| P06/N06 | Vista 320/390/768/1280 deja CTA funcional y tabla contenida | Desborde horizontal global oculta acciones |
| P07/N07 | Movimiento reducido conserva funcionalidad y orientacion | Animacion obligatoria para reconocer un estado |
| P08/N08 | Pruebas relacionadas con cambios llegan a CI exact-SHA SUCCESS | SKIPPED/CANCELLED/QUEUED se presenta como PASS |

Esta matriz constituye **oraculos de aceptacion por implementar en fases correspondientes**, no ejecuciones de Playwright, build, HTTP/MySQL o auditorias WCAG. Cuando se ejecute debera guardar SHA, navegador, rol, tenant sintetico, viewport, dispositivo, evidencia positiva/negativa, accesibilidad, artefactos, errores y rollback. La matriz no autoriza despliegue, produccion, eventos ni migraciones.

## 5. Impacto y plan de verificacion de esta entrega

- **Rutas afectadas en Git:** este documento nuevo y `CHANGELOG_AI.md` con seccion autocontenida. No hay mutacion de tokens, codigo, servicios, endpoints, contratos DTO, datos, indices, auth ni dependencias.
- **Clientes:** equipo UI, seguridad, QA futuro, soporte y revisores A5; el inventario se usa como referencia de flujo y lenguaje, no prueba operativa.
- **A0 documental:** leer alcance y diff de dos Markdown, validar ids A01–A12 / I01–I12 / F01–F08 y ausencia de secretos.
- **A1 documental:** comprobar que cada actor tiene entrada/accion/salida y limite; cada interfaz tiene origen y salida; ocho P/N son explicitamente futuros.
- **A2 documental:** verificar limite de autorizacion backend, JWT/RBAC/tenant A/B, error seguro, foco/aria y no persistencia de PII.
- **A3 documental:** correlacionar M01.01 y M01.03 con rutas/archivos reales y su Git blob; no se exige compilar un proyecto que no ha cambiado.
- **A4:** diff publicado por fast-forward/CAS en `dev` y checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` SUCCESS en SHA exacto, con readback Git y Sheets. Checks runtime no causales -> NO_APLICA motivado; los requeridos no se tratan PASS hasta completion.
- **A5:** revision independiente posterior por Task distinta de `6af16bbad26881918c283abe06e1f32a`, sin autoaprobacion ni autorizacion QA/main/PROD.
- **Rollback:** `git revert` del commit preciso por cambio documentario, sin forzar ref ni reescribir historia; preservar integridad BITACORA y dependencias.
