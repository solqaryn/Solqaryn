# Matrices de evaluación de interfaces — Gobierno canónico

## Propósito
Este árbol define el contrato verificable de cada interfaz de Solqaryn. Ninguna pantalla, diálogo de negocio, shell o componente interactivo con contrato propio se considera certificado sólo porque exista o compile.

## Orden obligatorio de trabajo
1. Inventario y baseline arquitectónico completo.
2. Catálogo y matrices de todas las interfaces existentes.
3. Arquitectura objetivo padre→hijo y contratos fuente-de-verdad.
4. Limpieza/refactor sólo contra evidencia del inventario y de las matrices.
5. Certificación funcional, RBAC, seguridad, responsive, accesibilidad y regresión.

Está prohibido eliminar código, rutas, tablas, campos, endpoints o documentación únicamente por parecer obsoletos. Debe demostrarse ausencia de uso/dependencia y existir rollback o recuperación segura.

## Estados de una matriz
`BASELINE_CREATED → LEVANTAMIENTO_COMPLETO → SPEC_COMPLETE → IMPLEMENTATION_REVIEWED → CERTIFIED`.

`CERTIFIED` exige evidencia material; un Markdown por sí solo no certifica nada.

## Fuente de verdad
- BD: integridad, constraints, persistencia y relaciones.
- Backend: autorización, tenant, reglas de negocio, transiciones, cálculos, validaciones autoritativas e idempotencia.
- Frontend: presentación, interacción, validación UX y accesibilidad. Nunca es autoridad de seguridad o negocio.

## Estructura documental objetivo
- `01_ACCESO`
- `02_INICIO`
- `03_CATALOGO`
- `04_CLIENTES`
- `05_PROVEEDORES`
- `06_INVENTARIO`
- `07_COMPRAS`
- `08_VENTAS`
- `09_FACTURACION`
- `10_RESERVAS`
- `11_CAJAS`
- `12_FINANZAS`
- `13_REPORTES`
- `14_ORGANIZACION`
- `15_SEGURIDAD`
- `16_BUSQUEDA`
- `17_SHELL_NAVEGACION`
- `18_COMPONENTES_COMPARTIDOS`
- `19_MODALES_ALERTAS`

Las carpetas funcionales se materializan al inventariar interfaces reales. No se crean matrices ficticias para superficies inexistentes.
