# SOLQARYN V5 — C05-02.M01.02: actores, datos y superficies de branding

**Microtarea:** C05-02.M01.02 — identificar entradas, salidas, actores y superficies; preparación documental de la fase M01 de Branding y presets. **Task autora:** PRIMARY DEV 01 / JAVIER_CHATGPT / \`6adbb9fff1548191a1bd5b30d9e9984a\`. **Fuente única:** Standard de Implementación de Automatizaciones V5 FINAL, Drive \`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA\`; PLAN_MAESTRO fila 759. **Fecha:** 2026-11-14. **Repo/rama:** \`solqaryn/Solqaryn@dev\`, STRICT.

## Alcance real

Esta unidad **delimita** quién inicia una acción de tema, identidad, logo o preset, qué datos ingresan y salen y qué frontera de confianza se debe validar en implementaciones posteriores. No implementa editor, preview versionada, tema por rubro, gateway, almacenamiento, revisión ni publicación automática. Las pruebas propuestas al final son oráculos futuros, no resultados runtime. El contrato de no-regresión M01.01 vive en \`docs/v5/c05-02-m01-01-objetivo-no-alcance-branding-presets.md\`; esta M01.02 no duplica sus objetivos.

## Actores y autoridad

| Actor | Operación prevista | Autoridad mínima y límite |
| --- | --- | --- |
| Administrador empresarial autenticado | Editar colores/identidad o subir logo de su empresa | JWT, membresía y permisos reales verificados **en API**, no por visibilidad de formulario ni parámetro \`empresaId\`. |
| Operador sin permiso de personalización | Consultar diseño publicado si su acceso a la aplicación lo permite | No obtiene edición, publicación ni lectura de datos de otra empresa. |
| Usuario de plataforma autorizado | Consultar administración global permitida | No hereda automáticamente permiso de editar cualquier tenant por elegir una marca. |
| Visitante de sitio público | Ver identidad pública verificada por host/tienda | No obtiene endpoints administrativos, parámetros internos ni datos privados. |
| Diseñador/revisor futuro de presets | Proponer preset, previsualizar o aprobar si se autoriza esa capacidad | **Rol futuro, no implementado aquí**; se requiere contrato RBAC y segregación de decisión. |
| Cliente Angular | Representar tokens, limpiar estado y gestionar feedback | Nunca es autoridad de empresa ni de autorización. |
| API .NET / servicios de aplicación | Autenticar, resolver empresa, validar cambios y emitir salida | Toda operación de escritura requiere control de permisos, esquema y auditoría. |
| Persistencia/almacenamiento existente o futuro | Guardar tema, logo o revisión autorizada | No inferir versionado o rollback ya existente por tener una operación \`restaurar\`. |

## Entradas, salidas y proveniencia observable

| Entrada / canal | Forma vigente leída | Salida/caso de error exigido |
| --- | --- | --- |
| \`TemaVisualService.get/update/restaurar\` | \`GET/PUT /tema-visual\`, \`POST /tema-visual/restaurar\` | \`ApiResponse<TemaVisual>\` en el cliente; errores deben mantener tema seguro. |
| \`TemaVisual\` y \`CAMPOS_TEMA\` | 16 campos conocidos de colores y destino CSS específico | Solo tokens permitidos; string hex \`#RGB\` o \`#RRGGBB\`; sin CSS libre. |
| \`ThemeApplierService\` | \`aplicarTemaGuardado\`, \`aplicarTemaPlataforma\`, \`aplicar\` | Aplicación al \`document.documentElement\` con limpieza previa, contraste y rechazo de respuestas anteriores. |
| \`EmpresaIdentidadService\` | \`getPublica\`, estado Signals, \`usarPlataforma\` | Nombre/logo de contexto vigente, fallback seguro; el texto no atribuye tenant. |
| \`EmpresaConfiguracionService\` | \`getPublica\`, \`update\`, \`updateLogo\`, \`restaurarLogo\`, rutas \`/tenant/{empresaId}\` y logo tenant | DTO y URL de logo según API; **no asumir** permiso de edición por \`empresaId\` solicitado. |
| \`EmpresaConfiguracionService.getPublicaTienda\` | Ruta pública por \`storeKey\` validado en frontend, host/tienda resueltos por backend | Identidad pública únicamente del contexto verificado; ningún token admin en respuesta. |
| \`ColorContrastService\` | \`foregroundFor\` y \`ensureReadable\` | Propuesta de texto legible; no certificación WCAG de la página entera. |
| Diseño futuro de presets | Fuente por rubro, versiones, preview/publish/rollback y auditoría | **No existe contrato acreditado por esta lectura**; requiere fase y aprobación propias. |

Las rutas y clases anteriores son evidencia de código, no una garantía de ejecución HTTP, MySQL o integración en producción. La existencia de \`/restaurar\` **no demuestra** versionado de snapshots con compare-and-swap ni recuperación de un tema publicado tras falla parcial.

## Blobs exactos observados en dev

| Fuente | SHA blob | Observación |
| --- | --- | --- |
| \`frontend/src/app/core/models/tema-visual.model.ts\` | \`9d99a1da70c9c6da408d9a564e91512150b4aedd\` | Lista \`TemaVisual\` y \`CAMPOS_TEMA\`, 16 asignaciones CSS. |
| \`frontend/src/app/services/tema-visual.service.ts\` | \`588aa46fc4c10f8d6be85628e919a7bd89f366ac\` | API de tema actual. |
| \`frontend/src/app/services/theme-applier.service.ts\` | \`cbccc2795cb1b010f74ce5b2b4dc1fb20f0b41f3\` | Regex hex, limpieza y protección frente a respuesta obsoleta. |
| \`frontend/src/app/services/theme-applier.service.spec.ts\` | \`99529c4f26a075f58d1b0a08f6817c4d42003814\` | Define casos unitarios positivos/negativos para colores, CSS ajeno y carreras A/B; se inspeccionó el archivo, **no se ejecutó**. |
| \`frontend/src/app/services/empresa-configuracion.service.ts\` | \`0e4f127bc2a7dce504a7318546231fb0776903fe\` | Logo y configuración, incluyendo endpoints de tenant. |
| \`frontend/src/app/services/empresa-identidad.service.ts\` | \`88526c37ad3d50dc454e8e23ce7899818a0ada72\` | Signal y versiones de identidad. |
| \`frontend/src/app/services/color-contrast.service.ts\` | \`2b789f4af6c345068dc689c727313b859f438364\` | Algoritmo de contraste y opción foreground. |
| \`frontend/src/styles/_enterprise-design-system.scss\` | \`6a188590af9a958bff2e3e0375c0c46cdb2b14e6\` | Tokens de base y foco/accesibilidad. |

## Fronteras de confianza y amenazas a negar

1. **Navegador → API privada:** el cliente no decide rol ni \`EmpresaId\` autoritativo; backend valida identidad, empresa, permiso y contexto en cada escritura.
2. **URL pública/host/storeKey → identidad visible:** resolver binding de tienda por autoridad backend; no aceptar slug como prueba de pertenencia.
3. **Tema recibido → CSS DOM:** solo claves \`CAMPOS_TEMA\` y colores hex; rechazar código ejecutable, \`url(...)\`, \`var(...)\`, valores con salto de línea y claves no conocidas.
4. **Empresa A → B:** limpiar tokens, logo y respuestas tardías; invalidar cachés condicionadas por empresa; no probar aislamiento de datos solo mirando estilos.
5. **Logo input → archivo/URL:** comprobar tipo real, límite de bytes, titular, ubicación y URL permitida antes de persistir; CDN o bucket futuro no reemplaza autorización.
6. **Preview → publicación:** sin escritura implícita; aprobación/versionado futuro con compare-and-swap y auditoría; revert debe ser reversible por historia.
7. **Cambio de paleta → accesibilidad:** verificar estados activos, foco, error no solo cromático, contraste y tamaños de vista; revisar reduced-motion.
8. **Error de red/guardar → fallback:** conservar identidad segura y señal clara sin considerar exitoso un PUT fallido.
9. **Identidad pública → datos privados:** respuesta pública excluye permisos, secretos, configuración confidencial y datos internos de otra empresa.

## Matriz de pruebas a implementar después (NO EJECUTADAS en M01.02)

| Positivo futuro | Negativo futuro |
| --- | --- |
| P01: empleado autorizado ve y edita solo tema de su empresa | N01: usuario sin permiso intenta \`PUT /tema-visual\` |
| P02: tema válido pasa por 16 campos y conserva contraste | N02: payload CSS o clave de campo no permitida |
| P03: A→B elimina tokens y respuesta tardía de A | N03: respuesta A llega después de cambiar a B |
| P04: visitante ve identidad de tienda resuelta por host | N04: \`storeKey\` o \`empresaId\` de A pretende datos B |
| P05: preview de preset futuro no persiste ni eleva rol | N05: preview provoca escritura o filtra claims JWT |
| P06: logo validado se conserva sin fuga pública de configuración interna | N06: archivo MIME engañoso o logo de empresa ajena |

Negativos complementarios: **N07** script en etiqueta, **N08** conflicto de versión en publicación, **N09** caída a mitad de persistencia, **N10** color ilegible/foco oculto, **N11** URL insegura de medio, **N12** rol inferido de marca. Los seis P y doce N son **criterios futuros** para fases de implementación/QA, no logs de ejecución ni PASS real de .NET/Angular/MySQL/Playwright.

## Gates de entrega

Preflight de M01.02: \`PLAN_MAESTRO!H759/I759\`, \`COLA!G759/O759\`, \`DAG_TECNICO!A720:G720\`. Los predecesores documentales del DAG y la preparación M01 no autorizan cambios de JWT/RBAC, migraciones, workers, almacenamiento R2, eventos, publicación real de presets, QA/main/PROD o cierre de M01.06.

**A0:** inventario causal de ocho blobs, diff solo Markdown, sin secretos. **A1:** seis escenarios positivos y doce negativos redactados como oráculos, no ejecutados. **A2:** revisar fronteras JWT/RBAC/tenant A/B, CSS y logo sin modificar runtime. **A3:** coherencia entre modelos, clientes y contratos de respuesta; sin afirmar pruebas HTTP/DB. **A4:** commit fast-forward exact-SHA, Scope Lock y check de procedencia V5 success, readback PLAN/COLA/BITACORA; otras compilaciones de CI \`NO_APLICA\` solo si el diff es documental. **A5:** revisión independiente pendiente de otra Task Javier.

Rollback de este documento por \`git revert\` normal sin fuerza. No modificar secretos, dominios, entornos productivos, ramas main/QA/PROD, configuración física de Tasks ni despachadores de eventos.
