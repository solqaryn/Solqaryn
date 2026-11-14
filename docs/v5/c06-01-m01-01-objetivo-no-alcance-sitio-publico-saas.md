# SOLQARYN V5 — C06-01.M01.01: objetivo, no-alcance y no-regresión del sitio público SaaS

**Microtarea:** `C06-01.M01.01`, PLAN_MAESTRO fila 866. **Autor:** SOLQARYN PRIMARY DEV 02 JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, documento Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Repositorio:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-14.

## Objetivo verificable del lote y frontera de esta microtarea

Delimitar el alcance de **C06-01 — sitio público SaaS**, como preparación para una futura superficie que comunique propuesta de valor, planes, sectores, demo, contacto, contenido legal y SEO, sin mezclarla con aplicaciones privadas, tienda, catálogo empresarial o credenciales. La evidencia futura requerida en el plan son **rutas públicas, metadatos, sitemap y formulario seguro**, cada uno validado en su propia fase de implementación. En esta M01.01 se redactan solamente objetivo, exclusiones y no-regresión: **no crea rutas, componentes, HTML, textos comerciales definitivos, API, formularios, precios, dominios ni indexación**.

La conversión comercial no justifica degradar JWT/RBAC/tenant, abrir rutas privadas, anunciar servicios no implementados o afirmar que un demo/plazo/promoción ha sido aprobado. La versión ejecutable del catálogo, precios y términos legales exige revisión comercial y jurídica antes de publicación. No se deduce autorización contractual de un prototipo.

## Línea base delimitada en HEAD de dev (lectura, no prueba de experiencia)

| Fuente leída | Blob Git | Observación limitada |
| --- | --- | --- |
| `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | Rutas Angular; el path raíz redirige a login, distingue storefront `tienda/*`, rutas guardadas `dashboard` y `plataforma`; no es una landing SaaS comercial certificada |
| `frontend/src/app/app.config.ts` | `cd77384e713014015d83b348955b3d83573348b3` | Router, guardias, interceptor HTTP, `provideZoneChangeDetection`, preload selectivo; no confirma SEO ni SSR |
| `frontend/angular.json` | `8b00f8fee80fdfafe1864e233be7d5f20ad444e3` | `@angular/build:application`, presupuesto initial 650/750 kB y assets; no informa peso emitido ni metadatos indexados |
| `frontend/src/app/shared/feedback-state/feedback-state.component.ts` | `7deb40475f0c492d09e649e7351b2915bd53d356` | Estados `loading/empty/success/error/warning`, `aria-live`; no asegura adopción en contacto/demo |
| `docs/v5/c05-01-m01-01-objetivo-no-alcance-design-tokens.md` | `ea7c5e7d339838f021b3f746fd9a7fe307b58b8d` | Contrato de tokens/contraste/responsive; no evidencia de cumplimiento E2E |
| Skill `solqaryn-project-governance` y `references/architecture.md` de dev | verificar ante cambio de HEAD | Angular 22.2.1 standalone con Zone.js, ASP.NET Core 10, MySQL y JWT/RBAC; las páginas públicas no heredan permisos privados |

La inspección de rutas es parcial al archivo citado y no demuestra que no existan otros canales externos; tampoco se ha realizado auditoría web, Lighthouse, prueba Playwright, verificación de robots/sitemap en un dominio ni revisión jurídica. Toda ruta o documento futuro se verificará en el SHA donde se implemente.

## Doce invariantes NR01–NR12 del sitio público futuro

| ID | Invariante de no-regresión | NO_GO observable |
| --- | --- | --- |
| NR01 | Separar página marketing pública y ERP privado | CTA o ruta pública desactiva authGuard/permisoGuard en rutas de negocio |
| NR02 | Propuesta de valor respaldada por funcionalidades certificadas | Afirmar facturación, pago, módulo o integración inexistente |
| NR03 | Planes/funciones y precios solo de catálogo aprobado | Precio o beneficio inventado, no vigente o asociado a otro tenant |
| NR04 | Sectores y casos de uso sin mostrar datos reales | Página incluye identificadores, fotos, ventas o PII de una empresa |
| NR05 | Demo aislada con datos ficticios y expiración | Demo consulta Aiven/Cloudinary productivo o cruza tenant |
| NR06 | Contacto antispam y mínimo de datos personales | Formulario público permite envío ilimitado, XSS o exposición de correo |
| NR07 | Páginas legales versionadas y aprobadas | Texto placeholder se publica como política o garantía legal vigente |
| NR08 | SEO sin indexar rutas privadas, duplicadas o con secretos | Sitemap publica dashboard, cuenta, login sensible o URLs con token |
| NR09 | Metadatos seguros y canónicos por ruta | HTML metatag inyectable, URL canónica no aprobada, redirección abierta |
| NR10 | Accesibilidad y ligereza por defecto | Navegación imposible a 320px, sin foco/ARIA o animación forzada |
| NR11 | Cargas de media optimizadas sin proveedor nuevo impuesto | Videos/imágenes abren bucket/SDK, PII o URL privada permanente |
| NR12 | Releases reversibles separados de auth/ERP y producción | Contenido público se despliega sin aprobación, CI o rollback |

Estos límites se aplicarán a futuras implementaciones en UI, API, SEO y formularios. Un requisito documental no autoriza pagar herramientas, crear dominio, modificar DNS, habilitar R2, enviar SMTP real ni activar recopilación de leads.

## Contratos esperados y exclusiones expresas

**Dentro del lote futuro, sujeto a gates propios:** ficha de propuesta de valor verificable, catálogo comercial aprobado, sectores sin testimonios inventados, demo efímera sin datos reales, contacto con consentimiento/validación y límites, contenido legal revisado, títulos/descripciones/canonical, robots/sitemap controlado, estado vacío/error/éxito accesible, lazy load y navegación móvil. Metadatos deben distinguir rutas `publicas` de rutas `privadas`, sin confiar en frontend como control de autorización.

**Fuera de M01.01:** marketing desplegado, redacción publicitaria final, precios, suscripción/cobros, chatbot, captación real de correos, seguimiento con cookies, pixel/analytics, migración de datos, SDK externo, backend nuevo, cambios de IAM/JWT/RBAC/tenant, URL de producción, robots live, releases QA/main/PROD, secrets, webhooks, workers y creación o reprogramación de Tasks.

**Criterios operativos de salida M01:** M01.02 identifica actores y entradas/salidas reales; M01.03 verifica fuentes con SHA y brechas; M01.04 documenta riesgos/decisiones; M01.05 especifica aceptación; M01.06 no se declara concluida hasta revisión A5 de antecedentes y evidencia causal. M02–M06 y pagos/datos siguen en NO_GO mientras carezcan de puertas y permisos propios.

## Pruebas positivas futuras P01–P06 — NO_EJECUTADAS

- **P01:** ruta pública aprobada responde con título/descripción/canonical correctos, sin token ni datos privados en HTML.
- **P02:** páginas planes muestran exclusivamente precios y prestaciones versionados y aprobados, con prueba de fecha/fuente.
- **P03:** demo sobre datos ficticios funciona sin acceso a tenant A/B real.
- **P04:** formulario contacto válido aplica validación, consentimiento y rate limiting, con respuesta accesible y mínima.
- **P05:** sitemap incluye solo rutas públicas aprobadas y robots excluye ERP, búsqueda privada y sesiones.
- **P06:** Playwright Chromium valida 320px, teclado, foco, reduced-motion, estados de error y rendimiento medido en fixture reproducible.

## Pruebas negativas futuras N01–N14 — NO_EJECUTADAS

- **N01:** usuario anónimo entra al dashboard; rechazar sin credenciales ni datos.
- **N02:** editar slug/header intenta cambiar tenant en demo; rechazar/fallar cerrado.
- **N03:** página pública filtra correo, cuenta o factura real; prueba de ausencia.
- **N04:** precio no aprobado aparece al público; bloquear publicación.
- **N05:** contenido legal sin revisión/versión se presenta como definitivo; NO_GO.
- **N06:** contacto sin consentimiento, payload XSS o URL externa maliciosa; rechazar.
- **N07:** spam masivo supera cuotas/captcha equivalente aprobado; limitar sin enviar email real en CI.
- **N08:** sitemap indexa `dashboard` o token de sesión; prueba que no incluye.
- **N09:** `canonical` apunta a dominio no aprobado/entorno privado; rechazar.
- **N10:** imágenes pesadas rompen budget/lazy routing, sin medición; no declarar PASS.
- **N11:** viewport 320px produce scroll/foco inaccesible; test Playwright falla.
- **N12:** error de formulario se oculta como éxito o divulga stacktrace; respuesta segura.
- **N13:** tracking externo transmite JWT/PII a tercero; NO_GO sin aprobación de privacidad.
- **N14:** despliegue a QA/main/PROD sin autorización explícita; rechazar.

Los casos positivos y negativos anteriores son **oráculos futuros**, no resultados de pruebas ejecutadas ni declaración de cumplimiento SEO/WCAG. Backend sigue siendo la autoridad de permiso y tenancy; el frontend solo controla experiencia.

## Preflight y controles A0–A5 de la preparación

**Fuentes y DAG:** `PLAN_MAESTRO!A866=C06-01.M01.01` originalmente PENDIENTE/I866 vacío, `COLA!G866/O866=SI`, `DAG_TECNICO!A827=C06-01.M01.01/F827=VALIDADO/G827=SI`. Los lotes documentales C02, C04 y C05 son antecedentes preparatorios, no autorización de campañas, pagos, accesos ni releases.

**Impacto real:** un nuevo documento `docs/v5/` y una sección del CHANGELOG. Cero cambios en Angular/SCSS, .NET, API/DTO, base, permisos, JWT/RBAC/tenant, datos, Cloudinary, correo, DNS o producción.

**A0:** comprobar diff de dos Markdown y procedencia/ausencia de secretos. **A1:** verificar NR01–NR12, P01–P06, N01–N14 y distinguir FUTUROS NO_EJECUTADOS. **A2:** preservar rutas privadas, tenant A/B, límites de PII/consentimiento y antiabuso, sin atribuir tests de runtime. **A3:** cotejar fuentes Git y arquitectura de `dev`, plan y DAG. **A4:** commit fast-forward CAS, `verify-solqaryn-project-scope` y V5 procedencia CHANGELOG `completed/success` sobre SHA exacto y readback PLAN/COLA/BITACORA; CI Angular, Playwright, .NET/MySQL sin runtime tocado es `NO_APLICA` y nunca PASS ficticio.

**A5_PENDIENTE:** revisión independiente por una Task Javier distinta de la autora antes de liberar lotes posteriores. **Rollback:** `git revert` normal del documento y CHANGELOG, sin reescribir historia. No QA/main/PROD, eventos ni activación de servicios externos.
