# C04-04.M04.03 — branding y configuración visual permitida (V22)

Fuente: PLAN_MAESTRO!D706. Entrega atómica de **aplicar branding, configuración dinámica y componentes permitidos** a la referencia privada y desconectada de enlaces de cobro en SOLQARYN dev. Riesgo R2 por proximidad financiera; sin autorización R3 ni flujo cobrable.

## Alcance y fronteras A0
- Angular 22.2.1 standalone, Signals e input reactivo: temaSolicitado admite únicamente base o contraste. Cualquier cadena no permitida, HTML hostil o carga larga cae en base; nunca se refleja texto de fuente externa ni se importan logotipos o scripts arbitrarios.
- El componente existente sigue bajo plataformaGuard y autorizado() de sesión Plataforma/administrador. Los tres pasos informativos SIN_CONEXION, la ruta y el backend actual permanecen intactos.
- El preset contraste emplea colores del sistema Canvas/CanvasText, bordes visibles y foco perceptible. Es un cambio local de presentación, no una nueva marca comercial por tenant.
- Sin API HTTP nueva, JWT emitido o alterado, pertenencia a empresa, migración Oracle EF/MySQL, PSP, webhooks, cuentas, compras, enlaces cobrables, secretos, datos productivos ni motor de temas remoto.

## Matriz A1–A5 y CI causal
- A1: seis nuevos Vitest/TestBed positivos, negativos, revocación, excepción de configuración, HTML hostil, frontera de 4096 caracteres y estados sin operaciones financieras; se mantienen los cuatro casos originales de guard y seis de estados M04.02.
- A2: verificación local de no elevación de privilegios, de ocultación tras revocación, de lista estricta y de no inyección. Una sesión simulada NO certifica JWT real ni autorización backend.
- A3: ejecutar en GitHub-hosted el workflow dedicado con Vitest y Chromium/Playwright real contra Angular loopback (sesión Playwright simulada). No se considera evidencia de PSP, tráfico real de pagos ni dispositivos físicos.
- A4: exigir SUCCESS de Scope Lock, V5, tipado, lint/build, Vitest y Chromium dirigidos del SHA exacto o equivalencia por blobs probada; CANCELLED/SKIPPED/FAIL/IN_PROGRESS no son PASS. Registrar resultados reales y readback PLAN/COLA/BITACORA.
- A5: revisión independiente por otra Task Javier distinta de DEV06 autora, después de LISTO A0–A4; sin autoprobación.

Rollback con git revert normal del commit del cambio en dev, sin force-push ni manipular registros históricos. No se tocan QA, main, PROD ni credenciales. CHANGELOG_AI.md intacto para evitar corrupción de su historia extensa por truncamiento; el alcance y limitaciones se registran en este documento y BITACORA.
