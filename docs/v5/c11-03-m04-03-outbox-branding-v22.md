# C11-03.M04.03 - Branding local seguro

SOLQARYN dev; PROJECT_SCOPE_LOCK=STRICT. Microtarea D1678: aplicar branding/configuracion permitidos a vista privada outbox, sin conectar eventos. Riesgo R2 por ruta privada.

Implementacion: input Signal de preset base/contraste con allowlist literal exacta y fallback base. No HTML arbitrario, CSS de URL, imagenes externas, logos proveedor o mutaciones; protegido por plataformaGuard existente. Pruebas adversariales Angular Vitest de positivo/negativo, entrada hostil y revocacion; navegador real Chromium 320/375/768/1280 y 403 con sesion UI sintetica, npm lint/build, Scope Lock, inventario P0.

LIMITACION: la sesion UI sintetica NO demuestra JWT firmado, HTTP MySQL real, relay, PSP, seguridad global o fase M04 completa. A0-A4 solo despues de checks GitHub-hosted SUCCESS exact SHA; A5 diferente Task Javier tras revision. Rollback git revert normal en dev con CAS.