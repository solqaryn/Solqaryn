# SOLQARYN — Inventario de limpieza (22-10-2026)

PROJECT_SCOPE_LOCK=STRICT

Este archivo sustituye una memoria informal con un registro versionado y verificable. No habilita Tasks, no autoriza escritura comercial y no sustituye el plan de negocio.

- [x] El propietario confirmó la eliminación de las diez Tasks de ChatGPT.
- [x] El propietario confirmó la limpieza de Google Drive y papelera; conserva dos documentos del proyecto.
- [x] GitHub PR #3713 retiró los cron VAEP y el reescritor automático de admisión.
- [x] El propietario retiró del ruleset dev el check `VAEP engine TASKS_ONLY`; verificar en API antes de fusionar.
- [x] PR #3715 retiró scripts VAEP, manifiestos, epoch, AntiG y configurador Jules antiguos del árbol actual.
- [x] PR #3715 sustituyó la autoridad antigua por `docs/AUTOMATION_STATUS.md` y adaptó los validadores.
- [x] PR #3715 convirtió a manuales los workflows de modernización y matrices, conservando las regresiones del ERP.
- [x] Ruleset actualizado y confirmado: únicamente los tres checks estándar del ERP; se retiran las capas temporales Fase 7.
- [x] PR #3715 fusionado y con readback de archivos. La certificación post-merge de este cambio final continúa aparte.
- [ ] Certificar la limpieza final en el HEAD integrado: ausencia de despacho, checks estándar, backups y fronteras QA/PROD.

**No borrar:** módulos de negocio ERP; JWT/RBAC/aislamiento empresa; backups y restauración MySQL; integraciones Cloudinary, SMTP, QuestPDF; controles de seguridad; historia Git. El flujo M12 de automatización comercial del ERP no equivale al ejecutor ChatGPT.

## Limpieza final — 22-10-2026

- Retirar aliases temporales «Fase 7» y workflows manuales VAEP que únicamente reproducían planes archivados.
- Corregir README, pendientes y documentos corrientes que describían las diez Tasks como pausadas.
- Admitir únicamente material documental histórico para comparar motores, sin convertirlo en autoridad, plan o ejecución.
- Mantener los recibos en `vaep/evidence/` por auditoría, pero sin motor ni autoridad activa.
- Preservar seguridad, compilación, regresiones funcionales, correo, MySQL y backup programado.
- Verificar GitHub Actions sobre el HEAD integrado exacto antes del cierre.
