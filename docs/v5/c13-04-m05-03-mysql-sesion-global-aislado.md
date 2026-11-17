# SOLQARYN V19 — C13-04.M05.03 integración real de sesión global en MySQL efímero

Repositorio `solqaryn/Solqaryn` | Rama `dev` | PROJECT_SCOPE_LOCK=STRICT
Responsable: DEV05, TASK_ID `6adc2e55f47c81919f6374e561687076`.
Plan institucional fila 2008. Riesgo R2 (seguridad y persistencia), candado V17 `2000002008`.

## A0 — Alcance y límites

Se añade solo una prueba xUnit y workflow de integración GitHub-hosted. `PlataformaScopeService`, `PlataformaSesionController` y `AppDbContext` son código existente: **no** se cambian Domain, Application, Infrastructure, API, Angular, DTO, schema, migraciones ni configuración del SaaS. El servicio de prueba usa **Oracle EF Core / MySQL 8.4 real efímero local** (`127.0.0.1`), con base aleatoria creada y eliminada; ningún acceso a Aiven, MySQL de negocio, infraestructura de pago o proveedores reales.

## A1 / A2 — Oráculos reales y adversariales

Fixture persistida en MySQL con un administrador global sin membresías empresariales, administrador comercial solo de empresa A y dos empresas A/B. Una nueva conexión revalida el grant de plataforma y audita la respuesta positiva; los intentos empresariales con cabeceras A y B forjadas deben denegarse; la revocación persistida del rol global debe denegar la solicitud siguiente en otra conexión; identidad HTTP no autenticada debe denegarse sin auditar. Auditoría del endpoint no debe atribuir EmpresaId en ningún caso. La autenticación HTTP de los tests utiliza `ClaimsIdentity` **sintética** y un `ICurrentUserService` de fixture: no acredita firma criptográfica JWT, middleware ASP.NET Core hospedado, IDOR de endpoints arbitrarios ni autorización de todo el SaaS.

## A3 — Integración

Workflow `v19-c13-04-m05-03-mysql-session.yml`: GitHub-hosted Ubuntu, imagen `mysql:8.4`, versión de servidor comprobada, .NET SDK 10.0.401, `dotnet test --configuration Release --filter FullyQualifiedName~C1304M0503MySqlPlataformaSesionIntegrationTests&Category=Integration`, reporte TRX con `executed >= 1`, `passed >= 1`, `failed = 0` y `notExecuted = 0`. La versión Oracle EF y el bootstrap real se acreditan en logs del job; **sin job SUCCESS/SHAs equivalentes no se declara A3 PASS**.

## A4 / A5 y continuidad

Verificar diff y SHA publicados en dev, check-runs obligatorios `completed/success` del SHA o descendiente con blobs y dependencias idénticos, Scope Lock, procedencia V5 y readback PLAN/COLA/BITACORA. Si GitHub Actions sigue QUEUED/IN_PROGRESS conservar VALIDANDO y registrar RUN/SHA, sin fingir PASS. A5 corresponde a otra Task Javier distinta de DEV05, sin firma propia. No desbloquear C13-04.M05.06 ni QA/main/PROD antes de sus requisitos reales. Rollback: `git revert` normal del commit en dev, sin force-push y sin borrar BITACORA.
