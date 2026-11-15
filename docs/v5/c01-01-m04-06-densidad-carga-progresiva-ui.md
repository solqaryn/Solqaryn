# SOLQARYN V5 — C01-01.M04.06 densidad, carga progresiva y ligereza perceptual

**Proyecto y alcance:** solqaryn/Solqaryn, exclusivamente dev, PROJECT_SCOPE_LOCK=STRICT. **Autor:** PRIMARY DEV 04 JAVIER, TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09. Fuentes institucionales: STANDARD V5 FINAL y PLAN_MAESTRO/COLA/DAG por C01-01.M04.06. Los predecesores M04.01–05 estan LISTO y revisados A5 independientemente; la liberacion reactiva G/O=SI fue leida antes del claim.

## Entrega real delimitada

Mejora la pantalla de usuarios ya protegida sin introducir rutas, APIs ni permisos. Durante el estado cargando, el componente Angular standalone OnPush `UsuariosEstadoListadoComponent` presenta tres barras de avance visual de baja densidad y geometria estable. El contenedor esta oculto del arbol de accesibilidad (`aria-hidden=true`) mientras el anuncio textual existente usa `role=status`; nunca se simulan usuarios, datos, progreso porcentual ni autorizaciones. Los placeholders desaparecen cuando se obtiene un resultado, se vacia la pagina o ocurre un error. CSS preserva tokens visuales, limite de ancho de 28rem y lineas compactas; `prefers-reduced-motion` desactiva completamente la animacion, `forced-colors` conserva el borde y la legibilidad. Mantiene el estado informativo de solo lectura y el conteo de la pagina, no del total remoto.

**Limites:** esto mejora la percepcion visual durante una consulta existente, pero NO introduce paginacion incremental real, streaming API, virtual scrolling, medicion de Core Web Vitals ni prueba de red lenta en dispositivo real. La vista no certifica rendimientos productivos ni aislamiento backend. No cambia datos, entidades, SQL/EF, autorizacion JWT/RBAC, tenant A/B, API, jobs, secretos, QA, main o PROD.

## Validaciones causales A0–A5

- **A0:** comparar tres archivos de componente HTML/SCSS/spec, fuente institucional, predecesores, seguridad y rollback. Sin alteraciones a API/servicios o acceso a datos; CHANGELOG separado y autocontenido.
- **A1:** los seis tests Vitest previos continuan; se agregan dos casos positivos/negativos: durante carga el indicador es visual y no presenta datos antiguos; tras exito o error se elimina de DOM y no se muestran registros ficticios. Se requiere resultado real en GitHub Actions del SHA publicado; no llamar PASS a pruebas aun en progreso.
- **A2:** la vista no concede permisos, no ejecuta consultas, no deriva tenancy de HTML y no modifica guardias. `aria-hidden` evita ruido en lector de pantalla; `prefers-reduced-motion` reduce movimiento.
- **A3:** ejecutar Vitest Angular dirigido, npm lint/build y controles UI aplicables en runner GitHub-hosted. Conservar el contrato de Signals existente y verificar que no se rompe el componente parent.
- **A4:** publicacion fast-forward/CAS en dev, comparar SHA y blobs HTML/SCSS/spec, `verify-solqaryn-project-scope`, V5 provenance, frontend y catalogo Angular con `completed/success` en SHA causal. El fallo o cancelacion requerida mantiene VALIDANDO.
- **A5:** otra Task Javier distinta al autor revisa blobs, comportamiento, pruebas, seguridad, readbacks y limites antes de entornos superiores; esta Task no puede autoaprobarse.

Rollback exclusivo DEV mediante `git revert` del commit del changeset, seguido de tests y evento compensatorio append-only en BITACORA. No force-push ni manipulacion de historia.
