# SOLQARYN V19 - C02-02.M04.06: densidad, carga progresiva y percepcion UI

Task autora: PRIMARY DEV 01 Javier, 6adbb9fff1548191a1bd5b30d9e9984a. Exclusividad V17 por PLAN_MAESTRO fila 349, candado 2000000349. Riesgo R2 por superficie privada de tenancy. Solo dev y Scope Lock STRICT.

## A0: alcance, dependencias, consumidores y rollback

La vista Angular standalone existente de placement ofrece densidad permitida (normal/compacta), revalidacion de sesion, aria-live y aria-busy. Este incremento incorpora pruebas adversariales E2E de esa experiencia en Chromium GitHub-hosted, sin introducir servicios, rutas, DTO, API, cambios de auth, operaciones DB, datos comerciales ni permisos. Consumidor: ruta privada /plataforma/placement-v5 con guard existente. Sin credenciales ni DSN. Rollback: git revert normal del commit de pruebas/documentacion.

## A1: positivos, negativos y fronteras

- P01: 320/768/1280 px muestran titulo, modalidades, densidad normal, control habilitado y overflow horizontal <= 2 px.
- P02: revalidacion GET demorada expone aria-busy=true, anuncio de carga, ausencia de modalidades y boton deshabilitado; una consulta mientras pendiente. Confirmada sesion retorna contenido y aria-busy=false.
- N01: 503 durante revalidacion bloquea modalidades, muestra aviso generico y no refleja cadenas de DSN ni formularios de mutacion.
- Conserva suites del componente de 15 casos y presentacion de 5 casos. Vitest y Chromium deben ejecutarse en CI y fallar ante regresiones.

## A2/A3: seguridad e integracion proporcional

Pruebas Playwright contra frontend Angular localhost real con sesion y HTTP de autorizacion SINTETICOS. No certifican JWT firmado, backend HTTP real, roles reales, aislamiento empresa A/B en MySQL, integracion EF o proveedor. MySQL 8.4/Aiven, router, datos productivos, migraciones: NO_APLICA al diff. Rendimiento servidor, LCP/RUM y experiencia de usuarios reales: NO_MEDIDO (no inventar umbrales).

## A4: publicacion y control

Commit fast-forward/CAS directo a dev, Scope Lock y V5 en SHA exacto, 15+5 Vitest, tres escenarios Playwright Chromium, lint/build. Hasta completed/success los resultados permanecen pendientes; no se declara LISTO ficticio.

## A5: independencia

Revisor debe ser otra de cinco Tasks Javier, distinto del autor. No concede autorización QA, main, PROD, datos reales, JWT/RBAC/tenant o migración. Sin despliegue, secretos o nuevas Tasks.
