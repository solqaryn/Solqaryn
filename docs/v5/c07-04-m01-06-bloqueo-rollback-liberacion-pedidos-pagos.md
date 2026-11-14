# SOLQARYN V5 — C07-04.M01.06: bloqueo, rollback y condición de liberación de pedidos, pagos y documentos

**Unidad:** `C07-04.M01.06`, PLAN_MAESTRO fila 1123, ámbito C07-04 — Pedidos, pagos y documentos, M01 preparación. **Autor:** PRIMARY DEV 02 / JAVIER_CHATGPT / TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Rama:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT, 2026-11-14.

## Propósito y alcance de cierre exclusivamente documental

Definir de manera trazable **qué bloquea**, cómo **revertir** y qué **evidencia habilita una evaluación de M02** para pedidos, cotizaciones, facturas, caja, pagos, anulaciones y exportaciones. No se modifica ni se declara certificado ningún proceso de cobro o emisión fiscal. El alcance solo añade esta política de liberación en Markdown y su sección autocontenida del CHANGELOG; no crea código, DTO, API, esquema MySQL, índices, migraciones, Webhook, PSP, workers, permisos, secretos o endpoints.

**Precondición de la reclamación:** C07-04.M01.01–05 están `LISTO` **con A5_PASS independiente acreditado**. Tras A5 de M01.04 y M01.05, las fuentes institucionales indicaron `COLA!G1123/O1123=SI` y `DAG_TECNICO!F1084=VALIDADO/G1084=SI`. Este gate de preparación no es permiso de ejecutar pagos, migrar datos, activar callbacks ni promocionar QA/main/PROD.

## Cadena causal M01.01–05 con límites de evidencia

| Predecesor | Evidencia documental verificada y alcance | A5 |
| --- | --- | --- |
| M01.01 | `docs/v5/c07-04-m01-01-alcance-pedidos-pagos-documentos.md`, blob `2efabd1f6b2444e605ca49cf1cce34ec4bce23d8`; invariantes NR01–NR12 | Independiente DEV05, sin runtime |
| M01.02 | `docs/v5/c07-04-m01-02-actores-interfaces-pedidos-pagos-documentos.md`, blob `9c5c0aba4694ffbbb6a4bfacfa683330de814f1d`; 12 actores, 12 interfaces y límites | Independiente DEV05, sin runtime |
| M01.03 | `docs/v5/c07-04-m01-03-inventario-contratos-codigo-pedidos-pagos.md`, blob `654815da8a0f9b4d60c45a033eed0430bf89d317`; 12 fuentes y brechas G01–G10 | Independiente DEV05, sin runtime |
| M01.04 | `docs/v5/c07-04-m01-04-riesgos-supuestos-decisiones-pedidos-pagos.md`, blob `74c4a64fe021a5f88fe712bf6cfcfa0d8f928d92`; R01–R12, S01–S08, D01–D08 PENDIENTES | Independiente DEV02, SHA `b4aa14646cb056af63038afc8fa1ee39afd98a90` |
| M01.05 | `docs/v5/c07-04-m01-05-criterios-aceptacion-pedidos-pagos-documentos.md`, blob `ef5e859e3bb46175d94cc29423f9dfd791682f6c`; P01–P14/N01–N14 FUTUROS NO_EJECUTADOS | Independiente DEV02, SHA `1732bb4cf5025dad5e0d949d6ebb969671bb6231` |

El readback A5 mantiene autoría original de cada fuente. Los dos A5 últimos constan en `PLAN_MAESTRO!K1121:K1122` y BITACORA, sin modificar eventos históricos. El código observado en M01.03 es **evidencia selectiva**, no un PASS de concurrencia, fiscalidad, aislamiento HTTP/DB o pagos.

## Bloqueos explícitos B01–B14 (fail-closed)

| ID | Desencadenante de bloqueo | Consecuencia y comprobación para resolver |
| --- | --- | --- |
| B01 | Ningún tenant confiable o EmpresaId NULL de procedencia ambigua | Denegar lectura/escritura cross-tenant; exigir inventario, reconciliación y negativo A/B |
| B02 | Token inválido, rol revocado o falta permiso específico | Rechazo backend; xUnit/HTTP contra acciones aplicar/anular/emitir |
| B03 | Pedido, factura o pago de empresa distinta al contexto | Denegación por ownership en API/Application/repositorio, sin datos B |
| B04 | Clave idempotente repetida con distinto fingerprint | Error de conflicto, sin nuevo pedido o cobro y con integridad durable |
| B05 | Dos reservas de stock o confirmaciones concurrentes | No confirmar hasta verificar transacción real en MySQL efímero |
| B06 | Cotización no aceptada o máquina de estados incompatible | Negar transición; pruebas de no duplicado y rollback |
| B07 | Saldo, impuesto, pago aplicado o vuelto inconsistente | Rechazar factura/pago, auditar y no generar movimiento parcial |
| B08 | Callback externo sin firma verificada o evento repetido | Ignorar o rechazar según contrato, nunca abonar ni revelar secreto |
| B09 | Proveedor PSP no seleccionado/autorizado o coste no aprobado | Prohibir activación de SDK, webhooks, cobros o credenciales |
| B10 | RTN, sucursal, correlativo o emisor fiscal ajenos/inválidos | Bloquear emisión; validar jurisdicción y sandbox autorizado |
| B11 | Exportación/PDF o URL pública revela factura, token o PII ajena | Retirar exposición, validar TTL/revocación y propiedad A/B |
| B12 | Anulación o rollback conserva pagos/stock/estado inconsistentes | Mantener NO_GO técnico hasta demostrar compensación y auditoría |
| B13 | CI obligatoria queued/in_progress/failed/cancelled/skipped | No certificar PASS, conservar microtarea afectada VALIDANDO |
| B14 | Intento de promover por solo M01 o sin A5 independiente | No liberar M02 terminal por inferencia, ni QA/main/PROD sin autoridad |

Cada B exige evidencia causal propia. Una matriz que nombra la prueba no demuestra que se ejecutó.

## Estrategia de reversión y recuperación RLB01–RLB08

**RLB01** — Para este documento: revertir por `git revert` regular el commit específico de M01.06, sin force-push; preservar CHANGELOG histórico y anexar evento correctivo nuevo en BITACORA si fuese necesario.

**RLB02** — Para cambios futuros de API/Domain/Application: despliegue reversible solo después de pruebas de compatibilidad y revisión de contratos/DTO, sin reescribir órdenes/ventas previas.

**RLB03** — Para migraciones MySQL: preferir cambios aditivos versionados y restauración ensayada con fixtures efímeros, preservar historial fiscal/contable; no aplicar DDL/DML hoy.

**RLB04** — Para orden/pago idempotente: conservar `tenant + idempotency key + fingerprint`, reconciliar efectos antes de reintentar; nunca hacer un cobro compensatorio automático sin evidencia y permiso.

**RLB05** — Para stock/factura: transacción o compensación verificable que conserve saldos, reservas, inventario y documentos; evitar hard-delete de historia referenciada.

**RLB06** — Para PSP/callback: mantener adapter/proveedor deshabilitado por defecto; secretos exclusivamente por entorno autorizado, no capturar ni insertar credenciales en Git, PLAN o BITACORA.

**RLB07** — Para exportación: revocar URLs y limpiar cachés segmentadas por tenant; probar que el acceso de empresa A jamás devuelve bytes o metadatos B.

**RLB08** — Para fallos CI o revisión: conservar estado real, SHA, logs y owner; no reintentar indiscriminadamente ni simular PASS. Una reparación requiere su propia evidencia exact-SHA y revalidación posterior.

## Matriz de condiciones para liberar una evaluación posterior (L01–L12)

| Control | Evidencia verificable exigida | Alcance habilitado |
| --- | --- | --- |
| L01 | Las cinco M01.01–05 están LISTO y A5_PASS independiente (verificado al claim) | Solo reclamar M01.06 documental |
| L02 | M01.06 tiene A0–A4 sobre SHA de publicación, Scope Lock/V5 SUCCESS y readback | LISTO_DEV de esta especificación |
| L03 | M01.06 obtiene A5 de Task Javier distinta de autora | Revalidar su gate de salida; no autoaprobar |
| L04 | DAG dinámico propio de M02.01 da VALIDADO/SI con COLA G/O=SI y PLAN PENDIENTE libre | Preflight de M02.01, no claim automático |
| L05 | Contrato de ownership por tenant/objeto, roles, UTC y datos NULL resueltos | Diseñar M02 sin conceder permisos |
| L06 | Casos A/B y API HTTP de autorización negativos definidos para código | Orientar futuras suites, aún no PASS |
| L07 | Índices, claves de idempotencia, transacción y concurrencia probados en MySQL efímero | Cierre posterior de implementación, no ahora |
| L08 | Montos, snapshot fiscal, correlativos y cambio coherentes | Cierre posterior de implementación, no ahora |
| L09 | Integración PSP y webhook con autenticidad, cuotas y autorización específica | Habilitación futura solo por nueva orden, no esta |
| L10 | Exportación/PDF sin PII cross-tenant, revocación y acceso limitado | Cierre posterior de pruebas por SHA |
| L11 | Logs/correlación sin secretos, PAN/CVV, JWT ni PII innecesaria | Revisión de seguridad dirigida posterior |
| L12 | Migración/release/rollback y QA/main/PROD tienen autorización diferenciada | Despliegue únicamente cuando sea autorizado |

**Resultado autorizable hoy:** M01.06 como **documento de bloqueo/liberación**, nunca como validación de pagos/stock/facturación ni permiso de implantar M02. El cierre de M01.06 no acredita la implementación de una fase completa: cada scope de M02–M06 tiene DAG, preflight, auditorías y A5 separados. Las decisiones comerciales/fiscales D01–D08 permanecen PENDIENTES.

## Validaciones a realizar según etapa y evidencia de esta unidad

- **A0:** cotejar Plan V5, plan/cola/dag, 5 predecesores LISTO+A5, autor único, exclusión de secretos y diff exactamente dos Markdown.
- **A1:** B01–B14 y RLB01–RLB08 son bloqueos/rollback trazables, L01–L12 liberación condicional, no invocación de servicios.
- **A2:** aislar tenant A/B, JWT/RBAC, idempotencia, pago, fiscalidad, PII, condiciones de FAIL sin mutar código ni afirmar test de seguridad real.
- **A3:** revisar contratos M01.01–05, coherencia con arquitectura .NET 10/Angular 22.2.1/MySQL 8.4 y dependencias, **NO_APLICA runtime** a Markdown solamente.
- **A4:** publicar con fast-forward y CAS de HEAD vivo, comprobar blobs y `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completos/success en SHA exacto, registrar BITACORA A:Z y readback PLAN/COLA. Backend/frontend/PSP/MySQL que sean no-causales al diff Markdown no son PASS atribuidos.
- **A5_PENDIENTE:** Task Javier distinta del autor debe auditar esta especificación sobre SHA y evidencia original antes de habilitar paso M02, sin autoaprobación.

**No se crean Tasks, PR ni ramas, eventos/webhooks, pagos, hardware o servicios nuevos; sin cambios QA/main/PROD.** Reversión por git revert normal, estados por CAS único, y BITACORA histórica inmutable.
