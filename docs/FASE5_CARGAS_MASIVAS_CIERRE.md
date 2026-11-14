# Fase 5 — Cargas masivas e importación controlada

Fecha de cierre técnico: 2026-07-29  
Rama autorizada: `Desarrollo`  
PR oficial: #2 (`Desarrollo -> main`, abierto y en borrador)

## Alcance completado

- Plantillas oficiales CSV y XLSX.
- Validación previa sin persistir información de negocio.
- Vista previa normalizada por fila con acción Crear/Actualizar.
- Confirmación explícita dentro de una transacción completa.
- Historial y trazabilidad por usuario.
- Informe descargable de errores en CSV y XLSX.
- Idempotencia por SHA-256 del archivo y tipo de carga.
- Permisos separados para ver, validar, confirmar, exportar y consultar historial.
- Cobertura para clientes, proveedores, colores, productos, variantes e inventario inicial.

## Seguridad

- Máximo 5 MB y 2,000 filas.
- Solo CSV y XLSX sin macros.
- Rechazo de fórmulas y protección contra inyección al exportar CSV/XLSX.
- Inspección del contenedor ZIP de XLSX antes del parser.
- Límites de entradas, tamaño descomprimido y relación de compresión.
- Bloqueo de rutas internas inseguras.
- Bloqueo MySQL por carga para impedir confirmaciones concurrentes.
- Reversión completa ante errores durante la confirmación.
- No se almacena el archivo original; se conserva la vista previa normalizada y los errores.

## Migración

`20260729040900_Fase5CargasMasivas`

Crea de forma aditiva:

- `CargasMasivas`
- `CargaMasivaErrores`
- índice único de idempotencia por `Tipo + HashArchivo`
- índices de estado/fecha y fila de error
- relación con eliminación en cascada de errores

## Certificación

Commit funcional certificado: `679df36c5fd22944d41757d2fac05412aca1450b`

- Desarrollo - Compilación y pruebas: `30422503573` — success.
- Desarrollo - aceptación funcional integral: `30422503560` — success.
- Auditoría de configuración y dependencias: `30422503554` — success.

Los commits posteriores a la certificación funcional contienen únicamente documentación de cierre.

## Restricciones preservadas

- `main` no fue modificada.
- No se crearon ramas nuevas.
- No se fusionó el PR #2.
- No se habilitó auto-merge.
- No se aplicaron migraciones en Producción.
- No se modificaron recursos ni credenciales productivas.
