# SOLQARYN V5 — C04-04.M01.02: entradas, salidas, actores y superficies de cobros por enlace

**Autor:** SOLQARYN PRIMARY DEV 05 — JAVIER, Task \`6adc2e55f47c81919f6374e561687076\`. **Fecha:** 2026-11-14. **Alcance:** \`solqaryn/Solqaryn@dev\`; \`PROJECT_SCOPE_LOCK=STRICT\`. **Naturaleza:** diseño preparatorio M01; **sin cambios de endpoints, cobros, workers, proveedores ni datos reales**.

## 1. Mandato y límites

Fuente única: \`STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES\`, Drive \`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA\`, \`PLAN_MAESTRO!A687:O687\`. El ID \`C04-04.M01.02\` exige *identificar entradas, salidas, actores y superficies afectadas* para preparar pagos por enlace, webhooks, reconciliación y reversión con contrato provider-neutral. El resultado no implementa esas funciones.

\`DAG_TECNICO!A648:G648\` declara \`DOC_M01_PREP\`, \`F=VALIDADO\`, \`G=SI\`; se constataron los 60 prerrequisitos documentales explícitos \`C01-01..04/C02-01..04/C03-01..04.M01.01..05\` en LISTO. \`C04-04.M01.01\` fue cerrado en DEV y sigue pendiente de revisión A5 independiente. Ninguna de estas señales libera fases de billing, QA o producción.

## 2. Fuentes de código y contrato verificadas, sin atribuirles pruebas nuevas

| Fuente en dev | Blob inspeccionado | Límite cierto |
| --- | --- | --- |
| \`backend/src/Application/Interfaces/IPagoOnlineProvider.cs\` | \`50ad09788c3ad31a1a727f9ae6b154834813ee26\` | Solicitud/resultado provider-neutral; no implica pasarela realmente configurada |
| \`backend/src/Application/Services/PagoOnlineService.cs\` | \`65272808514f2995f8c0abee5316aa872a9ce7a4\` | Valida saldo, monto, moneda e idempotencia local; no demuestra exclusión distribuida |
| \`backend/src/API/Controllers/PagosOnlineController.cs\` | \`99962b2957f672a6dd435895daffaba7333f8a6b\` | Superficie autenticada, permiso Facturacion/Aplicar para iniciar y chequeo de tenant; no autoriza por posesión de URL |
| \`backend/src/API/Controllers/InboundWebhooksController.cs\` | \`05bc86051f32c2de75b68877b79bf70b82c07078\` | Ingreso con validación de firma/tenant/proveedor y tamaño limitado; no demuestra settlement |
| \`backend/src/Domain/Entities/PagoOnline.cs\` | \`5063b0dc667583f7403fbeec879b519b6ca216d2\` | Intento asociado a empresa/factura y correlación de proveedor; no sustituye ledger |
| \`backend/src/Domain/Entities/WebhookEntrante.cs\` | \`90294acb096f4ab228bc974fce3be7e7eadbd9d0\` | Identificador externo, huella, estado y correlación; no ejecuta un dispatcher financiero |

El inventario corresponde a código existente en el HEAD del preflight, no a endpoints o pruebas de esta entrega.

## 3. Actores y límites de confianza

| ID | Actor / rol | Puede solicitar o producir | Control requerido / prohibición |
| --- | --- | --- | --- |
| ACT01 | Usuario autenticado de facturación del tenant | Solicitar inicio/consulta de intento | JWT vigente, membresía, permiso, empresa y factura en backend |
| ACT02 | Administrador de empresa autorizada | Configurar políticas futuras, revisar intentos propios | No administrar otra empresa ni secretos de provider por URL |
| ACT03 | Cliente pagador externo | Abrir enlace opaco generado por proveedor | La URL no es autorización RBAC ni acceso a factura ajena |
| ACT04 | API SOLQARYN | Validar solicitud y emitir respuesta segura | Nunca confiar en empresaId, importe o factura sin controles |
| ACT05 | Servicio Application de pagos | Coordinar idempotencia e inicio | Comprobar saldo/moneda, persistencia y respuesta de proveedor |
| ACT06 | Repositorio/DbContext | Persistir intento y recuperar estado por tenant | Aislamiento, integridad transaccional y claves únicas verificadas |
| ACT07 | Proveedor externo de pago | Generar referencia/link, notificar estado | Credenciales solo server-side; origen confiable o fallo cerrado |
| ACT08 | Emisor de webhook | Entregar evento firmado, id externo y payload | Validación criptográfica por tenant/proveedor, límite de tamaño |
| ACT09 | Reconciliador futuro (NO IMPLEMENTADO aquí) | Comparar evento con registro financiero aprobado | No afectar ledger por callback aislado o replay |
| ACT10 | Auditor autorizado | Consultar historia y correlación no secreta | Lectura por rol/tenant, sin PAN/CVV ni secret |
| ACT11 | Operador de soporte autorizado | Investigar incidentes, no aprobar cobros | Evidencia mínima, acceso segregado y trazabilidad |
| ACT12 | Política de reversión futura (NO IMPLEMENTADA) | Proponer compensación documentada | Aprobación específica y preservación de historial financiero |

## 4. Matriz de entradas, procesamiento, salidas y superficies

Los campos marcados **futuros** son especificaciones condicionadas; no se crea su interfaz ni se asegura que funcione.

| ID | Entrada | Origen / frontera | Salida esperada | Verificación antes del efecto |
| --- | --- | --- | --- | --- |
| IO01 | JWT, identidad y membresía | ACT01 -> API | Contexto de tenant autorizado o rechazo | Firma, permisos, estado de membresía |
| IO02 | EmpresaId, FacturaId, monto, moneda, proveedor | ACT01 -> controller de pagos | Inicio validado o error seguro | Tenant A/B, factura/saldo, importe, moneda |
| IO03 | Idempotency-Key | Cabecera cliente -> API/Application | Reutilización del mismo intento o conflicto | Longitud, scope empresa+proveedor+payload; unicidad durable requerida en etapas futuras |
| IO04 | Solicitud a \`IPagoOnlineProvider\` | Application -> proveedor externo | Referencia opaca, URL y estado | Proveedor configurado, sin exponer claves ni PAN/CVV |
| IO05 | Resultado de proveedor | Proveedor -> Application | Intento persistido o fallo controlado | Correlación con factura/empresa, URI válida y estado permitido |
| IO06 | Consulta individual de pago | ACT01 -> controller de pagos | DTO filtrado por tenant o 404/403 | Permiso Ver, empresa y registro que coincidan |
| IO07 | Listado/paginación de intentos | ACT01 -> controller | Página limitada del tenant | No exponer registros de empresa B |
| IO08 | HTTP webhook firmado | ACT08 -> controller de ingreso | Aceptado, duplicado, conflicto o rechazo | Proveedor/tenant configurados, firma, longitud, payload |
| IO09 | Identificador de evento externo + huella | Webhook ingress -> almacenamiento | Un registro idempotente o conflicto | No aceptar mismo identificador con payload distinto |
| IO10 | Evento de pago conciliable **futuro** | Almacén de eventos -> reconciliador | Decisión pendiente/aprobada/rechazada | Fuente financiera autorizada y equivalencia de importes |
| IO11 | Consulta de estado financiero **futura** | Reconciliador -> ledger de factura | Evidencia de estado contable coherente | No aplicar doble cargo ni cerrar deuda sin confirmación |
| IO12 | Solicitud de reversión **futura** | Operador autorizado -> flujo aprobado | Compensación auditable o rechazo | Permiso explícito, estado reversible, no borrar historia |
| IO13 | CorrelationId y sello UTC | API/Application/ingress -> auditoría | Traza sin datos sensibles | No registrar claves, PAN, CVV ni secretos |
| IO14 | Timeout/reintento del proveedor | Red/proveedor -> Application | Estado indeterminado seguro y reintento controlado | No interpretar timeout como cobro fallido o pagado definitivo |
| IO15 | Enlace opaco de pago | API -> pagador | Navegación al proveedor, no datos privilegiados | TLS, expiración/entrega según política futura, no conceder RBAC |
| IO16 | Controles de disponibilidad **futuros** | Operación -> monitor | Alerta/estado de salud | No crear dispatcher ni activar pagos por esta documentación |

## 5. Invariantes y escenarios contractuales futuros

**Reglas de no regresión:** NR01 aislamiento tenant; NR02 JWT/RBAC efectivo en API; NR03 separación entre URL de pago y autorización; NR04 factura propia con saldo/moneda coherentes; NR05 replay idempotente sin efecto doble; NR06 evento de webhook firmado y correlacionado; NR07 estados coherentes en reintentos y callbacks fuera de orden; NR08 reconciliación respaldada por fuente financiera aprobada; NR09 ausencia de PAN/CVV/secretos en API, logs o repositorio; NR10 rollback no destructivo, auditado y separado de la cancelación de suscripción.

**Positivos futuros, sin ejecutar:** P01 iniciar intento autorizado de tenant A; P02 consultar/recuperar su referencia opaca; P03 replay idéntico reutiliza resultado sin duplicar cobro; P04 webhook válido registrado una sola vez; P05 reconciliación futura confirma solo tras evidencia de fuente aprobada.

**Negativos futuros, sin ejecutar:** N01 JWT ausente; N02 permiso insuficiente; N03 EmpresaId B con credencial de A; N04 membresía revocada; N05 FacturaId de B; N06 monto mayor al saldo; N07 moneda distinta; N08 proveedor inexistente; N09 Idempotency-Key alterada o reutilización conflictiva; N10 webhook sin firma/secret; N11 mismo id externo con huella distinta; N12 timeout o callback tardío intenta conceder pago/entitlement sin conciliación. Se exigirá prueba real dirigida en la microtarea ejecutable que implemente cada contrato.

## 6. Riesgos, impacto, validación y rollback

**Impacto presente:** nuevo \`docs/v5/c04-04-m01-02-actores-interfaces-superficies-cobros.md\` y nueva sección íntegra en \`CHANGELOG_AI.md\`; las rutas Domain, Application, Infrastructure, API, frontend, DB y CI no se editan. Interfaces y consumidores futuros: facturación, suscripciones, metering, proveedores, webhook ingress, auditoría, reconciliación y checkout. No se modifica arquitectura, permisos, secretos, esquemas, servicios, datos productivos ni código de integraciones.

**A0 documental:** plan, DAG, propietario, SHA y blobs del sistema leídos; límites de confianza clasificados. **A1 documental:** actor/IO y cinco positivos especificados, NO runtime. **A2 documental:** diez invariantes y doce escenarios negativos/aislamiento definidos, NO certificación JWT/MySQL. **A3 documental:** entradas y superficies compatibles con código inspeccionado, sin afirmar reconciliación activa. **A4:** después de publicar exigir commit fast-forward, diff exacto de dos Markdown, control \`verify-solqaryn-project-scope\` y \`V5 - Sin encabezados vacios ni atribucion cruzada\` \`completed/success\` sobre SHA publicado, más readback PLAN/COLA/BITACORA. Pruebas backend/frontend/MySQL no se adjudican a cambios solo documentales (\`NO_APLICA\`, razonado). Si un check requerido está \`queued/in_progress/failed/skipped/cancelled\`, conservar VALIDANDO.

La aprobación **A5_PENDIENTE** requiere otra Task Javier distinta de la autora, y no autoriza QA/main/PROD. El rollback de esta documentación es \`git revert\` normal del commit, sin force-push y sin borrar BITACORA.
