# SOLQARYN V5 — C09-04.M01.04: riesgos, supuestos falsables y decisiones pendientes

**Unidad:** C09-04.M01.04 · PLAN_MAESTRO fila 1409 · **Autor:** PRIMARY DEV 06 JAVIER, TASK_ID 6af16bbad26881918c283abe06e1f32a · **Fecha:** 2026-11-14 · `solqaryn/Solqaryn` rama `dev`, PROJECT_SCOPE_LOCK=STRICT.

**Autoridad ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Alcance de este archivo:** registrar riesgos, supuestos y decisiones pendientes para futuros adaptadores de email, WhatsApp y otros canales, controles de webhook, replay, privacidad y no duplicación. Es únicamente análisis documental. **NO implementar ni habilitar eventos, webhooks, workers o proveedores** en esta etapa.

## Antecedentes verificables

- `docs/v5/c09-04-m01-01-alcance-mensajeria-multicanal.md`, blob `dbbed9aa74fa10f4b5e44dadf62edd2b7983ab24`, identifica NR01–NR14 y P01–P08/N01–N12 futuros.
- `docs/v5/c09-04-m01-02-actores-interfaces-mensajeria-multicanal.md`, blob `ab286319a4903bc0eb861eee76873a259455f362`, describe 14 actores, 12 interfaces y 9 fronteras de confianza, sin presumir que existan endpoints nuevos.
- `docs/v5/c09-04-m01-03-inventario-codigo-brechas-mensajeria.md`, blob `3626d69599d4c96f87c69464558dac31a9fa9ba8`, lista 14 fuentes reales con SHA y G01–G12 como brechas no certificadas.
- Código base observado: `MensajeOutbox`, `EmailEmpresarial`, `OutboxRetryPolicy`, `OutboxRetryProcessor`, `WhatsAppController`, `SmtpEmailService`. La existencia de estos componentes no demuestra campañas comerciales, consentimientos centralizados, callbacks universales ni efectividad del dispatcher.

## Riesgos R01–R14 y controles de aceptación futura

| Riesgo | Amenaza / modo de fallo | Mitigación requerida antes de implementar | Evidencia exigible, aún no ejecutada |
| --- | --- | --- | --- |
| R01 | Suplantación de empresa en cabecera/ruta | Contexto JWT/membresía verificado en backend | Rechazo tenant A→B con fixture |
| R02 | Mensaje promocional sin opt-in | Política de finalidad/canal y revocación antes de envío | Pruebas opt-out para cada canal |
| R03 | Dobles envíos por reintentos simultáneos | Idempotencia durable, claim CAS y deduplicación en proveedor | Dos workers concurrentes, un efecto máximo |
| R04 | Callback falso sin firma | Verificación HMAC server-side y validación de emisor | Firma corrupta 401/403 sin efecto |
| R05 | Replay tardío del webhook | Ventana temporal, nonce/ID consumible y TTL | Eventos repetidos fuera de ventana |
| R06 | Datos personales en plantilla/log | Minimización, redacción y controles tenant A/B | Sin PII en auditoría/errores |
| R07 | Token SMTP/WhatsApp filtrado | Referencias opacas, secretos fuera de repo y payload | Scanner y test de respuestas |
| R08 | Cliente legado EmpresaId NULL atribuido a empresa incorrecta | Fail-closed y reconciliación trazable | Caso NULL no se asigna implícitamente |
| R09 | Empresa cambia proveedor sin autorización | Configuración por tenant + roles y aprobación verificable | Usuario sin permiso no cambia canal |
| R10 | Reintentos ilimitados saturan proveedor | Backoff acotado y terminal dead-letter | Número máximo de intentos y tasa controlados |
| R11 | Error de proveedor afecta contabilidad | Límite transaccional y outbox posterior a commit | Fallo externo no duplica venta ni altera factura |
| R12 | Envío a destinatario equivocado por referencia cruzada | Validar recurso/tenant/sucursal/destinatario | Rechazo A→B e ID reciclado |
| R13 | Exportación de trazas exfiltra datos | Autorización y filtros tenant-scoped server-side | Exportación cruzada denegada |
| R14 | Canal alternativo no consentido | Prohibir fallback automático entre email/WhatsApp | Caída WhatsApp no envía email por defecto |

**Interpretación:** son riesgos de diseño; **no** constituyen vulnerabilidades confirmadas en la versión actual. La autorización para diagnosticar el riesgo no equivale a autorización para crear infraestructura, migraciones o tráficos externos.

## Supuestos S01–S08 con prueba de falsación

| Supuesto | Cómo se comprueba | Condición que lo invalida |
| --- | --- | --- |
| S01 Existe identidad de EmpresaId verificable para cada intento | Auditar contratos de tenant y fixtures | Identificador sólo del navegador |
| S02 Hay separación legítima de comunicaciones transaccionales y marketing | Revisar modelo de finalidad/consentimiento | Reuso de correo transaccional para campañas |
| S03 Los registros outbox admiten deduplicación de eventos | Inspección de unicidad e integración MySQL efímera | Clave duplicada persiste dos veces |
| S04 Los reintentos no esconden fallos permanentes | Test de retry/dead-letter | Recuperación infinita sin estado terminal |
| S05 El proveedor externo admite correlación/idempotencia | Contract test del proveedor autorizado | ProviderMessageId no correlaciona de forma estable |
| S06 Un webhook futuro puede verificarse criptográficamente | Prueba HMAC y anti-replay en sandbox | Callback sin firma fiable |
| S07 Los datos de contacto de legado tienen owner seguro | Tests de EmpresaId NULL | Cliente atribuido sin evidencia |
| S08 Es reversible la activación futura por tenant | Toggle seguro, rollback y auditoría | No se puede desactivar un canal |

**Todos los supuestos siguen abiertos** y deberán reevaluarse contra código y CI exact-SHA antes de una implementación. No es correcto marcarlos VALIDADOS por completar esta documentación.

## Decisiones D01–D08 pendientes de autoridad técnica

| Decisión | Opciones legítimas a evaluar | Gate que impide asumir resolución |
| --- | --- | --- |
| D01 Consentimiento | Granularidad por canal/finalidad/tenant | Evidencia legal/comercial y opt-out efectivo |
| D02 Destinatario y legado | Resolver pertenencia/identidad; si falta, rechazar | Fixtures de tenant A/B y NULL |
| D03 Forma de transportar email | Reutilizar SMTP existente o adaptador aprobado | Autorización de credenciales/cotas/contrato |
| D04 WhatsApp | Habilitar integración sólo con autorización distinta | No escanear QR ni usar tokens reales aquí |
| D05 Identidad de webhook | Clave y firma por tenant/proveedor, nonce | Pruebas criptográficas de replay |
| D06 Cola/outbox y reintentos | Aprovechar contratos existentes; no crear dispatcher ahora | Prohibición V5 de implementar eventos |
| D07 Retención y trazas | Metadatos mínimos, supresión y auditoría | Revisión de PII y seguridad |
| D08 Modo de rollback | Desactivar canal, restaurar estado sin reenvío | CI de idempotencia, migraciones y rollback |

Cada decisión se conserva en estado **PENDIENTE**, sin seleccionar proveedor, librería, cuotas de producción, secreto, despliegue ni solución arquitectónica transversal por esta M01.04.

## Casos de seguridad a convertir en tests; no se han ejecutado

**Positivos futuros P01–P08:** P01 intención autorizada tenant A; P02 consentimiento vigente; P03 replay idempotente; P04 render depurado; P05 acuse correlacionado; P06 retry acotado; P07 consultas de auditoría tenant-scoped; P08 revocación efectiva.

**Negativos futuros N01–N12:** N01 empresa B solicitada por actor A; N02 permiso ausente; N03 sucursal ajena; N04 cliente legacy NULL; N05 opt-out; N06 HMAC incorrecta; N07 nonce vencido; N08 secreto en respuesta; N09 payload diferente con misma clave; N10 retry ilimitado; N11 fallback de canal sin consentimiento; N12 exportación de PII tenant B.

**Etiqueta única de estos escenarios:** `FUTUROS_NO_EJECUTADOS`. Requieren tests .NET 10/EF Core 10/MySQL 8.4/Angular 22.2.1 según impacto real de fases M03–M05. La lectura de fuente o compilación CI de documentación nunca certifica envíos, permisos API efectivos o aislamiento del proveedor.

## Gate documental, dependencias, auditoría y reversión

`DAG_TECNICO!F1370/G1370=VALIDADO/SI` y `COLA!G1409/O1409=SI` habilitan la preparación **documental** de C09-04.M01.04 cuando PLAN H=PENDIENTE/I libre. No liberan M01.06/M06.06 ni JWT/RBAC/tenant, MySQL real, eventos, proveedores, QA/main/PROD. Cualquier cambio de código tendrá preflight propio, permisos, CI causal, SHA exacto y A5 por otro TASK_ID Javier.

Para cerrar DEV documental: inspección A0 del diff de sólo `docs/v5` y `CHANGELOG_AI.md`, A1 coherencia R/S/D y P/N no ejecutadas, A2 fronteras JWT/RBAC/tenant A/B y secretos, A3 referencias/blobs, A4 publicación fast-forward con `verify-solqaryn-project-scope` y check V5 provenance completados/success en SHA; tests runtime ajenos no son PASS. A5 independiente queda pendiente de otra Task. Rollback: `git revert` normal del commit en dev, conservando BITACORA histórica y sin force-push.

**Resultado:** matriz auditable de 14 riesgos, 8 supuestos falsables, 8 decisiones pendientes y pruebas futuras sin implementación ejecutable.
