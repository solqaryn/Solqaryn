# SOLQARYN V16 - C05-01.M02.04: configuracion tipada, ambitos y flags seguros del design system

**ID:** C05-01.M02.04, PLAN_MAESTRO fila 731. **Autor:** PRIMARY DEV 02 JAVIER_CHATGPT, TASK_ID 6af16bb9492c8191b8e8c7d606f11507. **Riesgo V15:** R0 (documental sin runtime). **Fuente:** V5 FINAL STANDARD, C05-01.M01.01-06 LISTO/A5, C05-01.M02.01-03 en estado respectivo, docs/QUALITY_RISK_AUDIT_V15.md, COLA G731/O731 y DAG F/G validados. **Ambito:** solqaryn/Solqaryn dev, PROJECT_SCOPE_LOCK=STRICT.

## 1. Resultado contractual y limites

El alcance de esta unidad es definir **opciones tipadas futuras, ambitos de resolucion, precedencia, denegacion y compatibilidad** de un sistema de diseño multiempresa. Se reutilizan DS01-DS14/INV01-INV18 de M02.01; operaciones conceptuales API01-API08 y errores E01-E12 de M02.02; persistencia DB01-DB12, indices IDX01-12 y migraciones MIG01-12 propuestas de M02.03.

No se agrega ni se activa una flag real, servicio, endpoint HTTP, configuracion de Vercel/Render, migracion EF/MySQL, CSS/JavaScript, Angular, formulario, API, secreto, permiso RBAC ni registro de produccion. Todas las validaciones siguientes son especificacion para implementacion futura, no pruebas ejecutadas. El navegador nunca elige tenant de autoridad; backend JWT/membresia/RBAC vigentes delimita cada solicitud.

## 2. Opciones tipadas OPT01-OPT14

| ID | Opcion conceptual | Tipo y default seguro | Ambito y propiedad |
| --- | --- | --- | --- |
| OPT01 | design.enabled | bool, false | Tenant; no habilita derechos backend |
| OPT02 | design.presetKey | identificador allowlisted o null | Plataforma publica catalogo; tenant selecciona entre permitidos |
| OPT03 | design.presetRevision | entero positivo o null | Catalogo global inmutable |
| OPT04 | design.assignmentRevision | entero monotono >0 | Tenant; compare-and-swap en backend |
| OPT05 | design.semanticColorTokens | mapa limitado de claves permitidas a color hex tipado | Tenant; sin CSS arbitrario |
| OPT06 | design.spacingScale | enumeracion compacta, default neutral | Plataforma; sin expresiones CSS |
| OPT07 | design.typographyScale | enumeracion/version allowlisted, default accesible | Plataforma; no fuentes remotas arbitrarias |
| OPT08 | design.motionPreference | auto/reduced/off, default reduced si contexto incierto | Sesion/UX; no altera permisos |
| OPT09 | design.contrastGuard | bool true | Global; no permitir tema sin contraste probado |
| OPT10 | design.focusVisibilityGuard | bool true | Global; nunca ocultar foco |
| OPT11 | design.assetReference | clave opaca de medio autorizado o null | Tenant propio; sin URL absoluta libre |
| OPT12 | design.previewOnly | bool true | Intencion de tema; nunca publica asignacion |
| OPT13 | design.sessionEpoch | entero positivo de contexto backend | Sesion/tenant; invalida respuesta A recibida en B |
| OPT14 | design.rollbackRevision | entero de revision previamente verificada o null | Tenant; rollback no activa sin autorizacion y audit trail |

**El contrato de opciones no es un inventario de configuraciones existentes.** Los nombres son candidatos y deben contrastarse con estilos/servicios Angular reales antes de instalarse. No implican una tabla en MySQL ni nueva capacidad disponible comercialmente.

## 3. Precedencia, confianza y denegacion V01-V18

1. **V01:** resolver identidad y EmpresaId en backend; denegar usuario no autentico/revocado.
2. **V02:** validar rol y permiso efectivo por operacion, nunca por flag de interfaz.
3. **V03:** nombres de opciones solo dentro del esquema tipado y revision allowlist, sin propiedades arbitrarias.
4. **V04:** precedencia de presentacion: default seguro de plataforma -> preset aprobado -> override tenant validado -> preferencia de accesibilidad del dispositivo, solo en ambito de presentacion.
5. **V05:** una preferencia de dispositivo nunca reemplaza JWT, RBAC, tenant ni política de seguridad.
6. **V06:** OPT05 rechaza valores con url(), expresiones, scripts, HTML, selectores, imports y cadenas no tipadas.
7. **V07:** un recurso grafico OPT11 requiere id opaco, pertenencia de empresa verificada y clasificacion permitida.
8. **V08:** cambio tenant A->B purga cache, assets, variables CSS y respuestas HTTP de A antes de mostrar B.
9. **V09:** respuesta asíncrona de A con epoch anterior a B no puede restaurar el tema de A.
10. **V10:** los defaults desconocidos o ausentes usan un tema neutral de plataforma sin datos privados; nunca el último tema de otro tenant.
11. **V11:** revision optimista de DS06/OPT04 antes de publicar; colision genera error y cero efectos.
12. **V12:** OPT12 preview no crea DS06 activo ni mutacion irreversible.
13. **V13:** intent de publicacion DS09 necesita idempotency key, fingerprint, SHA y actor backend; replay distinto es conflicto.
14. **V14:** accesibilidad OPT09/10 exige evidencia real futura de contraste, foco, teclado, 320/768/1280px y reduced-motion.
15. **V15:** OPT14 rollback propone revision anterior verificable con auditoria DS10 y autorizacion independiente, sin restaurar datos mediante Markdown.
16. **V16:** negar flags habilitadores cuando la fuente de politica, pertenencia, licencia o revision no esta disponible.
17. **V17:** no aceptar sessionStorage/localStorage, querystring, header, hostname o claim arbitrario como grant ni override de propiedad.
18. **V18:** documentar cambios de revision, fallback y rollback con log saneado; nunca incluir JWT, PII, URL privada, credenciales ni datos de B.

La precedencia de UI no es un orden de autorizacion: todas las capas consumen decisiones backend ya verificadas. La ausencia de opcion equivale a disabled o tema neutral, segun el tipo; jamás a un permiso global.

## 4. Compatibilidad y matriz futura de pruebas P01-P10 / N01-N10

| Caso futuro | Positivo por ejecutar | Negativo por ejecutar |
| --- | --- | --- |
| P01/N01 | Empresa A carga tema propio tipado | Empresa B solicita preset privado A y recibe rechazo sin datos A |
| P02/N02 | Un preset registrado resuelve tokens y revision | Clave desconocida, version retirada o ciclo de alias no aplica CSS |
| P03/N03 | Override validado de color hex | Texto CSS/JS, url() o selector inyectado se rechaza |
| P04/N04 | Asset interno de A verificado | URL externa o asset de B nunca se instala |
| P05/N05 | Epoch A cambia a B y muestra tema neutral/B | Respuesta tardia de A no reinyecta logo ni colores A |
| P06/N06 | Preview tipado presenta sin mutacion DS06 | Preview no activa ni concede rol |
| P07/N07 | Intent CAS revision actual con clave unica | Revision stale, replay con payload distinto o doble clic no publica dos veces |
| P08/N08 | Contraste, foco y teclado medidos con navegador | Tema inaccessible/foco oculto no se certifica |
| P09/N09 | Usuario autorizado puede solicitar rollback propuesto | Rol revocado, revision inexistente y tenant B son negados |
| P10/N10 | Sesion sin bandera usa fallback neutral | Cache/plan ausente no activa capacidades ni privilegios adicionales |

**Estado real de todos los casos:** FUTUROS_NO_EJECUTADOS; no hay medidas WCAG, Lighthouse, Playwright, HTTP, SQL ni test xUnit ejecutado en esta microtarea. Para codigo Angular posterior son obligatorios npm build, tipado, accesibilidad y Playwright dirigido; al modificar datos reales sera necesaria CI MySQL 8.4 efimera.

## 5. Gates de salida, riesgo y rollback

- **A0:** alcance documental acotado al archivo de este ID y seccion autocontenida de CHANGELOG, contratos M02 precedentes inspeccionados y SHA dev exacto; sin valores secretos.
- **A1:** validar integridad estatica OPT01-14, V01-18 y P/N10 con tablas sin contradicciones; ningun caso funcional ejecutado.
- **A2:** rechazo conceptual tenant A/B, JWT/RBAC revocacion, CSS/asset injection, epoch obsoleto, idempotencia y defaults fail-closed; no proclamar pruebas de vulnerabilidad ejecutadas.
- **A3:** consistencia con DS01-14/INV01-18, API01-08 y DB01-12; builds .NET/Angular y DB/Playwright son NO_APLICA para un diff Markdown-only, no se anotan como PASS.
- **A4:** fast-forward/CAS Git dev, verificacion del diff y blobs, PROJECT_SCOPE_LOCK STRICT, check verify-solqaryn-project-scope y control V5 procedencia ambos completed/success exact SHA, readback PLAN/COLA/DAG/BITACORA.
- **A5:** otro TASK_ID Javier de las cinco restantes revisa artefacto, fuentes, seguridad, SHA y evidencia antes de liberar sucesoras. El autor no se autoaprueba.

Rollback de esta entrega = git revert convencional en dev del documento y de su seccion CHANGELOG, evento compensatorio nuevo, sin force push ni tocar BITACORA historica. No ejecutar rollback de DB, infraestructura, secretos, DNS, QA/main/PROD desde este contrato.
