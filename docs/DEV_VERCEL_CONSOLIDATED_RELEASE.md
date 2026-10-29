# SOLQARYN — Publicación consolidada en Vercel DEV

**Alcance único:** repositorio `solqaryn/Solqaryn`, rama protegida `dev`, proyecto Vercel `solqaryn-dev` (`prj_1Anhx5mWyXEBX89lWC24Py6JXe7A`). **Nunca** `solqaryn-qa` ni `solqaryn-prod`. No modifica bases de datos, JWT, tenancy, secretos, QA, `main` ni PROD.

## 1. Desarrollo continuo y publicación de DEV

- Los commits y merges siguen ejecutándose por PR protegido con tres checks: `verify-solqaryn-project-scope`, `SOLQARYN - Seguridad multiempresa` y `SOLQARYN - Build y pruebas backend`.
- `frontend/vercel.json` establece `git.deploymentEnabled.dev=true`, habilitando los despliegues Git automáticos únicamente para la rama protegida `dev` del proyecto Vercel `solqaryn-dev`. QA y PROD no se modifican. Las previews de PR permanecen sujetas a la configuración propia del proyecto y no se habilitan con este cambio.
- `frontend/scripts/vercel-ignore-build.mjs` conserva el filtro de cambios de gobernanza para DEV. Una omisión de build NO se interpreta como despliegue `READY`; los cambios de `frontend/vercel.json` sí provocan build.
- El workflow consolidado continúa como verificación y recuperación del HEAD vivo: valida sus tres checks exactos y nunca selecciona arbitrariamente un SHA histórico ni un commit de un PR.

## 2. Publicación consolidada

El workflow `.github/workflows/solqaryn-dev-vercel-consolidated.yml` funciona por `workflow_dispatch` (input opcional `sha`, que debe coincidir con el HEAD vivo de `dev`) y por programación horaria al minuto `:53` UTC. Puede omitir la publicación cuando ya existe un deployment `READY` con el mismo SHA, o cuando existe un intento explícito de publicación consolidada reciente (menos de una hora). `concurrency` serializa las ejecuciones.

El job lee la configuración de `frontend/vercel.json`, ejecuta su suite negativa de seguridad, valida SHA y los tres checks exactos desde GitHub y, solamente después, llama al API de Vercel para el **proyecto DEV**. El `target=production` en la llamada Vercel significa el alias estable interno del proyecto **`solqaryn-dev`**: no toca el proyecto `solqaryn-prod` ni la rama `main`.

**Secreto obligatorio de GitHub Actions:** `VERCEL_TOKEN`, habilitado para GitHub Actions con acceso de mínimo privilegio al proyecto DEV del equipo SOLQARYN. Nunca registrar ni copiar su valor en archivos, chats o logs. El token de GitHub proviene de `github.token`. Si el secreto no existe o el proveedor deniega el despliegue, el job falla de manera explícita, sin declarar éxito. No contratar planes ni comprar capacidad.

## 3. Verificación de extremo a extremo

1. Confirmar que los merges a `dev` originan deployments en `solqaryn-dev` y consultar su estado por SHA/ref. Distinguir la ausencia de deployment de un estado `CANCELED`; ninguno de esos dos acredita `READY`.
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

## 3.3 Cambios en scripts raíz y el Ignored Build Step

El release explícito requiere un deployment READY del SHA exacto. Por eso frontend/scripts/vercel-ignore-build.mjs no debe clasificar todo el directorio raíz scripts/ como no runtime: ahí viven scripts/dev-vercel-release.mjs y tooling que controla la creación y certificación del deployment. Si el ignore command devuelve 0, Vercel cancela el deployment y el release falla correctamente al no observar READY. Los cambios en docs/, .github/, .agents/ y vaep/ pueden seguir omitiendo el build DEV; los cambios bajo scripts/ raíz deben permitirlo. QA y PROD conservan su política de build en sus ramas canónicas.

## 4. Recuperación

Rollback de política mediante PR protegido que restaura exclusivamente `git.deploymentEnabled.dev=false` y la aserción del workflow/script. No se usa `force-push`, no se despliega QA/PROD y no se cancela trabajo ajeno.

**Métrica de productividad:** microtareas en `LISTO` con cierre verificado por hora, no número de builds. La cuota de Vercel afecta la frecuencia de publicación, no autoriza omitir una prueba funcional que exija frontend desplegado.
