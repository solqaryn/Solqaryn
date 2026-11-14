> HISTÓRICO — evidencia de una fase cerrada. No define nombres vigentes de secrets, variables, environments, scripts ni workflows. La autoridad operativa actual es `docs/BACKUP_RESTORE_DESARROLLO_RUNBOOK.md`.

# FASE M11 — Backups y restauración en Desarrollo

Fecha de cierre: 2026-08-10  
Rama exclusiva: `Desarrollo`  
HEAD funcional certificado base: `b15adeaf7cb7557f4c0286b807cc60e9e4b03b7a`  
Producción: **FUERA DE ALCANCE**  
Estado: **COMPLETADA / CERTIFICADA AUTOMÁTICAMENTE**

## 1. Objetivo

Implementar un procedimiento reproducible, cifrado, verificable y fail-closed para respaldar los activos recuperables de Solqaryn en Desarrollo y demostrar una restauración completa sobre un entorno MySQL descartable antes de considerar el backup utilizable.

## 2. Activos incluidos

1. Base de datos MySQL completa mediante dump consistente `--single-transaction`, sin lock de tablas.
2. Conteos exactos de filas por tabla para verificación post-restore.
3. Historial `__EFMigrationsHistory` y metadata de versión.
4. Configuración versionada segura mediante allowlist, nunca `.env`, certificados ni archivos locales de secretos.
5. Documentación versionada bajo `docs/`.
6. Referencias de imágenes `ProductoImagenes` (`Url`, `PublicId`, ámbito de producto/variante).
7. Referencias y metadata de `CompraDocumentos` (`Url`, `PublicId`, tipo, tamaño y recurso).
8. Metadata del backup, commit Git, versión MySQL, modo TLS, número de migraciones, número de tablas y política de retención.

Los binarios que residen en proveedores externos no se confunden con el dump SQL: sus referencias y metadata quedan inventariadas explícitamente para auditoría y recuperación controlada.

## 3. Seguridad

- `scripts/m11_backup_desarrollo.sh` acepta únicamente `desarrollo`, `development` o `ci`.
- Cualquier entorno/base que parezca Producción se rechaza antes de conectarse.
- Los secretos llegan únicamente por variables de entorno y no se escriben en metadata.
- El payload se conserva únicamente después de cifrarse con OpenPGP simétrico AES-256.
- La passphrase se entrega a GPG por `stdin`, no como argumento visible del proceso.
- El backup cifrado y archivos auxiliares quedan con permiso `0600`.
- Existe checksum SHA-256 externo del artefacto cifrado y manifest SHA-256 interno por archivo.
- La retención elimina exclusivamente artefactos que coincidan con el patrón autorizado de M11; no borra archivos ajenos.
- `.gitignore` bloquea artefactos de backup/restore locales.
- `DB_SSL_MODE` soporta `DISABLED`, `PREFERRED`, `REQUIRED`, `VERIFY_CA` y `VERIFY_IDENTITY`; el flujo operativo de Aiven exige `REQUIRED`.

## 4. Restauración

`scripts/m11_restore_desarrollo.sh`:

- solo permite `ci`, `desarrollo-descartable` o `development-disposable`;
- exige `ALLOW_DESTRUCTIVE_RESTORE=YES_M11`;
- rechaza destinos que parezcan Producción;
- exige un nombre de base explícitamente descartable (`restore`, `drill`, `m11`, etc.);
- soporta TLS configurable mediante `TARGET_DB_SSL_MODE`;
- verifica checksum cifrado antes de descifrar;
- verifica todos los checksums internos antes de tocar MySQL;
- recrea exclusivamente la base descartable autorizada;
- restaura el dump;
- compara número de tablas, historial EF y conteos exactos de todas las tablas;
- extrae configuración/documentación/referencias a un directorio separado, sin sobrescribir el repositorio;
- genera un reporte JSON de restore.

## 5. Gate M11

Workflow:

`.github/workflows/m11-backup-restore-desarrollo.yml`

La certificación automatizada ejecutó:

1. MySQL 8.4 con `sql_require_primary_key=ON`;
2. historial EF completo desde cero;
3. registros sentinel de Producto, Imagen, Compra y Documento;
4. backup transaccional;
5. exportación de configuración, documentación y referencias externas;
6. checksums internos SHA-256;
7. cifrado AES-256;
8. comprobación de que no persisten `.sql` ni `.tar.gz` planos;
9. política de retención de 14 días sin borrar un archivo ajeno de control;
10. verificación del checksum cifrado;
11. descifrado en espacio temporal;
12. restore en `Solqaryn_m11_restore`;
13. comparación exacta de todas las tablas y filas;
14. comprobación de referencias sentinel de imagen y documento;
15. arranque real de la API contra la base restaurada;
16. `/health` y `/health/ready` exitosos;
17. pruebas fail-closed para Producción y destinos no descartables.

### Resultado certificado

Workflow: `M11 - Backup y restauración en Desarrollo`  
Run base: **`31410746477` — SUCCESS**  
HEAD base: `b15adeaf7cb7557f4c0286b807cc60e9e4b03b7a`

Todos los pasos del job `Backup cifrado y restore MySQL descartable` terminaron en `success`.

Artifact base:

- nombre: `m11-backup-restore-desarrollo`;
- ID: `9071507118`;
- SHA-256: `d4ddb482c79d6ea92fe4032e387c3c30c24fec186b2de67b20c414895da2ff13`;
- retención GitHub Actions: 14 días.

La fase fue recertificada posteriormente sobre el hardening operativo de M11; los runs posteriores mantienen el mismo drill completo de backup→restore y no sustituyen la evidencia base anterior.

## 6. Resultado de integridad del restore

El reporte generado por el propio drill certificó:

- formato: `M11.1`;
- base origen CI: `Solqaryn_m11_source`;
- base restaurada: `Solqaryn_m11_restore`;
- **54 tablas base**;
- **32 migraciones EF**;
- `checksumsVerified = true`;
- `allTableRowCountsVerified = true`;
- configuración extraída: `true`;
- documentación extraída: `true`;
- referencias externas extraídas: `true`;
- `productionTouched = false`;
- estado: **SUCCESS**.

La API arrancó sobre la restauración y `/health/ready` respondió HTTP 200.

## 7. Backup operativo de Aiven Desarrollo

Se agregó:

`.github/workflows/m11-backup-desarrollo-operativo.yml`

El workflow utiliza exclusivamente el environment GitHub existente:

`Desarrollo - solqaryn-api-desarrollo`

y espera secretos dedicados, cuyos valores nunca son leídos ni versionados por el código:

- `M11_DESARROLLO_DB_HOST`;
- `M11_DESARROLLO_DB_PORT`;
- `M11_DESARROLLO_DB_NAME`;
- `M11_DESARROLLO_DB_USER`;
- `M11_DESARROLLO_DB_PASSWORD`;
- `M11_BACKUP_PASSPHRASE`.

El job exige TLS `REQUIRED`, publica únicamente el `.gpg`, su checksum y metadata no sensible, y mantiene retención de 14 días.

### Disparadores seguros

Mientras `main` permanezca congelada, GitHub no debe considerarse dependiente del `schedule`/`workflow_dispatch` de un workflow que solo vive en `Desarrollo`. Para evitar modificar `main`, el workflow también soporta un trigger controlado desde la propia rama `Desarrollo`:

- push a `Desarrollo` que afecte `.github/checkpoints/m11-backup-request` (o los scripts/workflow M11);
- mensaje de commit que contenga exactamente `[M11-BACKUP-REAL]`;
- secretos M11 disponibles en el environment `Desarrollo - solqaryn-api-desarrollo`.

Sin el marcador explícito, el job real queda `skipped` y solo se ejecuta el gate estático de sintaxis/protecciones. Esto evita respaldos accidentales por cada commit.

Cuando, en una liberación futura autorizada, la definición exista en la rama por defecto, el workflow mantiene además `workflow_dispatch` y schedule diario; el schedule solo ejecuta el backup si `M11_DESARROLLO_BACKUP_SCHEDULE_ENABLED=true`.

### Validación del workflow operativo

GitHub reconoció el workflow desde `Desarrollo`. El gate `Validar definición y protecciones M11` certificó sintaxis de ambos scripts y sus invariantes de seguridad, mientras el job con secretos quedó correctamente omitido en una ejecución normal de PR/push.

### Límite de evidencia externa

La certificación automática demuestra el proceso completo backup→restore sobre MySQL real descartable y el arranque de la aplicación restaurada. **No se afirma que se haya generado un backup de los datos actuales de Aiven Desarrollo**, porque la integración GitHub disponible no permite leer/listar los secretos de ese environment y no se deben extraer credenciales desde Render.

Por tanto, el mecanismo M11 está certificado; la primera ejecución sobre los datos actuales de Aiven Desarrollo permanece como validación externa operativa hasta que los secretos M11 existan y se dispare explícitamente el job real.

## 8. Archivos principales

- `scripts/m11_backup_desarrollo.sh`;
- `scripts/m11_restore_desarrollo.sh`;
- `.github/workflows/m11-backup-restore-desarrollo.yml`;
- `.github/workflows/m11-backup-desarrollo-operativo.yml`;
- `.gitignore`;
- `docs/FASE_M11_BACKUPS_RESTAURACION_DESARROLLO.md`.

## 9. Seguridad del repositorio

Durante M11:

- solo se trabajó sobre `Desarrollo`;
- `main` no se modificó;
- no se creó ninguna rama;
- PR #2 no se fusionó;
- no se habilitó auto-merge;
- no se aplicó ningún restore contra Aiven ni Producción;
- no se modificaron credenciales, dominios, servicios ni datos productivos;
- ningún secreto fue incorporado al repositorio ni al artifact certificado;
- no se creó un environment GitHub nuevo: se reutiliza el environment de Desarrollo existente.

## 10. Cierre

**FASE M11 — Backups y restauración en Desarrollo: ✅ COMPLETADA Y CERTIFICADA AUTOMÁTICAMENTE.**

Siguiente fase del Plan Maestro:

**M12 — Automatización transversal.**

La ejecución de un backup real de la instancia Aiven Desarrollo queda identificada como validación externa operativa, no como evidencia automatizada ya realizada.
