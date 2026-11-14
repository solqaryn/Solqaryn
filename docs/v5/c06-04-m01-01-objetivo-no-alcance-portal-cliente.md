# SOLQARYN V5 — C06-04.M01.01: objetivo, exclusiones y no-regresión del portal de cliente

**ID:** C06-04.M01.01, PLAN_MAESTRO fila 974. **Autora operativa:** PRIMARY DEV 05 JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Proyecto:** solqaryn/Solqaryn `dev`; PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-14.

## Objetivo verificable y frontera de esta unidad

C06-04 pretende un **portal de cliente por empresa**, con futura gestión de registro/recuperación, pedidos, reservas, membresías, documentos y preferencias. El resultado exigido de fase incluye **flujos públicos y privados y pruebas negativas de aislamiento entre empresas y clientes**. Esta M01.01 fija límites y criterios de no-regresión; **NO implementa portal, alta de cuentas, recuperación, JWT nuevo, API, almacenamiento, UI, correo ni migraciones**. Tampoco confirma que las capacidades futuras ya estén disponibles. La puerta DAG `VALIDADO/SI` autoriza solamente esta preparación documental.

Un cliente final no equivale a usuario ERP, administrador de tenant o suscriptor SaaS. Un pedido de tienda o registro de contacto tampoco acredita membresía ni titularidad de documentos. Las relaciones entre cliente, identidad, cuenta empresarial, sucursal, pedido y reservas se definirán y probarán en sus propias microtareas; nunca se inferirán desde un ID presentado por navegador.

## Línea base limitada contrastada con el HEAD de dev

| Fuente observada | Blob Git verificado | Límite de lo que demuestra |
| --- | --- | --- |
| `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | Existen rutas públicas `tienda/*`, `tienda/cuenta`, `tienda/pedido/:id` y privadas bajo guards; ninguna de ellas demuestra portal integral C06-04 |
| `frontend/src/app/core/auth/tenant-context.service.ts` | `63a711355495d2f80efba35bffb2b8727401154a` | La empresa solicitada en frontend no es autoridad; el backend verifica membresía de usuario ERP |
| `frontend/src/app/core/guards/auth.guard.ts` | `b652a7af6343bdae83a4dc9f3065d9fc1aa120f6` | El shell ERP exige contexto tenant de servidor y revalida tras recarga; no autoriza un portal de consumidor |
| `frontend/src/app/core/guards/permiso.guard.ts` | `1052bd38041055cc81e84a14d5fe0364e169b1fe` | Rutas de ERP usan permisos runtime; no deben exponerse como si fueran cuentas públicas |
| `frontend/src/app/features/storefront/storefront-pedido.service.ts` | `4e01a19f32dfe26f92fbbb1f860f5887f2b47176` | Guarda recibo público temporal en sessionStorage con expiración, no es fuente de verdad persistente de pedidos del cliente |
| `frontend/src/app/features/pedidos-venta/pedido-venta.service.ts` | `de7f7a5cfae97a027321c55c1907d088620becd4` | Tiene consultas/acciones ERP de pedido, no API pública autorizada de historial personal |
| `frontend/src/app/core/models/cliente.model.ts` | `5be42ba0d7707bd1b75d745d47feae896aeb4451` | Modelo de cliente ERP con teléfono/correo/identidad potencialmente sensible; no prueba consentimiento para identidad de portal |
| `docs/v5/c06-02-m01-01-objetivo-no-alcance-landing-tenant.md` | `d749c986ad03bf20bded35a3afc87d2b05d4e9fd` | Define seguridad de host/slug y contenido público; no crea ni autoriza sesión de cliente final |

Esta es una **revisión focalizada de ocho fuentes**, no una certificación exhaustiva del backend, ni de HTTP live, SMTP, bases de datos, cookies, reservas, pagos o identidad de cliente.

## NR01–NR12: invariantes de no-regresión obligatorios

| ID | Invariante futura | Fallo observable que bloquea |
| --- | --- | --- |
| NR01 | Identidad de cliente final separada de usuario ERP y control-plane | Un cliente recibe permisos `dashboard` o rol de operador por usar `tienda/cuenta` |
| NR02 | Resolución tenant validada en backend, nunca en URL, Host o storage del cliente | Cliente B lee un pedido, perfil o documento de A con cambio de identificador |
| NR03 | Autorización por **propiedad de recurso**, además de autenticación | Usuario autenticado consulta pedido de otro cliente del mismo tenant |
| NR04 | Registro y recuperación con rate limit, token efímero de un solo uso y respuesta no enumerativa | Se revela si existe el correo, se reutiliza token o se abre cuenta sin verificación |
| NR05 | Datos PII mínimos, consentimiento verificable y logs sin secretos | Identidad/RTN, teléfono, enlace de recuperación o JWT aparece en logs o respuesta pública |
| NR06 | Pedidos e historial provienen de autoridad server-side y ownership probado | Un recibo de sessionStorage se considera pago, pedido confirmado o historial oficial |
| NR07 | Reservas y membresías requieren estado, caducidad e idempotencia propias | Doble solicitud crea reserva duplicada o da acceso por membresía vencida |
| NR08 | Documentos se entregan por autorización explícita y referencia indirecta | URL adivinable o enlace de otro cliente permite descargar factura/adjunto |
| NR09 | Preferencias y consentimientos son versionados, revocables y por finalidad | Una preferencia de marketing se infiere de aceptar términos de servicio |
| NR10 | Cambio de empresa/cliente invalida cachés, sesiones y referencias anteriores | Tras cambiar tenant se conservan pedidos, avatar o documento de la empresa anterior |
| NR11 | Errores y estados UI son accesibles, seguros y sin fuga de existencia | Diferencia de mensaje revela cuenta ajena o error expone payload privado |
| NR12 | Ninguna preparación documental publica portal, DNS/TLS, workers, datos o secretos | Se activa ruta, correo, proveedor o endpoint por interpretar DAG como permiso de despliegue |

## Superficies futuras separadas y dependencias

- **Públicas futuras:** presentación neutra, intención de registro, solicitud de recuperación y ayuda. Un formulario público no accede a objetos privados.
- **Privadas futuras:** perfil propio, historial de pedidos/recibos acreditados, reservas propias, membresías vigentes, descargas de documentos con autorización, gestión de consentimiento y preferencias. Cada una requiere API y policy server-side, sin reutilizar guards Angular como prueba de seguridad.
- **Administración empresarial:** ERP existente y sus permisos permanecen separados; ninguna credencial de consumidor concede rol de operador.
- **Infraestructura futura:** SMTP, cookies, storage, URL firmadas, idempotencia y eventos necesitan decisiones, tests y autorización por etapa. No se ejecuta dispatcher ni integración nueva en M01.
- **DAG:** se registran como contexto preparatorio las unidades documentales M01.01–05 de C01-01/02/03/04, C02-01/02/03/04 y C05-01/02/03/04. La fórmula individual de `DAG_TECNICO` manda; ninguna dependencia implícita libera IAM, M01.06, migraciones, runtime o QA.

## Oráculos de aceptación futuros, NO_EJECUTADOS

**Positivos P01–P06:** P01 registro verificado y vinculado solo al tenant correcto; P02 recuperación de cuenta con token único y expiración; P03 perfil e historial consultables solo por su titular; P04 reserva propia con estado idempotente; P05 descarga de documento propio con autorización y TTL; P06 gestión de preferencias revocables y UI usable a 320 px/teclado.

**Negativos N01–N12:** N01 tenant A/B cruzado rechazado; N02 pedido de otro cliente del mismo tenant rechazado; N03 ID arbitrario no amplía permisos; N04 cuenta inexistente no se enumera por recuperación; N05 token vencido/reutilizado rechazado; N06 usuario público no ingresa ERP; N07 recibo local falso no acredita pago; N08 reserva duplicada no genera efecto doble; N09 membresía vencida no autoriza; N10 URL adivinable no descarga documento; N11 cambiar tenant limpia contexto/cache privada; N12 error/log no expone correo sensible, JWT ni claves.

Estos casos son **especificaciones de pruebas futuras**, no resultados de xUnit, Playwright, API, MySQL ni de un entorno real.

## Análisis de impacto, verificaciones y rollback

**Impacto real de M01.01:** solo archivo nuevo de `docs/v5/` y sección autocontenida en `CHANGELOG_AI.md`; cero cambios en Domain/Application/Infrastructure/API, rutas Angular, interfaces, DTO, DB, migraciones, JWT/RBAC, jobs, observabilidad, secretos, proveedores y entornos. Las notas de seguridad no conceden derechos ni implementan funciones.

**A0:** coherencia con Plan V5, DAG individual, ocho fuentes y 12 invariantes; confirmar que no se mezclan roles ERP/cliente. **A1–A3 documentales:** cubrir escenarios positivos/negativos futuros, aislamiento A/B, ownership del mismo tenant, recuperación, PII y no-regresión; runtime=NO_APLICA para este diff Markdown. **A4:** diff limitado, publicación fast-forward sin force, Scope Lock y control V5 de encabezados sobre SHA exacto, readback PLAN/COLA/BITACORA. **A5:** PENDIENTE para otra Task Javier independiente; jamás autoaprobar. **Rollback:** `git revert` normal del changeset y nuevas evidencias aditivas; BITACORA histórica inmutable. No QA/main/PROD.
