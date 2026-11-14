# SOLQARYN V5 — C06-04.M01.05: criterios de aceptación del portal de cliente

**Microtarea:** C06-04.M01.05, PLAN_MAESTRO fila 978. **Autor:** PRIMARY DEV 02 JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Fecha:** 2026-11-14. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Repositorio:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. **Naturaleza:** contrato documental M01, no nuevas capacidades implementadas.

## Línea base y alcance de aceptación

El código de `StorefrontCuentaService` y `StorefrontCuentaComponent` ya presenta cuenta de tienda, registro/login, direcciones, favoritos, pedidos y notificaciones: **no se declara ausente ni se reimplementa**. Los escenarios aquí son oráculos **FUTUROS NO_EJECUTADOS** para certificar integralmente tenancy/ownership y ampliar recuperación, reservas, membresías, documentos y preferencias sólo cuando sus contratos e implementaciones específicos sean autorizados. Un recibo de `sessionStorage` no acredita una venta oficial ni un derecho persistente.

Fuentes de fase contrastadas por blob Git:
- `docs/v5/c06-04-m01-01-objetivo-no-alcance-portal-cliente.md` — `afa54d6939df46719cfe89a9f3b657b2bce38f03` (NR01–NR12).
- `docs/v5/c06-04-m01-02-actores-interfaces-portal-cliente.md` — `d0fc169736e28f93128b10b7c15f32406e1eb0a1` (A01–A12, I01–I12, B01–B08).
- `docs/v5/c06-04-m01-03-inventario-fuentes-portal-cliente.md` — `163402f99029b7371c5ed898562467267e6c1a7a` (S01–S16 y G01–G12).
- `docs/v5/c06-04-m01-04-riesgos-supuestos-decisiones-portal-cliente.md` — `19508bcb6b20eaad6043d100d872c6e105775903` (R01–R12, S01–S08, D01–D08 pendientes).
- `frontend/src/app/features/storefront/storefront-cuenta.service.ts` — `34e245d5db4c534fb2d942356c697f47d2224969`; `frontend/src/app/features/storefront/storefront-cuenta.component.ts` — `80335b33243854606f563312bf976e7b93493f39`; `frontend/src/app/features/storefront/storefront-pedido.service.ts` — `4e01a19f32dfe26f92fbbb1f860f5887f2b47176`.

La evidencia citada es una revisión acotada de fuentes, **no** un resultado de pruebas API, MySQL, browser, correo o seguridad live. Los parámetros HTTP, Host, slug, cookie, JWT presentado o `EmpresaId` del cliente nunca son autorización por sí mismos: tenant, miembro, propietario de objeto y permiso se comprueban en backend.

## Aceptación positiva — casos FUTUROS NO_EJECUTADOS

| ID | Precondición/estimulo | Oráculo observable esperado | Prueba futura |
| --- | --- | --- | --- |
| P01 | Alta de cuenta tienda verificada para cliente A de tenant A | Identidad y vínculo servidor con A; nunca rol ERP | API/DB efímera + A/B |
| P02 | Login de cliente A, sesión vigente y revocación | Cookie/token de consumidor acotado y cierre invalidado; sin JWT ERP implícito | API/HTTP |
| P03 | Recarga con sesión de tienda válida | Estado propio de A restaurado sólo tras verificar autoridad backend | API + Playwright |
| P04 | Cliente A edita dirección propia | Sólo dirección de A persiste; versión y auditoría sin afectar otros clientes | API/DB |
| P05 | Cliente A añade y consulta favorito propio | Listas aisladas cliente/tenant y sin duplicados involuntarios | API/DB |
| P06 | Cliente A consulta pedido de su historial existente | Servidor comprueba owner de pedido y tenant antes de devolverlo | API/DB |
| P07 | Checkout retorna referencia/recibo local | La UI distingue recibo temporal y orden confirmada server-side | API + browser |
| P08 | Recuperación verificada de cuenta (función futura) | Token temporal de un uso, rotación y respuesta no enumerativa | API + clock fake |
| P09 | Reserva propia (función futura) | Idempotency-key y control transaccional garantizan un único efecto | API/DB efímera |
| P10 | Membresía válida (función futura) | Beneficio limitado a tenant, owner, plan y vigencia aprobados | API/DB |
| P11 | Descarga de documento propio (función futura) | Autorización por objeto y enlace opaco TTL sin indexación | API/storage simulado |
| P12 | Preferencias/consentimiento (función futura) | Opt-in por finalidad, revocación efectiva, versión auditable | API + UI |
| P13 | Cliente autorizado cambia contexto entre tenants asociados | Revalidación servidor e invalidación de caché del tenant anterior | API A/B + browser |
| P14 | Pantalla móvil a 320px y navegación por teclado | Estados de carga/error recuperables, foco, labels accesibles, sin bloqueo de lectura | Playwright a11y |

## Aceptación negativa — casos FUTUROS NO_EJECUTADOS

| ID | Entrada adversarial o conflicto | Denegación/resultado exigido |
| --- | --- | --- |
| N01 | Anónimo consulta perfil/pedido privado | 401/403/404 neutro; sin datos personales |
| N02 | Cliente cambia cabecera EmpresaId, tenant_id o slug a B | Sin autorización basada en parámetro; ningún dato de B |
| N03 | Cliente X pide documento/pedido de Y en mismo tenant | Denegación por ownership de objeto |
| N04 | Sesión storefront solicita admin ERP o scopes de operador | Denegado, sin elevar JWT/RBAC |
| N05 | Token de recuperación expirado, repetido o adulterado | Rechazo, no creación de nueva sesión |
| N06 | Recuperar un correo existente versus inexistente | Respuesta indistinguible y rate-limit |
| N07 | Manipular recibo en sessionStorage para marcar pago | No crea ni confirma una orden oficial |
| N08 | Reintentar la misma reserva en paralelo | Sólo un efecto, el resto respuesta idempotente/conflicto |
| N09 | Membresía de otro tenant o vencida | Sin beneficio ni acceso a documento |
| N10 | URL de factura adivinada, ajena o caducada | Sin descarga, sin PII ni token en respuesta |
| N11 | Sin consentimiento de marketing o después de revocación | No comunicar promociones, sólo operación estrictamente necesaria |
| N12 | Cambio de tenant o logout con caché vieja | Sin acceso a datos, sesiones ni referencias del contexto anterior |
| N13 | Fallo 500 o validación inválida con correo/identidad/JWT | Mensaje neutro; logs sin secretos ni PII |
| N14 | Ráfaga de alta/login/recuperación | Rate limiting y control antiabuso sin enumeración |

## Evidencia causal y política de aprobación futura

- **Seguridad API (.NET 10):** para P01–P13 y N01–N14, documentar comando reproducible, actor, tenant A/B, recurso X/Y, resultado HTTP, estado servidor antes/después y prueba negativa; autorización relacional, revocación, sesión separada de ERP y defensa ante IDOR. No aceptar un guard Angular como control backend.
- **MySQL/EF con servicio efímero:** aislar pedidos/addresses/reservas/membresías/consentimientos y concurrencia; rollback y conteos antes/después, sin consultar ni alterar datos productivos.
- **Angular 22.2.1 / Playwright:** validar cuenta tienda existente, estados, 320px, teclado, foco, errores neutros, logout y pérdida de contexto; no confundir esos tests con el portal completo.
- **Integraciones futuras:** recuperar cuentas y enviar correo sólo con SMTP aprobado, límites, expiración; documentos con URL firmada/TTL y propiedad; cuotas de mensajes, auditoría mínima y secretos fuera del repositorio; no activar webhooks, eventos ni Tasks.
- **Trazabilidad:** P01/P02/P08 cubren NR01, NR04, B01/B02, I01–I04; P04–P07 y N02/N03/N07 cubren NR02/03/06, B03/B05, I05/06; P09/P10 y N08/N09 cubren NR07 e I07/08; P11/N10 cubren NR08 e I09; P12/N11 cubren NR09 e I10; P13/N12 cubren NR10/B04; P14/N13/N14 cubren NR05/11/12 y límites de observabilidad.
- **Decisiones pendientes:** D01–D08 de M01.04 permanecen **PENDIENTES**, sin inferir contrato de identidad de cliente, retención legal, esquema, proveedores, roles o autorización de despliegue. Toda dependencia M02/M03, IAM, cierre M01.06 y QA/main/PROD mantiene gates propios.

**Gate futuro:** sólo acreditar cada caso cuando existan implementación y tests positivos/negativos con artefactos y SHA exacto. `queued`, `in_progress`, `skipped`, `cancelled` y `failed` nunca equivalen a PASS de una prueba requerida. Un resultado documental M01 no concede A5 automáticamente.

## Impacto de esta microtarea, control A0–A5 y reversión

**Cambios de esta entrega:** exclusivamente este Markdown en `docs/v5/` y una sección íntegra de `CHANGELOG_AI.md`. Ninguna modificación de Domain/Application/Infrastructure/API, DTO, rutas Angular, SQL/EF, JWT, RBAC, tenant, PII, secretos, infraestructura, workers, correo, eventos o datos de producción; ningún cambio arquitectónico. Observabilidad runtime permanece intacta.

**A0:** coherencia de NR/I/B, riesgos y 14P/14N, ausencia de permisos nuevos y procedencia. **A1:** revisión de casos positivos versus estado conocido, sin atribuirles ejecución. **A2:** revisión negativa de aislamiento inter-tenant e intra-tenant, antienumeración, sesiones y secretos, sin test de seguridad live. **A3:** trazabilidad entre documentos y código inspeccionado, ejecución runtime = NO_APLICA a este changeset documental. **A4:** exigir fast-forward CAS, diff sólo dos Markdown, Scope Lock + procedencia CHANGELOG `completed/success` exact SHA, PLAN/COLA/BITACORA readback. **A5:** sólo Task Javier distinta de la autora, posterior a A0–A4, nunca autoaprobación.

**Rollback:** `git revert <SHA_PUBLICADO>` normal y auditoría aditiva, sin force-push ni modificación retrospectiva de BITACORA. Ninguna promoción QA/main/PROD ni implementación por aprobar este documento.
