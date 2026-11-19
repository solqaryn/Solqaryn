# C15-01.M04.06 - Densidad y carga progresiva V20 (DEV04)

Alcance R2: superficie de cobertura solo lectura /plataforma/trazabilidad-cobertura-v5. Playwright Chromium comprueba cuatro dimensiones, reflow a 320/375/768/1280, cero desbordamiento horizontal, ausencia de medios pesados y HTTP mutador; antes de autorizar la sesion los datos privados permanecen ocultos. Un rechazo HTTP simulado 403/503 no debe mostrar dimensiones ni el mensaje privado de error.

A0: cambia exclusivamente prueba E2E, workflow, documento y CHANGELOG; no cambia Angular componente, API, JWT, MySQL ni autorizaciones. A1: positivos, frontera responsive, loading gate, negativos 403/503. A2: sesion sintetica, no autentica JWT firmado/RBAC/tenant A/B de backend. A3: CI GitHub-hosted Node 24, build Angular y Chromium real; MySQL/EF/HTTP proveedor real no aplica al diff. A4: Scope Lock V5, CI causal por SHA, CAS PLAN/BITACORA y readback. A5: Task Javier distinta de DEV04 con revision real.

No se certifican Core Web Vitals, latencia, rendimiento global, cobertura de las 2340 filas, ni produccion. Rollback por git revert no destructivo, sin QA/main/PROD.
