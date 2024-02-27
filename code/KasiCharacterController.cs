using Sandbox;
using Sandbox.Citizen;
using System;


[Title("Obby Character Controller")]
[Category("Physics")]
[Icon("directions_walk")]
[EditorHandle("materials/gizmo/charactercontroller.png")]

public sealed class KasiCharacterController : Component
{
    [Property] public Vector3 Gravity { get; set; } = new Vector3(0, 0, -800f);
    
    // Movement Properties
    [Property, Group("Movement Properties")] public float MaxSpeed { get; set; } = 285.98f;
    [Property, Group("Movement Properties")] public float MoveSpeed { get; set; } = 130f;
    [Property, Group("Movement Properties")] public float RunSpeed { get; set; } = 250f;
    [Property, Group("Movement Properties")] public float CrouchSpeed { get; set; } = 85f;
    [Property, Group("Movement Properties")] public float StopSpeed { get; set; } = 80f;
    [Property, Group("Movement Properties")] public float Friction { get; set; } = 5.2f;
    [Property, Group("Movement Properties")] public float Acceleration { get; set; } = 5.5f;
    [Property, Group("Movement Properties")] public float AirAcceleration { get; set; } = 12f;
    [Property, Group("Movement Properties")] public float MaxAirWishSpeed { get; set; } = 30f;
    [Property, Group("Movement Properties")] public float JumpForce { get; set; } = 301.993378f;
    [Property, Group("Movement Properties")] private bool AutoBunnyhopping { get; set; } = false;
    
    // Stamina Properties, we don't use these but keep them here in case we want to
    [Property, Range(0f, 100f), Group("Stamina Properties")] public float MaxStamina { get; set; } = 80f;
    [Property, Range(0f, 100f), Group("Stamina Properties")] public float StaminaRecoveryRate { get; set; } = 60f;
    [Property, Range(0f, 1f), Group("Stamina Properties")] public float StaminaJumpCost { get; set; } =  0.08f;
    [Property, Range(0f, 1f), Group("Stamina Properties")] public float StaminaLandingCost { get; set; } =  0.05f;

    // Other Properties
    [Property] public float Weight { get; set; } =  1f;
    [Property] public TagSet IgnoreLayers { get; set; } = new TagSet();
    [Property] public GameObject Head { get; set; }
    [Property] public GameObject Body { get; set; }
    [Property] public BoxCollider CollisionBox { get; set; }

    // State Bools
    [Sync] public bool IsCrouching { get; set; } = false;
    public bool IsRunning = false;
    [Sync] public bool IsOnGround { get; set; } = false;

    // Internal objects
    private CitizenAnimationHelper animationHelper;
	private CameraComponent Camera;
	private ModelRenderer bodyRenderer;
    // Internal Variables
    public float Stamina = 80f;
    private float CrouchTime = 0.1f;
    private float jumpStartHeight = 0f;
    private float jumpHighestHeight = 0f;
    public bool canLook = true;
    private bool AlreadyGrounded = true;
    [Sync] private Vector3 LastSize { get; set; } = Vector3.Zero;
    
    // Size
    [Property, Group("Size")] private float Radius { get; set; } = 16;
    [Property, Group("Size")] private float StandingHeight { get; set; } = 72;
    [Property, Group("Size")] private float CroucingHeight { get; set; } = 54;
    [Sync] private float Height { get; set; } = 72f;
    [Sync] private float HeightGoal { get; set; } = 72f;
    private BBox BoundingBox => new BBox(new Vector3(0f - Radius, 0f - Radius, 0f), new Vector3(Radius, Radius, Height));
    private int _stuckTries;

    // Synced internal vars
    [Sync] private float InternalMoveSpeed { get; set; } = 250f;
    [Sync] public Vector3 WishDir { get; set; } = Vector3.Zero;
    [Sync] public Vector3 Velocity { get; set; } = Vector3.Zero;
	[Sync] public Angles LookAngle { get; set; }
    
    // Functions to make things slightly nicer

    private float GetStaminaMultiplier() {
        return Stamina / MaxStamina;
    }

    public void Punch(in Vector3 amount) {
        ClearGround();
        Velocity += amount;
    }

    private void ClearGround() {
        IsOnGround = false;
    }

    // Character Controller Functions
    
    private void Move(bool step) {
        if (step && IsOnGround)
        {
            Velocity = Velocity.WithZ(0f);
        }

        if (Velocity.Length < 0.001f)
        {
            Velocity = Vector3.Zero;
            return;
        }

        Vector3 position = base.GameObject.Transform.Position;
        CharacterControllerHelper characterControllerHelper = new CharacterControllerHelper(BuildTrace(position, position), position, Velocity);
        characterControllerHelper.Bounce = 0;
        characterControllerHelper.MaxStandableAngle = 45.5f;
        if (step && IsOnGround)
        {
            characterControllerHelper.TryMoveWithStep(Time.Delta, 18);
        }
        else
        {
            characterControllerHelper.TryMove(Time.Delta);
        }

        base.Transform.Position = characterControllerHelper.Position;
        Velocity = characterControllerHelper.Velocity;
    }
    
    private void Move()
    {
        if (!TryUnstuck())
        {
            if (IsOnGround)
            {
                Move(step: true);
            }
            else
            {
                Move(step: false);
            }
        }
    }

    private bool TryUnstuck() {
        if (!BuildTrace(base.Transform.Position, base.Transform.Position).Run().StartedSolid)
        {
            _stuckTries = 0;
            return false;
        }

        int num = 20;
        for (int i = 0; i < num; i++)
        {
            Vector3 vector = base.Transform.Position + Vector3.Random.Normal * ((float)_stuckTries / 2f);
            if (i == 0)
            {
                vector = base.Transform.Position + Vector3.Up * 2f;
            }

            if (!BuildTrace(vector, vector).Run().StartedSolid)
            {
                base.Transform.Position = vector;
                return false;
            }
        }

        _stuckTries++;
        return true;
    }

    private void CategorizePosition() {
        Vector3 position = base.Transform.Position;
        Vector3 to = position + Vector3.Down * 2f;
        Vector3 from = position;
        bool isOnGround = IsOnGround;
        if (!IsOnGround && Velocity.z > 40f)
        {
            ClearGround();
            return;
        }
        
        to.z -= (isOnGround ? 18 : 0.1f);
        SceneTraceResult sceneTraceResult = BuildTrace(from, to).Run();
        if (!sceneTraceResult.Hit || Vector3.GetAngle(in Vector3.Up, in sceneTraceResult.Normal) > 45.5)
        {
            ClearGround();
            return;
        }

        IsOnGround = true;
        // GroundObject = sceneTraceResult.GameObject;
        // GroundCollider = sceneTraceResult.Shape?.Collider as Collider;
        if (isOnGround && !sceneTraceResult.StartedSolid && sceneTraceResult.Fraction > 0f && sceneTraceResult.Fraction < 1f)
        {
            base.Transform.Position = sceneTraceResult.EndPosition + sceneTraceResult.Normal * 0f;
        }
    }

    private SceneTrace BuildTrace(Vector3 from, Vector3 to) {
        return BuildTrace(base.Scene.Trace.Ray(in from, in to));
    }

    private SceneTrace BuildTrace(SceneTrace source) {
        BBox hull = BoundingBox;
        return source.Size(in hull).WithoutTags(IgnoreLayers).IgnoreGameObjectHierarchy(base.GameObject);
    }

    private void GatherInput() {
        WishDir = 0;

        var rot = new Angles(0, Head.Transform.Rotation.Angles().yaw, 0).ToRotation();
        WishDir = Input.AnalogMove * rot;
        
        WishDir = WishDir.WithZ( 0 );

        if ( !WishDir.IsNearZeroLength ) WishDir = WishDir.Normal;

        IsRunning = Input.Down("Run");
        // IsCrouching = Input.Down("Duck");
        if (Input.Pressed("Duck")) {
            IsCrouching = !IsCrouching;
        }

        if (Input.Pressed("Duck") || Input.Released("Duck")) CrouchTime += 0.1f;
    }

    private void UpdateCitizenAnims() {
        if (animationHelper == null) return;

        animationHelper.WithWishVelocity(WishDir * InternalMoveSpeed);
        animationHelper.WithVelocity(Velocity);
        animationHelper.AimAngle = Head.Transform.Rotation;
        animationHelper.IsGrounded = IsOnGround;
        animationHelper.WithLook(Head.Transform.Rotation.Forward, 1f, 0.75f, 0.5f);
        animationHelper.MoveStyle = CitizenAnimationHelper.MoveStyles.Auto;
        animationHelper.DuckLevel = ((1 - (Height / StandingHeight)) * 3).Clamp(0, 1);
    }

    // Source engine magic functions

    private void ApplyFriction() {
        float speed, newspeed, control, drop;

        speed = Velocity.Length;

        // If too slow, return
        if (speed < 0.1f) return;
        
        drop = 0;

        // Apply ground friction
        if (IsOnGround)
        {
            // Bleed off some speed, but if we have less than the bleed
            // threshold, bleed the threshold amount.
            if (speed < StopSpeed) {
                control = StopSpeed;
            } else {
                control = speed;
            }
            // Add the amount to the drop amount.
            drop += control * Friction * Time.Delta;
        }

        // Scale the velocity
        newspeed = speed - drop;
        if (newspeed < 0) newspeed = 0;

        if (newspeed != speed)
        {
            // Determine proportion of old speed we are using.
            newspeed /= speed;
            // Adjust velocity according to proportion.
            Velocity *= newspeed;
        }

        Velocity -= (1 - newspeed) * Velocity.WithZ(0);
    }

    private void Accelerate(Vector3 wishDir, float wishSpeed, float accel) {
        float addspeed, accelspeed, currentspeed;
        
        currentspeed = Velocity.WithZ(0).Dot(wishDir);
        addspeed = wishSpeed - currentspeed;
    
        if (addspeed <= 0) return;
        
        accelspeed = accel * wishSpeed * Time.Delta;
        
        if (accelspeed > addspeed) accelspeed = addspeed;
        
        Velocity += wishDir * accelspeed;
    }

    private void AirAccelerate(Vector3 wishDir, float wishSpeed, float accel) {
        float addspeed, accelspeed, currentspeed, wshspd;

        wshspd = wishSpeed;

        // Cap Speed 
        if (wshspd > MaxAirWishSpeed)
            wshspd = MaxAirWishSpeed;

        currentspeed = Velocity.WithZ(0).Dot(wishDir);
        addspeed = wshspd - currentspeed;

        if (addspeed <= 0) return;

        accelspeed = accel * wishSpeed * Time.Delta;

        if (accelspeed > addspeed) accelspeed = addspeed;

        Velocity += wishDir * accelspeed;
    }
    
    private void GroundMove() {
        if (AlreadyGrounded == IsOnGround) {
            Accelerate(WishDir, WishDir.Length * InternalMoveSpeed * 1.8135f, Acceleration);
        }
        if (Velocity.WithZ(0).Length > MaxSpeed) {
            var FixedVel = Velocity.WithZ(0).Normal * MaxSpeed;
            Velocity = Velocity.WithX(FixedVel.x).WithY(FixedVel.y);
        }
        if (Velocity.z < 0) Velocity = Velocity.WithZ(0);

        if ((AutoBunnyhopping && Input.Down("Jump")) || Input.Pressed("Jump")) {
            animationHelper.TriggerJump();
            Punch(new Vector3(0, 0, JumpForce * GetStaminaMultiplier()));
            Stamina -= Stamina * StaminaJumpCost * 2.9625f;
            Stamina = (Stamina * 10).FloorToInt() * 0.1f;
            if (Stamina < 0) Stamina = 0;
        }
    }

    private void AirMove() {
        AirAccelerate(WishDir, WishDir.Length * InternalMoveSpeed, AirAcceleration);
    }

	// Overrides
    
    protected override void DrawGizmos() {
        Gizmo.GizmoDraw draw = Gizmo.Draw;
        BBox box = BoundingBox;
        draw.LineBBox(in box);
    }
    
	protected override void OnAwake() {
        Scene.FixedUpdateFrequency = 64;

        bodyRenderer = Body.Components.Get<ModelRenderer>();
        animationHelper = Components.GetInChildrenOrSelf<CitizenAnimationHelper>();

		if ( IsProxy )
			return;
            
		Camera = Components.GetInChildrenOrSelf<CameraComponent>();
        Camera.FieldOfView = Preferences.FieldOfView;
        
        Height = StandingHeight;
        HeightGoal = StandingHeight;
    }

    protected override void OnFixedUpdate() {
        if (CollisionBox.Scale != LastSize) {
            CollisionBox.Scale = LastSize;
            CollisionBox.Center = new Vector3(0, 0, LastSize.z / 2);
        }

        CollisionBox.Enabled = true;
        
		if ( IsProxy )
			return;
        
        CollisionBox.Enabled = false;

        GatherInput();

        InternalMoveSpeed = MoveSpeed;
        if (IsRunning) InternalMoveSpeed = RunSpeed;
        if (IsCrouching) InternalMoveSpeed = CrouchSpeed;
        InternalMoveSpeed *= GetStaminaMultiplier();
        InternalMoveSpeed *= Weight;

        // Crouching
        if (IsCrouching) {
            HeightGoal = CroucingHeight;
        } else {
            HeightGoal = StandingHeight;
            // Perform upward trace to ensure not clipping
        }
        
        var InitHeight = Height;
        Height = Height.LerpTo(HeightGoal, Time.Delta / CrouchTime.Clamp(0.125f, 0.5f));
        var HeightDiff = (InitHeight - Height).Clamp(0, 10);
        
        LastSize = new Vector3(Radius * 2, Radius * 2, HeightGoal);
        Head.Transform.LocalPosition = new Vector3(0, 0, Height * 0.89f);
        
        Velocity += Gravity * Time.Delta * 0.5f;
        
        if (AlreadyGrounded != IsOnGround) {
            if (IsOnGround) {
                var heightMult = Math.Abs(jumpHighestHeight - GameObject.Transform.Position.z) / 46f;
                Stamina -= Stamina * StaminaLandingCost * 2.9625f * heightMult.Clamp(0, 1f);
                Stamina = (Stamina * 10).FloorToInt() * 0.1f;
                if (Stamina < 0) Stamina = 0;
            } else {
                jumpStartHeight = GameObject.Transform.Position.z;
                jumpHighestHeight = GameObject.Transform.Position.z;
            }
        } else {
            if(IsOnGround) ApplyFriction();
        }
        
        if(IsOnGround) {
            GroundMove();
            //TODO: implement toggle command to show these
            //debugOverlay.Speed = Velocity.Length.CeilToInt();
        } else {
            AirMove();
            //debugOverlay.Speed = Velocity.WithZ(0).Length.CeilToInt();
        }

        AlreadyGrounded = IsOnGround;
        
        CrouchTime -= Time.Delta * 0.33f;
        CrouchTime = CrouchTime.Clamp(0f, 0.5f);

        Stamina += StaminaRecoveryRate * Time.Delta;
        if (Stamina > MaxStamina) Stamina = MaxStamina;
        
        // if (Velocity.Length != 0 || HeightDiff > 0f) {
        //     GameObject.Transform.Position += new Vector3(0, 0, HeightDiff * 0.5f);
        //     Move();
        // }
        if (HeightDiff > 0f) GameObject.Transform.Position += new Vector3(0, 0, HeightDiff * 0.5f);
        Move();
        CategorizePosition();
        
        Velocity += Gravity * Time.Delta * 0.5f;
        
        // Terminal velocity
        if (Velocity.Length > 3500) Velocity = Velocity.Normal * 3500;

        if (jumpHighestHeight < GameObject.Transform.Position.z) jumpHighestHeight = GameObject.Transform.Position.z;
    }
    
	protected override void OnUpdate() {
        UpdateCitizenAnims();
        
		bodyRenderer.RenderType = ModelRenderer.ShadowRenderType.On;

		Body.Transform.Rotation = LookAngle.WithPitch(0);
		Head.Transform.Rotation = LookAngle;
        
		if ( IsProxy )
			return;
        
		bodyRenderer.RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;

        if( canLook )
        {
            LookAngle += Input.AnalogLook * 0.022f * Preferences.Sensitivity * 3;
            LookAngle = LookAngle.WithPitch(LookAngle.pitch.Clamp(-89f, 89f));
		}

		Camera.Transform.Position = Head.Transform.Position;
		Camera.Transform.Rotation = Head.Transform.Rotation;
    }
}