# C09-04.M04.01 — checkpoint seguro de desarrollo (DEV04, V19)

Proyecto solqaryn/Solqaryn; rama dev; Scope Lock STRICT. TASK_ID propietario: 6aeacb7820ac8191b7d768a6f33dfa09. PLAN fila 1424. Riesgo R2 (mensajería, privacidad, autorización); candado SQV17_C09_04_M04_01 ID 2000001424. Estado: EN_PROGRESO, NO LISTO y NO A5.

## Requisitos A0 verificados y alcance incremental

El plan exige una ruta, pantalla o superficie pública/privada del lote C09-04 de mensajería multicanal. La rama dev contiene Angular 22.2.1, rutas lazy y plataformaGuard; no existe carpeta CRM/mensajería en frontend/src/app/features. El backend Application ya implementa OutboxTenantDeliveryWorker y casos de autorización, pero esta tarea NO habilita SMTP/WhatsApp ni crea nuevos grants, endpoints, servicios de terceros o datos productivos.

Propuesta mínima verificable: superficie privada read-only bajo /plataforma/mensajeria-multicanal-v19, protegida por plataformaGuard y comprobación reactiva de sesión Plataforma/Administrador. Mostrar tres canales declarativos (email, WhatsApp, otros) como NO_CONECTADO, sin enviar mensajes ni consultar información de un tenant. Ninguna prueba de UI se interpretará como autorización backend.

## Archivos e impactos precisos

- frontend/src/app/app.routes.ts: insertar ruta exacta, sin wildcard ni cambio de permisos empresariales.
- frontend/src/app/features/plataforma/c09-04-m04-01-mensajeria-privada.component.ts/html/css: componente standalone OnPush, Signals, acceso condicionado y pantalla sin controles de envío.
- frontend/src/app/features/plataforma/c09-04-m04-01-mensajeria-privada.spec.ts: Angular TestBed positivo/anonimo/Empresa/no-admin/revocacion/guard.
- frontend/e2e/v19-c09-04-m04-01-mensajeria.spec.ts y .github/workflows/v19-c09-04-m04-01-mensajeria.yml: Chromium real, sesión sintética claramente distinguida de JWT, 403, cero escrituras, Tab, tamaños 320/375/768/1280.
- docs/P0-007-A-INVENTARIO-COMPONENTES-DEV.md y scripts/verify-p0-007-a-component-catalog.mjs: inventario cerrado evoluciona de 158 a 159 (151 feature + 8 shared), preservando negativos.
- scripts/quality/priority4_quality_audit.py: allowlist exacta para nueva ruta privada con plataformaGuard; no aceptar patrones genéricos.
- CHANGELOG_AI.md y documentación de evidencias: sección autocontenida; validar origen de encabezados y viñetas.

## Criterios A1–A5 y rollback

A1: pruebas realmente fallables para admin, anonimo, empresa, revocación, teclado y frontera responsive. A2: asegurar ausencia de operaciones de envío, datos tenant o permisos ampliados; guard del navegador no sustituye verificación de servidor. A3: Angular TestBed y Playwright/Chromium GitHub-hosted; MySQL/SMTP/WhatsApp/JWT/HTTP real NO_APLICA para diff solo frontend (no afirmar certificación de servicios). A4: HEAD, Scope Lock, V5, catálogo P0-007.A, contrato de rutas ERP, compilación/CI y readback exact-SHA SUCCESS. A5: otra Task Javier distinta del autor.

CI_CANCEL_GUARD: comprobar no cancelar SHA requerido de otros autores; solqaryn-core-ci y vercel-consolidated tienen cancel-in-progress=false. Publicar UN commit fast-forward sobre HEAD revalidado; rollback por git revert normal, nunca force-push/reset.

## Estado del checkpoint

V17 CLAIM legítimo: addNamedRange id=2000001424, findReplace H PENDIENTE->EN_PROGRESO occurrencesChanged=1, I=DEV04, readback y BITACORA CLAIM. La aplicación/frontend y pruebas NO ESTÁN aún publicados ni certificados: este documento conserva A0 y plan de archivos para reanudar en siguiente corrida. Mantener el candado hasta reconciliación segura de EN_PROGRESO; no reclamar otra PENDIENTE mientras este scope esté activo. NO tratar el documento como entrega VALIDANDO ni como LISTO.

Limitación detectada: composición de patch integral no completó generación del árbol candidato durante la corrida; no publicar código parcial a dev. No se afirma fallo de GitHub Actions ni ejecución de Playwright.
