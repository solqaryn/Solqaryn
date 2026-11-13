# C00-02.M04.06 — densidad y carga progresiva de la superficie V5

**Alcance ejecutable:** dos casos Playwright Chromium ampliados para `/plataforma/flujo-v5`: (1) superficie ligera con una tarjeta de contrato y cuatro estados, sin carga de directorio ni endpoints institucionales; (2) respuesta de sesión retenida permite comprobar estado progresivo, anuncio accesible, botón deshabilitado y exactamente una petición GET hasta completar. La pantalla existente es de consulta, no un dispatcher.

**Prueba negativa y delimitación:** las respuestas 200 se simulan exclusivamente en navegador para prueba de UI; los casos preexistentes deniegan sin token y ante 403. Ninguna prueba implica grant JWT real, aislamiento productivo MySQL o ejecución de tareas. Nunca se presenta un resultado de sesión simulada como autoridad real.

**Matriz causal:** Node/Angular=SI, E2E/Chromium=SI, A2 seguridad UI negativa=SI, Scope Lock=SI, prueba de procedencia CHANGELOG=SI, dotnet/MySQL/Docker=NO_APLICA porque no se altera runtime API/DB. Requiere A0–A4 sobre SHA publicado y A5 por otra Task Javier. Rollback `git revert` normal, sin modificar QA/main/PROD.
