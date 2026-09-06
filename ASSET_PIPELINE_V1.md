# ASSET_PIPELINE_V1

> Status: V1 — living document. The **foundational technical rules** below are
> binding for every 3D asset that enters the project. Numbers marked *provisional*
> may change once real models exist; the conventions may not.
>
> Baseline gameplay this pipeline plugs into: git tag **`V01-Base-Line`**
> (commit `ee40a63`). If an art/3D integration breaks gameplay, return there.

---

## 0. Purpose

The prototype currently runs on **placeholder primitives**. This document defines
how real 3D art replaces them **without touching gameplay code**. The gameplay
systems are frozen at `V01-Base-Line`; art attaches to a clearly defined visual
hierarchy and must obey the gameplay root, never the other way around.

---

## 1. Coordinate / scale

- **Unity convention: 1 Unity Unit = 1 metre.** All models are authored and
  exported at real-world metric scale (import scale factor `1`, no unit
  conversion at import).
- The **`PlayerUnit` root is the FOOT POINT** — the bottom contact point of the
  pair. Gameplay code (`PlayerController`) drives `PlayerUnit.position`; when
  grounded, `PlayerUnit.position.y = platformSurfaceY + playerFootClearance`
  (`PrototypeConfig.playerFootClearance`, currently `0.03`).
- **Model art follows the gameplay root, not the reverse.** Do NOT rescale,
  offset, or re-pivot gameplay objects to match a model. If a model's scale or
  pivot is wrong, fix it in the 3D source.
- **No geometry below the FootPoint in the canonical (grounded, idle) pose.**
  Every vertex of Carrier + Jelly must have local `Y ≥ 0` relative to
  `PlayerUnit`. The foot clearance is a z-fighting epsilon, not a hiding gap.
- Gameplay must never depend on the mesh's bounding-box size. Landing uses a
  **collider raycast on the `Landable` layer only** — never mesh bounds.

---

## 2. Direction

- **+Z is gameplay-forward** (into the depth of the screen). The world scrolls
  toward `-Z`; the player unit sits at `playerZ` (currently `0`).
- The **Carrier faces +Z** — it performs the run / jump / lane motion into the
  depth of the world. Its "forward" bone/root axis is +Z.
- **Jelly may have its own orientation** so its face stays readable to the
  gameplay camera (which looks along +Z from behind/below — see
  `CAMERA_AND_DEPTH_SPEC_V1.md`). Face-toward-camera means facing roughly `-Z`
  in world space; Jelly's rig handles this locally.
- **Do not change this convention when importing a new model.** If a model comes
  in facing `-Z` or `+X`, correct it in the 3D source before export, not with a
  wrapper rotation in gameplay code.

---

## 3. Character separation

**Jelly and Carrier are two separate character assets.**

- Two source files, two prefabs, two rigs.
- **Never merge Jelly + Carrier into a single mesh / single skeleton.**
- Reason: the victory sequence separates the two characters (Jelly dismounts /
  the pair celebrates apart). A merged mesh makes that impossible without a
  re-export.
- They are treated as **one player unit for gameplay** (collision, input,
  scoring) but remain **two independent visual/animation assets**.

---

## 4. Carrier hierarchy

Target structure (or an equivalent that exposes the same anchor):

```
CarrierRoot            (attach point into the Visual hierarchy; +Z = forward)
  Visual / Rig         (skinned mesh + skeleton)
  JellySeat            (Transform anchor on the Carrier's back where Jelly mounts)
```

- **`JellySeat` is the single official anchor** for mounting Jelly. Jelly's root
  is parented to `JellySeat` (or its world pose is driven by `JellySeat`).
- **No magic positional offsets scattered in gameplay code.** The mount offset
  lives entirely in the `JellySeat` transform inside the Carrier prefab.
- `JellySeat` should be rig-driven if the Carrier's back moves during
  run/jump/landing, so Jelly rides along naturally.

---

## 5. Jelly hierarchy

```
JellyRoot              (parented to CarrierRoot/JellySeat)
  Visual / Rig
```

- Jelly has its **own root and rig**, independent of the Carrier skeleton.
- Must be prepared for: **idle, jump, squash / stretch, expressions, hit,
  victory** — see `CHARACTER_3D_SPEC_V1.md` for the per-character detail.
- Squash/stretch and "rounder when excited" are currently faked by scaling the
  placeholder cube (`PlayerVisuals`). Real Jelly must support this via
  rig/blendshape/deformation **without breaking the mesh**.

---

## 6. Rig requirements

**Carrier rig must support:**
- idle
- run
- jump (take-off)
- landing (impact / recover)
- lane lean (tilt left/right during a lane change)
- celebration / victory

**Jelly rig must support:**
- idle
- jump reaction
- squash / stretch (elastic body)
- facial expressions (cheerful, focused, surprised/scared, happy celebration,
  dizzy/hit) — see `GAMEPLAY_SPEC_V1.md` §4
- hit / fail reaction
- victory

Jelly deformation must be achievable **without tearing or self-intersecting the
mesh** at the extremes used by gameplay (squash on landing, stretch on take-off,
rounder body during rapid-tap chains).

---

## 7. 3D quality gate

A model is **NOT imported into Unity gameplay** until it passes review on:

| Check | What "pass" means |
| --- | --- |
| silhouette | reads clearly at gameplay camera distance, against a busy background |
| proportions | matches the approved reference; Jelly's face stays dominant |
| mesh integrity | watertight where needed, no flipped normals, no stray verts |
| separation of movable parts | Jelly and Carrier are separable; moving sub-parts are their own objects |
| topology suitability | clean edge flow for the required deformation; sane poly budget for mobile |
| scale | authored in metres; sits correctly on a platform at 1 UU = 1 m |
| orientation | Carrier faces +Z; Jelly faces the camera (`-Z`-ish) |
| pivot / root | Carrier root at the FOOT POINT (localY 0); `JellySeat` present; Jelly root at its mount point |
| material | mobile-friendly shader/material setup; consistent naming |
| canonical pose | a defined neutral pose (grounded idle) that matches the gameplay resting state |
| rig deformation | test-deform through the extremes (§6) with no mesh breakage |
| naming | follows a consistent convention (see §9) |

**If a model fails a check: fix it in the 3D source.**
Do **not** patch a bad source model with a hack in `PlayerController`,
`PlayerVisuals`, or a wrapper transform in the scene.

---

## 8. Unity integration rule

When a placeholder is later replaced:

**KEEP UNCHANGED:**
- the `PlayerUnit` foot-point convention (§1)
- `PlayerController` (jump / lane / 4-beat / gravity / landing logic)
- `PlayerCollisions` and the trigger hitbox contract
- the `Player` / `Landable` / `Hazard` / `Collectible` layers (`GameLayers`)
- `AutoPlayBot` behaviour and `PrototypeConfig.enableDebugAutoTest` gating
- `LevelData` / `ChallengeSegment` / `LevelRoute`

**Art attaches under the `Visual` hierarchy of `PlayerUnit`:**

```
PlayerUnit                         (gameplay root = FOOT POINT; PlayerController,
  |                                  PlayerCollisions, kinematic Rigidbody, trigger)
  Visual                           (PlayerVisuals)  local (0,0,0)
    LeanPivot                      (tilt about the foot on lane change)
      CarrierRoot                  <- real Carrier prefab replaces Carrier_PLACEHOLDER
        Visual / Rig
        JellySeat
          JellyRoot                <- real Jelly prefab replaces Jelly_PLACEHOLDER
```

- `PlayerVisuals.Bind(leanPivot, jellyTransform, jellyRenderer)` is the seam:
  the bootstrap passes the lean pivot, the Jelly transform (for squash/stretch),
  and its renderer (for reaction colour). Real art keeps these three handles
  meaningful.
- **Gameplay systems must not read arbitrary mesh dimensions.** Any value the
  gameplay needs (foot clearance, hitbox size, camera framing) lives in
  `PrototypeConfig` / the prefab, tuned deliberately — not derived from whatever
  model happens to be loaded.
- Environment art (platforms, coins, hazards, worlds) swaps via
  `WorldThemeData` prefab slots; `SpawnableFactory` instantiates the prefab or
  falls back to a tinted primitive. Same rule: gameplay asks for a
  `SpawnableKind`, never for a specific mesh.

---

## 9. Recommended asset stages

```
Reference (approved art direction)
  -> 3D Specification  (this repo: CHARACTER_3D_SPEC_V1.md, later env specs)
  -> AI 3D / modeling  (generate or model the mesh)
  -> cleanup           (topology, normals, UVs, poly budget)
  -> pivot / orientation validation  (root at FootPoint, +Z forward, JellySeat)
  -> rig               (skeleton + skinning for §6)
  -> animation validation  (idle/run/jump/landing/lean/celebration deform test)
  -> Unity import      (metric scale, correct axis, materials)
  -> prefab integration  (CarrierRoot / JellyRoot prefabs; wire JellySeat)
  -> gameplay verification
       - PlayerController unchanged, compiles clean
       - no geometry below FootPoint in idle
       - AutoPlayBot still clears level-01 Start -> Finish
       - Console has no red errors in Play Mode
```

A stage does not start until the previous stage passes.

### Naming convention (V1 suggestion)

- Meshes / prefabs: `Char_Jelly`, `Char_Carrier`, `Char_Carrier_JellySeat`
- Anims: `Carrier_Idle`, `Carrier_Run`, `Carrier_Jump`, `Carrier_Land`,
  `Carrier_LeanL`, `Carrier_LeanR`, `Carrier_Celebrate`; `Jelly_Idle`,
  `Jelly_Squash`, `Jelly_Stretch`, `Jelly_Express_*`, `Jelly_Hit`,
  `Jelly_Victory`
- World theme prefabs: `<Theme>_NormalPlatform`, `<Theme>_BouncePad`,
  `<Theme>_RotatingBar`, `<Theme>_ClosingGate`, `<Theme>_Obstacle`,
  `<Theme>_Coin` (e.g. `ToyWorkshop_NormalPlatform`)
