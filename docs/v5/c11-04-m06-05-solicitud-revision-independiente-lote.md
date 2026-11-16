# C11-04.M06.05 — solicitud institucional de revisión A5 independiente del lote

**Proyecto:** `solqaryn/Solqaryn@dev` con `PROJECT_SCOPE_LOCK=STRICT`. **Autor de esta solicitud:** DEV04 / Task `6aeacb7820ac8191b7d768a6f33dfa09`. PLAN fila 1728, candado exclusivo V17 2000001728. **Riesgo R0:** petición documental, sin código ejecutable, endpoints, DB, autorizaciones o despliegue.

## Petición efectiva y estado

Se deja **solicitada mediante documento de GitHub y evento institucional de BITACORA**, no aprobada, la revisión independiente de los alcances recién cerrados en DEV del lote C11-04.M06. La consulta será visible a las otras cinco Tasks Javier al leer PLAN/COLA/DAG/BITACORA. Esta solicitud **NO** invoca otra Task, no crea nuevas Tasks, no cambia sus schedules, no cambia `owner`, ni implica que alguien haya aceptado la revisión.

**Autoras y candidatas de revisión:** los incrementos de M06.01–05 son de DEV04. El A5 deberá acreditarlo una Task cuyo ID sea **distinto** de `6aeacb7820ac8191b7d768a6f33dfa09` entre: DEV01 `6adbb9fff1548191a1bd5b30d9e9984a`, DEV02 `6af16bb9492c8191b8e8c7d606f11507`, DEV03 `6adc2e4bd8a48191ac7d8641ed95a1a3`, DEV05 `6adc2e55f47c81919f6374e561687076`, DEV06 `6af16bbad26881918c283abe06e1f32a`. Esta enumeración es **allowlist**, no una asignación ni reserva.

## Matriz de material que la Task independiente debe leer

| ID | SHA de entrega en DEV | Cambio y prueba que la revisión A5 debe verificar | Estado A5 |
| --- | --- | --- | --- |
| `C11-04.M06.01` | `c30283c16fca5639ddb52864aa455bc2a868a5c0` | Auditoría documental M05.01–06 real vs fixture, 6 fuentes con límites precisos; Scope Lock/V5 exact SHA | PENDIENTE |
| `C11-04.M06.02` | `91c219eaba862ff29d01baefdd56c54738bec212` | Inventario reproducible de SHAs, jobs y diffs, comandos de lectura declarados no ejecutados; Scope Lock/V5 | PENDIENTE |
| `C11-04.M06.03` | `a628a643df449e686750dcf7e4d11d34385081a5` | Registro siete riesgos y decisiones, NO_MEDIDO para SLO/CWV/costes y no certificación HTTP JWT/MySQL/exporter | PENDIENTE |
| `C11-04.M06.04` | `c8ecd797386daa438c5f4c48675200b964affb3b` | Ensayo Git reversible en runner real, script/CI/seguridad, `ROLLBACK_ISOLATED_PASS=1`; solo A5 **después** de A0–A4 LISTO del SHA causal | CONDICIONAL A LISTO |
| `C11-04.M06.05` | SHA de entrega registrado al publicar | Este documento y su evento idempotente de solicitud, sin autoaprobación; verificar consistencia con PLAN/BITACORA | PENDIENTE |

La A5 debe revisar **código/documento, diff, requisitos y pruebas por sí misma**, no limitarse a aceptar que GitHub Actions muestra verde. Un `A5_FAIL` requiere registrar defecto y regresión causal sin borrar datos históricos. La revisión no debe atribuirse al autor del cambio ni aceptar un autor ajeno al allowlist.

## Puertas, limitaciones y secuencia

Exigir A0–A4 del SHA correcto, trazabilidad GitHub/PLAN/BITACORA y Scope Lock STRICT. Una sucesora no se libera mientras un antecedente requerido continúe VALIDANDO o falte un A5 del SHA pertinente; COLA G/O y DAG F/G son fórmulas, nunca escribir SI manualmente. En particular, no promover `C11-04.M06.06` a ejecutable por esta solicitud. Se puede seguir desarrollando cualquier otra tarea independiente conforme DAG sin esperar una intervención humana artificial.

No se ha probado un servidor JWT real, persistencia MySQL, coste/SLO/CWV, sistema de telemetría remoto ni reversión de datos de producción con estos documentos. La reversión Git ensayada no es restauración de DB. No cambiar QA/main/PROD, secretos, cuentas, scheduling o datos.

## A0–A5 específicos de esta microtarea

**A0:** un artefacto de solicitud claro, con allowlist, SHAs, criterio independiente y no falsificación de aprobación. **A1 documental:** comprobación de que los IDs anteriores corresponden a los artefactos y no se convierten en PASS por una petición. **A2/A3:** NO_APLICA por no tocar runtime ni datos. **A4:** GitHub Scope Lock/V5 exact SHA en el nuevo commit más readback institucional y evento idempotente `A5_REVIEW_REQUESTED`. **A5:** se mantiene PENDIENTE y deberá realizarla otra Task Javier. Reversibilidad con `git revert` normal, sin eliminar BITACORA ni alterar programación.
