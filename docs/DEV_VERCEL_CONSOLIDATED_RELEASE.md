# SOLQARYN — Publicación consolidada en Vercel DEV

**Alcance único:** repositorio `solqaryn/Solqaryn`, rama protegida `dev`, proyecto Vercel `solqaryn-dev` (`prj_1Anhx5mWyXEBX89lWC24Py6JXe7A`). **Nunca** `solqaryn-qa` ni `solqaryn-prod`. No modifica bases de datos, JWT, tenancy, secretos, QA, `main` ni PROD.

## 1. Desarrollo continuo, publicación desacoplada

- Los commits y merges siguen ejecutándose por PR protegido con tres checks: `verify-solqaryn-project-scope`, `SOLQARYN - Seguridad multiempresa` y `SOLQARYN - Build y pruebas backend`.
- `frontend/vercel.json` establece `git.deploymentEnabled.dev=false`, que impide **autodeploy Git de `dev`**. Las otras ramas no figuran en esta regla. El proyecto Vercel DEV tiene además las previews automáticas desactivadas (configuración de proyecto); verificación de esa propiedad desde el proveedor requerida ante cualquier cambio futuro.
- `frontend/scripts/vercel-ignore-build.mjs` mantiene la barrera de proyecto/rama (`dev` sólo en proyecto DEV; QA y PROD en sus ramas), pero **NO cancela una publicación de la rama canónica** por cambios únicamente documentales/CI. En Vercel `exit 0` cancela la construcción y `exit 1` la permite; la decisión de solicitar un release DEV corresponde exclusivamente al workflow consolidado, protegido con CI exact-SHA, token y cuota. `git.deploymentEnabled.dev=false` permanece intacto: no se reactiva autodeploy por commits.
- Múltiples commits se integran en `dev`; el release escoge **únicamente el HEAD vivo** que pasó 3/3 checks. Nunca selecciona arbitrariamente un SHA histórico ni un commit de un PR.

## 2. Publicación consolidada

El workflow `.github/workflows/solqaryn-dev-vercel-consolidated.yml` funciona por `workflow_dispatch` (input opcional `sha`, que debe coincidir con el HEAD vivo de `dev`) y por programación horaria al minuto `:53` UTC. Puede omitir la publicación cuando ya existe un deployment `READY` con el mismo SHA, o cuando existe un intento explícito de publicación consolidada reciente (menos de una hora). `concurrency` serializa las ejecuciones.

El job lee la configuración de `frontend/vercel.json`, ejecuta su suite negativa de seguridad, valida SHA y los tres checks exactos desde GitHub y, solamente después, llama al API de Vercel para el **proyecto DEV**. El `target=production` en la llamada Vercel significa el alias estable interno del proyecto **`solqaryn-dev`**: no toca el proyecto `solqaryn-prod` ni la rama `main`.

**Secreto obligatorio de GitHub Actions:** `VERCEL_TOKEN`, habilitado para GitHub Actions con acceso de mínimo privilegio al proyecto DEV del equipo SOLQARYN. Nunca registrar ni copiar su valor en archivos, chats o logs. El token de GitHub proviene de `github.token`. Si el secreto no existe o el proveedor deniega el despliegue, el job falla de manera explícita, sin declarar éxito. No contratar planes ni comprar capacidad.

## 3. Verificación de extremo a extremo

1. Evidencia de que los commits de PR intermedios NO originan deployments automáticos en `solqaryn-dev`; usar `list_deployments` por SHA/ref y comparar con el momento de integración. Distinguir la ausencia de deployments de un estado `CANCELED`; ninguno de esos dos acredita `READY`.
2. SHA candidato: HEAD vivo del `dev` protegido con los tres checks obligatorios completados exitosamente para ese mismo SHA.
3. Un deployment lanzado explícitamente debe devolver ID de Vercel, pertenecer al proyecto DEV, finalizar con `READY` y exponer `gitSource.sha` o `meta.githubCommitSha` coincidente. Se rechaza URL sin SHA verificable, deployment `CANCELED`, `BLOCKED` o `ERROR`.
4. Separar aceptación del **código** (CI/PR/merge) de aceptación del **despliegue** (estado READY + SHA y pruebas HTTP cuando el scope las exija). Una microtarea como P0-001.B que requiere paridad Render/Vercel no pasa a `LISTO` solo porque la publicación está programada.
5. Leer los eventos del deployment si el proveedor rechaza el job; no repetir a ciegas cuando haya límite Hobby. El target y la ruta siempre son DEV.

## 3.1. Cuota Hobby compartida y evidencia negativa real

El 25-10-2026, el proveedor respondió a una solicitud de despliegue explícito del HEAD protegido `08f84a3b096325b05702a44630f0c2fdb0c1fc4c` con `402 payment_required`, código `api-deployments-free-per-day` y `retryAfter=86400`: **no se obtuvo `READY` ni deployment ID nuevo**. En los últimos cien intentos visibles del equipo SOLQARYN había 32 del proyecto DEV, 34 de QA y 34 de PROD, todos `CANCELED`, principalmente porque se dispararon en ramas que el Ignored Build Step rechazaba. Los tres proyectos comparten la cuota del equipo. Esta observación es histórica, no prueba la capacidad futura.

Para evitar intentar publicaciones inútiles cada hora, `scripts/dev-vercel-release.mjs` hace una consulta **de solo lectura** al conjunto de deployments del equipo; si hay 95 o más intentos en la ventana de 24 horas, omite la llamada de creación y declara `NO_READY_ATTESTED`. La ejecución no infiere un límite distinto al que informe Vercel ni cambia permisos o proyectos QA/PROD. El resultado `READY` debe provenir de una nueva publicación exitosa y del SHA real, nunca de las pruebas offline.

## 3.2. GitHub Actions 404 en detalle histórico de Vercel (28-10-2026)

El HEAD protegido `038da7e9dca2addd1e185737117468cab3534286` tiene CI 3/3 PASS y Render DEV LIVE, pero el release consolidado reportó `PROVIDER_HTTP_404` / `Deployment not found` al intentar leer el detalle de un deployment `READY` anterior. Se observó en tres runs programados: `40610934731`, `40643507499`, `40698037166`. La misma referencia histórica aparece en la lista del proyecto y es consultable desde una conexión Vercel autenticada diferente: el 404 del token de GitHub Actions **no acredita eliminación real** ni fallo del despliegue nuevo.

La consulta detallada de cada candidato `READY` anterior es **optativa para calcular equivalencia de fuente**. Ante `404` en esa llamada de solo lectura, se omite exclusivamente el candidato, se imprime `DEV_HISTORICAL_READY_DETAIL_HTTP_404=UNAVAILABLE SOURCE_EQUIVALENCE=NOT_ATTESTED` y se comprueba el siguiente candidato. No se declara paridad por ese registro, ni se promociona un SHA histórico. Los errores `401/402/403/429/5xx` y todos los errores de GitHub, cuota, despliegue nuevo o SHA siguen siendo fatales/no autorizan certificación. Después de agotar candidatos, el motor conserva sus controles originales de 3/3 checks exactos, anti-duplicado, cuota compartida y destino **solo** proyecto Vercel DEV; únicamente un nuevo despliegue `READY` con SHA exacto puede certificar la publicación.

Si la publicación nueva también recibe 404/403 desde el token de GitHub Actions, ese es un **problema de acceso del token `VERCEL_TOKEN`**, no un permiso para usar otra cuenta, copiar credenciales a logs o simular `READY`. Revisar sus permisos de proyecto y equipo en GitHub Actions/Vercel por el propietario; no repetir el mismo error de forma agresiva. El conector Vercel de ChatGPT y el token de GitHub Actions pueden tener autorizaciones distintas.

## 3.3. Causa probada: Ignored Build Step cancela releases DEV (28-10-2026)

- GitHub Actions `40707395328`, job `122883098883`, verificó `dev@d2ea67586b7639a35b18c0b769568086f4c17c41` y 3/3 CI; creó `dpl_FcNqTvrcA7rXpCnDoR7B87DfD5eq`, pero el proveedor devolvió `CANCELED`.
- Los eventos de build de ese deployment prueban `NON_RUNTIME_GOVERNANCE_ONLY_SKIP files=3 branch=dev` y `The deployment was canceled because the Ignored Build Step command returned exit code 0.` No fallaron credenciales, compilación ni cuotas en ese intento: Vercel canceló por una regla local de optimización.
- Corrección: el Ignored Build Step **siempre permite el build de una rama canónica de un proyecto conocido** (exit 1), aunque el diff sea de gobierno/documentación. Para proyecto/rama cruzados conserva `CROSS_ENV_PROJECT_SKIP` (exit 0). ID de proyecto desconocido continúa sin certificado y debe pasar por los controles de binding del runtime.
- Pruebas offline negativas comprueban la matriz DEV/QA/PROD, ramas ausentes y proyectos desconocidos, tanto en PR como antes de una publicación programada. No cambian `git.deploymentEnabled.dev=false`, el guard de un release por hora, el umbral Hobby de 95/24h, checks GitHub 3/3, ni la exigencia de `READY` con SHA verificado. El estado no es CERTIFICADO hasta que una publicación posterior termine en READY y se lea el SHA y la paridad real. No forzar reintentos contra un rechazo del proveedor.

## 4. Recuperación

Rollback de política mediante PR protegido que revierte exclusivamente `git.deploymentEnabled.dev=false` y workflow/script, luego actualización explícita de la configuración de previews del proyecto DEV si se desea reactivar ese comportamiento. No se usa `force-push`, no se despliega QA/PROD y no se cancela trabajo ajeno.

**Métrica de productividad:** microtareas en `LISTO` con cierre verificado por hora, no número de builds. La cuota de Vercel afecta la frecuencia de publicación, no autoriza omitir una prueba funcional que exija frontend desplegado.
