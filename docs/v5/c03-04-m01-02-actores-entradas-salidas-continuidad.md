# SOLQARYN V5 FINAL — C03-04.M01.02: actores, entradas, salidas y superficies de continuidad

**Tipo:** preparación estrictamente DOCUMENTAL. **Autor:** PRIMARY DEV 01 — JAVIER_CHATGPT, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Repositorio y rama:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-13.

**Autoridad única:** [Standard V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). El DAG de `C03-04.M01.02` está VALIDADO, puerta SI, y exige 15 antecedentes `C03-01.M01.01–05`, `C03-02.M01.01–05` y `C03-03.M01.01–05`, todos LISTO al claim. Referencia local: [alcance y exclusiones de M01.01](c03-04-m01-01-objetivo-continuidad-incidentes.md). Esta preparación NO libera `M01.06`, `M02`, despliegues, migración, ejecución de backup/restore, QA, main o PROD.

**Resultado de fase futuro:** definir y comprobar incidentes, revocaciones, respaldos, restauración y continuidad operativa, con un runbook de recuperación y rollback verificable para los bloques C10/C12. **Resultado de esta unidad M01.02:** enumerar actores, flujos, interfaces, límites de confianza, entradas, salidas y oráculos positivos/negativos. No equivale a activar servicios, renovar certificados, restaurar bases ni medir RPO/RTO reales.

## 1. Matriz de actores y responsabilidades de aprobación

| ID | Actor / superficie | Entrada autorizada | Salida o responsabilidad | Límite obligatorio |
| --- | --- | --- | --- | --- |
| ACT01 | Propietario / responsable de incidente | Declaración de incidente y severidad | Decisión documentada, prioridad, autorización concreta | Nunca consentimiento implícito a cambio productivo |
| ACT02 | Operador de plataforma autorizado | Señal de fallo, runbook vigente, permiso comprobado | Contención y coordinación bajo alcance aprobado | No asumir que acceso a DEV permite PROD o IAM |
| ACT03 | Administrador de empresa A | Incidente sobre recursos de A | Solicitud limitada y acuse de impacto A | Sin acceso ni restauración de datos B |
| ACT04 | Usuario de empresa B | Sesión, evento o recurso propio | Notificación/denegación pertinente a B | No filtrar identidades, eventos o backups de A |
| ACT05 | Servicio de autenticación/JWT | Sesión y membresía verificadas | Login/renovación/denegación según contrato efectivo | Revocación inmediata global y denylist NO acreditadas |
| ACT06 | Servicio de permisos RBAC/tenant | Identidad, membresía, permiso y contexto | Veredicto server-side por empresa/recurso | Header, correlation ID y UI no son grants |
| ACT07 | Auditor de seguridad autorizado | Evento minimizado y permiso de lectura | Registro/correlación acotados y revisión | No incluir tokens, contraseñas ni PII ajena |
| ACT08 | Responsable de respaldo DEV | Solicitud aprobada y configuración externa | Backup cifrado, manifiesto/checksum, custodia | No copiar secretos ni archivos reales en repositorio |
| ACT09 | Operador de restauración aislada | Manifest, origen/versión y entorno efímero | Restore de ensayo, integridad y reporte | Nunca restaurar contra MySQL productivo |
| ACT10 | Responsable de base de datos | Política de MySQL/EF y esquema verificados | Evaluación de consistencia, migración/rollback | No editar Aiven real sin permiso explícito |
| ACT11 | Responsable de archivos / Cloudinary | Referencias de medio, propiedad y metadatos | Reconstrucción autorizada y verificación de objetos | No presumir R2/Cloudflare productivos ni migrar imágenes inexistentes |
| ACT12 | CI de GitHub Actions | SHA exacto, workflow YAML, variables autorizadas | Job, logs, pruebas y artefactos causales | SKIPPED/CANCELLED no son PASS; runner efímero |
| ACT13 | Responsables de frontend/backend (Vercel/Render) | Config, health/readiness y versión | Diagnóstico y restablecimiento sujeto a autorizaciones | Ningún despliegue o reconfiguración por documento |
| ACT14 | Respondedor de seguridad/privacidad | Brecha, dato minimizado y alcance legal | Escalado, preservación o aviso según norma aplicable | No inventar retención, SLA ni autorización jurídica |
| ACT15 | Revisor técnico A5 independiente | SHA, A0–A4, positivos y negativos | A5_PASS/A5_FAIL causal, por otra Task Javier | Autor no revisa su propio trabajo; cero cuentas externas |

Los actores son **roles de diseño**, no credenciales, miembros nuevos, scopes activos o permisos concedidos. Las cinco Tasks Javier restantes son las únicas candidatas a A5 independiente; no se usan Tasks de otra cuenta.

## 2. Contratos de entrada/salida y puntos de confianza

| ID | Entrada / emisor | Consumidor / superficie | Salida esperada o denegación observable |
| --- | --- | --- | --- |
| IO01 | Alerta o error de API/servicio | Operador + clasificación de incidente | Incidente con ID, severidad y origen verificables, sin secreto ni duplicado |
| IO02 | Identidad y membresía revocada | Auth/JWT + autorización por request | Solicitudes posteriores fuera de scope rechazadas; no afirmar invalidación total de todos los JWT antes de probar |
| IO03 | Correlation ID de cliente | Middleware de correlación / auditoría | Identificador saneado; CRLF/controles descartados, jamás usado como grant |
| IO04 | Acción crítica fallida | Servicio de auditoría y DB | Registro estricto o rollback donde el contrato lo exija; best-effort no prueba durabilidad |
| IO05 | Declaración de incidente multiempresa | Coordinación A/B y RBAC | Impacto separado por empresa y permisos; sin exposición cruzada |
| IO06 | Solicitud de backup DEV | Workflow permitido + secretos externos | Artefacto de backup autorizado y cifrado, checksum, origen y fecha; denegación sin permiso |
| IO07 | Manifiesto/versión de respaldo | Verificador de integridad | Checksum/identidad del origen congruentes o rechazo sin restaurar |
| IO08 | Restore de ensayo | MySQL 8.4 efímero + scripts | Reporte de restore y smoke/tenant; jamás escribir DB productiva |
| IO09 | Snapshot de objetos/documentos | Adaptador multimedia autorizado | Inventario y vínculo por tenant; sin prometer recuperación de objetos no inventariados |
| IO10 | Rollback de cambio ejecutable | Git dev, versiones y migración pertinente | Reversión no destructiva, coherencia de datos, diff y trazabilidad |
| IO11 | Pérdida de salud de API/UI | Monitor health/readiness | Diagnóstico con SHA y actor; no confundir ping con disponibilidad end-to-end |
| IO12 | Restauración sin permisos, checksum malo o destino real | Gate de operación | Rechazo fail-closed, cero escrituras y evidencia de intento saneada |
| IO13 | Requerimiento de RPO/RTO/retención | Propietario y política autorizada | Objetivo decidido y publicado antes de certificación, nunca valor supuesto |
| IO14 | Archivo de CI + SHA del candidato | A0–A4 y revisión A5 | Resultados exact-SHA, logs sin secretos, readback y A5 de otro Javier |
| IO15 | Solicitud de comunicación o exportación de incidente | Auditoría, privacidad y destinatario | Solo datos mínimos permitidos, ámbito tenant y propósito acreditados |
| IO16 | Cierre/postmortem | Propietario y responsables técnicos | Evidencia de incidente, contención, restauración, riesgos abiertos y mejoras con owner |

**Límites:** la topología vigente (Vercel/Render/Aiven/Cloudinary) se distingue de la topología futura Hostinger/Cloudflare/R2 del plan V5. El catálogo de interfaces no activa conectores, endpoints, alarmas, CLI ni nuevos flujos.

## 3. Fuentes reales verificables, sin presumir ejecuciones

| Referencia actual del repositorio | Git blob SHA | Qué acredita únicamente |
| --- | --- | --- |
| `.github/workflows/backup-dev-operativo.yml` | `90899f3807a073ccc07ab99756d239b908ef3fc4` | Workflow de backup DEV con cron/dispatch y pasos condicionados; existencia de YAML no certifica ejecución ni restauración |
| `.github/workflows/backup-restore-dev.yml` | `cee9cdd1e22d006c21d9a23cc40ead6223d7295c` | MySQL 8.4 descartable y job de ensayo; solo al ejecutarse puede producir prueba real |
| `.github/workflows/backup-restore-backend-smoke.yml` | `9ae22277208c78a13f6d7f81b8be740845a057fd` | Trigger push restringido + marcador `[SOLQARYN-BACKUP-SMOKE]`; no se dispara por modificar este Markdown |
| `docs/v5/c03-01-m01-02-actores-entradas-salidas-autenticacion.md` | `7174f44819017c48b77d7f6903ceb5f5974a8e99` | Contrato documental de renovación, revocación y abuso; no acredita revocación JWT global |
| `docs/v5/c03-02-m01-02-actores-entradas-salidas-superficies-rbac-abac.md` | `7b9a4e58c179cafd5fc9116501d2052cb5bdc7a4` | Fronteras tenant/roles/archivos; no sustituye pruebas BOLA/IDOR en HTTP+DB |
| `docs/v5/c03-04-m01-01-objetivo-continuidad-incidentes.md` | `1f5ff7892c6fc0e907f689a467bd3ef23e4d7f21` | Contrato M01.01 de incidentes/backup/restore futuro, sin implementar operaciones |

Los hashes son hashes de **contenido estático**, no run IDs. Las variables o secretos sintéticos declarados en CI no autorizan copiarlos a documentación/telemetría. Para un ensayo posterior, verificar SHA, disparador, permisos, ambiente, MySQL efímero y resultado real.

## 4. Casos positivos, negativos y riesgos abiertos: aceptación FUTURA

| Caso | Signo | Estímulo futuro | Oráculo / prueba pendiente |
| --- | --- | --- | --- |
| CT01 | + | Operador autorizado clasifica incidente de A | Evento único, severidad, owner, timestamp y alcance A, sin PII B |
| CT02 | - | Operador de A solicita restore de B | 403/rechazo y cero escrituras/filtración a B |
| CT03 | + | Backup DEV autorizado y checksum íntegro | Manifiesto + checksum + cifrado + readback con job ejecutado |
| CT04 | - | Backup con checksum roto, credencial vencida o manifest no coincidente | Abort antes de restore, no escribir datos ni declarar éxito |
| CT05 | + | Restore cifrado de ensayo a MySQL descartable | Integridad de tablas/relaciones, test de API y aislamiento A/B, logs causales |
| CT06 | - | Restore apunta a producción o pretende sobreescribir datos legítimos | Gate deniega operación y emite evidencia saneada |
| CT07 | + | Revocación autorizada de membresía A | Petición posterior reevalúa permisos y no permite recurso A revocado |
| CT08 | - | JWT viejo o header de empresa B se reusa para acceder a B sin grant | 401/403, ningún registro o recurso B expuesto |
| CT09 | - | Header de correlación incluye CRLF o >64 caracteres | Identificador seguro regenerado, sin inyección en logs |
| CT10 | - | Falla DB durante evento crítico obligatorio | Rollback / fail-closed conforme contrato; no prometer durabilidad best-effort |
| CT11 | + | Restauración y rollback documentados tras cambio de esquema | Pruebas reversibles en entorno efímero, aprobación y SHA exactos |
| CT12 | - | Se propone RTO/RPO sin medición o A5 del propio autor | Gate bloquea certificación y promoción, preserva pruebas reales |

Estos CT01–CT12 son **requisitos con oráculos**, NO casos ejecutados en esta microtarea. Lo que existe hoy no autoriza afirmar que toda sesión se revoca inmediatamente, que hay restore probado para el SHA de este documento, que RPO/RTO o retención están aprobados, o que auditoría A/B universal ya está certificada.

## 5. Clasificación, auditorías y reversibilidad de M01.02

- **A0 estática:** revisar Markdown, 15 actores ACT01–15, 16 entradas IO01–16 y 12 escenarios CT01–12; comparar fuentes/blobs, secretos, diff documental y enlaces.
- **A1 funcional documental:** verificar casos positivos CT01/CT03/CT05 y negativos CT02/CT04/CT06/CT08; todos definen actor, acción y oráculo, sin reportar como ejecución.
- **A2 seguridad documental:** JWT/RBAC/tenant A/B, revocación, no exposición de PII/tokens, cifrado y credenciales fuera de repo, no operar PROD/QA/main.
- **A3 integración documental:** 15 antecedentes DAG LISTO, contratos entre incidentes, Auth, auditoría, CI, backups, EF/MySQL y consumidores; no cambia ninguna interfaz ejecutable.
- **A4 pospublicación:** commit fast-forward directo en dev, SHA/diff y blob finales, Scope Lock exact-SHA completed/success, readback PLAN/COLA/BITACORA A:Z y rollback.
- **A5 independiente:** A5_PENDIENTE para otra de las cinco Tasks Javier con positivos/negativos documentales; A5_PASS nunca por esta Task autora.

`requiere_dotnet=NO_APLICA`, `requiere_node_angular=NO_APLICA`, `requiere_mysql=NO_APLICA`, `requiere_docker=NO_APLICA`, `requiere_e2e=NO_APLICA`, pues no se modifica código. `requiere_security=SI_REVISION_DOCUMENTAL`, `requiere_scope_lock=SI`. No exigir ni falsificar jobs de runtime irrelevantes. Rollback de contenido: revertir únicamente este commit documental mediante un nuevo commit de revert, preservar BITACORA y no reescribir historia; sin secrets, infra, QA/main/PROD, PR ni ramas.

**Salida:** mapa documental revisable; NO habilita M01.06/M02 ni ejecuta recuperación, cambio de estado de sesión o prueba de restore real.
