# SOLQARYN — Sincronización documental del plan V5 FINAL (2026-11-12)

> Estado de sincronización de referencias. Este archivo no es una nueva autoridad de ejecución ni una copia del plan; el estándar canónico y los permisos vigentes siguen prevaleciendo.

## Fuentes únicas
- Documento rector V5 FINAL: https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit
- Hoja operativa: https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit
- Rama de referencia de código: `solqaryn/Solqaryn@dev`.

## Integridad y habilitación
- 16 bloques, 65 fases, 390 microfases, 2.340 microtareas e IDs únicos; PLAN_MAESTRO y COLA comparten la misma secuencia.
- `PLAN_MAESTRO!H` es la fuente del estado; `COLA!E` lo replica sin estados heredados.
- `COLA!P2:Q66` determina las puertas de las 65 fases a partir de las dependencias explícitas de `FASE` presentes en `PLAN_MAESTRO!G`. La columna `COLA!G2:G2341` usa esas puertas; `COLA!O` selecciona únicamente tareas PENDIENTE sin responsable con dependencia satisfecha.
- El texto descriptivo `LOTE ANTERIOR` y `BLOQUE` se conserva en el plan para trazabilidad, pero no impone serialización de lotes ni cupo de seis. Las dependencias `FASE` siguen siendo obligatorias; no se habilitan fases dependientes antes de cerrar las fases explícitas.
- Estado comprobado tras sincronización inicial: 36 elegibles y 2.304 aún dependientes. El número de elegibles es dinámico, no garantía de 36 ejecutores simultáneos.
- La selección física, los permisos, los controles A0–A5, la disponibilidad del proveedor y la exclusión de ID duplicado siguen sujetos a su gobernanza vigente. No se aumenta ni modifica ninguna capacidad física.

## Medios inexistentes
- No asumir que hay imágenes, fotos o archivos históricos que migrar. Primero verificar inventario real. Un inventario cero permite documentar el caso de migración histórica como no aplicable; no obliga a copiar, exportar, calcular checksum de objetos inexistentes ni crear datos ficticios.
- La implementación futura de cargas de imágenes y archivos permanece válida con seguridad, validación MIME/tamaño, control por tenant, pruebas y rollback proporcional. No hay autorización para cambiar datos de usuarios/empresas ni migrar la base de datos por esta sincronización.
- La fase `C10-03` se aclaró en el documento canónico y en las 36 filas correspondientes de PLAN_MAESTRO. Los IDs se conservan.

## Congelación operativa
- Las seis Tasks Javier permanecen `is_enabled=false` por orden del propietario. No se modificaron sus IDs, RRULE, prompts ni vigilancia. La pausa anterior prevalece hasta orden posterior expresa; la supervisión entre compañeras no autoanula el hold.
- Las dos Tasks Alex son de otra cuenta y su estado de pausa requiere confirmación de su propietario; no inferir que están pausadas ni modificarlas desde Javier.
- Esta nota no reactiva tareas ni modifica Jira, Render, Vercel, `qa`, `main` o PROD.

## Recuperación y rollback
- Backup nativo de documento previo a la edición: https://docs.google.com/document/d/1tx3ie16MZKsgODo35Ly3i6hB1gttygGwRG0iaaYFLvc/edit
- Backup nativo de hoja operativa previo a la edición: https://docs.google.com/spreadsheets/d/1ZSHAYz3YSYLmzjqXgPtLBLaXKMUa33QuBGtY3ZvTc3U/edit
- HEAD de dev antes de este commit: `86d5519d9dc0cdb7246da1d603d13be7c709db30`. La historia de Git conserva el estado anterior.
- El cambio documental no equivale a certificación de código, nueva autorización productiva ni cierre de alguna microtarea.


## Orden posterior expresa de arranque V5 — 2026-11-12 (prevalece sobre la pausa del 20:56)

- El propietario AUTORIZA la reanudación de las seis Tasks JAVIER_CHATGPT únicamente después de certificar todos los checks requeridos en SUCCESS en el SHA exacto del nuevo commit base de cierre. Hasta el readback de reanudación, los seis `is_enabled=false` continúan describiendo el estado físico real; la pausa anterior es HISTÓRICA y deja de ordenar la ejecución una vez cumplida esta condición.
- Nueva fuente de trabajo: Google Doc canónico `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, V5 FINAL; Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. V5 contiene 2.340 microtareas `C00-01.M01.01` a `C15-05.M06.06`, 16 bloques, 65 fases y 390 microfases. El antiguo Plan Comercial V3/V13 y sus IDs históricos no son fuente de nuevos claims; V13 A0–A5 conserva sólo el protocolo de calidad y ejecución.
- `PLAN_MAESTRO!H` es estado único editable con evidencia y responsable; `COLA!A:O` son fórmulas y `COLA!P:Q` calculan dependencias explícitas por fase sin tope de seis. Candidata real: PENDIENTE, sin responsable, `COLA!G=SI` y `COLA!O=SI`, permisos reales, evaluación de factibilidad y exclusión por TASK_ID/ID. No reclamar automáticamente todo lo que esté en SI ni crear duplicados; múltiples IDs independientes pueden avanzar en paralelo.
- Mismo contrato V13: tareas individuales de tamaño ejecutable; PENDIENTE → EN_PROGRESO → VALIDANDO → LISTO únicamente con A0–A4 PASS, commit fast-forward en dev, CI causal SHA exacto y readback PLAN/COLA/BITACORA; A5 por otro TASK_ID para promoción QA/main/PROD, nunca autopromoción. Meta 1 LISTO, hito 3 no techo; continuar mientras haya tiempo/proveedor, dependencias y seguridad. No crear ramas/PR, no modificar QA/main/PROD ni datos existentes de usuarios/empresas.
- RESTAURAR vigilancia y recuperación cruzada exclusivamente entre seis Tasks Javier, IDs y horarios originales :00/:10/:20/:30/:40/:50. Una Task habilitada comprueba los seis IDs y puede intentar `is_enabled=true` UNA vez por compañera pausada si tiene permisos, no existe nueva orden de pausa ni acción humana obligatoria sin resolver; releer estado y registrar evidencia o error. Nunca suplantar, aprobar permisos ni reactivar Tasks de Alex desde Javier. Conservar sus dos IDs :05/:35 para gestión autónoma de su propietario y recuperación mutua sólo en esa cuenta.
- La eliminación física de filas/columnas auxiliares está verificada: DASHBOARD 40×10; CONFIG 150×3; LEYENDA 80×3; TAREAS_PROGRAMADAS 20×26; AUTOMATIZACIONES 20×18; PLAN_MAESTRO 2341×15; COLA 2341×17. BITACORA no tiene límite de crecimiento por esta decisión. Ningún bloqueo estructural impide crecimiento legítimo de BITACORA.
- C10-03 no requiere migrar imágenes inexistentes: inventario real cero → no hay exportaciones ni checksums históricos. Mantener capacidad de archivos futuros segura. La tarea `C15-05.M06.06` es terminal y sólo se habilita con evidencia de cierre de las 2.339 anteriores.
- Jira `KAN-4` es tablero informativo, NO fuente de backlog duplicado ni gate para LISTO. Los respaldos de la estructura y de los seis prompts antes de reactivación quedan preservados; no cambiar títulos, RRULE, zonas, roles, IDs, parámetros de razonamiento ni vigilancia existente fuera del ajuste temporal de pausa.
- Validación de arranque: CI exact-HEAD requerida = SUCCESS, Scope Lock SUCCESS, fuente Google Sheet íntegra, conectores autorizados, y readback de cada Task tras reanudar. Un check SKIPPED puramente auxiliar se identifica y no se falsea como PASS. Un fallo crítico o acciones humanas pendientes bloquean sólo la operación correspondiente.
