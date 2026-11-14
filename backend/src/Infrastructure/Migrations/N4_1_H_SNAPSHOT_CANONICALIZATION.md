# N4.1.H — Canonical EF snapshot normalization

This marker records the structural snapshot reconciliation performed for N4.1.H after exact-head ERP-N0.6 produced the authoritative `n41h-ef-drift-probe` artifact.

- Source head: `e7926516508bbcde7a558ece386a229aac486d66`
- ERP-N0.6 evidence run: `33473085304`
- Drift artifact: `n41h-ef-drift-probe` (`artifact_id=9787255844`)
- Artifact digest: `sha256:cc12b8a6931721e693b98d1140a9f4d45d9032f7c8371212ed143d4546f8af13`
- Canonical snapshot commit: `5fc7cd2f3b6d28868ff840ee30731f6a1e8e799a`

The generated canonical `AppDbContextModelSnapshot.cs` replaces the stale delegated split-snapshot representation as the migration snapshot authority. Existing migration history files are preserved; no migration is applied to Production by this change.

This file is intentionally placed under `backend/src/Infrastructure/Migrations/**` so the normal exact-head migration/ERP and M11 push gates certify the reconciled snapshot on `dev`.
