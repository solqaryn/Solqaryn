# SOLQARYN V5 FINAL — C00-01.M06.06: cierre baseline y mapa de dependencias

**Autor:** PRIMARY DEV 06 — JAVIER, TASK_ID 6af16bbad26881918c283abe06e1f32a. **Fecha:** 2026-11-13. **Alcance:** cierre documental y mapa DAG; NO cambia ejecutables, permisos, empresas, datos, migraciones, CI, ramas, Tasks ni despliegues. Repositorio solqaryn/Solqaryn@dev; Scope Lock STRICT.

Fuentes unicas: [V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) (revisión observada AHj4eMTZVP8PJLtFSO9xuGYJ7cmYrZtO7eOD2wFdY5XYpdBVTCjx-k3CQRcSyn8uN_uxAxy3TC4DNoJzqtk3H1cWAKyiwytEz_PSmNNECzw; revision ID, no hash criptografico) y [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Plan V5 contiene 2.340 microtareas; nunca generar una 2.341.

## Inventario de estados por fase

Al reclamar esta microtarea, las **35 antecesoras** C00-01.M01.01 a C00-01.M06.05 estaban en LISTO; M06.06 estaba PENDIENTE sin dueño, COLA G/O=SI y DAG_TECNICO VALIDADO.

| Fase | Antecesoras LISTO | Estado observado |
| --- | ---: | --- |
| M01 Alcance | 6/6 | LISTO |
| M02 Contratos | 6/6 | LISTO |
| M03 Implementación | 6/6 | LISTO |
| M04 UI | 6/6 | LISTO |
| M05 Pruebas | 6/6 | LISTO |
| M06 Calidad antes de M06.06 | 5/5 | LISTO |
| TOTAL | 35/35 | Sin otro pendiente en C00-01 |

Esta constancia no reemplaza pruebas causales de las 35 unidades ni reabre sus autores. El cierre no certifica seguridad universal de PROD.

## Puerta causal extraordinaria de M04.03

El A5_FAIL historico de C00-01.M04.03 (original fb20c55e) se conserva en PLAN_MAESTRO!J22. La revisión posterior de otra Task Javier autorizada J20 registró marcador **A5_PASS_CAUSAL_M0403_SHA=2e1285e785e41d113f0a21b9e1ee0a04a3607e0f**, tras corrección 1de966000059a53ace3faddbb1db53478cf800ff del autor original J00. No se borra la falla antigua ni se interpreta un A5 de cuenta retirada.

Evidencia real: [GitHub Actions Chromium](https://github.com/solqaryn/Solqaryn/actions/runs/43495509369), job 132127434825, PASS 3/3 sobre branding positivo y casos negativos A→login y A→B con HTTP retenido; lint/build Angular PASS. Check Scope Lock del SHA 2e1285... SUCCESS; seguridad multiempresa SUCCESS. Existían otros jobs ERP fallidos/cancelados no pertinentes a la corrección de navegación: **no declarar CI global verde**. El A5 causal habilita verificar el DAG de cierre, no autoriza QA/main/PROD.

## Manifiesto de artefactos / hashes de fuente

Los siguientes son SHA de blob Git (contenido), no hashes de ejecución de tests. Releer si la rama cambia sus archivos. La firma de autoría es el TASK_ID declarado, no una firma criptográfica.

| Artefacto | SHA blob | Verificación |
| --- | --- | --- |
| AGENTS.md | 063d342891ec6e0b23b9adb195ef3b358b51548d | Seis Tasks Javier; solo solqaryn/Solqaryn dev |
| docs/PROJECT_SCOPE_LOCK.md | 7dabef6946d75e0381f121a70458240ac702a30f | PROJECT_SCOPE_LOCK=STRICT |
| .agents/skills/solqaryn-project-governance/SKILL.md | 304e400d5c0f89fcfdcc65caece86c34276734b5 | Skill canónica |
| .agents/skills/solqaryn-project-governance/references/architecture.md | 1931e1229752028229b6ad1bb3368038e2c6769b | Arquitectura |
| frontend/package.json | e6d030f643421cb343cc12617e742469e88223c4 | Angular 22.2.1 y zone.js 0.16.3 |
| backend/src/API/Solqaryn.API.csproj | 2a7b33a0974d1ebe13676593d84b458b1243e037 | net10.0 |
| backend/src/Infrastructure/Solqaryn.Infrastructure.csproj | 6210d8b12abd77171d95d17e2292c1a319b89263 | EF Core 10.0.12 / MySQL Oracle 10.0.9 |

BITACORA real usa 26 columnas A:Z, no 21. EVENT_ID e IDEMPOTENCY_KEY deben ser únicos; no sustituir RUN_ID ausente. Fuente única de estado: PLAN_MAESTRO!H, COLA!E por fórmula.

## Mapa de dependencias validado

Para C00-01.M06.06, el DAG exige que **C00-01.M05.04, M06.01, M06.02, M06.03, M06.04, M06.05** estén LISTO y el marcador A5 causal exista en PLAN!J22. Los seis requisitos directos se comprobaron en LISTO, además de los 35 anteriores. El DAG F=VALIDADO/G=SI y COLA G=SI/O=SI fueron observados antes del claim.

La búsqueda de dependientes con C00-01.M06.06 como prerequisito dio cuatro salidas:

| Siguiente gate | Requisitos propios que deben cumplirse | Despacho |
| --- | --- | --- |
| C00-02.M01.06 | C00-02.M01.01 a .05 LISTO | Recalcular DAG/PLAN/COLA; no emitir cierre automáticamente |
| C00-03.M01.06 | C00-03.M01.01 a .05 LISTO | Recalcular, probar y auditar su propia fase |
| C00-04.M01.06 | C00-04.M01.01 a .05 LISTO | Revalidar dependencias y autoría |
| C01-01.M01.06 | C01-01.M01.01 a .05 y C00-03.M01.06 LISTO | Mantener bloqueada hasta cumplir TODOS sus prerequisitos |

Los cuatro gates eran NO en DAG antes de cerrar M06.06. El resultado posterior debe leerse de fuentes vivas, nunca presumirse. N=6 Tasks Javier habilitadas no crea permisos ni libera reservas automáticamente.

## Matriz de contradicciones

| Contradicción | Evidencia autoritativa | Acción |
| --- | --- | --- |
| A5_FAIL histórico frente a corrección causal | J22 incluye ambos marcadores, J20 A5 independiente real | Conservar falla original, aceptar nuevo gate causal limitado |
| BITACORA 21 columnas en textos antiguos | A1:Z1 físico, 26 campos | Append íntegro sin truncar |
| Ocho Tasks o cuenta externa en notas legadas | AGENTS.md/Scope Lock 6-Javier-only | Excluir cuentas retiradas; no reasignar ni activar |
| SKIPPED, CANCELLED o IN_PROGRESS en CI | GET check-runs por SHA exacto | No tratar como PASS de prueba requerida |
| COLA O=SI frente a prerequisito ausente | PLAN H/I + DAG F/G + prueba causal | NO_GO al claim o cierre afectado |
| LISTO_DEV frente a producción | V13 A0-A5 y autorización explícita | No QA/main/PROD, secretos ni DB real |

## A0–A5 proporcionados al cambio documental

**A0:** inspección estática del inventario y diff, coincidencia de las seis fases 35/35, coherencia de blobs, secretos y permisos; sin nuevos ejecutables. **A1:** caso positivo 35 LISTO + seis requisitos directos + J22 causal; caso negativo: negar cierre si falta cualquiera. **A2:** proteger JWT/RBAC/tenant: ningún nuevo permiso, migración o acceso; la revisión del navegador es puntual. **A3:** contrastar DAG, PLAN y COLA, y consumidores downstream, sin auto-despacho. **A4:** SHA/parent/diff del commit documental, Scope Lock SUCCESS exact-SHA, recálculo del DAG y readback PLAN/COLA/BITACORA. **A5:** PENDIENTE a otra Task Javier, no autopromoción. Requiere .NET/Node/Angular/MySQL/Docker/E2E nuevos: NO_APLICA a este cierre documental.

**Rollback:** git revert normal del commit y conciliación del estado institucional idempotente (sin borrar eventos), nunca force-push. No crear PR ni rama.
