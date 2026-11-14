# C00-03.M04.03 — Branding de matriz de cobertura y configuración visual permitida

La ruta protegida `/plataforma/cobertura-v5` utiliza la identidad institucional SOLQARYN, componentes Angular Material existentes y los tokens CSS seguros del sistema para la vista estándar.

El usuario con sesión de plataforma verificada puede alternar un estado visual de alto contraste mediante un único botón accesible `aria-pressed`. La preferencia `signal<boolean>` es **local, efímera y exclusiva** de la tarjeta de cobertura. No modifica temas empresariales, variables globales, permisos, datos, la base de datos ni otras páginas. La paleta oscura fija tiene fondo `#0b1220` y texto `#f8fafc`, con contraste elevado y sin animación exigida. Al volver al modo estándar se restaura el tema vigente.

El flujo no usa valores de URL, headers, localStorage ni CSS arbitrario como configuración: el único cambio de estado es el clic explícito del usuario. No se anuncian resultados de cobertura inexistentes ni se habilita un dispatcher.

**Prueba:** `frontend/e2e/v5-plataforma-browser-guards.spec.ts` agrega tests Chromium de identidad y alternancia de estado permitida, y de parámetros query maliciosos ignorados, además de la regresión existente de autorización del backend sintético. Se exigen A0 diff/secretos/tokens, A1+ A2 positivos/negativos de interfaz, A3 Angular lint/build y navegador, A4 exact-SHA, Scope Lock y readback. A5 independiente por otra Task Javier.

**No aplica:** .NET, EF/MySQL, Docker, autenticación HTTP de backend real y migraciones porque el diff no cambia estas superficies. Ninguna prueba mock se presenta como evidencia de permisos reales.

**Reversión:** git revert no destructivo del commit DEV; no QA/main/PROD.
