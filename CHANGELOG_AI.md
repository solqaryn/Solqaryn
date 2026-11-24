

## 2026-11-24 — DEV03 C03-03.M03.06: plazo total de lectura QA

- Acota lectura de cabeceras, cuerpo y parseo del proveedor QA con un plazo enlazado de hasta 12 segundos; conserva cancelacion del cliente y auditoria segura QA_UPSTREAM_TIMEOUT sin exponer respuesta externa.
- Extiende laboratorio HTTP JWT/MySQL 8.4 con proveedor simulado que entrega cabeceras y bloquea el cuerpo, verifica 503, auditoria persistida y recuperacion posterior. Sin cambio de roles, contratos, esquema ni activacion de proveedor externo.
- Pendientes CI exacta, evidencia de retencion y A5 independiente; no certifica LISTO. Scope Lock STRICT, solo dev.
