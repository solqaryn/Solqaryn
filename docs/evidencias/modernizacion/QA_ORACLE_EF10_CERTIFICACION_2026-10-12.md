# QA — Adopción Oracle EF Core 10 y preflight exact-head — 2026-10-12

**Scope:** SOLQARYN, Aiven `solqaryn_qa`, MySQL 8.4.8 aprobado, entorno QA. Sin modificaciones en datos de DEV ni PROD. El DNS `solqaryn.com` sigue aplazado.

## 1. Razón y seguridad del cambio

El workflow QA anterior podía ejecutar `dotnet ef database update` con una cadena histórica Pomelo/EF8. Antes de modernizar QA se sustituyó exclusivamente el workflow `.github/workflows/qa-schema-recovery.yml` por un **preflight Oracle EF Core 10 de solo lectura y fail-closed**, sin cambiar la aplicación desplegada. Fue una excepción de bootstrap limitada a QA, separada de la promoción DEV → QA: el guard de promoción sigue exigiendo que los PRs a QA procedan de `dev`. El PR exploratorio #3572 fue cerrado sin merge.

## 2. Auditorías del esquema anterior

- [Run 38302451112](https://github.com/solqaryn/Solqaryn/actions/runs/38302451112): primer preflight read-only. Aiven MySQL **8.4.8**, 136 tablas de aplicación, 107 migraciones EF históricas y marcador `20261006111818_OracleBaseline` ausente. Resultado **STOP** sin escrituras.
- [Run 38302585075](https://github.com/solqaryn/Solqaryn/actions/runs/38302585075): preflight ampliado read-only: fingerprints de tablas/engine/collation, FKs e índices **PASS** frente a DEV certificado; triggers `N0.6` **2/2** y CHECK **1/1**. El único bloqueo era el marcador Oracle.
- No se borraron, reemplazaron ni reejecutaron las 107 migraciones históricas; permanecen en la historia.

## 3. Backups y restauración sin escrituras QA

- [Run 38302947961](https://github.com/solqaryn/Solqaryn/actions/runs/38302947961): `QA_BACKUP_ENCRYPTED=PASS`, `QA_BACKUP_RESTORE=PASS tables=137 history=107 rows_exact=YES oracle_marker=0`, `QA_ORIGINAL_DB_WRITES=0`. Artefacto privado cifrado en GitHub Actions, retención 14 días.
- [Run 38303606678](https://github.com/solqaryn/Solqaryn/actions/runs/38303606678): segundo backup cifrado + restore efímero y ensayo del `Solqaryn.DatabaseBootstrap` oficial con SDK .NET 10.0.401 sobre la copia. `QA_ORACLE_RESTORE_ADOPTION_DRY_RUN=PASS tables_unchanged=136 history=108 qa_original_writes=0`.
- Después del ensayo exitoso se habilitó una **única adopción real** con el mismo migrador. Antes de aplicar comprobó nuevamente el binding de QA, esquema físico idéntico, versión 8.4.8, 107 registros históricos y marcador ausente; `QA_ORACLE_EF10_ADOPTION=PASS metadata_only=YES`. Readback: `QA_ORACLE_BASELINE_CERT=PASS applied=1 history=108 topology_unchanged=YES`.
- Se retiró inmediatamente el job temporal de adopción. El workflow QA de backup/restore permanece reutilizable, **sin ningún ejecutor de adopción**, con expectativa del marcador presente.

## 4. Certificación oficial independiente de la migración

- [Run 38304163633](https://github.com/solqaryn/Solqaryn/actions/runs/38304163633) — `QA_ORACLE_SCHEMA_CERT=PASS app_tables=136 baseline=1 triggers=2 check=1 mysql=8.4.8`, `QA_ORACLE_TOPOLOGY=PASS tables_fks_indexes_identical_to_dev`, `QA_DATABASE_WRITES=0`.
- No se atribuye certificación runtime exact-head QA al preflight SQL. El código QA continúa en la rama QA hasta que se integre el PR DEV → QA `#3567` y termine el despliegue + smoke exact-head.

## 5. Restricciones y rollback

- **No restaurar** el workflow automático EF8 anterior. Backups cifrados solo en GitHub Actions, sin secrets/PII en logs o repositorio. Restore probado únicamente en instancia efímera.
- La única mutación QA de este procedimiento fue la adopción metadata del baseline EF10 en `__EFMigrationsHistory`; tablas, índices, triggers y datos de negocio permanecieron intactos.
- La reversión de un fallo posterior no debe borrar tablas ni restaurar Aiven por defecto: detener despliegue y evaluar solo el marcador/estado con backup verificado y operación separada.
- Regla de promoción: **dev → qa → main**, PR de QA `#3567` como único camino para promover aplicación. QA → PROD requiere autorización nueva y explícita. SMTP OAuth2 real y `solqaryn.com` quedan aplazados por el propietario; Render Free sin cambios ni compras.

**Dictamen acotado:** `QA_ORACLE_SCHEMA=PASS`, `QA_APPLICATION_PROMOTION=NOT_YET_CERTIFIED`, `PROD_PROMOTION=NOT_AUTHORIZED`.
