# BlackHoleEdu

A real-time, general-relativity-based black hole educational simulation built with **Unity 6 + URP**.

*[한국어 README](README.ko.md)*

Everything you see in the image — the shadow, the photon ring, the lensed arcs — comes from
**numerically integrating the geodesic equation per pixel** in a fragment shader. Brightness and
color come from relativistic shifts: Doppler beaming, gravitational redshift, and blackbody radiation.

## Features

- **Schwarzschild raymarching** — null geodesics integrated with a leapfrog scheme on a single
  billboard quad; thin accretion disk + volumetric haze, relativistic beaming *I ∝ (δ·g)³*,
  Shakura–Sunyaev thin-disk temperature *T ∝ r^(−3/4)*, Planckian-locus blackbody colors
- **Kerr (spinning) black hole** — Kerr–Schild coordinate Hamiltonian integration: D-shaped shadow,
  frame dragging, disk inner edge tracking the prograde ISCO(a) (key 3, presets 0 → 0.998)
- **Binary black hole merger** — the GW150914 story (F4): two lensing centers inspiraling on a
  Peters-equation orbit inside a circumbinary-disk cavity, each carrying a **tidally truncated
  minidisk** of its own, gravitational-wave chirp audio synced to the actual orbital frequency,
  merger flash, quadrupole-deformed wavefronts, ringdown, and a Kerr remnant with 95% of the total
  mass and spin a ≈ 0.69
- **Experiences** — 11-step narrated guided tour (F1), star-collapse birth intro (F2), fully narrated
  first-person fall-in with a physically honest inside-the-horizon ending (F3)
- **Educational toggles** — photon trajectory launcher (Space), Einstein ring (E), spaghettification
  (T), relativistic jets (J), gravitational-lens magnifier (G), light curve (V), EHT photo comparison
  (4); every toggle shows a card explaining what you are looking at
- **Theory panel** — context-sensitive governing-equation cards (X; auto-shown at the advanced
  difficulty level, C)
- **Four languages** — Korean · English · Japanese · Chinese: every caption, panel, label and
  narration clip (neural TTS per language); switch any time from the top-right selector or K
- **Fully procedural assets** — starfield/Milky-Way skybox, star surface shader (convection
  granulation + corona), ambient soundscape and GW chirp are all generated in code; zero external
  art or audio assets
- **MR scene** — Quest passthrough (`BlackHoleMR`): room-scale hole you can grab and scale,
  throwable spectral-type star-balls, binary-merger haptics, palm-summoned mini black hole

## Controls (BlackHoleShowcase scene)

| Category | Keys |
|---|---|
| Experiences | **F1** guided tour (N/B to navigate) · **F2** birth of a black hole · **F3** fall in · **F4** merger · **Esc** skip/stop |
| Black hole | **1** disk colors · **2** mass presets · **3** spin · **4** EHT photo comparison |
| Phenomena | **Space** photons fire/clear · **E** Einstein ring (A/D) · **T** spaghettification · **J** jets · **G** lens magnifier · **V** light curve |
| Controls | RMB drag orbit · wheel/W/S zoom · **R** reset · **L** labels · **I** info panel · **X** theory · **U** immersive · **M** sound · **K** language · **P** perf HUD · **F12** snapshot · **H** help · **C** explanation level |

## The physics actually implemented

The geometry on screen is the numerical solution of the real equations — not an artist's
impression with lensing "painted on":

| Quantity | Equation | Notes |
|---|---|---|
| Light bending | null-geodesic ODE d²**x**/dλ² = −(3/2)h²**x**/r⁵ (GM=c=1) | integrated per pixel, leapfrog KDK; the shadow (b_crit = 3√3 GM/c² ≈ 2.6 Rs), photon ring, Einstein ring and the disk's over/under arcs all *emerge* from this — none are drawn by hand |
| Kerr spacetime | Kerr–Schild Hamiltonian, Φ = H(1+l·q)² | numerical gradient; horizon r₊ = M+√(M²−a²); prograde ISCO from the Bardeen–Press–Teukolsky formula |
| Beaming & redshift | δ = 1/(1−β·cosθ), g = √(1−Rs/r), I ∝ (δg)³, T_obs = T·δg | the bright/dim disk asymmetry is computed, not textured |
| Disk temperature | Shakura–Sunyaev T ∝ r^(−3/4), colors from the Planckian locus | anchored to ~12 MK for a 10 M☉ hole, T ∝ M^(−1/4) across mass presets |
| Time dilation | √(1−Rs/r) for both clocks | the observer clock uses the camera's true distance; the "far away 1 h = X min" numbers are exact for static observers |
| Real scales | Rs = 2.953 km × M/M☉, shadow ∅ = 5.2 Rs | panel numbers for 10 M☉ / Sgr A* / M87* are the real values |
| Binary inspiral | Peters decay a(t) = a_f + (a₀−a_f)(1−t/T)^¼, Kepler ω² = M/a³, f_GW = 2f_orb | remnant mass 0.95 M_tot and spin a = 0.69 are the measured GW150914 values |
| Photon launcher | the same geodesic ODE on the CPU | capture inside b_crit is genuine, not scripted |

## Simplifications and artistic license

Being honest about what is *not* rigorous:

- **Time and space are compressed.** The merger squeezes months of inspiral into ~40 s (Kepler
  scaling preserved); disk rotation is sped up (a real SMBH disk takes minutes–hours per orbit and
  would look frozen); mass presets change the numbers correctly but the visual scale ratios are
  stylized so everything stays on screen.
- **Binary lensing is a superposition** of two Schwarzschild deflections. No analytic two-black-hole
  metric exists; the last orbits really require numerical relativity. The gas picture follows the
  standard simulation morphology — circumbinary disk + cavity + per-hole minidisks (truncated at
  ~0.35 of the separation) — but uses the same stylized thin-disk model, with no accretion streams
  between the components. Ringdown audio is a damped sine, not the quasi-normal-mode spectrum. GW
  "rings" visualize invisible strain. (GW150914 itself was almost certainly gas-free — no
  electromagnetic counterpart was observed; the gas is there to make the dynamics visible.)
- **No radiative transfer.** Disk brightness/opacity profiles, turbulence noise and the bright
  "knots" are procedural art shaped by the physics (Keplerian shear), not MHD simulation output.
  Bloom and exposure are tuned for legibility.
- **The fall-in is a cinematic**, not a proper-time integration: pacing, camera tilts and the
  shrinking-sky circle after the horizon are staged (the *statements* in the captions — light cone
  tilting, last light overhead, the backward sky remaining visible — are correct physics).
- **Spaghettification** uses a capped (r₀/r)^1.4 stretch for readability; the real tidal gradient is
  Δa ∝ 1/r³. Supernova, jets and the intro are physically-motivated VFX, not simulations.
- **Kerr mode** shows the prograde equatorial thin disk only; jets are decorative and not tied to
  spin (no Blandford–Znajek), and the disk does not warp (no Bardeen–Petterson).
- MR star-ball orbits are Newtonian with a room-scale GM.

Each in-app theory card (X) states whether its topic is computed or stylized.

## Notes for classroom use

- **Trust the numbers, not the stopwatch.** Panel values (Rs, shadow size, temperatures, dilation
  factors) are correct; on-screen durations and angular sizes are compressed. A real Sgr A* shadow
  spans ~50 μas — no camera could hover at 26 Rs.
- **The dilation clocks assume *hovering* (static) observers.** Orbiting or falling observers need
  extra velocity terms — that is why the probe is described as "hovering beside the hole". Near the
  hole *both* clocks slow, so the panel switches to a far-away reference hour to keep the comparison
  intuitive.
- **Colors are physical hues, but real images differ:** EHT pictures are radio interferometry in
  false color; an optical view would be blindingly bright. Use the comparison mode (4) to discuss
  this explicitly.
- **Inside-horizon content is an educated illustration.** Nothing can report back from inside; the
  captions say as much and that claim *is* the physics.
- The narration scripts are the on-screen captions (all four languages) — safe to quote; regenerate
  audio with edge-tts if you edit them.

## Requirements

- **Unity 6000.5.3f1** (Unity 6, URP 17); the XR packages are only needed for the MR scene
- Narration audio ships in `Assets/BlackHoleEffect/Resources/Narration/` (regenerate with
  [edge-tts](https://github.com/rany2/edge-tts); transcripts live in each script's `Lines` arrays)

🤖 Generated with [Claude Code](https://claude.com/claude-code)
