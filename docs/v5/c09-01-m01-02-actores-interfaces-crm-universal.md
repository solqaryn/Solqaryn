# SOLQARYN V5 — C09-01.M01.02: actores, interfaces y fronteras del CRM universal

**Unidad:** C09-01.M01.02 · PLAN_MAESTRO fila 1299 · **Autor:** PRIMARY DEV 05 JAVIER, TASK_ID 6adc2e55f47c81919f6374e561687076 · **Fecha:** 2026-11-14 · solqaryn/Solqaryn@dev · PROJECT_SCOPE_LOCK=STRICT.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA. Esta M01.02 inventaría actores, entradas, salidas y superficies futuras del CRM universal. El bloque C09-01 exige historial, segmentos, oportunidades, actividades y seguimiento de clientes con timeline aislado y permisos por rol. **El presente documento no implementa ni certifica CRM o endpoints ejecutables.**

## Evidencia fuente observada en dev

| Fuente | Blob verificado | Lectura limitada |
| --- | --- | --- |
| docs/v5/c09-01-m01-01-alcance-crm-universal.md | 4d35d8113cf1a1718dd7f487c50a06f51c1848b5 | NR01–NR14 y oráculos futuros, no implementación |
| backend/src/Domain/Entities/Cliente.cs | 8c6d4c4453e44f8c0be9475340465e1aadb3f787 | Cliente con EmpresaId nullable legacy, Nombre, Correo y otros PII |
| backend/src/Domain/Entities/Empresa.cs | c44940df7c30983a4c23b93338e0cb9367962069 | Empresa raíz canónica, sin CRM probado |
| backend/src/Domain/Entities/UsuarioEmpresa.cs | f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c | Membresía empresarial y rol, no consentimiento de campañas |
| frontend/src/app/core/guards/permiso.guard.ts | 1052bd38041055cc81e84a14d5fe0364e169b1fe | Guarda UI, no autoridad server-side |
| frontend/src/app/core/interceptors/auth.interceptor.ts | 4bdf19135ed1932a99fd09ace1f900bcccc5b25b | X-Empresa-Id solicitado no concede permiso |

El inventario es deliberadamente parcial: la futura M01.03 auditará controladores, queries EF y suites reales. Ninguna existencia de clase se interpreta como endpoint CRM ni como test de aislamiento A/B completado.

## A01–A14: actores y superficies FUTURAS

| Actor | Entrada futura | Salida esperada y límite |
| --- | --- | --- |
| A01 Admin Empresa | Configuración CRM tipada, roles | Reglas de su empresa, auditadas |
| A02 Vendedor | Cliente, seguimiento, oportunidad | Cartera autorizada y mínima |
| A03 Supervisor ventas | Asignación y estados | Cartera de su ámbito, no privilegio global |
| A04 Operador sucursal | Cliente e interacción | Sólo sede y empresa verificadas |
| A05 Cliente titular | Solicitud de consulta o rectificación | Datos propios por canal validado |
| A06 Admin plataforma | Catálogo de capabilities CRM | Control gobernado sin acceso indiscriminado a PII |
| A07 Soporte autorizado | Motivo de diagnóstico | Traza saneada sin tokens ni PII ajena |
| A08 Backend API | JWT, recurso y operación | 401/403/404 seguro con tenant resuelto |
| A09 Application | Comando/consulta tipados | Autorización y deduplicación previa |
| A10 Domain | Cliente, Empresa, UsuarioEmpresa | Reglas de propiedad e invariantes |
| A11 Infrastructure | Query, transacción, filtro | Persistencia por tenant y auditoría |
| A12 Angular | Proyección permitida y estado | UI accesible sin bypass de API |
| A13 Fuente de ventas | Referencia comercial legítima | Vínculo sin duplicar facturas/pedidos |
| A14 Auditoría/CI | Fixtures A/B y SHA | Evidencia reproducible sin datos productivos |

Los actores son del **contrato futuro**, no identidades, permisos, módulos o servicios nuevos ya disponibles.

## I01–I12: interfaces de diseño, no API publicadas

| Interfaz | Entrada futura | Salida y autoridad obligatoria |
| --- | --- | --- |
| I01 Resolver tenant | Token, empresa solicitada, recurso | Empresa activa + UsuarioEmpresa validado en servidor |
| I02 Leer ficha | ClienteId, campos allowlisted | Perfil mínimo sólo del tenant y permiso |
| I03 Timeline | ClienteId, paginación | Historial cronológico A, sin registros de B |
| I04 Registrar actividad | DTO, versión, idempotency key | Única anotación auditada o conflicto seguro |
| I05 Gestionar oportunidad | Cliente, responsable, estado | Estado autorizado, sin crear cobro implícito |
| I06 Segmentos | Filtro tipado y tenant | Resultado paginado; no SQL/JS arbitrario |
| I07 Asignar cartera | Usuario, cliente, sucursal | Asignación por pertenencia activa |
| I08 Búsqueda | Texto normalizado, límites | Resultado mínimo, rate-limit y scope A |
| I09 Exportación | Alcance, motivo, permisos | Export auditado, retención y datos minimizados |
| I10 Angular vista | Capacidades y permisos derivados | Estado accesible; backend conserva autoridad |
| I11 Comunicación | Consentimiento, canal permitido | Solicitud futura; sin envío/worker activo aquí |
| I12 Rollback | Versión, actor y justificación | Reversión idempotente sin alterar tenant B |

Los DTO finales, rutas HTTP, índices y migraciones sólo se deciden en M02/M03 con tests y permisos.

## F01–F09: fronteras de confianza críticas

- **F01 UI → API:** ruta, slug, cabecera, cookie y EmpresaId del cliente no autorizan.
- **F02 API → Application:** JWT, membresía empresarial, permisos por recurso y revocación se verifican.
- **F03 Application → Domain:** Cliente y Empresa no se duplican; reglas no dependen de la UI.
- **F04 Application → Infrastructure/MySQL:** consultas y mutaciones filtran tenant, legacy NULL no es global.
- **F05 API → respuesta:** minimización de PII y errores sin fuga de tenant B.
- **F06 CRM → ventas/reservas:** enlaces referenciales sin repetir transacciones comerciales.
- **F07 CRM → mensajería:** preferencias y consentimiento verificables antes de cualquier envío futuro; sin activación aquí.
- **F08 Caché/búsqueda/exportación:** fallo, dato obsoleto o timeout debe denegar sin reutilizar caché de otro tenant.
- **F09 CI/auditoría:** fixtures sintéticos A/B, logs sin secretos, checks del SHA causal.

## Positivos y negativos FUTUROS_NO_EJECUTADOS

| Par | Positivo futuro | Negativo que deberá rechazarse |
| --- | --- | --- |
| P01/N01 | Vendedor A autorizado ve Cliente A | ID B conocido no revela nombre, correo ni teléfono |
| P02/N02 | Actividad A aparece una sola vez en su timeline | Usuario B/rol lector no crea ni lee actividad A |
| P03/N03 | Segmento allowlisted A contiene sólo clientes A | SQL/JS arbitrario o conteo de B se rechaza |
| P04/N04 | Oportunidad relaciona cliente y sede A válidos | Empresa B, sede ajena o cuenta revocada no opera |
| P05/N05 | Reintento idempotente reproduce el mismo resultado | Replay/concurrencia no duplican actividad |
| P06/N06 | Export autorizado de A queda auditado | Rol sin permiso o header falsificado no exporta B |
| P07/N07 | Preferencia/opt-out futuro es respetado | Sin consentimiento no se envían campañas |
| P08/N08 | UI con teclado/foco y 320px funciona con errores claros | Manipular guard UI no elude RBAC de API |

**Negativos adicionales N09–N12:** N09 token expirado/revocado; N10 EmpresaId NULL sin reconciliar; N11 error de búsqueda/caché no revela información; N12 proveedor caído no abre permisos ni repite envíos. Ningún caso aquí es prueba real .NET, Angular, MySQL o Playwright.

## Impacto, A0–A5 y rollback

**Impacto:** dos Markdown, sin cambios en Domain/Application/Infrastructure/API/Angular, JWT/RBAC, DB, trabajos, PII, correo, proveedores, seguridad operacional, main/QA/PROD. **A0:** Skill y arquitectura canónica, Scope Lock exact-SHA, PLAN H/I, COLA G/O, DAG F VALIDADO/G SI, owner exclusivo por CAS. **A1:** coherencia de 14 actores, 12 interfaces, nueve fronteras y escenarios futuros con M01.01. **A2:** amenazas A/B, legacy NULL, revocación, PII, consentimiento, replay y fail-closed analizadas documentalmente. **A3:** trazabilidad Domain ← Application ← Infrastructure/API/Angular; runtime NO_APLICA para diff únicamente Markdown, nunca PASS ficticio. **A4:** diff/blobs, publicación FF/CAS y checks Scope Lock/procedencia V5 completed/success en SHA exacto más readback PLAN/COLA/BITACORA. **A5:** PENDIENTE de otra Task Javier distinta, sin autorrevisión.

M01.03 deberá ampliar evidencia real; M01.04/05 documentan riesgos y aceptación; M01.06–M06 requieren gates propios. **Rollback:** git revert normal, sin force-push, borrar historia ni tocar datos reales. No se activaron webhooks, dispatcher, correo, WhatsApp, secretos, migraciones, QA/main/PROD.
