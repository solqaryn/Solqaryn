# P0-001.A — Baseline exacto de DEV y mapa de evidencia (24-10-2026)

> Snapshot **inmutable**, no monitor de despliegues. Origen: plan comercial inicial vigente, microtarea `P0-001.A` (`PLAN_MAESTRO!A2:R2`). El JSON adjunto `docs/baselines/P0-001-A-DEV-20261024.json` conserva el UTC original y enlaces verificables. Nunca usar una fotografía como permiso de despliegue o certificación futura.

## HEAD y ramas (readback GitHub)

- Repositorio original: `solqaryn/Solqaryn`; HEAD base `dev=41d05dda3ffb57707275132f834c40cace3f8594` (rama protegida).
- `qa=5cedcc91bfb2b69562e7093bc1fa3d1251fa31f3`, `main=072df831585f879cfd7bb429575d6d387076d97b` (ambas protegidas; **solo lectura**, no promoción).
- Se observó además una rama temporal residual (4 ramas en total). Su nombre no se incorpora al documento operativo por el Scope Lock de identidades retiradas; queda trazable mediante [listado nativo de ramas](https://github.com/solqaryn/Solqaryn/branches), sin atribuirle trabajo activo.
- PR fusionados recientes contra `dev`: [#3737](https://github.com/solqaryn/Solqaryn/pull/3737) → `41d05dda3ffb57707275132f834c40cace3f8594`, [#3736](https://github.com/solqaryn/Solqaryn/pull/3736) → `cca60745bc6a49c14a35064a88504dcef180fa1c`, [#3735](https://github.com/solqaryn/Solqaryn/pull/3735) → `e1d7afd82ea814cc70121b866b0ce86843eb8d10`, [#3734](https://github.com/solqaryn/Solqaryn/pull/3734) → `17ca160ae5f2d1e20ef27b1820e670fbfb1099d8`, [#3733](https://github.com/solqaryn/Solqaryn/pull/3733) → `3a98afbf290e8895e177861183b623ed3cb5bed0`.
- No había PR abierto hacia dev en la consulta previa a adquirir `work/solqaryn-p0-001-a`. La rama determinista se creó desde el HEAD exacto, sin colisión de ese ID.

## CI exacto de HEAD base

Para `41d05dda3ffb57707275132f834c40cace3f8594` se consultaron checks reales GitHub: [Scope Lock SUCCESS](https://github.com/solqaryn/Solqaryn/actions/runs/40110973868/job/120922432797), [Seguridad multiempresa SUCCESS](https://github.com/solqaryn/Solqaryn/actions/runs/40110973880/job/120922433040), [Backend SUCCESS](https://github.com/solqaryn/Solqaryn/actions/runs/40110973880/job/120922433109), junto con frontend y ERP SUCCESS. Se comprobó frescura exacta de la Skill (ignorando solo su front matter YAML) y `references/architecture.md` respecto de ese HEAD; `PROJECT_SCOPE_LOCK=STRICT`, runtime `ACTIVE_DEV`, un escritor programado autorizado, VAEP y Jules retirados. Estos resultados pertenecen al **HEAD base**, no anticipan CI del PR.

## Despliegues DEV: readback real y brechas explícitas

- **Vercel, proyecto `solqaryn-dev` (lectura nativa de 12 despliegues)**: el intento más reciente observado contra `41d05dda3ffb57707275132f834c40cace3f8594` está **CANCELED** ([despliegue](https://vercel.com/solqaryn/solqaryn-dev/3q9fTQ767MQaS6DrzJAMXZw7sAdh)). El primer `READY` encontrado en esa página de resultados apunta a `e1d7afd82ea814cc70121b866b0ce86843eb8d10` ([despliegue](https://vercel.com/solqaryn/solqaryn-dev/G1WdmcE8THiDZg5VPserQRo1qjLP)), **distinto del HEAD**. `target=production` es un campo de Vercel dentro del **proyecto DEV**; no se interpreta como autorización de producción. Por tanto **NO se acredita paridad desplegada de HEAD** y **no se declara READY del HEAD**.
- **Render**: `render.yaml` en HEAD declara `solqaryn-api-dev` ligado a rama `dev`, `checksPass` y `/health`. El proveedor devolvió `no workspace selected`; se requiere un `workspaceId` confirmado por el propietario para consultar el runtime, y **no se seleccionó arbitrariamente**. Estado / SHA realmente desplegado = `NO_VERIFICADO`. Ninguna afirmación de equivalencia, ejecución, salud o migración de base de datos.
- **No se desplegó, activó, pagó ni modificó ningún proveedor**. Esta brecha de comparación corresponde a certificación `P0-001.B` y nunca bloquea registrar la captura histórica honesta de `P0-001.A`.

## Mapa de componentes y límites

| Capa | Origen vivo | Alcance para esta microtarea |
| --- | --- | --- |
| Frontend | `frontend/`, Angular 22.2.1, Angular Material/CDK | Inventario referencial; sin UI ni modificaciones |
| API | `backend/src/API/`, ASP.NET Core `net10.0` | Sin endpoints, DTO o autenticación nuevos |
| Dominio y aplicación | `backend/src/Domain/`, `backend/src/Application/` | Sin cambios |
| Persistencia | `backend/src/Infrastructure/`, EF Core 10.0.12 / MySQL Oracle 10.0.9 | Sin datos, esquemas, migraciones ni transacciones nuevas |
| Seguridad | JWT, RBAC, aislamiento multiempresa, auditoría | Se conservan comprobaciones; no sustituidas por UI |
| Infra DEV | `render.yaml`, Vercel DEV | Solo inventario; Render live no verificado; Vercel HEAD cancelado |
| Gobierno | `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_STATUS.md`, `docs/AUTOMATION_AUTHORITY.md`, `docs/AUTOMATION_3X3_OPERATING_CONTRACT.md` | Sin cambios en autoridad o en Tasks |

**Impacto:** exclusivamente documentación y regresiones de baseline; sin API/DTO, bases de datos, PII, secretos, jobs, aplicaciones, integraciones, observabilidad ni permisos modificados. Dependencia comercial: `P0-012.B=LISTO` leído en `PLAN_MAESTRO!H25` y `COLA!E25`; `P0-001.B` no se inicia hasta `P0-001.A=LISTO`.

## Pruebas negativas y cierre

1. `scripts/verify-p0-001-a-baseline.mjs --self-test`: rechaza ID o SHA inválido, check incompleto, estados de despliegue contradictorios, falso READY en el HEAD y Render sin lectura acreditada. No ejecuta servicios ni red.
2. **Negativo causal observado**: Vercel `CANCELED` con SHA de HEAD y `READY` de otro SHA ⇒ `NO_GO` para afirmar despliegue equivalente, aunque el CI base sea verde.
3. **Negativo de proveedor**: la ausencia de workspace Render verificado ⇒ `NO_VERIFICADO`, no `SUCCESS`.
4. Cierre solo con PR hacia `dev`, CI proporcional exact-HEAD, revisión independiente, merge protegido y readback institucional. Rollback: revertir únicamente este changeset documental por PR, sin tocar despliegues o datos. `P0-001.A` permanece **VALIDANDO**, nunca LISTO por publicar evidencia.
