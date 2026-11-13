# SOLQARYN V5 FINAL — C02-03.M01.05: criterios de aceptación observables del ciclo de vida tenant

**Tipo 1 — sólo diseño documental.** Especifica criterios falsables para una implementación FUTURA de altas, suspensiones, reactivaciones, archivado y baja lógica de tenants con idempotencia, fallo parcial y recuperación. **No implementa ni certifica un lifecycle manager, workers, base dedicada, endpoint, operación real, migración o despliegue.** Autor de preparación: PRIMARY DEV 03 JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. Fuente única: Standard V5 FINAL Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` y Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. Scope Lock STRICT, dev solamente, sin QA/main/PROD.

## 1. Condiciones observables para el diseño futuro

- **Autorización**: grant de plataforma explícito validado por backend, distinto de RBAC empresarial, en cada transición; membresía activa no implica permiso de suspender/borrar otras empresas.
- **Estados propuestos**: SOLICITADO, PROVISIONANDO, ACTIVO, SUSPENDIDO, ARCHIVADO, BAJA_LOGICA y FALLA_REPARABLE. Ninguna transición es creada en este documento; corresponde confirmar si son compatibles con entidades y migración antes de implementar.
- **Operación idempotente**: identidad de tenant, tipo de comando, clave de idempotencia, huella de payload, versión/fencing y resultado auditable persistido. Repetir con misma clave y mismo payload debe devolver estado final coherente sin nuevos efectos.
- **Consistencia**: cada transición evita duplicar usuarios/roles, conserva datos legítimos y retención, revalida revocación/sesiones/caché; no borra físicamente bases ni documentos por confundir baja lógica.
- **Fallos**: el rechazo seguro, rollback o checkpoint reparable debe ser reproducible en ambiente de pruebas descartable. No prometer rollback total si un efecto externo irreversible no tiene compensación aprobada.
- **Evidencia requerida futura**: build/test .NET 10, pruebas MySQL 8.4/EF si cambia persistencia, escenarios A/B RBAC/JWT, fallos parciales, replay y observabilidad sobre SHA exacto. Los pipelines históricos no certifican implementaciones nuevas.

## 2. Matriz de aceptación funcional y técnica FUTURA (nada ejecutado aquí)

| ID | Caso | Estímulo/condición | Resultado verificable obligatorio, cuando exista implementación |
| --- | --- | --- | --- |
| AC01 | POS | Operador de plataforma con grant real solicita alta de empresa nueva con key única | Una empresa creada tras transacción, membresía y auditoría coherentes; respuesta estable |
| AC02 | POS | Operador suspende empresa A activa y vuelve a consultar su estado | SUSPENDIDO visible y escritura privada denegada por API y worker |
| AC03 | POS | Operador reactivador autorizado confirma requisitos de seguridad y membresía | ACTIVO sólo tras revalidación y registro de decisión/versionado |
| AC04 | POS | Empresa archivada bajo política de retención verificada | ARCHIVADO sin pérdida física de datos legítimos ni permisos residuales |
| AC05 | POS | Alta o suspensión repetida con misma clave y payload | Se observa un solo efecto durable y misma respuesta/versión lógica |
| AC06 | POS | Fallo transitorio de job tras checkpoint válido y autorización de reintento | Recuperación determinista sin duplicar registros, roles, eventos o archivos |
| AC07 | NEG | Administrador de empresa A solicita modificar estado de B | Denegación backend, sin modificaciones ni revelación de metadatos de B |
| AC08 | NEG | Token expirado, revocado o JWT con tenant distinto | Rechazo sin alcanzar persistencia/worker |
| AC09 | NEG | Cliente suministra `EmpresaId`, `host`, `DSN`, `placement` ajenos | Nunca convertirse en autoridad de tenant, destino o conexión |
| AC10 | NEG | Misma clave idempotente con payload diferente o tenant distinto | Conflicto verificable, sin reutilizar resultado de otro comando |
| AC11 | NEG | Dos operaciones concurrentes SUSPENDER/REACTIVAR sobre A | Fencing o CAS evita orden ambiguo y registra decisión reproducible |
| AC12 | NEG | Intento de escribir recurso privado tras SUSPENDIDO/ARCHIVADO | Rechazo también en API, repositorio, jobs y adaptadores pertinentes |
| AC13 | NEG | Reactivación con membresía eliminada o rol obsoleto | Rechazo y nueva revisión de grants, sin resucitar sesiones antiguas |
| AC14 | NEG | Baja lógica propuesta con obligaciones contables/retención sin resolver | Bloquear transición y preservar filas, FK, adjuntos, logs legítimos |
| AC15 | NEG | Acción de baja intenta `DROP DATABASE` o eliminación física | Denegación por política; la baja lógica jamás equivale a borrado real |
| AC16 | NEG | Worker de A recibe evento malformado con contexto de B | Sin efectos en B; input se rechaza y se audita en forma segura |
| AC17 | NEG | Se interrumpe alta entre usuarios, permisos y storage | Fallo reparable con checkpoint, compensación y sin side effects no auditados |
| AC18 | NEG | Mensaje duplicado llega después del procesamiento exitoso | No segunda ejecución, idempotencia durable y correlación con primera |
| AC19 | NEG | Log/DTO/error contiene JWT, contraseña, email completo o DSN | Sin fuga de secretos/PII, salida redactada, prueba de sanitización |
| AC20 | NEG | Cache/refresh tardío del estado anterior reautoriza empresa suspendida | Nueva consulta de versión/estado invalida permisos anteriores |
| AC21 | NEG | A5 se firma con TASK_ID igual al del autor o sin evidencia positiva/negativa | Rechazo y evento válido A:Z; no promocionar QA/main/PROD |
| AC22 | NEG | CI de SHA anterior o check obligatorio SKIPPED se anuncia SUCCESS | Rechazar A0–A4 hasta evidencia exact-SHA correspondiente |
| AC23 | NEG | Falla de rollback/restore o checksum/índice no coincide | Rechazar liberación del lote, conservar estado seguro y abrir corrección |
| AC24 | NEG | Se intenta liberar M01.06/M02 sólo por cerrar documentación M01.05 | No liberar: respetar DAG, ciclo completo, A5 causal y pruebas reales |

**Clasificación:** seis positivos (AC01–AC06) y dieciocho negativos (AC07–AC24). Son diseños de prueba que deberán ejecutarse sobre una implementación futura; en esta microtarea no hay pruebas .NET, MySQL, HTTP ni E2E realizadas y no se presenta PASS sintético.

## 3. Fuentes Git reales cotejadas; límites de inferencia

| Fuente existente en dev | Blob SHA verificado | Relevancia / límite |
| --- | --- | --- |
| `backend/src/Domain/Entities/Empresa.cs` | `c44940df7c30983a4c23b93338e0cb9367962069` | Modelo actual de empresa; no demuestra transiciones universales |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | Membresía y rol empresa; no concede grant plataforma |
| `backend/src/Domain/Security/ContextoTenantActual.cs` | `7377d2d752985cc9baf31d08280f38ad94a6a114` | Contexto tenant desde membresía real, no job lifecycle |
| `backend/src/Application/Interfaces/StorageTenantContext.cs` | `716e8e205502d6300f555947c79edc2852fd63ba` | Prefijo tenant para storage, no autorización de baja física |
| `backend/src/Infrastructure/Services/UsuarioScopeService.cs` | `14f4acbc5e1dc6a3ed1cf3485d54c7f1121b0e6f` | Autorización server-side, sin certificación de transición universal |

Revisar también la preparación M01.02 y el registro M01.04 publicados para conservar trazabilidad entre actores, riesgos y criterios. Su existencia no sustituye una revisión de implementación.

## 4. Dependencias y frontera de liberación

`DAG_TECNICO` F=`VALIDADO`, G=`SI` para C02-03.M01.05, exclusivamente preparatoria documental M01.01–05, con diecinueve predecesores: C00-01.M02.01–06 (6), C00-03.M01.01–05 (5), C01-01.M01.01–05 (5) y C02-02.M01.01–03 (3). PLAN_MAESTRO H/I/EVIDENCIA y COLA E/G/O deben concordar. El resultado V5 es proponer pruebas para alta/suspensión/reactivación/archivo/baja lógica idempotentes; NO implementación, despliegue ni cierre de fase. No liberar M01.06, M02, qa/main/PROD por una fórmula o revisión documental aislada. Si cambia un predecesor o aparece A5_FAIL, revalidar antes del siguiente gate.

## 5. Auditorías proporcionales A0–A5

**Clasificación:** `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO` porque sólo cambian documentación y CHANGELOG; `requiere_security=SI_DOCUMENTAL` por JWT/RBAC/tenant A/B, idempotencia, PII y retención; `requiere_scope_lock=SI` para commit documental a dev.

- **A0:** diff de dos archivos, formato de matriz, secretos, referencias, sin dependencia o archivo ejecutable añadido.
- **A1:** comprobar 24 criterios AC01–AC24, 6 positivos /18 negativos futuros, estímulo, resultado verificable y que no se confundan con tests ejecutados.
- **A2:** revisar fronteras de membresía/rol/empresa, denegación entre A/B, revocación, sesiones, retención y fail-closed en errores.
- **A3:** cotejar cinco fuentes Git y blobs, DAG19/19 LISTO, Standard V5, PLAN/COLA, BITACORA de 26 campos; no inventar MySQL o HTTP live.
- **A4:** tras commit directo fast-forward dev comprobar SHA exacto, blobs, diff, `verify-solqaryn-project-scope` completed/success para ese SHA, readback institucional y plan de reversión.
- **A5:** revisión de otra Task con identidad distinta, evento A5_PENDIENTE con EVENT_ID/IDEMPOTENCY_KEY únicos previo a LISTO; no autocertificación ni promoción.

**No-alcance:** no editar Domain/Application/API/Infrastructure, datos de empresa, EF/MySQL, roles/JWT/BCrypt, integraciones, secretos, runners/CI, Cloudinary, proveedores, recursos físicos, QA/main/PROD ni Tasks/horarios. Inventario de imágenes históricas vacío: migración NO_APLICA. No crear PR, ramas o nueva microtarea 2341.

**Rollback:** git revert aditivo y auditable del commit documental tras revalidar HEAD y dependencias; sin reset, force-push, pérdida de cambios ajenos ni automatización de deploy. Revalidar este contrato antes de implementar y cuando existan decisiones finales de retención, autorización o idempotencia.
