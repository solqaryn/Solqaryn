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
