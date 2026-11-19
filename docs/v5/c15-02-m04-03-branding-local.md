# C15-02.M04.03 - Branding local allowlisted y estilos seguros (DEV04 V20)

Alcance R2: la vista privada /plataforma/revision-arquitectura-v20 muestra cinco referencias NO_VERIFICADO. C1502_PRESENTACION inyecta SOLO {marca: institucional|neutra, densidad: regular|compacta}. El normalizador captura una vez los getters, rechaza arrays, proxies revocados, errores y CSS arbitrario, y retorna un objeto inmutable allowlisted. No cambia guards, backend, JWT, RBAC, MySQL, proveedores ni habilita escrituras. Ante ausencia de configuracion se mantiene estilo institucional original.

A0 componente y tests estrictamente locales, workflow, documentacion, CHANGELOG; sin nueva frontera arquitectonica. A1 tres Vitest negativos, positivos, getters con side effects, controles de presentacion; dos Playwright Chromium de layout responsive, no-mutacion y 403. A2 la sesion Empresa nunca muestra referencias; los tests HTTP son simulados y no validan tokens firmados o tenant A/B reales. A3 GitHub-hosted Node24 Angular lint/build y Chromium, no requiere EF/MySQL ni proveedor al no cambiarlos. A4 Scope Lock V5, pruebas GitHub exact-SHA y readback institucional. A5 Task Javier distinta de DEV04.

Prohibido promover este resultado a autorizacion R3 o certificacion de migracion, Cloudflare, R2, MySQL live, credenciales, QA/main/PROD. Rollback normal git revert sin force push.
