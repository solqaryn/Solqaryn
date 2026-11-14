# SOLQARYN V5 — C04-04.M01.05: aceptación funcional y técnica observable de cobros por enlace

**Autor:** SOLQARYN PRIMARY DEV 05 — JAVIER; `TASK_ID=6adc2e55f47c81919f6374e561687076`. **Fecha:** 2026-11-14. **Ubicación:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Tipo:** matriz documental de aceptación, no certificación de cobros.

## 1. Fuente, alcance y premisas

Microtarea `C04-04.M01.05`, `PLAN_MAESTRO!A690:O690`, autorizada como `DOC_M01_PREP` en `DAG_TECNICO!A651:G651` (`F=VALIDADO/G=SI`) conforme al documento V5 FINAL `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. El bloque requiere preparar **enlaces de cobro, webhooks, conciliación y reversión** sin acoplar proveedor; esta unidad define criterios verificables, no ejecuta transacciones.

Fuentes del mismo bloque, en `dev`: `C04-04.M01.01` (objetivo y límites), `.02` (12 actores y 16 interfaces), `.03` (13 blobs y 12 brechas técnicas) y `.04` (16 riesgos y 10 decisiones abiertas). Esas tareas están en LISTO_DEV pero sus revisiones A5 independientes se requieren para los cierres que las exijan; no se reinterpretan como aprobación comercial ni capacidad live.

**Estado de todos los casos AC01–AC20:** `DEFINIDO_DOCUMENTAL / NO_EJECUTADO`. La especificación de aceptación no constituye PASS funcional hasta la posterior implementación y CI dirigida en SHA exacto. No generar webhooks/event dispatcher, proveedor, migración, secreto ni pago real por esta matriz.

## 2. Aceptación funcional de un intento de pago (futura)

| ID | Dado / cuando | Entonces observable exigido | Evidencia real futura |
| --- | --- | --- | --- |
| AC01 | Actor con JWT vigente y permiso Facturación/Aplicar solicita link de factura del tenant A, con saldo y moneda compatibles | HTTP de éxito con identificador del intento y URL de proveedor opaca segura, asociado a A | Test HTTP end-to-end y assert sobre persistencia efímera |
| AC02 | Misma clave idempotente, proveedor y payload, mismo tenant | Replay devuelve mismo intento y no invoca doble cargo | Test de concurrencia, spy de proveedor y clave única durable |
| AC03 | Misma clave pero monto, moneda o factura distintos | Error de conflicto sin nuevo efecto | Test positivo/negativo y lectura de DB |
| AC04 | Factura sin saldo, importe superior al saldo o moneda distinta | Error de regla segura, sin crear un pago | Tests de validación Domain/Application y repositorio |
| AC05 | El proveedor retorna URL insegura, identificador inválido o falla | Rechazo o estado seguro sin revelar error/credencial | Tests de fallo y contrato URL HTTPS |
| AC06 | Usuario autorizado consulta o lista sus intentos | Solo intentos de tenant A paginados por estado/fecha | Tests de filtro con tenants A y B |
| AC07 | Proveedor envía evento firmado del tenant correcto con id nuevo | Ingreso validado y registro durable de evento, sin aplicar todavía un settlement automático | Tests firma HMAC y MySQL efímero |
| AC08 | Mismo evento/tenant/proveedor se reenvía con misma huella | Respuesta idempotente sin doble registro/efecto | Tests de replay e índice único |
| AC09 | El evento trae el mismo id externo con payload modificado | `409`/conflicto seguro sin nuevo efecto | Test de hash, error y readback DB |
| AC10 | La fuente financiera aprobada confirma pago **en futura etapa autorizada** | Estado conciliado consistente y auditado, sin doble asiento | CI de ledger, transacciones y conciliación con fixtures aprobados |

## 3. Aceptación de seguridad y aislamiento (futura)

| ID | Escenario negativo | Resultado requerido | Gate causal futuro |
| --- | --- | --- | --- |
| AC11 | Sin JWT / rol sin permiso / membresía revocada | Denegar todas las operaciones privilegiadas | Tests API de Authorization y revocación |
| AC12 | Credenciales de A intentan factura, pago o webhook de B | Denegar y no persistir datos ajenos | Tests A/B en API, Application y MySQL |
| AC13 | Firma ausente, inválida, timestamp obsoleto u origen no configurado | Rechazar antes de efectos | HMAC tests y revisión configuración secret |
| AC14 | Cuerpo webhook excesivo o entrada chunked sin Content-Length | Rechazo por límite; no bypass streaming | Tests del ingreso HTTP y límite de bytes |
| AC15 | Callback tardío/duplicado/reordenado o timeout externo | No conceder `Pagado`, entitlement ni refund sin reconciliación | Estado idempotente, transición negativa y auditoría |
| AC16 | Texto de error, telemetría y respuestas de API | No contienen PAN/CVV, token, firma ni secreto del proveedor | Escaneo de secretos, assertions sobre DTO/log |
| AC17 | Dos procesos/instancias simultáneas reciben la misma clave | Un único efecto persistente, no solo lock en memoria | Test multi-instancia, índice DB, rollback |
| AC18 | El actor posee URL opaca pero no JWT/permiso para gestión de suscripción | El enlace no concede lectura administrativa ni RBAC | E2E de límites de confianza |

## 4. Aceptación de integración y reversibilidad (futura)

| ID | Condición | Evidencia obligatoria antes de liberar |
| --- | --- | --- |
| AC19 | Reembolso, reverso o compensación, **solo tras aprobación explícita** | Permission distinta, razón, referencia anterior, ledger compensatorio, idempotencia y no borrado; tests de falla/reintento |
| AC20 | Cambio de persistencia / esquema o contrato de proveedor, **solo en scope autorizado** | Migración aditiva y rollback probados en MySQL efímero; verificación exact-SHA, cero secretos, compatibilidad de API/Angular y revisión tenant |

**Ejemplos de fixtures propuestos, no insertados:** A y B con empresas y permisos distintos; factura A pendiente 125.50 HNL y factura B de control; claves `idem-A-000000000001` y conflicto con diferente monto; evento externo X firmado para A; copia alterada y repetición del mismo evento; tres estados ordenados y dos callbacks desordenados. Los valores son únicamente escenarios ficticios, no datos reales ni política comercial aprobada.

## 5. Gates técnicos para etapas posteriores y controles de este documento

| Gate | Para esta M01.05 (documental) | Para futuro cambio ejecutable |
| --- | --- | --- |
| Identidad/gobernanza | GitHub Javier, skill canónica/arquitectura y Scope Lock STRICT; PLAN/COLA/DAG individual | Igual, revalidado en HEAD antes de cada commit |
| Código y seguridad | `NO_APLICA` a nuevas pruebas de runtime; inventario y criterios de no regresión | .NET 10 Release/tests, autorización A/B, HMAC, idempotencia |
| Datos | Ninguna migración, transacción ni Aiven live | EF Core 10/MySQL 8.4 en CI efímero con rollback/fixtures |
| Frontend | Sin modificación de Angular/UI | Angular 22.2.1, tests TypeScript/Playwright si se cambia UI |
| Proveedor | Puerto provider-neutral descrito, ningún proveedor activado | Aprobación comercial/seguridad, secreto server-side y tests sandbox |
| Publicación | Solo Markdown + CHANGELOG, commit FF, exact SHA y readbacks | Diff de código, pruebas causales exact-SHA completas, sin in-progress/skipped PASS |
| Independencia | A5 pendiente para otra Task Javier, distinta de autora | A5 separado del autor antes de QA/main/PROD |

Los resultados de CI de un SHA antiguo no autorizan cambios nuevos. Los `skipped/cancelled/failed/in_progress` de checks **requeridos** nunca son PASS. Un job general backend/frontend que corre durante un diff exclusivamente Markdown es **NO_APLICA** respecto de esta matriz si no toca runtime; jamás se presenta como PASS.

## 6. Puerta de cierre del lote y decisiones abiertas

El contrato de salida de `C04-04.M01.06` debe revisarse en su propia microtarea: estado LISTO y, cuando sea causalmente requerido, A5 independiente de M01.01–M01.05; no basta que el DAG esté VALIDADO. Las decisiones D01–D10 del documento M01.04 siguen pendientes; no se inventan valores comerciales para proveedores, monedas, vencimientos o devoluciones. La etapa M02 necesitará modelo, contratos y dependencias precisas; M03 necesitará código real y CI dirigida. La mera redacción de estos AC no permite desplegar ni cobrar.

## 7. Auditorías A0–A5 y reversibilidad

**A0:** fuente única, DAG y scopes preservados; arquitectura/seguridad no modificadas. **A1:** AC01–AC10 propuestos con resultado observable, sin atribuir ejecución. **A2:** AC11–AC18 de JWT/RBAC/tenant, firma y datos sensibles; positivos/negativos diseñados, no probados. **A3:** AC19–AC20 de integración y reversión, sin tocar MySQL/ledger ni proveedor. **A4:** una vez publicado, comprobar diff de dos Markdown, parent/HEAD fast-forward, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` sobre SHA de commit y PLAN/COLA/BITACORA readback; checks obligatorios pendientes bloquean LISTO. **A5_PENDIENTE:** otra de las cinco Tasks Javier, no autorrevisión; no QA/main/PROD.

Impacto presente: nuevo `docs/v5/c04-04-m01-05-criterios-aceptacion-cobros.md` y sección autocontenida de `CHANGELOG_AI.md`, sin tocar API, DB, JWT, frontend, datos productivos, Tasks, secretos, dispatcher o servicios de pago. Rollback: `git revert` normal, sin force-push y con BITACORA histórica intacta.
