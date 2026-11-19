# SOLQARYN V22 — C15-01.M06.03, límites, riesgos y decisiones verificables

**Salida atómica:** PLAN_MAESTRO!D2194 = «Documentar limitaciones, riesgos abiertos y decisiones tomadas». Fuentes: STANDARD V5 único, PLAN D/E, DAG_TECNICO!A2155:G2155 (VALIDADO/SI), COLA!G2194/O2194 (SI/SI al CLAIM), BITACORA y GitHub Actions. Fecha del censo: 19 de noviembre de 2026. Task DEV03 `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Riesgo R0:** solo documentación, sin cambios a frontend/backend/runtime, permisos, secretos, migraciones o workflows. PLAN E define una meta acumulativa de M06; este registro NO declara que se completó M06 ni la auditoría semántica de 2.340 IDs.

## Censo causal — limitaciones y acciones que permanecen abiertas
Todos estos 8 IDs aparecieron `VALIDANDO` durante el censo, con propietarios históricos conservados. Los números de checks corresponden exclusivamente al SHA causal indicado; las ejecuciones del HEAD y los estados pueden cambiar después del corte. Una suite verde dirigida no acredita cobertura general del producto.

| Microtarea | SHA fuente / CI constatada | Riesgo o limitación y acción de rescate |
| --- | --- | --- |
| C01-02.M04.01 | `ee844b004`, 16 SUCCESS y 3 SKIPPED; Angular TestBed 3/3 | Rutas/tema y guardas simuladas no prueban toda la aceptación JWT, revocación y tenants A/B con navegador y API reales; comprobar NG0955 y el gate causal antes del cierre |
| C01-04.M03.06 | `0b8a7ca4f`, 27 SUCCESS, 3 FAILURE y 2 SKIPPED | Falta completar la integración funcional del sink estructurado de auditoría y minimización de PII; analizar las tres fallas globales y registrar su causalidad |
| C02-01.M04.01 | `212f700e4`, 13 SUCCESS y un FAILURE global; 9/9 pruebas HTTP/JWT/MySQL/Chromium dirigidas | La matriz host/rubro/plan/sucursal/rol de contexto empresarial sigue sin acreditarse por completo; el rechazo de host reenviado falso es una prueba, no la matriz |
| C02-03.M03.03 | `820040239`, 22 SUCCESS y 2 SKIPPED; suite .NET de negativos | Falta acreditar wiring DI y contrato HTTP/JWT/RBAC real con revocación y aislamiento A/B en persistencia MySQL cuando aplique |
| C05-04.M05.06 | `102b7c5b4`, 15 SUCCESS, 1 SKIPPED, Chromium 4/4 | La adenda pide dispositivos físicos: viewports emulados no prueban hardware. CWV de campo LCP/CLS/INP = NO_MEDIDO |
| C06-04.M03.05 | `a440b54d6`, 21 SUCCESS, sin FAIL/SKIPPED | Prueba de aplicación no certifica worker/proveedor real, entrega durable, reintentos, dead-letter y tenant seguro |
| C14-01.M04.03 | `8d914060d`, 13 SUCCESS, un FAILURE global; Chromium branding dirigido PASS | La respuesta pública 503 fail-closed sin contexto no demuestra branding positivo por tenant, aislamiento y proveedor realmente integrado |
| C15-03.M05.05 | `fb096a161`, 13 SUCCESS, un FAILURE global; 9/9 JWT/MySQL/Chromium | Pendiente evidencia de ownership granular sucursal/archivos/jobs/recursos tras middleware P0 en cuarentena; no llamar PASS a un 503 en lugar de control 403 específico |

**Clasificación operativa:** las ocho requieren análisis o reparación adicional; no declarar READY_CLOSE sin cumplir A0-A4 de PLAN D. C05-04.M05.06 además depende de evidencia de hardware no disponible en el runner Chromium; no convertir en NO_APLICA la adenda. No atribuir a una Task que esté activa la escritura de todos sus scopes históricos; comprobar lock y heartbeat por ID.

## Decisiones y reglas de continuación
1. V21 REVIEW_FIRST con prioridad a cierres completos y reparaciones realizables; V22 permite CLAIM nuevo independiente aun cuando otra VALIDANDO espere pruebas, siempre con PLAN H=PENDIENTE/I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI, namedRange V17, CAS, BITACORA y readback; máximo un EN_PROGRESO propio.
2. Seguridad fail-closed: no desactivar P0, JWT/RBAC, verificaciones tenant A/B ni controles de Scope Lock para obtener tests verdes. `FAILED`, `CANCELLED`, `SKIPPED` e `IN_PROGRESS` requeridos jamás cuentan como PASS. Una falla global solo se declara no pertinente con logs/diff causal.
3. Preservar historial de defectos y A5_FAIL y owner I original. Cada reparación necesita código/test real, SHA, CI pertinente y revisor A5 independiente distinto del autor y reparador.
4. Para **esta salida R0 documental**, HTTP/JWT/MySQL/Playwright = NO_APLICA al diff, no a los riesgos registrados. Validar contenido contra fuentes, trazabilidad SHA, Scope Lock/V5, CAS y readback institucional. A5 de DEV03 debe ser por otra de las cinco Tasks autorizadas.
5. No activar QA/main/PROD, migraciones productivas, pagos, secretos ni borrados de BITACORA; no forzar fórmulas DAG/COLA ni cancelar trabajos ajenos. Ante reversión del documento usar `git revert` ordinario tras comprobar HEAD y exclusividad, nunca reescribir historia.

## Límites explícitos
La matriz es un censo dirigido, no un inventario exhaustivo de rutas ni confirmación de todas las dependencias comerciales. Las pruebas citadas son comprobables en commits y check-runs, pero no sustituyen integración ausente. Cierre de **C15-01.M06.03** requiere su A0 documental y A4 Scope Lock/V5 exact-SHA más readback; una futura A5 independiente no equivale a aceptar M06.06 ni a promocionar entornos.
