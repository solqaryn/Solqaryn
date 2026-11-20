## 2026-11-20 — V22 DEV05 C02-03.M03.03 lector plataforma JWT/RBAC real, DEV y OFF
- Adaptador API de preflight de solo lectura: JWT Bearer firmado, rol global actual, permiso Plataforma Configuracion.CambiarEstado desde EF y Empresa fenced; no privilegios por claims de UI ni X-Empresa-Id.
- DI únicamente Development y opt-in triple (feature + comando Suspend + revisión de config), sin endpoint nuevo ni mutaciones. Casos xUnit de flag OFF, comandos irreversibles, ruta tenant cruzada, principal sintético y cancelación.
- R2: CI GitHub-hosted exact-SHA pendiente; MySQL efímero con consulta EF real, CAS y revisión de policy durable aún requieren prueba. Mantener VALIDANDO y A5 por otro Javier; no QA/main/PROD.

