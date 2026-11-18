## 2026-11-17 — Corrección A5_FAIL P2 C01-02.M03.04 y recuperación de cola [ChatGPT directo]
- Se elimina mutabilidad de `C0102IsolatedCapabilityPolicySnapshotAdapter`: copias `FrozenSet` ordinales sustituyen `HashSet` mutable expuesto como `IReadOnlySet`; dos xUnit adversariales reproducen el fallo A5 y conservan denegación/aislamiento. Sin cambios productivos ni promoción.
- El histórico A5_FAIL se conserva. Los candidatos C01-02.M03.05/06 no se habilitan sin CI exact-SHA y nueva revisión A5 por otra Task Javier; no se modifica DAG/COLA para simular capacidad. 

## 2026-11-17 — V19 C15-05.M05.03 integración proveedor GitHub Checks real [Javier :40]
- Se agrega prueba Node de proveedor GitHub Checks API HTTPS real usando GITHUB_TOKEN efímero de permisos checks:read y contents:read; exige SHA exacto y rechaza contenido truncado o ambiguo.
- Suite de siete casos positivos/adversariales con resultados SUCCESS, SKIPPED/CANCELLED/IN_PROGRESS, check ausente, SHA/repo inválido, respuesta HTTP 403 y página incompleta; GitHub-hosted requiere 7/7 y Scope Lock, sin atribuir PASS antes de CI.
- R2 solo tests de integración externa y documentación, no cambios a JWT/RBAC/tenants/MySQL ni PROD. Mantener VALIDANDO/owner mientras CI externa y requerir A5 independiente.

## 2026-11-17 — V19 C15-05.M05.05 negativos de autoría y abuso [Javier :30]
- Doce pruebas xUnit adversariales para el adaptador V5 **en memoria**: acceso no autorizado, ID fuera de catálogo, gate DAG, token/replay, owner ajeno, SHA falso, A5 propia y duplicación inicial; sin confundir estas pruebas con CAS distribuido, JWT o MySQL.
- Workflow GitHub-hosted .NET 10 Release TRX 12/12 exigible, Scope Lock y V5 exact-SHA; mientras CI externa ejecuta conservar VALIDANDO, A5 únicamente otra Task Javier y rollback git revert.

## 2026-11-17 — V19 C15-05.M05.04 reparación causal CI Scope Lock [Javier :20]
- Se conserva el verificador de Scope Lock sin cambios y se ejecuta sobre checkout limpio antes de npm ci; el job inicial falló al recorrer documentación de terceros dentro de node_modules instalada por el workflow.
- Retener fallo histórico y revalidar nueva CI exact-SHA Angular/Chromium (tres casos); sin asumir PASS, sin JWT/tenant/MySQL live ni QA/main/PROD.

## 2026-11-17 — V19 C15-05.M05.02 contrato API/DTO de auditoría [Javier :30]
- Nueve pruebas xUnit Release de ruta/permiso, Forbid sin lectura, DTO paginado, filtros, límites y fallos no contados como OK; solo invocación directa al controlador existente, sin alterar runtime, sin atribuir JWT/HTTP/MySQL real.
- Workflow GitHub-hosted con TRX estricto 9/9 y Scope Lock, V5 exact SHA; A5 requiere otra Task Javier. CI QUEUED/IN_PROGRESS nunca PASS, dev exclusivo, rollback con git revert.

## 2026-11-17 — V19 C15-05.M05.04 aceptación positiva con Chromium real [Javier :20]
- Tres pruebas Playwright en ruta Angular privada: 320/1280 sin desborde, controles de solo lectura, teclado Tab/Enter real y cero solicitudes HTTP mutantes; sesión de prueba sintetizada, no JWT real.
- CI dirigida Node 24/Angular 22.2.1/Chromium, Scope Lock y V5 exact SHA. No afirmar PASS antes de RUN, ni seguridad/tenant/MySQL de backend o WCAG/performance global. A5 siempre independiente.

## 2026-11-17 — V19 C15-05.M05.01 contrato xUnit de múltiples IDs y CAS en memoria [Javier :20]
- Nueva regresión xUnit .NET 10 para dos IDs independientes: dos ganadores bajo 64 intentos, idempotencia, gates cerrados, propietario ajeno y capacidad; no se confunde adaptador in-memory con candado Google Sheets V17.
- CI GitHub-hosted dirigida con TRX obligatorio y 10 casos esperados; no se atribuye PASS antes del resultado exact-SHA. A5 independiente de otra Task Javier, dev exclusivo, no QA/main/PROD.

- Corrección causal antes de cierre: el primer conjunto declaraba 10 casos pero solo enumeraba 9; se agrega caso adversarial de SHA inválido con estado aislado inmutable, dejando 10 ejecuciones exigibles en TRX. No aceptar conteo falso ni SKIPPED.

- Consistencia de observabilidad: el título del job dirigido se corrige a diez casos, alineado al conteo TRX ejecutado; ninguna prueba se considera PASS antes de finalizar la CI.

## 2026-11-17 — V19 C15-04.M05.04 regresion AST de rutas importadas [Javier :10]
- A0.23: prueba dirigida verifica composicion real del router Angular (ocho modulos, spreads anidados) y evita el falso positivo donde una ruta no registrada igualmente redirige a login. CI Chromium esperada: 23 casos; JWT firmado/tenant A-B y auditoria UX global NO_MEDIDO hasta pruebas reales.

## 2026-11-17 — V19 C15-05.M04.06 densidad y carga progresiva verificables [Javier :50]
- Suite Playwright/Chromium nueva para 320/390/768/1280 px, indicador de espera accesible, bloqueo de doble revalidación, 503 recuperable y 403 fail-closed; usa sesión sintética sin JWT real ni producción.
- Selector exacto para estados 503/403: el componente expone dos alertas durante fallos; evitar expect estricto ambiguo y verificar el mensaje seguro concreto. No se alteró runtime.
- CI GitHub-hosted evidenció 58 PASS / 2 FAIL por dos roles `status` (anuncio de página y estado del componente), no por fallo de producto. Corrección mínima: localizar texto de estado visible y evitar selector ambiguo; nueva CI exact-SHA obligatoria, fallo histórico conservado.
- Workflow existente ampliado sin cancelar SHA ajenos; A0–A4 condicionados a checks exact-SHA, PLAN/COLA/DAG/BITACORA y A5 de otra Task Javier. No se declaran latencias o Core Web Vitals medidos.

## 2026-11-17 — V19 C15-05.M04.05 prueba P2 real de Tab y mutación adversarial [Javier :40]
- Se reemplaza foco programático .focus() por recorrido de Tab real acotado y Enter, para verificar accesibilidad del enlace de retorno sin falso PASS.
- Nueva regresión negativa DOM efímero tabindex=-1: el enlace fuera del orden de teclado no recibe foco. Se mantiene A5_FAIL previo, autor DEV01, no hay cambios runtime/API; nueva CI exact-SHA y A5 ajena obligatorias antes de certificar, sin QA/main/PROD.

## 2026-11-17 — V19 C09-04.M06.06 matriz terminal DEV dependencias reales [Javier :20]
- Se entrega matriz R2 documental de las seis M05 y cinco M06 con SHA, A5 independientes, correcciones P2, pruebas xUnit/Chromium/MySQL 8.4 y sus limites; preserva checks V5 no ejecutados, fallos A5 historicos y la brecha P1 de formulas DAG, sin desbloqueo artificial.
- El cierre dev exige CI exact-SHA, Scope Lock/V5, readback PLAN/COLA/DAG/BITACORA y A5 de otra Task Javier; no certifica proveedores vivos, JWT HTTP global, Aiven, QA, main ni PROD. No se hacen cambios de runtime ni secretos.

## 2026-11-17 - V19 C13-04.M05.04 extensiones sectoriales A/B [Javier :00]
- Prueba Chromium privada con JWT firmado y tenant A confirmado: pantalla sectorial informativa visible, cuatro capacidades sin conectar, anonimato y falsificación B rechazados por backend; CI exact-SHA por verificar, P1 global permanece abierto.

## 2026-11-17 — V19 C09-04.M06.04 reparación P2 ASCII E164 y aislamiento MySQL [Javier :40]
- Se corrige el desacuerdo dominio/SQL: sólo dígitos ASCII tras '+'; los decimales Unicode no deben pasar validación de dominio ni llegar a persistencia. Se mantienen los límites E.164, migraciones y referencias opacas.
- Nueva regresión adversarial en MySQL 8.4 efímero de dos empresas A/B, con árabe-índico/fullwidth negativos y cero mutaciones; workflow dirigido exige TRX 5/5 y checks Scope Lock/V5 exact-SHA antes de nueva A5 independiente. El A5_FAIL P2 anterior permanece; DEV06 conserva autoría, DEV05 aporta reparación. Sin QA/main/PROD.

## 2026-11-17 — Reconciliación estricta de historia Oracle 18/18 [ChatGPT directo]
- La migración E164 V19 es activa y válida; dos workflows verificaban 17 migraciones en vez de las 18 reales. Se actualiza la expectativa exacta y se comprueba el ID Oracle nuevo, sin deshabilitar checks. dev-ci también verifica el ID, preservando 107 fuentes históricas. Se requiere CI completa y A5 independiente para cierre C09-04.M06.04.

## 2026-11-17 — V19 C15-04.M05.04 cobertura negativa de rutas importadas [Javier :10]
- A0.22: Playwright incorpora 8 fuentes de rutas importadas y 51 declaraciones protegidas; 102 navegaciones anónimas reales 375/1280 px exigen redirect al login, cero mutaciones HTTP y guardas authGuard, sin proclamar prueba JWT/tenant backend.
- Workflow Chromium pasa a 22 escenarios; el CI del SHA publicado debe revisarse antes de contar PASS. R2 global sigue EN_PROGRESO DEV02, lock V17 2000002297; A5 requiere revisor distinto. Rollback mediante git revert normal.

## 2026-11-17 — Migración E164 registrada en ensamblado Oracle activo [ChatGPT directo]
- Se mueve la migración V19 de la carpeta histórica a `Infrastructure.Migrations/Migrations` y se exige desde xUnit que su ID exista en `GetMigrations` y `GetAppliedMigrations` de MySQL 8.4 efímero. Se actualiza el workflow dirigido y la documentación; Scope Lock, V5 y Oracle CI se validan en el SHA nuevo, sin despliegue ni aprobación artificial.

## 2026-11-17 — Reconciliación snapshot Oracle C09-04.M06.04 [ChatGPT directo]
- El CI Oracle baseline reveló has-pending-model-changes tras el E164: se sincroniza exactamente CK_ConfiguracionesWhatsAppEmpresa_Numero_E164 en el snapshot Oracle real de Infrastructure.Migrations con el modelo EF, sin cambiar tablas de producción ni debilitar los controles.
- Fuente original 9d131c4b: 4/4 MySQL 8.4 A/B PASS y seguridad PASS; Oracle baseline CI tenía FAIL y exige nueva verificación en SHA descendiente antes de cerrar VALIDANDO. No falsificar A5 ni aprobación productiva.

## 2026-11-17 - V19 C13-04.M05.04 firma JWT adulterada [Javier :00]
- Regresion Chromium: JWT original firmado 200, firma adulterada 401 y pantalla privada rechazada; CI externa exact-SHA y P1 global pendientes.

## 2026-11-17 — V19 reparacion E164 y rollback C09-04.M06.04 [ChatGPT directo]
- El CHECK MySQL 8.4 rechazaba +50499990001 valido: se sustituye el escape fragil por [+] en modelo/snapshot y se crea migracion forward-only sin reescribir N76C ni tocar produccion.
- Se restaura prueba MySQL de desactivacion A, preservacion B, transaccion rollback y test outbox A/B; dos negativos host/puerto. CI dirigida espera 4/4 con Scope Lock/V5 exact SHA. No declarar PASS o LISTO sin pruebas, autorizacion y A5 independiente.

## 2026-11-17 — Reparación A5_FAIL P2 C02-04.M05.06 [Javier :30]
- La revisión DEV06 rechazó cobertura global 2/15. Se amplía Playwright a 15/15 rutas reales, negativo anónimo/403 sintético, navegación del inventario de inspección y medición de retención HTTP sintética 225 ms; seis tests en Chromium real, sin afirmar laboratorio QA autónomo, JWT real ni latencia de producción.
- Se conserva A5_FAIL histórico. Publicación y CI causal exact-SHA, Scope Lock/V5 y nueva A5 ajena requeridos; no se certifica PASS antes de concluir GitHub Actions ni se toca QA/main/PROD.

## 2026-11-17 — V19 C15-04.M05.04 guardas anonimas del router completo [Javier :10]
- A0.21: se agrega una prueba Chromium causal que deriva 88 rutas protegidas directas (20 parametros) y exige denegacion anonima a 375/1280 en 176 navegaciones, sin efectos HTTP mutantes; CI exact SHA pendiente antes de PASS.
- No equivale a autorizacion backend, JWT real, roles/tenant A-B, rutas importadas, WCAG ni Web Vitals global; mantener EN_PROGRESO DEV02 y lock 2000002297, A5 independiente. Rollback git revert normal.

## 2026-11-17 — V19 C13-04.M05.04 revocacion de membresia A con JWT real [Javier :00]
- Se agrega un caso Chromium que verifica 200 para membresia y pantalla privada A, revoca la asignacion A en MySQL 8.4 efimero, exige rechazo 403/404 y regreso a login, y restaura 200 desde un bloque finally; no se simula HTTP ni JWT.
- Cambio R2 de prueba y evidencia, sin alterar producto, backend ni base productiva. Exigir GitHub Actions, Scope Lock y V5 exact-SHA antes de contar PASS; el P1 de matriz global ERP sigue abierto. VALIDANDO, sin LISTO, auto-A5 ni QA/main/PROD.

## 2026-11-17 — V19 reparación CI C02-04.M05.06 Scope Lock antes de npm ci [Javier :30]
- RUN 44172058206 falló antes de Chromium: el verificador de Scope Lock recorrió frontend/node_modules/playwright-core luego de npm ci y marcó contenido distribuido por Playwright. Se conserva íntegro el verificador, pero se ejecuta primero sobre checkout limpio; después corren npm ci, lint, build y los cuatro tests reales.
- No se cuentan como PASS el run fallido ni los tests que aún no han corrido. CI nueva exact-SHA, seguridad/V5/Scope Lock y A5 independiente siguen siendo exigibles; sin QA/main/PROD.

## 2026-11-17 — V19 C02-04.M05.06 regresión Chromium global [Javier :30]
- Se agregan cuatro pruebas reales Chromium para catálogo privado, 320/375/768/1280, Tab/Enter desde inicio, foco, contraste computado y error 503 con recuperación, mediante sesión de servidor sintética declarada.
- Riesgo R2 de superficie de autorización sin modificar JWT, MFA, MySQL, grants, API ni producción. CI GitHub-hosted, Scope Lock, V5 y A5 independiente pendientes de acreditación causal del SHA; sin atribuir PASS anticipado.

## 2026-11-17 — V19 C12-04.M06.05 solicitud de revisión independiente de MySQL privado [Javier :30]
- Se entrega una solicitud R0 documental y matriz trazable de seis M05 y cuatro M06: SHAs, CI dirigida, riesgos y prueba adversarial; M06.04 permanece VALIDANDO y M06.06 no queda autorizada por esta solicitud.
- No se acredita restore Aiven, checksum/conteos de datos reales, JWT/RBAC servidor, RPO/RTO, cutover ni QA/main/PROD. A0–A4 y Scope Lock/V5 del nuevo SHA se comprobarán causalmente; A5 se reserva a otra Task Javier.

## 2026-11-17 — V19 C15-04.M05.04 contraste de errores de cuenta [Javier :10]
- Incremento Chromium numero 20: contraste computado de error >=4.5:1, objetivos de pestañas >=44px, sin escrituras HTTP en 375/1280; Scope Lock, V5, tipado, build y CI exact-SHA pendientes de comprobar.
- Auditoria UX global R2 sin terminar; no se acredita JWT firmado, RBAC, tenant A/B, WCAG completo, rutas privadas ni Web Vitals globales. Owner DEV02, WIP/lock V17 conservados; A5 independiente; rollback git revert.

## 2026-11-17 — V19 C02-04.M05.02 reparación P2 límite de respuesta QA [Javier :50]
- Se detectó que `Content-Length` ausente en respuesta GitHub chunked permitía lectura JSON sin tope. Reparación R2 acota la lectura real a 512000 bytes y devuelve 503 sin exponer payload ni auditar éxito cuando se supera el límite.
- Nueva regresión TestServer/JWT firmado con respuesta chunked válida positiva y JSON sobredimensionado negativo. CI .NET10/MySQL8.4, Scope Lock y V5 del SHA nuevo aún por ejecutar; A5_FAIL histórico preservado, A5 correctiva pendiente, sin QA/main/PROD.

## 2026-11-17 — V19 C09-04.M06.02 reparación P2 procedencia V5 [Javier :30]
- Se rectifica la atribución de CI: check V5 exact-SHA SUCCESS en M05.01/02/03/04/06; M05.05 SHA 76ddaee9 conserva Scope Lock y 11/11 xUnit, pero cero check V5 entre 29 check-runs; NO_EJECUTADO/NO_ACREDITADO, nunca PASS.
- R0 documental únicamente, authored DEV06/reparación DEV04, A5_FAIL histórico conservado y nueva revisión independiente necesaria. Candado V17; sin cambios de código, DAG/COLA, tenants, JWT, MySQL, QA/main/PROD.

## 2026-11-17 — V19 C15-04.M05.04 validación accesible de cuenta [Javier :10]
- Se agregan errores asociados a campos invalidos de login/registro públicos mediante aria-describedby/aria-invalid y nueva regresión adversarial Playwright a 375/1280; cero operaciones mutantes. Requiere CI exact SHA y 19 tests, aún no PASS.
- R2 global, JWT/roles/tenant A/B, WCAG y Web Vitals siguen SIN_VERIFICAR; EN_PROGRESO DEV02/lock V17, A5 independiente, rollback git revert.

## 2026-11-17 — V19 C15-04.M05.04 fronteras de cuenta sin mutaciones [Javier :10]
- A1/A3 parcial: nuevo Playwright de login/registro publicos a 375/1280, correo invalido, nombre ausente, clave de 9/10 caracteres, bloqueo de submit, atributos autocomplete y aria-describedby, cero mutaciones HTTP; 19 tests Chromium CI pendiente del SHA propio, sin declarar PASS.
- No se registran usuarios ni se invocan proveedores; A0-A4 global, JWT firmado/roles/tenant A-B, WCAG y Web Vitals globales siguen sin certificar; WIP DEV02/lock V17 retenido, A5 solo revisor Javier distinto.

## 2026-11-17 — V19 C15-04.M05.04 ampliar landmarks y teclado responsive [Javier :10]
- Incremento R2 parcial: 24 navegaciones reales de seis rutas storefront a 320/375/768/1280 px; main/skip-link/H1/overflow/HTTP sin mutaciones y Tab+Enter reales a 320/768. El workflow requiere 18 tests Chromium, Scope Lock y V5 exact-SHA; CI por ejecutar, nunca PASS anticipado.
- Global UX, JWT firmado, tenant A/B, roles, WCAG global y Web Vitals global permanecen SIN_VERIFICAR; EN_PROGRESO de DEV02, candado V17 retenido y A5 de otra Task. Rollback git revert ordinario.

## 2026-11-17 — V19 C13-04.M05.04 empresa A positiva y ruta parametrizada B denegada [Javier :00]
- Prueba Playwright R2 añadida: JWT firmado y contexto MySQL 8.4 efímero permiten abrir /perfil en empresa A; una solicitud local de empresa B no autoriza /productos/999999/editar y exige rechazo backend. CI exact-SHA y Chromium pendientes; no se atribuye PASS antes del run.
- Cobertura parcial, sin afirmar aceptación ERP global ni permisos granulares de todos los módulos; P1 sigue abierto, VALIDANDO y A5 exclusivamente por otra Task Javier. Reversión: git revert normal.

## 2026-11-17 — V19 C09-04.M06.01 rectificación causal A5_FAIL P2 [Javier :50]
- Corregida atribución V5: M05.01/02/03/04/06 tienen check V5 SUCCESS exact-SHA; M05.05 `76ddaee9` cuenta 29 check-runs y ninguno V5 (`NO_EJECUTADO/NO_ACREDITADO`), aunque Scope Lock y 11/11 xUnit sí PASS.
- Conserva A5_FAIL previo y solicita revisión independiente sobre SHA corregido; R0 docs-only sin desbloquear M06.06, QA/main/PROD, proveedores o JWT real.

## 2026-11-17 — V19 C09-04.M06.05 solicitud condicionada de A5 ajena [Javier :30]
- Solicitud R0 de revisión independiente de M05.01–06 y M06.01–04: reconoce A5_FAIL M06.01/M06.02 y M06.04 en VALIDANDO, sin fingir gate aprobado.
- No libera M06.06, QA/PROD ni certifica proveedor/JWT/MySQL productivo. Scope Lock/V5 y readback exact-SHA pendientes.

## 2026-11-17 — V19 C09-04.M06.04 reparación causal del ensayo de rollback [Javier :50]
- La CI real MySQL 8.4 de SHA 00cce68f falló 1/3 por CK_ConfiguracionesWhatsAppEmpresa_Numero_E164 al insertar E.164 sintético; fallo registrado en BITACORA, esquema N76C no reparado ni aprobado.
- Se acota a la alternativa institucional **rollback**: xUnit con dos tenants/outbox durables, transacción de estado seguida de ROLLBACK y relectura separada sin borrar datos; exige TRX 3/3 y SHA exacto. No certifica desactivación de proveedor, HMAC, JWT, A5_FAIL M06.01 ni QA/PROD.

## 2026-11-17 - V19 C15-04.M05.04 reparar selector login y censo [Javier :10]
- GitHub run44147705676 detecto fallo del test Web Vitals: buscaba h1 visible en /login, pero el formulario usa h2#login-title; reparado manteniendo las cuatro navegaciones, sin falso PASS (CI nuevo pendiente).
- Censo actual de rutas 161 declaraciones en 9 fuentes, 47 parametrizadas y 110 en app.routes.ts; es solo inventario estatico, no auditoria global completada. EN_PROGRESO DEV02; A5 distinta Task.

## 2026-11-17 — V19 C09-04.M06.04 rollback/desactivación sintética en MySQL 8.4 [Javier :50]
- Prueba de integración xUnit + workflow GitHub-hosted MySQL 8.4: desactivación idempotente de configuración WhatsApp A, tenant B intacto, dos mensajes outbox preservados y transacción revertida.
- R2 test-only, sin dispatch, JWT, HMAC ni proveedor real; A5_FAIL previo de M06.01 sigue abierto. CI exact-SHA y A5 distinta requeridas, sin QA/main/PROD.

## 2026-11-17 - V19 C15-04.M05.04 muestras reales Performance Timing [Javier :10]
- Chromium registra TTFB, FCP, LCP y CLS disponibles en tienda/login a 375/1280; ausencia explicita NO_MEDIDO. El registro se obtiene en navegador real, sin umbrales Web Vitals globales inventados.
- Se esperan 17 pruebas Playwright, Scope Lock y V5 exact-SHA; auditoria R2 global continua EN_PROGRESO sin LISTO o A5 propia.

## 2026-11-17 - V19 C15-04.M05.04 skip-link ofertas/carrito [Javier :10]
- Se hace enfocable por fragmento el main de productos/ofertas con tabindex=-1; regresion Playwright usa Tab y Enter en ofertas/carrito a 375/1280 px y comprueba hash, ruta, overflow y ausencia de escrituras.
- CI de 16 escenarios, Scope Lock y V5 necesitan SUCCESS exact-SHA antes de acreditar resultados; UX global R2 sigue EN_PROGRESO, sin LISTO ni A5 propia.

## 2026-11-17 — V19 C13-04.M05.04 inventario recursivo de rutas importadas y rechazo anonimo [Javier :00]
- Playwright agrega descubrimiento de imports anidados de rutas ERP del checkout y valida existencia de módulos; cuatro navegaciones negativas reales Chromium sin interceptar red a ajustes, facturas, pedidos y reporte hijo.
- Alcance R2 de aceptación parcial: la matriz estática y denegación anónima NO prueban viajes positivos empresariales, JWT por capacidad, rutas dinámicas ni cobertura global. P1 permanece abierto, VALIDANDO con revisión A5 ajena pendiente; CI exact-SHA requerida y rollback git revert normal.

## 2026-11-17 — V19 C09-04.M06.03 riesgos abiertos y decisiones multicanal [Javier :50]
- Registro R01–R10 y decisiones D01–D08: webhooks, replay, PII, JWT/RBAC/tenant, consentimiento, duplicados, accesibilidad y bloqueo R3, con evidencia M05.01–06 delimitada por SHA y run.
- Alcance R0 documental; MySQL 8.4/Chromium de pruebas previas no certifican proveedor vivo, webhook firmado, Aiven o producción; A5 independiente pendiente y rollback git revert normal.

## 2026-11-17 — V19 C09-04.M06.02 manifiesto reproducible de seis pruebas M05 [Javier :50]
- Se documentan versiones, SHA exacto, job IDs y resultados 7/8/3/3/11/4 en seis runs GitHub-hosted, con comandos read-only para verificar checks, diff, logs y Category=Integration MySQL 8.4 sin confundir mocks con proveedores.
- Evidencia R0 de documentos/CHANGELOG, límites JWT/webhook/PII/SMTP/WhatsApp no certificados, rollback git revert normal y A5 independiente; no tocar runtime, QA/main/PROD.

## 2026-11-17 — V19 C09-04.M06.01 auditoria causal M05, pruebas reales frente a mocks [Javier :50]
- Matriz de seis SHAs y seis runs GitHub-hosted: 7/7, 8/8, 3/3 MySQL 8.4, 3/3, 11/11 xUnit y 4/4 Chromium; separa persistencia real aislada y browser real de mocks de sesión/proveedor.
- Riesgos abiertos de webhooks, replay remoto, privacidad, JWT firmado y entrega SMTP/WhatsApp se documentan como NO_MEDIDO. R0 documental; no modificar runtime, COLA/DAG, QA/main/PROD; A5 independiente requerida.

## 2026-11-17 — V19 C09-04.M05.06 regresion, rendimiento y accesibilidad mensajeria [Javier :30]
- Playwright Angular real prueba reflow 320/375/768/1280, cota DOM/DOMContentLoaded, foco nativo Tab/Enter, 403 tras tema y 503 sin fuga ni escrituras.
- Workflow GitHub-hosted Chromium con build Angular, Scope Lock y V5 por SHA; sesion simulada, sin certificacion JWT firmado, MySQL, SMTP/WhatsApp ni QA/PROD. R2; A5 de otro Javier y git revert normal.

## 2026-11-17 — V19 C15-04.M05.04 skip-link válido en cuenta pública [Javier :10]
- Se corrige P2 de accesibilidad: /tienda/cuenta tenía un enlace de salto a #catalogo sin destino; ahora apunta al main #cuenta-principal con tabindex=-1 y etiqueta específica.
- Regresión Playwright con Tab/Enter real, 375/1280 px, ruta/hash sin escape, sin overflow ni escrituras. Se esperan 15 casos Chromium en CI; no declarar PASS antes de logs exact-SHA, ni auditar como global la UX R2 aún EN_PROGRESO.

## 2026-11-17 — V19 C13-04.M05.04 revocacion JWT real con rol global efimero [Javier :00]
- Nuevo Playwright Chromium comprueba que revocar el rol en MySQL 8.4 de CI invalida la autoridad de un JWT previamente firmado, deniega HTTP y ruta privada; restauracion de una fila en finally y readback.
- P1 de cobertura recursiva permanece abierto: VALIDANDO, CI exact-SHA Scope Lock/V5 obligatoria y A5 independiente; sin datos de produccion, rollback git revert.

## 2026-11-17 — V19 C09-04.M05.05 abuso y aislamiento negativo outbox [Javier :20]
- Once casos xUnit adversariales sobre servicio Application real y dependencias simuladas: identidad o tenant inválidos, membresía revocada, idempotencia CR/LF/TAB, replay de clave con payload diferente, repositorio que devuelve otro tenant y payload vacío; cero altas en rechazos.
- Workflow GitHub-hosted .NET 10 Release exige TRX 11/11, Scope Lock y checks de seguridad; no acredita middleware JWT real, MySQL, SMTP, WhatsApp ni producción. Riesgo R2; A0-A4 por SHA y A5 de otra Task Javier; rollback git revert normal.

## 2026-11-17 — V19 C09-04.M05.03 integración Oracle EF MySQL 8.4 [Javier :20]
- Prueba de integración xUnit sobre MySQL 8.4 efímero en GitHub-hosted: claims CAS persistidos en contextos diferentes, empresas A/B, reintento tras fallo, rechazo de confirmaciones stale, rollback transaccional y denegación de conexiones remotas.
- Workflow dirigido exige 3/3 TRX de integración reales, .NET 10 Release y Scope Lock exact-SHA. No ejecuta proveedores SMTP/WhatsApp/webhooks, JWT real, Aiven, QA ni producción. A0-A4 y A5 independiente pendientes hasta evidencia causal de CI; rollback normal por git revert.

## 2026-11-17 — V19 C09-04.M05.04 aceptación positiva outbox [Javier :40]
- Tres pruebas xUnit integran controlador/API y servicio Application reales con repositorio y autoridad simulados; comprueban estado Pendiente, una única alta ante replay y separación de eventos/empresa A-B con igual clave.
- Workflow GitHub-hosted .NET 10 Release, sin afirmar autenticación JWT, MySQL, HTTP real ni proveedores; CI exact-SHA A0-A4 y revisor A5 independiente. Solo dev, rollback git revert normal.

## 2026-11-17 — V19 C15-04.M05.04 teclado accesible de pestañas cuenta [Javier :10]
- ARIA tablist de /tienda/cuenta: roving tabindex, ArrowRight/Left, Home/End y panel etiquetado; sin cambios de backend, credenciales ni datos.
- Nueva regresión Chromium real con dos viewports, foco, ausencia de overflow y cero mutaciones HTTP. Total esperado 14 tests; CI pendiente, auditoría R2 global EN_PROGRESO sin A5 propia.

## 2026-11-17 — V19 C09-04.M05.02 contrato API/DTO seguro [Javier :40]
- Se añaden tests xUnit del controlador real de outbox con servicio simulado para respuesta 202/DTO, cabecera idempotente ausente/vacia, tenant A/B, conflicto 409, membresia revocada 403 y DTO invalido 400, con ProblemDetails opaco.
- Workflow dirigido .NET 10 Release por SHA; no afirma JWT firmado, middleware, MySQL, webhooks ni proveedores reales. Requiere CI exact-SHA, Scope Lock, V5, A0-A4 antes de LISTO y A5 por otra Task Javier. Solo dev; git revert normal.

## 2026-11-17 — V19 C13-04.M05.04 cobertura global estatica desde Router [Javier :00]
- Prueba Playwright adicional con JWT real y MySQL efimero que descubre las rutas de plataformaGuard en app.routes.ts y recorre cada ruta estatica declarada (19 en base verificada). Assert HTTP 200, pathname y no login; sin interceptar HTTP.
- La cobertura recursiva de rutas importadas, permisos finos y revocacion sigue P1: conservar VALIDANDO y A5 pendiente. Exigir CI exact-SHA y Scope Lock; no QA/main/PROD.

## 2026-11-17 — V19 C12-04.M06.04 reparación causal bootstrap Oracle EF [Javier :50]
- CI run 44121100598 falló realmente por tabla Empresas ausente al intentar OracleBaseline sobre esquema vacío; se conserva la evidencia y se corrige el setup para usar bootstrap canónico fresh en MySQL 8.4 efímero, con verificaciones físicas y segunda migración idempotente.
- Test causal pendiente de nuevo SHA, no atribuir PASS. No se modifica runtime o datos productivos; Aiven/RPO/RTO/cutover no probados; LISTO y A5 no autorizados por esta reparación.

## 2026-11-17 — V19 C09-04.M05.01 replay y revocacion outbox [Javier :40]
- Dos aserciones xUnit nuevas sobre el worker real: clave idempotente duplicada impide toda entrega; revocacion en segundo mensaje solo permite primera entrega y registra fallo opaco sin envio adicional.
- GitHub-hosted .NET 10 Release, sin activar SMTP/WhatsApp, webhooks, JWT real, MySQL, secretos o produccion; A0-A4 y revision A5 independiente por SHA.

## 2026-11-17 — V19 C12-04.M06.04 ensayo Oracle EF MySQL 8.4 efimero [Javier :50]
- Se amplía la CI de restauración sintética con job dependiente para intentar aplicar migraciones Oracle EF 10.0.12 sobre MySQL 8.4 de GitHub-hosted, reejecución idempotente y suite Integration A/B con TRX verificable.
- Hasta concluir CI exact-SHA, el resultado es PENDIENTE y C12-04.M06.04 permanece VALIDANDO. No valida Aiven, restauración real, RPO/RTO, cutover ni producción; A5 de otra Task Javier.

## 2026-11-17 — V19 C15-04.M05.04 rutas públicas ofertas/carrito/cuenta [Javier :10]
- Se añaden seis navegaciones reales Chromium (tres rutas, dos anchos) con H1, no-overflow y cero mutaciones HTTP; cuenta invitada verifica tabs y submit vacío deshabilitado.
- La CI incremental exige trece casos exact-SHA; la auditoría global R2 continúa EN_PROGRESO sin LISTO ni A5, pendientes roles, tenants y WCAG.

## 2026-11-17 — V19 C15-04.M05.04 aislamiento real de Tab por ruta [Javier :10]
- Chromium run 44114180266 registró 11 PASS/1 FAIL: foco residual en la tercera navegación de misma pestaña. Nueva pestaña real por ruta en el contexto Playwright, sin focus() artificial, conserva Tab/Enter, target y pathname.
- Reparación estricta del fixture, no bypass; CI de 12 casos exact-SHA aún requerida. Auditoría UX R2 global permanece EN_PROGRESO.

## 2026-11-17 — V19 C15-04.M05.04 contratos storefront fases 3 y 7 [Javier :10]
- Regresión phase3 CI detectó expectativa heredada de skip-link con hash aislado; se sustituye por href canonico /tienda/productos#catalogo-productos. Fase7 exige /tienda#contenido-principal.
- Solo se actualizan dos aserciones específicas, conservando cobertura E2E y CI obligatoria exact-SHA. La auditoria UX global continúa EN_PROGRESO, A5 pendiente de otra Task.

## 2026-11-17 — V19 C15-04.M05.04 regresion fase2 URLs canonicas [Javier :10]
- CI fase2 y suites acumuladas detectaron expectativas heredadas del href fragmento. Dos asserts Playwright se actualizan a ruta+fragmento exactos en categorias y categoria por slug, preservando las pruebas.
- Se reactiva CI dedicada C15 con modificacion causal de spec; PASS pendientes hasta jobs exact-SHA, R2 global EN_PROGRESO, sin A5 propia.

## 2026-11-17 — V19 C15-04.M05.04 validador phase2 acorde al fix [Javier :10]
- Lint fase2 falló en SHA 71a32b6f porque exigia el href solo fragmento defectuoso. Se refuerza assert para exigir RouterLink relativo y fragment Angular, sin suprimir verificacion.
- Regresion Chromium y Scope Lock exact-SHA del nuevo commit requeridos, R2 global todavia EN_PROGRESO y A5 pendiente independiente.

## 2026-11-17 — V19 C15-04.M05.04 P2 salto teclado tienda [Javier :10]
- Chromium SHA 39b2e65d RUN 44112044539 encontro 11 PASS y 1 FAIL: Tab+Enter sobre enlace #contenido-principal salia de /tienda hacia / por el HTML base. QA_DEFECT_FOUND P2 genuino.
- Corregido enlace con RouterLink relativo y fragment, regresiones de tres rutas verifican destino y pathname. CI del SHA correctivo aun requiere comprobacion; auditoria UX global conserva EN_PROGRESO, A5 pendiente ajena.

## 2026-11-17 — V19 C15-04.M05.04 teclado y dialog movil [Javier :10]
- Incremento Playwright real: Tab/Enter de skip-link en tres rutas publicas y dialog movil con foco inicial, Escape y retorno, cero HTTP mutaciones.
- Doce casos requeridos en CI exact-SHA; A0-A4 globales siguen incompletos, EN_PROGRESO bajo DEV02, A5 ajena; sin certificar JWT/MySQL/tenant/WCAG/Web Vitals.

## 2026-11-17 — V19 C09-04.M04.06 densidad de mensajeria privada [Javier :30]
- Cuatro pruebas Chromium reales sobre Angular para reflow a 320/375/768/1280, contenido privado retenido hasta GET, 401/403/503 opacos y tema local reversible sin POST, persistencia ni proveedores.
- Workflow GitHub-hosted y evidencia A0-A4; sesion HTTP sintetica, NO JWT firmado, MySQL, envios reales, WCAG global ni Core Web Vitals. CI exact-SHA requerida; A5 de otra Task Javier y sin QA/main/PROD.

## 2026-11-17 — V19 C13-04.M05.04 matriz JWT positiva multirruta [Javier :00]
- Nueva prueba Playwright en CI .NET10/MySQL8.4/Angular con JWT firmado sobre cuatro rutas privadas y headings reales; conserva tres casos previos de autenticación, RBAC y tenant A/B.
- No certifica 100 % de rutas ni revocación: P1 de aceptación global sigue abierto, VALIDANDO y A5 pendiente de otra Task. Checks exact-SHA obligatorios, cero cambios a runtime, QA/main/PROD o secretos.

## 2026-11-17 — CI_REPAIR V19 C13-04.M05.04 puerto Angular aislado [Javier :00]
- Fallo reproducido en RUN 44101230377 SHA 5b02394a: `PORT=5005` heredado de API hizo que Angular CLI ignorase su puerto de inicio y chocara con API; Chromium no corrió, no es PASS.
- Reparación mínima `PORT=4200` solo para npm start del runner; API sigue 5005, MySQL 8.4 y JWT no se alteran. Revalidar SHA nuevo, P1 y VALIDANDO permanecen; sin QA/main/PROD.

## 2026-11-17 — V19 C15-04.M05.04 navegación pública storefront real [Javier :10]
- Test incremental Chromium sobre /tienda, /tienda/productos y /tienda/categorias a 375/1280 px, con main/h1, enlaces de salto, overflow y ausencia de escrituras HTTP; workflow pasa de 9 a 10 casos.
- No se prueban compras, cobros, API/tenant, WCAG global ni rutas privadas autenticadas. Requiere SUCCESS real en CI exact-SHA; mantener EN_PROGRESO/lock V17 y A5 pendiente de otra Task.

## 2026-11-17 — V19 C13-04.M05.04 prueba privada JWT y MySQL efimero [Javier :00]
- Nueva integración CI aislada .NET 10/MySQL 8.4/Angular/Chromium, tres casos browser/API positivos y adversariales con JWT firmado; sin mocks HTTP ni credenciales reales.
- Mantener C13-04.M05.04 VALIDANDO/P1 de cobertura global; las comprobaciones externas del nuevo SHA no son PASS hasta ejecución verificada. A5 debe ser independiente.
- Única rama dev; no se alteran IAM, backend runtime, QA/main/PROD ni secretos productivos. Rollback git revert.

## 2026-11-17 — V19 C15-04.M05.04 frontera login y teclado real [Javier :10]
- Se amplian las pruebas Chromium incrementales de 7 a 9 con limites 50/128, estados invalido/valido, Tab desde inicio, Espacio y aria-pressed; sin submits ni cambios a producto.
- CI exact-SHA pendiente de verificacion; no certificar cierre global, JWT/tenant A/B, MySQL, WCAG ni Web Vitals. Conservar EN_PROGRESO y candado V17; A5 independiente.

## 2026-11-17 — CI_REPAIR V19 C09-04.M04.03: auditoría ERP de ruta privada de estados [Javier :40]
- Se actualiza una ruta exacta del control plane en scripts/quality/priority4_quality_audit.py; no se elimina la comprobación plataformaGuard, autoridad del servidor, ambito Plataforma, administrador ni manejo de errores.
- Fallo causal reproducido en GitHub Actions ERP sobre 23b0af90: FRONTEND_ROUTE_SECURITY_CONTRACT UNGUARDED de /plataforma/mensajeria-multicanal-v19/estados, aunque el guard real estaba declarado. Reparación limitada al validador compartido sin sobrescribir la implementación de DEV03.
- El nuevo SHA requiere CI GitHub-hosted, readback y A0–A4 completos antes de LISTO; A5 independiente, sin certificar JWT/backend real, MySQL, SMTP, WhatsApp, QA, main o PROD.

## 2026-11-17 — V19 C12-04.M06.04 saneamiento negativo de restore [Javier :50]
- Prueba real en CI MySQL 8.4 efimero: SHA256 de dump, duracion sintetica, cleanup de destino corrupto tras SQL invalido y rechazo de duplicado (empresa_id,code), sin degradar controles.
- No certifica migraciones Oracle EF, respaldos reales, RPO/RTO productivos ni cutover; A0–A4 requieren CI exact-SHA y A5 independiente; conservar VALIDANDO sin QA/main/PROD.

## 2026-11-17 — V19 C09-04.M04.03 branding local cerrado y componentes permitidos [Javier :40]
- Selección reactiva de tres presets de presentación exclusivamente local, con allowlist congelada y rechazo fail-closed de temas arbitrarios; conserva tres canales NO_CONECTADO y plataformaGuard.
- Se añaden cinco pruebas Angular TestBed y tres escenarios Playwright Chromium para admin, denegación, CSS/URL/HTML inválidos, revocación, teclado Tab, responsive y ausencia de HTTP mutante; GitHub-hosted exact-SHA por verificar.
- No configura proveedores, no envía mensajes ni certifica JWT/RBAC multitenant backend, MySQL, SMTP, WhatsApp o webhooks. A0–A4 pendientes de CI causal; A5 de otra Task Javier; rollback normal sin QA/main/PROD.

## 2026-11-17 — V19 C09-04.M04.04 interacciones privadas sin envios [Javier :30]
- Cuatro pruebas Chromium sobre Angular real: anonimo/403, Empresa/no administrador, canales read-only sin formularios y Tab/Enter con segunda activacion sin POST, responsive 320–1280.
- Solo se simula la sesion HTTP; no hay boton de envio real, por lo que no se certifica deduplicacion SMTP/WhatsApp, JWT real, MySQL, webhooks ni privacidad del backend.
- CI GitHub-hosted exact-SHA y readback A0–A4 pendientes; A5 por otra Task Javier. Sin permisos nuevos ni QA/main/PROD; revert normal.

## 2026-11-17 — V19 C09-04.M04.02 integración de ruta protegida [Javier :20]
- Ruta hija real de plataforma con guard y lazy standalone de estados, sin alterar la pantalla base M04.01 ni sus controles; cero envíos, secretos, datos empresariales o nuevas operaciones mutantes.
- Cuatro escenarios Playwright Chromium con denegaciones, estado vacío, responsive 320/1280 y sesión HTTP sintética; CI GitHub-hosted exact-SHA pendiente. Ocho pruebas TestBed precedentes conservadas.
- No equivale a JWT/backend, proveedores, MySQL ni navegación enlazada desde la pantalla base. Mantener VALIDANDO hasta SUCCESS causal A0-A4 y readback, A5 por otra Task Javier. Revert normal sin reescritura.

## 2026-11-17 — V19 C15-04.M05.04 ruta privada y login sin escritura [Javier :10]
- Prueba incremental de 20 rutas privadas del inventario Angular con 40 navegaciones anónimas a 375/1280 px, redireccion fail-closed, sin overflow ni HTTP mutaciones, mas alternancia de contrasena.
- Siete escenarios Chromium requieren GitHub-hosted PASS exact-SHA; no certifica roles autenticados, tenant A/B, MySQL, WCAG global, Web Vitals ni LISTO. Retener EN_PROGRESO/lock V17; A5 de otra Task.

## 2026-11-17 — V19 C13-04.M05.04 guardas privadas anónimas [Javier :00]
- Tres casos Chromium adicionales comprueban rechazo de ingreso directo a dashboard, productos y plataforma sin token, sin mocks ni interceptación HTTP; se conservan cuatro escenarios públicos (7 casos dirigidos en nuevo SHA).
- La prueba de guard Angular no acredita JWT real, API, MySQL ni cobertura global: P1 sigue abierto y estado VALIDANDO; GitHub Actions exact-SHA y Scope Lock/V5 obligatorios antes de cualquier certificación.
- Entrega limitada a test/workflow/evidencia/CHANGELOG, sin cambiar servicios, tenants, secretos ni producto; rollback normal git revert.

## 2026-11-17 — V19 C12-04.M06.04 ensayo MySQL 8.4 sintetico real [Javier :50]
- Nuevo workflow GitHub-hosted de restore de mysqldump efimero de tres filas A/B, SHA2, rollback SQL y rechazo de dump malformado, sujeto a check exact-SHA.
- No usar Aiven ni datos reales; sin Oracle EF migrations o RPO/RTO acreditados; conservar VALIDANDO y A5 independiente pendiente.

## 2026-11-17 — V19 C09-04.M04.05 reparación P2 de selectores Playwright [Javier :30]
- CI GitHub-hosted run 44085577070 identificó 2/4 fallos de prueba por selectores globales ajenos al scope: el shell incluye dos main y cinco controles fuera de la pantalla.
- Selección causal corregida a main[aria-labelledby=c0904-titulo] y controles dentro de .mensajeria; se mantienen intactas las denegaciones, contraste >=4.5, Tab real, dimensiones y presupuesto de escrituras.
- Revalidar los cuatro escenarios en Chromium real sobre SHA nuevo; fallos históricos NO son PASS ni LISTO; A5 de Task Javier independiente.

## 2026-11-17 — V19 C09-04.M04.05 accesibilidad dirigida multicanal [Javier :30]
- Cuatro escenarios Chromium nuevos sobre pantalla privada existente: rechazo 403, 320–1280 px, Tab/Enter desde inicio y cálculo numérico de contraste de h1, sin .focus artificial.
- Solo prueba browser R2 con sesión de plataforma sintética, no JWT real ni certificación WCAG global; cero cambios de componente, servicios, permisos, proveedores, datos o entornos.
- CI GitHub-hosted exact-SHA pendiente, A5 independiente; rollback git revert normal, Scope Lock STRICT.

## 2026-11-17 — V19 C09-04.M04.01 superficie multicanal privada [Javier :30]
- Implementación real Angular standalone con plataformaGuard, Signals y estado declarativo NO_CONECTADO para email, WhatsApp y otros proveedores; cero credenciales, consultas tenant o operaciones de envío.
- Seis pruebas Vitest de permisos y revocación; cuatro escenarios Chromium reales para denegación, teclado, 320–1280 px y ausencia de escrituras, sujetos a CI exact-SHA.
- Inventario P0-007.A de 159 fuentes y auditoría ERP de ruta exacta actualizados sin bypass; A0–A4 solo tras CI, A5 por revisor Javier distinto; sin JWT real/SMTP/WhatsApp/MySQL/QA/main/PROD.

## 2026-11-17 — V19 C15-04.M05.04 inventario UX estático trazable [Javier :10]
- Censo de 160 declaraciones path en 9 archivos Angular de router importados, 47 parametrizadas; rutas/roles/detalles con evidencia aún SIN_VERIFICAR.
- Matriz de hashes blob por fuente anexada al documento del scope; 5/5 Chromium previos exitosos NO acreditan cobertura global ni JWT/RBAC/MySQL/WCAG.
- EN_PROGRESO original DEV02, lock V17 retenido; sin LISTO/A5 ni cambios de runtime.

## 2026-11-17 — V19 C13-04.M05.04 evidencia de brecha P1 global [Javier :00]
- Diagnóstico A0 reproducible de 109 rutas literales en app.routes.ts, 95 con guard declarado y 14 sin guard declarado; rutas importadas fuera de ese denominador.
- Los 4 Chromium previos solo recorren 6 destinos públicos, no JWT real ni rutas privadas: se detallaron pantallas pendientes y la matriz Angular/API/MySQL efímera necesaria.
- Se mantiene VALIDANDO y QA_DEFECT_FOUND P1; sin afirmar A0–A4 completo, LISTO, A5 ni QA/main/PROD. Actualización documental y rollback git revert.

## 2026-11-17 — V19 C15-04.M05.04 UX global incremental [Javier :10]
- Primer incremento R2 Playwright real: login y panel privado responsive, Tab/foco, contraste puntual, 403 y guard ERP, con sesión HTTP simulada.
- CI GitHub-hosted propia por SHA; primer run detectó un test P2 de fixture que reutilizaba token sintético entre 403 y ERP; corregido separando el caso ERP en contexto browser limpio, revalidación SHA descendiente obligatoria.
- EN_PROGRESO reanudable: inventario total de rutas, roles/tenants, WCAG global y Web Vitals siguen SIN_VERIFICAR; A5 de otra Task.

## 2026-11-17 — V19 C13-04.M05.05 negativos browser globales [Javier :50]
- Nueve pruebas Playwright/Chromium para anonimo, expiracion, empresas A/B sinteticas, errores HTTP 401/403/503, success=false y catalogo sin enlaces enumerables.
- Nuevo workflow GitHub-hosted Angular 22.2.1/Node 24/Chromium; SHA exacto y checks pertinentes pendientes hasta CI real.
- R2 test-only: sesion API simulada; no certificar JWT firmado, RBAC/MySQL real ni tenant A/B servidor. A5 por otra Task y sin liberar QA/main/PROD.

## 2026-11-17 — V19 C13-04.M05.03 integración MySQL de sesión global [Javier :40]
- Oracle EF/MySQL 8.4 efímero GitHub-hosted: prueba xUnit de sesión plataforma desde base real, denegaciones empresariales A/B, revocación persistida y auditoría sin EmpresaId.
- Workflow Category=Integration, TRX estricto y Scope Lock exact-SHA; no es PASS hasta CI completed/success y readback A0–A4. A5 independiente.
- Test-only de backend con identidad HTTP sintética: no certifica JWT firmado, HTTP alojado, MySQL productivo ni QA/main/PROD. Rollback git revert normal sin force-push.

## 2026-11-17 — V19 C09-04.M04.01 checkpoint reanudable A0 [Javier :30]
- Reclamación V17 exclusiva del alcance de superficie multicanal con owner DEV04, sin habilitar envíos, credenciales, proveedores ni accesos tenant.
- Documento A0 y matriz precisa de implementación/pruebas/rollback para reanudar EN_PROGRESO; NO se entrega UI, no se declara CI PASS, VALIDANDO, LISTO ni A5 de la microtarea.
- Scope Lock STRICT y avance documental seguro en dev, sin cambiar QA/main/PROD.

## 2026-11-17 — V19 C12-04.M06.04 ensayo de rollback Git aislado [Javier :50]
- Script shell real de `git revert --no-commit` de un commit de pruebas M05.06 en worktree efimero; valida árbol padre, tres rutas permitidas y conservación de SHA256 y conteos sintéticos, con rechazo de SHA no autorizada.
- Workflow GitHub-hosted por SHA con pruebas positiva/negativa, Scope Lock y V5 exigibles; CI no se atribuye PASS antes de ejecutarse.
- R2 test-only sin exportación/restore MySQL, EF, JWT/RBAC servidor, RPO/RTO ni cutover productivo. Estado institucional depende de CI causal y de brechas pendientes; A5 por otro Javier, únicamente dev.

## 2026-11-17 — V19 C09-04.M03.06 auditoría estructurada Outbox [Javier :10]
- OutboxTenantDeliveryWorker registra denegación tenant/mensaje, resumen del lote y error de procesamiento con ILogger opcional y campos allowlisted, sin payload, claves ni Exception.Message.
- Cinco pruebas xUnit de éxito, A/B, revocación y error del proveedor con secretos; .NET 10 Release/seguridad/Scope Lock/V5 requeridos exact-SHA, A5 independiente.
- Solo Application, pruebas, documento y CHANGELOG en dev; sin DI nuevo, MySQL, JWT HTTP, SMTP/WhatsApp reales ni QA/main/PROD; rollback git revert.

## 2026-11-17 — V19 C13-04.M05.04 aceptación pública sin mocks [Javier :00]
- Cuatro Playwright/Chromium nuevos verifican login, navegación a productos/categorías/ofertas y carrito en Angular real: cero `page.route()`, sin autenticación simulada ni API/tenants inventados.
- Workflow GitHub Actions Node24/npm11, lint/build, navegador real por SHA; A0–A4 causales y A5 de Task Javier distinta, candado V17 `2000002009`.
- R2 test-only sobre una muestra de rutas públicas, sin afirmar cobertura total, JWT, MySQL, pagos, QA/main/PROD ni restauraciones; rollback git revert.

## 2026-11-17 — V19 C13-04.M05.02 contrato HTTP/DTO fail-closed [Javier :40]
- Suite Angular TestBed dirigida del GET /plataforma/sesion: positivo, envelopes falsos/nulos, empresa no autorizada, nombres invalidos, revocacion y HTTP 401/403/500/503.
- Workflow GitHub-hosted Node 24/npm 11 por SHA, pruebas ejecutables, lint y build; A0–A4 y A5 requieren evidencia externa sin adelantar PASS.
- Transporte HTTP sintetico cliente, no certificacion JWT/RBAC/MySQL/tenant servidor. Solo dev, candado V17, rollback normal git revert, sin QA/main/PROD.

## 2026-11-17 — V19 C13-04.M05.01 regresión de reglas de rutas [Javier :00]
- Cinco Vitest nuevos para invariantes del catálogo Angular: 16 programas, herencia de guards, rutas de edición/creación, rechazo de enlaces hostiles, lazy sin ejecución y fronteras de matcher/raíz.
- Workflow Node 24/npm 11 por SHA, con prueba dirigida, tipado/lint y build; controles Scope Lock y V5 pertinentes. Pruebas de frontend puras, sin JWT/MySQL/backend live; A5 por otra Task Javier.
- Exclusivamente `dev`, claim único V17 `2000002006`, A0–A4 causales pendientes de CI antes de LISTO, rollback por git revert sin tocar QA/main/PROD.

## 2026-11-17 — V19 C12-04.M06.03 riesgos y decisiones MySQL/cutover [Javier :50]
- Registro R01–R10 de riesgos abiertos y D01–D08 de decisiones preventivas, contrastando M05.01–06 y las fuentes documentales M06.01/.02; checksums de origen, restore real, RPO/RTO y JWT servidor siguen NO_MEDIDO/NO_AUTORIZADO.
- A0–A3 auditables sin cambios de runtime, NO_APLICA a nuevas pruebas MySQL/HTTP; A4 exact SHA Scope Lock/V5 y readback, A5 requiere Task Javier distinta.
- Solo dev, no despliegues ni cambios en datos productivos; revert git normal, BITACORA histórica preservada.

## 2026-11-17 — V19 C12-04.M06.02 matriz causal reproducible M05 [Javier :40]
- Documento versionado con seis SHAs de M05.01–06, runs dirigidos, tipo de prueba, límites y diferencias entre mocks y MySQL efímero real; no afirma restore/cutover productivo.
- Conserva FAILURE histórico de checks ajenos en SHA M05.04, sin elevarlo a PASS. Reproducibilidad por commit, check-runs exact-SHA, blobs y readback institucional.
- R0 documental; Scope Lock/V5 y A5 independiente antes de cerrar etapa; sin QA/main/PROD, rollback git revert aditivo.

## 2026-11-17 — V19 C12-04.M06.01 evidencia real vs simulación [Javier :50]
- Revisión causal documental de seis M05 con SHA y runs: 5/5 + 7/7 Vitest, 3/3 MySQL 8.4.13 efímero, 18/18+5/5, 7/7 y 18/18+4/4 Chromium; no inferir restauración real, JWT backend o cutover autorizado.
- Matriz P1-P4 y N1-N6 diferencia fixtures, HTTP interceptado, MySQL efímero y límites NO_MEDIDO/NO_AUTORIZADO. A3 runtime NO_APLICA para este diff R0.
- A4 requiere Scope Lock/V5 exact SHA y readback; A5 de otra Task Javier, dev exclusivamente, sin QA/main/PROD, rollback git revert normal.

## 2026-11-17 — V19 C13-04.M04.06 densidad y carga progresiva [Javier :40]
- Cuatro Chromium sobre Angular: 16 programas/108+ rutas, reflow 320/375/768/1280, GET sesión demorado sin exposición, 401/403/503, Tab/foco/44px y cero escrituras.
- CI Node24/npm11 SHA-specific con Vitest base, lint/build, cuatro Chromium; A0-A4/Scope Lock/V5 y A5 por otra Task Javier.
- R2 test-only, sesión sintética: no JWT/tenant/MySQL live, Web Vitals, WCAG global, QA/main/PROD. Rollback git revert.

## 2026-11-17 — V19 C13-04.M04.01 recuperación P0-007.A y auditor ERP [Javier :30]
- QA_DEFECT_FOUND en CI: el catálogo estricto rechazó una nueva ruta de componente no registrada y el auditor ERP rechazó una nueva ruta privada no incluida en su lista cerrada.
- Alta exacta de un componente verificado en manifiesto (158 total, 150 features, 8 shared) y solo la ruta privada `plataforma/inventario-rutas-v19` al auditor sin relajar `plataformaGuard`, backend, controles negativos ni filtros de rutas.
- Requiere check P0-007.A, ERP, V19 seis Vitest/cuatro Chromium y Scope Lock/V5 SUCCESS en SHA reparado; A5 por otra Task. Sin QA/main/PROD, cambios de tenant/JWT/MySQL ni force-push.

## 2026-11-17 — V19 CI repair C13-04.M04.01 ruta parametrizada duplicada [Javier :30]
- QA_DEFECT_FOUND en prueba Chromium SHA 93edefa6: el path clientes/:id/editar aparece dos veces en configuraciones Router distintas; la aserción de unicidad fallaba aun con ambos sin enlace.
- La reparación exige presencia de una o más coincidencias y cero anchors para TODAS las filas con :id; se conservan los otros controles de 16 programas, rutas, 403 sintético, 320/375/768/1280 y Tab real.
- Requiere 6 Vitest y 4 Chromium completos, Scope Lock/V5 exact-SHA y A5 independiente; no acredita JWT, MySQL, QA/main/PROD ni considera SKIPPED como PASS.

## 2026-11-17 — V19 C13-04.M04.01 inventario global de programas y rutas [Javier :30]
- Añade vista privada guardada de 16 programas C00–C15 y enumeración real de todos los Route[] declarados en Angular, incluidos spreads, children, detalle y edición.
- Registra enlaces solamente para paths estáticos permitidos; rutas parametrizadas, wildcard y lazy pendiente se muestran sin inventar IDs, permisos ni cobertura completa.
- Seis pruebas Angular/Vitest y cuatro Playwright/Chromium adversariales, lint y build en GitHub-hosted exact-SHA; revisión A5 por otra Task.
- R2 sin JWT/tenant/MySQL live ni certificación WCAG global; solo dev, PROJECT_SCOPE_LOCK=STRICT, rollback git revert; no QA/main/PROD.

## 2026-11-17 — V19 C09-04.M03.05 worker outbox tenant-gated [Javier :50]
- Implementa worker invocable por empresa con gate obligatorio antes de claim y antes de despacho; no registra scheduler, DI ni proveedor externo.
- Agrega 5 xUnit positivos, adversariales y frontera sobre Application con dobles explícitos; CI real .NET10 y Scope Lock/V5 exact-SHA obligatorios.
- R2 sin migraciones, JWT HTTP o proveedores productivos; A5 de otra Task Javier, QA/main/PROD no autorizados, rollback git revert.

## 2026-11-17 — V19 reparación CI C12-04.M05.03 nullable EmpresaId [Javier :00]
- Corrige compilación CS1501: EmpresaId nullable debe serializarse mediante Convert.ToString con InvariantCulture; conserva todas las aserciones adversariales y la semántica SHA256 del snapshot A/B.
- Defecto detectado en job de seguridad multiempresa del SHA 35697a0d; revalidar Release, Category=Integration MySQL 8.4 efímero y Scope Lock/V5 exact-SHA sin SKIPPED=PASS; A5 ajena, dev únicamente.

## 2026-11-17 — V19 C12-04.M05.03 integración EF Oracle MySQL 8.4 A/B [Javier :00]
- Test xUnit Category=Integration con MySQL 8.4 efímero: productos de dos empresas, aislamiento A/B, rechazo IDOR, headers hostiles, huellas SHA256 estables de datos SQL y bloqueo previo de host remoto/puerto inseguro.
- GitHub Actions SHA-scoped ejecuta .NET 10 Release + MySQL 8.4 real, TRX con >=3 tests y cero fallos; Scope Lock/V5 exact-SHA y A5 independiente obligatorios.
- R2 test-only, sin cambios runtime, sin Aiven ni restauración/cutover real, JWT HTTP ni QA/main/PROD; una reclamación V17, rollback git revert sin force.

## 2026-11-17 — V19 C12-04.M05.04 aceptación positiva de cutover privado [Javier :30]
- Cinco pruebas Playwright/Chromium sobre Angular real con sesión sintética: cuatro checkpoints no operativos, revalidación GET, Tab/Enter, branding responsive, 503 fail-closed sin DSN ni escrituras.
- Workflow GitHub-hosted por SHA: Node24/npm11, Vitest, lint/build, cinco Chromium. A0–A4 sólo cuando CI requerida y Scope Lock/V5 concluyan SUCCESS; A5 de otro Javier.
- R2 test-only, no MySQL/restore/exportación real, JWT/RBAC ni tenant servidor; V17 una reclamación; sólo dev; rollback git revert.

## 2026-11-17 — V19 C09-04.M03.04 MySQL 8.4 outbox tenant/idempotencia [Javier :20]
- Infraestructura: validador de claves de idempotencia en lecturas EF rechaza controles CR/LF y valores fuera del limite, sin cambiar el contrato publico ni los datos.
- Regresion xUnit Category=Integration en MySQL 8.4 efimero real: persistencia, aislamiento A/B, duplicados/indice unico y claim CAS; workflow SHA-especifico sin cancelacion.
- R2 sin despliegue, API, JWT real ni proveedor de mensajeria. A0–A4 solo tras CI causal SUCCESS, A5 independiente; Scope Lock STRICT, rollback normal y ninguna autorizacion QA/main/PROD.

## 2026-11-17 — V19 C12-04.M05.02 contrato HTTP/DTO de cutover privado [Javier :40]
- Siete pruebas reales Angular TestBed+Componente de cutover y servicio de sesión con HttpClientTesting: 200 global, 401, 403, 503, 200 Empresa, success=false, anónimo; estados privados fail-closed.
- CI SHA-scoped GitHub-hosted Node24/npm11 ejecuta Vitest, lint/build con Scope Lock y V5; no certificar JWT/RBAC/tenant servidor, MySQL o restore mediante HTTP simulado. A5 independiente; solo dev.

## 2026-11-17 — V19 C12-04.M05.05 siete negativas de cutover privado [Javier :00]
- Siete nuevas pruebas Playwright Chromium sobre Angular real para anonimo/401/403/tenant Empresa/no admin/identidad malformada/revocacion 503, sin escritura ni exposicion de controles y secretos privados.
- GitHub-hosted Node24 npm11, regresiones Vitest, lint/build y Chromium por SHA con Scope Lock y V5; nunca SKIPPED o CI pendiente como PASS. Sesión HTTP es sintética: NO certifica JWT/tenant backend, restore ni MySQL real.
- R2 test-only, una reclamacion V17 exclusiva, A5 por Task Javier distinta, solo dev, rollback git revert normal.

## 2026-11-17 — V19 C12-04.M05.01 inmutabilidad de evidencias de cutover [Javier :30]
- Corrige Object.freeze superficial de cuatro checkpoints informativos: ahora congela también cada objeto para impedir falsificar localmente SIN_VERIFICAR/NO_MEDIDO/NO_AUTORIZADO como LISTO.
- Cinco Vitest Angular TestBed de mutación adversarial, estados, tema, revocación y fronteras; CI Node24/npm11 lint/build; no modifica MySQL/EF, restauraciones, tenant, JWT ni autorizaciones.
- Riesgo R2 por superficie sensible; sólo dev, V17 claim único, Scope Lock/V5, A0–A4 al concluir checks y A5 independiente de otra Task Javier; rollback git revert.

## 2026-11-17 — V19 C15-04.M05.03 ciclo de autorización y persistencia negativa [Javier :30]
- Tres Playwright/Chromium sobre Router y Servicio de Sesión Angular real con HTTP sintético: 200→403, 200→503 y 200 success=false; revocación sin tarjetas ni información sensible persistida.
- La vista UX es observacional y no tiene persistencia propia: MySQL/EF/proveedor de métricas NO_APLICA; no atribuir JWT/RBAC/tenant backend live, WCAG global ni producción.
- CI GitHub-hosted Node24/npm11, lint/build y Chromium por SHA; Scope Lock/V5, A0–A4 sólo con SUCCESS real, A5 independiente de otra Task Javier, V17 lock y git revert.

## 2026-11-17 — V19 C15-04.M05.02 contrato HTTP/DTO y denegaciones [Javier :40]
- Seis Vitest Angular reales con HttpClientTesting para GET /plataforma/sesion: contrato positivo, tenant Empresa 200, DTO incompleto, 401 revocado, 403 y 503 fail-closed sin cache. Backend HTTP simulado: NO certifica JWT/RBAC/tenant reales.
- CI por SHA Node24/npm11, lint/build y pruebas dirigidas; exigir Scope Lock, V5 y seguridad pertinente antes de LISTO y A5 independiente. Solo dev, sin cambios de producción.

## 2026-11-17 — V19 C09-04.M03.03 autorización tenant fail-closed del outbox [Javier :50]
- Application valida identidad de usuario, rol empresarial, nombre del rol y EmpresaId exacto del scope devuelto desde membresía server-side; no concede bypass por administrador global.
- Regresiones xUnit positivas y adversariales con conteo de cero lecturas/escrituras tras denegación, incluido fallo del proveedor de scope; GitHub-hosted .NET 10 + seguridad y Scope Lock requeridos por SHA.
- Cambio R2 limitado a Application/tests/docs/CHANGELOG, sin JWT HTTP/MySQL ni proveedor real; A5 independiente pendiente, lock V17 y rollback git revert en dev.

## 2026-11-17 — V19 C15-04.M05.05 negativas de acceso en Chromium [Javier :30]
- Cuatro pruebas E2E negativas en Angular real contra sesión sintética: anónimo, 401, 403 opaco y 200 de tenant Empresa; no render privado ni escrituras; no se adjudica autorización backend real.
- GitHub-hosted Node24/npm11, lint/build y Playwright Chromium dirigido por SHA, Scope Lock y procedencia V5; estado CI se acredita sólo cuando termine SUCCESS.
- Riesgo R2 test-only; A5 independiente de otra Task Javier, V17 una sola EN_PROGRESO, rollback git revert; no JWT real/MySQL/PROD, sólo dev.

## 2026-11-17 — V19 C12-04.M04.06 densidad y progreso privado [Javier :40]
- Tres nuevos Playwright Chromium prueban cuatro tarjetas, cuatro anchuras sin overflow, boton de 44px, GET pendiente con aria-busy y contenidos privados ocultos, y 503 opaco sin escrituras. Sesion simulada, no JWT ni MySQL real.
- CI SHA-scoped con cancel-in-progress=false exige 18 Vitest, 3 Chromium, Angular lint/build, Scope Lock y V5. A5 por revisor distinto, no Web Vitals productivos ni QA/main/PROD.

## 2026-11-17 — V19 C15-04.M05.01 contrato unitario reproducible UX [Javier :30]
- Cinco pruebas Angular TestBed/Vitest de ruta con guard, cinco tarjetas estables, NO_MEDIDO/SIN_VERIFICAR, lectura sin mutaciones y cuatro anchos declarativos; sin cambios runtime de producto.
- Workflow GitHub-hosted Node24/npm11, npm ci, Vitest dirigido, tipado/lint y build Angular; los checks requeridos del SHA, Scope Lock y V5 se certificarán sólo al finalizar SUCCESS.
- R1 test-only: no certifica JWT/RBAC/tenant live, MySQL, WCAG global ni Web Vitals; A5 independiente de otra Task Javier, V17 claim exclusivo, rollback git revert y sólo dev.

## 2026-11-17 — V19 C15-04.M04.06 densidad y carga progresiva verificadas [Javier :40]
- Implementa tres escenarios Playwright/Chromium sobre Angular real en loopback: denegación anónima/503 opaca, cinco tarjetas NO_MEDIDO con presupuestos verificables a 320/375/768/1280 px y carga diferida hasta sesión autorizada.
- Contratos de seguridad de la vista sin writes y con sesión HTTP sintética: NO certifica JWT/RBAC/tenant reales, MySQL, WCAG integral ni Web Vitals/SLO de producción; métricas no probadas = NO_MEDIDO.
- GitHub-hosted Node24/npm11 lint/build y 3/3 Chromium sobre SHA fuente; Scope Lock, procedencia V5 exact-SHA, readback institucional V17 y revisión A5 de otra Task Javier; sólo dev, rollback normal git revert.

## 2026-11-17 — V19 C09-04.M03.02 caso de uso idempotente outbox [Javier :10]
- Application rechaza CR/LF/TAB/NUL en Idempotency-Key original antes de Trim, evitando normalizar claves adversariales hacia un replay válido.
- Regresiones xUnit positivas, negativas, frontera 200/201 y empresa A/B; CI GitHub-hosted real por SHA, sin prometer JWT/MySQL/SMTP/WhatsApp.
- Claim exclusivo V17, A0–A4 y A5 de otra Task Javier; Scope Lock STRICT, dev únicamente, rollback git revert.

## 2026-11-17 — V19 C05-01.M06.06 cierre documental causal Design System [Javier :50]
- Matriz de seis M05 y cinco M06 LISTO con A5 independiente, SHAs y filas BITACORA; condicion transversal C00-03.M06.06 verificada; correccion P2 de M06.02 conservada y nueva A5 causal comprobada.
- Cierre R0 de dependencias Design tokens y componentes sin cambios runtime; pruebas y brechas de autenticacion real, MySQL, WCAG global y produccion declaradas NO_APLICA o NO_MEDIDO segun corresponda.
- Publicacion solo dev con Scope Lock/V5 exact SHA, readback y A5 de otra Task Javier pendiente; DAG/COLA siempre derivados, rollback por git revert normal.

## 2026-11-17 — V19 C09-04.M03.01 identificadores seguros outbox [Javier :40]
- Domain valida identificadores de evento, idempotencia, agregado y correlación contra CR/LF, TAB, NUL, preservando payload JSON, tenant y firmas previas.
- Pruebas xUnit positivas, negativas, frontera y rechazo adversarial publicadas para CI .NET 10 real por SHA; no se certifican MySQL, JWT HTTP, HMAC, SMTP ni WhatsApp.
- Claim V17 exclusivo, A0–A4 con readback y A5 independiente; PROJECT_SCOPE_LOCK STRICT, rollback normal git revert, sin QA/main/PROD.

## 2026-11-17 — V19 C09-04.M02.06 contrato de salida multicanal [Javier :20]
- Matriz trazable de cinco M02.01–05 LISTO con A5 independiente, SHAs origen y dependencias M03 por Domain, Application, autorización, MySQL/EF, proveedores y auditoría, sin alterar DAG ni COLA.
- Contrato fail-closed de tenant/consentimiento, idempotencia durable, HMAC/replay, concurrencia y rollback; positivos/negativos futuros explícitamente NO_EJECUTADOS. Cambio R0 documental sin worker, webhook, migración, SMTP ni WhatsApp reales.
- A0–A4 con documentos, Scope Lock/V5 del SHA exacto y readback institucional; A5 sólo otra Task Javier. No QA/main/PROD, sin secretos ni datos productivos.

## 2026-11-17 — V19 C12-04.M04.05 accesibilidad visual y teclado real [Javier :00]
- Cinco pruebas Playwright Chromium nuevas: semántica/nombres, 320–1280 px, Tab/Enter desde inicio, foco visible, contraste WCAG >=4.5:1, objetivos >=44 px y 403 adversarial.
- Workflow GitHub-hosted por SHA con npm ci, Vitest, lint/build y Chromium; seguridad real JWT/MySQL/restore NO CERTIFICADA por mocks. Conservar A0–A5 y A5 independiente; no adelantar LISTO hasta SUCCESS causal.
- Cambio acotado a pruebas, workflow, evidencia y changelog, sin producción, secretos, migraciones ni restauraciones; rollback git revert normal.

## 2026-11-17 — V19 C09-04.M02.05 eventos y límites [Javier :40]
- Define contrato futuro EV01–EV16 y EI01–EI20 con tenant, idempotencia durable, correlación, retries finitos y webhook HMAC aún no implementado; separa outbox real de garantías futuras.
- Diez positivos y catorce negativos NO_EJECUTADOS, A0–A5 documental y rollback seguro; no crea eventos, workers, colas ni envía comunicaciones.

## 2026-11-17 — V19 C09-04.M02.03 persistencia e índices multicanal [Javier :50]
- Matriz verificada de MensajesOutbox, repositorio y migración N71C (índices únicos tenant/idempotencia y claim), email tenant-scoped y referencia WhatsApp N76C; separa fuentes reales de esquemas D01–D06 FUTUROS_NO_IMPLEMENTADOS.
- Contrato de migración aditiva efímera MySQL 8.4 y fixtures A/B con diez casos T01–T10 FUTUROS_NO_EJECUTADOS, seguridad fail-closed y rollback; sin SQL, HTTP, envíos, datos reales ni certificaciones inventadas.

## 2026-11-17 — V19 C09-04.M02.04 configuración tipada y flags [Javier :40]
- Contrato documental CFG01–CFG12, CF01–CF16 y precedencia fail-closed por plataforma/tenant/actor, separando referencias de secretos existentes de opciones y flags NO_IMPLEMENTADOS.
- Ocho positivos y doce negativos FUTUROS_NO_EJECUTADOS; no habilita SMTP, WhatsApp, webhooks, JWT, MySQL ni eventos; A0–A5 causal y A5 independiente posterior.

## 2026-11-17 — V19 C09-04.M02.02 API DTO y errores multicanal [Javier :40]
- Se documentan rutas y contratos existentes de outbox, email y WhatsApp por blob, diferenciándolos de doce interfaces futuras sin endpoints nuevos; autorización tenant, ProblemDetails, idempotencia y webhook HMAC futura fail-closed.
- Nueve positivos y catorce negativos FUTUROS_NO_EJECUTADOS, preservación de respuestas vigentes, A0–A5 causal y A5 independiente; sin runtime ni proveedores activados.

## 2026-11-17 — V19 C12-04.M04.04 pruebas reales de acción única y teclado [Javier :30]
- Playwright Chromium GitHub-hosted comprueba GET de sesión con bloqueo single-flight y aria-busy, error 503 opaco, Tab/Enter de teclado real y ausencia total de comandos mutantes o formularios en la ruta privada de preparación MySQL.
- Cuatro casos nuevos en archivo E2E y workflow SHA-scoped (sin cancelación de CI previa); 18 Vitest previos y 4 Chromium nuevos a acreditar antes de LISTO. A5 por otra Task Javier, sin JWT/MySQL/restore/PROD real.

## 2026-11-17 — V19 C09-04.M02.01 modelo de propiedad de mensajería [Javier :40]
- Contrato documental E01–E16 e INV01–INV20 para tenant, consentimiento, idempotencia, secretos y webhook replay, separando cinco fuentes reales de propuestas no implementadas; sin runtime ni envío.
- Ocho positivos y doce negativos FUTUROS_NO_EJECUTADOS, A0–A5 con A5 independiente y rollback git revert; CI solamente causal al diff documental.

## 2026-11-17 — V19 C12-04.M04.03 reparación Vitest por botones de tema permitidos [Javier :30]
- La CI exact-SHA d220a8b2 ejecutó 17/18 Vitest y falló porque una aserción histórica prohibía también los dos botones de branding expresamente permitidos; se excluyen solo los selectores de tema allowlist, preservando la prohibición de acciones operativas y el control de HTML adversarial.
- Corregir la evidencia por CI nueva 18/18 Vitest + 7 Chromium con Scope Lock/V5/seguridad. FAIL histórico conservado; ninguna inferencia de MySQL/JWT/QA/PROD y A5 siempre independiente.

## 2026-11-17 — V19 C12-04.M04.03 branding privado allowlist [Javier :30]
- Dos presets Angular tipados Institucional/Alto contraste, aria-pressed y estilos locales cerrados, sin CSS arbitrario ni acciones de MySQL; ante revocación y carga pendiente se ocultan los controles.
- Cinco Vitest adversariales y un Playwright Chromium nuevos; se requieren 18 Vitest y 7 Chromium causales más seguridad, frontend, Scope Lock y V5 exact-SHA. A5 siempre independiente; no certificar restore/JWT/tenant real.

## 2026-11-17 — V19 C09-04.M01.06 gates y rollback documental [Javier :20]
- Define nueve bloqueos fail-closed de mensajería multicanal: tenant A/B, JWT/RBAC, consentimiento, idempotencia, secretos, webhook replay, CI exact-SHA y A5 independiente; no crea servicios ni envíos.
- Establece rollback documental no destructivo, pruebas futuras expresamente NO_EJECUTADAS y liberación solo por DAG/COLA derivados tras LISTO+A5; sin QA/main/PROD.

## 2026-11-17 — V19 C12-04.M04.02 reparación de aserción Vitest exact-SHA [Javier :30]
- La CI del SHA `1f9df9e1` falló con 12/13 Vitest porque esperaba `no confirmado` pero la interfaz segura presenta `Sin permiso de plataforma confirmado.`; se corrige únicamente la expectativa del test, no se relaja la denegación ni se modifica JWT/RBAC/MySQL.
- Conservar FAIL histórico, verificar de nuevo 13 Vitest, 6 Chromium, Scope Lock, V5 y controles pertinentes en GitHub-hosted; A5 por Task Javier distinta y rollback mediante `git revert` normal.

## 2026-11-17 — V19 C12-04.M04.02 estados de sesión fail-closed [Javier :30]
- Cinco estados UI de revalidación GET global: cargando, vacío, error, éxito de sesión y sin permiso; sin equiparar éxito de sesión con exportaciones, restore, migraciones o permiso de producción.
- Seis Vitest nuevos y dos Chromium de revalidación/503, protección ante tenant/rol/revocación y sin operaciones mutantes; se mantiene A5 independiente.

## 2026-11-17 — V19 C12-04.M04.01 reparación de prueba Chromium [Javier :30]
- Corregida aserción de alcance del test E2E: selector limitado a `.cutover` frente al shell ERP global; el fallo inicial 3/4 continúa visible y no se considera PASS.
- Nueva ejecución GitHub Actions requerida, conservando 7 Vitest, 4 Chromium y todos los controles de seguridad/inventario; no se modifica UI ni JWT/MySQL reales.

## 2026-11-17 — V19 C05-01.M06.02 corrección de enlace A5_FAIL P2 [Javier :30]
- Reemplazado un RUN inexistente 44012694950 por 44012694986, verificado GitHub REST completed SUCCESS sobre SHA 908bcdf0; documento de evidencia actualizado sin alterar runtime ni validadores.
- El A5_FAIL histórico permanece y exige revisión independiente nueva; no se autoaprueba ni se desbloquea DAG artificialmente.

## 2026-11-17 — V19 C12-04.M04.01 CI dirigida Vitest y Chromium [Javier :30]
- Nuevo workflow solo lectura GitHub-hosted con 7 Vitest y 4 Playwright Chromium contra Angular real y sesión HTTP sintética; pruebas de teclado, 403, responsive y ausencia de mutaciones.
- No se confunde interfaz protegida con integración MySQL ni se certifica JWT/restore/PROD; A0–A4 y revisión A5 siguen condicionadas a evidencia del nuevo SHA.

## 2026-11-17 — V19 C12-04.M04.01 reparación TypeScript de plantilla [Javier :30]
- Corregidos cuatro delimitadores de literales de plantilla en componente Angular que provocaron TS1127, TS1136 y TS1160 en CI exact-SHA; documentación Markdown normalizada sin cambiar rutas ni controles.
- Se conservan todos los tests y validadores; exigir nuevamente compilación y A0–A4 sobre commit reparado, no interpretar checks fallidos como PASS.

## 2026-11-17 — V19 C12-04.M04.01 CI repair de inventario y ruta [Javier :30]
- Reparados fallos causales exact-SHA: añadir el nuevo componente al inventario estricto P0-007.A y su total 157; conservar pruebas negativas y lista cerrada sin exclusiones.
- Incorporada únicamente la ruta `plataforma/mysql-cutover-readonly-v5` en la allowlist exacta de `plataformaGuard` del auditor ERP, sin modificar guards ni desactivar controles; revalidar CI y A5 independiente.

## 2026-11-16 — V19 C12-04.M04.01 — superficie privada de cutover [Javier :30]
- Ruta Angular privada de preparación MySQL con guard real, checklist estrictamente informativo y denegación segura; ninguna restauración, migración ni acción de proveedor.
- Pruebas Vitest adversariales, revocación, roles y tenant; evidencia A0-A5 separa pruebas de UI de integración real y permite rollback por git revert sin reescritura.

## 2026-11-16 — V19 C05-01.M06.04 rollback aislado GitHub-hosted [Javier :30]
- Ensayo real git revert --no-commit del commit de pruebas C05-01.M05.06 en worktree efimero; validar SHA/padre, cuatro archivos exactos, igualdad de tree al padre, checkout original inmutable y rechazo de SHA no autorizado.
- Script Bash y workflow propio sin permisos de escritura remota, sin datos, migraciones, secretos, MySQL ni cambios de runtime; QA/main/PROD intactos y RTO/RPO sin certificar.
- A0-A4 con GitHub Actions dirigido + Scope Lock + V5 exact SHA; A5 independiente, BITACORA y readback por transicion; rollback normal git revert.

## 2026-11-16 — V19 C05-01.M06.05 solicitud A5 independiente de Design System [Javier :40]
- Solicitud documental por microtarea y SHA para seis fuentes M05 y cinco revisiones M06, sin confundir petición con A5_PASS ni cerrar fases por lotes. M05.01–06 LISTO+A5; M06.01–02 LISTO sin A5; M06.03 en progreso y M06.04 pendiente al corte.
- Revisión A0–A5 propone pruebas adversariales de exclusividad por TASK_ID, SHA exacto, CI realmente SUCCESS y distinguir Chromium/Angular/HTTP interceptado de JWT/RBAC/MySQL/tenant vivos. Caso positivo y cinco rechazos en memoria, sin escribir gates.
- R0 documental; ningún permiso nuevo, ninguna promoción QA/main/PROD ni aprobación propia. Scope Lock + procedencia V5 exact-SHA y readback institucional, A5 propia por otra Task Javier.

## 2026-11-16 — V19 C05-01.M06.03 riesgos y decisiones de design system [Javier :30]
- Registro R0 documental de ocho riesgos verificables y cinco decisiones de C05-01: JWT/RBAC HTTP real, aislamiento tenant, WCAG, Web Vitals, proveedores, tokens, SHA/dependencias y A5 sin promocion a produccion.
- Limites de pruebas M05.01–06 de TestBed, HttpClientTesting y Chromium diferenciados de seguridad y datos reales; puertas COLA/DAG derivadas sin edicion de formulas.
- A0-A4 con Scope Lock y procedencia V5 SHA exacto, A5 independiente Javier, rollback git revert; sin QA/main/PROD.

## 2026-11-16 — V19 C05-01.M06.01 revisión causal de evidencias frente a mocks [Javier :40]
- Matriz de seis microtareas M05.01–06 con SHA real, xUnit/Angular/Chromium/HttpClientTesting, controles GitHub exact-SHA y límites explícitos: transporte sintético, tokens sin JWT, sin MySQL/tenant HTTP certificado.
- Readback de seis PLAN LISTO+A5 independientes y verificador documental en memoria con caso positivo y cinco negativas (fila ausente, duplicada, VALIDANDO, sin A5 y sin dueño); sin modificar DAG/COLA ni runtime.
- Riesgo R0 documental; Scope Lock y V5 exact-SHA requeridos; cierre institucional solo con A0–A4 y A5 por otra Task Javier. Sin QA/main/PROD, secretos ni pruebas de servidor inventadas.

## 2026-11-16 — V19 C05-01.M06.02 indice reproducible de artefactos [Javier :30]
- Matriz documental de C05-01.M05.01–06 con seis SHAs y seis runs GitHub Actions, pruebas atribuibles y limites JWT/HTTP/MySQL/WCAG sin certificaciones inventadas.
- Readback seis LISTO+A5 con propietarios y C00-03.M06.06 LISTO+A5, COLA/DAG derivados. Cambios R0 de documentacion sin runtime, CI, seguridad o DB.
- A0-A4 sobre SHA publicado, Scope Lock y V5; A5 independiente de otra Task Javier, rollback git revert; no QA/main/PROD.

## 2026-11-16 — V19 C12-04.M03.06 auditoria sintetica estructurada fail-closed [Javier :30]
- Infrastructure .NET 10: nuevo sink tipado de auditoria del worker sintetico C12-04.M03.05; seis resultados allowlisted, conteos acotados, log estructurado sin actor/tenant/SHA/DSN ni excepciones privadas.
- Un solo IsEnabled y LoggerMessage SkipEnabledCheck; rechazo ante proveedor deshabilitado o fallido, codigos forjados y cancelacion; recibo LogInvoked no implica persistencia o cutover.
- Pruebas xUnit positivas/negativas con adaptador real in-process, logger cambiante, limites, fallos de proveedor y cancelacion; CI GitHub-hosted .NET 10 Release dirigida.
- Sin MySQL/EF/backup real, HTTP/JWT, nuevos endpoints, secretos, QA/main/PROD. A0-A4 exact-SHA y A5 independiente; rollback git revert normal.

## 2026-11-16 — V19 C05-01.M05.06 regresion real visual [Javier :00]
- Playwright Chromium GitHub-hosted: Tab y Enter sin focus programatico, outline, 44px, reduced-motion, responsive 320/375/768/1280, presupuesto de GET sin escrituras, revocacion sintetica 403.
- Riesgo R2 de workflow/test-only; sesion sintetica NO prueba JWT/RBAC/MySQL real. A0-A5 por SHA y rollback git revert; sin QA/main/PROD.

## 2026-11-16 — V19 C00-04.M06.06 cierre documental y DAG [Javier :10]
- Mapa de 11/11 prerequisitos C00-04.M05.01–06 y M06.01–05: PLAN LISTO+A5 institucional, SHA/alcance y limites de xUnit, Chromium, mocks y rollback aislado; cuatro mutaciones documentales adversariales rechazadas en memoria.
- Solo docs/evidencias + CHANGELOG, sin tocar formulas COLA/DAG ni estado de otros IDs; Scope Lock y V5 exact-SHA pendientes de acreditar tras publicacion, A5 por otra Task Javier. Sin JWT/MySQL/WCAG/RTO/PROD certificadas.

## 2026-11-16 — V19 C00-04.M06.04 ensayo rollback aislado [Javier :00]
- Github-hosted ensaya git revert --no-commit en worktree efimero M05.06: arbol identico al padre, cuatro rutas exactas, rechazo adversarial SHA ajeno y cero operaciones sobre datos o refs remotas.
- Riesgo R2 por workflow, sin runtime nuevo; Scope Lock/V5 y A5 independiente exigidos. MySQL/JWT/prod sin certificar; rollback normal git revert.

## 2026-11-16 — V19 C00-04.M06.05 solicitud de revisión A5 independiente [Javier :40]
- Documento R0 de solicitud y criterios A5 para M06.01–.05, revisión por Task distinta, pruebas negativas, locks V17, SHA y gate explícito M06.06; no equivale a aprobación ni crea Tasks.
- A0–A4 documental por Scope Lock/V5 exact-SHA; A5 requiere otro Javier. Sin QA/main/PROD, rollback git revert.

## 2026-11-16 — V19 C00-04.M06.02 evidencia reproducible de gobierno [Javier :50]
- Documenta seis SHAs M05.01–M05.06, capas y pruebas xUnit/Chromium originales con límites explícitos; matriz reproducible de versiones, artefactos, diff y criterios positivos/negativos/fail-closed.
- Auditoría R0 documental: no modifica JWT/RBAC/tenant, DB ni runtime. Gates V17, Scope Lock, V5 y A5 de otra Task Javier obligatorios; sin certificar WCAG/SLO/HTTP/MySQL/QA/main/PROD. Revalidación de CI_CANCEL_GUARD según triggers reales.

## 2026-11-16 — V19 C00-04.M06.03 riesgos, decisiones y límites [Javier :40]
- Registro R0 de ocho riesgos específicos y cinco decisiones del lote Gobierno de cambios: fixtures frente a JWT/MySQL reales, A5/DAG, CI exact-SHA, concurrencia V17, WCAG/observabilidad NO_MEDIDO y rollback sin producción.
- Documentación sin runtime; Scope Lock/V5 del SHA y readback antes de LISTO_DEV, A5 por Task Javier distinta. Sin QA/main/PROD, git revert normal.

## 2026-11-16 — V19 C00-04.M06.01 revisión evidencia real vs mocks [Javier :40]
- Matriz documental R0 de seis M05.01–.06 con SHA y xUnit/Chromium reales; distingue fixtures A/B, HTTP interceptado, JWT/MySQL ausentes y WCAG/Web Vitals NO_MEDIDO sin atribuciones ficticias.
- A0–A4 con Scope Lock y V5 por SHA; A5 revisor distinto, sin activar M06.06, QA/main/PROD. Solo dev; rollback git revert normal.

## 2026-11-16 — C02-04.M05.02 reparación P1 API QA por SHA [reparación directa autorizada]
- Endpoint privado GET /plataforma/gestion/qa/evidencias/{sha} (.NET 10): token JWT firmado, grant global persistido por PlataformaScopeService y auditoría sin tenant. Solo consulta checks GitHub Actions del repositorio público original por SHA de 40 hex, sin URLs del cliente, secretos ni credenciales en Angular.
- Estados CI explícitos SUCCESS/FAIL/PENDING/SKIPPED, faltantes y A5 SIN_VERIFICAR; ni CI verde ni fixture generan aprobación humana o prueba de aislamiento vivo. TestServer/JWT/revocación, errores seguros y MySQL 8.4 efímero en CI dirigida. Validar exact-SHA y A0–A4 antes de LISTO; A5 debe ser independiente. Solo dev, rollback git revert.

## 2026-11-16 — V19 C00-04.M05.06 regresión Chromium real de gobierno [Javier :30]
- Cuatro pruebas Playwright: teclado Tab sin focus artificial, responsive y reduced-motion con hit-target 44px, presupuesto un GET sin escrituras y 403 fail-closed; sesión sintética, sin JWT/RBAC/MySQL/tenant real.
- CI Angular22/Node24/npm11 por SHA sin cancelación, riesgo R2 por workflow; A0–A4 y A5 independiente obligatorios, métricas Web Vitals NO_MEDIDO; solo dev, rollback git revert.

## 2026-11-16 — V19 C15-04.M04.01 reparación P0 y contrato seguridad de ruta [Javier :50]
- Reparación dirigida de checks del SHA 0f2affce: ERP FRONTEND_ROUTE_SECURITY_CONTRACT rechazó ruta nueva pese a conservar plataformaGuard; se añade exactamente el path /plataforma/auditoria-ux-v5 a la allowlist estricta existente, sin alterar la validación de guard, servidor ni controles de rechazo.
- P0-007.A detectó nuevo componente Angular no inventariado; se agrega archivo real al documento y se actualiza conteo estricto 156 archivos (148 features, 8 shared), conservando negativos de ausencia y alta sin catálogo. Sin certificar JWT/RBAC/tenant, WCAG ni mediciones; checks exact-SHA, A5 independiente, rollback git revert, sólo dev.

## 2026-11-16 — V19 C15-04.M04.01 reparación causal inventario de rutas V18 [Javier :50]
- El nuevo enlace UX detectó que el validador C02-04.M05.01 esperaba trece rutas pero app.routes ya contenía catorce antes del cambio y quince después. Se agregan los IDs GV14 visual y GV15 UX al catálogo explícito de solo lectura, sin cambiar los trece ID históricos ni los guards plataformaGuard.
- Corrige aserciones exactas a 15 rutas (script y dos Playwright); mantiene cinco fixtures adversariales y 33 controles Chromium (30 rechazos más tres contratos). NO_MEDIDO de serverAuth/CI_REQUIRED preservado; no certifica autorización JWT/tenant backend. Revalidar checks del SHA nuevo, A5 independiente, sólo dev; rollback git revert.

## 2026-11-16 — V19 C15-04.M04.01 ruta privada de auditoría UX [Javier :50]
- Agrega pantalla Angular standalone y ruta exclusiva plataformaGuard enlazada desde consola autorizada; cinco superficies de auditoría de landing, portal, ERP, builder y consola global. Muestra NO_MEDIDO/SIN_VERIFICAR para WCAG, Web Vitals y defectos: no declara métricas inventadas.
- Tres pruebas Chromium reales: usuario anónimo denegado, acceso positivo con cinco superficies responsive 320/375/768/1280 sin escrituras y sesión 403 opaca. Riesgo R2 por ruta privada, sin cambiar JWT/RBAC/tenant/MySQL; CI exact-SHA Node24/npm11, Scope Lock y V5, A5 independiente. Sólo dev, rollback git revert.

## 2026-11-16 — V19 C05-01.M05.03 integración real proveedor CSS y Signals [Javier :50]
- Tres Playwright/Chromium causales contra Angular real en loopback: cuatro swatches emparejados con variables CSS globales, cambio computado de preset Signal con idempotencia y cero escrituras, y denegación 403 que oculta tokens privados.
- Riesgo R2 conservador por nuevo workflow CI; proveedor local CSS/DOM, no persistencia, MySQL/JWT/RBAC/tenant HTTP real. GitHub-hosted Node24/npm11 lint/build/Chromium, Scope Lock y V5 exact-SHA, A5 de otra Task Javier; sólo dev, rollback mediante git revert normal.

## 2026-11-16 — V19 C05-01.M05.02 contrato DTO seguro de sesión Angular [Javier :40]
- Añade cinco pruebas reales Angular HttpClientTesting/RxJS del GET de sesión: DTO positivo, Empresa 200 forjada, atributos vacíos/flag false/nulo, 403 opaco y revocación antes de revalidar.
- Riesgo R2 por autorización, test-only: HTTP simulado no acredita JWT/RBAC/tenant/MySQL real. Workflow Node24/npm11 exact-SHA con Vitest lint/build, Scope Lock/V5 y A5 independiente; rollback git revert, sólo dev.

## 2026-11-16 — V19 C05-01.M05.05 denegación adversarial del catálogo privado [Javier :40]
- Agrega tres pruebas reales Playwright/Chromium para sesión empresarial 200 forjada, administrador global falso 200 y 403 con mensaje privado; exige redirección a login, cero tokens expuestos y cero escrituras HTTP.
- Casos R2 de interfaz con fixture sintético; sin cambios runtime ni certificación JWT/RBAC/tenant MySQL real. Workflow por SHA con Node24/npm11 lint/build; A0-A4 por CI y A5 independiente. Rollback git revert, sólo dev.

## 2026-11-16 — V19 C05-01.M05.04 aceptacion positiva real en Chromium [Javier :40]
- Dos Playwright Chrome reales en catálogo Angular protegido con sesión sintética: apertura y cierre del contenido, 4 tokens, 5 estados, 2 presets, cuatro viewports y ninguna escritura HTTP.
- Verifica cambios de preset institucional/alto contraste e idempotencia con ARIA; workflow por SHA con Node24/npm11, lint/build y Chromium. Riesgo R1, sin modificar runtime ni afirmar JWT/RBAC/tenant real; A0-A4 requieren CI real y A5 distinta Task. Rollback git revert, sólo dev.

## 2026-11-16 — V19 C05-01.M05.01 contrato unitario del catalogo privado [Javier :40]
- Agrega cinco pruebas Angular TestBed con fallos causales para sesión global, lista cerrada de cuatro tokens, cinco estados, IDs adversariales y revocación de sesión; no modifica runtime.
- GitHub Actions por SHA Node24/npm11 con Vitest, tipado, lint y build; R1. Las sesiones son sintéticas, no pruebas JWT/RBAC/tenant HTTP ni MySQL. A0-A4 exact-SHA, A5 independiente, rollback con git revert; sólo dev.

## 2026-11-16 — V19 C00-04.M05.03 integración local causal [Javier :20]
- Añade ocho casos xUnit de integración real en proceso Domain/Application/Infrastructure: tenant A/B, membresía revocada, snapshot ausente/error, copia SHA/revisión no coincidente, cambio NO_GO y 24 escritores con una sola copia.
- CI .NET10 Release por SHA sin cancelación, riesgo R2. No toca EF/MySQL, endpoint HTTP, proveedor remoto, JWT/RBAC runtime ni producción; A0-A4 requieren GH Actions y A5 independiente. Rollback git revert.

## 2026-11-16 — V19 C00-04.M05.05 seguridad negativa de gobierno [Javier :30]
- Añade siete pruebas xUnit adversariales sobre el servicio real Application: tenant A/B antes de I/O, administrador legacy sin permiso relacional, revocacion, ambito manipulado, NO_GO sensible e ID adulterado, mas control positivo inerte.
- CI .NET 10 Release dirigida por SHA sin cancelar ejecuciones previas, Scope Lock y V5; R2 sin modificar runtime ni afirmar HTTP/JWT/MySQL real. A0-A4 condicionados a pruebas exactas y A5 por otra Task Javier; rollback mediante git revert normal.

## 2026-11-16 — V19 C00-04.M05.02 contrato DTO seguro de gobierno [Javier :20]
- Incorpora xUnit sobre JSON real del DTO inerte: sin Motivo/IdempotencyKey privados, sin permisos, NO_GO para cambios sensibles, limites 600/601, SHA y entradas manipuladas.
- Workflow .NET 10.0.401 Release dirigido por SHA con concurrency sin cancelacion; riesgo R2 por datos y CI. No crea endpoint HTTP ni cambia runtime, tenant/JWT/RBAC/EF/MySQL, QA/main/PROD. A0-A4 sujetos a CI real y A5 independiente; rollback git revert.

## 2026-11-16 — V19 C00-04.M05.04 aceptacion real de gobierno V5 [Javier :10]
- Añade cuatro casos Playwright/Chromium en ruta protegida real: contrato de cuatro controles, revalidacion positiva con dos GET, retorno a plataforma y rechazo de sesion de administrador empresarial 200; fixtures HTTP son sinteticos.
- Workflow Node24/npm11/Angular22 con CI aislada por SHA, lint/build, Chromium real; riesgo R2 por CI. A0-A4 exact SHA y A5 independiente necesarios; sin cambiar runtime, JWT/RBAC, MySQL, QA/main/PROD. Rollback git revert.

## 2026-11-16 — V19 C00-04.M05.01 regresion unitaria de gobierno [Javier :10]
- Agrega regresiones xUnit Domain de límites de claves y motivos, controles CR/LF/TAB, rollback, SHA y NoGo de cambios sensibles; una propuesta con ID bien formado no demuestra pertenencia al plan ni autoriza ejecutar.
- Riesgo R1, pruebas sobre regla preexistente sin mutar Domain ni runtime; requiere .NET10 Release real, Scope Lock, V5 y A5 independiente. Solo dev; rollback git revert.

## 2026-11-16 — V19 C15-04.M04.02 estados privados Angular TestBed [Javier :00]
- Añade cinco pruebas Angular para denegación anónima/403, vacío sin datos institucionales, espera single-flight, éxito informativo y error 503 opaco con reintento.
- Workflow por SHA Node24/npm11 Vitest, lint/build, sin tocar código runtime; servicio sintético NO prueba JWT/RBAC/tenant HTTP ni MySQL real.
- Riesgo R1, A0-A4 sujetos a CI exacta, Scope Lock/V5 y readback; A5 independiente, rollback git revert, sólo dev.

## 2026-11-16 — V19 C15-04.M04.03 branding dinámico y componentes permitidos [Javier :00]
- Añade cuatro Vitest Angular TestBed para marca institucional/neutra, densidad compacta, configuración inmutable y ausencia de operaciones de escritura en dos componentes privados ya existentes.
- Incluye payload SVG y getter hostil para verificar fallback neutro, además de rechazo sin sesión; no atribuye JWT/RBAC/tenant real ni certificación visual global.
- Workflow SHA-específico Node24/Angular22 con lint/build y A0-A4 pendientes de CI exacta; A5 independiente, rollback git revert, solo dev.

## 2026-11-16 — V19 reparación UI C15-04.M04.05 objetivo de teclado 44px [Javier :00]
- CI original Chromium obtuvo 3/4 PASS y 1 FAIL: enlace de retorno de gobierno V5 medía 42.78125px, por debajo del objetivo real de 44px.
- Corrige min-height 44px en el componente existente y activa el workflow al cambiarlo; mantiene prueba adversarial, foco, teclado y contraste sin relajar aserciones.
- Cambio UI R1, revisión CI/SHA Scope Lock/V5 y A5 independiente exigidos; no altera JWT/RBAC/backend/tenant/DB/QA/main/PROD, rollback git revert.

## 2026-11-16 — V19 reparación de selectores C15-04.M04.04 tras CI fallida [Javier :00]
- Corrige dos localizadores Playwright ambiguos: el estado de página del router y el estado de gobierno comparten role=status; se restringe a .state-banner sin eliminar aserciones.
- Primer job exact-SHA 133786915409 obtuvo 3/5 PASS y 2 FAIL de strict mode; la reparación exige cinco Chromium y checks de nuevo SHA antes de LISTO.
- No hay cambio runtime ni JWT/RBAC/tenant HTTP; V17 con owner DEV01, A5 independiente, rollback git revert y Scope Lock STRICT.

## 2026-11-16 — V19 C15-04.M04.05 responsive y accesibilidad Chromium [Javier :00]
- Agrega cuatro pruebas Playwright de denegación, responsive 320–1280, Tab desde inicio, foco visible/44px y contraste >=4.5:1 en dos superficies privadas existentes.
- CI SHA-específica Node24/Angular22/Chromium real de loopback; sesión sintética NO certifica JWT, tenant A/B, MySQL, WCAG global ni SLO.
- Riesgo R1 sin cambios runtime, A0-A4 sujetos a CI y readback exact-SHA, A5 por otra Task Javier; git revert seguro, sólo dev.

## 2026-11-16 — V19 C00-04.M04.06 reparación real de selector Chromium [Javier :50]
- En el SHA e81b2a61 hubo 3/5 PASS y 2 FAIL por selector Playwright global role=status ambiguo (shell sr-only y banner propio). No es PASS ni defecto de autenticación.
- Precisa las tres aserciones a .state-banner [role=status], preservando estados, errores 503, singleflight y seguridad negativa sin tocar código runtime ni desactivar tests.
- Requiere nueva CI GitHub-hosted exact-SHA SUCCESS, Scope Lock/V5 y revisión A5 independiente. Mantener VALIDANDO, rollback git revert y sin QA/main/PROD.

## 2026-11-16 — V19 C15-04.M04.04 formularios, navegación y single-flight Chromium [Javier :00]
- Añade cinco pruebas Playwright de login y gobierno V5: límites de validación, errores 403/503 opacos, navegación Tab/Enter, espera y una sola solicitud de revalidación.
- Workflow por SHA con Node24/Angular22 y Chromium loopback; mocks de sesión son sintéticos, NO prueban JWT/RBAC/tenant HTTP, MySQL ni rendimiento global.
- Riesgo R1 sin cambios runtime; A0-A4 dependen de CI exact-SHA, Scope Lock/V5 y readback, A5 por otra Task Javier, rollback git revert, exclusivamente dev.

## 2026-11-16 — V19 C00-04.M04.06 densidad y progreso real de gobierno [Javier :50]
- Añade cinco pruebas Playwright/Chromium adversariales para ruta privada, 320/390/768/1280, CSS real sin desborde, carga diferida con un GET, error 503 opaco y ausencia de escrituras institucionales.
- Workflow GitHub-hosted reproducible Node24/npm11, lint, build Angular y Chromium local; pruebas de sesión y token son sintéticas, no certifican JWT/RBAC/tenant HTTP/MySQL ni métricas SLO/WCAG globales.
- Riesgo R1, predecesores LISTO+A5, V17 lock/CAS, Scope Lock V5 y A5 independiente; al publicar mantener VALIDANDO hasta CI real SUCCESS y readback, rollback con git revert; solo dev.

## 2026-11-16 — V19 C05-01.M04.06 densidad y revelado progresivo de catálogo privado [Javier :30]
- Añade dos pruebas Playwright al catálogo Angular existente: apertura/cierre del details sin densidad prematura, límites 4 tokens/5 estados/2 presets y respuesta en 320/375/768/1280.
- Niega la ruta ante 403 sintético y exige cero solicitudes de escritura; no afirma code splitting, JWT/tenant real ni métricas web inexistentes.
- Riesgo R1; exige Scope Lock/V5, GitHub Actions dirigido del SHA y A5 independiente; rollback con git revert, exclusivamente dev.

## 2026-11-16 — V19 C15-04.M03.06 reparación A5_FAIL P2 logger dinámico [Javier :40]
- Evita la segunda consulta mutable IsEnabled de LoggerMessage.Define con SkipEnabledCheck=true después de un precheck explícito para Information y Warning.
- Regresión xUnit de logger stateful que retorna FALSE en segunda consulta: verifica una consulta, un Log invocado y recibo no durable; mantiene rechazo si está deshabilitado al inicio.
- Conserva A5_FAIL histórico P2, solicita nueva A5 independiente tras CI exact-SHA. No toca tenant/JWT/RBAC/DB/QA/main/PROD ni desactiva validaciones.

## 2026-11-16 — V19 C13-04.M04.02 estados verificables sin PASS ficticio [Javier :10]
- Extiende la vista privada C13-04 con carga, vacío, éxito limitado a sesión global, error opaco y denegación; catálogo de rutas/módulos permanece SIN_VERIFICAR sin fuente versionada conectada.
- Pruebas Angular TestBed positivas/adversariales y Chromium sintético de anónimo, 503, Tab/320 px; workflow GitHub-hosted por SHA, sin modificar JWT/RBAC/backend/EF/MySQL ni habilitar QA.
- A0–A4 solo tras CI exact-SHA, Scope Lock/V5 y readback; A5 por otra Task Javier. Rollback git revert normal, BITACORA aditiva, sin QA/main/PROD.

## 2026-11-16 — V19 C08-04.M06.05 solicitud A5 independiente [Javier :20]
- Registra solicitud R0 de revisión independiente para el lote sectorial M05/M06, con fuente SHA y autores reales; distingue los LISTO previos de M06.04 aún PENDIENTE y de M06.06 no liberable.
- La solicitud NO es A5_PASS ni prueba nueva, y nunca concede JWT/RBAC/tenant/MySQL o autorización sobre módulos; renderer e integraciones reales siguen sujetos a sus gates, rendimiento NO_MEDIDO.
- A0–A4 documental con GitHub Scope Lock/V5 exact-SHA, readback V17 y A5 por Task Javier distinta; rollback git revert sin QA/main/PROD.

## 2026-11-16 — V19 C08-04.M06.02 manifiesto SHA y evidencias M05 [Javier :20]
- Registra los seis commits causales M05.01–M05.06 de extensiones sectoriales, sus rutas y workflows, con conteos de checks GitHub exact-SHA comprobados y reintentos SKIPPED excluidos de PASS.
- Documenta procedimiento reproducible para git show, check-runs, readback institucional y límites de evidencia JWT/RBAC/tenant/MySQL HTTP y rendimiento NO_MEDIDO sin nuevos grants ni código runtime.
- R0 documental con Scope Lock/V5 y BITACORA V17, A5 solo por otra Task Javier; rollback por git revert y ninguna operación sobre QA/main/PROD.

## 2026-11-16 — V19 C08-04.M06.03 limitaciones y riesgos sectoriales [Javier :20]
- Documenta la matriz de Reservas/Membresías/Catálogo/Sitios por rubro, actualmente solo informativa y sin activación, frente al código Angular y pruebas reales GitHub-hosted 4/4 Chromium de M05.06.
- Distingue denegaciones 403/503 y tenant-context sintéticos de JWT/RBAC/tenant HTTP real; WCAG global, CWV, p95/p99, costos y SLO se mantienen NO_MEDIDO/NO_CERTIFICADO, sin PASSES inventados.
- Entrega R0 exclusivamente documental, con A0-A4/Scope Lock/V5 por SHA y readback V17, A5 independiente pendiente; rollback git revert, sin QA/main/PROD.

## 2026-11-16 — V18 C15-04.M03.06 registro UX estructurado y fallos opacos [Javier :40]
- Nuevo sink ILogger .NET10 para el worker de observaciones UX existente, con evento estructurado fijo 150406, conteo limitado y sin tenant, SHA, token ni evidencia privada en el log.
- Casos xUnit positivos y adversariales: outcome falsificado, indices y capacidad, NullLogger/niveles deshabilitados, excepciones de proveedor y cancelacion; workflow dirigido aislado por SHA.
- "Recorded" solo describe llamada ILogger habilitada, NO durabilidad de auditoria, WCAG/Chromium, JWT/tenant/HTTP ni MySQL; A0-A4 por CI exact-SHA, A5 otra Task Javier, rollback git revert sin QA/main/PROD.

## 2026-11-16 — V18 C08-04.M05.06 regresion Chromium sectorial [Javier :10]
- Agrega cuatro casos Playwright reales con responsive 320/375/768/1280, Tab/foco visible y denegaciones 403/503 simuladas; niega activaciones y peticiones mutadoras.
- GitHub-hosted Node24/Angular 22 + Chromium con concurrency por SHA y cancel-in-progress=false; solo pruebas, workflow y documento, sin modificar componentes ni endpoints.
- Tenant-context es sintetico: NO certifica JWT/RBAC/MySQL/backend real ni SLO; A0–A4 exact-SHA, A5 independiente y rollback git revert obligatorios; no QA/main/PROD.

## 2026-11-16 — V18 C13-04.M04.04 controles reales de interacción [Javier :00]
- Cuatro pruebas Chromium entre dos vistas privadas existentes: anónimo/403, recuperación 503, single-flight de GET y navegación Tab/Enter sin doble acción.
- Workflow GitHub-hosted con Angular y Node24 aislado por SHA; las sesiones/HTTP son sintéticas, no certifican JWT/RBAC/tenant A/B real ni modifican runtime.
- Exigir A0–A4 y CI exact-SHA, A5 de revisor Javier distinto y readback V17; rollback git revert, sin QA/main/PROD.
- Reparación del criterio A0 formularios: quinta prueba Chromium real sobre login, requeridos, límites 50/128, mensajes y cero POST inválidos; mantiene fixtures sintéticos y exige nueva CI exact-SHA.

## 2026-11-16 — V18 C13-04.M04.05 teclado y coherencia visual [Javier :00]
- Agrega cuatro pruebas Chromium entre vistas privadas existentes: anonimo/403 sintético, responsive 320–1280, Tab inicial y contraste calculado.
- Workflow GitHub-hosted Angular/Playwright aislado por SHA, sin cambios runtime; la sesión simulada NO certifica JWT/RBAC ni aislamiento real de tenants.
- A0–A4 exigen CI completada, readback institucional V17 y A5 de Task distinta; rollback por git revert sin QA/main/PROD.

## 2026-11-16 — V18 C11-04.M06.06 cierre de evidencia y mapa DAG [Javier :00]
- Consolida fuentes y A5 de once predecesoras M05/M06 con SHA y límites reales; documenta métricas, SLO, exporter y JWT/MySQL no medidos ni certificados.
- Registra el gate derivado C11-04.M06.06 sin forzar fórmulas, inventar sucesoras o aprobar A5 propia; cambios exclusivamente documentales R0.
- CI Scope Lock/V5 exact-SHA, BITACORA/readback y A5 de otra Task siguen siendo obligatorios; rollback por git revert, sin QA/main/PROD.

## 2026-11-16 — V18 C11-04.M06.05 solicitud A5 independiente del lote [Javier :30]
- Registra petición documental auditable de A5 sobre cinco microtareas M06, con SHAs, riesgos y condiciones de revisión; autor DEV04 nunca se autoaprueba.
- La solicitud institucional NO asegura aceptación de otra Task ni libera DAG o QA/PROD; conserva allowlist Javier6 y dependencias exactas, sin nuevas Tasks/schedules.
- R0 documental: A0–A4 solo tras Scope Lock/V5 exact SHA, evento y readback; rollback git revert, sin cambio runtime/DB/secretos.

## 2026-11-16 — V18 C11-04.M06.04 ensayo Git reversible en runner aislado [Javier :30]
- Agrega GitHub Actions y script que ejecutan un revert sin commit en worktree efímero y prueban igualdad del árbol íntegro con el padre causal de M05.06, sin modificar dev remoto.
- Rechaza archivos fuera de cuatro rutas esperadas y no atribuye restauración MySQL/Aiven, JWT, datos productivos ni SLO al ensayo.
- R2 por CI nueva, A0–A4 y A5 independiente pendientes de comprobaciones exact-SHA/readback; rollback git revert sin force-push.

## 2026-11-16 — V18 C11-04.M06.03 registro de limitaciones y riesgos abiertos [Javier :30]
- Documenta siete riesgos causales del bloque M05 de observabilidad, con fronteras entre fixtures HTTP/JWT sintéticos y pruebas reales de Chromium/.NET en proceso.
- Registra expresamente CWV, SLO, latencia, coste y telemetría externa como NO_MEDIDO/NO_CERTIFICADO; propone remedios medibles sin crear permisos ni DAG falsos.
- R0 documental con A0–A4/Scope Lock/V5 exact-SHA y readback, A5 independiente pendiente; sin código runtime, QA/main/PROD ni datos modificados.

## 2026-11-16 — V18 C11-04.M06.02 versiones, diffs y artefactos CI por SHA [Javier :30]
- Registra seis M05 de observabilidad con SHA causal, IDs de workflow, resultados dirigidos y artefactos; especifica comandos reproducibles de lectura sin ejecutar ni certificar trabajo futuro.
- Separa Vitest/Chromium y LoggerFactory reales de los mocks de sesión y advierte sobre JWT, tenant, exporter, MySQL y SLO/CWV/costes NO_MEDIDO.
- R0 documental sin runtime: Scope Lock y V5 exact-SHA/PLAN-BITACORA readback antes de LISTO; A5 por otra Task Javier, reversión git revert, sin QA/main/PROD.

## 2026-11-16 — V18 C11-04.M06.01 auditoría falsable de evidencias reales vs mocks [Javier :30]
- Confronta seis microtareas M05 de observabilidad con SHA, suite real en GitHub-hosted y alcance demostrable; identifica dónde el navegador es real pero la sesión/HTTP está simulada.
- Declara NO_MEDIDO para SLO, Core Web Vitals, costes y exportadores reales; NO certifica JWT, RBAC, tenant A/B HTTP ni MySQL a partir de unit tests o fixtures.
- R0 documental: A0–A4 exige Scope Lock/procedencia exact-SHA y readback del Sheet; A5 pendiente de Task Javier independiente, rollback git revert, sin QA/main/PROD.

## 2026-11-16 — V18 reparación P2 C13-04.M03.06 recibo honesto ante logger deshabilitado [Javier :50]
- Corrige `Recorded=true` falso: IsEnabled(Information/Warning)=false devuelve AUDIT_SINK_DISABLED y Recorded=false; errores del logger permanecen opacos.
- Añade cuatro regresiones xUnit con niveles deshabilitados, NullLogger y fallos del proveedor; CI .NET 10 Release por SHA y nueva A5 independiente, conservando A5_FAIL histórico. No certifica auditoría durable, JWT ni MySQL.

## 2026-11-16 — V18 C08-04.M05.02 contrato seguro de DTO y errores [Javier :00]
- Repara la propagación de excepciones privadas del lector Application; respuestas fallidas usan un código opaco, sin conceder autorizaciones ni mutaciones.
- Añade cuatro regresiones xUnit adversariales y workflow GitHub-hosted de .NET 10 por SHA, con cancel-in-progress=false.
- A0–A4 requieren CI y Scope Lock exactos; la API HTTP sectorial, MySQL, JWT middleware y proveedores no están implementados ni certificados por este incremento. A5 independiente y git revert como rollback.

## 2026-11-16 — V18 C08-04.M05.03 contrato de integración aislada [Javier :50]
- Seis pruebas xUnit reales de Application/Infrastructure: adaptador de snapshot y fence fallan cerrado ante tenant B, revocación, spoof de permisos, revisión obsoleta y cancelación, sin llamar al proveedor.
- CI GitHub-hosted .NET 10 Release dirigida por SHA, documentación y A5 independiente pendiente; no certifica proveedor vivo, JWT HTTP ni MySQL/EF. Reversión por git revert.

## 2026-11-16 — V18 C08-04.M05.05 denegacion, abuso y aislamiento UX [Javier :40]
- Añade cinco escenarios Playwright adversariales para ruta privada sectorial: anonimo, 401, empresa B denegada 403, 503 opaco y bandera local maliciosa sin permisos efectivos; aserciones reales sobre DOM y peticiones.
- CI GitHub-hosted compila Angular y ejecuta Chromium; grupo de concurrencia aislado por SHA, cancel-in-progress=false. A0–A4 exigen checks reales exact-SHA y A5 independiente.
- Riesgo R2 por tests de seguridad UI y nuevo workflow; HTTP/tenant simulados NO certifican JWT/RBAC/aislamiento real A/B; no altera runtime, API, MySQL, migraciones, QA/main/PROD. Rollback git revert.

## 2026-11-16 — V18 C08-04.M05.04 aceptación positiva sectorial Chromium [Javier :30]
- Añade dos Playwright Chromium reales de aceptación: cuatro tarjetas ilustrativas en 320/1280, rotulación No conectado y cero solicitudes de mutación o activación; denegación 403 sintética oculta la vista y detalles privados.
- Workflow GitHub-hosted concurrente por SHA con npm ci, lint/build Angular y Chromium; HTTP tenant-context es simulado, no JWT/RBAC/tenancy real ni MySQL.
- Riesgo R2 por CI/browser, A0–A4 sujetos a comprobaciones SHA reales y A5 de Task Javier distinta; rollback git revert, sin QA/main/PROD.

## 2026-11-16 — V18 C08-04.M05.01 contrato unitario de capabilities [Javier :30]
- Añade cuatro Vitest Angular TestBed falsables sobre claves sectoriales unicas, estados visuales no autorizativos, revocacion de previews y texto HTML hostil escapado sin ejecutar.
- CI GitHub-hosted aislada por SHA ejecuta npm ci, Vitest dirigido, lint y build Angular productivo; sin cambios de runtime, JWT/RBAC, tenant, API o MySQL.
- R1 test-only; A0–A4 solo con checks reales SUCCESS del SHA causal, A5 de otra Task Javier; rollback git revert normal, sin QA/main/PROD.

## 2026-11-16 — V18 C11-04.M05.06 regresión observabilidad y accesibilidad [Javier :20]
- Cuatro pruebas Chromium nuevas sobre 320/375/768/1280, límites de DOM, foco Tab visible, revalidación GET retenida, 403/503 sin fuga y ausencia de exporter.
- CI SHA-aislada ejecuta Angular lint/build, Playwright real y xUnit .NET 10 de ILoggerProvider in-process; sin reemplazar JWT/MFA/MySQL ni desactivar controles.
- Core Web Vitals/SLO reales NO_MEDIDO; A0–A4 solo tras checks SUCCESS, A5 independiente, rollback git revert, sin QA/main/PROD.

## 2026-11-16 — V18 C02-04.M05.03 laboratorio HTTP JWT y MySQL 8.4 real A/B [Javier :10]
- Agrega un test Node de lectura HTTP JWT real en API localhost con empresas A/B MySQL efímero: token global, aislamiento empresarial, cabecera cruzada, paginación 0/51 y caché no-store.
- Extiende el P4 GitHub-hosted con validación adicional del grant persistido UsuarioRolesPlataforma y membresías UsuarioEmpresas A/B, MySQL 8.4 y .NET 10 reales.
- Riesgo R2 por CI; A0–A4 exigen logs SUCCESS de SHA exacto y A5 otra Task Javier; credenciales sintéticas efímeras, sin runtime/producción/Aiven/QA/main/PROD; rollback git revert.

## 2026-11-16 — V18 C05-04.M05.03 integración de build y bundle real [Javier :10]
- Incorpora pruebas Node sobre build Angular productivo real: index JS/CSS, bytes iniciales, gzip/brotli y fallos adversariales ante corrupción del informe, recurso inexistente o index sin referencias.
- Integra GitHub-hosted npm ci, lint, build, analizador existente y cuatro asserts falsables, con concurrencia aislada por SHA; no modifica runtime, backend, DTO, secretos, JWT, RBAC, tenant ni MySQL.
- Riesgo R2 por CI; CWV reales NO_MEDIDO, DB/HTTP vivo NO_APLICA; requerir A0–A4 causal y A5 por otra Task Javier; rollback git revert normal y nunca QA/main/PROD.

## 2026-11-16 — V18 C08-04.M04.06 densidad y carga progresiva sectorial [Javier :20]
- Tres Vitest de carga aria-busy, estado vacío verdadero, ocultación de tarjetas bajo error y revocación, sin cambios de ejecución ni controles de permisos.
- Tres pruebas Playwright con Chromium real de 320/375/768/1280, cuadrícula adaptativa y ausencia de mutaciones; sesión tenant simulada, JWT/DB vivos NO_MEDIDO.
- Workflow CI por SHA con npm/lint/build y Chromium; A0-A4 y A5 independiente requeridos, rollback git revert sin alterar BITACORA.

## 2026-11-16 — V18 C05-04.M05.02 contrato DTO seguro directorio ligero [Javier :50]
- Refuerza el servicio Angular de directorio existente: página/tamaño acotados, total coherente, IDs únicos y nombres no vacíos; rechazo fail-closed sin nuevos permisos, endpoints ni persistencia.
- Incorpora pruebas Vitest Angular con HttpTestingController: GET válido, vacío 200, 10 DTO adversariales y errores 403/503; CI GitHub-hosted por SHA con lint/build, A0–A4 sujetos a resultados reales.
- R2 API/DTO cliente: autoridad JWT, RBAC/tenants y MySQL reales NO_MEDIDO/NO_APLICA; A5 independiente, rollback git revert, sin QA/main/PROD.

## 2026-11-16 — V18 C02-04.M05.04 aceptación positiva 13 rutas globales [Javier :20]
- Prueba Playwright/Chromium real de catálogo GV01–GV13 y guard Angular con respuesta de sesión sintética 200; cada ruta presenta encabezado esperado y ningún método de mutación.
- Cobertura negativa 403 opaca, revalidación positiva de GET y 320 px sin overflow; backend JWT/MFA/RBAC/tenant/DB no se certifica por mocks.
- CI GitHub Actions SHA-aislado npm/lint/build y Chromium dirigido, A0–A4 causales antes de LISTO; A5 pendiente de otra Task Javier, rollback git revert sin tocar BITACORA.

## 2026-11-16 — V18 C05-04.M05.05 rechazos Chromium de ligereza [Javier :40]
- Añade cinco casos Playwright adversariales: anonimo, sesion 403, directorio 403, revocacion 403 tras 200 y error 503 tras 200. Verifica ausencia de datos previos, errores internos y peticiones de escritura.
- GitHub-hosted lint/build Angular y Chromium con concurrencia por SHA cancel-in-progress=false; A0–A4 requieren CI exact-SHA real y A5 independiente.
- R2 por pruebas/CI; HTTP sintético, no certifica JWT/RBAC/tenants reales, MySQL ni Core Web Vitals; sin cambios runtime ni QA/main/PROD. Rollback git revert.
- CI_REPAIR P2: Playwright restringe la comprobación previa al elemento cell del directorio porque la misma etiqueta se muestra también en un selector; conserva todos los asserts de ausencia tras 403/503 y las cinco pruebas adversariales.
- CI_REPAIR P2: escenario 503 expande el panel nativo details antes de exigir que el aviso sea visible, preservando denegación, limpieza de datos anteriores y ocultación de error interno; ningún cambio al producto.

## 2026-11-16 — V18 C05-04.M05.04 aceptación positiva combinada en Chromium [Javier :30]
- Añade cuatro E2E Playwright Chromium reales con sesión/HTTP sintéticos, directorio 25 filas y ambos controles de ligereza y marca alternados sin GET duplicado, cero operaciones de escritura y anchura 320/1280.
- Comprueba además vacío seguro 200, rechazo 403 y error 503 opaco, de forma independiente y falsable; workflow GitHub-hosted Angular lint/build y Chromium con concurrencia aislada por SHA.
- No afirma JWT/RBAC/tenant real ni MySQL/productores live; LCP/CLS NO_MEDIDO, A0–A4 solo tras CI exact-SHA y A5 por otra Task Javier; sin QA/main/PROD. Rollback git revert normal.

## 2026-11-16 — V18 C05-04.M05.01 regla unitaria reproducible de presupuesto inicial [Javier :30]
- Añade siete tests Node ejecutando el analizador existente de bundle sobre builds aislados: exclusión de chunk lazy, gzip/brotli, límites 1/2 MiB, error sin index y cero inicial sin referencias; pruebas falsables, no auditoría vacía.
- CI GitHub-hosted Node/npm y build Angular real con reporte JSON de bytes de assets; ejecuta pruebas únicamente en dev, con concurrency SHA y cancel-in-progress=false. Requiere A0-A4 y CI exact-SHA para cerrar LISTO; A5 independiente.
- Core Web Vitals de campo NO_MEDIDO; sin alterar runtime JWT/RBAC/tenancy/EF/MySQL, sin QA/main/PROD. Rollback git revert normal, BITACORA intacta.

## 2026-11-16 — V18 C12-04.M03.05 worker lectura sintética aislada [Javier :20]
- Implementación .NET 10 read-only de worker acotado a ocho tickets, snapshot de entrada previo a await y validación estricta de tenant, revisión, SHA y clave; integra adaptador M03.04 real en memoria sin ejecutar DB.
- xUnit adversarial cubre fixture A/B, revocación MFA/JWT/RBAC, falsificación de SHA, duplicados, 0/9, revision stale, respuesta fuente adulterada, error opaco y cancelación.
- Nueva CI GitHub Actions exact-SHA con test dirigido y Scope Lock/V5; mantener VALIDANDO mientras checks no terminen. Solo una Task Javier distinta puede emitir A5.
- Cero migraciones, DDL, MySQL live, backups/restores reales, API/HTTP, scheduler, QA/PROD o secretos; rollback git revert normal con bitácora preservada.

## 2026-11-16 — V18 C11-04.M05.03 integración ILoggerProvider real en proceso [Javier :20]
- Pruebas xUnit con LoggerFactory/ILoggerProvider de Microsoft.Extensions.Logging, EventId 110406, campos allowlisted y rechazo de etiquetas privadas; no es exporter externo ni auditoría durable.
- Casos tenant A/B sintéticos, revocación de membresía/RBAC, SHA discrepante, conteos adversariales, filtros de nivel, errores opacos y cancelación; el provider en proceso es real, la autoridad es fixture.
- CI dirigida en GitHub-hosted exact-SHA, backend Release, Scope Lock y procedencia V5 obligatorios; mantener VALIDANDO si los checks están pendientes y A5 siempre independiente.
- No se modifican EF/MySQL, HTTP/JWT, secretos, producción ni recursos de pago; rollback git revert normal con BITACORA preservada.

## 2026-11-16 — V18 C02-04.M05.02 pruebas reales de contrato API/DTO global [Javier :00]
- Añade xUnit para catálogo global y búsqueda DTO: parámetros inválidos, rechazo de JWT empresarial sin grant, revocación efectiva, DTO minimizado, auditoría global y no-store.
- El controlador productivo no cambia; pruebas EF InMemory y claims sintéticos no acreditan HTTP/MySQL real, MFA o API futura de QA. Exigir GitHub-hosted por SHA y A5 independiente antes de LISTO.
- Riesgo R2, Scope Lock STRICT y seguridad conservados; rollback git revert no destructivo.

## 2026-11-16 — V18 C08-04.M04.01 revalidación Chromium tras drift concurrente [Javier :20]
- La superficie sectorial de lectura fue modificada posteriormente en sus archivos compartidos por entregas M04.02/M04.03; se rechaza equivalencia ficticia con el SHA original y se exige nueva CI causal sobre el código integrado.
- La prueba Playwright propia amplía la frontera responsive a 320, 768 y 1280 px, y conserva rechazo 403, sesión anónima y cero consultas para activar módulos.
- El alcance sigue R1, sin nuevos grants, endpoints, proveedores, migraciones ni datos reales; VALIDANDO hasta tests Chromium, frontend, Scope Lock y V5 completed/success del SHA nuevo. A5 exige otra Task Javier. Rollback: git revert normal.

## 2026-11-16 — V18 C02-04.M05.05 aislamiento de header artificial Playwright [Javier :00]
- Chromium SHA 2766afd obtuvo 9/10 PASS y 1 FAIL: el request llevaba X-Empresa-Id: 1 inyectado por playwright.config.ts extraHTTPHeaders global, no por Angular; la asercion detecto contaminacion de fixture.
- Se desactiva extraHTTPHeaders solo dentro de suite C02-04.M05.05 con test.use({ extraHTTPHeaders: {} }), conservando selector empresa real y rechazo del header generado por interceptor. Sin cambiar auth, tenant ni CI compartida.
- Validacion real repetida por SHA, A0-A5, V17, Scope Lock, rollback normal. No certificar PASS hasta GitHub Actions.

## 2026-11-16 — V18 C13-04.M03.06 logger estructurado seguro en auditoría visual [Javier :50]
- Agrega sink .NET 10 aislado con ILogger y EventId estable, allowlist de resultados y logs estructurados sin texto de proveedor, tokens, claves ni SHA del solicitante.
- Pruebas xUnit de etapas/rechazos, límites 0/>8, cancelación, proveedor con excepción secreta y 32 eventos concurrentes multiempresa; A1-A4 sujetos a CI real exact-SHA, A5 por revisor Javier distinto.
- Riesgo R2; no introduce hosted worker, JWT/RBAC, persistencia MySQL, API, PROD ni auditoría duradera; rollback git revert.

## 2026-11-16 — V18 C02-04.M05.05 cobertura tenant header negativa efectiva [Javier :00]
- Se corrige la fixture del selector de empresa al key real solqaryn_empresa_solicitada_id y se añade asercion observable de ausencia X-Empresa-Id en GET global; antes la variable simulada no ejercitaba el interceptor real.
- Ningun cambio de auth productivo; la prueba falla si un tenant forjado viaja al backend de plataforma. Requerir 10/10 Chromium y P4 JWT/MySQL real por SHA posterior, sin aceptar resultados antiguos de browser.
- R2 A0-A5 y Scope Lock intactos; A5 por otro Task. Revert normal en dev.

## 2026-11-16 — V18 C02-04.M05.05 reparación de aserción 401 por cierre de sesión [Javier :00]
- GitHub Actions run 43955638841 obtuvo 9 PASS y 1 FAIL: ante 401 el interceptor elimina sesión y redirige a /login, pero el test suponía incorrectamente /login?plataforma=no-disponible. Es fallo de expectativa de prueba, no fuga de permisos observada.
- Se corrige únicamente la aserción 401 conservando las negativas 403/503, ausencia de datos privados y conteo de GET, sin cambiar código de auth ni desactivar seguridad. Nueva CI por SHA obligatoria, A3 real P4 y A5 independiente pendientes.
- Scope Lock STRICT, dev exclusivamente y rollback mediante revert normal.

## 2026-11-16 — V18 C02-04.M05.05 ampliacion causal CI HTTP JWT MySQL efimero [Javier :00]
- Se extiende exclusivamente el disparador de la suite P4 de HTTP JWT RBAC A/B y MySQL 8.4 efimero al test negativo C02-04.M05.05, sin cambiar pruebas ni desactivar validadores.
- A3 sigue pendiente de comprobar el job exact-SHA, fases active/revoked/inactive/role-revoked y 0 fallos; browser simulado por separado. No hay afirmacion de PASS hasta lectura de CI real.
- Scope Lock STRICT, V5, cambios solo en dev y rollback git revert. A5 por otro Task Javier.

## 2026-11-16 — V18 C08-04.M04.05 contraste accesible en extensiones sectoriales [Javier :50]
- Corrige contraste de etiquetas en preset oscuro con colores estáticos explícitos; preserva guardas y evita cambios de autorización o módulos.
- Añade cinco pruebas Playwright adversariales: anónimo/403 sintético, Tab real y foco visible, respuesta a cuatro anchos y contraste WCAG 4.5:1 de ambos presets con aserción capaz de fallar ante la regresión.
- R1. CI Angular/Chromium, Scope Lock, V5 y checks globales se evaluarán sobre SHA publicado, sin acreditar PASS anticipado; A5 independiente y rollback git revert. Sin JWT HTTP/MySQL/QA/main/PROD.

## 2026-11-16 — V18 C02-04.M05.05 pruebas negativas de control global [Javier :00]
- Se añaden 10 escenarios Chromium de denegacion anonima, rol empresa, token caducado, 401/403/503, respuesta malformada, revocacion y lectura sin mutacion.
- QA R2: la API es simulada en el browser; JWT HTTP, tenant A/B real, RBAC backend e IDOR/BOLA servidor permanecen NO_MEDIDO hasta pruebas independientes. No certificar LISTO sin A0-A4 causales y A5 de otra Task.
- GitHub Actions dirigido por SHA; Scope Lock/V5, sin tocar produccion, auth real, secrets ni datos. Rollback git revert normal.

## 2026-11-16 — V18 C02-04.M05.01 inventario global de rutas y pruebas Chromium [Javier :40]
- Se incorpora inventario versionado de 13 rutas de plataforma con ID, import y guard reales del HEAD; cinco fixtures negativos detectan desalineación y prohíben afirmar autorización backend desde UI.
- Se añaden 29 pruebas Playwright de navegador real sobre denegación anónima/403 sintético, lectura autorizada sintética y 503 opaco; su PASS depende del job GitHub-hosted exact-SHA, no del manifiesto.
- Riesgo R2; autoridad JWT/RBAC/MFA/tenant A-B y MySQL real NO_MEDIDO/NO_APLICA según scope. Sin cambios de runtime, secretos, QA/main/PROD. A5 por otra Task Javier, rollback git revert.

## 2026-11-16 — V18 C14-04.M04.06 reparación de aserción fail-closed ante 503 [Javier :10]
- CI real 5ceaa490 ejecutó 55 escenarios (54 PASS, 1 FAIL): después de HTTP 503 el contrato privado desapareció correctamente, pero el test esperaba texto dentro de un elemento ausente; error de aserción, no evidencia de fuga.
- Actualiza exclusivamente la prueba negativa para exigir eliminación total del contrato, manteniendo denegación, error opaco, ausencia de spinner y conteo GET; solicita nuevamente Chromium dirigido, Scope Lock, V5 y regresión exact-SHA.
- Riesgo R2 conservador, sin cambios de runtime/tenant/JWT/RBAC/MySQL/PROD; rollback git revert, A5 por otra Task Javier.

## 2026-11-16 — V18 C02-04.M04.06 reparación de regresión Chromium omitida [Javier :10]
- QA_DEFECT_FOUND P2: 10 pruebas Chromium y 12 Vitest anteriores fueron reales, pero la línea Playwright excluía los tres escenarios C02-04.M04.06 nuevos. Los PASS previos no los certifican.
- Corrige filtro de rutas mal concatenadas y ejecuta C02-04.M04.06 en la CI GitHub-hosted por SHA, sin cancelar jobs existentes; nuevas pruebas positivas, negativas y 503 requieren PASS efectivo antes de A0-A4 y revisor A5 separado.
- Sin cambios de runtime, backend, JWT/RBAC/tenant, MySQL, secretos o QA/main/PROD; rollback git revert en dev y Scope Lock STRICT.

## 2026-11-16 — V18 C14-04.M04.06 reparación de selección CI Chromium [Javier :10]
- QA_DEFECT_FOUND P2: el workflow anterior declaraba el spec nuevo como path, pero el comando Playwright no lo ejecutaba; 52 PASS anteriores no acreditaban las tres regresiones C14-04.M04.06.
- Corrige paths mal concatenados y añade el spec C14-04.M04.06 al comando GitHub-hosted real; exige nueva CI exact-SHA con 3 pruebas ejecutadas, Scope Lock, V5 y A0-A4 antes de LISTO, A5 independiente.
- Solo workflow y CHANGELOG, sin runtime/tenant/JWT/MySQL/PROD; rollback git revert, PROJECT_SCOPE_LOCK=STRICT.

## 2026-11-16 — V17 C05-04.M04.06 verificación real de densidad progresiva [Javier :50]
- Añade cinco pruebas Playwright Chromium para guard y 403, carga con GET retenido, densidad 25 registros a 320/375/768/1280 px, controles 44 px, errores 503 y reintento explícito sin duplicados.
- Integra workflow dirigido GitHub-hosted Angular lint/build y Chromium con concurrency por SHA y cancel-in-progress=false; sin cambio de runtime, JWT, tenant, EF, MySQL ni rutas.
- LCP/CLS reales y bundle de campo permanecen NO_MEDIDO; A0–A4 requieren CI exact-SHA, A5 independiente Javier, rollback git revert y Scope Lock STRICT; no QA/main/PROD.

## 2026-11-16 — V18 C08-04.M03.06 auditoría estructurada segura de módulos sectoriales [Javier :40]
- Implementa frontera Application in-process con eventos ATTEMPT/OUTCOME correlacionados, códigos estáticos sin PII/tenant/SHA y fail-closed cuando falla el audit sink, sin registros durables ni permisos nuevos.
- Ocho xUnit dirigidos cubren sondeo seguro, tenant A/B, RBAC revocado, dispatch malicioso, fallos de audit sink, excepción de proveedor, cancelación y dependencias inválidas; CI real exact-SHA pendiente de resultado.
- No DI, proveedor real, HTTP/JWT, MySQL, colas, worker alojado ni QA/main/PROD; A5 independiente y rollback mediante revert normal.

## 2026-11-16 — V18 C13-04.M03.05 worker acotado de revisión visual aislada [Javier :20]
- Implementa orquestador .NET 10 read-only en proceso con puerto al adaptador C13-04.M03.04; hasta 8 tickets, copia de entrada única antes de await, rechazo tenant/SHA mezclados, duplicados, solicitudes de publicación y enumeradores adversariales.
- Ocho regresiones xUnit verifican adaptador y autoridad sintética, revocación, errores opacos, revalidación de resultados, cancelación y ausencia de grants; ninguna sesión JWT HTTP ni MySQL real es certificada.
- Riesgo R2, sin hosted worker, proveedor live, migraciones, DI, endpoints ni QA/main/PROD. CI .NET Release/seguridad/Scope Lock/V5 del SHA publicado es requisito A0–A4; A5 únicamente de Task Javier distinta. Rollback git revert normal.

## 2026-11-16 — V17 C02-04.M04.06 consola global progresiva y densidad [Javier :10]
- Agrega tres pruebas Chromium de densidad responsive 320/375/768/1280, revalidación con carga accesible y petición única, y recuperación segura ante HTTP 503 sin exposición de diagnóstico.
- Incluye las regresiones en el workflow por SHA con cancel-in-progress=false y exige CI hospedada real, Scope Lock/V5, A0-A4 causales y A5 independiente; no confundir pruebas redactadas con PASS.
- Sesión sintética, R2 conservador, no modifica código productivo, JWT/RBAC/tenant, MFA, MySQL, QA/main/PROD ni secretos; rollback git revert normal.

## 2026-11-16 — V17 C14-04.M04.06 densidad y carga progresiva de readiness [Javier :10]
- Añade tres escenarios Chromium de densidad 320/375/768/1280, carga accesible durante revalidación retardada y salida segura ante HTTP 503; sesión simulada, sin métricas reales ni PASS inventado.
- Amplía la selección del workflow existente con concurrencia por github.sha y cancel-in-progress=false, preservando pruebas previas; requiere CI causal GitHub-hosted exact-SHA, Scope Lock y V5, y A5 independiente.
- R2 conservador, solo tests/documentación, sin cambiar JWT, tenant, backend, MySQL, permisos, QA/main/PROD o secretos; rollback git revert normal.

## 2026-11-16 — V17 C12-04.M03.04 adaptador aislado de manifiestos sintéticos [Javier :50]
- Añade Infrastructure C1204IsolatedManifestAdapter con aislamiento tenant/actor, snapshots inmutables y CAS de revisión; nunca evidencia MySQL ni autoridad de cutover.
- Añade nueve pruebas xUnit: A/B, revocación, SHA, doble escritura, frontera de capacidad, lista mutable y cancelación; requiere CI GitHub-hosted exact-SHA.
- Documenta alcance R2, NO_APLICA MySQL/HTTP para este diff, rollback normal y A5 independiente; sin secretos, QA/main/PROD ni migraciones.

## 2026-11-16 — V17 C02-04.M04.05 reparación A5_FAIL P2 de teclado real [Javier :20]
- Sustituye el atajo Playwright link.focus() por recorrido Tab acotado desde foco inicial, con aserción de alcance real; mantiene foco/blanco 44px y exige Shift+Tab + Enter de retorno.
- Conserva denegaciones anónimo/403, responsive y contraste calculado >=4.5:1; exige Chromium GitHub-hosted exact-SHA, Scope Lock/V5 y revisor A5 distinto. A5_FAIL previo no se borra.
- Sin cambios de UI productiva, JWT/RBAC/tenant, HTTP, MySQL, QA/main/PROD ni secretos; rollback git revert normal.

## 2026-11-16 — V17 C15-01.M04.03 reparación A5_FAIL P2 de getters [Javier :20]
- Captura marca/densidad una sola vez y rechaza getters que lanzan o Proxy revocado antes de renderizar; mantiene lista blanca, fallback neutro y permisos intactos.
- Añade tres pruebas adversariales Vitest: TOCTOU de getters cambiantes, error/Proxy revocado y montaje Angular sin filtrar diagnósticos; requiere CI exact-SHA, Scope Lock/V5 y nueva A5 independiente.
- Sin JWT HTTP real, MySQL, QA/main/PROD ni despliegue; preservar A5_FAIL histórico hasta reparación acreditada; rollback con git revert normal.

## 2026-11-16 — V17 reparación causal P0-007.A para dos componentes nuevos [Javier :10]
- El catalogador estricto detectó componente C07-04.M04.03 no inventariado en el SHA 022c5aa7; la nueva entrega C14-04.M04.03 exige otra entrada del mismo registro.
- Registra ambas rutas reales, actualiza exclusivamente conteos 153=145 features+8 shared y conserva comparador exacto, negativos de drift/duplicados y Scope Lock; no desactiva ninguna CI.
- Reparación de baseline compartido, no CLAIM adicional, LISTO ni A5. Esperar CI exact-SHA y evidencia de los dos scopes; no QA/main/PROD.

## 2026-11-16 — V17 C14-04.M04.03 presets visuales readiness [Javier :10]
- Añade panel Angular Signals a ruta privada existente /plataforma/flujo-v5; marca, densidad y contraste por lista permitida, ocultos al revocar sesión, sin go-live ni acciones operativas.
- Seis Vitest y tres Chromium de acceso anónimo, autorización sintética, 403, responsive 320px y cero solicitudes mutadoras; workflow independiente por SHA.
- R2, CI real exact SHA y A5 de otra Task Javier obligatorios antes de certificar; sin cambios backend/JWT/MySQL/QA/main/PROD y rollback git revert.

## 2026-11-16 — V17 C07-04.M04.03 branding reactivo privado [Javier :10]
- Agrega Angular Signals, opciones cosméticas por allowlist estricta y doble control accesible con rechazo al revocar tenant, permiso o ruta; no altera finanzas ni backend.
- Seis Vitest y tres Chromium 320 px ligados a workflow SHA aislado, con denegación y sin mutaciones; no simular JWT/HTTP/MySQL como integración real.
- R2, A0-A4 y A5 independiente obligatorios; sólo dev, rollback git revert y sin QA/main/PROD.

## 2026-11-16 — A5_FAIL TOCTOU C13-04.M03.04 reparación quirúrgica [ChatGPT / Javier]
- Se corrige el defecto P2 detectado por revisión independiente: entrada IReadOnlyList mutable se capturaba de nuevo tras un await y podía cambiar entre allowlist y almacenamiento.
- WriteAsync ahora captura una única copia acotada antes del primer await, valida y almacena los mismos elementos; fallos de enumeración se deniegan sin exposición de datos. Dos pruebas xUnit adversariales cubren mutación concurrente de dos actores y enumeración maliciosa.
- A5_FAIL permanece vigente hasta aprobación independiente; CI de SHA exacto, seguridad/Scope Lock/V5 y rollback git revert requeridos. Sin cambios en MySQL, JWT, QA/main/PROD ni configuraciones de Tasks.

## 2026-11-16 — V17 C14-04.M04.04 navegación y single-flight readiness [Javier :00]
- Añade cuatro pruebas Chromium de navegación privada, 403 de revocación, 503 con reintento y guard de petición duplicada; usa sesiones sintéticas, sin cambiar JWT/tenant/RBAC o backend.
- Amplía únicamente la selección explícita del workflow SHA-aislado, manteniendo pruebas previas y cancel-in-progress=false; exige CI causal real y Scope Lock/V5 antes de LISTO.
- Riesgo R2, A5 de Task distinta, rollback git revert normal; no QA/main/PROD ni servicios productivos.

## 2026-11-16 — V17 CI_REPAIR_C0504_M0403_SELECTOR_STATUS_20261116 [Javier :30]
- Detecta regresión P2 de selector ambiguo en Chromium C05-04.M04.01 tras branding de M04.03: dos roles status dentro de ligereza; job 133489542420 FAILURE en SHA 144819e8.
- Repara solo la aserción Playwright de la prueba predecesora con `.ui-lightness-v5__status`, preservando expectativa de «tres criterios», teclado y ausencia de POST/PUT/PATCH/DELETE; sin cambios de runtime, tenant, JWT/RBAC ni MySQL.
- Reparación R2 por integración CI, A0/A1 y A4 pendientes de CI exact-SHA y readback; A5 independiente; no atribuir PASS anticipado, sin QA/main/PROD y rollback git revert.

## 2026-11-16 — V17 C01-03.M03.05 worker tenant de configuración [Javier :50]
- Implementa worker .NET 10 de catálogo de configuración con lotes 1–16 y claves allowlisted: validates actor/backend, membresía/RBAC/entitlement/revocación, tenant A/B y SHA, sin exponer valores ni otorgar permisos o CAS.
- Añade pruebas xUnit de positivos, negativos, frontera 16/17, duplicación, secretos, fallos de proveedor, cancelación e inmutabilidad; no relaja tests existentes.
- Riesgo R2 en proyección in-process. MySQL/JWT HTTP/browser no intervenidos ni acreditados; require CI exact SHA, Scope Lock/V5 y A5 Task Javier distinta antes de LISTO; rollback git revert; no QA/main/PROD.

## 2026-11-16 — V17 corrección fixture de ruta detalle C07-04.M04.04 [Javier :40]
- Chromium SHA 305f1300 acreditó 9/10 PASS y un FAIL: guard real denegó el detalle porque el fixture solo permitía la ruta listado, no pedidos-venta/:id. No se cuenta CI fallida como éxito.
- Añade al allowlist SIMULADO del test la ruta de consulta explícita del detalle, sin cambiar permiso backend, producto, guard ni acciones mutadoras; conserva anónimo/403 y single-flight.
- Nueva CI GitHub-hosted exact-SHA, Scope Lock/V5 y A5 distinta requeridas. R2 por pedidos, solo dev, sin pagos, JWT real, QA/main/PROD.

## 2026-11-16 — V17 reparación regresión de cobertura por C14-04.M04.02 [Javier :40]
- Chromium del SHA 05e875f4 pasó 46/48 pruebas; dos casos de cobertura fallaron al recibir mensaje sin_permiso en lugar de su error de revocación existente. Ninguna ejecución fallida es PASS.
- Restringe la nueva clasificación HTTP 401/403 solamente a /plataforma/flujo-v5; preserva intacta la vista de cobertura y sus pruebas antiguas, además de los cinco escenarios nuevos de readiness.
- Requiere repetir CI GitHub-hosted exact SHA, Scope Lock/V5 y A5 independiente antes de LISTO; R2 UI, no JWT real, QA/main/PROD; rollback git revert.

## 2026-11-16 — V17 C07-04.M04.04 single-flight y navegación privada de pedidos [Javier :40]
- Bloquea GET duplicados en listado ERP y deshabilita filtro/paginación en vuelo, sin modificar backend, finanzas ni autorización.
- Cinco escenarios Chromium positivos/negativos de anónimo, 403, navegación lector, 503 con retry single-flight y paginación, sin quitar suite anterior ni controles SHA-aislados.
- R2; HTTP/JWT/tenant sinteticos no prueban pagos ni MySQL. Requiere Scope Lock, V5, pruebas causales y A5 de Task diferente; solo dev, rollback git revert.

## 2026-11-16 — V17 C01-04.M03.05 proyección acotada de cronología privada [Javier :50]
- Agrega un worker .NET 10 in-process, determinista y read-only para un máximo de 64 eventos de expediente; rechaza tenant A/B, expediente cruzado, versión futura, eventos duplicados, UTC inválido y evidencia insegura sin producir resultados parciales.
- Incluye diez pruebas xUnit positivas/negativas/frontera, snapshot de salida inmutable, cancelación, documento de límites y rollback. El resultado NO autoriza lectura, persistencia ni medios; no hay adaptador real EF/MySQL/JWT HTTP ni DI/endpoint.
- Riesgo R2, A0-A4 sujetos a CI causal GitHub-hosted y Scope Lock/V5 exact SHA, A5 por Task Javier distinta. Sin QA/main/PROD ni secretos; rollback git revert normal.

## 2026-11-16 — V17 C02-04.M04.03 branding seguro allowlisted de consola global [Javier :30]
- Agrega marca visual dinámica estrictamente local entre dos presets estáticos en la superficie privada del control plane, sin CSS remoto, nuevas llamadas, permisos, grants, tenancy ni datos reales.
- Conservar guard, comprobación de sesión del backend y acceso fail-closed: la selección no autoriza operaciones y se oculta ante 403 o revocación.
- Añade cinco escenarios Playwright Chromium para acceso anónimo/403, dos presets, teclado/responsive 320/375/768/1280, no duplicación de llamadas y revocación; sesiones HTTP sintéticas, sin certificar JWT/MFA o MySQL reales.
- Riesgo R2; GitHub-hosted lint/build, Vitest, Chromium, Scope Lock y V5 exact-SHA requeridos antes de LISTO, A5 independiente, rollback git revert, solo dev.

## 2026-11-16 — V17 C14-04.M04.02 estados seguros de readiness privada [Javier :40]
- Diferencia carga, exito de sesion, vacio de evidencias, error y permiso denegado en /plataforma/flujo-v5 con panel fail-closed y revalidacion GET sin dobles acciones.
- Añade cinco pruebas Playwright/Chromium adversariales, incluido 403 con payload sensible, 503 recuperable y carga diferida, al workflow V5 SHA-aislado sin eliminar pruebas existentes.
- R2 por frontera de sesion; sin endpoints, JWT real, MySQL, DNS, backup, QA/main/PROD ni certificacion de go-live. Exigir CI exact-SHA, Scope Lock/V5 y A5 independiente antes del cierre; rollback git revert.

## 2026-11-16 — V17 C05-04.M04.03 branding local allowlisted de ligereza [Javier :30]
- Añade dos configuraciones visuales Angular fijas (institucional/alto contraste) dentro de /plataforma con control Material, Signal, foco y aria-pressed, sin CSS externo ni configuración persistida.
- Mantiene guard y sesión Plataforma sin modificar JWT/RBAC del backend, APIs, datos de tenants, MySQL ni integraciones.
- Añade cuatro pruebas Chromium con HTTP sintético: anónimo/403, opciones permitidas, doble activación sin nuevos requests, teclado y responsive 320/375/768/1280.
- Riesgo R1; exigir CI GitHub-hosted exact-SHA, Scope Lock/V5 y frontend SUCCESS antes de LISTO, A5 Task Javier distinta y rollback git revert; sin QA/main/PROD.

## 2026-11-16 — V17 C15-01.M04.03 presentación visual tipada de cobertura [Javier :20]
- Incorpora configuración local opcional de marca institucional/neutra y densidad regular/compacta con allowlist estricta e inmutabilidad; rechaza valores arbitrarios sin alterar guard, sesión, tenant o evidencia NO_MEDIDO.
- Añade cuatro aserciones Vitest para variantes, rechazo de inyección, límites de escritura y revocación. Las pruebas Playwright/Chromium existentes siguen siendo obligatorias junto con Angular lint/build, Scope Lock y V5 exact SHA; A5 independiente.
- UI R2 de solo lectura: ninguna autenticación JWT real, persistencia, métricas globales, permisos, QA/main/PROD ni datos reales modificados; rollback git revert.

## 2026-11-16 — Reparación CI C15-01.M04.02 semántica ARIA sin duplicar status [Javier :50]
- SHA b963b248: Chromium 11/14 PASS, tres fallos por status duplicado en mensajes locales que invalidaba los selectores existentes de navegación y contraste; no son PASS.
- Conserva el único status NO_MEDIDO, transforma los nuevos avisos cargando/vacío/listo en aria-live polite y añade aserción real de unicidad en Vitest; no afloja pruebas ni modifica guard o backend.
- CI dirigida y aceptación exact-SHA pendientes, A5 independiente requerida; no JWT/MySQL real ni QA/main/PROD, rollback git revert.

## 2026-11-16 — V17 C12-04.M03.03 autorización preflight tenant A/B [Javier :10]
- Integra política .NET 10 fail-closed al servicio de preparación MySQL privado: actor validado servidor, JWT, rol, membresía/revocación, MFA, tenant exacto, SHA, idempotencia y denegación previa al lector.
- Agrega ocho pruebas xUnit (con múltiples casos) que verifican positivos/negativos, ausencia de llamadas externas ante rechazos y cero autorización SQL/deploy; sin DB, EF, JWT HTTP o restore real.
- R2, CI GitHub-hosted y Scope Lock/V5 exact SHA requeridos antes de LISTO; A5 Task Javier distinta, sólo dev, rollback git revert y sin QA/main/PROD.

## 2026-11-16 — V17 C15-01.M04.02 estados locales de cobertura [Javier :50]
- Amplía la superficie privada existente de cobertura con estados locales cargando, vacío, error recuperable, éxito declarativo y denegación de sesión, sin consultar Sheets ni inventar métricas o aprobaciones.
- Protege la revocación y resultados asíncronos tardíos mediante versión de lectura; valida formas de datos, oculta excepciones y ofrece reintento accesible de mínimo 44 px.
- Amplía Vitest con cinco regresiones adversariales y Chromium con dos pruebas de móvil/denegación sin HTTP mutador; mantiene pruebas anteriores y workflow SHA-aislado, Scope Lock y V5.
- Riesgo R1; la publicación sólo entra VALIDANDO hasta evidencias GitHub Actions exact-SHA y A0-A4; A5 exige otra Task Javier. No certifica JWT/MySQL/tenant HTTP real, QA/main/PROD; rollback mediante git revert.

## 2026-11-16 — V17 C07-04.M04.01 frontera horizontal y scroll interno probado [Javier :10]
- CI 43916659277 volvió a fallar 1/3 Chromium por overflow global 75 px pese a 6 Vitest PASS; no se cuenta como éxito.
- Añade clip horizontal solo al listado y prueba adversarial de scroll interno de la tabla a 320 px, manteniendo columnas, negativos tenant/rol y assert de documento.
- Requiere CI exact-SHA y A5 independiente antes de LISTO; sin API, finanzas, MySQL, QA/main/PROD, revert normal.

## 2026-11-16 — V17 C14-04.M04.05 accesibilidad readiness privada [Javier :20]
- Cuatro Playwright/Chromium reales de denegación anónimo/403, responsive 320/375/768/1280, semántica/labels con puertas NO_MEDIDO, ausencia de controles mutadores/foco atrapado y contraste calculado >=4.5:1.
- Amplía ruta y comando del workflow browser V5 por SHA conservando todas las pruebas existentes, lint/build y seguridad; no altera código UI, guard, JWT/RBAC, DB ni despliegue.
- R2 conservador; mocks NO prueban JWT HTTP/tenant real/MySQL. Requiere CI causal exact SHA, Scope Lock, V5 y revisor A5 diferente; rollback git revert; no QA/main/PROD.

## 2026-11-16 — V17 C07-04.M04.01 reparación CSS tras diagnóstico Chromium 320 px [Javier :10]
- Run 43914301315 confirmó overflow global de 75 px; geometría identificó la tabla de pedidos, no las tarjetas privadas (6 Vitest PASS, Chromium 2/3 FAIL).
- Limita ancho de página/tarjeta/contenedor y mantiene desplazamiento horizontal interno, datos y columnas; no relaja asserts, seguridad, permisos ni cambia backend.
- Exige nuevo CI GitHub-hosted exact-SHA, Scope Lock/V5 y A5 ajena; antes de eso NO_PASS/NO_LISTO; rollback git revert; sin QA/main/PROD.

## 2026-11-16 — V17 C15-01.M04.04 navegación y mensajes cobertura privada [Javier :20]
- Añade cuatro pruebas Chromium de denegación anónimo/403, mensajes NO_MEDIDO, navegación con Enter sin solicitudes mutadoras y ausencia de formularios/controles de doble envío en ruta privada read-only.
- Extiende workflow SHA-aislado C15-01 de ocho a doce Chromium (3 M04.01, 5 M04.05, 4 M04.04), manteniendo cuatro Vitest, Angular lint/build y guards; sin cambiar runtime ni quitar checks.
- R2 conservador por superficie privada; HTTP sintético NO certifica JWT real/tenant backend/MySQL. Requiere CI exact-SHA, Scope Lock/V5 y A5 distinta; rollback git revert; sin QA/main/PROD.

## 2026-11-16 — Reparación CI R1 C05-04.M04.04 detalles colapsados [Javier :50]
- GitHub Actions SHA e62eab99 probó 5/6 Chromium; el caso de doble acción falló al exigir visible un estado dentro de `<details>` cerrado, no por una omisión de protección en el componente.
- Corrige el test abriendo el panel mediante click real sobre summary antes de comprobar el resultado vacío y mantener comprobaciones de doble acción y ausencia de POST.
- Conserva el fallo original como QA_DEFECT_FOUND y exige nuevo SHA con tests reales, Scope Lock/V5 y A5 independiente, sin cambiar Angular, autorización, QA/main/PROD.

## 2026-11-16 — V17 C15-01.M04.05 accesibilidad Chromium cobertura privada [Javier :20]
- Añade cinco escenarios Chromium de denegación anónima/403/ámbito, responsive 320/375/768/1280, semántica/labels, Tab/foco y blanco 44px, y contraste >=4.5:1 sin cambiar código de autenticación ni resultados institucionales.
- Extiende el workflow SHA-aislado de C15-01.M04.01 para ejecutar ocho E2E (tres anteriores más cinco nuevos), cuatro Vitest y Angular lint/build, sin omitir pruebas existentes.
- R2 conservador por ruta privada; sesiones sintéticas NO prueban JWT HTTP, tenant backend, MySQL o producción. Exigir CI real exact SHA, Scope Lock, V5, A5 independiente y rollback git revert.

## 2026-11-16 — V17 C05-04.M04.04 interacción privada y doble acción [Javier :50]
- Verifica la superficie Angular privada existente con seis casos Chromium reales: anónimo, 403 sintético sin mensajes privados, teclado Tab/Enter con aria-pressed reversible, bloqueo de actualización GET concurrente, reintento 503 único y permiso 403 sin fuga.
- Publica suite Playwright, workflow GitHub-hosted SHA-aislado y contrato R1; no modifica rutas, JWT/RBAC, API, MySQL, permisos ni el componente compartido por otras Tasks; pruebas reales requieren SUCCESS exact-SHA antes de LISTO.
- V17 lock 2000000851, A0–A4 causales, A5 independiente Javier; rollback git revert y no QA/main/PROD.

## 2026-11-16 — V17 diagnóstico causal de overflow C07-04.M04.01 [Javier :10]
- Tras corregir el selector Playwright, el SHA 6399072d mantiene 2 Chromium PASS y 1 FAIL real por overflow global de 75 px a 320; se conserva el fallo como gate obligatorio.
- Instrumenta solo geometría de elementos (sin textos ni datos privados) y mantiene intacto el assert global para localizar el elemento causal antes de corregir CSS; no permite saltar pruebas.
- A4 y A5 pendientes, no se declara LISTO; R2 UI, Scope Lock STRICT, dev exclusivo, rollback normal.

## 2026-11-16 — V17 C13-04.M03.04 adaptador aislado de snapshots de revisión visual [Javier :50]
- Añade en Infrastructure .NET 10 un adaptador EN MEMORIA con snapshots por tenant, revisión optimista CAS, máximo 64 claves por tenant y copias inmutables; NO habilita persistencia MySQL/EF, DI live ni HTTP/JWT.
- Doce pruebas xUnit adversariales cubren tenants A/B, revocación, autorización actual, SHA ordinal, expiración, límites, snapshots mutables, cancelación, errores opacos y colisiones concurrentes; necesitan CI real exact-SHA, no son PASS por estar escritas.
- Scope Lock STRICT, V17 candado 2000001997, A0–A4 causales y A5 independiente requeridos; rollback git revert y ninguna escritura QA/main/PROD.

## 2026-11-16 — V17 reparación selector Playwright C07-04.M04.01 [Javier :10]
- El SHA 8820ca59 falló 1 de 3 Chromium por selector getByText('NO_MEDIDO') ambiguo ante dos textos legítimos; 6 Vitest y Angular lint/build sí aprobaron. Se conserva QA_DEFECT_FOUND P2.
- La corrección mantiene la exigencia de NO_MEDIDO y la limita al párrafo role=note único, sin quitar asserts de permisos, tenant, rutas, 320/1280 o ausencia de escritura.
- Requiere nueva CI causal y A5 independiente; no se atribuye PASS al SHA fallido. Sin cambios en API, finanzas, backend, JWT, MySQL, QA/main/PROD.

## 2026-11-16 — V17 C08-04.M03.05 contrato de sondeo read-only de módulos sectoriales [Javier :40]
- Añade frontera Application de integración in-process que requiere fence backend con identidad, tenant A/B, RBAC, módulo, SHA y revisión antes de un sondeo de proveedor de solo lectura.
- Ocho xUnit cubren positivos, negativos, revocación, error seguro, cancelación, idempotencia de lectura y denegación de cualquier dispatch o grant; el test real se certifica únicamente tras GitHub Actions exact-SHA.
- Sin worker, proveedor live, colas, endpoints, EF/MySQL, mutaciones, secretos ni autorizaciones de QA/main/PROD; A5 independiente y rollback mediante git revert.

## 2026-11-16 — Reparación QA C05-04.M04.02 sincronización de sesión 403 [Javier :30]
- GitHub Actions run 43912055324 terminó con 5/6 Chromium, fallo P2 causado por locator.count() antes de completar la carga GET de sesión.
- El test ahora aguarda la superficie autorizada y verifica permiso denegado en directorio, ausencia de reintento y secreto de servidor; no se tocan guard, backend o reglas de autorización.
- Source fallido 62f4da42 no es PASS; nueva CI exact-SHA obligatoria, A5 independiente, sin QA/main/PROD y revert normal.

## 2026-11-16 — V17 C07-04.M04.01 controles privados de pedidos y documentos [Javier :10]
- Agrega cuatro frentes de requisitos en la ruta ERP existente /pedidos-venta, con tenant verificado, Ventas:Ver y allowlist de ruta; no emite pagos ni documentos fiscales.
- Seis Vitest y tres escenarios Chromium se ejecutarán por GitHub Actions en SHA exacto; la API del navegador es simulada, JWT/tenancy backend, fiscalidad y MySQL reales permanecen NO_MEDIDO.
- A0–A4 causales y A5 independiente son obligatorios antes de certificación. Únicamente dev, sin QA/main/PROD, rollback mediante git revert.

## 2026-11-16 — Reparación QA C05-04.M04.02 ante 403 sin redirección [Javier :30]
- GitHub Actions run 43911108150 pasó 5/6 Chromium; un falso negativo P2 exigía /login aunque el interceptor revocaba la sesión sin navegación.
- Test negativo ahora exige ausencia de autoridad, ausencia de superficie privada y pantalla de denegación, manteniendo oculto el mensaje sensible; ningún código de aplicación ni permiso cambia.
- Source fallido 45734566 permanece FAIL; reparación requiere CI causal exact-SHA, Scope Lock y A5 de otra Task Javier; rollback git revert normal.

## 2026-11-16 — V17 C14-04.M04.01 reparación de cobertura CI Chromium [Javier :20]
- Detectado que el workflow browser V5 no ejecutaba el nuevo archivo de Playwright sobre readiness. Se amplía filtro y comando para ejecutar los cuatro casos nuevos además de los tests existentes sin omitir ni deshabilitar seguridad.
- La CI anterior del SHA 75a4448b con Chromium V5 en SUCCESS no prueba A3 del nuevo archivo; se exige nuevo SHA con ejecución causal y Scope Lock/V5 verdaderos.
- R2 controlado, no cambia código runtime ni autoriza QA/main/PROD; rollback git revert y A5 independiente.

## 2026-11-16 — V17 C05-04.M04.02 estados de lectura privada y denegación segura [Javier :30]
- Conecta el feedback de ligereza a los cinco estados reales de GET directorio existente: carga, vacío, error recuperable, éxito y falta de permiso; no muestra nombres ni respuestas crudas en el nuevo panel.
- Mantiene guard de plataforma, no añade endpoints ni llamadas de escritura y aplica reintento GET explícito protegido contra doble activación; incorpora seis pruebas Chromium causales y workflow SHA-aislado.
- Angular 22.2.1, riesgo R1; validación exact-SHA y A5 independiente exigidas. No certifica JWT/MySQL live ni LCP/CLS reales, sin QA/main/PROD; rollback git revert normal.

## 2026-11-16 — V17 C02-04.M04.05 accesibilidad y contraste de consola global [Javier :20]
- Cinco pruebas Playwright Chromium comprueban anónimo/403, responsive 320/375/768/1280, labels, teclado Tab/foco y blanco 44px y contraste calculado >=4.5:1.
- Se amplía workflow SHA-aislado de consola para ejecutar pruebas anteriores y nuevas con Angular lint/build sin bajar gates; no se cambia código de permisos, rutas ni backend.
- R2 por control global; mocks no son JWT HTTP, MFA, RBAC backend, tenant A/B, MySQL o producción. CI real, Scope Lock V5, A5 independiente y revert normal obligatorios.

## 2026-11-16 — Reparación causal CI C14-04.M04.01 selectores exclusivos [Javier :20]
- GitHub Actions run 43909251783 detectó dos fallos en pruebas Chromium V5 por reutilizar la clase genérica .v5-card y provocar coincidencias dobles; se preservan los tests y sus expectativas.
- El nuevo panel read-only usa la clase exclusiva .release-readiness-card sin cambiar guard, datos, autoridad, aria ni funcionalidad; requiere CI nuevamente y A5 independiente.
- No se declara PASS del SHA anterior fallido ni autorización QA/main/PROD; rollback git revert, solo dev.

## 2026-11-16 — V17 C14-04.M04.01 superficie privada de preparación de salida [Javier :20]
- En la ruta ya protegida /plataforma/flujo-v5 se añade un panel de cuatro puertas de readiness: todas SIN_VERIFICAR o NO_MEDIDO, sin datos ni grants nuevos.
- Playwright dirigido cubre anónimo, 403/503 con diagnóstico opaco, sesión sintética, 320/1280 px y ausencia de formularios; CI exact-SHA, Scope Lock y A5 independiente siguen obligatorios.
- Riesgo R2 por contexto de release, pero solo cambia frontend de lectura. No certifica QA/main/PROD, DNS, R2, restore, JWT HTTP ni MySQL; rollback git revert normal.

## 2026-11-16 — V17 reparación CI C15-01.M04.01 alcance y catálogo [Javier :10]
- Repara Playwright Chromium fallido por verificar controles de todo el shell compartido; el assert pasa a comprobar exclusivamente la superficie privada propia, sin ocultar botones ajenos.
- Agrega ruta guardada exacta al allowlist cerrado de auditoría ERP; mantiene el requisito `canActivate: [plataformaGuard]` y validación de sesión servidor, sin abrir prefijos genéricos.
- Registra 151 componentes (143 features + 8 shared) en el catálogo P0-007.A, preservando comparación exacta y pruebas negativas. Conserva SHA original fallido, exige CI causal nueva y A5 de otra Task.

## 2026-11-16 — V17 C15-01.M04.01 superficie privada de trazabilidad de cobertura [Javier :10]
- Implementa nueva ruta lazy protegida por plataformaGuard y componente Angular reactivo de solo lectura para describir cuatro dimensiones de comparación del plan.
- Declara NO_MEDIDO y SIN_VERIFICAR hasta obtener datos reales: no inventa cobertura, no expone datos a un usuario anónimo ni al ámbito Empresa y no añade llamadas HTTP.
- Pruebas: cuatro Vitest y tres Playwright Chromium sintéticos con casos de revocación, 403, 320/1280px y foco; CI, A5 y Scope Lock obligatorios antes de cierre; rollback git revert, solo dev.

## 2026-11-16 — V17 C11-04.M05.02 xUnit de contrato y errores seguros de observabilidad [Javier :40]
- Ocho métodos xUnit adicionales comprueban contrato tipado in-process, denegación tenant A/B, revocación, correlación CRLF, SHA incorrecto, etiqueta sensible, denominador cero y cancelación antes de leer evidencia.
- No se crea endpoint HTTP, DTO compilado nuevo, exporter, proveedor, MySQL ni privilegio; la aplicación solo retorna código de prevalidación, sin otorgar acceso. CI backend Release y seguridad causal obligatorias en SHA exacto.
- Riesgo R2 sintético, sin certificación JWT HTTP ni datos reales; Scope Lock/V5, A5 independiente y rollback normal git revert; QA/main/PROD excluidos.

## 2026-11-16 — V17 C14-02.M04.05 accesibilidad de guía privada [Javier :10]
- Agrega Playwright/Chromium real para 320 y 1280 px, teclado, foco visible, área táctil de 44 px, contraste del tema neutro y denegación 403 sintética.
- No altera componente, router, permisos, JWT, DB ni endpoints; la sesión simulada no acredita pentest, tenancy HTTP real ni auditoría WCAG global.
- Riesgo R2; CI causal del SHA, Scope Lock/V5 y revisión A5 por otra Task Javier; rollback git revert, sin QA/main/PROD.

## 2026-11-16 — V17 C02-04.M04.02 recuperación segura tras error transitorio [Javier :40]
- Los cinco estados privados de consola conservan ocultación fail-closed; ante 503 hay reintento GET explícito y los controles no reaparecen hasta validación del servidor.
- Nuevos Vitest y Chromium exigen 503 opaco, revalidación 200, 403 sin reintento y ausencia de acceso por caché local; CI exact-SHA, Scope Lock, V5 y A5 independiente obligatorios.
- Riesgo R2 solo de interfaz con backend simulado; no certifica JWT HTTP, MFA, RBAC backend, tenant A/B ni MySQL. Solo dev y rollback mediante git revert.

## 2026-11-16 — V19 C05-04.M04.01 superficie privada de ligereza visual [Javier :50]
- Agrega en /plataforma ya protegida una superficie Angular de consulta de presupuestos y objetivos LCP/CLS explícitamente NO_MEDIDOS, con resumen compacto por Signal y semántica accesible.
- Cuatro casos Chromium dirigidos de acceso, 403, teclado/Enter sin HTTP de escritura y responsive 320/375/768/1280; flujo CI exact-SHA SHA-aislado con lint/build; A0–A4 pendientes de resultados externos, A5 independiente.
- No crea ruta, privilegios, APIs ni datos; Scope Lock STRICT, rollback git revert y ninguna escritura en QA/main/PROD.

## 2026-11-16 — Reparación causal CI del catálogo y guard exacto C02-04.M04.01 [Javier :00]
- El SHA 55af37d5 expuso dos fallos de validación: P0-007.A detectó componente nuevo sin registrar y la auditoría ERP rechazó la ruta nueva pese a canActivate: [plataformaGuard].
- Agrega exclusivamente la ruta exacta al allowlist del auditor, que sigue exigiendo guard, autenticación y sesión servidor; actualiza el catálogo reproducible 150/150 sin desactivar verificadores ni pruebas negativas.
- Las 10 pruebas Vitest y cuatro Chromium del SHA origen aprobaron en CI; A4 permanece condicionada a nuevas CI exact SHA, Scope Lock y procedencia V5. Sin QA/main/PROD, rollback git revert normal.

## 2026-11-16 — V17 C02-04.M04.01 consola global auditada de solo lectura [Javier :00]
- Nueva ruta lazy privada con plataformaGuard; cuatro fronteras auditables de control plane sin otorgar privilegios, operaciones ni datos empresariales. Revalidación por GET de sesión global, denegación reactiva y errores opacos.
- Diez pruebas Vitest con acceso, denegación, revocación, 401/403/503, concurrencia y errores síncronos; cuatro Chromium sintéticos, lint y build reales en GitHub-hosted bajo CI SHA aislado.
- Riesgo R2 de superficie de autorización: no certifica MFA, grants, JWT HTTP, tenant A/B ni auditoría durable. Scope Lock, V5 y A5 independiente obligatorios; rollback git revert sin force; solo dev.

## 2026-11-16 — Corrección causal CI C05-01.M04.05 (foco de teclado) [Javier :50]
- La CI del SHA 18343ec3 falló 2 nuevas pruebas por invocar focus() programático al comprobar :focus-visible; 9 pruebas preexistentes sí aprobaron.
- La prueba de foco ahora navega físicamente con Tab en Chromium antes de comprobar el contorno; no se deshabilitan asserts ni se declara PASS sin nuevo run.
- Alcance test-only; no modifica seguridad, permisos, backend ni estado VALIDANDO. Rollback git revert normal.

## 2026-11-16 — V17 C05-01.M04.05 responsive y accesibilidad visual [Javier :50]
- Añade Chromium para etiquetas, foco, teclado, tamaño táctil, contraste y vistas 320/375/768/1280; refuerza selección y foco con colores forzados.
- Cambios limitados a biblioteca Angular privada sin HTTP comercial, migraciones, PII ni permisos nuevos.
- CI exact-SHA y A5 requieren verificación independiente; rollback mediante git revert.

## 2026-11-16 — V17 C14-02.M04.04 navegación, errores y single-flight en seguridad [Javier :10]
- Agrega tres Vitest de navegación interna, doble revalidación single-flight y error 429 sin diagnóstico sensible; dos Chromium sintéticos de retorno a plataforma y error 429.
- Cambia únicamente pruebas, etiqueta de workflow y documentación; frontend productivo, API, DTO, MySQL, JWT y reglas tenant quedan intactos.
- Riesgo R2 por frontera de acceso; CI exact-SHA requerida en GitHub-hosted, A5 independiente de otra Task Javier y rollback git revert normal; no QA/main/PROD.

## 2026-11-16 — V19 C05-01.M04.04 verificación de interacción visual [Javier :50]
- Verifica navegación real por Tab y Enter, mensajes accesibles, permisos de plataforma y 403 sintético en la biblioteca privada Angular.
- Evita segunda transición en la selección local y prueba ausencia de formularios y de escrituras HTTP; añade dos Vitest y dos Playwright Chromium adversariales.
- No acredita un formulario comercial real ni JWT HTTP, API de negocio o MySQL; CI causal exact-SHA, Scope Lock, V5 y A5 de revisor independiente. Rollback git revert normal.

## 2026-11-16 — V18 C02-04.M03.06 auditoría estructurada privada del control plane [Javier :00]
- Añade sink .NET 10 de auditoría no durable con EventId y códigos allowlist, consultando evidencia backend fresca de plataforma, tenant A/B, MFA, permiso, revocación y SHA exacto antes de emitir logs.
- Añade 13 pruebas xUnit con casos positivos, negativos, frontera, excepción y cancelación; CI GitHub-hosted, Scope Lock, V5, seguridad y Authorization son obligatorios antes de certificar LISTO.
- No registra actor, tenant, tokens ni payload; no habilita acceso global, proveedor real, auditoría durable, MySQL/JWT HTTP ni despliegue. Riesgo R2; A5 independiente y rollback git revert normal.

## 2026-11-16 — V18 C05-01.M04.03 branding local permitido [Javier :50]
- Añade dos presets visuales tipados a la biblioteca privada Angular con allowlist, selección en memoria, denegación por sesión y sin CSS/URL inyectables ni escrituras tenant.
- Incorpora Vitest adversarial para cadenas arbitrarias, Empresa y revocación y Chromium para selección real y responsive 320/768/1280; requiere CI exact-SHA, Scope Lock, V5 y A5 independiente.
- No acredita branding persistido, JWT HTTP ni MySQL. Solo dev, rollback git revert, sin QA/main/PROD.

## 2026-11-16 — C05-01.M04.02 estados visuales privados V18 [Javier :30]
- Amplía el catálogo Angular de plataforma con cinco muestras tipadas de carga, vacío, error, éxito y permiso, visibles solo con sesión de plataforma verificada; no introduce HTTP ni escrituras.
- Añade regresiones unitarias positivas y negativas y Playwright Chromium para denegación 403, revocación, teclado, ausencia de acciones y responsive 320/768/1280; exige CI exact-SHA y A5 independiente.
- Preserva JWT/RBAC/tenant, cuatro tokens anteriores, Scope Lock STRICT y el candado V17. Sin QA/main/PROD; rollback con `git revert`.

## 2026-11-16 — V17 C03-04.M04.04 navegación, mensajes y doble acción [Javier :10]
- Añade regresiones Vitest de doble click, retorno a plataforma, ausencia de formularios y revocación inmediata de la guía privada.
- Amplía Chromium dirigido con navegación positiva y 429 sintético retardado: un solo GET, guía oculta y diagnóstico interno no visible. Exige CI GitHub-hosted exact-SHA, Scope Lock, V5 y A5 independiente.
- Sólo pruebas y documentación R2 por superficie privada; ningún cambio runtime, API, JWT/RBAC, tenant, MySQL ni restauración. Solo dev; rollback git revert, sin QA/main/PROD.

## 2026-11-16 — V17 reparación P0-007.A C05-01.M04.01 [Javier :40]
- Repara el fallo de inventario exact-HEAD del SHA `0509d36948f2` agregando la ruta real `frontend/src/app/features/plataforma/design-system-v5.component.ts`, preservando la historia anterior.
- Actualiza umbrales estrictos a 149 rutas (141 features y 8 shared), manteniendo pruebas negativas de archivos faltantes, ficticios y duplicados; requiere GitHub Actions exact-SHA.
- Sin cambios de runtime, permisos, JWT/RBAC, tenants, datos, migraciones ni workflows; A0–A4 y A5 independiente permanecen obligatorios. Solo dev; rollback `git revert`.

## 2026-11-16 — C05-01.M04.01 biblioteca visual privada V5 [Javier :40]
- Añade un catálogo desplegable de tokens y tipografía a la administración de plataforma existente; sin ruta ni autorización nuevas, sin APIs de negocio y con negación explícita ante revocación de sesión.
- Incluye prueba Angular de anónimo/empresa/administrador/revocación y Chromium dirigido para 403 sintético, teclado y responsive 320/768/1280. Requiere CI causal y A5 independiente, sin marcar PASS anticipadamente.
- Solo `solqaryn/Solqaryn@dev`, candado V17 fila 740, rollback `git revert`; no QA/main/PROD.

## 2026-11-16 — V17 reparación QA P2 C01-03.M03.04 snapshot inmutable [Javier :20]
- Corrige el A5_FAIL P2 de DEV06: el adaptador sintético de Infrastructure ya no devuelve un HashSet mutable; usa FrozenSet con comparador ordinal estricto y mantiene el rechazo de tenant ajeno.
- Agrega regresión xUnit adversarial que rechaza el cast del conjunto devuelto a HashSet y verifica que mutaciones de la fuente no contaminen lecturas futuras; el test fallaría en el SHA anterior 408d188c.
- La reparación se limita a adaptador, test y documento C01-03.M03.04; requiere GitHub Actions .NET Release/xUnit, seguridad, autorización, Scope Lock y V5 del SHA publicado, con A5 independiente nueva y A5_FAIL histórico preservado. No activa JWT/EF/MySQL/HTTP, QA/main/PROD ni secretos.

## 2026-11-16 — V17 C11-04.M05.05 caso negativo 429 y confidencialidad [Javier :30]
- Añade prueba Playwright Chromium negativa para sesión 200 seguida de 429: cuatro tarjetas y branding privado se ocultan, payload con secreto no aparece y no hay reintento ni exporter.
- Conserva prueba positiva M05.04 y regresiones 403/503; el 429 es sintético, no acredita JWT/HTTP/tenant server-side, y no modifica runtime, MySQL ni proveedores.
- Requiere CI exact-SHA de Scope Lock/V5/frontend/Chromium antes de LISTO, A5 de otra Task Javier; rollback git revert, sin QA/main/PROD.

## 2026-11-16 — V17 C11-04.M05.04 aceptación positiva de observabilidad privada [Javier :30]
- Añade un Playwright Chromium dirigido para HTTP 200 sintético, revalidación sin GET duplicado, cuatro tarjetas read-only, responsive 320/1280 y ausencia de exporter.
- Preserva negativos existentes 403/503, revocación, foco, carga y seguridad; no modifica producto, backend, JWT/RBAC, MySQL o proveedores y no atribuye autenticación HTTP real al mock.
- CI causal Scope Lock/V5/frontend/Vitest/Chromium pendiente de GitHub Actions sobre SHA publicado; A5 requiere otra Task Javier, rollback por git revert y sin QA/main/PROD.

## 2026-11-16 — V17 C11-04.M05.01 pruebas de regresión unitarias de observabilidad [Javier :10]
- Agrega cuatro casos Vitest a la suite existente: aceptación sin exporter, revocación durante respuesta pendiente, empresa sin privilegio y error 429 sin fuga de secreto.
- Pruebas reales pendientes de GitHub Actions en SHA nuevo; no se atribuye PASS anticipado. La suite dirigida incluye Chromium, frontend, Scope Lock y V5.
- R2 prudente por fronteras de autorización probadas, sin cambiar runtime productivo, JWT, MySQL, proveedores ni QA/main/PROD; A5 independiente.

## 2026-11-16 — V17 reparación QA P2 C03-04.M04.03 botones de accesibilidad [Javier :10]
- Repara aserción de C03-04.M04.05 que esperaba un botón, aunque C03-04.M04.03 introdujo un segundo botón cosmético de densidad: exige exactamente dos, type=button y textos explícitos.
- SHA origen 1118cf68932fe0ef10d93ca0d5219421f4103192; CI fallida RUN 43856490141 con 3/4 Vitest. El fallo histórico no es PASS; exige CI causal exact-SHA, Scope Lock, V5, Chromium y readback.
- Solo test y changelog, sin cambios backend, JWT, tenant, MySQL ni despliegue; A5 de otra Task Javier.

## 2026-11-16 — V17 continuidad CI por SHA en dev [recuperación de automatizaciones]
- La aceptación funcional integral deja de cancelar la evidencia de un commit cuando otra microtarea publica en `dev`: `concurrency.group` ahora incluye `github.sha` y `cancel-in-progress=false`.
- Conserva todos los jobs y gates del workflow; un `FAIL`, `SKIPPED`, `CANCELLED` o `IN_PROGRESS` requerido sigue sin acreditar PASS. No modifica fuentes, horarios ni prompts de las seis Tasks; GitHub Actions puede consumir más minutos al ejecutar SHAs distintos.
- Cambio exclusivo de `.github/workflows/catalogos-aceptacion.yml`, rama `dev`; rollback `git revert` normal si hay regresiones o saturación. Validar Scope Lock/V5 y ejecución propia por nuevo SHA; no implica corrección del retraso nativo del programador.

## 2026-11-16 - V17 QA repair C11-04.M04.06 403 sintetico Chromium [Javier :40]
- Repara la prueba dirigida que restauraba token al recargar por page.addInitScript; verifica respuesta 403 explicita en pagina nueva sin acceso a las tarjetas privadas.
- Conserva los positivos de pintado progresivo, densidad y responsive; fallo anterior 13/14 Chromium no cuenta como PASS, requiere nuevo SHA y Scope Lock/V5.
- Solo fixture E2E y documentacion; sin cambios a JWT, guard, API, MySQL, QA/main/PROD; A5 independiente.

## 2026-11-16 — V17 C10-04.M04.02 regresión de excepción síncrona P2 [Javier :20]
- A5_FAIL DEV05 detectó un posible bloqueo de carga cuando obtener() lanza antes de devolver Observable; el componente vigente ya protege ese límite con RxJS defer.
- Añade dos Vitest adversariales para 403 síncrono y error inesperado: estado seguro, sin datos protegidos, aria-busy liberado, botón reintento y secretos no filtrados.
- Reparación de evidencia R2 sin cambios backend/JWT/MySQL; A5_FAIL histórico se conserva y requiere nueva A5 independiente tras GitHub-hosted CI exact-SHA, Scope Lock y procedencia V5; sin QA/main/PROD.

## 2026-11-16 — V17 C03-04.M04.03 presentación segura Reactiva [Javier :10]
- Integra Angular Signals con marca, densidad y contraste por allowlist estricta; excluye CSS, HTML, URL y tokens arbitrarios.
- Añade un control accesible de densidad, pruebas Vitest negativas de inyección/revocación y Chromium 320 px sin solicitudes operativas.
- Riesgo R2 de UI privada: no certifica JWT/MySQL/RESTORE reales; requiere exact-SHA CI, Scope Lock y V5, A5 distinto, sin QA/main/PROD.

## 2026-11-16 - V17 C11-04.M04.06 densidad y pintado progresivo observabilidad [Javier :40]
- Añade content-visibility:auto y tamaño intrínseco de reserva a cuatro tarjetas descriptivas, con padding fluido; conserva árbol semántico y guard privado.
- Amplía Playwright Chromium real con comprobación CSS, scroll, 320/768/1280, denegación de token y ausencia de exporter; exige lint, build, Scope Lock y procedencia V5.
- R1 Angular: ningún dato real, JWT HTTP, EF/MySQL o métricas de mejora medidas; A5 independiente, rollback revert normal y sin QA/main/PROD.

## 2026-11-16 - V17 C00-04.M04.01 reparacion QA P2 de navegador privado [Javier :20]
- Agrega tres pruebas Playwright Chromium reales dirigidas a gobierno-cambios-v5: rechazo anonimo, 403 sintetico sin contenido protegido y acceso sintetico de consulta.
- Comprueba responsive 320 px, Tab/foco y ausencia de solicitudes operativas; no cambia el guard, endpoint, tenant, JWT ni backend, y no infiere autenticacion HTTP real.
- A5_FAIL historico permanece; nueva A5 independiente solo tras CI causal del SHA y readback. Scope Lock/V5, sin QA/main/PROD.

## 2026-11-16 - V17 C12-01.M02.06 dependencias y salida documental [Javier :50]
- Consolida catorce gates D01-D14 y diez criterios O01-O10 con fuentes M01.06 y M02.01-05, owner, seguridad, evidencias y rollback; no libera G01-G16 de infraestructura.
- Define doce pares positivos/negativos futuros, distingue identidad de solicitud y HMAC completo antes de redacción, y mantiene 0 pruebas runtime ejecutadas en este R0.
- Entrega exclusivamente Markdown con Scope Lock/V5 exact-SHA y A5 independiente pendiente; sin VPS, DNS, MySQL, API, QA/main/PROD, pagos ni secretos.

## 2026-11-16 - V17 reparacion A5_FAIL P2 C12-01.M02.05 idempotencia documental [Javier :30]
- Corrige ENV10 con logicalRequestId estable y scope actor/ambiente/tenant/operacion/version; no fusionar solicitudes distintas por payload identico.
- Corrige ENV12 con HMAC versionado sobre contenido canonico completo antes de redaccion, y I01/I02/P02/P03 con concurrencia, PII diferente y conflictos negativos.
- Preserva A5_FAIL P2 DEV05; exige revisor Javier independiente y Scope Lock/V5 exact-SHA. Solo R0 documental y seis pruebas futuras NO_EJECUTADAS; sin MySQL/HTTP/Hostinger/QA/main/PROD.

## 2026-11-16 - V17 C02-02.M04.05 reparacion P2 foco alto contraste [Javier :40]
- Corrige la precedencia CSS del contorno de foco: el design system global fuerza 3px con !important y los controles privados de placement requieren 4px en teclado/alto contraste.
- Mantiene las pruebas reales Chromium de Tab, 320/768/1280 y revocacion 403 sin atenuar aserciones. El CI original fallido no se contabiliza como PASS; exigir repeticion y Scope Lock/V5 en SHA de reparacion.
- R2 acotado a estilos de pantalla privada y evidencia; sin cambios JWT/RBAC/tenant/backend/MySQL, QA, main ni PROD; A5 independiente pendiente.

## 2026-11-16 - V17 C11-04.M04.05 accesibilidad real de observabilidad privada [Javier :50]
- Amplía targets táctiles a 44 px sin desbordamiento y conserva foco visible en navegación privada Angular.
- Añade tres pruebas Chromium dirigidas: teclado Tab real sin focus() sintético, responsive 320/768/1280 con rejilla adaptativa y contraste WCAG calculado en el navegador.
- R1 Angular, sesión sintética no sustituye JWT HTTP; requiere CI causal Scope Lock/V5, Vitest/Chromium y A5 independiente, sin MySQL, proveedores, QA/main/PROD.

## 2026-11-16 - V17 C11-04.M04.04 navegacion y doble accion de observabilidad [Javier :20]
- Estructura las acciones de observabilidad como navegación semántica y enlaza el botón de revalidación al mensaje accesible aria-live sin formulario ni escritura.
- Agrega dos Vitest y dos Chromium dirigidos: enlace, foco accesible, carga bloqueada, 503 sin fuga de secretos y una sola llamada GET durante revalidación solapada.
- R1 Angular sin cambiar rutas, backend JWT/RBAC/tenant, MySQL, exporter ni métricas; CI exact-SHA, Scope Lock/V5 y A5 independiente obligatorios.

## 2026-11-16 - V17 C13-01.M02.06 contrato de salida de privacidad [Javier :10]
- Establece dieciseis puertas G01-G16 de consentimiento, versionado, tenants, revocacion, idempotencia, legal hold y A5 sin habilitar runtime.
- Documenta doce casos T01-T12 FUTUROS_NO_EJECUTADOS (0/12) y ocho salvaguardas RB01-RB08, incluyendo pruebas MySQL/Chromium futuras, sin afirmarlas aprobadas.
- R0 documental: Scope Lock/V5 causal, candado V17, BITACORA y readback; sin API, EF/MySQL, Angular, cookies, textos legales ni QA/main/PROD.

## 2026-11-16 - V17 C11-04.M04.03 branding tipado de observabilidad privada [Javier :20]
- Agrega identidad visual institucional o neutra fija y tipada a la ruta privada de observabilidad, visible solo si el backend confirma sesión de plataforma; valor desconocido revierte a neutro.
- Amplía Vitest (cinco casos nuevos) y Chromium (dos escenarios dirigidos) para variantes, revocación, ámbito empresarial, valor inválido, 403 y ancho móvil sin desbordamiento; CI exigida por SHA.
- R1 Angular sin nuevas operaciones, métricas, JWT, RBAC, EF/MySQL ni exportadores; A5 independiente y cierre solo con Scope Lock/V5/readback causales.

## 2026-11-16 - V17 C13-01.M02.05 eventos e idempotencia de privacidad [Javier :00]
- Define doce eventos EV01-EV12 y doce campos ENV01-ENV12 futuros para decisiones, evidencia, revocacion, derechos, retencion y readback opaco por tenant/sujeto.
- Disena doce invariantes, ocho limites, ocho errores y doce pares T01-T12 positivos/negativos FUTUROS_NO_EJECUTADOS (0/12 runtime); no crea broker, outbox, workers, API, tablas o datos.
- El nonce por solicitud logica y el digest canonico completo evitan replay, colisiones por carga redactada y falsas aprobaciones; conserva JWT/RBAC/revocacion y denegacion ante fallos.
- R0 documental: exige Scope Lock, V5, lock V17, readback y A5 independiente, sin autorizar QA/main/PROD ni proveedores.

## 2026-11-16 - V17 C13-01.M02.03 persistencia conceptual e indices de privacidad [Javier :00]
- Propone dieciseis claves/indices K01-K16, ocho etapas futuras MX01-MX08 y doce fixtures sinteticos F01-F12 para decisiones, revocacion, derechos, retencion y tenant A/B.
- Define idempotencia con nonce estable por solicitud logica y digest completo de payload canonico; evita reintentos cruzados, colision por datos redactados, IDOR/BOLA y revocacion obsoleta.
- Incluye doce pares T01-T12 FUTUROS_NO_EJECUTADOS (0/12 runtime); no modifica EF, MySQL, API, cookies, datos, proveedores ni politica legal.
- Riesgo R0 documental; exige Scope Lock y V5 causal, candado V17, readback y A5 independiente; ningun paso a QA/main/PROD.

## 2026-11-16 - V17 C13-01.M02.04 configuracion tipada y flags de privacidad [Javier :50]
- Define dieciseis opciones CFG01-CFG16, doce flags F01-F12 false por defecto y ocho errores EC01-EC08 para privacidad, consentimiento, retencion, revocacion y politicas por tenant.
- Agrega doce pares P/N FUTUROS_NO_EJECUTADOS (0/12 runtime) y restricciones contra autorizacion por UI, cross-tenant, JWT revocado, proveedores no aprobados y borrado destructivo por TTL inferido.
- Documento R0: sin API, Angular, EF/MySQL, cookies, datos o politica legal activa; conserva G01-G16, Scope Lock/V5 exact-SHA, candado V17 y A5 independiente.

## 2026-11-16 - V17 C13-01.M02.02 operaciones y DTO conceptuales de privacidad [Javier :50]
- Define ocho operaciones OP01-OP08, doce DTO, doce errores ER01-ER12 y dieciseis validaciones V01-V16 para privacidad, consentimiento, revocacion, retencion y derechos por tenant/sujeto.
- Registra doce pares P/N FUTUROS_NO_EJECUTADOS (0/12 runtime), denegacion por defecto, IDOR/BOLA, revocacion, correlacion e integridad sin atribuir cumplimiento legal ni permisos.
- R0 documental: no crea API, EF/MySQL, cookies, Angular, proveedores, workers ni datos; exige Scope Lock/V5 causal, readback y A5 independiente bajo candado V17.

## 2026-11-16 - V17 C13-01.M02.01 entidades, ownership e invariantes de privacidad [Javier :50]
- Define dieciseis entidades conceptuales E01-E16 y veinte invariantes I01-I20 para versiones, propositos, decisiones, revocacion, retencion, evidencia minima y derechos por tenant/sujeto.
- Incluye doce pares positivos/negativos T01-T12 FUTUROS_NO_EJECUTADOS (0/12 runtime); bloquea silencio como consentimiento, IDOR/BOLA, replay tras revocacion, PII en auditoria y eliminacion por plazo inventado.
- Solo R0 documental: sin crear tablas EF/MySQL, endpoints, cookies, politicas juridicas, proveedores, servicios ni datos; preserva G01-G16, Scope Lock/V5 causal y A5 independiente de otra Task Javier.

## 2026-11-16 - V17 C02-02.M04.05 responsive, teclado y contraste privados [Javier :40]
- Refuerza foco visible de 4 px, contraste elevado y colores forzados en revisión privada de placement, manteniendo guard, revalidación y ocultación de datos.
- Añade tres pruebas Chromium: 320/768/1280 sin desbordamiento, navegación Tab real y denegación 403 sin payload expuesto; requieren CI dirigida real en SHA.
- R2 visual-control-plane sin cambiar JWT backend, MySQL, DSN, API, infraestructura ni entornos; A5 independiente y bitácora institucional siguen obligatorias.

## 2026-11-16 - V17 C12-01.M02.05 eventos, idempotencia y limites conceptuales [Javier :30]
- Define doce eventos EV01-EV12, doce campos ENV01-ENV12, doce invariantes I01-I12, ocho limites, ocho errores ER-EV01-08 y doce pares P/N FUTUROS_NO_EJECUTADOS (0/12 runtime).
- Prohibe efectos por mera revision documental, cross-tenant, secretos o JWT en cargas, retry infinito y cutover sin G01-G16/R3; conserva configuracion actual Vercel/Render/Aiven/Cloudinary.
- Solo R0 documental: ningun broker, trabajador, endpoint, DB, DNS o proveedor activado; Scope Lock/V5 causal, readback y A5 independiente obligatorios.

## 2026-11-16 - V17 C12-01.M02.03 persistencia conceptual y migraciones reversibles [Javier :10]
- Documenta 16 claves e indices propuestos K01-K16, ocho etapas futuras de migracion MX01-MX08, doce fixtures sintéticos F01-F12 y 12 pares positivos/negativos P/N de topologia objetivo.
- Exige version y entorno compuestos, tenant A/B, JWT/RBAC, revocacion, secretos opacos, red privada, compatibilidad Oracle EF/MySQL 8.4 aislada y rollback real futuro; no se crean tablas, indices, migraciones, endpoints, proveedores ni datos.
- Evidencia R0: cero pruebas runtime, migraciones, fixtures y restore ejecutados; A3 NO_APLICA motivado, A0-A4 con Scope Lock y procedencia V5 exact-SHA y A5 independiente de otra Task Javier.
- No autoriza Hostinger, DNS, secretos, QA/main/PROD ni desactiva los gates G01-G16 o la dependencia institucional del DAG.

## 2026-11-16 - V17 C12-01.M02.04 configuracion tipada y flags futuros [Javier :30]
- Define dieciseis campos CFG01-CFG16, doce flags F01-F12, ocho errores ER-CFG01-08 y doce pares P/N futuros para evaluar topologia objetivo sin tocar entorno vivo.
- Todos los flags son false por defecto, especialmente cutoverEnabled; preserva tenant A/B, JWT/RBAC, secretos opacos, segregacion DEV/QA/PROD y gates G01-G16, sin activar Hostinger, DNS, MySQL ni PROD.
- Solo R0 documental: cero pruebas de runtime ejecutadas; exige Scope Lock/V5 exact-SHA, readback institucional y A5 de otra Task Javier.

## 2026-11-16 - V17 C11-01.M02.06 contrato documental de salida modelo relacional [Javier :00]
- Fija dieciseis gates G01-G16, ocho pasos futuros de rollback R01-R08 y doce pares T01-T12 positivos/negativos FUTUROS_NO_EJECUTADOS (0/12 runtime) para cerrar el diseño conceptual M02.
- Reconciliacion documental E01-E16, OP01-OP08, D01-D12, K01-K12, F01-F12, EV01-EV12 e invariantes; proteccion tenant A/B, legacy NULL, RBAC/revocacion, transacciones e idempotencia sin cambiar EF/MySQL ni API.
- Mantiene revision A5 independiente y relectura DAG antes de M03; ninguna migracion, DDL, servicio, QA/main/PROD, dato real o certificacion MySQL derivada de esta preparacion.

## 2026-11-16 - V17 C12-01.M02.02 operaciones y DTO documentales de topologia [Javier :20]
- Define ocho operaciones OP01-OP08, doce DTO D01-D12, doce errores ER01-ER12, dieciseis validaciones V01-V16 y doce pares P/N futuros sin crear endpoints.
- Conserva E01-E16/I01-I20, seguridad tenant/JWT/RBAC, red privada, referencias de secretos y gates G01-G16; no confunde contrato con prueba real ni permiso Hostinger/DNS/PROD.
- Riesgo R0 documental: runtime y proveedores NO_APLICA; requiere Scope Lock/V5 exact-SHA, readback institucional y A5 independiente de otra Task Javier.

## 2026-11-15 - V17 C09-03.M02.06 contrato de salida del Social Hub [Javier :10]
- Define dieciseis gates contractuales G01-G16, doce escenarios P/N FUTUROS_NO_EJECUTADOS (0/12 runtime), ocho pasos de rollback y dependencias A5 para campañas, consentimiento y opt-out.
- Exige aislamiento tenant A/B, JWT/RBAC y revocacion, CAS/idempotencia, cancelacion y verificacion de proveedor, sin crear envios, workers, tablas, credenciales ni migraciones.
- Conserva Scope Lock, procedencia V5 exact-SHA, candado V17, A5 independiente y salida documental R0; M03 y QA/main/PROD siguen sus gates reales y autorizaciones separadas.

## 2026-11-16 - V17 C12-01.M02.01 modelo conceptual de topologia futura [Javier :20]
- Define dieciseis entidades E01-E16, veinte invariantes I01-I20 y doce pares P/N futuros, con ownership, relaciones y aislamiento de ambientes/tenant.
- Solo preparacion documental R0: conserva Vercel, Render, Aiven y Cloudinary vigentes; ninguna provision Hostinger, DNS, MySQL, secreto, servicio ni cambio de permisos.
- Mantiene G01-G16 y rollback de M01.06; las pruebas runtime son FUTUROS_NO_EJECUTADOS y A5 exige otra Task Javier.

## 2026-11-15 - V17 C13-01.M01.06 bloqueo y liberacion documental privacidad/consentimiento [Javier :00]
- Define dieciseis gates G01-G16, ocho pasos de rollback futuro y doce pares POS/NEG FUTUROS_NO_EJECUTADOS (0/12 runtime) para privacidad, cookies, terminos, consentimiento y derechos.
- Separa textos versionados de cualquier certificacion legal o actividad de tracker, proveedor, JWT/tenant, retencion, supresion o base real; ninguna jurisdiccion ni plazo se infiere.
- Exige Scope Lock/V5 exact-SHA y A5 independiente para releer DAG M01-M02; sin politica publicada, QA/main/PROD, nuevos servicios ni datos reales.

## 2026-11-15 - V17 C11-01.M02.05 eventos idempotentes y correlacion del modelo relacional [Javier :10]
- Define doce eventos conceptuales EV01-EV12, doce invariantes de idempotencia/correlacion I01-I12 y doce pares P/N FUTUROS_NO_EJECUTADOS (0/12 runtime) para el futuro modelo relacional C11-01.
- Exige tenant A/B, membresia/roles vigentes, revocacion, digest canonico por actor y operacion, commit atomico y compensacion segura; no crea mensajes, outbox fisico, workers, API, migraciones ni broker.
- Conserva Scope Lock y procedencia V5 causal, revision A5 independiente y rollback documental; no implica autorizacion QA/main/PROD ni pruebas MySQL/HTTP ejecutadas.
- Reparacion documental de etiqueta EV09 para respetar el Scope Lock: se utiliza MovimientoInventarioConfirmado en lugar de un token de identidad retirado; sin desactivar validadores, requiere nuevo CI exact-SHA exitoso.

## 2026-11-15 - V17 C11-01.M02.04 configuracion tipada y flags modelo relacional [Javier :00]
- Define doce opciones conceptuales F01-F12, doce reglas de rechazo P01-P12 y doce pares POS/NEG FUTUROS_NO_EJECUTADOS (0/12 runtime) para seguridad, ownership y compatibilidad.
- Preserva tenant A/B, UsuarioEmpresa revocado, ownership legacy NULL en cuarentena, soporte read-only, transacciones y auditoria sin PII; no agrega configuracion real, EF, tablas, migraciones, API, secretos ni proveedores.
- Requiere Scope Lock/V5 exact-SHA y A5 independiente; QA/main/PROD y cambios productivos siguen NO_GO sin autorizacion adicional.

## 2026-11-15 - V17 C11-01.M02.03 contrato relacional de persistencia y migraciones [Javier :10]
- Especifica doce claves e indices conceptuales K01-K12, ownership por tenant, cautela legacy NULL y criterios de verificacion con AppDbContext, EXPLAIN y provider Oracle EF Core MySQL 8.4.
- Define ocho etapas futuras de migracion reversible M01-M08, seis fixtures sinteticos y doce pares positivos/negativos FUTUROS_NO_EJECUTADOS (0/12 runtime); no crea tablas, DDL, EF migrations ni datos.
- Delimita riesgos de FK cruzada, RBAC/revocacion, concurrencia e idempotencia, checks exact-SHA Scope Lock/V5, A5 independiente y rollback documental; no autoriza QA/main/PROD.

## 2026-11-15 - V17 C09-03.M02.05 eventos e idempotencia Social Hub [Javier :50]
- Define doce eventos propuestos, doce invariantes de correlacion e idempotencia y doce casos P/N FUTUROS_NO_EJECUTADOS (0/12 runtime), con tenant A/B, consentimiento, opt-out y revocacion.
- Diseña control de replay/CAS, limites UTC, fallos de proveedor y reconciliacion de efectos ambiguos sin crear jobs, tablas, APIs, brokers, envios ni credenciales.
- Exige Scope Lock y V5 causal, A5 independiente y alcance dev exclusivamente; documentacion R0 sin certificacion MySQL/HTTP/browser/produccion.

## 2026-11-15 - V17 C12-01.M01.06 bloqueos, rollback y liberacion documental [Javier :00]
- Define dieciseis gates futuros G01-G16, ocho pasos de reversion no ejecutados y doce pares POS/NEG FUTUROS_NO_EJECUTADOS (0/12 runtime) para la topologia objetivo de C12-01.
- Conserva Vercel/Render/Aiven/Cloudinary como entorno vigente y trata Hostinger/Cloudflare solo como alternativas futuras; no crea VPS, red, DB, worker, DNS, secretos, gastos ni nuevos permisos.
- Exige LISTO_DEV con A0-A4, Scope Lock y V5 exact-SHA, A5 independiente y DAG/COLA recalculados antes de cualquier M02; QA/main/PROD y cutover siguen NO_GO sin autorizacion adicional.

## 2026-11-15 - V17 C09-03.M02.03 persistencia conceptual Social Hub [Javier :50]
- Modela claves/ownership de E01-E16, doce indices y un plan de migracion reversible FUTURO para campañas, consentimiento, segmentos, calendario, opt-out e idempotencia.
- Especifica doce fixtures y pares P/N FUTUROS_NO_EJECUTADOS (0/12 runtime); no crea tablas, migraciones EF, SQL, APIs, proveedores, schedulers ni secretos.
- Requiere Scope Lock, procedencia V5 y A5 independiente; preserva aislamiento A/B, RBAC y revocacion sin habilitar QA/main/PROD.

## 2026-11-15 - V17 C05-01.M03.06 reparacion QA P2 de correlacion ordinal [Javier :50]
- Valida la correlacion ordinal exacta de cada elemento de respuesta del lector sintetico, rechazando ordinals forjados, negativos, duplicados, invertidos o fuera de rango.
- Agrega pruebas xUnit adversariales y verifica ausencia de registros exitosos al detectar respuestas falsas, sin conceder autoridad de tema, tenant, CSS, JWT o MySQL.
- Conserva A5_FAIL P2 original; requiere CI causal y nueva revision independiente antes de cerrar el defecto, sin promocion a QA/main/PROD.

## 2026-11-15 - V17 C09-03.M02.04 configuracion tipada y feature flags seguros Social Hub [Javier :40]
- Define doce flags conceptuales por plataforma/empresa/canal, doce restricciones fail-closed y doce pares de pruebas FUTUROS_NO_EJECUTADOS (0/12 runtime); R0 Markdown, no valores configurados en produccion.
- Mantiene denegacion por defecto, tenant A/B, consentimiento, opt-out, roles revocados, idempotencia y proveedor allowlist sin crear endpoints, schedulers, credenciales, workers o envios reales.
- Exige Scope Lock/V5 por SHA y A5 independiente; .NET/MySQL/HTTP/Chromium NO_APLICA a este diff; no habilita QA/main/PROD.

## 2026-11-15 - V17 C09-03.M02.02 contratos API DTO y errores Social Hub [Javier :40]
- Define doce operaciones conceptuales, doce DTO, dieciseis errores tipados y doce pares positivos/negativos FUTUROS_NO_EJECUTADOS (0/12 runtime) para campanas, consentimiento y opt-out.
- Especifica identidad de empresa del backend, RBAC/revocacion, aislamiento A/B, clave de idempotencia con digest separado, versionado, control de errores y rollback; R0 Markdown sin rutas, proveedores, envios ni tablas reales.
- Mantiene controles Scope Lock y procedencia V5 exact-SHA, candado V17 y A5 independiente; .NET/MySQL/HTTP/Chromium NO_APLICA a este diff; sin QA/main/PROD ni secretos.

## 2026-11-15 - V17 C09-03.M02.01 entidades, ownership y contratos de Social Hub [Javier :20]
- Documenta 16 entidades lógicas con relaciones/empresa, 18 invariantes fail-closed y diez pares P/N FUTUROS_NO_EJECUTADOS para campañas, consentimiento por canal, opt-out, calendario, idempotencia y métricas agregadas.
- R0 Markdown sin código ni servicios nuevos; bloquea cross-tenant A/B, JWT/RBAC asumidos, envíos sin consentimiento y proveedor no autorizado como requisitos de futuras implementaciones.
- Mantiene V17 candado, Scope Lock/V5 exact-SHA y A5 de otra Task Javier; no aplica pruebas HTTP/MySQL/Angular a este diff ni autoriza QA/main/PROD.

## 2026-11-15 - V17 C11-01.M02.02 contratos API DTO errores y compatibilidad relacional [Javier :30]
- Define ocho operaciones conceptuales y doce DTO de lectura, mutacion, membresia, ownership, concurrencia y esquema relacional, sin crear rutas ni codigo .NET.
- Especifica dieciseis errores futuros y doce pares P/N FUTUROS_NO_EJECUTADOS (0/12 runtime), con aislamiento A/B, RBAC, legacy NULL y transacciones all-or-nothing.
- R0 documental, solo este Markdown y CHANGELOG: exige Scope Lock/V5 completed success del SHA, A5 independiente, sin MySQL, JWT, Angular, HTTP, QA/main/PROD o datos reales.

## 2026-11-15 - V17 C11-01.M02.01 entidades, ownership y contratos relacionales R0 [Javier :30]
- Define contrato conceptual de 16 entidades existentes con ownership empresa/sucursal/usuario y relaciones candidatas sin declarar tablas, FK ni indices fisicos como certificados.
- Registra 16 invariantes de aislamiento tenant A/B, legacy NULL, RBAC, integridad comercial, idempotencia, auditoria y rollback; especifica 12 pares positivos/negativos FUTUROS_NO_EJECUTADOS (0/12 runtime).
- Entrega solo Markdown documental con Scope Lock y procedencia V5 exact-SHA, A5 pendiente de otra Task Javier, sin cambios EF/MySQL/API/Angular, sin QA/main/PROD ni datos reales.

## 2026-11-15 - V17 reparación P2 C14-02.M04.02 excepción síncrona de sesión [Javier :20]
- Corrige estado cargando persistente ante excepción síncrona de obtener() mediante RxJS defer, sin conceder permisos ni exponer diagnósticos.
- Añade dos negativos Vitest para error genérico y 403 síncronos; preserva la guía privada, 12 Vitest previos y seis Chromium; CI y A5 independiente requeridos.
- Riesgo R2, reparación de A5_FAIL histórico, sin JWT HTTP/MySQL reales ni QA/main/PROD; rollback por git revert.

## 2026-11-15 - V17 C11-01.M01.06 bloqueo, rollback y salida documental modelo relacional [Javier :10]
- Define dieciseis puertas de bloqueo para claves PK/FK, índices, ownership empresa/sucursal, legado nullable, migraciones reversibles e idempotencia, sin cambiar esquema, datos, EF ni servicios.
- Documenta doce pares positivos/negativos FUTUROS_NO_EJECUTADOS y ocho pasos de rollback, manteniendo R01-R16 como riesgos hipotéticos, S01-S10 sin verificar y D01-D10 pendientes.
- R0 documental en dev con A0-A4 y Scope Lock/V5 exact SHA; A5 exclusivamente por otra Task Javier, sin QA/main/PROD, datos reales, migraciones ni autorización productiva.

## 2026-11-15 - V17 C09-03.M01.06 gates, rollback y salida documental Social Hub [Javier :10]
- Define puertas de bloqueo para preparar Social Hub/campanas: consentimiento por canal/finalidad, opt-out y revocacion, tenant A/B, RBAC backend, idempotencia, auditoria, cuotas y privacidad sin activar publicaciones ni proveedores.
- Documenta doce escenarios positivos/negativos FUTUROS_NO_EJECUTADOS, decisiones D01-D08 pendientes, rollback en ocho pasos y dependencia de A0-A5, DAG dinamico y candado V17.
- R0 solo Markdown y CHANGELOG; sin .NET, Angular, MySQL, JWT real, integraciones ni QA/main/PROD. Scope Lock y procedencia V5 exact-SHA requeridos; A5 solo por otra Task Javier.

## 2026-11-15 - V17 C05-01.M03.06 auditoria segura sintetica de temas [Javier :50]
- Agrega servicio Infrastructure .NET 10 de auditoría estructurada, acotada e idempotente sobre el worker de previsualización M03.05; captura entradas, rechaza cruces de tenant, revisión, sesión, SHA, duplicados y concurrencia.
- Incorpora 11 pruebas xUnit adversariales de resultado nominal, denegaciones, errores sin filtración, respuestas forjadas, sink fallido, capacidad y cancelación; CI real GitHub-hosted exact-SHA obligatoria, sin atribuir PASS anticipado.
- Riesgo R2: logs con solo código y recuento, nunca identidades, CSS, claves ni secretos; no aplica tema, no integra JWT/MySQL/HTTP real ni autoriza QA/main/PROD, A5 de otra Task, rollback git revert.

## 2026-11-15 - V17 C14-02.M04.03 presentación local acotada de amenazas [Javier :20]
- Introduce dos modos visuales locales predefinidos y un selector accesible en la guía privada, sin cambio de JWT/RBAC, tenant, sesión, rutas ni políticas; rechaza valores arbitrarios.
- Añade tres casos Vitest y un Chromium sintético sobre alternancia, revocación, error, tokens y ausencia de inyección; requiere CI causal real, Scope Lock y V5 para LISTO.
- Riesgo R2; no hay pentest, MySQL, proveedores o HTTP JWT reales certificados; A5 independiente y rollback git revert, sin QA/main/PROD.

## 2026-11-15 - V17 C14-02.M04.02 estados de revisión de amenazas [Javier :20]
- Extiende guía Angular privada con estados de carga, vacío explícito de feed, error genérico, éxito de revalidación y denegación; solo GET /plataforma/sesion sin invocar mitigaciones ni pentest.
- Añade nueve Vitest de autorización/revocación/403/503/single-flight y cinco Chromium sintéticos; CI GitHub-hosted es obligatoria y no se atribuye PASS hasta check completed/success exact-SHA.
- Riesgo R2; guard backend como autoridad, sin MySQL o pentest real, A5 independiente y rollback git revert; no QA/main/PROD.

## 2026-11-15 - V17 C03-04.M04.02 corrección de regresión de prueba de accesibilidad [Javier :20]
- La evolución segura de la guía incorporó un único botón de GET /plataforma/sesion, por lo que la prueba heredada M04.05 que exigía cero botones dejó de expresar el contrato vigente. No se desactiva el workflow ni se oculta el fallo original.
- Corrige solo la aserción: prohíbe formularios e inputs, exige un único botón type=button con texto de revalidación; mantiene casos de foco, teclado, anónimo, empresa y revocación. Requiere 4/4 Vitest y 4/4 Chromium reales además de CI propia M04.02 sobre SHA reparado antes de LISTO.
- Riesgo R2, rollback git revert, A5 independiente; no verifica JWT HTTP real, MySQL ni restauración productiva.

## 2026-11-15 - V17 C03-04.M04.02 estados seguros de incidentes [Javier :20]
- Extiende superficie Angular privada con carga, vacío, error genérico, éxito de sesión y falta de permisos; solo GET /plataforma/sesion, sin feed ni restauraciones simuladas como reales.
- Propone nueve Vitest y cinco Chromium sintéticos, lint y build en GitHub-hosted; exigir SUCCESS exact-SHA antes de LISTO.
- Riesgo R2: conserva guard y sesión backend como autoridad, UI fail-closed y A5 independiente; no certifica MySQL, tenant real, JWT real ni restauración. Rollback git revert normal.

## 2026-11-15 — V17 C15-02.M03.06 auditoría segura de arquitectura [Javier :50]
- Agrega fachada Infrastructure .NET 10 de auditoría acotada y monohilo, lectura sintética de C15-02.M03.05, rechazo de doble selección y scopes de tenant/SHA inconsistentes, y sink estructurado sin secretos.
- Incorpora 13 pruebas xUnit positivas, negativas, revocación, tenant B, fallo de lector/sink, límite, concurrencia y cancelación; comprobación real GitHub-hosted A0-A4 pertinente al SHA.
- Riesgo R2. No ejecuta MySQL, migración, backup/restore, DNS, JWT ni proveedores reales; A5 independiente por Task Javier distinta, rollback git revert normal.

## 2026-11-15 — V17 C05-01.M03.05 worker acotado de design tokens [Javier :50]
- Agrega worker Infrastructure .NET 10 de previsualización secuencial de hasta 12 intenciones de tema, con comprobación fail-closed de tenant A/B, actor, SHA, revisión, sesión y claves ordinales.
- Incorpora 13 pruebas xUnit de casos positivos, negativos, fronteras, revocación, roles, valores CSS inseguros y cancelación; CI GitHub-hosted pertinente A0-A4.
- Riesgo R2. No aplica CSS ni cambia temas, autoriza JWT, consulta MySQL o declara accesibilidad real; A5 de otra Task Javier, rollback git revert normal.

## 2026-11-15 — V17 C02-01.M03.06 auditoría segura de selección de tenant [Javier :50]
- Añade fachada Infrastructure .NET 10 de auditoría de solo previsualización con logs estructurados sin PII, rechazo de doble selección y concurrencia, y denegación si falla el sink de auditoría.
- Incluye 15 pruebas xUnit de elección 1/N, revocación, tenant ajeno, SHA/actor, role hints falsos, repetición, mezcla, cancelación, fallos y solapamiento; validación GitHub-hosted A0-A4 exigida.
- Riesgo R2. No emite JWT, cambia empresa, persiste auditoría, usa MySQL ni habilita producción. A5 independiente por otra Task Javier; rollback git revert normal.

## 2026-11-15 — V17 C10-04.M04.05 correccion CSS alta visibilidad tras Chromium FAIL [Javier :40]
- En SHA 0d021aa8, CI dirigida verificó 4/5 Chromium aprobadas y 1 falla real: outline calculado 3px en prefers-contrast:more contra criterio >=4px. Conserva defecto/resultado, nunca PASS.
- Refuerza solo el outline del elemento enfocado en modo alto contraste con 4px important y comprueba matchMedia verdadero en Playwright; no altera permiso, datos ni rutas. CI nuevo SHA y A5 independiente pendientes.
- R2 UI, SKREF-10 Angular pin 6808966993bf2b2d455d9b6245e960a59a01b9a9; revert normal, sin QA/main/PROD ni certificación JWT/MySQL.

## 2026-11-15 — V17 C10-04.M04.05 reparacion P2 de evidencia teclado y contraste [Javier :40]
- Corrige A5_FAIL P2: Playwright exige foco alcanzado por Tab sin focus() programatico, 4px en prefers-contrast:more y contraste del outline >=3:1 con CanvasText/Canvas; detecta defectos que cinco Chromium verdes anteriores no detectaban.
- Cambio de foco exclusivamente en contraste alto, sin alterar auth, tenant, rutas, API o datos. R2 UI, fuente SKREF-10 angular/angular@6808966993bf2b2d455d9b6245e960a59a01b9a9; CI causal y posterior A5 independiente obligatorias, no se declara PASS anticipado.
- Mantener QA_DEFECT_FOUND/A5_FAIL hasta prueba dirigida y nuevo A5 de otra Task Javier. Sin JWT/MySQL/productivo; rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V17 C03-04.M04.05 teclado, responsive y contraste de incidentes [Javier :20]
- Mejora acceso por teclado a la guía privada de incidentes mediante skip link al título enfocable, retorno de 44px y estilos explícitos para forced-colors, contraste alto y textos largos, sin acciones de restauración.
- Agrega cuatro Vitest y cuatro Playwright Chromium sintéticos de anónimo, empresa/403, revocación, foco, 320–1440px y contraste forzado; workflow causal exact-SHA Angular lint/build, Scope Lock y procedencia V5.
- Riesgo R2, fuente SKREF-10 angular/angular pin 6808966993bf2b2d455d9b6245e960a59a01b9a9; no acredita JWT/API/MySQL/tenant backend ni métricas productivas. A5 otra Task Javier; rollback git revert, sin QA/main/PROD.
- Corrige regresión P2 real detectada por Playwright: el salto de teclado con ancla relativa navegaba a /#incident-title por el <base href="/"> de Angular. La reparación 63f17df4 usa preventDefault y viewChild para enfocar el título sin abandonar la ruta; CI original 3/4 Chromium FAIL y SHA corregido 4/4 Chromium + 4/4 Vitest PASS. Se conserva QA_DEFECT_FOUND y evidencia de reparación en BITACORA; nuevo V5 exact-SHA obligatorio y A5 independiente aún pendiente.

## 2026-11-15 — V17 C10-04.M04.04 reparación de evidencia CI P1 [Javier :10]
- Corrige la selección Vitest del workflow de interacción Angular para ejecutar las siete pruebas `continuidad-interacciones-v5.spec.ts`, antes omitidas por seleccionar cuatro pruebas heredadas de `continuidad-presentacion-v5.spec.ts`.
- Corrige el filtro de ruta del propio workflow M04.04; conserva lint, build, Chromium, Scope Lock, procedencia V5 y cancel-in-progress=false.
- Preserva el hallazgo A5_FAIL P1 de DEV05 y requiere nueva evidencia causal GitHub Actions y revisión independiente posterior; sin reclamar integración JWT, MySQL ni producción.

## 2026-11-15 — V17 C02-02.M04.04 navegación y mensajes seguros de placement [Javier :00]
- Robustecida revalidación Angular privada con defer ante excepción síncrona, estado fail-closed y botón single-flight; ruta de regreso y ausencia de formularios peligrosos preservadas.
- Añadidos tres Vitest dirigidos y Chromium sintético de navegación y 403 posterior, sin filtrar DSN ni autorizar migraciones; CI causal Angular/Chromium mediante workflows ya existentes.
- Riesgo R2: A0–A4 y checks exact-SHA obligatorios antes de LISTO; A5 por Task Javier distinta. Sin HTTP JWT real, MySQL, proveedores, QA/main/PROD; rollback git revert.

## 2026-11-15 — V17 C15-02.M03.05 worker acotado de revisión de arquitectura [Javier :50]
- Agrega worker Infrastructure .NET 10 de previsualización en memoria de hasta 12 probes; valida SHA, tenant, claves ordinales, no mutación, conjunto coherente y readback aislado antes de devolver únicamente evidencia requerida.
- Incorpora once pruebas xUnit de casos válidos y rechazo por tenant B, SHA stale, autoridad revocada, duplicados, batch mixto, comandos externos, error de proveedor de lectura, límites y cancelación; gates backend/security/Scope Lock/V5 requeridos.
- No certifica ejecución real de backups, MySQL, DNS, secretos, R2, Render, Vercel, HTTP/JWT ni permisos de producción. A5 otro Javier; rollback git revert normal, QA/main/PROD intactos.

## 2026-11-15 — V17 C11-04.M04.02 estados y revocación de observabilidad privada [Javier :00]
- Añade estados Angular tipados vacio/cargando/exito/sin_permiso/error y revalidación de autoridad server-side de solo lectura, ocultación inmediata y prevención de doble solicitud.
- Amplía Vitest positivo/negativo/frontera y Playwright Chromium sintético 200→503/403; conserva tests M04.01 y workflow causal dirigido, sin exporter, datos privados ni métricas en vivo.
- Riesgo R2: Scope Lock y V5 exact-SHA, Angular lint/build, Vitest y Chromium requeridos; A5 independiente. Sin QA/main/PROD, MySQL, JWT HTTP real ni proveedores; rollback git revert.

## 2026-11-15 — V17 C02-01.M03.05 worker acotado de revalidación de preferencia tenant [Javier :50]
- Añade worker Infrastructure .NET 10 real y secuencial para previsualizar hasta 12 solicitudes sintéticas, con validación atómica de keys, actor, revisión, SHA, selección coherente y hints no confiables.
- Añade once pruebas xUnit positivas y negativas: 1/N empresas, tenant ajeno, preferencia revocada, identidad/SHA obsoletos, claves repetidas, mezcla de selección, cancelación y límites; CI GitHub-hosted obligatoria.
- Riesgo R2, sin JWT emitido, permisos reales, MySQL, cambios de sesión, cola durable, browser, proveedores ni QA/main/PROD. A0-A4 causales y A5 independiente Javier; rollback git revert.

## 2026-11-15 — V17 C10-04.M04.05 accesibilidad y pruebas Chromium de continuidad privada [Javier :40]
- Reforzado aria-labelledby en lista de controles, wrap seguro para 320px y foco de alto contraste, manteniendo guard de plataforma, sesión y estados fail-closed sin nuevas operaciones.
- Añade cinco Playwright reales sobre sesión sintética: anónimo, 403, 320/768/1280, foco/labels/contraste fuerte y error 503 sin filtración; workflow GitHub-hosted Node24/npm11 Angular lint/build/Chromium, exact SHA.
- Riesgo R2: sin JWT/HTTP/MySQL/tenant A/B real ni restauración; exigir Scope Lock, V5 y CI dirigida antes de LISTO, A5 por otra Task Javier. Rollback git revert, QA/main/PROD intactos.

## 2026-11-15 — V17 C15-03.M04.03 branding reactivo y densidad segura de auditoría tenancy [Javier :40]
- Presentación Angular tipada con marca solqaryn/institucional y densidad normal/compacta, Signal reactivo y normalización fail-closed; no concede permisos, no aplica tema empresarial ni CSS arbitrario.
- Añade Vitest con casos positivos, adversariales, frontera y revocación, más Chromium para render DOM seguro; conserva el guard de plataforma y pruebas de M04.01/M04.02.
- Riesgo R2: exigir CI GitHub-hosted real para Angular/Chromium, Scope Lock y V5 del SHA; A5 independiente. Sin JWT/MySQL/HTTP real, pentest, datos, QA/main/PROD. Rollback git revert.

## 2026-11-15 — V17 reparación causal ERP: dos rutas privadas de plataforma guardadas [Javier :10]
- Auditoría `priority4_quality_audit.py` reconoce exclusivamente `plataforma/revision-amenazas-v5` y `plataforma/observabilidad-v5` existentes con `canActivate: [plataformaGuard]`, guard autenticado y sesión de plataforma validada por servidor.
- Se conserva el rechazo `UNGUARDED` de cualquier otra ruta no permitida, el contrato `permisoGuard` y todas las negativas; sin comodines, cambios de guards, permisos, endpoints o despliegue. La reparación del CI ERP solo se acredita tras check SUCCESS exact-SHA.
- Reparación de baseline de validaciones propias V17; no implica A5, QA/main/PROD, MySQL ni autorización adicional.

## 2026-11-15 — V17 reparación causal P0-007.A del catálogo Angular exact-HEAD [Javier :10]
- Reconciliados tres componentes privados ya publicados por otras Tasks Javier: observabilidad, respuesta a incidentes y revisión de amenazas, preservando autoría, guards y código originales.
- Manifiesto y validador fail-closed ajustados de 145 a 148 rutas (140 features, 8 shared), con negativas de omisiones, entradas ficticias y exclusiones intactas. No se desactivan controles ni cambia el runtime.
- Reparación de CI compartida para VALIDANDO C02-02.M04.03; sin certificar P0/ERP hasta SUCCESS en GitHub Actions. No implica A5, QA/main/PROD ni permisos nuevos.

## 2026-11-15 — V17 C14-04.M03.06 auditoría estructurada fail-closed de readiness [Javier :10]
- Integra sink .NET 10 de ILogger sobre siete resultados permitidos de revisión DEV: verifica fuente backend, tenant A/B, membresía, rol, revisor independiente, SHA y audit fence, sin registrar PII/JWT ni credenciales.
- Añade 16 xUnit positivos, negativos y error; exige CI GitHub-hosted exact-SHA Release, seguridad, autorización, Scope Lock y V5. No instala worker, proveedor, MySQL ni auditoría durable.
- Riesgo R2 con A5 de otra Task Javier pendiente. Sin permisos de despliegue, QA/main/PROD; rollback normal git revert.

## 2026-11-15 — V17 C11-04.M04.01 observabilidad privada de solo lectura [Javier :20]
- Agrega ruta lazy protegida con plataformaGuard y guía privada para logs, métricas/costos, trazas y alertas sin exporter, secretos, APIs ni mediciones conectadas.
- Incluye cinco Vitest positivos/negativos/revocación y tres Playwright Chromium sintéticos; exige lint/build, Scope Lock y procedencia V5 exact-SHA GitHub-hosted antes de LISTO.
- Riesgo R2: observabilidad, SLO, costos, JWT y tenant reales NO_MEDIDO; MySQL/EF/proveedor NO_APLICA al diff frontend; A5 de otro Task Javier, rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V17 reparación causal ERP: ruta privada de incidentes con guard exacto [Javier :10]
- Repara falso positivo FRONTEND_ROUTE_SECURITY_CONTRACT agregando exclusivamente `plataforma/respuesta-incidentes-v5` al allowlist cerrado de control-plane, cuya ruta usa `canActivate: [plataformaGuard]`.
- Conserva la comprobación obligatoria del guard real y sesión backend, sin comodines, bypass, desactivar validadores ni editar la ruta de su autor. Negativo: ruta sin guard continúa rechazándose.
- Baseline de CI fallido durante C02-02.M04.03; requiere repetir ERP, Scope Lock, V5 y pruebas pertinentes del SHA reparado con equivalencia de blobs del scope. Sin despliegues, QA/main/PROD.

## 2026-11-15 — V17 C14-02.M04.01 revisión de amenazas privada [Javier :20]
- Incorpora ruta Angular lazy protegida con plataformaGuard y guía de cuatro áreas críticas: BOLA/IDOR, suplantación, archivos/jobs/caché y host/backup, sin ejecutar operaciones ni consultar datos reales.
- Añade cinco Vitest con denegaciones y revocación y tres escenarios Playwright/Chromium sintéticos; GitHub-hosted valida lint/build, Scope Lock y procedencia V5 antes de cualquier cierre.
- Riesgo R2 UI: JWT, tenant A/B, pentest, restore/MySQL y proveedores reales NO_MEDIDO/NO_APLICA al diff; A5 de Task Javier distinta, rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V17 C02-02.M04.03 presentación reactiva y segura de placement [Javier :10]
- Incorpora token Angular Signal con marca institucional/SOLQARYN y densidad normal/compacta estrictamente permitidas, sin alterar permisos, tenant, DSN ni routing del backend.
- Añade cinco Vitest de variantes reactivas y negativos HTML/CSS, campos extra y revocación; workflow dirigido Angular/Chromium con API sintética y mantenimiento de guard privado.
- Riesgo R2; CI exact-SHA, Scope Lock y procedencia V5 obligatorios antes de LISTO. A5 de otra Task Javier pendiente. Sin DB, migraciones, QA/main/PROD; rollback git revert normal.

## 2026-11-15 — V17 C03-04.M04.01 respuesta privada a incidentes [Javier :20]
- Agrega una ruta lazy protegida con plataformaGuard y una vista solo lectura para contención, integridad, ensayo de respaldo y rollback sin operar restauración, revocación o proveedor.
- Implementa cinco Vitest adversariales (anónimo, empresa, grant, revocación, guard) y tres Chromium sintéticos; CI GitHub-hosted valida lint, build, Scope Lock y procedencia V5 por SHA antes del cierre.
- Riesgo R2: pruebas simuladas no acreditan JWT/MySQL/tenant A-B/backup real, RTO-RPO NO_MEDIDO; A5 corresponde a otro Task Javier. Rollback git revert normal, sin QA/main/PROD.

## 2026-11-15 — V17 C10-04.M04.04 navegación y doble acción privada [Javier :10]
- Corrige falla síncrona del proveedor de sesión mediante defer RxJS, manteniendo la UI fail-closed y el botón recuperable, sin alterar JWT, tenant, backend ni rutas de autoridad.
- Agrega siete pruebas Vitest de navegación interna, ausencia de formularios, single-flight, revocación, errores 403/503 y excepción síncrona; workflow propio con npm, build/lint y Playwright Chromium sintético.
- Riesgo R2, A0–A4 causales requeridos en GitHub Actions y Scope Lock/V5; A5 independiente Javier pendiente. No habilita restauración, workers, DB, QA/main/PROD. Rollback normal git revert.

## 2026-11-15 — V17 C05-04.M03.06 auditoria privada de ligereza UI [Javier :00]
- Añade sink .NET 10 con ILogger y codigo permitido, verifica identidad backend, tenant A/B, membresia, RBAC, SHA y audit fence en fuente confiable; no registra PII, excepciones, tenant ni JWT.
- Incluye 14 xUnit positivos/negativos/error; requiere comprobaciones GitHub Actions exact-SHA, Scope Lock y V5 antes de LISTO; A5 de otra Task Javier.
- Alcance R2 in-process no durable: sin Core Web Vitals browser, Angular, MySQL, HTTP, telemetria externa, scheduler, servidor productivo ni permiso para QA/main/PROD.

## 2026-11-15 — V17 C11-04.M03.06 auditoria estructurada de observabilidad segura [Javier :00]
- Implementa un sink .NET 10 de auditoria in-process con ILogger/EventId fijo, fuente de autoridad backend verificable, tenant/RBAC/SHA fail-closed y salida sin datos sensibles.
- Incorpora 14 xUnit positivos/negativos/frontera/error; Scope Lock, backend Release y seguridad deben aprobar en GitHub Actions del SHA causal. Sin exporter, MySQL, JWT HTTP, almacenamiento durable, scheduler, datos productivos ni permisos de despliegue.
- A5 corresponde a otra Task Javier; riesgo R2 y rollback git revert normal, sin tocar QA/main/PROD.

- CI_REPAIR C11-04.M03.06: corrige CS0542 al renombrar el logger simulado de las pruebas xUnit; conserva los 14 casos y la seguridad. Revalidar GitHub Actions exact-SHA antes de LISTO, A5 independiente pendiente.

## 2026-11-15 — V17 C08-04.M03.04 adaptador aislado de módulos sectoriales [Javier :20]
- Implementa en Infrastructure .NET 10 una fuente sintética por tenant y módulo exactos, revisión y SHA; fuerza los indicadores de lectura persistente y CAS/auditoría a false, sin conceder activaciones ni permisos reales.
- Añade nueve xUnit adversariales: tenant A/B, comparador ordinal, versiones y SHA, flags falsificados, constructor inválido y cancelación. No agrega endpoints/EF/MySQL/proveedores.
- Riesgo R2: CI backend Release, seguridad, autorización, Scope Lock y procedencia V5 por SHA exacto; A5 independiente de otra Task Javier. Rollback git revert normal; sin QA/main/PROD.

## 2026-11-15 — V17 C05-01.M03.04 adaptador aislado de design tokens, tenant y revision [Javier :50]
- Implementa Infrastructure .NET 10 de solo lectura sobre Application/Domain de design tokens, con captura inmutable del catálogo ordinal, actor/tenant, sesión/revisión y SHA; nunca crea provider MySQL, JWT, ruta, DI o cambio real de tema.
- Incorpora 15 pruebas xUnit adversariales R2 de tenant A/B, RBAC/revocación, SHA, CSS seguro, alteración posterior del allowlist, catálogos obsoletos, errores y cancelación; resultado positivo siempre exige un adaptador autenticado separado.
- Requiere CI causal Backend Release, seguridad, autorización, Scope Lock, V5, lectura institucional y A5 de Task Javier distinta; riesgo R2, rollback git revert normal, sin QA/main/PROD.

## 2026-11-15 — V17 C01-02.M03.04 snapshot aislado de capabilities tenant/rol [Javier :20]
- Implementa adaptador Infrastructure de captura sintética tenant/recurso con claves ordinales congeladas y flags de persistencia, RBAC y autorización forzados a false; nunca concede permisos ni accede MySQL/JWT real.
- Agrega 9 xUnit adversariales: tenant A/B, resource/ownership, colecciones mutables, comparer case-insensitive, capturas incompletas, cancelación y ausencia de promociones; pruebas reales pendientes GitHub-hosted.
- Riesgo R2: exigir backend Release, seguridad, authorization, Scope Lock y procedencia V5 exact SHA. Mantener VALIDANDO hasta CI real; A5 por otra Task Javier, rollback git revert convencional, no QA/main/PROD.

## 2026-11-15 — V17 C01-03.M03.04 adaptador aislado de snapshot de configuración dinámica [Javier :20]
- Implementa adaptador Infrastructure→Application de captura sintética read-only con tenant/clave ordinal, revisión y SHA exactos; no integra EF/MySQL ni concede escritura, CAS durable, JWT vivo o permisos de plataforma.
- Añade 9 pruebas xUnit de escenarios nominales y adversariales: tenant A/B, allowlist case-sensitive, colección mutable, versión/SHA stale, ownership, cancelación y no-promoción a escritura al componer el gate M03.03.
- Requiere CI .NET Release, seguridad, autorización, Scope Lock y procedencia V5 por SHA causal; A5 de otra Task Javier y rollback git revert convencional. No QA/main/PROD.

## 2026-11-15 — V17 C15-03.M04.02 estados de auditoría de tenancy con denegación segura [Javier :40]
- Añade cinco estados accesibles (vacío, cargando, éxito, denegado y error) a la ruta privada de auditoría existente, revalidación GET de sesión administrativa por servidor, revocación reactiva y prevención single-flight de acciones duplicadas.
- Siete nuevas pruebas Vitest positivas, negativas, fronteras y redacción de errores, más regresión Chromium con 503 sintético; no ejecuta pentest, consulta MySQL, modifica datos ni otorga privilegios desde navegador.
- Riesgo R2 UI/seguridad: exigir npm lint/build, Vitest y Chromium reales, Scope Lock y V5 del SHA exacto; A5 independiente. Ninguna certificación JWT/tenant backend real, QA/main/PROD. Rollback: git revert sin force push.

- CI_REPAIR causal C15-03.M04.02: Chromium job 133043534404 falló por aserción heredada de cero botones pese al nuevo botón único GET de revalidación; se conserva ausencia de formularios/mutaciones, se exige exactamente un botón accesible y se vuelve a ejecutar regresión real sin relajar guard ni autorización.

## 2026-11-15 — V17 C14-04.M03.05 worker acotado read-only de readiness DEV [Javier :00]
- Añade un processor .NET 10 de una invocación manual y lote máximo 16, con inbox backend confiable tipado, prevalidación íntegra de IDs, idempotencia intra-lote, SHA, ruta y entorno dev; sin programación, envío ni ack externo.
- Integra el adaptador de snapshots previamente autorizado con revisión tenant/RBAC/autor-revisor/CI para exigir revisión externa; 14 xUnit adversariales en proceso, no certificación de backend live, MySQL, producción ni exactly-once durable.
- A0–A4 por GitHub Actions del SHA y A5 por otra Task Javier; cambios fail-closed, Scope Lock STRICT; rollback git revert normal, sin QA/main/PROD.

## 2026-11-15 — V17 C02-02.M04.02 estados de placement con autorización fail-closed [Javier :40]
- La pantalla privada ya existente incorpora cinco estados accesibles (vacío, carga, éxito, sin permiso y error), revalidación de la sesión exclusivamente con el servidor, ocultación de modalidades durante solicitudes y revocación, y protección single-flight de doble acción.
- Añade siete pruebas Vitest positivas, negativas y de frontera, y regresión Chromium sintética de 503 con mensajes seguros; conserva los casos previos. Ninguna acción de placement, migración, conexión dedicada ni autorización backend se implementa en UI.
- Riesgo R2 por superficie privada y aislamiento; exigir frontend tipado/build, pruebas dirigidas Chromium, seguridad, Scope Lock y procedencia V5 sobre SHA publicado; A5 independiente. Sin QA/main/PROD, rollback por git revert normal.

## 2026-11-15 — V17 C13-04.M03.03 autoridad actual y tenancy fail-closed de auditoría visual [Javier :00]
- Introduce Infrastructure .NET 10 sobre Application/Domain con fuente confiable e independiente de la petición, validando identidad, tenant A/B, membresía/permiso/revocación, SHA ordinal, vigencia corta y allowlist de superficies server-side.
- Añade doce pruebas xUnit R2 de positivos/negativos, fronteras, replay de SHA, datos sensibles, fallo opaco y cancelación; no crea endpoint, DI real, MySQL, JWT HTTP ni medición WCAG.
- Requiere CI backend/seguridad/autorización, Scope Lock y V5 exact-SHA antes de LISTO_DEV y A5 por otra Task Javier; sin QA/main/PROD. Rollback mediante revert normal.

## 2026-11-15 — V17 reparación de baseline P0-007.A y rutas privadas de control plane [Javier :00]
- Sin cambiar autorización ni rutas, incorpora a inventario exacto dos componentes Angular privados ya publicados: auditoría de tenancy y revisión de placement; 145 archivos (137 features, 8 shared), preservando cortes históricos.
- Corrige el falso negativo ERP admitiendo únicamente dos rutas `plataforma/...` literales que ya exigen `plataformaGuard` y sesión administrativa validada por servidor; ningún comodín, auth bypass, nueva API ni integración productiva.
- Mantiene comparaciones fail-closed, pruebas negativas de inventario y checks CI requeridos; las pruebas sobre el nuevo SHA deben finalizar SUCCESS antes de atribuir A4. Rollback: revert normal con HEAD verificado.

## 2026-11-15 — V17 C02-02.M04.01 ruta privada shared/dedicated placement [Javier :20]
- Implementa Angular 22.2.1 componente lazy con guard de plataforma, modelo de responsabilidades Shared/Dedicated y checklist de validación/rollback, solo lectura sin DSN ni router/migraciones reales.
- Agrega 5 pruebas unitarias de grant/tenant empresarial/revocación y 3 Playwright Chromium sintéticos; exige lint, build, Scope Lock y V5 exact-SHA antes de LISTO.
- Riesgo R2 UI de infraestructura: no certifica MySQL, JWT, tenant A/B backend ni cutover, V17 lock exclusivo, A5 independiente, rollback git revert.

## 2026-11-15 — V17 C15-03.M04.04 verificación de navegación sin doble acción [Javier :20]
- Incorpora prueba funcional real Playwright Chromium sobre la ruta privada existente: anónimo y 403 sintéticos denegados, mensajes solo lectura/NO_MEDIDO, enlace único de regreso, visita repetida sin mutaciones a endpoints sensibles.
- No inventa formularios inexistentes ni afecta guard, API o servicios; exige npm lint/build, Chromium, Scope Lock y procedencia V5 exact-SHA antes de LISTO.
- Riesgo R2 UI privada: sesiones simuladas no certifican JWT, tenant A/B, DB ni pentest; candado V17 exclusivo, A5 independiente, rollback git revert.
- CI_REPAIR causal: job133035484583 evidenció 3/4 Playwright, aserción esperaba erróneamente 0 enlaces routerLink existentes mientras validaba regreso único. Ajustada a 1 enlace no operativo conservando guard/denegación; verificar nuevo SHA antes de LISTO.

## 2026-11-15 — V17 C11-04.M03.05 worker acotado de observabilidad [Javier :00]
- Implementa worker Infrastructure .NET 10 secuencial de revisión sintética, con puerto aislado hacia M03.04 y prevalidación de tenant, SHA, lote, cardinalidad y códigos sin acceso a telemetría real.
- Añade 13 xUnit R2 de positivo no autorizante, A/B, revocación implícita de origen, SHA, duplicados, 16/17, fallos y cancelación, más build Release, Seguridad multiempresa, Authorization, Scope Lock y V5 exact-SHA obligatorios.
- Entrega única V17 bajo candado 2000001710; sin HostedService, MySQL/EF, JWT HTTP, exporters, mediciones SLO/costos, QA/main/PROD; A5 de revisor Javier diferente.

## 2026-11-15 — V17 C15-03.M04.05 accesibilidad teclado y contraste [Javier :20]
- Añade enlace de salto protegido, foco visible de alto contraste, etiquetas para matriz de auditoría, mejoras responsive y navegación por teclado a la ruta privada existente sin otorgar permisos.
- Pruebas reales Chromium con sesiones sintéticas positiva, 403, anónimo, keyboard/focus, 320/768 px; requiere lint, build, Scope Lock y procedencia V5 exact SHA.
- Riesgo R2 UI: no certifica HTTP JWT, MySQL, tenants A/B ni pentest; candado V17 exclusivo, A5 independiente y rollback git revert.
- CI_REPAIR GitHub job133032969860: 3/4 Chromium PASS, assertion global detectó cinco controles del shell externo; el test ahora limita la ausencia de acciones al componente `.tenancy-audit`, sin esconder controles globales ni desactivar denegaciones. Revalidar en SHA nuevo.

## 2026-11-15 — V17 C15-01.M03.06 auditoría segura de cobertura de requisitos [Javier :00]
- Implementa sink Infrastructure .NET 10 no autorizante para la revisión acotada M03.05, con evento ILogger seguro y sin escritura institucional ni certificación global/A5.
- Incorpora 14 pruebas xUnit adversariales de tenant A/B, membresía/revocación, SHA, duplicados, 16/17 elementos, códigos inyectados, logger fallido y cancelación; exige Backend Release, seguridad, Authorization, Scope Lock y V5 exact-SHA.
- Una entrega R2 protegida por V17 lock 2000002179; no MySQL/EF, JWT HTTP, browser, proveedor, log durable ni QA/main/PROD; A5 independiente pendiente.

## 2026-11-15 — V17 C10-04.M04.03 branding privado y configuracion tipada [Javier :10]
- Agrega presentacion Angular reactiva mediante Signal y allowlist estricta de marca/densidad; valores arbitrarios y propiedades extra vuelven al modo seguro, sin usar configuracion como autoridad.
- Mantiene guard y revocacion de sesion privada, incluye Vitest negativo y regresion Chromium sobre API sintetica; exige CI GitHub-hosted exact-SHA.
- Cambio R2 UI individual bajo V17, Scope Lock/V5, A5 por otra Task Javier, rollback git revert; sin HTTP/JWT/MySQL/proveedores live ni QA/main/PROD.

## 2026-11-15 — V17 C15-03.M04.01 auditoría de tenancy privada Angular [Javier :20]
- Agrega ruta lazy protegida por plataformaGuard y guía estática de cuatro frentes de seguridad/aislamiento; la revocación de sesión oculta contenido, sin ejecutar pentest ni operaciones.
- Incorpora pruebas unitarias de guard/denegación/revocación y CI GitHub-hosted de Chromium con token y API sintéticos, además de lint y build Angular; no certifica backend JWT/MySQL/tenant real.
- Publicación individual V17 bajo Scope Lock y procedencia V5 exact SHA, A5 independiente requerida, rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V17 C10-04.M04.02 estados privados y permisos Angular [Javier :20]
- Refuerza estados carga/vacío/error/éxito/sin-permiso, oculta la guía ante revocación de sesión y evita declarar éxito con respuesta inválida o vacía; mantiene ruta guard y solo lectura.
- Amplía Vitest adversarial y agrega CI dirigida GitHub-hosted con lint, build Angular 22.2.1 y Chromium Playwright con API sintética, sin atribuir autorización HTTP/JWT/DB real.
- Entrega individual con candado V17, Scope Lock/V5 exact SHA, A5 de otra Task Javier y rollback por revert, sin QA/main/PROD.

## 2026-11-15 — V17 C02-02.M03.06 auditoría segura de cola de placement [Javier :50]
- Implementa sink Infrastructure .NET 10 de resultado estructurado y redactado para cola sintética M03.05; token, tenant, actor, SHA, DSN, idempotencia, PII y errores externos nunca salen en logs, ni se autoriza migración o acceso.
- Añade 13 xUnit para nominal no autorizante, denegaciones tenant A/B, membresía/revocación, SHA y correlación, código desconocido, cantidad inválida, contexto/constructor, cancelación y logger fallido.
- V15 R2 con Scope Lock STRICT, V17 lock 2000000343, CI Backend Release/seguridad/Authorization/Scope/V5 por SHA, A5 independiente; no MySQL/EF/HTTP/browser/worker duradero/QA/main/PROD.

## 2026-11-15 — V17 C04-03.M02.06 contrato terminal de metering [Javier :40]
- Cierra el documento de dependencias M02 con dieciseis puertas verificables, doce pares de pruebas futuras y ocho rutas de reversion, sin implementar API, MySQL, eventos o cobros.
- Confirma predecesoras M01 y M02.01-05 LISTO+A5; separa G/O dinamicos, idempotencia sin doble ledger y decisiones comerciales D01-D08 aun pendientes.
- R0 documental con V17 lock 2000000661, Scope Lock y procedencia exact-SHA, A5 de otra Task Javier y ninguna autorizacion QA/main/PROD.

## 2026-11-15 — V17 C03-04.M03.06 auditoría segura de incidentes y continuidad [Javier :50]
- Implementa sink Infrastructure .NET 10 de logging estructurado no durable para preview de continuidad: evento fijo, códigos seguros y correlación sólo bajo contexto backend verificado; no registra claves, tenant, actor, SHA, PII, JWT o excepciones.
- Añade 13 métodos xUnit de positivo, negaciones tenant A/B/revocación, SHA stale, lote, duplicidad no confiable, errores y cancelación; incluye composición in-memory real con worker Application M03.05 y cero permisos concedidos.
- Riesgo V15 R2, Scope Lock STRICT y candado V17 por ID; build/seguridad/autorización/Scope Lock/V5 requeridos en CI causal, A5 de otra Task Javier, no EF/MySQL/HTTP/backup real/QA/main/PROD.

## 2026-11-15 — V17 C05-04.M03.05 cola aislada de revision acotada de ligereza UI [Javier :40]
- Implementa en Infrastructure .NET10 un procesador en memoria acotado y serializado, que reusa preflight Application/Domain, sin HostedService, DI, cache real, HTTP, DB ni medicion CWV.
- Incorpora doce xUnit adversariales con tenant A/B, replay/conflicto, limites, SHA, cancelacion, idempotencia fail-closed y ausencia de grants.
- R2: Scope Lock, CI backend/seguridad/autorizacion y procedencia V5 exact-SHA obligatorios antes de LISTO; A5 independiente y no QA/main/PROD.

## 2026-11-15 — V17 C04-03.M02.04 configuración tipada conceptual de metering [Javier :20]
- Define seis ámbitos de autoridad y dieciséis opciones tipadas con defaults seguros, validaciones fail-closed, precedencia por tenant, revocación y rollback; documento R0, no runtime.
- Registra doce invariantes y diez pares positivos/negativos FUTUROS, sin atribuir pruebas .NET/MySQL/HTTP/Angular/Chromium ejecutadas ni activar flags, cobros, cuotas o migraciones.
- Preserva D01–D08 pendientes y exige V17 lock, Scope Lock/V5 exact SHA, A0–A4 documentales y revisión A5 independiente, exclusivamente en dev.

## 2026-11-15 — V17 C04-03.M02.03 persistencia conceptual de metering [Javier :30]
- Define doce tablas lógicas, doce índices compuestos tenant-first, ocho etapas reversibles propuestas y doce escenarios de datos sintéticos FUTUROS, sin DDL ni integración real para C04-03.M02.03.
- Mantiene EmpresaId, UTC, idempotencia, ledger aditivo, RBAC/PII y rollback como invariantes documentales; D01–D08 continúan pendientes sin activar pagos, EF/MySQL, APIs o jobs.
- Entrega individual R0 con candado V17 2000000658, Scope Lock y procedencia causal, A5 independiente de otra Task Javier y sin QA/main/PROD.

## 2026-11-15 — V17 C04-03.M02.05 contrato de eventos e idempotencia de metering [Javier :40]
- Define doce eventos conceptuales, sobre tipado versionado, dieciseis controles de idempotencia, tenant, UTC, ledger/outbox y doce pares de aceptacion futuros, sin API, DB ni pagos reales.
- Separa unicidad (EmpresaId, SourceSystem, IdempotencyKey) del PayloadDigest inmutable; el mismo key con digest diferente debe generar conflicto, nunca doble efecto.
- R0 documental individual con V17 lock 2000000660, A0-A4 sujetos a Scope Lock/V5 exact-SHA, A5 independiente, D01-D08 pendientes y prohibido QA/main/PROD.

## 2026-11-15 — V17 C04-03.M02.02 contratos conceptuales de metering [Javier :30]
- Define ocho operaciones y ocho DTO conceptuales, dieciséis validaciones, catorce errores, ocho reglas de compatibilidad y diez pares positivos/negativos futuros para C04-03.M02.02.
- Mantiene tenant/RBAC/idempotencia/UTC/ledger/fail-closed como contratos sin declarar pruebas HTTP, .NET o MySQL ejecutadas; D01–D08 continúan pendientes y el alcance R0 no agrega runtime, API ni billing real.
- Entrega individual V17 con candado 2000000657, Scope Lock y procedencia exact-SHA; A5 independiente de otra Task Javier y ninguna promoción QA/main/PROD.

## 2026-11-15 — V17 C04-03.M02.01 modelo conceptual y ownership de metering [Javier :30]
- Documenta entidades, claves compuestas por EmpresaId, relaciones e invariantes de consumo, ventanas UTC, idempotencia, ledger, compensación, conciliación y privacidad para M02.01 exclusivamente.
- Distingue criterios positivos/negativos futuros NO_EJECUTADOS de las pruebas efectivas R0; D01–D08 siguen pendientes y no se crean API, EF/MySQL, cuotas, pagos, jobs ni permisos de producción.
- Cumple secuencia V17 single active, candado 2000000656, Scope Lock y procedencia causal; requiere A5 independiente Javier, sin QA/main/PROD.

## 2026-11-15 — V17 CI_REPAIR C10-04.M04.01 auditor de rutas exactas [Javier :20]
- Repara FAIL real del job Priority4 ERP del SHA previo: el guard existente es plataformaGuard pero faltaba la ruta nueva del conjunto cerrado permitido. Añade solo el path literal `plataforma/continuidad-operativa-v5` al verificador R2, preservando todas las comprobaciones de sesión backend, denegación y prohibiciones de prefijos o comodines.
- Exige auditoría ERP, frontend Vitest 6+4, inventario P0-007.A, tipado/build, Scope Lock y procedencia por SHA/equivalencia auditada; ningún PASS ficticio sobre queued/in_progress/fail. Mantener VALIDANDO y A5 de otro autor hasta evidencia completa.
- Solo dev, rollback git revert normal, sin QA/main/PROD ni seguridad rebajada.

## 2026-11-15 — V17 CI_REPAIR C10-04.M04.01 catálogo exacto y baseline Vitest [Javier :20]
- Repara fallo P0-007.A real en SHA 125fe037: inventario aditivo 143 rutas (135 feature/8 shared) y detector fail-closed mantiene omisión, duplicación y archivo ajeno como errores. No reescribe recuentos históricos 137–142.
- Repara dos pruebas desactualizadas de auditoría operativa de otro alcance sin modificar la UI, el guard ni la autorización: las pruebas permiten solo el botón real de revalidación y verifican el texto de denegación existente; preservan controles ocultos, ausencia de formularios y mensajes sin secretos.
- Backend y seguridad permanecen intactos; revalidar Vitest dirigidos, prueba de catálogo, builds, Scope Lock y procedencia en SHA descendiente. Mantener C10-04.M04.01 VALIDANDO hasta CI completa, A5 por Task Javier distinta.

## 2026-11-15 — V17 C10-04.M04.01 pantalla privada de continuidad operativa [Javier :20]
- Añade ruta lazy de plataforma con plataformaGuard y componente Angular 22.2.1 solo lectura; cuatro controles, denegacion fail-closed y revalidacion single-flight, sin acciones de restore, permisos o proveedores.
- Agrega seis Vitest positivos/negativos con GitHub Actions Node 24/npm 11, lint y build; no certificar PASS antes de CI exacta ni afirmar MySQL/HTTP/Playwright no aplicables a la interfaz documental.
- R1 V15/V17 candado 2000001568 y A0–A4 por SHA; A5 independiente de otra Task Javier. Solo dev, sin QA/main/PROD; rollback git revert.

## 2026-11-15 — V17 C04-03.M01.06 contrato terminal de bloqueo y reversión de metering [Javier :10]
- Documenta B01–B16 de NO-GO, prerequisitos M01.01–05 con cinco A5 independientes y C01-03.M01.06+A5, riesgos y ocho decisiones comerciales D01–D08 aún PENDIENTE; DAG F/G VALIDADO/SI permite contrato M01.06, nunca aprobación de metering o cobro real.
- Añade ocho positivos y doce negativos FUTUROS_NO_EJECUTADOS para tenant A/B, idempotencia de consumo, cuotas, unidades, límites, privacidad, UTC, rollback, CI y A5; sin introducir API, DB, workers, eventos, proveedores ni cuotas activas.
- R0/V17 lock único 2000000655 y A0–A4 documental; requiere Scope Lock y V5 procedencia exact SHA, BITACORA/readback y A5 independiente. Sin QA/main/PROD, rollback git revert normal.

## 2026-11-15 — V17 C07-04.M03.06 auditoría comercial estructurada segura [Javier :00]
- Implementa pipeline Infrastructure .NET 10 acotado de auditoría de previsualizaciones comerciales, integrando lector de autoridad backend y batch M03.05; no crea worker alojado, pago, factura, transacción ni auditoría durable.
- Doce pruebas xUnit R2 de tenant A/B, membresía/RBAC/revocación, SHA, correlación maliciosa, replay, monto/fiscal, cancelación y fallas opacas de fuente/logger. Exige Backend Release, seguridad multiempresa, Authorization, Scope Lock y V5 por SHA exacto antes de LISTO.
- Solo registros ILogger con códigos fijos, correlación interna y conteos; nunca PII, JWT, SHA, identificador de pago, claves ni excepción. Integraciones HTTP/MySQL/PSP/Playwright no aplican en este scope aislado; A5 requiere otra Task Javier; no QA/main/PROD.

## 2026-11-15 — V17 C02-02.M03.05 previsualización de cola de placement aislada [Javier :10]
- Añade Infrastructure .NET 10 C0202PlacementIsolatedDispatchQueue: admisión sintética única, mutex y fingerprint ordinal sobre journal M03.04; ningún hosted worker, provider, DB, migración, red ni cutover real.
- Incorpora 11 xUnit positivos/negativos de tenant A/B, actor/SHA, JWT-RBAC/revocación, DSN, idempotencia/replay, concurrencia de 24, cancelación y constructor fail-closed; requeridos Backend Release, seguridad, Authorization, Scope Lock y V5 por SHA.
- V15 R2 / V17 single-active, A5 de otra Task Javier; no atribuir pruebas MySQL/HTTP, no QA/main/PROD; git revert normal como rollback.

## 2026-11-15 — V17 C14-02.M03.06 auditoría segura de aceptación [Javier :50]
- Implementa sink Infrastructure .NET 10 que resume decisiones del gate Application en evento tipado y redactado: resultados fijos, correlación sólo con contexto verificado, sin tenant/actor/recurso/SHA ni PII/JWT/secretos en logs.
- Incluye 11 xUnit positivos/negativos de tenant A/B, JWT/RBAC revocado simulado, SHA, resource spoof, error, bypass y restauración, cancelación y logger fallido; la auditoría in-memory no certifica proveedor duradero.
- V15 R2 / V17 one-active y Scope Lock STRICT: requeridos Backend Release, seguridad, Authorization y V5 en SHA exacto; A5 de Task Javier independiente; sin EF/MySQL/HTTP/Angular/QA/main/PROD, revert normal.

## 2026-11-15 — V17 C15-01.M03.05 revisión secuencial de cobertura en lotes [Javier :50]
- Implementa runner Infrastructure .NET 10 de máximo 16 requisitos; prevalidación íntegra de ID, SHA y unicidad ordinal antes de leer fuente, composición con adaptador existente M03.04; no certifica cobertura global ni escribe PLAN/Sheets.
- Añade 11 xUnit positivos/negativos de duplicidad, lote nulo/excesivo, SHA mezclado, fuente negada, excepción opaca y cancelación. CI Backend Release, seguridad multiempresa, autorización, Scope Lock y V5 requerida por SHA.
- V15 R2 y V17 exclusivo, sin provider externo/HTTP/MySQL/Angular ni autorización real de tenant, A5 por otra Task Javier; rollback git revert normal; no QA/main/PROD.

## 2026-11-15 — V17 C15-03.M03.06 auditoría estructurada segura [Javier :50]
- Implementa sink Infrastructure .NET 10 de auditoría estructurada, sobre decisiones Application: códigos normalizados, SHA y correlación canónicos; nunca serializa JWT, PII, secreto, tenant/actor IDs, texto de excepción o idempotency key.
- Incorpora 11 pruebas xUnit positivas, negativas y de borde para tenant A/B, fuente no verificada, bypass, superficie desconocida, SHA, denegación, logger inactivo/fallido y cancelación.
- V15 R2, V17 single-active: requiere Backend Release, seguridad multiempresa, Authorization, Scope Lock y V5 completed/success exact-SHA; A5 independiente Javier. Sink aislado sin DI ni entrega durable, sin HTTP/EF/MySQL/proveedor/QA/main/PROD; rollback git revert normal.

## 2026-11-15 — V17 C11-04.M03.04 adapter snapshot privado de observabilidad [Javier :40]
- Anade Infrastructure .NET10 aislado con snapshot sintetico inmutable y copia profunda de labels; compone gate Application con identidad tenant y SHA exactos, no expone telemetria, no consulta bases reales ni certifica SLO.
- 11 xUnit adversariales cubren tenant A/B, JWT/RBAC/revocacion, SHA, etiquetas PII, NO_MEDIDO, cambio mutable de labels, correlacion y cancelacion; CI Release/seguridad/Authorization/Scope Lock/V5 causal requerida.
- V15 R2, V17 single-active; A5 de Task Javier distinta, integraciones MySQL/HTTP no aplican a artefacto aislado; rollback git revert normal, sin QA/main/PROD.

## 2026-11-15 — V17 C07-04.M03.05 lote acotado de revisión comercial sin efectos [Javier :40]
- Implementa procesador Infrastructure .NET 10 de hasta 16 tickets; reutiliza adaptador de lectura M03.04 con validación backend de tenant, actor, pertenencia y permiso. Idempotencia intra-lote y rechazos de comandos inconsistentes, sin transacciones, pagos, exportaciones ni hosted worker.
- Diez xUnit adversariales: tenant B, revocación, SHA, importe, versión, divisa, operación fiscal, duplicado/replay, batch size y cancelación. CI Release, Seguridad multiempresa, Authorization, Scope Lock y V5 requeridos por SHA.
- V15 R2 / V17 single-active, A5 revisor Javier distinto; PSP/MySQL/HTTP no aplican a este código aislado, integración real pendiente de otro scope; revert normal, sin QA/main/PROD.

## 2026-11-15 — V17 C02-01.M03.04 preferencia tenant aislada read-only [Javier :30]
- Integra adapter Infrastructure .NET 10 no autoritativo con instantánea de preferencia por usuario, validación ordinal de actor/revisión/SHA y copia de membresías para evitar mutación externa.
- Incorpora 16 pruebas xUnit genuinas de tenant 0/1/N, preferencia revocada/ajena, sesión caducada, roles, fraude de browser/host, CAS, casos negativos y cancelación.
- Riesgo V15 R2 y candado único V17: requieren GitHub Actions backend, autorización, seguridad, Scope Lock y procedencia en SHA causal; A5 corresponde a otra Task Javier. Sin DB/EF/MySQL live, JWT HTTP, cambios UI, QA/main/PROD.

## 2026-11-15 — V17 C02-02.M03.04 rehearsal reversible aislado de placement [Javier :40]
- Implementa journal Infrastructure .NET 10 inerte con staging y rollback de un solo intento en memoria; compone el preflight M03.03, CAS monotono, idempotencia y checks tenant/actor/SHA; no abre conexiones ni activa migracion o cutover.
- Incorpora 12 xUnit adversariales: tenant A/B, JWT/RBAC revocacion, DSN/cutover, alias, replay, concurrencia 24 intentos, revision, rollback y cancelacion. Exige CI Backend Release, seguridad multiempresa, Authorization, Scope Lock y V5 en SHA exacto.
- R2 V15, V17 single-active, A5 por otra Task Javier; sin EF/MySQL/HTTP/DI/QA/main/PROD ni certificados de integracion real. Rollback mediante git revert normal.

## 2026-11-15 — V17 corrección preventiva CI C02-04.M03.05 params null xUnit [Javier :20]
- Corrige una sola expectativa xUnit de ticket nulo: en C# Preview(null) invoca la variante params con un arreglo completo nulo y obtiene CONTROL_PLANE_BATCH_REQUIRED; ahora se pasa explicitamente un arreglo de un ticket null para esperar CONTROL_PLANE_TICKET_INVALID.
- Hipotesis causal comprobable por fallo identico de C03-04.M03.05 (job 133000165419, 4702 PASS/1 FAIL) y log de C02-04.M03.05 job 133001145821 con la misma prueba previa; no se cambia codigo de runtime, control de tenant, MFA, grants ni validadores.
- Proteger A0-A5: nueva CI exact-SHA obligatoria Backend Release/xUnit, seguridad, Authorization, Scope Lock y V5; mantener ambas unidades VALIDANDO hasta SUCCESS verdadero; rollback mediante git revert normal, sin QA/main/PROD.

## 2026-11-15 — V17 reparación CI C03-04.M03.05 params null xUnit [Javier :20]
- Repara asercion de borde sin alterar comportamiento productivo: en C# un argumento null para metodo params nullable array significa lote null, no un array con un ticket null; la prueba ahora pasa explicitamente un array de un elemento null.
- Diagnostico causal: Backend Release job 133000165419 y Backend tests 133000165134 fallaron 1/4703 xUnit en NullEmptyMalformedAndOverLimitBatchesFailClosed, esperado INCIDENT_TICKET_INVALID frente a INCIDENT_BATCH_REQUIRED; la implementacion seguia denegando correctamente, 4702 PASS, ningun acceso concedido.
- No modifica compiladores, validadores, seguridad, Scope Lock, codigo de produccion ni comportamiento de autorizacion. Requiere CI xUnit/Backend/Seguridad/Authorization/ScopeLock/V5 nueva exact-SHA, estado VALIDANDO hasta PASS. Reversibilidad git revert.

## 2026-11-15 — V17 C02-04.M03.05 worker aislado read-only de control plane [Javier :20]
- Implementa worker Infrastructure .NET 10 de previsualizacion limitada a 24 tickets por llamada, compuesto con el adapter M03.04 y gate Application/Domain; no registra hosted runtime ni realiza ninguna operacion institucional.
- Incluye nueve metodos xUnit con positivos/negativos y fronteras: tenant A/B, actor, SHA, origen sintético no confiable, revocacion, MFA, grant separado, duplicado ordinal, exportacion, idempotencia y cancelacion.
- Riesgo R2 V15, Scope Lock STRICT; A0-A4 causal con GitHub-hosted .NET, seguridad, Authorization, procedencia V5, y A5 independiente por otra Task Javier. Un resultado favorable solo exige adapter autenticado posterior, NUNCA concede un permiso.
- No JWT/HTTP live, MySQL/EF, colas/proveedores, secrets, cambios de DI o autorizacion QA/main/PROD. Rollback git revert, sin force-push.

## 2026-11-15 — V17 C03-04.M03.05 worker aislado read-only de incidentes [Javier :20]
- Añade componente Application .NET 10 para previsualizar hasta 24 tickets de incidentes sin activar hosted worker, proveedor o restauracion real; resultados son decisiones fail-closed y nunca autorizan lectura, mutacion, evidencias ni backups.
- Incorpora suite xUnit con positivos, negativos y fronteras para tenant A/B, sucursal, SHA, idempotencia ordinal, procedencia, revocacion, sanitizacion, version, mutacion/restore, lotes limitados y cancelacion.
- Riesgo V15 R2, una microtarea propia EN_PROGRESO V17 y un commit causal; A0-A4 dependen de GitHub-hosted CI backend, seguridad, autorizacion, Scope Lock y procedencia V5 exact-SHA. A5 debe hacerla otra Task Javier.
- No hay JWT HTTP real, EF/MySQL, persistencia, cola externa, secretos, credenciales, IAM vivo, API ni autorizacion de QA/main/PROD; rollback por git revert normal.

## 2026-11-15 — V17 reparación CI C14-02.M03.05 denegación por revocación [Javier :00]
- Repara una única aserción xUnit cuyo código esperaba RESOURCE_OR_PERMISSION_REVOKED aunque el gate Application seguro rechaza antes con CURRENT_AUTHORIZATION_TENANT_AND_AUDIT_REQUIRED.
- Causa exacta acreditada por GitHub Actions seguridad multiempresa job 132996317941: 1620 PASS, 1 FAIL, error de comparación en FileDownloadIsNeverGrantedWhenLinkExpiredOrOwnershipRevoked, línea 158; no degradar denegación ni desactivar tests.
- Prueba y código de seguridad permanecen separados: solo se corrige la expectativa de rechazo temprano; exigir nuevo CI completo por SHA, Scope Lock, V5, backend, autorización y pruebas R2; A5 independiente sigue pendiente.

## 2026-11-15 — V17 C14-02.M03.05 matriz negativa de aceptación de seguridad [Javier :00]
- Agrega catorce métodos xUnit .NET 10 sobre Domain/Application/Infrastructure aislados: BOLA/IDOR tenant A/B, actor/recurso, sucursal, host, archivos, jobs, caché, restauración, JWT/RBAC, revocación, spoofing, SHA y cancelación.
- Exige Backend Release, seguridad multiempresa, Authorization, Scope Lock y procedencia V5 con SUCCESS causal antes de LISTO; un preflight nominal nunca concede accesos, escrituras, jobs ni despliegue.
- R2 de pruebas in-memory: no certifica JWT HTTP, MySQL/EF, backups/R2 ni proveedores productivos; A5 distinta Task Javier, rollback git revert, solo dev.

## 2026-11-15 — V17 C10-04.M03.06 auditoria estructurada segura cache/jobs [Javier :10]
- Implementa servicio .NET 10 Infrastructure con ILogger y lector de contexto autenticado, operaciones acotadas, tenant A/B y RBAC/revocacion fail-closed; solo codigo operacional y correlacion confiable sin secretos en logs.
- Nueve pruebas xUnit positivas/negativas de spoof, sink, autoridad, permisos revocados, errores sanitizados, cancelacion y no concesion, requieren Release/security/auth/Scope Lock/V5 causal.
- R2: no activa worker, cola, MySQL, HTTP, secretos ni proveedor real; sink durable requiere adaptador independiente y A5 otra Task Javier, rollback git revert.

## 2026-11-15 — V17 C02-01.M03.03 resolucion segura login 0/1/N [Javier :10]
- Implementa politica Application .NET 10 de identidad/JWT respaldados por lector backend, membresias actuales, RBAC, sesion, preferencia por usuario, revocacion y cambio seguro: no concede permisos HTTP ni JWT.
- Anade 15 pruebas xUnit de tenant A/B, sesion vencida, cross-account, host/role spoofing, error de proveedor, limites y cancelacion; CI backend/security/auth/Scope Lock/V5 causal obligatoria.
- Riesgo R2: solo politica ejecutable Application, sin modificar API/EF/MySQL/UI/produccion; exige integracion futura y A5 ajena, git revert normal.

## 2026-11-15 — V17 C15-03.M03.05 matriz negativa cruzada seguridad y tenancy [Javier :00]
- Añade catorce métodos xUnit reales sobre Domain/Application/Infrastructure aislados: tenant A/B, sucursal, propietario, SHA, roles, revocación, archivos, jobs, control plane, bypass, enumeración y cancelación.
- Los preflights nominales nunca conceden lectura, descarga, escritura, job ni control plane; exigir Backend Release, seguridad multiempresa, Authorization, Scope Lock y V5 exact-SHA antes de LISTO.
- R2: fakes no certifican JWT HTTP, MySQL/EF ni proveedor productivo; A5 solo por otra Task Javier, rollback git revert normal; sin QA/main/PROD.

## 2026-11-15 — V17 C01-03.M03.03 gate de autorización de configuración dinámica [.NET 10 Javier :50]
- Implementa gate Application que obtiene JWT/membresía/RBAC/entitlement actuales desde backend, valida tenant propietario y catálogo ordinal exacto y compone Domain/Application sin conceder escritura.
- Agrega doce xUnit de rutas positivas, negativas y errores, incluida regresión OrdinalIgnoreCase de M03.01; se exigen GitHub Actions backend, seguridad, autorización, Scope Lock y procedencia V5.
- Es código R2 de prevalidación sin DI/API/EF/MySQL ni mutación durable; resultado positivo requiere adaptador autenticado con CAS y A5 de otra Task Javier; rollback git revert normal.

## 2026-11-15 — V17 C14-01.M03.03 autorización fail-closed de aceptación funcional [Javier :50]
- Implementa gate Application .NET 10 con lector backend confiable, JWT actual, membresía/RBAC/ownership/tenant, SHA y catálogos verificados; nunca acepta elevación declarada por cliente.
- Añade trece pruebas xUnit positivas/negativas: tenant A/B, revocación, límites, SHA, privacidad, error, cancelación y ausencia de concesión; CI del SHA requerido para A0–A4.
- Permanece preflight aislado sin DI/HTTP/EF/MySQL/UI/PSP real; resultado favorable requiere adaptador autenticado, aceptación externa y A5 independiente. Solo dev y rollback git revert normal.

## 2026-11-15 — V17 C05-01.M03.03 fence de design tokens y RBAC [Javier :40]
- Integra guard de identidad relacional vigente en Application .NET 10 con permiso Configuracion/Editar, evidencia de catálogo backend por tenant y sesión/revisión; reutiliza el caso de uso M03.02 y reglas Domain M03.01.
- Añade 12 xUnit adversariales tenant A/B, RBAC/membresía, sesión, catálogo ordinal, CSS malicioso y cancelación; requiere Backend Release, Seguridad multiempresa, Authorization, Scope Lock y V5 exact-SHA.
- Resultado de preflight nunca concede tema, sesión JWT, write, API ni MySQL. R2, backend provider y HTTP por acreditar, A5 independiente, rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V17 C10-04.M03.05 cola acotada de revisión de jobs [Javier :40]
- Integra worker .NET 10 de revisión en memoria con Channel de capacidad 32 y composición del adaptador Infrastructure, gate Application y reglas Domain existentes; sin despachar jobs/restore/exportaciones ni conceder leases.
- Incorpora 12 xUnit adversariales de tenant A/B, actor, versión, replay, digest de idempotencia, payload, revocación, servicio, retry, deadline, capacidad y cancelación; requieren Backend Release, seguridad, Authorization, Scope Lock y V5 exact-SHA.
- R2: worker no persistente, sin MySQL/Cloudflare/R2 ni proveedor o restauración real. A0–A4 y revisión A5 de otra Task Javier; rollback git revert normal, no QA/main/PROD.

## 2026-11-15 — V17 C01-02.M03.03 fence de capabilities y RBAC [Javier :40]
- Implementa Application .NET 10 de intersección fail-closed por empresa, rol efectivo, sucursal, recurso, plan, rubro y capability; reconsulta identidad y RBAC relacional vigentes mediante IdentidadUniversalAuthorizationGuard.
- Añade 12 xUnit genuinos positivos/negativos de tenant A/B, revocación, grant falso, propiedad, dominio, fuente ausente, clave ordinal y cancelación; exigir Backend Release, seguridad, Authorization, Scope Lock y V5 exact-SHA.
- Resultado no autoriza endpoints, datos, exportaciones, activación ni override tenant; lector de política persistida y adaptador HTTP son integración futura independiente. R2, A5 por otra Task Javier, rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V17 C02-02.M03.03 adaptador aislado de contexto de ubicación tenant [Javier :30]
- Implementa Infrastructure .NET 10 con captura read-only de tenant, actor, revisión CAS y SHA, reutilizando Domain/Application sin seleccionar conexión, ejecutar MySQL, migraciones ni cutover.
- Añade 13 xUnit reales de tenant A/B, fuente obsoleta, propietario falsificado, RBAC, revocación, binding, restauración y idempotencia; exigir GH Actions Release, seguridad, autorización, Scope Lock y V5 exact-SHA.
- V15 R2, V17 single-active con candado fila 340; preflight no autoriza almacenamiento compartido/dedicado, necesita A5 de Task distinta y no toca QA/main/PROD.

## 2026-11-15 — V17 C14-02.M03.04 adaptador aislado de aceptación de seguridad [Javier :30]
- Implementa Infrastructure .NET 10 aislado read-only con snapshot de actor, tenant, recurso y SHA; reutiliza Domain/Application sin habilitar acceso, restauración, descarga, job o release.
- Incorpora 13 pruebas xUnit positivas/negativas de BOLA/IDOR, tenant A/B, revocación, autenticación declarada, permisos, auditoría, SHA, host, archivo expirado y cancelación; GH Actions debe acreditarlas causalmente.
- Riesgo V15 R2; V17 candado exclusivo fila 2069, A0–A4 y A5 independiente obligatorios. Ningún test JWT HTTP/MySQL real ni QA/main/PROD; rollback git revert normal.

## 2026-11-15 — V17 C08-04.M03.03 segunda reparación puntual Scope Lock [Javier :20]
- En el check exacto de 82612c80 subsistía una sola variante de fixture de módulo con mayúsculas. El primer reemplazo había corregido tres apariciones minúsculas, pero no la variante capitalizada; se corrigió ahora esa cuarta aparición en la prueba negativa, conservando el comportamiento y los asserts.
- Revisión del archivo completo contra patrones retirados del validador, 0 coincidencias. No se desactiva ni modifica Scope Lock o controles de seguridad; fallos anteriores conservados. Revalidar CI en SHA nuevo, mantener VALIDANDO hasta éxito de A0-A4.

## 2026-11-15 — V17 C08-04.M03.03 reparación Scope Lock R2 [Javier :20]
- Corrige únicamente el identificador de fixture de módulo en xUnit a la clave sector.orders, sin variar verificaciones positivas o negativas, autorización JWT/RBAC/tenant ni el test de denylist.
- En SHA 13ccfd71, Scope Lock y seguridad multiempresa fallaron por coincidencia accidental de identidad retirada en el fixture; fallo original permanece registrado, no se desactivó el verificador.
- Requiere nueva CI exact-SHA Backend Release, seguridad, Authorization, Scope Lock y procedencia V5, más readback institucional; mantiene VALIDANDO y A5 independiente.

## 2026-11-15 — V17 C08-04.M03.03 tenant module authorization fence R2 [Javier :20]
- Añade fence Application real no autorizante con lectores separados de actor backend vigente y recurso sectorial tenant, cotejo ordinal de clave/owner/SHA, revisión, JWT/RBAC/membresía y política actual.
- Incorpora xUnit positivos/negativos, rechazo A/B, revocación, catálogo, denylist, errores y cancelación. No registra DI, ejecuta SQL, activa módulos ni muta permisos; resultado exige adaptador transaccional futuro autenticado.
- A0–A4 con .NET GitHub-hosted, seguridad/autorización, Scope Lock y procedencia V5 causal; A5 por otra Task Javier, rollback git revert; sin QA/main/PROD.

## 2026-11-15 — V17 C05-01.M03.02 Application design token preflight R2 [Javier :20]
- Incorpora un caso de uso Application real, read-only y fail-closed con identidad backend, catálogo tenant y reglas Domain M03.01, sin permisos de escritura ni endpoints, EF/MySQL o adaptadores de proveedor.
- Añade xUnit con rechazo A/B, RBAC/JWT/revocación, clave ordinal, revisión/sesión, CSS inseguro, idempotencia, errores y cancelación. No declara éxito antes de GitHub Actions de SHA causal.
- A0–A4 CI Release/seguridad/autorización/Scope Lock/V5 y readbacks son obligatorios; A5 independiente posterior. Rollback git revert y sin QA/main/PROD.

## 2026-11-15 — V17 C01-03.M03.02 servicio Application de configuración segura R2 [Javier :10]
- Implementa Application .NET10 con lecturas tipadas de identidad confiable y catálogo por tenant backend, compara tenant, SHA, versión y clave canónica y delega al Domain validado.
- Agrega nueve xUnit positivos/negativos para revocación, RBAC, A/B, catálogos falsos, variantes de casing, CAS pendiente, datos inseguros, errores y cancelación; la ruta favorable nunca escribe ni autoriza un override.
- No registra DI/endpoint, ni SQL/EF/MySQL, flags efectivos o operaciones productivas; exige CI Release/seguridad/autorización/Scope Lock/V5 SHA causal, A5 de Task Javier distinta y rollback mediante git revert.

## 2026-11-15 — V17 C11-04.M03.03 gate autenticado tenant observabilidad R2 [Javier :00]
- Implementa gate .NET 10 de verificacion previa con principal JWT/membresia/RBAC vigente de fuente backend, sin tenant del cliente, lector particionado por identidad autenticada y cotejos Ordinal de tenant A/B.
- Reutiliza Application M03.02 y Domain M03.01, agrega xUnit adversariales de revocacion, rol/grant, SHA, senal/correlacion, PII, NO_MEDIDO, fuente ajena y cancelacion; requiere GitHub Actions backend Release/seguridad/autorizacion/Scope Lock/V5 SHA causal.
- El resultado es siempre NO autorizante: sin API, JWT middleware, EF/MySQL, export, SLO medido, grants, QA/main/PROD ni datos reales; A5 de otra Task Javier, rollback git revert normal.
## 2026-11-15 — V17 C14-04.M03.04 adaptador aislado snapshot readiness DEV [Javier :00]
- Implementa interfaz read-only de snapshot DEV y clase .NET 10 Application; usa solo tenant de identidad backend verificada, SHA exacto, auditoria, ownership, version y rollback para rechazo seguro, sin devolver datos.
- Agrega xUnit positivos/negativos de tenant A/B, JWT/RBAC/revocacion, QA/PROD, source SHA, digest, snapshot mutable, evidencia ausente, CI cancelada y self-review; CI backend Release, seguridad, autorizacion, Scope Lock y V5 por SHA obligatoria.
- No implementa EF/MySQL, restore real, endpoints, DDL, backups, provider, scheduler ni despliegue; A5 independiente y revert normal, sin QA/main/PROD.
## 2026-11-15 — V17 C07-04.M03.04 adaptador aislado de documentos comerciales R2 [Javier :10]
- Agrega Infrastructure read-only con fuentes de identidad/roles vigentes y documento por tenant+ID, validando pertenencia A/B, NULL legado, version, SHA y referencia antes de reutilizar la puerta Application comercial.
- Anade xUnit positivos, negativos, limites, revocacion, origen indisponible, identidad, permisos, monto, version y cancelacion; requiere CI backend Release, seguridad, autorizacion, Scope Lock y procedencia V5 en SHA causal.
- El adaptador no se registra en DI ni ejecuta pagos, facturas, SQL, migraciones, PSP, HTTP, exportacion o transacciones; resultado siempre sin permiso de efecto, A5 independiente y rollback git revert sin QA/main/PROD.

## 2026-11-15 — V17 C15-01.M03.04 adaptador aislado read-only de requisitos [Javier :50]
- Implementa adaptador .NET Infrastructure con puerto de snapshot institucional verificado, SHA exacto, catálogo ordinal, evidencia de lector y capacidad acotada; compone Application/Domain existentes sin leer Sheets directamente ni habilitar A5/escritura o declarar cobertura completa.
- Añade 12 xUnit positivos/negativos, incluyendo regresión de `IReadOnlySet` ordinal-insensible, fuente falsa, SHA, duplicados, estados, capacidad, revisor y cancelación; backend Release, seguridad, Scope Lock y V5 requeridos en SHA antes de LISTO.
- R2 de sólo lectura; sin MySQL/EF, API/HTTP, proveedores, runtime de Work ni cambios de fórmula. Cadena V17 y readbacks, A5 independiente, rollback git revert sin QA/main/PROD.

## 2026-11-15 — V17 C03-04.M03.04 adaptador aislado de incidentes [Javier :40]
- Añade proyeccion .NET 10 de metadatos de incidente sin EF ni MySQL; niega tenant B, branch ajena, SHA incorrecto, revocacion y evidencia insegura, sin conceder permisos reales.
- Ocho metodos xUnit positivos/negativos y frontera, solo para validacion del adaptador, no para certificar JWT, DB o restauracion; A5 independiente obligatoria.
- Un commit de cuatro archivos sobre dev con V17, Scope Lock STRICT y CI exact-SHA; sin QA/main/PROD, secretos o datos productivos.

## 2026-11-15 — V17 C14-02.M03.03 autorización fail-closed seguridad [Javier :50]
- Añade puerta Application `C1402SecurityAcceptanceAuthorizationGate` sobre contrato previo Domain/Application, lector backend de evidencia confiable, exact-SHA, recurso, membresía/JWT/RBAC y aislamiento tenant A/B; nunca concede acceso ni certifica seguridad HTTP/PROD.
- Incluye doce xUnit para negativos de spoofing, revocación, BOLA, SHA, overrides antes del lector, superficies, prueba insegura, fuente ausente y cancelación, más flujo válido no autorizante. .NET Release, seguridad, autorización, Scope Lock y V5 exact SHA son obligatorios para LISTO, sin PASS inventado.
- R2 aislado: sin EF/MySQL, API/HTTP, navegador, proveedores ni backup real; A5 solo por revisor Javier distinto, candado V17 con BITACORA/readback, rollback git revert, no QA/main/PROD.

## 2026-11-15 — V17 C12-04.M03.02 reparación defensiva de IDs y snapshot R2 [Javier :40]
- Endurece servicio Application para rechazar IDs actor/tenant no canónicos o mayores de 128 antes de consultar el proveedor de evidencias; no aceptar caracteres de control, URLs ni identificadores con separadores inseguros.
- Rechaza snapshots nulos de proveedor defectuoso con EVIDENCE_PROVENANCE_MISMATCH sin aceptar veredictos de autoridad ni hacer SQL/cutover; agrega dos xUnit negativos de regresión.
- Reparación mínima sobre cuatro archivos de la microtarea VALIDANDO, V17, SHA nuevo y revalidación backend/seguridad/autorización/ScopeLock/V5; A5 ajena, sin QA/main/PROD.

## 2026-11-15 — V17 C12-04.M03.02 servicio Application privado de revisión MySQL [Javier :40]
- Agrega servicio .NET 10 aislado con interfaz de lectura de evidencia, verificación fail-closed de actor/tenant/SHA y reutilización Domain C12-04.M03.01; jamás autoriza SQL, migración, cutover o deployment.
- Incluye ocho pruebas xUnit con caso de revisión dev acotada, rechazos JWT/RBAC/MFA/tenant A/B, grants, restore, fencing, SHA cruzado, cancelación y fallo del proveedor; lectores falsos no prueban MySQL ni HTTP reales.
- Sólo Application + xUnit + docs/V5 + CHANGELOG, Scope Lock STRICT, CI dirigida exact-SHA y A5 independiente; sin QA/main/PROD, bases reales, secretos o migraciones.

## 2026-11-15 — V17 C11-04.M03.02 prevalidación Application de observabilidad privada [Javier :50]
- Implementa `C1104ObservabilityApplicationService` e interfaz de lectura confiable para agregados privados, fuente SHA, JWT/RBAC/membresía actual, propiedad tenant A/B y fence de auditoría; nunca concede acceso, exportación, mutación ni certifica SLO/costo.
- Añade 13 casos xUnit con positivos, rechazos antes de I/O, comparación ordinal tenant/SHA, revocación, etiqueta prohibida, límites UTC/conteos/NO_MEDIDO y cancelación; backend Release y seguridad/autorización CI obligatorias en SHA causal, sin atribuir PASS hasta que terminen.
- R2 aislado Domain->Application: sin endpoint, MySQL/EF, Angular/Playwright, proveedor o datos reales. Scope Lock STRICT, V5/procedencia, candado V17, A0-A4 y A5 independiente; rollback git revert normal, sin QA/main/PROD.

## 2026-11-15 — V17 C05-01.M03.01 reparación comparación ordinal de tokens R2 [Javier :30]
- Repara A5_FAIL P2: el `IReadOnlySet` de claves podía usar comparador `OrdinalIgnoreCase` y aceptar una clave no canónica del catálogo. Exige coincidencia `StringComparison.Ordinal` sin depender del comparador del set.
- Agrega xUnit negativo para catálogo `PRIMARY-COLOR` con solicitud `primary-color` y positivo para catálogo exacto, aunque ambos sets utilicen comparación insensible; ningún veredicto otorga JWT/RBAC, mutación de tema ni permisos reales.
- Reparación Domain+test+CHANGELOG bajo V17 en dev; A5_FAIL y autoría previa se conservan. CI .NET/seguridad/autorización/Scope Lock/V5 del nuevo SHA y revisión independiente por otra Task antes de A5_PASS. Rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V17 reparación R2 C01-03.M03.01 allowlist ordinal [Javier :20]
- Repara defecto A5_FAIL detectado: el `Contains` de un `IReadOnlySet` con comparador `OrdinalIgnoreCase` podía aceptar una clave de configuración con casing no canónico pese al catálogo exacto.
- Aplica comparación `StringComparison.Ordinal` y agrega prueba xUnit negativa para variante `UI.compactMode` y positiva para `ui.compactMode` con el mismo `HashSet`; sin introducir grants, endpoints ni persistencia.
- El código afectado es Domain R2: CI backend/seguridad/autorización/Scope Lock/V5 del SHA nuevo obligatoria. Se conserva A5_FAIL hasta revisión por otra Task, historial aditivo y rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V17 C15-05.M04.03 branding tipado de auditoría [Javier :20]
- Añade identidad visual institucional o neutra mediante input/computed Angular con fallback fail-closed, sin CSS/HTML arbitrario ni identidad de tenant; conserva guard, revalidación y consulta de solo lectura.
- Incorpora cuatro Vitest y cuatro Playwright Chromium con casos positivos, negativos 403/anónimo, payload hostil y responsive; GitHub-hosted Node24/npm11, build/lint, sin simular JWT de backend como PASS.
- Trabajo R1 de una sola microtarea, Scope Lock/procedencia y CI exact-SHA obligatorios antes de LISTO; A5 exclusivamente por otra Task Javier, rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V17 C08-03.M02.06 contrato terminal M02 membresías gimnasio [Javier :10]
- Define barreras B01–B16, doce pares E01–E12 futuros NO_EJECUTADOS y ocho rutas RB01–RB08 de rollback para membresía/ingreso físico; mantiene tenant A/B, JWT/RBAC, revocación y UTC/DST fail-closed.
- Consolida M02.01–05 documentales solo como precondiciones; ninguno activa QR, hardware, pago, MySQL, API, worker ni eventos. M03 sujeto a DAG y A5 independiente real.
- Entrega R0 solo Markdown/CHANGELOG en dev, CI Scope Lock/V5 exact-SHA, bitácora, candado V17 y rollback git revert normal, sin QA/main/PROD.

## 2026-11-15 — V17 C08-02.M02.06 contrato terminal restaurante QR y rollback [Javier :00]
- Concilia cinco M02 LISTO con A5 independientes, dieciséis barreras B01–B16, doce pares E01–E12 FUTUROS_NO_EJECUTADOS y ocho reversas RB01–RB08 para QR falso, aislamiento, pedidos y cierre ERP idempotente.
- Conserva ERP como autoridad de precio, IVA, stock y factura, JWT/RBAC/tenant server-side, revocación, privacidad, concurrencia y reintentos seguros; no libera M03 sin DAG y A5 propio.
- Únicamente R0 documental sin QR/API/Angular/MySQL/eventos/PSP/hardware ejecutables, con Scope Lock/V5 exact SHA, lock V17/bitácora/readback y rollback git revert; sin QA/main/PROD.

## 2026-11-15 — V17 C08-03.M02.05 eventos, idempotencia y correlación de gimnasio [Javier :00]
- Define dieciséis eventos conceptuales EV01–EV16, dieciocho invariantes INV01–INV18, diez límites L01–L10 y doce escenarios T01–T12 positivos/negativos FUTUROS NO_EJECUTADOS.
- Protege aislamiento tenant/sede A/B, separación SaaS/gimnasio, ERP de pagos, UTC/DST, revocación/QR, idempotencia y denegación offline; no declara valores medidos de TTL o cuotas.
- Entrega R0 sólo documento Markdown/CHANGELOG sin bus, outbox, worker, webhook, PSP, hardware, MySQL o API real; Scope Lock/V5 exact SHA, candado V17, A5 independiente y rollback Git revert; sin QA/main/PROD.

## 2026-11-15 — V17 C08-03.M02.04 configuración tipada segura membresías gimnasio [Javier :10]
- Contrato R0 de seis ámbitos, dieciséis opciones tipadas O01–O16 con defaults fail-closed y distinción PositiveInteger/NonNegativeInteger; prohíbe acceso concedido por flags, QR o UI.
- Doce controles FG01–FG12 y diez pares T01–T10 FUTUROS_NO_EJECUTADOS: tenant A/B, JWT/RBAC, revocación, UTC/DST, idempotencia, offline y rollback; D01–D08 M01 siguen abiertas.
- Solo documento Markdown en dev; sin API, MySQL, lectores, pagos, UI ni eventos ejecutables. Candado V17, Scope Lock/procedencia SHA, BITACORA y A5 independiente; rollback git revert normal.

## 2026-11-15 — V17 C08-03.M02.03 persistencia conceptual membresías de gimnasio [Javier :00]
- Define dieciséis proyecciones conceptuales, doce restricciones/índices candidatos, ocho etapas FUTURAS de migración/rollback y doce fixtures positivos-negativos NO_EJECUTADOS para membresía, vigencia y acceso.
- Preserva tenant/sede, UTC/DST, suspensión, QR revocado, idempotencia transaccional y ERP comercial como autoridad; D01–D08 siguen pendientes, sin inventar esquema MySQL existente.
- Entrega R0 solo Markdown y CHANGELOG, sin EF, MySQL, API, hardware ni PSP reales; Scope Lock/V5 exact-SHA, candado V17/readback y A5 independiente; rollback git revert, no QA/main/PROD.

## 2026-11-15 — V17 C08-03.M02.02 contratos API/DTO de membresías gimnasio [Javier :00]
- Define ocho operaciones conceptuales, ocho DTO lógicos, doce errores seguros, dieciséis validaciones y diez pares de pruebas FUTURAS NO_EJECUTADAS para membresía, vigencia UTC/DST, suspensión y acceso.
- Separa UsuarioEmpresa/SuscripcionesSaaS de planes comerciales, impone tenant/sede server-side, JWT/RBAC, revocación, idempotencia y denegación offline; D01–D08 previas siguen pendientes.
- Solo R0 documental en dev, sin API, MySQL, Angular, pagos, QR, hardware ni pruebas runtime; Scope Lock/V5 exact-SHA, candado V17, readback institucional, A5 independiente y rollback git revert normal.

## 2026-11-15 — V17 C08-03.M02.01 modelo membresías y acceso de gimnasio [Javier :50]
- Diseña veinte entidades, relaciones Empresa/Sucursal/Miembro/Plan/Acceso, veinte invariantes y doce pares futuros positivos-negativos sobre caducidad UTC/DST, suspensión, tenant A/B, QR y revocación.
- Solo contrato documental R0, sin dispositivos, cobro, JWT, API o MySQL reales; conserva autoridad ERP, Scope Lock/V5 exact-SHA, gates DAG y A5 independiente pendiente, rollback git revert normal.

## 2026-11-15 — V17 C08-02.M02.05 contratos de eventos/idempotencia restaurante [Javier :50]
- Documenta dieciséis eventos futuros, envelope sin secretos, dieciséis invariantes anti-replay y doce pares de pruebas positivas/negativas NO_EJECUTADAS para mesas QR, cocina, ERP y cierre idempotente.
- Entrega R0 sin bus, colas, worker, webhook, PSP, API ni MySQL; Scope Lock y V5 exact-SHA, A5 independiente, candado V17 y rollback git revert normal.

## 2026-11-15 — V17 C08-02.M02.04 configuración tipada segura mesas QR [Javier :20]
- Especifica SEIS ámbitos deny-first y DIECISÉIS opciones O01–O16 conceptuales; límites NonNegativeInteger con cero expresamente deshabilitado, sin inferir permiso de flags, host, QR o frontend.
- Define DOCE reglas FG01–FG12 y DIEZ pares T01–T10 de prueba futura NO_EJECUTADOS: aislamiento A/B, firma/revocación, idempotencia, rol, cierre, auditoría y caché segura.
- Entrega R0 sólo documental en dev, Scope Lock/V5 por SHA, BITACORA/readback y A5 por otra Task Javier; sin configurar módulos, API, workers, eventos, MySQL, QA/main/PROD.

## 2026-11-15 — V17 C08-02.M02.02 reparación causal del Scope Lock [Javier :20]
- Corregido únicamente el código de error conceptual ER10: el identificador inglés previo coincidía con un patrón de identidad retirada del verificador de alcance; ahora STOCK_OR_PRICE_CONFLICT conserva la semántica de stock y precio canónico.
- Se mantiene intacta la arquitectura, las ocho operaciones, ocho DTO, doce errores, dieciséis validaciones y los pares de pruebas futuras; sin desactivar Scope Lock, seguridad ni otros checks.
- Reparación documental R0 en dev, con nueva comprobación CI del SHA publicado y A5 independiente pendiente; no se crean API, migraciones, cobros ni despliegues.

## 2026-11-15 — V17 C08-02.M02.03 persistencia conceptual restaurante/QR [Javier :50]
- Define esquema conceptual de 16 objetos, 10 reglas de índices/FK, seis pasos de migración/rollback futuros y doce fixtures A/B positivos-negativos NO_EJECUTADOS.
- Solo documento R0, sin EF/MySQL/migraciones en vivo; mantiene ERP como autoridad y exige futura CI MySQL 8.4 efímero, Scope Lock STRICT, A5 de otra Task Javier y candado V17 por fila.

## 2026-11-15 — V17 C08-02.M02.02 contratos API/DTO/errores de mesas QR [Javier :20]
- Propone OCHO operaciones y OCHO DTO inertes de mesas QR, menú, pedidos, cocina y cierre; exige verificación server-side tenant/sede, JWT/RBAC en mutaciones, capacidad separada de QR público y estados terminales sin precio ni stock del cliente.
- Define DOCE errores, DIECISÉIS validaciones y OCHO pares de aceptación futura positivos/negativos, todos NO_EJECUTADOS; no crea endpoints, scripts, esquemas, workers, secretos, cobros ni accesos a producción.
- Una microtarea R0 documental en dev, Scope Lock/V5 exact-SHA y BITACORA/readback; A5 independiente de otra Task Javier, rollback git revert normal y ningún QA/main/PROD.

## 2026-11-15 — V17 C08-03.M01.06: contrato terminal de membresías y acceso [Javier :40]
- Define 16 barreras B01–B16, 12 casos E01–E12 FUTUROS NO_EJECUTADOS y ocho reversas RB01–RB08 para membresía empresarial/comercial, vigencia UTC/DST, suspensión, control de acceso e idempotencia.
- Concilia antecedentes M01.01–05 LISTO+A5 y dependencias técnicas calculadas; M02 requiere A5 independiente de esta M01.06 cuando lo exija el DAG vivo, sin forzar fórmulas ni resolver por inferencia D01–D08 pendientes.
- Únicamente R0 documental en dev, sin hardware, QR ejecutable, pagos, JWT, API, MySQL, Angular, PSP, worker o webhook; CI Scope Lock/V5 exact-SHA, BITACORA/readback y rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V17 C08-02.M02.01 modelo conceptual restaurantes y QR [Javier :50]
- Define 20 entidades conceptuales, ownership Empresa/Sucursal/Mesa, 20 invariantes y 12 pares futuros/no ejecutados de QR falso, tenant A/B, roles revocados, idempotencia, pedido y cierre seguro.
- Entrega R0 solo Markdown, sin runtime, SQL, API ni cobros; ERP conserva autoridad, Scope Lock STRICT, sucesoras bajo DAG/A5 independiente y rollback git revert normal.

## 2026-11-15 — V17 C08-02.M01.06: contrato terminal de mesas, QR y reversas [Javier :40]
- Documenta 16 barreras B01–B16, 12 evidencias E01–E12 FUTURAS NO_EJECUTADAS y 8 rutas de rollback RB01–RB08 para QR falso, mesa ajena, pedido, idempotencia y cierre seguro.
- Concilia las cinco M01.01–05 LISTO con revisiones A5 independientes y las dependencias técnicas/fase calculadas, sin forzar COLA/DAG. M02 requiere gates propios y A5 independiente de esta M01.06 cuando corresponda.
- Sólo Markdown y CHANGELOG en dev, R0 sin API, JWT, MySQL, Angular, PSP, migración, worker, webhook o QR ejecutable; Scope Lock/V5 exact-SHA, BITACORA/readback y A5 de otra Task Javier. Rollback git revert normal, sin QA/main/PROD.

## 2026-11-15 — V17 C15-05.M04.02 estados accesibles de auditoría operativa [Javier :20]
- Implementa carga, vacío, error, éxito y denegación en la ruta privada existente; revalidación single-flight mediante sesión del backend, sin leer microtareas ni conceder permisos por UI.
- Añade Vitest positivos, negativos y bordes de permisos, y Chromium sintético de revalidación/móvil/teclado; los mocks no certifican JWT ni backend tenant real.
- Una única unidad R1 con candado V17 fila 2325, commit dev FF/CAS, checks A0–A4 pertinentes y A5 independiente pendiente; conserva Scope Lock, CHANGELOG autocontenido y rollback git revert.
- Reparación dirigida por fallo Chromium real en SHA 20d61e82: M04.05 exigía 0 botones y rechazó la nueva revalidación legítima; ahora permite exactamente ese botón con etiqueta accesible y conserva 0 formularios, entradas y botones operativos no autorizados. CI debe ejecutarse de nuevo, sin bypass.
- Conecta el nuevo Vitest dirigido C15-05.M04.02 al workflow de plataforma en SHA exacto (ruta y paso explícitos), preserva todas las pruebas existentes y no confunde tipado o build con asserts ejecutados.
- Repara dos fallos reales Vitest de navegación M04.04 (job 132948182785): prueba que sólo existe el botón permitido de revalidación y cero acciones operativas, y alinea el mensaje de denegación con el nuevo estado fail-closed; conserva los controles de autorización y rechazo.

## 2026-11-15 — V17 C04-02.M02.06 contrato de salida tecnico documental [Javier :10]
- Define dieciseis gates G01-G16 y doce pares positivos/negativos FUTUROS_NO_EJECUTADOS para contratos y suscripciones, firma, version, tenant A/B, conciliacion, transiciones invalidas, idempotencia, pruebas MySQL efimero y rollback.
- Predecesoras M02.01-05 cuentan con A5 independientes por DEV02; no se asumen aprobadas D01-D08 comerciales ni se instala API, worker, PSP, migracion o codigo ejecutable. Requiere A5 independiente de ESTE contrato antes de M03.
- R0 documental con V17 candado fila 625 ID 2000000625; Scope Lock/V5 exact-SHA y BITACORA/readback para A0-A4, datos/test de runtime NO_APLICA/NO_MEDIDO; sin QA/main/PROD, rollback git revert.

## 2026-11-15 — V17 C13-02.M02.06 contrato terminal de salida accesibilidad [Javier :00]
- Fija G01-G16 como puertas causales de M02 hacia runtime y doce pares P/N FUTUROS_NO_EJECUTADOS para teclado/foco, zoom/reflow, contraste, semantica, tenant A/B, JWT/RBAC, privacidad, evidencia CI y rollback; sin declarar conformidad WCAG.
- Referencia seis fuentes M01/M02, cinco revisiones A5 independientes de M02.01-05, contratos conceptuales de entidades/API/persistencia/flags/eventos; no hay SQL, Angular, API, eventos o Playwright implementados ni pruebas runtime ejecutadas.
- Unidad unica R0 C13-02.M02.06 + CHANGELOG dev, candado V17 fila 1921 ID 2000001921; A0-A4 documental Scope Lock/V5 exact-SHA, A5 propia pendiente para otra Task Javier; sin QA/main/PROD, secretos, datos reales; rollback normal git revert.

## 2026-11-15 — V17 C09-02.M02.05 eventos conceptuales fidelizacion y reputacion [Javier :10]
- Disena sobre logico tipado, doce contratos EV01-EV12 de puntos, canjes, promos, moderacion, posventa y consentimiento; doce invariantes ID01-ID12 de idempotencia, version CAS, antireplay y aislamiento tenant A/B.
- Doce pares P/N FUTUROS_NO_EJECUTADOS sobre replay, revocacion, fraude, XSS, IDOR, opt-out y poison events. Sin broker, webhook, worker, outbox, servicios, EF/MySQL, API, mensajes, codigo ejecutable ni metricas reales.
- Documento R0 V17 lock SQV17_C09_02_M02_05 fila 1344, Source SHA a verificar, Scope Lock/V5 exact-SHA A0-A4 y A5 independiente Javier; rollback git revert; sin QA/main/PROD.

## 2026-11-15 — V17 C09-02.M02.04 configuracion tipada y flags seguros de fidelizacion [Javier :00]
- Propone seis ambitos restrictivos global/empresa/sucursal/programa/canal/sesion y dieciseis opciones tipadas O01-O16 con defaults false, cero, null o conjunto vacio para puntos, promociones, resenas, posventa, moderacion y consentimiento.
- Define doce reglas fail-closed de precedencia, tenant A/B, JWT/RBAC, revocacion, EmpresaId=NULL, idempotencia y cache mas doce pares P/N FUTUROS_NO_EJECUTADOS; no crea flags runtime, endpoints, MySQL, mensajes o campañas, pruebas NO_MEDIDO.
- Una unidad R0 M02.04 + CHANGELOG en dev, candado V17 fila 1343 ID 2000001343; A0-A4 sujetos a Scope Lock/V5 del SHA, A5 independiente, BITACORA/readback; rollback revert Git normal, sin QA/main/PROD ni secretos.

## 2026-11-15 — V17 C09-02.M02.03 persistencia y fixtures conceptuales fidelizacion [Javier :10]
- Contrato R0 documental de persistencia futura: entidades tenant-scoped, doce indices propuestos I01-I12, ocho etapas aditivas/reversibles y doce pares F01-F12 positivos/negativos FUTUROS_NO_EJECUTADOS.
- Incluye ledger separado de dinero/credito, idempotencia, antifraude, canje CAS, resenas/moderacion, consentimiento/revocacion y aislamiento A/B/legacy NULL; ningun esquema, migracion, API, test MySQL ni modulo runtime ejecutado, metricas NO_MEDIDO.
- V17 candado SQV17_C09_02_M02_03 fila 1342; A0-A4 documentales sujetos a Scope Lock/V5 SUCCESS causal, PLAN/BITACORA/readback y A5 independiente Javier. Sin QA/main/PROD; rollback git revert.

## 2026-11-15 — V17 C13-02.M02.05 contratos de eventos accesibilidad [Javier :50]
- Define 12 eventos conceptuales accesibilidad, 18 invariantes de tenant/PII/idempotencia/correlación/orden y 12 pares P/N FUTUROS_NO_EJECUTADOS, con envelope y política de replay/TTL; ningún emisor/consumidor runtime instalado.
- No crea webhooks, workers, broker, API, MySQL, Angular ni eventos entre Tasks; no certifica WCAG, JWT, integración o navegador en el alcance R0 documental. Seguridad y pruebas reales exigibles en futura R2.
- V17 lock SQV17_C13_02_M02_05 fila 1920, un scope + CHANGELOG autocontenido con gates Scope Lock/V5 exact SHA y A5 otra Task Javier; rollback git revert normal, sin QA/main/PROD.

## 2026-11-15 — V17 C09-02.M02.02 contratos API/DTO conceptuales de fidelizacion [Javier :00]
- Define ocho operaciones logicas O01-O08, ocho DTO D01-D08, doce errores seguros X01-X12 y dieciseis validaciones V01-V16 para puntos, promociones, resenas, moderacion, posventa y consentimiento, sin publicar endpoints.
- Propone doce pares P/N FUTUROS_NO_EJECUTADOS para empresa A/B, revocacion JWT/RBAC, legacy NULL, canje concurrente, replay, PII, moderacion, opt-out y errores; ninguna prueba HTTP/MySQL/Playwright real, metricas NO_MEDIDO.
- R0 documental con un solo scope C09-02.M02.02 y CHANGELOG en dev, candado V17 fila 1341 ID 2000001341; A0-A4 por Scope Lock/V5 exact-SHA y BITACORA/readback; A5 independiente. Sin datos, cobros, mensajes, QA/main/PROD; rollback git revert.

## 2026-11-15 — V17 C13-02.M02.04 configuración segura accesibilidad [Javier :50]
- Contrato R0 con 24 opciones tipadas propuestas, precedencia PLATFORM/TENANT/MODULE/SURFACE/USER, 16 invariantes, seis P y seis N FUTUROS_NO_EJECUTADOS; no se instalaron flags o controles ejecutables.
- Seguridad JWT/RBAC/tenant A/B y revocación no pueden degradarse por UI o valores de menor ámbito; no se hicieron tests Angular/Playwright/MySQL ni certificación WCAG, únicamente definición de obligaciones futuras.
- V17 lock SQV17_C13_02_M02_04 fila 1919, commit dev con Scope Lock/V5 necesarios exact SHA; A0–A4 documental y A5 distinta Task Javier para posterior; rollback git revert normal, sin QA/main/PROD.

## 2026-11-15 — V17 C09-02.M02.01 modelo conceptual de fidelizacion y reputacion [Javier :00]
- Define E01-E16 entidades conceptuales, relaciones tenant-bound e INV01-INV20 para programa, puntos, promocion, antifraude, resenas, moderacion, apelacion, posventa, consentimiento y auditoria; Cliente.EmpresaId=NULL no demuestra ownership.
- Propone doce pares P/N FUTUROS_NO_EJECUTADOS para tenant A/B, JWT/RBAC, revocacion, replay, canje concurrente, PII, saneamiento y opt-out; runtime xUnit/MySQL/API/Angular/Playwright NO_APLICA al diff exclusivamente Markdown, pruebas reales NO_MEDIDO.
- Un solo scope M02.01 + CHANGELOG en dev, V17 lock fila 1340 ID 2000001340; A0-A4 documentales requieren Scope Lock/V5 exact-SHA y readback institucional; A5 por Task Javier distinta. Sin ledger, migraciones, puntos, campanas, secretos, QA/main/PROD; rollback git revert.

## 2026-11-15 — V17 C13-02.M02.03 persistencia conceptual accesibilidad [Javier :50]
- Documenta 12 registros/constraints, 12 claves e índices, plan MIG-01–06 de evolución y reversa, fixtures TENANT_A/TENANT_B, 12 pares de casos FUTUROS_NO_EJECUTADOS de aislamiento, auditoría, MySQL e idempotencia, sin crear esquema ni clases EF.
- No hay migración, backend, Angular, API, navegador, JWT o MySQL ejecutados en esta unidad R0; sus pruebas son NO_APLICA aquí y obligatorias R2 cuando haya cambios runtime. No declarar WCAG ni liberar M03/M04 por inferencia.
- V17 candado SQV17_C13_02_M02_03 PLAN fila 1918; commit exact SHA, Scope Lock/V5 y A0–A4 documentales a confirmar, A5 posterior por otra Task Javier; rollback git revert normal, sin QA/main/PROD.

## 2026-11-15 — V17 C13-02.M02.02 contratos accesibles API/DTO y errores [Javier :10]
- Define ocho operaciones O01–O08 y ocho DTO D01–D08 CONCEPTUALES, doce errores seguros X01–X12 y dieciseis validaciones V01–V16 para teclado/foco/estados/routing y fronteras API sin crear codigo, rutas ni CSS.
- Propone doce pares P/N FUTUROS_NO_EJECUTADOS, con permisos JWT/RBAC y tenant A/B resueltos en backend, revocacion, mensajes y reflow 320px, sin certificar WCAG, HTTP, browser o pruebas reales.
- R0 solo documento docs/v5 y CHANGELOG en dev, candado V17 fila 1917 ID 2000001917, A0–A4 pendientes de Scope Lock/V5 exact-SHA y A5 independiente; sin cambios UI/API/DB, QA/main/PROD ni secretos.

## 2026-11-15 — V17 C13-02.M02.01 modelo conceptual de accesibilidad [Javier :50]
- Define 16 entidades conceptuales, 20 invariantes de ownership/tenant A-B/teclado/foco/contraste/privacidad y 12 pares de escenarios FUTUROS_NO_EJECUTADOS para contrato documental M02 de accesibilidad; no modifica UI/API/DB.
- Conserva autoridad WCAG NO_CERTIFICADA, tests Playwright/MySQL/JWT NO_APLICA al diff R0, referencias a M01.01–06 y A5 independiente previo de C13-02.M01.06; cada sucesora requiere gates dinámicos reales.
- Exclusividad V17 candado SQV17_C13_02_M02_01 fila 1916; un alcance, SHA causal y Scope Lock/V5 pendientes de comprobación antes de LISTO_DEV; A5 propia a Task Javier distinta. Rollback git revert normal, sin QA/main/PROD.

## 2026-11-15 — V17 C09-01.M02.06 contrato terminal M02 de CRM universal [Javier :40]
- Contrato de salida R0 documental con ocho predecesores/grupos A5, 16 gates, 12 pares P/N FUTUROS_NO_EJECUTADOS y ocho reversas para CRM multiempresa; sin implementar API, bases, migraciones, campañas ni flags ejecutables.
- Tenant A/B, JWT/RBAC/revocación, Cliente.EmpresaId=NULL legado, consentimiento/PII, CAS/idempotencia, consultas y MySQL efímero quedan como exigencias técnicas futuras con pruebas reales; ninguna aprobación M03/QA/main/PROD por inferencia.
- Un scope C09-01.M02.06 más CHANGELOG, V17 lock fila 1309 y checks exact-SHA Scope Lock/V5; LISTO_DEV A0–A4 documental requiere A5 independiente posterior.

## 2026-11-15 — V17 C07-02.M02.03 persistencia y fixtures conceptuales storefront [Javier :10]
- Define 12 agrupaciones de persistencia propuestas D01-D12 y 12 índices/uniques candidatos I01-I12 con aislamiento por empresa, host confiable y separación de carrito, cotización, pedido, reserva y PSP; ninguna tabla, migración o EF creada.
- Propone 8 fases reversibles M01-M08 y 12 pares de fixtures sintéticos F01-F12 FUTUROS_NO_EJECUTADOS con fail-closed tenant A/B, idempotencia, JWT/RBAC, HMAC/replay, stock e inventario; 0 tests MySQL/xUnit/Playwright ejecutados y métricas NO_MEDIDO.
- Entrega R0 documental únicamente este Markdown y CHANGELOG en dev, candado V17 fila 1054 id 2000001054, A0-A4 contra SHA y A5 independiente pendiente; sin API, cobros, Aiven, secretos, QA/main/PROD; rollback git revert.

## 2026-11-15 — V17 C07-02.M02.05 eventos conceptuales e idempotencia de storefront/checkout [Javier :40]
- Contrato R0 de doce eventos E01–E12 INERTES, catorce campos de sobre, catorce invariantes de idempotencia/correlación y doce pares positivos/negativos FUTUROS_NO_EJECUTADOS; no introduce API, colas, workers o mensajería real.
- Tenant A/B, host, JWT/RBAC/revocación, HMAC/nonce/replay PSP, orden/stock, TTL, CAS e integridad financiera quedan fail-closed; validar checkout jamás equivale a cobrar o reservar.
- Un solo documento C07-02.M02.05 y sección autocontenida de CHANGELOG; V17 named range fila 1056 y A0–A4 por SHA, A5 independiente; sin credenciales, migraciones, QA/main/PROD.

## 2026-11-15 — V17 C09-02.M01.06 gates y liberacion condicional fidelizacion [Javier :10]
- Establece G01–G16 como puertas causales, 12 pares P/N FUTUROS_NO_EJECUTADOS, fuentes M01.01–05 con blobs y condiciones de no-regresion para tenant A/B, ledger, antifraude, moderacion, consentimiento y posventa.
- Separa puntos de CreditoCliente/Venta y saldo monetario, preserva EmpresaId=NULL sin ownership, A5 independiente y pruebas xUnit/MySQL 8.4/Angular/Playwright solo en etapas de codigo; cero runtime o campanas habilitados.
- Entrega R0 limitada a este documento y CHANGELOG, claim V17 fila 1339/candado 2000001339, A0–A4 documentales sujetos a Scope Lock/V5 exact-SHA, BITACORA/readback y rollback git revert; sin QA/main/PROD.

## 2026-11-15 — V17 CI_REPAIR C07-02.M02.04 identificador de flag compatible con Scope Lock [Javier :40]
- Corrige únicamente la nomenclatura propuesta de F05 a `StockReservation` para respetar el verificador de identidad del repositorio; no altera la semántica, defaults `OFF`, autorización, tenant, PSP, datos o runtime.
- Fallo Scope Lock reproducido en el SHA inicial 6b2c7a5c; reparación dirigida y reevaluación de CI por nuevo SHA, sin deshabilitar reglas, verificadores ni pruebas; A5 sigue independiente.

## 2026-11-15 — V17 C09-01.M02.04 CRM universal: configuración tipada y flags conceptuales [Javier :30]
- Define 16 opciones tipadas futuras O01–O16 con defaults fail-closed, 12 reglas R01–R12 de precedencia global/tenant/sucursal y 12 pares P/N FUTUROS_NO_EJECUTADOS; no crea flags ejecutables, endpoints, migraciones ni proveedores.
- Preserva JWT/RBAC y tenant A/B, Cliente.EmpresaId=NULL legado, consentimiento/opt-out, idempotencia, revocación, CAS, PII, límites y reversión sin habilitar contactos/export por configuración.
- Una sola microtarea R0, docs/v5 y CHANGELOG en dev, candado V17 fila 1307; A0–A4 documentales sujetos a CI exact-SHA/readback y A5 independiente; sin QA/main/PROD, secretos o datos reales.

## 2026-11-15 — V17 C07-02.M02.04 configuración tipada y flags seguros de storefront/checkout [Javier :40]
- Define contrato futuro R0 para 6 ámbitos restrictivos, 16 campos tipados C01–C16, 12 flags inertes F01–F12, precedencias fail-closed y 12 pares positivos/negativos FUTUROS_NO_EJECUTADOS, sin implementar configuración ni runtime.
- Aislamiento Empresa A/B y host confiable, JWT/RBAC, HMAC/replay, protección PSP, stock/IVA y PII; cobro/pedido/reserva deshabilitados por defecto, A5 independiente; pruebas runtime NO_MEDIDO.
- Un solo scope C07-02.M02.04 en docs/v5 y CHANGELOG, candado V17 fila 1055; Scope Lock/V5 por SHA y BITACORA/readback obligatorios, sin MySQL/Angular/API, QA/main/PROD, secretos o pagos reales.

## 2026-11-15 — V17 C07-02.M02.02 contratos API conceptuales storefront [Javier :20]
- Define 8 operaciones, 8 DTO, 12 errores, 16 validaciones y 12 pares P/N FUTUROS_NO_EJECUTADOS para host, catálogo, stock, cotización, checkout, HMAC/PSP y autorización tenant A/B.
- No crea endpoints, pagos, pedidos, reservas, migraciones ni secretos; contratos R0 y D01-D08 pendientes; A0-A4 documentales y A5 independiente, sin afirmar ejecución HTTP/MySQL/Playwright.
- Una sola unidad M02.02 y dos Markdown, candado V17 fila 1053, fast-forward CAS, Scope Lock/V5 exact-SHA y BITACORA/readback; rollback git revert, no QA/main/PROD.

## 2026-11-15 — V17 C07-02.M02.01 storefront y checkout: modelo conceptual y ownership [Javier :20]
- Documenta 16 conceptos E01-E16, 20 invariantes INV01-INV20, 12 pares P/N FUTUROS_NO_EJECUTADOS y 12 relaciones sobre tenant/host, catálogo, stock, carrito, checkout, orden, HMAC y PSP sin implementar recursos.
- Conserva rechazo de host/tenant B, firma/replay, costos y PII; validar checkout no constituye pedido/pago. R0 documental, A0-A4 exact-SHA y A5 independiente, D01-D08 pendientes.
- Un solo scope M02.01: docs/v5 y CHANGELOG, candado V17 fila 1052, Scope Lock/V5 y BITACORA/readback; sin API/EF/MySQL/Angular/PSP, QA/main/PROD, secretos ni pagos; rollback git revert.

## 2026-11-15 — V17 C09-01.M02.03 CRM universal: persistencia, índices y migraciones conceptuales [Javier :30]
- Define contratos lógicos multiempresa, 12 índices candidatos I01–I12, 8 etapas futuras M01–M08 de migración reversible y 12 fixtures sintéticos F01–F12 con positivos/negativos; no crea ni ejecuta EF, MySQL, SQL, pruebas o datos.
- Preserva owner de Cliente.EmpresaId=NULL legado, JWT/RBAC y tenant A/B fail-closed, idempotencia, CAS, PII, consentimiento, opt-out y reversión sin drop implícito; EXPLAIN/Category=Integration siguen NO_MEDIDO.
- Una sola microtarea R0 en docs/v5 y CHANGELOG, dev estricto, A0–A4 documentales condicionados a checks exact-SHA y readback, A5 únicamente por otra Task Javier; sin QA/main/PROD, credenciales ni datos reales.

## 2026-11-15 — V17 C09-01.M02.05 eventos conceptuales CRM [Javier :10]
- Establece 12 tipos semánticos INERTES, 14 campos mínimos de envelope, 16 rechazos futuros sobre tenant A/B, membresía, CAS, replay, PII, segmentación, consentimiento y contabilidad, sin declarar eventos o pruebas ejecutadas.
- Idempotencia futura por empresa/actor/operación/huella, auditoría mínima, opt-out prioritario y outbox sólo bajo implementación técnica autorizada y pruebas xUnit/MySQL pertinentes. Las decisiones D01–D08 siguen pendientes.
- Únicamente un Markdown de docs/v5 y esta sección de CHANGELOG en dev; A0–A4 R0 exact-SHA, A5 de otra Task Javier. No webhook, worker, API, base, secretos, QA/main/PROD.

## 2026-11-15 — V17 C09-01.M02.02 CRM universal: contratos API y validaciones conceptuales [Javier :20]
- Define 8 operaciones O01-O08, 8 DTO D01-D08, 12 errores X01-X12, 16 validaciones V01-V16 y 10 pares P/N FUTUROS_NO_EJECUTADOS de CRM universal, sin endpoints ni permisos implementados.
- Preserva JWT/RBAC/tenant A/B, Cliente.EmpresaId=NULL legado, PII, consentimiento, idempotencia y CAS fail-closed; D01-D08 abiertas; A0-A4 documentales y A5 independiente.
- Una sola microtarea M02.02, dos Markdown, candado V17 fila 1305, Scope Lock/V5 exact SHA, BITACORA/readback y rollback con git revert; sin MySQL/Angular/API reales, QA/main/PROD ni secretos.

## 2026-11-15 — V17 C09-01.M02.01 CRM universal: entidades y ownership conceptual [Javier :20]
- Especifica 16 conceptos E01-E16, 20 invariantes INV01-INV20, relaciones por Empresa y 12 pares P/N FUTUROS_NO_EJECUTADOS para Cliente, timeline, actividad, oportunidad, segmentos, consentimiento e idempotencia.
- Preserva Cliente.EmpresaId=NULL como SIN_OWNER_CONFIRMADO, JWT/RBAC y tenant A/B fail-closed; decisiones D01-D08 abiertas. No implementa tablas, endpoints, mensajería, migraciones ni pruebas runtime; A0-A4 documentales y A5 independiente exigidos.
- Un solo scope M02.01, dos archivos Markdown; Scope Lock y procedencia V5 exact-SHA, claim exclusivo V17 fila 1304, BITACORA/readback y rollback por git revert; sin QA/main/PROD ni secretos.

## 2026-11-15 — V17 C13-02.M01.06 accesibilidad: gates, rollback y liberación [Javier :10]
- Establece 16 gates bloqueantes y 12 pares de escenarios FUTUROS_NO_EJECUTADOS sobre teclado/foco, formularios, modal, contraste, reflow, lector, tenant A/B y evidencias WCAG sin atribuir certificación normativa o prueba browser.
- Cierra contrato R0 M01 con A0–A4 sujetos a Scope Lock/V5 exact-SHA y BITACORA/readback; preserva S01–S10 NO_VERIFICADOS, D01–D10 ABIERTAS y A5 de otra Task Javier antes de M02.
- Sólo documentación docs/v5 y CHANGELOG en dev. No se cambian Angular/CSS/API/MySQL/JWT/RBAC, proveedores, secretos, schedulers, QA/main/PROD; rollback con git revert aditivo.

## 2026-11-15 — V17 C09-01.M01.06 CRM universal: bloqueos, rollback y liberación [Javier :10]
- Define 16 gates de preparación CRM y 12 pares positivos/negativos FUTUROS_NO_EJECUTADOS sobre ownership de Cliente, timeline, segmentos, oportunidades, permisos, datos legados, consentimiento, export, accesibilidad y transacciones.
- Contrasta cinco documentos M01 anteriores y A5 independientes, preserva R01–R16 potenciales y D01–D08 pendientes; LISTO_DEV documental sólo A0–A4 exact-SHA y A5 ajeno antes de M02.
- Únicamente docs/v5 y CHANGELOG en dev, sin tocar Domain/API/Angular/MySQL/JWT/RBAC, PII, proveedores, workers, QA/main/PROD; revert normal auditado si falla.

## 2026-11-15 — V17 C15-05.M04.04 CI de pruebas reales de navegación [Javier :00]
- Integra la suite de cuatro Vitest al workflow existente `v5-plataforma-ui-tests.yml` con disparo por archivo y comando específico en GitHub-hosted; el anterior CI `Frontend tipado y build` compilaba pero no ejecutaba estos tests.
- Mantiene la concurrencia por SHA con cancel-in-progress=false y todos los casos previos; A4 exige nuevo SUCCESS causal de Vitest, Scope Lock, procedencia V5 y controles pertinentes, sin atribuir PASS al build solamente.
- Reparación de evidencia R1 test-only, sin cambiar UI ni permisos de JWT/RBAC, API, DB, datos reales o entornos QA/main/PROD. A5 independiente requerida.

## 2026-11-15 — V17 C07-02.M01.06 contrato de gates y rollback storefront [Javier :10]
- Define 16 puertas causales y 12 pares positivos/negativos FUTUROS_NO_EJECUTADOS para host/storeKey, tenant A/B, caché, validación, idempotencia, HMAC/replay, callbacks, privacidad, móvil y pruebas pertinentes; no certifica checkout o pagos.
- Acredita preparación M01.01–05+A5 por readback DAG/COLA y conserva D01–D08 pendientes. LISTO_DEV documental requiere Scope Lock/V5 exact-SHA, BITACORA y readback; A5 independiente distinto del autor antes de liberar M02.
- Sólo Markdown docs/v5 y CHANGELOG en dev. Sin API/Angular/EF/MySQL, provider PSP, pedidos, secreto, workers, QA/main/PROD; rollback por git revert y eventos aditivos.

## 2026-11-15 — V17 C15-05.M04.04 navegación sin mutaciones de auditoría [Javier :00]
- Añade cuatro casos reales Angular/Vitest sobre ruta privada, mensaje honesto sin datos vivos, enlace accesible de retorno, ausencia de formularios/botones/doble-submit y denegación sin administrador.
- La vista existente es solo lectura; no inventa endpoints, estado de carga, mutaciones, pagos ni JWT real. La prueba de navegador de otra unidad no sustituye este CI exact-SHA.
- R1 test-only, A4 Angular/Scope Lock/V5 y checks pertinentes aún requeridos antes de LISTO, A5 por revisor Javier distinto, sin QA/main/PROD.

## 2026-11-15 — Corrección de seguridad P2 C01-04.M03.04: membresía y permiso explícitos
- En el adaptador de lectura aislado, una lectura de origen backend no implica autorización: se exigen `MembershipActive` y `ExplicitPermissionGranted` verificadas antes de consultar expediente; sus valores reales llegan al ServerAuthority en lugar de asumir dos `true`.
- xUnit negativo para lectura confiable con membresía inactiva, permiso no concedido o ambas condiciones denegadas; conservados aislamiento tenant A/B, consulta read-only y las negativas existentes.
- Sin API/DI/MySQL/Angular ni cambios de datos productivos. No falsear pruebas CI ni A5: revisión independiente A5_FAIL sigue pendiente de repetición sobre corrección con evidencia exact-SHA; rollback git revert.

## 2026-11-15 — V17 C15-05.M04.05 reparación CI Chromium de denegación [Javier :00]
- Corrige aserción demasiado específica de alert global: el guard redirige al usuario sin privilegio antes de montar la vista; ahora exige ausencia del encabezado y controles privados, preservando el caso negativo fail-closed.
- En SHA 79e2d024, 31/32 tests browser aprobaron, uno falló por strict mode ante dos alertas legítimas; ese FAIL no se considera PASS. La corrección requiere nuevo Chromium SUCCESS exact-SHA, Scope Lock/V5 y readback.
- No cambia permisos, guard, rutas, API ni datos institucionales; sólo prueba dirigida y este CHANGELOG, A5 sigue independiente.

## 2026-11-15 — V17 C11-04.M03.01 reglas de dominio observabilidad privada [Javier :00]
- Agrega validador .NET 10 puro de señales allowlisted, correlación segura, etiquetas de baja cardinalidad, ventanas UTC, conteos y NO_MEDIDO; rechaza tenant A/B cruzado, membresía revocada y permiso denegado, sin conceder autoridad API.
- Incorpora 8 métodos xUnit con positivos, negativos y fronteras; backend/seguridad/Scope Lock/V5 exact-SHA deben finalizar SUCCESS antes de LISTO. Sin API real, JWT autenticado, MySQL, exporter, alertas, SLO ni proveedores.
- R2 fail-closed, una sola microtarea, cuatro archivos aditivos; A5 independiente, no QA/main/PROD.

## 2026-11-15 — V17 C15-05.M04.05 accesibilidad real de auditoría operativa [Javier :00]
- Incorpora cuatro pruebas Playwright/Chromium para 320–1280 px, teclado y foco, landmarks/labels/contraste WCAG AA, y negación sin rol administrador; asserts ejecutables, no escenarios inventados.
- Extiende únicamente el workflow de navegador V5 para incluir esta suite, sin desactivar controles ni cambiar concurrencia. Sesión sintética no equivale a JWT/API/tenant reales.
- R1 frontend/test/CI/documentación; A4 CI exact-SHA aún requerida antes de LISTO, A5 por Task Javier independiente. Sin datos reales, QA/main/PROD ni permisos externos.

## 2026-11-15 — V17 C10-04.M03.04 adaptador aislado de caché y jobs sin grants [Javier :50]
- Añade Infrastructure .NET10 aislado read-only con captura de operación allowlisted inmutable, tenant/actor/version scoped y reutilización de Domain/Application; nunca emite lease, queue, exportación, backup o grant R2.
- Incorpora 12 xUnit de negativos de identidad, tenant A/B, versión, permiso revocado, mutation del allowlist, service grant, payload inválido, deadline, retry, cancelación y ausencia de capacidades. Requiere CI backend/security/auth causal.
- Sin conexiones HTTP, JWT real, EF/MySQL, R2 o workers; A4 Scope Lock/V5 y A5 independiente obligatorios, sin QA/main/PROD.

## 2026-11-15 — V17 C15-03.M03.04 adaptador aislado de evidencia tenancy y seguridad [Javier :50]
- Implementa Infrastructure .NET 10 read-only que compara recurso/actor/tenant/SHA capturados, reutiliza Application+Domain y deniega cualquier acceso, persistencia, descarga, job, control plane o bypass.
- Incorpora 12 xUnit reales de preflight sin grant, tenant A/B, revocación, SHA, evidencia adulterada, branch, URL vencida, intentos de bypass/escritura y cancelación; su CI backend/seguridad/autorización debe certificar SHA causal.
- R2: no integra JWT HTTP, EF/MySQL ni cambia producción, secretos o permisos. El riesgo heredado de TenantQueryBypassGuard sin marca permanece abierto. Scope Lock/V5 y A5 independiente requeridos.

## 2026-11-15 — C15-05.M04.01 pantalla privada de auditoría operativa V5 [Javier :40]
- Introduce ruta Angular lazy `/plataforma/auditoria-operativa-v5` con `plataformaGuard` backend, pantalla read-only responsive y cuatro controles del ciclo de auditoría sin fingir estado en vivo.
- Incluye Vitest positivos/negativos de rutas, acceso y contenido; Playwright/Chromium sintético para anónimo, 403, móvil y teclado, sin confundir mocks con pruebas JWT/tenant reales.
- Cambios R1 solo frontend, test, documentación y este CHANGELOG, sin endpoints, base de datos, Claims, A5, recursos externos, QA/main/PROD. A0-A4 y CI causal necesarios para LISTO; A5 por otra Task.
- Recuperación CI P0-007.A: el catálogo fail-closed detectó el componente nuevo, se actualiza inventario aditivo 142 (134 features/8 shared) y los conteos del verificador, sin omitir el control ni relajar pruebas.
- Corrección CI sin bypass: auditoría ERP reconoce exclusivamente la nueva ruta protegida por `plataformaGuard` y Chromium V5 ejecutará las tres pruebas E2E específicas; conservar gates JWT/backend y fallos históricos.

## 2026-11-15 — V16 C11-02.M02.06 salida documental API modular [Javier :10]
- Define G01–G16 de DTO/API, identidad JWT/RBAC/tenant A/B, soporte read-only, paginación, errores seguros, EF/MySQL futuro, CI y A5; RB01–RB08 de reversión, T01–T12 futuros positivos/negativos (0 ejecutados).
- Prohíbe inferir endpoints, pruebas HTTP/MySQL, SLO, workers o promoción de contratos Markdown. No cambios en API, DB, Angular, secretos, infraestructura, QA/main/PROD.
- Entrega R0 unitaria Markdown y CHANGELOG autocontenido por FF/CAS en dev, A0–A4 con Scope Lock/V5 exact-SHA y A5 por otra Task Javier.

## 2026-11-15 — V16 C12-02.M02.06 contrato salida build CDN headers cache [Javier :10]
- Define G01–G16 de build tipado, host/proxy, CSP/CORS/TLS, tenant A/B, JWT/RBAC, cache privado y CI; RB01–RB08 de reversión y T01–T12 futuros positivos/negativos (0/12 ejecutados).
- TTFB, cache hit, CDN, DNS, costo y smoke live NO_MEDIDO/NO_VERIFICADO; ninguna configuración de DNS, proveedor, CDN, Vercel/Render o producción cambiada.
- Solo R0 Markdown y sección autocontenida CHANGELOG en dev por FF/CAS; A0–A4 Scope Lock/V5 exact-SHA, A5 de otra Task Javier; sin QA/main/PROD.

## 2026-11-15 — V16 C10-03.M02.06 salida documental condicionada de archivos [Javier :10]
- Define G01–G16 de inventario auténtico, privacidad, tenant A/B, retención, MIME, idempotencia, MySQL efímero y A5; RB01–RB08 de reversión y T01–T12 futuros positivos/negativos, 0 ejecutados.
- Histórico NO_VERIFICADO no significa cero; si cero verificado omitir pipelines; si positivo exigir manifiesto/conciliación; inconsistente bloquea cutover. Sin Cloudinary/R2/DB/API/proveedor real.
- Cambio R0 unitario Markdown y CHANGELOG en dev FF/CAS, A0–A4 con Scope Lock/V5 exact SHA y posterior A5 independiente otra Javier; sin QA/main/PROD.

## 2026-11-15 — V16 C03-04.M03.03 autorización y tenancy fail-closed incidentes [Javier :10]
- Implementa gate Application .NET 10 con fuente de evidencia verificada, SHA, ownership tenant A/B, permiso vigente, revocación, auditoría y composición M03.01–02; incluso el caso positivo exige adaptador autenticado y no concede lectura, escritura, restauración ni JWT.
- Agrega xUnit positivos y negativos de cliente/rol, tenant, SHA, usuario, sucursal, versión, revocación, errores y cancelación; ninguna llamada HTTP, MySQL o integración externa está activa. Requiere CI backend/security/auth exact-SHA.
- Un único cambio R2 en dev FF/CAS, Scope Lock/V5 y readback A0–A4; A5 pendiente de otra Task Javier; sin QA/main/PROD, secretos, DB ni deploy.

## 2026-11-15 — V16 C13-03.M02.06 contrato de salida rendimiento y ligereza [Javier :10]
- Define gates G01–G16 de budget, seguridad, tenant, accesibilidad, navegación y pruebas; T01–T12 positivos/negativos FUTUROS_NO_EJECUTADOS (0/12), RB01–RB08 de reversión.
- Mantiene SIN_RESOLVER presupuesto de bundle 650–750 KB frente a 1–2 MiB y métricas LCP/INP/CLS/p95/costos NO_MEDIDO; no se atribuye Playwright ni benchmark real.
- Únicamente R0 Markdown en dev con FF/CAS, Scope Lock/V5 exact SHA, A0–A4 y A5 independiente otra Javier, sin QA/main/PROD, MySQL ni runtime.

## 2026-11-15 — V16 C11-04.M02.06 salida contractual de observabilidad [Javier :10]
- Establece G01–G16 de autorización tenant/JWT/RBAC, privacidad, idempotencia, cardinalidad, SLI/SLO, diagnóstico, costos, CI y A5 independiente; define RB01–RB08 de contención/rollback futuro.
- Especifica T01–T12 positivos/negativos FUTUROS_NO_EJECUTADOS (0/12) y SLO/p95/p99/costos NO_MEDIDO. No activa collector, exporter, alarmas, API, MySQL, workers, browser ni proveedor.
- Cambios R0 solo Markdown + CHANGELOG_AI.md en dev, publicación secuencial FF/CAS; exigir Scope Lock/V5 completed/success exact-SHA y readback A0–A4 antes de LISTO, A5 por otra Task Javier. Sin QA/main/PROD.

## 2026-11-15 — V16 C08-01.M02.06 salida y rollback contractual de citas/agenda [Javier :50]
- Define 16 gates G01–G16 de tenancy A/B, RBAC, concurrencia de slots, idempotencia, DST, capacidad, MySQL efímero y CI causal, más ocho rutas RB01–RB08 de reversión futura.
- Diez pares T01–T10 positivos/negativos FUTUROS_NO_EJECUTADOS (0/10), métricas NO_MEDIDO; no se activan citas, calendarios, mensajes, workers, proveedores, MySQL ni API.
- Alcance R0 dev Markdown y CHANGELOG_AI.md secuencial por fast-forward/CAS, Scope Lock y procedencia V5 exact-SHA, A5 independiente de otra Task Javier; sin QA/main/PROD.

## 2026-11-15 — V16 C12-04.M03.01 guard Domain R2 para cutover MySQL privado [Javier :50]
- Implementa prevalidación pura .NET 10 fail-closed de rol/MFA/tenant A/B, SHA manifiesto, grants mínimos sin WITH GRANT OPTION, CI, evidencia de MySQL 8.4 efímero, backup/restore, lease y rollback; nunca autoriza SQL, DNS ni cutover real.
- Añade pruebas xUnit positivas, negativas y de frontera para revocación, producción/QA, doble escritor, manifest incorrecto, P0/P1, grants, reintentos y secretos. Booleanos de evidencia requieren comprobación externa real, no certifican infraestructura.
- Cuatro archivos (Domain/test/docs/v5/CHANGELOG), dev FF/CAS; A0–A4 requieren CI backend, seguridad y Scope Lock/V5 exact-SHA, A5 independiente otra Task Javier; sin QA/main/PROD ni migración real.

## 2026-11-15 — V16 C11-04.M02.05 eventos, idempotencia y correlación observabilidad [Javier :50]
- Especifica EVT01–EVT14 futuros, INV01–INV16 de aislamiento/privacidad/idempotencia, correlación causal y límites fail-closed; NO crea exporter, collector, worker, webhook, alerta, endpoint ni proveedor.
- T01–T14 positivos/negativos FUTUROS_NO_EJECUTADOS (0/14), p95, SLO y costos NO_MEDIDO; cuotas sin aprobación quedan DISABLED y tenant A/B/JWT/RBAC protegidos por servidor.
- Únicamente R0 Markdown en dev + CHANGELOG_AI.md autocontenido mediante FF/CAS, Scope Lock/V5 exact-SHA; A5 independiente otro Javier, sin QA/main/PROD.

## 2026-11-15 — V16 C13-03.M02.05 contrato documental de eventos, idempotencia y correlación [Javier :50]
- Define EVT01–EVT12 eventos conceptuales, INV01–INV14 de idempotencia/tenant, correlación causal y cuotas fail-closed; NO implementa workers, webhooks, mensajería, telemetría ni proveedores.
- T01–T12 positivos/negativos FUTUROS_NO_EJECUTADOS (0/12), LCP/INP/CLS NO_MEDIDO y conflicto de bundle 650–750 KB vs 1–2 MiB SIN_RESOLVER. Seguridad JWT/RBAC/tenant A/B prevalece.
- Alcance R0 sólo docs/v5 y CHANGELOG_AI.md, publicación dev fast-forward/CAS, Scope Lock/V5 exact-SHA pendiente de acreditación, A5 independiente otra Task Javier, sin QA/main/PROD.

## 2026-11-15 — V16 C11-04.M02.04 opciones tipadas y flags seguros observabilidad [Javier :40]
- Contrato R0 OPT01–OPT20, FL01–FL12, VAL01–VAL18 y política restrictiva por plataforma/tenant/componente; exporters, tracing remoto, alertas, captura PII/SQL y labels arbitrarios OFF, sin configuración ejecutable.
- Doce pares T01–T12 positivos/negativos FUTUROS_NO_EJECUTADOS (0/12) de RBAC, revocación, redacción, cardinalidad, política CAS, fallas y rollback; SLO, p95, retención y costos NO_MEDIDO.
- Dos Markdown en dev con fast-forward/CAS y Scope Lock/V5 exact SHA, A0–A4 R0, A5 independiente pendiente; sin cambios API, MySQL, Angular, collector, QA/main/PROD ni proveedores.

## 2026-11-15 — V16 C11-04.M02.03 persistencia conceptual observabilidad [Javier :40]
- R0 documental de P01–P12, IDX01–IDX12 y MIG01–MIG08 FUTUROS para logs, métricas, trazas, redacción de PII, SLI/SLO, auditoría scoped y costo agregado; ninguna tabla, EF/MySQL ni collector implementados.
- Doce pares F01–F12 positivos/negativos FUTUROS_NO_EJECUTADOS (0/12); JWT/RBAC/tenant A/B, revocación, cardinalidad, rollback y denegaciones previstas, con p95/SLO/costos NO_MEDIDO.
- Solo documento Markdown + CHANGELOG autocontenido por fast-forward/CAS dev; A0–A4 R0 con Scope Lock/V5 exact SHA y A5 distinta Javier pendiente; sin QA/main/PROD ni proveedores.

## 2026-11-15 — V16 C08-01.M02.05 eventos conceptuales e idempotencia agenda [Javier :40]
- Documento R0 con EV01–EV12, IDEM01–IDEM14, COR01–COR12 de cita, personal, recursos, correlación, retries y protección tenant/JWT/RBAC sin emitir mensajes ni crear infraestructura de eventos.
- Doce pares T01–T12 positivos/negativos FUTUROS_NO_EJECUTADOS (0/12), incluidos DST, 64 solicitudes concurrentes, cancelación única, CAS, replay y separación `ReservaInventario`; MySQL, API y entrega de eventos NO_MEDIDO.
- Solo Markdown de contrato y CHANGELOG autocontenido en dev por fast-forward/CAS, A0–A4 R0 con Scope Lock/V5 exact SHA, A5 de otra Task Javier, sin QA/main/PROD.

## 2026-11-15 — V16 C11-04.M02.02 contratos de API DTO y errores observabilidad [Javier :30]
- Contrato R0 con OP01–OP08, DTO01–DTO08, ERR01–ERR12, VAL01–VAL16 y T01–T10 FUTUROS_NO_EJECUTADOS para señales, correlación, SLI/SLO, roles y diagnósticos sin PII.
- Ningún endpoint .NET, token, rol, exporter, collector, dashboard, alerta, MySQL, Angular, proveedor, webhook o Tasks nuevos; 0/10 tests runtime y SLO/p95/costos NO_MEDIDO.
- Un commit serial dev de dos Markdown, Scope Lock/V5 exact SHA, BITACORA/readback; A5 independiente Javier requerida antes de gate técnico, sin QA/main/PROD.

## 2026-11-15 — V16 C08-01.M02.04 configuración y flags seguros de agenda [Javier :40]
- Contrato documental R0 de OPT01–OPT18, FL01–FL12, VAL01–VAL16 y precedencia restrictiva tenant/sucursal/recurso con defaults OFF fail-closed, JWT/RBAC/tenant A/B, versiones CAS, DST y privacidad.
- Doce pares T01–T12 FUTUROS_NO_EJECUTADOS (0/12); sin IOptions, configuración real, MySQL/EF, API, Angular, calendario, webhook o worker; 64 reservas concurrentes y métricas NO_MEDIDO.
- Solo este documento y CHANGELOG autocontenido por fast-forward/CAS dev, Scope Lock/V5 exact SHA y A0–A4 R0; A5 de otra Task Javier pendiente, sin QA/main/PROD.

## 2026-11-15 — V16 C07-03.M02.06 salida documental M02 inventario y compras [Javier :30]
- Contrato R0 de 16 gates G01–G16, 8 rollback RB01–RB08 y 12 pares P/N FUTUROS_NO_EJECUTADOS para ownership de almacenes, reservas, compras, recepciones, transferencias, costos y trazabilidad.
- Prerrequisitos M01 y M02.01–05+A5 y C07-01.M01.06+A5 confirmados por DAG dinámico; falla Scope Lock previa de M02.04 conservada y reparación comprobada, sin convertir SHA fallido en PASS.
- Sólo Markdown y CHANGELOG; 0/12 tests runtime, ninguna transacción, EF/migración MySQL, evento, worker, webhook, QA/main/PROD ni dato real modificado; A5 de otro Javier obligatoria.

## 2026-11-15 — V16 C08-01.M02.03 persistencia conceptual de citas/agenda [Javier :40]
- Contrato R0 P01–P12, IDX01–IDX12 y MIG01–MIG08 FUTUROS de sede, profesionales, recursos, ocupación, transiciones e idempotencia tenant-scoped, sin tablas, DDL ni EF/MySQL real.
- Doce pares F01–F12 positivos/negativos FUTUROS_NO_EJECUTADOS (0/12): UTC/IANA/DST, 64 intentos de doble booking, RBAC/tenant A/B, CAS, replay, revocación, rollback y separación ERP; rendimiento y concurrencia NO_MEDIDO.
- Únicamente este documento y CHANGELOG autocontenido por fast-forward/CAS en dev; Scope Lock/V5 exact SHA y A0–A4 R0, A5 de otra Task Javier, sin QA/main/PROD.

## 2026-11-15 — V16 C13-03.M02.04 opciones tipadas y flags seguros rendimiento [Javier :20]
- Contrato documental R0 de catorce opciones OPT01–OPT14, dieciséis validaciones VAL01–VAL16 y ocho fallos fail-closed FAL01–FAL08; ninguna configuración IOptions, appsettings, flag, caché o endpoint real activado.
- Diez pares T01–T10 positivos/negativos FUTUROS_NO_EJECUTADOS para tenant A/B, JWT/RBAC, caché, PII, accesibilidad y disponibilidad; LCP/INP/CLS NO_MEDIDO y conflicto de bundle 650–750 KB vs 1–2 MiB SIN_RESOLVER.
- Sólo documento y CHANGELOG Markdown autocontenido en dev por fast-forward/CAS; A0–A4 R0 sujetos a Scope Lock/V5 exact-SHA, A5 pendiente otra Task Javier, sin QA/main/PROD ni proveedor o DB real.

## 2026-11-15 — V16 C11-04.M02.01 contrato de entidades e invariantes de observabilidad [Javier :30]
- Define doce entidades E01–E12, dieciséis invariantes INV01–INV16, diez relaciones REL01–REL10 y doce pares T01–T12 FUTUROS_NO_EJECUTADOS sobre señales, correlación, roles, SLI/SLO, privacidad y alertas seguras.
- R0 documental, sin cambiar middleware .NET, exporter, collector, dashboard, alerts, MySQL, Angular, proveedores, credenciales ni Tasks; p95, error budget, SLO, costo, retención y overhead NO_MEDIDO.
- Dos archivos Markdown en dev por fast-forward/CAS con Scope Lock/V5; A5 independiente Javier distinta pendiente, sin QA/main/PROD ni liberación runtime.

## 2026-11-15 — V16 C13-03.M02.03 persistencia conceptual y fixtures de rendimiento [Javier :20]
- Especificación documental R0 de P01–P12, IDX01–IDX12 y MIG01–MIG08 FUTUROS: no se crea migración EF, índice, tabla, telemetría, endpoint o modificación de MySQL, y se mantiene intacto el aislamiento tenant.
- Doce pares F01–F12 positivos/negativos FUTUROS_NO_EJECUTADOS (0/12) de SHA, caché, retención, presupuestos, accesibilidad, rollback y privacidad; LCP/INP/CLS NO_MEDIDO y conflicto 650–750 KB vs 1–2 MiB SIN_RESOLVER.
- Sólo documento Markdown y CHANGELOG autocontenido mediante fast-forward/CAS dev; A0–A4 R0 sujetos a Scope Lock/V5 exact-SHA y readback; A5 requiere otra Task Javier, sin QA/main/PROD ni datos reales.

## 2026-11-15 — V16 C12-02.M02.03 persistencia conceptual de frontend separado [Javier :30]
- Documento R0 con P01–P12, IDX01–IDX10, MIG01–MIG08, F01–F12 y 12 pares T01–T12 FUTUROS_NO_EJECUTADOS para metadatos de release, assets, rutas, proxy, caché y rollback.
- Sin tablas, índices SQL, migraciones EF/MySQL, backend, Angular, HTTP, CDN, DNS, hosting ni datos reales; 0/12 tests runtime, LCP/INP/CLS NO_MEDIDO, conflicto de presupuesto 650–750KB vs 1–2MiB pendiente.
- Un changeset dev de dos Markdown, Scope Lock/V5 exact SHA; A5 independiente necesaria, sin QA/main/PROD ni autorización implícita de migración.

## 2026-11-15 — V16 C13-03.M02.02 API y DTO conceptuales de rendimiento [Javier :20]
- Contrato documental R0 de ocho operaciones futuras OP01–OP08, ocho DTO conceptuales, doce errores seguros ERR01–ERR12 y dieciséis reglas VAL01–VAL16 para experiencia ligera, API y privacidad, sin crear endpoints ni DTO compilados.
- Diez casos T01–T10 positivos/negativos FUTUROS_NO_EJECUTADOS; el conflicto del bundle 650–750 KB contra 1–2 MiB continúa SIN_RESOLVER y LCP/INP/CLS permanecen NO_MEDIDO; no se adjudica PASS de runtime.
- Sólo documento Markdown y CHANGELOG autocontenido en dev mediante fast-forward/CAS, A0–A4 documentales con Scope Lock/V5 exact-SHA; A5 pendiente de otra Task Javier, sin QA/main/PROD ni cambios DB, HTTP, UI o secretos.

## 2026-11-15 — V16 C13-03.M02.01 modelo conceptual de rendimiento y ligereza [Javier :20]
- Define 14 conceptos E01–E14 de evidencia, ownership, rutas, medios, caché, datos y rollback, 12 relaciones REL01–REL12 y 18 invariantes INV01–INV18; no crea entidades EF ni cambia frontend/backend.
- Diez oráculos T01–T10 positivos/negativos FUTUROS_NO_EJECUTADOS para métricas, móvil, navegación, caché, tenant A/B y accesibilidad. LCP/INP/CLS siguen NO_MEDIDO; conflicto de bundle 650–750 KB frente a 1–2 MiB permanece SIN_RESOLVER.
- Sólo dos Markdown y publicación fast-forward/CAS, A0–A4 documentales sujetos a checks Scope Lock/V5 exact-SHA; A5 independiente pendiente; sin pruebas runtime, producción, datos ni secretos.

## 2026-11-15 — V16 C08-01.M02.02 API DTO y errores seguros de citas/agenda [Javier :10]
- Define contratos R0 OP01–OP12, DTO01–DTO12, ERR01–ERR14 y VAL01–VAL18 de cita, recursos, personal, disponibilidad y cancelación con UTC/IANA/DST, A/B, JWT/RBAC y control de concurrencia.
- Doce pares T01–T12 FUTUROS_NO_EJECUTADOS; no endpoint, API real, MySQL, EF, UI, Playwright, evento, migración ni modificación de reservas ERP; métricas runtime NO_MEDIDO.
- Solo documento y sección CHANGELOG autocontenida, fast-forward/CAS en dev, Scope Lock y V5 exact SHA, BITACORA/readback y A5 distinto Javier pendiente; sin QA/main/PROD.

## 2026-11-15 — V16 C08-01.M02.01 entidades y ownership conceptual citas/agenda [Javier :10]
- Contrato documental R0 con E01–E14, INV01–INV18, REL01–REL12 para empresa, sucursal, servicio, profesional, recurso, citas, reprogramación y cancelación; separa cita de reservas de existencias.
- Doce pares T01–T12 FUTUROS_NO_EJECUTADOS sobre tenant A/B, JWT/RBAC, UTC/IANA/DST, doble booking, CAS, rollback e idempotencia; 0 tests runtime, MySQL 8.4 y concurrencia real pendientes para código R2.
- Solo documento Markdown y sección CHANGELOG autocontenida por fast-forward/CAS dev; A0–A4 documental, A5 de otra Task Javier, sin migraciones, HTTP, UI, QA/main/PROD.

## 2026-11-15 — V16 C07-03.M02.05 contratos futuros eventos, idempotencia y límites [Javier :10]
- Propone EV01–EV12, IDEM01–IDEM12, COR01–COR10 y LIM01–LIM12 para inventario, reservas, compras, transferencias, stock y costeo con tenant A/B y RBAC fail-closed; eventos NO EMITIDOS.
- Diez pares T01–T10 de pruebas FUTURAS_NO_EJECUTADAS para replay, rollback, commit causal, revocación, correlación y costos; no outbox, broker, worker, webhook, DB, endpoint ni proceso nuevo.
- Sólo documento R0 y CHANGELOG autocontenido en dev mediante fast-forward/CAS; A0–A4 Scope Lock/V5 exact SHA, A5 de otra Task Javier pendiente, sin QA/main/PROD.

## 2026-11-15 — V16 C07-03.M02.04 opciones tipadas y flags seguros inventario/compras [Javier :10]
- Contrato R0 de OPT01–OPT14, VAL01–VAL16 y R01–R08 para configuración fail-closed por tenant, sucursal, almacén y RBAC; ningún appsettings, IOptions, flag, EF, API o dato real modificado.
- Diez pares T01–T10 FUTUROS_NO_EJECUTADOS sobre A/B, permisos, revocación, rangos, CAS, caché, costos, idempotencia y fallo de proveedor; métricas runtime NO_MEDIDO.
- Solo dos Markdown y encabezado con viñetas contiguas; A0–A4 documental/Scope Lock/V5 por SHA, BITACORA/readback, A5 pendiente de otro Javier; sin QA/main/PROD.

## 2026-11-15 — V16 C07-03.M02.03 contrato documental de persistencia e índices para inventario/compras [Javier :10]
- Define P01–P12 de persistencia por tenant, IDX01–IDX12 candidatos y MIG01–MIG08 de migración futura reversible, sin crear DDL, tablas, índices, EF migration ni tocar existencias o costos reales.
- Establece F01–F12 positivos/negativos FUTUROS_NO_EJECUTADOS, con MySQL 8.4 aislado y Category=Integration exigibles solamente en implementación R2; los resultados runtime actuales son NO_MEDIDO.
- Solo dos archivos Markdown, CHANGELOG autocontenido, fast-forward/CAS dev, Scope Lock y procedencia V5 exact-SHA, BITACORA y readback; A5 otro Javier pendiente, sin QA/main/PROD.

## 2026-11-15 — V16 C12-02.M02.05 contratos inertes de eventos e idempotencia [Javier :50]
- Propone EVT01–EVT12, ID01–ID12 y diez oráculos T01–T10 positivos/negativos FUTUROS_NO_EJECUTADOS para build, hashes, versión, caché/host, smoke y rollback; sin dispatcher, webhook ni workers reales.
- Preserva JWT/RBAC, tenant A/B, privacidad, CSP/CORS y controles de SHA; D01–D10 de proveedor/CDN siguen pendientes, ningún DNS/host/pago/PROD modificado.
- Sólo docs/v5 y sección CHANGELOG autocontenida, fast-forward/CAS dev y Scope Lock/V5 exact-SHA; A0–A4 R0, A5 distinto Javier pendiente, sin QA/main/PROD.

## 2026-11-15 — V5 C08-01.M01.06: bloqueos y rollback documental de reservas y agenda [Javier :50]
- Define 16 gates y diez pares positivos/negativos FUTUROS_NO_EJECUTADOS sobre recursos, JWT/RBAC, tenant A/B, doble reserva MySQL, DST UTC/IANA, estados e idempotencia.
- Separa el cierre documental M01 de pruebas runtime, decisiones D01–D08 pendientes, A5 ajeno antes de M02 y regresiones reales de inventario/calendario; sin activar API, migrations, eventos o proveedores.
- Un documento docs/v5 y una sección CHANGELOG juntos, fast-forward/CAS dev, Scope Lock/V5 exact SHA, BITACORA/readback; no QA/main/PROD ni cuentas externas.

## 2026-11-15 — V5 C11-04.M01.06: barreras y rollback documental de observabilidad [Javier :50]
- Establece 16 gates de M01, condición de A5 ajeno antes de M02, y ocho pasos de rollback con conservación de JWT/RBAC, tenant A/B y BITACORA.
- Distingue criterios P01–P10/N01–N10 FUTUROS_NO_EJECUTADOS, decisiones D01–D08 PENDIENTES y SLO/dashboards NO_MEDIDO; no se implementan exporters, alertas ni servicios.
- Sólo nuevo docs/v5 y sección CHANGELOG autocontenida en dev; A0–A4 R0 y Scope Lock/V5 exact-SHA requeridos, A5 ajeno pendiente; sin QA/main/PROD.

## 2026-11-15 — V16 C07-03.M02.02 API DTO errores inventario y compras [Javier :40]
- Contrato R0 documental de 12 operaciones OP01-OP12, 12 DTO DTO01-DTO12, 12 errores ERR01-ERR12 y 16 validaciones VAL01-VAL16 con propiedad de almacenes, stock, compra, costo y tenancy.
- 12 pares T01-T12 FUTUROS_NO_EJECUTADOS; no endpoints nuevos, HTTP, EF/MySQL, movimientos, autorizaciones, migraciones ni QA/main/PROD.
- A0-A4 documentales con Scope Lock/V5 exact SHA y BITACORA/readback; A5 de otra Task Javier, decisiones D01-D08 sin certificar.

## 2026-11-15 — V16 C07-03.M02.01 entidades e invariantes de inventario/compras [Javier :40]
- Contrato R0 documental de 12 relaciones de entidades E01-E12, 16 invariantes INV01-INV16 y 10 relaciones REL01-REL10 para sucursal, almacen, reserva, compra, recepcion, transferencia, conteo, costeo y reconciliacion.
- 12 pares T01-T12 FUTUROS_NO_EJECUTADOS con negativos tenant A/B, legacy NULL, RBAC, stock doble, idempotencia, MySQL/concurrencia y censura de costo; 0 tests ejecutados.
- Solo 2 Markdown; sin transacciones, migraciones, backend/frontend, datos reales, QA/main/PROD; A0-A4 Scope Lock/V5 por SHA y A5 distinta pendiente.

## 2026-11-15 — V16 C05-03.M02.05 eventos e idempotencia builder [Javier :40]
- Contrato R0 de doce intenciones EV01-EV12, doce defensas IDEM01-IDEM12, ocho límites LIM01-LIM08, correlación y CAS fail-closed por tenant y recurso.
- Doce pares P/N T01-T12 FUTUROS_NO_EJECUTADOS; no worker, bus, outbox, webhook, API, MySQL, flags ni publicación runtime; 0 eventos ejecutados.
- A0-A4 documentales Scope Lock/V5 exact SHA y BITACORA/readback; A5 independiente pendiente y D01-D08/QA/main/PROD sin liberar.

## 2026-11-15 — V16 C05-03.M02.04 configuración tipada y feature flags builder [Javier :40]
- Contrato R0 documental: 16 campos CFG01-CFG16, 12 flags FF01-FF12 propuestos OFF y 8 precedencias PREC01-PREC08 de denegación por defecto/tenant/RBAC/revocación.
- Doce pares T01-T12 FUTUROS_NO_EJECUTADOS; no CONFIG, appsettings, flags reales, .NET, Angular, MySQL, API, secretos, workers, QA/main/PROD.
- A0-A4 Scope Lock/V5 exact SHA y BITACORA readback; A5 independiente por otra Task Javier y decisiones D01-D08 pendientes.

## 2026-11-15 — V16 C12-02.M02.04 configuracion tipada y flags frontend separado [Javier :30]
- Contrato R0 con catorce opciones CFG01-CFG14, seis ámbitos de confianza, catorce validaciones V01-V14 y diez pares T01-T10 FUTUROS_NO_EJECUTADOS.
- Sin configuración aplicada, DNS, CDN, frontend runtime, HTTP, JWT/RBAC modificados, MySQL, credenciales ni proveedores.
- V15 A0-A4 y Scope Lock/V5 exact SHA, A5 independiente para otra Task Javier; decisiones D01-D10 pendientes, sin QA/main/PROD.

## 2026-11-15 — V16 C12-02.M02.02 contratos API DTO errores frontend separado [Javier :30]
- Contrato R0 de ocho operaciones OP01-OP08, ocho DTO D01-D08, doce errores ERR01-ERR12 y catorce validaciones V01-V14.
- Diez pares T01-T10 FUTUROS_NO_EJECUTADOS; sin endpoints, TypeScript/C#, migraciones, CDN, DNS, host bindings ni publicación.
- A0-A4 Scope Lock/V5 por SHA y BITACORA; A5 de otra Task Javier, D01-D10 pendientes. Sin QA/main/PROD.

## 2026-11-15 — V16 C05-03.M02.03 persistencia e indices conceptuales builder [Javier :40]
- Contrato R0 documental: 12 estructuras P01-P12, 10 indices IDX01-IDX10 y 6 fases futuras MIG01-MIG06 con ownership por tenant, CAS, revocacion, host y migrador unico.
- 12 fixtures FX01-FX12 con pares positivos/negativos FUTUROS_NO_EJECUTADOS (0 ejecutados); no EF/MySQL, DDL, API, builder runtime, migracion ni QA/main/PROD.
- A0-A4 requieren Scope Lock/V5 exact-SHA y BITACORA/readback; A5 de otra Task Javier, decisiones D01-D08 y gates M03 pendientes.

## 2026-11-15 — V16 C12-02.M02.01 entidades y ownership frontend separado [Javier :30]
- Modelo documental R0: doce entidades E01-E12, dieciséis invariantes INV01-INV16 y diez pares positivos/negativos T01-T10 FUTUROS_NO_EJECUTADOS.
- Rutas, bundle, cache, SPA, manifest y tenant conservan dueño y límites; edge/CDN no sustituye JWT/RBAC backend; no servicios desplegados ni DNS cambiados.
- V15 A0-A4 Scope Lock/V5 por SHA y readback; A5 independiente requerido; D01-D10 de M01 siguen PENDIENTES. Sin QA/main/PROD.

## 2026-11-15 — V16 C13-03.M01.06 gates y rollback de rendimiento/ligereza [Javier :30]
- Contrato documental R0: consolidacion de M01.01-05+A5, 16 barreras G01-G16, ocho acciones RB01-RB08 y diez pares P/N T01-T10 FUTUROS_NO_EJECUTADOS.
- Budget actual 650-750 KB frente a propuesta 1-2 MiB permanece discrepancia ABIERTA; LCP/INP/CLS y latencias NO_MEDIDO hasta CI/browser y dispositivos reproducibles.
- A0-A4 documentales y A5 independiente; no cambio de Angular/API, MySQL, cache runtime, infraestructura, dominios, secretos, QA/main/PROD.

## 2026-11-15 — V16 C05-03.M02.02 API DTO y errores contractuales builder [Javier :20]
- Contrato R0 de ocho operaciones OP01-OP08, ocho DTO D01-D08, doce errores ERR01-ERR12 y catorce validaciones V01-V14 fail-closed por tenant/rol/version.
- Diez pares P/N T01-T10 FUTUROS_NO_EJECUTADOS; sin rutas API reales, Angular, SQL/EF, migraciones, HTML/JS libre, workers, webhooks, proveedores o publicacion.
- V15 A0-A4 Scope Lock/V5 exact SHA y readback; A5 independiente por otra Task Javier, decisiones y gates M03 pendientes. Sin QA/main/PROD.

## 2026-11-15 — V16 C05-03.M02.01 entidades e invariantes de builder gobernado [Javier :20]
- Modelo R0 con doce entidades conceptuales E01-E12, ownership por tenant/pagina/revision y dieciseis invariantes INV01-INV16 para controles backend fail-closed.
- Diez escenarios positivos/negativos T01-T10 FUTUROS_NO_EJECUTADOS; sin builder ejecutable, Angular, HTML/JS libre, API, EF/MySQL, migraciones, flags ni publicacion.
- Single-active DEV, A0-A4 Scope Lock/V5 exact SHA y readback; A5 de otra Task Javier, decisiones D01-D08 y puertas M03 pendientes. Sin QA/main/PROD.

## 2026-11-15 — V16 C12-04.M02.06 contrato salida M02 MySQL privado y cutover [Javier :20]
- Documento R0 con antecedentes M01/M02, dieciseis gates G01-G16 de tenancy, seguridad, migrador unico, permisos MySQL y consistencia transaccional.
- Ocho pasos RB01-RB08 y diez pares T01-T10 FUTUROS_NO_EJECUTADOS; no migracion EF, MySQL, restore, DNS, secrets, workers, servicios ni cutover real.
- V15 A0-A4 Scope Lock/V5 y BITACORA por SHA causal; A5 por Task Javier distinta, M03 R2 requiere MySQL 8.4 efimero y autorizacion futura. Sin QA/main/PROD.

## 2026-11-15 — V16 C07-01.M02.06 contrato salida M02 catalogo universal [Javier :20]
- Consolidado contrato R0 con seis antecedentes, dieciseis gates G01-G16 de seguridad tenant/JWT/RBAC, precios/stock y compatibilidad ERP.
- Ocho pasos RB01-RB08 y diez pares T01-T10 FUTUROS_NO_EJECUTADOS, sin API, DB, Angular, migracion, workers, eventos, flags ni despliegues.
- Single-active DEV, A0-A4 por Scope Lock/V5 exact SHA y readback; A5 independiente, DAG antes de M03 y sin QA/main/PROD.

## 2026-11-15 — V16 C06-01.M02.06 contrato salida landing SaaS [Javier :50]
- Contrato R0 documental con 16 puertas G, ocho rollbacks RB y ocho pares positivos/negativos T FUTUROS_NO_EJECUTADOS para planes, demo, legal, SEO, contacto y tenant A/B.
- Mantiene D01-D08 PENDIENTES, JWT/RBAC backend, consentimiento, privacidad, idempotencia y separación de entornos; no implementa Angular, API, MySQL, correo, eventos, proveedores o deploy.
- Entrega SINGLE_ACTIVE_DEV por FF/CAS, Scope Lock STRICT, V5 provenance y readbacks A0-A4 por SHA; A5 otra Task Javier. Sin QA/main/PROD, DNS ni secretos.

## 2026-11-15 — V16 C02-02.M03.02 Application preflight shared/dedicated [Javier :50]
- Implementado contrato de lector de evidencia backend, ownership tenant y catálogo de binding para Domain de placement; el veredicto nunca autoriza conexión, migración ni cutover.
- Incorporadas doce pruebas xUnit con escenarios de JWT/RBAC, A/B, DSN, snapshot, CAS, restore, idempotencia y cancelación; CI real pendiente de acreditar.
- Riesgo V15 R2 y SINGLE_ACTIVE_DEV, A0–A4 exact-SHA y A5 independiente; sin EF/MySQL/HTTP/UI, DNS, QA/main/PROD ni secretos.

## 2026-11-15 — V16 C02-01.M03.02 Application tenant resolution preflight [Javier :10]
- Servicio Application fail-closed no autorizante para tenant server-side, host confiable, recurso/sucursal A/B, revocación, membership y JWT verificados por backend; no permite host/rol/tenant/fallback del cliente.
- Doce pruebas xUnit adversariales con switch A→B, host spoof, store unavailable, tenant mismatch, revocación, sucursal B y datos ausentes; CI backend Release/seguridad/autorización exact SHA obligatoria.
- Solo preflight, sin middleware/HTTP, registros de host, EF/MySQL, secretos, QA/main/PROD ni grants; V15 R2, Scope Lock STRICT, A5 independiente, rollback git revert.

## 2026-11-15 — V16 C08-04.M03.02 Application gate sectorial [Javier :50]
- Creado lector de evidencia server-side de actor, tenant, permisos y módulo que alimenta reglas Domain de C08-04.M03.01; preflight nunca autoriza cambios, grants, EF ni acceso.
- Incluidas 14 pruebas xUnit con tenant A/B, revocación, permisos, fuente SHA, política, versión, lifecycle, cancelación y errores; CI real debe aprobar antes de LISTO.
- Riesgo V15 R2, entrega secuencial single-active; A0–A4 y A5 independiente, Scope Lock/V5, rollback normal; sin QA/main/PROD ni integraciones reales.

## 2026-11-15 — V16 C01-02.M03.02 preflight Application de capabilities [Javier :10]
- Nuevo C0102CapabilityApplicationPreflight no autorizante que verifica tenant, sucursal y rechaza políticas, grants, escritura y exportación del cliente antes de invocar Domain; todas las propiedades de acceso permanecen false.
- Doce tests xUnit positivos/negativos de contexto ausente, tenant B, branch, RBAC relacional, revocación, permisos y allowlist; exigir CI backend/seguridad/autorización exact SHA, no documentar PASS sin resultado real.
- R2 Application-only sin API, EF/MySQL, Angular, jobs runtime, secretos o producción; Scope Lock STRICT, A0–A4 y A5 independiente, rollback git revert.

## 2026-11-15 — V16 C05-01.M03.01 Domain de tokens de design system [Javier :50]
- Implementada prevalidacion pura ASP.NET Core 10 Domain de tokens tipados y allowlisted, contexto backend tenant A/B, RBAC, revocacion, epoch y revision optimista; nunca concede autorizacion ni actualiza temas.
- Incluidas 15 pruebas xUnit con asserts positivos, negativos, limites pixel, XSS/CSS, token desconocido, cross-tenant, revision, revocacion e idempotencia GUID; pruebas CI se acreditan solo con job real SUCCESS.
- Riesgo V15 R2; A0-A4 y A5 independiente, Scope Lock STRICT y procedencia V5 exigidos en SHA; sin cambios a EF/MySQL, HTTP, Angular, UI, QA/main/PROD o despliegue.

## 2026-11-15 — V16 C12-02.M01.06 bloqueos y rollback documental de frontend separado [Javier :10]
- Define 16 bloqueos BL01–BL16, ocho procedimientos REV01–REV08, doce gates LG01–LG12 y ocho parejas T01–T08 FUTUROS_NO_EJECUTADOS para Angular, proxy, cache, headers, SEO, accesibilidad y rollback.
- D01–D10 permanecen PENDIENTES; preserva JWT/RBAC/tenant A/B, ERP privado, Scope Lock y proveedores declarados; no mueve hosting, DNS, CDN, TLS, Vercel, Render, MySQL ni despliega nada.
- V15 R0 contractual, A0–A4 documentales y checks exact SHA, A5 independiente de otra Task Javier; rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V16 C06-01.M02.05 contrato de eventos e idempotencia del sitio publico [Javier :40]
- Define doce eventos conceptuales EV01–EV12, catorce invariantes ID01–ID14 y un sobre de correlacion opaca para landing, planes, contacto, demo, legal y SEO.
- Especifica diez pares P/N FUTUROS_NO_EJECUTADOS, aislamiento tenant A/B, JWT/RBAC, consentimiento, deduplicacion, reintentos acotados y privacidad sin API, workers, MySQL, SMTP ni dispatcher ejecutable.
- Publicacion individual R0 en dev bajo Scope Lock STRICT, A0-A4 por SHA real y A5 solo de otra Task Javier; rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V16 C06-01.M02.03 persistencia conceptual de landing SaaS [Javier :10]
- Define 12 familias P01–P12, 12 índices IDX01–IDX12, 12 etapas MIG01–MIG12 y diez pares FX01–FX10 FUTUROS_NO_EJECUTADOS para publicaciones, SEO, contacto, consentimiento y demo.
- Mantiene CAS e historia de versiones, aislamiento tenant A/B, fuente comercial/legal aprobada, retención PII y autoridad JWT/RBAC backend. No hay tablas EF, DDL, MySQL, API, Angular, SMTP ni migración real.
- Alcance V16/V15 R2 documental, A0–A4 con Scope Lock/V5 exact SHA, A5 independiente de otra Task Javier; rollback git revert; sin QA/main/PROD.

## 2026-11-15 — V16 C06-01.M02.04 configuracion tipada y flags inertes de landing SaaS [Javier :30]
- Define catorce opciones conceptuales OPT01-OPT14, dieciseis validaciones V01-V16 y ocho pares P/N FUTUROS_NO_EJECUTADOS de rutas, planes, demo, contacto, legales, SEO, tenant y accesibilidad.
- No escribe flags efectivos, frontend, backend, API, EF/MySQL, workers, SMTP, catalogos de precios, secretos, QA/main/PROD ni servicios; JWT/RBAC sigue en servidor.
- Entrega V15 R0 individual, Scope Lock STRICT y procedencia V5 exact-SHA, readback y A5 pendiente de otra Task Javier; rollback git revert documental.

## 2026-11-15 — V16 C06-01.M02.02 contrato API/DTO seguro para landing SaaS [Javier :40]
- Define ocho operaciones logicas y ocho DTO conceptuales, doce errores seguros, dieciseis validaciones y diez pares positivos/negativos FUTUROS_NO_EJECUTADOS para propuesta, planes, demo, contacto, legal y SEO.
- Protege rutas privadas/ERP, JWT/RBAC/tenant A/B, consentimiento, idempotencia y PII; D01-D08 siguen PENDIENTES. No crea API HTTP, Angular, MySQL/EF, SMTP, indexacion ni runtime.
- Entrega unica V15 R0 en dev con Scope Lock STRICT, A0-A4 documentales y CI pertinente SHA; A5 exige otra Task Javier, git revert sin QA/main/PROD.

## 2026-11-15 — V16 C06-01.M02.01 entidades y ownership conceptual de landing SaaS [Javier :10]
- Define 14 entidades E01–E14, 18 invariantes INV01–INV18 y diez parejas P/N FUTUROS_NO_EJECUTADOS para contenido público, planes, legal, SEO, contacto y demo sin cruzar tenants.
- Preserva autoridad comercial/legal D01–D08 PENDIENTES, backend JWT/RBAC/tenant A/B, privacidad, CAS, consentimiento, SEO allowlisted y guards ERP; solo diseño documental sin Angular, .NET runtime, EF/MySQL, rutas, SMTP o publicación.
- Entrega individual V16/V15 R0 con A0–A4 documentales y checks Scope Lock/V5 exact-SHA, A5 pendiente de otra Task Javier, rollback git revert; QA/main/PROD no autorizados.

## 2026-11-15 — V16 C05-01.M02.06 dependencias y salida de design system [Javier :30]
- Define dieciseis puertas de M03-M06, ocho procedimientos de rollback y ocho pares P/N FUTUROS_NO_EJECUTADOS para tokens, temas, accesibilidad, API, MySQL, seguridad, tenant A/B y version CAS.
- Integra solo documentos M02.01-.05 con A5 independientes y blobs auditados; no implementa Angular, CSS, .NET, EF, MySQL, eventos, UI, secretos, migracion ni deploy.
- V15 R0 y SINGLE_ACTIVE_DEV; Scope Lock STRICT/procedencia V5 exact-SHA antes de LISTO, A5 por otra Task Javier; rollback git revert normal; no QA/main/PROD.

## 2026-11-15 — V16 C05-03.M01.06 bloqueos y rollback documental del builder [Javier :10]
- Define 16 bloqueos BL01–BL16, diez pasos de rollback REV01–REV10, doce gates LG01–LG12 y diez pares de pruebas FUTUROS_NO_EJECUTADOS para catálogo tipado y aislamiento A/B.
- Conserva D01–D08 PENDIENTES y las fronteras JWT/RBAC, SSRF/XSS, CAS, versiones, permisos, accesibilidad y secretos; no implementa Angular, API, MySQL, editor, runtime ni publicación.
- Entrega individual V16/V15 R0 bajo Scope Lock STRICT, A0–A4 documentales y checks exact SHA; A5 solo otro Javier, rollback git revert; sin QA/main/PROD.

## 2026-11-15 — V16 C06-01.M01.06 bloqueos, rollback y liberacion documental de landing SaaS [Javier :30]
- Publica 16 barreras BL01-BL16, ocho procedimientos REV01-REV08 y ocho pares T01-T08 FUTUROS_NO_EJECUTADOS para rutas, planes, demo, contacto, legal, SEO, acceso tenant y accesibilidad.
- Preserva decisiones D01-D08 PENDIENTES, identidad JWT/RBAC y aislamiento A/B; no crea landing, API, integracion MySQL, SMTP, datos, servicios, despliegue ni permisos.
- Riesgo V15 R0; Scope Lock STRICT y procedencia V5 exact SHA obligatorios antes de LISTO_DEV; A5 solo de otra Task Javier; rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V16 C05-01.M02.03 persistencia e indices conceptuales de design system [Javier :20]
- Documenta doce familias de persistencia, doce accesos de indices, doce fases de migracion condicional y diez fixtures A/B positivos/negativos FUTUROS_NO_EJECUTADOS; no crea entidades EF, DDL ni datos reales.
- Distingue exclusividad transaccional de tema ACTIVO de un mero indice ambiguo, con aislamiento tenant, revocacion, idempotencia, seguridad CSS y rollback que necesita restore real futuro en MySQL efimero.
- Entrega individual R2 de frontera documental con Scope Lock STRICT, A0-A4 estaticas y checks exact-SHA, A5 pendiente de otra Task Javier, sin QA/main/PROD ni secreto o base modificada.

## 2026-11-15 — V16 C05-01.M02.02 contrato API/DTO seguro de design system [Javier :20]
- Define ocho operaciones futuras, ocho DTO tipados, doce errores seguros, dieciseis validaciones y diez pares P/N FUTUROS_NO_EJECUTADOS para tema, token, branding, preview, idempotencia y aislamiento A/B.
- No crea endpoints, controllers, DTO .NET, componentes Angular, CSS, EF/MySQL o rutas de despliegue; JWT/RBAC y ownership siguen siendo autoridad exclusiva del backend, NO_APLICA runtime al diff Markdown.
- Entrega R0 individual V16 con Scope Lock STRICT y A0-A4 documentales en SHA causal; A5 requiere otra Task Javier, sin QA/main/PROD y rollback git revert ordinario.

## 2026-11-15 — V16 C05-01.M02.01 entidades y ownership de design system [Javier :20]
- Define catorce entidades conceptuales, dieciocho invariantes fail-closed, relaciones por tenant y diez pares positivos/negativos FUTUROS_NO_EJECUTADOS para tokens, temas, componentes, branding, seguridad y accesibilidad.
- Preserva autenticacion/RBAC y propiedad A-B desde backend; ninguna entidad EF, API, CSS ni migracion fue implementada, ni se reclama ejecutar MySQL/Playwright o certificar WCAG.
- Entrega R0 individual bajo Scope Lock STRICT con A0-A4 documentales, checks exact-SHA, readback PLAN/COLA/BITACORA y A5 independiente pendiente; rollback por git revert normal, sin QA/main/PROD.

## 2026-11-15 — V5 C01-03.M03.01 Domain configuración dinámica segura [Javier :50]
- Añadidas reglas ejecutables .NET 10 de catálogo allowlisted/typed, límites tenant A/B, identidad+RBAC+entitlement, versión optimista, scope plataforma independiente y denegación de claves peligrosas.
- Incluidos diez xUnit positivos/negativos, límites de valor, scripts CSS/HTML, roles revocados, flags de seguridad, concurrency y errores; veredicto nunca concede escritura, JWT o CAS real.
- Riesgo V15 R2, validación CI backend/security/Scope Lock/V5 obligatoria sobre SHA; A5 de Task Javier diferente, no EF/MySQL/UI, rollback git revert sin QA/main/PROD.

## 2026-11-15 — V5 C06-04.M03.03 gate Authorization de portal cliente [Javier :50]
- Implementado Application .NET 10 que combina evidencia backend, JWT/token/sesión, membership/RBAC, recurso/tenant A/B, auditoría y versión contra preflight Application/Domain existente; respuestas siempre no autorizantes.
- Incluidos nueve métodos xUnit con asserts positivos, negativos, revocación, error, frontera, cross-tenant, propiedad, token y cancelación; no afirmar JWT HTTP real ni permisos de producción.
- Riesgo V15 R2. CI backend/seguridad/autorización/Scope Lock/V5 por SHA pendiente antes de LISTO; A5 independiente y git revert como rollback, sin DB/UI, QA/main/PROD.

## 2026-11-15 — V5 C02-04.M03.04 adaptador aislado de consola global [Javier :50]
- Implementado Infrastructure .NET 10 de snapshot read-only para el gate Application de control plane: actor, tenant, SHA y evidencia se verifican por identidad exacta, sin grants, proveedores, DI ni persistencia real.
- Añadidos nueve métodos xUnit positivos, negativos, adversariales y frontera para tenant A/B, revocación, MFA, permisos de plataforma, bypass cliente, cancelación, error y versión obsoleta.
- Riesgo V15 R2; CI backend, autorización, seguridad, Scope Lock y V5 requerida antes de LISTO. No JWT HTTP, EF/MySQL, QA/main/PROD. Revisión A5 de otra Task Javier, rollback git revert.

## 2026-11-15 — V5 C01-02.M03.01 capabilities Domain fail-closed [Javier :30]
- Implementa reglas .NET 10 Domain de intersección ordinal de capability, empresa/rol/membresía, ownership recurso, sucursal, plan/rubro y denylist; ningún entitlement crea grant RBAC.
- Añade 13 pruebas xUnit positivas/negativas contra actor/tenant cruzado, revocación, sucursal ajena, plan vencido, permiso faltante, matriz incompleta y capability deshabilitada; PASS sólo con CI del SHA.
- No activa JWT/HTTP, EF/MySQL, roles, flags, cobros, workers ni permisos de runtime; exige A0–A4 y A5 independiente, Scope Lock STRICT, dev únicamente y git revert.

## 2026-11-15 — V5 C05-04.M03.04 adaptador aislado de snapshots UI [Javier :50]
- Implementado Infrastructure .NET 10 con snapshot de solo lectura, bounds de recurso/SHA y aislamiento A/B; nueve xUnit positivos, adversariales, frontera y cancelación para Application/Domain sin autorizar acceso.
- No se incorpora DI, API, EF/MySQL, cache real, Angular, navegador, mediciones CWV, credenciales ni datos productivos; el snapshot por sí solo no certifica membresía/JWT externos.
- Riesgo V15 R2, controles CI backend/seguridad/Scope Lock/V5 necesarios por SHA antes de LISTO, A5 independiente de otra Task Javier, rollback git revert sin QA/main/PROD.

## 2026-11-15 — V16 continuidad secuencial sin cuotas por corrida [Javier propietario]
- Respaldo privado anterior verificado del Sheet integral, plan comercial y seis prompts con IDs/RRULE exactos; no operativo y BITACORA preservada.
- Aclara MAX_ACTIVE_WRITES_PER_TASK=1 como concurrencia real, no cantidad acumulada. Se mantiene capacidad advisory hasta 1000 scopes, sin reservas múltiples por Task.
- Meta de cierres UNBOUNDED_WITHIN_PROVIDER_RUN: REVIEW_FIRST, cerrar A0-A4 y A5 ajena, alternar VALIDANDO con CI externa, desarrollar solo una EN_PROGRESO a la vez y seguir en bucle mientras haya trabajo seguro y tiempo real del proveedor.
- Corrige selectWork target fijo 3 y añade pruebas de reconfirmación tras cierre, espera CI, duplicidad concurrente y salida segura. No crear Task ni tocar QA/main/PROD, secrets, permisos o A0-A5.

## 2026-11-15 — V5 C01-01.M06.06 cierre documental y gates del mapa de identidad [Javier :40]
- Reconciliados seis M05 y cinco M06 anteriores LISTO con A5 independiente real en PLAN/ COLA/DAG; incluida evidencia MySQL 8.4 efímera Category=Integration de M05.03 y ensayo sintético de git revert M06.04, sin extender esas pruebas a JWT/HTTP/PROD.
- Registradas fronteras de release: no hay certificación de JWT end-to-end, EF transversal, p95, WCAG completa ni QA; no se fuerzan gates G/O y este M06.06 necesita su propio A5 posterior.
- Cambio individual documental con rollback git revert, controles Scope Lock/V5 exact-SHA, BITACORA/readback y sin editar código runtime, cuentas, infraestructura o migraciones.

## 2026-11-15 — V5 C01-04.M03.04 adaptador Infrastructure aislado y sin grants [Javier :40]
- Implementado código real .NET 10: adaptador read-only tenant-scoped que combina dos fuentes backend confiables para preflight del expediente, deniega cross-tenant/legacy/revocación/SHA obsoleto y no registra DI, API, EF ni migraciones.
- Añadidas pruebas xUnit positivas/negativas que ejercitan el gate Application sin permisos de lectura/escritura. CI backend/seguridad obligatoria por SHA; no atribuir PASS antes de GitHub Actions.
- Documentados límites de snapshot simulado frente a JWT/HTTP/MySQL real, rollback normal y A5 pendiente de otra Task Javier; Scope Lock STRICT, sin QA/main/PROD.

## 2026-11-15 — V5 C10-04.M03.03 gate de autorización cache/jobs fail-closed [Javier :30]
- Implementa barrera real Application .NET 10 con fuente verificada, owner persistido, contexto tenant A/B, membresía/RBAC, grant de servicio y operaciones allowlisted; sin permisos efectivos ni efectos runtime.
- Incorpora doce tests xUnit positivos/negativos: suplantación, revocación, empresa cruzada, clave/hash, versión, deadline, fuente y cancelación. CI causal exacta pendiente hasta finalizar checks requeridos.
- No publica HTTP, workers, EF/MySQL, backup/restore, R2, leases ni secretos; A0–A4 causales y A5 independiente por otra Task Javier; rollback git revert y dev únicamente.

## 2026-11-15 — V5 C01-01.M06.04 ensayo aislado de rollback sin pérdida de datos [Javier :40]
- Ejecutado ensayo REAL en Git temporal sintético: 6/6 comprobaciones de cambio allowlisted, rechazo de alteración de datos, git revert y preservación de tres membresías A/B; SHA256 del fixture 8ade74dd43af6babefc3392df2e24dbc87a127ec470914701585918a46b1fcbc.
- Documentado runbook fail-closed de desactivación/reversión con evidencia positiva y negativa; NO se ejecutó rollback de SOLQARYN, JWT/HTTP, EF/MySQL, migraciones ni restauración de datos reales.
- Entrega individual C01-01.M06.04 sólo documento docs/v5 y esta sección; Scope Lock/V5 exact SHA y A5 por Task Javier distinta de DEV05, sin QA/main/PROD.

## 2026-11-15 — V15 calidad adaptativa A0–A5, respaldo y single-active [Javier propietario]
- Copia Drive privada PRECAMBIO del Sheet completo, plan y seis prompts nativos con IDs/horarios; respaldo no operativo.
- Riesgo R0–R3, pruebas negativas/adversariales e integración pertinente, A5 independiente, medición de defectos reales y reparación con regresión.
- MAX_ACTIVE_WRITES_PER_TASK=1 en capacidad, documentos y validadores de doble CLAIM; V13/1000 pasa a histórico no operativo, sin borrar bitácora.
- Scope Lock STRICT, solo seis Javier en dev; ninguna promoción QA/main/PROD, proveedor productivo o secretos.

## 2026-11-15 — V5 C01-01.M06.05 solicitud de revisión independiente del lote [Javier :20]
- Formaliza petición de A5 separada para M06.01 y M06.02 con SHAs/blobs y seis controles auditables, sin atribuir PASS al autor ni reservar revisor.
- Reconoce A5_PASS ya existentes para M05.01–06, respeta M06.03 con owner DEV02 y M06.04 pendiente; M06.06 continúa DAG G=NO hasta gates reales.
- Publicación documental única en dev, evento institucional y readback; no despierta Tasks, configura webhooks, ni modifica QA/main/PROD.

## 2026-11-15 — V5 C01-01.M06.03 riesgos y decisiones de identidad universal [Javier :10]
- Audita seis alcances M05: Domain xUnit, contrato Controller ScopeStub, integración EF Oracle MySQL local, aceptación y denegaciones Application/Domain, regresión y Chromium; identifica riesgos de JWT/HTTP y tenancy no cubiertos.
- Corrige atribución de CI MySQL M05.03: SHA 71eac84 es ancestro sin test nuevo; job Category=Integration del SHA fuente 11c6e30 reportó 47 PASS, 0 FAIL, 0 SKIPPED, sin Aiven/prod.
- Documenta R01–R12 y decisiones D01–D06, rollback y A5 independiente; entrega individual solo Markdown/CHANGELOG, sin código, infraestructura ni QA/main/PROD; Scope Lock y V5 requeridos por SHA exacto.

## 2026-11-15 — V5 C01-01.M06.02 manifiesto reproducible de pruebas y blobs [Javier :20]
- Registra SHAs de origen, ocho blobs actuales de siete archivos de pruebas más la auditoría M06.01, y comandos dirigidos .NET 10, MySQL 8.4 local y Chromium.
- Distingue código existente de pruebas efectivamente ejecutadas: no finge PASS JWT/HTTP, Category=Integration, p95 ni WCAG por un documento.
- Una sola unidad documental en dev, A0-A4 exact-SHA con Scope Lock/V5 y A5 independiente posterior; revert normal sin QA/main/PROD.

## 2026-11-15 — V5 C01-01.M06.01 auditoría de cobertura real e insuficiencia de dobles [Javier :20]
- Audita seis entregas M05 con evidencias de pruebas Domain, Application, controlador, EF/MySQL local y Chromium, separando dobles legítimos de ejecución integrada real.
- Documenta brechas de JWT/HTTP, filtros de persistencia, rendimiento p95 y cobertura WCAG sin inventar PASS ni ejecutar servicios; Scope Lock y seguridad tenant A/B intactos.
- Entrega individual en dev, A0-A4 documentales y A5 independiente pendiente; reversión git revert, sin QA/main/PROD ni proveedores reales.

## 2026-11-15 — V5 C05-04.M03.03 preflight autenticado de ligereza por tenant [Javier :10]
- Incorpora gate Application .NET 10 con evidencia backend, tenant A/B, propietario persistido, RBAC vigente, invalidación por revocación y SHA exacto. Rechaza bypass cliente, precarga privada y telemetría sensible.
- Añade diez tests xUnit positivos/negativos sobre identidad, revocación, cross-tenant, caché, versión y presupuestos. El veredicto nominal no concede JWT/HTTP ni Core Web Vitals: requiere adaptador runtime posterior.
- Alcance individual dev, commit CAS fast-forward y Scope Lock/V5; pruebas A0-A4 solo con CI real, A5 independiente. No cambia Angular, EF/MySQL, secretos, QA/main/PROD; reversión git revert.

## 2026-11-15 — V5 C10-03.M02.03 persistencia e índices conceptuales de inventario [Javier :00]
- Documenta 12 proyecciones futuras, 12 índices candidatos, 14 invariantes, siete etapas de migración condicionadas y doce pares sintéticos FUTUROS_NO_EJECUTADOS para archivos privados.
- Conserva inventario histórico NO_VERIFICADO, evita inferir cero, exige conciliación real, tenant A/B, JWT/RBAC, MIME por bytes, retención/hold, hash e idempotencia; Cloudinary existente no cambia.
- Entrega individual Markdown en dev con Scope Lock/V5 exact-SHA, A5 pendiente de otra Task Javier y revert ordinario; sin EF/SQL, MySQL real, R2/DNS, migraciones, QA/main/PROD.

## 2026-11-15 — V5 C10-03.M02.02 contratos propuestos API y DTO de inventario condicionado [Javier :00]
- Define 12 operaciones futuras, 12 DTO contractuales, 16 errores seguros y 12 pares positivos/negativos FUTUROS_NO_EJECUTADOS para inventario histórico NO_VERIFICADO y archivos privados.
- Preserva JWT/RBAC, tenant A/B, Cloudinary existente, MIME por bytes, límites de tamaño, firmas/TTL, retención, idempotencia y estados ZERO_VERIFIED/POSITIVE_VERIFIED sólo con evidencia real.
- Entrega individual Markdown en dev con Scope Lock/V5 exact-SHA, A5 por otra Task Javier y git revert ordinario; sin endpoints, EF/MySQL, inventario real, R2/DNS, QA/main/PROD.

## 2026-11-15 — CI auditada por SHA sin cancelaciones entre commits [Javier propietario]
- Preserva ejecuciones independientes de los cinco controles críticos en dev: backend, seguridad y pruebas, calidad ERP, Scope Lock y procedencia V5 mediante grupos de concurrencia por SHA con cancel-in-progress desactivado.
- No retira jobs, pruebas, pasos, filtros, permisos, validaciones, autoría ni branch protections. Cada ejecución mantiene su SHA, logs y resultado; CANCELLED, SKIPPED, FAIL e IN_PROGRESS jamás se clasifican como PASS.
- A0–A4 y A5 independiente continúan requiriendo evidencia verdadera. Reversión mediante git revert ordinario, preservando el historial y sin tocar QA/main/PROD, datos, proveedores ni secretos.

## 2026-11-15 — V5 C10-03.M02.01 entidades y ownership de inventario condicionado [Javier :50]
- Diseña 13 objetos lógicos futuros de inventario/medios, 14 invariantes tenant-aware y 10 pares de pruebas FUTURAS; inventario histórico permanece NO_VERIFICADO, nunca cero supuesto.
- Distingue cero autenticado (sin migración histórica), positivo conciliado y estado inconsistente; mantiene MIME/URL temporal/JWT/RBAC/hold y aislamiento A/B fail-closed sin ejecutar Cloudinary/R2/SQL.
- Entrega documental individual en dev tras claim único rescatado, Scope Lock/V5 exact-SHA, A5 ajena pendiente y rollback git revert, sin proveedores activos, QA/main/PROD.

## 2026-11-15 — V5 C04-02.M02.05 eventos futuros e idempotencia contractual [Javier :50]
- Define doce eventos conceptuales inertes, envelope mínimo y doce reglas de idempotencia/correlación, con 12 pares de pruebas FUTURAS sin emitir ni implementar mensajes.
- Distingue ledger actual de onboarding de futuras renovaciones, cobros y outbox; tenant A/B, JWT/RBAC, D01–D08, privacidad y callbacks se mantienen fail-closed.
- Entrega secuencial Markdown en dev con Scope Lock/V5 exact-SHA, A5 pendiente de Task Javier ajena y rollback git revert; sin eventos, workers, migraciones, QA/main/PROD.

## 2026-11-15 — V5 C04-02.M02.04 opciones tipadas contractuales y flags [Javier :50]
- Especifica 18 opciones contractuales inactivas, precedencia restrictiva, doce reglas fail-closed y 12 pares de pruebas FUTURAS, sin registrar valores ejecutables.
- Preserva tenant A/B, JWT/RBAC, membresía, idempotencia, auditoría y decisiones económicas D01–D08 abiertas; feature flags no autorizan cobros, mora, permisos o promoción.
- Publicación Markdown única por CAS dev, Scope Lock/V5 exact-SHA, A5 pendiente de Javier distinto y rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V5 C04-02.M02.03 persistencia conceptual y rollback SaaS [Javier :50]
- Documenta trece objetos propuestos de contratos con ownership EmpresaId, diez restricciones e índices y siete etapas de migración aditiva FUTURA, sin escribir EF/SQL ni datos.
- Define doce escenarios positivos/negativos de integración futura con MySQL 8.4 efímero, idempotencia y tenant A/B; diferencia ledger técnico actual de las decisiones D01–D08 aún sin aprobar.
- Publicación documental única en dev por CAS, Scope Lock/V5 exact-SHA, A5 por otro Javier y git revert ordinario; no QA/main/PROD, pagos ni migraciones ejecutadas.

## 2026-11-15 — V5 C04-02.M02.02 contratos propuestos API y DTO SaaS [Javier :50]
- Define sólo en documentos 10 operaciones contractuales futuras, nueve DTO, doce errores y 12 pares positivos/negativos para SaaS billing, con rutas inactivas y compatibilidad explícita del onboarding existente.
- Preserva JWT/RBAC, tenant A/B, versiones inmutables, idempotencia, BOLA/IDOR y decisiones D01–D08 aún no aprobadas; no implementa API, Angular, MySQL, cobros ni eventos.
- Entrega individual y secuencial en dev, CHANGELOG autocontenido, Scope Lock/V5 exact-SHA y A5 independiente pendiente; sin QA/main/PROD.

## 2026-11-15 — V5 C04-02.M02.01 entidades y ownership de contratos SaaS [Javier :50]
- Define diseño exclusivamente documental de 16 objetos contractuales, sus relaciones por EmpresaId y 18 invariantes fail-closed; diferencia contrato legal futuro de Suscripcion SaaS existente.
- Registra 12 pares de escenarios positivos/negativos FUTUROS, puertas D01–D08 económicas aún abiertas y restricciones JWT/RBAC/tenant A/B, idempotencia y auditoría sin activar runtime, MySQL, pagos ni eventos.
- Entrega única en dev con diff de dos Markdown y A0–A4 documentales; requiere Scope Lock/V5 exact-SHA, readback institucional y A5 independiente de otro Javier, nunca QA/main/PROD.

## 2026-11-15 — V5 C05-01.M01.06 cierre documental del sistema de diseño [Javier :40]
- Consolida cinco documentos M01 de design tokens y componentes con A5 independiente, 16 barreras fail-closed, diez pares positivos/negativos futuros y ocho reversas condicionadas.
- Conserva decisiones D01–D08 pendientes, riesgo de foco/contraste/responsive/branding tenant A-B y pruebas Angular/Playwright NO_EJECUTADAS; el menú y el tema no conceden JWT/RBAC.
- Publica sólo Markdown y CHANGELOG en dev con Scope Lock/V5 exact-SHA y A5 pendiente de otra Task Javier; sin alterar CSS/UI en runtime, QA/main/PROD, bases de datos ni Tasks.

## 2026-11-15 — V5 C08-04.M02.06 contrato de salida de extensiones sectoriales [Javier :40]
- Consolida seis antecedentes M01/M02 con A5 independiente, modelo de capacidades sin duplicar Empresa/Sucursal/Usuario y 16 gates técnicos fail-closed para una implementación futura de módulos por rubro.
- Define doce pares de aceptación/denegación FUTUROS_NO_EJECUTADOS y ocho mecanismos de reversión condicionados a JWT/RBAC, tenant A/B, CAS, auditoría, MySQL efímero y alcance.
- Entrega documental exclusivamente en dev y con CHANGELOG autocontenido; Scope Lock/V5 exact-SHA, A5 por otra Task Javier, sin migraciones, endpoints, flags reales, eventos, QA/main/PROD.

## 2026-11-15 — V5 C04-02.M01.06 barreras y rollback de contratos SaaS [Javier :40]
- Define 16 puertas fail-closed, matriz de transiciones positivas/negativas futuras y ocho respuestas de rollback para contratos SaaS sin ejecutar pagos, MySQL, JWT/HTTP ni migraciones.
- Conserva D01–D08 como decisiones económicas abiertas; M01.01–.05 con A5 independiente y C04-01.M01.06 son antecedentes, no autoridad para elevar permisos, publicar ni abrir M02 sin su DAG.
- Entrega documental exclusivamente en dev mediante diff Markdown y sección CHANGELOG autocontenida, Scope Lock y procedencia V5 exact-SHA; exige A5 posterior de otra Task Javier y rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V5 C05-04.M03.02 caso Application de ligereza y tenancy [Javier :20]
- Implementa servicio .NET 10 de preflight con fuente de evidencia backend, validación tenant A/B antes de I/O y veredicto no-autorizante sobre caché/budgets.
- Añade ocho xUnit reales de denegación cross-tenant, revocación, evidencia ausente, cancelación y revisión obsoleta; no afirma Core Web Vitals medidos ni integración HTTP/EF.
- Commit en dev con CHANGELOG autocontenido, Scope Lock y CI de backend/seguridad exact-SHA; A5 independiente, sin QA/main/PROD.

## 2026-11-15 — V5 C02-04.M03.03 control de autorizacion de consola global [Javier :30]
- Implementa gate Application .NET10 fail-closed y lector tipado de evidencia backend para actor/tenant/SHA, sin conceder permisos JWT, tenant, MFA ni lectura de datos, ni derivarlos de hints del cliente.
- Agrega siete pruebas xUnit dirigidas de revocacion, evidencia, MFA/reautenticacion, tenant A/B, grants de plataforma distintos al ERP, lease excepcional con aprobador y cancelacion; ninguna prueba se declara PASS antes de GitHub Actions causal.
- Reutiliza Domain M03.01 y Application M03.02, no incorpora HTTP/DI/EF/MySQL ni grant real; Scope Lock y V5 por SHA, A5 independiente, rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V5 reparación Scope Lock C10-03.M02.04/.05 [Javier :20]
- Sustituye once apariciones de un nombre técnico en inglés reservado por el verificador de identidad del proyecto, sólo en los dos contratos documentales nuevos.
- Corrige causa exacta de FAIL en Scope Lock y seguridad multiempresa del SHA c695a998; preserva los 18 campos conceptuales, 12 señales, invariantes y ausencia de datos reales.
- Mantiene gates JWT/RBAC/tenant A/B, no rebaja validadores y exige nueva CI exact-SHA SUCCESS antes de LISTO_DEV; sin QA/main/PROD.

## 2026-11-15 — V5 C10-03.M02.04 opciones tipadas para inventario condicionado [Javier :20]
- Define 18 opciones conceptuales, 4 ámbitos restrictivos, 12 reglas y 12 pares P/N FUTUROS_NO_EJECUTADOS; NO_VERIFICADO no se convierte en cero.
- Mantiene JWT/RBAC tenant A/B, retención, MIME, URL TTL/replay, sin flags runtime, R2, proveedor, DNS o inventario real.
- Entrega Markdown independiente en dev con Scope Lock/V5, A5 ajeno y git revert; no QA/main/PROD.

## 2026-11-15 — V5 C10-03.M02.05 eventos inertes de inventario y archivos [Javier :20]
- Documenta 12 señales hipotéticas, envelope mínimo, 14 invariantes de idempotencia/cuota/replay y 12 pares P/N futuros sin emitir eventos.
- Un inventario desconocido no habilita migración; no bus, worker, dispatcher, webhook, outbox, firmas, Cloudinary/R2/MySQL reales.
- Publicación documental independiente con trazabilidad y A0–A4 por ID, A5 otra Task Javier y rollback normal.

## 2026-11-15 — V5 C01-04.M03.03 case-file tenant authorization fail-closed [Javier :30]
- Implementa gate Application .NET 10 con evidencia de servidor, SHA exacto y ownership persistido para expediente/timeline; deniega tenant A/B ajeno, membresia revocada, branch incorrecta, bypass de filtros y escrituras institucionales.
- Agrega once pruebas xUnit positivas/negativas de autorizacion, idempotencia, version, adjuntos, revision independiente y cancelacion. Todo resultado carece de concesion runtime: requiere adaptador autenticado posterior, sin JWT HTTP, EF/MySQL, Cloudinary ni API activados.
- A0-A4 sujetos a GitHub Actions reales por SHA, Scope Lock y V5; A5 de otra Task Javier, rollback git revert, ningun cambio a QA/main/PROD ni secretos.

## 2026-11-15 — V5 C10-02.M02.06 contrato de salida M02 archivos y dominios [Javier :20]
- Consolida predecesores M02.01–05 LISTO+A5 y define G01–G16 gates reales y R01–R08 reversión futura, sin confundir docs con CI o inventario de proveedores.
- Mantiene tenant A/B JWT/RBAC server-side, TTL/replay, idempotencia, hold, secretos fuera del repo y estado inventario NO_VERIFICADO.
- Solo docs/v5 y CHANGELOG en dev por fast-forward/CAS; Scope Lock y V5 SHA causal, A5 independiente antes de M03, sin R2/DNS/QA/main/PROD.

## 2026-11-15 — V5 C06-04.M03.02 servicio Application del portal cliente [Javier :10]
- Implementa Application .NET10 de prevalidacion privada CUSTOMER con fuente verificada, enlazando C0604PortalClienteDomainRules; rechaza tenant B, cliente A2, owner NULL, sesion revocada/expirada, JWT no acreditado y CAS stale.
- Agrega diez tests xUnit positivos/negativos para permiso/tenant/cuenta/sesion/recurso/version, sin endpoint, DI, MySQL, pedidos, pagos, workers ni tokens.
- A0-A4 requieren GitHub Actions backend, seguridad, autorización y ScopeLock/V5 exact SHA; A5 independiente, rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V5 C01-01.M05.06 regresiones reales y accesibilidad [Javier :20]
- Agrega cinco pruebas xUnit sobre identidad tenant A/B con costo de lecturas acotado, cancelación y revocación; no sustituye benchmarks de producción o MySQL real.
- Añade dos pruebas Chromium de foco, reduced-motion y ruta privada, con workflow efímero de GitHub-hosted, sin credenciales ni API productiva.
- Pruebas, documento y CHANGELOG en dev; Scope Lock/CI exact-SHA, A5 independiente y rollback normal, sin QA/main/PROD.

## 2026-11-15 — V5 C10-04.M03.02 Application preflight cache jobs backups exports [.NET10 Javier :10]
- Implementa clase Application real con proveedor de contexto verificado, frontera read-only y enlace a C1004CacheJobsDomainRules; deniega cross-tenant A/B, permisos revocados, version stale, operaciones no allowlisted y reintentos fuera de politica.
- Agrega diez pruebas xUnit para casos positivos/negativos, hint cliente, SHA, version, expiry y cancelacion, todos los flags Allows... false; un preflight no concede lease, CI ni proveedor real.
- Requiere CI backend/security/auth/ScopeLock/V5 exact SHA antes LISTO; A5 independiente; sin workers, DB, backup, R2, QA/main/PROD.

## 2026-11-15 — V5 C10-03.M01.06 contrato de salida, bloqueos y rollback de archivos [Javier :20]
- Define BL01–BL16 y RB01–RB08 como requisitos documentales futuros, distinguiendo inventario NO_VERIFICADO del cero autenticado sin simular consultas.
- Constata M01.01–05 y C10-01/02.M01.06 LISTO+A5 al preflight; M02 solo después de A5 independiente de esta unidad y DAG/COLA reactivos reales.
- Docs y CHANGELOG dev, sin Cloudinary/R2/DNS, migración, MySQL, secrets, QA/main/PROD; Scope Lock/V5 exact SHA y rollback git revert.

## 2026-11-15 — V5 C10-02.M02.04 opciones tipadas y dominios de archivos futuros [Javier :10]
- Documenta 18 opciones conceptuales, 4 ámbitos de precedencia, 12 reglas fail-closed y 12 pares de aceptación FUTUROS_NO_EJECUTADOS para prefijos privados, enlaces firmados, hosts y lifecycle.
- Tenant A/B y JWT/RBAC sólo server-side, TTL/replay, retención, cache y secreto fuera del repo; no activa R2, DNS, migraciones, flags, runtime ni producción.
- Documento docs/v5 y CHANGELOG autocontenido, Scope Lock/V5 exact SHA para LISTO, A5 independiente y rollback git revert.

## 2026-11-15 — V5 C10-02.M02.05 eventos inertes, deduplicacion y limites futuros [Javier :10]
- Especifica 12 eventos propuestos, 14 invariantes de idempotencia/replay, 5 grupos de consumidores y 12 pares PN FUTUROS_NO_EJECUTADOS para archivo, URL firmada y dominios.
- Mantiene Cloudinary vigente, tenant A/B, privacidad y retención; cero webhooks, workers, colas, outbox, R2 o DNS reales.
- Publicacion documental de dos IDs independientes en un commit fast-forward CAS, cada ID con auditorias A0–A4 y A5 ajena pendiente.

## 2026-11-15 — V5 C10-02.M02.03 persistencia conceptual y fixtures de archivos [Javier :00]
- Define doce proyecciones de persistencia, doce índices candidatos, doce invariantes de integridad y doce pares de fixtures FUTUROS_NO_EJECUTADOS para archivos privados, TTL, retención y migración reversible.
- Sin SQL, MySQL, EF, Cloudinary/R2/DNS, proveedor, API ni QA/main/PROD; no ejecuta restauración ni autoriza archivos externos.
- Documental dev por fast-forward/CAS, CHANGELOG autocontenido, Scope Lock/V5 exact-SHA y A5 independiente pendiente.

## 2026-11-15 — V5 C10-02.M02.02 API/DTO conceptuales de archivos privados [Javier :00]
- Documenta doce operaciones futuras, ocho DTO, catorce errores fail-closed y doce pares P/N FUTUROS_NO_EJECUTADOS; tenant A/B, firma/TTL, replay, integridad y retención.
- Sin endpoints, Cloudinary/R2, DNS, proveedores, credenciales, MySQL, UI o QA/main/PROD; no se ejecutaron pruebas HTTP ni de archivos.
- GitHub dev por fast-forward/CAS, doc y sección de CHANGELOG autocontenida, Scope Lock y V5 CI causal, A5 independiente.

## 2026-11-15 — V5 C10-02.M02.01 entidades y ownership R2/dominios [Javier :00]
- Especifica doce proyecciones conceptuales y dieciséis invariantes de almacenamiento privado, tenant A/B, URL/TTL, replay, caché y retención; doce pares P/N FUTUROS NO_EJECUTADOS.
- No activa R2, DNS, Cloudinary, credenciales, redes, migraciones, API, MySQL ni QA/main/PROD; A5 independiente por otra Task Javier.
- Documentación en dev con fast-forward/CAS, CHANGELOG autocontenido, Scope Lock/V5 exact-SHA, eventos y readbacks.

## 2026-11-15 — V5 reparación dirigida Scope Lock C07-01.M02.04/M02.05 [Javier :10]
- Corrige dos nombres conceptuales que contenían una palabra inglesa reservada por el verificador de identidad del proyecto; usa StockAvailabilityMode y CatalogStockAvailabilityChanged sin alterar reglas, seguridad ni datos.
- Origen: check verify-solqaryn-project-scope FAILED en SHA 927e4a54 con dos rutas docs/v5; reparación mínima preserva pruebas y controles y requiere nueva CI exact-SHA SUCCESS antes LISTO_DEV.
- Solo dos Markdown y CHANGELOG, commit fast-forward con control de HEAD, sin desactivar analizador ni modificar runtime/QA/main/PROD.

## 2026-11-15 — V5 C07-01.M02.04 configuracion tipada conceptual y flags de catalogo [Javier :10]
- Define dieciocho opciones tipadas propuestas, cuatro ámbitos de precedencia, catorce reglas de validación y catorce pares PN FUTUROS_NO_EJECUTADOS para catálogo universal.
- Conserva tenant A/B y rol actual verificado en backend, NULL legacy en Producto/Variante y categoría sin EmpresaId asumida; presets fail-closed, sin secretos ni activación runtime.
- Sólo documento docs/v5 y sección íntegra CHANGELOG; Scope Lock/V5 exact SHA, A5 pendiente de otro Javier, rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V5 C07-01.M02.05 eventos conceptuales y limites catalogo universal [Javier :10]
- Define doce eventos FUTUROS_INERTES, envelope mínimo, diez reglas de idempotencia/replay/cuotas, consumidores objetivo y doce pares PN FUTUROS_NO_EJECUTADOS.
- Preserva ownership server-side tenant A/B, compatibilidad ERP, snapshots/precios/stock y cuarentena de NULL legacy, sin bus, workers, outbox, webhook ni payload real.
- Dos documentos independientes compartieron publicación documental fast-forward CAS; Scope Lock/V5 exact SHA, A5 ajena, no QA/main/PROD.

## 2026-11-15 — V5 C07-01.M02.03 persistencia conceptual de catálogo [Javier :00]
- Contrato de doce proyecciones, doce índices candidatos, doce reglas de integridad y doce pares de fixtures FUTUROS NO_EJECUTADOS; legado NULL y tenant A/B fail-closed.
- No SQL, EF/MySQL, API, UI, cambios de stock/precios, proveedores, secretos ni QA/main/PROD; A5 independiente y pruebas de runtime futuras.
- Dev fast-forward/CAS con CHANGELOG autocontenido, Scope Lock y V5 exact-SHA y readbacks institucionales.

## 2026-11-15 — V5 C07-01.M02.02 contratos conceptuales API/DTO, errores y compatibilidad de catálogo [Javier :10]
- Especifica doce operaciones propuestas, siete perfiles DTO, catorce errores seguros y doce pares positivos/negativos FUTUROS NO_EJECUTADOS para catálogo universal, variantes, categorías, impuestos y storefront.
- Preserva autoridad server-side JWT/RBAC/tenant A/B, NULL legacy en Producto/Variante, categoría sin EmpresaId asumida, snapshots financieros/stock y separación público/privado; sin runtime API, EF/MySQL, Angular, secretos ni despliegues.
- Un documento docs/v5 y sección autocontenida CHANGELOG_AI.md, fast-forward CAS en dev, Scope Lock/V5 exact SHA antes LISTO; A5 independiente pendiente y rollback git revert, no QA/main/PROD.

## 2026-11-15 — V5 C07-01.M02.01 entidades y ownership del catálogo universal [Javier :00]
- Contrato documental de doce conceptos y veinte invariantes sobre tenant A/B, NULL legado, variantes, categorías, precios, stock, servicio y storefront; doce pares positivos/negativos FUTUROS NO_EJECUTADOS.
- Sin nuevas clases, API, MySQL, Angular, JWT/RBAC, cambios económicos, secretos, QA/main/PROD ni proveedores; A5 por otra Task Javier.
- Dos archivos Markdown únicamente en dev, con fast-forward/CAS, Scope Lock y verificador V5 exact-SHA y readbacks causales.

## 2026-11-15 — V5 C02-04.M03.02: preflight Application de consola global fail-closed [.NET 10 Javier :50]
- Implementa servicio Application no autorizante sobre reglas Domain de consola global: validación UTC, tenant A/B, rechazo de autoridad de cliente, mutación/exportación, MFA, reautenticación, grants y aprobación excepcional.
- Añade nueve pruebas reales xUnit con rutas positivas/negativas de tenant/rol/excepción y política GrantsAccess=false; ningún resultado concede acceso ni emite credenciales, eventos o auditoría live.
- Solo Application + xUnit + docs/v5 + CHANGELOG en dev, Scope Lock y CI dirigida exact-SHA; sin API/DI, JWT live, MySQL, QA/main/PROD.

## 2026-11-15 — V5 C07-01.M01.06: gates, rollback y liberación documental de catálogo universal [Javier :50]
- Define K01–K14 como puertas de ownership tenant, NULL legacy, variantes, impuestos/precios/snapshots, inventario y storefront sin atribuir implementación universal.
- Separa LISTO_DEV documental, A5 independiente y M02 con DAG/COLA/CAS nuevos; incorpora reversión aditiva y oráculos P01–P06/N01–N12 FUTUROS NO_EJECUTADOS.
- Un documento docs/v5 y sección íntegra CHANGELOG_AI.md en dev por fast-forward CAS; sin migraciones, API, JWT/RBAC runtime, QA/main/PROD.

## 2026-11-15 — V5 C10-02.M01.06: gates de salida y reversión documental R2/dominios [Javier :50]
- Define B01–B12 para predecesores LISTO+A5, owner CAS, Scope Lock, JWT/RBAC/tenant A/B, TTL/replay, privacidad, retención e inventario sin activar proveedores ni infraestructura.
- Separa LISTO_DEV documental, A5 independiente y habilitación M02 reactiva; aporta niveles de rollback documental/futuro y P01–P06/N01–N12 FUTUROS NO_EJECUTADOS.
- Solo docs/v5 y CHANGELOG_AI.md, fast-forward CAS en dev, sin R2/DNS/cutover, MySQL real ni QA/main/PROD.

## 2026-11-15 — V5 C06-04.M03.01 validadores dominio portal cliente [Javier :40]
- Implementa guards .NET 10 de propiedad cliente/tenant, cuenta y sesion privada activa, expiracion/revocacion y version optimista; los ID autoritativos deben resolverse en servidor, sin nuevas rutas ni JWT.
- Agrega xUnit positivas/negativas para A/B y A1/A2, cuenta ajena, owner legacy null, sesion revocada/vencida y CAS esperado; Scope Lock, pruebas backend y seguridad exact SHA antes de LISTO.
- No activa portal HTTP, migracion, jobs, eventos, webhooks, backup, R2 ni QA/main/PROD; A5 independiente y rollback git revert.

## 2026-11-15 — V5 C01-01.M05.03 MySQL efimero identidad universal [Javier :40]
- Agrega una prueba real de integración .NET/MySQL 8.4 aislada de producción que persiste usuario global, empresas A/B, roles tenant-owned y membresías, limpia EF ChangeTracker y vuelve a leer filas.
- Verifica contexto A/B positivo y rechazos por rol cruzado, recurso de otra empresa y membresía revocada, con cleanup de base efímera localhost y sin migraciones nuevas.
- La CI MySQL/backend/seguridad/Scope Lock/V5 exact-SHA debe acreditar A0-A4; A5 por otra Task Javier y rollback git revert, sin JWT HTTP ni QA/main/PROD.

## 2026-11-15 — V5 C10-04.M03.01 reglas de dominio cache, jobs y artefactos [Javier :40]
- Implementa validadores reales .NET 10 puros para contexto tenant/actor, solicitudes idempotentes, limites de intento/deadline, lease y artefactos con checksum, sin conceder permisos ni efectos de runtime.
- Agrega pruebas xUnit positivas y negativas sobre A/B, actor cruzado, payload, versiones y artefactos incompletos; A0-A4 requieren CI backend/seguridad/Scope Lock/V5 exact SHA antes de LISTO.
- No implementa CAS de repositorio, API, JWT, EF/MySQL, workers, backups, restore, R2, eventos ni webhook; A5 independiente y rollback git revert; sin QA/main/PROD.

## 2026-11-15 — V5 C01-01.M05.05 rechazo negativo aislamiento identidad universal .NET 10 [Javier :30]
- Incorpora seis xUnit reales de rechazo: consulta A a B sin I/O, snapshot B, actor A1/A2, membresía revocada, rol cambiado y sucursal B tras cambio de owner.
- Ejecuta Application/Domain con fuente de prueba en memoria y oráculos de excepción/lectura; no certifica HTTP/JWT/EF/MySQL ni cambia código de producción.
- Requiere CI backend/seguridad pertinente, Scope Lock y V5 exact SHA, A5 independiente y rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V5 C01-01.M05.04 aceptación positiva identidad universal .NET 10 [Javier :30]
- Añade tres casos xUnit reales para el servicio Application/Domain: contextos A/B mismo usuario, sucursal propia explícita y revalidación de snapshots por solicitud.
- Usa únicamente fuente in-memory como costura de pruebas; no certifica JWT HTTP, EF/MySQL ni datos vivos y no modifica código de producción ni permisos tenant/RBAC.
- Scope Lock, backend xUnit y seguridad pertinentes exact SHA requeridos, A5 ajena pendiente y rollback git revert; sin QA/main/PROD.

## 2026-11-15 — V5 C01-01.M05.02 pruebas contrato API DTO tenant-context [Javier :20]
- Incorpora cinco casos xUnit .NET10 de TenantContextController: empresa 0/-1 rechazada antes de servicio, tenant sin membresia forbidded, scope valido con DTO y selector limitado a fuente servidora.
- Contrato real de controlador con ScopeStub determinista, NO certifica HTTP JWT EF MySQL de extremo a extremo; checks backend y seguridad exact-SHA obligatorios antes de LISTO_DEV.
- Sin alterar codigo de produccion ni permisos, rutas, DB, eventos, webhooks o QA/main/PROD; A5 independiente y rollback git revert.

## 2026-11-15 — V5 C01-01.M05.01 xUnit identidad y membresia tenant [Javier :20]
- Anade cinco casos de pruebas .NET 10 (tres variantes Theory y dos Fact) sobre rol empresarial vigente, revocacion, tenant A/B, IDs invalidos y rechazo de RolId legacy, sin modificar codigo de produccion.
- El PASS exige pruebas backend GitHub Actions exact-SHA, Scope Lock y procedencia V5; reglas Domain no certifican JWT, MySQL, filtros EF ni seguridad HTTP en vivo.
- Entrega pruebas mas contrato documental, A5 independiente pendiente, git revert normal; sin Angular, QA/main/PROD, eventos, webhooks ni dispatchers.

## 2026-11-15 — V5 C10-04.M02.06 salida documental cache jobs backups y exportaciones [Javier :30]
- Consolida cinco contratos M02 y sus A5 independientes, dieciseis gates G01-G16 y handoff Domain/Application/Infrastructure/API, cache, jobs, exportaciones y respaldos sin activar runtime.
- Documenta RB01-RB08 y doce pares P/N FUTUROS_NO_EJECUTADOS con aislamiento tenant A/B, usuarios A1/A2, JWT/RBAC, version CAS, cuotas e idempotencia fail-closed; no DDL, eventos, dispatcher, backups, endpoints ni proveedores.
- Solo docs/v5 + CHANGELOG_AI.md, Scope Lock/V5 exact-SHA, A0-A4 documentales y A5 otra Task Javier; rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V5 C06-04.M02.06 contrato de salida M02 portal de cliente [Javier :20]
- Consolida los contratos documentales M02.01–05 y sus A5 independientes, diez frentes de dependencia y dieciseis invariantes fail-closed para portal CUSTOMER/ERP y tenants A/B, clientes A1/A2.
- Define doce parejas de aceptacion POS/NEG FUTURAS_NO_EJECUTADAS y condiciones A0–A5, exact-SHA, rollback y DAG para M03; no implementa API, JWT, migracion, UI, eventos ni dispatcher.
- Entrega exclusivamente documental con CHANGELOG autocontenido; Scope Lock/V5 dirigidos, A5 independiente pendiente y prohibicion de QA/main/PROD.

## 2026-11-15 — V5 C06-04.M02.05 eventos e idempotencia inertes portal cliente [Javier :10]
- Especifica doce contratos de eventos futuros, envelope acotado, diez reglas de deduplicacion/reintentos/cuotas y ocho pares PN FUTUROS_NO_EJECUTADOS; no crea dispatcher, worker, bus, cola ni webhook.
- Preserva tenant A/B y A1/A2, principal CUSTOMER/ERP separado, idempotencia scope-bound, correlationId no autorizante, payload minimo y secreto fuera de log/evento.
- Cambio Markdown+CHANGELOG, check Scope Lock/V5 exact-SHA, A5 ajena pendiente, git revert sin force; no QA/main/PROD.

## 2026-11-15 — V5 C06-04.M02.04 configuracion tipada portal cliente [Javier :10]
- Define catorce opciones tipadas propuestas, doce reglas de validacion, cuatro ambitos y ocho pares PN FUTUROS_NO_EJECUTADOS; no activa flags, IAM, JWT, backend ni Angular.
- Conserva tenant A/B y clientes A1/A2, defaults fail-closed, politica de seguridad global, consentimiento/ownership, TTL y secreto fuera de configuracion publica.
- Entrega Markdown+CHANGELOG autocontenida, Scope Lock y check V5 exact-SHA, A5 independiente pendiente y rollback git revert; sin QA/main/PROD.

## 2026-11-15 — V5 C06-04.M02.03 persistencia conceptual portal cliente [Javier :10]
- Propone doce agrupaciones conceptuales de datos, diez índices/constraints y ocho pares de fixtures FUTUROS_NO_EJECUTADOS; cero tablas, migraciones EF, seeds o accesos MySQL reales.
- Preserva tenant A/B y cliente A1/A2, FK compuesta, CAS, challenge hash único uso, grants privados y reversión transaccional como requisitos futuros.
- Publicación documental SHA exacto Scope Lock/V5, A5 independiente pendiente y rollback git revert; sin QA/main/PROD.

## 2026-11-15 — V5 C06-04.M02.02 API/DTO de portal cliente [Javier :10]
- Define ocho rutas futuras propuestas, DTO de confianza, doce errores seguros y ocho parejas de aceptación FUTUROS_NO_EJECUTADOS; no implementa endpoints, OAuth, MySQL, Angular, workers ni proveedores.
- Mantiene aislamiento por tenant A/B y cliente A1/A2, separación JWT cliente/ERP, recuperación no enumerable, versión CAS, grants temporales, no-store y denegación por defecto.
- Cambio exclusivamente documental con fast-forward/CAS, Scope Lock y V5 exact-SHA, A5 independiente pendiente y rollback git revert; sin QA/main/PROD.

## 2026-11-15 — V5 C06-04.M02.01 entidades de portal de cliente [Javier :00]
- Define dieciseis entidades conceptuales, dieciseis invariantes y doce pares positivos/negativos FUTUROS_NO_EJECUTADOS de ownership por tenant y cliente, pedidos, direcciones, reservas, documentos y recuperacion.
- Conserva aislamiento tenant A/B e intra-tenant A1/A2, JWT cliente distinto de ERP, RBAC, CAS, PII y respuestas neutras; ninguna tabla, endpoint, Angular, migracion o servicio se activa.
- A0-A4 documentales y Scope Lock/V5 exact-SHA, A5 independiente pendiente; rollback git revert, sin webhooks, datos reales, secretos ni QA/main/PROD.

## 2026-11-15 — V5 C06-03.M02.06 salida documental editoriales/mapas [Javier :00]
- Consolida cinco contratos M02.01–05 con blobs y A5 independientes verificables, dieciseis gates G01-G16, seis fronteras por capa y ocho planes RB01-RB08 FUTUROS_NO_EJECUTADOS.
- Conserva tenant A/B, JWT/RBAC, Host/proxy sin autoridad, publicacion CAS, preview noindex, contacto y horarios aprobados, mapas opcionales con fallback textual; ningun runtime ni geolocalizacion se activa.
- A0-A4 documentales con Scope Lock/V5 exact-SHA, A5 de otra Task Javier pendiente; M03 exige codigo y CI real, sin MySQL, eventos, webhooks, DNS, proveedores, QA/main/PROD; rollback git revert.

## 2026-11-15 — V5 C06-03.M02.05 eventos conceptuales editoriales [Javier :00]
- Documenta doce intenciones EV01-EV12, doce invariantes ID01-ID12 y doce pares PN01-PN12 FUTUROS_NO_EJECUTADOS para sitios, moderacion, contacto, horarios y mapas opcionales por tenant.
- Conserva JWT/RBAC, separacion A/B, host/proxy sin autoridad, publicacion CAS, preview noindex, privacidad de ubicacion y fallback textual; ninguna cola, dispatcher, webhook, worker o evento runtime fue creado.
- Auditorias A0-A4 documentales y A5 independiente pendiente; exigir Scope Lock/V5 exact-SHA y readbacks. Sin MySQL, API, mapas de terceros, DNS, QA/main/PROD; rollback git revert.

## 2026-11-15 — V5 C06-03.M02.04 configuracion tipada y flags editoriales/mapas [Javier :50]
- Documenta 20 opciones futuras tipadas, ámbitos plataforma/tenant/site/location, defaults restrictivos y prioridad fail-closed sin código runtime.
- Define 12 pares P/N FUTUROS_NO_EJECUTADOS de seguridad tenant A/B, host/forwarded, RBAC, publicación CAS, preview, contacto, horarios, mapas opt-in y cache.
- Solo contrato documental de A0–A4 con Scope Lock/V5 exact-SHA; A5 otra Task Javier, sin geolocalización, API, migraciones, eventos ni QA/main/PROD; rollback git revert.

## 2026-11-15 — V5 C06-03.M02.03 persistencia documental editoriales [Javier :40]
- Propone doce estructuras conceptuales tenant-bound, diez índices, seis fases condicionadas de migración y doce pares futuros de fixtures de editoriales, contactos, horarios y mapa opcional.
- Conserva separación A/B, host/proxy no confiables, editor/moderador/publicador, preview noindex, SEO/caché y fallback textual sin ubicación privada ni proveedores reales.
- A0–A4 estrictamente documentales, Scope Lock/V5 exact-SHA y A5 de otro Javier pendiente; sin DDL, MySQL, DNS, eventos, QA/main/PROD; rollback git revert.

## 2026-11-15 — V5 C06-03.M02.02 contratos API/DTO editoriales [Javier :40]
- Define diez operaciones y diez DTO conceptuales, dieciséis errores seguros y doce pares futuros positivos/negativos de editoriales, horarios, contactos, sucursales y mapas opcionales.
- Conserva JWT/RBAC editor/moderador/publicador, aislamiento A/B, Host no confiable, preview noindex, mapa opt-in con fallback textual y cero proveedores externos instalados.
- Sólo contrato documental A0–A4 Scope Lock/V5 exact-SHA; A5 otro Task Javier pendiente. Sin endpoints, migraciones, geocodificación, DNS, eventos, QA/main/PROD; rollback git revert.

## 2026-11-15 — V5 C06-03.M02.01 entidades editoriales y mapas [Javier :40]
- Define 15 entidades conceptuales, 16 invariantes de aislamiento/ownership y 12 pares futuros positivos/negativos para sitios editoriales, contacto, horarios, sucursales, moderación y mapas opcionales.
- Conserva los gates G01–G16 y RB01–RB08 de M01.06: host/proxy no confieren permiso; proveedor de mapas opt-in, sin geocodificación, CDN/DNS, SQL, API, UI ni credenciales ejecutadas.
- Entrega documental A0–A4 con Scope Lock/V5 exact-SHA y A5 independiente pendiente; runtime no causal NO_APLICA, sin QA/main/PROD. Rollback git revert normal.

## 2026-11-15 — V5 C06-02.M02.06 handoff documental de landing [Javier :40]
- Consolida cinco contratos M02 con evidencia A5 independiente, 16 gates futuros G01–G16, ocho reversas RB01–RB08 y seis fronteras de implementación Domain/Application/Infrastructure/API/UI/Operación.
- Sin landing instalada, tablas MySQL, migraciones, host binding, eventos, workers, dispatcher, webhooks, DNS/TLS ni servicios; M03 exige código real y CI dirigida.
- A0–A4 solo documental con Scope Lock/V5 exact-SHA y readback, A5 separado pendiente; no QA/main/PROD. Rollback git revert normal.

## 2026-11-15 — V5 C06-03.M01.06 bloqueos y salida de sitios editoriales [Javier :30]
- Consolida cinco contratos M01 con blobs verificables, G01–G16 de aislamiento, permisos, publicación editorial y mapas opcionales, más RB01–RB08 de reversión sin activar proveedores.
- Conserva pendientes los riesgos y decisiones D01–D08, y los 14 pares P/N FUTUROS NO_EJECUTADOS; M02 requiere LISTO+A5 independiente y DAG dinámico, sin liberar M03/QA/PROD.
- Sólo dos archivos Markdown en dev, sin API, mapa, geocoding, pagos, MySQL, R2, DNS ni webhooks; A0–A4 documental con Scope Lock/V5 exact SHA, readback y A5 independiente; rollback git revert.

## 2026-11-15 — V5 C06-02.M02.05 contratos conceptuales de eventos [Javier :40]
- Documenta doce intenciones EV, doce reglas ID de idempotencia/correlación y doce pares PN FUTUROS_NO_EJECUTADOS para landing por tenant; no implementa ni emite eventos, dispatcher, webhook, cola, worker o Task.
- Delimita tenant A/B, Host no confiable, aprobación editor/publicador, publicación CAS, cache/SEO por versión, trazas minimizadas, errores fail-closed y rechazo de secretos/PII.
- A0–A4 documentales y checks Scope Lock/V5 exact-SHA; CI runtime no causal NO_APLICA y A5 independiente pendiente; rollback git revert y sin QA/main/PROD.

## 2026-11-15 — V5 C06-02.M02.04 configuración tipada y flags de landing [Javier :30]
- Define 18 opciones futuras con ámbitos server/global/tenant y defaults fail-closed: host validado, preview noindex/no-store, publicación RBAC/CAS, caché particionada, assets y SEO. No instala flags.
- Propone 12 pares positivos/negativos FUTUROS NO_EJECUTADOS; conserva las fronteras y bloqueos de M01.06/M02.01–03 y distingue tienda vinculada actual de landing CMS aún no desarrollada.
- Cambia solo dos Markdown en dev, sin API, MySQL, DNS/TLS, R2, CDN, eventos, worker ni QA/main/PROD; exige Scope Lock/V5 exact-SHA, A0–A4 y A5 independiente. Rollback git revert.

## 2026-11-15 — V5 C06-02.M02.03 persistencia tenant-first de landing [Javier :40]
- Define documentalmente 13 estructuras, 11 índices/restricciones, seis etapas de migración futura y doce pares positivos/negativos de fixtures para host aprobado, draft, preview, publicación CAS, SEO y caché aislada.
- Preserva G01–G16, RB01–RB08, A/B, RBAC de editor/publicador y prohibición de autoridad desde Host/forwarded-host; no crea tablas, migraciones, endpoints, DNS, workers ni datos reales.
- Exige A0–A4 documental/Scope Lock/V5 exact-SHA y A5 independiente; CI runtime solo NO_APLICA por diff Markdown, rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V5 C00-04.M04.04 verificación real formularios y navegación [Javier :30]
- Cuatro pruebas Vitest Angular sobre ruta privada, retorno, revalidación single-flight, HTTP 403 fail-closed y alta empresarial sin doble envío ni falso éxito antes de respuesta.
- Integra CI Node 24/npm 11 con Vitest dirigido, lint y build de Angular, conservando intactos componentes y guards de otras Tasks; no presenta simulación frontend como COMMIT MySQL real.
- Sólo pruebas UI, sin migraciones, JWT/RBAC, API, ambientes, QA/main/PROD ni A5 propia; Scope Lock/V5 y A5 independiente son necesarios. Rollback por git revert.

## 2026-11-15 — V5 C06-02.M02.02 API/DTO/errores de landing por tenant [Javier :50]
- Contrato documental de diez operaciones futuras, nueve DTO, dieciséis errores seguros y doce pares P/N FUTUROS_NO_EJECUTADOS para landing pública separada por tenant.
- Distingue editor y publicador RBAC, host/forwarded no confiables, preview noindex, CAS de revisión, caché por host+tenant, política SEO y aislamiento A/B, sin endpoints ni autorizaciones nuevas.
- Solo Markdown en dev, Scope Lock/V5 exact-SHA, A0–A4 documentales y A5 independiente; cero MySQL, DNS/TLS, provider R2, eventos, QA/main/PROD; rollback git revert.

## 2026-11-15 — V5 C00-04.M04.03 branding tipado de gobierno [Javier :30]
- Agrega marca de plataforma o variante neutra mediante Angular 22 Signals `input`/`computed`, clases y tokens visuales controlados, sin interpolación de CSS, HTML arbitrario o identidad de tenant.
- Incorpora cuatro pruebas Vitest positivas/negativas (autoridad, fallback, payload hostil y falta de sesión) en un job real Node 24 GitHub Actions; mantiene `plataformaGuard` y consulta de solo lectura.
- Sin API, MySQL, JWT/RBAC, trabajadores, despliegue ni QA/main/PROD; exigir A0-A4 con Scope Lock, build, pruebas dirigidas exact-SHA, A5 ajeno y rollback `git revert`.

## 2026-11-15 — V5 C06-02.M02.01 modelo de landing por tenant [Javier :50]
- Documenta 16 entidades conceptuales y 16 invariantes de ownership de landing/host/preview/publicacion/cache/SEO, con diez pares P/N FUTUROS_NO_EJECUTADOS y aislamiento tenant A/B.
- Alinea G01–G16 y RB01–RB08 de M01.06, roles editor/publicador, host confiable y caché particionada sin afirmar endpoints, MySQL, resolución de host ni publicación real.
- Solo documento y CHANGELOG en dev con A0–A4 Scope Lock/V5 exact SHA y A5 independiente; no DNS/TLS, provider R2, eventos, QA/main/PROD; rollback git revert.

## 2026-11-15 — V5 C00-04.M04.02 reparación de decorator Angular [Javier :20]
- La CI Frontend producción del SHA 30dea308 detectó TS1206 porque el nuevo alias `EstadoGobiernoV5` quedó entre `@Component` y la clase decorada. Se confirma la causa exacta y se corrige su ubicación antes del decorador.
- Conserva la clase, el guard y todas las pruebas positivas/negativas sin deshabilitar controles; requiere nueva CI exact SHA, no se acepta el FAIL anterior como PASS.
- Sin cambios a RBAC/JWT, endpoints, MySQL, ramas ajenas, QA/main/PROD ni permisos.

## 2026-11-15 — V5 C00-04.M04.02 estados y revalidación Angular [Javier :20]
- Implementa cinco estados visibles accesibles para la consulta de gobierno, con sesión validada por servidor, vacío institucional honesto y pantalla fail-closed ante 401/403 o errores de red.
- Revalidación single-flight sin doble petición, sin filtración de payload y sin conceder permisos; agrega cinco tests Vitest positivos/negativos y actualiza la aserción anterior de M04.01 a un único botón seguro.
- Exige CI dirigida Angular, Scope Lock y procedencia V5 sobre SHA causal, readbacks institucionales y A5 independiente; sin API/DB/JWT nuevo, webhooks ni QA/main/PROD.

## 2026-11-15 — V5 C00-04.M04.05 Chromium real responsive y accesibilidad [Javier :50]
- Incorpora cinco regresiones Playwright Chromium para ruta privada del gobierno de cambios: anónimo, 403, 390/1280px, foco/teclado, etiquetas y contraste >=4.5:1.
- CI GitHub-hosted Node24/npm fijo ejecuta Angular lint/build, navegador local y tests dirigidos; la sesión HTTP es sintética solo para la interfaz, no certifica JWT/RBAC ni MySQL reales.
- Mantiene intactos archivos de M04.01 y autorización backend; requiere CI exact-SHA Scope Lock/V5/browser, A5 independiente y rollback git revert; sin QA/main/PROD.

## 2026-11-15 — V5 C03-03.M02.06 dependencias y salida contractual de auditoría [Javier :50]
- Consolida cinco contratos M02 de auditoría de seguridad y prerrequisitos del DAG por ID exacto, con diez gates S01–S10 para M03 y doce pares P/N de aceptación FUTUROS_NO_EJECUTADOS.
- Preserva JWT/RBAC, aislamiento tenant A/B, auditoría strict frente a best-effort, correlación saneada, idempotencia futura, aprobación separada y política de retención no implementada.
- Entrega documental únicamente, sin API/SQL/eventos/worker/webhook ni QA/main/PROD; exige CI Scope Lock/V5 exact-SHA, A0–A4 con readback y A5 independiente, rollback git revert.

## 2026-11-15 — V5 C00-04.M04.01 Vitest dirigido en GitHub Actions [Javier :20]
- Añade al workflow V5 de estados de plataforma una ejecución real en GitHub-hosted Node 24 del nuevo `app.routes.governance-v5.spec.ts`, incluidos guard privado, ruta única, lazy-load y render accesible sin botones operativos.
- Agrega únicamente los paths necesarios para activar la prueba en cambios de componente/spec. Conserva los jobs históricos de permisos y branding, sin deshabilitar comprobaciones ni manipular resultados.
- El CI exact-SHA debe completar SUCCESS antes de A0–A4/LISTO; fallo/queued/cancelled no es PASS. Sin API, MySQL, JWT, QA/main/PROD ni webhook.

## 2026-11-15 — V5 C00-04.M04.01 reconciliación reproducible de catálogo Angular [Javier :20]
- Corrige P0-007.A catalog drift del SHA 1ecaac0c: el componente nuevo de gobierno se había publicado sin registrar su ruta `.component.ts` en el inventario histórico-aditivo.
- Registra exclusivamente la nueva ruta de archivo; mantiene el detector de omisiones, fuentes faltantes y test negativo, actualizando recuento de 140 a 141 (133 features, 8 shared).
- No cambia guards, JWT, RBAC, tenant, API, base de datos ni reglas de QA/main/PROD; CI GitHub del SHA de reparación obligatoria antes de LISTO.

## 2026-11-15 — V5 C00-04.M04.01 reparación causal del control de rutas ERP [Javier :20]
- GitHub Actions ERP - Contratos, pruebas y build falló en el SHA 7124d178 por FRONTEND_ROUTE_SECURITY_CONTRACT: el escáner de rutas mantenía un allowlist literal que no conocía la nueva ruta de plataforma.
- Extiende únicamente el conjunto de rutas exactas de plataforma reconocidas por el auditor, conservando las verificaciones obligatorias de plataformaGuard, JWT autenticado, sesión backend, ámbito Plataforma y esAdministrador verdadero; no permite rutas arbitrarias, públicas, ERP ni tiendas.
- Requiere nueva CI exact-SHA de calidad ERP, Angular, Scope Lock y procedencia V5; no se considera PASS un job fallido, cancelado o aún en progreso, y no se cambia ningún guard ni permiso.

## 2026-11-15 — V5 C00-04.M04.01 ruta protegida de gobierno de cambios [Javier :20]
- Implementa una superficie Angular standalone, OnPush, responsive y accesible para consultar la secuencia contractual de decisiones, sin datos institucionales en vivo ni acciones operativas.
- La nueva ruta lazy `plataforma/gobierno-cambios-v5` exige `plataformaGuard`, que revalida la sesión frente al backend; agrega pruebas Angular positivas/negativas de guard, navegación y ausencia de permisos/operaciones.
- Scope Lock STRICT en dev, A0–A4 con CI de frontend y procedencia V5 exact-SHA; A5 corresponde a otra Task Javier. Sin endpoints, JWT/RBAC, MySQL, eventos, webhooks, QA/main/PROD; rollback `git revert` normal.

## 2026-11-15 — V5 C03-03.M02.05 contrato inerte de eventos e idempotencia de auditoria [Javier :10]
- Documenta 12 eventos futuros, 16 campos de sobre saneado, diez invariantes de retries y 12 pares positivos/negativos FUTUROS_NO_EJECUTADOS para auditoría de seguridad por tenant.
- Separa decision JWT/RBAC backend, correlacion, identidad del efecto e idempotencia; eventos historicos no conceden grants, no exponen PII/JWT y auditoría strict debe fallar cerrado.
- NO IMPLEMENTAR EVENTOS TODAVÍA: solo documento y sección CHANGELOG autocontenida; sin cola, worker, dispatcher, webhook, migración ni QA/main/PROD, con A0-A4 documental, ScopeLock/V5 exact SHA y A5 independiente.

## 2026-11-15 — V5 C03-03.M02.03 persistencia, indices y fixtures conceptuales de auditoría [Javier :10]
- Diseña 12 agregados/ownership de auditoría, ocho índices tenant-bound propuestos, estrategia de migración/rollback FUTURA_NO_EJECUTADA y 10 pares de oráculos P/N sintéticos para MySQL 8.4.
- Preserva auditoría estricta frente a best-effort, separación empresa A/B y plataforma, política de retención, minimización y protección de JWT/PII; no afirma filtrar datos o EXPLAIN verificados en runtime.
- Modifica solo documentación y CHANGELOG autocontenido en dev, sin SQL ni migration real, IAM, workers, endpoints ni QA/main/PROD; exige A0–A4 documental, CI ScopeLock/V5 exact SHA y A5 independiente.

## 2026-11-15 — V5 C03-03.M02.04 configuración tipada y flags de auditoría [Javier :50]
- Define 16 opciones conceptuales, 18 invariantes fail-closed y 10 pares P/N FUTUROS_NO_EJECUTADOS para tenant A/B, JWT/RBAC, redacción, auditoría estricta, límites y retención sin políticas inventadas.
- Contrasta AuditoriaController, AuditoriaService best-effort/estricto, AuditoriaRepository y CorrelationIdMiddleware sin afirmar fuga, acceso excepcional, HTTP/MySQL o retención certificados; ninguna variable, servicio, secreto o base modificados.
- Sólo dos Markdown en dev, fast-forward/CAS, Scope Lock/V5 SHA exacto, A0–A4 documentales y A5 independiente; sin QA/main/PROD, revert normal.

## 2026-11-15 — V5 C03-03.M02.02 contratos de auditoría API/DTO y errores seguros [Javier :10]
- Especifica ocho operaciones actuales o futuras, 14 errores seguros y ocho pares positivos/negativos FUTUROS_NO_EJECUTADOS de auditoría: autorización, cambios críticos, acceso excepcional, correlación y no exposición.
- Preserva la diferencia best-effort/strict de AuditoriaService, control backend JWT/RBAC y tenant A/B, BOLA/IDOR, PII, fail-closed; no confunde correlación con idempotencia ni documento con prueba.
- Cambio exclusivamente documental en dev; sin API ejecutable, EF/MySQL, IAM, worker, webhook, retención activa, QA/main/PROD; Scope Lock/V5 exact-SHA, A0–A4, A5 independiente y rollback git revert.

## 2026-11-15 — V5 C03-03.M02.01 entidades y aislamiento de auditoría de seguridad [Javier :50]
- Documenta 16 entidades conceptuales/owners de auditoría, 18 invariantes A/B, JWT/RBAC, redacción y fallo estricto, 10 fallas y 10 pares P/N FUTUROS_NO_EJECUTADOS.
- Distingue AuditoriaController, AuditoriaService best-effort/estricto y AuditoriaRepository sin afirmar fuga ni aislamiento HTTP/MySQL ya certificado; no cambia código, permisos, retención, consultas, DB ni eventos.
- Dos Markdown en dev con CAS fast-forward, Scope Lock/V5 exact SHA, A0–A4 documentales y A5 de Task Javier distinta; git revert normal, sin QA/main/PROD.

## 2026-11-15 — V5 C11-02.M02.05 idempotencia y correlación de API modular [Javier :00]
- Define 10 contratos de frontera EV, 16 invariantes ID, 10 pares P/N FUTUROS_NO_EJECUTADOS y 8 barreras RB para seguridad tenant/JWT/RBAC, idempotencia, cuotas y correlation IDs sin permisos.
- Revisa API CorrelationIdMiddleware, ApiResponse, ExceptionHandling y servicios Outbox existentes; NO IMPLEMENTAR EVENTOS TODAVÍA, sin worker, relay, SQL, webhook ni llamadas entre Tasks.
- Cambio únicamente documental en dev por fast-forward/CAS, Scope Lock/V5 exact-SHA, A0–A4 reales para Markdown, A5 independiente posterior y rollback git revert; no QA/main/PROD.

## 2026-11-15 — V5 C11-02.M02.04 configuración tipada y flags API modular [Javier :50]
- Contrato documental de 16 opciones tipadas propuestas, 18 validaciones y 10 pares P/N FUTUROS_NO_EJECUTADOS: precedencia plataforma/tenant, JWT/RBAC, soporte lectura, CORS, rate limits, idempotencia y errores sin secretos.
- Contrasta API ASP.NET Core 10, Program.cs, AuthController, EF/MySQL y preparación M01 sin modificar appsettings, DI, endpoints, código, esquema, procesos, permisos, eventos ni infraestructura.
- Dos Markdown en dev, fast-forward/CAS, A0–A4 con Scope Lock/V5 exact-SHA, A5 por otra Task Javier y git revert normal; jamás QA/main/PROD.

## 2026-11-15 — V5 C11-02.M02.03 persistencia/índices/fixtures propuestos para API modular [Javier :00]
- Documenta 12 proyecciones de datos, 8 índices candidatos NO CREADOS, 6 etapas futuras de migración y 10 fixtures positivos/negativos FUTUROS_NO_EJECUTADOS, con aislamiento tenant A/B y compatibilidad de API.
- Coteja AppDbContext, IUsuarioRepository, UsuarioEmpresa, IUsuarioScopeService, PagedRequest y el interceptor de soporte, sin cambios en EF/MySQL, migraciones o datos.
- Solo dos archivos Markdown por fast-forward/CAS en dev; Scope Lock y procedencia V5 exact SHA, A0–A4 documentales, A5 independiente, rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V5 C11-02.M02.01 entidades e invariantes de API modular [Javier :50]
- Consolida 16 entidades lógicas y owners, 18 invariantes fail-closed, 10 fallas y 10 pares P/N FUTUROS_NO_EJECUTADOS con aislamiento tenant A/B, JWT/RBAC, permisos de soporte y DTO seguros.
- Contrasta el código real ASP.NET Core 10, API, Application, EF/MySQL, contrato ApiResponse y gate documental M01 con A5 independiente; no introduce endpoints, clases, esquema, migraciones, workers ni eventos.
- Entrega dos Markdown en dev con fast-forward/CAS, Scope Lock/V5 exact SHA, A0–A4 y A5 de otra Task Javier; sin QA/main/PROD, rollback por git revert normal.

## 2026-11-15 — V5 C11-02.M02.02 contratos API/DTO/errores y compatibilidad [Javier :00]
- Define 10 superficies/operaciones conceptuales, 16 invariantes de seguridad/compatibilidad, 10 pares P/N FUTUROS_NO_EJECUTADOS y 8 medidas de rollback para API modular, con JWT/RBAC/tenant A/B fail-closed.
- Contrasta AuthController, TenantContextController, PlataformaSesionController, UsuariosController, ApiResponse, DTOs, filtros y middleware existentes sin modificar ejecutables, rutas, EF/MySQL ni clientes.
- Documento y CHANGELOG_AI.md únicamente, publicados en dev por fast-forward/CAS; controles Scope Lock/V5 exact SHA, A0–A4 documentales y A5 independiente, sin QA/main/PROD.

## 2026-11-15 — V5 C11-03.M02.06 contrato de salida documental Outbox/eventos [Javier :00]
- Establece 16 barreras de salida, 10 pares P/N FUTUROS_NO_EJECUTADOS y 10 medidas de rollback para la frontera Outbox/eventos; conserva idempotencia, claim tenant-bound, A5 independiente y privacidad sin activar ninguna mensajería.
- Contrasta los cinco contratos C11-03.M02.01–05 con A5 independiente, el M01.06 y C10-04.M01.06; distingue documentación, código vigente y pruebas runtime aún no ejecutadas.
- Solo documento y CHANGELOG_AI.md en dev por fast-forward/CAS; Scope Lock/V5 exact-SHA, A0–A4 documentales y A5 independiente posterior. Sin workers, webhooks, SQL, migraciones, secretos, QA/main/PROD; rollback git revert normal.

## 2026-11-15 — V5 C12-03.M02.06 cierre documental backend/workers [Javier :50]
- Consolidación del contrato M02 de backend/workers: M01.01–06, M02.01–05 y C10-04/C11-03 terminales con A5; 16 gates de seguridad/tenancy/CAS/rollback y 10 escenarios P/N FUTUROS_NO_EJECUTADOS.
- Verifica fronteras reales API ASP.NET Core 10, EF/MySQL y OutboxRetryProcessor existente sin crear procesos, endpoints, eventos, dispatchers, webhooks, migraciones, conectores entre Tasks ni infraestructura.
- Entrega exclusivamente documental; A0–A4, Scope Lock y V5 exact-SHA, A5 por Task Javier distinta de DEV06 antes de sucesoras, sin QA/main/PROD y git revert normal.

## 2026-11-15 — V5 C12-03.M02.05 contrato inerte API/worker eventos [Javier :40]
- Fija 12 campos conceptuales, 16 invariantes A/B/CAS/identidad servicio, 10 políticas de cuota, 10 señales NO_EMITIDAS y 10 pares P/N FUTUROS_NO_EJECUTADOS para la separación API/worker.
- NO IMPLEMENTAR EVENTOS TODAVÍA: no crea bus, relays, scheduler, trabajador, webhook, llamada entre Tasks, SQL/EF, migración, API, proveedor externo ni proceso en ejecución; ninguna autoridad de QA/main/PROD.
- Dos Markdown en dev publicados por CAS fast-forward, Scope Lock/V5 exact-SHA, A0–A4 documental, A5 independiente y rollback con git revert normal.

## 2026-11-15 — V5 C12-03.M02.04 opciones tipadas y flags de workers [Javier :10]
- Describe dieciséis opciones F01-F16, dieciocho validaciones V01-V18 y diez pares P/N FUTUROS_NO_EJECUTADOS para separar backend/workers, identidad de servicio, tenant A/B, readiness, cuota, lease CAS, idempotencia y logs seguros.
- Contrasta OutboxRetryProcessorOptions.Validar(), IMensajeOutboxRepository, Domain MensajeOutbox y API Program.cs; NO cambia opciones reales, DI, procesos, proveedores, MySQL, eventos entre Tasks, cron, webhooks, workers o infraestructura.
- Solo dos Markdown en dev, Scope Lock y procedencia V5 SHA exacto, A0-A4 documentales y A5 independiente; rollback git revert normal, sin QA/main/PROD.

## 2026-11-15 — V5 C12-03.M02.03 persistencia conceptual API/workers [Javier :40]
- Define 12 proyecciones de persistencia, 8 índices candidatos, 6 pasos de migración FUTURA_NO_EJECUTADA y 12 fixtures sintéticos A/B, CAS, idempotencia, UTC, migrador único y rollback.
- No ejecuta SQL, EF, MySQL, migraciones, worker, scheduler, hosting, DNS, secretos, Task nueva, pruebas runtime ni despliegues; contrato solo en Markdown y fuentes Git versionadas.
- Publicación fast-forward/CAS dev, Scope Lock y V5 exact-SHA, A0–A4 documentales, A5 independiente por otra Task Javier; sin QA/main/PROD, revert normal.

## 2026-11-15 — V5 C12-03.M02.02 contratos API DTO y errores de backend-workers [Javier :10]
- Define diez operaciones conceptuales, catorce campos DTO propuestos, dieciséis errores seguros y diez pares positivos/negativos FUTUROS_NO_EJECUTADOS de separación API/worker, tenant A/B, JWT/RBAC, idempotencia y CAS.
- Comprueba contratos reales de MensajeOutbox, IMensajeOutboxRepository, OutboxRetryProcessor y API Program.cs; NO implementa APIs, trabajadores, relays, scheduling, webhooks, migraciones, infraestructura, proveedores ni eventos entre Tasks.
- Solo dos Markdown en dev, control Scope Lock y procedencia V5 SHA exacto, A0-A4 documentales, A5 por otra Task Javier; git revert normal y ninguna autorización QA/main/PROD.

## 2026-11-15 — V5 C12-03.M02.01 ownership conceptual API y workers [Javier :40]
- Documenta 12 componentes/futuros responsables, 16 invariantes tenant A/B, CAS, JWT/RBAC, no HttpContext, idempotencia y migrador único, 8 contratos de falla y 10 pares P/N FUTUROS_NO_EJECUTADOS.
- Sin implementar workers, scheduler, dispatcher, webhooks, Tasks, SQL/EF, migraciones, hosting ni cambios de API; fuentes NET10/EF/Outbox existentes referenciadas por blobs y ausencia de permiso PROD explícita.
- Cambia solo doc V5 y CHANGELOG en dev, bajo CAS fast-forward, Scope Lock y V5 exact SHA, A0–A4 documentales, A5 por otra Task Javier y rollback git revert normal; no QA/main/PROD.

## 2026-11-15 — V5 C12-03.M01.06 gates rollback backend/workers [Javier :30]
- Cierra preparación M01 documental de backend/workers separados con 16 NO_GO preventivos, diez pasos de rollback y ocho pares P/N FUTUROS_NO_EJECUTADOS, ligados a quince fuentes e identidad tenant fuera de HttpContext.
- Mantiene explícitos bloqueos JWT/RBAC, A/B, datos, MySQL, worker, retry idempotente, migrador único, secretos, readiness, proveedor y continuidad; no activa ningún servicio, topología, migración, DNS o QA/main/PROD.
- Requiere Scope Lock y procedencia V5 completed/success exact-SHA, A5 independiente, readback institucional y revert normal antes de nuevos gates.

## 2026-11-15 — V5 C11-03.M02.05 contrato inerte de eventos y límites [Javier :40]
- Define 12 campos conceptuales, 16 invariantes de tenant/idempotencia/CAS, 10 límites, 10 señales NO EMITIDAS y 10 pares de prueba FUTUROS_NO_EJECUTADOS sobre el outbox existente.
- NO IMPLEMENTAR EVENTOS TODAVÍA: sin bus, dispatcher, worker, webhook, cron, scheduler, llamadas entre Tasks, SQL/EF, endpoints, credenciales, proveedores, métricas emitidas, QA/main/PROD ni efectos externos.
- Cambios solo en dos Markdown de dev, Scope Lock y procedencia V5 exact-SHA, A0-A4 documentales, A5 independiente y rollback por git revert normal.

## 2026-11-15 — V5 C11-03.M02.04 configuración tipada y flags seguros del outbox [Javier :10]
- Define catorce opciones conceptuales F01-F14, dieciséis validaciones V01-V16 y diez pares P/N FUTUROS_NO_EJECUTADOS con defaults fail-closed, aislamiento tenant A/B, JWT/RBAC, cuota, CAS, idempotencia y privacidad.
- NO IMPLEMENTAR EVENTOS TODAVIA: ninguna variable real, HostedService, worker, dispatcher, webhook, API, EF/MySQL, cambio de proveedor, credencial o relé habilitado; coteja las fuentes Domain/Application y contratos C11-03.M01/M02 vigentes.
- Dos Markdown en dev; A0-A4 documentales con Scope Lock y procedencia V5 exact-SHA, A5 independiente de otra Task Javier, readbacks institucionales y rollback por git revert normal; no QA/main/PROD.

## 2026-11-15 — V5 C11-03.M02.03 persistencia e índices conceptuales outbox [Javier :40]
- Define 12 proyecciones de persistencia, 8 índices candidatos, 6 etapas de migración condicionadas FUTURAS_NO_EJECUTADAS y 12 fixtures sintéticos positivos/negativos, con tenant A/B, CAS, UTC, idempotencia, privacidad y rollback.
- Coteja entidades, repositorio EF, contrato de Application, procesador outbox y prueba MySQL de concurrencia solamente como fuentes versionadas; NO ejecuta DDL, EF/MySQL, cambios de entidad, worker, relay, dispatcher, webhooks, eventos entre Tasks o proveedores externos.
- Changeset documental acotado a dos Markdown en dev; A0–A4 y Scope Lock/V5 exact-SHA, A5 independiente de otra Task Javier, rollback por git revert normal; sin QA/main/PROD.

## 2026-11-15 — V5 C11-03.M02.02 contratos conceptuales API DTO outbox [Javier :30]
- Fija OP01-OP10 con las operaciones existentes y propuestas del outbox, E01-E16 para errores seguros, ocho positivos y doce negativos FUTUROS_NO_EJECUTADOS; coteja entidad MensajeOutbox, servicio RegistrarAsync y repositorio con CAS.
- Preserva aislamiento tenant A/B, JWT/RBAC, idempotencia, versionado y compatibilidad; no implementa endpoints, proveedores, colas, eventos, dispatcher, migraciones MySQL ni tasks nuevas.
- Publicacion fast-forward/CAS, Scope Lock y procedencia V5 exact-SHA, A5 independiente, BITACORA y readback; rollback git revert sin QA/main/PROD.

## 2026-11-15 — V5 C11-03.M02.01 entidades y ownership conceptual outbox [Javier :40]
- Define 14 elementos conceptuales, 16 invariantes tenant-bound/idempotencia/CAS/UTC/privacidad y 12 pares P/N FUTUROS_NO_EJECUTADOS para C11-03; verifica cinco blobs de código y documentos del contrato previo.
- Conserva los contratos outbox existentes y el A5 independiente de M01.06; no crea tablas, migraciones, workers, webhooks, relays, dispatcher, eventos entre Tasks, endpoints, proveedores ni cambios de runtime.
- Solo documento V5 y CHANGELOG en dev, Scope Lock/validación de procedencia exact-SHA, A0–A4 documental, A5 por otra Task Javier y rollback por git revert normal; nunca QA/main/PROD.

## 2026-11-15 — V5 C11-02.M01.06 gates, rollback y salida de API modular [Javier :30]
- Cierra contrato documental terminal de M01 con G01-G16, seis escenarios positivos y diez negativos FUTUROS_NO_EJECUTADOS, ocho pasos de rollback normal, autorizacion backend JWT/RBAC/tenant A/B y compatibilidad de API.
- Delimita fuentes M01.01-.05 ya LISTO con revisiones A5 independientes y C01-03.M01.06; no asume que el documento ejecuta API, MySQL, migraciones, workflows, eventos ni QA/main/PROD.
- Scope Lock y procedencia V5 exact-SHA exigibles, CI runtime no causal NO_APLICA, A5 por otra Task Javier, BITACORA y readback por transicion; revert normal sin force-push.

## 2026-11-15 — V5 C15-05.M03.06 correccion build CS0246 [Javier :30]
- Repara error real de GitHub-hosted Backend Release y authorization gate: incorpora using Solqaryn.Domain.AutomationV5 para V5OperationalAuditSecurityGate en la envoltura de observabilidad.
- Mantiene sin cambios clasificacion de logs, denegacion por defecto, tenant A/B, pruebas xUnit y controles de seguridad; no desactiva analizadores ni atribuye los jobs failed/cancelled como PASS.
- CI backend/authorization/seguridad/Scope Lock/V5 debe ejecutarse en nuevo SHA antes de A4; rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V5 C12-04.M02.05 contrato conceptual de senales MySQL cutover [Javier :20]
- Define dieciseis campos de sobre, diez senales FUTURAS_NO_EMITIDAS, dieciseis invariantes de tenancy/idempotencia/CAS y diez pares P/N de ensayo y rollback NO EJECUTADOS.
- NO IMPLEMENTAR EVENTOS TODAVIA: no se activa dispatcher, worker, webhook, Task, API, EF/MySQL, backup, restore, migracion o DNS; Hostinger/Cloudflare no activados.
- Dos Markdown solo en dev, Scope Lock/V5 exact-SHA, A0-A4 documentales, A5 independiente pendiente, sin QA/main/PROD.

## 2026-11-15 — V5 C12-04.M02.04 opciones seguras de cutover MySQL privado [Javier :10]
- Define catorce opciones tipadas, catorce validaciones y diez pares P/N futuros de integridad, autoridad, tenant A/B, CAS, restore efímero y separacion de aprobaciones.
- Mantiene flags de cutover, escrituras, restore y producción apagadas, sin modificar configuración real, DNS, credenciales, Aiven/Render/MySQL ni ejecutar migraciones o pruebas inexistentes.
- Entrega solo docs/v5 y CHANGELOG, requiere Scope Lock/V5 exact-SHA, A0–A4 documental, A5 otra Task Javier y nunca QA/main/PROD.

## 2026-11-15 — V5 C15-05.M03.06 logs estructurados y errores seguros [Javier :30]
- Agrega envoltura .NET 10 Infrastructure de observabilidad para gateway read-only V5, con EventId 150506-150509 y codigos de auditoria estructurados de vocabulario fijo, sin exponer tenant, principal, SHA, secretos ni excepciones.
- Incluye cinco pruebas xUnit positivas/negativas de verificacion externa obligatoria, aislamiento tenant A/B, errores redactados, cancelacion y dependencias nulas; pruebas CI GitHub-hosted requeridas, no se atribuyen como PASS antes de completion.
- No conecta proveedor real, migraciones, dispatcher, eventos, webhooks, API, A5 propia, QA/main/PROD; Scope Lock y procedencia V5 exact-SHA, posterior readback, y rollback por git revert normal.

## 2026-11-15 — V5 C12-04.M02.03 contrato documental de persistencia indices y fixtures [Javier :20]
- Registra doce proyecciones conceptuales, catorce invariantes y seis pasos expand/contract FUTUROS_NO_EJECUTADOS; no crea entidades EF, SQL, DDL, migraciones ni acceso Aiven/Hostinger.
- Diez pares P/N futuros requieren version EF/MySQL, control tenant A/B, manifest, hash, FK, migrador unico y ensayo de restore efimero con rollback; no se atribuyen tests realizados.
- Solo docs/v5 + CHANGELOG en dev con Scope Lock/V5 exact-SHA, A0-A4 documentales, A5 independiente; no QA/main/PROD ni cambios de proveedores.

## 2026-11-15 — V5 C12-04.M02.02 contrato conceptual API/DTO de cutover MySQL privado [Javier :10]
- Propone nueve operaciones, diez DTO, catorce errores saneados y ocho pares P/N futuros para preflight, manifiestos, comparaciones, restore efímero y aprobación separada de cutover.
- Mantiene rutas simbólicas NO_IMPLEMENTADAS y control tenant JWT/RBAC, checksum, SHA, CAS/lease, revocación y fail-closed; sin acciones sobre Aiven/MySQL productivo ni migraciones.
- Documento y CHANGELOG únicamente en dev, Scope Lock/V5 SHA exacto, A0–A4 documental, A5 independiente; prohíbe QA/main/PROD, DNS, secrets y permisos comerciales.

## 2026-11-15 — V5 C12-04.M02.01 entidades ownership MySQL privado cutover documental [Javier :20]
- Define doce entidades conceptuales, dieciseis invariantes de tenant/PK/FK/manifest/privilegios y diez pares positivos-negativos FUTUROS_NO_EJECUTADOS, sin inventar resultados MySQL.
- Conserva compatibilidad .NET 10/EF Core 10.0.12/MySql.EntityFrameworkCore 10.0.9, Verified* y autorizacion de migrador unico; Hostinger/Cloudflare son objetivos NO ACTIVADOS.
- Solo docs/v5 y CHANGELOG_AI.md en dev; Scope Lock/V5 exact SHA, A0-A4 documentales, A5 independiente; sin DDL, dump, backup, restore, QA/main/PROD o proveedores.

## 2026-11-15 — V5 C11-03.M01.06 puertas de salida y reversión de outbox/eventos [Javier :10]
- Consolida B01–B14 de aislamiento/actor/tenant, CAS/lease, idempotencia, orden, cuota y seguridad de payload; define RB01–RB10 de contención y rollback futuro.
- Propone diez pares P/N de aceptación FUTUROS_NO_EJECUTADOS, conserva contratos Outbox existentes, exige A5 independiente antes de M02 y CI real para código M03.
- Entrega documental, sin crear relay, webhooks, workers, migraciones, colas, proveedores o QA/main/PROD; requiere Scope Lock/V5 exact-SHA y BITACORA/PLAN/COLA readback.

## 2026-11-15 — V5 C10-04.M02.05 contrato conceptual de señales e idempotencia [Javier :20]
- Documenta dieciseis campos, diez senales FUTURAS_NO_EJECUTADAS, dieciseis invariantes y diez pares de oraculos P/N sobre tenant A/B, revocacion, lease CAS, retry, privacidad y rollback.
- NO IMPLEMENTAR EVENTOS TODAVIA: no crea outbox, scheduler, dispatcher, consumidor, webhook, cron, Tasks, APIs, migraciones, proveedores ni escrituras externas.
- Solo docs/v5 + CHANGELOG_AI.md en dev, Scope Lock/V5 exact SHA, A0-A4 documentales y A5 independiente pendiente; sin QA/main/PROD.

## 2026-11-15 — V5 C10-04.M02.02 API/DTO y denegaciones futuras de cache, jobs, respaldo y exportación [Javier :40]
- Define únicamente ocho rutas REST propuestas, DTO internos/externos y respuestas de error fail-closed: la pertenencia y permisos tenant A/B provienen del servidor, nunca de EmpresaId/slug/header aportados por cliente.
- Consolida doce oráculos positivos/negativos futuros NO_EJECUTADOS para retries, claves idempotentes, leasing, CSV formula injection, enlaces temporales, manifiestos backup y restore exclusivamente efímero.
- Coteja IPublicStoreCache, OutboxRetryProcessor e ICompraDocumentoStorageService sin modificarlos. Solo diseño Markdown; Scope Lock/V5 exact-SHA y A5 independiente, sin APIs, DDL/MySQL, workers, R2, secretos, QA/main/PROD.

## 2026-11-15 — V5 C10-04.M02.04 opciones tipadas, ámbitos y flags seguros documentales [Javier :10]
- Define catorce propuestas tipadas, 14 reglas V01–V14 y diez pares P/N de caché, jobs, backups y exportaciones, vinculadas a interfaces existentes en dev.
- Mantiene caché privada, dispatcher, exportaciones, R2 y restore apagados; sin activar flags ni migraciones, ni inventar cuotas medidas, SQL, HTTP o reintentos reales.
- Solo docs/v5 y CHANGELOG, Scope Lock y procedencia V5 exact SHA, A0–A4 documentales y A5 independiente; prohibidos secretos, QA/main/PROD y alteraciones de Tasks.

## 2026-11-15 — V5 C10-04.M02.03 contrato documental de persistencia e indices cache/jobs/backups [Javier :20]
- Define nueve proyecciones conceptuales, doce invariantes y seis pasos expand/contract FUTUROS_NO_EJECUTADOS, sin DDL, EF migration, backfill ni acceso a MySQL/Aiven.
- Incluye diez pares de fixtures positivos y negativos futuros para tenant A/B, CAS, idempotencia, exportaciones, auditoria y restore efimero; ninguna prueba o restauracion runtime se atribuye a esta entrega.
- Solo docs/v5 y CHANGELOG_AI.md en dev, Scope Lock/procedencia V5 exact SHA, A0-A4 documentales y A5 independiente; sin proveedores, QA/main/PROD ni eventos.

## 2026-11-15 — V5 C10-04.M02.01 modelo documental de entidades y ownership cache/jobs/backup/export [Javier :20]
- Documenta doce objetos logicos, 18 invariantes tenant/RBAC/lease/idempotencia/integridad, y diez pares de oraculos POS/NEG FUTUROS_NO_EJECUTADOS con fuentes de codigo verificadas.
- No crea tablas EF/MySQL, API, jobs, dispatcher, eventos, buckets, backups, exportaciones ni permisos; solo docs/v5 y CHANGELOG, A5 independiente pendiente.
- Exigir Scope Lock y procedencia V5 exact-SHA, A0-A4 documentales y readback PLAN/COLA/DAG/BITACORA antes de LISTO; no QA/main/PROD.

## 2026-11-15 — V5 C05-04.M03.01 reglas de dominio de presupuesto y caché tenant [.NET10 Javier :10]
- Implementa guardia C# real para aislamiento de caché A/B, pertenencia, revocación, protección de recursos privados, revisiones y evidencia de build por SHA exacto.
- Añade ocho pruebas xUnit de rechazo de autoridad cliente, respuestas 401/403 ocultas, datos JWT/PII en telemetría, budgets de Angular excedidos o advertidos y pruebas sin medición.
- Ningún CWV, browser ni HTTP real se reporta medido; sin API, DI, EF/MySQL, Angular, cambios de budgets, webhooks, QA/main/PROD; exige CI .NET/seguridad/Scope Lock/V5 y A5 ajena.

## 2026-11-15 — V5 C02-04.M03.01 reglas Domain de consola global fail-closed [.NET 10 Javier :10]
- Implementa tres prevalidaciones de solo lectura para operador de tenant, directorio de plataforma y acceso excepcional con grant temporal, MFA, reautenticación, auditoría y doble control.
- Añade ocho pruebas xUnit de aislamiento tenant A/B, ausencia de grants, revocación, manipulación cliente, ventana temporal y autoaprobación; la salida nunca autoriza ejecución runtime.
- No registra DI/API, cambia JWT/RBAC live, MFA real, EF/MySQL, datos, secretos, eventos ni QA/main/PROD; exige CI .NET/seguridad/Scope Lock/V5 exact SHA y A5 independiente antes de LISTO.

## 2026-11-15 — V5 C07-04.M03.03 puerta de autorización comercial fail-closed [.NET 10 Javier :40]
- Añade un gate Application que recibe identidad y pertenencia backend verificadas, propiedad persistida tenant A/B y catálogo de permisos, sin aceptar del cliente TrustedContext ni snapshots de autorización.
- Integra Domain y el servicio M03.02 para validar operaciones, versión, moneda, importe, estado, idempotencia y controles PSP/fiscales; nueve xUnit positivos/negativos, sin ejecutar mutaciones comerciales.
- El resultado positivo exige adaptador autenticado durable y A5 independiente; no añade HTTP, EF/MySQL, JWT/RBAC live, secretos, pagos, facturas, eventos, QA/main/PROD. CI exact SHA .NET/seguridad/Scope Lock/V5 requerida.

## 2026-11-15 — V5 C14-01.M03.02 caso Application de aceptación funcional y seguridad [.NET 10 Javier :40]
- Implementa caso real Application que compone reglas Domain para solicitudes de aceptación con tenant, ownership, rol vigente, acción/caso allowlisted, SHA y presupuesto finito de observaciones.
- Añade nueve xUnit positivos/negativos de revocación, tenant A/B, casos y acciones no autorizados, ausencia de catálogo, lote nulo/excesivo, privacidad, rollback, idempotencia y efectos productivos.
- No configura DI/API, backend IAM live, pagos, EF/MySQL, UI, secrets ni QA/main/PROD; exige CI backend/seguridad, Scope Lock, procedencia V5 exact SHA y A5 independiente antes de declarar cierre.

## 2026-11-15 — V5 C13-04.M03.02 caso Application para consistencia visual multiempresa [.NET 10 Javier :40]
- Implementa servicio real de Application sobre las reglas Domain C13-04.M03.01, con validación server-side de tenant, actor, permisos, catálogo de pantallas, SHA, lote máximo y rechazo de observaciones inconsistentes.
- Agrega nueve xUnit positivos/negativos con tenant A/B, revocación, catálogo vacío, límites de capacidad, superficie forjada, teclado, contraste, privacidad y SHA; solo prepara revisión externa y A5 independiente.
- No incorpora endpoints, DI runtime, UI, migraciones, JWT/roles nuevos, Playwright, accesos productivos, eventos, webhooks o permisos de publicación; exige CI .NET/seguridad, Scope Lock y procedencia V5 exact SHA antes de LISTO_DEV.

## 2026-11-15 — V5 C02-04.M02.06 salida documental de consola global [Javier :00]
- Consolida cinco contratos M02 revisados, dieciseis gates y diez pares positivos/negativos FUTUROS_NO_EJECUTADOS para seguridad de consola global y aislamiento tenant A/B.
- Exige IAM/MFA, JWT/RBAC, pruebas y autorización posterior antes de cualquier implementación; no crea API, consola, EF/MySQL, cuentas, flags, eventos ni elevaciones.
- Publicación documental en dev con Scope Lock y procedencia V5 exact SHA; A0–A4 causales, A5 pendiente de otra Task Javier, rollback Git normal; sin QA/main/PROD.

## 2026-11-15 — V5 C02-03.M02.06 salida documental de lifecycle tenant [Javier :00]
- Consolida cinco contratos M02 y A5 independientes de C02-03 con dieciseis controles OUT y diez pares positivos/negativos FUTUROS_NO_EJECUTADOS sobre lifecycle y permisos de plataforma.
- Mantiene M03 sujeto a M02.06 LISTO+A5 y C02-02.M06.06; no implementa endpoints, EF/MySQL, flags, eventos, restauracion, workers ni cambios de empresa.
- Documento y CHANGELOG por fast-forward/CAS en dev, Scope Lock/procedencia V5 exact SHA y A5 pendiente por otra Task; ninguna autorizacion QA/main/PROD.

## 2026-11-15 — V5 C02-02.M02.06 contrato de salida shared/dedicated [Javier :00]
- Consolida cinco contratos M02 con A5 independiente, dieciseis condiciones SAL y diez pares positivos/negativos FUTUROS_NO_EJECUTADOS para placement con aislamiento tenant A/B.
- Deja M03 bloqueado hasta sus prerequisitos LISTO+A5, pruebas runtime dirigidas y autorizaciones; no hay router, EF/MySQL, eventos, flags, backup ni restore ejecutados.
- Entrega documental en dev, Scope Lock y procedencia V5 exact SHA, A0–A4 dirigidas, A5 pendiente por otra Task Javier y rollback Git normal; sin QA/main/PROD.

## 2026-11-15 — V5 C12-04.M01.06 salida M01 MySQL/cutover documental [Javier :40]
- Consolida M01.01–05 LISTO+A5 y blobs originales, dieciséis bloqueos B01–B16 de exportación, restore, aislamiento tenant, checks y autorización humana futura.
- Define doce pares positivos/negativos FUTUROS_NO_EJECUTADOS y diez rutas RB de recuperación sin afirmar DDL, dump, backup, restore, métricas RTO/RPO ni cutover efectuados.
- Solo dos Markdown en dev: Scope Lock y procedencia V5 exact SHA, A0–A4 documentales, A5 independiente y rollback Git; no MySQL live, R2, DNS, QA/main/PROD ni pagos.

## 2026-11-15 - V5 C14-01.M03.01 preflight de aceptacion funcional .NET 10 [Javier :50]
- Agrega regla pura Domain para validar autoridad tenant/permiso/ownership, SHA, caso y accion allowlisted, evidencia, casos positivo-negativo y reversibilidad sin autorizar efectos ni QA.
- Suma 11 xUnit positivos y negativos de tenant A/B, rol revocado, datos privados, SHA, prueba incompleta, pago, rollback e idempotencia; se requiere CI backend/security y Scope Lock/V5 del SHA real.
- Sin cambios HTTP, EF/MySQL, Angular, PSP, secretos, eventos ni QA/main/PROD; A5 por otra Task Javier y rollback por git revert.

## 2026-11-15 - V5 C13-04.M03.01 consistencia visual de dominio [.NET 10 Javier :50]
- Agrega validacion puramente de dominio de tenant/permiso/SHA/superficie/estado/viewport, teclado, contraste y privacy; nunca autoriza acceso, WCAG ni release.
- Pruebas xUnit positivas/negativas por tenant A/B, revocacion, manipulacion, datos sensibles, UI state y contraste; requiere CI backend, seguridad, Scope Lock y V5 exact SHA.
- Sin cambios Angular, Playwright, API, JWT real, MySQL, DI, worker, webhooks ni QA/main/PROD; A5 independiente otra Task y rollback git revert.

## 2026-11-15 — V5 C14-04.M03.03 puerta de autorización DEV .NET 10 [Javier :40]
- Incorpora gate Application con identidad autenticada sólo desde backend confiable, membresía, grant explícito, rol revisor independiente, tenant A/B y SHA/CI causal fail-closed antes de revisión de release DEV.
- Agrega xUnit positivos/negativos de revocación, cross-tenant, task no autorizada, QA/PROD, impersonación, tests cancelados y SHA distinto; autorización de despliegue y escritura sigue siempre false.
- Sin proveedor de autenticación runtime conectado, HTTP/MySQL, secretos, cambios de IAM, flags, webhooks o QA/main/PROD; exige Backend Release, seguridad, Scope Lock/V5 exact SHA y A5 independiente antes del cierre.

## 2026-11-15 — V5 C00-04.M03.06 auditoría de gobierno .NET 10 [Javier :50]
- Implementa envoltura Application de consulta de gobierno con logging estructurado de resultado y denegaciones, reutilizando el guard tenant/RBAC actual; ningún log concede aprobación, escritura ni ejecución.
- Agrega nueve xUnit positivos/negativos, incluidos tenant A/B sin I/O, revocación, entrada manipulada, auditoría deshabilitada y error del proveedor; exige CI backend, seguridad, Scope Lock y procedencia V5 del SHA exacto.
- Sin integración HTTP, MySQL, DI, dispatcher, webhook, Workers, secreto ni QA/main/PROD; A5 revisada exclusivamente por otra Task Javier; rollback con git revert normal.

## 2026-11-15 — V5 C13-04.M02.06 contrato de salida UX y DAG [Javier :40]
- Consolida seis documentos M01 y cinco M02 con A5 independiente, blobs Git y gates dinámicos antes de habilitar código real M03.
- Define dieciséis barreras y doce pares positivos/negativos FUTUROS_NO_EJECUTADOS para rol/tenant A-B, responsive, estados, foco, idempotencia y SHA de pruebas.
- Sólo Markdown/CHANGELOG, sin UI/API/DB/eventos runtime; Scope Lock y V5 exact SHA, A0-A4, A5 por otro Task Javier, rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V5 C15-03.M03.03 gate de autorización tenant .NET 10 [Javier :20]
- Incorpora preautorización .NET 10 fail-closed con evidencia servidor/tenancy/ownership/permiso, SHA exacto y composición Domain M03.01; nunca concede query bypass, lecturas reales ni escrituras institucionales.
- Agrega nueve xUnit positivos/negativos para tenant A/B, mismo-tenant distinto recurso, revocación, sucursal, enlaces, jobs, control plane y SHA; requiere CI backend, seguridad y Scope Lock/V5 exact-SHA.
- Sin adaptador de identidad/persistencia conectado, HTTP/EF/MySQL, migración, secreto, webhook ni QA/main/PROD; el riesgo de TenantQueryBypassGuard sigue abierto y A5 exige revisor distinto.

## 2026-11-15 — V5 C06-04.M01.06: salida de preparación del portal de cliente [Javier :20]
- Relaciona cinco fuentes documentales originales y documenta B01–B16 bloqueos de seguridad, H01–H08 de contrato M02 y RB01–RB08 reversión del portal consumidor multiempresa.
- Preserva cuenta storefront ya existente sin certificar recuperación, pedidos ajenos, membresías ni descargas; pruebas P/N FUTURAS NO_EJECUTADAS, A/B y RBAC servidor pendientes.
- Solo dos Markdown en dev, Scope Lock y V5 exact SHA antes de LISTO, A5 independiente posterior; sin APIs, migraciones, providers, webhooks, secrets ni QA/main/PROD.

## 2026-11-15 — V5 C06-02.M01.06: contrato de salida de landing por tenant [Javier :20]
- Cierra el contrato documental M01 con 16 bloqueos G01–G16, seis reglas de handoff M02 y ocho rutas de rollback RB01–RB08; liga cinco documentos originales por sus blobs Git.
- Conserva como futuros NO_EJECUTADOS los oráculos positivos/negativos, aislamiento A/B, host binding, noindex, RBAC y pruebas HTTP/MySQL/Playwright; no activa landing, dominios, DNS, R2, eventos ni despliegues.
- Sólo dos Markdown en dev con control de Scope Lock/V5 exact-SHA, evidencias A0–A4 y A5 independiente pendiente; revert normal sin modificar historia BITACORA.

## 2026-11-15 — V5 C15-05.M03.05 fix CS0234 alias xUnit [Javier :10]
- Corrige el conflicto de nombres en tests Infrastructure: el alias Domain colisionaba con Solqaryn.Tests.Domain y resolvia WorkItem al namespace equivocado.
- Renombra exclusivamente el alias a AuditRules y su constructor de WorkItem, sin cambios de reglas de negocio, permisos, adaptador, eventos ni migraciones.
- Requiere nuevamente CI backend y seguridad reales, Scope Lock y V5 exact-SHA antes de A4; no convierte el fallo original en PASS ni libera A5 propia.

## 2026-11-15 — V5 C15-05.M03.05 frontera de proveedor read-only de auditoria [Javier :10]
- Incorpora interfaz y gateway .NET 10 para snapshots de evidencia con comprobacion previa de tenant, SHA, seis Task IDs, revocacion y denegacion de escritura.
- Anade xUnit positivos/negativos para respuesta ajena A/B, origen obsoleto, A5 faltante, error redactado y cancelacion; un snapshot completo sigue exigiendo AUTHENTICATED_EVIDENCE_REQUIRED.
- No conecta proveedores reales, workers, colas, eventos, webhooks ni DB; requiere CI .NET/seguridad, Scope Lock/V5 exact SHA y A5 independiente; rollback por git revert.

## 2026-11-15 — V5 C05-02.M02.06 contrato de salida de branding y presets [Javier :10]
- Fija los prerrequisitos LISTO + A5 de M01/M02 y C01-03.M01.06, con evidencia de reparacion de M01.02 y preservacion del A5_FAIL historico; no fuerza DAG.
- Define 16 gates G01-G16 y seis positivos/doce negativos FUTUROS_NO_EJECUTADOS para tenancy A/B, RBAC, CAS, seguridad visual, idempotencia, accesibilidad y auditoria.
- Solo documento en dev; sin presets runtime, endpoints, SQL, migraciones, eventos, workers, webhooks ni QA/main/PROD; Scope Lock y V5 exact-SHA, A5 independiente y rollback por git revert.

## 2026-11-15 — V5 C05-02.M02.05 contratos futuros de eventos branding [Javier :50]
- Define diez senales S01-S10 NO EMITIDAS, 16 campos F01-F16 y catorce invariantes I01-I14 de correlacion, CAS, idempotencia, revocacion y tenant A/B.
- Especifica cuatro escenarios positivos y diez negativos FUTUROS_NO_EJECUTADOS sin instalar eventos, dispatcher, worker, webhook ni Tasks nuevas.
- Solo Markdown dev con Scope Lock/V5 exact SHA y A5 independiente; no toca API, SQL, Angular ni QA/main/PROD.

## 2026-11-15 — V5 C05-02.M02.04 configuracion tipada y flags seguros [Javier :50]
- Distingue TemaVisual global y ConfigEmpresa tenant existentes; define 14 claves conceptuales K01-K14 con defaults denegados y seis niveles de precedencia.
- Registra 14 reglas fail-closed R01-R14 y 4 positivos/10 negativos FUTUROS_NO_EJECUTADOS de cambio A/B, CAS, assets, accesibilidad y scopes.
- Solo Markdown dev, sin flags activos ni API/SQL/Angular; Scope Lock y V5 exact SHA con A5 independiente, rollback git revert.

## 2026-11-15 — V5 C05-02.M02.03 diseno de persistencia branding y presets [Javier :50]
- Distingue TemaVisual global de ConfigEmpresa tenant real; propone 10 estructuras logicas, 10 constraints y 8 etapas FUTURAS_NO_EJECUTADAS de migracion reversible.
- Define ocho fixtures sinteticos futuros con aislamiento A/B, CAS, tokens seguros, versionado y rollback; no crea SQL, EF, datos reales ni indices.
- Solo Markdown dev con Scope Lock/V5 del SHA exacto y A5 independiente; prohibida implementacion de migraciones o QA/main/PROD desde este documento.

## 2026-11-15 — V5 C05-02.M02.01 entidades branding por empresa y presets [Javier :50]
- Distingue TemaVisual GLOBAL de Empresa/ConfigEmpresa reales y define 14 objetos logicos E01-E14 de branding tenant propuesto, sin materializar tablas o endpoints.
- Documenta 16 invariantes I01-I16 y 4 positivos/10 negativos FUTUROS_NO_EJECUTADOS para ownership, CAS, contraste, revisiones, assets y contexto A/B.
- Solo Markdown dev; Scope Lock y V5 exact-SHA, A5 independiente; sin Angular, MySQL, API, workers, migraciones ni QA/main/PROD.

## 2026-11-15 — V5 C02-03.M02.05 eventos futuros idempotencia y correlacion [Javier :50]
- Especifica 10 senales conceptuales NO EMITIDAS, 16 campos de sobre propuesto y 14 invariantes de entrega, versionado CAS, revocacion y compensacion.
- Registra cuatro casos positivos y diez negativos FUTUROS_NO_EJECUTADOS; nunca emplea tenant, host, DSN o rol cliente como autoridad ni introduce bus, dispatcher, worker o webhook.
- Documentacion solo dev con Scope Lock/V5 del SHA exacto, A5 independiente y rollback git revert; no modifica .NET, Angular, MySQL, QA/main/PROD.

## 2026-11-15 — V5 C02-03.M02.03 contrato de persistencia e idempotencia de lifecycle [Javier :40]
- Establece cinco modelos FUTUROS, ocho índices, seis pasos de migración condicionada y doce fixtures A/B, reintento, fallo parcial y retención sin ejecutar SQL ni desplegar.
- Contrasta blobs de Empresa, UsuarioEmpresa, ContextoTenantActual y UsuarioScopeService; distingue Activa y Eliminado NotMapped de un soft-delete real, mantiene unicidad y segregación de actores.
- Preparación documental con Scope Lock y procedencia exact-SHA requeridos, A5 independiente; migraciones EF/MySQL y JWT/RBAC siguen prohibidos sin gates M03; rollback git revert solo dev.

## 2026-11-15 — V5 C02-03.M02.01 modelo de ownership e invariantes lifecycle [Javier :50]
- Define 14 objetos logicos E01-E14 diferenciando Empresa y UsuarioEmpresa existentes de propuestas futuras; relaciona propiedad empresarial y versionado/idempotencia sin migraciones.
- Especifica 16 invariantes I01-I16 y 12 escenarios documentales P/N FUTUROS_NO_EJECUTADOS para rol plataforma, tenant A/B, revocacion, retencion, carreras y rollback.
- Solo Markdown en dev; Scope Lock y procedencia de CHANGELOG exact SHA requeridos; A5 de otra Task Javier y ninguna API/SQL/worker/QA/main/PROD nueva.

## 2026-11-15 — V5 C01-01.M04.03 tematizacion contextual sin autoridad nueva [Javier :40]
- Amplia branding Angular en /usuarios solo con empresa confirmada por TenantContextService; oculta marca durante revocacion A->B, sin mostrar identidad empresarial no verificada.
- Consume tokens dinamicos seguros --color-primary/--color-on-primary del ThemeApplierService existente y fallback forced-colors; agrega dos Vitest de ausencia y A/B junto a tres existentes.
- Sin alterar JWT/RBAC/tenant A/B, temas arbitrarios, permisos, backend, SQL ni rutas; CI exact-SHA y A5 independiente, rollback git revert solo dev.

## 2026-11-15 — V5 C01-01.M04.02 reparación causal P0-007.A de catálogo [Javier :40]
- El check exact-SHA detectó componente nuevo sin inventario; se añade la ruta a docs/P0-007-A-INVENTARIO-COMPONENTES-DEV.md y se actualiza el total 140 (132 feature, 8 shared).
- Mantiene la comprobación fail-closed de altas, bajas y rutas duplicadas; no se desactiva P0-007.A, Scope Lock, JWT/RBAC ni tenancy. CI exact-SHA pendiente; A5 de otro Javier.
- Solo dev, rollback git revert sin tocar QA/main/PROD.

## 2026-11-15 — V5 C01-01.M04.02 estados de usuarios y permisos [Javier :40]
- Integra componente Angular standalone OnPush en /usuarios existente: carga, vacio, error, exito por pagina y consulta sin permiso de crear, con aria-live.
- Seis pruebas Angular/Vitest dirigidas y workflow GitHub-hosted para Vitest, lint y build; CI exact-SHA requerida antes de LISTO_DEV, A5 independiente.
- Conserva JWT/RBAC/tenant A/B, guards, rutas, APIs y MySQL; sin QA/main/PROD, rollback git revert.

## 2026-11-15 — V5 C02-03.M01.06 gates y rollback de ciclo de vida tenant [Javier :30]
- Concilia M01.01–05 y A5 de antecesores, dieciseis gates G01–G16 y ocho rutas R01–R08 para alta, suspension, reactivacion, archivado y baja logica FUTURAS.
- Distingue Empresa.Activa real de Empresa.Eliminado false NotMapped y los AC01–AC24 FUTUROS_NO_EJECUTADOS; sin soft-delete, worker, MySQL, datos, JWT nuevo ni runtime activo.
- Solo Markdown, Scope Lock/V5 exact SHA y A5 independiente pendiente; rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V5 C02-02.M02.05 senales futuras e idempotencia placement [Javier :30]
- Define ocho senales SIG01–08 NO EMITIDAS, quince campos de sobre EVT01–15, catorce reglas fail-closed y diez pares P/N FUTUROS_NO_EJECUTADOS de migracion shared/dedicated.
- Sin dispatcher, webhook, workers, tareas nuevas ni eventos reales; conserva JWT/RBAC/tenant A/B, SHA exacto, idempotencia y redaccion de secretos/PII.
- Cambios solo Markdown con Scope Lock/V5 y A5 de otro Javier pendiente; rollback git revert sin QA/main/PROD.

## 2026-11-15 — V5 C02-02.M02.04 configuracion conceptual tipada y flags seguras [Javier :30]
- Establece dieciseis campos CFG, doce flags futuras apagadas salvo exigencia de restore, ocho reglas de precedencia y doce validaciones fail-closed de placement shared/dedicated.
- Define ocho pares P/N FUTUROS_NO_EJECUTADOS; no activa flags, DSN, MySQL, router, secretos, dual-write ni conexiones, y preserva JWT/RBAC/tenant A/B.
- Solo Markdown, Scope Lock/V5 exact SHA y A5 por otra Task; rollback git revert y ninguna autorizacion QA/main/PROD.

## 2026-11-15 — V5 C02-02.M02.03 persistencia e indices conceptuales shared/dedicated [Javier :30]
- Define once familias de entidades de almacenamiento y propiedad, diez indices tentativos compatibles a validar en MySQL 8.4 y ocho etapas futuras de migracion con rollback y aislamiento.
- Documenta doce fixtures FUTUROS_NO_EJECUTADOS y doce invariantes JWT/RBAC/tenant A/B; no ejecuta EF, SQL, MySQL, backups, restore, DNS, secretos ni cambia proveedores.
- Scope Lock y V5 exact SHA, A0–A4 documentales y A5 independiente pendiente, rollback git revert; solo dev y nunca QA/main/PROD.

## 2026-11-15 — V5 C02-02.M02.02 contratos conceptuales API y DTO placement [Javier :30]
- Define ocho operaciones futuras, cinco DTO conceptuales, doce errores seguros, catorce validaciones y diez pares P/N FUTUROS_NO_EJECUTADOS para almacenamiento shared/dedicated.
- Preserva JWT/RBAC/tenant A/B server-only, rechazo de DSN desde cliente, version y fence, idempotencia, restore e integridad sin crear endpoints, migraciones ni conexiones.
- Cambio exclusivamente Markdown, Scope Lock/V5 exact SHA y A5 independiente pendiente; rollback con git revert, sin secretos ni QA/main/PROD.

## 2026-11-15 — V5 C02-02.M02.01 contrato de entidades y ownership shared/dedicated [Javier :30]
- Define catorce entidades conceptuales TEN01–TEN14, cardinalidades, versiones y dieciseis invariantes para placement shared/dedicated, mapping opaco y segregacion tenant A/B.
- Agrega ocho pares P/N FUTUROS_NO_EJECUTADOS, rollback documental y limites de auth, snapshot, restore, cutover y CAS; no crea EF/MySQL, API, secretos, DNS ni runtime.
- Scope Lock y V5 por SHA exacto, A0–A4 documentales y A5 pendiente por otra Task Javier; sin QA/main/PROD y con git revert normal.

## 2026-11-15 — V5 C15-04.M03.04 adaptador aislado de snapshots UX [Javier :00]
- Implementa adaptador Infrastructure .NET 10 que copia observaciones y catalogos, valida SHA y tenant, y reutiliza preflight Application M03.03 sin escritura.
- Siete pruebas xUnit positivas y negativas de JWT/RBAC/tenant, SHA, catalogo, presupuesto UX y aislamiento de colecciones; requieren CI real exact-SHA.
- No certifica WCAG ni Playwright, JWT real, MySQL, permisos, desplegados ni A5; Scope Lock STRICT, rollback git revert y otra Task Javier para A5; sin QA/main/PROD.

## 2026-11-15 — V5 C15-02.M03.04 adaptador aislado de snapshots de migracion [Javier :00]
- Implementa adaptador real .NET 10 en Infrastructure con copias defensivas, comparacion SHA/tenant, lote maximo 64 y delegacion de solo lectura a preflight Application M03.03.
- Siete pruebas xUnit positivas y negativas de sujeto, RBAC, tenant A/B, SHA, QA, DNS/secretos, restore y mutacion de colecciones; checks causales sobre SHA.
- No ejecuta MySQL, DNS, R2, despliegue, JWT real ni A5; rollback git revert, Scope Lock STRICT y otra Task Javier para A5; sin QA/main/PROD.

## 2026-11-15 — V5 C04-01.M02.06 contrato terminal de salida catalogo de planes [Javier :10]
- Concilia cinco documentos M02.01-05 y A5 independientes; define dieciseis gates de seguridad, contratos de dependencia y doce pares P/N FUTUROS_NO_EJECUTADOS.
- Preserva Plan, PlanLimite y Suscripcion actuales; no crea SQL, EF, importes, cobros, workers, eventos runtime, permisos RBAC ni nuevos releases.
- Solo Markdown y CHANGELOG con Scope Lock/V5 exact SHA, A5 por otra Task Javier, rollback git revert; sin QA/main/PROD.

## 2026-11-15 — V5 C04-01.M02.05 eventos hipoteticos e idempotencia de planes [Javier :10]
- Define ocho senales NO EMITIDAS, quince campos conceptuales, catorce reglas fail-closed y doce pares P/N FUTUROS_NO_EJECUTADOS.
- No implementa dispatcher, webhook, worker, SQL, planes de precios, cobros ni grants JWT/RBAC/tenant; toda autoridad permanece en backend y fuentes institucionales.
- Solo contrato docs/v5 y CHANGELOG con Scope Lock/V5 exact SHA, A5 independiente, rollback git revert y sin QA/main/PROD.

## 2026-11-15 — V5 C04-01.M02.04 configuración tipada y flags seguros de planes SaaS [Javier :00]
- Propone dieciséis campos CFG, doce flags desactivadas, doce pares P/N FUTUROS_NO_EJECUTADOS y ocho rutas de recuperación para catálogo, versionado, límites y visibilidad.
- Mantiene autoridad backend JWT/RBAC/tenant A/B, deny-by-default, consentimiento/precio/moneda/impuesto sin aprobación, separación de grants y entitlements, Scope Lock STRICT y A5 independiente.
- Entrega documental exclusivamente: no registra flags, planes reales, cobros, PSP, MySQL, Angular, QA/main/PROD o eventos; rollback mediante git revert.

## 2026-11-15 — V5 C04-01.M02.03 persistencia e indices futuros de planes SaaS [Javier :10]
- Define diez estructuras relacionales hipoteticas, ocho indices candidatos, seis pasos de migracion futura y doce fixtures positivos/negativos FUTUROS_NO_EJECUTADOS.
- Conserva Plan, PlanLimite y Suscripcion implementados; sin DDL, EF migration, Aiven real, tarifas, cobros, workers ni grants JWT/RBAC/tenant.
- Solo contrato docs/v5 y CHANGELOG; Scope Lock/V5 exact SHA, A5 independiente, rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V5 C04-01.M02.02 contratos API, DTO, errores y compatibilidad del catálogo SaaS [Javier :00]
- Documenta doce operaciones API conceptuales, doce DTO, catorce validaciones, doce errores y doce pares P/N FUTUROS_NO_EJECUTADOS sobre planes versionados y límites por tenant.
- Preserva JWT/RBAC, membresía, aislamiento tenant A/B, versionado, idempotencia y decisiones comerciales de precios, moneda e impuestos aún no aprobadas; capacidad SaaS no otorga permisos.
- No modifica APIs, DTO reales, cobros, MySQL, PSP, Angular, secretos ni QA/main/PROD; A0–A4 y CI documental exact-SHA, A5 independiente y rollback git revert.

## 2026-11-15 — V5 C04-01.M02.01 contrato de entidades, ownership e invariantes de planes [Javier :10]
- Documenta doce objetos logicos, doce invariantes y doce pares P/N FUTUROS_NO_EJECUTADOS para catalogo SaaS, con fuente real Plan, PlanLimite y Suscripcion.
- Versionado, limites, precios y cambios de plan futuros no crean tablas, tarifas, privilegios, migraciones, cobros o webhooks; JWT/RBAC/tenant A/B fail-closed.
- Solo docs/v5 y CHANGELOG; exigir Scope Lock/V5 exact-SHA, A5 por otra Task Javier, sin QA/main/PROD; rollback git revert.

## 2026-11-15 — V5 C03-02.M02.06 contrato terminal de salida RBAC/ABAC [Javier :00]
- Consolida dieciséis condiciones RBA de aislamiento backend, doce pares positivos/negativos FUTUROS_NO_EJECUTADOS y ocho rutas de recuperación sobre JWT, RBAC/ABAC, tenant, sucursal, soporte, exportaciones y medios.
- Preserva requisitos M01.01–06 y M02.01–05 LISTO+A5 más C01-02/C02-01 M01.06+A5, Scope Lock STRICT, SHA exacto, CAS institucional, A5 independiente y rollback normal.
- Entrega documental sin nuevas rutas API, código de autorización, migraciones MySQL, eventos, workers, secretos, QA/main/PROD ni pruebas runtime; A0–A4 se auditan y A5 posterior queda pendiente.

## 2026-11-15 — V5 C03-02.M02.05 eventos e idempotencia conceptual RBAC/ABAC [Javier :50]
- Define ocho señales NO EMITIDAS, quince campos hipotéticos, catorce reglas fail-closed y doce pares P/N FUTUROS_NO_EJECUTADOS para permisos, revocación y auditoría multiempresa.
- Requiere origen backend validado, deduplicación por tenant, JWT/RBAC/ABAC, sucursal, export/medios y correlación saneada; no instala dispatcher, queue, worker, webhook, API ni MySQL.
- Entrega solo docs/v5 y CHANGELOG autocontenido, Scope Lock/V5 exact-SHA y A5 independiente; sin QA/main/PROD, secretos ni runtime autónomo.

## 2026-11-15 — V5 C03-02.M02.04 configuración tipada y flags seguros RBAC/ABAC [Javier :00]
- Define dieciséis claves de política conceptual, doce flags desactivadas y doce pares P/N FUTUROS_NO_EJECUTADOS de tenancy, grants, revocación, soporte, export y medios.
- Preserva JWT/RBAC backend, tenant/sucursal A/B, deny-by-default, ausencia de secretos y autoridad institucional; no crea configuración ejecutable ni autoriza QA/main/PROD.
- Documento y CHANGELOG publicados por fast-forward/CAS con controles Scope Lock y procedencia V5; A5 independiente posterior y rollback git revert.

## 2026-11-15 — V5 C03-02.M02.03 persistencia e índices RBAC/ABAC [Javier :50]
- Define diez entidades lógicas, ocho índices, seis pasos de migración futura y doce fixtures positivos/negativos FUTUROS_NO_EJECUTADOS para tenant, sucursal, grants y privacidad.
- Mantiene JWT/RBAC/ABAC/tenant A/B, revocación, auditoría sin PII y rollback; no crea DDL, EF, MySQL, exports, API, workers, eventos ni permisos.
- Documentación independiente con Scope Lock/V5 exact SHA, A0–A4 verificables y A5 pendiente de otra Task Javier; sin QA/main/PROD.

## 2026-11-15 — V5 C03-02.M02.02 API/DTO, errores y compatibilidad RBAC/ABAC [Javier :50]
- Especifica doce operaciones API conceptuales, ocho DTO, catorce errores seguros, dieciséis invariantes y doce pares P/N FUTUROS_NO_EJECUTADOS sobre autorización multiempresa.
- Conserva JWT, RolPermiso, ABAC por recurso/sucursal, exportaciones y enlaces a medios; contratos propuestos no crean endpoints, SQL, workers ni tests runtime.
- Entrega solo docs/v5 + CHANGELOG con Scope Lock/V5 exact-SHA y A5 independiente posterior, sin QA/main/PROD ni nuevos permisos.

## 2026-11-15 — V5 C14-01.M02.06 contrato terminal de aceptación funcional [Javier :00]
- Establece dieciséis condiciones AF de cierre, doce pares P/N FUTUROS_NO_EJECUTADOS y ocho rutas de recuperación y rollback para aceptación pública/privada multitenant.
- Preflight PLAN/COLA/DAG y M01.01–06 más M02.01–05 con A5 independiente; exige Scope Lock, SHA exacto, JWT/RBAC/tenant A/B y owner CAS sin sustituir pruebas runtime.
- No implementa API, SQL/MySQL, pagos, eventos, workers, E2E ni despliegues QA/main/PROD; A5 queda para otra Task Javier, reversión por git revert.

## 2026-11-15 — V5 C03-02.M02.01 entidades, ownership e invariantes RBAC/ABAC [Javier :50]
- Define 16 objetos conceptuales, 12 reglas fail-closed y 12 pares positivos/negativos FUTUROS_NO_EJECUTADOS para tenant, sucursal, recursos, export, medios y revocación.
- Protege JWT/RBAC/ABAC y aislamiento A/B; los controles actuales y contratos M01 son fuentes de diseño, no PASS runtime. No crea DB, API, policies ni workers.
- Únicamente docs/v5 + sección CHANGELOG autocontenida, A0–A4 con checks Scope Lock/V5 exact-SHA y A5 pendiente de otra Javier; sin QA/main/PROD ni secretos.

## 2026-11-15 — V5 C14-03.M02.06 contrato terminal de dependencias de promoción controlada [Javier :50]
- Fija dieciséis gates de diseño, dieciséis pares positivos/negativos FUTUROS_NO_EJECUTADOS y criterios de salida M02, separados de M03, QA/main/PROD y autorizaciones por entorno.
- Conserva Scope Lock STRICT, SHA/CI exactos, JWT/RBAC/tenant A/B, A5 independiente, owner CAS, rollback y defaults fail-closed; sin crear API, MySQL, webhooks, workers, secretos ni desplegar.
- Entrega documental con A0–A4 y readbacks individuales; Scope Lock y procedencia V5 deben aprobar el SHA publicado, A5 queda pendiente de otra Task Javier y rollback es git revert.

## 2026-11-15 — V5 C14-03.M02.05: señales conceptuales e idempotencia de promoción [Javier :30]
- Contrato documental de ocho señales NO EMITIDAS, quince campos hipotéticos, catorce reglas fail-closed y doce pares P/N FUTUROS_NO_EJECUTADOS para separación DEV/QA/PROD.
- Toda promoción futura requiere SHA/checks exactos, identidad y RBAC, A5 distinta, autorización de ambiente, rollback y readback real; una señal no equivale a PASS ni despierta Tasks.
- Sólo docs/v5 y sección CHANGELOG; sin dispatcher, webhook, pipeline, MySQL, secretos, infraestructura o deploy QA/main/PROD. A5 independiente posterior y rollback git revert.

## 2026-11-15 — V5 C14-03.M02.04 configuración tipada y flags seguros [Javier :40]
- Especifica 16 campos tipados, 12 flags apagados, 6 reglas de precedencia y 12 pares P/N futuros para revisión de promoción controlada, sin crear configuración runtime.
- Preserva JWT/RBAC/tenant A/B, evidencia CI exact SHA, Scope Lock y autorización distinta por ambiente; no toca QA/main/PROD ni secretos.
- Sólo contrato documental con A0-A4 y check V5 exact-SHA; A5 corresponde a Task Javier independiente y no sustituye permisos de release.

## 2026-11-15 — V5 C14-03.M02.03 contrato de persistencia para promoción controlada [Javier :40]
- Define diez entidades conceptuales, ocho índices propuestos, seis gates de migración futura y diez fixtures sintéticos para releases con SHA y digest trazables; no crea tablas ni migraciones.
- Mantiene JWT/RBAC/tenant A/B, Scope Lock y autorización independiente por ambiente; QA/main/PROD, MySQL real, eventos y webhooks permanecen sin cambios.
- Entrega documental bajo A0-A4 y CI de gobernanza exact-SHA; casos MySQL y seguridad runtime son FUTUROS_NO_EJECUTADOS, A5 deberá ser de otra Task Javier.

## 2026-11-15 — V5 reparación técnica independiente de C05-02.M01.02, conteo de tokens de marca [Javier :30]
- Corrige tres referencias documentales de 16 a 15 campos de color, asignaciones CAMPOS_TEMA y oráculo P02; contraste de blob real frontend/Angular y Domain TemaVisual con exactamente quince colores.
- Mantiene íntegros los permisos, arquitectura, rutas, diseño y autoría histórica de PRIMARY DEV 01; sólo cambia Markdown, sin código, MySQL, IAM, UI runtime, QA/main/PROD.
- A5_FAIL original se conserva como evidencia; esta reparación exige nueva revisión A5 independiente por Task Javier distinta de la Task reparadora J30; Scope Lock y procedencia V5 del nuevo SHA, rollback git revert.

## 2026-11-15 — V5 C14-03.M02.02 API/DTO y errores de promoción controlada [Javier :20]
- Documenta diez operaciones conceptuales, seis DTO futuros, doce códigos de error y dieciséis invariantes de identidad, SHA, CAS, A5 y permisos por ambiente; diez pares P/N futuros no ejecutados.
- Contrato exclusivamente dev sin rutas HTTP, workflows, deploys, secretos o DB; no autoriza QA/main/PROD y A5 independiente queda pendiente.

## 2026-11-15 — V5 C14-03.M02.01: modelo de entidades y ownership de promoción controlada [Javier :20]
- Define doce entidades conceptuales, veinte invariantes de SHA/CI/Scope Lock/tenant/seguridad/aprobación por ambiente y diez pares P/N futuros no ejecutados para release candidates y evidencia.
- Contrato documental exclusivo dev sin desplegar QA/main/PROD ni crear CI, secretos, migraciones o eventos; revisión A5 independiente pendiente.

## 2026-11-15 — V5 C10-01.M02.06 contrato terminal de dependencias y salida storage neutral [Javier :30]
- Documenta doce gates de diseño, contratos Domain/Application/Infrastructure/API, doce pares positivos/negativos FUTUROS_NO_EJECUTADOS y prerequisitos M01/M02 LISTO+A5; sin inferir autorización de M03.
- Conserva Cloudinary, tenant A/B/JWT/RBAC server-side, MIME y SHA-256 de bytes reales como requisitos futuros; no crea EF/MySQL, R2, workers, eventos, feature flags, secretos, endpoints, QA/main ni PROD.
- Entrega sólo Markdown + sección CHANGELOG autocontenida; A0–A4 documentales y Scope Lock/V5 exact SHA requeridos antes de LISTO_DEV, A5 por otra Task Javier y rollback git revert.

## 2026-11-15 — V5 C04-04.M02.05 eventos, idempotencia y límites de cobros [Javier :20]
- Especifica contrato documental de dieciséis campos de evento, veinte invariantes fail-closed HMAC/RBAC/tenant/idempotencia y diez pares P/N futuros no ejecutados para correlación y conciliación.
- No instala webhooks, colas, dispatchers, pagos, PSP, secretos ni cambios .NET/MySQL; A5 independiente pendiente y decisiones comerciales D01–D10 abiertas.

## 2026-11-15 — V5 C04-04.M02.02 contratos API/DTO y errores de cobros [Javier :50]
- Define ocho operaciones hipotéticas, seis DTO y catorce denegaciones HMAC/RBAC/tenant/moneda, con ocho pares P/N FUTUROS_NO_EJECUTADOS; preserva PagoOnline e ingreso firmado existentes.
- Cero PSP live, enlaces activados, webhooks ejecutados, cobros, reembolsos, migraciones o secretos; A5 ajena y CI Scope Lock/V5 exact-SHA requeridas.

## 2026-11-15 — V5 C04-04.M02.03 persistencia, índices y fixtures de cobros [Javier :50]
- Diseña ocho contratos conceptuales, siete consultas/index candidates, diez fixtures y seis pasos de migración futura; evita doble cobro, replay y cruce de tenants.
- No crea índices, tablas, migraciones EF/MySQL, asientos, proveedor, webhook ni movimientos monetarios; rollback Git normal, A5 independiente y Scope Lock STRICT.

## 2026-11-15 — V5 C04-04.M02.04 políticas tipadas y flags seguros de cobros [Javier :20]
- Define dieciséis opciones tipadas conceptuales, dieciséis invariantes de tenant/JWT/RBAC/PSP, HMAC/idempotencia y diez pares P/N futuros para enlaces de pago y conciliación.
- Flags de cobro deshabilitados por defecto, sin alterar código, proveedores, dinero, secretos, datos ni eventos; decisiones comerciales D01–D10 siguen abiertas, A5 de otra Task pendiente.

## 2026-11-15 — V5 C04-04.M02.01 entidades, ownership e invariantes de cobros [Javier :20]
- Define diez entidades conceptuales y veinte invariantes tenant/RBAC, HMAC anti-replay, idempotencia contable y propiedad de enlaces/pagos; diez pares de oráculos P/N futuros no ejecutados.
- Contrato documental sin proveedores, endpoints, webhooks, transacciones, migrations o cambios financieros; decisiones comerciales D01-D10 abiertas y A5 ajena pendiente. Sin QA/main/PROD.

## 2026-11-15 — V5 C10-01.M02.05 eventos, idempotencia y límites de storage neutral [Javier :50]
- Define ocho eventos conceptuales NO_EMITIDOS, envelope saneado, catorce reglas de deduplicación/correlación y nueve pares P/N FUTUROS_NO_EJECUTADOS para objetos con tenant/JWT/RBAC.
- No instala worker, broker, dispatcher, webhook, R2, migración ni Tasks; Scope Lock STRICT, A5 ajena, rollback normal y evidencias exact-SHA para eventual implementación.

## 2026-11-15 — V5 C10-01.M02.04: configuración tipada y banderas seguras de storage neutral [Javier :20]
- Especifica doce contratos tipados conceptuales, dieciséis invariantes de autoridad, límites y tenancy y ocho pares de casos futuros para políticas de almacenamiento por tipo, empresa y entorno.
- Banderas nuevas apagadas por defecto; sin código, cambios de DI/EF/MySQL, nuevos proveedores o eventos. Cloudinary permanece; R2/Hostinger y promoción QA/main/PROD no autorizados. A5 independiente pendiente.

## 2026-11-15 — V5 C10-01.M02.02 API, DTO y errores de storage neutral [Javier :50]
- Especifica ocho operaciones, siete DTO conceptuales y doce denegaciones con JWT/RBAC, tenant A/B, MIME real, SHA-256, tamaño y URLs temporales; escenarios P/N FUTUROS_NO_EJECUTADOS.
- Mantiene interfaces Cloudinary actuales, cero R2/HTTP real, sin migración, credenciales ni cambios QA/main/PROD; requiere A5 independiente y CI causal del SHA documental.

## 2026-11-15 — V5 C10-01.M02.03 persistencia, índices y fixtures de storage neutral [Javier :50]
- Diseña siete entidades conceptuales, seis claves de consulta, diez fixtures y siete etapas de migración futura con hashes, versionado y ownership tenant server-side.
- No crea tablas, índices, EF migrations, datos o bucket R2; rollback documental git revert, Scope Lock STRICT, A5 de otra Task y pruebas reales pendientes.

## 2026-11-15 — V5 C10-01.M02.01: entidades y ownership storage neutral [Javier :20]
- Contrato documental de ocho entidades conceptuales, dieciocho invariantes tenant/RBAC/integridad y ocho pares de escenarios futuros positivos/negativos para recursos, versiones, propietarios y localizadores opacos.
- Conserva interfaces Cloudinary existentes, no modifica código, EF, MySQL, servicios, permisos ni proveedores; M02/M03 mantienen sus DAG y A5 independiente. No declara pruebas runtime ejecutadas.

## 2026-11-15 — V5 C05-04.M02.06 dependencias y contrato de salida de ligereza UI [Javier :50]
- Consolida diez puertas de calidad, ocho pares P/N FUTUROS_NO_EJECUTADOS y dependencias LISTO+A5 de M01 y M02; budget Angular inicial 650kb/750kb y estilos 16kb/32kb son política configurada, no métricas medidas.
- No implementa código ni eventos, migraciones, telemetría, webhooks o despliegues; las CWV, Playwright, APIs y pruebas de aislamiento A/B quedan condicionadas a M03 con CI causal, A5 ajena y Scope Lock STRICT.

## 2026-11-15 — V5 C08-04.M02.01 entidades, ownership e invariantes sectoriales [Javier :50]
- Define seis objetos conceptuales de capacidades, catorce invariantes tenant/RBAC y ocho pares P/N futuros; reutiliza Empresa, UsuarioEmpresa y recursos sin duplicar identidad.
- Ninguna tabla, API, migración, permiso o capability activa: sólo contrato documental; Scope Lock STRICT, A5 independiente y rollback Git normal.

## 2026-11-15 — V5 C08-04.M02.02 API, DTO, errores y compatibilidad sectorial [Javier :50]
- Diseña ocho operaciones hipotéticas, seis DTO conceptuales y doce errores/denegaciones para módulos por rubro, con JWT/RBAC y propiedad empresa/sucursal.
- No implementa HTTP, DTO compilados, permisos, MySQL ni eventos; pruebas positivas y negativas futuras no ejecutadas, A5 pendiente de otra Task Javier.

## 2026-11-15 — V5 C08-04.M02.03 persistencia y fixtures sectoriales [Javier :10]
- Define seis entidades conceptuales, cinco claves de consulta y diez fixtures para módulos sectoriales, con propiedad empresa/tenant y esquema compartido.
- No crea tablas, índices, migraciones ni grants; pruebas .NET/MySQL y revisión A5 independientes quedan pendientes.

## 2026-11-15 — V5 C08-04.M02.04 configuración tipada y flags sectoriales [Javier :10]
- Propone doce opciones tipadas y doce criterios de rechazo de flags, RBAC, pertenencia y caché por tenant; preserva núcleo y fallback denegatorio.
- No activa flags ni cambia API/Angular/MySQL ni secretos; requiere CI documental y A5 ajena.

## 2026-11-15 — V5 C08-04.M02.05 contratos de eventos e idempotencia futuros [Javier :10]
- Documenta cinco mensajes hipotéticos, doce campos y doce controles de deduplicación, correlación y falla segura para capacidades por rubro.
- Ningún evento/dispatcher/worker/webhook se implementó; sin efecto runtime, A5 de otra Task y sin QA/main/PROD.

## 2026-11-15 — V5 C13-04.M02.03 modelo de evidencia, índices y fixtures visuales [Javier :10]
- Diseña cinco entidades conceptuales de evidencia reproducible, claves y diez fixtures positivos/negativos; no introduce MySQL, índices ejecutables ni migraciones.
- Preserva tenant A/B, privacidad, WCAG y Playwright como validación futura, con A5 independiente y Scope Lock STRICT.

## 2026-11-15 — V5 C13-04.M02.04 configuración tipada y ámbitos visuales [Javier :10]
- Propone doce claves tipadas y doce reglas de rechazo, preservando Angular 22.2.1, tema tenant y reduced motion sin activar feature flags operativos.
- Seguridad backend y regresión accesible permanecen pendientes de tests reales; sin cambio de CSS, API, JWT, MySQL o despliegue.

## 2026-11-15 — V5 C13-04.M02.05 contrato futuro de eventos UX e idempotencia [Javier :10]
- Define cuatro mensajes conceptuales, envelope de diez campos, doce reglas de idempotencia/correlación y ocho pares de pruebas futuras no ejecutadas.
- No instala webhook, cola, dispatcher, workers ni telemetría externa; mantiene auditoría append-only, Scope Lock y A5 independiente.

## 2026-11-15 — V5 C13-04.M02.01 entidades y ownership de experiencia visual [Javier :50]
- Define catorce entidades lógicas UI/tenant/foco/estado y catorce invariantes no-regresión, con ocho pares P/N FUTUROS_NO_EJECUTADOS.
- Conserva JWT/RBAC y tenant A/B desde backend, foco/responsive/accesibilidad como pruebas por ejecutar; ningún cambio Angular, CSS, API, tokens operativos o DB.
- Scope Lock STRICT, dos Markdown y A0–A4 documentales exact SHA con A5 independiente; no QA/main/PROD.

## 2026-11-15 — V5 C13-04.M02.02 contratos futuros de DTO y errores visuales [Javier :50]
- Propone doce rutas/DTO y doce errores con doce validaciones de autoridad backend y compatibilidad aditiva; ningún endpoint, controller o guard se modifica.
- Define ocho pares P/N de tenant A/B, revocación, estado accesible y privacidad de evidencia, todos FUTUROS_NO_EJECUTADOS.
- Dev exclusivo, Scope Lock/V5 exact-SHA, A0–A4 y A5 independiente; sin E2E, cambios de UX ejecutables, QA/main/PROD.

## 2026-11-15 — V5 C14-03.M01.06 bloqueos y reversión de promoción controlada [Javier :50]
- Documenta dieciséis barreras de salida, diez rutas de rollback y doce pares P/N futuros para separar evidencias DEV, permiso QA y eventual autorización PROD; no efectúa ningún despliegue.
- Verifica cinco predecesores LISTO+A5 independientes y blobs de fuentes, conserva riesgos y decisiones abiertas, exige SHA, JWT/RBAC/tenant A/B, CI causal y reversión probada para fases futuras.
- Dev y dos Markdown solamente, Scope Lock STRICT y V5 exact SHA, A5 posterior de otra Task; sin migraciones, secretos, QA/main/PROD, eventos, servicios ni pagos.

## 2026-11-15 — V5 C14-01.M02.04 configuración tipada de aceptación [Javier :50]
- Define dieciséis claves conceptuales CFG y doce validaciones VT con precedencia restrictiva global/tenant/rubro/sucursal/recurso; deny domina y flags de cliente no conceden permisos.
- Propone cuatro positivos y doce negativos FUTUROS_NO_EJECUTADOS, sin modificar appsettings, Angular, JWT, RBAC ni feature flags reales.
- Solo documentos dev con Scope Lock STRICT y V5 por SHA, A5 por otra Task Javier; sin E2E, pagos, DB, QA/main/PROD.

## 2026-11-15 — V5 C14-01.M02.05 contrato futuro de idempotencia y eventos [Javier :50]
- Documenta catorce mensajes futuros EV, diez claves de deduplicación ID y doce límites LM; no activa webhook, Task, worker, cola ni dispatcher.
- Conserva evidencia exact-SHA, A/B y JWT/RBAC, sin declarar PASS CI por eventos ni transacciones comerciales; escenarios FUTUROS_NO_EJECUTADOS.
- Solo Markdown dev, Scope Lock y provenance V5, A5 independiente posterior; sin datos, proveedor, QA/main/PROD.

## 2026-11-15 — V5 C14-01.M02.02 reparación causal de Scope Lock [Javier :50]
- Corrige dos identificadores contractuales ingleses heredados en API10 por la nomenclatura neutral `stock`, conservando método, permiso y límites backend/tenant.
- Respuesta al fallo exact-SHA de verify-solqaryn-project-scope; no se desactivan verificadores, no se modifican API ejecutables y se exigen checks del nuevo SHA.
- Reparación sólo dev, rollback por git revert sin reescribir historia; sin secretos, datos, QA/main/PROD.

## 2026-11-15 — V5 C14-01.M02.03 persistencia conceptual e índices de aceptación [Javier :50]
- Define doce agregados lógicos de evidencia de aceptación, ocho índices candidatos, doce fixtures A/B sintéticos y seis etapas de migración reversible FUTURA_NO_EJECUTADA.
- Separación fail-closed de hechos observados frente a oráculos, recibos exact-SHA y datos sensibles; sin DDL, EF/MySQL ejecutado, HTTP, PII ni prueba de backup/restore real.
- Documentos exclusivos dev con Scope Lock STRICT, CHANGELOG V5, A0–A4 exact SHA y A5 independiente; sin QA/main/PROD ni servicios externos.

## 2026-11-15 — V5 C14-01.M02.02 contratos API/DTO y errores de aceptación [Javier :50]
- Documenta doce rutas candidatas y DTO, catorce validaciones backend y doce errores seguros para aceptación pública, privada, administrativa y comercial; no crea endpoints ni altera contratos runtime.
- Incluye cuatro escenarios positivos y doce negativos FUTUROS_NO_EJECUTADOS, con límites JWT/RBAC/tenant A/B, PSP, fiscal, versión e idempotencia sin asumir resultados E2E.
- Dos Markdown dev, Scope Lock STRICT, V5 exact-SHA y A5 independiente; no API operativa, EF/MySQL, migraciones, cobros, webhooks, QA/main/PROD.

## 2026-11-15 — V5 C15-02.M03.03 preflight autorización, RBAC y tenancy para migración [Javier :40]
- Añade guard Application .NET 10 fail-closed de principal, identidad/tenant, permisos backend declarados, Scope Lock y SHA antes de evaluar el servicio de migración M03.02.
- Agrega xUnit de sesión no autenticada, rol ausente, tenant A/B, sujeto falseado, QA/PROD, proveedor no autorizado, DNS/secrets y recuperación insuficiente; no comprueba JWT firmado ni datos MySQL reales.
- El veredicto nunca autoriza migración, deploy ni A5; Scope Lock STRICT, CI backend/autorización/seguridad y CHANGELOG exact-SHA antes de LISTO; A5 independiente. Sin API, eventos ni QA/main/PROD.

## 2026-11-15 — V5 C14-01.M02.01 modelo de entidades y ownership de aceptación [Javier :50]
- Define dieciséis entidades conceptuales y dieciséis invariantes de autoridad, trazabilidad y aceptación funcional; no crea tablas, DTO ni API y no infiere permisos por tenant cliente.
- Propone cuatro oráculos positivos y doce negativos futuros, incluidas revocación JWT/RBAC, tenant A/B, privacidad, transacciones, responsive y CI; todos FUTUROS_NO_EJECUTADOS.
- Documento exclusivo dev, Scope Lock STRICT y checks V5 exact-SHA antes de LISTO, A5 independiente posterior; sin E2E, MySQL, pagos, datos, QA/main/PROD.

## 2026-11-15 — V5 C15-01.M03.02 servicio Application de preflight de cobertura [Javier :40]
- Implementa servicio .NET 10 read-only sobre validador Domain de M03.01: verifica SHA, revisión, catálogos y límites, sin autenticar fuentes institucionales ni certificar cobertura real.
- Añade xUnit positivos/negativos de SHA, catálogo, requisitos sin mapa, duplicados, evidencia insegura y capacidad; el resultado compatible nunca concede escritura, A5 ni promoción.
- Dev-only, Scope Lock STRICT, backend/security tests y procedencia V5 exact-SHA antes de LISTO; A5 independiente otra Task Javier. Sin API, EF/MySQL, eventos, dispatcher, workers o QA/main/PROD.

## 2026-11-15 — V5 C07-04.M03.02 Application preflight comercial .NET 10 [Javier :50]
- Incorpora servicio de aplicación real sobre reglas Domain de pedidos, facturas, pagos y exportación; verifica contratos de SHA/evidencia y conserva todas las operaciones como no autorizadas hasta adaptador autenticado.
- Añade xUnit positivos/negativos de tenant A/B, NULL legado, versiones, RBAC, idempotencia, importes, moneda, PSP y fiscal; jamás cobra, factura, persiste ni exporta mediante el preflight.
- Sólo dev, Scope Lock STRICT, verificación V5 y CI causal exact-SHA para A0–A4; A5 independiente. Sin API/EF/MySQL en vivo, webhooks, secretos, QA/main/PROD.

## 2026-11-15 — V5 C14-01.M01.06 barreras y rollback de aceptación funcional [Javier :40]
- Cierra preparación documental M01 de aceptación funcional: dieciséis gates, diez escenarios de reversión y doce oráculos P/N FUTUROS_NO_EJECUTADOS sobre acceso, comercio, inventario, responsive y CI.
- Contrasta cinco predecesores LISTO con A5 independiente; el documento no ejecuta Playwright, HTTP, JWT/RBAC, MySQL, cobros, pruebas A/B ni autoriza QA/main/PROD.
- Alcance exclusivo dev y dos Markdown; Scope Lock STRICT, fast-forward/CAS, comprobaciones exact-SHA y A5 posterior de otra Task Javier.

## 2026-11-15 — V5 C13-04.M01.06 contrato de bloqueos, rollback y liberación de experiencia visual [Javier :40]
- Completa contrato M01 documental: dieciséis barreras de salida, diez escenarios de rollback y doce pares positivos/negativos futuros, con cinco predecesores LISTO + A5 independiente por TASK_ID.
- Conserva límites JWT/RBAC/tenant A/B, foco, estados, reflow y pruebas causales; las matrices propuestas NO son WCAG/Playwright/HTTP ejecutados ni permiten habilitar sucesoras fuera de DAG.
- Sólo dos archivos Markdown, Scope Lock STRICT, publicación fast-forward/CAS, A0–A4 documentales y A5 por otra Task Javier; sin frontend/runtime, MySQL, migraciones, dispatcher, QA/main/PROD.

## 2026-11-15 — V5 C15-04.M03.03 guard de autorización de auditoría UX en Application [Javier :20]
- Implementa wrapper .NET 10 de preflight UX que exige JWT, membresía, permiso, contexto tenant y SHA verificables por backend; restringe catálogo autorizado de superficies y criterios.
- Añade xUnit positivos y negativos de tenant A/B, ausencia de autoridad, SHA obsoleto, presupuesto y catálogo ampliado por cliente; no concede acceso, escritura ni certifca métricas externas.
- Dev-only, CHANGELOG/Scope Lock y CI backend/seguridad exact-SHA antes de LISTO; A5 independiente. Sin endpoint real, API/EF/MySQL, browser/Playwright, eventos ni QA/main/PROD.

## 2026-11-15 — V5 C15-02.M03.02 servicio Application de preflight de migración [Javier :20]
- Agrega servicio .NET 10 de Application read-only para evaluar candidatos acotados con reglas M03.01, deteniendo en el primer rechazo y denegando siempre permisos de despliegue o migración.
- Agrega xUnit positivos/negativos para tenant A/B, SHA, scopes, proveedores, backup/restore, capacidad y hallazgos críticos. No prueba HTTP/EF/MySQL, DNS, Hostinger ni R2 reales.
- Sólo dev y Scope Lock STRICT, CI backend/seguridad y CHANGELOG V5 exact-SHA antes de LISTO; A5 independiente; sin secretos, infraestructura, eventos, QA/main/PROD.

## 2026-11-14 — V5 C03-04.M03.01 reglas Domain de incidentes y continuidad [Javier :50]
- Añade preflight .NET 10 fail-closed para incidente, contención y resolución, JWT/RBAC/tenant A/B, versionado, propiedad, sucursal, evidencia saneada e idempotencia.
- xUnit positivo/negativo para revocación, acceso cruzado, estados de incidente, permisos, razón y restore separado; no realiza backup/restore ni entrega acceso o autorización.
- Sólo dev, Scope Lock/V5 y CI backend/seguridad exact SHA, A5 independiente posterior; sin datos, migraciones, webhooks ni QA/main/PROD.

## 2026-11-14 — V5 C01-04.M03.01 reglas Domain reales de expediente universal [Javier :50]
- Implementa validador .NET 10 de dominio puro para timeline/observaciones/decisiones/adjuntos, tenant A/B, sucursal, membresía, permiso, versión, clave idempotente y archivado fail-closed.
- Incluye xUnit positivo/negativo; la respuesta READY_FOR_TRUSTED_CASEFILE_ENFORCEMENT no concede acceso, escritura, descarga ni certifica MySQL/HTTP/QA.
- Sólo dev, fast-forward con Scope Lock/V5 y backend/security CI exact-SHA requeridos, A5 independiente posterior; sin migraciones, eventos, workers, secretos ni QA/main/PROD.

## 2026-11-14 — V5 C15-02.M03.01 reparación dirigida de Scope Lock [Javier :50]
- Sustituye una denominación técnica inglesa heredada en Domain y xUnit por SourceOwnershipFromTrustedCatalog, preservando todas las condiciones fail-closed y las aserciones.
- Corrige únicamente el token rechazado por el verificador de identidad, sin desactivar reglas, tests ni analizadores; se exige nuevo Scope Lock, V5 y CI .NET/seguridad del SHA reparado.
- Reparación dev fast-forward, sin cambios en JWT/RBAC, infraestructura, datos, QA/main/PROD o el owner de la microtarea.

## 2026-11-14 — V5 C15-02.M03.01 dominio real para preflight de arquitectura y migración [Javier :50]
- Añade validador .NET 10 de dominio puro, fail-closed, para SHA, tenancy, inventario, proveedores, permisos, backup/restore sandbox, rollback y hallazgos críticos, sin ejecutar migraciones ni DNS.
- Pruebas xUnit positivas y negativas de tenant A/B, proveedor no autorizado, privilegios, QA/PROD, evidencias incompletas y cambios destructivos; estado READY_FOR_TRUSTED_ARCHITECTURE_REVIEW no autoriza despliegue.
- Sólo dev, commit fast-forward/CAS, Scope Lock/V5 y backend/security CI exact-SHA antes de LISTO_DEV, A5 independiente posterior; sin infraestructura, webhooks, QA/main/PROD.

## 2026-11-14 — V5 C14-02.M03.01 reglas reales de dominio para aceptación de seguridad [Javier :50]
- Agrega guard .NET 10 de dominio puro fail-closed para JWT/membresía/permiso, BOLA/IDOR, tenant A/B, sucursal, host, archivos, jobs, idempotencia y SHA; veredicto no concede acceso ni publicación.
- Pruebas xUnit positivas y negativas; integración HTTP/EF/MySQL y pentest siguen SIN_EJECUTAR, nunca se certifica P0/P1=0 con estos tests de dominio.
- Publicación exclusiva dev; Scope Lock, CHANGELOG V5, backend/security CI exact-SHA requeridos antes de LISTO; A5 sólo por otra Task Javier, sin eventos, migraciones, QA/main/PROD.

## 2026-11-14 — V5 C15-03.M03.01 reglas reales .NET 10 de tenancy y pruebas xUnit [Javier :20]
- Domain guard fail-closed para propiedad tenant A/B, NULL legado, sucursal, archivos revocados/expirados, jobs y control plane, con denegación de bypass de filtros y enumeración.
- Pruebas xUnit positivas/negativas; el resultado READY_FOR_SERVER_SECURITY_ENFORCEMENT no autoriza consultas y no certifica reparación del TenantQueryBypassGuard sin integración HTTP/EF/MySQL.
- Sólo dev y A0–A4/CI exact-SHA antes de LISTO, A5 independiente posterior; sin secretos, migraciones, nuevos dispatchers, QA/main/PROD.

## 2026-11-14 — V5 C07-04.M03.01 reglas de dominio comerciales reales .NET 10 y xUnit [Javier :20]
- Implementa Domain `V5CommercialDocumentDomainRules`: contexto backend confiable, propiedad tenant A/B y NULL legado, versión, moneda, importe, estado, grants, fingerprint SHA-256, fiscalidad y PSP fail-closed.
- Añade pruebas xUnit positivas/negativas de permisos, estados, concurrencia, A/B, idempotencia y autoridad; el veredicto positivo exige aún controles Application/API/DB, sin ejecutar pagos ni emitir facturas.
- Publicación sólo dev con A0–A4 y CI causal exact-SHA; A5 independiente pendiente, sin migraciones, eventos, QA/main/PROD ni cambios de secretos.

## 2026-11-14 — V5 C15-02.M01.06 bloqueo, rollback y liberación documental de arquitectura/migración [Javier :20]
- Cierra el contrato terminal de preparación M01 con BL01–BL16, RB01–RB08 y ocho puertas E01–E08; exige M01.01–05 LISTO+A5 independientes, sin inventar pruebas de infraestructura.
- Distingue Vercel/Render/Aiven/Cloudinary declarados de Hostinger/Cloudflare/R2 objetivos; no activa DNS, datos, JWT/RBAC, migraciones, webhooks, servicios ni QA/main/PROD.
- Sólo Markdown en dev, CI Scope Lock y procedencia V5 exact-SHA, estado LISTO_DEV sujeto a A0–A4, A5 posterior de otra Task Javier; rollback Git normal sin reescribir historia.

## 2026-11-14 — V5 C15-03.M02.06 dependencias y salida documental de auditoría de seguridad/tenancy [Javier :00]
- Cierra el contrato conceptual de M02 tras M01 y cinco M02 LISTO+A5 independientes: 16 controles SECOUT, 12 pares NEG futuros y 8 bloqueos explícitos.
- Riesgos de TenantQueryBypassGuard, NULL legado, FK compuestas, JWT/RBAC, archivos, jobs, control plane e idempotencia requieren pruebas .NET/HTTP/MySQL reales en M03, NO_EJECUTADAS.
- Sólo documento+CHANGELOG dev con CAS fast-forward, Scope Lock y V5 exact SHA; A5 por otra Task Javier pendiente, sin migraciones, eventos, secretos, QA/main/PROD.

## 2026-11-14 — V5 C15-05.M03.02 preflight real de auditoría operativa en Application [Javier :00]
- Implementa servicio .NET10 de sólo lectura para snapshots V5: SHA/revisión/catálogo/capacidad y Domain; resultados no autorizan CAS, cierre, A5 ni ejecución de eventos.
- Añade seis pruebas xUnit con positivos y negativos de SHA obsoleto, revisión, allowlist, A5 propia, incompletitud, tamaño e IDs duplicados o ajenos; sin API, SQL, dispatcher ni cambios tenant/JWT/RBAC.
- Requiere CI backend/security real y Scope Lock/V5 exact-SHA antes de LISTO_DEV, A5 posterior de otra Task Javier; rollback git revert, sin QA/main/PROD.

## 2026-11-14 — V5 C07-04.M02.06 dependencias y contrato de salida de pedidos/pagos [Javier :00]
- Verifica M01.01–06 y M02.01–05 LISTO con A5 independiente, cinco SHAs/blobs fuente y DAG/COLA G/O SI; define 16 condiciones OUT, 12 pares FUT y seis reglas de salida.
- Todo es documento FUTURO_NO_EJECUTADO: sin PSP, pagos, endpoints, migraciones, workers, dispatcher, eventos ni cambios runtime; seguridad JWT/RBAC/tenant A/B y reversión obligatorias.
- Cambios limitados a dos Markdown por fast-forward/CAS en dev, Scope Lock/V5 exact-SHA y A5 de otra Task pendiente; no QA/main/PROD.

## 2026-11-14 — V5 C07-04.M02.05 eventos conceptuales e idempotencia de pedidos/pagos [Javier :50]
- Especifica 14 eventos NO_EMITIDOS, 14 campos conceptuales, 12 reglas de idempotencia y 12 límites para cotización, pedido, factura, pago, stock y compensación.
- Define 12 pares positivos/negativos FUTUROS_NO_EJECUTADOS; no crea dispatcher, webhooks, Tasks, PSP, SQL, eventos runtime ni efectos económicos.
- Sólo doc+CHANGELOG en dev con CAS fast-forward, Scope Lock/V5 exact-SHA y A5 independiente pendiente; JWT/RBAC/tenant A/B y rollback git revert.

## 2026-11-14 — V5 C07-04.M02.04 configuración tipada, ámbito y flags de pedidos/pagos [Javier :50]
- Diseña 18 parámetros/estados conceptuales y 14 validaciones fail-closed sobre plan, tenant, sucursal, RBAC, factura, caja, PSP e idempotencia; sin activar flags reales.
- Establece 12 pares positivos/negativos FUTUROS_NO_EJECUTADOS; los overrides no habilitan efectos económicos ni saltan seguridad/Scope Lock/A5.
- Sólo docs/v5 y CHANGELOG en dev con CAS fast-forward y CI documental exact SHA; A5 independiente pendiente y rollback git revert, sin QA/main/PROD.

## 2026-11-14 — V5 C07-04.M02.03 persistencia, índices y fixtures de pedidos/pagos [Javier :50]
- Diseña 12 asociaciones lógicas, 10 índices hipotéticos, 8 pasos aditivos reversibles y 12 pares de fixtures futuros para cotización, pedidos, factura, caja, pagos y stock.
- Separa código/guardas tenant EF inspeccionados de tablas y migraciones NO IMPLEMENTADAS; mantiene EmpresaId NULL legado aislado, JWT/RBAC y tenant A/B fail-closed.
- Sólo documento y CHANGELOG dev con CAS fast-forward y Scope Lock/V5 exact SHA; A5 independiente pendiente, pruebas MySQL futuras NO_EJECUTADAS, sin QA/main/PROD.

## 2026-11-14 — V5 C07-04.M02.02 contratos API/DTO de pedidos, facturas y pagos [Javier :50]
- Diseña 10 operaciones, 12 DTO, 14 errores y 10 reglas de compatibilidad para pedidos/pagos sin añadir endpoints, contratos runtime, PSP ni esquemas MySQL.
- Registra 10 pares positivos/negativos futuros de tenant A/B, JWT/RBAC, idempotencia, permisos, fiscalidad y exportación; casos NO_EJECUTADOS sin afirmar CI HTTP funcional.
- Sólo docs/v5 y CHANGELOG en dev con CAS fast-forward; Scope Lock/V5 exact SHA, A5 independiente pendiente, rollback git revert y sin QA/main/PROD.

## 2026-11-14 — V5 C07-04.M02.01 entidades, ownership e invariantes de pedidos/pagos [Javier :50]
- Modela 12 objetos conceptuales y 14 invariantes fail-closed para cotizaciones, pedidos, facturas, cobros, stock y exportaciones; distingue código inspeccionado de contratos NO IMPLEMENTADOS.
- Define 10 pares de pruebas futuras positivas/negativas sobre tenant A/B, JWT/RBAC, fiscalidad, idempotencia y rollback, sin ejecutarlas ni cambiar runtime, SQL, pagos o proveedores.
- Sólo docs/v5 y CHANGELOG en dev; exige Scope Lock/V5 exact-SHA, A0–A4 documentales y A5 independiente; QA/main/PROD prohibidos y rollback git revert.

## 2026-11-14 — V5 C15-05.M03.01 reglas de dominio para auditoría operativa [Javier :50]
- Implementa validador puro .NET 10 para snapshots de auditoría: fuente, IDs, estados, owner permitido, SHA, revisores A5 diferentes y referencias de evidencias; no certifica datos suministrados sin verificación externa.
- Añade pruebas xUnit positivas/negativas y contrato de alcance; sin eventos, dispatcher, migraciones, IAM/JWT/RBAC nuevos, QA/main/PROD.
- Requiere CI backend y seguridad real, Scope Lock y V5 exact-SHA; mantener VALIDANDO hasta A0-A4 y A5 independiente posterior; rollback git revert normal.

## 2026-11-14 — V5 C15-03.M02.03 persistencia y fixtures de auditoría de seguridad/tenancy [Javier :40]
- Define PD01–PD12 modelos conceptuales, IX01–IX08 índices hipotéticos, MF01–MF06 pasos condicionales y FX01–FX12 pares FUTUROS_NO_EJECUTADOS, sin instalar SQL ni migraciones.
- Traza AppDbContext, doce FK compuestas comerciales, guard de consultas y escrituras, JWT/RBAC y aislamiento tenant A/B; exige MySQL efímero y evidencia antes de M03.
- Sólo documento y CHANGELOG en dev por fast-forward/CAS, Scope Lock/V5 exact-SHA, A5 independiente pendiente y rollback git revert; sin QA/main/PROD, secretos, workers ni eventos.

## 2026-11-14 — V5 C14-04.M02.05 eventos conceptuales e idempotencia de puesta en marcha [Javier :20]
- Especifica EV01–EV14 señales NO_EMITIDAS, CT01–CT12 campos de contrato, ID01–ID12 políticas y PN01–PN10 pares FUTUROS_NO_EJECUTADOS sin instalar dispatcher ni webhooks.
- Diferencia BITACORA append-only, recalculo reactivo DAG/COLA y las seis Tasks Javier de un runtime de eventos; exige seguridad tenant A/B, JWT/RBAC y límites de retries/cuota.
- Entrega dos Markdown exclusivamente dev con CAS fast-forward, Scope Lock/V5 exact-SHA y A5 independiente; rollback git revert y sin QA/main/PROD.

## 2026-11-14 — V5 C14-04.M02.04 configuracion tipada y flags de puesta en marcha [Javier :20]
- Define CFG01–CFG16 claves hipotéticas tipadas, IV01–IV12 invariantes y FT01–FT10 casos positivos/negativos FUTUROS_NO_EJECUTADOS; no crea configuración runtime.
- Preserva JWT/RBAC/tenant A/B, secretos fuera de Git, separación de grant QA/main/PROD y Cloudinary vigente frente a R2 futuro; nunca confunde flag con permiso.
- Solo documento + CHANGELOG autocontenido dev, Scope Lock/V5 exact SHA, A5 independiente y rollback git revert; sin cambios de proveedor, datos ni código.

## 2026-11-14 — V5 C14-04.M02.03 persistencia índices fixtures de puesta en marcha [Javier :20]
- Documenta DB01–DB14 registros conceptuales, IX01–IX10 índices hipotéticos y FX01–FX12 pares de pruebas FUTUROS_NO_EJECUTADOS, sin afirmar mediciones, migraciones o restores.
- Referencia AppDbContext EF Core real y preserva ownership, JWT/RBAC, aislamiento tenant A/B, evidencia exact-SHA y rollback; ninguna modificación de código, SQL o infraestructura.
- Sólo dos Markdown con sección CHANGELOG autocontenida en dev, Scope Lock/V5 exact SHA, A5 independiente y git revert; sin QA/main/PROD.

## 2026-11-14 — V5 C14-04.M02.02 contratos API DTO y errores de puesta en marcha [Javier :20]
- Define OP01–OP10 operaciones conceptuales, doce campos DTO, ER01–ER12 y AC01–AC10 pares de casos FUTUROS_NO_EJECUTADOS; no implementa endpoints.
- Revisa las rutas reales PlataformaSoporteController y AuditoriaController sin modificar API, JWT/RBAC, tenants, SQL ni servicios; evita prometer restauración, observabilidad o release probados.
- Documento y CHANGELOG autocontenido en dev, exact-SHA Scope Lock/V5, A5 independiente y rollback git revert; sin QA/main/PROD, secretos ni datos reales.

## 2026-11-14 — V5 C14-04.M02.01 entidades y ownership de puesta en marcha [Javier :20]
- Formaliza PM01–PM16 registros conceptuales, INV01–INV16 de integridad y T01–T10 escenarios positivos/negativos FUTUROS_NO_EJECUTADOS, con evidencias y límites explícitos.
- Relaciona candidato SHA, manifest, autorización de entorno, tenant, salud, soporte, alertas, backup, restore y rollback sin crear tablas, APIs, eventos o servicios.
- Documentación exclusiva dev y CHANGELOG autocontenido; exige Scope Lock/V5 exact-SHA, A5 independiente y rollback git revert. Sin QA/main/PROD, migraciones, datos reales o secretos.

## 2026-11-14 — V5 C14-04.M01.06 contrato de bloqueo y salida de puesta en marcha [Javier :10]
- Define B01–B14 bloqueos fail-closed, RB01–RB10 medidas preventivas y L01–L14 condiciones de salida con predecesores, A5 independiente y recálculo DAG/COLA.
- Registra ocho pares positivos/negativos FUTUROS_NO_EJECUTADOS y deja NF NO_MEDIDOS para JWT/RBAC/tenant A/B, archivos, backups, alertas, CI, accesibilidad y rollback; sin pruebas de runtime ficticias.
- Entrega sólo documento y CHANGELOG autocontenido en dev con CAS fast-forward, checks Scope Lock/V5 exact-SHA, A5 posterior de otra Task Javier y prohibición de QA/main/PROD, DNS, datos, secretos y eventos.

## 2026-11-14 — V5 C03-04.M02.06 contrato de salida de continuidad y rollback [Javier :00]
- Cierra documentalmente M02 con CONT01–CONT14 para incidentes, revocacion, JWT/RBAC, backup/restore, aislamiento tenant A/B, idempotencia, RPO/RTO y auditoria; todos los casos runtime FUTUROS_NO_EJECUTADOS.
- Define dependencias de M03 y contratos base para C10/C12, evidencia causal, denegacion por defecto y RB01–RB10; no certifica restores, backups, cambios de estado de servidores ni objetivos de recuperacion.
- Solo documento V5 y CHANGELOG autocontenido en dev, fast-forward/CAS y Scope Lock/V5 exact-SHA con A5 de otra Task Javier; sin migraciones, jobs, webhooks, secretos, QA/main/PROD.

## 2026-11-14 — V5 C01-04.M02.06 contrato de salida documental del expediente universal [Javier :00]
- Establece OUT01–OUT14 con pruebas futuras positivas/negativas para expediente, tenant A/B, JWT/RBAC, cronologia, observaciones, decisiones, adjuntos, versionado, CAS e idempotencia.
- Define entradas reales M03.01–06, expediente de evidencias y RB01–RB10 de contencion/rollback; separa M02 LISTO_DEV documental de API, migraciones, seguridad, UI o eventos implementados.
- Publica unicamente dos Markdown en dev con fast-forward/CAS, Scope Lock y V5 exact-SHA, A5 independiente pendiente; sin QA/main/PROD, secretos ni datos vivos.

## 2026-11-14 — V5 C14-02.M02.06 contrato de salida de aceptación de seguridad [Javier :00]
- Cierra documentalmente el modelo M02 con SEC01–SEC12 para BOLA/IDOR, spoofing, JWT/RBAC/tenant A/B, archivos, jobs, cache, backups, sesiones, P0/P1 y publicación; exige negativos y evidencia futura real, sin afirmar cero vulnerabilidades.
- Define entradas M03.01–06, expediente causal, estados NOT_EVALUATED/FAIL/PASS_DEV/A5 y RB01–RB08 de contención y rollback; separa entrega documental de pentest, CI runtime, migraciones y promoción.
- Cambia sólo documento y CHANGELOG autocontenido en dev por fast-forward/CAS exact-HEAD, requiere Scope Lock/V5 exact-SHA y A5 independiente; sin secretos, endpoints, workers, SQL, QA/main/PROD.

## 2026-11-14 — V5 C03-04.M02.05 eventos e idempotencia documental de continuidad [Javier :50]
- Define EV01–EV14 señales conceptuales NO_PUBLICADAS, ID01–ID10 reglas futuras de idempotencia y LM01–LM12 limites de backup, restore, revocacion, incidente y rollback.
- Incorpora FT01–FT12 positivos/negativos FUTUROS_NO_EJECUTADOS; separa job nativo y autorreporte, exige hash/manifest, tenant A/B, permiso destructivo, RPO/RTO medidos y evidencia causal.
- Solo dos Markdown en dev por CAS de HEAD, Scope Lock/V5 exact SHA, A5 ajeno pendiente y rollback git revert; sin eventos, webhooks, workers, backup/restore, MySQL ni QA/main/PROD.

## 2026-11-14 — V5 C03-04.M02.04 configuracion tipada y flags de continuidad [Javier :40]
- Especifica dieciseis claves CFG propuestas NO_IMPLEMENTADAS, scopes PLATFORM/TENANT/CI y reglas V01-V16 fail-closed, sin instalar configuracion ni alterar JWT/RBAC.
- Documenta cuatro positivos y doce negativos FUTUROS_NO_EJECUTADOS sobre tenant A/B, restauracion descartable, flags, versionado y secretos.
- Publica solo dos Markdown dev por fast-forward/CAS, Scope Lock/V5 exact-SHA y A5 independiente posterior; rollback git revert, sin MySQL, workers, QA/main/PROD.

## 2026-11-14 — V5 C01-04.M02.05 eventos conceptuales e idempotencia de expediente [Javier :50]
- Define EV01–EV14 señales PROPUESTAS_NO_IMPLEMENTADAS, ID01–ID10 reglas futuras de idempotencia y LM01–LM12 limites de cuotas, retencion, correlacion, CAS y tenancy A/B.
- Incluye FT01–FT12 positivos/negativos FUTUROS_NO_EJECUTADOS; niega entregas exactly-once, webhooks, workers o eventos runtime y preserva autorizacion de empresa/recurso/rol.
- Solo documento V5 y CHANGELOG en dev, Scope Lock/V5 exact-SHA y A5 independiente pendiente; rollback git revert, sin API, DB, QA/main/PROD ni nuevas Tasks.

## 2026-11-14 — V5 C03-04.M02.03 persistencia e integridad futura de continuidad [Javier :40]
- Define once agregados propuestos con ownership tenant-first, catorce invariantes de indices, claves, permisos, auditoria append-only e idempotencia, sin crear schema ni migraciones.
- Documenta seis pasos de migracion/rollback FUTUROS_NO_EJECUTADOS en MySQL 8.4 efimero, mas 4 positivos y 12 negativos de aislamiento A/B y recuperacion.
- Solo dos Markdown en dev, CI Scope Lock/V5 exact SHA, A5 independiente pendiente, rollback git revert; sin acceso Aiven real, backups/restores, workers, QA/main/PROD.

## 2026-11-14 — V5 C03-04.M02.02 contratos API/DTO futuros de continuidad [Javier :40]
- Documenta doce operaciones propuestas, catorce validaciones, once errores genericos y contrato versionado para incidentes, evidencia, backup, restore y rollback, sin endpoints reales.
- Define cuatro positivos y doce negativos FUTUROS_NO_EJECUTADOS de JWT/RBAC/tenant A/B, PII, correlacion, idempotencia, concurrencia y destinos CI/dev permitidos.
- Solo documento y CHANGELOG autocontenidos dev con Scope Lock/V5 exact SHA, A5 independiente y rollback git revert; sin API, migraciones, worker, eventos, QA/main/PROD.

## 2026-11-14 — V5 C01-04.M02.04 configuracion tipada y flags del expediente universal [Javier :50]
- Define CFG01–CFG16 opciones conceptuales PROPUESTAS_NO_IMPLEMENTADAS por ámbito de plataforma, plan, tenant, sucursal, recurso y rol, con precedencia fail-closed y sin concesión RBAC.
- Incluye VT01–VT12 pares positivos/negativos FUTUROS_NO_EJECUTADOS, cache tenant-aware, invalidación de membresía, cursor seguro, decisiones, adjuntos privados y rollback aditivo.
- Cambia sólo documento V5 y CHANGELOG en dev, con Scope Lock/V5 exact-SHA y A5 independiente pendiente; no flags runtime, API, MySQL, secretos, QA/main/PROD ni dispatcher.

## 2026-11-14 — V5 C03-04.M02.01 entidades e invariantes de respuesta y recuperacion [Javier :40]
- Define doce entidades conceptuales y propiedad verificable de incidente, evidencia, respaldo, restauracion y rollback, sin crear entidades EF, tablas ni endpoints.
- Establece dieciseis invariantes fail-closed sobre identidad, JWT/RBAC, aislamiento tenant A/B, allowlist de CI/dev, secretos, concurrencia y pruebas de recuperacion futuras.
- Contrasta cinco fuentes M01 y cinco archivos reales, con cuatro positivos y doce negativos FUTUROS_NO_EJECUTADOS; solo dos Markdown en dev, Scope Lock/V5 exact SHA, A5 ajeno y rollback git revert.

## 2026-11-14 — V5 C01-04.M02.03 persistencia e índices del expediente universal [Javier :50]
- Define P01–P12 registros conceptuales, IX01–IX08 índices/restricciones candidatos, seis pasos de migración FUTURA_NO_EJECUTADA y FT01–FT12 pares positivos/negativos FUTUROS_NO_EJECUTADOS.
- Explicita ownership compuesto por tenant, paginación estable UTC+ID, idempotencia, versiones CAS, cuarentena de EmpresaId legacy nulo, adjuntos privados y pruebas A/B; no atribuye código o base de datos ejecutados.
- Cambios exclusivamente documentales en dev, Scope Lock/V5 exigidos en SHA exacto, rollback git revert, A5 independiente pendiente; no EF/MySQL/API/QA/main/PROD/eventos.

## 2026-11-14 — V5 C01-04.M02.02 contratos API DTO y errores del expediente universal [Javier :30]
- Documenta API01–12 operaciones propuestas, E01–14 errores, T01–14 pares positivos/negativos FUTUROS_NO_EJECUTADOS, cursores, idempotencia y version de observaciones/decisiones/adjuntos.
- No crea endpoints, DTO compilados, MySQL, Angular, JWT/RBAC ni permisos: tenant A/B y recursos propios siempre se verificaran en backend, sin reutilizar auditoria existente por deduccion.
- Changeset solo dos Markdown en dev, CAS expected HEAD, Scope Lock y V5 exact-SHA, A5 independiente pendiente y rollback git revert; no QA/main/PROD.

## 2026-11-14 — V5 C01-04.M02.01 modelo y ownership de expediente universal [Javier :30]
- Formaliza EX01–EX16 agregados conceptuales, IN01–IN18 invariantes y EV01–EV12 pares de escenarios FUTUROS_NO_EJECUTADOS de timeline, decisiones, observaciones, adjuntos y versiones.
- Preserva autoridad backend JWT/RBAC, tenant A/B, permiso/sucursal/ownership, retencion y procedencia real; nunca atribuye API, SQL, storage o UI implementados.
- Sólo documento V5 + bloque autocontenido CHANGELOG en dev con CAS expected SHA, Scope Lock/V5 exact-SHA, A5 independiente pendiente, rollback git revert, sin QA/main/PROD.

## 2026-11-14 — V5 C01-01.M03.06 auditoria segura Application de identidad [Javier :40]
- Incorpora adaptador .NET 10 sobre la guardia vigente, EventId fijo, correlacion obligatoria y codigos de decision sin PII, JWT, secretos ni excepciones.
- Incluye ocho pruebas xUnit positivas/negativas tenant A/B, revocacion, permisos, correlacion, errores y cancelacion; no amplia autoridad RBAC ni altera fuente de membresias.
- Solo Application, tests, documento y CHANGELOG en dev; CI pertinente exact-SHA y A5 independiente, rollback git revert normal; sin DI/API runtime, MySQL, workers, eventos, QA/main/PROD.

## 2026-11-14 — V5 C15-01.M02.06 contrato de salida y gates de cobertura [Javier :30]
- Concilia C15-01.M01.01–06, M02.01–05 y C00-03.M01.06 con LISTO+A5 de autor distinto, blobs Git, PLAN/COLA/DAG y BITACORA; no certifica cobertura completa de 2340 IDs.
- Define B01–B16 bloqueos, RB01–RB10 rollback y AT01–AT12 positivos/negativos FUTUROS_NO_EJECUTADOS para trazabilidad, CI, concurrencia, seguridad JWT/RBAC/tenant A/B y A5.
- Sólo dos Markdown en dev por fast-forward/expected HEAD, Scope Lock y procedencia V5 exact-SHA, A5 ajeno pendiente y git revert; sin runtime, QA/main/PROD ni eventos.

## 2026-11-14 — V5 C15-05.M02.06 gates y contrato de salida de auditoria operativa [Javier :30]
- Concilia M01.01–06 y M02.01–05 en LISTO+A5 independiente con blobs originales, mas DAG/COLA calculados; no presupone certificacion global de 2340 IDs.
- Define B01–B16 gates, RB01–RB10 rollback y A01–A12 pares positivos/negativos FUTUROS_NO_EJECUTADOS para owner, CI exact-SHA, auditoria append-only y aislamiento JWT/RBAC/tenant A/B.
- Publicacion solo de dos Markdown dev por fast-forward/expected HEAD; Scope Lock/V5 exact-SHA, A5 ajeno pendiente y git revert normal; sin eventos runtime, QA/main/PROD ni migraciones.

## 2026-11-14 — V5 C14-02.M02.05 eventos, idempotencia y correlacion de aceptacion seguridad [Javier :30]
- Define EV01–EV12 eventos conceptuales, ID01–ID10 contratos de idempotencia, LM01–LM12 limites, y CA01–CA12 positivos/negativos FUTUROS_NO_EJECUTADOS.
- Preserva JWT/RBAC/tenant A/B, CI exact-SHA, A5 independiente, owner exclusivo y BITACORA append-only; distingue autoinforme de run nativo y prohíbe afirmar CAS distribuido con Sheets sin primitiva real.
- Solo dos Markdown documentales en dev con Scope Lock/V5 exact-SHA, rollback git revert y A5 ajeno; sin eventos runtime, worker, webhook, SQL, QA/main/PROD.

## 2026-11-14 — V5 C14-02.M02.04 configuracion tipada y flags de aceptacion de seguridad [Javier :30]
- Define CFG01–CFG16 claves propuestas no implementadas, limites por ambito y precedencia fail-closed para JWT/RBAC/tenant A/B, scope, host, medios, CI y A5.
- Documenta VAL01–VAL12 positivos/negativos FUTUROS_NO_EJECUTADOS y distingue documento de pentest o certificacion P0/P1; sin alterar configuracion, servicios, eventos, QA/main/PROD.
- Cambia solo documento V5 y CHANGELOG autocontenido en dev, Scope Lock y procedencia exact-SHA, rollback git revert normal, A5 independiente pendiente.

## 2026-11-14 — V5 C15-05.M02.05 eventos e idempotencia documental de auditoria operativa [Javier :20]
- Define EV01–EV12, ID01–ID10, LM01–LM12 y doce pares positivos/negativos FUTUROS_NO_EJECUTADOS para procedencia, owner, CI, A5, rescate y rollback sin eventos runtime.
- Separa run nativo de autorreporte, impide doble autor/owner, distingue eventos conceptuales de transportes instalados y preserva JWT/RBAC/tenant A/B y BITACORA append-only.
- Dos Markdown solo en dev mediante fast-forward/CAS, Scope Lock/V5 exact-SHA y A5 independiente; sin dispatcher, webhooks, workers, SQL, QA/main/PROD ni compras.

## 2026-11-14 — V5 C15-05.M02.04 configuracion tipada y ambitos de auditoria operativa [Javier :20]
- Define CFG01–CFG16 PROPUESTAS_NO_IMPLEMENTADAS y VAL01–VAL12 positivos/negativos NO_EJECUTADOS para gobierno, owner, readback, CI exact-SHA, A5, telemetria y tenant A/B.
- Reusa M02.01–03, STANDARD/PLAN/COLA/DAG/BITACORA y separa ultimo autorreporte de run nativo; prohibe forzar gates o declarar CAS transaccional sin soporte real.
- Changeset solo de dos Markdown en dev, Scope Lock/V5 exact-SHA y A5 ajeno requeridos, rollback git revert; sin flags runtime, webhook, eventos, SQL, QA/main/PROD.

## 2026-11-14 — V5 C15-01.M02.05 eventos e idempotencia documental de cobertura [Javier :20]
- Define EV01–EV10 senales conceptuales, ID01–ID08 contratos de idempotencia, LM01–LM12 limites y T01–T12 pares positivos/negativos FUTUROS_NO_EJECUTADOS para auditoria de cobertura.
- Enlaza STANDARD, PLAN/COLA/DAG/BITACORA y contratos C15-01.M01.06/M02.01–04; separa run nativo, causalidad, SHA exacto, A5 independiente y aislamiento tenant A/B sin suplantar CAS.
- Solo Markdown y CHANGELOG autocontenido en dev con Scope Lock/V5 exact-SHA, rollback git revert; sin ejecutar event dispatcher, webhook, MySQL, UI ni QA/main/PROD.

## 2026-11-14 — V5 C14-02.M02.03 persistencia, indices y fixtures de aceptacion de seguridad [Javier :10]
- Define doce agregados y ocho restricciones de persistencia PROPUESTOS_NO_IMPLEMENTADOS, seguridad tenant A/B JWT/RBAC y control de version CAS.
- Documenta seis pasos de migracion/rollback FUTUROS y 4 positivos +12 negativos FUTUROS_NO_EJECUTADOS sin tocar MySQL, EF, backups ni QA/PROD.
- Solo dos Markdown en dev con Scope Lock/V5 exact SHA, A5 independiente pendiente y revert normal; sin migraciones, endpoints ni despacho de eventos.

## 2026-11-14 — V5 C14-02.M02.02 API y DTO de aceptacion de seguridad [Javier :10]
- Define doce operaciones y contratos de API PROPUESTOS_NO_IMPLEMENTADOS, envelope DTO V1 conceptual, doce condiciones de error y semantica fail-closed tenant A/B JWT/RBAC.
- Documenta cuatro positivos y doce negativos FUTUROS_NO_EJECUTADOS, compatibilidad de consumidores y prohibicion de declarar P0/P1 cero, restauracion o pentest sin evidencia.
- Solo documento y CHANGELOG en dev, Scope Lock/V5 exact-SHA, A5 independiente pendiente y rollback git revert; sin endpoints, permisos, QA/main/PROD ni eventos runtime.

## 2026-11-14 — V5 C03-04.M01.06 bloqueos, rollback y liberacion documental de continuidad [Javier :10]
- Concilia las cinco entregas M01.01-05 con sus blobs Git y el baseline de backup/restore dev sin atribuir restauraciones ejecutadas.
- Define B01-B12, RB01-RB08, L01-L12 y catorce oraculos P/N FUTUROS_NO_EJECUTADOS; mantiene JWT/RBAC, tenant A/B, permisos y RPO/RTO fail-closed.
- Solo documento y CHANGELOG en dev, A5 independiente pendiente, Scope Lock/V5 exact-SHA, rollback git revert; sin QA/main/PROD ni runtime nuevo.

## 2026-11-14 — V5 C15-01.M02.04 configuracion tipada, ambitos y flags de cobertura [Javier :50]
- Define CFG01–CFG16 de claves PROPUESTAS_NO_IMPLEMENTADAS con defaults fail-closed y VAL01–VAL12 escenarios positivos/negativos FUTUROS_NO_EJECUTADOS para auditoria institucional.
- Separa gobierno/HEAD, auditoria, microtarea, ejecucion nativa y frontera tenant JWT/RBAC; no modifica CONFIG, formulas, flags runtime, tareas o autorizaciones.
- Solo documento y CHANGELOG autocontenido en dev con Scope Lock y V5 exact-SHA, A5 independiente pendiente, rollback git revert y sin QA/main/PROD ni eventos.

## 2026-11-14 — V5 C14-02.M02.01 ownership e invariantes de aceptación de seguridad [Javier :00]
- Modela SE01–SE14 y SI01–SI16 para sujeto, membresía, RBAC, tenant A/B, hosts, medios, hallazgos y revisión independiente, sin crear tablas ni endpoints.
- Especifica SA01–SA12 escenarios positivos/negativos FUTUROS_NO_EJECUTADOS; no inventa pentest, BOLA/IDOR certificados ni ausencia de P0/P1.
- Sólo dos Markdown en dev por fast-forward CAS con Scope Lock/V5 exact SHA, rollback git revert y A5 por otra Task Javier; sin QA/main/PROD.

## 2026-11-14 — V5 C15-01.M02.03 persistencia e indices de auditoria de cobertura [Javier :50]
- Propone PD01–PD12 registros logicos, IX01–IX06 indices condicionales y FX01–FX12 casos positivos/negativos FUTUROS_NO_EJECUTADOS; no crea tablas ni migraciones.
- Contrasta AppDbContext, TenantQueryBypassGuard y CanonicalMySqlPhysicalContract reales con la autoridad STANDARD/PLAN/COLA/DAG/BITACORA; no certifica cero huerfanos, P0/P1 ni integridad runtime.
- Solo nuevo Markdown y CHANGELOG autocontenido en dev, Scope Lock/V5 exact-SHA, A5 independiente pendiente y rollback git revert; sin API, datos, migraciones, eventos ni QA/main/PROD.

## 2026-11-14 — V5 C15-04.M02.06 gates y rollback de contratos UX [Javier :00]
- Verifica precondiciones M02.01–05 LISTO/A5 y define B01–B14 para seguridad, WCAG, UX, rendimiento, integraciones y reversión, sin atribuir certificación de runtime.
- Registra AT01–AT12 positivos/negativos FUTUROS_NO_EJECUTADOS; M03 depende del nuevo DAG y del A5 independiente de este terminal, jamás del texto del CHANGELOG.
- Publica sólo nuevo Markdown y sección autocontenida de CHANGELOG en dev por fast-forward/CAS; Scope Lock/V5 exact SHA, rollback git revert, sin QA/main/PROD ni eventos.

## 2026-11-14 — V5 C15-01.M02.02 contratos API/DTO, errores y compatibilidad de cobertura [Javier :50]
- Propone OP01–OP08 lecturas de auditoria, E01–E12 respuestas de error y VAL01–VAL10 de validacion con T01–T12 positivos/negativos FUTUROS_NO_EJECUTADOS; no agrega endpoints.
- Contrasta AuditoriaController, AutomatizacionesController y PlataformaSesionController reales sin confundir la auditoria comercial con el control institucional ni atribuir autoridad del cliente.
- Cambios exclusivamente documentales en dev y CHANGELOG autocontenido, Scope Lock/V5 exact-SHA, A5 ajeno pendiente; sin implementaciones MySQL/JWT/RBAC, webhooks, migraciones, QA/main/PROD.

## 2026-11-14 — V5 C15-05.M02.03 persistencia, índices y fixtures auditoría operativa [Javier :00]
- Define PD01–PD12 como invariantes de fuente autorizada, IX01–IX06 como índices hipotéticos y FX01–FX12 como escenarios sintéticos FUTUROS_NO_EJECUTADOS; no crea EF migrations ni tablas.
- Contrasta modelos C15-05.M02.01/M02.02 con PLAN/COLA/DAG/BITACORA, A5 independiente, SHA CI y seguridad tenant A/B, JWT/RBAC; no atribuye transacción distribuida al Sheet.
- Sólo dos Markdown publicados en dev con CAS/fast-forward, Scope Lock y procedencia V5 requeridos, rollback git revert; sin bases reales, QA/main/PROD, Tasks ni eventos.

## 2026-11-14 — V5 C15-05.M02.02 contrato API/DTO, errores y compatibilidad auditoría operativa [Javier :00]
- Define siete operaciones lógicas NO_IMPLEMENTADAS, ocho DTO conceptuales, doce códigos de error y doce pares positivos/negativos FUTUROS_NO_EJECUTADOS de auditoría operativa.
- Preserva AuditoriaController, JWT/RBAC/tenant A/B, seis Tasks Javier, SHA exacto, CAS, A5 independiente y distinción telemetría local/proveedor nativo; sin crear endpoint ni transportar datos.
- Changeset sólo de dos Markdown en dev, Scope Lock y procedencia V5 exact-SHA requeridos; rollback git revert, sin QA/main/PROD ni webhook/event dispatcher.

## 2026-11-14 — V5 C15-01.M02.01 modelo de propiedad y trazabilidad de cobertura [Javier :50]
- Define CT01–CT16 entidades conceptuales, CT06 matriz de enlaces requisito-ID y CV01–CV12 de casos positivos/negativos FUTUROS_NO_EJECUTADOS, sin afirmar cero huerfanos o P0/P1.
- Delimita contratos contra STANDARD/PLAN/COLA/DAG/BITACORA, A5 independiente y CAS, con controles JWT/RBAC/tenant y prohibicion de falsificar SI o reescribir historia.
- Sólo dos Markdown en dev, Scope Lock/V5 exact-SHA exigidos, rollback git revert, sin migraciones, endpoints, eventos ni QA/main/PROD.

## 2026-11-14 — V5 C15-05.M02.01 modelo de ownership e invariantes de auditoría operativa [Javier :20]
- Formaliza OM01–OM14 registros conceptuales, INV01–INV16 de seguridad/concurrencia y AT01–AT12 de casos FUTUROS_NO_EJECUTADOS para gobierno de 2340 IDs.
- Traza PLAN/COLA/DAG/BITACORA, seis Tasks Javier, Scope Lock y M01.06 A5 independiente; separa telemetría de último reporte frente al estado nativo, sin certificar cero carreras ni cobertura runtime.
- Sólo nuevo Markdown y CHANGELOG autocontenido en dev, Scope Lock y verificador V5 exact-SHA obligatorios, A5 pendiente de otra Task, rollback git revert y sin eventos/webhooks/QA/main/PROD.

## 2026-11-14 — V5 C15-04.M02.05 eventos, idempotencia, correlación y límites UX [Javier :20]
- Especifica EV01–EV08, ID01–ID08, LM01–LM10 y doce pares positivos/negativos FUTUROS_NO_EJECUTADOS para señales documentales de auditoría UX, sin activar transporte.
- Contrasta MensajeOutbox existente, AppDbContext, contratos UX M01/M02 y seguridad tenant A/B, JWT/RBAC, sin afirmar un dispatcher o webhook real.
- Dos Markdown en dev, Scope Lock y procedencia V5 exact-SHA requeridos, readback institucional, A5 de otra Task Javier pendiente; ningún evento entre Tasks ni QA/main/PROD.

## 2026-11-14 — V5 C15-04.M02.04 configuración tipada y feature flags UX [Javier :10]
- Especifica CFG01–CFG14 como claves PROPUESTAS no implementadas y VAL01–VAL12 como pruebas negativas futuras, con defaults cerrados, separación de fuentes y ámbitos.
- Contrasta environment development/production, tenant-context backend-verified y permisos runtime; no crea flags, endpoints, migraciones ni cambios en configuración productiva.
- Documenta solo este Markdown y CHANGELOG completo con Scope Lock/V5 exact-SHA, rollback git revert y A5 independiente pendiente; sin QA/main/PROD.

## 2026-11-14 — V5 C15-04.M02.03 persistencia, índices, migraciones y fixtures UX [Javier :20]
- Define PD01–PD12 de evidencia mínima, IX01–IX06 de índices condicionales y FX01–FX12 de pruebas sintéticas FUTURAS_NO_EJECUTADAS; ningún esquema, tabla, índice o migración real es alterado.
- Contrasta AppDbContext, guards de tenant, relaciones físicas compuestas y M01/M02 documentales; preserva JWT/RBAC, separación A/B, privacidad y fail-closed.
- Changeset exclusivamente documental en dev, Scope Lock y procedencia V5 exact-SHA obligatorios, readback institucional y A5 pendiente de otra Task Javier; rollback git revert y sin QA/main/PROD.

## 2026-11-14 — V5 C15-04.M02.02 contrato API/DTO, errores y compatibilidad UX [Javier :10]
- Modela OP01–OP08 y contratos Audit*V1 como propuesta NO_IMPLEMENTADA, ERR01–ERR10 y matriz de compatibilidad con DashboardController, authGuard y permisoGuard existentes.
- Preserva backend como autoridad JWT/RBAC/tenant, evita exponer PII y separa resultados NO_EJECUTADOS de PASS; no añade endpoints, EF, migraciones, datos o eventos.
- Publica solo documento y CHANGELOG íntegro por fast-forward CAS, Scope Lock y V5 exact-SHA, rollback git revert y A5 independiente pendiente.

## 2026-11-14 — V5 C15-04.M02.01 modelo conceptual UX, propiedad e invariantes [Javier :10]
- Delimita UX01–UX12 como registros de evidencia propuestos, INV01–INV16 de seguridad/accesibilidad y AT01–AT10 futuros sin atribuir pruebas WCAG o métricas ejecutadas.
- Conserva seis antecedentes C15-04.M01.01–06 LISTO/A5 independientes y separa modelo M02 de API, datos, eventos, M03 y permisos productivos.
- Entrega únicamente Markdown y CHANGELOG autocontenido, Scope Lock y procedencia V5 exact-SHA requeridos, reversión git revert y A5 ajeno pendiente.

## 2026-11-14 — V5 C15-05.M01.06 bloqueos, rollback y cierre de auditoría operativa [Javier :10]
- Define B01–B14 para identidad, PLAN/COLA/DAG, CAS, BITACORA, CI exact-SHA, procedencia de CHANGELOG, A5 y protección de ambientes; pruebas de concurrencia y cero P0/P1 siguen NO_CERTIFICADOS.
- Verifica M01.01–05 LISTO/A5 independientes y condiciona M02 a nuevo preflight y A5 propio de M01.06; no libera runtime, migraciones, QA/main/PROD ni eventos.
- Cambia solamente el nuevo Markdown y una sección completa de CHANGELOG; rollback git revert normal, Scope Lock y V5 requeridos en SHA publicado.

## 2026-11-14 — V5 C15-04.M01.06 bloqueos y salida documental UX/accesibilidad/rendimiento [Javier :00]
- Fija B01–B12 para teclado, foco, zoom, formularios, motion, temas, tenancy, estados de red, CI SHA y A5; no afirma WCAG ni métricas ejecutadas.
- Valida M01.01–05 LISTO y A5 independiente por otra Task; el avance a M02 exige nuevo preflight sin autorizar migraciones, QA/main/PROD ni endpoints.
- Dos Markdown en dev y CHANGELOG autocontenido, rollback git revert y BITACORA append-only; A5 de esta nueva unidad pendiente de Task distinta.

## 2026-11-14 — V5 C15-03.M01.06 bloqueos y liberación documental de auditoría de seguridad/tenancy [Javier :00]
- Fija B01–B12 sobre JWT/RBAC, tenant A/B, sucursales, EF, FK, hosts, soporte, medios, CI exact-SHA y A5; pruebas negativas de runtime y cero P0/P1 permanecen NO_CERTIFICADOS.
- Contrasta M01.01–05 LISTO/A5 independiente y define rollback fail-closed; M02 exige nuevo preflight y jamás se autorizan despliegues o migraciones por contrato.
- Publicación sólo de dos Markdown en dev, CHANGELOG indivisible, A5 propia pendiente de otra Task; sin MySQL real, secretos, QA/main/PROD ni event dispatcher.

## 2026-11-14 — V5 C15-01.M01.06 bloqueos y salida documental de cobertura [Javier :00]
- Establece B01–B12 para censo de 2340 IDs, huérfanos, duplicados, DAG, CAS, CI SHA, A5 independiente, seguridad y entorno; cero huérfanos/P0/P1 sigue NO_CERTIFICADO.
- Revisa M01.01–05 LISTO/A5 independiente y C00-03.M01.06 A5; M02 sólo se considera bajo nuevo preflight sin liberar runtime, migraciones ni QA/main/PROD.
- Dos Markdown en dev, reversión por git revert normal y BITACORA append-only; A5 de esta nueva entrega pendiente de otra Task Javier.

## 2026-11-14 — Reparación de CI para C01-01.M03.05 (orden directa del propietario)
- Reutilizar la contraseña validada al calcular su hash para evitar el argumento nullable, sin relajar las reglas de alta empresarial.
- Migrar tres contratos HTTP al HostBuilder y servidor de pruebas moderno de .NET 10, preservando rutas, respuestas y middleware de seguridad.
- Actualizar dos aserciones xUnit a las formas admitidas por los analizadores; conservar sus comprobaciones.
- Cambio técnico correctivo sobre dev para desbloquear A4; DEV 06 conserva la microtarea y A5 requiere Task distinta. Sin QA/main/PROD ni cambios a migraciones o secretos.

## 2026-11-14 — V5 C14-02.M01.06 bloqueos y liberación documental de aceptación de seguridad [Javier :40]
- Define B01–B12 como gates futuros sobre tenant A/B, JWT/RBAC, BOLA/IDOR, hosts, archivos, jobs, backups, CI exact SHA y revisión A5 separada.
- Documenta contención, rollback sin force-push y liberación condicionada: cero P0/P1 permanece NO_CERTIFICADO; M02 no gana permisos de runtime o despliegue.
- Sólo nuevo Markdown y CHANGELOG en dev; A5 independiente pendiente, sin pruebas HTTP/MySQL reales ni QA/main/PROD, proveedores, webhooks o nuevas Tasks.

## 2026-11-14 — V5 C15-05.M01.05 criterios de aceptación de auditoría operativa [Javier :40]
- Define 16 pares P/N FUTUROS_NO_EJECUTADOS sobre PLAN/COLA/DAG, claims CAS, BITACORA, CI exact-SHA, A5 y señales nativas.
- Fija 12 requisitos NF NO_MEDIDOS y evidencia exigida para 2340 IDs; cero huérfanos, duplicados críticos y P0/P1 permanecen NO_CERTIFICADOS.
- Cambio documental en dev, sin runtime/DB/worker/webhooks/QA/main/PROD; A5 pendiente de otra Task Javier, rollback git revert normal.

## 2026-11-14 — Corrección dirigida C15-03.M01.04 para recuperar Scope Lock
- Se sustituye en D02 un término heredado no permitido por `inventario causal`; no cambia el sentido técnico, el alcance del documento ni el verificador.
- Recuperación solicitada por el propietario. Se mantendrá VALIDANDO hasta verificar el SHA de `dev` con CI causal y revisión A5 independiente; sin operaciones en QA/main/PROD.

## 2026-11-14 — V5 C15-03.M01.04: riesgos y decisiones de seguridad/tenancy [Javier :10]
- Registra R01–R14 riesgos potenciales, S01–S08 supuestos NO_VERIFICADOS y D01–D08 decisiones pendientes, enlazados a quince blobs reales de M01.03 y contratos M01.01/M01.02.
- Define P01/N01–P08/N08 como casos FUTUROS_NO_EJECUTADOS; trata la consulta EF sin scope y su cuarentena externa como hipótesis a probar, nunca como vulnerabilidad certificada.
- Solo documento y CHANGELOG, Scope Lock y procedencia V5 exact SHA al publicar; A5 ajeno pendiente, rollback git revert, sin IAM/JWT/DB/QA/main/PROD ni dispatcher.

## 2026-11-14 — V5 C15-03.M01.03: fuentes y brechas de auditoría security/tenancy [Javier :10]
- Inventaría quince rutas y blobs Git reales de filtros JWT/RBAC/tenant, middleware, consultas y escritura EF, control plane, storefront y tests; lista explícita sin declarar suites ejecutadas ni cobertura total.
- Identifica doce brechas G01–G12 como NO_VERIFICADAS/FUTURAS y mapea NR01–NR16 e IO/T anteriores; la dependencia de la cuarentena del bypass EF requiere prueba integrada, no se declara vulnerabilidad.
- Solo Markdown y CHANGELOG, A5 independiente pendiente, rollback git revert; sin nuevos endpoints, DB, workers, secretos, QA/main/PROD ni tests de runtime atribuidos.

## 2026-11-14 — V5 C15-03.M01.02: actores e interfaces de auditoría de seguridad/tenancy [Javier :10]
- Delimita doce actores A01–A12 y doce entradas/salidas IO01–IO12, diferenciando JWT/RBAC/empresa/sucursal, soporte, tienda pública, archivos y jobs futuros sin inventar endpoints.
- Define T01–T10 positivos/negativos FUTUROS_NO_EJECUTADOS para fronteras cliente/API, membresía, EF, asociaciones, Host público, soporte, medios, worker y auditoría; fuentes Git reales con blobs.
- Sólo Markdown y CHANGELOG en dev; A5 independiente pendiente, rollback git revert; sin seguridad/runtime/DB/Cloudflare/QA/main/PROD ni liberación documental de fases terminales.

## 2026-11-14 — V5 C15-03.M01.01: alcance y no-regresión de auditoría de seguridad/tenancy [Javier :10]
- Define NR01–NR16 y P01/N01–P08/N08 como contrato FUTURO_NO_EJECUTADO de aislamiento tenant A/B, sucursal, propiedad, archivos, jobs y control plane; ninguna prueba HTTP/MySQL ni certificación de seguridad se atribuye.
- Vincula nueve fuentes reales y sus blobs del HEAD, destacando que TenantQueryBypassGuard sin empresa autorizada depende de cuarentena exterior y requiere test negativo, sin afirmar una vulnerabilidad probada.
- Sólo nuevo Markdown y CHANGELOG; A5 independiente pendiente, rollback git revert, sin IAM/JWT/RBAC/DB/servicios/QA/main/PROD ni liberación automática M01.06.

## 2026-11-14 — V5 C15-02.M01.05 criterios de aceptación de auditoría arquitectura/migración [Javier :00]
- Define 16 pares positivos/negativos FUTUROS_NO_EJECUTADOS para inventario Git, JWT/RBAC/tenant A/B, MySQL/restore, Cloudinary/R2, DNS/TLS y SHA/CI/A5 independientes.
- Propone doce requisitos no funcionales NO_MEDIDOS; distingue documentación contractual de evidencia real y preserva gate DAG NO para M01.06/M02.
- Sólo Markdown y CHANGELOG dev, A5 independiente pendiente; sin servicios Cloudflare/Hostinger, secretos, workers, migración, QA/main/PROD ni pruebas runtime declaradas.

## 2026-11-14 — V5 C15-02.M01.04 riesgos y decisiones de auditoría arquitectura/migración [Javier :00]
- Registra 14 riesgos POTENCIALES_NO_VERIFICADOS, diez supuestos falsables y diez decisiones ABIERTAS basadas en 21 fuentes Git y 12 brechas de M01.03; no declara P0/P1 reales.
- Define ocho escenarios positivos/negativos FUTUROS_NO_EJECUTADOS de JWT/RBAC/tenant A/B, MySQL/restore, Cloudinary/R2, DNS/TLS, CI exact-SHA e independencia A5.
- Sólo Markdown y CHANGELOG en dev, sin migración, eventos, secretos, pruebas de proveedores, QA/main/PROD ni autorización de cutover.

## 2026-11-14 — V5 C15-02.M01.03 inventario contrastable de auditoría arquitectura/migración [Javier :00]
- Inventaría 21 fuentes Git con blobs, distingue código existente de infraestructura activa y conserva SHA/cadena de evidencia para la futura auditoría C15-02.
- Registra 12 brechas NO_ACREDITADAS y ocho pares P/N FUTUROS_NO_EJECUTADOS para tenancy JWT/RBAC, MySQL, Cloudinary/R2, DNS/TLS, CI, restauración y rollback.
- Sólo documento de inventario y CHANGELOG en dev; sin pruebas HTTP/MySQL/Playwright ejecutadas, sin eventos, migración, secretos, QA/main/PROD; A5 independiente pendiente.

## 2026-11-14 — V5 C15-02.M01.02 actores e interfaces de auditoria de migracion [Javier :50]
- Prepara 12 participantes, 12 contratos de entrada/salida y 10 fronteras de confianza sobre frontend, API JWT/RBAC, tenant A/B, MySQL/EF, archivos, workers, CI y DNS/TLS.
- Propone ocho pares P/N FUTUROS_NO_EJECUTADOS; la topologia objetivo Hostinger/Cloudflare/R2 no se declara desplegada ni auditada.
- Solo Markdown y CHANGELOG en dev, A0-A4 documentales y A5 independiente pendiente; sin credenciales, QA/main/PROD, eventos, migracion ni compras.

## 2026-11-14 — V5 C15-02.M01.01 alcance de auditoria de arquitectura/migracion [Javier :50]
- Define alcance documental, topologia actual declarada y objetivo futuro Hostinger/Cloudflare/R2, sin afirmar proveedores activos ni migracion ejecutada.
- Fija 16 invariantes NR y 8 pares ARQ positivos/negativos FUTUROS_NO_EJECUTADOS para JWT/RBAC/tenant A/B, backup/restore MySQL, archivos, DNS/TLS, jobs y exact-SHA CI.
- Solo Markdown y CHANGELOG en dev, A5 independiente pendiente; sin codigo, secretos, eventos, QA/main/PROD, despliegues ni compras.

## 2026-11-14 — V5 C15-01.M01.05 criterios de aceptación de cobertura [Javier :40]
- Define 16 pares de aceptación positiva/negativa FUTUROS_NO_EJECUTADOS para requisitos, PLAN/COLA/DAG, autoría, Git/CI, tenant A/B, A5, seguridad y cierre causal.
- Propone doce requisitos no funcionales NO_MEDIDOS y formato de evidencia por CASE_ID; la meta de cero huérfanos/duplicados críticos permanece NO_CERTIFICADA.
- Sólo documento y CHANGELOG en dev, A0–A4 proporcionales y A5 independiente pendiente; sin pruebas runtime inventadas, workers, eventos, QA/main/PROD o tareas nuevas.

## 2026-11-14 — V5 C15-01.M01.04 riesgos y decisiones de cobertura [Javier :40]
- Delimita 14 riesgos POTENCIALES_NO_VERIFICADOS, diez supuestos pendientes y diez decisiones abiertas sobre requisitos, IDs, DAG, BITACORA, Git/CI y seguridad multiempresa.
- Prepara diez pares P/N FUTUROS_NO_EJECUTADOS, preservando que cero huérfanos/duplicados críticos y P0/P1=0 no están certificados; no inventa vulnerabilidades.
- Sólo documento y CHANGELOG en dev, A0–A4 documentales y A5 independiente pendiente; sin runtime, eventos, QA/main/PROD ni cambios a seis Tasks.

## 2026-11-14 — V5 C15-01.M01.03 inventario de fuentes de cobertura [Javier :40]
- Se leyeron 18 blobs de Git del baseline 83089fdbdc9a: contratos .NET/Angular/EF, seguridad declarada, gobernanza y CI; evidencia estática, no ejecución funcional.
- Define ocho uniones futuras de trazabilidad y diez brechas NO_ACREDITADAS; seis pares P/N FUTUROS_NO_EJECUTADOS, sin certificar cero huérfanos en 2340 IDs.
- Sólo Markdown y CHANGELOG en dev, A0–A4 documentales y A5 pendiente de otra Task Javier; sin workers, eventos, migraciones, QA/main/PROD ni Secrets.

## 2026-11-14 — V5 C15-01.M01.02 actores e interfaces de auditoria de cobertura [Javier :30]
- Define catorce actores, doce entradas, doce salidas, diez fronteras y diez pares PN FUTUROS_NO_EJECUTADOS para comparar V5, PLAN, COLA, DAG, BITACORA, SHA/checks y Tasks Javier por ID exacto.
- Preserva autoridad dev, CAS exclusivo, A5 independiente, JWT/RBAC/tenant A/B y evidencia causal, sin afirmar cero huérfanos/duplicados críticos, pruebas runtime ni auditoría exhaustiva completada.
- Sólo documento y CHANGELOG en dev, Scope Lock/V5 success exact-SHA y readback; no QA/main/PROD, nuevas Tasks, workers, migración, datos, secretos o webhooks.

## 2026-11-14 — V5 C15-01.M01.01 alcance y no-regresion de auditoria de cobertura [Javier :30]
- Prepara auditoría de trazabilidad V5 sobre requisitos, 2340 IDs PLAN, COLA/DAG/BITACORA, GitHub y seis Tasks Javier, con dieciséis invariantes NR y ocho pares P/N FUTUROS_NO_EJECUTADOS.
- Preserva dependencias causales, autoría y A5 independiente, seguridad tenant A/B/JWT/RBAC y separación de dev versus QA/main/PROD; cero huérfanos o duplicados críticos permanece NO_CERTIFICADO, sin auditoría exhaustiva fingida.
- Cambia sólo documento de alcance y CHANGELOG en dev; A0–A4 documentales y Scope Lock/V5 exact SHA, rollback git revert; sin backend, UI, DB, DNS, proveedores, webhooks, secrets ni Tasks nuevas.

## 2026-11-14 — V5 C14-04.M01.05 criterios de aceptación de puesta en marcha [Javier :30]
- Define dieciséis pares positivos/negativos FUTUROS_NO_EJECUTADOS de SHA/CI, landing/host, JWT/RBAC/tenant A/B, archivos, backups, health, alertas, incidentes, accesibilidad y rollback.
- Registra doce requisitos no funcionales NO_MEDIDOS con oráculos y evidencia causal exigida; enlaza contratos C14-04.M01.01–03 y brechas G01–G12, sin afirmar runtime ni proveedores certificados.
- Sólo Markdown y CHANGELOG en dev, A0–A4 documentales con checks Scope Lock/V5 exact-SHA y A5 pendiente a otra Task Javier; sin QA/main/PROD, DNS, R2, MySQL real, secretos, eventos o compras.

## 2026-11-14 — V5 C14-04.M01.04 riesgos y decisiones de puesta en marcha [Javier :20]
- Registra 14 riesgos POTENCIALES_NO_VERIFICADOS, 8 supuestos y 8 decisiones ABIERTAS para host, tenant, archivos, backup, incidentes, CI, observabilidad y rollback.
- Define ocho pares P/N FUTUROS_NO_EJECUTADOS sin declarar incidentes, RPO/RTO medidos, vulnerabilidades confirmadas, cuentas R2 o despliegues autorizados.
- Sólo documento y CHANGELOG dev, A0–A4 documentales y A5 independiente posterior; Scope Lock/V5 exact SHA, sin código runtime, QA/main/PROD ni compras.

## 2026-11-14 — V5 C14-04.M01.03 inventario para puesta en marcha [Javier :20]
- Verifica 19 rutas y blobs Git de gobierno, frontend SEO, API, CI, backups y arquitectura sobre HEAD exacto; no confunde archivos existentes con sistemas operativos.
- Identifica 12 brechas NO_ACREDITADO y ocho pares P/N FUTUROS_NO_EJECUTADOS para dominios, tenant A/B, archivos, restore, alertas, accesibilidad y promoción.
- Sólo documento más CHANGELOG dev, A0–A4 documental y A5 independiente pendiente; sin HTTP/MySQL/Playwright reales, QA/main/PROD, Cloudflare/R2, eventos ni secretos.

## 2026-11-14 — V5 C14-04.M01.02 actores y fronteras de puesta en marcha [Javier :20]
- Registra 14 actores, 12 entradas, 12 salidas, diez fronteras de confianza y diez pares PN FUTUROS_NO_EJECUTADOS para lanzamiento, soporte, dominios, storage, alertas y backup.
- Preserva separación DEV/QA/PROD, JWT/RBAC/tenant A/B, archivos, MySQL y secretos; ningún proveedor, alerta, restore, SLO o interfaz se afirma implementado.
- Sólo documento más CHANGELOG dev; A0–A4 documentales, Scope Lock/V5 exact SHA y A5 de Task Javier distinta pendientes; no runtime, eventos ni compras.

## 2026-11-14 — V5 C14-04.M01.01 alcance de puesta en marcha y soporte [Javier :20]
- Delimita preparación documental de landing, dominios, almacenamiento, soporte, alertas, backup/restore y respuesta a incidentes; sin habilitar infraestructura, DNS ni promoción.
- Define NR01–NR16 y ocho pares P/N FUTUROS_NO_EJECUTADOS, separando JWT/RBAC/tenant A/B, SHA/CI causal, integridad MySQL y continuidad operativa.
- Sólo documento y CHANGELOG en dev; Scope Lock/V5 exact SHA, A0–A4 documentales y A5 independiente pendiente; sin runtime, QA/main/PROD, secretos ni eventos.

## 2026-11-14 — V5 C14-03.M01.05 criterios de promoción controlada [Javier :10]
- Define 16 pares P/N FUTUROS_NO_EJECUTADOS y 10 requisitos NF NO_MEDIDOS para SHA/artefacto, CI, JWT/RBAC/tenant, MySQL, accesibilidad, rollback y auditoría.
- Distingue pruebas causales de Angular/.NET/Playwright/MySQL y autorización separada DEV/QA/main/PROD; no certifica ni despliega infraestructura.
- Solo Markdown y CHANGELOG en dev con Scope Lock/V5 exact SHA y A5 independiente pendiente; sin secretos, migraciones, pagos ni eventos.

## 2026-11-14 — V5 C14-03.M01.04 riesgos de promoción controlada [Javier :10]
- Registra 14 riesgos POTENCIALES, 8 supuestos NO_VERIFICADOS y 8 decisiones ABIERTAS sobre CI exact-SHA, JWT/RBAC/tenant, paridad y rollback.
- Define ocho parejas P/N FUTURAS_NO_EJECUTADAS; no declara seguridad/QA/PROD certificadas ni vulnerabilidades demostradas.
- Solo documento y CHANGELOG dev, Scope Lock/V5 exact-SHA y A5 independiente pendiente; sin deployment, secretos ni eventos.

## 2026-11-14 — V5 C14-03.M01.03 inventario de promoción controlada [Javier :10]
- Verifica 12 rutas y blobs Git de CI, paridad, Scope Lock, frontend, backend y gobierno; identifica 12 brechas NO_ACREDITADO, sin equiparar archivos a ejecuciones.
- Relaciona 8 pares P/N FUTUROS_NO_EJECUTADOS con NR01–NR16 y actores/interfaces M01.01–02; exige separación DEV/QA/PROD, JWT/RBAC/tenant A/B y SHA exacto.
- Solo documento y CHANGELOG, con Scope Lock/V5 exact SHA y A5 independiente posterior; no deployments, datos productivos, secretos, eventos ni PR/branches.

## 2026-11-14 — V5 C14-03.M01.02 actores, interfaces y fronteras de promoción [Javier :10]
- Documenta 12 actores de análisis, 12 fuentes Git/Sheets y ocho pares P/N FUTUROS_NO_EJECUTADOS para separación DEV/QA/PROD, paridad, autorización y rollback.
- Delimita SHA/artefacto, Scope Lock, CI causal, JWT/RBAC/tenant A/B y secreto mínimo; existencia de workflows no se trata como ejecución o aprobación de release.
- Solo Markdown y CHANGELOG en dev; Scope Lock/V5 exact SHA y A5 independiente pendiente. No modifica pipelines, ramas, infraestructura, cuentas ni eventos.

## 2026-11-14 — V5 C14-03.M01.01 alcance de promoción controlada [Javier :10]
- Establece objetivo DEV/QA/PROD separados, checklist futuro de SHA/artefacto/configuración, 16 invariantes NR y diez pares P/N FUTUROS_NO_EJECUTADOS.
- Declara gates de JWT/RBAC/tenant A/B, MySQL, CI exact SHA, aprobación independiente y reversión; no certifica paridad de ambientes ni despliega fuera de dev.
- Solo Markdown + CHANGELOG; Scope Lock/V5 exact SHA y A5 independiente pendiente. Sin secrets, producción, ramas/PR, servicios de pago ni eventos.

## 2026-11-14 — V5 C14-01.M01.05 criterios observables de aceptación funcional [Javier :10]
- Especifica 16 pares P/N de prueba FUTUROS_NO_EJECUTADOS para superficies públicas/privadas, sesión, RBAC, tenant A/B, ventas, inventario, contabilidad, accesibilidad y sectores.
- Vincula NR01–NR14 y documentos M01.01–04, evidencia requerida por SHA exacto y gates de no-regresión; no atribuye PASS HTTP, Angular, Playwright, MySQL o pagos inexistentes.
- Solo Markdown y CHANGELOG en dev; Scope Lock y V5 provenance por exact SHA, A5 independiente pendiente; sin implementación, QA/main/PROD, eventos ni secretos.

## 2026-11-14 — V5 C14-02.M01.05 criterios observables de aceptacion de seguridad [Javier :00]
- Define dieciseis pares P/N FUTUROS_NO_EJECUTADOS y doce requisitos NF NO_MEDIDOS para BOLA/IDOR, JWT/RBAC, tenant A/B, host, archivos, jobs, cache, backups y CI.
- Distingue evidencia documental de pruebas HTTP/MySQL/Playwright y del gate final cero P0/P1 mitigados: ninguno certificado por esta microtarea.
- Solo documento y CHANGELOG en dev FF/CAS, Scope Lock/V5 exact-SHA, A5 independiente; sin runtime, eventos ni QA/main/PROD.

## 2026-11-14 — V5 C14-02.M01.04 riesgos, supuestos y decisiones de aceptacion de seguridad [Javier :00]
- Registra R01-R16 riesgos POTENCIALES, S01-S10 supuestos NO_VERIFICADOS y D01-D10 decisiones ABIERTAS sobre BOLA/IDOR, RBAC/JWT, tenant A/B, archivos, jobs, cache, backups y sesiones.
- Usa el inventario real M01.03 y oraculos futuros NO_EJECUTADOS; no declara hallazgos explotables, cero P0/P1 ni seguridad de produccion certificada.
- Solo documento + CHANGELOG en dev por FF/CAS, Scope Lock/V5 exact-SHA y A5 independiente, sin codigo, eventos, secretos ni QA/main/PROD.

## 2026-11-14 — V5 C14-02.M01.03 inventario causal de seguridad [Javier :00]
- Inventaria doce fuentes y blobs Git autenticos de filtros API, guardia de autorizacion Application, middleware multitenant, persistencia, xUnit y rutas Angular; delimita lo leido de la integracion no verificada.
- Documenta doce brechas de evidencia no demostradas como vulnerabilidades y diez pares P/N FUTUROS_NO_EJECUTADOS de BOLA/IDOR, JWT/RBAC, host, archivos, jobs, cache, backups y tenant A/B.
- Solo documento y CHANGELOG en dev por fast-forward/CAS, Scope Lock/V5 exact-SHA, A5 independiente; no modificaciones runtime ni QA/main/PROD.

## 2026-11-14 — V5 C14-02.M01.02 actores, interfaces y fronteras de confianza de aceptacion de seguridad [Javier :00]
- Mapea doce actores de prueba, catorce interfaces y doce fronteras para BOLA/IDOR, JWT/RBAC, tenant A/B, host, archivos, jobs, cache, backup y sesiones, sin atribuir roles runtime inexistentes.
- Define ocho pares de aceptación positiva/negativa FUTUROS_NO_EJECUTADOS, mantiene M01.01 como alcance y reserva inspección real de codigo para M01.03.
- Solo Markdown + CHANGELOG fast-forward/CAS dev; Scope Lock y V5 exact-SHA, A5 independiente; sin cambios de API, MySQL, permisos, webhooks, QA/main/PROD.

## 2026-11-14 — V5 C14-01.M01.04 riesgos y decisiones de aceptacion funcional [Javier :50]
- Registra R01–R14 riesgos potenciales, S01–S10 supuestos NO_VERIFICADOS y D01–D10 decisiones ABIERTAS de aceptación funcional y seguridad tenant, JWT/RBAC, pago, inventario, UI y CI.
- Distingue ocho P/N FUTUROS_NO_EJECUTADOS de tests reales; no da por cerrado M01.03 de otra Task ni libera M01.06 o QA/PROD.
- Solo docs/v5 y CHANGELOG fast-forward dev, Scope Lock/V5 SHA exacto y A5 independiente; sin servicios runtime o migraciones.

## 2026-11-14 — V5 C14-02.M01.01 alcance y no-regresion de aceptacion de seguridad [Javier :00]
- Fija objetivo y exclusiones de aceptacion de seguridad: BOLA/IDOR, JWT/RBAC, tenant A/B, host, archivos, jobs, cache, backups y sesiones; sin ejecutar pruebas ni declarar cero P0/P1.
- Define NR01-NR18 y diez pares de escenarios SEC-01 a SEC-10 FUTUROS_NO_EJECUTADOS, separando preparacion documental M01.01 de certificacion funcional y release.
- Solo Markdown + CHANGELOG en dev por fast-forward/CAS; Scope Lock y V5 exact-SHA, A5 por otra Task Javier, rollback git revert; sin runtime, migraciones, QA/main/PROD ni webhooks.

## 2026-11-14 — V5 C14-01.M01.03 inventario causal de aceptacion funcional [Javier :30]
- Inventaria diez fuentes Git blob de storefront, aislamiento, browser guards, responsive, accesibilidad, fixtures tenant, rutas, permiso Angular, test API .NET y Scope Lock; distingue test fuente de ejecucion real.
- Declara B01–B14 brechas y ocho pares P/N FUTUROS_NO_EJECUTADOS para flujos publicos, autentificados, pedidos, inventario, tenancy, JWT/RBAC, UI y CI, sin certificacion falsa.
- Solo docs/v5 y CHANGELOG fast-forward CAS dev, Scope Lock/V5 exact SHA y A5 de otra Task Javier. Sin runtime, MySQL, pagos, QA/main/PROD ni eventos.

## 2026-11-14 — V5 C14-01.M01.02 actores y contratos de aceptacion funcional [Javier :50]
- Define doce actores, doce interfaces y ocho fronteras de confianza para flujos publicos, privados, admin, sectoriales y tenant A/B, sin introducir permisos nuevos.
- Describe ocho pares P/N FUTUROS_NO_EJECUTADOS sobre storefront, RBAC/JWT, errores, inventario, accesibilidad y exact-SHA; .spec no implica PASS.
- Solo docs/v5 y CHANGELOG por FF/CAS dev, Scope Lock/V5 y A5 independiente; sin .NET/Angular/MySQL runtime ni QA/main/PROD.

## 2026-11-14 — V5 C14-01.M01.01 alcance y no-regresion de aceptacion funcional [Javier :50]
- Define objetivo y exclusiones de aceptacion, catorce invariantes, diez blobs fuente y ocho pares P/N FUTUROS_NO_EJECUTADOS de storefront, sesion, JWT/RBAC, tenant A/B, inventario, UI y CI.
- Distingue DOC_M01_PREP de pruebas E2E reales, autorizacion de release y gates terminales; no inventa PASS de tests, WCAG, pago, MySQL ni QA.
- Dos Markdown en dev por FF/CAS, Scope Lock y V5 exact SHA, A5 independiente, rollback git revert; sin runtime ni QA/main/PROD.

## 2026-11-14 — V5 C13-04.M01.05 aceptacion observable de experiencia visual unificada [Javier :50]
- Define 16 pares P/N y 10 NF FUTUROS_NO_EJECUTADOS para foco, feedback, responsive, rutas, branding, WCAG no certificado, JWT/RBAC y aislamiento tenant A/B.
- Correlaciona M01.01/M01.02/M01.03 y fuentes Angular reales; M01.04 de otra autora no se certifica por inferencia, M01.06 permanece bloqueada por gates propios.
- Solo docs/v5 y CHANGELOG fast-forward dev, Scope Lock/V5 exact-SHA y A5 independiente; no frontend, backend, eventos, migraciones ni QA/main/PROD.

## 2026-11-14 — V5 C13-04.M01.04 riesgos, supuestos y decisiones de experiencia unificada [Javier :30]
- Documenta R01–R14 riesgos potenciales, S01–S10 supuestos NO_VERIFICADOS y D01–D10 decisiones ABIERTAS de UX, foco, estados, reflow, branding tenant A/B y permisos backend.
- Concilia M01.01/M01.03 y siete fuentes Git blob; ocho pares P/N FUTUROS_NO_EJECUTADOS, sin fingir pruebas WCAG, browser, rendimiento o JWT reales.
- Solo docs/v5 y CHANGELOG por fast-forward CAS dev; Scope Lock/V5 exact SHA y A5 independiente. Sin runtime, MySQL, QA/main/PROD ni eventos.

## 2026-11-14 — V5 C13-04.M01.02 actores, entradas, salidas y limites de UI unificada [Javier :50]
- Documenta doce perfiles de uso, doce contratos de interfaz y ocho fronteras de confianza, referenciando alcance M01.01 e inventario causal M01.03 de Angular, guards, estilos y Playwright.
- Define ocho pares positivos/negativos FUTUROS_NO_EJECUTADOS sobre foco, responsive, estados, permisos backend y aislamiento tenant A/B; no declara WCAG ni tests runtime ejecutados.
- Solo docs/v5 y CHANGELOG fast-forward dev con Scope Lock/V5 exact-SHA, A5 independiente; sin eventos, migraciones, QA/main/PROD.

## 2026-11-14 — V5 C13-04.M01.03 inventario causal de UI, estados y responsive [Javier :50]
- Registra 14 fuentes Git blob de rutas Angular, styles, tokens, navegacion, feedback, guards tenant, budgets y Playwright con limites comprobables; no confunde archivos de test con ejecuciones.
- Identifica 12 brechas y ocho pares P/N FUTUROS_NO_EJECUTADOS de coherencia visual, focus, responsive, seguridad A/B y rendimiento; C13-04.M01.01 ajena no se declara completada.
- Solo docs/v5 y CHANGELOG en dev, Scope Lock/V5 exact-SHA y A5 independiente; sin runtime, migraciones, eventos ni QA/main/PROD.

## 2026-11-14 — V5 C13-04.M01.01 alcance y no-regresion de experiencia unificada [Javier :40]
- Define objetivo y no-alcance con NR01–NR14 de jerarquia, formularios, tablas, estados, foco, responsive, branding por tenant, JWT/RBAC y UI ligera accesible.
- Coteja blobs reales styles.scss, app.routes, Angular, design system, accesibilidad y rendimiento; matriz de seis superficies y ocho pares P/N FUTUROS_NO_EJECUTADOS, sin fingir pruebas UI/WCAG.
- Solo documento y CHANGELOG por fast-forward CAS dev, Scope Lock/V5 exact SHA, A5 independiente; sin cambios Angular/API/MySQL, eventos, QA/main/PROD.

## 2026-11-14 — V5 C13-03.M01.05 aceptacion de rendimiento y ligereza [Javier :30]
- Define dieciseis P/N futuros y doce criterios NF de bundle, rutas lazy, cache, imagenes, tablas, LCP/INP/CLS, accesibilidad y tenant A/B; NO_EJECUTADOS.
- Mantiene budget 650/750kB como configuracion y discrepancia del script 1MiB/2MiB como pendiente de codigo separado; no inventa medidas ni cambia CI o seguridad.
- Solo documento y CHANGELOG, Scope Lock/V5 exact-SHA y A5 por otra Task; sin QA/main/PROD.

## 2026-11-14 — V5 C13-03.M01.04 riesgos y decisiones de rendimiento [Javier :40]
- Registra R01–R14 riesgos potenciales, S01–S10 supuestos NO_VERIFICADOS y D01–D10 decisiones ABIERTAS sobre budgets, lazy/preload, requests, cache tenant, medios, PII y observabilidad.
- Coteja M01.01/M01.02 y fuentes Angular autenticas; M01.03 estaba EN_PROGRESO ajena y no se declara terminada. Ocho pares P/N FUTUROS_NO_EJECUTADOS; no se fingen LCP, INP, CLS, pruebas ni SLAs.
- Solo documento y CHANGELOG dev FF/CAS; Scope Lock/V5 exact SHA y A5 independiente, sin runtime, workers, CDN, QA/main/PROD.

## 2026-11-14 — V5 C13-03.M01.03 inventario y brechas de rendimiento [Javier :30]
- Inventaria catorce blobs de Angular, scripts de budget, preloading, browser, Cloudinary, HTTP cache y contratos; no confunde fuente con test, metrica o build real.
- Detecta discrepancia documental verificable: budget vigente initial 650/750kB vs 1MiB/2MiB en performance-bundle-baseline.mjs; requiere reconciliacion causal separada, no declara regresion runtime.
- Documenta B01–B14 y P01–P08/N01–N08 FUTUROS_NO_EJECUTADOS; solo Markdown y A5 independiente; sin QA/main/PROD.

## 2026-11-14 — V5 C13-03.M01.02 actores y superficies de rendimiento [Javier :40]
- Delimita A01–A12 actores, I01–I12 entradas, O01–O10 salidas y F01–F08 fronteras de Angular, API, MySQL, caché tenant, medios y observabilidad.
- Contrasta M01.01, angular.json, app.routes, package y Playwright con ocho pares P/N FUTUROS_NO_EJECUTADOS; budgets 650/750kB son configuracion, no benchmark o PASS medido.
- Solo documento y CHANGELOG fast-forward CAS dev, Scope Lock/V5 SHA exacto, A5 independiente; sin modificar runtime, CDN, workers, migraciones, QA/main/PROD.

## 2026-11-14 — V5 C13-02.M01.05 criterios de aceptacion accesible [Javier :30]
- Define P01–P14/N01–N14 y NF01–NF10 verificables de teclado, foco, formularios, overlays, contraste, reflow, lector, idioma y tenant A/B; TODOS FUTUROS_NO_EJECUTADOS.
- Preserva NR01–NR14 y brechas B01–B14; ningun WCAG certificado, test runtime ni cumplimiento general se declara por escribir Markdown.
- Solo docs/v5 + CHANGELOG, Scope Lock/V5 exact-SHA y A5 independiente; sin Angular/API/MySQL/JWT/RBAC/QA/main/PROD.

## 2026-11-14 — V5 C13-02.M01.04 riesgos y decisiones de accesibilidad [Javier :40]
- Registra R01–R14 riesgos potenciales, S01–S10 supuestos NO_VERIFICADOS y D01–D10 decisiones ABIERTAS para teclado, foco, formularios, contraste, reflow, lector, permisos y tenant A/B.
- Concilia contratos M01.01–03, blobs de inventario y ocho pares P/N FUTUROS_NO_EJECUTADOS; no confunde archivos de tests con ejecuciones o conformidad WCAG.
- Solo documento y CHANGELOG en dev, Scope Lock/V5 exact SHA, A5 de otra Task Javier; sin Angular, API, MySQL, eventos, QA/main/PROD.

## 2026-11-14 — V5 C13-02.M01.03 inventario codigo de accesibilidad [Javier :20]
- Inventaria doce fuentes Git exactas de Angular, guards, Playwright, Vitest, SCSS y contratos; no confunde archivo de test con ejecucion o cumplimiento WCAG.
- Registra catorce brechas de evidencia B01–B14 y ocho pares P/N FUTUROS_NO_EJECUTADOS de teclado, foco, formularios, contraste, reflow, semantica y tenant A/B.
- Solo dos Markdown en dev, Scope Lock/V5 exact SHA y A5 independiente; sin nuevos componentes, backend, MySQL, eventos ni QA/main/PROD.

## 2026-11-14 — V5 C13-03.M01.01 alcance y no-regresion de rendimiento [Javier :30]
- Define rendimiento y ligereza con 14 invariantes, 4 positivos y 12 negativos FUTUROS_NO_EJECUTADOS; exige baseline SHA/device/red/cache antes de cualquier metrica PASS.
- Contrasta blobs Git de angular.json, package, rutas lazy, politica bundle, Playwright y C05-04/C12-02; mantiene budget 650/750kB como configuracion, no resultado medido.
- Solo documento y CHANGELOG fast-forward CAS dev; no cambia frontend/backend, MySQL, auth, CDN, CI, QA/main/PROD; Scope Lock/V5 exact SHA y A5 independiente posterior.

## 2026-11-14 — V5 C13-02.M01.02 actores y superficies de accesibilidad [Javier :20]
- Delimita catorce actores, doce entradas, diez salidas y ocho fronteras de UX accesible, foco, teclado, ARIA, contraste, reflow y multitenancy.
- Contrasta siete blobs Git y M01.01 sin confundir test source con prueba ejecutada; ocho pares P/N FUTUROS_NO_EJECUTADOS y WCAG no certificado.
- Unicamente Markdown en dev por fast-forward/CAS, Scope Lock y V5 exact SHA, A5 independiente, rollback git revert; sin Angular, API, MySQL, QA/main/PROD.

## 2026-11-14 — V5 C13-02.M01.01 alcance y no-regresion de accesibilidad [Javier :30]
- Define objetivo/no-alcance, NR01–NR14, P01–P04 y N01–N12 FUTUROS_NO_EJECUTADOS para teclado, foco, formularios, contraste, lectores, reflow y reduced-motion.
- Contrasta blobs reales de Angular, rutas, Playwright y contratos de design system/frontend; sin afirmar cumplimiento WCAG ni ejecutar browser, .NET, MySQL o API.
- Solo nuevo Markdown y CHANGELOG en dev por fast-forward/CAS; Scope Lock y V5 exact-SHA, PLAN/COLA/BITACORA readback, A5 de otra Task Javier y rollback git revert; sin QA/main/PROD.

## 2026-11-14 — V5 C13-01.M01.05 criterios de aceptacion de privacidad [Javier :20]
- Define AC01–AC16 positivos, N01–N16 negativos y NF01–NF10 de privacidad/versionado/revocacion, tenant A/B, JWT/RBAC y datos propios, todos FUTUROS_NO_EJECUTADOS.
- Concilia M01.01–04, brechas B01–B12 y decisiones D01–D10 sin afirmar legal compliance, consentimiento activo, cookies o tests runtime.
- Solo Markdown y CHANGELOG en dev, Scope Lock/V5 SHA exacto, A5 independiente y rollback git revert; sin endpoints, MySQL, eventos, QA/main/PROD.

## 2026-11-14 — V5 C13-01.M01.04 riesgos y decisiones de privacidad [Javier :20]
- Registra R01–R14 riesgos, S01–S10 supuestos NO_VERIFICADOS y D01–D10 decisiones ABIERTAS sobre cookies, retencion, versionado, revocacion, PII y tenant A/B.
- Contrasta M01.01–03 y fuentes Git autenticas; ocho pares P/N FUTUROS_NO_EJECUTADOS, sin afirmar consentimiento activo, cumplimiento juridico ni ejecucion runtime.
- Entrega solo dos Markdown en dev por fast-forward/CAS, Scope Lock/V5 exact SHA, A5 de otra Task Javier, rollback git revert; sin API, MySQL, eventos, QA/main/PROD.

## 2026-11-14 — V5 C13-01.M01.03 inventario codigo privacidad y consentimiento [Javier :10]
- Inventaria once archivos reales con blobs exactos, auth localStorage, tenant server-side, proxy, E2E y auditoria; diferencia lectura estatica de prueba real de cookies/consentimiento.
- Documenta doce brechas de evidencia, cuatro positivos y dieciseis negativos FUTUROS_NO_EJECUTADOS con controles JWT/RBAC/tenant A/B y sin declaracion de cumplimiento legal.
- Exclusivamente dos Markdown en dev, Scope Lock/V5 exact SHA y A5 independiente; sin endpoints, cookies, migraciones, eventos, QA/main/PROD ni secretos.

## 2026-11-14 — V5 C13-01.M01.02 actores, entradas y salidas de privacidad [Javier :10]
- Delimita quince actores/superficies, ocho entradas, ocho salidas esperadas y siete fronteras de confianza para consentimiento/privacidad sin inventar autoridad legal ni runtime.
- Inspecciona cuatro blobs Git preexistentes y referencia C13-01.M01.01; cuatro positivos/dieciseis negativos FUTUROS_NO_EJECUTADOS, JWT/RBAC/tenant A/B fail-closed.
- Solo dos archivos Markdown en dev, Scope Lock/V5 exact SHA y A5 independiente necesarios; sin cookies nuevas, proveedor, API, MySQL, eventos, QA/main/PROD.

## 2026-11-14 — V5 C01-02.M02.06 contrato de dependencias y salida capabilities [Javier :10]
- Concilia seis antecedentes M01.06/M02.01–05 con blobs Git y A5 independiente; define gates causales de arquitectura, seguridad JWT/RBAC/tenant A/B y DAG antes de M03.
- Cuatro escenarios positivos y dieciseis negativos FUTUROS_NO_EJECUTADOS; no crea permisos, endpoints, tablas MySQL, workers, eventos ni nuevas Tasks.
- Solo Markdown en dev; Scope Lock/V5 exact SHA y A5 de otra Task Javier requeridos, rollback git revert; sin QA/main/PROD.

## 2026-11-14 — V5 C13-01.M01.01 privacidad y consentimiento, preparacion [Javier :00]
- Define alcance de versionado de políticas, consentimiento donde corresponda, revocación y retención sin afirmar obligaciones legales no verificadas o existencia de banner/runtime.
- Traza cuatro archivos Git y 16 invariantes con cuatro positivos y doce negativos FUTUROS_NO_EJECUTADOS, preservando JWT/RBAC/tenancy y PII.
- Solo Markdown en dev, Scope Lock/V5 exact SHA, A5 independiente Javier, rollback git revert; sin instalar cookies, worker, evento, DB, QA/main/PROD.

## 2026-11-14 — V5 C04-01.M01.06 bloqueo y salida de catalogo de planes [Javier :00]
- Concilia cinco documentos M01.01–05 con blobs exactos y establece gates para catalogo/plan/version/limites, RBAC multiempresa, idempotencia y rollback comercial.
- Catorce negativos y cuatro positivos FUTUROS_NO_EJECUTADOS; no crea precios, facturacion, cobros, proveedor, migraciones o autorizaciones ejecutables.
- Publicacion solo Markdown en dev con Scope Lock/V5 exact SHA, A5 independiente Javier; sin workers, webhooks, QA/main/PROD o secretos.

## 2026-11-14 — V5 C03-02.M01.06 puertas de RBAC/ABAC y privacidad [Javier :00]
- Concilia cinco artefactos previos M01.01–05 con blobs Git exactos; fija bloqueos de identidad, membresia, recurso/sucursal, exportacion, URLs de media y soporte restringido.
- Documenta cuatro positivos y doce negativos FUTUROS_NO_EJECUTADOS, reglas de pruebas HTTP/EF/MySQL posteriores, no confunde test source con PASS runtime.
- Solo Markdown y CHANGELOG dev, Scope Lock/V5 exact SHA, A5 independiente Javier y rollback git revert; sin cambios JWT/RBAC/API/DB, eventos, workers o QA/main/PROD.

## 2026-11-14 — V5 C01-03.M02.06 salida contractual de configuración dinámica [Javier :00]
- Concilia los seis contratos M01.06/M02.01-05 con blobs Git exactos y delimita gates por fase, capas, tenant A/B, JWT/RBAC y versión; sin asumir implementación.
- Documenta cuatro casos positivos y doce negativos FUTUROS_NO_EJECUTADOS, idempotencia/correlación teóricas y NO activación de eventos, workers, API, MySQL ni despliegues.
- Entrega sólo Markdown en dev, Scope Lock/V5 exact-SHA, A5 de otra Task Javier y rollback git revert normal; sin QA/main/PROD.

## 2026-11-14 — V5 C01-02.M02.05 eventos e idempotencia conceptual de capabilities [Javier :50]
- Especifica envelope versionado, correlacion opaca e idempotencia tenant+actor con commit transaccional FUTURO; sin despacho de eventos, webhooks ni Tasks.
- Contrasta nueve fuentes Git y documenta 4 positivos/16 negativos FUTUROS_NO_EJECUTADOS sobre JWT/RBAC, revocacion, plan y aislamiento A/B.
- Solo documento y CHANGELOG en dev; Scope Lock/V5 exact SHA y A5 independiente requeridos; sin runtime, MySQL/QA/main/PROD ni compras.

## 2026-11-14 — V5 C01-02.M02.04 configuracion tipada y flags de capabilities [Javier :50]
- Contrasta diez fuentes Git (FeatureFlagsSaaS, ConfigEmpresa/EF, DTO y contratos M02); define límites plan/rubro/tenant/sucursal/recurso sin duplicar RBAC.
- Diseña tipado, defaults DENY, versionado/revocación y 4 positivos/14 negativos FUTUROS_NO_EJECUTADOS; no crea permisos, endpoints, flags, tablas o servicios.
- Solo Markdown y CHANGELOG en dev, Scope Lock/V5 exact-SHA exigidos y A5 de otra Task Javier; sin webhooks, MySQL real, QA/main/PROD.

## 2026-11-14 — V5 C01-02.M02.03 contrato persistencia y fixtures de capabilities [Javier :50]
- Contrasta ocho fuentes de código/documentación con blobs exactos; distingue índices EF existentes de modelo futuro de capabilities sin SQL/DDL aplicado.
- Diseña claves y relaciones tenant-scoped, vigencia/concurrencia, 4 positivos y 12 negativos FUTUROS_NO_EJECUTADOS con fail-closed JWT/RBAC/tenant A/B.
- Modifica solo contrato Markdown M02.03 y CHANGELOG en dev; Scope Lock/V5 exact-SHA requerido, A5 de otra Task; no MySQL real, API/DTO, workers/webhooks, QA/main/PROD.

## 2026-11-14 — V5 C01-02.M02.02 contratos API/DTO y compatibilidad capabilities [Javier :40]
- Diseña contratos conceptuales de evaluación de capacidades, catálogo y plan versionado, validaciones de ingreso y rutas FUTURAS no registradas; mantiene endpoints reales de permisos/matriz sin modificación.
- Documenta JWT/RBAC y tenant A/B fail-closed, códigos 401/403/404/409/422/503, tres oráculos positivos y diez negativos documentales NO_EJECUTADOS; sin autoridad por DTO, UI o plan.
- Únicamente Markdown M02.02 y CHANGELOG en dev; no hay DTO/endpoint MySQL/EF, workers/webhooks/eventos ni QA/main/PROD. Scope Lock/V5 exact-SHA, A5 pendiente y rollback git revert normal.

## 2026-11-14 — V5 C01-02.M02.01 modelo conceptual de capabilities y ownership [Javier :40]
- Define entidades conceptuales DefinicionCapacidad, ReglaCapacidadPlan, AsignacionPlanEmpresa y RestriccionCapacidadRecurso; intersecta membresía verificada, grants RBAC, filtro comercial y propiedad de recurso sin conceder permisos por plan ni rol legacy.
- Diez invariantes de denegación tenant A/B, rol, ownership, caducidad y auditoría; tres escenarios positivos y ocho negativos documentales NO_EJECUTADOS, con fuentes Git exactas.
- Solo documentación M02.01 y CHANGELOG en dev; sin entidades EF, migraciones MySQL, endpoint, JWT real, workers, webhooks, cobros, QA/main/PROD. A0–A4 documentales y A5 independiente posterior; rollback git revert ordinario.

## 2026-11-14 — V5 C12-04.M01.05: criterios de aceptación MySQL/cutover [Javier :30]
- Define AC01–AC16 positivos y N01–N16 negativos, más NF01–NF10 de tenant A/B, JWT/RBAC, hash, FK, DDL único, idempotencia, readiness, freeze, rollback y redacción de secretos; todos FUTUROS_NO_EJECUTADOS.
- Traza NR01–NR16, G01–G14, R01–R14 y D01–D10 a gates de M02–M06, sin afirmar RPO/RTO, volúmenes, checksums de datos ni restore reales.
- Solo dos Markdown en dev por CAS fast-forward, Scope Lock/V5 SHA exacto y A5 de otra Task; no MySQL, Hostinger, DNS, QA/main/PROD, secretos ni compras.

## 2026-11-14 — V5 C12-04.M01.04: riesgos y decisiones MySQL/cutover [Javier :30]
- Documenta R01–R14 riesgos potenciales de snapshot, tenant A/B, JWT/RBAC, DDL único, FK, escrituras financieras, red, DNS y rollback sin declarar incidentes ni exposición real.
- Conserva S01–S10 como NO_VERIFICADOS y D01–D10 como decisiones ABIERTAS con responsables; ocho pares P/N de pruebas futuras NO_EJECUTADAS y G01–G14 como brechas.
- Solo dos Markdown en dev por CAS fast-forward, Scope Lock/V5 exact-SHA y A5 de otra Task; sin MySQL real, dumps, migraciones, Hostinger, Cloudflare, QA/main/PROD ni compras.

## 2026-11-14 — V5 C12-04.M01.03: inventario Git MySQL privado y cutover [Javier :30]
- Inventaria 16 blobs Git del HEAD 213d08d6 (DbContext, API, EF/MySQL migrations, arquitectura, frontend y contratos C10/C12), diferenciando código versionado de datos/servicios live NO_VERIFICADOS.
- Registra G01–G14 brechas de exportación, restore, conteos, checksum, FK, tenancy A/B, DDL único, seguridad, infraestructura y rollback; ocho pares P/N FUTUROS_NO_EJECUTADOS.
- Cambia solo dos Markdown en dev por CAS fast-forward, Scope Lock/V5 exact-SHA y A5 de Task Javier distinta; sin MySQL real, DDL, Hostinger, Cloudflare, DNS, QA/main/PROD o compras.

## 2026-11-14 — V5 C12-04.M01.02: actores y fronteras MySQL privado/cutover [Javier :30]
- Identifica A01–A14 actores, I/O01–I/O12 entradas/salidas y F01–F10 fronteras de confianza de export, restore efímero, comparación, DDL único, cutover y reversión; preserva JWT/RBAC/tenant A/B.
- Define P01/N01–P10/N10 FUTUROS_NO_EJECUTADOS, con controles de snapshot, checksum, FK, pertenencia, logs sin secretos, writes transaccionales y rollback, sin afirmar MySQL/infra real verificada.
- Solo este Markdown y CHANGELOG en dev con CAS fast-forward, Scope Lock y procedencia V5 exact-SHA y A5 independiente pendiente; sin dump, migración, proveedores, DNS, QA/main/PROD o compras.

## 2026-11-14 — V5 C12-04.M01.01: alcance de restore y reversión [Javier :20]
- Define límites de exportación, restore de ensayo, comparación, ventana de cambio y rollback futuros; M01 es únicamente documental, sin conexión a datos productivos.
- Establece NR01–NR16 de no-regresión y P01–P10/N01–N10 FUTUROS_NO_EJECUTADOS para tenant A/B, JWT/RBAC, checksums, conteos, schema, transacciones y cutover.
- Solo docs/v5 y CHANGELOG en dev con Scope Lock/V5 exact SHA y A5 independiente pendiente; sin MySQL real, DNS, proveedores, compras, QA/main/PROD.

## 2026-11-14 — V5 C12-03.M01.05: criterios observables backend/worker [Javier :20]
- Define AC01–AC16 y N01–N16 para API, tenant A/B, JWT/RBAC, idempotencia, retry, health, DDL único, secret ausente, seguridad y rollback; todos FUTUROS_NO_EJECUTADOS.
- Registra NF01–NF10 mediciones futuras sin SLO inventado, rutas de evidencia M02–M06 y bloqueo estricto de runtime hasta CI causal exact-SHA.
- Solo docs/v5 y CHANGELOG en dev; A0–A4 documental Scope Lock/V5, A5 independiente pendiente, sin worker, eventos, MySQL, QA/main/PROD ni compras.

## 2026-11-14 — V5 C12-03.M01.04: riesgos y decisiones backend/worker [Javier :20]
- Documenta R01–R14 riesgos y controles causales de tenant A/B, RBAC diferido, idempotencia, DDL, reinicio, readiness, secrets y logs, sin afirmar trabajadores desplegados.
- Deja S01–S10 hipótesis NO_VERIFICADAS y D01–D10 decisiones abiertas con dueño y evidencia, enlazadas al inventario G01–G12 y NR01–NR16.
- Solo dos Markdown en dev, A0–A4 documental, Scope Lock/V5 exact-SHA y A5 independiente; sin workers, eventos, MySQL, QA/main/PROD ni compras.

## 2026-11-14 — V5 C12-03.M01.03: inventario Git backend y workers [Javier :10]
- Registra quince fuentes versionadas con SHA de blob comprobable de API, Application, Domain, Infrastructure, DbContext, migraciones, governance y documentos M01, sin afirmar workers desplegados.
- Expone G01-G12 brechas verificables de cola, tenant fuera de HttpContext, RBAC diferido, idempotencia, migración única, restart y secretos; ocho pares P/N FUTUROS_NO_EJECUTADOS.
- Solo dos Markdown en dev, CAS fast-forward, Scope Lock/V5 exact-SHA y A5 independiente pendiente; sin workers, MySQL, QA/main/PROD, DNS ni compras.

## 2026-11-14 — V5 C12-03.M01.02: actores y fronteras backend/worker [Javier :10]
- Describe A01-A14 actores, F01-F12 entradas/salidas y fronteras de confianza API, Application, Domain, DbContext/VerifiedTenant, MySQL y worker futuro, sin introducir runtime.
- Ocho pares P/N de aceptación FUTURA_NO_EJECUTADA; la ausencia de HttpContext en worker no autoriza tenant global ni bypass JWT/RBAC; secretos y transacciones fail-closed.
- Solo docs/v5 y CHANGELOG en dev por CAS fast-forward, Scope Lock/V5 SHA exacto y A5 independiente pendiente; sin eventos, DNS, QA/main/PROD, migraciones ni compras.

## 2026-11-14 — V5 C12-02.M01.05: criterios verificables frontend separado [Javier :00]
- Define AC01-AC16 positivos, N01-N16 negativos y NF01-NF10 para Angular, SPA, proxy, JWT/RBAC/tenant, headers/cache, SEO, accesibilidad y rollback; TODOS FUTUROS_NO_EJECUTADOS.
- Usa blobs Git del inventario C12-02.M01.03 y riesgos C12-02.M01.04; no inventa pruebas live ni umbrales técnicos/productivos.
- Solo dos Markdown (docs/v5 + CHANGELOG) en dev, CAS fast-forward y Scope Lock/V5 exact-SHA, A5 independiente pendiente; sin CDN, DNS, workers, QA/main/PROD o pagos.

## 2026-11-14 — V5 C12-03.M01.01: alcance backend y workers separados [Javier :10]
- Fija límites documentales para API .NET 10, EF/MySQL, JWT/RBAC/tenant A/B, workers futuros, migraciones exclusivas, reintentos e idempotencia sin crear servicio ni dispatcher.
- Establece NR01-NR16 y diez pares P/N de pruebas FUTURAS_NO_EJECUTADAS de arranque, rollback, secretos, aislamiento, reinicio y redelivery.
- Solo docs/v5 y CHANGELOG en dev, CAS fast-forward, Scope Lock/V5 exact-SHA y A5 independiente; sin QA/main/PROD ni proveedores, DNS o compras.

## 2026-11-14 — V5 C12-02.M01.04: riesgos y decisiones frontend separado [Javier :00]
- Documenta R01-R16 riesgos POTENCIALES, S01-S10 supuestos NO_VERIFICADOS y D01-D10 decisiones abiertas sobre Angular, proxy API, CSP/CORS, cache/CDN, branch binding y rollback.
- Ancla fuentes Git y el inventario C12-02.M01.03, incluye ocho pares de escenarios futuros NO_EJECUTADOS; no certifica smoke, DNS/TLS, CDN ni JWT/tenant live.
- Solo docs/v5 + CHANGELOG en dev por CAS fast-forward, Scope Lock/V5 exact SHA; A5 independiente pendiente; sin workers, eventos, QA/main/PROD o proveedores pagos.

## 2026-11-14 — V5 C12-02.M01.03: inventario frontend separado [Javier :00]
- Inventaria diez fuentes versionadas con SHA de blob verificable de Angular 22.2.1, Vercel, Render, guardas, proxy, headers, caché y arquitectura, sin confundir contrato con proveedor vivo.
- Registra doce brechas G01-G12 y seis pares de pruebas futuras P/N NO_EJECUTADAS, preservando JWT/RBAC/tenant A/B y segregación de entornos.
- Únicamente dos Markdown (docs/v5 y CHANGELOG) en dev; Scope Lock/V5 exact-SHA y A5 independiente requeridos; sin CDN, DNS, workers, eventos, QA/main/PROD o gastos.

## 2026-11-14 — V5 C12-02.M01.02: actores y fronteras frontend separado [Javier :50]
- Documenta A01-A12 actores, I01/O01-I12/O12 entradas/salidas y F01-F12 fronteras de API proxy, SPA, SEO, headers, CDN, cache y aislamiento tenant.
- Traza diez rutas Git/blobs de Angular, Vercel, guards, config y Render; ocho pares P/N FUTUROS_NO_EJECUTADOS, no se certifican CDN, smoke, DNS o migracion.
- Solo docs/v5 y CHANGELOG en dev, CAS fast-forward y Scope Lock/V5 exact-SHA; A5 pendiente de Task Javier independiente, sin compras, QA/main/PROD ni eventos.

## 2026-11-14 — V5 C12-02.M01.01: alcance de frontend separado [Javier :50]
- Define NR01-NR16 invariantes, P01-P16 positivos y N01-N16 negativos FUTUROS_NO_EJECUTADOS de Angular 22.2.1, Zone.js, routing/guards, Vercel, proxy, headers, cache, build y rollback.
- Contrasta diez fuentes Git por blob, incluido script ignore-build por rama y contratos de Render; mantiene Vercel/Render/Aiven/Cloudinary sin activar CDN nuevo, DNS, host ni secretos.
- Dos Markdown en dev por CAS fast-forward y checks Scope Lock/V5 del SHA; A5 independiente pendiente, sin builds/smoke de proveedor declarados PASS, QA/main/PROD ni gastos.

## 2026-11-14 — V5 C12-01.M01.05: criterios observables de topologia futura [Javier :50]
- Especifica P01-P16 positivos, N01-N16 negativos y NF01-NF10 no funcionales para API, DB privada, proxy, JWT/RBAC/tenant, contenedores, DNS/TLS, restore y rollback.
- Todos son FUTUROS_NO_EJECUTADOS, sin umbrales SLO/RTO ficticios; conserva infraestructura Vercel/Render/Aiven/Cloudinary y no activa Hostinger, Cloudflare, migraciones o webhooks.
- Solo docs/v5 y CHANGELOG en dev con CAS fast-forward y Scope Lock/V5 exact-SHA; A5 independiente pendiente, sin QA/main/PROD, secretos, compras o nuevos servicios.

## 2026-11-14 — V5 C12-01.M01.04: riesgos y decisiones de topologia futura [Javier :50]
- Registra R01-R16 riesgos, S01-S10 supuestos falsables y D01-D10 decisiones pendientes para topologia objetivo, con evidencia Git de Render/Aiven, proxy Vercel, Docker, tenant y rollback.
- Mantiene todos los riesgos abiertos; no acredita red privada, proveedor, backups, IAM, DNS, workers o restauracion como hechos ejecutados ni activa servicios/pagos.
- Solo docs/v5 y CHANGELOG en dev, publicacion CAS fast-forward y Scope Lock/V5 exact-SHA; A5 pendiente de otra Task Javier; sin QA/main/PROD, secretos o webhooks.

## 2026-11-14 — V5 C12-01.M01.03: inventario causal de topologia objetivo [Javier :50]
- Audita 14 rutas Git por blob SHA e identifica 12 brechas y evidencia futura de red privada, MySQL, DNS/TLS, seguridad, workers, rutas y rollback; distingue configuraciones versionadas de servicios reales.
- Documenta guards de Render/Aiven, Dockerfile, Vercel y stack Angular/.NET sin modificar codigo, proveedor, cuentas, API ni infraestructura; criterios runtime futuros NO_EJECUTADOS, A5 independiente pendiente.
- Unicamente docs/v5 y CHANGELOG en dev, publicacion fast-forward CAS con Scope Lock/V5 exact-SHA requerido; sin migraciones, eventos, webhooks, QA/main/PROD, secretos o compras.

## 2026-11-14 — V5 C12-01.M01.02: actores y fronteras de topologia objetivo [Javier :40]
- Documenta A01–A14 actores, I/O01–I/O12 y F01–F14 fronteras para futura segregación Angular/.NET/MySQL, red privada, secretos, backups y reversión, sin crear infraestructura real.
- Traza nueve fuentes Git y ocho pares de aceptación/rechazo FUTUROS_NO_EJECUTADOS; mantiene Vercel, Render, Aiven y Cloudinary actuales sin declarar Hostinger/Cloudflare activos ni inventar costos.
- Sólo docs/v5 y CHANGELOG en dev, con CAS fast-forward, Scope Lock/V5 exact-SHA, A5 independiente pendiente; sin worker, eventos, DNS, cuentas, QA/main/PROD o pago.

## 2026-11-14 — V5 C00-04.M03.05: correccion compilacion namespace tenant [Javier :40]
- GitHub Actions detecto CS0246 en Infrastructure/GobiernoCambiosIntegracionLocal.cs (ContextoTenantActual no resuelto): se agrega using Solqaryn.Domain.Security, sin cambios de comportamiento ni ampliacion de permisos.
- Se exige nueva compilacion y pruebas .NET 10, seguridad tenant, Scope Lock y procedencia V5 sobre SHA nuevo; el SHA anterior con CI FAIL no se cuenta como PASS ni autoriza LISTO.
- Unicamente dev, sin eventos, worker, DI, endpoint, MySQL, QA/main/PROD ni secrets.

## 2026-11-14 — V5 C00-04.M03.05: integracion local inerte de gobierno [Javier :40]
- Implementa .NET 10 Infrastructure→Application entre guard tenant/RBAC vigente y copia efímera institucional sin autoridad; el diagnóstico local nunca concede ejecución, aprobación, permisos ni escritura.
- Agrega siete pruebas xUnit positivas/negativas de tenant B, revocación, SHA distinto/ausente, propuesta NO_GO, ID manipulado y null; no agrega API, DI, MySQL, worker, eventos, webhook ni proveedor.
- Desarrollo exclusivo dev con CAS, CI backend/seguridad/Scope Lock/V5 exact SHA y A5 independiente; sin QA/main/PROD ni claims de integración distribuida.

## 2026-11-14 — V5 C12-01.M01.01: alcance topologia objetivo sin activacion [Javier :30]
- Fija alcance documental de frontend, API, workers, MySQL y red privada objetivo Hostinger/Cloudflare, conservando Render/Vercel/Aiven/Cloudinary actuales sin migracion ni compras.
- Ancla nueve blobs Git, NR01–NR16, P01–P08/N01–N08 FUTUROS_NO_EJECUTADOS y diez gates futuros sin afirmar VPS, DNS, backup o despliegue certificado.
- Solo docs/v5 y CHANGELOG por CAS fast-forward dev; Scope Lock/V5 exact SHA y A5 independiente pendiente; sin infra, secretos, migraciones, QA/main/PROD.

## 2026-11-14 — V5 C11-04.M01.05: criterios de aceptación de observabilidad [Javier :40]
- Define AC01–AC18, N01–N18 y RNF01–RNF12 como oráculos FUTUROS_NO_EJECUTADOS para correlación, métricas, SLI/SLO, alertas y costos; ningún umbral o runtime PASS inventado.
- Preserva privacidad, JWT/RBAC, tenant A/B, soporte de lectura, cardinalidad y aislamiento de fallos; sin crear exporters, Tasks, eventos ni proveedores externos.
- Sólo documentación docs/v5 + CHANGELOG en dev; Scope Lock y procedencia V5 exact-SHA, readbacks y A5 ajeno pendientes; sin QA/main/PROD.

## 2026-11-14 — V5 C11-04.M01.04: riesgos y decisiones de observabilidad [Javier :30]
- Documenta doce riesgos R01–R12 POTENCIALES, ocho supuestos S01–S08 NO_VERIFICADOS y ocho decisiones D01–D08 PENDIENTES sobre privacidad, costos, cardinalidad, roles y SLO.
- Ancla ocho blobs Git, relaciona M01.01–03 e incluye ocho pares positivos/negativos FUTUROS_NO_EJECUTADOS; no diagnostica vulnerabilidades confirmadas ni inventa pruebas runtime.
- Sólo docs/v5 + CHANGELOG por fast-forward CAS en dev, checks Scope Lock/V5 exact SHA y A5 independiente pendiente; sin exporters, servicios de pago, secrets, QA/main/PROD.

## 2026-11-14 — V5 C11-04.M01.03: inventario de evidencia de observabilidad [Javier :30]
- Ancla once fuentes con blobs exactos de API, middleware, EF, métricas, xUnit y documentos M01; separa código existente, pruebas definidas y funciones NO_CERTIFICADAS.
- Registra doce brechas G01–G12 no confirmadas como bugs, y ocho escenarios positivos/negativos FUTUROS_NO_EJECUTADOS; preserva JWT/RBAC, tenant A/B, PII y soporte read-only.
- Solo docs/v5 y CHANGELOG mediante CAS fast-forward en dev; Scope Lock/procedencia V5 exact-SHA y A5 independiente; sin exporters, alertas, infraestructura, cambios runtime ni QA/main/PROD.

## 2026-11-14 — V5 C11-04.M01.02: actores I/O y fronteras de observabilidad [Javier :20]
- Define A01–A14 actores, I01/O01–I14/O14 superficies y F01–F12 de confianza para logs, métricas, trazas, costos, SLI/SLO y diagnósticos.
- Ancla seis fuentes Git con blobs reales y distingue instrumentos in-process existentes de exporter, collector y tableros FUTUROS_NO_IMPLEMENTADOS; preserva tenant A/B, JWT/RBAC y protección PII.
- Solo docs/v5 + CHANGELOG con CAS fast-forward dev y checks Scope Lock/V5 exact-SHA; A5 independiente pendiente; sin runtime, proveedores, alertas ni QA/main/PROD.

## 2026-11-14 — V5 C11-04.M01.01: alcance observabilidad sin datos sensibles [Javier :20]
- Define objetivo y fronteras de logs, métricas, trazas, alertas, SLO y costos; 16 invariantes NR, 6 casos positivos y 8 negativos FUTUROS_NO_EJECUTADOS.
- Ancla seis fuentes Git reales, distingue counters y CorrelationId existentes de exporters externos y SLO certificados; preserva aislamiento tenant A/B, secretos y soporte read-only.
- Solo docs/v5 + CHANGELOG con fast-forward CAS dev, Scope Lock/V5 exact-SHA y A5 independiente pendiente; sin instrumentar proveedores, runtime ni QA/main/PROD.

## 2026-11-14 — V5 C11-02.M01.05: aceptación observable API modular [Javier :20]
- Define AC01–AC18, N01–N18 y RNF01–RNF12 como oráculos FUTUROS_NO_EJECUTADOS de HTTP, JWT/RBAC, tenant A/B, soporte read-only, errores, compatibilidad e integración.
- Traza fuentes API/EF con blobs anteriores e invariantes sin atribuir PASS a HTTP/MySQL/Playwright no ejecutados; no implementa endpoints, trabajadores ni cambios de autorización.
- Únicamente docs/v5 y CHANGELOG en dev por fast-forward CAS, con Scope Lock/V5 exact-SHA y A5 independiente pendiente; sin QA/main/PROD, secretos ni eventos.

## 2026-11-14 — V5 C11-03.M01.05: aceptacion observable Outbox y eventos [Javier :10]
- Define AC01–AC18, N01–N18 y RNF01–RNF12 como oraculos FUTUROS_NO_EJECUTADOS de atomo UoW, tenant A/B, claims, idempotencia, reintentos y fallos parciales.
- Distingue pruebas xUnit/SQLite existentes de MySQL/HTTP/dispatch real no ejecutado; no inventa PASS, throughput, garantias exactly-once ni autoriza provider.
- Solo docs/v5 y CHANGELOG con fast-forward CAS dev, checks Scope Lock/V5 exact SHA y A5 independiente pendiente, sin eventos, workers, SQL, secrets, QA/main/PROD.

## 2026-11-14 — V5 C11-03.M01.04: riesgos supuestos y decisiones Outbox [Javier :10]
- Registra R01–R16 como riesgos POTENCIALES, S01–S10 NO_VERIFICADOS y D01–D10 PENDIENTES sobre transaccion, tenant A/B, idempotencia, reintentos, orden y seguridad.
- Traza contratos Domain/Application/Infrastructure y evidencia xUnit no ejecutada; P01–P08/N01–N12 son FUTUROS_NO_EJECUTADOS, no bugs ni pruebas certificadas.
- Solo docs/v5 y CHANGELOG con CAS FF dev, Scope Lock/V5 exact-SHA, A5 pendiente de otra Task; sin implementar eventos, workers, SQL, provider ni QA/main/PROD.

## 2026-11-14 — V5 C11-03.M01.03: inventario de fuentes Outbox y brechas [Javier :10]
- Inventaria 15 archivos y contratos con blobs Git reales Domain/Application/Infrastructure/API/xUnit; los tests codificados no se presentan como ejecutados ni MySQL certificado.
- Clasifica G01–G14 como brechas de evidencia NO BUGS confirmados y P01–P08/N01–N14 como FUTUROS_NO_EJECUTADOS, protegiendo tenant A/B, idempotencia y confidencialidad.
- Solo docs/v5 y CHANGELOG; fast-forward CAS dev, checks exact-SHA Scope Lock/V5 y A5 independiente pendiente, sin crear eventos, workers, dispatcher, proveedores ni QA/main/PROD.

## 2026-11-14 — V5 C11-02.M01.04: riesgos y decisiones de API modular [Javier :00]
- Registra R01–R16 POTENCIALES, S01–S10 NO_VERIFICADOS y D01–D10 PENDIENTES sobre API, contratos, tenant A/B, JWT/RBAC, errores, compatibilidad y soporte; no afirma vulnerabilidades.
- Ancla ocho superficies API/Application en blobs reales y P01–P08/N01–N12 FUTUROS_NO_EJECUTADOS, preservando separación plataforma/empresa, validación backend y rollback.
- Documento M01.04 + CHANGELOG solo en dev por CAS fast-forward, con Scope Lock/V5 exact-SHA y A5 independiente pendiente; sin endpoints, SQL, workers, secretos ni QA/main/PROD.

## 2026-11-14 — V5 C11-03.M01.02: actores contratos y fronteras Outbox [Javier :10]
- Define actores A01–A14, interfaces I/O01–I/O12 y fronteras F01–F12 tenant A/B, UoW, idempotencia, dispatcher y proveedor futuro, sin ejecutar ni habilitar eventos o Tasks.
- Traza seis fuentes con blobs reales y P01–P08/N01–N14 FUTUROS_NO_EJECUTADOS; no confunde interfaces con infraestructura, transacciones o workers certificados.
- Solo docs/v5 y CHANGELOG con fast-forward CAS dev, Scope Lock/procedencia V5 exact SHA, A5 independiente pendiente; sin SQL, JWT/RBAC operativo, secretos, QA/main/PROD.

## 2026-11-14 — V5 C11-02.M01.03: inventario de contratos API por blobs [Javier :00]
- Inventaría 16 fuentes reales de API, Application, middleware, filtros y .NET/EF, con blobs y lectura acotada de Auth/tenant/plataforma, DTO, errores y soporte read-only; no declara cobertura completa.
- Enumera G01–G12 brechas de evidencia NO_FALLOS_CONFIRMADOS y pruebas P01–P08/N01–N12 FUTUROS_NO_EJECUTADOS; preserva JWT/RBAC/tenant A/B y compatibilidad Angular.
- Dos Markdown por CAS fast-forward dev, checks Scope Lock/V5 exact-SHA y A5 independiente pendiente; sin runtime, SQL, eventos, secretos ni QA/main/PROD.

## 2026-11-14 — V5 C11-03.M01.01: contrato documental Outbox y eventos [Javier :10]
- Define alcance y no-regresion NR01–NR16, P01–P08/N01–N12 FUTUROS_NO_EJECUTADOS para un futuro outbox/relay tenant-bound con idempotencia, correlacion y fallo parcial.
- Traza seis fuentes del repositorio con blobs Git; OutboxRetryProcessor no contiene scheduler y CorrelationId no concede autorizacion. No se implementan eventos, webhooks ni ejecucion entre Tasks.
- Solo docs/v5 y CHANGELOG; fast-forward CAS dev y checks Scope Lock/procedencia V5 exact-SHA; A5 pendiente de Task distinta, sin SQL, proveedores, secrets ni QA/main/PROD.

## 2026-11-14 — V5 C11-02.M01.02: actores I/O y fronteras API modular [Javier :00]
- Matriz A01–A12 de actores, I/O01–I/O12 de entradas/salidas y F01–F10 de fronteras de confianza para API modular; P01–P08/N01–N12 escenarios FUTUROS_NO_EJECUTADOS.
- Traza AuthController, TenantContextController, PlataformaSesionController, Program y versiones .NET/EF con blobs; preserva fail-closed, JWT/RBAC, tenant A/B, plataforma separada y soporte read-only.
- Solo preparación docs/v5 y CHANGELOG, fast-forward CAS y checks Scope Lock/V5 exact-SHA, A5 independiente pendiente; sin endpoints, workers, DB, secrets, QA/main/PROD.

## 2026-11-14 — V5 C11-02.M01.01: contrato de alcance API modular [Javier :00]
- Delimita puertos/casos de uso, DTO, validación, compatibilidad y errores seguros sin implementar endpoints; NR01–NR14 y escenarios P01–P08/N01–N12 FUTUROS_NO_EJECUTADOS.
- Traza AuthController, TenantContextController, PlataformaSesionController, Program y referencias .NET/EF con blobs reales; preserva JWT/RBAC/tenant A/B, soporte read-only y separación plataforma/empresa.
- Solo docs/v5 y CHANGELOG con CAS fast-forward en dev y checks exact-SHA Scope Lock/V5; A5 independiente pendiente, sin API, SQL, workers, secretos ni QA/main/PROD.

## 2026-11-14 — V5 C10-04.M01.06: gates rollback liberacion documental [Javier :50]
- Consolida los cinco antecedentes M01.01-.05 LISTO+A5 independiente y B01-B14 como controles NO_GO de cache/jobs/backups/exportaciones, tenant A/B, idempotencia y reversibilidad; sin simular servicios.
- Condiciona M02 a A0-A4 y A5 independiente de esta M01.06, con DAG/PLAN/COLA vivo; P01-P06/N01-N10 FUTUROS_NO_EJECUTADOS, no backup/restore ni worker real.
- Solo docs/v5 y CHANGELOG en dev con Scope Lock/V5 exact-SHA y CAS fast-forward; sin SQL, proveedores, eventos, secretos ni QA/main/PROD.

## 2026-11-14 — V5 C11-01.M01.05: aceptacion observable modelo relacional [Javier :50]
- Define criterios P01-P16 y N01-N16 FUTUROS_NO_EJECUTADOS para FK compuestas, tenant A/B, legacy nullable, migraciones expand/contract, soporte read-only y rollback transaccional.
- Fija RNF01-RNF10 y gate de evidencia MySQL efimero/EF, CI exact-SHA y A5 independiente, sin atribuir PASS funcional a criterios redactados ni indices/migraciones aplicadas.
- Solo docs/v5 y CHANGELOG mediante CAS fast-forward dev, controles Scope Lock/V5 del SHA, sin SQL real, endpoints, providers, secrets, QA/main/PROD.

## 2026-11-14 — V5 C11-01.M01.04: riesgos, supuestos y decisiones relacionales [Javier :50]
- Registra R01-R16 riesgos POTENCIALES, S01-S10 supuestos NO_VERIFICADOS y D01-D10 decisiones PENDIENTES sobre tenant A/B, claves FK, historial nullable, migraciones, índices y rollback.
- Traza código real de Empresa, Cliente, Sucursal, UsuarioEmpresa, EF, filtros y constraints, sin inferir migraciones aplicadas ni defectos certificados; P01-P08/N01-N12 FUTUROS_NO_EJECUTADOS.
- Sólo docs/v5 + CHANGELOG con Scope Lock y V5 provenance exact-SHA en dev, A5 independiente pendiente; cero SQL/Aiven, permisos, eventos, QA/main/PROD.

## 2026-11-14 — V5 C11-01.M01.03: inventario de fuentes y brechas del modelo relacional [Javier :50]
- Documenta 15 fuentes Git con blobs y clasificación de evidencia para FK/indices/tenant/EF/MySQL, incluyendo legacy nullable, constraints fisicos y migrations sin confundir existencia con esquema aplicado.
- Registra G01-G12 como brechas de evidencias, P01-P08/N01-N12 FUTUROS_NO_EJECUTADOS; no certifica MySQL, tests, integridad cross-tenant ni migraciones reales.
- Sólo documento C11-01.M01.03 y CHANGELOG en dev bajo CAS fast-forward y Scope Lock/V5 del SHA; A5 independiente pendiente, sin runtime, datos, APIs, secretos, QA/main/PROD.

## 2026-11-14 — V5 C11-01.M01.02: actores interfaces y fronteras del modelo relacional [Javier :50]
- Define A01-A12, I01/O01-I12/O12 y F01-F09 para claves/FK, ownership, transacciones, EF/MySQL, API y tenant A/B; los contratos son FUTUROS_NO_IMPLEMENTADOS.
- Traza Empresa, Cliente.EmpresaId nullable y AppDbContext con blobs de dev, preservando soporte read-only y frontera pública; P01-P06/N01-N10 son escenarios NO_EJECUTADOS.
- Exclusivamente docs/v5 y CHANGELOG por CAS fast-forward dev y checks exact-SHA Scope Lock/V5; A5 independiente pendiente, sin SQL, endpoints, workers, permisos, QA/main/PROD.

## 2026-11-14 — V5 C11-01.M01.01: alcance y no-regresion de modelo relacional [Javier :50]
- Fija objetivo/limitaciones de PK, FK, indices, ownership, MySQL y estrategia reversible; NR01-NR14, P01-P07/N01-N10 son criterios futuros NO_EJECUTADOS.
- Traza Empresa, Cliente.EmpresaId nullable historico, AppDbContext y pin EF/MySQL mediante blobs exactos; no confunde documentos, permisos o migraciones aplicadas.
- Solo docs/v5 y CHANGELOG en dev, Scope Lock y V5 provenance exact-SHA; A5 independiente pendiente, sin SQL/DB, JWT/RBAC, eventos, QA/main/PROD.

## 2026-11-14 — V5 C10-04.M01.05: aceptacion observable cache, jobs, backups, exportaciones [Javier :40]
- Define AC01–AC18, N01–N18 y RNF01–RNF12 para cache tenant A/B, idempotencia, worker, restore efimero, exportacion, seguridad, TTL, CSV injection y observabilidad; todos son oraculos FUTUROS_NO_EJECUTADOS.
- Traza lineas base de caché, OutboxRetryProcessor, filtro HTTP y worker Cloudinary DEV opt-in; sin atribuir PASS a xUnit/Node/MySQL/Playwright no ejecutados ni inventar RPO/RTO.
- Solo Markdown + CHANGELOG en dev con checks Scope Lock/procedencia V5 exact SHA, A5 independiente pendiente; no tareas, dispatcher, cron, proveedores, secretos, QA/main/PROD.

## 2026-11-14 — V5 C10-04.M01.04: riesgos, supuestos y decisiones cache/jobs/backups [Javier :40]
- Registra R01–R16 como riesgos POTENCIALES, S01–S10 NO_VERIFICADOS y D01–D10 PENDIENTES, con controles tenant A/B, JWT/RBAC, idempotencia, reintentos, cifrado, restauración, exportación y PII.
- Traza blobs reales de caché, OutboxRetryProcessor, worker Cloudinary DEV opt-in y documentos M01 previos; P01–P08/N01–N12 son FUTUROS_NO_EJECUTADOS, no defectos ni pruebas confirmados.
- Solo Markdown y CHANGELOG en dev, A5 independiente pendiente; no backups, cambios MySQL, dispatcher entre Tasks, proveedores, secretos, QA/main/PROD ni migración.

## 2026-11-14 — V5 C10-04.M01.03: inventario de fuentes de cache, jobs, backups y exportaciones [Javier :40]
- Inspecciona diez rutas fuente con blobs Git exactos de PublicStoreCache, OutboxRetryProcessor, cache HTTP, xUnit y Cloudinary DEV opt-in, distinguiendo código existente de ejecución certificada.
- Clasifica G01–G12 como brechas de EVIDENCIA, no bugs confirmados; oráculos P01–P08/N01–N12 FUTUROS_NO_EJECUTADOS. Mantiene histórico de archivos NO_VERIFICADO, no cero.
- Solo inventario documental y CHANGELOG en dev; A5 independiente pendiente; no se activan workers, cron, dispatcher, MySQL, backups, Cloudflare R2, QA/main/PROD o proveedores.

## 2026-11-14 — V5 C10-04.M01.02: actores e interfaces cache jobs backups [Javier :30]
- Define A01-A14 actores, I01/O01-I12/O12 contratos propuestos y F01-F09 fronteras para cache, trabajos idempotentes, export y backup aislados por tenant; NO_IMPLEMENTADOS.
- Traza documentos originales C10-01/02/03; inventario de archivos reales NO_VERIFICADO (no equivale a cero), y P01-P06/N01-N06 FUTUROS_NO_EJECUTADOS, sin afirmar seguridad runtime.
- Sólo documento M01.02 y CHANGELOG mediante CAS fast-forward dev con checks Scope Lock/V5 exact SHA; M01.01 de otro autor se respeta, A5 pendiente, ningún worker/evento/proveedor/QA/main/PROD.

## 2026-11-14 — V5 C10-04.M01.01: alcance y no-regresión cache, jobs, backups y exportaciones [Javier :40]
- Delimita NR01–NR16 y oráculos futuros P01–P08/N01–N14 para tenant A/B, idempotencia, retries, backups/restore, PII y exportación; escenarios FUTUROS_NO_EJECUTADOS, nunca PASS runtime.
- Coteja blobs de PublicStoreMemoryCache, DevCloudinaryLegacyNormalizationHostedService y fuentes C10-01/C10-03; no declara inventario histórico cero, jobs genéricos activos ni R2 disponible.
- Solo documento M01.01 y CHANGELOG en dev; A5 independiente pendiente, sin dispatcher, worker nuevo, MySQL/DB, secretos, QA/main/PROD ni cambio de proveedores.

## 2026-11-14 — V5 C09-02.M01.05: aceptacion observable fidelizacion [Javier :30]
- Establece AC01-AC16 con dieciseis positivos y negativos FUTUROS_NO_EJECUTADOS para saldo, canje, promos, reseñas, moderación, opt-out, posventa, tenant A/B, JWT/RBAC y reversibilidad.
- Registra RNF01-RNF10 y traza blobs M01.01–04, distinguiendo contratos futuros de pruebas reales, sin atribuir métricas ni PASS de MySQL/.NET/Angular/Playwright inexistentes.
- Sólo docs/v5 y CHANGELOG por CAS fast-forward dev, Scope Lock y V5 exact SHA; A5 por otra Task Javier pendiente, sin eventos, secrets, proveedores, QA/main/PROD.

## 2026-11-14 — V5 C09-02.M01.04: riesgos, supuestos y decisiones fidelizacion [Javier :30]
- Documenta R01-R14 riesgos POTENCIALES (no vulnerabilidades acreditadas), S01-S08 supuestos con estatus verificable y D01-D08 decisiones pendientes para puntos, promociones, reseñas y posventa, preservando la frontera tenant A/B.
- Traza los blobs M01.01/02/03 y las entidades Cliente, Venta, CreditoCliente y UsuarioEmpresa; P01-P08/N01-N08 son FUTUROS_NO_EJECUTADOS, nunca PASS runtime.
- Exclusivamente docs/v5 + CHANGELOG por CAS fast-forward dev y CI Scope Lock/V5 en SHA exacto; A5 independiente pendiente, sin JWT/RBAC nuevo, migraciones, eventos, proveedores, QA/main/PROD.

## 2026-11-14 — V5 C09-02.M01.03: inventario de codigo y brechas de fidelizacion [Javier :30]
- Contrasta 12 rutas originales con blobs exactos de Domain, Application, Infrastructure, API y Angular para delimitar Cliente, Venta, CreditoCliente, auditoria y permisos frente a futuros puntos, promociones y reseñas.
- Formula G01-G12 de evidencia y P01-P08/N01-N08 FUTUROS_NO_EJECUTADOS; no confunde ausencia de prueba con defecto real ni afirma runtime certificado.
- Unicamente doc M01.03 y CHANGELOG en dev por CAS fast-forward, Scope Lock y procedencia V5 exact SHA; A5 independiente pendiente, sin activar MySQL, JWT/RBAC, eventos, workers, proveedores, QA/main/PROD.

## 2026-11-14 — V5 C09-03.M01.05: aceptacion observable Social Hub [Javier :10]
- Define AC01-AC16 con dieciseis positivos P y dieciseis negativos N FUTUROS_NO_EJECUTADOS para campañas, segmentos, opt-out, idempotencia, tenant A/B, JWT/RBAC, métricas, auditoría y rollback, sin atribuir PASS runtime.
- Registra RNF01-RNF10 y traza cuatro documentos M01 anteriores y blobs Cliente, UsuarioEmpresa, MensajeOutbox; .NET/MySQL/Angular/Playwright se reservan para fases de código real y CI dirigida.
- Sólo docs/v5 + CHANGELOG por CAS fast-forward y checks Scope Lock/V5 exact SHA; A5 independiente pendiente, sin eventos, workers, proveedores, secretos, QA/main/PROD.

## 2026-11-14 — V5 C09-03.M01.04: riesgos supuestos y decisiones Social Hub [Javier :10]
- Registra R01-R14 riesgos POTENCIALES, S01-S08 supuestos NO_VERIFICADOS y D01-D08 decisiones PENDIENTES sobre consentimiento/opt-out, tenant A/B, JWT/RBAC, segmentación, contenido, proveedores, métricas y rollback; sin declarar vulnerabilidades existentes.
- Traza M01.01-03 y blobs reales de Cliente, UsuarioEmpresa, MensajeOutbox y guard Angular; P01-P06/N01-N10 son FUTUROS_NO_EJECUTADOS, no se atribuye PASS runtime.
- Solo docs/v5 y CHANGELOG, CAS fast-forward exact-HEAD y Scope Lock/V5 del SHA; A5 pendiente distinto Task, sin eventos, workers, proveedores, secretos ni QA/main/PROD.

## 2026-11-14 — V5 C09-03.M01.03: inventario de codigo y brechas Social Hub/campanas [Javier :10]
- Contrasta 14 rutas y blobs reales de Domain/Application/API/Infrastructure/Angular (Cliente, UsuarioEmpresa, Tenant, ClienteService, Outbox, SMTP, auditoria y guards), delimitando hechos y capacidad futura no acreditada.
- Trazabilidad de M01.01/.02 y CRM/mensajería; G01-G14 preguntas de evidencia y P01-P04/N01-N08 FUTUROS_NO_EJECUTADOS con tenant A/B, JWT/RBAC, opt-out, replay y privacidad; no declara defectos ni PASS runtime.
- Solo docs/v5 y CHANGELOG por fast-forward CAS, checks Scope Lock/V5 exact SHA; A5 pendiente, sin envío, eventos, workers, migraciones, secretos, QA/main/PROD.

## 2026-11-14 — V5 C09-03.M01.02: actores e interfaces Social Hub/campanas [Javier :10]
- Documenta A01-A14 actores conceptuales, I01-I14 entradas/salidas propuestas y F01-F10 fronteras para campañas, contenidos, calendarios, consentimiento, opt-out, métricas y aislamiento tenant, sin crear roles o endpoints live.
- Traza los blobs de C09-03.M01.01, Cliente, UsuarioEmpresa, MensajeOutbox, Auditoria y guard Angular; P01-P04/N01-N08 quedan FUTUROS_NO_EJECUTADOS, sin atribuir PASS runtime.
- Únicamente docs/v5 y CHANGELOG con CAS fast-forward y checks Scope Lock/V5 del SHA exacto; A5 independiente pendiente, sin proveedores, eventos, secretos, migraciones, QA/main/PROD.

## 2026-11-14 — V5 C09-03.M01.01: alcance y no-regresion de Social Hub/campanas [Javier :10]
- Define alcance FUTURO para contenidos, segmentos, calendarios, campañas y métricas con consentimiento por canal, opt-out y límites por tenant, sin habilitar emisiones ni publicaciones reales.
- Documenta NR01-NR16 y P01-P05/N01-N08 FUTUROS_NO_EJECUTADOS con referencias a blobs de Cliente, UsuarioEmpresa, MensajeOutbox, Auditoria y guarda Angular; no certifica tests runtime.
- Solo documentación y CHANGELOG en dev por CAS fast-forward con Scope Lock y procedencia V5 exact-SHA; A5 independiente pendiente, sin eventos, proveedores, migraciones, QA/main/PROD.

## 2026-11-14 — V5 C09-02.M01.02: actores, interfaces y fronteras de fidelizacion [Javier :00]
- Identifica A01–A14 actores, I01–I12 interfaces futuras y F01–F09 fronteras de confianza para puntos, promociones, resenas, moderacion y posventa; contrasta seis blobs originales sin suponer codigo de fidelizacion vivo.
- Documenta P01–P08/N01–N08 FUTUROS_NO_EJECUTADOS sobre tenant A/B, JWT/RBAC, PII, canjes concurrentes, replay, consentimiento y antifraude, sin afirmar PASS runtime.
- Sólo docs/v5 y CHANGELOG en dev mediante CAS fast-forward, Scope Lock/V5 exact-SHA y A5 pendiente de otra Task; sin pagos, worker, eventos, secretos ni QA/main/PROD.

## 2026-11-14 — V5 C09-02.M01.01: alcance y no-regresion de fidelizacion/reputacion [Javier :00]
- Delimita puntos, promociones, resenas, moderacion y postventa como alcance FUTURO; contrasta seis blobs de Cliente, Venta, TiendaCuentaCliente, CreditoCliente, Auditoria y UsuarioEmpresa, sin confundir puntos con dinero o credito.
- Establece NR01–NR14 y P01–P08/N01–N08 FUTUROS_NO_EJECUTADOS para tenant A/B, JWT/RBAC, antifraude, replay, privacidad, moderacion, consentimiento, idempotencia y rollback, sin afirmar PASS runtime.
- Solo docs/v5 y CHANGELOG_AI.md en dev con CAS fast-forward y Scope Lock/V5 exact-SHA; A5 independiente pendiente, sin pagos, eventos, workers, secretos ni QA/main/PROD.

## 2026-11-14 — V5 C09-01.M01.05: aceptacion observable y pruebas futuras de CRM universal [Javier :00]
- Especifica AC01–AC14 positivos/N01–N14 negativos FUTUROS_NO_EJECUTADOS para ficha, timeline, oportunidades, segmento, export, consentimiento, tenant A/B, JWT/RBAC, legacy NULL y replay.
- Define NFR01–NFR10 y matriz futura .NET 10, MySQL 8.4/EF, Angular 22.2.1 y Playwright sin inventar umbrales, suites ejecutadas o autorización de implementación.
- Sólo docs/v5 y CHANGELOG en dev con CAS fast-forward y checks Scope Lock/V5 exact-SHA; A5 independiente pendiente, sin CRM live, eventos, migraciones, secretos ni QA/main/PROD.

## 2026-11-14 — V5 C09-04.M01.05: criterios de aceptacion observables de mensajeria [Javier :50]
- Define P01–P16 positivos, N01–N16 negativos y RNF01–RNF10 FUTUROS_NO_EJECUTADOS para email, WhatsApp, idempotencia, HMAC/replay, consentimiento, privacidad, tenant A/B y resiliencia.
- Traza los cuatro artefactos M01 previos y mantiene puertas A0–A5, CI dirigida en fases runtime y decisiones de proveedor abiertas; no crea endpoints, workers, eventos o webhooks.
- Entrega docs/v5 + CHANGELOG con CAS fast-forward y checks Scope Lock/V5 exact-SHA, A5 independiente pendiente, sin migraciones, secretos, QA/main/PROD.

## 2026-11-14 — V5 C09-01.M01.04: riesgos, supuestos y decisiones del CRM universal [Javier :00]
- Registra R01–R16 POTENCIALES, S01–S08 NO_VERIFICADOS y D01–D08 PENDIENTES ligados a Cliente/UsuarioEmpresa, 16 fuentes M01.03, tenant A/B, JWT/RBAC, PII, replay, consentimiento y legacy NULL.
- Define P01–P08/N01–N08 FUTUROS_NO_EJECUTADOS y controles para timeline, segmentos, oportunidades, exportación, accesibilidad y rollback; no identifica defectos explotables ni PASS runtime.
- Solo documento y CHANGELOG en dev con CAS fast-forward; Scope Lock/V5 exact-SHA, A5 independiente pendiente, sin CRM ejecutable, DB, eventos, secretos o QA/main/PROD.

## 2026-11-14 — V5 C09-04.M01.04: riesgos, supuestos y decisiones pendientes de mensajeria [Javier :50]
- Registra R01–R14, S01–S08 y D01–D08 como riesgos, hipotesis falsables y decisiones abiertas para consentimientos, opt-out, tenant A/B, HMAC/replay, idempotencia, secretos y rollback, sin inferir fallos reales.
- Vincula tres artefactos M01 previos con blobs reales de Outbox/Email/WhatsApp; P01–P08/N01–N12 FUTUROS_NO_EJECUTADOS y sin activar trabajadores, proveedores, eventos o webhooks.
- Publicacion documental estricta en dev por CAS fast-forward, Scope Lock y procedencia V5 exact SHA; A5 independiente pendiente, sin migraciones, QA/main/PROD.

## 2026-11-14 — V5 C09-04.M01.03: inventario de codigo y brechas de mensajeria multicanal [Javier :50]
- Inventaría catorce rutas reales y sus blobs de Domain/Application/API/Infrastructure para Outbox, email y WhatsApp; distingue intención durable de envío, dispatcher, marketing y webhook aún no certificados.
- Documenta G01–G12 como preguntas verificables, P01–P08/N01–N12 FUTUROS_NO_EJECUTADOS para JWT/RBAC, tenant A/B, PII, consentimiento, idempotencia, secretos y retry; no declara fallos ni PASS runtime no demostrados.
- Sólo docs/v5 y CHANGELOG en dev con CAS fast-forward; Scope Lock y procedencia V5 exact-SHA, A5 pendiente por otra Task; sin servicios externos, eventos, migraciones, QA/main/PROD.

## 2026-11-14 — V5 C09-04.M01.02: actores, interfaces y fronteras de mensajeria multicanal [Javier :50]
- Define A01–A14 actores, I01–I12 contratos de entrada/salida y F01–F09 fronteras para email/WhatsApp, tenant A/B, idempotencia, consentimiento y replay; propuesta documental, sin activar transportes ni eventos.
- Relaciona nueve fuentes reales de Outbox/Email/WhatsApp/SMTP con sus blobs; P01–P08/N01–N12 quedan FUTUROS_NO_EJECUTADOS y se conserva la exigencia de permisos backend, sin supuestos de certificación runtime.
- Dos archivos Markdown en dev, Scope Lock y control V5 deben aprobar sobre SHA exacto; A5 independiente pendiente, sin migraciones, secretos, QA/main/PROD ni webhook dispatcher.

## 2026-11-14 — V5 C09-04.M01.01: alcance y no-regresión de mensajería multicanal [Javier :50]
- Fija NR01–NR14 y P01–P08/N01–N12 FUTUROS_NO_EJECUTADOS para email, WhatsApp, replay, consentimiento, PII, JWT/RBAC, tenant A/B y secretos; sin enviar mensajes ni activar eventos.
- Inventaría nueve blobs reales de Outbox/Email/WhatsApp/API/SMTP y delimita alcance documental M01 frente a workers/webhooks/migraciones futuros; no atribuye PASS runtime ni autorización QA/main/PROD.
- Publicación estricta en dev con CAS fast-forward; Scope Lock y procedencia V5 requieren SUCCESS en el SHA publicado, A5 independiente por otra Task Javier.

## 2026-11-14 — V5 C09-01.M01.03: inventario de codigo y brechas de CRM universal [Javier :40]
- Identifica dieciseis fuentes con blobs Git en Cliente, Empresa, UsuarioEmpresa, API, Application, Infrastructure y Angular; distingue el CRUD existente de timeline, segmentos y oportunidades aun no certificados.
- Registra G01–G12 como brechas de evidencia y P01–P08/N01–N12 FUTUROS_NO_EJECUTADOS sobre JWT/RBAC, tenant A/B, PII, legado NULL, consentimiento, replay y accesibilidad; no inventa vulnerabilidades ni PASS runtime.
- Sólo docs/v5 y CHANGELOG en dev con FF/CAS, Scope Lock y procedencia V5 exact-SHA; A5 de otra Task, sin CRM operativo, proveedores, eventos, migraciones, secretos ni QA/main/PROD.

## 2026-11-14 — V5 C09-01.M01.02: actores e interfaces de CRM universal [Javier :40]
- Mapea A01–A14 actores, I01–I12 interfaces y F01–F09 fronteras futuras para CRM, con fuente M01.01 y cinco blobs reales de Domain y Angular; no acredita CRM operativo.
- Documenta P01–P08/N01–N12 FUTUROS_NO_EJECUTADOS para tenant A/B, RBAC, PII, ownership NULL, replay, consentimiento y exportación segura sin atribuir PASS runtime.
- Sólo docs/v5 y CHANGELOG en dev, FF/CAS Scope Lock/V5 provenance exact-SHA, A5 por Task independiente; sin mensajes, jobs, migraciones, secretos ni QA/main/PROD.

## 2026-11-14 — V5 C09-01.M01.01: alcance y no-regresion de CRM universal [Javier :40]
- Delimita ficha, timeline, segmentos, oportunidades y actividades por Empresa reutilizando Cliente/UsuarioEmpresa; contrasta cinco blobs reales en Domain y Angular sin afirmar CRM funcional.
- Define NR01–NR14 y P01–P08/N01–N12 FUTUROS_NO_EJECUTADOS para JWT/RBAC, tenant A/B, PII, ownership NULL, idempotencia, consentimientos, exportaciones y accesibilidad.
- Sólo docs/v5 y CHANGELOG en dev mediante FF/CAS; Scope Lock/provenance V5 exact-SHA y A5 pendiente por Task distinta, sin mensajería, workers, migraciones, secretos ni QA/main/PROD.

## 2026-11-14 — V5 C08-04.M01.04: riesgos, supuestos y decisiones de extensiones por rubro [Javier :40]
- Clasifica R01–R16 como riesgos POTENCIALES, S01–S08 como supuestos NO_VERIFICADOS y D01–D08 como decisiones PENDIENTES, anclados a documentos M01.01–03/M01.05 y blobs reales de Empresa, UsuarioEmpresa y guard Angular.
- Define TP01–TP08/TN01–TN08 FUTUROS_NO_EJECUTADOS para tenant A/B, JWT/RBAC, host confiable, legacy NULL, replay, revocación, versiones, accesibilidad y rollback; no afirma fallos reales ni PASS de runtime.
- Sólo Markdown y CHANGELOG en dev, publicación fast-forward/CAS con Scope Lock y procedencia V5 exact-SHA, A5 independiente pendiente; sin módulos vivos, migraciones, eventos, secretos ni QA/main/PROD.

## 2026-11-14 — V5 C08-04.M01.05: criterios de aceptacion de extensiones por rubro [Javier :30]
- Define AC01–AC14/N01–N14 FUTUROS_NO_EJECUTADOS para capacidades por empresa, JWT/RBAC, tenant A/B, reservas, membresias, catalogo, host confiable, idempotencia y rollback.
- Establece NFR01–NFR10 y matriz futura .NET/MySQL/Angular/Playwright con fuentes M01.01–03 y blobs reales; no inventa implementacion, test runtime ni vulnerabilidad demostrada.
- Solo docs/v5 y CHANGELOG en dev con Scope Lock y procedencia V5 exact-SHA; A5 pendiente de otra Task Javier, sin workers, migraciones, QA/main/PROD, secretos ni cambio de Tasks.

## 2026-11-14 — V5 C10-03.M01.03: inventario de código y brechas de almacenamiento [Javier :30]
- Inventario ESTATICO verificable de doce fuentes y blobs Git de contratos Cloudinary, contexto tenant, sanitizacion, tests y script de auditoria DEV; NO ejecuta inventario real de objetos ni asume cero.
- Registra G01–G10 brechas de evidencia y P01–P08/N01–N08 oraculos FUTUROS NO_EJECUTADOS, con tenant A/B, MIME real, TTL, retencion y rollback; INVENTARIO_HISTORICO=NO_VERIFICADO.
- Solo docs/v5 y CHANGELOG en dev con Scope Lock y procedencia V5 exact-SHA, A5 independiente pendiente; sin R2, DNS, Cloudinary live, MySQL, migraciones, secretos ni QA/main/PROD.

## 2026-11-14 — V5 C08-04.M01.03: inventario de fuentes y brechas sectoriales [Javier :20]
- Revisa once blobs reales de dominio y frontend para Empresa, UsuarioEmpresa, Producto, Sucursal, Cliente, host binding, rutas, permisos y contexto tenant, diferenciando código existente de capabilities sectoriales aún no acreditadas.
- Registra G01–G10 como brechas de evidencia y P01–P08/N01–N08 como pruebas FUTURAS NO_EJECUTADAS; no declara vulnerabilidades, integración ni aislamiento runtime certificados por una lectura documental.
- Sólo Markdown y CHANGELOG en dev, A0–A3 documentales y A4 por Scope Lock/V5 exact-SHA más PLAN/COLA/BITACORA; A5 independiente pendiente, git revert sin QA/main/PROD.

## 2026-11-14 — V5 C08-04.M01.02: actores y contratos de interfaces sectoriales [Javier :20]
- Identifica A01–A14 actores/superficies e I01–I12 entradas, salidas y fronteras de capacidades futuras de agenda, membresias, catalogo, sitios, tenancy y auditoria.
- Coteja seis blobs reales incluyendo Empresa, UsuarioEmpresa, permisoGuard e interceptor; documenta T01–T06 positivos/negativos FUTUROS NO_EJECUTADOS, sin atribuir nueva funcionalidad, backend ni pruebas runtime.
- Solo docs/v5 y CHANGELOG; A4 requiere Scope Lock y procedencia V5 exact-SHA/readback; A5 independiente pendiente; rollback git revert, sin migraciones, QA/main/PROD ni cambios de Tasks.

## 2026-11-14 — V5 C08-04.M01.01: alcance y no-regresion de extensiones por rubro [Javier :20]
- Delimita capabilities futuras de agenda, membresias, catalogo y sitios por empresa sin duplicar tenant, identidad ni permisos; M01.01 solo documental, M01.06 no liberada.
- Contrasta seis blobs fuente y define NR01–NR12, P01–P06/N01–N06 como escenarios FUTUROS NO_EJECUTADOS, preservando JWT/RBAC, tenant A/B, historial e idempotencia.
- Solo nuevo Markdown y CHANGELOG en dev; A4 Scope Lock/V5 exact-SHA y PLAN/COLA/BITACORA readback, A5 independiente pendiente; rollback git revert sin QA/main/PROD.

## 2026-11-14 — V5 C08-03.M01.05: criterios de aceptación de gimnasio [Javier :10]
- Define AC01–AC14 con P01–P14/N01–N14 FUTUROS NO_EJECUTADOS, integrando membresía, tenant A/B, JWT/RBAC, vencimiento UTC/DST, suspensión, replay e idempotencia.
- Establece NFR01–NFR08 y futura matriz CI .NET/MySQL/Angular/Playwright, sin atribuir pruebas reales a contratos Markdown ni resolver decisiones D01–D08 pendientes.
- Sólo docs/v5 y CHANGELOG en dev por FF/CAS; Scope Lock/V5 exact-SHA y A5 por otra Task Javier; sin pagos, hardware, migraciones, workers ni QA/main/PROD.

## 2026-11-14 — V5 C08-03.M01.04: riesgos, supuestos y decisiones de membresías [Javier :10]
- Define R01–R14 POTENCIALES, S01–S08 NO_VERIFICADOS y D01–D08 PENDIENTES, sobre evidencias de 12 blobs reales del inventario M01.03.
- Especifica TP01–TP08/TN01–TN12 FUTUROS NO_EJECUTADOS para JWT/RBAC, tenant A/B, expiración, suspensión, replay e idempotencia, sin certificar gimnasio real.
- Sólo docs/v5 y CHANGELOG en dev por FF/CAS, Scope Lock y provenance V5 exact-SHA, A5 independiente; sin código, migraciones, hardware, pagos o QA/main/PROD.

## 2026-11-14 — V5 C08-03.M01.03: inventario de código y evidencias de membresías [Javier :10]
- Inventario documental de 12 archivos y blobs reales: Cliente, UsuarioEmpresa, Suscripción SaaS, servicios, controladores, Angular y stack .NET/MySQL; no se inventa módulo de gimnasio.
- Registra H01–H06, G01–G10 y P01–P06/N01–N12 FUTUROS NO_EJECUTADOS para seguridad A/B, expiración, suspensión, idempotencia y auditoría.
- Solo docs/v5 y CHANGELOG, Scope Lock y V5 exact-SHA, A5 pendiente de Task Javier distinta; sin runtime, migraciones, dispositivos, pagos, QA/main/PROD.

## 2026-11-14 — V5 C10-03.M01.05: criterios de aceptación de inventario y archivos [Javier :00]
- Establece AC01–AC14 con P01–P14/N01–N14 FUTUROS NO_EJECUTADOS y NFR01–NFR08; INVENTARIO_NO_VERIFICADO nunca se interpreta como cero.
- Si un inventario real completo acredita cero, omitir exportación, copia y checksum históricos; para datos positivos exigir manifiesto, integridad, retención y rollback.
- Sólo docs/v5 y CHANGELOG en dev, Scope Lock/V5 exact-SHA, A5 independiente pendiente; sin R2/DNS/cutover, secretos, migraciones ni QA/main/PROD.

## 2026-11-14 — V5 C10-03.M01.04: riesgos, supuestos y decisiones de archivos [Javier :00]
- Clasifica 14 riesgos POTENCIALES, ocho supuestos falsables y ocho decisiones PENDIENTES; no presume cero objetos históricos ni declara vulnerabilidades comprobadas.
- Define P01–P08/N01–N12 FUTUROS NO_EJECUTADOS para inventario conciliado, tenant A/B, MIME, TTL/replay, retención y rollback.
- Sólo docs/v5 y CHANGELOG en dev, Scope Lock/V5 exact-SHA y A5 de otra Task Javier; sin R2/DNS, migración real, secretos ni QA/main/PROD.

## 2026-11-14 — V5 C10-03.M01.02: actores y fronteras de inventario de archivos [Javier :00]
- Define A01–A14, interfaces candidatas I/O01–14 y F01–F09 para inventario de medios y almacenamiento futuro, manteniendo INVENTARIO_DESCONOCIDO distinto de cero acreditado.
- Documenta P01–P08/N01–N12 FUTUROS NO_EJECUTADOS para tenant A/B, MIME, TTL, retención, reconciliación y rollback; no conecta proveedores ni inventaría datos reales.
- Sólo docs/v5 y CHANGELOG en dev; Scope Lock/V5 exact-SHA, A5 pendiente de otra Task Javier; sin R2/DNS, migraciones, secretos ni QA/main/PROD.

## 2026-11-14 — V5 C08-02.M01.05: aceptación observable de mesas, QR y autoservicio [Javier :50]
- Define catorce pares P01–P14/N01–N14 FUTUROS NO_EJECUTADOS para QR falso, mesa ajena, menú, cocina, cierre, stock/facturas, replay, rollback y tenant A/B.
- Añade NFR01–NFR08 y requisitos de evidencia real .NET/MySQL/Angular/Playwright en fases futuras, sin afirmar PASS runtime ni resolver D01–D08 pendientes.
- Solo docs/v5 y CHANGELOG en dev, Scope Lock y procedencia V5 exact-SHA obligatorios, A5 por otra Task Javier, rollback git revert; sin pagos, eventos, migraciones ni QA/main/PROD.

## 2026-11-14 — V5 C10-03.M01.01: alcance e inventario real condicionado [Javier :00]
- Distingue INVENTARIO_DESCONOCIDO del cero acreditado y de inventario positivo; si cero real, omitir exportación, copia y checksum de archivos históricos sin inventar cantidades.
- Define NR01–NR14, P01–P08/N01–N12 FUTUROS NO_EJECUTADOS y fixtures no productivos para tenant A/B, MIME, retención y rollback; sin consultar objetos reales.
- Sólo docs/v5 y CHANGELOG en dev con Scope Lock/V5 exact-SHA, A5 pendiente de otra Task Javier; sin activar Cloudinary/R2, DNS, workers, secretos o QA/main/PROD.

## 2026-11-14 — V5 C08-02.M01.04: riesgos, supuestos y decisiones de mesas/QR [Javier :50]
- Documenta catorce riesgos R01–R14, ocho supuestos falsables S01–S08 y ocho decisiones D01–D08 PENDIENTES para mesa ajena, QR falso, tenant A/B, idempotencia, cierre y consistencia comercial.
- Define TP01–TP08 positivos/negativos FUTUROS NO_EJECUTADOS; hipótesis no significan bugs certificados ni PASS de .NET/Angular/MySQL/Playwright.
- Sólo docs/v5 y CHANGELOG en dev, Scope Lock y procedencia V5 exact-SHA obligatorios, A5 pendiente de otra Task Javier, rollback normal; sin pagos, eventos, migraciones, QA/main/PROD.

## 2026-11-14 — V5 C10-02.M01.05: aceptación observable R2 y dominios [Javier :00]
- Define AC01–AC14 con P01–P14 y N01–N14 FUTUROS NO_EJECUTADOS para tenant A/B, JWT/RBAC, firma/TTL, replay, caché, MIME, retención y rollback.
- Establece fixtures F01–F06, NFR01–NFR08 y trazabilidad a los cuatro documentos M01 previos; no ejecuta tests ni habilita R2/DNS/migración.
- Sólo docs/v5 y CHANGELOG en dev por fast-forward CAS; exige Scope Lock/V5 exact-SHA, A5 por otra Task Javier, sin runtime, secretos, QA/main/PROD.

## 2026-11-14 — V5 C08-02.M01.03: inventario de fuentes reales para mesas y QR [Javier :50]
- Inspecciona siete blobs exactos en Domain, Application, EF/repositorio y modelo Angular de pedido comercial, preparando la integración futura de restaurante sin asumir endpoint ni módulo completo.
- Registra G01–G10 brechas y P01–P06/N01–N12 FUTUROS NO_EJECUTADOS de QR falso, mesa ajena, tenant A/B, idempotencia, cocina/caja y cierre sin dobles cobros.
- Solo docs/v5 y CHANGELOG en dev, Scope Lock/V5 exact-SHA requerido, A5 pendiente de otra Task, git revert normal; ninguna prueba runtime declarada PASS ni implementación de eventos/QA/main/PROD.

## 2026-11-14 — V5 C08-02.M01.02: actores e interfaces de restaurante, mesas y QR [Javier :50]
- Define catorce actores A01–A14, catorce contratos I01–I14 y nueve fronteras F01–F09 para mesas, QR, menú, pedidos, cocina, caja y auditoría.
- Documenta P01–P06/N01–N06 FUTUROS NO_EJECUTADOS y denegación de mesa ajena, QR falso, replay, permisos JWT/RBAC y aislamiento tenant A/B; no se implementan API ni runtime.
- Sólo docs/v5 y CHANGELOG en dev, Scope Lock/V5 exact-SHA requeridos, A5 pendiente de otra Task Javier, rollback git revert; sin secretos, eventos, migraciones, QA/main/PROD.

## 2026-11-14 — V5 C08-02.M01.01: alcance y no-regresión de mesas, QR y autoservicio [Javier :40]
- Delimita mesas, QR, menú publicado, pedidos internos, cocina y caja con pertenencia tenant/sucursal y autorización server-side; no afirma implementación de POS ni QR real.
- Define NR01–NR14 y P01–P10/N01–N14 FUTUROS NO_EJECUTADOS para QR falso, mesa ajena, precio/cierre de pedido, idempotencia, privacidad, permisos y regresión ERP.
- Sólo Markdown en dev, claim CAS exclusivo, Scope Lock y procedencia V5 exact-SHA; A5 pendiente de otra Task, git revert normal, sin cobros, eventos, migraciones, secretos o QA/main/PROD.

## 2026-11-14 — V5 C08-01.M01.05: criterios observables de reservas y agenda [Javier :40]
- Define AC01–AC12 pares positivo/negativo FUTUROS NO_EJECUTADOS para citas, horarios, capacidad concurrente, UTC/IANA/DST, tenant A/B, JWT/RBAC, idempotencia y no regresión de inventario.
- Añade NFR01–NFR08, evidencia causal requerida para Domain, Application, MySQL, API y Angular/Playwright y NO_GO de seguridad; no reclama PASS runtime ni implementación de agenda.
- Solo Markdown en dev, CAS owner C08-01.M01.05, checks Scope Lock y procedencia V5 exact-SHA, A5 independiente pendiente, rollback git revert; sin cambios API/DB/events/QA/main/PROD.

## 2026-11-14 — V5 C08-01.M01.04: riesgos, supuestos y decisiones de reservas [Javier :40]
- Registra R01–R16 riesgos de doble ocupación, JWT/RBAC/tenant A/B, DST UTC/IANA, replay, estados, PII, accesibilidad y compatibilidad con inventario: brechas, no bugs certificados.
- Ordena S01–S08 supuestos, D01–D08 decisiones pendientes y TP01–TP08 escenarios positivos/negativos FUTUROS NO_EJECUTADOS; no crea citas, workers, colas ni migraciones.
- Cambio documental en dev con claim CAS exclusivo C08-01.M01.04, Scope Lock/V5 exact SHA y A5 pendiente de otra Task Javier; rollback git revert, sin eventos, secretos, QA/main/PROD.

## 2026-11-14 — V5 C10-02.M01.04: riesgos y decisiones R2/domains [Javier :30]
- Registra R01–R12 riesgos POTENCIALES, S01–S08 supuestos falsables y D01–D08 decisiones PENDIENTES sobre privacidad, tenant A/B, URL firmada, replay, caché, TTL, retención y rollback.
- Declara P01–P06/N01–N12 FUTUROS NO_EJECUTADOS; no hay fallo confirmado ni activación R2/DNS/cutover, secrets, Cloudinary o datos reales.
- Solo docs/v5 y CHANGELOG en dev, Scope Lock/V5 provenance exact-SHA, runtime NO_APLICA y A5 por otra Task Javier, sin QA/main/PROD.

## 2026-11-14 — V5 C10-02.M01.03: inventario verificable R2/domains [Javier :30]
- Inspecciona diez blobs Git exactos de interfaces/storage tenant, adaptadores Cloudinary, seguridad de imágenes, xUnit y contratos C10-02; separa evidencia presente de objetivo R2 futuro.
- Registra G01–G12 brechas sobre prefijos A/B, URLs firmadas, expiración, replay, CDN, lifecycle, datos, secrets y rollback; escenarios P/N FUTUROS NO_EJECUTADOS.
- Solo docs/v5 y CHANGELOG en dev; CI runtime NO_APLICA al Markdown, Scope Lock/V5 exact-SHA y A5 independiente pendiente, sin DNS/R2/PROD.

## 2026-11-14 — V5 C10-02.M01.02: actores e interfaces R2/domains [Javier :30]
- Define A01–A14 actores conceptuales, I/O01–12 entradas/salidas y F01–F08 fronteras de confianza para objetos privados, prefijos tenant, URL firmada, TTL, replay, caché y lifecycle.
- Exige JWT/RBAC/owner resuelto en backend y separación tenant A/B; P/N son FUTUROS NO_EJECUTADOS, no se implementaron DTO/API, R2, DNS, secretos, caché ni lifecycle.
- Solo docs/v5 y CHANGELOG en dev, Scope Lock y procedencia V5 exact-SHA; runtime NO_APLICA y A5 independiente pendiente.

## 2026-11-14 — V5 C10-02.M01.01: alcance preparatorio de R2 y dominios [Javier :30]
- Fija objetivo, no-alcance, NR01–NR14 y escenarios P01–P06/N01–N12 FUTUROS NO_EJECUTADOS para acceso privado, prefijos tenant, expiración, replay y lifecycle.
- Separa R2/DNS/cutover futuros de Cloudinary vigente; preserva permisos server-side JWT/RBAC, aislamiento A/B, retención, secretos y rollback sin crear infraestructura.
- Solo documentos en dev, Scope Lock y procedencia V5 exact-SHA; pruebas runtime NO_APLICA al Markdown y A5 independiente pendiente.

## 2026-11-14 — V5 C08-01.M01.03: inventario de fuentes y brechas de reservas [Javier :20]
- Contrasta seis blobs .NET de sucursales y reserva de inventario, rutas Angular y árboles actuales, distinguiendo claramente stock reservado de futuras citas con recursos, profesionales y horarios.
- Registra G01–G10 brechas de evidencia para tenant A/B, agenda, concurrencia, UTC/IANA/DST, permisos, idempotencia, UI, auditoría e integración; P/N son FUTUROS NO_EJECUTADOS, no defectos certificados.
- Sólo documentos en dev, CAS de C08-01.M01.03 y checks Scope Lock + V5 exact-SHA; A5 independiente pendiente, rollback git revert, sin cambios runtime/MySQL/migraciones, secretos ni QA/main/PROD.

## 2026-11-14 — V5 C08-01.M01.02: actores e interfaces de reservas y agenda [Javier :20]
- Define A01–A14 actores, I01–I14 interfaces conceptuales y F01–F07 fronteras para citas, horarios, recursos, personal y cancelaciones, con rol/sede y tenant A/B verificables en fases futuras.
- Establece P01–P08/N01–N14 FUTUROS NO_EJECUTADOS para concurrencia, UTC/IANA/DST, idempotencia, privacidad, seguridad JWT/RBAC y UX accesible; no acredita implementación ni pruebas runtime.
- Sólo docs/v5 y CHANGELOG en dev; DAG VALIDADO/SI y CAS owner J20, checks Scope Lock y V5 SHA exacto para cierre, A5 independiente pendiente, git revert normal; sin migraciones, compras, Tasks nuevas, QA/main/PROD.

## 2026-11-14 — V5 C07-04.M01.06: bloqueo, rollback y salida documental de pedidos/pagos [Javier :10]
- Formaliza B01–B14 bloqueos fail-closed, RLB01–RLB08 reversión y L01–L12 condiciones de liberación para cotización, pedido, factura, pagos, fiscalidad, stock, autorización tenant, idempotencia y auditoría.
- Coteja M01.01–05 LISTO con A5_PASS independientes, preservando D01–D08 PENDIENTES; no atribuye pruebas MySQL/HTTP/PSP ejecutadas ni autoriza cobros, migraciones o M02 automáticamente.
- Solo docs/v5 y CHANGELOG en dev, checks Scope Lock/V5 exact-SHA, BITACORA/readback y A5 pendiente de otra Task Javier; sin QA/main/PROD ni eventos.

## 2026-11-14 — V5 C08-03.M01.02: actores e interfaces de membresías y acceso [Javier :10]
- Describe A01–A14 actores/superficies e I01–I14 contratos FUTUROS de perfil, planes, altas, verificación de acceso, check-in, suspensión, renovación, auditoría, dispositivo y conciliación.
- Define F01–F07 límites API/Application/Domain/Infrastructure con tenant A/B, JWT/RBAC, UTC/IANA, DST, idempotencia, replay y privacidad, además de P01–P06 y N01–N15 FUTUROS NO_EJECUTADOS.
- Sólo docs/v5 y CHANGELOG en dev, Scope Lock y V5 exact-SHA, BITACORA/readback, A5 independiente pendiente y git revert normal; sin código, lectores, pagos, webhooks, DB, QA/main/PROD.

## 2026-11-14 — V5 C08-03.M01.01: alcance y no-regresión de membresías de gimnasio [Javier :10]
- Define membresías y acceso de gimnasio como contrato FUTURO, distinguiendo membresía empresarial JWT/RBAC de derecho comercial, con NR01–NR14 sobre tenant A/B, vigencia UTC/IANA, DST, suspensión, idempotencia y auditoría.
- Especifica P01–P06 y N01–N14 FUTUROS NO_EJECUTADOS, no una implementación ni autorización para lectores, QR, pasarelas de pago, bases de datos o producción.
- Sólo docs/v5 y CHANGELOG en dev con Scope Lock/V5 provenance exact-SHA, BITACORA y readback; A5 independiente pendiente y rollback git revert normal, sin QA/main/PROD.

## 2026-11-14 — V5 C08-01.M01.01: alcance y no-regresion reservas y agenda [Javier :00]
- Delimita citas, horarios, disponibilidad, personal, recursos, cancelaciones, zonas IANA y conflictos de agenda con doce invariantes NR01–NR12; no declara la agenda implementada.
- Define P01–P08 y N01–N12 FUTUROS NO_EJECUTADOS, incluyendo concurrencia, tenant A/B, DST, idempotencia, privacidad y accesibilidad móvil; ninguna prueba runtime pasa por redactarlos.
- Solo docs/v5 y CHANGELOG en dev, 60/60 predecesores documentales, DAG VALIDADO/SI; Scope Lock/V5 provenance exact SHA obligatorios, A5 independiente pendiente, sin migraciones, cobros, workers, secretos ni QA/main/PROD.

## 2026-11-14 — V5 C07-04.M01.05: criterios de aceptación pedidos, pagos y documentos [Javier :50]
- Define 14 escenarios positivos P01–P14 y 14 negativos N01–N14 FUTUROS NO_EJECUTADOS para tenant, idempotencia, stock, facturación, pago, cambio, anulación, PSP, fiscalidad, exportación y reversión.
- Traza cuatro contratos anteriores M01.01–04 y blobs reales de PedidoVenta, FacturaPago y controladores; las ocho decisiones D01–D08 permanecen PENDIENTES y no se declara certificación transaccional.
- Sólo docs/v5 y CHANGELOG en dev; DAG documental VALIDADO/SI, Scope Lock/V5 exact-SHA, A5 independiente pendiente; sin migraciones, cobros, secrets, workers ni QA/main/PROD.

## 2026-11-14 — V5 C07-03.M01.05: criterios de aceptacion inventario/compras [Javier :00]
- Define 14 pares P01–P14/N01–N14 para almacenes, reservas, ordenes, recepciones, transferencias, conteos, costeo, reconciliacion y UI; TODOS FUTUROS NO_EJECUTADOS.
- Traza contratos M01.01–04, 12 riesgos R01–R12 y decisiones D01–D08 pendientes; exige MySQL efimero, A/B, RBAC y Playwright causal en futuras tareas de implementacion, no los certifica hoy.
- Solo docs/v5 y CHANGELOG en dev, DAG preparatorio VALIDADO/SI y Scope Lock/V5 provenance exact-SHA; A5 independiente pendiente, sin codigo, stock real, migraciones, secretos, workers ni QA/main/PROD.

## 2026-11-14 — V5 C07-03.M01.04: riesgos y decisiones inventario/compras [Javier :00]
- Identifica doce riesgos R01–R12, ocho supuestos falsables S01–S08 y ocho decisiones D01–D08 PENDIENTES sobre reserva, compras, recepción, transferencias, conciliación y costo; no hay defectos certificados.
- Contrasta M01.01–03 y referencias a veinte fuentes/5 pruebas, define P01–P08 y N01–N14 FUTUROS NO_EJECUTADOS; sin afirmar ejecución de MySQL, API, seguridad A/B o Playwright.
- Solo docs/v5 y CHANGELOG en dev; DAG VALIDADO/SI preparación 65/65 prerequisitos LISTO, Scope Lock/V5 provenance exact SHA requeridos, A5 independiente pendiente; sin migración, cambios stock, workers, secrets ni QA/main/PROD.

## 2026-11-14 — V5 C07-02.M01.05: aceptación observable storefront y checkout [Javier :50]
- Define 14 positivos P01–P14 y 14 negativos N01–N14 FUTUROS NO_EJECUTADOS sobre binding host, catálogo/tenant A/B, stock, precio, carrito, pedido/idempotencia, HMAC/nonce, PSP, recibo, móvil, caché y privacidad.
- Traza contratos M01.01–03 y siete blobs de código/documentos; M01.01 sigue VALIDANDO por CI CANCELLED y M01.04 no está aprobado, sin inferir reserva, pago ni HMAC operativo.
- Sólo docs/v5 y CHANGELOG en dev, DAG documental VALIDADO/SI, Scope Lock/V5 provenance exact-SHA y A5 independiente; sin endpoints, migraciones, workers, secrets, pagos, QA/main/PROD.

## 2026-11-14 — V5 C07-02.M01.04: riesgos y decisiones storefront/checkout [Javier :00]
- Registra doce riesgos R01–R12, ocho supuestos falsables S01–S08 y ocho decisiones D01–D08 PENDIENTES sobre host, tenant, cache, HMAC/replay, pagos y móvil; no certifica vulnerabilidades.
- Contrasta contratos M01.01–02 y catorce blobs API/Application/Domain/Angular; P01–P06/N01–N12 son FUTUROS NO_EJECUTADOS, no tests runtime.
- Sólo docs/v5 y CHANGELOG en dev; Scope Lock y V5 provenance exact-SHA requeridos, A5 independiente pendiente, sin código, migraciones, secrets, pagos, workers ni QA/main/PROD.

## 2026-11-14 — V5 C07-02.M01.03: inventario causal de storefront y checkout [Javier :50]
- Contrasta doce blobs Git reales de contratos M01.01/M01.02 y backend/frontend storefront, binding, catálogo, identidad y SEO; registrar fuentes no demuestra certificación live.
- Identifica brechas G01–G10 para host/proxy, tenant A/B, caché, checkout, pedido/idempotencia, HMAC/replay, PSP y accesibilidad; P01–P06/N01–N12 son FUTUROS NO_EJECUTADOS.
- Solo docs/v5 y CHANGELOG en dev; DAG documental VALIDADO/SI, Scope Lock/V5 exact-SHA y A5 independiente pendientes; M01.01 sigue VALIDANDO por CI CANCELLED, sin pagos, migraciones, secretos ni QA/main/PROD.

## 2026-11-14 — V5 C07-03.M01.03: inventario causal ERP inventario/compras [Javier :40]
- Inventaría veinte blobs Git de Domain/API/Application/Infrastructure y cinco pruebas existentes para compras, reservas, transferencias, conteos, costeo, reportes, aislamiento y concurrencia; no se ejecutaron tests.
- Identifica diez brechas G01–G10 para tenant A/B, NULL legado, stock concurrente, idempotencia, transferencias, costo/censura y rollback; son hipótesis, no fallos certificados.
- Sólo docs/v5 y CHANGELOG en dev, DAG VALIDADO/SI y Scope Lock/V5 exact-SHA requeridos; A5 independiente pendiente, sin código, migraciones, secrets, workers, QA/main/PROD.

## 2026-11-14 — V5 C07-03.M01.02: actores e interfaces de inventario/compras [Javier :40]
- Identifica 12 actores A01–A12, 12 interfaces I01–I12 y 10 fronteras B01–B10 de ERP, compras, reservas, conciliación y costeo; no cambia roles, stock ni código.
- Contrasta diez blobs reales API y distingue dos superficies pendientes de auditoría M01.03; P01–P06 y N01–N10 son FUTUROS NO_EJECUTADOS, no pruebas de aislamiento HTTP/MySQL.
- Sólo docs/v5 y CHANGELOG en dev, DAG VALIDADO/SI, Scope Lock/V5 exact-SHA, A5 independiente pendiente; sin migraciones, pagos, workers ni QA/main/PROD.

## 2026-11-14 — V5 C07-03.M01.01: alcance y no-regresión inventario, compras y operaciones [Javier :40]
- Define alcance documental y doce invariantes NR01–NR12 para tenant/sucursal/almacén, reservas, transferencias, compras idempotentes, conteos, costeo histórico y conciliación; no cambia existencias ni código.
- Contrasta diez blobs Git de Domain/API de inventario y compras; P01–P06 y N01–N08 son oráculos FUTUROS NO_EJECUTADOS, no certificación .NET/MySQL, RBAC HTTP o aislamiento A/B runtime.
- Sólo docs/v5 y CHANGELOG en dev, DAG VALIDADO/SI, Scope Lock y V5 provenance exact-SHA; A5 independiente pendiente, sin migración, workers, pagos, QA/main/PROD.

## 2026-11-14 — V5 C07-04.M01.04: riesgos y decisiones de pedidos y pagos [Javier :30]
- Registra doce riesgos R01–R12, ocho supuestos falsables S01–S08 y ocho decisiones D01–D08 PENDIENTES sobre tenant, idempotencia, pagos/cambio, fiscalidad, snapshots y rollback; no identifica fallos confirmados.
- Traza contratos M01.01–03 y cuatro blobs actuales Domain/API; P01–P06/N01–N14 son oráculos FUTUROS NO_EJECUTADOS y no validación runtime de JWT/MySQL/proveedor.
- Solo docs/v5 y CHANGELOG en dev; DAG VALIDADO/SI, Scope Lock y V5 provenance exact-SHA, A5 independiente; sin implementación, migraciones, pagos live, secrets, workers ni QA/main/PROD.

## 2026-11-14 — V5 C07-04.M01.03: inventario de contratos de pedidos y pagos [Javier :30]
- Contrasta doce blobs Git de Domain y controladores API para cotizaciones, pedidos, ventas, facturas, pagos online y fiscalidad, sin declarar integración universal ya ejecutada.
- Registra diez brechas G01–G10 de prueba por tenant, idempotencia durable, conciliación, snapshots, proveedor, PDF y rollback; P01–P06/N01–N12 son oráculos FUTUROS NO_EJECUTADOS.
- Solo docs/v5 y CHANGELOG en dev; DAG preparatorio VALIDADO/SI, Scope Lock y V5 provenance exact-SHA, A5 independiente; sin código, migraciones, secretos, pagos reales, eventos, workers ni QA/main/PROD.

## 2026-11-14 — V5 C07-02.M01.02: actores e interfaces de storefront/checkout [Javier :20]
- Define 12 actores A01–A12, 12 interfaces I01–I12 y 10 fronteras B01–B10 de host/tenant, carrito, precio server-side, HMAC/replay futuro, accesibilidad y pagos no implementados.
- Contrasta seis blobs Git de API, Application, Domain, Angular y contrato M01.01; P01–P06/N01–N12 son FUTUROS NO_EJECUTADOS, sin afirmar reserva, pedido ERP o PSP.
- Solo docs/v5 y CHANGELOG en dev; DAG 70/70 preparaciones LISTO, Scope Lock/V5 exact-SHA, A0–A4 documentales y A5 independiente; sin nuevos servicios, secretos, QA/main/PROD.

## 2026-11-14 — V5 C07-04.M01.02: actores y fronteras de pedidos, pagos y documentos [Javier :30]
- Inventaría doce actores A01–A12, doce interfaces I01–I12 y ocho fronteras B01–B08 de cotizaciones, pedidos, ventas, facturas, pagos online y emisión fiscal, con alcance por tenant y permisos backend.
- Contrasta seis controladores y blobs Git; P01–P08/N01–N14 son oráculos FUTUROS NO_EJECUTADOS, no ejecución .NET/EF/MySQL, pagos, fiscal ni autorización productiva.
- Solo docs/v5 y CHANGELOG en dev; DAG VALIDADO/SI, Scope Lock y V5 provenance en SHA exacto, A5 de otra Task Javier; sin API/Angular, JWT/RBAC, migraciones, secretos, trabajadores ni QA/main/PROD.

## 2026-11-14 — V5 C07-04.M01.01: alcance y no-regresión de pedidos, pagos y documentos [Javier :30]
- Define frontera documental de cotizaciones, pedidos, ventas, facturas, cobros, caja y comprobantes; NR01–NR12 de aislamiento tenant, EmpresaId NULL, idempotencia, snapshots, conciliación, autorización, reversión y concurrencia.
- Contrasta seis blobs reales de Domain/API; P01–P06 y N01–N14 son oráculos FUTUROS NO_EJECUTADOS, sin afirmar certificación HTTP/EF/MySQL, pagos, fiscalidad ni stock real.
- Solo docs/v5 y CHANGELOG en dev; DAG preparatorio VALIDADO/SI, Scope Lock y V5 provenance exact-SHA, A5 independiente; sin endpoints, JWT/RBAC, migraciones, workers, secretos, QA/main/PROD.

## 2026-11-14 — V5 C07-02.M01.01: alcance y no regresión de storefront/checkout [Javier :20]
- Fija alcance y doce invariantes NR01–NR12 para binding host/tenant, carrito, validación sin reserva, precios server-side, HMAC/replay futuro, confirmaciones idempotentes y uso móvil; no implementa cobros ni pedidos ERP.
- Contrasta cinco blobs actuales de TiendaController, TiendaBindingController, PublicStoreBinding, CatalogoPublicoService y storefront.models; define P01–P12/N01–N12 FUTUROS NO_EJECUTADOS.
- Sólo docs/v5 y CHANGELOG en dev, 70/70 prerequisitos M01 de preparación LISTO, Scope Lock/V5 exact-SHA, A5 independiente. Sin MySQL, UI nueva, APIs, pagos, DNS ni QA/main/PROD.

## 2026-11-14 — V5 C07-01.M01.05: aceptación observable del catálogo universal [Javier :20]
- Define 14 pares P01–P14/N01–N14 de aceptación futura para producto, variante, NULL histórico, categoría, tributos, snapshots, stock, sucursal, servicios, tienda, DTO, caché, revocación y SKU/media; todos FUTUROS NO_EJECUTADOS.
- Traza las matrices M01.01–04, hipótesis R01–R12 y decisiones D01–D08 PENDIENTES; separa aislamiento A/B, permisos JWT/RBAC y compatibilidad ERP de cualquier certificación runtime.
- Sólo docs/v5 y CHANGELOG en dev; DAG preparatorio 60/60 LISTO, Scope Lock/V5 exact-SHA, A0–A4 documentales y A5 independiente; sin nuevas APIs, migraciones, workers ni QA/main/PROD.

## 2026-11-14 — V5 C07-01.M01.04: riesgos y decisiones de catálogo universal [Javier :00]
- Registra doce riesgos R01–R12, ocho supuestos falsables S01–S08 y ocho decisiones D01–D08 PENDIENTES; no declara vulnerabilidades ni autoriza cambios estructurales.
- Traza los contratos M01.01–03 con blobs Git, casos P01–P06/N01–N12 FUTUROS NO_EJECUTADOS y especial cautela frente a EmpresaId NULL, categorías, impuestos, inventario y caché A/B.
- Solo docs/v5 y CHANGELOG en dev; prerequisitos 60/60 LISTO y DAG VALIDADO/SI para preparación, Scope Lock/V5 exact SHA y A5 independiente; sin runtime, migraciones, QA/main/PROD.

## 2026-11-14 — V5 C07-01.M01.03: inventario causal de contratos y código de catálogo [Javier :00]
- Contrasta diecisiete blobs Git de Domain, Application, Infrastructure, API y Angular; identifica rutas reales de productos, variantes, impuestos y tienda pública sin afirmar implementación universal.
- Registra brechas G01–G08 de aislamiento/ownership, NULL legado, caché pública, impuesto y factura; P01–P06/N01–N12 son FUTUROS NO_EJECUTADOS, no pruebas runtime.
- Sólo docs/v5 y CHANGELOG en dev; DAG 60/60 prerequisitos LISTO, Scope Lock/V5 provenance exact-SHA, A5 independiente pendiente, sin nuevos permisos, migraciones, endpoints, eventos ni QA/main/PROD.

## 2026-11-14 — V5 C07-01.M01.02: actores e interfaces del catalogo universal [Javier :10]
- Define 12 actores A01–A12, 12 interfaces I01–I12 y 10 fronteras B01–B10 de ERP, tienda, variantes, precios, stock, impuestos y servicios futuros, sin crear rutas ni permisos.
- Coteja ocho fuentes Git; P01–P08/N01–N12 son oraculos FUTUROS NO_EJECUTADOS. No atribuye cierre a M01.01 de otro autor ni universalidad a entidades existentes.
- Solo docs/v5 y CHANGELOG dev; A0–A4 documentales, Scope Lock/procedencia exact SHA, A5 de otra Task Javier, sin migraciones, workers, secretos, QA/main/PROD.

## 2026-11-14 — V5 C07-01.M01.01: alcance y no-regresión de catálogo universal [Javier :00]
- Define frontera futura de productos, servicios, variantes, categorías, impuestos, precios, ownership tenant y compatibilidad ERP; doce invariantes con A/B fail-closed.
- Contrasta seis blobs existentes de Domain y declara los escenarios P01–P06/N01–N12 como FUTUROS NO_EJECUTADOS; no afirma aislamiento perfecto ni un nuevo catálogo operativo.
- Sólo documentación y CHANGELOG en dev; DAG 60/60 prerrequisitos LISTO, A0–A4 documentales, Scope Lock/provenance exact-SHA, A5 de otra Task pendiente; sin JWT, MySQL, migraciones, eventos, QA/main/PROD.

## 2026-11-14 — V5 C06-04.M01.05: aceptación verificable del portal de cliente [Javier :10]
- Establece catorce escenarios positivos P01–P14 y catorce negativos N01–N14 FUTUROS NO_EJECUTADOS, distinguiendo cuenta de tienda existente y capacidades adicionales aún no certificadas.
- Traza NR01–NR12, A/I/B, brechas y decisiones D01–D08 PENDIENTES; exige aislamiento por tenant y objeto, recuperación antienumeración, documentos TTL, consentimientos y accesibilidad sin afirmar pruebas runtime.
- Solo documento docs/v5 y CHANGELOG en dev; requiere Scope Lock y procedencia exact-SHA, A0–A4 documentales, readbacks y A5 independiente. Sin JWT/MySQL/Angular, eventos, workers, SMTP, secretos, QA/main/PROD.

## 2026-11-14 — V5 C06-03.M01.05: criterios de aceptación editoriales y mapas [Javier :00]
- Define 14 escenarios positivos P01–P14 y 14 negativos N01–N14 futuros con oráculos API/DB/Angular/SEO/seguridad por tenant, host, RBAC, sanitización, mapas opcionales y accesibilidad; NO_EJECUTADOS.
- Traza riesgos R01–R12, contratos M01.01–04, cache/preview y regresión tienda/ERP a evidencias y pruebas futuras sin declarar CMS, mapas, permisos o proveedor implementados.
- Solo docs/v5 y CHANGELOG en dev; A0–A4 documentales con Scope Lock/provenance exact-SHA, readbacks y A5 pendiente de otra Task Javier; sin migraciones, servicios nuevos, pagos, QA/main/PROD.

## 2026-11-14 — V5 C06-03.M01.04: riesgos y decisiones de sitios editoriales y mapas [Javier :00]
- Documenta doce riesgos potenciales R01–R12, ocho supuestos falsables S01–S08 y ocho decisiones D01–D08 PENDIENTES; evita afirmar vulnerabilidades, permisos o implementación de CMS/mapas.
- Coteja contratos M01.01–03 y fuentes reales SEO/storefront/sitemap con blobs Git; P01–P06/N01–N12 son escenarios FUTUROS NO_EJECUTADOS, no CI de producto.
- Solo docs/v5 y CHANGELOG en dev; exige Scope Lock y provenance V5 exact SHA, readback PLAN/COLA/BITACORA y A5 independiente; sin JWT/MySQL/Angular, eventos, proveedores, secretos, QA/main/PROD.

## 2026-11-14 — V5 C06-03.M01.03: inventario causal de fuentes editoriales y mapas [Javier :50]
- Contrasta doce fuentes V5, Angular/storefront, autorización solicitada y SEO/robots/sitemap por blobs Git y registra ocho brechas pendientes G01–G08 de sitios editoriales y mapas por tenant, sin inventar implementación.
- Los oráculos futuros P01–P06/N01–N12 son NO_EJECUTADOS; no se presenta inventario selectivo como auditoría backend exhaustiva ni como certificación live.
- Solo docs/v5 y CHANGELOG en dev, Scope Lock y provenance exigidos SHA exacto, A5 distinto pendiente; sin endpoints, migraciones, mapas activados, secretos, DNS ni QA/main/PROD.

## 2026-11-14 — V5 C06-03.M01.02: actores, interfaces y fronteras editoriales [Javier :50]
- Registra doce actores A01–A12, doce interfaces I01–I12 y ocho fronteras B01–B08 para contenido público, ubicación, horarios y mapas de empresa, sin atribuir funciones nuevas al código actual.
- Contrasta M01.01 y fuentes reales Angular/storefront/SEO/guards; los oráculos P01–P06/N01–N12 son FUTUROS NO_EJECUTADOS, no prueba runtime ni autorización tenant concedida.
- Solo docs/v5 y CHANGELOG en dev, Scope Lock/CHG exact-SHA y A5 de otra Task obligatorios; sin API, JWT, MySQL, geocodificación, workers, secretos, DNS ni QA/main/PROD.

## 2026-11-14 — V5 C06-03.M01.01: alcance y no-regresión de sitios editoriales y mapas [Javier :50]
- Fija objetivo y exclusiones de páginas, bloques, ubicación, horarios, contacto y mapas futuros por empresa, con doce invariantes de ownership, moderación, sanitización, accesibilidad, SEO y caché segura.
- Ocho fuentes Git del frontend, storefront, SEO y landing tenant contrastadas por blob; escenarios positivos P01–P06 y negativos N01–N12 son FUTUROS NO_EJECUTADOS, no runtime certificado.
- Solo documentación y CHANGELOG en dev; sin implementación de mapas, API, base de datos, JWT/RBAC, dominios, pagos ni despliegue. A5 independiente pendiente, Scope Lock y provenance exigidos sobre el SHA exacto.

## 2026-11-14 — V5 C06-04.M01.04: riesgos y decisiones pendientes del portal de cliente [Javier :40]
- Registra R01–R12 riesgos potenciales, S01–S08 supuestos falsables y D01–D08 decisiones PENDIENTES sin otorgar aprobaciones, privilegios ni cambios de arquitectura.
- Coteja M01.01–03 y sesión storefront vigente; P01–P06/N01–N14 son oráculos FUTUROS NO_EJECUTADOS, no hallazgos de vulnerabilidad ni pruebas de portal completado.
- Solo docs/v5 y CHANGELOG dev, Scope Lock/V5 exact SHA, A5 independiente pendiente; sin JWT/MySQL/Angular, SMTP, workers, eventos, secretos, QA/main/PROD.

## 2026-11-14 — V5 C06-04.M01.03: inventario causal del portal de cliente [Javier :40]
- Contrasta 16 blobs reales Angular/storefront/ERP: ya existe cuenta de tienda con login, registro, direcciones, favoritos, pedidos y notificaciones; separa esas interfaces de la certificación integral de portal C06-04.
- Registra G01–G12 brechas de pruebas/contratos no certificadas, y P01–P06/N01–N14 como oráculos FUTUROS NO_EJECUTADOS; no imputa vulnerabilidades ni ausencia global sin evidencia.
- Dos Markdown en dev, Scope Lock/V5 exact SHA y A5 independiente; sin backend/Angular/MySQL, sesiones, trabajadores, eventos, secretos o QA/main/PROD.

## 2026-11-14 — V5 C06-02.M01.05: aceptación de landing por tenant [Javier :30]
- Establece AC01–AC14 positivos y N01–N14 negativos FUTUROS NO_EJECUTADOS para host/slug, JWT/RBAC, preview noindex, tenant A/B, SEO, cache, publicación CAS y rollback.
- Contrasta M01.01–M01.04 y blobs de router/interceptor/seo-utils; D01–D08 siguen PENDIENTES. No declara pruebas Playwright, HTTP/.NET, MySQL, host binding, DNS/TLS ni seguridad runtime.
- Dos Markdown en dev con Scope Lock/V5 exact-SHA, PLAN/COLA/BITACORA y A5 independiente; sin cambios de código, eventos, workers, secretos ni QA/main/PROD.

## 2026-11-14 — V5 C06-02.M01.04: riesgos y decisiones landing tenant [Javier :30]
- Documenta R01–R12 riesgos potenciales, S01–S08 supuestos falsables y D01–D08 decisiones PENDIENTES para host/tenant, preview, noindex, SEO, cache, RBAC, publicación, rollback y DNS/TLS.
- Contrasta tres documentos de fase y blobs de router, storefront, interceptor y seo-utils; P01–P06/N01–N12 son oráculos FUTUROS NO_EJECUTADOS, no vulnerabilidades ni tests runtime confirmados.
- Solo dos Markdown en dev con Scope Lock y V5 exact-SHA, readback y A5 independiente; sin cambios API/Angular/MySQL/JWT, DNS, secrets, eventos ni QA/main/PROD.

## 2026-11-14 — V5 C06-04.M01.02: actores e interfaces del portal de cliente [Javier :40]
- Inventaría A01–A12 actores, I01–I12 fronteras funcionales y B01–B08 controles fail-closed de portal futuro: consumidor separado de ERP, tenant/ownership, recuperación, pedidos, reservas, membresías, documentos y privacidad.
- Coteja siete blobs de código y la entrega M01.01; escenarios P01–P06/N01–N12 FUTUROS NO_EJECUTADOS, no prueba de portal o API live.
- Solo dos Markdown dev, Scope Lock y V5 provenance exact SHA, A5 independiente pendiente; sin Angular/.NET/MySQL runtime, SMTP, eventos, secretos ni QA/main/PROD.

## 2026-11-14 — V5 C06-04.M01.01: alcance y no-regresión del portal de cliente [Javier :40]
- Define frontera de cuenta cliente frente a ERP y doce invariantes NR01–NR12 de autenticación, ownership tenant/cliente, recuperación, PII, pedidos, reservas, documentos y preferencias.
- Contrasta ocho blobs reales Angular/modelos y C06-02; P01–P06/N01–N12 son criterios FUTUROS NO_EJECUTADOS, no portal o API implementados.
- Solo dos Markdown dev; Scope Lock y V5 provenance exact SHA, A5 pendiente de otro Javier, rollback normal; sin JWT/MySQL/UI, eventos, secretos, QA/main/PROD.

## 2026-11-14 — V5 C06-02.M01.03: inventario de fuentes de landing tenant [Javier :30]
- Verifica 14 archivos y blobs Git de contratos, Angular, guards/interceptor, storefront, robots/sitemap y SEO; distingue tienda actual de host binding y publicación FUTUROS.
- Registra G01–G10 brechas no certificadas y P01–P06/N01–N12 oráculos FUTUROS NO_EJECUTADOS; hostFromRequest usa x-forwarded-host/host sin afirmar vulnerabilidad explotable.
- Solo dos Markdown en dev; Scope Lock/V5 exact-SHA, PLAN/COLA/BITACORA y A5 independiente; sin Angular/API/MySQL, JWT/RBAC, DNS/TLS, CDN, eventos, secrets ni QA/main/PROD.

## 2026-11-14 — V5 C06-02.M01.02: actores y fronteras de landing por tenant [Javier :30]
- Identifica A01–A12 actores e I01–I12 interfaces futuras de host/slug, draft/preview, editor, RBAC, publicación, SEO, CDN y rollback con B01–B08 fronteras fail-closed.
- Contrasta cinco blobs reales Angular/guards/storefront y el alcance M01.01; P01–P06/N01–N12 son oráculos FUTUROS NO_EJECUTADOS, no bindings, DNS/TLS, API ni permisos implementados.
- Solo dos archivos Markdown en dev; Scope Lock/V5 de SHA exacto, readback y A5 independiente; ningún cambio JWT/RBAC/MySQL, UI, secrets, workers, eventos, QA/main/PROD.

## 2026-11-14 — V5 C06-02.M01.01: alcance y no-regresión de landing por tenant [Javier :20]
- Define objetivo de landing pública administrable por empresa y NR01–NR12 sobre host binding seguro, slug, preview, noindex, branding, tenant A/B, cache y publicación versionada.
- Contrasta router, storefront, guards, interceptor y contratos V5; P01–P06/N01–N12 son oráculos FUTUROS NO_EJECUTADOS, no host/TLS/DNS implementado ni aprobado.
- Solo Markdown y CHANGELOG dev, Scope Lock/procedencia exact-SHA y A5 de otra Task; sin endpoints, MySQL, Angular, secrets, dominios, eventos o QA/main/PROD.

## 2026-11-14 — V5 C06-01.M01.05: aceptación observable de sitio público SaaS [Javier :20]
- Define 14 criterios AC01–AC14 y 14 negativas N01–N14 FUTURAS NO_EJECUTADAS para landing, precios aprobados, SEO, consentimiento, rate limit, demo, tenant A/B, accesibilidad y rollback.
- Coteja NR01–NR12, A01–A12/I01–I12, B01–B08 y D01–D08 PENDIENTES; no afirma pruebas Playwright, HTTP, .NET, SEO, aprobación comercial o sitio publicado.
- Solo dos Markdown dev con Scope Lock/procedencia V5 exact-SHA y A5 independiente; sin código UI/API, SMTP, MySQL, secretos, eventos ni QA/main/PROD.

## 2026-11-14 — V5 C06-01.M01.04: riesgos y decisiones de sitio público SaaS [Javier :20]
- Registra R01–R12 riesgos potenciales, S01–S08 supuestos falsables y D01–D08 decisiones PENDIENTES sobre marketing, SEO, privacidad, contacto y separación tenant.
- Contrasta contratos M01.01/M01.02/M01.03 y código Angular/guards; oráculos P01–P06/N01–N12 FUTUROS NO_EJECUTADOS; no se atribuye incidencia, cumplimiento web ni permiso comercial.
- Solo dos Markdown en dev; Scope Lock/V5 exact-SHA y A5 de otra Task Javier; sin API/Angular/MySQL/SMTP, secrets, robots publicado, QA/main/PROD o eventos.

## 2026-11-14 — V5 C06-01.M01.03: inventario causal de sitio público SaaS [Javier :20]
- Inspecciona doce fuentes y hashes Git para contratos M01, router Angular, guards, interceptor, storefront y feedback; distingue tienda existente de landing FUTURA.
- Declara brechas B01–B08 y oráculos P01–P06/N01–N12 FUTUROS NO_EJECUTADOS para SEO, consentimiento, contacto, tenant A/B, demo, accesibilidad y rollback.
- Solo dos Markdown dev; exigir Scope Lock y procedencia V5 del SHA publicado y A5 independiente; sin nuevas rutas, API/MySQL/SMTP, secretos, datos reales, eventos o QA/main/PROD.

## 2026-11-14 — V5 C05-04.M01.06: gate y rollback documental de ligereza [Javier :10]
- Cierra contractualmente M01 de C05-04 tras cinco antecedentes LISTO y A5 independientes (J00/J10), con diez puertas G01–G10 para Scope Lock, seguridad, accesibilidad, medición y privacidad.
- Delimita P01–P06/N01–N14 FUTUROS NO_EJECUTADOS; budgets Angular 650/750 kB son configuración y no benchmarks ni CWV aprobados, D01–D08 permanecen PENDIENTES.
- Solo documento y CHANGELOG en dev, CI Scope Lock/V5 exact-SHA, PLAN/COLA/BITACORA y A5 propio pendiente de otra Task Javier; no cambios Angular/API, QA/main/PROD, workers, secretos o eventos.

## 2026-11-14 — V5 C06-01.M01.02: actores y fronteras sitio público SaaS [Javier :10]
- Define A01–A12 actores e I01–I12 interfaces FUTURAS de landing, planes, sectores, demo, contacto, legal, SEO, robots, navegación y releases; no implementa rutas ni publica precios.
- Ocho fronteras F01–F08 y oráculos P01–P06/N01–N14 NO_EJECUTADOS para JWT/RBAC/tenant A/B, PII, consentimiento, anti-spam, metadatos, accesibilidad y rollback.
- Solo docs/v5 y CHANGELOG, A0–A4 documentales con Scope Lock/V5 exact SHA + readback; A5 independiente; sin Angular/API/MySQL/SMTP live, tracking, pagos, secrets ni QA/main/PROD.

## 2026-11-14 — V5 C06-01.M01.01: alcance y no-regresión de sitio público SaaS [Javier :10]
- Delimita propuesta de valor, planes/sectores, demo, contacto, legal y SEO con doce invariantes NR01–NR12; la ruta raíz actual redirige a login y no prueba una landing comercial terminada.
- Define P01–P06/N01–N14 como pruebas FUTURAS NO_EJECUTADAS de privacidad, anti-spam, rutas públicas/privadas, sitemap, tenant A/B, accesibilidad y no inventar ofertas ni metadatos aprobados.
- Solo docs/v5 y CHANGELOG en dev; Scope Lock/V5 exact-SHA, PLAN/COLA/BITACORA y A5 independiente; sin UI/API/SMTP/planes reales, secretos, trabajadores, QA/main/PROD ni eventos.

## 2026-11-14 — V5 C10-01.M01.05: aceptación de storage neutral [Javier :10]
- Define AC01–AC16 y N01–N16 de pruebas FUTURAS NO_EJECUTADAS para storage provider-neutral: JWT/RBAC, tenant A/B, MIME real, tamaños, checksum, TTL, errores y rollback.
- Alinea NR01–NR12, I01–I12, R01–R12, S01–S08 y D01–D08 pendientes; no atribuye migración, Cloudinary/R2, datos reales, xUnit/Playwright ni aprobación de proveedores.
- Solo Markdown y CHANGELOG en dev; A0–A4 documental, Scope Lock/V5 exact-SHA y BITACORA/PLAN/COLA readback; A5 de otra Task Javier, sin QA/main/PROD, workers, eventos ni secretos.

## 2026-11-14 — V5 C05-04.M01.05: aceptación observable de ligereza [Javier :00]
- Define AC01–AC14/N01–N14 como oráculos FUTUROS NO_EJECUTADOS para bundle, Core Web Vitals, preload, HTTP, A/B, JWT/RBAC, ARIA, móvil 320px, privacidad y rollback.
- Preserva R01–R12, S01–S08 y D01–D08 PENDIENTE de M01.04; no aprueba umbrales CWV ni presupuestos nuevos, coteja ocho blobs de documentación/Angular.
- Solo dos Markdown en dev, Scope Lock y V5 provenance exact-SHA, A5 distinta y rollback normal; sin código, métricas runtime, eventos, QA/main/PROD.

## 2026-11-14 — V5 C10-01.M01.04: riesgos y decisiones de storage neutral [Javier :10]
- Inventaría R01–R12 riesgos potenciales, S01–S08 supuestos falsables y D01–D08 decisiones PENDIENTES de ownership, MIME, tamaño, checksum, TTL, rollback y proveedores; no afirma incidentes probados.
- Especifica P01–P05/N01–N12 como escenarios FUTUROS NO_EJECUTADOS, exige JWT/RBAC y aislamiento tenant A/B, Cloudinary vigente y R2/Hostinger solo como objetivo condicional.
- Exclusivamente docs/v5 y CHANGELOG en dev; Scope Lock/V5 exact-SHA, BITACORA/PLAN/COLA y A5 independiente; no SDK, endpoints, workers, secretos, migraciones, objetos ni QA/main/PROD.

## 2026-11-14 — V5 C05-04.M01.04: riesgos y decisiones de ligereza [Javier :00]
- Registra R01–R12 riesgos hipotéticos, S01–S08 supuestos falsables y D01–D08 decisiones PENDIENTE sobre bundle, HTTP, tenancy A/B, accesibilidad, RUM, preload y rollback, sin afirmar incidencias.
- Coteja fuentes documentales y ocho blobs Angular/SCSS; P06/N12 son oráculos FUTUROS NO_EJECUTADOS; ningún presupuesto CWV, permiso o arquitectura nueva queda aprobado.
- Solo dos Markdown, Scope Lock/procedencia CHANGELOG exact-SHA y A5 independiente, rollback git revert; sin datos, código runtime, workers, eventos, QA/main/PROD.

## 2026-11-14 — V5 C05-04.M01.03: inventario real de fuentes de ligereza [Javier :00]
- Inspecciona diez referencias de preparación, Angular, router, preload selectivo, SCSS responsive, estados ARIA y scripts; blobs exactos y límites de inferencia, sin benchmark.
- Documenta diez brechas de evidencia y P06/N12 oráculos FUTUROS NO_EJECUTADOS para bundle, Core Web Vitals, HTTP, tenant A/B, accesibilidad, preload y rollback.
- Solo dos Markdown en dev; Scope Lock/procedencia CHANGELOG exact-SHA, A5 independiente y revert normal, sin runtime, secretos, eventos, workers, QA/main/PROD.

## 2026-11-14 — V5 C05-04.M01.02: actores y fronteras de ligereza [Javier :00]
- Delimita 12 actores y 12 entradas/salidas futuras de rendimiento percibido: lazy routing, preload explícito, feedback ARIA, métricas repetibles y aislamiento tenant A/B.
- Contrasta cinco blobs de Angular y M01.01 con P01–P06/N01–N12 como oráculos FUTUROS NO_EJECUTADOS; el budget 650/750 kB es configuración, no prueba de build.
- Solo dos Markdown en dev; sin cambiar UI/API/DB, JWT/RBAC, worker ni eventos; Scope Lock y V5 provenance exact SHA, A5 independiente y rollback normal, sin QA/main/PROD.

## 2026-11-14 — V5 C05-04.M01.01: objetivo y no-regresión de ligereza [Javier :50]
- Define alcance de futura mejora de carga inicial, duplicación HTTP, densidad visual y percepción con NR01–NR12, seis positivos y doce negativos FUTUROS, sin afirmar mediciones o builds ejecutados.
- Coteja ocho blobs Angular/SCSS: budgets declarados 650/750 kB, preload selectivo, rutas lazy, foco/feedback y scripts; conserva tenant A/B, JWT/RBAC, accesibilidad, reduced-motion y rollback.
- M01.01 solo dos Markdown en dev, sin cambios UI/API/DB ni eventos o tareas; Scope Lock y V5 provenance exact SHA, A5 por otro Javier, revert ordinario, ninguna promoción QA/main/PROD.

## 2026-11-14 — V5 C10-01.M01.03: inventario real de almacenamiento [Javier :40]
- Coteja diez blobs de interfaces, Cloudinary actual, StorageTenantContext, ImagenUploadSecurity y fuentes xUnit; distingue código actual de abstracción provider-neutral FUTURA sin R2 activo.
- Identifica seis brechas de evidencia y P01–P05/N01–N12 como oráculos FUTUROS NO EJECUTADOS: MIME, tamaño, hash real, tenant A/B, ACL, URL temporal, auditoría, migración y rollback.
- M01.03 solo documental dev, Scope Lock/V5 exact SHA y A5 independiente; sin transferir objetos, secretos, buckets, migraciones, pruebas live ni QA/main/PROD.

## 2026-11-14 — V5 C05-03.M01.05: aceptación observable del builder [Javier :50]
- Fija 14 criterios AC01–AC14 y 14 negativos N01–N14 con condiciones observables y suites FUTURAS (no ejecutadas) para catálogo allowlisted, tenant A/B, JWT/RBAC, preview, CAS, accesibilidad 320px y rollback.
- Mantiene D01–D08 PENDIENTE del contrato M01.04, coteja línea base de 12 blobs de M01.03 y no afirma builder ni autoriza editor/API/DB, workers, eventos o publicación.
- Solo dos Markdown en dev, Scope Lock y procedencia CHANGELOG exact SHA requeridos, A5 de otro Javier, rollback git revert ordinario; CI de runtime NO_APLICA a este diff, sin QA/main/PROD.

## 2026-11-14 — V5 C05-03.M01.04: riesgos y decisiones del builder [Javier :40]
- Documenta R01–R12 riesgos hipotéticos, S01–S08 supuestos falsables y D01–D08 decisiones PENDIENTE con roles propuestos de aprobación; ningún permiso ni decisión se concede.
- Coteja nueve blobs de código y preparación M01; P01–P05/N01–N12 son oráculos FUTUROS NO EJECUTADOS, con guardas de seguridad para XSS/SSRF, tenant A/B, JWT/RBAC, payload y CAS.
- Solo Markdown M01.04 en dev; Scope Lock/V5 exact SHA, A5 independiente y rollback git revert; no builder, migración, dispatcher, QA/main/PROD.

## 2026-11-14 — V5 C05-03.M01.03: inventario causal builder gobernado [Javier :50]
- Audita 12 blobs exactos en frontend, Application, API y estilos: tema visual, filtros de permisos, tenant verificado, configuración empresarial y diseño; NO afirma que exista builder funcional.
- Distingue patrones reutilizables de siete brechas: catálogo cerrado, XSS/SSRF, JWT/RBAC/tenant A/B, versionado CAS, UI accesible, persistencia y preview/publicación; P01–P05/N01–N12 son criterios FUTUROS, no pruebas ejecutadas.
- Solo dos Markdown en dev, sin runtime/DB/worker/eventos; Scope Lock y procedencia de CHANGELOG exact-SHA requeridos, A5 de Task Javier distinta y rollback normal mediante git revert; sin QA/main/PROD.

## 2026-11-14 — V5 C05-03.M01.02: actores y fronteras del builder [Javier :40]
- Documenta 12 actores y 12 entradas/salidas FUTURAS de builder allowlisted con preview aislada, publicación versionada y permisos backend/tenant.
- Coteja seis blobs de tema Angular/SCSS/API/rutas; P01–P05 y N01–N12 son oráculos FUTUROS NO EJECUTADOS sobre código inyectado, URL/SSRF, A/B, JWT/RBAC, profundidad y CAS.
- M01.02 solo documental en dev, Scope Lock/V5 exact SHA, A5 independiente y git revert normal; sin API, MySQL, workers, dispatcher, QA/main/PROD.

## 2026-11-14 — V5 C05-02.M01.04: riesgos, supuestos y decisiones de marca [Javier :00]
- Registra R01–R12 riesgos hipotéticos, S01–S08 supuestos falsables y D01–D08 decisiones PENDIENTE sobre tema global vs tenant, logos, presets, caché, autorización, versionado, publicación y rollback; sin afirmar vulnerabilidades.
- Siete blobs Domain/API/EF/Angular sustentan cinco oráculos positivos y doce negativos FUTUROS no ejecutados; IAM/JWT/RBAC/tenant y acceso público permanecen bajo autorización backend.
- M01.04 documental dev, Scope Lock/V5 exact-SHA, A5 independiente y revert ordinario; sin cambiar código, migraciones, eventos, workers, datos, Tasks, QA/main/PROD.

## 2026-11-14 — V5 C05-02.M01.03: inventario real de branding [Javier :00]
- Inspecciona 13 blobs exactos Domain/Application/API/EF/Angular: TemaVisual global Id=1, GET público colores, PUT protegido, reset a default, sanitización de hex y pruebas frontend existentes sin atribuir ejecución.
- Separa seis brechas técnicas de vulnerabilidades no demostradas; seis oráculos positivos y doce negativos FUTUROS cubren tenant A/B, JWT/RBAC, versionado CAS, caché, contrastes, logo y rollback.
- M01.03 solo inventario documental en dev, sin migrar tema global a per-tenant ni cambiar API/DB/UI; Scope Lock/V5 exact SHA, A5 independiente, rollback revert normal, no QA/main/PROD.

## 2026-11-14 — V5 C05-02.M01.02: actores y superficies de branding [Javier :00]
- Traza ocho actores, nueve entradas/salidas y nueve fronteras de confianza para tema visual, presets futuros, logo, identidad pública y estado multiempresa, sin conceder autoridad desde el cliente.
- Coteja ocho blobs Angular/SCSS; seis positivos y doce negativos de aceptación FUTUROS (no ejecutados), diferenciando `restaurar` existente de versionado CAS/publicación aún sin acreditar.
- M01.02 únicamente documental dev; sin API, MySQL, UI, worker, webhooks ni migración; Scope Lock/CHANGELOG exact SHA, A5 de otra Task Javier y rollback git revert. Sin QA/main/PROD.

## 2026-11-14 — V5 C05-02.M01.01: frontera de branding y presets [Javier :00]
- Define objetivo, exclusiones y doce invariantes NR para temas, logos, identidad, presets futuros, accesibilidad, revisión y restauración sin permitir autoridad por branding.
- Inspección de siete blobs Angular/SCSS; seis oráculos positivos y doce negativos futuros sobre tenant A/B, JWT/RBAC, sanitización, concurrencia y rollback; NO son pruebas runtime ejecutadas.
- M01.01 solo documental dev; sin cambios en API, DB, UI, eventos, worker o migración; requiere Scope Lock/procedencia CHANGELOG exact SHA y A5 independiente. Revert regular y ninguna promoción QA/main/PROD.

## 2026-11-14 — V5 C04-03.M01.05: criterios observables de metering [Javier :20]
- Define AC01–AC14 con negativos N01–N14 como pruebas FUTURAS de tenant A/B, unidades, idempotencia consumo, fuente confiable, periodo UTC, conciliación, retención, JWT/RBAC, API, MySQL y UI, sin atribuir tests ejecutados.
- Coteja blobs de M01.01–04 y SuscripcionesSaaS, separando idempotencia de onboarding de consumo; D01–D08 quedan PENDIENTE de autorización expresa.
- Solo aceptación documental M01.05 en dev; sin worker, ledger, migración, cobros, eventos ni QA/main/PROD; exige Scope Lock/V5 exact SHA, A5 independiente y rollback git revert normal.

## 2026-11-14 — V5 C04-02.M01.05: aceptación contractual observable [Javier :30]
- Define catorce criterios AC01–AC14 y negativos N01–N14 para alta, estados, renovación, cancelación, mora, tenant/JWT/RBAC, replay, MySQL, UI accesible, auditoría y release seguro.
- Distingue oráculos FUTUROS no ejecutados de la evidencia existente Domain/Application/API/EF/xUnit/Angular y mantiene ocho decisiones comerciales sin aprobación ni precios inventados.
- M01.05 solo documental en dev, Scope Lock/procedencia V5 exact-SHA y A5 independiente; no habilita M01.06/M02, cobros, migraciones, events, workers, QA/main/PROD. Rollback git revert normal.

## 2026-11-14 — V5 C04-03.M01.04: riesgos y decisiones del metering [Javier :20]
- Registra R01–R12 riesgos posibles, S01–S08 supuestos falsables y D01–D08 decisiones PENDIENTE con rol de aprobador futuro; no aprueba cuotas, retención, facturación o políticas.
- Diferencia ledger idempotente de onboarding, límites PlanLimite y UI de cualquier medidor real; cuatro P/ocho N documentales no ejecutados, sin falsas vulnerabilidades A/B confirmadas.
- Entrega M01.04 solo documental en dev, sin API, MySQL, migraciones, worker, eventos ni QA/main/PROD; Scope Lock/V5 exact SHA, A5 independiente y rollback git revert normal.

## 2026-11-14 — V5 C04-02.M01.04: riesgos y decisiones contractuales [Javier :30]
- Documenta doce riesgos R01–R12, ocho supuestos S01–S08 y ocho decisiones D01–D08 pendientes sobre renovación, mora, cancelación, factura y autoridad económica, sin aprobar reglas financieras.
- Cinco verificaciones documentales y diez negativos futuros delimitan aislamiento tenant A/B, JWT/RBAC, replay, rollback, MySQL, UI, CI y A5; no atribuyen pruebas runtime.
- M01.04 exclusivamente en dev, exact-SHA Scope Lock/procedencia V5 y A5 independiente; sin cobros, migraciones, callbacks, workers, nuevas Tasks, QA/main/PROD. Revert normal no destructivo.

## 2026-11-14 — V5 C04-03.M01.03: inventario de codigo y evidencia metering [Javier :20]
- Inspección autenticada de once blobs Domain/Application/DTO/API/EF/repository/tests/Angular/documentos V5, diferenciando límites y ledger de onboarding del metering futuro no acreditado.
- Identifica seis brechas y cinco escenarios P/doce N documentales no ejecutados: replay por consumo, tenant A/B, unidad/periodo, conciliación, privacidad y persistencia.
- Solo inventario M01.03 en dev, sin código, DB, workers, eventos, facturación, QA/main/PROD; Scope Lock/V5 exact-SHA, A5 por otra Task Javier y rollback git revert normal.

## 2026-11-14 — V5 C04-02.M01.03: inventario real de contratos y suscripciones [Javier :30]
- Relee doce archivos con blobs verificables Domain/Application/DTO/API/EF/MySQL repository/xUnit/Angular y distingue estados SaaS de renovaciones, mora y cobros futuros.
- Documenta seis contrastes positivos y diez negativos de evidencia, seguridad tenant/JWT/RBAC, idempotencia, compatibilidad y rollback; ningún test de runtime se atribuye por haber leído el código.
- Solo inventario M01.03 en dev, Scope Lock/procedencia V5 sobre SHA exacto y A5 independiente; sin alterar API, DB, UI, pagos, migraciones, workers, eventos, QA/main/PROD. Rollback git revert ordinario.

## 2026-11-14 — V5 C04-03.M01.02: actores, entradas y superficies de metering [Javier :20]
- Documenta diez actores y catorce fronteras futuras para medición de usuarios, operaciones, almacenamiento, archivos y proyecciones con tenant resuelto en servidor; conserva blobs reales y límites de evidencia.
- Establece P01–P05 y N01–N12 como oráculos posteriores no ejecutados: replay, correlación, unidad y corte, conciliación, RBAC y aislamiento A/B.
- Exclusivamente M01.02 documental: sin API, MySQL, workers, eventos, cobros, QA/main/PROD; Scope Lock/V5 exact-SHA, A5 por otro Javier y rollback git revert ordinario.

## 2026-11-14 — V5 C04-02.M01.02: actores y superficies contractuales [Javier :30]
- Delimita ocho actores del ciclo de suscripciones, cinco bordes de entrada/salida SaaS y siete fronteras de confianza, distinguiendo código vigente de cobros y morosidad futuros.
- Especifica cinco oráculos positivos y diez negativos futuros para aislamiento tenant A/B, JWT/RBAC, idempotencia, suspensión, cancelación, renovación y falla de proveedor; ninguno se declara ejecutado como runtime.
- Únicamente preparación M01.02 documental con Scope Lock y procedencia V5 exact-SHA, A5 pendiente de Task Javier distinta; no migraciones, endpoints, workers, eventos, pagos, QA/main/PROD. Rollback git revert normal.

## 2026-11-14 — V5 C04-03.M01.01: alcance y no-regresion del metering SaaS [Javier :20]
- Define preparación documental de conteo de usuarios, almacenamiento, operaciones y archivos por tenant con métrica/unidad, corte temporal, idempotencia y conciliación futuros; lectura de blobs reales de PlanLimite y SuscripcionesSaaS, sin asumir ledger ya implementado.
- Documenta NR01–NR14 y P01–P05/N01–N12 como oráculos futuros no ejecutados; separa JWT/RBAC/tenant server-side, entitlement, replay, cuotas, confidencialidad y estado de suscripción.
- No implementa eventos, tablas, API, workers, cobros, tarifas, migraciones, runtime, QA/main/PROD; exige Scope Lock/V5 exact-SHA, revisión A5 independiente y rollback por git revert normal.

## 2026-11-14 — V5 C04-02.M01.01: frontera contractual de suscripciones [Javier :30]
- Define objetivo y no-alcance de alta, renovación, pausa, cancelación, mora y cierre contractual; diez invariantes NR01–NR10 y quince oráculos futuros positivos/negativos sin declarar pruebas runtime.
- Lee entidades, servicio SaaS, controlador y DTO existentes; separa membresía tenant/RBAC, estado y vigencia UTC, idempotencia, historia y rollback; no inventa cobros, monedas, tarifas ni pasarelas.
- Entrega exclusivamente documentación M01 en dev, Scope Lock/V5 exact-SHA y A5 independiente; sin migraciones, endpoints, workers, webhooks, automations, QA/main/PROD. Rollback git revert normal.

## 2026-11-14 — V5 C03-03.M01.06: puertas de auditoría de seguridad documental [Javier :20]
- Cierra únicamente la definición M01 de bloqueo, rollback y salida para auditoría de autorización/denegación con cinco documentos M01 y C02-04.M01.06 LISTO/A5; conserva R01–R14 y AU01–AU16 como pendientes de evidencia runtime.
- Establece G01–G07, cuatro oráculos P y doce negativos N: aislamiento tenant A/B, permisos backend, correlación segura, minimización de secretos, falla DB, idempotencia, retención y revisión independiente.
- No implementa ni certifica auditoría HTTP/MySQL/EF, JWT/RBAC, MFA, retención, workers, eventos, QA/main/PROD; A0–A4 documentales y Scope Lock/V5 exact-SHA, A5 de otra Task Javier, rollback git revert normal.

## 2026-11-14 — V5 C04-01.M01.05: criterios observables del catálogo SaaS [Javier :10]
- Define 14 criterios AC con respectivos negativos N01–N14 para versión inmutable, límites, tarifas aprobadas, A/B tenant, JWT/RBAC, replay, integridad MySQL, API, UI, privacidad y gate CI exact-SHA.
- AC/N son contratos futuros y NO pruebas runtime ejecutadas; no inventa precios, monedas, impuestos, autorización comercial, migraciones, workers o cobros.
- Entrega M01.05 solo documental en dev, A0–A4 Scope Lock/V5 exact-SHA y A5 independiente; no libera M01.06/M02 ni QA/main/PROD. Rollback: git revert normal.

## 2026-11-14 — V5 C04-01.M01.04: matriz de riesgos y decisiones abiertas del catálogo [Javier :10]
- Documenta 12 riesgos, ocho supuestos con estado de evidencia y ocho decisiones D01–D08 explícitamente pendientes sobre versionado, tarifa, periodicidad, downgrade, cobros, RBAC y datos; cinco controles P y diez N son oráculos documentales no ejecutados.
- Delimita A/B tenant, JWT/RBAC, idempotencia y preservación de historia; no inventa importes ni aprueba precios, impuestos, pasarelas, migraciones, endpoints, workers o eventos.
- Solo preparación documental M01.04 en dev, con Scope Lock/V5 SHA exacto antes de A4 y revisión A5 independiente; sin QA/main/PROD. Rollback git revert ordinario.

## 2026-11-14 — V5 C04-01.M01.03: inventario comprobado de código y evidencia SaaS [Javier :10]
- Registra doce blobs reales Domain/Application/API/EF/Angular/tests de SuscripcionesSaaS, siete capacidades con certeza limitada y cinco brechas de catálogo comercial versionado.
- Diferencia explícitamente código leído, tests Moq existentes, snapshot EF y CI runtime realmente ejecutada; no inventa precios, monedas, migraciones aplicadas, accesos tenant ni certificaciones.
- Solo M01.03 documental, sin tocar código ni habilitar M01.06/M02, billing, eventos o QA/main/PROD. A0–A4 con Scope Lock/V5 exact-SHA, A5 independiente y rollback git revert normal.

## 2026-11-14 — V5 C04-01.M01.02: actores, entradas, salidas y superficies SaaS [Javier :10]
- Documento de frontera con ocho actores, seis bordes API/Application/Infrastructure/Angular, seis fronteras de confianza y diez negativos futuros; referencias a blobs reales del catálogo SaaS existente.
- Delimita contrato futuro de versiones, precios, periodicidad y upgrade sin inventar tarifas ni ampliar RBAC, tenant, MySQL, endpoint, worker, webhook, pasarela o secreto; las pruebas P/N están especificadas pero no ejecutadas como runtime.
- Solo preparación M01.02 en dev; A0–A4 documentales con checks Scope Lock/procedencia V5 exact-SHA, A5 por otra Task Javier, rollback git revert; no QA/main/PROD.

## 2026-11-14 — V5 C04-01.M01.01: objetivo y frontera del catálogo de planes [Javier :10]
- Contrato documental de objetivo, no-alcance, diez invariantes NR y quince escenarios P/N no ejecutados; alinea futura versión de planes, capacidades, precios, periodicidad y upgrade con fuentes reales de SuscripcionesSaaS.
- Preserva tenant server-side, RBAC independiente del entitlement, idempotencia y datos comerciales; no inventa tarifas ni instala billing, workers, webhooks, migraciones, API, nuevos permisos o Tasks.
- Solo M01.01 y dev; A0–A4 documentales, Scope Lock y procedencia V5 exact-SHA antes de LISTO, A5 por otra Task Javier y rollback git revert normal. Sin QA/main/PROD.

## 2026-11-14 — V5 C00-04.M03.02: preparación Application inerte de decisiones de gobierno [Javier :50]
- Agrega servicio .NET 10 Application para crear una proyección consultiva a partir de invariantes Domain validadas, sin ejecutar decisiones, persistir propuestas, aprobar cambios ni otorgar permisos.
- La salida siempre exige autorización externa y no devuelve motivo ni clave de idempotencia; cambios sensibles o sin rollback permanecen NO_GO. JWT/RBAC/tenant/DAG/CAS siguen fuera de esta preparación y deben comprobarse en su frontera auténtica.
- Añade pruebas xUnit positivas y negativas de propuesta, rollback, cambios sensibles, entradas inválidas y no elevación de permisos; exige CI backend/seguridad/Scope Lock y control V5 exact-SHA antes de LISTO_DEV, A5 independiente de otra Task Javier. Sin API, DB, workers, webhooks, nuevas Tasks ni QA/main/PROD. Rollback: git revert normal.

## 2026-11-14 — V5 C00-04.M03.01: invariantes Domain de propuestas de gobierno [Javier :00]
- Agrega reglas puras .NET 10 para validar microtarea, decision, motivo, SHA de evidencia, clave de idempotencia, revision y clasificacion de riesgo; rechaza entradas malformadas, cambios sensibles y propuestas sin rollback.
- Ningun resultado concede ejecucion o aprobacion: tenant, RBAC, DAG, A5 y CAS institucional exigen autoridad externa. Sin endpoints, EF/MySQL, workers, webhooks, Tasks nuevas ni QA/main/PROD.
- Pruebas xUnit positivas y negativas en Domain; CI backend, seguridad, Scope Lock y procedencia CHANGELOG sobre SHA exacto; A5 por otra Task Javier y rollback mediante revert normal.

## 2026-11-14 — V5 C00-04.M02.06: contrato documental de salida M02 y dependencias [Javier :40]
- Cierra el mapa documental de 13 predecesoras LISTO/A5, seis contratos M02, controles fail-closed, oraculos P01-P04/N01-N12 y handoff condicionado a DAG individual; no habilita automaticamente M03, QA/main/PROD.
- Solo Markdown y CHANGELOG: sin API, JWT/RBAC/tenant runtime, MySQL/EF, migracion, dispatcher, webhook, worker, nuevas Tasks o eventos. A0-A4 documentales y Scope Lock/V5 exact-SHA; A5 por otra Javier; rollback git revert.

## 2026-11-14 — V5 C00-04.M02.05: contrato documental de eventos, idempotencia y limites [Javier :30]
- Define sobre logico futuro, deduplicacion por tenant/scope/clave, correlacion y causacion, cuotas, seguridad y oraculos P01-P05/N01-N13 como criterios NO ejecutados; A5 independiente de M02.04 y DAG150 VALIDADO/SI.
- Solo documento y CHANGELOG; sin workers, webhooks, dispatcher, outbox, Task-to-Task, MySQL, API, JWT/RBAC, nuevos permisos ni QA/main/PROD. A0-A4 documentales requieren Scope Lock y V5 exact-SHA; A5 posterior por otra Task; rollback git revert.

## 2026-11-14 — V5 C00-04.M02.04: contrato documental de configuración tipada y feature flags seguros [Javier :10]
- Diseño allowlisted y fail-closed de claves tipadas, ámbitos PLATFORM/ENVIRONMENT/TENANT_BOUND, precedencia, TTL/revisión, once negativos y cuatro positivos documentales; nueve predecesores LISTO/A5 y DAG149 individual VALIDADO/SI.
- Solo Markdown y CHANGELOG, sin appsettings, secrets, runtime, JWT/RBAC/tenant, MySQL, API, eventos, webhooks, Tasks, permisos QA/main/PROD; A0–A4 documentales con Scope Lock/V5 exact-SHA, A5 independiente y rollback git revert.

## 2026-11-14 — V5 C00-04.M02.03: diseño documental de persistencia y rollback del gobierno [Javier :00]
- Modelo de fuentes institucionales PLAN/COLA/DAG/BITACORA y Git; catálogo de proyecciones FUTURAS no autoritativas, índices lógicos y ocho fixtures negativos/positivos. Ocho predecesores C00-04.M01.01–06/M02.01–02 LISTO con A5 independiente; DAG148 F=VALIDADO/G=SI individual.
- Solo documentación y CHANGELOG; ninguna entidad EF, MySQL, SQL, migración, webhook, worker, API, JWT/RBAC, tenant, Task, QA/main/PROD o datos reales. A0–A4 documentales requieren Scope Lock y procedencia V5 exact SHA; A5 por otra Javier. Rollback con revert ordinario.

## 2026-11-14 — V5 C00-04.M02.02: contratos documentales API/DTO del gobierno de cambios [Javier :40]
- Definición tipada de consultas, decisiones propuestas, condiciones de ejecución, evidencia idempotente, errores y compatibilidad; siete predecesores LISTO con A5 independiente, DAG147 VALIDADO/SI para este ID exclusivamente.
- Alcance documental: ninguna ruta HTTP, servicio, autorización, JWT/RBAC/tenant, migración, worker, webhook o Task nueva; exigir A0–A4 con Scope Lock y procedencia exact-SHA y A5 por otra Javier. Rollback git revert sin force-push; QA/main/PROD no autorizados.

## 2026-11-14 — V5 C01-01.M03.02: revalidación Application de identidad universal [Javier :00]
- Servicio real `IdentidadUniversalApplicationService` y contrato de lectura server-side `IIdentidadUniversalSnapshotSource`: comprueba tenant autenticado antes del I/O, vuelve a validar membresía/rol/sucursal desde datos confiables y deniega drift, revocación o tenant B.
- Suite xUnit dirigida `IdentidadUniversalApplicationServiceTests.cs` con positivos y negativos de aislamiento A/B, rol cambiado, usuario ajeno, sucursal inexistente/ajena, ausencia de fuente, revocación y no-escalamiento implícito. No registra DI, API/endpoint, JWT, migración/SQL, workers ni webhooks.
- TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`; DAG146 VALIDADO/SI individual. A0–A4 sólo con CI real exact SHA; A5 por Task Javier distinta, solo dev. Rollback: git revert sin force-push.

## 2026-11-14 — V5 C03-01.M01.04: reparación documental A5_FAIL sobre autoridad aprobadora [Javier :40]
- Corrección por autor original TASK_ID `6adc2e55f47c81919f6374e561687076` del documento `docs/v5/c03-01-m01-04-riesgos-supuestos-decisiones-autenticacion.md`: D01–D08 ahora identifican explícitamente el rol responsable de futura aprobación y el dictamen técnico previo, sin asumir personas designadas, decisiones aprobadas ni cambios de proveedores.
- Conserva el A5_FAIL independiente histórico del SHA `e214487251a9c09028382594fa33f63770afa01e`; requiere nuevo A0–A4 en SHA corregido y nueva A5 de otra Task Javier. Scope documental solamente; JWT/RBAC/tenant, API, DB, MFA, workers, QA/main/PROD y programación intactos. Rollback: git revert normal.

## 2026-11-14 — V5 C00-04.M02.01: entidades, ownership e invariantes del gobierno de cambios [Javier :10]
- Documento contractual `docs/v5/c00-04-m02-01-entidades-ownership-invariantes.md`: define nueve entidades lógicas de gobierno, diez invariantes y relaciones de decisión, autorización, owner, riesgo, auditoría y rollback; no crea tablas, DTO, API ni automatizaciones.
- Entrada causal: seis `C00-04.M01.01–06` LISTO con A5 correctiva/independiente, DAG145 `VALIDADO/SI`; propiedad exclusiva `C00-04.M02.01`, sin liberar M02.02–06, IAM, JWT/RBAC, migraciones ni QA/main/PROD por inferencia.
- Verificación documental A0–A4, Scope Lock y control V5 de procedencia sobre SHA exacto de publicación; `A5_PENDIENTE` hasta revisor Javier distinto, BITACORA A:Z append-only, rollback por revert ordinario.

## 2026-11-14 — V5 C01-01.M03.01: invariantes de identidad universal tenant-aware [Javier :00]
- Código real de Domain en `backend/src/Domain/Security/IdentidadUniversalReglas.cs`: usuario/empresa activos, membresía vigente y rol empresarial asignable, sin grants por `Usuario.RolId` legacy, rol de plataforma o tenant B; sucursal exige propietario verificable y estado habilitado.
- Suite xUnit `IdentidadUniversalReglasTests.cs` con positivos y negativos de tenant A/B, revocación, roles, empresa, usuario bloqueado y sucursal sin owner. El guard puro no reemplaza JWT/RBAC ni middleware API, no ejecuta migraciones MySQL ni incorpora endpoints, Angular o eventos.
- Task-ID `6adbb9fff1548191a1bd5b30d9e9984a`, Scope `C01-01.M03.01`, DAG144 individual. Evidencia A0–A4 y CI dirigida del commit exact-SHA obligatoria; A5 independiente por otra Task Javier, solo dev y rollback por revert ordinario.

## 2026-11-13 — V5 C01-01.M02.06: dependencias técnicas y contrato de salida M02 [Javier :10]
- Nuevo contrato documental `docs/v5/c01-01-m02-06-dependencias-contrato-salida.md`: cinco fuentes M02 con blobs exactos y M01.06, 14 predecesoras LISTO, DAG F143 VALIDADO/G143 SI, matriz de consumidores y oráculos positivos/negativos P01–P04/N01–N08.
- La salida solo cierra diseño M02: no libera automáticamente M03/M04/M05/M06, IAM/JWT/RBAC, EF/MySQL, Angular, eventos, webhooks, workers ni QA/main/PROD. Los consumidores requieren nuevo DAG individual, A0–A4 reales y A5 independiente.
- Cambios estrictamente Markdown; Scope Lock y control V5 de CHANGELOG exact-SHA requeridos para LISTO_DEV. A5 queda pendiente de otra Task Javier; rollback por git revert normal sin reescritura.

## 2026-11-13 — V5 C01-01.M02.05: contrato de eventos e idempotencia, sin implementación [Javier :30]
- Diseño documental `docs/v5/c01-01-m02-05-contrato-eventos-idempotencia-correlacion.md`: cinco blobs de código real (correlación, observabilidad, outbox, servicio y retry), seis futuros eventos de identidad, 12 oráculos EVT01–12 con positivos/negativos.
- Diferencia EventoId, ClaveIdempotencia por EmpresaId y CorrelationId, límites, claims/leases, deduplicación y fail-closed tenant JWT/RBAC, sin crear webhook, dispatcher, workers, notificaciones, mensajes ni datos en proveedor.
- C01-01 M01.01–06/M02.01–04 y tres cierres C00 LISTO; DAG F142 VALIDADO/G142 SI, claim CAS. Solo documento y CHANGELOG, A0–A4 proporcionales, A5 pendiente revisión independiente, no M02.06/M03/QA/main/PROD.

## 2026-11-13 — V5 C01-01.M02.04: diseño de configuración tipada y flags fail-closed [Javier :30]
- Contrato `docs/v5/c01-01-m02-04-configuracion-tipada-ambitos-flags.md` contrastado contra seis fuentes dev: ConfigEmpresa, EF, PlanModulo/PoliticaModulosSaaS, TenantPermissionContext, VerifiedPlatformSupportTenant y ADR de precedencia.
- Define ámbitos lógicos y diez casos CFG01–CFG10 positivos/negativos de tenant A/B, RBAC, suscripción, versión CAS, caché y referencias opacas sin credenciales; ningún flag, JWT, API, DB, plan o configuración ejecutable se modifica.
- Preflight nueve M01/M02.01–03 y tres C00 LISTO, DAG F141 VALIDADO/G141 SI, claim CAS; requiere Scope Lock/V5 provenance exact-SHA y A5 por otra Task Javier, sin liberar M02.05–06/M03/QA/main/PROD.

## 2026-11-13 — V5 C01-01.M02.03: diseño EF/MySQL de identidad, índices y fixtures [Javier :30]
- Contrato `docs/v5/c01-01-m02-03-persistencia-indices-migraciones-fixtures.md` sobre ocho fuentes EF/Domain/Test con hashes; describe tabla UsuarioEmpresas, FKs restrictivas, unicidad U/E, Sucursal legacy nullable, índice calculado y migración histórica sin ejecutarla.
- Diez criterios DB01–DB10 y ocho fixtures simbólicos de tenant A/B, rol cruzado, revocación, datos legacy y rollback; ninguna fila real, dato de Aiven, migration, SQL, .NET o MySQL ejecutados en esta unidad documental.
- Preflight M01.01–06 y M02.01–02 + tres cierres C00 LISTO; DAG F140 VALIDADO/G140 SI, claim exclusivo; A0–A4 doc con Scope Lock y V5 provenance SHA exacto, A5_PENDIENTE otra Task Javier, no M02.04–06/M03/QA/main/PROD.

## 2026-11-13 — V5 C01-01.M02.02: contratos API/DTO, errores y compatibilidad tenant [Javier :30]
- Documento `docs/v5/c01-01-m02-02-contratos-api-dto-errores-compatibilidad.md`: nueve contratos fuente GitHub, cinco superficies API/filtros, matriz 200/400/401/403/404/409/429/503 y ocho oráculos positivos/negativos de autorización y compatibilidad.
- M02.01 LISTO_DEV documental y C01-01.M01.01–06+C00-01.M06.06+C00-03.M01.06+C00-03.M06.06 LISTO; DAG individual F139 VALIDADO/G139 SI; no habilita automáticamente otras M02, M03 ni IAM/JWT/tenant/DB.
- Alcance solo documental: no cambiar rutas, DTOs, Angular, EF, JWT/RBAC ni datos; requerir Scope Lock y V5 provenance exact-SHA, A5_PENDIENTE por Task Javier diferente, no QA/main/PROD.

## 2026-11-13 — V5 C01-01.M02.01: contratos de entidades, ownership e invariantes de identidad universal [Javier :30]
- Documento `docs/v5/c01-01-m02-01-contratos-entidades-ownership-invariantes.md`: matriz de siete fuentes de código reales con blob, grafo de pertenencia y doce invariantes positivas/negativas, tenant A/B, revocación, rol y privacidad.
- Preflight individual C01-01.M01.01–06 + C00-01.M06.06 + C00-03.M01.06 + C00-03.M06.06 LISTO; DAG F138 VALIDADO/G138 SI y claim CAS exclusivo; COLA sola no constituye autorización general.
- TIPO 1 diseño documental: NO implementa .NET, API, Angular, EF/MySQL, JWT/RBAC, workers ni eventos; no desbloquea M02.02–06/M03/M06/QA/main/PROD; requiere Scope Lock y V5 provenance SUCCESS exact-SHA, A5_PENDIENTE otra Task Javier.

## 2026-11-13 — V5 C00-02.M06.06: cierre DEV y mapa de dependencias del modelo de Tasks [Javier :30]
- Inventario 35/35 predecesoras LISTO (M01–M05 completos, M06.01–05), gate DAG F113 VALIDADO/G113 SI, COLA G73/O73 SI y cuenta Javier verificados antes de reclamar C00-02.M06.06.
- Cierre documental controlado: 136 entradas DAG examinadas sin sucesoras explícitas de C00-02.M06.06; no se liberan tareas sin DAG validado, no se modifican fórmulas ni se crea dispatcher, webhook, PR, rama, Task o permisos.
- Artefacto docs/v5/c00-02-m06-06-model-phase-release-gate.md; requerir Scope Lock y V5 CHANGELOG SUCCESS exact-SHA para LISTO_DEV, con A5_PENDIENTE de Task Javier distinta y readback institucional; no JWT/MySQL/Sheets CAS/QA/main/PROD.

## 2026-11-13 — V5 C00-03.M06.06: cierre técnico y mapa de dependencias de cobertura [Javier :20]
- Se inventarían 35/35 predecesoras LISTO de C00-03 (M01–M05 completos y M06.01–05) con seis SHA M05 y cinco fuentes M06, DAG F137 VALIDADO/G137 SI y COLA G109/O109 SI al reclamar.
- Se documenta la liberación estrictamente del estado DEV y la ausencia de sucesoras explícitas en DAG: no se crean Tasks, nuevas fases, permisos, fórmulas manuales, webhooks ni nueva microtarea; A5 independiente sigue pendiente y no hay QA/main/PROD.
- Artefacto docs/v5/c00-03-m06-06-coverage-phase-release-gate.md; requeridos Scope Lock y prueba V5 CHANGELOG del SHA exacto, readback PLAN/COLA/BITACORA; JWT/MySQL/CAS/tenant productivos NO certificados; rollback ordinario git revert.

## 2026-11-13 — V5 C00-03.M06.04: ensayo no destructivo de desactivación de cobertura [Javier :10]
- Se agregan ocho escenarios xUnit (dos variantes Theory y seis Facts) contra V5CoverageMemoryAdapter real: revocación de actor/scope, reapertura, revisión obsoleta, idempotencia, capacidad y namespace sin borrar registros legítimos.
- El ensayo es in-memory con allowlist provista: NO implementa desactivación de Tasks, rollback Sheets/MySQL, CAS distribuido, JWT/RBAC live, tenant A/B persistido ni workers.
- Test y docs/v5/c00-03-m06-04-safe-disable-rehearsal.md. Requiere CI .NET/seguridad, Scope Lock y CHANGELOG exact-SHA; A5 independiente, M06.06 no liberada, git revert normal, no QA/main/PROD.

## 2026-11-13 — V5 C00-03.M06.03: riesgos y decisiones de matriz integrada [Javier :10]
- Se documentan doce riesgos R01-R12 y cinco decisiones D01-D05 con responsabilidad técnica y seis SHA de cobertura M05; se contrastan M06.01 y el manifiesto M06.02 que corrigió rutas de Chromium.
- Se acotan TestServer, adaptador en memoria y sesiones Chromium simuladas; ninguna es prueba de CAS Sheets, JWT/RBAC live, MySQL durable ni tenancy persistida. Cancelled/skipped/in-progress no son PASS.
- Archivo docs/v5/c00-03-m06-03-coverage-risks-decisions.md. Scope Lock y verificador CHANGELOG exact-SHA, A5 independiente y readback requeridos; no liberar M06.06 ni QA/main/PROD, rollback ordinario git revert.

## 2026-11-13 — V5 C00-03.M06.05: solicitud de revisión independiente del lote de cobertura [Javier :00]
- Se documenta paquete A5 de seis implementaciones M05 con SHA, contratos, checks y casos positivos/negativos para un revisor Javier distinto, sin declarar revisión realizada ni despachar nuevas Tasks.
- La solicitud preserva límites de JWT/RBAC/tenant, Sheets CAS, MySQL y pruebas con fixtures/sesiones sintéticas; no certifica LIVE ni cambia QA/main/PROD.
- Archivo docs/v5/c00-03-m06-05-independent-review-request.md; requieren Scope Lock y procedencia V5 SUCCESS exact-SHA, A5 pendiente para esta propia microtarea y rollback normal git revert.

## 2026-11-13 — V5 C00-03.M06.02: manifiesto de hashes y corrección de procedencia M06.01 [Javier :00]
- Se registran seis SHA exactos, árboles, rutas y blobs de los test M05 para reproducción verificable, sin afirmar ejecución nueva de suites ni garantías live de CAS, JWT/RBAC, tenant o MySQL.
- Se corrige la procedencia documental de M06.01: M05.04 corresponde a c00-03-m04-05-accessibility.spec.ts (e16a8ea3), M05.06 a c00-03-m04-06-density.spec.ts (fd3756ee); el spec genérico citado antes no era el diff original.
- Se documenta el reparo sin reescribir eventos; requiere Scope Lock y procedencia CHANGELOG SUCCESS exact-SHA, A5 independiente; no QA/main/PROD, rollback normal por git revert.

## 2026-11-13 — V5 C00-03.M06.01: auditoría causal de suficiencia de pruebas de cobertura [Javier :00]
- Se cotejan seis entregables M05.01–06 con pruebas .NET/TestServer/in-memory/Chromium y sus SHA exactos, sin ejecutar de nuevo pipelines ajenos al diff documental.
- Evidencia suficiente sólo para contratos acotados: no certificar Sheets CAS, JWT/RBAC live, MySQL durable ni tenant persistido por fixtures o sesiones sintéticas; CANCELLED no es PASS.
- Archivo docs/v5/c00-03-m06-01-test-sufficiency-audit.md; Scope Lock, procedencia V5, readback y A5 independiente requeridos. Sin liberar M06.06, QA/main/PROD; rollback git revert normal.

## 2026-11-13 — V5 C00-02.M06.04: ensayo no destructivo de desactivación de cola en memoria [Javier :30]
- Se incorporan seis casos xUnit reales (4 Theory + 2 Facts) contra V5WorkItemMemoryAdapter: gates cerrados rechazan nuevos claims sin borrar snapshots legítimos, reactivación con token idempotente y rechazo de ownership ajeno.
- El alcance se limita al adaptador actual en memoria: NO implica rollback de Sheets/MySQL, datos de producción, JWT live ni detener jobs ya iniciados; no modifica código productivo de seguridad ni las seis Tasks Javier.
- Archivos test y docs/v5/c00-02-m06-04-safe-disable-rehearsal.md; exigir .NET build/test/security y Scope Lock/CHANGELOG exact-SHA; A5_PENDIENTE, rollback normal git revert sin reescribir historia ni liberar M06.06.

## 2026-11-13 — V5 C00-02.M06.05: solicitud institucional de revisión independiente [Javier :30]
- Se registra solicitud A5 por ID/SHA del lote C00-02 con origen, autoría y pruebas positivas/negativas; se exige revisor Task Javier distinto, readback y no duplicar A5_PASS/FAIL existentes.
- La solicitud es documental y consultable en BITACORA, no webhook, dispatcher, nueva Task o notificación entregada; no declara JWT/Sheets CAS/MySQL live ni ensayo de rollback.
- Archivo docs/v5/c00-02-m06-05-independent-review-request.md, A0-A4 documentales con Scope Lock/CHANGELOG causal; A5_PENDIENTE propia y M06.06 sin liberar, rollback por git revert normal.

## 2026-11-13 — V5 C00-02.M06.03: riesgos, limitaciones y decisiones [Javier :30]
- Se documentan once riesgos y cinco decisiones causales sobre C00-02 M05/M06: seis SHA fuente, controles, dueños y límites reales de tests xUnit, TestServer y Chromium. No certificar CAS Sheets, JWT/RBAC live ni MySQL mediante fixtures.
- Se preservan jobs CANCELLED sin PASS, independencia A5 entre seis Tasks Javier, BITACORA 26 columnas y M06.06 como gate distinto. No se activan despachos ni se autorizan QA/main/PROD.
- Nuevo docs/v5/c00-02-m06-03-risks-decisions.md; A0-A4 documentales con Scope Lock y procedencia de CHANGELOG exact-SHA, A5_PENDIENTE y rollback por git revert normal.

## 2026-11-13 — V5 C00-02.M06.02: manifiesto reproducible de evidencia y versionado [Javier :10]
- Se registra una matriz causal de las seis microtareas M05.01–06, commits, blobs y alcance real de suites xUnit/TestServer/Chromium; se diferencia el SHA origen cancelado de la verificación causal por descendiente con blob intacto.
- La guía de reproducción distingue evidencia verificada de comandos de referencia, y no certifica CAS distribuido de Sheets, JWT/RBAC live, MySQL ni tenant A/B persistido. Cambio exclusivamente documental; A5 por otra Task Javier.
- Archivo docs/v5/c00-02-m06-02-reproducibility-manifest.md. Se exige Scope Lock y procedencia CHANGELOG exact-SHA antes de LISTO DEV; rollback por git revert normal, sin QA/main/PROD.

## 2026-11-13 — V5 C00-02.M06.01: auditoría de suficiencia causal de pruebas [Javier :10]
- Se documenta auditoría real de seis pruebas M05.01–06: reglas puras .NET, HTTP TestServer con ruta de prueba, adaptador in-memory, aceptación positiva con flags, guard de autorización con fixtures y Chromium con sesión simulada.
- El resultado se limita a los contratos efectivamente implementados; NO certifica CAS institucional Sheets, JWT/RBAC live, MySQL, tenant A/B persistido ni despliegues. Se conservan CANCELLED/SKIPPED/IN_PROGRESS como no PASS.
- Documento docs/v5/c00-02-m06-01-test-sufficiency-audit.md. Cambio sólo documental, con Scope Lock, procedencia V5, readback y A5 independiente; rollback git revert normal, sin QA/main/PROD.

## 2026-11-13 — V5 C00-02.M05.03: integración del selector y proveedor de estados en memoria [Javier :00]
- Se añade suite xUnit real que conecta V5WorkItemQueueSelector y V5WorkItemMemoryAdapter, verifica intención sin mutación, claim y replay idempotente, rechazo de competidor y puertas de área/HEAD/DAG.
- Este provider es exclusivamente in-process y no implementa Sheets CAS, MySQL, JWT ni lease distribuido; no cambia runtime, permisos o infraestructura. CI .NET, seguridad proporcional, CHANGELOG y Scope Lock exact-SHA siguen exigibles; A5 independiente.
- Documento dirigido: docs/v5/c00-02-m05-03-provider-integration.md. Revert ordinario; sin QA/main/PROD.

## 2026-11-13 — V5 C00-03.M05.03: integración de proveedor de cobertura en memoria [Javier :00]
- Se añade suite xUnit de integración real entre V5CoverageMemoryAdapter y V5CoverageTenantProjectionService: guardado/lectura/proyección, namespace de scope, revisión obsoleta y evidencia no certificada.
- El proveedor es estrictamente en memoria; NO prueba MySQL, Sheets, JWT HTTP ni CAS institucional, y no añade un backend durable. Requiere .NET, controles de seguridad por scope, CHANGELOG y Scope Lock exact-SHA antes de LISTO_DEV; A5 independiente.
- Documento dirigido: docs/v5/c00-03-m05-03-provider-integration.md. Rollback por git revert normal, sin QA/main/PROD.

## 2026-11-13 — V5 C00-03.M05.06: regresion y accesibilidad de cobertura [Javier :30]
- Se amplian pruebas Chromium reales de la ruta Angular V5 con dos casos dirigidos: presupuesto de carga, no overflow 390/1280, tabla y scroll de teclado, ausencia de llamadas institucionales, y denegacion sin token.
- Solo tests de interfaz con sesion simulada, no JWT, MySQL ni certificacion A5 live; require_node_angular/e2e/seguridad/Scope Lock, rollback por git revert normal.

## 2026-11-13 — V5 C00-03.M05.05: negativos de aislamiento de cobertura [Javier :30]
- Se incorpora xUnit .NET 10 sobre V5CoverageTenantProjectionService: un positivo sin certificacion ficticia y 11 rechazos dirigidos de actor, tenant, scope, item y requisito; ningun cambio de API o persistencia.
- El contrato no prueba JWT/MySQL live, no escribe Sheet ni otorga A5. Requiere CI .NET, seguridad y Scope Lock exact-SHA; rollback por git revert ordinario.

## 2026-11-13 — V5 C00-03.M05.02: contrato HTTP de cobertura y errores seguros [Javier :20]
- Se agrega suite xUnit TestServer del servicio real V5CoverageTenantProjectionService y middleware productivo; HTTP 200 conserva DTO y EVIDENCE_NOT_VERIFIED; 400 rechaza actor, tenant, SHA o requisito invalido sin filtracion.
- Ruta exclusiva de tests, no API publicada ni autenticacion JWT live/MySQL/Sheets/A5; se exige .NET CI y Scope Lock exact-SHA, provenance y A5 por otra Task Javier.

## 2026-11-13 — V5 C00-02.M05.05: seguridad negativa de contexto tenant/scope [Javier :10]
- Se agrega suite xUnit real V5C0002M0505NegativeIsolationTests con rechazos de IDs de scope ajeno en cinco operaciones, variantes de tenant, actores retirados, bindings ausentes y auto-revisión A5 prohibida.
- La seguridad se prueba sobre el guard Application real con mapas fiables de fixture, NO JWT live, HTTP productivo, MySQL, CAS institucional, A5_PASS ni dispatcher. Ningún cambio a permisos o datos productivos.
- Requiere dotnet build/test, seguridad multiempresa, autorización, Scope Lock y procedencia CHANGELOG del SHA exacto; A5_PENDIENTE por otra Task Javier, rollback git revert normal, sin QA/main/PROD.

## 2026-11-13 — V5 C00-02.M05.06: regresión responsive accesible de flujo V5 [Javier :00]
- Se amplía Playwright Chromium con una regresión dirigida para la vista de cuatro pasos V5 en tres anchos (390, 768 y 1280), sin desbordamiento horizontal exterior y con navegación por foco/teclado.
- El navegador comprueba que no se llaman endpoints institucionales de escritura. La sesión es sintética exclusivamente para UI; no afirma autenticar JWT, aislar tenants mediante HTTP real ni medir latencia de backend.
- Requiere build/lint Angular y browser V5 exact-SHA, Scope Lock y procedencia CHANGELOG. A5 independiente por otra Task Javier; sin QA/main/PROD.

## 2026-11-13 — V5 C00-02.M05.04: aceptación positiva .NET del ciclo V5 [Javier :10]
- Se añade la suite xUnit real V5C0002M0504PositiveAcceptanceTests: CLAIM autorizado, paso a VALIDANDO, LISTO con auditorías declaradas y readback de propiedad en el adaptador en memoria; dos ítems independientes conservan sus autores.
- El test NO implementa un CAS institucional de Sheets, JWT vivo, base MySQL, A5 independiente ni eventos entre Tasks. Un PASS de estas pruebas no sustituye controles reales A0-A4 del SHA exacto.
- Requiere dotnet build/test y gates Scope Lock, seguridad proporcional y procedencia CHANGELOG SUCCESS sobre el SHA publicado; A5_PENDIENTE para otra Task Javier, sin QA/main/PROD.

## 2026-11-13 — V5 C00-03.M05.04: aceptación positiva de cobertura V5 en Chromium [Javier :00]
- Se amplía la suite Playwright de cobertura V5 con aceptación positiva real de navegador: sesión simulada 200, tabla de cinco columnas, estado sin datos verificados y revalidación explícita sin escritura a PLAN_MAESTRO, COLA, BITACORA ni claims.
- La autenticación usada en este test es deliberadamente sintética y no acredita JWT/RBAC, MySQL ni datos entre empresas. La experiencia conserva controles de lectura sin certificar estados institucionales.
- Requiere Angular lint/build y Chromium/Playwright en el workflow V5 de browser exact-SHA, Scope Lock y procedencia CHANGELOG. A5 por otra Task Javier; sin QA/main/PROD.

## 2026-11-13 — V5 C00-03.M05.01: pruebas unitarias de reglas de cobertura [Javier :00]
- Se incorpora la suite xUnit real V5CoverageM05RuleRegressionTests para estados PENDIENTE/EN_PROGRESO/VALIDANDO/LISTO, cobertura de catálogo, duplicados, falta de evidencia, SHA movido, resultado exigido vacío e identidad de snapshot inconsistente.
- Los negativos fallan de forma cerrada y no producen filas parciales; LISTO no equivale a A5_PASS ni a evidencia causal verificada. Sólo se prueban reglas puras Application/Domain; no se simulan HTTP, autenticación JWT, MySQL, navegador ni revisión A5.
- Requiere dotnet build/test y Scope Lock + procedencia CHANGELOG SUCCESS sobre el SHA exacto antes de LISTO_DEV. A5_PENDIENTE para otra Task Javier; sin cambios en QA/main/PROD.

## 2026-11-13 — V5 C00-02.M05.02: regresión HTTP de DTO y errores seguros [Javier :50]
- Se agregan pruebas xUnit reales sobre TestServer HTTP y middleware de excepciones para respuesta correcta, denegación segura 400, conflicto 409 y error interno 500 sin detalles privados de otras empresas.
- La ruta de prueba es un stub, no un endpoint nuevo ni permiso de producción; no se simula autorización JWT, CAS Sheets ni persistencia MySQL.
- Validación de compilación y pruebas .NET 10, seguridad, Scope Lock y provenance requiere jobs SUCCESS del SHA exacto, con A5 independiente posterior y sin QA/main/PROD.

## 2026-11-13 — V5 C00-02.M05.01: regresiones unitarias de ciclo y autoría [Javier :50]
- Se incorpora suite xUnit reproducible de reglas V5 para seis Task IDs autorizadas, rechazo de Task retirada, puertas COLA/DAG, doble claim, SHA, auditorías y A5 independiente.
- Solo se verifica lógica pura de dominio; no se simulan permisos reales, CAS de Sheets, JWT, MySQL, llamadas a Tasks ni producción.
- Build/tests .NET 10, seguridad pertinente, V5 provenance y Scope Lock requieren SUCCESS sobre el commit exacto publicado; A5 por otra Task Javier, sin QA/main/PROD.

## 2026-11-13 — V5 C00-03.M04.03: branding permitido y contraste local [Javier :00]
- La matriz privada de cobertura muestra identidad oficial y ofrece un control de alto contraste accesible con dos estados tipados y limitados al mat-card de cobertura; sin configuracion CSS libre, nuevo endpoint o cambio de tema de otra empresa.
- Angular usa los tokens globales seguros para modo normal y una paleta local de contraste alto; entradas query/tenant no gobiernan el switch y no crean datos de cobertura, grants ni operacion de escritura.
- Playwright Chromium suma dos casos de alternancia permitida y rechazo de parametros de visualizacion no confiables; Angular lint/build, V5 provenance y Scope Lock requieren checks reales del SHA publicado. .NET, MySQL y Docker no aplican al diff UI.
- A5 pendiente de Task Javier independiente; rollback git revert normal; no QA/main/PROD, ramas ni PR.

## 2026-11-13 — V5 C00-03.M04.02: estados de cobertura privada y permisos [Javier :00]
- La ruta protegida de cobertura distingue permiso confirmado, vacio institucional honesto, revalidacion en curso y denegacion/error generico sin divulgar respuestas del servidor.
- Reutiliza exclusivamente el GET de sesion global existente; no carga ni simula datos institucionales de cobertura y no concede JWT/RBAC ni operaciones de escritura.
- Playwright Chromium agrega pruebas positivas/negativas de vacio, una sola revalidacion, 403 y no filtracion. Angular lint/build, Scope Lock y procedencia CHANGELOG requieren checks del SHA publicado; MySQL/.NET/Docker no aplican a este diff frontend.
- La regresión Chromium 18/19 localizó dos alertas tras 403; se conserva un único mensaje accesible compatible con la prueba previa y se evita la tarjeta duplicada al fallar la sesión de cobertura.
- A5 independiente otra Task Javier; rollback por git revert normal, sin QA/main/PROD ni nuevas rutas.

## 2026-11-13 — V5 M04.01: Chromium real para rutas privadas de flujo y cobertura [Javier :50]
- Cuatro casos Playwright positivos/negativos incluyen ausencia de JWT, denegacion 403, vista de flujo informativa, navegacion responsive hacia cobertura sin mutaciones ni endpoints institucionales.
- Workflow dedicado con Node24/npm11, Angular build, Chromium real y backend sintetico solo para UI; NO certifica JWT/RBAC/MySQL productivo ni permite denegar/otorgar permisos.
- Las dos superficies siguen VALIDANDO hasta Scope Lock, calidad ERP y prueba navegador exact SHA; A5 independiente pendiente.

## 2026-11-13 — SOLQARYN V5 C00-02.M03.03 correccion A5: vinculo ID→scope de servidor [Javier :20]
- Corrige A5_FAIL J00 sobre SHA 244e5448: el catalogo global confirma existencia, no pertenencia de cada item al scope solicitado. Guard deniega por defecto ITEM_SCOPE_MISMATCH si trustedItemScopes falta o vincula el ID a otro scope.
- Tests xUnit: caso positivo del item asociado al scope de su tenant, negativos para ID ajeno presente en catalogo, todos los tipos de operacion y mapa ausente. Sin nuevas grants JWT/RBAC, endpoints, MySQL, workers, eventos o despliegues.
- A0 revision estatica previa; A1-A3 requieren .NET y security CI reales, A4 exige exact SHA, Scope Lock y readback; A5 independiente debe repetir revision del SHA nuevo. A5_FAIL historico preservado. Rollback revert normal, sin force-push ni PR.

## 2026-11-13 — V5 C00-02.M04.01 + C00-03.M04.01: reparar detector de guard privado [Javier :50]
- El scanner de calidad ERP reconoce exclusivamente /plataforma, /plataforma/flujo-v5 y /plataforma/cobertura-v5 como rutas del control plane protegidas por el MISMO plataformaGuard validado contra el backend.
- Sin permitir rutas arbitrarias ni debilitar JWT, RBAC, Scope Lock o denegaciones. Se conserva la excepcion exacta server-side y los tests negativos de plataforma; auditoria Priority4 y frontend se verifican en CI exact SHA.
- Corrige el fallo FRONTEND_ROUTE_SECURITY_CONTRACT de ambas superficies, que permanecieron VALIDANDO hasta confirmar nuevamente sus auditorias A0-A4; A5 independiente, sin promocion ni despliegue.

## 2026-11-13 — V5 C00-03.M04.01: pantalla privada de cobertura integrada [Javier :50]
- Angular agrega /plataforma/cobertura-v5 con plataformaGuard ya verificado, reutiliza PlataformaComponent y muestra las cinco columnas del contrato sin inventar filas de cobertura ni estados del Sheet.
- Pruebas Vitest de ruta, privacidad y estado vacio, y contrato docs/v5/c00-03-m04-01-coverage-surface.md; sin nuevo *.component.ts, endpoint, dependencia o dato sensible.
- CI Angular y Scope Lock exact SHA; auditoria A5 independiente posterior, rollback normal; no eventos entre Tasks, MySQL, PR, ramas ni QA/main/PROD.

## 2026-11-13 — V5 C00-02.M04.01: superficie privada de lectura del ciclo [Javier :50]
- Angular reutiliza PlataformaComponent con ruta /plataforma/flujo-v5 protegida por plataformaGuard y verificación server-side; muestra pasos contractuales, sin datos live ni mutaciones.
- Tests de contrato de ruta y regresión de guard; documentación docs/v5/c00-02-m04-01-platform-readonly-surface.md; no añade *.component.ts al inventario canónico.
- A0–A4 requieren Angular y Scope Lock SHA exacto. Sin API, MySQL, migraciones, eventos, PR, ramas ni QA/main/PROD; A5 independiente y rollback git revert.

## 2026-11-13 — V5 C00-03.M03.05: cola de evidencias de cobertura sin eventos [Javier :30]
- Backend Application V5CoverageEvidenceQueue integra la proyeccion de matriz C00-03 con cola inmutable, catalogo, actor/scope allowlist y SHA exacto, sin despachar Tasks.
- Tests xUnit V5CoverageEvidenceQueueTests cubren escenarios positivos/negativos, A5 no autorrevisable, stale SHA, duplicados, scope, capacidad y evidencia pendiente; contrato docs/v5/c00-03-m03-05-coverage-evidence-queue.md.
- .NET/seguridad/Scope Lock y CHANGELOG requieren checks causales; MySQL/Angular/Docker/E2E no aplican. Sin webhooks, IAM, secretos, PR, ramas, QA/main/PROD; rollback revert normal.

## 2026-11-13 - V5 C00-02.M03.05: cola segura sin dispatcher [Javier :30]
- Codigo real backend/src/Application/AutomationV5/V5WorkItemQueueSelector.cs selecciona una intencion CLAIM validada por PLAN G/O/DAG, catalogo, allowlist y SHA; no despacha ni crea Tasks.
- Tests xUnit V5WorkItemQueueSelectorTests y contrato docs/v5/c00-02-m03-05-queue-selector.md con positivos y negativos de actor, fuente, capacidad y colisiones.
- .NET y seguridad requieren CI causal; Scope Lock y CHANGELOG obligatorios, MySQL/Angular/Docker/E2E NO_APLICA. A5 pendiente independiente, rollback normal; no eventos ni webhook.

## 2026-11-13 — V5 C00-02.M03.04: adaptador aislado de estados y revisión CAS [Javier :10]
- `backend/src/Application/AutomationV5/V5WorkItemMemoryAdapter.cs` implementa estados in-memory con lock, token idempotente, autorización de actor, catálogo y validaciones Domain de G/O/DAG, ownership y A0–A4.
- Pruebas xUnit `backend/tests/Solqaryn.Tests/Application/V5WorkItemMemoryAdapterTests.cs` cubren positivos, denegaciones, una ganadora entre 32 reclamaciones concurrentes, SHA inválido, auditorías ausentes, tokens/owner y capacidad. Contrato en `docs/v5/c00-02-m03-04-workitem-isolated-adapter.md`.
- El adaptador no es CAS de Sheets/MySQL ni persistencia durable: no crea Tasks, roles ni endpoints, ni sustituye CI y A5. CI real .NET, seguridad, Scope Lock y CHANGELOG exact SHA antes de LISTO; revert normal como rollback.

## 2026-11-13 — V5 C00-03.M03.04: adaptador aislado para cobertura V5 [Javier :10]
- `backend/src/Application/AutomationV5/V5CoverageMemoryAdapter.cs` incorpora un adaptador de memoria con revision CAS, reintento idempotente, límites de capacidad y acceso fail-closed por actor/scope. Reutiliza el validador Domain de cobertura.
- `backend/tests/Solqaryn.Tests/Application/V5CoverageMemoryAdapterTests.cs` comprueba revisión, idempotencia, rechazos, aislamiento y 32 primeros escritores concurrentes con un solo ganador. Contrato explícito en `docs/v5/c00-03-m03-04-isolated-coverage-adapter.md`.
- El adaptador es aislado y efímero: no persiste en MySQL/Sheets, no sustituye un CAS distribuido, no modifica tenant JWT/RBAC ni ejecuta Tasks. CI .NET/seguridad y Scope Lock exact-SHA obligatorios antes de LISTO; A5 independiente. Rollback git revert.

## 2026-11-13 — V5 C00-03.M03.06: auditoría segura y logs estructurados de cobertura [Javier :10]
- `backend/src/Application/AutomationV5/V5CoverageAuditService.cs` implementa validación de cobertura V5 con `ILogger` y eventos tipados; logs con digest SHA-256 de ID validado, SHA causal, conteo y códigos seguros.
- Pruebas xUnit positivas/negativas en `backend/tests/Solqaryn.Tests/Application/V5CoverageAuditServiceTests.cs`: requisitos desconocidos, contenido privado, SHA inválido, duplicados, batch válido y sanitización de errores. Contrato en `docs/v5/c00-03-m03-06-coverage-audit.md`.
- No revela claves, referencias ni mensajes de excepción; no escribe BITACORA/Sheets ni modifica JWT, RBAC, tenant o MySQL. CI .NET/seguridad y Scope Lock exact-SHA obligatorios antes de LISTO; A5 independiente. Rollback git revert normal.

## 2026-11-13 — V5 C00-02.M03.06: auditoría estructurada de ciclo V5 [Javier :50]
- Implementación real `backend/src/Application/AutomationV5/V5WorkItemAuditLogger.cs`: logging estructurado de cambios de microtarea con formato estricto, códigos de error permitidos, UUID de correlación y rechazo sin emisión de entradas no seguras; no implementa CAS, BITACORA ni nueva autoridad.
- Pruebas xUnit positivas y negativas `backend/tests/Solqaryn.Tests/Application/V5WorkItemAuditLoggerTests.cs` y contrato `docs/v5/c00-02-m03-06-audit-logging.md`. A0–A3 proporcionales con .NET 10/seguridad, Scope Lock y provenance exact SHA para A4; sin MySQL, frontend, Docker, PR, ramas, QA/main/PROD; reversión `git revert`.

## 2026-11-13 — V5 C00-03.M03.02: servicio Application de cobertura integrada [Javier :00]
- `backend/src/Application/AutomationV5/V5RequirementCoverageProjectionService.cs` implementa proyección de matriz requisito→microtarea para IDs existentes con estado/owner/SHA verificados. Conserva faltantes explícitos, no concede PASS por una referencia documental ni crea microtareas, endpoints o persistencia.
- `backend/tests/Solqaryn.Tests/Application/V5RequirementCoverageProjectionServiceTests.cs` incorpora xUnit positivos y negativos: mapeo uno-a-varios, lote vacío, SHA inválido/obsoleto, snapshot ausente, ID/estado inválido y cobertura duplicada; contrato en `docs/v5/c00-03-m03-02-application-service.md`. Resultado CI .NET/seguridad/Scope Lock exact-SHA pendiente hasta su ejecución.
- Alcance Application puro, sin JWT/RBAC/tenant nuevo, MySQL, Angular, Docker, migraciones, worker ni despacho de eventos. A5 corresponde a otra Task Javier distinta; rollback mediante `git revert` normal, sin force-push.

## 2026-11-13 — V5 C00-02.M03.02: caso de uso Application V5 sin efectos externos [Javier :00]
- Se implementa `backend/src/Application/AutomationV5/V5WorkItemTransitionPlanner.cs`: propuestas inmutables de CLAIM, VALIDANDO y LISTO_DEV con expectativas de estado/owner; rechaza actor no autorizado y retorna intención nula ante gates negativos.
- Pruebas xUnit reales añadidas en `backend/tests/Solqaryn.Tests/Application/V5WorkItemTransitionPlannerTests.cs`; contrato y límites en `docs/v5/c00-02-m03-02-application-service.md`. CI .NET/seguridad/Scope Lock exact-SHA se verificará después de publicación; no atribuir PASS por anticipado.
- No implementa CAS, write a Sheets, endpoint, dispatcher, JWT nuevo, MySQL, migraciones ni acceso tenant; sólo preparación de transiciones. A5 corresponde a otra Task Javier; rollback mediante revert normal, sin reescritura.

## 2026-11-13 — V5 C00-03.M03.01: validación de dominio de cobertura integrada [Javier :50]
- Implementación .NET 10 en `backend/src/Domain/AutomationV5/V5RequirementCoverageRules.cs`: exige referencia institucional, ID del catálogo existente, versión, contrato, prueba y evidencia; rechaza duplicados compuestos y preserva correspondencias muchos-a-muchos sin abrir un backlog adicional.
- Pruebas xUnit `backend/tests/Solqaryn.Tests/Domain/V5RequirementCoverageRulesTests.cs` y contrato `docs/v5/c00-03-m03-01-coverage-domain.md`. A0–A3 con código real, CI dirigida a SHA exacto y Scope Lock; ninguna migración, nuevo dispatcher, endpoint, PR, rama ni promoción QA/main/PROD.

## 2026-11-13 — V5 C00-02.M03.01: reglas de dominio para cola V5 [Javier :50]
- C# real en `backend/src/Domain/AutomationV5/V5WorkItemRules.cs` valida identidad de microtarea, allowlist inyectado, G/O/DAG, exclusividad de owner, candidato, A0–A4, SHA exacto y A5 de revisor distinto; sin efectos externos ni simular CAS de Sheets.
- Pruebas xUnit en `backend/tests/Solqaryn.Tests/Domain/V5WorkItemRulesTests.cs` y contrato `docs/v5/c00-02-m03-01-domain-rules.md`; .NET y seguridad relevantes en CI exact-SHA, Scope Lock obligatorio. Sin nuevas ramas, PR, MySQL, Angular, cambios de tenants ni QA/main/PROD; rollback aditivo `git revert`.

## 2026-11-13 — V5 C01-01.M01.06: gates de identidad universal, rollback y salida documental M01 [Javier :40]
- Documento `docs/v5/c01-01-m01-06-gates-bloqueo-rollback-identidad.md`: siete prerequisitos verificados, sustitución A5 vigente Javier para la M01.05 histórica, fronteras JWT/RBAC/tenant, bloqueo/reversión y liberación M02 solo por preflight individual.
- Alcance TIPO1 documental: P01–P03/N01–N12, A0–A3 proporcionales y A4 Scope Lock + procedencia exact SHA; no modifica API/Angular/MySQL, no certifica runtime, no PR/ramas ni QA/main/PROD.

## 2026-11-13 — V5 C00-04.M01.06: gates de gobierno de cambios y liberación documental M01 [Javier :30]
- `docs/v5/c00-04-m01-06-bloqueo-rollback-liberacion.md` fija seis dependencias verificadas (cinco C00-04.M01 y C00-01.M06.06), A5 correctiva independiente, DAG VALIDADO, matriz de bloqueos, autorización, rollback y condición de evaluación M02 sin despacho automático.
- Solo contrato documental TIPO 1: A0–A3 proporcionales, revisión de fronteras JWT/RBAC/tenant y Scope Lock/procedencia exact-SHA para A4; A5 independiente de otra Task Javier. .NET/Angular/MySQL/Docker/E2E no aplican al diff, sin PR/ramas ni QA/main/PROD.

## 2026-11-13 — V5 C00-03.M02.06: cierre contractual de cobertura M02 [Javier :10]
- Documento `docs/v5/c00-03-m02-06-dependencias-salida-cobertura.md`: matriz R01–R12, seis fuentes M01/M02 con blobs verificables, dependencias y gates estrictos de salida, 4 positivos/12 negativos documentales; no implementación, activación de dispatcher ni M03 automático.
- A0–A3 documentales dirigidos, evidencia A:Z/26, JWT/RBAC/tenant, Scope Lock y validación de procedencia exact-SHA antes de A4; A5 independiente. Sin PR/ramas, QA/main/PROD, migraciones ni proveedores activos.

## 2026-11-13 — V5 C00-03.M02.05: contrato de eventos de cobertura integrada [Javier :10]
- Documento `docs/v5/c00-03-m02-05-eventos-correlacion-cobertura.md`: define tipos de evento lógicos, claves de cobertura e idempotencia, trazabilidad y límites del mapa requisito↔microtarea sin desplegar dispatcher, CAS, API, worker ni DB.
- Contrato documental TIPO 1: oráculos P01–P04/N01–N10, autoridad PLAN/COLA/DAG/BITACORA A:Z, cinco hashes de fuentes, seguridad tenant/JWT/RBAC, Scope Lock exact-SHA y A5 independiente; sin QA/main/PROD.

## 2026-11-13 — V5 C00-03.M02.04: configuración tipada de cobertura [Javier :00]
- `docs/v5/c00-03-m02-04-configuracion-cobertura.md`: contrato de configuración, ámbitos, 11 parámetros tipados propuestos y cuatro feature flags conceptuales no activos, con límites JWT/RBAC/tenant/CI y 4 positivos/10 negativos; sin código ni config real.
- TIPO 1: A0–A3 documentales sobre blob `d61ab68b98c828dfb09a84912cb15de88ce3e69a`, .NET/Angular/MySQL/Docker/E2E NO_APLICA a este diff. A4 commit FF con Scope Lock + V5 attribution exact-SHA, A5 independiente otra Javier; sin QA/main/PROD/branches/PR, rollback `git revert`.

## 2026-11-13 — V5 C00-03.M02.03: persistencia de matriz de cobertura [Javier :00]
- `docs/v5/c00-03-m02-03-persistencia-cobertura.md`: esquema lógico propuesto de vínculos requisito–microtarea–contrato–prueba–evidencia, índices y migración reversible futura. Fija 4 oráculos P01–P04 y 10 negativos N01–N10, sin crear SQL, EF migrations ni nuevo backlog.
- Entrega documental TIPO 1: A0–A3 de diff, fuente, seguridad y casos sobre blob `745e9a3eae0514ee83fe2de12a535fb084548c74`; .NET/Angular/MySQL/Docker/E2E NO_APLICA al diff. A4 requiere commit FF y Scope Lock SUCCESS exact SHA; A5_PENDIENTE de Task Javier diferente, sin QA/main/PROD, ramas ni PR. Reversión por `git revert`.

## 2026-11-13 — Rectificación causal de procedencia V5 C00-04.M01.01–05 (A5 pendiente)
- Se reagrupan siete viñetas preexistentes bajo cinco encabezados originales (DAG20, C01-02.M01.04 y C01-03.M01.03/.04/.05). Las secciones C00-04.M01.01–05 de Javier :10 conservan sus propias viñetas. No se borran pruebas ni entradas historicas.
- El validador `scripts/quality/verify-v5-changelog-provenance.mjs` y workflow `.github/workflows/v5-changelog-provenance.yml` detectan nuevas secciones vacías o atribuciones cruzadas sin compilar código runtime innecesariamente.
- Los A5_FAIL originales siguen vigentes hasta aprobación independiente sobre SHA correctivo por una Task Javier distinta de J10. No se declara A5_PASS automático ni se cambia QA/main/PROD. Rollback mediante revert normal en dev, nunca force-push.

## 2026-11-13 — V5 C00-02.M02.06: contrato de dependencias y salida M02 [Javier :00]
- `docs/v5/c00-02-m02-06-dependencias-salida.md`: integra M02.01–05 con prerequisitos, autoridad PLAN/COLA/DAG, transiciones e idempotencia BITACORA A:Z/26, ejemplo de ciclo y oráculos P01–P03/N01–N08; no crea código/servicio/DB.
- TIPO 1 documental: A0–A3 verificación de fuentes, casos, seguridad y ausencia de secretos sobre blob `95cdaef09583fc859aa25228057e1f2c22f60477`; .NET/Angular/MySQL/Docker/E2E NO_APLICA al diff; A4 posterior con commit FF y Scope Lock SHA exacto; A5 independiente de otra Javier. Sin QA/main/PROD, PR, ramas ni force-push; rollback normal `git revert`.

## 2026-11-13 — V5 C00-03.M02.02: interfaces, DTO y errores de la matriz integrada [Javier :40]
- Documento `docs/v5/c00-03-m02-02-contratos-api-dto-errores-cobertura.md`: contratos de lectura/mapeo/validación de cobertura, DTO propuestos, compatibilidad V5 FINAL, errores de seguridad y 16 oráculos P01–P04/N01–N12; no crea API/DB, nuevas Tasks ni sustituye PLAN H/I.
- TIPO 1 documental, A0–A3 sobre blob 803685cc695660ce2fadee3175b5d97e40e3f3ab; .NET/Angular/MySQL/Docker/E2E NO_APLICA al diff; A4 Scope Lock exact SHA/commit FF dev/readback y A5 independiente pendiente. Sin PR, ramas, force-push ni QA/main/PROD; rollback git revert.

## 2026-11-13 — V5 C00-03.M02.01: entidades e invariantes de la matriz integrada de cobertura [Javier :40]
- `docs/v5/c00-03-m02-01-entidades-ownership-invariantes-cobertura.md`: modelo lógico de doce entidades, catorce invariantes y correspondencia documental R01–R12 entre requisitos, mejoras, contratos, pruebas y evidencias, sin duplicar los 2.340 IDs ni declarar implementación.
- Entrega TIPO 1: A0–A3 documentales antes del commit sobre blob b6b16dfa1775ef07666d108580f6b21efe9b081a; casos P01–P04 y N01–N12 de aceptación contractual; .NET/Angular/Node/MySQL/Docker/E2E NO_APLICA al diff sin código. A4 exige commit FF dev, Scope Lock exact SHA y readback. A5 pendiente de otra Task Javier, sin QA/main/PROD, sin PR/ramas. Reversión git revert.

## 2026-11-13 — V5 C00-02.M02.03: modelo de persistencia, índices y migración reversible [Javier :40]
- `docs/v5/c00-02-m02-03-persistencia-indices-migraciones-datos-prueba.md`: especifica almacenamiento institucional PLAN/COLA/DAG/BITACORA A:Z/monitor, unicidad, índices relacionales **propuestos no implementados**, plan de migración futuro reversible y casos P01–P04/N01–N09 documentales; reconoce que Sheets no proporciona CAS seguro por una simple lectura/escritura.
- TIPO 1 documental: A0–A3 proporcionales sobre blob eb3110d591950728b325396a5cf7902d857fff3f; .NET/Angular/MySQL/Docker/E2E NO_APLICA a este diff y NO se ensayó migración/backup live. A4 exige commit FF, Scope Lock exact SHA y readback; A5 revisor distinto del allowlist Javier; sin QA/main/PROD, PR ni ramas; rollback git revert.

## 2026-11-13 — V5 C00-02.M02.02: contrato de API/DTO, errores y compatibilidad [Javier :40]
- Documento `docs/v5/c00-02-m02-02-api-dto-errores-compatibilidad.md`: contrato documental de lectura, claims, candidato, auditorías, eventos A:Z y cierre con errores lógicos, compatibilidad y casos positivos/negativos. Aclara que no existe CAS nativo demostrado ni nuevo endpoint/worker, con prohibición de elevar permisos tenant.
- TIPO 1 documental sin código ejecutable; A0–A3 revisión documental previa sobre blob 6e8170eea8fe05bba4f5610fced1d159719e03f1; .NET/Angular/MySQL/Docker/E2E NO_APLICA al diff; A4 requiere Scope Lock exact-SHA, readback institucional y A5 pendiente de otra Task Javier. Sin QA/main/PROD, ramas, PR ni force-push; rollback por git revert.

## 2026-11-13 — V5 C03-04.M01.02: actores y contratos de incidentes/continuidad [Javier :00]
- Nuevo `docs/v5/c03-04-m01-02-actores-entradas-salidas-continuidad.md`: 15 actores, 16 interfaces y 12 oráculos positivos/negativos para incidentes, revocación, backup, restore y continuidad, distinguiendo YAML/contratos de ejecución real; 15 predecesores DAG LISTO.
- Entrega documental, sin .NET, Angular, MySQL, Docker, E2E, secretos, datos ni despliegue; A0–A3 documentales, A4 Scope Lock exact-SHA después del FF en dev, A5 por otra Task Javier. Sin QA/main/PROD ni ramas/PR.

## 2026-11-13 — Seis Tasks Javier: multi-microtarea por corrida, vigilancia segura y monitor de última señal
- Se reemplazaron exclusivamente los prompts de seis Tasks Javier con bucle hasta agotar trabajo seguro o límites reales, CI independiente en VALIDANDO y verificación de compañeras con readback y exclusión atómica; sin tocar sus horarios, IDs, títulos ni is_enabled.
- Sheet V5: extendida TAREAS_PROGRAMADAS a AA:AN, añadido Dashboard A41:J51 y CONFIG A149:C150. No se tocaron PLAN_MAESTRO/COLA/BITACORA y sus nueve pestañas se conservan.
- Monitoring de estado nativo es fotografía fechada, no streaming; los reportes por corrida se actualizarán en futuras ejecuciones verificables. Dispatcher por eventos solo análisis futuro, ningún servicio nuevo ni relanzamientos ciegos.
- Rollback seguro: revert documental en dev mediante fast-forward, restauración controlada de prompts/sheet previa verificación; sin force-push, QA/main/PROD, secretos ni datos productivos.

## 2026-11-13 — V5 C03-03.M01.04: riesgos/supuestos/decisiones auditoría de seguridad [Javier :10]
- `docs/v5/c03-03-m01-04-riesgos-supuestos-decisiones-auditoria.md`: 14 riesgos R01–14, 8 supuestos S01–08 y 8 decisiones D01–08, anclados a ocho blobs de código y diez precedentes DAG LISTO. Delimita consultas A/B, auditoría best-effort, logging de secretos, correlación, retención y acceso excepcional sin afirmar una vulnerabilidad ni certificación runtime sin prueba.
- Autor 6af16bb9492c8191b8e8c7d606f11507; A0–A3 PASS documentales, .NET/Angular/MySQL/Docker/E2E NO_APLICA al diff de documentos. A4 exige Scope Lock exact SHA y readback, A5 revisor Javier independiente. Sin QA/main/PROD, permisos/infra/código ejecutable, ramas o PR; rollback normal git revert.

## 2026-11-13 — V5 C00-02.M02.01: entidades y exclusividad [Javier :20]
- Documento `docs/v5/c00-02-m02-01-entidades-ownership-invariantes.md` define diez entidades, 12 invariantes de identidad, DAG, owner, estados y SHA, ejemplo de ciclo y tres casos positivos/nueve negativos para evitar duplicados.
- Cambio exclusivamente documental, sin runtime: A0–A3 proporcionales, A4 Scope Lock SHA exacto, A5 otra Task Javier; no nuevas filas/Tasks, branches, PR ni QA/main/PROD. Rollback git revert.

## 2026-11-13 — V5 C00-02.M02.04: configuración tipada y flags seguros [Javier :20]
- Documento `docs/v5/c00-02-m02-04-configuracion-tipada-ambitos-flags.md` define frontera config entorno/tenant/usuario, cinco blobs existentes, validaciones tipadas y feature flags futuros no activos; tres casos positivos y nueve negativos con Scope Lock.
- Alcance solo documental, sin editar JSON/env/secretos, endpoints, JWT, DB ni Angular. A0–A3 proporcionales, A4 Scope Lock exact SHA, A5 por otra Task Javier. Sin QA/main/PROD, branches o PR; rollback git revert.

## 2026-11-13 — V5 C03-03.M01.02: actores/IO auditoría de seguridad [Javier :10]
- `docs/v5/c03-03-m01-02-actores-entradas-salidas-auditoria.md`: 12 actores ACT01–12, 14 entradas/salidas IO01–14, 10 casos futuros T01–T10 de auditoría de autorizaciones, denegaciones, cambios críticos, correlación, retención y no exposición; 8 blobs reales y 10 antecedentes DAG LISTO. No acredita HTTP/MySQL/export/alerta live ni acceso excepcional real.
- Autor Task 6af16bb9492c8191b8e8c7d606f11507; A0–A3 PASS documentales sobre candidato, A4 Scope Lock exact SHA + readback pendiente, A5 por otra Task Javier. No se modifican .NET, Angular, MySQL, Docker, secretos, QA/main/PROD, PR o ramas; rollback normal git revert.

## 2026-11-13 — V5 C00-02.M02.05: eventos, idempotencia y correlación [Javier :20]
- Documento `docs/v5/c00-02-m02-05-eventos-idempotencia-correlacion.md` define entidades lógicas, 26 columnas A:Z, EVENT_ID/IDEMPOTENCY_KEY, transiciones, SHA causal y rollback, tres casos positivos y nueve negativos; respeta DAG y seis Tasks Javier.
- Alcance estrictamente documental: A0–A3 verificaciones de artefacto/seguridad, A4 Scope Lock del SHA exacto, A5 independiente. No código, .NET/Angular/MySQL/Docker/E2E, PR, ramas ni QA/main/PROD. Sin certificación runtime ni CAS de Sheets inventado.

## 2026-11-13 — V5 C03-03.M01.03: inventario auditado de seguridad [Javier :10]
- `docs/v5/c03-03-m01-03-inventario-auditoria-seguridad.md`: ocho fuentes de API/servicio/modelo/repository/middleware/test con SHA, 12 controles de cobertura (3 positivos, 8 negativos, 1 control), brechas reales delimitadas: consulta A/B requiere revisar filtros globales; best-effort audit no garantiza durabilidad; correlación no es idempotencia y redacción/retención quedan pendientes.
- Autor 6af16bb9492c8191b8e8c7d606f11507. A0–A3 PASS documentales, sin runtime/.NET/Angular/MySQL/Docker/E2E por no cambiar código ejecutable. A4 Scope Lock exact-SHA y readback tras FF en dev; A5 independiente pendiente. No QA/main/PROD, no PR/branches, rollback `git revert`.

## 2026-11-13 — V5 C03-03.M01.05: aceptación de auditoría de seguridad [Javier :10]
- `docs/v5/c03-03-m01-05-criterios-auditoria-seguridad.md`: 16 criterios AU01–AU16 (autorización/denegación/cambio crítico/acceso excepcional, correlación, retención y no exposición), con ocho fuentes de código y diez prerrequisitos DAG LISTO. Delimita riesgos de tenant A/B, auditoría best-effort vs estricta, redacción PII y pruebas futuras sin declararlas ejecutadas.
- Autor 6af16bb9492c8191b8e8c7d606f11507; A0–A3 PASS documental antes del commit, A4 exact-SHA y A5 independiente pendientes. No ejecuta .NET/Angular/MySQL/Docker/E2E para un cambio de documentación; no modifica runtime, secretos, QA/main/PROD, ramas o PR; rollback `git revert`.

## 2026-11-13 — C00-03.M01.06: cierre M01 matriz integrada V5 [Javier :00]
- Documento docs/v5/c00-03-m01-06-cierre-matriz-integrada.md: 5/5 M01 LISTO con A5 causal, C00-01.M06.06 LISTO+A5, matriz de 12 referencias → mejora/contrato/prueba/cierre, 2 casos positivos y 12 negativos de gobierno documental.
- No crea backlog, cola, PR ni ramas; no ejecuta pruebas de runtime ni libera M02 sin sus puertas. A0–A4 proporcional y Scope Lock exacto; A5 de otra Task Javier, cero QA/main/PROD.

## 2026-11-13 — C00-02.M01.06: cierre documentado de M01 y puerta condicional M02 [Javier :00]
- Contrato docs/v5/c00-02-m01-06-bloqueo-rollback-liberacion-modelo.md: cinco M01 LISTO+A5, cierre C00-01.M06.06 LISTO+A5 causal, DAG VALIDADO, máquina de estados, CAS de owner, BITACORA 26 campos, dos casos positivos y doce negativos documentales.
- Preparación sin ejecución de despachador ni liberación ficticia M02; A0–A4 proporcionales, Scope Lock exact-SHA, A5 de otra Task Javier, sin PR/branches ni QA/main/PROD.

## 2026-11-13 — V5 C03-01.M01.05: aceptación observable de autenticación y recuperación (Javier :10)
- `docs/v5/c03-01-m01-05-aceptacion-autenticacion-recuperacion.md`: 16 criterios AC01–AC16 (2 positivos, 12 negativos, 2 controles) de login/renovar/revocación/recuperación futura, con oráculos HTTP/DB, 10 blobs de código/tests y 10 predecesores DAG LISTO. Diferencia evidencias estáticas/mocks de pruebas runtime exigidas a una futura implementación.
- Autor TASK_ID 6af16bb9492c8191b8e8c7d606f11507. A0–A3 PASS documental antes de publicar: calidad/no secretos, aceptación falsable, seguridad JWT/RBAC/tenant A/B y contratos. Dotnet/Angular/MySQL/Docker/E2E NO_APLICA a este cambio documental. A4 requiere Scope Lock del SHA publicado y readback; A5 pendiente de otra Task Javier. Sin APIs/DB/proveedores/QA/main/PROD/PR/ramas; rollback con git revert no destructivo.

## 2026-11-13 — V5 C03-01.M01.03: inventario de autenticacion y pruebas existentes (Javier :50)
- `docs/v5/c03-01-m01-03-inventario-contratos-codigo-evidencia.md`: ocho fuentes de codigo + tres tests con Git blobs, pruebas delimitadas de login, renovar, JWT, localStorage, tenancy y session-activity; trece controles pendientes para implementación futura.
- Separa archivos reales de mocks/EF InMemory; NO certifica recuperacion, revocacion JWT global, refresh rotativo, MFA/passkeys ni ataques HTTP 429. A0-A3 documentales antes del commit; A4 exact-SHA Scope Lock, A5 otra Task Javier. Sin runtime, secretos, QA/main/PROD, PR ni ramas; rollback git revert.

## 2026-11-13 — C03-02.M01.02: actores, IO y superficies RBAC/ABAC [Javier :00]
- Documento docs/v5/c03-02-m01-02-actores-entradas-salidas-superficies-rbac-abac.md: nueve actores conceptuales, catorce entradas/salidas y catorce escenarios documentales positivos/negativos futuros. Siete blobs de código contrastables; límites JWT/RBAC/tenant/propiedad y exportación.
- Únicamente preparación sin API/DB/UI ni tests runtime nuevos; DAG 10/10, A0–A4 proporcional, A5 otra Task Javier, Scope Lock SHA exacto, sin QA/main/PROD.

## 2026-11-13 — V5 C03-01.M01.02: actores y superficies de autenticación (Javier :50)
- Nuevo `docs/v5/c03-01-m01-02-actores-entradas-salidas-autenticacion.md`: diez actores, catorce entradas/salidas y catorce casos de aceptación FUTURA positiva/negativa. Distingue login/renovar existentes de recovery/MFA/passkeys y revocación global no acreditadas.
- Siete blobs Git revisados, A0-A3 documentales antes de publicar; .NET/Angular/MySQL/Docker/E2E NO_APLICA. A4 requiere commit fast-forward SHA exacto y Scope Lock; A5 a otra Task Javier. Sin runtime, nuevas cuentas, secretos, QA/main/PROD ni PR. Rollback con git revert.

## 2026-11-13 — C03-02.M01.01: objetivo y no-regresión RBAC/ABAC [Javier :00]
- Documento docs/v5/c03-02-m01-01-objetivo-no-regresion-rbac-abac.md define fronteras server-side de empresa, sucursal, propiedad, archivos y exportaciones y 16 criterios futuros positivos/negativos; 7 contratos Git cotejados por blob, DAG 10/10.
- Exclusivamente documental: no implementación ni PASS runtime de RBAC/ABAC; A0–A4 proporcionales, Scope Lock exact SHA y A5 independiente de otra Task Javier; sin JWT/roles/DB/UI/QA/main/PROD.

## 2026-11-13 — V5 C03-01.M01.01: contrato documental de autenticación y recuperación (Javier :50)
- `docs/v5/c03-01-m01-01-objetivo-autenticacion-recuperacion.md` fija objetivo, exclusiones y 16 casos de no-regresión FUTUROS para login, renovación, revocación, recovery, abuso, tenant A/B y MFA/passkeys solo con proveedor aprobado.
- Siete fuentes Git inspeccionadas; tipo 1 documental sin C# ni Angular/MySQL/Docker/E2E, A0-A3 verificación documental dirigida. A4 exige SHA nuevo y Scope Lock exacto. A5 pendiente de otra de cinco Tasks Javier; no QA/main/PROD. Rollback: git revert del commit.

## 2026-11-13 — V5 C02-04.M01.05: aceptación futura de consola global [Javier :40]
- Documento de 24 criterios de aceptación AC01–AC24: 6 positivos y 18 negativos FUTUROS para rol global separado de ERP, MFA/reautenticación, grants temporales, motivo, auditoría crítica, tenant A/B, PII y rollback. Seis fuentes Git respaldan limites, sin pruebas runtime atribuidas.
- TIPO1 documental, A0–A4 proporcional y Scope Lock SHA exacto; A5 de otra Task Javier. No crear permisos/MFA reales ni liberar M01.06/M02, QA/main/PROD.

## 2026-11-13 — C03-02.M01.04: riesgos de autorización y privacidad (Javier :50)
- Nuevo docs/v5/c03-02-m01-04-riesgos-supuestos-decisiones.md: R01–R12, S01–S06 y D01–D06 con trazabilidad a inventario y aceptación, cobertura BOLA/IDOR, claims falsos, sucursales, archivos y exportaciones. No certifica funcionamiento HTTP/MySQL ni aplica cambios JWT/RBAC.
- Documental A0-A4 proporcional, Scope Lock exact-SHA; A5 independiente de otra Task Javier. No cambia código, migraciones, Tasks, QA/main/PROD; rollback git revert.

## 2026-11-13 — V5 C02-04.M01.04: riesgos y decisiones de consola global [Javier :40]
- Matriz documental 15 riesgos R, 8 supuestos S y 10 decisiones D sobre privilegios globales, MFA/reauth, TTL, tenant A/B, PII, soporte DEV limitado y auditoría crítica. Fuentes Git verificadas; mecanismos futuros no implementados ni certificados.
- TIPO1 sin runtime/secretos ni migraciones; A0–A4 documentales y Scope Lock exact SHA; A5 pendiente independiente Javier, sin liberar M01.06/M02 ni QA/main/PROD.

## 2026-11-13 — C00-01.M06.06: cierre documental con A5 causal y mapa DAG (Javier :50)
- Se incorpora docs/v5/c00-01-m06-06-cierre-baseline-mapa-dependencias.md: 35/35 predecesoras LISTO, causal A5 de M04.03 (Chromium 3/3, revisor independiente), cuatro dependientes dinámicos, inventario de blobs y contradicciones. El cierre no despacha automáticamente ni autoriza QA/PROD.
- A0-A4 proporcionales a documento, Scope Lock exact-SHA y readback PLAN/COLA/DAG/BITACORA, A5 independiente pendiente. Sin runtime/DB/UI, Tasks, branches o PR; rollback git revert.

## 2026-11-13 — V5 C02-04.M01.03: auditoría de contratos y evidencia de consola global [Javier :40]
- Inventario documental de diez archivos/blobs observados en dev, capacidades existentes parciales y controles NO_CERTIFICADOS de MFA, reautenticación, grants TTL, auditoría crítica, rollback y consola global. Tres positivos/seis negativos futuros propuestos, no pruebas live.
- TIPO1 documental; A0-A4 proporcional, Scope Lock SHA exacto y A5 independiente. Sin cambios de JWT/RBAC/Angular/.NET/MySQL, grants, datos ni QA/main/PROD.

## 2026-11-13 — V5 C02-04.M01.02: actores y entradas/salidas de consola global [Javier :40]
- Matriz documental de 8 actores, 12 IO y 9 casos (+/-) FUTUROS para consola global: separación de plataforma/ERP, MFA/reautenticación, motivos, grants temporales, aislamiento tenant A/B y auditoría crítica, con seis fuentes Git por blob.
- M01.02 TIPO1 sin cambios .NET/Angular/MySQL/secretos. A0–A4 documentales, Scope Lock exact-SHA, A5 ajeno; no habilita M01.06, MFA real ni QA/main/PROD.

## 2026-11-13 — Reparación de gate de calidad ERP para censo V5 de seis Tasks
- `scripts/quality/priority4_quality_audit.py`: mantiene las comprobaciones estrictas de seguridad e identidad, pero actualiza la aserción obsoleta de ocho Tasks a seis Tasks y comprueba `PHYSICAL_TASKS_REGISTERED=6`.
- `docs/AUTOMATION_STATUS.md`: declara explícitamente `LEGACY_AUTOMATION_RUNTIME=RETIRED`, conservando `VAEP_DISPATCH_AUTHORIZED=FALSE` y `JULES_WORKERS_AUTHORIZED=0`. No se activan motores ni cuentas nuevas.
- El cambio solo alinea la política CI vigente; no modifica frontend/backend, secrets, QA/main/PROD ni las evidencias independientes de M04.03.

## 2026-11-13 — C03-02.M01.05: aceptación RBAC/ABAC documental (Javier :50)
- Nuevo docs/v5/c03-02-m01-05-criterios-aceptacion-tenant-privacidad.md: 14 criterios observables, incluidos BOLA/IDOR, claims falsos, sucursal cruzada, archivos, export y revocación; 6 reglas de no-regresión y rutas de futura prueba HTTP+MySQL/Playwright. Son contratos futuros, no se declaran PASS live.
- DAG VALIDADO 10/10; Scope Lock exact-SHA, A0-A4 documental proporcional y A5 pendiente de otra Task Javier. Ningún cambio runtime, QA/main/PROD, PR, rama ni Tasks.

## 2026-11-13 — C03-02.M01.03: inventario de autorizacion/privacidad V5 (Javier DEV 06)
- Se incorpora docs/v5/c03-02-m01-03-inventario-autorizacion-privacidad.md: 9 contratos Git con blobs, 5 suites de pruebas existentes y matriz de brechas BOLA/IDOR, claims falsos, sucursal cruzada y exportacion ajena. Alcance M01.03 documental; no se interpreta como certificacion de ABAC o integracion live.
- DAG VALIDADO y 10/10 prerrequisitos documentales LISTO; A0 revisa estructura/secretos, A1-A3 proporcionales al documento, A4 requiere SHA de publicacion y Scope Lock. A5 de otra Task Javier; rollback por git revert normal. Sin cambios C#/Angular/MySQL, Tasks, QA/main/PROD, ramas o PR.

## 2026-11-13 — V5 C02-04.M01.01: objetivo/no-regresión de consola global auditada [Javier :40]
- Contrato documental M01.01 de separación plataforma/ERP, MFA/reautenticación, motivo, acceso excepcional temporal y auditoría crítica; 15 criterios NR (+/-) FUTUROS, seis fuentes Git verificadas por blob SHA.
- TIPO1 sin código ejecutable ni cambio de JWT/RBAC/MySQL/Angular/secretos; DAG C02-01/02/03 M01.01-05 prerequisitos LISTO. A0–A4 documentales proporcionales, Scope Lock exact-SHA y A5 independiente; no habilitar M01.06/M02, QA/main/PROD.

## 2026-11-13 — M04.03: regresión Chromium real A→login y A→B; A5 sujeto a CI
- Añadido `frontend/e2e/c00-01-m04-03-route-context.spec.ts` con HTTP público retenido en navegador, una prueba positiva de branding y dos negativas de SEO/tema después de navegar sin recarga. No se simula Angular Router ni AppComponent.
- Añadido workflow dirigido `.github/workflows/v5-m04-03-browser-regression.yml`, que ejecuta lint, build, unitarios y Playwright Chromium en entorno descartable; no requiere deployment Vercel ni toca QA/main/PROD.
- Histórico A5_FAIL permanece hasta evidencia CI exact-SHA y revisión independiente distinta del autor. Rollback normal por revert en dev.

## 2026-11-13 — Orden vigente: SOLQARYN con seis Tasks Javier y Alex retirado
- Por orden expresa del propietario, el allowlist comercial y el despacho operativo quedan restringidos exclusivamente a las seis Tasks Javier existentes (:00, :10, :20, :30, :40, :50). DEV 07 y DEV 08 Alex no conservan permisos institucionales de despacho, reactivación, relevo ni revisión A5. Sus Tasks privadas se reportaron pausadas, sin lectura física desde esta cuenta.
- Se actualizaron `docs/AUTOMATION_STATUS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `AGENTS.md` y el Google Sheet V5 (CONFIG, TAREAS_PROGRAMADAS, AUTOMATIZACIONES). Se mantuvieron ID, RRULE y habilitación de Javier. Evidencia histórica en BITACORA, Git y PLAN_MAESTRO conservada, sin interpretación como autorización activa.
- Única microtarea con owner Alex no LISTO identificada: C01-04.M01.01 en VALIDANDO. Instrucción de rescate exclusiva a una Task Javier con lectura física, ausencia de lease/escritura concurrente, CAS de owner y A0–A5 reales; no se ha falseado transición LISTO ni A4. Corrección de clave de compatibilidad de self-test de Skill, sin reintroducir VAEP/Jules. Alcance documental, sin tocar frontend/backend/DB ni QA/main/PROD. Reversión: commit inverso normal y restauración auditada del Sheet, no force-push.

## 2026-11-13 — Rescate cruzado seguro entre Tasks [orden expresa propietario]
- Se autoriza a un ejecutor disponible a resolver bloqueos de microtareas ajenas mediante ayuda con pruebas/CI o correcciones causales, conservando el owner activo. Handoff exclusivo solo sin lease activo, compare-and-swap y eventos idempotentes; revisor A5 siempre independiente. Alex y Javier conservan separación de cuentas y permisos. Ninguna Task ni su horario cambia. 
- Cambia el gobierno canónico AGENTS, AUTOMATION_V13 y AUTOMATION_STATUS, sin alterar código, DAG, asignaciones, QA/main/PROD. Rollback normal git revert, no force-push.

## 2026-11-13 — V5 C02-03.M01.05: criterios de aceptación del ciclo de vida tenant [Javier :20]
- Contrato documental preliminar AC01–AC24: seis positivos y dieciocho negativos FUTUROS para alta/suspensión/reactivación/archivo/baja lógica idempotentes, concurrencia, JWT/RBAC/tenant A/B, retención y rollback; cinco fuentes Git con SHA.
- Sin ejecución runtime ni cambios de entidades/datos/CI; DAG19 documental, A0–A4 proporcional, Scope Lock exact-SHA y A5 otro TASK_ID. No liberar M01.06/M02 ni QA/main/PROD.

## 2026-11-13 — V5 C02-03.M01.03: contratos, código y evidencia lifecycle tenant [Javier :30]
- Inventario documental de seis fuentes Git con SHA, límites de autorización y seis capacidades futuras de alta/suspensión/reactivación/archivo/baja lógica/reparación; sin atribuir runtime implementado.
- A0–A3 dirigidos: 6/6 rutas+hashes, 19/19 prerequisitos DAG LISTO, matriz de seguridad JWT/RBAC/tenant A/B y negativas. No cambia .NET/Angular/MySQL/Docker, CI técnica no aplicable; Scope Lock exact-SHA y A4 pendientes de pospublicación. A5 por otro TASK_ID, sin QA/main/PROD.

## 2026-11-13 — V5 C02-03.M01.01: objetivo y no-regresión de aprovisionamiento [Javier :10]
- Documentación exclusiva del ciclo alta/suspensión/reactivación/archivo/baja lógica idempotente, ocho conceptos y 16 criterios NR (+/−), sobre 6 blobs existentes.
- No crea ruta de aprovisionamiento ni migración; A0–A4 proporcional documental, Scope Lock exact-SHA y A5 pendiente ajeno; no QA/main/PROD.

## 2026-11-13 — V5 C02-03.M01.04: riesgos, supuestos y decisiones del lifecycle tenant [Javier :20]
- Matriz documental preliminar R01–R12, S01–S06 y D01–D08 de altas, suspensiones, reactivaciones, archivo y baja lógica; pruebas positivas/negativas futuras, cinco fuentes Git con SHA.
- Sin cambios de runtime, MySQL, IAM, secretos ni datos, sin afirmación de pruebas live. Gate DAG documental 19 prerequisitos; A0–A4 documentales con Scope Lock exact-SHA; A5 independiente, sin liberar M01.06/M02 ni QA/main/PROD.

## 2026-11-13 — V5 C02-03.M01.02: actores e interfaces de ciclo tenant [Javier :20]
- Preparación documental M01.02 de aprovisionamiento: ocho actores, doce contratos IO, cuatro positivos y doce negativos futuros para alta/suspensión/reactivación/archivado/baja lógica idempotentes; cinco archivos de dominio/seguridad verificados.
- Sin implementación, jobs, datos ni migraciones reales; A0–A4 documentales, Scope Lock exact-SHA, A5 distinta Task; no habilita M01.06/M02 ni QA/main/PROD.

## 2026-11-13 — V5 C02-02.M01.02: actores, entradas y superficies shared/dedicated [Javier :20]
- Matriz documental de ocho actores y doce interfaces IO01–IO12 de placement server-side, shared/dedicated y migración reversible; cuatro positivos y catorce negativos propuestos, seis blobs backend verificados.
- TIPO1 documental; sin router/migración ejecutados, sin DSN del cliente ni cambios .NET/Angular/MySQL/Docker/E2E. Requiere seguridad documental, Scope Lock exact-SHA, A0–A4 proporcionados, A5 independiente; no liberar M01.06/M02 ni QA/main/PROD.

## 2026-11-13 — V5 C02-02.M01.03: inventario shared/dedicated [Javier :00]
- Inventario documental de diez fuentes Git con hash, límites de routing, EF, tenant, hosts, jobs y backup; 2 positivos/4 negativos; no modificaciones runtime ni conexión dedicada.
- Preparación provisional por DAG VALIDADO, Scope Lock exact-SHA y A5 independiente; no habilita M01.06/M02.

## 2026-11-13 — V5 C01-04.M01.05: aceptación funcional y técnica de expediente [Javier :10]
- Documento de 18 criterios AC-01–18 para timeline, decisiones, observaciones, adjuntos, versiones, tenant A/B, concurrencia, UI y comprobación CI causal; incluye negativos y gates por superficie.
- Sin implantación ni tests runtime; A0–A4 documentales, Scope Lock SHA exacto, A5 independiente; no liberar M01.06/M02, ni QA/main/PROD.

## 2026-11-13 — V5 C02-02.M01.05: aceptación shared/dedicated [Javier :00]
- Contrato documental con 20 criterios AC01–AC20, 6 positivos/14 negativos, seis fuentes Git, tenancy, DSN, backfill, migración y rollback; no se crea infraestructura ni se hacen cambios ejecutables.
- DAG permite preparación M01.01–05, no M01.06/M02; Scope Lock exact-SHA y A5 revisión independiente.

## 2026-11-13 — V5 C01-04.M01.04: riesgos y decisiones del expediente universal [Javier :10]
- Registro documental EXP-R01–12, EXP-S01–06 y DEC-01–08 sobre 10 fuentes reales, con riesgo de aislamiento de auditoría, adjuntos legacy, secuencia y seguridad sin certificar vulnerabilidades.
- Sin cambios ejecutables ni despliegues; A0–A4 proporcionales, Scope Lock exact-SHA y A5 pendiente de revisión ajena; rollback por revert normal.

## 2026-11-13 — V5 C02-02.M01.04: riesgos y decisiones shared/dedicated [Javier :00]
- Preparación documental: matriz R01–R10, supuestos S01–S05 y decisiones D01–D06 sobre placement, router, migración, aislamiento y rollback; seis fuentes Git; ningún cambio ejecutable.
- DAG VALIDADO documental; no habilita router, base dedicada, DSN, secretos, MySQL, M01.06/M02 ni QA/main/PROD. Validar Scope Lock y A5 independiente.

## 2026-11-13 — V5 C02-02.M01.01: objetivo shared/dedicated y no-regresión [Javier :30]
- Documento preliminar de placement server-side compartido/dedicado, fail-closed y rollback, con 6 blobs actuales y 13 casos NR (3 positivos, 10 negativos) previstos, sin conexión, migración ni pruebas live.
- A0–A4 documentales con Scope Lock SHA exacto, A5 independiente. No se modifican DSN, Aiven, JWT/RBAC, secretos, QA/main/PROD, ramas o providers.

## 2026-11-13 — V5 C01-04.M01.03: inventario documental de contratos para expediente universal [Javier :10]
- Lectura de 10 blobs reales: auditoría controller/DTO/service/repository/domain, storage de compras, contexto tenant y frontend; brechas de cronología, consulta tenant, adjuntos y versionado sin asumir API de expediente existente.
- Documental; A0–A4 proporcionales, Scope Lock SHA exacto y A5 independiente. No cambian runtime/.NET/Angular/MySQL/Docker/E2E, qa, main ni PROD.

## 2026-11-13 — V5 C02-01.M01.05: aceptación documental contexto tenant [Javier :00]
- Matriz de 18 casos AC01–AC18 (5 positivos, 13 negativos), ocho archivos Git y requisitos futuros de host/JWT/RBAC/membresía. Sin modificaciones de runtime, MySQL, secretos ni permisos.
- Preparación DAG VALIDADO, A0–A4 documentales proporcionales, Scope Lock exact-SHA y A5 por otro TASK_ID; revalidar antes de M01.06/M02.

## 2026-11-13 — V5 C02-01.M01.02: actores, IO y superficies de contexto tenant [Javier :30]
- Matriz documental provisional de 8 actores, 10 interfaces/entradas y salidas, 3 positivos y 10 negativos futuros, con 6 blobs de servidor reales y límites de host/JWT/membresía sin autoridad de cliente.
- Sin implementar API/DB/Angular, DNS ni workers; A0–A4 proporcionales con Scope Lock exact SHA, BITACORA A:Z y revisión A5 distinta. QA/main/PROD no autorizados.

## 2026-11-13 — V5 C02-01.M01.03: inventario de contratos de contexto tenant [Javier :00]
- Preparación documental TIPO1: 12 fuentes de Git, host exacto, JWT, membresía, API, filtros, audit y límites de evidencia; aceptación documental P01-P02/N01-N04; no cambios ejecutables.
- Conservar DAG validado, no adelantar M01.06/M02; A0-A4 documentales y Scope Lock exact SHA; A5 revisión por otra Task.

## 2026-11-13 — V5 C01-04.M01.02: actores, entradas, salidas y superficies de expediente universal [Javier :10]
- Preparación documental exclusiva M01.02: siete perfiles de actor, diez interfaces EXP-IO y seis referencias de código real con límites JWT/RBAC/tenant A/B, adjuntos y versionado.
- No implementa expediente ni API universal. .NET/Angular/MySQL/Docker/E2E no aplican al diff documental; A0–A4 proporcionales y Scope Lock SHA exacto; A5 independiente; sin QA/main/PROD.

## 2026-11-13 — V5 C02-01.M01.01: objetivo y no-regresión tenant server-side [Javier :30]
- Preparación documental preliminar del contexto host/identidad/membresía/ámbito; matriz NR-01–NR-12 con tres positivos y nueve negativos propuestos, cuatro blobs originales contrastables y alcance sin código ejecutable.
- A0–A4 documentales proporcionales y Scope Lock exact-SHA; A5 independiente. Sin migración, autorización nueva, DNS, QA/main/PROD ni pruebas runtime atribuidas.

## 2026-11-13 — V5 C02-01.M01.04: matriz documental de riesgos del contexto tenant [Javier :00]
- Registro inicial de riesgos R01–R10, supuestos S01–S05, decisiones D01–D06, fuentes backend y casos documentales positivos/negativos; no modifica ejecutables, datos, permisos ni infraestructura.
- Preparación provisional DAG VALIDADO. Scope Lock y A4 exact-SHA exigibles; A5 por otra Task y revalidación al cerrar contratos C00/C01.

## 2026-11-13 — V5 C01-04.M01.01: objetivo documental expediente universal [Alex :05]
- Contrato preliminar de timeline, decisiones, observaciones, adjuntos y versionado; define alcance/no-alcance y 12 invariantes positivas/negativas para aislamiento tenant/JWT/RBAC, adjuntos, idempotencia, PII, regresión y rollback.
- Únicamente M01.01 documental: no crea endpoints, datos, UI, migraciones, workers ni adjuntos; .NET/Angular/MySQL/Docker/E2E no aplican. Requiere A0–A4 causal, Scope Lock exact-SHA y A5 independiente; sin QA/main/PROD.

## 2026-11-13 — Autoactivación VALIDANDO por DAG24 (gobierno reactivo)
- El propietario exige reponer la cola al pasar prerequisitos a VALIDANDO. Se especifica gate condicional solo para tareas documentales independientes y A5/gates estrictos para cierres y cambios críticos. Fórmulas DAG_TECNICO!G y COLA!G/O se recalculan automáticamente al cambiar PLAN_MAESTRO; las Tasks hacen el despacho real.
- Fase preparatoria autorizada: C01-04, C02-01, C02-02, C02-03 M01.01-05 (20); cuatro M01.06 preparadas pero bloqueadas por cierres y A5. C00-01.M06.06 exige causal A5 independent sobre M04.03. Reposición futura ante reserva baja se gobierna por verificación técnica por ID, nunca saltando dependencias ni creando tareas ficticias.
- Respaldo previo del Sheet y Doc rector confirmado en Drive; cambio solo de gobierno y fórmulas con lectura posterior, sin tocar aplicación, CI ni prompts/horarios físicos. Rollback normal git revert y restauración de snapshot de hoja si fuera necesario, sin force-push.

## 2026-11-13 — V5 C01-01.M01.05: aceptación preliminar identidad universal [Javier :30]
- Matriz documental ID01–ID16 para Empresa, Sucursal, Usuario, membresías, rol tenant, recursos, seguridad JWT/RBAC y criterios positivos/negativos.
- No implementa la fase ni prueba runtime; A0–A4 proporcionales con Scope Lock SHA exacto y A5 independiente. Sin QA/main/PROD.

## 2026-11-13 — V5 C01-01.M01.03: inventario de contratos de identidad [Javier :10]
- Inventario documental de siete blobs reales de empresa, sucursal, usuario, membresía, rol, contexto tenant y autenticación; dos positivos y ocho negativos de seguridad/alcance.
- TIPO1 documental: sin runtime, .NET/Angular/MySQL/Docker/E2E no aplican; A0-A4 proporcionales y Scope Lock exact-SHA, A5 independiente; sin QA/main/PROD.

## 2026-11-13 — V5 C01-01.M01.04: riesgos, supuestos y decisiones de identidad universal [Javier :50]
- Registro documental R-ID-01–12 y supuestos S-01–06, vinculados a 8 blobs reales de dominio, aplicación, infraestructura y M01.01; fronteras A/B, legacy, claims, contexto y bypass bajo decisión pendiente.
- Solo documentación; ningún cambio runtime, EF/MySQL, Angular, permisos, secretos o despliegues. A0–A4 proporcionales y Scope Lock exact-SHA; A5 independiente para QA/main/PROD.

## 2026-11-13 — V5 C01-02.M01.01: alcance y no regresión capabilities [Javier :30]
- Objetivo documental, límites, decisiones de M02 y 12 invariantes NR01–NR12 para permiso explícito, entitlement, tenant, sucursal, recurso y soporte.
- Sin código ejecutable ni autorización nueva; A0–A4 proporcionales, Scope Lock SHA exacto, A5 por Task distinta; no QA/main/PROD.

## 2026-11-13 — V5 C01-01.M01.02: actores, IO y fronteras de identidad [Javier :40]
- Análisis documental con siete actores, diez entradas/salidas candidatas, referencias a seis blobs de dominio, tres positivos y ocho negativos de tenancy/RBAC/JWT; no implanta API/DB/Angular.
- Protocolo A0–A4 proporcional con Scope Lock exact-SHA, BITACORA A:Z/26 y A5 por Task distinta; no QA/main/PROD.

## 2026-11-13 — V5 C01-02.M01.03: inventario verificable de contratos [Javier :30]
- Inventario de 7 blobs de código/tests con limitaciones explícitas, matriz de fuentes V5, siete brechas de implementación y casos positivos/negativos documentales.
- Ninguna prueba runtime atribuida; Scope Lock exact-SHA para commit solo documental, A0–A4 proporcionales y A5 otra Task; no QA/main/PROD.

## 2026-11-13 — V5 C01-01.M01.01: objetivo y no-regresión de identidad universal [Javier :40]
- Documento inicial: frontera tenant/sucursal/membresía/rol/plataforma, seis fuentes de código con blob SHA y doce pruebas positivas/negativas propuestas (no ejecutadas en runtime).
- Excluye cambios de código, EF/MySQL, Angular, secretos, proveedor y QA/main/PROD. A0–A4 documentales con Scope Lock exact-SHA; A5 independiente pendiente.

## 2026-11-13 — V5 C01-02.M01.05: criterios de aceptación de capabilities [Javier :30]
- Matriz preparatoria AC01–AC16 de aceptación funcional/técnica y seis negativos documentales para RBAC, entitlement, tenant, sucursal y recurso.
- No implementa autorización ni prueba runtime; A0–A4 proporcionales al documento, Scope Lock exact-SHA, A5 independiente; sin cambios QA/main/PROD.

## 2026-11-13 — V5 C01-03.M01.01: objetivo, no-alcance y no-regresión de configuración dinámica [Javier :40]
- Preparación exclusivamente documental: mapa de fronteras, límites de implementación y NR-01–NR-10 positivos/negativos; no activa configuraciones ni modifica runtime, secretos, permisos, DB o proveedores.
- Seguridad JWT/RBAC/tenant, inyección, concurrencia, BITACORA A:Z y rollback delimitados. A0–A4 proporcionales; Scope Lock exact-SHA, A5 independiente pendiente; no QA/main/PROD.

## 2026-11-13 — V5 C01-02.M01.02: actores, entradas y superficies de capabilities [Javier :30]
- Matriz preparatoria de doce actores/consumidores, entradas y salidas, fronteras JWT/RBAC/tenant/sucursal/recurso y seis negativas; decisiones comerciales de M02 siguen pendientes.
- Sin cambios de runtime, permisos, DTO, MySQL ni frontend; A0-A4 documentales con Scope Lock exact-SHA, A5 independiente; no QA/main/PROD.

## 2026-11-13 — V5 C01-02.M01.04: riesgos y decisiones preliminares de capabilities [Javier :30]
- Inventario documental R01–R11, supuestos S01–S06, decisiones D01–D06 y positivos/negativos para RBAC/plan/rubro/tenant/sucursal/recurso, basado en contratos reales.
- Sólo M01.04 documental; no habilita capabilities, modifica API/Angular/MySQL ni certifica runtime. Aplicar A0–A4 proporcionales, Scope Lock exact-SHA y A5 por Task distinta. No QA/main/PROD.

## 2026-11-13 — V5 C00-04.M01.05: criterios de aceptación observables [Javier :10]
- Contrato documental AC01–AC12, P1/P2, N1–N7 y D01–D05 con gates de autorización, DAG, CI exact-SHA, seguridad, BITACORA A:Z y rollback; sin runtime.
- A0–A4 dirigidos, Scope Lock en el SHA publicado y A5 pendiente de revisor independiente; QA/main/PROD no autorizados.

## 2026-11-13 — V5 C01-03.M01.02: actores, límites y entradas/salidas de configuración [Javier :20]
- Se identificaron 6 actores, 8 entradas/salidas propuestas, cuatro superficies fuente con blobs verificables y 2 positivos/6 negativos de tenancy/RBAC/inyección, ámbito/versiones/errores; propuesta M01 sin contrato final ni cambio de runtime. A0–A3 documentales; A4 Scope Lock SHA exacto y readback; A5 revisor distinto. No QA/main/PROD.

## 2026-11-13 — V5 C01-03.M01.03: inventario preliminar de contratos y evidencia [Javier :20]
- Se leyeron y fijaron cuatro blobs de código real (TemaVisual DTO/metadata UI, TemaVisualService HTTP, ThemeApplierService hex allowlist y Program.cs) con afirmaciones y límites explícitos; seis casos documentales D-01–06 positivos/negativos de JWT, RBAC, tenancy e inyección. No se certifica API universal ni MySQL/Angular/browser. A0–A3 documentales, A4 exact-SHA Scope Lock y A5 por otra Task; sólo M01, sin QA/main/PROD.

## 2026-11-13 — V5 C00-04.M01.04: registro de riesgos y decisiones pendientes [Javier :10]
- Matriz documental R01-R10, supuestos, decisiones D01-D05, cinco negativos, gates de seguridad/identidad/CI/tenant/SHA/rollback y relación con A5_FAIL histórico sin certificar PROD.
- Sin cambio ejecutable; A0-A4 proporcionales, Scope Lock SHA exacto tras publicar, BITACORA A:Z/26; A5 independiente y no QA/main/PROD.

## 2026-11-13 — V5 C01-03.M01.04: matriz preliminar de riesgos de configuración [Javier :20]
- Documento de preparación M01 para configuración dinámica: diez riesgos R01-R10, cinco supuestos S01-S05, seis decisiones D01-D06 y tres escenarios negativos de integridad/aislamiento/inyección. Referencias V5/DAG, seguridad JWT/RBAC/tenant y rollback, sin implementación ni migración; no se declaran pruebas .NET/Angular/MySQL. A0–A4 documentales y A5 independiente requeridos. No QA/main/PROD.

## 2026-11-13 — V5 C00-04.M01.03: inventario de contratos, código y evidencias [Javier :10]
- Inventario documental con fuentes GitHub de SHA/blobs, Plan V5, Sheet institucional y DAG; prueba de límites de inferencia, permisos y autorizaciones; criterios positivos/negativos para no confundir CI, fuente y certificación.
- Sin cambios runtime; A0–A4 proporcionales, Scope Lock exact-SHA de publicación, A5 independiente, BITACORA A:Z/26, rollback normal; no QA/main/PROD.

## 2026-11-13 — V5 C01-03.M01.05: aceptación preliminar de configuración tipada [Javier :20]
- Se fija documento M01 de 10 criterios observables con positivos/negativos de JWT/RBAC, tenant A/B, inyección, versionado, errores y rollback. Sólo preparación documental; no runtime, DB, providers ni migración. A0–A3 de integridad documental; A4 Scope Lock del commit exacto y readback, A5 por otro TASK_ID. No QA/main/PROD; revalidar C00-01 y C01-01 antes de fases ejecutables.

## 2026-11-13 — V5 C00-04.M01.02: actores y superficies gobierno de cambios [Javier :10]
- Inventario contractual documental de entradas/salidas, actores Javier/Alex, Drive/Sheets, GitHub, control de estados, interfaces, decisiones y riesgos; positivo y cinco negativos, BITACORA A:Z/26, sin código runtime.
- Validar A0–A4 proporcionales, Scope Lock SHA de commit documental y readback; A5 independiente, no QA/main/PROD. Rollback git revert ordinario.

## 2026-11-13 — Ampliacion V5 DAG20 de preparacion documental independiente [gobierno]
- Backups nativos del Sheet y Plan V5 verificados. Se registran 20 IDs ya existentes M01.01-05 de C00-04 y C01-01/02/03 con once prerequisitos cerrados, sin ciclo, como documentos preparatorios. Gate C00-01.M06.06 individual, no apertura de implementacion ni QA/main/PROD.
- El Doc rector, DAG_TECNICO, AGENTS, AUTOMATION_V13, AUTOMATION_STATUS, CONFIG y Jira se sincronizan con lectura posterior; los seis prompts y horarios Javier y dos Alex no se modifican. Rollback GitHub por git revert sin force-push; desactivar filas nuevas del DAG (F=EN_REVISION) antes de revertir el contrato.

## 2026-11-13 — V5 C00-04.M01.01: alcance de gobierno de cambios [Javier :10]
- Contrato documental de decisiones, autorizaciones, riesgos, gates, casos positivos/negativos y rollback; distingue preparación M01 de implementación/deploy. Fuente V5; sin cambios de runtime, base de datos, secretos o configuraciones.
- A0–A4 documentales requeridas y A5 independiente antes de QA/main/PROD; Scope Lock exact SHA al publicar, BITACORA A:Z/26 e idempotencia. Autor conserva TASK_ID original.

## 2026-11-13 — V5 C00-02.M01.01: rectificación documental A5_FAIL BITACORA A:Z/26 [Alex :05]
- Se corrige la referencia obsoleta A:U/21 a A:Z/26, EVENT_ID A, IDEMPOTENCY_KEY R, TASK_ROLE E, ACCOUNT_OWNER F y TASK_ID G, y se añaden negativos reproducibles para eventos truncados de 21 a 25 campos, claves duplicadas y auto-revisión.
- A0–A3: revisión estática y de contrato documental frente a encabezado vivo de 26 columnas; .NET/Angular/MySQL/Docker/Playwright NO_APLICA al no editar código ejecutable. A4 requiere commit fast-forward dev y Scope Lock del SHA publicado; A5 por otro TASK_ID sobre nuevo SHA, conservando el A5_FAIL histórico. Rollback con git revert ordinario, sin cambios a QA/main/PROD.

## 2026-11-13 — V5 C00-01.M06.05: solicitud documental de A5 independiente y matriz de riesgos [Javier :00]
- Se solicita revisión por otro TASK_ID de M06.01/M06.02/M06.05, preservando A5_PASS de M06.03/M06.04, A5_FAIL transversal M04.03, SHA/blobs, diferencias entre E2E/HTTP/mocks y BITACORA A:Z/26; M06.06 no se libera. Cambios sólo documentales, sin claims de revisión A5 aprobada ni modificación de runtime, DB, workflows, Tasks, QA/main/PROD. Rollback: git revert sin force-push.

## 2026-11-13 — V5 C00-01.M06.02: manifiesto causal de versiones, diffs y artefactos [Javier :50]
- Se agregó un manifiesto reproducible con fuente V5, estado Sheet/BITACORA A:Z/26, SHA, tree, blobs, diff documental +53/+3 de M06.01, jobs reales M10 12/12, seguridad 1109/1109 y Scope Lock SHA exacto, más límites mocks vs HTTP y A5 independiente. Sólo documentación; no modifica código runtime, datos, tareas ni entornos QA/main/PROD.

## 2026-11-13 — V5 C00-01.M06.01: auditoría causal de pruebas vs mocks [Javier :50]
- Se audita evidencia real de GitHub Actions (M10 MySQL/Angular/Playwright 12/12, security xUnit 1109/1109, backend/frontend y Scope Lock) contra fuente exacta 14ea2e23; se distinguen casos HTTP reales, route.fulfill UI sintético, Subjects unitarios, límites RBAC/tenancy y pruebas de rendimiento no productivas. Solo docs: sin cambios de runtime, datos, secretos o despliegues. A5 independiente queda pendiente; QA/main/PROD no autorizado.

## 2026-11-13 — Saneamiento documental C00-03.M01.05 esquema BITACORA A:Z/26 [propietario, sin cambio de Task]
- Alinea dos referencias de aceptación con los 26 campos físicos de BITACORA; incorpora rechazo explícito de eventos 21-25, duplicados y A5 autoaprobado. Preserva la autoria original del TASK_ID y requiere nueva revisión A5 por Task distinta; no implica permiso QA/main/PROD ni pruebas runtime nuevas. Rollback: git revert del commit de documentación, sin force push.

## 2026-11-13 — Corrección A5_FAIL C00-03.M01.04: RI10 BITACORA A:Z/26 [Javier :20]
- Se rectifica en el documento propio RI10: 26 columnas A:Z, EVENT_ID e IDEMPOTENCY_KEY únicos, RUN_ID vacío sin lectura nativa y rechazo explícito de eventos de 21–25 campos; se conserva A5_FAIL del SHA bfb4c325 y se solicita A5 nueva e independiente para el SHA corregido. Sólo documentación; no afecta código ejecutable, fórmulas, Tasks ni QA/main/PROD.

## 2026-11-13 — Corrección A5_FAIL C00-01.M06.03: matriz BITACORA A:Z/26 [Javier :00]
- Corrige referencia documental de 21 a 26 columnas físicas, EVENT_ID/IDEMPOTENCY_KEY, RUN_ID no inventado y negativos contra truncamiento y suplantación; conserva evidencia original de A5_FAIL sobre 66b640fb y exige nuevo A5 independiente. No cambia runtime, Sheets, Tasks, QA/main/PROD ni gates M06.

## 2026-11-13 — Corrección A5_FAIL C00-02.M01.03: BITACORA A:Z/26 [Javier :00]
- Corrige contrato documental original A:U/21 a encabezado físico institucional A:Z/26; define EVENT_ID e IDEMPOTENCY_KEY únicos, RUN_ID vacío sin ejecución comprobable y negativos contra truncamiento/duplicación/suplantación. Preserva A5_FAIL del SHA 81141057 y eventos históricos, solicita nueva A5 independiente tras Scope Lock exact-SHA. Solo documentación; no altera código, Sheets, Tasks, QA/main/PROD.

## 2026-11-13 — V5 C00-03.M01.04: riesgos, supuestos y decisiones de matriz integrada [Javier :20]
- Se incorpora registro documental de diez riesgos RI01–RI10, cinco supuestos S01–S05 y seis decisiones D01–D06, más aceptación P1/P2 y ocho rechazos N1–N8; delimita matriz V5 integrada, exige fuente/ID único, fronteras tenant/PII, CI proporcional, Scope Lock y cero migración de imágenes históricas. No implementa software, migra proveedores, crea IDs ni declara cierre de fase o A5_PASS.

## 2026-11-13 — V5 C00-03.M01.05: aceptación funcional y técnica de matriz integrada [Javier :10]
- Contrato documental con AC-01–AC-10 observables, evidencia mínima por unidad, negativos fail-closed, independencia de autor/A5, Scope Lock y dependencia DAG C00-01.M02; no cierra matriz completa ni implementa runtime.
- TIPO1_DOCUMENTACION: dotnet/Node/Angular/MySQL/Docker/E2E=NO_APLICA; security=SI_REVISION_DOCUMENTAL; scope_lock=SI; A0-A3 documentales tras VALIDANDO y A4 SHA/readback, A5 independiente. No QA/main/PROD.

## 2026-11-13 — V5 C00-01.M06.03: matriz de riesgos, límites y decisiones [Javier :00]
- Registro firmado por Task/SHA con riesgos abiertos M05.04, A5_FAIL original M04.03, strict -warnaserror N1.1/N1.2, CSS budget, TestServer obsoleto y límites de rendimiento CI. Matriz de contradicciones V5/DAG/CI/autoría, sin liberar M06 ni cambiar runtime; Scope Lock y A5 independiente.

## 2026-11-13 — V5 C00-03.M01.03: lectura causal de fuentes y evidencia, sin certificación [Javier :10]
- Inventario documental de doce blobs Git originales (Skill, arquitectura, Scope Lock, contratos, frontend net10, CI y C00-03), fuentes Doc/Sheet V5, seis prerrequisitos M02 LISTO y matriz de evidencia disponible versus por comprobar; ningún color de check histórico implica PASS de nueva implementación.
- TIPO1_DOC, requiere_dotnet/Node/Angular/MySQL/Docker/E2E=NO; requiere_security=SI_REVISION_FRONTERAS; requiere_scope_lock=SI_EXACT_SHA; A0-A3 auditoría documental y A4 diff/readback después de FF dev, A5 de Task distinta. No QA/main/PROD.

## 2026-11-13 — V5 C00-02.M01.05: aceptación funcional y técnica observable del modelo [Javier :00]
- Matriz de diez criterios positivos/negativos para elegibilidad DAG, autoría, estados, A0–A5, CI exact-SHA, BITACORA idempotente y rollback; ejemplo real M05.01. Solo documentación; Scope Lock exact-SHA para cierre, A5 independiente, sin QA/main/PROD.

## 2026-11-13 — V5 C00-02.M01.04: riesgos, supuestos y decisiones [Alex :05]
- Matriz de riesgos R01–R10 y supuestos contrastables S1–S6 del modelo V5: exclusión de custodios, fórmulas COLA, unicidad BITACORA, concurrencia Git, Skill/Scope Lock, CI exact-SHA y separación QA/PROD.
- Sólo docs y CHANGELOG, sin runtime ni permisos nuevos; A0–A3 documentación, A4 exact SHA/readback y A5 independiente pendiente; no QA/main/PROD.

## 2026-11-13 — V5 C00-03.M01.02: entradas, salidas, actores y superficies de matriz integrada [Javier :10]
- Contrato documental con siete entradas autorizadas, seis actores con fronteras de permiso y siete superficies (gobierno, dominio, API/tenancy, EF/MySQL, Angular, integraciones y evidencia), casos positivos/negativos y dependencia C00-01.M02 ya finalizada; no crea nueva microtarea ni certifica matriz completa.
- TIPO1_DOCUMENTACION; requiere_dotnet/Node/Angular/MySQL/Docker/E2E=NO (sin cambio ejecutable), requiere_security=SI_REVISION_DOCUMENTAL y requiere_scope_lock=SI_EXACT_SHA; A0-A3 dirigidas y A4 readback tras FF dev, A5 independiente. No toca QA/main/PROD.

## 2026-11-13 — V5 C00-02.M01.03: inventario causal de contratos y evidencias [Javier :00]
- Fuentes V5 y blobs HEAD, PLAN/COLA/BITACORA y fórmulas E/G/O; contrato de estados e idempotencia, ejemplo real C00-01.M05.01 y negativos de claim/CI; sólo documentación, sin cambios runtime ni autorizaciones QA/PROD. Scope Lock y readback exact-SHA antes de LISTO, A5 independiente.

## 2026-11-13 — V5 C00-02.M01.02: entradas, salidas, actores y superficies [Alex :05]
- Matriz documental de autoridad V5: PLAN_MAESTRO editable, COLA derivada, BITACORA idempotente, GitHub dev, identidades físicas, CI exact-SHA y revisión A5 ajena. Casos positivos/negativos para propiedad, idempotencia, permisos y preservación de fórmulas.
- Sin cambios de runtime, secretos, API, DB, dependencias o Tasks; A0–A3 documentales y A4 Scope Lock exact SHA/readback antes de LISTO. No QA/main/PROD.

## 2026-11-13 — V5 C00-03.M01.01: objetivo y no-regresión de la matriz integrada [Javier :10]
- Delimitación documental de fuente V5 única, perímetro de 2.340 IDs, seis familias de evidencia, dependencias del DAG técnico de preparación transversal y aceptación positiva/negativa fail-closed. No implementa ni certifica la matriz completa, C00-01 final, QA/PROD, migraciones ni autorizaciones.
- Clasificación: TIPO1_DOCUMENTACION; requiere_dotnet=NO; requiere_node_angular=NO; requiere_mysql=NO; requiere_docker=NO; requiere_e2e=NO; requiere_security=SI_REVISION_FRONTERAS_Y_SECRETOS_DOCUMENTAL; requiere_scope_lock=SI_SHA_EXACTO. A0–A3 dirigidas durante VALIDANDO; A4 commit FF, Scope Lock y readback; A5 independiente.

## 2026-11-13 — V5 C00-02.M01.01: objetivo, no-alcance y no-regresión del modelo de trabajo [Alex :05]
- Documento acotado de preparación de la cola V5: fuente de 2.340 IDs, PLAN/COLA/BITACORA, exclusividad por TASK_ID, casos de aceptación positivos/negativos, límites de seguridad, A0–A5 y rollback. Sin cambios en runtime, fórmulas institucionales, permisos, API, DB, configuración o Tasks.
- Validación proporcional: revisión estática/documental de diff y coherencia V5/V13; Scope Lock exact-SHA y readback pospublicación antes de LISTO. No atribuir pruebas de Angular/.NET/MySQL/E2E a una entrega documental y no promover QA/main/PROD.

## 2026-11-13 — V5 C00-01.M05.02: contrato HTTP/DTO y errores seguros [Javier :50]
- Nuevo test xUnit TestServer que atraviesa ExceptionHandlingMiddleware de ASP.NET Core 10 en HTTP in-memory; verifica contrato ApiResponse 200, ProblemDetails 400/409, envelope 500 y sentinelas privadas ausentes del cuerpo de respuesta (1 positivo y 4 negativos). No cambia servicios, endpoints, migraciones, base de datos, UI ni dependencias.
- Clasificacion: .NET=SI; Angular/Node=NO; MySQL=NO; Docker=NO; browser E2E=NO; seguridad de respuestas=SI; Scope Lock exact-SHA=SI. A0 revision estatica sin nuevos secretos ni deps; A1-A3 suite GitHub Actions y A4 posintegracion pendientes al publicar; A5 independiente posterior. Sin QA/main/PROD.

## 2026-11-13 — V5 C00-01.M05.01: regresión causal ante respuesta tardía de tienda [Javier :00]
- AppComponent descarta callbacks tardíos de StorefrontIdentidadService cuando cambia ruta a plataforma, nueva página de tienda o se destruye el shell; no restaura SEO ni CSS de tenant anterior. Tres pruebas unitarias de navegación con NavigationEnd y Subjects RxJS cubren positivo y negativos; Priority4 CI las ejecuta en Angular 22 con Node 24. Enmienda también la brecha señalada por A5_FAIL de M04.03, sin autodeclarar una A5_PASS. Sin cambios de API/MySQL/permisos/secretos; requiere CI exact-SHA y rollback normal.

## 2026-11-13 — V5 C00-01.M04.04: corrección del selector de formulario en loading [Javier :00]
- M10 del SHA cca72cfc falló 1/12 porque la etiqueta de submit cambia al comenzar el login. El E2E mantiene disabled y POST único usando selector estable del botón; exigir nuevo SHA y nueva CI. Sin cambios de auth, base de datos, secretos ni permisos.

## 2026-11-13 — V5 C00-01.M04.01: mantenimiento causal del inventario de componentes [Javier :10]
- El check Scope Lock del commit 8bd38254 encontró catalog drift por la nueva vista de ruta desconocida; se conserva el inventario histórico P0-007.A y se añade la nueva ruta al catálogo vigente (138 componentes) con verificador de drift y negativos sin desactivar el gate. No concede certificación del backlog antiguo.

## 2026-11-13 — V5 C00-01.M04.01: ruta pública de página no encontrada [Javier :10]
- La ruta Angular wildcard presenta una vista pública accesible con navegación a login/tienda, sin divulgar query, IDs de empresa o rutas internas. Añade regresión Playwright Chromium real al workflow N8.2; rutas privadas siguen usando guards existentes. Sin API, MySQL, Docker, permisos, QA, main ni PROD.

## 2026-11-13 — V5 C00-01.M04.04: navegación y envío único de login [Javier :00]
- Añade tres escenarios Playwright reales: guard anónimo/formulario inválido, error seguro de credenciales inválidas y POST único durante latencia de la API con tenant server-side. Se incorpora prueba en runner M10 ya autorizado; sin cambios de autenticación, base productiva, secretos o privilegios. Requiere evidencia exact-SHA A0–A4 y A5 independiente.

## 2026-11-13 — V5 C00-01.M04.03: branding visual seguro por tenant [Javier :00]
- ThemeApplierService restringe tokens dinámicos a colores hexadecimales, limpia temas previos y descarta respuestas HTTP tardías A al cambiar a B/plataforma. Añade 4 casos positivos/negativos Angular y paso de CI dirigido Priority4; mantiene M10 E2E, Scope Lock y A0–A5 por SHA. No toca API, datos, roles, QA, main ni producción.

## 2026-11-13 — Corrección CI C00-01.M04.05: segundo toggle de contraseña [Javier :40]
- Primera CI M10 SHA c45c2d86 falló en test E2E propio (8/9 PASS) porque el segundo Enter se disparaba sin verificar foco en el botón actualizado. Ajuste dirigido Playwright: enfocar y acreditar foco en Ocultar contraseña antes de pulsar Enter; sin cambio funcional de login, API, backend o seguridad. Requiere nueva ejecución exact-SHA M10.

## 2026-11-13 — V5 C00-01.M04.06 verificación UI de densidad/progresividad [Javier :50]
- Se amplía prueba Playwright N8.2 de portada pública para medir geometría hero/CTAs 390/1440 px, ausencia de desborde, carga diferida y telemetría de rendimiento. Sin modificar producción ni prometer SLA; fixture branding solo en CI. Requiere Node/Angular lint/build, N8.2 y Scope Lock exact-SHA; A5 independiente.

## 2026-11-13 — V5 C00-01.M04.05: verificación de accesibilidad en login público [Javier :40]
- Añade Playwright real al gate M10 para /login anónimo a 320px: labels, orden de foco teclado, mostrar/ocultar contraseña, formulario habilitado/deshabilitado, contraste y desbordamiento.
- Solo alcance de regresión frontend, sin cambios de API, tenancy, esquemas MySQL, claves, infraestructura o permisos. Exigir CI E2E M10 y frontend/SCOPE_LOCK causal SHA; A5 independiente. No autoriza QA/main/PROD.

## 2026-11-13 — V5 C00-01.M04.02: estados accesibles de dashboard y recuperación [Javier :50]
- Se corrige el dashboard empresarial que quedaba sin contenido cuando GET /dashboard/resumen fallaba o retornaba data=null. Ahora expone estados de carga, error, vacío y éxito, reintento explícito con 44px y foco visible, sin reflejar detalles internos de HTTP. Conserva authGuard/permisoGuard, RBAC y tenant existentes; no cambia API, contrato SQL, migraciones ni secretos.
- Regresión Playwright M10 con API/MySQL efímeros: denegación 503 y ausencia de filtración de mensaje, respuesta vacía y reintento exitoso real. Requiere A0 de diff, Angular lint/build, E2E M10, Scope Lock y readback SHA exacto. No promover QA/main/PROD sin A5 independiente.

## 2026-11-13 — V5 C00-01.M03.04 correccion CI CS0117 en test legacy [Javier :30]
- El commit b64b881f no compilaba por usar EmpresaId en OrdenCompra legacy sin esa propiedad (job 131891745374, error CS0117). Se corrige exclusivamente fixture xUnit para monoempresa; se preserva contrato de seguridad sin fingir aislamiento A/B en esta prueba.
- Reejecutar GitHub Actions en SHA correctivo para .NET, seguridad y MySQL; mantener VALIDANDO y A5 pendiente.

## 2026-11-13 — V5 C00-01.M03.04 persistencia, paginacion extrema fail-closed [Javier :30]
- OrdenCompraRepository.GetPagedAsync calcula offset en long, pagina/tamano acotados, retorna vacio fuera de rango sin desbordamiento int ni filtrado tenant ampliado.
- xUnit positivos y negativos de paginacion monoempresa legacy; A/B solo suites de seguridad CI separadas; GitHub Actions .NET 10/MySQL 8.4 sobre SHA nuevo exigido, sin modificar API, schema, JWT/RBAC, UI o produccion. A5 independiente.

## 2026-11-13 — V5 C00-01.M03.03: ampliar denegaciones de cabeceras duplicadas [Javier :20]
- La corrección de P2001F en SHA `bed9975bd31d1f0a35b478614385c623807bb92b` cambió solo test P2 y el clasificador `ci-impact-routing` marcó el job MySQL como SKIPPED. Para evaluar el candidato completo bajo las pruebas reales pertinentes, la regresión nueva de seguridad amplía los casos a cabecera multi-valor repetida (17/17, 18/18) y orden invertido (18/17): deben fallar cerrados. Es una prueba causal adicional, no una bandera artificial.
- GitHub Actions dotnet/tenancy/MySQL 8.4 deben ejecutarse sobre un nuevo SHA; no atribuir PASS al job SKIPPED ni cerrar DEV antes de A0–A4. No se modifica el código de producción en esta corrección. Rollback: revert normal, sin PR, rama o despliegue.

## 2026-11-13 — V5 C00-01.M03.03: alinear regresión P2001F tras CI fallida [Javier :20]
- GitHub Actions CORE run 43425902857, job 131887123198, compiló el código, pero detectó cinco fallos entre 3402 xUnit: P2001F esperaba fallback desde `X-Empresa-Id=-202`, incompatible con el nuevo contrato fail-closed. Las cinco variantes se corrigen para exigir `ForbiddenAccessException`, cero invocación del controlador, cero autoridad tenant y ningún acceso de permisos/scope. No se relaja la protección en producción ni se alteran migraciones.
- La corrección necesita nuevo commit fast-forward en dev y revalidación CI exact-SHA de .NET 10, seguridad, MySQL 8.4 y A4; M03.03 sigue VALIDANDO. Rollback mediante git revert seguro.

## 2026-11-13 — V5 C00-01.M03.03: selector de tenant inválido falla cerrado [Javier :20]
- `TenantPermissionContext` rechaza cabecera explícita `X-Empresa-Id` vacía, inválida o con múltiples valores antes del fallback de empresa única. Se preservan pertenencia backend verificada, autorización RBAC y selección positiva compatible.
- xUnit dirigido cubre tenant A positivo, fallback sin cabecera, casos inválidos, duplicados y tenant B sin membresía. Validar en GitHub Actions .NET 10 y MySQL 8.4 según impacto del SHA publicado; no atribuir PASS hasta leer logs. Alcance y rollback documentados en `docs/v5/c00-01-m03-03-tenant-auth.md`. DEV únicamente; A5 independiente.

## 2026-11-13 - V5 C00-01.M03.05 batch outbox duplicado fail-closed [Javier :50]
- Prevalida EventoId y ClaveIdempotencia del lote completo para impedir despachos repetidos de un repositorio defectuoso, sin cambio en API/SQL/proveedor.
- Añade pruebas xUnit positivas/negativas; exige CI .NET10/MySQL exact-SHA y A5 independiente.

## 2026-11-13 — V5 C00-01.M03.06: corrección dirigida tras CI xUnit [Javier :10]
- El primer SHA `a1087d334d03941de0b0eba0108d2d411604a61d` compiló en GitHub-hosted .NET 10 pero dev-ci detectó 1 fallo entre 3388 xUnit: `N49FPeriodoContableObservabilityTests` aún esperaba revelar un `ArgumentException.Message` arbitrario. La prueba se reconcilia con el contrato de error público seguro (400 ProblemDetails genérico), verificando además que el texto interno no se expone.
- Mantiene código de producción del primer commit sin modificaciones adicionales. Los controles A1–A4 se reevalúan en el SHA correctivo exacto mediante GitHub Actions; no atribuir PASS hasta la evidencia causal. `VALIDANDO` conserva el mismo TASK_ID. Rollback: git revert ordenado sin reescritura.

## 2026-11-13 — V5 C00-01.M03.06: errores de argumentos seguros y correlación estructurada [Javier :10]
- En `ExceptionHandlingMiddleware`, las excepciones `ArgumentException` devuelven ProblemDetails 400 con detalle genérico y `traceId`; el mensaje arbitrario potencialmente confidencial no se expone al cliente ni se escribe al log. Se registra únicamente el tipo de excepción y referencia estructurada.
- Pruebas xUnit dirigidas: dos negativas con datos privados y una positiva de propagación sin error. No se alteran JWT, RBAC, filtros de tenant, modelos ni migraciones.
- Rutas de validación: GitHub Actions `solqaryn-core-ci.yml` (.NET 10, pruebas backend y seguridad), `dev-ci.yml` (filtrado backend y MySQL según impacto). A0–A4 y SHA exacto quedan pendientes de resultados causales; no se afirma PASS anticipado. Sin cambios en qa/main/PROD; rollback aditivo mediante git revert no destructivo.

## 2026-11-13 - V5 C00-01.M03.02 aplicacion: replay SHA-256 fail-closed [Javier :00]
- Alinea OrdenCompraService.ResolverReintento con el dominio M03.01: compara bytes de SHA-256 en tiempo constante; rechaza fingerprints heredados invalidos y acepta hex equivalente mayuscula sin duplicar ordenes.
- Dos xUnit dirigidos y contrato V5 de limites del servicio. CI .NET/MySQL sobre SHA publicado; A0-A4 y A5 se certifican por evidencia real, nunca por tests simulados. No QA/main/PROD.

## 2026-11-13 — Scope Lock: mutación exacta de la clave del plan
- Corregida exclusivamente la auto-prueba negativa `wrong plan` en `validate-automation-authority.mjs`: ahora modifica `EXECUTION_PLAN_DRIVE_FILE_ID=` en lugar de la primera aparición del ID dentro de la introducción V5; la prueba vuelve a comprobar un plan realmente incorrecto. Se conserva el fail-closed del contrato y la Skill canónica sin modificación.

## 2026-11-13 — Conciliación V5 y selección de IDs; no modifica el progreso de M03
- El commit base `f4b8528f57e82625d343dd1590076df44cacaee2` es antecesor confirmado de `4d582aa48dcdf8e9efb8d4c15ec229cbc0d603a5` (13 commits posteriores de C00-01.M01/M02) y del commit `4fc1f138a3323268d3efb6962b1182ced567c435` que publicó código para C00-01.M03.01; el HEAD vivo avanza y nunca se inmoviliza en el SHA base.
- Seis Tasks Javier verificadas habilitadas con prompts V5 + ruta GitHub Actions; se repararon las claves operativas que aún decían 0/6 y la suspensión antigua pasó a histórico, sin cambiar RRULE/IDs ni el ámbito exclusivo Alex.
- Selector `scripts/validate-dual-task-configuration.mjs` ahora reconoce IDs Cdd-dd.Mdd.dd del V5, rechaza la cola V13 antigua y conserva pruebas de identidad/seguridad. CI de jobs y logs sólo acredita el SHA exacto y los tests realmente ejecutados; un job SKIPPED nunca equivale a PASS.
- Snapshot a la hora de la intervención: C00-01.M03.01 registrada VALIDANDO, 12 LISTO, 5 candidatas PENDIENTE habilitadas; este commit documental no la convierte en LISTO ni cambia PLAN_MAESTRO, COLA, BITACORA, backend o datos. Rollback por git revert con HEAD revalidado.

## 2026-11-13 - V5 C00-01.M03.01 dominio: fingerprint SHA-256 fail-closed [Javier :50]
- Corrige `OrdenCompra.EstablecerIdempotencia`: exige hex ASCII de 64 caracteres y canoniza replay de huella equivalente; rechaza reuso con payload distinto y preserva estado ante error.
- Añade xUnit positivo/negativo de dominio, contrato documentado y límites de evidencia. A0-A3 deben validarse en candidato; A3 .NET/MySQL en GitHub Actions del SHA nuevo, A4 posdev y A5 por revisor ajeno. Sin QA/main/PROD.

## 2026-11-13 - V5 C00-01.M02.05 eventos e idempotencia [Javier :00]
- Definicion documental trazable de correlation HTTP/Kardex, OrdenCompra, MensajeOutbox y MensajeOutboxService, con limites de IdempotencyKey, correlacion, retries y denegacion multiempresa; no cambios runtime, SQL ni providers.
- Estado EN_PROGRESO y VALIDANDO registrado antes de auditorias formales A0-A3; Node 22.16.0 --check, inspeccion dirigida de secretos y 9/9 pruebas documentales (1 positiva, 8 negativas) PASS. Sin afirmar HTTP/MySQL/live ejecutados.
- Commit directo fast-forward dev con A4 CI 5/5 exact SHA y readback pendientes; registrar A5_PENDIENTE para otro TASK_ID, rollback git revert no destructivo, no QA/main/PROD.

## 2026-11-13 — V5 C00-01.M02.04 configuracion tipada y feature flags [Javier :40]
- Reconstruido candidato documental en estado VALIDANDO con mismo TASK_ID; ocho contratos CFG01-CFG08 de configuracion, grants, pilotos UI, aislamiento DB y rollback, fundamentados en cinco blobs reales de dev. No se activaron flags ni modifico runtime, secretos, datos, Angular, QA o PROD.
- A0 Node 22.16.0 --check y patrones de secretos sin coincidencias; A1 matriz documental positiva; A2 negativos dirigidos de fail-closed y tenant A/B; A3 node --test 9/9 PASS (documentacion y fixtures sinteticos, NO HTTP/MySQL/browser/tenancy live).
- A4 requiere diff, SHA publicado, CI exacto y readback PLAN/COLA/BITACORA antes de LISTO_DEV. A5_PENDIENTE para otro TASK_ID, sin autoaprobar QA/main/PROD. Rollback: git revert normal sin force-push.

## 2026-11-13 - V5 C00-01.M02.06 dependencias tecnicas y salida M02 [Javier :00]
- Contrato documental V5 FINAL de las seis unidades M02; puerta G/O del ID separada de cierre del lote, matriz de dependencias y evidencias, negacion de acceso cross-tenant, A5 independiente, CI exact SHA y rollback no destructivo.
- Claim y VALIDANDO preservan TASK_ID 6adbb9fff1548191a1bd5b30d9e9984a en PLAN; candidato preparado y auditado despues de VALIDANDO con Node v22.16.0 --check, secreto/whitespace scan sin hallazgos, Node --test 8/8 (1 positivo, 7 negativos), sin dependencia adicional ni cambio a backend/frontend/MySQL.
- Commit directo fast-forward dev condicionado al HEAD; A4 requiere verificacion posdev SHA, pruebas repetidas y CI 5/5 exact SHA antes de LISTO; A5_PENDIENTE de otro Scope. No se certifican HTTP/MySQL/browser ni se promociona QA/main/PROD. Rollback git revert aditivo.

## 2026-11-13 — V5 C00-01.M02.03 persistencia, indices, migraciones y datos de prueba [Javier :20]
- Se documenta el contrato de nueve invariantes DB01-DB09 de MySQL/EF Core/Oracle, indexes compuestos de Cliente/Producto por EmpresaId, migracion reversible y fixtures aislados A/B. Rutas y blobs Git comprobados en dev, evidencia estatica no es ejecucion DB.
- Cambio acotado a docs/v5/c00-01-m02-03-persistence-contract.md, scripts/tests/c00-01-m02-03-persistence-contract.test.mjs y CHANGELOG. A0 sintaxis Node 22.16.0 y escaneo de patrones de secretos; A1 positivo documental; A2 ocho negativos fail-closed; A3 Node 9/9 PASS. Sin dependencias nuevas ni cambios en runtime, datos, JWT/RBAC, QA/main/PROD.
- A4 requiere commit directo fast-forward, diff, CI exact SHA y readback institucional. A5_PENDIENTE exige otro TASK_ID antes de QA/main/PROD, rollback aditivo git revert sin reescribir historia. No inventar ejecucion MySQL, HTTP, Playwright ni migraciones.

## 2026-11-13 — V5 C00-01.M02.02 contratos API/DTO/errores y validadores [Javier :40]
- Inventario documental V5 de ocho contratos reales en API/Application con hashes de blobs Git: Empresas API/DTO, tenant-context, Productos API/DTO, FluentValidation producto y login, middleware global de errores 400/403/404/409. Se protegen límites JWT, RBAC, compatibilidad legacy, variantes, tenant A/B y no exposición de secretos.
- Transición de PLAN PENDIENTE→EN_PROGRESO→VALIDANDO registrada antes de A0–A3, con mismo TASK_ID, BITACORA y COLA por fórmula. Node22 --check, scan dirigido de secretos y 8/8 pruebas documentales (una positiva, siete negativas) PASS sobre candidato; sin cambios runtime/DB/Angular, ni afirmar HTTP/MySQL/browser/tenancy live.
- Commit directo fast-forward dev, A4 CI exact SHA y readback posteriores necesarios antes de LISTO; A5_PENDIENTE de otro TASK_ID previo a QA/main/PROD; rollback git revert aditivo sin force-push.

## 2026-11-13 — V5 C00-01.M01.05 aceptación funcional y técnica observable [Javier :20]
- Definida matriz AC01-AC12 con señales positivas, negativas y evidencia futura de gobierno, V5, autoría, seguridad multitenant, DTO, MySQL, UX, A0-A5, rollback, proveedores e integración. Estado explícito DEFINIDO_NO_CERTIFICADO; el documento no constituye evidencia live.
- Cambios delimitados a docs/v5/c00-01-m01-05-acceptance.md, scripts/tests/c00-01-m01-05-acceptance.test.mjs y esta nota. Node 22.16.0 node --check y node --test 9/9 (1 positivo, 8 negativos) PASS sobre blobs exactos 308033dd y 74f412ab; revisión dirigida de secretos sin hallazgos y sin dependencias nuevas.
- A0-A3 dirigidas durante VALIDANDO; A4 requiere SHA publicado, diff, CI causal y readback PLAN/COLA/BITACORA. A5_PENDIENTE para otro TASK_ID. Sin cambiar Angular/.NET/MySQL/JWT/tenancy, sin QA/main/PROD. Rollback aditivo git revert del commit tras validar HEAD.

## 2026-11-13 — V5 C00-01.M02.01 ownership, entidades e invariantes [Javier :40]
- Contrato documental verificable sobre nueve fuentes reales de Domain (Empresa, Usuario, ContextoTenantActual, Producto, Variante, MovimientoInventario, Factura, BaseEntity y AuditableEntity), con identificadores de blob rastreables del HEAD de entrada, límites entre tenant A/B y nulidad histórica, normalización, lote/vencimiento, movimiento, auditoría e independencia A5.
- Microtarea V5 FINAL C00-01.M02.01 sin cambios de runtime Angular/.NET/MySQL, JWT/RBAC ni migraciones. Tras EN_PROGRESO se registró VALIDANDO ANTES de A0–A3; Node 22.16.0 --check, revisión de secretos y ocho pruebas dirigidas de contrato (positivo y siete negativos) PASS. Solo se afirma alcance documental; no se certifican .NET HTTP, tenancy live, MySQL, Angular browser ni proveedores.
- Publicación directa fast-forward en dev y A4 sujetas a commit/CI exact-SHA/readback institucional, luego registrar A5_PENDIENTE para TASK_ID ajeno, nunca autorización de QA/main/PROD; rollback git revert aditivo, sin force-push.

## 2026-11-13 — V5 C00-01.M01.06 bloqueo, rollback y liberación de M01 [Javier :20]
- Contrato documental de nueve categorías de NO_GO con evidencias y acción de salida (Skill/Scope Lock, identidad, concurrencia, calidad A0–A4, independencia A5, JWT/RBAC/tenancy, proveedores y medios históricos cero). No se inventa un nuevo lock de M02 ni se suplanta la fórmula de COLA. La salida M01.06 no acredita los seis cierres de M01.
- Fuente: V5 FINAL / C00-01.M01.06. Cambios exclusivamente docs/v5/c00-01-m01-06-release-gate.md y scripts/tests/c00-01-m01-06-release-gate.test.mjs, CHANGELOG aditivo; Node 22.16.0 dirigido 7/7 (positivo y seis negativos), --check, revisión de secretos sin hallazgos, sin dependencias nuevas. Precheck falló inicialmente por omitir palabra GitHub y se corrigió antes de VALIDANDO; auditorías A0–A3 ejecutadas de nuevo después de VALIDANDO 7/7 PASS.
- A4 requiere commit directo FF, comprobación del SHA exacto, CI/GitHub y readback PLAN/COLA/BITACORA. A5_PENDIENTE otro TASK_ID antes de QA/main/PROD. No se afirma HTTP/MySQL/Angular-browser, seguridad de tenancy viva ni migración de datos. Rollback: revert aditivo sin force-push.

## 2026-11-13 — correccion causal de Scope Lock V5 C00-01.M01.03 [Javier :30]
- GitHub Actions exact SHA bac2583c falla Scope Lock y Seguridad porque el nombre de dos archivos incluia una identidad heredada bloqueada por scripts/verify-project-scope.mjs. Se corrigieron solamente las rutas y el import del test; sin alterar politica, scripts de seguridad, runtime ni contratacion.
- Rutas nuevas docs/v5/c00-01-m01-03-source-audit.md y scripts/tests/c00-01-m01-03-source-audit.test.mjs; rutas viejas retiradas del arbol por commit aditivo, sin reset ni force. Node --check PASS; Node --test 9/9 PASS; el patron prohibido ya no aparece en paths ni fuente del test. A4 y LISTO sujetos a nueva CI exact SHA y readback.

## 2026-11-13 — V5 C00-01.M01.03: inventario de fuentes, evidencia y contradicciones [Javier :30]
- Documenta 10 IDs de blob Git verificados, Google Doc V5 FINAL y Sheet 2.340 IDs, limites de hash/autoridad, estado V5, topologia objetivo frente a realidad, pausa/reanudacion y formulas PLAN/COLA. La autoria Git no es firma criptografica del propietario.
- Cambio documental con prueba Node 22.16.0: 9/9 PASS (1 positivo, 8 negativos fail-closed), node --check PASS y escaneo dirigido de secretos PASS. A0 estatico, A1 funcional documental, A2 limites de tenancy/JWT/RBAC/PII sin runtime modificado, A3 integracion de contrato Node PASS. No afirmar HTTP/MySQL/Playwright ni proveedores reales.
- A4 sujeto a lectura Git dev FF exact SHA, diff final y readback PLAN/COLA/BITACORA; A5_PENDIENTE para TASK_ID independiente antes de qa/main/PROD. Rollback git revert protegido, no reset/force.

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

## 2026-11-13 — V5 C00-02.M01.02 / M01.04: corrección documental del esquema BITACORA (Alex :05)
- Rectificación causal de A5_FAIL en dos documentos de autoría propia: el encabezado vivo de BITACORA contiene 26 columnas A:Z, no el esquema antiguo de 21 A:U. No se modifican PLAN, COLA, fórmulas ni eventos históricos.
- Corrección limitada a los dos docs de C00-02.M01.02 y C00-02.M01.04 más esta entrada aditiva; sin runtime/API/migraciones/secretos. A0–A3: revisión documental causal, 26 encabezados y negativos de duplicación/identidad. A4 pendiente de CI exact SHA tras publicar; A5 independiente pendiente. Rollback: git revert normal sin force. QA/main/PROD no autorizados.

## 2026-11-13 — Rectificación documental A5_FAIL C00-03.M01.01/.02/.03 [Javier :10]
- Corrige esquema físico de BITACORA de A:U/21 a A:Z/26, EVENT_ID A e IDEMPOTENCY_KEY R, sin reinterpretar ni reescribir eventos históricos.
- Añade verificación positiva 26/26 y negativas de truncamiento, duplicación y autoaprobación; preserva A5_FAIL anterior, exige Scope Lock exact-SHA y A5 independiente sobre este nuevo commit. Sin cambios ejecutables, QA/main/PROD, nuevos IDs, ramas o PR.

## SOLQARYN V5 — C03-01.M01.04, registro documental de riesgos de autenticación (2026-11-13)

- PRIMARY DEV 05 JAVIER (`6adc2e55f47c81919f6374e561687076`) prepara `docs/v5/c03-01-m01-04-riesgos-supuestos-decisiones-autenticacion.md` para registrar R01–R14, S01–S08 y D01–D08 respecto de login, renovación, revocación, recuperación y factores futuros.
- Alcance TIPO1 exclusivamente documental. Sin cambios de código ejecutable, DB, JWT, permisos, proveedores o entornos. A0–A3 verificaciones documentales; A4 requiere Scope Lock exact SHA y readback de PLAN/COLA/BITACORA, A5 revisión independiente pendiente. Reversión con git revert no destructivo.

## 2026-11-13 — SOLQARYN V5 C03-03.M01.01 (PRIMARY DEV 04 Javier :30)
- Preparación documental de objetivo, no-alcance y catorce contratos falsables de no-regresión para auditoría de autorización/denegación, cambios críticos, correlación y protección de secretos.
- Inventario estático de cinco fuentes backend con sus blobs; distingue expresamente evidencia de código de pruebas HTTP/MySQL/retención futuras. No implementa auditoría, permisos ni política de retención y no libera M01.06/M02.
- A0–A3 documentales dirigidas en VALIDANDO; A4 exclusivamente después de commit FF dev, Scope Lock exact SHA y readback PLAN/COLA/BITACORA A:Z; A5_PENDIENTE de otra Task Javier; no qa/main/PROD, secretos ni pagos.
- Rollback mediante revert normal del commit publicado sin reescribir trabajo concurrente.

## 2026-11-13 — SOLQARYN V5 C03-04.M01.01 (PRIMARY DEV 04 Javier :30)
- Objetivo de continuidad, respuesta a incidentes, revocación y backup/restore en preparación M01.01; criterios CO01–CO16 positivos/negativos y decisiones pendientes, sin ejecución de proveedor.
- Fuentes estáticas verificadas: workflows backup-restore de DEV con triggers restringidos, runbook y test de servicio con mocks. Distinguir existencia de YAML de drill efectivo; no fijar RPO/RTO ni confundir environments DEV/Desarrollo.
- Tipo 1 documental: sin .NET/Angular/MySQL/Docker/E2E ejecutables; controles de seguridad y Scope Lock sí aplican. A0–A3 documentales en VALIDANDO, A4 solo después de SHA publicado, A5 por otra Task Javier.
- No toca secretos, QA/main/PROD, infraestructura, Tasks ni servicios pagos. Rollback: git revert no destructivo.

## 2026-11-13 — SOLQARYN V5 C03-04.M01.04 (PRIMARY DEV 04 Javier :30)
- Registro documental de 16 riesgos, 8 supuestos y 9 decisiones pendientes del contrato de incidentes, revocación, backup/restore y continuidad para C10/C12.
- Coteja cuatro fuentes runtime/workflow con blobs, distingue la existencia de jobs de restauración demostrada, y registra discrepancia DEV/Desarrollo como gate que requiere lectura real.
- Documentación solamente (A0–A3 proporcional); .NET/Node/Angular/MySQL/Docker/E2E no aplican, Scope Lock exact SHA sí. A4 solo poscommit, A5 independiente Javier.
- Sin secretos, restore, migraciones, cambios de proveedores, QA/main/PROD, nuevas ramas/PR ni Tasks. Rollback git revert normal, con revisión de trabajo concurrente.

## 2026-11-13 — SOLQARYN V5 C03-04.M01.03 (PRIMARY DEV 04 Javier :30)
- Inventario documental de contratos backup/restore/revocación con 10 fuentes reales y hashes de blobs, 16 oráculos positivos/negativos IV01–IV16, brechas BR01–BR09; sin afirmar ejecución de los workflows.
- Separación entre YAML/triggers/fixtures y restauración MySQL 8.4, y discrepancia documentada DEV/Desarrollo que requiere autenticación de settings; JWT/RBAC/tenant A/B y PII fail-closed.
- TIPO1 doc: A0–A3 revisión documental; sin cambios de runtime, scripts ni workflows, .NET/Angular/MySQL/Docker/E2E NO_APLICA, Scope Lock exact SHA requerido para A4 y A5 pendiente de otra Task Javier.
- No QA/main/PROD, pagos, secretos o backups. Rollback git revert normal con HEAD revalidado.

## 2026-11-13 — SOLQARYN V5 C03-04.M01.05 (PRIMARY DEV 06 JAVIER :50)
- Contrato documental de 18 criterios falsables (8 positivos, 10 negativos) para incidentes, revocacion JWT, backup cifrado, restore MySQL 8.4 descartable, aislamiento A/B, rollback y continuidad; NO se ejecutaron los escenarios futuros.
- Fuentes estaticas con blobs: dos workflows de backup/restore, runbook DEV/Desarrollo, prueba Moq de renovacion, matriz C03-04.M01.04; 15 prerequisitos documentales de C03-01/02/03 registrados LISTO, DAG VALIDADO.
- TIPO1: no cambios runtime, contratos API/DTO, migraciones, secretos, permisos o infrastructure; dotnet/Angular/MySQL/Docker/Playwright NO_APLICA para el changeset; A0-A3 documentales y control de JWT/RBAC/tenant en requisitos; Scope Lock exact SHA requerido en A4.
- A5_PENDIENTE para otra Task JAVIER, M01.06/M02/C10/C12 siguen sujetos a gates propios, sin QA/main/PROD; rollback git revert normal, sin force-push.

## 2026-11-13 — SOLQARYN V5 C00-02.M03.03, aislamiento de autorizacion por scope/tenant [Javier :20]
- Codigo .NET `backend/src/Application/AutomationV5/V5WorkItemTenantAuthorizationGuard.cs`: politica pura fail-closed con allowlist Javier, bindings confiables de scope y tenant, catalogo V5 y owner/A5 independiente.
- Pruebas xUnit `V5WorkItemTenantAuthorizationGuardTests.cs` con positivos y negativos: cross-tenant, actor externo, bindings ausentes, ID no catalogado, conflicto de owner y autorrevisión.
- No sustituye JWT/RBAC/tenancy HTTP real ni implementa CAS de Google Sheets; la autorizacion aplicativa real y la persistencia requieren consumidores y controles propios. Ningun acceso se concede implicitamente.
- Clasificacion: .NET=SI, security=SI, scope_lock=SI; Angular/MySQL/Docker/E2E=NO_APLICA al diff; A1-A3 requieren CI real del SHA exacto; A4 readback y A5 independiente. Sin QA/main/PROD, secretos, ramas ni PR. Rollback revert normal.

## 2026-11-13 — SOLQARYN V5 C00-03.M03.03, proyeccion de cobertura con aislamiento tenant [Javier :20]
- `V5CoverageTenantProjectionService.cs` agrega lectura ejecutable de cobertura con allowlist de actor, binding server-side de scope/tenant, requisito e item al scope correcto, denegacion por defecto y evidencia pendiente por SHA.
- `V5CoverageTenantProjectionServiceTests.cs` cubre positivos y negativos (actor, scope, tenant, item, requisito, bindings ausentes, SHA y duplicados).
- La proyeccion es solo lectura, no sustituye JWT/RBAC del backend, CAS ni prueba real de integracion; el caller debe aportar snapshots confiables. A0 estatico, A1-A3 dotnet/security CI SHA exacto y A4 readback/Scope Lock; A5 pendiente por otra Task Javier.
- .NET=SI; security=SI; Scope Lock=SI; Angular/MySQL/Docker/E2E=NO_APLICA al cambio aislado. No QA/main/PROD, secretos, PR ni ramas. Rollback normal.

## 2026-11-13 — SOLQARYN V5 C00-02.M04.02 estados de experiencia protegida [Javier :10]
- Angular: la vista de solo lectura /plataforma/flujo-v5 distingue validación de sesión en curso, falta de permiso global, fallo seguro, sesión autorizada y ausencia explícita de datos institucionales conectados.
- Pruebas Vitest positivas/negativas para los cinco estados, rechazo de detalles internos en errores y ausencia de peticiones de directorio desde la vista contractual.
- Sin nuevos endpoints, grants JWT/RBAC, operaciones sobre PLAN/COLA/BITACORA, tablas MySQL, proveedores, eventos, dispatcher, PR o ramas. La decisión de acceso real permanece en /plataforma/sesion.
- A0–A3 se verifican por diff y CI Angular pertinente en SHA publicado; A4 requiere Scope Lock, pruebas causales y readback; A5 queda pendiente de otra Task Javier. Rollback git revert sin force-push.
## 2026-11-13 — SOLQARYN V5 C00-02.M04.02 CI dirigida Angular [Javier :10]
- Se agrega gate GitHub Actions efímero de Vitest para los estados UI de plataforma cuando cambian sus dos fuentes o el propio workflow; no ejecuta despliegues, API, migraciones ni datos.
- El workflow usa Node 24 y npm 11 fijados por el repositorio, checkout SHA dev, npm ci y pruebas del fichero Angular realmente modificado. La compilación/lint es cubierta por los jobs frontend existentes en el mismo SHA.
- Este gate cubre la ausencia comprobada de un job de pruebas unitarias frontend para el commit inicial de la microtarea; no se contabilizan CHECKS pendientes o SKIPPED como PASS.
- A0 requiere revisar permisos de workflow contents:read y procedencia de actions; A1/A2/A3 y A4 solo con resultados reales del nuevo SHA, A5 independiente. Sin nuevas Tasks, PR, ramas ni infraestructura, rollback git revert normal.
## 2026-11-13 — SOLQARYN V5 C00-02.M04.03 branding seguro de plataforma [Javier :10]
- El shell Angular restablece inmediatamente los tokens e identidad SOLQARYN al entrar a /plataforma y subrutas V5, incluso si la cuenta conserva un contexto empresarial anteriormente verificado.
- Las rutas empresariales fuera de /plataforma conservan la configuración dinámica existente del tenant; no se acepta branding, CSS, permisos o contexto enviados por URL.
- Se añaden tres pruebas unitarias de frontera de ruta, retorno a contexto global y prefijo negativo, y se amplía el gate Vitest V5 exact-SHA. El archivo app.component.ts no agrega API, nuevo componente ni endpoint.
- A0 inspección estática del parser de ruta, dependencias, changelog y seguridad; A1-A3 Vitest + Angular lint/build de CI real; A4 check-runs y Sheet, A5 solo otra Task Javier. No MySQL, Docker, workers, QA/main/PROD; rollback git revert normal.
## 2026-11-13 — SOLQARYN V5 C00-02.M04.04 verificación de interacción protegida [Javier :10]
- Se añaden pruebas Vitest reales sobre la ruta de contrato V5: navegación de salida explícita con guard plataforma, ausencia de formularios/operaciones empresariales, serialización single-flight de revalidación y error 403 genérico con retry consciente.
- No modifica el runtime ni la lógica de autenticación existente; verifica que el botón se desactiva durante una petición y no ejecuta una segunda llamada mientras siga pendiente.
- El workflow V5 ya existente ejecuta el spec modificado en runner Node 24/npm 11 y reproduce los casos positivos/negativos en SHA exacto. Formularios de edición NO_APLICA porque la vista es deliberadamente solo lectura.
- A0 revisión estática, A1 y A2 tests UI/guard y fuga de errores, A3 Angular build/lint + Vitest y A4 Scope Lock/readback/rollback; A5 pendiente de otro Javier. No MySQL, Docker, infra, QA/main/PROD ni PR/ramas.

## 2026-11-13 — SOLQARYN V5 C00-02.M04.05 accesibilidad y responsive de la consulta V5 [Javier :30]
- Se amplía el Playwright Chromium de rutas V5 con pruebas móviles y escritorio (390/1280), cuatro pasos sin overflow, navegación por Tab, tabla semántica desplazable por teclado y contraste de texto mínimo 4.5:1 según estilos computados.
- Las rutas usan respuestas de sesión sintéticas únicamente para validar experiencia y guardas negativas; no se afirma JWT/MySQL real, tráfico institucional, permisos adicionales ni mutaciones operativas.
- A0 sobre diff, tipos, secretos y procedencia; A1/A2/A3 mediante browser real positivo y negativo, lint/build Angular; A4 solo check-runs exact-SHA y readback del Sheet; A5 independiente de otra Task Javier. Sin API, DB, Docker, workers, despliegues, PR o ramas; rollback git revert.

## 2026-11-13 — SOLQARYN V5 C00-02.M04.06 densidad y carga progresiva controlada [Javier :30]
- Playwright Chromium comprueba cuatro pasos en una única tarjeta de contrato sobre móvil y ausencia de carga de directorio, formularios operativos o consulta institucional oculta; no agrega endpoints ni negocio.
- Una prueba asíncrona retiene una respuesta GET sintética de sesión para verificar anuncio de espera y acción deshabilitada hasta la confirmación; se contabiliza un único request y se conserva denegación fail-closed en pruebas existentes.
- A0 static+secrets+CHANGELOG; A1/A2/A3 Playwright UI positivo/negativo y build/lint Angular, A4 solo checks reales del SHA y readback; A5 independent Javier Task distinta. Sin API/DB/MySQL/Docker/eventos ni permisos añadidos; revert sin reescritura.

## 2026-11-13 — SOLQARYN V5 C00-03.M04.04 navegación y acción única de cobertura [Javier :30]
- Se amplía Playwright real de la matriz de cobertura V5 con recorrido protegido cobertura → administración → cobertura, cinco columnas semánticas y ausencia de formularios/operaciones empresariales o institucionales.
- Segunda prueba controla un GET de sesión sintético retenido: una sola solicitud, botón desactivado, denegación 403 con mensaje seguro y reintento explícito posterior; no se presume JWT/RBAC/MySQL verificados en vivo.
- A0 static/procedencia, A1-A3 Chromium positivo/negativo y Angular lint/build, A4 solo exact SHA + readback, A5 independiente Javier; sin nuevo endpoint, DB, workers, despliegue, rama o PR. Rollback git revert.

## 2026-11-13 — V5 C00-03.M04.05: accesibilidad de la cobertura privada en Chromium real [Javier :40]
- Agrega cuatro verificaciones Playwright sobre /plataforma/cobertura-v5: rechazo sin JWT, denegacion 403 de sesion simulada, mobile 390px/escritorio 1280px, ausencia de overflow exterior, tabla semantica desplazable por teclado, foco y labels, contraste de texto 4.5:1.
- Amplia el workflow navegador V5 para ejecutar el archivo de pruebas separado sin modificar guards, servicios, rutas, MySQL ni componentes de terceros. Backend de sesion sintetico solo para experiencia UI; no certifica JWT/RBAC/tenant de servidor ni datos live.
- A0 revision estatica, secretos y CHANGELOG; A1-A3 browser positivo/negativo, Angular lint y build; A4 exige CI causal del SHA, Scope Lock y readback institucional; A5_PENDIENTE para otra Task Javier. Rollback mediante revert normal, sin reescribir commits.

## 2026-11-13 — V5 C00-03.M04.06: densidad y carga progresiva de cobertura [Javier :40]
- Dos tests Playwright Chromium exclusivos de la ruta /plataforma/cobertura-v5: matriz vacia semantica con cinco columnas y un solo contenedor, sin consultas institucionales ocultas ni widgets de gestion.
- Verifica revalidacion de sesion con GET sintetico pendiente: progreso visible, boton deshabilitado hasta resolucion y exactamente una solicitud. No modifica API, autorizacion JWT/RBAC, MySQL, tenants ni empresas; no inventa filas de cobertura.
- Amplia CI navegador exact SHA sin deshabilitar casos anteriores; A0 static/provenance y secretos, A1-A3 Angular lint/build + Chromium positivo/negativo, A4 checks causales y readback, A5 por otra Task Javier. Revert normal, sin PR/ramas/PROD.

## 2026-11-14 — SOLQARYN V5 C01-01.M03.03 autorización y aislamiento por permiso empresarial [Javier :10]
- Nueva guardia Application .NET 10 fail-closed: membresía actual del servidor, rol empresarial activo, permiso relacional explícito por módulo/acción y sucursal de la misma empresa; rol global legacy no concede acceso.
- xUnit positivos tenant A y sucursal A, negativos empresa B antes de I/O, revocación de membresía, cambio de rol, permisos ausentes/inactivos/foreign, módulo inválido y sucursal B. La fuente es un fixture controlado, no HTTP/JWT/EF/MySQL real.
- A0-A4 requieren CI backend y seguridad SHA exacto, Scope Lock, integridad de CHANGELOG y readback. A5 reservado para Javier independiente. Sin nueva ruta/DI/API, migración, trabajador, eventos, QA/main/PROD; rollback revert normal.

## 2026-11-14 — SOLQARYN V5 C00-04.M03.03 consulta de gobierno tenant-safe [Javier :10]
- Nuevo servicio Application Governance inerte con preflight tenant antes de I/O, permiso relacional actual Configuracion/Administrar y negativa de rol legacy; no ejecuta, aprueba ni persiste cambios.
- xUnit positivos tenant A/sucursal A; negativos empresa B, revocacion, rol cambiado, permiso ausente o accion incorrecta, sucursal B, propuesta sensible NO_GO y solicitud manipulada.
- A0-A4 validacion por CI .NET10/seguridad/autorizacion y Scope Lock exact SHA, CHANGELOG autocontenido y readback; A5 por otro Javier. Sin API/DI/JWT/DB/worker/webhook/QA/main/PROD; rollback git revert normal.

## 2026-11-14 — SOLQARYN V5 C01-01.M03.04 adaptador EF read-only identidad tenant [Javier :50]
- Fuente EF Core 10 de instantanea actual para Application: membresia empresa/usuario, rol vigente con permisos relacionales, sucursal vinculada; filtros AsNoTracking y denegacion por defecto sin usar rol legacy.
- xUnit InMemory positivo A y negativos tenant B, revocacion, rol ajeno, usuario bloqueado y sucursal invalida; prueba Integration MySQL efimero local para A/B y revocacion, sin acceso remoto.
- A0-A4 requieren GitHub Actions .NET, seguridad, MySQL, Scope Lock, CHANGELOG SHA exacto y readback. Sin migracion, DI/API/JWT, escritura, worker/webhook, QA/main/PROD; A5 por Javier independiente; revert git normal.

## 2026-11-14 — SOLQARYN V5 C00-04.M03.04 adaptador aislado de gobierno [Javier :30]
- Agrega adapter .NET 10 Infrastructure/Governance exclusivamente efímero, sin MySQL/EF/API/DI, que guarda lecturas no autoritativas por ID/revisión con cuota fija, datos validados y primer escritor local sin reemplazos.
- Pruebas xUnit reales a ejecutar en GitHub Actions: 64 escritores/1 ganador local, casos positivos de versionado y negativos de ID, SHA, estado, UTC y duplicación; no acredita CAS distribuido, JWT/RBAC ni tenant.
- A0-A4 condicionadas a CI backend, seguridad, Scope Lock y CHANGELOG exact-SHA, readback institucional y A5 independiente de otra Task Javier. Sin webhooks, dispatcher, workers, nuevas Tasks, QA/main/PROD. Rollback git revert normal.

## 2026-11-14 — SOLQARYN V5 C01-02.M01.06 salida documental capabilities y RBAC [Javier :20]
- Contrato de bloqueo, rollback y salida M01 para C01-02; siete antecedentes LISTO con revisión A5 Javier válida y DAG individual VALIDADO/SI. No libera M02/M03 en cascada.
- Denegación por defecto: grant RBAC y entitlement futuro son restricciones acumulativas, siempre con membresía, tenant y ownership verificados; plan, UI o rol legacy no sustituyen grants. Positivos P01-P03 y negativos N01-N07 definidos como oráculos documentales, no pruebas runtime ejecutadas.
- A0-A4 documentales condicionadas a Scope Lock y procedencia V5 SUCCESS exact SHA, BITACORA 26 columnas y readback PLAN/COLA. A5 pendiente de otra Task Javier. Sin API, JWT, MySQL, migración, worker, dispatcher, cobros, QA/main/PROD; rollback git revert normal.

## 2026-11-14 — SOLQARYN V5 C01-03.M01.06 bloqueo, rollback y salida documental [Javier :10]
- Contrato de cierre M01 de configuración dinámica, siete predecesores concretos y seis negativas fail-closed; los A5 históricos de ejecutores retirados no habilitan M02, revisiones correctivas Javier registradas sin reescribir BITACORA.
- Sin código runtime, cambios JWT/RBAC/tenant, migraciones, worker, webhook o eventos; M02 únicamente por DAG propio VALIDADO/SI y nueva CAS/readback, QA/main/PROD prohibidos sin orden adicional.
- Requiere A0–A4 documental y checks Scope Lock/V5 sobre SHA exacto, A5 independiente distinta del autor J10 y rollback por commit git revert normal.

## 2026-11-14 — SOLQARYN V5 C01-04.M01.06 salida documental expediente universal [Javier :20]
- Define bloqueo, reversión y liberación limitada de M01 para expediente/timeline: cinco preparativos propios y antecedentes C01-01/C01-02/C01-03 M01 verificables con A5 Javier, DAG individual VALIDADO/SI. Ninguna fase se libera en cascada.
- Rechazos para lectura cross-tenant, membresía revocada, adjuntos externos, observación presentada como aprobación, orden no determinista y owner ambiguo. El hallazgo de filtro explícito ausente en inventario es riesgo a investigar, no prueba de vulnerabilidad explotable. Oráculos P01-P03 y N01-N07 documentales, no runtime.
- A0-A4 documentales: publicación FF, Scope Lock y V5 provenance SUCCESS SHA exacto + BITACORA A:Z y readback. A5 por Task Javier independiente. Sin API, worker, webhook, MySQL, cambios JWT, eventos activos, QA/main/PROD; rollback git revert normal.

## 2026-11-14 — SOLQARYN V5 C02-01.M01.06 bloqueo y rollback tenant server-side [Javier :10]
- Cierra solo el contrato documental M01 de contexto tenant: seis predecesores LISTO/A5 Javier, 3 casos positivos y 7 negativos sobre spoofing host/headers, pertenencia revocada, RBAC, sucursal, A/B, caché y privacidad.
- No implementa resolver host, API, JWT, middleware, EF/MySQL ni DNS; ningún worker/evento/Task nuevo. Evaluación M02 exclusivamente por DAG técnico individual y CAS readback; QA/main/PROD no autorizados.
- A0–A4 documentales condicionadas a Scope Lock y proveniencia CHANGELOG exact SHA y BITACORA A:Z; A5 independiente por otro Javier; rollback git revert normal.

## 2026-11-14 — SOLQARYN V5 C02-04.M01.06 salida documental consola global [Javier :20]
- Contrato de bloqueo, rollback y salida M01 de consola global auditada; 40/40 antecedentes M01 LISTO+A5 Javier, DAG por ID VALIDADO/SI y CAS único. No libera M02/privilegios en cascada.
- Separa plataforma de ERP con requisitos futuros de MFA/reauth, motivo, TTL, scope por tenant/recurso/acción, auditoría crítica e idempotencia. P01-P03 y N01-N07 son oráculos documentales, no tests de privilegio en vivo.
- A0-A4 documentales con Scope Lock y V5 CHANGELOG SUCCESS exact-SHA + BITACORA 26 campos y PLAN/COLA readback; A5 por Task Javier distinta. Sin MFA/JWT/MySQL/API/worker/eventos nuevos, QA/main/PROD; rollback git revert normal.

## 2026-11-14 — SOLQARYN V5 C01-03.M02.01 entidades, ownership e invariantes de configuración dinámica [Javier :30]
- Define catálogo de claves allowlisted, valor por ámbito y owner, revisiones CAS, auditoría y diez negativos; separa ConfigEmpresa tenant-scoped del legado EmpresaConfiguracion global sin inferir aislamiento inexistente.
- Contrato M02 estrictamente documental; no crea APIs, permisos, migraciones, flags activos, trabajadores, eventos o integraciones. A0–A3 documentales, A4 requiere checks Scope Lock/procedencia SHA exacto y readback.
- A5 independiente pendiente de otra Task Javier; sin QA/main/PROD ni certificación runtime. Rollback mediante git revert normal.

## 2026-11-14 — SOLQARYN V5 C01-03.M02.02 API, DTO, errores y compatibilidad documental [Javier :30]
- Define operaciones futuras de clave/valor allowlisted, DTO tipados, versión/idempotencia y ocho errores seguros; conserva rutas existentes de EmpresaConfiguracion y distingue público/privado con permiso tenant real.
- Tres positivos y diez negativas documentales de A/B, RBAC, revocación, inyección, versión y secretos; no crea ni modifica endpoints, código .NET/Angular, BD, flags, workers o eventos.
- Scope Lock y procedencia V5 exact-SHA requeridos para A4, readback institucional y A5 independiente pendiente de otra Task Javier; sin QA/main/PROD, rollback por git revert.

## 2026-11-14 — SOLQARYN V5 C01-03.M02.03 persistencia, índices, migraciones y fixtures [Javier :30]
- Contrato documental de catálogo tipado por scope/owner, índices propuestos, revisión CAS, auditoría e idempotencia, distinguiendo ConfigEmpresas tenant-first de EmpresaConfiguraciones legacy global; revisa migración N67C vigente sin ejecutarla.
- Doce fixtures simbólicos (cuatro positivos y ocho negativos) y plan de expansión/rollback futuro con MySQL/EF efímero; no crea tablas, datos, migraciones, scripts activos, workers o autorizaciones.
- A0–A4 documentales sujetos a checks Scope Lock y CHANGELOG V5 exact-SHA y readback institucional. A5 por Javier distinto de la autoría, sin QA/main/PROD; rollback del documento por git revert normal.

## 2026-11-14 — SOLQARYN V5 C02-01.M02.01 entidades, ownership e invariantes tenant [Javier :40]
- Contrato M02 documental: entidades Usuario, Empresa y UsuarioEmpresa existentes; contexto comercial server-side, ownership por recurso/sucursal, host propuesto y auditoría mínima; 10 invariantes fail-closed y casos P01-P04/N01-N10 como oráculos futuros, no tests runtime.
- Referencias leídas de código .NET y cierre M01.06, predecesores con A5 Javier y DAG propio VALIDADO/SI; no nuevas API, JWT, migraciones, datos, workers, eventos, cuentas ni permisos.
- A0-A4 documentales sujetos a checks Scope Lock y V5 provenance exact-SHA, CAS institucional y BITACORA A:Z; A5 independiente por Task Javier distinta. Sin QA/main/PROD; rollback git revert normal.

## 2026-11-14 — SOLQARYN V5 C01-03.M02.04 configuración tipada, ámbitos y flags seguros [Javier :30]
- Catálogo documental de claves allowlisted, tipos, permisos, owner, ámbitos y versionado; separa PlanModulo entitlement de RBAC, evita secretos y valores ejecutables y conserva ThemeApplierService hex/accesibilidad.
- Cuatro positivos y doce negativos diseñados de cross-tenant, revocación, inyección, scopes, concurrencia y flags reservados, sin implementar API, BD, Angular, workers, eventos ni privilegios.
- A0–A4 documentales supeditadas a Scope Lock y CHANGELOG V5 exact-SHA, readback institucional, A5 pendiente de otra Task Javier; no QA/main/PROD. Rollback git revert normal.

## 2026-11-14 — SOLQARYN V5 C02-01.M02.02 contrato API, DTO, errores y compatibilidad tenant [Javier :40]
- Define comportamiento de rutas tenant-context existentes y DTO/host-resolution futuros sin implementarlos; 12 validaciones V01-V12, cuatro positivos y doce negativos documentales de JWT, membresía, RBAC, aislamiento A/B, spoofing y errores seguros.
- Contrato M02 documental con fuente de tres blobs de código observados y gate institucional por ID; no crea API, migraciones, permisos, JWT, workers, eventos, cuentas o datos productivos.
- A0-A4 documentales sujetos a Scope Lock/V5 provenance SHA exacto y BITACORA/PLAN/COLA readback; A5 pendiente de otra Task Javier. Sin QA/main/PROD, rollback git revert normal.

## 2026-11-14 — SOLQARYN V5 C02-01.M02.03 diseño persistencia, índices y fixtures tenant [Javier :40]
- Documenta esquema EF existente UsuarioEmpresas y Empresas con FK restrictivas/índices reales del código, ownership tenant y un eventual host binding condicional, sin crear ni ejecutar migraciones/SQL, nuevos grants o datos.
- Plan futuro expand/contract reversible y 12 fixtures simbólicos F01-F12 (4 positivos, 8 negativos) para aislamiento A/B, membresía revocada, host falso, índice único, rol y auditoría; no se presentan como tests MySQL/HTTP.
- A0-A4 documentales condicionadas a Scope Lock y V5 provenance exact-SHA, BITACORA A:Z y PLAN/COLA readbacks; A5 por Task Javier distinta. Sin QA/main/PROD, rollback git revert normal.

## 2026-11-14 — V5 C01-03.M02.05: protocolo documental de eventos de configuración [Javier :50]
- Define sobre conceptual post-commit, idempotencia ligada a actor/tenant/clave/version, correlación opaca, límites de cuota y prohibición explícita de payload privado y elevación JWT/RBAC.
- Incorpora 4 oráculos positivos y 10 negativos simbólicos sobre repetición, A/B, permisos revocados, versión obsoleta, timeouts, inyección y consumidor duplicado; son criterios documentales, no pruebas runtime.
- Sin desplegar eventos, webhook, dispatcher, workers, API, migración, secretos ni Tasks; M03/M05 dependen de DAG individual, CI causal y A5 independiente. Scope Lock y CHANGELOG exact-SHA; rollback git revert normal, jamás QA/main/PROD.

## 2026-11-14 — V5 C02-01.M02.04: catálogo tipado y ámbitos seguros para tenancy [Javier :50]
- Especifica claves/flags de contexto tenant, límites de ámbito PLATFORM/TENANT/BRANCH/USER y política de denegación: membresía y RBAC server-side nunca se sustituyen por plan, header, host o flag.
- Registra 4 oráculos positivos y 10 negativos documentales de aislamiento A/B, caché revocada, host suplantado, inyección, versión y secretos; no presenta pruebas de ejecución ni configuraciones activadas.
- Sólo Markdown y CHANGELOG; sin crear endpoints, flags operativos, MySQL/EF, migraciones, workers, eventos o Tasks. A5 independiente y CI Scope Lock/V5 exact-SHA; rollback git revert, sin QA/main/PROD.


## 2026-11-14 — V5 C02-04.M02.01: entidades e invariantes conceptuales de consola global [Javier :00]
- Define contrato documental de ownership global vs empresarial, MFA/reauth futuros, concesiones excepcionales de alcance mínimo y auditoría crítica fail-closed; inspecciona cuatro fuentes reales por blob SHA y seis preparaciones M01 con A5 acreditado.
- Matriz de 16 invariantes y 4 criterios positivos/14 negativos simbólicos; NO son pruebas ejecutadas ni habilitan implementación IAM, JWT, grants, SQL, frontend, workers, eventos, webhooks o QA/main/PROD.
- Requiere A0-A4 documentales con GitHub Scope Lock y V5 provenance exact-SHA, PLAN/COLA/BITACORA readback; A5 por otra Task Javier. Rollback git revert normal.

## 2026-11-14 — V5 C02-01.M02.05: contrato documental de correlación de contexto tenant [Javier :50]
- Define envelope conceptual de cambios autorizados por servidor, idempotencia por actor/empresa/revisión, trazas mínimas, cuota y rechazo de host, header o flag como autoridad; eventos exclusivamente futuros.
- Fija cuatro oráculos positivos y diez negativos simbólicos para aislamiento A/B, membresía revocada, payload distinto, replay, timeout, host falso y consumidor duplicado; no son tests ejecutados.
- Sin implementar webhooks, bus, dispatcher, workers, API, tablas, migraciones, JWT/RBAC o permisos; A5 independiente y Scope Lock/V5 exact-SHA, rollback git revert normal; no QA/main/PROD.


## 2026-11-14 — V5 C02-04.M02.02: contrato documental API/DTO y compatibilidad de consola global [Javier :00]
- Documenta fronteras actuales GET global paginado, borrador y alta controlada frente a seis flujos futuros de acceso excepcional; contrasta source blobs de controller, scope, interface y allowlist sin modificar endpoints ni grants.
- Define doce invariantes, ocho categorías de errores objetivo y 5 casos positivos/15 negativos simbólicos con A/B, MFA, reauth, auditoría y compatibilidad; no son pruebas runtime ni autorización de accesos.
- A0-A4 documentales sujetos a Scope Lock y V5 provenance exact-SHA, PLAN/COLA/BITACORA readback; A5 exige otra Task Javier. Sin código API, IAM, JWT, MySQL, workers, eventos, QA/main/PROD; rollback git revert normal.

## 2026-11-14 — V5 C02-04.M02.03: diseño documental de persistencia de consola global [Javier :50]
- Contrato futuro de grants excepcionales, comprobantes MFA de un solo uso, TTL/revocación, auditoría crítica, idempotencia y relaciones de propiedad; separa explícitamente roles plataforma/ERP y carece de SQL ejecutado.
- Incluye plan expand/rollback condicionado y 4 fixtures positivos/10 negativos simbólicos para aislamiento A/B, MFA/reauth, abuso de scope, simultaneidad, expiración y falla de auditoría; no son pruebas MySQL.
- Sólo Markdown y CHANGELOG: ningún grant, endpoint, migración, base productiva, worker o evento real. A5 independiente, Scope Lock/V5 exact-SHA, rollback documental git revert y sin QA/main/PROD.


## 2026-11-14 — V5 C02-04.M02.04: configuración y flags seguros de consola global (contrato) [Javier :00]
- Define opciones tipadas futuras, ámbitos y precedencia fail-closed frente a configuración real existente de `PlataformaEmpresasController`; no cambia valores de flags, proveedor, appsettings, scopes ni privilegios.
- Documenta doce invariantes, 4 casos positivos y 14 negativos simbólicos de identidad global, MFA/reauth, TTL, cuotas, no-store, auditoría y kill-switch; no son pruebas ejecutadas ni activaciones de consola.
- Solo documento y CHANGELOG, con Scope Lock/V5 exact-SHA, BITACORA/PLAN/COLA readback, A5 independiente posterior. Sin código, SQL, JWT/RBAC nuevo, workers, eventos, QA/main/PROD. Rollback git revert normal.


## 2026-11-14 — V5 C02-04.M02.05: diseño inerte de eventos, idempotencia y correlación [Javier :00]
- Contrato documental de sobre conceptual, cuotas, replay, deduplicación y correlación opaca; rastrea RegistroAuditoria y control de plataforma por blobs reales, sin sustituir BITACORA institucional.
- Doce invariantes y 5 positivos/14 negativos simbólicos; no son pruebas runtime ni autorizan emitir, consumir, instalar, ejecutar o programar eventos, webhooks, brokers, workers o Tasks.
- Scope Lock y V5 provenance exact SHA, PLAN/COLA/BITACORA readback, A5 independiente pendiente; sin API/JWT/MySQL/secretos ni QA/main/PROD, rollback git revert normal.

## 2026-11-14 — SOLQARYN V5 C04-04.M01.01 alcance documental de cobros por enlace (PRIMARY DEV 05 JAVIER)
- Se delimita objetivo y no-alcance de pagos por enlace, webhook firmado, replay, conciliación y reversión provider-neutral en `docs/v5/c04-04-m01-01-alcance-cobros-enlaces-provider-neutral.md`, sin ejecutar pagos ni activar proveedores.
- Preflight DAG documental validado con 60/60 prerrequisitos LISTO; se distinguen invariantes NR01–NR10, cinco positivos y doce negativos FUTUROS, no tests runtime.
- A0–A3 son controles documentales; A4 requiere checks Scope Lock y procedencia del CHANGELOG en SHA publicado y readback institucional; revisión A5 separada, solo otra Task Javier. No QA/main/PROD.

## 2026-11-14 — SOLQARYN V5 C04-04.M01.02 actores y superficies de cobros (PRIMARY DEV 05 JAVIER)
- Se inventarian doce actores, dieciseis interfaces y fronteras de confianza de cobros por enlace, webhook, conciliación y reversión en \`docs/v5/c04-04-m01-02-actores-interfaces-superficies-cobros.md\`; no se implementan proveedores ni pagos.
- Se distinguen componentes ya existentes de funciones futuras no desplegadas, con diez invariantes, cinco positivos y doce negativos propuestos, preservando tenant/JWT/RBAC, firmas e idempotencia.
- A0–A3 son documentales; A4 requiere Scope Lock y procedencia CHANGELOG exact-SHA con readback PLAN/COLA/BITACORA; A5 independiente queda pendiente. Sin QA/main/PROD.

## 2026-11-14 — SOLQARYN V5 C04-04.M01.03 auditoría causal de fuentes de cobros (PRIMARY DEV 05 JAVIER)
- Se inventarian trece fuentes con blobs GitHub y límites de verificación para pagos online, ingress webhook, EF, xUnit y certificaciones históricas en `docs/v5/c04-04-m01-03-auditoria-fuentes-contratos-cobros.md`.
- Se registran doce brechas técnicas y escenarios documentales futuros, sin suponer pasarela live, CAS distribuido, migración MySQL real o ejecución de xUnit con un cambio Markdown.
- A0–A3 documentales; A4 depende de diff, Scope Lock y procedencia de CHANGELOG exact-SHA y readback institucional. A5 independiente pendiente. Sin QA/main/PROD.

## 2026-11-14 — SOLQARYN V5 C04-04.M01.04 riesgos y decisiones de cobros (PRIMARY DEV 05 JAVIER)
- Se documenta un registro de dieciséis riesgos con mitigaciones y evidencias, más diez decisiones comerciales/técnicas abiertas, en `docs/v5/c04-04-m01-04-riesgos-supuestos-decisiones-cobros.md`.
- Se separan hechos de código, supuestos y aprobaciones futuras; no se habilita proveedor, cobro, migración, dispatcher ni acción productiva.
- A0–A3 documentales; A4 exact-SHA requiere Scope Lock, procedencia CHANGELOG y readbacks; A5 independiente queda pendiente, sin QA/main/PROD.

## 2026-11-14 — SOLQARYN V5 C04-04.M01.05 aceptación funcional/técnica de cobros (PRIMARY DEV 05 JAVIER)
- Se definen veinte criterios de aceptación observables para pagos por enlace, webhook, replay, conciliación, aislamiento tenant y reversión, todos como NO_EJECUTADO, en `docs/v5/c04-04-m01-05-criterios-aceptacion-cobros.md`.
- Se separan pruebas futuras .NET/MySQL/Angular/proveedor de la validación documental actual; no se autoriza cobro real, migración, dispatcher ni configuración de secretos.
- A4 documental requiere Scope Lock y procedencia CHANGELOG exact-SHA, readback PLAN/COLA/BITACORA. A5 independiente pendiente antes de QA/main/PROD.

## 2026-11-14 — V5 C05-01.M01.01: alcance y no-regresión de design tokens [Javier :50]
- Delimita el objetivo de tipografía, paleta, espaciado, foco, estados y responsive del sistema de diseño con tres fuentes SCSS y rutas Angular verificadas por blob SHA; no afirma catálogo completo ni E2E ejecutado.
- Define doce invariantes NR01–NR12 y cinco positivos/doce negativos documentales futuros de contraste, teclado, 320px, modales, branding seguro y aislamiento A/B; no sustituye JWT/RBAC server-side.
- Solo Markdown y CHANGELOG, sin cambiar estilos activos, API, BD, permisos, workers, eventos o Tasks; A4 condicionado a Scope Lock/V5 exact-SHA, readback institucional y A5 independiente. Rollback git revert, sin QA/main/PROD.

## 2026-11-14 — V5 C05-01.M01.02: interfaces, actores y superficies de design system [Javier :50]
- Inventario documental de actores y nueve entradas/salidas I01–I09 para SCSS, Angular, modales, formularios, navegación, estados, foco y preferencias de marca; identifica fuentes reales con SHA o rutas sin inventar implementación.
- Cinco positivos y doce negativos son oráculos futuros, NO pruebas runtime; preservan 320px, teclado, reduced-motion, límites tenant A/B y JWT/RBAC server-side frente a tokens, tema o host.
- Solo documento y CHANGELOG, sin cambiar UI activa, API, datos, permisos, workers, eventos ni Tasks; A4 Scope Lock/V5 exact-SHA + readback, A5 independiente pendiente y rollback git revert. Sin QA/main/PROD.

## 2026-11-14 — V5 C05-01.M01.03: manifiesto de fuentes y brechas de design system [Javier :50]
- Auditoría documental de nueve archivos reales por blob SHA: SCSS, accesibilidad, rutas, diálogo, feedback ARIA, navegación y ThemeApplier con spec presente; distingue hechos de pruebas pendientes.
- Ocho dimensiones de diagnóstico y cinco oráculos positivos/doce negativos futuros: contraste, foco, 320px, A/B, CSS seguro y permiso backend; no atribuye Angular/Playwright/HTTP ejecutados a esta lectura.
- Solo documento y CHANGELOG sin cambios UI/API/datos, workers, eventos, secretos o Tasks. A4 requiere Scope Lock y V5 exact-SHA y readback; A5 independiente de otra Task, rollback git revert normal, sin QA/main/PROD.

## 2026-11-14 — V5 C04-04.M01.06: gate documental y rollback de cobros provider-neutral [Javier :50]
- Cierra contractualmente M01 de enlaces de cobro con diez puertas G01–G10, políticas NO_GO, reversión futura sin operaciones reales y diferenciación clara entre preparación M02 y autorización comercial/QA/PROD.
- Verifica cinco antecedentes C04-04.M01.01–05 en LISTO con A5 independiente J50 sobre sus SHA originales y 60 prerrequisitos documentales; AC01–AC20 permanecen NO_EJECUTADOS, D01–D10 sin aprobación económica.
- Cuatro positivos/doce negativos son criterios de revisión documental, no tests .NET/MySQL/web; solo Markdown y CHANGELOG, sin cobros, webhooks, dispatchers, secretos o migraciones. Scope Lock/V5 exact-SHA, BITACORA/PLAN/COLA readback y A5 independiente posterior obligatorios, rollback git revert.

## 2026-11-14 — V5 C05-01.M01.04: riesgos, supuestos y decisiones design system [Javier :20]
- Registra R01–R12 riesgos potenciales, S01–S08 supuestos falsables y D01–D08 decisiones PENDIENTE; no declara vulnerabilidades, cumplimiento WCAG o políticas aprobadas.
- Coteja nueve blobs reales Angular/SCSS y las fuentes M01.01–03: foco, contraste, responsive 320px, reducción de movimiento, tenant A/B, tema allowlisted y autorización JWT/RBAC server-side.
- Solo documentación y CHANGELOG en dev, A0–A3 documentales, A4 Scope Lock/V5 exact SHA más PLAN/COLA/BITACORA, A5 independiente por otro TASK_ID; CI runtime NO_APLICA al diff Markdown, rollback git revert sin QA/main/PROD.

## 2026-11-14 — V5 C05-02.M01.05: aceptación funcional y técnica de branding y presets [Javier :30]
- Criterios de aceptación futuros P01–P08 y escenarios de rechazo N01–N16 para tema global, futura marca tenant, previews, publicación versionada y rollback; cada oráculo permanece NO_EJECUTADO, no equivale a prueba runtime.
- Diferencia el TemaVisual global existente de branding multiempresa no implementado; D01–D08 y gates M01.06 siguen pendientes. Respeta JWT/RBAC/tenant A/B, allowlist, accesibilidad, idempotencia y ausencia de escrituras en datos reales.
- Solo documento M01.05 y CHANGELOG; A0–A4 documentales con Scope Lock/V5 provenance exact-SHA y PLAN/COLA/BITACORA readback. A5 independiente de otra Task Javier, rollback git revert; sin UI/backend/DB nuevos, eventos, workers, QA/main/PROD.

## 2026-11-14 — V5 C05-01.M01.05: aceptación documental de design system [Javier :20]
- Define AC01–AC14 y N01–N14 como oráculos FUTUROS NO_EJECUTADOS de contraste, foco, teclado, diálogo, formulario/ARIA, 320px, reduced-motion, tokens y aislamiento de tema tenant A/B.
- Vincula fuentes verificadas M01.01–04, NR01–NR12, I01–I09, F01–F09, riesgos R01–R12 y decisiones D01–D08 pendientes; no atribuye Playwright, Angular, .NET, MySQL ni cumplimiento WCAG sin pruebas.
- Solo nuevo Markdown y CHANGELOG; A0–A3 documentales, A4 Scope Lock/V5 exact SHA y readback institucional, A5 independiente sin autoaprobación, rollback git revert. No QA/main/PROD, eventos ni secretos.

## 2026-11-14 — V5 C10-01.M01.01: objetivo y no-regresión de storage provider-neutral [Javier :30]
- Delimita interfaz futura para imágenes, documentos, videos y exportaciones, con doce invariantes de propietario/tenant, JWT/RBAC, MIME, tamaño, checksum, URLs temporales, fallos, auditoría y rollback.
- Define P01–P05 y N01–N12 como oráculos FUTUROS NO_EJECUTADOS; distingue Cloudinary de transición de Hostinger/Cloudflare R2 como topología objetivo sin autorizar cambios ni suponer archivos existentes.
- Solo documento M01.01 y CHANGELOG, con Scope Lock y V5 provenance exact-SHA, readback institucional y A5 independiente posterior; sin código, storage, MySQL, secretos, workers, pagos, QA/main/PROD ni force-push.

## 2026-11-14 — V5 C05-03.M01.01: objetivo y fronteras del builder gobernado [Javier :20]
- Delimita builder FUTURO de páginas/secciones configurables con catálogo allowlisted de componentes/campos, sin HTML/JS/SQL ejecutable, URLs peligrosas, payloads excesivos ni bypass JWT/RBAC/tenant.
- Coteja seis blobs reales Angular/SCSS/API de tema y rutas; NR01–NR12, P01–P05 y N01–N12 son contratos de no-regresión y pruebas FUTURAS NO_EJECUTADAS, no una implementación de builder.
- Solo Markdown/CHANGELOG dev; A0–A3 documentales, A4 Scope Lock/V5 exact SHA y readback, A5 revisión independiente, rollback git revert, sin QA/main/PROD ni cambios ejecutables.

## 2026-11-14 — V5 C10-01.M01.02: actores y fronteras de storage neutral [Javier :30]
- Documenta doce actores/superficies y doce interfaces futuras de upload, descarga, exportación, metadatos, revocación, auditoría, verificación MIME/size/checksum y migración reversible; NO implementa endpoints, SDK ni buckets.
- Registra P01–P05/N01–N12 como oráculos futuros NO_EJECUTADOS, con rechazo cross-tenant, JWT/RBAC backend, control de URL temporal, seguridad de PII y fallos sin objeto parcial.
- Solo documento y CHANGELOG, con Scope Lock/V5 provenance exact-SHA, readback PLAN/COLA/BITACORA y A5 independiente posterior. Cloudinary sigue vigente; R2/Hostinger objetivo no activado, sin QA/main/PROD.

## 2026-11-14 — V5 C15-03.M01.05: aceptación observable de auditoría de seguridad y tenancy [Javier :20]
- Define AC01–AC16 y N01–N16 como positivos y negativos FUTUROS NO_EJECUTADOS para JWT/RBAC, selector de empresa, sucursales, lecturas EF, escrituras, host, soporte, archivos, jobs, errores, caché y CI exact-SHA.
- Especifica fixture A/B y MySQL efímero, evidencias HTTP/DB/logs, ocho requisitos no funcionales y condición NO_APLICA para funcionalidades no implementadas; no declara ausencia de vulnerabilidades ni pruebas runtime realizadas.
- Preparación documental M01.05 sólo en dev, con A0–A4 y Scope Lock/procedencia SHA verificables después de publicar; A5 corresponde a otra Task Javier y M01.06 no se libera automáticamente; rollback por git revert, sin QA/main/PROD.

## 2026-11-14 — V5 C15-04.M01.01: alcance y no-regresión UX, accesibilidad y rendimiento [Javier :20]
- Delimita landing, storefront, portal, ERP y builder futuro, responsive, teclado/foco, formularios, lectura asistida, ligereza visual y carga progresiva con evidencias exigibles; no certifica WCAG ni métricas o implementación inexistentes.
- Coteja fuentes Git Angular/SCSS y Playwright con blobs exactos, define NR01–NR14 y P01–P08/N01–N08 como oráculos FUTUROS NO_EJECUTADOS sin inventar tests ni umbrales.
- Sólo documentación de M01.01 en dev, A0–A4 documentales, Scope Lock/V5 exact SHA tras publicar y A5 independiente; rollback git revert, sin cambios de código, datos, secretos, QA/main/PROD.

## 2026-11-14 — V5 C15-04.M01.02: actores, interfaces y fronteras de UX/accesibilidad [Javier :20]
- Identifica A01–A12 actores y consumidores, IO01–IO12 entradas/salidas y F01–F10 fronteras de confianza para landing, portal, ERP, teclado, foco, formularios, responsive, motion, red y tenancy.
- Trazabilidad contra M01.01, cinco fuentes Angular/SCSS/Playwright con blobs observados; casos positivos/negativos FUTUROS NO_EJECUTADOS, sin afirmar cumplimiento WCAG ni implementaciones de builder/checkout.
- Sólo Markdown/CHANGELOG en dev; A0–A4 documentales y CI Scope Lock/V5 exact-SHA, A5 independiente de otro Javier; rollback git revert, sin cambios runtime, QA/main/PROD.

## 2026-11-14 — V5 C15-04.M01.03: fuentes y brechas auditables UX/accesibilidad/rendimiento [Javier :20]
- Verifica doce fuentes y blobs Git de Angular/SCSS/Playwright y antecedentes V5; inventario parcial, no afirmación de auditoría completa ni certificación WCAG.
- Registra doce brechas G01–G12 no verificadas, matriz de superficies y pruebas futuras P01–P08/N01–N08, fuentes exactas, dispositivo, captura y SHA, sin inventar resultados E2E, Lighthouse, lector asistivo o tenant.
- Cambios sólo Markdown/CHANGELOG en dev, A0–A4 documentales y checks Scope Lock/V5 exact SHA, A5 independiente; sin código, datos, secretos, QA/main/PROD ni servicios.

## 2026-11-14 — V5 C15-04.M01.04: riesgos, supuestos y decisiones UX/accesibilidad/rendimiento [Javier :30]
- Documenta R01–R12 riesgos potenciales de responsive, teclado/foco, formularios, contrastes, loading, rendimiento, aislamiento tenant A/B y CI exact-SHA; no declara defectos o cumplimiento WCAG confirmados.
- Registra S01–S08 supuestos NO_VERIFICADOS, D01–D08 decisiones pendientes y P01–P08/N01–N08 como escenarios FUTUROS_NO_EJECUTADOS; trazabilidad causal a M01.01–03 y G01–G12.
- Diff sólo Markdown y CHANGELOG en dev; A0–A4 documentales con Scope Lock/V5 en SHA exacto, A5 independiente; sin Angular, API, DB, secretos, QA/main/PROD o infraestructura real.

## 2026-11-14 — V5 C15-04.M01.05: aceptación observable UX/accesibilidad/rendimiento [Javier :30]
- Define AC01–AC16 positivos y negativos FUTUROS_NO_EJECUTADOS para rutas, teclado/foco, responsive, semántica, motion, estados de error, tokens, caché tenant A/B y métricas reproducibles, sin declarar auditoría ejecutada.
- Especifica RNF01–RNF10 PENDIENTE_MEDICION y matriz de evidencia por viewport, navegador, red, fixture sintético, SHA y CI; enlaza M01.01–04 y brechas G01–G12, sin inventar presupuestos ni WCAG PASS.
- Documentación exclusiva en dev, A0–A4 documentales con Scope Lock/V5 exact-SHA y A5 independiente posterior; no Angular ejecutable, .NET, MySQL, DNS, QA/main/PROD ni permisos de migración.

## 2026-11-14 — V5 C15-05.M01.01: alcance y no-regresión de auditoría operativa [Javier :30]
- Delimita auditoría V5 de PLAN/COLA/DAG, autoridad exclusiva de seis Tasks Javier, CAS owner, BITACORA A:Z, telemetría auto-reportada, CI exact-SHA y A5 independiente; sin modificar runtime ni programación.
- Define NR01–NR14 y P01–P08/N01–N08 como oráculos FUTUROS_NO_EJECUTADOS sobre concurrencia, duplicados, permisos, evidencia, rollback y fail-closed; trazabilidad a fuentes actuales y riesgos a comprobar.
- Entrega documental en dev con A0–A4 y Scope Lock/V5 del commit exacto; A5 pendiente de otra Task Javier, sin QA/main/PROD, secretos, migraciones o infraestructura productiva.

## 2026-11-14 — V5 C15-05.M01.02: actores, interfaces y fronteras de auditoría operativa [Javier :30]
- Delimita A01–A12 actores/roles de las seis Tasks Javier, GitHub, Drive, CI, revisor A5 y consumidor institucional, sin atribuir nuevas autorizaciones físicas ni cuentas.
- Define IO01–IO12 entradas/salidas y F01–F10 fronteras con positivos/negativos FUTUROS_NO_EJECUTADOS: claim CAS, DAG, BITACORA append-only, telemetría local vs runtime nativo, CI exact-SHA y A5 diferenciado.
- Sólo documentación dev y CHANGELOG; A0–A4 documentales y checks Scope Lock/V5 SHA exacto, A5 independiente posterior; sin runtime nuevo, QA/main/PROD ni programación alterada.

## 2026-11-14 — V5 C15-05.M01.03: fuentes y brechas de auditoría operativa [Javier :30]
- Verifica ocho fuentes Git originales y SHA de AGENTS, Scope Lock, autoridad/status V5, Skill, arquitectura, procedencia CHANGELOG y contexto; la lectura no sustituye pruebas de automatización nativa.
- Registra doce brechas G01–G12 y ocho pares P01–P08/N01–N08 FUTUROS_NO_EJECUTADOS para CAS, DAG, BITACORA, CI SHA, A5, telemetría y rollback; sin afirmar ausencia de defectos.
- Sólo Markdown y CHANGELOG en dev, A0–A4 documentales y Scope Lock/V5 exact-SHA; A5 independiente posterior, sin Tasks nuevas, scopes externos, QA/main/PROD o modificaciones de runtime.

## 2026-11-14 — V5 C15-05.M01.04: riesgos y decisiones de auditoría operativa [Javier :30]
- Clasifica R01–R12 como riesgos potenciales NO_REPRODUCIDOS de identidad, CAS, DAG, BITACORA, CI, CHANGELOG, A5, monitor y rollback; no los presenta como incidentes observados.
- Registra S01–S08 NO_VERIFICADOS, D01–D08 PENDIENTES y P01–P08/N01–N08 FUTUROS_NO_EJECUTADOS con fuentes M01.01–03 y brechas G01–G12.
- Sólo documentación dev y sección CHANGELOG; A0–A4 documentales exact-SHA, A5 independiente posterior, sin cambios JWT/RBAC, DB, schedules, QA/main/PROD o infraestructura.

## 2026-11-14 — C01-01.M03.05 — integración de identidad universal (DEV)
- Registro DI Scoped de fuente EF de identidad y servicios de aplicación/autorización; no se añaden endpoints, workers, eventos ni migraciones.
- Pruebas xUnit de resolución y aislamiento de scopes, pertenencia tenant A/B, denegación por permiso ausente y membresía revocada; documentación de límites y rollback.
- A5 permanece pendiente de revisión independiente; no autoriza QA, main ni producción.
## 2026-11-14 — V5 C15-01.M03.01: reglas de dominio para cobertura [Javier :30]
- Implementa validador puro en Domain para conjuntos de requisitos autorizados y microtareas existentes, cotejo de revisión de fuente, formato de ID, referencias seguras, duplicidad y requisitos sin correspondencia.
- Añade pruebas xUnit positivas y negativas de revisión obsoleta, ID desconocido, duplicados, referencias con controles y cobertura incompleta; sin inventar requisitos ni liberar gates técnicos.
- Un resultado READY_FOR_EVIDENCE_REVIEW no es certificación de 2.340 IDs ni A5/CI/tenant; sin API, migración, dispatcher, escritura de Sheet desde backend, QA/main/PROD o infraestructura real.
## 2026-11-14 — V5 C14-04.M02.06: dependencias de puesta en marcha [Javier :30]
- Consolida dieciséis gates G01–G16, diez escenarios de rollback R01–R10 y diez pares positivos/negativos T01–T10 FUTUROS_NO_EJECUTADOS de go-live, soporte y monitoreo.
- Referencia M01 y cinco contratos M02 con sus SHA originales y revisiones A5 de otra Task; el documento no valida smoke real, backup/restore, DNS, TLS o disponibilidad.
- Sólo documento V5 y CHANGELOG en dev; Scope Lock y procedencia exact-SHA antes de LISTO_DEV, A5 independiente posterior, sin cambios MySQL, eventos, runtime, QA/main/PROD ni servicios reales.
## 2026-11-14 — V5 C14-04.M02.06: corrección de enlace documental [Javier :30]
- Rectifica la referencia a M01.06 contra el archivo existente en dev, manteniendo invariantes de seguridad, gates, alcance documental y A5 pendiente; no cambia runtime ni entornos.
## 2026-11-14 — V5 C15-04.M03.01: reglas de dominio de auditoría UX [Javier :30]
- Agrega validación server-side pura de observaciones de UX/accesibilidad/rendimiento: superficie y criterio allowlisted, tenant de sesión, SHA de origen, evidencia, comparación MIN/MAX y presupuestos medidos.
- Incluye pruebas xUnit positivas y negativas para aislamiento tenant A/B, origen obsoleto, latencia fuera de presupuesto, contraste inferior, referencias inseguras y métricas inválidas.
- READY_FOR_EVIDENCE_REVIEW no es una prueba Playwright, WCAG o p95 real, ni aprobación A5; no añade endpoints, workers, despliegues, migraciones, secretos, QA/main/PROD ni métricas inventadas.
## 2026-11-14 — V5 C15-03.M02.01: modelo de seguridad y tenancy [Javier :30]
- Define SEC01–SEC16 (modelo lógico), INV01–INV12 (fronteras de autoridad y auditoría) y diez pares positivos/negativos T01–T10 FUTUROS_NO_EJECUTADOS para auditoría de seguridad y tenant.
- Deriva precedencias de M01.06 con A5 independiente y fuentes PLAN/COLA/DAG, sin declarar cero P0/P1, ejecutar pentest ni conceder acceso JWT/RBAC.
- Únicamente documento V5 y CHANGELOG en dev; gates exact-SHA, A5 posterior de otra Task Javier, sin cambios en API, MySQL, frontend, secretos, QA/main/PROD ni proveedores.
## 2026-11-14 — V5 C15-03.M02.02: API y DTO de auditoría de seguridad [Javier :30]
- Propone ocho operaciones de lectura OP01–OP08, ocho DTO mínimos, doce errores E01–E12 y doce pares positivos/negativos V01–V12 como futuros contratos de seguridad/tenancy, sin nuevos endpoints.
- Distingue JWT/RBAC real y empresa A/B desde backend de datos no confiables del cliente; no reclama ejecutar pentest, tests de API ni certificar cero hallazgos.
- Exclusivamente docs/v5 y CHANGELOG en dev, gates exact-SHA Scope Lock/V5 y A5 de otra Task; sin migraciones, dispatcher, secretos, QA/main/PROD ni servicios reales.
## 2026-11-14 — V5 C15-03.M02.04: configuración segura de auditoría [Javier :30]
- Documenta jerarquía SYS/ENV/TEN/PRN/RUN/DOC, CFG01–CFG16, IV01–IV12 y diez pares T01–T10 FUTUROS_NO_EJECUTADOS para opciones de auditoría de seguridad/tenant.
- Mantiene deny-by-default JWT/RBAC/tenant A/B, credenciales fuera de Git, membresía revocable y distinción de lectura de Tasks frente a concesión de autoridad.
- Sólo docs/v5 y CHANGELOG en dev; sin activar flags, secret management, workers, API o migraciones; A5 independiente y controles Scope Lock/V5 exact-SHA antes de cierre.
## 2026-11-14 — V5 C15-03.M02.05: señales de auditoría de seguridad [Javier :30]
- Define EV01–EV12 conceptuales, envelope mínimo, ID01–ID12 de idempotencia y cuota y diez pares T01–T10 FUTUROS_NO_EJECUTADOS para tenancy, hallazgos y revisión A5.
- No crea webhooks, dispatcher, workers, colas, reintentos, programaciones ni privilegios; BITACORA A:Z sigue siendo evidencia histórica institucional.
- Sólo documento y CHANGELOG en dev, con Scope Lock y procedencia exact-SHA, readback y A5 posterior por otra Task Javier; sin afectar MySQL, JWT/RBAC runtime ni QA/main/PROD.

## 2026-11-14 — V5 C14-04.M03.01: validación pura de evidencias DEV para puesta en marcha [Javier :10]
- Implementa en Domain .NET 10 reglas de release DEV: grant explícito, SHA/manifest/digest coherentes, CI completed/success exact-SHA, A5 independiente y aislamiento tenant A/B.
- Añade pruebas xUnit positivas y negativas de entornos QA/PROD, permisos, SHA, checks cancelados/fallidos, autoaprobación, rollback y criterios no ejecutados; READY_FOR_EXTERNAL_EVIDENCE_REVIEW no implica producción ni A5.
- Agrega contrato de alcance y rollback; sin API, MySQL, migraciones, dispatcher, webhooks, workers, secretos, QA/main/PROD ni despliegues. Validación CI causal obligatoria antes de LISTO_DEV.

## 2026-11-14 — V5 C15-02.M02.01: entidades, ownership e invariantes conceptuales de auditoría de migración [Javier :30]
- Define ENT01–ENT16 y relaciones auditables para tenants, recursos, backups, objetos, dominios, TLS, secretos opacos, rollback y evidencia independiente, sin crear tablas ni desplegar proveedores.
- Fija INV01–INV16 y ocho pares P/N FUTUROS_NO_EJECUTADOS para aislamiento, idempotencia, restauración, CI exact-SHA, permisos y A5; no declara un entorno externo auditado.
- Sólo docs/v5 y CHANGELOG autocontenido en dev. A0–A4 documentales y Scope Lock/V5 exact SHA requeridos, A5 pendiente de otra Task Javier; sin migraciones, QA/main/PROD, DNS, datos reales o secretos.

## 2026-11-14 — V5 C15-02.M02.02: API/DTO conceptuales, validación y compatibilidad de auditoría [Javier :30]
- Contrato de 12 operaciones futuras API01–API12, 16 validaciones V01–V16 y 12 errores ERR01–ERR12 para inventario, tenancy, backup, restore, dominios y evidencia, sin endpoints reales ni ampliación de privilegios.
- Fija compatibilidad aditiva y ocho pares T01–T08 FUTUROS_NO_EJECUTADOS; los negativos de SHA/CI, A5, JWT/RBAC, host y tenant siguen fail-closed, sin declarar recursos externos comprobados.
- Dev y documento M02.02 + CHANGELOG autocontenido exclusivamente; A0–A4 documentales y checks Scope Lock/V5 exact-SHA, A5 pendiente de otra Task Javier; sin migraciones, eventos, secretos ni QA/main/PROD.

## 2026-11-14 — V5 C15-02.M02.02: reparación del guard Scope Lock [Javier :30]
- Corrige únicamente el nombre del error ERR09 a PROVIDER_CATALOG_UNVERIFIED en el contrato documental M02.02; se preservan semántica, pruebas futuras, contratos y propiedad.
- No altera scripts de seguridad, reglas de autorización, checks ni controles: el commit anterior quedó VALIDANDO con Scope Lock FAILED y este SHA nuevo exige CI causal SUCCESS antes de LISTO_DEV; rollback mediante git revert normal.

## 2026-11-14 — V5 C15-02.M02.04: ámbitos y configuración tipada de auditoría de migración [Javier :30]
- Contrato documental S01–S06 de scopes y CFG01–CFG14 de valores allowlisted con defaults fail-closed, sin crear settings reales, secrets ni migraciones.
- Define precedencia PR01–PR08 y negativos N01–N12 FUTUROS_NO_EJECUTADOS; ningún flag autoriza DNS, backend tenant, tareas externas, QA/main/PROD ni servicios de pago.
- Modifica sólo documento docs/v5 y CHANGELOG autocontenido en dev; requiere A0–A4 documentales con Scope Lock/V5 exact SHA y A5 independiente de otra Task Javier.

## 2026-11-14 — V5 C15-02.M02.03 persistencia, índices y fixtures conceptuales de auditoría arquitectónica [Javier :40]
- Define DAT01–DAT12, IDX01–IDX12, MIG01–MIG08 y FX01–FX12 positivos/negativos FUTUROS_NO_EJECUTADOS, preservando fail-closed para tenant A/B, NULL legado y autorización JWT/RBAC.
- Contrato exclusivamente documental: sin DDL, EF migrations, MySQL real, recursos externos, DNS, R2, workers, webhooks ni ejecución de pruebas de restauración; origen del esquema NO_ACREDITADO hasta análisis posterior.
- Publicación dev mediante SHA/CAS fast-forward y checks Scope Lock/V5 exactos; A0–A4 documentales, A5 de otra Task Javier y rollback git revert; no QA/main/PROD.

## 2026-11-14 — V5 C15-02.M02.05: señales futuras, idempotencia y correlación de migración [Javier :30]
- Contrato documental EV01–EV14, envelope mínimo y reglas ID01–ID12 de idempotencia, correlación, cuotas y fail-closed, sin crear webhooks, eventos runtime, jobs ni Task nuevas.
- Define diez pares T01–T10 FUTUROS_NO_EJECUTADOS, preservando contexto tenant A/B, A5 independiente, SHA causal y BITACORA append-only sin atribuir pruebas de proveedores.
- Sólo docs/v5 más CHANGELOG autocontenido en dev, A0–A4 documentales y checks Scope Lock/V5 exact SHA antes de LISTO, A5 por otro Javier; sin QA/main/PROD, secretos ni infraestructura.

## 2026-11-14 — V5 C15-05.M03.03 reglas .NET 10 de autorización y tenancy fail-closed [Javier :40]
- Añade Domain `V5OperationalAuditSecurityGate`, método Application de preflight protegido y pruebas xUnit positivas/negativas para tenant A/B, JWT/membresía, revocación, actor servicio, cliente spoofing, bypass de filtros y SHA.
- Incluso con contexto lógico válido, deniega escritura institucional: falta autenticación real del adaptador, pruebas HTTP/EF/MySQL y A5 independiente; ninguna nueva API, migración, worker, dispatcher, webhook o permiso productivo.
- Alcance sólo dev con CI backend/seguridad, Scope Lock/V5 exact-SHA, CAS y readbacks por estado; no QA/main/PROD, rollback git revert.

## 2026-11-14 — V5 C15-04.M03.02 preflight de auditoría UX en Application .NET 10 [Javier :40]
- Implementa `V5UxAuditApplicationService` y xUnit, reutilizando `V5UxAuditRules` sin modificar el Domain: catálogo/superficie, tenant A/B, SHA, presupuesto y evidencia fail-closed.
- Una observación válida sigue exigiendo fuente autenticada, medición externa real y revisión A5 ajena; todas las salidas niegan escritura institucional, acceso y certificación externa.
- Solo dev con Scope Lock/V5, backend y seguridad CI dirigidos; sin endpoints, Playwright live, DB, worker, webhooks, QA/main/PROD ni activación de infraestructura.

## 2026-11-14 — V5 C15-02.M02.06 contrato de salida de arquitectura/migración [Javier :40]
- Cierra sólo el diseño M02: OUT01–OUT16 barreras, RB01–RB08 rollback y EV01–EV08 evidencias futuras, apoyado en cinco M02 LISTO+A5 diferentes.
- El inventario cloud, restore, migraciones, JWT/RBAC/tenant A/B, DNS/TLS y seguridad HTTP/MySQL permanecen NO_ACREDITADOS o NO_EJECUTADOS; el contrato no activa proveedores ni libera QA/main/PROD.
- Documento+CHANGELOG autocontenidos en dev con publicación FF/CAS, Scope Lock/V5 exact SHA, A0–A4 documentales y A5 pendiente de otra Task Javier; rollback git revert.

## 2026-11-14 — V5 C10-01.M01.06: bloqueo, rollback y salida de storage neutral [Javier :00]
- Cierra solo el contrato documental terminal M01 de C10-01: BL01–BL14, RB01–RB10 y gate de paso a diseño M02 sin activar Cloudinary/R2/Hostinger ni migrar objetos.
- Contrasta cinco documentos M01.01–05 con blobs y A5 independientes, el precedente C01-03.M01.06, JWT/RBAC y tenant A/B; los tests HTTP/MySQL/checksum/restore siguen NO_EJECUTADOS.
- Dos Markdown dev por fast-forward/CAS, Scope Lock y V5 exact-SHA antes de LISTO_DEV; A5 pendiente de otra Task Javier, rollback git revert sin QA/main/PROD.

## 2026-11-14 — V5 C02-01.M02.06 salida documental de identidad/tenant [Javier :00]
- Cierra únicamente contrato M02 de C02-01, con BL01–BL16 y RB01–RB10 de JWT/RBAC, tenant A/B, host/ownership, eventos futuros e idempotencia; ningún evento o worker implementado.
- Revalida precedentes M01 6/6 y M02 5/5 LISTO+A5 y C01-01.M01.06, con última A5 independiente M02.05; pruebas positivas y negativas runtime permanecen NO_EJECUTADAS.
- Dos Markdown en dev con publicación fast-forward/CAS, Scope Lock y procedencia V5 exact-SHA, A5 pendiente otra Task Javier; sin MySQL, migraciones, QA/main/PROD ni secretos.

## 2026-11-14 — V5 C08-04.M01.06 salida documental de extensiones sectoriales [Javier :00]
- Contrato terminal M01 de capabilities sectoriales sin duplicar entidades/tenants ni implementar módulos; enumera BL01–BL16 y RB01–RB10 para rollback y no-regresión.
- Prerrequisitos M01.01–05 LISTO+A5 y DAG individual verificados, A5 de M01.04 independiente; activación/desactivación real, JWT/RBAC, MySQL y tenant A/B quedan NO_EJECUTADOS.
- Publicación solo documento+CHANGELOG en dev por FF/CAS, Scope Lock/V5 exact SHA, A5 pendiente de otra Task Javier y reversión git revert; no QA/main/PROD.

## 2026-11-14 — V5 C05-04.M02.01 modelo conceptual de ligereza UI [Javier :00]
- Define E01–E08, INV01–INV14 y cinco positivos/diez negativos futuros de UX ligera, tenancy A/B, deduplicación HTTP, acceso RBAC y medición de budget/CWV sin fingir valores medidos.
- Contrato M02.01 documental con M01.01–06 LISTO+A5, sin crear tablas, DTO, CSS, Angular, endpoints, métricas reales ni scripts de CI.
- Dev fast-forward/CAS, Scope Lock y provenance V5 SHA exacto, A5 de otra Task Javier pendiente y rollback git revert; no QA/main/PROD.

## 2026-11-15 — V5 C05-04.M02.02: Contratos API, DTO, errores y compatibilidad de ligereza UI [Javier :10]
- Contrato M02 documental de C05-04.M02.02 con escenarios positivos/negativos FUTUROS_NO_EJECUTADOS, fuentes Git reales y límites tenant A/B, JWT/RBAC, privacidad, accesibilidad y seguridad fail-closed.
- Sin cambios runtime de Angular/.NET, endpoints, DTO compilados, MySQL, flags, dispatcher, webhooks ni eventos; métricas bundle/CWV HTTP NO_MEDIDAS; M02.06 sigue sujeto a DAG y A5.
- Solamente docs/v5 y CHANGELOG en dev con CAS, Scope Lock y check V5 exact-SHA; A5 independiente pendiente; rollback git revert sin QA/main/PROD.

## 2026-11-15 — V5 C05-04.M02.03: Persistencia, índices, migraciones y datos de prueba de ligereza UI [Javier :10]
- Contrato M02 documental de C05-04.M02.03 con escenarios positivos/negativos FUTUROS_NO_EJECUTADOS, fuentes Git reales y límites tenant A/B, JWT/RBAC, privacidad, accesibilidad y seguridad fail-closed.
- Sin cambios runtime de Angular/.NET, endpoints, DTO compilados, MySQL, flags, dispatcher, webhooks ni eventos; métricas bundle/CWV HTTP NO_MEDIDAS; M02.06 sigue sujeto a DAG y A5.
- Solamente docs/v5 y CHANGELOG en dev con CAS, Scope Lock y check V5 exact-SHA; A5 independiente pendiente; rollback git revert sin QA/main/PROD.

## 2026-11-15 — V5 C05-04.M02.04: Configuración tipada, ámbitos y flags seguros de ligereza UI [Javier :10]
- Contrato M02 documental de C05-04.M02.04 con escenarios positivos/negativos FUTUROS_NO_EJECUTADOS, fuentes Git reales y límites tenant A/B, JWT/RBAC, privacidad, accesibilidad y seguridad fail-closed.
- Sin cambios runtime de Angular/.NET, endpoints, DTO compilados, MySQL, flags, dispatcher, webhooks ni eventos; métricas bundle/CWV HTTP NO_MEDIDAS; M02.06 sigue sujeto a DAG y A5.
- Solamente docs/v5 y CHANGELOG en dev con CAS, Scope Lock y check V5 exact-SHA; A5 independiente pendiente; rollback git revert sin QA/main/PROD.

## 2026-11-15 — V5 C05-04.M02.05: Eventos conceptuales, idempotencia, correlación y límites de ligereza UI [Javier :10]
- Contrato M02 documental de C05-04.M02.05 con escenarios positivos/negativos FUTUROS_NO_EJECUTADOS, fuentes Git reales y límites tenant A/B, JWT/RBAC, privacidad, accesibilidad y seguridad fail-closed.
- Sin cambios runtime de Angular/.NET, endpoints, DTO compilados, MySQL, flags, dispatcher, webhooks ni eventos; métricas bundle/CWV HTTP NO_MEDIDAS; M02.06 sigue sujeto a DAG y A5.
- Solamente docs/v5 y CHANGELOG en dev con CAS, Scope Lock y check V5 exact-SHA; A5 independiente pendiente; rollback git revert sin QA/main/PROD.

## 2026-11-15 — V5 C01-04.M03.02 aplicación de expediente universal fail-closed [Javier :10]
- Implementa servicio Application .NET 10 que compone las reglas Domain M03.01 con verificación de SHA y referencia de evidencia, y niega autorización definitiva de persistencia y medios.
- Añade pruebas xUnit positivas y negativas de tenant A/B, revocación, idempotencia, versión, SHA, adjuntos y revisión de decisiones; requiere CI exact-SHA antes de LISTO_DEV.
- Sin endpoints, DTO HTTP, DI, MySQL, migraciones, dispatcher, eventos, secretos, QA/main/PROD; rollback git revert, A5 independiente pendiente.

## 2026-11-15 — V5 C03-04.M03.02 preflight Application para incidentes [Javier :10]
- Implementa Application .NET10 read-only que compone reglas Domain M03.01, valida SHA y evidencia depurada, y siempre niega escritura, restauración o exposición de incidentes sin adaptador autenticado.
- Añade xUnit de rechazo por tenant A/B, JWT/membresía revocada, sucursal, idempotencia, versión, evidencia insegura y solicitudes de restauración; CI exact-SHA requerida para LISTO_DEV.
- No API, DI activa, MySQL/EF, migraciones, backup, worker, eventos ni QA/main/PROD; A5 independiente pendiente, rollback git revert.

## 2026-11-15 — V5 C14-02.M03.02 preflight Application de aceptación de seguridad [Javier :10]
- Implementa Application .NET10 fail-closed que compone la política Domain M03.01 con SHA coherente y referencia de evidencia saneada; jamás concede acceso, release ni escritura.
- Añade pruebas xUnit positivas/negativas de JWT/RBAC, tenant A/B, BOLA, host, adjuntos, jobs, filtro y restore; CI exact-SHA y revisión A5 ajena obligatorias antes de certificación.
- Sin DI activa, endpoints, EF/MySQL, migraciones, workers, secretos, runtime PROD o QA/main; revert normal preserva controles.

## 2026-11-15 — V5 C15-03.M03.02 servicio Application fail-closed de auditoría tenancy [Javier :30]
- Implementa .NET 10 Application service sobre M03.01, fuente de autoridad backend verificada y lector de propiedad persistida scoped por tenant, sin aceptar contexto de cliente como permiso.
- Añade pruebas xUnit positivas y negativas: SHA/evidencia, identidad y membresía, tenant A/B/NULL, consulta con bypass, sucursal, archivos, jobs/idempotencia y control plane.
- No añade endpoint ni DI activa, consultas EF/MySQL, workers, acceso a archivos ni acciones privilegiadas; sigue pendiente resolver TenantQueryBypassGuard con contexto nulo e integración real antes de declarar seguridad end-to-end.
- Scope Lock y CI causal exact-SHA requeridos antes de LISTO_DEV; A5 sólo por otra Task Javier; rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V5 C15-05.M03.04 adaptador aislado .NET10 auditoría operacional [Javier :30]
- Implementa adaptador Infrastructure read-only en memoria con copia defensiva de snapshot autorizado y composición Application/Domain sin mutar las fuentes.
- Añade pruebas xUnit de SHA, revisión fuente, owner, A5 independiente, IDs, evidencia incompleta, catálogo y rechazo de mutaciones de entrada.
- Nunca autentica GitHub/Sheets/CI ni permite persistencia, bypass tenant, escrituras institucionales, EF/MySQL, migraciones o certificación de las 2.340 filas.
- CI .NET, Scope Lock y V5 exact SHA antes de LISTO; A5 solo por Task Javier diferente; git revert, sin QA/main/PROD.

## 2026-11-15 — V5 C14-04.M03.02 Application preflight readiness DEV [Javier :30]
- Implementa servicio .NET 10 con frontera de evidencia backend y composición Domain readiness para SHA, tenant, CI, rollback y revisión, sin autorizar release.
- Añade xUnit positivos/negativos de SHA, autoridad, tenant A/B, entorno PROD, CI cancelled/stale, auto-A5 y aceptación no ejecutada.
- No DI ni HTTP, EF/MySQL, workers, flags, secrets, despliegues, QA/main/PROD; el resultado positivo exige verificación externa real.
- CI backend y Scope Lock/V5 exact-SHA obligatorios antes de LISTO, A5 de otra Task y rollback git revert.

## 2026-11-15 — V5 C05-02.M01.06 bloqueo, rollback y liberación documental de branding [Javier :20]
- Define catorce bloqueos fail-closed y ocho rutas de recuperación diferenciando revert documental de rollback de implementación futura; decisiones D01–D08 siguen pendientes.
- Distingue tema global existente de presets por tenant no implementados; registra oráculos documentales positivos/negativos sin inventar pruebas Angular, API, MySQL, browser ni seguridad runtime.
- Requiere A0–A4 con Scope Lock/V5 exact-SHA, owner exclusivo y readback PLAN/COLA/DAG/BITACORA; A5 independiente anterior a M02, sin QA/main/PROD.

## 2026-11-15 — V5 C02-02.M01.06 gates y rollback documental shared/dedicated [Javier :20]
- Formaliza dieciseis gates fail-closed G01–G16 y ocho rutas R01–R08 diferenciando git revert documental de restauracion futura de datos, sin ejecutar router, MySQL ni migraciones.
- Traza M01.01–05 y el A5 historico independiente de M01.01 documentado en BITACORA; preserva autorizacion JWT/RBAC/tenant A/B y DSN server-only sin validar operaciones runtime.
- Documenta P-DOC01–04/N-DOC01–04 futuros, Scope Lock/V5 y A0–A4 exact SHA; A5 independiente requerida antes de despachar M02, sin QA/main/PROD.

## 2026-11-15 — V5 C01-01.M04.01 superficie privada contexto empresarial verificado [Javier :20]
- Integra en /usuarios existente una superficie Angular read-only alimentada solo por TenantContextService.contextoVerificado, protegida por los guards y permisos ya existentes sin crear ruta o autoridad nueva.
- Agrega pruebas unitarias de contexto no confirmado, revocacion A y nueva seleccion B, y escape de HTML en rol; CSS ligero accesible sin cambios de API/EF/MySQL/secretos.
- CI frontend/tests, Scope Lock y V5 exact-SHA obligatorios para A0–A4; A5 independiente, rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V5 C01-01.M04.01 reparacion causal catalogo Angular P0-007.A [Javier :20]
- Corrige drift determinista del catalogo tras agregar un componente real: inventario aditivo de la nueva ruta fuente y recuento vigente 139 (131 features, 8 shared), preservando la foto historica original.
- Mantiene checks negativos exact-HEAD, deteccion de archivos no registrados y borrados, sin saltar verificaciones ni ampliar JWT/RBAC, API o SQL.
- Reejecutar CI relevante del nuevo SHA, Scope Lock y V5; reparacion de baseline relacionada con M04.01, sin QA/main/PROD.

## 2026-11-15 — V5 C01-01.M04.01 CI dirigida Vitest Angular [Javier :20]
- Agrega job GitHub-hosted exact SHA con Vitest de contexto A/B, interpolacion segura de rol, lint y build Angular, sin dependencias de servicios de produccion.
- Mantiene concurrente por SHA sin cancelar pruebas por nuevos commits y deja A4 bloqueada hasta success real, sin tratar queued/cancelled como PASS.
- Solo dev, sin nuevas Tasks, PR, ramas, secretos, cambios de seguridad o despliegues.

## 2026-11-15 — V5 C01-01.M04.04 regresión real formularios Usuarios y doble accion [Javier :20]
- Incorpora cinco pruebas unitarias Angular/Vitest para validar formulario inválido, permisos de rol/contraseña, bloqueo del doble envío, rechazo HTTP 403 y ruta con ID inválido.
- Añade GitHub Actions dirigido de test, lint y Angular build en Node 24/npm fijo, sin alterar runtime, endpoint, JWT/RBAC/tenant ni datos de MySQL.
- A0–A4 y Scope Lock/V5 requieren resultados exact-SHA completed/success y readback institucional; A5 independiente y rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V5 C01-01.M04.05 regresion Chromium de responsive y accesibilidad [Javier :20]
- Añade cuatro pruebas browser reales del login privado: 360px sin recorte de controles, teclado y boton ver contraseña, contraste WCAG 4.5:1 del encabezado y denegacion de /usuarios anonimo.
- CI Playwright Chromium en GitHub-hosted, Node 24/npm fijo, Angular local, lint y build de produccion, sin autenticacion a backend real ni cambios de UI, JWT, RBAC, tenant o MySQL.
- Requiere checks exact-SHA completed/success y readback A0–A4 antes de LISTO_DEV; A5 independiente, git revert normal y ninguna promocion QA/main/PROD.

## 2026-11-15 — V5 C01-01.M04.05 ajuste causal Playwright del runner [Javier :20]
- Corrige la expresion regular TypeScript de navegacion privada y elimina el selector --project ausente del config Playwright, sin modificar tests de seguridad ni desactivar verificaciones.
- La prueba Chromium real, lint, build y Scope Lock/V5 siguen requiriendo SUCCESS exact-SHA sobre commit corregido, con A5 posterior independiente y sin QA/main/PROD.

## 2026-11-15 — V5 C02-03.M02.02 contrato API DTO y errores de lifecycle tenant [Javier :20]
- Documenta las rutas existentes de empresas y plataforma y un endpoint FUTURO no implementado de transiciones con DTO versionado, CAS, autoridad global e idempotencia por operacion.
- Fija matriz de ocho transiciones, doce validaciones de errores y ocho pares positivos/negativos documentales, preservando JWT/RBAC/tenant A/B, revocacion, retencion y rechazo fail-closed.
- Sin endpoints ni codigo, EF/MySQL, workers, credenciales o activaciones reales; A0–A4 Scope Lock/V5 exact-SHA y A5 independiente para desbloquear etapas posteriores, revert normal.

## 2026-11-15 — V5 C02-03.M02.04 configuracion tipada flags lifecycle tenant [Javier :20]
- Define contrato documental de 18 opciones tipadas, defaults restrictivos, 12 precedencias fail-closed y ocho pares P/N para futura alta, suspension, reactivacion, archivo y baja logica.
- Mantiene autoridad global distinta de tenant A/B, JWT/RBAC, version CAS, idempotencia, retencion, CI exact-SHA y A5 independiente; no habilita ninguna feature flag real.
- Solo Markdown sin cambios backend, frontend, EF/MySQL ni webhooks: Scope Lock/V5 y A0–A4 documentales exact-SHA obligatorios antes de LISTO, rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V5 C05-02.M02.02 API DTO branding y presets versionados [Javier :20]
- Contrato documental real sobre rutas existentes empresa-configuracion, seis DTO FUTUROS preview/publicacion/rollback, catorce invariantes, diez errores y ocho pares positivos/negativos de cross-tenant y accesibilidad.
- Distingue tema y logo actuales de capacidades no implementadas de presets y publicacion revisionada; conserva validacion hex, permisos tenant A/B y bloqueo de URLs/activos ajenos.
- Solo Markdown, sin nuevos endpoints, estilos, storage ni flags runtime; Scope Lock/V5 exact-SHA y A5 independiente, rollback git revert, sin QA/main/PROD.

## 2026-11-15 — V5 C01-01.M04.06 densidad y carga visual progresiva de usuarios [Javier :30]
- Agrega tres indicadores visuales de carga de peso ligero al listado protegido de usuarios, ocultos a accesibilidad pero con estado textual anunciado; no generan datos ni permisos ficticios.
- CSS responsive con tokens del tema, contraste forzado y animacion eliminada con prefers-reduced-motion; conserva loading, vacio, error, resultados y solo lectura.
- Amplia Vitest dirigido con dos casos de aparicion y desaparicion de placeholders tras exito/fallo; Scope Lock, frontend y V5 de SHA causal obligatorios antes de LISTO.
- Sin paginacion real nueva, API, JWT/RBAC/tenant backend, EF/MySQL, migraciones, servicios, QA/main/PROD; A5 independiente y git revert normal.

## 2026-11-15 — V5 C15-01.M03.03 autorizacion y tenancy fail-closed para cobertura [Javier :30]
- Agrega servicio .NET10 de solo lectura que coteja principal backend, subject, tenant, membresia, RBAC coverage.review, SHA exacto y Scope Lock antes de invocar el preflight de cobertura previo.
- Incluye siete pruebas xUnit positivas/negativas para contexto no autenticado, tenant B, permisos revocados, SHA, solicitud incoherente y requisitos sin mapeo.
- Incluso con un corte internamente consistente exige adaptador externo autenticado y A5 ajena; nunca concede acceso, escritura, certificacion de 2340 tareas ni promocion de entornos.
- Sin nuevas API, JWT/EF/MySQL real, migraciones, jobs, secrets, QA/main/PROD; CI backend/seguridad/Scope Lock/V5 exact-SHA y git revert sin force.

## 2026-11-15 — V16 C02-01.M03.01 resolucion Domain segura de tenant [Javier :10]
- Implementado preflight ejecutable .NET 10 de identidad, tenant, membresia vigente, RBAC, host mapping unico/fresco desde backend y ownership A/B de recursos y sucursales; ninguna cabecera o ruta crea autoridad.
- Agregados doce metodos xUnit positivos, negativos y frontera: JWT ausente, host falso/forwarded no confiable, tenant B, rol revocado, membresia desactivada, tenant suspendido, proveedor sin autoridad, fallback cliente y errores de superficie.
- Riesgo V15 R2: no modifica middleware, JWT HTTP real, EF/MySQL ni API; el veredicto solo exige enforcement independiente en backend. CI backend/security/auth/Scope Lock/V5 exact-SHA obligatoria; A5 de Task Javier distinta y rollback git revert.

## 2026-11-15 — V16 C02-02.M03.01 Domain placement seguro shared-dedicated [Javier :10]
- Implementado preflight puro .NET 10 de asignacion shared/dedicated por tenant y revision CAS; JWT/membresia/RBAC servidor, binding opaco de catalogo y rechazo de destino tenant B.
- Anadidos once tests xUnit positivos/negativos: DSN de cliente, fallback cruzado, cutover y DDL destructivos, version caduca, rol revocado, idempotencia faltante y ausencia de backup/restore de prueba.
- Riesgo V15 R2; veredicto nunca autoriza conexion ni migracion; sin EF/MySQL, API, infraestructura real ni secretos. CI backend/seguridad/autorizacion/Scope Lock/V5 por SHA, A5 independiente y rollback git revert.

## 2026-11-15 — V16 C07-03.M01.06 bloqueo y rollback inventario y compras [Javier :10]
- Cierra contrato DOCUMENTAL de M01 con dieciseis BL01-BL16 para tenencia A/B, almacen/sucursal, idempotencia, transferencias, costos, stock, fallos parciales, Scope Lock, CI y A5.
- Define diez alternativas RB01-RB10 y doce condiciones SAL01-SAL12; relaciona NR01-12, R01-12, D01-08 y P01-14/N01-14 FUTUROS NO_EJECUTADOS, sin afirmar test SQL/HTTP ejecutado.
- R0, solo documento y CHANGELOG, sin API, JWT, DB, facturas, workers o UI; Scope Lock y V5 exact-SHA antes de LISTO, A5 otra Task Javier, rollback git revert sin QA/main/PROD.

## 2026-11-15 — V16 C08-04.M03.01 Domain ciclo modulos sectoriales [Javier :10]
- Implementado Domain .NET 10 para prevalidar ciclo Disabled/Enabled/Suspended/Retired de capacidades sectoriales, con backend JWT/RBAC, membresia, allowlist tipada, pertenencia A/B, plan/rubro/tenant, denylist, idempotencia y revision CAS.
- Doce metodos xUnit positivos, negativos y fronteras sobre rol revocado, tenant cruzado, capabilities desconocidas, overrides del cliente, estado ilegal y version stale. Todo veredicto NO autoriza runtime, DB, API ni activacion efectiva.
- V15 riesgo R2: CI backend Release, autorizacion, seguridad, Scope Lock y V5 exact-SHA antes de LISTO; A5 Task Javier distinta, sin MySQL/EF, Angular, API, secrets, webhook, produccion ni rollout.

## 2026-11-15 — V16 CI_REPAIR C08-04.M03.01 evidencia nombre capability tenant neutral [Javier :10]
- Reparacion Scope Lock fail-closed del commit b7989a8a: fixture de tests reemplaza identificador de proyecto retirado por clave sector.stock y variante case sector.Stock sin modificar semantica, dominios, autorizacion ni revisiones.
- Causa exacta: GitHub Actions verify-solqaryn-project-scope y Seguridad multiempresa reportaron retired identity token en el archivo de tests. No se desactivan controles; se exige nueva CI exact-SHA y A5 independiente.

## 2026-11-15 — V16 C05-01.M02.04 configuracion tipada design system multiempresa [Javier :10]
- Contrato Markdown de catorce opciones OPT01-OPT14 para tokens semanticos, preset, revision, branding, accesibilidad, sesion/epoch y rollback, siempre con fuente backend/ownership de tenant verificado.
- Dieciocho reglas V01-V18 y diez pares P/N FUTUROS_NO_EJECUTADOS sobre JWT/RBAC, A/B, CSS/URL injection, cache, contraste, foco, idempotencia y revocacion; no se activaron flags ni implementaron endpoints.
- Riesgo documental R0, solo docs/v5 y CHANGELOG en dev, Scope Lock y V5 exact-SHA, A5 de otra Task Javier y rollback git revert, sin Angular/EF/MySQL/QA/main/PROD.

## 2026-11-15 — V16 C05-01.M02.05 eventos inertes design system [Javier :10]
- Contratos de doce eventos futuros EV01-EV12 para tokens, presets, asignacion, intent, rollback y epoch de sesion, con envelope tipado sin secretos, JWT, PII ni datos tenant ajeno.
- Define catorce reglas ID01-ID14 de version CAS, digest/idempotencia, dedupe y no-regresion; diez pares F01-F10 de pruebas FUTURAS_NO_EJECUTADAS sobre replay, revocacion, A/B, rollback, fallo de proveedor y sesion.
- Solo documento Markdown y CHANGELOG; ningun evento, outbox, API, broker, worker, webhook, MySQL, CSS, infraestructura o permiso activado; Scope Lock/V5 exact SHA, A5 otro Javier, rollback git revert.

## 2026-11-16 — V17 C05-04.M04.05 regresión accesible de ligereza [Javier :00]
- Añade suite Playwright/Chromium de cinco casos causales para guard sin sesión, denegación 403, teclado Tab y Enter real sin foco programático, etiquetas, foco visible, 320/375/768/1280 y contraste WCAG AA 4,5:1 calculado.
- Agrega workflow aislado por SHA, cancel-in-progress=false, Node/Angular build y Chromium loopback con fixtures sintéticos; no acredita JWT, MySQL, tenancy backend ni Web Vitals reales.
- A0 R1 y rollback documentados; A1-A4 sujetos a validación GitHub Actions y Scope Lock exact-SHA; A5 otra Task Javier. V17 lock 2000000852 y solo dev.

## 2026-11-16 — V17 C07-04.M04.02 pruebas reales estados y permisos pedidos [Javier :00]
- Añade cinco pruebas Playwright/Chromium: anónimo y permiso Ventas:Ver denegado, carga a vacío, error 503 con retry a éxito, lectura autorizada sin acciones ni HTTP mutador a 320/768/1280.
- Nuevo workflow dirigido por SHA exacto y cancel-in-progress=false con Angular lint/build y Chromium en loopback, fixtures sintéticos, sin certificar JWT/tenant backend, MySQL, caja, pagos ni fiscalidad.
- Alcance R2, A0 estática y rollback git revert documentados; A1-A4 sujetos a CI exact-SHA y readback; A5 otra Task Javier, V17 lock 2000001137; no QA/main/PROD.

## 2026-11-16 — V17 C02-04.M04.04 pruebas de interacción privada de control plane [Javier :00]
- Añade cinco pruebas Playwright/Chromium con guard anónimo, 403 sin fuga, teclado real Tab/Enter, navegación, error 503 con recuperación y botón single-flight ante segunda pulsación, sin formularios ni escrituras.
- Nuevo CI dirigido por SHA, cancel-in-progress=false, npm lint/build y Chromium loopback; fixtures sintéticos no certifican JWT, MFA, tenancy A/B, MySQL ni grants de plataforma backend.
- Alcance R2 test-only con A0 y rollback git revert; A1-A4 sujetas a ejecución exact-SHA, A5 solo Task Javier distinta, V17 lock 2000000419. Sin QA/main/PROD.

## 2026-11-16 — C07-04.M04.05 · pruebas accesibilidad pedidos (DEV05)
- Añade pruebas reales Playwright 320/768/1280, Tab y foco, contraste elevado y denegaciones de acceso del listado privado de pedidos.
- El workflow dirigido ejecuta Vitest, lint/build Angular y Chromium por SHA sin cancelar SHA anteriores; no certifica backend, pagos, fiscalidad ni MySQL real.
- Riesgo R2, A0–A4 pendientes de resultados CI externos, A5 exclusivamente independiente; sin QA/main/PROD.

## 2026-11-16 — V17 C02-04.M04.04 reparación de control single-flight [Javier :00]
- Corrige botón de revalidación global que desaparecía durante la solicitud HTTP pendiente: permanece visible y deshabilitado sin modificar grants, MFA, JWT, tenant ni API.
- Mantiene cinco asserts Chromium; run original 43917701525 4 PASS/1 FAIL, la reparación exige nuevo resultado real y Scope Lock/V5 exact-SHA. A5 independiente; rollback git revert; solo dev.

## 2026-11-16 — V17 C07-04.M04.02 reparacion responsive del listado de pedidos [Javier :00]
- Repara contención del host y box sizing del listado móvil sin alterar lógica de pedido ni tabla desplazable; CI original 43916820506 4 PASS/1 FAIL por overflow horizontal 260 px a 320.
- Mantiene el umbral estricto de <=2 px, añade localización de responsables para fallos futuros y exige nueva CI Chromium/Scope Lock/V5 exact-SHA antes de LISTO; A5 otro Javier; solo dev.

## 2026-11-16 — V17 C15-04.M03.05 worker acotado de auditoría UX [Javier :00]
- Implementa procesador sincrónico read-only .NET 10 para hasta 32 observaciones UX aisladas, delegando en autoridad Application existente y rechazando mezclas tenant A/B/SHA, claves duplicadas y cancelación sin resultados parciales.
- Nueve xUnit de aceptación estructural y rechazos; nunca escribe base de datos, proveedor, JWT, scheduler ni certifica Playwright, A5 o producción. CI real .NET/seguridad/Scope Lock/V5 y readback requeridos; solo dev.

## 2026-11-16 — V18 C15-04.M03.05 correccion de limite adversarial del worker UX [Javier :00]
- Se rechaza una coleccion con Count falso y mas de 32 elementos mediante snapshot de enumeracion realmente acotado; excepciones y cancelacion devuelven denegacion sin resultados parciales.
- Cuatro regresiones xUnit adversariales, documentacion de riesgo R2, controles de tenant/SHA intactos. CI exact-SHA y nuevo A5 independiente obligatorios; el A5_FAIL P2 historico permanece. Solo dev.

## 2026-11-16 — C08-04.M04.01: superficie sectorial privada de solo lectura (DEV03)
- Alcance: Angular 22.2.1, ruta privada /extensiones-sectoriales con authGuard, componente standalone/Signals; cuatro ámbitos informativos y estado sin módulos conectados. Ninguna acción, concesión RBAC ni API, DB o migración nueva.
- Pruebas causales: Vitest route/surface y Playwright Chromium anónimo/tenant sintético 403/permitido a 320/1280; CI exact-SHA y A5 independiente requeridos, no presupuestos como PASS. Autor TASK_ID 6adc2e4bd8a48191ac7d8641ed95a1a3, scope dev STRICT; rollback git revert normal.

## 2026-11-16 — C08-04.M04.01: corrección selector Playwright de estado accesible (DEV03)
- Regresión GitHub-hosted: run 43949578542, 2 Chromium PASS y tercero FAIL por selector ambiguo getByRole(status) ante un live-region global adicional. Corrección mínima: seleccionar el estado accesible por nombre exacto «Estado de las capacidades»; no se modifica la superficie ni se atenúa el assert. Exigir nuevo SHA y 3/3 Chromium, Vitest y Scope Lock antes de A0–A4; A5 de otra Task.

## 2026-11-16 — C13-04.M03.05: reparación P2 de coherencia de revision de snapshots (DEV03)
- A5 independiente DEV05 identificó discrepancia entre Outcome.Revision y Snapshot.Revision que podía contar como revisada una lectura incoherente. Se exige igualdad exacta, con prueba xUnit negativa en ambas direcciones, rechazo opaco y contador cero. No se altera API, JWT/RBAC, MySQL ni workers alojados. Nueva CI y A5 distinta requeridas; A5_FAIL histórico conservado.

## 2026-11-16 — V18 C08-04.M04.02 estados sectoriales seguros [Javier :30]
- La superficie privada Angular de extensiones añade cinco estados de presentación (cargando, vacío, error, éxito informativo, permiso no confirmado); las ramas restrictivas ocultan los ejemplos y preservan la ruta con authGuard existente, sin nuevos grants, llamadas de módulos ni mutaciones.
- Añade Vitest de positivo/negativo/borde/revocación, workflow GitHub-hosted para Angular lint/build y Chromium sintético (anon/403/320/1280); pruebas escritas son PENDIENTE hasta SUCCESS exact-SHA y aceptación integral aplicable.
- R1 visual, sin JWT/RBAC/backend/MySQL reales certificados, sin dependencias nuevas ni producción. A5 independiente de otra Task Javier, candado V17 2000001281, rollback por revert ordinario.

## 2026-11-16 — C08-04.M04.01: reparar gate P0-007.A de catálogo Angular (DEV03)
- El check GitHub exact-SHA P0-007.A falló por componente sectorial no inventariado; se añade su ruta real a `docs/P0-007-A-INVENTARIO-COMPONENTES-DEV.md`, y se actualiza el conteo exacto del validador de 153 a 154 (146 features/8 shared) sin permitir ausencias, duplicados ni rutas nuevas sin registrar. Exigir CI nueva P0, Scope Lock, V5 y Angular/Chromium; no sustituye A5 independiente ni concede autoridad.

## 2026-11-16 — V18 C08-04.M04.03 branding sectorial por allowlist local [Javier :30]
- Añade dos presets visuales tipados (institucional y contraste alto) a la vista privada de extensiones: CSS y Signal locales, normalizador fail-closed ante entrada desconocida o getters hostiles, sin concesiones JWT/RBAC ni requests a proveedores.
- Pruebas Vitest de valores permitidos, negativos, excepción getter y ocultación bajo sin_permiso/error, más workflow Angular build/lint y Playwright Chromium anónimo/403/responsive; SUCCESS exact-SHA exigido antes de LISTO, A5 de otra Task Javier.
- R1 exclusivamente UI, ninguna autorización real o modificación MySQL/tenant/backend, rollback git revert; lock V17 2000001282 y Scope Lock STRICT.

## 2026-11-16 — V18 C08-04.M04.04 navegación, mensajes y no doble acción [Javier :30]
- Añade tres E2E Chromium causales: tenant sintético 403 sin exposición, Tab real desde el foco inicial y Enter al enlace de retorno, y pantalla de módulos sin formularios/controles de activación ni mutaciones HTTP. No hay doble-submit porque la superficie es de solo lectura; no se inventa una API.
- Workflow específico con Vitest de los cinco estados, Angular typecheck/lint/build y navegador Playwright real; requiere CI completed/success exact-SHA para A0-A4, A5 de revisor Javier distinto. Pruebas sintéticas no certifican JWT/RBAC/tenant A/B reales.
- R1 pruebas Angular, sin modificar runtime/backend/MySQL/seguridad/producción. Scope Lock STRICT, candado V17 2000001283 y rollback git revert.

## 2026-11-16 — V18 C08-04.M04.04 reparación de destino protegido post-Enter [Javier :30]
- QA_DEFECT_FOUND P2 de prueba: Chromium GitHub-hosted 43952391693 obtuvo 2/3 PASS; la ruta /dashboard es seguida legítimamente de /perfil por el guard bajo sesión sintética Lectura, por lo que esperar /dashboard final fue una aserción incorrecta.
- Se mantiene Tab desde foco inicial, Enter, href /dashboard, 403 y ausencia de mutaciones; solo se espera el destino final protegido /perfil. Requerir Chromium y Scope Lock/V5 del nuevo SHA, sin desactivar validadores, con A5 independiente.
- Sin cambios de código de producto, JWT/RBAC/tenant/MySQL/QA/PROD; rollback git revert y bloqueo VALIDANDO hasta A0-A4 reales.

## 2026-11-16 - V18 C13-04.M04.03 configuracion visual permitida [Javier :30]
- Ruta privada de plataforma con marca y densidad locales tipadas, fallback ante getters hostiles y sesion revocada; no hay concesiones ni CSS/HTML remoto.
- Regression Angular TestBed y Playwright Chromium real en CI SHA-aislada, A0-A5 y Scope Lock exigidos, solo dev; ningun PASS atribuido antes de GitHub Actions.

## 2026-11-16 - V18 CI repair C13-04.M04.03 TS2352 and revoked Proxy [Javier :30]
- Corrige tipado adversarial en prueba sin alterar contrato de PlataformaSesion; mueve Array.isArray al manejo fail-closed para proxies revocados, sin exponer controles ni saltar permisos.
- Regresión original GitHub Actions falla en compilación TypeScript, reparación exige nueva CI Angular/Chromium/Scope Lock/V5 exact SHA; ninguna prueba saltada es PASS.

## 2026-11-16 - V18 CI repair C13-04.M04.03 guard ERP allowlist estricta [Javier :30]
- Incidente causal: audit Priority4 marcó UNGUARDED una ruta que sí usa plataformaGuard, por no estar en su conjunto cerrado. Se incorpora solo el nombre de ruta privada a la lista exacta y permanecen todas las condiciones del guard JWT/servidor, sin debilitar comprobaciones.
- Se exige re-ejecutar ERP quality y CI dirigida, V5 y Scope Lock en SHA nuevo, sin convertir CANCELLED/SKIPPED/QUEUED en PASS ni autorizar QA/PROD.

## 2026-11-16 - V18 CI repair C13-04.M04.03 inventario exacto Angular [Javier :30]
- P0-007.A falló en SHA dc26b1c0 porque el archivo real c13-04-presentacion-v5.component.ts faltaba en el catálogo; se añade solamente ese path, actualizando conteos estrictos a 155/147/8 sin borrar históricos.
- Conservados comparadores positivos y negativos, Scope Lock/V5, ausencia de grants y CI pendiente; ninguna prueba fallida, cancelada u omitida se acredita PASS.

## 2026-11-16 - V18 C08-04.M06.01 auditoria causal tests vs mocks [Javier :30]
- Matriz de seis suites Vitest/xUnit/Chromium y sus hashes/SHAs causales distingue clases reales y navegador real de actores/proveedores y tenant-context simulados; no se certifican JWT/RBAC/MySQL ni módulos activos. Ver docs/v5/c08-04-m06-01-auditoria-cobertura-real-vs-simulada.md.
- Cambio documental exclusivo, sin runtime ni QA/PROD. Scope Lock, V5, CI del SHA publicado y A5 independiente siguen siendo obligatorios; SKIPPED nunca PASS.

## 2026-11-16 - V18 C08-04.M06.04 ensayo rollback aislado de entrega test-only [Javier :30]
- Script y workflow GitHub-hosted ensayan git revert --no-commit de M05.05 en worktree efímero; prueban árbol idéntico al padre, whitelist de 4 paths y rechazo de SHA no autorizada. Evidencia docs/v5/c08-04-m06-04-rollback-aislado.md.
- No realizan restore MySQL ni desactivan módulos reales; el resultado está sujeto a GitHub Actions exact-SHA y A5 independiente. Sin mutaciones en QA/main/PROD o tenants.

## 2026-11-16 - V19 C08-04.M06.06 cierre documental de dependencias verificables [Javier :50]
- Agrega matriz de seis entregas M05 y cinco M06 con SHA reales, estados LISTO y A5 independientes leídos de PLAN; distingue pruebas Chromium/xUnit de datos, módulos y backend sintéticos.
- Revalida el DAG C08-04.M06.06 como derivado de PLAN/COLA, sin forzar gates; no certifica activación sectorial, rollback MySQL ni QA/PROD. Requiere Scope Lock/V5 y A5 de otro Javier para este propio cierre.

## 2026-11-17 - V19 C09-04.M04.02 estados privados de mensajeria [Javier :20]
- Componente standalone Angular OnPush reutilizable con estados cargando, vacio, error y exito declarativo; deniega acceso por falta de sesion Plataforma/Administrador y revocacion, sin acciones de proveedor ni datos tenant.
- Ocho pruebas TestBed adversariales y workflow GitHub-hosted dirigidos, con tipado/lint/build, Scope Lock y procedencia V5 exact-SHA exigidos; no se atribuye PASS hasta CI real. La ruta privada sigue siendo responsabilidad separada de C09-04.M04.01; no certificar JWT HTTP, MySQL, envios ni QA/PROD.

## 2026-11-17 - V19 reparacion CI P0-007.A C09-04.M04.02 [Javier :20]
- La CI de catálogo exact-SHA 83e8323a detectó el componente real no inventariado; se añade sólo ese archivo y el conteo estricto actual, sin desactivar negativos ni controles.
- Requiere revalidación exact-SHA de P0-007.A, TestBed Angular, V5 y Scope Lock. Sin certificación de proveedores, backend JWT, MySQL, QA/main/PROD.

## 2026-11-17 - V19 C13-04.M05.04 lectura ERP A/B en CI efimera [Javier :00]
- Nueva prueba Playwright JWT y MySQL aislado: grants reales, rutas positivas Productos/Clientes/Proveedores, parametro dinamico sin inventar registros y rechazo de empresa B.
- PrivateReadPilotEnabled se habilita solo en GitHub-hosted CI; no se alteran permisos ni flags de producto. Requiere Scope Lock/V5 y Chromium SUCCESS exact SHA, P1 global pendiente y A5 de otro Javier.

## 2026-11-17 - V19 reparacion A5_FAIL P2 C14-01.M03.03 catalogo ordinal [Javier :20]
- Corrige exclusivamente la comprobacion Domain de catálogos confiables: compara claves por StringComparison.Ordinal aun cuando un IReadOnlySet utilice StringComparer.OrdinalIgnoreCase; evita aceptar variantes no canonicas de caso/accion.
- Agrega regresion adversarial xUnit con caso valido y dos variantes rechazadas; conserva el A5_FAIL historico, requiere CI exact-SHA Scope Lock/V5/backend/seguridad y nueva A5 independiente. Sin endpoints, migraciones, JWT real ni QA/main/PROD.

## 2026-11-17 - V19 C14-01.M03.04 adaptador aislado de autoridad [Javier :30]
- Crea un puente Application solo lectura desde una fuente de snapshot confiable hacia el preflight V5 existente, sin DI real, HTTP, EF/MySQL, migraciones ni permisos productivos; fail-closed ante revocacion, ausencia de autoridad y fallos.
- Regresiones xUnit comprueban claves y acciones ordinales, tenant A/B, cancelacion, catalogo inmutable, errores y ausencia de concesion de acceso. Requiere CI Release y seguridad, Scope Lock y V5 exact-SHA y A5 independiente de otra Task Javier.

## 2026-11-17 - V19 C15-04.M05.04 pruebas UX autenticadas aisladas [Javier :10]
- Se incorpora un workflow de GitHub-hosted con API .NET 10, MySQL 8.4 efimero, JWT firmado y Playwright/Chromium, sin tocar runtime productivo ni datos reales.
- Cobertura nueva: perfil de plataforma, denegacion 403 empresarial, tenant A/B en ruta dinamica y rechazo de firma JWT alterada, con secretos sinteticos generados dentro del runner.
- Solo se reconocera PASS tras resultados exact-SHA y logs; auditoria UX R2 global, WCAG y WebVitals siguen incompletas, sin transicion LISTO ni A5 propio.

## 2026-11-17 - V19 C15-04.M05.04 contraste y performance localhost en UX privada [Javier :10]
- Se extiende suite real JWT/MySQL a quinto caso Chromium: Tab/outline/contraste >=4.5 en 320/375/768/1280 y muestras TTFB/FCP autenticadas, sin datos productivos.
- LCP/CLS no medidos permanecen NO_MEDIDO; WCAG/WebVitals global no se certifican. Exigir logs y Scope Lock por nuevo SHA; owner DEV02 conserva EN_PROGRESO, A5 externa.

## 2026-11-17 - V19 C14-01.M03.05 evidence provider [Javier :40]
- Integra proveedor de prueba de aceptación mediante GET real GitHub Checks API, SHA/repositorio exactos, token efímero y estados incompletos fail-closed; no otorga permisos de API, tenant ni release.
- Incorpora ocho casos Node (uno HTTPS real, siete contractuales/adversariales), CI por SHA con concurrency cancel-in-progress=false y contrato de rollback; conserva A5 independiente y limita el PASS a evidencias concretas.


## 2026-11-17 - V19 C13-04.M05.04 permisos ERP solo lectura y denegacion de escritura [Javier :00]
- Añade regresion E2E JWT/MySQL 8.4 efimero: Productos/Clientes/Proveedores con capacidad Ver, rechazos Crear/Editar por URL directa y retorno a lectura autorizada, sin mocks HTTP.
- A0-A4 requieren CI exact-SHA y readback; P1 ERP global permanece abierto, A5 independiente. Solo test/documentacion; sin cambios productivos, QA/main/PROD.


## 2026-11-17 - V19 A0.26 LCP/CLS reales C15-04.M05.04 [Javier :10]
- Agrega sexto escenario JWT/MySQL/Chromium real con PerformanceObserver previo y LCP/CLS en cuatro anchos; falla si LCP ausente o CLS excede 0.1.
- Requiere PASS exact-SHA; no interpreta muestras localhost como Web Vitals productivos ni cierra R2 global, A5 independiente.
