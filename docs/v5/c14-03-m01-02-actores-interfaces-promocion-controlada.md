# SOLQARYN V5 FINAL — C14-03.M01.02: actores, superficies y contratos de promoción controlada

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 02 JAVIER (`6af16bb9492c8191b8e8c7d606f11507`). **Repositorio:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Clase:** preparación documental de M01.02. No autoriza ejecución sobre QA, main, PROD ni despliegues.

**Fuentes autorizadas:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), [M01.01 alcance](./c14-03-m01-01-alcance-promocion-controlada.md), `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, Skill canónica. Habilitación inicial verificada: PLAN H=PENDIENTE/I vacío (fila 2091), COLA G/O=SI y DAG F=VALIDADO/G=SI. Claim condicional sobre PLAN y lectura de owner. Los flujos aquí descritos son contratos futuros, no promoción ni pruebas ejecutadas.

## 1. Actores de análisis A01–A12 y límites de autoridad

Los perfiles se usan para diseñar una futura matriz RACI y **no** crean cuentas, secretos, permisos de GitHub, roles de plataforma ni aprobadores automáticamente.

| Actor de análisis | Entrada o responsabilidad futura | Salida esperada | Límite / caso negativo |
| --- | --- | --- | --- |
| A01 Autor del changeset | SHA, diff, riesgo y pruebas dirigidas dev | Evidencia A0–A4 reproducible | No se revisa A5 a sí mismo |
| A02 Revisor Javier independiente | SHA/blobs, CI, arquitectura y seguridad | A5_PASS o A5_FAIL motivado | Revisor debe tener TASK_ID distinto al autor |
| A03 Propietario autorizador | Solicitud explícita para ambiente ajeno a dev | Decisión verificable y acotada | No se infiere autorización desde LISTO_DEV/A5 |
| A04 Mantenedor de rama | Reglas, referencias y head actual | Publicación fast-forward con CAS | Sin force-push, reset o historia reescrita |
| A05 Responsable de QA futuro | Artefacto, fixtures y contrato de pruebas | Resultado de QA separado | Sin uso de datos productivos por defecto |
| A06 Responsable de producción futuro | Evidencia, ventana, rollback y aprobación | Decisión de despliegue individual | Ninguna Task actual concede por sí misma ese rol |
| A07 Seguridad / IAM | JWT, RBAC, tenant y secreto mínimo | Matriz de autorizaciones y negativos | No se exponen tokens ni PII en logs |
| A08 Operador de datos | Backups, migraciones y reconciliación | Plan de restauración/rollback aislado | Sin DDL ni cambios de MySQL vivo por documento |
| A09 Desarrollador frontend | Build Angular, rutas, E2E y accesibilidad | Artifacto/resultado test con SHA | Guard UI no sustituye seguridad backend |
| A10 Desarrollador API | Build .NET, DTO, middleware, tests | Binary y regresiones causales por SHA | No declarar tests de API por lectura de fuentes |
| A11 Observabilidad / soporte | Health, logs y reportes redactados | Diagnóstico y criterio de abortar | Sin secretos, datos A/B ni ruido de CI como PASS |
| A12 Auditor institucional | PLAN/COLA/DAG/BITACORA/telemetría | Trazabilidad, owners y readback | No edita BITACORA histórica ni deduce estado físico de una fila |

## 2. Superficies e interfaces reales I01–I12

Las rutas siguientes se confirmaron como fuentes o rutas de trabajo en el repositorio/Skill vigente. Su existencia es **evidencia estática**, no acreditación de que el workflow se haya ejecutado, de que exista un ambiente QA operativo ni de que haya paridad actual.

| Interfaz | Fuente/repositorio | Entrada futura | Salida u oráculo futuro |
| --- | --- | --- | --- |
| I01 | `.github/workflows/dev-ci.yml` | SHA dev, cambios y matriz de tests | Check-runs pertinentes por exact SHA |
| I02 | `.github/workflows/ci.yml` | Build/test según workflow real | Resultado conclusivo sin equiparar skipped con PASS |
| I03 | `.github/workflows/environment-infra-parity.yml` | Contratos de paridad y configuración | Evidencia real del job; nombre no prueba paridad |
| I04 | `.github/workflows/dev-final-functional-certification.yml` | Casos funcionales dirigidos | PASS/FAIL de ejecución real, nunca nombre de workflow |
| I05 | `.github/workflows/catalogos-aceptacion.yml` | Matriz de rutas/catálogos y pruebas | Resultado reproducible con fixtures sintéticos |
| I06 | `scripts/verify-project-scope.mjs` | Repo/branch/alcance | Rechazo de cruce de proyecto y check de Scope Lock |
| I07 | `scripts/quality/verify-v5-changelog-provenance.mjs` | Árbol candidato y CHANGELOG | Sin encabezados vacíos ni viñetas cruzadas |
| I08 | `frontend/package.json` | Versión/dependencias de Angular/Node | Toolchain exacta verificada contra el checkout |
| I09 | `backend/src/API/Solqaryn.API.csproj` | TargetFramework y dependencias API | Build .NET 10, test requerido si cambio toca runtime |
| I10 | `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj` | EF Core/proveedor MySQL | Versiones y test de integración pertinentes |
| I11 | `docs/PROJECT_SCOPE_LOCK.md` y `AGENTS.md` | Gobernanza, allowlist y alcance | Reglas activas en exact HEAD, sin fallback histórico |
| I12 | `PLAN_MAESTRO`, `COLA`, `DAG_TECNICO`, `BITACORA` | ID, owner, estado, prerequisitos, EVENT_ID | CAS exclusivo y lectura posterior auditable |

Las rutas de la matriz no autorizan invocar automáticamente workflows, deploys, secretos o APIs de infraestructura. El origen de cada resultado futuro debe vincularse a la ejecución real y su SHA exacto.

## 3. Entradas/salidas obligatorias por frontera

| Frontera | Entrada mínima a contrastar | Salida o rechazo esperado futuro |
| --- | --- | --- |
| Autor→revisión A5 | TASK_ID autor, SHA, diff, pruebas y hashes | A5 por otra Task Javier, con motivo; autoaprobación rechazada |
| Dev→QA | Solicitud de entorno, autorización, artifact digest, tests | Sin permiso expreso: STOP sin publicar ni modificar ramas |
| QA→PROD | Nueva autorización, paridad documentada, respaldo y rollback | No promover por herencia de éxito DEV |
| Frontend→API | UI, rutas, JWT, rol y tenant seleccionado | API valida autorización/propiedad; UI no crea privilegios |
| API→MySQL | Contexto tenant/sucursal y transacción | Consistencia, aislamiento y rollback verificables |
| API→integración | Cuenta técnica, timeout, idempotencia y secreto inyectado | Reintento seguro o rechazo, sin pérdida ni doble efecto |
| CI→auditoría | Git SHA, job, conclusión, logs y artefactos | Solo `completed/success` pertinente cuenta como PASS |
| Run→BITACORA | EVENT_ID/IDEMPOTENCY_KEY nuevos, owner y SHA | Append idempotente y conservación íntegra de historia |

## 4. Ocho pares de aceptación futura P01–P08 / N01–N08

| Caso | Positivo futuro | Negativo bloqueante |
| --- | --- | --- |
| P01/N01 | Construcción y despliegue futuro trazan mismo artifact digest y commit | No coincide SHA/artefacto o se promociona por etiqueta mutable |
| P02/N02 | A5 independiente audita autor y blobs | Task autora se autoaprueba |
| P03/N03 | Permiso explícito acota un ambiente concreto | Documento de diseño se interpreta como autorización PROD |
| P04/N04 | API mantiene rechazo A→B y RBAC server-side | Cambio de header/ID/rol supera tenant o permiso |
| P05/N05 | MySQL efímero valida integración, rollback y consistencia | DDL destructivo no probado o transacción parcial |
| P06/N06 | UI Angular y API tienen regresiones y Playwright ejecutado | Fuente .spec o check in_progress declarado PASS |
| P07/N07 | Observabilidad health/rollback queda trazada y redactada | Error 5xx o secreto en logs sin gestión de incidente |
| P08/N08 | PLAN/COLA/BITACORA concilian ID/estado/owner/sha | Doble claim, A5 falsa o evento histórico reescrito |

**Estado de todos los casos:** `FUTURO_NO_EJECUTADO`. Los oráculos no aseguran que existan roles, ambientes o permisos físicos que aún no se hayan verificado.

## 5. Controles de publicación de este documento

- **A0:** cotejar cobertura A01–A12, I01–I12, fronteras y límites con M01.01.
- **A1:** verificar que P01–P08 y N01–N08 especifican resultado observable sin atribuir test runtime.
- **A2:** preservar JWT/RBAC, tenant A/B, secretos, datos comerciales y separación de ambientes por especificación.
- **A3:** inspección documental de fuentes y contratos; no equivaler archivo CI presente a ejecución o PASS.
- **A4:** commit dev por fast-forward + CAS, diff solo Markdown/CHANGELOG, Scope Lock y V5 provenance exact SHA `completed/success`, lectura posterior PLAN/COLA/BITACORA.
- **A5:** solo revisor entre otras cinco Tasks Javier; queda `A5_PENDIENTE` al cerrar esta entrega.

Rollback de este changeset: revert normal aditivo de `dev`, sin force-push, reescritura de auditoría, cambio de configuración física ni operaciones sobre QA/main/PROD.
