# SOLQARYN V5 — C07-04.M01.02 — Actores, entradas, salidas y fronteras de confianza de pedidos, pagos y documentos

**14 noviembre 2026 · M01 preparación documental.** Autoridad: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, `C07-04 | Pedidos, pagos y documentos`. Complementa el alcance `C07-04.M01.01`, documento `docs/v5/c07-04-m01-01-alcance-pedidos-pagos-documentos.md` (blob `2efabd1f6b2444e605ca49cf1cce34ec4bce23d8`). M01.01 tiene A0–A4 documentales y **A5 independiente pendiente**: esta unidad no lo considera certificado para despliegues ni reemplaza gates terminales.

## Inventario de código observado (referencias Git, no ejecución de servicios)

| Archivo vigente / blob | Superficie observada |
| --- | --- |
| `backend/src/API/Controllers/CotizacionesController.cs` / `b6cc9aa76dc6f0ee178a7e37b6c4471dac7a749e` | Ruta `cotizaciones`, `Authorize`, permisos de ver/crear/editar/enviar/aceptar/rechazar/convertir/duplicar; no demuestra autorización tenant de extremo a extremo. |
| `backend/src/API/Controllers/PedidosVentaController.cs` / `293839e33ced2fda5328eee8b42b2e7ae7a1fa50` | Ruta `pedidos-venta`; `Idempotency-Key` exigida al crear y permisos específicos para confirmar/anular; no garantiza por sí sola deduplicación durable de aplicación. |
| `backend/src/API/Controllers/VentasController.cs` / `5a1041030359ed1503c8740d2e04e60ade984797` | Ruta `ventas`, creación borrador, vista previa, confirmación y permisos `ModuloSistema.Ventas`; requiere inspección de servicio/EF para afirmar efectos reales. |
| `backend/src/API/Controllers/FacturasController.cs` / `8a70f59763749896c3662a365ec9e8ffe47dc72a` | Ruta `facturas`, pago y anulación protegidos por `RequierePermiso` y auditoría en endpoints inspeccionados. |
| `backend/src/API/Controllers/PagosOnlineController.cs` / `99962b2957f672a6dd435895daffaba7333f8a6b` | Ruta `pagos-online`; consulta scope tenant server-side, `Idempotency-Key` de 16–128 caracteres, intentos provider-agnostic; no afirma integración real con proveedor. |
| `backend/src/API/Controllers/FacturacionFiscalController.cs` / `6dd92895e2299feda3737a1a6eb28a3d960bcf7e` | Ruta `facturacion-fiscal`, permiso de crear, identidad fiscal por Empresa y validación de sucursal/tenant. No equivale a certificación fiscal o emisión real. |

El inventario es selectivo y no sustituye una revisión total de repositorios, funciones EF, reglas transaccionales, configuración externa o cumplimiento jurisdiccional. Los endpoints observados son código preexistente; este changeset no los modifica.

## Actores A01–A12 y autoridad permitida

| ID | Actor o agente lógico | Entrada / límite de responsabilidad |
| --- | --- | --- |
| A01 | Vendedor autenticado de empresa A | Crea cotización/venta con permiso, sin acceso a B. |
| A02 | Operador de pedidos | Crea/confirma/anula pedido con permiso explícito y alcance de cliente/empresa. |
| A03 | Cajero autorizado | Registra pago físico y cambio; no modifica comprobante fiscal por inferencia. |
| A04 | Responsable de facturación | Consulta, emite o anula con permisos y vigencia fiscal verificables. |
| A05 | Responsable de supervisión | Revisa auditoría, ajustes y conciliación; privilegio nunca implícito por UI. |
| A06 | Cliente de empresa A | Acceso a sus pedidos y documentos sólo mediante superficie pública autorizada; hoy no se certifica portal universal. |
| A07 | Proveedor externo de pagos | Entrega referencias opacas; credenciales y verificaciones no nacen en payload cliente. |
| A08 | Adaptador fiscal externo | Identidad jurídica y prueba de proveedor requerida antes de emitir. |
| A09 | Servicio interno de aplicación .NET | Orquesta transacciones desde DTO validado y scope autenticado. |
| A10 | Persistencia EF/MySQL | Aplica transacciones/índices/ownership sujetos a tests y migraciones autorizadas. |
| A11 | Componente Angular de ventas/facturación | Presenta estado; no constituye autoridad RBAC ni aislamiento server-side. |
| A12 | Auditor y consumidor de exportaciones | Sólo lectura de datos propios, PII minimizada y evidencia íntegra. |

A06–A08 y A12 describen roles y fronteras **de diseño**; no se afirma disponibilidad ni integración adicional de esos agentes.

## Interfaces I01–I12 (entrada → salida, código observado o contrato futuro)

| ID | Interfaz y condición de entrada | Salida esperada; verificación futura |
| --- | --- | --- |
| I01 | `POST cotizaciones` con actor autorizado | Cotización borrador propia, referencia de cliente y línea auditables. |
| I02 | `POST cotizaciones/{id}/aceptar` con permiso Aprobar | Estado aceptado, sin anticipar factura. |
| I03 | `POST cotizaciones/{id}/convertir` con permiso Confirmar | Conversión controlada de documento aceptado, sin duplicidad. |
| I04 | `POST pedidos-venta` con `Idempotency-Key` y DTO | Pedido propio creado/reutilizado sin segundo efecto. |
| I05 | `POST pedidos-venta/{id}/confirmar` | Confirmación/stock transaccional sin ejecución doble. |
| I06 | `POST pedidos-venta/{id}/anular` | Anulación autorizada, motivo y reversión trazable. |
| I07 | `POST ventas` / `ventas/{id}/confirmar` | Venta borrador/confirmada según política, sin sobrescribir snapshots. |
| I08 | `GET facturas/{id}` y `GET facturas/venta/{id}` | Documento solo de tenant y consumidor permitidos. |
| I09 | `POST facturas/{id}/pagos` / `pagos/{pagoId}/anular` | Aplicación/compensación auditada con saldo correcto. |
| I10 | `POST pagos-online/iniciar` con tenant, idempotencia y referencia | Intento provider-agnostic; proveedor real y conciliación son gates separados. |
| I11 | `POST facturacion-fiscal/emisiones` | Respuesta fiscal sólo con identidad empresarial, sucursal y proveedor verificados. |
| I12 | Exportaciones PDF/CSV, enlace público o conciliación | Salida restringida por tenant, TTL/permisos y PII; endpoints/formatos específicos por inventariar. |

I12 es **contrato futuro** hasta su inventario; I01–I11 usan rutas observadas, pero esta revisión no invocó HTTP ni acreditó sus resultados.

## Fronteras de confianza B01–B08 y denegaciones obligatorias

| ID | Cruce de confianza | Prueba negativa obligatoria posterior |
| --- | --- | --- |
| B01 | Navegador/UI → API JWT/RBAC | Cliente A con ID de B recibe denegación, incluso con botón oculto/forzado. |
| B02 | DTO externo → Application | `EmpresaId` enviado por cliente no sustituye identidad/membresía activa; NULL legado no se comparte. |
| B03 | Cotización → pedido → venta | Estados inválidos o replay de misma clave con datos distintos no causan segundo documento. |
| B04 | Factura/pago → DB contable | Concurrencia/cambio y anulación no alteran saldo/stock de forma parcial. |
| B05 | Pasarela externa → adaptador de pagos | Callback no verificado, referencia duplicada o proveedor no configurado fallan cerrados. |
| B06 | Configuración fiscal → emisor | RTN inexistente, sucursal ajena o snapshot fiscal incongruente niegan emisión. |
| B07 | Persistencia interna → exportaciones y enlaces públicos | Documento ajeno/PII sin permiso no se filtra a PDF/CSV/cache. |
| B08 | Actualización/reversión → auditoría y observabilidad | No borrar evento histórico, no secretos/PAN/CVV en logs, rollback trazado. |

## Casos de aceptación a ejecutar en fases posteriores

**P01–P08, FUTUROS NO_EJECUTADOS:** crear cotización propia; aceptar; convertir única vez; crear pedido con idempotencia; confirmar con reserva válida; aplicar pago; anular con permiso y motivo; consultar/exportar documento propio.

**N01–N14, FUTUROS NO_EJECUTADOS:** A ve factura B; B crea con EmpresaId A; NULL legacy publicado; cotización no aceptada convertida; replay conflictivo; doble confirmación; pago negativo/cambio desbalanceado; pago sin permiso; anulación sin permiso; callback no autenticado; duplicado providerEventId; RTN inválido; sucursal ajena; exportación ajena/PII. Tests reales xUnit/HTTP/MySQL/Playwright son responsabilidad de unidades futuras; no se atribuyen resultados a esta entrega.

## Impacto, verificaciones y salida

- **Alcance técnico:** sólo este documento y sección autocontenida de `CHANGELOG_AI.md`; sin cambiar código ejecutable, API/DTO, EF/MySQL, esquema, JWT/RBAC, datos, hosting, proveedores, secretos, workers ni webhooks.
- **Dependencias:** DAG `C07-04.M01.02` F=VALIDADO/G=SI y COLA G/O=SI antes del claim; documentación preparatoria no autoriza liberaciones terminales `M01.06` ni saltos de A5.
- **A0** inventario selectivo de seis blobs API más M01.01; **A1** coherencia de 12 actores/12 interfaces/8 fronteras y escenarios futuros; **A2** modelado documental de tenant A/B, RBAC y fiscal, no prueba de seguridad runtime; **A3** `NO_APLICA` compilación/HTTP/DB/Playwright para Markdown; **A4** fast-forward exact-SHA, Scope Lock y CHG provenance `completed/success`, readback PLAN/COLA/BITACORA; **A5** otra Task Javier distinta del autor.
- **Rollback:** `git revert` normal con registro compensatorio posterior, jamás modificación de BITACORA histórica. No promoción QA/main/PROD, activación fiscal/pagos o integraciones hasta permisos nuevos y pruebas específicas.

**Estado de diseño:** interfaces y actores inventariados, no desplegados ni re-certificados por esta tarea.
