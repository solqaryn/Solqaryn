# C02-04.M05.05 — comprobaciones negativas de acceso global (V18)

ID: C02-04.M05.05
Risk: R2 (autorización, frontera ERP/plataforma, revocación)
OWNER: DEV01; A5 obligatorio por otra Task Javier distinta.

## Alcance real
- Añade pruebas Playwright/Chromium para acceso anónimo, token caducado, administrador de empresa sin autoridad global, respuesta 200 malformada o sin autorización, errores 401/403/503 y revocación durante la sesión.
- Aserciones negativas de ausencia de tarjetas privadas, ausencia de mensajes internos y ausencia de mutaciones HTTP en modo lectura.
- No modifica lógica de seguridad del producto. Solo añade pruebas y workflow dirigido, no abre rutas, grants ni endpoints.

## Evidencia y límites A0–A5
- A0: fuente de requisitos PLAN_MAESTRO fila 426; ruta /plataforma/consola-global-v5, servicio de sesión y guard. Código de UI es lector, el backend sigue siendo autoridad.
- A1/A2: las aserciones Chromium están diseñadas para detectar regresiones; **PASS pendiente de CI GitHub Actions exact-SHA** hasta comprobar logs.
- A3: el backend /plataforma/sesion se sustituye **por simulación de respuestas HTTP** en el navegador; esto NO constituye integración HTTP autenticada real, pruebas JWT/MFA, ni ensayo IDOR/BOLA servidor ni tenant A/B contra MySQL. Completar una prueba server-side aislada antes de certificación final R2 y A5; NO_MEDIDO mientras no exista evidencia.
- A4: comprobar Scope Lock, procedencia V5, frontend, CI dirigida y readback institucional en SHA publicado. Resultado pendiente hasta CI completa.
- A5: pendiente revisión independiente de otra de las cinco Tasks Javier.
- No afecta esquema MySQL, datos de clientes, secretos, QA, main ni PROD. Rollback: git revert del changeset sin force-push.

## Handoff
Mantener VALIDANDO (o EN_PROGRESO si falta entrega segura) hasta contar con CI exact-SHA y pruebas de integración reales pertinentes. No certificar LISTO ni liberar sucesora técnica solamente por Chromium simulado.
