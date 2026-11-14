# C00-03.M01.05 — Criterios de aceptación de la matriz integrada V5

**Microtarea:** `C00-03.M01.05` — Definir aceptación funcional y técnica observable. **Unidad:** C00-03 Matriz integrada de alcance, M01 preparación. **Fuentes únicas:** `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` V5 FINAL (Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`) y Sheet institucional `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. **Identidad:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`, ejecutor PRIMARY DEV 02 — JAVIER, `TASK_ID=6af16bb9492c8191b8e8c7d606f11507`.

**Resultado acotado:** definir qué evidencia funcional y técnica deberá acompañar a cada requisito al construir la futura matriz única. Este archivo **no** acredita una matriz completa de las 2.340 microtareas ni que una implementación posterior esté certificada; no modifica autoridad institucional ni código ejecutable.

## Aceptación de esta definición documental

| Criterio | Positivo observable | Negativo que debe fallar cerrado |
| --- | --- | --- |
| AC-01 Identidad única | Existe correspondencia unívoca entre ID V5 FINAL, nombre y RESULTADO_EXIGIDO del Plan, dentro de 2.340 IDs `C00-01.M01.01`–`C15-05.M06.06` | Requisito antiguo V3/V13, ID duplicado o ID 2.341 se presenta como ejecutable |
| AC-02 Dependencia causal | `PLAN_MAESTRO!H` coincide con `COLA!E` por fórmula, `G=SI`, `O=SI`, DAG validado, sin responsable antes de claim | Se usa `Q=SI` ajeno, `G=SI` aislado, fase incompleta, o un scope con dueño |
| AC-03 Estado y propietario | `PENDIENTE→EN_PROGRESO→VALIDANDO→LISTO` requiere responsable/TASK_ID verificado, transiciones y readback institucional | Dos Tasks reclaman un ID, se modifica trabajo ajeno o se escribe LISTO directamente |
| AC-04 Pruebas aplicables | A0 estática, A1 positivo/negativo, A2 seguridad, A3 integración proporcional, A4 SHA/readback; declarar `NO_APLICA` por tecnología no afectada | `SKIPPED` o job ausente equivale a PASS, CI anterior acredita cambio nuevo, mocks reemplazan prueba MySQL/HTTP requerida |
| AC-05 Aislamiento tenant y seguridad | Backend verifica JWT/BCrypt, membresía, RBAC, empresa A positiva y empresa B denegada cuando esa superficie se modifica | `EmpresaId`, slug, URL, cookie o header cliente decide autoridad, se filtran PII o secretos |
| AC-06 UI y rendimiento | Si cambia Angular, scripts npm/Angular reales, accesibilidad responsive/teclado/foco y Playwright/Chromium proporcional con evidencia causal | Pantalla navegable se considera segura solo por guard frontend; test visual no ejecutado se marca aprobado |
| AC-07 Persistencia/integraciones | Si cambia SQL/EF/worker/proveedor, job MySQL 8.4/Docker/contrato real pertinente, consistencia, tenant y rollback | Migración destructiva o test simulado sin DB real se usa como evidencia de integración |
| AC-08 Auditoría independiente | SHA, árbol/diff, blobs, eventos BITACORA de 26 columnas A:Z (EVENT_ID A, IDEMPOTENCY_KEY R) e `A5_PENDIENTE` reservado a otra Task | Autor declara su propio A5_PASS, se promociona QA/main/PROD sin revisión y autorización aparte |
| AC-09 Migración/archivos | Inventario previo permite `0` imágenes históricas => migración `NO_APLICA`; archivos futuros con firma, permisos y lifecycle | Se fabrican imágenes o se mueve DNS/Cloudflare R2/MySQL productivo para dar una prueba |
| AC-10 No-regresión arquitectónica | Angular 22 standalone + ASP.NET Core 10 + EF Core/MySQL Oracle + JWT/RBAC se conservan hasta cambio explícito autorizado | Plan objetivo Hostinger/Cloudflare se declara infraestructura ya activa o cambia proveedor sin gate |

Esta tabla es **criterio de aceptación**, no ejecución satisfactoria de todos sus criterios sobre el producto. Los tests runtime se ejecutarán por los scopes que implementen los requisitos; no se atribuyen aquí.

## Contrato mínimo de evidencia por fila futura

Toda futura fila de matriz integra: `V5_ID`; `FUENTE_DOC_Y_VERSION`; `RESULTADO_EXIGIDO`; `PRERREQUISITOS_ID_Y_DAG`; `SUPERFICIE`; `ACTOR_Y_LIMITES`; `CAMBIO_REAL_O_NO_APLICA`; `PRUEBA_POSITIVA`; `PRUEBA_NEGATIVA`; `A0`; `A1`; `A2`; `A3`; `A4`; `A5`; `COMMIT_SHA`; `WORKFLOW_JOB_RUN_ID` cuando aplique; `EVENT_ID_BITACORA`; `ROLLBACK`; `NO_ALCANCE`. Un enlace, un SHA o un evento faltante no se inventa; se registra la brecha.

**Comportamiento ante una falla:** sólo el ID impactado queda PENDIENTE/EN_PROGRESO/VALIDANDO según su estado verdadero; anotar `TEST_GATE_UNAVAILABLE`, check fallido, ausencia de permiso o dependencia aún no finalizada con evidencia exacta. Seguir otras unidades independientes elegibles; no reabrir PR históricos ni alterar el responsable concurrente.

## Dependencia especial y alcance operativo

El `DAG_TECNICO` permite la **preparación documental independiente** `C00-03.M01.01–05` porque `C00-01.M02.01–06` ya están `LISTO`. Esto NO libera `C00-03.M01.06`, M02–M06 o puertas de fase. Antes de implantar/promocionar revalidar todo C00-01 final, no sólo seis contratos M02. `PLAN_MAESTRO!H` sigue siendo autoridad; `COLA!E` fórmula y `BITACORA!A:Z` append-only. No modificar estas fórmulas como parte de la redacción.

**Prueba documental positiva de esta microtarea:** el archivo contiene AC-01…AC-10, especificación de evidencia por fila, clasificación de prueba, dependencia por ID y un rollback no destructivo. **Negativos verificables:** rechazar una fila sin `RESULTADO_EXIGIDO`, exigir A5 distinto para promoción, rechazar tenant cliente como identidad, no aceptar SKIPPED o evidencia de otro SHA, no declarar C00-03 terminada con este archivo.

## Clasificación de validación y reversión

`TIPO1_DOCUMENTACION`: `requiere_dotnet=NO`; `requiere_node_angular=NO`; `requiere_mysql=NO`; `requiere_docker=NO`; `requiere_e2e=NO`; `requiere_security=SI` por revisión de fronteras y secretos en texto; `requiere_scope_lock=SI` porque se publica en dev.

- **A0**: diff acotado, referencias válidas, no secretos ni dependencias nuevas.
- **A1**: comprobar AC-01–AC-10, positivo y negativos, sin afirmar test runtime.
- **A2**: verificar límites JWT/RBAC/tenant/PII y prohibición de modificación de autorizaciones.
- **A3**: validar contrato con fuentes V5, DAG de C00-03 y seis prerequisitos M02 LISTO; no requerir MySQL/Angular no afectados.
- **A4**: commit FF `dev`, SHA exacto, Scope Lock SUCCESS, blobs/diff + readback de PLAN/COLA/BITACORA.
- **A5**: `A5_PENDIENTE` hasta revisión de TASK_ID independiente; sin promoción QA, main ni PROD.

**Rollback:** `git revert <SHA>` en commit específico tras revalidar HEAD, sin force-push ni borrar historia o BITACORA. No ejecutar DNS, R2, Hostinger, migración de MySQL, ni cambiar datos de empresas. Inventario histórico de imágenes cero, `NO_APLICA`.


## CORRECCION_A5_PENDIENTE_SCHEMA26_20261113
Correccion documental dirigida sobre la autoridad fisica vigente `BITACORA!A1:Z1`: 26 campos contiguos, EVENT_ID unico en A, IDEMPOTENCY_KEY unico en R y RUN_ID en H solo si existe evidencia nativa. Positivo: readback de 26 encabezados y eventos con 26 posiciones, SHA exacto y Task de autor original conservada. Negativos: rechazar eventos de 21 a 25 campos, duplicados, autoaprobacion A5 y CI SKIPPED contada como PASS. Esta correccion de propiedad del proyecto no cambia el TASK_ID autor original, no reclama una nueva microtarea, no reescribe BITACORA y exige A5 independiente nueva sobre SHA correctivo antes de usar la evidencia para liberar un cierre de fase, QA, main o PROD.
