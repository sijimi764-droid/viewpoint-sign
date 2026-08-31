# G1 Virtual Reprojection Design v0.1

## Objective

Prove the complete vector geometry chain from target glyph geometry to per-surface installation artwork and back to the target plane for PoC-A TEST-01..03.

## Pipeline

1. Accept target glyphs as `GlyphGeometry` containing polygon/multipolygon outlines.
2. Project each finite installation surface to its target-plane visibility footprint.
3. Resolve overlap ownership using G0 per-ray depth rules.
4. Intersect each glyph with each owned surface region.
5. Build the exact target-footprint ↔ surface-local homography from the four corresponding corners.
6. Map owned target fragments to surface-local vector artwork.
7. Map the local artwork back to the target plane using the inverse homography.
8. Union reconstructed fragments and calculate Gap against the target glyph union.
9. Record maximum source-vertex forward/inverse round-trip error.

## Geometry truth

Raster output is not authoritative. `SourceTargetGeometry`, `LocalArtwork`, ownership regions, reconstructed geometry and Gap remain vector geometry.

## G1 output model

`SurfaceArtworkFragment` retains:

- glyph ID
- installation surface ID
- source target-plane geometry
- local surface artwork geometry
- reconstructed target-plane geometry

`VirtualProjectionResult` retains:

- all fragments
- ownership regions
- target geometry
- reconstructed geometry
- Gap geometry
- maximum round-trip error in mm

## Acceptance mapping

TEST-01 validates the ordinary single-plane case.

TEST-02 uses three parallel surfaces at different depths with overlapping footprints, requiring correct nearest-surface ownership while all three surfaces contribute artwork.

TEST-03 uses three vertical surfaces with different plan orientations, exercising non-constant depth relationships and exact projective mapping.

For all cases:

- Gap must be zero when geometry intentionally provides complete coverage.
- ownership regions must not overlap after resolution.
- reconstructed geometry must have no material missing/extra target region.
- maximum round-trip error must be <= 0.5 mm; deterministic test geometry is expected to be far tighter.
- emitted local artwork coordinates must be finite and geometrically valid.

## Non-goals

Printing, page tiling, physical calibration, smartphone capture, multi-view readability optimization and duplicate projection remain outside G1.
