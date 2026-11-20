# C04-04.M04.02 — estados de interfaz desconectada (V22)

SOLQARYN dev, PROJECT_SCOPE_LOCK STRICT. PLAN D705: implementar estados visuales de carga, vacío, error, éxito de preparación de interfaz y acceso denegado para la vista privada de enlaces de cobro. El contrato es **solo UI**, no red ni operación bancaria. Angular standalone con Signals (input); sin nuevo endpoint, DTO, tabla, migración, trabajador, webhooks, secretos, datos productivos ni proveedor de pago. R2 conservador por dominio financiero. 

A0: se extiende el componente privado existente sin abrir rutas públicas ni cambiar guards, contratos o RBAC backend. Estados explícitos:
- `cargando`: role=status, aria-live, aria-busy, nada de proveedores
- `vacio`: SIN_CONEXION por defecto; sin transacciones
- `error`: role=alert, mensaje fijo, sin reflejar errores con PII
- `listo`: éxito del renderizado informativo únicamente; no confirma un cobro
- sin sesión o sin administración: se oculta totalmente la información privada, sin importar el estado visual
- valor desconocido: fallback seguro, sin HTML dinámico

A1: seis pruebas TestBed/Vitest positivas, negativas y adversariales, más cuatro pruebas originales de guard y pantalla; A2: no se elude aislamiento de backend ni se crea permiso nuevo; JWT, tenant A/B, MySQL 8.4 y proveedor de cobro no aplican a este cambio porque no agrega runtime de red ni persistencia. A3: GitHub-hosted Angular build/lint, Vitest 10 pruebas y Playwright Chromium actual de la ruta privada. El workflow primero valida Scope Lock en la raíz antes de npm ci (para no inspeccionar node_modules de terceros) y no desactiva verificaciones. A4: exigir check exact-SHA SUCCESS en GitHub; ningún QUEUED/IN_PROGRESS/SKIPPED/FAIL se considera aprobado; A5 independiente Javier distinto del DEV06 autor. 

Rollback: revert fast-forward del changeset con conservación del inventario y plan. Limitación: los estados `listo` y `error` representan la carga de la referencia local, nunca una transacción, aprobación de PSP o servicio de pagos. No QA/main/PROD.

CHANGELOG_AI.md contiene historia extensa que no se reemplaza parcialmente por riesgo de truncamiento. Se documenta este alcance aquí y mediante BITACORA, preservando el archivo previo íntegro.

## Corrección causal P1 V22: ERP ruta privada clasificada incorrectamente

El workflow ERP GitHub-hosted exact SHA `072133d7` (`job/136435140627`) falló en `FRONTEND_ROUTE_SECURITY_CONTRACT` porque las rutas protegidas explícitas `plataforma/eventos-outbox-v22` y `plataforma/enlaces-cobro-simulacion-v22` faltaban del conjunto cerrado del auditor, pese a tener ambas `canActivate: [plataformaGuard]`, sesión backend comprobada y rechazo de usuario anónimo en Vitest/Chromium. Solo se agregan las **dos rutas exactas** a la lista del auditor; se preserva la exigencia de `plataformaGuard`, `auth.isAuthenticated`, `plataforma.obtener`, backend, sesión administradora y `catchError`. No se admiten prefijos, comodines, ruta ERP sin permiso, ni bypass del validador. Ambos componentes informan `SIN_FUENTE_CONECTADA`/`SIN_CONEXION` sin pagos ni eventos vivos. La inclusión no equivale a certificación JWT HTTP, MySQL o proveedor; se requiere ERP audit + frontend tests + Scope Lock/V5 en CI de SHA posterior. Mantener P1 histórico y no proclamar LISTO hasta tests causales reales y readback.
