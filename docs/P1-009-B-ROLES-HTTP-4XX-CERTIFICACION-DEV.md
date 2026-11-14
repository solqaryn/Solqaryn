# P1-009.B — Certificación HTTP negativa de plantillas y unicidad de roles (DEV)

## Alcance y autoridad
- Fuente ejecutable: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, documento institucional 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA.
- PLAN_MAESTRO!43 y COLA!43: P1-009.B; depende de P1-009.A=LISTO, PR #3841 fusionado y readback BITACORA!516.
- Responsable físico exclusivo: SOLQARYN PRIMARY DEV 04 JAVIER :30, TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09, GitHub jmejia31; rama work/solqaryn-p1-009-b; base de trabajo dev@edc7be2c36abd903318dab9cc96489a668207817.
- PROJECT_SCOPE_LOCK=STRICT, WIP global máximo 6, máximo dos scopes propios y un editor activo. Las otras Tasks Javier no confieren independencia formal.
- Resultado requerido: «Rol ajeno/Global asignado a empresa: 4xx» con rechazo antes de escribir. No modificar capas de autorización para conseguir un test artificialmente verde.

## Reutilización causal
1. El PR #3669 y la certificación P1-009.A/#3841 ya cubren reglas de Rol.EsAsignableAEmpresa, unicidad final EF por tenant y nombres normalizados. P1-009.B agrega evidencia de transporte HTTP que P1-009.A expresamente no certificaba.
2. La ruta real POST /roles/{id}/duplicar utiliza RolesController, RequierePermiso, TenantPermissionContext, RolService y ExceptionHandlingMiddleware. BusinessRuleException se transforma en HTTP 400 en el middleware vigente.
3. El nuevo test P1009BRoleHttpIsolationCertificationTests usa TestServer con autenticación JWT firmada solo para pruebas, empresa A=11 y permiso Duplicar A verificado por claim. El repositorio se simula deliberadamente para presentar un rol de B, uno global de Plataforma y uno huérfano no plantilla, sin depender de datos externos.
4. Para cada caso exige HTTP 400 con mensaje causal «No se puede duplicar», una lectura exacta del rol, comprobación de permiso y cero AddAsync/SaveChangesAsync. De este modo un rechazo de model binding o un HTTP 500 no aprobarían accidentalmente.
5. Regresión relacionada existente: P1001ARolesHttpJwtIsolationTests comprueba GET A/B/anónimo; RolServiceTenantOwnershipTests cubre servicio y no persistencia de roles no autorizados; P1009ARoleTemplateUniquenessTests cubre la semántica y el modelo EF final.

## Límites, riesgo, pruebas y cierre
- Fixture con repositorio mock: valida ruta HTTP, auth JWT local, filtro, controlador, servicio y middleware, pero NO demuestra memberships reales de MySQL, permisos productivos, migración, índice físico Aiven ni HTTP live de DEV. Esas evidencias requieren diagnóstico independiente cuando sea exigible. No confundir test unitario con aceptación live.
- Se modifica exclusivamente un test xUnit, esta acta y CHANGELOG_AI.md. No se alteran API, RBAC, Domain, Application, Infrastructure, persistencia, infraestructura, secretos, QA, main ni PROD.
- Prueba dirigida exigida: dotnet test backend/tests/Solqaryn.Tests/Solqaryn.Tests.csproj --configuration Release --filter FullyQualifiedName~P1009B. En CI, comprobar TRX real, SHA físico de PR, Scope Lock AUTO_CERT=PASS y todos los checks aplicables terminales SUCCESS/SKIPPED; revisar diferencias y mergeable frente al HEAD vivo.
- Si el flujo protegido admite merge comercial mediante AUTO_CERT=PASS confiable no se exige aprobación humana rutinaria; nunca afirmar APPROVED humano o autoaprobar PR propio.
- Cierre LISTO SOLO tras merge protegido a dev, CI postmerge aplicable terminal y readback PLAN_MAESTRO!H43, COLA!E43/G sucesoras y BITACORA con evento único, conservando fórmula COLA.
- Al preparar el changeset: trabajo EN_PROGRESO; aún NO acreditados tests, CI, merge ni cierre. Las evidencias CI posteriores deben anotarse con SHA, jobs y resultado real, sin reutilizar PASS de otra entrega.
- Rollback: revert exclusivamente de test/acta/changelog por PR protegido; no hay cambios de datos que revertir.
