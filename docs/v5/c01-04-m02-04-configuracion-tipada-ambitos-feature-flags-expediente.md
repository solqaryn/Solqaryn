# SOLQARYN V5 FINAL — C01-04.M02.04: configuración tipada, ámbitos y flags seguros del expediente universal

**Autor:** PRIMARY DEV 06 — JAVIER_CHATGPT (TASK_ID 6af16bbad26881918c283abe06e1f32a). **Fecha:** 2026-11-14, America/Tegucigalpa. **Rama:** solqaryn/Solqaryn@dev. **Scope Lock:** STRICT.

**Salida atómica M02.04:** definir el contrato de configuración tipada por empresa, recurso, sucursal, plan y rol para expediente/timeline/observaciones/decisiones/adjuntos/versiones. **Este documento es PROPUESTO_NO_IMPLEMENTADO:** no agrega variables a appsettings, secretos, flags, guards, endpoints, base de datos, workers, servicios, UI ni permisos de uso. Pruebas previstas: FUTURAS_NO_EJECUTADAS. No modifica QA/main/PROD.

## 1. Autoridad y alcance

Autoridad: [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) para C01-04.M02.04 y [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). El DAG específico F=VALIDADO/G=SI exige C01-04.M01.01–06 con revisión A5. No supone M02.02 o M02.03 A5 aprobado, tampoco habilita M02.06 ni M03, IAM/JWT/RBAC, SQL o QA.

Fuentes concretas: docs/v5/c01-04-m02-01-modelo-entidades-ownership-invariantes-expediente.md (EX01–EX16, IN01–IN18), docs/v5/c01-04-m01-03-inventario-contratos-codigo-evidencia.md, backend/src/Domain/Security/ContextoTenantActual.cs (membresía verificada; ExigirEmpresa), backend/src/Domain/Entities/RegistroAuditoria.cs (EmpresaId nullable legacy), backend/src/Domain/Entities/CompraDocumento.cs (objeto de compra con PublicId y EmpresaId nullable). Su presencia **no acredita** resolución segura de todas las consultas del expediente. La propuesta no cambia estas fuentes.

## 2. Contrato de configuración tipada CFG01–CFG16 (NO IMPLEMENTADA)

| ID | Clave conceptual / tipo | Ámbito autorizado y valor seguro inicial |
| --- | --- | --- |
| CFG01 | Expediente.Enabled : bool | tenant + recurso; false |
| CFG02 | Expediente.ReadMode : enum | tenant + módulo; Deny hasta preflight |
| CFG03 | Expediente.WriteMode : enum | tenant + rol + recurso; Deny |
| CFG04 | Timeline.PageLimit : int | plataforma con límite superior; 50, sujeto a pruebas |
| CFG05 | Timeline.CursorTtlMinutes : int | plataforma/tenant restringido; 10, no autoriza tenant cruzado |
| CFG06 | Timeline.MaxQueryWindowDays : int | plataforma con límite; 31, por confirmar con volumen |
| CFG07 | Timeline.ProjectionVersion : string | despliegue compatible por versión; no activar sin soporte |
| CFG08 | Observacion.MaxBytes : int | plataforma/plan restringido; 4096, validación backend posterior |
| CFG09 | Decision.RequiresReview : bool | política por tipo de decisión; true por defecto |
| CFG10 | Decision.AllowDirectApprove : bool | tenant + rol efectivo; false |
| CFG11 | Adjunto.PrivateReadEnabled : bool | tenant + recurso; false |
| CFG12 | Adjunto.MaxSizeBytes : long | plataforma + plan, techo absoluto; valor pendiente de evidencia |
| CFG13 | Adjunto.AllowedMime : set<string> | plataforma, lista positiva estrecha; ninguna hasta aprobación |
| CFG14 | Versioning.RequireIfMatch : bool | operaciones de escritura; true como requisito futuro |
| CFG15 | Idempotency.RetentionHours : int | plataforma + tenant dentro de límites; 24 candidato |
| CFG16 | Audit.SafeCorrelation : bool | transversal no desactivable; true |

Las claves CFG01–CFG16 y sus valores son **propuestas**, no nombres de opciones .NET existentes ni valores desplegados. Cualquier número ejemplar exige ensayo de capacidad, retención, privacidad y experiencia antes de materializarse. Si una clave es desconocida, mal tipada, vacía, inválida o excede un límite, el servicio futuro falla cerrado y no amplía capacidades. Ningún flag puede elevar autoridad efectiva de JWT, UsuarioEmpresa, rol o membresía revocada.

## 3. Resolución de ámbito, precedencia y límites

1. Identidad efectiva se resuelve en backend con usuario, membresía activa, EmpresaId, sucursal y rol. Un EmpresaId proporcionado por request, ruta, cookie, localStorage o UI **no** es autoridad.
2. La configuración global sólo puede imponer máximos, deshabilitaciones y valores conservadores. Un override de tenant no supera esos máximos ni habilita algo explícitamente denegado por seguridad o plan.
3. La capa de plan/rubro restringe recursos y cuotas sin conceder derechos RBAC. La capa tenant/sucursal especifica preferencia operativa dentro del máximo; un override de usuario no altera scopes de empresa, permisos ni retención.
4. Si existe conflicto o ausencia de configuración, aplicar intersección de restricciones, denegación por defecto y error seguro con correlación saneada. Nunca usar tenant anterior como fallback al cambiar contexto.
5. Cache de opciones debe estar aislado por EmpresaId, contexto de membresía/rol, recurso, versión y TTL. Invalidar ante revocación, cambio de empresa, plan, rol, configuración o recurso. Prohibido usar una clave global que comparta datos A/B.
6. Desplegar futuros flags con auditoría de quién, qué, cuándo, ámbito y versión; cambios concurrentes requieren comparación de versión atómica, no simple último escritor.
7. Secretos y credenciales sólo por referencias a gestor autorizado; **nunca** dentro de opciones, markdown, repositorio, logs o payload de frontend. Ningún valor devuelve URLs privadas estables ni tokens.
8. La lectura de documento, observación, decisión y versión siempre vuelve a aplicar autorización backend. Una opción true no elimina comprobación de propiedad real del recurso ni revisión obligatoria.

## 4. Matriz de precedencia y casos de fallo

| Elemento | Resolución futura requerida | Comportamiento seguro ante error |
| --- | --- | --- |
| Autenticación y rol | Verificación JWT + membresía server-side + RBAC vigente | Deny y traza mínima, sin exposición de recurso |
| Tenant/Sucursal | Empresa/sucursal del contexto autorizado y recurso | Deny de empresa B a usuario A |
| Plan/capacidad | Intersección de plan y política de plataforma | Capacidad no disponible, nunca escalamiento |
| Configuración individual | Valor tipado dentro de límites de plataforma y empresa | Valor conservador / denegación |
| Retención de adjuntos | Política efectiva legal/tenant y propietario de origen | No exponer ni borrar por opción inválida |
| Expiración de cursor | Scope + tenant + filtros + versión + firma | Cursor caducado/corrupto rechazado |
| Revisión y versión | Revisor autorizado y versión CAS antes de efecto | Conflicto, sin cambio parcial ni auditoría perdida |
| Correlación | Identificador saneado único por operación | Rechazar trazas sensibles y generar código seguro |

**Invariantes:** EX01–EX16 siguen siendo modelos conceptuales; RegistroAuditoria.EmpresaId/CompraDocumento.EmpresaId null no deben convertirse en modo global; EmpresaId es frontera de consulta y FK. La UI Angular puede ocultar controles por experiencia, pero no concede acceso. No se introducen permisos, perfiles, roles, migraciones ni políticas ocultas.

## 5. Doce validaciones propuestas, FUTURAS_NO_EJECUTADAS

| ID | Camino positivo futuro | Camino negativo futuro |
| --- | --- | --- |
| VT01 | Tenant A habilitado, miembro A ve recurso autorizado | A nunca ve recurso de B aunque flag true |
| VT02 | Valor numérico tipado dentro de máximo se acepta | Valor negativo, overflow o cadena causa rechazo |
| VT03 | Plan permite cuota bajo límite de plataforma | Override tenant no supera techo del plan |
| VT04 | Revocación invalida caché tenant/rol | Caché previa autoriza membresía revocada |
| VT05 | Cambio A→B obtiene opciones y resultados B | Respuesta vieja A aparece en B |
| VT06 | Configuración desconocida produce denegación trazable | Clave mal escrita activa feature por default |
| VT07 | Decisión requiere revisor real | DirectApprove false es ignorado por cliente |
| VT08 | Adjunto privado exige permiso y URL breve | PublicId/URL de B elude tenant |
| VT09 | Cursor válido vinculado a filtros funciona | Cursor de otro tenant, vencido o modificado funciona |
| VT10 | Dos escritores CAS dejan un ganador | Última escritura sobrescribe versión anterior |
| VT11 | A/B generan logs con correlación sin datos personales | JWT, secreto o email viaja a observabilidad |
| VT12 | Flag inactivo conserva flujos legacy sin mutarlos | Activar opción documental cambia DB o CI |

Las 24 columnas positivo/negativo son **oráculos futuros, no pruebas unitarias, de integración, pentest ni Playwright ejecutadas**. Cuando haya código: .NET 10, pruebas de seguridad A/B, autorización de recursos, migraciones MySQL 8.4 efímero cuando procedan, contrato HTTP y UI solo cuando exista consumidor. No simular PASS de suite no ejecutada.

## 6. Plan de introducción y reversión sin implementación en esta entrega

**Fase futura A — diseño:** mapear a options tipadas existentes (si corresponden), revisar capa API/Application/Domain/Infrastructure, contratos de clientes, permisos, niveles de plan, retención y auditoría. Verificar ausencia de otro autor sobre archivos y diseñar compatibilidad de flags.

**Fase futura B — expansión controlada:** agregar validación de opciones y configuración seguro-por-defecto con tests negativos; no escribir secretos ni usar flags como reemplazo de permisos. Publicación requiere exact-SHA y pruebas runtime causales.

**Fase futura C — rollout/rollback:** activar sólo por tenant y recurso previamente autorizados; medir respuestas, aislamiento, latencia y errores; revertir deshabilitando opciones aditivas y recuperando versión auditada, sin borrar registros, cambiar rol ni degradar controles. No se habilita nada por la existencia de este documento.

Rollback inmediato de este cambio puramente documental: revert normal de sus dos archivos Markdown en dev, preservando modificaciones concurrentes y BITACORA histórica.

## 7. Auditorías A0–A5 y salida

**A0:** comprobar contrato V5, DAG F/G, doce límites de seguridad, CFG01–CFG16 y VT01–VT12, sin secretos ni cambio ejecutable; CHANGELOG autocontenido. **A1:** oráculos positivos/negativos concretos NO_EJECUTADOS; no declarar funcionalidad. **A2:** evaluación estática de aislamiento tenant A/B, JWT/RBAC, revocación y denegación por defecto; runtime NO_APLICA al diff Markdown. **A3:** cotejo con M01.01–06+A5, M02.01 y fuentes del código, PLAN/COLA/BITACORA sin alterar API/DB. **A4:** diff exclusivamente documento V5 y CHANGELOG, commit fast-forward con HEAD esperado, Scope Lock + V5 completed/success en SHA exacto, readbacks de PLAN/COLA/DAG y BITACORA. **A5:** pendiente de revisión real por otra Task Javier distinta de la autora; ni autoaprobación ni promoción de QA/main/PROD.

Esta especificación no cambia el estado de autorización ni hace verdadera una configuración ficticia: todo consumidor futuro requiere su microtarea, pruebas causales y liberación por dependencias reales.
