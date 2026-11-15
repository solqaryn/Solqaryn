# SOLQARYN V5 — C14-03.M02.05: eventos conceptuales, idempotencia, correlación y límites de promoción controlada

**Task autora:** PRIMARY DEV 04 JAVIER, `6aeacb7820ac8191b7d768a6f33dfa09`. **Fecha:** 2026-11-15. **Repositorio único:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` y Sheet institucional `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`.

**ALCANCE DOCUMENTAL M02. NO IMPLEMENTAR EVENTOS TODAVÍA.** Se describe un contrato futuro de señales relacionadas con promoción entre entornos. No hay emisores o consumidores instalados, broker, webhook, dispatcher, suscriptor, scheduler nuevo, proceso de despliegue, aprobación real ni promoción QA/main/PROD. Ninguna señal aquí diseñada equivale a A5, JWT, RBAC, CI, autorización comercial o revisión independiente.

## 1. Autoridad, evidencia previa y fronteras de ejecución

La microtarea tiene `PLAN_MAESTRO!H=PENDIENTE/I` vacío al claim, `COLA!G=SI/O=SI` y `DAG_TECNICO!F=VALIDADO/G=SI` para su ID individual; los predecesores `C14-03.M01.01–06` aparecen LISTO con A5 independiente. Este gate permite **diseñar M02.05**; no autoriza despliegue ni código M03, y no convierte una tarea M02 hermana pendiente en cerrada. `C14-03.M02.04` tenía propietario distinto activo durante el preflight; esta entrega no toca sus archivos, estado ni scope.

Contrato institucional de salida para C14-03: distinguir `DEV`, `QA` y `PROD` por SHA exacto, configuración efectiva, controles, rollback y permisos distintos. La evidencia arquitectónica de V5 mantiene frontend Angular 22.2.1, backend .NET 10, MySQL 8.4, GitHub Actions y servidor multitenant, pero este artefacto **no ejecuta** compilaciones, migraciones, pruebas HTTP, despliegues o cambios de cuentas.

**Fronteras irrevocables:** (a) un evento no ejecuta ni autoriza publicación; (b) la fuente de verdad de estados de microtareas sigue siendo PLAN_MAESTRO/COLA/DAG_TECNICO y BITACORA, no un bus; (c) JWT/RBAC, pertenencia tenant y permiso por ambiente se resuelven server-side; (d) QA/main/PROD requieren autoridad separada; (e) ningún SHA de CI cancelado, omitido, fallido o en progreso se convierte en PASS; (f) una A5 propia está prohibida.

## 2. Catálogo futuro de ocho señales NO EMITIDAS

| Señal conceptual futura | Cuándo podría emitirse después de autorización | Qué nunca permite por sí sola |
| --- | --- | --- |
| `release.requested.v1` | Solicitud firmada para ambiente específico, alcance y artefacto versionado | Iniciar deploy, resolver permisos ni ejecutar GitHub Actions |
| `release.preflight.attested.v1` | Preflight externo acreditado: SHA, árbol, pruebas y configuración aprobados | Sustituir la lectura real de checks del SHA |
| `release.review.recorded.v1` | Revisión independiente y autorización humana real con evidencia | Auto-A5, autorizarse a sí mismo o promover desde un texto |
| `release.approved.v1` | Puerta de ambiente satisfecha y autorización específica verificada | Publicar sin validación de firma, cuota, estado y revocaciones |
| `release.rejected.v1` | Permisos, tenancy, evidencia, CI o ambiente no acreditados | Reintento infinito o cambio automático de ambiente |
| `release.completed.v1` | Despliegue futuro confirmado por readback de plataforma, SHA y salud | Declarar release basado únicamente en un mensaje o pipeline queued |
| `release.rollback.requested.v1` | Autoridad separada solicita restauración con rollback seguro validado | Restaurar o escribir datos sin backup/test y aprobación |
| `release.rollback.completed.v1` | Resultado futuro acreditado de restauración con SHA y pruebas | Borrar auditoría, ocultar fallos o reabrir un ambiente superior |

Todos los nombres son **propuestas**, no contratos C#, endpoints, webhooks ni nuevas acciones GitHub/Render/Vercel/Aiven. Se evita toda conexión a QA, main, producción, bases de datos reales y proveedores externos. No existe mecanismo de despertar o coordinar las seis Tasks mediante eventos.

## 3. Sobre de correlación futuro (quince campos)

| Campo hipotético | Fuente verificable requerida | Control fail-closed |
| --- | --- | --- |
| `schemaVersion` | Versión admitida del contrato | Rechazo de versión desconocida |
| `eventId` | ID aleatorio único de evento | No reutilizar como autorización ni RUN_ID nativo |
| `idempotencyKey` | Hash de ambiente+operación+artefacto+solicitud autorizada | Mismo key con payload distinto produce conflicto |
| `correlationRef` | Trazabilidad opaca de solicitud | Nunca contiene JWT, correo o PII |
| `causationRef` | Hecho causante realmente comprobado | No inventar un evento anterior |
| `actorRef` | Identidad autenticada en contexto de autoridad | No inferir privilegios de un mensaje |
| `tenantScopeRef` | Empresa efectiva de backend, si aplica | A no publica ni recibe datos de B |
| `targetEnvironment` | Enum dev/qa/prod autorizado por gobierno | Entorno desconocido o superior produce rechazo |
| `artifactSha` | Commit Git y manifiesto del binario exacto | Un SHA de otra rama no satisface CI |
| `configurationFingerprint` | Huella de configuración saneada | No exponer secretos ni copiar config de otro ambiente |
| `checkEvidenceRef` | Referencia de jobs GitHub exact-SHA y resultados finales | queued/skipped/cancelled/failed != success |
| `approvalEvidenceRef` | A5 ajena + autorizaciones de promoción correspondientes | No aceptar autores/revisores iguales |
| `rollbackEvidenceRef` | Snapshot, restore y versión anterior verificables | Sin plan de reversión, no promover |
| `occurredAtUtc` | Reloj del emisor real autorizado | No confundir timestamp de Sheets con tiempo real |
| `outcome` | Resultado comprobado por lectura posterior | Mensaje emitido no significa acción completada |

El sobre no se persiste ni envía en esta unidad. La publicación futura debe usar firma, caducidad, consumo idempotente con bloqueo transaccional verificable, control de cuotas, reintentos acotados y observabilidad depurada; los parámetros numéricos deben definirse conforme a políticas vigentes, sin inventar TTL ni presupuesto.

## 4. Catorce reglas de seguridad, cuotas y causalidad

- **EV01:** antes de evaluar una solicitud, autenticar actor, permiso RBAC y alcance efectivo en servidor; no usar `empresaId` cliente, dominio o nombre de archivo como autoridad.
- **EV02:** separar estrictamente DEV, QA y PROD por autorización, ramas, configuración, secretos y permisos; cero promoción implícita por fase documental.
- **EV03:** exigir coincidencia exacta entre `artifactSha` y checks requeridos `completed/success`; cancelado, omitido, fallido, en cola o ejecutándose no es PASS.
- **EV04:** A5 debe tener Task revisora Javier distinta de la autora; nunca autovalidar, ni confundir una notificación con una revisión.
- **EV05:** mismo `idempotencyKey`, mismo ambiente/artefacto y mismo contenido => un único resultado; cuerpo diferente => conflicto sin segundo efecto.
- **EV06:** un retry debe tener límite, causa y nueva hipótesis; jamás bucle automático entre Tasks o proveedores.
- **EV07:** `requested`, `preflight` y `approved` no equivalen a `completed`; el resultado requiere prueba de plataforma y readback.
- **EV08:** la revocación de acceso/rol/permiso invalida solicitudes pendientes y grants, incluso si hay mensaje previo.
- **EV09:** una solicitud QA/PROD no puede heredar datos, secreto, autorización o configuración de DEV.
- **EV10:** fallo en rollback/restore real bloquea promoción; no cambiar estado ni ocultar evidencias para forzar éxito.
- **EV11:** no emitir JWT, tokens, claves, URL firmada, payload de cliente ni PII en logs, mensajes o BITACORA.
- **EV12:** correlación y caché se delimitan por actor, empresa, ambiente, artefacto y versión; evitar replay o mezcla A/B.
- **EV13:** un proceso futuro no modifica PLAN/COLA/DAG ni despierta otras Tasks por una simple señal; toda transición institucional requiere CAS, evento y readback autorizado.
- **EV14:** event dispatcher, webhook, worker, GitHub Actions, esquema MySQL o integración de pago quedan NO_IMPLEMENTADOS hasta microtarea específica y permiso posterior.

## 5. Doce escenarios positivos/negativos FUTUROS_NO_EJECUTADOS

| ID | Positivo hipotético tras implementar | Negativo obligatorio |
| --- | --- | --- |
| P01/N01 | Solicitud DEV válida cita SHA y permiso reales | SHA ajeno a CI requerida se declara aprobado |
| P02/N02 | Revisión independiente coincide con commit y scope | Task autora se autoaprueba por evento |
| P03/N03 | IdempotencyKey repetida conserva un único efecto | Mismo key/cuerpo distinto inicia segunda promoción |
| P04/N04 | Empresa A accede a su evidencia autorizada | Evento A permite exponer datos o configuración de B |
| P05/N05 | Configuración saneada coincide con ambiente autorizado | Dev secreta/config reemplaza QA/PROD silenciosamente |
| P06/N06 | CI final `completed/success` satisface prerequisito | CI cancelled/skipped/in_progress se toma como PASS |
| P07/N07 | Permiso revocado rechaza solicitud pendiente | Un evento viejo recupera derecho de usuario |
| P08/N08 | Rollback previamente probado conserva estado válido | Error de restore aparece como `completed` |
| P09/N09 | Log depurado referencia corrección y resultado | JWT, clave o URL firmada entra al envelope |
| P10/N10 | Retry acotado con causa nueva termina seguro | Mensajes hacen reintentos perpetuos entre Tasks |
| P11/N11 | Promoción completada acredita readback de ambiente | `release.completed` sin evidencia de despliegue |
| P12/N12 | Separación de tenant y ambiente impide replay | Reuso de clave idempotente DEV en PROD |

**Ningún P/N fue ejecutado**. No hay datos HTTP, capturas de Playwright, logs de proveedor, migración, red o binarios que se presenten como PASS. Son exigencias para tareas técnicas futuras; cualquier autorización de QA/main/PROD sigue separada.

## 6. A0–A5, dependencia y reversibilidad

**A0:** diff de dos Markdown, este documento y una sección indivisible en `CHANGELOG_AI.md`, sin cambio en código, pipelines, secretos o infraestructura; autoría original de M02.04 preservada. **A1:** comprobar ocho mensajes, quince campos, catorce reglas y doce pares P/N diseñados, ninguno ejecutado. **A2:** revisar alcance y denegación JWT/RBAC, empresa A/B, ambiente, firma, idempotencia, rollback, revocación y privacidad, sin certificar runtime. **A3:** compatibilidad con M01.01–06 LISTO+A5, contexto M02.01–.04 y distinción de fuentes de GitHub/Sheets; CI .NET/Angular, EF/MySQL, Playwright y despliegue = NO_APLICA causal al diff sólo Markdown. **A4:** publicar por fast-forward/CAS en `dev`, verificar `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` en SHA exacto, comparar blobs, PLAN/COLA/BITACORA readbacks. **A5:** revisión posterior por otra Task Javier, distinta de PRIMARY DEV 04, nunca autoprobación.

**Bloqueadores posteriores:** faltan A5 propia de este documento y gates individuales de M02.06/M03; no se permite convertir eventos en autorizaciones, ni promover código, datos o infraestructura. El rollback actual es `git revert` normal del cambio documental con reconciliación de HEAD, sin fuerza ni modificación histórica de BITACORA. Ningún evento real, traslado de datos, nuevo proveedor, deploy ni operación QA/main/PROD ocurre aquí.
