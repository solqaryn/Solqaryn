# SOLQARYN — Publicación consolidada en Vercel DEV

**Alcance único:** repositorio `solqaryn/Solqaryn`, rama protegida `dev`, proyecto Vercel `solqaryn-dev` (`prj_1Anhx5mWyXEBX89lWC24Py6JXe7A`). **Nunca** `solqaryn-qa` ni `solqaryn-prod`. No modifica bases de datos, JWT, tenancy, secretos, QA, `main` ni PROD.

## 1. Desarrollo continuo, publicación desacoplada

- Los commits y merges siguen ejecutándose por PR protegido con tres checks: `verify-solqaryn-project-scope`, `SOLQARYN - Seguridad multiempresa` y `SOLQARYN - Build y pruebas backend`.
- `frontend/vercel.json` establece `git.deploymentEnabled.dev=false`, que impide **autodeploy Git de `dev`**. Las otras ramas no figuran en esta regla. El proyecto Vercel DEV tiene además las previews automáticas desactivadas (configuración de proyecto); verificación de esa propiedad desde el proveedor requerida ante cualquier cambio futuro.
- El script anterior `frontend/scripts/vercel-ignore-build.mjs` se conserva. Una omisión de build NO se interpreta como despliegue `READY`.
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

## 4. Recuperación

Rollback de política mediante PR protegido que revierte exclusivamente `git.deploymentEnabled.dev=false` y workflow/script, luego actualización explícita de la configuración de previews del proyecto DEV si se desea reactivar ese comportamiento. No se usa `force-push`, no se despliega QA/PROD y no se cancela trabajo ajeno.

**Métrica de productividad:** microtareas en `LISTO` con cierre verificado por hora, no número de builds. La cuota de Vercel afecta la frecuencia de publicación, no autoriza omitir una prueba funcional que exija frontend desplegado.
