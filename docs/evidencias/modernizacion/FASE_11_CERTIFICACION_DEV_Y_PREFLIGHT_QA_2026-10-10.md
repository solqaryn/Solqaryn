# Fase 11 — Evidencia causal DEV y preflight seguro de promoción QA

**Documento de evidencia, no Plan Maestro ni autorización adicional de producción.**

- Repositorio: `solqaryn/Solqaryn`; origen de promoción: `dev`; destino previsto: `qa`; rama productiva `main` **intocable sin autorización explícita nueva**.
- Corte técnico DEV inspeccionado: `b11ce636c6a5cfc7065e6f2ea220c9298d1976e3`.
- **Estado del alcance Fase 11:** DEV técnico validado; **Fase 11 integral no es CERTIFIED hasta certificar adopción/schema, deploy exact-head y smoke QA, y resolver los criterios SMTP establecidos**.
- Coste incremental autorizado: **USD 0**. Sin nuevos servicios, upgrades, keep-alive ni compras. MySQL corporativo Aiven **8.4.8 aprobado**, sin intento de actualizar a 8.4.11.

## 1. Certificación exact-head DEV

Nueve workflows `success` del mismo commit `b11ce636`:

| Gate | GitHub Actions | Resultado |
| --- | --- | --- |
| Scope Lock | 38108305232 | SUCCESS |
| VAEP OPEN-only | 38108305201 | SUCCESS |
| Certificación funcional HTTP de DEV | 38108305296 | SUCCESS |
| QuestPDF / PDF | 38108305255 | SUCCESS |
| NuGet Fase 8 | 38108305312 | SUCCESS |
| MySQL EF provider Fase 6 | 38108305317 | SUCCESS |
| .NET EF Oracle Fase 7 | 38108305347 | SUCCESS |
| Aceptación Playwright integral (100/100) | 38108305335 | SUCCESS |
| Frontend tooling Fase 5, npm audit | 38108305275 | SUCCESS |

Evidencia transversal adicional, ejecutada antes del último changeset de CI sin cambios del runtime funcional: `SOLQARYN - Backup y restauración en DEV` [38088803575](https://github.com/solqaryn/Solqaryn/actions/runs/38088803575) `SUCCESS`: bootstrap Oracle EF10, 136 tablas de app, cifrado/checksum, restore descartable, sentinels y arranque de API restaurada. Backup **real** fuente Aiven DEV + restore local efímero + topología/FK/índices equivalentes: [38107320797](https://github.com/solqaryn/Solqaryn/actions/runs/38107320797) `SUCCESS`. El run 38107320766 aprobó la validación estática, pero los jobs de backup real quedaron `skipped`; **no** se usa ese run como prueba de respaldo real. Fase 10 en commit anterior `7dd652f`: 2,401 unitarios y 23 pruebas EF SQLite/InMemory/Moq, con NuGet sin advisories en su alcance.

Render DEV `dep-db5g9j740ujc73cf95r0` `live` sobre el HEAD; Vercel DEV `dpl_D46ZjdpKNyEk5uMRn68RUkGv48ya` `READY` sobre el mismo HEAD.

## 2. Estado QA y riesgo de migración

- QA HEAD observado: `682f4ee3b414c83bcef5745ccdcd4e110f7f3692`, no contiene aún la modernización completa.
- Divergencia Git: DEV presenta 485 commits no contenidos en QA y QA 27 commits propios desde ancestro `1c8e6781469532bc3ec80261343d3c484f06964b`. Preservar cambios propios QA; no force-push.
- PR de promoción [#3567](https://github.com/solqaryn/Solqaryn/pull/3567), **DRAFT**, código mergeable sin conflictos; NO se ha integrado a QA.
- Última auditoría de base QA conocida: [37023690322](https://github.com/solqaryn/Solqaryn/actions/runs/37023690322), **2026-10-02**: 137 tablas totales, 107 entradas en historial EF, 89 migraciones referenciadas por el antiguo código y 18 históricas adicionales. Es evidencia de esa fecha, no certificación Oracle actual.
- La migración Oracle `20261006111818_OracleBaseline` y los invariantes SQL N0.6 deben verificarse contra QA real **antes** del cambio de backend. El workflow histórico de recuperación QA todavía contenía una ruta antigua `dotnet ef database update --project Infrastructure` y podía autoescribir: se sustituye por **auditoría exclusiva de solo lectura**, que bloquea si falta baseline o contrato físico Oracle.
- Contrato QA: `solqaryn_qa` / `solqaryn_qa_user`, TLS requerido, Render Staging, prefijo `solqaryn_qa`, Vercel QA con binding explícito. Las migraciones de esquema/datos QA solo se permiten mediante una operación controlada independiente con backup cifrado probado, readback y rollback; **nunca con auto-migrate startup**.
- El frontend READY QA encontrado anteriormente pertenece a un commit anterior al HEAD QA; se exige reconciliar despliegues y verificar SHA exacto después de promover.

## 3. Bloqueo de correo y criterio de salida

La configuración Outlook.com/OAuth2, pruebas de configuración, retry, adjuntos PDF y SMTP simulado están implementadas. Sin embargo, `docs/DETALLES_PENDIENTES.md` y `docs/RENDER_ENVIRONMENT_CONTRACT.md` registran que la conexión SMTP saliente/STARTTLS, autenticación y entrega/recepción real desde Render Free **no están certificadas**. Aunque existía una decisión operativa anterior de aplazar ese transporte sin comprar, la solicitud de Fase 11 lo menciona entre sus requisitos: **no etiquetar `SMTP_E2E=PASS` sin una ejecución real**. Un cierre por excepción debe identificarse como tal, no como prueba realizada.

### Excepción operativa ratificada por propietario — 2026-10-12

El propietario ordenó **mantener SMTP OAuth2 con envío/recepción real expresamente aplazado** en `docs/DETALLES_PENDIENTES.md` y no comprar servicios. Por tanto:

- `SMTP_RUNTIME_E2E=NOT_EXECUTED / DEFERRED_BY_OWNER`; **jamás escribir `SMTP_OK` ni `PASS` sin evidencia de entrega y recepción**.
- Se permite evaluar certificación de los otros controles independientes de la Fase 11 sin fingir SMTP aprobado. Cualquier certificación integral debe identificar el alcance excluido; no cabe un `100%` de requisitos originales sin acreditar SMTP real.
- La excepción no autoriza QA → PROD, cambios en secrets/OAuth, puertos ni pago de infraestructura, y **no exime el gate Oracle QA, smoke exact-head QA ni smoke PROD autorizado**.
- DEV actual: PR #3569 integrado `71bcfbee`, Render/Vercel exact-head operativos y descargas de producto/ZIP confirmadas por propietario. PR #3570 auditoría solo lectura `42e60d9` detectó 11 imágenes de producto en carpetas no canónicas; la normalización física permanece separada.
 
## 4. Corrección en DEV de gates de promoción (scope de este changeset)

1. `.github/workflows/qa-ci.yml`: ejecuta build/tests frontend/backend y npm audit en PRs cuyo destino es `qa`, antes de merge; sin secretos QA en PR.
2. `.github/workflows/qa-schema-recovery.yml`: retira el camino obsoleto autoescritor, ejecuta únicamente consultas `SELECT` en `qa`, valida binding, MySQL 8.4.8, 136 tablas de aplicación, tablas Oracle requeridas, marcador OracleBaseline, triggers/CHECK de N0.6. Si falta algo, falla cerrado y **no toca ningún dato QA**.
3. No se modifican bases, secretos, servicios, precios, `qa`, `main` ni PROD. QA requiere validación del nuevo preflight en su rama; no se presupone PASS.

## 5. Rollback / salida

Rollback de los cambios de gates: revert normal del merge específico en `dev`, preservando historia. Si el preflight SQL QA es rojo, **STOP antes del merge de modernización**; solucionar incompatibilidades con respaldo verificable sin copiar DEV sobre QA. No se autoriza ni se ejecuta ningún deployment productivo.

**Dictamen en este corte:** `FASE11_DEV_TECHNICAL_GATES=PASS`; `FASE11_QA_PROMOTION=NOT_EXECUTED`; `FASE11_SMTP_RUNTIME_E2E=UNVERIFIED`; `FASE11_INTEGRAL_100_PERCENT=NOT_CERTIFIED`.
