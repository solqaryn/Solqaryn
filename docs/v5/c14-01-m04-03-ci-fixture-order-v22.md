# C14-01.M04.03 — Reparacion de inicializacion de fixtures en E2E (V22)

## Evidencia de defecto y nueva hipotesis
En [GitHub Actions job 135839157984](https://github.com/solqaryn/Solqaryn/actions/runs/44603008506/job/135839157984), el SHA causal 8d914060dfac37acf3f29185a6975447c45725eb ejecuto 100 Playwright: 97 PASS, 3 FAIL. El formulario reporto mat-select de Almacen origen desmontado del DOM; lista y detalle de transferencias no mostraron TR-000041 ni seccion Recepcion. La antigua configuracion llamaba a iniciarSesionReal() ANTES de registrar rutas de permisos y, para formulario, de datos de negocio. La hipotesis concreta es un guard/snapshot Angular inicializado sin la fixture que despues se esperaba en navegacion.

## Reparacion y limites A0–A4
- A0: reordenar **solo los beforeEach** de dos suites E2E para instalar los mocks de respuesta comercial antes del login real. Conserva todas las comprobaciones posteriores y rutas firmadas por backend.
- A1: repetir caso de formulario completo, lista/detalle y recepcion con assertions adversariales originales sin omitir pruebas fallidas ni aumentar timeouts artificialmente.
- A2: la autenticacion usa ASP.NET Core/MySQL 8.4 efimero y JWT firmado, sin falsificar identidad. Los permisos/movimientos de negocio si son fixtures HTTP de prueba; NO certifican RBAC real comercial, tenancy de datos en produccion, branding provider ni M04 completa.
- A3: ejecutar Chromium/Angular Playwright real en GitHub-hosted; las rutas de negocio interceptadas no demuestran integracion SQL real de transferencias. No usar servicios reales ni datos productivos.
- A4: exigir job global anteriormente FAIL, Scope Lock, V5/procedencia y controles pertinentes COMPLETED SUCCESS exact SHA despues del commit. Antes de verificar, no proclamar CI verde ni LISTO. A5 solo por Task Javier distinta de autor DEV01 y reparador DEV06.

## Impacto y reversibilidad
Dos archivos frontend/e2e y registro aditivo; ningun cambio de runtime Angular, .NET, MySQL, roles, migraciones, proveedores, secretos o despliegue. Rollback: revert normal del changeset tras comprobar HEAD, sin reescribir BITACORA o pruebas historicas. R2 por autorizacion/CI. PLAN H2038 permanece VALIDANDO con owner DEV01 historico.
