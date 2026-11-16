# SOLQARYN V17 - C14-02.M04.02: estados seguros de revisión de amenazas

Autor DEV03, Task 6adc2e4bd8a48191ac7d8641ed95a1a3. PLAN_MAESTRO fila 2073, candado SQV17_C14_02_M04_02, ID 2000002073. Riesgo R2: sesión administradora de plataforma y mensajes de seguridad.

## A0
Extiende la pantalla privada de C14-02.M04.01 sin cambiar ruta, guard, grants, backend, JWT, RBAC, tenant o persistencia. Estado vacío es guía sin feed de amenazas conectado. El estado exito solo certifica sesión revalidada con GET /plataforma/sesion; no representa pentest real ni concesión de permisos.

## A1/A2
Nueve pruebas Vitest sobre ruta, anonimato, empresa, grant, revocación, estados vacío/cargando/error/éxito/denegación y single-flight; cinco Chromium sintéticos con 403/503 y sin exposición de diagnóstico. Prohibidas acciones de mitigación o pentest en la vista. Exigir ejecución real del workflow antes de afirmar PASS.

## A3
CI Angular lint, build y Playwright Chromium en GitHub-hosted con datos de API sintéticos. No se modifican MySQL/EF, proveedores, backend o HTTP JWT real; no inferir integración efectiva de tenant A/B a partir de mocks.

## A4/A5
Verificar SHA publicado, Scope Lock, V5 y workflow dirigido completed/success, readback PLAN/COLA/BITACORA, revisión posterior por distinto Task ID Javier. Ninguna autorización QA/main/PROD. Rollback por git revert normal, sin force-push.

## Reparación P2 V15/V17 de excepciones síncronas (2026-11-15)

Una revisión A5 independiente detectó que obtener() podía lanzar antes de devolver un Observable, dejando verificacion=cargando indefinida. El cambio mínimo encapsula la llamada en RxJS defer, encamina el rechazo al mismo manejador seguro, oculta detalles internos y evita fuga del mensaje de error. Se agregan dos Vitest negativos (error genérico y 403 síncronos), conservando los 12 anteriores y los seis Playwright sintéticos. Evidencia CI y nueva A5 independiente obligatorias sobre SHA publicado; no se infiere integración HTTP/JWT, MySQL ni datos productivos. El A5_FAIL histórico no se borra; rollback: git revert normal sobre dev.

Nota de regresión (iteración CI): el caso de excepción 403 síncrona verifica `verificacion=sin_permiso`, `estadoVista=sin_permiso` y que no se filtre el diagnóstico. La prueba 403 asíncrona existente comprueba que la lista desaparece del DOM tras la revocación real de la sesión. No se modifica ni se desactiva el test de denegación visual; el mock síncrono no revoca `sesion`, por lo que no debe confundirse con un 403 HTTP de backend. Mantener ambas coberturas y no certificar hasta GitHub Actions SUCCESS.
