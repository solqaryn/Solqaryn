# SOLQARYN V17 — C02-02.M04.04: formularios, navegación, mensajes y single-flight de placement

**Autor:** PRIMARY_DEV_01 JAVIER_CHATGPT, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`; rama `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. PLAN fila 347, V17 lock 2000000347. Riesgo R2 por información privada y control de tenancy.

## A0 — alcance real, capas y límites

La ruta existente `/plataforma/placement-v5` sigue protegida por `plataformaGuard` y exclusivamente consulta la autoridad actual mediante `PlataformaSesionService`. La vista es informativa y no ofrece formularios para alterar bases, DSN, pooling, tenant, cutover ni migraciones. Se robustece `revalidar()` mediante `defer` para que una excepción síncrona del lector se convierta en estado de error seguro, no en spinner permanente ni mensaje del proveedor. `RouterLink` continúa apuntando a `/plataforma`, sin redirección operativa. El botón permanece single-flight; los estados accesibles `vacio/cargando/exito/sin_permiso/error` se muestran con `aria-live` y `aria-busy`.

## A1/A2 — negativos, fronteras, autorización

Ampliación de Vitest: excepción síncrona sin exposición de DSN, 401 con contenido oculto y navegación guardada, doble revalidación sobre una solicitud y finalización con estado éxito. Se preservan negativos previos 403/503, stream vacío, rol Empresa forjado, revocación, ruta lazy guardada y ausencia de formulario. Playwright Chromium sintético comprueba navegación, grant inicial y 403 posterior ocultando las dos modalidades y sin filtrar payload. No se infiere autorización por control visual, tema, cabecera o tenant ID de navegador.

## A3/A4 — evidencia exigida, NO_APLICA causal

Los workflows existentes `v17-c02-02-m04-01-placement.yml` y `v17-c02-02-m04-03-placement.yml` seleccionan el Vitest y Playwright modificados y ejecutan realmente npm ci, lint/build Angular, Chromium en GitHub-hosted; deben terminar `completed/success` con SHA exacto, junto con `verify-solqaryn-project-scope`, V5 y demás required checks pertinentes. Ninguna prueba redactada constituye PASS antes de logs. MySQL 8.4 EF, DSN, migración, JWT HTTP real, servidores de tenant y proveedores externos **NO_APLICA** al diff UI aislado; no se oculta esa limitación. Mantener VALIDANDO si CI requerida no terminó.

## A5 y reversión

A5 pendiente de revisor entre las otras cinco Tasks Javier, distinto de DEV01, con revisión propia de código, pruebas y CI. QA, main y producción no autorizados. Revertir el commit normal de dev sin force-push ni tocar datos, infraestructura o programaciones.
