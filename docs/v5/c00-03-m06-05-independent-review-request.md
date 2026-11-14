# C00-03.M06.05 — Solicitud de revisión técnica independiente V5

**Tipo y efecto:** documento institucional de solicitud, no revisión realizada, no mensaje enviado ni llamada entre Tasks. Preparación del gate A5 para la matriz C00-03 de cobertura. ID existente `C00-03.M06.05`, ejecutor autor `6adbb9fff1548191a1bd5b30d9e9984a`. La solicitud se hace consultable por archivo en dev y eventos idempotentes de BITACORA; **NO constituye A5_PASS ni autorización QA/main/PROD**.

## Autoridad y elegibilidad
- V5 FINAL, PLAN_MAESTRO y COLA: seis predecesoras M05.01–06 en LISTO, DAG F=VALIDADO/G=SI; fuente única sin backlog histórico. Microtarea M06.05 es documental, mientras M06.06 sigue siendo gate terminal independiente que nunca se libera por una solicitud.
- Solo un revisor Task de **otra** de las cinco IDs Javier autorizados, distinto al autor efectivo de cada unidad, puede registrar A5_PASS/A5_FAIL después de autenticarse y verificar evidencia. La cuenta GitHub compartida jmejia31 no sustituye TASK_ID. No usar Task ajena al allowlist ni emitir A5_PASS delegado.

## Paquete de fuente para revisión independiente

| Entrega | SHA de implementación revisable | Contratos y prueba que debe inspeccionar el revisor |
| --- | --- | --- |
| C00-03.M05.01 | `f22e544177d1017cd894c7984aec0edd70eb14df` | Reglas de cobertura y negativas de catálogo, duplicidad, SHA/evidencia; sin suponer datos live |
| C00-03.M05.02 | `8d6e12e88922140eca880f3f0d754a8d07dcbbbb` | HTTP TestServer 200 y 400; contrastar contrato DTO y no confundir endpoint de prueba con API real |
| C00-03.M05.03 | `f320739bb1c57c8872c49c8bb13cd38d4be89e75` | Adaptador de memoria/proyección con cross-scope, revisión obsoleta, EVIDENCE_NOT_VERIFIED; reruns seguridad/autorización PASS exact-SHA, originales CANCELLED |
| C00-03.M05.04 | `eced2da14dc3c812e137f1837e007ea6c22108c9` | Playwright `c00-03-m04-05-accessibility.spec.ts`, sesión simulada, rechazo 403, teclado/contraste |
| C00-03.M05.05 | `1f04379da266451f54dd88669e3178481a88f7d8` | Test de proyección autorizada, negativo actor/scope/tenant, mapas confiados; no JWT ni DB real |
| C00-03.M05.06 | `fe73c45f5ec1d4f8c6dfa2a69f000725898dc36a` | Playwright `c00-03-m04-06-density.spec.ts`, vista sin operaciones, responsive, foco y navegación |

**Informe de soporte:** `docs/v5/c00-03-m06-01-test-sufficiency-audit.md` (citas corregidas en commit `3a874546`) y `docs/v5/c00-03-m06-02-reproducibility-manifest.md`, también en commit `3a874546`; contienen árbol/hash y límites. El SHA nuevo de esta solicitud se verificará después del commit; los SHA históricos no quedan automáticamente recertificados por él.

## Condiciones de aceptación A5 (otro TASK_ID Javier)

1. Identificar autor real desde PLAN/I y BITACORA, verificar revisor distinto y dentro del allowlist de seis; confirmar source SHA y que archivo/test coincide con diff del commit original, no con nombre deducido.
2. Comprobar pruebas relevantes `completed/success` y `head_sha` exacto. Abrir logs de Scope Lock y los tests aplicables (.NET, Chromium, security); CANCELLED/SKIPPED/IN_PROGRESS no son PASS requeridos. Comparar al menos un caso positivo y un negativo pertinente al contrato.
3. Evaluar límites JWT/BCrypt/RBAC, tenant A/B, PII/secretos, acceso a Sheets CAS, persistencia MySQL durable, regresiones y rollback según alcance real. No convertir fixtures, TestServer o browser mock en autorización productiva.
4. Registrar en BITACORA **un** EVENT_ID e IDEMPOTENCY_KEY únicos de 26 campos A:Z por SHA revisado, con reviewer TASK_ID distinto, A5_PASS o A5_FAIL acreditado, enlaces a CI y readback; no reemplazar eventos históricos. Si A5 falla, bloquear promoción y exigir corrección causal + nueva revisión independiente.
5. Ninguna conclusión A5 autoriza modificar qa/main/PROD, reglas, secretos, cuentas ni schedules. A5_PENDIENTE permite LISTO_DEV A0–A4 verdadero, pero nunca declara A5_PASS.

## Clasificación A0–A4 y reversión de esta solicitud

`requiere_dotnet=NO_APLICA`; `requiere_node_angular=NO_APLICA`; `requiere_mysql=NO_APLICA`; `requiere_docker=NO_APLICA`; `requiere_e2e=NO_APLICA` para este **diff documental**; `requiere_security=SI` para los límites de revisión y `requiere_scope_lock=SI`. A0: diff/formato/enlaces/hashes/secretos; A1: solicitud y casos positivos/negativos exigibles; A2: independencia/tenancy/PII; A3: seis fuentes, consumidores y registro existente; A4: commit fast-forward directo, check Scope Lock/V5 provenance exact-SHA, readback PLAN/COLA/BITACORA. A5 de esta solicitud **PENDIENTE** de otra Task Javier. Revertir normalmente el commit documental si procede, sin borrar BITACORA, sin ramas ni force-push.
