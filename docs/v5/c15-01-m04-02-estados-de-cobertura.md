# SOLQARYN V17 — C15-01.M04.02 estados locales de cobertura

Repositorio solqaryn/Solqaryn, rama dev. DEV06 (6af16bbad26881918c283abe06e1f32a), PLAN_MAESTRO fila 2181. Candado V17 2000002181, COLA G/O=SI y DAG F=VALIDADO/G=SI comprobados antes del CLAIM.

## A0, alcance y riesgo
Riesgo R1 por estado visual local, con revisión conservadora de permisos de pantalla. Se amplía la superficie privada `/plataforma/trazabilidad-cobertura-v5` sin crear ruta, endpoints, persistencia, peticiones institucionales ni permisos. Fuente predeterminada: cuatro dimensiones declarativas ya existentes; no mide requisitos, duplicados, CWV ni cobertura del plan. Un InjectionToken opcional permite pruebas con resoluciones y fallos controlados, nunca inyecta información de fuentes externas en runtime.

## A1 a A3 causal
Estados: cargando mientras termina la promesa local, vacío con lista vacía, error sin volcar excepciones, éxito con dimensiones declarativas y denegado por sesión/ámbito. Botón reintentar sólo tras error. Revisión/revocación invalida la solicitud pendiente para que un resultado tardío no reaparezca; faltan valores obligatorios => error cerrado. Pruebas Vitest cubren positivo, carga, vacío, error/reintento, frontera de una dimensión, contenido inválido y revocación con respuesta tardía; navegador Chromium prueba 320/768, lectura sin POST y anónimo. La sesión HTTP sintética de Playwright no certifica JWT/tenant backend real.

No cambian MySQL/EF, API, integraciones, tablas, Cloudinary ni información comercial; A2 servidor y A3 MySQL/HTTP real NO_APLICA al diff. A3 sí exige compilación Angular y Chromium reales GitHub-hosted sobre SHA publicado.

## A4 y A5
No declarar LISTO mientras falten checks obligatorios exact-SHA o equivalencia auditada, Scope Lock y V5, ni antes de readback del PLAN/COLA/DAG/BITACORA. Tras ci externo real en ejecución, estado VALIDANDO y owner DEV06 con RUN/SHA, candado liberado; A5 sólo por otra Task Javier que inspeccione código, pruebas y fallos. Rollback seguro mediante git revert sobre dev después de verificar HEAD; no force-push ni QA/main/PROD.
