# C04-01.M05.04 — aceptación positiva del catálogo privado de referencia (V22)

**PLAN_MAESTRO D605:** "Ejecutar caso positivo de aceptación del alcance"; E605 "Completa M05" es META ACUMULATIVA de fase, no exige que esta sola unidad active el producto comercial. Autor DEV03 Javier, ámbito **dev** solamente, PROJECT_SCOPE_LOCK=STRICT. V17 namedRange SQV17_C04_01_M05_04 fila 605; riesgo conservador R2 por contexto SaaS/billing. No cambios a producción, contratos comerciales, precios, secretos, API, DB ni JWT.

## A0 — alcance, dependencias y rollback

Se añade una suite E2E Playwright contra el Angular real existente en la ruta privada /plataforma/catalogo-planes-v22. No se altera el componente ni sus rutas/guards y NO se crea un checkout, cobro, plan activo o límite comercial. La aceptación positiva está circunscrita al contrato realmente implementado: referencia privada con tres secciones congeladas, titular claro, marcador NO_CERTIFICADO, navegación accesible y ausencia de operaciones comerciales. Depende de M04.01 frontend existente. Rollback con git revert normal del commit exacto en dev, nunca force-push.

## A1/A2 — pruebas capaces de detectar defectos

Cuatro escenarios reales de Chromium en GitHub-hosted: (1) usuario de plataforma autorizado ve tres secciones exactas y NO_CERTIFICADO, sin POST ni controles comerciales; (2) activación de enlace por teclado real Enter y navegación a /plataforma; (3) sesión inicialmente autorizada deja de estarlo, nueva petición 403 y redirección a login sin panel privado; (4) referencia válida a 320, 375, 768 y 1280 px sin desbordamiento ni activación de acción. Assert positivos, negativos, frontera de viewport y revocación. Fallan si el DOM, guard, navegación o política de no comercialización cambia.

## A3 — integración pertinente, limitaciones explícitas

Angular 22.2.1 + Playwright/Chromium reales, ejecutados en Linux GitHub-hosted desde commit SHA exacto. La respuesta del endpoint /plataforma/sesion es **un fixture sintético** y el token local no está firmado. No se afirma probar JWT firmado, HTTP real de backend, RBAC relacional, tenant A/B MySQL, persistencia comercial, PSP, límite server-side ni aceptación del catálogo facturable; esos quedan pendientes para unidades específicas M05.02/.03/.05/.06 y cierre acumulativo. D605 solo exige caso de aceptación positivo de la referencia existente; para este changeset test-only MySQL/EF y migraciones = NO_APLICA motivado.

## A4/A5 y entrega segura

Exigir que la CI dirigida ejecute realmente 4/4 Playwright en Chromium, cero FAIL/SKIP, tipado/build Angular, Scope Lock y procedencia V5 en SHA exacto COMPLETED/SUCCESS. Que el workflow esté QUEUED/IN_PROGRESS no acredita PASS; conservar VALIDANDO con SHA/run y readback, luego cerrar LISTO únicamente con A0–A4 causales. A5 por otra Task Javier de las seis distinta de DEV03 (autor) tras revisión independiente. BITACORA A:Z append-only, V17 CAS, no forzar DAG ni borrar eventos históricos. CHANGELOG_AI.md permanece íntegro sin reemplazo parcial del archivo grande: documentación del incremento y BITACORA registran la entrega, y la comprobación V5 de procedencia se exige en CI.
