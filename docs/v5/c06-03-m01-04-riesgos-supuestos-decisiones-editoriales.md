# SOLQARYN V5 — C06-03.M01.04: riesgos, supuestos y decisiones de sitios editoriales y mapas

**Unidad:** C06-03.M01.04 (PLAN_MAESTRO fila 941). **Autor:** PRIMARY DEV 01 JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Fecha:** 2026-11-14. **Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Ámbito:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. **Naturaleza:** preparación documental M01; NO es código, despliegue, prueba runtime ni autorización de proveedor.

## Evidencia observada y límites

La exigencia futura de C06-03 contempla páginas y bloques editoriales por empresa, ubicación/horarios/contacto, mapas opcionales, moderación, sanitización, accesibilidad y SEO. Las fuentes acotadas del árbol `dev` incluyen:

- `docs/v5/c06-03-m01-01-alcance-sitios-editoriales-mapas.md` (blob `d16fd73d5211161bdde429a3b705ec9b9d09cdcf`): doce invariantes NR01–NR12.
- `docs/v5/c06-03-m01-02-actores-interfaces-editoriales.md` (blob `c5f815be7ef044ffb2cd4e4910aa97dff511731a`): A01–A12, I01–I12, B01–B08 y frontera pública/privada.
- `docs/v5/c06-03-m01-03-inventario-fuentes-editoriales.md` (blob `c4434ef2fc3cfb351a0ea5d5c819c0b5d2d560b7`): doce fuentes, ocho brechas G01–G08 y escenarios futuros.
- `frontend/server/seo-utils.js` (blob `b0886e30cc85efa6f800e558d5284412d8c573fc`): `hostFromRequest` lee `x-forwarded-host` o `host` y `isIndexableHost` comprueba coincidencia con `publicOrigin`; esto NO demuestra un exploit, ni establece una autoridad host→tenant general.
- `frontend/api/sitemap.js` (blob `ee50880520c906178a7ebd31553f3ab6d28f6b2d`): genera sitemap de categorías/productos de tienda y fallback vacío/noindex; no es prueba de publicación editorial.
- `frontend/src/app/features/storefront/storefront-identidad.service.ts` (blob `c7dfc61c0b24cba9287b28fab25d3ef45cc4e05f`): identidad de tienda con fallback neutro; no acredita un CMS por tenant.

La lectura es una muestra trazable, no auditoría exhaustiva de backend, MySQL ni superficies externas. Todo riesgo siguiente es **potencial/condicional**; no representa una vulnerabilidad demostrada ni una prueba negativa ejecutada.

## R01–R12: registro de riesgos potenciales y controles exigibles

| ID | Riesgo potencial / condición disparadora | Control de diseño y prueba futura |
| --- | --- | --- |
| R01 | Confianza indebida en Host, X-Forwarded-Host o slug para determinar empresa | Cadena de proxies confiables; resolver backend ligado a tenant autorizado; prueba de host falso A/B |
| R02 | Contenido borrador o preview expuesto por URL o caché pública | Estados draft/revisión/publicado separados; noindex/no-store y token preview con TTL; pruebas crawler |
| R03 | Editor publica o modera sin rol apropiado | RBAC servidor con permisos diferenciados, ownership objeto y denegación por defecto |
| R04 | HTML, URL o bloque editorial permite XSS o embed inseguro | Tipos de bloque permitidos, sanitización en servidor, escapar en UI, CSP y casos maliciosos |
| R05 | Dirección privada, contacto o coordenadas demasiado precisas se publican | Consentimiento y clasificación público/privado; revisión explícita, minimización y controles PII |
| R06 | Horarios y excepciones se mezclan entre sucursales/zonas | EmpresaId+sucursal, zona horaria y calendario explícitos; prueba positiva y negativa A/B |
| R07 | Dependencia de mapas remotos expone claves, tracking o gasto no autorizado | Proveedor y cuota sujetos a decisión; claves nunca en cliente, carga opt-in, fallback textual |
| R08 | Mapa indisponible impide teclado, lectura o contacto | Texto de dirección/horario accesible, enfoque y contraste; pruebas sin JS y mobile 320px |
| R09 | Dos editores pisan la versión aprobada o una reversión pierde historia | ETag/revision CAS, auditoría inmutable, snapshot publicado y rollback normal |
| R10 | Canonical/robots/sitemap indexan hosts no aprobados o contenido privado | Allowlist de host verificado y versión pública; crawl preview/draft NO; pruebas SEO |
| R11 | Caché, assets, previews o errores sirven contenidos cruzados entre tenants | Clave tenant+publicación+visibilidad, invalidación por versión, pruebas A/B y fallos seguros |
| R12 | Documentar fase M01 se interpreta como permiso para DB, JWT, DNS o producción | Gate separado por ID, A0–A4/CI causal, A5 independiente; cambios runtime y despliegue NO autorizados |

**Prioridad de tratamiento propuesta (no programación automática):** R01/R02/R03/R04/R05/R11 son barreras de seguridad y privacidad para cualquier implementación pública; R06–R10 completan consistencia, accesibilidad y SEO; R12 protege alcance y gobernanza. No se asigna severidad de CVE ni se certifica exposición.

## S01–S08: supuestos falsables (estado NO_VERIFICADO)

| ID | Supuesto que NO se adopta como hecho | Evidencia requerida antes de diseñar M02/M03 |
| --- | --- | --- |
| S01 | Cada página editorial tendrá tenant y sucursal verificables | Contrato Domain/Application/API y pruebas A/B |
| S02 | Existirá un flujo editor→moderador→publicador con separación RBAC | Roles reales, matriz de permisos y tests de denegación |
| S03 | El proxy/control de host será confiable y configurado por ambiente | Diagrama de entrada, allowlist y pruebas de cabeceras falsificadas |
| S04 | Habrá soporte de snapshot/versiones y reconciliación de edición concurrente | Contratos ETag/CAS, persistencia y rollback ensayados |
| S05 | El render aceptará solo bloques seguros y URLs aprobadas | Esquemas tipados, sanitizador y suite XSS/URL maliciosa |
| S06 | Ubicación/contacto publicable tendrá consentimiento y reglas PII | Definición de campos, precisión, auditoría y consentimiento |
| S07 | Un mapa externo será necesario, asequible y accesible | Decisión comercial, privacidad, quota y fallback textual |
| S08 | El sitemap actual puede evolucionar sin filtración de borradores | Suite SSR/SEO por host y tenant; regresión de tienda existente |

Si un supuesto falla, **no** liberar automáticamente implementación dependiente: actualizar arquitectura/contratos y DAG individual con evidencia, sin falsear gate SI ni cerrar un lote.

## D01–D08: decisiones pendientes de autoridad/arquitectura

| ID | Decisión que queda PENDIENTE | Opciones y condición de autorización |
| --- | --- | --- |
| D01 | Modelo de página/bloque y ownership | Tenant/sucursal/versiones; definir modelo en M02 sin migrar todavía |
| D02 | Flujo editorial y matriz RBAC | Separación edición, moderación, publicación, auditoría y revocación |
| D03 | Resolver de host y control de proxy | Host verificado con proxies confiables; no asumir X-Forwarded-Host como identidad |
| D04 | Política rich-text/embed/media | Allowlist de componentes, sanitizador, CSP, enlaces y retención |
| D05 | Clasificación y consentimiento de ubicación/contacto | Públicos/privados, precisión, revocación, geolocalización opt-in |
| D06 | Proveedor de mapas y presupuesto | Alternativa sin proveedor, cuotas, privacidad, credenciales y aprobación explícita |
| D07 | Versionado, rollback y SEO público | CAS de publicación, preview TTL, caché particionada, canonical y sitemap |
| D08 | Autorización operativa y lanzamiento | Separar permisos para código, migraciones, dominios, secretos, QA/main/PROD |

Ninguna decisión está APROBADA por este documento. D03 requiere observar configuración real del proxy antes de conectar dominios a tenants; D06 y D08 requieren decisión explícita del propietario antes de contratos o servicios de pago. Cloudinary existente no se sustituye por inferencia.

## Escenarios de validación posteriores: PROPUESTOS, NO_EJECUTADOS

**P01–P06 positivos futuros:** P01 público obtiene solo página publicada de tenant A; P02 editor A guarda draft sin publicar; P03 moderador/publicador autorizados generan snapshot auditado; P04 horario/ubicación públicos preservan sucursal/zona y privacidad; P05 mapa opt-in cae a dirección textual y teclado; P06 SEO indexa solo versión pública con rollback reproducible.

**N01–N12 negativos futuros:** N01 Host/cabecera de B no resuelve A; N02 editor sin publish recibe denegación; N03 draft no aparece en sitemap ni caché; N04 HTML con script/evento inline o URL javascript: se rechaza; N05 dirección privada no sale al público; N06 horario de otra sucursal no se mezcla; N07 caída mapa conserva contenido textual; N08 clave/proveedor no se entrega al browser sin permiso; N09 edición concurrente falla CAS; N10 caché A no sirve a B; N11 host no validado produce respuesta segura noindex; N12 error/reversión no revela token, PII o secreto.

Todos esos escenarios son criterios de aceptación **futuros**, no reportes de pruebas Playwright, xUnit, API, MySQL ni HTTP realizadas.

## Impacto real, pruebas proporcionales y rollback

**Modificación efectiva:** exclusivamente este archivo Markdown y una sección nueva autocontenida en `CHANGELOG_AI.md`; no cambia Domain, Application, Infrastructure, API, JWT/RBAC, tenant, Angular, MySQL, migraciones, workers, eventos, proveedores, mapas, DNS/TLS, observabilidad ni configuraciones productivas. Las restricciones de seguridad, publicación y rollback se documentan, no se implementan.

**A0:** comprobar fuente V5, HEAD, DAG F=VALIDADO/G=SI, ownership y doce riesgos, ocho supuestos y ocho decisiones; verificar alcance documental. **A1/A2/A3 documentales:** cotejar riesgos contra invariantes NR/B/G, escenarios positivos/negativos y denegación tenant A/B; runtime NO_APLICA motivado. **A4:** diff dos Markdown, commit fast-forward exact-SHA, Scope Lock y procedencia CHANGELOG `completed/success`, seguido de readback PLAN/COLA/BITACORA. **A5:** PENDIENTE hasta revisión independiente por otra de las cinco Tasks JAVIER, nunca autoaprobación; QA/main/PROD no autorizados.

**Reversión:** `git revert` normal del commit en `dev`, previo readback y CI causal, sin force-push, y registrar nuevo evento en BITACORA sin alterar historia. Si una etapa futura precisa API, datos, IAM o proveedores, diseñar y probar con su microtarea y autorización propias.
