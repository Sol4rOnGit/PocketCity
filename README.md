<img src="https://github.com/Sol4rOnGit/PocketCity/blob/main/img/GoodBanner.png" alt="Capital Chaos Banner" width="100%">

<p align="center">
  <img src="https://img.shields.io/badge/Status-Beta-success.svg">
  <img src="https://img.shields.io/badge/Platform-Stardance-purple.svg">
  <img src="https://img.shields.io/badge/Engine-Unity-blue.svg">
</p>

<h1 align="center">Capital Chaos</h1>

<h3>This is a game where you try to manage an ever growing city... while its being bombed, flooded, infected, shot at, nuked, and so much more.</h3>
<p>See Technicality for (most of) the game features</p>
<p>Scroll for controls</p>

<p align="center">
  <a href="http://dsc.gg/capitalchaos">
    <img src="https://img.shields.io/badge/Discord-Join%20the%20Server-5865F2?style=for-the-badge&logo=discord&logoColor=white" alt="Join the Discord">
  </a>
</p>

---
## Play & Watch!

- **Game Link:** [Not currently Live. Will be on itch.io though!](https://sol4ronitchio.itch.io/)
- **Watch My Videos:** [YouTube](https://www.youtube.com/@actual_techscope)
- **Scroll down for Stardance Reviewer Notes, Controls & Known Issues**

<img src="https://github.com/Sol4rOnGit/PocketCity/blob/main/img/Screenshot%202026-08-08%20231745.png" alt="2nd Capital Chaos Banner" width="100%">

## Notes For Stardance Reviewers (really hope it's a 36/36)

### Originality
- Have you ever seen a city builder on Stardance?
- Have you seen an undertale style bossfight in a city builder?
- Do you see a city builder with this level of chaos? I think not!
- Also the boss fight sound track is beautiful isn't it.
- Military, UFO, Asteroid, UI, Service buildings, Utility buildings, all 

### Technicality
A <strong>LOT</strong> of technicality, even for this first ship. 
- Grid tile based system as the game base, with procedural roads that auto figure out connections.
- Automatic city generation that adhere to your city zoning. Multiple game managers for a financial system that tracks employment, vacancies and auto fills with population.
- Centralised game manager that tracks days, and all the other actions and ideally where everything goes through.
- Chunk Manager for tree rendering initially and then expanded out to create chunk utilities for water & energy requirements. It also "intelligently" distributes outward to other chunks that have a defecit given that you have enough surplus in other chunks.
- Services that respond to events, such as hospitals for infections, firetrucks for fires and police for any crime scenes. All of which are neatly implemented into the above.
- A council FX menu that has broad game effects which utilise a lot of actions & delegates as do most other things to keep the game tightly optimised.
- Asteroid Attacks & UFO invasions that creates an explosion and abducts the population while creating this really clean UFO pulse.
- Military equipment, including attack helicopters with rotating blades, the ability to shoot missiles at a located target, which curves toward the target down. A turret that tries to counter these missiles by auto aiming at them and then using predictive aiming. Raycast for hit, so yes the turret does miss. And the turret rotates in real time. A B2Bomber that drops a massive nuke that creates a detonation.
- An entire game inside of this game - for the final boss fight. This uses lots of attacks and coroutines and a bunch of cool math to create all the nice patterns. Not a single hitbox is used here instead all mathematically figured out.
- GPU Optimisations to 6-7x the performance using GPU Rendering instead of GameObjects, and chunk-based tree rendering.

### Usability
- You can play it right now!
- There is game difficulty settings depending on how far you seem to be able to get.
- There is a start menu, and the game has the settings menu everywhere so you can turn graphics up and down to fit your device.
- Game optimisation is added here as well for a wider audience. Try it out.

### Storytelling
- I've got one of the highest view, like and comment counts in all of (Stardance)[https://stardancestats.xyz/people?metric=comments_received&size=25], so I do strongly believe I have some of the best storytelling on this platform, mixed with my humour. I've even got a mini community that I'm super duper thankful for.
- And on top of all that is all the youtube videos that you can (and should) bingewatch [here!!!](https://www.youtube.com/@actual_techscope)

## Controls
_Format: Control Keyboard / Xbox Controller_
<h4>In-Game Controls</h4>

- **Movement:** WSAD or Arrow Keys / Left Joystick
- **Zoom:** Scroll Wheel / Right Joystick
- **Sprint:** Shift / Left Joystick Down
- **Place:** Left Click / Right Trigger
- **Destroy:** Right Click / Left Trigger
- **Road Build:** 1 / X
- **Grid Zoning:** 2 / Y
- **Special Building Placement:** 3 / B
- **Retrofit Building:** F1 / A
- **Cycle Tool Category:** R / Right Shoulder
- **Cycle Building Type:** B / Left Shoulder


<h4>In-Game UI Controls</h4>

- **Toggle City Stats:** F / D-Pad Left
- **Toggle Council FX Menu:** E / D-Pad Right
- **Hide/Unhide Zoning:** Z / Right Stick Down
- **Hide/Unhide UI:** F5 / No keybind
- **Accept:** Y or Enter / A
- **Deny:** N / No Keybind (timeout) or B in menus

<h4>Other UI Controls</h4>

- **Pause:** Esc / Start
- **Debug Menu:** F3 / No keybind
- **Generic Menu Controls:** Mouse / left stick to move, left click / right trigger to select. (Makes you think why it's inverted?!)

<p>Damn that's a pretty long list of controls</p>

<details>
  <summary><h3>Known issues</h3></summary>
  <ul>
        <li>Okay Controller Support (best played on keyboard/mouse)</li>
        <li>I know there isn't a tutorial or anything so yeah</li>
        <li>Fullscreen support isn't really there for resolutions that isn't native</li>
        <li>Audio isn't really up to tip top standard, but I will work on this in future ships :)</li>
        <li>Game freezes? Only happened once so I'm not sure.</li>
        <li>Boss fight slight desync. Since it's randomised this is a given sadly but not anything major to the game design.</li>
        <li>No sound on asteroid strike. I have some clue why it's happening, and I'm actively working on it.</li>
  </ul>
  <p>Do bear in mind I have exams and stuff soon so I can't dedicate TOO much time unfortunately. And it's hard for one person to test everything in a game of this size.</p>
</details>
