# SOLQARYN — Inventario de limpieza (22-10-2026)

PROJECT_SCOPE_LOCK=STRICT

Este archivo sustituye una memoria informal con un registro versionado y verificable. No habilita Tasks, no autoriza escritura comercial y no sustituye el plan de negocio.

- [x] El propietario confirmó la eliminación de las diez Tasks de ChatGPT.
- [x] El propietario confirmó la limpieza de Google Drive y papelera; conserva dos documentos del proyecto.
- [x] GitHub PR #3713 retiró los cron VAEP y el reescritor automático de admisión.
- [x] El propietario retiró del ruleset dev el check `VAEP engine TASKS_ONLY`; verificar en API antes de fusionar.
- [ ] Retirar scripts VAEP, manifiestos, epoch, AntiG y configurador Jules antiguos del árbol actual.
- [ ] Sustituir autoridad antigua por `docs/AUTOMATION_STATUS.md` y adaptar validadores canónicos.
- [ ] Desacoplar los workflows de fases anteriores, dejando sus pruebas históricas como manuales y conservando regresiones del ERP.
- [ ] Sustituir los checks antiguos de Fase 7 por checks de calidad generales; actualizar ruleset tras comprobar su funcionamiento.
- [ ] Ejecutar pruebas, comprobar PR, integrar protegido y efectuar readback.
- [ ] Verificar que no queden cron ni dispatch comerciales programados y que seguridad, QA/PROD y backups mantengan sus controles.

**No borrar:** módulos de negocio ERP; JWT/RBAC/aislamiento empresa; backups y restauración MySQL; integraciones Cloudinary, SMTP, QuestPDF; controles de seguridad; historia Git. El flujo M12 de automatización comercial del ERP no equivale al ejecutor ChatGPT.
