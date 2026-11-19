# C13-04.M06.04 — ensayo real de desactivación y reversión visual

SOLQARYN / solqaryn/Solqaryn@dev / PROJECT_SCOPE_LOCK=STRICT. DEV01, Task 6adbb9fff1548191a1bd5b30d9e9984a. PLAN D2015: Ensayar rollback o desactivación sin borrar datos legítimos. PLAN E es meta acumulativa de M06, no certificación de la fase.

## A0: alcance y riesgo R2
Cuatro archivos de cambio: script Node, workflow GitHub Actions, este documento y sección de CHANGELOG. Sin cambios a frontend/src, backend/src, MySQL, JWT/RBAC, infraestructura, cuentas, Tasks físicas ni producción.

El ensayo realmente ejecuta git init, git commit y git revert --no-edit en un repositorio Git efímero sin remote, con bandera visual sintética. Preserva cambios legítimos posteriores en branding A/B, manifiesto de capturas y bitácora sintética, comparando SHA-256.

## A1, A2, A3: integración y adversariales
1. Positivo: revert habilita el catálogo visual sin eliminar activos, preserva tenant A/B, evidencias y evento V1; repositorio limpio e historia aditiva de cuatro commits.
2. Negativo: se rechaza antes de escribir el revert de un commit que cambia tenant/bitácora fuera de la allowlist de rutas.
3. Negativo: expected HEAD obsoleto falla sin alterar el estado.
4. Negativo: SHA malformado y worktree sucio rechazan rollback de manera conservadora.

A3 es integración auténtica con Git en Linux GitHub-hosted, no mock del comando. Los datos y las banderas son sintéticos para evitar acción irreversible R3. No acreditar rollback de producción, dispositivos físicos, Web Vitals, JWT/HTTP/MySQL ni fidelidad visual integral. Persisten los tres fallos de transferencias del check transversal M05.06: este test no los corrige ni convierte en PASS.

## A4, A5 y límites
Exigir workflow V22 C13-04.M06.04 visual revert isolated completado en success 4/4 exact-SHA, Scope Lock y V5 procedencia exact-SHA, CAS/readback PLAN/COLA/BITACORA, namedRange V17 fila2015. SKIPPED, IN_PROGRESS, FAIL y CANCELLED requeridos no equivalen a PASS.

A5 por otra Task Javier distinta de DEV01 y de cualquier reparador, con auditoría independiente del código y negativos. LISTO de este ensayo no es certificación de M06.06 ni QA/main/PROD. Si hiciera falta revertir este changeset en dev, usar git revert normal tras HEAD y lock verificados; nunca force-push, borrar bitácora o afectar datos reales.
