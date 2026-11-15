# SOLQARYN V16 — C06-01.M02.02: contrato API/DTO/errores para sitio público SaaS

**Identificador:** C06-01.M02.02 (PLAN_MAESTRO fila 873). **Autor:** PRIMARY DEV 05 JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Repositorio:** `solqaryn/Solqaryn@dev`. **Scope Lock:** STRICT. **Riesgo V15:** R0, contrato documental sin código ejecutable.

## 1. Resultado real, no-alcance y fuentes

Se define una **interfaz de contrato futuro** para propuesta de valor, planes/sectores, solicitud de demo, contacto, legal y SEO, incluyendo DTO, denegaciones y compatibilidad. **NO existe ninguna ruta nueva ni API implementada por esta microtarea**; los nombres de operación/URI siguientes son propuestas sujetas a asignación de rutas en M03/M04. No se modifica Angular, ASP.NET Core, MySQL/EF, SMTP, JWT/RBAC, DNS, dominios, secretos, precios, catálogo publicado ni despliegues. El sitio actual mantiene raíz hacia login, storefront `tienda/*` distinto y ERP con guardias.

Fuentes originales revisadas desde dev: `C06-01.M01.01` blob `96d8280c96125af30b8f3f580c092460cb397b84` (NR01-NR12); `M01.02` `abc1084b024d32391559f02c365cf4111c5f739a` (A01-A12/I01-I12); `M01.03` `e3d90f0f7f49da2360b11120139ff4003d4ae50e` (12 fuentes/B01-B08); `M01.04` `c2b970feee1b2ab1d62349f21af394655070a77a` (R01-R12, D01-D08 PENDIENTES); `M01.05` `c6e254f6728a00a220216875341f8dd61065e936` (AC01-AC14/N01-N14 FUTUROS_NO_EJECUTADOS); `M01.06` `5f13fec349767c5b6deccd8857eafd33109ae49d` (16 BL, 8 REV, 8 T hipotéticos). Los A5 de M01 se verifican en PLAN/BITACORA; DAG F/G y COLA G/O se releen en el momento de ejecutar cada sucesor.

La arquitectura vigente continúa Angular 22.2.1 standalone+Zone.js, ASP.NET Core 10 Domain←Application←Infrastructure con API como borde, MySQL 8.4/EF Core 10, JWT/RBAC/autorización de servidor y aislamiento tenant A/B. Nada en este documento modifica esos contratos.

## 2. Ocho operaciones lógicas propuestas (NO ENDPOINTS)

| Operación | URI tentativa, método | Precondición futura | Resultado o rechazo |
| --- | --- | --- | --- |
| OP01 lectura de landing | GET /api/publico/v1/presentacion | Publicación de contenido aprobado y versionado | LandingPublicaV1 o 404 sin información privada |
| OP02 catálogo de planes | GET /api/publico/v1/planes | Catálogo comercial firmado y vigente D01 | Lista de planes públicos filtrados; si no aprobado, sin precios |
| OP03 sectores | GET /api/publico/v1/sectores | Copia aprobada, recursos multimedia allowlisted | TarjetasSectorV1, nunca datos reales de clientes |
| OP04 documento legal | GET /api/publico/v1/legal/{tipo} | D02 aprobada por versión/jurisdicción | DocumentoLegalV1 o 404 sin placeholder |
| OP05 metadatos SEO | GET /api/publico/v1/seo/{ruta} | Ruta pública allowlisted y canonical aprobada | MetadatosSeoV1 o denegación de rutas privadas |
| OP06 solicitud de demo | POST /api/publico/v1/demo/solicitudes | Consentimiento, TTL ficticio, antifraude y cuota | AcusePublicoV1 neutro, sin crear tenant real |
| OP07 contacto | POST /api/publico/v1/contacto/solicitudes | DTO mínimo validado, consentimiento y rate limit | AcusePublicoV1 neutro, sin SMTP automático |
| OP08 disponibilidad pública | GET /api/publico/v1/estado-contenido | Solo estado editorial NO estado de infraestructura | VersionPublicaV1 o 503 genérico sin diagnóstico interno |

Las URI son **propuestas NO_REGISTRADAS**, no pruebas de existencia, ni URL para visitantes. Separación de `/login`, `/tienda`, `/dashboard`, `/plataforma` y rutas existentes es obligatoria. Operaciones futuras de administración de catálogo y precios requieren superficies privadas distintas con autenticación backend, revisión legal/comercial y RBAC; **no** se añaden a la API pública anónima.

## 3. Ocho DTO conceptuales y límites

| DTO | Campos mínimos propuestos | Rechazo/conservación |
| --- | --- | --- |
| LandingPublicaV1 | version, título, resumen, CTA allowlisted, enlacesPublicos | Nunca tenantId ni recursos privados |
| PlanesPublicosV1 | catalogoVersion, items[planCode, nombre, prestacionesPublicas, moneda?, precioAprobado?] | Precio ausente/no vigente se omite, nunca se inventa |
| TarjetasSectorV1 | version, items[codigo, titulo, copySeguro, imagenPermitida?] | Sin testimonios o identidades de clientes no autorizados |
| DocumentoLegalV1 | tipo, version, jurisdiccion, aprobadoEn, contenidoSeguro | Prohibir placeholder legal y texto sin aprobación |
| MetadatosSeoV1 | routeKey, title, description, canonical, indexable | canonical del origen allowlisted, sin tokens |
| SolicitudDemoV1 | emailMinimo?, consentimientoVersion, motivo, nonce, idioma | Ni credenciales, ni tenantId, ni datos de PROD |
| SolicitudContactoV1 | nombreMinimo?, emailMinimo, mensajeLimitado, consentimientoVersion, nonce | No adjuntos arbitrarios, XSS ni PII redundante |
| AcusePublicoV1 | correlacionOpaca, estadoNeutro, versionContrato, reintentarDespues? | Sin existencia de usuario/empresa, SMTP, IDs internos |

`VersionPublicaV1` en OP08 se representa por una proyección reducida del estado editorial versionado, sin uptime interno, buildSHA, credenciales ni metadata de infraestructura. Los campos anteriores son **especificaciones**: nombres/cotas definitivos dependen de aprobación y pruebas M03; no se afirma serialización HTTP ni DTO .NET real.

## 4. Doce categorías de errores de contrato (NO IMPLEMENTADAS)

| Código propuesto | Causa futura | Comportamiento fail-closed |
| --- | --- | --- |
| E01 | ruta no pública | 404/403 seguro, sin metadata ERP |
| E02 | catálogo no aprobado | ausencia de precio o 503 neutro |
| E03 | versión legal no aprobada | 404; no publicar borrador |
| E04 | consentimiento ausente/inválido | 400 sin guardar lead |
| E05 | campo inválido/longitud | 400 con campo permitido, sin eco PII |
| E06 | cuota/rate limit | 429 con espera acotada |
| E07 | replay de nonce/idempotencia | respuesta neutra idempotente, no duplicar efectos |
| E08 | contenido XSS/HTML malicioso | 400 o texto seguro sin render ejecutable |
| E09 | URL externa/canonical no autorizada | 400/404 sin redirect abierto |
| E10 | intento tenant A/B o recurso privado | 403/404 sin enumeración ni datos |
| E11 | proveedor temporalmente caído | 503 genérico, sin stack traces |
| E12 | cancelación/timeout/conflicto de versión | 409/408/503 seguro según causa validada |

Los códigos HTTP son objetivos **a verificar en integración futura**, no evidencia de rutas existentes. Nunca registrar JWT, tokens, correos completos, mensajes de contacto, cabeceras sensibles ni datos empresariales en observabilidad pública.

## 5. Dieciséis validaciones futuras verificables V01–V16

| ID | Validación requerida antes de crear runtime |
| --- | --- |
| V01 | Publicación versionada firmada de claims y CTA sin beneficios inventados |
| V02 | D01 catálogo de planes/precios/monedas aprobado o no exposición de precio |
| V03 | D02 legal/privacidad/consentimiento aprobado, no placeholder |
| V04 | URI y nombre de ruta estrictamente allowlisted, sin redirección abierta |
| V05 | Contrato V1 con negociación/compatibilidad, no romper login ni tienda |
| V06 | Sin confiar en `tenantId`, slug, host ni cabecera aportados por visitante |
| V07 | API backend siempre decide RBAC/tenant cuando exista recurso privado |
| V08 | Sanitización contextual XSS/HTML y límites de texto/bytes UTF-8 |
| V09 | Datos de demo solo sintéticos, aislados y con TTL demostrable |
| V10 | Contacto/demo con consentimiento explícito, nonce e idempotencia acotada |
| V11 | Rate limit y antifraude por contexto sin enumeración ni fuga de IP/PII |
| V12 | SMTP u otros proveedores solo con secretos externos y aprobación explícita |
| V13 | Robots/sitemap/canonical no listan URL ERP, token o id de empresa |
| V14 | Logs correlacionados por identificador opaco; retención/PII minimizados |
| V15 | Carga Angular responsive/teclado/ARIA y navegación anónima Playwright |
| V16 | CI del SHA, seguridad A/B, Scope Lock, rollback probado y aprobación A5 |

Estas son obligaciones para la **implementación futura**. V08/V10/V11 exigirán pruebas negativas reales de servidor y proveedor al implementarse; una tabla Markdown no valida seguridad de ejecución.

## 6. Diez pares de aceptación futuros P01–P10 / N01–N10

| Caso | Positivo FUTURO_NO_EJECUTADO | Negativo/frontera FUTURO_NO_EJECUTADO |
| --- | --- | --- |
| P01/N01 | OP01 sólo presenta copy aprobado | visitante no abre dashboard privado |
| P02/N02 | OP02 entrega plan comercial aprobado | precio inventado no se entrega |
| P03/N03 | OP03 muestra sector sin PII | recurso tenant B denegado a A |
| P04/N04 | OP04 entrega versión legal vigente | borrador/consentimiento inválido no publica |
| P05/N05 | OP05 canonical allowlisted por ruta | URL externa/token no indexa |
| P06/N06 | OP06 registra solicitud de demo consentida | nonce repetido/no-tenant válido no crea demo |
| P07/N07 | OP07 valida contacto mínimo | XSS, tamaño excesivo, spam y 429 rechazados |
| P08/N08 | OP08 muestra versión editorial neutra | error interno no filtra build/stack/secreto |
| P09/N09 | rutas públicas conservan auth ERP | usuario anónimo o revocado no obtiene datos |
| P10/N10 | despliegue futuro con CI/rollback causal | SHA distinto/CANCELLED/SKIPPED bloquean |

**Número de pruebas ejecutadas por este documento: cero.** Son criterios falsables vinculados a AC01–AC14/N01–N14 y BL01–BL16 de M01, no sustituyen xUnit, HTTP, Playwright ni integración con MySQL.

## 7. Compatibilidad, dependencias y rollback

- **Backward compatibility:** no modificar `frontend/src/app/app.routes.ts` ni prefijo privado en esta microtarea. El contrato V1 es opt-in y no habilita Angular rutas públicas; mantenimiento de `/login`, `/tienda` y ERP privado se prueba en M03/M04.
- **Fronteras:** dominio de contenido público sin identidad empresarial; backend sigue siendo autoridad para JWT/RBAC/tenant. Versionado y estado editorial no confieren grants ni constituyen aprobación comercial, legal o de producción.
- **Consistencia:** POST de contacto/demo deberá tener idempotencia con ventana y retención aprobadas, rechazo de replays, auditoría minimizada y reversibilidad. No se diseñan escrituras directas desde el navegador a MySQL.
- **Dependencias DAG:** C06-01.M01.01–06 LISTO + A5 independiente, más gates institucionales calculados; los M02 hermanos son documentos independientes y no se les atribuye implementación. Ninguna M03/M04/M05/M06 técnica se desbloquea manualmente.
- **Rollback de este documento:** revert ordinario del commit que agrega este archivo y su sección autocontenida de CHANGELOG, sin force-push ni alterar BITACORA. Rollback de API futura requiere código, datos de prueba y revisión separada.

## 8. Verificación por alcance A0–A5

- **A0 R0:** examinar documento y CHANGELOG, 8 operaciones propuestas, 8 DTO conceptuales, 12 errores, 16 validaciones y 10 pares P/N; comprobar no incluir código ejecutable/secretos/claims comerciales no aprobados.
- **A1 R0:** comprobar coherencia lógica entre DTO/operación, contrato positivo/negativo, rutas existentes y decisiones D01–D08 PENDIENTES. **No son pruebas HTTP ejecutadas.**
- **A2 R0:** revisar barreras escritas JWT/RBAC, tenant A/B, XSS, autorización backend, consentimiento y PII; no afirmar test de seguridad runtime.
- **A3 R0:** `NO_APLICA` a compilación .NET/Angular, MySQL/EF, SMTP, HTTP y Playwright por diff sólo Markdown. Inspección documental y fuentes no equivale a integración.
- **A4:** exigir Scope Lock + procedencia V5 `completed/success` en SHA de publicación (o equivalencia demostrada), diff exacto de 2 Markdown, BITACORA y readbacks institucionales. Nunca atribuir PASS a check pendiente, skipped o cancelled.
- **A5:** `A5_PENDIENTE` de una de las otras cinco Tasks Javier; el autor no puede revisarse. No habilita QA, main, PROD, dominios ni datos de clientes.

**Estado final de esta especificación:** contrato documental R0, no release público ni activación de producto; cualquier decisión comercial/legal D01–D08 permanece pendiente.
