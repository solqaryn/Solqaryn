# C04-04.M05.04 — aceptación positiva de reserva provider-neutral (V22)

Acción exacta de PLAN_MAESTRO D713: **ejecutar caso positivo de aceptación del alcance**. Riesgo R2 financiero/concurrencia; incremento **test-only** sobre un contrato de reserva previo y aislado, sin modificar runtime comercial. La meta acumulativa de PLAN E ("Completa M05") no se atribuye a esta prueba individual.

## Evidencias A0–A4

- **A0:** prueba aislada que utiliza `C0404PaymentReservationStore` y su `C0404PaymentReservationDbContext`; no cambia `AppDbContext`, controllers, autorización real, DI comercial, proveedores de pago, cobros ni reembolsos.
- **A1:** aceptación `Reserved` para tenant A, replay idempotente con el mismo ID, conflicto ante monto cambiado, separación de tenant B, persistencia al reabrir un DbContext, rollback de una reserva nueva, conteo exacto sin cargos.
- **A2:** los campos `AuthorizesProviderCall`, `AuthorizesChargeOrRefund` y `CertifiesJwtOrCommercialIntegration` continúan `false`; el aislamiento SQL por ID empresa no equivale a JWT/RBAC firmados ni a titularidad comercial real.
- **A3:** ejecución genuina GitHub-hosted .NET 10/Oracle EF contra MySQL 8.4 efímero; TRX dirigido **1/1, 0 FAIL, 0 SKIP** es requisito. No usar InMemory como certificación SQL. Esquema efímero eliminado en `finally`.
- **A4:** exigir `verify-solqaryn-project-scope`, comprobación de Skill, procedencia V5, backend/security pertinentes y job dirigido **SUCCESS** sobre SHA exacto antes de `LISTO`. Hasta entonces `VALIDANDO`.

## Límites y rollback

No existe pago por enlace, PSP real, webhook, ledger, conciliación o reembolso activado por esta prueba; esta es **aceptación positiva de la reserva técnica aislada**, no certificación de la meta de fase completa. Para una prueba integral de aceptación comercial hay que implementar backend autenticado, titularidad, tenancy y proveedor autorizado por un scope posterior explícito. No se crean secretos reales ni se publica fuera de `dev`. A5 se realiza por una Task Javier distinta. Rollback: `git revert` de este changeset, sin reescribir historia compartida.
