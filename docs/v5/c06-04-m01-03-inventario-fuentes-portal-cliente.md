# SOLQARYN V5 — C06-04.M01.03: inventario causal de código y contratos del portal de cliente

**Unidad:** C06-04.M01.03, PLAN_MAESTRO fila 976. **Autor:** PRIMARY DEV 05 JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Rama exclusiva:** solqaryn/Solqaryn `dev`, PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-14.

## Qué existe y qué falta acreditar

C06-04 pretende integrar registro/recuperación, pedidos, reservas, membresías, documentos y preferencias en un portal de cliente multiempresa con aislamiento verificable. **Hallazgo importante del inventario:** la aplicación **ya incluye una experiencia de cuenta de tienda**, y no debe tratarse como funcionalidad inexistente ni reconstruirse sin analizarla: `StorefrontCuentaService` implementa llamadas cliente a `/tienda/cuenta/registrar`, `/login`, restauración, logout, direcciones, favoritos, pedidos y notificaciones, y `StorefrontCuentaComponent` presenta las pantallas y sus estados. El código leído **no certifica** las reglas backend de aislamiento por tenant y cliente, recuperación, reservas, membresías, documentos, ni suites de seguridad extremo a extremo. La diferencia es entre **interfaz y contratos existentes** y **cobertura integral C06-04 auditada**, no entre presencia y ausencia de cualquier cuenta.

Esta microtarea lee y clasifica evidencia, **no modifica autenticación, UI, rutas, API, base de datos, contratos de cliente ni almacenamiento**. Ningún token de sesión, correo, número de cliente o petición enviada desde navegador constituye por sí mismo prueba de propiedad de recursos.

## S01–S16 — fuentes y hashes del árbol `dev` contrastados

| ID | Fuente | Blob Git | Evidencia acotada / límite |
| --- | --- | --- | --- |
| S01 | `docs/v5/c06-04-m01-01-objetivo-no-alcance-portal-cliente.md` | `afa54d6939df46719cfe89a9f3b657b2bce38f03` | Doce invariantes NR01–NR12 y límites documentales, no pruebas runtime |
| S02 | `docs/v5/c06-04-m01-02-actores-interfaces-portal-cliente.md` | `d0fc169736e28f93128b10b7c15f32406e1eb0a1` | A01–A12, I01–I12, B01–B08 y escenarios de aceptación FUTUROS |
| S03 | `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | `tienda/cuenta` y `tienda/pedido/:id` públicas, ERP guardado; no prueba integral de autorización |
| S04 | `frontend/src/app/features/storefront/storefront-cuenta.service.ts` | `34e245d5db4c534fb2d942356c697f47d2224969` | API cliente de alta/login, direcciones, favoritos, pedidos, notificaciones; `X-Storefront-Session` local, falta verificar backend/aislamiento |
| S05 | `frontend/src/app/features/storefront/storefront-cuenta.component.ts` | `80335b33243854606f563312bf976e7b93493f39` | UI de registro/login, direcciones, favoritos, pedidos/notificaciones; no certificar E2E ni accesibilidad sólo por código |
| S06 | `frontend/src/app/features/storefront/storefront-pedido.service.ts` | `4e01a19f32dfe26f92fbbb1f860f5887f2b47176` | Recibo temporal en `sessionStorage`; no es confirmación contable persistente |
| S07 | `frontend/src/app/features/storefront/storefront-pedido.component.ts` | `8fe447cf0d7a62047e1a6ee0bd6a578ad2530e96` | Renderiza recibo local por referencia; no valida ownership en backend por sí solo |
| S08 | `frontend/src/app/features/storefront/storefront.models.ts` | `de45fc1e9c0e3d1b98bd560b6e8006200d3a2b08` | Tipos de tienda y cuenta cliente, no autorización server-side |
| S09 | `frontend/src/app/core/auth/auth.service.ts` | `9c9b58c4b289ccc3269b8e99b614a2a9dbaae557` | JWT/sesión del ERP, separados conceptualmente de sesión storefront |
| S10 | `frontend/src/app/core/auth/tenant-context.service.ts` | `63a711355495d2f80efba35bffb2b8727401154a` | Empresa del ERP validada en backend; localStorage sólo intención, no autorización |
| S11 | `frontend/src/app/core/guards/auth.guard.ts` | `b652a7af6343bdae83a4dc9f3065d9fc1aa120f6` | ERP requiere auth y revalidación tenant, no delega permiso a cuenta cliente |
| S12 | `frontend/src/app/core/guards/permiso.guard.ts` | `1052bd38041055cc81e84a14d5fe0364e169b1fe` | RBAC de rutas privadas; no certifica permiso de portal externo |
| S13 | `frontend/src/app/core/models/cliente.model.ts` | `5be42ba0d7707bd1b75d745d47feae896aeb4451` | Modelo de cliente ERP con datos de contacto/identidad potencialmente sensibles |
| S14 | `frontend/src/app/features/pedidos-venta/pedido-venta.service.ts` | `de7f7a5cfae97a027321c55c1907d088620becd4` | Servicios de pedido administrativo con idempotency-key de creación; no exponen por sí mismos historial personal seguro |
| S15 | `frontend/src/app/features/pedidos-venta/pedido-venta.model.ts` | `57f4437372c734824a40be8e9731c0e75c0f18ba` | Estado/cliente/snapshots y detalle de pedido; vinculación a cuenta portal no acreditada |
| S16 | `frontend/src/app/core/models/perfil.model.ts` | `9b0fcb337ccae8d5d5819e0b9111237df430dc86` | Perfil y cambio password ERP; no equivale a recuperación de cliente tienda |

Los hashes identifican blobs efectivamente leídos o cotejados en el repositorio, **no** certificación exhaustiva de controladores ASP.NET, queries EF/MySQL, endpoints HTTP ejecutados, accesibilidad real ni pruebas A/B. Un archivo con `/registrar` como URL es evidencia de **cliente API**, no de funcionamiento de esa API. Al implementar las etapas posteriores se requerirá inventario backend y pruebas dirigidas de respuestas HTTP reales.

## G01–G12 — brechas y verificaciones pendientes (no incidentes probados)

| ID | Brecha exacta | Evidencia requerida antes de certificar M03–M05 |
| --- | --- | --- |
| G01 | Autenticación storefront y ERP mantienen tokens/autoridad distintos | Tests de sesión cruzada, logout, expiración, revocación y deny-by-default |
| G02 | Tenant y ownership de cuenta final se resuelven en backend | Tests usuario A/B y clientes X/Y del mismo tenant; no confiar en request ID |
| G03 | Registro/login storefront existe como interfaz, falta cobertura negativa causal aquí | API con duplicados, fuerza bruta, enumeración, throttling y fallos seguros |
| G04 | Recuperación cuenta final no queda certificada por las fuentes | Token un uso/TTL, expiración, reintentos, respuesta uniforme, revocación |
| G05 | Pedidos storefront y recibos locales no equivalen a ERP oficial | Integración servidor de historial propio y rechazo de pedido ajeno |
| G06 | Direcciones/favoritos necesitan ownership y borrado comprobado | Pruebas clientes cruzados y tenant A/B en GET/POST/DELETE |
| G07 | Reservas e idempotencia portal no se acreditan en esta selección | Test de doble comando, estados, rollback transaccional, disponibilidad |
| G08 | Membresías y vigencias no están certificadas por esta muestra | Casos de plan vencido, revocación, tenant erróneo y prohibición de beneficio |
| G09 | Documentos y URL firmadas requieren política de acceso por recurso | Descargar propio/ajeno, TTL, URL adivinada, audit trail |
| G10 | Preferencias/consentimiento y notificaciones requieren privacidad comprobada | Opt-in granular, opt-out, retención, envío duplicado, no PII en logs |
| G11 | Accesibilidad y experiencia responsive no se acreditan sólo con TS | Playwright teclado, foco, 320 px, errores sin enumeración |
| G12 | Integraciones externas y migraciones requieren gate distinto | No ejecutar SMTP/Cloudinary, bases reales, workers, eventos o producción por doc |

El servicio storefront utiliza `sessionStorage` para sesión del navegador y una cabecera `X-Storefront-Session`; esta es descripción de implementación frontend, no juicio concluyente de seguridad del servidor. La existencia de funciones actuales **no se presume fallida**, y la ausencia de un módulo en los archivos inspeccionados **no prueba ausencia global**.

## Oráculos FUTUROS NO_EJECUTADOS

**P01–P06:** P01 login storefront real y sesión acotada; P02 cuenta propia muestra direcciones/favoritos y pedidos propios; P03 recuperación por token de un uso; P04 reserva idempotente propia; P05 descarga documental autorizada; P06 preferencias revocables y accesibilidad de pantallas.

**N01–N14:** N01 JWT ERP no se confunde con sesión cliente; N02 cliente A no lee B; N03 cliente X no lee pedidos de Y del mismo tenant; N04 token expirado/revocado denegado; N05 contraseña y cuenta no enumerables; N06 token recuperación reutilizado falla; N07 recibo manipulado no confirma pedido/pago; N08 dirección ajena no se edita; N09 reserva duplicada no duplica efectos; N10 membresía expirada no autoriza; N11 URL documento ajeno no descarga; N12 consentimiento de marketing no es implícito; N13 datos de tenant previo no quedan en caché tras cambiar; N14 logs/errores no contienen JWT, session token ni PII. **Ninguno se ejecutó en esta microtarea.**

## A0–A5, alcance de publicación y rollback

**Impacto de esta entrega:** solo este documento y nueva sección autocontenida de `CHANGELOG_AI.md`. Cero cambio en UI Angular, TS/JS de servicio, .NET, MySQL, EF, endpoints, JWT, roles, guards, API, DNS, secrets, Cloudinary, SMTP, datos o integraciones. **A0:** inspección de fuentes y limitación explícita de inferencias. **A1–A3 documentales:** consistencia de S01–S16/G01–G12, escenarios P/N futuros y preservación de aislamiento tenant/cliente; pruebas runtime NO_APLICA a cambio Markdown. **A4:** publicación fast-forward CAS, diff solo 2 Markdown, Scope Lock y V5 provenance exact SHA, BITACORA + PLAN/COLA readback. **A5:** pendiente de otra de las cinco Tasks Javier, no revisión propia. **Rollback:** revert normal con nuevo commit preservando BITACORA inmutable. Nunca QA/main/PROD ni activación de webhooks/dispatcher.
