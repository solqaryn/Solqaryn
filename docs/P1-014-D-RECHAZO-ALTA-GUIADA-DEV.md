# P1-014.D — Prueba negativa del asistente guiado de alta (DEV)

## Autorización y alcance
- Plan Comercial Inicial, P1-014.D, orden 62, PLAN_MAESTRO fila 63; dependencia P1-014.C = LISTO, COLA!G63 = SI.
- Ejecución nativa Javier :30, TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09, GitHub jmejia31. Routing developerFor('P1-014.D') de scripts/automation-review-routing.mjs en dev según contrato directo PR #3869.
- Se reutiliza exclusivamente la rama existente work/solqaryn-p1-014-d creada previamente sin cambios; ni cesión de otro trabajo activo ni preasignación de otro TASK_ID.

## Una condición negativa verificable
Cuando el formulario de tres pasos ha recopilado datos válidos, el servidor puede revocar la autorización empresarial antes de confirmar. La prueba P1014D simula HTTP 403 como respuesta de altaControlada y verifica: un solo intento, ningún alta por la ruta de administrador nuevo, nombre empresarial conservado, wizard en paso 3, señal de error visible y ausencia de operación en curso. Ninguna aserción presupone que Angular sustituya el gate de seguridad del backend.

- Código: frontend/src/app/features/plataforma/plataforma-gestion-mvp.component.spec.ts; prueba dirigida xUnit backend/tests/Solqaryn.Tests/API/Controllers/P1014DGuidedOnboardingUnauthorizedTests.cs, que reutiliza el caso negativo real de ForbidResult y cero escrituras Empresa/UsuarioEmpresa.
- CI scope-matched: dotnet test backend/tests/Solqaryn.Tests/Solqaryn.Tests.csproj --filter FullyQualifiedName~P1014D; exigir >=1 PASS y 0 FAIL en check comercial dirigido.
- Ejecución Angular sugerida: cd frontend && npm run test:unit -- --include src/app/features/plataforma/plataforma-gestion-mvp.component.spec.ts.
- Resultado de test local: NO_EJECUTADO en este runtime. CI exact-SHA, Scope Lock, revisión independiente, merge protegido y postmerge se verificarán antes de LISTO.
- Evidencia CI dirigida verificada en el SHA `fe215c7914d39d4435febc6164bb36e611949f67`: xUnit `P1014D` 1/1 PASS, 0 FAIL, job https://github.com/solqaryn/Solqaryn/actions/runs/42114226337. No hay evidencia de ejecución de este spec Angular; compilación/lint no sustituyen su test unitario.
- Incidencia CI de ese mismo SHA: `SOLQARYN Project Scope Lock` FAILURE (`SCOPE_DIRECTED_TEST_MISSING:P1-014.D`) aunque la prueba dirigida aprobó; GitHub registra dos checks homónimos `SOLQARYN - Directed commercial scope`, uno SKIPPED desde DRAFT y otro SUCCESS tras READY. El verificador vigente agrega por nombre y la colisión impide certificar. La sincronización no destructiva con HEAD dev y un nuevo SHA obligan a revalidar TODOS los gates; no se relajó ningún check.
- Estado posterior a sincronización: CI del nuevo SHA y prueba Angular específica PENDIENTES; no hay TECH_REVIEW_PASS, APPROVED ni LISTO.

- Es una prueba Angular con doble de API; no acredita JWT/HTTP/MySQL real, flags DEV, escritura productiva ni aislamiento de tenants por sí sola. No se cambian servicios productivos, API/DTO, migraciones, credenciales, QA/main/PROD o reglas de autorización.
- Rollback: revertir este único spec, esta acta y su changelog mediante PR protegido, preservando auditoría y commits ajenos.
