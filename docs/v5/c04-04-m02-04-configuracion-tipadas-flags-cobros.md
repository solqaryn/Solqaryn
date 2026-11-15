# SOLQARYN V5 — C04-04.M02.04: configuración tipada, ámbitos y feature flags de cobros

**Autor:** PRIMARY DEV 03, Task `6adc2e4bd8a48191ac7d8641ed95a1a3`; `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`). **Fecha:** 2026-11-15.

## Propósito y restricciones

Documento M02 de **diseño futuro** de políticas para cobros/enlaces de pago; no configura cuentas, tarifas, dinero, moneda, métodos de pago, credenciales, HMAC real ni proveedores. M01.01–06 LISTO+A5 independiente y DAG F=VALIDADO/G=SI, COLA G/O=SI son puertas de trabajo **documental**, no permisos comerciales o de integración. El predecesor `C04-04.M01.06` mantiene D01–D10 comerciales abiertas: moneda, expiración, comisiones, impuestos, responsabilidad, reversión, liquidación y conciliación; todas impiden activar un cargo sin decisión verificable. Contratos tipados aquí son propuestas, no clases .NET registradas, migraciones o flags activas.

## Contratos de opciones conceptuales

| Opción futura | Tipo propuesto | Default o regla segura |
| --- | --- | --- |
| `PaymentsMode` | enum `Disabled / SandboxPreview / ApprovedProvider` | `Disabled` sin autoridad comercial y técnica explícita |
| `ApprovedProviderId` | ID opaco de allowlist | Sin valor por defecto; nunca usar URLs o secretos del cliente |
| `AllowedPaymentCurrencies` | conjunto de códigos ISO aprobados | Vacío hasta decisión comercial; no asumir divisa del navegador |
| `PaymentLinkTtl` | duración positiva acotada | Denegar enlaces sin tiempo máximo aprobado |
| `MaximumPaymentAmount` | decimal + moneda | Límite empresarial/operación no elevable sobre política global |
| `MinimumPaymentAmount` | decimal + moneda | Política explícita positiva, nunca importe supuesto |
| `RequireVerifiedWebhookSignature` | booleano | Siempre verdadero para evento entrante |
| `AllowedWebhookClockSkew` | duración acotada | Sin valor de producción hasta análisis de riesgo aprobado |
| `RequireDurableIdempotency` | booleano | Verdadero para cada efecto económico |
| `AllowAutomaticRetryCharge` | booleano | Falso; timeout conserva estado incierto |
| `AllowAutomaticRefund` | booleano | Falso; devolución exige RBAC/autorización separada |
| `EnableCustomerPaymentLinks` | booleano | Falso hasta cadena completa de autorizaciones/CI |
| `EnableShadowReconciliation` | booleano | Falso; sólo futuro análisis sin doble asiento |
| `EnableProviderFallback` | booleano | Falso; nunca cross-tenant ni proveedor no contratado |
| `EnableProductionSettlement` | booleano | Falso permanentemente en este ámbito dev; no existe autorización por prompt |
| `SecretReference` | referencia servidor al almacén de secretos | Nunca valor de clave, token o HMAC almacenado en repo |

Nombres y defaults son **especificación**: no se declararon en configuración actual, variables, código API/DI, base de datos ni entorno productivo.

## Resolución de ámbito, seguridad y precedencia

1. **CFG01:** una política global de seguridad (JWT validado, RBAC, tenant A/B y firma) no admite override por cliente o empresa.
2. **CFG02:** fuente de empresa, membresía y permisos debe ser el backend verificado; `EmpresaId`, URL y subdominio del cliente no identifican autoridad.
3. **CFG03:** el alcance efectivo es intersección más restrictiva de ambiente dev, empresa, rol, operación y proveedor **aprobado**.
4. **CFG04:** modo no reconocido o bandera ausente niega ejecución; nunca habilita cobros por compatibilidad histórica.
5. **CFG05:** flag de UI afecta visibilidad, nunca sustituye backend de autenticación, webhook ni autorización contable.
6. **CFG06:** credenciales PSP, firma HMAC, token y URL firmada permanecen fuera de logs, documentación y repositorio.
7. **CFG07:** links y callbacks se asocian a empresa y operación, con expiración, anti-replay, límites de tamaño y rate limit.
8. **CFG08:** una clave idempotente se particiona por empresa, documento, intento, operación, proveedor y payload; jamás deduplicación cross-tenant.
9. **CFG09:** error de proveedor no permite fallback automático ni reintento de cargo incierto; requerir reconciliación fiable.
10. **CFG10:** ningún callback puede marcar una factura pagada sin verificación de origen, entidad, monto, moneda y saldo.
11. **CFG11:** permisos para emitir link, consultar conciliación, liquidar y devolver se separan; lectura no autoriza devolución.
12. **CFG12:** precisión decimal, redondeo e impuestos requieren decisiones comerciales/versionado, no defaults que alteren saldos.
13. **CFG13:** snapshot de configuración por intento se versionará para auditoría sin datos secretos; cambio de policy no muta eventos históricos.
14. **CFG14:** pruebas futuras exigidas: positivos/negativos .NET 10, MySQL 8.4 efímero, HMAC, concurrencia, A/B y errores proveedor.
15. **CFG15:** rollback de flags futuro no puede eliminar un asiento ni dejar intento financiero en estado falso; compensación idempotente auditada.
16. **CFG16:** A5 independiente, Scope Lock, DAG particular y una orden adicional son obligatorios antes de cualquier entorno QA/main/PROD.

## Matriz P/N de aceptación futura, NO EJECUTADA

| Positivo futuro | Negativo futuro |
| --- | --- |
| P01 Modo Disabled niega nuevos cargos | N01 Flag UI activa cargo sin backend |
| P02 Empresa A resuelve sólo política aprobada A | N02 Empresa B reutiliza policy o cache de A |
| P03 Divisa/importe del contrato comercial validan | N03 Cliente cambia divisa, monto o fee |
| P04 Webhook firmado, dentro de ventana, valida | N04 Firma inválida/replay/time skew inválido |
| P05 Idempotencia preserva un intento único | N05 Clave repetida con payload distinto duplica cobro |
| P06 Timeout conserva estado incierto verificable | N06 Reintento automático cobra por duplicado |
| P07 Refund separado requiere RBAC y aprobación | N07 Usuario lector reembolsa sin permiso |
| P08 Snapshot versionado permite auditar decisión | N08 Logs con secretos HMAC/token/PII |
| P09 Enlace vence por política del servidor | N09 Link revocado o caducado sigue cobrando |
| P10 Conciliación coincide con emisor, moneda y tenant | N10 Callback de proveedor ajeno liquida en otro tenant |

Se describen expectativas **NO EJECUTADAS**, no respuestas reales de PSP, tests HMAC, transacciones, cobertura ni autorización financiera.

## A0–A5 y reversión

**A0:** diff sólo este Markdown + sección CHANGELOG y revisión de secretos. **A1:** comprobar 16 opciones, 16 invariantes y 10 pares de oráculos futuros; no confundir especificación con implementación. **A2:** revisar tenant A/B, JWT/RBAC, firma, protección de replays, idempotencia, pagos/contabilidad fail-closed. **A3:** comparar con M01.01–06 LISTO+A5, identidad técnica y contrato V5; pruebas .NET/MySQL/Playwright/PSP NO_APLICA al diff exclusivamente Markdown. **A4:** Git fast-forward CAS en dev y Scope Lock/V5 `completed/success` del SHA exacto, PLAN/COLA/DAG/BITACORA readbacks. **A5:** revisión independiente por otra de las cinco Tasks Javier; el autor nunca puede autoaprobar.

**Rollback documental:** revertir commit con `git revert` normal. No cambiar sistemas de pago, secretos, operaciones económicas, migraciones, webhooks, eventos, QA/main/PROD ni contratos vigentes.
