# PoC-A Acceptance Criteria v0.2

## Gate G0 — mathematical unit tests

G0 passes only when all deterministic geometry tests pass.

Required tests:

1. Ray-plane intersection returns the analytically known point.
2. Parallel ray/plane is rejected without NaN/Infinity propagation.
3. Intersections behind the viewer (`t <= 0`) are rejected.
4. Point-in-surface accepts interior and boundary-with-tolerance cases and rejects exterior points.
5. 3D world point ↔ surface-local `(u,v)` round-trip stays within `0.001 mm` for PoC-scale coordinates.
6. Projection of a finite installation surface to the target plane produces the analytically expected footprint for an axis-aligned case.
7. Target-plane point → installation-plane local `(u,v)` → target-plane round-trip stays within `0.001 mm` for deterministic samples.
8. Projective mapping preserves collinearity on each plane within numerical tolerance.
9. Polygon clipping returns the correct glyph/surface-footprint intersection for known convex and concave glyph test polygons.
10. Where two projected surface footprints overlap, the nearest physical surface owns the overlap region.
11. A partially occluded rear surface retains only its actually visible target-plane region.
12. Degenerate surface definitions and singular projective mappings are rejected with explicit diagnostics.
13. Repeated execution with identical input produces byte-equivalent normalized geometry output where serialization order is defined.

## Gate G1 — virtual reprojection

For each TEST-01..03:

- project each finite installation surface to its target-plane visibility footprint
- clip target glyph polygons against those footprints
- resolve visible ownership in overlapping regions
- reverse-map the owned vector fragments to installation-surface coordinates
- render those fragments back from the same reference camera
- compare the reconstructed target with the original target representation

Initial engineering acceptance for geometry, before human readability calibration:

- no unexplained missing region when test geometry is defined to provide full coverage
- reconstructed contour positional error <= `0.5 mm` on the virtual target plane for deterministic contour samples
- TEST-01 must produce no Gap
- TEST-02/03 Gap must equal only intentionally uncovered target regions
- target-plane ownership regions must be non-overlapping after simple visibility resolution
- occluded rear fragments must not appear in reference reconstruction
- output vector polygons must remain valid (closed, finite coordinates, non-degenerate area unless explicitly representing a boundary)

The 0.5 mm value is a PoC engineering tolerance, not the final warehouse readability threshold.

## Gate G2 — print dimensional integrity

G2 passes when:

- PDF page dimensions match selected ISO paper size
- 100 mm calibration element is encoded as 100.000 mm physical length within PDF-coordinate tolerance
- artwork is emitted at 1:1 physical scale
- page tiling covers the complete surface artwork without unintended gaps
- configured overlap is reflected correctly in adjacent pages
- every page contains project ID, surface ID, page index, orientation and registration information

A physical printer-driver scaling check remains required before G3.

## Gate G3 — physical PoC

Use the same geometry as a passing virtual case and manually measure the mock installation surfaces.

Record observations from at minimum:

- reference position
- lateral -0.5 m
- lateral +0.5 m
- lateral -1.0 m
- lateral +1.0 m

G3 passes when:

- printed calibration line confirms acceptable print scale or documented correction is applied
- installers can place pages using provided page/registration/origin information without measuring individual glyph fragments
- the reference viewpoint yields an unambiguous `GA`
- observed deformation trends away from the reference position qualitatively match the simulator
- any discrepancy can be attributed to a measured category: print scale, geometry input, installation offset, viewing-position error, or projection implementation

G3 does not yet require a fixed numeric readable-zone threshold. That threshold will be calibrated from physical results before G4 acceptance is frozen.
