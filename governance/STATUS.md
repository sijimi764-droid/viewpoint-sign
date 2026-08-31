# STATUS

**Updated:** 2026-09-01  
**Authoritative baseline:** `Warehouse_Viewpoint_Sign_System_BASELINE_v1.0_2026-09-01.md`  
**Current phase:** PoC-A / Gate G1 implementation

## Current state

- Requirements baseline v1.0: BASELINED
- HG-01..HG-08: CLOSED
- PoC-A specification v0.2: DEFINED
- G0 mathematical model v0.1: DEFINED
- Gate G0: **PASS**
- Gate G1: IN PROGRESS

## G0 executable evidence

- PR: #1 `PoC-A G0 geometry core and acceptance coverage`
- merged to `main`: `a0f6570c742089ba2711dec159b968e4824ae1f0`
- GitHub Actions: run #8
- restore: PASS
- Release build: PASS
- tests: PASS
- NetTopologySuite 2.6.0 restore: PASS
- dependency license: BSD-3-Clause; redistribution obligations recorded in `governance/THIRD_PARTY_DEPENDENCIES.md`

## G0 acceptance mapping

1. analytic ray/plane intersection — PASS
2. parallel rejection — PASS
3. behind-viewer rejection — PASS
4. finite-surface bounds/tolerance — PASS
5. local/world round trip <= 0.001 mm — PASS
6. analytic footprint projection — PASS
7. projective round trip <= 0.001 mm — PASS
8. collinearity preservation — PASS
9. polygon clipping including concave subject — PASS
10. nearest-surface ownership — PASS
11. partial rear-surface occlusion — PASS
12. degenerate/singular rejection — PASS
13. deterministic normalized output — PASS

## G1 objective

Implement the complete virtual reprojection chain for TEST-01..03:

`target glyph polygons → visible surface footprints → ownership → clipping → target-to-surface homography → surface artwork fragments → surface-to-target reconstruction → contour/gap verification`

G1 does not yet perform printing, physical installation, smartphone geometry acquisition, readability optimization, or duplicate projection.

## G1 exit rule

G1 may be marked PASS only when TEST-01..03 execute in CI and satisfy `tests/POC_A_ACCEPTANCE.md`, including reconstructed contour tolerance <= 0.5 mm, correct Gap behavior, non-overlapping ownership, no visible occluded rear fragments, and valid finite vector polygons.

## Human Gate

No new Human Gate is currently required.
