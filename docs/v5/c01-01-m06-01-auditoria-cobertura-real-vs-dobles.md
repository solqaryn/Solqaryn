# SOLQARYN V5 — C01-01.M06.01: auditoría de pruebas frente a alcance real

**Autor:** PRIMARY DEV 03 — JAVIER_CHATGPT (`6adc2e4bd8a48191ac7d8641ed95a1a3`). **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Autoridad:** Standard de Implementación de Automatizaciones V5 FINAL, `PLAN_MAESTRO!A176`; predecesores técnicos M05 y DAG calculado. Esta entrega es una **revisión documental estática**, no una nueva ejecución de tests, certificación runtime ni autorización de QA/PROD.

## Matriz de correspondencia entre pruebas y alcance

| Entrega previa | Prueba/artefacto real revisado | Qué acredita el código | Qué NO acredita |
| --- | --- | --- | --- |
| C01-01.M05.01 | `backend/tests/Solqaryn.Tests/Domain/IdentidadUniversalReglasTests.cs` | Reglas productivas de membresía, rol vigente, revocación, rol legacy, empresa y sucursal; aserciones negativas de tenant A/B. | JWT, controlador HTTP, persistencia ni petición autenticada completa. |
| C01-01.M05.02 | `backend/tests/Solqaryn.Tests/API/Controllers/TenantContextV5ContractTests.cs` | Invocación del controlador real: IDs inválidos, `ForbidResult`, `OkObjectResult` y DTO sin datos externos. | `ScopeStub` sustituye el servicio; no ejercita middleware de autenticación/JWT, DI HTTP, EF ni MySQL. |
| C01-01.M05.03 | `backend/tests/Solqaryn.Tests/C0101M0503MySqlTenantContextIntegrationTests.cs` | Test `Category=Integration` con `AppDbContext`, `UseMySQL`, usuario y membresías A/B persistidos, `ChangeTracker.Clear`, relectura y revocación. Usa BD aleatoria y `EnsureDeletedAsync`. | Solo corre si CI aporta `ConnectionStrings__DefaultConnection` de localhost/127.0.0.1. La presencia del test NO prueba que un job lo ejecutó; no cubre JWT ni un endpoint HTTP conectado a la BD. |
| C01-01.M05.04 | `backend/tests/Solqaryn.Tests/Application/V5C0101M0504PositiveAcceptanceTests.cs` | Servicio productivo Application de resolución positiva, empresa/sucursal y consultas repetidas. | La fuente de snapshots está en memoria; no autentica contra una BD ni representa un usuario HTTP real. |
| C01-01.M05.05 | `backend/tests/Solqaryn.Tests/Application/V5C0101M0505NegativeIsolationTests.cs` | Servicio productivo de identidad con denegación de empresa B, membresía ajena, revocación, cambio de rol y sucursal externa. | Fuente simulada: no prueba filtro EF, claims reales, autorización final en API ni explotación adversarial. |
| C01-01.M05.06 | `backend/tests/Solqaryn.Tests/Application/V5C0101M0506RegresionTests.cs`; `frontend/e2e/c01-01-m05-06-regresion.spec.ts` | 128 resoluciones A/B y conteo de lecturas, denegación cross-tenant, cancelación, revocación; dos casos Chromium de foco/reduced-motion y acceso anónimo a ruta privada. | `FuenteContada` es in-memory; el conteo no mide p95, carga, throughput ni consumo en producción; Playwright no cubre login JWT autorizado ni flujo completo API/MySQL. |

## Veredicto de revisión: cobertura adecuada dentro de fronteras explícitas

- **A0, alcance y pruebas reales:** se inspeccionaron los seis artefactos citados sobre el árbol de `dev`; existen aserciones que ejercitan Domain, Application y controlador, no solo documentos o afirmaciones. El caso MySQL usa EF y un servicio local efímero, condicionado a los logs de CI para certificar ejecución.
- **A1, positivos/negativos:** la cobertura de reglas de pertenencia y revocación es pertinente al contrato actual; los dobles `ScopeStub` y las fuentes en memoria son legítimos **solo** para pruebas unitarias/contratos acotados, nunca como prueba de integración completa.
- **A2, seguridad:** el control de aislamiento tenant A/B tiene rechazos negativos en código. **No certificar** JWT HTTP end-to-end, IDOR por rutas reales, políticas RBAC con middleware, filtros EF globales ni carga maliciosa fuera de los escenarios cubiertos.
- **A3, integración:** hay prueba física MySQL aislada con `Trait(Category=Integration)`, pero su resultado debe leerse en job GitHub-hosted con MySQL 8.4 y filtro que incluya la categoría; un build verde o tests `Category!=Integration` no la sustituyen. Esta auditoría documental no ejecuta MySQL, Angular ni navegador.
- **A4, publicación:** registrar SHA exacto del documento, blobs, diff, Scope Lock y procedencia V5 una vez publicados y finalizados; un check pendiente, cancelado, omitido o fallido no es PASS. El A5 de este documento será exclusivamente independiente por otra Task Javier.

## Brechas que siguen abiertas

1. **P1 de alcance**, no defecto P0 demostrado: falta una regresión integrada de endpoint autenticado + middleware JWT/RBAC + snapshot/EF real para distinguir autorización del controlador de un `ScopeStub`.
2. **P1 de evidencia**: la ejecución de integración MySQL 8.4 debe relacionarse con logs de tests `Category=Integration` del SHA causal, no con el simple nombre de un workflow.
3. **P1 de alcance**: rendimiento perceptible y accesibilidad total requieren medición p95 y auditoría WCAG más allá de conteos de fuente y dos escenarios Chromium. La UI no concede permisos.
4. **Riesgo residual**: cambios posteriores a Domain/Application/Infrastructure, rutas o configuración CI exigen revalidación de equivalencia causal antes de reutilizar resultados.

**Decisión:** M06.01 puede entregar **auditoría documental acotada** con estas reservas; no convierte huecos de integración/seguridad en PASS. No se cambian endpoints, seguridad, datos, migraciones, servicios, secretos, gates DAG, webhooks, QA/main/PROD. Reversión: `git revert` ordinario del commit documental, preservando BITACORA y estados históricos.
