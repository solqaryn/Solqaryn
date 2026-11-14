# Backup verificado de la base productiva anterior — 2026-09-26

Estado: **PASS / RESTORE VERIFICADO**.

## Fuente

- Base: `defaultdb`.
- Acceso: lectura consistente mediante `mysqldump --single-transaction --skip-lock-tables --quick`.
- Tablas base observadas: **132**.
- Migraciones EF observadas: **104**.
- Escrituras sobre la fuente: **0**.
- `main` tocada durante el backup: **NO**.

## Evidencia causal

- Workflow: `LEGACY PROD - Backup verificado antes de main`.
- Run: `36228394479`.
- Resultado: `SUCCESS`.
- Artifact cifrado: `solqaryn-legacy-prod-defaultdb-backup-36228394479`.
- Artifact ID: `10901905430`.
- Artifact digest: `sha256:772e6a7a54d6ce6d71570895a69dc1a63638cb8b0abc79f720add830f9d12d2a`.
- Expiración informada por GitHub: `2026-12-25T07:58:42Z`.

Marcadores finales:

```text
LEGACY_PROD_SOURCE_GUARD=PASS
LEGACY_PROD_READ_ONLY_PREFLIGHT=PASS
LEGACY_PROD_ENCRYPTED_BACKUP=PASS
LEGACY_PROD_RESTORE_VERIFICATION=PASS
LEGACY_PROD_BACKUP_FINAL=PASS
SOURCE_DATABASE=defaultdb
SOURCE_BASE_TABLES=132
SOURCE_EF_MIGRATIONS=104
SOURCE_WRITES=0
MAIN_TOUCHED=FALSE
```

## Integridad y recuperación

El artifact fue cifrado con GPG/AES256 antes de publicarse. El workflow verificó:

1. SHA-256 del artifact cifrado.
2. Descifrado controlado dentro del runner.
3. checksums internos de `defaultdb.sql`, `row-counts.tsv` y metadata.
4. restore del mismo dump en MySQL 8.4 descartable.
5. igualdad de número de tablas.
6. igualdad de conteos de filas para todas las tablas base.
7. igualdad del conteo de migraciones EF.

No se versionan dump, passwords ni passphrases en el repositorio.
