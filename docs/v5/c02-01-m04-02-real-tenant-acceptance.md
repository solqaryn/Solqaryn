# C02-01.M04.02 — Evidencia R2 de aceptación autenticada

Alcance: selección empresarial Angular 22.2.1, JWT real ASP.NET Core 10, MySQL 8.4 aislado y Chromium. Este changeset añade pruebas de integración adversariales y un workflow dirigido; no cambia endpoints, tablas ni despliegues.

Pruebas: identidad A puede acceder solo a empresa A, identidad B solo a B; listado de membresías devuelto por la API; intento de usar tenant B con JWT A debe producir HTTP 403/404 y retorno a login; JWT con firma alterada y acceso anónimo denegados. No se interceptan respuestas de autorización. Runner CI efímero utiliza usuarios y claves aleatorias, bootstrap y API reales, Angular y Playwright sin la fixture sintética habitual.

A0: diff de tests y workflow contra contrato de estados 0/1/2 empresas, sin modificar lógica de UI. A1/A2/A3: verificar ejecución real y errores por SHA; no dar PASS por SKIPPED, CANCELLED o IN_PROGRESS. A4: Scope Lock, V5, CI causal, readback Sheet y BITACORA; preservar VALIDANDO hasta evidencia suficiente. Host falso y revocación dinámica de membresías, si aún no cubiertos, siguen pendientes; A5 corresponde a tercera Task distinta de DEV05 autor y DEV01 reparador.

Rollback: revert de commit normal en dev. QA/main/PROD, secretos y tareas físicas intactos.

## V21 rescate DEV06 — nueva hipotesis: revocacion SQL post JWT
El cliente que obtuvo un JWT valido antes del cambio de membresia B no debe conservar la lectura B tras UPDATE directo REAL de UsuarioEmpresas.Activa=FALSE dentro de MySQL 8.4 efimero. La suite inicial verifica B positiva y guarda el token solo con permisos 0600 en RUNNER_TEMP (no artifacts ni logs); la nueva fase SQL exige que queden cero membresias activas y una nueva prueba Playwright API exige 401/403/404 para /tenant-context/2 y ausencia de B en /tenant-context/empresas. Cualquier 200 en lectura B, 503 o fallo de la comprobacion SQL provoca fallo CI, no un PASS sintetico. No cambia runtime, grants, planes ni variables de produccion. Requerir GitHub Actions completed/success exact-SHA, Scope Lock, V5, readback y A5 por tercero distinto del autor DEV05 y reparadores DEV01/DEV06. Host falso, UI revocada y toda la matriz siguen pendientes hasta pruebas especificas adicionales.
