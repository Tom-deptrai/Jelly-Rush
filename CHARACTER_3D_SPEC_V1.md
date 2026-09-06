# CHARACTER_3D_SPEC_V1

> Status: V1 — living document. Scale / proportion numbers are **provisional**
> and will be locked once real models exist. The relationship, anchors, axes and
> the "no permanent merge" rule are **binding**.
>
> Companion doc: `ASSET_PIPELINE_V1.md` (import rules, quality gate, stages).
> Design context: `GAMEPLAY_SPEC_V1.md`, `CAMERA_AND_DEPTH_SPEC_V1.md`.
> Gameplay baseline: git tag `V01-Base-Line` (commit `ee40a63`).

---

## 1. The pair

Two characters, treated as **one player unit** for gameplay, kept as **two
separate assets** for art and animation.

| | Jelly | Carrier |
| --- | --- | --- |
| Role | the mascot / emotional focus | the small energetic animal that carries Jelly and does the jumping |
| Asset | own mesh, own root, own rig | own mesh, own root, own rig |
| Motion | rides on the Carrier's back; reacts (squash, expressions) | performs run / jump / landing / lane lean / celebration |
| Facing | toward the gameplay camera (face readable) | into gameplay depth (+Z) |

**Jelly rides Carrier.** Jelly is parented to a dedicated anchor on the
Carrier's back — never fused to it.

**Do NOT merge Jelly + Carrier into one permanent mesh or one skeleton.** The
victory sequence (future) separates them.

---

## 2. Anchors and hierarchy

```
CarrierRoot                 root at the FOOT POINT (local Y = 0), +Z = forward
  Visual / Rig
  JellySeat                 the ONE official anchor for mounting Jelly
    JellyRoot               Jelly parented here (or pose-driven by JellySeat)
      Visual / Rig
```

- **`JellySeat`** is a Transform on the Carrier (rig-driven if the back moves).
  All of Jelly's mount position/rotation offset lives in this transform.
  Gameplay code contains **no** Jelly-vs-Carrier positional offsets.
- `CarrierRoot` attaches under `PlayerUnit > Visual > LeanPivot` in the scene
  (see `ASSET_PIPELINE_V1.md` §8). `LeanPivot` tilts the whole pair about the
  foot during a lane change.

---

## 3. Gameplay contract the models must respect

- **FootPoint:** `PlayerUnit.position` is the bottom contact point. When
  grounded, `PlayerUnit.y = surfaceY + playerFootClearance` (currently `0.03`).
  In the canonical grounded/idle pose, **no vertex of either character sits
  below local Y = 0**.
- **Forward = +Z.** Carrier's forward axis is +Z. World scrolls toward `-Z`.
- **Jelly's face must stay clearly visible** from the gameplay camera
  (perspective, positioned behind + below the pair, looking along +Z — see
  `CAMERA_AND_DEPTH_SPEC_V1.md`). Jelly's head/face orientation points roughly
  `-Z` in world space. The face should remain visible as much as possible during
  jump and lane-change poses.
- **Carrier orients into gameplay depth**, showing it is the one doing the
  physical jump.
- Gameplay never scales itself to the mesh. Hitbox, camera framing and clearance
  are authored in `PrototypeConfig` / prefabs.

---

## 4. Provisional scale / proportion (placeholder, will be re-chocked)

Current placeholder primitives (`PrototypeBootstrap.BuildPlayer`), for reference
only — **do not treat as final**:

| Part | Placeholder now | Notes |
| --- | --- | --- |
| Carrier body | box ~`1.0 (X) × 0.5 (Y) × 1.5 (Z)`, bottom at local Y 0 | long axis along +Z (depth) |
| JellySeat height | ~`0.5` above the foot | on the Carrier's back |
| Jelly body | ~`0.9 × 0.85 × 0.85`, bottom resting on the seat (~Y 0.5) | elastic blob |
| Face | small quad on the `-Z` side of Jelly, ~`0.7 × 0.5` | keeps a "face" toward camera |
| Total pair height | ~`1.35` | fits current camera framing (offset `(0, 3.8, -7.8)`, pitch `16°`, FOV `58`) |

Lane spacing is `~2.15` UU; height tiers Low/Mid/High are `Y = 0 / 1.6 / 3.2`.
Final character size will be chosen so the pair reads well against these and the
camera does not need to change.

---

## 5. Animation / deformation the models must be built for

**Carrier:** idle · run · jump take-off · landing · lane lean (L/R) · celebration.

**Jelly:** idle · jump reaction · squash / stretch (elastic, "rounder when
excited") · facial expressions (cheerful, focused/playful, surprised-scared then
recover, big happy celebration, dizzy/shocked/sad on fail) · hit reaction ·
victory.

Jelly must deform through squash-on-land and stretch-on-takeoff **without the
mesh tearing or self-intersecting**.

Currently these are stubbed: `PlayerVisuals` scales the Jelly cube and tints its
material; `PlayerVisuals.OnLevelComplete()` is an empty hook. Real rigs replace
the stubs; the hook points stay.

---

## 6. Victory animation readiness (future round — not now)

The models must be authored so that a later victory sequence can:

- **separate the two characters** — Jelly dismounts / lifts off the Carrier;
- pose and animate each independently (independent roots + rigs);
- do this with **no re-export** of geometry.

This is the main reason Jelly and Carrier stay separate assets. Any modeling or
rigging decision that would make separation require a merge/re-export is a
**quality-gate failure**.

---

## 7. Out of scope for this round

- No 3D model creation.
- No replacing the placeholders.
- No gameplay changes.

This document only records the constraints so that when modeling starts, the
result drops into the frozen `V01-Base-Line` gameplay without edits to
`PlayerController` or the collision / layer / Auto Test systems.
