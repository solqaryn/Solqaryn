# C14-04.M04.03 — Branding seguro para readiness DEV

## A0, arquitectura e impacto
Riesgo R2 por sesión privada y controles de go-live. Se amplía solamente
/plataforma/flujo-v5, ruta existente bajo plataformaGuard y sesión confirmada
por GET servidor. La superficie nueva usa Angular 22 Signals y opciones cerradas:
marca SOLQARYN/SOLQARYN DEV; densidad cómoda/compacta; contraste normal/alto.
No admite CSS, URLs, HTML, flags externos ni nombres de tenant.
No certifica el go-live, CI, DNS, Cloudflare, R2, backup, restore ni SLO.
No modifica seguridad backend, JWT, RBAC, EF/MySQL, APIs, infraestructura o datos.

## A1–A3 pruebas pertinentes
Seis Vitest (positivo, payload negativo, default, toggle, sesión ausente,
revocación) y tres pruebas Chromium (anónimo, permiso sintético 320px, 403).
El panel no contiene formularios ni botones mutadores; contraste y densidad
se presentan como data-attributes cerrados, controles de 44px, sin overflow.
Los tests no convierten sesión HTTP simulada en JWT real o evidencia MySQL.
A3 para UI es Angular build y Chromium hosted; MySQL/EF no aplica a este diff.

## A4 / A5 y rollback
CI con SHA publicado: tests 6/3, Chromium de rutas V5, lint/TS/build,
Scope Lock, procedencia CHANGELOG, seguridad y aceptación aplicables
completed/success. QUEUED/IN_PROGRESS/SKIPPED/FAIL nunca PASS; validar
equivalencia por blobs si CI original cancela.
A5 sólo Task Javier distinta de DEV02. Rollback normal git revert verificando
HEAD y dependencias; jamás force-push. Sin QA/main/PROD, dinero ni proveedores.
Referencia consultada: SKREF-10 angular/angular@6808966993bf2b2d455d9b6245e960a59a01b9a9
original; no nueva arquitectura o paquetes npm.
