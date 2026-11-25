# QA — Galería de categorías: activación segura

## Alcance

Migración aditiva `20261125212000_CategoriasGalerias` de Oracle EF Core 10.
No cambia las categorías existentes, los datos de inventario ni las tablas anteriores.
La identidad de la galería es EmpresaId + CategoriaId. El backend antiguo puede operar con la tabla adicional.

## Antes del despliegue del backend

1. Confirmar que el checkout y el SHA aprobados pertenecen a `solqaryn/Solqaryn`, rama `qa`, y que la compilación y las pruebas del commit están aprobadas.
2. Ejecutar y comprobar el workflow **QA - Backup cifrado y restore aislado Oracle EF10**; debe finalizar con `QA_BACKUP_RESTORE=PASS` y `QA_ORIGINAL_DB_WRITES=0`. Conservar el artefacto cifrado y sus checksums. No exponer secretos ni SQL descifrado.
3. Obtener autorización para aplicar la migración sobre la base activa. Verificar conexión por TLS al host QA canónico, puerto 14402, usuario QA, base exacta `solqaryn_qa`, MySQL 8.4.11, marcador OracleBaseline y ausencia de migraciones inesperadas. No utilizar las conexiones DEV o PROD.
4. Desde el mecanismo de migración EF de SOLQARYN, usar el proyecto de migraciones Oracle y el startup API con la conexión QA verificada. Listar las migraciones pendientes y aplicar exclusivamente el destino `20261125212000_CategoriasGalerias`. No revertir migraciones existentes ni activar migraciones al arrancar Render.
5. Ejecutar la certificación read-only del esquema QA: deben pasar las verificaciones de las 136 tablas base y la estructura, índices, claves externas y marcador de CategoriasGalerias. El resultado esperado es `QA_ORACLE_SCHEMA_CERT=PASS`.
6. Desplegar manualmente el backend QA en Render al SHA aprobado. Certificar API y Vercel; realizar prueba autenticada de carga de cinco fotos, rechazo de sexta, cambio de portada, eliminación, recarga y aislamiento. Verificar portada pública y detalle de la categoría.

## Reversión

Si el nuevo backend falla, desplegar el SHA anterior manteniendo la tabla aditiva. No ejecutar Down ni borrar las imágenes sin respaldo y autorización explícita.

## Ejecución excepcional autorizada (25 de noviembre de 2026)

El workflow `qa-categorias-galeria-apply-once.yml` aplica únicamente este delta desde el SHA padre aprobado, después de repetir respaldo/restore, verificar SQL generado por EF y ensayar en MySQL aislado. No se dispara por cambios de código ordinarios ni desde otras ramas. La certificación read-only original permanece intacta. No repetir una escritura si el estado de la base cambió o si el ensayo no aprueba. El despliegue de Render y la prueba Cloudinary autenticada son verificaciones separadas.
