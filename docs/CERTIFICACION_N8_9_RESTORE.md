# Certificación ERP-N8.9 — Restore real en Desarrollo

Fecha de revalidación current-standard: 2026-09-17  
Rama: `Desarrollo`  
Autoridad operativa: `docs/VAEP_AUTHORITY.md`  
Scope: **Desarrollo únicamente**  
Producción: **FUERA DE ALCANCE**

## 1. Objetivo certificado

N8.9 exige demostrar que un backup no se considera válido sólo por existir: debe ser restaurado materialmente y la restauración debe probar integridad y capacidad de arranque. La evidencia current-standard demuestra una restauración lógica real del material generado por M11 sobre MySQL descartable, con verificación de datos, referencias externas inventariadas y API ejecutándose contra la base restaurada.

Este cierre **no** afirma ni ejecuta un restore destructivo del servicio administrado Aiven. La prueba causal restaura el backup cifrado en una base MySQL aislada y descartable de Desarrollo/CI; esa es la superficie segura autorizada para demostrar recuperabilidad sin tocar datos productivos ni alterar el servicio administrado.

## 2. Cadena current-standard A–G

- `N8.9.A — PRE`: `LISTO`; inspección dirigida de alcance, dependencias, riesgos, rollback y criterios de aceptación.
- `N8.9.B — DOMAIN`: `LISTO / N_A_GROUNDED`; restore es capacidad operativa/CI, no un agregado o contrato de negocio nuevo.
- `N8.9.C — DB_MIG`: `LISTO`; no requiere migración nueva; el restore reconstruye y valida la persistencia existente.
- `N8.9.D — BACKEND_API`: `LISTO`; la API arranca contra la base restaurada y `/health` + `/health/ready` verifican el runtime y conectividad DB.
- `N8.9.E — FRONTEND_UX`: `LISTO / N_A_GROUNDED`; no se expone una operación destructiva de restore dentro de la UI del producto.
- `N8.9.F — SEC_AUDIT`: `LISTO`; cifrado, permisos, retención y guards fail-closed certificados.
- `N8.9.G — TEST_CI`: `LISTO`; bundle causal de restore + equivalencia current-standard certificado.

Todos los cierres A–G tienen REVIEW_FIRST `PASS`, `P0=0`, `P1=0`, receipt y readback.

## 3. Evidencia causal de restauración

Workflow: `M11 - Backup y restauración en Desarrollo`  
Run: `35223693868`  
Job: `105209822216` — `Backup cifrado y restore MySQL descartable`  
HEAD probado: `ab7b5a35cdc91312254b8a96b13ac24c53e28f44`  
Conclusión: `SUCCESS`

La corrida ejecutó materialmente:

1. construcción del esquema completo desde las migraciones existentes;
2. carga de registros y referencias sentinel;
3. backup transaccional;
4. cifrado del material y checksum;
5. comprobación de que no persistieran `.sql` o `.tar.gz` planos;
6. restore en una base MySQL descartable;
7. validación del reporte de integridad;
8. validación de Producto, ProductoImagen y CompraDocumento sentinel después del restore;
9. arranque Release de la API usando la base restaurada;
10. `/health` y `/health/ready` exitosos;
11. prueba negativa que rechaza backup de Producción;
12. prueba negativa que rechaza un destino de restore no descartable.

Por tanto, el backup probado sí atravesó el ciclo `backup → decrypt/checksum → restore → integridad → runtime`, y no se certifica por mera existencia.

## 4. Equivalencia current-standard

La reutilización del gate causal no es histórica por confianza implícita; está atada a equivalencia demostrada del scope:

- workflow M11 probado/current: `5cba214846ace7c45984d23b3e0db013404c3f01`;
- `scripts/m11_backup_desarrollo.sh` probado/current: `c160f588f465bc4b69f447225825eccef79ce6dd`;
- `scripts/m11_restore_desarrollo.sh` probado/current: `8dad281093d69eb2c3e55722516ac63f35bf6968`;
- delta de migraciones desde el gate causal: `0`;
- la equivalencia backend/API posterior quedó certificada en N8.9.D usando el backend endurecido current-standard y verificando que los cambios de `Program.cs` no alteraron el registro de DB ni la semántica de `/health`/`/health/ready`;
- no se extiende esta equivalencia a archivos o comportamientos fuera del scope N8.9.

## 5. Seguridad y rollback

Controles certificados:

- backup cifrado; no persiste dump SQL o tar plano;
- artefactos con permisos restrictivos y retención controlada;
- metadata exige `encrypted=true` y `productionInScope=false`;
- restore sólo sobre target descartable y con guard explícito `ALLOW_DESTRUCTIVE_RESTORE=YES_M11`;
- backup que parezca Producción falla cerrado;
- destino de Desarrollo no descartable falla cerrado;
- el reporte de restore exige `productionTouched=false`;
- el workflow usa `contents: read`;
- no se exponen secretos en el repositorio ni se añade UI destructiva.

Rollback de la prueba: descartar la base/contenedor de restore aislado. N8.9 no introduce migración, DDL, contrato de dominio ni endpoint nuevo que requiera rollback de producto.

## 6. Relación con Aiven y N8.8

N8.8 certificó el mecanismo de backup/restore y la prueba read-only del proveedor Aiven, incluida visibilidad de backups/PITR/capacidad, sin ejecutar un restore destructivo del proveedor. N8.9 agrega la exigencia de **restaurar materialmente el backup** y la satisface mediante el restore real y aislado de M11.

No se afirma que Aiven haya sido clonado, forkeado, restaurado o modificado. Hacerlo excedería el scope seguro actual y requeriría una autorización específica distinta.

## 7. Documentación aplicable

Runbook/base técnica reutilizada: `docs/FASE_M11_BACKUPS_RESTAURACION_DESARROLLO.md`.

No aplica crear OpenAPI, ADR o ERD nuevos porque N8.9 no añade contrato HTTP, decisión arquitectónica de producto, entidad de dominio ni cambio de esquema. Esta certificación y el runbook M11 cubren operación, rollback y evidencia.

## 8. Dictamen

El alcance funcional/operativo N8.9.A–G cumple current-standard con `P0=0/P1=0`: existe una restauración lógica real y reproducible sobre MySQL descartable, con integridad de DB/referencias, API saludable y guards fail-closed.

`N8.9.H` sólo puede pasar a `LISTO` después de reconciliar de forma aditiva/history-preserving `TASKS.md` y `CHANGELOG_AI.md`, ejecutar REVIEW_FIRST documental final con `P0=0/P1=0`, persistir receipt y efectuar write/readback del control-plane.

No se tocaron `main`, Producción, PR #2, secretos, DNS/certificados ni datos productivos.
