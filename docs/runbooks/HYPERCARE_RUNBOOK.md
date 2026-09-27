# HYPERCARE_RUNBOOK — SOLQARYN DEV

## Propósito y alcance

Ventana de observación posterior a un cambio o ensayo autorizado de `dev`. No autoriza cambios de Producción, DNS, certificados, plan, secretos, `main` ni PR #2. Los únicos endpoints públicos usados por este runbook son:

- `https://solqaryn-dev.vercel.app`
- `https://solqaryn-api-dev-fxx8.onrender.com/health`
- `https://solqaryn-api-dev-fxx8.onrender.com/health/ready`

## Roles y locks

- `OPERADOR_DEV`: mantiene el lock/lease del scope mientras exista una mutación o validación activa.
- `REVISOR`: inspecciona evidencia y clasifica P0–P3; no modifica el mismo scope en paralelo.
- `CLOSER`: libera el lock únicamente después de receipt y readback.
- `OWNER`: decide sobre cualquier acción fuera de DEV.

Regla single-writer: un scope con writer vivo y progreso material fresco no recibe una segunda escritura. Otro agente puede hacer QA no solapado, pero no mutar el mismo servicio/artefacto.

## Gate 0 — identidad y baseline

```bash
set -euo pipefail
test "$(git branch --show-current)" = "dev"
git remote get-url origin | grep -Eq '(^git@github.com:|^https://github.com/)solqaryn/Solqaryn(\.git)?$'
export FRONTEND_URL="https://solqaryn-dev.vercel.app"
export BACKEND_URL="https://solqaryn-api-dev-fxx8.onrender.com"
git rev-parse HEAD
date -u +%Y-%m-%dT%H:%M:%SZ
```

STOP si la identidad no coincide o si existe un writer concurrente sobre el mismo scope.

## Gate 1 — ventana de 15 minutos

Se realizan 15 observaciones separadas por 60 segundos. Se registran timestamp, código HTTP y tiempo total sin imprimir cookies/tokens.

```bash
set -euo pipefail
mkdir -p .vaep-hypercare
rm -f .vaep-hypercare/n823-health.tsv
for i in $(seq 1 15); do
  ts="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
  backend_ready="$(curl --silent --show-error --max-time 30 -o .vaep-hypercare/backend-ready.json -w '%{http_code}\t%{time_total}' "$BACKEND_URL/health/ready")"
  backend_live="$(curl --silent --show-error --max-time 30 -o /dev/null -w '%{http_code}\t%{time_total}' "$BACKEND_URL/health")"
  frontend="$(curl --silent --show-error --max-time 30 -L -o /dev/null -w '%{http_code}\t%{time_total}' "$FRONTEND_URL/")"
  printf '%s\t%s\t%s\t%s\n' "$ts" "$backend_ready" "$backend_live" "$frontend" | tee -a .vaep-hypercare/n823-health.tsv
  grep -Eq '"status"[[:space:]]*:[[:space:]]*"ready"' .vaep-hypercare/backend-ready.json
  grep -Eq '"database"[[:space:]]*:[[:space:]]*"connected"' .vaep-hypercare/backend-ready.json
  sleep 60
done
```

Criterio fail-closed: ningún muestreo puede devolver 5xx. `/health/ready` debe devolver 2xx y `database=connected` en todas las muestras. Dos fallos transitorios consecutivos o un fallo material reproducible son P1 como mínimo.

## Gate 2 — resumen objetivo

```bash
set -euo pipefail
awk -F '\t' '
  BEGIN { bad=0; n=0 }
  { n++; if ($2 !~ /^2/ || $4 !~ /^2/ || $6 !~ /^[23]/) bad++ }
  END { printf "samples=%d bad=%d\n", n, bad; exit bad>0 }
' .vaep-hypercare/n823-health.tsv
```

Si `bad` es mayor que cero, no cerrar hasta revisar la causa y repetir la ventana después de la corrección o recovery.

## Gate 3 — smoke posterior a la ventana

```bash
set -euo pipefail
curl --fail-with-body --silent --show-error --max-time 30 "$BACKEND_URL/health/ready"
printf '\n'
curl --fail-with-body --silent --show-error --max-time 30 -L -o /dev/null "$FRONTEND_URL/"
```

Además debe revisarse el log del deployment actual mediante la superficie autorizada de Render/Vercel. Buscar errores 5xx, restarts repetidos, fallos de conexión DB, excepciones no controladas y errores de assets/build. No copiar valores de variables de entorno ni secretos a la evidencia.

## Gate 4 — control de datos, rotación y seguridad

Durante hypercare:

- no ejecutar SQL manual contra Producción;
- no usar `avnadmin`;
- no volcar connection strings ni variables de entorno;
- no capturar JWT/cookies en artefactos;
- no deshabilitar auth, rate limit, CORS o TLS para “hacer pasar” el smoke;
- no cambiar plan, región, DNS o certificados;
- no reintentar una mutación si existe duda de idempotencia; primero clasificar y revisar.

Toda evidencia debe anonimizar identificadores de cliente/usuario cuando no sean estrictamente necesarios para demostrar el gate.

Los secretos efímeros generados para ensayos se destruyen al finalizar el proceso y nunca se versionan. Si se detecta exposición o sospecha de compromiso de una credencial real, clasificar P0, detener el runbook y solicitar revocación/rotación mediante el proveedor autorizado; N8.23.F no rota ni modifica secretos por sí mismo. Después de una rotación autorizada, repetir health/smoke e iniciar una ventana nueva de hypercare antes de certificar.

## STOP / recovery

STOP y activar recovery cuando ocurra cualquiera de estas condiciones:

- readiness pierde conexión a DB;
- 5xx reproducible o dos muestras consecutivas anómalas;
- restart loop;
- deployment/alias deja de corresponder a la versión certificada;
- aparece P0/P1 de seguridad o datos;
- se detecta Producción en el destino;
- se necesita un secreto fuera de la superficie autorizada.

Recovery de servicios DEV se rige por `docs/ROLLBACK_RUNBOOK.md`. Después de recovery se reinicia la ventana completa de 15 minutos; no se conserva como PASS una ventana anterior al recovery.

## Clasificación P0–P3

- **P0:** Producción afectada, pérdida/corrupción de datos, secreto expuesto, bypass de seguridad, indisponibilidad crítica sostenida.
- **P1:** readiness/health inestable, 5xx reproducible, restart loop, rollback/forward recovery fallido, versión/alias incorrectos.
- **P2:** latencia/degradación no crítica o evidencia incompleta sin fallo funcional material; requiere seguimiento antes de certificación si compromete el DoD.
- **P3:** observación cosmética/documental sin impacto operativo.

## Evidencia mínima

HEAD o equivalencia, deployment ids/SHA, archivo TSV de 15 muestras sanitizado, resultado de readiness final, revisión de logs autorizada, tiempos de recovery si aplica, REVIEW_FIRST, P0=0/P1=0, receipt y readback. Los archivos temporales `.vaep-hypercare/` son evidencia local de ejecución y no deben versionarse si contienen respuestas operativas sin revisar.

## Bloqueo estricto de alcance del proyecto

```text
PROJECT_SCOPE_LOCK=STRICT
EXTERNAL_PROJECT_CONTEXT=DENY_BY_DEFAULT
PROJECT_SCOPE_POLICY=docs/PROJECT_SCOPE_LOCK.md
EXTERNAL_CONTEXT_ALLOWLIST=docs/PROJECT_EXTERNAL_CONTEXT_ALLOWLIST.md
PROJECT_SKILL=.agents/skills/solqaryn-project-governance/SKILL.md
EXTERNAL_SKILL_REGISTRY=docs/REGISTRO_REFERENCIAS_SKILLS_SOLQARYN.md
LOCAL_SKILL_COUNT=1
```

Este runbook solo puede ejecutarse con contexto de SOLQARYN. No consultar ni utilizar skills, documentación, chats, repositorios, memorias o reglas de otro proyecto salvo autorización explícita del propietario o allowlist `ACTIVE`. Ante duda, fail-closed. La única skill local es `solqaryn-project-governance`; las nueve referencias externas solo se consultan en su origen original, pin y ruta registrados.

