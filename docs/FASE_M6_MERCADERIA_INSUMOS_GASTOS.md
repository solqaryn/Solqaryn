# FASE M6 — Mercadería, insumos administrativos y gastos

Estado: **COMPLETADA / CERTIFICADA AUTOMÁTICAMENTE**

Fecha de cierre técnico: 2026-08-09 (Honduras)
Rama exclusiva: `Desarrollo`
PR oficial: `#2 Desarrollo -> main` (abierto y borrador)
Producción: no intervenida

## 1. Objetivo

Consolidar la separación operativa y contable entre:

1. **Mercadería vendible**: inventario físico destinado a ventas.
2. **Insumo administrativo**: inventario físico de consumo interno, nunca vendible.
3. **Gasto financiero/operativo**: movimiento monetario que no representa existencias y por tanto no debe modelarse como Producto.

M6 reutiliza la arquitectura ya existente (`TipoInventario`, `ConsumoInsumo`, `MovimientoFinanciero`, Producto/ProductoVariante) y evita crear sistemas paralelos.

## 2. Resultado funcional

### 2.1 Mercadería vendible

- `TipoInventario.MercaderiaVenta` continúa siendo el tipo comercial vendible.
- Las cantidades físicas siguen teniendo como fuente de verdad `ProductoVariante.Cantidad`.
- La valoración potencial de venta se calcula exclusivamente sobre mercadería vendible.
- Dashboard y Finanzas separan la valoración de mercadería respecto de insumos administrativos.

### 2.2 Insumos administrativos

- `TipoInventario.InsumoAdministrativo` representa existencias físicas de uso interno.
- Los insumos se excluyen del flujo comercial de venta y scanner/autocomplete comercial según las protecciones ya existentes.
- Se preserva el bloqueo defensivo de persistencia que impide materializar `VentaDetalle` para un insumo administrativo incluso si otra capa intentara saltarse las validaciones normales.
- `ConsumoInsumo` continúa siendo el agregado transaccional de salida de inventario administrativo, con trazabilidad de usuario, motivo, producto/variante, cantidad y snapshots históricos.
- Los consumos administrativos no se convierten en ventas ni en ingresos comerciales.

### 2.3 Gastos financieros

- Los gastos siguen representándose mediante `MovimientoFinanciero`, no como productos ni inventario ficticio.
- El alta manual ahora falla cerrada ante tipos o categorías desconocidos: no convierte silenciosamente entradas inválidas a `Egreso/GastoOperativo`.
- Las categorías automáticas documentales (`Venta`, `Compra`) no pueden falsificarse mediante un movimiento manual.
- `GastoOperativo` exige semántica de `Egreso`.
- El formulario financiero sincroniza tipo y categoría para impedir combinaciones semánticamente inválidas.
- `UtilidadNeta` descuenta únicamente gastos operativos manuales válidos, pagados y no anulados; otros egresos no se mezclan automáticamente con ese indicador.

## 3. Valoración separada

Se incorporaron consultas de repositorio por `TipoInventario` para que las métricas financieras no mezclen activos físicos con distinta finalidad.

El resumen financiero expone:

- `ValorInventarioCostoMercaderia`;
- `ValorInventarioCostoInsumosAdministrativos`;
- `ValorInventarioCosto` total físico;
- `ValorPotencialVentaMercaderia`;
- `GastosOperativos`;
- utilidad potencial comercial calculada sobre mercadería, no sobre insumos.

El Dashboard administrativo también distingue:

- unidades de mercadería;
- unidades de insumos administrativos;
- costo de mercadería;
- costo de insumos;
- valor comercial potencial de la mercadería.

Los usuarios sin privilegio administrativo continúan sin recibir valores sensibles de costo.

## 4. Integridad y seguridad

M6 conserva y verifica las siguientes defensas:

- insumos administrativos no vendibles;
- variantes como fuente física de inventario;
- consumos de insumos transaccionales;
- movimientos de inventario trazables;
- movimientos financieros automáticos protegidos frente a anulación manual directa;
- categorías automáticas de Venta/Compra no falsificables desde alta manual;
- permisos existentes para inventario, consumos y finanzas;
- snapshots históricos de consumo;
- sin cambios destructivos sobre documentos históricos.

No fue necesaria una nueva migración de esquema para el núcleo de M6; se reutilizó la estructura versionada existente y se endurecieron servicios, consultas, DTOs, UI y pruebas.

## 5. Pruebas M6 e incidencias resueltas

Se añadieron/reforzaron pruebas para:

- separación de valoración entre Mercadería e InsumoAdministrativo;
- valor potencial de venta exclusivo de mercadería;
- rechazo de tipo financiero inválido;
- rechazo de categoría financiera inválida;
- `GastoOperativo` exclusivamente como egreso;
- rechazo de categorías automáticas en movimientos manuales;
- cálculo de gastos operativos sin mezclar otros egresos;
- cálculo de utilidad neta;
- protección existente contra venta de insumos administrativos.

Durante el cierre se detectaron dos fallos consecutivos de compilación exclusivamente en los mocks de `FinanzasServiceTests`:

1. Moq no admite argumentos opcionales dentro de determinados expression trees cuando se omiten en el `Setup`.
2. El primer ajuste intentó pasar `CancellationToken` a métodos cuya firma real recibe únicamente `int? usuarioId`.

Causa raíz: desalineación entre las expresiones de prueba y las firmas reales de `IVentaRepository` / `ICompraRepository`; no era un defecto de la lógica funcional de M6.

Corrección final:

- se alinearon explícitamente los mocks con `int? usuarioId` usando `(int?)null`;
- no se debilitó ninguna prueba;
- no se eliminaron tests;
- no se modificó la lógica de negocio para forzar CI verde;
- se repitieron los gates completos hasta obtener éxito real.

## 6. Evidencia CI

### 6.1 Checkpoint funcional

HEAD funcional certificado: `552b52e270f2f42dcc2a49215782efde73023d26`

Resultados:

- `Desarrollo - Compilación y pruebas` — run `31344665928` — **SUCCESS**.
- `Desarrollo - aceptación funcional integral` — run `31344665924` — **SUCCESS**.
- `Fase 2 - Auditoría de configuración y dependencias` — run `31344665923` — **SUCCESS**.
- `Bloque 2C.1 - Variante técnica y migración` — run `31344665894` — **SUCCESS**.
- `Fase 8 - Validación completa automatizada` — run `31344665921` — **SUCCESS**.
- `Solqaryn CI` — run `31344665938` — **SKIPPED**; no se contabiliza como validación verde.

Dentro de `Desarrollo - Compilación y pruebas` quedaron verificados, entre otros:

- Backend Release y pruebas: SUCCESS.
- Frontend lint/build producción: SUCCESS.
- Docker y aislamiento: SUCCESS.
- Higiene del repositorio: SUCCESS.
- migraciones e integración MySQL 8.4: SUCCESS.

### 6.2 Checkpoint documental de cierre

HEAD documental verificado antes de este resumen final: `136d918dcc080cd49bda1b9d89703fdd6245af21`.

Resultados del HEAD documental:

- `Desarrollo - Compilación y pruebas` — run `31345152745` — **SUCCESS**.
- `Desarrollo - aceptación funcional integral` — run `31345152728` — **SUCCESS**.
- `Fase 2 - Auditoría de configuración y dependencias` — run `31345152727` — **SUCCESS**.
- `Bloque 2C.1 - Variante técnica y migración` — run `31345152784` — **SUCCESS**.
- `Fase 8 - Validación completa automatizada` — run `31345152738` — **SUCCESS**.
- `Solqaryn CI` — run `31345152747` — **SKIPPED**; no se contabiliza como fallo ni como validación verde.

Por tanto, tanto el código funcional como el documento de certificación de M6 superaron los gates relevantes del repositorio.

## 7. Alcance no realizado

- No se modificó `main`.
- No se creó ninguna rama.
- No se fusionó PR #2 ni se habilitó auto-merge.
- No se modificó Producción.
- No se ejecutaron migraciones contra Producción.
- No se modificaron secretos o credenciales productivas.

## 8. Dictamen final

M6 deja separados los tres conceptos de negocio:

`Mercadería vendible != Insumo administrativo físico != Gasto financiero`

La mercadería conserva inventario y valor comercial; los insumos conservan inventario físico y consumo interno pero no son vendibles; los gastos afectan Finanzas sin fabricar existencias ficticias.

No quedan fallos funcionales o de CI conocidos atribuibles a M6 después del cierre técnico y documental.

**FASE M6: COMPLETADA / VALIDADA / CERTIFICADA AUTOMÁTICAMENTE.**

## 9. Estado del plan y siguiente fase

Fases empresariales cerradas hasta este punto:

- M0 — Auditoría y mapa de impacto: COMPLETADA.
- M1 — Saneamiento relacional integral: COMPLETADA / CERTIFICADA.
- M2 — Motor de variantes multidimensionales: COMPLETADA / CERTIFICADA.
- M3 — Configuración fiscal ISV/ISC: COMPLETADA / CERTIFICADA.
- M4 — Estado persistente de filtros y navegación: COMPLETADA / CERTIFICADA.
- M5 — Clientes y segmentación: COMPLETADA / CERTIFICADA.
- M6 — Mercadería, insumos administrativos y gastos: **COMPLETADA / CERTIFICADA**.

Siguiente fase oficial: **M7 — Costos de envío profesionales**.

Alcance previsto de M7 según el Plan Maestro:

- zona;
- ciudad;
- departamento;
- modalidad/tipo de envío;
- precio;
- prioridad;
- vigencia;
- activo;
- predeterminado;
- historial;
- snapshots de Venta/Factura;
- eliminación/desactivación segura.

M7 deberá construir sobre la restricción de costo de envío predeterminado ya reforzada en M1, sin crear una segunda fuente de verdad y conservando historial de documentos confirmados.
