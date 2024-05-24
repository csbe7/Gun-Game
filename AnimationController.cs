using Godot;
using System;

public partial class AnimationController : Node
{
    CharacterController c;
    Node3D meshPivot;
    
    AnimationTree at;

    AnimationNodeStateMachine stateMachine;
	AnimationNodeStateMachinePlayback stateMachinePlayback;
    
    Tween tween;

    [Export]public float animationTimescale = 1;
    [Export]public float rotationSpeed;
    Vector2 dir = Vector2.Zero;
    [Export]float lerpSpeed = 7f;

    [ExportCategory("IK")]
    public Skeleton3D skeleton;

    public SkeletonIK3D leftArmIK;
    public SkeletonIK3D rightArmIK;

    //public Node3D weapon;
    
    public override void _Ready()
    {
        c = GetParent().GetNode<CharacterController>("CharacterBody");
        meshPivot = GetParent().GetNode<Node3D>("Mesh");
        
        at = meshPivot.GetNode<AnimationTree>("AnimationTree");

        stateMachine = (AnimationNodeStateMachine)at.TreeRoot;
		stateMachinePlayback = (AnimationNodeStateMachinePlayback)at.Get("parameters/playback");
        
        skeleton = meshPivot.GetNode<Skeleton3D>("%GeneralSkeleton");

        rightArmIK = meshPivot.GetNode<SkeletonIK3D>("%right_arm_IK3D");
        //weapon = meshPivot.GetNode<Node3D>("%weaponAttachment/Weapon");
        leftArmIK = GetNode<SkeletonIK3D>("%left_arm_IK3D");

        //rightArmIK.Start();


        c.MovementStateChanged += onChangeMovementState;
        c.StateChanged += onChangeState;
        //stateMachinePlayback.Start("MoveState");
    }
    

    public void onChangeMovementState(MovementState ms)
    {
        if (IsInstanceValid(tween)) tween.Kill();
        
        tween = CreateTween();
        tween.TweenProperty(at, "parameters/MoveState/movementBlend/blend_position", ms.id, 0.2);
        tween.Parallel().TweenProperty(at, "parameters/MoveState/movementTimescale/scale", ms.animationSpeed, 0.7);
    }

    public void onChangeState()
    {
        if (c.holdingWeapon)
        {
            leftArmIK.Start();
            at.Set("parameters/MoveState/hasWeapon/transition_request", "has_weapon");
            if (c.isCrouching)
            {
                at.Set("parameters/MoveState/isCrouchingGun/transition_request", "is_crouching");
            }
            else at.Set("parameters/MoveState/isCrouchingGun/transition_request", "not_crouching");
        }
        else
        {
            leftArmIK.Stop();
            at.Set("parameters/MoveState/hasWeapon/transition_request", "no_weapon");
            if (c.isCrouching)
            {
                at.Set("parameters/MoveState/isCrouchingGun/transition_request", "is_crouching");
            }
            else at.Set("parameters/MoveState/isCrouchingGun/transition_request", "not_crouching");
        }
    }

    
    
    public override void _PhysicsProcess(double delta)
    {
        float Delta = (float)delta * animationTimescale * c.localTimeScale * c.game.Timescale; 
        at.Advance(Delta);
        
        //MOVE DIRECTION AND GUN TOGGLE
        Vector3 inputDir = new Vector3(c.inputDir.X, 0f, c.inputDir.Y);
        inputDir = inputDir.Rotated(Vector3.Up, (float)Math.PI/4);
        Vector3 moveDir3D = (inputDir.Z * meshPivot.GlobalBasis.Z) + (inputDir.X * meshPivot.GlobalBasis.X);
        Vector2 moveDir = new Vector2 (moveDir3D.X, moveDir3D.Z);

        dir = dir.Lerp(moveDir, Delta * lerpSpeed);
        
        if (c.holdingWeapon)
        {
            if (c.isCrouching)
            {
                at.Set("parameters/MoveState/gunCrouchBlend/blend_position", dir);
            }
            else at.Set("parameters/MoveState/gunIdleBlend/blend_position", dir);
            Rotate(c.forwardDir, rotationSpeed, Delta);
        }
        else
        {
            //at.Set("parameters/MoveState/movementBlend/blend_position", c.currMoveState.id);

            if (c.Velocity.Length() > 0)
            {
                Rotate(c.Velocity, rotationSpeed, Delta);
            }

        }
        
        //RECOIL
        if (c.recoiling) Recoil(Delta);

    }



    public void Rotate(Vector3 direction, float speed, float delta)
	{
        Vector3 scale = meshPivot.Scale;

		float angle = Mathf.LerpAngle(meshPivot.GlobalRotation.Y, Mathf.Atan2(direction.X, direction.Z), speed * delta); 
		Vector3 newRotation = new Vector3(meshPivot.GlobalRotation.X, angle, meshPivot.GlobalRotation.Z);
		meshPivot.GlobalRotation = newRotation;

		meshPivot.Scale = scale;
	}

    

    //RECOIL
    Node3D rightHandTarget; //set in ready
    int chest;
    Vector3 rightHandStartPos, chestStartPos;
    public void StartRecoil()
    {
        int bone = skeleton.FindBone(rightArmIK.TipBone);
        Transform3D boneTransformLocal = skeleton.GetBoneGlobalPose(bone);
        rightHandStartPos = boneTransformLocal.Origin;//skeleton.ToGlobal(boneTransformLocal.Origin);
        GD.Print(rightHandStartPos);
        rightHandTarget = GetNode<Node3D>("%IK_targets/right_arm_target");
        rightHandTarget.Position = rightHandStartPos;
        
        chest = skeleton.FindBone("Chest");
        boneTransformLocal = skeleton.GetBoneGlobalPose(chest);
        chestStartPos = skeleton.ToGlobal(boneTransformLocal.Origin);

        c.recoiling = true;
        rightArmIK.Start();
    }
    
    float recoilProgress = 0;
    public void Recoil(float delta)
    {
        Transform3D boneTransformLocal = skeleton.GetBoneGlobalPose(chest);
        boneTransformLocal.Origin = chestStartPos - (c.forwardDir * c.wm.currWeapon.kickback * c.wm.currWeapon.recoilCurve.Sample(recoilProgress) * 0.5f);
        
        Vector3 newRightPos = rightHandStartPos;
        newRightPos.Z = newRightPos.Z - (c.wm.currWeapon.kickback * c.wm.currWeapon.recoilCurve.Sample(recoilProgress));
        rightHandTarget.Position = newRightPos; //rightHandStartPos - (c.forwardDir * c.wm.currWeapon.kickback * c.wm.currWeapon.recoilCurve.Sample(recoilProgress));
        
        recoilProgress += delta * c.wm.currWeapon.recoilSpeed;

        if (recoilProgress > 1)
        {
            recoilProgress = 0;
            rightArmIK.Stop();
            c.recoiling = false;
        }
    }
}
