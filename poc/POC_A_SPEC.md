# PoC-A Detailed Specification v0.2

## Purpose

Validate the end-to-end chain **manual geometry → reverse projection → virtual reconstruction → true-scale print → physical installation** before implementing smartphone capture.

PoC-A intentionally excludes Android, marker detection, AR, AI plane detection and warehouse-photo processing.

## Inputs

### Sign

- text: `GA`
- target width/height in mm
- target-plane pose
- glyph outlines as vector polygons
- foreground/background: black on white for PoC-A

### Viewer

- reference eye position `(x,y,z)`
- look target
- up vector
- vertical field of view
- render dimensions

### Installation surfaces

At least 1 and up to 3 manually defined rectangular planes for the initial PoC. Each surface has:

- ID
- four 3D corners
- physical width/height
- availability status
- local 2D coordinate system `(u,v)`

### Viewing zone

G0/G1 requires a reference point. The data model must already allow multiple sample viewpoints so G4 does not require a model rewrite.

## Core mathematical operation

For a target-plane point `T` and reference viewer position `V`, construct:

`R(t) = V + t(T - V), t > 0`

For an installation plane with point `P` and normal `n`, solve:

`t = dot(P - V, n) / dot(T - V, n)`

An intersection is valid only when:

- denominator is not approximately zero
- `t > 0`
- the 3D intersection lies inside the finite installation surface
- no nearer selected installation surface owns the same target-plane region

The valid 3D hit point is converted to the surface-local 2D `(u,v)` coordinates for artwork generation.

## Exact planar mapping strategy

PoC-A must not depend on raster projection as its geometry truth. For each planar installation surface:

1. Project its four boundary corners from the reference viewer onto the virtual target plane. This produces the surface's **target-plane visibility footprint**.
2. Intersect that footprint polygon with the target glyph polygons.
3. Resolve overlap between surface footprints by visibility depth so the nearest physical surface owns the overlapped target region for the simple G0/G1 projection.
4. Map each owned clipped polygon from target-plane coordinates to the installation surface's local `(u,v)` coordinates using the exact planar projective transform induced by the viewer and the two planes.
5. Preserve the resulting polygons as vector geometry.

For two planes viewed from a fixed pinhole viewpoint, the mapping is projective; an exact homography/projective transform can therefore be used for planar coordinates. Adaptive subdivision may be used only as a rendering fallback, not as the authoritative geometry algorithm.

Each projected polygon fragment must retain:

- source glyph ID
- source contour ID
- installation surface ID
- source target-plane polygon
- local surface polygon

## Surface ownership rule

Where projected surface footprints overlap on the target plane, the nearest valid physical surface along the relevant viewing ray owns that region for the initial simple projection. Ownership boundaries must be represented geometrically rather than decided only at raster sample points.

Later multi-view duplicate projection may intentionally add redundant artwork, but it is outside G0/G1.

## Gap definition

A target-sign region not covered by any usable visible surface footprint is a Gap. PoC-A records the Gap polygon set and area but does not yet optimize around it.

## Test geometries

### TEST-01 — single plane

A single installation plane parallel to the target sign plane. Expected result: equivalent to an ordinary perspective-correct sign with no artificial fragmentation.

### TEST-02 — three parallel depth planes

Three finite planes at different depths cover different target-plane regions. Expected result: owned polygon fragments reconstructed from the reference view form `GA`.

### TEST-03 — three differently oriented planes

Three finite planes emulate a wall/column/rack-side arrangement. Expected result: perspective-distorted fragments reconstruct to `GA` from the reference view.

## Required outputs for G0/G1

- machine-readable projected vector fragments
- per-surface 2D artwork preview
- reference-view reconstruction image/vector scene
- target-plane visible-footprint polygons
- Gap polygons and statistics
- diagnostic geometry dump containing selected deterministic rays/intersections and ownership decisions

## G2 print preparation

Printing is implemented only after G1 passes. Required print concepts are already fixed:

- physical units in mm
- A4 portrait/landscape selection
- 1:1 output
- 100 mm calibration line
- page/surface IDs
- orientation marks
- registration marks
- configurable overlap, initial default 10 mm

## G3 physical PoC

Use manually measured mock surfaces. Print and install the generated pages, then inspect from the reference location and lateral offsets. G3 does not establish the final human-readability threshold; it validates that the digital prediction survives printing and installation.

## Non-goals

- optimization of size/position
- duplicate projection
- camera geometry acquisition
- OCR-based scoring
- curved surfaces
- production UI polish
