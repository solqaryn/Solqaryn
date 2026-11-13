# SOLQARYN V5 FINAL — C00-02.M02.06: dependencias técnicas y contrato de salida M02

**Clase:** TIPO 1, contrato documental; no se modifica código ejecutable. **Autor:** SOLQARYN PRIMARY DEV 01 — JAVIER; TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Ámbito:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`.

**Resultado exigido (V5 FINAL):** definir cola, dependencias, autoría, estados y cierre sin duplicados. La evidencia mínima del bloque es contrato de estados y ejemplo de ciclo completo. Esta unidad **completa el contrato de diseño M02**, no implementa la cola, servidor de eventos, DB, API o un mecanismo CAS nuevo. Autoridad exclusiva: Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`; gobierno vivo `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_STATUS.md`, `docs/AUTOMATION_AUTHORITY.md`.

## 1. Condiciones observadas de entrada y trazabilidad

A la revisión del 13-nov-2026, `C00-01.M06.06`, `C00-02.M01.06` y las cinco `C00-02.M02.01–05` tenían `PLAN_MAESTRO!H=LISTO`. `C00-02.M02.06` estaba PENDIENTE sin propietario, `COLA!G/O=SI`; las fórmulas de COLA son solo criterios derivados. No existe registro individual de M02.06 en `DAG_TECNICO`: se conserva la puerta conservadora, sin convertir el objetivo de reserva en autorización de despachar M03.

| Pieza del contrato M02 | Artefacto existente | Blob SHA de lectura |
| --- | --- | --- |
| M02.01 Entidades, dueños y 12 invariantes | `docs/v5/c00-02-m02-01-entidades-ownership-invariantes.md` | `4a1f5c4641a48a8cb169ee8ef89f7e2b194c4e17` |
| M02.02 Interfaces, DTO, errores | `docs/v5/c00-02-m02-02-api-dto-errores-compatibilidad.md` | `6e8170eea8fe05bba4f5610fced1d159719e03f1` |
| M02.03 Modelo de persistencia reversible | `docs/v5/c00-02-m02-03-persistencia-indices-migraciones-datos-prueba.md` | `eb3110d591950728b325396a5cf7902d857fff3f` |
| M02.04 Configuración tipada y flags futuros | `docs/v5/c00-02-m02-04-configuracion-tipada-ambitos-flags.md` | `588527a2d66265db04ce5dfdeecd987e8d484750` |
| M02.05 Eventos, idempotencia y correlación | `docs/v5/c00-02-m02-05-eventos-idempotencia-correlacion.md` | `58ee748ae7c83bf801a3ad737f4ec43b145979f5` |
| M01.06 Reglas de salida del bloque anterior | `docs/v5/c00-02-m01-06-bloqueo-rollback-liberacion-modelo.md` | `4595a0973d95296d6a437d696d8a1ed542f94150` |

Los blobs confirman qué fuente documental se inspeccionó; no constituyen pruebas de integraciones en vivo. Si cambian, el consumidor debe releer SHA y contrato antes de adoptar conclusiones.

## 2. DAG, autoridad, límites y contrato de salida

1. **Identidad y exclusividad:** el catálogo V5 tiene 2.340 IDs; solo las seis Tasks Javier del allowlist de `AUTOMATION_STATUS.md` son ejecutores. `PLAN_MAESTRO!A` identifica la unidad, `H` es el único estado mutable y `I` el propietario real. Un revisor A5 debe ser una Task Javier distinta de la autora.
2. **Dependencias técnicas:** antes de reclamar cualquier M03, comprobar `M02.01–06=LISTO` individualmente, `C00-02.M01.06=LISTO`, requisitos de fase pertinentes, `COLA!G=SI/O=SI`, DAG VALIDADO cuando exista, owner vacío y ausencia de conflicto sobre archivos o claims. Un `G/O=SI` aislado —incluido el que se observe mientras M02.06 aún no cierre— nunca sustituye estas pruebas.
3. **Estados y eventos:** transiciones `PENDIENTE→EN_PROGRESO→VALIDANDO→LISTO`, con candidato identificable antes de A0–A3; evento por transición y auditoría con 26 campos `BITACORA!A:Z`, `EVENT_ID` e `IDEMPOTENCY_KEY` exclusivos. `RUN_ID` solo si existe ejecución nativa real. `COLA!E` conserva su fórmula y replica PLAN.
4. **Publicación:** solo commit directo fast-forward en `dev`, previa conciliación de HEAD concurrente; verificar check-runs `/commits/{SHA_EXACTO}/check-runs` con `status=completed` y `conclusion=success` para checks realmente exigibles. `SKIPPED`, `CANCELLED` o SHA antiguo no son PASS.
5. **Calidad y revisión:** A0 estática, A1 funcional, A2 seguridad y A3 integración proporcionales al diff; A4 verifica SHA, rollback y readbacks. `LISTO_DEV` requiere A0–A4 PASS y A5_PENDIENTE registrada. A5_PASS independiente es condición previa técnica a promoción, pero **no autoriza** QA/main/PROD sin orden separada.
6. **Concurrencia real:** la lectura y escritura ordinaria de Google Sheets **no son CAS atómico demostrado**; ninguna Task puede declarar que un preflight simple garantiza exclusión matemática. Si aparece colisión o sustitución de propietario sin mecanismo de comparación atómica, conservar evidencia y detener ese ID, no pisar la fila.
7. **No implementación:** el contrato no añade webhook, scheduler, fila, PR, rama, workflow, servicio, tabla SQL, feature flag activo ni permiso de red/tenant; el objetivo `MAX(24,3*N)` es meta de capacidad, no puerta de elegibilidad.

**Condición de salida de M02:** sus seis documentos integran entidades, interfaces, persistencia, configuración, eventos y esta matriz; un cierre de M02.06 no certifica ejecución de las implementaciones M03/M04 ni libera por sí solo M06.06 o fases posteriores. Cada ID futuro requiere su propio gate y evidencia A0–A5.

## 3. Ejemplo de ciclo completo (oráculo hipotético, NO evento real)

Dado un ID V5 de prueba ya existente, PLAN H=PENDIENTE, I vacío, COLA G/O=SI, prerequisitos cerrados y permiso válido, el autor registra un claim idempotente con readback; prepara su artefacto, registra VALIDANDO sin soltar la propiedad y ejecuta A0–A3 sobre el candidato. Publica un commit FF en `dev`, consulta check-runs sobre el SHA exacto y verifica A4 + readbacks antes de marcar LISTO con A5_PENDIENTE. Una Task Javier distinta evalúa A5 y registra PASS o FAIL **del SHA revisado**, sin tocar QA/PROD. Ningún paso de esta secuencia se presenta como llamada real a API/MySQL.

## 4. Casos de aceptación documentales: positivos y negativos

| Caso | Esperado |
| --- | --- |
| P01 | Seis contratos M02 observados, M01.06 LISTO y mismo ID V5: la matriz identifica responsables, fuentes y gates, sin asignar un nuevo claim |
| P02 | Estado PENDIENTE, I vacío, G/O=SI y prerequisitos reales cerrados: candidato *puede* pasar a preflight de permiso, no se marca LISTO automáticamente |
| P03 | SHA exacto y check Scope Lock SUCCESS tras FF, A0–A3 pertinentes PASS: A4/readback puede acreditarse; A5 queda para otro TASK_ID |
| N01 | M02.06 PENDIENTE o en VALIDANDO aunque M03 tenga G/O=SI: denegar liberación M03 por dependencia incompleta |
| N02 | `PLAN!I` pertenece a otro dueño o se detecta escritura concurrente: denegar segundo claim; sin CAS probado, no forzar relevo |
| N03 | Evento BITACORA truncado a 21 columnas, EVENT_ID duplicado o RUN_ID inventado: rechazar append y cierre |
| N04 | Revisor A5 coincide con autora o es cuenta externa: rechazar A5_PASS y bloquear promoción |
| N05 | Check requerido skipped/cancelled/queued o de SHA anterior: conservar VALIDANDO; no inventar A4 |
| N06 | Intento de tocar QA/main/PROD, datos reales, secreto JWT, tenant B o nueva tarea 2.341: NO_GO |
| N07 | Faltan los cambios C#/Angular/MySQL prometidos por una unidad ejecutable: documentación sola NO basta para LISTO |
| N08 | Se afirma dispatcher funcional, exclusión CAS, base restaurada o E2E real usando solo este Markdown: rechazar certificación |

Estos casos son **oráculos documentales verificables**, no sustituyen pruebas runtime cuando los cambios posteriores las exijan.

## 5. Clasificación, auditoría y rollback

**Impacto:** exclusivamente este Markdown y una entrada aditiva en `CHANGELOG_AI.md`; sin cambios en Domain, Application, Infrastructure, API, DTO, tablas, migraciones, frontend, seguridad ejecutable o proveedores. Consumidores: las seis Tasks Javier, plan, cola, DAG y revisor independiente. Riesgos: doble claim, atribución cruzada, idempotencia, fuga de secretos y elevación tenant A→B.

`requiere_dotnet=NO`; `requiere_node_angular=NO`; `requiere_mysql=NO`; `requiere_docker=NO`; `requiere_e2e=NO`; `requiere_security=SI_DOCUMENTAL`; `requiere_scope_lock=SI`. A0 revisa diff, Markdown y secretos; A1 verifica P01–P03/N01–N08; A2 aplica allowlist/independencia/JWT/RBAC/tenant/PII sin cambiar código; A3 coteja contratos M02.01–05 y PLAN/COLA/DAG/BITACORA reales. Para pruebas técnicas de runtime no afectado: `NO_APLICA` motivado, no PASS. A4 exigirá commit FF, check Scope Lock exact SHA y readback institucional; A5 queda a otra Task Javier.

**Rollback:** usar `git revert` normal de este changeset tras revalidar HEAD, sin force-push ni borrar BITACORA, conservando evidencia y autoría de otros Scopes.
