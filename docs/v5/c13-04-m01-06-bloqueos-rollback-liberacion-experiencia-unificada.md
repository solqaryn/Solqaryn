# SOLQARYN V5 — C13-04.M01.06: bloqueos, rollback y liberación documental de experiencia visual unificada

**Microtarea:** `C13-04.M01.06`. **Autor:** PRIMARY DEV 05 — JAVIER_CHATGPT, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Fecha:** 2026-11-15. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

**Alcance:** establecer condiciones verificables para concluir **solamente la preparación documental M01** sobre experiencia visual, accesibilidad, estados y responsive. El documento no implementa diseño visual, HTML/SCSS, roles, JWT/RBAC, tenancy, interfaz, backend, migraciones, workers, eventos, webhooks, proveedores ni QA/main/PROD. `LISTO_DEV` de este contrato y A5 documental **no certifican conformidad WCAG, pruebas E2E, aislamiento A/B efectivo ni habilitan por sí solos M02**: el siguiente ID exige su propia elegibilidad real.

## 1. Autoridad y condiciones del reclamo

Autoridad ejecutable única: [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO, COLA, DAG_TECNICO y BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, Skill canónica y `docs/AUTOMATION_STATUS.md`. Al reclamar el ID: PLAN fila 1987 H=PENDIENTE, I sin owner; COLA G/O=SI; DAG fila 1948 F=VALIDADO/G=SI; actualización condicional a EN_PROGRESO bajo Task DEV 05, con lectura posterior de PLAN y COLA y evento de BITACORA. Estos son hechos del reclamo; los checks posteriores se verifican por SHA publicado y no por el texto de este plan.

### Predecesores documentales y procedencia Git

| ID | Artefacto | Blob Git | Estado y límite |
| --- | --- | --- | --- |
| M01.01 | [Objetivo, no-regresión](./c13-04-m01-01-alcance-no-regresion-experiencia-unificada.md) | `fedf00f157e9d14e9150c48601070e84b0306ff3` | NR01–NR14, seis superficies, A5 independiente; sin pruebas de UI |
| M01.02 | [Actores, entradas y salidas](./c13-04-m01-02-actores-entradas-salidas-superficies-ui.md) | `f7c75a75c8c724af54527a1eccfdd1c78a0994e9` | 12 actores, 12 interfaces, 8 fronteras; A5 independiente |
| M01.03 | [Fuentes y brechas](./c13-04-m01-03-inventario-codigo-brechas-coherencia-visual.md) | `f02a82a632b6abb6f35f83a22cb8e7fe834bb573` | 14 referencias, 12 brechas hipotéticas; A5 independiente |
| M01.04 | [Riesgos y supuestos](./c13-04-m01-04-riesgos-supuestos-decisiones-experiencia-unificada.md) | `5e49c4eeb5575b0960837535105a279305bff4b1` | 14 riesgos, 10 supuestos, 10 decisiones abiertas; A5 independiente |
| M01.05 | [Criterios observables](./c13-04-m01-05-criterios-aceptacion-experiencia-unificada.md) | `c6a3070256c216c49c4c827c6b0ce4b0587cf548` | 16 pares positivos/negativos y diez NF futuros; A5 independiente |

PLAN y BITACORA identifican revisores entre las otras cinco Tasks Javier, distintos de cada autor. En particular M01.04 recibió revisión de DEV05 distinta del autor DEV04; ello no habilita autoaprobación de la presente M01.06. Los cinco artefactos se leen como contratos, nunca como resultados automáticos de accesibilidad, desempeño o seguridad.

## 2. Barreras de salida G01–G16

| Gate | Requisito verificable antes de un avance futuro | Si falta |
| --- | --- | --- |
| G01 | Repo `solqaryn/Solqaryn`, `dev`, Skill canónica vigente y Scope Lock STRICT | NO_GO de escritura/publicación |
| G02 | ID exacto en STANDARD; PLAN H/I, COLA G/O, DAG F/G sin discrepancia | No reclamar ni forzar fórmulas |
| G03 | M01.01–05 LISTO con revisiones A5 de Task distinta y blobs preservados | No cerrar M01.06 documental |
| G04 | Propiedad exclusiva por ID, evidencia y readback; sin escritor concurrente | Detener cambio de estado |
| G05 | BITACORA aditiva A:Z con EVENT_ID e IDEMPOTENCY_KEY únicos por transición | Corregir por nuevo evento, sin borrar historia |
| G06 | NR01–NR14 y actores/entradas de M01.01–02 trazados a superficies reales | Mantener brecha documentada |
| G07 | Inventario F01–F14 y brechas B01–B12 con blob, SHA y alcance declarados | Revalidar inventario |
| G08 | Riesgos R01–R14, supuestos S01–S10 y decisiones D01–D10 siguen sin afirmar cierre | No convertir hipótesis en certificado |
| G09 | P01/N01–P16/N16 y NF01–NF10 son escenarios futuros, no pruebas ejecutadas | Rechazar PASS inventado |
| G10 | Privacidad de capturas/logs, control tenant A/B, guard y permiso backend separados | Detener publicación insegura |
| G11 | Matriz ruta/rol/tenant/estado/viewport/resultado accesible/SHA será exigible en implementación | No conceder aprobación UX por texto |
| G12 | `CHANGELOG_AI.md`: encabezado con viñetas contiguas, sin encabezados vacíos | Corregir procedencia |
| G13 | Commit `dev` fast-forward/CAS, diff restringido a este Markdown y CHANGELOG; rollback viable | Abortar si HEAD cambió o hay conflicto |
| G14 | Checks `verify-solqaryn-project-scope` y V5 CHANGELOG `completed/success` del SHA exacto | Permanecer VALIDANDO |
| G15 | A5 independiente posterior por otra Task Javier distinta de DEV05, con diff, blobs, CI y BITACORA | A5_PENDIENTE, no QA/main/PROD |
| G16 | M02.01 y cualquier M03/M04/M05/M06 revisan **su propio** DAG/PLAN/COLA, seguridad y pruebas causales | No liberar sucesores artificialmente |

**Decisión de salida:** M01.06 puede alcanzar `LISTO_DEV` sólo tras A0–A4 documentales, publicación y readback; la revisión A5 requiere otro TASK_ID. No se supone que completar M01.06 cierre brechas de UI, cambie el estado de documentación de otras unidades o autorice despliegues.

## 3. Recuperación y reversión RB01–RB10

| Riesgo | Acción mínima y comprobación de seguridad |
| --- | --- |
| RB01 — SHA Scope Lock FAIL | Leer logs exactos, corregir Markdown sin desactivar verificador y emitir nuevo SHA |
| RB02 — HEAD avanzado | Releer archivo/CHANGELOG, reconstruir diff sobre el HEAD nuevo y CAS fast-forward |
| RB03 — owner ocupado | No escribir sobre scope; releer PLAN/BITACORA y continuar trabajo ajeno no conflictivo |
| RB04 — CI en ejecución | Mantener VALIDANDO y owner, no contar PASS, continuar otra candidata independiente |
| RB05 — CI cancelada/skipped/fallida | Separar check causal de no causal; no convertirlo en PASS |
| RB06 — A5 propia o ajena no verificable | Rechazar autoaprobación, no liberar sucesoras |
| RB07 — inconsistencia DAG/COLA | Identificar predecesor y estado; nunca cambiar manualmente G/O a SI |
| RB08 — PII, secretos o fuga A/B en evidencia | Contener, redactar y volver a comprobar; no usar capturas reales |
| RB09 — evento duplicado en BITACORA | Reconciliar idempotencia mediante evento nuevo compensatorio, sin edición histórica |
| RB10 — documento mal publicado | `git revert` normal del commit objetivo después de revisar commits concurrentes, sin force-push |

**Rollback de este cambio:** revertir únicamente el commit documental mediante reversión Git aditiva y nuevo control exact-SHA, registrar corrección en BITACORA y verificar PLAN/COLA; nunca reset destructivo. Las acciones correctivas futuras sobre Angular o backend requieren su propio owner y CI dirigido.

## 4. Aceptación futura: doce pares de oráculos NO_EJECUTADOS

| Escenario | Positivo futuro | Negativo que debe bloquear |
| --- | --- | --- |
| P01/N01 | Login anuncia validación con foco | Campo error sin label ni foco |
| P02/N02 | Tienda y dashboard identifican CTA y jerarquía | Acción esencial oculta |
| P03/N03 | Loading/empty/error anuncian estado y recuperación | Spinner indefinido o estado sólo cromático |
| P04/N04 | Reflow 320/390/768/1280 conserva acciones | Scroll global oculta botón |
| P05/N05 | Tabla/modal mantiene selección y retorna foco | Trap o acción perdida |
| P06/N06 | Cambio tenant A→B limpia tema y datos y backend aísla | Se muestra dato/tema de A en B |
| P07/N07 | Ruta protegida y RBAC backend coinciden | Menú/guard concede permiso |
| P08/N08 | Teclado y lector anuncian controles correctamente | Nombre accesible ausente |
| P09/N09 | Reduced-motion no afecta información | Animación indispensable |
| P10/N10 | Contraste y forced-colors medidos en estado real | Afirmación WCAG por SCSS |
| P11/N11 | Bundle y métricas LCP/INP/CLS medidos con SHA | Budget configurado presentado como medición |
| P12/N12 | Evidencia CI exact-SHA y fixtures A/B saneados | Check CANCELLED/SKIPPED/QUEUED/FAIL denominado PASS |

Estos escenarios se documentan para fases futuras, **no se ejecutaron** en esta microtarea. Aprobación futura exige oráculo y resultado por ruta, pantalla, actor, rol, tenant sintético, estado, viewport, browser/zoom, SHA, prueba positiva/negativa, seguridad, trazas sin PII y rollback. Una fuente `.spec.ts` sin ejecución tampoco equivale a PASS.

## 5. Impacto técnico y pruebas A0–A5

- **Frontend, acceso y UX:** sólo contrato Markdown; Angular 22.2.1, Material/CDK, tokens, menú, guards, CSS y componentes existentes permanecen intactos. Usuarios de teclado/lector, soporte y equipo visual son consumidores futuros, no permisos nuevos.
- **API, datos, seguridad:** ninguna clase .NET 10, DTO, entidad EF Core/MySQL, query, JWT/RBAC, tenant, servicio externo ni migración; el backend conserva autoridad. No consultar/modificar datos de empresas A/B reales.
- **Integraciones/observabilidad:** no crear jobs, tracking, dispatcher, webhooks, infra ni entradas de Tasks; evidencia y telemetría deben distinguir último autorreporte de ejecución nativa.
- **A0:** revisar alcance, integridad de cinco blobs, G01–G16, RB01–RB10, P/N y ausencia de secretos/encabezados vacíos.
- **A1:** validar coherencia documental y negativos observables; no afirmar comportamiento browser/HTTP.
- **A2:** revisar delimitación JWT/RBAC/tenant y privacidad, sin atribuir pruebas de penetración o A/B ejecutadas.
- **A3:** cotejar M01.01–05 y sus A5 con PLAN/BITACORA, inventario y STANDARD; compilación .NET/Angular/MySQL/Playwright `NO_APLICA_DOCUMENTAL` por diff sólo Markdown.
- **A4:** publicar por fast-forward/CAS en `dev`, inspeccionar commit, blob y diff, verificar Scope Lock+procedencia V5 `completed/success` del SHA exacto, registrar transición BITACORA y readback.
- **A5:** otra Task Javier distinta de DEV05 revisa fuente, autor, SHA, blob y controles y añade `A5_PASS` o `A5_FAIL` real; no se da por aprobado en esta entrega.

**Estado de implementación UI y de escenarios P/N/NF:** `FUTUROS_NO_EJECUTADOS`. **Promoción a QA/main/PROD:** no autorizada.
