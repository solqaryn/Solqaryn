# C11-03.M04.06 — densidad y carga progresiva (V22)

Accion atomica PLAN_MAESTRO D1681: verificar densidad, carga progresiva y percepcion de ligereza en la UI privada outbox desconectada. Riesgo R2 conservador por sesion privada. PLAN E es meta acumulativa M04; esta microtarea no certifica relay, outbox MySQL o todos los casos M04.

A0: incremento TEST-ONLY, tres Vitest nuevos de estados locales cargando-vacio, error-listo y revocacion mientras carga; dos Playwright en Chromium GitHub-hosted para 320/375/768/1280 con texto 200%, DOM acotado y sesion sintetica artificialmente retenida que no puede exponer panel antes del permiso. Se reutilizan pruebas existentes del guard de plataforma y estados. No cambia componente runtime, backend, HTTP, JWT, MySQL, API, persistencia, cola ni proveedor.

A1: positivo y negativos de aria-busy, estado accesible, error seguro, tres pasos, ausencia de mutaciones y datos privados, no overflow horizontal, fallo cerrado al revocar. A2: la sesion artificial en browser no es JWT firmado ni prueba RBAC/tenant backend; negatives UI aislados. A3: Playwright/Chromium real en GitHub-hosted con Angular loopback, no MySQL ni outbox real; NO_APLICA a integraciones que no toca la accion. A4: GitHub Actions exact-SHA con Vitest nuevos+anteriores, Angular lint/build, Scope Lock, checks P0 pertinentes y resultado COMPLETED SUCCESS, no SKIPPED/CANCELLED/IN_PROGRESS requeridos. A5 por Task Javier distinta del autor/reparador.

LIMITACION: Web Vitals FCP/LCP/INP/CLS en campo, dispositivos fisicos y Lighthouse son NO_MEDIDO. DOM ligero no prueba FPS ni latencia del relay. Sin cobros, datos sensibles ni autorizacion de QA/main/PROD. Rollback por git revert normal tras exclusividad.
