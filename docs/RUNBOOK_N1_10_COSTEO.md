# Runbook ERP-N1.10 — Costeo

## Propósito

Guía operativa para diagnosticar, cambiar y recuperar la política de costeo sin reescribir historia ni romper la autoridad empresarial.

## Precondiciones

1. Trabajar únicamente en el entorno autorizado.
2. Confirmar que existe exactamente una empresa activa resoluble por la aplicación.
3. Confirmar permisos `MovimientosInventario/Ver` para consulta y `MovimientosInventario/Editar` para cambio.
4. Verificar health/readiness antes de una operación administrativa.
5. No modificar manualmente registros históricos de política.

## Consulta

- Política vigente: `GET /costeo-inventario/politica-vigente`.
- Historial: `GET /costeo-inventario/politicas`.
- Catálogo: `GET /costeo-inventario/metodos`.

Los filtros de fecha se expresan en UTC. Un rango invertido debe ser rechazado.

## Cambio controlado

1. Consultar la política vigente.
2. Seleccionar un método del catálogo canónico servido por backend.
3. Registrar un motivo explícito de 3–500 caracteres.
4. Ejecutar `PUT /costeo-inventario/politica-vigente`.
5. Confirmar que la respuesta representa la nueva política vigente.
6. Consultar historial y comprobar que la versión anterior quedó cerrada, no editada.
7. Revisar auditoría correlacionada de la mutación.

Solicitar el mismo método ya vigente es idempotente: no debe crear versión ni auditoría duplicada.

## Diagnóstico

### No existe empresa activa

Comportamiento esperado: fail-closed. No crear política ni inventar empresa. Reconciliar configuración empresarial antes de reintentar.

### Existen múltiples autoridades empresariales

No forzar selección implícita. Corregir la ambigüedad de configuración y repetir preflight.

### Error durante la transacción

La operación debe considerarse no completada hasta verificar persistencia e historial. No repetir ciegamente mutaciones; consultar primero la política vigente.

### UI no permite editar

Verificar permiso relacional `MovimientosInventario/Editar`. No agregar bypass visual ni administrativo.

## Rollback funcional

Una política histórica no se borra ni se reescribe. Para volver a un método anterior:

1. Confirmar el método deseado.
2. Crear una nueva versión mediante el endpoint normal con motivo de reversión.
3. Verificar nueva vigencia, cierre de la anterior y auditoría.

Esto preserva la trazabilidad temporal.

## Rollback técnico

La estrategia de datos es forward-only:

- No ejecutar `Down` destructivo sobre Producción como respuesta ordinaria a incidentes.
- Corregir defectos de esquema mediante nueva migración forward.
- Mantener snapshots/backups según M11.
- Restaurar un backup solamente bajo procedimiento formal de recuperación, tras validar alcance, punto de restauración y pérdida potencial de datos.

## Validación posterior

Tras cualquier cambio relevante ejecutar/verificar, según alcance:

- backend Release + unitarias;
- frontend lint/build productivo;
- integración MySQL y migraciones;
- contrato HTTP/RBAC;
- seguridad HTTP;
- aceptación funcional y M13 cuando el changeset lo requiera.

Baseline certificado de referencia: `142435e063767e6106bdc8dad2ccb9dd7645f137` con Desarrollo `32134812652`, Fase8 `32134812633`, M10 `32134812567`, aceptación `32134812695` y M13 `32134812757` en SUCCESS.