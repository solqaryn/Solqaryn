# SOLQARYN — Limpieza controlada de datos de prueba comerciales DEV

**Decisión 2026-10-13:** el propietario declaró descartables los 19 registros históricos de las cinco raíces comerciales en Aiven DEV. No se necesita backfill. Este documento es un contrato preventivo, no un borrado ejecutado ni permiso para tocar QA o PROD.

## Preservar identidad y acceso

- Conservar Usuarios y sus contraseñas/hashes, roles, permisos, UsuarioEmpresas, Empresas y la configuración/referencias mínimas para login y RBAC. Nunca borrar estos datos ni borrarlos por cascada.
- El resto de datos de prueba comerciales podrá limpiarse SOLO después de separar tablas globales, catálogos compartidos, documentos/inventarios/contabilidad de prueba y dependencias referenciales. Hay 132 entidades EF y 66 descendientes comerciales inventariados.
- Las referencias Cloudinary e integraciones externas se inventarían por separado; no borrarlas como efecto colateral de un borrado MySQL.

## Preflight para operación posterior exclusiva de DEV

1. Verificar repo, dev exact-head, Environment DEV, usuario y base Aiven solqaryn_dev, versión MySQL 8.4.8 y ausencia de writers simultáneos.
2. Inventariar tablas/FKs reales, cascadas, referencias sin FK y recuentos; definir allowlist de filas comerciales descartables y denylist de identidades/catálogos indispensables.
3. Crear respaldo cifrado fresco con scripts/backup_dev.sh y restaurarlo con scripts/restore_dev.sh en servidor efímero, comprobando checksum, conteos, migraciones y recuperación.
4. Ensayar eliminación idempotente por grafo en copia efímera, desde dependientes hacia raíces, con transacción, predicados específicos, checks referenciales, rollback ante error y verificación de login/membresías.
5. Solo tras revisión y autorización de escritura DEV, limpiar datos acotados de Aiven DEV; registrar conteos agregados antes/después sin IDs ni PII, y validar restaurabilidad. Prohibido deshabilitar FOREIGN_KEY_CHECKS, usar TRUNCATE masivo o DROP DATABASE en Aiven.
6. Revalidar usuarios intactos, autenticación, permisos, FKs, ausencia de fixtures comerciales y funcionamiento de nuevas altas. No habilitar segunda empresa todavía.

## Distinción del cierre

Se cierra la necesidad de ATTRIBUIR los 19 registros; la limpieza física permanece sin ejecutar. El aislamiento de datos FUTUROS requiere pruebas A/B completas de consultas/escrituras, documentos, inventario, archivos, tienda pública, trabajos, caché y RBAC antes de levantar la cuarentena.

QA, PROD, dominio y automatizaciones programadas: fuera de alcance.
