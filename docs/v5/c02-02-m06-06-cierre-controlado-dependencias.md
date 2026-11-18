# C02-02.M06.06 — Cierre controlado DEV y mapa de dependencias de placement shared/dedicated

**Fecha de corte:** 2026-11-18. **Repositorio:** `solqaryn/Solqaryn` · **rama:** `dev` · **PROJECT_SCOPE_LOCK:** `STRICT`. **ID institucional:** `C02-02.M06.06`, PLAN_MAESTRO fila 361. **Riesgo del cambio:** R0 documental; el riesgo real de tenancy, JWT/RBAC, DB, migración y cutover sigue siendo R2/R3. **Autor de esta entrega:** Task Javier DEV03.

## A0 — Resultado, alcance y frontera de seguridad

El requisito de la microtarea es **«Cerrar estado, liberar el siguiente lote y actualizar el mapa de dependencias»**. Este manifiesto documenta el cierre del **alcance DEV de evidencia y planificación**, no una activación de infraestructura, router, migración, tenant dedicated, servicio vivo ni una aprobación de QA/main/PROD. La única escritura de código permitida para este ID es este archivo y una entrada autocontenida de `CHANGELOG_AI.md`. No se cambian Domain, Application, Infrastructure, API, migraciones, datos, archivos de configuración, JWT, secretos, workers ni proveedores.

La transición institucional `PENDIENTE -> EN_PROGRESO -> VALIDANDO -> LISTO` solo procede tras A0–A4 verificables sobre el SHA publicado y CAS V17 con readback. El A5 de **esta nueva unidad M06.06** será una revisión genuina de otra Task Javier distinta de DEV03 y no se autoconcede por este texto. El DAG y COLA recalculan gates mediante fórmulas vigentes; **no se escriben SI manuales ni se presupone que un successor quedó libre**.

## Evidencias previas verificadas al preparar la entrega

| Prerrequisito | PLAN H y A5 independiente existente | SHA/fuente de aceptación | Límite exacto |
| --- | --- | --- | --- |
| C02-02.M05.01–06 | Seis entregas LISTO con A5 de otra Task Javier; matriz detallada en [M06.05](c02-02-m06-05-handoff-revision-independiente.md) | M05.03 corregida: `e3688574566e84e4def5d98fda8ec852d3158a1e`; otros SHA en M06.05 | No certifican JWT HTTP firmado, router real, consultas SQL row-level o conexión dedicada |
| C02-02.M06.01 | LISTO; A5 DEV05 sobre trabajo DEV04 | `06446d0969a99ec91538335b771813bd83174617` | Auditoría documental que distingue pruebas reales y mocks |
| C02-02.M06.02 | LISTO; A5 DEV04 sobre trabajo DEV03 | `a8286ba556d6c88e414dc2ef1f303b7381d93712` | Manifiesto/versiones, no repetición de pruebas por este documento |
| C02-02.M06.03 | LISTO; A5 DEV04 sobre trabajo DEV03 | `5628543352af66fc01c5c998d0bc3d5ddddc6fe6` | Siete riesgos PL-01..PL-07 siguen documentados y abiertos según alcance |
| C02-02.M06.04 | LISTO por cierre cruzado DEV01; A5 DEV02 sobre trabajo DEV03 | `07628bc77766974ee38526844f678ece76cc5c21` · [GitHub Actions MySQL 8.4](https://github.com/solqaryn/Solqaryn/actions/runs/44413913380/job/135189027831) | Oracle EF/MySQL 8.4 efímero; xUnit Category=Integration 3/3; dos filas Empresa preservadas tras rollback; no datos productivos |
| C02-02.M06.05 | LISTO; A5 DEV06 sobre trabajo DEV04 | `bdd95e342ab85bfb24cbfc02ba5aa43d532b00c4` | Handoff R0 de revisión independiente, no nueva aprobación de otros ID |
| C01-01.M06.06 y precedentes | DAG_TECNICO del ID M06.06 en `F=VALIDADO`, `G=SI` al reclamar | Fuente institucional dinámica PLAN/COLA/DAG | Elegibilidad técnica de documentación; no habilita acciones R3 |

**Readback de admisión:** PLAN_MAESTRO H361=PENDIENTE e I361 vacía al seleccionar, COLA G/O=SI, DAG F=VALIDADO/G=SI, sin otra EN_PROGRESO de DEV03. Candado V17 `SQV17_C02_02_M06_06` sobre H:I de la fila 361 y CAS `occurrencesChanged=1`; owner histórico DEV03. El evento CLAIM se registra separadamente en BITACORA A:Z. Los A5 históricos de filas anteriores se conservan; no se duplican.

## Mapa de dependencias y alcance habilitado

1. **Entrada M06.06:** `C02-02.M05.01–06+A5`, `C02-02.M06.01–05+A5`, `C01-01.M06.06+A5`, conforme a DAG_TECNICO de M06.06; verificar de nuevo F/G inmediatamente antes de cualquier cierre.
2. **Salida M06.06:** tras cierre DEV A0–A4 y eventual A5 independiente, reconsultar las filas sucesoras en DAG_TECNICO A/F/G y COLA G/O; permitir a otras Tasks reclamar **solo** si su PLAN H=PENDIENTE e I vacía y los cuatro indicadores DAG/ COLA siguen genuinamente positivos. Ningún valor se fuerza desde este documento.
3. **Separación por ambiente:** `LISTO_DEV` y `A5_PASS` son evidencias de flujo de desarrollo; no son autorización para QA/main/PROD, Aiven productivo, DNS, Vercel/Render productivos, migraciones irreversibles, acceso a secretos ni cambio de connection strings.
4. **Dependencias reales abiertas:** autenticación JWT firmada por HTTP, RBAC/roles/revocación y pruebas adversariales A/B/IDOR/BOLA en API real; filtrado de consulta por tenant, router server-side y DSN no manipulable; migración/dual-write/cutover real con respaldos verificados, checksum y RPO/RTO (**NO_MEDIDO**). Estos requisitos no se consideran PASS por el DAG SI ni por pruebas de EF aisladas.

## A1–A3 y aceptación observable

**A1 documental:** cotejar ID/estado/owner de cada prerrequisito, seis M05 y cinco M06, con PLAN_MAESTRO, BITACORA y fuentes de commit; confirmar A5 de revisor diferente, preservar cualquier A5_FAIL histórico y el único ensayo R2 MySQL real. Si una referencia no coincide, no cerrar.

**A2:** NO_APLICA **al diff R0** porque no altera autenticación, RBAC, tenant, datos ni runtime. Permanecen NO_VERIFICADOS los controles empresariales reales enumerados; no afirmar certificación total.

**A3:** NO_APLICA **al diff R0** por ausencia de ejecutables y de cambios de proveedor. El ensayo Category=Integration 3/3 de M06.04 es evidencia previa distinta, no una prueba ejecutada por M06.06 ni una autorización de producción.

## A4, A5 y reversión

A4 exige commit fast-forward condicionado al HEAD actual, análisis estático sin secciones vacías de CHANGELOG, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completados con SUCCESS sobre **el SHA propio**, readback del estado/owner y evento BITACORA único. Nunca calificar SKIPPED/CANCELLED/IN_PROGRESS como PASS. Solo después de validar A0–A4 cambiar `VALIDANDO -> LISTO` mediante candado propio V17 y CAS, conservando owner en I.

**A5:** no concedida al publicar; requiere revisor de otra Task Javier, revisión independiente del diff y este mapa con SHA/blobs comprobados. QA/main/PROD siguen NO_GO.

**Rollback de este cambio R0:** revert normal del commit documental después de revalidar HEAD; no borrar historia compartida, eventos BITACORA, gates o A5 pasados. Los cambios productivos futuros necesitan plan y autorización separados.

