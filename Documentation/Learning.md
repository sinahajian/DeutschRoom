# DeutschRoom learning flow

Design reference: [GDD](GDD.md). Player-facing sequence from stage 0: [Game stages](GameStages.md). Update both alongside relevant game changes.

Play SampleScene. LearningProgress is added automatically by PlayerInteraction.

- Entry: a menu offers Start/Continue, Restart (clears the local learning save and reloads the room), and Touch/Mouse control selection. Escape pauses and reopens the menu. Start begins spoken lessons. Aim within 2.7 metres. E uses or picks up; F speaks and briefly displays the German name including its article. Nearby visible objects show mint dots with generous touch targets: tap to hear and see the name. On desktop, aim at a dot and click. The top right task card shows the stage number and repeats its spoken instruction. The top left pause and music controls use the device safe area and scaled 58-unit touch targets. Each new stage shows an animated card and progress bar. Pause freezes world animation and movement.
- Original tasks 1–17: find and pick up each object, starting with der Löffel. A wrong pickup does not advance the task. Put down the current item with G before picking up another. R rotates it. On touch, use appears only for a usable target; rotate appears only while holding an item; place appears only while aiming at a nearby upward-facing surface. Hidden controls have no active hit area.
- After the original 17 objects, 36 new pickup objects are introduced (53 pickup tasks, then 10 bathroom, 20 bedroom and 20 living-room/hallway and 20 house-accessory and 10 final-furnishing fixed identification tasks), followed by three article tasks. New items are appended so original task IDs/order stay stable. Existing saves on article tasks/completion migrate to the same article stage.
- Article tasks: place objects with der, die, then das on TableTop. Each article task first asks the player to clear all pre-existing items, naming the next item to remove. After cleanup, only matching, unheld objects resting within the table footprint count. Every placement is named aloud; wrong articles are explicitly named for removal and block completion. Moving an object off the table removes it from the current count. Targets use up to ten matching objects actually available: the expanded scene has 21 der, 18 die and 14 das pickup objects.
- Placement accepts nearby upward facing surfaces, including floor, table edges and other items. Footprint and overlap rejection have been removed for free placement; objects can overlap if placed at the same point. Vertical walls are excluded.
- Progress and placed object positions are saved locally with PlayerPrefs, key DeutschRoom.Learning.v1. A returning player continues their saved task. This is one local profile, not an account or cloud save. Held objects return to their last saved/initial placement on reload. Door and faucet states are not saved.

## Next content steps

1. Play through the preview and spoon task on desktop and touch.
2. Give every new pickup a unique GameObject name and a German displayName beginning with der, die or das.
3. For a larger curriculum, replace the generated task sequence with authored task assets and stable task IDs before shipping saved progress to players.
4. German task prompts, pronunciation and correct/incorrect feedback use bundled offline WAV clips in Assets/Resources/Voice. Documentation/Voice.json maps each hashed filename to its spoken text. Replace these clips with matching recordings to customize the voice. New object names require matching clips; GermanVoice.Key(text) defines filenames.

## Validation

Compile Assembly-CSharp.csproj and Assembly-CSharp-Editor.csproj. In Unity, Tools > DeutschRoom > Validate Interactions (Play Mode) exercises doors, pickup, free surface placement, restoration and touch controls. This validator disables learning persistence before moving objects. Full learning progression and relaunch persistence still need a Unity Play Mode check.

## Background music

Put one music file at `Assets/Resources/Music/background.mp3` (alternatively `background.ogg` or `background.wav`, with the same lowercase base name). Unity imports it automatically; no scene or Inspector setup is required. A README also marks the destination folder. There is intentionally no placeholder music track.

The note icon in the top bar and the menu's Musik control toggle only background music. The choice persists across restarts. Music loops at low volume and ducks during German narration. Music and voice use separate AudioSources.

## Vocabulary labels

Cabinet labels are `der Hängeschrank` and `der Unterschrank`, without construction numbers, English directions, or internal scene identifiers. Internal GameObject names stay stable so saved object IDs keep working. Corresponding bundled pronunciation clips use the corrected names.

## Cabinet contents and posture

The authored scene now contains 36 additional pickup objects in wall/base cupboards, the moving drawer, under the sink, the refrigerator and freezer. Contents live behind physical doors; door/wall occlusion also hides their learning markers. Fixed plumbing, sink parts, cabinet/fridge shelves, four chairs and the table have vocabulary labels. All additions are listed in German, Persian and English in `Documentation/tools.txt`, section 10.

In the free look area on the right, swipe down and hold still for 0.6 seconds to sit. Release, then repeat to lie down. Swipe up and hold to rise one posture. One held finger changes only one posture; the joystick, buttons and cancelled touches do not trigger this gesture. Desktop: C lowers posture, X raises it. Camera and collision capsule lower smoothly, movement slows, and rising is blocked under low obstacles.

Rebuild authored contents using Tools > DeutschRoom > Rebuild Cabinet Contents outside Play Mode. This replaces only the generated content groups and updates section 10 of tools.txt. It also opens and saves SampleScene, so save other scene work before invoking it.

Drawer-supported objects save local poses relative to their moving parent, with world-pose fallback. Objects removed from a drawer and placed on a static surface detach from it, including on reload; items placed back in the drawer follow its movement.

## Window garden and spoken lesson feedback

The west window overlooks an authored garden with trees, shrubs, grass, flowers, stones, a path, a bench and distant hills. Decorative exterior meshes have no physics and remain outside the room. Nature names are recorded in section 11 of Documentation/tools.txt.

GermanLessonSpeech produces accusative names (den Löffel) and matching pronouns for successful finds, placement acknowledgements and removal instructions. GermanVoice plays the entire requested sentence sequence, with a short pause between clips. Repeat repeats the current action, including the next object to remove from an uncleared table. The tablePrepared save flag preserves cleanup/sorting phase; older saves enter cleanup once. A held cleanup object must be placed outside the table before counting starts. Live counts decrease when an item is removed; replacing the same item earns no duplicate count. The next article starts by clearing the previous article's items.

## Neural voice pack

The game voice is regenerated with `de-DE-KatjaNeural`, at `-8%` rate for language learning. Clips remain mono PCM WAV at 24 kHz, bundled offline under their original hashed filenames. Existing `.meta` GUIDs and German lesson text stay stable. `Documentation/VoiceProfile.json` records the profile; `Documentation/VoicePreview.mp3` previews it.

To regenerate on macOS, create a Python virtual environment, install `Tools/Voice/requirements.txt`, then run `python Tools/Voice/generate.py`. Generation uses the online Microsoft Edge speech service via [edge-tts](https://github.com/rany2/edge-tts); playback in the game requires no internet or Python dependency. The script stages and validates the full pack before replacing anything, preserves `.meta` files, and backs up the preceding WAVs in the system temporary folder. Voice availability is documented by [Microsoft](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/language-support?tabs=text-to-speech).

## Presentation and mobile sound

The opening/pause menu includes Text & Stimme settings: Nur Text, Nur Stimme or Beides, narration volume, a neural-voice test, and a manual "Ich höre nichts" fallback. Mode and volume persist independently of music and learning progress. Text-only keeps lesson captions and the current task readable without playing narration; voice-only hides lesson captions and object-name cards; combined mode enables both.

[Unity's Mobile.muteState](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/audiosettings/mobile/mutestate) and its mute-change event detect zero media volume on Android/iOS. A detected mute, app narration volume of zero, or manual no-sound report displays a written warning and enables text even in voice-only mode. Restoring media volume restores the chosen mode. iOS's hardware Silent switch is not exposed by this API; the written sound-check hint and manual fallback cover that case without claiming automatic switch detection. Physical devices still need validation.

Stage announcements now use the same Katja Neural voice as the rest of the lesson, including all 136 stage numbers. Captions follow each queued sentence; when a message ends, the current instruction remains visible in text-enabled modes.

## Empty home shell

The kitchen's original south door opens into a 2-metre hallway. West: living room and bedroom 1. East: bedroom 2 and a combined bathroom/WC. Each room has a floor, ceiling, enclosing walls and a window; four new interactive doors connect them to the corridor. Bedrooms use the BedroomFurnishings group and bathroom fixtures use BathroomFurnishings; living room and hallway use LivingHallFurnishings (F05–F08). Architectural names are recorded in section 12 of tools.txt. Rebuild using Tools > DeutschRoom > Rebuild Empty Home outside Play Mode.

## Lesson minimap

A north-up floor plan at the top shows the full kitchen and home, physical wall/door outlines, the table, a mint player heading arrow and a pulsing gold target with horizontal distance. The map reads live world positions, including objects inside moving drawers. It uses flat GUI geometry rather than an extra camera or render texture. Narrow-screen controls leave room for it; the map consumes touches so it cannot start look gestures.

Search tasks point to the exact requested pickup. Article cleanup points to the next object named for removal; holding a cleanup/wrong item points to a suggested floor location outside the table. During sorting, an unheld player is directed to a nearby matching object that is not already on the table. Holding a matching object points to the tabletop. Incorrect table items take priority when hands are empty. Preview and completed lessons have no stale target marker.

## Visual polish

Tools > DeutschRoom > Apply Visual Polish saves updated surface assignments, procedurally authored 512px albedo/normal textures, physical-scale box UVs, small edge bevels, refined tableware visuals, baseboards and bathroom tile cladding. Assets live in Assets/Art/VisualPolish. The original pickup names, logical objects and colliders remain in place. A duplicate legacy ground renderer is hidden to prevent floor z-fighting; its collider is preserved.

Both URP profiles render at native scale. Forward mobile uses 2x MSAA; the camera component selects SMAA for desktop. Shadow/ambient lighting, ACES tone mapping and a baked kitchen reflection probe improve material readability. Probe baking and before/after screenshots require a graphics-enabled editor. Documentation/VisualReview contains comparison views; this is not a real-device performance benchmark. Reapply after rebuilding generated scene geometry.

The furnishings list in tools.txt section 14 contains 80 planned instances (80 built, zero pending), grouped F01–F16. Each [ ] row means not added at that room/location. Remove its prefix only when that exact instance is actually built and verified. Do not create learning tasks or spawn planned furniture solely from the inventory list.

## Bathroom batches and component identification

F10/F11 add ten fixed fixtures with FixedLearningTarget order 0–9, following the 53 pickup objectives. Identify them by marker/F; parts belonging to the target also complete it. Article tasks now display 134–136 after the bedroom additions. SaveData.fixedCount and RestoreStage preserve old article/completion phases and mid-fixture progress. Full expanded playthroughs start from Restart; old completed profiles remain completed.

VocabularyPart is separate from KitchenInteractable: naming a handle never replaces its opening action. Part markers prefer the closest component on ties, identify the component and its owner, and hide while the owning pickup is carried. The hood and major room objects have whole-object vocabulary labels. All names and fixed objective IDs are in BathroomContent.json and tools.txt section 15. Two vanity doors open; flushing and shower-water simulation are not implemented. All 80 planned furnishing instances are now built.

## Bedroom batches F01–F04

BedroomFurnishingBuilder adds 20 room-specific fixed identification targets, order 10–29, after the ten bathroom targets. Tasks 64–83 identify bedroom contents; article tasks now display 134–136. Fixed objects include bedding, book and hanger in this batch; these are not new portable pickups. Each same-named bed/pillow in the other room is a separate target. Wardrobe doors and eight total bedroom doors/drawers work through Openable; component names retain those actions.

BedroomContent.json records item IDs, room, order and trilingual names. Inventory prefixes are removed only for F01–F04. All 80 planned instances are built; none remain pending. Rebuild only this group with Tools > DeutschRoom > Rebuild Bedrooms F01-F04. Existing article/completion saves migrate to the same phase; use Restart for a full expanded run. Real-device performance remains unmeasured.

## Living-room and entrance batches F05–F08

LivingHallFurnishingBuilder creates twenty fixed targets at order 30–49 in LivingHallFurnishings. Tasks 84–103 identify these objects; articles are 134–136. The original 53 pickups and previous 30 fixed targets retain their order. RestoreStage migrates prior article/completion profiles using saved fixedCount without resetting progress. The TV remote, cushion and throw are fixed identification objects, not additional pickups. The TV shows a still landscape image, living-room lamps respond to the hallway switch and the mirror uses polished metal rather than a live reflection.

LivingHallContent.json and tools.txt section 17 record the trilingual names, IDs and component vocabulary. Rebuild with Tools > DeutschRoom > Rebuild Living Room and Hall F05-F08. Two shoe-cabinet doors open; named handles retain their Openable owner. Rug and curtain markers use exposed anchors. Routes down the hall and into the living room remain clear with storage closed. The F14 bin bag and F15/F16 are now built in FinalFurnishings.

## House accessories F09, F12, F13 and first four F14 items

HouseAccessoryBuilder creates twenty fixed targets at orders 50–69 in HouseAccessories. Identify them at tasks 104–123; article tasks now display 134–136. Previous 53 pickup and 50 fixed target orders remain intact. RestoreStage migrates previous article/completion phases using saved fixedCount; existing fixed-target progress is unchanged. Restart starts the full expanded sequence.

HouseAccessoryContent.json records the trilingual catalog. Hallway: key/rack, umbrella/stand, clock. Bathroom: mirror, towel/rail, soap, toilet brush/paper holder, washer/basket/detergent and four cleaning tools. Bedroom 2: drying rack and peg. All are fixed, including the small items. Washer door and detergent drawer open through Openable, preserving named part ownership. Washing/cleaning cycles, umbrella folding, running clock and live reflections are not simulated. The final ten rows (F14 bin bag and F15/F16) are built in FinalFurnishings. Rebuild via Tools > DeutschRoom > Rebuild Accessories F09 F12-F14, preserving other furnishing roots.

## Final ten F14–F16: inventory complete

FinalFurnishingBuilder creates FinalFurnishings with fixed orders 70–79, tasks 124–133. Articles are 134–136. All eighty planned instances are built, with no pending item rows. FinalFurnishingContent.json and tools.txt section 19 record the trilingual catalog. Previous 53 pickups and 70 fixed target orders stay unchanged; old article/completion saves retain the equivalent phase through RestoreStage. Restart runs the full expanded sequence.

The hallway rocker owns RoomLightSwitch while its parent retains the fixed learning target. E/Use toggles the living room's ambient, floor and ceiling lights; F/marker identification also works without replacing the action. The switch resets on after reloading, like unsaved door states. Thermostat and appliances are fixed identification models: no heating, live socket operation, cutting, toasting, brewing or weighing simulation. Rebuild with Tools > DeutschRoom > Rebuild Final Ten F14-F16 and preserve all five furnishing roots. Real-phone testing is left to the user, as requested.
