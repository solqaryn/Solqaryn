# SOLQARYN - CI por impacto en DEV

## Controles que no se retiran

Siguen obligatorios `verify-solqaryn-project-scope`, `SOLQARYN - Seguridad multiempresa`, `SOLQARYN - Build y pruebas backend` y el check dirigido comercial aplicable. El verificador de certificacion automatica conserva todos los controles requeridos, el check dirigido, la revision independiente y el rechazo de errores ya observados. Solo un conjunto conocido de checks extensos pendientes y causalmente ajenos al diff puede diferirse en la via rapida comercial, bajo prueba fail-closed.

## Enrutamiento causal

- `catalogos-aceptacion.yml`: ejecuta Playwright ante cambios frontend, backend operativo o sus otros disparadores declarados. Excluye exclusivamente cambios dentro de `backend/tests/**` del disparo de esta suite.
- `priority4-quality.yml`: no se activa por `backend/tests/**` como unico impacto; mantiene compilacion, lint y regresiones `Category!=Integration`. La integracion MySQL pertenece al job de migraciones de `dev-ci.yml`.
- `dev-ci.yml`: un preflight `impact` ejecuta el clasificador en Node 24, lo prueba y compara los SHA exactos de base y HEAD; el job MySQL solo se omite cuando el diff contiene exclusivamente frontend, documentacion, `CHANGELOG_AI.md` y/o archivos unitarios comerciales `backend/tests/Solqaryn.Tests/PddddX*Tests.cs` cuyo contenido no declara `Category=Integration` ni hace uso conocido de base de datos.
- `scripts/security/ci-impact-routing.mjs`: cambios desconocidos, eliminaciones ambiguas, rutas de gobernanza, codigo productivo, migraciones, pruebas de integracion o referencias a DbContext/MySQL obligan a ejecutar MySQL; fallos del diff o del clasificador impiden la certificacion. Dispacho manual sin base ejecuta la suite completa.
- `solqaryn-commercial-directed.yml`: solo se ejecuta para ramas comerciales deterministas `work/solqaryn-p*`; los PR de gobierno no disparan una prueba comercial ficticia. Scope Lock sigue rechazando cualquier PR comercial en rama incorrecta.
- `project-scope-lock.yml`: ejecuta los casos positivos y negativos del clasificador; permanece con revision independiente requerida para cualquier cambio de workflows o rutas de `scripts/security/**`.

## Fast lane causal de certificacion (noviembre 2026)

- **Obligatorios sin excepciones**: Scope Lock exact-HEAD, `SOLQARYN - Seguridad multiempresa`, `SOLQARYN - Build y pruebas backend`, y `SOLQARYN - Directed commercial scope` para PR comerciales. Revisiones independientes siguen siendo requeridas segun autor y cambio de gobierno.
- El verificador puede finalizar sin esperar exclusivamente checks opcionales extensos de la lista `BROAD_OPTIONAL` que aun esten `queued`/`in_progress` y sean de GitHub Actions oficial, cuando el diff sea solamente `CHANGELOG_AI.md` + Markdown no protegido bajo `docs/**`, o tests unitarios comerciales PddddX exactos con clasificacion MySQL confiable PASS y job MySQL explicitamente SKIPPED.
- Los cambios de workflows, scripts, backend/frontend productivo, migraciones, gobernanza, secretos, infraestructura, borrados/renombrados y archivos de clasificacion desconocida no entran en fast lane. Documentos de contratos de produccion, rendimiento y modernizacion tampoco.
- **Nunca se ignora un check que ya termino con failure o cancelled**, aunque fuese opcional. Los checks pendientes desconocidos tampoco se omiten. Solo se difiere la espera; las suites siguen ejecutandose en GitHub y mantienen su resultado visible.
- Logs `AUTO_CERT_DEFERRED_UNRELATED_CHECKS` auditan los checks diferidos. `shouldWaitForActiveChecks` espera solo jobs realmente relevantes del SHA evaluado; un job ajeno no puede enmascarar un check dirigido ausente.
- El fast lane no autoriza fusiones con Core incompleta ni cierres `LISTO` prematuros. Las Tasks pausadas siguen pausadas; no cambia la autorizacion individual ni se activa ninguna Task. PR de instalacion exige review independiente, CI, merge protegido y readback.
- Reversion: PR protegido que revierta el clasificador en el verificador y esta politica, sin modificar rulesets ni entornos.

## Aseguramiento

Para cambios solo unitarios PddddX se exige de todas formas Core, seguridad multiempresa, prueba dirigida y Scope Lock exact-SHA. Los cambios de backend fuente, migraciones e integracion mantienen cobertura MySQL y, cuando aplican, Playwright/Quality. Los workflows integrales conservan ejecucion manual mediante `workflow_dispatch`.

QA de aceptacion de este cambio: autoprueba `CI_IMPACT_ROUTING_SELF_TEST=PASS`, `CI_IMPACT_MYSQL_INTEGRATION=REQUIRED` ante workflows y fuente backend, validaciones de CI del PR protegido y aprobacion independiente del SHA final. Probar en un PR futuro con solo prueba comercial que las suites ajenas no bloqueen.

El cambio no modifica ERP, autentificacion, RBAC, PII, Aiven, grants, Render, Vercel, QA/main/PROD ni rulesets. Reversion exclusiva mediante otro PR protegido y lectura posterior.
