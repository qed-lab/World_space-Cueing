# World-Space Covert Cueing — Standalone Demo

A minimal, self-contained Unity project demonstrating the **world-space cueing technique** for guiding visual attention in 3D environments. This project accompanies our research paper and allows readers to reproduce and experiment with the technique locally.

## Overview

Covert cueing subtly modulates (flickering effect) objects in a viewer's **peripheral vision** to draw their attention, while suppressing the effect when the viewer looks directly at the object. The cue operates below conscious awareness — the viewer's gaze is guided without them realizing a visual manipulation is occurring.

This technique was originally developed by Monthir Ali, Po-Jui Huang, and Rogelio E. Cardona-Rivera for use with VR eye-tracking headsets (HTC Vive with Wave SDK). This standalone demo replaces hardware eye tracking with **simulated gaze** (camera forward direction via mouse look), making it accessible to anyone with Unity installed.

## How the Technique Works

The covert cueing system has three interconnected components:

### 1. The Shader (`Covert_S.shader` — QEDLab/Covert)

A custom CG/HLSL shader that adds a **radial brightness boost** to an object's surface:

- Computes the world-space distance from each rendered pixel to a center point (`_CenterPosition`)
- Normalizes this distance into a 0–1 falloff based on `_Radius` (1.0 at center, 0.0 at edge)
- Adds brightness: `color += 0.095 * falloff * _Modulation`
- The `0.095` constant produces an approximately **9.5% maximum brightness increase** — subtle enough to avoid conscious detection
- A `_Color` tint property allows setting the object's base color (multiplied with the texture)

### 2. The Cueing Controller (`CovertObject.cs`)

Attached to each object that should act as a covert cue:

- **Sends shader parameters** each frame: center position, radius, and a time-varying modulation value
- **Modulation**: Driven by an `AnimationCurve` evaluated at `Time.time % 0.2f`, creating a repeating 0.2-second pulse cycle
- **Gaze detection**: Computes the angle between the player's gaze direction and the direction toward the object
- **Running average**: Smooths the angle measurement over a sliding window of 5 samples to prevent flickering
- **Threshold**: If the smoothed angle ≤ **15 degrees** (foveal vision), the radius is set to 0 (cue suppressed). If > 15 degrees (peripheral vision), the radius is restored (cue active)

### 3. The Gaze Provider (`SimulatedEyeSight.cs`)

A singleton that provides the current gaze direction:

- In this demo: uses `Camera.main.transform.forward` (mouse look direction = simulated gaze)
- In the original VR implementation: used `EyeManager.GetCombinedEyeDirectionNormalized()` from HTC Vive Wave SDK
- Interface: `SimulatedEyeSight.Instance.Trans.forward` — the gaze direction vector

### 4. Gaze Radius Overlay (`GazeRadiusOverlay.cs`)

An on-screen visualization that makes the invisible gaze threshold visible:

- Draws a **circular outline** on screen representing the 15° foveal threshold
- A **crosshair** at the center marks the current simulated gaze point
- Objects **inside** the circle are in foveal vision — their cue is suppressed
- Objects **outside** the circle are in peripheral vision — their cue is active
- Text labels indicate the zones and controls for quick understanding
- Attached to the Main Camera; can be toggled on/off via the `Show Labels` checkbox in the Inspector

## Key Parameters

| Parameter | Value | Description |
|-----------|-------|-------------|
| Brightness boost | `0.095` (9.5%) | Maximum RGB increase per channel at the center of the effect |
| Gaze threshold | `15 degrees` | Angle below which the cue is suppressed (foveal vision) |
| Running average window | `5 samples` | Number of frames used to smooth gaze angle measurements |
| Modulation cycle | `0.2 seconds` | Period of the temporal pulse/flicker pattern |
| Radius | `2.0–3.0 units` | Spatial extent of the brightness falloff from the object center |

## How to Run

1. Open this project in **Unity 2022.3 LTS** or later
2. Open the scene: `Assets/Scenes/SampleScene.unity`
3. Press **Play**
4. Use **WASD** to move, **mouse** to look around
5. Press **Escape** to unlock cursor, **click** to re-lock

## How to Observe the Effect

1. Walk near any of the cue target objects (colored cylinders, cubes, and sphere scattered around the gray environment)
2. Notice the **white circle** on screen — this is the 15° foveal threshold boundary
3. **Look away** from an object so it falls **outside** the circle — it will subtly pulse brighter in your peripheral vision
4. **Look directly** at the object (bring it **inside** the circle) — the pulsing stops
5. The effect is intentionally subtle; look for slight brightness changes at the edges of your vision

### Re-integrating VR Eye Tracking

To use real eye-tracking hardware instead of simulated gaze:

1. Replace `SimulatedEyeSight.cs` with a class that reads from your eye-tracking SDK
2. Ensure your replacement exposes `Instance.Trans.forward` as the gaze direction vector
3. The rest of the system (`CovertObject.cs` and `Covert_S.shader`) requires no changes
4. You may want to disable or remove `GazeRadiusOverlay` since the gaze circle is a demo aid

### Using Custom 3D Models

1. Apply a material using the `QEDLab/Covert` shader to your model
2. Set the `_Color` property to tint the material (darker colors make the brightness boost more visible)
3. Add the `CovertObject` component to the GameObject
4. Set the `Radius` to cover the object's visual extent
5. Configure the `Carve` AnimationCurve for the desired pulse pattern

### Adjusting the Technique

- **Brightness intensity**: Modify the `0.095f` constant in `Covert_S.shader` (line with `col.rgb +=`)
- **Gaze threshold**: Change the `15f` value in `CovertObject.cs` (`runningEyeSightAngleAvg <= 15f`) and update `gazeThresholdDegrees` in `GazeRadiusOverlay` to match
- **Smoothing**: Adjust the `capacity` field in `CovertObject.cs` (higher = more smoothing, slower response)
- **Pulse speed**: Change the `0.2f` modulo value in `CovertObject.cs` (`Time.time % 0.2f`)

## Requirements

- Unity 2022.3 LTS or later
- Built-in Render Pipeline (not URP or HDRP)
- No external packages required

## Citation

Ali, Monthir, Po-Jui Huang, and Rogelio E. Cardona-Rivera. "World-Space Cueing: A Geometrically-Compact Modulation Technique for Subtle Gaze Direction in Head-Mounted Virtual Reality Displays." In 2025 IEEE International Conference on Artificial Intelligence and eXtended and Virtual Reality (AIxVR), pp. 10-18. IEEE, 2025.

## License

MIT License
