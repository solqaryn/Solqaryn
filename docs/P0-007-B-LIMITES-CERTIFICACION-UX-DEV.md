# P0-007.B — Certificación negativa del inventario de componentes (DEV)

PROJECT_SCOPE_LOCK=STRICT
MICROTAREA=P0-007.B
ESTADO_UX=NO_CERTIFICADO
E2E_NO_EJECUTADO

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, Plan Comercial Inicial V3, P0-007, L15/UI. **Ejecutor:** SECONDARY_REVIEW_01, ALEX_CHATGPT, TASK_ID 6adbbc9ad9e88191bea8eb69104afb25, GitHub morales35alex. **Rama base:** dev@05cfd207279fac6feca768f6e9cf743da513d812.

## Alcance y evidencia reutilizada

La microtarea P0-007.A cerró con PR #3753, autor morales35alex, revisión independiente de jmejia31 y merge protegido 72327d132ee69955c5bb13a03423b229a08bd432. Su validación nativa comprueba el manifiesto exhaustivo de 137 archivos .component.ts en features/shared (129/8), detecta altas y bajas sin documentar y excluye archivos ajenos a ese alcance. Esta es evidencia de CÓDIGO inventariado, no de pantallas visibles, renderizadas o utilizables.

La certificación negativa presente exige ejecutar, sobre un checkout real del SHA del PR, scripts/verify-p0-007-b-static-boundary.mjs --self-test. El script encadena el verificador de P0-007.A, comprueba un archivo real del manifiesto y somete seis supuestos sintéticos a un criterio que NUNCA concede UX_CERTIFIED por mera presencia del código.

## Caso negativo obligatorio

| Evidencia | Afirmación admisible | Afirmación NO demostrada |
| --- | --- | --- |
| Archivo .component.ts en la fuente | Archivo presente e inventariado | Vista renderizada y funcional |
| Ruta declarada | Mapeo estático | Navegación correcta y autorización efectiva |
| Frontend CI verde | Compilación satisfactoria | Uso correcto en escritorio, tablet y 320px |
| Comparador de 137 rutas PASS | Inventario sin deriva | E2E, accesibilidad o acciones reales |
| Declaración de pruebas o capturas sin proveedor | Pendiente de revisión nativa | Certificación UX, RBAC backend o aislamiento multiempresa |

Negativos sintéticos: (1) archivo existe sin pruebas, (2) supuesta compilación/ruta pero falta E2E, (3) falta aislamiento tenant, (4) falta evidencia a 320px, (5) fuente desconocida aunque existan afirmaciones de pruebas, (6) afirmación sintética completa que conduce solamente a INDEPENDENT_REVIEW_REQUIRED. Ninguno concede certificación UX; las referencias ficticias de fixtures no constituyen evidencia.

## Estado y límites de certificación

**Resultado esperado:** ESTADO_UX=NO_CERTIFICADO. Una ejecución satisfactoria del nuevo script certifica exclusivamente el caso negativo y la estabilidad de catálogo sobre el SHA probado, NO certifica usabilidad. No se ejecutaron aquí navegadores, Playwright, capturas, tests de accesibilidad, estados de carga/error, compras, JWT real, backend RBAC, aislamiento tenant A/B, API ni despliegues. Cualquier afirmación positiva de UX exige evidencia real del proveedor y revisión separada; la UI nunca reemplaza autorización backend o constraints.

No se modifican fuentes Angular, API, esquema, seguridad, infraestructura, QA, main, PROD, credenciales ni datos. El checkout de GitHub Actions usa contents:read. Se espera CI exact-SHA 3/3 obligatorio, test Node nativo, revisión GitHub independiente y merge protegido dev, seguido de PLAN_MAESTRO/COLA calculada/BITACORA con readback antes de marcar LISTO. El trabajo permanece VALIDANDO hasta estos controles.

**Rollback:** revert protegido de estos cambios documentales y del workflow aislado; no force-push ni sustitución de evidencias.
