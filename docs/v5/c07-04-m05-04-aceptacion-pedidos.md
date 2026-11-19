# C07-04.M05.04 — aceptación positiva de seguimiento de pedidos

**Alcance R2** (pedidos comerciales). La entrega contiene únicamente Playwright y su workflow; no modifica lógica de cobros, confirmaciones de inventario, backend, base de datos ni permisos.

## A0–A3 causal

Tres escenarios Chromium sobre la aplicación Angular real levantada en GitHub-hosted: (1) listar PED-47, paginar a PED-48 y regresar, comprobando identificadores y ausencia de escrituras; (2) como lector autorizado abrir PED-47 a 320 px, comprobar cotización y detalle, sin botones de confirmar ni anular; (3) denegación del permiso `Ventas:Ver` impide exponer la ruta privada. Se utilizan respuestas simuladas de contexto empresarial, permisos y pedidos con nombres sintéticos; las páginas, router, guardias, componentes y solicitudes las ejecuta Angular real. Se observan métodos HTTP para rechazar intentos mutantes bajo el scope.

**Límite esencial:** esta es aceptación de UI para lectura, no aceptación integral de transacciones. La cabecera JWT, contexto de empresa y backend están simulados; no se acredita JWT firmado, RBAC servidor, tenant A/B, persistencia EF/MySQL, pagos, reservas ni idempotencia financiera. A1 positivos/negativos/frontera sobre frontend; A2 solo comportamiento UI; A3 Chromium real con servidor Angular local, API real NO_APLICA a prueba dirigida pero pendiente de acreditación global R2.

## A4–A5

Exigir GitHub Actions exact-SHA con tres Playwright Chromium reales, lint, build Angular, Scope Lock y V5; en `QUEUED/IN_PROGRESS` conservar VALIDANDO sin PASS. A5 requiere Task Javier distinta de DEV04 que inspeccione fuentes, aserciones, requisitos y pruebas, con rechazo posible. SKREF-10 Angular Developer original Google/Angular pin `6808966993bf2b2d455d9b6245e960a59a01b9a9` y testing-fundamentals consultados. Solo dev; rollback Git revert normal condicionado a no colisión; prohibido QA/main/PROD, datos reales y secretos.

## Reparación CI P2 de permisos por ruta (V20)

Primera ejecución Chromium: 2/3 PASS, detalle redirigió a `/perfil` porque el mock solo concedía `pedidos-venta`, mientras `permisoGuard` comprueba la ruta completa `pedidos-venta/:id`. Se corrige **únicamente el contrato del mock** para conceder ambos paths en el caso positivo y se añade un cuarto caso adversarial que mantiene exclusivamente `pedidos-venta` y exige denegación del detalle. No se modifica ni debilita el guard, ruta privada o backend; no afirmar que el backend real otorgue esa ruta sin inspeccionar su propia respuesta. CI anterior FAIL se conserva, nuevo SHA debe demostrar 4/4 reales, V5, Scope Lock y frontend.
