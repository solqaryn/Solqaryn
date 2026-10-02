## 2026-10-02 — Limpieza integral de identidad legacy en DEV

- Se ejecutó un barrido repository-wide sobre todos los archivos versionados de texto y se normalizaron **378 archivos** que aún contenían terminología o identificadores retirados.
- Se retiraron paths legacy, documentación obsoleta y nomenclatura inglesa heredada del dominio de inventario; el módulo funcional vigente permanece en español bajo `/inventario/...`.
- `scripts/verify-project-scope.mjs` ahora aplica un gate fail-closed sobre **todo el árbol versionado**, no sólo sobre código operativo: cualquier reintroducción de identidades retiradas o de la terminología inglesa prohibida falla CI.
- El árbol vigente no contiene paths de identidades retiradas y la verificación repository-wide del sweep terminó en `PASS`.
- Alcance: exclusivamente `dev`. No se modificaron QA, `main`, PROD, bases de datos, secretos ni infraestructura productiva.

## 2026-10-02 — Reconciliación canónica de pendientes vigentes

- `docs/DETALLES_PENDIENTES.md` queda reconciliado como fuente canónica de pendientes deliberadamente diferidos/activos.
- Se consolidan cinco puntos vigentes: Render paid/always-on; certificación SMTP real end-to-end; cutover `solqaryn.com` -> PROD; diez automatizaciones VAEP pausadas; y continuidad del Plan Maestro/ERP como roadmap normal de producto.
- DEV/QA/PROD permanecen certificados bajo Render Free con contrato 28/28, aislamiento DB y binding Vercel -> API; Free se acepta temporalmente y no se declara always-on.
- No se modifica main/PROD, secretos, datos, DNS, planes ni automatizaciones en este changeset documental.
- Tras contrastar las fuentes canónicas actuales, no se identifica otro pendiente deliberadamente aplazado fuera de esos cinco puntos; el trabajo funcional futuro queda englobado por el Plan Maestro vigente.

## 2026-10-02 — Smoke QA alineado al contrato storefront vigente

- La certificación viva QA aún esperaba `data.nombreComercial`, forma previa al bootstrap tenant-aware actual.
- El contrato vivo de `GET /tienda/bootstrap` expone la identidad bajo `data.identidad.nombreComercial`; el gate se actualiza a esa ruta sin cambiar API ni datos.
- El cambio evita un falso negativo después de promover la build QA con attestation `x-solqaryn-environment`.
- Sin cambios en main/PROD, secretos, grants ni datos.

## 2026-10-02 — Certificación QA preserva historial EF válido

- El recovery QA confirmó backup cifrado, 137 tablas, 107 filas de historial EF y cero migraciones pendientes; no fue necesario aplicar migraciones nuevas.
- El gate anterior exigía igualdad byte-a-byte entre archivos de migración actuales e historial persistido, lo que rechazaba 18 IDs históricos legítimos ya ejecutados cuyos archivos fueron retirados del árbol vigente.
- El gate ahora falla si falta cualquier migración versionada actual y también rechaza IDs históricos con timestamp posterior al conjunto vigente, pero preserva filas históricas anteriores sin borrar ni reescribir `__EFMigrationsHistory`.
- No se modifican datos de negocio, grants, secretos ni PROD; el cambio se promueve primero DEV → QA y se recertifica exact-head.

## 2026-10-02 — Recovery de promoción controlada DEV → QA

- El PR #3512 fue integrado mediante merge normal para preservar los fixes propios de QA y absorber el baseline DEV vigente; tras el merge, QA quedó `behind_by=0` respecto de DEV.
- La certificación post-merge detectó dos defectos de infraestructura de prueba, no de datos ni negocio: `mysqldump` intentaba `FLUSH TABLES` bajo el usuario mínimo QA y el smoke esperaba una attestation HTTP que el proxy sólo enviaba hacia el upstream.
- El backup QA fija `--set-gtid-purged=OFF` para evitar exigir privilegios globales `RELOAD/FLUSH_TABLES`; se mantiene el principio de mínimo privilegio y no se amplían grants de `solqaryn_qa_user`.
- El proxy Vercel devuelve ahora `x-solqaryn-environment` con el entorno resuelto por `VERCEL_PROJECT_ID`, y el validador de routing exige esa attestation canónica.
- Este recovery se publica primero en `dev`; `main`/PROD aún no se modifica hasta que QA complete nuevamente todos los gates.

## 2026-10-01 — Validación post-limpieza SMTP DEV y corrección de fallback de logo

- Readback Render DEV posterior a la limpieza manual: el nuevo deployment ya no detecta las siete claves SMTP legacy retiradas; el único extra restante es `AppSettings__LogoPublicUrl` (29 observadas vs 28 canónicas), por lo que el guard abortó el deployment nuevo sin sustituir la instancia sana.
- Se confirmó en código que `Smtp__PasswordSmtp`, `Smtp__OAuth2TokenEndpoint`, `Smtp__OAuth2Scope`, `Smtp__UsarSsl`, `Smtp__RequiereAutenticacion`, `Smtp__CorreoRemitente` y `Smtp__CorreoRespuesta` no son requeridas por Render: OAuth2 usa refresh token/client id, endpoint/scope son constantes seguras, TLS/autenticación default a true y remitente/Reply-To derivan de `Smtp__UsuarioSmtp`.
- Añadida prueba dirigida que certifica `SmtpEmailService` con únicamente las nueve claves SMTP canónicas desplegadas.
- La auditoría de facturas reveló que `AppSettings__LogoPublicUrl` aún era leído como fallback y que, sin logo, QuestPDF mostraba un monograma fijo de cliente. Se eliminó ese fallback global: la autoridad pasa a `EmpresaConfiguracion.LogoUrl` y la ausencia de logo usa monograma derivado de `EmpresaNombre`.
- El bootstrap vivo DEV y PROD devuelve actualmente `logoUrl=null`; por tanto, el fallback tenant-derived es necesario para evitar branding fijo de un cliente.
- Durante la misma verificación viva, QA reveló una deuda independiente de esquema: `solqaryn_qa.EmpresaConfiguraciones` no existe y `/api/tienda/bootstrap` devuelve 500. Queda abierto recuperar el esquema QA antes de certificar los tres entornos.
- No se tocaron datos PROD en este changeset.

## 2026-10-01 — Hardening y certificación estructural de infraestructura DEV/QA/PROD

- Añadido `RenderEnvironmentContractGuard`: el backend Render exige exactamente las 28 claves canónicas, valores requeridos no vacíos y constantes públicas coherentes; registra sólo conteo/fingerprints no sensibles.
- `EnvironmentDatabaseGuard` ahora cubre Development/Staging/Production y exige el endpoint Aiven corporativo `solqaryn-mysql-solqaryn.h.aivencloud.com:14402` con TLS requerido, además de base/usuario exclusivos.
- Añadido workflow reutilizable `Environment infrastructure parity` para DEV/QA/PROD: 4 variables DB + 3 secretos requeridos, binding real MySQL, cross-access DENY, mínimo privilegio, token Aiven y passphrase de backup.
- DEV: run `36954462879` certificó GitHub/Aiven; el deploy Render `dep-davh89id0e5s73800o9g` falló cerrado al detectar ocho variables legacy extra y conservó la instancia sana anterior.
- QA: run `36954576441` certificó GitHub/Aiven; Render `dep-davh760u01pc73eomtg0` quedó LIVE con 28 claves y readiness conectado; CI QA `36954576416` terminó SUCCESS.
- QA Vercel sigue bloqueado únicamente en control-plane: el deployment nuevo está READY pero el alias canónico permanece en un deployment anterior porque el proyecto aún no promueve `qa` como Production Branch.
- La evidencia viva y los bloqueos externos se registran en `docs/evidencias/INFRA_PARITY_DEV_QA_PROD_2026-10-01.md`.
- No se exponen valores secretos ni se ejecutan migraciones/datos productivos en este changeset.

## 2026-10-01 — Cloudinary QA certificado y probe temporal retirado

- Render QA desplegó `4d01204d0303875437edb97a7500ef3312147e77` como `dep-davgkfpsrm7s73bu69r0` y quedó `LIVE`.
- Certificación runtime: `CLOUDINARY_QA_CERT=PASS` con cloud `riyrzmob`, prefijo `solqaryn_qa`, autenticación API real, upload bajo namespace QA y cleanup inmediato del activo temporal.
- Evidencia persistida en `docs/evidencias/CLOUDINARY_QA_CERTIFICACION_2026-10-01.md`.
- El probe temporal fue retirado de `dev` inmediatamente después de capturar la evidencia; no queda hook diagnóstico permanente.
- No se modificaron `main`, PROD, datos productivos, DNS, certificados ni activos DEV/PROD.

## 2026-10-01 — Cierre operativo GitHub + Aiven QA

- GitHub Environment `QA` quedó restringido a la rama `qa`, con protection rule activa y sin bypass administrativo.
- Variables QA canónicas: host, port, database y user de `solqaryn_qa`/`solqaryn_qa_user`.
- Secretos QA provisionados: `SOLQARYN_QA_DB_PASSWORD`, `SOLQARYN_AIVEN_TOKEN` y `SOLQARYN_QA_BACKUP_PASSPHRASE`.
- El token Aiven QA queda reservado exclusivamente a control-plane/GitHub Actions; no se expone al backend Render.
- Aiven quedó certificado con mínimo privilegio por usuario: DEV sólo `solqaryn_dev.*`, QA sólo `solqaryn_qa.*`, PROD sólo `solqaryn_prod.*`.
- Sin cambios en `main`, datos PROD, DNS, certificados ni servicios pagos.

## 2026-10-01 — Cierre de aislamiento Aiven QA y contrato GitHub Environments

- Certificado con MySQL real que `solqaryn_dev_user` conserva únicamente `USAGE ON *.*` + `ALL PRIVILEGES ON solqaryn_dev.*`; run DEV `36932283653` en verde y sin acceso a QA/PROD.
- Auditoría autoritativa con `avnadmin` confirmó que `solqaryn_prod_user` conserva únicamente `USAGE ON *.*` + `ALL PRIVILEGES ON solqaryn_prod.*`; no tiene grants sobre DEV/QA.
- `solqaryn_qa_user` fue recreado preservando su password vigente, con `mysql_grants=[]`, y recibió después únicamente `ALL PRIVILEGES ON solqaryn_qa.*`; run `36932132076` = SUCCESS y `AIVEN_QA_LEAST_PRIVILEGE=PASS`.
- QA ya no conserva `WITH GRANT OPTION`, `ROLE_ADMIN`, `REPLICATION_APPLIER` ni grants cruzados hacia `solqaryn_dev`/`solqaryn_prod`.
- Los workflows permanentes de aislamiento DEV/QA ahora fallan si reaparecen grants administrativos o referencias a otra base.
- Añadido `docs/GITHUB_ENVIRONMENT_CONTRACT.md`: QA usa exactamente cuatro variables DB y un único secreto DB; no se duplican Aiven token, backup passphrase ni URLs sin consumidor real.
- La regla de deployment esperada para QA queda restringida exclusivamente a la rama `qa`.
- Sin cambios en `main`, datos PROD, DNS, certificados ni servicios pagos.

## 2026-10-01 — Paridad estructural del GitHub Environment QA

- `qa-live-certification.yml` dejó de requerir variables redundantes de URL; los endpoints canónicos QA quedan fijados en el workflow.
- El Environment `QA` vuelve al contrato estructural de cuatro variables DB: host, port, name y user, más el password como secret.
- La regla de protección esperada para QA queda ligada exclusivamente a la rama `qa`; no se autoriza cruce DEV/QA/PROD.
- Sin cambios en `main`, PROD, datos productivos, DNS, certificados ni servicios pagos.

## 2026-10-01 — Fundación QA persistente y aislamiento DEV/QA/PROD

- Incorporado QA persistente al contrato técnico: rama `qa`, Vercel `solqaryn-qa`, Render `solqaryn-api-qa`, base/usuario esperados `solqaryn_qa`/`solqaryn_qa_user` y prefijo Cloudinary `solqaryn_qa`.
- Añadido binding Vercel por `VERCEL_PROJECT_ID` para QA, guard fail-closed de Render `Staging`, CI/certificación viva QA y guard de promoción `dev -> qa -> main`.
- Corregida la ruta Dockerfile declarativa de Render QA a `./backend/Dockerfile` para mantener paridad con DEV/PROD.
- Reconciliados `PROJECT_CONTEXT.md`, `docs/ENTORNOS_DEV_PROD.md` y `docs/RENDER_ENVIRONMENT_CONTRACT.md` con la topología de tres entornos.
- En Render QA se aplicaron únicamente variables no sensibles del entorno; el servicio permanece fail-closed hasta provisionar/conectar sus secretos y recursos QA propios.
- Sin cambios en `main`, configuración PROD, datos productivos, DNS, certificados ni servicios pagos.

## 2026-09-30 — Fase 4: reconciliación final de documentación de entornos

- Corregidas referencias stale en `docs/ENTORNOS_DEV_PROD.md`: Vercel PROD ya existe, la migración histórica PROD ya está cerrada y el custom domain continúa aplazado.
- Alineado `docs/RENDER_ENVIRONMENT_CONTRACT.md` con la decisión vigente de SMTP aplazado/no bloqueante sobre Render Free; no se introduce compra ni requisito artificial de keep-alive.
- Cambio documental únicamente; sin runtime, `main`, PROD, datos, secretos, DNS ni Plan Maestro.

## 2026-09-30 — Fase 4: cierre técnico DEV y runbook de promoción futura

- Consolidada evidencia exact-head del runtime DEV `7452c43c489f467727ab2569f34d43074ce3e06a`: CI, aceptación integral, recuperación MySQL, Vercel DEV y Render DEV certificados.
- Añadido `docs/DEV_CIERRE_TECNICO_PROMOCION.md` con snapshot de datos, aislamiento, rollback y precondiciones de una futura promoción independiente `dev -> main/PROD`.
- Corregido `docs/ENTORNOS_DEV_PROD.md` para reflejar el estado corporativo vigente de Vercel PROD y la migración histórica ya cerrada.
- El Plan Maestro no fue modificado. La regla reforzada de aislamiento y la revisión de `SQ-350`/`SQ-351` quedan reservadas para una actualización mayor posterior y no adquieren autoridad ejecutable por este changeset.
- Sin cambios de runtime, `main`, PROD, datos, secretos, DNS ni compra de servicios.

## 2026-09-30 — Evitar metadata locks al adoptar DDL Fase 12 ya existente

- El smoke DEV mostró que CREATE TABLE IF NOT EXISTS todavía podía quedar esperando metadata lock sobre tablas ya materializadas.
- La recuperación Fase 12 ahora consulta INFORMATION_SCHEMA antes de cualquier DDL de tabla: si la tabla existe, no ejecuta CREATE TABLE; si falta, la crea.
- Índices siguen el mismo patrón y la validación final de tablas, columnas, índices y FK permanece fail-closed.
- No se borran ni reescriben filas; el cambio busca únicamente convergencia segura de esquema e historial.
- Sin main, PROD, secretos ni servicios pagos.

## 2026-09-30 — Reconciliación fail-closed de StorefrontFase12CuentaCliente en DEV

- El segundo smoke de Render DEV demostró que la deriva histórica también incluía las tablas de cuenta de storefront ya materializadas sin su fila de historial EF.
- 20260919011500_StorefrontFase12CuentaCliente ahora crea únicamente tablas/índices ausentes y después valida que existan las cuatro tablas, 32 columnas requeridas, siete índices y cinco relaciones FK esperadas.
- Si una estructura preexistente no coincide, la migración provoca un fallo explícito y no registra el historial como convergente.
- La recuperación no borra ni reescribe filas de cuentas, favoritos, direcciones, sesiones o productos.
- Se añadió prueba contractual para impedir regresar a CreateTable/CreateIndex no idempotentes.
- Scope exclusivo DEV; sin main, PROD, secretos ni servicios pagos.

## 2026-09-30 — Recuperación idempotente de StorefrontFase7ProductoDestacado en DEV

- El smoke exact-head de Render DEV detectó deriva histórica: `Productos.EsDestacado` existía físicamente mientras `20260918162000_StorefrontFase7ProductoDestacado` faltaba en `__EFMigrationsHistory`.
- La migración se volvió idempotente ante DDL parcial: comprueba `INFORMATION_SCHEMA` y sólo crea la columna o índice si realmente faltan.
- No se elimina, sobrescribe ni migra contenido de productos; la recuperación completa estructura faltante y permite que EF registre su historial.
- Se añadió prueba contractual para impedir volver a una operación `AddColumn/CreateIndex` no idempotente en esta migración.
- Scope exclusivo DEV; sin cambios en `main`, PROD, secretos ni servicios pagos.

## 2026-09-30 — QA Fase 3: fail-closed de base/usuario por entorno Render

- Durante QA DEV se detectó que el binding Vercel→API ya fallaba cerrado, pero el backend aún aceptaba cualquier base MySQL sintácticamente válida.
- Se añadió `EnvironmentDatabaseGuard` y se conectó al startup únicamente cuando `RENDER=true`.
- Render Development exige `solqaryn_dev` + `solqaryn_dev_user`; Render Production exige `solqaryn_prod` + `solqaryn_prod_user`.
- Se agregaron pruebas para binding válido, base cruzada, usuario cruzado, entorno ambiguo/no canónico y cadena sin base.
- Cambio sólo en `dev`; no se modificó `main`, Render PROD, datos, migraciones, secretos, DNS ni servicios pagos.

## 2026-09-30 — Aislamiento Vercel DEV: bloqueo de previews cruzados hacia proyecto PROD

- El guard `frontend/scripts/vercel-ignore-build.mjs` ahora usa `VERCEL_PROJECT_ID` para impedir que commits de la rama `dev` creen builds/previews dentro del proyecto Vercel `solqaryn-prod`.
- El bloqueo usa identidad inmutable de proyecto y no hostname/alias; `solqaryn-dev` continúa construyendo DEV normalmente.
- `validate-environment-routing.mjs` valida también esta frontera cross-project además del binding `SOLQARYN_ENV + API_UPSTREAM` y del fail-closed DEV/PROD.
- Cambio exclusivo en `dev`; no se modificó `main`, configuración/variables de PROD, datos, DNS, secretos ni servicios pagos.

## 2026-09-30 — Corrección final de ownership VAEP Javier/Alex

- Se confirmó que las cinco Supervisor canónicas pertenecen físicamente a la cuenta de Alex Morales con IDs: :05=6abd62255ae88191a2dba6e1b00d3b4d, :17=6abd6235e100819195785fc76a44a8e0, :29=6abd6241c3e48191a8856ed7b0f42c5c, :41=6abd624e5e108191bef6bb879559328c, :53=6abd625b397c8191a49904227946a20f.
- Las cinco Primary canónicas permanecen en Javier Mejía y fueron repareadas a esos IDs de Alex: :00=6aa15346f5408191bdd9043fd26ff7aa, :12=6aa1534deee481918280def1343adcfa, :24=6abd70e42cb4819190b3b28916dc9dbb, :36=6aa1535a51508191a610e6cdb90a2a4d, :48=6aa1535f8cd48191b73e10f17843372a.
- Las cinco Supervisor creadas por error en la cuenta de Javier (IDs 6abd7946e7c881919c60ed6b3bf9a81a, 6abd79517d908191b08f770920ae63d4, 6abd795d81d08191b58518eddd62ffac, 6abd7970c2288191ac452301b3bc4df2, 6abd797bbf008191a9939cc2ccc11a06) quedaron RETIRADAS, deshabilitadas y marcadas explícitamente DO_NOT_RUN.
- `_AUTOMATION_SOURCE` ya contiene los IDs reales de Alex, responsables correctos, parejas bidireccionales y 0/10 habilitadas.
- `docs/VAEP_AUTHORITY.md` fue reconciliado nuevamente para apuntar exclusivamente a los IDs reales de Alex.
- No se activó ninguna automation ni se ejecutaron runs nuevos durante esta corrección.

## 2026-09-30 — Reconstrucción y resincronización de las cinco Supervisor VAEP

- Se detectó que el Sheet canónico conservaba las cinco Supervisor, pero los objetos runtime ya no estaban presentes en el inventario vivo.
- Se recrearon las cinco Supervisor canónicas :05/:17/:29/:41/:53 con RESPONSABLE_OPERATIVO=ALEX_MORALES, zona America/Tegucigalpa, horarios canónicos y estado deshabilitado.
- Nuevos IDs Supervisor canónicos: :05=6abd7946e7c881919c60ed6b3bf9a81a, :17=6abd79517d908191b08f770920ae63d4, :29=6abd795d81d08191b58518eddd62ffac, :41=6abd7970c2288191ac452301b3bc4df2, :53=6abd797bbf008191a9939cc2ccc11a06.
- Las cinco Primary fueron repareadas a estos IDs y permanecen deshabilitadas.
- _AUTOMATION_SOURCE fue actualizado con los IDs nuevos y las relaciones cruzadas; las vistas derivadas continúan 0/10 habilitadas.
- docs/VAEP_AUTHORITY.md fue actualizado para eliminar los cinco IDs Supervisor retirados.
- Las cinco Supervisor recreadas tienen last_run_time=null; no se ejecutó ningún run durante la reconstrucción.
- No se tocó main, PROD, datos productivos, secretos, DNS, certificados ni servicios pagos.

## 2026-09-30 — Recreación controlada de SOLQARYN VAEP Primary :24

- La tarea `SOLQARYN VAEP Primary :24` fue eliminada accidentalmente desde la cuenta de Javier y recreada con el mismo horario `:24`, mismo responsable operativo, mismo contrato VAEP y estado PAUSADO.
- Nuevo ID canónico de Primary :24: `6abd70e42cb4819190b3b28916dc9dbb`.
- Se actualizó el Sheet operativo y las autoridades canónicas actuales para sustituir el ID retirado `6aa153545c5c819199047566bda1cdac`.
- Las cinco Primary de Javier permanecen pausadas; no se activó ninguna automatización.
- La Supervisor :29 de Alex debe releer/actualizar su `PRIMARY_PAIR_ID` al nuevo ID antes de declarar certificación runtime 10/10 definitiva.

## 2026-09-30 — Reparto operativo VAEP Javier/Alex y sustitución de Supervisor canónicas

- Workspace ChatGPT Business: `SOLQARYN`, con dos miembros humanos: Javier Mejía y Alex Morales.
- Javier conserva como canónicas únicamente las cinco Primary `:00/:12/:24/:36/:48`.
- Alex Morales aporta las cinco Supervisor canónicas `:05/:17/:29/:41/:53` con IDs `6abd62255ae88191a2dba6e1b00d3b4d`, `6abd6235e100819195785fc76a44a8e0`, `6abd6241c3e48191a8856ed7b0f42c5c`, `6abd624e5e108191bef6bb879559328c`, `6abd625b397c8191a49904227946a20f`.
- Las cinco Supervisor equivalentes de la cuenta de Javier fueron marcadas RETIRADA y permanecen inactivas; no deben reactivarse.
- Las cinco Primary de Javier fueron repareadas a los IDs Supervisor de Alex y permanecen inactivas.
- El Google Sheet operativo actualizó `_AUTOMATION_SOURCE`, `AUTOMATIZACIONES`, `TAREAS_PROGRAMADAS` y `TAREAS_DE_SUPERVISION`; readback confirma 5 Primary/Javier + 5 Supervisor/Alex y `0/10` habilitadas.
- `SOLQARYN - AUTORIDAD OPERATIVA VAEP`, `AGENTS.md`, `docs/VAEP_AUTHORITY.md`, `PROJECT_CONTEXT.md` y el inventario de plataformas fueron reconciliados con el nuevo reparto.
- No se tocó `main`, PROD, datos productivos, secretos, DNS, certificados ni servicios pagos.

## 2026-09-30 — Rebase integral del control-plane al Plan Maestro único SOLQARYN

- Se certificó como único roadmap vivo el Google Doc nativo `PLAN MAESTRO SOLQARYN` (ID `1YdQlNJ312HuziyKb9E-GEt55dcgSmuFGgfsHxyzPUaw`), con 354 objetivos continuos `SQ-000..SQ-353`.
- El antiguo Doc rector VAEP con contenido ERP-N/M0-M13/V5/Jules fue retirado de la carpeta operativa y preservado sólo en backup histórico.
- Se creó `SOLQARYN - AUTORIDAD OPERATIVA VAEP` (ID `1frrmekon0pBTrLcXk0yUZSJa4uISjEzrwrni3ja5y38`) como contrato operativo auxiliar; explícitamente no es un segundo Plan Maestro.
- `Notas SOLQARYN_DEV.docx` queda definida como bandeja de observaciones no ejecutables hasta aprobación e incorporación al Plan Maestro único.
- El Sheet `SOLQARYN - PLAN MAESTRO DE AUTOMATIZACIONES` fue limpiado de COLA/PLAN_MAESTRO/BITACORA históricos y rebasado a fuentes nuevas: `_MASTER_SOURCE`, `_RUNTIME_SOURCE`, `_AUTOMATION_SOURCE`, `_EVENTS_SOURCE`.
- PLAN_MAESTRO y COLA visibles ahora contienen exclusivamente los objetivos SQ del Maestro actual y estado runtime derivado por fórmulas; BITACORA visible parte vacía y deriva únicamente eventos nuevos.
- DASHBOARD, CONFIG, LEYENDA, TAREAS_PROGRAMADAS, CONTROL_TOWER, AUTOMATIZACIONES y TAREAS_DE_SUPERVISION fueron reconciliadas al modelo actual; se retiraron N8/ERP-N/M0/Jules como autoridad vigente.
- Las 14 pestañas del Sheet tienen protección administrativa; las cuatro fuentes técnicas permanecen ocultas. Nota técnica: Google Drive siempre conserva al propietario capacidad final de edición, por lo que la prohibición absoluta de edición manual se implementa como política + protección + vistas derivadas, no como imposibilidad criptográfica para el propietario.
- Las diez Tasks canónicas fueron repunteadas al nuevo Plan Maestro/contrato operativo y permanecen deliberadamente **INACTIVAS (0/10)**.
- No se tocó `main`, PROD, datos productivos, secretos, DNS, certificados ni servicios pagos.

## 2026-09-29 — Punto 10: Render Free y ruta comercial

- Readback vivo: DEV y PROD continúan en Render `free`; no se compró ni activó ningún plan.
- Auditoría del repo: no existen keep-alives, UptimeRobot ni cron/pings públicos para mantener Render despierto; los `curl /health` existentes son sólo CI local.
- Añadida guarda `scripts/validate-render-free-policy.mjs` al workflow `SOLQARYN Project Scope Lock` para impedir futuros pings artificiales a `*.onrender.com`.
- Logs DEV demuestran cold start: bootstrap 3371.5 ms con sólo 130.6 ms DB y 2608.6 ms con 89.1 ms DB; después 13.5 ms y 1.7 ms con 0 queries.
- DEV puede permanecer Free aceptando warm-up; PROD no se declara always-on mientras siga Free.
- El tamaño de un futuro plan always-on se decidirá por métricas; no hay evidencia actual que justifique CPU/RAM grande.
- Sin compras, upgrades, cambios Aiven, datos, secretos, `main` o tráfico PROD.
- Evidencia: `docs/evidencias/DEV_ANALISIS_PUNTO_10_RENDER_FREE_ALWAYS_ON_2026-09-29.md`.

## 2026-09-29 — Punto 9: Aiven/topología evaluados después de reducir queries

- La decisión se tomó después de optimizar el read path: listado público en 5 queries por miss y hits de cache en 0 queries.
- Pasadas calientes DEV observadas: 216.5–254.1 ms total con 107.5–112.3 ms DB.
- Readback vivo: único workspace Render `SOLQARYN`; DEV `oregon/free/dev` y PROD `virginia/free/main`; Aiven permanece `do-sfo`.
- Logs DEV recientes reconfirman productos en 5 queries y ~207–257 ms calientes, con hits de cache en 0 queries.
- Render no expuso series HTTP de latencia/request-count en la ventana consultada, por lo que la distancia PROD Virginia ↔ San Francisco queda como riesgo no cuantificado, no como fallo demostrado.
- DEV se mantiene en Oregon; no existe evidencia que justifique mover Aiven o DEV y no se ejecutó ningún cambio productivo.
- Si una medición futura confirma penalización material, la vía definida es backend PROD oeste en blue/green, smoke/read-only, comparación causal y cutover únicamente con autorización explícita.
- Sin compras, upgrades, migraciones, cambios de datos, secretos, `main`, tráfico PROD ni cambios Aiven.
- Evidencia: `docs/evidencias/DEV_ANALISIS_PUNTO_9_AIVEN_TOPOLOGIA_2026-09-29.md`.

## 2026-09-29 — Punto 8: Angular medido y adelgazado

- Añadido build productivo con `stats.json`, baseline raw/gzip/Brotli y desglose por paquete/módulo.
- Baseline inicial real Angular CLI: 730.17 kB raw / 171.55 kB transfer estimado.
- El shell raíz dejó de importar Material Button/Icon; la navegación conserva iconos con la fuente ya existente y botones nativos accesibles.
- `provideAnimations()` pasó a `provideAnimationsAsync()`; `@angular/animations` (~62.7 kB antes) queda fuera del grafo inicial.
- Portada SOLQARYN: contenido bajo el fold usa `@defer (on idle)`; catálogo, detalle y categorías usan preload selectivo sólo tras estabilidad. `PreloadAllModules` permanece prohibido.
- Resultado causal: 580.96 kB raw / 137.46 kB transfer estimado; reducción de 20.4% raw y 19.9% transfer. `main` baja de 126.32 a 66.94 kB (-47.0%).
- Budget `initial` endurecido de 1 MiB/2 MiB a 650 kB warning / 750 kB error y protegido por `validate-angular-bundle-policy.mjs`.
- Workflow post-optimización `36650833149`: SUCCESS.
- No se compró ningún servicio; sin DB, secretos, `main` ni PROD.
- Evidencia: `docs/evidencias/DEV_CERTIFICACION_PUNTO_8_ANGULAR_BUNDLE_2026-09-29.md`.

## 2026-09-29 — Punto 7 implementado: Cloudinary delivery responsive

- Añadido helper central de delivery Cloudinary con `f_auto,q_auto,c_limit` y variantes 320/480/640/800.
- `app-producto-imagen` y el storefront público usan `srcset`/`sizes`, dimensiones explícitas, lazy/async fuera del viewport y prioridad alta sólo para la imagen LCP de cada vista.
- Cobertura storefront: home, catálogo, categoría, detalle, miniaturas, relacionados, carrito y lightbox.
- El backend ya cumplía el contrato ligero: listados/destacados retornan `TiendaProductoResumenDto` con `ImagenPrincipalUrl`; las galerías completas quedan reservadas al detalle.
- Añadida guarda `validate-cloudinary-responsive.mjs` al lint canónico.
- Sin migración de Cloudinary, sin cambios de uploads/assets/credenciales, sin DB, secretos, `main`, PROD ni servicios pagos.
- Evidencia: `docs/evidencias/DEV_PUNTO_7_CLOUDINARY_DELIVERY_RESPONSIVE_2026-09-29.md`.
- Validación runtime aún no se declara LISTO: Vercel reporta `build-rate-limit` y el entorno local de ejecución no pudo clonar GitHub por ausencia de DNS. No se realizará upgrade ni compra para resolverlo.

## 2026-09-29 — Punto 6 certificado: cache HTTP + ETag + compresión

- Certificación DEV cerrada en `docs/evidencias/DEV_CERTIFICACION_PUNTO_6_HTTP_CACHE_ETAG_COMPRESION_2026-09-29.md`.
- Backend: Response Compression con Brotli/Gzip sobre HTTPS para JSON/text, sin servicio externo.
- Seguridad: `private, no-store, max-age=0` es el default de toda respuesta API; sólo GET públicos explícitos del storefront pueden sobrescribirlo, y cualquier request autenticado/Authorization vuelve a `no-store`.
- `PublicHttpCacheAttribute` emite perfiles diferenciados, ETag débil SHA-256, `Vary: Accept-Encoding` y 304 ante `If-None-Match`.
- Identidad/tema/WhatsApp/categorías usan TTL moderado + SWR; bootstrap usa TTL corto por contener destacados; productos usan TTL corto + must-revalidate.
- Contexto de carrito, checkout, endpoints autenticados y administración permanecen fuera de cache público.
- Vercel: bundles Angular hashados reciben `public, max-age=31536000, immutable`; rewrites `/api/*` habilitan caching para respetar exclusivamente las políticas upstream.
- Guardas en `validate-http-cache-contract.mjs` y pruebas backend verifican ETag/304, no-store de errores/autenticación y exclusión de rutas sensibles.
- Scope Lock + SOLQARYN Fases 1–7: **SUCCESS** sobre el HEAD exacto `6b364c7c3e1fd24374fbc02cface54ad3f977bee`.
- PR `#3481` integrada en `dev` como `f3d1119133c1991b742575f8a675c9f012b9b42e`; el fallo intermedio de pruebas fue sólo un harness MVC sin `RouteData` y quedó corregido/revalidado.
- Runtime DEV certificado: Render `dep-dau37vvlot8c739g9s6g` está `live`; Vercel `dpl_E1h8BhRAVcE97JcnKJjMfcozWjB3` está `READY` y sirve `solqaryn-dev.vercel.app`; el workflow canónico `36638217740` pasó ETag/304, Brotli, Gzip y no-store.
- CDN DEV verificado: bundles hashados `immutable` por un año y `/api/tienda/bootstrap` conserva Cache-Control, ETag, Brotli y `Vary: Accept-Encoding`.
- Sin migraciones, datos, secretos, `main`, entorno PROD ni servicios pagos. **Punto 6 LISTO en DEV.**

## 2026-09-29 — Punto 5 certificado: cache público tenant-aware en dos niveles

- Certificación DEV publicada en `docs/evidencias/DEV_CERTIFICACION_PUNTO_5_CACHE_DOS_NIVELES_2026-09-29.md`.
- Angular comparte identidad en vuelo y categorías entre rutas con replay; backend usa `IMemoryCache` tenant-aware con lock por key e invalidación generacional.
- TTL: identidad/tema/categorías 5 min; destacados 30 s; listados 15 s. Detalle/contexto/checkout permanecen fuera de cache.
- Scope Lock y regresiones SOLQARYN Fases 1–7: SUCCESS sobre el HEAD final de la PR.
- Render DEV desplegó `10f77f08226bd97d966f20fb53ed8b56022d2fb6` y quedó `live`.
- Runtime: bootstrap hit observado hasta **1.0 ms / 0 queries**; listado hit **1.3–2.3 ms / 0 queries**; categorías compartidas **0.8 ms / 0 queries**.
- Separación de parámetros comprobada: `pageSize=24` y `pageSize=12` generan misses independientes y cada repetición posterior cae a 0 queries.
- Vercel preview `dpl_DoZuHfXdPM91YEbyFaVLJopSeqns` validó el preview runtime-equivalente y luego `dpl_J5KYkQ7FmVs81pMBQsvzsXhw5H7r` quedó READY como deployment canónico de `solqaryn-dev.vercel.app`, sin upgrade ni pago.
- Sin migraciones, datos productivos, cambios de RBAC/tenancy, secretos, PROD, `main` ni servicios pagos.
- Punto 5: **LISTO técnicamente en DEV**; no requiere compra de servicios.

## 2026-09-29 — Punto 4 certificado: bootstrap único del storefront

- Certificación DEV publicada en `docs/evidencias/DEV_CERTIFICACION_PUNTO_4_BOOTSTRAP_STOREFRONT_2026-09-29.md`.
- `GET /tienda/bootstrap` consolida identidad pública mínima, WhatsApp público, tema, hasta 6 categorías de navegación y 4 destacados ligeros.
- Angular comparte una única carga inicial con `shareReplay`; la prueba Playwright exige 1 request bootstrap y 0 requests iniciales separados a identidad, WhatsApp, tema, categorías y destacados.
- Scope Lock y regresiones SOLQARYN Fases 1–7: SUCCESS.
- Render DEV desplegó el functional HEAD `3ad6450e037466e78c25aa07f37ff4e77e5ec109` y quedó `live`.
- Vercel `solqaryn-dev` desplegó el mismo functional HEAD y quedó `READY` sin upgrade; el rate limit temporal no bloqueó el merge final.
- Bootstrap runtime: 5 queries internas; pasadas calientes estables observadas de 215.0–332.0 ms, con picos 543.8–836.1 ms asociados a mayor tiempo DB/infraestructura gratuita.
- Sin migraciones, escrituras de datos de negocio, cambios de RBAC/tenancy, secretos, PROD, `main` ni servicios pagos.
- Punto 4: **LISTO en DEV**; no requiere acción manual del propietario.

## 2026-09-29 — Punto 3 certificado: read models públicos ligeros

- Certificación DEV publicada en `docs/evidencias/DEV_CERTIFICACION_PUNTO_3_READ_MODELS_PUBLICOS_LIGEROS_2026-09-29.md`.
- Listados/destacados usan `TiendaProductoResumenDto` + `ProductoCatalogoResumenReadModel`; detalle conserva el contrato rico con galería completa.
- `TiendaController` exige el read path público dedicado y ya no puede caer al repositorio administrativo ni a un mapper público legacy.
- Render DEV desplegó el functional HEAD `3d2af21c83403fd7f4fd4f3039a26058be64bf54` y quedó `live`.
- Runtime real confirmó que el listado no devuelve galerías y que `/tienda/productos/{slug}` sí devuelve la galería rica.
- Pasadas calientes posteriores: 216.5–254.1 ms con 5 queries, dentro del target inicial <=500 ms y una query menos que el read path previo.
- Vercel alcanzó el límite de builds del plan en commits posteriores; la equivalencia Angular quedó demostrada con preview READY previo y no se compró ni cambió ningún plan.
- Sin migraciones, datos escritos, RBAC/tenancy, secretos, PROD o `main`.
- Punto 3: **LISTO en DEV**; no requiere acción manual del propietario.

## 2026-09-29 — Punto 3: read models públicos ligeros

- Listados y destacados del storefront usan `TiendaProductoResumenDto` y `ProductoCatalogoResumenReadModel`, separados del DTO/read model rico de detalle.
- La proyección paginada elimina la query intermedia de IDs y no materializa galerías de variantes, color, talla ni descripción completa; conserva sólo los datos necesarios para tarjetas, selector de variante, precio/oferta y disponibilidad.
- `/tienda/productos/{slug}` conserva la galería y detalle completo; carrito/cuenta siguen usando contexto por IDs y no se amplía su alcance.
- Angular usa `mapearProductoResumen()` en home, catálogo, categoría y relacionados; el mapper rico queda para detalle/contexto.
- Sin migraciones, escrituras de datos, cambios de RBAC/tenancy, secretos, PROD, `main` ni servicios pagos. La certificación runtime DEV se registra después del deploy causal.

## 2026-09-29 — Punto 2 certificado: catálogo público sin descarga completa

- Certificación DEV publicada en `docs/evidencias/DEV_CERTIFICACION_PUNTO_2_CATALOGO_ACOTADO_2026-09-29.md`.
- El storefront ya no expone ni consume `obtenerCatalogo()`; catálogo, categoría, detalle, carrito, checkout y cuenta usan paginación/filtros server-side o contexto acotado por IDs.
- Render DEV y Vercel DEV verificaron el HEAD funcional `ff8743aa14ef018450ebacafb74272076e6ab269` como `live`/`READY`.
- Baseline de `GET /tienda/productos`: 2912.0 ms antes; pasadas calientes posteriores de 264.9–497.2 ms en la muestra certificada, con reducción aproximada de 83%–91%.
- Sin migraciones, datos escritos, cambios de RBAC/tenancy, secretos, PROD, `main` ni servicios pagos.
- Punto 2: **LISTO en DEV**; no requiere acción manual del propietario.

## 2026-09-29 — Catálogo remoto distingue vacío real de filtros sin coincidencias

- El catálogo paginado server-side ya no confunde una respuesta de cero resultados causada por filtros con un catálogo público realmente vacío.
- Con filtros activos se conserva el estado “No encontramos coincidencias” y la acción “Limpiar filtros”; sin filtros y sin productos se mantiene el empty state real.
- Corrección causal de la regresión Fase 8 detectada tras reemplazar la descarga completa del catálogo.
- Sin cambios de backend, datos, PROD, RBAC, tenancy ni servicios pagos.

## 2026-09-29 — Regresiones E2E alineadas con contexto reducido del carrito

- Las regresiones públicas Fases 4/5/6/9/12 mockean `POST /tienda/productos/contexto` cuando usan catálogo real.
- Se conserva la prueba de rehidratación segura tras reload sin volver a depender de descargar el catálogo completo.
- El cambio es de QA; no altera lógica de negocio, datos, PROD ni contratos públicos de runtime.

## 2026-09-29 — Rendimiento storefront: read models y lecturas acotadas

- Se añadió un read path público ligero para SOLQARYN/SOLQARYN, separado de `ProductoRepository.ConIncludes()`.
- Listado: paginación, búsqueda, categoría, disponibilidad, rango de precio y orden se envían al backend; el filtro de ofertas evalúa candidatos ligeros server-side y devuelve sólo la página solicitada.
- Detalle: carga únicamente el producto solicitado con su galería/variantes.
- Carrito, checkout, categorías y cuenta: rehidratan sólo los productos cuyos IDs están persistidos o referenciados; no descargan el catálogo completo.
- Categoría y relacionados: consultan páginas pequeñas por `categoriaId`.
- Se actualizaron gates estáticos del storefront para prohibir la reintroducción de `obtenerCatalogo()`.
- Sin migraciones, sin datos escritos, sin cambio de RBAC/tenancy, sin secretos, sin PROD y sin servicios pagos.
- Medición comparativa DEV queda pendiente del deploy exact-head de este changeset.

## 2026-09-29 — Primera captura real del baseline DEV

- Render DEV live sobre `02ec7994430812543e43371387318cfa9822e7dd` confirmó la instrumentación API/DB.
- Identidad caliente observada en 120.0–182.9 ms y categorías calientes en 44.7–184.9 ms: ambas dentro del target inicial <=300 ms.
- `GET /tienda/productos` observado en 2912.0 ms con 7 queries y 210.4 ms acumulados de DB: falla el target <=500 ms y muestra que la mayor parte del tiempo está fuera de ejecución SQL medida.
- Build Angular exact-head: 724.32 kB raw / 169.88 kB estimated transfer inicial; cumple el warning vigente de 1 MiB.
- LCP/INP/CLS y requests/bytes por pantalla quedan instrumentados y requieren únicamente captura real de navegador DEV; no se compró ni contrató ningún servicio.

## 2026-09-29 — Baseline browser DEV endurecido para Render Free

- El capturador frontend conserva una muestra rápida a 3 s y añade una muestra `settled` a 10 s para incluir requests lentas que todavía estén en vuelo.
- La primera navegación mide recursos desde el inicio del documento, evitando subcontar recursos iniciales anteriores al bootstrap Angular.
- Alcance exclusivo DEV/local; PROD, datos, secretos, RBAC, tenancy y lógica de negocio permanecen sin cambios.

## 2026-09-29 — Recuperación causal responsive durante baseline DEV

- Las regresiones Fase 10/11/12 detectaron que una tablet táctil con viewport ancho recibía el header de escritorio aunque el catálogo permanecía en layout táctil.
- Se alineó el breakpoint del header con la política mobile-first ya usada por el catálogo: el layout de escritorio requiere además `(hover: hover) and (pointer: fine)`.
- Cambio acotado a CSS responsive; no modifica negocio, datos, backend, PROD, `main` ni la instrumentación del baseline.

## 2026-09-28 — Baseline de rendimiento DEV sin servicios pagados

- Se instrumentó DEV para registrar duración total API, cantidad y tiempo de queries MySQL por request y correlación `Server-Timing`, sin registrar SQL, parámetros, PII ni secretos.
- Angular DEV captura TTFB, requests/bytes por pantalla, LCP/INP/CLS y mantiene las muestras en `window.__SOLQARYN_PERF_BASELINE__`; no envía telemetría a terceros.
- Se añadió `npm run perf:bundle-baseline` para medir bundle inicial y chunks en raw/gzip/Brotli.
- Targets iniciales: API pública caliente <=500 ms; identidad/categorías calientes <=300 ms; LCP <=2.5 s; INP <=200 ms; CLS <=0.10.
- La instrumentación queda habilitada por `appsettings.Development.json`; PROD, `main`, datos, secretos e infraestructura productiva no se modifican.

## 2026-09-28 — Contexto canónico reconciliado con cierre PROD

- `PROJECT_CONTEXT.md` deja de marcar Vercel PROD como pendiente: `solqaryn-prod` ya está activo y certificado sobre `main`.
- El contexto canónico registra que la migración histórica de SOLQARYN hacia PROD ya fue ejecutada y certificada.
- Cloudflare queda descrito en su estado real: `solqaryn.com` delegado, con cutover del dominio personalizado deliberadamente aplazado y no bloqueante.
- No hubo cambios de código, datos, secretos, runtime ni infraestructura; la corrección es exclusivamente documental.
- `docs/DETALLES_PENDIENTES.md` permanece sin cambios y conserva únicamente los tres aplazamientos deliberados vigentes.

## 2026-09-27 — Retiro de repositorio personal legacy y deudas cerradas

- El repositorio personal privado `jmejia31/SOLQARYN` fue eliminado por el propietario y la API de GitHub confirma `404 Not Found`.
- El repositorio corporativo vigente continúa siendo `solqaryn/Solqaryn`.
- Las ramas temporales de migración/auditoría ya fueron retiradas; permanecen únicamente `main` y `dev`.
- `solqaryn-prod` en Vercel ya existe y la certificación final del frontend PROD fue cerrada; por ello se retiró ese ítem de `docs/DETALLES_PENDIENTES.md`.
- Se eliminó del documento de pendientes la deuda de retiro de recursos GitHub legacy, ya que dejó de ser pendiente.

## 2026-09-26 — Identidad SOLQARYN y legado bloqueados

- Autoridad operativa fijada en `SOLQARYN / solqaryn/Solqaryn / dev`.
- El único artefacto heredado permitido como fuente futura es el respaldo verificado de la base histórica de SOLQARYN.
- Infraestructura, cuentas, deployments, repositorios, dominios y variables legacy no son dependencias ni fallback de SOLQARYN.
- SOLQARYN permanece como primer tenant/cliente, no como identidad de plataforma.
- Los defaults de `EmpresaConfiguracion` fueron neutralizados para no imponer la marca de un tenant a nuevas empresas.
- Runbooks de entornos, rollback, equivalencia, producción y migración fueron actualizados a la topología SOLQARYN vigente.
- Las entradas históricas inferiores se conservan únicamente como trazabilidad; no tienen autoridad operativa.

## 2026-09-26 — SMTP y variables Render normalizados en DEV

- DEV y PROD quedan definidos con el mismo contrato canónico de 28 variables en `render.yaml`; solo cambian valores propios del entorno.
- SMTP de SOLQARYN usa Outlook.com + OAuth2 exclusivamente para autenticación real.
- Se retiró del backend el camino de autenticación SMTP por contraseña y también el uso de client secret OAuth2.
- Se retiraron del contrato desplegado las variables redundantes documentadas en `docs/RENDER_ENVIRONMENT_CONTRACT.md`.
- El refresh token continúa siendo secreto independiente por entorno.
- No hubo migración de base de datos.
- PROD no se cambia en runtime hasta certificar DEV con build/deploy, `SMTP_OK` y envío real controlado.

## 2026-09-25 — Retiro DEV personal: auditoría destructiva previa

- Vercel personal DEV `identidad-retirada-desarrollo`: ya eliminado previamente; el team corporativo sólo contiene `solqaryn-dev`.
- Render: workspace corporativo visible = `SOLQARYN`; servicios visibles = `solqaryn-api-dev` y servicio reservado PROD. El DEV histórico `solqaryn-api-desarrollo` / `srv-d9jblq7avr4c73c74jng` no es accesible desde ese workspace y su hostname público ya no resuelve. No se ejecutó delete.
- Aiven: workflow read-only `DEV - Inventario Aiven para retiro legacy`, run `36193976802`, SUCCESS. El token corporativo ve únicamente proyecto `solqaryn` y servicio `solqaryn-mysql`; `legacyProjectNames=[]`. Esto prueba aislamiento de la cuenta corporativa, pero NO prueba que PROD legacy no consuma el Aiven personal. Borrado Aiven personal queda bloqueado hasta auditoría de PROD legacy desde la cuenta antigua.
- Cloudinary: el cloud nuevo `riyrzmob` ya sirve DEV; el cloud personal legacy no se elimina hasta confirmar cero consumidores de PROD legacy.
- Cloudflare corporativo: cuenta `Solqaryn.platform@outlook.com's Account`; zona `solqaryn.com` pending; sólo dos TXT `_acme-challenge`; cero aliases DEV legacy en la zona corporativa.
- GitHub: permiso efectivo observado `jmejia31=admin`, `morales35alex=write`. Se mantiene la decisión ratificada: `jmejia31` es Owner secundario/de recuperación, no acceso residual.
- Producción no fue modificada.

## 2026-09-25 — DEV: certificación funcional final PASS

- Workflow canónico: `DEV - Certificación funcional final`.
- Run: `36192919335` → SUCCESS.
- `/login`: HTTP 200 y shell/SEO SOLQARYN.
- `/dashboard`: HTTP 200 y shell/SEO SOLQARYN; la evidencia visual autenticada del propietario muestra el administrativo SOLQARYN operativo.
- `/SOLQARYN` y `/SOLQARYN/productos`: HTTP 200.
- API identidad: SOLQARYN / “Eleva tu mundo digital”.
- API categorías: 2 categorías.
- API productos: 4 productos públicos migrados y stock reconciliado.
- Imágenes: URLs canónicas `res.cloudinary.com/riyrzmob/.../solqaryn_dev/...` verificadas con HTTP 200.
- Render: `/health/ready` HTTP 200.
- Aiven DEV: escritura + lectura sobre tabla temporal dentro de `solqaryn_dev` → PASS, sin cambio persistente.
- Runtime source gate: cero referencias a `identidad-retirada-desarrollo`, `identidad-retirada-mysql-identidad-retirada.c.aivencloud.com` y `SOLQARYN_desarrollo`.
- Logs Render actuales: conexiones únicamente a `solqaryn_dev` en `solqaryn-mysql-solqaryn.h.aivencloud.com`; búsqueda del host Aiven personal y del proyecto Vercel legacy devolvió cero logs.
- PROD touched: FALSE.

## 2026-09-25 — Cloudflare DEV/DNS cerrado como N/A runtime

- Vercel DEV canónico `solqaryn-dev` fue leído por API y expone únicamente dominios administrados por Vercel: `solqaryn-dev.vercel.app`, `solqaryn-dev-solqaryn.vercel.app` y `solqaryn-dev-git-dev-solqaryn.vercel.app`.
- Render DEV canónico expone únicamente `https://solqaryn-api-dev-fxx8.onrender.com`.
- El repositorio no contiene una integración/configuración Cloudflare ni un dominio `solqaryn.com` activo para DEV.
- No existe actualmente un CNAME/A/AAAA custom de DEV que deba transferirse para mantener operativa la plataforma.
- Conclusión: Cloudflare no forma parte de la cadena crítica DEV actual; el punto Cloudflare DEV/DNS queda **N/A / CERRADO** para el cierre de DEV.
- La titularidad de la cuenta Cloudflare y cualquier zona futura/productiva se auditarán por separado al preparar dominios propios/PROD; no se realizó ningún cambio DNS ni certificado.

## 2026-09-25 — DEV: reconciliado stock administrativo vs storefront

- Causa confirmada: el listado administrativo proyecta `ProductoVariante.Cantidad` como bridge de compatibilidad; el storefront y checkout usan `ExistenciaVariante.StockDisponible` como autoridad física.
- Diagnóstico read-only run `36188235798`: 9 variantes legacy, una sola existencia física, 8 variantes legacy con stock positivo y 7 de ellas sin existencia; total bridge 57 vs stock físico 10. En productos activos/no eliminados, el desfase era 52 vs 10.
- Antes de escribir se ejecutó backup cifrado real + restore drill del mismo artifact: run `36188392969` SUCCESS.
- Reconciliación fail-closed run `36190745792`: único almacén operativo activo `UAT-ALM-001`; 6 filas `ExistenciaVariante` creadas para productos activos, sin sobrescribir la existencia ya correcta del UAT.
- Postcheck transaccional: stock activo `ProductoVariante.Cantidad=52` y `ExistenciaVariante.StockFisico=52`; 0 variantes activas sin existencia; 0 diferencias.
- Verificación API pública posterior: Cargador 26, Laptop 15, Funda para samsung 1, UAT Producto 001 10; todos con disponibilidad coherente.
- Se dejaron fuera dos variantes de productos eliminados que suman 5 unidades legacy; no se reactivaron ni se convirtieron en stock público.
- PROD no fue tocado.

## 2026-09-25 — Cloudinary DEV: upload canónico validado e inventario legacy cuantificado

- El upload real desde la aplicación DEV creó un asset en el cloud `riyrzmob` bajo `solqaryn_dev/Solqaryn/productos/empresas/1/`; la API pública devuelve la URL nueva.
- Render DEV quedó `live` después de configurar `Cloudinary__CloudName=riyrzmob`, la key DEV y `Cloudinary__EnvironmentPrefix=solqaryn_dev`.
- Inventario read-only run `36182095589` / artifact `cloudinary-dev-legacy-inventario-36182095589` detectó 13 filas lógicas aún dependientes del cloud legacy: 10 imágenes de producto, 1 documento de compra y 2 fotos de perfil.
- No hubo escrituras en el inventario ni se tocó PROD.
- Decisión: no eliminar todavía el Cloudinary personal. El siguiente subpaso es migrar esas 13 referencias y revalidar cero dependencias.

## 2026-09-25 — Cloudinary DEV conectado a Render y redeploy validado

- Captura de Render `solqaryn-api-dev` confirma las variables `Cloudinary__ApiKey`, `Cloudinary__ApiSecret`, `Cloudinary__CloudName=riyrzmob` y `Cloudinary__EnvironmentPrefix=solqaryn_dev`.
- API Key y API Secret permanecen ocultos; la key destinada es `solqaryn_dev`.
- Render lanzó el deploy manual `dep-darcsc0jo6nc73fffmtg`, que terminó `live` el 2026-09-25T19:41:55Z.
- El health `/health/ready` del nuevo instance respondió HTTP 200 después del redeploy.
- Falta únicamente el upload funcional de un activo DEV para demostrar que el PublicId/URL nuevo queda bajo el cloud `riyrzmob` y prefijo `solqaryn_dev/`.

## 2026-09-25 — Cloudinary keys DEV/PROD creadas

- Evidencia visual confirma tres API keys activas en el cloud `riyrzmob`: `Root`, `solqaryn_dev` y `solqaryn_prod`.
- `Root` queda reservada para administración/recuperación.
- `solqaryn_dev` queda destinada exclusivamente a Render DEV.
- `solqaryn_prod` queda reservada para la futura fase PROD; no se conecta ni usa todavía.
- Los API Secret permanecen ocultos y no se registraron en repositorio/chat.

## 2026-09-25 — Cloudinary DEV: inventario de API keys

- Captura de `Cloudinary -> Product environment settings -> API Keys` para cloud `riyrzmob` muestra una sola key activa llamada `Root`, creada el 2026-09-22.
- No existe todavía una key dedicada llamada `solqaryn_dev`.
- El valor de API Secret no fue revelado ni registrado.
- Decisión: no reutilizar la key Root como credencial operativa DEV; crear una API key dedicada `solqaryn_dev`, mantener Root para administración/recuperación y luego configurar Render DEV con la key dedicada.
- PROD no se toca.

## 2026-09-25 — Cloudinary DEV ownership confirmado

- Capturas del panel confirman perfil `Solqaryn Platform` con correo `solqaryn.platform@outlook.com`.
- Existe un único Product Environment activo: cloud name `riyrzmob`, ID `7ab9e3e6de660a0b70eb4a5bacf331`.
- La Media Library del nuevo cloud muestra solo assets de ejemplo; no se consideran migrados los medios históricos de SOLQARYN.
- Próximo gate: revisar API Keys, certificar la key DEV y demostrar mediante upload que Render DEV escribe bajo `solqaryn_dev/`.
- No borrar todavía credenciales/assets del Cloudinary legacy.

## 2026-09-25 — Vercel DEV legacy eliminado de la cuenta personal

- El propietario eliminó manualmente el proyecto `proyecto Vercel DEV legacy retirado` del workspace personal `workspace Vercel personal legacy`.
- Evidencia visual posterior muestra que en ese workspace ya sólo permanece `SOLQARYN`.
- `SOLQARYN` se mantiene congelado para la futura fase PROD.
- Resultado: Vercel DEV nuevo `solqaryn-dev` continúa como único DEV canónico bajo SOLQARYN y la dependencia Vercel DEV de la cuenta personal queda retirada.

## 2026-09-25 — Vercel legacy DEV: variables ambientales auditadas antes de eliminación

- Captura de `proyecto Vercel DEV legacy retirado -> Environment Variables` confirma `No Environment Variables Added`.
- El proyecto legacy DEV no contiene variables de entorno de proyecto que deban migrarse o conservarse.
- Junto con la auditoría previa de dominios (solo `alias automático Vercel DEV retirado`), el proyecto `proyecto Vercel DEV legacy retirado` queda autorizado para eliminación manual desde la cuenta personal.
- `SOLQARYN` PROD permanece fuera de alcance y no debe tocarse.

## 2026-09-25 — Vercel legacy DEV: dominios auditados antes de eliminación

- Captura del proyecto personal `proyecto Vercel DEV legacy retirado` confirma que la sección Domains contiene únicamente `alias automático Vercel DEV retirado`.
- No se observan dominios personalizados adicionales en ese proyecto.
- El dominio es el alias automático de Vercel del proyecto legacy y no necesita migración.
- El proyecto aún NO se elimina hasta revisar Environment Variables.
- `SOLQARYN` PROD permanece fuera de alcance.

## 2026-09-25 — Vercel legacy personal inventariado antes del retiro DEV

- Captura del propietario confirma que la cuenta/workspace Vercel personal `workspace Vercel personal legacy` mantiene dos proyectos: `proyecto Vercel DEV legacy retirado` y `SOLQARYN`.
- Alcance de cierre DEV: auditar y retirar únicamente `proyecto Vercel DEV legacy retirado`.
- `SOLQARYN` queda explícitamente fuera de alcance y congelado hasta la fase PROD.
- No se ha eliminado ningún proyecto en este paso.

## 2026-09-25 — Vercel DEV cerrado con ownership corporativo confirmado visualmente

- Evidencia visual del dashboard `vercel.com/solqaryn` muestra el workspace/team activo `SOLQARYN` y la sesión `solqarynplatform-5337` asociada a `solqaryn.platform@outlook.com`.
- Esto completa la comprobación de ownership que el conector Vercel no podía exponer por API.
- El proyecto nuevo `solqaryn-dev` queda **CERRADO / PASS** en Vercel DEV.
- Permanece pendiente únicamente retirar `proyecto Vercel DEV legacy retirado` desde la cuenta Vercel personal antigua, tras inspeccionar que no tenga dominios/variables que deban conservarse.
- `SOLQARYN` de Producción no se toca en la fase DEV.

## 2026-09-25 — Vercel DEV técnicamente certificado; cierre de ownership/legacy requiere panel

- Team leído por el conector: `SOLQARYN` / `team_owJ2SudSPWiEzeiDthSVV063`.
- Único proyecto visible: `solqaryn-dev` / `prj_1Anhx5mWyXEBX89lWC24Py6JXe7A`.
- Dominio canónico: `solqaryn-dev.vercel.app`; deployments `READY` desde `solqaryn/Solqaryn`, rama `dev`.
- Rutas `/`, `/login`, `/dashboard`, `/SOLQARYN` y `/SOLQARYN/productos` responden HTTP 200.
- APIs de identidad, categorías y productos responden HTTP 200 con los datos migrados de SOLQARYN.
- Vercel reporta cero runtime errors en las últimas 24 horas.
- El conector no expone el email del owner del team ni tiene acceso al proyecto personal legacy `proyecto Vercel DEV legacy retirado`; el propietario debe confirmar el email del team y, al final del punto, eliminar el proyecto legacy desde la cuenta personal.
- Se observan URLs Cloudinary históricas en el catálogo; se trasladan al punto Cloudinary y no se borran activos aún.

## 2026-09-25 — Render DEV certificado bajo SOLQARYN

- Workspace leído directamente por el conector Render: `SOLQARYN`, email `solqaryn.platform@outlook.com`.
- Servicio canónico: `solqaryn-api-dev`, rama `dev`, repositorio `solqaryn/Solqaryn`, auto deploy condicionado a checks, Dockerfile `./backend/Dockerfile`.
- URL canónica: `https://solqaryn-api-dev-fxx8.onrender.com`; health path `/health/ready`; maintenance OFF; servicio no suspendido.
- Logs actuales confirman que el backend usa la base `solqaryn_dev` en `solqaryn-mysql-solqaryn.h.aivencloud.com`.
- Logs actuales de `/health/ready` responden HTTP 200.
- Punto Render DEV: **CERRADO**. La eliminación de cualquier Render DEV legacy personal requiere únicamente localizar ese recurso en la cuenta antigua y comprobar cero consumidores.

## 2026-09-25 — Aiven DEV certificado bajo SOLQARYN

- Run canónico: `36175275439` — `DEV - Certificación canónica Aiven` — SUCCESS.
- Artifact: `aiven-dev-certification-36175275439`.
- Aiven API confirmó el proyecto `solqaryn`, servicio `solqaryn-mysql`, estado `RUNNING` y que `solqaryn.platform@outlook.com` pertenece a la organización que contiene el proyecto.
- MySQL real confirmó `DATABASE()=solqaryn_dev` y usuario efectivo `solqaryn_dev_user` sobre MySQL 8.4.8.
- Inventario observado: 137 tablas base, 107 migraciones EF, 8 productos y 2 categorías.
- No se imprimieron secretos ni se tocó Producción.
- Punto Aiven DEV: **CERRADO**. La eliminación de Aiven legacy personal queda condicionada únicamente a comprobar que PROD legacy no lo consume.

## 2026-09-25 — Certificación canónica Aiven DEV preparada

- Se añade un gate repetible de solo lectura para certificar que el project `solqaryn` y el service `solqaryn-mysql` corresponden al endpoint DEV configurado.
- El gate comprueba mediante Aiven API que `solqaryn.platform@outlook.com` pertenece a la cuenta que contiene el proyecto canónico, sin imprimir tokens ni contraseñas.
- El gate comprueba mediante MySQL real que DEV opera sobre `solqaryn_dev` con el usuario efectivo `solqaryn_dev_user`, además de verificar esquema/migraciones y presencia de catálogo migrado.
- No realiza escrituras de aplicación ni toca Producción.

## 2026-09-25 — Ratificación de ownership GitHub y colaboradores canónicos

**Decisión explícita del propietario; supersede la nota operativa anterior que proponía retirar a `jmejia31`.**

- Repositorio canónico: `solqaryn/Solqaryn`, dentro de la organización `solqaryn`; no se regresa a un repositorio personal.
- Identidad corporativa primaria para cuentas/proveedores de SOLQARYN: `solqaryn.platform@outlook.com`.
- `jmejia31` permanece intencionalmente como **Owner secundario/de recuperación** de la organización. Su permiso efectivo `admin` sobre el repositorio es esperado y no constituye deuda de migración.
- `morales35alex` permanece como colaborador externo con permiso `write`.
- No remover, degradar ni tratar a `jmejia31` como acceso residual sin una nueva autorización explícita del propietario.
- Esta decisión queda documentada en `PROJECT_CONTEXT.md`, `docs/ENTORNOS_DEV_PROD.md` y `docs/COLABORATIVO.md` para que cualquier conversación/agente nuevo recupere el criterio correcto desde el repositorio.

## 2026-09-25 — Inicio controlado de migración DEV legacy de SOLQARYN

- Se añade un workflow aislado que usa el environment `LEGACY_DEV_MIGRATION` para extraer únicamente `SOLQARYN_desarrollo` desde el Aiven legacy.
- El backup se cifra antes de publicarse como artifact, incluye SHA-256, conteos por tabla, metadata y referencias externas, y se restaura en MySQL 8.4 descartable para verificar integridad.
- El workflow no toca `defaultdb`, Producción, `solqaryn_dev`, `solqaryn_prod` ni recursos legacy de Vercel/Render.
- No se versionan passwords ni passphrases; los valores viven únicamente en GitHub Environment secrets.

## 2026-09-25 — Auditoría DEV: proxy Vercel, CI y endpoint Render canónico

- Vercel DEV deja de apuntar al hostname inexistente `solqaryn-api-dev.onrender.com` y usa `solqaryn-api-dev-fxx8.onrender.com`.
- La raíz `/` ya no reescribe bots al SEO de SOLQARYN; SOLQARYN conserva su identidad de plataforma y el cliente vive bajo `/SOLQARYN`.
- Se corrigieron gates CI que todavía exigían `solqaryn-api-desarrollo` o el hostname Render anterior.
- Runbooks operativos y auditoría M13 quedaron alineados al hostname DEV real.
- `Database__ServerVersion` declarativo se alinea con MySQL 8.4.8 observado en Aiven DEV.

## 2026-09-25 — Shell DEV desacoplado de la identidad de SOLQARYN

- El storefront obtiene una frontera propia `SOLQARYNIdentidadService`: su identidad comercial se carga desde la configuración pública y, ante indisponibilidad, degrada a `Tienda` sin presentar SOLQARYN como si fuera el cliente.
- `/login` y demás rutas públicas de plataforma restablecen la identidad SOLQARYN y eliminan overrides visuales de empresa; una identidad de empresa solo se aplica al storefront o a una sesión autenticada con tenant verificado.
- Se retiraron fallbacks comerciales hardcodeados de SOLQARYN en header/footers/checkout/SEO; los datos reales persistidos siguen teniendo prioridad.
- Alcance deliberado: separación de contextos para el primer cliente sin migraciones ni cambios productivos. Antes de publicar un segundo storefront debe existir resolución pública tenant-addressed (dominio/slug -> Empresa) para identidad, tema y catálogo.

- El shell global del frontend ahora identifica a `SOLQARYN` en `index.html` (title, description y Open Graph).
- Se retiró el favicon global `assets/SOLQARYN-logo.png`; el activo de SOLQARYN permanece únicamente como activo de su cliente/storefront donde corresponda.
- La identidad fallback de `EmpresaIdentidadService` ahora es SOLQARYN y ya no cae a SOLQARYN cuando la configuración pública no está disponible.
- La ruta raíz `/` deja de abrir directamente el storefront de SOLQARYN y redirige a `/login`; el cliente SOLQARYN continúa disponible explícitamente bajo `/SOLQARYN`.
- Se actualizó la validación de Fase 11 para exigir que el shell global sea SOLQARYN sin alterar las pruebas SEO específicas del cliente SOLQARYN.
- Alcance: frontend DEV. No se modificó `main`, Producción, datos, secretos, dominios ni infraestructura productiva.
- MAPA_ARQUITECTURA: separación de identidad de plataforma vs. cliente en el shell global y fallback frontend; las rutas y SEO específicos de SOLQARYN se preservan como funcionalidad de cliente.

## 2026-09-23 — Configuración externa SOLQARYN generalizada

- La configuración operativa de GitHub Actions dejó de usar nombres ligados a una fase histórica.
- Environment canónico: `Desarrollo`.
- Variables canónicas: `SOLQARYN_DESARROLLO_BACKUP_SCHEDULE_ENABLED`, `SOLQARYN_DESARROLLO_DB_HOST`, `SOLQARYN_DESARROLLO_DB_PORT`, `SOLQARYN_DESARROLLO_DB_NAME`, `SOLQARYN_DESARROLLO_DB_USER`.
- Secrets canónicos: `SOLQARYN_DESARROLLO_DB_PASSWORD`, `SOLQARYN_DESARROLLO_BACKUP_PASSPHRASE`, `SOLQARYN_AIVEN_TOKEN`, `SOLQARYN_DESARROLLO_DB_CONNECTION`.
- Los scripts y workflows activos de backup/restore se renombraron por propósito; la documentación de la fase histórica quedó marcada como evidencia histórica.
- Se eliminaron dependencias operativas restantes a `jmejia31/Solqaryn` en workflows activos.
- Los valores secretos no fueron leídos ni copiados; la migración de valores debe completarse desde GitHub Settings/Aiven y validarse antes de retirar los nombres antiguos.
- MAPA_ARQUITECTURA: NO_APLICA — cambio de naming/gobierno de configuración operativa, sin cambio de dominio ni persistencia de producto.

## 2026-09-23 — Identidad canónica SOLQARYN consolidada

- Repositorio canónico: `solqaryn/Solqaryn`.
- Identidad de proyecto: `PROJECT_ID=SOLQARYN`.
- Se normalizaron rutas, namespaces, scripts, documentación, guards y referencias operativas a SOLQARYN.
- La rama canónica continúa siendo `Desarrollo`.
- La migración no reescribe el historial Git; el árbol vigente queda gobernado únicamente por SOLQARYN.
- MAPA_ARQUITECTURA: identidad/namespaces actualizados sin cambiar las fronteras funcionales del dominio.

## 2026-09-23 — Modelo definitivo: una skill local + nueve referencias originales

- SOLQARYN mantiene exactamente una skill local: `.agents/skills/solqaryn-project-governance/SKILL.md`.
- Las nueve capacidades adicionales no se copian ni instalan como skills locales; quedan registradas como referencias externas autorizadas a sus fuentes originales.
- Registro canónico: `docs/REGISTRO_REFERENCIAS_SKILLS_SOLQARYN.md`.
- Allowlist canónica: `docs/PROJECT_EXTERNAL_CONTEXT_ALLOWLIST.md`.
- Se verificó la existencia de los ocho repositorios externos y sus pins fijados; las rutas oficiales de especificación/`SKILL.md` quedaron registradas.
- La referencia oficial de autoría integrada se resuelve directamente en ChatGPT/OpenAI cuando la tarea de skills la requiere.
- Queda prohibido sustituir una fuente original por una copia, fork, mirror o reutilización alojada dentro de otro proyecto.
- Se retiraron las nueve copias locales creadas durante la iteración previa; el gate ahora exige `LOCAL_SKILL_COUNT=1`.
- Los 18 puntos operativos declaran el registro externo y la única skill local.
- `scripts/verify-project-scope.mjs` valida una sola skill local, nueve fuentes originales autorizadas y sus pins/resoluciones.
- Consultar una referencia externa no autoriza instalar dependencias, ejecutar scripts/binarios ni modificar arquitectura, seguridad o Producción.
- MAPA_ARQUITECTURA: NO_APLICA — cambio de gobierno de skills y fuentes; no modifica dominio, persistencia, tenancy ni deployment.

## 2026-09-23 — Namespace exclusivo de skills SOLQARYN

- La única skill de gobierno activa del proyecto queda identificada como `.agents/skills/solqaryn-project-governance/SKILL.md`.
- Convención obligatoria: directorio y frontmatter de toda skill deben comenzar por `solqaryn-`; el nombre visible debe comenzar por `SOLQARYN`.
- Se eliminó la ruta anterior de la skill y no se conserva alias.
- Los 18 archivos operativos de gobierno apuntan exclusivamente a la skill SOLQARYN.
- `scripts/verify-project-scope.mjs` rechaza skills sin namespace SOLQARYN, identidades de proyecto no canónicas, repositorios no autorizados y URIs externas de skills.
- `docs/PROJECT_SCOPE_LOCK.md` y `docs/PROJECT_EXTERNAL_CONTEXT_ALLOWLIST.md` quedaron vinculados a SOLQARYN.
- Auditoría de referencias de proyecto ajenas en el repositorio: sin coincidencias detectadas para los identificadores previamente contaminantes ni para URIs externas de skills.
- Paquete de la skill SOLQARYN validado correctamente.

`PROJECT_SCOPE_LOCK=STRICT`  
`SKILL_NAMESPACE=solqaryn-`

MAPA_ARQUITECTURA: NO_APLICA — cambio de gobierno y aislamiento de contexto; no modifica dominio, datos, tenancy ni deployment de la aplicación.

## 2026-09-23 — Blindaje estricto de aislamiento de proyecto y skill propia de Solqaryn

**Objetivo:** impedir que chats, agentes, automatizaciones o scripts de Solqaryn consulten o utilicen skills, documentación, repositorios, chats, memorias o gobierno de otros proyectos sin autorización explícita del propietario.

- Se creó la política canónica `docs/PROJECT_SCOPE_LOCK.md`.
- Se creó `docs/PROJECT_EXTERNAL_CONTEXT_ALLOWLIST.md` con estado inicial sin excepciones activas.
- Se creó la skill propia `.agents/skills/solqaryn-project-governance/SKILL.md`, exclusiva de `solqaryn/Solqaryn`.
- Se agregó `scripts/verify-project-scope.mjs` como gate fail-closed.
- Se agregó `.github/workflows/project-scope-lock.yml` para validar el aislamiento en push/PR.
- Los 18 archivos operativos solicitados declaran `PROJECT_SCOPE_LOCK=STRICT` y `EXTERNAL_PROJECT_CONTEXT=DENY_BY_DEFAULT`.
- `.githooks/pre-commit` ejecuta el scope validator y bloquea el commit si falla.
- `.githooks/post-commit` bloquea el auto-push cuando el scope validator falla.
- Se verificó mediante lectura remota que los 18 archivos, la política, la allowlist y la skill contienen el lock y no contienen referencias conocidas al gobierno FUENTE_EXTERNA_RETIRADA.
- Un permiso externo persistente solo existe si queda versionado como `ACTIVE` en la allowlist; en caso contrario, el default es DENY.

`PROJECT_ID=SOLQARYN`  
`REPOSITORY=solqaryn/Solqaryn`  
`PROJECT_SCOPE_LOCK=STRICT`

- El validador fue endurecido para rechazar cualquier `PROJECT_ID` distinto de `SOLQARYN`, cualquier `REPOSITORY` distinto de `solqaryn/Solqaryn`, URIs `skills://` en gobierno del proyecto y referencias GitHub a repositorios ajenos en la superficie canónica.
- La política declara explícitamente que cada chat/sesión nueva nace con el scope lock activo y no debe descubrir skills específicas de otros proyectos.

MAPA_ARQUITECTURA: NO_APLICA — cambio de gobierno, aislamiento de contexto y controles de repositorio; no modifica arquitectura funcional, datos, tenancy ni deployment de la aplicación.

## 2026-09-23 — Migración operativa GitHub a organización solqaryn

**Objetivo:** cerrar la dependencia operativa del repositorio respecto a la cuenta personal `jmejia31`.

- Propietario canónico del repositorio: `solqaryn/Solqaryn`.
- Rama de trabajo: `Desarrollo`.
- Se actualizaron gates, hooks, scripts de sesión, documentación canónica, autoridad VAEP y runbooks activos para usar `solqaryn/Solqaryn`.
- `.github/CODEOWNERS` dejó de exigir a `@jmejia31` como aprobador obligatorio.
- La evidencia histórica conserva referencias antiguas cuando corresponden a hechos pasados; no constituye autoridad operativa.
- La instalación GitHub de la organización `solqaryn` existe para este repositorio.
- La cuenta autenticada actual `jmejia31` todavía conserva permiso `admin`; esto es acceso personal residual y debe retirarse desde la configuración de la organización cuando exista al menos otro owner/admin organizacional confirmado.

MAPA_ARQUITECTURA: NO_APLICA — cambio de ownership/gobernanza GitHub, sin modificación de arquitectura de aplicación.

## 2026-09-15 — Cierre visual SMTP — materialización de capturas

**Responsable:** Codex, ejecución autorizada por Javier Mejía en `Desarrollo`.

Se materializaron como PNG las pantallas reales de Render (servicio Live, Environment con secretos ocultos, arranque y logs del envío) y Solqaryn (facturación, FAC-000003, historial y evidencia persistente posterior al único envío). Se incorporaron la captura de recepción, el PDF A4 y la captura final del inventario de la carpeta en `docs/evidencias/cierre-correo-smtp/2026-09-15_1048/`. No hubo cambios de código, nuevas ventas, nuevos correos, cambios de variables, Producción ni WhatsApp. La única reapertura diagnóstica permitida mostró un fallo transitorio y quedó descrita sin fabricar un PASS.
# CHANGELOG_AI — Solqaryn

Bitácora colaborativa de cambios realizados por Javier Mejía, Codex, AntiG/Antigravity, ChatGPT, Chat B (ChatGPT Business) y futuros agentes autorizados.

No reemplaza `git log`: registra intención, alcance, validaciones y handoff. Todo changeset intencional debe incluir una entrada breve; no modificar otros colaborativos si su contenido no cambió.


## 2026-09-24 — Cierre de configuración Aiven/GitHub canónica DEV/PROD

**Responsable:** ChatGPT/VAEP con autorización expresa del propietario.

- Aiven quedó normalizado al proyecto `solqaryn`, servicio MySQL Free único `solqaryn-mysql`, bases `solqaryn_dev` y `solqaryn_prod`.
- Los usuarios `solqaryn_dev_user` y `solqaryn_prod_user` fueron limitados exclusivamente a su base; las pruebas cruzadas devolvieron Access denied en el entorno opuesto.
- GitHub recibió los Environments canónicos `Desarrollo` y `Produccion` con variables/secrets separados y tokens Aiven independientes.
- La prueba `N8.8.G - Aiven DEV Environment Token Scope Proof` pasó en el run `35959515163`.
- El primer backup real detectó un defecto legítimo del script al consultar `__EFMigrationsHistory` sobre una base nueva sin esquema. Se corrigieron backup/restore para soportar una base canónica vacía sin degradar integridad: tabla de migraciones opcional y archivo de conteos vacío permitido solo cuando `baseTableCount=0`.
- El run `35959731072` terminó con provider proof, backup cifrado real y restore same-artifact descartable en SUCCESS; artifact `solqaryn-backup-desarrollo-35959731072` generado.
- Se actualizaron `ARCHITECTURE.md`, `PROJECT_CONTEXT.md`, `PROJECT_INDEX.md`, `ARCHITECTURE_CHANGELOG.md` y `docs/ENTORNOS_DESARROLLO_PRODUCCION.md` para reflejar la topología real.
- No se desplegó Produccion, no se modificó `main`, no se escribieron datos productivos y no se expusieron secretos.

## 2026-09-15 — Reconciliación de cola N8.5/N8.6

**Responsable:** Codex, ejecución autorizada por Javier Mejía en `Desarrollo`.

Se registró el receipt de cierre de los bloqueos externos sustituidos por evidencia SMTP existente y el modo WhatsApp `USER_INITIATED_HANDOFF`. La validación visual CUA queda pendiente por ausencia de sesión; no se fabricó evidencia. No se modificó `N8.7.A`, su lease ni las automatizaciones.

## 2026-09-15 — WhatsApp — fallback visible ante popup bloqueado

El flujo de factura conserva el enlace `wa.me` seguro y lo muestra como fallback cuando el navegador bloquea la apertura automática.

## 2026-09-15 — WhatsApp — erratum de contrato de auditoría

Se corrigió el doble enmascarado: factura envía el teléfono normalizado de forma transitoria y el backend aplica la máscara una sola vez antes de persistir. Se añadió manejo visible de error de auditoría y cobertura frontend/backend del contrato. No se enviaron mensajes reales ni se tocaron `N8.7.A` o las automatizaciones.

El receipt correctivo quedó fijado al HEAD funcional exacto de la corrección.

## 2026-09-15 — WhatsApp — UX veraz del popup y auditoría desacoplada

Se aisló el booleano real de `window.open`: el fallback depende solo de la apertura aceptada y el error de auditoría usa wording neutral. Se añadió el contrato ejecutable Factura→Service con casos popup aceptado, bloqueado y auditoría fallida.

Se añadió el addendum y receipt final de N8.6 con el contrato UX veraz, sin capturas fabricadas y sin interferencia con `N8.7.A`.

## 2026-09-15 — WhatsApp — handoff canónico sin API de proveedor

**Responsable:** Codex, ejecución autorizada por Javier Mejía en `Desarrollo`.

Se consolidó `WhatsAppShareService` y `WhatsAppSharePolicy` para normalización multi-país con fallback Honduras, enlaces `wa.me` URL-encoded, apertura oficial y destinatarios enmascarados. El historial acepta únicamente estados de handoff y rechaza afirmaciones de entrega/lectura. Facturas, catálogo público y checkout consumen la misma política. No se añadieron APIs pagadas, tokens, secretos, envíos reales ni cambios al scheduler.

Validación: 24 pruebas backend dirigidas, 4 pruebas Vitest de política frontend, `npm run lint`, `npm run build:prod` y `git diff --check` superados.

## 2026-09-15 — Responsive — paneles globales de select y autocomplete

**Responsable:** Codex, ejecución autorizada por Javier Mejía en `Desarrollo`.

Los paneles de Angular Material para selects y autocompletado ahora tienen un ancho mínimo legible y un máximo ligado al viewport, con opciones de altura flexible y texto envolvente. El gate responsive abre un select real por ruta cuando existe y falla si el panel u opciones quedan fuera, estrechos o recortados.

## 2026-09-15 — Responsive — corrección de campos de detalle en Nueva venta

**Responsable:** Codex, ejecución autorizada por Javier Mejía en `Desarrollo`.

La fila de detalle móvil de `Ventas/Nueva venta` tenía una columna automática que comprimía `Cantidad` y dejaba el outline del campo junto a `Precio unitario`. La grilla ahora usa dos columnas fluidas reales para ambos controles, manteniendo producto/variante/acción y evitando colisiones. El gate global añade detección de campos de detalle demasiado estrechos y solapamientos entre outlines.

Validación: build productivo y lint del frontend superados antes del cambio; la verificación remota se repetirá sobre la ruta desplegada sin crear una venta ni enviar correo.

## 2026-09-15 — Responsive global — foundation y gate automático

**Responsable:** Codex, ejecución autorizada por Javier Mejía en `Desarrollo`.

Se consolidó la base responsive global del frontend: shell con drawer móvil, topbar y contenedores fluidos, controles y medios que respetan su contenedor, diálogos limitados al viewport y primitivas compartidas para grids, filtros, detalle, KPI, acciones y tablas. Se eliminó la dependencia de `overflow-x: hidden` en `body`. Se añadió `docs/FRONTEND_RESPONSIVE_STANDARD.md` y el gate `npm run test:responsive`, que compara el inventario de rutas fuente y recorre 320/360/375/390/412/430/768/1024/1440 px comprobando overflow, clipping, tablas, objetivos táctiles, accesibilidad y drawer.

Validación real: `npm run lint`, `npm run build:prod` y `git diff --check` superados. El build conserva únicamente advertencias Angular preexistentes de proyección/imports; no hubo cambios de backend, secretos, Producción, `main`, WhatsApp ni flujos transaccionales.

## 2026-09-15 — UX — eliminación de diálogos nativos en flujos empresariales

**Responsable:** ChatGPT/VAEP, ejecución autorizada por Javier Mejía en `Desarrollo`.

Se reemplazaron todos los `window.confirm`, `confirm`, `window.prompt` y `prompt` de `frontend/src/app` por el `AppAlertService` compartido. Las acciones de compras, ventas, facturación, pagos, inventario, productos, solicitudes, órdenes, preparaciones, cargas masivas y administración usan ahora modales propios con texto semántico, motivos obligatorios cuando aplican, cancelación accesible y estado de confirmación sin ventanas nativas del navegador.

Validación real: barrido `rg` sin diálogos nativos de producción; `npm run lint` y `npm run build:prod` superados. El build backend Release superó 0 advertencias/0 errores; la ejecución local de pruebas .NET quedó impedida por la directiva de Control de aplicaciones del host al cargar `Solqaryn.Tests.dll`, sin resultado PASS inventado.

## 2026-09-15 — N8.1.G — evidencia de UAT delegado y reconciliación pendiente

**Responsable:** ChatGPT/VAEP, ejecución autorizada por Javier Mejía en `Desarrollo`.

Se registró el intento de UAT delegado sobre `c3e2b5fc34d2f51738a33a04d092104ecf2bf1c7`: la sesión CUA disponible quedó en `/login` después del redeploy y no se fabricaron capturas, DOM ni aceptación humana. La evidencia conserva el PASS existente de Inventario, los PASS técnicos previos de los otros grupos, el alcance interno provider-neutral/fail-closed de Integrations y el barrido de diálogos nativos en cero. La reconciliación del Sheet queda explícitamente pendiente por falta de acceso conectado.

## 2026-09-15 — N8.1.G — validación técnica de los seis grupos restantes

**Responsable:** Codex, ejecución autorizada por Javier Mejía en `Desarrollo`.

Se validaron Compras, Ventas/Facturación, Tesorería/Finanzas, BI/Reportes y Multiempresa/RBAC con UAT autenticado, contratos dirigidos y regresión completa. La evidencia consolidada está en `vaep/evidence/reviews/N8.1.G_CODEX_TECHNICAL_UAT_REMAINING_6_20260915T074247Z.json`; la preparación de sign-off está en `vaep/evidence/reviews/N8.1.G_READY_FOR_FINAL_HUMAN_SIGNOFF_20260915T074247Z.json`.

Se corrigió el defecto causal `N8.1.G-FIN-001`: al registrar o anular un pago de factura, `FacturaService` ahora sincroniza el estado del movimiento financiero automático de la Venta y cuenta con regresión dirigida. No se modificaron `main`, Producción, secretos, dominios ni PR #2.

Validación real: build backend 0 advertencias/0 errores; 2269 pruebas backend no-Integration superadas; filtros dirigidos 294/82/131 superados; lint y build productivo frontend superados. Integrations queda `BLOCKED_EXTERNAL` sólo por falta de sandbox/proveedores externos; no se inventaron credenciales. Falta la aceptación humana autorizada para los seis grupos restantes, por lo que N8.1.G no se marca `LISTO_REAL`.

## 2026-09-11 — ERP-N6.2.H — recuperación documental y REVIEW_FIRST

**Responsable:** CHATGPT_VAEP como first detector/correction owner.

El ATTEMPT1 J1 terminó con patch sobre `CHANGELOG_AI.md` y `TASKS.md`, pero el contrato terminal falló: `TASKS.md` es control-plane prohibido para Jules y faltaban las evidencias de self-review/tests. REVIEW_FIRST descartó íntegramente el fragmento de `TASKS.md`, corrigió la narrativa de cierre y materializó la reconciliación canónica en `docs/N6.2_TENANT_AWARE_DATA_MODEL_PREFLIGHT.md`.

La evidencia A–G quedó enlazada de forma source-backed; N6.3 (Empresa→sucursales), N6.4 (membresías/roles por empresa) y N6.5 (aislamiento anti-leakage completo) permanecen explícitamente fuera de N6.2. No hubo cambio de comportamiento productivo, esquema, migración, API, frontend, workflow, secrets, `main`, Producción ni PR #2.

Estado: `REVIEW_FIRST_ACCEPTED_AFTER_CONTROLLER_DIRECT_FIX__P0_0__P1_0__PENDING_EXACT_HEAD_GATES__NOT_LISTO_REAL`. No se consumió R2; R3 permanece prohibido.

## 2026-09-08 — Chat B + reconciliación canónica de estado VAEP

**Responsable:** Codex, por orden explícita del propietario.

**Equipo:** se incorporó Chat B (ChatGPT Business) como peer controller/QA full-access dentro de `Desarrollo`, con REVIEW_FIRST, QA_TAKEOVER, corrección, integración, CI, certificación, rollup y failover bajo `docs/VAEP_AUTHORITY.md`. No es una quinta lane Jules y no puede saltar gates, crear R3+, tocar `main`/Producción/secretos ni falsear evidencia.

**Repositorio:** se actualizó el MAESTRO y los documentos colaborativos para que Chat B consuma la misma autoridad única. El remoto avanzó concurrentemente a `0d0ba5e9` con `dispatch-admission=CLOSED`; se corrigió a `FROZEN`, el único valor contractual válido para contención por REVIEW_FIRST pendiente, conservando `allowExistingActiveSessions=true`.

**Drive compartido:** se reconciliaron CONFIG, DASHBOARD, COLA, PLAN_MAESTRO, TAREAS_PROGRAMADAS, EJECUCION_MANUAL y LEYENDA. El estado vigente es `CURRENT_PARENT=N4.11.H`, `FUNCTIONAL_HEAD=b30b949e`, `HEAD=5ed9b3d6` como descendiente de control-plane, `dispatch-admission=FROZEN`, `N4.11.B–G=LISTO_REAL` con evidencia, y `N4.11.H=VALIDANDO` por REVIEW_FIRST/QA_TAKEOVER pendiente. No se despachó ningún Jules manual ni se fabricó backlog.

**Documento rector compartido:** se añadió un bloque de vigencia al inicio de `Plan Maestro ERP V5 — Solqaryn — FUENTE RECTORA VAEP`, con chip nativo de fecha y precedencia explícita del MAESTRO versionado sobre contenido histórico.

**Verificación:** readback de todas las celdas objetivo, readback nativo del documento (incluido `dateElement` y estilos), `git diff --check`, rama default GitHub `Desarrollo`, `main` sin cambios. Pendiente externo: no se concedió una cuenta GitHub/Drive adicional porque no existe un email/login verificable de Chat B; el rol operativo quedó registrado sin inventar credenciales.

## 2026-08-25 — ERP-N3.5 Venta/factura — CIERRE FORMAL

**Responsable:** ChatGPT/VAEP v3.25 Closure Governor.

**Objetivo/alcance:** registrar el cierre formal del bloque N3.5 (Venta y Factura), confirmando que dichas entidades conservan su autoridad existente y que `PedidoVenta` (N3.2) permanece estrictamente desacoplado, sin introducir una conversión directa (`PedidoVenta` ↔ `Venta`), FKs cross-document, idempotencia, ni orquestación nueva.

**Evidencia:** las microtareas fueron concluidas y validadas según su dominio:
- N3.5.A #516 `LISTO`
- N3.5.B #517 `LISTO` N/A domain grounded
- N3.5.C #518 `LISTO` N/A persistence grounded
- N3.5.D #519 `LISTO` N/A Application/API grounded
- N3.5.E #520 `LISTO` N/A frontend grounded
- N3.5.F #521 `LISTO` N/A security/audit grounded
- N3.5.G #522 `LISTO` N/A QA/CI grounded

**Certificación funcional:** el control reporta la certificación `56a422f0bf0e882fa6c9d800061154031f701091`, TASKS `a298bf537c98da8a9f1e31f4a2d8f8e6cc50e572`, con baseline funcional en `a167434880eab07c3b08ca651ae9309da964c23b` tras M13 #32809392404 en `SUCCESS`. P0/P1 atribuibles conocidos a la fecha: 0.

## 2026-08-24 — Codex — ejecutor Jules v3.25

- Se alineó `.github/scripts/vaep-jules-worker-v320.sh` con semántica v3.25 conservando el nombre por compatibilidad con cuatro workflows.
- Los lanes Jules A/B/C/D ahora identifican v3.25; se preservaron v4.6, ATTEMPT1+R2, R3 prohibido, QA takeover, doble revisión, artefactos/Issues, `Desarrollo` y prohibición de push/merge/deploy Jules.
- Se retiró del ejecutor el sprint vencido y se añadió cierre por padre con checkpoints `:00/:15/:30/:45/:55`.
- Se añadió `--static-self-test` para validar guardrails sin red, secretos, sesión ni attempt. La prueba de integración real no se ejecutó porque un dispatch crea sesión y consume attempt.

## 2026-08-24 — Codex — autoridad VAEP/Jules v3.25

- Se unificó la gobernanza documental en `V3.25_CURRENT`, cierre por padre y checkpoints `:00/:15/:30/:45/:55`, preservando control-plane global v4.6.
- v3.20/v3.21 quedaron marcados como historia; continúan ATTEMPT1+R2, R3 prohibido, QA takeover, HEAD freeze, evidencia causal y protección de `Desarrollo`/main/Producción.
- Se aclaró que el Sheet registra/describe automatizaciones y el sistema de tareas ejecuta; no se modificó ni afirmó ejecución de una automatización real.
- Cambio exclusivamente documental; sin código, workflows, infraestructura, secretos ni Sheet.

## 2026-08-24 — Codex — reconciliación documental ChatGPT/VAEP

- Se amplió `docs/CONTEXTO_CHATGPT_VAEP.md` con roles, ciclo automático, mutex/actividad/CI/handoff, fuentes de verdad, consulta selectiva, estado local observable y mejoras priorizadas.
- Se documentó fail-closed el conflicto Jules v3.20/v3.21 en `docs/VAEP_AUTHORITY.md`, `PLAN_EJECUCION_AUTONOMA.md`, `PROJECT_CONTEXT.md` y `TASKS.md` sin reescribir el historial.
- No se consultó Sheet/Drive ni se afirmó estado externo fresco; no se modificaron código, workflows o infraestructura.

## 2026-08-24 — Codex — guía operativa por dominio

- Se amplió `PROJECT_INDEX.md` con mapa por capas, matriz por dominio, flujos transversales y límites de inspección para cambios locales.
- Se corrigió el mapa de datos para reflejar las dos ubicaciones históricas reales de migraciones.
- Se registró el cambio en `ARCHITECTURE_CHANGELOG.md`; no se modificó código ni configuración.

## 2026-08-24 — Codex — contexto ChatGPT/VAEP

- Se incorporó `docs/CONTEXTO_CHATGPT_VAEP.md` como referencia histórica/operativa de Solqaryn.
- Se enlazó desde `PROJECT_INDEX.md` y se registró en `ARCHITECTURE_CHANGELOG.md`.
- Se documentaron VAEP, validación causal, cadena compras-recepciones-reservas-facturación, no duplicación y consulta selectiva sin presentarlos como estado no verificado.
- Cambio exclusivamente documental; no se ejecutó ni modificó código de producción.

## 2026-08-24 — Codex — mapa técnico persistente

- Se consolidó `PROJECT_INDEX.md` como mapa rápido con índice de decisión, puntos de entrada y comandos verificados.
- Se creó `ARCHITECTURE_CHANGELOG.md` y se enlazó la convención de mantenimiento desde el contexto y la arquitectura canónicos.
- Se alineó la declaración `PROJECT_ID: SOLQARYN` con el guard obligatorio de inicio de sesión.
- Cambio exclusivamente documental; no se ejecutó ni modificó código de producción.

## 2026-08-23 — ERP-N3.1 Cotizaciones — CIERRE FORMAL

**Responsable:** ChatGPT/VAEP v3.21 mediante PARENT-CLOSURE-FIRST y QA takeover documental.

**Objetivo/alcance:** cerrar N3.1.A-H con Cotización como documento comercial previo al Pedido de Venta, snapshots de cliente/producto y lifecycle `Borrador → Enviada → Aceptada/Rechazada → Convertida`, sin adelantar el dominio de Pedidos N3.2.

**Validación final:** baseline funcional `d4d296e229d266a1442de3bc4e07b03bfab35a9f`; HEAD de control `eea11fb0e3ba1f1afc3010362f87caecf89f6c22` con Development `#32687639976`, Acceptance `#32687639981`, Fase 8 `#32687640010`, M13 `#32687640016` y Recovery MySQL `#32687640017` en SUCCESS. El único delta entre ambos era un manifest evidence-only de cierre Jules A, sin cambio funcional. P0/P1 bloqueantes conocidos=0.

**Cierre documental/control:** certificación canónica `docs/CERTIFICACION_N3_1_COTIZACIONES.md`; `TASKS.md` reconciliado. El dispatch Jules A de cierre no produjo sesión ni actividad útil dentro del umbral y quedó `BOOTSTRAP_STALLED_NO_SESSION / ACTIVE=NO`, sin consumir ATTEMPT1 funcional; ChatGPT/VAEP cerró H directamente. Parent40 avanza `29→30/40`, GAP `11→10`, y el selector fail-closed promueve inmediatamente `N3.2.A — Pedidos de venta / Auditoría y preflight`.

## 2026-08-23 — ERP-N2.9 Evaluación de proveedores — CIERRE FORMAL

**Responsable:** ChatGPT/VAEP v3.21 mediante QA takeover y cierre canónico parent-first.

**Objetivo/alcance:** N2.9.A-H completadas; la evaluación factual de proveedores cubre tiempos/cumplimiento de entrega, diferencias, devoluciones, costos y calidad sin inventar fórmulas de scoring, pesos, umbrales ni rankings.

**Validación final:** paquete canónico `af3439ea00a7ff09333926e79f5668e0f2c8e1e9`; baseline de control `13f59ee7c6272bb3a8d02e293c20f7b645bb7017` con Development #32634001803 SUCCESS, Acceptance #32634001793 SUCCESS, Fase8 #32634001797 SUCCESS, M13 #32634001794 SUCCESS y Recovery MySQL #32634001786 SUCCESS. P0/P1 bloqueantes conocidos=0.

**Control:** Parent40 avanza 21→22/40 y GAP 19→18 únicamente tras review/CI causal de este cierre de changelog; `GATE-N2` es el siguiente padre dependency-valid y ERP-N3 no puede promoverse antes de `GATE-N2=LISTO`. Jules A agotó ATTEMPT2/2 en el cierre documental y quedó liberado; R3+ permanece prohibido.

## 2026-08-22 — ERP-N2.8 Cuentas por pagar — CIERRE FORMAL

**Responsable:** ChatGPT/VAEP v3.21 mediante cierre canónico parent-first; artifacts Jules se usaron únicamente como evidencia revisada cuando correspondió y no sustituyen el DoD causal.

**Objetivo/alcance:** cerrar formalmente ERP-N2.8 Cuentas por pagar con N2.8.A–H completadas: preflight, dominio/contratos, persistencia y migración, Application/API, frontend/UX, RBAC/auditoría/seguridad/observabilidad, QA/regresión/CI y documentación/certificación. El alcance cubre obligación financiera por factura de proveedor, contado/crédito, vencimientos, pagos parciales, anticipos, retenciones y saldo, sin adelantar evaluación de proveedores de N2.9.

**Validación final:** HEAD documental `360ff3303af3587810c21e32ceeeb88fcc9e51d3`; Development #32607259773 SUCCESS; Acceptance #32607259650 SUCCESS; Fase8 #32607259716 SUCCESS; M13 #32607259703 SUCCESS; Recovery MySQL #32607259695 SUCCESS. `TASKS.md` ya declara ERP-N2.8 cerrado y la bitácora queda ahora reconciliada. P0/P1 bloqueantes conocidos=0.

**Control:** `N2.8.A–H` quedan formalmente cerrados. Parent40 avanza 13→14/40, GAP 27→26. La siguiente MICROTAREA dependency-valid es `N2.9.A — Evaluación de proveedores — Auditoría y preflight`; reutilizar su evidencia histórica existente y no repetir preflight redundante. `main`, Producción, PR #2 merge/auto-merge, ramas nuevas, force-push, secretos y despliegues permanecen intactos.

## 2026-08-22 — ERP-N2.7 NotaCreditoProveedor — CIERRE FORMAL

**Responsable:** ChatGPT/VAEP mediante QA takeover v3.21, reutilizando únicamente el contenido documental validado del artifact Jules D #348; el resultado Jules no se integró por incumplir el gate de self-review independiente.

**Objetivo/alcance:** cierre formal canónico de ERP-N2.7 Nota de crédito de proveedor, con N2.7.A-H completadas, sin adelantar trabajo de N2.8.

**Validación:** baseline funcional `42f83b365392f45de39bd0e0ca4fa0638dd0eb10` y paquete documental `c466ec3099c2a498c2353af82b99ce0be9d46e29`; Development #32574284665, Acceptance #32574284640, Fase8 #32574284638 y M13 #32574284639 SUCCESS. El HEAD de control-plane `e72f709bdade0dbec6198fa483aaa213a5e6c66d` también terminó Development #32576077991, Acceptance #32576077933, Fase8 #32576077965, M13 #32576077925 y recovery MySQL #32576077970 en SUCCESS. P0/P1 bloqueantes conocidos=0.

## 2026-08-19 — ERP-N2.2 OrdenCompra — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive, con exclusión total del scope reservado de Jules.

**Objetivo/alcance:** cerrar formalmente ERP-N2.2 después de completar preflight, dominio/contratos, persistencia/migración, aplicación/API, frontend/UX, RBAC/auditoría/seguridad/observabilidad, QA/regresión/CI y documentación. `OrdenCompra` queda como documento empresarial independiente que representa el compromiso comercial con el proveedor; no representa recepción física, stock, Kardex, costeo, factura de proveedor ni obligación financiera.

**Resultado funcional:** lifecycle `Borrador → PendienteAprobacion → Aprobada` con cancelación controlada, moneda ISO, proveedor/snapshots, detalles, descuentos/impuestos, fecha esperada, observaciones e idempotencia durable `Idempotency-Key + SHA-256`. La API `/ordenes-compra` exige autenticación y grants relacionales `Compras:Ver/Crear/Editar/Confirmar/Aprobar/Anular`. Frontend cubre listado, creación/edición, detalle, aprobación/cancelación, errores fail-closed, paginación y performance. La migración canónica `20260818204700_N2_2_OrdenCompraPersistencia` crea tablas dedicadas con guards y rollback bloqueado cuando existen documentos.

**Trazabilidad:** A `73ef31c49f08c8bff9732978ffc86dbe74e0a116`; B `88047cde42929c1b2dcd8faf77da1c6543a2f2a9` + fix `f17983ef49bb8f5032e6fb328564f36c02f103b9`; C `adff03723b4336b570328179e468e8470e611b95`; D hasta `a5340f991b0f93438ac184afeac41cc9ed82a756`; E.1 `26a7eada...`, E.2 `9ede060d...`, E.3 `f9000061...`; F hasta `1eb26cf60a3d4e1e37f9c89b60929f432de3c1ac`; G.1 `23fa5ac6...`; G.2/G.3 baseline `b4d477e2de25077c459d02b479968c93c93bc910`. Paquete H: `e59b7bb59cf51b99ae14665cee18c1fe70220bbb`, `6d53ae43f4a9fa54b41f1981704cb03c427d2a74`, `74ebbe969b22b9d8e0130ea733ae0c9fa9f18891`, `821431340afceb70b93f5431a719b8adc2ab6717` y candidato documental `736683476714300d6bf29406967e17c312abac7d`; `TASKS.md` reconciliado en `da05e6625ec6caf98f4e7e4a6dc4912d284dd805`.

**Validación:** baseline funcional `b4d477e2...`: Development `32218997006`, Acceptance `32218996971`, Fase 8 `32218996994`, M10 `32218996973` y M13 `32218996978` SUCCESS. Persistencia N2.2.C: M12 `32184108722` SUCCESS en MySQL 8.4. Sobre el candidato documental `73668347...`, Development `32227719896` terminó SUCCESS completo —backend/unitarias, frontend, higiene, Docker, aplicación de migraciones, integración MySQL y SQL forward— y recovery MySQL `32227719707` SUCCESS; el diff H es exclusivamente documental/colaborativo y no modifica aplicación ni workflows.

**Documentación:** `docs/ERP_N2_2_ORDEN_COMPRA.md`, `docs/RUNBOOK_N2_2_ORDEN_COMPRA.md`, `docs/ADR_N2_2_ORDEN_COMPRA_AUTORIDAD_DOCUMENTAL.md`, `docs/OPENAPI_N2_2_ORDEN_COMPRA.md` y `docs/CERTIFICACION_N2_2_ORDEN_COMPRA.md`, más el preflight histórico `docs/ERP_N2_2_ORDEN_COMPRA_PREFLIGHT.md`.

**Control:** `N2.2.A–H` quedan formalmente cerrados. El siguiente foco FINISH_FIRST elegible es `N2.3.A — Recepción de mercancía — Auditoría y preflight`, donde recién debe materializarse el incremento de stock por recepción real. El scope Jules no fue editado ni integrado. `main`, Producción, merge/auto-merge del PR #2, ramas nuevas, force-push, secretos e infraestructura productiva permanecen intactos.

## 2026-08-18 — ERP-N2.1 SolicitudCompra — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive, preservando cambios concurrentes publicados en `Desarrollo`.

**Objetivo/alcance:** cerrar formalmente ERP-N2.1 después de completar preflight, dominio y contratos, persistencia/migración, Application/API, frontend/UX, RBAC/auditoría/seguridad/observabilidad, QA/regresión y documentación. `SolicitudCompra` queda como documento empresarial independiente con lifecycle `Borrador → Solicitada → Aprobada/Rechazada` y sin efectos de stock, Kardex, costeo o finanzas.

**Decisiones y seguridad:** una solicitud aprobada continúa siendo documental y no crea implícitamente una `Compra`; la materialización posterior pertenece a `N2.2` y siguientes. Update/Enviar/Aprobar/Rechazar se serializan con transacción y lock pesimista. La autorización usa grants relacionales sin bypass efectivo por `EsAdministrador`. Crear/Editar/Enviar/Aprobar/Rechazar registran auditoría estricta dentro de la unidad transaccional, sin copiar notas u observaciones sensibles. Correlation ID, health/readiness y configuración segura reutilizan la infraestructura transversal existente.

**Trazabilidad:** D `01770a23cbf9a50e7d21a0a7913f32e31ce6070a`; E.1 `f52f9f746427d18675073ba769c2a78c2f13d900`; E.2 `112ef6b8660fb12c80d6981eac81b55f6c32bdec`; E.3 hasta `07275df6af316aff83f250c6cf9d9b1b1ad335d3`; F.1 `d3f039efafe0bf7ccfd487ba4ca7c66e07625fc3`; F.2 `adea50ac65bacceff42cd23c110afea77817ca44`; F.3 `12b26459004dc01a17b5b2af4602dbb906470bae`; G baseline `a1a6f699cbad0186d0e0d7d7ac7f366c51009f7c`; paquete documental H `d8760bff2e9322e6f09612f64a89c2de888aa9d8`.

**Validación:** baseline funcional `a1a6f699cbad0186d0e0d7d7ac7f366c51009f7c`; Development #32172981351 SUCCESS incluido frontend, backend y MySQL con 994/994 pruebas backend. Sobre el commit documental `d8760bff2e9322e6f09612f64a89c2de888aa9d8`, Development `32177459360`, Fase 8 `32177459423`, M10 `32177459382`, M12 `32177459385`, backup operativo `32177459445`, backup/restauración `32177459455` y recuperación MySQL `32177459334` terminaron SUCCESS; los workflows históricos ERP-N0 que fallen por incompatibilidades de su propio alcance no se usan como gate causal de N2.1.

**Documentación/riesgos residuales:** fuentes canónicas `docs/ADR_N2_1_SOLICITUD_COMPRA_INDEPENDIENTE.md`, `docs/ERP_N2_1_SOLICITUD_COMPRA.md` y `docs/RUNBOOK_N2_1_SOLICITUD_COMPRA.md`. Riesgo residual deliberado: la conversión `SolicitudCompra → OrdenCompra/Compra`, impuestos/moneda/condiciones y recepción pertenecen a microtareas posteriores; no se adelantan en N2.1. No quedan bypasses temporales conocidos atribuibles a N2.1.

**Control:** `N2.1.A–H` quedan formalmente cerrados tras reconciliar `TASKS.md`, esta bitácora y VAEP. El siguiente foco FINISH_FIRST elegible es `N2.2.A — Orden de compra — Auditoría y preflight`. `main`, Producción, merge/auto-merge del PR #2, secretos, infraestructura productiva, force-push y ramas nuevas permanecen intactos.

## 2026-08-17 — ERP-N1.9 Series, lotes y vencimientos — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

**Objetivo/alcance:** cerrar formalmente ERP-N1.9 después de completar auditoría/preflight, dominio y contratos, persistencia/migración, aplicación/API, frontend/UX, RBAC/auditoría/seguridad, QA/regresión y documentación. La capacidad queda deliberadamente opt-in por `ProductoVariante`: Lote, Número de Serie y Fecha de Vencimiento no se imponen a todos los productos y `ExistenciaVariante` continúa siendo la única autoridad cuantitativa del stock físico.

**Resultado funcional:** `LoteInventario` y `SerieInventario` funcionan como subledger de identidad trazable, no como una segunda autoridad de cantidad. La persistencia es aditiva y preserva históricos con flags desactivados por defecto, sin inventar backfill de lotes/series/vencimientos. La API y UI permiten configurar la política por variante, capturar/listar lotes y series y controlar vencimientos. La seguridad usa RBAC relacional de `MovimientosInventario`, auditoría estricta transaccional e idempotente, correlation saneado y contratos HTTP protegidos. Durante QA se detectó y corrigió causalmente la falta de límite de longitud de `NumeroSerie`; el dominio ahora rechaza valores de más de 120 caracteres antes de mutar estado.

**Documentación:** paquete canónico compuesto por `docs/ERP_N1_9_SERIES_LOTES_VENCIMIENTOS.md`, `docs/ADR_N1_9_AUTORIDAD_TRAZABILIDAD.md`, `docs/ERD_N1_9_TRAZABILIDAD.md`, `docs/RUNBOOK_N1_9_TRAZABILIDAD.md`, `docs/OPENAPI_N1_9_TRAZABILIDAD.md`, `docs/RUNBOOK_N1_9_MIGRACION.md` y `docs/CERTIFICACION_N1_9_TRAZABILIDAD.md`. El baseline funcional de QA es `4b5a5c9a8b495fcef62464bf50010ac69117fe48`; el baseline documental certificable es `7bc4b7935cc92e15d24f90a79f3915ab14e2d243`.

**Validación final de `7bc4b793...`:** Development `32089179243` SUCCESS; Acceptance `32089179228` SUCCESS incluido Playwright integral + SMTP/PDF; Fase8 `32089179144` SUCCESS; M10 `32089179156` SUCCESS; M13 `32089179175` SUCCESS completo, incluido Backend/MySQL/migraciones/upgrade, Frontend, Docker/backup, Secretos/Higiene, Runtime/Playwright, SMTP/PDF/logs y `Dictamen automatizado M13` SUCCESS exigiendo todos los gates verdes.

**Control:** `TASKS.md` queda reconciliado con VAEP, incluyendo el desfase histórico de `N1.7.H` que ya estaba `LISTO` en el tablero. `N1.9.A–H` quedan formalmente cerrados. `main`, Producción, merge/auto-merge del PR #2, secretos, infraestructura productiva, force-push y ramas nuevas permanecen intactos.

## 2026-08-17 — ERP-N1.8 Reservas de inventario — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

**Objetivo/alcance:** cerrar formalmente ERP-N1.8 después de completar auditoría/preflight, dominio y contratos, persistencia/migración, backend/API, frontend/UX, RBAC, auditoría crítica, seguridad/observabilidad, regresión integral y documentación. El objetivo empresarial queda cumplido: diferenciar stock físico, reservado y disponible e impedir overselling sin crear una segunda autoridad cuantitativa.

**Resultado funcional:** `ExistenciaVariante` permanece como autoridad única de cantidad por clave física `ProductoVarianteId + AlmacenId + UbicacionAlmacenId`; `ReservaInventario` y sus detalles explican el compromiso reservado y su lifecycle. Activar/consumir/liberar/expirar/cancelar opera bajo lock pesimista y transacción; la auditoría crítica es obligatoria y usa `RegistrarEstrictoAsync` dentro de `IUnitOfWork`, por lo que una mutación no puede confirmarse si su evidencia falla. Frontend y API conservan RBAC relacional, CorrelationId saneado, estados físico/reservado/disponible y protección de rutas/acciones.

**Documentación:** `docs/ERP_N1_8_RESERVAS.md`, `docs/ADR_N1_8_RESERVAS_STOCK_RESERVADO_Y_OVERSELLING.md`, `docs/RUNBOOK_N1_8_RESERVAS.md` y `docs/ERD_N1_8_RESERVAS.md`, publicados en `11865b97f00f662728f7fe85a7466af89a9084df`. El baseline funcional previo es `95baf2763b912e1015a3bdd25a37aca649e34c37`.

**Validación final del HEAD documental `11865b97...`:** Development `32037186026` SUCCESS 5/5; Acceptance `32037186011` SUCCESS incluido Playwright/SMTP/PDF; Fase8 `32037186066` SUCCESS; M10 `32037186054` SUCCESS; M13 `32037186024` SUCCESS completo, incluido backend/MySQL/migraciones/upgrade histórico, frontend, Docker/backup, secretos/dependencias, seguridad HTTP, runtime/Playwright, SMTP/PDF/logs y `Dictamen automatizado M13` SUCCESS exigiendo todos los gates verdes.

**Control:** `TASKS.md` queda reconciliado con VAEP, incluyendo el desfase histórico de `N1.7.H` que ya estaba `LISTO` en el tablero. `N1.8.A–H` quedan formalmente cerrados. `main`, Producción, merge/auto-merge del PR #2, secretos, infraestructura productiva, force-push y ramas nuevas permanecen intactos. Siguiente foco FINISH_FIRST: `N1.9.A — Series, lotes y vencimientos — Auditoría y preflight`.

## 2026-08-14 — ERP-N1.3 Ubicaciones internas de almacén — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

**Objetivo/alcance:** completar `UbicacionAlmacen` como topología física jerárquica interna de cada Almacén para pasillos, estantes, racks, secciones, bins y otras ubicaciones, sin introducir todavía existencias, cantidades ni semántica WMS avanzada.

**Resultado funcional:** `UbicacionAlmacen.AlmacenId` es la única relación organizacional persistida; `SucursalId` y `EmpresaId` se derivan transitivamente. Padre opcional restringido al mismo Almacén, prevención de ciclos directos/indirectos, protección de descendientes al mover/desactivar/eliminar, código operativo único por Almacén, soft-delete y estados idempotentes. MySQL 8.4 conserva la invariante anti-self-parent mediante triggers porque un CHECK no puede referenciar el `Id AUTO_INCREMENT`. API `/ubicaciones-almacen` soporta búsqueda, Almacén, padre/raíz, tipo, estado, paginación, CRUD y operaciones de estado. Frontend incorpora listado responsive, filtros server-side, formulario jerárquico, selectores de Almacén/padre, rutas y menú protegidos por RBAC.

**RBAC/auditoría/seguridad:** módulo `UbicacionesAlmacen`, permisos `Ver/Crear/Editar/Activar/Desactivar/EliminarLogico`, auditoría de mutaciones con referencia de entidad y pruebas que congelan los 9 contratos de autorización. Se reutilizan Correlation ID, ProblemDetails, headers de seguridad y health/readiness globales. N1.3 no contiene campos de stock; `ExistenciaVariante` queda reservado para ERP-N1.4.

**Trazabilidad:** D backend `4d2cc04b363df602f6de97b7f5ea876ea35a6196`, run `31843085895`, job `94903923345` SUCCESS; E frontend `91f878ef3cbc56219b637e9b62c99bdd1109a9df`, run `31846161956`, job `94912936660` SUCCESS; F/G baseline `4a6be38683f03fc2076f18a71115480c930ba79b`.

**QA real:** run agregado `31846485117` SUCCESS: higiene `94913888918`, Backend Release/unitarias `94913888850`, frontend producción `94913888865`, Docker `94913888808` y MySQL 8.4/integración `94913888844`; el job MySQL aplicó migraciones actuales, ejecutó `Category=Integration`, verificó snapshot/variantes/cargas y generó SQL forward sin regresiones.

**Documentación/control:** preflight `docs/ERP_N1_3_UBICACIONES_PREFLIGHT.md`; cierre canónico `docs/ERP_N1_3_UBICACIONES_ALMACEN.md`; TASKS, CHANGELOG y tablero VAEP reconciliados preservando historial. `main`, Producción, merge/auto-merge del PR #2, secretos y force-push permanecen intactos. **ERP-N1.3 queda formalmente cerrado** y el siguiente foco FINISH_FIRST es `N1.4.A — ExistenciaVariante — Preflight y diseño`.

## 2026-08-14 — ERP-N1.2 Almacenes empresariales — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

**Objetivo/alcance:** implementar y certificar `Almacen` como maestro hijo obligatorio de `Sucursal`, con tipos Tienda/Bodega/Transito/Devolucion/Cuarentena, persistencia MySQL, API, RBAC relacional, auditoría, observabilidad, frontend responsive/accesible y QA dedicado, sin adelantar ubicaciones N1.3, existencias por almacén N1.4 ni multiempresa N6.

**Resultado funcional:** `Almacen.SucursalId` queda como única jerarquía organizacional de N1.2; una introducción concurrente de `EmpresaId` duplicada fue detectada y corregida forward-only en `85f2b845ca60d8e797425bd5b0f9a7d597a6cfa8`. Persistencia final con FK Restrict a `Sucursales`, código activo único, índices/checks y rollback fail-closed. API `/almacenes` soporta CRUD, filtros/paginación, catálogo de tipos, activos y operaciones de estado. Crear/mover/reactivar falla cerrado si la Sucursal no existe o está inactiva. RBAC `Almacenes=29`, auditoría `Entidad=Almacen` y métrica P50/P95 sin término/PII quedan integrados. Frontend ofrece lista server-side, selector Sucursal/tipo, rutas y menú protegidos, tabla/cards responsive y formulario sin stock ni EmpresaId.

**Trazabilidad:** B final `85f2b845ca60d8e797425bd5b0f9a7d597a6cfa8`; C `bebafe3abb2ddc66448c805b107f8d1f8ee3f3e9`; D `5a97bf3844069a565e1aecf39e4b8001c10f386b`; E `3a1b8004f2120c4be6459bb46fd120eff8704fe9`; F `30c7e9ff1dedf69eb860916b92b1d5bee0941084`; G base `f6f51bb6d0d5d1910e9561de30d934b30fa79b`, corrección harness `3049cfdf637eb1c1d2fb0be7f9881e517a3cf13f` y corrección routing/final funcional `053152ae51de3617bf30a4e9987574c7879e3049`.

**QA real:** el primer certificado `31836552560` dejó 6 pruebas API verdes y detectó que el harness levantaba API en 5006 mientras Angular consumía 5005; se corrigió sin alterar la app. El segundo `31836970704` confirmó el login y detectó que `provideRoutes(ALMACENES_ROUTES)` registraba Almacenes después del wildcard `**`; se corrigió a `provideRouter([...ALMACENES_ROUTES, ...routes])`. El certificado final `31837394309`, job `94886619205`, terminó `SUCCESS`: build `-warnaserror`, 376 tests backend, API+migraciones MySQL 8.4+health, npm ci/lint/build, Angular y Chromium/Playwright `8 passed / 0 failed / 0 skipped`.

**Documentación/control:** fuente canónica `docs/ERP_N1_2_ALMACENES.md`; TASKS, CHANGELOG y tablero VAEP se reconcilian en N1.2.H. `main`, Producción, PR #2 merge/auto-merge, secretos y force-push permanecen intactos. **ERP-N1.2 queda formalmente cerrado** y el siguiente foco FINISH_FIRST es `N1.3.A — Ubicaciones internas / auditoría y preflight`.

## ERP-N1.1 — Sucursales empresariales

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

**Objetivo/alcance:** implementar y certificar el primer maestro de ERP-N1, `Sucursal`, de extremo a extremo: dominio/contratos, persistencia MySQL forward-only, API, RBAC relacional, auditoría, observabilidad, frontend responsive/accesible y QA específico. `EmpresaId` queda nullable como reserva de compatibilidad futura, sin FK ni semántica tenant antes de ERP-N6; Almacenes/Ubicaciones/Existencias permanecen en N1.2/N1.3/N1.4.

**Resultado funcional:** tabla `Sucursales` con código activo único mediante columna computada, índices de EmpresaId/estado, soft-delete y rollback fail-closed; API `/sucursales` con búsqueda/filtros/paginación, CRUD, activar/desactivar idempotente y baja lógica; `ModuloSistema.Sucursales=28` con grants persistidos `Ver/Crear/Editar/Activar/Desactivar/EliminarLogico`; auditoría `Entidad=Sucursal`; métricas P50/P95 de búsqueda sin término/PII; frontend con lista server-side, estados loading/error/vacío, formulario, permisos runtime, tabla desktop/cards móvil y rutas protegidas.

**Trazabilidad:** B `0a576db21e583a76418ce037ca53f8c30d3b7eb1`; C persistencia `3ca70a8b41125ba501b9d94261e43d9dcd269df9` + snapshot `65785999934d8f02ffdf947fa24f48ceb9059076`; D aplicación/API `c511039680938fb758c60cf199a0c665462c7e79` + pruebas `805818140ef78183e52a17d196f36c452d39ebc2`; E `d3009e051ffea91631673dc764e56fdf8cab70b2`; F `9ead42f594aea12c20612d7c15e21768c090f828`; G base `704d451e216ab4a48042ae8bfaca5995d77e9cdb`; fix QA `b82c8d8325866fdf4408e22424fefe692965b8d9`; certificado G `42a241162dc54c8fddf040a7321d57dd229f7e5b`.

**Defecto descubierto por E2E:** el primer certificado dedicado `31829945647` creó correctamente la Sucursal pero encontró `HTTP 500` al filtrar Auditoría por `accion=Crear`; `AuditoriaRepository` usaba `enum.ToString()` dentro de LINQ, no traducible de forma segura por EF/MySQL. Se corrigió forward-only a `Enum.TryParse<TEnum>` + comparación tipada y filtro inválido fail-closed; además `AuditoriaRepository.cs` quedó incluido en los paths del workflow N1.1 para impedir regresión silenciosa.

**Validación real final:** workflow permanente `ERP-N1.1 - Certificación Sucursales`, run `31830346962`, job `94864277702`, `SUCCESS`: restore, build Release `-warnaserror`, unit tests, API, migraciones MySQL 8.4, health/ready, npm ci, lint, build producción, Angular, Chromium/Playwright y E2E específico. El E2E valida 401 anónimo, correlation ID, alta/normalización, duplicados, auditoría, filtros/paginación, idempotencia, edición sin mutar estado, reactivación, UI móvil sin overflow y soft-delete. M10 del frontend también quedó verde en `31829186290`.

**Documentación/control:** fuente canónica `docs/ERP_N1_1_SUCURSALES.md`; `TASKS.md`, CHANGELOG y tablero VAEP reconciliados. `main`, Producción, merge/auto-merge del PR #2, secretos y force-push permanecen intactos. **ERP-N1.1 queda formalmente cerrado** y el siguiente foco autorizado es `N1.2.A — Almacenes / auditoría y preflight`.

## 2026-08-14 — ERP-N0.8 Migraciones y limpieza — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

**Objetivo/alcance:** cerrar ERP-N0.8 después de consolidar el preflight de saneamiento, materializar la relación `Compras.MetodoPagoId`, reconciliar las FKs tipadas de `MovimientoInventario` con el modelo EF, retirar el raw SQL como autoridad normal de origen, migrar Compra hacia el catálogo relacional de métodos de pago y eliminar la lista hardcodeada de pagos del formulario de Compras. El saneamiento fue deliberadamente conservador: una columna legacy solo se retira físicamente cuando históricos, reversión y consumidores permiten demostrar que el DROP es seguro.

**Resultado funcional:** `Compras.MetodoPagoId` se backfillea por `MetodosPago.Codigo` estable —nunca por equivalencia de IDs— y queda protegido por FK; Compra crea/edita/confirma mediante catálogo activo y falla cerrado ante métodos no representables. El bridge legacy es one-way y bajo lock: una fila histórica válida con FK nula converge al catálogo antes de confirmar, sin convertir el enum en autoridad. `MovimientoInventario` persiste/consulta `CompraId`/`VentaId`/`ConsumoInsumoId`/`AjusteInventarioId` mediante EF; `ReferenciaTipo/ReferenciaId` quedan solo como snapshot/correlación. El frontend de Compras consume `/metodos-pago/activos`, muestra el nombre, envía el código estable y bloquea Guardar ante loading/error/0 métodos/inactividad.

**Persistencia/rollback:** migración `20260814155400_N0_8_PersistenciaLimpiezaTransicional`, postcheck `backend/scripts/postdeploy-erp-n0-8-c-persistencia.sql` y snapshot EF reconciliado. La migración es forward-only: el rollback seguro exige respaldo/restauración compatible o corrección forward; no se autoriza un DROP improvisado de la nueva FK. `Producto.Cantidad/Costo`, `Compra.MetodoPago`, `MovimientoInventario.ReferenciaTipo/ReferenciaId` y `MovimientoFinanciero.ModuloOrigen/ReferenciaId` permanecen únicamente donde cumplen una función histórica/snapshot/bridge demostrada, no como autoridad primaria.

**Trazabilidad A–G:** A `c7d39903eb978337d501a37c4d9c32b506c450f3`; B `c20151391d696ebe1d172ae3341e579cc371c35f`; C `b7b1db8746beac2a6e3f25c68afcafd8768383c8`; D cierre dirigido `633d8fc36e2b825a6362f418c01254c8886f37fe`; E `4693502282f54e3adfeee97669e0ca7ffa10b3ae`; G/funcional final `369158761ad05671b9a1859d17796c8ca4a09bf8`. La regresión específica `frontend/e2e/n0-8-compras-metodos-pago-regresion.spec.ts` cubre método administrable dinámico y catálogo no disponible fail-closed.

**Validación final sobre `369158761ad05671b9a1859d17796c8ca4a09bf8`:** CI principal `31821172124` SUCCESS completo; M10 `31821172381` SUCCESS; Fase 8 `31821172230` SUCCESS; aceptación integral `31821172223` SUCCESS incluido Playwright/SMTP/PDF; M13 `31821172341` SUCCESS completo incluido historial MySQL, integración, SQL forward, upgrade histórico, frontend, seguridad HTTP, Playwright, SMTP/PDF/logs y `Dictamen automatizado M13` SUCCESS. No quedan P0/P1 conocidos atribuibles a ERP-N0.8.

**Documentación/control:** fuente final `docs/ERP_N0_8_MIGRACIONES_LIMPIEZA.md`; preflight `docs/ERP_N0_8_MIGRACIONES_LIMPIEZA_PREFLIGHT.md`; `TASKS.md`, CHANGELOG y tablero VAEP se reconcilian en N0.8.H. No se tocó `main`, Producción, merge/auto-merge del PR #2, secretos, infraestructura productiva, force-push ni ramas nuevas. El siguiente foco debe seleccionarse únicamente desde el gate/dependencias VAEP.

## 2026-08-14 — ERP-N0.7 AjusteInventario formal — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

**Objetivo/alcance:** cerrar formalmente ERP-N0.7 después de completar el agregado `AjusteInventario`, persistencia/snapshots, API y frontend, RBAC, auditoría crítica, correlación HTTP, regresión y certificación. Durante N0.7.H se detectó que los endpoints legacy `ajustes-stock`, aunque ya tenían el permiso correcto, todavía conservaban `InventarioAjusteService` como segunda autoridad de mutación. El cierre se detuvo y la arquitectura se corrigió antes de certificar.

**Corrección final:** `InventarioAjusteService` queda como adaptador puro hacia `IAjusteInventarioService`; el servicio formal concentra la única autoridad de stock. La compatibilidad legacy crea y confirma el `AjusteInventario` dentro de una sola transacción, conserva `CantidadActualEsperada` como precondición comprobada bajo lock y falla cerrada antes de movimiento/mutación si la lectura del cliente está obsoleta. Confirmar/Anular mantienen auditoría `RegistrarEstrictoAsync` dentro de la misma transacción y movimientos con origen tipado `AjusteInventarioId`.

**Cadena correctiva H:** `554c9f24902e12388c00e8ca093aa29b533c2ac1`, `3416e47e811a2f7c7387bbdaf9964e745a0f6021`, `28a0fe5a945c2071fe160bd208ca9cfc4a07013d`, `d0bd3b18f092d189efea5ee69b229bce669387f5`, `f26b7513cfb34ce9a9be54202b2363c1f19e712c`, `6e17376837e13fb70960da7b523785f54c23b04b`, `7079263f86461bae136b509151da491d2b8bfcbe` y SHA funcional final `cd5c1f058fc7a24fd477a4c9e8cda7cff4c99850`. El run sobre `7079263f...` reveló un test histórico que aún construía el adaptador con seis dependencias eliminadas; se corrigió forward-only en `cd5c1f05...`, sin ocultar el fallo.

**Validación final sobre `cd5c1f058fc7a24fd477a4c9e8cda7cff4c99850`:** CI principal `31808933744` SUCCESS completo, incluida integración MySQL 8.4; aceptación integral `31808933692` SUCCESS completo, incluido Playwright/SMTP/PDF; M13 `31808933833` COMPLETED/SUCCESS, incluido backend/MySQL/migraciones/upgrade histórico, frontend, Docker/backup, secretos/dependencias, seguridad HTTP, runtime/Playwright, SMTP/PDF/logs y `Dictamen automatizado M13` SUCCESS exigiendo todos los gates verdes.

**Documentación/control:** fuente canónica `docs/ERP_N0_7_AJUSTE_INVENTARIO.md`; `TASKS.md`, CHANGELOG y tablero VAEP quedan reconciliados. N0.7.A–H quedan cerrados y el siguiente foco FINISH_FIRST elegible es `N0.8.A`. No se tocó main, Producción, merge/auto-merge del PR #2, secretos, infraestructura productiva, force-push ni ramas nuevas.

## 2026-08-13 — ERP-N0.6 Referencias polimórficas críticas — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

**Objetivo/alcance:** cerrar formalmente ERP-N0.6 después de migrar la autoridad de origen de movimientos de inventario desde `ReferenciaTipo/ReferenciaId` hacia relaciones tipadas `CompraId`/`VentaId`/`ConsumoInsumoId`, preservando los campos legacy sólo como snapshots/bridge de transición. En finanzas se confirmó que `CompraId`/`VentaId`/`FacturaId` siguen siendo la autoridad y `ModuloOrigen/ReferenciaId` permanecen únicamente para auditoría/correlación.

**Resultado:** dominio tipado `Compra`/`Venta`/`ConsumoInsumo`; preflight y backfill fail-closed; C2/C3 y boundary typed-first; productores Compra/Venta/ConsumoInsumo migrados; contrato DTO/API tipado; frontend y nueva superficie RBAC marcados N/A por inspección dirigida; QA/regresión N0.6 cerrada sin crear pruebas redundantes. La fuente canónica final es `docs/ERP_N0_6_REFERENCIAS_POLIMORFICAS.md`; el preflight inicial permanece como antecedente histórico.

**Validación final sobre `0e35a9f75c49b6ddfbd5ef21d426521e2b559c40`:** ERP-N0.6 `31754907625` SUCCESS; Desarrollo build/tests `31754907682` SUCCESS; recovery MySQL `31754907598` SUCCESS; M11 backup/restore `31754907601` SUCCESS; Fase 8 `31754907626` SUCCESS; aceptación integral `31754907600` SUCCESS; M13 `31754907614` SUCCESS. Las pruebas críticas demuestran que la FK tipada manda aunque el snapshot legacy discrepe, que el bridge sólo cubre escritores legacy sin FK y que un mismatch tipado/legacy falla cerrado.

**Control:** N0.6.G y N0.6.H quedan cerrados, `TASKS.md` y VAEP se reconcilian y el siguiente foco FINISH_FIRST es N0.7.A — AjusteInventario formal / auditoría y preflight. No se tocó main, Producción, merge/auto-merge del PR #2, secretos, infraestructura productiva, force-push ni ramas nuevas.

## 2026-08-13 — ERP-N0.5 MetodoPago — CIERRE FORMAL

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

**Objetivo/alcance:** cerrar formalmente ERP-N0.5 después de completar frontend/selectores, RBAC/auditoría, reportes/facturas/PDF, regresión integral, workflow dedicado y recertificación M13. Se crea `docs/ERP_N0_5_METODOS_PAGO.md` como documento canónico y se reconcilia `TASKS.md` contra la evidencia real de COLA/GitHub.

**Correcciones de recertificación:** N0.5.14 detectó incompatibilidad con MySQL administrado/Aiven cuando `sql_require_primary_key=ON`. Se reemplazaron snapshots temporales `CREATE TEMPORARY TABLE ... AS SELECT` por tablas explícitas con PK y tipos históricos exactos en `20260812023600_N0_5_BackfillMetodoPagoHistorico.cs` (`20b3c3b42c8dbeff884a71493d4e1f9b33ad2394`) y, como regresión transversal descubierta por M13, en `20260812083000_N0_6_OrigenTipadoMovimientoInventario.cs` (`1bbccd9cccdcc181ab8c1e842ea0ff8343831197`). No se alteró el significado funcional ni el backfill histórico.

**Validación real final sobre `1bbccd9cccdcc181ab8c1e842ea0ff8343831197`:** ERP-N0.5 `31753406161` SUCCESS; recovery MySQL/Aiven-like `31753406119` SUCCESS; M11 backup/restore `31753406267` SUCCESS; Desarrollo build/tests `31753406190` SUCCESS; aceptación funcional integral `31753406328` SUCCESS; M13 `31753406059`, attempt 2, SUCCESS. M13 cubrió historial desde cero con MySQL estricto, integración, SQL forward, upgrade representativo, preservación histórica, frontend, runtime/Playwright, SMTP/PDF, seguridad/auditoría, Docker y vigencia de backup.

**Documentación:** `docs/ERP_N0_5_METODOS_PAGO.md` documenta contrato canónico, códigos históricos estables, migraciones, backend/API, frontend, históricos/snapshots, trazabilidad N0.5.09–N0.5.14, CI y riesgos residuales. `TASKS.md` queda reconciliado con los puntos ya certificados.

**Control:** ERP-N0.5 queda funcionalmente cerrado; N0.5.15 completa el cierre documental. No se tocó `main`, Producción, merge/auto-merge del PR #2, secretos, infraestructura productiva, force-push ni ramas nuevas.

## 2026-08-12 — N0.5.08 Backend/API/CRUD/DTOs MetodoPago — LISTO

**Responsable:** ChatGPT mediante conexiones autorizadas GitHub + Google Drive.

**Objetivo/alcance:** cerrar el backend administrable del catálogo relacional `MetodoPago` sin reintroducir el enum legacy como autoridad. Quedaron integrados DTOs, contratos e implementación de repositorio/servicio, API CRUD, activar/desactivar, reordenamiento, validación/canonicalización de metadata, DI, RBAC relacional y auditoría de mutaciones.

**Correcciones durante validación:** el CI inicial sobre `90fa101dca265c936f9007bf26209f903e24e4e3` detectó que los atributos runtime `MetodosPago:*` todavía no podían seedearse desde `CatalogoPermisosBase`; se incorporó el módulo con el mantenimiento completo en `b94aa0d9346f6efafe73b7911f07673ef07aceee`. Después se añadieron pruebas dirigidas del servicio en `016cfa1ff5712ad1d1e14d06f179de470d6a07c1`; dos fallos estrictamente de prueba —ambigüedad entidad/enum y analyzer xUnit sobre `DateTime` no nullable— se corrigieron forward-only en `d35030bfaa10018fa1a74b6e1efeca11d5cb5bd3` y `5827e610cf9cae1b6a3d5745d10e1cee59df6c78`.

**Cobertura dirigida:** crear normaliza código/canoniza metadata y audita; código duplicado falla cerrado sin persistir; editar registra usuario/auditoría; activar-desactivar preserva eliminación lógica; eliminar aplica trazabilidad; reordenamiento rechaza IDs duplicados antes de persistir. La prueba runtime de catálogo RBAC vuelve a garantizar que todos los permisos exigidos por controladores existen en el catálogo base.

**Validación real:** ERP-N0.5 run `31650122695` terminó `SUCCESS` completo: restore/build/pruebas backend, esquema relacional, historia representativa, fail-closed, preflight, backfill histórico, postcheck y snapshot EF quedaron verdes. El CI general `31650122667` terminó `SUCCESS` completo en sus cinco jobs: Backend Release/pruebas, migraciones e integración MySQL 8.4, Docker, frontend e higiene.

**Control:** `N0.5.08` queda `LISTO` y habilita `N0.5.09`, `N0.5.10` y `N0.5.11` según dependencias de `COLA`. No se tocó `main`, Producción, merge/auto-merge de PR #2, force-push ni ramas nuevas.

## 2026-08-12 — VAEP v2.2 EXECUTION_TRUTH + CI sin push funcional de GITHUB_TOKEN — CONFIGURADO

**Responsable:** ChatGPT mediante conectores autorizados GitHub + Google Drive + Programación.

**Problema detectado:** la corrida programada de las 13:02 terminó después de publicar la reparación EF, pero `RUNNER_MUTEX_STATE` quedó `RUNNING` con heartbeat 13:03, lo que podía confundirse con actividad real. Además, la reparación canónica de `N0.5.07B2` fue publicada por el workflow temporal `vaep-ef-snapshot-repair.yml` con `permissions: contents: write`; el HEAD `fc2ca060bbc7eefd84ead93ea370b292e3e200f2` quedó técnicamente actualizado, pero los workflows `pull_request` asociados aparecieron `action_required` y sin jobs, por lo que no constituyen evidencia de fallo funcional ni de CI ejecutado.

**Corrección de gobierno:** `PLAN_EJECUCION_AUTONOMA.md` evoluciona a VAEP v2.2 `EXECUTION_TRUTH`: el mutex deja de ser equivalente a actividad; se incorporan `RUNNER_ACTIVITY_STATE`, `RUNNER_LAST_REAL_ACTION_AT`, `STOP_REASON`, `RESUME_POINT` y un PRE-FINAL GATE obligatorio. Una respuesta final queda prohibida cuando la invocación todavía tiene capacidad, no hay CI activo y existe trabajo recuperable. Si la plataforma termina la invocación, debe declararse `IDLE_PLATFORM_LIMIT`/`WAITING_CI` según corresponda en vez de fingir ejecución continua.

**Corrección CI:** queda prohibido usar workflows temporales con `contents: write` para commitear/pushear cambios funcionales o migraciones mediante `GITHUB_TOKEN`. Actions podrá generar artefactos, pero la publicación final debe realizarla el Runner mediante el conector GitHub normal y fast-forward. `action_required` con jobs vacíos debe investigarse inmediatamente y no dejarse esperando hasta la siguiente hora.

**Continuidad B2:** `N0.5.07B2` continúa `VALIDANDO`; el snapshot/migración EF canónicos de Banco permanecen en `fc2ca060...`. El siguiente changeset operativo retira el workflow temporal escritor mediante el conector GitHub normal para provocar una sincronización ordinaria del PR y obtener CI real. No se toca `main`, Producción, merge/auto-merge de PR #2, force-push ni ramas nuevas.

## 2026-08-12 — VAEP v2.1 FINISH_FIRST: cerrar árbol foco antes de abrir hermanos

**Responsable:** ChatGPT mediante conectores autorizados GitHub + Google Drive + Programación.

**Objetivo/alcance:** corregir la selección que permitía dejar `N0.5` parcialmente abierto mientras el runner avanzaba `N0.6`. Se alinea `PLAN_EJECUCION_AUTONOMA.md`, `CONFIG` y el prompt de `Solqaryn VAEP v2 Runner` para priorizar el punto padre más antiguo ya iniciado y terminar todos sus hijos/subhijos antes de abrir un hermano.

**Cambios de gobierno:** `MAX_MICROTAREAS_POR_CORRIDA=SIN_TOPE_FIJO`; `REGLA_BLOQUEO=NO_SALTAR_ARBOL_FOCO`; política `RUNNER_SELECTION_POLICY=FINISH_FIRST`; locks propios stale deben reconciliarse/recuperarse; padres deben reflejar estado de hijos; un bloqueo real conserva el foco y detiene la corrida en vez de saltarlo. `RUNNER_CURRENT_RECOVERY_TARGET=N0.5` congela nuevas aperturas de N0.6 hasta cerrar N0.5, preservando intacto todo lo ya certificado en N0.6.

**Evidencia:** protocolo versionado en commit `9efbfbe7d7d8a701d86b1aa60940321747c61783`; tablero CONFIG/BITACORA actualizado y Runner horario actualizado en sitio. Cambio exclusivamente documental/de gobierno, con `[skip ci]`; no modifica código funcional, main, Producción, PR #2, auto-merge ni ramas.

## 2026-08-12 — N0.6.D2B/D2: productores tipados cerrados — LISTO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo/alcance:** reconciliar y cerrar la cadena `N0.6.D2` después de certificar los tres productores documentales de `MovimientoInventario`, sin tocar `N0.5.07B/07B1`, contratos DTO/API ni persistencia adicional. Esta entrada supersede el estado operativo antiguo de D2A que quedó registrado abajo como `VALIDANDO`: la solución finalmente certificada fue el boundary `typed-first` del repositorio, no el intento incompleto de mapping EF.

**Resultado:** `D2A` quedó certificado mediante `6eadf19a27a0c7c90b0cec54262070f896209738` y CI `31587640123`; `D2B1` Compra mediante `e62b0667f4faace2d8d6520f753547b3e2624a1d` / pruebas `c76124980914edbea57ad7ff97eaa705171a2d58` / CI `31589093189`; y `D2B2` Venta mediante `bac4d61b34813168b087fd7e9caf740a518c354a` / pruebas `06dea3390e0c40bef94e80f2e0ce30f482cac1f2` / CI `31589968458`; y `D2B3` ConsumoInsumo mediante `8648cc61f29a878d213ff2ddcce4e3731a81ff43` con correcciones de prueba hasta `ed570bb842ae4fbeb57b981bd596dfafbecf6072`.

**Validación real:** el CI general `31594243722` sobre `ed570bb842ae4fbeb57b981bd596dfafbecf6072` terminó `SUCCESS` completo en Backend Release/pruebas, migraciones e integración MySQL 8.4, Docker, frontend e higiene. Los intentos previos `31593684786` y `31593975660` fallaron por defectos de prueba/build (`mapping EF` asumido y API `SqlQueryInterpolated` no disponible) y fueron corregidos sin modificar el servicio funcional; la verificación final usa ADO.NET contra MySQL real.

**Control:** `N0.6.D2B3`, `N0.6.D2B` y `N0.6.D2` quedan `LISTO`; `N0.6.D3` queda habilitada. `N0.5.07B/07B1` conserva su lock concurrente. No se tocó main, Producción, PR #2, auto-merge ni ramas nuevas.

## 2026-08-12 — N0.6.D2B1: productor Compra migra a origen tipado — LISTO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo/alcance:** migrar exclusivamente el productor de movimientos de inventario de Compras al boundary `typed-first` ya certificado en D2A, sin tocar Venta, ConsumoInsumo, EF, migraciones ni contratos HTTP. D2B se subdividió adaptativamente en D2B1 Compra, D2B2 Venta y D2B3 ConsumoInsumo para mantener un concern por changeset.

**Resultado:** `CompraService.ConfirmarAsync` y `AnularAsync` escriben mediante `IMovimientoInventarioRepository.AddConOrigenTipadoAsync` con `OrigenMovimientoInventario.DesdeCompra(compra.Id)`. La anulación usa `CausaMovimientoInventario.AnulacionCompra`, por lo que el repositorio deriva el snapshot legacy `CompraAnulada` sin recuperar autoridad desde `ReferenciaTipo/ReferenciaId`.

**Pruebas dirigidas:** `CompraServiceTests` cubre confirmación/anulación con origen tipado y ausencia del writer legacy en confirmación.

**Validación real:** CI general `31589093189` terminó `SUCCESS` completo sobre `c76124980914edbea57ad7ff97eaa705171a2d58`: Backend Release/pruebas, migraciones e integración MySQL 8.4, Docker, frontend e higiene. La integración dirigida quedó incluida en el job MySQL.

**Control:** `N0.6.D2B1` queda `LISTO`; habilita `N0.6.D2B2`. `N0.5.07B/07B1` conserva su lock concurrente y no fue intervenido. No se tocó main, Producción, PR #2, auto-merge ni ramas nuevas.

## 2026-08-12 — N0.6.D2A: mapear origen tipado de MovimientoInventario en dominio/EF — VALIDANDO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo/alcance:** incorporar al modelo de dominio y metadatos EF las columnas `CompraId`, `VentaId` y `ConsumoInsumoId` que C2/C3 ya crearon y certificaron físicamente. Esta microtarea no crea DDL nuevo ni modifica todavía los productores.

**Resultado:** `MovimientoInventario` expone las tres FKs nullable; `MovimientoInventarioConfiguration` las mapea hacia `Compra`, `Venta` y `ConsumoInsumo` con `DeleteBehavior.Restrict`, los nombres reales de constraints N0.6 y los índices existentes. Se añadió una prueba de metadatos EF para verificar propiedades y principales relacionales.

**Control:** estado `VALIDANDO` hasta CI real. Las columnas `ReferenciaTipo/ReferenciaId` permanecen como snapshot de compatibilidad y D2B sigue pendiente. No se tocó N0.5.07B/07B1, main, Producción, PR #2, auto-merge ni ramas nuevas.

## 2026-08-12 — N0.6.D1: repositorio y consultas de MovimientoInventario usan origen tipado — LISTO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo/alcance:** retirar `ReferenciaTipo/ReferenciaId` como autoridad decisoria en las consultas de inventario usadas por anulación de compras, manteniendo el fallback legacy únicamente para el provider InMemory de pruebas hasta que D2 migre productores.

**Resultado:** `MovimientoInventarioRepository` consulta `CompraId` en el provider relacional para localizar el movimiento original de compra y para determinar las claves de movimientos posteriores. La prueba de integración aislada fuerza desacuerdo entre el snapshot legacy y `CompraId` y demuestra que MySQL sigue la FK tipada.

**Evidencia funcional:** `2a2e093f66899b9c02c18026ecd3f270b6a730c1`. El primer CI general `31585321041` falló exclusivamente porque el fixture generaba un `NumeroCompra` más largo que la columna; el defecto de prueba quedó corregido en `c19aa5005ef7262d91f118f5f4adf7b78aaf41e9`, sin ocultar el fallo inicial.

**Validación real:** CI general `31585718867` terminó `SUCCESS` completo sobre `c19aa5005ef7262d91f118f5f4adf7b78aaf41e9`: Backend Release/pruebas, migraciones e integración MySQL 8.4, Docker, frontend e higiene. La integración dirigida `MovimientoInventarioOrigenTipadoIntegrationTests` quedó incluida en el job MySQL que finalizó en verde.

**Control:** `N0.6.D1` queda `LISTO`; habilita `N0.6.D2`. Los locks concurrentes `N0.5.07B/07B1` no se intervinieron. No se tocó main, Producción, PR #2, auto-merge ni ramas nuevas.

## 2026-08-12 — N0.6.C3: postcheck, constraints e integridad histórica — LISTO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo/alcance:** cerrar la persistencia/migración N0.6 con postcheck y constraints de origen tipado, preservando el bridge transitorio desde `ReferenciaTipo/ReferenciaId` y sin retirar todavía las columnas legacy.

**Resultado:** después del primer intento de C3, el CI general detectó ocho integraciones incompatibles con una restricción demasiado amplia. La corrección final `01c1116e6db4e839b56176333251e3992fa09d77` acota la obligatoriedad del origen tipado a movimientos documentales mapeables (`Compra`, `Venta`, `ConsumoInsumo`) y permite que ajustes no documentales conserven temporalmente cero FKs tipadas. Se mantiene exclusividad, equivalencia con `ReferenciaId`, triggers transitorios de bridge y fail-closed frente a combinaciones inválidas.

**Evidencia funcional:** C3 evolucionó mediante `48ec0e9b9251e95522194e1580c0702a100e026c`, `e68184b2fccc9fd3e5e8c8950e261dce2d1c3e04` y corrección final `01c1116e6db4e839b56176333251e3992fa09d77`. El fallo inicial del CI general `31580565994` quedó corregido, no ocultado.

**Validación real:** CI general `31581993565` terminó `SUCCESS` en Backend Release/pruebas, migraciones e integración MySQL 8.4, Docker, frontend e higiene. ERP-N0.6 `31581993553` terminó `SUCCESS`: preflight C1 fail-closed, historia representativa, aplicación C2/C3, integridad tipada, bridge legacy→FK, constraint permanente fail-closed y snapshot EF sin drift.

**Control:** `N0.6.C1/C2/C3` y el padre `N0.6.C` quedan `LISTO`. La siguiente tarea propia del punto es `N0.6.D`; no se inicia en esta corrida porque el usuario limitó la ejecución a una única tarea independiente. `N0.5.07B/07B1` mantiene lock concurrente y no fue intervenido. No se tocó main, Producción, PR #2, auto-merge ni ramas nuevas.

## 2026-08-12 — N0.6.B: contrato de origen tipado de MovimientoInventario — LISTO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo/alcance:** introducir exclusivamente el contrato de dominio e invariante del origen tipado definido por el preflight N0.6.A, sin adelantar persistencia, configuración EF, backfill ni consumidores de N0.6.C/D.

**Resultado:** se añadieron `TipoOrigenMovimientoInventario` y el value object `OrigenMovimientoInventario`. El contrato representa `Compra`, `Venta` o `ConsumoInsumo`, expone el identificador tipado correspondiente y falla cerrado si no existe origen, existen varios orígenes o el identificador no es positivo. La operación concreta continúa separada en `TipoMovimientoInventario`/`CausaMovimientoInventario`; no se codifican anulaciones/reversiones en strings del origen.

**Pruebas dirigidas:** `OrigenMovimientoInventarioTests` cubre los tres orígenes admitidos, exclusividad del ID tipado, cero orígenes, múltiples orígenes e IDs no positivos.

**Evidencia funcional:** `5fe605cc93470a4f4b90f73185016b9e15bc622e`, publicado por fast-forward exclusivamente en `Desarrollo`.

**Validación real:** CI general run `31575657900`: `Backend Release y pruebas` terminó `SUCCESS`, incluyendo restore, build Release y pruebas backend no-integración; `Frontend producción`, `Higiene del repositorio` y `Docker y aislamiento de entornos` también terminaron `SUCCESS`. El job MySQL continuaba ejecutándose al cierre proporcional de B y no se usa como evidencia de cierre porque esta microtarea no modifica EF ni persistencia.

**Concurrencia/control:** `N0.5.07B/07B1` mantiene lock de otro runner y no fue intervenido. `N0.6.C` queda habilitada por dependencia; deberá añadir persistencia nullable, preflight/backfill/constraints/postcheck sin retirar aún las columnas legacy. No se tocó main, Producción, PR #2, auto-merge ni ramas nuevas.

## 2026-08-12 — N0.6.A: preflight de referencias polimórficas críticas — LISTO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo/alcance:** auditoría dirigida del punto N0.6 sin cambios funcionales. Se confirmó que `MovimientoInventario` todavía usa `ReferenciaTipo + ReferenciaId` como autoridad de origen sin FK tipada y que esa pareja participa en la seguridad de anulación de compras. Los productores confirmados son compra/compra anulada, venta/venta anulada y consumo/reversión de insumos. El DTO/API de movimientos también expone el contrato legacy.

**Finanzas:** `MovimientoFinanciero` ya dispone de `CompraId`, `VentaId` y `FacturaId`; su configuración EF declara esas FKs como autoridad y conserva `ModuloOrigen/ReferenciaId` únicamente como snapshot de auditoría/correlación. N0.6 no debe deshacer esa migración ni eliminar snapshots antes de certificar históricos.

**Diseño de transición:** `N0.6.B` debe separar origen tipado de operación/reversión y preparar como mínimo `CompraId`, `VentaId` y `ConsumoInsumoId` en inventario. `N0.6.C` deberá añadir persistencia nullable de transición, preflight fail-closed sobre valores históricos, backfill determinista, constraints/postcheck y mantener columnas legacy hasta limpieza posterior segura en N0.8.

**Evidencia:** `docs/ERP_N0_6_REFERENCIAS_POLIMORFICAS_PREFLIGHT.md` contiene alcance, archivos afectados, riesgos, rollback y matriz de validaciones. `TASKS.md` registra N0.6.A cerrado y la continuidad B→C→D–H.

**Validación real:** validación documental proporcional: inspección dirigida de entidades, EF, repositorio, productores Compra/Venta/ConsumoInsumo, DTO/servicio/API y finanzas; no se ejecutaron builds ni tests porque el changeset es exclusivamente documental/preflight y no modifica app, workflows, migraciones ni entorno. Publicación exclusivamente en `Desarrollo` con `[skip ci]` conforme a `AGENTS.md`.

**Concurrencia/control:** `N0.5.07B/07B1` estaba tomado por otro runner ChatGPT y no fue intervenido. `N0.6.A` es independiente de ese lock; el Plan Maestro declara N0.6 dependiente de N0.0. No se tocó main, Producción, PR #2, auto-merge ni ramas nuevas.

## 2026-08-12 — N0.5.07A: elegibilidad Activo + preservación histórica — LISTO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo/alcance:** primer hijo de N0.5.07. Los resolvers de `VentaRepository`, `FacturaRepository` y `MovimientoFinancieroRepository` solo devuelven métodos `Activo && !Eliminado` para operaciones nuevas. El límite de persistencia financiera rechaza también una FK/navegación directa inactiva o eliminada. Las lecturas históricas no filtran la navegación, por lo que relaciones existentes continúan visibles tras desactivar el catálogo; las reversiones históricas conservan su relación original.

**Pruebas dirigidas:** `MetodoPagoElegibilidadRepositoryTests` cubre resolver activo/inactivo/eliminado en los tres consumidores, lectura histórica de un método inactivo y fail-closed de una nueva operación financiera con catálogo inactivo.

**Validación real:** CI general `31571200414` terminó `SUCCESS` en backend/pruebas, integración MySQL, Docker, frontend e higiene; ERP-N0.5 `31571200316` terminó `SUCCESS` completo. Evidencia funcional `11c958ead2a7a8cc5a3b1db4b502cbe63e8efba7`.

## 2026-08-11 — N0.5.06 C: MovimientoFinanciero migra a autoridad relacional — LISTO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Resultado:** `MovimientoFinanciero.MetodoPago` legacy dejó de ser autoridad persistente/operativa sin adelantar N0.5.07. `IMovimientoFinancieroRepository` resuelve catálogo por código/nombre; todas las lecturas cargan `MetodoPagoCatalogo`; el límite de persistencia normaliza cualquier entrada legacy transitoria hacia `MetodoPagoId` y limpia el enum antes de guardar. La reversión pagada de compra copia exclusivamente FK/navegación relacional y falla cerrada si el original carece de relación. `FinanzasService` resuelve movimientos manuales contra catálogo y sus DTOs leen el nombre relacional.

**Pruebas dirigidas:** `FinanzasServiceTests` cubre persistencia/lectura relacional y fail-closed de método inexistente; `MovimientoFinancieroRepositoryTests` cubre normalización legacy→FK, carga de navegación y reversión sin propagación del enum.

**Evidencia funcional:** commit `0f14b9b9f5248a01cb6c98fa456cd306fe38ae19` publicado en `Desarrollo`. El temporal accidental `NOPE_DO_NOT_CREATE` fue eliminado de la punta efectiva mediante fast-forward, sin force-push.

**Validación real:** workflow dedicado `ERP-N0.5 - Certificación MetodoPago histórico`, run `31568099373`, terminó `success`: restauración/compilación/pruebas backend, esquema relacional, historia representativa, fail-closed, preflight, backfill histórico, postcheck/preservación 1:1 y snapshot EF quedaron verdes. El CI general run `31568099446` también terminó `success` en sus cinco jobs: Backend Release/pruebas, migraciones e integración MySQL 8.4, Docker, frontend e higiene.

**Control:** `N0.5.06C` y su padre `N0.5.06` quedan `LISTO`; con A1/A2/A3/B/C cerradas, la siguiente tarea de la cadena es `N0.5.07`, dependiente directamente de C.

## 2026-08-11 — N0.5.06 B: FacturaPago migra hacia MetodoPago relacional — LISTO

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Resultado:** `FacturaPago` dejó de usar el enum como autoridad operativa. `IFacturaRepository`/`FacturaRepository` resuelven métodos por código/nombre y cargan `MetodoPagoCatalogo` tanto en pagos como en la venta de origen. `FacturaService.RegistrarPagoAsync` resuelve el DTO temporal contra catálogo y persiste `MetodoPagoId`/navegación; el enum queda solo como proyección de compatibilidad derivada. Los DTOs de factura/pago leen el nombre desde el catálogo relacional.

**Evidencia funcional:** implementación hasta `d5e9a98c17848001fc64387c709a72ce0e379cd3`; fixtures relacionales ajustados en `e8ab2b733affea70ba47b3ea8a7ff450c6b7766f`; cierre resumido en `c53a99150520d25b3a91d4e8aee7d3c6003ccd97`.

**Validación real:** CI general run `31567189353` completó `success` en Backend Release/pruebas, migraciones MySQL, Docker, frontend e higiene. Workflow dedicado ERP-N0.5 run `31567189393` completó su job `metodo-pago-historico` en `success`: backend, esquema, historia representativa, fail-closed, preflight, backfill, postcheck y snapshot EF.

**Control:** el Sheet estaba rezagado en `VALIDANDO` y fue reconciliado contra GitHub. El siguiente punto elegible de la cadena es `N0.5.06C`, retiro de autoridad legacy en `MovimientoFinanciero`.

## 2026-08-11 — N0.5.06 A3: lectura y propagación de Venta migradas a MetodoPago relacional

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Resultado:** microtarea A3 `LISTO`. El commit funcional `c024cc7c96da45f6d2b21867950de3c4dce49fd4` eliminó el uso de `Venta.MetodoPago` como autoridad de lectura/propagación dentro de `VentaService`: `VentaDto.MetodoPago` se obtiene desde `MetodoPagoCatalogo.Nombre` y el `MovimientoFinanciero` automático creado al confirmar una venta recibe `MetodoPagoId`/`MetodoPagoCatalogo`; su enum legacy se deriva únicamente del catálogo cuando existe.

**Pruebas dirigidas:** `05687cffcf9d34b3fdd8efd9becf9d158b61f028` añadió cobertura para comprobar que el DTO usa el nombre del catálogo aunque el enum legacy difiera y que la confirmación propaga FK/navegación al movimiento financiero sin copiar el enum legacy de Venta.

**Validación real:** CI general run `31566541771`: job `Backend Release y pruebas` completó `success`, incluyendo restore, build Release y pruebas backend no-integración; `Frontend producción`, `Higiene del repositorio` y `Docker y aislamiento de entornos` también completaron `success`. El job MySQL seguía ejecutándose al cierre operativo de A3, por lo que no se atribuye un resultado aún no finalizado. El workflow dedicado ERP-N0.5 run `31566541808` fue generado para el mismo SHA y continuaba su certificación histórica.

**Control:** A3 no modifica `FacturaPago` ni el servicio financiero general. El siguiente punto de la cadena es B, que debe retirar la autoridad enum de `FacturaPago` y sus DTOs/flujos sin ampliar todavía reglas operativas de N0.5.07.

## 2026-08-11 — N0.5.06 A2: escrituras de Venta migradas a MetodoPago relacional

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Resultado:** microtarea A2 `LISTO`. El commit funcional `32feca8840122c7eccd58246a6db7196730d8491` migró `VentaService.CreateAsync/UpdateAsync`: el texto temporal del DTO se resuelve contra el catálogo persistente mediante `IVentaRepository.GetMetodoPagoPorCodigoONombreAsync`, se establecen `MetodoPagoId` y `MetodoPagoCatalogo`, y el enum legacy queda únicamente como proyección de compatibilidad derivada. Un método inexistente o vacío produce `BusinessRuleException`; ya no existe fallback silencioso de método desconocido a `Efectivo`.

**Pruebas dirigidas:** `e00e20c614c8c66c34f726c82ef4922d48dc21d8` añadió `VentaMetodoPagoServiceTests` para creación con FK/navegación, rechazo de método inexistente y actualización hacia catálogo relacional.

**Validación real:** workflow `ERP-N0.5 - Certificación MetodoPago histórico` run `31566179324` completó finalmente `success`: restore/build/tests backend, esquema relacional, historia representativa, fail-closed, preflight, backfill, postcheck y snapshot EF quedaron verdes. CI general run `31566179269` fue generado para el mismo SHA; Docker e higiene estaban `success` durante el cierre operativo.

**Control:** A2 no cambia migraciones ni contratos HTTP; `CreateVentaDto/UpdateVentaDto.MetodoPago` sigue siendo adaptador string temporal. N0.5.06 no está cerrado: A3 migra lectura de `VentaDto` y propagación automática hacia `MovimientoFinanciero`.

## 2026-08-11 — Cierre N0.5.06 A1: repositorio Venta preparado para MetodoPago relacional

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Resultado:** microtarea A1 `LISTO`. El commit funcional `d987cb669de6dfbd00b8691a46e27f566e32138c` añadió resolución de `MetodoPago` por código/nombre en `IVentaRepository`/`VentaRepository`, carga `MetodoPagoCatalogo` en lecturas operativas y carga explícita de la navegación en `FOR UPDATE`.

**Validación real:** en el CI general run `31563809556`, el job `Backend Release y pruebas` completó `success`, incluyendo restore, build Release y pruebas backend no-integración; frontend, higiene y Docker también completaron `success`. El workflow dedicado `ERP-N0.5 - Certificación MetodoPago histórico`, run `31563809580`, completó su job `metodo-pago-historico` en `success`: backend, esquema relacional, historia representativa, fail-closed, preflight, backfill histórico, postcheck/preservación 1:1 y snapshot EF quedaron verdes.

**Continuidad:** N0.5.06 no está cerrado. El siguiente punto elegible de esta cadena es A2: migrar escrituras de `VentaService` hacia `MetodoPagoId`/catálogo. A3, FacturaPago y MovimientoFinanciero continúan después según dependencias VAEP.

## 2026-08-11 — N0.5.06 A1: preparar Venta para autoridad relacional de MetodoPago

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo:** iniciar la eliminación de la doble autoridad de métodos de pago con un changeset pequeño y coherente, preparando el repositorio de Venta para que las siguientes microtareas puedan resolver y leer el catálogo relacional sin depender del enum legacy.

**Granularización VAEP:** el punto original N0.5.06 cruzaba Venta, FacturaPago y MovimientoFinanciero y resultó demasiado amplio. Se subdividió en A1 repositorio/carga relacional de Venta, A2 escrituras de Venta, A3 lecturas/propagación de Venta, B FacturaPago y C MovimientoFinanciero. N0.5.07 depende del cierre de C.

**Alcance funcional de A1:**

- `IVentaRepository` expone resolución de `MetodoPago` por código/nombre;
- `VentaRepository` carga `MetodoPagoCatalogo` en consultas operativas normales;
- la lectura transaccional `FOR UPDATE` carga explícitamente la navegación `MetodoPagoCatalogo`;
- se añade resolución dirigida contra el catálogo persistente excluyendo registros eliminados;
- no se cambia todavía el DTO/API, las reglas `Activo/RequiereReferencia/...`, ni se retiran físicamente columnas legacy.

**Validación previa real:** se verificaron de forma dirigida `Venta`, `FacturaPago`, `MovimientoFinanciero`, sus configuraciones EF, `VentaService`, `FacturaService`, `IVentaRepository` y `VentaRepository`. La revisión confirmó que `VentaService` todavía usa el enum como autoridad en creación/edición y que N0.5.06 debía dividirse antes de modificarlo. El build/CI se ejecutará sobre el commit funcional publicado; no se declara éxito de CI en esta entrada antes de que GitHub lo reporte.

**Riesgo/control:** A1 es infraestructura preparatoria y no declara N0.5.06 cerrado. Las escrituras y lecturas de `VentaService` siguen pendientes en A2/A3; GitHub/CI determinarán si A1 puede marcarse `LISTO`.

## 2026-08-11 — VAEP-001: reducir ejecuciones CI redundantes en certificaciones ERP-N0

**Responsable:** ChatGPT mediante conexión GitHub autorizada.

**Objetivo:** evitar que los workflows históricos de certificación ERP-N0.2, N0.3, N0.4 y N0.5 consuman CI ante cambios exclusivamente frontend/documentales/no relacionados, sin reducir cobertura cuando cambien backend, tests, scripts propios o el workflow correspondiente.

**Alcance:** se añadieron filtros `paths` al evento `push` de `.github/workflows/erp-n0-2-ci.yml`, `erp-n0-3-ci.yml`, `erp-n0-4-ci.yml` y `erp-n0-5-ci.yml`, alineándolos con sus filtros de `pull_request`. `workflow_dispatch` permanece intacto y el CI general `desarrollo-ci.yml` no se reduce.

**Validación real:** el commit funcional `d2466a3047e7cd2001f1cf998faa08c4ae229c1b` fue publicado por fast-forward sobre `Desarrollo`. GitHub aceptó los cuatro YAML y generó ejecuciones `push`/`pull_request` para los workflows modificados; por ejemplo ERP-N0.2 run `31562526962` y ERP-N0.5 run `31562526984` fueron creados sobre el mismo SHA. El diff confirma que el cambio funcional se limita a los filtros `paths` de `push`; N0.1 ya estaba filtrado. Los jobs de certificación seguían ejecutándose al momento del cierre documental, por lo que no se atribuye un resultado funcional de esas suites que todavía no había concluido.

**Resultado:** `VAEP-001` queda `LISTO` porque el objetivo de trigger fue implementado y aceptado por GitHub. Los futuros pushes exclusivamente frontend/documentales/no relacionados dejan de disparar estas cuatro certificaciones históricas; cambios en backend, tests, scripts propios y los propios workflows siguen cubiertos.

## 2026-08-11 — VAEP v2: Plan Maestro ERP V5 completo + cola granular

**Responsable:** ChatGPT mediante conectores autorizados GitHub + Google Drive.

**Objetivo:** convertir el Plan Maestro ERP V5 en una ejecución autónoma integral, granular y auditable, evitando changesets gigantes y permitiendo continuidad cuando exista un bloqueo independiente.

**Alcance:** importación del Plan Maestro a Drive, tablero VAEP v2, ERP-N0→N9, gates, T0–T12, backlog futuro no-core, 778 microtareas, granularización adaptativa y bloqueo transitivo.

**Validación real:** fuente rectora y tablero verificados; sin cambios productivos.

## 2026-08-11 — VAEP v1: ejecución autónoma, Drive y dependencias

Estados estrictos, selección por prioridad/dependencias, lock lógico y bloqueo no global.

## 2026-08-11 — Gobierno colaborativo v2

Gate `PROJECT_ID=SOLQARYN`, aislamiento entre proyectos, lectura mínima, evidencia obligatoria y hardening de publicación.

## 2026-08-11 — Gobierno colaborativo y memoria canónica

Creación/alineación de memoria canónica y reglas de continuidad.

## 2026-08-11 — ERP-N0 Punto 5: backfill histórico de MetodoPago

Migración, seed idempotente, backfill, preflight/postcheck y workflow N0.5 certificados.

## 2026-08-11 — Catálogo público SOLQARYN

**Responsable:** Codex. Consulta pública segura y personalización pública.

## Formato futuro

Cada entrada debe contener fecha, agente, objetivo, alcance, validaciones reales, riesgos/pendientes y commit cuando sea útil. No registrar secretos ni datos sensibles.
## 2026-08-12 - N0.5.07B2 - snapshot EF canonico de Banco - VALIDANDO

**Responsable:** ChatGPT / VAEP v2 Runner.

**Correccion:** los CI 31622173253 y 31622173357 demostraron que la logica Banco/fail-closed y las pruebas pasaban, pero dotnet ef migrations has-pending-model-changes detecto drift porque la migracion inicial de Banco no actualizo el snapshot EF canonico. Se reemplaza esa migracion manual por una migracion generada con EF Core 8.0.8, su Designer y AppDbContextModelSnapshot, sin relajar ninguna validacion ni alterar el diseno normalizado.

**Control:** B2 permanece VALIDANDO hasta que los CI reales sobre el changeset canonico terminen en verde. No se toca main, Produccion, PR #2, auto-merge ni ramas nuevas.

## 2026-08-24 — ERP-N3.3 Reserva automática de inventario — CIERRE FORMAL

**Responsable:** ChatGPT/VAEP v3.25 mediante PARENT CLOSURE GOVERNOR y ATOMIC_PARENT_PUBLISH documental.

**Objetivo/alcance:** cerrar formalmente N3.3.A-H sin reabrir código funcional. La confirmación de `PedidoVenta` reutiliza `ReservaInventario` y la autoridad física `ExistenciaVariante`; la reserva compromete `StockReservado` sin mover `StockFisico` por el mero acto de reservar y no introduce una segunda autoridad cuantitativa ni selección automática inventada de almacén/ubicación.

**Evidencia:** baseline funcional `960ac07ed1e96d1d2e98a51fdb5dc216fbc8d0f3`; N3.3.D/E/F/G ya estaban `LISTO` en COLA, la regresión E2E `reservation-automatic-flow.spec.ts` fue aceptada por el control VAEP y P0/P1 bloqueantes conocidos atribuibles a N3.3=0. Los fallos de workflows legacy ERP-N0 observados en paralelo no se usan como gate causal sin evidencia directa.

**Documentación/control:** `docs/CERTIFICACION_N3_3_RESERVA_AUTOMATICA.md`, `docs/RUNBOOK_N3_3_RESERVA_AUTOMATICA.md` y el ADR vigente `docs/ADR_N1_8_RESERVAS_STOCK_RESERVADO_Y_OVERSELLING.md`. `TASKS.md` se reconcilia en el mismo commit atómico. Siguiente parent dependency-valid: `N3.4.A — Remisiones/entregas / Auditoría y preflight`.

## 2026-08-26 — ERP-N3.6 Devoluciones de clientes — CIERRE FORMAL

N3.6.A-H formally closed only as the target content being prepared for controller integration.

Approved closure facts:
- baseline functional 6c5a3164ab11a1dcdcdfa9418c61bb0165251239
- Development #32913855654 SUCCESS
- Acceptance #32913854936 SUCCESS
- Fase8 #32913854958 SUCCESS
- M13 #32913854923 SUCCESS
- certification 4fe25e8cf656f82e3883f0585fa29358769aa48c
- runbook d906393fc26b0073ac782721ea08cb0fa35827b5
- TASKS rollup 6efbb72880a15bd6cf7f2d5d6bbb3d1b0d0118d7
- P0/P1 known attributable to N3.6 = 0
- next parent after H is N3.7.A, promotion blocked until H LISTO.

## 2026-08-26 — ERP-N3.7 Nota de crédito de cliente — CIERRE FORMAL

**Responsable:** ChatGPT/VAEP v3.25.1 Closure Governor mediante QA takeover documental y hard verify history-preserving.

**Objetivo/alcance:** cerrar formalmente N3.7.A-H sin reabrir código funcional ni inventar semánticas fiscales, stock, Kardex, caja o downstream no certificadas. `NotaCreditoCliente` conserva el alcance y contratos ya certificados por N3.7.A-G.

**Evidencia:** N3.7.A Issue #752 `LISTO_REAL`; N3.7.B `46a250fcc0cfd1562306538375e772a94c39bea5`; N3.7.C `9810cf2e7fd0289a9374a8477a4131f3f73fef38`; N3.7.D `8bcacae8a45fe3c0072bf519610bcc1ec1203a4f`; N3.7.E `f9ef582749a79c8900741d1a40ff393039c7b287`; N3.7.F `943aa0e607af3221ed8987a0edac37a539561696`; N3.7.G Issue #781 `LISTO_REAL`. Los gates causales y P0/P1 atribuibles de esos padres quedaron certificados en sus cierres.

**Cierre documental/control:** `TASKS.md` ya contiene el rollup N3.7 y esta publicación agrega únicamente este bloque a `CHANGELOG_AI.md`, preservando byte por byte todo el blob source `d53c56416ac7ac01beef761adab5172cf5297487` y sin eliminar ni reformular historia previa. Issue #782 es el control de cierre; PR #2 permanece Draft `Desarrollo → main`, sin merge. P0/P1 atribuibles conocidos al cierre: 0.

**Promoción:** con esta publicación N3.7.H queda formalmente `LISTO`; el selector fail-closed puede promover `N3.8.A` y mantener N3.8.B como pipeline SAFE según dependencias.

## 2026-08-26 — ERP-N3.8 Nota de débito de cliente — CIERRE CONDICIONAL/N/A

**Responsable:** ChatGPT/VAEP v3.25.1 Closure Governor.

**Dictamen:** N3.8.A-H se cierra para el alcance actual como N/A con evidencia porque el roadmap condiciona la Nota de débito a una necesidad legal/operativa y no existe todavía requisito autoritativo suficiente para fijar su contrato. No se afirma que `NotaDebitoCliente` haya sido implementada.

**Evidencia:** A=`034ec3305422016d6c571d0ffcf1332e3bbbe6b6`; B=`affb58f2b9e7d8ab25c051fed5b9f4ee5f317584`; C-G=`3a89725e4a76c4d85c0c4adc04f0affa4a61e79a`; certificación=`docs/CERTIFICACION_N3_8_NOTA_DEBITO_CLIENTE.md`. Delta funcional=0 y P0/P1 atribuibles conocidos=0.

**Reapertura:** si legislación/operación exige esta capacidad, reabrir desde N3.8.B con contrato explícito antes de dominio/persistencia/API/UI. El selector puede promover `N3.9.A`.

## 2026-08-26 — ERP-N3.9 Cuentas por cobrar — CIERRE FORMAL

**Responsable:** ChatGPT/VAEP v3.25.1 Closure Governor mediante QA takeover documental y hard verify history-preserving.

**Objetivo/alcance:** cerrar formalmente N3.9.A-H con base en hechos certificados. La proyección Cuentas por cobrar está implementada como una vista de solo lectura (GET /cuentas-por-cobrar) sobre la verdad operativa de Factura y FacturaPago, reutilizando el control RBAC existente (Facturacion/Ver) sin introducir libros contables mutables, esquemas propios, endpoints de escritura, lógica de mora/anticipos, ni nuevos permisos.

**Evidencia:** N3.9.A-G están formalmente `LISTO_REAL`. La certificación canónica documental reposa en `docs/CERTIFICACION_N3_9_CUENTAS_POR_COBRAR.md`. P0/P1 atribuibles conocidos al cierre: 0.

**Promoción:** con esta publicación, N3.9.H queda formalmente `LISTO`. El selector fail-closed puede promover el siguiente padre `N3.10.A`, respetando el bloqueo que impedía avanzar antes del cierre de H.

## 2026-08-27 — ERP-N3.10 Crédito del cliente — CIERRE FORMAL

**Responsable:** ChatGPT/VAEP v3.25.1 Closure Governor mediante QA takeover documental y hard verify history-preserving.

**Objetivo/alcance:** cerrar formalmente N3.10.A-H con base en las autoridades certificadas, manteniendo la capacidad de crédito integrada a Cliente. Este cierre no introduce una segunda autoridad comercial, motor autónomo de scoring, ledger paralelo, nuevos permisos RBAC ni efectos automáticos adicionales sobre venta, factura, stock, Kardex, caja o contabilidad.

**Evidencia:** N3.10.C=`619a0ba2a53ad70fb332c9f61198eb3b022ddcc1`; N3.10.D=`3c5a2c30a3d8427d0d0764ef1d4bc4e895d4d585`; N3.10.E=`615d1a4878854bf22770b945256db39fea44e08f`; N3.10.F/G=`98b7777555cd6f7ee881edb76321cd1226ca69eb`; certificación canónica=`docs/CERTIFICACION_N3_10_CREDITO_CLIENTE.md`. Los gates causales aplicables de estas autoridades están certificados y P0/P1 atribuibles conocidos al cierre=0.

**Cierre documental/control:** este bloque y el rollup paralelo de `TASKS.md` son exclusivamente aditivos. Todo el contenido histórico previo de ambos archivos debe permanecer byte-prefix intacto; PR #2 continúa Draft `Desarrollo → main`, sin merge.

**Promoción:** con esta publicación `N3.10.H` queda formalmente `LISTO_REAL`; el selector fail-closed puede promover `N3.11.A` y mantener N3.11.B/C como pipeline SAFE según dependencias.

## 2026-08-27 — ERP-N3.11 POS / Venta rápida — CIERRE FORMAL

**Responsable:** ChatGPT/VAEP v3.25.1 Closure Governor mediante QA takeover documental y hard verify history-preserving.

**Objetivo/alcance:** cerrar formalmente N3.11.A-H con base en las autoridades certificadas, reutilizando la autoridad existente de Venta para el alcance de venta rápida. La experiencia existente (ventas/nueva) provee la funcionalidad requerida sin introducir una segunda superficie POS independiente en dominio, persistencia, API, frontend o permisos.

**Evidencia:** N3.11.A-G están certificados para el alcance vigente (LISTO_REAL / QA_TAKEOVER_CERTIFIED). Certificación canónica = `docs/CERTIFICACION_N3_11_POS.md`. P0/P1 atribuibles conocidos al cierre: 0.

**Decisiones pendientes:** cashier/session/terminal, split-tender/change, suspension/reprint, offline, POS-specific idempotency y POS-specific RBAC quedan explícitamente como DECISION_PENDING. No se materializan en el producto hasta un requisito autoritativo futuro.

**Cierre documental/control:** este bloque es exclusivamente aditivo sobre el histórico existente. PR #2 continúa Draft `Desarrollo → main`, sin merge.

**Promoción:** con esta publicación, N3.11.H quedará formalmente LISTO (TARGET_AFTER_PUBLICATION).

## 2026-09-03 — ERP-N4.4 Cuentas por cobrar — CIERRE DOCUMENTAL

Responsable ChatGPT/VAEP v3.25 Closure Governor / Jules A; objective finalización lógica N4.4 A-G; evidence A-F LISTO_REAL and G LISTO_REAL with docs/CERTIFICACION_N4_4_CUENTAS_POR_COBRAR.md and docs/RUNBOOK_N4_4_CUENTAS_POR_COBRAR.md; closure H is NOT declared LISTO_REAL.

## 2026-09-03 — ERP-N4.5 Cuentas por pagar — ROLLUP DOCUMENTAL

Responsable Codex local autorizado; objetivo reconciliar de forma aditiva la certificación N4.5 sin duplicar autoridad financiera ni declarar prematuramente el cierre H. N4.5.A-G quedan respaldadas por `docs/CERTIFICACION_N4_5_CUENTAS_POR_PAGAR.md`, reutilizando la autoridad ERP-N2.8 y el baseline funcional `541ec12b72912c769c6f54b8821771e509818375`. El HEAD posterior contiene únicamente documentación y manifests VAEP/Jules, sin delta productivo. N4.5.H permanece `EN_PROGRESO` hasta obtener gates exact-head terminales y revalidar P0=0/P1=0; `N4.6.A` continúa `PREARMED/PROMOTION_HELD`.

Reconciliación QA append-only: este rollup supersede únicamente el estado operativo stale anterior del bloque N4.4; las menciones históricas `CURRENT_PARENT=N4.4.H` permanecen como evidencia histórica, ya no gobiernan el estado actual y no se reescribe ni elimina ninguna historia previa. El estado operativo vigente de este cierre es `CURRENT_PARENT=N4.5.H`. Certificación documental exacta: commit `fde578dd69cbfe91c054138d33404cec342093f6`; `productBaseHead=541ec12b72912c769c6f54b8821771e509818375`. N4.5.H no se declara `LISTO_REAL` en este rollup.

## 2026-09-03 14:32:52 -06:00 - Optimización controlada CI/CD y Vercel

Responsable: Codex local autorizado en `Desarrollo`; HEAD inicial `1cf6847e4e70e2fe99ef5ec59b57c33f4f7c49d3`.

Alcance: auditoría de los 43 workflows existentes, sin eliminar workflows ni evidencias. `erp-n0-2-ci.yml`, `erp-n0-3-ci.yml`, `erp-n0-4-ci.yml` y `erp-n0-5-ci.yml` quedaron clasificados como certificación histórica y sus triggers genéricos `backend/src/**`/`backend/tests/**` fueron acotados a artefactos, migraciones, pruebas, scripts y workflows propios; todos conservan `workflow_dispatch`. `erp-n0-6-preflight-ci.yml` fue revisado y no modificado porque ya usa paths específicos. `desarrollo-ci.yml` ahora escucha solo áreas técnicas backend/frontend, configuración de build, migraciones e infraestructura asociada; la documentación, VAEP, bitácoras y evidencias no disparan el CI pesado. `vaep-jules-diagnostic.yml` conserva su no-op por ausencia de manifest y ahora filtra PR por `vaep/jules/diagnostic/*.json` y su propio workflow.

Playwright: se agregó `actions/cache@v4` para `~/.cache/ms-playwright`, versionado mediante `hashFiles('frontend/package-lock.json')`, a los 12 workflows que ejecutan `npx playwright install --with-deps chromium`. Se conserva `--with-deps` para las dependencias Linux; no se afirma ahorro cuantitativo sin medición histórica comparable. No se implementó build-once entre jobs porque los jobs de base e integración tienen restores, bases MySQL y artefactos separados; hacerlo en este cambio ampliaría el riesgo.

Vercel: `frontend/vercel.json` usa un `ignoreCommand` local fail-open que solo omite build cuando la comparación Git confirma que todos los archivos son documentación, gobierno, `.github` o `vaep`; cualquier cambio frontend, backend o no clasificado fuerza build. No se modificó el dashboard ni la separación externa de ramas/proyectos (`solqaryn-desarrollo` para `Desarrollo`, `SOLQARYN` para producción); esa configuración queda pendiente de verificación/ajuste externo seguro.

Validaciones realizadas: `git diff --check` sin errores; inspección de diff/stat/status; comprobación de que los workflows históricos conservan `workflow_dispatch`; comprobación estática de que los filtros N0.2-N0.5 ya no contienen los comodines genéricos; comprobación de los 12 caches Playwright y de la conservación de sus instalaciones; sintaxis JavaScript del script Vercel y pruebas de comportamiento fail-open/path-based con SHA document-only y SHA con cambios de código. No se ejecutaron builds o suites completas porque el changeset solo modifica workflows/configuración de CI y Vercel.

Riesgos y pendientes: GitHub Actions debe confirmar en el siguiente run que los filtros coinciden con los paths reales; Vercel debe validar desde cada proyecto que el `ignoreCommand` se ejecuta con el root esperado. La optimización no elimina certificaciones históricas ni sus pruebas manuales, y no demuestra por sí sola un ahorro medido. Sin cambios a `main`, Producción, secretos, bases productivas, dominios, deploys ni PR #2.

## 2026-09-03 15:07:51 -06:00 - RCA P0 Vercel y aislamiento Desarrollo/Producción

Responsable: Codex local autorizado en `Desarrollo`; HEAD inicial de esta auditoría `2658d5b0139e85957463cb227f11ea65f42bef13`. La consulta read-only del equipo `Solqaryn` confirmó dos proyectos Vercel vinculados al mismo repositorio GitHub `jmejia31/Solqaryn`: `solqaryn-desarrollo` (`prj_JkRGpdSnGlMQ4Qc3eqw4bscY4Flu`) y `SOLQARYN` (`prj_djMCand2yYeY3AvaUWsjwHDDJDkM`).

RCA confirmado: `SOLQARYN` acepta pushes de `Desarrollo` mediante Git Integration y crea deployments de fuente `git` con `githubCommitRef=Desarrollo`, `target=null` y alias `SOLQARYN-git-desarrollo-SOLQARYN.vercel.app`. Ocurrió para `eaacb832dfc78723ad9cb7d119d88a32c62a0047` y nuevamente para `2658d5b0139e85957463cb227f11ea65f42bef13`. Por tanto, fijar solamente `Production Branch=main` no basta: debe deshabilitarse la creación de Preview Deployments de `Desarrollo` en el proyecto `SOLQARYN` mediante la configuración de Git Integration/Preview Branches, conservando producción en `main`. El conector read-only no expone ni permite editar esos campos; no se realizó cambio externo.

Estado observado: para `2658d5b...`, `Vercel - solqaryn-desarrollo` quedó `FAILURE` con `Deployment rate limited - retry in 24 hours`; `Vercel - SOLQARYN` quedó `SUCCESS`. `solqaryn-desarrollo` sí generó su deployment para `Desarrollo` (`target=production`, alias `solqaryn-desarrollo-git-desarrollo-SOLQARYN.vercel.app`), consistente con el diseño documentado. La duplicación real de builds/deployments quedó probada en ambos proyectos.

La configuración local `frontend/vercel.json` y `frontend/scripts/vercel-ignore-build.mjs` no se modificó en esta toma: JSON y JavaScript válidos; pruebas del ignore: solo documentación=`exit 0`, frontend/runtime=`exit 1`, diff no clasificable=`exit 1`. El mecanismo es fail-open y basado en paths, pero no puede impedir que un segundo proyecto Git cree el deployment antes de evaluar el ignore; además, un deployment cancelado puede seguir consumiendo cuota.

Ajuste externo pendiente, seguro y reversible: en `SOLQARYN`, confirmar `Production Branch=main`, desactivar Preview Deployments para la rama `Desarrollo` (o excluir explícitamente `Desarrollo` en la regla de ramas de preview), conservar root `frontend` y no cambiar dominio, secrets ni producción. En `solqaryn-desarrollo`, confirmar `Production Branch=Desarrollo`, root `frontend` y que sus previews/runtime apunten únicamente a Desarrollo. La aplicación debe registrar estado antes/después y no usar deploy manual. No se relanzaron workflows, no se cambió `main`, no se tocó N0.2-N0.5, y no se modificó el commit concurrente N4.6.B.

## 2026-09-03 15:33:53 -06:00 - Certificación read-only del aislamiento Vercel

Responsable: Codex local autorizado; HEAD inicial `e5d48ef2f5dfdfabe0866f957beef4b744f9ac33`, repositorio `jmejia31/Solqaryn`, rama `Desarrollo`. El preflight confirmó árbol limpio, `HEAD=origin/Desarrollo` y no hubo fast-forward adicional.

Ajuste externo manual reportado y verificado por el operador: `SOLQARYN` queda con Production Branch/tracking `main` y Preview/Avance `Disabled`; `solqaryn-desarrollo` queda con Production Branch/tracking `Desarrollo` y Preview/Avance `Disabled`. Codex no modificó Vercel, no hizo deployment, rollback, cambio de Git Integration, dominio ni secreto.

Evidencia read-only: el equipo `Solqaryn` mantiene exactamente los proyectos `solqaryn-desarrollo` (`prj_JkRGpdSnGlMQ4Qc3eqw4bscY4Flu`) y `SOLQARYN` (`prj_djMCand2yYeY3AvaUWsjwHDDJDkM`), ambos vinculados a `jmejia31/Solqaryn`. Desde el commit documental anterior `e5d48ef2` no aparecen deployments nuevos en ninguno. El último evento de `SOLQARYN` es el Preview de `Desarrollo` cancelado por `Ignored Build Step`; no existe evidencia posterior que contradiga el ajuste manual.

RCA previo: antes del ajuste, ambos proyectos reaccionaban al mismo repositorio y `SOLQARYN` creaba Preview Deployments desde `Desarrollo`; el ignore podía cancelar el build, pero el deployment ya consumía cuota. El estado manual actual asigna `Desarrollo` únicamente a `solqaryn-desarrollo` y `main` únicamente a `SOLQARYN`, desactivando Preview tracking automático en ambos proyectos.

Limitación de certificación: el conector read-only no expone los campos Production Branch/Preview Tracking y todavía no existe un push legítimo posterior al cambio manual. Por ello, la configuración final se registra como verificada manualmente por el operador y respaldada por ausencia de deployments posteriores, pero la prueba natural definitiva queda pendiente del próximo push legítimo de `Desarrollo`. El resultado esperado es deployment solo en `solqaryn-desarrollo` cuando corresponda y ningún Preview en `SOLQARYN`.

El estado `build-rate-limit` queda documentado como limitación previa de `solqaryn-desarrollo`; no se relanzó ningún workflow/deployment para intentar evadir la cuota. No se modificaron `main`, PR #2, Producción, secretos, BD, dominios, N0.2-N0.5, `frontend/vercel.json` ni `frontend/scripts/vercel-ignore-build.mjs`.

## 2026-09-03 16:10:00 - N4.6.C persistencia de plan de cuentas

Responsable: Codex local autorizado sobre `a85cb962`; la implementación posterior de C en `Desarrollo` fue reconciliada desde los commits concurrentes hasta `f3f92b4b`. La persistencia EF de `CuentaContable` quedó materializada con configuración jerárquica, FK restrictiva, restricciones, índices, migración compatible con MySQL 8.4 y snapshot efectivo.

Evidencia local de la revisión inicial: Infrastructure/API compilaron y las pruebas dirigidas de persistencia pasaron. El controller de cierre registró posteriormente N4.6.C como `LISTO_REAL` en el handoff autoritativo de GitHub, con CI exact-head terminal y P0/P1=0; Codex no sustituye esa evidencia ni duplica el changeset concurrente.

## 2026-09-03 16:35:00 - N4.6.D backend API — candidato local

Responsable: Codex local autorizado; base reconciliada `f3f92b4b`. Se integró y revisó el contrato de DTO/servicio de CuentaContable y se añadió la superficie HTTP protegida de lectura jerárquica, raíces, creación y actualización bajo `ModuloSistema.Finanzas`, reutilizando el repositorio y excepciones existentes. La lectura construye el árbol desde el conjunto completo para no truncar subcuentas profundas.

Validación local: `dotnet test --filter FullyQualifiedName~CuentaContable` PASS (5/5). El artifact Jules D no se integró: su review autoritativo lo rechazó por patch anidado, stubs y pruebas RBAC insuficientes. N4.6.D no se declara `LISTO_REAL` hasta completar security/QA/CI exact-head y revisión del controller; no se afirma evidencia no ejecutada.

## 2026-09-03 17:20:00 - N4.6.E frontend del plan de cuentas

Responsable: Codex local autorizado; se añadió la feature standalone `PlanCuentasComponent`, su modelo/servicio HTTP y la ruta protegida `/plan-cuentas`. La UI presenta el árbol completo, alta/edición, selección de padre sin descendientes cíclicos, tipo, estado, aceptación de movimientos, loading/empty/error states, responsive y navegación bajo permisos de Finanzas. La autoridad de validación permanece en el backend.

Validación real: `npm.cmd run lint` PASS; `npm.cmd run build -- --configuration development` PASS. El build de producción no se pudo completar porque Angular intentó inlining de fuentes externas y el entorno rechazó la conexión; no se cambió configuración de producción ni Vercel.

## 2026-09-04 02:31:00Z - Cierre N4.6 Plan de cuentas

Responsable: Codex local autorizado en `Desarrollo`; cierre documental append-only sobre el exact-head funcional `9d649bbbb4279e41e8cf5b7f5f9b84c26cc362bf`.

N4.6.A-H queda `LISTO_REAL` con persistencia jerárquica EF, repositorio, API protegida, UI Angular, RBAC/auditoría y certificación en `docs/CERTIFICACION_N4_6_PLAN_CUENTAS.md`. Gates reales: Development `#33828121004`, aceptación `#33828121038`, Fase 8 `#33828121029`, M13 `#33828121086` y M10 `#33828121034`, todos `SUCCESS`; `Solqaryn CI` `SKIPPED` no se usa como PASS. P0/P1 atribuibles al alcance: `0/0`.

La ejecución Fase 2 terminó `FAILURE` únicamente por HTTP 503 de `registry.npmjs.org` durante `npm audit`; se clasifica `EXTERNAL_INFRA`, sin evidencia de regresión causal y sin rerun artificial. No se modificaron producción, secretos, dominios, Vercel ni workflows históricos N0.2-N0.5. El siguiente parent dependency-valid es `N4.7.A`; este changeset no inicia su scope.

## 2026-09-04 - VAEP hardening — Bloque 1 admission/schema

Responsable: Codex local autorizado en `Desarrollo`; changeset preparado sobre el cierre N4.6 `d6ca66fd790eb3446d028f84f57bd03ccc5647bf`.

Se añadió `vaep/schemas/jules-dispatch.schema.json` y el validador reutilizable `scripts/vaep/dispatch-preflight.mjs`. La admisión exige identidad Solqaryn, rama `Desarrollo`, task/parent/dependencias, scope protegido, base SHA existente y ancestral, ownership, sesión y attempt válido. `ADMITTED` es el único resultado que puede iniciar ATTEMPT; JSON inválido, dependencia bloqueada, duplicado, scope inválido o conflicto fallan antes del worker sin consumir ATTEMPT. Un base stale ancestral sin solapamiento devuelve `REFRESHABLE`; un stale con solapamiento material devuelve `FAIL_CLOSED`.

Validación real: JSON schema parseable, `node --check` en ambos scripts y `node scripts/vaep/dispatch-preflight-self-test.mjs` con 9/9 casos PASS. No se modifican manifests históricos, código de producto, Producción ni `main`.

## 2026-09-04 - VAEP hardening — Bloque 2 evidence/bundles

Se añadió el schema de fragmentos `vaep/schemas/evidence-fragment.schema.json`, la política de escritura aislada en `vaep/evidence/fragments/`, el agregador `scripts/vaep/aggregate-evidence.mjs` y el planificador `scripts/vaep/bundles.mjs`. Los fragmentos son por task/dispatch; Jules ya no necesita editar `CHANGELOG_AI.md`. El agregador valida evidencia, estado terminal, SHA y P0/P1, ordena de forma determinística, detecta duplicados/colisiones, ofrece `--check`, `--dry-run` y `--apply`, y usa escritura temporal seguida de rename; una repetición con el mismo digest es no-op. El agregador solo deja evidencia para revisión y nunca marca `LISTO_REAL`.

El modo `bundled` agrupa los gates existentes como `CORE=A-D`, `UI_RBAC=E-F` y `E2E_CERT=G-H`, manteniendo cada gate visible y reteniendo los siguientes ante fallo de dependencia. `VAEP_EXECUTION_MODE=legacy` continúa disponible y es el fallback inicial. No se eliminaron filas A-H ni se generaron commits prewarm/filler.

Validación real: `node --check` de los tres scripts, parseo del schema y `node scripts/vaep/block2-self-test.mjs` con 6/6 casos PASS, incluyendo dry-run, apply, idempotencia, tres bundles, retención C/D tras fallo B y fallback legacy.

## 2026-09-04 - VAEP hardening — Bloque 3 sync/reconcile/metrics

Se añadió `scripts/vaep/sync-bitacora.mjs` para payload estructurado GitHub→BITÁCORA con URL/token por entorno, HMAC opcional, timeout, tres retries e idempotency key; sin configuración queda `SKIPPED` explícito y seguro. `scripts/vaep/reconcile-status.mjs` valida exact-head, gates terminales, P0/P1, documentación, dependencias y ownership, pero devuelve únicamente `ELIGIBLE_FOR_CONTROLLER_REVIEW` con `autoPromote=false`; no habilita `SUCCESS -> LISTO_REAL`. `scripts/vaep/metrics.mjs` y `vaep/metrics/baseline.json` dejan contadores observables sin inventar baseline histórico.

Se añadió el workflow liviano `.github/workflows/vaep-engine-ci.yml`, limitado a paths VAEP, con parseo de schemas, syntax checks y self-tests. La documentación canónica queda en `docs/VAEP_PERFORMANCE_HARDENING.md`; `VAEP_EXECUTION_MODE=legacy` continúa como fallback y ningún gate A-H fue eliminado.

## 2026-09-03 — Integración AntiG Reviewer/Fixer automático

Responsable: ChatGPT remoto autorizado en `Desarrollo`.

Se materializó la integración repo-side de Antigravity como reviewer/fixer automático entre Jules y VAEP. Componentes: Custom Agent `.agents/agents/solqaryn-reviewer/agent.md`, contrato `vaep/schemas/antig-review-result.schema.json`, worker `scripts/antig/antig-review-worker.ps1`, instalador `scripts/antig/install-antig-automation.ps1`, self-test `scripts/antig/antig-self-test.ps1` y runbook `docs/ANTIGRAVITY_AUTOMATION.md`.

El worker consume únicamente Issues terminales `[VAEP-JULES*] ... result`, resuelve el workflow causal, descarga su artifact, valida task/dispatch/attempt/scope, invoca Antigravity CLI headless con el agente `solqaryn-reviewer`, permite correcciones menores/medias dentro del mismo scope y produce exclusivamente `READY_FOR_VAEP`, `RETURN_TO_JULES`, `BLOCKED_QA_TAKEOVER` o `NO_ACTION`. `LISTO_REAL` no existe en el schema de decisión AntiG.

La publicación queda separada del agente: el Custom Agent tiene prohibidos commit/push/merge/rebase/reset/checkout/switch. El wrapper solo integra cuando el checkout comenzó limpio y sincronizado, la salida estructurada reporta P0=0/P1=0, no hay blocker ni scope leak, `git diff --check` pasa y `origin/Desarrollo` conserva el exact-head inicial. Nunca se usa force-push ni rebase automático. Si el remoto cambia, falla cerrado y preserva los commits locales para reconciliación humana/controller.

Gobernanza actualizada: ATTEMPT=1 con defecto estructural puede regresar al único R2; ATTEMPT=2 no vuelve a Jules y pasa a `BLOCKED_QA_TAKEOVER`; R3+ permanece prohibido. AntiG prepara `READY_FOR_VAEP`; VAEP/controller conserva la autoridad independiente de certificación.

CI: `.github/workflows/vaep-engine-ci.yml` ahora incluye paths AntiG, parsea el schema estructurado y ejecuta `scripts/antig/antig-self-test.ps1 -StaticOnly`.

Límite real: ChatGPT remoto no tiene shell en la PC autorizada, por lo que no puede registrar por sí mismo el Scheduled Task de Windows ni modificar `~/.gemini/antigravity-cli/settings.json`. La activación física queda reducida a una ejecución local de `scripts/antig/install-antig-automation.ps1`, que valida `agy`, GitHub auth, workspace agent, configura permisos finos, crea watermark para no reprocesar historia y registra `Solqaryn-AntiG-Reviewer` cada minuto. No se tocaron `main`, Producción, Vercel, secretos ni BD productiva.

## 2026-09-03 — Hardening P1 AntiG posterior a auditoría Codex

Responsable: ChatGPT remoto autorizado en `Desarrollo`.

Se corrigieron los P1 que bloqueaban la activación local de AntiG. El worker ya no modifica ni restaura el checkout primario: crea un Git worktree temporal aislado desde el exact-head inicial y elimina únicamente ese worktree al terminar. Se eliminó `git add --all`; la publicación usa staging explícito de la lista exacta de paths autorizados y verifica que staged==delta autorizado.

Se añadió validación causal Issue -> workflow run -> artifact único -> dispatch -> result -> gitpatch -> changes.patch; validación de attempt/base/scope, rutas protegidas, identidad de salida AntiG y compatibilidad controlada entre manifests Jules v3.25 y el contrato estructurado v1.0. Un handoff inválido se cuarentena bajo `.git/vaep-antig/quarantine/` y consume su watermark para que no bloquee Issues posteriores; fallos transitorios continúan fail-closed sin consumo automático.

El self-test ahora ejecuta pruebas funcionales del contrato y una prueba real de aislamiento con Git worktree que confirma que un archivo concurrente del checkout primario sobrevive intacto. El instalador conserva permisos finos y añade rollback transaccional de settings/estado/tarea si falla su fase mutante. La activación física continúa prohibida hasta CI del exact-head final + auditoría final independiente.

## 2026-09-04 - Cierre técnico P1 AntiG [Codex]

Sobre el exact-head inicial `b8905c185df2c40df6f036b1a8f082c36130d260`, se corrigieron los gaps restantes dentro de `scripts/antig/`: causalidad estricta `Issue -> run -> artifact -> dispatch -> result -> gitpatch -> changes.patch`, contrato legacy v3.25 fail-closed, contrato estructurado v1.0 completo, detección staged/unstaged/untracked y staging explícito con igualdad de paths.

Se bloquearon explícitamente `frontend/vercel.json` y `frontend/scripts/vercel-ignore-build.mjs`. Handoffs estructuralmente inválidos se cuarentenan y avanzan watermark; errores transitorios de GitHub/CLI/red permanecen retryables sin consumir watermark. La publicación confirma `evidenceHead` antes de persistir `COMMENT_PENDING`, de modo que un fallo de comentario no republica el mismo patch.

El instalador captura y restaura el XML de una tarea previa, o elimina únicamente una tarea nueva durante rollback. Se ampliaron self-tests funcionales de contrato, patch causal, artifacts ambiguos, staging, aislamiento, watermark/idempotencia y planes de rollback. No se instaló AntiG, no se creó Scheduled Task real y no se tocaron main, Producción, Vercel, secretos ni BD productiva.

La activación local ahora resuelve `agy` desde el PATH o desde `%LOCALAPPDATA%\agy\bin\agy.exe`, evitando dependencia de un PATH interactivo no refrescado. El self-test corrige el manejo de stderr informativo de Git y evalúa el éxito únicamente mediante el exit code nativo.

## 2026-09-04 — ERP-N4.7 Asientos Contables — ROLLUP DOCUMENTAL

**Responsable:** ChatGPT/VAEP bajo `docs/VAEP_AUTHORITY.md`.

**Objetivo/alcance:** reconciliar de forma aditiva/history-preserving el cierre documental de N4.7 sin modificar producto ni adelantar N4.8. La certificación canónica es `docs/CERTIFICACION_N4_7_ASIENTOS.md`; el baseline funcional certificado de A-G es `c8d1e373ba8ea008bf773e69afa10f5f18d6de8b`.

**Evidencia previa al rollup:** N4.7.F `LISTO_REAL @09:49 -06`, N4.7.G `LISTO_REAL @09:50 -06`; REVIEW_FIRST del certificado canónico PASS; exact-head documental previo `6986874048985e4746d21e23254479b391220445` con gates aplicables terminales SUCCESS; `Solqaryn CI=SKIPPED` excluido; P0=0/P1=0.

**Control fail-closed:** este changeset completa exclusivamente el rollup documental `TASKS.md` + `CHANGELOG_AI.md`. `N4.7.H` no se declara `LISTO_REAL` por este texto: permanece pendiente de gates aplicables terminales y revalidación P0/P1=0 sobre el exact-head resultante del rollup. Solo después VAEP puede cerrar H y promover `N4.8.A`. No se modifica `main`, Producción, secretos, deploy, ramas ni PR #2.

## 2026-09-04 — Cierre Fase 0 y Fase 1 de migración VAEP MASTER

Responsable: ChatGPT/VAEP sobre `Desarrollo`.

Se cerraron formalmente las fases de migración `FASE 0 — EXCLUSIVIDAD` y `FASE 1 — FREEZE + RECONCILIACIÓN`. La autoridad operativa continúa en `docs/VAEP_AUTHORITY.md`; ChatGPT/VAEP conserva controller/REVIEW_FIRST/QA/certificación. No se otorgó autoridad operativa a Codex ni AntiG.

Se restauró el freeze técnico de admisión mediante `vaep/control/dispatch-admission.json` con `newDispatchAdmission=FROZEN` y el guard correspondiente en `.github/scripts/vaep-jules-master.sh`. El runtime trata cero manifests nuevos como `NO_OP`, falla cerrado con múltiples manifests, valida el control state antes de crear sesión/attempt/ownership/recovery y bloquea nuevos dispatches cuando la admisión está congelada. La batería aislada previamente ejecutada para `NO_OP/FROZEN/OPEN/MULTI/INVALID` permanece `PASS`.

La reconciliación operativa conserva `CURRENT_PARENT=N4.7.H` y `N4.8.A=HELD`. Este cierre de migración NO equivale a certificar `N4.7.H=LISTO_REAL`; no se promueve N4.8, no se consume attempt y no se abre trabajo nuevo durante el freeze.

Resultado: `MIGRATION_PHASE_0=CLOSED/PASS`; `MIGRATION_PHASE_1=CLOSED/PASS`; `NEW_DISPATCH_ADMISSION=FROZEN`; `FASE_2=NOT_STARTED`; `FALSE_PASS=NO`; `FALSE_LISTO=NO`; `SCOPE_LEAK=NO`. No se modificaron `main`, Producción, secretos, dominios, Vercel ni BD productiva.

## 2026-09-04 — Cierre Fase 2 de migración VAEP MASTER — Política Machine-Readable

Responsable: AntiG/Antigravity (intervención puntual autorizada por Javier exclusivamente para Fase 2).

Se ejecutó la consolidación de la política operativa machine-readable dentro del MAESTRO único `docs/VAEP_AUTHORITY.md` (`AUTOMATION_AUTHORITY=MASTER`):

1. Bloque canónico único: Se embebió exactamente una vez el bloque estructurado delimitado por `BEGIN_AUTOMATION_POLICY` y `END_AUTOMATION_POLICY` con las 6 claves operativas autorizadas.
2. Parser fail-closed: Se implementó `.github/scripts/vaep-policy-parser.sh` en Bash puro sin `eval` ni `source`. Valida unicidad de bloque, 6 keys requeridas, rechazo estricto fail-closed ante claves desconocidas, duplicadas, faltantes o valores inválidos. Emite flags `--env`, `--get`, `--commit-sha`, `--hash` determinístico SHA-256 (`dfebc1a87010bf57742b05dae258ae29b23c9b881e40a9700fed237498fbf599`), y cuenta con suite de validación `--self-test` pasando 100%.
3. Deduplicación de runtime y workflows: `.github/scripts/vaep-jules-master.sh` y `.github/scripts/vaep-jules-worker.sh` consumen la política mediante el parser; se eliminaron hardcodes redundantes. En los 8 workflows `vaep-jules-*.yml` se retiraron overrides de budget conservando `timeout-minutes: 25` exclusivamente como safety-net externo.
4. Consumidores activos alineados: `AGENTS.md`, `docs/VAEP_JULES.md`, `PLAN_EJECUCION_AUTONOMA.md`, `PROJECT_CONTEXT.md`, `docs/COLABORACION_IA.md` y `docs/CONTEXTO_CHATGPT_VAEP.md` apuntan a `docs/VAEP_AUTHORITY.md` (`AUTOMATION_AUTHORITY=MASTER`) sin duplicar reglas. AntiG queda documentado como `RESERVED_INACTIVE`.
5. Protocolos numéricos eliminados en superficies activas (`NUMERIC_PROTOCOL_LABELS=0`).
6. Cierre: `MIGRATION_PHASE_2=CLOSED/PASS`; `CURRENT_PARENT=N4.7.H`; `N4.8.A=HELD`; `FASE_3=NOT_STARTED`. Autorización puntual AntiG expirada; sin scheduler ni Scheduled Tasks.

## 2026-09-04 — Corrección final de cierre Fase 2 VAEP MASTER

REVIEW_FIRST posterior al handoff AntiG detectó que la primera publicación de Fase 2 aún conservaba dos defectos de consolidación: `.github/scripts/vaep-jules-worker.sh` repetía límites de retry como literales en prompts y validación, y master/worker consumían `--env` mediante process substitution sin propagar de forma explícita el exit status del parser. Por ello, la afirmación inicial `RUNTIME_POLICY_DUPLICATES=0` no se tomó como evidencia suficiente hasta corregir ambos puntos.

El commit `dd518b608f0404f249a6315b10793da1c500226c` elimina esos gaps sin ampliar scope: master/worker capturan primero la salida del parser y abortan fail-closed si este falla; los self-tests dejan de duplicar los valores canónicos; retry validation, prompts, logs e Issues consumen `JULES_MAX_ATTEMPTS`/`JULES_REWORK_MAX` desde MASTER; los artifacts/resultados incluyen `MASTER_COMMIT_SHA` y `AUTOMATION_POLICY_HASH`.

La evidencia causal del exact-head de código corregido es `VAEP engine lightweight checks #33909740182 = SUCCESS`. `Solqaryn CI=SKIPPED` queda explícitamente excluido como PASS. Los ocho workflows Jules continúan sin overrides de budget y conservan solo `timeout-minutes: 25` como safety-net externo.

Cierre corregido: `MIGRATION_PHASE_2=CLOSED/PASS`; `RUNTIME_POLICY_DUPLICATES=0`; `POLICY_BLOCK_COUNT=1`; `PARSER_FAIL_CLOSED=PASS`; `MASTER_SHA_POLICY_HASH_EVIDENCE=PASS`; `ACTIVE_NUMERIC_PROTOCOL_AUTHORITIES=0`; `FASE_3=NOT_STARTED`; `CURRENT_PARENT=N4.7.H`; `N4.8.A=HELD`; `FALSE_PASS=NO`; `FALSE_LISTO=NO`; `SCOPE_LEAK=NO`.

## 2026-09-04 — Cierre Fase 3 VAEP MASTER — Runtime Jules

Responsable: ChatGPT/VAEP sobre `Desarrollo`.

Se implementó el hardening aprobado del runtime Jules A/B/C/D. `.github/scripts/vaep-jules-master.sh` ahora clasifica cero manifests como `NO_OP/exit 0` sin crear sesión/attempt/ownership/recovery y conserva fail-closed para múltiples manifests. El presupuesto interno permanece gobernado exclusivamente por `JULES_LANE_BUDGET_SECONDS` desde MASTER; los ocho workflows A/B/C/D + recovery mantienen `timeout-minutes: 25` únicamente como safety-net externo y no contienen overrides 5400/1080.

El worker persiste estado causal de la sesión antes y después de adquirir sesión Jules. Al timeout, MASTER intenta una señal de detención mediante la operación Jules ya conocida `:sendMessage`; no se inventa un endpoint de cancelación. Independientemente del resultado remoto, el timeout revoca ownership local, marca la sesión `SUPERSEDED`, libera la lane, produce evidence JSON con `MASTER_COMMIT_SHA` + `AUTOMATION_POLICY_HASH`, crea un Issue durable `[VAEP-JULES-SUPERSEDED]` y deriva a `QA_TAKEOVER_AND_ASSIGN_NEXT_SAFE_IMMEDIATELY`.

El worker consulta el ledger de supersession antes de crear/reanudar sesión y nuevamente antes de publicar un resultado. Un resultado tardío superseded se convierte en `LATE_RESULT_SUPERSEDED`, queda `lateResultAutoIntegrationDenied=true` y no puede terminar como COMPLETED integrable.

Los auxiliares stop/session-health/feedback/diagnostic fueron ligados explícitamente a MASTER. Se corrigió además el heredoc YAML inválido preexistente de `.github/workflows/vaep-jules-diagnostic.yml`; el run PR `33911803009` volvió a ejecutar job y terminó SUCCESS. El gate causal del runtime `VAEP engine lightweight checks` push `33911698939` terminó SUCCESS con Validate VAEP/Jules MASTER y todos los VAEP self-tests en SUCCESS. `Solqaryn CI=SKIPPED` queda excluido como PASS.

Resultado: `MIGRATION_PHASE_3=CLOSED/PASS`; `NO_OP=PASS`; `MULTI_MANIFEST_FAIL_CLOSED=PASS`; `TIMEOUT_SUPERSESSION=PASS`; `LATE_RESULT_GUARD=PASS`; `DURABLE_TIMEOUT_EVIDENCE=PASS`; `FASE_4=NOT_STARTED`; `CURRENT_PARENT=N4.7.H`; `N4.8.A=HELD`; `FALSE_PASS=NO`; `FALSE_LISTO=NO`; `SCOPE_LEAK=NO`.

## 2026-09-04 — Cierre Fase 4 VAEP MASTER — Historical writers

Responsable: ChatGPT/VAEP sobre `Desarrollo`.

Se retiraron los siete writers históricos aprobados de Fase 4: `vaep-control-plane-ci-guard.yml`, `vaep-n36h-exact-publish.yml`, `vaep-n37h-exact-publish.yml`, `vaep-n38h-exact-publish.yml`, `vaep-n39h-exact-publish.yml`, `vaep-n310h-exact-publish.yml` y `vaep-n311h-exact-changelog-publisher.yml`. Todos contenían autoridad de escritura histórica sobre `Desarrollo` mediante `contents: write`, `git commit`, `git push` y/o `git reset --hard`.

`.github/workflows/ci.yml` fue auditado por separado. Se preservaron sus jobs de backend, frontend y aceptación, sus builds/tests, MySQL temporal, Playwright y artifacts. Se redujo `permissions.contents` de `write` a `read` y se eliminó únicamente el step que publicaba migración/SQL mediante commit/push a `agent/mejoras-solqaryn`.

Después del changeset se auditaron los 38 workflows restantes. Resultado: `contents: write=0`, `git push=0`, `git commit=0`, `git reset --hard=0`, `update-ref/force-push=0`. El CI de producto permanece presente; no se eliminó ninguna validación funcional de backend/frontend/acceptance.

Evidencia causal del commit `e1ff079ef8645da4c1cc4bff8e9967b8d31ed954`: `VAEP engine lightweight checks #33912398582=SUCCESS` y `VAEP Jules Diagnostic #33912398627=SUCCESS`. `Solqaryn CI=SKIPPED` queda explícitamente excluido como PASS.

Resultado: `MIGRATION_PHASE_4=CLOSED/PASS`; `HISTORICAL_GIT_WRITERS=0`; `UNAUTHORIZED_COMMIT_PUSH_RESET=0`; `PRODUCT_CI_PRESERVED=PASS`; `FASE_5=NOT_STARTED`; `CURRENT_PARENT=N4.7.H`; `N4.8.A=HELD`; `FALSE_PASS=NO`; `FALSE_LISTO=NO`; `SCOPE_LEAK=NO`.

## 2026-09-04 — Cierre Fase 5 VAEP MASTER — Cleanup documental y manifests

Responsable: ChatGPT/VAEP sobre `Desarrollo`.

Se completó el semantic-diff de los cuatro documentos protocolarios versionados contra `docs/VAEP_AUTHORITY.md`. Resultado: `UNIQUE_LIVE_RULES_NOT_IN_MASTER=0`. Retry cap, pre-session sin consumo de attempt, doble self-review, parent-close, zero-idle/failover, QA takeover y reglas de seguridad ya están representados por MASTER; los objetivos Sprint40/Rolling40 y labels/checkpoints numéricos de esos archivos son evidencia histórica expirada. Por ello se retiraron del árbol activo `docs/VAEP_V320_RETRY_CAP.md`, `docs/VAEP_V320_SPRINT40_QUEUE.md`, `docs/VAEP_V321_PARENT40_QUEUE.md` y `docs/VAEP_V321_PARENT_CLOSURE.md`, preservando íntegramente Git history.

Antes del cleanup existían 2795 manifests JSON dentro de rutas de dispatch activas: A=780, B=693, C=656, D=663 y `.vaep/jules/dispatch`=3. Todos eran artifacts de transporte ya comprometidos/disparados. El estado vigente mantiene `N4.7.A-G=LISTO_REAL`; el único manifest N4.7.H en las rutas canónicas, `VAEP-MASTER-JULES-B-N47H-DOC-CERT-B1-20260904T0952HND`, ya produjo Jules `COMPLETED` con Issue #2654 y quedó bajo REVIEW_FIRST del controller, por lo que tampoco representa trabajo pendiente dentro del filesystem de dispatch.

El commit `e34c148a06a2f054a40c790d28d7aaf01e30eedb` limpia todos los JSON históricos de las cuatro rutas canónicas, mantiene cada ruta con `.gitkeep` y elimina por completo `.vaep/jules/dispatch`. Git/Issues/artifacts conservan la evidencia histórica. La limpieza validó el comportamiento de runtime requerido: A #33913519604, B #33913519565, C #33913519551 y D #33913519538 ejecutaron `VAEP/Jules MASTER` y finalizaron SUCCESS sin artifact, confirmando `NO_OP` frente a un commit de cleanup sin manifest añadido. `VAEP engine lightweight checks #33913519654=SUCCESS` y `VAEP Jules Diagnostic #33913524944=SUCCESS`.

No se utilizó `[skip ci]` como mecanismo de seguridad. Los labels protocolarios numéricos que sobrevivían únicamente dentro de prompts históricos N4.7 salieron de las rutas activas junto con esos manifests. Los próximos dispatches deben nacer desde MASTER.

Resultado: `MIGRATION_PHASE_5=CLOSED/PASS`; `UNIQUE_LIVE_RULES_NOT_IN_MASTER=0`; `ACTIVE_VERSIONED_PROTOCOL_DOCS=0`; `HISTORICAL_MANIFESTS_IN_ACTIVE_PATHS=0`; `DOT_VAEP_DISPATCH=0`; `CLEANUP_TRIGGER=PASS`; `FASE_6=NOT_STARTED`; `CURRENT_PARENT=N4.7.H`; `N4.8.A=HELD`; `FALSE_PASS=NO`; `FALSE_LISTO=NO`; `SCOPE_LEAK=NO`.

## 2026-09-04 — Cierre Fase 6 VAEP MASTER — AntiG RESERVED_INACTIVE

Responsable: ChatGPT/VAEP sobre `Desarrollo`.

Se formalizó técnicamente el estado reservado de AntiG sin eliminar su infraestructura. MASTER contiene `ANTIG_STATUS=RESERVED_INACTIVE`, `ANTIG_OPERATIONAL_NOW=FALSE`, `ANTIG_SCHEDULER=DISABLED`, `ANTIG_HANDOFF_PROCESSING=DISABLED`, `ANTIG_AUTHORITY=MASTER`, `ANTIG_CAN_CERTIFY_LISTO_REAL=FALSE` y `ANTIG_FUTURE_REINCORPORATION=EXPLICIT_AUTHORIZATION_REQUIRED`.

Los seis componentes requeridos permanecen en el árbol. El agente está deshabilitado para uso operativo; el worker es un shim fail-closed que no consume handoffs; el instalador no contiene una ruta de creación de scheduler y solo conserva self-test + eliminación de tarea heredada. El schema permanece como contrato técnico dormido y no permite `LISTO_REAL`. El runbook describe únicamente el estado vigente y la reincorporación futura autorizada.

Los tres checkboxes históricos de activación pendiente quedaron cerrados como estado reservado: `ANTIG_CURRENT_QUEUE_ITEM=NO`.

Resultado: `MIGRATION_PHASE_6=CLOSED/PASS`; `ANTIG_STATUS=RESERVED_INACTIVE`; `ANTIG_OPERATIONAL_NOW=FALSE`; `ANTIG_SCHEDULER=DISABLED`; `ANTIG_HANDOFF_PROCESSING=DISABLED`; `ANTIG_CURRENT_QUEUE_ITEM=NO`; `ANTIG_CAN_CERTIFY_LISTO_REAL=FALSE`; `FASE_7=NOT_STARTED`; `CURRENT_PARENT=N4.7.H`; `N4.8.A=HELD`; `FALSE_PASS=NO`; `FALSE_LISTO=NO`; `SCOPE_LEAK=NO`.

## 2026-09-04 — Certificación final VAEP MASTER — Fases 0 a 7

Responsable: ChatGPT/VAEP sobre `Desarrollo`.

Se certifica el cierre integral de la migración F0-F7 hacia el MAESTRO único `docs/VAEP_AUTHORITY.md`. Durante REVIEW_FIRST final se detectó y corrigió una regresión real: el control técnico `vaep/control/dispatch-admission.json` había sido retirado porque no estaba definido por MASTER. La corrección de Fase 7 incorpora la semántica de admisión dentro del mismo MAESTRO y reintroduce el state machine como consumidor subordinado, eliminando la contradicción de autoridad.

El commit de implementación `66d4ded1fca2f51854a50ca3f6a44725dc6c1ef6` dejó admisión `FROZEN` y pasó `VAEP engine lightweight checks #33917014608=SUCCESS` y `VAEP Jules Diagnostic #33917014756=SUCCESS`. El self-test cubre `NO_OP`, multi-manifest fail-closed, `FROZEN`, `OPEN`, state inválido, clave desconocida y state ausente; el rechazo `FROZEN` ocurre antes de sesión/attempt/ownership/recovery. `Solqaryn CI=SKIPPED` no se usa como PASS.

La certificación integral revalida: un único bloque de política machine-readable; parser fail-closed sin `source`/`eval`; `MASTER_COMMIT_SHA` y `AUTOMATION_POLICY_HASH` en evidencia; runtime Jules con timeout/supersession/late-result guard; cero protocol docs versionados activos; cero manifests históricos en rutas activas; cero historical Git writers; CI de producto preservado; AntiG `RESERVED_INACTIVE`; cinco tareas `VAEP MASTER 00/15/30/45/55` enabled con telemetría reciente y prompts MASTER-bound.

La cadena end-to-end real queda respaldada por el dispatch N4.7.H, Jules B run `33892158842`, artifact `9944920873`, Issue #2654, REVIEW_FIRST + CI y el cierre/handoff registrado en BITACORA/COLA. Estado operativo fresco: `N4.7.H=LISTO_REAL RE-CERTIFIED` y `CURRENT_PARENT=N4.8.A`; snapshots anteriores que todavía mostraban H held quedan superseded como historia de fase.

Tras superar el gate causal de implementación, el control `vaep/control/dispatch-admission.json` se abre con `newDispatchAdmission=OPEN` para el retorno operativo. PR #2 permanece `OPEN+DRAFT`; no hubo merge, force-push ni cambio sobre main/Producción/secrets/deploy.

Resultado final: `MIGRATION_PHASE_7=CLOSED/PASS`; `MIGRATION_F0_F7=CLOSED/PASS`; `NEW_DISPATCH_ADMISSION=OPEN`; `MASTER_UNIQUE=PASS`; `FIVE_AUTOMATIONS=PASS`; `END_TO_END_FLOW=PASS`; `FALSE_PASS=NO`; `FALSE_LISTO=NO`; `SCOPE_LEAK=NO`.

## 2026-09-04 — Remediación post-auditoría Codex

Responsable: ChatGPT/VAEP sobre `Desarrollo`.

La auditoría externa de Codex detectó tres deudas reales de endurecimiento: 54 manifests históricos residuales en `vaep/jules-a/dispatch/`, ambigüedad de snapshots antiguos en `TASKS.md` para parsers ingenuos y tres constantes operativas aún fuera del bloque machine-readable del MAESTRO.

Se eliminó del árbol activo la ruta legacy `vaep/jules-a/dispatch/` con sus 54 JSON, preservando íntegramente Git history. `TASKS.md` ahora declara explícitamente que no es fuente machine-readable de estado vigente; `CONFIG/COLA/BITACORA` frescos siguen siendo la fuente operativa.

El bloque `BEGIN_AUTOMATION_POLICY` conserva una sola instancia y pasa de 6 a 9 claves, incorporando `PARENT_STALL_NO_PROGRESS_MINUTES`, `MAX_VOLUNTARY_IDLE` y `VAEP_CHECKPOINTS`. El parser fail-closed, `vaep-jules-master.sh` y `vaep-jules-worker.sh` fueron actualizados para consumir esas claves; se retiró el literal runtime de checkpoints.

Evidencia causal del hardening: `VAEP engine lightweight checks #33920294318=SUCCESS` y `VAEP Jules Diagnostic #33920294338=SUCCESS` sobre `09ee682712ba29d79d235a62415de20c308db7c9`. `Solqaryn CI=SKIPPED` queda excluido como PASS.

El punto de Codex sobre las cinco automatizaciones era una limitación de auditoría Git-only, no una ausencia operativa: `VAEP MASTER 00/15/30/45/55` están presentes y habilitadas en el control-plane externo. La imposibilidad local de ejecutar Bash en su entorno Windows tampoco se usa como PASS; la validación causal proviene de GitHub Actions.

Resultado: `CODEX_AUDIT_REMEDIATION=PASS`; `LEGACY_JULES_A_MANIFESTS=0`; `MASTER_POLICY_KEYS=9`; `TASKS_MACHINE_CURRENT_STATE=PROHIBITED`; `FIVE_AUTOMATIONS_EXTERNAL_EVIDENCE=PASS`; `FALSE_PASS=NO`.



## VAEP dependency-safe closure reconciliation

Controller: CHATGPT_BUSINESS / canonical checkpoint.
Validated parent: `GATE-N4`; successor: `N5.1.A`.
Base: `560f51596734900edf7fcba73235cda5b96922c7`. Existing closure receipt and exact-head causal gates validated.
Selector uses explicit roadmap dependencies; no lexical ordering or gate bypass.
Admission transition is guarded; no production, merge or secret changes.


## VAEP dependency-safe closure reconciliation

Controller: CHATGPT_BUSINESS / canonical checkpoint.
Validated parent: `N5.1.B`; successor: `N5.1.C`.
Base: `936eb5d16eb6bde8afdfc9087aa0e10f44984088`. Existing closure receipt and exact-head causal gates validated.
Selector uses explicit roadmap dependencies; no lexical ordering or gate bypass.
Admission transition is guarded; no production, merge or secret changes.


## VAEP dependency-safe closure reconciliation

Controller: CHATGPT_BUSINESS / canonical checkpoint.
Validated parent: `N5.1.C`; successor: `N5.1.D`.
Base: `ded69fe8f25c2de77e41b0acdf32fc5dbff87726`. Existing closure receipt and exact-head causal gates validated.
Selector uses explicit roadmap dependencies; no lexical ordering or gate bypass.
Admission transition is guarded; no production, merge or secret changes.


## VAEP dependency-safe closure reconciliation

Controller: CHATGPT_BUSINESS / canonical checkpoint.
Validated parent: `N5.1.H`; successor: `N5.2.A`.
Base: `45f8e9e3da51470cfd4e1b742ee2eba55b8b3bcf`. Existing closure receipt and exact-head causal gates validated.
Selector uses explicit roadmap dependencies; no lexical ordering or gate bypass.
Admission transition is guarded; no production, merge or secret changes.


## VAEP dependency-safe closure reconciliation

Controller: CHATGPT_BUSINESS / canonical checkpoint.
Validated parent: `N5.2.B`; successor: `N5.2.C`.
Base: `35c87f161819f40e680d9034a202d60fbac2743b`. Existing closure receipt and exact-head causal gates validated.
Selector uses explicit roadmap dependencies; no lexical ordering or gate bypass.
Admission transition is guarded; no production, merge or secret changes.


## VAEP dependency-safe closure reconciliation

Controller: CHATGPT_BUSINESS / canonical checkpoint.
Validated parent: `N5.3.A`; successor: `N5.3.B`.
Base: `ecadca71adb7a85dfc12666ab7db504cf0d7ddfc`. Existing closure receipt and exact-head causal gates validated.
Selector uses explicit roadmap dependencies; no lexical ordering or gate bypass.
Admission transition is guarded; no production, merge or secret changes.


## VAEP dependency-safe closure reconciliation

Controller: CHATGPT_BUSINESS / canonical checkpoint.
Validated parent: `N5.3.C`; successor: `N5.3.D`.
Base: `0fe20816e5ec5347296cc666ed03e88983850420`. Existing closure receipt and exact-head causal gates validated.
Selector uses explicit roadmap dependencies; no lexical ordering or gate bypass.
Admission transition is guarded; no production, merge or secret changes.


## 2026-09-11 — ERP-N6.4 Usuarios por empresa — cierre documental append-only

**Responsable:** CHATGPT_VAEP / VAEP :12 Recovery.

**Objetivo/alcance:** resolver de forma estrictamente aditiva y byte-preserving el único P1 documental abierto de `N6.4.H`. `TASKS.md` y `docs/CERTIFICACION_N6_4_USUARIOS_POR_EMPRESA.md` conservan la evidencia funcional y de certificación; esta entrada no reabre código de producto ni adelanta `N6.5`.

**Evidencia:** `N6.4.A–G=LISTO_REAL`; functional head final `3f788ec78b03b820592c7a514fa3a63e345b3754`; baseline documental `375c7f76625a2b23b1acbd839e368b3d0e5a11e3`; REVIEW_FIRST bloqueante `vaep/evidence/reviews/N6.4.H_REVIEW_FIRST_20260911T2041Z_SUP24.json`. La lectura conectada confirmó el cuerpo completo de `CHANGELOG_AI.md` y el blob exacto `f30be5c7d586e74fc7ba945f8ab95b60a3cb1cd9` antes del append.

**Control:** esta publicación cierra únicamente el P1 de append history-preserving. No declara por sí sola `N6.4.H=LISTO_REAL`: requiere REVIEW_FIRST fresco P0=0/P1=0, gates causales terminales exact-head o equivalencia demostrada, receipt H y reconciliación canónica antes de promover `N6.5.A`. Sin cambios a `main`, Producción, secretos, deploys ni PR #2.

## 2026-09-12 — ERP-N6.8 Storage aislado — cierre documental append-only

**Responsable:** CHATGPT_VAEP / Tarea Supervisión :00.

**Objetivo/alcance:** reconciliar de forma estrictamente aditiva/history-preserving el cierre documental de `N6.8.H`, sin reabrir código runtime ni adelantar `N6.9`. La certificación canónica es `docs/CERTIFICACION_N6_8_STORAGE_AISLADO.md`; el functional storage candidate permanece en la evidencia ya certificada de `N6.8.G`.

**Evidencia:** `N6.8.A–G=LISTO_REAL`; candidate de storage/TEST_CI `f5d29e7471762a3fe6fe735ba98c2ad1f0188727`; causal storage-isolation run `34714029058`, job `103607801664=SUCCESS`; material DOC_CERT `dae43dfc5a3211643565c7002abcc67a845ef179`; REVIEW_FIRST bloqueante `6acb02e87d7cbde013fe2bed2ff79a7a9b068482` / `vaep/evidence/reviews/N6.8.H_REVIEW_FIRST_20260912T2002Z.json` con `P0=0/P1=2` exclusivamente por `CHANGELOG_AI.md` y `TASKS.md`.

**Control:** esta publicación resuelve el P1 de `CHANGELOG_AI.md` de forma append-only. No declara por sí sola `N6.8.H=LISTO_REAL`: requiere reconciliar `TASKS.md`, rerun REVIEW_FIRST con P0=0/P1=0, DoD PASS y receipt persistido/releído antes de promover `N6.9.A`. Sin cambios a `main`, Producción, secretos, deploys ni PR #2.

## 2026-09-13 — ERP-N7.1 Outbox Pattern — cierre documental append-only

**Responsable:** CHATGPT_VAEP / VAEP :12 Recovery.

**Objetivo/alcance:** resolver de forma estrictamente aditiva y byte-preserving el único P1 documental abierto de `N7.1.H`, sin reabrir el runtime de Outbox ni adelantar `N7.2`.

**Evidencia:** `N7.1.A–G=LISTO_REAL`; baseline seguro previo `6a4a8df9b4397a8028c74b50870295ca7d940cd7`; blob fuente exacto de `CHANGELOG_AI.md` `e48e7f339e09f385df329591eb1806fc33978323`. El append se publica sobre el árbol restaurado `5d186f061a3b7bc1545e27002b3fa455aa582bc6` y debe verificarse como único archivo modificado, con `deletions=0`.

**Control:** esta publicación resuelve únicamente el P1 de `CHANGELOG_AI.md`. No declara por sí sola `N7.1.H=LISTO_REAL`: requiere REVIEW_FIRST fresco `P0=0/P1=0`, receipt H persistido/releído y reconciliación canónica antes de promover `N7.2.A`. Sin cambios a `main`, Producción, secretos, deploys ni PR #2.

## 2026-09-13 — ERP-N7.2 Retry controlado del Outbox — reconciliación documental append-only

**Responsable:** CHATGPT_VAEP / VAEP :12 Recovery.

**Objetivo/alcance:** resolver `CHANGELOG_AI_ADDITIVE_RECONCILIATION` de `N7.2.H` de forma estrictamente aditiva/history-preserving, sin reabrir runtime ni adelantar `N7.3`.

**Evidencia:** `N7.2.A–G=LISTO_REAL`; certificación canónica `docs/CERTIFICACION_N7_2_RETRY.md` en `3fda2525d0a9f0931e25cfa146c372c2e48fb5f5`; N7.2.G candidate congelado `6f297cd6107312ad9e301491d47d9f88b5497539`, gate exact-head run `34757554438`, job `103724457658=SUCCESS`, receipt `vaep/evidence/fragments/N7.2.G_LISTO_REAL_20260913T124538Z.json`. `TASKS.md` quedó reconciliado aditivamente en `e3bc89479493b163c99ccf53e7a52d8047342c44`; REVIEW_FIRST vigente `vaep/evidence/reviews/N7.2.H_REVIEW_FIRST_20260913T140109Z_SUP48.json` sobre control-head `695adaf21df85b872b27d51d9f18822c381793c4` dejó P0=0/P1=1 exclusivamente por `CHANGELOG_AI_ADDITIVE_RECONCILIATION`.

**Control:** esta publicación resuelve únicamente el P1 de `CHANGELOG_AI.md`. `N7.2.H` no se declara `LISTO_REAL` hasta REVIEW_FIRST fresco P0=0/P1=0, gates causales exact-head aplicables PASS y receipt persistido/releído. Sin cambios a `main`, Producción, secretos, deploys ni PR #2.

## 2026-09-13 — ERP-N7.3 Dead-letter del Outbox — reconciliación documental append-only

**Responsable:** CHATGPT_VAEP / Tarea Supervisión :48.

**Objetivo/alcance:** resolver `CHANGELOG_AI_ADDITIVE_RECONCILIATION` de `N7.3.H` de forma estrictamente aditiva/history-preserving, sin reabrir runtime ni adelantar el sucesor.

**Evidencia:** `N7.3.A–G=LISTO_REAL`; certificación canónica `docs/CERTIFICACION_N7_3_DEAD_LETTER.md`; N7.3.G functional candidate `2c19221fdbb0a3cdfdcf8afdc839999bf8e80265`, REVIEW_FIRST `67be3a7aff8f6ce77bb6de27f1a96187452565ad` con `P0=0/P1=0`, receipt `vaep/evidence/receipts/N7.3.G_LISTO_REAL_20260913T191310Z.json` en `1f5a62910b3a84b99ee90b45d204ee2fcf240f75`; `TASKS.md` quedó reconciliado de forma aditiva/history-preserving en `4ed8cfce16ad00b735194280b73f42c5e28e58a2`. La certificación N7.3 registra 17/17 criterios de aceptación satisfechos.

**Control:** esta publicación resuelve únicamente el P1 de `CHANGELOG_AI.md`. `N7.3.H` no se declara `LISTO_REAL` hasta REVIEW_FIRST fresco `P0=0/P1=0`, gates causales exact-head aplicables PASS y receipt persistido/releído. Sin cambios a `main`, Producción, secretos, deploys ni PR #2.

## 2026-09-13 — ERP-N7.4 Idempotencia — reconciliación documental append-only

**Responsable:** CHATGPT_VAEP / Tarea Supervisión :48.

**Objetivo/alcance:** resolver `CHANGELOG_AI_ADDITIVE_RECONCILIATION` de `N7.4.H` de forma estrictamente aditiva/history-preserving, sin reabrir runtime ni adelantar `N7.5`.

**Evidencia:** `N7.4.A–G=LISTO_REAL`; certificación canónica `docs/CERTIFICACION_N7_4_IDEMPOTENCIA.md` en `6097f60ff68d163878d5d3d85c590c59f0cd2f75`; functional candidate `1c366562a90c590cc2925a153298fd1b758e8dab`; causal gates `34782712890/103792518381`, `34782712890/103792518352`, `34782712890/103792518396`, `34782712884/103792518243` y `34782712884/103792518046` en `SUCCESS`; receipt G `vaep/evidence/receipts/N7.4.G_LISTO_REAL_20260913T220100Z_SUP48.json`.

**Control:** esta publicación resuelve únicamente el P1 de `CHANGELOG_AI.md`. `N7.4.H` no se declara `LISTO_REAL` hasta resolver también `TASKS_ADDITIVE_STATE_RECONCILIATION`, repetir REVIEW_FIRST con `P0=0/P1=0`, demostrar equivalencia funcional y persistir/releer receipt H. Sin cambios a `main`, Producción, secretos, deploys ni PR #2.

## 2026-09-14 — ERP-N7.5 Webhooks seguros — reconciliación documental append-only

**Responsable:** CHATGPT_VAEP / recovery byte-exacto N7.5.H.

**Objetivo/alcance:** resolver `TASKS_ADDITIVE_STATE_RECONCILIATION` y `CHANGELOG_AI_ADDITIVE_RECONCILIATION` de `N7.5.H` de forma estrictamente aditiva/history-preserving, sin reabrir runtime de webhooks ni adelantar `N7.6`.

**Evidencia:** `N7.5.A-G=LISTO_REAL`; certificación canónica `docs/CERTIFICACION_N7_5_WEBHOOKS.md`; functional candidate `dbd515909d98893f4924bb73d0a48f74fe9b97c3`; gates causales exact-head `34800735379/103842737649=SUCCESS` y `34800735411/103842739664=SUCCESS`; receipt G `vaep/evidence/receipts/N7.5.G_LISTO_REAL_20260914T030250Z_SUP36.json`. El probe off-ref `350f27db5040be75f7d84e78cbaa8e802f5cbf20` fue rechazado y nunca publicado porque mutaba historia; este recovery usa append de bytes al EOF sobre los blobs exactos vigentes.

**Control:** esta publicación resuelve únicamente los dos P1 documentales mediante append byte-exacto. No declara por sí sola `N7.5.H=LISTO_REAL`: exige REVIEW_FIRST fresco `P0=0/P1=0`, equivalencia funcional, compare de `TASKS.md` y `CHANGELOG_AI.md` con `additions>0/deletions=0`, receipt H persistido/releído y sólo entonces promoción de `N7.6.A`. Sin cambios a `main`, Producción, secretos, deploys ni PR #2.

## 2026-09-14 — ERP-N7.6 Integración API de WhatsApp Business — reconciliación documental append-only

**Responsable:** VAEP / DOC_CERT N7.6.H.

**Objetivo/alcance:** cerrar documentalmente la cadena N7.6 sin reabrir lógica ya certificada ni ampliar alcance. La reconciliación preserva byte-for-byte toda historia previa de `TASKS.md` y `CHANGELOG_AI.md`; el estado machine-readable continúa exclusivamente en `CONFIG/COLA` bajo `docs/VAEP_AUTHORITY.md`.

**Evidencia funcional:** `N7.6.A-G=LISTO_REAL`; candidate funcional final `abb4a3bfdbe0d2896abcf33e5c9547e1dfc1016b`; receipts B-G en `vaep/evidence/receipts/`. El alcance certificado incluye configuración tenant-scoped con referencias opacas a secretos, persistencia/migración, boundary API fail-closed, frontend con tenant verificado/RBAC reactivo/estado truthful, controles de seguridad y regresión exact-head. N7.6.G certificó gates `34864310838/104044575850`, `34864310860/104044621837`, `34864311032/104044562880`, `34864310684/104044559652` y `34864310645/104044379586` en `SUCCESS`, reutilizando además la migración causal N7.6.C `34841294854` sin schema delta posterior.

**Control:** esta publicación resuelve únicamente `TASKS_ADDITIVE_STATE_RECONCILIATION` y `CHANGELOG_AI_ADDITIVE_RECONCILIATION` mediante append byte-exacto. No declara por sí sola `N7.6.H=LISTO_REAL`: exige hard verify de prefijo/tamaño, REVIEW_FIRST fresco P0=0/P1=0, equivalencia funcional, certificación canónica releída y receipt H persistido/releído antes de promover `N7.7.A`. Sin cambios a `main`, Producción, deploys, secretos ni PR #2.

## 2026-09-14 — ERP-N7.7 Email empresarial — reconciliación documental append-only

**Responsable:** VAEP / DOC_CERT N7.7.H.

**Objetivo/alcance:** cerrar documentalmente N7.7 sin reabrir lógica ya certificada ni ampliar alcance. Esta entrada y el rollup de `TASKS.md` preservan byte-for-byte sus históricos previos.

**Evidencia funcional:** N7.7.A-G=`LISTO_REAL`; candidate funcional `7a0765aa37533b8df2e52807f0a63800872c009d`; receipt G `vaep/evidence/receipts/N7.7.G_LISTO_REAL_20260914T181734Z_SUP12.json`; certificación `docs/CERTIFICACION_N7_7_EMAIL_EMPRESARIAL.md`. N7.7.H no introduce delta de producto ni schema.

**Control:** esta publicación resuelve los P1 documentales mediante append byte-exacto. No declara por sí sola `N7.7.H=LISTO_REAL`: exige hard verify de prefijo/tamaño, REVIEW_FIRST fresco P0=0/P1=0, equivalencia funcional y receipt H persistido/releído antes de promover N7.8.A. Sin cambios a `main`, Producción, deploys, secretos ni PR #2.


## 2026-09-14 — ERP-N7.8 Pagos online — reconciliación documental append-only

**Responsable:** VAEP / DOC_CERT N7.8.H.

**Objetivo/alcance:** cerrar documentalmente N7.8 sin reabrir lógica ya certificada ni ampliar alcance. Esta entrada y el rollup de `TASKS.md` preservan byte-for-byte sus históricos previos.

**Evidencia funcional:** N7.8.A-G=`LISTO_REAL`; candidate funcional `b217cc00bfa9bfc452674f0bdacbef52e50186a7`; receipt G `vaep/evidence/receipts/N7.8.G_LISTO_REAL_20260914T212600Z_SUP48.json`; certificación `docs/CERTIFICACION_N7_8_PAGOS_ONLINE.md`. N7.8.F resolvió el P1 de checkout inseguro exigiendo URL absoluta HTTPS y añadió prueba negativa dirigida. N7.8.G certificó los gates aplicables; el fallo del run integral `34896693703` quedó probado no causal por pertenecer a suites legacy/global de frontend sin delta frontend atribuible a N7.8. N7.8.H no introduce delta de producto ni schema.

**Control:** esta publicación resuelve los P1 documentales mediante append byte-exacto. No declara por sí sola `N7.8.H=LISTO_REAL`: exige hard verify de prefijo/tamaño, REVIEW_FIRST fresco P0=0/P1=0, equivalencia funcional y receipt H persistido/releído antes de promover el sucesor dependency-valid. Sin cambios a `main`, Producción, deploys, secretos ni PR #2.

## 2026-09-14 — ERP-N7.9 Ecommerce — reconciliación documental append-only

**Responsable:** VAEP / DOC_CERT N7.9.H.

**Objetivo/alcance:** cerrar documentalmente N7.9 sin reabrir lógica ya certificada ni ampliar alcance. Esta entrada y el rollup de `TASKS.md` preservan byte-for-byte sus históricos previos.

**Evidencia funcional:** N7.9.A-G=`LISTO_REAL`; functional candidate final `5de96492290218639f18c1648668215339c34a1e`; receipt G `vaep/evidence/receipts/N7.9.G_LISTO_REAL_20260914T230920Z_SUP00.json`; REVIEW_FIRST G P0=0/P1=0/P2=0. El workflow causal `34905794077` terminó con backend Release/pruebas, Docker, frontend producción, higiene y MySQL/migraciones en `SUCCESS`; backend registró 2245 passed, 0 failed, 0 skipped. Los commits posteriores al candidate hasta el receipt G son exclusivamente evidencia y no introducen delta funcional de producto/schema.

**Control:** esta publicación resuelve los P1 documentales mediante append byte-exacto. No declara por sí sola `N7.9.H=LISTO_REAL`: exige hard verify de prefijo/tamaño, REVIEW_FIRST fresco P0=0/P1=0, equivalencia funcional y receipt H persistido/releído antes de promover `N7.10.A`. Sin cambios a `main`, Producción, deploys, secretos ni PR #2.

## 2026-09-14 — ERP-N7.10 Facturación fiscal/electrónica — reconciliación documental append-only

**Responsable:** VAEP / DOC_CERT N7.10.H.

**Objetivo/alcance:** cerrar documentalmente N7.10 sin reabrir lógica ya certificada ni ampliar alcance. Esta entrada y el rollup de `TASKS.md` preservan byte-for-byte sus históricos previos.

**Evidencia funcional:** N7.10.A-G=`LISTO_REAL`; functional candidate final `903901c6b30a4a2d70b4a52dc440ef8fc61adc5b`; receipt F `vaep/evidence/receipts/N7.10.F_LISTO_REAL_20260915T011651Z_SUP00.json`; receipt G `vaep/evidence/receipts/N7.10.G_LISTO_REAL_20260915T012330Z_SUP00.json`; REVIEW_FIRST F/G P0=0/P1=0/P2=0. El workflow causal `34916355954` terminó con backend Release/pruebas, Docker, frontend producción, higiene y MySQL/migraciones/integración en `SUCCESS`; backend registró 2263 passed, 0 failed, 0 skipped. El gate suplementario `34916355934` terminó `SUCCESS` para restore aislado y seguridad/tenant/secrets. Los commits posteriores al candidate hasta el receipt G son exclusivamente evidencia.

**Seguridad y operación:** la emisión fiscal permanece autenticada, permission-gated, tenant-aware, idempotente y fail-closed ante configuración/proveedor inválido. La auditoría evita claves de idempotencia, hash de snapshot, payloads del proveedor, referencias externas y excepciones crudas. No se introdujo webhook fiscal ni se certifica una superficie inexistente.

**Control:** esta publicación resuelve los rollups documentales mediante append byte-exacto. No declara por sí sola `N7.10.H=LISTO_REAL`: exige hard verify de prefijo/tamaño, REVIEW_FIRST fresco P0=0/P1=0, equivalencia funcional y receipt H persistido/releído antes de promover `GATE-N7`. Sin cambios a `main`, Producción, deploys, secretos ni PR #2.

## 2026-09-15 — ERP-N8.2 Compatibilidad de dispositivos — reconciliación documental H

**Responsable:** Tarea Supervisión :48 bajo `docs/VAEP_AUTHORITY.md`.

Se cerró la cobertura causal de N8.2 para escritorio, laptop, tablet, Android e iPhone. El P1 inicial por falta de perfiles explícitos se resolvió same-run con `frontend/e2e/n82-device-compatibility.spec.ts` y el gate dedicado `.github/workflows/n8-2-device-compatibility.yml`. El workflow `N8.2 - Compatibilidad de dispositivos` run `34941980085`, job `104292533908`, terminó `success` sobre `1f7008f464f195aab3020a5d0102a85428c8919a`; lint, build de producción y Playwright dirigido quedaron PASS.

N8.2.E, N8.2.F y N8.2.G quedaron `LISTO_REAL` mediante receipts VAEP con REVIEW_FIRST P0=0/P1=0/P2=0. No hubo delta de runtime backend, persistencia, migraciones, RBAC, límites tenant, deploy, Producción, secretos ni PR #2. La certificación canónica se materializó en `docs/CERTIFICACION_N8_2_DISPOSITIVOS.md`.

Este registro es histórico y no falsea H: N8.2.H sólo es `LISTO_REAL` cuando exista su receipt final tras REVIEW_FIRST documental y readback/reconciliación de control-plane.

## 2026-09-15 — ERP-N8.3 Compatibilidad de navegadores — reconciliación documental H

**Responsable:** Tarea Supervisión :48 bajo `docs/VAEP_AUTHORITY.md`.

Se cerró la cobertura causal de compatibilidad del storefront en Chromium, Firefox y WebKit sobre el functional candidate `b833e44a976f92a426fff7f969a3f3f757234f20`. El workflow `N8.3 - Compatibilidad de navegadores`, run `34943377733`, job `104296994230`, terminó `success`; lint, build de producción y Playwright dirigido quedaron PASS en los tres motores.

N8.3.F y N8.3.G quedaron `LISTO_REAL` con REVIEW_FIRST P0=0/P1=0/P2=0. El workflow usa permisos `contents: read`, no consume secretos y sirve Angular únicamente en `127.0.0.1`. N8.3 no introduce delta backend, persistencia, migraciones, autenticación, RBAC, Producción ni deploy. La certificación canónica se materializó en `docs/CERTIFICACION_N8_3_NAVEGADORES.md`.

Este registro es histórico y no falsea H: N8.3.H sólo es `LISTO_REAL` cuando exista su REVIEW_FIRST documental, receipt final y readback/reconciliación del control-plane.

- N8.1.G delegated browser UAT captured on Desarrollo at 2026-09-15T15:10:49.1317092Z (exact head 0e1854c42941e3eaf74af88628f99f197ddfbf89): real CUA observations cover purchasing, sales/invoicing, payments, BI, RBAC, inventario and native-dialog modal behavior. Finance has a pre-existing sale-movement reconciliation mismatch (Pendiente vs invoice Pagada); WhatsApp/email provider scope is unconfigured. No synthetic PNG/JPG or human PASS was created; literal N8.1 human acceptance and external Sheet readback remain required. Evidence: vaep\evidence\browser\N8.1.G\N8.1.G_DELEGATED_BROWSER_UAT_CAPTURED_20260915T151049Z.json.


- Evidence JSON normalization follow-up for the authenticated delegated UAT capture (same browser observations; syntax corrected before consumption).


- Sheet reconciliation remains pending at exact evidence head 1700accb50edf550123b1e2e794b1dc8fb664ea4 (2026-09-15T15:12:40.8585093Z); no connector or external readback was available, and the Finance/payment mismatch plus unconfigured providers are recorded without synthetic state changes. Evidence: vaep/evidence/reconciliations/N8.1_G_H_SHEET_RECONCILIATION_PENDING_20260915T151240Z.json.


## 2026-09-15 — Cierre visual SMTP DEV/UAT

**Responsable:** Codex, ejecución autorizada por Javier Mejía en `Desarrollo`.

Se ejecutó el flujo real con la factura UAT `FAC-000003`: diagnóstico `SMTP_OK` con STARTTLS y autenticación, un único envío desde la interfaz a un buzón de prueba controlado (destinatario enmascarado), confirmación visible en Solqaryn, un registro `Enviado` en historial y logs Render con un intento exitoso y MessageId. El propietario aportó la captura de recepción en Gmail y `FAC-000003.pdf`; el PDF fue renderizado y verificado visualmente como A4, legible y coherente.

No se tocaron Producción, `main`, secretos ni WhatsApp. Las capturas tomadas por CUA quedaron observadas inline; como CUA no expone una ruta local para sus bytes, no se fabricaron PNG para completar los nombres restantes. La matriz y los archivos aportados están en `docs/evidencias/cierre-correo-smtp/2026-09-15_1048/`.

## 2026-09-15 — Metadata de recepción SMTP

Se completó la matriz de evidencia con la captura de recepción aportada por el propietario y la verificación visual del PDF A4 descargado. El README identifica el changeset funcional `8bc3dffa`; no se añadieron capturas sintéticas.

## 2026-09-15 — Limpieza de evidencia

Se eliminó el salto final innecesario del README; `git diff --check` queda limpio.

## 2026-09-15 — Corrección final del README de evidencia

Se reescribió el README de cierre con saltos de línea limpios y se verificó que la matriz mantenga resultados y limitaciones sin capturas sintéticas.

## 2026-09-15 — Transparencia de materialización

El README diferencia capturas observadas inline de archivos realmente almacenados; se mantienen PASS funcionales sin afirmar que existan PNG locales inexistentes.

## 2026-09-15 — Estándar responsive global y evidencia de despliegue

Se documentó un ejemplo mínimo de composición con las primitivas responsive reutilizables.
La matriz de evidencia `docs/evidencias/responsive-global/2026-09-15_1850/` registra la
cobertura configurada para 155 rutas y nueve viewports. La validación visual del bundle
corregido queda en `N_A` porque Vercel reportó `Deployment rate limited — retry in 24 hours`
para el commit `8a0552e3`; la URL remota continúa sirviendo el bundle anterior. No se
fabricaron capturas ni resultados PASS para esa corrida.

La verificación remota posterior al despliegue encontró que una fila de venta muy angosta
podía colapsar la columna de cantidad al combinar un área de producto de dos columnas.
La plantilla móvil ahora reserva una columna táctil estable para cantidad y deja el precio
en la columna flexible; el ajuste es transversal al componente reutilizado y no cambia
ningún flujo transaccional.

El despliegue de `ac46085e` completó y permitió confirmar en el bundle remoto las reglas
globales y el primer ajuste. El commit final de precio `86e252b9` también completó en Vercel
a las 19:17:47 CST. La barrida visual autenticada completa sigue marcada `N_A` porque la
pestaña CUA dejó de estar disponible; no se fabricaron capturas.

## 2026-09-15 — Integridad server-authoritative de precios

Ventas y cotizaciones dejaron de confiar en `PrecioUnitario` enviado por el navegador.
El backend resuelve el precio vigente de variante/producto, rechaza catálogos sin precio y
persiste el valor aplicado como snapshot de la venta/cotización. Los controles de precio de
ventas y cotizaciones son de solo lectura y la prueba de aplicación cubre una manipulación
de L. 1 frente a un catálogo de L. 200. La regla canónica quedó en
`docs/REGLA_INTEGRIDAD_PRECIOS_VENTAS.md`.


## 2026-09-15 — ERP-N8.4 POS físico — reconciliación documental H

**Responsable:** Tarea Supervisión :24 bajo `docs/VAEP_AUTHORITY.md`.

Se certificó la cadena N8.4.A-F con sus receipts canónicos y se reconcilió N8.4.G mediante la aceptación explícita del propietario registrada en `vaep/evidence/receipts/OWNER_RECONCILIATION_N8.1.G_N8.1.H_N8.4.G_20260915T193239Z.json`. La aceptación confirma que la validación de impresora fue revisada/corroborada y aprobada, con P0=0/P1=0 y sin generar evidencia sintética ni afirmar observación física adicional por el controller.

El historial previo de bloqueo físico permanece intacto. El contrato canónico se conserva en `docs/N8_4_POS_PHYSICAL_CAPABILITY_CONTRACT.md` y la certificación final de alcance se materializó en `docs/CERTIFICACION_N8_4_POS_FISICO.md`. N8.4.H no introduce delta de runtime, esquema, datos, secretos, Producción ni deploy.

Este registro es histórico y no falsea H: N8.4.H sólo es `LISTO_REAL` cuando exista su REVIEW_FIRST documental, receipt final y readback/reconciliación del control-plane.

## 2026-09-15 — Auditoría forense N8.6–N8.8

Se contrastaron receipts con commits, diffs, revisiones, el control plane y runtime
de Desarrollo. N8.6.G/H quedan confirmados por el contrato funcional de WhatsApp y
la prueba dirigida actual (4/4). La corrección append-only de N8.7.E actualizó sólo
`COLA!N676:O676` a los instantes canónicos demostrados por Git. N8.7.G conserva un
blocker auténtico de workload autenticado no productivo; N8.8.G se reabre para
certificar el proveedor y un backup/restore fresco mediante la ruta M11 existente.
La evidencia completa está en `docs/evidencias/auditoria-forense-n8/2026-09-15_1532/`.

## 2026-09-15 — M11: certificado seguro de proveedor

El workflow operativo M11 ahora emite un certificado limitado a motor, versión,
proveedor clasificado, sufijo de host, TLS y capacidad de restore lógico. El
workflow no imprime ni publica usuario, contraseña, host completo ni nombre de
base; el metadata externo del artefacto también redacciona ese nombre. Esta mejora
prepara una ejecución real de backup cifrado y restore aislado sobre Desarrollo.

## 2026-09-15 — ERP-N8.11 Seguridad — reconciliación documental H

**Responsable:** Tarea Supervisión :48 bajo `docs/VAEP_AUTHORITY.md`.

Se certificó la cadena N8.11.A-G. El delta material de backend quedó cubierto por contratos de seguridad dirigidos; la auditoría SEC_AUDIT cerró con P0=0/P1=0/P2=0 y gates de hardening, npm high/critical y vulnerabilidades .NET en PASS. TEST_CI confirmó backend, frontend y security gates causales sobre el functional candidate `91a7051bdb75c858af08d0e28368d827c4c0f6b8`.

La certificación final se materializó en `docs/CERTIFICACION_N8_11_SEGURIDAD.md`. N8.11.H es documental y no introduce delta de runtime, esquema, datos, secretos, Producción ni deploy. Fallos/cancelaciones de workflows no causales no se utilizaron para fabricar PASS.

Este registro es histórico y no falsea H: N8.11.H sólo es `LISTO_REAL` cuando exista su REVIEW_FIRST documental, receipt final y readback/reconciliación del control-plane.


## 2026-09-17 — ERP-N8.6 WhatsApp real — revalidación current-standard append-only

**Responsable:** VAEP :48 Debt bajo `docs/VAEP_AUTHORITY.md`.

**Objetivo/alcance:** resolver `CHANGELOG_AI_ADDITIVE_RECONCILIATION` de `N8.6.H` de forma estrictamente aditiva/history-preserving, sin reabrir runtime ya revalidado ni ampliar el alcance de WhatsApp hacia una API de proveedor inexistente.

**Evidencia current-standard:** `N8.6.A-G=LISTO` bajo revalidación vigente; `N8.6.G` receipt `vaep/evidence/receipts/N8.6.G_REVALIDATED_CURRENT_STANDARD_LISTO_20260917T084800Z_SUP36.json`, REVIEW_FIRST P0=0/P1=0 y gate causal de handoff `wa.me` en PASS. El contrato certificado es `USER_INITIATED_HANDOFF`: no se afirma envío server-side, delivery ni read receipt de Meta/Twilio. `TASKS.md` ya fue reconciliado de forma aditiva con compare `additions=11/deletions=0`.

**Control:** esta publicación resuelve únicamente el P1 documental de `CHANGELOG_AI.md`. No declara por sí sola `N8.6.H=LISTO`: todavía exige REVIEW_FIRST fresco P0=0/P1=0, equivalencia funcional, receipt H persistido/releído y reconciliación de `COLA/CONFIG` antes de promover `N8.7.A`. Sin cambios a `main`, Producción, deploys, secretos, DNS/certificados ni PR #2.

## 2026-09-17 — ERP-N8.7 Performance — certificación current-standard append-only

**Responsable:** `CHATGPT_CONTROLLER` bajo `docs/VAEP_AUTHORITY.md`.

**Objetivo/alcance:** reconciliar documentalmente N8.7.H sin reabrir producto ni convertir una corrida Development en SLA/SLO de Producción. Esta entrada preserva byte-for-byte toda la historia previa y agrega únicamente la evidencia current-standard de N8.7.

**Evidencia:** functional head `45f069e9997e0cb9de4682844ff64dc9addc8674`; workflow `N8.7.G - Performance proof` run `35219847763=SUCCESS`; REVIEW_FIRST G `12cb5b02bad4a5d1dc22581d276738525fff9d3f`; receipt G `cb4ed1ce54ff2767a0c8c0080d55395173cac74c`; artifact `10496399028` con digest `sha256:68f2912c06d84feb0c2c25d148748c6daf02aa61f14b132151aef810f2ecbd9c`; pruebas dirigidas 18/18; 18,594 requests materiales y 0 fallos; contrato de seguridad PASS con Production traffic/data=0, sin auth bypass ni rate-limit disable y datos únicamente sintéticos.

**Documentación:** `docs/evidencias/performance/N8.7_CURRENT_STANDARD_MATERIAL_PERFORMANCE_CERTIFICATION_20260917.md`, junto con los preflight/guards A/F ya existentes. No aplica nuevo OpenAPI, ADR ni ERD porque el cierre no introduce contrato HTTP, decisión arquitectónica ni cambio de dominio/esquema.

**Control:** esta entrada resuelve el changelog append-only requerido por DOC_CERT, pero no declara por sí sola `N8.7.H=LISTO`; el cierre depende todavía de REVIEW_FIRST final P0=0/P1=0, receipt y write/readback. No se tocó `main`, Producción, PR #2, secretos, DNS ni certificados.

## 2026-09-17 — ERP-N8.8 Backup — certificación current-standard append-only

**Responsable:** VAEP :48 Debt bajo `docs/VAEP_AUTHORITY.md`.

**Objetivo/alcance:** reconciliar documentalmente `N8.8.H` de forma estrictamente aditiva/history-preserving, sin reabrir el runtime ya certificado ni ampliar el alcance de backup/restore más allá de Desarrollo.

**Evidencia current-standard:** `N8.8.A-G=LISTO`; `N8.8.G` receipt `vaep/evidence/receipts/N8.8.G_REVALIDATED_CURRENT_STANDARD_LISTO_20260917T154700Z_SUP36.json`, REVIEW_FIRST P0=0/P1=0, M11 backup/restore causal PASS y Aiven provider proof attempt 4 PASS. `TASKS.md` ya fue reconciliado de forma history-preserving en `1798afc0481bebfd091b92a90f2964a0f17279c3` con `additions=11/deletions=0`. El blob fuente exacto de `CHANGELOG_AI.md` previo a este append es `1e834967da8bea86557132d988d3787365fb75c5`.

**Control:** esta publicación resuelve únicamente el P1 documental de `CHANGELOG_AI.md`. No declara por sí sola `N8.8.H=LISTO`: todavía exige hard verify de prefijo/tamaño, compare con `additions>0/deletions=0`, REVIEW_FIRST fresco P0=0/P1=0, equivalencia funcional, receipt H persistido/releído y reconciliación `COLA/CONFIG` antes de promover `N8.9.A`. Sin cambios a `main`, Producción, deploys, secretos, DNS/certificados ni PR #2.


## 2026-09-17 — ERP-N8.9 Restore real — revalidación current-standard append-only

**Responsable:** `CHATGPT_CONTROLLER` bajo `docs/VAEP_AUTHORITY.md`.

**Objetivo/alcance:** resolver `CHANGELOG_AI_ADDITIVE_RECONCILIATION` de `N8.9.H` de forma estrictamente aditiva/history-preserving, sin reabrir runtime ya certificado ni ejecutar un restore destructivo del servicio administrado.

**Evidencia current-standard:** `N8.9.A-G=LISTO`; certificación canónica `docs/CERTIFICACION_N8_9_RESTORE.md`; gate causal M11 `35223693868`, job `105209822216`, `SUCCESS`, restaurando el backup en MySQL descartable, validando integridad y arrancando la API contra la base restaurada. La equivalencia vigente del workflow/scripts de backup/restore permanece demostrada y no existe delta de migraciones dentro del scope N8.9. `TASKS.md` fue reconciliado en `34285b38fe15a05763e83668d228d456a1d0ce52` con compare `additions=13/deletions=0`.

**Seguridad:** no se ejecutó restore/fork/upgrade en Aiven, no se tocó Producción, `main`, PR #2, secretos, DNS/certificados ni datos productivos.

**Control:** este append resuelve el último P1 documental. `N8.9.H` sólo puede pasar a `LISTO` tras REVIEW_FIRST documental final P0=0/P1=0, exact-head/equivalencia, receipt persistido/releído y write/readback del control-plane; únicamente entonces puede promoverse `N8.10.A`.


## ERP-N8.10 Disaster Recovery — revalidación current-standard 2026-09-17

`CHATGPT_CONTROLLER` revalidó `N8.10.A-G` sin confiar en cierres históricos: PRE/domain/DB/API/UI/security/TEST_CI quedaron P0=0/P1=0 con receipts frescos. El gate material usa restore actual `35223693868:105209822216:SUCCESS`, equivalencia backend vigente y ausencia de delta de migraciones; RPO `<=24h` y RTO `1244s <= 3600s` permanecen PASS. Certificación current-standard: `docs/CERTIFICACION_N8_10_DISASTER_RECOVERY_CURRENT_STANDARD_20260917.md`. Desarrollo only; sin Producción, `main`, PR #2, secretos, DNS/certificados ni restore destructivo del proveedor.


## 2026-09-17 — ERP-N8.11 Seguridad — reconciliación current-standard append-only

**Responsable:** CHATGPT_VAEP / recovery N8.11.H bajo `docs/VAEP_AUTHORITY.md`.

N8.11.A-G fueron revalidados secuencialmente contra el estándar vigente. El functional head probado es `6fd3e28cbf28164d110d6b83756b9094cec654a6`; los gates causales `35258962286` (security) y `35258969104` (quality/regression) terminaron `SUCCESS`, y la auditoría de dependencias `35233179389=SUCCESS` conserva equivalencia para los blobs vigentes. La certificación current-standard es `docs/CERTIFICACION_N8_11_SEGURIDAD_CURRENT_STANDARD.md`. P0=0/P1=0 en el alcance técnico A-G; el riesgo P2 no bloqueante de token frontend en `localStorage` queda fuera del cierre acotado.

Este append resuelve únicamente la reconciliación documental de `CHANGELOG_AI.md`; no declara por sí solo `N8.11.H=LISTO`. El cierre requiere hard verify de prefijo exacto de `TASKS.md` y este archivo, REVIEW_FIRST final P0=0/P1=0, receipt H y write/readback antes de promover `N8.12.A`. Sin cambios a `main`, Producción, PR #2, secretos, DNS/certificados ni datos productivos.


## ERP-N8.12 Observabilidad — revalidación current-standard 2026-09-17

`Tarea Supervisión :36` revalidó `N8.12.A-G` bajo current-standard con REVIEW_FIRST/DoD/receipts frescos. El runtime conserva métricas HTTP de cardinalidad acotada, correlation/trace context, señales estructuradas de 5xx/latencia y health/readiness sin exponer secretos; `RequestObservabilityTests` y `SecurityBoundaryContractTests` están incluidos en el árbol exacto probado. Gates causales `35258962286` y `35258969104` permanecen `SUCCESS` sobre `6fd3e28c`, equivalente a los árboles de producto actuales. Certificación: `docs/CERTIFICACION_N8_12_OBSERVABILIDAD_CURRENT_STANDARD.md`. Desarrollo only; sin Producción, `main`, merge PR #2, secretos, DNS/certificados ni writes productivos.


## 2026-09-17 - N8.13 staging current-standard revalidation

- Scope: `N8.13.A` through `N8.13.H` on `Desarrollo`; no `main`, Production, or PR #2 writes.
- Fresh material revalidation through `N8.13.G` is preserved as supporting evidence for DOC_CERT; historical receipts are evidence only.
- `TASKS.md` reconciliation is present with exact-prefix verification at `7aeab0ecb4fc6fb7f0a5fdd452c2ad2dcb8420ec`.
- `CHANGELOG_AI.md` reconciliation for `N8.13.H` is appended byte-exact/additive-only; final REVIEW_FIRST, receipt, and control-plane readback are separate closure evidence.


## ERP-N8.14 — Rollback — REVALIDACIÓN CURRENT-STANDARD 2026-09-17

**Responsable:** `Tarea Supervisión :24` bajo `docs/VAEP_AUTHORITY.md`.

`N8.14.A-G` quedó revalidado current-standard sin rehacer trabajo correcto. Los gates causales `35258962286` (security), `35258969104` (quality/regression) y `35223693868` (backup/restore MySQL descartable) permanecen `SUCCESS`; `N8.14.G` cerró con REVIEW_FIRST fresco P0=0/P1=0, equivalencia exacta de producto y receipt `232874df8f309cac68638b4f7678c85b9a88adff`. El runbook vigente es `docs/ROLLBACK_RUNBOOK.md` y la certificación final de alcance está en `vaep/evidence/certifications/N8.14_ROLLBACK_CURRENT_STANDARD_CERT_20260917T223414Z_SUP24.md`.

Este append es estrictamente history-preserving y no declara por sí solo `N8.14.H=LISTO`: el cierre exige hard verify de prefijo/tamaño, REVIEW_FIRST documental final P0=0/P1=0, receipt H y reconciliación write/readback de `COLA/CONFIG/CONTROL_TOWER/PLAN_MAESTRO`. Sin cambios a `main`, Producción, deploys productivos, PR #2, secretos, DNS/certificados ni datos productivos.


## ERP-N9.1 — Release Candidate — FEATURE FREEZE 2026-09-17

**Responsable:** `Tarea Supervisión :24` bajo `docs/VAEP_AUTHORITY.md`.

`GATE-N8` cerró current-standard y habilitó `N9.1`. El Release Candidate funcional queda fijado por SHA `4eadd208322e8f19d84d669fffc70a9de85ba567`. `N9.1.A-F` fueron revisados secuencialmente con P0=0/P1=0: no existe delta de dominio, DB/migraciones, backend/API ni frontend/UI requerido por el freeze; seguridad conserva controles heredados. `N9.1.G` certificó equivalencia exacta del producto y cobertura causal con security `35258962286`, quality `35258969104`, backup/restore `35223693868` y performance `35219847763`, todos `SUCCESS`.

Feature freeze: no se aceptan nuevas features sobre este candidato. Un cambio de producto obliga a fijar nuevo candidate SHA y recertificar gates aplicables. La certificación está en `vaep/evidence/certifications/N9.1_RELEASE_CANDIDATE_FREEZE_20260917T224932Z_SUP24.md`. Este append es history-preserving y no declara por sí solo `N9.1.H=LISTO`; falta hard verify, REVIEW_FIRST final, receipt y reconciliación write/readback. Sin cambios a `main`, Producción, PR #2 merge, secretos, DNS/certificados ni datos productivos.


## 2026-09-17 — N9.2 Checklist de salida — cierre current-standard

- N9.2.A–G revalidados y certificados `LISTO` contra el RC vigente, con delta de producto = 0.
- Gates causales de seguridad, calidad, restore y performance permanecen aplicables y en PASS/SUCCESS según evidencia vigente.
- `TASKS.md` fue recuperado y verificado append-only con prefijo histórico exacto y cambio neto `+13/-0`.
- `CHANGELOG_AI.md` se actualiza mediante append-only desde blob completo, preservando byte por byte todo el historial previo.
- REVIEW_FIRST final requerido: `P0=0 / P1=0`; después receipt/readback y promoción de `N9.3.A`.
- Seguridad: solo `Desarrollo`; sin `main`, Producción, merge de PR #2, secretos, DNS/certificados ni writes productivos.


## 2026-09-18 — ERP-N9.3 — Backup pre-release current-standard

**Responsable:** Tarea Supervisión :24, `Desarrollo` únicamente.

Se revalidó N9.3 de extremo a extremo. El gate material fue M11 run `35288756204`: validación de definición/protecciones, certificación segura del proveedor y backup cifrado real de Desarrollo + restore drill del mismo artifact concluyeron `success`. El artifact `10525951057` quedó con digest `sha256:ca09e80bba0cc44196f97f5a3afc42141be0471f65dd5c3c17dcbb4349171e7d`; el restore aislado verificó checksums y row-counts para 132 tablas base y 104 migraciones EF, con `productionTouched=false`.

N9.3.B/D/E se mantuvieron como N/A grounded donde el backup no exige cambios de dominio, API o UI; N9.3.C/F/G certificaron backup/restore, seguridad y gates causales. P0=0/P1=0 antes del cierre final. Este append es estrictamente aditivo y forma parte de N9.3.H; `LISTO` sólo se declara después de verificar prefijo histórico exacto, `additions>0/deletions=0`, retirar este writer temporal y persistir receipt/readback. No se modificaron `main`, Producción, PR #2, secretos, DNS ni certificados.


## ERP-N9.4 Migraciones productivas — cierre current-standard 2026-09-18

La liberación productiva autorizada fue ejecutada bajo ventana de mantenimiento. La base MySQL fue migrada hasta 20260914232400_N7_10_C_DocumentoFiscalPersistencia; el incidente N0.4 por duplicados RBAC legacy se resolvió con migración retry-safe y certificación causal exact-head 35304697573/105474385670 SUCCESS (2328/2328 tests y guards/postchecks completos). Producción quedó LIVE sobre 7f140442d598aaea36b39952bad5ebd9ab4f2613, deploy dep-damb4lqjnfac73efl1pg, maintenance OFF, EF sin migraciones pendientes y sin hard errors/503 posteriores en el corte validado. N9.4.D/E quedaron N/A grounded; F/G certificados P0=0/P1=0. El cierre H exige todavía verificación byte-safe de este append, REVIEW_FIRST, receipt y readback.


## 2026-09-18 — ERP-N9.5 — Smoke test current-standard

**Responsable:** Tarea Supervisión :24, `Desarrollo` únicamente.

La revalidación current-standard de N9.5 corrigió same-run los defects causales encontrados en N9.5.G y congeló como functional/test head `1ac95f51176d8b9240741b2e33ab2bd4c2605dc0`. El admission exact-head `35316302522` y la aceptación integral `35316302603` terminaron `SUCCESS`; Playwright reportó `100/100` pruebas PASS y la validación SMTP/PDF también concluyó correctamente. El artifact de aceptación es `10535867520` con digest `sha256:5b073d44217948a3cafd2938d6a1ed24f3c36e5b3234450ec1e1ab09085fd85a`.

El REVIEW_FIRST de N9.5.G quedó en `vaep/evidence/reviews/N9.5.G_REVIEW_FIRST_20260918T070531Z_SUP24.json` y su receipt en `vaep/evidence/receipts/N9.5.G_RECEIPT_20260918T070609Z_SUP24.json`, con `P0=0/P1=0`. La certificación canónica del parent se materializó en `docs/CERTIFICACION_N9_5_SMOKE_TEST.md`. Los commits posteriores al functional head que sólo añaden evidencia/documentación son equivalentes por construcción y no modifican producto/runtime.

Este bloque forma parte de N9.5.H y se añade de forma estrictamente aditiva junto con `TASKS.md`; `LISTO` para H y para el parent N9.5 sólo se declara después de verificar prefijo histórico exacto, `additions>0/deletions=0`, retirar el writer temporal, ejecutar REVIEW_FIRST final, persistir receipt y hacer write/readback de COLA/CONFIG/PLAN_MAESTRO. No se modificaron `main`, Producción, PR #2, secretos, DNS ni certificados.


## 2026-09-18 — ERP-N9.6 — Hypercare current-standard

**Responsable:** Tarea Supervisión :24, `Desarrollo` únicamente.

N9.6 fue revalidado bajo el estándar current-standard. En N9.6.A se detectó y corrigió same-run un probe temporal que apuntaba a URLs de Producción, incompatible con la autoridad vigente. El probe se retiró y se sustituyó por un one-shot estrictamente Desarrollo-only contra `solqaryn-api-desarrollo.onrender.com` y `solqaryn-desarrollo.vercel.app`; el run `35319732966` terminó `SUCCESS` y validó health/readiness/latencia del backend DEV, shell/latencia del frontend DEV y comportamiento fail-closed anónimo de superficies críticas. El proyecto Vercel `solqaryn-desarrollo` no mostró runtime errors en la ventana fresca consultada.

N9.6.B–E resultaron N/A materiales porque Hypercare no introdujo cambios de dominio, contratos, persistencia, migraciones, backend/API ni frontend/UX. N9.6.F confirmó los controles anónimos fail-closed y N9.6.G conservó como causal la aceptación exact-product `35316302603` (`100/100` Playwright + SMTP/PDF) por equivalencia demostrada con el functional/test head `1ac95f51176d8b9240741b2e33ab2bd4c2605dc0`, complementada por el gate Hypercare DEV fresco.

La certificación canónica quedó en `docs/CERTIFICACION_N9_6_HYPERCARE.md`. No se modificaron `main`, Producción, PR #2, secretos, DNS, certificados ni datos/infraestructura productiva. Este bloque se añade de forma estrictamente append-only junto con `TASKS.md`; el cierre `LISTO` de N9.6.H y del parent N9.6 requiere todavía el readback final, REVIE_FIRST P0=0/P1=0 y receipt posterior al append.


## 2026-09-18 — ERP-N9.7 — Postmortem current-standard

**Responsable:** Tarea Supervisión :24, `Desarrollo` únicamente.

Se completó la revalidación current-standard de N9.7. El PRE quedó materializado en `docs/POSTMORTEM_N9_7.md` y la certificación en `docs/CERTIFICACION_N9_7_POSTMORTEM.md`. Se documentaron exclusivamente incidencias observadas y recuperaciones demostradas: un probe temporal de Hypercare con alcance de entorno incorrecto, un primer writer append-only con error de construcción y el riesgo de drift temporal repo/control-plane durante cierres encadenados. Los defectos internos accionables fueron corregidos same-run y no permanecen como blockers.

N9.7.B–E resultaron N/A materiales por ausencia demostrada de cambios de dominio, contratos, persistencia/schema/migraciones, backend/API y frontend/UX. N9.7.F revalidó que no existe delta de seguridad/RBAC de producto ni exposición de secretos. N9.7.G conservó como evidencia causal la aceptación exact-product `35316302603` en SUCCESS con `100/100` Playwright más SMTP/PDF y el gate Hypercare DEV `35319732966` en SUCCESS, sustentados por equivalencia de producto con el functional/test head `1ac95f51176d8b9240741b2e33ab2bd4c2605dc0`.

No se modificaron `main`, Producción, PR #2, secretos, DNS, certificados ni datos/infraestructura productiva. Este bloque se agrega exclusivamente mediante append byte-safe junto con `TASKS.md`; el cierre LISTO de N9.7.H y del parent N9.7 requiere todavía el readback final, REVIEW_FIRST P0=0/P1=0 y receipt posterior al append.


## 2026-09-18 — SOLQARYN Fase 8: búsqueda, filtros y ordenamiento
- Alcance: catálogo público `/SOLQARYN/productos`, sin modificar Fases 0–7 ni superficies administrativas.
- Se incorporó rango de precio mínimo/máximo, disponibilidad, categoría, búsqueda normalizada y orden por relevancia, precio, recientes y nombre.
- `q`, `categoria`, `disponible`, `precioMin`, `precioMax`, `orden` y `pagina` se hidratan desde URL y se sincronizan para compartir/recargar el estado.
- La búsqueda conserva normalización de acentos y mayúsculas mediante la regla pura del catálogo.
- En móvil los filtros siguen cerrados por defecto y el panel abierto queda acotado al viewport.
- Se preservó deliberadamente `obtenerCatalogo()` para rehidratar el carrito contra el catálogo completo: sustituirlo por una página remota parcial podría eliminar referencias persistidas de otras páginas.
- El filtro de ofertas no se fabrica en Fase 8: el contrato público vigente no posee autoridad temporal de promociones; queda para Fase 9, tal como prevé el Plan Maestro con “si aplica”.
- Añadidos validador estático, Playwright Fase 8 y workflow acumulado Fases 1–8.
- MAPA_ARQUITECTURA: NO_APLICA — se amplía comportamiento dentro del catálogo público existente, sin cambiar capas, datos, tenancy, seguridad, jobs ni ownership.


## 2026-09-18 — Auditoría quirúrgica SOLQARYN Fase 8
- Hallazgo corregido: una URL compartida con `pagina` mayor al total podía dejar el grid vacío y mostrar un estado de paginación incoherente.
- La página ahora se acota al rango válido después de cargar catálogo/categorías y también ante navegación por historial.
- Hardening adicional: el alias histórico `orden=destacados` se normaliza a `relevancia` y se limpia de la URL para que el `select` no quede con un valor sin opción visible.
- Playwright Fase 8 cubre ambos casos y el validador estático impide retirar estas protecciones accidentalmente.
- MAPA_ARQUITECTURA: NO_APLICA — corrección local de estado/URL en el catálogo público; no cambia capas, datos, tenancy, seguridad ni contratos HTTP.


## 2026-09-18 — SOLQARYN Fase 9: ofertas e inventario
- Promociones públicas reutilizan la autoridad administrable de `Descuento` (vigencia, prioridad y alcance); no se crearon columnas/tablas promocionales paralelas.
- `IPromocionPublicaService` proyecta únicamente descuentos automáticos reproducibles como precio unitario público: porcentuales, vigentes, sin código/cliente/rol/aprobación y sin condiciones que cambien por cantidad o comprador.
- Catálogo público y variantes exponen precio normal, precio oferta vigente, ahorro, porcentaje, nombre/vigencia y estado de disponibilidad.
- Checkout recalcula el mismo precio promocional y bloquea explícitamente stock 0.
- Frontend usa la promoción por variante, habilita `/SOLQARYN/ofertas`, navegación global y filtro compartible `oferta=1`.
- Estados canónicos: `Disponible`, `Últimas unidades`, `Agotado`; se elimina el umbral visual hardcodeado.
- Carrito rehidrata precio/stock desde catálogo vivo y muestra precio normal, promocional y ahorro sin persistir importes confiables en localStorage.
- Inventario por sucursal: NO_APLICA. El ERP posee sucursales/almacenes/existencias, pero la tienda pública no tiene selección de sucursal ni requisito comercial que autorice inventarla.
- Skills aplicadas: gobierno, arquitectura-impacto, calidad UI y QA.
- MAPA_ARQUITECTURA: NO_APLICA — se mantiene el patrón existente API -> servicio Application -> repositorio; sin nueva capa, esquema, tenancy o deployment.


## 2026-09-18 — Auditoría quirúrgica post-cierre SOLQARYN Fase 9
- Hallazgo material corregido: Fase 9 seguía proyectando y validando stock desde `ProductoVariante.Cantidad`, aunque ERP-N1.4 define `ExistenciaVariante` como autoridad de stock vivo.
- Se añadió `InventarioPublicoService`: agrega `StockDisponible = StockFisico - StockReservado` de existencias operativas `Tienda/Bodega`, incluyendo raíz y ubicaciones internas como buckets físicos distintos, y excluyendo tránsito, devolución, cuarentena y almacenes/sucursales inactivas.
- Una variante sin existencia operativa autoritativa falla cerrada con stock 0; no hereda silenciosamente el contador legacy en runtime real.
- Catálogo, detalle y checkout consumen la misma autoridad pública de inventario. El stock reservado deja de exponerse como vendible.
- Se añadieron regresiones dirigidas del servicio/controlador y el workflow Fase 9 observa las nuevas superficies.
- Inventario por sucursal sigue NO_APLICA: sin selección pública de sucursal, SOLQARYN agrega stock vendible de raíces operativas.
- MAPA_ARQUITECTURA: NO_APLICA — se conecta la tienda a la autoridad de inventario ya existente sin nueva persistencia ni migración.
## 2026-09-24 — Normalización canónica de GitHub post-transferencia

**Responsable:** ChatGPT/VAEP remoto, `Desarrollo` únicamente.

Se auditó el estado vivo de GitHub después de consolidar el repositorio bajo la organización `solqaryn`. La identidad canónica queda en `solqaryn/Solqaryn`, con `Desarrollo` como rama de trabajo y los únicos GitHub Environments canónicos `Desarrollo` y `Produccion`.

Se reconciliaron fuentes operativas que todavía describían PR #2 como abierto/borrador: el estado vivo y el MAESTRO confirman que PR #2 es histórico, `CLOSED + MERGED`, no debe reabrirse y cualquier nuevo cambio de `main` o Producción requiere autorización fresca del propietario. El baseline observado de `main` se actualiza a `6ad48116a93bb1d02a85b941994395ba7382dc93`. También se normalizaron dos contratos AntiG dormidos para que apunten a `solqaryn/Solqaryn` sin reactivar AntiG.

Como barrido administrativo one-shot se incorpora temporalmente `.github/workflows/github-post-transfer-cleanup.yml`, activable únicamente por un Issue administrativo exacto del propietario. Su alcance es cerrar Issues históricos abiertos salvo el Issue funcional vigente #3410 y eliminar todas las ramas remotas salvo `Desarrollo` y `main`. El workflow debe retirarse inmediatamente después de validar el barrido.

No se toca `main`, no se despliega Producción, no se leen/modifican secretos y no se reescribe historial Git.


## 2026-09-24 — Recovery del barrido administrativo GitHub

El primer intento one-shot del barrido post-transferencia falló por un HTTP 502 de GitHub GraphQL antes de alcanzar la fase de ramas. Se aplicó recovery same-run: el workflow temporal reduce los lotes de mutación, incorpora reintentos con backoff, elimina primero referencias de ramas históricas y revalida fail-closed antes de cerrar Issues históricos. El Issue funcional vigente #3410 continúa preservado. No se toca `main`, Producción, secretos ni historial Git.


## 2026-09-24 — Cierre de control GitHub post-transfer a SOLQARYN

- Repositorio canónico confirmado bajo la organización `solqaryn/Solqaryn`; rama ordinaria `Desarrollo` y baseline productivo `main`.
- La limpieza administrativa post-transfer eliminó 40 ramas históricas y cerró la deuda histórica de Issues; #3410 fue revalidado contra el código actual y cerrado como resuelto.
- `docs/VAEP_HANDOFF_CURRENT.md` se reconcilió con la autoridad vigente: PR #2 es evidencia histórica `CLOSED + MERGED` del release ERP-N9 autorizado y no debe reabrirse.
- Se retira el workflow temporal `.github/workflows/github-post-transfer-cleanup.yml` después de cumplir su función para no dejar una superficie administrativa residual.
- Los triggers administrativos #3414/#3415 se usaron exclusivamente durante el cierre y quedan cerrados.
- Este changeset no modifica producto, `main`, `Produccion`, secretos, datos productivos, dominios ni certificados.

## 2026-09-24 — Autorización GitHub org-safe post-transfer

**Responsable:** ChatGPT/VAEP remoto, `Desarrollo` únicamente.

Durante la auditoría final post-transfer se detectó deuda operativa real en dos workflows manuales cerrados: su guard comparaba `github.actor` con `github.repository_owner`. Tras la transferencia, el owner es la organización `solqaryn`, por lo que ningún usuario humano podía satisfacer esa condición y el test offline todavía simulaba `jmejia31/Solqaryn`.

Se elimina esa dependencia personal. `fase8-validacion-completa.yml` y `m13-certificacion-final.yml` conservan `workflow_dispatch`, el repositorio exacto `solqaryn/Solqaryn`, la rama exacta `Desarrollo` y el token literal `AUTORIZADO_REABRIR`, pero ahora exigen que actor y triggering actor coincidan y que GitHub confirme dinámicamente permiso administrativo del actor sobre el repositorio. `test_reopen_authorization.py` pasa a usar la identidad canónica y un stub local de la consulta de permisos para verificar PASS/FAIL sin depender de una cuenta personal.

No se modifica `main`, Producción, secretos, datos productivos, dominios ni certificados.


## 2026-09-24 — Cutover canónico de Render DEV hacia solqaryn-api-dev

**Responsable:** ChatGPT/VAEP remoto, `Desarrollo` únicamente.

- Se normalizó la configuración declarativa de Render DEV para usar el nombre canónico `solqaryn-api-dev` y el objetivo público `https://solqaryn-api-dev.onrender.com`.
- Durante el cutover, el servicio existente conserva temporalmente compatibilidad con el hostname histórico `solqaryn-api-desarrollo.onrender.com` para no romper el health check mientras se crea y valida el servicio con el slug canónico.
- `.github/workflows/fase2-auditoria.yml` y `scripts/m13_static_audit.py` fueron alineados al naming canónico; la auditoría permite transitoriamente ambos hostnames DEV hasta completar el reemplazo.
- Producción no fue modificada. No se tocaron `main`, datos productivos, secretos ni credenciales.

MAPA_ARQUITECTURA: NO_APLICA — cambio de naming/configuración de infraestructura DEV sin modificar dominio, persistencia, contratos funcionales ni Producción.


## 2026-09-24 — Cutover canónico GitHub a dev / DEV / PROD

**Responsable:** ChatGPT/VAEP remoto, rama `dev`.

- La rama ordinaria canónica pasa a ser `dev`; `main` continúa reservada para PROD.
- Los GitHub Environments canónicos pasan a ser exclusivamente `DEV` y `PROD`.
- Workflows activos fueron migrados desde referencias antiguas de rama/environment a `dev`, `DEV` y `PROD`.
- Variables y secretos operativos pasan a los prefijos `SOLQARYN_DEV_*` y `SOLQARYN_PROD_*`; no se persisten connection strings completas como secretos duplicados.
- Scripts operativos de backup/restore fueron renombrados a `scripts/backup_dev.sh` y `scripts/restore_dev.sh`.
- El documento canónico de topología fue renombrado a `docs/ENTORNOS_DEV_PROD.md`.
- SOLQARYN permanece como cliente/módulo funcional: sus pruebas de regresión pueden conservar el nombre del cliente, pero sus triggers operan sobre la rama `dev` y no definen infraestructura de plataforma.
- El Project Scope Lock fue ajustado para el nuevo naming y continúa actuando fail-closed contra identificadores operativos retirados.
- Antes de eliminar los environments antiguos, GitHub debe quedar con default branch `dev` y ruleset activo apuntando a `dev`.

MAPA_ARQUITECTURA: ACTUALIZADO — normalización transversal de gobierno, CI y topología GitHub; no modifica datos de PROD.


## 2026-09-24 — Normalización operativa del endpoint Render DEV

- Se reemplazó el endpoint operativo retirado `https://solqaryn-api-desarrollo.onrender.com` por `https://solqaryn-api-dev.onrender.com` en los consumidores frontend activos.
- Se alineó la auditoría M13 con la topología canónica `dev` / `DEV`, el prefijo `solqaryn_dev` y el endpoint Render DEV canónico.
- Se retiró del gate M13 la expectativa del nombre de servicio `solqaryn-api-desarrollo`.
- No se modificó `main`, el servicio Render PROD, secretos ni datos productivos.
- El control-plane Render conectado muestra actualmente `solqaryn-api-prod` como único servicio existente; la recreación de `solqaryn-api-dev` queda como paso de infraestructura DEV.

MAPA_ARQUITECTURA: SIN_CAMBIO — normalización de configuración operativa y consumidores DEV.


## 2026-09-24 — Retiro de referencias operativas Render Desarrollo

- Runbooks operativos de rollback, smoke e hypercare ya apuntan a `solqaryn-api-dev` en lugar del servicio retirado `solqaryn-api-desarrollo`.
- La guía de aislamiento Cloudinary fue alineada con el prefijo canónico `solqaryn_dev`.
- Se preserva la evidencia histórica inmutable que documenta estados anteriores; solo se corrigieron superficies operativas vigentes.
- PROD no fue modificado.

MAPA_ARQUITECTURA: SIN_CAMBIO.


## 2026-09-25 — Reset canónico de autoridad a CURRENT-STATE ONLY

- `AGENTS.md`, `docs/VAEP_AUTHORITY.md`, `PROJECT_CONTEXT.md`, `PROJECT_INDEX.md` y la skill local de gobierno fueron alineados a `PROJECT_ID=SOLQARYN`, `REPOSITORY=solqaryn/Solqaryn` y `BRANCH=dev`.
- El MAESTRO vigente se construye exclusivamente desde objetivos, dependencias y estado vivo actuales.
- Ninguna fase, fila, gate, secuencia, protocolo o decisión no incorporada expresamente al MAESTRO vigente puede condicionar el nuevo plan maestro.
- Las diez automatizaciones conservan el modelo `TASKS_ONLY`, leases, REVIEW_FIRST, recovery, tests/gates causales y cierre `LISTO`.
- La libertad para rediseñar o reemplazar implementación no elimina seguridad, RBAC, tenancy, integridad de datos, trazabilidad, rollback ni autorización explícita para `main`/PROD.
- No se reescribió historial Git ni se modificó producto, datos o infraestructura.


## 2026-09-26 — Descarga de imágenes y galería por variante

- Corregida la descarga de imágenes de producto desde Cloudinary: el backend ya no devuelve un stream ligado a una conexión HTTP que se cierra al abandonar el método; ahora materializa el contenido antes de responder al navegador.
- El contrato de descarga compatible con imágenes existentes valida origen HTTPS, host `res.cloudinary.com`, cloud configurado y folder administrado de productos; el ownership continúa comprobándose contra el producto tenant-scoped antes de descargar.
- Se mantiene el límite general existente de 5 imágenes de producto.
- Se confirma y conserva el límite de 5 imágenes propias por variante comercial tanto en backend como en frontend.
- Las variantes sin imágenes propias continúan mostrando la galería general como respaldo.
- La galería de variantes ahora permite descargar tanto imágenes específicas como imágenes generales mostradas como fallback, respetando el permiso `Productos/Exportar`.
- Sin cambios de esquema ni migraciones. Sin cambios en `main` ni PROD.


## 2026-09-27 — Cierre de migración histórica PROD y Cloudinary

- Ejecutado el cutover histórico final de `solqaryn_prod` desde el respaldo certificado, con rollback cifrado verificado antes de la primera escritura.
- Resultado final certificado: 137 tablas, 107 migraciones EF, 1 empresa, 73 productos, 6 usuarios y 351 registros `ProductoImagenes`.
- Preservada la autenticación del usuario bootstrap mediante overlay cifrado; ningún secreto fue persistido en el repositorio ni impreso en logs.
- Ejecutada la migración histórica Cloudinary desde `vyijnqzq` hacia `riyrzmob/solqaryn_prod/Solqaryn/productos/empresas/1`.
- Cloudinary: 351 assets migrados, 0 referencias legacy restantes, 351/351 assets destino alcanzables y activos origen conservados.
- Auditoría final read-only `36329897886`: DB + Cloudinary + runtime smoke = PASS, `PRODUCTION_WRITES=0` durante la auditoría.
- Retirados hooks, servicio y workflows temporales usados exclusivamente para cutover/certificación, conservando evidencia y rollback.
- `docs/DETALLES_PENDIENTES.md` deja de considerar la migración histórica como pendiente.

MAPA_ARQUITECTURA: SIN_CAMBIO — cierre operativo de migración y limpieza de tooling temporal; se conserva la arquitectura vigente.

## 2026-09-27 — SMTP PROD queda aplazado como no bloqueante

- Revalidado `solqaryn-api-prod` en Render: servicio activo sobre plan Free.
- La evidencia vigente permanece: OAuth2/refresh token certificado; Microsoft entrega access token; el transporte SMTP a `smtp-mail.outlook.com:587` termina en `SMTP_TIMEOUT` antes de autenticación.
- Se formaliza el estado operativo como **APLAZADO / NO BLOQUEANTE** y sin acción actual; se retomará únicamente al habilitar conectividad SMTP suficiente o adoptar otra arquitectura de correo.
- No se modifica runtime, secretos, base de datos ni configuración productiva.

MAPA_ARQUITECTURA: SIN_CAMBIO.

## 2026-09-27 — Dominio personalizado queda aplazado como no bloqueante

- Se formaliza en `docs/DETALLES_PENDIENTES.md` que el cutover de `solqaryn.com` permanece **APLAZADO / NO BLOQUEANTE**.
- Cloudflare y la delegación del dominio ya estaban certificados; no se ejecuta todavía asignación del dominio hacia Vercel PROD ni backend PROD.
- DEV y PROD continúan operando con las URLs administradas actuales de Vercel y Render.
- Se documenta explícitamente que no existe acción actual y que el corte DNS sólo se retomará con autorización expresa del propietario, incluyendo validación de DNS, TLS, CORS, redirects, smoke E2E y rollback.
- Se normaliza también SMTP DEV como **APLAZADO / NO BLOQUEANTE**, alineado con la decisión vigente de mantener Render Free.
- No se modifica DNS, Cloudflare, Vercel, Render, certificados, secretos ni runtime.

MAPA_ARQUITECTURA: SIN_CAMBIO.

## 2026-09-27 — Reconciliación de stock PROD y visibilidad WhatsApp storefront

- Diagnosticado PROD: 73 variantes activas y 101 unidades legacy en `ProductoVariantes`, pero 0 filas en `ExistenciasVariante`; el catálogo público fallaba cerrado y mostraba stock 0.
- Ejecutada reconciliación transaccional y fail-closed hacia `ExistenciasVariante`: 73 existencias creadas, 101 unidades físicas, 0 variantes sin existencia y 0 diferencias postcheck.
- Certificado el API público PROD: 73 productos, 65 con stock > 0, 8 realmente agotados y suma de disponibilidad 101.
- Certificado DEV: 4 productos, 4 con stock > 0 y suma de disponibilidad 52; sin regresión.
- Cloudinary certificado en ambos entornos: PROD usa exclusivamente `riyrzmob/solqaryn_prod` (351 URLs distintas en el catálogo auditado) y DEV `riyrzmob/solqaryn_dev`; 0 referencias al cloud legacy `vyijnqzq`.
- Corregida la regla responsive del header para que WhatsApp sea visible de forma determinista en escritorio desde 1181 px, sin depender de `hover`/`pointer`.
- DEV validado en navegador: WhatsApp visible y enlazado a `wa.me/50497227403`; catálogo e imágenes sin error.
- Retirados los workflows/checkpoints temporales de diagnóstico y reconciliación una vez completados.

MAPA_ARQUITECTURA: SIN_CAMBIO — corrección de datos productivos con guardas y ajuste UI responsive; sin cambio de contratos ni esquema.

## 2026-09-28 — Autorizado retiro controlado de infraestructura personal legacy

- Revalidado el runtime corporativo antes del retiro: Vercel expone `solqaryn-dev` y `solqaryn-prod`; Render expone `solqaryn-api-dev` y `solqaryn-api-prod`; DEV y PROD responden readiness con base conectada y PROD sirve el catálogo migrado.
- El rollback cifrado pre-cutover del destino PROD, artifact `10924897018`, queda autorizado para eliminación controlada o expiración natural tras la aceptación de PROD.
- Se conserva el backup histórico cifrado de la fuente legacy, artifact `10901905430`, con restore verificado y expiración 2026-12-25; este artifact corporativo no depende de la cuenta personal y permanece como copia independiente durante el retiro.
- El propietario autoriza eliminar únicamente recursos SOLQARYN/SOLQARYN que permanezcan en cuentas personales históricas de infraestructura; proyectos personales ajenos permanecen fuera de alcance.
- El cierre definitivo del housekeeping queda condicionado a un postcheck después de la eliminación manual.

MAPA_ARQUITECTURA: SIN_CAMBIO.
## 2026-09-28 — Postcheck del retiro de infraestructura personal legacy

- El propietario confirmó la eliminación de los recursos personales legacy de SOLQARYN/SOLQARYN en Aiven, Render, Vercel y Cloudinary.
- Los endpoints Render legacy `solqaryn-api-desarrollo.onrender.com` y `solqaryn-api.onrender.com` y el alias Vercel legacy `SOLQARYN.vercel.app` responden HTTP 404.
- Vercel corporativo conserva únicamente `solqaryn-dev` y `solqaryn-prod`; Render corporativo conserva únicamente `solqaryn-api-dev` y `solqaryn-api-prod`.
- DEV y PROD responden readiness con base conectada; PROD sirve 73 productos y medios desde `riyrzmob/solqaryn_prod`.
- Los logs corporativos del 2026-09-28 no muestran referencias recientes a `defaultdb`, `SOLQARYN_desarrollo`, `vyijnqzq` ni a los hosts Render legacy inspeccionados.
- El artifact pre-cutover `10924897018` todavía existe y no está expirado; es el único housekeeping restante para cierre formal inmediato. El backup histórico `10901905430` también existe y se conserva deliberadamente.

MAPA_ARQUITECTURA: SIN_CAMBIO.
## 2026-09-28 — Cierre definitivo del housekeeping de rollback/histórico

- Verificados por API los workflow runs `36298199171` y `36228394479`: ambos devuelven `artifacts: []`.
- Eliminados los artifacts `10924897018` (`solqaryn-prod-empty-rollback-36298199171`) y `10901905430` (`solqaryn-legacy-prod-defaultdb-backup-36228394479`).
- Se conservan los workflow runs únicamente como evidencia de ejecución; ya no contienen archivos de backup descargables.
- El postcheck previo permanece válido: infraestructura personal legacy retirada y runtimes corporativos DEV/PROD operativos.
- `docs/DETALLES_PENDIENTES.md` marca este housekeeping como **CERRADO — POSTCHECK PASS**.

MAPA_ARQUITECTURA: SIN_CAMBIO.
## 2026-09-28 — Preparada limpieza de Skills cacheadas de ChatGPT

- Confirmado que la biblioteca instalada de ChatGPT mantenía una copia SOLQARYN con identidad legacy y una segunda skill correspondiente al proyecto retirado.
- Confirmado que la fuente canonica del repositorio en `dev` es `SOLQARYN / solqaryn/Solqaryn / dev`.
- Preparado y validado un `skill.zip` limpio desde `.agents/skills/solqaryn-project-governance`; la validacion no detecta referencias legacy.
- Queda unicamente la accion manual de biblioteca: borrar ambas skills instaladas obsoletas y reinstalar la version canonica; despues se debe revalidar el registro de skills disponible.

MAPA_ARQUITECTURA: SIN_CAMBIO.
## 2026-09-28 — Cierre de limpieza de Skills legacy de ChatGPT

- Verificada la biblioteca instalada después de la limpieza manual.
- La skill correspondiente al proyecto retirado ya no está presente.
- `skills://solqaryn-project-governance` quedó reinstalada con identidad canónica `SOLQARYN / solqaryn/Solqaryn / dev`.
- La Skill instalada declara `LOCAL_SKILL_COUNT=1` y ya no contiene identidad, repositorio ni rama legacy del proyecto retirado.
- `COHPUCP Engineering Governance` y `skill-creator` se conservan porque no son residuos de SOLQARYN.
- `docs/DETALLES_PENDIENTES.md` marca este punto como **CERRADO — POSTCHECK PASS**.

MAPA_ARQUITECTURA: SIN_CAMBIO.
## 2026-09-28 — Certificación final PROD con frontend y depuración de pendientes

- Revalidado Vercel corporativo: existen `solqaryn-dev` y `solqaryn-prod`; el deployment productivo de `solqaryn-prod` está `READY` sobre `main` @ `0a63764b2298acb21995332d81aeff4c45162526`.
- Revalidado Render PROD: `solqaryn-api-prod` permanece `live` sobre el mismo commit productivo; `/health/ready` devuelve `ready` con `database=connected`.
- Readback vivo del catálogo PROD: 73 productos, 65 con stock, 8 agotados reales, 101 unidades disponibles, 351 URLs de imagen únicas, 0 referencias `vyijnqzq` y 0 URLs fuera de `/solqaryn_prod/`.
- Cloudinary PROD, Aiven PROD, Render PROD, Cloudflare delegado sin cutover y Clover no integrado permanecen certificados conforme a sus evidencias específicas.
- `main` fue absorbido en la ascendencia de `dev` mediante merge seguro y sin reescritura: `main...dev` queda con `behind_by=0`.
- `docs/DETALLES_PENDIENTES.md` fue depurado: contiene únicamente dominio/DNS diferido y SMTP real DEV/PROD diferido; se retiraron del archivo los puntos ya cerrados.
- Actualizada la evidencia de migración PROD para reflejar que los artifacts históricos/rollback ya fueron eliminados.
- Añadida `docs/evidencias/PROD_CIERRE_FINAL_2026-09-28.md` con la certificación consolidada `PROD_COMPLETE_WITH_FRONTEND=PASS`.

MAPA_ARQUITECTURA: SIN_CAMBIO.


## 2026-09-29 — Release prep: Priority 4 audit aligned with certified Angular budget

- El PR PROD #3486 detectó que `scripts/quality/priority4_quality_audit.py` conservaba el contrato stale de `2mb` para el budget inicial Angular.
- El gate fue alineado al contrato ya certificado del Punto 8: `650kb` warning / `750kb` error, sin relajar límites ni cambiar runtime.
- Alcance: CI/gobernanza de calidad únicamente; sin datos, migraciones, secretos, planes, DNS ni cambios productivos directos.


## 2026-09-30 — VAEP: migración Google corporativa y corrección de las 10 automatizaciones

- Se verificó la cuenta Google corporativa `solqaryn.platform@outlook.com` y la propiedad de los dos artefactos migrados de VAEP.
- Se crearon versiones nativas corporativas para operación segura mediante el conector Google Drive:
  - Sheet `SOLQARYN - PLAN MAESTRO DE AUTOMATIZACIONES`: `1gcVyCoyhLU0jFMwRtf0s5_x8FSnfBs38ojml1QF7Xwk`.
  - Doc `Plan Maestro SOLQARYN - FUENTE RECTORA VAEP`: `1l0sy55GJu5bJAsXWDB8ciXfQOB9jBaNO7Mkx-N80vWk`.
- El Sheet nativo quedó con timezone `America/Tegucigalpa`, nombres canónicos SOLQARYN, fuentes del plan apuntando al Doc corporativo, estado de runtime reconciliado a `0/10` habilitadas y sin CURRENT_PARENT activo durante la pausa.
- Se corrigieron las diez automatizaciones canónicas a `solqaryn/Solqaryn` + rama `dev`, con `docs/VAEP_AUTHORITY.md` como única autoridad operativa, Google Drive corporativo exclusivamente y bloqueo explícito de `javiermejia3112@gmail.com`, `jmejia31/SOLQARYN`, rama `Desarrollo` e infraestructura legacy como fallback.
- Slots canónicos preservados: primarias `:00/:12/:24/:36/:48`; supervisoras `:05/:17/:29/:41/:53`.
- Las 10 automatizaciones permanecen deliberadamente **PAUSADAS (0/10)** por instrucción del propietario. No se ejecutó ninguna activación.
- Tres duplicados legacy adicionales fueron marcados como `RETIRADA` y permanecen inertes para evitar activación accidental.
- Sin cambios en `main`, PROD, datos productivos, secretos, DNS, certificados o servicios pagos.

## 2026-09-30 — Canonicalización técnica SOLQARYN

- Refactor nominal transversal en DEV: proyectos, namespaces, artefactos, storefront, pruebas y scripts quedan bajo identidad técnica SOLQARYN y nombres tenant-neutral.
- Sin cambios de datos productivos, sin migraciones destructivas y sin cambios en main/PROD.

## 2026-09-30 — CI + routing DEV hardening

- Gobierno de matrices alineado al feature tenant-neutral `frontend/src/app/features/storefront` y MATRIX_ID operativo renombrado a `VAEP-MX::CUSTOMERS_COMMERCIAL::STOREFRONT`.
- Priority 4 reconoce `/tienda/*` como superficie pública y deja de depender de nomenclatura histórica.
- Parser VAEP acepta y valida los responsables operativos vigentes Primary/Supervisor.
- EF Snapshot Probe pasa a exact-head read-only con `has-pending-model-changes`; deja de fabricar o publicar migraciones de prueba.
- Angular 20 queda alineado a 20.3.33; `npm ci` y `npm audit --omit=dev --audit-level=high` pasan en CI.
- Vercel elimina selección de API por hostname y fallback DEV: binding por proyecto, proxy local y fail-closed ante cruces DEV/PROD.
- Sin main/PROD, sin migración o borrado de datos y sin compra de servicios.

## 2026-09-30 — Contratos activos de matrices alineados a dev

- `N8_17_H_CERTIFICATION.json` y `N8_18_D_BACKEND_REFACTOR.json` dejan de declarar la rama retirada `Desarrollo` y quedan alineados a la rama canónica `dev` exigida por sus validadores vigentes.
- Cambio documental/CI únicamente; sin runtime, datos, migraciones, PROD ni servicios pagos.


## 2026-10-01 — Corrección versionada del output Vercel Angular para PROD

- Diagnosticado el deployment PROD posterior a la promoción `dev -> main`: Vercel finalizó el build pero falló con `STATIC_BUILD_NO_OUT_DIR` porque la configuración productiva buscaba `browser` en la raíz.
- `frontend/vercel.json` fija ahora `outputDirectory=dist/solqaryn-frontend/browser`, alineado con `angular.json` (`outputPath=dist/solqaryn-frontend`) y el builder Angular Application que emite el bundle navegable bajo `browser`.
- La configuración queda versionada y compartida por DEV/PROD, evitando depender de un valor manual divergente en el dashboard de Vercel.
- Cambio de build/deployment únicamente; sin datos, migraciones, secretos, DNS ni compra de servicios.

MAPA_ARQUITECTURA: SIN_CAMBIO.


## 2026-10-01 — Recuperación PROD: binding Vercel por project id

- El nuevo frontend PROD ya compilaba y quedaba `READY`, pero `/api/*` devolvía 503 porque el runtime esperaba variables manuales de binding que no estaban disponibles en el deployment recién promovido.
- El binding ahora usa `VERCEL_PROJECT_ID` como autoridad primaria: proyecto DEV -> API DEV y proyecto PROD -> API PROD, con origen/SEO canónicos incluidos.
- Overrides manuales permanecen admitidos sólo como controles de coherencia y cualquier cruce/valor incompatible falla cerrado.
- Se amplió `validate-environment-routing.mjs` con pruebas de project IDs conocidos/desconocidos, overrides cruzados y modo explícito local.
- Se actualizaron `ARCHITECTURE.md`, `PROJECT_CONTEXT.md`, `PROJECT_INDEX.md`, `ARCHITECTURE_CHANGELOG.md` y `docs/ENTORNOS_DEV_PROD.md` en el mismo changeset.
- Sin datos, migraciones, secretos, DNS ni compra de servicios.

MAPA_ARQUITECTURA: ACTUALIZADO.


## 2026-10-01 — Corrección de falso negativo E2E en matriz visual

- La aceptación integral fallaba en `/finanzas` aunque la interfaz mostraba correctamente iconos visibles del shell.
- La causa era el helper E2E: sólo buscaba elementos `<mat-icon>`, mientras la navegación canónica usa también `<span class="material-icons">`.
- `matriz-modulos-visual.spec.ts` valida ahora ambos contratos de iconografía visibles sin relajar contraste, tamaño, opacidad ni navegación.
- Cambio exclusivo de prueba E2E; sin runtime funcional, datos, migraciones, secretos, DNS ni servicios pagos.

MAPA_ARQUITECTURA: SIN_CAMBIO.

## 2026-10-02 — Retiro de superficies de planificación histórica

- `TASKS.md` deja de almacenar rollups históricos numerados, parents, gates y snapshots de planes retirados; queda como superficie compacta de estado actual.
- Se retiran del árbol vivo los artefactos locales de planificación anteriores que ya no tienen autoridad; la trazabilidad histórica permanece en Git y la evidencia técnica/certificaciones funcionales no se convierten en roadmap.
- Los checks operativos dejan de depender de índices locales de planes históricos y validan directamente la autoridad vigente: Plan Maestro único + `docs/VAEP_AUTHORITY.md` + `CURRENT_STATE_ONLY`.
- Se corrigen referencias operativas residuales a ramas/PRs históricos y se normaliza la colaboración IA al modelo CURRENT_STATE_ONLY sin lanes, fases ni handoffs retirados como autoridad.
- Se endurece el gate de scope para impedir la reintroducción de superficies de planificación retiradas, identificadores operativos históricos o autoridades antiguas en documentos de estado actual.
- Cambio de gobierno/documentación/CI únicamente; sin cambios funcionales de backend/frontend, datos, migraciones, secretos, QA, main ni PROD.

MAPA_ARQUITECTURA: SIN_CAMBIO.

## 2026-10-02 — Fase 0 de modernización: baseline técnico reproducible

- Se incorpora un baseline de estado actual previo a cualquier migración de versiones, sin modificar runtime funcional ni versiones productivas.
- El baseline congela dependencias directas, TargetFramework, imágenes Docker, MySQL configurado y el blob vigente de `frontend/package-lock.json` en `docs/evidencias/modernizacion/BASELINE_FASE_0.json`.
- Se agrega un workflow exact-head que exige scope SOLQARYN, backend Release + tests, Docker, auditoría NuGet, frontend lint/audit/bundle, seguridad/tenancy/secret scan, MySQL 8.4 efímero, Playwright integral, versión MySQL Aiven DEV, backup cifrado real de DEV y restore del mismo artifact en MySQL descartable.
- El workflow también captura las versiones realmente resueltas de Node, npm, .NET SDK, SO del runner, árbol npm directo, hashes SHA-256 y evidencia de bundle.
- Toda operación de base real queda limitada a DEV; el restore está fail-closed a un MySQL local descartable y declara `productionTouched=false`.
- Este changeset sólo crea evidencia/gates de modernización. No cambia Angular, Node, .NET, EF, provider MySQL, datos productivos, QA ni PROD.

MAPA_ARQUITECTURA: SIN_CAMBIO.

## 2026-10-02 — Recovery Fase 0: aislamiento tenant exacto en Sucursales

- El baseline de modernización detectó un rojo real: el permiso HTTP validaba una Empresa, pero el servicio de Sucursales todavía aceptaba un EmpresaId independiente en query/body y lecturas sin filtro, permitiendo desacoplar el tenant autorizado del recurso consultado.
- El gate de permisos conserva ahora, sólo después de verificar membresía/rol/grant, el EmpresaId efectivamente autorizado en HttpContext; Sucursales consume exactamente ese valor en búsquedas, lecturas y mutaciones.
- Buscar/listar sin EmpresaId queda acotado automáticamente al tenant autorizado; un EmpresaId explícito distinto falla cerrado. Lectura/estado/eliminación de una sucursal de otro tenant se comportan como no encontrada y update/create no pueden cruzar tenants.
- El auditor multi-tenant se corrige para reconocer UsuarioEmpresa como binding canónico, validar el handoff de tenant realmente autorizado, tratar BackgroundService como N/A cuando no existe runtime de background y reconocer la clave de idempotencia de correo acotada por PK global de Factura + usuario + destinatario + clave.
- Se añadieron/ajustaron regresiones tenant-aware y se fuerza una nueva certificación Fase 0 sobre el HEAD exacto del recovery.
- Sin cambios de versiones, migraciones, datos productivos, QA ni PROD.

MAPA_ARQUITECTURA: SIN_CAMBIO.

## 2026-10-02 — Fase 0: cancelar certificaciones stale al avanzar HEAD

- El workflow de baseline pasa a `cancel-in-progress: true` para que una ejecución ya invalidada por un recovery no bloquee la certificación del HEAD vigente.
- La política mantiene una única certificación exact-head activa y evita consumir runners en candidatos que ya no pueden cerrar la Fase 0.
- Sin cambios funcionales, versiones, datos, QA ni PROD.

MAPA_ARQUITECTURA: SIN_CAMBIO.

## 2026-10-02 — Recovery Fase 0: contrato tenant y E2E canónico

- El primer baseline exhaustivo reveló dos clases de rojo: un contrato de Sucursales demasiado acoplado a `EmpresaId` enviado por el cliente y una ejecución Playwright no canónica que mezcló suites históricas/aisladas bajo un único estado compartido.
- Sucursales mantiene fail-closed el tenant autorizado: si el cliente omite `EmpresaId`, se usa el tenant ya autorizado por el gate HTTP; si lo envía, debe coincidir exactamente o la operación falla. Esto conserva compatibilidad sin volver a confiar en un tenant controlado por el body.
- Los validators aceptan `EmpresaId` ausente y sólo rechazan valores no positivos cuando se especifica.
- El baseline E2E se alinea con la suite de aceptación integral vigente, materializa el tenant 1 y una suscripción SaaS descartable, y fija explícitamente `E2E_TENANT_ID`/`PHASE7_EMPRESA_ID`.
- Se mantiene la evidencia de la corrida fallida como diagnóstico; no se oculta ni se reescribe historia. La nueva corrida debe certificar el HEAD exacto antes de cerrar Fase 0.
- Sin cambios de versiones, migraciones persistentes, QA ni PROD.

MAPA_ARQUITECTURA: SIN_CAMBIO.

## 2026-10-02 — Recovery Fase 0: alinear contratos tenant y entorno runtime

- Se corrigen cuatro pruebas N6.2 que aún exigían que el cliente enviara `EmpresaId`, aunque el servicio actual ya resuelve el tenant propietario exclusivamente desde el contexto previamente autorizado del servidor.
- Las regresiones ahora certifican que omitir `EmpresaId` conserva/persiste exactamente el tenant autorizado y que el cliente no puede sustituirlo por otro tenant.
- El runtime E2E de Fase 0 vuelve a `Staging`; el seed canónico no depende de `Development` y Swagger debe permanecer deshabilitado durante la certificación fail-closed.
- Sin cambios de versiones, migraciones, datos productivos, QA ni PROD.

MAPA_ARQUITECTURA: SIN_CAMBIO.

