# SOLQARYN V20 — C15-03.M04.06 densidad y carga progresiva de auditoría tenancy

Rama dev, PROJECT_SCOPE_LOCK=STRICT. PLAN_MAESTRO fila 2257, V17 namedRange SQV17_C15_03_M04_06 (2000002257), owner DEV03 (6adc2e4bd8a48191ac7d8641ed95a1a3). Riesgo R2 porque prueba una superficie privada de seguridad; **solo modifica tests, workflow, documentación y changelog**, no código ejecutable de aplicación, API, base de datos, JWT/RBAC, empresa ni proveedores.

## A0 — alcance y criterios falsables

- En la ruta Angular existente /plataforma/auditoria-tenancy-v5, cuatro frentes aparecen con columnas 1/2/2 a 320/768/1280 px y sin overflow horizontal, tolerancia <=2 px.
- Un test adversarial inyecta una columna de 650px en viewport de 320px y debe detectar desborde; no modifica los estilos de producción.
- La revalidación simulada con respuesta retrasada 900ms oculta la información protegida y deshabilita el botón para prevenir doble acción; la guía reaparece únicamente al éxito.
- La revalidación simulada HTTP503 oculta contenido, no expone un marcador de error interno y no envía POST/PUT/PATCH/DELETE en la interacción.
- Se mide DOMContentLoaded local en 320 y 1280 px con umbral de humo 15s y se conserva la indicación NO_MEDIDO. No es prueba de SLA/Web Vitals de producción.

## A1–A3 — alcance causal de las pruebas

Cinco escenarios GitHub-hosted Playwright/Chromium auténticos (positivo, negativo, frontera, error, carga). Seguridad A2: UI fail-closed con sesión HTTP simulada y detección de intentos mutantes globales; no equivale a JWT firmado, RBAC backend, aislamiento tenant A/B ni pentest. Integración A3: navegador Angular real en runner GitHub; MySQL/EF, API real y proveedor externo NO_APLICA para este diff test-only. La auditoría completa de seguridad queda fuera de esta prueba específica.

## A4, A5 y rollback

A4 requiere CI dirigida exact-SHA con cinco Chromium SUCCESS, lint/build, Scope Lock, V5 y checks obligatorios relevantes completed/success, evidencia de RUN/JOB y readback PLAN/COLA/BITACORA. No transformar SKIPPED/CANCELLED/IN_PROGRESS/FAILED en PASS. Con CI pendiente conservar H=VALIDANDO, owner original y liberar V17 tras readback; nueva A5 solo por una Task Javier distinta de DEV03. Rollback mediante git revert normal sobre dev, nunca force-push. QA, main, PROD, secretos y datos productivos prohibidos.
