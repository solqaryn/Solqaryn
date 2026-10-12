# Modernización — Fase 0: baseline y congelación técnica

Fecha de certificación: 2026-10-02 (UTC)
Repositorio: `solqaryn/Solqaryn`
Rama: `dev`
HEAD certificado: `ffe3ac936cbcdaa65e513042b1efa9242805b28a`
HEAD fuente del snapshot pre-modernización: `1c8e6781469532bc3ec80261343d3c484f06964b`

## Alcance y dictamen

La Fase 0 congeló las versiones y lockfiles de la plataforma antes de iniciar la migración y certificó el HEAD de `dev` con checks de backend, frontend, Docker, seguridad/tenant, MySQL efímero, Playwright y acceso DEV de Aiven. El run final exact-head [37067448864](https://github.com/solqaryn/Solqaryn/actions/runs/37067448864) terminó `success` en sus siete jobs; su Dictamen registró `FASE_0_BASELINE=PASS`, `BACKUP_RESTORE_SAME_ARTIFACT=PASS` y `PRODUCTION_TOUCHED=false`.

El workflow final incluye recuperaciones de fallos iniciales y no oculta sus corridas canceladas/fallidas: tenant scope de Sucursales, seed/runtime E2E y el contrato de logs se corrigieron antes de la certificación final. La corrida final ejecutó aceptación Playwright canónica `100/100` y validó SMTP/PDF.

## Snapshot de versiones

`BASELINE_FASE_0.json` identifica el snapshot fuente `1c8e6781469532bc3ec80261343d3c484f06964b`, los hashes SHA de `package.json`, `package-lock.json`, proyectos .NET, Dockerfile y `render.yaml`, además de versiones resueltas archivadas como artefacto del run final. El baseline congeló Angular 20.3.33, Node 20 en la reproducción histórica de CI, .NET 8, EF Core 8.0.2, Pomelo 8.0.2, MySqlConnector 2.3.7 y MySQL/Aiven observado 8.4.8. El snapshot es histórico; no sustituye el stack modernizado vigente.

## Gates exact-head

- Workflow de baseline [37067448864](https://github.com/solqaryn/Solqaryn/actions/runs/37067448864), HEAD `ffe3ac936cbcdaa65e513042b1efa9242805b28a`: los siete jobs terminaron `success`.
- El job Aiven DEV verificó identidad, versión y backup cifrado; restauró el mismo artefacto únicamente en MySQL descartable. `productionTouched=false`.
- En el mismo HEAD, el gate final Aiven/Fase 1 [37067448903](https://github.com/solqaryn/Solqaryn/actions/runs/37067448903) terminó `success` y evitó mantenimiento del servicio compartido.
- Artefactos del baseline y sus digestes se conservan en Actions. Al 2026-10-05 el backup cifrado `fase0-aiven-backup-restore-37067448864` está disponible, con digest `sha256:f9ea24691a6233f805d11b2002571ea2508c46c73f946a8b8d8256d78e263d1c` y expiración `2026-10-16T21:33:38Z`; la evidencia y los artefactos de versiones/e2e tienen expiración posterior `2026-12-31`.

## Límites

Esta fue una certificación histórica de preparación. No se vuelve a ejecutar sólo para renovar el backup: eso volvería a leer datos reales de DEV. El artefacto está cifrado; nunca se restauró sobre Aiven ni sobre QA/PROD. La ausencia de una nueva copia de datos reales en las fases posteriores no invalida este baseline, pero la expiración del artefacto se conserva explícitamente para que no se confunda disponibilidad temporal con retención permanente.

**Fase 0: PASS histórico y cerrada.** No hay evidencia de una deuda técnica de Fase 0 que justifique repetir sus efectos sobre Aiven. Las fases siguientes deben evaluarse con sus propios gates y versiones vigentes.
