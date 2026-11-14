# SOLQARYN V5 — C05-04.M01.06: bloqueo, rollback y salida documental del lote de ligereza

**Unidad:** `C05-04.M01.06`; PLAN_MAESTRO fila 835, orden 834. **Autor:** SOLQARYN PRIMARY DEV 02 JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`; **fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; **repositorio:** `solqaryn/Solqaryn@dev`; Scope Lock STRICT; fecha 2026-11-14.

## Resultado atómico y límites de liberación

Esta unidad **cierra contractualmente la preparación M01** del lote `C05-04 — Ligereza y percepción`, definiendo bloqueo, vuelta atrás, evidencia y el handoff para analizar M02 **solo si su DAG individual y permisos autorizan**. El objetivo futuro incluye reducir carga inicial, densidad visual, solicitudes duplicadas y ruido percibido manteniendo accesibilidad, desempeño y aislamiento multiempresa. No habilita directamente M03–M06, builds productivos, modificaciones Angular, HTTP, migraciones, CRM, IAM/JWT/RBAC, cambios de tenant ni QA/main/PROD.

**Estado de prueba real:** la configuración Angular declara budgets `initial` warning 650 kB/error 750 kB y `anyComponentStyle` warning 16 kB/error 32 kB; **no se midió un bundle nuevo**, Lighthouse, LCP/INP/CLS, flujo HTTP, cachés A/B, estabilidad o métrica RUM en M01.06. Las propuestas se mantienen sin certificación de resultados. Ninguna decisión D01–D08 pendiente se considera aprobada por esta entrega.

## Predecesores M01.01–M01.05 verificados y A5 independiente

| Antecesor | Estado institucional y procedencia Git | A5 y conclusión acotada |
| --- | --- | --- |
| C05-04.M01.01 — objetivo/NR01–NR12 | PLAN fila 830 LISTO, doc blob `a9b7219c7094b8b162f53117a2c67d2c86dd7c21`; SHA `2c25254b4be14442b7eb5c443be1a136b440a0c0` | A5_PASS por Task J00 diferente a autora J50; documento, sin benchmark |
| C05-04.M01.02 — actores/interfases I01–I12 | PLAN fila 831 LISTO, doc blob `93efea304f25ea098f73f78b2502fe62be054a8c`; SHA `c4cc3d8699282b50eaabe539849dd4c09fb83c9f` | A5_PASS J10 sobre fuente de J00, sin prueba HTTP/E2E |
| C05-04.M01.03 — inventario/G01–G10 | PLAN fila 832 LISTO, doc blob `a20753ad76388ec48fe766af54bf07a2456edc8e`; SHA `3a6ddb020a8a4807752881cf26445412a168f42a` | A5_PASS J10 sobre fuente de J00, no certifica CWV |
| C05-04.M01.04 — R01–R12/S01–S08/D01–D08 | PLAN fila 833 LISTO, doc blob `8554efc8ccf82ca4c48282eb5f4ab61288a2e4b1`; SHA `4fca987fdb9e856576dd2b44d35b610b409e6c7b` | A5_PASS J10 sobre fuente de J00, decisiones siguen PENDIENTES |
| C05-04.M01.05 — aceptación AC01–AC14/N01–N14 | PLAN fila 834 LISTO, doc blob `6b9293d1435ab148a2afdf6b0b2a63455e92b05e`; SHA `c5c12d27a1a03ecfb600ebf4c93759a2acecb897` | A5_PASS J10 sobre fuente de J00, pruebas de runtime FUTURAS NO_EJECUTADAS |

Las cinco revisiones A5 se acreditan **independientes de la Task autora de cada antecedente** y delimitadas a sus blobs/documentos originales; no constituyen permisos comerciales ni pruebas de runtime. Las fuentes y evidencias permanecen auditables en PLAN/COLA/BITACORA, sin alterar BITACORA histórica. No se atribuyen pruebas a la cuenta ajena ni se autoaprueba esta propia M01.06; su A5 queda pendiente de otra Task Javier.

**Gate individual de esta unidad:** `PLAN_MAESTRO!A835=C05-04.M01.06`, previo H=PENDIENTE/I vacío; `COLA!G835=SI/O835=SI`; `DAG_TECNICO!A796=C05-04.M01.06/F796=VALIDADO/G796=SI`, dependencias C01/C02/C05 M01 preparatorias y M01.01–05+A5. Es una autorización **solo documental** para esta unidad, no una liberación automática de tareas posteriores.

## Diez controles de salida G01–G10

| ID | Evidencia exigida antes de habilitar trabajo posterior | Condición NO_GO, incluso con DAG SI |
| --- | --- | --- |
| G01 | Scope Lock exact SHA de la propia publicación | Check ausente, failed, cancelled, skipped o en progreso |
| G02 | Documentos M01.01–05 íntegros y A5 por otra Task | Cualquier autor se revisa a sí mismo o fuente no coincide |
| G03 | Línea base Angular de rutas, estilos y preloading inspeccionada | Tratar configuración budget como tamaño real medido |
| G04 | Matriz R/S/D documentada sin aprobación implícita | Asumir que un D pendiente autoriza refactor, RUM o proveedor |
| G05 | Plan de medición reproducible con SHA, navegador/red y fixture | Afirmar mejora LCP/INP/CLS/HTTP sin mediciones comparables |
| G06 | Seguridad JWT/RBAC/tenant A/B conservada por diseño | Cache compartida A/B, quitar guardia o confiar en tenant del cliente |
| G07 | Accesibilidad: teclado, 320px, ARIA, foco, zoom y reduced-motion | Publicar una UI sin verificaciones browser proporcionales |
| G08 | Privacidad de RUM y logs, secretos fuera de repositorio | Transmitir JWT/PII o insertar trackers sin permiso |
| G09 | Plan de rollback documentado que no reescriba historia | Rollback sin prueba de regresión causal o force-push |
| G10 | Control A0–A4 completo, readback PLAN/COLA/BITACORA, A5 independiente posterior | M01.06 marcado LISTO sin pruebas dirigidas o auto-A5 |

G01–G10 son **puertas documentales a validar por la etapa**; no representan diez tests E2E ejecutados ni diez funcionalidades desplegadas. Un fallo real de autorización, tenant o datos mantiene el NO_GO de esa implementación y exige reparación causal. Para M02, el contrato específico y sus estados derivados deben comprobarse después de esta unidad, sin sobrescribir autoría ni inventar predecesor LISTO.

## Handoff gobernado para M02: trabajos permitidos y bloqueados

**Permitido evaluar en M02**, si el plan y DAG vigente lo habilitan con CAS: especificación de presupuestos de bundle, objetivos de Core Web Vitals medibles, carga por ruta, cancelación de requests, feedback accesible, caché segregada, estados UI, telemetría sin PII, API de métricas si realmente se autoriza y compatibilidad por versión. El diseño no sustituye la implementación ni crea tareas adicionales.

**Bloqueado por M01.06:** cambiar producción, rutas protegidas, JWT/RBAC, modelo MySQL, tokens, Cloudflare/SDK, secretos, pagos, telemetría exterior, QA/main/PROD, escalamiento, event dispatcher, Workers, migraciones o servicios pagos. Estos requieren unidades M03/M04/M05 y autorizaciones explícitas con CI .NET/Angular/MySQL/Playwright causal. No usar una CI general `cancelled/skipped/in_progress` como PASS.

**Pruebas futuras positivas:** P01 build reproduce bytes/chunks bajo SHA; P02 trazas LCP/INP/CLS comparan baseline; P03 transición tenant A→B elimina estado privado A; P04 320px/foco/ARIA/zoom; P05 HTTP con 403/500 correcto; P06 revert de optimización mantiene funcionalidad.

**Pruebas futuras negativas:** N01 caché A visible en B; N02 guardias omitidas para aumentar velocidad; N03 error 403 oculto como empty; N04 bundle/CLS sin medición real; N05 precarga privada en público; N06 requests duplicados no detectados; N07 inyección de tenant por header; N08 foco invisible/trap; N09 error 320px/zoom; N10 movimiento obligatorio; N11 RUM con JWT/PII; N12 rollback destructivo; N13 permiso RBAC ampliado; N14 una publicación sin CI exacta. Estos escenarios **NO_EJECUTADOS** solo fijan aceptación futura.

## Reversibilidad, pruebas y cierre

**Rollback documental inmediato:** ante error de alcance, procedencia o autorización, conservar estado `VALIDANDO` y emitir reparación por commit normal o `git revert` de la unidad, sin force-push, sobrescribir trabajo ajeno ni tocar BITACORA histórica. Para cambios ejecutables futuros, exigir antes/después de tiempos/bytes, filtros A/B, acuse de datos, CI causal y regresión tras restaurar flag/version con autorización. No se sugiere revert de datos productivos.

**A0:** plan y cinco A5 previos, fuentes originales y dos Markdown de diff, sin secretos. **A1:** G01–G10, reglas de rollback y P06/N14 FUTUROS documentados, sin declarar métricas. **A2:** límites tenant A/B, JWT/RBAC, privacidad, accesibilidad y permisos no modificados. **A3:** documentos M01.01–05 con blobs y matriz R/S/D contra DAG dinámico y arquitectura Angular 22.2.1/.NET 10/MySQL 8.4. **A4:** fast-forward CAS a dev, `verify-solqaryn-project-scope` y V5 CHANGELOG `completed/success` del SHA exacto, readback H/I + COLA E/I + BITACORA A:Z. Builds Angular/Node, .NET/MySQL, Playwright son **NO_APLICA para cambio exclusivo Markdown**, no PASS funcional. **A5_PENDIENTE:** otra Task Javier distinta, sin atribución propia; no QA/main/PROD.

La única salida de esta microtarea es un contrato de gates preparado para M02, **no** una mejora de rendimiento certificada.
