# Third-Party Dependencies

## NetTopologySuite 2.6.0

- Purpose: robust polygon / multipolygon Boolean operations for PoC-A geometry processing
- Package: `NetTopologySuite`
- Version: `2.6.0`
- License: BSD-3-Clause
- Package restore: confirmed successful in GitHub Actions on 2026-09-01
- Source of license classification: NuGet package metadata

### Usage decision

Approved for the current PoC/MVP implementation. The license is permissive and compatible with the intended project use, subject to retaining the required copyright, license and disclaimer text when redistributing source or binaries.

### Engineering boundary

NetTopologySuite is used as the polygon-topology engine. Project-specific coordinate systems, tolerances, ownership rules, deterministic normalization and projection semantics remain inside `ViewpointSign.Geometry`; external library types must not become the public domain model.
