# SOLQARYN V17 - C03-04.M04.02 estados seguros de incidentes

Autor DEV03, TASK_ID 6adc2e4bd8a48191ac7d8641ed95a1a3. PLAN fila 561, V17 lock 2000000561; riesgo R2 por sesión y permisos de vista privada.

## A0 alcance
Extiende la ruta protegida existente sin cambiar guards, DTO backend, base de datos, migraciones, tenant real, restore, pagos ni secretos. Revalidación: exclusivamente GET /plataforma/sesion. Estado vacío es explícito: no hay feed de incidentes, jamás inventar incidentes. Éxito significa solo sesión confirmada, no incidente respondido.

## A1-A2
Nueve Vitest de permisos, carga, vacío, error, éxito, revalidación simple, duplicados, 403/503 y revocación; cinco Chromium sintéticos, con la guía oculta durante fallo o espera. Las pruebas requieren evidencia real de GitHub Actions en el SHA, no se declaran PASS antes de completarse. El backend sigue siendo autoridad de permiso y el cliente no opera restauraciones ni revocaciones.

## A3
Angular build y Playwright Chromium con API de sesión simulada. Sin integración nueva con backend, MySQL, EF, JWT real o datos de proveedores; pruebas sintéticas no certifican esos entornos.

## A4-A5
Verificar CI dirigida, Scope Lock y procedencia V5 SUCCESS exact-SHA y readback PLAN/COLA/BITACORA. A5 pendiente de otra Task Javier distinta del autor. Sin QA, main ni PROD.

## Rollback
Reversión mediante git revert normal de commit de esta unidad en dev, conservando commits concurrentes y BITACORA histórica. No force-push ni reset.

## Reparación causal de CI de accesibilidad M04.05
El primer SHA 09970551 produjo FAILURE del job 133090587789 (1/4 Vitest) al exigir cero botones, incompatible con el botón legítimo de revalidación ya implementado en M04.02. Se ajusta la prueba para exigir exactamente un botón type=button de sesión, ningún form/input y conservar foco, teclado, anonimato y revocación. Revalidar workflow M04.05 y M04.02 en nuevo SHA; el fallo previo sigue histórico. No es autorización para operaciones de incidentes.

## V19 — CI_REPAIR A5_FAIL P2 por revocacion durante GET (18 noviembre 2026)
La revision independiente DEV05 detecto que `PlataformaSesionService.obtener()` podia confirmar un HTTP 200 antiguo despues de `limpiar()` y restaurar autoridad cliente con la guia todavia montada. Se preserva A5_FAIL historico; no se atribuye bypass del backend/JWT/MySQL. DEV01 asiste tecnicamente sin sustituir autor/owner DEV03.

La correccion introduce una version monotona de comprobacion, invalida peticiones al limpiar y cuando comienza una comprobacion nueva; un 200 o error de una version anterior no modifica el estado de la sesion actual. Regresiones adversariales: revocacion a mitad de GET, resultado antiguo tras revalidacion nueva, error tardio que no limpia autoridad mas reciente, y TestBed con servicio HTTP real donde un 200 tardio no reabre la guia. El workflow original ejecuta ambas suites Vitest, Angular lint/build y Chromium dirigido. Conservar H=LISTO, I=DEV03 y A5_FAIL hasta un nuevo A5 independiente por Task distinta del autor real del cambio. CI requerida debe acreditar SHA exacto; mientras este QUEUED/IN_PROGRESS/FAIL no declarar PASS ni levantar el gate. Sin cambios de backend, JWT, RBAC, tenant MySQL, QA, main, PROD o secretos; rollback normal git revert.
