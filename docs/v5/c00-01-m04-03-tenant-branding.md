# V5 C00-01.M04.03 — Branding seguro y configuración visual por tenant

Proyecto \`solqaryn/Solqaryn\`, rama \`dev\`, V5 FINAL. Autor: PRIMARY DEV 01 JAVIER, TASK_ID \`6adbb9fff1548191a1bd5b30d9e9984a\`.

## Resultado atómico

Aplicar temas visuales mediante los campos allowlisted de \`CAMPOS_TEMA\`, sin interpretar cadenas arbitrarias como CSS y sin permitir que una respuesta asíncrona de empresa A restaure sus colores tras pasar a B o a plataforma. Los tokens de \`styles.scss\` constituyen el fallback; no se modifica autenticación, API, MySQL, RBAC, datos productivos ni proveedores.

## Impacto

- Primario: \`frontend/src/app/services/theme-applier.service.ts\` (servicio Angular), consumidor \`app.component.ts\` y temas de empresa.
- Fuente externa: \`TemaVisualService.get()\` con datos potencialmente obsoletos; campos admitidos solo colores \`#RGB\` y \`#RRGGBB\`.
- Seguridad: no se admite \`url(...)\`, \`var(...)\` ni expresiones CSS arbitrarias provenientes del DTO. Se invalida respuesta de tenant anterior.
- UI: colores legibles mediante \`ColorContrastService\`; las propiedades inválidas se eliminan para recuperar tokens CSS base, no se heredan del contexto anterior.
- Rollback: revert aditivo del commit exacto en \`dev\` después de revisar HEAD, sin force-push, borrar datos ni afectar QA/main/PROD.

## Auditorías exigibles

- A0: inspección estática de allowlist, diff, secretos, dependencias, tipos y workflow dirigido.
- A1 positiva: tema válido aplica color y contraste; A1 negativa: entrada CSS inválida y respuesta tardía A no contaminan B.
- A2: aislamiento visual entre tenants y plataforma; comprobar que la entrada no concede permisos ni elige EmpresaId.
- A3: Angular TypeScript, lint/build y suite \`theme-applier.service.spec.ts\` mediante GitHub Actions \`priority4-quality.yml\`; E2E M10 pertinente para contraste visual en login y rutas existentes.
- A4: inspeccionar jobs y logs de SHA exacto, Scope Lock, diff, rollback y PLAN/COLA/BITACORA.
- A5: segundo TASK_ID distinto al autor, antes de QA/main/PROD.

Este documento describe criterios. Su existencia no constituye PASS, CI aprobada ni cierre de la microtarea.
