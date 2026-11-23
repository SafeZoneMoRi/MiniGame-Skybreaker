using System.Numerics;
using Raylib_cs;
using HeavyWeapons.Core.Commands;
using HeavyWeapons.Core.Entities;
using HeavyWeapons.Core.Entities.Enemies;
using HeavyWeapons.Core.Receivers;
using HeavyWeapons.Core.Replay;
using HeavyWeapons.Core.Systems;

namespace HeavyWeapons;

/// <summary>
/// ตัวควบคุมเกมหลัก (Game Runner / Coordinator)
/// มี Game Loop ที่สมบูรณ์แบบ: Loading Screen -> Main Menu -> How To Play -> Endless Survival -> Game Over -> Replay Theater
/// </summary>
public class GameRunner
{
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;
    private const float GroundLevelY = 640f;
    private static readonly Vector2 StartPosition = new(640f, GroundLevelY);

    // ตัวละครรถถัง (Receivers)
    private PlayerTank _player = null!;
    private GhostTank _ghost = null!;

    // สถาปัตยกรรม Command & Replay
    private readonly InputHandler _inputHandler = new();
    private readonly CommandRecorder _recorder = new();
    private readonly GhostPlaybackEngine _ghostEngine = new();

    // เอนทิตีในฉาก (Entities)
    private readonly List<Projectile> _projectiles = new(256);
    private readonly List<Bomb> _bombs = new(128);
    private readonly List<EnemyPlaneBase> _enemies = new(64);
    private readonly List<SupplyDrop> _supplyDrops = new(16);
    private readonly ParticleSystem _particles = new();
    private readonly WaveDirector _waveDirector = new();
    private float _supplyDropTimer = 12f;

    // สถานะของ Game Loop
    private enum GameState
    {
        LoadingScreen,
        MainMenu,
        HowToPlay,
        Playing,
        GameOver,
        ReplayTheater
    }

    private GameState _state = GameState.LoadingScreen;
    private GameState _stateBeforeReplay = GameState.GameOver;

    // ระบบหน้าโหลด (Loading Screen)
    private float _loadingProgress = 0f;
    private const float LoadingSpeed = 0.55f;

    // ระบบเมนูหลัก (Main Menu Animation)
    private float _searchlightAngle1 = -0.6f;
    private float _searchlightAngle2 = 0.6f;
    private int _menuSelectedIndex = 0; // 0: Start, 1: HowToPlay, 2: Replay, 3: Exit

    // ตัวแปรจับเวลาและคะแนน
    private float _survivalTime = 0f;
    private float _gameOverTimer = 0f;
    private int _score = 0;
    private int _bestScore = 0;
    private float _bestSurvivalTime = 0f;

    // บันทึกข้อมูลการเล่นรอบล่าสุด (Last Run Replay Data)
    private readonly List<TimedCommand> _lastRunTape = new(1024);
    private float _lastRunDuration = 0f;
    private int _lastRunScore = 0;
    private int _lastRunKills = 0;
    private int _lastRunSeed = 1337;
    private int _currentRunSeed = 1337;
    private float _replayTimer = 0f;

    // ระบบแรงสั่นสะเทือนหน้าจอ (Screen Shake)
    private float _screenShakeTimer = 0f;
    private Vector2 _shakeOffset = Vector2.Zero;

    // รายการบันทึก Command Stream ล่าสุดสำหรับแสดงบน HUD Console
    private readonly Queue<string> _commandLogQueue = new();

    public void Run()
    {
        Raylib.InitWindow(ScreenWidth, ScreenHeight, "Heavy Weapons: Endless Survival & Ghost Replay Engine | GI161");
        Raylib.SetTargetFPS(60);
        Raylib.SetExitKey(KeyboardKey.Null); // ปิดการทำงานของปุ่ม ESC ที่จะปิดเกมอัตโนมัติของ Raylib

        InitializeEntities();

        while (!Raylib.WindowShouldClose())
        {
            float dt = Raylib.GetFrameTime();
            Update(dt);
            Draw();
        }

        Raylib.CloseWindow();
    }

    private void InitializeEntities()
    {
        _player = new PlayerTank(StartPosition);
        _ghost = new GhostTank(StartPosition);

        // เชื่อม Action การยิงปืนใหญ่จากรถถังทั้งสองตัวให้เสก Projectile เข้าสู่โลกเกม
        _player.OnFireAction = (muzzleTip, angleDeg, isGhost) =>
        {
            _projectiles.Add(new Projectile(muzzleTip, angleDeg, 720f, isGhost));
            _particles.SpawnMuzzleFlash(muzzleTip, angleDeg, isCyan: false);
        };

        _ghost.OnFireAction = (muzzleTip, angleDeg, isGhost) =>
        {
            _projectiles.Add(new Projectile(muzzleTip, angleDeg, 720f, isGhost));
            _particles.SpawnMuzzleFlash(muzzleTip, angleDeg, isCyan: false);
        };
    }

    private void StartNewSurvivalRun()
    {
        _survivalTime = 0f;
        _score = 0;
        _state = GameState.Playing;
        _gameOverTimer = 0f;

        _projectiles.Clear();
        _bombs.Clear();
        _enemies.Clear();
        _supplyDrops.Clear();
        _particles.Clear();
        _supplyDropTimer = 12f;

        // สุ่ม Seed ประจำรอบเพื่อให้ศัตรูเกิดแบบคาดเดาไม่ได้ แต่ Replay ได้แม่นยำ 100%
        _currentRunSeed = Random.Shared.Next(10000, 99999);
        _waveDirector.StartRun(_currentRunSeed);

        _inputHandler.Reset();
        _recorder.StartRecording();

        _player.Reset(StartPosition);

        // ในโหมดเล่นปกติ: ปิด Ghost โดยสมบูรณ์! จะไม่มี Ghost มาวิ่งคู่ให้สับสน
        _ghost.IsActive = false;
        _ghost.IsTheaterMode = false;
        _ghostEngine.StopPlayback(_ghost);

        _commandLogQueue.Clear();
        AddCommandLog("System", "=== ENDLESS AIR RAID SURVIVAL INITIATED ===");
        AddCommandLog("System", "Survive as long as possible! Difficulty increases over time.");
    }

    private void StartReplayTheater()
    {
        if (_lastRunTape.Count == 0)
        {
            AddCommandLog("Replay", "No recorded run found to replay!");
            return;
        }

        _stateBeforeReplay = _state;
        _state = GameState.ReplayTheater;
        _replayTimer = 0f;

        _projectiles.Clear();
        _bombs.Clear();
        _enemies.Clear();
        _supplyDrops.Clear();
        _particles.Clear();

        // 1. ปิดตัวละครผู้เล่นจริงโดยสิ้นเชิง ไม่แสดงตัว และไม่รับดาเมจใดๆ
        _player.Stop();

        // 2. สั่ง GhostTank ให้สวมบทบาทเป็น Replay Tank ในโหมด Theater
        _ghost.IsTheaterMode = true;
        _ghost.Reset(StartPosition);
        _ghostEngine.LoadNewPersonalBest(0, _lastRunTape);
        _ghostEngine.StartPlayback(_ghost, StartPosition);

        // 3. เริ่มต้นระบบสปอว์นด้วย Seed เดียวกับรอบที่เพิ่งเล่น เพื่อให้เครื่องบินเกิดเหมือนเดิม 100%
        _waveDirector.StartRun(_lastRunSeed);

        AddCommandLog("Replay", $"🎬 REPLAY THEATER: Playing back run ({_lastRunTape.Count} cmds, {_lastRunDuration:00.0}s)");
    }

    private void ExitReplayTheater()
    {
        _ghostEngine.StopPlayback(_ghost);
        _ghost.IsTheaterMode = false;
        _ghost.IsActive = false;

        _projectiles.Clear();
        _bombs.Clear();
        _enemies.Clear();
        _particles.Clear();

        _state = _stateBeforeReplay;
        _gameOverTimer = 1.0f;
        AddCommandLog("Replay", "🎬 Exited Replay Theater. Returned to previous screen.");
    }

    private void Update(float dt)
    {
        _particles.Update(dt);

        // ------------------------------------------------------------------
        // 1. โหมดหน้าจอโหลดเกม (LOADING SCREEN)
        // ------------------------------------------------------------------
        if (_state == GameState.LoadingScreen)
        {
            _loadingProgress += dt * LoadingSpeed;
            if (_loadingProgress >= 1.0f || Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                _loadingProgress = 1.0f;
                _state = GameState.MainMenu;
            }
            return;
        }

        // ------------------------------------------------------------------
        // 2. โหมดหน้าจอเมนูหลัก (MAIN MENU)
        // ------------------------------------------------------------------
        if (_state == GameState.MainMenu)
        {
            _searchlightAngle1 += dt * 0.9f;
            _searchlightAngle2 -= dt * 0.7f;

            // การเลือกเมนูด้วยคีย์บอร์ด
            if (Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W))
            {
                _menuSelectedIndex = (_menuSelectedIndex + 3) % 4;
            }
            if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S))
            {
                _menuSelectedIndex = (_menuSelectedIndex + 1) % 4;
            }

            // ปุ่มลัดตัวเลข
            if (Raylib.IsKeyPressed(KeyboardKey.One)) _menuSelectedIndex = 0;
            if (Raylib.IsKeyPressed(KeyboardKey.Two)) _menuSelectedIndex = 1;
            if (Raylib.IsKeyPressed(KeyboardKey.Three)) _menuSelectedIndex = 2;
            if (Raylib.IsKeyPressed(KeyboardKey.Four)) _menuSelectedIndex = 3;

            // กด ESC ในหน้าเมนูหลัก: เลื่อนไปที่ปุ่ม Exit (ถ้าอยู่ปุ่ม Exit อยู่แล้วให้ปิดเกม)
            if (Raylib.IsKeyPressed(KeyboardKey.Escape))
            {
                if (_menuSelectedIndex == 3)
                {
                    Raylib.CloseWindow();
                }
                else
                {
                    _menuSelectedIndex = 3;
                }
            }

            // การกดยืนยันเลือกเมนู
            if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space))
            {
                ExecuteMenuSelection();
            }

            // ตรวจสอบการคลิกเมาส์บนปุ่มเมนู
            CheckMenuMouseClicks();
            return;
        }

        // ------------------------------------------------------------------
        // 3. โหมดหน้าจอคำแนะนำการเล่น (HOW TO PLAY)
        // ------------------------------------------------------------------
        if (_state == GameState.HowToPlay)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                _state = GameState.MainMenu;
            }
            return;
        }

        // คำนวณ Screen Shake สำหรับการเล่น
        if (_screenShakeTimer > 0f)
        {
            _screenShakeTimer -= dt;
            _shakeOffset = new Vector2(
                (float)(Random.Shared.NextDouble() - 0.5) * 10f,
                (float)(Random.Shared.NextDouble() - 0.5) * 10f
            );
        }
        else
        {
            _shakeOffset = Vector2.Zero;
        }

        // ------------------------------------------------------------------
        // 4. โหมด REPLAY THEATER: ฉายซ้ำเฉพาะตัว GhostTank (ไม่มีตัวละคร Player)
        // ------------------------------------------------------------------
        if (_state == GameState.ReplayTheater)
        {
            _replayTimer += dt;

            // กด P, Space หรือ ESC เพื่อออกจาก Replay กลับสู่หน้าเดิมทันที
            if (Raylib.IsKeyPressed(KeyboardKey.P) || Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.Escape))
            {
                ExitReplayTheater();
                return;
            }

            // GhostPlaybackEngine ป้อนคำสั่งลงบน GhostTank
            _ghostEngine.Update(_replayTimer, _ghost);
            _ghost.UpdatePhysics(dt, minX: 50f, maxX: ScreenWidth - 50f);

            // อากาศยานศัตรูเล็งโจมตีใส่ GhostTank
            _waveDirector.Update(dt, enemy =>
            {
                enemy.OnSpawnEnemyAttack = (pos, vel, attackType) =>
                {
                    _bombs.Add(new Bomb(pos, vel, attackType));
                };
                _enemies.Add(enemy);
            });

            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                _enemies[i].Update(dt, _ghost.Position);
                if (!_enemies[i].IsActive)
                {
                    _enemies.RemoveAt(i);
                }
            }

            // อัปเดตกระสุนและลูกระเบิด
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                _projectiles[i].Update(dt);
                if (!_projectiles[i].IsActive) _projectiles.RemoveAt(i);
            }

            for (int i = _bombs.Count - 1; i >= 0; i--)
            {
                _bombs[i].Update(dt, GroundLevelY);
                if (!_bombs[i].IsActive)
                {
                    _particles.SpawnExplosion(_bombs[i].Position, count: 16, maxSpeed: 120f);
                    _bombs.RemoveAt(i);
                }
            }

            // ตรวจสอบการชนกับ GhostTank (Player ไม่มีอยู่จริงใน Replay)
            int dummyScore = 0;
            CollisionManager.ProcessCollisions(
                _projectiles,
                _bombs,
                _enemies,
                _ghost,
                isReplayMode: true,
                _particles,
                ref dummyScore,
                _waveDirector,
                AddCommandLog
            );

            // เมื่อหมดเวลา Replay ของรอบล่าสุด ให้กลับสู่หน้าเดิม
            if (_replayTimer >= _lastRunDuration + 1.2f || (_ghostEngine.HasReachedEnd && _enemies.Count == 0))
            {
                ExitReplayTheater();
            }
            return;
        }

        // ------------------------------------------------------------------
        // 5. โหมด GAME OVER: เลือกเริ่มใหม่ ดู Replay หรือกลับสู่เมนูหลัก
        // ------------------------------------------------------------------
        if (_state == GameState.GameOver)
        {
            _gameOverTimer += dt;

            // ปุ่ม [P]: เข้าดู Replay รอบล่าสุด
            if (Raylib.IsKeyPressed(KeyboardKey.P))
            {
                StartReplayTheater();
                return;
            }

            // ปุ่ม [M] หรือ [ESC]: กลับสู่เมนูหลัก (Main Menu)
            if (Raylib.IsKeyPressed(KeyboardKey.M) || Raylib.IsKeyPressed(KeyboardKey.Escape))
            {
                _state = GameState.MainMenu;
                return;
            }

            // ปุ่ม [Space] หรือ [Enter] หรือ [R]: เริ่มเล่นรอบใหม่
            if (_gameOverTimer >= 0.5f && (Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.R)))
            {
                StartNewSurvivalRun();
            }
            return;
        }

        // ------------------------------------------------------------------
        // 6. โหมดเล่นสด (PLAYING - CONTINUOUS SURVIVAL)
        // ------------------------------------------------------------------
        // ปุ่ม [ESC] หรือ [M]: พักเกมและกลับสู่เมนูหลัก
        if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsKeyPressed(KeyboardKey.M))
        {
            _state = GameState.MainMenu;
            return;
        }

        // ปุ่มลัด Reset Run (R) ขณะเล่น
        if (Raylib.IsKeyPressed(KeyboardKey.R))
        {
            StartNewSurvivalRun();
            return;
        }

        _inputHandler.Update(dt);
        _survivalTime += dt;

        // Invoker: ฮาร์ดแวร์ -> InputHandler -> Player Command Execution
        ICommand? moveCmd = _inputHandler.CheckHorizontalMovement();
        if (moveCmd != null)
        {
            moveCmd.Execute(_player);
            _recorder.Record(_survivalTime, moveCmd);
            AddCommandLog("Player", $"[{_survivalTime:00.00}s] {moveCmd}");
        }

        ICommand? aimCmd = _inputHandler.CheckTurretAim(_player.TurretPivot);
        if (aimCmd != null)
        {
            aimCmd.Execute(_player);
            _recorder.Record(_survivalTime, aimCmd);
        }

        ICommand? fireCmd = _inputHandler.CheckFire();
        if (fireCmd != null)
        {
            fireCmd.Execute(_player);
            _recorder.Record(_survivalTime, fireCmd);
            AddCommandLog("Player", $"[{_survivalTime:00.00}s] {fireCmd}");
        }

        // ฟิสิกส์การเคลื่อนที่ของรถถังผู้เล่นจริง
        _player.UpdatePhysics(dt, minX: 50f, maxX: ScreenWidth - 50f);

        // สปอว์นอากาศยานศัตรูต่อเนื่องตามเวลาที่รอดชีวิต
        _waveDirector.Update(dt, enemy =>
        {
            enemy.OnSpawnEnemyAttack = (pos, vel, attackType) =>
            {
                _bombs.Add(new Bomb(pos, vel, attackType));
            };
            _enemies.Add(enemy);

            if (enemy.IsBoss)
            {
                AddCommandLog("Alert", "★ DREADNOUGHT AIR FORTRESS INCOMING! ★");
            }
        });

        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            _enemies[i].Update(dt, _player.Position);
            if (!_enemies[i].IsActive)
            {
                _enemies.RemoveAt(i);
            }
        }

        // สปอว์นกล่องเสบียงชูชีพ (+35 HP)
        _supplyDropTimer -= dt;
        if (_supplyDropTimer <= 0f)
        {
            _supplyDropTimer = 14f;
            SupplyType type = SupplyType.ArmorRepair;
            float dropX = 120f + (float)Random.Shared.NextDouble() * 1040f;
            _supplyDrops.Add(new SupplyDrop(new Vector2(dropX, 40f), type));
            AddCommandLog("HQ", "Allied Supply Crate airdropped!");
        }

        for (int i = _supplyDrops.Count - 1; i >= 0; i--)
        {
            _supplyDrops[i].Update(dt, GroundLevelY);
            if (!_supplyDrops[i].IsActive) _supplyDrops.RemoveAt(i);
        }

        CollisionManager.ProcessSupplyPickups(_supplyDrops, _player, _particles, AddCommandLog);

        // อัปเดตกระสุนและระเบิด
        for (int i = _projectiles.Count - 1; i >= 0; i--)
        {
            _projectiles[i].Update(dt);
            if (!_projectiles[i].IsActive) _projectiles.RemoveAt(i);
        }

        for (int i = _bombs.Count - 1; i >= 0; i--)
        {
            _bombs[i].Update(dt, GroundLevelY);
            if (!_bombs[i].IsActive)
            {
                _particles.SpawnExplosion(_bombs[i].Position, count: 18, maxSpeed: 130f);
                _bombs.RemoveAt(i);
            }
        }

        // ตรวจจับการชน
        CollisionManager.ProcessCollisions(
            _projectiles,
            _bombs,
            _enemies,
            _player,
            isReplayMode: false,
            _particles,
            ref _score,
            _waveDirector,
            AddCommandLog
        );

        // ตรวจสอบเงื่อนไขจบเกม (รถถังถูกทำลาย)
        if (!_player.IsAlive)
        {
            _state = GameState.GameOver;
            _screenShakeTimer = 0.5f;
            _recorder.StopRecording();

            // บันทึกเทปรอบนี้เก็บไว้สำหรับ Replay ทันที
            _lastRunTape.Clear();
            _lastRunTape.AddRange(_recorder.GetTapeCopy());
            _lastRunDuration = _survivalTime;
            _lastRunScore = _score;
            _lastRunKills = _waveDirector.EnemiesKilled;
            _lastRunSeed = _currentRunSeed;

            // บันทึกสถิติ Personal Best
            if (_score > _bestScore)
            {
                _bestScore = _score;
                _bestSurvivalTime = _survivalTime;
                AddCommandLog("System", $"★ NEW PERSONAL BEST! Score: {_bestScore:N0} ★");
            }

            AddCommandLog("System", "MISSION TERMINATED! Press [P] for Replay | [Space] to Play Again");
        }
    }

    private void ExecuteMenuSelection()
    {
        switch (_menuSelectedIndex)
        {
            case 0: // Start Mission
                StartNewSurvivalRun();
                break;
            case 1: // How to Play
                _state = GameState.HowToPlay;
                break;
            case 2: // View Replay
                if (_lastRunTape.Count > 0)
                {
                    StartReplayTheater();
                }
                break;
            case 3: // Exit
                Raylib.CloseWindow();
                break;
        }
    }

    private const int MenuBtnWidth = 380;
    private const int MenuBtnHeight = 50;
    private const int MenuBtnStartY = 270;
    private const int MenuBtnSpacing = 64;

    private static Rectangle GetMenuButtonRect(int index)
    {
        int startX = ScreenWidth / 2 - MenuBtnWidth / 2;
        return new Rectangle(startX, MenuBtnStartY + index * MenuBtnSpacing, MenuBtnWidth, MenuBtnHeight);
    }

    private void CheckMenuMouseClicks()
    {
        Vector2 mouse = Raylib.GetMousePosition();

        for (int i = 0; i < 4; i++)
        {
            Rectangle rect = GetMenuButtonRect(i);
            if (Raylib.CheckCollisionPointRec(mouse, rect))
            {
                _menuSelectedIndex = i;
                if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    ExecuteMenuSelection();
                }
            }
        }
    }

    private void AddCommandLog(string source, string message)
    {
        _commandLogQueue.Enqueue($"[{source}] {message}");
        if (_commandLogQueue.Count > 6)
        {
            _commandLogQueue.Dequeue();
        }
    }

    private void Draw()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(12, 16, 28, 255));

        switch (_state)
        {
            case GameState.LoadingScreen:
                DrawLoadingScreen();
                break;

            case GameState.MainMenu:
                DrawMainMenu();
                break;

            case GameState.HowToPlay:
                DrawHowToPlay();
                break;

            case GameState.Playing:
            case GameState.GameOver:
            case GameState.ReplayTheater:
                DrawGameplayWorld();
                DrawHUD();
                break;
        }

        Raylib.EndDrawing();
    }

    private void DrawLoadingScreen()
    {
        Raylib.ClearBackground(new Color(10, 14, 22, 255));

        // กล่องคอนโซลตรงกลาง
        int boxW = 620;
        int boxH = 260;
        int boxX = ScreenWidth / 2 - boxW / 2;
        int boxY = ScreenHeight / 2 - boxH / 2;

        Raylib.DrawRectangle(boxX, boxY, boxW, boxH, new Color(16, 24, 38, 230));
        Raylib.DrawRectangleLines(boxX, boxY, boxW, boxH, new Color(60, 90, 130, 255));

        Raylib.DrawText("HEAVY WEAPONS: SYSTEM BOOTSTRAP", boxX + 30, boxY + 28, 20, Color.Gold);
        Raylib.DrawText("GI161 Object Oriented Programming for Games | Bangkok University", boxX + 30, boxY + 54, 13, Color.Gray);

        // ข้อความสถานะการโหลด
        string statusText = _loadingProgress switch
        {
            < 0.25f => "[01/04] Initializing Raylib-cs 2D Graphic Engine...",
            < 0.55f => "[02/04] Compiling GoF Command Pattern (ICommand, Receivers)...",
            < 0.80f => "[03/04] Calibrating Threat Vectoring & Airspace Radar...",
            < 0.98f => "[04/04] Arming 120mm Armor-Piercing & Atomic Warheads...",
            _ => "[SUCCESS] All Systems Nominal. Press [SPACE] to Enter HQ!"
        };

        Color statusColor = _loadingProgress >= 0.98f ? Color.Green : Color.SkyBlue;
        Raylib.DrawText(statusText, boxX + 30, boxY + 115, 14, statusColor);

        // หลอด Progress Bar
        int barW = boxW - 60;
        int barH = 24;
        Raylib.DrawRectangle(boxX + 30, boxY + 145, barW, barH, new Color(25, 35, 50, 255));
        Raylib.DrawRectangleLines(boxX + 30, boxY + 145, barW, barH, Color.DarkGray);

        int fillW = (int)(barW * Math.Clamp(_loadingProgress, 0f, 1f));
        Raylib.DrawRectangle(boxX + 30, boxY + 145, fillW, barH, Color.Orange);

        int percent = (int)(_loadingProgress * 100);
        string pctStr = $"{percent}%";
        Raylib.DrawText(pctStr, boxX + boxW / 2 - Raylib.MeasureText(pctStr, 14) / 2, boxY + 150, 14, Color.White);

        Raylib.DrawText("PRESS [SPACE] OR [ENTER] TO SKIP", boxX + boxW / 2 - 130, boxY + 215, 13, Color.LightGray);
    }

    private void DrawMainMenu()
    {
        // 1. วาดฉากหลังพร้อมไฟค้นหาท้องฟ้า (Searchlights)
        DrawBattlefieldBackdrop();
        DrawSearchlights();

        // 2. โลโก้ชื่อเกมขนาดใหญ่ (Metallic Gold Title Banner)
        string title = "HEAVY WEAPONS";
        int titleSize = 62;
        int titleW = Raylib.MeasureText(title, titleSize);
        int titleX = ScreenWidth / 2 - titleW / 2;
        int titleY = 110;

        // เงาตัวอักษร
        Raylib.DrawText(title, titleX + 4, titleY + 4, titleSize, new Color(0, 0, 0, 180));
        Raylib.DrawText(title, titleX, titleY, titleSize, Color.Gold);

        string subtitle = "ENDLESS AIR RAID SURVIVAL & COMMAND REPLAY";
        int subSize = 18;
        int subW = Raylib.MeasureText(subtitle, subSize);
        Raylib.DrawText(subtitle, ScreenWidth / 2 - subW / 2, titleY + 70, subSize, Color.SkyBlue);

        // 3. ปุ่มเมนู 4 ตัวเลือก
        string[] menuLabels = {
            "1. START MISSION",
            "2. HOW TO PLAY & INTEL",
            _lastRunTape.Count > 0 ? "3. WATCH LAST REPLAY THEATER" : "3. REPLAY THEATER (NO RUN)",
            "4. EXIT TO DESKTOP"
        };

        for (int i = 0; i < 4; i++)
        {
            bool isSelected = (_menuSelectedIndex == i);
            Rectangle rect = GetMenuButtonRect(i);

            Color bg = isSelected ? new Color(35, 60, 95, 235) : new Color(20, 28, 42, 210);
            Color border = isSelected ? Color.Gold : new Color(60, 80, 110, 255);
            Color textCol = isSelected ? Color.Yellow : Color.White;

            if (i == 2 && _lastRunTape.Count == 0)
            {
                textCol = Color.DarkGray;
            }

            Raylib.DrawRectangleRec(rect, bg);
            Raylib.DrawRectangleLinesEx(rect, isSelected ? 2.5f : 1.2f, border);

            int textW = Raylib.MeasureText(menuLabels[i], 18);
            Raylib.DrawText(menuLabels[i], (int)rect.X + (int)rect.Width / 2 - textW / 2, (int)rect.Y + 16, 18, textCol);
        }

        // 4. แถบสถิติคะแนนสูงสุด (Personal Best Card) ด้านล่าง
        Raylib.DrawRectangle(ScreenWidth / 2 - 280, 560, 560, 60, new Color(15, 22, 34, 220));
        Raylib.DrawRectangleLines(ScreenWidth / 2 - 280, 560, 560, 60, new Color(50, 75, 105, 255));

        int min = (int)(_bestSurvivalTime / 60);
        float sec = _bestSurvivalTime % 60;
        Raylib.DrawText($"★ HIGH SCORE RECORD: {_bestScore:N0} PTS", ScreenWidth / 2 - 250, 574, 16, Color.Gold);
        Raylib.DrawText($"LONGEST SURVIVAL: {min:00}:{sec:00.0}", ScreenWidth / 2 + 50, 574, 16, Color.SkyBlue);
        Raylib.DrawText("GI161 OOP for Games | Bangkok University", ScreenWidth / 2 - 130, 600, 12, Color.Gray);

        // 5. คำแนะนำการควบคุมเมนูด้านล่างสุด
        Raylib.DrawText("[UP/DOWN] or [1-4] Select | [ENTER] / [SPACE] Confirm | [ESC] Exit", ScreenWidth / 2 - 230, ScreenHeight - 34, 14, Color.LightGray);
    }

    private void DrawSearchlights()
    {
        // ลำแสงไฟส่องท้องฟ้าเคลื่อนไหวแบบ Sine/Cosine
        float light1X = ScreenWidth * 0.25f;
        float light2X = ScreenWidth * 0.75f;

        Vector2 origin1 = new(light1X, GroundLevelY);
        Vector2 tip1 = new(light1X + MathF.Sin(_searchlightAngle1) * 320f, 60f);

        Vector2 origin2 = new(light2X, GroundLevelY);
        Vector2 tip2 = new(light2X + MathF.Sin(_searchlightAngle2) * 320f, 60f);

        Color beamColor = new(255, 255, 200, 22);
        Raylib.DrawTriangle(origin1, new Vector2(tip1.X - 50, tip1.Y), new Vector2(tip1.X + 50, tip1.Y), beamColor);
        Raylib.DrawTriangle(origin2, new Vector2(tip2.X - 50, tip2.Y), new Vector2(tip2.X + 50, tip2.Y), beamColor);
    }

    private void DrawHowToPlay()
    {
        Raylib.ClearBackground(new Color(12, 16, 26, 255));
        DrawBattlefieldBackdrop();

        // กรอบคู่มือตรงกลาง
        int cardW = 960;
        int cardH = 560;
        int cardX = ScreenWidth / 2 - cardW / 2;
        int cardY = 50;

        Raylib.DrawRectangle(cardX, cardY, cardW, cardH, new Color(16, 22, 34, 240));
        Raylib.DrawRectangleLines(cardX, cardY, cardW, cardH, Color.Gold);

        Raylib.DrawText("MISSION INTEL & COMBAT CONTROLS", cardX + 35, cardY + 28, 24, Color.Gold);
        Raylib.DrawText("Heavy Weapons Operation Manual | Command Pattern Architecture", cardX + 35, cardY + 56, 13, Color.Gray);

        // ส่วนที่ 1: การควบคุมรถถัง
        Raylib.DrawText("1. TANK CONTROLS (PLAYER RECEIVER)", cardX + 35, cardY + 100, 18, Color.SkyBlue);
        Raylib.DrawText("• [A / D] or [Left / Right]: Drive Tank across ground trench", cardX + 50, cardY + 130, 15, Color.White);
        Raylib.DrawText("• [Mouse Cursor]: Aim cannon turret with 360 upward arc", cardX + 50, cardY + 155, 15, Color.White);
        Raylib.DrawText("• [Spacebar / Left-Click]: Fire 120mm High-Explosive Shells", cardX + 50, cardY + 180, 15, Color.White);
        Raylib.DrawText("• [Right-Click / B / F]: Deploy Atomic Warhead (Wipes active airspace!)", cardX + 50, cardY + 205, 15, Color.Orange);

        // ส่วนที่ 2: ฝูงบินศัตรู
        Raylib.DrawText("2. ENEMY AIRCRAFT THREATS (POLYMORPHIC ENEMIES)", cardX + 35, cardY + 250, 18, Color.SkyBlue);
        Raylib.DrawText("• Fighter Jet: High speed with Delta wings, fires twin laser machine guns (12 DMG)", cardX + 50, cardY + 280, 15, Color.White);
        Raylib.DrawText("• Heavy Bomber: Strategic payload carrier, drops devastating gravity bombs (35 DMG!)", cardX + 50, cardY + 305, 15, Color.Yellow);
        Raylib.DrawText("• Gunship Heli: Undulating flight pattern, locks and fires guided rockets (24 DMG)", cardX + 50, cardY + 330, 15, Color.White);
        Raylib.DrawText("• Dreadnought Air Fortress: Massive aerial boss gunship arriving every 50 seconds", cardX + 50, cardY + 355, 15, Color.Red);

        // ส่วนที่ 3: เสบียงและ Replay
        Raylib.DrawText("3. SUPPLY CRATES & REPLAY THEATER", cardX + 35, cardY + 400, 18, Color.SkyBlue);
        Raylib.DrawText("• Allied Parachutes: Grab Green crates to restore +35 HP!", cardX + 50, cardY + 430, 15, Color.Lime);
        Raylib.DrawText("• Replay Theater: When destroyed, press [P] to watch spectator replay of your run!", cardX + 50, cardY + 455, 15, Color.Gold);

        // ปุ่มย้อนกลับ
        Raylib.DrawRectangle(cardX + cardW / 2 - 160, cardY + cardH - 58, 320, 40, new Color(30, 50, 80, 230));
        Raylib.DrawRectangleLines(cardX + cardW / 2 - 160, cardY + cardH - 58, 320, 40, Color.Gold);
        Raylib.DrawText("PRESS [ESC] OR [SPACE] TO RETURN", cardX + cardW / 2 - 145, cardY + cardH - 45, 15, Color.White);
    }

    private void DrawGameplayWorld()
    {
        // 1. วาดฉากหลัง
        DrawBattlefieldBackdrop();

        // 2. วาดตัวละครรถถัง
        if (_state == GameState.ReplayTheater)
        {
            // ในโหมด Replay: ไม่วาด Player! วาดเฉพาะ Replay Tank
            _ghost.Draw();
        }
        else
        {
            // ในโหมดปกติ: วาดเฉพาะรถถังผู้เล่นจริงตัวเดียวเท่านั้น!
            _player.Draw();
        }

        // 3. วาดกล่องสัมภาระชูชีพ
        foreach (var crate in _supplyDrops)
        {
            crate.Draw();
        }

        // 4. วาดฝูงบินศัตรู
        foreach (var enemy in _enemies)
        {
            enemy.Draw();
        }

        // 5. วาดลูกระเบิดและกระสุนปืนใหญ่
        foreach (var bomb in _bombs)
        {
            bomb.Draw();
        }
        foreach (var proj in _projectiles)
        {
            proj.Draw();
        }

        // 6. วาดอนุภาคการระเบิด
        _particles.Draw();

        // 7. วาดเป้าเล็ง (เฉพาะโหมดเล่นสด)
        if (_state == GameState.Playing)
        {
            DrawAimCrosshair();
        }
    }

    private void DrawBattlefieldBackdrop()
    {
        Raylib.DrawRectangleGradientV(0, 0, ScreenWidth, (int)GroundLevelY, new Color(10, 14, 26, 255), new Color(25, 35, 60, 255));

        Raylib.DrawTriangle(new Vector2(0, GroundLevelY), new Vector2(240, 480), new Vector2(500, GroundLevelY), new Color(18, 24, 40, 255));
        Raylib.DrawTriangle(new Vector2(380, GroundLevelY), new Vector2(700, 430), new Vector2(1040, GroundLevelY), new Color(22, 28, 48, 255));
        Raylib.DrawTriangle(new Vector2(900, GroundLevelY), new Vector2(1150, 490), new Vector2(1280, GroundLevelY), new Color(18, 24, 40, 255));

        Raylib.DrawRectangle(140, (int)GroundLevelY - 45, 60, 45, new Color(32, 42, 54, 255));
        Raylib.DrawLineEx(new Vector2(170, GroundLevelY - 45), new Vector2(170, GroundLevelY - 80), 3f, Color.DarkGray);
        Raylib.DrawCircleV(new Vector2(170, GroundLevelY - 80), 8f, Color.Red);

        Raylib.DrawRectangle(0, (int)GroundLevelY, ScreenWidth, ScreenHeight - (int)GroundLevelY, new Color(42, 48, 40, 255));
        Raylib.DrawLineEx(new Vector2(0, GroundLevelY), new Vector2(ScreenWidth, GroundLevelY), 4f, new Color(110, 130, 95, 255));

        for (int x = 0; x < ScreenWidth; x += 60)
        {
            Raylib.DrawLineEx(new Vector2(x, GroundLevelY + 4), new Vector2(x + 30, GroundLevelY + 22), 3f, new Color(75, 80, 50, 160));
        }
    }

    private void DrawAimCrosshair()
    {
        Vector2 mouse = Raylib.GetMousePosition();
        if (mouse.Y < GroundLevelY)
        {
            Raylib.DrawCircleLines((int)mouse.X, (int)mouse.Y, 12f, Color.Green);
            Raylib.DrawCircleLines((int)mouse.X, (int)mouse.Y, 4f, Color.Gold);
            Raylib.DrawLineEx(new Vector2(mouse.X - 16, mouse.Y), new Vector2(mouse.X + 16, mouse.Y), 1.5f, Color.Green);
            Raylib.DrawLineEx(new Vector2(mouse.X, mouse.Y - 16), new Vector2(mouse.X, mouse.Y + 16), 1.5f, Color.Green);
        }
    }

    private void DrawHUD()
    {
        // -------------------------------------------------------------
        // หากอยู่ในโหมด REPLAY THEATER: แสดงแถบควบคุม Replay ด้านบนจอ
        // -------------------------------------------------------------
        if (_state == GameState.ReplayTheater)
        {
            Raylib.DrawRectangle(0, 0, ScreenWidth, 54, new Color(10, 20, 36, 230));
            Raylib.DrawLineEx(new Vector2(0, 54), new Vector2(ScreenWidth, 54), 2f, Color.Gold);

            string title = $"🎬 REPLAY THEATER - PLAYING BACK LAST SURVIVAL RUN ({_lastRunTape.Count} COMMANDS)";
            Raylib.DrawText(title, 24, 16, 18, Color.Gold);

            string progress = $"TIME: {_replayTimer:00.0}s / {_lastRunDuration:00.0}s";
            Raylib.DrawText(progress, 680, 18, 16, Color.SkyBlue);

            string exitPrompt = "Press [P], [SPACE], or [ESC] to Exit Replay";
            Raylib.DrawText(exitPrompt, ScreenWidth - 360, 18, 14, Color.Yellow);

            // หลอดแสดง Timeline Progress Bar
            float progressRatio = _lastRunDuration > 0 ? Math.Clamp(_replayTimer / _lastRunDuration, 0f, 1f) : 0f;
            Raylib.DrawRectangle(0, 52, (int)(ScreenWidth * progressRatio), 4, Color.Orange);
            return;
        }

        // -------------------------------------------------------------
        // แถบข้อมูลด้านบนซ้าย: Score, Time, Threat, PB
        // -------------------------------------------------------------
        Raylib.DrawRectangle(16, 16, 400, 110, new Color(15, 22, 36, 210));
        Raylib.DrawRectangleLines(16, 16, 400, 110, new Color(70, 90, 120, 255));

        int minutes = (int)(_survivalTime / 60f);
        float seconds = _survivalTime % 60f;
        string timeStr = $"SURVIVAL: {minutes:00}:{seconds:00.0}";
        Raylib.DrawText(timeStr, 30, 26, 21, Color.Gold);

        Raylib.DrawText($"SCORE: {_score:N0}", 30, 54, 20, Color.White);
        Raylib.DrawText($"BEST SCORE: {_bestScore:N0}", 30, 78, 16, new Color(100, 220, 255, 255));
        Raylib.DrawText($"THREAT: LEVEL {_waveDirector.ThreatLevel}", 235, 28, 16, Color.Orange);
        Raylib.DrawText($"KILLS: {_waveDirector.EnemiesKilled}", 235, 78, 16, Color.LightGray);

        // -------------------------------------------------------------
        // กล่องแสดง Real-time Command Stream Log ด้านบนขวา
        // -------------------------------------------------------------
        Raylib.DrawRectangle(ScreenWidth - 490, 16, 474, 130, new Color(15, 22, 36, 220));
        Raylib.DrawRectangleLines(ScreenWidth - 490, 16, 474, 130, new Color(70, 90, 120, 255));

        Raylib.DrawText("GOF COMMAND STREAM RECORDER", ScreenWidth - 478, 24, 13, Color.SkyBlue);
        Raylib.DrawText($"Tape Size: {_recorder.CommandCount} cmds | Last Run: {_lastRunTape.Count} cmds", ScreenWidth - 478, 40, 11, Color.Gray);

        int logY = 56;
        foreach (var log in _commandLogQueue)
        {
            Color logCol = log.Contains("Player") ? Color.Green : (log.Contains("Alert") ? Color.Red : Color.Gold);
            Raylib.DrawText(log, ScreenWidth - 478, logY, 11, logCol);
            logY += 14;
        }

        // -------------------------------------------------------------
        // แถบพลังชีวิตผู้เล่นด้านล่างซ้าย
        // -------------------------------------------------------------
        Raylib.DrawRectangle(16, ScreenHeight - 56, 420, 42, new Color(15, 22, 36, 220));
        Raylib.DrawRectangleLines(16, ScreenHeight - 56, 420, 42, new Color(70, 90, 120, 255));

        Raylib.DrawText("ARMOR", 24, ScreenHeight - 50, 13, Color.LightGray);
        float hpRatio = (float)_player.Health / _player.MaxHealth;
        Raylib.DrawRectangle(80, ScreenHeight - 48, 180, 18, Color.DarkGray);
        Color barColor = hpRatio > 0.5f ? Color.Green : (hpRatio > 0.25f ? Color.Yellow : Color.Red);
        Raylib.DrawRectangle(80, ScreenHeight - 48, (int)(180 * hpRatio), 18, barColor);
        Raylib.DrawText($"{_player.Health} / {_player.MaxHealth}", 140, ScreenHeight - 46, 12, Color.Black);

        // -------------------------------------------------------------
        // คู่มือปุ่มควบคุมด้านล่างขวา
        // -------------------------------------------------------------
        Raylib.DrawText("[A/D] Move | [Mouse] Aim | [Space/L-Click] Fire | [ESC] Menu", ScreenWidth - 520, ScreenHeight - 34, 13, Color.RayWhite);

        // -------------------------------------------------------------
        // ป้ายแสดงสถานะ Game Over
        // -------------------------------------------------------------
        if (_state == GameState.GameOver)
        {
            Raylib.DrawRectangle(0, ScreenHeight / 2 - 110, ScreenWidth, 220, new Color(30, 10, 10, 245));
            Raylib.DrawRectangleLines(0, ScreenHeight / 2 - 110, ScreenWidth, 220, Color.Red);

            string title = "MISSION TERMINATED - TANK DESTROYED";
            Raylib.DrawText(title, ScreenWidth / 2 - Raylib.MeasureText(title, 36) / 2, ScreenHeight / 2 - 85, 36, Color.Red);

            string sub1 = $"Time Survived: {_lastRunDuration:00.0}s | Hostiles Neutralized: {_lastRunKills} aircraft";
            Raylib.DrawText(sub1, ScreenWidth / 2 - Raylib.MeasureText(sub1, 20) / 2, ScreenHeight / 2 - 35, 20, Color.RayWhite);

            string sub2 = $"Final Score: {_lastRunScore:N0} pts | Recorded Commands: {_lastRunTape.Count} cmds";
            Raylib.DrawText(sub2, ScreenWidth / 2 - Raylib.MeasureText(sub2, 18) / 2, ScreenHeight / 2 - 5, 18, Color.SkyBlue);

            string prompt = "[P] WATCH REPLAY | [SPACE] PLAY AGAIN | [M] / [ESC] MAIN MENU";
            Raylib.DrawText(prompt, ScreenWidth / 2 - Raylib.MeasureText(prompt, 18) / 2, ScreenHeight / 2 + 45, 18, Color.Gold);
        }
    }
}
