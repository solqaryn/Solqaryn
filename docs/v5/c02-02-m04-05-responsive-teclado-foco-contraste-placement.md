# SOLQARYN V17 — C02-02.M04.05: responsive y accesibilidad privada de placement

**ID:** C02-02.M04.05 · **PLAN_MAESTRO fila:** 348 · **Autor:** PRIMARY DEV05 (6adc2e55f47c81919f6374e561687076) · **Riesgo:** R2 por superficie privada · **Rama:** solqaryn/Solqaryn@dev · **Scope Lock:** STRICT.

## A0: alcance y rollback

Cambio limitado a estilos del componente standalone de revisión de placement y pruebas de navegador Chromium dirigidas. Se incrementa a 4 px el contorno de foco de los controles de navegación y revalidación, con ajustes para contraste elevado y colores forzados. No cambia routing, interfaces, DTO, autorización backend, datos, migraciones, tenancy, credenciales, proveedores, planes, despliegue ni infraestructura. Se conservan Angular 22.2.1, Signals, guard de plataforma, separación de modalidad compartida/dedicada y capa de autorización de servidor. Reversión permitida por commit revert normal tras readback de HEAD y sin tocar otros commits.

## A1: escenarios que pueden fallar

La ampliación Playwright de `frontend/e2e/c02-02-m04-01-placement.spec.ts` requiere tres casos nuevos: (1) ancho 320/768/1280 px sin scroll horizontal significativo, etiquetas accesibles y columna única/doble correspondiente; (2) avance real con tecla Tab al enlace y al botón, contorno calculado de 4 px o más con `prefers-contrast: more`, sin `.focus()` programático; (3) sesión inicialmente válida seguida de denegación 403 en 320 px, sin modalidades ni exposición del payload. Son pruebas, no simples declaraciones documentales; su éxito queda supeditado a la CI real.

## A2 y A3: frontera de seguridad y navegador

Se preserva `plataformaGuard`, `PlataformaSesionService.obtener()` con denegación por defecto, visibilidad de modos sólo al verificar sesión y ocultación de respuesta HTTP 403/503. Las pruebas Playwright emplean sesión sintética, NO validan JWT real, MySQL 8.4, autoría por tenant en backend, API de placement productiva, migración, DSN ni servicios externos. A3 sí requiere GitHub-hosted Chromium y build Angular, lint y Vitest existentes sobre el SHA del cambio; los tests encolados o cancelados no son PASS.

## A4 y A5: verificación y continuidad

Exigir check `verify-solqaryn-project-scope`, procedencia V5, compilación Angular y workflows dirigidos de placement `v17-c02-02-m04-01-placement.yml` y `v17-c02-02-m04-03-placement.yml` con `completed/success` en SHA exacto o equivalencia de blobs totalmente demostrada. Conservar estado VALIDANDO mientras haya CI externa realmente en curso. A5 requiere revisión independiente de otra Task Javier con lectura de diff, tests, límites y requisitos; no se concede QA, main ni PROD.

**Sin cambio arquitectónico:** sólo estilos y pruebas sobre una pantalla privada informativa; no nuevo framework, red, base de datos ni controlador.
