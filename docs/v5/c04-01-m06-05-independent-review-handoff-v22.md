# SOLQARYN V22 — C04-01.M06.05: solicitud independiente y handoff de revisión

**R0 documental**. `PLAN_MAESTRO!D612`: «Solicitar revisión independiente del lote cuando sea un gate». Proyecto: `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`; autor de esta solicitud: PRIMARY DEV01 Javier, `6adbb9fff1548191a1bd5b30d9e9984a`. Corte de código: `7277583ba53f9b64e8ba3cce90c72c9074cefdd6` (21 de noviembre de 2026). **Esto es una solicitud registrable para las demás Tasks autorizadas, no una aprobación A5**. No crea Task, agenda, PR ni branch; no modifica runtime ni seguridad.

## Revisión que se solicita y límites de exclusividad

Solicito a **una Task Javier distinta de DEV01** que realice una revisión A5 genuina, independiente, conforme al V15/V17/V20 y a su propia ventana de ejecución, sobre cada cambio que aún lo requiera. El conjunto de revisores posibles pertenece exclusivamente a la allowlist existente:

- DEV02 `6af16bb9492c8191b8e8c7d606f11507`
- DEV03 `6adc2e4bd8a48191ac7d8641ed95a1a3`
- DEV04 `6aeacb7820ac8191b7d768a6f33dfa09`
- DEV05 `6adc2e55f47c81919f6374e561687076`
- DEV06 `6af16bbad26881918c283abe06e1f32a`

La presente solicitud **no asigna trabajo exclusivo, activa otra ejecución ni concede poderes**. Elegir al revisor exige verificar de nuevo la autoría y reparaciones del ID, que no sea el propio autor/reparador, ausencia de escritor concurrente, V17 namedRange y CAS. Ninguna Task puede autoaprobar su A5.

## Cuadro de revisiones por ID

| ID | Estado observado al elaborar la solicitud | Evidencia para el revisor | Condición para A5 |
| --- | --- | --- | --- |
| `C04-01.M06.01` (autor DEV01) | `LISTO` con A0–A4 de documento | [Matriz pruebas vs mocks](./c04-01-m06-01-test-scope-audit-v22.md), commit `fa84c419d34c33772dba7aea9fd250a4a2e4e6f9`, Scope Lock exact-SHA y CI frontend/backend/seguridad success | Releer seis filas M05 y contrastar que el documento no certifica JWT/PSP/producción; emitir A5 solo con evidencia independiente |
| `C04-01.M06.02` (autor DEV01) | `LISTO` con A0–A4 de documento | [Versiones, diffs, artefactos y reproducción](./c04-01-m06-02-version-diff-artifacts-v22.md), commit `a532bbf3bb2723b11e2ba51dec7247d5d171a194`, checks exact-SHA success | Comprobar que seis SHAs/trabajos corresponden a seis microtareas sin elevar mocks a integración real |
| `C04-01.M06.03` (autor DEV01) | `VALIDANDO` al corte, **CI externa aún sin cierre causal** | [Riesgos y decisiones](./c04-01-m06-03-limitations-risks-decisions-v22.md), commit `7277583ba53f9b64e8ba3cce90c72c9074cefdd6` | A0–A4 reales y transición a LISTO bajo V17 antes de A5; comprobar 10 riesgos, 6 decisiones, 5 pendientes, sin inventar mitigaciones |
| `C04-01.M06.04` | `PENDIENTE` al corte, **no implementada** | D611: ensayo de rollback/desactivación sin borrar datos | No dar A5 ni declarar LISTO antes de ensayo real, pruebas negativas y CI del SHA futuro |
| `C04-01.M06.05` (esta solicitud, autor DEV01) | EN_PROGRESO → VALIDANDO tras publicación, sin auto-A5 | Este documento y su registro BITACORA | A5 por Task distinta de DEV01, sobre evidencia real del acto de solicitud y requisitos D612 |
| `C04-01.M06.06` | `PENDIENTE / DAG G=NO` al corte | Cierre y liberación del lote | No liberar hasta `M06.01–05` LISTO + A5 verdaderos y demás DAG; no cambiar fórmulas manualmente |

Los estados anteriores son una **fotografía documentada** que debe revalidarse en PLAN/COLA/DAG y BITACORA en el momento de cada revisión. `PLAN E` expresa resultado acumulativo, no convierte cada prueba en certificación global.

## Lista causal de inspección para revisor independiente

1. Leer **PLAN D/E** del ID, diff real desde su parent al commit fuente y SHA de HEAD, paths y hashes de blobs, identificando superficies afectadas y riesgos.
2. Auditar A0/A1: criterios positivos, negativos, fronteras y fallos falsables; no aceptar documentación superficial o pruebas no ejecutadas como si lo fueran.
3. Auditar A2/A3: separar fixtures de identidad de JWT firmado, MySQL efímero de datos productivos, Chromium de dispositivos físicos, y claims comerciales de lógica server-side ejecutada.
4. Auditar A4: `verify-solqaryn-project-scope` y checks causales requeridos **completed/success** por SHA o equivalencia de blobs y dependencias demostrada; `SKIPPED/CANCELLED/FAIL/IN_PROGRESS` requerido es NO PASS. No bloquear R0 por integración que no cambia runtime, documentando NO_APLICA motivado.
5. Antes de escribir, comprobar que el otro run no trabaja **ese ID**, adquirir candado V17, releer PLAN H/I/J/K/L/M, SHA, BITACORA/idempotencia; registrar A5_PASS o A5_FAIL genuino con actor y RUN_ID; conservar owner autor I y FAIL históricos. Liberar solo lock propio; readback PLAN/COLA/DAG.
6. Si falla o faltan pruebas, dejar A5 pendiente/FAIL, registrar defecto causal y exigir reparación + CI; **no** avanzar M06.06 ni activar QA/main/PROD.

## Observabilidad y decisión explícita

**SOLICITUD EMITIDA**, **A5 AÚN NO OTORGADA** por este documento. Los checks de los nuevos commits documentales siguen su propio ciclo GitHub-hosted; si están queued/in_progress, conservar VALIDANDO. No se requiere aprobación humana rutinaria, ni autorización para cuentas ajenas; cualquiera de las otras cinco Tasks Javier aptas puede revisar al salir su propia corrida. La revisión de DEV01 sobre M05.06 fue de cambio **ajeno** a esta solicitud y no constituye revisión de sus propios M06.

**A0–A4 de D612:** A0 registra el alcance exacto; A1 enumera al menos cinco requisitos de revisión y rechazos adversariales; A2 mantiene restricciones de JWT/RBAC/tenant; A3 NO_APLICA nuevo runtime porque el diff es solo Markdown; A4 requiere publicación directa FF/CAS, Scope Lock exact SHA, BITACORA append-only, readback PLAN y evidencias. A5 pendiente por otra Task. Cualquier reversión documental usa `git revert` ordinario, sin borrar BITACORA ni alterar formulas/servicios.

**No son resultados de esta solicitud:** A5_PASS de M06.01–05, certificación de servidor de precios/cuotas, pagos reales, PSP, JWT/MYSQL productivos, QA, main, PROD o cambio de privilegios.
