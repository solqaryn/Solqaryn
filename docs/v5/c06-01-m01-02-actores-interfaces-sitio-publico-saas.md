# SOLQARYN V5 — C06-01.M01.02: actores, entradas, salidas y fronteras del sitio público SaaS

**Microtarea:** `C06-01.M01.02`, PLAN_MAESTRO fila 867. **Autor:** PRIMARY DEV 02 JAVIER, Task `6af16bb9492c8191b8e8c7d606f11507`. **Fecha:** 2026-11-14. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, documento Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; repositorio `solqaryn/Solqaryn@dev`, Scope Lock STRICT.

## Alcance: interfaces previstas, NO implementadas

Esta preparación clasifica actores, entradas/salidas futuras y fronteras de confianza del **sitio público SaaS**: propuesta de valor, planes, sectores, demo, contacto, legal y SEO. Es continuación documental de `C06-01.M01.01` (commit `a8bdca8d8dacf229a12fc8c41b781b99861e560a`, NR01–NR12). No crea rutas, diseño activo, formularios, campañas, SMTP, cobros, contenido legal aprobado, indexación, datos ni servicios externos.

Hechos de código consultados: `frontend/src/app/app.routes.ts` (blob `2f560c14693455b79ac4546e9333fb74586ef234`), donde `''` redirige a `login`, `tienda/*` identifica storefront y rutas ERP usan guardias; `frontend/src/app/app.config.ts` (blob `cd77384e713014015d83b348955b3d83573348b3`) compone router, interceptor y preload; `frontend/angular.json` (blob `8b00f8fee80fdfafe1864e233be7d5f20ad444e3`) declara build Angular y presupuestos, no tamaños medidos; `frontend/src/app/shared/feedback-state/feedback-state.component.ts` (blob `7deb40475f0c492d09e649e7351b2915bd53d356`) contiene estados ARIA, pero no prueba su uso en un formulario de contacto; `docs/v5/c05-01-m01-01-objetivo-no-alcance-design-tokens.md` (blob `ea7c5e7d339838f021b3f746fd9a7fe307b58b8d`) especifica futuras comprobaciones visuales. No se afirma haber observado un portal marketing ni sitemap real, ni se ha medido SEO, Lighthouse o HTTP.

## Doce actores y sus límites A01–A12

| Actor | Entrada/propósito futuro | Salida y frontera obligatoria |
| --- | --- | --- |
| A01 visitante anónimo | Abrir landing, sectores y descripción de planes | Contenido exclusivamente público, sin token ni datos de empresas |
| A02 cliente potencial | Consultar planes o solicitar demo | Información aprobada, sin creación automática de cuenta/tenant |
| A03 interesado en contacto | Enviar consulta mínima consentida | Validación/rate-limit server-side; acuse sin revelar infraestructura |
| A04 administrador comercial aprobado | Gestionar catálogo público futuro | Solo cambios versionados y autorizados; no acceso implícito al ERP |
| A05 redactor de contenidos legales | Aportar términos y políticas sujetos a revisión | Publicación únicamente con versión y aprobación jurídica |
| A06 robot de búsqueda legítimo | Leer robots/sitemap y metadatos públicos | Nunca enumerar rutas privadas, tokens, datos o parámetros sensibles |
| A07 navegador Angular | Render de ruta pública responsiva y accesible | No sustituye permisos backend y no guarda datos cross-tenant |
| A08 backend Application/API | Validar solicitudes de contacto/demo bajo permisos | DTO mínimo, saneamiento, cuotas, contexto seguro y auditoría |
| A09 servicio de correo vigente | Notificación potencial de contacto autorizada | SMTP solo tras habilitación propia, anti-spam, sin secreto en Git |
| A10 responsable de privacidad | Aprobar consentimiento, retención y tracking | No instalar pixels, cookies o terceros por inferencia documental |
| A11 equipo de seguridad/QA | Probar controles A/B, datos ficticios, abuso y rutas | Evidencia causal separada para gates; no QA/main/PROD automático |
| A12 propietario comercial/operación | Aprobar precios, promesas, dominios y lanzamiento | Una decisión humana no equivale a despliegue sin checks y rollback |

Ninguno de los actores adquiere permisos de otro por visitar una ruta; authGuard, permisoGuard y autorización backend siguen vigentes. Usuarios de empresa A no pueden obtener recursos de empresa B por un slug, cabecera, parámetro o caché del sitio público.

## Doce interfaces futuras y entradas/salidas I01–I12

| ID | Entrada prevista | Salida/rechazo verificable, aún FUTURO |
| --- | --- | --- |
| I01 Landing pública | Ruta, locale, metadata pública aprobados | Propuesta comprobable, canonical correcto, 404 seguro |
| I02 Catálogo de planes | Versión autorizada de prestaciones/precios | Comparación fiel; NO_GO ante precio inexistente o no aprobado |
| I03 Sector/beneficio | Copy e imágenes allowlisted, sin datos reales | Contenido acotado; sin atribuir testimonios ficticios a clientes |
| I04 CTA de demo | Solicitud mínima y consentimiento explícito | Resultado sin crear tenant real, ni cargar datos empresariales |
| I05 Acceso demo futura | Fixture efímero, identidad/TTL declarados | Entorno aislado A/B, sin backend productivo |
| I06 Contacto | Nombre/correo/mensaje bajo límites y validación | Acuse accesible, rate-limit, redacción de logs y antiabuso |
| I07 Recurso legal | Versión, fecha, aprobación y jurisdicción | Documento solo si aprobado; no servir texto placeholder como política |
| I08 SEO title/description | Texto controlado por ruta/entorno | Escape seguro, canonical allowlisted, sin inyección |
| I09 Sitemap/robots | Rutas efectivamente publicadas y autorizadas | Exclusión de dashboard/cuenta/empresa, URL privada y token |
| I10 Navegación principal | Menú, teclado, ancho, cambio de idioma si aplica | Foco, ARIA, 320px, reduced-motion y fallback de error |
| I11 Observabilidad pública | Correlación opaca sin token ni PII | Métricas limitadas aprobadas; no tracking externo implícito |
| I12 Release/reversión | SHA, diff, checks, configuración y aprobaciones | Rollback ensayado; sin salto a main/PROD por DAG |

Estas interfaces no son endpoints implementados, ni flujos SMTP reales, ni un contrato de precios confirmado. Para datos personales de contacto la política de privacidad y retención debe verificarse antes de capturarlos. Las páginas públicas jamás deben conceder acceso a recursos privados por rutas/client-side checks.

## Fronteras de confianza F01–F08

**F01 visitante→Angular:** rutas, locale y query params son no confiables; escaping y canonical/redirect allowlist. **F02 Angular→API:** formularios requieren validación backend, cuotas y límites; no confiar en campos del navegador. **F03 público→ERP:** las guardias frontend ayudan UX pero el backend JWT/RBAC/tenant sigue denegando acceso. **F04 demo→datos:** fixture aislado, nunca cuentas/datos reales; no entrar en MySQL empresarial sin scope autorizado. **F05 contenido→HTML/SEO:** sanitizar texto, media y enlaces, sin HTML/JS arbitrario. **F06 email/contacto→SMTP:** idempotencia y anti-spam futuros, no credenciales o envío live por documento. **F07 sitemap→indexador:** no filtrar URL privada o datos sensibles en robots, headers, sitemap. **F08 sitio→observabilidad/terceros:** consentimiento, retención y filtrado de PII antes de conectar analytics.

## Oráculos futuros P01–P06 y N01–N14 — NO_EJECUTADOS

**Positivos:**
- **P01** sitio público aprobado ofrece rutas/anuncios y canonical verificables sin datos ERP;
- **P02** precios y funcionalidades coinciden con catálogo aprobado y versión;
- **P03** demo A/B usa solo fixtures inventados y expira;
- **P04** contacto válido usa validación, consentimiento, throttling y mensaje accesible;
- **P05** sitemap/robots solo incluyen superficie pública y metadatos permitidos;
- **P06** Playwright comprueba navegación 320px, teclado, foco, ARIA, reduced-motion y error.

**Negativos:**
- **N01** visitante anónimo intenta abrir dashboard; acceso denegado;
- **N02** usuario A manipula tenant slug/header para ver B; backend deniega;
- **N03** contacto inserta XSS/script en texto; respuesta neutralizada;
- **N04** spam excede cuota y el servidor limita antes de SMTP;
- **N05** texto comercial no aprobado declara precio o integración inventada; publicación bloqueada;
- **N06** legal placeholder se muestra como política aprobada; NO_GO;
- **N07** sitemap contiene ruta privada/token/correo; validación falla;
- **N08** canonical redirige a dominio no aprobado; bloquear;
- **N09** demo usa cliente real o fixture B desde A; rechazo;
- **N10** UI de contacto presenta falso éxito tras fallo; prueba debe fallar;
- **N11** una ruta sin foco o con scroll horizontal a 320px rompe aceptación;
- **N12** carga de imágenes excede presupuesto y se declara PASS sin medición; bloquear;
- **N13** analítica exporta JWT, correo o PII a tercero no autorizado; rechazar;
- **N14** publicación solicita QA/main/PROD sin permiso adicional y validaciones; NO_GO.

El plan de pruebas futuras exige fixtures reproducibles, SHA, resultado esperado/real y logs redactados; enumerar P/N aquí **NO** implica ejecutar HTTP, Playwright, SEO, SMTP ni seguridad runtime.

## Preflight individual, impacto, auditorías y rollback

**Gate leído:** `PLAN_MAESTRO!A867=C06-01.M01.02`, originalmente H=PENDIENTE/I vacío; `COLA!G867/O867=SI`; `DAG_TECNICO!A828=C06-01.M01.02/F828=VALIDADO/G828=SI`. Dependencias documentales C02/C04/C05 son referencias de preparación, no permiso para marketing live o alterar rutas.

**Impacto real:** nuevo Markdown de `docs/v5` y sección autocontenida del `CHANGELOG_AI.md`; no tocan Angular, API, Domain, Application, Infrastructure, BD, contratos live, JWT/RBAC, PII de usuarios, workers, servicios SMTP, robots ni herramientas de pago.

**A0** comprueba solo dos Markdown, fuentes blobs y ausencia de credenciales. **A1** comprueba A01–A12, I01–I12, F01–F08 y P06/N14 FUTUROS. **A2** preserva tenant A/B, JWT/RBAC, protección de contacto y datos sensibles, solo como contrato. **A3** compara M01.01, arquitectura, DAG y las interfaces Angular reales, sin inferir API o rutas no observadas. **A4** requiere fast-forward/CAS, Scope Lock y check V5 CHANGELOG sobre SHA exacto en `completed/success`, con BITACORA/PLAN/COLA readback; build frontend, .NET, MySQL o Playwright son `NO_APLICA` al diff exclusivamente documental, no PASS fingidos.

**A5_PENDIENTE** hasta revisión independiente por otra de las cinco Tasks Javier; no autoaprobación ni promoción. **Rollback:** `git revert` regular del commit documental, conservar BITACORA sin rewrite, nunca cambiar QA/main/PROD, data, servicios externos o cuentas.
