# SOLQARYN V17 — C13-02.M01.06: gates, rollback y liberación documental de accesibilidad

**Unidad:** C13-02.M01.06; **PLAN_MAESTRO:** fila 1915; **autor:** PRIMARY DEV 02 JAVIER_CHATGPT, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Fecha:** 2026-11-15; **repositorio:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Fuente: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; calidad V15, pipeline V16 y candado atómico V17.

## Alcance, decisión y prohibiciones

Este contrato R0 define **bloqueos, rollback y condiciones futuras de liberación** para accesibilidad aplicable a interfaz web, teclado, foco, formularios, contraste, reflow, lector de pantalla y estados de carga/error. No modifica Angular, Material/CDK, CSS, rutas, guards, API, EF/MySQL, JWT/RBAC/tenant, datos reales, dominios, servicios, costes, eventos, CI, implementaciones, producción ni infraestructura.

**No certifica WCAG 2.2 AA ni conformidad legal**, no ejecuta lector de pantalla/Playwright/Chromium y no fija un umbral normativo sin decisión competente. `LISTO_DEV` de un Markdown no constituye `A5_PASS`: M02 permanece sujeta a A5 independiente de esta unidad y lectura dinámica de DAG/COLA. Un control accesible de UI no es autorización de backend.

## Predecesores y evidencia causal

Antes del claim, PLAN H1915=PENDIENTE/I vacío, COLA G1915/O1915=SI, DAG_TECNICO F1876=VALIDADO/G1876=SI. El DAG además requiere preparaciones documentales C01/C02/C03/C05/C06/C12 de M01 y `C13-02.M01.01–05+A5`, `C05-01.M01.06+A5`, `C06-04.M01.06+A5`. La fórmula es evidencia de puerta **para esta unidad**, no certifica una suite de UI. Verificar condiciones de nuevo al actuar. Candado V17 por ID y fila: `SQV17_C13_02_M01_06`, namedRangeId `2000001915`, H:I.

| M01 | Documento original | Evidencia y límite |
| --- | --- | --- |
| .01 | `docs/v5/c13-02-m01-01-alcance-no-regresion-accesibilidad.md` | NR01–NR14, objetivos preparatorios |
| .02 | `docs/v5/c13-02-m01-02-actores-interfaces-superficies-accesibilidad.md` | actores A01–A14, interfaces I01–I12 y fronteras F01–F08 |
| .03 | `docs/v5/c13-02-m01-03-inventario-codigo-evidencia-accesibilidad.md` | inventario selectivo de fuentes y B01–B14, no cobertura global |
| .04 | `docs/v5/c13-02-m01-04-riesgos-supuestos-decisiones-accesibilidad.md`, blob `78702fa751d8700a5f4b9126563c9f485f7a409a` | R01–R14 **potenciales**, S01–S10 **NO_VERIFICADOS**, D01–D10 **ABIERTAS**; A5 independiente acreditado |
| .05 | `docs/v5/c13-02-m01-05-criterios-aceptacion-accesibilidad.md`, blob `fb1b7461b6d15d5e587f6397730a03d52be46ca5` | P01–P14/N01–N14 y NF01–NF10 **FUTUROS_NO_EJECUTADOS**; A5 independiente acreditado |

Las fuentes de código `frontend/e2e/c00-03-m04-05-accessibility.spec.ts`, `frontend/src/app/features/centros-costo/centros-costo.a11y.spec.ts` y `frontend/src/styles/_m10-accessibility.scss` son ejemplos de activos existentes, **no evidencias de prueba global ni ausencia de defectos**. Las etapas M01.01–03 se cotejan en PLAN/BITACORA; no extrapolar sus decisiones a certificación normativa.

## Dieciséis gates bloqueantes para una liberación técnica futura

| Gate | Condición verificable | Bloqueo si falta |
| --- | --- | --- |
| G01 | Skill/arquitectura en HEAD, identidad Javier, Scope Lock STRICT | no modificar rama ni certificar |
| G02 | PLAN, COLA, DAG y predecesores+A5 verdaderos por ID exacto | no forzar fórmulas ni reservar otra unidad |
| G03 | Exclusividad V17 por namedRange y CAS H=1 con owner/readback | no competir con escritor, lease o Task activa |
| G04 | Matriz representativa de rutas × permisos × tenant × viewport aprobada | no inferir cobertura total por unas pantallas |
| G05 | Versión, nivel WCAG aplicable, excepciones y autoridad fijadas | no declarar WCAG AA o obligación legal |
| G06 | Secuencia Tab/Shift+Tab/Enter/Escape y foco visible por ruta | no aceptar control sólo mouse |
| G07 | MatDialog/menus/overlays restauran y contienen foco correctamente | no aceptar trampa permanente ni fondo activo |
| G08 | Labels, ayuda, errors y estados aria pertinentes por control | no comunicar error sólo por color |
| G09 | Tablas, scroll y zoom/reflow a 320/390/768/1280 sin ocultar acciones | no acreditar mobile con una captura |
| G10 | Contraste por tema, estado, foco y forced-colors con medidas | no inferir contraste desde variables SCSS |
| G11 | Reduced-motion, idiomas/landmarks, imágenes, alternativas y orden lector | no prometer lector real desde scanner |
| G12 | Loading/empty/success/403/500 accesibles sin PII ni foco errante | no mostrar éxito falso o spinner eterno |
| G13 | API .NET aplica JWT/RBAC/membresía/tenant A/B y revocación, no sólo guard Angular | rechazar bypass de permisos y fuga cross-tenant |
| G14 | Pruebas Angular/Vitest, Playwright/Chromium y evaluación humana cuando proceda, por SHA | SKIPPED/CANCELLED/IN_PROGRESS nunca PASS |
| G15 | Cambio de código: CI pertinente, exact SHA/blobs, Scope Lock/V5 y regresión de seguridad | no reemplazar pruebas runtime con documento |
| G16 | A5 por Task Javier distinta del autor revisa código/evidencia, criterios y límites | LISTO_DEV sin A5 no abre M02 |

G04–G15 son obligaciones futuras; **ninguno se declara satisfecho técnicamente en M01.06**. Si una superficie crítica impide navegación, la prueba futura debe producir FAIL con reproducción, no forzar un PASS. Las métricas sin numerador/denominador verificado son `NO_MEDIDO`.

## Niveles de aceptación y matrices futuras

**Nivel R0 M01.06 (actual):** A0 alcance y cinco contratos; A1 revisar la correspondencia R14/S10/D10 con los 16 gates y casos P/N; A2 aislar seguridad tenant/PII y dependencia de autorización server-side, sin afirmar resultados; A3 .NET/EF/MySQL/Angular/Playwright/lector de pantalla **NO_APLICA** a un diff limitado a este Markdown y una sección autocontenida del CHANGELOG, sin convertir NO_APLICA en PASS; A4 commit fast-forward/CAS en dev, Scope Lock y control V5 `completed/success` de SHA causal, BITACORA y readback.

**A5 independiente (posterior):** otra Task Javier con TASK_ID distinto valida procedencia, contenido, calidad, debilidades, tests futuros y controles; puede registrar FAIL. Ninguna revisión documental acredita WCAG o una suite browser que no se ejecutó.

**Nivel M02 posterior:** sólo elegir un ID con PLAN H=PENDIENTE/I vacío, COLA G/O=SI y DAG F=VALIDADO/G=SI. Implementaciones M03/M04 y cualquier UI requieren Angular 22.2.1 build/tests, Chromium y evaluación de teclado/foco, seguridad .NET A/B y rollback según alcance. No despliegue QA/main/PROD por el cierre de este contrato.

| Escenario futuro | Positivo a demostrar | Negativo a detectar |
| --- | --- | --- |
| P01/N01 | teclado activa navegación pública/privada y foco visible | control esencial sólo mouse |
| P02/N02 | skip-link y landmarks conducen a main correcto | destino inválido o título ambiguo |
| P03/N03 | diálogo restaura foco tras Escape/cerrar | trampa o foco perdido |
| P04/N04 | input tiene nombre y error textual asociado | error sólo rojo o control anónimo |
| P05/N05 | loading/error/403 se anuncian sin robar foco | spinner eterno o estado silencioso |
| P06/N06 | tabla permite lectura y acciones con scroll/zoom | acción crítica fuera de foco/viewport |
| P07/N07 | contraste medido por tema/estado/forced-colors | foco invisible/contraste insuficiente |
| P08/N08 | 320px/zoom conserva funciones y feedback | control cortado por reflow |
| P09/N09 | alt de imagen informa, decorativa no interfiere | ausencia/redundancia no justificada |
| P10/N10 | reduced-motion mantiene contenido sin efecto | animación obligatoria con movimiento dañino |
| P11/N11 | error HTTP redactado con recuperación accesible | secretos/PII/stack en mensajes |
| P12/N12 | tenant A sólo obtiene recurso A server-side | JWT A/rol revocado lee datos B |

Todos los 12 pares de esta tabla son **FUTUROS_NO_EJECUTADOS**. La matriz original de M01.05 contiene 14 pares que también siguen sin ejecución. Para la futura certificación cada caso debe tener entorno/OS/browser/lector, fixture, rol, empresa, SHA, pasos/esperado/observado, evidencia redactada y fallo reproducible. Ninguna prueba automatizada acredita por sí sola toda WCAG.

## Rollback, límites y continuidad

1. **Error R0 documental:** reparar/revertir mediante `git revert` normal más bitácora aditiva; no reescribir historial ni forzar rama.
2. **Conflicto V17 de propiedad:** CAS fallido o lock ya existente = NO_GO sólo a ese ID; no borrar namedRange incierto ni reclamar WIP adicional.
3. **Defecto de teclado o accesibilidad real futuro:** bloquear promoción del componente afectado, registrar ID/ruta/SHA/severidad, corregir y repetir prueba humana y automatizada.
4. **Fuga tenant/JWT/RBAC/PII futura:** aplicar bloqueo fail-closed backend, no aflojar permisos por UI, reproducir aislamiento A/B y no exponer datos reales en artefactos.
5. **Regresión responsive/contraste/motion:** revertir sólo cambio compatible, conservar estados y navegación útil, registrar medida real y aceptación por autoridad pertinente.
6. **Pruebas CI:** requerida QUEUED/IN_PROGRESS mantiene VALIDANDO con run/SHA/owner, FAILED exige reparación, CANCELLED/SKIPPED nunca PASS; equivalencia de descendiente sólo con blobs/pruebas equivalentes.
7. **Dependencias técnicas:** M02/M03 no se habilitan por VALIDANDO provisional, A5 pendiente o una promesa de test; DAG y COLA calculan sin `SI` manual.
8. **Seguridad del proyecto:** no publicar en QA/main/PROD, tocar Aiven/datos, instalar proveedores o crear Tasks, runners, webhooks, secretos y schedulers desde esta unidad.

**Resultado de esta microtarea:** documentación de cierre preparatorio de accesibilidad, no una suite WCAG aprobada. El futuro M02 requiere A5 independiente y elegibilidad calculada, y la certificación UI futura necesita pruebas reales y revisión competente.
