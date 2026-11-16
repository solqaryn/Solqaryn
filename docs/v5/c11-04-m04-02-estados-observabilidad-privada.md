# C11-04.M04.02 — Estados y revalidación segura de observabilidad privada

Repositorio `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. Autor: DEV01 Javier, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. Riesgo R2 por revalidación de autoridad.

## Contrato ejecutable y límites

La superficie existente `/plataforma/observabilidad-v5` conserva el `plataformaGuard` y añade estados tipados `vacio`, `cargando`, `exito`, `sin_permiso` y `error`. Solo `GET /plataforma/sesion` del backend revalida autoridad; el botón no ejecuta exporter, alerta, métricas, auditoría durable, query de tenant ni consulta de logs. Durante `cargando`, ante 401/403/503, stream vacío o excepción, los cuatro frentes privados desaparecen; nunca se muestra el payload de error. Una única revalidación simultánea. La revocación del signal de sesión oculta contenido sin esperar otro HTTP.

A0: `observabilidad-privada-v5.component.ts`, su Vitest, Playwright, este documento y CHANGELOG. La modificación no toca API, DTO, DB, JWT, migraciones, servicios reales, credenciales ni routing de otras vistas. Dependencia: M03.01–06 y A5 acreditados por DAG; no promueve QA/PROD.

A1: Vitest de estado vacío, éxito, carga, una solicitud, respuesta vacía, excepción, 403, 503, suplantación y revocación; Playwright Chromium sintético 200→503 y 200→403, además de pruebas anteriores de ruta, denegación anónima y 375px.

A2: `plataformaGuard` no se elimina y la única fuente de permisos es `PlataformaSesionService`. Una etiqueta local no concede autoridad ni revela tenant. El resultado 403 o pérdida de sesión nunca muestra contenido.

A3: CI realiza Angular lint/build + Vitest + Chromium reales contra sesión API simulada; NO_APLICA pruebas de MySQL/EF, JWT HTTP real, exporters, SLO y bases productivas por ausencia de esos cambios; ninguna aserción equivale a pentest tenant A/B real.

A4: exigir en el SHA publicado `verify-solqaryn-project-scope`, V5, frontend y el job dirigido `C11-04.M04.01 - Vitest y Chromium de observabilidad` en completed/success. El workflow anterior ejecuta el fichero de Vitest ampliado y Playwright ampliado sin saltar negativos. Al publicar dejar VALIDANDO si CI externa está QUEUED/IN_PROGRESS, con RUN/SHA y readback.

A5: `A5_PENDIENTE` hasta revisión real por una Task Javier distinta de DEV01, que puede rechazar. Reversión mediante commit `git revert` del SHA de este alcance, nunca force-push. No se certifica latencia/CWV/SLO, proveedor, HTTP real, MySQL ni producción.
