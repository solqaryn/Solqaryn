# SOLQARYN V5 FINAL — C13-04.M01.04: riesgos, supuestos y decisiones de experiencia unificada

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 04 JAVIER (`6aeacb7820ac8191b7d768a6f33dfa09`). **Alcance:** revisión M01 documental de `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Estado de pruebas de interfaz:** `FUTURAS_NO_EJECUTADAS`; esto no certifica aplicación, conformidad WCAG ni rendimiento.

**Fuentes autorizadas:** [Standard V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO/COLA/DAG_TECNICO](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), `docs/PROJECT_SCOPE_LOCK.md`, `AGENTS.md`, Skill canónica y contratos C13-04.M01.01/M01.03. El preflight de esta microtarea observó PLAN fila 1985 PENDIENTE sin owner, COLA G/O=SI y DAG fila 1946 F=VALIDADO/G=SI; reclamación condicional por celda de estado y readback de owner, sin asignar otro ID.

## 1. Evidencia que justifica la matriz y sus límites

| Referencia inspeccionada | Git blob | Conclusión limitada |
| --- | --- | --- |
| `docs/v5/c13-04-m01-01-alcance-no-regresion-experiencia-unificada.md` | `fedf00f157e9d14e9150c48601070e84b0306ff3` | NR01–NR14 y seis superficies son contratos de preparación, no pruebas de UI |
| `docs/v5/c13-04-m01-03-inventario-codigo-brechas-coherencia-visual.md` | `f02a82a632b6abb6f35f83a22cb8e7fe834bb573` | F01–F14 y B01–B12 documentan evidencia estática y preguntas pendientes |
| `frontend/src/styles.scss` | `2eb48562fd77c758662af6b9683649b4f9698a64` | Tokens semánticos y reflow existen; no se ha medido contraste real |
| `frontend/src/styles/_enterprise-design-system.scss` | `6a188590af9a958bff2e3e0375c0c46cdb2b14e6` | Hay escala compartida de espaciado y tipografía, no adopción universal |
| `frontend/src/app/shared/feedback-state/feedback-state.component.ts` | `7deb40475f0c492d09e649e7351b2915bd53d356` | Estados semánticos declarados; no cobertura demostrada de todos los flujos |
| `frontend/src/app/core/auth/tenant-context.service.ts` | `63a711355495d2f80efba35bffb2b8727401154a` | Contexto tenant distingue selección/validación, no demuestra aislamiento A/B runtime |
| `frontend/e2e/m10-ui-ux-accesibilidad.spec.ts` | `ce3d50c53b5de8447fa9eba09208160657415fa3` | Fuente de Playwright no equivale a ejecución exitosa ni WCAG certificado |

Los blobs se refieren al inventario causal de M01.01/M01.03; ante cambios posteriores se deben revalidar contra el SHA real de la prueba. Las brechas B01–B12 son hipótesis falsables, **no fallos/vulnerabilidades confirmados**.

## 2. Riesgos potenciales R01–R14 (NO_VERIFICADOS)

| Riesgo | Hipótesis, impacto y condición observable de rechazo |
| --- | --- |
| R01 — jerarquía divergente | Storefront y backoffice presentan CTA/título sin prioridad consistente; rechazo si la acción esencial deja de ser identificable |
| R02 — tokens contradictorios | Color/espaciado/estado difiere entre módulos pese a reglas comunes; rechazo con matriz visual semántica incoherente |
| R03 — error solo cromático | Loading/empty/error/403 se distinguen únicamente por color o spinner; rechazo sin texto, rol accesible o recuperación |
| R04 — foco perdido | Router/dialogo/validación altera orden y retorno del foco; rechazo si teclado pierde continuidad o queda atrapado |
| R05 — controles sin nombre | Iconos, enlaces o botones carecen de nombre accesible y área táctil; rechazo por interacción inaccesible |
| R06 — reflow roto | Tablas y modales fuerzan desborde global a 320/390px; rechazo si una acción crítica se corta u oculta |
| R07 — motion forzado | Transiciones locales ignoran reduced-motion y comunican información solo por animación; rechazo si se pierde contenido |
| R08 — navegación/permiso | Contrato de menú y rutas no coinciden con permisos del servidor; rechazo por error 403 mal presentado o falsa autorización en UI |
| R09 — contaminación A/B | Tema, marca, caché o dato de tenant A persiste tras cambiar a tenant B; rechazo inmediato incluso si solo es visual |
| R10 — validación incongruente | Mensajes/campos no enlazan label, ayuda, error y recuperación; rechazo si usuario no entiende cómo corregir |
| R11 — diseño superficial | Una normalización CSS visual degrada legibilidad, densidad o estado de tareas; rechazo por pérdida de datos/acciones |
| R12 — budget ambiguo | `angular.json` 650/750 kB y política auxiliar de baseline distinta generan expectativas incompatibles; reconciliar por SHA antes de reclamar regresión |
| R13 — observabilidad sensible | Capturas de UI/E2E o logs de error contienen JWT/PII/identificadores tenant; rechazo de evidencia insegura |
| R14 — aprobación falsa | Archivo de test o CI cancelled/skipped/in_progress se presenta como PASS; rechazo sin prueba causal exact-SHA |

Para todos los riesgos: severidad **pendiente de evidencia**, responsables de decisión **por asignar**, y mitigación técnica **no ejecutada**. Ningún riesgo constituye autorización para tocar rutas, CSS, API o datos en M01.04.

## 3. Supuestos S01–S10, expresamente NO_VERIFICADOS

| Supuesto | Evidencia requerida para confirmarlo |
| --- | --- |
| S01 | Los tokens globales son consumidos por todas las vistas relevantes; comprobar importación efectiva y computed styles |
| S02 | Los guards Angular coinciden con RBAC backend; cotejar HTTP permitido/denegado con roles sintéticos |
| S03 | Los estados compartidos se utilizan consistentemente; auditar árbol de componentes por ruta y caso |
| S04 | Las seis superficies de M01.01 son representativas; completar inventario de rutas y pantallas reales |
| S05 | La marca cambia limpiamente al intercambiar tenants; probar sesión A/B con caché, vista y API |
| S06 | Modal/overlay devuelve foco y anuncia error; Playwright + navegación por teclado real |
| S07 | La UI es operable con zoom y viewport estrecho; capturas, scroll y foco a 320/390/768/1280 |
| S08 | Motion respeta preferencias de OS/browser; emulación de reduced-motion con estados estables |
| S09 | El budget de build y política auxiliar reflejan requisitos finales; comparar scripts, configuración y build exact-SHA |
| S10 | La instrumentación no expone secretos/PII; auditar trazas, reportes, screenshots y redacción |

## 4. Decisiones D01–D10 ABIERTAS, con dueño y gate futuro

| Decisión pendiente | Resolución necesaria antes de alterar runtime |
| --- | --- |
| D01 | Fijar contrato de tokens y semántica por estado sin duplicar design system |
| D02 | Determinar vistas/roles/tenants que definen cobertura mínima real |
| D03 | Definir oráculo de mensajes loading/empty/error/403 y rutas de recuperación |
| D04 | Acordar pautas teclado/foco/overlay y mecanismo de retest visual accesible |
| D05 | Resolver reflow y estrategia de tablas densas sin esconder operaciones |
| D06 | Delimitar branding por tenant y reglas de invalidación seguras, probadas A/B |
| D07 | Conciliar rutas, menú, guard y respuesta backend sin confiar en permiso frontend |
| D08 | Seleccionar entornos/datasets sintéticos y evidencia de browser autorizada |
| D09 | Reconciliar budgets Angular frente a scripts auxiliares con medición real y rollback |
| D10 | Establecer umbrales de aprobación, responsable independiente A5 y rollback sin QA/PROD |

Ninguna decisión puede darse por tomada desde esta matriz. Toda implementación posterior necesita una microtarea elegible distinta, análisis de impacto, autorizaciones vigentes, pruebas positivas/negativas y Scope Lock.

## 5. Ocho pares de escenarios para verificar en un scope posterior

| Caso | Positivo futuro esperado | Negativo futuro que debe rechazar |
| --- | --- | --- |
| P01/N01 | Título, acción principal y estado coherentes en login/storefront/backoffice | Jerarquía oculta acción primaria |
| P02/N02 | Focus visible y retorno correcto después de diálogo/error | Foco perdido/trap tras navegación |
| P03/N03 | Loading/empty/error tienen texto, role y recuperación | Estado solo por color o spinner infinito |
| P04/N04 | Reflow 320/390/768/1280 preserva formulario, tabla y CTA | Scroll horizontal global/acción inaccesible |
| P05/N05 | Cambio tenant A→B limpia tema, cache y datos visibles | Persisten tokens o datos de A en B |
| P06/N06 | Permiso backend A/B responde 200/403 conforme a rol autorizado | Ruta/guard habilita recurso sin JWT/RBAC |
| P07/N07 | Motion reducido mantiene significado, foco y estados | Animación imprescindible/flash permanente |
| P08/N08 | CI causal exact-SHA y comparación antes/después documentadas | Check skipped/cancelled/queued atribuido PASS |

**Todos P/N FUTUROS_NO_EJECUTADOS**. La ejecución real necesita rutas y roles concretos, fixture sintético, viewport, navegador, pasos, evidencia accesible, URL CI y SHA exacto. Una captura, un script fuente o un contrato escrito no son PASS.

## 6. Impacto, A0–A5 y rollback

- **Archivos afectados ahora:** solo este nuevo Markdown y una sección contigua de `CHANGELOG_AI.md`. No hay edición de Angular, SCSS, Material, rutas, componentes, .NET 10, DTO, EF/MySQL, migraciones, Cloudinary, Playwright, CI ni configuración.
- **Seguridad:** tenants A/B, JWT/RBAC relacional, pertenencia y ámbito de empresa siguen controlados por backend; la UI no autoriza acceso. No introducir secretos, PII o screenshots de clientes reales.
- **A0 documental:** verificar existencia/procedencia de 7 referencias, 14 R, 10 S, 10 D, 8 P/N; encabezados no vacíos y CHANGELOG atómico.
- **A1 documental:** criterios positivos/negativos trazables a NR01–NR14, F01–F14 y B01–B12; `NO_APLICA` a pruebas funcionales runtime de cambio exclusivamente Markdown.
- **A2 documental:** revisar preservación de aislamiento A/B, autorización backend y privacidad; no afirmar pruebas HTTP/JWT reales.
- **A3 documental:** cotejar inventarios y fuentes con blobs; build de Angular, MySQL y Playwright **NO_APLICA** al diff solo Markdown, no PASS.
- **A4:** publicar fast-forward con CAS HEAD, comprobar Git diff exacto, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` en SHA publicado, readback PLAN/COLA/BITACORA.
- **A5:** queda `A5_PENDIENTE` para otra de las cinco Tasks Javier; prohibida autoaprobación, QA/main/PROD o liberación M01.06 por evidencia provisional.
- **Rollback:** `git revert` no destructivo del commit publicado, registrar evento compensatorio nuevo, conservar historial y revalidar estado/CI.

**Resultado entregable M01.04:** riesgos, supuestos, decisiones y pruebas futuras con límites explícitos. No ejecuta auditoría visual, no implementa una solución, no certifica accesibilidad ni modifica producción.
