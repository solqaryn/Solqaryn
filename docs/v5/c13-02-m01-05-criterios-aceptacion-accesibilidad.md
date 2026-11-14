# SOLQARYN V5 FINAL — C13-02.M01.05: aceptación verificable de accesibilidad

**Fecha:** 2026-11-14. **Autor responsable:** PRIMARY DEV 04 JAVIER, TASK_ID \`6aeacb7820ac8191b7d768a6f33dfa09\`. **Fuente de cambios:** \`solqaryn/Solqaryn@dev\`; \`PROJECT_SCOPE_LOCK=STRICT\`. **Tipo:** contrato documental M01; ni certificación WCAG ni prueba de navegador ejecutada.

**Autoridad:** [Standard V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). El resultado exigido por C13-02.M01.05 es definir **aceptación funcional y técnica observable**, no implementar accesibilidad. La lista de fuentes de M01.03, de 12 blobs, 14 brechas B01–B14 y ocho pares de pruebas futuras, constituye el inventario inicial. M01.01 define NR01–NR14; M01.02 identifica actores A01–A14, entradas I01–I12, salidas S01–S10 y fronteras F01–F08. Estas fuentes documentales no sustituyen resultados de CI o evaluación humana.

## 1. Condiciones de aceptación transversales

- **AT01 — alcance verificable:** para cada pantalla/flujo se consigna ruta, estado autenticado o anónimo, rol, tenant ficticio, versión/HEAD, navegador, sistema operativo, dispositivo/viewport y método de entrada. Una ruta no muestreada permanece NO_VERIFICADA.
- **AT02 — norma aplicable:** un responsable competente define versión, nivel y criterios WCAG aplicables antes de medir conformidad. Este contrato **no afirma WCAG 2.2 AA certificado** ni inventa obligación legal.
- **AT03 — evidencia doble:** casos automatizables con Playwright/Angular/Vitest se complementan con evaluación humana, teclado y lector de pantalla cuando el criterio no pueda inferirse de un scanner. La presencia de \`.spec.ts\` es fuente, no resultado.
- **AT04 — aislamiento:** las respuestas y cambios de empresa A y B requieren verificación JWT/RBAC/membresía/recurso en .NET; ningún guard Angular, aria-label o selector visual autoriza acceso.
- **AT05 — trazabilidad y falsabilidad:** cada fallo debe poder reproducirse, asociarse a un componente y SHA, tener severidad, expectativa/observado, fix, prueba negativa y rollback. Nunca se llama PASS a SKIPPED, CANCELLED, QUEUED o IN_PROGRESS requerido.
- **AT06 — datos de prueba:** fixtures sintéticos; no exportar secretos, tokens, PII, registros financieros o datos productivos en capturas, logs o artefactos.

## 2. Matriz de aceptación funcional: positivos P01–P14 y negativos N01–N14 (FUTUROS_NO_EJECUTADOS)

| Caso | Positivo esperado y superficie | Negativo que debe detectarse | Evidencia futura, criterio de salida |
| --- | --- | --- | --- |
| P01/N01 | Navegación en ruta pública/privada con Tab, Shift+Tab, Enter y Escape completa acciones esenciales; NR01/NR02 | Un botón sólo mouse o un \`div\` clicable impide operar | Recorrido teclado, foco visible y traza por ruta/HEAD; rechazo si no existe alternativa operable |
| P02/N02 | Enlaces y skip-link alcanzan \`main\`, con título/landmarks coherentes; NR09 | Sin \`main\`, destino de salto inexistente o título duplicado engañoso | Árbol accesible y secuencia de foco antes/después de ruta |
| P03/N03 | MatDialog gestiona foco inicial, navegación interna, Escape y retorno al invocador; NR03 | Modal retiene foco tras cerrar o permite tabular al fondo indebidamente | Video/trace, elemento activo antes/durante/después, sin trampa permanente |
| P04/N04 | Formulario enlaza nombre, label, ayuda y error textual con control; NR04 | Input sin nombre o error comunicado sólo por rojo | Árbol accesible, \`aria-describedby\` donde proceda y reproducción de validación |
| P05/N05 | Loading/éxito/error se anuncian oportunamente sin robar foco; NR05/NR12 | Región cambia sin anuncio o salta arbitrariamente a \`body\` | Observación de lector real cuando proceda y traza de estado asincrónico |
| P06/N06 | Tablas presentan encabezados, región de scroll navegable y contexto de paginación; NR08/NR09 | Tabla mobile es inaccesible por teclado o esconde acción crítica | 390px/1280px, teclado, caption/headers y registro de desbordamiento |
| P07/N07 | Contraste de texto, iconos funcionales y foco se mide por estado y tema; NR06/NR07 | Color único comunica error o tema deshabilitado oculta foco | Mediciones concretas y criterio normativo aprobado, no mera existencia de SCSS |
| P08/N08 | Reflow y zoom conservan controles esenciales en viewports acordados; NR08 | A 320px o zoom acordado se recorta un control de pago/guardar | Captura, zoom real, scroll horizontal y teclado; excepciones justificadas |
| P09/N09 | Imágenes informativas tienen alternativa significativa, decorativas no producen ruido; NR10 | Alt ausente en imagen clave o duplicado redundante | Árbol accesible y evaluación del significado contextual |
| P10/N10 | \`prefers-reduced-motion\` conserva información sin efectos no esenciales; NR11 | Animación forzosa o parpadeo bloquea comprensión | Configuración OS/browser reproducible, prueba de estado y captura |
| P11/N11 | Errores HTTP 401/403/500 ofrecen mensaje seguro, recuperable y accesible; NR12 | Respuesta expone PII/stack o spinner interminable | Test de UI + respuesta API sintética/real pertinente, logs anonimizados |
| P12/N12 | Tenant A sólo recibe su ámbito con permisos vigentes, aun al manipular URL/estado UI; NR13 | JWT A solicita recurso B o rol revocado y obtiene datos B | Prueba backend HTTP con fixtures A/B y denegación, nunca inferida de un guard |
| P13/N13 | Internacionalización conserva \`lang\`, nombres y mensajes por contexto; NR09 | Lector usa pronunciación errónea por idioma marcado incorrectamente | Browser/lector/versiones y snapshot/observación lingüística |
| P14/N14 | El reporte distingue evidencia ejecutada, no aplicable y pendiente; NR14 | Un scanner, test cancelado o esta especificación se registra como conformidad global | Matriz con URL del job, head_sha exacto, \`completed/success\` donde es requerido y NO_VERIFICADO en el resto |

**Interpretación:** todos los P01–P14/N01–N14 son **oráculos FUTUROS_NO_EJECUTADOS**. Los enunciados definen qué debe aceptarse/rechazarse; no implican que actualmente exista defecto ni que una superficie ya pase la prueba.

## 3. Requisitos técnicos no funcionales y criterios de evidencia

| ID | Requisito observable | Método exigido para decidir |
| --- | --- | --- |
| NF01 | Orden lógico de foco al navegar rutas lazy/privadas | Matriz route/rol + Tab/Shift+Tab y foco persistido/restaurado |
| NF02 | Percepción de estados de carga, vacío, validación y 403 | Prueba de estados con teclado y tecnologías asistivas elegidas |
| NF03 | Contraste para normal, hover, focus, disabled y forced-colors | Medición por tema/dispositivo y umbral normativo acordado |
| NF04 | Reflow de tablas, formularios y overlays sin esconder acciones | Viewports reales, zoom y capturas con medidas |
| NF05 | Alternativas semánticas para iconos, imágenes y regiones | Árbol accesible, prueba humana cuando aplique |
| NF06 | Aislamiento tenant A/B al recuperar/reintentar en pantalla accesible | Endpoint .NET autenticado, roles sintéticos y negativo cross-tenant |
| NF07 | Artefactos sin PII/tokens y sin trazas privadas en anuncios | Revisión de logs, capturas, mensajes y escáner de secretos |
| NF08 | Pruebas repetibles con artefactos por SHA de código y fixture | Job GitHub Actions exact-SHA, comando, runner, matriz de fallos y rollback |
| NF09 | Operación ligera sin retirar \`aria-*\`, controles o estados | Comparación bundle/render y regresión de teclado tras optimizar |
| NF10 | No prometer conformidad por documentación o mocks | Reporte separa alcance cubierto, excluido, pendiente y decisión normativa |

## 4. Ejecución futura, alcance y riesgos pendientes

El orden de pruebas futuras es: **(1)** pactar rutas representativas y criterio normativo; **(2)** capturar baseline exact-SHA, viewport, OS/navegador/lector y fixtures; **(3)** probar positivo y negativo por caso; **(4)** documentar defectos con reproducibilidad y tratamiento de PII; **(5)** verificar corrección en la misma superficie con GitHub Actions + evaluación manual según aplique; **(6)** obtener A5 independiente antes de cualquier promoción autorizada.

Las brechas B01–B14 de M01.03 siguen abiertas hasta evidencia causal. Priorizar rutas de autenticación, formularios, tablas financieras, overlays y flujos de tienda/checkout no implica que las demás ya estén verificadas. Definir e implementar la instrumentación con permiso específico en M03–M05; ninguna modificación de frontend, CSS, API, MySQL, Playwright, runner o proveedor forma parte de esta M01.05.

**Impacto:** sólo archivo \`docs/v5\` y sección de \`CHANGELOG_AI.md\`. No cambia Angular 22.2.1, ASP.NET 10, EF/MySQL, DTO, JWT/RBAC, límites tenant, datos, API, integraciones, infraestructura, QA/main/PROD, ni se introducen licencias o servicios de pago.

## 5. Comprobaciones, límites de cierre y reversión

- **A0:** verificar cabeceras, matriz P/N completa, ausencia de secretos, diff sólo Markdown y sección autónoma sin intercalado de CHANGELOG.
- **A1:** cotejo documental P01–P14, N01–N14, NF01–NF10, NR01–NR14, entradas/salidas M01.02 y B01–B14 de M01.03. Este cotejo **no es prueba funcional ejecutada**.
- **A2:** aislar permisos A/B, no revelar PII en errores/logs, conservar backend como autoridad; escenarios P12/N12 permanecen FUTUROS_NO_EJECUTADOS.
- **A3:** coherencia con los tres documentos previos y fuentes Git de M01.03; \`dotnet build\`, \`npm build\`, Playwright, tests de DB y escáner runtime son **NO_APLICA para un diff solamente documental**; no se les atribuye PASS.
- **A4:** publicación directa a dev sólo con fast-forward/compare-and-swap; checks \`verify-solqaryn-project-scope\` y \`V5 - Sin encabezados vacios ni atribucion cruzada\` deben resultar \`completed/success\` sobre el SHA publicado; readback Git, PLAN_MAESTRO, COLA y BITACORA idempotente A:Z.
- **A5:** **PENDIENTE** de revisor con TASK_ID Javier distinto del autor. No libera M01.06, seguridad, JWT/RBAC, QA, main o producción.
- **Rollback:** nuevo commit de \`git revert\` documentado con nueva evidencia y evento compensatorio, nunca force-push ni borrado de historial.

**Conclusión:** criterios M01.05 definidos y falsables; conformidad WCAG, cobertura de pantallas, pruebas navegador/API/MySQL y cualquier despliegue **NO_VERIFICADOS**.
