# Lesson validation

Run from the repository root:

- `python3 Tools/Lessons/validate_assets.py` checks actual scene bindings/orders, authored answers, task/document counts, links, and mesh-derived fit of spatial targets in the practice zone. It does not execute Unity physics or render the UI.
- Compile the runtime scripts, then run `dotnet run --project Tools/Lessons/ManagedChecks/ManagedChecks.csproj -p:DeutschRoomAssembly=/absolute/path/to/Assembly-CSharp.dll -- Assets/Resources/Lessons/IntroA11.json` using .NET 10. This executes the compiled catalog, migration, answer/action and placement geometry rules without Unity native calls. Use `BaseIntermediateOutputPath` and `OutputPath` overrides for outputs outside the repository.
- `python Tools/Voice/generate.py --pack intro --audit-only` checks installed recordings using the voice tool's dependencies.
- `IntroLessonValidation.Run` is integrated into the existing Unity interaction validation. It exercises actual pickup, primary identification, placement, resets, choice-pointer routing, original-pose saving and legacy profile preservation. The previous headless Unity licensing blocker prevents claiming a current native interaction pass; phone verification is separate.

Current implemented range: CUR-001–430. The original 400 baseline and its free-production limits are retained in the plan.

CUR-291–330 add data-driven independent scenarios. `ScenarioLessonValidation` covers real action callbacks, explicit settling, report touch routing, forbidden actions and confirmations. Settling is forced after an actual interaction in this synchronous harness; elapsed animation, full rendering/reachability and phone behavior still require native/device testing. All language reports currently use three choices; free production remains planned.

CUR-331–380 add protected conditional states, held/zone presets, clearance exercises, desk/shoe controls and four clarification dialogues. E/M source packets are exact four-page German texts with narration; reading acknowledgements are not comprehension or C1 production scores. First viewing requires all pages, later same-session views allow a shortcut. Full writing/speech criteria remain planned. The native harness includes reader-page/dialogue touch routing, exclusions, protected devices and distance guard attempts; native rendering/reachability remains unverified.

CUR-381–430 completes W/H and adds thirty C1.2 integration tasks requested beyond the revisable 400 baseline. Four source packets contain sixteen exact German pages. Stage400 is an intermediate checkpoint, not course completion; old380/400 IDs resume normally. Same runtime operations and 53 portable/80 fixed scene populations; no phone installation.
