# P1-014.E — Regresión positiva de alta guiada (DEV)

- Fuente única: Plan Comercial Inicial, P1-014.E, orden 66, PLAN_MAESTRO fila 67. Depende de P1-014.D = LISTO.
- Task ejecutora Javier :00: `6adbb9fff1548191a1bd5b30d9e9984a`, cuenta GitHub `jmejia31`; rama `work/solqaryn-p1-014-e`, basada en `dev@6a0fdb22a158247ded6255a92f9e03108369a8ee`.
- Requisito: prueba positiva dirigida del asistente de tres pasos; no basta el build Angular ni la prueba negativa previa P1-014.D.
- Angular `P1014E`: con autorización de directorio positiva y administrador nuevo válido, el botón de confirmación está habilitado, se envía **una** solicitud al servicio adecuado, no se utiliza la vía de administrador existente, y el formulario vuelve al primer paso, limpia el nombre y la clave de prueba sin perder el identificador del administrador creado.
- Backend `P1014E`: reutiliza el fixture integrado `P1014A_ConfirmacionGuiadaNoEludeGatesNiCreaDatosSinAutorizacion` para la transición autorizada y sus efectos en EF InMemory (Empresa, ConfigEmpresa y UsuarioEmpresa). El caso original incluye también rechazo con flags desactivados. No constituye HTTP/JWT/MySQL DEV vivo ni prueba de valores remotos de flags.
- Pruebas dirigidas: `dotnet test backend/tests/Solqaryn.Tests/Solqaryn.Tests.csproj --filter FullyQualifiedName~P1014E` y `cd frontend && npm run test:unit -- --include src/app/features/plataforma/plataforma-gestion-mvp.component.spec.ts`; exigir ejecución y evidencia CI exact-SHA antes de TECH_REVIEW/merge/LISTO.
- Estado de entrega inicial: NO_TEST_LOCAL_EXECUTED en este runtime; no se atribuye PASS. CI GitHub Actions del HEAD nuevo debe verificarse sobre su SHA exacto.
- Sin cambios en API, servicios, autenticación, RBAC, bases de datos, migraciones, flags, secretos ni entornos QA/main/PROD. La UI no sustituye autorización del backend.
- Rollback: revertir únicamente esta prueba Angular, caso xUnit, acta y changelog por PR protegido conservando historial ajeno. Revisión final por un TASK_ID distinto del autor.
