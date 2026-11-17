# C00-04.M05.01 — regresion de reglas de gobierno V19

PROJECT_SCOPE_LOCK=STRICT; repositorio solqaryn/Solqaryn, rama dev. Propietario TASK DEV02.

## A0 alcance, impacto y rollback
- Criterio real del plan: ejecutar pruebas unitarias o reglas con resultado reproducible. Gate: PLAN H134=PENDIENTE/I vacia previo CLAIM, COLA G/O=SI, DAG F=VALIDADO/G=SI; candado V17 y CAS H=1, owner/readback, evento idempotente.
- Reglas existentes: `GobiernoCambiosReglas.CrearPropuesta` en Domain. Se agrega xUnit aislado; no se cambia Domain, Application, Infrastructure, API, UI, migraciones, Docker, dependencia, provider, JWT, RBAC, tenant, Sheets ni producción.
- La forma válida de un ID no certifica el catálogo institucional y `AutorizaEjecucion` permanece false. Los cambios sensibles conservan NO_GO.
- Rollback: `git revert` del commit de pruebas, documento y CHANGELOG sin force-push; no producción.

## A1 pruebas que pueden fallar
- Positivas: extremos de clave 12 y 128 caracteres, motivo de 600, revisión positiva máxima y SHA hexadecimal completo, identidad de datos en salida sin permisos.
- Negativas y frontera: clave 11/129, motivo 601, caracteres de control CR/LF/TAB, revisión cero/negativa, rollback ausente, cambios sensibles aunque tengan rollback.
- El ID sintáctico hipotético C99-99.M99.99 puede representar solo una propuesta inerte; ninguna prueba finge presencia de ese ID en PLAN_MAESTRO.

## A2–A3
- A2: fallar cerrado sin conceder ejecución en todos los casos, preservando límites de autorización. NO_APLICA certificación real de JWT, tenant A/B, RBAC HTTP y bases de datos: no se tocan esas capas.
- A3: compilar y ejecutar xUnit en .NET 10 Release con runner GitHub Actions del SHA del commit; no sustituir logs por lectura estática. MySQL/Playwright no son pertinentes al cambio de pruebas Domain.

## A4–A5
- Exigir CI `Backend Release y pruebas`, Scope Lock, V5 CHANGELOG, seguridad aplicable y pruebas dirigidas realmente `completed/success` exact-SHA. `queued`, `in_progress`, `cancelled`, `skipped` o `failure` nunca son PASS requeridos.
- Pasar a VALIDANDO al publicar entrega atómica y registrar SHA/RUN si la CI externa sigue ejecutándose; cerrar LISTO con A0–A4, lock V17, CAS/bitácora y readback. A5 solo revisor Javier de Task distinta.
- Las pruebas xUnit no ejecutan una operación real de Google Sheets ni una restauración, despliegue o transacción comercial. Sin QA/main/PROD.
