# SOLQARYN V17 — C14-04.M04.06 densidad, carga progresiva y percepción de ligereza

**Autor:** PRIMARY DEV 02 JAVIER, TASK_ID=6af16bb9492c8191b8e8c7d606f11507. **Rama:** dev. **PLAN:** fila 2149. **Lock V17:** SQV17_C14_04_M04_06 / 2000002149. **Riesgo:** R2 conservador por ruta privada y feedback de autorización.

## A0: alcance y reversión
Pruebas browser-only sobre la superficie de release readiness ya implementada en /plataforma/flujo-v5. No añade diseño ficticio, componente, backend, endpoint, permisos, cambios de datos, despliegue ni operación real. Una suite de Chromium distinta de la accesibilidad M04.05 comprueba específicamente densidad responsive, progresión observable durante revalidación y terminación opaca ante error. Reversión con git revert ordinario, sin force-push ni borrar BITACORA.

## A1–A3: regresiones reales y frontera de evidencia
Tres escenarios Playwright, sesiones HTTP **sintéticas** interceptadas en Chromium GitHub-hosted: (1) 320/375/768/1280 px, cuatro criterios, sin desbordamiento lateral ni controles mutadores en el contrato, sin PASS inventado; (2) segunda petición GET deliberadamente retenida, data-state=cargando, mensaje de progreso accesible, botón deshabilitado, sin éxito anticipado, luego transición real a exito/vacio y exactamente dos GET; (3) HTTP 503 simulado, paso a error sin spinner perpetuo, sin filtración de diagnóstico ni PASS falso. La prueba puede fallar ante regresión de estado o ancho.

Es una revisión de UX local y accesibilidad; NO acredita latencia real, Lighthouse, Core Web Vitals, SLO, rendimiento productivo, MySQL, JWT de backend, tenant A/B HTTP real ni proveedor externo. Se conserva NO_MEDIDO.

## A4–A5: evidencia obligatoria
Workflow existente con github.sha y cancel-in-progress=false; incluir este spec explícitamente junto con los anteriores. GitHub Actions debe completar Angular lint/build, Chromium, Scope Lock STRICT, procedencia V5 y checks pertinentes sobre SHA publicado antes de pasar de VALIDANDO a LISTO. CANCELLED/SKIPPED/QUEUED/IN_PROGRESS no equivalen a PASS. A5 exige revisión independiente por otra Task Javier y lectura institucional PLAN/COLA/DAG/BITACORA. Sin QA/main/PROD.
