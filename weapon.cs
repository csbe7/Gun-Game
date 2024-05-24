using Godot;
using System;

public partial class weapon : Resource
{
    public enum WeaponType
    {
        meele,
        ranged,
    }
    [ExportCategory("Info")]
    [Export] public string name;
    [Export] WeaponType type;

    [ExportCategory("Visual Settings")]
    [Export] public Mesh mesh;
    [Export] public Vector3 rotation;
    [Export] public Vector3 position;
    [Export] public Vector3 scale;
    
    [ExportCategory("Gamepay Settings")]
    [Export] public PackedScene projectile;
    [Export] public float damage;
    [Export] public float delay;
    [Export] public int ammo;
    [Export] public int currAmmo;
    [Export] public float reloadTime;
    [Export] public float drawTime;
    [Export] public float weight;

    [ExportCategory("IK")]
    [Export] public Vector3 leftArm_target_rotation;
    [Export] public Vector3 leftArm_target_position;

    [ExportCategory("Recoil")]
    [Export] public float kickback;
    [Export] public float kickup;
    [Export] public float recoilSpeed;
    [Export] public Curve recoilCurve;
}
