# P5-002.C - Delimitacion del shell administrativo (DEV)

- Autoridad: Plan Comercial Inicial V3, P5-002.C, origen P5-002.A. Resultado exigido: identificar un archivo/escenario del shell, guardar ruta, SHA y un cambio minimo, sin tocar otros modulos.
- TARGET_PATH (unico archivo real de producto): `frontend/src/app/app.component.ts`.
- BASELINE_BLOB_SHA: `11aa993b011119d33e402f3f2bc1d08f133e34bd` (GitHub, sobre commit bdd96f8be928081cd17d1e8b7bf767627d0d860f; reconfirmar inmutabilidad del blob en HEAD al publicar).
- Escenario delimitado: shell administrativo condicionado a `auth.isAuthenticated()`, enlace para saltar a `#main-content`, menu lateral accesible, encabezado y router-outlet, Escape para cerrar el menu movil y devolver foco, cambio de empresa que invalida contexto y permisos antes de navegar a `/login`.
- Cambio minimo: prueba estatica de una sola fuente en `scripts/tests/p5-002-c-delimitacion-shell.test.mjs`. No se cambia el componente Angular, servicios, guards, RBAC backend, API, migraciones, datos ni infraestructura.
- Criterio positivo: la prueba sobre el contenido real de app.component.ts debe hallar el gate autenticado, las anclas accesibles y el reset de contexto. Criterio negativo: quitar el gate, romper el skip-link/foco o relajar la limpieza tenant debe provocar fallo. Se rechaza HTML crudo.
- Comando reproducible en checkout autorizado: `node --check scripts/tests/p5-002-c-delimitacion-shell.test.mjs && node --test scripts/tests/p5-002-c-delimitacion-shell.test.mjs`.
- Limites de certificacion: este micro-paso DELIMITAR comprueba codigo fuente estatico, no UX real de navegador, lectores de pantalla, Render/Vercel, seguridad real A/B de endpoints, ni conducta de Angular en runtime; esas pruebas son posteriores y pertenecen a otros scopes.
- Seguridad: la UI no otorga permisos y no reemplaza JWT, RBAC ni validaciones del backend. No se agregan dependencias, secretos ni persistencia.
- Rollback: revertir selectivamente la prueba, nota y entrada de changelog por un nuevo commit fast-forward dev, preservando trabajo de otros Scopes.
