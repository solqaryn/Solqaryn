# SOLQARYN V5 — C07-01.M01.05 — Criterios observables de aceptación del catálogo universal

**Autor:** PRIMARY DEV 03 — JAVIER; **fecha:** 2026-11-14; **fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, C07-01.M01.05. Esta es una **entrega documental M01**, no código de catálogo, migración, revisión de seguridad runtime ni habilitación de M01.06/M02/M03. Todos los escenarios P/N siguientes son **FUTUROS NO_EJECUTADOS**. El criterio aquí aceptado es la integridad de esta especificación y su trazabilidad, no que el ERP ya cumpla los escenarios.

## Alcance, evidencia y fronteras

- Resultado institucional: catálogo que conserva productos, servicios, variantes, categorías, impuestos, atributos, precios y la compatibilidad ERP, con matriz de ownership A/B verificable. No duplicar módulos actuales ni tratar una pantalla como autorización.
- Contratos revisados de M01: `c07-01-m01-01-alcance-catalogo-universal.md` blob `4f91db421fe4c5e7ad5e0ea080035eabe8f8c1dd` (I01–I12); `c07-01-m01-02-actores-interfaces-catalogo-universal.md` blob `30e4d0c677112bf490cc937ba6700cd531d29376` (A/I/B); `c07-01-m01-03-inventario-fuentes-catalogo-universal.md` blob `8fa23417014077135ba6366f89635291f78e2a4f` (G01–G08); `c07-01-m01-04-riesgos-supuestos-decisiones-catalogo-universal.md` blob `b51e9047954ac15912d77fde3dfbe656b6693301` (R01–R12, S01–S08, D01–D08 PENDIENTES).
- Superficies efectivamente inventariadas, no auditadas exhaustivamente: `Producto`, `ProductoVariante`, `Categoria`, `Impuesto`, `PublicStoreBinding`, `ProductoService`, `ProductoRepository`, `ProductosController`, `ProductoVariantesController`, `TiendaController`, `CatalogoPublicoService`, `frontend/src/app/services/producto.service.ts`. `Producto.EmpresaId` y `ProductoVariante.EmpresaId` son nullable; `Categoria` no declara `EmpresaId`. Ninguno de estos hechos autoriza exposición global.
- El servidor valida JWT, membresía actual, rol, empresa y sucursal para operaciones privadas. Host/storeKey público requiere binding verificado, DTO mínimo y partición de caché; el cliente no decide `EmpresaId`, costo ni impuesto. Los snapshots de facturas y el historial de stock deben preservarse. D01–D08 no están resueltas.

## Matriz de aceptación funcional y negativa (oráculos para fases futuras)

**Condición común de ejecución futura:** fixtures independientes empresa A/B, usuarios autorizados y revocados, sucursales separadas, producto con `EmpresaId=NULL`, variante activa/inactiva, categoría e impuesto, host A/B, factura histórica inmutable; datos aislados y eliminables en CI efímera. Cada ensayo futuro deberá registrar endpoint/método, fixture, resultado, log sin secreto, SHA exacto, salida reproducible y rollback; si no hay un flujo implementado, el caso queda PENDIENTE, jamás PASS.

| Par | Positivo observable FUTURO | Negativo observable FUTURO / denegación | Capas y riesgo |
| --- | --- | --- | --- |
| P01/N01 | Usuario A con membresía vigente lista, crea y consulta solo sus productos | JWT A con ID producto B o `EmpresaId=B` forzado no consulta ni muta B | API/Application/EF; R01 R09 |
| P02/N02 | Variante A vinculada a producto A respeta SKU y relaciones | Variante B bajo padre A, relación o imagen B es rechazada sin escritura | Domain/Application/EF; R02 R11 |
| P03/N03 | Producto histórico NULL se conserva para reconciliación controlada, sin acceso público | NULL no funciona como comodín global ni permite write o publicación | Repositorio/EF; R01 S01 |
| P04/N04 | Categoría/atributo solo se asocia según política de ownership aprobada | Referencia de categoría ajena se rechaza; no inferir tenant desde `Categoria` | Application/EF; R03 D01 |
| P05/N05 | Impuesto de vigencia y contexto válidos produce monto y snapshot trazables | Tasa ajena, expirada o de otra jurisdicción no se aplica | App/Impuesto/Factura; R04 S04 |
| P06/N06 | Cambio de tarifa afecta nuevos documentos sin reescribir facturas históricas | Update de catálogo no altera total ni snapshot de factura cerrada | ERP/UnitOfWork; R05 D04 |
| P07/N07 | Stock consolidado y variantes activas concuerdan con política vigente | Variante inactiva/soft-delete no infla existencia ni reabre lote vencido | Inventario/Domain/DB; R06 |
| P08/N08 | Gestor autorizado de sucursal A ve y modifica únicamente stock permitido | Mismo usuario con sucursal B no ajusta inventario de A | JWT/RBAC/stock; R06 R09 |
| P09/N09 | Servicio no inventariable tiene precio y flujo comercial sin movimiento físico | Servicio no crea inventario/serie/lote ni cobra sin contrato aprobado | Domain/ERP; S05 D06 |
| P10/N10 | Host registrado A resuelve solamente productos publicados de A | Host desconocido o storeKey ajeno no resuelve oferta de otra empresa | PublicStoreBinding/API; R08 S06 |
| P11/N11 | DTO público de A ofrece solo campos publicados y precio permitido | Respuesta pública no contiene costo, tokens, PII, stock privado ni draft | Tienda/DTO; R07 |
| P12/N12 | Caché separada por identidad pública A/B e invalidación al actualizar oferta | Consulta B después de A nunca reutiliza listado/imagen de A | CatalogoPublicoService/cache; R08 G07 |
| P13/N13 | Cambio válido de membresía/rol refleja permisos actuales antes del write | JWT emitido antes de revocación no conserva capacidad de CRUD | API/RBAC/tenancy; R09 S07 |
| P14/N14 | SKU, galería de variante y rechazo de colisiones siguen política decidida | SKU ajeno, imagen cruzada, ID manipulable o conflicto concurrente no filtran datos | API/DB/Media; R10 R11 R12 |

Los criterios anteriores no prueban vulnerabilidades ni certifican el aislamiento ya implementado. Toda decisión comercial pendiente (D01–D08) requiere evidencia y, cuando aplique, aprobación separada antes de implementar o habilitar el caso. Ante un criterio no implementable por ausencia de contrato, registrar NO_GO causal y no inventar datos o permisos.

## Evidencia mínima de aceptación de implementación futura

1. **A1 funcional:** casos positivos con fixtures idempotentes; comparar DTO y estado final antes/después, incluyendo facturas históricas y stock.
2. **A2 seguridad:** HTTP directo sin confiar en Angular, pruebas A/B con membership revocado, IDs adivinables, host manipulado, campos privados, cache y consultas legacy NULL; validar denegación y ausencia de enumeración/PII.
3. **A3 integración:** xUnit .NET 10 y EF Core con MySQL 8.4 efímero cuando intervengan persistencia, locks o impuestos; Angular/Playwright para UI y accesibilidad cuando se modifique frontend; ninguna prueba SKIPPED/FAILED se interpreta como PASS requerida.
4. **A4 publicación:** GitHub Actions `completed/success` contra `head_sha` exacto de cambios de código, Scope Lock STRICT, trazabilidad de diff, readback PLAN/COLA/BITACORA y rollback no destructivo.
5. **A5:** auditoría de una **Task Javier distinta** del autor. LISTO_DEV documental no autoriza QA, main, PROD, dominio, migración, secretos, pagos o nuevas integraciones.

## Gate de esta M01.05 y reversión

- **DAG individual:** C07-01.M01.05, fila 975, F=VALIDADO/G=SI; `COLA!G/O=SI` y `PLAN_MAESTRO!H=PENDIENTE/I=vacío` al claim. Predecesoras documentales de C01-01..04, C02-01..04, C05-01..04, M01.01–05 = 60/60 LISTO. Este gate es solo preparación, sin liberar automáticamente M01.06, IAM, MySQL ni entornos.
- **Impacto real:** nuevo archivo `docs/v5/` y encabezado autocontenido en `CHANGELOG_AI.md`, sin código productivo, esquema, API, DTO, runtime, auditoría o dependencias. Sin cambios arquitectónicos.
- **A0 documental:** consistencia de fuentes M01.01–04, R/S/D, identificación de oráculos y alcance. **A1/A2 documentales:** revisión de trazabilidad, pares P/N y fail-closed; casos `NO_EJECUTADOS`. **A3:** compilación .NET/Angular/EF/Playwright `NO_APLICA` solo a este diff Markdown, jamás se cuentan como PASS. **A4:** diff de los dos Markdown, Scope Lock y verificación V5 en el SHA publicado con readback institucional.
- **Rollback:** `git revert <SHA_DE_ESTA_PUBLICACION>` normal y registro compensatorio nuevo en BITACORA; no force-push, no borrar eventos ni tocar QA/main/PROD.
