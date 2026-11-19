# C15-01.M06.01 — auditoría causal: pruebas reales frente a mocks insuficientes (V22)

**Única unidad:** PLAN_MAESTRO D2192, «Revisar que las pruebas correspondan al alcance real y no a mocks insuficientes». Autor: DEV01 Task 6adbb9fff1548191a1bd5b30d9e9984a; SOLQARYN dev, PROJECT_SCOPE_LOCK=STRICT. **R0 documental**: ningún runtime, dependencia, API, servicio, base de datos, UI, CI o autorización se modifica. PLAN E2192 «Completa M06» es objetivo de fase acumulativo, no conclusión de este reporte.

## A0/A1 — revisión de seis SHA causales del lote M05

Se consultaron los checks de GitHub del SHA fuente EXACTO de cada unidad, sus nombres, status y conclusion, además de los requisitos D de PLAN y la evidencia K/L. Una prueba de regla sobre datos fixture es auténtica como prueba de regla, pero no significa que la integración empresaria viva haya sido ejecutada.

| ID y fuente inmutable | Evidencia verificada en GitHub Actions | Límite que NO se promueve a PASS |
| --- | --- | --- |
| M05.01 — 7ce694ac0375415ac2695b6aa3084adfcdd82226 | Check «C15-01.M05.01 - cobertura 2340 y cinco negativos» completed/success, Scope Lock/V5 success | Conteos/orden y negativos de fixture/instantánea, no mapeo semántico 2340 IDs × pantalla × ruta × API; no JWT vivo |
| M05.02 — 531717a8dca49ab0787d611c77652d5eab081ab9 | Contrato/DTO xUnit en GitHub-hosted .NET 10, backend y Scope Lock/V5 success | Fixtures y contrato de errores no demuestran cada endpoint autenticado ni proveedor de permisos real. Check global «Módulos, cargas, sesión, catálogos, variantes, accesibilidad, facturación y correo» **completed/failure** en este SHA |
| M05.03 — 71fae45dcd26a0fab79b081ac0d3806f39dad4d3 | Check «C15-01.M05.03 - real MySQL 8.4 2340 IDs and rollback» completed/success con servicio aislado, Scope Lock/V5 success | MySQL efímero de IDs de prueba ≠ cruce vivo e institucional entre 2340 tareas y artefactos de frontend/backend, ≠ Aiven/PROD |
| M05.04 — 1bd41c39492799eb77f99cc9cd9ca802edde5a23 | Check «C15-01.M05.04 - source audit and adversarial cases» completed/success, Scope Lock/V5 success | Inspección Node estática/lexical de rutas y casos negativos ≠ navegación real de todas las pantallas ni autorización JWT por tenant |
| M05.05 — 1bee9d8b92a83331895527892c808aecac036be3 | Check «C15-01.M05.05 - 8 regresiones adversariales rutas y guardas» completed/success, Scope Lock/V5 success | Mutaciones adversariales de fuente ≠ prueba HTTP de BOLA/IDOR por tenant, sucursal, fichero, job y propiedad |
| M05.06 — c1f717d6dd705d350a4e77dc3c5fb780adb78abd | Check «C15-01.M05.06 - Chromium tres regresiones y presupuesto local» completed/success; log 3/3 real Chromium y build Angular; Scope Lock/V5 success | Playwright 320/768/1280, Tab auténtica, 403/503 fail-closed con localStorage sintético NOT-JWT y respuestas page.route: no JWT firmado, MySQL empresarial, dispositivos físicos ni Core Web Vitals de campo. **Check global** de módulos/transfers **completed/failure** en SHA fuente |

Las dos fallas globales citadas en M05.02 y M05.06 **se conservan**. El hecho de que los checks dirigidos o el Scope Lock sean success no convierte un job global en success ni obliga a bloquear esta auditoría documental por suites ajenas a su diff. La regresión 89a553089a882b8ae4aea7918b6b1c78c1cda368 2/3 falló antes de la reparación c1f717d6..., no se borra ni se reinterpreta.

## A1 negativo explícito — afirmaciones que deben rechazarse

1. **RECHAZAR:** «M05.01 pasó, por tanto hay trazabilidad semántica completa entre los 2340 IDs, todos los módulos y todas las rutas». Su check valida el alcance codificado de instantánea/reglas, no integraciones de todos los módulos.
2. **RECHAZAR:** «HTTP/DTO de prueba equivale a JWT firmado con permisos y revocación de datos reales»; distinguir fixture de autoridad.
3. **RECHAZAR:** «MySQL de runner demuestra readback del Sheet institucional o restore productivo»; exigir evidencia de fuente específica sin salir del Scope Lock.
4. **RECHAZAR:** «Chromium de viewport emulado con token sintético equivale a dispositivo físico, JWT real, WCAG integral o métricas p75 LCP/INP/CLS».
5. **RECHAZAR:** «Los workflows globales failed/skipped/in_progress pasaron porque un check dirigido fue success». Estado exacto y SHA importan.

## A2/A3, riesgos y dependencias

Para **este documento R0** no aplican nuevos HTTP, JWT/RBAC, MySQL, browser, pruebas de roles ni rollback runtime porque no se alteran esas tecnologías. NO_APLICA es exclusivamente para ejecutar de nuevo suites runtime por este archivo. La categoría de riesgo de los módulos inspeccionados continúa R2, con controles JWT firmado, RBAC, tenant A/B, recursos/propiedad, rollback real, dispositivo y rendimiento global cuando PLAN D de un ID específico lo exija. Mantener fallo histórico; no forzar PLAN, COLA o DAG G/O, no editar BITACORA previa. Esta auditoría no es reparación de ninguno de esos deficits; se entregan discrepancias causalmente verificadas para resolver en su ID de origen.

## A4/A5 y reproducibilidad

Reproducir para cada SHA con endpoint GitHub /repos/solqaryn/Solqaryn/commits/<SHA>/check-runs?per_page=100, comparar check.name, status=completed, conclusion y head_sha exacto, leer job/log en pruebas pertinentes. Para este changeset exigir diff limitado a este documento y sección autocontenida aditiva de CHANGELOG, verificación exact-SHA de verify-solqaryn-project-scope y V5 procedencia, lectura posterior PLAN/COLA/BITACORA y candado V17 CAS. No aceptar SKIPPED/CANCELLED/IN_PROGRESS/FAIL como PASS requerido.

**A5 PENDIENTE:** otra Task Javier distinta de DEV01 debe revisar independientemente exactitud de los seis SHA, clasificación fixture vs integración y falsos PASS. No acredita «M06 completa», no libera C15-01.M06.06 por decreto y no permite QA/main/PROD. Rollback de este texto con git revert normal bajo exclusión, sin force-push ni borrado histórico.
