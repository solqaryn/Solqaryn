# Fase 10 — Testing .NET 10 / NuGet — Contrato de certificación DEV

Fecha de implementación: 2026-10-10. Autoridad de código: `solqaryn/Solqaryn`, exclusivamente `dev`.

## Matriz exacta

| Componente | Base en DEV | Objetivo de Fase 10 |
|---|---:|---:|
| Microsoft.NET.Test.Sdk | 17.10.0 | **18.10.1** |
| Moq | 4.20.70 | **4.21.0** |
| Microsoft.EntityFrameworkCore.InMemory | 10.0.12 | 10.0.12 (ya alineado) |
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.12 | 10.0.12 (ya alineado) |
| xunit | 2.8.1 | **2.8.1 (v2 preservado)** |
| xunit.runner.visualstudio | 2.8.2 | 2.8.2 (preservado) |
| TFM/SDK | net10.0 / 10.0.401 | sin cambios |
| EF Core runtime / Oracle MySQL provider | 10.0.12 / 10.0.9 | sin cambios |

`xunit.v3 4.0.1` es **fuera del alcance** de este changeset. Una eventual evaluación de xUnit v3 requiere changeset, pruebas y aprobación propios, sin acoplar el cambio de framework de pruebas al runtime productivo.

## Superficies y límites

- Modificaciones exclusivamente de dependencias **de test**, workflow dedicado de Fase 10 y documentación técnica. Sin cambios de lógica de negocio, API, DTO, migraciones, esquema, EF productivo, provider Oracle, autenticación, seguridad, tenancy ni datos persistentes.
- El workflow `.github/workflows/modernization-phase10-testing-net.yml` se dispara sobre PR/push en `dev` con cambios de `backend/**`, además de `workflow_dispatch`.
- La suite unitaria completa prueba compatibilidad de xUnit v2 con el SDK18 y de los mocks Moq. La regresión dirigida ejercita contratos existentes implementados sobre EF SQLite/InMemory y Moq. La integración real Oracle/MySQL, tenancy y seguridad siguen cubiertas por `Fase 7`, `Fase 6` y `DEV - Compilación y pruebas` del **mismo SHA**.
- El gate NuGet inspecciona paquetes directos y transitivos e impide cerrar con vulnerabilidades conocidas detectadas por `dotnet list ... package --vulnerable`.
- No se alteran QA, `main`, PROD, Aiven ni los diez controladores VAEP pausados. Los reportes TRX se publican como artifacts temporales de CI.

## Aceptación técnica obligatoria

1. Confirmar versiones exactas y xUnit v2 sin `xunit.v3`, SDK 10.0.401/TFM net10.0.
2. `dotnet restore` y `dotnet build -c Release` de la solución: PASS.
3. `dotnet test` de todos los unitarios `Category!=Integration`: PASS, sin pérdida de descubrimiento, fallidos=0.
4. Pruebas dirigidas SQLite/InMemory/Moq: PASS, tests realmente descubiertos.
5. NuGet audit directa/transitiva: vulnerabilidades detectadas=0.
6. Ejecución MySQL Oracle `Category=Integration`, Fase 6/Fase 7 y gates funcionales de `dev`: PASS en el **exact-head**.
7. PR hacia `dev`, integración sin desviaciones, lectura final del HEAD y **CI post-merge del mismo SHA**: PASS.
8. P0=0, P1=0 en el alcance, sin cambios a infraestructuras o bases productivas.

El estado de cierre no se declara por este documento por anticipado: la evidencia causal (commit, ID de PR, IDs de run, recuentos y dictamen) se adjunta a la conversación del PR tras comprobar los runs en su HEAD final. No se agrega un commit documental post-merge que invalide la certificación exact-head.

## Reversión

Revertir el changeset de paquetes de prueba y workflow desde `dev`, sin reescribir historia compartida ni modificar datos persistentes. Mantener `net10.0`, EF 10, Oracle y el baseline productivo.
