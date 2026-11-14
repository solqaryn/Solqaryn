# BACKUP_MANIFEST — N8.21

## Backup automático administrado

La evidencia causal previa y obligatoria de N8.20 certificó la instancia DEV real como MySQL 8.4.8 sobre Aiven, servicio `solqaryn-mysql`, plan `free-1-1gb`, con modo de recovery `pitr`, disponibilidad de PITR, política de retención demostrada y ruta de restore `AIVEN_FORK_AND_RESTORE_CONTROL_PLANE`. Esa prueba proviene de runtime/control-plane evidence de la instancia concreta y no de documentación genérica.

Referencia: `vaep/evidence/receipts/N8.20.H_LISTO_REAL_20260916T135053Z_SUP00.json`.

## Backup lógico temporal y ejecución material

- Entorno origen: Desarrollo.
- Run causal: `35111491587` — `M11 - Backup operativo de Desarrollo`.
- Fuente: conexión MySQL autorizada con TLS `REQUIRED`.
- Acceso a fuente durante el backup: lectura consistente mediante `mysqldump --single-transaction --skip-lock-tables --quick`.
- Producción: fuera de alcance y no tocada.

## Contenido y protección

El backup lógico contiene dump MySQL, conteos de integridad por tabla, configuración versionada allowlisted, documentación versionada y referencias de assets externos. El payload se empaqueta y se cifra antes de la persistencia mediante OpenPGP simétrico AES256; el artefacto cifrado lleva checksum SHA-256 externo y el payload contiene `MANIFEST.sha256` interno.

Los archivos temporales de texto claro viven únicamente en un directorio protegido del runner (`umask 077`) y se eliminan mediante trap de cleanup. No se commiteó dump SQL ni backup cifrado al repositorio.

## Resultado material

- `MANAGED_AUTOMATIC_BACKUP_PITR_PROVEN=true`
- `BACKUP_REAL=PASS`
- `RESTORE_FROM_SAME_ENCRYPTED_ARTIFACT=PASS`
- Base tables: `132`
- EF migrations: `104`
- Foreign keys verificadas en topología: `240`
- Índices verificados en topología: `542`
- Todos los conteos de tablas: equivalentes origen/restore.
- Secretos expuestos: `0`.

## Retención final

El artefacto temporal con la copia lógica completa de la base (`10452911215`) fue eliminado después de la validación causal mediante el run `35120379562`. El readback de artifacts del run original confirmó que ya no está presente. Sólo permanece evidencia no secreta necesaria para auditoría, como el certificado sanitizado del proveedor.
