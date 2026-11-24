## 2026-11-23 — DEV01 V31 C07-01.M06.06: duplicación fiscal con auditoría atómica y rollback real
- Runtime R2: `ImpuestoService.DuplicarAsync` conserva el alta de impuesto y su auditoría estricta en una transacción Oracle EF/MySQL; la falla del sink impide dejar una copia fiscal huérfana sin recibo.
- Integración dirigida: la suite MySQL 8.4 existente prueba excepción de auditoría y readback físico de ausencia de copia por código único con contexto nuevo; las verificaciones exact-SHA GitHub-hosted quedan pendientes hasta ejecución, sin declarar PASS, VALIDANDO, LISTO o A5. Sin cambios QA/main/PROD.

