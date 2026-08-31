# STATUS

**Updated:** 2026-09-01  
**Authoritative baseline:** `Warehouse_Viewpoint_Sign_System_BASELINE_v1.0_2026-09-01.md`  
**Current phase:** PoC-A / Gate G2 implementation

## Current state

- Requirements baseline v1.0: BASELINED
- HG-01..HG-08: CLOSED
- PoC-A specification v0.2: DEFINED
- G0 mathematical model v0.1: DEFINED
- Gate G0: **PASS**
- Gate G1: **PASS**
- Gate G2: IN PROGRESS

## G0 executable evidence

- PR: #1 `PoC-A G0 geometry core and acceptance coverage`
- merged to `main`: `a0f6570c742089ba2711dec159b968e4824ae1f0`
- GitHub Actions: run #8
- restore/build/tests: PASS

## G1 executable evidence

- PR: #2 `PoC-A G1 virtual reprojection pipeline`
- merged to `main`: `26733c7ed31079caad4ff8e31b64c29c01707d02`
- GitHub Actions: run #12 (`33421204192`)
- restore: PASS
- Release build: PASS
- G0 regression tests: PASS
- G1 TEST-01..03: PASS

## G2 objective

Prove print dimensional integrity for PoC-A:

`surface-local vector artwork → page tiling → physical PDF coordinates → registration/orientation metadata → 1:1 PDF`

Required concepts are fixed by the baseline and acceptance criteria:

- dimensions represented in millimetres internally
- ISO A4 portrait/landscape
- PDF physical conversion `72 pt / 25.4 mm`
- artwork at 1:1 physical scale
- 100 mm calibration line
- project ID, surface ID and page index on every page
- orientation and registration marks
- configurable overlap; initial default 10 mm
- complete tiling without unintended gaps

## G2 implementation policy

For PoC-A, PDF writing will use a small deterministic project-owned writer rather than introducing a layout framework. This keeps physical page dimensions and coordinates directly auditable and avoids allowing a rendering/layout library to become geometry truth.

The PDF coordinate system is an output encoding only. Authoritative artwork remains the surface-local vector geometry in millimetres.

## G2 exit rule

G2 may be marked PASS only when CI verifies the programmatically generated PDF structure and dimensions against `tests/POC_A_ACCEPTANCE.md`:

1. selected ISO page dimensions are correct
2. 100 mm calibration element encodes exactly as 100 mm within PDF-coordinate tolerance
3. artwork remains 1:1
4. page tiles cover the required surface artwork without unintended gaps
5. configured overlap is represented correctly
6. every page contains required identification, orientation and registration information

A real printer-driver scaling check remains deferred to G3 and cannot be replaced by CI.

## Human Gate

No new Human Gate is currently required for G2 software implementation and digital dimensional verification.
