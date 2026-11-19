# C13-04.M06.03 — limitaciones, riesgos abiertos y decisiones (V22)

**Identidad y alcance.** SOLQARYN / `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`, `MICROTAREA_ID=C13-04.M06.03`, `AUTHOR_TASK_ID=6adc2e4bd8a48191ac7d8641ed95a1a3`. **Riesgo del changeset: R0 documental**; los riesgos de UI de plataforma, autorización y multiempresa descritos siguen R2. La acción exacta de `PLAN_MAESTRO!D2014` es **documentar limitaciones, riesgos abiertos y decisiones tomadas**. `PLAN E2014` («revisar interfaces existentes y unificar lenguaje visual, estados y responsive; completar M06») es **meta acumulativa**, no certificación de todas las pantallas por este documento.

## A0 — Evidencias existentes y alcance verdadero

| Microtarea | Commit fuente | Evidencia acotada | Límite |
| --- | --- | --- | --- |
| M05.01 | `cc82b73c1391d28c8bad0455c19a404c708c2f98` | Vitest 5/5 de reglas de rutas/guardas y 16 programas, CI frontend y A5 independiente | Reglas y tests frontend; no JWT HTTP, SQL, ni WCAG global |
| M05.02 | `f762c6a4f73a31790996f7b4d7487d1fde6dbd3d` | Angular TestBed 8/8 de contrato y estados, HTTP mock | No contrato HTTP backend autenticado ni revocación persistente |
| M05.03 | `22c007d866a7fd84f3e1e941d8eedd5c6ded8b06` | MySQL **8.4.13 efímero** 1/1 Category=Integration y suite global 52/52, aislamiento A/B en pruebas | No Aiven productivo ni JWT HTTP/comportamiento comercial completo |
| M05.04 | `d37e117bab2cc500d9e65d594887dbb1a7f418aa` | GitHub-hosted Chromium 13/13 con .NET 10, JWT firmado y MySQL efímero para recorridos concretos | No prueba todos los módulos, roles, terminales ni operación real |
| M05.05 | `bf97c9a252fa4d4b495786b28160434c39d43dc4` | Chromium 8/8 de negativas JWT, revocación y anti-enumeración; A5 distinto de autor/reparador | La matriz de archivos/jobs/rutas globales no queda certificada |
| M05.06 | `acbc5e6dbf1d85cc21c88f18f2b264ce6f382c5d` | Chromium 5/5 de cinco superficies públicas, tres tamaños, Tab/Enter, anónimo/overflow, build/lint, Scope Lock, V5; A5 independiente | No dispositivos físicos, métricas de campo LCP/INP/CLS, cobertura de todas las pantallas ni JWT real en **esa** suite pública |

Los conteos anteriores son **pruebas históricas documentadas en PLAN J/K/L, BITACORA y Github**, no resultados ejecutados nuevamente por este documento. No transferir un PASS a otro SHA sin comprobar blobs, dependencias y configuración de tests. La prueba M05.06 inicialmente falló **1/5** porque el selector `h1,h2` elegía un título promocional oculto; la corrección usa `getByRole('heading', { name: 'Iniciar sesión' })` y [run 44610791484](https://github.com/solqaryn/Solqaryn/actions/runs/44610791484) acredita 5/5. Se **conserva** la falla P2 histórica; no se reescribe.

**Otro fallo que NO se borra:** el mismo SHA de M05.06 tuvo el check transversal [«Módulos, cargas, sesión, catálogos, variantes, accesibilidad, facturación y correo»](https://github.com/solqaryn/Solqaryn/actions/runs/44610791487/job/135866520987) en `completed/failure` con **97 casos aprobados y 3 casos fallidos de transferencias de inventario** (selectores/visibilidad/clic), distintos de las cinco superficies públicas de la prueba dirigida. Es un bloqueo real de ese alcance global y necesita reparación causal separada. No se convierte en PASS de toda la plataforma por el A5 acotado de M05.06.

## Registro de riesgos y decisiones auditables

| Código | Riesgo/limitación no certificada | Decisión vigente y condición de salida |
| --- | --- | --- |
| R01 | Cinco rutas públicas no representan todas las pantallas/versiones y estados del catálogo global | Mantener inventario de rutas por SHA y matriz pantalla × estado × acción × viewport; no declarar cobertura total |
| R02 | `viewport` Chromium emulado ≠ dispositivo físico | Etiquetar simulación; si el PLAN D de otro ID exige dispositivo físico, requerir evidencia de ejecución física antes de cierre |
| R03 | DOMContentLoaded local y capturas de runner ≠ Core Web Vitals de usuarios | Registrar LCP, INP, CLS de campo como `NO_MEDIDO` hasta tener telemetría auténtica permitida |
| R04 | Tab/Enter sobre cinco páginas no prueba WCAG integral | Retener accesibilidad de foco, labels, contraste, lectores de pantalla, reduced motion, 0/1/N y errores en matriz de alcance |
| R05 | Un mock de login/token, HTTP interceptado o `localStorage` sintético podría aparentar JWT/RBAC real | Preservar pruebas reales firmadas y tenant A/B de M05.04/.05 solo por su scope; no extrapolar a todas las rutas/roles |
| R06 | Acceso a archivos/jobs/sucursales de otro tenant puede escapar a rutas ya comprobadas | Exigir negativos BOLA/IDOR por recurso, rol, revocación y 401/403 en unidades técnicas específicas, sin bypass |
| R07 | Cambios de UI pueden publicar contenido privado o retener diagnósticos 403/503 | Preservar denegación anónima, ausencia de escrituras y pruebas de no filtración, con errores opacos |
| R08 | Reintentos, carga progresiva y cambios de densidad pueden duplicar GET o exponer estados inconsistentes | Reproducir concurrencia, 0/1/N y contratos de respuestas cuando la tarea toque esa pantalla |
| R09 | Regresión global transferencias inventario 3 FAIL puede quedar oculta por CI dirigida verde | Conservar fracaso histórico, diagnosticar selectores/requisitos verdaderos y obtener nuevo CI SUCCESS en SHA causal por su propio scope |
| R10 | MySQL 8.4 efímero se puede confundir con Aiven, rollback fiscal o respaldo de producción | Mantener separadas pruebas SQL reales aisladas de R3 operaciones productivas; no hacer restauraciones ni pagos sin permiso explícito |
| R11 | Nuevos commits entre pruebas pueden invalidar SHA o archivos compartidos | Revalidar HEAD, hashes de spec/workflow/componentes/lockfiles y CI; fast-forward serializado con CAS; `SKIPPED` nunca cuenta como PASS obligatorio |
| R12 | LISTO individual/A5 independiente puede habilitar erróneamente un sucesor global | Conservar PLAN H/I/J/K/L/M, BITACORA histórica y fórmulas DAG/COLA; M06.04 rollback, M06.05 revisión y M06.06 cierre exigen sus propios gates |

## Decisiones adoptadas por esta unidad

**D01** Registrar solo evidencia ya observada, con SHA y límites, sin fingir nuevas ejecuciones. **D02** Preservar las pruebas reales MySQL 8.4 y JWT/Chromium donde existieron, distinguiéndolas de mocks. **D03** Retener los 3 fallos globales de transferencias inventario para reparación por scope legítimo; ningún PASS global inventado. **D04** Mantener A5 independiente por Task ID distinto del autor y reparador, con lectura de código, pruebas y event_id idempotente. **D05** No alterar source code, Angular, backend, base de datos, workflow, roles o proveedores en este cambio R0. **D06** No modificar manualmente `COLA!G/O`, `DAG_TECNICO!F/G` ni borrar FAIL histórico. **D07** No liberar QA/main/PROD ni pagar/cobrar, migrar, restaurar datos o cambiar secretos. **D08** La meta acumulativa C13-04 M06 queda pendiente de posteriores microtareas y gates; este registro individual no la certifica.

## A1–A5 y reversión

- **A0:** cotejar PLAN D2014, fuentes M05.01–06, el diff y el árbol. Cambio solo Markdown y CHANGELOG aditivo; arquitectura intacta.
- **A1:** comprobar los 12 riesgos y 8 decisiones falsables, incluido el contraejemplo de falso PASS global. Un estado anterior PASS no acredita nuevas pruebas.
- **A2:** `NO_APLICA` ejecutar seguridad JWT/RBAC/tenant en este diff documental sin runtime; **no** significa que las carencias globales estén aprobadas.
- **A3:** `NO_APLICA` ejecutar nueva integración HTTP/MySQL/Chromium/PSP en esta documentación; las pruebas históricas quedan referenciadas con límites.
- **A4:** exigir GitHub `verify-solqaryn-project-scope` y «V5 - Sin encabezados vacios ni atribucion cruzada» `completed/success` del **SHA exacto publicado**, diff/procedencia y PLAN/COLA/DAG/BITACORA readback, V17/CAS. Ningún check requerido `failed/skipped/cancelled/queued/in_progress` es PASS.
- **A5:** PENDIENTE revisión genuina por otra Task Javier, distinta de DEV03 autora, sobre el mismo documento/SHA; no autoaprobar ni promover ambientes.

**Rollback:** `git revert` normal del commit del documento en dev tras revisar HEAD/escritores, conservando BITACORA y demás cambios, nunca `force-push`/reset. Sin secretos, producción o proveedores.
