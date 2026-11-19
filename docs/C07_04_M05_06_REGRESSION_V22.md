# C07-04.M05.06 — Regresion proporcional V22 pedidos de venta

## A0 y alcance
PLAN D pide ejecutar regresion, rendimiento y accesibilidad proporcionales. Esta microtarea R2 agrega dos oraculos Playwright nuevos y un workflow exact-SHA que vuelve a ejecutar tres suites del componente existente: M04.05 (WCAG teclado/contraste), M04.06 (densidad, carga, 503, FCP) y M05.04 (paginacion, detalle, autorizacion negativa), mas los casos M05.06 (responsive/reduced motion/Tab/DCL y anonimo). Son pruebas de Angular real en Chromium real con tenant/permisos/HTTP simulados y observaciones acotadas. No se modifica codigo runtime, controller, DTO, DB, migracion, pagos ni fiscalidad.

## Gates causales
A1: positivos/negativos y frontera de 320/768/1280, 503 y anonymous, Tab real sin focus programatico, contencion de overflow y excepciones privadas no reveladas. A2: prueba de ruta y permisos solo frontend con HTTP sintetico; NO acredita firma JWT, autorizacion backend, tenant A/B ni revocacion real. A3: Chromium/Angular en Github-hosted con npm lint/build y pruebas reales de UI; no usa MySQL/EF/PSP, NO_APLICA al diff test-only. A4: Scope Lock STRICT, V5 procedencia y GHA suite exact-SHA completados con SUCCESS, bitacora y readback PLAN/COLA antes de LISTO. A5: revisor Task Javier independiente de DEV01, con revision de logs y test nuevos.

Las referencias de PLAN E a pedidos, facturas, caja, pagos, anulaciones y exportaciones son metas acumulativas M05, NO pruebas declaradas por este changeset. DCL/FCP locales no constituyen Web Vitals de campo, p95/p99, laboratorio de dispositivos, idempotencia persistente ni reconciliacion fiscal. CI pendiente implica VALIDANDO hasta PASS real. No QA/main/PROD. Rollback mediante commit git revert aditivo, sin force-push.
