# RESTORE_VERIFICATION — N8.21

## Restore aislado

El run `35111491587` restauró el mismo artefacto cifrado generado desde Desarrollo sobre un servidor MySQL local y descartable del runner. El script de restore bloquea destinos remotos, nombres con apariencia de Producción y cualquier ejecución que no declare explícitamente un entorno CI/Desarrollo descartable.

## Controles verificados

- SHA-256 externo del backup cifrado: PASS.
- Descifrado temporal protegido: PASS.
- `MANIFEST.sha256` interno: PASS.
- Restore del dump MySQL: PASS.
- Conteo de tablas: `132/132`.
- Historial de migraciones EF: `104/104`.
- Conteos de todas las tablas: equivalencia exacta.
- Topología: `240` foreign keys y `542` índices verificados.
- Base origen y base restaurada: distintas; la fuente de Desarrollo no fue destino del restore.

## Sanity de aplicación

El run `35113349678` ejecutó el backend contra el restore aislado y terminó `success`:

- build backend sin errores ni warnings;
- health y readiness PASS;
- conexión de base `connected`;
- compatibilidad de migraciones PASS;
- lecturas críticas read-only PASS;
- pruebas dirigidas de tenant isolation PASS;
- envíos de email/WhatsApp: NO;
- transacciones reales: NO.

Resultado final: `RESTORE_ISOLATED=PASS`, `active_dev_db_mutated=false`, `production_touched=false`, `secrets_exposed=0`.
