## 2026-11-13 — V5 C00-01.M01.04: riesgos, supuestos y decisiones pendientes [Javier :20]
- Inventario causal de nueve riesgos R01–R09, cuatro supuestos y decisiones de salida del baseline: orden posterior V5 frente a hold histórico, claim concurrente J10/J20 de M01.02 resuelto sin tomar trabajo ajeno, infraestructura propuesta no desplegada, imágenes históricas cero (NO_APLICA), seguridad A/B y A5 pendientes sin falsos PASS.
- Cambios exclusivos: docs/v5/c00-01-m01-04-risks.md, scripts/tests/c00-01-m01-04-risks.test.mjs y CHANGELOG aditivo. A0: Node 22.16.0 --check y grep dirigido de secretos PASS; A1/A2: positivo + cinco negativos fail-closed, 6/6 Node tests; A3: integración documental/protocolo sin runtime, sin deps nuevas. No se simulan .NET, Angular/Playwright browser, HTTP, MySQL, tenants live, providers ni migración.
- A4 requiere SHA fast-forward, diff y tests pospublicación, checks GitHub exact-SHA y readback PLAN/COLA/BITACORA. A5_PENDIENTE corresponde a otro TASK_ID, sin QA/main/PROD. Rollback: git revert aditivo sin force-push.

## 2026-11-13 — V5 C00-01.M01.02 inventario de actores, interfaces y superficies
- Matriz de entradas, salidas, actores y límites de confianza para plan V5 (solo documentación baseline). Caso positivo y cinco negativos ejecutados con Node 22: 6/6 PASS, análisis estático y scanner de secretos dirigido; sin .NET, Angular E2E, HTTP, MySQL ni disponibilidad de proveedores. Conserva JWT/BCrypt/RBAC/tenancy A/B, separación QA/main/PROD, migración histórica de imágenes NO_APLICA. A5_PENDIENTE independiente; rollback mediante git revert normal. Ningún PR o branch nuevo.

## 2026-11-13 — V5 C00-01.M01.01 alcance y no-regresión
- Documentado objetivo, no-alcance, fuentes Git con huellas, contradicciones y rollback del baseline de la primera microtarea V5. Sin cambios de API, DB, frontend, migraciones, autenticación ni proveedores. Prueba Node 22 dirigida 5/5 (positivo y cuatro negativos) más A0 scan de secretos; sin E2E/HTTP/MySQL/producción. `A5_PENDIENTE` de Task independiente. Commit directo a `dev`, sin PR/ramas.

## 2026-11-12 — Base documental V5 y nueva orden condicional de reanudación
- Plan rector V5 FINAL y Sheet 2.340 IDs verificados; COLA derivada mediante 35.100 fórmulas + 65 gates; DASHBOARD 13; PLAN y BITACORA son fuentes de escrituras reales. Filas auxiliares recortadas y BITACORA sin límite adicional.
- Autoriza reactivar seis Javier tras todos los checks requeridos verdes del SHA exacto; conservar seis prompts/horarios/IDs/V13 A0–A5, retirar únicamente orden temporal OWNER_PAUSE_HOLD después de CI y restaurar peer-watch. Dos Alex sólo desde su titular. Copia de las seis configuraciones: https://docs.google.com/document/d/1w_nqqAPX3bhvMTVjfrDQEr8Weii8jZ4BcaE8qFvnrsw/edit
- Jira KAN-4 informativo, sin duplicar 2.340 issues; backup previo a limpieza: https://docs.google.com/spreadsheets/d/1QjTHCYKaerAtshAIkXKvTXg-KidavnDG18DOw2jLh2k/edit . No cambia runtime ni DB, QA/main/PROD, ni concede permisos nuevos. Revert normal selectivo para rollback de este commit.

## 2026-11-12 — P5-001.C: nuevo contrato delimitado de tokens institucionales
- Nueva prueba de contrato sobre `frontend/src/styles.scss` blob `2eb48562fd77c758662af6b9683649b4f9698a64`, ocho subtests negativos/positivos, workflow exact-HEAD y acta de recuperación. Sin cambios de runtime, DB, API ni secretos.
- Candidato previo SHA256 de Alex irrecuperable; esta reconstrucción corresponde a una nueva intervención autorizada de Javier, no atribuirla a Alex. A4 requiere readback CI+Sheets; A5 independiente pendiente antes de QA/PROD.
- No reactivar las Tasks Javier o Alex ni reclamar microtareas nuevas durante la pausa del propietario.

## 2026-11-12 20:56 America/Tegucigalpa — Pausa ejecutiva y congelamiento de nuevas reclamaciones
- El propietario ordenó pausar seis Javier y dos Alex para modificar el Plan Comercial. Se aplicó actualización física a las seis Javier con is_enabled=false y prompt OWNER_PAUSE_HOLD (IDs, títulos, RRULE y zona sin cambios). Readback físico: cero habilitadas Javier.
- Reanudar compañeras queda expresamente prohibido; únicamente puede terminar una microtarea reclamada antes de recibir la orden y salir sin nuevo claim. DEV 03/04/05/06 estaban in_progress cuando se consultó; el cambio físico no garantiza recepción de la orden por las corridas ya iniciadas.
- Las dos Alex pertenecen a otra cuenta; no se modificaron y su suspensión no está verificada. No inventar ocho pausadas. Su titular debe gestionar la pausa física.
- Sin cambios de código productivo ni de seguridad, QA/main/PROD, PR, secretos o roles. Retorno del scheduler únicamente bajo orden posterior y revisión del plan.

## 2026-11-12 — P5-006.C: delimitación del modal de confirmación institucional (Javier :30)
- Archivo de producto único `frontend/src/app/shared/confirm-dialog.component.ts`, blob `f9b73544cc70beba83a891b2fc89537f0718af8b`. Contrato Cancelar=false/Eliminar=true, título/mensaje interpolados, Material Dialog standalone.
- Prueba estática `scripts/tests/p5-006-c-delimitacion-confirmacion.test.mjs` (Node 22.16.0, seis subtests dirigidos) y `docs/P5-006-C-DELIMITACION-CONFIRMACION-DEV.md` documentan el escenario y negativos. No altera runtime Angular, ni certifica navegador, JWT, MySQL o QA/PROD.
- Rollback con revert normal selectivo; A4 exige SHA exacto/readback tras dev; A5 independiente pendiente para promoción.

## 2026-11-12 — P5-004.C: delimitación estática de detalle modal (Javier :40)
- Fuente única `frontend/src/app/shared/alerts/app-alert.service.ts`, blob `3bde601b54bad7d979fca9029025ce4eb72a44db`, escenario de detalle optativo seguro. No certifica todos los modales ERP.
- Prueba `scripts/tests/p5-004-c-delimitacion-modal-detalle.test.mjs`, Node 22.16.0 6/6 preflight extracto y 8/8 inspección de fuente real en GitHub, sin afirmar browser/E2E.
- Nota `docs/P5-004-C-DELIMITACION-MODAL-DETALLE-DEV.md`; solo test/nota/changelog, A5 independiente pendiente.

## 2026-11-12 — P5-008.D: reuso de alertas institucionales existente (Javier :20)
- REUSE del servicio `frontend/src/app/shared/alerts/app-toast.service.ts` (blob `0478821b246cae5fdf3016a5c25081a1726f925a`): severidades, duración, acción texto Cerrar, anuncio aria y estilos. Sin modificar Angular runtime ni servicios.
- Prueba dirigida `scripts/tests/p5-008-d-reuso-alertas.test.mjs`: Node 22.16.0 6/6 PASS tras VALIDANDO (control positivo de fuente real, 5 negativos sobre duración, texto/HTML, aria, wrapper error y acción de cerrar). SHA256 `89721265805b7f22b4121d61df2790a1de6bc32ee365e07ce1fd82c9325bead2`. Nota `docs/P5-008-D-REUSO-ALERTAS-DEV.md`.
- No certificar navegador, lector, tenants, JWT/DB; A4 SHA/readback poscommit y A5 independiente pendiente. Rollback mediante revert aditivo sin force.

## 2026-11-12 - P5-012.C: delimitacion del mapa de navegacion jerarquica V1 (Alex :35)
- Contrato producto existente frontend/src/app/shared/navigation/navigation-contract.json, blob 7142082462d7bc53ff4f0cdf96f67f21405f3a44, no editado.
- Prueba scripts/tests/p5-012-c-delimitacion-jerarquia-navigation.test.mjs: 5 casos Node real JSON, control de esquema, duplicados, metadatos modulo/accion y admins, rechazo de nietos no soportados; nota docs/P5-012-C-DELIMITACION-NAVEGACION-JERARQUICA-DEV.md.
- Solo DELIMITAR: padre/hijo V1, no profundidad ilimitada, autorizacion backend o navegador. A5 independiente pendiente; rollback aditivo via git revert sin force.

## 2026-11-12 — P5-009.C: delimitación de scroll, foco y Escape (Javier :20)
- Un solo archivo de producto inspeccionado: `frontend/src/app/app.component.ts`, blob exacto `11aa993b011119d33e402f3f2bc1d08f133e34bd`, sin cambios funcionales. Contrato observado: foco/retorno de menú, Escape, scroll lock/release, destino de contenido y limpieza de contexto.
- Test Node v22.16.0 `scripts/tests/p5-009-c-delimitacion-scroll-foco-escape.test.mjs` ejecutado después de VALIDANDO: 6/6 PASS (positivo real-source más cinco negativos); SHA256 `b0aed580f93e6f272b00d1201650b57a70d5d0583568cbf712fb5fa30fc42740`. Nota `docs/P5-009-C-DELIMITACION-SCROLL-FOCO-ESCAPE-DEV.md`.
- Es delimitación estática, no prueba de navegador, foco real, accesibilidad ni permiso multiempresa. A5 independiente pendiente; rollback con revert aditivo, sin force-push.

## 2026-11-12 — P5-014.C: delimitación estática de menú accesible (Javier :40)
- Fuente exclusiva `frontend/src/app/shared/navigation/app-navigation-menu.component.ts` blob `b70ef4ffb252228cf2ab19754a5e30d53f64aca0`, con enlaces semánticos/ARIA y RBAC de navegación, sin alterar el runtime.
- Test dirigido `scripts/tests/p5-014-c-delimitacion-teclado-aria-menu.test.mjs` (Node 22.16.0, 6/6 preflight sobre extracto, y 8/8 pruebas sobre fuente completa GitHub por inspección dirigida); NO certifica navegador, lector de pantalla ni auth backend.
- Evidencia y rollback `docs/P5-014-C-DELIMITACION-TECLADO-ARIA-MENU-DEV.md`; no API, migraciones, datos, dependencias, QA/main/PROD; A5 independiente pendiente.
## 2026-11-12 - P5-017.C: delimitacion responsive de modal de alerta (Alex :35)
- Archivo de producto de referencia sin modificar: frontend/src/app/shared/alerts/app-alert.service.ts, blob 3bde601b54bad7d979fca9029025ce4eb72a44db.
- Test scripts/tests/p5-017-c-delimitacion-layout-responsive.test.mjs: cinco casos Node positivos/negativos sobre ancho 94vw, breakpoint 480px, apilado, foco restaurado, rechazo HTML. Nota docs/P5-017-C-DELIMITACION-LAYOUT-RESPONSIVE-DEV.md.
- Alcance DELIMITAR, no certifica dispositivo real, WCAG, tenancy/JWT/RBAC, QA/main/PROD; A5 independiente pendiente. Reversión selectiva con commit ordinario sin force.

## 2026-11-12 — P5-015.C: delimitación de estados de carga y vacío (Javier :20)
- Archivo real único `frontend/src/app/features/usuarios/usuario-detail.component.html` (blob `bca657fd3ede944cb10c77ddc72063c11eb0ec65`): ramas loading spinner, notFound vacío y usuario cargado con interpolación segura; sin modificar runtime.
- `scripts/tests/p5-015-c-delimitacion-loading-empty.test.mjs` Node22.16.0 (5/5 PASS después de VALIDANDO): positivo, y negativos de spinner ausente, ramas no exclusivas, loaded sin guardia y HTML/PII insegura; `docs/P5-015-C-DELIMITACION-LOADING-EMPTY-DEV.md`. SHA256 `cf33d0e8cca241de789c920b3c869af64c2f1c90143a5fc620e8a4bc46fdea6c`.
- Alcance exclusivamente estático sobre código real; no afirmar navegador, Angular E2E, tenants o datos live. A4 postcommit y A5 revisión por otra Task. Revert aditivo sin force.

## 2026-11-12 — P5-002.C: delimitación estática del shell administrativo (Javier :40)
- Se identifica como único TARGET_PATH de producto `frontend/src/app/app.component.ts`, blob `11aa993b011119d33e402f3f2bc1d08f133e34bd` (HEAD dev comprobado); gate autenticado, navegación lateral, skip-link, foco y reset de contexto.
- Se agrega `scripts/tests/p5-002-c-delimitacion-shell.test.mjs` con un positivo y negativos de auth, accesibilidad, limpieza tenant y HTML inseguro; preflight de lógica Node 6/6 sobre extracto fuente y lectura directa exacta de GitHub 8/8, sin fingir E2E/browsers.
- Nota causal `docs/P5-002-C-DELIMITACION-SHELL-DEV.md` con ruta, SHA, evidencia y rollback. No se modifica Angular runtime, permisos, API, DB, dependencias, QA/main/PROD; A5 por otro TASK_ID después de A0–A4.
## 2026-11-12 — P5-007.C: delimitación de modal de proceso institucional (Javier :20)
- Delimitado el componente real de anulación `frontend/src/app/shared/anular-dialog.component.ts` (blob `d751503438f963f1deebe371cd14623ad6038e16`), sin cambio de runtime; flujo motivado, cancelación segura, texto interpolado y diálogo tipado.
- `scripts/tests/p5-007-c-delimitacion-modal-proceso.test.mjs` prueba el contrato estructural con Node v22.16.0 5/5, positivo y cuatro negativos dirigidos: cancelación, motivo vacío, HTML crudo, contexto perdido; SHA256 `34b37488f05dae7dd68049d892a8436288470a036999f4dfa35324b9ec59332b`. Documentación `docs/P5-007-C-DELIMITACION-MODAL-PROCESO-DEV.md`.
- A0–A3 ejecutadas tras VALIDANDO; A4 requiere readback exact dev, A5 independiente pendiente. No certificar navegador, Angular live, tenancy, MySQL, QA/PROD. Rollback con revert aditivo sin force-push.

## 2026-11-12 - P5-003.C: delimitacion del contrato de cuatro modales (Alex :35)
- Fuente unica de runtime existente: frontend/src/app/shared/alerts/app-alert.service.ts, blob 3bde601b54bad7d979fca9029025ce4eb72a44db; sin modificar Angular.
- Prueba estructural scripts/tests/p5-003-c-delimitacion-contrato-modales.test.mjs (5 casos, positivo y negativos de cancelacion, foco, confirmacion, requisito y HTML) y nota docs/P5-003-C-DELIMITACION-CONTRATO-MODALES-DEV.md.
- Alcance solamente DELIMITAR; no afirma cuatro modales operativos, browser/E2E, autorizacion RBAC/JWT/tenancy ni QA/main/PROD. A5 independiente pendiente; revert selectivo sin force.

## 2026-11-12 — P5-010.C: delimitación de paginación y filtros de listas (Javier :20)
- Foco acotado a `frontend/src/app/core/navigation/list-navigation-state.service.ts` (blob `513765de62e3ad2fac85f1495054448cfea2ba9e`), sin cambiar frontend productivo; se documentan claves permitidas, precedencia query params, `replaceUrl`, almacenamiento tolerante a errores, coerción y clave de usuario.
- Prueba sobre código fuente real `scripts/tests/p5-010-c-delimitacion-paginacion.test.mjs` con Node 22.16.0: `node --check` y `node --test` 5/5 PASS, control positivo y negativos de allowlist, query, historial, namespace, coerción y storage. SHA256 de prueba `8e76f7e14e5e8dfdb4dee591e62436a31e1e5feef453aa5afe3946c753662293`.
- Solo delimitación: no acredita browser real, aislamiento multiempresa, Angular runtime, MySQL ni QA/main/PROD. A5 pendiente de otro TASK_ID. Rollback: revert selectivo aditivo, sin force-push.

## 2026-11-12 — P5-016.C: delimitación de acciones visuales coherentes (Javier :20)
- Contrato real de acción uniforme `Cerrar` en `frontend/src/app/shared/alerts/app-toast.service.ts`, blob `0478821b246cae5fdf3016a5c25081a1726f925a`; no se modifica producto Angular.
- Test de delimitación `scripts/tests/p5-016-c-delimitacion-acciones.test.mjs` 5/5 Node 22.16.0, positivo y negativos de ausencia de fallback Cerrar, wrappers de severidad incongruentes, estilos sin variante, HTML inseguro y contrato vacío. Nota `docs/P5-016-C-DELIMITACION-ACCIONES-DEV.md`.
- Solo evidencia estática del código vivo; no certifica interacción en navegador, lector de pantalla, responsive, JWT/RBAC/tenant, backend ni QA/PROD. Rollback selectivo con revert aditivo en dev sin force. A5 independiente pendiente.

## 2026-11-12 — P5-008.C: delimitación de alertas institucionales (Javier :20)
- Archivo real único `frontend/src/app/shared/alerts/app-toast.service.ts`, blob `0478821b246cae5fdf3016a5c25081a1726f925a`. Escenario acotado de notificaciones toast, no el subsistema modal completo.
- Prueba estática dirigida `scripts/tests/p5-008-c-delimitacion-alertas.test.mjs` (Node 22.16.0; 5/5 precheck) y nota `docs/P5-008-C-DELIMITACION-ALERTAS-DEV.md`: variantes, urgencia accesible, duración y rechazo de HTML inseguro. No cambia Angular ni certifica browser/tenancy/live.
- Reversión selectiva aditiva en dev sin force; A4 requiere SHA y readback del mismo changeset; A5 independiente pendiente antes de QA/PROD.

## 2026-11-12 — P5-013.C: delimitacion de una rama activa de menu (Javier :10)
- Contrato unico de producto `frontend/src/app/shared/navigation/navigation-contract.json`, blob 7142082462d7bc53ff4f0cdf96f67f21405f3a44. Escenario grupo `administracion`, sin editar runtime ni declarar expansion exclusiva del menu.
- Nota `docs/P5-013-C-DELIMITACION-RAMA-MENU-DEV.md` y test Node `scripts/tests/p5-013-c-delimitacion-menu.test.mjs`, casos estructurales positivo y negativos de grupos/rutas/permisos. No se acredita prueba Angular, navegador, JWT/backend ni QA/PROD; rollback mediante revert normal del changeset.

## 2026-11-12 — Identidad DEV 01/05 y selección factible; siete estados COLA sincronizados
- `is_self_owned_automation=false` significa sólo que la automatización no corre para el agente que hace esa consulta; no es revocación de cuenta. `Automations.get 404` no demuestra inexistencia de la Task; conservar `peek` como lectura positiva y validar propiedad/ID/conectores reales/Skill antes de cualquier escritura. Denegación real continúa NO_GO.
- V13 exige escanear TODAS las PENDIENTE habilitadas, comprobar herramientas concretas y continuar con otra elegible si .NET/Angular/browser/proveedor real no permite acreditar pruebas del primer candidato. Sin inventar A0–A5 ni completar falsamente.
- COLA filas 108,110,118,120,127,128,249: E manual VALIDANDO convertido a fórmula que refleja PLAN_MAESTRO H (PENDIENTE al corte). Nota en cada celda conserva antiguo estado, evidencias y autorías J:M. No se cambió PLAN, responsables, PR ni QA/PROD.

## 2026-11-12 — P5-018.C: delimitación del caso de accesibilidad transversal (Javier :50)
- Plan Comercial Inicial V3 P5-018.C, antecedente P1-016.B LISTO. Un solo archivo de producto: `frontend/src/styles/_m10-accessibility.scss`, blob de línea base `6140d41b8a7e1e98f4fa1e43e0ab72f04b6ae29f`. Documentación de foco, reduced motion y forced colors en `docs/P5-018-C-DELIMITACION-ACCESIBILIDAD-DEV.md`.
- Test Node aislado `scripts/tests/p5-018-c-delimitacion-accessibility.test.mjs` con caso positivo y rechazos de eliminación de motion, forced-colors y foco; no altera SCSS de producto, Angular runtime, backend, RBAC, tenants, MySQL, secretos ni entornos QA/main/PROD.
- Esta microtarea solo delimita el escenario; no certifica WCAG 2.2 AA, contraste, zoom, teclado/E2E ni lector de pantalla. P5-018.D/E requieren pruebas de uso real; rollback mediante revert selectivo normal en dev, sin force.

## 2026-11-12 - G0-006.B: cierre de matriz actual F/G/H y rechazo fases historicas (Javier :30)
- scripts/quality/g0-006-b-traceability-matrix.mjs y scripts/tests/g0-006-b-traceability-matrix.test.mjs verifican forma de recibos comerciales F/G/H: ID, test, SHA commit, estados LISTO, dependencia SI, readback institucional, A5 pendiente o PASS sin permiso de promocion; rechazo de fases 8/13 y reactivacion de migracion Angular concluida.
- Las referencias de commit son F=6a10d42e, G=9c6f2158, H=a8de9825 leidas de GitHub y PLAN/COLA; control positivo en test sintetico NO certifica por si mismo fuentes externas ni autoriza QA/main/PROD. La matriz real se registra en el ledger institucional, sin introducir otro Plan Maestro.
- A0 Node22 --check, inspeccion secretos y ausencia de deps; A1 5/5 pruebas de contrato; A2 fases obsoletas, evidencias falseadas y A5 autopromocion rechazadas; A3 20/20 suites Node (B/G/H y compatibilidad D/E/F) PASS. A4 requiere commit exacto FF y readback; A5 pendiente de otra Task; rollback revert selectivo sin force.

## 2026-11-12 - G0-006.H: regresion positiva de trazabilidad V13 Direct DEV (Javier :30)
- Prueba scripts/tests/g0-006-h-positive-traceability.test.mjs verifica recibo sintetico positivo de requisito G0-006.H a codigo/test/auditorias/readback, junto con negatives SHA/arbol, A4, ausencia de tests, SKIPPED, fase historica y falta de evidencia Sheets/BITACORA. No certifica recibos reales ni promocion de entornos.
- A0 Node22.16.0 node --check, escaneo de secretos sin patrones; A1 5/5 Node focused suites PASS; A2 fase obsoleta no reabre Angular; A3 15/15 Node test suites compatibles con G0-006.G y pruebas locales D/E/F, sin dependencias ni modificaciones runtime. A4 exige SHA de commit y readback exacto; A5 otro Scope.
- Rollback via git revert selectivo, sin cambios a frontend/backend/MySQL/JWT/RBAC/QA/main/PROD. Ninguna atestacion externa del control sintetico.

## 2026-11-12 - G0-006.G: rechazo de reapertura historica Angular 8/13 (Javier :30)
- Se endurecio el helper informativo scripts/quality/direct-dev-traceability.mjs para rechazar intentos estructurados de reabrir fases retiradas 8/13 o migracion Angular completada aun cuando se disfracen con scope y hashes comerciales validos. No concede autoridad para publicar ni marca LISTO por si solo.
- Prueba dirigida scripts/tests/g0-006-g-legacy-angular-reopen.test.mjs: 5 suites Node; 10/10 PASS con regresion local de controles D/E/F. A0 Node22 --check y busqueda de secretos; A1 positivo+negativos sinteticos; A2 rechazo de fase heredada fail-closed; A3 API guard Node sin dependencias nuevas, sin mutaciones frontend/backend/DB/RBAC/JWT.
- Evidencia offline, no atestacion real de GitHub/Sheets/A5/QA/PROD; A4 exige commit dev fast-forward, readback SHA y BITACORA; A5 queda PENDIENTE por otro Task. Rollback mediante revert selectivo del commit publicado, sin force.

## 2026-11-12 — Incorporación Alex 2/2 según certificación del propietario
- Alex comunicó readback nativo desde su cuenta de sus dos Tasks existentes: PRIMARY DEV 07 :05 y PRIMARY DEV 08 :35, is_enabled=true, prompts V13 A0–A5 y recuperación mutua, completed y requires_user_action=false.
- Esta conciliación de documentos se basa en reporte de Alex, no en conexión nativa independiente desde Javier. No se tocaron Tasks físicas, permisos ni modelo de razonamiento; mantener límites de seguridad y pruebas A0–A5.

## 2026-11-12 — G0-005.B: puerta estructural de readback DEV/QA/PROD (Javier :20)
- Se agregan scripts/quality/g0-005-b-readback-policy.mjs y 4 pruebas Node dirigidas en scripts/tests/g0-005-b-readback-policy.test.mjs; un control positivo sintético y 33 negativos fail-closed para recibos F/G/H, SSO 503, upstream 503, origen, proyecto, SHA y separación de referencias no secretas.
- El positivo solo afirma STRUCTURAL_CONSISTENCY_NOT_LIVE_ATTESTATION. No certifica proveedores reales, bases/caches/secretos reales, login ni aprobación QA/PROD; ninguna credencial se inspecciona, se almacena o se imprime.
- Implementación Node 22.16.0 sin dependencias nuevas ni modificación de Angular, backend, MySQL, auth, tenancy, QA/main/PROD. Rollback: revert aditivo selectivo de commit directo dev sin force.

## 2026-11-12 - G0-005.H - positivo estructural y regresión de SHA exacto (Javier :50)
- Plan Comercial Inicial V3, microtarea G0-005.H, predecesora G0-005.G LISTO. Agrega scripts/tests/g0-005-h-positive-readback.test.mjs, 5 pruebas dirigidas y regresión A/F/G conjunta 34/34 Node22 PASS. SHA GitHub base observado 323e3333e73ce28369d82032c442705a325a9739, no equivale a SHA READY de Vercel real.
- Caso positivo DEV (sintético): mismo SHA en HEAD, deployment metadata y readback, alias SSO esperado 403, upstream 200. El global permanece NO_GO si QA/PROD no observados. Un segundo positivo completamente sintético utiliza dominios .synthetic.invalid de QA/PROD exclusivamente en memoria para comprobar un contrato estructural, explícitamente NOT_LIVE_ATTESTATION; **NO** representa QA/PROD publicado ni crea configuraciones reales.
- Negativos SHA incorrecto, proyecto Vercel foráneo y no exposición de PII/secretos conservan NO_GO. A0 sintaxis Node + inspección secretos/dependencias; A1 positivos/negativos; A2 fail-closed; A3 regresión Node; A4 requiere readback commit dev tras publicarse; A5_PENDIENTE revisión por otro TASK_ID para promoción.
- Sin cambios de seguridad runtime, JWT/RBAC/tenancy, backend, frontend, datos, workflows, QA/main/PROD, secretos o servicios de pago. Reversión: revert selectivo del commit directo en dev, sin force-push, preservando trabajo concurrente.

## 2026-11-12 - G0-005.G - regresión dirigida 503 y READY ajeno (Javier :50)
- Autoridad Plan Comercial Inicial V3 G0-005.G; depende de G0-005.F ya LISTO. Se agregan 7 pruebas negativas dirigidas en scripts/tests/g0-005-g-negative-readback.test.mjs; se reutiliza sin modificar el clasificador G0-005.A y el fixture sintético G0-005.F.
- Rechaza explícitamente: alias SSO 503, API upstream 503 con SSO 403 permitido, Vercel READY de proyecto ajeno, SHA de deployment distinto a HEAD, origen DEV ajeno y cualquier retorno con PII/secreto. No se consulta ningún origen proveedor en vivo: son casos sintéticos que acreditan lógica del clasificador, no disponibilidad externa.
- A0 Node v22.16.0 node --check y revisión de nuevos secretos/dependencias, PASS; A1 7/7 negativos funcionales PASS; A2 fail-closed de PII y falsos positivos PASS; A3 regresión Node --test G0-005.A/F/G 29/29 PASS. A4 requiere commit SHA exacto dev y readback posterior; A5_PENDIENTE revisión independiente antes QA/main/PROD.
- Rollback por revert selectivo de este commit directo DEV, sin force-push ni modificación de QA/main/PROD, MySQL, seguridad runtime, credenciales ni workflows.

## 2026-11-12 - G0-005.F - fixture sintético 503 para readback DEV (Javier :50)
- Fuente Plan Comercial Inicial V3 G0-005.F; depende de G0-005.A LISTO. Prepara un caso medible de resultado esperado, NO un 503 real: fixtures/g0-005-f-operational-503.json con recibos G0-005.C/D/E, SSO alias 503, API upstream 503, QA y PROD NOT_OBSERVED. DEV SHA del fixture es un identificador SINTÉTICO, no prueba de READY de Vercel.
- Prueba nueva scripts/tests/g0-005-f-operational-503-fixture.test.mjs: 4 casos dirigidos; NO_GO ante 503, NO_GO cuando falta observación de QA/PROD aunque controles DEV se vuelvan sanos, no promoción ni filtración PII. Regresión conjunta con G0-005.A: 22/22 Node v22.16.0 PASS; JSON parse y node --check correctos, cero dependencias o secretos añadidos.
- No se consultaron ni cambiaron qa/main/PROD, proveedor real, permisos, sesiones, MySQL ni infraestructura; ningún resultado de prueba sintética certifica HTTP/SSO/despliegues. Se conserva la custodia del autor y A5_PENDIENTE para revisión distinta.
- Rollback: revert selectivo del commit directo en dev, sin force-push, preservando G0-005.A y sus recibos.

## 2026-11-12 - G0-005.A - cierre de readback operativo con clasificador fail-closed (Javier :50)
- Fuente exclusiva: Plan Comercial Inicial V3, G0-005.A. Se reutilizan sin reescritura los recibos GitHub G0-005.C=0cea0aaae291a4d8161791f2b170d7087d5bc0e1, G0-005.D=e9a486776cbecdec8d6dfb6a7b1172cec6c28877 y G0-005.E=544a28fca6f63a9d5827086ccd07ac645a8f61e6, comprobados contra los commits originales y COLA G335=SI.
- Archivo nuevo scripts/quality/g0-005-a-readback-closure.mjs: clasifica recibos, matriz DEV/QA/PROD, origen, proyecto, SHA GitHub/despliegue y HTTP. El alias público DEV esperado es solqaryn-dev.vercel.app y el alias SSO dev protegido es solqaryn-dev-solqaryn.vercel.app. SSO 401/403 => AUTH_GATED_EXPECTED; SSO 503 y API upstream 503 => HTTP_ANOMALOUS/NO_GO; Vercel READY de proyecto ajeno o SHA divergente => NO_GO. Salida estructural nunca equivale a despliegue vivo ni A5.
- Matriz de observabilidad: DEV origen conocido https://solqaryn-dev.vercel.app/; QA/PROD: origen, proyecto, despliegue SHA, status HTTP y motivos NO_OBSERVADOS en esta ejecución, no se alteraron entornos ni se inventó equivalencia LIVE. Los ejemplos de pruebas Node son sintéticos. Si falta proveedor/lectura autentica el clasificador responde NO_GO; no reemplaza CI ni reviews oficiales.
- Pruebas dirigidas: scripts/tests/g0-005-a-readback-closure.test.mjs, 18/18 PASS Node v22.16.0, 16 escenarios negativos; A0 --check, dependencias nuevas 0, revisión de secretos sin hallazgos; A1 funcional sintético, A2 seguridad fail-closed/PII, A3 integración de blob exacto G0-005.E y regresión; A4 requiere SHA dev publicado + readback. A5_PENDIENTE: revisor TASK_ID distinto antes de QA/main/PROD.
- Rollback: revertir exclusivamente el commit directo G0-005.A en dev sin force-push, preservando los recibos C/D/E y commits de terceros. No tocar runtime auth, tenancy, MySQL, frontend, workflows, qa/main/PROD ni secretos.

## 2026-11-12 — Recuperación entre Scopes de Javier y preferencia máxima
- Seis Javier verificadas enabled. El propietario autorizó a las seis Tasks a reactivar una compañera pausada con una sola acción nativa is_enabled=true, lectura posterior y sin cambiar permisos o configuración.
- Preferencia de máximo razonamiento: instrucción y documentación actualizadas; herramienta de Tasks no expone selección model/reasoning_effort, por lo que no se certifica el nivel nativo. Alex sigue fuera de alcance.
- Se mantiene continuidad sin tope de cierres por corrida, auditorías A0–A5 y control estricto de acceso.

## 2026-11-12 - G0-006.F: preparar caso medible de trazabilidad (Javier :30)
- Fixture sintetico para detectar recibo de G0-006.F que reutiliza requirementId G0-006.E; esperado contract/NO_GO, no certificacion institucional.
- Cinco controles Node dirigidos: lectura fixture, baseline sintetico positivo, requisito ajeno negativo, hash de prueba obsoleto negativo y readback BITACORA ausente negativo. El helper de produccion se reutiliza sin alterarlo.
- A0-A3 dirigidas Node 22.16.0, sin cambios de runtime, RBAC, tenants, DB, CI, API, secretos ni dependencias. A4 requiere SHA exacto publicado y readback; A5_PENDIENTE solicita otra Task antes de QA/PROD. Rollback por git revert del commit directo, sin force.

## 2026-11-12 — Continuidad productiva sin tope de cierres
- Se refuerza continuar en una misma ejecución después de 3, 4, 5 o más cierres, sólo si hay tiempo, elegibilidad y evidencia completa A0–A4, registrando A5 pendiente de revisor externo.
- No autopausar Tasks por metas o cola momentáneamente vacía; si el proveedor impone suspensión o requires_user_action, comunicar causa verificable por run_id. No se garantiza eliminar pausas del proveedor.
- PRIMARY DEV 03 :20 se constató pausada por acción requerida sin causa disponible en el registro.

## 2026-11-12 — G0-006.A: trazabilidad directa V13 A0-A5 (Javier :20)
- Cierre atómico del Plan Comercial Inicial: evidencia C/D/E LISTO y matriz requisito→consumidor→código→prueba→SHA en docs/G0-006-A-TRAZABILIDAD.md, sin crear otro plan.
- Puerta asesora scripts/quality/g0-006-a-closure.mjs y pruebas Node deterministas: 1 control positivo, 23 negativos de legado, A0 estática, A5 falso, SHAs/dependencias/readbacks. La puerta no escribe ni certifica externamente.
- A0-A3 dirigidas a Node 22.16.0; A4 requiere SHA fast-forward/readback dev; A5_PENDIENTE exige otro TASK_ID antes de promover QA/PROD. Rollback: revert aditivo del commit, sin force-push; runtime/tenant/DB intactos.

## 2026-11-12 — Calidad A0–A5, títulos Javier secuenciales y continuidad de Tasks
- Se añaden A0 estática exigente sobre candidato y A5 revisión independiente posdev con evidencia diferenciada. El cierre dev exige A0–A4 PASS + A5_PENDIENTE; qa/main/PROD requiere A5_PASS y autorización aparte. No se deshabilitan CI ni seguridad existentes.
- Los nombres de las seis Tasks Javier se normalizan por minuto, preservando ID, RRULE, propietario, historial y estado habilitado. Las dos Alex permanecen pendientes de actualización física desde su cuenta, con propuesta 07 :05 y 08 :35.
- Pausas automáticas de proveedor: no garantizables ni anulables por prompt; registrar requires_user_action y alertar sin bypass. Mantener throughput, sin nuevos PR, ramas, rulesets ni Tasks.

## 2026-11-12 - G0-005.E - readback operativo DEV negativo dirigido (Javier :50)
- Fuente Plan Comercial Inicial V3 G0-005.E, predecesora G0-005.D LISTO. Clasificador determinista sin llamadas externas para distinguir alias SSO protegido, upstream API 503, proyecto Vercel ajeno, READY de SHA distinto y evidencias incompletas.
- 15 casos sintéticos con Node test; no certifican un despliegue real, paridad externa, login, tenants ni estado LISTO. Resultado positivo solo EVIDENCE_CONSISTENT_NOT_LIVE_ATTESTATION; evidencia insuficiente NO_GO.
- Archivos scripts/quality/g0-005-e-operational-readback.mjs y scripts/tests/g0-005-e-operational-readback.test.mjs. Rollback: revert selectivo del commit directo en dev sin force; sin cambio de runtime, DB, secretos ni QA/main/PROD.

## 2026-11-12 - G0-004.G - rechazo dirigido de E2E falso verde (Javier :40)
- Alcance exclusivo del Standard Comercial V3 G0-004.G: casos sintéticos de retry tras fallo, skip por configuración de secreto no disponible, navegador omitido y fallo global de worker. Se comprueba FLAKY_REVIEW/INCOMPLETE/INVALID_REPORT/FAIL; control positivo limpio PASS. No equivale a E2E browser real ni solicita leer secretos.
- Script dirigido `scripts/tests/g0-004-g-directed-negative.test.mjs`, compara clasificador existente sin cambiar API, JWT, empresa, MySQL ni frontend productivo. Reversión: revert protegido exclusivamente de esta prueba y nota de CHANGELOG en dev, nunca force-push.

## 2026-11-12 - G0-004.F - fixture medible de resultado E2E (Javier :40)
- Condicion seleccionada: Playwright JSON con 1 prueba pasada y 1 error global de setup; el verdict esperado es FAIL, la salida CLI es codigo 2 y no reproduce detalles del error. Control positivo: quitar el error global deja PASS. Son fixtures sinteticos y pruebas Node; NO equivalen a una ejecucion E2E real.
- Fuente Plan Comercial V3 G0-004.F; depende de G0-004.A LISTO, reusa clasificador `scripts/quality/playwright-outcome-classifier.mjs`. Regresion y fixture versionados en `scripts/tests/g0-004-f-global-setup-error.test.mjs` y `scripts/tests/fixtures/g0-004-f-global-setup-error.json`; rollback por revert protegido solo del commit directo en dev, sin force ni QA/main/PROD.

## 2026-11-12 - G0-004.A - cierre causal de clasificacion Playwright (Javier :40)
- Fuente Plan Comercial Inicial V3 G0-004.A; reutiliza G0-004.C/D/E LISTO. Corrige falso PASS si Playwright JSON contiene `errors` globales de setup/worker con tests positivos. Reportes con `errors` malformado son INVALID_REPORT. Errores globales solo cuentan numericamente, sin exponer titulos, PII, tokens ni contenido de errores.
- Regresion dirigida Node `scripts/tests/g0-004-a-playwright-global-errors.test.mjs`: positivos limpios, fallos globales, reporte sin pruebas, metadatos invalidos, skips, flakes, CLI no cero y no filtracion. Sin atribuir E2E navegador real, MySQL o auth a pruebas sinteticas.
- Archivos: `scripts/quality/playwright-outcome-classifier.mjs`, test nuevo y este CHANGELOG. Rollback: revert protegido del commit directo en dev, sin reescribir historia ni tocar main/qa/prod. A1-A4 y PLAN/COLA/BITACORA requieren readback exact SHA.

# SOLQARYN — Cambios operativos vigentes

## 2026-11-12 — Preparación documental de Alex 2/2 para eventual 6→8
- IDs Alex existentes preservados; horarios físicos registrados :10/:50 y nombres SECONDARY REVIEW 01/03 no alterados. Propuesta pendiente de decisión del titular: PRIMARY DEV 07 minuto :05, PRIMARY DEV 08 minuto :35, sin tocar las seis Tasks de Javier.
- Se deja handoff para sincronización física por Alex de prompts V13 direct-dev, readback real de identidad/horario/prompt y habilitación posterior únicamente por autorización expresa.
- Cuatro auditorías V13 sin cambios. Sin nuevas Tasks, ramas, PR, cambios funcionales o modificaciones de QA/PROD.


## 2026-11-12 — G0-006.E prueba negativa de fases retiradas (Javier :30)
- Cinco pruebas Node dirigidas al validador de trazabilidad de DEV directo: rechazan FASE8, M13, recibos de PR históricos, estados VALIDANDO disfrazados de LISTO, tests SKIPPED, SHA falso, auditoría A4 ausente y readback institucional no comprobado.
- Casos sintéticos, sin reabrir migraciones ni autorizar trabajo ajeno; el control del Plan Comercial sigue siendo externo al validador de forma intencional.

## 2026-11-12 — G0-004.E regresión de evidencia E2E fail-closed (Javier :30)
- Añadidas ocho pruebas Node sobre clasificador Playwright JSON vigente: reintentos fallidos/flaky, skip por secreto opcional, navegador no soportado, suite anidada, reporte vacío/inválido y CLI con salida distinta de cero; un PASS limpio es el control positivo.
- Pruebas sintéticas sin secretos o datos de empresas; solo cobertura de calidad, no autorización de despliegue ni certificación E2E contra navegador real.

## 2026-11-12 — Visibilidad exacta de VALIDANDO y paralelismo real por Scope
- Precisa la transición inmediata a EN_PROGRESO al reclamar microtarea; VALIDANDO sólo al disponer de candidato completo, ANTES de auditorías funcional, seguridad e integración; misma Task corrige sin abandonar la custodia; commit directo dev, cuarta auditoría posintegración y LISTO verificado. Otras Tasks trabajan en paralelo IDs libres. Meta 1/3 por corrida sin falsos cierres.


## 2026-11-12 — Limpieza definitiva de V13 directo en dev
- Reemplazados capítulos históricos contradictorios por un único contrato directo en docs/AUTOMATION_V13.md.
- Censo original y seis horarios Javier conservados, sin reactivación. Alex permanece fuera del control de la cuenta Javier.
- Cuatro auditorías distintas por microtarea, objetivo operativo mínimo 1 LISTO y meta 3 por corrida, commits fast-forward en dev, resolución causal y readback PLAN/COLA/BITACORA.
- Reglas antiguas de PR protegido, espera de approvals, checks bloqueantes, 52 PR FIFO, microfases y pilotos retiradas de las instrucciones activas.
- La historia anterior sigue disponible a través de commits anteriores de GitHub, pero no forma parte del contrato operativo actual.
- No hay cambios funcionales en frontend, backend, esquemas, QA/main/PROD, secretos, proveedores ni Tasks físicas.

## 2026-11-12 — G0-006.D: trazabilidad de cierre directo por cuatro auditorías
- Añadido verificador asesor y pruebas negativas del contrato requisito → código → pruebas → cuatro auditorías → SHA fast-forward dev → readback PLAN/COLA/BITACORA; un recibo PR-only o incompleto no acredita cierre directo.
- El contador histórico preserva sus eventos anteriores pero reconoce recibos DIRECT_DEV_FOUR_AUDITS únicamente cuando están completos y verificables. No concede LISTO, permisos, push ni modifica seguridad/tenants ni arquitectura.

## 2026-11-12 — G0-005.C: readback de DEV, rutas canonicas
- Delimitada la evidencia de equivalencia Vercel/DEV a scripts/vercel-dev-runtime-equivalence.mjs: las rutas comparadas no pueden tener segmentos vacios, separador Windows, caracteres de control ni terminacion de directorio.
- Anexadas cuatro negativas de la misma prueba offline, conservando positivos y el rechazo de cambios runtime/backend. No se infiere despliegue READY ni se toca QA/main/PROD.
- Rollback: revertir solo este bloque del normalizador y las cuatro pruebas, sin cambios en API, migraciones, datos ni credenciales.

## 2026-11-12 — G0-004.D: clasificar resultados Playwright E2E
- Nuevo CLI scripts/quality/playwright-outcome-classifier.mjs para resumir reportes JSON sin nombres ni datos de tests; PASS exige ejecuciones limpias, FAIL/FLAKY_REVIEW/INCOMPLETE/INVALID_REPORT no se cuentan verde.
- Cuatro tests Node con casos positivos, negativos, flaky, skip, reportes invalidos y exit codes 2/3; sin modificar configuracion de runner, secretos, rutas de tenants, frontend ni backend. Rollback: revertir exclusivamente script y test.

## 2026-11-12 — G0-005.D: validación canónica de manifiesto DEV/Vercel
- `scripts/vercel-dev-runtime-equivalence.mjs`: fail-closed de rutas del manifiesto `frontend_before/after` usando segmento top-level canónico, sin `.`/`..` ni separadores Windows/control. Agregadas 4 negativas al self-test offline, positivos preservados.
- Solo contrato de readback, no prueba de un despliegue READY real. Sin cambios de Vercel, Render, credenciales, datos, frontend/backend ni QA/PROD. Rollback: revertir esta validación y las cuatro negativas.

## 2026-11-12 — G0-004.H: regresión positiva del clasificador Playwright E2E
- Nuevo `scripts/tests/g0-004-h-positive-regression.test.mjs`: caso positivo de tres pruebas PASS anidadas y CLI exit 0, junto con negativos FAIL/FLAKY_REVIEW/INCOMPLETE/INVALID_REPORT y sanitización de títulos/errores sintéticos.
- A1 funcional y A2 seguridad fail-closed; A3 integración: Node 22 `node --test scripts/tests/g0-004-h-positive-regression.test.mjs` 4/4 PASS y `node --check` de test y módulo. Módulo original blob `c5085b044ffb0b4ffa8d971ab3280afb4a02c037` sin cambios; no significa ejecución real de navegador Playwright.
- Cambio limitado a test + CHANGELOG, sin runtime, dependencias, secretos, JWT/RBAC, tenant ni datos. Rollback: revertir sólo test G0-004.H y este bloque aditivo.

## 2026-11-12 — G0-004.B: matriz de recibos G0-004.F/G/H y regresión E2E sintética
- Matriz `docs/quality/g0-004-b-e2e-receipts.md` identifica 10 condiciones, rutas y negativos: setup global, worker error, retries false-green, skips por secret y navegador, éxito anidado y sanitización. Nuevo test `scripts/tests/g0-004-b-quality-receipts.test.mjs`: 4/4 Node22 PASS, source classifier exact blob c5085b; no se presenta como una ejecución real del navegador.
- A1 funcional, A2 seguridad fail-closed y A3 integración Node dirigida PASS; no altera runtime, tenants, JWT/RBAC, secretos o dependencias. Rollback selectivo: revertir este test, la matriz y el párrafo de changelog.

## 2026-11-12 — P5-005.C: delimitacion del formulario de anulacion (Javier :30)
- Archivo real inalterado `frontend/src/app/shared/anular-dialog.component.ts`, blob `d751503438f963f1deebe371cd14623ad6038e16`. DELIMITAR, no implementar un modal universal.
- `scripts/tests/p5-005-c-delimitacion-modal-formulario.test.mjs` y `docs/P5-005-C-DELIMITACION-MODAL-FORMULARIO-DEV.md`: Node 22.16.0 `node --check` PASS y `node --test` 6/6 PASS tras VALIDANDO sobre archivo real exact-blob. Negativos de binding, cancelacion, motivo vacio, HTML crudo y dependencia FormsModule; secretos añadidos no detectados.
- A0-A3 dirigidas a delimitacion estatica real. No acredita navegador Angular, API/JWT/RBAC, MySQL ni datos reales. Sin cambios de runtime, paquetes, migraciones, secretos ni infra. Rollback: revert selectivo normal de estos tres archivos; A5 pendiente independiente antes de promocion.

## 2026-11-12 — P5-011.C: delimitación de lista extensa en modal de inventario (Javier :50)
- TARGET_PATH único de producto sin modificar: `frontend/src/app/features/inventario/ajustes-list.component.ts`, blob `01dc08065d6bdcde789a2af467fa749dbb385de0`. Modal bounded-scroll con contador de detalles, sin lista extensa virtualizada; listado principal con `mat-paginator` fuera del modal.
- Prueba estructural Node `scripts/tests/p5-011-c-delimitacion-listas-modal.test.mjs` y acta `docs/P5-011-C-DELIMITACION-LISTAS-MODAL-DEV.md`, positivo y seis negativos de regresión; no se afirma navegador, E2E, API/DB ni tenancy live.
- Sin cambios Angular runtime, dependencias, migraciones ni secretos. A4 debe verificar commit DEV exacto y readback; A5 independiente antes de QA/main/PROD. Rollback selectivo normal, sin force-push.

## 2026-11-12 — P5-006.D: reuso de confirmación institucional (Javier :50)
- REUSE_NO_RUNTIME_CHANGE: `frontend/src/app/shared/confirm-dialog.component.ts` blob `f9b73544cc70beba83a891b2fc89537f0718af8b`, Cancelar=false, Eliminar=true/warn, textos interpolados y Angular Material existente.
- Se reutiliza sin duplicación el test exacto `scripts/tests/p5-006-c-delimitacion-confirmacion.test.mjs` blob `baccb39ccc8845658d6d19773b4cdc4d70fa09fc`; 6/6 Node --test PASS en archivos SHA exactos. Acta `docs/P5-006-D-REUSO-CONFIRMACION-DEV.md`. No se certifica navegador, autorización JWT/RBAC ni QA/PROD.
- Revert selectivo aditivo, A4 exact-commit y A5 independiente pendiente.


## Orden posterior expresa de arranque V5 — 2026-11-12 (prevalece sobre la pausa del 20:56)

- El propietario AUTORIZA la reanudación de las seis Tasks JAVIER_CHATGPT únicamente después de certificar todos los checks requeridos en SUCCESS en el SHA exacto del nuevo commit base de cierre. Hasta el readback de reanudación, los seis `is_enabled=false` continúan describiendo el estado físico real; la pausa anterior es HISTÓRICA y deja de ordenar la ejecución una vez cumplida esta condición.
- Nueva fuente de trabajo: Google Doc canónico `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, V5 FINAL; Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. V5 contiene 2.340 microtareas `C00-01.M01.01` a `C15-05.M06.06`, 16 bloques, 65 fases y 390 microfases. El antiguo Plan Comercial V3/V13 y sus IDs históricos no son fuente de nuevos claims; V13 A0–A5 conserva sólo el protocolo de calidad y ejecución.
- `PLAN_MAESTRO!H` es estado único editable con evidencia y responsable; `COLA!A:O` son fórmulas y `COLA!P:Q` calculan dependencias explícitas por fase sin tope de seis. Candidata real: PENDIENTE, sin responsable, `COLA!G=SI` y `COLA!O=SI`, permisos reales, evaluación de factibilidad y exclusión por TASK_ID/ID. No reclamar automáticamente todo lo que esté en SI ni crear duplicados; múltiples IDs independientes pueden avanzar en paralelo.
- Mismo contrato V13: tareas individuales de tamaño ejecutable; PENDIENTE → EN_PROGRESO → VALIDANDO → LISTO únicamente con A0–A4 PASS, commit fast-forward en dev, CI causal SHA exacto y readback PLAN/COLA/BITACORA; A5 por otro TASK_ID para promoción QA/main/PROD, nunca autopromoción. Meta 1 LISTO, hito 3 no techo; continuar mientras haya tiempo/proveedor, dependencias y seguridad. No crear ramas/PR, no modificar QA/main/PROD ni datos existentes de usuarios/empresas.
- RESTAURAR vigilancia y recuperación cruzada exclusivamente entre seis Tasks Javier, IDs y horarios originales :00/:10/:20/:30/:40/:50. Una Task habilitada comprueba los seis IDs y puede intentar `is_enabled=true` UNA vez por compañera pausada si tiene permisos, no existe nueva orden de pausa ni acción humana obligatoria sin resolver; releer estado y registrar evidencia o error. Nunca suplantar, aprobar permisos ni reactivar Tasks de Alex desde Javier. Conservar sus dos IDs :05/:35 para gestión autónoma de su propietario y recuperación mutua sólo en esa cuenta.
- La eliminación física de filas/columnas auxiliares está verificada: DASHBOARD 40×10; CONFIG 150×3; LEYENDA 80×3; TAREAS_PROGRAMADAS 20×26; AUTOMATIZACIONES 20×18; PLAN_MAESTRO 2341×15; COLA 2341×17. BITACORA no tiene límite de crecimiento por esta decisión. Ningún bloqueo estructural impide crecimiento legítimo de BITACORA.
- C10-03 no requiere migrar imágenes inexistentes: inventario real cero → no hay exportaciones ni checksums históricos. Mantener capacidad de archivos futuros segura. La tarea `C15-05.M06.06` es terminal y sólo se habilita con evidencia de cierre de las 2.339 anteriores.
- Jira `KAN-4` es tablero informativo, NO fuente de backlog duplicado ni gate para LISTO. Los respaldos de la estructura y de los seis prompts antes de reactivación quedan preservados; no cambiar títulos, RRULE, zonas, roles, IDs, parámetros de razonamiento ni vigilancia existente fuera del ajuste temporal de pausa.
- Validación de arranque: CI exact-HEAD requerida = SUCCESS, Scope Lock SUCCESS, fuente Google Sheet íntegra, conectores autorizados, y readback de cada Task tras reanudar. Un check SKIPPED puramente auxiliar se identifica y no se falsea como PASS. Un fallo crítico o acciones humanas pendientes bloquean sólo la operación correspondiente.
