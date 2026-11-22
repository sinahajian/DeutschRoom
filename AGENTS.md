# DeutschRoom project instructions

## Keep the user's design documents current

The user explicitly requires ongoing documentation updates as the game changes.

Before changing game design, content, scenes, controls, UI, audio or progression, read:

- `Documentation/GDD.md` — living requirements and implementation status in Persian, English and German.
- `Documentation/GameStages.md` — the player-facing sequence from stage 0, objectives and completion rules.
- `Documentation/tools.txt` when objects, rooms or architecture are affected.

For every relevant user-requested game change, update both design documents in the same task:

1. Update the affected GDD requirements in all three languages. Distinguish implemented, requested future work, unverified behavior and current limitations. Do not label a plan as implemented.
2. Update affected stage entries, shared rules, numbering or ending in GameStages.md. Append actual new objectives when implemented; do not invent playable stages for UI/audio-only changes or merely planned content.
3. Preserve existing document tracking IDs and change history. These IDs are documentation identifiers, not a claim that runtime task assets exist.
4. Verify actual progression against `Assets/scripts/LearningProgress.cs`, `PickupItem.LessonOrder` and the pickup GameObject names in `Assets/Scenes/SampleScene.unity`. Preserve the spoon-first swap. Do not assume that documentation section numbers in tools.txt equal gameplay stage numbers.
5. When new objects, room elements or architecture are added, register their German name including article, Persian translation and English translation in tools.txt. Keep internal construction numbers/directions out of player-visible vocabulary.
6. Record the change and update the document date using the user's local date. Keep Persian, English and German descriptions equivalent. For changes unrelated to the game, do not fabricate a design change.

The three-language requirement applies to these documents. It does not by itself authorize translating the game's UI or altering gameplay to match a draft. For documentation-only work, verify links, counts and consistency; a Unity run is not required.

Bedroom F01–F04, living-room/entrance F05–F08 and bathroom F10/F11 are built. F09, F12/F13 and the first four F14 rows are also built. The final F14 bin bag and F15/F16 are also built; all eighty planned instances are complete. Additional unplanned furniture requires a subsequent request.

## Planned furniture inventory

`Documentation/tools.txt`, section 14, contains room-specific future batches F01–F16. A row starting with `[ ]` means NOT ADDED at that location. Do not spawn these items just because they are listed. When the user requests a batch and an item is actually built and verified, remove only that row's `[ ] ` prefix so it becomes a normal inventory entry. Keep other instances of the same noun in other rooms pending. Update GDD.md and GameStages.md for the implemented change; planned batches alone are not playable stages.

After rebuilding generated kitchen contents, garden or home architecture, reapply Tools > DeutschRoom > Apply Visual Polish if those objects/material assignments were regenerated. Preserve subsequent inventory sections in tools.txt when rebuilding an earlier section.

FixedLearningTarget entries now follow portable pickup targets in LearningProgress. Read both target populations before changing stage numbers or migrating saves. VocabularyPart is identification metadata; do not replace the interaction-owning KitchenInteractable on handles, lids or legs with a label component. Component identification should preserve pickup/open actions and display/speak its owner when available.

After rebuilding home/kitchen architecture, rerun Bathroom & Vocabulary annotation when needed to restore component/whole-object labels on regenerated objects. Preserve the bathroom root and completed F10/F11 inventory status. BathroomFurnishingBuilder generates fixed targets and part metadata; its catalog is Documentation/BathroomContent.json. Use Tools/Voice/generate.py after adding new phrases to Voice.json.

BedroomFurnishingBuilder generates the 20 F01–F04 instances in a separate BedroomFurnishings root; Documentation/BedroomContent.json records fixed orders 10–29, after bathroom orders 0–9. Keep both generated furnishing roots when editing the house. All eighty planned instances are built; none remain pending. Appending fixed targets shifts the article-task display numbers; migrate previous fixed counts without restarting or erasing the user's saved profile.

LivingHallFurnishingBuilder generates F05–F08 in LivingHallFurnishings, fixed orders 30–49; preserve this root alongside bedrooms and bathroom. LivingHallContent.json records its catalog. Current sequence: 53 pickups, 80 fixed targets, three article tasks (136 total). Keep corridor and doorway routes clear when placing further furnishings.

HouseAccessoryBuilder generates HouseAccessories, fixed orders 50–69; preserve it with all other furnishing roots. HouseAccessoryContent.json records F09, F12, F13 and the first four F14 items. Washing machine/basket/detergent are in the bathroom northeast; drying rack/peg in bedroom 2; cleaning tools on the bathroom south wall. All are fixed identification targets, with two washer Openables. All eighty planned instances are now built.

FinalFurnishingBuilder generates FinalFurnishings at orders 70–79, catalog FinalFurnishingContent.json. Preserve all five furnishing roots. RoomLightSwitch is the child action owner for the hallway rocker and toggles three living-room lights; its parent is the fixed learning target. All 80 F01–F16 item rows are built. Device testing is performed by the user; do not install onto their phone as part of furnishing work.


## Keep the player guide current

For every new or changed player-facing feature, update the English How to play guide in Assets/scripts/RoomExperience.cs in the same task. Explain how the player accesses and uses it, with accurate touch and keyboard/mouse instructions where applicable. Also maintain the existing trilingual GDD/GameStages updates. New or changed lesson instructions must include a matching englishInstruction in IntroA11.json; translation help must not complete tasks or reveal answer choices.
