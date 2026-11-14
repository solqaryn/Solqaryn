# SOLQARYN V5 — C11-03.M01.03: inventario verificable de código, contratos y evidencia Outbox

**Microtarea:** C11-03.M01.03, PLAN_MAESTRO fila 1660; **autor:** PRIMARY DEV 02 — JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Scope:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Fecha 2026-11-14.

## Resultado exigido y límites

M01.03 exige **leer contratos, código y evidencia disponibles sin asumir certificación** del lote C11-03 Outbox y eventos. Este inventario ancla archivos observables a blobs Git y separa implementación ya versionada, contratos parciales, pruebas declaradas y brechas que deben validarse antes de una arquitectura de eventos o relay. **No implementa ni activa eventos**, workers, proveedores, dispatcher, webhook, cron, llamadas entre Tasks, esquema SQL, secretos o producción. Los trabajos PENDIENTE de M03/M05 necesitan sus propios gates y CI.

**Preflight:** al reclamar, PLAN_MAESTRO!H1660=PENDIENTE, I1660 vacío; COLA!G1660=SI/O1660=SI; DAG_TECNICO!A1621=C11-03.M01.03, F1621=VALIDADO/G1621=SI, sujeto a readback vivo y exclusividad CAS. La regla DAG es una habilitación documental, no certificación de red, datos, IAM, A5 ni autorización QA/main/PROD. Preceden C11-03.M01.01 y .02, con objetivos y fronteras documentados; A5 propia nunca sustituye una revisión independiente.

## Inventario de fuentes reales verificadas mediante Git

| Fuente del repositorio | Blob SHA | Qué se puede afirmar y qué no |
| --- | --- | --- |
| `backend/src/Domain/Entities/MensajeOutbox.cs` | `891572626c19cd6cf8f522fca5ec876333286ffb` | Entidad con identidad empresarial e idempotente; no acredita persistencia productiva |
| `backend/src/Application/Services/MensajeOutboxService.cs` | `a00d353c474d7d7f785a58f443c66c5f12a542f7` | Servicio valida contexto tenant y stagea intención; no confirma por sí solo transacción |
| `backend/src/Application/Interfaces/IMensajeOutboxRepository.cs` | `a7dc258df9b1123dfe54ea85ba6d4dcc2a1eb478` | Contrato de Add, consultas, claim y transición; una interfaz no demuestra implementación SQL correcta |
| `backend/src/Application/Services/OutboxRetryProcessor.cs` | `542c0a655a3ee0698e3d80f1854d43903b6470fa` | Lógica de batch tenant-bound, recuperaciones y retries; sin scheduler ni HostedService propio |
| `backend/src/Infrastructure/Repositories/MensajeOutboxRepository.cs` | `056d5aa380b53ed697ddcb6852df55394e6544a7` | Repositorio EF con actualizaciones condicionales; fuente ≠ MySQL probado en este documento |
| `backend/src/API/Middleware/CorrelationIdMiddleware.cs` | `3b76dca41bb4bc0b44f89c2a4664c79907f9f242` | Sanitiza X-Correlation-ID; el header no concede capacidad ni pertenencia |
| `backend/tests/Solqaryn.Tests/N72DOutboxMySqlConcurrencyTests.cs` | `ebf6fd4861c55145dc40021152bf1c73ff4edce4` | Fuente xUnit de integración marcada Integration; no se ejecutó en esta microtarea |
| `backend/tests/Solqaryn.Tests/N72DOutboxRepositoryClaimTests.cs` | `6ce44c3a74eb1fb9ce5297b8b0274a0036dbb6ea` | Ensayos codificados con SQLite; no sustituyen prueba MySQL exact SHA |
| `backend/tests/Solqaryn.Tests/N72DOutboxRetryProcessorTests.cs` | `37525d79c6db2077c6155494a5d21fe987a8038e` | Fuente con FakeRepository, sin dispatcher de red |
| `backend/tests/Solqaryn.Tests/N74DOutboxIdempotentReplayTests.cs` | `c58d94828b1b7e37bc601dcf0938caa2e4a285c3` | Casos xUnit de repetición, no garantía universal de exactly-once |
| `backend/tests/Solqaryn.Tests/P2012DOutboxLoteCruzadoTests.cs` | `2140d2a9e87b6000298555bf577d11c07c939492` | Prueba sintética de lote A/B sin HTTP, red o MySQL |
| `backend/tests/Solqaryn.Tests/N72FOutboxSecurityAuditTests.cs` | `65dba47ca91ee0800bdbd119f8454bf3d39e40f4` | Escenarios de auditoría con datos allow-listed, sin certificación productiva |
| `docs/v5/c00-01-m02-05-events-idempotency.md` | `d8abb4ec291a3cc4afcd5b93b11b6c3bcbfa56dd` | Contrato documental previo; no es una ejecución |
| `docs/v5/c11-03-m01-01-alcance-no-regresion-outbox-eventos.md` | `a058779633c30a155ac8df5dacb8159e979876df` | Objetivos NR01–NR16 y pruebas futuras NO_EJECUTADAS |
| `docs/v5/c11-03-m01-02-actores-interfaces-fronteras-outbox.md` | `f66bb8296257ed72bb7a69b67704f99b9f0360b1` | Actores A01–A14, interfaces I/O y fronteras F01–F12 documentales |

**Alcance de inspección:** estos 15 blobs se obtuvieron de GitHub en `dev`, no del estado desplegado de API, Aiven, MySQL ni de un proveedor. La presencia de pruebas xUnit NO acredita PASS; sus versiones y resultado exact SHA deberán consultarse cuando corresponda un cambio ejecutable. La ausencia de un scheduler en `OutboxRetryProcessor.cs` tampoco demuestra ausencia universal de otros jobs en el repositorio.

## Brechas de evidencia G01–G14, NO BUGS CONFIRMADOS

| Brecha | Evidencia causal pendiente antes de implementación/certificación |
| --- | --- |
| G01 | Verificar atomicidad UoW negocio + outbox en EF/MySQL efímero |
| G02 | Medir unicidad de claim bajo dos consumidores reales y mismo intento |
| G03 | Confirmar rechazo de lote contaminado A/B antes de primer efecto |
| G04 | Probar reentrega tras crash entre dispatch y confirmación sin duplicar operación |
| G05 | Probar coincidencia y conflicto de clave idempotente entre requests |
| G06 | Probar stale claims, backoff, jitter, reintentos máximos y dead-letter |
| G07 | Confirmar correlación acotada sin tomar X-Correlation-ID como autorización |
| G08 | Comprobar redacción de excepciones, payload, JWT y PII en auditoría |
| G09 | Revisar costos, cuotas y permiso del eventual proveedor externo autorizado |
| G10 | Especificar semántica de orden por agregado, cancelación y reintentos |
| G11 | Identificar adaptador/host de dispatch futuro sin activarlo ahora |
| G12 | Verificar compatibilidad de contrato con consumidores y transacciones existentes |
| G13 | Ejecutar pruebas exact SHA de .NET 10/EF Core/MySQL/tenant A/B si hay código |
| G14 | Obtener revisión A5 independiente antes de liberar gates terminales o fases |

Ninguna brecha se presenta como error comprobado; algunas pueden estar cubiertas parcialmente por otros tests que requerirían evaluación propia. Tampoco existe evidencia aquí de una cola externa o de proveedor real activo.

## Oráculos de aceptación FUTUROS_NO_EJECUTADOS

**Positivos P01–P08:** P01 creación UoW atómica; P02 duplicado con misma clave sin doble efecto; P03 claim A exclusivo; P04 claim B independiente; P05 reintento por fallo parcial; P06 dead-letter tras límite; P07 correlación segura; P08 recuperación y rollback sin borrar historial.

**Negativos N01–N14:** N01 empresa B en batch A; N02 EmpresaId inyectado; N03 idempotencia con payload distinto; N04 intentos duplicados concurrentes; N05 stale claim sin límite; N06 excepción de proveedor contiene token; N07 payload sensible logueado; N08 soporte read-only despacha; N09 JWT revocado; N10 fallo SQL parcial; N11 repetición cobra dos veces; N12 evento fuera de orden muta inventario; N13 `SKIPPED` o `IN_PROGRESS` tratado como PASS; N14 dispatcher de Tasks activado por documento.

**Todos FUTUROS_NO_EJECUTADOS.** No se ejecutó dotnet test, MySQL, Playwright, HTTP, red, workers ni proveedores para esta microtarea documental. Una implementación futura de M03 deberá realizar pruebas causales y A0–A5, sin depender de los resultados descritos aquí.

## Impacto, auditorías y rollback

**Impacto:** exclusivamente este Markdown y una sección de CHANGELOG_AI.md, sin modificar código .NET/Angular, data model, DI, endpoints, SDK, infraestructura, secretos, telemetría de negocio o scheduling. **A0:** Skill/HEAD/plan/cola/DAG y separación de propietarios. **A1:** inventario con blobs y brechas auditables. **A2:** revisión documental JWT/RBAC, tenant A/B, datos personales, idempotencia y autorización de soporte. **A3:** contraste entre Domain, Application, Infrastructure, API, fuentes de test y contratos; integración real NO_APLICA a Markdown. **A4:** CAS fast-forward exact HEAD, comprobar diff, `verify-solqaryn-project-scope` y control V5 CHANGELOG `completed/success` sobre SHA publicado; readback PLAN/COLA/BITACORA. **A5:** otra Task Javier con ID diferente del autor.

**Rollback:** commit `git revert` normal con revalidación de HEAD; jamás force-push/reset o reescritura histórica de BITACORA. No se autoriza publicar en QA/main/PROD ni activar eventos o migraciones.
