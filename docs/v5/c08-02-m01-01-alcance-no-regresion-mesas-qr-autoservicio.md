# SOLQARYN V5 — C08-02.M01.01 — Alcance y no-regresión de mesas, QR y autoservicio

**Autor:** SOLQARYN PRIMARY DEV 05 — JAVIER (TASK_ID `6adc2e55f47c81919f6374e561687076`). **Fecha:** 2026-11-14. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, C08-02, M01.01. **Estado del producto:** únicamente planificación documental; no se ha implementado aquí POS de restaurante, QR firmado, menú público, pago, mesa digital, pedido ni integración.

## 1. Objetivo verificable y no-alcance

El bloque **C08-02 — Mesas, QR, menú, pedidos internos y autoservicio** exige en fases posteriores trazabilidad de **QR falso, mesa ajena y cierre de pedido**. El objetivo futuro es que personal autorizado pueda configurar mesas de una sucursal, publicar un menú mínimo, recibir pedidos asociados a una mesa válida y cerrar su estado sin que otro tenant, usuario o visitante falsifique permisos, mesa, precios o pago.

La microtarea M01.01 **únicamente** fija objetivo, exclusiones y reglas de no-regresión. Los M01.02–05 deberán documentar actores, interfaces, fuentes, riesgos y oráculos futuros; M01.06/M02+ tienen gates propios, no se liberan por este documento. No se prueban aquí endpoints ni existencia/ausencia exhaustiva de módulos; M01.03 inspeccionará los blobs reales antes de afirmar un inventario.

| Superficie futura | Capacidad objetivo | Excluido de esta entrega |
| --- | --- | --- |
| Empresa y sucursal | Cada mesa pertenece a empresa y sede explícitas, resueltas en backend | Crear empresa, rol global, nuevas policies JWT/RBAC |
| Mesa/QR | Referencia opaca verificable, revocable y ligada a mesa/tenant/contexto | Emitir o desplegar QR real, token o clave de firma |
| Menú | Proyección publicada de productos, variantes, disponibilidad y precio de servidor | Cambiar catálogo, stock, impuestos o descuentos vigentes |
| Pedido interno | Alta/edición/cancelación autorizada, estados y versión | Crear pedidos, endpoints o workers reales |
| Autoservicio | UI mínima, accesible, clara ante red lenta y errores | Crear app Angular, host binding o SMS/SMTP |
| Cocina y caja | Estado de preparación/cierre reconciliado con facturación autorizada | Cobro, captura de pago, conciliación, fiscalidad real |
| Auditoría | Actor, tenant, sucursal, mesa, correlación y resultado saneados | Registrar PII, secretos, tokens, medios de pago |
| Operaciones | Idempotencia y controles de concurrencia en backend | Migraciones SQL, modificaciones de datos productivos, cron o event dispatcher |

**Distinción clave:** un QR escaneable **no es autorización** por sí mismo; la firma, su origen y expiración tampoco sustituyen permisos backend ni controles de publicación. Una mesa de tenant B, incluso con ID numérico válido, no debe permitir lectura o alteración de pedidos de B desde A.

## 2. Fronteras de confianza y propiedad

El navegador o cámara sólo proporcionan una referencia no confiable de mesa, sede y menú. El servidor debe resolver la empresa desde host/contexto permitido y comparar mesa/sucursal/empresa contra su base canónica. No confiar en `EmpresaId`, `MesaId`, importes, permisos, `isPaid`, fecha de expiración o estado enviados por el cliente como autoridad. Para lectura anónima futura, publicar únicamente campos mínimos y no incluir datos de cliente, caja, cocina privada, otros pedidos, JWT ni claves de QR.

La validación de firma/revocación y protección contra replay debe ser criptográficamente segura y ejecutarse en backend. El diseño debe documentar ciclo de vida de QR rotado o invalidado y respuesta ante QR falso, expirado, copiado o generado para otra sede. Nada de esto se ha implementado ni ensayado en esta M01.01.

Los cambios futuros que relacionen pedido, inventario, contabilidad, impuestos o pago requieren transacción, identidad y auditoría causales. No hay autorización para desplegar pasarelas, activar proveedores, crear webhooks o modificar QA/main/PROD. La UI Angular no sustituye autorización backend. Se conserva el stack vigente .NET 10, Angular 22.2.1, MySQL/EF Core, JWT y RBAC, sin alterar arquitectura.

## 3. Invariantes de no-regresión NR01–NR14

- **NR01 — Tenant A/B:** ninguna referencia de QR/mesa/pedido permite a A leer o mutar datos o recursos internos de B.
- **NR02 — Sucursal/mesa:** una mesa pertenece a exactamente un ámbito autorizado; el cliente no puede escoger sede de otra empresa por ID.
- **NR03 — QR no autoritativo:** QR falso, manipulado, expirado, revocado o de otra sede se rechaza sin efectos ni divulgación.
- **NR04 — Publicación mínima:** menú anónimo sólo incluye información publicada; nunca PII, datos internos o productos ocultos.
- **NR05 — Precio/stock canónicos:** el servidor recalcula precios, variante, disponibilidad, impuestos y total; cliente no puede alterarlos.
- **NR06 — Estado único:** transiciones pedido nuevo/aceptado/preparando/servido/cerrado/cancelado se autorizan según máquina de estados definida en futuro M02.
- **NR07 — Cierre seguro:** un pedido cerrado no puede volver a abierto ni cobrarse/consumirse dos veces por replay.
- **NR08 — Idempotencia:** doble clic, timeout y reintento con clave/payload idénticos no duplican pedidos, líneas ni cargo.
- **NR09 — Concurrencia:** dos modificaciones/cierres concurrentes conflictivos de una mesa no pierden ni mezclan pedidos.
- **NR10 — Roles:** personal cocina/caja/recepción/administración sólo ve o cambia los campos/estados de su rol y tenant.
- **NR11 — Auditoría:** toda alta/cambio/cierre registra actor, tenant, origen, versión y resultado sin JWT ni secretos.
- **NR12 — UX accesible:** teclado, foco, lector de pantalla y 320px dan estados de error/carga coherentes; no se muestra pago o pedido confirmado sin servidor.
- **NR13 — Compatibilidad ERP:** pedidos/ventas/facturas, inventario, caja y descuentos existentes mantienen invariantes; autoservicio no altera deuda ni saldo sin flujo autorizado.
- **NR14 — Rollback:** fallas entre pedido/stock/factura/pago no dejan estados parciales irreversibles ni datos inter-tenant expuestos.

Los invariantes establecen **criterios futuros**. Su redacción no es una declaración PASS de tests, HTTP, MySQL, UI, auditoría runtime, pagos o fiscalidad.

## 4. Escenarios P01–P10 y N01–N14 — FUTUROS NO_EJECUTADOS

| Par | Positivo futuro observable | Negativo obligatorio futuro |
| --- | --- | --- |
| P01/N01 | Mesa A muestra sólo menú público de su sede | QR/mesa B no revela pedidos privados ni menú restringido |
| P02/N02 | QR autorizado y vigente resuelve mesa publicada | Firma adulterada, QR falso o revocado se rechaza |
| P03/N03 | Producto disponible se agrega con precio de servidor | Cliente cambia monto/IVA/stock/tenant sin efecto |
| P04/N04 | Pedido autorizado se crea una sola vez | Dos submits/retry/timeout no duplican pedido ni líneas |
| P05/N05 | Cocina recibe sólo pedidos de su sucursal/rol | Cajero/cocinero sin rol o de B no accede a pedido A |
| P06/N06 | Cambiar estado permitido deja historial auditable | Estado terminal/version stale no modifica pedido |
| P07/N07 | Cierre permitido ocurre exactamente una vez | Replay de cierre no duplica facturas ni cobros |
| P08/N08 | UI móvil accesible refleja respuesta real del servidor | Timeout offline no muestra confirmado/paid falso |
| P09/N09 | Mesas con pedidos distintos mantienen aislamiento | Cambiar MesaId/SucursalId entre requests no mezcla cuentas |
| P10/N10 | Pedido/venta/stock mantienen consistencia transaccional | Fallo intermedio revierte operaciones y preserva stock |

Adicionales **N11–N14**: N11 URL de QR en log o referer revela secreto; N12 recurso de sucursal revocada sigue operando; N13 pago rechazado activa cierre falso; N14 roles revocados conservan sesión con permiso indebido. Las pruebas futuras deben acreditar 403/404 seguros, persistencia final e impacto en datos autorizados, no sólo mensajes visuales.

## 5. Dependencias e impacto arquitectónico

`PLAN_MAESTRO!A1190:O1190` referencia la fase C07.02/C08.01 y bloques C01/C02/C06. `DAG_TECNICO!A1151:J1151` tiene F=VALIDADO y G=SI para la preparación M01.01, y `COLA!G1190/O1190=SI` antes de la reclamación. Esos gates son sólo para **análisis documental preparatorio**: no certifican M01.06 terminal, código POS, JWT, pagos, migraciones o QA/PROD. Las relaciones concretas deberán reconfirmarse al cerrar M01 y antes de cualquier M02+.

**Cambios permitidos en esta entrega:** este documento bajo `docs/v5` y una sección autocontenida de `CHANGELOG_AI.md`. **No se tocan** Domain, Application, Infrastructure, API, DTO, Angular, EF/MySQL, Task schedules, infraestructura, DNS, secretos o datos. Interfaces/contratos futuros no se crean ahora; observabilidad y rollback se definen como criterios de salida. El próximo inventario debe leer código y blobs exactos sin asumir ausencia o equivalencia de nombres.

## 6. A0–A5, evidencia y reversión

- **A0:** confirmar repo/rama `dev`, skill/Scope Lock, plan fuente, DAG por ID, `PLAN_MAESTRO!H1190/I1190` y claim CAS exclusivo, contenido documental sin secretos y CHANGELOG sin encabezados vacíos.
- **A1:** consistencia interna de objetivo, NR01–NR14 y P01–P10/N01–N14 como casos **FUTUROS NO_EJECUTADOS**; no son pruebas funcionales PASSED.
- **A2:** revisar diseño de amenaza QR falso, IDOR, mesa ajena, tenant A/B, JWT/RBAC, PII, replay, doble cierre, facturación y denegación segura; no certificar aislamiento vivo sin tests reales.
- **A3:** contraste documental con interfaces/capas existentes y plan comercial; dotnet, MySQL, Angular y Playwright son `NO_APLICA` al diff únicamente Markdown y no PASS de runtime.
- **A4:** publicar con git commit fast-forward exact HEAD; comprobar Scope Lock y procedencia CHANGELOG `completed/success` en SHA publicado, diff/blobs y readback PLAN/COLA/BITACORA. Si un check requerido está queued/in_progress/failed, mantener VALIDANDO.
- **A5:** revisión independiente por otra Task Javier distinta del autor, sin autorrevisión; no QA/main/PROD antes de autorización diferenciada.

**Rollback:** `git revert` normal del commit documental y sección CHANGELOG, más evento compensatorio aditivo en BITACORA. Nada autoriza crear workers, eventos, servicios de pago o alterar información comercial real.
