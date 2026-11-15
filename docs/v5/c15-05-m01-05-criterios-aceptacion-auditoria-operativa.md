# SOLQARYN V5 — C15-05.M01.05: criterios de aceptación de auditoría operativa

**Fecha de preparación:** 2026-11-14 (America/Tegucigalpa). **Autor:** PRIMARY DEV 05 JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Destino:** `solqaryn/Solqaryn@dev`. **Control:** `PROJECT_SCOPE_LOCK=STRICT`. **Naturaleza:** contrato documental de aceptación; los ensayos indicados son FUTUROS_NO_EJECUTADOS, sin certificar el comportamiento global del automatizador.

## 1. Autoridad, fuentes y alcance

El [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), la [hoja institucional](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_STATUS.md`, `docs/AUTOMATION_AUTHORITY.md` y la Skill canónica gobiernan esta fase. Se relaciona con [M01.01](./c15-05-m01-01-alcance-no-regresion-auditoria-operativa-automatizaciones.md) (NR01–NR14) y [M01.02](./c15-05-m01-02-actores-interfaces-fronteras-auditoria-operativa.md) (A01–A12, IO01–IO12, F01–F10). M01.03/M01.04 pueden aportar inventario y riesgos posteriores, pero no se presume aquí su cierre, revisión independiente o resultados ejecutados.

El criterio de esta entrega es definir **qué se debe medir y demostrar** en un futuro ensayo de trazabilidad del plan, no declarar que las 2.340 unidades estén libres de huérfanos, colisiones, P0/P1 o defectos. PLAN_MAESTRO H/I determina estado y responsable; COLA G/O es prefiltro; DAG_TECNICO A/F/G valida dependencias; BITACORA A:Z preserva hechos; TAREAS_PROGRAMADAS AA:AN es sólo último autorreporte.

## 2. Formato obligatorio de una prueba de aceptación real

Toda prueba futura deberá incluir `CASE_ID`, unidad existente, evidencia previa de PLAN/COLA/DAG, TASK_ID del autor permitido, revisor A5 distinto, identidad/permisos de conexión, UTC inicio/fin, RUN_ID sólo cuando lo provea el scheduler, SHA base/publicado, archivos cambiados, prueba positiva/negativa con resultado observado, conclusiones del check obligatorio por `head_sha`, evento BITACORA/IDEMPOTENCY_KEY, readback posterior y rollback. Redactar datos personales y secretos. Una regla escrita o una captura de fórmula **no sustituye** el resultado ejecutado.

## 3. Criterios observables P01–P16 / N01–N16 — FUTUROS_NO_EJECUTADOS

| Caso | Positivo a acreditar en entorno autorizado | Negativo cuya presencia bloquea aceptación |
| --- | --- | --- |
| P01/N01 | Cada ID de PLAN tiene correspondencia única en COLA | Un duplicado crea dos propietarios o decisiones divergentes |
| P02/N02 | El DAG identifica el mismo ID y prerrequisitos comprobables | COLA O=SI con DAG ausente/NO se convierte en claim |
| P03/N03 | Sólo PENDIENTE sin owner y G/O/F/G válidos se reclama | Una unidad ocupada o bloqueada cambia de dueño sin CAS |
| P04/N04 | Dos reclamos concurrentes producen exactamente un ganador | Ambos actores escriben la misma unidad |
| P05/N05 | Un claim registra transición y readback de H/I | Un registro de intención se cuenta como LISTO |
| P06/N06 | BITACORA inserta 26 campos y claves idempotentes únicas | Un retry duplica evento o edita historia anterior |
| P07/N07 | Las auditorías A0–A4 usan artefactos y SHA causal | Un test de otro SHA certifica el nuevo cambio |
| P08/N08 | Un check obligatorio `completed/success` acredita ese SHA | `queued`, `in_progress`, `skipped`, `cancelled` o `failure` se contabiliza PASS |
| P09/N09 | A5 pertenece a una Task Javier distinta de la autora | La autora se autoaprueba o revisa una Task excluida |
| P10/N10 | `dev` avanza fast-forward con control HEAD | Una carrera sobre CHANGELOG sobrescribe viñetas ajenas |
| P11/N11 | CHANGELOG conserva cada encabezado con su propio cuerpo | Hay cabecera vacía o atribución cruzada |
| P12/N12 | Una Task failed se analiza junto a corrida posterior y leases | Un `completed` habilitado dispara rescue automático |
| P13/N13 | Una pausa inequívoca se trata con autorización y readback | Un humano pausó deliberadamente y un watchdog la reactiva |
| P14/N14 | Telemetría distingue señal local de run nativo | `FINALIZADA` auto-reportado se publica como estado físico actual |
| P15/N15 | JWT/RBAC/tenant A/B y secretos permanecen aislados | El frontend, un parámetro tenant o un log concede permisos backend |
| P16/N16 | Rollback sin force-push conserva auditoría y otros autores | Reversión borra commits, altera QA/main/PROD o borra BITACORA |

**Estado de los 32 oráculos:** `FUTUROS_NO_EJECUTADOS`. No se ha realizado una carrera simultánea sintética, un pentest, prueba integral de las 2.340 filas ni benchmark por la redacción de este documento.

## 4. Requisitos no funcionales NF01–NF12 — NO_MEDIDOS

| ID | Medición requerida posteriormente |
| --- | --- |
| NF01 | Integridad de referencias para 2.340 IDs y unicidad por hoja |
| NF02 | Reconciliación de H/I con G/O y DAG por ID exacto |
| NF03 | Consistencia bajo reclamaciones concurrentes y pérdida de conexión |
| NF04 | Tasa de eventos BITACORA únicos y reconstrucción de transiciones |
| NF05 | Latencia entre commit, check-runs y cierre sin inventar un máximo |
| NF06 | Reproducibilidad de fallos, reintentos y pruebas en SHA exacto |
| NF07 | Trazabilidad de autor y revisor A5 independientes |
| NF08 | Integridad de changelog y ausencia de sobrescritura entre commits |
| NF09 | Pérdida de señal, telemetría obsoleta y clasificación explícita |
| NF10 | Protección de PII, credenciales y contexto tenant A/B |
| NF11 | Capacidad de rollback sin pérdida de historial |
| NF12 | Evidencia de P0/P1 y aprobación de mitigaciones antes de release |

No se inventan cifras de latencia, cuota, cobertura ni porcentaje de disponibilidad. El umbral de **cero duplicados críticos y cero huérfanos** exige un censo ejecutado y conciliado; queda `NO_CERTIFICADO`. Un hallazgo P0/P1 no puede ocultarse por publicar estos criterios.

## 5. Bloqueos, responsabilidades y liberación

- M01.06 requiere predecesores completos, evidencia, DAG propio y revisión A5 independiente; un `VALIDANDO` provisional no libera un gate terminal.
- Si falla Scope Lock o alguna comprobación pertinente del commit, permanecer `VALIDANDO`; no asignar PASS a pruebas canceladas o no causales.
- Una ausencia de permiso, falla real de API o empate CAS impide escribir ese scope y permite evaluar otro independiente; no promueve un `NO` a `SI`.
- Una revisión documental no despliega webhooks, workers, tareas, MySQL, DNS, Hostinger/Cloudflare, ni modifica producción; QA/main/PROD requieren orden distinta.

## 6. Auditoría A0–A5 y rollback del documento

**A0:** comprobar estructura, 16 pares positivos/negativos, 12 NF, referencias, seguridad y ausencia de secretos. **A1:** comprobar la especificación de escenarios adversariales; sus pruebas permanecen no ejecutadas. **A2:** preservar fronteras de Task ID, JWT/RBAC, tenant A/B y no-autorrevisión; sin cambio runtime. **A3:** cotejar el Standard, PLAN/COLA/DAG y contratos M01.01–02; no atribuir los futuros test a la entrega. **A4:** publicación directa fast-forward con diff exclusivamente de este documento y sección CHANGELOG autocontenida; Scope Lock y V5 concluidos SUCCESS en SHA exacto y readback de estado/evento. **A5_PENDIENTE:** tarea Javier distinta de la autora debe revisar causalmente antes de cualquier aprobación independiente.

**Rollback técnico:** `git revert` normal después de verificar el HEAD compartido, sin force-push ni borrar registros. El rollback del documento no elimina hallazgos ni concede permisos comerciales.
