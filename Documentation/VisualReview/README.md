# Visual comparison — 2026-11-20

Fixed review cameras, before and after the visual pass:

| View | Before | After |
|---|---|---|
| Kitchen | [Before](kitchen-before.png) | [After](kitchen-after.png) |
| Tableware | [Before](objects-before.png) | [After](objects-after.png) |
| Hallway | [Before](hallway-before.png) | [After](hallway-after.png) |
| Living room | [Before](living-room-before.png) | [After](living-room-after.png) |

The images were rendered in Unity with graphics enabled. Changes include surface textures and normal maps, physical-scale UVs, softened prop edges, refined tableware, warm material/light balance, architectural trim, bathroom tile and removal of an overlapping legacy ground renderer. Built furniture batches are documented below; only remaining [ ] rows in tools.txt are pending.

These are desktop review captures, not a measured mobile performance result. Forward mobile now uses native rendering scale and 2x MSAA; desktop selects SMAA. Recheck frame rate, heat and readability on target phones.


New bathroom batch F10/F11: [Room view](bathroom-after.png) · [Washbasin and vanity](bathroom-vanity-after.png). Ten fixed fixtures are now present; other batches are tracked in tools.txt.


New F01–F04 bedroom batch: [Bedroom 1](bedroom-1-after.png) · [Bedroom 1 storage](bedroom-1-storage-after.png) · [Bedroom 2](bedroom-2-after.png) · [Study area](bedroom-2-study-after.png). Twenty room-specific instances are now built; remaining accessories are tracked independently.

F05–F08: [Living room](living-room-after.png) · [TV and seating](living-room-tv-after.png) · [Hall storage](hallway-storage-after.png) · [Entrance](hallway-entry-after.png). Twenty new fixed identification instances, with two working shoe-cabinet doors. The living-room overview camera was repositioned for the furnished layout, so its before/after framing differs.

Next accessories F09/F12/F13/partial F14: [Laundry corner](bathroom-laundry-after.png) · [Cleaning tools](bathroom-cleaning-after.png) · [Drying rack](bedroom-2-drying-rack-after.png) · [Key rack](hallway-keys-after.png) · [Umbrella stand](hallway-umbrella-after.png). Twenty new fixed targets, an additional bathroom mirror and two washer opening actions; 70 planned instances built and ten pending.

Final F14–F16: [Counter appliances](kitchen-final-appliances-after.png) · [Counter tools/scale](kitchen-final-tools-after.png) · [Radiator and thermostat](living-room-radiator-after.png) · [Switch and socket](hallway-switch-socket-after.png). Last ten targets complete the eighty-instance inventory. The hallway switch controls all three living-room lights. Earlier batch counts above describe their addition date; the current pending count is zero.

Final-ten validation ran in an isolated project copy while the main Unity editor remained open. FinalFurnishingValidation passed target accessibility, German name/prompt audio, paper-roll/dish-rack separation, radiator orientation, three-light switching and save migration. The full interaction, lesson, presentation, bedroom, bathroom and living-room validators also passed. This is editor validation; the user handles real-phone testing.


Small-object readability — 2026-11-20. These closeups use the same temporary table position, camera and existing kitchen lighting for each before/after pair. Other portable items are hidden only during capture; authored object positions and the saved scene remain intact. They demonstrate geometry/material readability on desktop, not normal gameplay distance or real-phone performance.

| Object | Before | After |
|---|---|---|
| Spoon | [Before](clarity-Loeffel_Spoon-before.png) | [After](clarity-Loeffel_Spoon-after.png) |
| Fork | [Before](clarity-Gabel_Fork-before.png) | [After](clarity-Gabel_Fork-after.png) |
| Knife | [Before](clarity-Messer_Knife-before.png) | [After](clarity-Messer_Knife-after.png) |
| Apple | [Before](clarity-Contents_Apple-before.png) | [After](clarity-Contents_Apple-after.png) |
| Tomato | [Before](clarity-Contents_Tomato-before.png) | [After](clarity-Contents_Tomato-after.png) |

ObjectClarityBuilder validates geometry, upward spoon-interior normals, existing pickup colliders and the unchanged 53 portable/80 fixed target populations. Visual replacements add no physics colliders. Existing collider shapes remain approximate; on-device recognition, touch and performance still require the user's test.


Player avatar — 2026-11-20: [Look down](player-body-first-person.png) · [Look forward](player-body-forward.png) · [Character overview](player-avatar-overview.png).

Captured in Play Mode in an isolated project. The overview temporarily enables the head renderers for inspection; normal gameplay keeps the head shadow-only and uses the existing first-person camera. The downward view lowers the arms to reveal the feet. Player-body checks and the full interaction/posture/progression suite passed. These are desktop previews; mobile performance and appearance remain unverified. The avatar is a simple procedural model, with approximate limb animation rather than a skinned animation rig or per-object finger grips.
