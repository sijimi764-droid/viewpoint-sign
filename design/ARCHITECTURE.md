# Architecture v0.1

## Objective

Keep the geometry/projection core independent from UI, rendering, printing, and future Android capture.

## Logical modules

- `ViewpointSign.Domain` — immutable domain types, units, constraints, revisions
- `ViewpointSign.Geometry` — vectors, planes, rays, intersections, transforms
- `ViewpointSign.Projection` — target sign plane, reverse projection, surface clipping
- `ViewpointSign.Readability` — multi-view rendering metrics and later calibrated readability
- `ViewpointSign.Optimization` — staged candidate search and ranking
- `ViewpointSign.Printing` — page tiling, registration marks, scale calibration, PDF output
- `ViewpointSign.ProjectFile` — project serialization and later `.wsa` handling
- `ViewpointSign.Desktop` — Windows UI only
- `ViewpointSign.Tests` — deterministic unit/integration tests

## Dependency direction

`Desktop → Optimization / Printing / ProjectFile → Projection / Readability → Geometry → Domain`

Rendering must not own geometry truth. All geometric values use millimetres in the domain layer and double precision internally.

## Coordinate convention

Right-handed world coordinates:

- `+X`: right
- `+Y`: depth / forward into warehouse
- `+Z`: up
- length unit: millimetre

A planar installation surface is an ordered convex quadrilateral `P0..P3`. PoC-A may initially constrain it to a rectangle in 3D while retaining the quadrilateral interface.

## Camera / viewer model for PoC-A

Use a pinhole camera model with explicit position, target, up-vector, vertical FOV and output aspect ratio. Orthographic projection is not accepted for G0/G1 because perspective depth is the core phenomenon.

## Target sign plane

The desired sign is defined on a virtual target plane. Glyph outlines are converted to 2D polygons on that plane. Rays from the reference viewer through target-plane points are intersected with installation surfaces.

## Determinism

PoC-A algorithms must be deterministic for identical inputs. Randomized search, if introduced later, must accept an explicit seed and persist it with the solution.

## Numerical policy

- Use `double`
- Never compare geometry using exact floating-point equality
- Centralize tolerances
- Initial engineering epsilon: `1e-8` in normalized/vector operations; physical acceptance tolerances are defined separately in tests
- Reject degenerate planes/quadrilaterals early

## Initial implementation target

Windows PoC-A: C#/.NET with WPF reserved for UI. G0 and G1 should be executable and testable without WPF.
