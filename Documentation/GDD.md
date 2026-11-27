# DeutschRoom — GDD

[فارسی](#فارسی) · [English](#english) · [Deutsch](#deutsch)

## فارسی

آخرین به‌روزرسانی: **2026-11-27 — Europe/Berlin**

| سند | نقش |
| --- | --- |
| **GDD** | طراحی جاری |
| [GameStages](GameStages.md) | ترتیب و تکمیل وظایف |
| [Changelog](CHANGELOG.md) | تاریخچه و نتایج بررسی‌های گذشته |

### 1. خلاصه و چشم‌انداز

DeutschRoom بازی آموزشی سه‌بعدی اول‌شخص در یک خانه است. بازیکن با پیدا کردن و نام‌شنیدن اشیا، جابه‌جایی وسایل، انتخاب پاسخ و اجرای موقعیت‌های خانگی، واژگان و دستور زبان آلمانی را تمرین می‌کند. اصول طراحی: یادگیری در بافت واقعی، ارتباط روشن دستور و عمل، بازخورد قابل اصلاح و کمک اختیاری.

### 2. مخاطب و دامنه

مخاطب، زبان‌آموز آلمانی است؛ گروه سنی مشخص نشده است. محتوا تقسیم داخلی A1.1–C1.2 دارد، اما مدرک سطح یا نتیجهٔ رسمی نیست. کنترل لمس برای گوشی و ماوس/صفحه‌کلید برای رایانه وجود دارد؛ انتشار و کیفیت دستگاه واقعی هنوز تأیید نشده‌اند. امتیاز پولی، جان و پرداخت درون‌بازی پیاده نشده‌اند.

### 3. چرخهٔ بازی

ورود یا ادامه ← خواندن/شنیدن دستور ← جست‌وجوی شیء یا بررسی پرونده ← عمل لازم و پاسخ در صورت درخواست ← بازخورد و اصلاح ← ثبت پیشرفت و هدف بعدی. نقشه در مرور تا درخواست کمک هدف را آشکار نمی‌کند. آموزش کنترل جدا از درس است و پیشرفت درسی ایجاد نمی‌کند. پس از پایان، تمرین آزاد ممکن است.

### 4. محتوا و پیشرفت

| شاخص | وضعیت جاری |
| --- | --- |
| وظایف | **428**; P: 106 · I: 92 · G: 90 · C: 140 |
| جمعیت صحنه | **53** قابل حمل + **79** ثابت |
| شروع | CUR-001 · Loeffel_Spoon · der Löffel |
| پایان | CUR-430 · **428** |
| بازنشسته | CUR-102 · CUR-191; Hall_Mirror؛ ترتیب ثابت **48** خالی است |
| پرونده‌ها | E · M · W · H؛ **16** صفحه |

P برداشتن واقعی، I شناسایی هدف، G دستور زبان و C موقعیت ترکیبی است. ۵۳ معرفی قابل حمل سپس ۷۹ معرفی ثابت می‌آیند؛ ادامه شامل مرور، دستور و سناریو است. ترتیب دقیق در GameStages و IntroA11.json است؛ شمارهٔ نمایشی با شناسهٔ CUR یکی نیست. قاشق با جابه‌جایی اولین هدف مرتب‌شده بر اساس LessonOrder در ابتدا می‌ماند. ذخیره بر اساس شناسه است، نه تعداد قدیمی؛ مسیر قدیمی حذف شده است.

### 5. مکانیک و کنترل

#### 5.1 حرکت و تعامل

| ID | وضعیت | شرح |
| --- | --- | --- |
| GDD-03 | Done | توقف، حرکت و پویانمایی محیط را متوقف می‌کند؛ ادامه، بازی را برمی‌گرداند. |
| GDD-04 | Done | کنترل گوشی: جوی‌استیک حرکت، کشیدن برای نگاه و دکمه‌های وابسته به عمل قابل انجام. |
| GDD-05 | Done | رایانه: WASD، نگاه ماوس، E تعامل، F نام کل، Shift+F جزء، G گذاشتن، R چرخش، Escape توقف و H آشکارکردن هدف در مرور. |
| GDD-06 | Done | پایین کشیدن و ۰٫۶ ثانیه نگه‌داشتن: نشستن؛ تکرار پس از رها کردن: درازکشیدن. حرکت رو به بالا، یک حالت را برمی‌گرداند. C/X معادل رایانه‌اند. |
| GDD-07 | Done | دوربین و کپسول برخورد با وضعیت بدن تغییر می‌کنند؛ زیر مانع بلند شدن مجاز نیست. |
| GDD-08 | Done | دکمهٔ دایره‌ای یگانهٔ شناسایی برای شیء نزدیک و روبه‌رو، نامش را با Anhören طبق حالت نمایش، نشان می‌دهد یا می‌خواند. |
| GDD-09 | Done | برداشتن یک وسیله در هر لحظه، چرخش ۴۵ درجه و گذاشتن روی سطح رو به بالا در دسترس. |
| GDD-10 | Done | درهای کابینت‌ها، کشو، وسایل بزرگ و اتاق‌ها تعاملی‌اند؛ شیر آب جریان نمایشی دارد. |
| GDD-11 | Done | آشپزخانه ۵۳ وسیلهٔ قابل حمل دارد؛ ظروف، ابزار و خوراکی‌ها در کابینت، کشو، زیر سینک، یخچال و فریزر قرار دارند. |
| GDD-12 | Done | صندلی‌ها، میز، طبقه‌ها و اجزای سینک مانند خروجی، سیفون، لوله و شلنگ‌ها نام دارند؛ اجزای ثابت قابل حمل نیستند. |
| GDD-25 | Done | وسیلهٔ داخل کشو همراه آن حرکت می‌کند؛ پس از گذاشتن روی سطح ثابت از کشو جدا می‌شود و پس از بازگرداندن به کشو دوباره همراهش حرکت می‌کند. |
| GDD-31 | Done | هود و وسایل مهم موجود نام دارند. اجزای قابل تشخیص مانند پایه و صفحهٔ میز، در و دستگیرهٔ کابینت، قاب پنجره و اجزای حمام نام و صدا دارند؛ Anhören فقط نام صاحب و Detail فقط نام جزء را ارائه می‌کند؛ عمل اصلی صاحب حفظ می‌شود. |

#### 5.2 تکمیل و راهنما

| ID | وضعیت | شرح |
| --- | --- | --- |
| GDD-15 | Done | مسیر یگانه ۴۲۸ وظیفه دارد: ۱۰۶ P، ۹۲ I، ۹۰ G، ۱۴۰ C؛ CUR-102 و CUR-191 بازنشسته‌اند. |
| GDD-16 | Done | عمل لازم و پاسخ درست شرط تکمیل تمرین ترکیبی است؛ پایان CUR-430 در شمارهٔ نمایشی ۴۲۸ است. |
| GDD-19 | Done | نقشه هدف زنده و مسیر را نشان می‌دهد؛ در مرور هدف/مسیر تا «Ziel zeigen» یا H مخفی‌اند و با مرحلهٔ بعد دوباره مخفی می‌شوند. |
| GDD-24 | Done | پیشرفت با شناسهٔ CUR و وضعیت اشیای قابل حمل محلی ذخیره می‌شود؛ بازنشانی پس از تأیید؛ ذخیرهٔ ابری نیست. |

### 6. جهان و اشیا

| ID | وضعیت | شرح |
| --- | --- | --- |
| GDD-01 | Done | یادگیری واژه‌های آلمانی با حرف تعریف در خانهٔ سه‌بعدی و دید اول‌شخص. |
| GDD-13 | Done | بیرون پنجرهٔ آشپزخانه باغی تزئینی با درخت، چمن، بوته، گل، مسیر، سنگ، نیمکت و تپه دیده می‌شود. |
| GDD-14 | Done | اتاق‌خواب‌ها، پذیرایی، ورودی و حمام مبله‌اند؛ موجودی جاری ۷۹ هدف ثابت است و آینهٔ راهرو حذف شده است. |
| GDD-26 | Done | هر ۸۰ نمونهٔ برنامه ساخته شد؛ پس از حذف آینهٔ راهرو ۷۹ هدف ثابت باقی است؛ سفارش تازه لازم است. |
| GDD-30 | Done | صف ساخت F01–F16 تکمیل است؛ این سابقه به معنی وجود فعلی آینهٔ راهرو نیست. |
| GDD-34 | Done | ۲۰ نمونهٔ F01–F04 شامل تخت و وسایل خواب، کمد، دراور، پاتختی، چراغ، چوب‌لباسی، میز تحریر، صندلی کار، قفسه و کتاب ساخته شدند؛ هر نمونه هدف مستقلِ شناسایی دارد. هشت در/کشو باز می‌شوند و اجزای مهم نام و صدا دارند. |
| GDD-36 | Done | ۱۹ هدف ثابت F05–F08 پس از حذف آینهٔ راهرو باقی است؛ ترتیب 48 خالی می‌ماند. |
| GDD-38 | Done | ۲۰ مورد F09، F12، F13 و چهار ردیف اول F14 ساخته شدند؛ نام و صدای اجزا، هدف مستقل و دو عمل بازکردن لباس‌شویی دارند. |
| GDD-40 | Done | ده وسیلهٔ آخر F14–F16 نام، صدای آلمانی، هدف مستقل و نام اجزای مهم دارند؛ کلید برق سه چراغ پذیرایی را قطع و وصل می‌کند و وضعیتش با ورود دوباره روشن می‌شود. |

### 7. رابط و دسترس‌پذیری

| ID | وضعیت | شرح |
| --- | --- | --- |
| GDD-02 | Done | منوی ورود شامل شروع، ادامه، شروع دوباره، تنظیمات، انتخاب کنترل و آموزش انگلیسی How to play است؛ آموزش روی بازی فقط در اولین اجرا خودکار و سپس از منو باز می‌شود. |
| GDD-20 | Done | تنظیمات درس شامل فقط متن، فقط صدا و هر دو، بلندی گوینده، تست صدا و گزینهٔ «صدا نمی‌شنوم» است؛ حالت و بلندی ذخیره می‌شوند. |
| GDD-21 | Done | با صدای رسانهٔ صفر یا گزارش نشنیدن، هشدار نوشتاری و متن جایگزین فعال می‌شود؛ تشخیص خودکار کلید Silent آیفون پشتیبانی نمی‌شود. |

### 8. هنر و صدا

| ID | وضعیت | شرح |
| --- | --- | --- |
| GDD-22 | Done | صدای آلمانی Tanja با سرعت ۸٪ کمتر؛ ۱۸۸۱ فایل موجود در منابع. پخش گوشی نیازمند بررسی است. |
| GDD-23 | Done | موسیقی از Assets/Resources/Music/background.mp3 بارگذاری می‌شود؛ کلید قطع/وصل در منوی مشترک کنار نقشه دارد و هنگام گویندگی آرام‌تر می‌شود. فایل موسیقی را کاربر می‌گذارد. |
| GDD-29 | Done | اصلاح بصری سطوح چوب، گچ، سنگ، کاشی و فلز؛ لبه‌های نرم‌شده، مدل پالایش‌شدهٔ کاسه/بشقاب/لیوان و قاشق/چنگال/چاقو/سیب/گوجه، قرنیز و پوشش کاشی، نور و بازتاب محیط و رندر موبایل با وضوح اصلی. |

### 9. محدودیت‌ها و کار آینده

| ID | وضعیت | شرح |
| --- | --- | --- |
| GDD-27 | Verify | آزمون روی گوشی واقعی برای چندلمسی، اندازهٔ رابط، صدا، هدست، بازگشت به برنامه و هشدار بی‌صدا لازم است؛ آزمون Unity جای آن را نمی‌گیرد. |
| GDD-28 | Limit | گذاشتن آزاد فعلی، هم‌پوشانی وسایل و لبهٔ میز را رد نمی‌کند؛ پخت‌وپز، سرمایش و جمع شدن آب شبیه‌سازی نشده‌اند. |
| GDD-33 | Limit | تجهیزات حمام در این نسخه برای شناسایی‌اند؛ آب دوش، فلاش و تخلیه شبیه‌سازی نمی‌شوند. دو درِ کابینت روشویی باز می‌شوند؛ عملکرد شیر آشپزخانهٔ قبلی حفظ شده است. |
| GDD-35 | Limit | اشیای اتاق خواب ثابت‌اند؛ بالش، کتاب و چوب‌لباسی قابل حمل نیستند. چراغ پاتختی اتاق اول و چراغ مطالعهٔ اتاق دوم اکنون پس از آماده‌شدن سناریو کنترل زمان اجرا دارند؛ سایر چراغ‌های این گروه کلید مستقل ندارند. |
| GDD-37 | Limit | تلویزیون تصویر ثابت دارد؛ تعویض کانال نیست؛ کلید راهرو سه چراغ پذیرایی را کنترل می‌کند. |
| GDD-39 | Limit | لوازم این گروه ثابت‌اند؛ شست‌وشو، نظافت، چتر تاشو و ساعت زنده شبیه‌سازی نشده‌اند. |
| GDD-41 | Limit | این ده مورد ثابت‌اند؛ گرمایش، عملکرد پریز، برش، توست‌کردن، قهوه‌سازی و وزن‌کشی شبیه‌سازی نشده‌اند. |
| GDD-42 | Next | ۴۲۸ اقتباس گزینه‌ای اجرا می‌شوند؛ معیارهای اصلی تولید آزاد هنوز Next هستند. |
| GDD-43 | Done | متن، پاسخ و شرط عمل برای ۴۲۸ وظیفه و چهار پرونده در منابع بازی تعریف شده‌اند؛ تولید آزاد در GDD-42/GDD-44 و تست موتور در GDD-49 جداگانه باز است. |
| GDD-44 | Next | هدف دوره A1.1–C1.2 با مرور هر ده و سنجش هر بیست مرحله؛ .1/.2 تقسیم داخلی‌اند. C1 به متن چنددیدگاهی، استنباط، پاسخ باز و پرسش تازه نیاز دارد؛ تعداد کلیک مدرک سطح نیست. |
| GDD-45 | Verify | فلاش، هود، دوش، شیر روشویی، چراغ پاتختی و مطالعه با کنترل زمان اجرا، شمارندهٔ رویداد و چیدمان مستقل در کد پیاده شده‌اند. صدا/جریان، شبیه‌سازی آموزشی است؛ اجرای کامل موتور و دستگاه تأیید نشده است. شناسه و پیشرفت حفظ‌اند. |
| GDD-46 | Verify | ۴۲۸ ردیف، شناسه‌ها، اتصال به ۵۳ قابل حمل/۷۹ ثابت، چهار منبع و شمار صدا به‌صورت ایستا/مدیریت‌شده بررسی شده‌اند. هم‌ارزی سطح، کیفیت ارزیاب و تجربهٔ زبان‌آموز نیازمند بازبینی آموزشی و آزمون‌اند؛ نتیجهٔ رسمی C1 ثبت نشده است. |
| GDD-47 | Done | محتوای داخلی A1.1–C1.2؛ سنجش رسمی یا تولید آزاد C1 اجرا نشده است. |
| GDD-48 | Limit | دسترسی تولید فایل با Sonna تأیید نشد؛ پس از درخواست تازهٔ کاربر، صدای آلمانی دیگری انتخاب و Tanja نصب شد؛ این صدا Sonna نامیده نمی‌شود. |
| GDD-49 | Verify | کامپایل/آزمون مدیریت‌شده موفق است؛ اجرای جداگانهٔ تازه پیش از آزمون با کد ‎−9 و بدون لاگ Unity بسته شد. codesign امضای هر دو نصب Editor را نامعتبر گزارش کرد؛ تلاش‌های قبلی خطای Licensing Client/headless داشتند. هیچ نتیجهٔ کامل موتور/گوشی ثبت نشده است. |

آینهٔ حمام بازتاب زنده دارد؛ ظاهر نهایی و کارایی گوشی نیازمند آزمون است. شخصیت رویه‌ای اول‌شخص و حرکت دست پیاده شده‌اند. ترجمهٔ دستور از i یا I باز می‌شود و پاسخ‌ها را آشکار یا وظیفه را کامل نمی‌کند. راهنمای انگلیسی از منو قابل دسترسی است.

### 10. فناوری و داده

Unity 6000.6.7f1 با URP؛ صحنهٔ اصلی SampleScene است. LearningProgress منطق پیشرفت و ذخیرهٔ محلی، IntroA11.json محتوای اجرا و کاتالوگ‌های اتاق‌ها موجودی ساخت را تعریف می‌کنند. پنج ریشهٔ مبلمان باید حفظ شوند. RAM/FPS واقعی، انتشار موبایل و ارزیابی رسمی زبان تأیید نشده‌اند.

### 11. پذیرش و اعتبارسنجی

هدف، نام، صوت و نشان نقشه باید به نمونهٔ درست اشاره کنند. P نیازمند برداشتن تازه است؛ I فقط با شناسایی اصلی هدف یا جزء متعلق به آن کامل می‌شود. پاسخ اشتباه و عمل نامرتبط نباید پیشرفت ایجاد کنند. ترجمه و آموزش هدف را تکمیل نمی‌کنند. سناریوها مستقل و موقت‌اند؛ شرط عمل و پاسخ باید با دادهٔ همان وظیفه همسان باشند. ذخیرهٔ شناسه‌ای و جای وسایل باید پس از ادامه حفظ شوند. بررسی ایستا/مدیریت‌شده با تأیید Unity و گوشی و اعتبار آموزشی جداست؛ تاریخ نتایج قبلی در Changelog است.

### 12. وضعیت‌ها

| کد | شرح |
| --- | --- |
| Done | پیاده‌شده؛ وضعیت کد و صحنه، نه تأیید انتشار |
| Next | درخواست آینده؛ هنوز انجام نشده |
| Verify | بررسی لازم، هنوز تأیید کامل نشده |
| Limit | محدودیت فعلی یا قابلیت پشتیبانی‌نشده |
| Retired | حذف‌شده از بازی؛ فقط سابقه |
| History | شناسهٔ گزارش تاریخی، نه الزام جاری |

### 13. ردیابی شناسه‌های تاریخی

| ID | وضعیت | شرح |
| --- | --- | --- |
| GDD-17 | Retired | قاعدهٔ مسیر قدیمی حذف‌شده؛ فقط سابقه در Changelog، نه هدف جاری. |
| GDD-18 | Retired | قاعدهٔ مسیر قدیمی حذف‌شده؛ فقط سابقه در Changelog، نه هدف جاری. |
| GDD-32 | Retired | قاعدهٔ مسیر قدیمی حذف‌شده؛ فقط سابقه در Changelog، نه هدف جاری. |
| GDD-50 | History | شناسهٔ سابقه؛ متن و معانی پیشین در Changelog حفظ‌اند؛ مشخصات جاری در بخش‌های این سند است. |
| GDD-51 | History | شناسهٔ سابقه؛ متن و معانی پیشین در Changelog حفظ‌اند؛ مشخصات جاری در بخش‌های این سند است. |
| GDD-52 | History | شناسهٔ سابقه؛ متن و معانی پیشین در Changelog حفظ‌اند؛ مشخصات جاری در بخش‌های این سند است. |
| GDD-53 | History | شناسهٔ سابقه؛ متن و معانی پیشین در Changelog حفظ‌اند؛ مشخصات جاری در بخش‌های این سند است. |
| GDD-54 | History | شناسهٔ سابقه؛ متن و معانی پیشین در Changelog حفظ‌اند؛ مشخصات جاری در بخش‌های این سند است. |
| GDD-55 | History | شناسهٔ سابقه؛ متن و معانی پیشین در Changelog حفظ‌اند؛ مشخصات جاری در بخش‌های این سند است. |
| GDD-56 | History | شناسهٔ سابقه؛ متن و معانی پیشین در Changelog حفظ‌اند؛ مشخصات جاری در بخش‌های این سند است. |
| GDD-57 | History | شناسهٔ سابقه؛ متن و معانی پیشین در Changelog حفظ‌اند؛ مشخصات جاری در بخش‌های این سند است. |
| GDD-58 | History | شناسهٔ سابقه؛ متن و معانی پیشین در Changelog حفظ‌اند؛ مشخصات جاری در بخش‌های این سند است. |
| GDD-59 | History | شناسهٔ سابقه؛ متن و معانی پیشین در Changelog حفظ‌اند؛ مشخصات جاری در بخش‌های این سند است. |
| GDD-60 | History | شناسهٔ سابقه؛ متن و معانی پیشین در Changelog حفظ‌اند؛ مشخصات جاری در بخش‌های این سند است. |
| GDD-61 | History | شناسهٔ سابقه؛ متن و معانی پیشین در Changelog حفظ‌اند؛ مشخصات جاری در بخش‌های این سند است. |
| GDD-62 | History | شناسهٔ سابقه؛ متن و معانی پیشین در Changelog حفظ‌اند؛ مشخصات جاری در بخش‌های این سند است. |
| GDD-63 | History | شناسهٔ سابقه؛ متن و معانی پیشین در Changelog حفظ‌اند؛ مشخصات جاری در بخش‌های این سند است. |
| GDD-64 | History | شناسهٔ سابقه؛ متن و معانی پیشین در Changelog حفظ‌اند؛ مشخصات جاری در بخش‌های این سند است. |
| GDD-65 | History | شناسهٔ سابقه؛ متن و معانی پیشین در Changelog حفظ‌اند؛ مشخصات جاری در بخش‌های این سند است. |
| GDD-66 | History | شناسهٔ سابقه؛ متن و معانی پیشین در Changelog حفظ‌اند؛ مشخصات جاری در بخش‌های این سند است. |
| GDD-67 | History | شناسهٔ سابقه؛ متن و معانی پیشین در Changelog حفظ‌اند؛ مشخصات جاری در بخش‌های این سند است. |
| GDD-68 | History | شناسهٔ سابقه؛ متن و معانی پیشین در Changelog حفظ‌اند؛ مشخصات جاری در بخش‌های این سند است. |
| GDD-69 | History | شناسهٔ سابقه؛ متن و معانی پیشین در Changelog حفظ‌اند؛ مشخصات جاری در بخش‌های این سند است. |
| GDD-70 | History | شناسهٔ سابقه؛ متن و معانی پیشین در Changelog حفظ‌اند؛ مشخصات جاری در بخش‌های این سند است. |

برخی شناسه‌های ۵۸–۶۰ قبلاً برای بیش از یک موضوع استفاده شده‌اند؛ همهٔ معانی بدون شماره‌گذاری مجدد در نسخهٔ تاریخی Changelog حفظ‌اند. شناسهٔ سند به معنی دارایی وظیفه در اجرا نیست.

### 14. منابع

| فایل | شرح |
| --- | --- |
| [GameStages.md](GameStages.md) | مرحلهٔ ۰، ترتیب واقعی هدف‌ها و شرط تکمیل |
| [CurriculumPlan.md](CurriculumPlan.md) · [JSON](CurriculumPlan.json) | طرح پایهٔ ۴۰۰تایی؛ ۴۲۸ وظیفهٔ جاری؛ تولید آزاد Next |
| [tools.txt](tools.txt) | نام اشیا و معماری به سه زبان؛ شمارهٔ بخش‌ها مربوط به توسعه است |
| [Learning.md](Learning.md) | راهنمای پیاده‌سازی، کنترل و بررسی |
| [IntroVoice.json](IntroVoice.json) | متن متناظر با کلید فایل صوتی |
| [IntroVoiceProfile.json](IntroVoiceProfile.json) | پروفایل صدای فعلی |
| [SampleScene.unity](../Assets/Scenes/SampleScene.unity) | صحنهٔ فعلی |
| [LearningProgress.cs](../Assets/scripts/LearningProgress.cs) | منبع منطق و ترتیب مراحل |

### 15. نگهداری

همراه هر درخواست جدید، الزام مرتبط در سه زبان و وضعیت واقعی آن اصلاح شود. تغییر مراحل در GameStages.md ثبت شود؛ برای تغییری که مرحلهٔ قابل بازی نمی‌سازد، شمارهٔ تازه اختراع نشود. نام هر شیء جدید در tools.txt ثبت شود. موارد انجام‌نشده فقط با وضعیت Next یا Verify درج شوند؛ محدودیت‌ها پنهان نشوند. راهنمای پایدار این کار در [AGENTS.md](../AGENTS.md) ثبت شده است.

GDD فقط مشخصات جاری را نگه دارد؛ هر تغییر با تاریخ محلی و توضیح سه‌زبانه در [CHANGELOG.md](CHANGELOG.md) ثبت شود. تصمیم‌های نامشخص به‌جای اختراع قابلیت، نامشخص بمانند.

---

## English

Last updated: **2026-11-27 — Europe/Berlin**

| Document | Role |
| --- | --- |
| **GDD** | Current design |
| [GameStages](GameStages.md) | Task order and completion |
| [Changelog](CHANGELOG.md) | History and earlier checks |

### 1. Overview and vision

DeutschRoom is a first-person 3D learning game set in a home. Finding and identifying objects, moving items, selecting answers and carrying out household scenarios practise German vocabulary and grammar. Design pillars: learning in context, clear instruction/action links, recoverable mistakes and optional help.

### 2. Audience and scope

Intended for German learners; an age group is unspecified. Content uses internal A1.1–C1.2 bands without certifying proficiency or formal outcomes. Touch and desktop mouse/keyboard controls exist; release readiness and actual device quality are unverified. Currency, lives and in-game payments are not implemented.

### 3. Core loop

Start/resume → read/hear instruction → find the object or examine the case → perform the required action and answer when requested → feedback/retry → save progress and advance. Review targets remain hidden on the map until help is requested. Controls practice is separate from lessons and earns no course progress. Free practice remains available after completion.

### 4. Content and progression

| Metric | Current state |
| --- | --- |
| Tasks | **428**; P: 106 · I: 92 · G: 90 · C: 140 |
| Scene population | **53** portable + **79** fixed |
| Start | CUR-001 · Loeffel_Spoon · der Löffel |
| Ending | CUR-430 · **428** |
| Retired | CUR-102 · CUR-191; Hall_Mirror; fixed order **48** vacant |
| Case packets | E · M · W · H; **16** pages |

P is genuine pickup, I target identification, G grammar and C combined scenarios. Fifty-three portable introductions precede 79 fixed introductions, followed by reviews, grammar and scenarios. GameStages and IntroA11.json define exact order; display numbers differ from CUR IDs. The spoon remains first by swapping it with the first LessonOrder-sorted target. Saves use IDs rather than old counts; the legacy course is removed.

### 5. Mechanics and controls

#### 5.1 Movement and interaction

| ID | Status | Description |
| --- | --- | --- |
| GDD-03 | Done | Pause freezes movement and world animation; resume restores play. |
| GDD-04 | Done | Mobile controls use a movement joystick, swipe look and contextual action buttons. |
| GDD-05 | Done | Desktop: WASD, mouse look, E interact, F whole name, Shift+F part, G place, R rotate, Escape pause, H reveals a review target. |
| GDD-06 | Done | Swipe down and hold for 0.6 seconds to sit; release and repeat to lie down. Swipe up and hold to rise one posture. Desktop equivalents are C/X. |
| GDD-07 | Done | Camera and collision capsule change with posture; obstacles prevent standing up. |
| GDD-08 | Done | A single circular Anhören button for the nearby aimed object identifies it using the selected text/voice mode. |
| GDD-09 | Done | Carry one item at a time, rotate it by 45 degrees and place it on a reachable upward-facing surface. |
| GDD-10 | Done | Cabinet, drawer, appliance and room doors are interactive; the faucet has a visual water stream. |
| GDD-11 | Done | The kitchen has 53 portable objects, including dishes, tools and food in cupboards, drawer, under-sink cabinet, refrigerator and freezer. |
| GDD-12 | Done | Chairs, table, shelves and sink components such as drain, trap, pipes and hoses have vocabulary labels; fixed parts cannot be carried. |
| GDD-25 | Done | Drawer contents move with it; placing an item on a static surface detaches it, and returning it to the drawer reattaches it. |
| GDD-31 | Done | The hood and major existing objects are named. Recognizable parts such as table legs/top, cabinet doors/handles, window frames and bathroom parts have names and voices; Anhören names only the owner and Detail names only the part, preserving the owner's action. |

#### 5.2 Completion and guidance

| ID | Status | Description |
| --- | --- | --- |
| GDD-15 | Done | The sole course has 428 tasks: 106 P, 92 I, 90 G, 140 C; CUR-102 and CUR-191 are retired. |
| GDD-16 | Done | Combined tasks require the specified action and correct response; CUR-430 ends the course at display number 428. |
| GDD-19 | Done | Map follows live target/route; reviews hide marker/route until Ziel zeigen/H and hide them again on the next task. |
| GDD-24 | Done | CUR-based progress and portable object poses save locally; restart requires confirmation; no cloud saves. |

### 6. World and objects

| ID | Status | Description |
| --- | --- | --- |
| GDD-01 | Done | Learn German vocabulary with articles in a first-person 3D home. |
| GDD-13 | Done | The kitchen window overlooks a decorative garden with trees, grass, shrubs, flowers, paths, stones, a bench and hills. |
| GDD-14 | Done | Bedrooms, living room, entrance and bathroom are furnished; 79 fixed targets remain after hall mirror removal. |
| GDD-26 | Done | All eighty planned instances were built; 79 fixed targets remain after hall mirror removal; additions require a new request. |
| GDD-30 | Done | F01–F16 construction queue is complete; this history does not imply the hall mirror still exists. |
| GDD-34 | Done | 20 F01–F04 instances provide beds/bedding, wardrobe, dresser, bedside furniture, lamps, hanger, desk, office chair, bookcase and book; each instance has its own identification target. Eight doors/drawers open and meaningful parts have names/voices. |
| GDD-36 | Done | Nineteen F05–F08 fixed targets remain after hall mirror removal; order 48 stays vacant. |
| GDD-38 | Done | Twenty F09, F12, F13 and first-four-F14 instances have independent targets, named/spoken parts and two washer opening actions. |
| GDD-40 | Done | Final ten F14–F16 objects have German names/voice, independent targets and named parts; the switch controls three living-room lights and resets on at scene reload. |

### 7. Interface and accessibility

| ID | Status | Description |
| --- | --- | --- |
| GDD-02 | Done | The opening menu provides start, continue, restart, settings, control selection and an English spotlight How to play tour that opens automatically only on first launch and can be replayed from the menu. |
| GDD-20 | Done | Lesson settings provide text only, voice only or both, narration volume, a sound test and a no-sound fallback; mode and volume persist. |
| GDD-21 | Done | Zero media volume or a no-sound report enables a written warning and text fallback; automatic detection of the iPhone Silent switch is unsupported. |

### 8. Art and audio

| ID | Status | Description |
| --- | --- | --- |
| GDD-22 | Done | German Tanja voice at 8% slower rate; 1881 files present in resources. Phone playback requires verification. |
| GDD-23 | Done | Music loads from Assets/Resources/Music/background.mp3, has a toggle in the shared menu beside the map and ducks during narration. The user supplies the track. |
| GDD-29 | Done | Visual polish adds wood, plaster, stone, tile and metal surfaces; softened edges, refined bowl/plate/glass and spoon/fork/knife/apple/tomato models, baseboards and tile cladding, environment light/reflections and native-resolution mobile rendering. |

### 9. Limits and future work

| ID | Status | Description |
| --- | --- | --- |
| GDD-27 | Verify | Real-device testing is required for multitouch, UI sizing, sound, headphones, app resume and mute warnings; Unity tests do not replace it. |
| GDD-28 | Limit | Current free placement does not reject item overlaps or table edges; cooking, refrigeration and water accumulation are not simulated. |
| GDD-33 | Limit | Bathroom fixtures currently support identification; shower water, flushing and drainage are not simulated. The vanity's two doors open; the existing kitchen faucet remains functional. |
| GDD-35 | Limit | Bedroom objects remain fixed; pillows, book and hanger are not portable. First-bedroom bedside lamp and second-bedroom desk lamp now receive runtime action owners when scenarios initialize; other lamps in this batch have no independent controls. |
| GDD-37 | Limit | TV has a still image with no channel control; the hall switch controls three living-room lights. |
| GDD-39 | Limit | Accessories remain fixed; washing, cleaning, umbrella folding and a live clock are not simulated. |
| GDD-41 | Limit | These ten objects are fixed; heating, socket operation, cutting, toasting, brewing and weighing are not simulated. |
| GDD-42 | Next | 428 selection-based adaptations run; original free-production criteria remain Next. |
| GDD-43 | Done | Text, choices and action criteria for 428 tasks/four packets are defined in runtime resources; free production remains open in GDD-42/GDD-44 and engine testing in GDD-49. |
| GDD-44 | Next | Target A1.1–C1.2 with review every ten and checks every twenty stages; .1/.2 are internal divisions. C1 needs multiple perspectives, inference, open responses and fresh follow-ups; click counts do not certify level. |
| GDD-45 | Verify | Flush, hood, shower, basin tap, bedside and desk lamps have runtime controls, event counters and independent presets in code. Effects are educational simulations; full engine/device execution remains unverified. IDs/progress are retained. |
| GDD-46 | Verify | 428 rows, IDs, 53-portable/79-fixed bindings, four packets and voice counts passed static/managed checks. Level equivalence, assessor quality and learner experience still require teaching review/trials; no formal C1 result is recorded. |
| GDD-47 | Done | Internal A1.1–C1.2 content; formal/free-production C1 assessment not implemented. |
| GDD-48 | Limit | Sonna file-generation access remains unconfirmed; after the new user request another German voice was selected and Tanja installed, without labeling it Sonna. |
| GDD-49 | Verify | Compilation/managed checks pass. Latest isolated launch exited −9 before tests without a Unity log; codesign reports invalid signatures for both Editor installations. Earlier runs failed on Licensing Client/headless. No full engine/device pass is recorded. |

The bathroom mirror has live reflections; final appearance and phone performance require testing. A procedural first-person avatar and hand motion are implemented. Instruction translation opens through i/I without revealing choices or completing tasks. English How to play is available from the menu.

### 10. Technology and data

Unity 6000.6.7f1 with URP; SampleScene is the main scene. LearningProgress defines progression/local persistence, IntroA11.json runtime lessons and room catalogs generated inventory. Preserve all five furnishing roots. Actual RAM/FPS, mobile release and formal language assessment remain unverified.

### 11. Acceptance and validation

Target, name, audio and map marker must identify the correct instance. P requires a fresh pickup; I accepts primary identification of the target or its owned part. Wrong answers/unrelated actions must not advance progress. Translation/tutorial help cannot complete tasks. Scenarios use independent temporary presets; action/answer gates must match each task's data. ID-based progress and item poses must survive resume. Static/managed checks, Unity/device validation and educational validity are separate; earlier results are dated in Changelog.

### 12. Status legend

| Code | Description |
| --- | --- |
| Done | Implemented in code/scene; not a release certification |
| Next | Requested future work; not implemented |
| Verify | Verification required, not fully confirmed |
| Limit | Current limitation or unsupported capability |
| Retired | Removed from play; history only |
| History | Historical report ID, not current requirement |

### 13. Historical ID traceability

| ID | Status | Description |
| --- | --- | --- |
| GDD-17 | Retired | Removed legacy-course rule; retained in Changelog only, not a current objective. |
| GDD-18 | Retired | Removed legacy-course rule; retained in Changelog only, not a current objective. |
| GDD-32 | Retired | Removed legacy-course rule; retained in Changelog only, not a current objective. |
| GDD-50 | History | Historical ID; original text and previous meanings remain in Changelog; current specification is in this document. |
| GDD-51 | History | Historical ID; original text and previous meanings remain in Changelog; current specification is in this document. |
| GDD-52 | History | Historical ID; original text and previous meanings remain in Changelog; current specification is in this document. |
| GDD-53 | History | Historical ID; original text and previous meanings remain in Changelog; current specification is in this document. |
| GDD-54 | History | Historical ID; original text and previous meanings remain in Changelog; current specification is in this document. |
| GDD-55 | History | Historical ID; original text and previous meanings remain in Changelog; current specification is in this document. |
| GDD-56 | History | Historical ID; original text and previous meanings remain in Changelog; current specification is in this document. |
| GDD-57 | History | Historical ID; original text and previous meanings remain in Changelog; current specification is in this document. |
| GDD-58 | History | Historical ID; original text and previous meanings remain in Changelog; current specification is in this document. |
| GDD-59 | History | Historical ID; original text and previous meanings remain in Changelog; current specification is in this document. |
| GDD-60 | History | Historical ID; original text and previous meanings remain in Changelog; current specification is in this document. |
| GDD-61 | History | Historical ID; original text and previous meanings remain in Changelog; current specification is in this document. |
| GDD-62 | History | Historical ID; original text and previous meanings remain in Changelog; current specification is in this document. |
| GDD-63 | History | Historical ID; original text and previous meanings remain in Changelog; current specification is in this document. |
| GDD-64 | History | Historical ID; original text and previous meanings remain in Changelog; current specification is in this document. |
| GDD-65 | History | Historical ID; original text and previous meanings remain in Changelog; current specification is in this document. |
| GDD-66 | History | Historical ID; original text and previous meanings remain in Changelog; current specification is in this document. |
| GDD-67 | History | Historical ID; original text and previous meanings remain in Changelog; current specification is in this document. |
| GDD-68 | History | Historical ID; original text and previous meanings remain in Changelog; current specification is in this document. |
| GDD-69 | History | Historical ID; original text and previous meanings remain in Changelog; current specification is in this document. |
| GDD-70 | History | Historical ID; original text and previous meanings remain in Changelog; current specification is in this document. |

IDs 58–60 were previously reused for different topics; all meanings remain in the historical Changelog snapshot without renumbering. A document ID does not imply a runtime task asset.

### 14. Sources

| File | Description |
| --- | --- |
| [GameStages.md](GameStages.md) | Stage 0, actual objective order and completion rules |
| [CurriculumPlan.md](CurriculumPlan.md) · [JSON](CurriculumPlan.json) | 400-task baseline; 428 live tasks; free production Next |
| [tools.txt](tools.txt) | Trilingual object/architecture names; section numbers refer to development |
| [Learning.md](Learning.md) | Implementation, controls and validation guide |
| [IntroVoice.json](IntroVoice.json) | Text mapped to each audio-file key |
| [IntroVoiceProfile.json](IntroVoiceProfile.json) | Current voice profile |
| [SampleScene.unity](../Assets/Scenes/SampleScene.unity) | Current scene |
| [LearningProgress.cs](../Assets/scripts/LearningProgress.cs) | Source of progression/order rules |

### 15. Maintenance

With each new request, update the related trilingual requirement and actual status. Record stage changes in GameStages.md; do not invent a new playable stage for a feature-only change. Register every new object name in tools.txt. Mark unfinished work Next or Verify and retain known limitations. Persistent instructions are in [AGENTS.md](../AGENTS.md).

Keep current specifications in GDD; record each change with local date and trilingual description in [CHANGELOG.md](CHANGELOG.md). Leave undecided design questions explicit rather than inventing features.

---

## Deutsch

Zuletzt aktualisiert: **2026-11-27 — Europe/Berlin**

| Dokument | Zweck |
| --- | --- |
| **GDD** | Aktuelles Design |
| [GameStages](GameStages.md) | Aufgabenfolge und Abschluss |
| [Changelog](CHANGELOG.md) | Verlauf und frühere Prüfungen |

### 1. Überblick und Vision

DeutschRoom ist ein dreidimensionales Lernspiel in Ichperspektive in einem Haus. Suchen und Benennen, Umplatzieren, Antwortauswahl und Haushaltsszenarien üben deutschen Wortschatz und Grammatik. Gestaltungsprinzipien: Lernen im Kontext, klare Verbindung von Anweisung und Handlung, korrigierbare Fehler und freiwillige Hilfe.

### 2. Zielgruppe und Umfang

Für Deutschlernende; Altersgruppe nicht festgelegt. Inhalte nutzen interne Abschnitte A1.1–C1.2 ohne Niveaunachweis oder formales Ergebnis. Touch sowie Maus/Tastatur vorhanden; Veröffentlichungsreife und Gerätequalität ungeprüft. Währung, Leben und In-App-Zahlungen nicht umgesetzt.

### 3. Kernablauf

Start/Fortsetzen → Anweisung lesen/hören → Objekt suchen oder Fall prüfen → geforderte Handlung und gegebenenfalls Antwort → Rückmeldung/Korrektur → speichern und weiter. Wiederholungsziele bleiben bis zur Hilfeanforderung auf der Karte verborgen. Steuerungsübung ist vom Lernpfad getrennt und erzeugt keinen Lernfortschritt. Nach Abschluss bleibt freies Üben möglich.

### 4. Inhalte und Fortschritt

| Kennzahl | Aktueller Stand |
| --- | --- |
| Aufgaben | **428**; P: 106 · I: 92 · G: 90 · C: 140 |
| Szenenbestand | **53** tragbar + **79** fest |
| Anfang | CUR-001 · Loeffel_Spoon · der Löffel |
| Ende | CUR-430 · **428** |
| Stillgelegt | CUR-102 · CUR-191; Hall_Mirror; feste Reihenfolge **48** bleibt frei |
| Fallpakete | E · M · W · H; **16** Seiten |

P bedeutet echtes Aufheben, I Zielbenennung, G Grammatik, C kombinierte Szenarien. Auf 53 tragbare Einführungen folgen 79 feste, danach Wiederholung, Grammatik und Szenarien. GameStages und IntroA11.json bestimmen die genaue Folge; Anzeigenummern unterscheiden sich von CUR-IDs. Der Löffel bleibt durch Tausch mit dem ersten nach LessonOrder sortierten Ziel vorne. Spielstände nutzen IDs statt alter Anzahlen; Altpfad entfernt.

### 5. Mechanik und Steuerung

#### 5.1 Bewegung und Interaktion

| ID | Status | Beschreibung |
| --- | --- | --- |
| GDD-03 | Done | Pause hält Bewegung und Weltanimationen an; Fortsetzen setzt das Spiel fort. |
| GDD-04 | Done | Mobilsteuerung: Bewegungsjoystick, Wischen zum Umschauen und situationsabhängige Aktionstasten. |
| GDD-05 | Done | Computer: WASD, Mausblick, E Aktion, F Gesamtname, Shift+F Teil, G ablegen, R drehen, Escape Pause, H Wiederholungsziel zeigen. |
| GDD-06 | Done | Nach unten wischen und 0,6 Sekunden halten: hinsetzen; loslassen und wiederholen: hinlegen. Nach oben wischen und halten: eine Haltung aufrichten. Am Computer: C/X. |
| GDD-07 | Done | Kamera und Kollisionskapsel folgen der Körperhaltung; Hindernisse verhindern das Aufstehen. |
| GDD-08 | Done | Eine einzelne runde Anhören-Taste für den nahen anvisierten Gegenstand benennt ihn im gewählten Text-/Sprachmodus. |
| GDD-09 | Done | Jeweils einen Gegenstand tragen, um 45 Grad drehen und auf einer erreichbaren nach oben gerichteten Fläche ablegen. |
| GDD-10 | Done | Schranktüren, Schublade, Geräte- und Zimmertüren sind bedienbar; der Wasserhahn zeigt einen Wasserstrahl. |
| GDD-11 | Done | Die Küche enthält 53 tragbare Gegenstände: Geschirr, Werkzeuge und Lebensmittel in Schränken, Schublade, Spülenunterschrank, Kühlschrank und Gefrierfach. |
| GDD-12 | Done | Stühle, Tisch, Böden und Spülenteile wie Abfluss, Siphon, Rohre und Schläuche sind benannt; feste Teile sind nicht tragbar. |
| GDD-25 | Done | Schubladeninhalt bewegt sich mit; Ablegen auf einer festen Fläche löst den Gegenstand, Zurücklegen in die Schublade verbindet ihn wieder. |
| GDD-31 | Done | Abzugshaube und wichtige vorhandene Objekte sind benannt. Erkennbare Teile wie Tischbeine/-platte, Schranktüren/-griffe, Fensterrahmen und Badteile haben Namen und Stimmen; Anhören benennt nur das Gesamtobjekt und Detail nur das Teil; die ursprüngliche Aktion bleibt erhalten. |

#### 5.2 Abschluss und Hinweise

| ID | Status | Beschreibung |
| --- | --- | --- |
| GDD-15 | Done | Einziger Lernpfad: 428 Aufgaben, 106 P, 92 I, 90 G, 140 C; CUR-102 und CUR-191 stillgelegt. |
| GDD-16 | Done | Kombinationsaufgaben verlangen vorgesehene Handlung und richtige Antwort; CUR-430 beendet den Pfad bei Anzeigenummer 428. |
| GDD-19 | Done | Karte folgt echtem Ziel/Weg; Wiederholungen verbergen Ziel/Weg bis Ziel zeigen/H und bei nächster Aufgabe erneut. |
| GDD-24 | Done | CUR-basierter Fortschritt und Positionen tragbarer Objekte lokal gespeichert; Neustart nach Bestätigung; keine Cloud-Speicherung. |

### 6. Welt und Objekte

| ID | Status | Beschreibung |
| --- | --- | --- |
| GDD-01 | Done | Deutsche Wörter mit Artikeln in einem dreidimensionalen Zuhause aus der Ichperspektive lernen. |
| GDD-13 | Done | Durch das Küchenfenster sieht man einen dekorativen Garten mit Bäumen, Gras, Sträuchern, Blumen, Wegen, Steinen, einer Bank und Hügeln. |
| GDD-14 | Done | Schlafzimmer, Wohnzimmer, Eingang und Bad sind eingerichtet; nach Entfernung des Flurspiegels bleiben 79 feste Ziele. |
| GDD-26 | Done | Alle achtzig Planinstanzen wurden gebaut; nach Flurspiegelentfernung bleiben 79 feste Ziele; Ergänzungen erfordern neue Anweisung. |
| GDD-30 | Done | Bauplan F01–F16 abgeschlossen; daraus folgt kein aktueller Bestand des Flurspiegels. |
| GDD-34 | Done | 20 F01–F04-Instanzen umfassen Betten/Bettzeug, Schrank, Kommode, Nachttisch, Lampen, Bügel, Schreibtisch, Bürostuhl, Regal und Buch; jede hat ein eigenes Benennungsziel. Acht Türen/Schubladen öffnen sich, wichtige Teile haben Namen/Stimmen. |
| GDD-36 | Done | Nach Flurspiegelentfernung bleiben 19 feste F05–F08-Ziele; Reihenfolge 48 bleibt frei. |
| GDD-38 | Done | Zwanzig Instanzen aus F09, F12, F13 und den ersten vier F14-Zeilen haben eigene Ziele, benannte/gesprochene Teile und zwei Waschmaschinen-Öffnungsaktionen. |
| GDD-40 | Done | Letzte zehn F14–F16-Objekte haben deutsche Namen/Stimme, eigene Ziele und benannte Teile; der Schalter steuert drei Wohnzimmerlampen und ist nach Neuladen wieder an. |

### 7. Oberfläche und Zugänglichkeit

| ID | Status | Beschreibung |
| --- | --- | --- |
| GDD-02 | Done | Das Startmenü bietet Start, Fortsetzen, Neustart, Einstellungen, Steuerungswahl und eine englische How-to-play-Einführung direkt im Spiel, die nur beim ersten Start automatisch und danach auf Menüaufruf erscheint. |
| GDD-20 | Done | Lektionseinstellungen bieten nur Text, nur Stimme oder beides, Sprachlautstärke, Klangtest und eine Hilfe bei fehlendem Ton; Modus und Lautstärke werden gespeichert. |
| GDD-21 | Done | Bei Medienlautstärke null oder gemeldetem fehlendem Ton erscheinen ein schriftlicher Hinweis und Ersatztext; der iPhone-Stummschalter wird nicht automatisch erkannt. |

### 8. Gestaltung und Audio

| ID | Status | Beschreibung |
| --- | --- | --- |
| GDD-22 | Done | Deutsche Tanja-Stimme, 8 % langsamer; 1881 Dateien in Ressourcen. Smartphone-Wiedergabe benötigt Prüfung. |
| GDD-23 | Done | Musik wird aus Assets/Resources/Music/background.mp3 geladen, lässt sich im gemeinsamen Menü neben der Karte schalten und wird während der Sprache leiser. Die Musikdatei liefert der Nutzer. |
| GDD-29 | Done | Optische Überarbeitung mit Holz-, Putz-, Stein-, Fliesen- und Metalloberflächen; weicheren Kanten, verfeinerten Schüssel-/Teller-/Glas- sowie Löffel-/Gabel-/Messer-/Apfel-/Tomatenmodellen, Sockelleisten und Fliesenverkleidung, Umgebungslicht/-reflexionen und mobiler Darstellung in nativer Auflösung. |

### 9. Grenzen und künftige Arbeit

| ID | Status | Beschreibung |
| --- | --- | --- |
| GDD-27 | Verify | Tests auf echten Geräten für Mehrfingereingabe, UI-Größe, Ton, Kopfhörer, App-Rückkehr und Stummhinweise sind nötig; Unity-Tests ersetzen sie nicht. |
| GDD-28 | Limit | Das freie Ablegen verhindert derzeit weder Überlappungen noch Tischkanten; Kochen, Kühlung und Wasseransammlung werden nicht simuliert. |
| GDD-33 | Limit | Badobjekte dienen derzeit der Benennung; Duschwasser, Spülung und Abfluss werden nicht simuliert. Die zwei Waschtischtüren öffnen sich; der bisherige Küchenwasserhahn bleibt funktionsfähig. |
| GDD-35 | Limit | Schlafzimmerobjekte bleiben fest; Kissen, Buch und Bügel sind nicht tragbar. Nachttischlampe im ersten und Leselampe im zweiten Schlafzimmer erhalten bei Szenariostart Laufzeitbedienung; andere Lampen dieser Gruppe haben keine eigene Steuerung. |
| GDD-37 | Limit | TV zeigt ein Standbild ohne Kanalwahl; Flurschalter steuert drei Wohnzimmerlampen. |
| GDD-39 | Limit | Zubehör bleibt fest; Waschen, Reinigen, Schirmfalten und laufende Uhr nicht simuliert. |
| GDD-41 | Limit | Diese zehn Objekte sind fest; Heizung, Steckdosenbetrieb, Schneiden, Toasten, Kaffeebrühen und Wiegen werden nicht simuliert. |
| GDD-42 | Next | 428 Auswahlanpassungen laufen; ursprüngliche freie Produktionskriterien bleiben Next. |
| GDD-43 | Done | Text, Auswahl und Handlungskriterien für 428 Aufgaben/vier Pakete liegen in Laufzeitressourcen; freie Produktion bleibt in GDD-42/GDD-44 und Engineprüfung in GDD-49 offen. |
| GDD-44 | Next | Ziel A1.1–C1.2 mit Wiederholung je zehn und Prüfung je zwanzig Aufgaben; .1/.2 sind interne Abschnitte. C1 braucht Perspektiven, Schlussfolgern, offene Antworten und neue Rückfragen; Klickzahlen belegen kein Niveau. |
| GDD-45 | Verify | Spülung, Haube, Dusche, Waschtischarmatur, Nacht-/Leselampe haben Laufzeitbedienung, Ereigniszähler und unabhängige Vorgaben im Code. Effekte sind Lernsimulationen; vollständige Engine-/Geräteausführung ungeprüft. IDs/Fortschritt bleiben. |
| GDD-46 | Verify | 428 Zeilen, IDs, Bindungen an 53 tragbare/79 feste Ziele, vier Quellen und Sprachanzahl sind statisch/verwaltet geprüft. Niveaugerechtigkeit, Bewertungsqualität und Lernerfahrung brauchen didaktische Prüfung/Tests; kein formales C1-Ergebnis. |
| GDD-47 | Done | Interner Inhalt A1.1–C1.2; formale/freie C1-Produktionsbewertung nicht umgesetzt. |
| GDD-48 | Limit | Sonna-Dateierzeugung bleibt unbestätigt; auf neue Nutzeranweisung andere deutsche Stimme gewählt und Tanja installiert, nicht als Sonna bezeichnet. |
| GDD-49 | Verify | Kompilierung/verwaltete Prüfungen bestanden. Neuester isolierter Start endete vor Tests mit −9 ohne Unity-Log; codesign meldet ungültige Signaturen beider Editorinstallationen. Frühere Läufe scheiterten an Licensing Client/headless. Kein vollständiger Engine-/Gerätepass. |

Der Badspiegel hat Echtzeitreflexionen; endgültiges Aussehen und Smartphone-Leistung benötigen Tests. Prozedurale Ichfigur und Handbewegung sind umgesetzt. Anweisungsübersetzung über i/I verrät keine Antwort und erfüllt keine Aufgabe. Englische How-to-play-Hilfe ist im Menü verfügbar.

### 10. Technik und Daten

Unity 6000.6.7f1 mit URP; Hauptszene SampleScene. LearningProgress bestimmt Fortschritt/lokales Speichern, IntroA11.json Laufzeitaufgaben und Raumkataloge erzeugten Bestand. Alle fünf Einrichtungswurzeln erhalten. Tatsächlicher RAM/FPS, mobile Veröffentlichung und formale Sprachbewertung ungeprüft.

### 11. Abnahme und Prüfung

Ziel, Name, Audio und Kartenmarkierung müssen dieselbe richtige Instanz benennen. P verlangt frisches Aufheben; I akzeptiert Hauptbenennung des Ziels oder zugehörigen Teils. Falsche Antworten/fremde Handlungen dürfen keinen Fortschritt auslösen. Übersetzung/Einführung erfüllen keine Aufgabe. Szenarien verwenden unabhängige temporäre Vorgaben; Handlungs-/Antwortbedingungen müssen zu den Aufgabendaten passen. ID-Fortschritt und Objektpositionen müssen Fortsetzen überstehen. Statische/verwaltete Prüfung, Unity-/Gerätetests und didaktische Gültigkeit sind getrennt; frühere Ergebnisse datiert im Changelog.

### 12. Statuslegende

| Code | Beschreibung |
| --- | --- |
| Done | In Code/Szene umgesetzt; keine Veröffentlichungsfreigabe |
| Next | Gewünschte künftige Arbeit; noch nicht umgesetzt |
| Verify | Prüfung erforderlich, noch nicht vollständig bestätigt |
| Limit | Aktuelle Einschränkung oder nicht unterstützte Funktion |
| Retired | Aus Spiel entfernt; nur Verlauf |
| History | Historische Berichts-ID, keine aktuelle Anforderung |

### 13. Historische ID-Nachverfolgung

| ID | Status | Beschreibung |
| --- | --- | --- |
| GDD-17 | Retired | Regel des entfernten Altpfads; nur im Changelog erhalten, kein aktuelles Ziel. |
| GDD-18 | Retired | Regel des entfernten Altpfads; nur im Changelog erhalten, kein aktuelles Ziel. |
| GDD-32 | Retired | Regel des entfernten Altpfads; nur im Changelog erhalten, kein aktuelles Ziel. |
| GDD-50 | History | Historische ID; Originaltext und frühere Bedeutungen bleiben im Changelog; aktuelle Spezifikation in diesem Dokument. |
| GDD-51 | History | Historische ID; Originaltext und frühere Bedeutungen bleiben im Changelog; aktuelle Spezifikation in diesem Dokument. |
| GDD-52 | History | Historische ID; Originaltext und frühere Bedeutungen bleiben im Changelog; aktuelle Spezifikation in diesem Dokument. |
| GDD-53 | History | Historische ID; Originaltext und frühere Bedeutungen bleiben im Changelog; aktuelle Spezifikation in diesem Dokument. |
| GDD-54 | History | Historische ID; Originaltext und frühere Bedeutungen bleiben im Changelog; aktuelle Spezifikation in diesem Dokument. |
| GDD-55 | History | Historische ID; Originaltext und frühere Bedeutungen bleiben im Changelog; aktuelle Spezifikation in diesem Dokument. |
| GDD-56 | History | Historische ID; Originaltext und frühere Bedeutungen bleiben im Changelog; aktuelle Spezifikation in diesem Dokument. |
| GDD-57 | History | Historische ID; Originaltext und frühere Bedeutungen bleiben im Changelog; aktuelle Spezifikation in diesem Dokument. |
| GDD-58 | History | Historische ID; Originaltext und frühere Bedeutungen bleiben im Changelog; aktuelle Spezifikation in diesem Dokument. |
| GDD-59 | History | Historische ID; Originaltext und frühere Bedeutungen bleiben im Changelog; aktuelle Spezifikation in diesem Dokument. |
| GDD-60 | History | Historische ID; Originaltext und frühere Bedeutungen bleiben im Changelog; aktuelle Spezifikation in diesem Dokument. |
| GDD-61 | History | Historische ID; Originaltext und frühere Bedeutungen bleiben im Changelog; aktuelle Spezifikation in diesem Dokument. |
| GDD-62 | History | Historische ID; Originaltext und frühere Bedeutungen bleiben im Changelog; aktuelle Spezifikation in diesem Dokument. |
| GDD-63 | History | Historische ID; Originaltext und frühere Bedeutungen bleiben im Changelog; aktuelle Spezifikation in diesem Dokument. |
| GDD-64 | History | Historische ID; Originaltext und frühere Bedeutungen bleiben im Changelog; aktuelle Spezifikation in diesem Dokument. |
| GDD-65 | History | Historische ID; Originaltext und frühere Bedeutungen bleiben im Changelog; aktuelle Spezifikation in diesem Dokument. |
| GDD-66 | History | Historische ID; Originaltext und frühere Bedeutungen bleiben im Changelog; aktuelle Spezifikation in diesem Dokument. |
| GDD-67 | History | Historische ID; Originaltext und frühere Bedeutungen bleiben im Changelog; aktuelle Spezifikation in diesem Dokument. |
| GDD-68 | History | Historische ID; Originaltext und frühere Bedeutungen bleiben im Changelog; aktuelle Spezifikation in diesem Dokument. |
| GDD-69 | History | Historische ID; Originaltext und frühere Bedeutungen bleiben im Changelog; aktuelle Spezifikation in diesem Dokument. |
| GDD-70 | History | Historische ID; Originaltext und frühere Bedeutungen bleiben im Changelog; aktuelle Spezifikation in diesem Dokument. |

IDs 58–60 wurden früher für verschiedene Themen verwendet; sämtliche Bedeutungen bleiben ohne Neunummerierung im historischen Changelog erhalten. Eine Dokument-ID bedeutet kein Laufzeit-Aufgabenasset.

### 14. Quellen

| Datei | Beschreibung |
| --- | --- |
| [GameStages.md](GameStages.md) | Stufe 0, tatsächliche Zielreihenfolge und Abschlussregeln |
| [CurriculumPlan.md](CurriculumPlan.md) · [JSON](CurriculumPlan.json) | 400er-Grundplan; 428 aktuelle Aufgaben; freie Produktion Next |
| [tools.txt](tools.txt) | Dreisprachige Objekt-/Architekturnamen; Abschnittsnummern beziehen sich auf die Entwicklung |
| [Learning.md](Learning.md) | Leitfaden für Umsetzung, Steuerung und Prüfung |
| [IntroVoice.json](IntroVoice.json) | Text zu jedem Audiodateischlüssel |
| [IntroVoiceProfile.json](IntroVoiceProfile.json) | Aktuelles Stimmprofil |
| [SampleScene.unity](../Assets/Scenes/SampleScene.unity) | Aktuelle Szene |
| [LearningProgress.cs](../Assets/scripts/LearningProgress.cs) | Quelle der Fortschritts- und Reihenfolgeregeln |

### 15. Pflege

Bei jeder neuen Anweisung die zugehörige dreisprachige Anforderung und ihren tatsächlichen Status aktualisieren. Aufgabenänderungen in GameStages.md nachführen; für reine Funktionsänderungen keine neue spielbare Stufe erfinden. Neue Objektnamen in tools.txt eintragen. Unfertige Arbeit als Next oder Verify kennzeichnen und bekannte Einschränkungen beibehalten. Dauerhafte Anweisungen stehen in [AGENTS.md](../AGENTS.md).

Im GDD nur aktuelle Spezifikationen pflegen; jede Änderung mit lokalem Datum und dreisprachiger Beschreibung in [CHANGELOG.md](CHANGELOG.md) festhalten. Offene Designfragen offen lassen statt Funktionen zu erfinden.
