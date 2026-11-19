# C15-02.M04.02 — estados de UI fail-closed para revisión de arquitectura

Scope SOLQARYN/dev; PROJECT_SCOPE_LOCK=STRICT. Autor DEV03; ID 6adc2e4bd8a48191ac7d8641ed95a1a3. Riesgo R2 por superficie privada de plataforma.

## A0 — impacto y contrato
- Frontend Angular 22.2.1, componente existente de revisión de arquitectura y nueva suite TestBed. Sin rutas adicionales, llamadas a infraestructura, DTO/API nuevos, migraciones, datos, secretos ni despliegues.
- Sin proveedor conectado: el componente presenta exactamente las cinco referencias locales preexistentes para preservar C15-02.M04.01. El token opcional `C1502_SUPERFICIES_FUENTE` permite probar una lectura local inyectable; no representa conexión real a Hostinger, Cloudflare, R2 o MySQL.
- Estados explícitos: cargando, vacío, error, listo y denegado. Errores genéricos sin divulgar excepciones. Reintento solo autorizado, y rechazo de ids duplicados/malformados o más de 16 registros.
- La respuesta de lectura queda invalidada cuando cambia la sesión de plataforma o se destruye la vista. Ningún estado ni valor de UI concede JWT/RBAC o permisos de tenant; el backend conserva la autoridad real.

## A1–A3 — pruebas causales
- Seis regresiones Angular TestBed: carga/vacío, éxito, error/reintento, duplicación adversarial, revocación de respuesta tardía y denegación del ámbito Empresa incluso con flag administrativo.
- A2 seguridad: fail-closed local, sin filtración de contenido ni errores. Pruebas de UI con sesión simulada; no certifican JWT firmado, pertenencia tenant A/B, HTTP real, R2 de proveedores o secretos.
- A3 GitHub Actions sobre SHA exacto: Node 24, npm 11, lint/tsc, build producción, seis pruebas nuevas y regresiones M04.01. No requiere MySQL porque el changeset no modifica EF ni DB. No se adjudica QA/PROD ni servicios reales.

## A4–A5 y rollback
- Exigir SUCCESS causal de GitHub Actions, Scope Lock y procedencia V5 en SHA exacto; verificar que CI obligatoria no fue cancelada ni saltada.
- Releer PLAN/COLA/DAG/BITACORA después de cada transición; una VALIDANDO externa no es LISTO, A5 ni autorización R3.
- A5 requiere revisor Javier de Task distinta a DEV03 con revisión verdadera de código, pruebas y límites. Rollback: git revert normal de un changeset aditivo, conservando historial, sin force-push.

## Reparación causal del control transversal ERP — 18 noviembre 2026
El job ERP - Contratos, pruebas y build falló en la primera etapa de auditoría estática al no reconocer el path privado preexistente `plataforma/revision-arquitectura-v20` dentro del conjunto de rutas autorizables. La ruta ya existe y exige `canActivate: [plataformaGuard]`, que a su vez exige autenticación y sesión global verificada mediante GET servidor. Se incorpora **únicamente este path exacto** a la lista declarativa permitida, sin añadir wildcard, quitar guard ni debilitar validadores. El resto de condiciones del auditor permanece intacto. Se exige repetir GitHub-hosted ERP, Scope Lock, V5 y los seis TestBed en el nuevo SHA. No equivale a JWT real, MySQL o QA/main/PROD certificados.
