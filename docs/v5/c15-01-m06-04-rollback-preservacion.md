# C15-01.M06.04 — Ensayo de rollback real Git y preservación de evidencias

SOLQARYN, dev, PROJECT_SCOPE_LOCK=STRICT. Autor DEV03, TASK_ID 6adc2e4bd8a48191ac7d8641ed95a1a3. PLAN D2195: Ensayar rollback o desactivación sin borrar datos legítimos. La frase PLAN E «Completa M06» es meta acumulativa: **NO** se certifica el lote ni la cobertura semántica 2.340 IDs.

## A0 — impacto y seguridad R2
Se crean cuatro artefactos: prueba Node24, workflow GitHub Actions, este documento y sección autocontenida CHANGELOG. Sin modificaciones de backend/frontend, JWT, RBAC, MySQL, proveedores, Google Sheets, migraciones, cuentas, secretos ni productivo. El entorno Git de la prueba se inicializa en carpeta temporal, sin remote y sin credenciales. Se usa un registro **sintético** de 2.340 IDs, no la tabla institucional real. No se restaura base de datos o despliegue real; el gate R3 sigue prohibido.

## A1–A3 — prueba efectiva e intentos adversariales
El job dirigido debe ejecutar node --test en GitHub-hosted Node24 con git real. Positivo: tras dos commits de baseline/desactivación y otro commit concurrente legítimo de evidencia, ejecutar git revert --no-edit del commit de desactivación; recuperar la bandera y comprobar SHA-256 idéntico de plan de 2.340 IDs, bitácora y mapa de evidencias, conservando cuatro commits aditivos y worktree limpio.

Negativo 1: rechazar rollback de commit que afecte evidencia fuera de allowlist de features/coverage.json. Negativo 2: detectar expected HEAD obsoleto sin revertir. Negativo 3: rechazar SHA malformado o worktree con cambios sin seguimiento, sin alterar HEAD.

A3 demuestra comandos Git reales con datos sintéticos; **NO_APLICA al diff** probar HTTP, JWT firmado, tenants reales, MySQL y dispositivos físicos porque no se cambia runtime. Esto no convierte en NO_APLICA ninguna obligación de seguridad o integración de los otros IDs. No simular rollback real de ERP, SaaS ni de las celdas del Sheet.

## A4, A5, rollback de changeset
Exigir job c15-rollback-2340-preservation **completed/success 4/4 y cero fallos o skips**, plus verify-solqaryn-project-scope y V5 provenance success en **SHA exacto**. Un check requerido queued, in_progress, cancelled, skipped o failed no es PASS. Bajo lock V17 fila 2195, CAS H/I y readback PLAN/COLA/BITACORA; conservar I autor. A5 obligatoria por Task Javier distinta de DEV03 tras auditoría genuina. Si CI sigue en curso dejar VALIDANDO con RUN/SHA y liberar lock, evitando la inactividad. No modificar QA/main/PROD ni pagos o datos reales; git revert ordinario después de HEAD/CAS si hay que retirar este changeset.
