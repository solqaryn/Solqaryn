# C10-04.M06.02 — Versión, diff y artefactos reproducibles

**Acción PLAN D:** registrar versión, diff, artefactos y resultado reproducible del bloque C10-04. La celda E («Completa M06») corresponde a una meta acumulativa; este ID **no** acredita un restore, worker ni una autorización de exportación. Proyecto `solqaryn/Solqaryn`, rama única `dev`, `PROJECT_SCOPE_LOCK=STRICT`. Entrega de riesgo **R0 documental**, Task DEV01 `6adbb9fff1548191a1bd5b30d9e9984a`, PLAN fila 1581. No modifica aplicación, infraestructura, DB, JWT/RBAC, API, UI, permisos ni configuración productiva.

## A0 — Evidencia de estado y versión

Autoridad: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`) y Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. En el reclamo se comprobó PLAN H=PENDIENTE/I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI; `SQV17_C10_04_M06_02` candado ID `2000001581`, CAS H con una coincidencia y owner DEV01. BITACORA histórica preservada.

**HEAD observado al producir este registro:** `674770661faaa92afd60f98c2296bf242981b26a`; la fuente reproducible de verdad después de publicar es el nuevo SHA de GitHub, no una cifra declarada antes del commit. El diff esperado del nuevo commit comprende exclusivamente este documento y una sección autocontenida aditiva de `CHANGELOG_AI.md`, que debe preservar íntegro el historial existente. La versión anterior del changelog sufrió un truncamiento accidental en `ad58148f`; los posteriores commits DEV02 recuperaron el blob histórico y su entrada A0.48; este changeset debe mantener **ambos**. No se autocertifica el CI ajeno.

## Manifiesto verificable de archivos existentes

| Archivo/recurso | Git blob verificado | Conclusión permitida |
| --- | --- | --- |
| `backend/src/Application/Services/OutboxRetryProcessor.cs` | `542c0a655a3ee0698e3d80f1854d43903b6470fa` | Procesador de retry existente, sin probar worker nuevo |
| `backend/src/Application/Interfaces/IPublicStoreCache.cs` | `15aeb1c7f4afa6c3b8422b3bd0700d345446f3f8` | Contrato cache pública, no privada distribuida |
| `backend/src/Infrastructure/Services/PublicStoreMemoryCache.cs` | `1a102138b88bd54989eeade5acf7a5ee7bf63867` | Segmentación memoria tenant, no invalidez multinodo |
| `backend/src/Application/Interfaces/ICompraDocumentoStorageService.cs` | `454d588851ecc7db810259e63705852360172928` | Interfaz storage, no proveedor de producción |
| `frontend/e2e/c10-04-m05-06-regression-perf-a11y.spec.ts` | `cc47a9d8ab9cde0ebde79c776d4def6027d80487` | Chromium con fixtures sintéticas, no JWT real |
| `.github/workflows/v19-c10-04-m05-06-regression.yml` | `09e963968c9e1887a8a2f0ee6819a008f36205ee` | Workflow GitHub-hosted, no equivale a PASS ejecutado |
| `scripts/quality/verify-c10-04-m05-06-ephemeral-restore.sh` | `82a72952fbab871d458bdd3049e3bd11ab7f275d` | Ensayo MySQL 8.4 efímero, no Aiven ni backup comercial |
| `docs/v5/c10-04-m05-06-regresion-rendimiento-accesibilidad.md` | `a29ac0a7b2cd3a0ce52d1279b5e13470f0e0e1fd` | Alcance/límites Chromium y MySQL temporal |
| `docs/v5/c10-04-m06-03-limitaciones-riesgos-decisiones.md` | `5733e6e58c09d44e969eadd46154bfae25eeaf13` | Registro R0 de riesgos, CI/A5 de su unidad separados |

**Previas:** en el Sheet C10-04.M05.01–06 aparecen LISTO con A5 independientes registrados; no se infiere de ello que M06 final, workers reales, RPO/RTO o restauración de producción estén certificados. Cada fuente mantiene su SHA y el revisor debe volver a comparar blobs si HEAD avanza.

## Reproducción de procedencia para un checkout verificado

Los comandos siguientes son instrucciones, **NO** pruebas que esta Task haya ejecutado. Cualquier ejecución debe hacerse sobre SHA exacto en un checkout autorizado, sin usar ambientes productivos.

```bash
git rev-parse HEAD
git diff-tree --no-commit-id --name-status -r HEAD
git diff --stat HEAD^ HEAD
git diff --check HEAD^ HEAD
git ls-tree HEAD backend/src/Application/Services/OutboxRetryProcessor.cs
git ls-tree HEAD frontend/e2e/c10-04-m05-06-regression-perf-a11y.spec.ts
git ls-tree HEAD scripts/quality/verify-c10-04-m05-06-ephemeral-restore.sh
node scripts/verify-solqaryn-skills.mjs --self-test
node scripts/verify-project-scope.mjs
node scripts/quality/verify-v5-changelog-provenance.mjs
```

El resultado de CI se consulta exclusivamente en GitHub Actions para el SHA causal con `status=completed` y `conclusion=success`; un workflow escrito en YAML no certifica su propia ejecución. No sustituir `queued/in_progress/skipped/cancelled/failed` de un check requerido por PASS.

## A1–A5 y límites de la unidad documental

- **A0:** diff restringido a documentación, SHA base, hashes de archivos existentes y alcance PLAN D explícito; registrar el nuevo SHA verdadero en BITACORA tras publicación y readback.
- **A1:** comprobar consistencia de manifest/hashes y lenguaje NO_CERTIFICADO; pruebas xUnit/Playwright nuevas `NO_APLICA_R0` porque no hay cambio ejecutable.
- **A2:** revisión de ausencia de secretos, grants, permisos de tenant y PII; JWT/RBAC/tenant backend `NO_APLICA_R0` al diff, sin afirmar que esos subsistemas pasen todo el plan.
- **A3:** integración MySQL/HTTP/Chromium/proveedor nuevos `NO_APLICA_R0` al diff; no heredar un PASS de ensayos históricos. Los riesgos de worker real, failover, rol revocado, backup cifrado y RPO/RTO siguen abiertos.
- **A4:** commit FF/CAS sobre dev, Scope Lock y V5, checks pertinentes `completed/success` exact-SHA, readback PLAN/COLA/BITACORA y conservación de changelog histórico. Mantener VALIDANDO si CI externa pendiente.
- **A5:** revisión auténtica independiente por otra Task Javier distinta de DEV01; no autoaprobar, no elevar QA/main/PROD.

Resultado de esta unidad: registro documental versionado reproducible, **NO** ejecución de restore, job, pago o proveedor. Sin métricas nuevas, `NO_MEDIDO`. Rollback: `git revert` después de releer HEAD; nunca force-push, reset destructivo o borrado de BITACORA. Mantener DAG y fórmulas intactas.
