# C00-01.M01.06 — Bloqueo, reversión y salida del lote M01

**Proyecto:** `solqaryn/Solqaryn` · **Rama:** `dev` · `PROJECT_SCOPE_LOCK=STRICT` · **Fuente:** V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` y Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. **Autor:** PRIMARY_DEV_03, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`.

## Resultado y frontera

Esta microtarea delimita el contrato **documental** de bloqueo, rollback y condición de salida de `C00-01.M01`, sin afirmar cierre automático de otras cinco tareas ni de la fase `C00-01`. La salida V5 «Habilita M02» expresa una secuencia técnica, no concede permiso adicional, ni configura un nuevo bloqueo artificial en Sheets. Las dependencias **FASE** se evalúan únicamente en `COLA!G` y las puertas de `COLA!P:Q` (fórmulas); `COLA!O=SI` identifica candidatos libres, pero no sustituye autorización, preflight o controles de calidad. `PLAN_MAESTRO!H` es el único estado mutable; `COLA!E` lo refleja por fórmula. Nunca escribir manualmente `COLA!E/G/O`.

## Tabla de decisiones de bloqueo y evidencia

| Categoría | Señal observable | Resultado dirigido | Liberación verificable |
| --- | --- | --- | --- |
| Gobierno | Skill instalada distinta de canónica de dev o `PROJECT_SCOPE_LOCK` no verificable | NO_GO del cambio afectado | Readback skill exacta + Scope Lock SUCCESS de HEAD exacto |
| Identidad | TASK_ID/propietario fuera del allowlist o permiso real denegado | NO_GO de escritura/claim | Identidad y permiso real autenticados; no inferir desde is_self_owned=false |
| Concurrencia | Otro TASK_ID mantiene EN_PROGRESO o VALIDANDO del mismo ID | NO_GO para reclamar; no borrar trabajo ajeno | Readback del primer claim idempotente; retirar claim posterior con BITACORA |
| Calidad | A0 estática, A1 funcional, A2 seguridad o A3 integración sin PASS causal | Mantener VALIDANDO y corregir | Pruebas positivas/negativas reproducidas sobre candidato exacto |
| Posintegración | HEAD avanzó, CI exact-SHA requerida falló o sigue pendiente | Mantener VALIDANDO y no declarar A4_PASS | Rebase no destructivo del candidato; fast-forward; A4 y CI required SUCCESS exact SHA |
| Independencia | Autor intenta declarar A5_PASS sobre su SHA o revisor no ejecuta negativo | A5_PENDIENTE; bloquear QA/main/PROD | Otro TASK_ID autentica SHA y ejecuta revisión A5 real |
| Seguridad | JWT, BCrypt, RBAC, tenant A/B, PII o secretos con riesgo crítico sin resolver | NO_GO del componente; jamás compensar con mocks | Corrección y pruebas reales del límite afectado, sin saltar autorización |
| Proveedores | Hostinger/Cloudflare/R2 objetivo confundido con despliegue o MySQL histórico no respaldado | NO_GO de migración | Orden nueva, backup/restore, seguridad y rollback antes de cambios productivos |
| Medios | Inventario histórico de imágenes cero y alguien propone exportar objetos inexistentes | NO_APLICA histórico; bloquear export ficticio | Verificar inventario; archivos futuros exigen MIME, tamaño y control tenant |

## Procedimiento para esta unidad

1. **PENDIENTE → EN_PROGRESO:** comprobar `PLAN_MAESTRO!H`, `COLA!E/G/O`, dependencia, owner, SHA y colisiones; registrar TASK_ID + timestamp + EVENT_ID único en BITACORA.
2. **EN_PROGRESO → VALIDANDO:** terminar candidato documental y código de prueba, registrar fingerprint antes de iniciar A0–A3.
3. **VALIDANDO:** A0 revisar sintaxis, diff, secretos y dependencias; A1 validar aceptación positiva; A2 ejecutar negativos de seguridad/abuso; A3 ejecutar integración proporcional. Un contrato Node no demuestra HTTP, MySQL, Angular browser ni tenancy A/B live.
4. Publicar exclusivamente por commit directo fast-forward en `dev` cuando A0–A3 realmente PASS y HEAD revalidado. No crear PR, rama, ruleset, Tasks ni force-push; preservar concurrencia.
5. **A4:** readback HEAD SHA, parent/tree/diff, pruebas repetidas, CI GitHub Actions exact SHA, rollback y estado de PLAN/COLA/BITACORA. Registrar A5_PENDIENTE para otro TASK_ID; solo entonces `VALIDANDO → LISTO` para esta unidad, conservando toda evidencia. La transición no significa `A5_PASS` ni promociona QA/main/PROD.

## Rollback y condición de liberación de M01

**Rollback de código:** `git revert <SHA_PUBLICADO>` como commit aditivo tras revalidar HEAD y preservar commits ajenos; nunca usar `git reset`, force-push ni editar main/qa. Para error de estado en PLAN, reconciliar con evidencia y evento de corrección único de 21 campos; BITACORA es apendicular, nunca borrar ni reemplazar historia. No revertir datos/usuarios/empresas productivos desde esta microtarea.

**Condición verificable de salida del lote M01:** comprobar los seis IDs `C00-01.M01.01` a `C00-01.M01.06` en `PLAN_MAESTRO!H` individualmente con evidencias A0–A4, readback y dependencias. No inferir seis cierres del LISTO de `M01.06`, ni alterar el DAG de fases para fabricarlos. La columna `COLA!P:Q` es la autoridad técnica de puertas explícitas; si M02 tiene `SI` por diseño V5, no inventar otro lock. **A5_PASS** de revisor independiente es indispensable antes de QA/main/PROD, además de autorización separada; la documentación no ejecuta promoción.

**Límite de certificación:** esta prueba acredita un contrato documental y sus casos negativos, no la funcionalidad real de migraciones, autorización HTTP, MySQL o Playwright. Si cualquier evidencia requerida falta, registrar `NO_GO` exacto y continuar otro ID elegible sin falsos PASS.
