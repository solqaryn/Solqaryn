# SOLQARYN V5 FINAL — C01-04.M01.01: objetivo, no-alcance y no-regresión del expediente universal

**Tipo:** preparación DOCUMENTAL M01.01, no implementación. **Fuente ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL y PLAN_MAESTRO/COLA/DAG_TECNICO institucionales. **Resultado de C01-04:** unificar cronología, decisiones, observaciones, adjuntos y versiones; evidencia mínima futura: contrato de timeline y consulta aislada por ámbito. Esta microtarea únicamente delimita el objetivo y sus condiciones de no-regresión. No certifica pantallas, endpoints ni datos de expediente.

## 1. Objetivo y límites de la unidad

Definir un **expediente y seguimiento universal** que, cuando sea diseñado e implementado por sus fases posteriores, permita asociar eventos de una misma entidad de negocio, ordenarlos por tiempo y relacionar decisiones, observaciones, documentos adjuntos y cambios de versión con actor y contexto. Debe conservar el historial sin confundir una proyección de timeline con la fuente transaccional que lo originó. La consulta futura deberá aislar empresa, sucursal o recurso según su propietario real y el permiso de la operación.

**Salida atómica de M01.01:** objetivo verificable, exclusiones expresas e invariantes falsables de no-regresión. M01.02 inventariará actores y superficies; M01.03 inspeccionará contratos y código; M01.04 evaluará riesgos/decisiones; M01.05 fijará aceptación. Este documento NO completa esos otros IDs ni libera M01.06/M02/M03.

## 2. Futuro modelo de experiencia, sin afirmar implementación

| Concepto | Intención funcional a validar en M02+ | Límite de confianza y trazabilidad |
| --- | --- | --- |
| Expediente | Agrupar referencias de un recurso propio sin alterar la entidad comercial original | Dueño/ámbito resuelto por el backend; referencias huérfanas o ambiguas se deniegan |
| Entrada de timeline | Mostrar secuencia auditable de hechos, decisiones, cambios y estados | Diferenciar momento de ocurrencia, persistencia y visualización; no inventar eventos |
| Decisión / observación | Registrar autor y alcance, con historial de cambios cuando corresponda | No reemplazar aprobación formal, roles efectivos ni evidencia de origen |
| Adjunto | Vincular archivo por referencia autorizada, metadatos y versión | URL firmada y permisos en servidor cuando exista almacenamiento real; sin publicar PII ni asumir archivos históricos |
| Versiones | Mantener relaciones anterior/siguiente y motivo; evitar pérdida de evidencia | No reescribir historia ni borrar eventos ajenos para resolver conflictos |
| Consulta | Filtrar cronología por recurso, ámbito y permisos con paginación | El frontend no determina acceso por slug, empresa, cookie, cabecera o ID arbitrario |
| Integración | Relacionar eventos autorizados desde módulos existentes o futuros | Idempotencia, correlación y límites de reproceso pertenecen al contrato posterior |

Una futura vista Angular sólo presenta datos ya autorizados; el backend .NET 10 valida JWT, membresía y RBAC efectivo y la persistencia EF/MySQL conserva las fronteras de empresa/sucursal. Ningún concepto anterior afirma que exista una ruta, tabla, DTO, worker o integración de expediente en dev.

## 3. Fuera de alcance explícito

Esta entrega **no crea ni modifica** endpoints, controladores, DTO, entidades, API pública, formularios, rutas Angular, JWT, RBAC, BCrypt, servicios, EF Core, MySQL, índices, migraciones, buckets, Cloudinary, R2, workers, colas, eventos reales, archivos, permisos, usuarios o bases de datos. No habilita un timeline en producción ni prueba E2E, API o MySQL; no migra adjuntos que no se hayan inventariado.

No concede privilegios globales, no adopta un tenant desde parámetros del navegador, no convierte observaciones en decisiones aprobadas, no borra versiones ni eventos legales, no revela secretos ni PII y no autoriza cambios de dominio/infraestructura, servicios pagos, QA/main/PROD o cronogramas de Tasks.

## 4. Invariantes de no-regresión para la futura implementación

| Caso | Entrada / situación falsable | Resultado exigido y evidencia posterior |
| --- | --- | --- |
| NR-01 (+) | Miembro autorizado de empresa A consulta expediente de recurso A | Sólo eventos y adjuntos A autorizados, orden estable y paginación; prueba real API/DB cuando exista |
| NR-02 (+) | Actor con permiso agrega observación a un expediente propio | Se atribuye autor, tiempo, ámbito y versión sin mutar hechos previos; readback auditado |
| NR-03 (+) | Vista recibe actualizaciones concurrentes legítimas | No pierde ni duplica entradas y declara un orden determinista; pruebas de concurrencia/idempotencia |
| NR-04 (-) | Usuario A fuerza EmpresaId, SucursalId, slug, header o URL para expediente B | Denegación 401/403/404 según contrato; cero filas/adjuntos B expuestos o modificados |
| NR-05 (-) | Token inválido, membresía revocada o rol empresarial intenta expediente de plataforma | Denegación server-side, sin permiso heredado por cache/claims/frontend |
| NR-06 (-) | Referencia a adjunto de otro expediente o URL firmada vencida | Sin lectura ni metadatos sensibles; autorización reevaluada por recurso/tenant |
| NR-07 (-) | Orden de escritura o versión obsoleta provoca conflicto | Error controlado o reconciliación explícita; nunca pérdida silenciosa de historial |
| NR-08 (-) | Mensaje malicioso, HTML/script, enlace peligroso o payload excesivo | Texto seguro, validación y límites; no ejecución/inyección ni filtración de PII |
| NR-09 (-) | Evento duplicado, webhook repetido o correlación equivocada | No duplica hecho material; distingue operación nueva de reintento idempotente |
| NR-10 (-) | Cambio de tenant/logout mientras se resuelve solicitud asíncrona | No muestra datos A en B y descarta respuesta fuera de contexto |
| NR-11 (control) | Error de proveedor de adjuntos, consulta parcial o rollback | Falla de forma segura y recuperable; sin borrar datos comerciales o alterar la cronología fuente |
| NR-12 (control) | Revisión A5 por la misma Task, evidencia de otro SHA o BITACORA incompleta | Rechazo de autoauditoría y trazabilidad; BITACORA A:Z (26 campos) con EVENT_ID e IDEMPOTENCY_KEY únicos |

Los casos NR-01–12 son **criterios diseñados**, no pruebas de runtime superadas. Para esta microtarea documental, la prueba positiva es la presencia coherente de un objetivo y una ruta de consulta A autorizada; la prueba negativa es comprobar que ningún texto confiere permiso implícito ni afirma un endpoint/adjunto implementado. Todo comportamiento de API, DB y browser queda condicionado a entregas ejecutables futuras.

## 5. Dependencias, verificación y liberación

La puerta DAG de `C01-04.M01.01` exige 13 preparatorias: `C00-01.M02.01–06`, `C00-03.M01.01–05` y `C01-01.M01.01–02`, todas LISTO en el readback previo a esta unidad; DAG_TECNICO F=VALIDADO/G=SI, PLAN_MAESTRO H=PENDIENTE y COLA G=SI/O=SI antes de reclamar. La continuación es únicamente preparación M01.01–05, sin adelantar M01.06, M02, IAM, tenancy ejecutable, QA/main/PROD o un cierre de fase.

**A0**: revisar diff documental, integridad de fuentes, ausencia de secretos y efectos colaterales; **A1**: verificar objetivo, exclusiones y criterios positivos/negativos NR; **A2**: revisar límites JWT/RBAC/tenant A/B, PII y archivos, *sin* simular pruebas de seguridad runtime; **A3**: verificar contrato V5, DAG y coherencia con arquitectura, `NO_APLICA` para builds .NET/Angular, MySQL/Docker y E2E porque no se modifica ejecución. **A4**: después del commit fast-forward en dev, comprobar SHA, blobs, diff y Scope Lock pertinente, COLA/PLAN/BITACORA. **A5**: pendiente para Task diferente antes de cualquier promoción.

**Rollback:** un `git revert` normal del commit documental publicado, previa relectura de dev, sin force-push ni borrado histórico. No revertir registros institucionales de auditoría: si se requiere corrección, agregar evento compensatorio idempotente y conservar la historia.