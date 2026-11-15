# SOLQARYN V5 FINAL — C14-01.M02.02: contratos API/DTO, errores y compatibilidad de aceptación

**Tipo:** diseño documental de contratos, sin endpoints implementados. **Autor:** PRIMARY DEV 06 / `6af16bbad26881918c283abe06e1f32a`. **Fecha:** 2026-11-15. **Repositorio:** `solqaryn/Solqaryn@dev`; Scope Lock STRICT.

## Fuentes, dependencia y límites

[Plan V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO/COLA/DAG_TECNICO](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). `C14-01.M01.01–06` están LISTO con A5 independiente, incluido `C14-01.M01.06` sobre SHA `19bc00901492c173e40af6eeb47968eb7653b54d`. PLAN fila 2025 PENDIENTE sin owner, COLA G/O=SI y DAG F=VALIDADO/G=SI antes del reclamo. Esta unidad es paralela e independiente de M02.01: sólo toma M01.01–06+A5 como prerrequisito técnico, sin atribuir implementación a M02.01.

Las rutas siguientes son **contratos candidatos** para diseño y pruebas posteriores, no URLs desplegadas ni evidencia de implementaciones existentes. Los equipos de dominio/API validarán nombres y compatibilidad frente al código real antes de M03/M04. Ningún campo de solicitud de navegador puede certificar identidad, tenant, rol, permisos, estado fiscal o autorización PSP.

## Matriz API01–API12 propuesta (NO implementada)

| ID | Método | Ruta conceptual | Permiso backend requerido | DTO conceptual | Respuestas propuestas | Regla sensible |
| --- | --- | --- | --- | --- | --- | --- |
| API01 | GET | /public/catalog | anon | PublicCatalogView | 200/404 | No datos privados ni precios fuera de visibilidad |
| API02 | GET | /private/orders/{id} | orders.read | OrderView | 200/403/404 | Ownership backend por tenant y sucursal |
| API03 | POST | /private/orders | orders.create | CreateOrderCommand | 201/400/409 | Idempotency-Key y validación de stock |
| API04 | POST | /private/orders/{id}/confirm | orders.confirm | ConfirmOrderCommand | 200/409/403 | Version/ETag, no dobles confirmaciones |
| API05 | POST | /private/orders/{id}/cancel | orders.cancel | CancelOrderCommand | 200/409/403 | Auditoría, motivo y referencia |
| API06 | GET | /private/invoices/{id} | invoices.read | InvoiceView | 200/403/404 | No emitir factura por consulta |
| API07 | POST | /private/invoices/draft | invoices.draft | InvoiceDraftCommand | 202/400/409 | Emisor fiscal validado aparte |
| API08 | GET | /private/payments/{id} | payments.read | PaymentStatusView | 200/403/404 | Sólo PSP aprobado y propiedad persistida |
| API09 | POST | /private/payments/intents | payments.prepare | PaymentIntentCommand | 202/409/403 | No captura financiera en endpoint conceptual |
| API10 | GET | /private/stock/availability | stock.read | AvailabilityView | 200/403/400 | Sucursal autorizada y versión |
| API11 | GET | /admin/acceptance/runs/{id} | acceptance.audit | AcceptanceRunView | 200/403/404 | Sólo artefactos saneados y roles administrativos |
| API12 | POST | /admin/acceptance/runs | acceptance.execute | AcceptanceRunRequest | 202/403/409 | CI efímera autorizada, no QA/main/PROD |

Los DTO de entrada serán allowlisted y tipados; los identificadores públicos se resuelven a propiedad persistida antes de entregar datos. Las respuestas de error deben ser coherentes y no exponer stack traces, proveedor, secretos, pertenencia a otro tenant ni correlaciones internas sensibles. Los prefijos `/private` y `/admin` son **marcadores de alcance propuestos**, no un router efectivo.

## Validaciones V01–V14 (backend primero)

| ID | Condición de validación futura |
| --- | --- |
| V01 | JWT firmado y no expirado en private/admin |
| V02 | Membresía y revocación verificadas en servidor |
| V03 | Permiso backend efectivo por operación y recurso |
| V04 | Tenant de recurso persistido coincide con tenant autenticado |
| V05 | Sucursal pertenece a tenant y grant verificado |
| V06 | Propiedad legacy NULL se deniega por defecto |
| V07 | Version/If-Match obligatorio para mutaciones de estado |
| V08 | Idempotency-Key consistente, acotado y persistido durablemente en implementación futura |
| V09 | Cantidad e importe decimal positivo con escala y moneda compatibles |
| V10 | Estado permitido por máquina de transiciones |
| V11 | Emisor fiscal validado por backend antes de emitir |
| V12 | Proveedor de pago aprobado y secrets sólo en servidor |
| V13 | DTO no acepta rol, permisos ni tenant arbitrario como autoridad |
| V14 | Evidencia de aceptación sana sin PII/JWT/cabeceras sensibles |

Las validaciones JWT/RBAC, estado y ownership son gates de cada implementación real. El guard de Angular sólo mejora UX; nunca reemplaza autenticación/autorización server-side ni el enforcement transaccional.

## Contrato de errores tipados ERR01–ERR12 (propuesto)

| Error | HTTP futuro | Código lógico | Semántica |
| --- | --- | --- | --- |
| ERR01 | 401 | AUTH_REQUIRED | JWT ausente, revocado o no válido |
| ERR02 | 403 | PERMISSION_DENIED | RBAC o membresía denegada |
| ERR03 | 404 | RESOURCE_NOT_FOUND | No revelar existencia de recursos de otro tenant |
| ERR04 | 409 | STALE_RESOURCE_VERSION | Concurrencia y ETag obsoletos |
| ERR05 | 409 | IDEMPOTENCY_CONFLICT | Clave repetida con payload diferente |
| ERR06 | 422 | INVALID_STATE_TRANSITION | Estado comercial no compatible |
| ERR07 | 422 | INVALID_MONETARY_CONTRACT | Moneda o importe inválidos |
| ERR08 | 422 | INVALID_BRANCH_SCOPE | Sucursal ajena o no autorizada |
| ERR09 | 422 | FISCAL_ISSUER_UNVERIFIED | Autoridad fiscal no comprobada |
| ERR10 | 503 | PROVIDER_UNAVAILABLE | PSP/integración no disponible sin aceptar pago |
| ERR11 | 429 | REQUEST_RATE_LIMITED | Límite por usuario/tenant sin fuga |
| ERR12 | 412 | PRECONDITION_REQUIRED | If-Match o versión ausente |

Todos los cuerpos de error futuros incluirán `code`, `message` seguro y `correlationId` opaco cuando el backend lo genere; no se debe confiar en un `correlationId` aportado por el cliente como evidencia. Las respuestas 403/404 deben evitar filtración de existencia entre tenants A/B. Versiones HTTP/DTO y compatibilidad regresiva se negociarán al integrar rutas reales, sin asumir endpoints que aún no existen.

## Escenarios positivos/negativos FUTUROS_NO_EJECUTADOS

| Caso positivo | Oráculo | Caso negativo | Oráculo |
| --- | --- | --- | --- |
| P01 | Actor público ve catálogo limitado | N01 | Un anónimo solicita pedido privado y se rechaza |
| P02 | Miembro A obtiene su pedido con grant backend | N02 | Miembro B consulta id de A y se deniega sin filtrar datos |
| P03 | Reintento idempotente conserva una sola intención comercial | N03 | Payload distinto con misma clave devuelve conflicto |
| P04 | Revisión de aceptación recupera resultados saneados | N04 | Sin grant auditor, el artefacto queda inaccesible |

Ocho negativas adicionales:

- **N05:** Revocación o token expirado bloquea lectura privada.
- **N06:** Empresa seleccionada en DTO no sustituye tenant autenticado.
- **N07:** Documento con owner NULL no queda accesible por fallback.
- **N08:** Versión desactualizada bloquea confirmación concurrente.
- **N09:** Escala decimal o moneda no admitida bloquea mutación.
- **N10:** PSP sin autorización no genera cobro ni intención aceptada.
- **N11:** Evidencia con secreto/PII se rechaza o sanea sin retenerla.
- **N12:** CI CANCELLED/SKIPPED no habilita certificación ni despliegue.

Un test futuro necesita URL real, fixture A/B sintético, JWT/membresía respaldada por servidor, rol, recurso, status HTTP, payload esperado, SHA exacto, job con PASS y rollback. Este documento no ejecuta HTTP, no prueba EF/MySQL, no registra cobros ni verifica WCAG.

## Compatibilidad y política de evolución

Los DTO existentes no se cambian en esta microtarea. En implementación futura, toda nueva versión será aditiva, preservará los consumidores antiguos o tendrá plan explícito de transición, actualizará documentación y tests contractuales, y usará compatibilidad reversible bajo bandera tipada cuyo alcance/tenant provenga del backend. Rechazar `tenantId`, `EmpresaId`, `userRole`, `allowPayment` o `fiscalVerified` del cliente como autoridad. Los requests de aceptación no deben disparar efectos comerciales ni publicar entornos.

## A0–A5, evidencia y reversión

**A0:** revisión estática de dos Markdown, 12 rutas candidatas, 14 validaciones, 12 errores; sin modificación API/DTO existentes ni secretos. **A1:** oráculos escritos positivos P01–P04 y negativos N01–N12, todos futuros/no ejecutados. **A2:** cobertura documental de tenant A/B, revocación, idempotencia, PSP, fiscal y denegación por defecto, sin atribuir PASS runtime. **A3:** dependencia exclusiva de M01.01–06 LISTO+A5; compatibilidad conceptual con fronteras actuales Angular → API .NET10 → Application → persistencia MySQL; no se compila API por este artefacto. **A4:** FAST_FORWARD/CAS HEAD, evidencia Git de diff y blobs, checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` con `completed/success` en SHA exacto o equivalencia técnicamente acreditada; BITACORA/readback. Si CI falta, permanecer VALIDANDO. **A5:** revisión por otra Task Javier autorizada, distinta del autor.

Rollback documental: `git revert` normal, preservar BITACORA histórica, no cambiar contraseñas, secret manager, pagos, facturas, base de datos, dominio ni QA/main/PROD.
