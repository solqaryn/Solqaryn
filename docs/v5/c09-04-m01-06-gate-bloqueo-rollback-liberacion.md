# SOLQARYN V5 — C09-04.M01.06: bloqueo, rollback y condición de liberación documental

**ID institucional:** C09-04.M01.06 · PLAN_MAESTRO!1411 · **Riesgo:** R0 documental · **Revisión:** A5 por una Task Javier distinta de DEV03.  
**Alcance:** únicamente preparación del lote M01 de mensajería multicanal en `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. No habilita envío, WhatsApp, correo comercial, proveedores, webhooks, workers, eventos, migraciones, datos reales, QA/main/PROD ni permisos adicionales.

## Baseline y autoridad comprobable

La fuente ejecutable es el *STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES* V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`). El ID figura PENDIENTE, sin owner antes del CLAIM exclusivo V17, con COLA G/O=SI y DAG_TECNICO F=VALIDADO/G=SI; esas puertas permiten preparar este documento y no sustituyen A0–A5.

El lote C09-04.M01.01–05 constaba LISTO al corte de esta entrega, con revisiones A5 independientes registradas; se preservan autores, SHA y bitácora originales. Referencias de publicación: M01.01 `e5bf1807`, M01.02 `46159fb5`, M01.03 `e3f8f935`, M01.04 `e35ff707`, M01.05 `eab3e72a`. La M01.01 inventarió nueve blobs de Domain/Application/API/Infrastructure verificables en el ancestro `cf04aad8`; esa inspección no demuestra que SMTP, WhatsApp, webhook o MySQL hayan sido ejecutados. La M01.06 no modifica dichas capas.

## Bloqueos obligatorios, sin liberación manual

| Gate | Condición que bloquea avance de implementación posterior | Prueba futura exigible |
| --- | --- | --- |
| G01 | Empresa/tenant de intención, destinatario o recurso sin correspondencia autorizada | Positivo tenant A y rechazo cruzado A→B en API/DB aislada |
| G02 | JWT, membresía, RBAC, revocación o consentimiento insuficiente | Denegación de usuario sin rol, sesión revocada y opt-out |
| G03 | Idempotencia o exclusión de reintentos no demostrada | Dos escritores/replays no duplican efecto externo |
| G04 | Contacto legado con EmpresaId nulo o ambiguo | No se asigna tenant por suposición |
| G05 | Secreto de transporte, PII o payload sensible en logs/API | Regresión negativa de filtración |
| G06 | Firma HMAC, ventana temporal y replay de webhook no acreditados | Firma incorrecta, expiración y replay rechazados |
| G07 | Retries/dead-letter sin límite o fallback entre canales no consentido | Límite, auditoría, recuperación y rechazo de fallback |
| G08 | Cualquier check causal pendiente, fallido, omitido, cancelado o SHA no equivalente | CI requerida completed/success del SHA o equivalencia de blobs auditada |
| G09 | A5 independiente faltante, A5_FAIL sin reparar, propietario ambiguo o candado V17 ocupado | Reviewer TASK_ID distinto, readback institucional y exclusividad real |

Estas condiciones son **criterios**; no son implementaciones, certificaciones ni pruebas ejecutadas. Un gate técnico SI no autoriza operaciones R3, secretos, despliegues ni bypass de seguridad.

## Reversión y recuperación

- **Rollback de esta entrega R0:** revertir exclusivamente su commit de documento y CHANGELOG con `git revert` normal desde HEAD vigente, sin force-push ni reescritura de historia; conservar BITACORA y resultados de revisiones.
- **Rollback futuro de mensajería:** diseñar una estrategia por proveedor y tenant que detenga nuevos envíos, preserve registros ya emitidos e idempotency keys y evite doble notificación. No ejecutar rollback real ni asegurar restauración de datos sin ambiente autorizado y pruebas.
- **Ante CI fallida o conflicto:** mantener VALIDANDO con SHA/RUN comprobables; reparar la causa en dev sin degradar checks y pedir nueva A5 si cambia el alcance; no escribir LISTO por un build verde aislado.
- **Ante interrupción o lock huérfano:** conservar owner y artefactos; aplicar V17/V18 con ausencia real de escritor, candado verificado y evento idempotente. No liberar el candado de otra Task por antigüedad.

## Aceptación documental verificable

**Positivo P-DOC-01:** M01.01–06 tienen cada una entrega real, owner válido, A0–A4 causales y A5 aprobada por revisor distinto, con ninguna puerta de seguridad pendiente; únicamente entonces la fórmula DAG/COLA puede liberar una sucesora autorizada.

**Negativos N-DOC-01–06:** (1) falta A5, (2) check requerido distinto de completed/success, (3) tenant A/B sin protección, (4) opt-out ignorado, (5) lock activo o doble escritor, (6) rollback que borra evidencias: cada condición debe impedir la liberación, aunque un dashboard muestre SI. Son verificaciones de requisitos escritas, **no pruebas de runtime ejecutadas**.

**A0:** doc y CHANGELOG son los únicos archivos a publicar, sin cambio de API, DTO, datos ni arquitectura. **A1:** un positivo y seis rechazos documentales revisables. **A2:** JWT/RBAC/tenancy/consentimiento preservados como límites; NO_APLICA prueba HTTP real por no modificar runtime. **A3:** NO_APLICA MySQL/Angular/Chromium/proveedor real. **A4:** exigir Scope Lock y V5 SUCCESS sobre SHA exacto más readback PLAN/COLA/DAG/BITACORA; el resto de CI ajena no se cuenta PASS. **A5:** PENDIENTE de revisión independiente; autor DEV03 no puede aprobarse.

**Liberación institucional:** no editar manualmente COLA G/O ni DAG F/G. Conservar este ID VALIDANDO mientras falte CI exigible; transicionar a LISTO_DEV solo tras A0–A4, registrar idempotencia y solicitar A5 externa. M01.06 LISTO por sí solo no habilita M02 ni QA/main/PROD.
