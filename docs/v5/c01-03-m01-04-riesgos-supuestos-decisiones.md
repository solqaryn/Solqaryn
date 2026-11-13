# SOLQARYN V5 FINAL — C01-03.M01.04: riesgos, supuestos y decisiones de configuración dinámica

**Estado:** análisis PRELIMINAR de M01, sin implementación, certificación de código, modelo persistido ni cambio de permisos. **Autoridad:** Standard V5 FINAL C01-03; DAG_TECNICO F=VALIDADO para preparación documental M01.01–05 con once prerrequisitos C00-01.M02.01–06 y C00-03.M01.01–05 LISTO. **Revalidar** contra identidad C01-01 y cierre C00-01 antes de M01.06, M02, implementación o promoción.

## 1. Contexto y frontera

Objetivo futuro: permitir edición de valores tipados sin convertir datos de usuario en código ejecutable, con ámbito/propiedad y controles RBAC en backend. La presente unidad registra riesgos, supuestos, decisiones pendientes, responsables sugeridos, efectos y pruebas para diseñar el contrato. **No autoriza** un endpoint genérico de configuración, ejecutar scripts/SQL/HTML/CSS, crear tablas/migraciones, cambiar JWT/RBAC, manipular datos de usuarios/empresas, alterar imágenes/proveedores, activar flags ni tocar QA/main/PROD.

Arquitectura vigente consultada: Angular standalone, .NET 10 por capas, EF Core/MySQL Oracle, JWT, BCrypt, RBAC y tenancy verificada en servidor. Evidencia de un patrón localizado: `frontend/src/app/services/theme-applier.service.ts` limita colores CSS a hexadecimales. Este ejemplo **no demuestra** que el servicio de configuración universal ya exista o esté probado; los contratos y consumidores finales deben revisarse en M02.

## 2. Registro de riesgos preliminares

| ID | Riesgo y disparador | Impacto | Control exigido / prueba negativa futura | Dueño de mitigación sugerido |
| --- | --- | --- | --- | --- |
| R01 | Cliente envía `EmpresaId`, `tenant_id`, slug o cabecera de empresa ajena | Lectura o mutación cross-tenant | Resolver contexto en servidor, JWT/membresía; probar A contra B y denegar sin fuga | Backend / seguridad |
| R02 | Clave dinámica sin allowlist o tipo y límites | Código arbitrario o configuración corrupta | Catálogo tipado; rechazar claves desconocidas, nulos prohibidos, valores extensos y tipos incorrectos | Dominio / API |
| R03 | Aceptar HTML/JS/CSS/SQL literal en configuración | XSS, inyección, cambios de estilo no autorizados | Bloquear `<script>`, `javascript:`, SQL en campos tipados; nunca interpolar como código | Seguridad / UI / API |
| R04 | Feature flag editable modifica autorización | Escalamiento RBAC por UI | Ningún flag puede conceder membresía/rol; 401/403 de API con y sin flag | Seguridad / identidad |
| R05 | Configuración heredada de tenant equivocado o respuesta tardía | Branding/SEO/datos de otra empresa | Invalidar contexto al cambiar ruta/tenant, prueba A→B / A→login | Frontend / backend |
| R06 | Dos escritores actualizan misma clave/version | Pérdida silenciosa de cambios | Versión optimista, rechazo de escritura obsoleta, auditoría de cambio | Aplicación / DB |
| R07 | Error entre validación y escritura | Estado parcial y datos inconsistentes | Operación atómica con rollback; prueba de fallo de persistencia con DB efímera | Infraestructura |
| R08 | Logging expone secreto, PII o valor sensible | Fuga de privacidad | Redacción/allowlist de metadatos en logs, test de ausencia de tokens/secretos | Observabilidad / seguridad |
| R09 | Valor por defecto no seguro o precedencia ambigua | Comportamiento no determinista o escalada | Definir orden explícito de scopes y fallback fail-closed, probar lectura sin ámbito | Arquitectura / producto |
| R10 | Uso de CI pasada, mock o job SKIPPED como prueba funcional | Cierre falso con riesgo abierto | A0–A4 proporcionales sobre SHA exacto; ejecutar API/DB/browser cuando se cambie runtime | Task autora/revisora |

**Severidad preliminar:** R01–R04 y R08 son potenciales críticos hasta prueba negativa real; R05–R07 y R09 requieren evidencias de integración. Ningún riesgo queda "mitigado" solamente por escribir esta tabla.

## 3. Supuestos explícitos que requieren validación

| ID | Supuesto, no hecho certificado | Cómo confirmarlo | Si falla |
| --- | --- | --- | --- |
| S01 | Habrá un catálogo acotado de claves y tipos | Contrato M02 revisado por backend + seguridad | Bloquear API genérica |
| S02 | JWT y membresía verificada identifican ámbito efectivo | Código y tests de tenancy C01-01 y C01-02 | Bloquear edición multitenant |
| S03 | El modelo de scopes y precedencia se acordará por producto/seguridad | Matriz de prioridad con positivos/negativos M02 | Usar defaults seguros, no activar |
| S04 | Persistencia e índices permitirán rollback sin afectar datos existentes | Diseño EF/MySQL, pruebas y respaldo verificable | No migrar ni activar |
| S05 | Las superficies Angular conocerán el contrato tipado y estados de error | Revisar DTO/consumidores cuando existan en M04 | No asumir cobertura de UI |

## 4. Decisiones de diseño y estado

| ID | Decisión propuesta | Estado y condición de aprobación |
| --- | --- | --- |
| D01 | Configuración **tipada, allowlisted y de tamaño acotado**, nunca código dinámico | Propuesta; validar catálogo M02 |
| D02 | Ownership y RBAC exclusivamente en servidor desde identidad comprobada, nunca desde el cliente | Requisito de seguridad; comprobar con A/B en M03/M05 |
| D03 | Valores sensibles, rutas administrativas y secrets quedan fuera del editor genérico | Propuesta conservadora; seguridad debe validar clasificación |
| D04 | Especificar control de versión, transacciones y auditoría por cambio | Pendiente API/DB y pruebas de concurrencia |
| D05 | No introducir automáticamente nuevo proveedor, Hostinger/Cloudflare, R2 o migración histórica sin inventario y orden posterior | Restricción vigente; cualquier ejecución requiere gate separado |
| D06 | La revisión A5 independiente y CI pertinente son necesarias para liberación, no para declarar que un documento preparatorio implementó el sistema | Gobernanza vigente; distinguir LISTO_DEV de A5_PASS y QA |

**Ninguna decisión anterior autoriza una migración ni una alteración técnica por sí sola.**

## 5. Verificación dirigida, positivos y negativos de este documento

**Positivo documental:** cada R01–R10 tiene disparador, impacto, control/protocolo y responsable sugerido; S01–S05 se presentan como hipótesis; D01–D06 tienen condición y carácter propuesto. Los IDs son únicos. Las dependencias de once predecesoras y la limitación M01–05 constan en DAG validado.

**Negativo documental 1:** si se elimina el rechazo de `<script>`, `javascript:` o SQL, el control de inyección queda incompleto y esta versión NO es aceptable.

**Negativo documental 2:** si un contrato permite que `EmpresaId` enviado por navegador otorgue autoridad, falla el aislamiento A/B y se rechaza el contrato.

**Negativo documental 3:** si se declara que cualquier R está mitigado sin prueba real o que SKIPPED=PASS, se rechaza la evidencia.

Las pruebas de API, DB, JWT, Angular y E2E **no se ejecutan ni se requieren por este cambio exclusivamente documental**; se mapean a M02–M05 cuando se desarrollen superficies reales. Para esta unidad, `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`, `requiere_security=SI` (análisis de límites, sin prueba live) y `requiere_scope_lock=SI` (commit documental a dev).

## 6. Evidencias, revalidación y rollback

Fuentes: V5 FINAL, `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_V13.md`, skill `solqaryn-project-governance` canónica y arquitectura viva, hoja `PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA` (26 campos A:Z). Documento vecino de criterios: `docs/v5/c01-03-m01-05-aceptacion-configuracion.md`; no usar su estado LISTO_DEV como certificación del bloque C01-03.

**A0–A3** para esta entrega: integridad y diferencia documental, casos positivos/negativos, análisis de seguridad lógica, coherencia de dependencias. **A4**: commit directo fast-forward, diff de archivos, Scope Lock SUCCESS exact-SHA, readback PLAN/COLA/BITACORA. **A5**: otra Task distinta de la autora; pendiente hasta revisión real.

**Rollback documental:** `git revert <SHA_DEL_COMMIT>` tras revalidar HEAD y consumidores; no force-push, no borrar eventos institucionales ni sobrescribir documentos de compañeras. Antes de fases ejecutables, revalidar C00-01 final, C01-01 y amenazas/decisiones propuestas; abrir corrección si cambia el contrato.
