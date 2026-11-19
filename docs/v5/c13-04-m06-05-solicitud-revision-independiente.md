# C13-04.M06.05 — solicitud de revisión independiente del lote visual (V22)

**Repositorio único:** `solqaryn/Solqaryn`, rama `dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Autor de esta solicitud:** Task DEV05 `6adc2e55f47c81919f6374e561687076`. **Clasificación de esta entrega:** R0 documental, sin modificar runtime, permisos, APIs, base de datos ni workflows. **Acción exacta `PLAN_MAESTRO!D2016`:** «Solicitar revisión independiente del lote cuando sea un gate». La evidencia acumulativa `E2016` no equivale a certificar todas las fases ni autoriza QA/main/PROD.

## Objeto y destinatario de la solicitud

Se solicita revisión **genuina** por una de las otras cinco Tasks Javier autorizadas (Task ID distinto de DEV05, y distinto de la autora o reparadora del componente que examine) de las evidencias y pendientes de `C13-04`. Esta solicitud NO sustituye la revisión A5, NO crea un `A5_PASS`, NO presume disponibilidad ni identidad de un revisor, y NO obliga a cerrar `M06.06`. El revisor debe obtener su propio candado V17 sobre el scope que audite y dejar BITACORA con ID, SHA, actor, veredicto y causa verificable.

**Fuentes obligatorias de revisión:** [manifiesto de versiones y pruebas M06.02](c13-04-m06-02-version-diff-artefactos-reproducibles.md), [limitaciones y riesgos M06.03](c13-04-m06-03-limitaciones-riesgos-decisiones.md), PLAN/COLA/DAG actualizados por fórmula, BITACORA A:Z y los seis commits M05 originales. Verificar SHA de cada una y su relación con el scope, nunca aceptar afirmaciones aisladas o un PASS de otro SHA sin equivalencia por blobs y dependencias.

## Matriz de decisión para revisión externa

| Bloque | Evidencia favorable que el revisor debe verificar | Fallos/límites que debe conservar |
| --- | --- | --- |
| M05.01 | Vitest 5/5 de rutas, Angular CI exact-SHA | No certifica JWT real ni todo el catálogo |
| M05.02 | Angular TestBed 8/8 de contratos/error | HTTP interceptado/mocks; no firma JWT ni RBAC backend |
| M05.03 | Oracle EF, MySQL 8.4 efímero, Category=Integration real 1/1 | Identidad sintética en el test y cero Aiven productivo |
| M05.04 | Chromium 13/13, JWT firmado, API y MySQL efímero en casos acotados | No cubrir todos roles, rutas y dispositivos físicos |
| M05.05 | Chromium 8/8 de seguridad negativa | Check transversal `Módulos, cargas, sesión, catálogos, variantes, accesibilidad, facturación y correo` `completed/failure` en su SHA fuente; no falsear PASS |
| M05.06 | Chromium 5/5 de cinco páginas públicas | Check transversal `completed/failure` con tres regresiones de transferencias; no hardware real ni Core Web Vitals de campo |
| M06.01 | Contraste entre pruebas reales y mocks, revisar diff y limitaciones | No convertir análisis en aceptación global |
| M06.02 | Manifiesto de versiones, hashes, artefactos y procedimiento de reproducción | A5 de M06.02 debe venir de otra Task, no DEV05 |
| M06.03 | Registro R01–R12 y evidencia de fallos histórica no borrada | La documentación de riesgos no es reparación técnica |

### Criterios expresos de aceptación de la revisión

1. **A0:** contrastar acción atómica `D` de cada ID, archivos y dependencias, y diferencias de código/documento con SHA origen. Si existe cambio de runtime/tenant/seguridad fuera de esta solicitud, revisar esos controles concretos, no reducir riesgos R2.
2. **A1–A3:** inspeccionar pruebas positivas, negativas, fronteras, errores, replay y resultados HTTP/MySQL/JWT/browser reales **solo donde apliquen**. `NO_APLICA` a nuevas pruebas runtime para este archivo R0 porque `D2016` únicamente solicita revisión.
3. **A4:** exigir GitHub `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` en SHA de la publicación, readback PLAN/COLA/DAG/BITACORA y no contar `SKIPPED/CANCELLED/IN_PROGRESS` como PASS requerido.
4. **A5:** revisor real distinto de autor y reparador; puede registrar `A5_FAIL` o `A5_PENDIENTE` con defecto concreto. Si PASS, preservar historial negativo y registrar acto en BITACORA y PLAN sin cambiar owner I. Una A5 parcial de documento no libera gates técnicos pendientes ni autoriza QA.
5. **Cierre:** exigir evidencia de M06.04 rollback, de cualquier riesgo R2 requerido y de los gates por ID antes de considerar `C13-04.M06.06`. La elegibilidad DAG la calculan fórmulas, nunca este archivo.

## Acciones posteriores y rollback

- **Pendientes conocidos:** reparar los 3 fallos globales de transferencias mediante cambios y CI causal en su scope; obtener pruebas en dispositivos físicos si algún PLAN D lo exige; completar matriz global de accesibilidad, roles/rutas y métricas de campo cuando sea requisito explícito; inspeccionar M06.04 en su propio entregable. No afirmar que esos trabajos fueron ejecutados por esta solicitud.
- **Rollback de este changeset:** `git revert <commit_sha>` sobre HEAD actual de `dev` con exclusión del escritor, sin force-push y conservando BITACORA. No alterar producción, QA, `main`, secretos, pagos ni datos reales.

**Estado de la solicitud:** entregable documental de petición; **A5 de esta misma microtarea sigue PENDIENTE** de otra Task Javier. Ningún veredicto independiente se autogenera.
