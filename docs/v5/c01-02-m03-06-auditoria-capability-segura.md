# C01-02.M03.06 — auditoria estructurada segura, dev V19

## A0 — alcance y riesgo R2
C0102SafeCapabilityAuditReader es un envoltorio de lectura en proceso del adaptador aislado C0102ReadThroughCapabilityProvider. Emite EventId constantes para entrada invalida, evidencia no confiable o ausente y fallo opaco. Sin registros de tenant, usuario, recurso, capacidad, claims, exception ni mensajes secretos. No se registra en DI live ni en API/JWT/DB; no worker, cola o efectos. Esto es observabilidad aislada verificable y NO certificacion de auditoria durable productiva. Requiere no filtrar informacion al mecanismo de logging.

## A1 — pruebas adversariales con capacidad de fallar
Seis hechos xUnit: (1) IDs invalidos denegados sin llamadas y sin fugas, (2) evidencia positiva se registra UNTRUSTED y no se eleva autoridad, (3) error de fuente con secreto omitido sin excepcion, (4) cambios entre lecturas y revocacion sin cache de ALLOW, (5) cancelacion propagada sin false audit, (6) dependencias nulas rechazadas.

## A2 — seguridad y aislamiento
El adaptador delega la validacion tenant A/B, actor, rol, sucursal y revocacion al proveedor aislado existente, y no promueve ninguna bandera de persistencia/RBAC. Logger escribe exclusivamente textos estaticos; no loguear exception ni informacion tenant. No inferir que los logs conceden permiso. El proveedor no esta conectado a infraestructura real.

## A3 — integracion
Build Release y xUnit reales en GitHub-hosted .NET10 del SHA; fuentes externas, MySQL, JWT real, navegador, servidor HTTP y log exporter NO_APLICA: este scope no los modifica ni ejecuta. El logger inyectado se prueba mediante ILogger en memoria.

## A4/A5 — gates
Revalidar Scope Lock, V5, seguridad multiempresa y CI dirigida 6/6; QUEUED/IN_PROGRESS/CANCELLED/SKIPPED NO PASS. Registrar SHA/RUN y readback Sheets/BITACORA. Mantener VALIDANDO hasta SUCCESS causal o equivalencia auditada. A5 solo otra Task Javier distinta de autor DEV05. Sin QA/main/PROD. Rollback git revert normal, sin alterar historia ni fuentes institucionales.
