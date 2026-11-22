# C02-04.M05.06 — regresión, rendimiento sintético y accesibilidad de consola global

Riesgo **R2** por rutas privadas, control-plane, autenticación y navegador. Autor: DEV04 `6aeacb7820ac8191b7d768a6f33dfa09`. Rama dev exclusiva, Scope Lock STRICT, PLAN fila 427, candado V17 `2000000427`. Pruebas Chromium GitHub-hosted; no modifica aplicación ni backend: sólo spec, workflow, este documento y CHANGELOG.

## A0: contrato, dependencias y rollback
Reutiliza el catálogo existente `docs/v5/c02-04-m05-01-routes.json` y la ruta Angular real `/plataforma/consola-global-v5`, con guard/interceptor/routing reales y el endpoint `/plataforma/sesion` **interceptado sintéticamente**. No se prueba JWT HTTP firmado, MFA, revocación de servidor, otorgamiento de grants, base MySQL real, REST productivo, cookies de terceros ni autorización entre empresas A/B. No se modifica Angular 22, API .NET 10, migraciones EF Oracle, secretos ni cuentas de proveedor. Revertir el commit con git revert normal, no force-push.

## A1 — seis pruebas fallables Chromium
1. Catálogo contractual de 15 IDs/rutas únicos; las 15 rutas reales renderizan encabezado tras sesión 200 interceptada y emiten cero POST/PUT/PATCH/DELETE.
2. Consola en 320/375/768/1280: cuatro tarjetas, encabezado semántico, cero overflow horizontal, revalidación con objetivo vertical >=44px, Tab natural desde inicio (sin `.focus()`) alcanza botón y Enter envía exactamente otro GET; foco visible medido.
3. Contraste **computado** de color del contenedor de marca >=4.5:1 en estados institucional/alto contraste. Interacción local no crea grants ni tráfico mutante; no se afirma WCAG integral.
4. Segundo GET retenido simula lentitud real del navegador: `aria-busy=true`, botón disabled, tarjetas privadas ocultas; HTTP 503 opaco sin revelar diagnóstico, tercer GET recupera la sesión y jamás emite mutación.

## A2/A3 — fronteras
El navegador y compilación Angular son reales en runner GitHub-hosted con Node 24/npm 11/Chromium. El backend de sesión está simulado a propósito: controles multiempresa/JWT/RBAC servidor y MySQL se mantienen **NO_APLICA a diff test-only / NO_MEDIDO como verificación end-to-end de autorización**. Se conservan resultados de otras microtareas sin reutilizar PASS de SHAs distintos. El runner usa localhost y no se conecta a Aiven/Render/Vercel productivos.

## A4/A5
Validar el run dirigido `V19 C02-04.M05.06 - Chromium global catalogue regression` con seis pruebas completadas SUCCESS exact SHA, Scope Lock, V5, tipado/build y seguridad pertinente. Un check QUEUED/IN_PROGRESS/FAILURE/CANCELLED/SKIPPED no es PASS. Con CI externa pendiente conservar VALIDANDO, owner y SHA/RUN; A5 independiente por otra Task Javier, nunca DEV04. Sin QA/main/PROD ni liberación por observaciones de texto.

## Reparación correctiva A5_FAIL P2 — 17/11/2026
La revisión independiente DEV06 detectó que el SHA 3d953b5d sólo navegaba 2/15 rutas y omitía negativas anónimo/403 y cobertura del inventario de inspección. Esta reparación añade navegación real de las 15 entradas, respuesta 403 sintética con redirección, navegación del inventario Angular y una medición controlada de 225 ms de espera sintética. Las seis pruebas no son una certificación de JWT firmado, backend HTTP ni del laboratorio independiente de QA (aún no disponible como ruta). La latencia de proveedores reales permanece NO_MEDIDO; no se inventa un SLO.
A5_FAIL previo debe permanecer en BITACORA y solo puede superarse mediante nueva A5 de una Task distinta tras CI exact-SHA. Revert normal sin force-push.

## V24 corrección causal de A5_FAIL — QA independiente existente (22/11/2026)
Nueva hipótesis verificable: el registro Angular vigente ya contiene una superficie QA **distinta** del inventario de rutas, `/plataforma/qa-global-v19` (componente `C1401QaGlobalComponent`, lista estática de cinco controles, `plataformaGuard`). Los seis casos anteriores sólo visitaban `/plataforma/inventario-rutas-v19`, por lo que no demostraban regresión de esa superficie QA.
Se agregaron dos pruebas Playwright adversariales (ocho en total): navegación Angular real en cinco anchos 320/390/768/1280/1440, cinco controles `NO_VERIFICADO`, captura 320/1440, cero desbordamiento/document writes; y denegación de usuario anónimo/HTTP 403 sin exposición de controles. La sesión HTTP continúa siendo **interceptada sintéticamente**, sin certificar JWT firmado, laboratorio backend aislado, MySQL, MFA ni decisiones de permiso del servidor. El panel QA es inventario estático y **no equivale** a una ejecución real de jobs o a un laboratorio de pruebas de aislamiento: la revisión A5 deberá decidir si queda una brecha de laboratorio no cubierta.
A0 alcance test-only/CI/documentación; A1/A2/A3 requieren ejecución causal en GitHub-hosted; A4 Scope Lock STRICT, V5 y CI exigibles `COMPLETED/SUCCESS` del nuevo SHA. Mantener `PLAN_MAESTRO!H=LISTO`, autor DEV04, `A5_FAIL` histórico e impedir que DAG libere M06 por inferencia. Nueva A5 independiente por Task Javier distinta de DEV04 autor y DEV06 reparador, sólo después de evidencia. Rollback: revert normal del changeset, sin force-push.
