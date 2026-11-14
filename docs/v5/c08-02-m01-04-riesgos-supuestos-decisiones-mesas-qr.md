# SOLQARYN V5 — C08-02.M01.04 — Riesgos, supuestos y decisiones pendientes de mesas y QR

**Autor:** PRIMARY DEV 06 — JAVIER; TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Fecha:** 2026-11-14. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, C08-02.M01.04. **Estado:** análisis documental de riesgos potenciales, NO defectos certificados, NO implementación de POS.

## 1. Alcance, evidencias y método

Se continúa `C08-02.M01.01` (NR01–NR14 y P01–P10/N01–N14), `C08-02.M01.02` (actores A01–A14, interfaces I01–I14 y fronteras F01–F09) y `C08-02.M01.03` (inventario de siete blobs ERP y brechas G01–G10). El objetivo del plan exige riesgos y decisiones para mesa, QR falso, pedido ajeno y cierre; este documento **no cambia** .NET, Angular, MySQL, JWT, RBAC ni datos.

Las observaciones de M01.03 son limitadas a archivos inspeccionados: no prueban ausencia total de un módulo restaurante en todo el árbol ni fallo ejecutado. Los riesgos de esta tabla son **hipótesis a verificar** durante M02–M06 mediante evidencia real, no bugs P0/P1 ni PASS de seguridad.

## 2. Riesgos R01–R14 — hipótesis y mitigación exigida

| Riesgo | Escenario potencial | Mitigación y futura prueba causal |
| --- | --- | --- |
| R01 | QR manipulado, clonado o de otra sede | Tratar URL como entrada no confiable; negar por firma/expiración/revocación y no revelar mesa existente |
| R02 | IDOR por MesaId/EmpresaId | Resolver servidor empresa+sucursal; casos A/B y sucursal cruzada sin efectos |
| R03 | Menú privado publicado por error | Proyección whitelisted de productos visibles con tests negativos y revisión de caché |
| R04 | Precio o IVA suministrado por cliente | Recalcular servidor con catálogo/impuestos canónicos y versiones |
| R05 | Replay de creación de pedido | IdempotencyKey+fingerprint+unicidad transaccional, conflicto de payload distinto |
| R06 | Doble cierre/cobro/factura | Máquina de estados terminal y confirmación comercial exactamente una vez |
| R07 | Concurrencia mesero/caja/cocina | Versión de pedido y bloqueo/transacción en MySQL efímero sin pérdida de actualización |
| R08 | Permisos de cocina y caja mezclados | Roles separados; JWT/RBAC y estado de membresía revocada revalidados backend |
| R09 | Inventario reservado dos veces | Coordinar pedido/inventario con compensación atómica; no inferir del prep ERP |
| R10 | PII o QR secreto en logs/cachés | Minimización, TTL y headers; auditoría saneada; no loguear JWT ni payload sensible |
| R11 | QR obsoleto operativo tras traslado | Ciclo de vida de mesa/sede/versión y revocación explicita |
| R12 | UI offline confirma estado falso | Mostrar estado pendiente y refetch del servidor, no prometer pago o cocina |
| R13 | Pedido anulado reabierto | Transición terminal validada en Domain/DB, rechazo 409 y reversión trazable |
| R14 | Caída a mitad de cierre comercial | Rollback normal y reconciliación auditada pedido/stock/factura; no abonar sin PSP autorizado |

Los riesgos R01/R02/R08/R10 son de seguridad y aislamiento; cualquiera que no pueda cerrarse con pruebas negativas **impide** liberar futuras interfaces. R05/R06/R07/R09/R13/R14 comprometen dinero, stock o consistencia y exigen MySQL transaccional real en CI efímera antes de certificar runtime. Una verificación del CHANGELOG no sustituye esas suites.

## 3. Supuestos falsables S01–S08

| ID | Supuesto de diseño (NO hecho acreditado) | Evidencia para aceptarlo o descartarlo |
| --- | --- | --- |
| S01 | El catálogo y la fiscalidad se obtendrán del servidor existente, no se duplicarán | Inventario de contratos M02 y pruebas precio A/B |
| S02 | La mesa tiene una relación inequívoca con empresa y sucursal | Modelo/índice/fixtures MySQL antes de M03 |
| S03 | La API podrá separar proyección pública de operaciones protegidas | Threat model y matrices RBAC futuras |
| S04 | Existe mecanismo futuro de revocación de QR sin cambiar seguridad global | Especificación de token/TTL/rotación, sin claves en repo |
| S05 | El pedido restaurante podrá convivir con PedidoVenta ERP sin romper cotización | Comparación de contratos e integración real |
| S06 | Un timeout no cambiará la identidad de la operación idempotente | Diseño de claves, dedupe y pruebas de reintento |
| S07 | Los operadores poseen membresías vigentes por empresa/sede | JWT/RBAC validado por servidor por operación |
| S08 | La UI de autoservicio debe funcionar móvil y accesible | Pruebas Angular y Playwright futuras bajo red lenta |

No convertir estos supuestos en autorización técnica. Cualquier incompatibilidad con arquitectura, Scope Lock, proveedor real, JWT/RBAC o flujo comercial obliga a reevaluar el scope afectado sin saltar la secuencia. La interfaz pública no adquiere privilegios de caja/cocina aunque el visitante presente un QR válido.

## 4. Decisiones pendientes D01–D08 — NO RESUELTAS

| ID | Decisión futura requerida | Estado y condición mínima |
| --- | --- | --- |
| D01 | Entidad Mesa y relación sucursal/empresa | PENDIENTE: Pendiente de contrato Domain, migración versionada y tenant A/B |
| D02 | Estructura opaca, firma, expiración y revocación de QR | PENDIENTE: Pendiente de amenaza y manejo de secretos |
| D03 | Política de visibilidad del menú y caché pública | PENDIENTE: Pendiente de ACL y pruebas de contenido privado |
| D04 | Estado de cocina versus pedido comercial ERP | PENDIENTE: Pendiente de integración semántica y compatibilidad |
| D05 | Idempotencia y deduplicación de pedido/cierre | PENDIENTE: Pendiente de clave, fingerprint, índices y transacción |
| D06 | Permisos por rol/sede para mesero/cocina/caja | PENDIENTE: Pendiente de autorización backend y fixtures negativos |
| D07 | Facturación, inventario y pago tras cierre | PENDIENTE: Pendiente de estrategia reversible y no habilitar PSP |
| D08 | Auditoría, retención y protección de PII | PENDIENTE: Pendiente de redacción de logs, legalidad y rollback |

No hay decisión autorizada sobre proveedor de pago, webhook, firma real, migración MySQL, nueva base, QR productivo, Docker o cambio de roles. Una decisión pendiente es evidencia de alcance no resuelto, no permiso para ejecutar un diseño por defecto.

## 5. Escenarios futuros positivos y negativos TP01–TP08

| Escenario | Positivo observable por probar | Negativo obligatorio por probar |
| --- | --- | --- |
| TP01 | QR A vigente abre menú público de sede A | QR modificado, expirado, revocado o B no revela datos |
| TP02 | Mesero autorizado crea pedido de mesa A | Cambiar MesaId/SucursalId/EmpresaId a B recibe rechazo sin persistencia |
| TP03 | Cliente observa precio y stock canónicos servidor | Alterar total, IVA o producto oculto en request no cambia cobro |
| TP04 | Retry igual produce misma operación | Replay concurrente/diferente payload no duplica pedido o factura |
| TP05 | Cocina propia avanza estado permitido | Rol cocina B o revocado no avanza ni lee pedido A |
| TP06 | Cajero autorizado cierra exactamente una vez | Cierre repetido, stale ETag o rollback no duplica cargo ni stock |
| TP07 | UI móvil anuncia estados accesibles y errores | Fallo de red no muestra estado paid/confirmado ficticio |
| TP08 | Reversión conserva auditoría y conciliación | Falla de MySQL/PSP no deja datos inter-tenant o saldo inconsistente |

**TP01–TP08 son FUTUROS NO_EJECUTADOS.** Se diseñan oráculos para M01.05 y etapas posteriores. Las pruebas requeridas por cambios de código (xUnit, DB MySQL 8.4 efímera, Angular/Playwright, seguridad A/B) se ejecutarán sólo cuando exista su implementación pertinente y checks exact-SHA, nunca se imputan a este análisis Markdown.

## 6. Gates, impacto, trazabilidad y reversión

`PLAN_MAESTRO!A1193:I1193`, `COLA!G1193/O1193` y `DAG_TECNICO!F1154/G1154` permiten reclamar **M01.04 documental** si al momento de la reclamación conservan PENDIENTE sin owner / SI-SI / VALIDADO-SI. No liberan M01.06, M02+, IAM/JWT/RBAC/tenant, DB/migraciones, pagos, webhooks ni QA/main/PROD. Revalidar las fórmulas, dependencias y A5 independientes en cada cierre posterior.

**Impacto:** `docs/v5` y sección aditiva `CHANGELOG_AI.md`; Domain/Application/Infrastructure/API/Angular permanecen intactos. Interfaces DTO, persistencia, transacciones, secretos, roles y usuarios reales no se alteran. Las medidas aquí son requisitos futuros de observabilidad y rollback, no cambios instrumentados.

**Auditorías para esta microtarea:** A0 coteja plan, identidad, branch dev, Scope Lock y coherencia de R/S/D/TP. A1 revisa trazabilidad R01–R14/S01–S08/D01–D08/TP01–TP08 y referencias previas, sin confundir revisión documental con test funcional. A2 inspecciona diseño de QR falso, mesa ajena, PII, tenant A/B, JWT/RBAC, replay y doble cierre. A3 contrasta inventario M01.03 y arquitectura; runtime .NET/Angular/MySQL/Playwright es `NO_APLICA` al diff sólo Markdown, no PASS. A4 requiere commit fast-forward en dev, Scope Lock y control V5 de CHANGELOG `completed/success` exact-SHA, BITACORA y readback PLAN/COLA. A5 es independiente por otra de las cinco Tasks Javier y permanece pendiente sin autoaprobación.

**Rollback:** `git revert` normal del commit Markdown y un evento compensatorio aditivo, nunca editar BITACORA histórica. Ningún despliegue, movimiento de stock, cargo, migración, proveedor, nueva Task ni recurso pagado.
