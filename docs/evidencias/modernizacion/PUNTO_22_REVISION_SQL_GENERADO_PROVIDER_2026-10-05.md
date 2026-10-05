# Punto 22 — Revisión del SQL generado por providers

Fecha: 2026-10-05 (UTC)  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`

## Criterio y alcance

Se separan tres preguntas que no son intercambiables: (1) si Pomelo genera y aplica su historia vigente; (2) si el baseline candidato Oracle EF10 produce un esquema físicamente equivalente al esquema canónico en la prueba dedicada; y (3) si Oracle EF8 puede sustituir a Pomelo como provider drop-in para la historia antigua. Un `false` en (3) no invalida por sí mismo la comparación dedicada (2), pero sí impide afirmar que Oracle EF8 sea drop-in.

## Evidencia

- La comparación dedicada `modernization-oracle-baseline-probe.yml`, run `37232938573`, HEAD `0e4b552ed058b538f9b7bbe38d642a28191e7720`, generó el bootstrap de `OracleBaseline` con EF Core 10.0.12 y Connector/NET 10.0.9, lo aplicó sobre MySQL limpio restaurado desde el baseline y obtuvo `ORACLE_EF10_SCRIPT_BOOTSTRAP_SCHEMA_EQUIVALENT=true`. El artefacto `oracle-baseline-candidate-37232938573` conserva SQL, fuentes de comparación y diffs.
- Ese comparador valida catálogos físicos de tablas/engine/opciones/collation, columnas/orden/tipo/nullability/default/generated/collation, índices/columnas/orden/expresión, constraints, FK, CHECK, vistas/definiciones, triggers/cuerpo, rutinas y eventos; cualquier delta hace fallar la comparación.
- Entre el HEAD del comparador y el HEAD exacto de regresión `d8cd50078f7ad44b2d8f34bd885670b10483def4` no cambió ninguna entrada del modelo/DDL: `backend/src/Infrastructure/Persistence`, `backend/src/Infrastructure/Migrations`, `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj` y `backend/src/Domain/Entities` son idénticos. Verificación reproducible: `git diff --exit-code 0e4b552ed058b538f9b7bbe38d642a28191e7720 d8cd50078f7ad44b2d8f34bd885670b10483def4 -- <rutas anteriores>`.
- En la certificación exact-head de Fase 6 `37317784220` (HEAD `d8cd500...`), Pomelo aplicó la historia vigente de 107 migraciones sobre MySQL 8.4.11 y pasó los contratos de esquema/tipos/collation/índices/FK/CHECK; su integración completa fue 28/28. Oracle EF10/net10 pasó 2,336 unitarias, 22 integraciones y 34 probes LINQ de repositorios; el run además confirmó JSON, decimal, `datetime(6)`, transacciones, retry y concurrencia.
- **Hallazgo negativo, conservado explícitamente:** el subexperimento distinto `Oracle Connector/NET 8.0.28` contra el runtime/historia Pomelo net8 del mismo run `37317784220` no es drop-in. Resultado: `freshMigration=false`, `freshError="Field 'Id' doesn't have a default value"`, `ORACLE_SCHEMA_EQUIVALENT=false`; el comparador no compara dos esquemas válidos porque Oracle EF8 no pudo aplicar la historia. El dictamen del gate conserva `ORACLE_DROP_IN=false` y mantiene a Pomelo como provider productivo certificado. Esto no se presenta como equivalencia ni como certificación de Oracle EF8.
- El candidato de modernización es la ruta **Oracle EF10 + baseline de adopción separado**, cuya generación/aplicación física sí fue certificada por el comparador dedicado. No se reescribe la historia de 107 migraciones autoridad de Pomelo.

## Dictamen

**Punto 22: CERRADO para la revisión comparativa del SQL generado**, con estas fronteras: Pomelo conserva y aplica su historia; el baseline candidato Oracle EF10 cuenta con comparación física dedicada satisfactoria; Oracle EF8 queda expresamente **no drop-in** y no se selecciona como provider estable. El run exact-head `37317784220` terminó exitosamente, pero su éxito general no borra el resultado negativo del subexperimento Oracle EF8.

No se ejecutaron DDL ni cambios en bases productivas, no se cambió el TargetFramework productivo y no se inició Fase 7.

MAPA_ARQUITECTURA: SIN_CAMBIO.
