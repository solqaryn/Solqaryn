# P1-008.A — Consulta de membresías inactivas y reactivación segura (DEV)

## Autoridad y alcance
- Plan rector: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (Drive ID `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`).
- Fila institucional: `P1-008.A` (P1, orden 39), dependencia `P0-012.B=LISTO`; `COLA!G40=SI`, inicialmente PENDIENTE sin responsable.
- Executor: `JAVIER_CHATGPT`, Task `6adc2e55f47c81919f6374e561687076`, GitHub `jmejia31`. Base `dev@30bf43749a8eb7b164a065f5aabebfa024aa0faa`.
- Rama exclusiva: `work/solqaryn-p1-008-a`. No se reclaman `P1-008.B` ni scopes ajenos.

## Revisión del runtime reutilizado
- Backend: `GET /plataforma/gestion/empresas/{empresaId}/membresias` incluye registros inactivos usando `IgnoreQueryFilters()` con filtro explícito por `EmpresaId`. Requiere autoridad global vigente, valida empresa, limita resultados a 200 y registra auditoría.
- Backend: `PUT /plataforma/gestion/empresas/{empresaId}/membresias/{usuarioId}/estado` usa transacción serializable relacional y busca exclusivamente por par `EmpresaId + UsuarioId`. Solo reactiva si usuario vigente y rol empresarial válido; no otorga rol de plataforma. Bloquea quitar al último administrador.
- Frontend: el gestor de plataforma muestra las membresías inactivas y ofrece `Reactivar acceso` mediante servicio tipado `cambiarMembresia`; al éxito recarga el listado.
- Dependencias: `PlataformaGestionMvpController`, entidades `Usuario`/`UsuarioEmpresa`/`Rol`, `PlataformaScopeService`, EF Core 10 y UI Angular. Sin migración, contrato API nuevo, servicio ajeno ni cambio de arquitectura.

## Regresiones agregadas en este scope
Archivo: `backend/tests/Solqaryn.Tests/API/Controllers/PlataformaGestionMvpSecurityTests.cs`.
1. La membresía A inactiva aparece en GET, puede reactivarse, y la membresía B del mismo usuario permanece activa y conserva su rol.
2. Un rol de plataforma no es válido como rol de membresía empresarial A; la reactivación se rechaza sin mutaciones ni registro de operación exitosa.
3. Un actor sin permiso global vigente no puede listar ni cambiar membresías y no altera los estados.

Las pruebas de este changeset se ejecutarán por CI en el SHA del PR; no se declaran PASS hasta readback de GitHub Actions. La prueba E2E con JWT HTTP + MySQL de reactivación y fallos adversariales corresponde a validación adicional posterior (P1-008.B o gate específico, según autoridad); este documento no declara su ejecución.

## Seguridad y límites
- Mantener autorización backend y frontera multiempresa, no confiar en selector visual ni JWT aislado.
- No tocar QA, main, PROD, Aiven, credenciales, datos reales, ni editar PR/ramas de otras Tasks.
- Un botón de reactivación no habilita una identidad global suspendida ni concede privilegios de plataforma.
- Para el cierre: CI exact-head, certificación automática confiable `AUTO_CERT=PASS` si aplica, prueba dirigida, merge protegido a `dev`, CI postmerge y readback de PLAN_MAESTRO/COLA/BITACORA; jamás LISTO por compilar.
- Rollback limitado: revertir este PR por vía protegida si una regresión se detecta; conservar el comportamiento productivo existente, sin tocar datos.
