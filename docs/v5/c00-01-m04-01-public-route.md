# V5 C00-01.M04.01 — Superficie pública para rutas no reconocidas

## Resultado exigido
Implementar una pantalla ligera y accesible para rutas Angular desconocidas, preservando la navegación pública y las puertas JWT/RBAC/tenant existentes.

## Impacto y límites
- Frontend: wildcard de `app.routes.ts` cambia de redirección silenciosa al login a lazy-loading de `NotFoundComponent`.
- La pantalla muestra un título descriptivo, mensaje genérico, navegación a login y a tienda; no refleja rutas, query parameters, IDs de empresa ni respuestas del backend.
- No modifica las rutas conocidas, guards `authGuard`/`permisoGuard`/`plataformaGuard`, API, DTO, SQL, configuración de tenants, secretos, proveedor de medios ni sesiones.
- Browser/UX: encabezado semántico, `main`, `nav`, foco visible y objetivos interactivos >=44px. HTML servido por SPA no equivale a status HTTP 404 del servidor.

## Auditorías y pruebas
- A0: revisión de diff y patrones sensibles; lint TypeScript, Angular build; cambios limitados a frontend, CI dirigida y trazabilidad.
- A1: Playwright positivo de acceso público y navegación de regreso al login; negativo de ruta desconocida que no refleja query ni revela contenido privado.
- A2: guard de dashboard anónimo conserva redirección a login; secretos/tenants no entran al componente.
- A3: workflow N8.2 CI ejecuta Chromium real contra Angular dev-server; no requiere .NET/MySQL/Docker porque no hay interacción con API ni migración por este cambio.
- A4: check exact-SHA de Scope Lock, frontend compilación/lint, N8.2 Playwright; readback de PLAN/COLA/BITACORA.
- A5: revisión independiente pendiente tras cierre DEV, no autorización automática para QA/main/PROD.

## Rollback
Revertir el commit con `git revert <sha>` de forma no destructiva tras revalidar HEAD. No fuerza refs ni elimina trabajo concurrente.
