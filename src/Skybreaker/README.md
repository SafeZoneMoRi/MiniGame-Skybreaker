# 🎖️ Heavy Weapons: Endless Survival & Command Replay Theater (Arcade 2D)

เกม **Heavy Weapons: Endless Air Raid Survival** พัฒนาขึ้นด้วยภาษา **C# (.NET 10.0)** และไลบรารี **Raylib-cs** ตามหลักการ **Object-Oriented Programming (OOP)** และ **GoF Command Pattern พร้อม Replay Theater** (GI161 Object Oriented Programming for Games)

---

## 🚀 วิธีการรันโปรเจกต์ (How to Run)

เปิด Command Prompt หรือ Terminal ในโฟลเดอร์โปรเจกต์ แล้วสั่งรันคำสั่ง:

```bash
cd src/HeavyWeapons
dotnet run
```

---

## 🎮 รูปแบบการเล่น (Endless Survival Mode)

* **ไม่มีการตัดจบแบบ Wave:** เครื่องบินศัตรูจะบินเข้ามาโจมตีอย่างต่อเนื่องแบบ Non-stop
* **ระดับความยากเพิ่มขึ้นตามเวลา (Progressive Threat Level):**
  * **0 - 20 วินาที:** เครื่องบินขับไล่ความเร็วสูง (Fighter Jets)
  * **20 - 45 วินาที:** เริ่มมีเครื่องบินทิ้งระเบิดหนัก (Heavy Bombers) เข้ามาเสริม
  * **45 วินาทีขึ้นไป:** เฮลิคอปเตอร์จู่โจมยิงจรวด (Attack Helicopters) พร้อมอัตราการสปอว์นที่ถี่ขึ้นเรื่อยๆ (ลงไปถึง 0.35 วินาทีต่อลำ)
  * **อีเวนต์บอสลอยฟ้า:** บอสยักษ์ **Dreadnought Air Fortress** จะบินตัดผ่านน่านฟ้าทุกๆ 50 วินาที
* **กล่องเสบียงชูชีพ (Supply Airdrops):** ทิ้งร่มลงมาเป็นระยะ ให้ขับรถถังไปเก็บเพื่อเติมเลือด (**+35 HP**)

---

## 💥 ระดับความเสียหาย (Lethal Damage)
* **Heavy Bomb (ลูกระเบิดดิ่งจากบอมเบอร์):** ดาเมจ **35 HP** (แรงสะใจ โดน 3 ลูกรถถังพังทันที ต้องคอยวิ่งหลบตลอดเวลา)
* **Guided Rocket (จรวดมิสไซล์จากเฮลิคอปเตอร์):** ดาเมจ **24 HP**
* **Red Machine Gun (ปืนกลเลเซอร์จากเจ็ต):** ดาเมจ **12 HP ต่อนัด**

---

## 🕹️ ลูปการเล่นและเมนูหลัก (Game Loop Architecture)

เกมมีวงจรสเตตสมบูรณ์แบบ (Finite State Loop):
`Loading Screen` ➔ `Main Menu` ➔ `How To Play` ➔ `Live Survival` ➔ `Game Over` ➔ `Replay Theater` ➔ `Main Menu`

### 1. หน้าจอ Loading Screen:
* จำลองการตรวจสอบไดรเวอร์และคอมไพล์สถาปัตยกรรม OOP Command Pattern พร้อมแถบ Progress Bar (0% - 100%)
* กด [Space] หรือ [Enter] เพื่อข้ามเข้าสู่เมนูหลักทันที

### 2. หน้าจอ Main Menu:
* กราฟิกลำแสงไฟค้นหา (Searchlights) ส่องฟากฟ้ายามค่ำคืนแบบเรียลไทม์
* เมนู 4 ตัวเลือก รองรับทั้งการคลิกเมาส์และกดปุ่มตัวเลข `[1-4]` / ลูกศร:
  * **[1] START MISSION:** เริ่มต้นภารกิจ Endless Survival
  * **[2] HOW TO PLAY & INTEL:** ดูคู่มือและข้อมูลยุทธวิธี
  * **[3] REPLAY THEATER:** ดูการเล่นย้อนหลังของรอบล่าสุด (ปลดล็อกเมื่อเล่นจบ 1 รอบ)
  * **[4] EXIT TO DESKTOP:** ออกจากเกม
* แสดงสถิติ **HIGH SCORE** และเวลา **LONGEST SURVIVAL**

### 3. ระหว่างเล่นสด (Live Survival):
* **[A / D]** หรือ **[ลูกศรซ้าย / ขวา]**: เคลื่อนที่รถถัง
* **[Mouse Aim]**: เล็งป้อมปืนขึ้นท้องฟ้า
* **[Spacebar]** หรือ **[คลิกซ้าย]**: ยิงปืนใหญ่หลัก
* **[ESC / M]**: พักและกลับสู่เมนูหลัก

### 4. หน้าจอจบเกม (Game Over):
* **[P]**: **เข้าดู REPLAY THEATER ของรอบล่าสุด**
* **[Space]** หรือ **[Enter]**: เริ่มเล่นรอบใหม่ทันที
* **[M]** หรือ **[ESC]**: กลับสู่หน้าเมนูหลัก (Main Menu)
