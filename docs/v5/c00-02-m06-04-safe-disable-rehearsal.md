# SOLQARYN V5 — C00-02.M06.04: ensayo real y acotado de desactivación no destructiva

**Resultado exigido:** ensayar **desactivación** (alternativa válida al rollback) sin borrar datos legítimos de C00-02. El sistema implementado en `dev` contiene `V5WorkItemMemoryAdapter` con gates de claim y snapshots **en memoria**, pero no tiene proveedor Sheets CAS/lease distribuido ni persistencia MySQL V5. Por seguridad, este ensayo actúa sólo sobre instancias de memoria creadas por xUnit, con el **código real** del adaptador; no se ejecuta desactivación de las seis Tasks físicas, no se cambia configuración del proveedor ni se toca ningún dato real.

**Base:** `c58e1fb6334c8717fb10faa0889a20603e081fbb`; alcance: nueva suite `backend/tests/Solqaryn.Tests/Application/V5C0002M0604SafeDisableRehearsalTests.cs` y este documento + CHANGELOG. Dependencias: `PLAN_MAESTRO!H62:H67=LISTO` (M05 6/6), `COLA!G/O=SI` y `DAG_TECNICO!F=VALIDADO/G=SI` para M06.04 al preflight; la elegibilidad no sustituye prueba. Fuente única [V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit); control institucional [Sheet](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit).

## Contrato de desactivación y datos preservados

`V5WorkItemMemoryAdapter` ofrece `Claim(id,actor,token,dependencies,developmentEnabled,dagValidated,dagGateOpen)`. Un gate cerrado **rechaza un nuevo claim** sin escribir snapshot. La suite emplea dos registros legítimos simulados mediante la clase real: `C00-02.M05.03` en `LISTO` con propietario Javier :30, y `C00-02.M06.04` en `PENDIENTE` sin owner. La desactivación se ejecuta cerrando cada una de las cuatro puertas de entrada individualmente. No se presenta como rollback de base de datos ni cancelación de trabajos ya iniciados.

| Prueba ejecutable dirigida | Aceptación positiva / negativa |
| --- | --- |
| `ClosingAnyDevelopmentGatePreservesLegitimateStoredSnapshots` (4 casos xUnit) | **Negativo:** gates de dependencia, habilitación, DAG y apertura cerrados devuelven `DEPENDENCY_BLOCKED`; ningún cambio de `PENDIENTE`, owner nulo ni de snapshot `LISTO` previo |
| `ReenablingGateAllowsOnlyNewAuthorizedClaimAndKeepsCompletedItem` | **Positivo:** tras restablecer un gate en un request nuevo, el actor permitido recibe `EN_PROGRESO`; replay mismo token es idempotente y `LISTO` previo permanece idéntico |
| `DeactivationCannotEraseCompletedItemOrGrantForeignOwnership` | **Negativo:** actor diferente no puede reclamar con gate cerrado ni desplazar ownership tras reabrir; snapshot `LISTO` preexistente se preserva |

**Alcance de la prueba:** xUnit es real y usa el adaptador del producto, no un mock del método bajo prueba. Los snapshots iniciales son fixtures de memoria; no representan archivos, registros legales ni información persistida de clientes. El test no desactiva directamente un claim **ya EN_PROGRESO** porque las puertas actuales sólo se evalúan en **nuevos claims**; la cancelación de trabajo activo requiere contrato y evidencia por separado.

## Análisis de riesgo y clasificación técnica

- `requiere_dotnet=SI`: compilar .NET 10 y ejecutar xUnit real en GitHub Actions sobre SHA publicado; son pruebas funcionales del código existente.
- `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO` para este **diff únicamente de tests/documentos**: no modifica frontend, proveedor MySQL, migraciones ni workers. Si se ensaya rollback de persistencia en futuro, esos controles deberán activarse para ese scope.
- `requiere_security=SI`: garantizar rechazo de actor ajeno, ausencia de modificación de owner y preservación de estado `LISTO`, sin cambiar JWT/RBAC/tenant. `requiere_scope_lock=SI` y check de procedencia V5 sobre el SHA del commit.
- **A0:** revisar código xUnit, ausencia de secretos, API real y diff CHANGELOG por encabezado completo.
- **A1:** seis casos xUnit (4 Theory + 2 Facts): desactivación y recuperación, positivos/negativos reales del adaptador.
- **A2:** test de propiedad ajena y no borrado; no declarar test de JWT/MySQL/tenancy live.
- **A3:** compilación, regresión xUnit .NET y suite seguridad pertinente de GitHub Actions; sin usar checks cancelados o de HEAD previo.
- **A4:** publicar commit directo fast-forward en `dev`; verificar SHA exacto, blobs, check-runs Scope Lock, xUnit/backend, seguridad y V5 CHANGELOG, más readback PLAN/COLA/BITACORA. Mantener `VALIDANDO` hasta que todo gate requerido finalice PASS.
- **A5:** otra de las cinco Tasks Javier distinta de autora verifica SHA y pruebas positivas/negativas con independencia real; A5_PENDIENTE no es aprobación de QA/main/PROD.

**Rollback:** revertir el commit de **suite/documentos** mediante `git revert` normal de `dev`; no reescribir historia ni modificar BITACORA, cuentas, secretos o datos. No se ejecuta `git revert` real sobre `dev` aquí porque destruiría el resultado entregado; la vía de reversión queda explícita, no se finge que se ha probado con datos reales.

**Límites pendientes:** sin CAS distribuido de Sheets, sin MySQL 8.4 durable, sin cancelación de inflight jobs y sin deactivación de scheduler físico. El resultado que puede cerrarse es el **ensayo de desactivación del proveedor realmente implementado en memoria**. M06.06 conserva gate terminal propio y no se libera por este experimento.
