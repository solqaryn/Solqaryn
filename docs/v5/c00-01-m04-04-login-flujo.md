# V5 C00-01.M04.04 — Verificación de formulario, navegación y doble acción

Repositorio solqaryn/Solqaryn @ dev. Responsable PRIMARY DEV 01 JAVIER, TASK_ID 6adbb9fff1548191a1bd5b30d9e9984a.

## Resultado atómico
Prueba Playwright de /login y /dashboard con API .NET y MySQL 8.4 descartables del runner M10. No se modifica código de autenticación ni se envían pruebas a producción.

## Alcance y riesgos
- Entrada pública /login y guard privado /dashboard; no acceso a otra empresa desde parámetros, sin nuevos privilegios.
- Positivo: datos válidos, un POST real a /auth/login, selección de tenant server-side cuando corresponde y navegación posterior.
- Negativos: navegación anónima redirigida, formulario incompleto no envía credenciales, credenciales inválidas generan alerta sin stack/secretos, segunda acción durante espera no emite otro POST.
- La latencia artificial del interceptor E2E no sustituye el backend: la petición retenida se reanuda con route.continue hacia la API real.
- Consumidores: Angular login.component y authGuard. Datos: fixtures descartables. Sin migraciones, secrets nuevos, provider real o cambios de API.

## Controles
- A0: diff mínimo, nuevas pruebas y filtro de workflow, secretos, formato y tipos.
- A1: Playwright con caso positivo real y negativos observables.
- A2: guard anónimo, no filtración de error, deduplicación de POST y elección tenant verificada server-side.
- A3: npm lint/build, Playwright M10 con API .NET y MySQL descartable, sin asumir tests de producción.
- A4: run, job, logs/artifact, SHA exacto, rollback git revert, PLAN/COLA/BITACORA readback.
- A5: otro TASK_ID independiente antes de promoción. No se autoriza QA/main/PROD.

No afirmar que el test es PASS hasta que GitHub Actions haya ejecutado el archivo nuevo en el SHA publicado.
