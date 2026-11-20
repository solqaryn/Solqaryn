# SOLQARYN V5 V22 — C15-02.M06.05 | Solicitud técnica de revisión independiente

**Acción puntual PLAN_MAESTRO!D2232:** «Solicitar revisión independiente del lote cuando sea un gate». **Autor de este entregable documental:** PRIMARY DEV 03 / JAVIER_CHATGPT, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Repo/rama:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Riesgo del diff:** R0 documental; **riesgo de las integraciones auditadas:** R2 y recursos externos R3 sin permiso. **Fuente vigente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Doc Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`) y Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`.

## 1. Solicitud efectiva e independencia

Se deja esta **solicitud verificable de A5** para `C15-02.M06.05` dentro del expediente institucional y la BITACORA. La revisión de **este documento** debe efectuarla una de las otras cinco Tasks JAVIER autorizadas; **NO** la autora DEV03, ni quien repare este artefacto si se llegara a reparar. La solicitud no es aprobación ni asignación forzada de una ejecución física; ninguna Task nueva, RRULE ni scheduler es creado. Para cada cambio distinto, usar un revisor que no sea autor ni reparador de ese SHA. El revisor debe leer diff, contratos de PLAN D2232/E2232, checks en SHA exacto, matriz positiva y negativa, y registrar A5_PASS/A5_FAIL con lock V17, K, BITACORA A:Z y readback; puede rechazar.

Si `C15-02.M06.04` todavía no acredita un rollback real limitado al alcance seguro, su revisión queda PENDIENTE; **M06.05 no lo sustituye ni certifica M06.06**. Las diferencias por autor entre M06.01–05 son independientes; no deducir A5 grupal por A5 individual.

## 2. Manifiesto de fuentes, pruebas dirigidas y límites sin certificación cruzada

| ID | SHA fuente de entrega | Qué debe comprobar A5 | Limitación / falla que no se borra |
| --- | --- | --- | --- |
| M05.01 | `a2dd7af7d6f1a437d1d886ad4596a4e47720d9c0` | 4/4 Vitest unitario Angular, JOB 136014234950 | No backend/JWT/MySQL; release/Modulos global FAIL |
| M05.02 | `01e4131f03ffed3887ace004738c9018e2314211` | 6/6 Vitest `HttpTestingController` JOB 136018264999 | Simulador de cliente, no HTTP servidor; V5 original FAIL corregido en `ec765931f852fbb7e8f24e5761dd7e6b274b8540` descendiente |
| M05.03 | `b9466fc06b4c2c882948fe12dae0ccdaf46c70b8` | 6/6 xUnit Category=Integration, Oracle EF en MySQL 8.4 efímero, JOB 136021869603 | Modelo aislado, no AppDbContext productivo; migración global FAIL |
| M05.04 | `8865c6c483157e67598d77436d2df58584ca7865` | 2/2 Playwright Chromium JOB 136016023781 | Sesión sintética; no proveedor/JWT genuino |
| M05.05 | `ae7d06e00db9aa647792911f2648833af61775e5` | 5/5 Playwright Chromium con JWT HTTP A/B aislado JOB 136018083744 | No Hostinger/Cloudflare/R2, ni QA/PROD; check global FAIL preservado |
| M05.06 | `2ca545eaa8eb9a75e0b909fff0f6404281dcc6ab` | 3/3 Playwright Chromium JOB 136028793145 con Tab/Enter y performance lab | 403 fixture, FCP/DCL lab; LCP/CLS/INP campo NO_MEDIDO |
| M06.01 | `f665ee71e9acc34696d1733ca8481c9fe171737a` | Auditoría que distingue pruebas reales de mocks, [acta](https://github.com/solqaryn/Solqaryn/blob/f665ee71e9acc34696d1733ca8481c9fe171737a/docs/v5/c15-02-m06-01-real-vs-mock-scope-audit.md) | R0 documental; A5_PASS independiente DEV03 sobre ese SHA; sin certificación global |
| M06.02 | `2dbe319320936ec79312540e2a0416e13368130a` | [manifiesto](https://github.com/solqaryn/Solqaryn/blob/2dbe319320936ec79312540e2a0416e13368130a/docs/v5/c15-02-m06-02-reproducibility-manifest-v22.md), SHA/diff/RUN/JOB y negativo | A5_PASS independiente DEV03 sobre ese SHA; sin nuevas pruebas de proveedor |
| M06.03 | `3aeb9ae1f1b920a555d79ca497cf3fc5546520b8` | [riesgos abiertos](https://github.com/solqaryn/Solqaryn/blob/3aeb9ae1f1b920a555d79ca497cf3fc5546520b8/docs/v5/c15-02-m06-03-riesgos-limitaciones-decisiones-v22.md) | A5_PASS independiente DEV03 sobre ese SHA, FAIL global y R3 no cerrado |

**Los A5_PASS de M06.01/.02/.03** requieren validación de `PLAN_MAESTRO!K2228:K2230` y BITACORA por el revisor; no son hechos atemporales ni autorizan extender aprobación a M06.04/05/06. M05.01–06 tenían PLAN H LISTO y K A5_PASS en el censo causal de M06.01. Revisar si cambian antes de cierre.

## 3. Prueba positiva, negativa y criterio falsable para la revisión

**Positiva:** un revisor encuentra el commit de cada unidad, su diff + job id exacto y aserciones para el alcance declarado; específicamente [MySQL 8.4 efímero 6/6](https://github.com/solqaryn/Solqaryn/actions/runs/44655887915/job/136021869603) y [JWT HTTP A/B 5/5](https://github.com/solqaryn/Solqaryn/actions/runs/44654745490/job/136018083744), sin tratarlos como MySQL/Aiven productivo. Para esta solicitud, verifica que los enlaces a tres auditorías M06.01-.03 y sus aprobaciones independientes son consistentes con K/BITACORA.

**Negativa:** el revisor debe **rechazar** que `HttpTestingController` demuestre transporte HTTP real, que `403` fixture demuestre JWT firmado, que `skipped/cancelled/in_progress/failed` requerido sea PASS, que CWV campo sea medido, que A5 de un SHA se copie a otro, o que quien escribió/repairó M06.05 se autoapruebe. Si existe P0/P1 causal o fracaso de gate obligado, conservar A5_FAIL y pedir reparación; nunca borrar el fallo anterior ni convertir R3 en permitido.

## 4. Criterios de salida, riesgos, rollback y seguimiento

- **A0 del diff:** comparar Markdown + sección autocontenida en CHANGELOG sin líneas intercaladas, hashes, 9 filas fuente, ID D2232 y sin secretos.
- **A1:** contrato de solicitud A5 con positivos, negativos, procedencia de cada evidencia y respuesta PASS/FAIL falsable, sin confundir petición con aprobación.
- **A2:** aislamiento tenant/JWT/RBAC y permisos tratados como límites probados solo por pruebas dirigidas originales, nunca certificación de backend/permisos globales por este texto.
- **A3:** `NO_APLICA` nuevas pruebas .NET/HTTP/Angular/MySQL/Chromium a diff documental; sí leer logs históricos y estados check-runs, sin reemplazar validaciones faltantes.
- **A4:** commit FF/CAS a dev, Scope Lock y V5 check `completed/success` SHA exacto, transición PLAN H bajo V17, BITACORA 26 columnas y relectura COLA/DAG derivadas.
- **A5 propia M06.05:** `PENDIENTE_OTRA_TASK_JAVIER` tras LISTO DEV, revisor realmente distinto de DEV03 y de reparador.
- **Riesgos vivos:** fallos globales de release/migraciones/Modulos, datos de campo `NO_MEDIDO`, proveedores Hostinger/Cloudflare/R2/DNS, secretos, pagos y despliegue R3 sin permiso. M06.06 no se cierra por solicitud documental.
- **Rollback de esta solicitud:** `git revert` normal del commit, idempotencia nueva si se reabre; preservar estados, owners, fórmulas y BITACORA, no force-push. Ningún rollback live o borrado de datos reales.

**Resultado específico D2232:** solicitud técnica independiente y paquete reproducible con criterios de revisión y rechazo, distinta de revisión realizada. **No autoriza QA/main/PROD ni acredita R3.**
