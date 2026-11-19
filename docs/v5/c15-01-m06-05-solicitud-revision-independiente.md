# C15-01.M06.05 — solicitud formal de revisión independiente del lote V22

**Unidad atómica PLAN D2196:** Solicitar revisión independiente del lote cuando sea un gate. Autor DEV01 (Task 6adbb9fff1548191a1bd5b30d9e9984a). Repo solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. R0 documental. Este documento solicita una revisión genuina **sin emitir por sí mismo A5_PASS** ni registrar aprobador ficticio. PLAN E «Completa M06» es meta de fase, no resultado de este documento.

## A0 — objetos exactos sujetos a revisión

| Unidad | SHA de implementación documentado | Estado/gate observado al preparar esta solicitud |
| --- | --- | --- |
| C15-01.M05.01–06 | 7ce694ac, 531717a8, 71fae45d, 1bd41c39, 1bee9d8b, c1f717d6 (SHAs completos en el manifiesto M06.02) | LISTO con revisiones A5 independientes registradas por pares autorizados; **no** completar M05 global por mera sumatoria |
| C15-01.M06.01 | c6f2e5306bf35875f51ba9f30a74a888d91d3723 | LISTO del auditoría R0 con A5 pendiente de otra Task |
| C15-01.M06.02 | 7885e60b72a53742d2c565d8c316f49eb2ccaaff | LISTO del manifiesto R0 con A5 pendiente de otra Task |
| C15-01.M06.03 | 4245f05bf7fde483b80f105194896b6242e7d64c | LISTO, autor DEV03, A5 pendiente de revisor distinto |
| C15-01.M06.04 | No determinado al escribir | PENDIENTE: rollback/desactivación debe producir cambio y prueba propia en su ID, no heredarse de C13/C15-05 |
| C15-01.M06.05 | El SHA de esta solicitud tras su publicación | Esta unidad **no** se aprueba a sí misma |
| C15-01.M06.06 | No determinado | PENDIENTE: cierre total de bloque por sus condiciones institucionales exactas, nunca fuerza de fórmulas |

Los estados son una fotografía. Antes de A5, reviewer debe releer PLAN H/I/K/L, BITACORA por MICROTAREA_ID, COLA G/O, DAG F/G, HEAD, SHA/blobs, check-runs y candado V17 bajo exclusividad por scope. Un owner histórico no confiere derecho exclusivo a la VALIDANDO, pero un escritor activo impide intervenir.

## A1/A2/A3 — revisión negativa requerida, sin inflar resultados

La Task revisora Javier, **distinta del autor y de cualquier reparador**, deberá confirmar que el test y la evidencia refieren a cada acción concreta PLAN D, no al objetivo acumulativo PLAN E. Revisar en particular:

- M05.01/mapeo 2340: cardinalidad/orden/IDs y guardas sintácticas no prueban 2340 correspondencias semánticas entre rutas, controles y artefactos.
- M05.02 contrato/DTO xUnit: no confundir fixture, servicio Application, respuestas seguras y proveedor empresarial HTTP/JWT/RBAC/tenant real.
- M05.03 integración MySQL 8.4 efímera: prueba real de aislamiento y rollback sobre datos sintéticos; no readback Aiven, producción ni vinculación semántica completa al Sheet institucional.
- M05.04/M05.05 guardas Node sobre código/rutas: pruebas de reglas reales, no pentest, ni HTTP BOLA/IDOR multiempresa integral.
- M05.06 Playwright real en Chromium: token synthetic-NOT-JWT, page.route y viewport emulado. Reconocer 3/3 tras reparación; preservar FAILURE 2/3 anterior. Tiempos de localhost y Tab no acreditan CWV de campo, autenticación real, dispositivos físicos o WCAG completa.
- En SHAs de M05.02 y M05.06 el check transversal de módulos tuvo **completed/failure**; no se cambia a SUCCESS por prueba dirigida verde. NO_APLICA de HTTP/MySQL para documentos de auditoría R0 **no se traslada** a pruebas de componentes R2 que sí lo requieran.
- Validar source y artefactos exactos en documentos M06.01/M06.02, límites/riesgos M06.03, y los resultados de la futura M06.04 de forma independiente. No autoaprobar el gate final M06.06.

## A4 — proceso de revisión segura

En GitHub leer GET /repos/solqaryn/Solqaryn/commits/<SHA>/check-runs?per_page=100, status=completed, conclusion=success, head_sha exacto para check REQUERIDO por alcance. SKIPPED, CANCELLED, FAIL e IN_PROGRESS nunca son PASS requerido. Verificar diff y blobs; conservar los FAIL originales, revalidar si las dependencias cambian.

A5 independiente: otra Task de la lista Javier6 registra revisión de código/documento, cobertura real, negativos y decisión A5_PASS o A5_FAIL con su TASK_ID, SHA y event_id idempotente en BITACORA A:Z, bajo namedRange V17 y readback. Esta solicitud no inicia Automations nuevas ni solicita aprobación humana, y no asigna reviewer imaginario. El revisor puede rechazar, manteniendo FAIL histórico y gate DAG, y documentar hipótesis de reparación.

La salida de este documento consiste en **solicitar explícitamente y delimitar** esa revisión; no es autorización de QA/main/PROD, ni certificación de los 2340 IDs, secrets, pagos o infraestructura. Es un incremento sólo documental. Su rollback eventual usa git revert normal contra HEAD actual y exclusión, sin force-push, reset ni borrado de BITACORA.
