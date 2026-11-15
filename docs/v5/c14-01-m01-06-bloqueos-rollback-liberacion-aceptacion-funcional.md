# SOLQARYN V5 — C14-01.M01.06: barreras, rollback y condición de liberación de aceptación funcional

**Microtarea:** `C14-01.M01.06` · **Autor:** PRIMARY DEV 05 — JAVIER_CHATGPT, TASK_ID `6adc2e55f47c81919f6374e561687076` · **Fecha:** 2026-11-15 · **Repositorio:** `solqaryn/Solqaryn@dev` · **PROJECT_SCOPE_LOCK=STRICT**.

**Resultado exigido:** cierre **documental preparatorio M01** de aceptación funcional con condiciones de bloqueo, revisión y reversión. `LISTO_DEV` de este archivo no certifica pruebas E2E, endpoints, JWT/RBAC, aislamiento A/B, transacciones, Playwright, MySQL, WCAG, pagos, QA/main/PROD ni autoriza promoción; cada fase posterior requiere su propio gate dinámico, pruebas y aprobaciones. No se crean nuevas Tasks, eventos, trabajadores, dispatcher, API ni migraciones.

## 1. Autoridad, procedencia y situación al reclamo

Fuentes institucionales exclusivas: [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Gobierno: `AGENTS.md`, Skill canónica del HEAD, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_AUTHORITY.md`. Antes del reclamo de la fila 2023: PLAN H=PENDIENTE/I vacío, COLA G/O=SI, DAG del ID exacto F=VALIDADO/G=SI; transición condicional a EN_PROGRESO bajo DEV05 con PLAN y BITACORA leídos posteriormente. Esta evidencia sólo autoriza el documento M01.06.

| ID de preparación | Git blob comprobado en dev | Propósito documental y control |
| --- | --- | --- |
| M01.01 | `12d813a5b3f2838c2b19b8731efacb0e1d5856c6` | 14 invariantes NR, objetivo/no-alcance; LISTO y A5 independiente |
| M01.02 | `865115f359193d7a601b08139f6ed75e322ca71a` | 12 perfiles, 12 interfaces y límites de confianza; LISTO+A5 |
| M01.03 | `fd4e0740aff79c1dbb5368f616c32a8e76f239aa` | 10 fuentes Git y 14 brechas falsables; LISTO+A5 |
| M01.04 | `2e1d24c755c061ed658bf96043e241e16a5fcdbf` | 14 riesgos, 10 supuestos, 10 decisiones ABIERTAS; LISTO+A5 |
| M01.05 | `25d4609233d7131eaba5af311ba607d65a29c6a0` | 16 pares positivos/negativos FUTUROS_NO_EJECUTADOS; LISTO y A5 independiente DEV05 sobre autor DEV02 |

Los cinco archivos están bajo `docs/v5/c14-01-m01-0X-*.md` y conservan el mismo blob en el HEAD observado antes de la publicación. A5 exige un TASK_ID distinto de cada autor, evidencia de diff y check SHA; una revisión de M01.05 no equivale a autoaprobación de M01.06.

## 2. Barreras operativas G01–G16, sin liberación implícita

| Gate | Verificación requerida | Bloqueo cuando falla |
| --- | --- | --- |
| G01 | Identidad SOLQARYN, dev, Skill canónica y Scope Lock STRICT | No publicar ni certificar |
| G02 | PLAN H/I y COLA G/O + DAG F/G coherentes con ID exacto | No reclamar ID ni modificar fórmula |
| G03 | M01.01–05 LISTO+A5 independiente con blobs/versiones verificables | No cerrar M01.06 |
| G04 | Un owner por ID con claim condicional y readback; no escritor simultáneo | Detener transición conflictiva |
| G05 | BITACORA A:Z append-only con EVENT_ID e IDEMPOTENCY_KEY únicos | Rectificar por evento nuevo, sin reescribir |
| G06 | Matriz de alcance NR01–NR14 y actores/entradas de M01.01–02 delimitados | Documentar brecha y origen |
| G07 | Inventario M01.03 F01–F10 distingue fuente de evidencia ejecutada | Rechazar cobertura por existencia de .spec |
| G08 | R01–R14, S01–S10 y D01–D10 siguen con estado potencial/abierto | Prohibido convertir hipótesis en PASS |
| G09 | P01–P16/N01–N16 sólo oráculos de futuros tests; casos no ejecutados | No certificar acceptance |
| G10 | JWT real, RBAC de backend, propiedad/empresa/sucursal y tenant A/B | Detener cualquier cambio funcional sin negativos |
| G11 | Carrito/pedido/pago/factura/reserva requieren idempotencia y pruebas transaccionales | No declarar consistencia por documentos |
| G12 | Frontend requiere accesibilidad, foco, semántica, responsive y métricas realmente medidas | Sin certificado WCAG ni benchmark por plan |
| G13 | Tests de runtime exigen CI pertinente `completed/success` exact-SHA y fixtures sin PII | Cancelled/skipped/queued/in_progress no PASS |
| G14 | Diff sólo este Markdown y CHANGELOG; encabezado seguido de viñetas; Git fast-forward/CAS | Mantener VALIDANDO ante conflicto |
| G15 | `verify-solqaryn-project-scope` y procedencia V5 `completed/success` en SHA publicado | Reparar antes de LISTO_DEV |
| G16 | A5 posterior de otra Task Javier; M02 y releases con nuevos prerrequisitos/A0–A5 | No lanzar QA/main/PROD ni inferir DAG=SI |

**Regla decisoria:** una suite de código o de navegador no ejecutada permanece `NO_EJECUTADA`, y un documento de aceptación no es gate de despliegue. Si una fase posterior demanda ENDPOINT REAL, fixture, base de datos efímera, Playwright o servidor, la Task de aquella fase deberá ejecutar pruebas específicas y comparar el SHA exacto. No hay autorización comercial para pagos reales o proveedores.

## 3. Matriz RB01–RB10 de fallos y reversión

| Riesgo concreto | Medida de recuperación y evidencia |
| --- | --- |
| RB01 — Scope Lock FAILURE | Leer logs exact-SHA, corregir artefacto causal sin desactivar políticas, revalidar |
| RB02 — CHANGELOG intercalado | Preservar encabezado/viñetas contiguos, rehacer blob antes de publicar |
| RB03 — HEAD movido | Recuperar HEAD vivo, recomponer diff aditivo y CAS nuevo; no sobrescribir otros autores |
| RB04 — CAS o owner disputado | Abort claim o publicación, leer PLAN/BITACORA y verificar ausencia de escritor |
| RB05 — CI requerida pendiente | Conservar VALIDANDO y owner, seguir otra microtarea independiente |
| RB06 — CI fallida/cancelada | No tratarla PASS; investigar causa y revalidar sólo si procede |
| RB07 — A5 self-review o falta de blobs | Denegar aprobación y mantener A5_PENDIENTE |
| RB08 — gate DAG NO o dependencia incorrecta | Revisar estado de cada predecesor, no forzar SI |
| RB09 — fuga de token/PII/datos A/B en prueba | Contener, sanear evidencia, exigir fixture sintético y prueba de denegación |
| RB10 — cambio documental defectuoso | `git revert` no destructivo del commit, revalidar Scope Lock y anexar evento compensatorio |

Para reversión de transacciones financieras, reservas o inventario **no se aplicará este rollback Git documental**; cada intervención de datos requiere diseño e integración propia, respaldo, autorización y reversibilidad probada con MySQL efímero antes de operaciones reales.

## 4. Criterios de salida futura A01–A12 (NO_EJECUTADOS)

| Caso | Resultado positivo FUTURO | Fallo negativo FUTURO |
| --- | --- | --- |
| A01 | Visitante anónimo ve sólo productos públicos | Pedido privado visible por URL |
| A02 | JWT válido/membresía activa accede sólo a recurso autorizado | Token expirado/revocado acepta 200 |
| A03 | Tenant A y sucursal propia filtran datos correctamente | Tenant B leído por parámetro, header o body |
| A04 | Permiso backend autoriza acciones según RBAC efectivo | Guard de Angular usado para elevar privilegios |
| A05 | Carrito y pedido procesan retries idempotentes | Dos facturas/pedidos por mismo token |
| A06 | Reserva concurrente preserva disponibilidad | Sobreventa y existencia negativa |
| A07 | Referencia contable y factura son consistentes | Confirmación comercial sin persistencia correlativa |
| A08 | Web 320/390/768/1280 conserva acción, error y foco | CTA oculto o focus trap |
| A09 | Datos de prueba en CI no contienen secretos ni PII | Log/captura divulga JWT/correo de cliente |
| A10 | CI pertinente exact-SHA y negativos completan PASS | Check cancelado/skipped/pending presentado como éxito |
| A11 | Rollback medido restituye contrato sin borrar historial | Revert destruye commits ajenos o evidencia |
| A12 | A5 independiente documentada sin elevar a QA/PROD | Autor se autoaprueba o DEV se despliega |

**A01–A12 son oráculos, no ensayos ejecutados**. Aceptación posterior necesita al menos actor, rol, tenant/empresa/sucursal, ruta, API, fixture A/B, ambiente permitido, navegador/viewport, resultados positivos y negativos, logs saneados, commit SHA, check-runs y reversión. La presencia de pruebas xUnit/Playwright en Git no satisface la ejecución.

## 5. Impacto, controles A0–A5 y siguiente etapa

- **Cambios reales de este scope:** únicamente `docs/v5/c14-01-m01-06-bloqueos-rollback-liberacion-aceptacion-funcional.md` y sección independiente en `CHANGELOG_AI.md`. Sin editar Angular 22.2.1, .NET 10, MySQL 8.4, EF, JWT/RBAC, API, DTO, migración, cuentas, horarios ni eventos.
- **Riesgos de seguridad:** preservar pertenencia/propiedad servidor, aislamiento tenant A/B, no confianza en headers cliente, sin bypass de query filters, sin exportaciones de PII. No se prueba el runtime por editar Markdown.
- **A0 estática:** comprobar G01–G16, RB01–RB10, A01–A12, cinco blobs de predecesores y ausencia de secretos/encabezados vacíos.
- **A1 documental:** comprobar pares positivos/negativos, riesgos y supuestos sin adjudicar PASS funcional. Las pruebas HTTP/E2E/MySQL permanecen `NO_EJECUTADAS`.
- **A2 documental:** revisar que nunca se confunde permiso frontend con JWT/RBAC backend, no se expone otro tenant ni se crean credenciales.
- **A3 documental:** fuentes M01.01–05 y A5 independiente trazadas por PLAN/BÍTACORA, rutas Git y Standard; build frontend/backend/MySQL/Playwright `NO_APLICA` al diff sólo Markdown, no PASS.
- **A4:** publicación dev con parent SHA vivo, fast-forward/CAS, dos archivos Markdown, blobs/diff verificados y Scope Lock+V5 `completed/success` exact-SHA, BITACORA y readback.
- **A5:** revisor Task Javier distinta del autor DEV05; A5_PENDIENTE hasta revisión efectiva; jamás self-review o promoción automática.

**Siguiente etapa:** tras LISTO+A5 de M01.06, reevaluar `C14-01.M02.01` por sus propios PLAN/COLA/DAG y dependencias de fase. Este documento no cambia estados de otras microtareas, no ejecuta pagos ni genera una declaración de conformidad funcional integral.
