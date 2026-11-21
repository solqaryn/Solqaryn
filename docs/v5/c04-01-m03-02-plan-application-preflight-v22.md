# SOLQARYN V22 — C04-01.M03.02 preflight Application SaaS

**ID:** C04-01.M03.02, PLAN_MAESTRO D591 «Implementar caso de uso, servicio o aplicación con límites claros». **Autor de este incremento:** DEV05; **riesgo:** R2; **rama:** solqaryn/Solqaryn@dev, Scope Lock STRICT. PLAN E define una meta acumulativa M03; esta unidad NO certifica M03.03–06 ni pagos.

## A0 — impacto
Se crea una clase Application y dos contratos de fuentes verificadas server-side, separados de Domain C04-01.M03.01. Solo se reciben pistas del cliente, nunca grants. Tenant del snapshot proviene del actor JWT verificado en servidor; no existe registro DI, controller, endpoint, transaccion, plan comercial tarifario, proveedor de pagos, migración ni cambio de schema MySQL. Los límites de seguridad permanecen en backend; API futura deberá revalidar JWT/RBAC relacional, plan, suscripción y cuota dentro de una única transacción MySQL con control de concurrencia, idempotencia y reversión.

## A1–A2 — pruebas adversariales
Nuevo xUnit real C0401M0302PlanApplicationServiceTests: preflight positivo SIN grant, entradas inválidas, cliente que fabrica roles/reserva/cobros, token no verificado, RBAC o membresía revocados, tenant A contra B, snapshot no filtrado, plan distinto, versión obsoleta, expiración UTC, sucursal indebida, cuota insuficiente, fuentes que fallan y cancelación después de awaits no cooperativos. Fuentes de prueba son dobles de contrato, NO JWT HTTP, EF/MySQL, ni integraciones vivas. Ninguna salida autoriza lectura, escritura, cuota reservada o pago.

## A3 — delimitación causal
A3 HTTP/JWT/MySQL/Chromium **NO_APLICA AL DIFF DE M03.02 NO CABLEADO**, porque no hay adaptador runtime ni cambio de API, EF o UI; esta limitación no se puede usar para cerrar integraciones M03.03–06. No afirmar pruebas HTTP real, tenant firmado o MySQL por xUnit con dobles. La aceptación M03.02 exige que todos los tests xUnit y checks pertinentes sean COMPLETED SUCCESS, con historial V5 intacto.

## A4 y A5
GitHub-hosted .NET10 build Release y xUnit sobre SOURCE_SHA exacto, Scope Lock exact-SHA y procedencia V5; si un check requerido sigue IN_PROGRESS, FAILED, CANCELLED o SKIPPED, mantener VALIDANDO, registrar SHA/run y retomar con evidencia. A5 debe realizarla una Task Javier distinta de DEV05 autor y reparador, bajo lock y BITACORA idempotente. No extender autoridad a QA/main/PROD o R3. Rollback: git revert normal de un commit serializado y registro institucional, nunca reset ni force-push.
