# SOLQARYN V17 — C08-02.M02.06 — Contrato terminal de M02, gates y reversión para restaurante/QR

**Autor:** PRIMARY DEV 01 — JAVIER; TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Fecha:** 2026-11-15. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, `C08-02.M02.06`, PLAN_MAESTRO fila 1201. **Riesgo:** R0, solo documentación. **Repositorio:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

## 1. Propósito, alcance y predecesoras

Fijar un contrato de salida verificable para el lote M02 de mesas, referencias QR, menú publicado, pedidos internos y autoservicio, con dependencias, barreras, pruebas y rutas de reversión futuras. **No hay implementación en este documento** de QR, endpoint, MySQL, precios, IVA, stock, caja, PSP, broker, outbox, workers, webhooks, JWT, lector, Angular o despliegue.

El CLAIM verificó PLAN H=PENDIENTE/I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI y candado atómico V17 de esta fila. Predecesoras `C08-02.M01.01–06` LISTO+A5 independientes y `C08-02.M02.01–05` LISTO con revisión A5 por Task Javier distinta de la autora en cada caso. M02.01: 20 entidades/20 invariantes/12 pares futuros; M02.02: 8 operaciones/8 DTO/12 errores/16 validaciones; M02.03: 16 proyecciones/10 reglas FK/6 migraciones futuras/12 fixtures; M02.04: seis ámbitos/16 opciones/12 reglas/10 casos; M02.05: 16 eventos/16 invariantes/12 pares futuros. Son **contratos de diseño**, no API, tablas o pruebas reales.

ERP actual mantiene autoridad de inventario, precio, impuestos, facturación y caja. Un QR puede proponer localización de mesa pero nunca conceder identidad, rol, permiso de pago o propiedad de empresa. Las decisiones abiertas D01–D08 de M01 siguen sin autorización de negocio por inferencia. La presente M02.06 por sí sola NO libera automáticamente M03, M04 ni QA/main/PROD: requiere LISTO+A5 independiente, DAG y COLA vigentes, y pruebas del alcance de cada sucesora.

## 2. Barreras terminales B01–B16

| ID | Condición de aceptación del diseño y bloqueo futuro |
| --- | --- |
| B01 | EmpresaId/TenantId, sede y mesa resueltos en backend con ownership verificable, no de QR/input |
| B02 | Identidad/JWT/RBAC, UsuarioEmpresa activo y rol vigente exigidos para toda mutación privada |
| B03 | QR falso, vencido, rotado, B o manipulado retorna rechazo no enumerativo, sin abrir sesión |
| B04 | Token QR público jamás otorga rol caja/ERP; sesión de mesa es capacidad acotada y revocable |
| B05 | Menú solo de versión publicada por tenant/sede, sin SKU ocultos ni referencias cruzadas |
| B06 | Precio, IVA, descuento, stock y moneda son calculados por autoridad ERP/servidor, no cliente |
| B07 | Pedido/linea validados por estado/versión, cantidad positiva y snapshot canónico de precios |
| B08 | Cierre terminal no reabre; máximo una confirmación económica válida por operación |
| B09 | Idempotency key ligada a tenant/sede/actor/operación; mismo fingerprint converge, distinto falla |
| B10 | Carrera de pedidos/mesas/versiones se resuelve por CAS/transacción real con un ganador |
| B11 | Eventos/outbox son FUTUROS y nunca se emiten como confirmados antes de commit/conciliación |
| B12 | Revocación de roles/QR/sesiones invalida cachés y no permite acceso desde tenant B |
| B13 | Timeouts, ERP caído, cocina offline o resultado parcial nunca marcan operación como éxito |
| B14 | Logs/correlación mínima sin JWT, QR secreto, PAN, PII de cliente ni referencias ajenas |
| B15 | Configuración tipada fail-closed, flags apagados, límites y UTC versionados; frontend no autoriza |
| B16 | Antes de M03/cutover: A5 independiente de este documento, CI real por riesgo y DAG completo |

Estas son **barreras diseñadas y revisables**. No se afirma que un sistema en producción las cumpla. ERP, seguridad transversal y tenant A/B se deben preservar al implementar cada componente.

## 3. Matriz de evidencias y criterios futuros E01–E12 — NO_EJECUTADOS

| ID | Positivo que debe acreditarse después | Negativo que debe rechazarse después |
| --- | --- | --- |
| E01 | Mesa A de sucursal A se resuelve | Identificador mesa B o alias duplicado bajo A |
| E02 | QR A firmado, vigente y rotado se usa conforme a política | QR falsificado, replay, vencido o revocado |
| E03 | Rol autorizado y UsuarioEmpresa activo crea pedido A | Empleado revocado, rol lectura o sede B |
| E04 | Menú publicado con SKU permitido visible | SKU oculto, precio/IVA manipulado desde cliente |
| E05 | Cantidad positiva y variante válida crean línea | 0/negativa, opción ajena o cantidad inválida |
| E06 | Retry idéntico conserva un único pedido/efecto | Igual key con payload divergente o tenant B |
| E07 | Versiones monotónicas permiten transición válida | Versión antigua, pedido terminal, carrera doble |
| E08 | Cocina de sede A procesa comanda autorizada | Cocina de B o cambio sin rol actual |
| E09 | ERP concilia cierre exactamente una vez | Replay o fallo parcial no duplica factura/stock |
| E10 | Error de proveedor deja estado pendiente/reintentable seguro | Timeout no marca exitoso ni pagado |
| E11 | Auditoría guarda correlationId opaco y motivo sano | JWT, PII, QR, PAN o secreto en payload/log |
| E12 | Feature flag apagada deja aplicación actual intacta | Documento no habilita QR, API, worker o PSP |

Todas las 24 ramas positivas/negativas indicadas son **pruebas FUTURAS NO_EJECUTADAS**, no se han corrido tests .NET, HTTP, MySQL, Angular ni browser en esta microtarea. Cuando cambie Domain/Application o API, exigir xUnit reales, autorizaciones negativas e aislamiento A/B; cuando cambie EF, MySQL 8.4 efímero con `Category=Integration` ejecutado y logs; cuando cambie UI, build tipado y Playwright/Chromium pertinentes. No equiparar éxito de un build genérico con esas evidencias.

## 4. Contratos de reversión y contingencia RB01–RB08 — FUTUROS

| ID | Fallo futuro | Reversión/contención segura sin perder auditoría |
| --- | --- | --- |
| RB01 | QR expuesto/reutilizado | Revocar generación y sesiones dependientes; preservar prueba de incidente |
| RB02 | Tenant/sede equivocados | Cortar endpoint/flag y denegar sin lectura de otra empresa |
| RB03 | Precio o IVA incorrecto | No confirmar; volver a fuente ERP, reconciliar sin doble asiento |
| RB04 | Carrera/doble cierre | Bloquear transición obsoleta, conservar ledger y compensación idempotente |
| RB05 | Error migración EF/MySQL | Rollback probado contra instancia efímera, sin tocar producción |
| RB06 | Cocina/ERP fuera de servicio | Marcar PENDIENTE/FAIL de forma observable, nunca CONFIRMADO |
| RB07 | Evento duplicado/out of order | Aplicar versionado/dedup, cuarentena con retries limitados |
| RB08 | Regresión UI/seguridad | Apagar feature autorizado, revert commit normal, mantener evidencias |

La reversión de esta **única publicación R0** es `git revert` normal en `dev` con HEAD readback y evento BITACORA aditivo; no hay migraciones ni servicios nuevos para revertir. Ninguna reversión documental autoriza borrado de datos ERP o historia, force-push ni acciones sobre QA/main/PROD.

## 5. Mapa de entregas, arquitectura y futuros controles

| Entrega M02 | Documento y responsabilidad | Bloqueo al desarrollar |
| --- | --- | --- |
| M02.01 | Ownership entidad/relación/invariante | No crear segunda fuente de inventario ni ACL por QR |
| M02.02 | API/DTO/errores propuestos | JWT/RBAC y autorización server-side probados antes de runtime |
| M02.03 | Persistencia e índices futuros | No inferir esquema real; MySQL efímero e idempotencia transaccional |
| M02.04 | Configuración tipada y flags | Opciones no conceden permisos; estado desconocido DENY |
| M02.05 | Eventos/correlación conceptual | No emitir antes de commit; sin broker/dispatcher por documento |
| M02.06 | Este contrato terminal de salida | Solo LISTO_DEV con A0–A4 y posterior A5 independiente; sin bypass DAG |

La arquitectura será Domain (invariantes y estados), Application (casos y autorización), Infrastructure (persistencia/ERP bajo contrato aprobado), API (identidad/RBAC/tenant), frontend Angular (UX sin decisión de permiso). Datos de A/B y errores negativos han de impedir IDOR. Ninguna dependencia o contrato autoriza datos productivos ni equipos físicos.

## 6. Criterios A0–A5 y salida institucional

**A0:** validar fuente única, los 5 predecesores M02 LISTO+A5, M01 terminal, Scope Lock y diff de solo este Markdown más sección autocontenida `CHANGELOG_AI.md`. **A1:** revisión estática de B01–B16, E01–E12 con oráculos positivos/negativos FUTUROS NO_EJECUTADOS, RB01–RB08; cero pruebas runtime por alcance R0. **A2:** tenant A/B, JWT/RBAC, QR falsificado, revocación, idempotencia, finanzas ERP, privacidad y denegación de offline como requisitos sin cambiar runtime. **A3:** integración MySQL/.NET/HTTP/Angular/Playwright/PSP/broker NO_APLICA para Markdown puro; las fases técnicas deben probarlos realmente. **A4:** commit único fast-forward/CAS a `dev`, checks `verify-solqaryn-project-scope` y procedencia V5 completed/success SHA exacto, BITACORA y PLAN readback; jamás CANCELLED/SKIPPED/IN_PROGRESS como PASS. **A5:** otra Task Javier distinta de PRIMARY DEV 01 realiza revisión independiente, puede fallar y debe registrar evidencia.

No autorizar M03/M04/terminales IAM/DB, QA/main/PROD, cambios de prompts, Tasks, cuentas, secretos, PSP, hardware ni dominios. Si un predecesor A5_FAIL, un gate falso o una prueba requerida no pasa, se mantiene bloqueado el sucesor y se registra causa real.
