# SOLQARYN V16 — C05-03.M01.06: bloqueos, reversión y liberación condicionada del builder gobernado

**Microtarea:** C05-03.M01.06, PLAN_MAESTRO fila 799. **Responsable/autor documental:** PRIMARY DEV 02 JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Fuente única:** Standard de Implementación de Automatizaciones V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Riesgo V15:** R0 documental. **Fecha:** 2026-11-15.

## 1. Límite y criterio exacto de salida

Este cierre M01 define **puertas documentales** de bloqueo y reversión para un FUTURO builder de páginas/secciones configurables a partir de un catálogo tipado de componentes Angular precompilados, sin permitir HTML/JS/SQL arbitrarios, URL peligrosas, configuración sin límites ni bypass de autoridad backend. No desarrolla ni activa el builder, API, editor, renderizador, esquema EF, migración, integración, host, rutas públicas, flags, permisos, datos, proveedor o despliegue.

La línea base existente TemaVisual, tokens SCSS y servicios de configuración de empresa **NO equivale a un constructor** ni constituye prueba de sanitización global. El front Angular 22.2.1 no reemplaza ASP.NET Core 10, JWT/RBAC, ownership de recursos y aislamiento A/B. Los riesgos R01–R12 y decisiones D01–D08 de M01.04 permanecen hipótesis/decisiones PENDIENTES, no aprobaciones.

**Preflight documental:** PLAN H799=EN_PROGRESO e I799=6af16bb9492c8191b8e8c7d606f11507 por CLAIM previo, DAG_TECNICO A760=C05-03.M01.06/F760=VALIDADO/G760=SI al revisar; no reclamar otra EN_PROGRESO propia. La conclusión de este documento no transforma la futura implementación M02–M06 en trabajo habilitado sin sus dependencias reales y A5 de otra Task Javier.

## 2. Bloqueos BL01–BL16

Se deben evaluar antes de implementar, publicar o revertir versiones del builder; un solo bloqueo aplicable impide el paso técnico, no impide conservar este contrato documental.

| ID | Condición de bloqueo verificable | Respuesta fail-closed exigida |
| --- | --- | --- |
| BL01 | Catálogo versionado inexistente o clave desconocida | Ningún componente/propiedad arbitraria se interpreta |
| BL02 | Markup HTML, handlers, script o expresiones dinámicas no permitidas | El contenido queda como dato o se rechaza sin ejecución |
| BL03 | CSS libre o estilo que evade tokens y CSP | Solo opciones tipadas y componentes precompilados |
| BL04 | Destino URL peligroso, red interna, esquemas inseguros o SSRF | Validación allowlist y rechazo server-side |
| BL05 | JWT inválido, expirado, revocado o falta de membresía | Acceso, edición, preview y publicación denegados |
| BL06 | Tenant A alcanza recurso de tenant B por ID, slug, host o caché | Probar aislamiento A/B y no divulgación |
| BL07 | Preview con persistencia, worker, correo, webhook o publicación | Preview estrictamente sin efectos secundarios |
| BL08 | Usuario sin permiso intenta guardar, revisar o publicar | RBAC backend de operación y recurso obligatorio |
| BL09 | Revisión obsoleta sobreescribe versión nueva | CAS/ETag, conflicto 409 y preservación de historia |
| BL10 | No hay límites aprobados de bytes, secciones o profundidad | Rechazo seguro de carga excesiva/recursión |
| BL11 | Errores o auditoría filtran secretos, PII, consultas o IDs ajenos | Redacción y respuesta mínima verificadas |
| BL12 | Host público se vincula a tenant erróneo | Mapeo server-side y caché particionada verificados |
| BL13 | Fallback o rollback borra revisiones o cambia datos ajenos | Reversión aditiva trazable y recuperación probada |
| BL14 | Editor/preview falla en teclado, foco, 320px o reduced-motion | A11y y comportamiento responsivo probados |
| BL15 | Cualquier control obligatorio CI está FALLIDO, CANCELADO o pendiente | No declarar PASS ni habilitar técnica sucesora |
| BL16 | Persisten decisiones D01–D08 sin autoridad/evidencia requerida | NO_GO al runtime/publicación, aunque M01 documental esté LISTO |

**Regla:** los textos que contienen palabras SQL/HTML son datos inocuos cuando están escapados; se rechaza su *ejecución* o interpretación libre, no una secuencia de caracteres legítima. Los riesgos se confirman mediante pruebas futuras, no mediante esta matriz.

## 3. Procedimiento REV01–REV10 de rollback FUTURO

Cada paso necesitará ownership, permisos operativos, ambiente aislado, evidencia y decisión autorizada; ninguno fue ejecutado durante esta microtarea.

| ID | Acción condicionada para el runbook de implementación |
| --- | --- |
| RB01 | Identificar versión exacta, tenant, publicador y cambio; congelar nuevas publicaciones afectadas con autorización futura |
| RB02 | Conservar log, manifiesto/version, SHA, permisos y evidencia antes de intervenir; no borrar auditoría |
| RB03 | Revocar temporalmente capacidad del builder en el ámbito afectado mediante mecanismo previamente aprobado |
| RB04 | Dirigir tráfico a versión previamente publicada y comprobada sin reactivar contenido inseguro |
| RB05 | Revertir configuración con nueva revisión, nunca modificar historial o hacer hard-delete |
| RB06 | Invalidar caché segmentada solo de host/tenant afectado y comprobar ausencia de mezcla A/B |
| RB07 | Verificar enlaces, assets y esquemas permitidos; retirar referencias riesgosas sin eliminar recursos ajenos |
| RB08 | Comprobar JWT/RBAC/membresía, acceso anónimo mínimo y revocación después del rollback |
| RB09 | Ejecutar regresión funcional, seguridad, A/B, teclado, accesibilidad y telemetría antes de liberar |
| RB10 | Registrar resultado, causa, aprobaciones y camino de recuperación/republicación; mantener bloqueo si falla |

Si el rollback degrada aislamiento, accesibilidad, auditoría o integridad de borradores/versiones, mantener **NO_GO** y revertir de forma aditiva tras revisión competente. No operar QA/main/PROD o Aiven real mediante esta definición.

## 4. Puertas de liberación LG01–LG12

Todas son **REQUISITO FUTURO / NO EVALUADO** para implementar o poner en servicio el builder, no resultados A0–A4 de esta documentación.

| ID | Puerta | Evidencia técnica que la satisface en el futuro |
| --- | --- | --- |
| LG01 | D01 catálogo/version tipado | Contrato aprobado con claves y campos permitidos; rechazo unknown |
| LG02 | D02 permisos por operación | Matriz de roles con backend authorization probada |
| LG03 | D03 cuotas medibles | Cotas acordadas y tests límite, error y carga con evidencias |
| LG04 | D04 URL/CSP | Esquemas/destinos permitidos y negativos SSRF/XSS ejecutados |
| LG05 | D05 versionado/CAS | Conflictos y reversión versionada probados sin lost update |
| LG06 | D06 preview | Sin writes, workers, eventos ni proveedores y con aislamiento |
| LG07 | D07 binding host/tenant/cache | Pruebas de publicación pública y A/B cruzada sin fugas |
| LG08 | D08 accesibilidad | Playwright/Chromium teclado, 320px, foco, reduced-motion |
| LG09 | Integración Angular/.NET | Frontend, API y reglas backend reales con CI causal |
| LG10 | EF/MySQL, si se implementa persistencia | MySQL 8.4 efímero, migración/rollback y test Category=Integration |
| LG11 | Seguridad y observabilidad | A2 JWT/RBAC/PII/secretos, auditoría, revocación y tests adversariales |
| LG12 | Evidencia de publicación autorizada | Scope Lock STRICT, V5, A0–A4 exact SHA, A5 independiente; QA/PROD aparte |

M01.06 **LISTO documental** no convalida decisiones D01–D08 de M01.04, ni habilita por sí solo código de componentes, IAM, JWT/RBAC, persistencia, host, tenancy, migraciones, QA o producción. Para cualquier sucesora técnica aplicar de nuevo PLAN H/I, COLA G/O, DAG F/G, A5 y Scope Lock en ese momento.

## 5. Diez parejas de escenarios positivos/negativos FUTUROS, NO EJECUTADOS

Cada pareja será un oráculo de aceptación de fases implementadas y debe fallar frente al defecto simulado; no describe resultados reales de tests.

| Par | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| T01 | catálogo válido presenta componente tipado | clave no registrada o versión incompatible se rechaza |
| T02 | texto inocuo se muestra escapado | handler/JS/markup nunca se ejecuta |
| T03 | URL pública aprobada se acepta como dato | javascript:/file:/IP privada se rechaza |
| T04 | usuario A con permiso edita borrador A | A no puede acceder a recurso ni host de B |
| T05 | publicador autorizado usa revisión actual | sin permiso o revisión obsoleta falla sin mutar |
| T06 | preview propio responde sin efectos | preview no lanza webhook/worker/publicación |
| T07 | payload dentro de límites es aceptado | exceso bytes/recursión se rechaza sin efectos |
| T08 | anónimo ve proyección pública mínima | host/slug erróneo no filtra privado ni tenant B |
| T09 | teclado/reduced-motion/320px mantienen interacción | componente no accesible bloquea publicación |
| T10 | rollback autorizado restaura revisión previa preservando historia | rollback sin permiso o corrupción no altera datos |

No declarar 10/10 PASS: hay **10/10 parejas documentadas y 0/10 ejecutadas**. Las comprobaciones aplicables a cambios de código futuros son xUnit backend, pruebas de servidor JWT/RBAC/tenant, Playwright/Chromium para UI y MySQL 8.4 aislado solo si se introduce persistencia.

## 6. A0–A5, impacto y rollback de ESTA entrega R0

- **A0:** scope idéntico al ID 799; diff previsto: este Markdown y una sección completa de CHANGELOG_AI.md, sin cambio de runtime, dependencias o gobierno.
- **A1:** revisión documental de BL16, REV10, LG12, diez parejas P/N trazables a M01.04 (D01–D08) y M01.05 (AC01–AC14/N01–N14); pruebas del builder **NO EJECUTADAS**.
- **A2:** JWT/RBAC/tenant A/B, CSP/URL, privacidad y conflictos de versión continúan fail-closed; ningún cambio operativo en seguridad; pruebas A/B reales **NO_APLICA** al diff exclusivamente Markdown.
- **A3:** comprobación de correspondencia con Standard V5 y contratos de M01.01–M01.05; .NET/Angular/MySQL/HTTP/browser de runtime **NO_APLICA** al cambio documental.
- **A4:** exigir commit fast-forward a `dev`, Scope Lock exact SHA y verificación V5 de procedencia `completed/success`, más PLAN/COLA/DAG/BITACORA readback antes de marcar LISTO; no contar checks CANCELLED/SKIPPED/IN_PROGRESS como PASS obligatorio.
- **A5:** después de LISTO DEV, revisión independiente real por una de las otras cinco Tasks Javier distinta del autor; **A5_PENDIENTE** mientras no exista evidencia y readback.

**Rollback propio:** `git revert` aditivo del commit documental, comprobando HEAD actual, sin force-push. No publicar a otros entornos, ejecutar migraciones, cambiar cuentas, secretos, schedules o propiedad ajena.
