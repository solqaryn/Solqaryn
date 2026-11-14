# N9.7 — Postmortem current-standard

Fecha UTC: 2026-09-18
Rama: `Desarrollo`
Autoridad: `docs/VAEP_AUTHORITY.md`
Dependencia cerrada: `N9.6.H`

## Objetivo

Documentar de forma causal los problemas realmente observados durante la revalidación/cierre vigente, las soluciones aplicadas, oportunidades de mejora y deuda remanente, sin convertir el postmortem en una reimplementación de producto ni en una autorización implícita sobre Producción.

## Estado de partida

`N9.6 — Hypercare` quedó certificado current-standard con `REVIEW_FIRST`, P0=0/P1=0, gate DEV read-only verde y documentación append-only preservada. El producto permanece equivalente al functional/test head aceptado; los cambios posteriores corresponden a documentación, evidencia VAEP y mecanismos temporales de verificación ya retirados.

## Problemas observados y recuperación

### 1. Fuga de alcance en probe temporal de Hypercare

Un workflow temporal de N9.6.A apuntó a URLs de Producción pese a que la autoridad vigente era Desarrollo-only. El defecto se detectó antes de certificar LISTO. Se retiró el probe, se armó una verificación read-only estrictamente DEV y el run `35319732966` terminó `SUCCESS`, cubriendo health/readiness/latencia del backend DEV, shell/latencia del frontend DEV y comportamiento fail-closed anónimo de superficies críticas. El workflow temporal DEV se retiró después de producir la evidencia.

**Causa operacional:** faltaba una guardia de entorno suficientemente explícita en el probe temporal.

**Control preventivo:** cualquier probe temporal debe declarar hosts DEV de forma explícita, evitar credenciales y retirarse tras la evidencia. Bajo autoridad Desarrollo-only, una URL productiva debe considerarse fallo de admisión, no un objetivo alternativo.

### 2. Error interno en el primer writer byte-safe de N9.6.H

El primer workflow temporal para append-only falló por construcción incorrecta del heredoc antes de realizar el append/commit. Se aplicó `FIRST_DETECTOR_OWNS_RECOVERY`: writer retirado, versión corregida ejecutada y run `35320388749` en `SUCCESS`. El commit resultante modificó exclusivamente `TASKS.md` y `CHANGELOG_AI.md`, con `TASKS.md +10/-0`, `CHANGELOG_AI.md +11/-0`, preservación exacta del prefijo histórico y `git diff --check` limpio. El writer corregido también fue retirado inmediatamente.

**Causa operacional:** error de ensamblaje del workflow temporal, no defecto de producto.

**Control preventivo:** reutilizar el patrón probado: blobs frescos antes de armar, `read_bytes()`, assert de hash, append exclusivamente, prefijo exacto, conjunto de archivos permitido, `additions>0/deletions=0`, `git diff --check`, commit/push y retiro inmediato del writer.

### 3. Riesgo de desalineación repo ↔ control-plane durante cierres encadenados

Los receipts y commits pueden existir antes de que COLA/CONFIG/PLAN_MAESTRO/CONTROL_TOWER reflejen el mismo cierre. Si se promueve el sucesor antes del write/readback, el estado operacional queda temporalmente contradictorio.

**Control aplicado:** la promoción se trata como una transacción lógica: evidencia y receipt → transición COLA/PLAN → sincronización CONFIG/CONTROL_TOWER/telemetría → readback → sucesor dependency-valid.

## Soluciones consolidadas

- Probes de verificación: ambiente explícito, read-only, sin secretos y con retiro inmediato cuando sean temporales.
- Append colaborativo: byte-safe y demostrable; no whole-file replacement cuando exista riesgo de truncamiento o modificación histórica.
- Cierre current-standard: REVIEW_FIRST + DoD material + gates causales + P0=0/P1=0 + equivalencia/exact-head + receipt + write/readback.
- Recuperación: defecto interno detectado se corrige same-run; no se convierte en blocker externo ni se notifica si termina resuelto en la misma corrida.
- Promoción: ninguna tarea posterior se usa para ocultar un prerequisito incompleto.

## Oportunidades

1. Consolidar un verificador Hypercare DEV reutilizable, estrictamente read-only y sin permisos de escritura, para reducir workflows one-shot sin crear runtime permanente inseguro.
2. Mantener una plantilla canónica de append byte-safe validada sintácticamente antes de armar cada writer temporal.
3. Reforzar guardias automáticas que rechacen hosts productivos en workflows creados bajo una autoridad Desarrollo-only.
4. Mantener el cierre repo/Sheet como una sola disciplina transaccional con readback obligatorio antes de promover el siguiente parent.

## Deuda remanente

- La observabilidad y evidencia de post-release en Producción no quedan certificadas por DEV y permanecen fuera de la autoridad actual; no deben inferirse.
- Las oportunidades anteriores son mejoras operacionales. No justifican reabrir código de producto correcto ni degradan por sí mismas un cierre ya certificado.

## Riesgos y rollback

Los riesgos principales son fuga de alcance de entorno, corrupción de archivos append-only, drift entre evidencia y control-plane y cierres basados sólo en histórico. El rollback de este PRE es documental: si el texto del postmortem requiere corrección, se corrige la documentación; no se revierte producto aceptado para ajustar narrativa operacional.

## Estrategia de validación para N9.7

Cada microtarea B–G debe revalidar current-standard su aplicabilidad. Cuando el postmortem no implique cambios de dominio, schema, API, UI o seguridad, se documentará N/A material con equivalencia demostrada, no por simple referencia histórica. QA/CI debe confirmar que la documentación no oculta drift causal de producto y que las evidencias vigentes continúan siendo aplicables. N9.7.H sólo podrá cerrar con certificación final, P0=0/P1=0, receipt y reconciliación/readback del control-plane; si el cierre cambia estado, los registros append-only deberán preservarse con el mecanismo seguro exigido por la autoridad.
