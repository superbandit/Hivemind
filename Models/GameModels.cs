namespace Hivemind.Models;

// ── Environment (received once at startup) ──

public record GameEnvironment(MapSize MapSize, TankInfo[] Tanks);

public record MapSize(double Width, double Height);

public record TankInfo(int Id, int TeamId, string Name, string TeamName, bool IsEnemy, bool IsYou);

// ── State (received each step) ──

public record GameState(
    int Step,
    string? GameResult,
    TankState Tank,
    ScannedTank[] TankScans,
    DestroyedTank[] DestroyedTankScans,
    BulletScan[] BulletScans,
    PowerupScan[] PowerupScans,
    Hit[] Hits,
    ChatMessage[] ChatMessages);

public record TankState(
    Vec2 Location,
    double Heading,
    double TurretHeading,
    double Velocity,
    Gauge Health,
    Gauge GunEnergy,
    Gauge ChatEnergy);

public record Gauge(double Value, double Max);

public record ScannedTank(
    int TankId,
    string Name,
    Vec2 Location,
    double Heading,
    double TurretHeading,
    Gauge Health,
    bool IsEnemy);

public record DestroyedTank(int TankId, string Name, Vec2 Location, bool IsEnemy);

public record BulletScan(int BulletId, Vec2 Location, Vec2 Velocity);

public record PowerupScan(int Id, Vec2 Location, string Type);

public record Hit(int Damage, int TankId, string Name);

public record ChatMessage(int TankId, int TeamId, string Name, string Message);
