# SOLQARYN - CI por impacto en DEV

## Controles que no se retiran

Siguen obligatorios `verify-solqaryn-project-scope`, `SOLQARYN - Seguridad multiempresa`, `SOLQARYN - Build y pruebas backend` y el check dirigido comercial aplicable. El verificador de certificacion automatica sigue intacto: cualquier check de GitHub realmente activado que falle o quede pendiente bloquea el merge.

## Enrutamiento causal

- `catalogos-aceptacion.yml`: ejecuta Playwright ante cambios frontend, backend operativo o sus otros disparadores declarados. Excluye exclusivamente cambios dentro de `backend/tests/**` del disparo de esta suite.
- `priority4-quality.yml`: no se activa por `backend/tests/**` como unico impacto; mantiene compilacion, lint y regresiones `Category!=Integration`. La integracion MySQL pertenece al job de migraciones de `dev-ci.yml`.
- `dev-ci.yml`: un preflight `impact` ejecuta el clasificador en Node 24, lo prueba y compara los SHA exactos de base y HEAD; el job MySQL solo se omite cuando el diff contiene exclusivamente frontend, documentacion, `CHANGELOG_AI.md` y/o archivos unitarios comerciales `backend/tests/Solqaryn.Tests/PddddX*Tests.cs` cuyo contenido no declara `Category=Integration` ni hace uso conocido de base de datos.
- `scripts/security/ci-impact-routing.mjs`: cambios desconocidos, eliminaciones ambiguas, rutas de gobernanza, codigo productivo, migraciones, pruebas de integracion o referencias a DbContext/MySQL obligan a ejecutar MySQL; fallos del diff o del clasificador impiden la certificacion. Dispacho manual sin base ejecuta la suite completa.
- `project-scope-lock.yml`: ejecuta los casos positivos y negativos del clasificador; permanece con revision independiente requerida para cualquier cambio de workflows o rutas de `scripts/security/**`.

## Aseguramiento

Para cambios solo unitarios PddddX se exige de todas formas Core, seguridad multiempresa, prueba dirigida y Scope Lock exact-SHA. Los cambios de backend fuente, migraciones e integracion mantienen cobertura MySQL y, cuando aplican, Playwright/Quality. Los workflows integrales conservan ejecucion manual mediante `workflow_dispatch`.

QA de aceptacion de este cambio: autoprueba `CI_IMPACT_ROUTING_SELF_TEST=PASS`, `CI_IMPACT_MYSQL_INTEGRATION=REQUIRED` ante workflows y fuente backend, validaciones de CI del PR protegido y aprobacion independiente del SHA final. Probar en un PR futuro con solo prueba comercial que las suites ajenas no bloqueen.

El cambio no modifica ERP, autentificacion, RBAC, PII, Aiven, grants, Render, Vercel, QA/main/PROD ni rulesets. Reversion exclusiva mediante otro PR protegido y lectura posterior.
