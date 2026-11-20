# C04-04.M04.04 — navegación, mensajes y doble acción (V22)

PLAN_MAESTRO D707: verificar formularios, navegación, mensajes y prevención de doble acción. Microtarea R2 test-only de la vista privada **desconectada** de enlaces de cobro. El diseño no contiene formularios ni botones de pago. No se simula una acción cobrable inexistente y no se añade integración a PSP, webhook, JWT, RBAC backend, MySQL, EF o producción.

A0: inspección de componente Angular 22, ruta con plataformaGuard, tres pasos SIN_CONEXION, enlace «Volver a plataforma», estado informativo vacío y ausencia de controles financieros. Sin cambiar runtime ni código de autenticación.

A1–A2: tres tests Playwright Chromium positivos, negativos y de repetición. Comprueban mensajes role=status, lista de tres etapas, ausencia de formularios/inputs/botones, navegación de regreso dos veces y ninguna petición HTTP mutante. Anónimo y 403 niegan la vista y no exponen mensaje interno del proveedor. Fixture de sesión falsa de UI, jamás certificado JWT firmado ni backend/MySQL real. El doble clic en pago no existe y por tanto el test no prueba transacciones dobles reales.

A3: CI dirigida en GitHub-hosted con Chromium contra Angular loopback, Vitest4 del guard + Vitest6 de los estados, lint y build. A4: solo PASS al completarse las comprobaciones pertinentes en SHA causal, Scope Lock, V5 y readback Sheets/BITACORA; QUEUED/IN_PROGRESS/FAILED/CANCELLED/SKIPPED requeridos no PASS. A5: revisor Javier Task distinto de DEV06 autor, con revisión independiente genuina.

Limitación de fase: no demuestra protección transaccional frente al doble cobro real; proveedor de cobros no está conectado. No QA/main/PROD ni secretos; rollback con git revert normal. CHANGELOG_AI.md íntegro, evidencia en docs y BITACORA.
