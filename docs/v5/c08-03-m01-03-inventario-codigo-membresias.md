# SOLQARYN V5 — C08-03.M01.03: inventario de contratos, código y evidencia de membresías

**Autor:** PRIMARY DEV 02 / JAVIER_CHATGPT / TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Plan:** fila 1228, C08-03 Membresías y acceso, M01. **Fuente ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Rama:** `solqaryn/Solqaryn@dev`, STRICT. **Fecha:** 2026-11-14.

## Propósito y método

Leer contratos/código realmente presentes y evidencias de fase documental, sin equiparar la membresía empresarial del SaaS con la membresía comercial de un gimnasio. Se leyeron las 12 rutas siguientes en el HEAD de dev, con blobs Git verificados; además se inspeccionaron las carpetas de Entities, Application/Services, API/Controllers y Angular/features. El inventario es una muestra causal delimitada, no una prueba exhaustiva de ausencia de módulos en todo el repositorio.

## Inventario de 12 fuentes existentes

| # | Artefacto | Ruta exacta | Blob Git SHA-1 | Hallazgo y límite |
| --- | --- | --- | --- | --- |
| 1 | M01.01 alcance | `docs/v5/c08-03-m01-01-alcance-membresias-acceso.md` | `79bcd2aa2dc4c2f5cb8abdf243334d09ed6d347b` | NR01-NR14, no certifica acceso físico |
| 2 | M01.02 interfaces | `docs/v5/c08-03-m01-02-actores-interfaces-membresias.md` | `6bfc9fd5f36a232f1d37be64cf00d9fb23de78d2` | 14 interfaces FUTURAS |
| 3 | Cliente dominio | `backend/src/Domain/Entities/Cliente.cs` | `8c6d4c4453e44f8c0be9475340465e1aadb3f787` | EmpresaId opcional; ownership histórico no certificado |
| 4 | UsuarioEmpresa dominio | `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | membresía de EMPRESA, no de gimnasio |
| 5 | Suscripcion SaaS dominio | `backend/src/Domain/Entities/SuscripcionesSaaS.cs` | `d9b5dbe19fb728293b00b748f10ac10336debd2d` | plan SaaS empresarial, no plan de gimnasio |
| 6 | ClienteService | `backend/src/Application/Services/ClienteService.cs` | `51661879903e96064557f1a1b4bd3f2c1c79a77b` | creación resuelve VerifiedCommercialTenant |
| 7 | SuscripcionesSaaSService | `backend/src/Application/Services/SuscripcionesSaaSService.cs` | `bb501285f11cd4b32709a58a2c51efa19c3664ea` | empresa efectiva server-side |
| 8 | ClientesController | `backend/src/API/Controllers/ClientesController.cs` | `023d639f5c6ebf20519a22b175e2bf17c53e6d0d` | Authorize y RequierePermiso; no check-in |
| 9 | SuscripcionesSaaSController | `backend/src/API/Controllers/SuscripcionesSaaSController.cs` | `cab95db5a33c6a15448e2437481eaaac25ffc4fe` | ruta SaaS, no torniquete |
| 10 | Angular routing | `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | rutas inspeccionadas sin gimnasio |
| 11 | .NET 10 API | `backend/src/API/Solqaryn.API.csproj` | `2a7b33a0974d1ebe13676593d84b458b1243e037` | net10.0 |
| 12 | EF/MySQL | `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj` | `6210d8b12abd77171d95d17e2292c1a319b89263` | EF 10.0.12 y MySql.EntityFrameworkCore 10.0.9 |

## Hechos observados y límites de inferencia

- **H01:** `Cliente.EmpresaId` es nullable para registros históricos; no se puede certificar pertenencia completa ni reutilizar `Cliente.Id` como credencial de acceso.
- **H02:** `UsuarioEmpresa` representa pertenencia/rol empresarial, no suscripción de gimnasio; un abono de gimnasio no autoriza el ERP.
- **H03:** `Suscripcion` y `SuscripcionesSaaSService` gestionan planes SaaS de una Empresa. Su validación del tenant no constituye contrato de cobro, cuota ni check-in de gimnasio.
- **H04:** `ClienteService` deriva EmpresaId de `VerifiedCommercialTenant.Resolve`; el futuro servicio de gimnasio debe validar identidad, permiso y tenant A/B en servidor.
- **H05:** `ClientesController` contiene permisos sobre clientes, mientras `SuscripcionesSaaSController` expone acciones SaaS. Los archivos inspeccionados no implementan acceso físico de gimnasio. Tampoco se observó ruta específica en `app.routes.ts`; esto **no demuestra ausencia global** de cualquier módulo.
- **H06:** API net10.0 y EF Core/MySQL están declarados, pero ninguna migración ni base de datos productiva fue consultada. Los blobs son evidencia de texto, no de ejecución ni estado real de datos.

## Brechas y decisiones pendientes

| Código | Pregunta o dependencia no acreditada | Gate futuro |
| --- | --- | --- |
| G01 | ¿Cuál es la fuente canónica de identidad de miembro comercial? | Ownership y tenant server-side probados |
| G02 | ¿Cómo se modelan plan, cuota, vigencia y derecho sin reutilizar Suscripcion SaaS? | Contrato de dominio y autorización separados |
| G03 | ¿Cómo se interpretan UTC/IANA, DST y borde de expiración? | Pruebas de tiempo deterministas |
| G04 | ¿Qué sucursal, recurso y operación puede usar cada actor? | RBAC/membresía y sede validados |
| G05 | ¿Cómo bloquea de inmediato una suspensión o revocación? | Prueba sin acceso residual por cache |
| G06 | ¿Cómo se evitan replay y doble check-in concurrente? | Consistencia transaccional e idempotencia |
| G07 | ¿Qué auditoría mínima evita PII, secretos o token durable en logs? | Redacción y correlación verificadas |
| G08 | ¿Cuál es la prueba de pago realmente autorizada? | Sin cobros ni activación por UI |
| G09 | ¿Cómo se maneja lector desconectado? | Denegar por defecto; sin hardware habilitado |
| G10 | ¿Qué tests A/B, .NET, MySQL y Playwright certificarán la implementación? | CI pertinente del SHA exacto |

## Oráculos FUTUROS, NO_EJECUTADOS

**Positivos P01-P06:** P01 titular de A consulta sólo A; P02 recepción autorizada verifica sede/plan; P03 vencimiento UTC/IANA exacto; P04 suspensión revoca acceso; P05 reintento idempotente deja un único check-in; P06 interfaz responsive y accesible informa decisión sin otorgar permisos.

**Negativos N01-N12:** N01 selector de tenant B desde A; N02 EmpresaId falsificado; N03 Cliente sin ownership; N04 operador lectura intenta activar; N05 JWT revocado; N06 plan suspendido; N07 reloj del navegador alterado; N08 DST ambiguo; N09 QR replay; N10 check-in concurrente duplicado; N11 lector offline sin confirmación; N12 webhook/pago sin prueba autorizada. En todos, el comportamiento futuro exigido es denegación segura y auditoría minimizada. **No se ejecutaron tests** de ninguno.

## Auditorías proporcionadas y rollback

**A0:** contrastar fuentes de la tabla, Skill canónica, HEAD, COLA G/O=SI y DAG F=VALIDADO/G=SI, diff sólo dos Markdown. **A1:** revisar inventario, delimitaciones y pruebas positivas/negativas como especificación documental, no PASS de runtime. **A2:** verificar que ningún texto autorice JWT/RBAC/tenant A/B, PII, secreto, dispositivo u operación de pago por inferencia. **A3:** mapear Domain/Application/API/Angular y dependencias reales sin editar código. **A4:** publicación fast-forward CAS, blobs exactos, Scope Lock y check ligero de CHANGELOG `completed/success` para el SHA publicado y readback institucional. **A5:** revisión independiente por otra Task Javier, no autoaprobación.

**Rollback:** git revert normal del SHA publicado, con bitácora aditiva y readback, nunca force-push. Este documento NO añade endpoints, entidades, migraciones, roles, tablas, eventos, webhooks, workers, pagos, dispositivos, acceso físico ni permisos de QA/main/PROD. No libera automáticamente M01.06/M02. Los oráculos listados requieren pruebas reales sólo cuando una futura implementación esté autorizada.
