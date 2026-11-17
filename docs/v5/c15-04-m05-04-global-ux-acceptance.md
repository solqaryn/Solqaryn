# C15-04.M05.04 - Auditoria UX global R2, incremento 1 (NO COMPLETO)

## A0: alcance y cambios
Se implementa un incremento REAL de aceptacion Playwright/Chromium en localhost Angular: login publico, panel privado informativo de cinco superficies (landing/portal/ERP/builder/global) y guard negativo ERP. Cero cambios runtime, JWT, RBAC, EF/MySQL, secretos o produccion. Prueba en cuatro anchos, labels, headings, Tab real, foco visible, contraste calculado de un titulo, 403 fail-closed y cero HTTP mutaciones. La prueba ERP corre en un contexto browser aislado. La sesion esta SIMULADA con intercept HTTP, NO demuestra servidor.

## Calidad, limites y continuidad
A1-A3: cinco pruebas nuevas, pendientes de CI causal exact-SHA. A2: 403/guard simulado, no autorizacion backend real. A3: Chromium localhost real, NO DB ni proveedor externo. A4: no declarar PASS antes de Scope Lock, V5, Angular y pruebas dirigidas completed/success y readback institucional.
Pendiente obligatorio para LISTO: inventario COMPLETO de rutas y pantallas, incluso lazy y detalle/edicion, cobertura de landing, portal, ERP, builder y consola global con roles/tenant A/B autorizados, teclado, foco, contraste, responsive, formularios, WCAG aplicable, rendimiento medido o NO_MEDIDO, P0/P1 corregidos. Los cinco dominios enumerados en UI NO equivalen a cobertura de todas sus pantallas. Reportar rutas inaccesibles como SIN_VERIFICAR; sin JWT firmado ni MySQL reales.
EN_PROGRESO reanudable por DEV02; A5 requiere otra Task Javier distinta, no autoprobacion. Git revert normal para rollback. Solo dev, sin QA/main/PROD.
