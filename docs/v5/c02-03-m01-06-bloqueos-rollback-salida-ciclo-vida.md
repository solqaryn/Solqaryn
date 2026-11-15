# SOLQARYN V5 FINAL — C02-03.M01.06: bloqueos, rollback y condición de salida del ciclo de vida tenant

**Autor:** PRIMARY DEV 04, JAVIER_CHATGPT, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Rama:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-15.

**Fuentes institucionales:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA; Skill canónica y `AGENTS.md`; `docs/v5/c02-03-m01-01-objetivo-ciclo-vida-provisionamiento.md` hasta `docs/v5/c02-03-m01-05-criterios-aceptacion-lifecycle.md`.

**Clase de entrega:** preparación **exclusivamente documental** para cerrar M01 y definir entrada condicional a diseño M02. No crea alta, suspensión, reactivación, archivo ni baja lógica de empresas en runtime. No ejecuta MySQL, migraciones, API, worker, cambio de roles, borrado físico, restore, QA/main/PROD ni cuenta nueva.

## 1. Baseline real y límites de autoridad

El código existente aporta `Empresa.Activa`, `Activar`, `Desactivar` y `Empresa.Eliminado => false` marcado NotMapped. La última propiedad no demuestra baja lógica disponible; **ningún archivado ni eliminación lógica de Empresa debe declararse implementado**. `EmpresasController`, `PlataformaEmpresasController`, `EmpresaService`, `UsuarioEmpresa`, `ContextoTenantActual` y `UsuarioScopeService` aportan funciones delimitadas de empresa, membresía y autorización; no constituyen por sí mismos un motor universal de lifecycle idempotente.

Estados futuros propuestos en M01.01–05: SOLICITADO, PROVISIONANDO, ACTIVO, SUSPENDIDO, ARCHIVADO, BAJA_LOGICA y FALLA_REPARABLE, sujetos a revisión de compatibilidad y migración. Los 24 criterios AC01–AC24 de M01.05 son **FUTUROS_NO_EJECUTADOS** (seis positivos y dieciocho negativos); no permiten afirmar altas, archivos o reactivaciones ejecutadas.

## 2. Dependencias institucionales y condición M01

El DAG exacto de `C02-03.M01.06` exige C01-01.M01.01–05, C01-02.M01.01–05, C01-03.M01.01–05, C01-04.M01.01–05, C02-02.M01.01–05, C02-03.M01.01–05+A5 y C02-02.M01.06+A5. Preflight de esta unidad: PLAN H367=PENDIENTE, I367 vacío, COLA G367/O367=SI, DAG F/G=VALIDADO/SI. La preparación C02-03.M01.01–05 ya estaba LISTO con A5 independiente según readbacks y C02-02.M01.06 quedó LISTO+A5 de otra Task. Esto habilita **sólo redactar este contrato**, no eliminar validaciones, activar M02 por anticipación o emitir permisos técnicos.

## 3. Gates de bloqueo G01–G16

| ID | Causa concreta de NO_GO | Evidencia mínima para reconsiderar |
| --- | --- | --- |
| G01 | Predecesor no LISTO o A5 no independiente | PLAN H/K, BITACORA A:Z, DAG recalculado e identidad de autor/revisor |
| G02 | Scope ocupado por otro owner o lease incierta | Exclusión verificada y control de concurrencia real, nunca takeover por reloj |
| G03 | COLA G/O no SI o DAG F/G no VALIDADO/SI | Analizar causa de fórmula/estado sin forzar puerta |
| G04 | Scope Lock o procedencia CHANGELOG no exitosa en SHA | Check nativo completed/success con head_sha exacto |
| G05 | Prueba causal requerida fallida, cancelada, omitida o en curso | Reparación/CI exact-SHA; no sustituir con pruebas ajenas |
| G06 | Usuario de empresa pretende actuar como operador global | JWT/membresía y RBAC de plataforma diferenciados |
| G07 | Empresa A opera estado de empresa B | Denegación efectiva server-side y prueba cross-tenant |
| G08 | Alta repetida duplica empresa/membresía/host | Registro idempotente durable y prueba de concurrencia |
| G09 | Suspensión deja caches/sesiones/jobs con permiso vigente | Revocación comprobada en API, jobs y consumidores |
| G10 | Reactivación resucita permisos o sesiones revocadas | Nueva validación de membresía, rol, credenciales y versión |
| G11 | Archivado o baja lógica se confunde con DELETE/DROP | Política de retención, auditoría y diseño de soft-delete real |
| G12 | Error parcial deja tenant falso activo o recursos huérfanos | Checkpoint, compensación y ensayo de reanudación |
| G13 | Retry usa mismo key con payload distinto | Conflicto seguro sin escritura ni autorización |
| G14 | Log, evento o DTO expone JWT, DSN, email/PII o secreto | Revisión de redacción y tests negativos |
| G15 | Rollback sin snapshot/restore/retención ni target verificable | Prueba de recuperación efímera con integridad por empresa |
| G16 | Se intenta QA/main/PROD, datos reales o proveedor sin orden | Autorización separada y gates de ambiente; no basta M01 LISTO |

**Clasificación:** G01–G05 son gobierno y evidencias; G06–G16 seguridad/operación futura. El documento verifica que estos gates estén **definidos**, no que la implementación los haya superado.

## 4. Recuperación y rollback R01–R08

| ID | Incidente | Respuesta futura o documental |
| --- | --- | --- |
| R01 | Error en esta publicación Markdown/CHANGELOG | `git revert` normal en dev, conservar BITACORA y readback |
| R02 | Alta parcial sin membresía o configuración | Estado FALLA_REPARABLE, transacción/compensación ensayadas antes del uso real |
| R03 | Suspensión tardía deja permisos stale | Fail-closed en API/jobs, invalidar caché/sesión y auditar |
| R04 | Reactivación con grants revocados | Denegar y solicitar nueva verificación de autorización |
| R05 | Archivado con retención contable no resuelta | Bloquear transición y conservar documentos/datos |
| R06 | Idempotencia/replay o carrera entre dos operadores | CAS/versión, relectura y un solo resultado durable |
| R07 | Copia/backup/restauración no íntegra | Abortar operación, no usar destino y probar restore efímero |
| R08 | Incidente de fuga entre tenants o de secretos | Denegar, contener, sanear y auditar con personal autorizado |

R01 es el único rollback aplicable **en esta microtarea documental**. R02–R08 exigen futuras implementaciones, fixtures, permisos y pruebas; no implican que exista un worker o saga productivos.

## 5. Aceptación de este contrato y límites futuros

| Prueba documental | Criterio positivo | Negativa |
| --- | --- | --- |
| DOC01 | Reconoce estados propuestos y funciones actuales específicas | Confundir `Empresa.Eliminado=false` NotMapped con baja lógica implementada |
| DOC02 | Referencia los cinco artefactos M01 y A5 causal de los antecedentes | Dar por aprobado A5 propio o un VALIDANDO provisional |
| DOC03 | Enumera G01–G16 con causa y prueba futura | Usar PLAN o texto como PASS de runtime |
| DOC04 | Describe R01–R08 y distingue reversión Git de datos | Afirmar que `git revert` restaura MySQL |
| DOC05 | Separa AC01–AC24 FUTUROS_NO_EJECUTADOS | Llamar xUnit/MySQL/HTTP a oráculos aún escritos |
| DOC06 | Preserva JWT/RBAC/tenant A/B, retención y secreto no expuesto | Autorizar QA/main/PROD, DROP, provider o nuevos permisos |

Los 24 AC de M01.05 cubren alta, suspensión, reactivación, archivo, idempotencia, fallos parciales, revocación, tenant A/B, datos/retención y CI exact-SHA. Su ejecución real corresponde a M03/M04/M05 y requiere CI .NET10, EF/MySQL8.4 efímera, pruebas negativas de autorización y fallos, más readbacks nativos.

## 6. Protocolo proporcional A0–A5 y handoff

**A0:** revisar fuente V5, cinco documentos de preparación y diff de solo dos Markdown con sección de CHANGELOG contigua. **A1:** comprobar DOC01–DOC06 y los 16 gates + 8 planes de recuperación, sin inventar implementación. **A2:** separar grant plataforma y RBAC empresarial, tenant A/B, revocación, idempotencia y retención; zero secretos. **A3:** comprobar DAG C02-03.M01.06 y predecesores LISTO+A5, correspondencia de condiciones AC01–AC24. **A4:** commit fast-forward en dev y SHA/diff/blobs exactos; checks `verify-solqaryn-project-scope` y V5 procedencia completed/success sobre commit publicado; PLAN/COLA/BITACORA readback. Build/tests backend, frontend y MySQL se anotan NO_APLICA al diff exclusivamente documental, nunca PASS. **A5:** sólo otra de las cinco Tasks Javier, diferente del autor, valida SHA, contrato, pruebas documentales, seguridad y readbacks antes de liberar sucesoras que requieran A5.

**Siguiente etapa:** M02 sólo después de LISTO_DEV de este ID, A5 independiente, y nueva consulta PLAN H/I, COLA G/O y DAG F/G para su ID exacto. Ninguna autorización se deriva de la salida documental para migración, operación global, QA/main/PROD, servicios de pago o datos vivos.

**Rollback real de este artefacto:** `git revert` normal en `dev` sin rewrite/force y nuevo check de Scope Lock; preservar autoria y BITACORA histórica.
