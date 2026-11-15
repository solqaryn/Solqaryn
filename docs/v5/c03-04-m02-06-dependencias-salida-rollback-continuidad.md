# SOLQARYN V5 — C03-04.M02.06: dependencias y contrato de salida para respuesta y recuperación

**Microtarea:** `C03-04.M02.06`. **Autor:** PRIMARY DEV 01 JAVIER_CHATGPT (`6adbb9fff1548191a1bd5b30d9e9984a`). **Fecha:** 2026-11-14 America/Tegucigalpa. **Rama:** `solqaryn/Solqaryn@dev`. **PROJECT_SCOPE_LOCK=STRICT**.

**Resultado estrictamente DOCUMENTAL:** cerrar contratos M02 para incidentes, revocaciones, backup, restauración y continuidad operativa, con dependencias, permisos, prueba futura, criterios para C10/C12, recuperación y rollback. No se ejecuta un backup, restore, revocación, incidente, migración, worker, API, webhook ni despliegue. Los escenarios son **FUTUROS_NO_EJECUTADOS**. No se certifican RPO/RTO reales, recuperación de datos, ausencia de P0/P1, ni seguridad productiva.

## 1. Autoridad y precondiciones reales

Fuente única: [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [Sheet institucional](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), Skill canónica, AGENTS, Scope Lock y autoridad dev. El preflight de este ID verificó PLAN_MAESTRO fila 553 H=PENDIENTE/I vacía, COLA fila 553 G/O=SI y DAG_TECNICO fila 514 F=VALIDADO/G=SI. Se registró CLAIM único de PRIMARY DEV 01 y su readback EN_PROGRESO. El gate específico exige `C03-04.M01.01–06+A5` y `C03-04.M02.01–05+A5` con controles de autoría/revisión distintos; no basta M02 en VALIDANDO.

| Antecedente inmutable de dev | Documento y blob Git | Contrato, no prueba real |
| --- | --- | --- |
| M02.01 | `docs/v5/c03-04-m02-01-entidades-ownership-invariantes-continuidad.md` · `faf1e21e16265d6ec4045b72c28cf71adb52096b` | Entidades conceptuales, ownership, doce entidades y dieciséis invariantes |
| M02.02 | `docs/v5/c03-04-m02-02-contratos-api-dto-errores-compatibilidad-continuidad.md` · `8896c890a4a7f15261bf7c07c335f3b95c0b1815` | Operaciones y validaciones futuras, sin endpoints |
| M02.03 | `docs/v5/c03-04-m02-03-persistencia-indices-migraciones-fixtures-continuidad.md` · `928a7b2e4d74dedf3751f25f990ac1424b31fcb0` | Persistencia conceptual, integridad y migración no ejecutada |
| M02.04 | `docs/v5/c03-04-m02-04-configuracion-tipada-ambitos-flags-continuidad.md` · `d0d3f0ff6dc43021e74364eca75a901f34ef0828` | Configuración propuesta, sin activar flags |
| M02.05 | `docs/v5/c03-04-m02-05-eventos-idempotencia-correlacion-limites-continuidad.md` · `e7d8e1c3224d1e12e168d60fa6f870c68ea2c1bf` | Eventos conceptuales EV01–14 sin publicación |

Los cinco artefactos permanecían idénticos a sus SHA originales durante la revisión. Sus A5 independientes se acreditaron mediante los eventos y PLAN!K de esta corrida; M02.01–04 son de PRIMARY DEV 05, M02.05 de PRIMARY DEV 06, y todas las revisiones de PRIMARY DEV 01. Los cinco commits originales modificaron exclusivamente Markdown y tuvieron Scope Lock/V5 exact-SHA SUCCESS; `CANCELLED` de suites runtime no se registró como PASS. La revisión no certifica ejecución de backups ni restore.

Como contexto técnico, `scripts/backup_dev.sh` y `scripts/restore_dev.sh` contienen restricciones de entorno, `.github/workflows/backup-restore-dev.yml` describe CI efímera, `docs/BACKUP_RESTORE_DESARROLLO_RUNBOOK.md` y `docs/ROLLBACK_RUNBOOK.md` contienen procedimientos. Su presencia **no** demuestra prueba ejecutada. Antes de cualquier invocación destructiva futura se exigen permiso, runner controlado, destino descartable, manifiesto, checksum, garantía tenant A/B y rollback. No ejecutar scripts en este entregable.

## 2. Contrato de salida de continuidad (CONT01–CONT14)

Cada gate define un oráculo positivo y contraprueba negativa que deberán ejecutarse en fases M03–M06; la evaluación actual es `NOT_EVALUATED_RUNTIME`, no `PASS`.

| Gate | Prueba positiva futura | Negativa que exige bloqueo |
| --- | --- | --- |
| CONT01 Incidente | Incidente trazable al ámbito/autor autorizado | ID de incidente de tenant B visible desde A |
| CONT02 Autorización | JWT y membresía actuales + RBAC por operación | Token revocado o permiso obsoleto inicia contención |
| CONT03 Evidencia | Correlación opaca, SHA, job exacto y redacción | JWT, secreto, PII o cadena de conexión en evidencia |
| CONT04 Revocación | Revocación invalida sesiones/consumidores afectados | Sesión antigua conserva acceso después de revocación |
| CONT05 Contención | Aprobador diferente autorizado, versión de decisión | Autorrevisión o rol UI concede permisos nuevos |
| CONT06 Backup | Manifest, integridad, cifrado, origen/dev e inventario medidos | Backup sin checksum, sin ownership o con destino ajeno |
| CONT07 Restore | Destino efímero autorizado, permiso destructivo previo | Restauración a Aiven/live o host arbitrario por parámetro |
| CONT08 Datos A/B | Restauración sintética conserva aislamiento por empresa | Filas, respaldos o claves de B aparecen en A |
| CONT09 Idempotencia | Un solo efecto con versionado/clave y reintentos finitos | Dos procesos restauran o revocan dos veces |
| CONT10 Orden operacional | Registro append-only y orden de estados auditable | Fallo borra trazas, timestamps o saltan gates |
| CONT11 RPO/RTO | Medición observada en runner con escenarios reproducibles | Documento declara objetivos alcanzados sin cronometrar |
| CONT12 CI causal | Checks pertinentes SHA exacto completed/success | SKIPPED/CANCELLED/IN_PROGRESS/FAIL contabilizado como PASS |
| CONT13 Rollback | Ensayo reversible con integridad y readback | Revert de código deja datos inconsistentes o filtra tenant |
| CONT14 Promoción | A0–A4, A5 ajena, autorización explícita por ambiente | LISTO_DEV o diseño activa QA/main/PROD sin autorización |

**Cierre documental de M02** significa que CONT01–14, autoridad, impacto, bloqueos y rollback son coherentes y rastreables. No demuestra que el sistema ya cumpla CONT01–14. Backup/restore/destrucción y revocación real permanecen fuera de alcance. Valores RPO/RTO aún requieren medición; no inventarlos.

## 3. Contrato de recuperación para C10, C12 y M03

La referencia comercial de C10/C12 sólo consume **contratos y oráculos**, nunca permisos operativos implícitos:

- **C10 almacenamiento y medios:** no confundir objeto legado con archivo migrado; exigir inventario real, hash/origen, acceso firmado, caducidad/owner/tenant y reversión sin borrado silencioso. El diseño de continuidad no ordena migrar R2 ni Cloudinary.
- **C12 frontend separado:** restauración y revocación deben presentarse con errores seguros, estados vacíos/reintentos finitos y ningún dato sensible en Angular. El frontend no puede decidir membresía ni activar restore, con pruebas accesibles posteriores.
- **M03.01 Domain:** reglas de incidente, versionado de estado, propiedad y autorizaciones, pruebas positivas/negativas con tenant sintético.
- **M03.02 Application:** orquestación de permiso por operación, clasificación de incidente, denegación ante permisos faltantes, trazabilidad de compensación.
- **M03.03 Seguridad:** JWT/RBAC, membresía efectiva, aislamiento tenant A/B y audiencias exactas con tests server-side negativos.
- **M03.04 Persistencia:** MySQL 8.4 efímero para estructuras reales y recuperación reversible, migración y rollback comprobados; nunca Aiven live por documento.
- **M03.05 Integración:** workers/proveedores/eventos sólo si otra autorización real existe; cuota, bloqueo atómico, idempotencia y política de reintento con CI dirigida.
- **M03.06 Observabilidad:** logs minimizados, retención, correlación, error seguro y evidencia original de job/SHA.

Cada M03 debe verificar PLAN/COLA/DAG del ID exacto, exclusividad de owner, capas afectadas, predecesores LISTO+A5 y CI real. C03-04.M02.06 en VALIDANDO **no** habilita sus sucesoras técnicas, y LISTO_DEV documental no sustituye A5 independiente.

## 4. Paquete futuro de evidencia y estados

Evidencia requerida por escenario: `microtask_id`, `case_id`, `environment`, `source_sha`, `head_sha`, `check_run_id`, `operator_task_id`, `reviewer_task_id`, `tenant_fixture`, `expected_denial`, `observed_result`, `manifest_checksum`, `backup_origin`, `restore_destination`, `rpo_rto_measurements` y `rollback_proof`. Todo se redacta y se protege por permisos. Es **estructura documental**, no tabla ni pipeline instalado.

| Estado | Regla mínima |
| --- | --- |
| NOT_EVALUATED | Documento describe prueba; ejecución o permiso ausentes |
| IN_PROGRESS | Prueba/CI activa; conservar VALIDANDO sin PASS |
| FAIL | Contraprueba revela falla, check obligatorio no exitoso o permiso insuficiente |
| PASS_DEV | Test real causal con checks success exact-SHA, A0–A4 y readback |
| A5_PASS | Otro TASK_ID Javier revisa SHA, diff, evidencias y controles |
| BLOCKED_RELEASE | No hay autorización específica por ambiente o hay hallazgo crítico |

Ningún identificador de BITACORA sustituye el run nativo; un timestamp autorreportado no prueba actividad de Task externa, y las lecturas/escrituras de Sheets sin CAS atómico no garantizan exclusión distribuida. La única bitácora A:Z conserva historia y los cambios se agregan por EVENT_ID único.

## 5. Recuperación y rollback (RB01–RB10)

| Gate | Acción de contención | Evidencia posterior |
| --- | --- | --- |
| RB01 Scope Lock FAIL | No publicar cambio inseguro; reparar causa en dev | Nuevo SHA y Scope Lock SUCCESS |
| RB02 CI pendiente/cancelled | Mantener VALIDANDO y otra unidad independiente | Prueba causal completed/success, no skipped |
| RB03 Tenant A/B | Denegar efecto y revisar filtros/owner/permiso | Retest cruzado sintético |
| RB04 JWT/RBAC | Rechazar sesión revocada o rol indebido | Test negativo exact-SHA de revocación |
| RB05 Incidente/contención | Aislar scope, preservar traza y aprobación | Reconciliación de estado y decisiones |
| RB06 Backup corrupto | No restaurar, retener evidencia y checksum | Manifest/integridad medidos en destino permitido |
| RB07 Restore fallido | Detener impacto, no tocar prod; revert en sandbox | Conteos/consistencia y rollback verificados |
| RB08 Evento duplicado | No habilitar productor; exigir efecto idempotente | Test de replay y un solo ganador |
| RB09 Carrera Git/owner | Abort de escritor perdedor, no force-push | HEAD y PLAN/BITACORA readback |
| RB10 Error documental | Git revert normal del changeset y evento aditivo | Nuevo commit/CI Scope Lock y V5 |

No confundir rollback de Markdown con recuperación real de base de datos; nunca alterar BITACORA histórica.

## 6. Auditorías A0–A5

**A0:** diff restringido a este Markdown y sección autocontenida de CHANGELOG_AI; precedentes M01/M02 y diez RB, catorce CONT, cero secretos, sin encabezados vacíos. **A1:** positivos/negativos futuros especificados de forma falsable; no test runtime ejecutado. **A2:** seguridad estática de JWT/RBAC/tenant A/B, revocación y rollback, sin ejecutar acciones. **A3:** verificar STANDARD, PLAN/COLA/DAG y cinco blobs Git exactos de M02, con C10/C12 como dependencias de diseño, no despliegue. **A4:** publicar fast-forward/CAS sobre HEAD actual de dev, verificar Scope Lock y V5 completed/success del SHA exacto, diff/blobs y readback PLAN/BITACORA. **A5:** pendiente de Task Javier distinta de PRIMARY DEV 01, nunca autoaprobación. Ninguna A5 documental certifica restore, disponibilidad, RPO/RTO ni autoriza QA/main/PROD.

**Rollback inmediato de esta entrega:** revert normal del commit exclusivamente documental en dev, con evento compensatorio, sin force-push, scripts destructivos ni modificación de datos vivos.
