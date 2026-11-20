# DeutschRoom — مراحل بازی / Game stages / Spielstufen

آخرین به‌روزرسانی / Last updated / Zuletzt aktualisiert: **2026-11-20 — Europe/Berlin**

[GDD](GDD.md) · [فهرست نام‌ها / Vocabulary / Wortliste](tools.txt) · [راهنمای فنی / Technical guide / Technischer Leitfaden](Learning.md)

## شماره‌گذاری / Numbering / Nummerierung

**فارسی:** مرحلهٔ ۰ ورود و تنظیمات است. سپس ۵۳ هدف برداشتن وسیله، ۱۰ هدف شناسایی حمام، ۲۰ هدف شناسایی دو اتاق خواب، ۲۰ هدف پذیرایی و راهرو، ۲۰ هدف لوازم خانه، ۱۰ هدف نهایی و سه تمرین مقاله اجرا می‌شوند؛ ۱۳۶ وظیفه در مجموع. شمارهٔ ذخیرهٔ stage یک واحد از شمارهٔ وظیفهٔ نمایش‌داده‌شده کمتر است. بخش‌های tools.txt مراحل توسعه‌اند، نه شمارهٔ وظیفه.

**English:** Stage 0 is entry/setup, followed by 53 pickup objectives, 10 bathroom, 20 bedroom, 20 living-room/hallway and 20 house-accessory and 10 final-furnishing identification objectives and three article tasks: 136 tasks total. Saved stage is one below the displayed task number. tools.txt sections refer to development phases, not task numbers.

**Deutsch:** Stufe 0 ist Einstieg/Vorbereitung, gefolgt von 53 Aufhebezielen, 10 Bad-, 20 Schlafzimmer-, 20 Wohnzimmer-/Flur- und 20 Haushaltszubehör- und 10 abschließenden Einrichtungs-Benennungszielen und drei Artikelaufgaben: insgesamt 136 Aufgaben. Der gespeicherte stage-Wert liegt eins unter der angezeigten Aufgabennummer. tools.txt-Abschnitte bezeichnen Entwicklungsphasen, keine Aufgabennummern.

## راهنمای مشترک رابط / Shared interface guidance / Gemeinsame Bedienhinweise

**فارسی:** پیاده‌شده در کد در 2026-11-20: متن راهنما شش ثانیه می‌ماند و طی یک ثانیه محو می‌شود؛ هر جملهٔ تازه زمان را تمدید می‌کند. تکرار راهنما متن را برمی‌گرداند؛ حالت فقط صدا بدون هشدار بی‌صدایی متن ندارد. نقشهٔ کوچک بالا چپ با لمس بزرگ و با لمس دوباره کوچک می‌شود؛ فقط کف و دیوارهای واقعی، جهت بازیکن، هدف، فاصله و مسیر زرد از درگاه‌ها دیده می‌شوند. سقف و شبکهٔ تزئینی حذف شدند. در نقشهٔ بزرگ حرکت متوقف می‌شود. یک دکمه کنار نقشه گزینه‌های توقف و موسیقی را باز می‌کند. مربع‌های روی اشیا حذف شدند. نزدیک شوید و به شیء نگاه کنید تا دکمهٔ گرد برداشتن یا استفاده ظاهر شود؛ گذاشتن و چرخش هنگام حمل باقی‌اند. نام با دکمهٔ Anhören یا F پخش می‌شود؛ عبور از کنار اشیا و استفاده در پیش‌نمایش نام‌گویی خودکار ندارند. راهنمای مرحله و بازخورد موفقیت باقی‌اند. مسیر، مبلمان را دور نمی‌زند و درهای بسته را قابل بازکردن فرض می‌کند. کامپایل مستقل همهٔ اسکریپت‌های زمان اجرا با اسمبلی‌های Unity موفق بود. اجرای دیداری Unity و آزمون گوشی هنوز تأیید نشده‌اند؛ Unity در این محیط پیش از گزارش با کد ۱۳۷ متوقف شد. ۵۳ هدف برداشتن با قاشقِ اول، ۸۰ هدف ثابت و سه تمرین مقاله، مجموع ۱۳۶ وظیفه و ذخیره‌ها تغییر نکرده‌اند.

**English:** Implemented in code on 2026-11-20: captions stay for six seconds and fade for one; each new sentence restarts the timer. Repeat restores captions; voice-only without an audio warning has no captions. The small top-left map expands on tap and collapses on another tap, showing actual floors/walls, heading, target, distance and yellow routing through doorways. Ceilings and decorative grid are excluded. Movement stops with the expanded map. One button beside the map reveals Pause and Music. Object squares are removed. Approach and aim to reveal the round pickup/use button; place/rotate remain contextual while carrying. Anhören or F names the object; walking past objects and using them in preview do not automatically identify them. Lesson guidance and success feedback remain. Routing does not avoid furniture and assumes closed doors can be opened. Standalone compilation of all runtime scripts against Unity assemblies passed. Unity visual execution and phone tests remain unverified; Unity exited 137 before logging in this environment. The 53 spoon-first pickup targets, 80 fixed targets and three article tasks, 136 total, and saves are unchanged.

**Deutsch:** Im Code am 2026-11-20 umgesetzt: Hinweise bleiben sechs Sekunden und verblassen eine Sekunde; jeder neue Satz startet die Zeit neu. Wiederholen zeigt Hinweise erneut; nur Stimme ohne Tonwarnung bleibt ohne Hinweistext. Die kleine Karte oben links wird durch Antippen vergrößert und erneut verkleinert; sie zeigt echte Böden/Wände, Blickrichtung, Ziel, Entfernung und gelbe Wege durch Türöffnungen. Decken und Ziergitter entfallen. Bei großer Karte stoppt Bewegung. Eine Taste neben der Karte öffnet Pause und Musik. Objektquadrate entfallen. Nähern und anvisieren zeigt die runde Aufheben-/Benutzen-Taste; Ablegen/Drehen bleiben beim Tragen situationsabhängig. Anhören oder F benennt das Objekt; Vorbeigehen und Benutzen in der Vorschau benennen nicht automatisch. Aufgabenansagen und Erfolgsrückmeldung bleiben. Wege umgehen keine Möbel und setzen öffenbare Türen voraus. Separate Kompilierung aller Laufzeitskripte gegen Unity-Assemblies erfolgreich. Sichtprüfung in Unity und Smartphone-Tests bleiben unbestätigt; Unity endete hier vor dem Protokoll mit Code 137. Die 53 Aufhebeziele mit Löffel zuerst, 80 festen Ziele und drei Artikelaufgaben, insgesamt 136, sowie Speicherstände bleiben unverändert.



### دکمه‌های دایره‌ای و نام کل/جزء / Circular controls and whole/part names / Runde Tasten und Gesamt-/Teilnamen — 2026-11-20

**فارسی — پیاده‌شده در کد:** دکمهٔ مستطیلی شنیدن از رابط گوشی حذف شده است. فقط یک دایرهٔ Anhören وجود دارد؛ برای وسیلهٔ ثابت، دایرهٔ استفادهٔ تکراری حذف می‌شود. برداشتن، بازکردن، بستن و استفاده در دایرهٔ عمل جداگانه کنار شنیدن باقی‌اند. هنگام نگاه به جزء دارای نام متفاوت، دایرهٔ Detail کنار Anhören ظاهر می‌شود. Anhören نام کل وسیله یا صاحب جزء را می‌گوید، مثل der Regenschirm، der Kühlschrank یا der Spiegel؛ Detail فقط نام دقیق جزء روبه‌رو را می‌گوید، مثل das Schirmdach، der Griff یا der Rahmen، بدون تکرار نام صاحب. جزئیات فقط وقتی دادهٔ نام‌گذاری موجود باشد در دسترس‌اند. F نام کل و Shift+F نام جزء است. Detail وظیفهٔ کل وسیله را کامل نمی‌کند؛ شناسایی اصلی از روی خود وسیله یا جزء آن، هدف ثابت را طبق قاعدهٔ قبلی جلو می‌برد. بازکردن، برداشتن و کلید چراغ صاحب جزء حفظ شده‌اند. نام‌گویی عادی متن مرحله را برنمی‌گرداند. ۵۳ هدف برداشتن، ۸۰ هدف ثابت و سه تمرین مقاله (۱۳۶ وظیفه)، قاشق اول و ذخیره‌ها حفظ شده‌اند؛ مرحلهٔ جدیدی اضافه نشد.

**English — implemented in code:** The rectangular mobile hearing button is removed. There is one circular Anhören control; fixed vocabulary objects no longer have a duplicate use/identify circle. Pickup, open, close and use retain a separate action circle beside hearing. A Detail circle appears beside Anhören when the aimed part has a distinct annotated name. Anhören names only the whole object/part owner, such as der Regenschirm, der Kühlschrank or der Spiegel; Detail names only the exact aimed part, such as das Schirmdach, der Griff or der Rahmen, without repeating the owner. Details require existing part metadata. F names the whole and Shift+F names the part. Detail does not complete the whole-object lesson; primary identification while aiming at the whole or a part still advances its fixed target. The owner's opening, pickup and light-switch actions remain intact. Ordinary naming does not restore stage captions. The 53 pickup targets, 80 fixed targets and three article tasks (136 total), spoon-first order and saves remain unchanged; no new stages.

**Deutsch — im Code umgesetzt:** Die rechteckige mobile Anhören-Taste entfällt. Es gibt eine runde Anhören-Taste; feste Vokabelobjekte haben keine doppelte Benutzen-/Benennen-Taste mehr. Aufheben, Öffnen, Schließen und Benutzen bleiben als eigene runde Aktion daneben erhalten. Eine Detail-Taste erscheint neben Anhören, wenn das anvisierte Teil einen eigenen abweichenden Namen hat. Anhören nennt nur das Gesamtobjekt/den Besitzer, etwa der Regenschirm, der Kühlschrank oder der Spiegel; Detail nennt nur das genaue anvisierte Teil, etwa das Schirmdach, der Griff oder der Rahmen, ohne den Besitzer zu wiederholen. Details benötigen vorhandene Teilmetadaten. F benennt das Gesamtobjekt, Shift+F das Teil. Detail erfüllt keine Gesamtobjektaufgabe; die Hauptbenennung beim Anvisieren des Objekts oder eines Teils erfüllt weiterhin sein festes Ziel. Öffnen, Aufheben und Lichtschalten bleiben erhalten. Gewöhnliche Benennung zeigt den Aufgabentext nicht erneut. Die 53 Aufhebeziele, 80 festen Ziele und drei Artikelaufgaben (136 insgesamt), Löffel zuerst und Speicherstände bleiben erhalten; keine neuen Aufgaben.

**بررسی / Verification / Prüfung:** Standalone runtime compilation passed. Unity Play Mode verification passed for real part owners, separate speech, circular touch routing and the full interaction/progression suite; real-phone visual/touch verification remains pending. / کامپایل مستقل موفق بود؛ آزمون Play Mode صاحب واقعی جزء، صدای جدا، لمس دایره‌ها و مجموعهٔ کامل تعامل و پیشرفت موفق بود؛ آزمون دیداری و لمس روی گوشی هنوز تأیید نشده است. / Separate Laufzeitkompilierung erfolgreich; Play-Mode-Prüfung für echte Teilbesitzer, getrennte Sprache, runde Touch-Steuerung und die vollständige Interaktions-/Fortschrittsprüfung bestanden; Sicht-/Touchprüfung auf echten Smartphones bleibt offen.



### شخصیت بازیکن در اول‌شخص / First-person player avatar / Spielfigur in der Ichperspektive — 2026-11-20

**فارسی — پیاده‌شده در کد:** بازیکن یک بدن انسانی ساده و سبک با لباس سبزآبی، شلوار تیره، کفش، دست و انگشت شست دارد. دست‌ها نزدیک پایین دید و بدن و پاها با نگاه به پایین دیده می‌شوند. حرکت دست‌وپا از جابه‌جایی واقعی پیروی می‌کند؛ در توقف بازی ثابت است. بدن با ارتفاع فعلی نشستن/درازکشیدن هماهنگ می‌شود و دست راست هنگام حمل به نزدیکی وسیله می‌رود. سر و مو و اجزای صورت برای دوربین خود بازیکن فقط سایه دارند تا دید را نپوشانند. مدل هنگام شروع یک‌بار ساخته و با بازیکن پاک می‌شود؛ برخوردگرهای تزئینی حذف می‌شوند و برخورد حرکت فقط با CharacterController موجود است. هدف آموزشی، نام قابل لمس یا صدای تازه‌ای اضافه نشده است. مدل بازیکن عضو صف وسایل F01–F16 نیست. قاشق اول، ۵۳ هدف قابل حمل، ۸۰ هدف ثابت، سه تمرین مقاله و ذخیره‌ها حفظ شده‌اند. دست‌ها انیمیشن گرفتن انگشتی یا تطبیق با شکل هر وسیله ندارند؛ زیر میز یا کنار دیوار ممکن است اجزای نمایشی از سطح رد شوند، چون برخوردگر بدن همان کپسول فعلی است. آزمون گوشی و عملکرد روی دستگاه هنوز تأیید نشده‌اند.

**English — implemented in code:** The player has a lightweight stylized human body with a teal sweatshirt, dark trousers, sneakers, hands and thumbs. Hands sit near the lower view; looking down reveals the body and feet. Limb motion follows actual displacement and freezes while paused. The body follows the current sitting/lying height, and the right hand moves near a carried item. Head, hair and facial features cast shadows only in normal play so they cannot block the player's camera. The model is created once at startup and cleaned up with the player; decorative colliders are removed, leaving the existing CharacterController responsible for movement collision. No learning target, tappable vocabulary or new speech is added. The avatar is outside the F01–F16 furnishing queue. Spoon-first order, 53 portable targets, 80 fixed targets, three article tasks and saves are preserved. Hands have no finger-grip animation or per-object fitting; visual limbs may intersect surfaces near walls/under furniture because the existing capsule remains the collision body. Phone appearance and device performance remain unverified.

**Deutsch — im Code umgesetzt:** Die Spielfigur hat einen einfachen leichten menschlichen Körper mit türkisgrünem Sweatshirt, dunkler Hose, Turnschuhen, Händen und Daumen. Hände liegen am unteren Bildrand; beim Herunterschauen sind Körper und Füße sichtbar. Gliedmaßen folgen der tatsächlichen Bewegung und stehen bei Pause still. Der Körper folgt der aktuellen Sitz-/Liegehöhe; die rechte Hand nähert sich einem getragenen Gegenstand. Kopf, Haare und Gesichtsteile werfen im normalen Spiel nur Schatten, damit sie die eigene Kamera nicht verdecken. Das Modell entsteht einmal beim Start und wird mit der Spielfigur entfernt; dekorative Collider entfallen, Bewegungskollision bleibt beim vorhandenen CharacterController. Keine neuen Lernziele, antippbaren Wörter oder Sprachaufnahmen. Die Spielfigur gehört nicht zum Einrichtungsplan F01–F16. Löffel zuerst, 53 tragbare Ziele, 80 feste Ziele, drei Artikelaufgaben und Speicherstände bleiben erhalten. Keine Finger-Greifanimation oder Anpassung an einzelne Gegenstände; sichtbare Gliedmaßen können Wände/Möbel durchdringen, da die bisherige Kollisionskapsel bleibt. Smartphone-Darstellung und Geräteleistung sind noch ungeprüft.


**بررسی / Validation / Prüfung:** آزمون Play Mode بدن، دست‌وپا، حذف برخوردگر تزئینی و دیده‌نشدن سر توسط دوربین خود، همراه مجموعهٔ کامل تعامل، وضعیت بدن و پیشرفت موفق بود. / Play Mode body, limb, collider-exclusion and own-head visibility checks passed with the full interaction, posture and progression suite. / Play-Mode-Prüfungen für Körper, Gliedmaßen, Collider-Ausschluss und Ausblendung des eigenen Kopfes sowie sämtliche Interaktions-, Haltungs- und Fortschrittsprüfungen bestanden.

[تصاویر شخصیت / Avatar previews / Vorschau der Spielfigur](VisualReview/README.md)

| تاریخ / Date / Datum | فارسی | English | Deutsch |
|---|---|---|---|
| 2026-11-20 | بدن و دست‌وپای بازیکن با حرکت و حالت بدن؛ بدون مرحلهٔ تازه | Player body/limbs follow movement and posture; no new stages | Spielerkörper/Gliedmaßen folgen Bewegung und Haltung; keine neuen Aufgaben |

## مرحلهٔ ۰ / Stage 0 / Stufe 0 — آماده‌سازی / Setup / Vorbereitung

شناسهٔ سند / Document ID / Dokument-ID: `SETUP` — **پیاده‌شده / Implemented / Umgesetzt**

| فارسی | English | Deutsch |
|---|---|---|
| منوی ورود، شروع/ادامه/شروع دوباره، انتخاب کنترل، موسیقی و تنظیم متن/صدا/هر دو | Opening menu, start/continue/restart, controls, music and text/voice/both selection | Startmenü, Start/Fortsetzen/Neustart, Steuerung, Musik und Auswahl Text/Stimme/beides |
| امکان تست صدا و دریافت متن جایگزین هنگام بی‌صدایی؛ کلید Silent آیفون نیازمند بررسی دستی است | Sound test and text fallback when inaudible; the iPhone Silent switch requires manual checking | Klangtest und Ersatztext bei fehlendem Ton; der iPhone-Stummschalter muss manuell geprüft werden |
| بدون هدف زرد مرحله؛ پس از شروع، هدف ۱ یا وظیفهٔ ذخیره‌شده فعال می‌شود | No yellow task target; starting activates objective 1 or the saved task | Keine gelbe Aufgabenmarkierung; Start aktiviert Ziel 1 oder die gespeicherte Aufgabe |

## مراحل ۱ تا ۵۳ / Stages 1–53 / Stufen 1–53 — جست‌وجو / Search / Suchen

**فارسی:** همهٔ ردیف‌ها پیاده‌شده‌اند. شرط تکمیل، برداشتن همان هدف است؛ لمس Anhören برای شنیدن نام به‌تنهایی کافی نیست. هدف اشتباه مرحله را جلو نمی‌برد. پس از موفقیت، بازی نام وسیله، راهنمای گذاشتن، شماره و هدف بعدی را اعلام می‌کند؛ قبل از برداشتن بعدی باید دست خالی شود. مینی‌مپ جای زندهٔ هدف، حتی در کابینت یا کشو، را نشان می‌دهد. شماره‌های ۱ تا ۱۷ محتوای اولیه و ۱۸ تا ۵۳ محتوای اضافه‌شده‌اند.

**English:** All rows are implemented. Completion requires picking up the specific target; tapping Anhören alone is insufficient. A wrong pickup does not advance. Success names the object and announces putting it down, the next stage number and target; free the hand before picking up the next item. The minimap follows the target's live position, including cupboards/drawer. Stages 1–17 are the original content; 18–53 are additions.

**Deutsch:** Alle Zeilen sind umgesetzt. Zum Abschluss muss das bestimmte Ziel aufgehoben werden; bloßes Antippen zur Benennung reicht nicht. Ein falscher Gegenstand führt nicht weiter. Bei Erfolg werden Gegenstand, Ablegen, nächste Aufgabennummer und nächstes Ziel angesagt; vor dem nächsten Aufheben die Hand leeren. Die Minikarte folgt der aktuellen Zielposition, auch in Schrank oder Schublade. Stufen 1–17 sind ursprünglicher Inhalt, 18–53 Ergänzungen.

| مرحله / Stage / Stufe | هدف و شناسهٔ سند / Target and document ID / Ziel und Dokument-ID | فارسی | English | Deutsch |
|---|---|---|---|---|
| 1 | der Löffel<br>`SEARCH-Loeffel_Spoon` | «قاشق» را پیدا کن و بردار. | Find and pick up the spoon. | Finde als Nächstes den Löffel und hebe den Gegenstand auf. |
| 2 | die Bratpfanne<br>`SEARCH-Bratpfanne_FryingPan` | «ماهیتابه» را پیدا کن و بردار. | Find and pick up the frying pan. | Finde als Nächstes die Bratpfanne und hebe den Gegenstand auf. |
| 3 | die Gabel<br>`SEARCH-Gabel_Fork` | «چنگال» را پیدا کن و بردار. | Find and pick up the fork. | Finde als Nächstes die Gabel und hebe den Gegenstand auf. |
| 4 | das Geschirrtuch<br>`SEARCH-Geschirrtuch_Portable` | «دستمال آشپزخانه» را پیدا کن و بردار. | Find and pick up the dish towel. | Finde als Nächstes das Geschirrtuch und hebe den Gegenstand auf. |
| 5 | das Glas<br>`SEARCH-Glas_DrinkingGlass` | «لیوان» را پیدا کن و بردار. | Find and pick up the drinking glass. | Finde als Nächstes das Glas und hebe den Gegenstand auf. |
| 6 | der Kochtopf<br>`SEARCH-Kochtopf_Pot` | «قابلمه» را پیدا کن و بردار. | Find and pick up the cooking pot. | Finde als Nächstes den Kochtopf und hebe den Gegenstand auf. |
| 7 | das Abtropfgestell<br>`SEARCH-Abtropfgestell_DishRack` | «آبچکان» را پیدا کن و بردار. | Find and pick up the dish drying rack. | Finde als Nächstes das Abtropfgestell und hebe den Gegenstand auf. |
| 8 | das Messer<br>`SEARCH-Messer_Knife` | «چاقو» را پیدا کن و بردار. | Find and pick up the knife. | Finde als Nächstes das Messer und hebe den Gegenstand auf. |
| 9 | der Mülleimer<br>`SEARCH-Muelleimer_TrashBin` | «سطل زباله» را پیدا کن و بردار. | Find and pick up the trash bin. | Finde als Nächstes den Mülleimer und hebe den Gegenstand auf. |
| 10 | der Pfefferstreuer<br>`SEARCH-Pfefferstreuer_PepperShaker` | «فلفلدان» را پیدا کن و بردار. | Find and pick up the pepper shaker. | Finde als Nächstes den Pfefferstreuer und hebe den Gegenstand auf. |
| 11 | der Salzstreuer<br>`SEARCH-Salzstreuer_SaltShaker` | «نمکدان» را پیدا کن و بردار. | Find and pick up the salt shaker. | Finde als Nächstes den Salzstreuer und hebe den Gegenstand auf. |
| 12 | das Schneidebrett<br>`SEARCH-Schneidebrett_CuttingBoard` | «تخته برش» را پیدا کن و بردار. | Find and pick up the cutting board. | Finde als Nächstes das Schneidebrett und hebe den Gegenstand auf. |
| 13 | die Schüssel<br>`SEARCH-Schuessel_Bowl` | «کاسه» را پیدا کن و بردار. | Find and pick up the bowl. | Finde als Nächstes die Schüssel und hebe den Gegenstand auf. |
| 14 | das Spülmittel<br>`SEARCH-Spuelmittel_DishSoap` | «مایع ظرف‌شویی» را پیدا کن و بردار. | Find and pick up the dishwashing liquid. | Finde als Nächstes das Spülmittel und hebe den Gegenstand auf. |
| 15 | der Spülschwamm<br>`SEARCH-Spuelschwamm_Sponge` | «اسفنج ظرف‌شویی» را پیدا کن و بردار. | Find and pick up the dish sponge. | Finde als Nächstes den Spülschwamm und hebe den Gegenstand auf. |
| 16 | der Teller<br>`SEARCH-Teller_Plate` | «بشقاب» را پیدا کن و بردار. | Find and pick up the plate. | Finde als Nächstes den Teller und hebe den Gegenstand auf. |
| 17 | der Wasserkessel<br>`SEARCH-Wasserkessel_Kettle` | «کتری» را پیدا کن و بردار. | Find and pick up the kettle. | Finde als Nächstes den Wasserkessel und hebe den Gegenstand auf. |
| 18 | der Apfel<br>`SEARCH-Contents_Apple` | «سیب» را پیدا کن و بردار. | Find and pick up the apple. | Finde als Nächstes den Apfel und hebe den Gegenstand auf. |
| 19 | die Backform<br>`SEARCH-Contents_BakingTin` | «قالب کیک» را پیدا کن و بردار. | Find and pick up the baking tin. | Finde als Nächstes die Backform und hebe den Gegenstand auf. |
| 20 | der Eimer<br>`SEARCH-Contents_Bucket` | «سطل» را پیدا کن و بردار. | Find and pick up the bucket. | Finde als Nächstes den Eimer und hebe den Gegenstand auf. |
| 21 | die Butter<br>`SEARCH-Contents_Butter` | «کره» را پیدا کن و بردار. | Find and pick up the butter. | Finde als Nächstes die Butter und hebe den Gegenstand auf. |
| 22 | der Dosenöffner<br>`SEARCH-Contents_CanOpener` | «دربازکن قوطی» را پیدا کن و بردار. | Find and pick up the can opener. | Finde als Nächstes den Dosenöffner und hebe den Gegenstand auf. |
| 23 | die Auflaufform<br>`SEARCH-Contents_Casserole` | «ظرف گراتن» را پیدا کن و بردار. | Find and pick up the casserole dish. | Finde als Nächstes die Auflaufform und hebe den Gegenstand auf. |
| 24 | das Müsli<br>`SEARCH-Contents_Cereal` | «موسلی» را پیدا کن و بردار. | Find and pick up the muesli. | Finde als Nächstes das Müsli und hebe den Gegenstand auf. |
| 25 | der Käse<br>`SEARCH-Contents_Cheese` | «پنیر» را پیدا کن و بردار. | Find and pick up the cheese. | Finde als Nächstes den Käse und hebe den Gegenstand auf. |
| 26 | das Reinigungsmittel<br>`SEARCH-Contents_Cleaner` | «شوینده» را پیدا کن و بردار. | Find and pick up the cleaning product. | Finde als Nächstes das Reinigungsmittel und hebe den Gegenstand auf. |
| 27 | der Kaffee<br>`SEARCH-Contents_Coffee` | «قهوه» را پیدا کن و بردار. | Find and pick up the coffee. | Finde als Nächstes den Kaffee und hebe den Gegenstand auf. |
| 28 | das Sieb<br>`SEARCH-Contents_Colander` | «آبکش» را پیدا کن و بردار. | Find and pick up the colander. | Finde als Nächstes das Sieb und hebe den Gegenstand auf. |
| 29 | die Gurke<br>`SEARCH-Contents_Cucumber` | «خیار» را پیدا کن و بردار. | Find and pick up the cucumber. | Finde als Nächstes die Gurke und hebe den Gegenstand auf. |
| 30 | die Tasse<br>`SEARCH-Contents_Cup` | «فنجان» را پیدا کن و بردار. | Find and pick up the cup. | Finde als Nächstes die Tasse und hebe den Gegenstand auf. |
| 31 | die Spülbürste<br>`SEARCH-Contents_DishBrush` | «برس ظرف‌شویی» را پیدا کن و بردار. | Find and pick up the dish brush. | Finde als Nächstes die Spülbürste und hebe den Gegenstand auf. |
| 32 | das Ei<br>`SEARCH-Contents_Egg` | «تخم‌مرغ» را پیدا کن و بردار. | Find and pick up the egg. | Finde als Nächstes das Ei und hebe den Gegenstand auf. |
| 33 | das Mehl<br>`SEARCH-Contents_Flour` | «آرد» را پیدا کن و بردار. | Find and pick up the flour. | Finde als Nächstes das Mehl und hebe den Gegenstand auf. |
| 34 | die Reibe<br>`SEARCH-Contents_Grater` | «رنده» را پیدا کن و بردار. | Find and pick up the grater. | Finde als Nächstes die Reibe und hebe den Gegenstand auf. |
| 35 | das Eis<br>`SEARCH-Contents_IceCream` | «بستنی» را پیدا کن و بردار. | Find and pick up the ice cream. | Finde als Nächstes das Eis und hebe den Gegenstand auf. |
| 36 | die Marmelade<br>`SEARCH-Contents_Jam` | «مربا» را پیدا کن و بردار. | Find and pick up the jam. | Finde als Nächstes die Marmelade und hebe den Gegenstand auf. |
| 37 | der Saft<br>`SEARCH-Contents_Juice` | «آب‌میوه» را پیدا کن و بردار. | Find and pick up the juice. | Finde als Nächstes den Saft und hebe den Gegenstand auf. |
| 38 | der Messbecher<br>`SEARCH-Contents_MeasuringJug` | «پارچ اندازه‌گیری» را پیدا کن و بردار. | Find and pick up the measuring jug. | Finde als Nächstes den Messbecher und hebe den Gegenstand auf. |
| 39 | die Milch<br>`SEARCH-Contents_Milk` | «شیر» را پیدا کن و بردار. | Find and pick up the milk. | Finde als Nächstes die Milch und hebe den Gegenstand auf. |
| 40 | der Becher<br>`SEARCH-Contents_Mug` | «ماگ» را پیدا کن و بردار. | Find and pick up the mug. | Finde als Nächstes den Becher und hebe den Gegenstand auf. |
| 41 | die Nudeln<br>`SEARCH-Contents_Pasta` | «پاستا» را پیدا کن و بردار. | Find and pick up the pasta. | Finde als Nächstes die Nudeln und hebe den Gegenstand auf. |
| 42 | der Schäler<br>`SEARCH-Contents_Peeler` | «پوست‌کن» را پیدا کن و بردار. | Find and pick up the peeler. | Finde als Nächstes den Schäler und hebe den Gegenstand auf. |
| 43 | der Reis<br>`SEARCH-Contents_Rice` | «برنج» را پیدا کن و بردار. | Find and pick up the rice. | Finde als Nächstes den Reis und hebe den Gegenstand auf. |
| 44 | das Nudelholz<br>`SEARCH-Contents_RollingPin` | «وردنه» را پیدا کن و بردار. | Find and pick up the rolling pin. | Finde als Nächstes das Nudelholz und hebe den Gegenstand auf. |
| 45 | die Untertasse<br>`SEARCH-Contents_Saucer` | «نعلبکی» را پیدا کن و بردار. | Find and pick up the saucer. | Finde als Nächstes die Untertasse und hebe den Gegenstand auf. |
| 46 | der Pfannenwender<br>`SEARCH-Contents_Spatula` | «کفگیر» را پیدا کن و بردار. | Find and pick up the spatula. | Finde als Nächstes den Pfannenwender und hebe den Gegenstand auf. |
| 47 | die Vorratsdose<br>`SEARCH-Contents_StorageBox` | «ظرف نگهداری مواد غذایی» را پیدا کن و بردار. | Find and pick up the food storage container. | Finde als Nächstes die Vorratsdose und hebe den Gegenstand auf. |
| 48 | die Zuckerdose<br>`SEARCH-Contents_SugarBowl` | «قندان» را پیدا کن و بردار. | Find and pick up the sugar bowl. | Finde als Nächstes die Zuckerdose und hebe den Gegenstand auf. |
| 49 | der Tee<br>`SEARCH-Contents_Tea` | «چای» را پیدا کن و بردار. | Find and pick up the tea. | Finde als Nächstes den Tee und hebe den Gegenstand auf. |
| 50 | die Teekanne<br>`SEARCH-Contents_Teapot` | «قوری» را پیدا کن و بردار. | Find and pick up the teapot. | Finde als Nächstes die Teekanne und hebe den Gegenstand auf. |
| 51 | die Tomate<br>`SEARCH-Contents_Tomato` | «گوجه‌فرنگی» را پیدا کن و بردار. | Find and pick up the tomato. | Finde als Nächstes die Tomate und hebe den Gegenstand auf. |
| 52 | das Weinglas<br>`SEARCH-Contents_WineGlass` | «جام» را پیدا کن و بردار. | Find and pick up the wine glass. | Finde als Nächstes das Weinglas und hebe den Gegenstand auf. |
| 53 | der Joghurt<br>`SEARCH-Contents_Yogurt` | «ماست» را پیدا کن و بردار. | Find and pick up the yogurt. | Finde als Nächstes den Joghurt und hebe den Gegenstand auf. |


## مراحل ۵۴ تا ۶۳ / Stages 54–63 / Stufen 54–63 — شناسایی حمام / Bathroom identification / Badobjekte benennen

**فارسی:** همهٔ این ده وسیله ساخته شده‌اند و ثابت‌اند. نزدیک شوید و به شیء نگاه و Anhören را لمس کنید؛ در رایانه F نیز کار می‌کند. شناسایی اصلی هدف، حتی هنگام نگاه به جزء آن، یک وظیفه را جلو می‌برد؛ تکرار هدف قبلی پیشرفت تازه ندارد. راهنمای گذاشتن برای این وسایل پخش نمی‌شود. مینی‌مپ محل هدف فعلی را نشان می‌دهد.

**English:** All ten fixtures are built and fixed in place. Approach and aim and tap Anhören; on desktop F also works. Primary identification of the target, including while aiming at its part, advances one task; repeating the previous target does not advance again. There is no put-down instruction. The minimap shows the current target.

**Deutsch:** Alle zehn Objekte sind gebaut und fest eingebaut. Nähern, anvisieren und Anhören antippen; am Computer funktioniert auch F. Die Hauptbenennung des Ziels, auch beim Anvisieren eines Teils, führt eine Aufgabe weiter; Wiederholung des alten Ziels nicht. Kein Ablegehinweis. Die Minikarte zeigt das aktuelle Ziel.

| مرحله / Stage / Stufe | هدف و شناسه / Target and ID / Ziel und ID | فارسی | English | Deutsch |
|---|---|---|---|---|
| 54 | die Toilette<br>`IDENTIFY-Bath_Toilet` | «توالت؛ کاسه و نشیمن» را پیدا کن و نامش را شناسایی کن. | Find and identify the toilet fixture. | Finde die Toilette und tippe auf Anhören. |
| 55 | der Spülkasten<br>`IDENTIFY-Bath_Cistern` | «فلاش‌تانک» را پیدا کن و نامش را شناسایی کن. | Find and identify the toilet cistern. | Finde den Spülkasten und tippe auf Anhören. |
| 56 | das Waschbecken<br>`IDENTIFY-Bath_Washbasin` | «روشویی» را پیدا کن و نامش را شناسایی کن. | Find and identify the washbasin. | Finde das Waschbecken und tippe auf Anhören. |
| 57 | die Armatur<br>`IDENTIFY-Bath_BasinTap` | «شیر روشویی» را پیدا کن و نامش را شناسایی کن. | Find and identify the basin tap. | Finde die Armatur und tippe auf Anhören. |
| 58 | die Dusche<br>`IDENTIFY-Bath_Shower` | «دوش» را پیدا کن و نامش را شناسایی کن. | Find and identify the shower. | Finde die Dusche und tippe auf Anhören. |
| 59 | die Duschkabine<br>`IDENTIFY-Bath_Enclosure` | «کابین دوش» را پیدا کن و نامش را شناسایی کن. | Find and identify the shower enclosure. | Finde die Duschkabine und tippe auf Anhören. |
| 60 | der Duschkopf<br>`IDENTIFY-Bath_ShowerHead` | «سردوش» را پیدا کن و نامش را شناسایی کن. | Find and identify the shower head. | Finde den Duschkopf und tippe auf Anhören. |
| 61 | der Duschschlauch<br>`IDENTIFY-Bath_ShowerHose` | «شلنگ دوش» را پیدا کن و نامش را شناسایی کن. | Find and identify the shower hose. | Finde den Duschschlauch und tippe auf Anhören. |
| 62 | der Bodenablauf<br>`IDENTIFY-Bath_FloorDrain` | «کف‌شور» را پیدا کن و نامش را شناسایی کن. | Find and identify the floor drain. | Finde den Bodenablauf und tippe auf Anhören. |
| 63 | der Waschbeckenunterschrank<br>`IDENTIFY-Bath_Vanity` | «کابینت زیر روشویی» را پیدا کن و نامش را شناسایی کن. | Find and identify the vanity cabinet. | Finde den Waschbeckenunterschrank und tippe auf Anhören. |

## مراحل ۶۴ تا ۸۳ / Stages 64–83 / Stufen 64–83 — دو اتاق خواب / Two bedrooms / Zwei Schlafzimmer

**فارسی:** هر ردیف یک نمونهٔ واقعیِ ساخته‌شده از F01–F04 است. هدف با لمس Anhören، F حتی هنگام نگاه به جزء آن کامل می‌شود؛ برداشتن لازم نیست. نمونهٔ هم‌نامِ اتاق دیگر، هدف فعلی را کامل نمی‌کند. مینی‌مپ محل دقیق نمونهٔ خواسته‌شده را نشان می‌دهد. کمد و کشوها با دکمهٔ استفاده باز می‌شوند؛ چوب‌لباسی داخل کمد است.

**English:** Each row is one built F01–F04 instance. Complete it by tapping Anhören, pressing F while aiming at it or one of its parts; no pickup is required. A same-named instance in the other room does not complete the current target. The minimap points to the exact requested instance. Use opens wardrobe/drawers; the hanger is inside the wardrobe.

**Deutsch:** Jede Zeile ist eine gebaute F01–F04-Instanz. Abschluss durch Anhören oder F beim Anvisieren des Objekts oder eines Teils; Aufheben ist nicht nötig. Eine gleichnamige Instanz im anderen Zimmer erfüllt das aktuelle Ziel nicht. Die Minikarte zeigt die genaue Zielinstanz. Benutzen öffnet Schrank/Schubladen; der Bügel hängt im Schrank.

| مرحله / Stage / Stufe | هدف و شناسه / Target and ID / Ziel und ID | فارسی | English | Deutsch |
|---|---|---|---|---|
| 64 | das Bett<br>`IDENTIFY-Bedroom1_Bed` | «تخت» در اتاق خواب ۱ را شناسایی کن. | Identify the bed in bedroom 1. | Benenne das Bett in Schlafzimmer 1. |
| 65 | die Matratze<br>`IDENTIFY-Bedroom1_Mattress` | «تشک» در اتاق خواب ۱ را شناسایی کن. | Identify the mattress in bedroom 1. | Benenne die Matratze in Schlafzimmer 1. |
| 66 | das Kopfkissen<br>`IDENTIFY-Bedroom1_Pillow` | «بالش» در اتاق خواب ۱ را شناسایی کن. | Identify the pillow in bedroom 1. | Benenne das Kopfkissen in Schlafzimmer 1. |
| 67 | die Bettdecke<br>`IDENTIFY-Bedroom1_Duvet` | «لحاف» در اتاق خواب ۱ را شناسایی کن. | Identify the duvet in bedroom 1. | Benenne die Bettdecke in Schlafzimmer 1. |
| 68 | das Bettlaken<br>`IDENTIFY-Bedroom1_Sheet` | «ملحفه» در اتاق خواب ۱ را شناسایی کن. | Identify the bedsheet in bedroom 1. | Benenne das Bettlaken in Schlafzimmer 1. |
| 69 | der Kleiderschrank<br>`IDENTIFY-Bedroom1_Wardrobe` | «کمد لباس» در اتاق خواب ۱ را شناسایی کن. | Identify the wardrobe in bedroom 1. | Benenne den Kleiderschrank in Schlafzimmer 1. |
| 70 | die Kommode<br>`IDENTIFY-Bedroom1_Dresser` | «دراور» در اتاق خواب ۱ را شناسایی کن. | Identify the chest of drawers in bedroom 1. | Benenne die Kommode in Schlafzimmer 1. |
| 71 | der Nachttisch<br>`IDENTIFY-Bedroom1_Nightstand` | «پاتختی» در اتاق خواب ۱ را شناسایی کن. | Identify the bedside table in bedroom 1. | Benenne den Nachttisch in Schlafzimmer 1. |
| 72 | die Nachttischlampe<br>`IDENTIFY-Bedroom1_NightLamp` | «چراغ پاتختی» در اتاق خواب ۱ را شناسایی کن. | Identify the bedside lamp in bedroom 1. | Benenne die Nachttischlampe in Schlafzimmer 1. |
| 73 | der Kleiderbügel<br>`IDENTIFY-Bedroom1_Hanger` | «چوب‌لباسی» در اتاق خواب ۱ را شناسایی کن. | Identify the clothes hanger in bedroom 1. | Benenne den Kleiderbügel in Schlafzimmer 1. |
| 74 | das Bett<br>`IDENTIFY-Bedroom2_Bed` | «تخت» در اتاق خواب ۲ را شناسایی کن. | Identify the bed in bedroom 2. | Benenne das Bett in Schlafzimmer 2. |
| 75 | die Matratze<br>`IDENTIFY-Bedroom2_Mattress` | «تشک» در اتاق خواب ۲ را شناسایی کن. | Identify the mattress in bedroom 2. | Benenne die Matratze in Schlafzimmer 2. |
| 76 | das Kopfkissen<br>`IDENTIFY-Bedroom2_Pillow` | «بالش» در اتاق خواب ۲ را شناسایی کن. | Identify the pillow in bedroom 2. | Benenne das Kopfkissen in Schlafzimmer 2. |
| 77 | die Bettdecke<br>`IDENTIFY-Bedroom2_Duvet` | «لحاف» در اتاق خواب ۲ را شناسایی کن. | Identify the duvet in bedroom 2. | Benenne die Bettdecke in Schlafzimmer 2. |
| 78 | der Nachttisch<br>`IDENTIFY-Bedroom2_Nightstand` | «پاتختی» در اتاق خواب ۲ را شناسایی کن. | Identify the bedside table in bedroom 2. | Benenne den Nachttisch in Schlafzimmer 2. |
| 79 | der Schreibtisch<br>`IDENTIFY-Bedroom2_Desk` | «میز تحریر» در اتاق خواب ۲ را شناسایی کن. | Identify the desk in bedroom 2. | Benenne den Schreibtisch in Schlafzimmer 2. |
| 80 | der Bürostuhl<br>`IDENTIFY-Bedroom2_OfficeChair` | «صندلی کار» در اتاق خواب ۲ را شناسایی کن. | Identify the desk chair in bedroom 2. | Benenne den Bürostuhl in Schlafzimmer 2. |
| 81 | die Schreibtischlampe<br>`IDENTIFY-Bedroom2_DeskLamp` | «چراغ مطالعه» در اتاق خواب ۲ را شناسایی کن. | Identify the desk lamp in bedroom 2. | Benenne die Schreibtischlampe in Schlafzimmer 2. |
| 82 | das Bücherregal<br>`IDENTIFY-Bedroom2_Bookcase` | «قفسه کتاب» در اتاق خواب ۲ را شناسایی کن. | Identify the bookcase in bedroom 2. | Benenne das Bücherregal in Schlafzimmer 2. |
| 83 | das Buch<br>`IDENTIFY-Bedroom2_Book` | «کتاب» در اتاق خواب ۲ را شناسایی کن. | Identify the book in bedroom 2. | Benenne das Buch in Schlafzimmer 2. |

## مراحل ۸۴ تا ۱۰۳ / Stages 84–103 / Stufen 84–103 — پذیرایی و راهرو / Living room and hallway / Wohnzimmer und Flur

**فارسی:** ۲۰ نمونهٔ ساخته‌شدهٔ F05–F08 با لمس Anhören، F حتی هنگام نگاه به جزء هدف کامل می‌شوند. همه ثابت‌اند؛ کنترل تلویزیون، کوسن و پتو هم در این گروه قابل حمل نیستند. مینی‌مپ محل هر نمونه را نشان می‌دهد. دو در جاکفشی با استفاده باز می‌شوند؛ باز کردن در به‌تنهایی هدف شناسایی را کامل نمی‌کند.

**English:** Identify the twenty built F05–F08 instances by Anhören or F while aiming at the whole or an owned part. All are fixed, including the remote, cushion and throw. The minimap locates the exact instance. Use opens the two shoe-cabinet doors; opening alone does not complete identification.

**Deutsch:** Die zwanzig gebauten F05–F08-Instanzen über Anhören oder F beim Anvisieren des Objekts oder eines Teils benennen. Alle sind fest, einschließlich Fernbedienung, Kissen und Decke. Die Minikarte zeigt die genaue Instanz. Benutzen öffnet die beiden Schuhschranktüren; Öffnen allein erfüllt kein Benennungsziel.

| مرحله / Stage / Stufe | هدف و شناسه / Target and ID / Ziel und ID | فارسی | English | Deutsch |
|---|---|---|---|---|
| 84 | das Sofa<br>`IDENTIFY-Living_Sofa` | «مبل» در پذیرایی را شناسایی کن. | Identify the sofa in the living room. | Benenne das Sofa im Wohnzimmer. |
| 85 | der Sessel<br>`IDENTIFY-Living_Armchair` | «مبل تک‌نفره» در پذیرایی را شناسایی کن. | Identify the armchair in the living room. | Benenne den Sessel im Wohnzimmer. |
| 86 | der Couchtisch<br>`IDENTIFY-Living_CoffeeTable` | «میز جلو مبلی» در پذیرایی را شناسایی کن. | Identify the coffee table in the living room. | Benenne den Couchtisch im Wohnzimmer. |
| 87 | der Fernsehtisch<br>`IDENTIFY-Living_TVStand` | «میز تلویزیون» در پذیرایی را شناسایی کن. | Identify the TV stand in the living room. | Benenne den Fernsehtisch im Wohnzimmer. |
| 88 | der Fernseher<br>`IDENTIFY-Living_TV` | «تلویزیون» در پذیرایی را شناسایی کن. | Identify the television in the living room. | Benenne den Fernseher im Wohnzimmer. |
| 89 | die Fernbedienung<br>`IDENTIFY-Living_Remote` | «کنترل تلویزیون» در پذیرایی را شناسایی کن. | Identify the remote control in the living room. | Benenne die Fernbedienung im Wohnzimmer. |
| 90 | das Sofakissen<br>`IDENTIFY-Living_Cushion` | «کوسن مبل» در پذیرایی را شناسایی کن. | Identify the sofa cushion in the living room. | Benenne das Sofakissen im Wohnzimmer. |
| 91 | die Wohndecke<br>`IDENTIFY-Living_Throw` | «پتوی مبل» در پذیرایی را شناسایی کن. | Identify the throw blanket in the living room. | Benenne die Wohndecke im Wohnzimmer. |
| 92 | der Teppich<br>`IDENTIFY-Living_Rug` | «فرش» در پذیرایی را شناسایی کن. | Identify the rug in the living room. | Benenne den Teppich im Wohnzimmer. |
| 93 | der Vorhang<br>`IDENTIFY-Living_Curtain` | «پرده» در پذیرایی را شناسایی کن. | Identify the curtain in the living room. | Benenne den Vorhang im Wohnzimmer. |
| 94 | die Stehlampe<br>`IDENTIFY-Living_FloorLamp` | «چراغ ایستاده» در پذیرایی را شناسایی کن. | Identify the floor lamp in the living room. | Benenne die Stehlampe im Wohnzimmer. |
| 95 | die Deckenleuchte<br>`IDENTIFY-Living_CeilingLight` | «چراغ سقفی» در پذیرایی را شناسایی کن. | Identify the ceiling light in the living room. | Benenne die Deckenleuchte im Wohnzimmer. |
| 96 | die Zimmerpflanze<br>`IDENTIFY-Living_Plant` | «گیاه آپارتمانی» در پذیرایی را شناسایی کن. | Identify the houseplant in the living room. | Benenne die Zimmerpflanze im Wohnzimmer. |
| 97 | der Blumentopf<br>`IDENTIFY-Living_Pot` | «گلدان گیاه» در پذیرایی را شناسایی کن. | Identify the plant pot in the living room. | Benenne den Blumentopf im Wohnzimmer. |
| 98 | das Bild<br>`IDENTIFY-Living_Picture` | «تابلو» در پذیرایی را شناسایی کن. | Identify the picture in the living room. | Benenne das Bild im Wohnzimmer. |
| 99 | der Schuhschrank<br>`IDENTIFY-Hall_ShoeCabinet` | «جاکفشی» در راهرو را شناسایی کن. | Identify the shoe cabinet in the hallway. | Benenne den Schuhschrank im Flur. |
| 100 | die Garderobe<br>`IDENTIFY-Hall_CoatRack` | «رخت‌آویز ورودی» در راهرو را شناسایی کن. | Identify the coat rack in the hallway. | Benenne die Garderobe im Flur. |
| 101 | die Fußmatte<br>`IDENTIFY-Hall_Mat` | «پادری» در راهرو را شناسایی کن. | Identify the doormat in the hallway. | Benenne die Fußmatte im Flur. |
| 102 | der Spiegel<br>`IDENTIFY-Hall_Mirror` | «آینه» در راهرو را شناسایی کن. | Identify the mirror in the hallway. | Benenne den Spiegel im Flur. |
| 103 | die Sitzbank<br>`IDENTIFY-Hall_Bench` | «نیمکت ورودی» در راهرو را شناسایی کن. | Identify the entrance bench in the hallway. | Benenne die Sitzbank im Flur. |

## مراحل ۱۰۴ تا ۱۲۳ / Stages 104–123 / Stufen 104–123 — لوازم خانه / House accessories / Haushaltszubehör

**فارسی:** این ۲۰ هدف از F09، F12، F13 و چهار ردیف اول F14 با لمس Anhören، F حتی هنگام نگاه به جزء هدف کامل می‌شوند. اشیا ثابت‌اند و برداشتن لازم نیست. نقشه نمونهٔ دقیق را نشان می‌دهد؛ آینهٔ حمام با آینهٔ راهرو هدف جدا دارد. در و کشوی لباس‌شویی باز می‌شوند، ولی باز کردن به‌تنهایی هدف را کامل نمی‌کند. بند رخت و گیره در اتاق خواب ۲، باقی لوازم رخت‌شویی و نظافت در حمام‌اند.

**English:** These twenty F09, F12, F13 and first-four-F14 targets complete by Anhören or F while aiming at the whole or an owned part. Objects are fixed; pickup is not required. The map points to the exact instance; the bathroom mirror is separate from the hallway mirror. The washer door and drawer open, but opening alone does not complete identification. Drying rack/peg are in bedroom 2; other laundry and cleaning items are in the bathroom.

**Deutsch:** Diese zwanzig Ziele aus F09, F12, F13 und den ersten vier F14-Zeilen werden über Anhören oder F beim Anvisieren des Objekts oder eines Teils erfüllt. Die Objekte sind fest; Aufheben ist nicht nötig. Die Karte zeigt die genaue Instanz; Bad- und Flurspiegel sind getrennte Ziele. Waschmaschinentür und Schublade öffnen sich, Öffnen allein erfüllt jedoch kein Benennungsziel. Wäscheständer/Klammer stehen in Schlafzimmer 2, übrige Wäschepflege- und Reinigungsobjekte im Bad.

| مرحله / Stage / Stufe | هدف و شناسه / Target and ID / Ziel und ID | فارسی | English | Deutsch |
|---|---|---|---|---|
| 104 | der Schlüssel<br>`IDENTIFY-Hall_Key` | «کلید» در راهرو را شناسایی کن. | Identify the key in the hallway. | Benenne den Schlüssel im Flur. |
| 105 | das Schlüsselbrett<br>`IDENTIFY-Hall_KeyRack` | «آویز کلید» در راهرو را شناسایی کن. | Identify the key rack in the hallway. | Benenne das Schlüsselbrett im Flur. |
| 106 | der Regenschirm<br>`IDENTIFY-Hall_Umbrella` | «چتر» در راهرو را شناسایی کن. | Identify the umbrella in the hallway. | Benenne den Regenschirm im Flur. |
| 107 | der Schirmständer<br>`IDENTIFY-Hall_UmbrellaStand` | «جاچتری» در راهرو را شناسایی کن. | Identify the umbrella stand in the hallway. | Benenne den Schirmständer im Flur. |
| 108 | die Wanduhr<br>`IDENTIFY-Hall_Clock` | «ساعت دیواری» در راهرو را شناسایی کن. | Identify the wall clock in the hallway. | Benenne die Wanduhr im Flur. |
| 109 | der Spiegel<br>`IDENTIFY-Bath_Mirror` | «آینه» در حمام را شناسایی کن. | Identify the mirror in the bathroom. | Benenne den Spiegel im Bad. |
| 110 | das Handtuch<br>`IDENTIFY-Bath_Towel` | «حوله» در حمام را شناسایی کن. | Identify the towel in the bathroom. | Benenne das Handtuch im Bad. |
| 111 | der Handtuchhalter<br>`IDENTIFY-Bath_TowelRail` | «آویز حوله» در حمام را شناسایی کن. | Identify the towel rail in the bathroom. | Benenne den Handtuchhalter im Bad. |
| 112 | die Seife<br>`IDENTIFY-Bath_Soap` | «صابون» در حمام را شناسایی کن. | Identify the soap in the bathroom. | Benenne die Seife im Bad. |
| 113 | die Toilettenbürste<br>`IDENTIFY-Bath_ToiletBrush` | «برس توالت» در حمام را شناسایی کن. | Identify the toilet brush in the bathroom. | Benenne die Toilettenbürste im Bad. |
| 114 | der Toilettenpapierhalter<br>`IDENTIFY-Bath_PaperHolder` | «نگهدارنده دستمال توالت» در حمام را شناسایی کن. | Identify the toilet roll holder in the bathroom. | Benenne den Toilettenpapierhalter im Bad. |
| 115 | die Waschmaschine<br>`IDENTIFY-Bath_Washer` | «ماشین لباس‌شویی» در حمام را شناسایی کن. | Identify the washing machine in the bathroom. | Benenne die Waschmaschine im Bad. |
| 116 | der Wäschekorb<br>`IDENTIFY-Bath_LaundryBasket` | «سبد لباس» در حمام را شناسایی کن. | Identify the laundry basket in the bathroom. | Benenne den Wäschekorb im Bad. |
| 117 | das Waschmittel<br>`IDENTIFY-Bath_Detergent` | «مایع یا پودر لباس‌شویی» در حمام را شناسایی کن. | Identify the laundry detergent in the bathroom. | Benenne das Waschmittel im Bad. |
| 118 | der Wäscheständer<br>`IDENTIFY-Bedroom2_DryingRack` | «بند رخت تاشو» در اتاق خواب ۲ را شناسایی کن. | Identify the drying rack in the bedroom 2. | Benenne den Wäscheständer im Schlafzimmer 2. |
| 119 | die Wäscheklammer<br>`IDENTIFY-Bedroom2_ClothesPeg` | «گیره لباس» در اتاق خواب ۲ را شناسایی کن. | Identify the clothes peg in the bedroom 2. | Benenne die Wäscheklammer im Schlafzimmer 2. |
| 120 | der Staubsauger<br>`IDENTIFY-Bath_Vacuum` | «جاروبرقی» در حمام را شناسایی کن. | Identify the vacuum cleaner in the bathroom. | Benenne den Staubsauger im Bad. |
| 121 | der Besen<br>`IDENTIFY-Bath_Broom` | «جارو» در حمام را شناسایی کن. | Identify the broom in the bathroom. | Benenne den Besen im Bad. |
| 122 | das Kehrblech<br>`IDENTIFY-Bath_Dustpan` | «خاک‌انداز» در حمام را شناسایی کن. | Identify the dustpan in the bathroom. | Benenne das Kehrblech im Bad. |
| 123 | der Wischmopp<br>`IDENTIFY-Bath_Mop` | «تی» در حمام را شناسایی کن. | Identify the mop in the bathroom. | Benenne den Wischmopp im Bad. |

## مراحل ۱۲۴ تا ۱۳۳ / Stages 124–133 / Stufen 124–133 — ده وسیلهٔ نهایی / Final ten furnishings / Letzte zehn Einrichtungsobjekte

**فارسی:** ده هدف آخر با لمس Anhören، F حتی هنگام نگاه به جزء هدف کامل می‌شوند. اشیا ثابت‌اند و نقشه نمونهٔ دقیق را نشان می‌دهد. کلید برق با دکمهٔ استفاده، سه چراغ پذیرایی را قطع و وصل می‌کند؛ تغییر وضعیت کلید به‌تنهایی هدف شناسایی را کامل نمی‌کند. ترموستات، توستر، قهوه‌ساز و ترازو در این نسخه برای شناسایی‌اند؛ گرمایش و کارکرد دستگاه‌ها شبیه‌سازی نشده‌اند.

**English:** Complete the last ten targets by Anhören or F while aiming at the whole or an owned part. All are fixed and the map locates the exact instance. Use toggles the switch's three living-room lights; toggling alone does not complete identification. Thermostat, toaster, coffee machine and scale are identification objects in this version; heating and appliance operation are not simulated.

**Deutsch:** Die letzten zehn Ziele über Anhören oder F beim Anvisieren des Objekts oder eines Teils erfüllen. Alle sind fest, die Karte zeigt die genaue Instanz. Benutzen schaltet die drei Wohnzimmerlampen; Schalten allein erfüllt kein Benennungsziel. Thermostat, Toaster, Kaffeemaschine und Waage dienen in dieser Version dem Benennen; Heizung und Gerätebetrieb werden nicht simuliert.

| مرحله / Stage / Stufe | هدف و شناسه / Target and ID / Ziel und ID | فارسی | English | Deutsch |
|---|---|---|---|---|
| 124 | die Mülltüte<br>`IDENTIFY-Bath_BinBag` | «کیسه زباله» در حمام را شناسایی کن. | Identify the bin bag in the bathroom. | Benenne die Mülltüte im Bad. |
| 125 | der Lichtschalter<br>`IDENTIFY-Hall_LightSwitch` | «کلید برق» در راهرو را شناسایی کن. | Identify the light switch in the hallway. | Benenne den Lichtschalter im Flur. |
| 126 | die Steckdose<br>`IDENTIFY-Hall_PowerSocket` | «پریز» در راهرو را شناسایی کن. | Identify the power socket in the hallway. | Benenne die Steckdose im Flur. |
| 127 | der Heizkörper<br>`IDENTIFY-Living_Radiator` | «رادیاتور» در پذیرایی را شناسایی کن. | Identify the radiator in the living room. | Benenne den Heizkörper im Wohnzimmer. |
| 128 | das Thermostat<br>`IDENTIFY-Living_Thermostat` | «ترموستات» در پذیرایی را شناسایی کن. | Identify the thermostat in the living room. | Benenne das Thermostat im Wohnzimmer. |
| 129 | die Küchenschere<br>`IDENTIFY-Kitchen_Scissors` | «قیچی آشپزخانه» در آشپزخانه را شناسایی کن. | Identify the kitchen scissors in the kitchen. | Benenne die Küchenschere in der Küche. |
| 130 | die Küchenrolle<br>`IDENTIFY-Kitchen_PaperRoll` | «دستمال کاغذی رولی آشپزخانه» در آشپزخانه را شناسایی کن. | Identify the kitchen roll in the kitchen. | Benenne die Küchenrolle in der Küche. |
| 131 | der Toaster<br>`IDENTIFY-Kitchen_Toaster` | «توستر» در آشپزخانه را شناسایی کن. | Identify the toaster in the kitchen. | Benenne den Toaster in der Küche. |
| 132 | die Kaffeemaschine<br>`IDENTIFY-Kitchen_CoffeeMachine` | «قهوه‌ساز» در آشپزخانه را شناسایی کن. | Identify the coffee machine in the kitchen. | Benenne die Kaffeemaschine in der Küche. |
| 133 | die Küchenwaage<br>`IDENTIFY-Kitchen_Scale` | «ترازوی آشپزخانه» در آشپزخانه را شناسایی کن. | Identify the kitchen scale in the kitchen. | Benenne die Küchenwaage in der Küche. |

## مراحل ۱۳۴ تا ۱۳۶ / Stages 134–136 / Stufen 134–136 — مقاله‌ها / Articles / Artikel

| مرحله / Stage / Stufe | شناسه / ID | فارسی | English | Deutsch |
|---|---|---|---|---|
| 134 | `SORT-DER` | میز را خالی کن؛ سپس ۱۰ وسیله با مقالهٔ der روی میز بگذار. | Clear the table, then place 10 objects with article der on it. | Räume den Tisch leer und lege danach 10 Gegenstände mit dem Artikel der darauf. |
| 135 | `SORT-DIE` | وسایل قبلی را از میز بردار؛ سپس ۱۰ وسیله با مقالهٔ die روی میز بگذار. | Remove the previous objects, then place 10 objects with article die on the table. | Entferne die bisherigen Gegenstände und lege danach 10 Gegenstände mit dem Artikel die auf den Tisch. |
| 136 | `SORT-DAS` | دوباره میز را خالی کن؛ سپس ۱۰ وسیله با مقالهٔ das روی میز بگذار. | Clear the table again, then place 10 objects with article das on it. | Räume den Tisch erneut leer und lege danach 10 Gegenstände mit dem Artikel das darauf. |

### قواعد مشترک / Shared rules / Gemeinsame Regeln

| فارسی | English | Deutsch |
|---|---|---|
| وضعیت: هر سه پیاده‌شده‌اند. مقدار هدف در کد حداقلِ ۱۰ و تعداد وسایل موجود آن مقاله است؛ اکنون برای هر سه برابر ۱۰ است. | Status: all three are implemented. The coded target is the smaller of 10 and available items of the article; all three currently require 10. | Status: alle drei umgesetzt. Das Ziel ist im Code das Minimum aus 10 und verfügbaren Gegenständen des Artikels; derzeit benötigen alle drei 10. |
| پیش از هر تمرین، حتی وسایل درستِ از قبل روی میز باید برداشته شوند. آخرین وسیلهٔ برداشته‌شده باید بیرون میز گذاشته شود تا آماده‌سازی تمام شود. | Before each task, even pre-existing matching items must be removed. Place the final cleared item away from the table to finish preparation. | Vor jeder Aufgabe müssen auch vorher vorhandene passende Gegenstände entfernt werden. Den letzten weggenommenen Gegenstand außerhalb des Tisches ablegen, um die Vorbereitung abzuschließen. |
| فقط وسیلهٔ دارای مقالهٔ درست، رهاشده و واقعاً داخل محدودهٔ سطح میز شمرده می‌شود. وسیلهٔ در دست شمرده نمی‌شود. | Count only matching, unheld objects resting within the tabletop footprint. Held items do not count. | Nur passende, nicht getragene Gegenstände innerhalb der Tischfläche zählen. Getragene Gegenstände zählen nicht. |
| نام هر وسیلهٔ گذاشته‌شده و شمار درست‌ها اعلام می‌شود. وسیلهٔ اشتباه با نام برای برداشتن معرفی می‌شود و مانع تکمیل است. | Each placement and the matching count are announced. Wrong items are named for removal and block completion. | Jeder abgelegte Gegenstand und die passende Anzahl werden angesagt. Falsche Gegenstände werden zum Entfernen benannt und verhindern den Abschluss. |
| برداشتن یک وسیله شمار را کم می‌کند؛ گذاشتن دوباره همان وسیله شمار اضافی ایجاد نمی‌کند. | Removing an item reduces the count; replacing it adds no duplicate credit. | Wegnehmen senkt die Anzahl; erneutes Ablegen desselben Gegenstands zählt nicht doppelt. |
| مینی‌مپ: وسیلهٔ لازم برای خالی‌کردن، هدف مناسب برای برداشتن، میز برای وسیلهٔ درست در دست، یا محل پیشنهادی بیرون میز برای گذاشتن وسیلهٔ نامناسب را نشان می‌دهد. | Minimap: points to cleanup items, a suitable pickup, the table for a matching held item, or a suggested off-table drop location for an unsuitable item. | Minikarte: zeigt aufzuräumende Gegenstände, ein passendes Suchziel, den Tisch für einen passenden getragenen Gegenstand oder eine vorgeschlagene Ablagestelle außerhalb des Tisches für unpassende Gegenstände. |
| شرط تکمیل: رسیدن به شمار هدف و نبود هیچ وسیلهٔ دارای مقالهٔ اشتباه روی میز؛ سپس تمرین بعدی با خالی‌کردن دوباره شروع می‌شود. | Completion: reach the required count with no wrong-article item remaining on the table; the next task starts with cleanup again. | Abschluss: Zielanzahl erreichen, ohne Gegenstand mit falschem Artikel auf dem Tisch; die nächste Aufgabe beginnt erneut mit Aufräumen. |

## پایان فعلی / Current ending / Aktuelles Ende

**فارسی:** پس از وظیفهٔ ۱۳۶ پایان اعلام می‌شود و تمرین آزاد ادامه دارد؛ هدف زرد باقی نمی‌ماند. ذخیرهٔ قدیمیِ تمرین مقاله‌ها یا پایان، با جابه‌جایی شماره به همان وضعیت منتقل می‌شود؛ برای اجرای همهٔ وظیفه‌های جدید از ابتدا، از شروع دوباره استفاده شود.

**English:** After task 136, completion is announced and free practice remains; no stale yellow target remains. Existing article/completion saves migrate to the equivalent phase with shifted numbering; restart to run the full expanded sequence from the beginning.

**Deutsch:** Nach Aufgabe 136 wird der Abschluss angesagt, freies Üben bleibt möglich und kein altes gelbes Ziel bleibt. Bisherige Artikel-/Abschlussstände werden bei verschobenen Nummern phasengleich übernommen; mit Neustart beginnt die vollständige erweiterte Folge.

## تغییرات بعدی / Future changes / Künftige Änderungen

**فارسی:** صف ۸۰ وسیلهٔ خانه اکنون کامل است؛ برای محتوای جدیدِ بعدی، تنها وظیفهٔ واقعاً ساخته‌شده ثبت شود. با اضافه شدن محتوای واقعی، هدف‌ها و شرط تکمیل همین فایل به‌روزرسانی شوند. شناسه‌های SEARCH بر پایهٔ نام GameObject برای نگهداری سندند؛ هنوز سیستم مستقل شناسهٔ مرحله در بازی نیستند. اگر هدف‌های جدید پیش از تمرین مقاله‌ها وارد شوند، شماره‌های نمایش و این جدول مطابق ترتیب واقعی کد بازشماری شوند؛ شناسهٔ SORT و سوابق تغییر حفظ شوند. تغییر رابط یا صدا به‌تنهایی مرحلهٔ تازه ایجاد نمی‌کند.

**English:** The eighty-instance furnishing queue is complete; register only actually built objectives for any subsequently requested content. When real content is added, update objectives and completion rules here. SEARCH IDs use GameObject names for document maintenance; they are not a separate runtime task-ID system. If new search objectives precede article tasks, renumber displayed stages and this table according to actual code order; preserve SORT identifiers and change history. A UI or voice change alone creates no new stage.

**Deutsch:** Der Plan mit achtzig Einrichtungsinstanzen ist abgeschlossen; für künftig beauftragte Inhalte nur tatsächlich gebaute Ziele eintragen. Bei tatsächlichen Ergänzungen Ziele und Abschlussregeln hier aktualisieren. SEARCH-IDs verwenden GameObject-Namen zur Dokumentpflege; sie sind kein eigenständiges Laufzeit-Aufgaben-ID-System. Werden neue Suchziele vor Artikelaufgaben eingefügt, Anzeige und Tabelle nach der tatsächlichen Codereihenfolge neu nummerieren; SORT-Kennungen und Änderungshistorie erhalten. Eine UI- oder Stimmänderung allein erzeugt keine neue Stufe.

## Änderungsverlauf / Change history / تغییرات

| تاریخ / Date / Datum | فارسی | English | Deutsch |
|---|---|---|---|
| 2026-11-20 | ایجاد مرجع مراحل ۰ تا ۵۶؛ ترتیب ۵۳ وسیله از SampleScene و LearningProgress استخراج و با نام‌های سه‌زبانه تطبیق داده شد. | Created the stage 0–56 reference; extracted the order of 53 items from SampleScene and LearningProgress and matched trilingual names. | Referenz für Stufen 0–56 erstellt; Reihenfolge der 53 Gegenstände aus SampleScene und LearningProgress ermittelt und mit dreisprachigen Namen abgeglichen. |


## سابقهٔ اصلاح بصری و صف اولیه / Historical visual pass and initial queue / Frühere Überarbeitung und ursprüngliche Planung

**فارسی:** اصلاح گرافیکِ وسایل فعلی و معماری، مرحلهٔ جدید ایجاد نمی‌کند؛ این سند اکنون مراحل ۰ تا ۱۳۶ را دارد. بخش ۱۴ tools.txt، هشتاد نمونهٔ وسیله را در گروه‌های F01 تا F16 برای ساخت بعدی ثبت می‌کند. ردیف‌های [ ] هنوز اضافه نشده‌اند. پس از ساخت هر گروه طبق درخواست بعدی، جای شیء، هدف آموزشی، صدا و شرط تکمیل آن تعیین و فقط مرحلهٔ واقعاً پیاده‌شده اینجا ثبت می‌شود. شناسهٔ گروه‌های ساخت با شمارهٔ مرحلهٔ داخل بازی یکی نیست.

**English:** Polishing existing props and architecture creates no new stage; this document now covers stages 0–136. Section 14 of tools.txt lists 80 item instances in groups F01–F16 for later construction. [ ] rows are not added. After a future requested batch is built, define its placement, learning objective, voice and completion rule and register only actually implemented stages here. Construction-batch IDs differ from in-game stage numbers.

**Deutsch:** Die optische Überarbeitung vorhandener Gegenstände und Architektur erzeugt keine neue Stufe; diese Datei umfasst jetzt Stufen 0–136. Abschnitt 14 von tools.txt listet 80 Instanzen in Gruppen F01–F16 für spätere Umsetzung. [ ]-Zeilen sind noch nicht hinzugefügt. Nach einer künftig beauftragten Gruppe werden Standort, Lernziel, Stimme und Abschlussregel festgelegt und nur tatsächlich umgesetzte Aufgaben hier eingetragen. Baugruppen-IDs unterscheiden sich von Spielstufennummern.

| تاریخ / Date / Datum | فارسی | English | Deutsch |
|---|---|---|---|
| 2026-11-20 | بهبود بصری با حفظ ترتیب و شرط تکمیل؛ ۸۰ وسیله فقط در صف برنامه‌ریزی قرار گرفتند | Visual upgrade preserved order/completion rules; 80 items were added only to the planning queue | Optische Verbesserung bei unveränderter Reihenfolge/Abschlussregel; 80 Gegenstände nur in die Planung aufgenommen |


## وضعیت فعلی صف وسایل / Current furnishing queue / Aktueller Einrichtungsplan

**فارسی:** هر ۸۰ مورد گروه‌های F01–F16 ساخته و علامت [ ] تمام ردیف‌های وسایل حذف شده است؛ موردی در این صف باقی نمانده است. شناسایی هود و اجزای میز، کابینت، پنجره و حمام در همهٔ وظیفه‌ها قابل استفاده است؛ برای هر جزء، وظیفهٔ اضافی اختراع نشده است.

**English:** All eighty F01–F16 instances are built and their [ ] prefixes removed; none remain pending in this queue. Hood and table/cabinet/window/bathroom part identification works throughout the tasks; no extra task was invented for every part.

**Deutsch:** Alle achtzig F01–F16-Instanzen sind gebaut und ihre [ ]-Präfixe entfernt; in diesem Plan bleibt keine offen. Benennung von Haube und Tisch-/Schrank-/Fenster-/Badteilen funktioniert während aller Aufgaben; nicht für jedes Teil wurde eine weitere Aufgabe erfunden.

| تاریخ / Date / Datum | فارسی | English | Deutsch |
|---|---|---|---|
| 2026-11-20 | افزودن وظیفه‌های شناسایی ۵۴–۶۳؛ جابه‌جایی SORT-DER/DIE/DAS به ۶۴–۶۶ و حفظ ترتیب ۵۳ هدف قبلی | Added identification tasks 54–63; moved SORT-DER/DIE/DAS to 64–66 and preserved the original 53 pickup targets | Benennungsaufgaben 54–63 ergänzt; SORT-DER/DIE/DAS auf 64–66 verschoben, bisherige 53 Aufhebeziele beibehalten |


| تاریخ / Date / Datum | فارسی | English | Deutsch |
|---|---|---|---|
| 2026-11-20 | افزودن ۲۰ هدف مستقل اتاق خواب در ۶۴–۸۳؛ مقاله‌ها به ۸۴–۸۶ رفتند، ۶۳ هدف قبلی و ذخیرهٔ معادل حفظ شد | Added 20 independent bedroom targets at 64–83; articles moved to 84–86, preserving the first 63 targets and equivalent saves | 20 unabhängige Schlafzimmerziele 64–83 ergänzt; Artikel auf 84–86 verschoben, bisherige 63 Ziele und entsprechende Speicherstände erhalten |

| 2026-11-20 | ساخت F05–F08 و افزودن شناسایی ۸۴–۱۰۳؛ مقاله‌ها ۱۰۴–۱۰۶، با حفظ ۸۳ هدف قبلی و ذخیرهٔ معادل | Built F05–F08 and added identification 84–103; articles 104–106, preserving the previous 83 targets and equivalent saves | F05–F08 gebaut und Benennung 84–103 ergänzt; Artikel 104–106, bisherige 83 Ziele und entsprechende Speicherstände erhalten |

| 2026-11-20 | افزودن ۲۰ هدف لوازم خانه ۱۰۴–۱۲۳؛ مقاله‌ها ۱۲۴–۱۲۶؛ حفظ ۱۰۳ هدف قبلی و ذخیره‌های معادل؛ ۷۰ ساخته و ۱۰ در انتظار | Added twenty house-accessory targets 104–123; articles 124–126; preserved previous 103 targets and equivalent saves; 70 built/10 pending | Zwanzig Haushaltszubehörziele 104–123 ergänzt; Artikel 124–126; bisherige 103 Ziele und entsprechende Speicherstände erhalten; 70 gebaut/10 offen |

| 2026-11-20 | ساخت ده مورد آخر F14–F16 و وظیفه‌های ۱۲۴–۱۳۳؛ مقاله‌ها ۱۳۴–۱۳۶؛ حفظ ۱۲۳ هدف قبلی و ذخیرهٔ معادل؛ تکمیل هر ۸۰ مورد | Built final ten F14–F16 instances and tasks 124–133; articles 134–136; preserved previous 123 targets and equivalent saves; all eighty instances complete | Letzte zehn F14–F16-Instanzen und Aufgaben 124–133 gebaut; Artikel 134–136; bisherige 123 Ziele und entsprechende Speicherstände erhalten; alle achtzig Instanzen fertig |

| تاریخ / Date / Datum | فارسی | English | Deutsch |
|---|---|---|---|
| 2026-11-20 | متن محوشونده، نقشه و مسیر، منوی توقف/موسیقی و شناسایی دستی؛ مرحلهٔ تازه اضافه نشد | Fading captions, map/routes, pause/music menu and explicit identification; no new stages | Verblassende Hinweise, Karte/Wege, Pause-/Musikmenü und ausdrückliche Benennung; keine neuen Aufgaben |


### اصلاح Anhören / Anhören correction / Anhören-Korrektur — 2026-11-20

**فارسی — پیاده‌شده در کد:** شنیدن نام شیء یا جزء با Anhören یا F، زمان نمایش متن مرحله را دوباره فعال نمی‌کند. فقط کارت نام نمایش داده می‌شود و پس از پایان نام‌گویی، متن مرحله به‌عنوان جایگزین برنمی‌گردد. تکرار مرحله و پیشرفت واقعیِ هدف ثابت همچنان راهنمای مرحله را فعال می‌کنند. شرط تکمیل و ترتیب ۵۳ هدف قابل حمل، ۸۰ هدف ثابت و سه تمرین مقاله (۱۳۶ وظیفه، قاشق اول) تغییر نکرده‌اند. کامپایل مستقل بررسی شد؛ آزمون دیداری Unity و گوشی هنوز تأیید نشده است.

**English — implemented in code:** Hearing an object/part name with Anhören or F does not restart the stage-caption timer. Only the name card appears, and the stage text does not return as a fallback after naming finishes. Task repetition and actual fixed-target progression still activate stage guidance. Completion rules and the sequence of 53 portable targets, 80 fixed targets and three article tasks (136 tasks, spoon first) are unchanged. Standalone compilation checked; Unity visual and phone testing remain unverified.

**Deutsch — im Code umgesetzt:** Objekt-/Teilnamen über Anhören oder F starten die Aufgabenanzeige nicht erneut. Nur die Namenskarte erscheint; nach der Benennung kehrt der Aufgabentext nicht als Ersatz zurück. Aufgabenwiederholung und tatsächlicher Fortschritt bei festen Zielen aktivieren weiterhin Aufgabenhinweise. Abschlussregeln und Reihenfolge von 53 tragbaren, 80 festen Zielen und drei Artikelaufgaben (136 Aufgaben, Löffel zuerst) bleiben unverändert. Separate Kompilierung geprüft; Sichtprüfung in Unity und Smartphone-Tests bleiben unbestätigt.

| تاریخ / Date / Datum | فارسی | English | Deutsch |
|---|---|---|---|
| 2026-11-20 | رفع بازگشت متن مرحله پس از شنیدن نام؛ بدون مرحلهٔ جدید | Fixed stage text returning after object naming; no new stages | Rückkehr des Aufgabentexts nach Benennung behoben; keine neuen Aufgaben |


### خوانایی وسایل کوچک / Small-object readability / Erkennbarkeit kleiner Gegenstände — 2026-11-20

**فارسی — پیاده‌شده:** ظاهر پنج هدف موجود بازسازی شد: قاشق با کاسهٔ گود و لبهٔ واقعی، چنگال با چهار دندانهٔ جدا و خمیده، چاقو با تیغه و دستهٔ تیره و پرچ، سیب با بدنهٔ لوب‌دار و فرورفتگی بالا و ساقه و برگ، و گوجه با بدنهٔ تخت‌تر و پنج کاسبرگ. فلز روشن‌تر با وابستگی کمتر به بازتاب و پوست میوهٔ بافت‌دار، تشخیص را آسان‌تر می‌کنند. شناسه، جای اولیه، برخوردگر و صاحب تعامل وسایل حفظ شده‌اند؛ این کار وسیله یا مرحلهٔ تازه نمی‌افزاید. هدف‌های ۱، ۳، ۸، ۱۸ و ۵۱ همچنان با برداشتن کامل می‌شوند. ترتیب قاشق اول، ۵۳ هدف قابل حمل، ۸۰ هدف ثابت، سه تمرین مقاله و ذخیره‌ها تغییر نکرده‌اند. ابزار Apply Visual Polish این مدل‌ها را دوباره اعمال می‌کند.

**English — implemented:** Five existing targets have rebuilt visuals: a spoon with a concave bowl and rim, a fork with four separated curved tines, a knife with blade/dark riveted grip, a lobed apple with top depression/stem/leaf, and a flatter tomato with five sepals. Lighter metal with less dependence on reflections and textured fruit skins improve recognition. IDs, initial poses, colliders and interaction owners are preserved; no object or stage was added. Targets 1, 3, 8, 18 and 51 still require pickup. Spoon-first order, 53 portable targets, 80 fixed targets, three article tasks and saves are unchanged. Apply Visual Polish reapplies these models.

**Deutsch — umgesetzt:** Fünf vorhandene Ziele erhielten neue Modelle: Löffel mit konkaver Mulde und Rand, Gabel mit vier getrennten gebogenen Zinken, Messer mit Klinge/dunklem vernietetem Griff, gelappter Apfel mit oberer Vertiefung/Stiel/Blatt und flachere Tomate mit fünf Kelchblättern. Helleres, weniger spiegelungsabhängiges Metall und strukturierte Fruchtschalen erleichtern das Erkennen. Kennungen, Ausgangspositionen, Collider und Interaktionsbesitzer bleiben erhalten; kein Gegenstand und keine Aufgabe kam hinzu. Ziele 1, 3, 8, 18 und 51 erfordern weiterhin Aufheben. Löffel zuerst, 53 tragbare Ziele, 80 feste Ziele, drei Artikelaufgaben und Speicherstände bleiben unverändert. Apply Visual Polish wendet diese Modelle erneut an.

**بررسی / Verification / Prüfung:** Unity geometry/normal/collider checks and 53/80 population validation passed in an isolated project. Before/after desktop renders are in [VisualReview](VisualReview/README.md). Real-phone readability and performance remain unverified. / بررسی هندسه، جهت سطح و برخوردگر و شمار ۵۳/۸۰ در کپی جدا موفق بود؛ تصاویر قبل/بعد ثبت شدند؛ خوانایی و عملکرد روی گوشی هنوز تأیید نشده‌اند. / Geometrie-, Normalen-, Collider- und 53/80-Zielprüfung im separaten Projekt bestanden; Vorher-/Nachher-Bilder erfasst; Erkennbarkeit und Leistung auf echten Smartphones bleiben ungeprüft.

| تاریخ / Date / Datum | فارسی | English | Deutsch |
|---|---|---|---|
| 2026-11-20 | بازسازی ظاهر قاشق، چنگال، چاقو، سیب و گوجه؛ هدف تازه ندارد | Refined spoon, fork, knife, apple and tomato visuals; no new objectives | Löffel, Gabel, Messer, Apfel und Tomate überarbeitet; keine neuen Ziele |

| تاریخ / Date / Datum | فارسی | English | Deutsch |
|---|---|---|---|
| 2026-11-20 | دایره‌های یگانهٔ شنیدن/عمل؛ نام کل با Anhören و جزء با Detail؛ بدون مرحلهٔ جدید | Single hearing/action circles; whole via Anhören, part via Detail; no new stages | Einzelne Anhören-/Aktionskreise; Gesamtobjekt über Anhören, Teil über Detail; keine neuen Aufgaben |


### اندازهٔ شمارهٔ وظیفه / Task-number text size / Schriftgröße der Aufgabennummer — 2026-11-20

**فارسی — پیاده‌شده در کد:** شمارهٔ وظیفه در کادر بالای صفحه با قلم کوچک‌ترِ ۱۶ و بدون شکستن سطر نمایش داده می‌شود؛ اگر متن از عرض داخلی بیشتر شود، قلم تا ۱۰ کوچک می‌شود. شماره‌های دو و سه‌رقمی مانند Aufgabe 10 / 136 و Aufgabe 136 / 136 در همان کادر جا می‌گیرند. این اصلاح فقط نمایش است؛ ۵۳ هدف برداشتن، ۸۰ هدف ثابت، سه تمرین مقاله، قاشق اول و ذخیره‌ها تغییر ندارند. آزمون دیداری روی گوشی هنوز تأیید نشده است.

**English — implemented in code:** The top task-number card uses a smaller 16-point single-line style; text that exceeds its padded width shrinks down to 10 points. Two-/three-digit labels such as Aufgabe 10 / 136 and Aufgabe 136 / 136 fit the same card. This is display-only: 53 pickup targets, 80 fixed targets, three article tasks, spoon-first order and saves are unchanged. Phone visual verification remains pending.

**Deutsch — im Code umgesetzt:** Die Aufgabennummer oben nutzt eine kleinere einzeilige Schrift mit Größe 16; bei Überschreiten der Innenbreite schrumpft sie bis Größe 10. Zwei-/dreistellige Angaben wie Aufgabe 10 / 136 und Aufgabe 136 / 136 passen in dieselbe Karte. Nur die Darstellung ändert sich: 53 Aufhebeziele, 80 feste Ziele, drei Artikelaufgaben, Löffel zuerst und Speicherstände bleiben erhalten. Sichtprüfung auf Smartphones steht noch aus.

| تاریخ / Date / Datum | فارسی | English | Deutsch |
|---|---|---|---|
| 2026-11-20 | کوچک‌کردن و تطبیق شمارهٔ وظیفه با عرض کادر؛ مرحلهٔ تازه ندارد | Smaller task-number text fitted to card width; no new stages | Kleinere, an Kartenbreite angepasste Aufgabennummer; keine neuen Aufgaben |
