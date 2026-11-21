# SOLQARYN V22 — C04-01.M06.01: correspondencia entre pruebas y alcance real

**Ámbito:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Riesgo:** R0 documental (auditoría de evidencia publicada; no cambia runtime). **Tarea física autora:** JAVIER DEV01, `6adbb9fff1548191a1bd5b30d9e9984a`. **Corte de código auditado:** `3a6d757d997bc6616d9787f93b01bfdf8fadb994`, 2026-11-21.

## Criterio de aceptación delimitado

`PLAN_MAESTRO!D608` exige **«Revisar que las pruebas correspondan al alcance real y no a mocks insuficientes»**. `PLAN_MAESTRO!E608` describe el objetivo **acumulativo** de M06 y no autoriza certificar facturación, cuotas ni todos los entregables de M05/M06 en una sola unidad. El presente artefacto contrasta el alcance declarado de seis microtareas M05 con pruebas realmente ejecutadas, separa transporte real de identidades sintéticas y conserva las obligaciones pendientes. No añade endpoints, servicios, migraciones, precios, contabilidad ni proveedor externo.

## Matriz de seis pruebas y límites verificables

| ID / SHA de entrega | Prueba acreditada en dev y revisión independiente | Qué **sí** acredita | Qué **no** acredita |
| --- | --- | --- | --- |
| `C04-01.M05.01` / `e1a5ee640d4a7d25a2120e0cfff7a1da6c2d7062` | Angular TestBed/Vitest **6/6**, negativos de referencia; A5 por DEV03 | Invariantes de componente/política de lectura con fixture | Cobro, cuota productiva, backend JWT/SQL ni tarifas autorizadas |
| `C04-01.M05.02` / `655d5001af0b7c90ded883356fbb33e1a6d9ad9f` | ASP.NET Core 10 TestServer + aplicación/middleware real, **6/6** xUnit; A5 por DEV06 | JSON `ApiResponse`, HTTP 200/400/403/503/500 y ocultación de error privado | Actor sintético **no** firma JWT real ni certifica proveedor MySQL/PSP |
| `C04-01.M05.03` / `a09612bb1fb33f27fff1bb839ffb02e816a52a15` | Oracle EF / MySQL **8.4.13 efímero** con rollback y revocación **1/1** TRX; A5 por DEV06 | Transacciones reales de laboratorio y aislamiento A/B en el caso SQL probado | Persistencia comercial, pago real, migración/rollout productivo |
| `C04-01.M05.04` / `75836d241b9a6a7481bfa61e0f56109a643c7455` | Playwright/Chromium real **4/4**; A5 por DEV04 | Navegación de referencia privada, teclado/guardas y presentación de aceptación | El login de fixture **no** demuestra JWT firmado ni checkout financiero |
| `C04-01.M05.05` / `9b54102b7aa40b59e27565211fc0c1bf9ea590cd` | HTTP TestServer **JWT firmado**, Oracle EF/MySQL 8.4.13 efímero, prueba adversarial **1/1**; A5 por DEV06 | Rechazo de spoofing A/B, usuario no miembro, token alterado y acceso revocado en contrato de pruebas | Endpoint real de facturación, compra, pagos, pricing/cuotas globales |
| `C04-01.M05.06` / `3a6d757d997bc6616d9787f93b01bfdf8fadb994` | Chromium real **5/5** sin skip; frontend tipado/build, seguridad y Scope Lock PASS exact-SHA; A5 por DEV01 distinta del autor DEV06 | Regresión readonly, cuatro anchos, foco mediante Tab/Enter, reducción de movimiento, tiempos de CI y 401/403/503 | Sesión sintética no es JWT real; tiempo CI **no** es Core Web Vitals de campo ni WCAG exhaustiva |

Fuente causal dirigida: [Chromium 5/5 M05.06](https://github.com/solqaryn/Solqaryn/actions/runs/44892159766/job/136810149216), [HTTP/DTO 6/6 M05.02](https://github.com/solqaryn/Solqaryn/actions/runs/44890825950/job/136806032102), [MySQL aislado M05.03](https://github.com/solqaryn/Solqaryn/actions/runs/44890557494/job/136805225899), [Chromium M05.04](https://github.com/solqaryn/Solqaryn/actions/runs/44889532480/job/136802106742), [JWT/MySQL M05.05](https://github.com/solqaryn/Solqaryn/actions/runs/44890020052/job/136803604770). Estos enlaces son pruebas de **sus casos**, no certificación de todo M05. La autoridad de estado/A5 es el readback actual de PLAN/COLA/BITACORA, no una descripción anterior.

## Contrapruebas, falsos positivos que hay que impedir

1. **Rechazar:** «Angular/Chromium verde implica autenticación JWT backend, rol y aislamiento real». Hay fixtures explícitos `NOT_SIGNED_JWT`; solo M05.05 prueba JWT firmado y MySQL aislado en su contrato específico.
2. **Rechazar:** «TestServer HTTP 403 implica todo proveedor/servicio comercial autorizado». M05.02 inyecta `ActorSource` y `PlanSource` de laboratorio, sin activar servicio productivo.
3. **Rechazar:** «MySQL 1/1 implica precio, periodicidad, upgrade/downgrade, cobro real y auditoría contable». El caso demuestra un rollback/revocación puntuales; no hay PSP ni cobro real autorizado.
4. **Rechazar:** «Playwright 5/5 a 320/375/768/1280 certifica WCAG completo, hardware físico o métricas Core Web Vitals reales». Se midieron solamente invariantes de Chromium en CI y límites de tiempo/DOM.
5. **Rechazar:** «Una CI general SKIPPED/CANCELLED/FAIL/IN_PROGRESS equivale a PASS». El job pertinente se evalúa por SHA y conclusión; la prueba no aplicable queda identificada como tal, no transmutada a éxito.
6. **Rechazar:** «LISTO de seis IDs y sus A5 permite pasar a QA, main, PROD o emitir pagos». La aprobación dev no equivale a permiso R3; no cambiar gating técnico ni fórmulas.

## Acciones que permanecen abiertas

**R2 — software/comercio:** incorporar en scopes específicamente autorizados la aplicación real de precios y límites *server-side*, versión vigente del plan, periodicidad, transiciones contractuales, persistencia por tenant, autoridad JWT/RBAC y pruebas negativas con MySQL 8.4 aislado. No atribuir estos trabajos a `C04-01.M06.01`.

**R3 — producción y pagos:** desplegar/promover, habilitar PSP, ejecutar pagos o alterar datos reales queda explícitamente fuera de esta revisión; requiere una orden específica. No emitir un A5 propio para `C04-01.M06.01`: corresponde a una **Task Javier distinta de DEV01** tras evidencia de este artefacto y sus checks causales.

**Dependencias:** `C04-01.M06.01–05` conservan sus estados y A5 individuales; `M06.06` solo se habilita por fórmulas DAG después de los gates reales, sin sustitución manual de `COLA!G/O`.

## A0–A4 de esta salida documental

- **A0:** alcance D608 contrastado contra D/E602–607 y diffs/documentos/trabajos de seis entregas; solo se añade este Markdown.
- **A1:** matriz de positivos y cinco falsos positivos adversariales explícitos y revisables, incluyendo frontera entre transporte real, DB efímera y fixture; no se reporta una suite nueva que no se haya ejecutado.
- **A2:** segregación tenant/JWT/RBAC queda expresamente no certificada fuera de M05.05; ninguna política de seguridad se altera.
- **A3:** **NO_APLICA una integración nueva** a un diff exclusivamente documental; se enlazan las integraciones anteriores verdaderamente ejecutadas sin elevar su alcance.
- **A4:** publicar mediante commit directo serializado y fast-forward/CAS sobre HEAD `dev`; comprobar Scope Lock exact-SHA y la procedencia V5 si corresponde, fuente en Git y readback PLAN/COLA/BITACORA; conservar `VALIDANDO` si CI pertinente aún no terminó. No modificar `CHANGELOG_AI.md` enorme: preservar sus blobs/historia; registrar entrega en BITACORA. Rollback: `git revert` ordinario del commit documental tras nueva validación, sin force-push.

**Resultado:** una revisión independiente de alcance vs mocks; **ninguna certificación de pagos/producción**.