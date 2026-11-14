# ERP-N2.1 — Solicitud de compra — Auditoría y preflight

Fecha: 2026-08-18.

## Estado real

No existe actualmente una entidad, servicio ni endpoint `SolicitudCompra` en el baseline inspeccionado de `Desarrollo`.

La capacidad existente es `Compra`, cuyo flujo documental es distinto: nace en `EstadoDocumento.Borrador`, permite edición únicamente mientras está en Borrador y pasa directamente a `Confirmada` mediante `POST /compras/{id}/confirmar`; esa confirmación actualiza inventario/Kardex y efectos financieros. El enum compartido `EstadoDocumento` sólo contiene `Borrador`, `Confirmada` y `Anulada`.

Por tanto, **N2.1 no debe implementarse reutilizando ni ampliando `EstadoDocumento` a ciegas**. Una solicitud de compra es un documento previo a la compra y su ciclo `Borrador → Solicitada → Aprobada/Rechazada` no debe disparar stock, Kardex, costo ni movimiento financiero.

## Alcance N2.1

Crear una autoridad documental independiente para solicitudes de compra:

- cabecera `SolicitudCompra`;
- detalles `SolicitudCompraDetalle`;
- estado propio y explícito;
- creación y edición en Borrador;
- envío a aprobación (`Solicitada`);
- decisión `Aprobada` o `Rechazada`;
- historial/auditoría de transiciones;
- lectura/listado paginado y filtros;
- permisos relacionales y UX correspondiente en etapas posteriores B–H.

## Fuera de alcance de N2.1.A

Esta microtarea no implementa aún:

- DDL/migración;
- entidades definitivas;
- API nueva;
- UI;
- conversión automática a orden/compra;
- recepción de mercancía;
- actualización de inventario;
- pago o movimiento financiero.

Esos cambios pertenecen a B–H y a puntos posteriores del ERP-N2 según dependencias.

## Autoridades y límites detectados

### Compra actual

`Compra` seguirá siendo la autoridad de una compra materializada. Su confirmación es una operación con efectos físicos/financieros y no debe confundirse con la aprobación administrativa de una solicitud.

### Solicitud futura

`SolicitudCompra` será la autoridad del requerimiento previo. La aprobación habilita continuidad comercial, pero por sí sola **no** incrementa existencias, no crea Kardex, no materializa costo y no genera movimiento financiero.

### Estado

Crear un enum dedicado, por ejemplo `EstadoSolicitudCompra`, con valores estables:

1. Borrador.
2. Solicitada.
3. Aprobada.
4. Rechazada.

No reutilizar `EstadoDocumento`, porque su semántica existente `Borrador/Confirmada/Anulada` es usada por documentos transaccionales y cambiarla introduciría acoplamiento/regresión innecesaria.

## Modelo recomendado para N2.1.B

### SolicitudCompra

Campos mínimos propuestos:

- `Id`.
- `NumeroSolicitud` único y estable.
- `Estado` (`EstadoSolicitudCompra`).
- `ProveedorId` nullable si el requerimiento todavía no obliga proveedor.
- snapshots mínimos de proveedor sólo si el dominio decide preservarlos; no duplicar autoridad sin necesidad.
- `FechaSolicitudUtc` nullable hasta transición a Solicitada.
- `FechaDecisionUtc` nullable.
- `SolicitadaPorUsuarioId` / nombre snapshot.
- `DecididaPorUsuarioId` / nombre snapshot.
- `MotivoRechazo` nullable y obligatorio al rechazar.
- `Notas`.
- campos estándar de auditoría técnica/herencia usados por el proyecto.
- colección de detalles.

### SolicitudCompraDetalle

Campos mínimos propuestos:

- `SolicitudCompraId`.
- `ProductoId`.
- `ProductoVarianteId` nullable según la selección operacional.
- cantidad solicitada positiva.
- costo estimado nullable; no es costo contable/materializado.
- observación.
- snapshots de identificación comercial únicamente si el patrón de Compras y trazabilidad histórica lo exige.

La unidad operativa preferida debe ser `ProductoVariante` cuando exista variante, respetando la autoridad consolidada de ERP-N1.

## Invariantes

- Borrador: editable y eliminable lógicamente según política definida.
- Solicitada: contenido congelado para revisión; no editable de forma ordinaria.
- Aprobada/Rechazada: estados terminales para N2.1 salvo una capacidad posterior explícita de reapertura/cancelación.
- Sólo `Borrador → Solicitada`.
- Sólo `Solicitada → Aprobada` o `Solicitada → Rechazada`.
- Rechazo exige motivo.
- Solicitud sin detalles no puede enviarse.
- Cantidades deben ser > 0.
- Ninguna transición de N2.1 toca stock/Kardex/costo/pagos.
- Transiciones críticas deben serializarse contra doble decisión concurrente.

## API objetivo para etapas posteriores

Propuesta de superficie consistente:

- `GET /solicitudes-compra`
- `GET /solicitudes-compra/{id}`
- `POST /solicitudes-compra`
- `PUT /solicitudes-compra/{id}`
- `POST /solicitudes-compra/{id}/solicitar`
- `POST /solicitudes-compra/{id}/aprobar`
- `POST /solicitudes-compra/{id}/rechazar`
- `DELETE /solicitudes-compra/{id}` únicamente para Borrador si el diseño final conserva eliminación lógica.

No reutilizar `POST /compras/{id}/confirmar` para aprobación.

## RBAC recomendado

Mantener el módulo relacional `Compras` salvo que el Plan Maestro introduzca un módulo separado. Acciones mínimas:

- Ver para consultas.
- Crear para nuevo borrador.
- Editar para modificación de borrador.
- una acción de transición/confirmación apropiada para `Solicitar` si el catálogo actual lo permite;
- `Aprobar`/`Rechazar` deben quedar diferenciados si el catálogo de permisos ya dispone de acciones equivalentes; si no, N2.1.B/F debe definir el mínimo cambio relacional sin bypass.

No otorgar privilegios por `EsAdministrador` ni por lógica hardcodeada de rol.

## Riesgos

1. **Acoplar solicitud con Compra:** podría causar entradas de inventario antes de recepción/compra real.
2. **Reutilizar `EstadoDocumento`:** mezclaría semánticas incompatibles y ampliaría regresión transversal.
3. **Doble aprobación concurrente:** requiere lectura/lock transaccional en la transición.
4. **Doble autoridad de proveedor/producto:** debe referenciar catálogos existentes y limitar snapshots a trazabilidad histórica.
5. **Costos estimados tratados como reales:** los valores de solicitud no deben alimentar N1.10/Kardex.
6. **Permisos insuficientemente granulares:** aprobación y rechazo son decisiones auditables y no deben depender sólo de permiso genérico de edición.

## Estrategia de transición

Es una capacidad aditiva. No requiere convertir compras históricas en solicitudes ni inventar solicitudes para documentos existentes.

- Crear tablas/contratos nuevos.
- No backfillear `SolicitudCompra` desde `Compra`.
- Mantener flujo actual de Compra mientras puntos posteriores integran la conversión formal.
- Cualquier vínculo futuro entre solicitud aprobada y orden/compra debe ser FK explícita y no un string polimórfico.

## Rollback

Antes de uso productivo, los cambios N2.1 deben permanecer aditivos y reversibles en Desarrollo. Tras existir datos reales, preferir corrección forward y preservación histórica. No borrar solicitudes aprobadas/rechazadas para revertir una decisión.

## Matriz de pruebas requerida

- creación de borrador válido;
- rechazo de solicitud sin detalles al enviar;
- edición permitida sólo en Borrador;
- transición Borrador→Solicitada;
- transición Solicitada→Aprobada;
- transición Solicitada→Rechazada con motivo;
- rechazo de transiciones ilegales/repetidas;
- concurrencia de doble decisión;
- persistencia paginada/filtros;
- RBAC HTTP;
- auditoría estricta de transiciones;
- cero mutación de inventario/Kardex/costeo/finanzas durante solicitar/aprobar/rechazar;
- migración fresh install/upgrade y snapshot EF;
- frontend/E2E en etapas E/G.

## Criterio de cierre N2.1.A

Preflight concluido: el gap está identificado, el boundary con `Compra` es explícito, no existe legado `SolicitudCompra` que migrar, se definieron autoridad, riesgos, transición, rollback, API objetivo y matriz de pruebas sin adelantar implementación de B/C.