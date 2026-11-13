# P5-013.C — Delimitación causal: una rama activa del menú

- Autoridad: Plan Comercial Inicial V3; P5-013.A, dependencia P1-016.B = LISTO. Este cambio cumple solamente la microtarea **DELIMITAR**; no afirma que el comportamiento de acordeón esté implementado.
- Único archivo de producto seleccionado (`TARGET_PATH`): `frontend/src/app/shared/navigation/navigation-contract.json`.
- `BASELINE_BLOB_SHA`: `7142082462d7bc53ff4f0cdf96f67f21405f3a44`, leído de `solqaryn/Solqaryn@dev`; el contrato de navegación no se modifica por esta delimitación.
- Escenario concreto: grupo `administracion` de `groups` y sus rutas hijas. El contrato delimita su identificación y permisos; `AppNavigationMenuComponent` renderiza las secciones, pero el JSON no posee estado `activeBranch` ni demuestra apertura exclusiva de una única rama.
- Positivo reproducible: esquema versionado, ocho grupos con IDs distintos, grupo `administracion` único, rutas no duplicadas y permiso `Permisos/Administrar`; `Auditoria` mantiene `requires_admin=true`.
- Negativos reproducibles: grupo duplicado, ruta duplicada, debilitamiento de `Permisos/Administrar` y pérdida de `requires_admin` de auditoría deben rechazarse con `node --test scripts/tests/p5-013-c-delimitacion-menu.test.mjs`.
- Límite explícito: esta comprobación es de contrato estructural real; **no prueba** una sola rama expandida en el navegador, foco/ARIA interactivo, autorización HTTP/JWT, RBAC del servidor, MySQL, accesibilidad WCAG o despliegue live. El alcance de ajuste de expansión pertenece a P5-013.D y sus verificaciones posteriores.
- Impacto: una nota y un test Node sin dependencias externas; ningún cambio en UI Angular ejecutable, componentes, DTO/API, persistencia, JWT/BCrypt, pertenencias A/B, secretos, infraestructura, QA/main/PROD. Los controles server-side siguen siendo obligatorios e independientes del menú.
- Rollback: revert selectivo mediante un nuevo commit normal en `dev` que quite nota, prueba y entrada de CHANGELOG; nunca force-push ni revertir trabajo de terceros.
