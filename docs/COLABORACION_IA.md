# Colaboración de desarrollo SOLQARYN

PROJECT_SCOPE_LOCK=STRICT
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
EXECUTION_PLAN_DRIVE_FILE_ID=1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA

Los chats o agentes autorizados trabajan sobre el repositorio y el alcance actual de STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES. Consultar `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, el HEAD vivo de `dev` y `docs/AUTOMATION_STATUS.md`.

Los diez ejecutores ChatGPT anteriores, sus pares Primary/Supervisor, epochs, hojas y scripts VAEP fueron retirados. **No recuperar, buscar ni crear Tasks**, ni ejecutar automatizaciones antiguas como requisito para nuevos cambios.

El trabajo manual se documenta mediante PR, pruebas causales y reversión. Revisar seguridad, tenancy, RBAC, migraciones y permisos según el impacto real. Ninguna fase anterior constituye un gate operativo por sí misma. Los jobs de CI del ERP y los respaldos continúan como controles técnicos independientes; cualquier despliegue a QA o PROD exige autorización explícita.
