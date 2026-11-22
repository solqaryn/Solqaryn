

## 2026-11-22 — C01-03.M04.04 V24: CI Angular JWT readonly A/B y editor monotienda aislado
- Se corrige el diseño de pruebas sin alterar permisos runtime: en MySQL 8.4 efímero, primero se acredita JWT firmado A/B de solo lectura y denegación de mutación, después se retira tenant B de la base exclusiva CI y se prueba un editor con grant verdadero en la empresa única.
- El escenario de doble submit preserva un HTTP 409 simulado como prueba del control cliente; no se certifica CAS backend por ese mock. Las pruebas reales, Scope Lock, V5 y A5 independiente permanecen pendientes de la nueva CI exact-SHA. Sin QA/main/PROD.
