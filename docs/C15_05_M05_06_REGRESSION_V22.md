# C15-05.M05.06 — V22 regresion y accesibilidad de auditoria operativa

## Alcance atomico A0
PLAN D exige regresion, rendimiento y accesibilidad proporcionales. Esta entrega es exclusivamente prueba de pantalla Angular ya existente /plataforma/auditoria-operativa-v5 (R2 por privilegio de plataforma). No modifica UI, rutas, backend, JWT, DTO, DB, permisos ni estado institucional. No confundir PLAN E de fase M05 con el cumplimiento de una prueba individual. El objetivo de validar toda la fase y liberar DAG sigue siendo acumulativo.

## Cuatro pruebas adversariales de Chromium
1. Anchuras 320/768/1280, sin overflow horizontal, cero mutaciones HTTP, no A5_PASS ficticio y cuatro controles de solo lectura.
2. Teclado Tab real y Enter para revalidar sesión, sin .focus() programatico ni aprobacion.
3. 403 durante revalidacion: oculta controles privados, no filtra mensaje de proveedor.
4. prefers-reduced-motion y DOMContentLoaded local bajo 30000ms como humo amplio, nunca Web Vitals LCP/CLS/INP ni p95/p99 de campo.

## Gates y limitaciones
A1 contiene positivos, negativos, limites y error. A2 solo acredita simulacion de sesion para la capa UI: NO JWT firmado, RBAC backend, tenant A/B, revocacion real ni aislamiento MySQL. A3 exige GitHub-hosted con Angular real + Chromium real, dirigido por workflow exact-SHA; HTTP de sesion sigue simulado y no certifica integraciones externas. A4 Scope Lock STRICT, V5, workflow dirigido, SHA causal y readback PLAN/COLA/BITACORA, todos SUCCESS para pasos aplicables. No confundir SKIPPED/IN_PROGRESS con PASS. A5 independiente por otra Task Javier, distinta de DEV01. No QA/main/PROD. Rollback con commit revert aditivo normal sin fuerza.
