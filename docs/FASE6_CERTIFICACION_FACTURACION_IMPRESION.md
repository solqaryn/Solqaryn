# FASE 6 — Certificación de facturación e impresión

Fecha: 27 de julio de 2026. Rama: `Desarrollo`. Producción permaneció congelada.

## Resultado

Se implementaron A4, Carta, Legal, Oficio, A5, POS 58 mm y POS 80 mm. El formato no modifica ni recalcula la factura; todos consumen el mismo snapshot fiscal. A4 permanece como documento oficial para correo, WhatsApp y enlaces públicos.

| Código | Dimensión | Tipo |
|---|---:|---|
| `a4` | 210 × 297 mm | Página fija |
| `carta` | 215.9 × 279.4 mm | Página fija |
| `legal` | 215.9 × 355.6 mm | Página fija |
| `oficio` | 215.9 × 330.2 mm | Página fija |
| `a5` | 148 × 210 mm | Página fija compacta |
| `pos58` | 58 mm | Rollo continuo |
| `pos80` | 80 mm | Rollo continuo |

## Implementación

Catálogo backend, generador QuestPDF, endpoint de formatos, descarga auditada por perfil, selector de papel, preferencia local, impresión del PDF real, vista previa responsive y compatibilidad con integraciones A4 existentes. No se requirió migración.

## Certificación

Commit funcional: `14bf32069f9d87f731e59f230b9e9f5f16ade14e`.

- Compilación `30295557180`: **success**.
- Aceptación `30295557155`: **success**.
- Auditoría `30295557157`: **success**.

```text
51 pruebas
51 aprobadas
0 fallos
0 errores
0 omitidas
```

Artefacto `desarrollo-aceptacion-integral`, id `8664760725`, SHA-256 `f7e2ca67e311f43f9e13fedcce57fc43cb9b93da314827587226de922694b640`.

Incluye siete PDFs y ocho capturas. Los PDFs fueron renderizados e inspeccionados sin recortes, superposiciones, glifos rotos ni pérdida visible de datos.

## Pendiente físico

Solo en `SOLQARYN_desarrollo`: impresoras reales, drivers, conectividad, márgenes, densidad, corte, avance, escalado y ancho imprimible.

## Cierre

La Fase 6 queda completa y certificada. La siguiente etapa es la Fase 7 — Envío de correo. No autoriza merge, despliegue ni modificación de Producción.
