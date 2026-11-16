# C07-04.M04.03 — Presentación reactiva segura de pedidos privados

## Alcance A0 y clasificación
- Riesgo R2: UI privada de pedidos/ventas (sin mutaciones financieras).
- Gobernanza SOLQARYN V17, PROJECT_SCOPE_LOCK=STRICT, dev, TASK DEV02, PLAN fila 1138.
- Añade panel Angular Signals a listado existente y opciones locales exactas:
  marca SOLQARYN/SOLQARYN ERP, densidad cómoda/compacta, contraste normal/alto.
- No acepta HTML, URL, CSS, scripts, privilegios, plantillas o configuración externa.
- Muestra controles sólo con contexto tenant verificado, Ventas:Ver y ruta allowlist;
  revocación oculta el panel y bloquea cambios locales.
- No cambia APIs, Domain, Application, Infrastructure, JWT servidor, EF, MySQL,
  inventario, importes, facturación, pagos, secretos ni datos productivos.

## Criterios y pruebas A1-A3
- Seis Vitest: lista blanca positiva, payload malicioso negativo, permisos,
  revocación, fronteras de estado y UI sin formulario ni endpoints.
- Tres Playwright Chromium sobre ruta Angular real en GitHub-hosted: anónimo,
  autorizado a 320 px, botones accesibles y rechazo de permisos; backend,
  tokens y empresa son sintéticos y se declara NO_CERTIFICADO para HTTP/JWT/DB.
- Falla si hay desbordamiento horizontal del documento a 320 px o petición mutadora.
- A2 conserva permisos de servidor; este panel cosmético no amplía autorización.
- A3 UI/Chromium pertinente, MySQL 8.4/EF/proveedor NO_APLICA: sin modificaciones DB.

## A4, A5 y reversión
- Requerir GitHub Actions del SHA exacto: Vitest/Chromium, Angular build/lint,
  Scope Lock, V5 changelog y seguridad/aceptación aplicable SUCCESS.
- Si CI QUEUED/IN_PROGRESS, conservar VALIDANDO y propietario. Fallo implica
  QA_DEFECT_FOUND y reparación causal; no marcar LISTO ni usar SKIPPED como PASS.
- A5 por Task Javier distinta de DEV02, no autorrevisión.
- Rollback por git revert normal del único commit sobre HEAD nuevo verificado.
- No QA, main, PROD, SQL, transacciones financieras ni servicios de pago.
- SKREF-10 original angular/angular@6808966993bf2b2d455d9b6245e960a59a01b9a9
  consultada para Signals, UI y pruebas, sin cambiar arquitectura.
