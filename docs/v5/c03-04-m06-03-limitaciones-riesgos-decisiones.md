# C03-04.M06.03 — Limitaciones, riesgos abiertos y decisiones de continuidad (V22)

PROJECT_SCOPE_LOCK=STRICT
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
MICROTAREA_ID=C03-04.M06.03
TASK_ID=6aeacb7820ac8191b7d768a6f33dfa09
RISK=R0_DOCUMENTAL_SIN_RUNTIME
A5=REVISION_INDEPENDIENTE_PENDIENTE

## Acción atómica y autoridad

PLAN_MAESTRO!D574 exige **documentar limitaciones, riesgos abiertos y decisiones tomadas** de C03-04 (incidentes, revocaciones, respaldo, restauración y continuidad). PLAN E574 es el objetivo acumulativo de M06 y no permite certificarlo por escribir esta unidad. Se preservan los prerequisitos dinámicos DAG/COLA, la historia de BITACORA y las decisiones anteriores. Este changeset contiene exclusivamente este Markdown y una sección autocontenida del CHANGELOG; no modifica controladores, UI, DB, infraestructura, migraciones, secretos, configuración de entornos ni autorizaciones.

Fuentes de comprobación: [auditoría M06.01](./c03-04-m06-01-auditoria-pruebas-vs-mocks.md), [manifiesto M06.02](./c03-04-m06-02-version-diff-evidencia.md), [registro de riesgos M01.04](./c03-04-m01-04-riesgos-supuestos-decisiones-continuidad.md) y los seis SHA causales M05 expuestos abajo. Las pruebas anteriores **no se ejecutan de nuevo** por esta documentación.

## Evidencia disponible, valor y limitación

| ID | SHA de entrega | Evidencia ya referenciada | Limitación conservada |
| --- | --- | --- | --- |
| M05.01 | `a2aa4ff18010d88330d326061211d30fb02ccbd3` | 27 xUnit de reglas de dominio y negativos | Fuente/autoridad simulada; no JWT HTTP o DB real |
| M05.02 | `c33d3b80bc38e009790c3071b6f82983f9de5b77` | 22 pruebas de contratos DTO/errores | Contrato in-process, no servidor autenticado |
| M05.03 | `e8232053e1680303abd2a6fa7a6c5a4ec3774b19` | 3 integraciones MySQL 8.4 efímero | No se valida backup/restore íntegro ni servicio JWT |
| M05.04 | `6b523ce077f534e6d6326165c82ec812f247a929` | 17 pruebas positivas de aceptación | No equivale a E2E de API |
| M05.05 | `204a4f972e3d6935ec553f82c36777271002b0fe` | 35 casos negativos de dominio | Fixtures y roles in-process |
| M05.06 | `4b4163c55b002ffe02c05f60240fa059b834a492` | 10/10 xUnit Release y MySQL 8.4.13 efímero; [job 135845972811](https://github.com/solqaryn/Solqaryn/actions/runs/44604929709/job/135845972811) | Preflight de solo lectura; no ejecuta restauración, autorización de mutaciones ni recuperación productiva |

Los conteos M05.01–05 proceden del manifiesto institucional M06.02/PLAN M05 y sus enlaces. El job de M05.06 es evidencia real de un servicio desechable GitHub-hosted, **no** de Aiven productivo, un backup restaurado o una operación R3. Un check global fallido/cancelado/en curso nunca se toma como PASS por citar este cuadro.

## Matriz de riesgos abiertos y salida falsable

| Riesgo / severidad | Realidad y límite | Condición futura para considerarlo mitigado |
| --- | --- | --- |
| R01/R02 — crítico: restaurar al destino equivocado o backup corrupto | La evidencia SQL efímera valida rollback de fixture, no restauración del backup cifrado con manifest | Negativo de destino no autorizado, checksum incorrecto, restore de la misma copia en destino aislado, comparación de integridad y rollback, con SHA/job |
| R03/R11 — crítico: exposición de credenciales, JWT o PII | Logs/fixtures controlados no prueban ausencia en todos los proveedores y adjuntos | Sanitización en logger/transporte/artefactos y pruebas negativas sin imprimir valores sensibles |
| R04/R05/R06 — crítico: cruce A/B, rol global indebido o revocación incompleta | Asserts de dominio no sustituyen JWT firmado, HTTP real, membresía/roles vivos y denegación tras revocación | Pruebas por superficie de autenticación/IDOR/BOLA/tenant A-B, token revocado y 401/403 con persistencia aislada |
| R07/R16 — alto: dos escritores o rollback que pisa cambios | CAS y candados V17 protegen el registro operativo, no una migración/restore futuro | Lock efectivo por operación e idempotencia, carreras adversariales, HEAD CAS y reversión no destructiva |
| R08/R14 — alto: deriva EF/SQL u objetos Cloudinary no restaurados | MySQL 8.4 efímero no contiene el conjunto DB+archivos real | Comparar esquema, migraciones, FK y objetos vinculados con manifest de ambos medios en laboratorio autorizado |
| R09/R10/R13/R15 — alto: RPO/RTO o llaves/runner/entorno inferidos | No existe evidencia en esta salida de tiempos de recuperación, custodia de claves o equivalencia DEV/Desarrollo | Medir drill real autorizado, verificar entornos/llaves vigentes, costes y fail-closed de proveedor |

Ninguna fila declara un riesgo cerrado. El inventario completo R01–R16 sigue en [M01.04](./c03-04-m01-04-riesgos-supuestos-decisiones-continuidad.md), sin cambiar la severidad ni eliminar FAIL históricos.

## Decisiones registradas y decisiones expresamente pendientes

- **Confirmado para esta microtarea:** documentar con pruebas de procedencia, no ejecutar código ni restauraciones. Mantener autorizaciones en backend, mínimo privilegio, aislamiento A/B, prohibición de secretos en repositorio y diferencia entre CI sintética y real.
- **Pendiente de evidencia técnica/alcance independiente:** mecanismo de revocación JWT efectivo (D03); política de recuperación consistente DB+objetos (D04); control break-glass scoped temporal y auditado (D05); trigger y configuración de entorno (D06); ensayo de restore/rollback idempotente (D08).
- **No fijado ni autorizado por este documento:** RPO/RTO (D01), retención de respaldo/PII (D02), costes/credenciales/proveedor para nuevas pruebas (D07), promoción a QA/main/PROD (D09). No transformar estas decisiones en aprobaciones humanas rutinarias para trabajo DEV independiente; aplicar R3_NO_GO a ejecución productiva/irreversible.
- **Siguiente entrega propia:** M06.04 debe ensayar rollback/desactivación de forma proporcional y sin destruir datos; M06.05 requiere revisión real independiente; M06.06 solo se habilita por los prerequisitos calculados y A5 aplicables. Este archivo no fuerza G/O a SI ni sustituye esos entregables.

## Gates de calidad de esta salida

- **A0:** requisito D574, diff sólo documental, enlaces y riesgos por identificador; revisión de consumidores C10/C12 sin cambiar contratos.
- **A1:** negativos explícitos: rechazo de restauración con checksum inválido, tenant cruzado, SHA ajeno/SKIPPED, ausencia de secretos y rollback concurrente. Son **criterios futuros**, no test ejecutado. Para esta salida documental se valida que la matriz distingue evidencia demostrada de pendientes.
- **A2:** NO_APLICA a modificación de JWT/RBAC/tenant: no hay código ni datos ejecutables en el diff. Los riesgos de autorización siguen abiertos y no se califican PASS.
- **A3:** NO_APLICA a integración nueva de HTTP/MySQL/Chromium/backup: no se toca runtime. Las integraciones de M05 se citan sin atribuir nuevos ensayos.
- **A4:** verificar después de publicar en `dev` los checks exact-SHA `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada`, el diff, candado V17, CAS y readback PLAN/COLA/BITACORA. `queued`, `in_progress`, `skipped` o `failure` requeridos no son PASS.
- **A5:** revisor genuino de otra de las cinco Tasks Javier, distinto del autor DEV04, sin autopase; no se otorga QA/main/PROD por LISTO documental.

Rollback técnico de este cambio: `git revert` normal del commit correspondiente en `dev`, preservando el historial de CHANGELOG y BITACORA y sin force-push ni reset. Ningún rollback de base de datos se efectúa en esta unidad.
