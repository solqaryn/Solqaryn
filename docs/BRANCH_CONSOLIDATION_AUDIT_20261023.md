# SOLQARYN — Auditoría de consolidación de ramas DEV (23-10-2026)

> **EVIDENCIA INFORMATIVA, NO AUTORIZACIÓN DE MERGE, BORRADO O RUNTIME.** Esta acta es una fotografía de las referencias remotas de GitHub y despliegues al realizar la consulta. Ante nuevos commits, repetir comparaciones antes de cualquier operación.

## CERTIFICACIÓN EJECUTADA — 23 de octubre de 2026

**Resultado: `BRANCH_RETIREMENT_47_47_PASS`.** La limpieza autorizada se realizó realmente tras la fusión protegida del PR #3728 en `dev` (`0cb0c69573ae555feae9b5de0a29d63e2cc7b47e`).

- GitHub Actions run [40008279563](https://github.com/solqaryn/Solqaryn/actions/runs/40008279563): job `retire` **SUCCESS**, después de checks obligatorios del SHA exacto y prueba efectiva de crear y borrar una referencia temporal.
- Lectura posterior de ramas: solamente `dev`, `qa`, `main`, las tres protegidas. Fueron eliminadas 47 referencias adicionales; ningún commit exclusivo de ellas se fusionó a DEV.
- SHA sin alterar de QA: `5cedcc91bfb2b69562e7093bc1fa3d1251fa31f3`. SHA sin alterar de PROD (`main`): `072df831585f879cfd7bb429575d6d387076d97b`.
- Los 13 PR históricos abiertos fueron cerrados; la lectura del listado de PR abiertos devolvió cero. No hubo promociones a QA/PROD.
- El PR #3728 retira además del árbol actual de DEV 1.493 ficheros de evidencia antigua VAEP, dos archivos de herramienta heredada, un disparador efímero y tres workflows obsoletos con ejecución manual. Los commits previos siguen localizables mediante historia Git; no confieren autoridad operativa.
- La limpieza de una sola ejecución se retira mediante un PR posterior protegido: `scripts/maintenance/prune-branches-once.mjs` y `.github/workflows/branch-retirement-once.yml` no deben permanecer activos tras ese cierre.
- **Google Sheets microfase 2 continúa certificada**. `AUTOMATION_RUNTIME=RETIRED`, cero Tasks físicas y autorizadas. El despliegue de SHA exacto en Render/Vercel constituye comprobación independiente y no se presume PASS por esta certificación GitHub.

El inventario y la clasificación que siguen corresponden al **snapshot previo a la limpieza**, se conservan exclusivamente para trazabilidad y **no son una lista de ramas existentes**.

---

## Identidad, límites y situación comprobada

- Proyecto único: `solqaryn/Solqaryn`. `PROJECT_SCOPE_LOCK=STRICT`.
- Rama estándar de cambios: `dev`, HEAD observado `eb149ff4d25f9ed37d65fbf2582c19bf5c560baa` (merge protegido PR #3725). **No se autorizan escrituras directas ni bypass del ruleset**.
- Ramas ambientales protegidas que **no se modifican**: `qa` en `5cedcc91bfb2b69562e7093bc1fa3d1251fa31f3`; `main` (PROD) en `072df831585f879cfd7bb429575d6d387076d97b`. No existe una rama distinta `prod` en el inventario; PRODUCCIÓN = `main`.
- Rulesets activos: DEV #22829243 (PR y checks obligatorios: `verify-solqaryn-project-scope`, `SOLQARYN - Seguridad multiempresa`, `SOLQARYN - Build y pruebas backend`), MAIN #23956158 y QA #24341047; sin bypass de actores declarados. Consulta de rulesets públicos, sin cambios.
- Inventario del endpoint oficial: 50 ramas, tres protegidas y 47 adicionales.
- Las comparaciones `dev...rama` arrojan 11 ramas `behind` con `ahead_by=0` y 36 `diverged` con commits propios. El indicador `ahead_by` es evidencia de genealogía Git, **no justifica fusionar código obsoleto**.
- 13 PR abiertos: #3723, #3695, #3687, #3604, #3583, #3582, #3581, #3580, #3578, #3577, #3574, #3554, #3549. Algunos apuntan a `qa` o `main`; su cierre o merge no está autorizado por esta acta.
- Render `solqaryn-api-dev`: despliegue `dep-dbdq8vss728c7385augg` en `live`, SHA exacto `eb149ff4d25f9ed37d65fbf2582c19bf5c560baa`.
- Vercel `solqaryn-dev`: último despliegue `READY` observado `dpl_9pDsRVnujouNZYnZ67obAdUexRne` en `2d196cd61d9d5c45509291ae6dcf51c345557465`; los builds de `eb149ff4...` constan `CANCELED` por `NON_RUNTIME_GOVERNANCE_ONLY_SKIP`, no `READY`. El `compare` de ese SHA base a `dev` identifica 42 rutas modificadas de gobierno, CI, documentación y scripts, sin rutas `frontend/` o `backend/`. **No se declara igualdad de SHA publicado**.
- Google Sheets de seguimiento: documento único `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`, 8 pestañas; `MICROFASE_2_STATUS=CERTIFICADA`; `AUTOMATION_RUNTIME=RETIRED`; 0 Tasks autorizadas/físicas; posiciones `PRIMARY_01` Javier y `SECONDARY_02` Alex siguen `NO_CREADA`. El Sheet no autoriza motor comercial.

## Candidatas ya absorbidas por DEV — 11

Estas referencias no contienen commits exclusivos respecto al HEAD observado. **Solo son candidatas** a borrado después de revalidar SHA, comparador y ausencia de PR abiertos/enlaces activos; no fueron borradas mediante este changeset.

| Rama | Ahead de DEV | Behind de DEV |
|---|---:|---:|
| `audit/pre-microfase1-20261022-1903-hn-4402b183` | 0 | 32 |
| `backup/dev-20261023-f567b7d-pre-rollback` | 0 | 4 |
| `cleanup/remove-obsolete-vaep-state-20261022` | 0 | 48 |
| `docs/dev-p0-011a-commercial-initial-scope-20261021` | 0 | 96 |
| `feat/dev-p4-commercial-draft-ab-pilot-20261014` | 0 | 659 |
| `fix/dev-p4-tenant-boundary-consolidated-20261014` | 0 | 773 |
| `fix/prod-oracle-release-gate-merged-pr-20261012` | 0 | 1020 |
| `fix/vaep-stale-owner-fenced-recovery-20261020` | 0 | 160 |
| `modernization/phase7-closeout-certify` | 0 | 1172 |
| `security/imagesharp-4.1.2` | 0 | 1134 |
| `vaep/primary24-p0-004a-schema-audit-20261020` | 0 | 162 |

## Divergentes — 36, conservar hasta decisión por contenido

No incorporar automáticamente commits heredados. Analizar diff versus código vigente, pruebas, regresiones, seguridad, uso real y obsolescencia. Las ramas VAEP heredadas no son autoridad ejecutable. Ramas vinculadas a PR abiertos no se borran sin disposición del PR.

| Rama | Ahead de DEV | Behind de DEV |
|---|---:|---:|
| `audit/dev-test-b-tenant-membership-20261019` | 1 | 296 |
| `chore/comercial-rename-required-ci-checks-20261019` | 3 | 173 |
| `chore/vaep-logical-prego-evidence-epoch-20261021` | 6 | 95 |
| `cleanup/canonical-solqaryn-identity-20260930` | 38 | 1807 |
| `diagnostic/dev-tenant-readonly-[MASKED_IDENTITY_TOKEN]-20261012 (SHA `fab9e6d22c07d55f26212e6fee2a7ddf419e03e4`)` | 3 | 972 |
| `feat/dev-commercial-independent-owners-20261013` | 35 | 959 |
| `feat/dev-commercial-natural-keys-scoped-20261013` | 10 | 956 |
| `feat/dev-commercial-sales-documents-20261013` | 13 | 962 |
| `feat/dev-commercial-tenant-invoice-documents-20261013` | 17 | 963 |
| `feat/dev-p4-ab-scoped-read-pilot-failclosed-20261014` | 8 | 749 |
| `fix/dev-n11-n12-oracle-bootstrap-20261012` | 5 | 1020 |
| `fix/dev-p4-commercial-natural-keys-reconciled-20261014` | 11 | 730 |
| `fix/dev-p4-config-rbac-route-20261014` | 2 | 804 |
| `fix/dev-p4-[MASKED_IDENTITY_TOKEN]-physical-scope-20261014 (SHA `8062cff73ebb09449d4ba9a065337037f9a4a6a5`)` | 10 | 774 |
| `fix/dev-p4-kardex-tenant-scoped-20261014` | 6 | 774 |
| `fix/dev-p4-legacy-scope-failclosed-20261014` | 5 | 774 |
| `fix/dev-p4-m9-bootstrap-ci-20261014` | 4 | 765 |
| `fix/dev-p4-private-invoice-ownership-20261014` | 5 | 774 |
| `fix/dev-p4-sql-locked-purchase-sales-20261014` | 12 | 774 |
| `fix/dev-vaep-ef-snapshot-gate-20261013` | 2 | 958 |
| `fix/oracle10-e2e-fresh-bootstrap-20261012` | 8 | 1020 |
| `fix/phase7-final-closure-20261006` | 8 | 1172 |
| `fix/phase7-postmerge-ci` | 11 | 1184 |
| `fix/phase7-postmerge-ci-followup` | 2 | 1183 |
| `fix/qa-oracle-ef10-readonly-preflight-20261012` | 29 | 1570 |
| `fix/vaep-fenced-lease-recovery-wip-20261020` | 2 | 154 |
| `fix/vaep-stale-lock-recovery-20261020` | 4 | 160 |
| `governance/approvals-any-eligible-20261023` | 4 | 10 |
| `ops/cloudinary-dev-normalize-11-20261012` | 2 | 1060 |
| `ops/cloudinary-dev-normalize-11-on-certified-base-20261012` | 2 | 1020 |
| `ops/dev-cloudinary-11-normalization-activation-20261012` | 1 | 1060 |
| `ops/dev-p4-certification-fixture-prearm-20261015` | 3 | 609 |
| `phase7/closure-certification` | 8 | 1172 |
| `release/prod-oracle-main-bootstrap-20261012` | 35 | 1570 |
| `test/chatgpt-write-verification-20261020` | 6 | 148 |
| `test/dev-p4-ab-read-pilot-mysql-http-20261014` | 3 | 714 |

## Gates de salida y rollback

1. Revisar cada rama divergente contra `dev` y las fuentes canónicas actuales, clasificando **INTEGRAR_VIA_PR**, **SUPERADA**, **HISTORICA_ARCHIVAR** o **BLOQUEADA** con justificación exacta. No inferir que todo commit antiguo debe pasar a `dev`.
2. Para cambios válidos: PR independiente desde rama temporal hacia `dev`, diff revisado, CI exact-head y políticas de aprobación nativas. No modificar `qa`, `main`, PROD, secretos, código fuera de alcance ni datos comerciales. No actualizar `dev` mediante escritura directa.
3. Para cierre seguro de ramas: revalidar `ahead_by=0` y que la rama no sea protegida, default, referencia de PR abierto o única copia de trabajo. Si hay commits propios obsoletos, conservar evidencia histórica comprobable antes de cualquier borrado. La eliminación requiere una acción real de GitHub y readback de referencias.
4. Confirmar Render DEV `live` y Vercel DEV `READY` para el artefacto que corresponda al último cambio **ejecutable**; para commits documentales, registrar explícitamente el skip deliberado de Vercel. No crear despliegues artificiales ni promover a QA/PROD.
5. Preservar contrato Sheet microfase 2 y `docs/AUTOMATION_STATUS.md` sin activar Tasks (`RETIRED` y cero autorizadas).
6. Reversión de esta evidencia: revert de este documento y entrada de `CHANGELOG_AI.md` mediante PR protegido. **Este documento por sí solo no ejecuta borrados, escrituras comerciales, CI ni despliegues.**

**Estado de cierre de la consolidación:** `BRANCH_RETIREMENT_47_47_PASS` — quedan 36 ramas divergentes y 13 PR abiertos por clasificar antes de declarar «solo tres ramas». No inventar certificación de eliminación ni de merge.
