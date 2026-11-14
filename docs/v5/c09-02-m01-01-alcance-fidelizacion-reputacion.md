# SOLQARYN V5 — C09-02.M01.01: alcance y no-regresión de fidelización y reputación

**Microtarea:** C09-02.M01.01 · PLAN_MAESTRO fila 1334 · **Autor:** PRIMARY DEV 01 JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a` · **Fecha:** 2026-11-14 · `solqaryn/Solqaryn@dev` · `PROJECT_SCOPE_LOCK=STRICT`.

**Fuente ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **C09-02 Fidelización y reputación:** puntos, promociones, reseñas, respuestas y atención posventa, con antifraude, moderación y trazabilidad. **Alcance de M01.01:** fijar el objetivo, las exclusiones y los invariantes de no-regresión. Es un contrato **DOCUMENTAL DE PREPARACIÓN**, no una entrega de puntos, moderación, reviews o motor antifraude operativos.

## Evidencia estática original y brecha de implementación

| Archivo existente consultado en dev | Blob Git | Qué demuestra y qué NO demuestra |
| --- | --- | --- |
| `backend/src/Domain/Entities/Cliente.cs` | `8c6d4c4453e44f8c0be9475340465e1aadb3f787` | Cliente con `EmpresaId` nullable para legado; NO es ledger de puntos |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | Relación de usuario con empresa; NO equivale a consentimiento de campaña ni rol de moderador |
| `backend/src/Domain/Entities/TiendaCuentaCliente.cs` | `aa2bc1541afffd68aa980520178d2b6f0d5e7aad` | Cuenta pública, sesión, direcciones, favoritos; NO demuestra programa de puntos |
| `backend/src/Domain/Entities/Venta.cs` | `64ab3cd92e81031e74137f420cd748c564e28f01` | Documento comercial y relación Cliente; `EmpresaId` nullable NO autoriza acceso global |
| `backend/src/Domain/Entities/CreditoCliente.cs` | `651fb9f9530daded4a11611a7d58a1f1cc401183` | Crédito comercial, **no** saldo de puntos, cashback o wallet |
| `backend/src/Domain/Entities/RegistroAuditoria.cs` | `d669ac397a78ba02a5ba19e2cfb4bc9c7a115462` | Auditar acción, entidad, empresa y correlación; NO es historial financiero de fidelidad |

**Límite probatorio:** las seis rutas fueron comprobadas por blob en el árbol dev y se leyeron contenidos pertinentes; un archivo ausente de esta lista puede existir en otra carpeta. **No se certificó búsqueda exhaustiva de todas las clases ni se ejecutaron tests.** Las fuentes CRM C09-01 son dependencias documentales, no licencia para duplicar o compartir datos sin control. Los predecesores, el estado de cada ID y su autorización DAG se verifican en el Sheet institucional al ejecutar cada unidad.

## Objetivo funcional futuro (NO_IMPLEMENTADO)

Delimitar un programa opcional por empresa con reglas tipadas/versionadas de acumulación y canje; registro inmutable de movimientos de puntos; promociones limitadas y auditables; reseñas con moderación, apelación y respuestas; seguimiento posventa que reutilice la identidad de Cliente sin mezclar datos de otros tenants. Separar **saldo de puntos** de crédito comercial, saldo monetario, facturación, descuentos contables y plan de suscripción SaaS.

El diseño futuro debe distinguir tres actores: cliente final, operador empresarial con RBAC y soporte de plataforma con controles extraordinarios. Ninguna acción pública se eleva por slug, host, cookie, parámetro `EmpresaId` o cabecera `X-Empresa-Id`. Los límites de privacidad y retención requieren decisiones antes de crear APIs y persistencia.

## No-alcance actual

- No crear tablas, migraciones, eventos, workers, webhooks, caché productiva, APIs, SDKs de marketing ni UI operativa.
- No activar incentivos, puntos convertibles a dinero, wallets, créditos, cobros, pagos, envíos promocionales, notificaciones o proveedores externos.
- No ofrecer moderación automatizada con poder de censura irreversible ni publicar reseñas de clientes reales.
- No cambiar JWT/RBAC, configuración tenancy, permisos del frontend, datos vivos, precios, facturas, créditos, dominios, DNS, QA, main o PROD.
- No inferir que `Cliente.EmpresaId=NULL` o `Venta.EmpresaId=NULL` conceden acceso entre empresas o que `TiendaCuentaCliente` identifica inequívocamente una empresa.
- No interpretar esta preparación como A5_PASS, certificación de fraude, métricas de conversión, permisos de despliegue o cumplimiento legal.

## NR01–NR14 — invariantes de no regresión

| Invariante | Condición obligatoria de diseño e implementación futura |
| --- | --- |
| NR01 | Un cliente o registro de puntos de empresa A nunca es visible ni canjeable por empresa B |
| NR02 | El tenant real y el acceso se resuelven en backend con pertenencia y permisos vigentes |
| NR03 | `EmpresaId=NULL` legado permanece de ownership desconocido hasta reconciliación verificada |
| NR04 | Crear, conceder, debitar, reversar y expirar puntos exige transacción e idempotencia verificables |
| NR05 | El saldo de puntos no modifica `CreditoCliente`, `Venta` ni efectivo por inferencia |
| NR06 | Una promoción no omite límites, acumulación máxima, expiración ni exclusiones por versión |
| NR07 | Intentos concurrentes de canje no permiten saldo negativo, duplicado ni doble descuento |
| NR08 | Un rol lector, cliente final o sesión revocada no obtiene permisos de moderación/ajuste |
| NR09 | Reseñas y respuestas no exponen datos de contacto ni contenido privado de terceros |
| NR10 | Moderación deja motivo, actor, fecha, revisión y vía de restauración/apelación auditables |
| NR11 | Opt-out y falta de consentimiento bloquean campañas, sin bloquear la compra legítima |
| NR12 | Los fallos de caché, worker, reloj y proveedor externo no activan beneficios por defecto |
| NR13 | Las pantallas futuras son accesibles por teclado, 320px, foco visible y estados de error |
| NR14 | Desactivar el módulo preserva ventas, créditos, saldo histórico y trazabilidad sin efectos externos |

No se define aún si los puntos tienen valor económico; **esa es una decisión de negocio y seguridad pendiente**, nunca equivalencia automática a moneda o crédito.

## Casos positivos y negativos FUTUROS_NO_EJECUTADOS

| Caso | Positivo a demostrar en fase posterior | Negativo que debe fallar cerrado |
| --- | --- | --- |
| P01/N01 | Cliente A consulta saldo y movimientos sólo A | Sesión A pide puntos de cliente B |
| P02/N02 | Venta confirmada autorizada acumula una sola vez según regla vigente | Replay/carrera o venta anulada duplica puntos |
| P03/N03 | Canje autorizado actualiza saldo y referencia transaccional | Dos canjes concurrentes gastan el mismo saldo |
| P04/N04 | Promoción A válida tiene vigencia, uso y límites tipados | Promoción de B, expirada, desactivada o de código arbitrario se aplica |
| P05/N05 | Reseña A se crea con política de identidad y privacidad | Reseña forjada de B o PII no permitida se expone |
| P06/N06 | Moderador autorizado registra decisión reversible con motivo | Rol lector o cliente intenta ocultar, borrar o alterar reseñas |
| P07/N07 | Solicitud posventa legítima se asocia al Cliente/Venta A | Actor A accede a ticket, venta o cuenta B |
| P08/N08 | UI accesible informa estado de incentivo sin conceder privilegios | Bypass de guard Angular no elude RBAC de API |

**Clasificación:** P01–P08/N01–N08 son especificaciones **FUTURAS_NO_EJECUTADAS**, no PASS de CI, pruebas ejecutadas ni detección de fallos reales.

## Interfaces y dependencias que deben estudiarse antes de M02–M03

- **Domain:** Cliente, Venta, CréditoCliente, TiendaCuentaCliente y RegistroAuditoria tienen significados diferentes; nuevas reglas de puntos/campañas/reseñas no deben apropiarse de sus campos o alterar historial.
- **Application/API:** contratos para consulta, concesión, canje, promociones, moderación y soporte separados por permisos, empresa y sede, con validación server-side.
- **Infrastructure/MySQL 8.4:** evaluar ledgers, índices, claves de idempotencia por tenant, transacciones y pruebas concurrentes en una fase autorizada; no migrar ahora.
- **Frontend Angular 22.2.1:** sólo presentación del estado y acciones habilitadas; el backend decide autoridad real. Accesibilidad, lightweight UI y pruebas Playwright cuando se implemente.
- **Seguridad:** JWT, RBAC, aislamiento A/B, PII, consentimiento, fraude por replay, abuso de promociones, revocación y trazas saneadas.
- **Observabilidad/rollback:** auditoría por actor, motivo, versión y correlación, reversión de beneficios sin borrar movimientos; no desplegar worker ni eventos en este alcance.

## Gates A0–A5 y reversión documental

- **A0:** Task J00/owner del ID 6adbb9fff1548191a1bd5b30d9e9984a, Skill local y canónica, `PROJECT_SCOPE_LOCK=STRICT`, HEAD actual, PLAN H/I, COLA G/O, DAG F/G y claim CAS con readback.
- **A1:** comparación positiva/negativa de alcance, exclusiones, NR01–NR14 y P01–P08/N01–N08 con las seis fuentes reales. No declarar que un requisito está implementado.
- **A2:** análisis de JWT/RBAC, tenancy A/B, privacidad, saldo/pagos, replay, promociones, consentimiento y fail-closed; no se prueba seguridad runtime.
- **A3:** mapa de capas y puntos de integración futuros; xUnit, MySQL, Angular y Playwright `NO_APLICA` para el diff exclusivamente Markdown, sin contarlos como PASS.
- **A4:** publicar dos Markdown en `dev` por fast-forward/CAS, revisar blob/diff y comprobar en SHA exacto `verify-solqaryn-project-scope` y check V5 de CHANGELOG `completed/success`; readback PLAN/COLA/BITACORA.
- **A5:** `A5_PENDIENTE`, revisión independiente por una Task Javier distinta del autor; A5 no autoriza QA/main/PROD.
- **Rollback:** git revert normal del changeset documental, sin force-push, alteración de BITACORA histórica ni modificaciones de datos.

**Siguiente trabajo:** M01.02 identifica actores/superficies y M01.03 lee contratos y código; M01.04/M01.05 documentan riesgos y criterios observables. M01.06/M02–M06 conservan gates técnicos individuales, decisiones de negocio y pruebas reales proporcionales. No se habilitó fidelización ni reputación live.
