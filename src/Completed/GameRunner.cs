using System.Numerics;
using Raylib_cs;
using Game_CommandPattern_Completed.Core.Characters;
using Game_CommandPattern_Completed.Core.Commands;
using Game_CommandPattern_Completed.Core.Replay;

namespace Game_CommandPattern_Completed;

/// <summary>
/// ตัวควบคุมเกมหลัก (Game Runner / Coordinator)
/// ทำหน้าที่บริหารจัดการ Game Loop, การเรนเดอร์ฉาก 2D Platformer,
/// กล้อง 2D ติดตามตัวละคร, และผสานกลไก Command Pattern เข้ากับ Ghost Replay Engine
/// </summary>
public class GameRunner
{
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;
    private const float LevelLength = 2550f;
    private static readonly Vector2 StartPosition = new(80f, 440f);

    // ตัวละคร
    private MarioPlayer _player = null!;
    private GhostRunner _ghost = null!;

    // เครื่องยนต์ Command & Replay
    private readonly InputHandler _inputHandler = new();
    private readonly CommandRecorder _recorder = new();
    private readonly GhostPlaybackEngine _ghostEngine = new();

    // ทรัพยากรภาพ (Textures)
    private Texture2D _texCharacter;
    private Texture2D _texTiles;
    private Texture2D _texPortal;
    private Texture2D _texBgSky;
    private Texture2D _texBgMountains;
    private Texture2D _texBgTrees;

    // กล้องและฉาก
    private Camera2D _camera;
    private readonly List<Rectangle> _platforms = new();
    private readonly Rectangle _portalRect = new(2340f, 416f, 64f, 64f);

    // เวลาและสถานะเกม
    private float _levelTimer = 0f;
    private float? _bestTime = null;
    private bool _hasFinished = false;
    private float _finishStateTimer = 0f;
    private bool _isNewRecord = false;

    // อนิเมชัน Portal
    private float _portalAnimTimer = 0f;
    private int _portalCurrentFrame = 0;

    // รายการบันทึก Command Stream ล่าสุดสำหรับแสดงบน HUD
    private readonly Queue<string> _commandLogQueue = new();

    public void Run()
    {
        Raylib.InitWindow(ScreenWidth, ScreenHeight, "Week 8: Command Pattern & Ghost Replay Engine | Bangkok University");
        Raylib.SetTargetFPS(60);

        LoadAssets();
        BuildLevelGeometry();
        ResetRace();

        while (!Raylib.WindowShouldClose())
        {
            float dt = Raylib.GetFrameTime();
            Update(dt);
            Draw();
        }

        UnloadAssets();
        Raylib.CloseWindow();
    }

    private void LoadAssets()
    {
        string assetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
        
        _texCharacter = Raylib.LoadTexture(Path.Combine(assetsDir, "sprites", "character.png"));
        _texPortal = Raylib.LoadTexture(Path.Combine(assetsDir, "sprites", "portal.png"));
        _texTiles = Raylib.LoadTexture(Path.Combine(assetsDir, "tiles", "floor_tiles.png"));
        _texBgSky = Raylib.LoadTexture(Path.Combine(assetsDir, "backgrounds", "bg_sky.png"));
        _texBgMountains = Raylib.LoadTexture(Path.Combine(assetsDir, "backgrounds", "bg_mountains.png"));
        _texBgTrees = Raylib.LoadTexture(Path.Combine(assetsDir, "backgrounds", "bg_trees.png"));

        // ตั้งค่า Point Filter ให้พิกเซลคมชัดสไตล์ Retro 2D
        Raylib.SetTextureFilter(_texCharacter, TextureFilter.Point);
        Raylib.SetTextureFilter(_texTiles, TextureFilter.Point);
        Raylib.SetTextureFilter(_texPortal, TextureFilter.Point);
        Raylib.SetTextureFilter(_texBgSky, TextureFilter.Point);
        Raylib.SetTextureFilter(_texBgMountains, TextureFilter.Point);
        Raylib.SetTextureFilter(_texBgTrees, TextureFilter.Point);

        _player = new MarioPlayer(StartPosition);
        _ghost = new GhostRunner(StartPosition);

        _camera = new Camera2D
        {
            Offset = new Vector2(ScreenWidth / 3f, ScreenHeight * 0.65f),
            Target = _player.Position,
            Rotation = 0f,
            Zoom = 2.0f
        };
    }

    private void BuildLevelGeometry()
    {
        _platforms.Clear();

        // 1. ลานเริ่มต้น (Start Platform)
        _platforms.Add(new Rectangle(0f, 480f, 600f, 120f));
        _platforms.Add(new Rectangle(320f, 390f, 130f, 26f)); // บล็อกลอยขั้นต่ำ

        // หลุมที่ 1 (Pit 1): x = 600 - 740

        // 2. ลานกลาง 1 (Mid Ground 1)
        _platforms.Add(new Rectangle(740f, 480f, 400f, 120f));
        _platforms.Add(new Rectangle(860f, 400f, 96f, 24f));  // บันไดขั้น 1
        _platforms.Add(new Rectangle(1000f, 320f, 96f, 24f)); // บันไดขั้น 2

        // หลุมที่ 2 (Pit 2): x = 1140 - 1260

        // 3. สะพานกลางและแท่นสูง (High Bridge)
        _platforms.Add(new Rectangle(1260f, 480f, 420f, 120f));
        _platforms.Add(new Rectangle(1380f, 360f, 160f, 24f));

        // หลุมที่ 3 (Pit 3): x = 1680 - 1800

        // 4. เสาโดดเด่นกลางหุบเหว (Floating Pillar)
        _platforms.Add(new Rectangle(1800f, 420f, 140f, 180f));

        // หลุมที่ 4 (Pit 4): x = 1940 - 2040

        // 5. ลานเส้นชัยและ Goal Portal (Finish Arena)
        _platforms.Add(new Rectangle(2040f, 480f, 520f, 120f));

        // กำแพงกั้นซ้ายสุดและขวาสุด
        _platforms.Add(new Rectangle(-32f, 0f, 32f, 720f));
        _platforms.Add(new Rectangle(LevelLength, 0f, 32f, 720f));
    }

    private void ResetRace()
    {
        _levelTimer = 0f;
        _hasFinished = false;
        _finishStateTimer = 0f;
        _isNewRecord = false;

        _inputHandler.Reset();
        _recorder.StartRecording();

        _player.Reset(StartPosition);
        _ghostEngine.StartPlayback(_ghost, StartPosition);

        _commandLogQueue.Clear();
        AddCommandLog("System", "Run Initialized. Recorder ARMED.");
    }

    private void Update(float dt)
    {
        // อัปเดตเฟรมแอนิเมชัน Goal Portal
        _portalAnimTimer += dt;
        if (_portalAnimTimer >= 0.08f)
        {
            _portalAnimTimer = 0f;
            _portalCurrentFrame = (_portalCurrentFrame + 1) % 10;
        }

        // ปุ่มลัด Quick Restart (R)
        if (Raylib.IsKeyPressed(KeyboardKey.R) ||
            (Raylib.IsGamepadAvailable(0) && Raylib.IsGamepadButtonPressed(0, GamepadButton.RightFaceUp)))
        {
            ResetRace();
            return;
        }

        if (_hasFinished)
        {
            _finishStateTimer += dt;
            // หลังจากจบ 2.5 วินาที ให้ผู้เล่นกด Space หรือปุ่ม A เพื่อวิ่งรอบใหม่
            if (_finishStateTimer >= 1.5f && (Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.Enter) ||
                (Raylib.IsGamepadAvailable(0) && Raylib.IsGamepadButtonPressed(0, GamepadButton.RightFaceDown))))
            {
                ResetRace();
            }
            return;
        }

        _levelTimer += dt;

        // -------------------------------------------------------------
        // 1. Invoker A: ฮาร์ดแวร์ -> InputHandler -> Player Command Execution
        // -------------------------------------------------------------
        ICommand? moveCmd = _inputHandler.CheckHorizontalMovement();
        if (moveCmd != null)
        {
            moveCmd.Execute(_player);
            _recorder.Record(_levelTimer, moveCmd);
            AddCommandLog("Player", $"[{_levelTimer:00.00}s] {moveCmd}");
        }

        ICommand? jumpCmd = _inputHandler.CheckJump();
        if (jumpCmd != null)
        {
            jumpCmd.Execute(_player);
            _recorder.Record(_levelTimer, jumpCmd);
            AddCommandLog("Player", $"[{_levelTimer:00.00}s] {jumpCmd}");
        }

        // -------------------------------------------------------------
        // 2. Invoker B: Ghost Tape -> GhostPlaybackEngine -> Ghost Execution
        // -------------------------------------------------------------
        _ghostEngine.Update(_levelTimer, _ghost);

        // -------------------------------------------------------------
        // 3. อัปเดตฟิสิกส์และการชนของตัวละครทั้งสองตัว (Receivers)
        // -------------------------------------------------------------
        _player.UpdatePhysics(dt, _platforms);
        if (_ghost.IsActive)
        {
            _ghost.UpdatePhysics(dt, _platforms);
        }

        // ตรวจสอบการตกเหว (Fell into pit)
        if (_player.Position.Y > 620f)
        {
            AddCommandLog("System", $"[{_levelTimer:00.00}s] Fell into pit! Restarting...");
            ResetRace();
            return;
        }

        // ตรวจสอบการเข้าเส้นชัย (Goal Portal Reached)
        if (Raylib.CheckCollisionRecs(_player.Bounds, _portalRect))
        {
            OnFinishLineReached();
        }

        // -------------------------------------------------------------
        // 4. กล้องติดตามตัวละคร (Camera Smoothing Follow)
        // -------------------------------------------------------------
        float targetCamX = _player.Position.X;
        _camera.Target = new Vector2(
            MathF.Max(320f, MathF.Min(LevelLength - 320f, targetCamX)),
            380f
        );
    }

    private void OnFinishLineReached()
    {
        _hasFinished = true;
        _finishStateTimer = 0f;
        _recorder.StopRecording();
        _ghostEngine.StopPlayback(_ghost);
        _player.Stop();

        // ตรวจสอบ Personal Best
        if (!_bestTime.HasValue || _levelTimer < _bestTime.Value)
        {
            _isNewRecord = true;
            _bestTime = _levelTimer;
            _ghostEngine.LoadNewPersonalBest(_levelTimer, _recorder.GetTapeCopy());
            AddCommandLog("System", $"NEW PB! {_levelTimer:00.00}s Saved to Ghost Tape ({_recorder.CommandCount} cmds)");
        }
        else
        {
            _isNewRecord = false;
            AddCommandLog("System", $"Course Clear: {_levelTimer:00.00}s (PB: {_bestTime:00.00}s)");
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
        Raylib.ClearBackground(new Color(15, 23, 42, 255)); // สีกรมท่าลึก (Slate 900)

        // 1. วาด Parallax Background (Fixed Screen Coordinates with Camera Offset)
        DrawParallaxBackground();

        // 2. วาดวัตถุในโลกเกมผ่าน 2D Camera
        Raylib.BeginMode2D(_camera);
        {
            // วาดบล็อกพื้นและสิ่งกีดขวาง
            DrawLevelTiles();

            // วาด Goal Portal (Animated 64x64)
            DrawGoalPortal();

            // วาด Ghost Runner (ร่างเงาสีฟ้าใส)
            _ghost.Draw(_texCharacter);

            // วาด Live Player (ตัวละครสดสีเต็ม)
            _player.Draw(_texCharacter);
        }
        Raylib.EndMode2D();

        // 3. วาด HUD Overlay และสถานะระบบ Command Pattern
        DrawHUD();

        Raylib.EndDrawing();
    }

    private void DrawParallaxBackground()
    {
        float camX = _camera.Target.X;

        // เลเยอร์ท้องฟ้า (ไกลสุด - เคลื่อนที่ช้า 0.05x)
        float skyOffsetX = -(camX * 0.05f) % _texBgSky.Width;
        for (float x = skyOffsetX - _texBgSky.Width; x < ScreenWidth + _texBgSky.Width; x += _texBgSky.Width)
        {
            Raylib.DrawTextureEx(_texBgSky, new Vector2(x, 0), 0f, 3.0f, Color.White);
        }

        // เลเยอร์ภูเขา (กลาง - เคลื่อนที่ 0.2f)
        float mountainOffsetX = -(camX * 0.2f) % (_texBgMountains.Width * 2.5f);
        for (float x = mountainOffsetX - _texBgMountains.Width * 2.5f; x < ScreenWidth + _texBgMountains.Width * 2.5f; x += _texBgMountains.Width * 2.5f)
        {
            Raylib.DrawTextureEx(_texBgMountains, new Vector2(x, 100), 0f, 2.5f, new Color(220, 230, 255, 200));
        }

        // เลเยอร์ทิวไม้ (ใกล้ - เคลื่อนที่ 0.45f)
        float treesOffsetX = -(camX * 0.45f) % (_texBgTrees.Width * 2.5f);
        for (float x = treesOffsetX - _texBgTrees.Width * 2.5f; x < ScreenWidth + _texBgTrees.Width * 2.5f; x += _texBgTrees.Width * 2.5f)
        {
            Raylib.DrawTextureEx(_texBgTrees, new Vector2(x, 220), 0f, 2.5f, new Color(200, 220, 240, 230));
        }
    }

    private void DrawLevelTiles()
    {
        foreach (var plat in _platforms)
        {
            // หากเป็นกำแพงขอบฉากไม่ต้องวาด
            if (plat.X < 0 || plat.X >= LevelLength) continue;

            int tilesWide = (int)(plat.Width / 32f);
            int tilesHigh = (int)(plat.Height / 32f);

            for (int col = 0; col < tilesWide; col++)
            {
                for (int row = 0; row < tilesHigh; row++)
                {
                    float destX = plat.X + col * 32f;
                    float destY = plat.Y + row * 32f;

                    // เลือกชิ้นส่วนของกระเบื้องจาก Tileset
                    Rectangle srcRec;
                    if (row == 0)
                    {
                        // ผิวหญ้าด้านบน (Top Edge)
                        if (col == 0) srcRec = new Rectangle(0, 0, 32, 32);          // Top-Left corner
                        else if (col == tilesWide - 1) srcRec = new Rectangle(64, 0, 32, 32); // Top-Right corner
                        else srcRec = new Rectangle(32, 0, 32, 32);                  // Top-Center grass
                    }
                    else
                    {
                        // ชั้นดินด้านล่าง (Underground Dirt)
                        srcRec = new Rectangle(32, 32, 32, 32);
                    }

                    Raylib.DrawTextureRec(_texTiles, srcRec, new Vector2(destX, destY), Color.White);
                }
            }
        }
    }

    private void DrawGoalPortal()
    {
        Rectangle srcRec = new Rectangle(
            _portalCurrentFrame * 64,
            0,
            64,
            64
        );

        Raylib.DrawTextureRec(_texPortal, srcRec, new Vector2(_portalRect.X, _portalRect.Y), Color.White);

        // วาดแสงเปล่งประกายรอบประตู
        Raylib.DrawCircleLines((int)_portalRect.X + 32, (int)_portalRect.Y + 32, 36f, new Color(96, 165, 250, 160));
        Raylib.DrawText("GOAL", (int)_portalRect.X + 12, (int)_portalRect.Y - 14, 14, Color.Gold);
    }

    private void DrawHUD()
    {
        // 1. หัวข้อและแถบเวลา (Top Left Speedrun Panel)
        Raylib.DrawRectangleRounded(new Rectangle(20, 20, 380, 100), 0.2f, 8, new Color(15, 23, 42, 220));
        Raylib.DrawRectangleRoundedLines(new Rectangle(20, 20, 380, 100), 0.2f, 8, new Color(59, 130, 246, 180));

        // เวลาปัจจุบัน (Current Time)
        string timeStr = $"TIME: {_levelTimer:00.00}s";
        Raylib.DrawText(timeStr, 40, 32, 26, Color.RayWhite);

        // เวลาสถิติเดิม (Personal Best)
        string pbStr = _bestTime.HasValue ? $"PB: {_bestTime.Value:00.00}s" : "PB: --:--.--";
        Raylib.DrawText(pbStr, 40, 64, 18, Color.Gold);

        // ความต่างของเวลากับ Ghost (Delta)
        if (_ghostEngine.IsActive && _ghost.IsActive)
        {
            float deltaX = _player.Position.X - _ghost.Position.X;
            string deltaStr = deltaX >= 0 ? $"AHEAD: +{deltaX:0.0}px" : $"BEHIND: {deltaX:0.0}px";
            Color deltaColor = deltaX >= 0 ? Color.Green : Color.Red;
            Raylib.DrawText(deltaStr, 220, 64, 16, deltaColor);
        }

        // สถานะของ Ghost Tape
        string ghostStatus = _ghostEngine.HasTape 
            ? $"GHOST TAPE: {_ghostEngine.TotalCommands} COMMANDS" 
            : "GHOST TAPE: EMPTY (RUN TO RECORD)";
        Raylib.DrawText(ghostStatus, 40, 88, 12, new Color(148, 163, 184, 255));

        // 2. แผงแสดง Command Stream สด (Top Right Command Feed)
        Raylib.DrawRectangleRounded(new Rectangle(ScreenWidth - 420, 20, 400, 175), 0.15f, 8, new Color(15, 23, 42, 220));
        Raylib.DrawRectangleRoundedLines(new Rectangle(ScreenWidth - 420, 20, 400, 175), 0.15f, 8, new Color(168, 85, 247, 180));

        Raylib.DrawText("LIVE COMMAND STREAM", ScreenWidth - 400, 30, 15, Color.Magenta);
        Raylib.DrawText("Invoker -> Recorded Commands", ScreenWidth - 400, 48, 11, Color.LightGray);
        Raylib.DrawLine(ScreenWidth - 400, 64, ScreenWidth - 40, 64, new Color(168, 85, 247, 100));

        int logY = 72;
        foreach (var log in _commandLogQueue)
        {
            Color logCol = log.Contains("Jump") ? Color.Yellow : (log.Contains("Move") ? Color.SkyBlue : Color.Green);
            Raylib.DrawText(log, ScreenWidth - 400, logY, 12, logCol);
            logY += 16;
        }

        // 3. แถบคำแนะนำการควบคุม (Bottom Control Bar)
        Raylib.DrawRectangleRounded(new Rectangle(ScreenWidth / 2f - 290, ScreenHeight - 45, 580, 32), 0.4f, 8, new Color(15, 23, 42, 200));
        Raylib.DrawText("[A / D / ← →] Move  |  [Space / W / ↑] Jump  |  [R] Quick Reset", (int)(ScreenWidth / 2f - 260), ScreenHeight - 37, 14, Color.SkyBlue);

        // 4. แบนเนอร์ฉลองเมื่อเข้าเส้นชัย (Finish Line Banner)
        if (_hasFinished)
        {
            Raylib.DrawRectangle(0, ScreenHeight / 2 - 90, ScreenWidth, 180, new Color(15, 23, 42, 235));
            Raylib.DrawLine(0, ScreenHeight / 2 - 90, ScreenWidth, ScreenHeight / 2 - 90, Color.Gold);
            Raylib.DrawLine(0, ScreenHeight / 2 + 90, ScreenWidth, ScreenHeight / 2 + 90, Color.Gold);

            string title = _isNewRecord ? "★ NEW PERSONAL BEST! ★" : "COURSE CLEARED!";
            Color titleCol = _isNewRecord ? Color.Gold : Color.Green;
            int titleWidth = Raylib.MeasureText(title, 36);
            Raylib.DrawText(title, ScreenWidth / 2 - titleWidth / 2, ScreenHeight / 2 - 60, 36, titleCol);

            string detail = $"Final Time: {_levelTimer:00.00}s | Recorded: {_recorder.CommandCount} Commands";
            int detailWidth = Raylib.MeasureText(detail, 22);
            Raylib.DrawText(detail, ScreenWidth / 2 - detailWidth / 2, ScreenHeight / 2 - 10, 22, Color.RayWhite);

            string prompt = "Press [SPACE] or [R] to Run Again vs Ghost";
            int promptWidth = Raylib.MeasureText(prompt, 18);
            Raylib.DrawText(prompt, ScreenWidth / 2 - promptWidth / 2, ScreenHeight / 2 + 30, 18, Color.SkyBlue);
        }
    }

    private void UnloadAssets()
    {
        Raylib.UnloadTexture(_texCharacter);
        Raylib.UnloadTexture(_texPortal);
        Raylib.UnloadTexture(_texTiles);
        Raylib.UnloadTexture(_texBgSky);
        Raylib.UnloadTexture(_texBgMountains);
        Raylib.UnloadTexture(_texBgTrees);
    }
}

