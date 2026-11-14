# SOLQARYN V5 — C06-04.M01.02: actores, interfaces y fronteras del portal de cliente

**ID:** C06-04.M01.02, PLAN_MAESTRO fila 975. **Autor:** PRIMARY DEV 05 JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Rama:** solqaryn/Solqaryn `dev`, PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-14.

## Alcance y estado comprobado

El futuro C06-04 requiere portal por empresa para registro, recuperación, pedidos, reservas, membresías, documentos y preferencias. Esta unidad **documenta actores, entradas/salidas, superficies y controles de confianza**, sin programar rutas, APIs, identidad de cliente, MySQL, JWT, cookies, SMTP, enlaces temporales o UI. Su puerta `DAG_TECNICO!F/G=VALIDADO/SI` autoriza solo análisis M01.02. El documento M01.01 fue entregado y cerrado con doce invariantes NR01–NR12; su A5 sigue independiente y no se considera implícitamente aprobada.

**Línea base por blob inspeccionado**: `frontend/src/app/app.routes.ts` (`2f560c14693455b79ac4546e9333fb74586ef234`) tiene `tienda/cuenta` y `tienda/pedido/:id`, no demuestra portal integral; `core/auth/tenant-context.service.ts` (`63a711355495d2f80efba35bffb2b8727401154a`) requiere selección verificada por backend; `core/guards/auth.guard.ts` (`b652a7af6343bdae83a4dc9f3065d9fc1aa120f6`) y `permiso.guard.ts` (`1052bd38041055cc81e84a14d5fe0364e169b1fe`) protegen ERP; `features/storefront/storefront-pedido.service.ts` (`4e01a19f32dfe26f92fbbb1f860f5887f2b47176`) mantiene recibo efímero local, no historial oficial; `features/pedidos-venta/pedido-venta.service.ts` (`de7f7a5cfae97a027321c55c1907d088620becd4`) presenta API de ERP, no licencia de acceso público; `core/models/cliente.model.ts` (`5be42ba0d7707bd1b75d745d47feae896aeb4451`) contiene campos de contacto/identidad sensibles. `docs/v5/c06-04-m01-01-objetivo-no-alcance-portal-cliente.md` (blob `afa54d6939df46719cfe89a9f3b657b2bce38f03`) delimita NR01–NR12. Ninguna fuente acredita cuentas finales live, autorización HTTP de historial, reserva o descarga.

## A01–A12: actores y límites de autoridad FUTUROS

| ID | Actor | Entrada esperada | Salida o denegación |
| --- | --- | --- | --- |
| A01 | Visitante anónimo | Página pública y solicitud de alta | Datos públicos, sin lectura privada ni enumeración de cuentas |
| A02 | Titular de cuenta final | Credenciales verificadas y tenant asociado | Sesión limitada a su identidad, nunca privilegios ERP |
| A03 | Cliente invitado en checkout | Pedido efímero/recibo | Confirmación mínima sin equiparar sessionStorage con orden real |
| A04 | Cliente con múltiples empresas | Elección explícita y vinculaciones verificadas | Contexto único, caché y sesión separados por tenant |
| A05 | Solicitante de recuperación | Correo/teléfono sin divulgación de existencia | Respuesta indistinguible; token de un uso y expiración |
| A06 | Usuario de reservas | Reserva propia + estado/idempotency-key | Lectura/escritura propias; denegar reserva ajena y duplicado |
| A07 | Titular de membresía | Identidad y plan/estado vigente | Solo beneficios autorizados; nunca por rol declarativo |
| A08 | Receptor de documento | Recurso, permiso y propiedad verificados | Descarga limitada, URL opaca efímera; recurso ajeno denegado |
| A09 | Gestor de preferencias | Elección por finalidad y consentimiento | Registro/revocación auditable sin suscripción inferida |
| A10 | Operador ERP del tenant | JWT/RBAC ERP existente | Interfaz privada por permisos; no se transforma en cliente por ruta pública |
| A11 | Soporte de plataforma | Contexto privilegiado explícito y auditado | Acceso mínimo justificado, sin bypass invisible ni impersonación |
| A12 | Sistema de mensajería/auditoría | Evento validado, datos mínimos y cuota | Notificación sin secretos, PII ni activación de worker en M01 |

## I01–I12: contratos de frontera previstos, no API existente

| ID | Superficie | Input/validación servidor | Output y fallo seguro |
| --- | --- | --- | --- |
| I01 | Alta de cuenta cliente | Tenant permitido, contacto validado, antispam y prueba de posesión | Estado neutro, sin crear membresía ERP |
| I02 | Inicio/cierre de sesión | Identidad de consumidor y sesión rotada | Cookie/token acotado, invalidación; sin reutilizar JWT administrativo por inferencia |
| I03 | Recuperación | Solicitud genérica, rate-limit, token único y TTL | Misma respuesta exista o no usuario; revocar sesiones cuando aplique |
| I04 | Contexto de cliente/empresa | Relación cliente-tenant y recurso verificada | Identidad efectiva o deny-by-default; cabecera local no es autoridad |
| I05 | Perfil propio | Claim autorizado + propiedad real | Solo campos consentidos; actualizar con validación, auditoría y versión |
| I06 | Historial de pedidos | Identidad, tenant y ownership por registro | Paginación de pedidos propios; rechazo de ID ajeno |
| I07 | Reserva/consulta | Recurso propio, disponibilidad, idempotencia y estado | Resultado único, rollback transaccional al fallar |
| I08 | Membresías | Vigencia, tenant, identidad y condiciones aprobadas | Estado/beneficios reales o sin acceso; no pagos ficticios |
| I09 | Descarga documental | Autorizar relación con documento y tenant, TTL corto | Stream/enlace temporal no enumerable; rechazo cross-tenant |
| I10 | Preferencias y consentimientos | Finalidad, prueba de consentimiento y versión | Cambio/revocación auditable; no marketing implícito |
| I11 | Notificaciones | Plantilla, evento verificable y destinatario probado | Mensaje mínimo y cuota; no implementar dispatcher en M01 |
| I12 | Gestión/auditoría/soporte | JWT empresarial y permiso de soporte/tenant | Seguimiento limitado y auditado, sin PII en logs ni privilegio público |

## B01–B08: fronteras de confianza cerradas

- **B01 — Anónimo/privado:** no se devuelve perfil, historial, documento ni existencia de cuenta por ruta pública.
- **B02 — Consumidor/ERP:** identidad del portal no hereda `authGuard`, roles administrativos ni permisos sobre módulos internos.
- **B03 — Cliente A/cliente B en el mismo tenant:** la igualdad de empresa nunca autoriza leer registros ajenos.
- **B04 — Tenant A/tenant B:** cualquier cambio de contexto requiere revalidación servidor y eliminación de datos/cache del contexto anterior.
- **B05 — Recibo local/registro oficial:** sessionStorage no certifica cobro, reserva, membresía ni titularidad.
- **B06 — Link y token efímero/autoridad duradera:** recuperación y documentos requieren caducidad, invalidación, secreto fuera de logs y replay rechazado.
- **B07 — Consentimiento/marketing:** términos legales y compra no equivalen a opt-in; revocación no rompe operaciones obligatorias.
- **B08 — Portal SaaS/servicios externos:** SMTP, CDN, Cloudinary, DNS y proveedores de pago son integraciones con autorización y pruebas separadas; no se activan aquí.

## Oráculos de fase FUTUROS NO_EJECUTADOS

**P01–P06:** alta de cliente con verificación y tenant correcto; recuperación única sin enumeración; lectura de perfil e historial propios; reserva propia idempotente; documento propio autorizado/TTL; consentimiento granular revocable en móvil/teclado.

**N01–N12:** visitante sin sesión no lee privados; JWT cliente no abre ERP; cuenta A no consulta B; cliente X no accede pedido Y del mismo tenant; header de tenant adulterado no cambia contexto; recibo local manipulado no acredita orden; recuperación inválida/reutilizada no activa cuenta; reserva duplicada no duplica efecto; membresía vencida no concede acceso; URL documento adivinada/expirada no descarga; sin consentimiento no hay marketing; logs no filtran PII, token ni secreto. **No son pruebas runtime ejecutadas**.

## Impacto, A0–A5 y rollback

Archivos afectados por M01.02: exclusivamente este Markdown y sección nueva de `CHANGELOG_AI.md`. No se modifican código Angular/.NET, rutas, SQL/EF, datos reales, endpoints, DTO, políticas JWT/RBAC, observabilidad runtime, UI/UX, webhooks, eventos, SMTP ni secretos. Estos contratos son propuestas para las fases pertinentes; no cambian arquitectura canónica.

**A0:** lectura de M01.01, fuentes citadas, actores/interfaces 12+12 y ocho fronteras. **A1–A3:** comprobación documental de casos P/N, autoridad server-side, tenant/ownership y coherencia con línea base, sin atribuir tests live. **A4:** solo dos Markdown, fast-forward CAS, Scope Lock y V5 proveniencia SUCCESS de SHA exacto, readback PLAN/COLA/BITACORA. Suites .NET/Angular/MySQL no causales para este diff = NO_APLICA, jamás PASS. **A5:** otra Task Javier distinta del autor, pendiente. **Rollback:** revert Git normal, sin borrar historial; ninguna promoción QA/main/PROD.
