# SOLQARYN V18 — C08-04.M04.03 branding local permitido de módulos sectoriales

**Autor:** PRIMARY DEV 04 JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Scope:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. **PLAN fila:** 1282. **Candado V17:** SQV17_C08_04_M04_03 / 2000001282. **Riesgo R1:** presentación Angular, sin modificación de autoridad ni datos.

## A0 — Contrato real, consumidores y rollback

Reutiliza la ruta privada con authGuard y el componente de C08-04.M04.01-.02. Introduce exactamente dos presets tipados de apariencia local: institucional (por defecto) y contraste alto. El componente usa Signal y clase CSS estática, sin cargar CSS, URLs, recursos remotos, secretos, variables de tenant ni código de configuración no confiable. `normalizarMarcaVisualSectorial(unknown)` admite solo el literal primitivo `contraste`, cayendo de forma segura a institucional para cualquier otra entrada sin evaluar getters, conversión a texto ni HTML arbitrario.

La selección es exclusivamente presentación ilustrativa y no está conectada a capacidades, flags reales, backend ni persistencia. Ninguna variante puede conceder permisos, modificar JWT/RBAC, ocultar authGuard ni activar módulos. No se agregan controles operativos ni llamadas HTTP. La documentación M03 es contrato previo, no autoridad efectiva. Rollback por `git revert` ordinario; no QA/main/PROD.

## A1/A2/A3 — Pruebas pertinentes

Vitest dirigido exige preset por defecto, variante segura, casos negativos de entradas arbitrarias y getters hostiles, restauración a default y que error/sin_permiso continúen ocultando ámbitos bajo contraste. Playwright GitHub-hosted repite las comprobaciones de la ruta original (anónimo, tenant sintético 403, responsive 320/1280 y no llamadas de módulos) porque el CSS es visible. Angular typecheck/lint/build pertinentes. En ausencia de proveedor conectado, MySQL/EF/API reales no aplican; JWT/tenancy no son certificados aquí por browser sintético.

## A4/A5 — Comprobaciones

Exigir GitHub CI real completed/success en SHA causal para Vitest propio, Playwright dirigido, Scope Lock, procedencia V5, frontend y seguridad pertinente. CANCELLED/SKIPPED/QUEUED/IN_PROGRESS no son PASS. Solo LISTO con readback de PLAN/COLA/DAG/BITACORA y A0-A4 reales. A5 debe realizarlo otra Task Javier, nunca el autor. Sin producción ni nuevas herramientas.
