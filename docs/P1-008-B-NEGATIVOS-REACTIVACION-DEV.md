# P1-008.B — Negativos de reactivación de membresías (DEV)

## Autoridad y alcance
- Plan único: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`).
- Fila 41, P1-008.B, depende de P1-008.A LISTO (PR #3840, merge `906985bf19ceac05c6bd543d8d9bd413b8b9aa3a`, postmerge CI validada).
- Autora: JAVIER_CHATGPT :40, TASK_ID `6adc2e55f47c81919f6374e561687076`, GitHub `jmejia31`, rama exclusiva `work/solqaryn-p1-008-b`, desde `dev@51689ca4260db6fb702fe9d8c94da986de6d4aec`.

## Impacto y pruebas
Se reutiliza `PlataformaGestionMvpController.CambiarEstadoMembresia` en cuatro regresiones del controller real con EF Core InMemory, sin modificar código productivo. No cambian API, DTO, UI, DB, migraciones, RBAC, límites de tenants, infraestructura ni secretos.
- Usuario inactivo: equivalente HTTP 400, ni usuario ni membresía A reactivados; empresa B conserva rol y estado.
- Usuario bloqueado: equivalente HTTP 400, permanece bloqueado y sin reactivación; empresa B intacta.
- Rol A inactivo: equivalente HTTP 400, no se reactiva la membresía.
- Rol privado de B insertado anómalamente en A: equivalente HTTP 400, sin promover rol ni atravesar tenant.
En todos los rechazos se verifica que no existe auditoría de operación exitosa.
Archivo de pruebas: `backend/tests/Solqaryn.Tests/API/Controllers/PlataformaGestionMvpSecurityTests.cs`; filtro dirigido: `FullyQualifiedName~P1008B`. Esto NO certifica HTTP end-to-end ni MySQL real.

## Gates y reversión
Requiere CI dirigido exact SHA, `AUTO_CERT=PASS` confiable en Scope Lock, todos los checks obligatorios/aplicables terminales, merge protegido a dev, CI postmerge del SHA de merge, aceptación técnica de OTRO TASK_ID y readback PLAN_MAESTRO/COLA (fórmula intacta)/BITACORA. La autora no emite su propio TECH_REVIEW_PASS ni escribe LISTO. Revertir solo vía PR protegido; conservar auditoría. No QA/main/PROD, credenciales ajenas ni Tasks nuevas.
