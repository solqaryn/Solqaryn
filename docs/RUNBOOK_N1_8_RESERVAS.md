# Runbook operativo — ERP-N1.8 Reservas de inventario

## 1. Propósito

Guía de operación, diagnóstico y recuperación de Reservas de inventario sin alterar la autoridad física ni introducir correcciones manuales peligrosas.

Este runbook complementa `RUNBOOK_N1_8_RESERVAS_MIGRACION.md`; el primero cubre runtime, el segundo despliegue/migración.

## 2. Autoridad operativa

Siempre partir de:

```text
ExistenciaVariante = autoridad de StockFisico / StockReservado / StockDisponible
ReservaInventario   = documento que explica el compromiso
Clave física        = Variante + Almacén + Ubicación
```

No calcular disponibilidad únicamente sumando documentos de Reserva si el objetivo es autorizar una operación de inventario.

## 3. Operaciones normales

### 3.1 Crear borrador

1. seleccionar existencia física real;
2. registrar cantidad positiva;
3. evitar duplicar la misma clave física en el documento;
4. si se informa expiración, debe ser futura;
5. guardar como Borrador.

No cambia `StockReservado`.

### 3.2 Editar

Sólo Borrador. Cambiar detalles no reserva stock hasta Activar.

### 3.3 Activar

1. autenticar y exigir `MovimientosInventario:Confirmar`;
2. bloquear claves físicas;
3. validar disponibilidad actual;
4. incrementar `StockReservado`;
5. cambiar estado a Activa;
6. persistir auditoría estricta en la misma unidad de trabajo;
7. confirmar transacción.

Si falla cualquier paso, no considerar la reserva activa.

### 3.4 Consumir

Sólo Activa. Registra consumo, retira el compromiso reservado y pasa a Consumida. No ejecutar manualmente un decremento adicional de reservado tras una respuesta exitosa.

### 3.5 Liberar

Sólo Activa. Requiere motivo operativo; retira el compromiso y pasa a Liberada.

### 3.6 Expirar

Sólo Activa y únicamente cuando la fecha de expiración fue alcanzada. Una petición prematura debe fallar cerrado.

### 3.7 Cancelar

- Borrador: cancela sin mutar reservado.
- Activa: retira reservado y cancela.
- Estado terminal distinto: rechazar o responder idempotentemente según el contrato del caso de uso; nunca duplicar el decremento.

## 4. Diagnóstico de “sin disponibilidad”

Comprobar, en este orden:

1. `ProductoVarianteId` correcto;
2. `AlmacenId` correcto;
3. `UbicacionAlmacenId` correcto/nulo según la existencia;
4. `StockFisico` actual;
5. `StockReservado` actual;
6. reservas activas relacionadas;
7. operaciones concurrentes recientes;
8. CorrelationId y auditoría del intento.

No aumentar stock, borrar reservas ni editar `StockReservado` directamente para forzar el éxito.

## 5. Diagnóstico de divergencia Reserva ↔ StockReservado

Síntoma: una reserva activa aparenta no estar representada en reservado, o una liberación encuentra `StockReservado` menor que su demanda.

Procedimiento:

1. detener reintentos automáticos sobre la misma clave;
2. capturar ID/número de reserva y CorrelationId;
3. revisar estado y timestamps del lifecycle;
4. revisar auditoría estricta de la transición;
5. revisar la existencia física exacta;
6. revisar otras reservas activas de esa clave;
7. verificar si hubo operación física concurrente o rollback;
8. si existe inconsistencia demostrada, preparar corrección **forward** con prueba y evidencia; no aplicar DML manual productivo desde este runbook.

La validación `StockReservado < cantidad a retirar` es un seguro fail-closed: no debe neutralizarse.

## 6. Fallo de auditoría

Desde N1.8.F la auditoría de mutaciones es parte de la transacción crítica.

Resultado esperado:

```text
Falla RegistrarEstrictoAsync
        ↓
propaga excepción
        ↓
IUnitOfWork revierte la operación
        ↓
la Reserva/StockReservado no deben considerarse confirmados
```

No cambiar a auditoría tolerante para “desbloquear” una incidencia. Corregir el subsistema de persistencia/auditoría y reintentar con idempotencia.

## 7. CorrelationId

Usar el CorrelationId/TraceIdentifier emitido por la plataforma para correlacionar API, auditoría y logs. El sistema sanea el identificador antes de usarlo como evidencia; no asumir que un header arbitrario enviado por cliente es confiable.

No registrar secretos, tokens o payloads sensibles completos en logs de diagnóstico.

## 8. Reintentos e idempotencia

- GET puede reintentarse de forma segura.
- Las transiciones mutables sólo deben reintentarse después de confirmar el estado real.
- Si la primera respuesta se perdió, consultar la Reserva antes de volver a activar/liberar/cancelar.
- Un estado terminal no debe provocar un segundo ajuste de reservado.
- No paralelizar dos transiciones opuestas del mismo documento.

## 9. Expiración

La fecha de expiración es una regla de negocio, no una autorización para mutar sin verificar estado.

Checklist:

- fecha configurada;
- `UtcNow >= FechaExpiracion`;
- estado actual Activa;
- lock físico adquirido;
- reservado suficiente;
- auditoría estricta disponible.

## 10. Permisos

| Acción | Permiso |
|---|---|
| Consultar | `MovimientosInventario:Ver` |
| Crear | `MovimientosInventario:Crear` |
| Editar | `MovimientosInventario:Editar` |
| Activar/Consumir | `MovimientosInventario:Confirmar` |
| Liberar/Cancelar | `MovimientosInventario:Anular` |
| Expirar | `MovimientosInventario:CambiarEstado` |

Una denegación 401/403 se corrige en autenticación/RBAC; no se elimina `[Authorize]` ni `RequierePermiso`.

## 11. Migración y rollback de esquema

Seguir `docs/RUNBOOK_N1_8_RESERVAS_MIGRACION.md`.

Reglas:

- preflight antes de aplicar;
- no inventar backfill de reservas;
- verificar FKs/constraints/postcheck;
- si ya hay datos reales, no ejecutar DROP destructivo como rollback operativo;
- preferir corrección forward o restauración controlada compatible;
- Producción sólo se toca mediante procedimiento explícitamente autorizado fuera de esta certificación.

## 12. Validación después de incidencia

Como mínimo, en un entorno autorizado:

1. backend Release + unitarias;
2. integración MySQL/migraciones si se tocó persistencia;
3. pruebas N18 de reservas;
4. Acceptance/Playwright si afectó API/UI/lifecycle;
5. Fase8 por seguridad/runtime;
6. M13 cuando el cambio sea transversal o de cierre.

Baseline conocido bueno antes del cierre documental:

```text
95baf2763b912e1015a3bdd25a37aca649e34c37
Development #32035509947 SUCCESS
Acceptance  #32035509805 SUCCESS
Fase8       #32035509973 SUCCESS
M10         #32035509930 SUCCESS
M13         #32035509964 SUCCESS
```

## 13. Acciones prohibidas

- editar `StockReservado` manualmente para esconder una divergencia;
- crear una segunda tabla de saldo reservado como autoridad;
- usar Variante sin Almacén/Ubicación donde la existencia sea física;
- desactivar auditoría estricta;
- aceptar el CorrelationId bruto del cliente como evidencia confiable;
- forzar push, mergear a `main` o tocar Producción para cerrar un gate.
