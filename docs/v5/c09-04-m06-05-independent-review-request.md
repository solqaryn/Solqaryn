# C09-04.M06.05 — Solicitud de revisión independiente del lote
Fecha: 2026-11-17. Tipo: R0 documental. NO es A5_PASS, LISTO del lote ni liberación de M06.06.

## Alcance y matriz de evidencia
Solicitar revisión independiente a una de las cinco Tasks Javier distintas de DEV04. Revisar requisitos, fuentes SHA, estados y pruebas, sin alterar API, JWT, MySQL, proveedores ni datos.

| Unidad | Estado observado | Evidencia y límite |
| --- | --- | --- |
| M05.01 | LISTO; A5 ajena documentada | 0263060c4f6042e634f26bd5f6c5119babb5bebf; pruebas xUnit, no proveedor vivo |
| M05.02 | LISTO; A5 ajena documentada | 9a0dab0695a8212b29222c68db57ade9a11beffa; run 44124315470; controlador directo, no JWT HTTP firmado |
| M05.03 | LISTO; A5 ajena documentada | a44a227f4bc4e5ee6a77762b997afadccdee86e2; run 44126557626, integración acotada |
| M05.04 | LISTO; A5 ajena documentada | 655a28e7633bd5b3d32a56a8e2e7b7cd4d731323; aceptación sintética |
| M05.05 | LISTO; A5 ajena documentada | 76ddaee9990cb1dece9af8813c62612915255b8c; run 44129286222, 11 pruebas negativas |
| M05.06 | LISTO; A5 ajena documentada | 3dd2b7be2787c87dcf021eecd628e7238503217d; run 44140941910, 4 Chromium |
| M06.01 | LISTO pero A5_FAIL | Error documental: atribución de check V5 a M05.05 sin ejecución acreditada |
| M06.02 | LISTO pero A5_FAIL | Error documental: afirmación indiscriminada de V5 SUCCESS para seis SHAs fuente |
| M06.03 | LISTO con A5 ajena | Riesgos y decisiones, sin eliminar A5_FAIL de otras unidades |
| M06.04 | VALIDANDO, owner DEV06 | SHA 36e152f4e0d7332277584ade6d2690197e826f16; MySQL 8.4 en CI al preflight; CHECK E164 y alcance de rollback sintético pendientes de valoración |

Estado obtenido de PLAN_MAESTRO H/I/J/K/L/M, BITACORA y GitHub al inicio de esta solicitud. Cada revisor debe releer fuentes y CI; menciones en notas nunca sustituyen evento idempotente ni prueba causal.

## Solicitud formal al revisor distinto de DEV04
1. Verificar M05.01–06 por SHA exacto, pruebas realmente ejecutadas y autoría independiente. No aceptar SKIPPED, CANCELLED, QUEUED, IN_PROGRESS ni FAIL como PASS.
2. Conservar A5_FAIL de M06.01 y M06.02 hasta reparación documental por su autor o rescate exclusivo, CI del SHA corregido y nueva A5 independiente.
3. Verificar el MySQL efímero y TRX real 3/3 de M06.04 solo cuando concluya CI. No presentar un ensayo de rollback como desactivación real de proveedor, webhook HMAC, JWT, RBAC, consentimiento, privacidad, duplicados o migración productiva. Mantener CHECK E164 abierto hasta corrección causal.
4. A5_PASS/A5_FAIL requiere revisor Javier TASK_ID distinto del autor y readback de la BITACORA. La solicitud M06.05 no es esa aprobación.
5. M06.06, QA, main, PROD y proveedores vivos siguen bloqueados. Defecto global P1 de la fórmula DAG con coincidencia textual A5_PASS no resuelto.

## Validación y reversión
A0: inventario de diez entregas, fuente y diferencia entre hechos/no probados. A1: positivo seis M05 y cuatro M06 inventariados; negativos dos A5_FAIL explícitos, un VALIDANDO y ningún PASS de CI pendiente. A2: límites de tenant/RBAC y privacidad registrados; sin código ejecutable. A3: HTTP/MySQL/Playwright NO_APLICA al diff documental. A4: Scope Lock, V5 y readback requeridos en SHA publicado. A5 propia de M06.05 requiere revisión posterior por otra Task Javier.
Rollback: git revert normal contra HEAD, nunca reset/force-push, manteniendo historial institucional.
