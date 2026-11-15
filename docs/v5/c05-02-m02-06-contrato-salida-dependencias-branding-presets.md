# SOLQARYN V5 FINAL — C05-02.M02.06: contrato de salida M02 para branding y presets

**Autor:** PRIMARY DEV 02 — JAVIER_CHATGPT; TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 15 de noviembre de 2026. **Naturaleza:** entrega exclusivamente documental; no implementa software, permisos, datos ni infraestructura.

**Fuentes de autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive ID `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`); `PLAN_MAESTRO`, `COLA`, `DAG_TECNICO`, `BITACORA` del Sheet institucional, `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, Skill canónica de SOLQARYN y contratos documentales `C05-02.M01.01–06` y `C05-02.M02.01–05`. Las condiciones aquí definidas no reemplazan la fórmula viva ni conceden autorización para M03, QA, main o PROD.

## 1. Evidencia del baseline y fronteras de confianza

- `backend/src/Domain/Entities/TemaVisual.cs`: tema visual **global** de fila `Id=1`, quince propiedades de color, **sin** `EmpresaId`. Nunca tratarlo como una personalización tenant.
- `backend/src/Domain/Entities/ConfigEmpresa.cs`: configuración operativa **de empresa** con `EmpresaId` y `Version`; no acredita perfiles visuales, presets empresariales versionados ni un mecanismo vigente de preview-publicación-rollback.
- `backend/src/API/Controllers/EmpresaConfiguracionController.cs`: rutas `empresa-configuracion` con superficie tenant existente; el identificador de ruta no sustituye la comprobación server-side de membresía y permisos.
- `frontend/src/app/services/theme-applier.service.ts`: aplicación visual y validación conservadora de hexadecimales; esta limpieza de UI **no** es una autorización JWT/RBAC del backend.
- Los contratos M02.01–05 describen **propuestas** de entidades E01–E14, DTO, persistencia, flags y eventos. Sus objetos nuevos, tablas, endpoints, publicación versionada, señales, workers y dispatcher **no están implementados** por estos documentos.

## 2. Prerrequisitos del terminal M02 y exclusividad

| Referencia | Condición observable en el preflight del contrato | Consecuencia |
| --- | --- | --- |
| C05-02.M01.01–06 | Estado LISTO y revisión A5 independiente para cada ID, según fuente institucional viva | No convertir una preparación provisional en implementación autorizada |
| C05-02.M01.02 | El antiguo A5_FAIL por 16 frente a 15 colores se conserva en historial; existe corrección en SHA `e53df2cdb69d70bf4a0c7a2b65efa83560062d6f` y A5_PASS independiente posterior | Preservar fail histórico y evidencia de reparación, sin ocultarlo |
| C05-02.M02.01–05 | Cada contrato está LISTO y recibió A5 independiente de otra Task Javier antes de la reclamación de M02.06 | No suponer A5 por simple publicación del Markdown |
| C01-03.M01.06 | LISTO con A5 independiente en PLAN, prerrequisito específico de DAG para M02.06 | No inferir permisos de la arquitectura transversal |
| C05-02.M02.06 | PLAN H=PENDIENTE e I vacío antes del claim; COLA G/O=SI, DAG F=VALIDADO/G=SI; claim exclusivo de DEV02 con readback | Solo un autor, sin dos escritores sobre el scope |
| Fases posteriores M03–M06 | Gate calculado **independientemente** por DAG y A5 de M02.06; M03 código verdadero requiere pruebas dirigidas | Este documento por sí solo nunca pone G/O=SI |

**Importante:** las observaciones anteriores son evidencia de admisión, no garantías perpetuas. Ante cambios de PLAN/HEAD/A5, revisar nuevamente las filas y la fórmula antes de cualquier sucesora.

## 3. Matriz terminal de salida documental G01–G16

| Gate | Criterio de salida comprobable | Decisión conservadora |
| --- | --- | --- |
| G01 | Contratos M01 y M02.01–05 identificados por ID exacto y con A5 independiente | Denegar salida si falta una revisión o es autoaprobación |
| G02 | Separar `TemaVisual` global de `ConfigEmpresa` tenant | Rechazar cualquier afirmación de branding empresarial existente basada solo en `Id=1` |
| G03 | Catálogo lógico y relaciones futuras identificadas con EmpresaId server-side | No materializar nuevas entidades sin M03 |
| G04 | DTO propuestos identificados como NO existentes | No exponer rutas ficticias ni cambiar compatibilidad de API |
| G05 | Perfil, paleta, asset, revisión, publicación y preview con tenant correcto | Negar lectura o publicación de material B desde A |
| G06 | RBAC efectivo, JWT y membresía verificados en el servidor para mutaciones futuras | Rol o flag de UI nunca eleva permisos |
| G07 | Colores de allowlist HEX, texto escapado y ausencia de CSS arbitrario | Rechazar CSS/URL/script en tokens de personalización |
| G08 | Contraste, foco y accesibilidad definidos como criterios de prueba futura | No certificar WCAG mediante especificación escrita |
| G09 | Logo/asset con identificador opaco, misma empresa y estado autorizado | No reutilizar archivo privado de otro tenant |
| G10 | Publicación condicionada a `expectedRevision` y CAS futuro | No admitir last-write-wins o sobrescritura silenciosa |
| G11 | Versionado de previews y rollback documentado sin alterar tema global | Reset global nunca cuenta como rollback empresarial |
| G12 | Idempotencia, `operationId` y correlación con tenant explícito | No duplicar publicación ni evento por retry |
| G13 | Señales M02.05 expresamente FUTURAS_NO_EMITIDAS | Prohibido activar dispatcher, webhook, worker o cola desde M02 |
| G14 | Auditoría sin secretos, JWT, DSN, PII cruda o payload de otro tenant | Denegar salida si hay fuga o falta de redacción |
| G15 | CI de procedencia y Scope Lock completed/success del SHA publicado; diff limitado | Cancelled/skipped/in_progress jamás PASS de control requerido |
| G16 | A0–A4 reales de esta entrega y A5 por Task distinta, con BITACORA y readback | Nunca promover a M03, QA, main o PROD por este contrato |

La decisión de salida de **documentación M02** exige G01–G16 en su ámbito documental. G05–G14 describen restricciones de una implementación futura: aquí se verifica coherencia escrita, **no** ejecución de código ni garantías runtime.

## 4. Dependencias y alcance futuro de M03

M03 podrá considerar reglas del dominio, servicios Application, contratos API, persistencia EF/MySQL y UI Angular solo después de **su propio gate DAG** y revisión de independencia. Requiere un preflight nuevo sobre rutas exactas, datos y consumidores, seguridad JWT/RBAC/tenancy A/B, pruebas positivas y negativas ejecutadas, integración real en CI GitHub-hosted sobre SHA pertinente y estrategia de rollback. Ningún documento M02 hace que una propuesta se convierta en tabla, endpoint, feature flag activo, migración, worker o permiso.

Las puertas técnicas prohibidas en estado provisional incluyen terminales dependientes, migraciones destructivas, IAM/JWT/RBAC, resolución de tenant, datos productivos, QA/main/PROD, dominio, DNS y servicios externos de pago. El lote no se declara migrado ni implantado.

## 5. Oráculos de aceptación P01–P06 y N01–N12: FUTUROS_NO_EJECUTADOS

| Caso | Tipo | Estímulo propuesto | Oráculo requerido para una futura prueba ejecutada |
| --- | --- | --- | --- |
| P01 | positivo | Admin de empresa A autorizado consulta su branding versionado | Solo estado A, versión A y permisos A |
| P02 | positivo | Usuario A abre preview privada y la descarta | Publicación vigente intacta |
| P03 | positivo | Editor permitido publica revisión A con CAS vigente | Nueva versión A, auditoría A única |
| P04 | positivo | Reintento idéntico con idempotencyKey vigente | Resultado previo sin duplicación material |
| P05 | positivo | Reversión a snapshot válido de la misma empresa | Nueva decisión auditada y versión coherente |
| P06 | positivo | Usuario cambia de A a B validado | Tokens antiguos A borrados y sólo contexto B autorizado |
| N01 | negativo | Usuario A envía EmpresaId de B | Denegar sin revelar perfil, logo ni configuración B |
| N02 | negativo | Usuario sin `Configuracion.Editar` intenta publicar | Denegar, sin nueva revisión |
| N03 | negativo | Preset global se usa como grant empresarial | Denegar; preset no confiere permisos |
| N04 | negativo | Logo B se incluye en preview A | Denegar asset, sin filtración |
| N05 | negativo | Color CSS con `url()` o script | Rechazo por allowlist de tokens |
| N06 | negativo | Revisión CAS obsoleta | Conflicto y ninguna sobrescritura |
| N07 | negativo | Misma idempotencyKey, payload distinto | Conflicto, no emisión doble |
| N08 | negativo | Respuesta antigua de A llega tras entrar a B | Nunca restituir tema A |
| N09 | negativo | Reset de `TemaVisual` global se informa como rollback A | No certificar reversión tenant |
| N10 | negativo | CI obligatoria está CANCELLED/IN_PROGRESS | Permanecer VALIDANDO, no declarar PASS |
| N11 | negativo | Se usa evento conceptual como webhook activo | NO_GO, ninguna infraestructura de eventos nueva |
| N12 | negativo | A5 coincide con TASK_ID autor o predecesor no aprobado | NO_GO para sucesora y cualquier promoción |

Estos 18 escenarios son especificaciones para pruebas posteriores, **no pruebas HTTP/Angular/MySQL ni de seguridad ejecutadas** por esta tarea documental.

## 6. Auditorías A0–A5, publicación y rollback

- **A0 estática:** verificar las autoridades, prerrequisitos, 16 gates y 18 oráculos; diferencia entre baseline existente y futuro; diff de este documento más sección autocontenida en `CHANGELOG_AI.md`, sin secretos ni archivos ejecutables.
- **A1 funcional proporcional a la documentación:** comprobar coherencia G01–G16, P01–P06, N01–N12 y los contratos M02.01–05. Nunca contabilizar los escenarios propuestos como tests ya ejecutados.
- **A2 seguridad documental:** evaluar prohibiciones de cross-tenant A/B, membresía backend, JWT/RBAC, CAS, assets, CSS, idempotencia y PII redactada. No afirmar que un test runtime se ejecutó.
- **A3 integración documental:** contrastar `TemaVisual`, `ConfigEmpresa`, rutas de `EmpresaConfiguracionController`, aplicador de tema y DAG/PLAN/COLA; sin cambiar DTO, API, DB ni UI.
- **A4 publicación:** publicar exclusivamente en `dev` por fast-forward con lease CAS del HEAD. Revisar diff, integridad de encabezado y viñetas del CHANGELOG, Scope Lock y verificador V5 `completed/success` sobre SHA exacto, eventos BITACORA y readback institucional. CI backend/frontend/security sin cambios ejecutables es NO_APLICA motivado, **nunca PASS**.
- **A5 independiente:** otro de los cinco TASK_ID Javier distinto del autor inspecciona diff original, SHA y evidencias antes de registrar A5_PASS. Un A5 de esta Task sobre su propia M02.06 es inválido.

**Rollback de esta microtarea:** `git revert` normal mediante nuevo commit sujeto a Scope Lock y V5, preservando historial Git y BITACORA. No existe en esta entrega una migración, activación ni estado productivo que deshacer. Siguiente acción: obtener A5 real de M02.06 y dejar que DAG/COLA calculen por fórmula la aptitud de cada M03; si G=NO, diagnosticar causas, jamás forzar SI.
