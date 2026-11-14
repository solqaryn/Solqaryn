# SOLQARYN V5 FINAL — C02-01.M02.05: eventos, idempotencia y correlación del contexto tenant

**Clase:** especificación M02.05 estrictamente DOCUMENTAL. **Autor:** PRIMARY DEV 06 — JAVIER_CHATGPT, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Repositorio:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, PLAN_MAESTRO/COLA/DAG_TECNICO del Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`.

## 1. Alcance exacto — sin implementar eventos

Esta unidad establece **criterios futuros** para trazabilidad, idempotencia, correlation IDs, límites de cuota y mensajes sobre cambios autorizados del contexto multitenant. **No implementa un sistema de eventos, dispatcher, webhooks, bus, outbox, workers, trigger entre Tasks, API, middleware, caché, tabla, migración ni cambio de configuración de JWT/RBAC**. La decisión y verificación de tenant permanece en el backend, no en un evento, header, cookie, host, slug o cliente.

Los predecesores documentales M02.01 (entidades/ownership), M02.02 (API/DTO y errores), M02.03 (persistencia/fixtures) y M02.04 (claves tipadas y flags) se utilizan como referencias contractuales, **no** como evidencia de que haya host-routing universal, persistencia futura o cambio de contexto transaccional ya desplegados. Esta especificación no autoriza M02.06, M03, migración, nuevas identidades, QA/main/PROD o cuentas/servicios de pago.

## 2. Identidad y autoridad antes de cualquier mensaje

1. El backend autentica al actor y verifica usuario activo, membresía vigente, rol de `UsuarioEmpresa` para la empresa exacta, empresa activa, ownership de recurso/sucursal y permiso RBAC. `VerifiedCommercialTenant` es una marca de autoridad de servidor; ningún input cliente la crea.
2. Host/slug/forwarded host pueden ser una **selección solicitada**, jamás una acreditación. Resolución futura exige binding único aprobado, proxy confiable y nueva comprobación de membresía. Host inexistente/ambiguo o callback que llega tarde se rechaza; no se reutiliza contexto A después de seleccionar B.
3. Eventualmente un evento sólo puede describir **un cambio validado y confirmado**, nunca activar permisos, derechos SaaS, feature flags sensibles, sesiones o cambios de tenant. Un consumidor no transforma un mensaje en autorización; debe revalidar acceso al dato en su propio backend.
4. La revocación de membresía, rol, empresa, sesión o permisos obliga a invalidar caché/resultado; una idempotency key previamente aceptada no autoriza un replay desde identidad inválida.
5. Un usuario A no observa resultados, correlaciones privadas, errores enriquecidos o datos de la empresa B. Ausencia de información = **fail-closed**, no fallback al tenant por defecto.

## 3. Sobre de evento conceptual (no producido ni enviado)

| Campo propuesto | Requisito antes de cualquier implementación | Rechazo |
| --- | --- | --- |
| `eventId` | Opaque, único, generado por servidor después de commit | Duplicado/cliente elige ID autoritativo |
| `eventType` | Enum versionado restringido, p.ej. `TenantContextChanged.v1` solo si existe transición autorizada | Ejecución arbitraria, tipo libre, comando por mensaje |
| `schemaVersion` | Versión explícita con consumidor de compatibilidad limitada | Versión desconocida interpretada de forma permisiva |
| `occurredAtUtc` | Tiempo servidor del commit, no navegador | Fecha navegador como prueba de orden |
| `tenantRef` | Identificador mínimo interno del tenant autorizado por servidor, según canal privado y retención | `EmpresaId` de URL convertido en ownership |
| `actorRef` | Referencia mínima seudónima de actor autorizado; no token/cookie | JWT, correo, claims completos, PII |
| `scopeKind` / `resourceRef` | Ámbito y recurso propio, verificados en servidor, sin enumeración externa | Recurso B atribuido a tenant A |
| `membershipRevision` | Versión opaca de membresía/política si existe, nunca `RoleId` del navegador | Caché sin comprobar revocación |
| `correlationId` | Identificador opaco con longitud/caracteres acotados; generar seguro ante input inválido | CRLF, traversal o valores con secretos |
| `idempotencyDigest` | Huella de servidor ligada a actor+tenant+operación+revisión+request canónico | Compartir clave entre tenants o mostrar token original |
| `outcomeCode` | Código seguro y allowlisted de resultado confirmado, sin mensajes privados | Fuga de host, recurso, estado B o stack trace |

Este sobre **no contiene** PII, tokens, password hashes, credenciales, connection strings, host secret, correo, cookies, contenidos privados ni lista de empresas accesibles. Incluso eventos internos futuros requieren clasificación, autorización por consumidor, cuota, retención y minimización. No publicar estos eventos en `BITACORA` de microtareas como si fueran ejecución real.

## 4. Idempotencia, transacciones, reintentos y exclusión

La identidad propuesta de deduplicación es la combinación `(actorServidor, empresaServidor, operacion, recursoVerificado, idempotencyKey)` más una huella canónica **versionada** que incluya revisión esperada y payload permitido. El servidor debe verificar permisos y contexto *en cada intento*, incluidos reintentos que intenten recuperar un resultado cacheado. Una clave opaca enviada por cliente no es autenticación ni permiso.

| Situación futura | Resultado seguro |
| --- | --- |
| Primera transición autorizada con `expectedRevision` vigente | Actualizar dentro de transacción autorizada, registrar resultado y sólo entonces eventual señal |
| Replay idéntico por mismo actor/tenant todavía autorizado | Misma respuesta segura, sin segunda mutación ni nuevo `eventId` |
| Misma clave, payload/actor/tenant diferentes | Conflicto o namespace distinto; nunca retornar datos del tenant previo |
| Timeout antes del commit | No afirmar éxito; reintento consulta resultado transaccional |
| Crash tras commit, antes de entrega | Outbox duradero *si futuro diseño autoriza*, consumidor idempotente; no prometer exactly-once |
| Membresía, usuario o rol revocado durante replay | Rechazo sin servir resultado almacenado |
| Cambio A→B con respuesta vieja A en vuelo | Descartar mensaje/caché A en frontend y revalidar estado B en backend |
| Consumidor con mensaje duplicado o versión desconocida | No aplicar efecto doble ni degradar restricciones |

En cualquier implementación, `CAS` sobre estado/revisión e idempotency record deben compartir una frontera atómica demostrable con integración MySQL efímera y pruebas de concurrencia. Este Markdown **no crea CAS real**; no altera `UsuarioEmpresa`, `Empresas`, `VerifiedCommercialTenant` ni DB existente. Sin transacción/outbox real no hay emisión asíncrona autorizada.

## 5. Correlación, seguridad y límites de cuota

- Correlacionar sólo referencias opacas de `requestId`, evento/operación, tenant de servidor y revisión auditada; no registrar payload, JWT ni identidad/host B en trazas accesibles a A.
- Limitar longitud y caracteres de identifiers, tamaño de envelope, tasa por actor+empresa, reintentos, concurrent requests, TTL de idempotencia, retención de auditoría y número de consumidores. **Los valores numéricos no se inventan**: requerir threat model, carga medida, revisión de privacidad y aprobación posterior.
- No tratar `featureFlag=true`, entitlement activo, header `X-Tenant` o host válido como sustituto de sesión, pertenencia, propiedad del recurso ni RBAC vigente. Eventos no confieren permisos a subscribers ni habilitan funcionalidades.
- Cualquier fallo de autorización, firma, lookup, disponibilidad, cuota o resolución duplicada debe producir rechazo sin cambio de tenant ni resultado; las respuestas externas omiten enumeración de empresas o detalles internos.
- En frontend Angular, una selección visual de tenant requiere descartar respuestas asíncronas del contexto anterior; esto se valida en scope UI posterior y **no** queda implementado aquí.

## 6. Matriz de oráculos — documental, NO pruebas ejecutadas

| Caso | Tipo | Resultado exigido para una implementación futura |
| --- | --- | --- |
| P01 | POS | A autorizado, membresía A vigente y revisión válida: operación A auditada; evento sólo después de commit |
| P02 | POS | Mismo actor A, misma operación/huella y sesión vigente: replay no duplica efecto |
| P03 | POS | Miembro A/B cambia a B autorizado: contexto y correlación de B revalidados, jamás reutiliza permiso A |
| P04 | POS | Consumidor interno recibe duplicado post-commit: deduplica por ID/versión sin segundo efecto |
| N01 | NEG | Actor exclusivo A presenta host/header/slug B: denegar, sin datos B |
| N02 | NEG | JWT expirado/alterado o usuario bloqueado con idempotency previa: denegar |
| N03 | NEG | Membresía revocada o rol sin permiso con cache vigente: denegar y no reproducir resultado |
| N04 | NEG | Clave idempotente con payload diferente: conflicto sin mutación |
| N05 | NEG | Versión obsoleta o owner de recurso distinto: conflicto/denegación, sin evento |
| N06 | NEG | Proxy no confiable, wildcard host o mapping ambiguo: fallo cerrado |
| N07 | NEG | CorrelationId con CRLF, muy largo o PII/secretos: rechazar/regenerar y redactar logs |
| N08 | NEG | Cliente envía rol, tenant, plan o flag elevado: ignorar como permiso y denegar |
| N09 | NEG | Crash/timeout antes de commit, o outbox inexistente: no reportar evento/éxito |
| N10 | NEG | Mensaje duplicado, schema desconocido o respuesta tardía A tras B: no exponer ni duplicar efectos |

La matriz anterior es una **inspección de expectativas**, no xUnit, HTTP, MySQL, Playwright ni JWT ejecutados. La etapa de implementación real debe demostrar al menos positivos/negativos A/B, autenticación, revocación, host falso, autorización de recursos, persistencia/rollback y reintentos en CI de SHA exacto.

## 7. Criterios de cierre documental y reversión

**Preflight:** `C02-01.M01.01–06` LISTO y A5 de Javier distinto del autor; `PLAN_MAESTRO!H300=PENDIENTE/I300` vacío, `COLA!G300/O300=SI` y `DAG_TECNICO` exacto `F=VALIDADO/G=SI` comprobados antes de reclamar mediante CAS. Los documentos M02.01–04 contextualizan; ninguno abre permisos de DB, auth o eventos.

**A0:** diff Markdown delimitado, fuente/CHANGELOG y secretos. **A1:** verificar simbólicamente P01–P04 y N01–N10, no tests runtime. **A2:** límites de confianza, aislamiento A/B, revocación y no elevación RBAC/tenancy en la especificación. **A3:** consistencia de los cuatro contratos M02 previos, DAG, auditoría y plan; sin integración ejecutable. **A4:** publicación fast-forward a dev, Scope Lock y check V5 de procedencia CHANGELOG `completed/success` del SHA exacto, readback PLAN/COLA/BITACORA. Compilaciones/servicios no relevantes para Markdown: `NO_APLICA` motivado, jamás contarlas PASS ni prometerlas. **A5:** revisión independiente únicamente por una de otras cinco Tasks Javier, sin autorrevisión.

Rollback de este **documento**: revert normal del commit con readback nuevo; BITACORA histórica inmutable. No cambiar QA/main/PROD, DNS, datos productivos, proveedores, logs reales ni crear Tasks o servicios.
