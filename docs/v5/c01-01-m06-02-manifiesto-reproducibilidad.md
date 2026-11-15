# SOLQARYN V5 — C01-01.M06.02: manifiesto reproducible de versiones y evidencias

**Unidad:** `C01-01.M06.02`, PLAN_MAESTRO fila 177, DAG_TECNICO fila 197. **Responsable:** PRIMARY DEV 03, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. Repositorio exclusivo `solqaryn/Solqaryn@dev`, Scope Lock STRICT. Autoridad: Standard de Implementación de Automatizaciones V5 FINAL. Este manifiesto es documental y **no** vuelve a ejecutar ni certifica pruebas históricas de otros autores.

## Inventario verificable del alcance C01-01.M05 y auditoría M06.01

**HEAD base leído para estos blobs:** `b00558af0d83f7237bb093af0abca2763b1e58ce`. Cada SHA de origen identifica el commit de la entrega; el blob identifica el contenido **actual de esa ruta**, no prueba ejecución de CI. Los registros anteriores de A0–A4 se conservan por separado en PLAN_MAESTRO/BITACORA.

| Unidad | Commit de publicación origen | Blob verificado en HEAD base | Ruta | Evidencia y límite |
| --- | --- | --- | --- | --- |
| `C01-01.M05.01` | `29f0b9b9001a595db9f3bfc8b2ac99e9e6d89ce9` | `e62ed4ef7c1fe4b39f27c43bb4853eeba32a2d17` | `backend/tests/Solqaryn.Tests/Domain/IdentidadUniversalReglasTests.cs` | Domain real: reglas y fallos tenant A/B |
| `C01-01.M05.02` | `7a1c78a733d1de1e72c7433f4558903019e1f96f` | `018abf7b57185b98e7fb0d8b5cbb65935eeb37bc` | `backend/tests/Solqaryn.Tests/API/Controllers/TenantContextV5ContractTests.cs` | Controller real con ScopeStub; no JWT HTTP |
| `C01-01.M05.03` | `11c6e30b4512b47e6b22f8a17c9fd03bbebac883` | `8d9bc82d42ffcab202f46ad3c501564cd71b5980` | `backend/tests/Solqaryn.Tests/C0101M0503MySqlTenantContextIntegrationTests.cs` | MySQL EF local Category=Integration |
| `C01-01.M05.04` | `8f005c593a4b76e62cffc88ea3f6215bfbf832f0` | `39fcec70e590434ba6574164669aecd901f4db04` | `backend/tests/Solqaryn.Tests/Application/V5C0101M0504PositiveAcceptanceTests.cs` | Application real + fuente in-memory |
| `C01-01.M05.05` | `71eac84d8eee2e23731d0504ac4d371fb85b1c7b` | `c512771ee2580c861b1888c24fd3d03665ca7701` | `backend/tests/Solqaryn.Tests/Application/V5C0101M0505NegativeIsolationTests.cs` | Application real + negativos in-memory |
| `C01-01.M05.06` | `38a7798cddcec6198ed8125246cc7c97347bd897` | `92587b9f987efa95cab2dcb5ed6354a9a13e75e5` | `backend/tests/Solqaryn.Tests/Application/V5C0101M0506RegresionTests.cs` | xUnit regresión; conteos I/O ≠ p95 |
| `C01-01.M05.06` | `38a7798cddcec6198ed8125246cc7c97347bd897` | `2247e620a8910fe54f01c1247f798d3cc7e71c76` | `frontend/e2e/c01-01-m05-06-regresion.spec.ts` | Chromium UI guard; no HTTP JWT real |
| `C01-01.M06.01` | `b00558af0d83f7237bb093af0abca2763b1e58ce` | `49619ba2996d29fd97c0e4e2d98b19428b1c710c` | `docs/v5/c01-01-m06-01-auditoria-cobertura-real-vs-dobles.md` | Auditoría documental de 6 pruebas y brechas |

El commit de esta unidad actualizará exclusivamente este manifiesto y `CHANGELOG_AI.md`. Su SHA y esos dos blobs se documentarán en BITACORA tras publicación y check-run exacto. No se alteran artefactos de M05 ni M06.01.

## Reproducción dirigida y criterio de aceptación (comandos de referencia, NO ejecutados en esta auditoría)

```bash
# dotnet 10 en checkout exacto y dependencias restauradas.
dotnet test backend/Solqaryn.sln --configuration Release --filter "FullyQualifiedName~IdentidadUniversalReglasTests|FullyQualifiedName~TenantContextV5ContractTests|FullyQualifiedName~V5C0101M0504PositiveAcceptanceTests|FullyQualifiedName~V5C0101M0505NegativeIsolationTests|FullyQualifiedName~V5C0101M0506RegresionTests"
# Requiere MySQL 8.4 efímero local y ConnectionStrings__DefaultConnection segura.
dotnet test backend/Solqaryn.sln --configuration Release --filter "Category=Integration&FullyQualifiedName~C0101M0503MySqlTenantContextIntegrationTests"
# Chromium con dependencias frontend y browsers instalados por CI, sin credenciales reales.
cd frontend && npx playwright test e2e/c01-01-m05-06-regresion.spec.ts
```

La reproducción es válida **solo** cuando los jobs realmente ejecutan esos filtros, reportan tests aprobados (cero fallos), identifican su commit/SHA, entorno MySQL efímero y logs; un build exitoso, un runner sin MySQL o un filtro `Category!=Integration` no acreditan integración. La fuente MySQL rechaza servidores distintos de `localhost`/`127.0.0.1` y elimina únicamente la base aleatoria de pruebas. El navegador solo comprueba rutas/foco/reduced-motion, no un flujo autenticado end-to-end. Para cada SHA publicado, Scope Lock y procedencia V5 deben terminar `completed/success`.

## Resultado reproducible y fronteras de seguridad

- **Positivo demostrado en código:** reglas productivas Domain/Application, controlador invocado, escenarios tenant A/B y persistencia EF local condicionada al job; `M06.01` trazó la distinción entre cobertura unitaria y cobertura integrada.
- **Negativo demostrado en código:** rechazo de membresía revocada, cross-tenant, usuario/rol no vigente, sucursal ajena, DTO sin filtración y cancelación previa a fuente. Los stubs de controller y snapshots in-memory prueban interfaces concretas, no red real.
- **No acreditado por este manifiesto:** ejecución nueva de xUnit/Playwright/MySQL, token JWT firmado y validado end-to-end, middleware RBAC con HTTP real, latencia p95, WCAG completo, QA/PROD o migraciones. Ninguno pasa implícitamente por este documento.
- **A0–A3 de la unidad:** exactitud de inventario de archivos/blobs, reconstrucción de comandos y límites por tecnología, seguridad tenant sin otorgar acceso y clasificación documental `NO_APLICA` de ejecución runtime nueva. **A4** queda sujeto a commit/diff exactos y checks obligatorios de este nuevo documento; **A5** exige otra Task Javier independiente.
- **Rollback:** `git revert` convencional del commit individual, sin reescribir historia ni tocar datos/servicios. Ningún gate DAG/COLA se establece manualmente.

**Fuente de referencias:** árboles GitHub en el HEAD base, PLAN_MAESTRO/COLA/DAG y V5 FINAL vigente; los SHA históricos son identificadores de entregas, no autorizan reutilizar checks sin comprobar blobs y dependencias.
