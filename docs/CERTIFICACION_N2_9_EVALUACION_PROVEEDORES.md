# Certificación ERP-N2.9 — Evaluación de proveedores

## Dictamen

**Estado del paquete funcional A–G: CERTIFICADO.**

N2.9 implementa y valida la evaluación factual de proveedores a partir de orden de compra + recepción materializada, sin introducir scoring, ranking, pesos o umbrales no autorizados.

## Contratos certificados

- Persistencia `EvaluacionesProveedor` con migración `20260823042000_N2_9_EvaluacionProveedorPersistencia`, snapshot Part22, preflight/postcheck y rollback fail-closed.
- API autenticada `evaluaciones-proveedor`.
- Lecturas: `Compras/Ver`.
- Generación desde recepción: `Compras/Crear`.
- Recepción obligatoriamente `Recibida` con fecha real UTC.
- Orden con `FechaEsperadaUtc` y proveedor válido.
- Auditoría estricta en generación/actualización.
- Frontend de consulta/generación alineado al contrato real.

## Evidencia de CI

| Bloque | HEAD | Development | Acceptance | Fase 8 | M13 | Recovery |
|---|---|---:|---:|---:|---:|---:|
| N2.9.C | `69419edf` | 32617575595 | 32617575668 | 32617575661 | 32617575687 | 32617575639 |
| N2.9.D | `ca03082f` | 32622074034 | 32622073980 | 32622073999 | 32622074016 | 32622073966 |
| N2.9.E | `1d7c10a9` | 32626602367 | 32626602450 | 32626602397 | 32626602394 | 32626602428 |
| N2.9.G | `19db085b` | 32627965927 | 32627965884 | 32627965969 | 32627965880 | — |
| H pre-cierre | `16f1c70c` | 32629015701 | 32629015690 | 32629015682 | 32629015708 | — |

Todos los runs enumerados están en `SUCCESS` al momento de preparar este cierre.

## REVIEW-FIRST Jules A

Issue #395 / session `2873162910990620855` / artifact `9490601902`: `COMPLETED`, patch scope-clean, base exacta `16f1c70c...`, `SELF_REVIEW_PASS_1=PASS` y `SELF_REVIEW_PASS_2=PASS` emitidos en actividades distintas. VAEP lo clasificó PASS como evidencia complementaria y liberó el lane; no sustituye la certificación canónica.

## Riesgos y límites

- No existe un score/ranking de proveedor certificado en N2.9; agregarlo requiere requisitos explícitos.
- No convertir resultados observacionales en sanciones, bloqueos o reglas financieras implícitas.
- La reversión de migración no debe destruir evaluaciones persistidas: el DownGuard exige cero filas.
- `main`, Producción, secretos, despliegues y merge del PR #2 permanecen fuera de alcance.

## Cierre

Este documento certifica la evidencia funcional A–G y prepara el cierre H. La promoción de `N2.9.H` a `LISTO` requiere que este paquete documental/colaborativo sea publicado en `Desarrollo`, que sus gates causales aplicables finalicen en verde, que `TASKS.md` y `CHANGELOG_AI.md` queden reconciliados y que no exista P0/P1 abierto.