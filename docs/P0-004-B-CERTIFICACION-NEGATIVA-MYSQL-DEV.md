# P0-004.B — Certificación negativa de inventario MySQL DEV

**Fuente:** Plan Comercial Inicial Unificado; PLAN_MAESTRO fila 9, COLA fila 9. **Dependencias:** P0-004.A y P0-012.B LISTO al seleccionar. **Alcance:** DEV solamente, PROJECT_SCOPE_LOCK=STRICT.

## Evidencia y límites

- Referencia: PR #3766 (https://github.com/solqaryn/Solqaryn/pull/3766), fusionado en dev. Sus nueve consultas manuales de metadatos SELECT y su validador aportan 50 pruebas negativas.
- Readback institucional de P0-004.A: evidencia **atribuida al propietario**, MySQL DEV 8.4.11, 139 tablas y 2289 columnas físicas; modelo EF 135 tablas y 2271 columnas; 15/15 migraciones aplicadas. Las cuatro tablas y dieciocho columnas adicionales se clasificaron como auxiliares. **Esta microtarea no obtuvo segunda conexión SQL nativa ni certifica físicamente de nuevo esas cifras.**
- Backup y retención fueron excluidos por decisión del propietario solamente del criterio de P0-004.A. No representan backup+restore probado y **no autorizan limpieza de datos**. Para futuras limpiezas DEV exigir respaldo/restauración probados, grafo FK, DML granular y readback por contrato separado.
- Este PR comprueba el rechazo de SQL inseguro mediante tests offline sobre copias temporales. No ejecuta SQL real, DatabaseBootstrap ni migraciones; no consulta registros comerciales, usuarios, permisos, secretos ni QA/PROD.

## Pruebas positivas y negativas

Comando: node scripts/verify-p0-004-b-certificacion-negativa.mjs

La prueba reutiliza el validador de P0-004.A (nueve SELECT y cincuenta negativas), luego altera una copia temporal del SQL en ocho situaciones adversariales: UPDATE, DELETE, ALTER, CALL, INTO OUTFILE, acceso cross-schema, lectura de filas comerciales y SELECT *. Cada alteración debe ser rechazada. Node estándar sin conexión MySQL; directorio temporal eliminado al finalizar.

El workflow separado p0-004-b-certificacion-negativa.yml exige checkout exacto del SHA del PR, permiso contents:read y Node 24. Además son obligatorios CI 3/3 exact-SHA, revisión genuinamente independiente antes del merge protegido y readback PLAN_MAESTRO/COLA/BITACORA.

## Condición de cierre, impacto y rollback

Mantener EN_PROGRESO mientras falta CI o revisión. VALIDANDO solo con PR real, checks exitosos y sin edición activa. LISTO únicamente tras revisión independiente anterior al merge, CI exacto, integración protegida en dev y readback comercial. Un caso negativo aceptado produce FAIL.

Impacto limitado a test offline, workflow, acta y CHANGELOG_AI.md. No hay cambios de arquitectura, API, DTO, EF, índices, permisos, datos, infraestructura o deployment. Rollback mediante PR protegido que revierta estos archivos; no se requiere rollback MySQL.
