# Allowlist de contexto externo — SOLQARYN

```text
PLATFORM=SOLQARYN
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
PROJECT_SCOPE_LOCK=STRICT
DEFAULT=DENY
AUTHORIZED_ORIGINAL_SKILL_SOURCES=12
```

## Regla

Toda fuente externa queda denegada por defecto excepto las doce referencias de skills autorizadas por el propietario y registradas en `docs/REGISTRO_REFERENCIAS_SKILLS_SOLQARYN.md`.

Estas excepciones permiten **consulta de instrucciones en origen**. No autorizan por sí mismas instalar dependencias, ejecutar scripts externos, copiar binarios, cambiar arquitectura, acceder a datos/secretos ni realizar acciones productivas.

## Entradas ACTIVE

| ID | Fuente exacta | Pin / resolucion | Alcance |
|---|---|---|---|
| SKREF-01 | `agentskills/agentskills` | `69ef37e9424c0a7ea9dd2293b559e43ec8176379` | Consultar especificacion oficial de formato de skills. |
| SKREF-02 | Skill Creator oficial de ChatGPT / OpenAI | Integrado en el entorno | Consultar para autoria/validacion de la unica skill local cuando la tarea lo requiera. |
| SKREF-03 | `pbakaus/impeccable` | `2149fcce39a90bb409df5f16515f316a76dc6199` | Consultar guia original de UI/producto. |
| SKREF-04 | `emilkowalski/skills` | `d23d7f88a2e21c9e4b1418c7abe420f5c1052ba7` | Consultar skills originales de motion/interaccion. |
| SKREF-05 | `Leonxlnx/taste-skill` | `ccbc15639c97057cbfcf32ecebc38ef716e4bb37` | Consultar skill original de diseno visual/marketing. |
| SKREF-06 | `blader/humanizer` | `9862685f575c65a8247f90369951df1b3416e3d6` | Consultar skill original de redaccion. |
| SKREF-07 | `blader/napkin` | `27fa60a895de4383b26a539136bc983155cb979c` | Consultar skill original de runbook/memoria operativa. |
| SKREF-08 | `alexgreensh/token-optimizer` | `37a9546b9fecba2c4e9a02ef4e90855d449bf08f` | Consultar skill original de eficiencia de contexto. |
| SKREF-09 | `JuliusBrussee/caveman` | `15581d14007fd01fb3f132016741962f34936ca2` | Consultar skills originales de compresion de contexto. |
| SKREF-10 | `angular/angular` | `6808966993bf2b2d455d9b6245e960a59a01b9a9` | Angular Developer original: consultar para tareas Angular 22, Signals, router y accesibilidad. |
| SKREF-11 | `openai/skills` | `49f948faa9258a0c61caceaf225e179651397431` | Security Threat Model original: amenazas A/B, JWT/RBAC/HMAC o límites de confianza cuando sea necesario. |
| SKREF-12 | `dotnet/skills` | `9386ed7c1148a7bef970aa0502371c982effa01c` | .NET Performance original: diagnóstico y optimización .NET con mediciones. |

## Condiciones de uso

1. La consulta debe ir al origen exacto registrado, nunca a una copia alojada en otro proyecto.
2. Debe usarse el pin registrado.
3. Debe leerse primero la ruta oficial indicada en el registro.
4. Solo pueden leerse referencias adicionales del mismo origen/pin cuando sean necesarias para la tarea.
5. Si el pin o ruta no existe, la referencia queda bloqueada; no se sustituye por una copia o version distinta.
6. Cualquier cambio de pin exige autorizacion del propietario y actualizacion conjunta del registro.
7. La autoridad final siempre permanece en la skill local y documentacion canonica de SOLQARYN.

Cualquier otra fuente externa permanece en `DENY`, incluida Playwright CLI. La autorización es únicamente para lectura de las instrucciones originales por microtarea; no instala herramientas, scripts, ejecutores, dependencias ni suscripciones.
