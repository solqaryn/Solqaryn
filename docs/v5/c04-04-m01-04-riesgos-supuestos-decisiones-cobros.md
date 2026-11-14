# SOLQARYN V5 — C04-04.M01.04: riesgos, supuestos y decisiones pendientes en cobros por enlace

**Autor:** SOLQARYN PRIMARY DEV 05 — JAVIER, `TASK_ID=6adc2e55f47c81919f6374e561687076`. **Fecha:** 2026-11-14. **Alcance único:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. Entrega preparatoria de riesgos, no aprobaciones comerciales ni implementación.

## 1. Autoridad, dependencias y objetivo

`STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), `PLAN_MAESTRO!A689:O689`, microtarea `C04-04.M01.04`: «Registrar riesgos, supuestos y decisiones pendientes». Pertenece a la preparación de cobros por enlace, webhooks, conciliación y reversión sin dependencia de una pasarela concreta. `DAG_TECNICO!A650:G650` clasifica `DOC_M01_PREP`, F=`VALIDADO`, G=`SI`, con 60 antecedentes M01 de C01–C03. La autorización es documental; no da permiso para cobros, base de datos, webhook dispatcher, JWT ni QA/main/PROD.

Antecedentes del mismo bloque ya publicados: `C04-04.M01.01` (alcance/no-regresión), `.02` (actores/IO) y `.03` (auditoría de trece fuentes). Sus revisiones A5 pueden permanecer pendientes y sus especificaciones no significan aprobación comercial, financiera o productiva.

## 2. Separación explícita entre observación, supuesto y decisión

- **Observado en código:** `IPagoOnlineProvider` ofrece puerto provider-neutral; `PagoOnlineService` valida factura/saldo/moneda e idempotencia con un lock local; `PagosOnlineController` exige identidad, permiso Facturacion y tenant; `InboundWebhooksController` valida firma y tamaño del payload. Fuentes/blobs en `c04-04-m01-03-auditoria-fuentes-contratos-cobros.md`.
- **Observado documentalmente:** existen entidades, índices EF, migraciones versionadas y tests xUnit de cobro/replay/webhook. Esta microtarea no ejecuta ninguno ni verifica esquema en Aiven o pasarela live.
- **Supuesto no aprobado:** un proveedor de pago específico, tasa, moneda comercial, cargos, ventanas, reintentos, expiración, refund, reglas de mora, settlement y acuerdos contractuales. Ninguno se debe convertir en constante, flag o compra por inferencia.
- **Decisión pendiente:** cualquier contrato económico, nuevo endpoint de cobro, migración o flujo financiero requiere microtarea técnica/operativa propia, A0–A4 causal, A5 independiente y autorizaciones del propietario cuando correspondan.

## 3. Registro de riesgos y controles propuestos

La severidad es **estimación de diseño**, no valoración observacional de un incidente ocurrido.

| Riesgo | Severidad | Fuente / escenario | Mitigación obligatoria antes de código productivo | Evidencia/gate a obtener |
| --- | --- | --- | --- | --- |
| R01 Cobro ajeno entre tenants | Crítica | EmpresaId/ruta/factura no coinciden con membresía validada | Guardas en backend, ámbito empresa+factura y permisos en cada lectura/escritura | Pruebas A/B y revocación JWT/RBAC |
| R02 Doble efecto por reintento | Crítica | Misma clave en procesos concurrentes | Unicidad durable por tenant/proveedor, control de payload y transacción segura | CI real de concurrencia + MySQL efímero |
| R03 Confundir timeout con confirmación | Alta | Proveedor no responde/estado ambiguo | Estado pendiente/revisable; nunca marcar pagado por ausencia de respuesta | Tests de timeout, reintento y conciliación |
| R04 Spoof de webhook | Crítica | Firma ausente/maliciosa o secreto incorrecto | Validación HMAC, timestamp/frescura, origen/tenant, fail-closed | Negativos de firma, replay, payload oversized |
| R05 Evento duplicado o fuera de orden | Alta | Id externo igual y payload distinto, callback tardío | Unicidad por tenant/proveedor/id, comparación hash, máquina de estados auditable | CI de duplicados, secuencias y choque de huellas |
| R06 Cobro inconsistente con factura | Crítica | Monto/moneda distinta a saldo o factura B | Validación server-side antes de iniciar y antes de conciliar | Positivos de saldo y negativos de moneda/scope |
| R07 Error de enlace expone datos | Alta | URL con credenciales o PAN/CVV en logs | URL opaca, HTTPS y no registrar secretos/cardholder data | Escaneo logs, prueba URL HTTP rechazada |
| R08 Estado de suscripción concedido por pago no conciliado | Alta | Callback de pago usado como permiso de producto | Separar entitlement de RBAC y ledger; reconciliación aprobada | Tests de autorización A/B y transición comercial |
| R09 Migración financiera destructiva | Crítica | Índice/tabla altera registros históricos | Cambios aditivos, backup, reversión no destructiva y revisión DBA | Migración/rollback MySQL efímero, plan auditado |
| R10 Proveedor no configurado o caído | Alta | Puerto sin credencial/configuración activa | Degradación segura, ningún cargo ni acceso ficticio | Tests de proveedor ausente, errores sanitizados |
| R11 Ventana de expiración inconsistente | Media | Link expirado aún utilizable/reutilizable | Estado y reloj UTC versionados, política comercial explícita | Negativos de expiración y replay aprobado |
| R12 Reembolso/reverso no conciliado | Crítica | Doble refund o eliminación de historial | Reversión idempotente con asiento compensatorio y permiso distinto | Ensayos de reversión auditable (sin borrar historia) |
| R13 Credenciales o tokens compartidos | Crítica | Secreto en repo, frontend, eventos o logs | Secretos solo server-side, rotación y mínimos privilegios | Escaneo de secretos, prueba de redacción |
| R14 Divergencia SaaS/ERP | Alta | Pago facturado y plan/entitlement desincronizados | Transacciones/compensación y orden causal de eventos aprobado | Contrato integrado billing-subscription |
| R15 Activación prematura de dispatcher | Crítica | Se interpreta preparación documental como trigger live | Prohibición vigente: no implementar eventos todavía | Verificar diff solo Markdown y cero acciones entre Tasks |
| R16 Evidencia histórica confundida con aprobación V5 | Media | N7.5/N7.8, workflows antiguos y pruebas mock | Revalidar HEAD exacto, fuente V5, PLAN/COLA/DAG y A0–A5 | Check Scope Lock, trazas idempotentes y readback |

## 4. Registro de decisiones abiertas (sin ratificar)

| ID | Decisión aún necesaria | Autoridad futura o función que debe dictaminar | Efecto prohibido mientras está abierta |
| --- | --- | --- | --- |
| D01 | Proveedor o proveedores, credenciales y jurisdicción | Propietario/comercial y responsables de seguridad | No contratar, cargar secret o activar pasarela |
| D02 | Monedas permitidas, tipo de cambio, redondeo y tasas | Comercial/finanzas | No fijar reglas monetarias ni modificar facturas |
| D03 | Vigencia del enlace y política de expiración | Propietario/comercial con revisión técnica | No emitir enlaces reales con política improvisada |
| D04 | Reintentos, estado ambiguo y conciliación | Finanzas y arquitectura backend | No declarar Pagado por timeout/callback |
| D05 | Permiso para reversión, refund y doble aprobación | Propietario/finanzas/seguridad | No habilitar refund ni compensación productiva |
| D06 | Contrato webhook, proveedores, firma y freshness | Arquitectura/seguridad y proveedor aprobado | No configurar webhooks live ni dispatcher |
| D07 | Modelo de ledger y consistencia con suscripciones | Arquitectura/finanzas/DB | No crear migraciones/ledger sin modelo aprobado |
| D08 | Observabilidad, retención de evidencia y PII | Seguridad/operaciones | No emitir logs con secretos o pagos sensibles |
| D09 | Idempotencia duradera y alcance de clave por tenant | Arquitectura/DB/seguridad | No asumir lock in-memory como exclusión distribuida |
| D10 | Rollback, reconciliación de incidentes y compensación | Finanzas/operaciones/DBA | No borrar registros ni reintentar cargos a ciegas |

Estos roles indican el **tipo de aprobación exigible en una fase posterior**, no que una persona específica ya haya emitido aprobación. Las decisiones continúan abiertas hasta evidencia causal escrita y autorizada.

## 5. Matriz de aceptación de la propia tarea documental

| Control | Oráculo positivo documental | Negativo que debe fallar |
| --- | --- | --- |
| A0 alcance | Cada riesgo conecta con fuente, etapa y dueño funcional | Se introduce roadmap o pago en producción |
| A1 consistencia | Riesgos R01–R16 y decisiones D01–D10 completos | D01–D10 se presentan como ratificados |
| A2 seguridad | Tenant/JWT/RBAC, firma, idempotencia, PII y ledger separados | Se equipara enlace con permiso o callback con settlement |
| A3 integración | Se cruzan Domain/Application/API/EF/tests con límites históricos sin ejecutarlos | Se cita un mock como prueba real de provider/MySQL |
| A4 publicación | HEAD/dev FF, diff solo dos Markdown, Scope Lock + V5 provenance exact-SHA completed/success, PLAN/COLA/BITACORA readback | Check requerido pending/failed/skipped/cancelled se trata como PASS |

**Casos propuestos para futuras etapas, no ejecutados:** cinco positivos (pago autorizado propio, replay idéntico, firma válida, conciliación confirmada, reversión autorizada) y doce negativos (sin JWT, sin RBAC, tenant B, membresía revocada, factura ajena, saldo insuficiente, moneda distinta, proveedor ausente, clave conflictiva, firma inválida, callback duplicado/distinto, timeout o reversión no autorizada). Los futuros tests .NET/HTTP/MySQL deberán acreditar la semántica, no basta documentación.

## 6. Impacto y cierre responsable

Impacto de esta M01.04: nuevo `docs/v5/c04-04-m01-04-riesgos-supuestos-decisiones-cobros.md` y sección completa del `CHANGELOG_AI.md`. No modifica Domain/Application/Infrastructure/API/Angular, endpoints, schemas, secretos, servicios, pagos, Jobs, usuarios ni entornos.

Auditorías A0–A3 son **documentales**, con supuestos diferenciados y ningún runtime afirmado. A4 exige checks específicos y lectura posterior de plan, cola y bitácora sobre SHA exacto. Compilación .NET/Angular o MySQL no se computa como PASS requerido para un diff exclusivamente Markdown (`NO_APLICA` motivado). A5 queda **PENDIENTE**, la debe ejecutar otra de las cinco Tasks Javier; no autorrevisión. El rollback es `git revert` normal sin force-push, conservando BITACORA. No autoriza M01.06/M02 por inferencia ni QA/main/PROD.
