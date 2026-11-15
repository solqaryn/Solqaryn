# SOLQARYN V5 — C04-04.M02.01: entidades, ownership e invariantes para cobros

**Autor:** PRIMARY DEV 03 — JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Repositorio autorizado:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Fecha:** 2026-11-15. **Etapa:** M02 documental, NO implementación de pasarela.

## Alcance y autoridad

Definir **únicamente** entidades conceptuales, ownership, relaciones y reglas de invariantes para una futura abstracción de enlaces y cobros: emisión autorizada, webhooks verificados, conciliación e idempotencia de efectos contables. Los cinco M01.01–05 y M01.06 constan LISTO con A5 independiente. Antes del claim, `DAG_TECNICO!A653:G653` indicó `F=VALIDADO/G=SI` y `COLA!G692/O692=SI`. La admisión permite **documentar** M02.01, no autoriza cuentas PSP, tarifas, métodos de pago, nuevos endpoint HTTP, secret keys ni movimiento económico.

La documentación `C04-04.M01.06` mantiene abiertas las decisiones comerciales D01–D10 (divisas, comisiones, impuestos, vencimiento, devoluciones y responsabilidades). **No suponer proveedor, tarifa, settlement ni contrato comercial aprobado.** La integridad de cobros existentes, facturas y asientos es un requisito; este documento no altera su estado. La entidad `Empresa` y la membresía RBAC pertenecen a los contratos del SaaS actual; no crear duplicados de identidad.

## Entidades conceptuales — ninguna tabla ni clase C# creada

| Entidad proyectada | Dueño y clave conceptual | Relaciones/invariantes propuestas |
| --- | --- | --- |
| `SolicitudCobro` | ID opaco + `EmpresaId` de contexto autenticado servidor | Emisor, documento comercial de origen y motivo de solicitud; no equivale a cargo exitoso |
| `EnlacePago` | Solicitud, versión, vencimiento y estado | Token opaco de exposición mínima; sin datos de tarjeta ni privilegio de consulta de otra empresa |
| `IntentoPago` | Solicitud + ID proveedor opaco + clave idempotente | Un único resultado de negocio por clave y payload, no convertir timeout en cobro duplicado |
| `EventoProveedor` | ID externo normalizado, firma y timestamp validables | Ingreso no confiable hasta autenticar firma/HMAC y correlación server-side |
| `ConciliacionPago` | Cobro/empresa + referencia externa verificada | Relaciona evento confirmado, estado local y diferencia observada; no liquidar por redirect cliente |
| `MovimientoContable` | Empresa, operación origen, moneda y monto | Efecto inmutable y trazable, sólo después de confirmación fiable y guardas contables |
| `ReversionCobro` | Cargo original, motivo, autorización y referencia compensatoria | No borrar historia; reembolso/contracargo requieren autoridad distinta y trazabilidad |
| `PoliticaCobro` | Versión y ámbito empresa/canal aprobado | Moneda/importe/fees/expiración desde autoridad comercial, sin defaults financieros inventados |
| `EvidenciaAuditoriaPago` | Evento interno, actor y correlación minimizada | Hash/ID seguro, causalidad y resultado sin secretos, datos de pago ni PII innecesaria |
| `EstadoIntegracionProveedor` | Configuración aprobada por ambiente y empresa | Estado conceptual; nunca activa proveedor por documentarlo ni autoriza fallback entre tenants |

**Cardinalidades propuestas**, sujetas a revisión de modelos existentes: `Empresa 1:N SolicitudCobro`; `SolicitudCobro 1:N EnlacePago` y `1:N IntentoPago`; `IntentoPago 1:N EventoProveedor`; `IntentoPago 0..1 ConciliacionPago`; `ConciliacionPago 0..N MovimientoContable` por partidas de compensación. Referencias externas se almacenarán como valores opacos y nunca como autorización tenant.

## Invariantes de negocio, seguridad y transacción

1. **COB01:** toda solicitud pertenece a una sola empresa desde JWT validado, membresía actual y ámbito de servidor; `EmpresaId` cliente no tiene autoridad.
2. **COB02:** actor emisor necesita permiso RBAC específico y documento comercial propio vigente; un lector no puede cobrar.
3. **COB03:** creación de enlace no registra pago ni altera saldo; redirect o pantalla `success` tampoco.
4. **COB04:** monto y moneda se originan en una obligación autorizada del backend y una política comercial aprobada; no confiar en parámetros de navegador.
5. **COB05:** cada intento tiene idempotencia persistente y separación por empresa, solicitud, operación, proveedor y payload.
6. **COB06:** misma clave + payload distinto deniega operación, genera conflicto auditable y nunca crea cargo nuevo.
7. **COB07:** todo webhook requiere autenticidad HMAC, timestamp, límite de tamaño y anti-replay antes de producir efectos.
8. **COB08:** evento tardío, duplicado o desordenado no puede rebajar estado confirmado ni crear dos movimientos.
9. **COB09:** confirmación de pago procede de verificación server-to-server y reconciliación, no del cliente ni de un estado remoto sin validación.
10. **COB10:** cada efecto económico se aplica atómicamente con conciliación, cuenta, empresa y asiento; rollback no borra historia.
11. **COB11:** operaciones de devolución/contracargo exigen permiso diferenciado y referencia inmutable al movimiento original.
12. **COB12:** TTL, vencimiento y revocación de enlaces se aplican desde servidor; enlaces revocados no se reactivan por retry.
13. **COB13:** errores de proveedor y timeouts quedan como estado incierto comprobable; jamás reintentar un cargo a ciegas.
14. **COB14:** no filtrar datos de tarjeta, tokens de sesión, firmas HMAC, claves PSP ni secretos en enlaces, logs o DTO.
15. **COB15:** solicitudes/conciliaciones B nunca se leen, modifican o correlacionan bajo empresa A; pruebas tenant A/B obligatorias en implementación.
16. **COB16:** defaults sin política comercial aprobada son `NO_GO` para cobro real y `NO_APLICA` para PSP; documentación no libera el requisito.
17. **COB17:** importes requieren precisión decimal y controles de redondeo establecidos por contrato comercial, nunca `float` ni conversiones implícitas.
18. **COB18:** límites de cuota, rate limit, correlación y reintentos protegen tanto recursos como costos sin bloquear otras empresas.
19. **COB19:** migración/adaptador futuro exige CI real .NET 10, MySQL 8.4 efímero, tests tenant A/B e integridad idempotente.
20. **COB20:** A0–A4 y A5 independiente autorizan sólo cierre técnico DEV de esta documentación, no QA/main/PROD ni operaciones económicas.

## Oráculos futuros (NO EJECUTADOS)

| Positivo de futura implementación | Negativo que deberá rechazarse |
| --- | --- |
| P01 Emisor con RBAC vigente crea solicitud en empresa A | N01 Actor de A manipula factura de B |
| P02 Mismo request+idempotency devuelve mismo intento | N02 Misma clave con payload distinto duplica cobro |
| P03 Webhook firmado y correlacionado concilia una vez | N03 Firma inválida, timestamp vencido o replay |
| P04 Timeout produce estado incierto y consulta segura | N04 Timeout provoca segundo cargo inmediato |
| P05 Devolución autorizada registra compensación | N05 Usuario lector ejecuta reembolso |
| P06 Enlace vigente opera según TTL server-side | N06 Link expirado o revocado procesa pago |
| P07 Evento tardío deja evidencia sin sobrescribir liquidado | N07 Evento duplicado crea asiento duplicado |
| P08 A puede consultar exclusivamente sus conciliaciones | N08 Cache/correlación permite a A leer movimientos B |
| P09 Política comercial aprobada determina divisa/tarifa | N09 Default improvisado cobra sin política |
| P10 Logs muestran correlación redactada | N10 Registro incluye secret/HMAC/PII |

La enumeración no acredita pagos reales, pruebas HMAC/MySQL/HTTP, compatibilidad de proveedores, devolución ni transacciones verificadas.

## A0–A5 y plan de reversión

**A0:** revisar diff estrictamente Markdown de esta entrega y su sección autocontenida `CHANGELOG_AI.md`; confirmar owner único y no secretos. **A1:** estructura de diez entidades, 20 invariantes y diez pares futuros P/N; verificar que no se afirma funcionamiento. **A2:** conservación de JWT/RBAC, tenant A/B, HMAC, anti-replay, idempotencia, límites financieros y fail-closed. **A3:** coherencia con M01.01–06 LISTO+A5, DAG por ID y decisiones D01–D10 abiertas; .NET/MySQL/Playwright/PSP runtime NO_APLICA a cambios exclusivamente documentales. **A4:** fast-forward CAS real en `dev`, Scope Lock y procedencia V5 `completed/success` del SHA, BITACORA/PLAN/COLA/DAG readback. **A5:** revisión documental independiente posterior por otra Task Javier distinta del autor, nunca autoaprobación.

**Rollback del changeset:** `git revert` normal de los dos Markdown. No eliminar historial BITACORA ni modificar endpoints, secretos, cuentas PSP, pagos, QA/main/PROD, migraciones, webhooks ni sistemas productivos.
