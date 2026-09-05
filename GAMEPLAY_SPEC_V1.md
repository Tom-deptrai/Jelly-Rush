# Jelly Rush — Gameplay Spec V1

> Living document: this file records the current agreed prototype direction. It is not a rigid final design. We will update it as we build, playtest, and learn.

## 1. Controls

- **Tap** = jump straight ahead.
- **Swipe left** = jump and move to the left lane.
- **Swipe right** = jump and move to the right lane.
- Prototype V1 uses **3 lanes**: left, center, right.
- The game is designed for **one-finger play**.
- Rapid tapping is allowed; there is no artificial minimum interval between taps.

## 2. Gameplay Flow

- The play space is a **3D depth corridor**.
- Platforms, obstacles, coins, targets, and scenery appear from **deep inside the screen**, initially small, then grow larger as they move toward the player/camera.
- The player reads upcoming challenges in the distance, chooses a lane, jumps, changes direction when needed, avoids obstacles, lands on targets, and maintains momentum/combo.
- The visual feeling should be that the pair is moving deeper/upward through the world while the environment continuously comes toward the camera.

## 3. Player Characters

### Main mascot
- Keep the approved **blue Jelly robot mascot** design.
- The Jelly remains the emotional and visual focus.
- Its face should remain clearly visible to the player as much as possible.

### Carrier companion
- The approved companion is a **small, energetic animal-like carrier** that carries the Jelly.
- The companion faces into the gameplay depth/direction of travel.
- It performs the physical jumping and lane-changing motion.
- Both characters are treated technically as **one player unit** for prototype gameplay.

## 4. Character Expressions and Feedback

The Jelly should react constantly so the player feels rewarded emotionally while playing:

- Normal jump → cheerful / excited.
- Rapid consecutive jumps → increasingly energetic, rounder Jelly body, stronger motion effects.
- Swipe / lane change → focused or playful reaction.
- Near miss → brief surprised / scared reaction, then recovery.
- Perfect landing → strong happy celebration.
- Hit / fail → dizzy, shocked, sad, or funny reaction.

The goal is that simply watching the mascot react should make repeated tapping feel satisfying.

## 5. World, Camera, and Presentation

- Mobile **portrait orientation**.
- Full 3D stylized visuals, but gameplay is constrained for readability and control.
- Perspective camera must show strong depth.
- The upper part of the screen may show sky, ceiling, factory structure, temple architecture, etc., depending on the world.
- Gameplay content should **not simply fall vertically from the top edge**.
- Instead, it should emerge from the vanishing/depth area of the scene, starting small and scaling naturally larger as it approaches.
- The first prototype world is intended to be a colorful premium casual environment such as **Toy Workshop**, with later worlds like Candy Factory, Jungle Temple, and Sky Station possible.

## 6. Prototype V1 Direction

Prototype V1 should focus on validating:

- 3-lane controls feel natural.
- Tap jumping feels responsive and satisfying.
- Swipe-left / swipe-right lane changes are easy to understand.
- Upcoming obstacles are readable from the depth of the scene.
- Landing decisions are fun and fair.
- Rapid tapping plus Jelly reactions feels rewarding.
- Death/failure feels understandable rather than random.
- The player wants to retry immediately.

This document is intentionally flexible. Values, mechanics, obstacle types, jump timing, camera settings, effects, and other details may change after playtesting.
