# SOLQARYN V5 — C06-04.M03.04: adaptador aislado de evidencia persistida
**Scope:** solqaryn/Solqaryn@dev · **Riesgo:** R2, frontera de persistencia y tenant.

## Entrega
Se añade `C0604IsolatedPortalPersistenceAdapter`, puente de Application entre una fuente
de evidencia persistida read-only y `IC0604PortalAuthorizationEvidenceReader`.
Comprueba recurso exacto y seguro, SHA canónico, versión positiva y coincidente con
snapshot, procedencia autenticada y propiedad declarada verificadas por backend,
no revocación, timestamp UTC y concordancia de IDs tenant/cliente/cuenta.
Niega lecturas nulas, origen no confiable, desalineaciones y errores del proveedor;
propaga cancelaciones. No registra ningún provider, endpoint o DI de producción.

La fuente conectada en el futuro **debe demostrar realmente** JWT, pertenencia,
hash de sesión, roles, propiedad transaccional y permisos vigentes; los booleanos de
un contrato por sí solos no son prueba de autenticación. El gate M03.03 conserva
denegación completa, sin emitir JWT, datos privados, escritura ni orden.

## Pruebas A0–A5 y limitaciones
- A0: diff exclusivo M03.04 (Application + xUnit + este documento + CHANGELOG);
  rollback reversible por git revert normal, sin sobrescribir HEAD concurrente.
- A1: xUnit positivo solo de preflight no autorizante y negativos de ruta, SHA,
  versión, fuente nula/error, propiedad, proveniencia, revocación y cancelación.
- A2: tenant A/B, account/customer mismatches, error fail-closed, cero grants.
- A3: tests .NET 10 in-process en GitHub Actions; MySQL/EF/HTTP/Playwright
  NO_APLICA al adaptador sin infraestructura, DB, UI ni endpoints. Ningún test
  sintético acredita MySQL real o JWT firmado.
- A4: Scope Lock STRICT, V5, backend/security en SHA exacto con readback Sheets.
- A5: revisión distinta del autor entre las seis Tasks Javier, no autoaprobación.
No se cambian migraciones, datos productivos, flags, QA/main/PROD o secretos.
