# SOLQARYN V5 — C08-02.M01.03 — Inventario de código y evidencia para mesas, QR y autoservicio

**Autor:** PRIMARY DEV 06 — JAVIER; TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Fecha:** 2026-11-14. **Fuente ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, C08-02.M01.03. **Tipo:** lectura y contraste de fuentes reales, no código runtime, pruebas HTTP ni despliegue.

## 1. Propósito y método de inspección

La microtarea **«Leer contratos, código y evidencia disponibles sin asumir certificación»** audita siete blobs reales del HEAD de `dev` a partir del alcance `C08-02.M01.01` y de los actores/superficies `C08-02.M01.02`. Se distingue entre capacidad ERP existente **en estos archivos**, propuesta restaurante y evidencia futura pendiente. Se inspeccionaron archivos en Domain, Application, Infrastructure y Angular; **no se ejecutaron** HTTP, base MySQL, JWT, QR, e2e o permisos por haber leído sus fuentes. El SHA de cada blob permite reproducir y detectar deriva en revisiones.

## 2. Inventario exacto de siete fuentes (lectura, no certificación)

| Fuente en repositorio | Blob SHA-1 exacto leído | Observación sustentada por código |
| --- | --- | --- |
| `backend/src/Domain/Entities/PedidoVenta.cs` | `12c265e9b3dbd6d27883d52e3b2a0b05783eb0cf` | El documento comercial de pedido declara EmpresaId nullable legacy, estado Borrador/Confirmado/Anulado e IdempotencyKey/Fingerprint; no modela por sí solo QR/mesa restaurante. |
| `backend/src/Domain/Entities/PreparacionPedidoVenta.cs` | `6ebf571dab2809fd768c492fa25bc341fdba7962` | Trazabilidad de preparación física de PedidoVenta confirmado y reserva de inventario; explícitamente no es autoridad de stock ni documento comercial de cocina restaurante. |
| `backend/src/Application/Services/PedidoVentaService.cs` | `74e38548b43f9dcc870d951b203f551b00dd13d8` | Servicio Application con operaciones de pedido y normalización de clave de idempotencia; requiere estudio causal antes de reutilizarlo para mesas y QR. |
| `backend/src/Infrastructure/Repositories/PedidoVentaRepository.cs` | `c741fe677d86a6962f4c0f579ac461e4fe567dc6` | Repositorio EF con lectura/locking de pedidos y condición EmpresaId en SQL; la presencia de filtro no certifica aislamiento A/B de futuros flujos anónimos. |
| `backend/src/Infrastructure/Persistence/Configurations/PedidoVentaConfiguration.cs` | `9819d899e2200e39cec32821047a194545b761ec` | Configuración EF para PedidosVenta, checks de estados e idempotencia e índice por EmpresaId/Id; no demuestra una relación MesaId/SucursalId para restaurante. |
| `backend/src/Domain/Entities/FacturaPago.cs` | `c35f5e678a2a267f8dd56fedefd24450ae36ee7b` | Entidad de pago de factura con EmpresaId nullable legacy y FacturaId, distinta de un cierre de mesa; ninguna captura de pago real se ensaya aquí. |
| `frontend/src/app/features/pedidos-venta/pedido-venta.model.ts` | `57f4437372c734824a40be8e9731c0e75c0f18ba` | Modelo Angular tipado de pedido comercial y estados Borrador/Confirmado/Anulado; no contiene en el archivo auditado contrato de mesa/QR/pedido autoservicio. |

**Lectura transversal:** existen contratos ERP de pedido comercial, confirmación/preparación, idempotencia y pago de factura en rutas auditadas. Esto **no implica** que exista un módulo restaurante integral ni prueba la ausencia exhaustiva de otros componentes: la inspección está limitada a los siete archivos listados. El módulo de QR/mesa, la publicación de menú, la vinculación a sucursal y la autorización anónima son **requisitos futuros cuya cobertura causal no queda demostrada por estos blobs**.

## 3. Mapa de frontera y compatibilidad

| Necesidad C08-02 | Evidencia parcial en archivos | Lo que NO puede inferirse |
| --- | --- | --- |
| Pedido y transición | `PedidoVenta` y `PedidoVentaService` presentan estados e idempotencia comercial | No se confirma estado mesa, cocina restaurante o cobro |
| Preparación | `PreparacionPedidoVenta` modela preparación de pedido confirmado de inventario | No se confirma workflow de mesas/cocina POS |
| Persistencia tenant | `PedidoVentaRepository` y `PedidoVentaConfiguration` contienen filtro/índice de empresa | No hay test A/B o firma de QR en esta inspección |
| Dinero y factura | `FacturaPago` modela pago de factura | No hay autorización para cobro, refund o cierre de mesa |
| Cliente Angular | `pedido-venta.model.ts` contiene DTO de pedido comercial | No hay aceptación UI de mesa/QR en este archivo |
| Menú y catálogo | La reutilización futura debe respetar catálogo, precio y stock canónicos | No se contrastó aquí una publicación de menú activa o su disponibilidad |

**Ruta de impacto futura:** navegador/cámara → Angular público o interfaz operador → API (valida host/tenant, JWT/RBAC cuando aplica) → Application (contexto, idempotencia, estado) → Domain (invariantes) → Infrastructure/MySQL (transacción y filtro empresa/sede) → proyección mínima. No se crea ni conecta esa ruta en M01.03.

## 4. Diez brechas verificables G01–G10

| Brecha | Evidencia adicional obligatoria en fases posteriores |
| --- | --- |
| G01 — Mesa y sucursal | Entidad/relación de mesa con tenant+sede y prueba A/B; no inferir de PedidoVenta |
| G02 — QR falso | Especificación de token opaco/firma/revocación/expiración y rechazo uniforme, sin secrets en logs |
| G03 — Menú público | Proyección explícita de precios y visibilidad calculados por servidor, sin datos internos |
| G04 — Identidad de mesa | Demostrar que cambiar MesaId/SucursalId/EmpresaId desde cliente no altera pertenencia |
| G05 — Pedido idempotente | Reintentos concurrentes misma clave y payload distinto con transacción y no duplicación |
| G06 — Cocina/caja | Matriz de roles y transiciones específicas, incluyendo rol revocado, separadas de ERP inventario |
| G07 — Cierre de pedido | Pruebas de replay y cierre terminal sin doble factura, pago o descuento de stock |
| G08 — Integración ERP | Evidencia positiva y negativa de compatibilidad de cotización, venta, inventario, factura y devolución |
| G09 — UI/móvil | Prototipo accesible de QR/menú/cocina con estados 401/403/404/409, carga y falla offline |
| G10 — Auditoría/privacidad | Logs saneados con correlación tenant/sede/actor y tests sin JWT/PII/QR secreto |

Las brechas significan **evidencia pendiente**, no defectos runtime demostrados. Es posible que existan capacidades fuera de estos siete blobs; validar exact-HEAD y alcance de futuros repositorios/contratos antes de clasificar ausencia.

## 5. Escenarios futuros P01–P06/N01–N12 — NO_EJECUTADOS

- **P01/N01:** QR válido muestra menú A / QR B, manipulado o expirado no expone datos.
- **P02/N02:** pedido de mesa A se crea con precio canónico / cliente falsifica precio, tenant o mesa B y se rechaza.
- **P03/N03:** reintento con misma clave devuelve pedido original / payload distinto o carrera no crea duplicados.
- **P04/N04:** cocina autorizada avanza estados propios / sesión revocada o cocina B no puede actuar sobre A.
- **P05/N05:** cajero autorizado cierra una única vez / doble cierre o respuesta tardía no duplica cobro/factura/stock.
- **P06/N06:** UI móvil accesible refleja estado real / offline/timeout no presenta éxito de pago falso.

**Adicionales negativos N07–N12:** suplantación de sucursal, lectura de pedido sin autenticación, replay de QR, escritura de total/IVA por cliente, fuga de JWT en observabilidad y reversión parcial que mezcla tenants. Todos quedan **FUTUROS NO_EJECUTADOS**, no se reportan suites verdes por inspección documental.

## 6. Plan de validación causal y rollback

- **A0 estática:** comprobar siete rutas/SHAs, identidad `jmejia31`, Scope Lock STRICT, HEAD actual, elegibilidad PLAN/COLA/DAG y ninguna información sensible en documentos.
- **A1 documental:** correlación comprobable entre fuentes/afirmaciones, G01–G10 y escenarios P/N. No es prueba funcional de POS.
- **A2 diseño de seguridad:** tenant A/B, mesa ajena, JWT/RBAC, QR falso, replay, idempotencia, PII, pagos; no certificar aislamiento vivo sin suite negativa.
- **A3 integración documental:** contrastar inventario con M01.01/M01.02, capas y contratos del plan; futuras suites .NET, Angular, MySQL efímero y Playwright son requisitos de implementación, **NO_APLICA** a este diff exclusivamente Markdown.
- **A4 publicación:** sólo `docs/v5` y `CHANGELOG_AI.md`, commit fast-forward con SHA exacto, controles obligatorios `verify-solqaryn-project-scope` y procedencia V5 `completed/success`, readback institucional; cualquier check obligatorio no exitoso conserva VALIDANDO.
- **A5 independiente:** revisión por otra Task Javier diferente de autora; sin autoaprobación, sin QA/main/PROD.

**Límite operativo:** no crear QR, endpoint, migración, secret, cuentas, pagos, colas, webhooks o tareas. **Rollback:** revert Git normal del commit documental con evento BITACORA compensatorio; jamás borrar historial.
