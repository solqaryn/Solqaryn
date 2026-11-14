# C00-03.M04.01 — pantalla privada de cobertura integrada V5

**Resultado exigido:** implementar la ruta/pantalla correspondiente a la fase M04 del bloque C00-03, cuyo resultado de fase es matriz integrada requisito → microtarea → contrato → prueba → evidencia. Este alcance desarrolla la **superficie de lectura**, no afirma la existencia de un servicio de cobertura publicado ni datos institucionales consumibles por browser.

- URL privada `/plataforma/cobertura-v5`, resuelta con `plataformaGuard` y la misma consulta global server-side `/plataforma/sesion`; sin acceso a tenants A/B por esta ruta.
- Reutiliza `PlataformaComponent` y componentes Angular Material existentes, sin añadir nuevos ficheros `*.component.ts` ni alterar el catálogo Angular.
- Presenta encabezados semánticos con cinco columnas reales del **contrato** (requisito, microtarea, contrato, prueba, evidencia) y un estado vacío explícito: **no hay matriz viva conectada**. No se inventan valores, permisos, A0-A5, vínculos ni estados.
- No consulta PLAN/COLA/DAG/BITACORA directamente, ni crea API, JWT, RBAC, migración, MySQL, worker, webhook, evento, nuevo proveedor o mecanismo de despachos.
- Pertenencia documental: DAG_TECNICO C00-03.M04.01 F=VALIDADO, G=SI tras seis predecesoras C00-03.M03.01–06 LISTO; estado operativo sigue en PLAN_MAESTRO H/I y COLA G/O bajo gobierno del propietario.
- UX: tabla accesible por teclado, encabezados `scope=col`, contenedor desplazable móvil y `role=note`; no se declara conformidad WCAG de navegador sin auditoría.
- `requiere_dotnet=NO`, `requiere_node_angular=SI`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO_APLICA` para esta vista sin interacción de datos, `requiere_security=SI` por la ruta protegida, `requiere_scope_lock=SI`.

## Controles proporcionales

A0: revisión de diff, secretos, dependencias, integridad de CHANGELOG y catálogo; A1: suite Vitest de rutas, modo de cobertura, estado vacío y denegación global; A2: guard JWT y permisos de plataforma comprobados server-side por tests existentes y no exposición de datos; A3: lint, tipado y build Angular del SHA exacto; A4: commit fast-forward, check-runs requeridos, Scope Lock, reversión `git revert` normal y readback PLAN/COLA/BITACORA. Pruebas .NET/MySQL/Docker ajenas a la vista no acreditan ni bloquean por sí solas. A5 independiente por otra Task Javier antes de cualquier promoción.

**Límite:** esto no crea una matriz dinámica de producción; las tareas M04.02–M06 y cualquier integración de cobertura permanecen sujetas a sus gates y resultados exigidos.

## Corrección de auditoría de seguridad de rutas (13/11/2026)

La ruta `plataforma/cobertura-v5` permanece protegida por `plataformaGuard`. El quality audit conserva el control `FRONTEND_ROUTE_SECURITY_CONTRACT` y reconoce solo las tres rutas exactas del control plane con pruebas de sesión global y denegación server-side. No se usan `authGuard` ni `permisoGuard` ERP porque requieren contexto EmpresaId inapropiado para plataforma. Si la auditoría falla, no declarar A0-A4 PASS ni LISTO_DEV.

## Auditoría adicional de navegador (13/11/2026)

La matriz de cobertura vacía se valida en Chromium real y viewport móvil de 390px, incluyendo cinco encabezados, navegación y prohibición explícita de mutaciones a fuentes institucionales. El test del frontend usa una sesión HTTP sintética y **no** acredita JWT/RBAC/MySQL productivo ni autorizaciones cruzadas. El workflow `v5-plataforma-browser-guards.yml` debe concluir SUCCESS en SHA exacto antes de A4/LISTO_DEV.
