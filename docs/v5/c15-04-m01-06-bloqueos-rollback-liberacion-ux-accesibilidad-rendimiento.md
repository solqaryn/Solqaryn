# SOLQARYN V5 FINAL — C15-04.M01.06: bloqueos, rollback y salida documental UX, accesibilidad y rendimiento

**Autor:** PRIMARY DEV 01 JAVIER (`6adbb9fff1548191a1bd5b30d9e9984a`). **Repositorio:** `solqaryn/Solqaryn@dev`. **Scope Lock:** STRICT. **Clase:** cierre de PREPARACIÓN DOCUMENTAL M01; NO certificación WCAG, rendimiento medido, Playwright ejecutado ni nuevas pantallas implementadas.

**Autoridad:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), más Skill canónica, AGENTS y PROJECT_SCOPE_LOCK. No reutilizar backlog archivado.

## 1. Prerrequisitos y condición de salida de M01

| ID preparatorio | Estado y revisión independiente leídos en PLAN antes de reclamar esta unidad | Qué permite |
| --- | --- | --- |
| C15-04.M01.01 | LISTO, A5 de Task Javier :30 sobre `eb4cf6f9263aad255b23388c54b41d44d1b3928d` | 14 invariantes NR y ocho P/N FUTUROS |
| C15-04.M01.02 | LISTO, A5 de Task Javier :30 sobre `aa3f535258f26dbf95e2eb18cdd6d3b7e05041a7` | Actores, IO y diez fronteras conceptuales |
| C15-04.M01.03 | LISTO, A5 de Task Javier :30 sobre `5a07fb176fefe38b4f5026cef42fea30356ac708` | Doce fuentes/brechas documentadas, ninguna medición |
| C15-04.M01.04 | LISTO, A5 distinta de su autora Task :00 sobre `949e0073e7cb4e9c8d24cb879d39a5635274ee3c` | R01–R12, S01–S08 y D01–D08 como hipótesis |
| C15-04.M01.05 | LISTO, A5 distinta de su autora Task :00 sobre `592879366935e65140d8b3693a8b3cd0dfdbbf03` | AC01–AC16 y diez RNF FUTUROS, sin ejecución |

Antes del CLAIM se exigieron PLAN H=PENDIENTE/I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI, Scope Lock exact-SHA SUCCESS y readback CAS de owner. Es una prueba de elegibilidad, **no** de calidad de UI. La salida M01.06 habilita solamente a **reevaluar M02** con su propia elegibilidad, autoría y pruebas; no libera automáticamente M03, QA, main, producción, migración o cambios de dominio.

## 2. Doce condiciones de bloqueo de aceptación posterior

| Gate | Bloqueo que debe prevalecer | Evidencia necesaria para liberarlo |
| --- | --- | --- |
| B01 — Alcance | Página, builder o storefront todavía no implementado se declara probado | Inventario de rutas reales, feature flag y evidencia de existencia |
| B02 — Viewport | Acción o contenido crítico queda oculto a 320 px, zoom o texto largo | Browser real con capturas y bounding boxes sin solapamiento |
| B03 — Teclado | Navegación mouse-only, salto en tabulación o foco invisible | Tab, Shift+Tab, Enter y Espacio en rutas implementadas |
| B04 — Diálogos | Foco atrapado o perdido tras apertura y cierre | Traza de foco antes/durante/después con componente y viewport |
| B05 — Formularios | Error sólo visual/color, sin etiqueta o ayuda accesible | Asociación semántica y mensajes verificables en árbol accesible |
| B06 — Iconos/imágenes | Acción sin nombre o imagen crítica sin alternativa | Auditoría de nombre, role y texto equivalente aplicable |
| B07 — Movimiento | Animación forzada pese a reduced-motion o estado no perceptible | Comparación con prefers-reduced-motion y flujo preservado |
| B08 — Estados de red | Spinner infinito, error mudo, reintento sin límite ni foco recuperable | Fixture de red degradada, timeout y recuperación finita |
| B09 — Temas y tenants | Token de color ilegible, branding o cache B aparece para A | Contraste medido y fixture tenant A/B con backend autorizado |
| B10 — Rendimiento | LCP/INP/CLS, tamaño de bundle o latencia afirmados sin baseline | Red/dispositivo, muestras, percentiles, tooling y SHA exacto |
| B11 — CI / A5 | skipped, cancelled, queued, failure, SHA anterior o autor propio cuentan PASS | Check-run completed/success exact SHA y revisor distinto Javier6 |
| B12 — Entorno | LISTO_DEV documental interpreta permiso QA/main/PROD o despliegue | Autorización nueva, pruebas causales y rollback por ambiente |

**Los B01–B12 son puertas y escenarios FUTUROS_NO_EJECUTADOS, no defectos confirmados.** Las doce brechas G01–G12 y doce riesgos R01–R12 conservan sus estatus NO_ACREDITADO/NO_REPRODUCIDO. Los dieciséis pares AC01–AC16 y los diez RNF de M01.05 permanecen sin ejecución/medición. No existe certificación WCAG o de rendimiento por listar pruebas.

## 3. Reglas de evidencia, seguridad y pruebas

Cuando M02/M03 autoricen trabajos ejecutables, usar Angular 22.2.1, Material/CDK 22.2.1, Signals y Zone.js 0.16.3 según arquitectura vigente, con Playwright/Chromium en GitHub Actions del **SHA exacto** para casos de UI. Cada caso registra ruta realmente implementada, navegador, viewport, zoom, teclado, motion, contexto de red, fixture tenant, esperado, observado, captura/traza, estado y conclusión de CI, severidad probada, responsable y rollback. Las funciones no implementadas se registran `NO_APLICA_FUNCIONALIDAD_NO_EXISTENTE`, no PASS.

Nunca confiar en un guard Angular, slug, parámetro, token de tema o caché como autorización: JWT/RBAC/tenant A/B se autorizan realmente en backend, con negativos HTTP cuando la fase de código lo requiera. Pruebas del frontend no sustituyen persistencia MySQL ni seguridad backend. No utilizar datos reales, secretos, configuración de producción, servicios de pago, DNS ni proveedores aún no activados.

Los casos `queued`, `in_progress`, `cancelled`, `skipped`, `failure` no son PASS requerido. Para artefactos sólo Markdown, compilación, Playwright, MySQL y HTTP son `NO_APLICA_DOCUMENTAL` motivado, no éxito. La revisión A5 debe pertenecer a otra Task Javier distinta de la autora, con blob y SHA originales.

## 4. Contención y rollback

Si cambia el HEAD antes de publicar, releer y reconstruir un nuevo árbol a partir del HEAD vigente con CAS único; no force-push, reset ni overwrite de historia. Si Scope Lock o el verificador V5 no tienen `completed/success` exact SHA, mantener sólo esta microtarea en VALIDANDO y seguir una independiente. Si el check obligatorio falla, registrar fallo causal y no marcar LISTO. Rollback documental exclusivamente con `git revert` normal, acompañado de nuevo commit y readback; BITACORA A:Z es append-only.

Una excepción real de accesibilidad o de rendimiento requiere evidencia reproducida antes de clasificar P0/P1, corrección causal, repetición de prueba y revisión independiente. Ninguna condición aquí despliega nuevas aplicaciones ni introduce workers, event dispatcher, migraciones EF/MySQL, Cloudflare/R2, QA/main/PROD.

## 5. Auditorías A0–A5 de esta unidad

- **A0:** fuente V5 y cinco antecedentes M01.01–05 con A5 separados, B01–B12, Scope Lock y ausencia de claims de mediciones.
- **A1:** positivos/negativos futuros y condiciones de contención documental consistentes; tests de navegador NO_EJECUTADOS.
- **A2:** delimitación entre UI y autorización backend JWT/RBAC/tenant A/B; no cambios de seguridad runtime, no PASS HTTP.
- **A3:** integración de contratos NR, IO, G, R y AC con fuentes originales; compilación y navegador NO_APLICA a dos Markdown.
- **A4:** fast-forward serializado con CAS dev, diff sólo documento y CHANGELOG_AI.md sección íntegra, checks Scope Lock y V5 `completed/success` SHA exacto y readback PLAN/COLA/BITACORA.
- **A5:** PENDIENTE de otra Task Javier con TASK_ID distinto; no autoaprobación ni promoción.

**Rollback:** `git revert` normal del SHA publicado, sin alterar el historial de auditoría ni otras tareas.
