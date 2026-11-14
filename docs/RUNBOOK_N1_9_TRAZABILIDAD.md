# Runbook N1.9 — Trazabilidad por lote, serie y vencimiento

## Objetivo

Operar y diagnosticar N1.9 sin inventar identidades, sin corregir Producción manualmente y sin romper la autoridad de `ExistenciaVariante`.

## Preflight

1. Confirmar variante, almacén/ubicación y política de trazabilidad.
2. Verificar si existe stock físico antes de habilitar una dimensión nueva.
3. Revisar lotes/series activos y vencimientos pendientes.
4. Confirmar que no exista una identidad equivalente ya registrada.
5. Correlacionar con auditoría y `TraceIdentifier` cuando se investigue una mutación.

## Incidentes

### Serial duplicado

- no borrar el registro existente;
- identificar variante y payload de ambas solicitudes;
- si coinciden, tratar como idempotencia; si difieren, conservar fail-closed y corregir el origen de la petición.

### Lote no desactivable

- comprobar series activas y política de la variante;
- resolver primero las identidades dependientes mediante el flujo empresarial correspondiente;
- no desactivar por DDL/DML manual.

### Activación de trazabilidad bloqueada por stock

- no crear identidades ficticias;
- ejecutar adopción/reconciliación explícita con evidencia;
- sólo después habilitar la dimensión requerida.

### Vencimiento inconsistente

- verificar política `ControlaFechaVencimiento` y lote;
- no inventar fechas de vencimiento históricas;
- aplicar corrección forward documentada.

## Rollback

Las tablas/identidades de trazabilidad son históricas. Si ya existen referencias reales, no usar `Down` destructivo como procedimiento operativo. Preferir forward-fix o restauración completa compatible con la base y el código.

## Verificación posterior

- CI backend/unitarias verde;
- migraciones/snapshot sin diferencias pendientes;
- M13 y aceptación integral verdes;
- RBAC y auditoría sin bypass;
- PR #2 continúa Draft y sin merge;
- `main` y Producción permanecen intactos.