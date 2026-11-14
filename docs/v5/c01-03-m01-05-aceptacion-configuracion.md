# SOLQARYN V5 FINAL — C01-03.M01.05: criterios de aceptación observables para configuración dinámica

**Naturaleza:** preparación DOCUMENTAL de M01 (no implementación, contrato final, migración, activación de flags ni autorización de despliegue). **Fuente:** Standard de Implementación V5 FINAL, bloque C01-03; registro DAG_TECNICO de este ID validado. **Estado del objetivo:** criterios definidos; todavía NO se ha demostrado su cumplimiento en API, DB o browser.

## 1. Propósito y límites

El resultado futuro de C01-03 es separar **valores editables** del código ejecutable, con ámbitos y tipos validados. Esta microtarea sólo fija criterios de aceptación verificables, positivos y negativos; M02 definirá contrato, esquemas y ownership y M03–M05 implementarán y ejecutarán los tests reales. El DAG autoriza exclusivamente M01.01–05 preliminares con once predecesoras LISTO (C00-01.M02.01–06 y C00-03.M01.01–05). Se revalidará al cerrar C00-01 y la identidad C01-01, antes de M01.06, M02 o escritura ejecutable. No inferir habilitación de C01-03 desde reserva de capacidad.

**Fuera de alcance:** ningún cambio a .NET/Angular/Node, DB/MySQL, tenant existente, secrets, permisos/RBAC, proveedores, DNS, Hostinger/Cloudflare, QA/main/PROD o automatizaciones; no cargar configuración arbitraria ni migrar imágenes inexistentes.

## 2. Contrato de aceptación propuesto (no esquema DB implementado)

Para cada clave editable, M02 deberá especificar como mínimo `key` en allowlist, `valueType` (booleano, número con límites, string acotado, enum o estructura tipada), `scope` autorizado, valor predeterminado seguro, constraints/validadores, propietario lógico, capacidad RBAC para editar y leer, versión/control de concurrencia, auditoría y regla de fallback. El alcance y el actor efectivo se calculan **en el servidor** desde JWT/membresía verificada, nunca desde `EmpresaId`, `tenant_id`, slug, URL, encabezados o cookies del cliente. Los clientes sólo pueden enviar claves/valores tipados permitidos y no son autoridad de autorización.

**Invariantes:** denegación por defecto; no ejecutar ni interpretar JavaScript, CSS/HTML libre, SQL, plantillas con código, rutas de archivos o URL `javascript:`; rechazar claves desconocidas o secretos; no leer/escribir configuración de otra empresa; no degradar RBAC por feature flags; no exponer PII ni secretos en respuesta, auditoría o log. Una configuración inválida no debe activar parcialmente capacidades ni alterar datos legítimos. El frontend sólo representa el resultado autorizado del backend. No introducir cambios de proveedor o infraestructura por completar un checklist.

## 3. Criterios de aceptación medibles de la implementación futura

| ID | Caso y entrada | Resultado observable | Evidencia requerida en fase ejecutable |
| --- | --- | --- | --- |
| AC-01 (+) | Administrador con membresía/capacidad válida actualiza clave allowlisted del ámbito propio, con tipo y límite válidos | API valida y persiste sólo valor del ámbito autorizado; readback coincide con tipo/versión y registra evento idempotente | Test API + repositorio/DB si hay persistencia + auditoría |
| AC-02 (+) | Lectura por usuario autorizado de su ámbito | Aplica valor efectivo, fallback seguro y regla documentada de precedencia; sin extraer otro tenant | Test integración de lectura y contrato de DTO |
| AC-03 (+) | Doble actualización con versión válida y luego obsoleta | Primera actualización trazable; versión obsoleta no sobrescribe cambio reciente | Positivo + negativo de concurrencia sobre proveedor real |
| AC-04 (-) | Empresa A intenta leer/modificar valor de empresa B, cambiando slug/ID/cabecera/cookie | 403/404 seguro según contrato futuro, sin modificación ni datos de B y con registro de denegación no sensible | Autenticación JWT/membresía A/B y prueba negativa backend |
| AC-05 (-) | Usuario sin rol/capacidad o token inválido/caducado intenta editar | 401/403, sin mutación ni elevación por flag | Prueba de RBAC/JWT positiva y negativa |
| AC-06 (-) | Clave desconocida, tipo incorrecto, nulo prohibido, rango fuera de límites o tamaño excesivo | 400 seguro y atómico; ninguna escritura parcial | Matriz de validadores con casos límite |
| AC-07 (-) | Valores `<script>alert(1)</script>`, `javascript:alert(1)`, `'; DROP TABLE x; --` o CSS libre en campo tipado | Rechazo por tipo/allowlist, sin ejecución/renderizado ni consultas SQL de texto arbitrario | Pruebas negativas de inyección por superficie realmente existente |
| AC-08 (-) | Petición de editar secreto, conexión, URL de proveedor o feature flag reservado | Denegación explícita sin retorno del secreto ni cambio de proveedor | Prueba de permisos + comprobación de logs/PII |
| AC-09 (-) | Fallo de validación o de persistencia después de recibir petición | Respuesta segura, transacción revertida, sin estado mixto ni datos de otros tenants | Test de rollback transaccional/infraestructura cuando se implemente |
| AC-10 (+/-) | UI recibe estado de carga, error, vacío, éxito y permiso denegado | Muestra estados accesibles, no privilegia datos visibles como fuente de autoridad | Angular tests y E2E/teclado si UI real es parte del cambio |

**Condición de salida futura del bloque:** ninguna AC crítica de seguridad o aislamiento falla; evidencia con comando, toolchain, test, resultado, SHA exacto y rollback. Un test `SKIPPED`, mock sin validación real o check de SHA anterior no acredita la prueba que exige API/DB.

## 4. Matriz proporcional de verificación y dependencias

| Etapa | Verificación | Regla |
| --- | --- | --- |
| M01.05 (esta unidad) | Confrontar fuente V5 + PLAN/COLA/DAG, coherencia de los diez AC, negativos de inyección, límites y rollback documental | Solo pruebas de **consistencia de especificación**, sin acreditar runtime |
| M02 contratos | Esquema tipado, allowlist, DTO, ownership y errores explícitos | Revalidar C01-01 y alcance C00 definitivo antes de fijar firmas |
| M03 implementación | Lint/análisis estático, seguridad, JWT/RBAC/tenant A/B, transacciones/persistencia según archivos | CI .NET/MySQL pertinente para **SHA publicado**; no usar gates ajenos |
| M04 superficies | Tipado Angular/UI, accesibilidad y estados del consumidor | npm/Angular y E2E únicamente si el cambio toca UI/navegación |
| M05 verificación | Ejecutar AC positivas y negativas con proveedores/DB cuando proceda | Jobs reales sobre SHA exacto; jamás `SKIPPED=PASS` |
| M06 liberación | Evidencia, A5 por TASK_ID distinto y rollback seguro | QA/main/PROD requiere autorización aparte; jamás automático |

Para esta **tarea documental**: `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`; `requiere_security=SI` por especificación de amenazas y negación, sin afirmar test live; `requiere_scope_lock=SI` para el commit de documentación y los checks exigibles. A0–A3 documentales verifican fuente, integridad, riesgos y coherencia; A4 exige SHA/Scope Lock y readback institucional. A5 técnica independiente del SHA publicado, sin autopromoción.

## 5. Evidencia, incertidumbres y reversión

Referencias consultadas: Plan V5 FINAL, `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_V13.md`, Skill canónica y `references/architecture.md`; referencia de patrón frontend real: `frontend/src/app/services/theme-applier.service.ts` aplica sólo colores hexadecimales permitidos. **Este patrón no demuestra que exista un subsistema general de configuración dinámica ni sus endpoints**, por lo que AC-01–10 no son certificaciones de implementación.

Riesgos pendientes: modelo de scopes y precedencia aún sin resolver en M02; coordinación con identidad/tenant C01-01; seguridad de logs y versionado; pruebas SQL/API/Playwright aún no ejecutadas. La implementación futura debe preservar usuarios y datos existentes. Rollback de esta entrega: `git revert <SHA_DEL_COMMIT_DOCUMENTAL>` ordinario tras revisar HEAD y dependencias, sin force-push; no borrar ni reescribir BITACORA. Al corregir este documento, repetir los controles causales y solicitar nueva A5 independiente.
